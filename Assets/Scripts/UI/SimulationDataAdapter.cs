using UnityEngine;
using Puente;

namespace FarmDashboard
{
    // Bridges the REAL simulation (Puente/GestorSimulacion + ConexionSimulacion,
    // already wired to the Python server) into DashboardState, replacing the
    // placeholder TickDemo() data with live values. Only reads from Puente's
    // public API/events -- never changes simulation behavior.
    //
    // Status string caveat: the exact "estado" vocabulary the Python server
    // sends isn't confirmed from this repo (granja.py lives outside it), so
    // MapEstado() is a best-effort keyword match with a safe fallback to
    // "Activo" plus the raw string kept on VehicleData.EstadoRaw for display.
    public class SimulationDataAdapter : MonoBehaviour
    {
        private DashboardState _state;
        private ConexionSimulacion _conexion;

        // Once the user clicks Pause, ignore OnPaso's "Running=true" until they
        // click Resume again -- otherwise a step already in flight when Pause
        // was sent (the server takes a moment to actually stop) would flip the
        // header pill straight back to "En vivo" a frame later, making Pause
        // look like it didn't do anything.
        private bool _userPaused;

        public bool IsConnected => _conexion != null;

        public void Init(DashboardState state)
        {
            _state = state;
        }

        private void Start()
        {
            var gestor = GestorSimulacion.Instancia;
            _conexion = FindFirstObjectByType<ConexionSimulacion>();

            if (gestor == null)
            {
                Debug.LogWarning("SimulationDataAdapter: no hay GestorSimulacion en la escena -- el dashboard se queda con datos demo.");
                return;
            }

            gestor.AlIniciar += OnInit;
            gestor.AlAvanzarPaso += OnPaso;
            _state.UsingLiveData = true;
        }

        private void OnDestroy()
        {
            var gestor = GestorSimulacion.Instancia;
            if (gestor == null) return;
            gestor.AlIniciar -= OnInit;
            gestor.AlAvanzarPaso -= OnPaso;
        }

        private void OnInit(InitDTO init)
        {
            _state.Config.Rows = init.filas;
            _state.Config.Cols = init.columnas;

            _state.Vehicles.Clear();
            foreach (var h in init.harvesters) _state.Vehicles.Add(BuildVehicle(h));
            foreach (var t in init.tractores) _state.Vehicles.Add(BuildVehicle(t));

            _state.Tick = 0;
            _state.NotifyChanged();
        }

        private static VehicleData BuildVehicle(AgenteInitDTO a)
        {
            bool esHarvester = a.clase == "harvester";
            return new VehicleData
            {
                Id = $"{a.clase}-{a.id}",
                Type = esHarvester ? VehicleType.Cosechador : VehicleType.Tractor,
                Label = (esHarvester ? "Cosechador " : "Tractor ") + a.id,
                Fila = a.fila,
                Col = a.col,
                Fuel = 100f,
                Status = VehicleStatus.Activo,
            };
        }

        private void OnPaso(PasoDTO paso)
        {
            // A "paso" arriving at all is the strongest signal the sim is
            // actively advancing (there's no explicit paused/running field in
            // the protocol) -- keeps the header's En vivo/Pausado pill honest
            // even before anyone touches Resume/Pause. Suppressed while
            // _userPaused, so a step already in flight when Pause was clicked
            // doesn't immediately flip the pill back to "En vivo".
            if (!_userPaused) _state.Running = true;
            _state.Tick = paso.t;

            foreach (var a in paso.agentes)
            {
                var v = _state.FindVehicle($"{a.clase}-{a.id}");
                if (v == null)
                {
                    Debug.LogWarning($"SimulationDataAdapter: paso con agente desconocido {a.clase}#{a.id}");
                    continue;
                }
                v.Fila = a.fila;
                v.Col = a.col;
                v.Fuel = a.gasolina;
                v.Carga = a.carga;
                v.CosechadoTotal = a.cosechado_total;
                v.EstadoRaw = a.estado;
                v.Status = MapEstado(a.estado);
            }

            if (paso.metricas != null)
            {
                _state.CosechadoPctServer = paso.metricas.cosechado_pct;
                _state.GranoEntregado = paso.metricas.grano_entregado;
                _state.RecargasTotales = paso.metricas.recargas_totales;
                _state.DistanciaTotalServer = paso.metricas.distancia_total;
                _state.DescomposturasTotales = paso.metricas.descomposturas_totales;
            }

            _state.NotifyChanged();
        }

        private static VehicleStatus MapEstado(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return VehicleStatus.Activo;
            var s = raw.ToLowerInvariant().Replace(" ", "").Replace("_", "");
            if (s.Contains("cargando") || s.Contains("recarga")) return VehicleStatus.Cargando;
            if (s.Contains("descargando") || s.Contains("descarga")) return VehicleStatus.Descargando;
            if (s.Contains("camino") || s.Contains("movi")) return VehicleStatus.EnCamino;
            return VehicleStatus.Activo;
        }

        public void Pause()
        {
            _userPaused = true;
            _conexion?.EnviarPausar();
        }

        public void Resume()
        {
            _userPaused = false;
            _conexion?.EnviarReanudar();
        }

        public void Restart(SimConfig cfg)
        {
            if (_conexion == null) return;
            _conexion.EnviarReiniciar(
                shape: new[] { cfg.Rows, cfg.Cols },
                nHarvesters: cfg.Cosechadores,
                nTractores: cfg.Tractores,
                pctObstaculos: cfg.PctObstaculos,
                capacidadHarvester: cfg.CapacidadCosechador,
                capacidadTractor: cfg.CapacidadTractor,
                probDescompostura: cfg.ProbDescompostura,
                seed: cfg.Semilla,
                steps: cfg.Pasos);
        }
    }
}
