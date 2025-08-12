using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI; // Necesario para manejar el Slider
using System;

public class CicloDiaNoche : MonoBehaviour
{
    public static CicloDiaNoche Instancia { get; private set; }
    public static event Action<FaseDia> AlCambiarFase;

    [Header("🔆 Luz Global 2D")]
    [SerializeField] private Light2D luzGlobal;

    [Header("🎚️ UI - Slider de Transición Día/Noche")]
    [SerializeField] private Slider sliderTransicion; // Referencia al Slider de UI

    [Header("⏱️ Duración de cada fase (segundos)")]
    [SerializeField] private float duracionManana = 15f;
    [SerializeField] private float duracionDia = 15f;
    [SerializeField] private float duracionTarde = 15f;
    [SerializeField] private float duracionNoche = 15f;

    [Header("🎨 Colores")]
    [SerializeField] private Color colorManana = new Color(1f, 0.85f, 0.6f);
    [SerializeField] private Color colorDia = Color.white;
    [SerializeField] private Color colorTarde = new Color(1f, 0.5f, 0.3f);
    [SerializeField] private Color colorNoche = new Color(0.1f, 0.1f, 0.3f);

    [Header("💡 Intensidad")]
    [SerializeField] private float intensidadDia = 1f;
    [SerializeField] private float intensidadNoche = 0.2f;

    private float tiempoActual;
    private float duracionTotal;

    private FaseDia faseActual;
    private FaseDia faseAnterior;

    public FaseDia FaseActual => faseActual;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    private void Start()
    {
        duracionTotal = duracionManana + duracionDia + duracionTarde + duracionNoche;
        faseActual = CalcularFase(0f);
        faseAnterior = faseActual;

        // Configura el slider si fue asignado
        if (sliderTransicion != null)
        {
            sliderTransicion.minValue = 0f;
            sliderTransicion.maxValue = duracionTotal;
            sliderTransicion.interactable = false; // Garantiza que no sea modificable
        }
    }

    private void Update()
    {
        tiempoActual += Time.deltaTime;
        float tiempoEnCiclo = tiempoActual % duracionTotal;

        FaseDia faseAnteriorFrame = faseActual;
        faseActual = CalcularFase(tiempoEnCiclo);
        ActualizarLuz(tiempoEnCiclo);

        // Actualizar el slider de forma representativa
        if (sliderTransicion != null)
        {
            // Reinicio visual del slider al pasar de noche a mañana
            if (faseAnteriorFrame == FaseDia.Noche && faseActual == FaseDia.Manana)
                sliderTransicion.SetValueWithoutNotify(0f);
            else
                sliderTransicion.SetValueWithoutNotify(tiempoEnCiclo);
        }

        // Emitir evento si la fase cambia
        if (faseActual != faseAnterior)
        {
            AlCambiarFase?.Invoke(faseActual);
            faseAnterior = faseActual;
        }
    }

    private FaseDia CalcularFase(float tiempo)
    {
        if (tiempo < duracionManana) return FaseDia.Manana;
        if (tiempo < duracionManana + duracionDia) return FaseDia.Dia;
        if (tiempo < duracionManana + duracionDia + duracionTarde) return FaseDia.Tarde;
        return FaseDia.Noche;
    }

    private void ActualizarLuz(float tiempo)
    {
        Color colorActual = colorDia;
        float intensidad = intensidadDia;

        if (faseActual == FaseDia.Manana)
        {
            float t = Smooth(tiempo / duracionManana);
            colorActual = Color.Lerp(colorNoche, colorManana, t);
            intensidad = Mathf.Lerp(intensidadNoche, intensidadDia * 0.6f, t);
        }
        else if (faseActual == FaseDia.Dia)
        {
            colorActual = colorDia;
            intensidad = intensidadDia;
        }
        else if (faseActual == FaseDia.Tarde)
        {
            float t = Smooth((tiempo - duracionManana - duracionDia) / duracionTarde);
            colorActual = Color.Lerp(colorDia, colorTarde, t);
            intensidad = Mathf.Lerp(intensidadDia, intensidadDia * 0.5f, t);
        }
        else if (faseActual == FaseDia.Noche)
        {
            float t = Smooth((tiempo - duracionManana - duracionDia - duracionTarde) / duracionNoche);
            colorActual = Color.Lerp(colorTarde, colorNoche, t);
            intensidad = Mathf.Lerp(intensidadDia * 0.5f, intensidadNoche, t);
        }

        luzGlobal.color = colorActual;
        luzGlobal.intensity = intensidad;
    }

    private float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
}