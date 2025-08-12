using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum TipoHablante
{
    Jugador,
    NPC
}

[System.Serializable]
public struct LineaDialogo
{
    public string nombre;
    public string texto;
    public TipoHablante hablante;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instancia { get; private set; }

    [Header("Caja Jugador")]
    [SerializeField] private GameObject boxPJ;
    [SerializeField] private TMP_Text textoPJ;
    [SerializeField] private TMP_Text nombrePJ;
    [SerializeField] private Image avatarPJ;
    [SerializeField] private CanvasGroup grupoPJ;

    [Header("Caja NPC")]
    [SerializeField] private GameObject boxNPC;
    [SerializeField] private TMP_Text textoNPC;
    [SerializeField] private TMP_Text nombreNPC;
    [SerializeField] private Image avatarNPC;
    [SerializeField] private CanvasGroup grupoNPC;

    [Header("Datos de Personajes Jugables")]
    [SerializeField] private List<DatosPersonaje> personajesJugador = new();

    [Header("Datos de NPCs (opcionales)")]
    [SerializeField] private List<DatosNPC> npcsDefinidos = new();

    [Header("Representación visual de avance")]
    [SerializeField] private Button botonAvanzar;
    [SerializeField] private Animator animadorBoton;

    [Header("Transición visual")]
    [SerializeField] private float duracionTransicion = 0.4f;

    private Dictionary<string, DatosPersonaje> dicJugador;
    private Dictionary<string, DatosNPC> dicNPC;

    private List<LineaDialogo> lineasDialogo = new();  // Lista completa del diálogo actual
    private int indiceActual = 0;                       // Índice de la línea actual
    private LineaDialogo actual;
    private bool esperaPrimerFrame = false;
    public bool dialogoActivo = false;
    public bool EstaDialogoActivo => dialogoActivo;
    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        dicJugador = new();
        foreach (var pj in personajesJugador)
        {
            if (!dicJugador.ContainsKey(pj.nombre))
                dicJugador.Add(pj.nombre, pj);
        }

        dicNPC = new();
        foreach (var npc in npcsDefinidos)
        {
            if (!dicNPC.ContainsKey(npc.nombre))
                dicNPC.Add(npc.nombre, npc);
        }

        boxPJ.SetActive(false);
        boxNPC.SetActive(false);

