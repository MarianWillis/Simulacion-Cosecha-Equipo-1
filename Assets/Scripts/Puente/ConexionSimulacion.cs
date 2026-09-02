using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Puente
{
    /// <summary>
    /// Cliente WebSocket hacia puente_unity.py, con System.Net.WebSockets
    /// (parte de .NET, no requiere paquetes adicionales; funciona en el
    /// Editor y en builds de escritorio, no en WebGL). Python es el
    /// servidor y empuja el estado; esta clase solo escucha y reparte cada
    /// mensaje a GestorSimulacion segun su campo "tipo".
    ///
    /// La recepcion corre en una tarea de fondo (ClientWebSocket.ReceiveAsync
    /// no se puede llamar desde Update sin bloquear); los mensajes completos
    /// se encolan en `mensajesPendientes` y se procesan en Update() para que
    /// todo el trabajo con la API de Unity (Instantiate, etc.) quede en el
    /// hilo principal.
    /// </summary>
    public class ConexionSimulacion : MonoBehaviour
    {
        [SerializeField] private string host = "localhost";
        [SerializeField] private int puerto = 8765;

        private ClientWebSocket socket;
        private CancellationTokenSource cts;
        private readonly ConcurrentQueue<string> mensajesPendientes = new();

        private async void Start()
        {
            socket = new ClientWebSocket();
            cts = new CancellationTokenSource();
            var uri = new Uri($"ws://{host}:{puerto}");

            try
            {
                await socket.ConnectAsync(uri, cts.Token);
                Debug.Log("Conectado al puente de simulacion.");
                _ = EscucharMensajes();
            }
            catch (Exception ex)
            {
                Debug.LogError($"No se pudo conectar al puente ({uri}): {ex.Message}");
            }
        }

        private async Task EscucharMensajes()
        {
            var buffer = new byte[8192];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var acumulado = new StringBuilder();
                    WebSocketReceiveResult resultado;
                    do
                    {
                        resultado = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        if (resultado.MessageType == WebSocketMessageType.Close)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                            return;
                        }
                        acumulado.Append(Encoding.UTF8.GetString(buffer, 0, resultado.Count));
                    } while (!resultado.EndOfMessage);

                    mensajesPendientes.Enqueue(acumulado.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                // cierre normal via OnApplicationQuit
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Conexion con el puente terminada: {ex.Message}");
            }
        }

        private void Update()
        {
            // Un solo mensaje por frame: si Python se adelanta (p.ej. Unity
            // tardo en instanciar el terreno en ManejarInit), la cola se
            // vacia en frames sucesivos en vez de aplicar de golpe varios
            // "paso" en el mismo frame, que hacia parecer que se saltaba
            // una fila entera de cultivo apenas arrancaba la simulacion.
            if (mensajesPendientes.TryDequeue(out var json))
            {
                ProcesarMensaje(json);
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

        private async void OnApplicationQuit()
        {
            cts?.Cancel();
            if (socket != null && socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "salida", CancellationToken.None);
                }
                catch (Exception)
                {
                    // la app ya esta cerrando, no hay mucho que hacer con el error
                }
            }
        }
    }
}
