using UnityEngine;

public class ZonaAudio : MonoBehaviour
{
    [SerializeField] private AudioClip clipDeMusica;
    [SerializeField] private AudioClip clipDeAmbiente;

    [SerializeField] private float volumenMusica = 0.8f;
    [SerializeField] private float volumenAmbiente = 0.8f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            AudioManager.Instance.CambiarZona(
                clipDeMusica, volumenMusica,
                clipDeAmbiente, volumenAmbiente
            );
        }
    }
}