using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// IsoClickMover (v2.6 – exploración con snap‐to‐grid y colliders siempre activos)
///
/// • Mueve un GameObject por una grilla isométrica (Cell Layout = Isometric, Cell Swizzle = XYZ)
///   al hacer clic sobre el Tilemap caminable.
/// • Calcula un path A* (solo 4 direcciones) entre la casilla actual y la casilla clicada.
/// • Interpola suavemente entre centros de casilla con MoveTowards.
/// • En cuanto llega “cerca” a cada destino, hace SnapToCell(...) EXACTO para evitar desfases.
/// • Cuando el líder llega a una nueva casilla, notifica al PartyManager para que los seguidores
///   reproduzcan la fila india (posición en trailCells).
/// • **Los colliders NUNCA se desactivan**, confiamos en la verificación de “IsWalkable”
///   para que no elijan casillas ocupadas y no se bloqueen mutuamente.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CharacterStats))]
public class IsoClickMover : MonoBehaviour
{
    [Header("Referencias de Grilla")]
    [Tooltip("Grid isométrico (Cell Layout = Isometric, Cell Swizzle = XYZ).")]
    public Grid grid;

    [Tooltip("Tilemap que define dónde se puede caminar. Si es null, TODO es transitable.")]
    public Tilemap walkableMap;

    [Header("Velocidad de Exploración")]
    [Tooltip("Velocidad (metros Unity/segundo) al desplazarse de casilla a casilla.")]
    public float moveSpeed = 4f;

    [Header("Party (líder + seguidores)")]
    [Tooltip("PartyManager debe tener en su lista partyMembers este IsoClickMover + sus seguidores.")]
    public PartyManager partyManager;

    // --- Estado interno: pathfinding y fila india ---
    private Vector3Int _currentCell;           // Casilla donde está parado el avatar
    private Queue<Vector3Int> _path = new Queue<Vector3Int>();
    private Vector3 _targetWorld;              // Posición mundial del próximo paso
    private bool _isMoving = false;            // true mientras se interpola
    public List<Vector3Int> trailCells = new List<Vector3Int>(); // Histórico de casillas del líder

    private CharacterStats stats;

