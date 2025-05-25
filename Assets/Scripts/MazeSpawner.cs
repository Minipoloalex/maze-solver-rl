using UnityEngine;

public class MazeSpawner : MonoBehaviour
{
    [Header("Script Reference")]
    public MazeRuntimeGrid mazeGrid; // This is assigned by MazeController if it needs it, or used internally

    [Header("Prefabs")]
    public GameObject platformAgentPrefab;
    public GameObject wallPrefab;           // 1x1x1
    public GameObject floorPrefab;          // 1x1x1 cube (for trigger purpose only)
    public GameObject ballPrefab;           // sphere: radius = 1
    public GameObject floorTriggerPrefab;   // prefab with script for on hover (see ghostPrefab)
    public GameObject ghostPrefab;          // Used by FloorCell

    [Header("Scales")]
    public Vector3 wallScale = Vector3.one;
    public Vector3 ballScale = Vector3.one;

    // These are set by MazeController after spawning the respective containers
    [HideInInspector] public GameObject platformAgent;
    [HideInInspector] public GameObject triggersParent;
    [HideInInspector] public GameObject wallsParent;

    public GameObject SpawnPlatformAgent()
    {
        // The platform agent becomes a child of whatever GameObject MazeSpawner is on.
        return Instantiate(platformAgentPrefab, transform.position, Quaternion.identity, transform);
    }

    public GameObject SpawnFloor(int rowCount, int colCount, Transform parent)
    {
        // Calculate actual width/depth based on cell count and wall scale
        float width = wallScale.x * colCount;
        float depth = wallScale.z * rowCount;
        // Floor Y position should be slightly below walls/triggers, or centered.
        // Current logic: floor is at Y=0 relative to parent, scaled.
        // If walls are at Y=0.5, floor might need to be at Y=-0.05 to avoid Z-fighting,
        // or walls shifted up. Assuming current prefab setup handles this.
        Vector3 floorCenter = new Vector3(0, -0.05f, 0); // Example: slightly below center.
        Vector3 floorScale = new Vector3(width, 0.1f, depth);

        GameObject floor = Instantiate(floorPrefab, parent.TransformPoint(floorCenter), parent.rotation, parent);
        floor.transform.localScale = floorScale; // Use calculated dimensions
        return floor;
    }

    public GameObject SpawnWallsContainer(Transform parent)
    {
        var container = new GameObject("Walls");
        container.transform.SetParent(parent);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        this.wallsParent = container;
        return container;
    }

    public GameObject SpawnFloorTriggersContainer(Transform parent)
    {
        var container = new GameObject("FloorTriggersContainer");
        container.transform.SetParent(parent);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        this.triggersParent = container;
        return container;
    }

    public GameObject SpawnWall(Vector3 worldPos, Vector2Int posId)
    {
        if (wallsParent == null)
        {
            Debug.LogError("wallsParent is not set in MazeSpawner!");
            return null;
        }
        var wall = Instantiate(wallPrefab, worldPos, wallsParent.transform.rotation, wallsParent.transform);
        wall.transform.localScale = Vector3.Scale(wall.transform.localScale, wallScale);    // relative to prefab's scale

        var wallCell = wall.GetComponent<WallCell>();
        if (wallCell != null)
        {
            wallCell.posId = posId;
            wallCell.spawner = this; // WallCell needs this to call SpawnFloorTrigger
        }

        if (mazeGrid != null) mazeGrid.AddWall(posId);
        return wall;
    }
    public GameObject SpawnBall(Vector3 worldPos)
    {
        // The platformAgent is a child of this.transform
        // So, parent the ball to this.transform as well, to keep it at the same hierarchy level as the agent.
        // This makes the ball independent of the platformAgent's rotation.
        var ball = Instantiate(ballPrefab, worldPos, Quaternion.identity, this.transform);
        ball.transform.localScale = Vector3.Scale(ball.transform.localScale, ballScale);

        // Agent initialization is handled by MazeController using SetInitialBallMazePosition
        return ball;
    }

    public GameObject SpawnFloorTrigger(Vector3 worldPos, Vector2Int posId)
    {
        if (triggersParent == null)
        {
            Debug.LogError("triggersParent is not set in MazeSpawner!");
            return null;
        }
        var trigger = Instantiate(floorTriggerPrefab, worldPos, triggersParent.transform.rotation, triggersParent.transform);
        trigger.transform.localScale = Vector3.Scale(trigger.transform.localScale, wallScale);  // relative to prefab's size

        var script = trigger.GetComponent<FloorCell>();
        if (script != null)
        {
            script.posId = posId;
            script.spawner = this; // FloorCell needs this to call SpawnWall
            if (ghostPrefab != null) script.ghostPrefab = ghostPrefab; // Ensure ghost prefab is passed
        }

        if (mazeGrid != null) mazeGrid.RemoveWall(posId); // Mark as empty in runtime grid
        return trigger;
    }

