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
//    El camino termina en otra celda del borde que no este en la misma fila ni columna
//    que el emisor y que este lejos de el. Ahi ponemos el receptor.
// 4. Enlazamos algunos espejos: al girar uno, tambien gira su enlazado (pero no al reves).
//    Un espejo solo se enlaza con otro que este MAS ADELANTE en el camino del rayo.
// 5. Desordenamos. Primero decidimos cuantas veces tendra que pulsar el jugador
//    cada espejo (de 1 a 3) de forma que sumen al menos "Pulsaciones Minimas".
//    Despues hacemos esas pulsaciones "al reves" desde la solucion.
//    Como los enlaces solo van hacia delante, cada espejo tiene una unica forma de
//    quedar bien, asi que el jugador necesitara EXACTAMENTE esas pulsaciones.
// 6. Por seguridad simulamos el rayo y comprobamos que no haya un atajo por otro
//    camino (con pocos espejos casi nunca pasa y la comprobacion es muy rapida).
//
// Todo esto se hace primero solo con numeros (sin crear objetos).
// Cuando el puzle es bueno, creamos los cubos de verdad.
//
// PARA USARLO EN OTRO PROYECTO:
// - Copia la carpeta PuzleLuz.
// - Crea un objeto vacio, ponlo SOBRE EL SUELO de la sala y añadele este script.
// - Ajusta "Tamaño Zona" hasta que el recuadro cian quepa dentro de la sala.
// - En tu jugador, al pulsar interactuar, llama a Rotar() del Espejo que tenga delante
//   (mira Player.cs de este proyecto como ejemplo).
public class GeneradorPuzle : MonoBehaviour
{
    [Header("Zona del puzle")]
    [Tooltip("Ancho (X) y largo (Z) de la zona en metros. El objeto debe estar sobre el suelo y con escala 1.")]
    [SerializeField] private Vector2 tamanoZona = new Vector2(14f, 12f);
    [Tooltip("Tamaño de cada celda. Mas grande que el jugador para que pueda pasar entre espejos.")]
    [SerializeField] private float tamanoCelda = 2f;
    [Tooltip("Altura del rayo de luz sobre el suelo.")]
    [SerializeField] private float alturaRayo = 1f;

    [Header("Detección de obstáculos")]
    [Tooltip("Qué capas cuentan como obstáculo. Por defecto todas.")]
    [SerializeField] private LayerMask capasObstaculos = ~0;
    [Tooltip("Altura de la caja con la que se comprueba si una celda esta libre.")]
    [SerializeField] private float alturaComprobacion = 2f;

    [Header("Dificultad")]
    [Tooltip("El puzle nunca se podra resolver con menos pulsaciones que estas. Como mucho puede ser 3 x espejos del camino.")]
    [SerializeField] private int pulsacionesMinimas = 6;
    [SerializeField] private int espejosMinimos = 3;
    [SerializeField] private int espejosMaximos = 5;
    [Range(0f, 1f)]
    [SerializeField] private float probabilidadGiro = 0.35f;
    [Tooltip("Probabilidad de que cada espejo tenga otro enlazado que gira con el.")]
    [Range(0f, 1f)]
    [SerializeField] private float probabilidadEnlace = 0.5f;
    [SerializeField] private int intentosMaximos = 500;

    [Header("Prefabs (opcionales: si estan vacios se crean cubos)")]
    [Tooltip("Pivote en el suelo. La cara que refleja mira hacia su eje Z (flecha azul). " +
             "Necesita un collider plano que llegue a la altura del rayo y debe caber en una celda.")]
    [SerializeField] private GameObject prefabEspejo;
    [Tooltip("Pivote en el suelo. Dispara hacia su eje Z (flecha azul). El rayo sale a la altura del rayo.")]
    [SerializeField] private GameObject prefabEmisor;
    [Tooltip("Pivote en el suelo. Necesita un collider que llegue a la altura del rayo.")]
    [SerializeField] private GameObject prefabReceptor;

    [Header("Que pasa al resolverlo")]
    [SerializeField] private Puerta puerta;

    private int columnas;
    private int filas;
    private bool[,] celdaBloqueada;
    private bool[,] celdaUsada;

    // datos del camino generado
    private int ladoEmisor;
    private Vector2Int celdaEmisor;
    private Vector2Int direccionEmisor;
    private Vector2Int celdaReceptor;

