using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Genera el puzle de baldosas al cargar la escena.
//
// EL PUZLE:
// En el suelo hay X por Y baldosas apagadas y algunas rocas. Cada vez que el jugador
// pisa una baldosa, cambia: si estaba apagada se enciende y si estaba encendida se apaga.
// Hay que encenderlas todas, así que lo ideal es pasar por cada baldosa UNA sola vez.
// Si el jugador se equivoca, vuelve a pasar por encima para apagarla.
// Junto a la entrada hay un interruptor que apaga todas las baldosas para volver a empezar.
// Cuando todas están encendidas, se abre la puerta.
//
// CÓMO FUNCIONA:
// 1. Dividimos la zona en X por Y celdas. Las celdas donde ya hay algo de la escena
//    (una pared, una caja...) no tendrán baldosa (se ven en rojo en la escena).
// 2. Ponemos rocas al azar, sin que se toquen entre ellas, para que queden repartidas.
// 3. Buscamos un camino que empiece en la entrada y pase por todas las baldosas una sola vez.
//    Lo buscamos probando caminos ("búsqueda en profundidad"): avanzamos, y si nos quedamos
//    sin salida, deshacemos el último paso y probamos otro. Para que sea rápido:
//    - Primero probamos las casillas con menos salidas (así no dejamos rincones para el final).
//    - Si alguna baldosa se queda sin forma de llegar a ella, abandonamos ese camino.
// 4. Si no hay camino, probamos con otras rocas. Si aun así no sale, ponemos una roca menos.
// Así SIEMPRE se puede resolver: el camino que hemos encontrado es la solución.
// (En la vista Scene, durante el Play, se dibuja la solución en cian.)
//
// EL TABLERO DE AJEDREZ:
// Si pintamos las celdas como un tablero de ajedrez, al andar siempre pasamos de una
// clara a una oscura y al revés. Así que un camino que pasa por todas las baldosas
// necesita que haya las mismas claras que oscuras (o una más del color de la entrada).
// Por eso elegimos de qué color es cada roca: si no, la mayoría de intentos serían imposibles.
//
// PARA USARLO EN OTRO PROYECTO:
// - Copia las carpetas Comun y PuzleBaldosas.
// - Crea un objeto vacío, ponlo SOBRE EL SUELO de la sala y añádele este script.
// - Ajusta el número y el tamaño de las baldosas hasta que el recuadro cian quepa en la sala.
// - Arrastra la puerta de entrada a "Entrada" (el camino empieza en la baldosa más cercana),
//   la de salida a "Salida" (esa baldosa nunca tendrá roca) y el jugador a "Jugador".
// - El interruptor de reinicio se pone solo junto a la entrada. Si quieres elegir el sitio,
//   crea un objeto vacío donde lo quieras y arrástralo a "Lugar Interruptor".
// - En "Al Resolver" conecta lo que deba pasar al resolverlo (por ejemplo Puerta > Abrir).
public class GeneradorPuzleBaldosas : MonoBehaviour
{
    [Header("Baldosas")]
    [SerializeField] private int baldosasX = 8;
    [SerializeField] private int baldosasY = 8;
    [SerializeField] private float tamanoBaldosa = 1.5f;
    [SerializeField] private Transform entrada;
    [SerializeField] private Transform salida;
    // si está vacío se busca el objeto con la etiqueta "Player"
    [SerializeField] private Transform jugador;
    // dónde va el interruptor que apaga todas las baldosas.
    // Si está vacío se pone dentro de la sala, al lado de la entrada.
    [SerializeField] private Transform lugarInterruptor;

    [Header("Detección de obstáculos")]
    [SerializeField] private LayerMask capasObstaculos = ~0;
    [SerializeField] private float alturaComprobacion = 2f;

