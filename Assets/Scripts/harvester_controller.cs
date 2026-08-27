using UnityEngine;

public class harvester_controller : MonoBehaviour
{
    public float velocidad = 2f;

    void Update()
    {
        transform.position += transform.forward * velocidad * Time.deltaTime;
    }
}
