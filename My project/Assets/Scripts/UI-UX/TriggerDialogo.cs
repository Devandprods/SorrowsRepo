using UnityEngine;

public class TriggerDialogo : MonoBehaviour
{
    [SerializeField] private LineaDialogo[] lineasDeDialogo;
    [SerializeField] private GameObject iconoInteraccion;

    private bool jugadorEnRango = false;
    private bool dialogoActivo = false;

    private void Update()
    {
        if (jugadorEnRango && Input.GetKeyDown(KeyCode.A) && !dialogoActivo)
        {
            if (DialogueManager.Instancia != null && DialogueManager.Instancia.gameObject != null)
            {
                DialogueManager.Instancia.MostrarDialogo(lineasDeDialogo);
                dialogoActivo = true;
            }
            else
            {
                Debug.LogWarning("No se encontró DialogueManager activo en escena.");
            }
        }

        if (dialogoActivo && !DialogueEstaVisible())
        {
            dialogoActivo = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnRango = true;
            if (iconoInteraccion != null)
                iconoInteraccion.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnRango = false;
            if (iconoInteraccion != null)
                iconoInteraccion.SetActive(false);

            if (DialogueManager.Instancia != null && DialogueManager.Instancia.gameObject != null)
            {
                DialogueManager.Instancia.CerrarDialogo();
            }

            dialogoActivo = false;
        }
    }

    private bool DialogueEstaVisible()
    {
        if (DialogueManager.Instancia == null)
            return false;

        return DialogueManager.Instancia.EstaDialogoActivo;
    }
}