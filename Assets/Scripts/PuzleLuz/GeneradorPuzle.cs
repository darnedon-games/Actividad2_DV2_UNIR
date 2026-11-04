using System.Collections.Generic;
using UnityEngine;

// Genera el puzle de luz al cargar la escena.
//
// COMO FUNCIONA:
// 1. Dividimos la zona del puzle (el recuadro cian que se ve en la escena) en celdas.
//    Cada celda es un posible punto de spawn.
// 2. Miramos cada celda con Physics.CheckBox. Si hay algo dentro (una pared, una caja,
//    el jugador...) la celda queda BLOQUEADA y no se usa (se ve en rojo en la escena).
// 3. Ponemos el emisor en una celda libre de un borde, mirando hacia dentro.
//    Creamos un camino al azar por las celdas libres: el rayo va recto y a veces gira.
//    En cada giro ponemos un espejo bien orientado.
//    El camino termina en el borde contrario (o en otro borde si no es posible),
//    y ahi ponemos el receptor.
//    El camino nunca pasa dos veces por la misma celda ni por celdas bloqueadas,
//    asi que el puzle SIEMPRE tiene solucion.
// 4. Giramos los espejos al azar para que el jugador tenga que colocarlos.
// 5. Ponemos espejos falsos en celdas libres por donde no pasa el camino.
//
// PARA USARLO EN OTRO PROYECTO:
// - Copia la carpeta PuzleLuz.
// - Crea un objeto vacio, ponlo SOBRE EL SUELO de la sala y añadele este script.
// - Ajusta "Tamaño Zona" hasta que el recuadro cian quepa dentro de la sala.
// - En tu jugador, al pulsar interactuar, llama a Rotar() del Espejo mas cercano
//   (mira Player.cs de este proyecto como ejemplo).
public class GeneradorPuzle : MonoBehaviour
{
    [Header("Zona del puzle (recuadro cian en la escena)")]
    [Tooltip("Ancho (X) y largo (Z) de la zona en metros. El objeto debe estar sobre el suelo y con escala 1.")]
    [SerializeField] private Vector2 tamanoZona = new Vector2(14f, 12f);
    [Tooltip("Tamaño de cada celda. Mejor que sea mas grande que el jugador para que pueda pasar entre espejos.")]
    [SerializeField] private float tamanoCelda = 2f;
    [Tooltip("Altura del rayo de luz sobre el suelo.")]
    [SerializeField] private float alturaRayo = 1f;

    [Header("Deteccion de obstaculos")]
    [Tooltip("Que capas cuentan como obstaculo. Por defecto todas.")]
    [SerializeField] private LayerMask capasObstaculos = ~0;
    [Tooltip("Altura de la caja con la que se comprueba si una celda esta libre.")]
    [SerializeField] private float alturaComprobacion = 2f;

    [Header("Dificultad")]
    [SerializeField] private int espejosMinimos = 4;
    [SerializeField] private int espejosMaximos = 8;
    [Range(0f, 1f)]
    [SerializeField] private float probabilidadGiro = 0.35f;
    [SerializeField] private int espejosFalsos = 3;
    [SerializeField] private int intentosMaximos = 500;

    [Header("Que pasa al resolverlo")]
    [SerializeField] private Puerta puerta;

    private int columnas;
    private int filas;
    private bool[,] celdaBloqueada;
    private bool[,] celdaUsada;

    // datos del camino generado
    private Vector2Int celdaEmisor;
    private Vector2Int direccionEmisor;
    private int ladoEmisor;
    private Vector2Int celdaReceptor;
    private List<Vector2Int> celdasEspejos = new List<Vector2Int>();
    private List<Vector3> normalesEspejos = new List<Vector3>();

    // los 4 lados de la zona: 0 = izquierda, 1 = derecha, 2 = abajo, 3 = arriba
    private const int IZQUIERDA = 0;
    private const int DERECHA = 1;
    private const int ABAJO = 2;
    private const int ARRIBA = 3;

    private bool resuelto = false;

