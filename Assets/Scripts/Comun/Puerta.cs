using UnityEngine;

// La puerta se queda cerrada hasta que alguien llama a Abrir().
// Al abrirse sube hacia arriba poco a poco.
public class Puerta : MonoBehaviour
{
    [SerializeField] private float alturaApertura = 3.2f;
    [SerializeField] private float velocidad = 2f;
    [SerializeField] private bool pintarPuerta = true;

    private bool abierta = false;
    private Vector3 posicionCerrada;
    private Vector3 posicionAbierta;

    void Start()
    {
        posicionCerrada = transform.position;
        posicionAbierta = posicionCerrada + Vector3.up * alturaApertura;

        // le damos un color para que se distinga de las paredes
        if (pintarPuerta)
        {
            GetComponent<Renderer>().material.color = new Color(0.45f, 0.25f, 0.1f);
        }
    }

    void Update()
    {
        if (abierta)
        {
            transform.position = Vector3.MoveTowards(transform.position, posicionAbierta, velocidad * Time.deltaTime);
        }
    }

    public void Abrir()
    {
        if (abierta == false)
        {
            abierta = true;
            Debug.Log("Puerta abierta!");
        }
    }
}
