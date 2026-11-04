using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "InputManager")]
public class InputManagerSO : ScriptableObject
{
    Controls misControles;
    public event Action OnSaltar;
    public event Action<Vector2> OnMover;
    public event Action OnInteractuar;
    private void OnEnable()
    {
        misControles = new Controls();
        misControles.Gameplay.Enable();
        misControles.Gameplay.Saltar.started += Saltar;
        misControles.Gameplay.Mover.performed += Mover;
        misControles.Gameplay.Mover.canceled += Mover;
        misControles.Gameplay.Interactuar.started += Interactuar;
    }
    
    private void OnDisable()
    {
        misControles.Gameplay.Saltar.started -= Saltar;
        misControles.Gameplay.Mover.performed -= Mover;
        misControles.Gameplay.Mover.canceled -= Mover;
        misControles.Gameplay.Interactuar.started -= Interactuar;
        misControles.Gameplay.Disable();
    }

    private void Mover(InputAction.CallbackContext ctx)
    {
        OnMover?.Invoke(ctx.ReadValue<Vector2>());
    }
    
    private void Saltar(InputAction.CallbackContext ctx)
    {
        OnSaltar?.Invoke();
    }

    private void Interactuar(InputAction.CallbackContext ctx)
    {
        OnInteractuar?.Invoke();
    }
}
