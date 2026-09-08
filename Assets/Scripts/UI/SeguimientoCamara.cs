using UnityEngine;

namespace FarmDashboard
{
    // Camara de persecucion en tercera persona, compartida por las dos
    // pestanas que la usan (CamarasAngulos, en Camaras, y
    // CamaraSeguimientoVehiculo, en Combustible/Tractores/Cosechadoras).
    // Vive aparte para que las dos se muevan EXACTAMENTE igual: si se ajusta
    // el suavizado o el salto, se ajusta en un solo lugar.
    //
    // Las distancias van en CELDAS y se multiplican por TamCelda, el mismo
    // factor con el que GestorSimulacion coloca todo en el mundo.
    public static class SeguimientoCamara
    {
        // Suavizado exponencial (1/s). Independiente del framerate; mas alto
        // = la camara se pega mas al agente y tiembla mas en los giros.
        public const float Suavizado = 5f;

        public static void Perseguir(Camera cam, Transform objetivo, float tam,
            float atras, float arriba, float mira, float lado = 0f)
        {
            if (cam == null || objetivo == null) return;

            Vector3 punto = objetivo.position + Vector3.up * (mira * tam);
            Vector3 deseada = punto
                              - objetivo.forward * (atras * tam)
                              + objetivo.right * (lado * tam)
                              + Vector3.up * (arriba * tam);

            // De un salto si esta lejisimos: al abrir la pestana por primera
            // vez la camara sigue donde quedo, y al cambiar de vehiculo (o
            // reiniciar la sim) el objetivo aparece en otra esquina. Sin esto
            // se ve un barrido de varios segundos cruzando todo el campo.
            if (Vector3.Distance(cam.transform.position, deseada) > atras * tam * 6f)
            {
                cam.transform.SetPositionAndRotation(deseada, Quaternion.LookRotation(punto - deseada));
                return;
            }

            float t = 1f - Mathf.Exp(-Suavizado * Time.deltaTime);
            var pos = Vector3.Lerp(cam.transform.position, deseada, t);
            var rot = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(punto - pos), t);
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        // Camara quieta mirando un punto del campo, desde atras-izquierda.
        public static void Fija(Camera cam, Vector3 punto, float tam, float atras, float arriba)
        {
            if (cam == null) return;
            var pos = punto + new Vector3(-atras, arriba, -atras) * tam;
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(punto - pos));
        }
    }
}
