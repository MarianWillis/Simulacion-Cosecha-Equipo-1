using Puente;
using UnityEngine;

namespace FarmDashboard
{
    // Las cuatro camaras de la pestana "Camaras" (ver CamarasView, que arma
    // las tarjetas y define el orden de los angulos). Igual que
    // CamarasZonasCultivo: se crean solas, se enganchan a los slots por
    // nombre y solo renderizan mientras la pestana esta abierta.
    //
    // A diferencia de las camaras de Cultivo (cenitales, quietas, encuadrando
    // un rectangulo fijo), estas van A RAS DE CAMPO y en movimiento:
    //
    //   0  Cosechadora -- persecucion en tercera persona del primer harvester
    //   1  Tractor     -- lo mismo con el primer tractor
    //   2  Silo        -- fija, angulo bajo sobre el punto de entrega
    //   3  Dron        -- orbita lenta alrededor del centro del campo
    //
    // Todas las distancias estan en CELDAS y se multiplican por TamCelda, que
    // es el mismo factor con el que GestorSimulacion coloca todo en el mundo:
    // asi el encuadre no cambia si se ajusta la escala en el Inspector.
    public class CamarasAngulos : MonoBehaviour
    {
        private const int Cosechadora = 0, Tractor = 1, Silo = 2, Dron = 3;

        // Persecucion: cuantas celdas atras y arriba del agente va la camara,
        // y a que altura mira. Da un angulo de ~20 grados: se ve al agente de
        // cuerpo entero y unas celdas de trigo por delante.
        private const float SeguirAtras = 6f;
        private const float SeguirArriba = 3.2f;
        private const float SeguirMira = 0.8f;

        // Camara fija del silo: atras-izquierda y arriba del punto de entrega.
        private const float SiloAtras = 7f;
        private const float SiloArriba = 5f;

        // Dron: radio y altura como fraccion del lado mayor del campo, y
        // grados por segundo. El radio es chico a proposito -- la vista
        // completa del campo ya la da la camara general de Home.
        private const float DronRadio = 0.34f;
        private const float DronAltura = 0.30f;
        private const float DronGrados = 7f;

        private const int TexturaAncho = 960;
        private const int TexturaAlto = 540;

        private Camera[] _camaras;
        private CameraFeedSlot[] _slots;

        private int _idCosechadora = -1, _idTractor = -1;
        private Vector3 _puntoSilo, _centroCampo;
        private float _radioDron, _alturaDron, _anguloDron;
        private bool _hayCampo;

        private void Start()
        {
            _camaras = new Camera[CamarasView.NumAngulos];
            _slots = new CameraFeedSlot[CamarasView.NumAngulos];

            for (int i = 0; i < _camaras.Length; i++)
            {
                _camaras[i] = CrearCamara(i);
                _slots[i] = CameraFeedSlot.Find(CamarasView.NombreSlot(i));
                if (_slots[i] != null) _slots[i].SetFeed(_camaras[i].targetTexture);
            }

            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar += AlIniciar;
        }

        private void OnDestroy()
        {
            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar -= AlIniciar;

            if (_camaras == null) return;
            foreach (var cam in _camaras)
            {
                if (cam == null || cam.targetTexture == null) continue;
                var rt = cam.targetTexture;
                cam.targetTexture = null;
                rt.Release();
                Destroy(rt);
            }
        }

        private Camera CrearCamara(int indice)
        {
            var go = new GameObject($"CamaraAngulo{indice + 1}");
            go.transform.SetParent(transform, false);

            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.enabled = false; // hasta que la pestana este abierta (ver LateUpdate)
            cam.targetTexture = new RenderTexture(TexturaAncho, TexturaAlto, 24)
            {
                name = $"RT_Angulo{indice + 1}",
            };
            cam.aspect = TexturaAncho / (float)TexturaAlto;
            return cam;
        }

        private void AlIniciar(InitDTO init)
        {
            var gestor = GestorSimulacion.Instancia;
            if (gestor == null) return;

            // El primer harvester y el primer tractor del init. CamarasView
            // muestra los datos del primer vehiculo de cada tipo, y
            // DashboardState los guarda en el mismo orden: es el mismo agente.
            _idCosechadora = init.harvesters != null && init.harvesters.Count > 0 ? init.harvesters[0].id : -1;
            _idTractor = init.tractores != null && init.tractores.Count > 0 ? init.tractores[0].id : -1;

            float tam = gestor.TamCelda;
            _centroCampo = new Vector3((init.columnas - 1) * tam / 2f, 0f, (init.filas - 1) * tam / 2f);
            _puntoSilo = init.silo != null && init.silo.Length == 2
                ? gestor.CeldaAMundo(init.silo[0], init.silo[1])
                : _centroCampo;

            float lado = Mathf.Max(init.filas, init.columnas) * tam;
            _radioDron = lado * DronRadio;
            _alturaDron = lado * DronAltura;
            _hayCampo = init.filas > 0 && init.columnas > 0;

            // Clip lejano segun el tamano del campo: con el valor por defecto
            // (1000) un grid grande se cortaba a media pantalla.
            float lejos = lado * 2f + 200f;
            foreach (var cam in _camaras) if (cam != null) cam.farClipPlane = lejos;

            SeguimientoCamara.Fija(_camaras[Silo], _puntoSilo, tam, SiloAtras, SiloArriba);
        }

        // LateUpdate y no Update: MovimientoSuave interpola la posicion de los
        // agentes en Update, y si la camara se coloca antes que eso va siempre
        // un frame atras (se ve un temblor en el agente perseguido).
        private void LateUpdate()
        {
            if (_camaras == null) return;

            bool alguna = false;
            for (int i = 0; i < _camaras.Length; i++)
            {
                if (_camaras[i] == null || _slots[i] == null) continue;
                _camaras[i].enabled = _slots[i].Image.isActiveAndEnabled;
                alguna |= _camaras[i].enabled;
            }
            // Nada que mover si la pestana esta cerrada.
            if (!alguna || !_hayCampo) return;

            var gestor = GestorSimulacion.Instancia;
            if (gestor == null) return;
            float tam = gestor.TamCelda;

            Perseguir(Cosechadora, gestor.BuscarAgente("harvester", _idCosechadora), tam);
            Perseguir(Tractor, gestor.BuscarAgente("tractor", _idTractor), tam);
            Orbitar(Dron);
        }

        private void Perseguir(int indice, Transform objetivo, float tam)
        {
            var cam = _camaras[indice];
            if (cam == null || !cam.enabled) return;

            if (objetivo == null)
            {
                // Sin agente (todavia no llego el init, o se reinicio con
                // menos agentes): se queda mirando el campo en vez de
                // apuntar al origen.
                SeguimientoCamara.Fija(cam, _centroCampo, tam, SiloAtras, SiloArriba);
                return;
            }

            SeguimientoCamara.Perseguir(cam, objetivo, tam, SeguirAtras, SeguirArriba, SeguirMira);
        }

        private void Orbitar(int indice)
        {
            var cam = _camaras[indice];
            if (cam == null || !cam.enabled) return;

            _anguloDron += DronGrados * Time.deltaTime;
            float rad = _anguloDron * Mathf.Deg2Rad;
            var pos = _centroCampo
                      + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * _radioDron
                      + Vector3.up * _alturaDron;
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(_centroCampo - pos));
        }
    }
}