    // datos de los espejos, en el orden en que los recorre el rayo
    // el giro de un espejo va de 0 a 3, en pasos de 45 grados:
    //   0 = de frente (como el generador)   1 = 45 grados   2 = 90 grados   3 = 135 grados
    //   (180 grados es igual que 0 porque el espejo refleja por las dos caras)
    private List<Vector2Int> celdasEspejos = new List<Vector2Int>();
    private List<int> girosEspejos = new List<int>();
    private List<int> enlacesEspejos = new List<int>();   // indice del enlazado, o -1 si no tiene
    private int[,] espejoEnCelda;                          // indice del espejo en cada celda, o -1

    // los 4 lados de la zona
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

        bool puzleCreado = false;
        for (int i = 0; i < intentosMaximos; i++)
        {
            // la primera mitad de intentos exigimos que el receptor quede lejos del emisor.
            // si no sale (por ejemplo, por muchos obstáculos), en la segunda mitad no lo exigimos.
            bool exigirDistancia = i < intentosMaximos / 2;

            if (IntentarCrearCamino(exigirDistancia) == false)
            {
                continue;
            }

            PrepararEspejos();

            if (Desordenar())
            {
                puzleCreado = true;
                break;
            }
            // si no se ha conseguido la dificultad pedida, probamos con otro camino
        }

        if (puzleCreado == false)
        {
            Debug.LogError("No se ha podido generar el puzle. Prueba con menos pulsaciones mínimas, " +
                           "más espejos mínimos, una zona más grande o con menos obstáculos.");
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

    private bool DentroDeLaZona(Vector2Int celda)
    {
        return celda.x >= 0 && celda.x < columnas && celda.y >= 0 && celda.y < filas;
    }

    // Se puede pasar si esta dentro de la cuadricula, no esta bloqueada y no la hemos usado ya
    private bool PuedoPasar(Vector2Int celda)
    {
        if (DentroDeLaZona(celda) == false)
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

    private bool IntentarCrearCamino(bool exigirDistancia)
    {
        celdaUsada = new bool[columnas, filas];
        celdasEspejos.Clear();
        girosEspejos.Clear();

        // el emisor va en una celda de un borde al azar, mirando hacia dentro
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
                        celdasEspejos.Add(actual);
                        girosEspejos.Add(GiroCorrecto(direccion, nuevaDireccion));

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

            // si ya hay espejos suficientes y estamos en un buen sitio para el receptor, terminamos
            if (celdasEspejos.Count >= espejosMinimos && SitioValidoParaReceptor(actual, exigirDistancia))
            {
                celdaReceptor = actual;
                return true;
            }
        }

        // no hemos llegado a un buen sitio, este intento no sirve
        return false;
    }

    // Que giro (1 o 3) necesita un espejo para que el rayo que llega en una direccion
    // salga por la otra. La cara del espejo tiene que apuntar a (salida - entrada):
    // si apunta en diagonal (+,+) o (-,-) es el giro 1 (45 grados), si no es el 3 (135 grados).
    private int GiroCorrecto(Vector2Int entrada, Vector2Int salida)
    {
        Vector2Int normal = salida - entrada;
        if (normal.x * normal.y > 0)
        {
            return 1;
        }
        return 3;
    }

    // El receptor tiene que estar en un borde, no puede estar en la misma fila ni columna
    // que el emisor (asi el rayo nunca va recto) y, si se pide, tiene que estar lejos.
    private bool SitioValidoParaReceptor(Vector2Int celda, bool exigirDistancia)
    {
        bool enBorde = celda.x == 0 || celda.x == columnas - 1 || celda.y == 0 || celda.y == filas - 1;
        if (enBorde == false)
        {
            return false;
        }

        if (celda.x == celdaEmisor.x || celda.y == celdaEmisor.y)
        {
            return false;
        }

        if (exigirDistancia)
        {
            // distancia en celdas (sumando horizontal y vertical)
            int distancia = Mathf.Abs(celda.x - celdaEmisor.x) + Mathf.Abs(celda.y - celdaEmisor.y);
            int distanciaMinima = (columnas + filas) / 2;
            if (distancia < distanciaMinima)
            {
                return false;
            }
        }

        return true;
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

    // ---------------- ENLACES (solo datos) ----------------

    private void PrepararEspejos()
    {
        // apuntamos que espejo hay en cada celda para la simulacion del rayo
        espejoEnCelda = new int[columnas, filas];
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                espejoEnCelda[col, fila] = -1;
            }
        }
        for (int i = 0; i < celdasEspejos.Count; i++)
        {
            espejoEnCelda[celdasEspejos[i].x, celdasEspejos[i].y] = i;
        }

        // enlaces: un espejo solo puede enlazarse con otro que este mas adelante en el camino.
        // (el ultimo espejo no tiene ninguno por delante, asi que nunca tiene enlace)
        enlacesEspejos.Clear();
        for (int i = 0; i < celdasEspejos.Count; i++)
        {
            int enlace = -1;
            bool hayEspejosDelante = i < celdasEspejos.Count - 1;
            if (hayEspejosDelante && Random.value < probabilidadEnlace)
            {
                enlace = Random.Range(i + 1, celdasEspejos.Count);
            }
            enlacesEspejos.Add(enlace);
        }
    }

