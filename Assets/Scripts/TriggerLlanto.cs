using UnityEngine;

// Va en un objeto con un collider marcado como "Is Trigger".
// Cuando el jugador entra, suena el llanto del SoundManager.
public class TriggerLlanto : MonoBehaviour
{
    [SerializeField] private SoundManager soundManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            soundManager.ReproducirSonidoLlanto();
            // quitamos este script del trigger para que no vuelva a sonar
            Destroy(this);
        }
    }
}
