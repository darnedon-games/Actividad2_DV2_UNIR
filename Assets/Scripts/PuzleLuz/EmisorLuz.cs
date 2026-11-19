using System.Collections.Generic;
using UnityEngine;

// Lanza un rayo de luz hacia delante (eje Z del objeto).
// Si choca con un espejo, el rayo rebota. Si llega al receptor, lo activa.
// El rayo se dibuja con un LineRenderer y brilla (con el Bloom del postprocesado).
// Además pone una luz pequeña en cada espejo donde rebota y donde termina,
// para que el rayo ilumine lo que tiene alrededor.
public class EmisorLuz : MonoBehaviour
{
    public float alturaSalida = 0f;
    public float distanciaMaxima = 50f;
    public int rebotesMaximos = 20;

    [Header("Aspecto del rayo")]
    public float anchoRayo = 0.08f;
    public Color colorRayo = Color.white;
    // cuánto brilla el rayo: 1 = color normal; más de 1 hace que el Bloom le dé un halo
    public float brilloRayo = 3f;
    // si lo dejas vacío se crea uno; si se coloca otro material, el brillo depende de ese material
    public Material materialRayo;

    [Header("Luces del rayo")]
    public bool lucesEnImpactos = true;
    public float intensidadLuces = 2f;
    public float alcanceLuces = 2.5f;

    private LineRenderer linea;

    // las luces se crean una vez y se reutilizan (se activan solo las que hacen falta)
    private List<Light> luces = new List<Light>();

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
            // un color muy blanco (valores mayores que 1): así el Bloom lo hace brillar
            linea.material.color = new Color(brilloRayo, brilloRayo, brilloRayo, 1f);
        }
        linea.startColor = colorRayo;
        linea.endColor = colorRayo;
    }

    void Update()
    {
        List<Vector3> puntos = new List<Vector3>();
        // dónde ponemos una luz: en cada espejo y donde termina el rayo
        List<Vector3> impactos = new List<Vector3>();

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
                    impactos.Add(centroEspejo);

                    // (un rayo que empieza dentro de un collider no choca con el,
                    // asi que no choca con el propio espejo al salir)
                    origen = centroEspejo + direccion * 0.01f;
                }
                else
                {
                    // ha chocado con el receptor, una pared, el jugador u otra cosa:
                    // la luz va un poco por delante del punto de choque para no quedar dentro
                    impactos.Add(hit.point - direccion * 0.15f);

                    if (receptor != null)
                    {
                        receptor.Activar();
                    }
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

        ColocarLuces(impactos);
    }

    // Pone una luz en cada punto y apaga las que sobran
    private void ColocarLuces(List<Vector3> posiciones)
    {
        int necesarias = 0;
        if (lucesEnImpactos)
        {
            necesarias = posiciones.Count;
        }

        // si faltan luces, creamos las que hagan falta
        while (luces.Count < necesarias)
        {
            luces.Add(CrearLuz(luces.Count));
        }

        for (int i = 0; i < luces.Count; i++)
        {
            if (i < necesarias)
            {
                luces[i].transform.position = posiciones[i];
                luces[i].gameObject.SetActive(true);
            }
            else
            {
                luces[i].gameObject.SetActive(false);
            }
        }
    }

    private Light CrearLuz(int numero)
    {
        GameObject objeto = new GameObject("Luz del rayo " + numero);
        objeto.transform.SetParent(transform);

        Light luz = objeto.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = colorRayo;
        luz.intensity = intensidadLuces;
        luz.range = alcanceLuces;
        // sin sombras: así varias luces apenas cuestan rendimiento
        luz.shadows = LightShadows.None;
        return luz;
    }
}
