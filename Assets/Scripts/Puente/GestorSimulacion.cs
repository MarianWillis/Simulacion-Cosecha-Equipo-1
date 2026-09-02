using System.Collections.Generic;
using UnityEngine;

namespace Puente
{
    /// <summary>
    /// Traduce los mensajes del puente (init/paso/fin) a objetos de escena:
    /// instancia el terreno (obstaculos, trigo, silo, base) una vez con
    /// "init", y en cada "paso" mueve agentes existentes y apaga el trigo
    /// recien cosechado. No hay logica de simulacion aca: todo el estado
    /// (gasolina, rutas, colisiones) vive en granja.py.
    /// </summary>
    public class GestorSimulacion : MonoBehaviour
    {
        public static GestorSimulacion Instancia { get; private set; }

        [Header("Prefabs de agentes")]
        [SerializeField] private GameObject prefabHarvester;
        [SerializeField] private GameObject prefabTractor;

        [Header("Prefabs de terreno")]
        [SerializeField] private GameObject prefabCamino;
        [SerializeField] private GameObject prefabTrigo;
        [SerializeField] private GameObject prefabObstaculoRoca;   // tipo_visual 0
        [SerializeField] private GameObject prefabObstaculoPoste;  // tipo_visual 1
        [SerializeField] private GameObject prefabSilo;
        [SerializeField] private GameObject prefabBase;

        [Header("Config")]
        [Tooltip("Unidades de Unity por celda de grid. Puramente visual: separa " +
                 "obstaculos/trigo/agentes entre si sin tocar la simulacion en Python.")]
        [SerializeField] private float tamCelda = 1f;
        [Tooltip("Debe coincidir en orden de magnitud con --intervalo del puente, " +
                 "para que el movimiento interpolado no vaya ni muy lento ni muy rapido.")]
        [SerializeField] private float intervaloEsperadoEntrePasos = 0.2f;

        private readonly Dictionary<(int clase, int id), Transform> agentes = new();
        private readonly Dictionary<(int fila, int col), GameObject> trigoPorCelda = new();

        public MetricasDTO UltimasMetricas { get; private set; }

        private void Awake()
        {
            Instancia = this;
        }

        private Vector3 CeldaAMundo(int fila, int col)
        {
            return new Vector3(col * tamCelda, 0f, fila * tamCelda);
        }

        private static int ClaseId(string clase) => clase == "harvester" ? 0 : 1;

        public void ManejarInit(InitDTO init)
        {
            // tamCelda es un ajuste puramente visual del lado Unity (ver
            // Inspector): a granja.py no le importa cuantas unidades de
            // Unity mide una celda, asi que el init.tam_celda del mensaje
            // no se usa aca.

            if (prefabCamino != null && init.camino != null)
            {
                foreach (var celda in init.camino)
                    Instantiate(prefabCamino, CeldaAMundo(celda[0], celda[1]), Quaternion.identity, transform);
            }

            foreach (var obstaculo in init.obstaculos)
            {
                var prefab = obstaculo.tipo_visual == 0 ? prefabObstaculoRoca : prefabObstaculoPoste;
                Instantiate(prefab, CeldaAMundo(obstaculo.fila, obstaculo.col), Quaternion.identity, transform);
            }

            foreach (var celda in init.trigo_listo)
            {
                int fila = celda[0], col = celda[1];
                var instancia = Instantiate(prefabTrigo, CeldaAMundo(fila, col), Quaternion.identity, transform);
                trigoPorCelda[(fila, col)] = instancia;
            }

            if (init.silo != null && init.silo.Length == 2)
                Instantiate(prefabSilo, CeldaAMundo(init.silo[0], init.silo[1]), Quaternion.identity, transform);

            if (init.baseCombustible != null && init.baseCombustible.Length == 2)
                Instantiate(prefabBase, CeldaAMundo(init.baseCombustible[0], init.baseCombustible[1]),
                    Quaternion.identity, transform);

            foreach (var h in init.harvesters)
                CrearAgente(prefabHarvester, "harvester", h.id, h.fila, h.col);

            foreach (var t in init.tractores)
                CrearAgente(prefabTractor, "tractor", t.id, t.fila, t.col);
        }

        private void CrearAgente(GameObject prefab, string clase, int id, int fila, int col)
        {
            var instancia = Instantiate(prefab, CeldaAMundo(fila, col), Quaternion.identity, transform);
            if (instancia.GetComponent<MovimientoSuave>() == null)
                instancia.AddComponent<MovimientoSuave>();
            agentes[(ClaseId(clase), id)] = instancia.transform;
        }

        public void ManejarPaso(PasoDTO paso)
        {
            float velocidad = tamCelda / Mathf.Max(intervaloEsperadoEntrePasos, 0.001f);

            foreach (var estado in paso.agentes)
            {
                if (!agentes.TryGetValue((ClaseId(estado.clase), estado.id), out var t))
                {
                    Debug.LogWarning($"Paso con agente desconocido: {estado.clase} #{estado.id}");
                    continue;
                }

                Vector3 destino = CeldaAMundo(estado.fila, estado.col);
                Quaternion rotacion = t.rotation;
                if (estado.direccion != null && (estado.direccion[0] != 0 || estado.direccion[1] != 0))
                {
                    // direccion = [df, dc] en coordenadas de grid (fila, col);
                    // CeldaAMundo mapea fila->Z, col->X, asi que el forward
                    // en mundo es (dc, 0, df).
                    Vector3 adelante = new Vector3(estado.direccion[1], 0f, estado.direccion[0]);
                    rotacion = Quaternion.LookRotation(adelante);
                }

                var movimiento = t.GetComponent<MovimientoSuave>();
                if (movimiento != null)
                    movimiento.FijarObjetivo(destino, rotacion, velocidad);
                else
                    t.SetPositionAndRotation(destino, rotacion);
            }

            foreach (var celda in paso.cosechadas)
            {
                if (trigoPorCelda.TryGetValue((celda[0], celda[1]), out var trigo) && trigo != null)
                    trigo.SetActive(false);
            }

            UltimasMetricas = paso.metricas;
        }

        public void ManejarFin(FinDTO fin)
        {
            Debug.Log("Simulacion terminada. Reportes: " +
                Newtonsoft.Json.JsonConvert.SerializeObject(fin.reportes));
        }
    }
}
