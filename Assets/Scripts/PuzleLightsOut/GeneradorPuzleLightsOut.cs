using UnityEngine;
using UnityEngine.Events;

// Genera el puzle Lights Out al cargar la escena.
//
// EL PUZLE:
// Hay X por Y lámparas sobre pilares. Al pulsar una, cambia ella y sus 4 vecinas
// (arriba, abajo, izquierda y derecha): las encendidas se apagan y las apagadas se encienden.
// Hay que dejarlas TODAS encendidas. Entonces se abre la puerta.
// Al tener una lámpara delante, su pilar se resalta, y los de las vecinas que cambiarán
// se resaltan con otro color.
//
// CÓMO FUNCIONA:
// 1. Empezamos con todas encendidas (el puzle resuelto).
// 2. Lo desordenamos pulsando lámparas al azar. Como lo hemos desordenado con pulsaciones,
//    deshaciendo esas mismas pulsaciones se resuelve: SIEMPRE tiene solución.
// 3. Calculamos la solución más corta "persiguiendo las luces" (mira CalcularSolucion).
//    Si se resuelve con menos de "Pulsaciones Mínimas", desordenamos otra vez.
// (En la vista Scene, durante el Play, se marcan en cian las lámparas que hay que pulsar.)
//
// TRUCOS DE LIGHTS OUT QUE USAMOS:
// - Pulsar dos veces la misma lámpara es como no pulsarla.
// - El orden de las pulsaciones da igual.
// Así que una solución es simplemente "qué lámparas pulsar", cada una como mucho una vez.
//

public class GeneradorPuzleLightsOut : MonoBehaviour
{
    [Header("Lámparas")]
    [SerializeField] private int lamparasX = 5;
    [SerializeField] private int lamparasY = 5;
    // distancia entre el centro de una lámpara y el de la siguiente
    [SerializeField] private float separacion = 2.5f;

    [Header("Dificultad")]
    // la solución más corta nunca tendrá menos pulsaciones que estas
    [SerializeField] private int pulsacionesMinimas = 6;
    // intentos con cada número de pulsaciones mínimas antes de probar con uno menos
    [SerializeField] private int intentosMaximos = 1000;

    [Header("Colores")]
    [SerializeField] private Color colorApagada = new Color(0.15f, 0.15f, 0.2f);
    [SerializeField] private Color colorEncendida = new Color(1f, 0.85f, 0.2f);
    // color normal del pilar (solo sin prefab: con prefab se queda el color de su material)
    [SerializeField] private Color colorPilar = new Color(0.5f, 0.5f, 0.55f);
    // colores con los que BRILLA el pilar al resaltarlo
    [SerializeField] private Color colorResaltado = new Color(0.3f, 0.8f, 1f);
    [SerializeField] private Color colorVecinas = new Color(0.2f, 0.45f, 0.6f);
    // cuánto brilla el pilar resaltado: 0 = nada, 1 = mucho
    [SerializeField] private float brilloResaltado = 0.25f;

    [Header("Prefabs (opcionales)")]
    // si está vacío se crea un pilar con un cubo encima. Pivote en el suelo.
    // Si el prefab tiene una luz (Light) dentro, aunque esté desactivada, el puzle
    // la enciende y la apaga en lugar de cambiar colores.
    [SerializeField] private GameObject prefabLampara;

    [Header("Al resolver")]
    [SerializeField] private UnityEvent alResolver;

    private int columnas;
    private int filas;
    private bool[,] encendida;
    private Lampara[,] lamparas;
    private bool resuelto = false;

    // la solución más corta desde como están ahora las luces (para verla en la escena)
    private bool[,] solucion;
    private int pulsacionesSolucion;

