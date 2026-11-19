using UnityEngine;
using System.Collections.Generic;

// Un espejo que refleja el rayo de luz.
// Cada vez que el jugador interactua con el, gira 45 grados.
// Si tiene un espejo enlazado, ese tambien gira (pero girar el enlazado no gira necesariamente este).
// Cuando el jugador lo tiene delante se resalta (brilla), y su enlazado brilla con otro color.
// Hereda de Interactuable para que el jugador pueda usarlo sin saber que es un espejo.
public class Espejo : Interactuable
{
    private float gradosPorGiro = 45f;
    // colores con los que BRILLA el espejo al resaltarlo
    public Color colorResaltado = new Color(1f, 0.9f, 0.3f);
    public Color colorEnlazado = new Color(1f, 0.5f, 0.1f);
    // cuánto brilla al resaltarlo: 0 = nada, 1 = mucho
    public float brilloResaltado = 0.25f;

    // espejo que gira tambien cuando giramos este (puede estar vacio)
    public Espejo espejoEnlazado;

    // Todos los materiales del espejo (un prefab puede tener varias piezas y materiales)
    // y cómo era el brillo de cada uno al empezar, para dejarlos igual al quitar el resaltado.
    // La posición en las listas indica de qué material es cada dato.
    private List<Material> materiales = new List<Material>();
    private List<bool> teniaEmision = new List<bool>();
    private List<Color> emisionOriginal = new List<Color>();
    private List<Texture> mapaEmisionOriginal = new List<Texture>();

    void Awake()
    {
        // InChildren por si es un prefab con el modelo en objetos hijos
        foreach (Renderer render in GetComponentsInChildren<Renderer>())
        {
            foreach (Material material in render.materials)
            {
                materiales.Add(material);
                teniaEmision.Add(material.IsKeywordEnabled("_EMISSION"));

                Color emision = Color.black;
                if (material.HasProperty("_EmissionColor"))
                {
                    emision = material.GetColor("_EmissionColor");
                }
                emisionOriginal.Add(emision);

                Texture mapa = null;
                if (material.HasProperty("_EmissionMap"))
                {
                    mapa = material.GetTexture("_EmissionMap");
                }
                mapaEmisionOriginal.Add(mapa);
            }
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

    // Hace que el espejo BRILLE con ese color (emisión). Se ve aunque tenga
    // textura o esté a oscuras, a diferencia de cambiar su color.
    public void PonerColor(Color color)
    {
        for (int i = 0; i < materiales.Count; i++)
        {
            // si el material no tenía brillo propio, brilla con su misma textura:
            // así se sigue viendo el dibujo del material y no queda un bloque de color liso
            if (mapaEmisionOriginal[i] == null)
            {
                materiales[i].SetTexture("_EmissionMap", materiales[i].mainTexture);
            }
            materiales[i].EnableKeyword("_EMISSION");
            materiales[i].SetColor("_EmissionColor", color * brilloResaltado);
        }
    }

    // Deja todos los materiales con el brillo que tenían al empezar
    public void QuitarColor()
    {
        for (int i = 0; i < materiales.Count; i++)
        {
            materiales[i].SetColor("_EmissionColor", emisionOriginal[i]);
            materiales[i].SetTexture("_EmissionMap", mapaEmisionOriginal[i]);
            if (teniaEmision[i] == false)
            {
                materiales[i].DisableKeyword("_EMISSION");
            }
        }
    }
}
