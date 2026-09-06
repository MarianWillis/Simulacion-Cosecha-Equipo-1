using System;
using System.Collections.Generic;
using System.Linq;

namespace FarmDashboard
{
    public enum DashScreen { Intro, Dashboard }
    public enum DashView { Home, Camaras, Combustible, Cultivo, Tractores, Cosechadoras }
    public enum VehicleType { Tractor, Cosechador }
    public enum VehicleStatus { Activo, EnCamino, Cargando, Descargando }

    [Serializable]
    public class VehicleData
    {
        public string Id;
        public VehicleType Type;
        public string Label;

        // Demo-sim-only (see DashboardState.TickDemo): a serpentine path index
        // that ComputePos() turns into a grid cell. Real server data instead
        // sets Fila/Col directly from the agent's actual reported position.
        public int StepIndex;
        public int Fila;
        public int Col;

        public float Fuel = 100f;
        public int Rounds;
        public int CosechadoTotal;
        public int Carga;
        public VehicleStatus Status = VehicleStatus.Activo;
        // Raw "estado" string as sent by the Python server -- kept alongside
        // the best-effort Status enum mapping (see SimulationDataAdapter)
        // since the exact server vocabulary isn't confirmed from this repo.
        public string EstadoRaw;
        public int RechargeTicks;
        public float Distancia;
    }

    [Serializable]
    public class ZoneData
    {
        public string Id;
        public string Label;
        public float Pct;
        public string StatusLabel;
    }

    [Serializable]
    public class SimConfig
    {
        public int Rows = 4;
        public int Cols = 4;
        public int Cosechadores = 1;
        public int Tractores = 2;
        public int Pasos = 240;
        public int Semilla = 1337;
    }

    // Mirrors the HTML prototype's `Component` state + tickSim()/renderVals() logic.
    // THIS IS PLACEHOLDER/DEMO DATA -- per the README's data mapping table, `vehicles`,
    // `tick`, and the zone percentages must eventually be driven by the Python
    // simulation server instead of TickDemo(). Kept isolated behind small public
    // methods (RebuildVehicles/TickDemo) so swapping the data source later doesn't
    // require touching the view code that reads this state.
    public class DashboardState
    {
        public DashScreen Screen = DashScreen.Intro;
        public bool IntroFading;
        public SimConfig Config = new();
        public List<VehicleData> Vehicles = new();
        public bool Running;
        public int Tick;
        public string ActiveCamera = "general";
        public DashView View = DashView.Home;
        public string DetailVehicleId;

        // When true, SimulationDataAdapter is feeding real server data via
        // GestorSimulacion's events -- DashboardBootstrap must not also run
        // TickDemo(), or the two data sources would fight over Vehicles/Tick.
        public bool UsingLiveData;

        // Aggregate metrics from the server's MetricasDTO (see SimulationDataAdapter).
        // Only meaningful when UsingLiveData is true.
        public float CosechadoPctServer;
        public int GranoEntregado;
        public int RecargasTotales;
        public int DistanciaTotalServer;
        public int DescomposturasTotales;

        public event Action Changed;
        public void NotifyChanged() => Changed?.Invoke();

        public void RebuildVehicles()
        {
            Vehicles = new List<VehicleData>();
            for (int i = 0; i < Config.Tractores; i++)
            {
                Vehicles.Add(new VehicleData
                {
                    Id = "tractor-" + i,
                    Type = VehicleType.Tractor,
                    Label = "Tractor " + (i + 1),
                    StepIndex = i * 7,
                });
            }
            for (int i = 0; i < Config.Cosechadores; i++)
            {
                Vehicles.Add(new VehicleData
                {
                    Id = "cosechador-" + i,
                    Type = VehicleType.Cosechador,
                    Label = "Cosechador " + (i + 1),
                    StepIndex = i * 11 + 3,
                });
            }
            foreach (var v in Vehicles)
            {
                var pos = ComputePos(v.StepIndex);
                v.Fila = pos.row;
                v.Col = pos.col;
            }
            Tick = 0;
            NotifyChanged();
        }

        public (int row, int col) ComputePos(int stepIndex)
        {
            int total = Config.Rows * Config.Cols;
            int s = ((stepIndex % total) + total) % total;
            int row = s / Config.Cols;
            int colInRow = s % Config.Cols;
            int col = row % 2 == 0 ? colInRow : (Config.Cols - 1 - colInRow);
            return (row, col);
        }

        public void TickDemo()
        {
            foreach (var v in Vehicles)
            {
                if (v.RechargeTicks > 0)
                {
                    v.RechargeTicks -= 1;
                    v.Fuel = Math.Min(100f, v.Fuel + 34f);
                    v.Status = v.RechargeTicks == 0 ? VehicleStatus.Activo : VehicleStatus.Cargando;
                    continue;
                }
                if (v.Fuel <= 12f)
                {
                    v.RechargeTicks = 3;
                    v.Status = VehicleStatus.Cargando;
                    continue;
                }
                int prevStep = v.StepIndex;
                v.StepIndex += 1;
                v.Distancia += 10f;
                int total = Config.Rows * Config.Cols;
                bool wrapped = (v.StepIndex / total) > (prevStep / total);
                if (wrapped) v.Rounds += 1;
                v.Fuel = Math.Max(0f, v.Fuel - (v.Type == VehicleType.Cosechador ? 1.6f : 1.1f));
                if (v.Type == VehicleType.Cosechador && wrapped) v.Status = VehicleStatus.Descargando;
                else v.Status = (v.StepIndex % 5 == 0) ? VehicleStatus.EnCamino : VehicleStatus.Activo;

                var pos = ComputePos(v.StepIndex);
                v.Fila = pos.row;
                v.Col = pos.col;
            }

            Tick += 1;
            if (Config.Pasos > 0 && Tick >= Config.Pasos)
            {
                Running = false;
            }
            NotifyChanged();
        }

        public List<ZoneData> ComputeZones()
        {
            var zones = new List<ZoneData>();
            for (int i = 0; i < 4; i++)
            {
                float pct = Math.Clamp(Tick * 3f - i * 45f, 0f, 100f);
                string label = pct >= 100f ? "Completado" : pct > 0f ? "En progreso" : "Pendiente";
                zones.Add(new ZoneData { Id = "zona-" + (i + 1), Label = "Zona " + (i + 1), Pct = pct, StatusLabel = label });
            }
            return zones;
        }

        public float CosechadoPct()
        {
            var zones = ComputeZones();
            return zones.Count == 0 ? 0f : zones.Sum(z => z.Pct) / zones.Count;
        }

        public VehicleData FindVehicle(string id) => Vehicles.FirstOrDefault(v => v.Id == id);
        public IEnumerable<VehicleData> Tractors => Vehicles.Where(v => v.Type == VehicleType.Tractor);
        public IEnumerable<VehicleData> Cosechadores => Vehicles.Where(v => v.Type == VehicleType.Cosechador);
    }
}
