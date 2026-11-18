using UnityEngine;

// Una lámpara del puzle Lights Out: un pilar con una luz encima.
// Cuando el jugador interactúa con ella, avisa al generador, que es quien
// cambia esta lámpara y sus vecinas (la lámpara no sabe nada de las reglas).
public class Lampara : Interactuable
{
    public GeneradorPuzleLightsOut generador;
    public Vector2Int celda;

    // la parte que se enciende y el pilar que se resalta.
    // Si usas un prefab, añádele este script y arrástralas en el prefab.
    // Si están vacías se usa el primer Renderer que haya.
    public Renderer luz;
    public Renderer pilar;

    // una luz de verdad (Light) dentro del prefab, por ejemplo un Point Light.
    // Si la hay, se enciende y se apaga en lugar de cambiar el color de "luz".
    public Light bombilla;

    // cómo era el material del pilar al empezar, para dejarlo igual al quitar el resaltado
    private Color colorOriginalPilar = Color.white;
    private Color emisionOriginalPilar = Color.black;
    private bool teniaEmision = false;
    private Texture mapaEmisionOriginal = null;

    void Awake()
    {
        if (bombilla == null)
        {
            // el "true" hace que también la encuentre aunque esté desactivada
            bombilla = GetComponentInChildren<Light>(true);
        }

        if (luz == null)
        {
            luz = GetComponentInChildren<Renderer>();
        }
        if (pilar == null)
        {
            pilar = luz;
        }
    }

    // Apunta cómo es el material del pilar ahora mismo. El generador la llama
    // al crear la lámpara, cuando ya sabe cuál es el pilar.
    public void GuardarAspectoPilar()
    {
        if (pilar == null)
        {
            return;
        }
        colorOriginalPilar = pilar.material.color;
        teniaEmision = pilar.material.IsKeywordEnabled("_EMISSION");
        if (pilar.material.HasProperty("_EmissionColor"))
        {
            emisionOriginalPilar = pilar.material.GetColor("_EmissionColor");
        }
        if (pilar.material.HasProperty("_EmissionMap"))
        {
            mapaEmisionOriginal = pilar.material.GetTexture("_EmissionMap");
        }
    }

    public override void Interactuar(Transform jugador)
    {
        generador.Pulsar(celda);
    }

    // Al tenerla delante, el generador resalta esta lámpara y las vecinas que cambiarán
    public override void Resaltar(bool activar)
    {
        generador.ResaltarLampara(celda, activar);
    }

    public void PintarLuz(Color color)
    {
        if (luz != null)
        {
            luz.material.color = color;
        }
    }

    public bool TieneBombilla()
    {
        return bombilla != null;
    }

    public void EncenderBombilla(bool encender)
    {
        if (bombilla != null)
        {
            bombilla.gameObject.SetActive(encender);
        }
    }

    public void PintarPilar(Color color)
    {
        if (pilar != null)
        {
            pilar.material.color = color;
        }
    }

    // Hace que el pilar BRILLE con ese color (emisión). Se ve aunque el pilar
    // tenga textura o esté a oscuras, a diferencia de cambiar su color.
    public void IluminarPilar(Color color)
    {
        if (pilar == null)
        {
            return;
        }
        // si el material no tenía brillo propio, brilla con su misma textura:
        // así se sigue viendo el dibujo del material y no queda un bloque de color liso
        if (mapaEmisionOriginal == null)
        {
            pilar.material.SetTexture("_EmissionMap", pilar.material.mainTexture);
        }
        pilar.material.EnableKeyword("_EMISSION");
        pilar.material.SetColor("_EmissionColor", color);
    }

    // Deja el pilar como estaba al empezar: su color y su brillo
    public void RestaurarPilar()
    {
        if (pilar == null)
        {
            return;
        }
        pilar.material.color = colorOriginalPilar;
        pilar.material.SetColor("_EmissionColor", emisionOriginalPilar);
        pilar.material.SetTexture("_EmissionMap", mapaEmisionOriginal);
        if (teniaEmision == false)
        {
            pilar.material.DisableKeyword("_EMISSION");
        }
    }
}
