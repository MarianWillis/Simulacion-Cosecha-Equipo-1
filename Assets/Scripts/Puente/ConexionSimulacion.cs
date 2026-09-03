using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NativeWebSocket;
using UnityEngine;

namespace Puente
{
    /// <summary>
    /// Cliente WebSocket hacia puente_unity.py usando NativeWebSocket (funciona
    /// igual en el Editor, builds de escritorio y WebGL). Python sigue siendo
    /// el servidor: empuja el estado de la simulacion (init/paso/fin) y ahora
    /// tambien escucha comandos de control que Unity puede mandar en cualquier
    /// momento por el mismo socket (ver puente_unity.py, _escuchar_comandos):
    /// pausar, reanudar y reiniciar con parametros nuevos (num. de harvesters/
    /// tractores, tamano de grid, etc).
    ///
    /// NativeWebSocket ya entrega OnMessage/OnOpen/OnClose en el hilo
    /// principal siempre que DispatchMessageQueue() se llame desde Update
    /// (necesario fuera de WebGL), asi que no hace falta una cola thread-safe
    /// como con ClientWebSocket. Se mantiene una cola simple para procesar un
    /// solo mensaje por frame (ver comentario en Update).
    /// </summary>
    public class ConexionSimulacion : MonoBehaviour
    {
        [SerializeField] private string host = "localhost";
        [SerializeField] private int puerto = 8765;

        private WebSocket socket;
        private readonly Queue<string> mensajesPendientes = new();

        public bool Conectado => socket != null && socket.State == WebSocketState.Open;

        private async void Start()
        {
            var uri = $"ws://{host}:{puerto}";
            socket = new WebSocket(uri);

            socket.OnOpen += () => Debug.Log("Conectado al puente de simulacion.");
            socket.OnError += (mensaje) => Debug.LogError($"Error de socket con el puente: {mensaje}");
            socket.OnClose += (codigo) => Debug.LogWarning($"Conexion con el puente cerrada ({codigo}).");
            socket.OnMessage += (bytes) => mensajesPendientes.Enqueue(Encoding.UTF8.GetString(bytes));

            await socket.Connect();
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            socket?.DispatchMessageQueue();
#endif
            // Un solo mensaje por frame: si Python se adelanta (p.ej. Unity
            // tardo en instanciar el terreno en ManejarInit), la cola se
            // vacia en frames sucesivos en vez de aplicar de golpe varios
            // "paso" en el mismo frame, que hacia parecer que se saltaba
            // una fila entera de cultivo apenas arrancaba la simulacion.
            if (mensajesPendientes.Count > 0)
            {
                ProcesarMensaje(mensajesPendientes.Dequeue());
            }
        }

        private void ProcesarMensaje(string json)
        {
            string tipo;
            try
            {
                tipo = (string)JObject.Parse(json)["tipo"];
            }
            catch (Exception ex)
            {
                Debug.LogError($"Mensaje del puente no es JSON valido: {ex.Message}");
                return;
            }

            var gestor = GestorSimulacion.Instancia;
            if (gestor == null)
            {
                Debug.LogWarning("Llego un mensaje del puente pero no hay GestorSimulacion en la escena.");
                return;
            }

            switch (tipo)
            {
                case "init":
                    gestor.ManejarInit(JsonConvert.DeserializeObject<InitDTO>(json));
                    break;
                case "paso":
                    gestor.ManejarPaso(JsonConvert.DeserializeObject<PasoDTO>(json));
                    break;
                case "fin":
                    gestor.ManejarFin(JsonConvert.DeserializeObject<FinDTO>(json));
                    break;
                default:
                    Debug.LogWarning($"Tipo de mensaje desconocido del puente: {tipo}");
                    break;
            }
        }

        private void EnviarComando(object comando)
        {
            if (!Conectado)
            {
                Debug.LogWarning("No se puede mandar el comando: el socket con el puente no esta abierto.");
                return;
            }
            socket.SendText(JsonConvert.SerializeObject(comando,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        public void EnviarPausar() => EnviarComando(new ComandoSimpleDTO { tipo = "pausar" });

        public void EnviarReanudar() => EnviarComando(new ComandoSimpleDTO { tipo = "reanudar" });

        /// <summary>
        /// Reinicia la simulacion en Python con nuevos parametros. Cualquier
        /// argumento en null/0 se omite del mensaje y el puente conserva el
        /// valor actual de ese parametro (ver PARAMETROS en granja.py).
        /// </summary>
        public void EnviarReiniciar(int[] shape = null, int? nHarvesters = null,
            int? nTractores = null, int? seed = null, int? steps = null)
        {
            EnviarComando(new ComandoReiniciarDTO
            {
                parametros = new ParametrosReinicioDTO
                {
                    shape = shape,
                    n_harvesters = nHarvesters,
                    n_tractores = nTractores,
                    seed = seed,
                    steps = steps,
                }
            });
        }

        private async void OnApplicationQuit()
        {
            if (socket != null && socket.State == WebSocketState.Open)
            {
                await socket.Close();
            }
        }
    }
}
