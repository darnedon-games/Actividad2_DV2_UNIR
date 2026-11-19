using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Menú de pausa: se abre y se cierra con Escape (teclado) o Start (mando).
// Mientras está abierto el juego se congela: el tiempo se para, el jugador no
// recibe controles y los componentes de "Desactivar En Pausa" (como el control
// de la cámara) se apagan.
// Los botones del panel llaman a Continuar() y SalirAlMenu().
public class MenuPausa : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa;
    // el botón que se selecciona al abrir el menú (para poder usarlo con el mando)
    [SerializeField] private GameObject botonInicial;
    [SerializeField] private InputManagerSO inputManager;
    // componentes que se apagan mientras el juego está congelado (por ejemplo, el control de la cámara)
    [SerializeField] private Behaviour[] desactivarEnPausa;
    [SerializeField] private string escenaMenu = "Menu";

    private bool enPausa = false;
    private CursorLockMode cursorAntes;

    void Start()
    {
        panelPausa.SetActive(false);
        // por si venimos de otra escena con los controles apagados
        inputManager.ActivarControles(true);
    }

    void Update()
    {
        if (SeHaPulsadoPausa())
        {
            if (enPausa)
            {
                Continuar();
            }
            else
            {
                Pausar();
            }
        }

        if (enPausa)
        {
            MantenerBotonSeleccionado(botonInicial);
        }
    }

    private bool SeHaPulsadoPausa()
    {
        bool teclado = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool mando = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        return teclado || mando;
    }

    public void Pausar()
    {
        enPausa = true;
        CongelarJuego(true);
        panelPausa.SetActive(true);
        EventSystem.current.SetSelectedGameObject(botonInicial);
    }

    // Botón CONTINUE
    public void Continuar()
    {
        enPausa = false;
        panelPausa.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
        CongelarJuego(false);
    }

    // Botón EXIT MENU
    public void SalirAlMenu()
    {
        CargarEscena(escenaMenu);
    }

    // Para o reanuda el juego. También lo usa el menú de victoria.
    public void CongelarJuego(bool congelar)
    {
        if (congelar)
        {
            Time.timeScale = 0f;
            cursorAntes = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = cursorAntes;
            Cursor.visible = cursorAntes != CursorLockMode.Locked;
        }

        // pausa (o reanuda) todos los sonidos que estén sonando
        AudioListener.pause = congelar;

        inputManager.ActivarControles(congelar == false);
        foreach (Behaviour componente in desactivarEnPausa)
        {
            if (componente != null)
            {
                componente.enabled = (congelar == false);
            }
        }
    }

    // Antes de cambiar de escena dejamos el tiempo, el sonido y los controles como estaban,
    // porque no se reinician solos al cargar otra escena.
    public void CargarEscena(string nombre)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        inputManager.ActivarControles(true);
        SceneManager.LoadScene(nombre);
    }

    // Con el mando solo se puede ir de un botón a otro si hay alguno seleccionado.
    // Si se pierde la selección (por ejemplo, al hacer clic fuera con el ratón)
    // y el jugador usa el mando, volvemos a seleccionar el botón indicado.
    public static void MantenerBotonSeleccionado(GameObject boton)
    {
        if (EventSystem.current == null || Gamepad.current == null)
        {
            return;
        }
        if (EventSystem.current.currentSelectedGameObject == null && Gamepad.current.wasUpdatedThisFrame)
        {
            EventSystem.current.SetSelectedGameObject(boton);
        }
    }
}
