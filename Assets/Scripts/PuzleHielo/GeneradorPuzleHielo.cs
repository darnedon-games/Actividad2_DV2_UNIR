using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static UnityEditor.PlayerSettings;

// Genera el puzle de hielo al cargar la escena.
//
// EL PUZLE:
// Hay un bloque sobre hielo. Al empujarlo se desliza sin parar hasta chocar con
// una roca, una pared o el borde de la zona. Hay que dejarlo PARADO encima del
// interruptor (si solo pasa por encima, no cuenta).
//
// CÓMO FUNCIONA:
// 1. Dividimos la zona (el recuadro cian de la escena) en celdas, igual que en el puzle de espejos.
// 2. Miramos cada celda con Physics.CheckBox. Si hay algo dentro (una pared, una caja...)
//    la celda queda BLOQUEADA y cuenta como obstáculo (se ve en rojo en la escena).
// 3. Ponemos rocas al azar y comprobamos que no dejen zonas cerradas.
// 4. Ponemos el bloque en una celda libre al azar.
// 5. "Jugamos" todas las partidas posibles a la vez, primero las de 1 empujón, luego las de 2, etc...
//    (es una "búsqueda en anchura"). Así sabemos el MÍNIMO de empujones para llevar el bloque
//    a cada celda. Tenemos en cuenta dónde puede estar el jugador: para empujar tiene que
//    poder llegar andando a la celda de detrás del bloque (el bloque y las rocas le cortan el paso).
// 6. El interruptor va en la celda mas dificil que este entre "Empujes Mínimos" y "Empujes Máximos".
// Si no sale, probamos con otras rocas. Si después de muchos intentos sigue sin salir,
// bajamos los empujes mínimos de uno en uno. Cuando el puzle es bueno, creamos los objetos.

public class GeneradorPuzleHielo : MonoBehaviour
{
    [Header("Zona del puzle")]
    [SerializeField] private Vector2 tamanoZona = new Vector2(14f, 12f);
    [SerializeField] private float tamanoCelda = 1f;
    [SerializeField] private Transform[] accesos;

    [Header("Detección de obstáculos")]
    [SerializeField] private LayerMask capasObstaculos = ~0;
    [SerializeField] private float alturaComprobacion = 2f;

    [Header("Dificultad")]
    [SerializeField] private int empujesMinimos = 8;
    [SerializeField] private int empujesMaximos = 15;
    [Range(0f, 0.4f)]
    [SerializeField] private float probabilidadRoca = 0.15f;
    [SerializeField] private int intentosMaximos = 2000;

    [Header("Si el bloque se atasca")]
    [SerializeField] private float segundosAntesDeReiniciar = 1f;

    [Header("Prefabs (opcionales)")]
    // si están vacíos se crean cubos)
    [SerializeField] private GameObject prefabBloque;
    [SerializeField] private GameObject prefabRoca;
    [SerializeField] private GameObject prefabInterruptor;
    [Range(0.5f, 1f)]
    [SerializeField] private float tamanoPiezas = 0.8f;
    [SerializeField] private bool crearSueloHielo = true;
    [SerializeField] private Color colorInterruptorActivado = Color.green;

    [Header("Al resolver")]
    [SerializeField] private UnityEvent alResolver;

    private int columnas;
    private int filas;
    private bool[,] celdaBloqueada;   // ocupada por algo de la escena (pared, caja...)
    private bool[,] celdaRoca;        // ocupada por una roca que hemos puesto nosotros
    private bool[,] celdaAcceso;      // delante de una puerta: siempre libre

    private Vector2Int celdaInicioBloque;
    private Vector2Int celdaBloque;   // donde esta el bloque ahora mismo
    private Vector2Int celdaJugador;  // desde donde empujo el jugador la ultima vez
    private Vector2Int celdaInterruptor;
    private int empujesSolucion;

    // las 4 direcciones en las que se puede empujar el bloque
    // (x = columnas, hacia la derecha de la zona; y = filas, hacia delante de la zona)
    private Vector2Int[] direcciones = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    private BloqueHielo bloque;
    private Vector3 desfaseBloque;    // diferencia entre el suelo de la celda y la posicion del bloque
    private Renderer renderInterruptor;
    private Transform jugador;        // lo guardamos la primera vez que empuja
    private bool resuelto = false;

