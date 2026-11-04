using UnityEngine;

// Un espejo que refleja el rayo de luz.
// Cada vez que el jugador interactua con el, gira 45 grados.
// Cuando el jugador lo tiene delante, se resalta con otro color.
public class Espejo : MonoBehaviour
{
    [SerializeField] private float gradosPorGiro = 45f;
    [SerializeField] private Color colorResaltado = new Color(1f, 0.9f, 0.3f);

    private Renderer render;
    private Color colorNormal;

    void Start()
    {
        render = GetComponent<Renderer>();
        colorNormal = render.material.color;
    }

    public void Rotar()
    {
        transform.Rotate(0, gradosPorGiro, 0);
    }

    public void Resaltar(bool activar)
    {
        if (render == null)
        {
            return;
        }

        if (activar)
        {
            render.material.color = colorResaltado;
        }
        else
        {
            render.material.color = colorNormal;
        }
    }
}