    [Header("Dificultad")]
    [SerializeField] private int numeroRocas = 8;
    // intentos con cada número de rocas antes de probar con una menos
    [SerializeField] private int intentosMaximos = 3000;
    // cuántos pasos puede dar la búsqueda del camino en cada intento antes de rendirse.
    // Es más rápido rendirse pronto y probar otras rocas que insistir mucho con las mismas.
    [SerializeField] private int pasosMaximosBusqueda = 200;

    [Header("Colores")]
    [SerializeField] private Color colorApagada = new Color(0.25f, 0.3f, 0.4f);
    [SerializeField] private Color colorEncendida = new Color(1f, 0.85f, 0.2f);

    [Header("Prefabs (opcionales)")]
    // si están vacíos se crean cubos. Pivote en el suelo y que quepan en una baldosa.
    [SerializeField] private GameObject prefabBaldosa;
    [SerializeField] private GameObject prefabRoca;
    // el prefab del interruptor necesita un collider para que el jugador lo detecte
    [SerializeField] private GameObject prefabInterruptor;

    [Header("Al resolver")]
    [SerializeField] private UnityEvent alResolver;

    private int columnas;
    private int filas;
    private bool[,] celdaBloqueada;   // ocupada por algo de la escena (pared, caja...)
    private bool[,] celdaRoca;        // ocupada por una roca que hemos puesto nosotros

    private Vector2Int celdaEntrada;
    private Vector2Int celdaSalida;
    private bool haySalida = false;

    // datos de la búsqueda del camino
    private List<Vector2Int> camino = new List<Vector2Int>();
    private bool[,] visitada;
    private int totalBaldosas;
    private int pasosBusqueda;

    // las 4 direcciones en las que se puede andar
    private Vector2Int[] direcciones = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    // durante el juego
    private Baldosa[,] baldosas;
    private int encendidas = 0;
    private Vector2Int celdaAnteriorJugador = new Vector2Int(-1, -1);
    private bool resuelto = false;

    void Start()
    {
        columnas = baldosasX;
        filas = baldosasY;
        if (columnas < 2 || filas < 2)
        {
            Debug.LogError("El puzle de baldosas necesita al menos 2x2 baldosas");
            PuzleResuelto(); // para que el jugador no se quede encerrado
            return;
        }

        if (jugador == null)
        {
            GameObject objetoJugador = GameObject.FindWithTag("Player");
            if (objetoJugador != null)
            {
                jugador = objetoJugador.transform;
            }
            else
            {
                Debug.LogError("El puzle de baldosas no encuentra al jugador: arrástralo a \"Jugador\"");
            }
        }

        BuscarCeldasBloqueadas();
        BuscarEntradaYSalida();
        if (DentroDeLaZona(celdaEntrada) == false)
        {
            Debug.LogError("El puzle de baldosas no tiene ninguna celda libre (¿está la zona dentro de una pared?)");
            PuzleResuelto();
            return;
        }

        // probamos con las rocas pedidas; si no sale, con una menos, y así hasta 0
        bool puzleCreado = false;
        for (int rocas = numeroRocas; rocas >= 0 && puzleCreado == false; rocas--)
        {
            for (int i = 0; i < intentosMaximos; i++)
            {
                if (IntentarCrearPuzle(rocas))
                {
                    puzleCreado = true;
                    Debug.Log("Puzle de baldosas: " + totalBaldosas + " baldosas y " + rocas + " rocas");
                    break;
                }
            }

            if (puzleCreado == false)
            {
                Debug.LogWarning("Puzle de baldosas: no sale con " + rocas + " rocas, probamos con menos");
            }
        }

        if (puzleCreado == false)
        {
            Debug.LogError("No se ha podido generar el puzle de baldosas. Prueba con otro número de baldosas " +
                           "o quita obstáculos de la zona. Abrimos la puerta para no bloquear el juego.");
            PuzleResuelto();
            return;
        }

        CrearPiezas();
    }

    // ---------------- JUEGO ----------------

