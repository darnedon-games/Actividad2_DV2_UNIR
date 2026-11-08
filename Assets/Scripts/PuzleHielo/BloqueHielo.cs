using UnityEngine;

// El bloque que el jugador empuja en el puzle de hielo.
// Al interactuar con el, se desliza en direccion contraria al jugador
// y no para hasta chocar con una roca, una pared o el borde de la zona.
//
// El bloque no decide hasta donde llega: se lo pregunta al generador,
// que es quien conoce la cuadricula. El bloque solo se mueve hasta alli.
public class BloqueHielo : Interactuable
{
    public GeneradorPuzleHielo generador;
    public float velocidad = 8f;
    public Color colorResaltado = new Color(1f, 0.9f, 0.3f);

    private Renderer render;
    private Color colorNormal;

    private bool moviendose = false;
    private Vector3 destino;

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

    void Update()
    {
        if (moviendose == false)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

        // ¿hemos llegado?
        if (transform.position == destino)
        {
            moviendose = false;
            generador.BloqueHaParado();
        }
    }

    // El jugador nos empuja: nos movemos en la direccion que va DEL jugador AL bloque
    public override void Interactuar(Transform jugador)
    {
        // mientras se desliza no se puede volver a empujar
        if (moviendose)
        {
            return;
        }

        Vector3 direccion = transform.position - jugador.position;
        generador.EmpujarBloque(direccion, jugador);
    }

    // Lo llama el generador cuando ya sabe hasta donde tiene que deslizarse
    public void MoverA(Vector3 posicion)
    {
        destino = posicion;
        moviendose = true;
    }

    // Lo pone directamente en una posicion, sin deslizarse (lo usa el generador al reiniciar)
    public void ColocarEn(Vector3 posicion)
    {
        moviendose = false;
        transform.position = posicion;
    }

    public override void Resaltar(bool activar)
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