    void Start()
    {
        CalcularCuadricula();
        if (columnas < 3 || filas < 3)
        {
            Debug.LogError("La zona del puzle de hielo es demasiado pequeña para el tamaño de celda");
            PuzleResuelto(); // para que el jugador no se quede encerrado
            return;
        }

        BuscarCeldasBloqueadas();
        BuscarCeldasDeAcceso();

        // probamos con la dificultad pedida; si no sale, con un empujon menos, y asi hasta 1
        bool puzleCreado = false;
        for (int minimo = empujesMinimos; minimo >= 1 && puzleCreado == false; minimo--)
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
                Debug.LogWarning("Puzle de hielo: no sale con " + minimo + " empujones minimos, probamos con menos");
            }
        }

        if (puzleCreado == false)
        {
            Debug.LogError("No se ha podido generar el puzle de hielo. Prueba con una zona mas grande, " +
                           "celdas mas pequeñas o menos obstaculos. Abrimos la puerta para no bloquear el juego.");
            PuzleResuelto();
            return;
        }

        Debug.Log("Puzle de hielo: se resuelve con " + empujesSolucion + " empujones como minimo");
        CrearPiezas();
    }

    // ---------------- JUEGO ----------------

    // Lo llama el bloque cuando el jugador lo empuja
    public void EmpujarBloque(Vector3 direccionMundo, Transform quienEmpuja)
    {
        if (resuelto)
        {
            return;
        }
        jugador = quienEmpuja;

        Vector2Int direccion = DireccionCuadricula(direccionMundo);

        // las mismas reglas que al generar: detras del bloque tiene que haber una celda libre
        // (si no, el jugador esta fuera de la zona o en una posicion rara y no cuenta)
        Vector2Int celdaDetras = celdaBloque - direccion;
        if (CeldaVacia(celdaDetras) == false)
        {
            return;
        }

        Vector2Int destino = Deslizar(celdaBloque, direccion);

        // si tiene una roca o una pared justo delante, no se mueve
        if (destino == celdaBloque)
        {
            return;
        }

        // si estaba esperando para reiniciarse, lo cancelamos: al parar volveremos a comprobarlo
        CancelInvoke(nameof(Reiniciar));

        celdaJugador = celdaDetras;
        celdaBloque = destino;
        bloque.MoverA(PosicionCelda(destino) + desfaseBloque);
    }

    // Lo llama el bloque cuando termina de deslizarse
    public void BloqueHaParado()
    {
        if (celdaBloque == celdaInterruptor)
        {
            PuzleResuelto();
            return;
        }

        // ¿desde aqui todavia se puede llegar al interruptor?
        int[,] empujes = CalcularEmpujes(celdaBloque, celdaJugador);
        if (empujes[celdaInterruptor.x, celdaInterruptor.y] < 0)
        {
            Debug.Log("El bloque se ha atascado, vuelve a la salida");
            // Invoke llama a la funcion Reiniciar pasados unos segundos
            Invoke(nameof(Reiniciar), segundosAntesDeReiniciar);
        }
    }

    // Vuelve a poner el bloque donde empezo.
    // Tambien puedes llamarlo desde el Inspector (por ejemplo, desde un boton de reinicio).
    public void Reiniciar()
    {
        if (resuelto || bloque == null)
        {
            return;
        }

        // si el jugador esta justo donde tiene que aparecer el bloque, esperamos un poco
        if (jugador != null && CeldaDePosicion(jugador.position) == celdaInicioBloque)
        {
            Invoke(nameof(Reiniciar), 0.5f);
            return;
        }

        celdaBloque = celdaInicioBloque;
        bloque.ColocarEn(PosicionCelda(celdaBloque) + desfaseBloque);
    }

    private void PuzleResuelto()
    {
        if (resuelto)
        {
            return;
        }
        resuelto = true;
        Debug.Log("Puzle de hielo resuelto!");

        if (renderInterruptor != null)
        {
            renderInterruptor.material.color = colorInterruptorActivado;
        }

        // avisamos a todo lo que se haya conectado en el Inspector (abrir puerta, sonido...)
        alResolver.Invoke();
    }

    // Pasa una direccion del mundo (por ejemplo, del jugador al bloque)
    // a la direccion de la cuadricula que mas se le parece
    private Vector2Int DireccionCuadricula(Vector3 direccionMundo)
    {
        // cuanto va la direccion hacia la derecha y cuanto hacia delante de la zona
        float x = Vector3.Dot(direccionMundo, transform.right);
        float z = Vector3.Dot(direccionMundo, transform.forward);

        if (Mathf.Abs(x) > Mathf.Abs(z))
        {
            if (x > 0)
            {
                return Vector2Int.right;
            }
            return Vector2Int.left;
        }
        else
        {
            if (z > 0)
            {
                return Vector2Int.up;
            }
            return Vector2Int.down;
        }
    }

    // ---------------- REGLAS DEL HIELO ----------------
    // Estas funciones solo trabajan con numeros. Las usamos tanto para generar
    // el puzle como durante el juego, asi las reglas son siempre las mismas.

    // Una celda esta vacia si esta dentro de la zona y no tiene ni obstaculo ni roca
    private bool CeldaVacia(Vector2Int celda)
    {
        if (DentroDeLaZona(celda) == false)
        {
            return false;
        }
        return celdaBloqueada[celda.x, celda.y] == false && celdaRoca[celda.x, celda.y] == false;
    }

    // Devuelve la celda donde se para el bloque si lo empujamos desde "desde" hacia "direccion"
    private Vector2Int Deslizar(Vector2Int desde, Vector2Int direccion)
    {
        Vector2Int actual = desde;
        while (CeldaVacia(actual + direccion))
        {
            actual = actual + direccion;
        }
        return actual;
    }

    // Devuelve todas las celdas a las que puede llegar andando el jugador desde "inicio".
    // El jugador no puede atravesar las rocas, los obstaculos ni el bloque.
    // Empezamos en su celda y vamos "pintando" sus vecinas, y las vecinas de sus vecinas...
    private bool[,] ZonaDelJugador(Vector2Int inicio, Vector2Int celdaDelBloque)
    {
        bool[,] alcanzable = new bool[columnas, filas];
        if (CeldaVacia(inicio) == false || inicio == celdaDelBloque)
        {
            return alcanzable;
        }

        Stack<Vector2Int> pendientes = new Stack<Vector2Int>();
        pendientes.Push(inicio);
        alcanzable[inicio.x, inicio.y] = true;

        while (pendientes.Count > 0)
        {
            Vector2Int actual = pendientes.Pop();

            foreach (Vector2Int direccion in direcciones)
            {
                Vector2Int vecina = actual + direccion;
                if (CeldaVacia(vecina) && vecina != celdaDelBloque && alcanzable[vecina.x, vecina.y] == false)
                {
                    alcanzable[vecina.x, vecina.y] = true;
                    pendientes.Push(vecina);
                }
            }
        }

        return alcanzable;
    }

    // Busqueda en anchura: devuelve, para cada celda, el minimo de empujones necesarios
    // para llevar el bloque alli. -1 si no se puede llegar.
    //
    // Cada "situacion" de la partida es: donde esta el bloque + a que zona puede llegar el jugador.
    // (Da igual en que celda exacta este el jugador: si puede ir andando de una a otra, es lo mismo.)
    private int[,] CalcularEmpujes(Vector2Int bloqueInicio, Vector2Int jugadorInicio)
    {
        int[,] empujes = new int[columnas, filas];
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                empujes[col, fila] = -1;
            }
        }

        // situaciones pendientes de mirar: celda del bloque, celda del jugador y empujones hasta ahi
        Queue<Vector2Int> colaBloque = new Queue<Vector2Int>();
        Queue<Vector2Int> colaJugador = new Queue<Vector2Int>();
        Queue<int> colaEmpujes = new Queue<int>();
        colaBloque.Enqueue(bloqueInicio);
        colaJugador.Enqueue(jugadorInicio);
        colaEmpujes.Enqueue(0);

        // situaciones ya vistas. Cada una se guarda como un numero:
        // indice de la celda del bloque * total de celdas + indice de la primera celda de la zona del jugador
        HashSet<int> vistas = new HashSet<int>();
        int totalCeldas = columnas * filas;

        while (colaBloque.Count > 0)
        {
            Vector2Int celdaB = colaBloque.Dequeue();
            Vector2Int celdaJ = colaJugador.Dequeue();
            int n = colaEmpujes.Dequeue();

            bool[,] zona = ZonaDelJugador(celdaJ, celdaB);
            int situacion = Indice(celdaB) * totalCeldas + Indice(PrimeraCelda(zona));
            if (vistas.Contains(situacion))
            {
                continue;
            }
            vistas.Add(situacion);

            // como miramos primero las situaciones con menos empujones,
            // la primera vez que el bloque llega a una celda es con el minimo
            if (empujes[celdaB.x, celdaB.y] == -1)
            {
                empujes[celdaB.x, celdaB.y] = n;
            }

            foreach (Vector2Int direccion in direcciones)
            {
                // el jugador tiene que poder llegar andando a la celda de detras del bloque
                Vector2Int celdaDetras = celdaB - direccion;
                if (CeldaVacia(celdaDetras) == false || zona[celdaDetras.x, celdaDetras.y] == false)
                {
                    continue;
                }

                Vector2Int destino = Deslizar(celdaB, direccion);
                if (destino == celdaB)
                {
                    continue;
                }

                // despues de empujar, el jugador se queda en la celda de detras
                colaBloque.Enqueue(destino);
                colaJugador.Enqueue(celdaDetras);
                colaEmpujes.Enqueue(n + 1);
            }
        }

        return empujes;
    }

    // Convierte una celda en un numero unico (0, 1, 2...)
    private int Indice(Vector2Int celda)
    {
        return celda.x * filas + celda.y;
    }

    // La primera celda marcada de una zona (recorriendo columna a columna)
    private Vector2Int PrimeraCelda(bool[,] zona)
    {
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (zona[col, fila])
                {
                    return new Vector2Int(col, fila);
                }
            }
        }
        return Vector2Int.zero;
    }

    // ---------------- GENERAR ----------------

    private bool IntentarCrearPuzle(int minimo)
    {
        ColocarRocas();

        List<Vector2Int> libres = CeldasVacias();
        if (libres.Count < 3)
        {
            return false;
        }

        // las rocas no pueden dejar zonas cerradas
        Vector2Int ninguna = new Vector2Int(-1, -1);
        if (TodoConectadoSin(ninguna, libres) == false)
        {
            return false;
        }

        // el bloque empieza en una celda libre que no este delante de una puerta
        // y que no corte el paso a ninguna parte de la sala
        celdaInicioBloque = libres[Random.Range(0, libres.Count)];
        if (celdaAcceso[celdaInicioBloque.x, celdaInicioBloque.y])
        {
            return false;
        }
        if (TodoConectadoSin(celdaInicioBloque, libres) == false)
        {
            return false;
        }
        celdaBloque = celdaInicioBloque;

        // al empezar, el jugador puede estar en cualquier celda libre (estan todas conectadas)
        Vector2Int inicioJugador = libres[0];
        if (inicioJugador == celdaInicioBloque)
        {
            inicioJugador = libres[1];
        }
        celdaJugador = inicioJugador;

        // buscamos la celda mas dificil dentro de lo que queremos
        int[,] empujes = CalcularEmpujes(celdaInicioBloque, inicioJugador);
        List<Vector2Int> candidatas = new List<Vector2Int>();
        int masEmpujes = -1;

        foreach (Vector2Int celda in libres)
        {
            int n = empujes[celda.x, celda.y];
            if (n < minimo || n > empujesMaximos)
            {
                continue;
            }
            // el interruptor no puede estar delante de una puerta
            // ni cerrar el paso cuando el bloque este encima (por ejemplo, a la puerta de salida)
            if (celdaAcceso[celda.x, celda.y] || TodoConectadoSin(celda, libres) == false)
            {
                continue;
            }

            if (n > masEmpujes)
            {
                // hemos encontrado una mas dificil: empezamos la lista de nuevo
                masEmpujes = n;
                candidatas.Clear();
            }
            if (n == masEmpujes)
            {
                candidatas.Add(celda);
            }
        }

        if (candidatas.Count == 0)
        {
            return false;
        }

        celdaInterruptor = candidatas[Random.Range(0, candidatas.Count)];
        empujesSolucion = masEmpujes;
        return true;
    }

    private void ColocarRocas()
    {
        celdaRoca = new bool[columnas, filas];
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (celdaBloqueada[col, fila] || celdaAcceso[col, fila])
                {
                    continue;
                }
                if (Random.value < probabilidadRoca)
                {
                    celdaRoca[col, fila] = true;
                }
            }
        }
    }

    private List<Vector2Int> CeldasVacias()
    {
        List<Vector2Int> vacias = new List<Vector2Int>();
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                Vector2Int celda = new Vector2Int(col, fila);
                if (CeldaVacia(celda))
                {
                    vacias.Add(celda);
                }
            }
        }
        return vacias;
    }

    // Comprueba que, si ponemos el bloque en "celdaOcupada", el jugador
    // puede seguir yendo andando a todas las demas celdas libres
    private bool TodoConectadoSin(Vector2Int celdaOcupada, List<Vector2Int> libres)
    {
        // empezamos en cualquier celda libre que no sea la ocupada
        Vector2Int inicio = libres[0];
        if (inicio == celdaOcupada)
        {
            inicio = libres[1];
        }

        bool[,] zona = ZonaDelJugador(inicio, celdaOcupada);
        foreach (Vector2Int celda in libres)
        {
            if (celda != celdaOcupada && zona[celda.x, celda.y] == false)
            {
                return false;
            }
        }
        return true;
    }

    // ---------------- CUADRICULA ----------------

    private void CalcularCuadricula()
    {
        columnas = Mathf.FloorToInt(tamanoZona.x / tamanoCelda);
        filas = Mathf.FloorToInt(tamanoZona.y / tamanoCelda);
    }

    // Posicion de una celda en el mundo, a la altura del suelo
    private Vector3 PosicionCelda(Vector2Int celda)
    {
        float x = (celda.x - (columnas - 1) / 2f) * tamanoCelda;
        float z = (celda.y - (filas - 1) / 2f) * tamanoCelda;
        return transform.position + transform.right * x + transform.forward * z;
    }

    // Lo contrario: la celda en la que esta una posicion del mundo.
    // Si la posicion esta fuera de la zona, devuelve la celda del borde mas cercana.
    private Vector2Int CeldaDePosicion(Vector3 posicion)
    {
        Vector3 desdeElCentro = posicion - transform.position;
        float x = Vector3.Dot(desdeElCentro, transform.right) / tamanoCelda + (columnas - 1) / 2f;
        float z = Vector3.Dot(desdeElCentro, transform.forward) / tamanoCelda + (filas - 1) / 2f;

        int col = Mathf.Clamp(Mathf.RoundToInt(x), 0, columnas - 1);
        int fila = Mathf.Clamp(Mathf.RoundToInt(z), 0, filas - 1);
        return new Vector2Int(col, fila);
    }

    private bool DentroDeLaZona(Vector2Int celda)
    {
        return celda.x >= 0 && celda.x < columnas && celda.y >= 0 && celda.y < filas;
    }

    // Comprueba si hay algun objeto de la escena dentro de la celda
    private bool HayAlgoEnCelda(Vector2Int celda)
    {
        // empezamos 10 cm por encima del suelo para no detectar el propio suelo
        Vector3 centro = PosicionCelda(celda) + transform.up * (0.1f + alturaComprobacion / 2f);
        Vector3 mitadTamano = new Vector3(tamanoCelda / 2f, alturaComprobacion / 2f, tamanoCelda / 2f);
        return Physics.CheckBox(centro, mitadTamano, transform.rotation, capasObstaculos, QueryTriggerInteraction.Ignore);
    }

    private void BuscarCeldasBloqueadas()
    {
        celdaBloqueada = new bool[columnas, filas];
        int bloqueadas = 0;

        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (HayAlgoEnCelda(new Vector2Int(col, fila)))
                {
                    celdaBloqueada[col, fila] = true;
                    bloqueadas++;
                }
            }
        }

        Debug.Log("Puzle de hielo: " + columnas + "x" + filas + " celdas, " + bloqueadas + " bloqueadas");
    }

    // Marca la celda de la zona mas cercana a cada puerta
    private void BuscarCeldasDeAcceso()
    {
        celdaAcceso = new bool[columnas, filas];
        if (accesos == null)
        {
            return;
        }

        foreach (Transform acceso in accesos)
        {
            if (acceso != null)
            {
                Vector2Int celda = CeldaDePosicion(acceso.position);
                celdaAcceso[celda.x, celda.y] = true;
            }
        }
    }

    // ---------------- CREAR OBJETOS ----------------

    private void CrearPiezas()
    {
        float lado = tamanoCelda * tamanoPiezas;

        // suelo de hielo: solo decorativo, sin collider
        if (crearSueloHielo)
        {
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suelo.name = "Suelo de hielo";
            Destroy(suelo.GetComponent<Collider>());
            suelo.transform.SetParent(transform);
            suelo.transform.position = transform.position + transform.up * 0.01f;
            suelo.transform.rotation = transform.rotation;
            suelo.transform.localScale = new Vector3(columnas * tamanoCelda, 0.02f, filas * tamanoCelda);
            suelo.GetComponent<Renderer>().material.color = new Color(0.75f, 0.92f, 1f);
        }

        // rocas
        for (int col = 0; col < columnas; col++)
        {
            for (int fila = 0; fila < filas; fila++)
            {
                if (celdaRoca[col, fila])
                {
                    Vector2Int celda = new Vector2Int(col, fila);
                    CrearPieza(prefabRoca, "Roca " + col + "," + fila, PosicionCelda(celda),
                        new Vector3(lado, 1.2f, lado), new Color(0.4f, 0.4f, 0.45f));
                }
            }
        }

        // interruptor: es solo decorativo, le quitamos el collider para que no estorbe
        GameObject interruptor = CrearPieza(prefabInterruptor, "Interruptor", PosicionCelda(celdaInterruptor),
            new Vector3(lado, 0.05f, lado), Color.yellow);
        if (prefabInterruptor == null)
        {
            Destroy(interruptor.GetComponent<Collider>());
        }
        renderInterruptor = interruptor.GetComponentInChildren<Renderer>();

        // bloque
        GameObject objetoBloque = CrearPieza(prefabBloque, "Bloque de hielo", PosicionCelda(celdaInicioBloque),
            new Vector3(lado, lado, lado), new Color(0.5f, 0.8f, 1f));
        bloque = objetoBloque.GetComponent<BloqueHielo>();
        if (bloque == null)
        {
            bloque = objetoBloque.AddComponent<BloqueHielo>();
        }
        bloque.generador = this;

        // guardamos a que altura queda el bloque respecto al suelo de su celda,
        // para colocarlo igual en las demas celdas
        desfaseBloque = objetoBloque.transform.position - PosicionCelda(celdaInicioBloque);
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
        // el centro del cubo esta a media altura, asi su base queda en el suelo
        cubo.transform.position = posicionSuelo + transform.up * (escalaCubo.y / 2f);
        cubo.transform.rotation = transform.rotation;
        cubo.transform.localScale = escalaCubo;
        cubo.GetComponent<Renderer>().material.color = colorCubo;
        return cubo;
    }

    // ---------------- AYUDA VISUAL EN EL EDITOR ----------------

    // Dibuja la zona y las celdas: verde = libre, rojo = ocupada por algo, amarillo = acceso
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

        // las celdas solo con el objeto seleccionado y fuera del Play (en Play ya hay rocas)
        bool seleccionado = false;
#if UNITY_EDITOR
        seleccionado = UnityEditor.Selection.activeGameObject == gameObject;
#endif
        if (seleccionado && Application.isPlaying == false)
        {
            BuscarCeldasDeAcceso();

            for (int col = 0; col < columnas; col++)
            {
                for (int fila = 0; fila < filas; fila++)
                {
                    float x = (col - (columnas - 1) / 2f) * tamanoCelda;
                    float z = (fila - (filas - 1) / 2f) * tamanoCelda;

                    if (HayAlgoEnCelda(new Vector2Int(col, fila)))
                    {
                        Gizmos.color = Color.red;
                    }
                    else if (celdaAcceso[col, fila])
                    {
                        Gizmos.color = Color.yellow;
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                    }
                    Gizmos.DrawWireCube(new Vector3(x, 0.05f, z), new Vector3(tamanoCelda * 0.9f, 0.1f, tamanoCelda * 0.9f));
                }
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
