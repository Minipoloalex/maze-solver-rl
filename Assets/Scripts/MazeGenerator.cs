using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a 2‑D rectangular maze using depth‑first search (recursive back‑tracker) and
/// optionally increases the branching factor / loopiness according to a 0–1 <c>difficulty</c> value.
///
/// A value of 0 produces an almost straight corridor; 1 produces a very loopy labyrinth.
/// The generator is deterministic for the same <c>seed</c> so you can replay episodes.
///
/// Cell indices: (0,0) is bottom‑left, +X (columns) to the right, +Y (rows) upwards.
/// (Internally, we use x for columns, y for rows in the 2D array for convention with grid[width, height])
/// The resulting int[,] grid stores bit‑flags per cell:
/// 1 = wall on +X (East)
/// 2 = wall on +Y (North)  <- Changed from Z for consistency if using 2D array indices
/// 4 = wall on -X (West)
/// 8 = wall on -Y (South)  <- Changed from Z
/// With this convention neighbouring cells share the same edge bit.
/// </summary>
public static class MazeGenerator
{
    [Flags]
    public enum Wall : byte // Made public for easier use by other classes if needed
    {
        None = 0, // Useful for clarity
        East = 1,  // +X
        North = 2, // +Y
        West = 4,  // -X
        South = 8, // -Y
        All = East | North | West | South
    }

    // DIRS[0] is East, DIRS[1] is North, DIRS[2] is West, DIRS[3] is South
    private static readonly Vector2Int[] DIRS =
    {
        new Vector2Int(1, 0),  // East (+X)
        new Vector2Int(0, 1),  // North (+Y)
        new Vector2Int(-1,0),  // West (-X)
        new Vector2Int(0,-1)   // South (-Y)
    };

    // Corresponding walls for each direction
    private static readonly Wall[] DIR_TO_WALL =
    {
        Wall.East, Wall.North, Wall.West, Wall.South
    };

    // Opposite walls for each direction
    private static readonly Wall[] OPPOSITE_WALL =
    {
        Wall.West, Wall.South, Wall.East, Wall.North
    };


    /// <summary>
    /// Generate a maze.
    /// </summary>
    /// <param name="width">Number of cells on X (columns)</param>
    /// <param name="height">Number of cells on Y (rows)</param>
    /// <param name="seed">Random seed</param>
    /// <param name="difficulty">0–1. 0 = straighter corridors, 1 = many loops</param>
    /// <returns>2‑D int array (grid[column, row]) where bits represent remaining walls per cell</returns>
    public static int[,] Generate(int cellWidth, int cellHeight, int seed, float difficulty)
    {
        if (cellWidth <= 0 || cellHeight <= 0)
        {
            Debug.LogError("Maze dimensions must be positive.");
            return new int[0, 0];
        }

        difficulty = Mathf.Clamp01(difficulty);
        // grid[column, row]
        int[,] grid = new int[cellWidth, cellHeight];
        bool[,] visited = new bool[cellWidth, cellHeight];
        var rng = new System.Random(seed);

        // Initialize grid with all walls up
        for (int c = 0; c < cellWidth; c++)
        {
            for (int r = 0; r < cellHeight; r++)
            {
                grid[c, r] = (int)Wall.All;
            }
        }

        // --- Depth‑first search – carve perfect maze ---
        var stack = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(rng.Next(cellWidth), rng.Next(cellHeight));
        stack.Push(start);
        visited[start.x, start.y] = true; // x is column, y is row

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek(); // current.x = column, current.y = row

            List<int> unvisitedDirIndices = new List<int>(4);
            for (int dirIdx = 0; dirIdx < 4; dirIdx++)
            {
                Vector2Int neighbor = current + DIRS[dirIdx];
                // Check bounds (neighbor.x is column, neighbor.y is row)
                if (neighbor.x >= 0 && neighbor.x < cellWidth &&
                    neighbor.y >= 0 && neighbor.y < cellHeight &&
                    !visited[neighbor.x, neighbor.y])
                {
                    unvisitedDirIndices.Add(dirIdx);
                }
            }

            if (unvisitedDirIndices.Count == 0)
            {
                // Back‑track
                stack.Pop();
            }
            else
            {
                // Pick random unvisited neighbor
                int chosenDirIdx = unvisitedDirIndices[rng.Next(unvisitedDirIndices.Count)];
                Vector2Int next = current + DIRS[chosenDirIdx];

                // Remove wall between current and next
                RemoveWallBetween(grid, current, next, chosenDirIdx);

                visited[next.x, next.y] = true;
                stack.Push(next);
            }
        }

        // --- Increase branching / add loops based on difficulty ---
        // More difficulty = more loops.
        // Max loops could be roughly (width-1)*height + (height-1)*width - (width*height-1) = total internal edges - edges in spanning tree
        // Let's try to remove a percentage of remaining internal walls.
        int potentialExtraLoops = (cellWidth - 1) * cellHeight + (cellHeight - 1) * cellWidth; // Max possible internal walls
        int extraConnectionsToRemove = Mathf.RoundToInt(difficulty * potentialExtraLoops * 0.25f); // Remove 25% of potential loops at max difficulty

