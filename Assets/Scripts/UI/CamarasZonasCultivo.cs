using Puente;
using UnityEngine;

namespace FarmDashboard
{
    // Crea las 4 camaras de la vista Cultivo (una por zona) y las mantiene
    // encuadradas sobre su cuadrante en cada "init". No hay nada que
    // arrastrar en el Inspector: las camaras, sus RenderTextures y el
    // enganche con los slots ("zona-1".."zona-4" de CultivoCamarasView) se
    // arman solos, igual que el resto del dashboard.
    //
    // Las zonas son los mismos 4 cuadrantes (2x2) que cuenta el panel de
    // Cultivo -- la definicion vive en SimulationDataAdapter.CropZone, aca
    // solo se pide el rectangulo con CropZoneBounds -- para que el
    // porcentaje de abajo corresponda a lo que se ve en la camara.
    //
    // Encuadre: misma idea que EncuadreCamaraGranja (la camara general se
    // sube lo necesario para que quepa todo el campo, y se reajusta sola
    // cuando el "init" trae otro tamano), solo que aca el rectangulo a
    // cubrir no es el campo entero sino el cuadrante de la zona -- asi que
    // el CENTRO cambia por camara, no solo la altura.
    //
    // Para que el cuadrante se vea COMPLETO y sin sobras, la imagen toma el
    // aspecto del propio cuadrante: la RenderTexture se crea con ese
    // aspecto y el slot lo respeta con un AspectRatioFitter (FitInParent).
    // Si en vez de eso se estirara la imagen a la tarjeta, la camara
    // tendria que abrirse hasta el aspecto de la tarjeta y entrarian
    // celdas de las zonas vecinas (que era justo lo que se veia antes).
    public class CamarasZonasCultivo : MonoBehaviour
    {
        // 60 de FOV, el mismo de la camara general.
        private const float Fov = 60f;
        // Un poco de aire alrededor del cuadrante para que no quede pegado
        // al borde de la imagen.
        private const float Margen = 1.06f;
        // Lado mayor de la RenderTexture. Solo define cuantos pixeles tiene
        // la imagen; el ENCUADRE sale del aspecto del cuadrante.
        private const int TexturaLadoMayor = 768;
        private const int TexturaLadoMinimo = 64;

        private Camera[] _camaras;
        private CameraFeedSlot[] _slots;
        private int _filas;
        private int _columnas;

        private void Start()
        {
            _camaras = new Camera[CultivoCamarasView.NumZonas];
            _slots = new CameraFeedSlot[CultivoCamarasView.NumZonas];

            for (int i = 0; i < _camaras.Length; i++)
            {
                _camaras[i] = CrearCamara(i);
                _slots[i] = CameraFeedSlot.Find(CultivoCamarasView.NombreSlot(i));
            }

            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar += Reencuadrar;
        }

        private void OnDestroy()
        {
            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar -= Reencuadrar;

            if (_camaras == null) return;
            for (int i = 0; i < _camaras.Length; i++) LiberarTextura(i);
        }

        private Camera CrearCamara(int indice)
        {
            var go = new GameObject($"CamaraZona{indice + 1}");
            go.transform.SetParent(transform, false);
            // Mirando derecho hacia abajo; el "arriba" de la imagen es +Z,
            // asi que las filas crecen hacia arriba en pantalla.
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = Fov;
            cam.nearClipPlane = 0.3f;
            cam.enabled = false; // hasta que la tarjeta este visible (ver Update)
            return cam;
        }

        private void Update()
        {
            if (_camaras == null) return;

            for (int i = 0; i < _camaras.Length; i++)
            {
                if (_camaras[i] == null || _slots[i] == null) continue;

                // Estas camaras solo se dibujan cuando la vista Cultivo esta
                // abierta: renderizar 4 camaras extra todo el tiempo cuesta
                // caro (mismo motivo por el que el trigo necesito GPU
                // Instancing).
                _camaras[i].enabled = _camaras[i].targetTexture != null
                                      && _slots[i].Image.isActiveAndEnabled;
            }
        }

