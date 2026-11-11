using UnityEngine;

// Una baldosa del puzle de baldosas.
// Puede estar apagada o encendida, y cada vez que el jugador la pisa cambia.
// No sabe nada del jugador: el generador le dice cuándo cambiar.
public class Baldosa : MonoBehaviour
{
    public Color colorApagada = new Color(0.25f, 0.3f, 0.4f);
    public Color colorEncendida = new Color(1f, 0.85f, 0.2f);

    private Renderer render;
    private bool encendida = false;

    // Awake se ejecuta justo al añadir el script
    void Awake()
    {
        // InChildren por si es un prefab con el modelo en un objeto hijo
        render = GetComponentInChildren<Renderer>();
    }

    public bool EstaEncendida()
    {
        return encendida;
    }

    // Si estaba apagada se enciende y si estaba encendida se apaga
    public void Alternar()
    {
        encendida = !encendida;
        ActualizarColor();
    }

    public void Apagar()
    {
        encendida = false;
        ActualizarColor();
    }

    private void ActualizarColor()
    {
        if (render == null)
        {
            return;
        }

        if (encendida)
        {
            render.material.color = colorEncendida;
        }
        else
        {
            render.material.color = colorApagada;
        }
    }
}
