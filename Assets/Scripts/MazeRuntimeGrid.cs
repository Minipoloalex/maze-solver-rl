using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores the maze grid. Basically, which cells currently contain a wall.
/// </summary>
[RequireComponent(typeof(AsciiMazeLoader))]
public class MazeRuntimeGrid : MonoBehaviour
{
    [System.Serializable]
    public class MazeRow    // allow serializing multi-dimensional array
    {
        [SerializeField] private int[] cells;
        public MazeRow(int colCount)
        {
            cells = new int[colCount]; // Default int value is 0 (empty)
        }
        public int this[int index]
        {
            get => cells[index];
            set => cells[index] = value;
        }
        public int Length => cells.Length;
    }

    [HideInInspector] public MazeRow[] maze;    // 0 = empty, 1 = wall

    public void Init(int rowCount, int colCount)
    {
        maze = new MazeRow[rowCount];
        for (int r = 0; r < rowCount; r++)
        {
            maze[r] = new MazeRow(colCount);    // empty by default
        }
    }

    public void AddWall(Vector2Int posId)
    {
        if (IsWithinBounds(posId))
        {
            maze[posId.x][posId.y] = 1;
        }
        else
        {
            Debug.LogWarning($"AddWall: Position {posId} is out of bounds.");
        }
    }

    public void RemoveWall(Vector2Int posId)
    {
        if (IsWithinBounds(posId))
        {
            maze[posId.x][posId.y] = 0;
        }
        else
        {
            Debug.LogWarning($"RemoveWall: Position {posId} is out of bounds.");
        }
    }

    public bool HasWall(Vector2Int posId)
    {
        if (IsWithinBounds(posId))
        {
            return maze[posId.x][posId.y] == 1;
        }
        Debug.LogWarning($"HasWall: Position {posId} is out of bounds. Returning true (treat as wall) by default.");
        return true; // Or false, depending on desired behavior for out-of-bounds checks
    }

    private bool IsWithinBounds(Vector2Int posId)
    {
        return maze != null && posId.x >= 0 && posId.x < maze.Length && posId.y >= 0 && posId.y < maze[posId.x].Length;
    }
}
