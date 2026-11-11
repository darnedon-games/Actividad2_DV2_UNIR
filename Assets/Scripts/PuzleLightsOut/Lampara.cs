using UnityEngine;

// Una lámpara del puzle Lights Out: un pilar con una luz encima.
// Cuando el jugador interactúa con ella, avisa al generador, que es quien
// cambia esta lámpara y sus vecinas (la lámpara no sabe nada de las reglas).
public class Lampara : Interactuable
{
    public GeneradorPuzleLightsOut generador;
    public Vector2Int celda;

    // la parte que se enciende y el pilar que se resalta.
    // Si usas un prefab, arrástralas en el prefab. Si están vacías se usa el primer Renderer que haya.
    public Renderer luz;
    public Renderer pilar;

    void Awake()
    {
        if (luz == null)
        {
            luz = GetComponentInChildren<Renderer>();
        }
        if (pilar == null)
        {
            pilar = luz;
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

    public void PintarPilar(Color color)
    {
        if (pilar != null)
        {
            pilar.material.color = color;
        }
    }
}
