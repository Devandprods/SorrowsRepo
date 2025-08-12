using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CombatMover), typeof(CharacterStats))]
public class EnemyCombatAI : MonoBehaviour
{
    [HideInInspector] public CombatMover mover;
    [HideInInspector] public CharacterStats stats;
    private CombatManager manager;
    private List<CharacterStats> players;

    private void Awake()
    {
        mover = GetComponent<CombatMover>();
        stats = GetComponent<CharacterStats>();
        manager = CombatManager.Instance;
    }

    /// <summary>
    /// Se llama desde CombatManager al inicio del turno del enemigo.
    ///   • SnapToCell, buscar jugador más cercano, elegir celda adyacente libre,
    ///     GoToCell(...) y esperar a IsMoving()==false.
    ///   • Luego notificar fin de turno.
    /// </summary>
    public IEnumerator ExecuteTurn()
    {
        // 1) Alinear
        mover.SnapToCell(mover.CurrentCell);

        // 2) Encontrar jugador más cercano
        players = manager.playersInCombat;
        CharacterStats target = null;
        int bestDist = int.MaxValue;
        foreach (var p in players)
        {
            if (p == null) continue;
            int d = manager.ManhattanDistance(mover.CurrentCell, p.GetComponent<CombatMover>().CurrentCell);
            if (d < bestDist) { bestDist = d; target = p; }
        }

        // 3) Si encontramos objetivo
        if (target != null)
        {
            Vector3Int pc = target.GetComponent<CombatMover>().CurrentCell;
            Vector3Int[] neigh = new Vector3Int[] {
                pc + Vector3Int.right,
                pc + Vector3Int.left,
                pc + Vector3Int.up,
                pc + Vector3Int.down
            };

            // 4) Elegir adyacente libre y más cercano
            Vector3Int chosen = mover.CurrentCell;
            int bestN = int.MaxValue;
            foreach (var c in neigh)
            {
                if (manager.IsCellBlockedForEnemy(c)) continue;
                int dd = manager.ManhattanDistance(mover.CurrentCell, c);
                if (dd < bestN) { bestN = dd; chosen = c; }
            }

            // 5) Mover si cambió
            if (chosen != mover.CurrentCell)
            {
                mover.GoToCell(chosen);
                while (mover.IsMoving()) yield return null;
            }
        }

        yield break;
    }
}
