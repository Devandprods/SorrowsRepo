using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [SerializeField] private string idUnico = "checkpoint_01"; // ID único para diferenciar los puntos
    [SerializeField] private Transform puntoDeReaparicion;     // Punto exacto donde reaparece el jugador

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica que sea el jugador quien entra
        if (other.CompareTag("Player"))
        {
            // Actualiza el punto de respawn en el sistema global
            RespawnManager.Instance.ActualizarRespawn(idUnico, puntoDeReaparicion.position);
            Debug.Log($"Nuevo punto de respawn activado: {idUnico}");
        }
    }

    // Permite al RespawnManager validar puntos conocidos
    public string GetID() => idUnico;
    public Vector3 GetPosicion() => puntoDeReaparicion.position;
}
