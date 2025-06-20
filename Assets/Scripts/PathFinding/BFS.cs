using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A container for the results of a Breadth-First Search.
/// </summary>
public class BfsResult
{
    /// <summary>
    /// A 2D array where `Distance[r, c]` is the shortest distance from the start to cell (r, c).
    /// A value of -1 indicates the cell is unreachable.
    /// </summary>
    public int[,] Distance { get; }

    /// <summary>
    /// A 2D array where `Parent[r, c]` is the cell from which we first reached cell (r, c).
    /// The start cell's parent is `(-1, -1)`.
    /// </summary>
    public Vector2Int[,] Parent { get; }

    public BfsResult(int[,] distance, Vector2Int[,] parent)
    {
        Distance = distance;
        Parent = parent;
    }

    /// <summary>
    /// Gets the shortest distance from the search's start node to a specified target position.
    /// </summary>
    /// <param name="targetPos">The position of the target cell.</param>
    /// <returns>The distance to the target, or -1 if the target is out of bounds or unreachable.</returns>
    public int GetDistanceTo(Vector2Int targetPos)
    {
        // Check if the target position is within the bounds of the distance array.
        if (targetPos.x >= 0 && targetPos.x < Distance.GetLength(0) &&
            targetPos.y >= 0 && targetPos.y < Distance.GetLength(1))
        {
            return Distance[targetPos.x, targetPos.y];
        }

        // If out of bounds, it's considered unreachable.
        Debug.LogError($"GetDistanceTo: Position {targetPos} is out of bounds.");
        return -1;
    }
}

/// <summary>
/// Contains static methods for pathfinding algorithms on a MazeRuntimeGrid.
/// </summary>
public static class MazePathfinderBFS
{
    /// <summary>
    /// Performs a Breadth-First Search (BFS) on the maze.
    /// This implementation uses 2D arrays for efficient tracking of visited cells, distances, and path parents.
    /// </summary>
    /// <param name="mazeGrid">The maze grid to search on.</param>
    /// <param name="startR">The starting row (x-coordinate).</param>
    /// <param name="startC">The starting column (y-coordinate).</param>
    /// <returns>A BfsResult object containing the distance and parent arrays.</returns>
    public static BfsResult SearchBFS(MazeRuntimeGrid mazeGrid, int startR, int startC)
    {
        Vector2Int startPos = new Vector2Int(startR, startC);

        if (mazeGrid == null || mazeGrid.Length == 0)
        {
            Debug.LogError("BFS Error: Maze grid is null or empty.");
            return new BfsResult(new int[0, 0], new Vector2Int[0, 0]);
        }

        int rowCount = mazeGrid.Length;
        int colCount = mazeGrid[0].Length;

        // Initialize the data structures to be returned.
        var distance = new int[rowCount, colCount];
        var parent = new Vector2Int[rowCount, colCount];
        var visited = new bool[rowCount, colCount];

        for (int r = 0; r < rowCount; r++)
        {
            for (int c = 0; c < colCount; c++)
            {
                distance[r, c] = -1; // -1 represents infinity/unvisited
                visited[r, c] = false;
                parent[r, c] = new Vector2Int(-1, -1); // Represents no parent
            }
        }

        // Check if the starting position is valid.
        if (mazeGrid.IsWall(startPos))
        {
            Debug.LogWarning($"BFS cannot start at ({startR}, {startC}) because it's a wall.");
            return new BfsResult(distance, parent); // Return the initialized (empty) result.
        }

        // The queue for cells to visit. Stores positions directly.
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Initialize with the starting position.
        distance[startPos.x, startPos.y] = 0;
        visited[startPos.x, startPos.y] = true;
        // parent[startPos.x, startPos.y] is already (-1, -1), which is correct for the start node.
        queue.Enqueue(startPos);

        Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.down, Vector2Int.up };

        // Process the queue until it's empty.
        while (queue.Count > 0)
        {
            Vector2Int currentPos = queue.Dequeue();

            // Explore neighbors.
            foreach (var dir in directions)
            {
                Vector2Int neighborPos = currentPos + dir;

                // Check if the neighbor is valid and unvisited.
                // IsWall handles bounds checks.
                if (!mazeGrid.IsWall(neighborPos) && !visited[neighborPos.x, neighborPos.y])
                {
                    visited[neighborPos.x, neighborPos.y] = true;
                    distance[neighborPos.x, neighborPos.y] = distance[currentPos.x, currentPos.y] + 1;
                    parent[neighborPos.x, neighborPos.y] = currentPos;
                    queue.Enqueue(neighborPos);
                }
            }
        }

        return new BfsResult(distance, parent);
    }

    /// <summary>
    /// Reconstructs the path from start to end using the parent array from a BfsResult.
    /// </summary>
    /// <param name="parent">The 2D parent array from the BfsResult.</param>
    /// <param name="startPos">The starting position of the search.</param>
    /// <param name="endPos">The destination position.</param>
    /// <returns>A list of positions representing the path, or an empty list if no path exists.</returns>
    public static List<Vector2Int> ReconstructPath(Vector2Int[,] parent, Vector2Int startPos, Vector2Int endPos)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        // Check if a path exists by seeing if the end position has a parent.
        if (parent[endPos.x, endPos.y] == new Vector2Int(-1, -1) && endPos != startPos)
        {
            return path; // No path found, return empty list.
        }

        Vector2Int current = endPos;
        while (current != startPos)
        {
            path.Add(current);
            // Check for the sentinel value to prevent infinite loops if startPos is unreachable
            if (parent[current.x, current.y] == new Vector2Int(-1, -1))
            {
                return new List<Vector2Int>(); // Path doesn't lead back to start, something is wrong
            }
            current = parent[current.x, current.y];
        }
        path.Add(startPos);
        path.Reverse();

        return path;
    }
}
