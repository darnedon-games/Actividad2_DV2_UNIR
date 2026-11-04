using UnityEngine;

// El receptor es el objetivo: cuando el rayo de luz le llega, avisa al generador
// y el generador abre la puerta (y lo que haya en "Al Resolver Puzle").
public class Receptor : MonoBehaviour
{
    public GeneradorPuzle generador;
    public Color colorActivado = Color.green;

    private bool activado = false;

    public void Activar()
    {
        if (activado)
        {
            return;
        }
        activado = true;

        Renderer render = GetComponentInChildren<Renderer>();
        if (render != null)
        {
            render.material.color = colorActivado;
        }

        if (generador != null)
        {
            generador.PuzleResuelto();
        }
        else
        {
            Debug.LogWarning("El receptor no sabe a que generador avisar");
        }
    }
}
