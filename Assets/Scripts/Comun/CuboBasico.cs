using UnityEngine;

// Crea un cubo como GameObject.CreatePrimitive, pero con un material que funciona
// en el juego compilado.
//
// CreatePrimitive le pone al cubo el material por defecto de Unity. En el editor se ve
// bien, pero en el juego compilado con URP ese material no existe y el cubo sale ROSA.
// Por eso le ponemos un material nuevo con el shader Lit de URP
// (o con el "Standard" si el proyecto no usa URP).
public static class CuboBasico
{
    public static GameObject Crear()
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        if (shader != null)
        {
            cubo.GetComponent<Renderer>().material = new Material(shader);
        }
        return cubo;
    }
}