        private void Reencuadrar(InitDTO init)
        {
            _filas = init.filas;
            _columnas = init.columnas;
            for (int i = 0; i < _camaras.Length; i++) Encuadrar(i);
        }

        private void Encuadrar(int indice)
        {
            if (_camaras == null || _camaras[indice] == null) return;
            if (_filas <= 0 || _columnas <= 0) return;
            if (GestorSimulacion.Instancia == null) return;

            float tam = GestorSimulacion.Instancia.TamCelda;

            // Cuadrante de esta zona, pedido a quien lo define (el mismo
            // corte con el que se cuentan las celdas cosechadas).
            SimulationDataAdapter.CropZoneBounds(indice, _filas, _columnas,
                out int filaInicio, out int filaFin, out int colInicio, out int colFin);

            // Rectangulo a cubrir: solo el cuadrante. El centro es el suyo,
            // no el del campo -- por eso cada camara mira a otro lado.
            float ancho = (colFin - colInicio + 1) * tam;
            float profundidad = (filaFin - filaInicio + 1) * tam;
            var centro = new Vector3(
                (colInicio + colFin) * tam / 2f,
                0f,
                (filaInicio + filaFin) * tam / 2f);

            float aspecto = AjustarTextura(indice, ancho / profundidad);

            // La camara ve, sobre el suelo, un alto de 2*altura*tan(fov/2) y
            // un ancho de eso por el aspecto. Se toma la altura que satisfaga
            // AMBOS lados: como el aspecto de la imagen ya es el del
            // cuadrante, los dos dan practicamente lo mismo y el cuadrante
            // llena el cuadro.
            float necesario = Mathf.Max(profundidad, ancho / aspecto);
            float altura = necesario * Margen / (2f * Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad));

            _camaras[indice].transform.position = new Vector3(centro.x, altura, centro.z);
            _camaras[indice].farClipPlane = altura * 1.2f + 100f;
        }

        // Deja la RenderTexture (y el slot que la muestra) con el aspecto del
        // cuadrante. Devuelve el aspecto REAL conseguido, que es el que usa el
        // encuadre: los lados son enteros, asi que puede diferir un pelo del
        // pedido y conviene que camara e imagen usen exactamente el mismo.
        private float AjustarTextura(int indice, float aspectoPedido)
        {
            if (aspectoPedido <= 0f || float.IsNaN(aspectoPedido) || float.IsInfinity(aspectoPedido))
                aspectoPedido = 1f;

            int ancho, alto;
            if (aspectoPedido >= 1f)
            {
                ancho = TexturaLadoMayor;
                alto = Mathf.Clamp(Mathf.RoundToInt(TexturaLadoMayor / aspectoPedido), TexturaLadoMinimo, TexturaLadoMayor);
            }
            else
            {
                alto = TexturaLadoMayor;
                ancho = Mathf.Clamp(Mathf.RoundToInt(TexturaLadoMayor * aspectoPedido), TexturaLadoMinimo, TexturaLadoMayor);
            }

            var actual = _camaras[indice].targetTexture;
            if (actual == null || actual.width != ancho || actual.height != alto)
            {
                LiberarTextura(indice);
                var textura = new RenderTexture(ancho, alto, 24) { name = $"RT_Zona{indice + 1}" };
                _camaras[indice].targetTexture = textura;
                if (_slots[indice] != null) _slots[indice].SetFeed(textura);
            }

            float aspectoReal = ancho / (float)alto;
            // La camara toma el aspecto de su targetTexture sola, pero se
            // deja explicito: si alguien le puso otro aspecto antes, se queda
            // pegado y la escena sale deformada.
            _camaras[indice].aspect = aspectoReal;
            if (_slots[indice] != null) _slots[indice].SetAspect(aspectoReal);
            return aspectoReal;
        }

        private void LiberarTextura(int indice)
        {
            if (_camaras[indice] == null) return;

            var vieja = _camaras[indice].targetTexture;
            if (vieja == null) return;

            _camaras[indice].targetTexture = null;
            vieja.Release();
            Destroy(vieja);
        }
    }
}
