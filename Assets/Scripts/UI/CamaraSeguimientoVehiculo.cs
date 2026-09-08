using Puente;
using UnityEngine;

namespace FarmDashboard
{
    // La camara de la mitad derecha de Combustible/Tractores/Cosechadoras:
    // persigue al vehiculo que el usuario marco en la lista (ver
    // SeguimientoVehiculoView y DashboardState.VehiculoSeguido).
    //
    // Angulo tres-cuartos, no directamente atras como las de la pestana
    // Camaras: aca importa VER al vehiculo (su carga, si esta parado en el
    // silo), no ir montado en el.
    public class CamaraSeguimientoVehiculo : MonoBehaviour
    {
        private const float Atras = 5.5f;
        private const float Lado = 3.5f;
        private const float Arriba = 3.5f;
        private const float Mira = 0.8f;

        private const int TexturaAncho = 960;
        private const int TexturaAlto = 540;

        private DashboardState _state;
        private Camera _camara;
        private CameraFeedSlot _slot;
        private Vector3 _centroCampo;
        private bool _hayCampo;

        public void Init(DashboardState state) => _state = state;

        private void Start()
        {
            var go = new GameObject("CamaraSeguimiento");
            go.transform.SetParent(transform, false);

            _camara = go.AddComponent<Camera>();
            _camara.fieldOfView = 55f;
            _camara.nearClipPlane = 0.1f;
            _camara.enabled = false; // hasta que la pestana este abierta
            _camara.targetTexture = new RenderTexture(TexturaAncho, TexturaAlto, 24)
            {
                name = "RT_Seguimiento",
            };
            _camara.aspect = TexturaAncho / (float)TexturaAlto;

            _slot = CameraFeedSlot.Find(SeguimientoVehiculoView.NombreSlot);
            if (_slot != null) _slot.SetFeed(_camara.targetTexture);

            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar += AlIniciar;
        }

        private void OnDestroy()
        {
            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar -= AlIniciar;

            if (_camara == null || _camara.targetTexture == null) return;
            var rt = _camara.targetTexture;
            _camara.targetTexture = null;
            rt.Release();
            Destroy(rt);
        }

        private void AlIniciar(InitDTO init)
        {
            var gestor = GestorSimulacion.Instancia;
            if (gestor == null) return;

            float tam = gestor.TamCelda;
            _centroCampo = new Vector3((init.columnas - 1) * tam / 2f, 0f, (init.filas - 1) * tam / 2f);
            _hayCampo = init.filas > 0 && init.columnas > 0;
            _camara.farClipPlane = Mathf.Max(init.filas, init.columnas) * tam * 2f + 200f;
        }

        // LateUpdate: MovimientoSuave interpola a los agentes en Update, y
        // colocar la camara antes deja al vehiculo temblando un frame atras.
        private void LateUpdate()
        {
            if (_camara == null || _slot == null) return;

            _camara.enabled = _slot.Image.isActiveAndEnabled;
            if (!_camara.enabled || !_hayCampo || _state == null) return;

            var gestor = GestorSimulacion.Instancia;
            if (gestor == null) return;
            float tam = gestor.TamCelda;

            var objetivo = BuscarSeguido(gestor);
            if (objetivo == null)
            {
                SeguimientoCamara.Fija(_camara, _centroCampo, tam, 7f, 5f);
                return;
            }

            SeguimientoCamara.Perseguir(_camara, objetivo, tam, Atras, Arriba, Mira, Lado);
        }

        // VehicleData.Id es "<clase>-<id>" (ver SimulationDataAdapter), que es
        // justo lo que GestorSimulacion necesita para dar con el Transform.
        private Transform BuscarSeguido(GestorSimulacion gestor)
        {
            var v = _state.VehiculoSeguido();
            if (v == null) return null;

            int guion = v.Id.LastIndexOf('-');
            if (guion <= 0 || guion == v.Id.Length - 1) return null;
            if (!int.TryParse(v.Id.Substring(guion + 1), out int id)) return null;

            return gestor.BuscarAgente(v.Id.Substring(0, guion), id);
        }
    }
}
