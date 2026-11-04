using UnityEngine;

// Un espejo que refleja el rayo de luz.
// Cada vez que el jugador interactua con el, gira 45 grados.
// Si tiene un espejo enlazado, ese tambien gira (pero girar el enlazado no gira este).
// Cuando el jugador lo tiene delante se resalta, y su enlazado se resalta con otro color.
public class Espejo : MonoBehaviour
{
    [SerializeField] private float gradosPorGiro = 45f;
    [SerializeField] private Color colorResaltado = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private Color colorEnlazado = new Color(1f, 0.5f, 0.1f);

    // espejo que gira tambien cuando giramos este (puede estar vacio)
    public Espejo espejoEnlazado;

    private Renderer render;
    private Color colorNormal;

    // Awake se ejecuta justo al añadir el script, asi guardamos el color que le puso el generador
    void Awake()
    {
        render = GetComponent<Renderer>();
        colorNormal = render.material.color;
    }

    // Lo que pasa cuando el jugador interactua
    public void Rotar()
    {
        GirarSolo();

        if (espejoEnlazado != null)
        {
            espejoEnlazado.GirarSolo();
        }
    }

    // Gira solo este espejo, sin tocar el enlazado
    public void GirarSolo()
    {
        transform.Rotate(0, gradosPorGiro, 0);
    }

    public void Resaltar(bool activar)
    {
        if (activar)
        {
            PonerColor(colorResaltado);
            if (espejoEnlazado != null)
            {
                espejoEnlazado.PonerColor(colorEnlazado);
            }
        }
        else
        {
            QuitarColor();
            if (espejoEnlazado != null)
            {
                espejoEnlazado.QuitarColor();
            }
        }
    }

    public void PonerColor(Color color)
    {
        render.material.color = color;
    }

    public void QuitarColor()
    {
        render.material.color = colorNormal;
    }
}