    public void ClearSpawnedChildren()
    {
        // This assumes platformAgentPrefab and ballPrefab are assigned
        string platformAgentCloneName = platformAgentPrefab != null ? platformAgentPrefab.name + "(Clone)" : "###UnlikelyName###";
        string ballCloneName = ballPrefab != null ? ballPrefab.name + "(Clone)" : "###UnlikelyName###";

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<PlatformAgent>() != null || 
                child.name == platformAgentCloneName ||
                child.name == ballCloneName) // Check for ball clone name too
            {
    #if UNITY_EDITOR
                // In edit mode, DestroyImmediate is often needed for immediate effect.
                // Ensure this is not called during Play mode if it causes issues.
                // The MazeController calls this from Start(), so it will be in Play mode.
                // DestroyImmediate should generally be avoided in Play mode for non-Editor scripts.
                // Using Destroy() is safer for runtime.
                if (!Application.isPlaying) {
                    DestroyImmediate(child.gameObject);
                } else {
                    Destroy(child.gameObject);
                }
    #else
                Destroy(child.gameObject);
    #endif
            }
        }
        // Reset internal state
        platformAgent = null; // This is fine as a new one will be spawned
        // wallsParent and triggersParent are children of the platformAgent, so they will be destroyed with it.
        // No need to explicitly null them here if platformAgent is destroyed.
        // However, if platformAgent is only 'cleared' of its children, then nulling these would be important.
        // Since the platformAgent itself is destroyed, their references become invalid anyway.
    }

    public Vector3 GetWorldPosition(Vector2Int cellIndex, int gridRows, int gridCols, float yPosition)
    {
        // This calculation should match how MazeFromAscii originally placed objects.
        // It centers the maze around (0, y, 0) in the platform agent's local space.
        float xShift = (gridCols - 1) * 0.5f * wallScale.x;
        float zShift = (gridRows - 1) * 0.5f * wallScale.z;

        // cellIndex.x is row, cellIndex.y is column
        // MazeFromAscii: (rowCount - 1 - r) for Z, to make row 0 top.
        // So, cellIndex.x = r, cellIndex.y = c
        float worldX = (cellIndex.y * wallScale.x) - xShift;
        float worldZ = ((gridRows - 1 - cellIndex.x) * wallScale.z) - zShift;

        // Position is relative to the platform agent, which is spawned at MazeSpawner's origin.
        Vector3 localPos = new Vector3(worldX, yPosition, worldZ);

        // If platformAgent is already spawned and possibly moved, transform to its local space
        if (platformAgent != null)
        {
            return platformAgent.transform.TransformPoint(localPos);
        }
        // Otherwise, assume it's relative to this spawner's transform (world if spawner is at origin)
        return transform.TransformPoint(localPos);
    }

    public void PopulateFromBlockGrid(bool[,] blockGrid, int gridRows, int gridCols, MazeRuntimeGrid gridToUpdate)
    {
        //float yPositionForCells = 0.5f * wallScale.y; // Assuming walls/triggers are centered vertically
        // Centered on the agent's plane. If agent is at Y=0, cells are at Y=wallScale.y/2
        // However, if floor is at Y=0 (relative to agent), walls should be on top of it.
        // Let's assume prefabs are setup so their base is at their local Y=0.
        // And wallScale.y is their height. So position them at Y = wallScale.y / 2.
        float yPositionForCells = 0; // If prefabs pivots are at their base.
                                     // If pivots are centered, then it's 0.
                                     // Your original MazeFromAscii used 0.5f, implying pivot at base and scale=1, or pivot center and height 1.
                                     // Let's stick to consistency with original:
        if (wallPrefab != null && wallPrefab.GetComponent<Collider>() != null)
        {
            // A common setup is pivot at base, so Y pos is half height if scale is uniform.
            // If wallScale.y is the actual height, and pivot is at center of mesh:
            // yPositionForCells = 0;
            // If pivot is at bottom of mesh:
            yPositionForCells = wallScale.y * 0.5f;
        }
        else
        {
            yPositionForCells = 0.5f; // Fallback from original
        }


        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                Vector2Int cellId = new Vector2Int(r, c);
                Vector3 worldPos = GetWorldPosition(cellId, gridRows, gridCols, yPositionForCells);

                if (blockGrid[r, c]) // True means wall
                {
                    SpawnWall(worldPos, cellId, gridToUpdate);
                }
                else // False means empty, spawn floor trigger
                {
                    SpawnFloorTrigger(worldPos, cellId, gridToUpdate);
                }
            }
        }
    }
}
