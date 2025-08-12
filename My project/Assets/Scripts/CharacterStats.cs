using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CharacterStats : MonoBehaviour
{
    [Header("Datos Básicos")]
    public int HP = 100;
    public int Speed = 5;
    public int MovementRange = 3;

    [Header("Combate")]
    [Tooltip("Rango de ataque en celdas (Manhattan).")]
    public int AttackRange = 1;
    [Tooltip("Daño base que inflige al atacar.")]
    public int AttackPower = 10;

    [HideInInspector]
    public bool isInCombat = false;

    [Header("Indicador de Turno")]
    public GameObject turnIndicator;

    [HideInInspector] public IsoClickMover explorerMover;
    [HideInInspector] public CombatMover combatMover;

    private SpriteRenderer _sr;
    private void Awake()
    {
        explorerMover = GetComponent<IsoClickMover>();
        combatMover = GetComponent<CombatMover>();
        _sr = GetComponent<SpriteRenderer>();

        if (explorerMover != null) explorerMover.enabled = true;
        if (combatMover != null) combatMover.enabled = false;
        if (turnIndicator != null) turnIndicator.SetActive(false);
    }

    public void EnterCombat()
    {
        isInCombat = true;
        if (explorerMover != null) explorerMover.enabled = false;

        if (combatMover != null)
        {
            Vector3Int cell = explorerMover != null
                ? explorerMover.CurrentCell
                : combatMover.CurrentCell;
            combatMover.SnapToCell(cell);
            combatMover.enabled = true;
        }

        if (turnIndicator != null) turnIndicator.SetActive(false);
    }

    public void ExitCombat()
    {
        isInCombat = false;
        if (combatMover != null) combatMover.enabled = false;

        if (explorerMover != null && combatMover != null)
        {
            Vector3Int cell = combatMover.CurrentCell;
            explorerMover.SnapToCell(cell);
            explorerMover.enabled = true;
        }

        if (turnIndicator != null) turnIndicator.SetActive(false);
    }

    /// <summary>
    /// Aplica daño, hace flash rojo y, si HP ≤ 0, muere.
    /// </summary>
    public void TakeDamage(int amount)
    {
        HP -= amount;
        StartCoroutine(FlashRed());

        if (HP <= 0)
            Die();
    }

    private IEnumerator FlashRed()
    {
        Color original = _sr.color;
        _sr.color = Color.red;
        yield return new WaitForSeconds(1f);
        _sr.color = original;
    }

    private void Die()
    {
        // Notificar al CombatManager
        CombatManager.Instance.OnEnemyDeath(this);
        Destroy(gameObject);
    }
}