    void Start()
    {
        CalcularCuadricula();
        if (columnas < 2 || filas < 2)
        {
            Debug.LogError("La zona del puzle es demasiado pequeña para el tamaño de celda");
            return;
        }

        BuscarCeldasBloqueadas();

        bool caminoCreado = false;
        for (int i = 0; i < intentosMaximos; i++)
        {
            // la primera mitad de intentos exigimos que el receptor quede en el lado opuesto.
            // si no sale, en la segunda mitad vale cualquier otro borde.
            bool soloLadoOpuesto = i < intentosMaximos / 2;

            if (IntentarCrearCamino(soloLadoOpuesto))
            {
                caminoCreado = true;
                break;
            }
        }

        if (caminoCreado == false)
        {
            Debug.LogError("No se ha podido generar el puzle. Prueba con una zona mas grande, " +
                           "celdas mas pequeñas, menos obstaculos o menos espejos minimos.");
            return;
        }

        CrearPiezas();
    }

    // Lo llama el receptor cuando le llega la luz
    public void PuzleResuelto()
    {
        if (resuelto)
        {
            return;
        }
        resuelto = true;

        if (puerta != null)
        {
            puerta.Abrir();
        }
    }

    // ---------------- CUADRICULA ----------------

    private void CalcularCuadricula()
    {
        columnas = Mathf.FloorToInt(tamanoZona.x / tamanoCelda);
        filas = Mathf.FloorToInt(tamanoZona.y / tamanoCelda);
    }

    // Posicion de una celda en el mundo, a la altura del suelo
    private Vector3 PosicionCelda(int col, int fila)
    {
        float x = (col - (columnas - 1) / 2f) * tamanoCelda;
        float z = (fila - (filas - 1) / 2f) * tamanoCelda;
        return transform.position + transform.right * x + transform.forward * z;
    }

    // Pasa una direccion de la cuadricula a una direccion del mundo
    private Vector3 DireccionMundo(Vector2Int direccion)
    {
        return transform.right * direccion.x + transform.forward * direccion.y;
    }

    // Comprueba si hay algun objeto dentro de la celda
    private bool CeldaLibre(int col, int fila)
    {
        // empezamos 10 cm por encima del suelo para no detectar el propio suelo
        Vector3 centro = PosicionCelda(col, fila) + transform.up * (0.1f + alturaComprobacion / 2f);
        Vector3 mitadTamano = new Vector3(tamanoCelda / 2f, alturaComprobacion / 2f, tamanoCelda / 2f);
        bool hayAlgo = Physics.CheckBox(centro, mitadTamano, transform.rotation, capasObstaculos, QueryTriggerInteraction.Ignore);
        return hayAlgo == false;
    }

