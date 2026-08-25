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
    public float velocidad = 5f;
    public float velocidadRotacion = 5f;
    public bool enBucle = false;

    private readonly List<Vector3> puntosRuta = new List<Vector3>();
    private int indiceActual = 0;

    void Start()
    {
        GenerarRutaZigzag();
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
        Vector3 direccion = destino - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.001f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, destino) < 0.1f)
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