    // la lámpara del centro y sus 4 vecinas: lo que cambia al pulsar
    private Vector2Int[] cruz = { Vector2Int.zero, Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    void Start()
    {
        columnas = lamparasX;
        filas = lamparasY;
        if (columnas < 2 || filas < 2)
        {
            Debug.LogError("El puzle Lights Out necesita al menos 2x2 lámparas");
            PuzleResuelto(); // para que el jugador no se quede encerrado
            return;
        }
        if (columnas > 12)
        {
            // CalcularSolucion prueba 2 elevado a "columnas" opciones: con muchas columnas tardaría mucho
            Debug.LogWarning("Puzle Lights Out: con más de 12 columnas puede tardar en generarse. " +
                             "Si quieres un puzle alargado, mejor pon más filas que columnas.");
        }

        // probamos con la dificultad pedida; si no sale, con una pulsación menos, y así hasta 1
        bool puzleCreado = false;
        for (int minimo = pulsacionesMinimas; minimo >= 1 && puzleCreado == false; minimo--)
        {
            for (int i = 0; i < intentosMaximos; i++)
            {
                if (IntentarCrearPuzle(minimo))
                {
                    puzleCreado = true;
                    break;
                }
            }

            if (puzleCreado == false)
            {
                Debug.LogWarning("Puzle Lights Out: no sale con " + minimo + " pulsaciones mínimas, probamos con menos");
            }
        }

        if (puzleCreado == false)
        {
            Debug.LogError("No se ha podido generar el puzle Lights Out. Abrimos la puerta para no bloquear el juego.");
            PuzleResuelto();
            return;
        }

        Debug.Log("Puzle Lights Out: se resuelve con " + pulsacionesSolucion + " pulsaciones como mínimo");
        CrearLamparas();
    }

    // Lo llama una lámpara cuando el jugador interactúa con ella
    public void Pulsar(Vector2Int celda)
    {
        if (resuelto)
        {
            return;
        }

        CambiarCruz(encendida, celda);

        // actualizamos el color de las lámparas que han cambiado
        foreach (Vector2Int desplazamiento in cruz)
        {
            Vector2Int vecina = celda + desplazamiento;
            if (DentroDeLaZona(vecina))
            {
                PintarLuz(vecina);
            }
        }

        if (TodasEncendidas(encendida))
        {
            PuzleResuelto();
        }
        else
        {
            // recalculamos la solución para verla en la escena
            CalcularSolucion(encendida);
        }
    }

    // Lo llama una lámpara cuando el jugador empieza o deja de tenerla delante
    public void ResaltarLampara(Vector2Int celda, bool activar)
    {
        foreach (Vector2Int desplazamiento in cruz)
        {
            Vector2Int vecina = celda + desplazamiento;
            if (DentroDeLaZona(vecina) == false)
            {
                continue;
            }

            if (activar == false)
            {
                QuitarResaltado(vecina);
                continue;
            }

            Color color = colorVecinas;
            if (vecina == celda)
            {
                color = colorResaltado;
            }
            lamparas[vecina.x, vecina.y].IluminarPilar(color * brilloResaltado);
        }
    }

    private void QuitarResaltado(Vector2Int celda)
    {
        // el pilar deja de brillar y vuelve a como era su material
        lamparas[celda.x, celda.y].RestaurarPilar();

        // los cubos que se crean sin prefab, además, se pintan del gris de "Color Pilar"
        if (prefabLampara == null)
        {
            lamparas[celda.x, celda.y].PintarPilar(colorPilar);
        }
    }

    private void PuzleResuelto()
    {
        if (resuelto)
        {
            return;
        }
        resuelto = true;
        solucion = null;
        Debug.Log("Puzle Lights Out resuelto!");

        // avisamos a todo lo que se haya conectado en el Inspector (abrir puerta, sonido...)
        alResolver.Invoke();
    }

    // ---------------- REGLAS ----------------
    // Estas funciones solo trabajan con números (true = encendida, false = apagada).

    // Cambia la lámpara de "celda" y sus 4 vecinas en "luces"
    private void CambiarCruz(bool[,] luces, Vector2Int celda)
    {
        foreach (Vector2Int desplazamiento in cruz)
        {
            Vector2Int vecina = celda + desplazamiento;
            if (DentroDeLaZona(vecina))
            {
                luces[vecina.x, vecina.y] = !luces[vecina.x, vecina.y];
            }
        }
    }

    private bool TodasEncendidas(bool[,] luces)
    {
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (luces[col, fila] == false)
                {
                    return false;
                }
            }
        }
        return true;
    }

    // Busca la solución más corta y la guarda en "solucion" y "pulsacionesSolucion".
    //
    // "PERSEGUIR LAS LUCES":
    // Imagina que ya hemos decidido qué lámparas pulsar en la primera fila.
    // Entonces, si en la primera fila queda una lámpara apagada, la ÚNICA forma de
    // encenderla sin estropear lo demás es pulsar la que tiene justo debajo.
    // Así que la segunda fila ya no se decide: viene obligada. Y lo mismo con la tercera...
    // Al llegar al final, si la última fila ha quedado encendida, ¡es una solución!
    //
    // Por eso basta con probar todas las formas de pulsar la primera fila
    // (con 5 columnas son 2x2x2x2x2 = 32) y quedarnos con la que menos pulsaciones necesita.
    private void CalcularSolucion(bool[,] luces)
    {
        solucion = null;
        pulsacionesSolucion = -1;

        int opcionesPrimeraFila = 1 << columnas; // 2 elevado a "columnas"

        for (int opcion = 0; opcion < opcionesPrimeraFila; opcion++)
        {
            bool[,] copia = (bool[,])luces.Clone();
            bool[,] pulsadas = new bool[columnas, filas];
            int pulsaciones = 0;

            // primera fila: "opcion" es un número cuyos bits dicen qué lámparas pulsar
            // (bit 0 = columna 0, bit 1 = columna 1...). Así salen todas las combinaciones.
            for (int col = 0; col < columnas; col++)
            {
                bool pulsar = ((opcion >> col) & 1) == 1;
                if (pulsar)
                {
                    CambiarCruz(copia, new Vector2Int(col, 0));
                    pulsadas[col, 0] = true;
                    pulsaciones++;
                }
            }

            // el resto de filas: pulsamos debajo de cada lámpara que siga apagada
            for (int fila = 1; fila < filas; fila++)
            {
                for (int col = 0; col < columnas; col++)
                {
                    if (copia[col, fila - 1] == false)
                    {
                        CambiarCruz(copia, new Vector2Int(col, fila));
                        pulsadas[col, fila] = true;
                        pulsaciones++;
                    }
                }
            }

            // ¿ha quedado todo encendido? ¿es la más corta hasta ahora?
            if (TodasEncendidas(copia) && (solucion == null || pulsaciones < pulsacionesSolucion))
            {
                solucion = pulsadas;
                pulsacionesSolucion = pulsaciones;
            }
        }
    }

    private bool IntentarCrearPuzle(int minimo)
    {
        // empezamos resuelto: todas encendidas
        encendida = new bool[columnas, filas];
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                encendida[col, fila] = true;
            }
        }

        // desordenamos pulsando lámparas al azar (algunas más que el mínimo,
        // porque a veces unas pulsaciones se anulan con otras)
        int pulsacionesAlAzar = minimo + Random.Range(0, 4);
        for (int i = 0; i < pulsacionesAlAzar; i++)
        {
            Vector2Int celda = new Vector2Int(Random.Range(0, columnas), Random.Range(0, filas));
            CambiarCruz(encendida, celda);
        }

        // ¿es lo bastante difícil?
        CalcularSolucion(encendida);
        return solucion != null && pulsacionesSolucion >= minimo;
    }

    private bool DentroDeLaZona(Vector2Int celda)
    {
        return celda.x >= 0 && celda.x < columnas && celda.y >= 0 && celda.y < filas;
    }

    // Posición de una lámpara en el mundo, a la altura del suelo
    private Vector3 PosicionCelda(Vector2Int celda)
    {
        float x = (celda.x - (columnas - 1) / 2f) * separacion;
        float z = (celda.y - (filas - 1) / 2f) * separacion;
        return transform.position + transform.right * x + transform.forward * z;
    }

    private void CrearLamparas()
    {
        lamparas = new Lampara[columnas, filas];

        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                Vector2Int celda = new Vector2Int(col, fila);
                lamparas[col, fila] = CrearLampara(celda);
                QuitarResaltado(celda);
                PintarLuz(celda);
            }
        }
    }

    private Lampara CrearLampara(Vector2Int celda)
    {
        string nombre = "Lampara " + celda.x + "," + celda.y;
        Vector3 posicion = PosicionCelda(celda);
        GameObject objeto;
        Renderer luz = null;
        Renderer pilar = null;

        if (prefabLampara != null)
        {
            objeto = Instantiate(prefabLampara, posicion, transform.rotation, transform);
            objeto.name = nombre;
        }
        else
        {
            // un objeto vacío con dos cubos dentro: el pilar y la luz de encima
            objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform);
            objeto.transform.position = posicion;
            objeto.transform.rotation = transform.rotation;

            pilar = CrearCubo("Pilar", objeto.transform, 0.5f, new Vector3(0.6f, 1f, 0.6f));
            luz = CrearCubo("Luz", objeto.transform, 1.1f, new Vector3(0.5f, 0.2f, 0.5f));
        }

        Lampara lampara = objeto.GetComponent<Lampara>();
        if (lampara == null)
        {
            lampara = objeto.AddComponent<Lampara>();
        }
        if (luz != null)
        {
            lampara.luz = luz;
            lampara.pilar = pilar;
        }
        lampara.generador = this;
        lampara.celda = celda;
        lampara.GuardarAspectoPilar();
        return lampara;
    }

    // Crea un cubo dentro de "padre", con su centro a la altura indicada
    private Renderer CrearCubo(string nombre, Transform padre, float alturaCentro, Vector3 escala)
    {
        GameObject cubo = CuboBasico.Crear();
        cubo.name = nombre;
        cubo.transform.SetParent(padre);
        cubo.transform.position = padre.position + transform.up * alturaCentro;
        cubo.transform.rotation = padre.rotation;
        cubo.transform.localScale = escala;
        return cubo.GetComponent<Renderer>();
    }

    private void PintarLuz(Vector2Int celda)
    {
        if (lamparas == null)
        {
            return;
        }

        Lampara lampara = lamparas[celda.x, celda.y];

        // si el prefab trae una luz de verdad (Light), la encendemos o apagamos
        if (lampara.TieneBombilla())
        {
            lampara.EncenderBombilla(encendida[celda.x, celda.y]);
            return;
        }

        // si no, cambiamos el color, como con los cubos que se crean sin prefab
        if (encendida[celda.x, celda.y])
        {
            lampara.PintarLuz(colorEncendida);
        }
        else
        {
            lampara.PintarLuz(colorApagada);
        }
    }

    // ---------------- AYUDA VISUAL EN EL EDITOR ----------------

    // Dibuja la zona y dónde irá cada lámpara.
    // Durante el Play marca en cian las lámparas que hay que pulsar para resolverlo.
    private void OnDrawGizmos()
    {
        if (separacion <= 0 || lamparasX < 1 || lamparasY < 1)
        {
            return;
        }
        columnas = lamparasX;
        filas = lamparasY;

        // la solución, en coordenadas del mundo
        if (Application.isPlaying && solucion != null)
        {
            Gizmos.color = Color.cyan;
            for (int col = 0; col < columnas; col++)
            {
                for (int fila = 0; fila < filas; fila++)
                {
                    if (solucion[col, fila])
                    {
                        Gizmos.DrawSphere(PosicionCelda(new Vector2Int(col, fila)) + transform.up * 1.6f, 0.25f);
                    }
                }
            }
        }

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Gizmos.color = Color.cyan;
        Vector3 tamanoTotal = new Vector3(columnas * separacion, 1.2f, filas * separacion);
        Gizmos.DrawWireCube(new Vector3(0, 0.6f, 0), tamanoTotal);

        // dónde irá cada lámpara
        Gizmos.color = Color.yellow;
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                float x = (col - (columnas - 1) / 2f) * separacion;
                float z = (fila - (filas - 1) / 2f) * separacion;
                Gizmos.DrawWireCube(new Vector3(x, 0.6f, z), new Vector3(0.6f, 1.2f, 0.6f));
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
