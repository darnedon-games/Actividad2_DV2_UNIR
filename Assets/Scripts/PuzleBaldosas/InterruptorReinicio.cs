using UnityEngine;
using System.Collections.Generic;

// El interruptor del puzle de baldosas: al usarlo, todas las baldosas se apagan y el jugador puede volver a empezar.
// Lo crea el generador junto a la entrada de la sala.

public class InterruptorReinicio : Interactuable
{
    public GeneradorPuzleBaldosas generador;

    // color con el que BRILLA el interruptor cuando el jugador lo tiene delante
    public Color colorResaltado = new Color(1f, 0.9f, 0.3f);
    // cuánto brilla al resaltarlo: 0 = nada, 1 = mucho
    public float brilloResaltado = 0.25f;

    // Todos los materiales del interruptor (un prefab puede tener varias piezas y materiales)
    // y cómo era el brillo de cada uno al empezar, para dejarlos igual al quitar el resaltado.
    // La posición en las listas indica de qué material es cada dato.
    private List<Material> materiales = new List<Material>();
    private List<bool> teniaEmision = new List<bool>();
    private List<Color> emisionOriginal = new List<Color>();
    private List<Texture> mapaEmisionOriginal = new List<Texture>();

    // Awake se ejecuta justo al añadir el script (el generador ya ha creado el modelo)
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

    public override void Interactuar(Transform jugador)
    {
        generador.Reiniciar();
    }

    public override void Resaltar(bool activar)
    {
        for (int i = 0; i < materiales.Count; i++)
        {
            if (activar)
            {
                // si el material no tenía brillo propio, brilla con su misma textura:
                // así se sigue viendo el dibujo del material y no queda un bloque de color liso
                if (mapaEmisionOriginal[i] == null)
                {
                    materiales[i].SetTexture("_EmissionMap", materiales[i].mainTexture);
                }
                materiales[i].EnableKeyword("_EMISSION");
                materiales[i].SetColor("_EmissionColor", colorResaltado * brilloResaltado);
            }
            else
            {
                // lo dejamos como estaba al empezar
                materiales[i].SetColor("_EmissionColor", emisionOriginal[i]);
                materiales[i].SetTexture("_EmissionMap", mapaEmisionOriginal[i]);
                if (teniaEmision[i] == false)
                {
                    materiales[i].DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