    void Update()
    {
        if (resuelto || jugador == null || baldosas == null)
        {
            return;
        }

        // solo hacemos algo cuando el jugador cambia de celda
        Vector2Int celda = CeldaDePosicion(jugador.position);
        if (celda == celdaAnteriorJugador)
        {
            return;
        }
        celdaAnteriorJugador = celda;

        // ¿ha entrado en una baldosa? (fuera de la zona la celda es (-1, -1) y no hay baldosa)
        if (DentroDeLaZona(celda) == false || baldosas[celda.x, celda.y] == null)
        {
            return;
        }

        Baldosa baldosa = baldosas[celda.x, celda.y];
        baldosa.Alternar();

        if (baldosa.EstaEncendida())
        {
            encendidas++;
        }
        else
        {
            encendidas--;
        }

        if (encendidas == totalBaldosas)
        {
            PuzleResuelto();
        }
    }

    // Lo llama el interruptor: apaga todas las baldosas para volver a empezar
    public void Reiniciar()
    {
        if (resuelto || baldosas == null)
        {
            return;
        }

        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (baldosas[col, fila] != null)
                {
                    baldosas[col, fila].Apagar();
                }
            }
        }
        encendidas = 0;

        // la baldosa en la que esté el jugador ahora no cuenta como pisada:
        // solo cambiará cuando salga de ella y vuelva a entrar
        if (jugador != null)
        {
            celdaAnteriorJugador = CeldaDePosicion(jugador.position);
        }
        Debug.Log("Puzle de baldosas reiniciado");
    }

    private void PuzleResuelto()
    {
        if (resuelto)
        {
            return;
        }
        resuelto = true;
        Debug.Log("Puzle de baldosas resuelto!");

        // avisamos a todo lo que se haya conectado en el Inspector (abrir puerta, sonido...)
        alResolver.Invoke();
    }

    // ---------------- GENERAR ----------------

    private bool IntentarCrearPuzle(int rocas)
    {
        if (ColocarRocas(rocas) == false)
        {
            return false;
        }

        // contamos las baldosas que hay que encender
        totalBaldosas = 0;
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (EsLibre(new Vector2Int(col, fila)))
                {
                    totalBaldosas++;
                }
            }
        }

        // buscamos un camino que empiece en la entrada y pase por todas una sola vez
        camino.Clear();
        visitada = new bool[columnas, filas];
        pasosBusqueda = 0;

        camino.Add(celdaEntrada);
        visitada[celdaEntrada.x, celdaEntrada.y] = true;

        return BuscarCamino();
    }

    // Pone las rocas al azar, repartidas y del color de tablero que haga falta
    private bool ColocarRocas(int rocas)
    {
        celdaRoca = new bool[columnas, filas];

        // contamos las celdas sin obstáculos de cada color del tablero (0 = clara, 1 = oscura)
        int[] celdasDeColor = new int[2];
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (celdaBloqueada[col, fila] == false)
                {
                    celdasDeColor[ColorTablero(new Vector2Int(col, fila))]++;
                }
            }
        }

        // cuántas baldosas de cada color queremos que queden:
        // la mitad de cada una, y si sobra una, del color de la entrada
        int libres = celdasDeColor[0] + celdasDeColor[1] - rocas;
        int[] queremos = new int[2];
        queremos[0] = libres / 2;
        queremos[1] = libres / 2;
        if (libres % 2 == 1)
        {
            queremos[ColorTablero(celdaEntrada)]++;
        }

        // así que estas son las rocas que tiene que haber de cada color
        int[] rocasDeColor = new int[2];
        rocasDeColor[0] = celdasDeColor[0] - queremos[0];
        rocasDeColor[1] = celdasDeColor[1] - queremos[1];
        if (rocasDeColor[0] < 0 || rocasDeColor[1] < 0)
        {
            return false;
        }

        // recorremos las celdas en orden aleatorio poniendo rocas donde se pueda
        List<Vector2Int> celdas = new List<Vector2Int>();
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                celdas.Add(new Vector2Int(col, fila));
            }
        }
        Desordenar(celdas);

        foreach (Vector2Int celda in celdas)
        {
            int color = ColorTablero(celda);
            if (rocasDeColor[color] == 0 || celdaBloqueada[celda.x, celda.y])
            {
                continue;
            }
            // la entrada y la salida, libres; y nada pegado a la entrada para que se pueda empezar
            if (celda == celdaEntrada || (haySalida && celda == celdaSalida) || SonVecinas(celda, celdaEntrada))
            {
                continue;
            }
            // dos rocas nunca se tocan, así quedan repartidas
            if (TieneRocaAlLado(celda))
            {
                continue;
            }

            celdaRoca[celda.x, celda.y] = true;
            rocasDeColor[color]--;
        }

        // ¿hemos podido poner todas?
        return rocasDeColor[0] == 0 && rocasDeColor[1] == 0;
    }

    // Búsqueda en profundidad: intenta alargar el camino hasta pasar por todas las baldosas.
    // Devuelve true si lo consigue. Se llama a sí misma para cada paso ("recursividad").
    private bool BuscarCamino()
    {
        if (camino.Count == totalBaldosas)
        {
            return true; // ¡hemos pasado por todas!
        }

        // si tarda demasiado, nos rendimos y probamos con otras rocas
        pasosBusqueda++;
        if (pasosBusqueda > pasosMaximosBusqueda)
        {
            return false;
        }

        Vector2Int actual = camino[camino.Count - 1];

        if (HayBaldosasPerdidas(actual))
        {
            return false;
        }

        // las celdas a las que podemos ir, primero las que tienen menos salidas
        List<Vector2Int> opciones = new List<Vector2Int>();
        foreach (Vector2Int direccion in direcciones)
        {
            Vector2Int vecina = actual + direccion;
            if (EsLibre(vecina) && visitada[vecina.x, vecina.y] == false)
            {
                opciones.Add(vecina);
            }
        }
        Desordenar(opciones);
        opciones.Sort((a, b) => Salidas(a).CompareTo(Salidas(b)));

        foreach (Vector2Int siguiente in opciones)
        {
            // damos el paso...
            camino.Add(siguiente);
            visitada[siguiente.x, siguiente.y] = true;

            if (BuscarCamino())
            {
                return true;
            }

            // ...y si por ahí no se llega, lo deshacemos y probamos la siguiente opción
            camino.RemoveAt(camino.Count - 1);
            visitada[siguiente.x, siguiente.y] = false;
        }

        return false;
    }

    // Mira si alguna baldosa sin visitar ya no se podrá recorrer:
    // - si no tiene ninguna forma de llegar a ella, seguro que no.
    // - si tiene una sola, solo puede ser el final del camino, y final solo hay uno.
    private bool HayBaldosasPerdidas(Vector2Int actual)
    {
        int finalesPosibles = 0;

        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                Vector2Int celda = new Vector2Int(col, fila);
                if (EsLibre(celda) == false || visitada[col, fila])
                {
                    continue;
                }

                int formasDeLlegar = Salidas(celda);
                if (SonVecinas(celda, actual))
                {
                    formasDeLlegar++;
                }

                if (formasDeLlegar == 0)
                {
                    return true;
                }
                if (formasDeLlegar == 1)
                {
                    finalesPosibles++;
                }
            }
        }

        return finalesPosibles > 1;
    }

    // Cuántas vecinas libres y sin visitar tiene una celda
    private int Salidas(Vector2Int celda)
    {
        int salidas = 0;
        foreach (Vector2Int direccion in direcciones)
        {
            Vector2Int vecina = celda + direccion;
            if (EsLibre(vecina) && visitada[vecina.x, vecina.y] == false)
            {
                salidas++;
            }
        }
        return salidas;
    }

    // ---------------- CUADRICULA ----------------

    // Una celda es libre (tendrá baldosa) si está dentro y no tiene obstáculo ni roca
    private bool EsLibre(Vector2Int celda)
    {
        if (DentroDeLaZona(celda) == false)
        {
            return false;
        }
        return celdaBloqueada[celda.x, celda.y] == false && celdaRoca[celda.x, celda.y] == false;
    }

    private bool DentroDeLaZona(Vector2Int celda)
    {
        return celda.x >= 0 && celda.x < columnas && celda.y >= 0 && celda.y < filas;
    }

    // 0 o 1, como las casillas claras y oscuras de un tablero de ajedrez
    private int ColorTablero(Vector2Int celda)
    {
        return (celda.x + celda.y) % 2;
    }

    private bool SonVecinas(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    private bool TieneRocaAlLado(Vector2Int celda)
    {
        foreach (Vector2Int direccion in direcciones)
        {
            Vector2Int vecina = celda + direccion;
            if (DentroDeLaZona(vecina) && celdaRoca[vecina.x, vecina.y])
            {
                return true;
            }
        }
        return false;
    }

    // Cambia el orden de una lista al azar
    private void Desordenar(List<Vector2Int> lista)
    {
        for (int i = 0; i < lista.Count; i++)
        {
            int otro = Random.Range(i, lista.Count);
            Vector2Int guardada = lista[i];
            lista[i] = lista[otro];
            lista[otro] = guardada;
        }
    }

    // Posición de una celda en el mundo, a la altura del suelo
    private Vector3 PosicionCelda(Vector2Int celda)
    {
        float x = (celda.x - (columnas - 1) / 2f) * tamanoBaldosa;
        float z = (celda.y - (filas - 1) / 2f) * tamanoBaldosa;
        return transform.position + transform.right * x + transform.forward * z;
    }

    // Lo contrario: la celda en la que está una posición del mundo.
    // Si está fuera de la zona devuelve (-1, -1).
    private Vector2Int CeldaDePosicion(Vector3 posicion)
    {
        Vector3 desdeElCentro = posicion - transform.position;
        float x = Vector3.Dot(desdeElCentro, transform.right) / tamanoBaldosa + (columnas - 1) / 2f;
        float z = Vector3.Dot(desdeElCentro, transform.forward) / tamanoBaldosa + (filas - 1) / 2f;

        Vector2Int celda = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(z));
        if (DentroDeLaZona(celda) == false)
        {
            return new Vector2Int(-1, -1);
        }
        return celda;
    }

    // La celda sin obstáculos más cercana a una posición del mundo
    private Vector2Int CeldaLibreMasCercana(Vector3 posicion)
    {
        Vector2Int mejor = new Vector2Int(-1, -1);
        float distanciaMasCorta = Mathf.Infinity;

        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (celdaBloqueada[col, fila])
                {
                    continue;
                }
                Vector2Int celda = new Vector2Int(col, fila);
                float distancia = Vector3.Distance(PosicionCelda(celda), posicion);
                if (distancia < distanciaMasCorta)
                {
                    distanciaMasCorta = distancia;
                    mejor = celda;
                }
            }
        }
        return mejor;
    }

    // Comprueba si hay algún objeto de la escena dentro de la celda
    private bool HayAlgoEnCelda(Vector2Int celda)
    {
        // empezamos 10 cm por encima del suelo para no detectar el propio suelo
        Vector3 centro = PosicionCelda(celda) + transform.up * (0.1f + alturaComprobacion / 2f);
        Vector3 mitadTamano = new Vector3(tamanoBaldosa / 2f, alturaComprobacion / 2f, tamanoBaldosa / 2f);
        return Physics.CheckBox(centro, mitadTamano, transform.rotation, capasObstaculos, QueryTriggerInteraction.Ignore);
    }

    private void BuscarCeldasBloqueadas()
    {
        celdaBloqueada = new bool[columnas, filas];
        celdaRoca = new bool[columnas, filas];

        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (HayAlgoEnCelda(new Vector2Int(col, fila)))
                {
                    celdaBloqueada[col, fila] = true;
                }
            }
        }
    }

    private void BuscarEntradaYSalida()
    {
        // sin entrada empezamos en el centro de un lado
        if (entrada != null)
        {
            celdaEntrada = CeldaLibreMasCercana(entrada.position);
        }
        else
        {
            Debug.LogWarning("Puzle de baldosas sin entrada: el camino empezará en el borde izquierdo");
            celdaEntrada = CeldaLibreMasCercana(PosicionCelda(new Vector2Int(0, filas / 2)));
        }

        if (salida != null)
        {
            celdaSalida = CeldaLibreMasCercana(salida.position);
            haySalida = true;
        }
    }

    // ---------------- CREAR OBJETOS ----------------

    private void CrearPiezas()
    {
        baldosas = new Baldosa[columnas, filas];
        encendidas = 0;

        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                Vector2Int celda = new Vector2Int(col, fila);

                if (celdaRoca[col, fila])
                {
                    CrearPieza(prefabRoca, "Roca " + col + "," + fila, PosicionCelda(celda),
                        new Vector3(tamanoBaldosa * 0.8f, 1.2f, tamanoBaldosa * 0.8f), new Color(0.4f, 0.4f, 0.45f));
                }
                else if (EsLibre(celda))
                {
                    baldosas[col, fila] = CrearBaldosa(celda);
                }
            }
        }

        CrearInterruptor();
    }

    private void CrearInterruptor()
    {
        Vector3 posicion;
        Quaternion rotacion;

        if (lugarInterruptor != null)
        {
            posicion = lugarInterruptor.position;
            rotacion = lugarInterruptor.rotation;
        }
        else if (entrada != null)
        {
            // desde la entrada, un poco hacia dentro de la sala y a un lado de la puerta
            Vector3 haciaDentro = PosicionCelda(celdaEntrada) - entrada.position;
            haciaDentro = Vector3.ProjectOnPlane(haciaDentro, transform.up).normalized;
            Vector3 aUnLado = Vector3.Cross(transform.up, haciaDentro);

            posicion = entrada.position + haciaDentro * 0.6f + aUnLado * 1.2f;
            // mirando hacia la puerta, que es por donde llega el jugador
            rotacion = Quaternion.LookRotation(-haciaDentro, transform.up);
        }
        else
        {
            // sin entrada, fuera de la zona, al lado de la baldosa donde empieza el camino
            posicion = PosicionCelda(celdaEntrada) - transform.right * tamanoBaldosa;
            rotacion = transform.rotation;
        }

        GameObject objeto;
        if (prefabInterruptor != null)
        {
            objeto = Instantiate(prefabInterruptor, posicion, rotacion, transform);
            objeto.name = "Interruptor reinicio";
        }
        else
        {
            // un objeto vacío con un poste y un botón rojo encima
            objeto = new GameObject("Interruptor reinicio");
            objeto.transform.SetParent(transform);
            objeto.transform.position = posicion;
            objeto.transform.rotation = rotacion;

            CrearCuboHijo("Poste", objeto.transform, 0.55f, new Vector3(0.3f, 1.1f, 0.3f), new Color(0.4f, 0.4f, 0.45f));
            CrearCuboHijo("Boton", objeto.transform, 1.15f, new Vector3(0.22f, 0.1f, 0.22f), new Color(0.8f, 0.1f, 0.1f));
        }

        InterruptorReinicio interruptor = objeto.GetComponent<InterruptorReinicio>();
        if (interruptor == null)
        {
            interruptor = objeto.AddComponent<InterruptorReinicio>();
        }
        interruptor.generador = this;
    }

    // Crea un cubo dentro de "padre", con su centro a la altura indicada
    private void CrearCuboHijo(string nombre, Transform padre, float alturaCentro, Vector3 escala, Color color)
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = nombre;
        cubo.transform.SetParent(padre);
        cubo.transform.position = padre.position + padre.up * alturaCentro;
        cubo.transform.rotation = padre.rotation;
        cubo.transform.localScale = escala;
        cubo.GetComponent<Renderer>().material.color = color;
    }

    private Baldosa CrearBaldosa(Vector2Int celda)
    {
        // un poco más pequeña que la celda para que se vea la separación entre baldosas
        GameObject objeto = CrearPieza(prefabBaldosa, "Baldosa " + celda.x + "," + celda.y, PosicionCelda(celda),
            new Vector3(tamanoBaldosa * 0.92f, 0.04f, tamanoBaldosa * 0.92f), colorApagada);

        // la baldosa es solo decorativa: sin collider para que no moleste al andar
        if (prefabBaldosa == null)
        {
            Destroy(objeto.GetComponent<Collider>());
        }

        Baldosa baldosa = objeto.GetComponent<Baldosa>();
        if (baldosa == null)
        {
            baldosa = objeto.AddComponent<Baldosa>();
        }
        baldosa.colorApagada = colorApagada;
        baldosa.colorEncendida = colorEncendida;
        baldosa.Apagar();
        return baldosa;
    }

    // Si hay prefab, lo crea con el pivote en el suelo.
    // Si no hay prefab, crea un cubo apoyado en el suelo con la escala y el color indicados.
    private GameObject CrearPieza(GameObject prefab, string nombre, Vector3 posicionSuelo, Vector3 escalaCubo, Color colorCubo)
    {
        if (prefab != null)
        {
            GameObject pieza = Instantiate(prefab, posicionSuelo, transform.rotation, transform);
            pieza.name = nombre;
            return pieza;
        }

        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = nombre;
        cubo.transform.SetParent(transform);
        // el centro del cubo está a media altura, así su base queda en el suelo
        cubo.transform.position = posicionSuelo + transform.up * (escalaCubo.y / 2f);
        cubo.transform.rotation = transform.rotation;
        cubo.transform.localScale = escalaCubo;
        cubo.GetComponent<Renderer>().material.color = colorCubo;
        return cubo;
    }

    // ---------------- AYUDA VISUAL EN EL EDITOR ----------------

    // Dibuja la zona y las celdas: verde = libre, rojo = ocupada por algo.
    // Durante el Play dibuja la solución en cian.
    private void OnDrawGizmos()
    {
        if (tamanoBaldosa <= 0 || baldosasX < 1 || baldosasY < 1)
        {
            return;
        }
        columnas = baldosasX;
        filas = baldosasY;

        // la solución, en coordenadas del mundo
        if (Application.isPlaying && camino.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 1; i < camino.Count; i++)
            {
                Vector3 desde = PosicionCelda(camino[i - 1]) + transform.up * 0.2f;
                Vector3 hasta = PosicionCelda(camino[i]) + transform.up * 0.2f;
                Gizmos.DrawLine(desde, hasta);
            }
            Gizmos.DrawSphere(PosicionCelda(camino[0]) + transform.up * 0.2f, 0.2f);
        }

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Gizmos.color = Color.cyan;
        Vector3 tamanoTotal = new Vector3(columnas * tamanoBaldosa, alturaComprobacion, filas * tamanoBaldosa);
        Gizmos.DrawWireCube(new Vector3(0, alturaComprobacion / 2f, 0), tamanoTotal);

        // las celdas solo con el objeto seleccionado y fuera del Play
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
                    float x = (col - (columnas - 1) / 2f) * tamanoBaldosa;
                    float z = (fila - (filas - 1) / 2f) * tamanoBaldosa;

                    if (HayAlgoEnCelda(new Vector2Int(col, fila)))
                    {
                        Gizmos.color = Color.red;
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                    }
                    Gizmos.DrawWireCube(new Vector3(x, 0.05f, z), new Vector3(tamanoBaldosa * 0.9f, 0.1f, tamanoBaldosa * 0.9f));
                }
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
