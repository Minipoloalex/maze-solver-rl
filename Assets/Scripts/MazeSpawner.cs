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
    public GameObject floorTriggerPrefab;   // prefab with script for on hover (see ghostPrefab)
    public GameObject ghostPrefab;  // 1x1x1 cube (transparent): shown on hover (when adding a new wall)

    [Header("Scales")]
    public Vector3 wallScale = Vector3.one;
    public Vector3 ballScale = Vector3.one;

    public GameObject SpawnPlatformAgent(Transform parent)
    {
        return Instantiate(platformAgentPrefab, parent.position, parent.rotation, parent);
    }

    public GameObject SpawnFloor(Transform parent)
    {
        float width = wallScale.x * gridSize.x;
        float depth = wallScale.z * gridSize.y;

        Vector3 floorRelativePos = new Vector3(0, -0.05f, 0); // slightly below to avoid issues
        Vector3 floorScale = new Vector3(width, 0.1f, depth);

        GameObject floor = Instantiate(floorPrefab, parent.position + floorRelativePos, parent.rotation, parent);
        floor.transform.localScale = floorScale;
        return floor;
    }

    public GameObject SpawnWallsContainer(Transform parent)
    {
        var container = new GameObject("Walls");
        container.transform.SetParent(parent);
        return container;
    }

    public GameObject SpawnFloorTriggersContainer(Transform parent)
    {
        var container = new GameObject("FloorTriggers");
        container.transform.SetParent(parent);
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

    public GameObject SpawnBall(Transform parent, Vector2Int posId, MazeController controller)
    {
        Vector3 pos = GetWorldRelativePosition(posId);

        var ball = Instantiate(ballPrefab, parent.position + pos, parent.rotation, parent);
        ball.transform.localScale = Vector3.Scale(ball.transform.localScale, ballScale);

        return ball;
    }

    public Vector3 GetWorldRelativePosition(Vector2Int posId)
    {
        // x are columns, z are rows
        float zShift = ((gridSize.y - 1) / 2.0f) * wallScale.z;
        float xShift = ((gridSize.x - 1) / 2.0f) * wallScale.x;
        int r = posId.x;
        int c = posId.y;

        // flip Z so row 0 is top
        Vector3 pos = new Vector3(c - xShift, 0.5f, (gridSize.y - 1 - r) - zShift);

        return pos;
    }
}
