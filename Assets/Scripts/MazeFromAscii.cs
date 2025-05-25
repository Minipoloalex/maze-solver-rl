#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Reads an ASCII map where '#' = solid wall, ' ' (space) = walkable floor, 'B' = ball (only one should exist)
/// then instantiates prefabs in EDIT-TIME so that we can easily see the results
/// </summary>
[ExecuteInEditMode]
public class MazeFromAscii : MonoBehaviour
{
    public TextAsset asciiMaze;
    [Header("Script Reference")]
    public MazeSpawner spawner;

    // Fixed information about maze
    [HideInInspector] public int rowCount;
    [HideInInspector] public int colCount;

    private void CheckColumnSizes(string[] rows)
    {
        foreach (string row in rows)
        {
            if (row.Length != colCount)
            {
                Debug.LogError("All rows must have the same size! Ensure there is no empty line at the end of the file.");
            }
        }
    }

    [ContextMenu("Build From Config")]
    public void Build()
    {
#if UNITY_EDITOR
        if (asciiMaze == null)
        {
            Debug.LogError("Assign an ASCII TextAsset first.");
            return;
        }

        // spawner.ClearSpawnedChildren();

        string[] rows = asciiMaze.text.Replace("\r", "").Split('\n');
        rowCount = rows.Length;
        colCount = rows[0].Length;
        CheckColumnSizes(rows);
        // spawner.InitGrid(rowCount, colCount);

        // GameObject platformAgent = spawner.SpawnPlatformAgent();
        // spawner.platformAgent = platformAgent;

        // GameObject floor = spawner.SpawnFloor(rowCount, colCount, platformAgent.transform);

        // GameObject wallsContainer = spawner.SpawnWallsContainer(platformAgent.transform);
        // spawner.wallsParent = wallsContainer;

        // GameObject floorTriggersContainer = spawner.SpawnFloorTriggersContainer(platformAgent.transform);
        // spawner.triggersParent = floorTriggersContainer;

        // // x are columns, z are rows
        // float zShift = ((rowCount - 1) / 2.0f) * spawner.wallScale.z;
        // float xShift = ((colCount - 1) / 2.0f) * spawner.wallScale.x;
        // bool foundBall = false;
        // for (int r = 0; r < rowCount; r++)
        // {   // z
        //     for (int c = 0; c < colCount; c++)
        //     {   // x
        //         char cell = rows[r][c];
        //         Vector3 pos = new Vector3(c - xShift, 0.5f, (rowCount - 1 - r) - zShift);   // flip Z so row 0 is top
        //         Vector2Int posId = new Vector2Int(r, c);
        //         switch (cell)
        //         {
        //             case ' ':
        //                 // floor is a large tile, but we also add here a trigger
        //                 // to allow adding walls
        //                 spawner.SpawnFloorTrigger(pos, posId);
        //                 break;
        //             case '#':
        //                 spawner.SpawnWall(pos, posId);
        //                 break;
        //             case 'B':
        //                 // Going to be where we define the ball
        //                 if (foundBall)
        //                 {
        //                     Debug.LogError("Cannot spawn two balls at once! (at least not yet)");
        //                 }
        //                 foundBall = true;
        //                 spawner.SpawnBall(pos);
        //                 spawner.SpawnFloorTrigger(pos, posId);  // also spawn a trigger (allows spawning walls in the initial ball position)
        //                 break;
        //             default:
        //                 Debug.LogWarning($"Unknown char '{c}' at {r},{c}: skipping");
        //                 break;
        //         }
        //     }
        // }

        // UnityEditor.SceneView.RepaintAll();   // refresh scene view immediately
#endif
    }
}
