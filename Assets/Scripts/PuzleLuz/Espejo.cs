using UnityEngine;

// Un espejo que refleja el rayo de luz.
// Cada vez que el jugador interactua con el, gira 45 grados.
// Si tiene un espejo enlazado, ese tambien gira (pero girar el enlazado no gira necesariamente este).
// Cuando el jugador lo tiene delante se resalta, y su enlazado se resalta con otro color.
// Hereda de Interactuable para que el jugador pueda usarlo sin saber que es un espejo.
public class Espejo : Interactuable
{
    private float gradosPorGiro = 45f;
    public Color colorResaltado = new Color(1f, 0.9f, 0.3f);
    public Color colorEnlazado = new Color(1f, 0.5f, 0.1f);

    // espejo que gira tambien cuando giramos este (puede estar vacio)
    public Espejo espejoEnlazado;

    private Renderer render;
    private Color colorNormal;

    // Awake se ejecuta justo al añadir el script, asi guardamos el color que le puso el generador
    void Awake()
    {
        // InChildren por si es un prefab con el modelo en un objeto hijo
        render = GetComponentInChildren<Renderer>();
        if (render != null)
        {
            colorNormal = render.material.color;
        }
    }

    // Lo que pasa cuando el jugador interactua: giramos el espejo
    // (no necesitamos saber donde esta el jugador, asi que no usamos "jugador")
    public override void Interactuar(Transform jugador)
    {
        Rotar();
    }

    // Gira este espejo y tambien su enlazado
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

    public override void Resaltar(bool activar)
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
        if (render == null)
        {
            return;
        }
        render.material.color = color;
    }

    public void QuitarColor()
    {
        if (render == null)
        {
            return;
        }
        render.material.color = colorNormal;
    }
}
