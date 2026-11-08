using System.Collections.Generic;
using UnityEngine;

// Lanza un rayo de luz hacia delante (eje Z del objeto).
// Si choca con un espejo, el rayo rebota. Si llega al receptor, lo activa.
// El rayo se dibuja con un LineRenderer.
public class EmisorLuz : MonoBehaviour
{
    public float alturaSalida = 0f;
    public float distanciaMaxima = 50f;
    public int rebotesMaximos = 20;

    [Header("Aspecto del rayo")]
    public float anchoRayo = 0.08f;
    public Color colorRayo = Color.red;
    public Material materialRayo;

    private LineRenderer linea;

    void Start()
    {
        // si el prefab ya trae su propio LineRenderer lo usamos, si no lo creamos
        linea = GetComponent<LineRenderer>();
        if (linea == null)
        {
            linea = gameObject.AddComponent<LineRenderer>();
        }
        linea.startWidth = anchoRayo;
        linea.endWidth = anchoRayo;

        if (materialRayo != null)
        {
            linea.material = materialRayo;
        }
        else
        {
            linea.material = new Material(Shader.Find("Sprites/Default"));
        }
        linea.startColor = colorRayo;
        linea.endColor = colorRayo;
    }

    void Update()
    {
        List<Vector3> puntos = new List<Vector3>();

        // un rayo que empieza dentro de un collider no choca con el,
        // asi que podemos salir desde dentro del propio emisor
        Vector3 origen = transform.position + transform.up * alturaSalida;
        Vector3 direccion = transform.forward;
        puntos.Add(origen);

        for (int i = 0; i < rebotesMaximos; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(origen, direccion, out hit, distanciaMaxima, ~0, QueryTriggerInteraction.Ignore))
            {
                puntos.Add(hit.point);

                // GetComponentInParent por si el collider esta en un hijo del prefab
                Espejo espejo = hit.collider.GetComponentInParent<Espejo>();
                Receptor receptor = hit.collider.GetComponentInParent<Receptor>();

                if (espejo != null)
                {
                    // rebotamos el rayo usando la normal de la cara del espejo
                    direccion = Vector3.Reflect(direccion, hit.normal);

                    // el rebote lo hacemos desde el CENTRO del espejo y no desde su cara.
                    // Si no, cada rebote desplaza un poco el rayo por el grosor del espejo
                    // y acaba pasando rozando otros espejos en vez de chocar con ellos.
                    Vector3 centroEspejo = espejo.transform.position;
                    centroEspejo.y = hit.point.y;
                    puntos[puntos.Count - 1] = centroEspejo;

                    // (un rayo que empieza dentro de un collider no choca con el,
                    // asi que no choca con el propio espejo al salir)
                    origen = centroEspejo + direccion * 0.01f;
                }
                else if (receptor != null)
                {
                    receptor.Activar();
                    break;
                }
                else
                {
                    // ha chocado con una pared, el jugador u otra cosa
                    break;
                }
            }
            else
            {
                // no choca con nada, dibujamos el rayo hasta la distancia maxima
                puntos.Add(origen + direccion * distanciaMaxima);
                break;
            }
        }

        linea.positionCount = puntos.Count;
        linea.SetPositions(puntos.ToArray());
    }
}
