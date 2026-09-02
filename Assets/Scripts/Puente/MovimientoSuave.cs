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
        private Quaternion rotacionObjetivo;
        private float velocidad = 3f;

        private void Awake()
        {
            posicionObjetivo = transform.position;
            rotacionObjetivo = transform.rotation;
        }

        /// <param name="velocidadCeldasPorSegundo">
        /// Distancia recorrida por segundo, calculada por GestorSimulacion
        /// a partir de tamCelda / intervalo esperado entre pasos, para que
        /// el movimiento termine mas o menos cuando llega el proximo paso.
        /// </param>
        public void FijarObjetivo(Vector3 posicion, Quaternion rotacion, float velocidadCeldasPorSegundo)
        {
            posicionObjetivo = posicion;
            rotacionObjetivo = rotacion;
            velocidad = velocidadCeldasPorSegundo;
        }

        private void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, posicionObjetivo,
                velocidad * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacionObjetivo,
                360f * Time.deltaTime);
        }
    }
}
