using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Menú de victoria: lo abre TriggerVictoria cuando el jugador llega a la salida.
// Congela el juego (usando el menú de pausa) y ya no deja abrir la pausa.
// Los botones del panel llaman a Reiniciar() y SalirAlMenu().
public class MenuVictoria : MonoBehaviour
{
    [SerializeField] private GameObject panelVictoria;
    // el botón que se selecciona al abrir el menú (para poder usarlo con el mando)
    [SerializeField] private GameObject botonInicial;
    [SerializeField] private MenuPausa menuPausa;
    [SerializeField] private string escenaMenu = "Menu";

    private bool mostrado = false;

    void Start()
    {
        panelVictoria.SetActive(false);
    }

    void Update()
    {
        if (mostrado)
        {
            MenuPausa.MantenerBotonSeleccionado(botonInicial);
        }
    }

    public void Mostrar()
    {
        if (mostrado)
        {
            return;
        }
        mostrado = true;

        // ya no se puede pausar: el juego ha terminado
        menuPausa.enabled = false;
        menuPausa.CongelarJuego(true);

        panelVictoria.SetActive(true);
        EventSystem.current.SetSelectedGameObject(botonInicial);
    }

    // Botón RESTART: vuelve a cargar la escena en la que estamos
    public void Reiniciar()
    {
        menuPausa.CargarEscena(SceneManager.GetActiveScene().name);
    }

    // Botón EXIT MENU
    public void SalirAlMenu()
    {
        menuPausa.CargarEscena(escenaMenu);
    }
}
