using UnityEngine;

public class SpawnCreature : MonoBehaviour
{
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private SoundManager efectoSonido;

    void Start()
    {

    }

    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Instantiate(creaturePrefab, spawnPoint.position, spawnPoint.rotation);
            efectoSonido.ReproducirSonidoAparicion();
            Destroy(this);
        }
    }
}
