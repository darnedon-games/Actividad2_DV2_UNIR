using UnityEngine;

public class SpawnCreature : MonoBehaviour
{
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private Transform spawnPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Instantiate(creaturePrefab, spawnPoint.position, spawnPoint.rotation);
            Destroy(this);
        }
    }
}
