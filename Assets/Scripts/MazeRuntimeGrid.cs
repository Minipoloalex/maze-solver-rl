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
        public int[] cells;

        public int this[int index]
        {
            get => cells[index];
            set => cells[index] = value;
        }
        public int Length => cells.Length;
    }

    public MazeRow[] maze;    // maybe in the future, we might want to mark exits

    public void Init(int rowCount, int colCount)   // my init function
    {
        maze = new MazeRow[rowCount];
        for (int r = 0; r < rowCount; r++)
        {
            maze[r] = new MazeRow
            {
                cells = new int[colCount]
            };
        }
    }

    public void AddWall(Vector2Int posId)
    {
        Debug.Log("Heelo");
        Debug.Log(posId);
        Debug.Log(posId.x + " " + posId.y);
        if (maze == null)
        {
            Debug.Log("Maze is unexpectedly null!");
        }
        maze[posId.x][posId.y] = 1;
        Debug.Log("After");
        // optional: raise event → update pathfinding, save-game, etc.
    }

    public void RemoveWall(Vector2Int posId)
    {
        maze[posId.x][posId.y] = 0;
    }

    public bool HasWall(Vector2Int posId) => maze[posId.x][posId.y] == 1;
}
