using System;
using System.Collections.Generic;
using UnityEngine;

namespace Puente
{
    /// <summary>
    /// Traduce los mensajes del puente (init/paso/fin) a objetos de escena:
    /// instancia el terreno (obstaculos, trigo, silo, base) una vez con
    /// "init", y en cada "paso" mueve agentes existentes y reemplaza el
    /// trigo recien cosechado por prefabTrigoCosechado. No hay logica de
    /// simulacion aca: todo el estado
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
        [SerializeField] private GameObject prefabTrigoCosechado;
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

        // Expuesto para EncuadreCamaraGranja: necesita el mismo factor de
        // escala que usa CeldaAMundo para calcular donde cae cada esquina
        // del campo en unidades de mundo.
        public float TamCelda => tamCelda;

        // Hooks de solo-lectura para el dashboard de UI (Assets/Scripts/UI):
        // se disparan al final de ManejarInit/ManejarPaso sin cambiar nada de
        // la logica de esta clase. GestorSimulacion no conoce ni depende del
        // dashboard -- es el dashboard el que se suscribe.
        public event Action<InitDTO> AlIniciar;
        public event Action<PasoDTO> AlAvanzarPaso;

        private void Awake()
        {
            Instancia = this;
        }

        // Publico para las camaras del dashboard (CamarasAngulos): necesitan
        // saber donde cae el silo/la base en unidades de mundo, y esa cuenta
        // tiene que salir de aca para no duplicar el mapeo fila->Z, col->X.
        public Vector3 CeldaAMundo(int fila, int col)
        {
            return new Vector3(col * tamCelda, 0f, fila * tamCelda);
        }

        // Transform vivo de un agente ya instanciado, o null si todavia no
        // existe (o si el ultimo "init" lo quito). Solo lectura: quien lo
        // pida NO debe moverlo -- de eso se encarga ManejarPaso.
        public Transform BuscarAgente(string clase, int id) =>
            agentes.TryGetValue((ClaseId(clase), id), out var t) ? t : null;

        private static int ClaseId(string clase) => clase == "harvester" ? 0 : 1;

        public void ManejarInit(InitDTO init)
        {
            // tamCelda es un ajuste puramente visual del lado Unity (ver
            // Inspector): a granja.py no le importa cuantas unidades de
            // Unity mide una celda, asi que el init.tam_celda del mensaje
            // no se usa aca.

            Debug.Log($"[Puente] Init recibido: grid={init.filas}x{init.columnas} " +
                $"harvesters={init.harvesters?.Count ?? 0} tractores={init.tractores?.Count ?? 0} " +
                $"obstaculos={init.obstaculos?.Count ?? 0} trigo_listo={init.trigo_listo?.Count ?? 0} " +
                $"camino={init.camino?.Count ?? 0}");

            // Un "init" puede llegar mas de una vez: el puente manda otro
            // cada vez que Unity pide "reiniciar" con parametros nuevos
            // (num. de agentes, tamano de grid, etc), sin cerrar el socket.
            // Hay que limpiar lo instanciado por el init anterior antes de
            // reconstruir la escena.
            LimpiarEscena();

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

            AlIniciar?.Invoke(init);
        }

        private void LimpiarEscena()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            agentes.Clear();
            trigoPorCelda.Clear();
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

                // velocidad segun la distancia real a recorrer en este paso,
                // no una celda fija: un agente puede avanzar varias celdas
                // por paso (ver Maquina.moverse()/velocidad_tractor en
                // granja.py), y PasoDTO solo manda la posicion final. Con una
                // velocidad fija calibrada para 1 celda, el deslizamiento de
                // un salto de varias celdas tardaba varios intervalos en
                // completarse: el proximo "paso" sobreescribia el objetivo
                // antes de llegar, y el agente se quedaba cada vez mas atras
                // de la celda que el mensaje ya reportaba como cosechada.
                float distancia = Vector3.Distance(t.position, destino);
                float velocidad = distancia / Mathf.Max(intervaloEsperadoEntrePasos, 0.001f);

                var movimiento = t.GetComponent<MovimientoSuave>();
                if (movimiento != null)
                    movimiento.FijarObjetivo(destino, rotacion, velocidad);
                else
                    t.SetPositionAndRotation(destino, rotacion);
            }

            foreach (var celda in paso.cosechadas)
            {
                var clave = (celda[0], celda[1]);
                if (trigoPorCelda.TryGetValue(clave, out var trigo) && trigo != null)
                {
                    if (prefabTrigoCosechado != null)
                    {
                        var reemplazo = Instantiate(prefabTrigoCosechado, trigo.transform.position,
                            trigo.transform.rotation, transform);
                        trigoPorCelda[clave] = reemplazo;
                        Destroy(trigo);
                    }
                    else
                    {
                        trigo.SetActive(false);
                    }
                }
            }

            UltimasMetricas = paso.metricas;

            string gasolinaPorAgente = string.Join(", ",
                paso.agentes.ConvertAll(a => $"{a.clase}#{a.id}={a.gasolina:F1}"));
            string cosechadoPorHarvester = string.Join(", ",
                paso.agentes.FindAll(a => a.clase == "harvester")
                    .ConvertAll(a => $"harvester#{a.id}={a.cosechado_total}"));
            Debug.Log($"[t={paso.t}] cosechado={paso.metricas.cosechado_pct:F1}% " +
                $"entregado={paso.metricas.grano_entregado} " +
                $"recargas={paso.metricas.recargas_totales} " +
                $"distancia={paso.metricas.distancia_total} " +
                $"descomposturas={paso.metricas.descomposturas_totales} " +
                $"gasolina=[{gasolinaPorAgente}] " +
                $"cosechado_x_harvester=[{cosechadoPorHarvester}]");

            AlAvanzarPaso?.Invoke(paso);
        }

        public void ManejarFin(FinDTO fin)
        {
            Debug.Log("Simulacion terminada. Reportes: " +
                Newtonsoft.Json.JsonConvert.SerializeObject(fin.reportes));
        }
    }
}
