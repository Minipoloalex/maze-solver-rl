using UnityEngine;

/// <summary>
/// Contains methods to spawn objects in the scene
/// </summary>
public class MazeSpawner : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridSize;

    [Header("Prefabs")]
    public GameObject platformAgentPrefab;
    public GameObject wallPrefab;   // 1x1x1 
    public GameObject floorPrefab;  // 1x1x1 cube (for trigger purpose only)
    public GameObject ballPrefab;   // sphere: radius = 1
    [Tooltip("Helper for agent to track ball")] // allows agent to have a grid sensor component on the ball
    public GameObject ballGridAnchorPrefab;
    public GameObject floorTriggerPrefab;   // prefab with script for on hover (see ghostPrefab)
    public GameObject ghostPrefab;  // 1x1x1 cube (transparent): shown on hover (when adding a new wall)
    public GameObject exitPadPrefab;    // prefab 1 x 1 x 1: represents the exit (only for visualisation for now)
    [Tooltip("Y-scaling for the exit pad prefab")]
    public float exitPadScalingY = 0.01f; // should be about 0.1f

    [Header("Scales")]
    public Vector3 wallScale = Vector3.one;
    public Vector3 ballScale = Vector3.one;

    public GameObject SpawnPlatformAgent(Transform parent, MazeController controller, Vector2Int exitPosId)
    {
        var agent = Instantiate(platformAgentPrefab, parent.position, parent.rotation, parent);
        var agentScript = agent.GetComponent<PlatformAgent>();
        agentScript.controller = controller;
        agentScript.worldExitPosition = GetWorldRelativePosition(exitPosId);
        return agent;
    }

    public GameObject SpawnFloor(Transform parent)
    {
        float width = wallScale.x * gridSize.x;
        float depth = wallScale.z * gridSize.y;

        Vector3 floorRelativePos = new Vector3(0, -0.25f, 0); // slightly below to avoid issues
        Vector3 floorScale = new Vector3(width, 0.5f, depth);

        GameObject floor = Instantiate(floorPrefab, parent.position + floorRelativePos, parent.rotation, parent);
        floor.transform.localScale = floorScale;
        return floor;
    }

    public GameObject SpawnWallsContainer(Transform parent)
    {
        var container = new GameObject("Walls");
        container.transform.SetParent(parent);
        container.transform.position = parent.transform.position;
        return container;
    }

    public GameObject SpawnFloorTriggersContainer(Transform parent)
    {
        var container = new GameObject("FloorTriggers");
        container.transform.SetParent(parent);
        container.transform.position = parent.transform.position;
        return container;
    }

    public GameObject SpawnWall(Transform parent, Vector2Int posId, MazeController controller)
    {
        Vector3 pos = GetWorldRelativePosition(posId);

        var wall = Instantiate(wallPrefab, parent.position + pos, parent.rotation, parent);
        wall.transform.localPosition = pos;
        wall.transform.localRotation = Quaternion.identity;
        wall.transform.localScale = Vector3.Scale(wall.transform.localScale, wallScale);

        var wallCell = wall.GetComponent<WallCell>();
        wallCell.posId = posId;
        wallCell.controller = controller;

        return wall;
    }

    public GameObject SpawnFloorTrigger(Transform parent, Vector2Int posId, MazeController controller)
    {
        Vector3 pos = GetWorldRelativePosition(posId);

        var trigger = Instantiate(floorTriggerPrefab, parent.position + pos, parent.rotation, parent);
        trigger.transform.localPosition = pos;
        trigger.transform.localRotation = Quaternion.identity;
        trigger.transform.localScale = Vector3.Scale(trigger.transform.localScale, wallScale);

        var script = trigger.GetComponent<FloorCell>();
        script.posId = posId;
        script.controller = controller;

        return trigger;
    }
    public GameObject SpawnExitPad(Transform parent, Vector2Int posId)
    {
        Vector3 pos = GetWorldRelativePosition(posId, height: exitPadScalingY / 2);

        var exitPad = Instantiate(exitPadPrefab, parent.position + pos, parent.rotation, parent);
        exitPad.transform.localPosition = pos;
        exitPad.transform.localRotation = Quaternion.identity;

        exitPad.transform.localScale = Vector3.Scale(exitPad.transform.localScale, wallScale);
        exitPad.transform.localScale = Vector3.Scale(exitPad.transform.localScale, new Vector3(1, exitPadScalingY, 1));
        return exitPad;
    }

    public GameObject SpawnBall(Transform parent, Vector2Int posId, MazeController controller)
    {
        Vector3 pos = GetWorldRelativePosition(posId);

        var ball = Instantiate(ballPrefab, parent.position + pos, parent.rotation, parent);
        ball.transform.localScale = Vector3.Scale(ball.transform.localScale, ballScale);

        return ball;
    }
    // no longer instantiated in runtime (because requires registering the sensor)
    // public GameObject SpawnBallGridAnchor(Transform agentParent, Transform ballTransform)
    // {
    //     var ballGridAnchor = Instantiate(ballGridAnchorPrefab, ballTransform.position, agentParent.rotation, agentParent);
    //     ballGridAnchor.transform.localScale = Vector3.Scale(ballGridAnchor.transform.localScale, ballScale);
    //     return ballGridAnchor;
    // }
    public Vector3 GetWorldRelativePosition(Vector2Int posId, float height = 0.5f)
    {
        // x are columns, z are rows
        float zShift = ((gridSize.y - 1) / 2.0f) * wallScale.z;
        float xShift = ((gridSize.x - 1) / 2.0f) * wallScale.x;
        int r = posId.x;
        int c = posId.y;

        // flip Z so row 0 is top
        Vector3 pos = new Vector3(c - xShift, height, (gridSize.y - 1 - r) - zShift);

        return pos;
    }
    /// <summary>
    /// Converts a local position relative to the maze's parent transform back to a grid coordinate (posId).
    /// This is the inverse of GetWorldRelativePosition.
    /// Very important: it requires that the position is given for a non-rotated plane.
    /// You may need to unrotate the positions using Quaternion.inverse(planeRot) * position
    /// </summary>
    /// <param name="pos">The local position within the maze's parent transform.</param>
    /// <returns>The corresponding grid cell coordinate (row, col).</returns>
    public Vector2Int GetPosIdFromWorldRelativePosition(Vector3 pos)
    {
        // Calculate the same shifts used when creating the positions
        float zShift = ((gridSize.y - 1) / 2.0f) * wallScale.z;
        float xShift = ((gridSize.x - 1) / 2.0f) * wallScale.x;

        // --- Inverse Calculations ---

        // Solve for 'c' (column) from the x-coordinate
        // Original: pos.x = c - xShift
        // Inverse: c = pos.x + xShift
        int c = Mathf.RoundToInt(pos.x + xShift);

        // Solve for 'r' (row) from the z-coordinate
        // Original: pos.z = (gridSize.y - 1 - r) - zShift
        // Inverse: r = gridSize.y - 1 - (pos.z + zShift)
        int r = Mathf.RoundToInt(gridSize.y - 1 - (pos.z + zShift));

        return new Vector2Int(r, c);
    }
}
