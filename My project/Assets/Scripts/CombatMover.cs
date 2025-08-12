using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// CombatMover (v2.6 – movimiento en combate con snap-to-grid y sin desactivar colliders)
///
/// • Se usa únicamente durante el combate (IsoClickMover está deshabilitado).
/// • OnEnable(): SnapToCell para alinear inmediatamente al centro de la celda actual.
/// • GoToCell(cell): calcula A* 4-direccional (sin diagonales), evitando tiles en obstacleTilemaps
///   y celdas ocupadas por cualquier otro CharacterStats en combate, y encola los pasos.
/// • Update(): 
///     – Si _isMoving == true, interpola hacia _targetWorld; al llegar “cerca”, hace SnapToCell exacto.
///     – Si !_isMoving y hay pasos en _path, desempila el siguiente y vuelve a interpolar.
/// • Los colliders permanecen siempre activos: la lógica de IsWalkable evita superposiciones.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class CombatMover : MonoBehaviour
{
    [Header("Grilla Isométrica")]
    [Tooltip("Grid Isométrico (Cell Layout = Isometric, Cell Swizzle = XYZ).")]
    public Grid grid;

    [Tooltip("Tilemaps con muros/rocas que bloquean el movimiento en combate.")]
    public List<Tilemap> obstacleTilemaps = new List<Tilemap>();

    [Header("Velocidad de Movimiento")]
    [Tooltip("Unidades Unity por segundo al moverse de celda a celda.")]
    public float moveSpeed = 4f;

    // --- Estado interno ---
    private Vector3Int _currentCell;               // Celda actual del avatar
    private Queue<Vector3Int> _path = new Queue<Vector3Int>();
    private Vector3 _targetWorld;                  // Posición mundial del próximo paso
    private bool _isMoving = false;                // True mientras interpola
    private CharacterStats stats;                  // Referencia a CharacterStats

    /// <summary>
    /// Celda en la que actualmente está el avatar.
    /// </summary>
    public Vector3Int CurrentCell => _currentCell;

    /// <summary>
    /// Devuelve true si la unidad aún tiene pasos pendientes o está interpolando.
    /// </summary>
    public bool IsMoving() => _isMoving || _path.Count > 0;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        if (stats == null)
            Debug.LogError("[CombatMover] Falta CharacterStats en este GameObject.");
    }

    private void OnEnable()
    {
        // Al activarse en combate, alineamos inmediatamente a la grilla
        _currentCell = grid.WorldToCell(transform.position);
        SnapToCell(_currentCell);
    }

    private void Update()
    {
        // Si estamos interpolando, avanzamos hacia el objetivo
        if (_isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                _targetWorld,
                moveSpeed * Time.deltaTime
            );

            // Al llegar “cerca” al destino, hacemos snap exacto
            if (Vector3.Distance(transform.position, _targetWorld) < 0.001f)
            {
                transform.position = grid.GetCellCenterWorld(_currentCell);
                _isMoving = false;
            }
        }
        else if (_path.Count > 0)
        {
            // Si hay ruta pendiente, sacamos el siguiente paso
            Vector3Int next = _path.Dequeue();
            _targetWorld = grid.GetCellCenterWorld(next);
            _currentCell = next;
            _isMoving = true;
        }
    }

    /// <summary>
    /// GoToCell(targetCell):
    ///   • Si targetCell == _currentCell, no hace nada.
    ///   • Calcula ruta A* 4-direccional entre _currentCell y targetCell,
    ///     evitando obstacleTilemaps y celdas ocupadas por otros CharacterStats.
    ///   • Encola los pasos (sin incluir la celda de partida).
    /// </summary>
    public void GoToCell(Vector3Int targetCell)
    {
        if (targetCell == _currentCell) return;

        List<Vector3Int> ruta = FindPath(_currentCell, targetCell);
        if (ruta == null || ruta.Count == 0) return;

        _path.Clear();
        foreach (var paso in ruta)
            _path.Enqueue(paso);
    }

    /// <summary>
    /// SnapToCell(cell):
    ///   • Teletransporta instantáneamente al centro exacto de la celda.
    ///   • Actualiza _currentCell y borra cualquier ruta pendiente.
    /// </summary>
    public void SnapToCell(Vector3Int cell)
    {
        Vector3 worldPos = grid.GetCellCenterWorld(cell);
        transform.position = worldPos;
        _currentCell = cell;
        _isMoving = false;
        _path.Clear();
    }

    // ================================================
    //      MÉTODOS DE PATHFINDING (A*)
    // ================================================

    private List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        var openSet = new PriorityQueue<Vector3Int>();
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector3Int, int> { [start] = Heuristic(start, goal) };

        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            Vector3Int current = openSet.Dequeue();
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            foreach (var neighbor in CardinalNeighbors(current))
            {
                if (!IsWalkable(neighbor)) continue;

                int tentativeG = gScore[current] + 1;
                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return null;
    }

    private static int Heuristic(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static readonly Vector3Int[] _dirs = new[]
    {
        new Vector3Int( 1,  0, 0),
        new Vector3Int(-1,  0, 0),
        new Vector3Int( 0,  1, 0),
        new Vector3Int( 0, -1, 0),
    };
    private IEnumerable<Vector3Int> CardinalNeighbors(Vector3Int cell)
    {
        foreach (var d in _dirs) yield return cell + d;
    }

    /// <summary>
    /// True si la casilla no está bloqueada por un tile de obstáculo
    /// y no está ocupada por otro CharacterStats.
    /// </summary>
    private bool IsWalkable(Vector3Int cell)
    {
        // Verificar obstáculos estáticos
        foreach (var tmap in obstacleTilemaps)
            if (tmap != null && tmap.GetTile(cell) != null)
                return false;

        // Verificar ocupación por otras unidades
        foreach (var cs in FindObjectsByType<CharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var mover = cs.GetComponent<CombatMover>();
            if (mover != null && mover.CurrentCell == cell && mover != this)
                return false;
        }

        return true;
    }

    private List<Vector3Int> ReconstructPath(
        Dictionary<Vector3Int, Vector3Int> cameFrom,
        Vector3Int current)
    {
        var path = new List<Vector3Int> { current };
        while (cameFrom.TryGetValue(current, out var prev))
        {
            current = prev;
            path.Insert(0, current);
        }
        path.RemoveAt(0);
        return path;
    }

    // ====================================
    //    PRIORITY QUEUE (para A*)
    // ====================================
    private class PriorityQueue<T>
    {
        private List<(T item, int priority)> _data = new();
        public int Count => _data.Count;

        public void Enqueue(T item, int priority)
        {
            _data.Add((item, priority));
            int c = _data.Count - 1;
            while (c > 0)
            {
                int p = (c - 1) / 2;
                if (_data[c].priority >= _data[p].priority) break;
                (_data[c], _data[p]) = (_data[p], _data[c]);
                c = p;
            }
        }

        public T Dequeue()
        {
            int li = _data.Count - 1;
            var front = _data[0];
            _data[0] = _data[li];
            _data.RemoveAt(li);
            li--;
            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                if (left > li) break;
                int right = left + 1;
                int smallest = (right <= li && _data[right].priority < _data[left].priority)
                    ? right
                    : left;
                if (_data[parent].priority <= _data[smallest].priority) break;
                (_data[parent], _data[smallest]) = (_data[smallest], _data[parent]);
                parent = smallest;
            }
            return front.item;
        }

        public bool Contains(T item)
            => _data.Exists(e => EqualityComparer<T>.Default.Equals(e.item, item));
    }
}
