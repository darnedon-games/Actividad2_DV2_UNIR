using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private AudioSource efectos;
    [SerializeField] AudioClip sonidoAparicion;
    [SerializeField] AudioClip sonidoPuerta;

    void Start()
    {
        efectos = GetComponent<AudioSource>();
    }

    public void ReproducirSonidoAparicion()
    {
        efectos.PlayOneShot(sonidoAparicion);
    }

    public void ReproducirSonidoPuerta()
    {
        efectos.PlayOneShot(sonidoPuerta);
    }
}