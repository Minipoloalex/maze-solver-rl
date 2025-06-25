using UnityEngine;

/// <summary>
/// Receives an ASCII map of a maze where:
/// '#' = solid wall
/// ' ' (space) = walkable floor
/// 'B' = ball (only one should exist)
/// 'E' = exit (only one should exist)
/// It returns the result as a MazeRuntimeGrid (grid of booleans) and out-arguments
/// </summary>
public class AsciiMazeLoader : MonoBehaviour
{
    public TextAsset asciiMaze;

    private void CheckColumnSizes(string[] rows, int rowCount, int colCount)
    {
        foreach (string row in rows)
        {
            if (row.Length != colCount)
            {
                Debug.LogError("All rows must have the same size! Ensure there is no empty line at the end of the file.");
                Debug.Log($"rows={rowCount}, cols={colCount}, unexpected cols={row.Length}");
            }
        }
    }
    public MazeRuntimeGrid ReadGrid(out Vector2Int ballPos, out Vector2Int exitPos)
    {
        ballPos = new Vector2Int(-1, -1);
        exitPos = new Vector2Int(-1, -1);

        if (asciiMaze == null)
        {
            Debug.LogError("Assign an ASCII TextAsset first.");
            return null;
        }

        string[] rows = asciiMaze.text.Replace("\r", "").Split('\n');
        int rowCount = rows.Length;
        int colCount = rows[0].Length;
        CheckColumnSizes(rows, rowCount, colCount);

        MazeRuntimeGrid grid = new MazeRuntimeGrid(rowCount, colCount);
        for (int r = 0; r < rowCount; r++)
        {
            for (int c = 0; c < colCount; c++)
            {
                char cell = rows[r][c];
                Vector2Int posId = new Vector2Int(r, c);
                switch (cell)
                {
                    case ' ':
                        // no wall
                        grid.RemoveWall(posId); // not necessary, because default is "no wall"
                        break;
                    case '#':
                        // wall
                        grid.AddWall(posId);
                        break;
                    case 'B':
                        // ball
                        ballPos = posId;
                        break;
                    case 'E':
                        // exit
                        exitPos = posId;
                        break;
                    default:
                        Debug.LogWarning($"Unknown char '{c}' at {r},{c}: skipping");
                        break;
                }
            }
        }
        if (ballPos.x == -1 && ballPos.y == -1)
        {
            Debug.LogError("No ball found in the grid. Corresponds to character B.");
        }
        if (exitPos.x == -1 && exitPos.y == -1)
        {
            Debug.LogError("No exit found in the grid. Corresponds to character E.");
        }
        return grid;
    }
}
