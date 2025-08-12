using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource fuenteMusicaA;
    [SerializeField] private AudioSource fuenteMusicaB;
    [SerializeField] private AudioSource fuenteAmbiente;

    private bool usandoFuenteA = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Cambia la música y el ambiente según la zona con transiciones suaves
    /// </summary>
    public void CambiarZona(AudioClip nuevaMusica, float volumenMusica, AudioClip nuevoAmbiente, float volumenAmbiente)
    {
        // Cambiar música con crossfade
        StartCoroutine(FadeMusica(nuevaMusica, volumenMusica));

        // Cambiar ambiente (más simple)
        fuenteAmbiente.clip = nuevoAmbiente;
        fuenteAmbiente.volume = volumenAmbiente;
        fuenteAmbiente.Play();
    }

    private System.Collections.IEnumerator FadeMusica(AudioClip nuevaMusica, float volumenDestino)
    {
        AudioSource fuenteActual = usandoFuenteA ? fuenteMusicaA : fuenteMusicaB;
        AudioSource nuevaFuente = usandoFuenteA ? fuenteMusicaB : fuenteMusicaA;

        nuevaFuente.clip = nuevaMusica;
        nuevaFuente.volume = 0f;
        nuevaFuente.Play();

        float duracion = 2f; // segundos
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;
            fuenteActual.volume = Mathf.Lerp(volumenDestino, 0f, t);
            nuevaFuente.volume = Mathf.Lerp(0f, volumenDestino, t);
            yield return null;
        }

        fuenteActual.Stop();
        usandoFuenteA = !usandoFuenteA;
    }

    public void AjustarVolumen(string parametro, float volumenDecibel)
    {
        mixer.SetFloat(parametro, volumenDecibel);
    }
}