    // ---------------- DESORDENAR ----------------

    // Devuelve false si con este camino no se puede (entonces probaremos con otro camino).
    private bool Desordenar()
    {
        int numeroEspejos = girosEspejos.Count;

        // cada espejo se puede pulsar como mucho 3 veces (4 veces es dar la vuelta entera)
        if (numeroEspejos * 3 < pulsacionesMinimas)
        {
            return false;
        }

        for (int intento = 0; intento < 20; intento++)
        {
            // 1. decidimos cuantas veces tendra que pulsar el jugador cada espejo (1, 2 o 3)
            int[] pulsacionesJugador = new int[numeroEspejos];
            int total = 0;
            for (int i = 0; i < numeroEspejos; i++)
            {
                pulsacionesJugador[i] = Random.Range(1, 4);
                total += pulsacionesJugador[i];
            }

            // si no llegamos al minimo, vamos sumando pulsaciones a espejos al azar
            while (total < pulsacionesMinimas)
            {
                int i = Random.Range(0, numeroEspejos);
                if (pulsacionesJugador[i] < 3)
                {
                    pulsacionesJugador[i]++;
                    total++;
                }
            }

            // 2. desde la solucion, pulsamos cada espejo las veces que "deshacen" lo del jugador.
            //    (si el jugador tiene que pulsar 1 vez, nosotros pulsamos 3, porque 1 + 3 = 4 = vuelta entera)
            int[] giros = girosEspejos.ToArray();
            for (int i = 0; i < numeroEspejos; i++)
            {
                int vecesDesordenar = 4 - pulsacionesJugador[i];
                for (int v = 0; v < vecesDesordenar; v++)
                {
                    Pulsar(giros, i);
                }
            }

            // 3. por seguridad, comprobamos que no haya un atajo por otro camino del rayo
            bool hayAtajo = BuscarSolucion(giros, pulsacionesMinimas - 1, 0);
            if (hayAtajo == false)
            {
                for (int i = 0; i < numeroEspejos; i++)
                {
                    girosEspejos[i] = giros[i];
                }
                //Debug.Log("Puzle de luz: " + numeroEspejos + " espejos, se resuelve con " + total + " pulsaciones");
                return true;
            }
        }

        return false;
    }

    // Lo mismo que pasa al interactuar: gira el espejo y su enlazado 45 grados
    private void Pulsar(int[] giros, int espejo)
    {
        giros[espejo] = (giros[espejo] + 1) % 4;
        int enlace = enlacesEspejos[espejo];
        if (enlace >= 0)
        {
            giros[enlace] = (giros[enlace] + 1) % 4;
        }
    }

    // Deshace una pulsacion (girar 3 veces mas es lo mismo que volver atras)
    private void DeshacerPulsacion(int[] giros, int espejo)
    {
        Pulsar(giros, espejo);
        Pulsar(giros, espejo);
        Pulsar(giros, espejo);
    }

    // Prueba todas las combinaciones de como mucho "pulsacionesQueQuedan" pulsaciones.
    // Devuelve true si alguna hace que el rayo llegue al receptor.
    // Solo probamos pulsaciones en orden (espejo 0, luego 1...) porque el orden
    // en que se pulsa no cambia el resultado final, y asi probamos muchas menos.
    private bool BuscarSolucion(int[] giros, int pulsacionesQueQuedan, int desdeEspejo)
    {
        if (RayoLlegaAlReceptor(giros))
        {
            return true;
        }
        if (pulsacionesQueQuedan == 0)
        {
            return false;
        }

        for (int i = desdeEspejo; i < giros.Length; i++)
        {
            Pulsar(giros, i);
            bool resuelve = BuscarSolucion(giros, pulsacionesQueQuedan - 1, i);
            DeshacerPulsacion(giros, i);

            if (resuelve)
            {
                return true;
            }
        }
        return false;
    }