    private void BuscarCeldasBloqueadas()
    {
        celdaBloqueada = new bool[columnas, filas];
        int bloqueadas = 0;

        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (CeldaLibre(col, fila) == false)
                {
                    celdaBloqueada[col, fila] = true;
                    bloqueadas++;
                }
            }
        }

        Debug.Log("Puzle de luz: " + columnas + "x" + filas + " celdas, " + bloqueadas + " bloqueadas");
    }

    // Se puede pasar si esta dentro de la cuadricula, no esta bloqueada y no la hemos usado ya
    private bool PuedoPasar(Vector2Int celda)
    {
        if (celda.x < 0 || celda.x >= columnas || celda.y < 0 || celda.y >= filas)
        {
            return false;
        }
        if (celdaBloqueada[celda.x, celda.y] || celdaUsada[celda.x, celda.y])
        {
            return false;
        }
        return true;
    }

    // ---------------- CAMINO ----------------

    private bool IntentarCrearCamino(bool soloLadoOpuesto)
    {
        celdaUsada = new bool[columnas, filas];
        celdasEspejos.Clear();
        normalesEspejos.Clear();

        // el emisor va en una celda libre de un borde al azar, mirando hacia dentro
        ladoEmisor = Random.Range(0, 4);
        if (ladoEmisor == IZQUIERDA)
        {
            celdaEmisor = new Vector2Int(0, Random.Range(0, filas));
            direccionEmisor = new Vector2Int(1, 0);
        }
        else if (ladoEmisor == DERECHA)
        {
            celdaEmisor = new Vector2Int(columnas - 1, Random.Range(0, filas));
            direccionEmisor = new Vector2Int(-1, 0);
        }
        else if (ladoEmisor == ABAJO)
        {
            celdaEmisor = new Vector2Int(Random.Range(0, columnas), 0);
            direccionEmisor = new Vector2Int(0, 1);
        }
        else
        {
            celdaEmisor = new Vector2Int(Random.Range(0, columnas), filas - 1);
            direccionEmisor = new Vector2Int(0, -1);
        }

        if (PuedoPasar(celdaEmisor) == false)
        {
            return false;
        }

        Vector2Int actual = celdaEmisor;
        Vector2Int direccion = direccionEmisor;
        celdaUsada[actual.x, actual.y] = true;
        int pasosDesdeUltimoGiro = 0;

        for (int paso = 0; paso < columnas * filas; paso++)
        {
            // no giramos en la celda del emisor ni nada mas girar
            if (pasosDesdeUltimoGiro > 0 && celdasEspejos.Count < espejosMaximos)
            {
                bool delanteLibre = PuedoPasar(actual + direccion);

                // giramos si delante hay algo o si toca por probabilidad
                if (delanteLibre == false || Random.value < probabilidadGiro)
                {
                    Vector2Int nuevaDireccion = ElegirGiro(actual, direccion);
                    if (nuevaDireccion != Vector2Int.zero)
                    {
                        // la normal del espejo es la mitad entre la direccion de salida y la de entrada
                        Vector3 entrada = DireccionMundo(direccion);
                        Vector3 salida = DireccionMundo(nuevaDireccion);
                        celdasEspejos.Add(actual);
                        normalesEspejos.Add((salida - entrada).normalized);

                        direccion = nuevaDireccion;
                        pasosDesdeUltimoGiro = 0;
                    }
                }
            }

            if (PuedoPasar(actual + direccion) == false)
            {
                break;
            }

            actual = actual + direccion;
            celdaUsada[actual.x, actual.y] = true;
            pasosDesdeUltimoGiro++;

            // si ya hay espejos suficientes y hemos llegado a un borde valido, terminamos aqui
            if (celdasEspejos.Count >= espejosMinimos && BordeValidoParaReceptor(actual, soloLadoOpuesto))
            {
                celdaReceptor = actual;
                return true;
            }
        }

        // no hemos llegado a un borde valido, este intento no sirve
        return false;
    }

    private bool EstaEnLado(Vector2Int celda, int lado)
    {
        if (lado == IZQUIERDA) return celda.x == 0;
        if (lado == DERECHA) return celda.x == columnas - 1;
        if (lado == ABAJO) return celda.y == 0;
        return celda.y == filas - 1;
    }

    private int LadoOpuesto(int lado)
    {
        if (lado == IZQUIERDA) return DERECHA;
        if (lado == DERECHA) return IZQUIERDA;
        if (lado == ABAJO) return ARRIBA;
        return ABAJO;
    }

    private bool BordeValidoParaReceptor(Vector2Int celda, bool soloLadoOpuesto)
    {
        if (soloLadoOpuesto)
        {
            return EstaEnLado(celda, LadoOpuesto(ladoEmisor));
        }

        // cualquier borde menos el del emisor
        if (EstaEnLado(celda, ladoEmisor))
        {
            return false;
        }
        return EstaEnLado(celda, IZQUIERDA) || EstaEnLado(celda, DERECHA) ||
               EstaEnLado(celda, ABAJO) || EstaEnLado(celda, ARRIBA);
    }

    // Devuelve la direccion girando a izquierda o derecha (al azar).
    // Si no se puede girar a ningun lado devuelve (0,0).
    private Vector2Int ElegirGiro(Vector2Int actual, Vector2Int direccion)
    {
        Vector2Int izquierda = new Vector2Int(-direccion.y, direccion.x);
        Vector2Int derecha = new Vector2Int(direccion.y, -direccion.x);

        Vector2Int primera = izquierda;
        Vector2Int segunda = derecha;
        if (Random.value < 0.5f)
        {
            primera = derecha;
            segunda = izquierda;
        }

        if (PuedoPasar(actual + primera))
        {
            return primera;
        }
        if (PuedoPasar(actual + segunda))
        {
            return segunda;
        }
        return Vector2Int.zero;
    }

    // ---------------- CREAR OBJETOS ----------------

    private void CrearPiezas()
    {
        // emisor
        Vector3 posEmisor = PosicionCelda(celdaEmisor.x, celdaEmisor.y);
        Quaternion rotEmisor = Quaternion.LookRotation(DireccionMundo(direccionEmisor), transform.up);
        GameObject emisor = CrearCubo("Emisor de luz", posEmisor, rotEmisor,
            new Vector3(tamanoCelda * 0.3f, alturaRayo * 2f, tamanoCelda * 0.3f), Color.red);
        emisor.AddComponent<EmisorLuz>();

        // receptor
        Vector3 posReceptor = PosicionCelda(celdaReceptor.x, celdaReceptor.y);
        GameObject receptor = CrearCubo("Receptor", posReceptor, transform.rotation,
            new Vector3(tamanoCelda * 0.4f, alturaRayo * 2f, tamanoCelda * 0.4f), Color.yellow);
        Receptor scriptReceptor = receptor.AddComponent<Receptor>();
        scriptReceptor.generador = this;

        // espejos del camino, mal girados a proposito
        for (int i = 0; i < celdasEspejos.Count; i++)
        {
            Vector2Int celda = celdasEspejos[i];
            Quaternion rotacionCorrecta = Quaternion.LookRotation(normalesEspejos[i], transform.up);
            GameObject espejo = CrearEspejo(celda, rotacionCorrecta);

            // 1, 2 o 3 giros de 45 grados (nunca 0, asi nunca empieza resuelto)
            int girosAleatorios = Random.Range(1, 4);
            espejo.transform.Rotate(0, 45f * girosAleatorios, 0);
        }

        // espejos falsos en celdas libres que no use el camino
        int colocados = 0;
        int intentos = 0;
        while (colocados < espejosFalsos && intentos < 200)
        {
            Vector2Int celda = new Vector2Int(Random.Range(0, columnas), Random.Range(0, filas));
            if (PuedoPasar(celda))
            {
                Quaternion rotacion = transform.rotation * Quaternion.Euler(0, 45f * Random.Range(0, 4), 0);
                CrearEspejo(celda, rotacion);
                celdaUsada[celda.x, celda.y] = true;
                colocados++;
            }
            intentos++;
        }
    }

    private GameObject CrearEspejo(Vector2Int celda, Quaternion rotacion)
    {
        Vector3 posicion = PosicionCelda(celda.x, celda.y);
        // la cara del espejo mira hacia su eje Z (flecha azul).
        // Si mas adelante usas un prefab, hazlo aqui con Instantiate y que mire tambien hacia Z.
        GameObject espejo = CrearCubo("Espejo " + celda.x + "," + celda.y, posicion, rotacion,
            new Vector3(tamanoCelda * 0.6f, 1.6f, 0.1f), new Color(0.6f, 0.9f, 1f));
        espejo.AddComponent<Espejo>();
        return espejo;
    }

    // Crea un cubo con su centro a la altura del rayo
    private GameObject CrearCubo(string nombre, Vector3 posicionSuelo, Quaternion rotacion, Vector3 escala, Color color)
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = nombre;
        cubo.transform.SetParent(transform);
        cubo.transform.position = posicionSuelo + transform.up * alturaRayo;
        cubo.transform.rotation = rotacion;
        cubo.transform.localScale = escala;
        cubo.GetComponent<Renderer>().material.color = color;
        return cubo;
    }

    // ---------------- AYUDA VISUAL EN EL EDITOR ----------------

    // Dibuja la zona y las celdas: verde = libre, rojo = ocupada por algo
    private void OnDrawGizmos()
    {
        if (tamanoCelda <= 0)
        {
            return;
        }
        CalcularCuadricula();

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Gizmos.color = Color.cyan;
        Vector3 tamanoTotal = new Vector3(columnas * tamanoCelda, alturaComprobacion, filas * tamanoCelda);
        Gizmos.DrawWireCube(new Vector3(0, alturaComprobacion / 2f, 0), tamanoTotal);

        // las celdas solo con el objeto seleccionado y fuera del Play (en Play ya hay espejos)
        bool seleccionado = false;
#if UNITY_EDITOR
        seleccionado = UnityEditor.Selection.activeGameObject == gameObject;
#endif
        if (seleccionado && Application.isPlaying == false)
        {
            for (int col = 0; col < columnas; col++)
            {
                for (int fila = 0; fila < filas; fila++)
                {
                    float x = (col - (columnas - 1) / 2f) * tamanoCelda;
                    float z = (fila - (filas - 1) / 2f) * tamanoCelda;

                    if (CeldaLibre(col, fila))
                    {
                        Gizmos.color = Color.green;
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                    }
                    Gizmos.DrawWireCube(new Vector3(x, 0.05f, z), new Vector3(tamanoCelda * 0.9f, 0.1f, tamanoCelda * 0.9f));
                }
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
