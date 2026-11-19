using UnityEngine;

// Script principal del Player para los controles y animaciones

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
    [SerializeField] private float anguloInteraccion = 40f;
    
    // private float inputH;
    // private float inputV;
    private CharacterController controller;
    private Animator anim;
    private Vector3 direccionMovimiento;
    private Vector3 direccionInput;
    private Vector3 velocidadVertical;
    private Interactuable objetoSeleccionado;

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
    
    // Interactua con el objeto que hay delante (resaltado).
    private void Interactuar()
    {
        if (objetoSeleccionado != null)
        {
            // le pasamos nuestro transform por si necesita saber donde estamos
            objetoSeleccionado.Interactuar(transform);
        }
    }
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
    }

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
        ActualizarObjetoSeleccionado();
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

    // Busca el objeto interactuable mas cercano que este DELANTE del jugador.
    // Los que estan detras o muy a los lados no cuentan.
    private Interactuable BuscarInteractuableDelante()
    {
        Collider[] cercanos = Physics.OverlapSphere(transform.position, distanciaInteraccion);

        Interactuable mejorObjeto = null;
        float distanciaMasCorta = 9999f;

        foreach (Collider col in cercanos)
        {
            // InParent por si el collider esta en un hijo del prefab
            Interactuable objeto = col.GetComponentInParent<Interactuable>();
            if (objeto == null)
            {
                continue;
            }

            // direccion hacia el objeto sin tener en cuenta la altura
            Vector3 haciaObjeto = objeto.transform.position - transform.position;
            haciaObjeto.y = 0;

            // angulo entre hacia donde mira el jugador y donde esta el objeto
            float angulo = Vector3.Angle(transform.forward, haciaObjeto);
            if (angulo > anguloInteraccion)
            {
                continue;
            }

            float distancia = haciaObjeto.magnitude;
            if (distancia < distanciaMasCorta)
            {
                distanciaMasCorta = distancia;
                mejorObjeto = objeto;
            }
        }

        return mejorObjeto;
    }

    // Cambia el objeto resaltado si ahora tenemos otro delante
    private void ActualizarObjetoSeleccionado()
    {
        Interactuable nuevo = BuscarInteractuableDelante();

        if (nuevo != objetoSeleccionado)
        {
            if (objetoSeleccionado != null)
            {
                objetoSeleccionado.Resaltar(false);
            }
            if (nuevo != null)
            {
                nuevo.Resaltar(true);
            }
            objetoSeleccionado = nuevo;
        }
    }

    private void OnDrawGizmos()
    {
        // esfera para ver colisiones entre los pies y el suelo
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(pies.position, radioDeteccion);

        // rayos de ayuda para ver hasta donde llega la interaccion
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

        // linea verde hasta el objeto seleccionado
        if (objetoSeleccionado != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origen, objetoSeleccionado.transform.position);
        }
    }
}
