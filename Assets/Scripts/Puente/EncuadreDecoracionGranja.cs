using System.Collections.Generic;
using UnityEngine;

namespace Puente
{
    /// <summary>
    /// Reparte la decoracion de fondo ("Surrounding decoration*") alrededor
    /// del campo cada vez que llega un "init" nuevo, pegada al borde de
    /// afuera y ajustandose sola al tamano del grid.
    ///
    /// Cada grupo de arboles se queda con su propio angulo alrededor del
    /// centro del campo (el mismo orden angular que ya tenian en la escena,
    /// pero repartidos parejo) y se coloca justo donde ese angulo cruza el
    /// rectangulo del campo inflado por: la mitad del tamano real del grupo
    /// (medido con sus Renderers) + un margen. Asi nunca se traslapan con el
    /// campo ni se amontonan todos del mismo lado.
    /// </summary>
    public class EncuadreDecoracionGranja : MonoBehaviour
    {
        private const string PrefijoNombre = "Surrounding decoration";

        [Tooltip("Aire extra entre el borde del campo y la decoracion.")]
        [SerializeField] private float margen = 60f;

        private class Grupo
        {
            public Transform T;
            public float AlturaY;
            public float HalfX;
            public float HalfZ;
            public float AnguloOriginal;
            // El pivote de estos grupos NO coincide con el centro de los
            // arboles (medido: hasta ~358 unidades de desfase). Si se
            // coloca el pivote en el borde, el bosque entero cae encima
            // del campo. Se guarda el desfase para colocar el CENTRO real
            // y despues devolver la posicion que le toca al pivote.
            public Vector3 PivoteMenosCentro;
        }

        private readonly List<Grupo> _grupos = new();

        private void Start()
        {
            var raices = new List<GameObject>();
            gameObject.scene.GetRootGameObjects(raices);

            foreach (var raiz in raices)
            {
                if (!raiz.name.StartsWith(PrefijoNombre)) continue;
                var bounds = MedirBounds(raiz.transform);
                if (bounds == null) continue;

                var pos = raiz.transform.position;
                var centroReal = bounds.Value.center;
                _grupos.Add(new Grupo
                {
                    T = raiz.transform,
                    AlturaY = pos.y,
                    HalfX = bounds.Value.extents.x,
                    HalfZ = bounds.Value.extents.z,
                    PivoteMenosCentro = pos - centroReal,
                    // Angulo respecto al origen del campo, solo para conservar
                    // el orden en que ya estaban repartidos alrededor.
                    AnguloOriginal = Mathf.Atan2(centroReal.x, centroReal.z),
                });
            }

            _grupos.Sort((a, b) => a.AnguloOriginal.CompareTo(b.AnguloOriginal));

            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar += Reencuadrar;
        }

        private void OnDestroy()
        {
            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar -= Reencuadrar;
        }

        private void Reencuadrar(InitDTO init)
        {
            if (_grupos.Count == 0) return;

            // Rectangulo del campo calculado directo de filas/columnas (no
            // medido con Renderers): al reiniciar, los objetos del init
            // anterior siguen vivos un frame mas (Destroy es diferido) y
            // ensuciarian la medicion.
            float tam = GestorSimulacion.Instancia.TamCelda;
            var centro = new Vector3((init.columnas - 1) * tam / 2f, 0f, (init.filas - 1) * tam / 2f);
            float campoHalfX = init.columnas * tam / 2f;
            float campoHalfZ = init.filas * tam / 2f;

            for (int i = 0; i < _grupos.Count; i++)
            {
                var g = _grupos[i];
                if (g.T == null) continue;

                // Repartidos parejo en el circulo, conservando el orden que
                // ya tenian entre ellos.
                float angulo = (i + 0.5f) * 2f * Mathf.PI / _grupos.Count;
                float dirX = Mathf.Sin(angulo);
                float dirZ = Mathf.Cos(angulo);

                // Donde ese angulo cruza el rectangulo del campo inflado por
                // el tamano del propio grupo + margen (interseccion rayo-caja).
                float limiteX = campoHalfX + g.HalfX + margen;
                float limiteZ = campoHalfZ + g.HalfZ + margen;
                float tX = Mathf.Abs(dirX) < 0.0001f ? float.MaxValue : limiteX / Mathf.Abs(dirX);
                float tZ = Mathf.Abs(dirZ) < 0.0001f ? float.MaxValue : limiteZ / Mathf.Abs(dirZ);
                float distancia = Mathf.Min(tX, tZ);

                // Donde debe quedar el CENTRO real del bosque...
                var centroDestino = new Vector3(
                    centro.x + dirX * distancia,
                    0f,
                    centro.z + dirZ * distancia);

                // ...y de ahi se saca donde va el pivote (que esta desfasado).
                g.T.position = new Vector3(
                    centroDestino.x + g.PivoteMenosCentro.x,
                    g.AlturaY,
                    centroDestino.z + g.PivoteMenosCentro.z);
            }
        }

        private static Bounds? MedirBounds(Transform raiz)
        {
            var renderers = raiz.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return null;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
