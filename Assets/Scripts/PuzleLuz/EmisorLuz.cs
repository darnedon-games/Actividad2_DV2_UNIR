using System.Collections.Generic;
using UnityEngine;

// Lanza un rayo de luz hacia delante (eje Z del objeto).
// Si choca con un espejo, el rayo rebota. Si llega al receptor, lo activa.
// El rayo se dibuja con un LineRenderer.
public class EmisorLuz : MonoBehaviour
{
    public float distanciaMaxima = 50f;
    public int rebotesMaximos = 20;

    [Header("Aspecto del rayo")]
    public float anchoRayo = 0.08f;
    public Color colorRayo = Color.red;
    [Tooltip("Opcional. Si esta vacio se crea uno con el shader Sprites/Default")]
    public Material materialRayo;

    private LineRenderer linea;

    void Start()
    {
        linea = gameObject.AddComponent<LineRenderer>();
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
        Vector3 origen = transform.position;
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
                    origen = hit.point + direccion * 0.01f;
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
