using UnityEngine;

namespace Puente
{
    /// <summary>
    /// Interpola posicion/rotacion hacia el ultimo objetivo fijado por
    /// GestorSimulacion, para que el agente no salte celda a celda cada
    /// vez que llega un mensaje "paso" discreto.
    /// </summary>
    public class MovimientoSuave : MonoBehaviour
    {
        private Vector3 posicionObjetivo;
        private float velocidad = 3f;

        private void Awake()
        {
            posicionObjetivo = transform.position;
        }

        /// <param name="velocidadCeldasPorSegundo">
        /// Distancia recorrida por segundo, calculada por GestorSimulacion
        /// a partir de la distancia real al destino, para que el movimiento
        /// termine mas o menos cuando llega el proximo paso.
        /// </param>
        public void FijarObjetivo(Vector3 posicion, Quaternion rotacion, float velocidadCeldasPorSegundo)
        {
            posicionObjetivo = posicion;
            velocidad = velocidadCeldasPorSegundo;
            // la rotacion no se interpola: granja.py no distingue un paso
            // "solo girar" (el giro llega junto con el avance a la siguiente
            // celda, ver Maquina.moverse()), asi que animarla por separado
            // solo generaba el efecto de "cortar camino" en las esquinas.
            transform.rotation = rotacion;
        }

        private void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, posicionObjetivo,
                velocidad * Time.deltaTime);
        }
    }
}
