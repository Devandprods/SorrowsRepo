using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [SerializeField] private string idRespawnActual = "";
    [SerializeField] private Vector3 posicionRespawn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        CargarRespawn();
    }

    /// <summary>
    /// Actualiza el punto de respawn activo
    /// </summary>
    public void ActualizarRespawn(string nuevoID, Vector3 nuevaPosicion)
    {
        idRespawnActual = nuevoID;
        posicionRespawn = nuevaPosicion;

        // Guardar para sesiones futuras
        PlayerPrefs.SetString("Respawn_ID", idRespawnActual);
        PlayerPrefs.SetFloat("Respawn_X", posicionRespawn.x);
        PlayerPrefs.SetFloat("Respawn_Y", posicionRespawn.y);
        PlayerPrefs.SetFloat("Respawn_Z", posicionRespawn.z);
    }

    /// <summary>
    /// Teletransporta al jugador al punto de respawn
    /// </summary>
    public void ReaparecerJugador(GameObject jugador)
    {
        jugador.transform.position = posicionRespawn;
    }

    /// <summary>
    /// Carga el último respawn guardado
    /// </summary>
    private void CargarRespawn()
    {
        if (PlayerPrefs.HasKey("Respawn_ID"))
        {
            idRespawnActual = PlayerPrefs.GetString("Respawn_ID");
            float x = PlayerPrefs.GetFloat("Respawn_X");
            float y = PlayerPrefs.GetFloat("Respawn_Y");
            float z = PlayerPrefs.GetFloat("Respawn_Z");
            posicionRespawn = new Vector3(x, y, z);
        }
    }
}