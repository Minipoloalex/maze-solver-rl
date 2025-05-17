#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Reads an ASCII map where '#' = solid wall, ' ' (space) = walkable floor,
/// then instantiates prefabs in EDIT-TIME so you can tweak, bake, or light it.
/// </summary>
[ExecuteInEditMode]
public class MazeFromAscii : MonoBehaviour
{
    public TextAsset asciiMaze;

    [Header("Prefabs")]
    public GameObject floorAgentPrefab;   // 1×1 on X-Z, pivot at base
    public GameObject ballPrefab;
    public GameObject wallPrefab;    // 1×1×1 cube stretched to desired height

    [Header("Scales")]
    public Vector3 wallScale;
    public Vector3 ballScale;
    private void CheckColumnSizes(string[] rows, int colCount)
    {
        foreach (string row in rows)
        {
            if (row.Length != colCount)
            {
                Debug.LogError("All rows must have the same size! Ensure there is no empty line at the end of the file.");
            }
        }
    }
    private GameObject SpawnFloor(int rowCount, int colCount)
    {
        float floorScaleX = wallScale.x * colCount;
        float floorScaleZ = wallScale.z * rowCount;

        Vector3 floorScale = new Vector3(floorScaleX, 0.1f, floorScaleZ);

        GameObject floorAgent = Instantiate(floorAgentPrefab, Vector3.zero, Quaternion.identity, transform);
        floorAgent.transform.localScale = floorScale;
        return floorAgent;
    }
    private GameObject SpawnWallsContainer(Transform parent)
    {
        GameObject wallsContainer = new GameObject("Walls");
        wallsContainer.transform.SetParent(parent);
        wallsContainer.transform.localPosition = Vector3.zero;
        return wallsContainer;
    }
    private void SpawnWall(Vector3 pos, Transform parent)
    {
        GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, parent);
        wall.transform.localScale = Vector3.Scale(wall.transform.localScale, wallScale);
    }
    private void SpawnBall(Vector3 pos)
    {
        GameObject ball = Instantiate(ballPrefab, pos, Quaternion.identity, transform);
        ball.transform.localScale = Vector3.Scale(ball.transform.localScale, ballScale);
    }

    [ContextMenu("Build From Config")]
    public void Build()
    {
#if UNITY_EDITOR
        if (asciiMaze == null) { Debug.LogError("Assign an ASCII TextAsset first."); return; }

        ClearChildren();

        string[] rows = asciiMaze.text.Replace("\r", "").Split('\n');
        int rowCount = rows.Length;
        int colCount = rows[0].Length;
        CheckColumnSizes(rows, colCount);

        GameObject floorAgent = SpawnFloor(rowCount, colCount);
        GameObject wallsContainer = SpawnWallsContainer(floorAgent.transform);

        // x are columns, z are rows
        float zShift = ((rowCount - 1) / 2.0f) * wallScale.z;
        float xShift = ((colCount - 1) / 2.0f) * wallScale.x;
        bool foundBall = false;
        for (int z = 0; z < rowCount; z++)
        {
            for (int x = 0; x < colCount; x++)
            {
                char c = rows[z][x];
                Vector3 pos = new Vector3(x - xShift, 0.5f, (rowCount - 1 - z) - zShift);   // flip Z so row 0 is top
                switch (c)
                {
                    case ' ':
                        // do nothing (floor is simply a large tile)
                        break;
                    case '#':
                        SpawnWall(pos, wallsContainer.transform);
                        break;
                    case 'B':
                        // Going to be where we define the ball
                        if (foundBall)
                        {
                            Debug.LogError("Cannot spawn two balls at once! (at least not yet)");
                        }
                        foundBall = true;
                        SpawnBall(pos);
                        break;
                    default:
                        Debug.LogWarning($"Unknown char '{c}' at {x},{z}: skipping");
                        break;
                }
            }
        }

        UnityEditor.SceneView.RepaintAll();   // refresh scene view immediately
#endif
    }

    void ClearChildren()
    {
#if UNITY_EDITOR
        // DestroyImmediate works in edit mode
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
#endif
    }
}
