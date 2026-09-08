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
            _userPaused = false;
            _state.Running = true;
            _state.SimulationGeneration++;
            _state.Config.Rows = init.filas;
            _state.Config.Cols = init.columnas;
            _state.Config.Cosechadores = init.harvesters?.Count ?? 0;
            _state.Config.Tractores = init.tractores?.Count ?? 0;
            if (init.harvesters != null && init.harvesters.Count > 0)
                _state.Config.CapacidadCosechador = init.harvesters[0].capacidad;
            if (init.tractores != null && init.tractores.Count > 0)
                _state.Config.CapacidadTractor = init.tractores[0].capacidad;
            System.Array.Clear(_state.CropZoneTotals, 0, _state.CropZoneTotals.Length);
            System.Array.Clear(_state.CropZoneHarvested, 0, _state.CropZoneHarvested.Length);
            _state.CosechadoPctServer = 0f;
            _state.GranoEntregado = 0;
            _state.RecargasTotales = 0;
            _state.DistanciaTotalServer = 0;
            _state.DescomposturasTotales = 0;
            if (init.trigo_listo != null)
                foreach (var cell in init.trigo_listo)
                    _state.CropZoneTotals[CropZone(cell[0], cell[1], init.filas, init.columnas)]++;

            _state.Vehicles.Clear();
            if (init.harvesters != null)
                for (int i = 0; i < init.harvesters.Count; i++)
                    _state.Vehicles.Add(BuildVehicle(init.harvesters[i], i + 1));
            if (init.tractores != null)
                for (int i = 0; i < init.tractores.Count; i++)
                    _state.Vehicles.Add(BuildVehicle(init.tractores[i], i + 1));

            _state.Tick = 0;
            _state.NotifyChanged();
        }

        private static VehicleData BuildVehicle(AgenteInitDTO a, int displayNumber)
        {
            bool esHarvester = a.clase == "harvester";
            return new VehicleData
            {
                Id = $"{a.clase}-{a.id}",
                Type = esHarvester ? VehicleType.Cosechador : VehicleType.Tractor,
                // Keep the Python ID in Id for matching future paso messages,
                // but show people a simple 1..N number within each vehicle type.
                Label = (esHarvester ? "Cosechador " : "Tractor ") + displayNumber,
                Fila = a.fila,
                Col = a.col,
                Fuel = 100f,
                FuelRaw = a.gasolina_max,
                FuelMax = a.gasolina_max,
                Capacity = a.capacidad,
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
                v.FuelRaw = a.gasolina;
                v.Fuel = v.FuelMax > 0f ? Mathf.Clamp01(a.gasolina / v.FuelMax) * 100f : 0f;
                v.Carga = a.carga;
                v.CosechadoTotal = a.cosechado_total;
                v.EstadoRaw = a.estado;
                v.Status = MapEstado(a.estado);
            }

            if (paso.cosechadas != null)
                foreach (var cell in paso.cosechadas)
                    _state.CropZoneHarvested[CropZone(cell[0], cell[1], _state.Config.Rows, _state.Config.Cols)]++;

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

        // DEFINICION DE LAS ZONAS -- unico lugar donde se decide que celda
        // cae en que zona. Son los 4 CUADRANTES del campo (2x2), numerados
        // como se ven en pantalla con la camara mirando hacia abajo (las
        // filas crecen hacia arriba, las columnas hacia la derecha):
        //
        //     Zona 1 (0) | Zona 2 (1)      <- mitad de arriba (filas altas)
        //     -----------+-----------
        //     Zona 3 (2) | Zona 4 (3)      <- mitad de abajo  (filas bajas)
        //
        // Las camaras de la vista Cultivo encuadran exactamente estos mismos
        // cuadrantes (ver CropZoneBounds / CamarasZonasCultivo), asi que el
        // porcentaje de cada tarjeta corresponde a lo que se ve en ella.
        public static int CropZone(int row, int col, int rows, int cols)
        {
            if (rows <= 0 || cols <= 0) return 0;
            int mitadFila = Mathf.Clamp(row * 2 / rows, 0, 1); // 0 = abajo, 1 = arriba
            int mitadCol = Mathf.Clamp(col * 2 / cols, 0, 1);  // 0 = izquierda, 1 = derecha
            return (1 - mitadFila) * 2 + mitadCol;
        }

        // Rectangulo de celdas (inclusive) que cubre una zona. Es la inversa
        // exacta de CropZone: sale de la misma division en mitades, para que
        // el encuadre de la camara y el conteo de celdas nunca se separen.
        public static void CropZoneBounds(int zone, int rows, int cols,
            out int rowStart, out int rowEnd, out int colStart, out int colEnd)
        {
            zone = Mathf.Clamp(zone, 0, 3);
            int mitadFila = 1 - zone / 2;
            int mitadCol = zone % 2;

            Mitad(mitadFila, rows, out rowStart, out rowEnd);
            Mitad(mitadCol, cols, out colStart, out colEnd);
        }

        // Indices [inicio, fin] de la mitad 0 o 1 de un lado de N celdas,
        // usando el mismo corte que 'indice * 2 / N'.
        private static void Mitad(int mitad, int total, out int inicio, out int fin)
        {
            if (total <= 0) { inicio = 0; fin = 0; return; }
            inicio = Mathf.CeilToInt(mitad * total / 2f);
            fin = Mathf.CeilToInt((mitad + 1) * total / 2f) - 1;
            inicio = Mathf.Clamp(inicio, 0, total - 1);
            fin = Mathf.Clamp(fin, inicio, total - 1);
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
