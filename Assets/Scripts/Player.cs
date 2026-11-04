using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float velocidadMovimiento;
    [SerializeField] private float factorGravedad;
    [SerializeField] private Transform camara;
    [SerializeField] private InputManagerSO inputManager;
    [SerializeField] private float alturaDeSalto;
    
    [Header("Detección Suelo")]
    [SerializeField] private Transform pies;
    [SerializeField] private float radioDeteccion;
    [SerializeField] private LayerMask queEsSuelo;

    [Header("Interacción")]
    [SerializeField] private float distanciaInteraccion = 2.5f;
    [Tooltip("Cuantos grados a cada lado de la mirada cuenta como 'delante'")]
    [SerializeField] private float anguloInteraccion = 40f;
    
    // private float inputH;
    // private float inputV;
    private CharacterController controller;
    private Animator anim;
    private Vector3 direccionMovimiento;
    private Vector3 direccionInput;
    private Vector3 velocidadVertical;
    private Espejo espejoSeleccionado;

    private void OnEnable()
    {
        inputManager.OnSaltar += Saltar;
        inputManager.OnMover += Mover;
        inputManager.OnInteractuar += Interactuar;
    }
    
    private void OnDisable()
    {
        inputManager.OnSaltar -= Saltar;
        inputManager.OnMover -= Mover;
        inputManager.OnInteractuar -= Interactuar;
    }

    // Solo se ejecuta cuando se actualice el input de movimiento
    private void Mover(Vector2 ctx)
    {
        direccionInput = new Vector3(ctx.x, 0, ctx.y);
    }
    
    private void Saltar()
    {
        if (EstoyEnSuelo())
        {
            anim.SetTrigger("salto");
            velocidadVertical.y = Mathf.Sqrt(-2 * factorGravedad * alturaDeSalto);
        }
    }
    
    // Gira el espejo que tenemos delante (el que esta resaltado)
    private void Interactuar()
    {
        if (espejoSeleccionado != null)
        {
            espejoSeleccionado.Rotar();
        }
    }

    // Busca el espejo mas cercano que este DELANTE del jugador.
    // Los que estan detras o muy a los lados no cuentan.
    private Espejo BuscarEspejoDelante()
    {
        Collider[] cercanos = Physics.OverlapSphere(transform.position, distanciaInteraccion);

        Espejo mejorEspejo = null;
        float distanciaMasCorta = 9999f;

        foreach (Collider col in cercanos)
        {
            Espejo espejo = col.GetComponent<Espejo>();
            if (espejo == null)
            {
                continue;
            }

            // direccion hacia el espejo sin tener en cuenta la altura
            Vector3 haciaEspejo = espejo.transform.position - transform.position;
            haciaEspejo.y = 0;

            // angulo entre hacia donde mira el jugador y donde esta el espejo
            float angulo = Vector3.Angle(transform.forward, haciaEspejo);
            if (angulo > anguloInteraccion)
            {
                continue;
            }

            float distancia = haciaEspejo.magnitude;
            if (distancia < distanciaMasCorta)
            {
                distanciaMasCorta = distancia;
                mejorEspejo = espejo;
            }
        }

        return mejorEspejo;
    }

    // Cambia el espejo resaltado si ahora tenemos otro delante
    private void ActualizarEspejoSeleccionado()
    {
        Espejo nuevo = BuscarEspejoDelante();

        if (nuevo != espejoSeleccionado)
        {
            if (espejoSeleccionado != null)
            {
                espejoSeleccionado.Resaltar(false);
            }
            if (nuevo != null)
            {
                nuevo.Resaltar(true);
            }
            espejoSeleccionado = nuevo;
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // inputH = Input.GetAxis("Horizontal");
        // inputV = Input.GetAxis("Vertical");

        direccionMovimiento = camara.forward * direccionInput.z + camara.right * direccionInput.x;
        direccionMovimiento.y = 0;
        controller.Move(direccionMovimiento * (velocidadMovimiento * Time.deltaTime));

        anim.SetFloat("velocidad", controller.velocity.magnitude);

        if (direccionMovimiento.sqrMagnitude > 0)
        {
            RotarHaciaDestino();
        }

        // si hemos aterrizado...
        if (EstoyEnSuelo() && velocidadVertical.y < 0)
        {
            velocidadVertical.y = 0;
            anim.ResetTrigger("salto");
        }

        AplicarGravedad();
        ActualizarEspejoSeleccionado();
    }

    private void AplicarGravedad()
    {
        velocidadVertical.y += factorGravedad * Time.deltaTime;
        controller.Move(velocidadVertical  * Time.deltaTime);
    }

    private bool EstoyEnSuelo()
    {
        return Physics.CheckSphere(pies.position, radioDeteccion, queEsSuelo);
    }

    private void RotarHaciaDestino()
    {
        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
        transform.rotation = rotacionObjetivo;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(pies.position, radioDeteccion);

        // Rayos de ayuda para ver hasta donde llega la interaccion (solo se ven en el editor)
        Vector3 origen = transform.position + Vector3.up * 1f;

        // rayo central: hacia donde mira el jugador, con la distancia de interaccion
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origen, transform.forward * distanciaInteraccion);

        // los dos bordes del angulo que cuenta como "delante"
        Vector3 bordeIzquierdo = Quaternion.Euler(0, -anguloInteraccion, 0) * transform.forward;
        Vector3 bordeDerecho = Quaternion.Euler(0, anguloInteraccion, 0) * transform.forward;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(origen, bordeIzquierdo * distanciaInteraccion);
        Gizmos.DrawRay(origen, bordeDerecho * distanciaInteraccion);

        // linea verde hasta el espejo seleccionado
        if (espejoSeleccionado != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origen, espejoSeleccionado.transform.position);
        }
    }
}