        if (botonAvanzar != null)
            botonAvanzar.interactable = false;
    }

    private void Update()
    {
        if (esperaPrimerFrame)
        {
            esperaPrimerFrame = false;
            return;
        }

        // Avanzar diálogo con tecla A solo si diálogo activo y alguna caja visible
        if (dialogoActivo && (boxPJ.activeSelf || boxNPC.activeSelf) && Input.GetKeyDown(KeyCode.A))
        {
            SimularPulsado();
            MostrarSiguienteLinea();
        }
    }

    /// <summary>
    /// Inicia el diálogo con el array de líneas dado.
    /// </summary>
    public void MostrarDialogo(LineaDialogo[] lineas)
    {
        if (lineas == null || lineas.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] No se recibieron líneas para mostrar.");
            return;
        }

        lineasDialogo.Clear();
        lineasDialogo.AddRange(lineas);
        indiceActual = 0;
        dialogoActivo = true;

        esperaPrimerFrame = true;
        MostrarLineaPorIndice(indiceActual);

        if (botonAvanzar != null)
            botonAvanzar.interactable = true;
    }

    /// <summary>
    /// Muestra la línea correspondiente al índice dado.
    /// </summary>
    private void MostrarLineaPorIndice(int indice)
    {
        if (indice < 0 || indice >= lineasDialogo.Count)
        {
            Debug.Log("[DialogueManager] Índice fuera de rango, terminando diálogo.");
            CerrarDialogo();
            return;
        }

        actual = lineasDialogo[indice];

        Debug.Log($"[DialogueManager] Mostrando línea {indice + 1}/{lineasDialogo.Count}: {actual.nombre} ({actual.hablante}) → {actual.texto}");

        LimpiarDialogoVisual();

        switch (actual.hablante)
        {
            case TipoHablante.Jugador:
                StartCoroutine(MostrarPJ(actual));
                break;
            case TipoHablante.NPC:
                StartCoroutine(MostrarNPC(actual));
                break;
        }
    }

    /// <summary>
    /// Avanza a la siguiente línea del diálogo.
    /// </summary>
    public void MostrarSiguienteLinea()
    {
        if (!dialogoActivo)
            return;

        indiceActual++;

        if (indiceActual < lineasDialogo.Count)
        {
            MostrarLineaPorIndice(indiceActual);
        }
        else
        {
            Debug.Log("[DialogueManager] Fin del diálogo.");
            CerrarDialogo();
        }
    }

    /// <summary>
    /// Opcional: muestra la línea anterior, si quieres implementar retroceso.
    /// </summary>
    public void MostrarLineaAnterior()
    {
        if (!dialogoActivo)
            return;

        indiceActual = Mathf.Max(indiceActual - 1, 0);
        MostrarLineaPorIndice(indiceActual);
    }

    /// <summary>
    /// Limpia la UI de diálogo para la próxima línea.
    /// </summary>
    private void LimpiarDialogoVisual()
    {
        boxPJ.SetActive(false);
        boxNPC.SetActive(false);

        grupoPJ.alpha = 0;
        grupoNPC.alpha = 0;

        textoPJ.text = "";
        textoNPC.text = "";
        nombrePJ.text = "";
        nombreNPC.text = "";

        avatarPJ.sprite = null;
        avatarNPC.sprite = null;
    }

    private IEnumerator MostrarPJ(LineaDialogo linea)
    {
        grupoNPC.alpha = 0;
        boxNPC.SetActive(false);

        boxPJ.SetActive(true);

        nombrePJ.text = linea.nombre;

        if (dicJugador.ContainsKey(linea.nombre))
        {
            var datos = dicJugador[linea.nombre];
            avatarPJ.sprite = datos.retrato;
            nombrePJ.color = datos.colorNombre;
        }
        else
        {
            avatarPJ.sprite = null;
            nombrePJ.color = Color.white;
        }

        textoPJ.text = linea.texto;

        yield return StartCoroutine(Fade(grupoPJ, 0, 1));
    }

    private IEnumerator MostrarNPC(LineaDialogo linea)
    {
        grupoPJ.alpha = 0;
        boxPJ.SetActive(false);

        boxNPC.SetActive(true);

        nombreNPC.text = linea.nombre;

        if (dicNPC.ContainsKey(linea.nombre))
        {
            var datos = dicNPC[linea.nombre];
            avatarNPC.sprite = datos.retrato;
            nombreNPC.color = datos.colorNombre;
        }
        else
        {
            avatarNPC.sprite = null;
            nombreNPC.color = Color.white;
        }

        textoNPC.text = linea.texto;

        yield return StartCoroutine(Fade(grupoNPC, 0, 1));
    }

    private IEnumerator Fade(CanvasGroup grupo, float desde, float hasta)
    {
        float t = 0f;
        grupo.alpha = desde;

        while (t < duracionTransicion)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracionTransicion);
            yield return null;
        }

        grupo.alpha = hasta;
    }

    private void SimularPulsado()
    {
        if (animadorBoton != null)
            animadorBoton.SetTrigger("Pulsar");
    }

    /// <summary>
    /// Cierra el diálogo y limpia todo.
    /// </summary>
    public void CerrarDialogo()
    {
        dialogoActivo = false;
        indiceActual = 0;
        lineasDialogo.Clear();

        boxPJ?.SetActive(false);
        boxNPC?.SetActive(false);

        textoPJ.text = "";
        textoNPC.text = "";

        nombrePJ.text = "";
        nombreNPC.text = "";

        avatarPJ.sprite = null;
        avatarNPC.sprite = null;

        if (botonAvanzar != null)
            botonAvanzar.interactable = false;

        Debug.Log("[DialogueManager] Diálogo cerrado.");
    }
}
