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
    
    // private float inputH;
    // private float inputV;
    private CharacterController controller;
    private Animator anim;
    private Vector3 direccionMovimiento;
    private Vector3 direccionInput;
    private Vector3 velocidadVertical;

    private void OnEnable()
    {
        inputManager.OnSaltar += Saltar;
        inputManager.OnMover += Mover;
    }
    
    private void OnDisable()
    {
        inputManager.OnSaltar -= Saltar;
        inputManager.OnMover -= Mover;
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
        Gizmos.DrawSphere(pies.position, radioDeteccion);
    }
}
