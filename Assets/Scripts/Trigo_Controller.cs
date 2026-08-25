using UnityEngine;

public class Trigo_Controller : MonoBehaviour
{
    public string tagTractor = "Tractor";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagTractor))
        {
            gameObject.SetActive(false);
        }
    }
}
