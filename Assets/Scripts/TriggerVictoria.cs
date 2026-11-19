using UnityEngine;

// Va en un objeto con un collider marcado como "Is Trigger".
// Cuando el jugador entra, se abre el menú de victoria.
public class TriggerVictoria : MonoBehaviour
{
    [SerializeField] private MenuVictoria menuVictoria;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            menuVictoria.Mostrar();
        }
    }
}
