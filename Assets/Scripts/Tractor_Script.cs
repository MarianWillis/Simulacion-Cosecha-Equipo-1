using System.Collections.Generic;
using UnityEngine;

public class Tractor_Script : MonoBehaviour
{
    [Header("Configuración del Zigzag")]
    public int numeroDeFilas = 6;
    public float largoDeFila = 20f;
    public float espacioEntreFilas = 3f;
    public bool avanzaEnZ = true;

    [Header("Movimiento")]
    public float velocidad = 2f;
    [Tooltip("Grados por segundo que puede girar el tractor. Un valor bajo produce curvas amplias (como manejando); uno alto gira casi en el sitio.")]
    public float velocidadRotacion = 200f;
    public float radioLlegada = 1.5f;
    public bool enBucle = false;

    [Header("Corrección de orientación del modelo")]
    [Tooltip("Si el frente del modelo 3D no coincide con el eje Z del objeto, ajusta este ángulo (ej. 180 si el tractor avanza de espaldas).")]
    public float anguloOffsetModelo = 0f;

    private readonly List<Vector3> puntosRuta = new List<Vector3>();
    private int indiceActual = 0;
    private Vector3 direccionActual;

    void Start()
    {
        GenerarRutaZigzag();
        direccionActual = transform.forward;
    }

    void GenerarRutaZigzag()
    {
        puntosRuta.Clear();
        Vector3 posicionInicial = transform.position;
        puntosRuta.Add(posicionInicial);

        for (int fila = 0; fila < numeroDeFilas; fila++)
        {
            bool haciaAdelante = fila % 2 == 0;
            Vector3 offsetLargo = avanzaEnZ ? Vector3.forward * largoDeFila : Vector3.right * largoDeFila;
            if (!haciaAdelante) offsetLargo = -offsetLargo;

            Vector3 finalDeFila = puntosRuta[puntosRuta.Count - 1] + offsetLargo;
            puntosRuta.Add(finalDeFila);

            bool esUltimaFila = fila == numeroDeFilas - 1;
            if (!esUltimaFila)
            {
                Vector3 offsetLateral = avanzaEnZ ? Vector3.right * espacioEntreFilas : Vector3.forward * espacioEntreFilas;
                puntosRuta.Add(finalDeFila + offsetLateral);
            }
        }
    }

    void Update()
    {
        if (puntosRuta.Count == 0) return;

        Vector3 destino = puntosRuta[indiceActual];
        Vector3 haciaDestino = destino - transform.position;
        haciaDestino.y = 0f;

        if (haciaDestino.sqrMagnitude > 0.0001f)
        {
            float radianesMaximos = velocidadRotacion * Mathf.Deg2Rad * Time.deltaTime;
            direccionActual = Vector3.RotateTowards(direccionActual, haciaDestino.normalized, radianesMaximos, 0f).normalized;
        }

        transform.position += direccionActual * velocidad * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direccionActual) * Quaternion.Euler(0f, anguloOffsetModelo, 0f);

        Vector3 posicionPlana = new Vector3(transform.position.x, destino.y, transform.position.z);
        if (Vector3.Distance(posicionPlana, destino) < radioLlegada)
        {
            indiceActual++;
            if (indiceActual >= puntosRuta.Count)
            {
                indiceActual = enBucle ? 0 : puntosRuta.Count - 1;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (puntosRuta.Count < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < puntosRuta.Count - 1; i++)
        {
            Gizmos.DrawLine(puntosRuta[i], puntosRuta[i + 1]);
        }
    }
}
