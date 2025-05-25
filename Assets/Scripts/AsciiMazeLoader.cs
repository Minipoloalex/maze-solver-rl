#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Linq; // <--- ADD THIS LINE

// Removed [ExecuteInEditMode] unless you still want specific editor-only build functionality
// separate from the runtime loading controlled by MazeController.
// For this refactor, we assume it's primarily for runtime parsing.
public class AsciiMazeLoader : MonoBehaviour // MonoBehaviour if it needs to be in scene, or static class
{
    // If you had a "Build from Config" button in editor that used MazeFromAscii,
    // you might want to keep parts of the old logic under an #if UNITY_EDITOR block
    // or create a separate editor script.
    // For now, this focuses on the runtime parsing part.

    public bool[,] ParseAsciiMaze(TextAsset asciiFile, out int numRows, out int numCols, out Vector2Int? ballInitialCell)
    {
        ballInitialCell = null;
        if (asciiFile == null)
        {
            Debug.LogError("Ascii TextAsset is null in ParseAsciiMaze.");
            numRows = 0;
            numCols = 0;
            return new bool[0, 0];
        }

        string[] rows = asciiFile.text.Replace("\r", "").Split('\n');
        // Filter out empty lines, especially at the end of the file
        // This line requires System.Linq
        rows = rows.Where(row => !string.IsNullOrWhiteSpace(row)).ToArray();


        if (rows.Length == 0)
        {
            Debug.LogError("ASCII maze file is empty or contains only whitespace.");
            numRows = 0;
            numCols = 0;
            return new bool[0, 0];
        }

        numRows = rows.Length;
        numCols = rows[0].Length;

        // Validate column sizes
        for (int i = 1; i < numRows; i++)
        {
            if (rows[i].Length != numCols)
            {
                Debug.LogError($"Row {i} has inconsistent length. Expected {numCols}, got {rows[i].Length}. Ensure no empty lines in the middle or trailing spaces altering perceived length.");
                // Optionally, handle this more gracefully, e.g. by truncating/padding or returning error.
                // For now, we proceed but this could lead to issues.
            }
        }

        bool[,] blockGrid = new bool[numRows, numCols];
        bool ballFound = false;

        for (int r = 0; r < numRows; r++)
        {
            for (int c = 0; c < numCols; c++)
            {
                if (c >= rows[r].Length)
                { // Safety for ragged rows if not strictly handled above
                    blockGrid[r, c] = true; // Treat as wall
                    continue;
                }

                char cellChar = rows[r][c];
                switch (cellChar)
                {
                    case ' ':
                        blockGrid[r, c] = false; // Empty
                        break;
                    case '#':
                        blockGrid[r, c] = true;  // Wall
                        break;
                    case 'B':
                        blockGrid[r, c] = false; // Ball position is empty
                        if (ballFound)
                        {
                            Debug.LogWarning("Multiple 'B' (ball start) characters found. Using the last one.");
                        }
                        ballInitialCell = new Vector2Int(r, c);
                        ballFound = true;
                        break;
                    default:
                        Debug.LogWarning($"Unknown char '{cellChar}' at r:{r},c:{c} in ASCII file. Treating as wall.");
                        blockGrid[r, c] = true; // Treat unknown as wall
                        break;
                }
            }
        }

        if (!ballFound)
        {
            Debug.LogWarning("No 'B' (ball start) character found in ASCII maze.");
        }

        return blockGrid;
    }

    // If you still want an editor button to build directly (bypassing MazeController for quick tests)
#if UNITY_EDITOR
    [Header("Editor Build Settings (Optional)")]
    public TextAsset editorAsciiMaze;
    public MazeSpawner editorSpawner;
    public MazeRuntimeGrid editorRuntimeGrid;
    // This reference is for the OLD direct spawning, not the new MazeController flow
    [ContextMenu("Editor: Build Maze Directly (uses editor references)")]
    public void EditorDirectBuild()
    {
        if (editorAsciiMaze == null || editorSpawner == null || editorRuntimeGrid == null)
        {
            Debug.LogError("Assign TextAsset, MazeSpawner, and MazeRuntimeGrid in the inspector for EditorDirectBuild.");
            return;
        }

        editorSpawner.ClearSpawnedChildren(); // Make sure this only destroys what it should

        Vector2Int? ballPos;
        int rCount, cCount;
        bool[,] grid = ParseAsciiMaze(editorAsciiMaze, out rCount, out cCount, out ballPos);

        GameObject platformAgent = editorSpawner.SpawnPlatformAgent();
        editorSpawner.platformAgent = platformAgent;

        editorSpawner.SpawnFloor(rCount, cCount, platformAgent.transform);
        
        GameObject wallsCont = editorSpawner.SpawnWallsContainer(platformAgent.transform);
        editorSpawner.wallsParent = wallsCont;
        GameObject triggersCont = editorSpawner.SpawnFloorTriggersContainer(platformAgent.transform);
        editorSpawner.triggersParent = triggersCont;

        editorRuntimeGrid.Init(rCount, cCount);
        editorSpawner.PopulateFromBlockGrid(grid, rCount, cCount, editorRuntimeGrid);

        if (ballPos.HasValue)
        {
            Vector3 worldPos = editorSpawner.GetWorldPosition(ballPos.Value, rCount, cCount, platformAgent.transform.position.y + 0.5f * editorSpawner.ballScale.y);
            GameObject ball = editorSpawner.SpawnBall(worldPos);
            PlatformAgent paScript = platformAgent.GetComponent<PlatformAgent>();
            if (paScript) paScript.SetInitialBallMazePosition(worldPos, ball);
        }
        else
        {
            Debug.LogWarning("EditorBuild: No ball spawn point defined in ASCII.");
        }

        Debug.Log("Editor direct build complete. Repaint scene if necessary.");
        SceneView.RepaintAll();
    }
#endif
}
