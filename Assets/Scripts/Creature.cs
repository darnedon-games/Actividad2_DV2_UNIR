using UnityEngine;

public class Creature : MonoBehaviour
{
    [SerializeField] private float velocidadMovimiento;
    private Animator anim;
    private Vector3 direccionMovimiento;
    public Vector3 posicionObjetivo;
    private bool isWalking;
    private float tiempoDeVida = 10f; // segundos antes de destruirse

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        Destroy(gameObject, tiempoDeVida);
        posicionObjetivo = transform.position + (transform.right * 4f);
    }

    private void Update()
    {
        direccionMovimiento = posicionObjetivo - transform.position;
        direccionMovimiento.y = 0;

        // ¿hemos llegado? (cerca basta, no hace falta la posición exacta)
        if (direccionMovimiento.sqrMagnitude < 0.01f)
        {
            if (isWalking)
            {
                StopWalking();
                Destroy(gameObject);
            }
            return;
        }

        if (isWalking == false)
        {
            StartWalking();   // solo una vez, no en cada frame
        }

        RotarHaciaDestino();
        transform.position = Vector3.MoveTowards(transform.position, posicionObjetivo, velocidadMovimiento * Time.deltaTime);
    }

    public void StartWalking()
    {
        isWalking = true;
        anim.SetBool("walking", true);
    }

    public void StopWalking()
    {
        isWalking = false;
        anim.SetBool("walking", false);
    }

    private void RotarHaciaDestino()
    {
        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
        transform.rotation = rotacionObjetivo;
    }
}
