using UnityEngine;

/// <summary>
/// A utility class to add wall padding around an existing MazeRuntimeGrid.
/// This is useful to ensure reinforcement learning agents with grid sensors
/// clearly perceive the boundaries of the maze.
/// </summary>
public static class MazePadding
{
    /// <summary>
    /// Creates a new, larger grid with a border of walls around the original maze.
    /// It also updates the ball and exit positions to be relative to the new, padded grid.
    /// </summary>
    /// <param name="originalGrid">The maze to pad.</param>
    /// <param name="padding">The thickness of the wall border to add on all sides.</param>
    /// <param name="ballPos">Reference to the ball's position, which will be updated.</param>
    /// <param name="exitPos">Reference to the exit's position, which will be updated.</param>
    /// <returns>A new MazeRuntimeGrid with the padding applied.</returns>
    public static MazeRuntimeGrid Pad(MazeRuntimeGrid originalGrid, int padding, ref Vector2Int ballPos, ref Vector2Int exitPos)
    {
        if (padding <= 0)
        {
            return originalGrid;
        }

        int originalRows = originalGrid.Length;
        int originalCols = originalGrid[0].Length;

        int newRows = originalRows + padding * 2;
        int newCols = originalCols + padding * 2;

        var paddedGrid = new MazeRuntimeGrid(newRows, newCols);

        // Iterate over the new, larger grid
        for (int r = 0; r < newRows; r++)
        {
            for (int c = 0; c < newCols; c++)
            {
                // Check if the current cell is in the padding area
                if (r < padding || r >= originalRows + padding || c < padding || c >= originalCols + padding)
                {
                    // This is a padding cell, so make it a wall
                    paddedGrid.AddWall(new Vector2Int(r, c));
                }
                else
                {
                    // This is a cell from the original maze, so copy its state
                    int original_r = r - padding;
                    int original_c = c - padding;
                    if (originalGrid[original_r][original_c]) // Check if it was a wall
                    {
                        paddedGrid.AddWall(new Vector2Int(r, c));
                    }
                    else // It was a floor
                    {
                        // The MazeRuntimeGrid is assumed to be initialized with floors,
                        // or you have a RemoveWall/AddFloor method.
                        // If it initializes as walls, you would call RemoveWall here.
                        // Based on your controller, we assume AddWall is all that's needed.
                    }
                }
            }
        }

        // Update the ball and exit positions to be in the new coordinate system
        ballPos.x += padding;
        ballPos.y += padding;
        exitPos.x += padding;
        exitPos.y += padding;

        return paddedGrid;
    }
}
