using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float velocidadMovimiento;
    [SerializeField] private Transform camara;
    [SerializeField] private InputManagerSO inputManager;
    
    // private float inputH;
    // private float inputV;
    private CharacterController controller;
    private Animator anim;
    private Vector3 direccionMovimiento;
    private Vector3 direccionInput;

    private void OnEnable()
    {
        inputManager.OnSaltar += Saltar;
        inputManager.OnMover += Mover;
    }

    // Solo se ejecuta cuando se actualice el input de movimiento
    private void Mover(Vector2 ctx)
    {
        direccionInput = new Vector3(ctx.x, 0, ctx.y);
        RotarHaciaDestino();
    }
    
    private void Saltar()
    {
        Debug.Log("El player salta");
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
    }

    private void RotarHaciaDestino()
    {
        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
        transform.rotation = rotacionObjetivo;
    }
}
