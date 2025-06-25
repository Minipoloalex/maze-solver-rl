using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a 2‑D rectangular maze using depth‑first search (recursive back‑tracker) and
/// optionally increases the branching factor / loopiness according to a 0–1 <c>difficulty</c> value.
/// The generator is deterministic for the same <c>seed</c> so you can replay episodes.
/// Cell indices for internal int[,] grid: (0,0) is bottom‑left, +X (columns) to the right, +Y (rows) upwards.
/// The resulting int[,] grid stores bit‑flags per cell.
/// Provides a higher-level function to convert this int[,] grid to a MazeRuntimeGrid.
/// </summary>
public static class MazeGenerator
{
    [Flags]
    public enum Wall : byte
    {
        None = 0,
        East = 1,  // +X
        North = 2, // +Y
        West = 4,  // -X
        South = 8, // -Y
        All = East | North | West | South
    }

    private static readonly Vector2Int[] DIRS =
    {
        new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1,0), new Vector2Int(0,-1)
    };
    private static readonly Wall[] DIR_TO_WALL =
    {
        Wall.East, Wall.North, Wall.West, Wall.South
    };
    private static readonly Wall[] OPPOSITE_WALL =
    {
        Wall.West, Wall.South, Wall.East, Wall.North
    };

    /// <summary>
    /// Generates the low-level maze data (wall flags per cell).
    /// </summary>
    /// <param name="cellWidth">Number of cells on X (columns) for the generator's internal grid.</param>
    /// <param name="cellHeight">Number of cells on Y (rows) for the generator's internal grid.</param>
    /// <param name="seed">Random seed.</param>
    /// <param name="difficulty">0–1. 0 = straighter corridors, 1 = many loops.</param>
    /// <returns>2‑D int array (grid[column, row]) where bits represent remaining walls per cell.</returns>
    public static int[,] Generate(int cellWidth, int cellHeight, int seed, float difficulty)
    {
        if (cellWidth <= 0 || cellHeight <= 0)
        {
            Debug.LogError("Maze dimensions must be positive.");
            return new int[0, 0];
        }

        difficulty = Mathf.Clamp01(difficulty);
        int[,] grid = new int[cellWidth, cellHeight]; // grid[column, row]
        bool[,] visited = new bool[cellWidth, cellHeight];
        var rng = new System.Random(seed);

        for (int c = 0; c < cellWidth; c++)
            for (int r = 0; r < cellHeight; r++)
                grid[c, r] = (int)Wall.All;

        var stack = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(rng.Next(cellWidth), rng.Next(cellHeight));
        stack.Push(start);
        visited[start.x, start.y] = true;

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<int> unvisitedDirIndices = new List<int>(4);
            for (int dirIdx = 0; dirIdx < 4; dirIdx++)
            {
                Vector2Int neighbor = current + DIRS[dirIdx];
                if (neighbor.x >= 0 && neighbor.x < cellWidth &&
                    neighbor.y >= 0 && neighbor.y < cellHeight &&
                    !visited[neighbor.x, neighbor.y])
                {
                    unvisitedDirIndices.Add(dirIdx);
                }
            }

            if (unvisitedDirIndices.Count == 0)
            {
                stack.Pop();
            }
            else
            {
                int chosenDirIdx = unvisitedDirIndices[rng.Next(unvisitedDirIndices.Count)];
                Vector2Int next = current + DIRS[chosenDirIdx];
                RemoveWallBetween(grid, current, next, chosenDirIdx);
                visited[next.x, next.y] = true;
                stack.Push(next);
            }
        }

        int potentialExtraLoops = (cellWidth - 1) * cellHeight + (cellHeight - 1) * cellWidth;
        int extraConnectionsToRemove = Mathf.RoundToInt(difficulty * potentialExtraLoops * 0.25f);

        for (int i = 0; i < extraConnectionsToRemove; i++)
        {
            if (cellWidth <= 1 && cellHeight <= 1) break;
            int c = (cellWidth > 1) ? rng.Next(cellWidth - 1) : 0; // Pick column for East/North removal
            int r = (cellHeight > 1) ? rng.Next(cellHeight - 1) : 0; // Pick row for East/North removal
            // If cellWidth = 1, c will be 0. If cellHeight = 1, r will be 0.

            Vector2Int cell = new Vector2Int(c, r);

            List<int> possibleDirsToRemove = new List<int>();
            if (c < cellWidth - 1) possibleDirsToRemove.Add(0); // East
            if (r < cellHeight - 1) possibleDirsToRemove.Add(1); // North

            if (possibleDirsToRemove.Count == 0) continue;

            int dirIdxToRemove = possibleDirsToRemove[rng.Next(possibleDirsToRemove.Count)];
            Vector2Int neighbor = cell + DIRS[dirIdxToRemove];

            if ((grid[cell.x, cell.y] & (int)DIR_TO_WALL[dirIdxToRemove]) != 0)
            {
                RemoveWallBetween(grid, cell, neighbor, dirIdxToRemove);
            }
        }

        if (difficulty < 0.3f)
        {
            float pruneProbability = (0.3f - difficulty) * 2f;
            PruneDeadEnds(grid, cellWidth, cellHeight, rng, pruneProbability);
        }

        return grid;
    }

    /// <summary>
    /// Generates a maze and converts it into a MazeRuntimeGrid, also providing ball and exit positions.
    /// </summary>
    /// <param name="generatorCellWidth">Number of cells on X (columns) for the base maze generation.</param>
    /// <param name="generatorCellHeight">Number of cells on Y (rows) for the base maze generation.</param>
    /// <param name="seed">Random seed.</param>
    /// <param name="difficulty">0–1. 0 = straighter corridors, 1 = many loops.</param>
    /// <param name="ballPosRuntime">Output: The ball's starting position in MazeRuntimeGrid coordinates (row, col).</param>
    /// <param name="exitPosRuntime">Output: The exit's position in MazeRuntimeGrid coordinates (row, col).</param>
    /// <returns>A MazeRuntimeGrid representing the generated maze.</returns>
    public static MazeRuntimeGrid GenerateMazeForRuntimeGrid(
        int generatorCellWidth,
        int generatorCellHeight,
        int seed,
        float difficulty,
        out Vector2Int ballPosRuntime,
        out Vector2Int exitPosRuntime)
    {
        // Generate the low-level maze data
        // Note: generatorCellWidth and generatorCellHeight must be at least 1.
        int genWidth = Mathf.Max(1, generatorCellWidth);
        int genHeight = Mathf.Max(1, generatorCellHeight);

        int[,] mazeData = Generate(genWidth, genHeight, seed, difficulty);

        // Determine the generator's start cell (used for ball position)
        // The DFS in Generate() picks a random start cell. We replicate that first RNG pick.
        System.Random tempRngForStart = new System.Random(seed);
        Vector2Int generatorStartCell = new Vector2Int(tempRngForStart.Next(genWidth), tempRngForStart.Next(genHeight));
        // generatorStartCell.x is column, generatorStartCell.y is row in mazeData's context

        // Define MazeRuntimeGrid dimensions
        // Each cell from mazeData maps to a 2x2 area, plus boundary.
        // MazeRuntimeGrid coordinates are (row, col).
        int runtimeGridRows = genHeight * 2 + 1;
        int runtimeGridCols = genWidth * 2 + 1;
        MazeRuntimeGrid runtimeGrid = new MazeRuntimeGrid(runtimeGridRows, runtimeGridCols);

        // Initialize MazeRuntimeGrid: Make all cells walls initially.
        // (Assuming MazeRuntimeGrid constructor defaults to floor, so we add walls)
        for (int r_rt = 0; r_rt < runtimeGridRows; r_rt++)
        {
            for (int c_rt = 0; c_rt < runtimeGridCols; c_rt++)
            {
                runtimeGrid.AddWall(new Vector2Int(r_rt, c_rt));
            }
        }

        // Carve paths in MazeRuntimeGrid based on mazeData
        // mazeData[col, row] stores wall flags.
        // Vector2Int for MazeRuntimeGrid is (row, col).
        for (int gen_r = 0; gen_r < genHeight; gen_r++) // Iterate generator's rows
        {
            for (int gen_c = 0; gen_c < genWidth; gen_c++) // Iterate generator's columns
            {
                // Center of the current generator cell in the runtime grid
                int rt_row_center = gen_r * 2 + 1;
                int rt_col_center = gen_c * 2 + 1;

                // This cell itself is a path
                runtimeGrid.RemoveWall(new Vector2Int(rt_row_center, rt_col_center));

                // If no East wall in generator cell, carve path to the East in runtime grid
                if ((mazeData[gen_c, gen_r] & (int)Wall.East) == 0)
                {
                    if (rt_col_center + 1 < runtimeGridCols) // Boundary check
                    {
                        runtimeGrid.RemoveWall(new Vector2Int(rt_row_center, rt_col_center + 1));
                    }
                }

                // If no North wall in generator cell, carve path to the North in runtime grid
                if ((mazeData[gen_c, gen_r] & (int)Wall.North) == 0)
                {
                    if (rt_row_center + 1 < runtimeGridRows) // Boundary check
                    {
                        // North in generator (increasing Y/row) means increasing row in runtime grid.
                        runtimeGrid.RemoveWall(new Vector2Int(rt_row_center + 1, rt_col_center));
                    }
                }
                // South and West walls are implicitly handled by connections from neighboring cells
                // (e.g. cell (c-1,r)'s East wall removal carves current cell's West passage)
            }
        }

        // Set ball position (maps from generator's start cell)
        // generatorStartCell.x is col, .y is row. MazeRuntimeGrid uses (row, col).
        ballPosRuntime = new Vector2Int(generatorStartCell.y * 2 + 1, generatorStartCell.x * 2 + 1);

        // Set exit position (e.g., map from a corner of the generator's grid)
        // Default to top-right corner of the generator grid
        int exit_gen_r = genHeight - 1;
        int exit_gen_c = genWidth - 1;

        // If the start cell (ball) is the same as this default exit, pick another corner for exit,
        // unless it's a 1x1 generator grid where they must be the same.
        if (generatorStartCell.x == exit_gen_c && generatorStartCell.y == exit_gen_r && (genWidth > 1 || genHeight > 1))
        {
            exit_gen_r = 0; // Move to bottom-left
            exit_gen_c = 0;
        }

        exitPosRuntime = new Vector2Int(exit_gen_r * 2 + 1, exit_gen_c * 2 + 1);

        // Ensure ball and exit positions are actually paths (they should be by construction)
        if (runtimeGrid.IsWall(ballPosRuntime))
            Debug.LogWarning($"Generated ball position {ballPosRuntime} is a wall. Review conversion logic. gen_start=({generatorStartCell.x},{generatorStartCell.y})");
        if (runtimeGrid.IsWall(exitPosRuntime))
            Debug.LogWarning($"Generated exit position {exitPosRuntime} is a wall. Review conversion logic. gen_exit=({exit_gen_c},{exit_gen_r})");


        return runtimeGrid;
    }


    #region helpers

    private static void RemoveWallBetween(int[,] grid, Vector2Int cellA, Vector2Int cellB, int dirIdxFromAtoB)
    {
        grid[cellA.x, cellA.y] &= ~(int)DIR_TO_WALL[dirIdxFromAtoB];
        grid[cellB.x, cellB.y] &= ~(int)OPPOSITE_WALL[dirIdxFromAtoB];
    }

    private static void PruneDeadEnds(int[,] grid, int cellWidth, int cellHeight, System.Random rng, float probability)
    {
        bool changedInPass;
        List<Vector2Int> deadEnds = new List<Vector2Int>();

        for (int repeat = 0; repeat < 2; repeat++)
        {
            deadEnds.Clear();
            for (int c = 0; c < cellWidth; c++)
            {
                for (int r = 0; r < cellHeight; r++)
                {
                    int cellWallData = grid[c, r];
                    int openDirections = 0;
                    for (int dir = 0; dir < 4; dir++)
                    {
                        if (((Wall)cellWallData & DIR_TO_WALL[dir]) == 0)
                        {
                            openDirections++;
                        }
                    }
                    if (openDirections == 1)
                    {
                        deadEnds.Add(new Vector2Int(c, r));
                    }
                }
            }

            if (deadEnds.Count == 0) break;
            changedInPass = false;

            for (int i = 0; i < deadEnds.Count; i++) // Shuffle
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
                    int openingDirIdx = -1;
                    for (int dirIdx = 0; dirIdx < 4; dirIdx++)
                    {
                        if ((grid[deadEndCell.x, deadEndCell.y] & (int)DIR_TO_WALL[dirIdx]) == 0)
                        {
                            openingDirIdx = dirIdx;
                            break;
                        }
                    }
                    if (openingDirIdx == -1) continue;

                    List<int> possibleWallsToOpen = new List<int>(3);
                    for (int dirIdx = 0; dirIdx < 4; dirIdx++)
                    {
                        if (dirIdx == openingDirIdx) continue;
                        Vector2Int neighbor = deadEndCell + DIRS[dirIdx];
                        if (neighbor.x >= 0 && neighbor.x < cellWidth &&
                            neighbor.y >= 0 && neighbor.y < cellHeight)
                        {
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
            if (!changedInPass) break;
        }
    }
    #endregion
}
