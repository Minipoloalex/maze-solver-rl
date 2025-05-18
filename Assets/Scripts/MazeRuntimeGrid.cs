using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores which cells currently contain a wall.
/// </summary>
[RequireComponent(typeof(MazeFromAscii))]
public class MazeRuntimeGrid : MonoBehaviour
{
    [System.Serializable]
    public class MazeRow    // allow serializing multi-dimensional array
    {
        [SerializeField] private int[] cells;
        public MazeRow(int colCount)
        {
            cells = new int[colCount];
        }
        public int this[int index]
        {
            get => cells[index];
            set => cells[index] = value;
        }
        public int Length => cells.Length;
    }

    [HideInInspector] public MazeRow[] maze;    // we might want to mark exits in the future

    public void Init(int rowCount, int colCount)   // my init function
    {
        maze = new MazeRow[rowCount];
        for (int r = 0; r < rowCount; r++)
        {
            maze[r] = new MazeRow(colCount);
        }
    }

    public void AddWall(Vector2Int posId)
    {
        maze[posId.x][posId.y] = 1;
        // optional: raise event → update pathfinding, save-game, etc.
    }

    public void RemoveWall(Vector2Int posId)
    {
        maze[posId.x][posId.y] = 0;
    }

    public bool HasWall(Vector2Int posId) => maze[posId.x][posId.y] == 1;
}
