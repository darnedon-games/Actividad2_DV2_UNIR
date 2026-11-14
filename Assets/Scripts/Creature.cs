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
    }

    private void Update() {

        direccionMovimiento = posicionObjetivo - transform.position;
        direccionMovimiento.y = 0;

        if (direccionMovimiento.sqrMagnitude > 0)
        {
            RotarHaciaDestino();
        }

        if (isWalking)
        {
            if (transform.position != posicionObjetivo)
            {
                StartWalking();
                transform.position = Vector3.MoveTowards(transform.position, posicionObjetivo, velocidadMovimiento * Time.deltaTime);
            }
            else
            {
                StopWalking();
                Destroy(this.gameObject);
            }
        }
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