    // Simula el rayo celda a celda, sin fisicas, con los espejos en los giros indicados
    private bool RayoLlegaAlReceptor(int[] giros)
    {
        Vector2Int celda = celdaEmisor;
        Vector2Int direccion = direccionEmisor;

        for (int paso = 0; paso < columnas * filas * 4; paso++)
        {
            celda = celda + direccion;

            if (DentroDeLaZona(celda) == false)
            {
                return false;   // sale de la zona
            }
            if (celda == celdaReceptor)
            {
                return true;
            }
            if (celda == celdaEmisor || celdaBloqueada[celda.x, celda.y])
            {
                return false;   // choca con algo
            }

            int espejo = espejoEnCelda[celda.x, celda.y];
            if (espejo >= 0)
            {
                int giro = giros[espejo];
                if (giro == 1)
                {
                    direccion = new Vector2Int(-direccion.y, -direccion.x);
                }
                else if (giro == 3)
                {
                    direccion = new Vector2Int(direccion.y, direccion.x);
                }
                else
                {
                    // espejo recto (0 o 90 grados): el rayo vuelve hacia atras
                    direccion = new Vector2Int(-direccion.x, -direccion.y);
                }
            }
        }
        return false;
    }

    // ---------------- CREAR OBJETOS ----------------

    private void CrearPiezas()
    {
        // emisor
        Vector3 posEmisor = PosicionCelda(celdaEmisor.x, celdaEmisor.y);
        Quaternion rotEmisor = Quaternion.LookRotation(DireccionMundo(direccionEmisor), transform.up);
        GameObject emisor = CrearPieza(prefabEmisor, "Emisor de luz", posEmisor, rotEmisor,
            new Vector3(tamanoCelda * 0.3f, alturaRayo * 2f, tamanoCelda * 0.3f), Color.red);
        EmisorLuz scriptEmisor = emisor.GetComponent<EmisorLuz>();
        if (scriptEmisor == null)
        {
            scriptEmisor = emisor.AddComponent<EmisorLuz>();
        }
        // el cubo tiene el centro a la altura del rayo, pero el prefab tiene el pivote en el suelo
        if (prefabEmisor != null)
        {
            scriptEmisor.alturaSalida = alturaRayo;
        }

        // receptor
        Vector3 posReceptor = PosicionCelda(celdaReceptor.x, celdaReceptor.y);
        GameObject receptor = CrearPieza(prefabReceptor, "Receptor", posReceptor, transform.rotation,
            new Vector3(tamanoCelda * 0.4f, alturaRayo * 2f, tamanoCelda * 0.4f), Color.yellow);
        Receptor scriptReceptor = receptor.GetComponent<Receptor>();
        if (scriptReceptor == null)
        {
            scriptReceptor = receptor.AddComponent<Receptor>();
        }
        scriptReceptor.generador = this;

        // espejos, ya desordenados
        List<Espejo> espejos = new List<Espejo>();
        for (int i = 0; i < celdasEspejos.Count; i++)
        {
            Quaternion rotacion = transform.rotation * Quaternion.Euler(0, 45f * girosEspejos[i], 0);
            espejos.Add(CrearEspejo(celdasEspejos[i], rotacion));
        }

        // enlaces
        for (int i = 0; i < espejos.Count; i++)
        {
            if (enlacesEspejos[i] >= 0)
            {
                espejos[i].espejoEnlazado = espejos[enlacesEspejos[i]];
            }
        }
    }

    private Espejo CrearEspejo(Vector2Int celda, Quaternion rotacion)
    {
        Vector3 posicion = PosicionCelda(celda.x, celda.y);
        // la cara del espejo mira hacia su eje Z (flecha azul)
        GameObject espejo = CrearPieza(prefabEspejo, "Espejo " + celda.x + "," + celda.y, posicion, rotacion,
            new Vector3(tamanoCelda * 0.6f, 1.6f, 0.1f), new Color(0.6f, 0.9f, 1f));

        Espejo scriptEspejo = espejo.GetComponent<Espejo>();
        if (scriptEspejo == null)
        {
            scriptEspejo = espejo.AddComponent<Espejo>();
        }
        return scriptEspejo;
    }

    // Si hay prefab, lo crea con el pivote en el suelo.
    // Si no hay prefab, crea un cubo con la escala y el color indicados.
    private GameObject CrearPieza(GameObject prefab, string nombre, Vector3 posicionSuelo, Quaternion rotacion,
                                  Vector3 escalaCubo, Color colorCubo)
    {
        if (prefab != null)
        {
            GameObject pieza = Instantiate(prefab, posicionSuelo, rotacion, transform);
            pieza.name = nombre;
            return pieza;
        }

        return CrearCubo(nombre, posicionSuelo, rotacion, escalaCubo, colorCubo);
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
