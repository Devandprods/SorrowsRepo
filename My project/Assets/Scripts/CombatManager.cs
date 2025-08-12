// ⚔️ CombatManager.cs – Controlador de combates por turnos
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Prefabs & UI")]
    [SerializeField] private GameObject movementHighlightPrefab;
    [SerializeField] private Button endTurnButton;

    [Header("Grid & Obstáculos")]
    [SerializeField] private Grid mainGrid;
    [SerializeField] private List<Tilemap> obstacleTilemaps = new();

    [Header("Jugadores y Enemigos")]
    public  List<CharacterStats> playersInCombat = new();
    public  List<CharacterStats> enemiesInCombat = new();
    public  List<CharacterStats> turnOrder = new();

    private int currentTurnIndex = 0;
    private bool inCombat = false;
    private List<GameObject> activeHighlights = new();

    [Header("UI Ataque")]
    [SerializeField] private Button attackButton;
    [SerializeField] private GameObject attackConfirmPanel;
    [SerializeField] private Button confirmAttackButton;
    [SerializeField] private Button cancelAttackButton;

    private CharacterStats selectedTarget;

    private void Awake() => Instance = this;

    private void Start()
    {
        attackButton.onClick.AddListener(OnAttackButtonPressed);
        confirmAttackButton.onClick.AddListener(OnConfirmAttack);
        cancelAttackButton.onClick.AddListener(OnCancelAttack);
        attackConfirmPanel.SetActive(false);

        {
            if (attackButton == null || confirmAttackButton == null || cancelAttackButton == null)
            {
                Debug.LogError("Faltan asignar uno o más botones en CombatManager.");
                return;
            }

            attackButton.onClick.AddListener(OnAttackButtonPressed);
            confirmAttackButton.onClick.AddListener(OnConfirmAttack);
            cancelAttackButton.onClick.AddListener(OnCancelAttack);
            attackConfirmPanel.SetActive(false);
        }

    }

    public void StartCombatWith(CharacterStats e, CharacterStats p)
    {
        if (inCombat) return;
        inCombat = true;

        // Agrega a todos los miembros del PartyManager como jugadores en combate
        playersInCombat.Clear();
        var pm = Object.FindFirstObjectByType<PartyManager>();
        foreach (var mover in pm.partyMembers)
        {
            var cs = mover.GetComponent<CharacterStats>();
            cs.EnterCombat();
            playersInCombat.Add(cs);
        }

        // Encuentra enemigos activos en la escena
        enemiesInCombat.Clear();
        foreach (var cs in FindObjectsByType<CharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (cs.CompareTag("Enemy"))
            {
                cs.EnterCombat();
                enemiesInCombat.Add(cs);
            }
        }

        BuildTurnOrderAndStart();
    }

    public void EndCombat()
    {
        inCombat = false;

        foreach (var cs in playersInCombat) cs.ExitCombat();
        foreach (var cs in enemiesInCombat) cs.ExitCombat();

        playersInCombat.Clear();
        enemiesInCombat.Clear();
        turnOrder.Clear();
        currentTurnIndex = 0;

        foreach (var cs in FindObjectsByType<CharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            cs.turnIndicator?.SetActive(false);

        foreach (var h in activeHighlights)
            Destroy(h);
        activeHighlights.Clear();
    }

    public void OnEnemyDeath(CharacterStats dead)
    {
        enemiesInCombat.Remove(dead);
        turnOrder.Remove(dead);

        // Validación extra: si no quedan enemigos o turnos, terminar combate
        if (enemiesInCombat.Count == 0 || turnOrder.Count == 0)
        {
            Debug.LogWarning("Combate terminado: no quedan enemigos o turnos.");
            EndCombat();
        }
    }

    private void BuildTurnOrderAndStart()
    {
        turnOrder.Clear();
        turnOrder.AddRange(playersInCombat);
        turnOrder.AddRange(enemiesInCombat);

        // Ordena por velocidad descendente
        turnOrder.Sort((a, b) => b.Speed.CompareTo(a.Speed));

        currentTurnIndex = 0;
        StartCoroutine(ProcessTurns());
    }

    private IEnumerator ProcessTurns()
    {
        CharacterStats previous = null;

        while (inCombat)
        {
            if (turnOrder.Count <= 0)
            {
                Debug.LogWarning("[ProcessTurns] turnOrder vacío, terminando combate.");
                EndCombat();
                yield break;
            }

            var actor = turnOrder[currentTurnIndex];

            previous?.turnIndicator?.SetActive(false);
            actor.turnIndicator?.SetActive(true);

            var cm = actor.GetComponent<CombatMover>();
            cm?.SnapToCell(cm.CurrentCell);

            if (actor.CompareTag("Player"))
                yield return StartCoroutine(PlayerTurn(actor));
            else
                yield return StartCoroutine(EnemyTurn(actor));

            actor.turnIndicator?.SetActive(false);
            previous = actor;

            // 🛡️ Protegemos división por cero
            if (turnOrder.Count == 0)
            {
                Debug.LogWarning("¡Turn order quedó vacío después del turno! Terminando combate.");
                EndCombat();
                yield break;
            }

            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            yield return null;
        }

        previous?.turnIndicator?.SetActive(false);
    }

    private IEnumerator PlayerTurn(CharacterStats actor)
    {
        var mover = actor.GetComponent<CombatMover>();
        var start = mover.CurrentCell;
        mover.SnapToCell(start);

        // Calcula todas las celdas a las que puede llegar el jugador
        HashSet<Vector3Int> reachable = ComputeReachableCells(actor, start, actor.MovementRange);

        // Destaca visualmente esas celdas
        foreach (var cell in reachable)
        {
            var pos = mainGrid.GetCellCenterWorld(cell);
            activeHighlights.Add(Instantiate(movementHighlightPrefab, pos, Quaternion.identity));
        }

        bool done = false;
        Vector3Int chosen = start;

        while (!done)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                wp.z = 0f;
                var hit = Physics2D.Raycast(wp, Vector2.zero);

                if (hit.collider != null && hit.collider.CompareTag("Enemy"))
                {
                    var enemyStats = hit.collider.GetComponent<CharacterStats>();
                    var ecell = enemyStats.GetComponent<CombatMover>().CurrentCell;
                    int dist = ManhattanDistance(start, ecell);

                    if (dist <= actor.AttackRange)
                    {
                        Debug.Log($"[PlayerTurn] {actor.name} ataca a {enemyStats.name}");
                        enemyStats.TakeDamage(actor.AttackPower);
                        done = true;
                        break;
                    }
                }

                Vector3Int clickedCell = mainGrid.WorldToCell(wp);
                if (reachable.Contains(clickedCell) && clickedCell != start)
                {
                    chosen = clickedCell;
                    Debug.Log($"[PlayerTurn] {actor.name} se moverá a {chosen}");
                    done = true;
                    break;
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                Debug.Log($"[PlayerTurn] {actor.name} pasa turno");
                done = true;
                break;
            }

            yield return null;
        }

        foreach (var h in activeHighlights) Destroy(h);
        activeHighlights.Clear();

        if (chosen != start)
        {
            mover.GoToCell(chosen);
            while (mover.IsMoving()) yield return null;
        }
    }

    private IEnumerator EnemyTurn(CharacterStats actor)
    {
        var mover = actor.GetComponent<CombatMover>();
        var start = mover.CurrentCell;
        mover.SnapToCell(start);

        CharacterStats best = null;
        int bd = int.MaxValue;

        foreach (var p in playersInCombat)
        {
            var d = ManhattanDistance(start, p.GetComponent<CombatMover>().CurrentCell);
            if (d < bd) { bd = d; best = p; }
        }

        if (best != null)
        {
            var pc = best.GetComponent<CombatMover>().CurrentCell;
            var neigh = new[] { pc + Vector3Int.right, pc + Vector3Int.left, pc + Vector3Int.up, pc + Vector3Int.down };
            Vector3Int chosen = start; int bnd = int.MaxValue;

            foreach (var c in neigh)
            {
                if (IsCellBlockedForEnemy(c)) continue;
                var d = ManhattanDistance(start, c);
                if (d < bnd) { bnd = d; chosen = c; }
            }

            if (chosen != start)
            {
                mover.GoToCell(chosen);
                while (mover.IsMoving()) yield return null;
            }
        }
    }

    public HashSet<Vector3Int> ComputeReachableCells(CharacterStats actor, Vector3Int start, int range)
    {
        var visited = new HashSet<Vector3Int> { start };
        var queue = new Queue<(Vector3Int, int)>();
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (cell, dist) = queue.Dequeue();
            if (dist >= range) continue;

            foreach (var d in new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down })
            {
                var nxt = cell + d;
                if (visited.Contains(nxt)) continue;

                bool tileBlock = false;
                foreach (var t in obstacleTilemaps)
                    if (t.GetTile(nxt) != null) tileBlock = true;
                if (tileBlock) continue;

                bool occupied = false;
                foreach (var cs in FindObjectsByType<CharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    var mv = cs.GetComponent<CombatMover>();
                    var pos = (mv != null && mv.enabled) ? mv.CurrentCell : new Vector3Int(int.MaxValue, int.MaxValue, 0);
                    if (pos == nxt && cs != actor) { occupied = true; break; }
                }

                if (occupied) continue;
                visited.Add(nxt);
                queue.Enqueue((nxt, dist + 1));
            }
        }

        return visited;
    }

    public bool IsCellBlockedForEnemy(Vector3Int cell)
    {
        foreach (var p in playersInCombat)
            if (p.GetComponent<CombatMover>().CurrentCell == cell) return true;

        foreach (var e in enemiesInCombat)
            if (e.GetComponent<CombatMover>().CurrentCell == cell) return true;

        foreach (var t in obstacleTilemaps)
            if (t.GetTile(cell) != null) return true;

        return false;
    }

    public int ManhattanDistance(Vector3Int a, Vector3Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private void OnAttackButtonPressed()
    {
        StartCoroutine(SelectEnemyTarget());
    }

    private IEnumerator SelectEnemyTarget()
    {
        selectedTarget = null;

        while (selectedTarget == null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                wp.z = 0f;
                var hit = Physics2D.Raycast(wp, Vector2.zero);

                if (hit.collider != null && hit.collider.CompareTag("Enemy"))
                {
                    selectedTarget = hit.collider.GetComponent<CharacterStats>();
                    attackConfirmPanel.SetActive(true);
                }
            }

            yield return null;
        }
    }

    private void OnConfirmAttack()
    {
        if (selectedTarget == null) return;

        // ✅ Protege contra objetivo nulo o eliminado del combate
        if (!enemiesInCombat.Contains(selectedTarget))
        {
            Debug.LogWarning("[CombatManager] El objetivo ya no está en combate.");
            attackConfirmPanel.SetActive(false);
            selectedTarget = null;
            return;
        }

        var actor = turnOrder[currentTurnIndex];
        if (actor.CompareTag("Player"))
        {
            var start = actor.GetComponent<CombatMover>().CurrentCell;
            var ecell = selectedTarget.GetComponent<CombatMover>().CurrentCell;
            int dist = ManhattanDistance(start, ecell);

            if (dist <= actor.AttackRange)
            {
                selectedTarget.TakeDamage(actor.AttackPower);
            }
        }

        attackConfirmPanel.SetActive(false);
        selectedTarget = null;
    }

    private void OnCancelAttack()
    {
        attackConfirmPanel.SetActive(false);
        selectedTarget = null;
    }
}