        for (int i = 0; i < extraConnectionsToRemove; i++)
        {
            // Pick random cell that is not on the border (to ensure we are picking an internal wall)
            if (cellWidth <= 1 && cellHeight <= 1) break; // No internal walls

            int c = (cellWidth > 1) ? rng.Next(cellWidth - 1) : 0;
            int r = (cellHeight > 1) ? rng.Next(cellHeight - 1) : 0;
            Vector2Int cell = new Vector2Int(c, r);

            // Pick a random internal wall direction (East or North)
            // If we pick cell (c,r), we can try to remove its East wall (to cell c+1, r) or North wall (to cell c, r+1)
            List<int> possibleDirsToRemove = new List<int>();
            if (c < cellWidth - 1) possibleDirsToRemove.Add(0); // East
            if (r < cellHeight - 1) possibleDirsToRemove.Add(1); // North

            if (possibleDirsToRemove.Count == 0) continue;

            int dirIdxToRemove = possibleDirsToRemove[rng.Next(possibleDirsToRemove.Count)];
            Vector2Int neighbor = cell + DIRS[dirIdxToRemove];

            // Check if wall actually exists
            if ((grid[cell.x, cell.y] & (int)DIR_TO_WALL[dirIdxToRemove]) != 0)
            {
                RemoveWallBetween(grid, cell, neighbor, dirIdxToRemove);
            }
        }


        // --- Prune dead ends for lower difficulty (difficulty < 0.3) ---
        // Lower difficulty = more pruning = straighter paths.
        if (difficulty < 0.3f)
        {
            // Probability of pruning a dead-end. Higher when difficulty is lower.
            float pruneProbability = (0.3f - difficulty) * 2f; // Max 0.6 probability
            PruneDeadEnds(grid, cellWidth, cellHeight, rng, pruneProbability);
        }

        return grid;
    }

    #region helpers

    private static void RemoveWallBetween(int[,] grid, Vector2Int cellA, Vector2Int cellB, int dirIdxFromAtoB)
    {
        // cellA.x, cellA.y are col, row
        // Remove wall from cellA
        grid[cellA.x, cellA.y] &= ~(int)DIR_TO_WALL[dirIdxFromAtoB];
        // Remove corresponding wall from cellB
        grid[cellB.x, cellB.y] &= ~(int)OPPOSITE_WALL[dirIdxFromAtoB];
    }

    private static void PruneDeadEnds(int[,] grid, int cellWidth, int cellHeight, System.Random rng, float probability)
    {
        bool changedInPass;
        List<Vector2Int> deadEnds = new List<Vector2Int>();

        // It's often better to iterate multiple times or until no changes,
        // as pruning one dead-end might create another.
        // However, for simplicity and performance, one pass with a check or a few fixed passes can be okay.
        // For this version, let's collect all dead-ends first, then decide to prune.
        // This avoids issues where pruning one affects the dead-end status of its neighbor in the same loop.

        for (int repeat = 0; repeat < 2; repeat++) // Repeat pruning a couple of times
        {
            deadEnds.Clear();
            for (int c = 0; c < cellWidth; c++)
            {
                for (int r = 0; r < cellHeight; r++)
                {
                    // Skip border cells if you want to ensure entry/exit points are not removed,
                    // or handle entry/exit explicitly. For now, we consider all cells.
                    // if (c == 0 || c == cellWidth - 1 || r == 0 || r == cellHeight - 1) continue;


                    int cellWallData = grid[c, r];
                    int openDirections = 0;
                    for (int dir = 0; dir < 4; dir++)
                    {
                        if (((Wall)cellWallData & DIR_TO_WALL[dir]) == 0) // If wall in this direction is NOT present
                        {
                            openDirections++;
                        }
                    }

                    if (openDirections == 1) // This is a dead-end
                    {
                        // Don't prune if it's the start cell of the DFS,
                        // though that's unlikely to be a dead-end after full generation unless 1x1 maze.
                        deadEnds.Add(new Vector2Int(c, r));
                    }
                }
            }

            if (deadEnds.Count == 0) break; // No dead ends found

            changedInPass = false;
            // Shuffle dead ends to prune randomly if many exist
            for (int i = 0; i < deadEnds.Count; i++)
            {
                var temp = deadEnds[i];
                int randomIndex = rng.Next(i, deadEnds.Count);
                deadEnds[i] = deadEnds[randomIndex];
                deadEnds[randomIndex] = temp;
            }

            foreach (var deadEndCell in deadEnds)
            {
                if (rng.NextDouble() < probability)
                {
                    // Find the single opening
                    int openingDirIdx = -1;
                    for (int dirIdx = 0; dirIdx < 4; dirIdx++)
                    {
                        if ((grid[deadEndCell.x, deadEndCell.y] & (int)DIR_TO_WALL[dirIdx]) == 0)
                        {
                            openingDirIdx = dirIdx;
                            break;
                        }
                    }
                    if (openingDirIdx == -1) continue; // Should not happen for a dead-end

                    // Try to open one of the other 3 walls
                    List<int> possibleWallsToOpen = new List<int>(3);
                    for (int dirIdx = 0; dirIdx < 4; dirIdx++)
                    {
                        if (dirIdx == openingDirIdx) continue; // Don't close the only opening

                        Vector2Int neighbor = deadEndCell + DIRS[dirIdx];
                        // Check bounds for neighbor
                        if (neighbor.x >= 0 && neighbor.x < cellWidth &&
                            neighbor.y >= 0 && neighbor.y < cellHeight)
                        {
                            // Check if this wall actually exists
                            if ((grid[deadEndCell.x, deadEndCell.y] & (int)DIR_TO_WALL[dirIdx]) != 0)
                            {
                                possibleWallsToOpen.Add(dirIdx);
                            }
                        }
                    }

                    if (possibleWallsToOpen.Count > 0)
                    {
                        int wallToOpenDirIdx = possibleWallsToOpen[rng.Next(possibleWallsToOpen.Count)];
                        Vector2Int neighborToConnect = deadEndCell + DIRS[wallToOpenDirIdx];
                        RemoveWallBetween(grid, deadEndCell, neighborToConnect, wallToOpenDirIdx);
                        changedInPass = true;
                    }
                }
            }
            if (!changedInPass) break; // If no changes in this pass, further passes won't help
        }
    }

    #endregion
}
