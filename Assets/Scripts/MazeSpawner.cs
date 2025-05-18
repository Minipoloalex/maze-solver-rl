using UnityEngine;

public class MazeSpawner : MonoBehaviour
{
    [Header("Script Reference")]
    public MazeRuntimeGrid mazeGrid;

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

    [HideInInspector] public GameObject platformAgent;
    [HideInInspector] public GameObject triggersParent;
    [HideInInspector] public GameObject wallsParent;

    public Transform WallsParent { get; private set; }
    public void InitGrid(int rowCount, int colCount)
    {
        mazeGrid.Init(rowCount, colCount);
    }
    public GameObject SpawnPlatformAgent()
    {
        return Instantiate(platformAgentPrefab, Vector3.zero, Quaternion.identity, transform);
    }

    public GameObject SpawnFloor(int rowCount, int colCount, Transform parent)
    {
        float width = wallScale.x * colCount;
        float depth = wallScale.z * rowCount;
        Vector3 floorScale = new Vector3(width, 0.1f, depth);

        GameObject floor = Instantiate(floorPrefab, Vector3.zero, Quaternion.identity, parent);
        floor.transform.localScale = floorScale;
        return floor;
    }

    public GameObject SpawnWallsContainer(Transform parent)
    {
        var container = new GameObject("Walls");
        container.transform.SetParent(parent);
        WallsParent = container.transform;
        return container;
    }

    public GameObject SpawnFloorTriggersContainer(Transform parent)
    {
        var container = new GameObject("FloorTriggersContainer");
        container.transform.SetParent(parent);
        return container;
    }

    public GameObject SpawnWall(Vector3 pos, Vector2Int posId)
    {
        var wall = Instantiate(wallPrefab, pos, Quaternion.identity, wallsParent.transform);
        wall.transform.localScale = Vector3.Scale(wall.transform.localScale, wallScale);

        var wallCell = wall.GetComponent<WallCell>();
        wallCell.posId = posId;
        wallCell.spawner = this;

        mazeGrid.AddWall(posId);

        return wall;
    }
    public GameObject SpawnBall(Vector3 pos)
    {
        var ball = Instantiate(ballPrefab, pos, Quaternion.identity, transform);
        ball.transform.localScale = Vector3.Scale(ball.transform.localScale, ballScale);

        var agent = platformAgent.GetComponent<PlatformAgent>();
        agent.Init(ball);

        return ball;
    }

    public GameObject SpawnFloorTrigger(Vector3 pos, Vector2Int posId)
    {
        var trigger = Instantiate(floorTriggerPrefab, pos, Quaternion.identity, triggersParent.transform);
        trigger.transform.localScale = Vector3.Scale(trigger.transform.localScale, wallScale);

        var script = trigger.GetComponent<FloorCell>();
        script.posId = posId;
        script.spawner = this;

        mazeGrid.RemoveWall(posId);

        return trigger;
    }

    public void ClearSpawnedChildren()
    {
#if UNITY_EDITOR
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
        foreach (Transform child in transform)
            Destroy(child.gameObject);
#endif
    }
}
