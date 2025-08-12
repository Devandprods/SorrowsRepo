using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PartyManager : MonoBehaviour
{
    [Tooltip("Líder en índice 0, luego los seguidores en orden de fila india.")]
    public List<IsoClickMover> partyMembers = new List<IsoClickMover>();

    [Tooltip("Si quieres respetar un Tilemap que marca dónde se puede caminar.")]
    public Tilemap walkableMap;

    /// <summary>
    /// Devuelve true si el IsoClickMover dado es el líder (índice 0).
    /// </summary>
    public bool IsLeader(IsoClickMover mover)
    {
        return partyMembers.Count > 0 && partyMembers[0] == mover;
    }

    /// <summary>
    /// Llamar a este método cuando el líder termina de llegar a una celda nueva.
    ///   – leaderCell = la celda actual del líder (IsoClickMover.CurrentCell).
    ///
    /// Ahora, en lugar de teletransportar a los seguidores, les damos un destino
    /// adyacente libre y llamamos a MoveToCell(...) para que ellos hagan el pathfinding y caminen.
    /// </summary>
    public void LeaderArrivedAtCell(Vector3Int leaderCell)
    {
        if (partyMembers == null || partyMembers.Count <= 1)
            return;

        // 1) Listado de vecinos cardinales de la celda del líder:
        List<Vector3Int> posibles = new List<Vector3Int>()
        {
            new Vector3Int( 1,  0, 0) + leaderCell, // derecha
            new Vector3Int(-1,  0, 0) + leaderCell, // izquierda
            new Vector3Int( 0,  1, 0) + leaderCell, // arriba
            new Vector3Int( 0, -1, 0) + leaderCell  // abajo
        };

        // 2) Filtrar solo las celdas “válidas y libres”:
        List<Vector3Int> libres = new List<Vector3Int>();
        foreach (Vector3Int cell in posibles)
        {
            // 2.a) Si hay walkableMap y no hay tile, no es transitable:
            if (walkableMap != null && walkableMap.GetTile(cell) == null)
                continue;

            // 2.b) Si hay un CharacterStats en esa celda, descartamos:
            if (IsCellOccupied(cell, null))
                continue;

            // Queda libre:
            libres.Add(cell);
        }

        // 3) Mezclar el listado de celdas libres de manera aleatoria:
        ShuffleList(libres);

        // 4) Asignar a cada seguidor la siguiente celda libre, pero pidiéndole que camine:
        int idxLibre = 0;
        for (int i = 1; i < partyMembers.Count; i++)
        {
            IsoClickMover follower = partyMembers[i];
            CharacterStats followerStats = follower.GetComponent<CharacterStats>();

            // Si ya no quedan celdas libres, salimos:
            if (idxLibre >= libres.Count)
                break;

            Vector3Int destino = libres[idxLibre];

            // Antes de ordenar movimiento, asegurarnos de que la casilla sigue libre:
            if (IsCellOccupied(destino, followerStats))
            {
                // Si justo se ocupó, avanzamos al siguiente candidato:
                idxLibre++;
                i--;
                continue;
            }

            // 4.a) EN VEZ DE TELETRANSPORTAR, llamamos a MoveToCell(destino)
            follower.MoveToCell(destino);

            idxLibre++;
        }
        // Los seguidores que queden sin celda libre se quedan en su lugar.
    }

    /// <summary>
    /// Verifica si una celda está ocupada por algún personaje, ignorando al especificado.
    /// Usa el nuevo método recomendado por Unity (FindObjectsByType).
    /// </summary>
    private bool IsCellOccupied(Vector3Int cell, CharacterStats ignorar)
    {
        // Busca todos los objetos activos del tipo CharacterStats (sin incluir los desactivados)
        CharacterStats[] todosLosPersonajes = Object.FindObjectsByType<CharacterStats>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (CharacterStats personaje in todosLosPersonajes)
        {
            if (personaje == null || personaje == ignorar)
                continue;

            // Detecta componente de movimiento en exploración o combate
            IsoClickMover explorador = personaje.GetComponent<IsoClickMover>();
            CombatMover combatiente = personaje.GetComponent<CombatMover>();

            Vector3Int celdaActual;

            if (explorador != null && explorador.enabled)
            {
                celdaActual = explorador.CurrentCell;
            }
            else if (combatiente != null && combatiente.enabled)
            {
                celdaActual = combatiente.CurrentCell;
            }
            else
            {
                continue; // Si no está activo en ninguna forma de movimiento, se ignora
            }

            if (celdaActual == cell)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Mezcla el contenido de la lista de forma aleatoria (Fisher–Yates).
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }
}
