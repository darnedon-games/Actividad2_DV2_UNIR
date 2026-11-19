using UnityEngine;

// Crea un cubo como GameObject.CreatePrimitive con un material que funciona en una BUILD

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