    /// <summary>
    /// Celda actual de lectura pública (para PartyManager u otros).
    /// </summary>
    public Vector3Int CurrentCell
    {
        get { return _currentCell; }
    }

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        if (stats == null)
            Debug.LogError("[IsoClickMover] Falta CharacterStats en este GameObject.");
    }

    private void Start()
    {
        // 1) Inicializar _currentCell y hacer snap inmediato
        _currentCell = grid.WorldToCell(transform.position);
        SnapToCell(_currentCell);

        // 2) Llenar trailCells con la posición inicial del líder
        trailCells.Clear();
        trailCells.Add(_currentCell);
    }

    private void Update()
    {
        // ===========================
        //  A) Si estamos interpolando:
        // ===========================
        if (_isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                _targetWorld,
                moveSpeed * Time.deltaTime
            );

            // Si llegamos “cerca” a _targetWorld, hacemos snap exacto
            if (Vector3.Distance(transform.position, _targetWorld) < 0.01f)
            {
                transform.position = grid.GetCellCenterWorld(_currentCell);
                _isMoving = false;

                // Notificar a PartyManager si soy líder y estoy en exploración
                if (partyManager != null && !stats.isInCombat && partyManager.IsLeader(this))
                {
                    partyManager.LeaderArrivedAtCell(_currentCell);
                }
            }
        }
        else
        {
            // ===========================
            //  B) Si no está interpolando 
            //     y hay ruta pendiente:
            // ===========================
            if (_path.Count > 0)
            {
                Vector3Int next = _path.Dequeue();
                _targetWorld = grid.GetCellCenterWorld(next);
                _currentCell = next;
                _isMoving = true;
            }
            else
            {
                // =============================================
                //  C) Si no hay ruta y estamos en exploración,
                //     procesar clic izquierdo
                // =============================================
                if (stats != null && !stats.isInCombat)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        mouseWorld.z = 0f;
                        Vector3Int clickedCell = grid.WorldToCell(mouseWorld);

                        // Si clicqueamos en la misma casilla, nada que hacer
                        if (clickedCell == _currentCell) return;

                        // Comprobar walkableMap (si existe)
                        if (walkableMap != null)
                        {
                            TileBase floor = walkableMap.GetTile(clickedCell);
                            if (floor == null) return; // no caminable
                        }

                        // Calcular ruta A* desde _currentCell hasta clickedCell
                        List<Vector3Int> ruta = FindPath(_currentCell, clickedCell);
                        if (ruta != null && ruta.Count > 0)
                        {
                            _path.Clear();
                            foreach (var paso in ruta)
                                _path.Enqueue(paso);
                        }
                    }
                }
            }
        }

        // =================================================
        //  D) (Opcional) Tecla para detener en seco y hacer snap
        // =================================================
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SnapToCell(_currentCell);
        }
    }

    /// <summary>
    /// SnapToCell(cell):
    ///   • Teletransporta al avatar al centro EXACTO de “cell”.  
    ///   • Actualiza _currentCell y elimina ruta pendiente.
    ///   • Si soy líder en exploración, guarda “cell” en trailCells.
    /// </summary>
    public void SnapToCell(Vector3Int cell)
    {
        Vector3 centro = grid.GetCellCenterWorld(cell);
        transform.position = centro;
        _currentCell = cell;
        _isMoving = false;
        _path.Clear();

        // Si soy líder en exploración, guardar en trailCells
        if (partyManager != null && !stats.isInCombat && partyManager.IsLeader(this))
        {
            trailCells.Insert(0, cell);
            int maxTrail = partyManager.partyMembers.Count + 1;
            if (trailCells.Count > maxTrail)
                trailCells.RemoveRange(maxTrail, trailCells.Count - maxTrail);
        }
    }

    /// <summary>
    /// MoveToCell(targetCell):
    ///   • Calcula ruta A* desde la celda actual hasta targetCell.
    ///   • Si existe una ruta válida, llena _path con cada paso.
    ///   • El Update() se encarga de interpolar y hacer SnapToCell al final de cada paso.
    /// </summary>
    public void MoveToCell(Vector3Int targetCell)
    {
        // Si el personaje está en combate, no moverse
        if (stats != null && stats.isInCombat)
            return;

        // Si es la misma celda, nada que hacer
        if (targetCell == _currentCell)
            return;

        // Comprobar walkableMap (si existe)
        if (walkableMap != null)
        {
            TileBase floor = walkableMap.GetTile(targetCell);
            if (floor == null) return; // destino no es caminable
        }

        // Calcular ruta A* entre _currentCell y targetCell
        List<Vector3Int> ruta = FindPath(_currentCell, targetCell);
        if (ruta != null && ruta.Count > 0)
        {
            _path.Clear();
            foreach (var paso in ruta)
                _path.Enqueue(paso);
        }
        // Si la ruta es nula o vacía, no se mueve.
    }

    /// <summary>
    /// IsMoving(): true si hay pasos pendientes o estamos interpolando.
    /// </summary>
    public bool IsMoving()
    {
        return _isMoving || _path.Count > 0;
    }

    // ================================================
    //      MÉTODOS AUXILIARES DE PATHFINDING (A*)
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

            foreach (Vector3Int neighbor in CardinalNeighbors(current))
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

    private static readonly Vector3Int[] _dirs = new Vector3Int[]
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
    /// IsWalkable(cell):
    ///   • true si (walkableMap == null o hay tile en walkableMap),
    ///     y no hay ningún CharacterStats (jugador o enemigo) en “cell”,
    ///     ni ningún tile en obstacleTilemaps (opcional en exploración).
    /// </summary>
    private bool IsWalkable(Vector3Int cell)
    {
        // 1) Verificar walkableMap
        if (walkableMap != null && walkableMap.GetTile(cell) == null)
            return false;

        // 2) Verificar que no haya CharacterStats distinto de este (para no superponerse)
        CharacterStats[] all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        foreach (CharacterStats cs in all)
        {
            if (cs == null) continue;
            IsoClickMover im = cs.GetComponent<IsoClickMover>();
            CombatMover cm = cs.GetComponent<CombatMover>();
            Vector3Int pos = (im != null && im.enabled)
                             ? im.CurrentCell
                             : (cm != null && cm.enabled ? cm.CurrentCell : new Vector3Int(int.MaxValue, int.MaxValue, 0));
            if (pos == cell && cs != stats)
                return false;
        }

        return true;
    }

    private List<Vector3Int> ReconstructPath(
        Dictionary<Vector3Int, Vector3Int> cameFrom,
        Vector3Int current)
    {
        var path = new List<Vector3Int> { current };
        while (cameFrom.TryGetValue(current, out Vector3Int prev))
        {
            current = prev;
            path.Insert(0, current);
        }
        path.RemoveAt(0);
        return path;
    }

    // ================================================
    //   PRIORITY QUEUE (para A*)
    // ================================================
    private class PriorityQueue<T>
    {
        private List<(T item, int priority)> _data = new List<(T, int)>();
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
                int smallest = (right <= li && _data[right].priority < _data[left].priority) ? right : left;
                if (_data[parent].priority <= _data[smallest].priority) break;
                (_data[parent], _data[smallest]) = (_data[smallest], _data[parent]);
                parent = smallest;
            }
            return front.item;
        }

        public bool Contains(T item)
        {
            return _data.Exists(e => EqualityComparer<T>.Default.Equals(e.item, item));
        }
    }
}
