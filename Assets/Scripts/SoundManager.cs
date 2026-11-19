using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private AudioSource efectos;
    [SerializeField] AudioClip sonidoAparicion;
    [SerializeField] AudioClip sonidoPuerta;
    [SerializeField] AudioClip sonidoLlanto;

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

    public void ReproducirSonidoLlanto()
    {
        efectos.PlayOneShot(sonidoLlanto);
    }
}