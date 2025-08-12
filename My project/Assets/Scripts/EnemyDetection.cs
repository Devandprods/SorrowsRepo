using UnityEngine;

/// <summary>
/// EnemyDetection:
///   • Detecta al jugador cuando entra en la zona de trigger (CircleCollider2D con IsTrigger).
///   • Llama a CombatManager.Instance.StartCombatWith() para iniciar la fase de combate.
///   • Se asegura de que el combate solo se inicie una vez (flag interno).
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class EnemyDetection : MonoBehaviour
{
    private bool hasTriggered = false;             // Para que no vuelva a disparar varias veces
    private CharacterStats enemyStats;

    private void Awake()
    {
        enemyStats = GetComponent<CharacterStats>();
        if (enemyStats == null)
            Debug.LogError("[EnemyDetection] Falta CharacterStats en este GameObject.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo disparar una vez y solo para objetos con tag "Player"
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        // Obtener CharacterStats del jugador
        CharacterStats playerStats = other.GetComponent<CharacterStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("[EnemyDetection] El objeto con tag 'Player' no tiene CharacterStats.");
            return;
        }

        // Iniciar combate
        CombatManager.Instance.StartCombatWith(enemyStats, playerStats);
        hasTriggered = true;
    }

    // (Opcional) resetea la detección si el combate termina y quieres reactivar esta zona:
    private void OnCombatEnded()
    {
        hasTriggered = false;
    }
}
