using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores the maze grid. Basically, which cells currently contain a wall.
/// </summary>
public class MazeRuntimeGrid
{
    [System.Serializable]
    public class MazeRow    // allow serializing multi-dimensional array
    {
        [SerializeField] private bool[] cells;
        public MazeRow(int colCount)
        {
            cells = new bool[colCount]; // Default value is false (empty)
        }
        public bool this[int index]
        {
            get => cells[index];
            set => cells[index] = value;
        }
        public int Length => cells.Length;
    }

    [HideInInspector] public MazeRow[] maze; // false = empty, true = wall

    public MazeRuntimeGrid(int rowCount, int colCount)
    {
        maze = new MazeRow[rowCount];
        for (int r = 0; r < rowCount; r++)
        {
            maze[r] = new MazeRow(colCount); // empty by default
        }
    }

    public void AddWall(Vector2Int posId)
    {
        if (IsWithinBounds(posId))
        {
            maze[posId.x][posId.y] = true;
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
            maze[posId.x][posId.y] = false;
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
            return maze[posId.x][posId.y];
        }
        // Out of bounds check => return true (the same as having a wall)
        Debug.LogWarning($"HasWall: Position {posId} is out of bounds. Returning true (treat as wall) by default.");
        return true;
    }

    private bool IsWithinBounds(Vector2Int posId)
    {
        return maze != null &&
               posId.x >= 0 && posId.x < maze.Length &&
               posId.y >= 0 && posId.y < maze[posId.x].Length;
    }

    public MazeRow this[int index]
    {
        get => maze[index];
    }
    public int Length => maze.Length;
}
