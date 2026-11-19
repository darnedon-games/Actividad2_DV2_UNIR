using UnityEngine;

// Solo se usa para abrir la primera puerta del juego

public class TriggerStart : MonoBehaviour
{
    [SerializeField] private Puerta puertaStart;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            puertaStart.Abrir();
        }
    }
}
