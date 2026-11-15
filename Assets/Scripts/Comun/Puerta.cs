using UnityEngine;

// La puerta se queda cerrada hasta que alguien llama a Abrir().
// Al abrirse gira sobre su bisagra poco a poco, como una puerta de verdad.
//
// El objeto con este script es la BISAGRA: la hoja de la puerta es un hijo
// colocado de forma que su borde queda en el pivote de este objeto.
public class Puerta : MonoBehaviour
{
    // grados que gira al abrirse; el signo decide hacia qué lado se abre
    [SerializeField] private float anguloApertura = -90f;
    // grados por segundo
    [SerializeField] private float velocidadGiro = 90f;

    private bool abierta = false;
    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;

    void Start()
    {
        rotacionCerrada = transform.localRotation;
        rotacionAbierta = rotacionCerrada * Quaternion.Euler(0, anguloApertura, 0);
    }

    void Update()
    {
        if (abierta)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, rotacionAbierta, velocidadGiro * Time.deltaTime);
        }
    }

    public void Abrir()
    {
        if (abierta == false)
        {
            abierta = true;
            Debug.Log("Puerta abierta!");
        }
    }
}
