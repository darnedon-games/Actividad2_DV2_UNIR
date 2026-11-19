using UnityEngine;

// Clase base para todo aquello con lo que el jugador puede interactuar
//
// El jugador solo conoce esta clase: busca el Interactuable que tiene delante,
// lo resalta y, al pulsar interactuar, llama a Interactuar(jugador).
// Se le pasa el jugador por si el objeto necesita saber donde esta
// Cada objeto concreto hereda de aqui

public class Interactuable : MonoBehaviour
{
    // Lo que pasa cuando el jugador pulsa interactuar teniendo este objeto delante
    public virtual void Interactuar(Transform jugador)
    {
    }

    // Se llama con true cuando el jugador lo tiene delante y con false cuando no
    public virtual void Resaltar(bool activar)
    {
    }
}
