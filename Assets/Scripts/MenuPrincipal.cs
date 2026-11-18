using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] private string escenaJuego = "GameScene";

    // el botón que se selecciona al usar el mando (normalmente el primero, START)
    [SerializeField] private GameObject botonInicial;

    void Start()
    {
        // por si venimos del juego con el ratón bloqueado, lo dejamos libre para usar el menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Con el mando solo se puede ir de un botón a otro si hay alguno seleccionado.
        // Si no hay ninguno (por ejemplo, porque se ha hecho clic fuera con el ratón)
        // y el jugador usa el mando, seleccionamos botón inicial
        if (EventSystem.current == null || Gamepad.current == null)
        {
            return;
        }
        if (EventSystem.current.currentSelectedGameObject == null && Gamepad.current.wasUpdatedThisFrame)
        {
            EventSystem.current.SetSelectedGameObject(botonInicial);
        }
    }

    // Botón START
    public void Empezar()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    // Botón EXIT: en el editor para el Play; en el juego ya compilado, lo cierra
    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
