using UnityEngine;

// Attach to a purely-visual child of the tractor (NOT the transform that
// Tractor_Script drives every frame) so the engine-idle jitter never fights
// the movement/rotation logic written to the root transform.
public class EngineIdleVibration : MonoBehaviour
{
    [Header("Vibración de motor")]
    [Tooltip("Qué tan fuerte es el temblor en posición (unidades de mundo).")]
    public float amplitudPosicion = 0.008f;
    [Tooltip("Qué tan fuerte es el temblor en rotación (grados).")]
    public float amplitudRotacionGrados = 0.35f;
    [Tooltip("Qué tan rápido vibra. Más alto = temblor más nervioso/rápido.")]
    public float frecuencia = 18f;

    private Vector3 posicionBase;
    private Quaternion rotacionBase;
    private float semilla;

    void Awake()
    {
        posicionBase = transform.localPosition;
        rotacionBase = transform.localRotation;
        semilla = Random.Range(0f, 1000f);
    }

    void Update()
    {
        float t = Time.time * frecuencia;

        float dx = (Mathf.PerlinNoise(semilla, t) - 0.5f) * 2f;
        float dy = (Mathf.PerlinNoise(semilla + 37f, t) - 0.5f) * 2f;
        float dz = (Mathf.PerlinNoise(semilla + 71f, t) - 0.5f) * 2f;
        transform.localPosition = posicionBase + new Vector3(dx, dy, dz) * amplitudPosicion;

        float rx = (Mathf.PerlinNoise(semilla + 113f, t) - 0.5f) * 2f;
        float ry = (Mathf.PerlinNoise(semilla + 149f, t) - 0.5f) * 2f;
        float rz = (Mathf.PerlinNoise(semilla + 191f, t) - 0.5f) * 2f;
        transform.localRotation = rotacionBase * Quaternion.Euler(new Vector3(rx, ry, rz) * amplitudRotacionGrados);
    }
}
