using UnityEngine;
using UnityEngine.Assertions;

public class MazeController : MonoBehaviour
{
    [Header("Maze Parameters")]
    public bool generateRandomMaze = true;
    public bool allowRuntimeModifications = true; // TODO: in case clicker stuff is slow, do not spawn it

    public MazeRuntimeGrid grid;
    [HideInInspector] public MazeSpawner spawner;

    [HideInInspector] public GameObject wallsOn;
    [HideInInspector] public GameObject wallsOff;

    public void Generate()
    {
        // Generate the Maze
        // TODO: must include random generated maze and others
        // after initializing the maze, the walls need to be added in
        this.grid = new MazeRuntimeGrid(10, 10);
        grid.AddWall(new (1, 8));
        grid.AddWall(new (1, 7));
        grid.AddWall(new (1, 6));
        grid.AddWall(new (1, 5));
        grid.AddWall(new (1, 4));
        grid.AddWall(new (1, 3));
        grid.AddWall(new (1, 2));
        grid.AddWall(new (1, 1));
        grid.AddWall(new (2, 1));
        grid.AddWall(new (3, 1));
        grid.AddWall(new (4, 1));
        grid.AddWall(new (5, 1));
        grid.AddWall(new (6, 1));
        grid.AddWall(new (7, 1));
        grid.AddWall(new (8, 1));
        
        grid.AddWall(new (8, 8));
        grid.AddWall(new (8, 7));
        grid.AddWall(new (8, 6));
        grid.AddWall(new (8, 5));
        grid.AddWall(new (8, 4));
        grid.AddWall(new (8, 3));
        grid.AddWall(new (8, 2));
        grid.AddWall(new (2, 8));
        grid.AddWall(new (3, 8));
        grid.AddWall(new (4, 8));
        grid.AddWall(new (5, 8));
        grid.AddWall(new (6, 8));
        grid.AddWall(new (7, 8));
        grid.AddWall(new (8, 8));
    }

    public void SetupSpawner()
    {
        spawner = gameObject.GetComponent<MazeSpawner>();
        Assert.IsNotNull(spawner, "Spawner must be assigned before calling SetupSpawner.");
        spawner.gridSize = new Vector2Int(10, 10); // TODO
        // TODO
    }

    public void Spawn()
    {
        // For each of the maze's components,
        // create a view for them
        GameObject agent = spawner.SpawnPlatformAgent(transform);
        GameObject floor = spawner.SpawnFloor(agent.transform);
        this.wallsOn = spawner.SpawnWallsContainer(agent.transform);
        this.wallsOff = spawner.SpawnFloorTriggersContainer(agent.transform);
        for (int row = 0; row < grid.Length; row++)
        {
            for (int col = 0; col < grid[row].Length; col++)
            {
                var posId = new Vector2Int(row, col);
                if (grid[row][col])
                {
                    // Spawn a Wall
                    spawner.SpawnWall(wallsOn.transform, posId, this);
                }
                else
                {
                    // Spawn a Floor
                    spawner.SpawnFloorTrigger(wallsOff.transform, posId, this);
                }
            }
        }
        // TODO: generate the balls position?
        GameObject ball = spawner.SpawnBall(this.transform, new Vector2Int(5, 5), this);

        // Give the agent all the information
        // that it might need in the future
        PlatformAgent agentScript = agent.GetComponent<PlatformAgent>();
        agentScript.Init(ball);
    }

    public void SwitchWallToFloor(Vector2Int posId, GameObject wallObject)
    {
        grid[posId.x][posId.y] = false;
        spawner.SpawnFloorTrigger(wallsOff.transform, posId, this);
        Destroy(wallObject); // Wall visual disappears, runtime grid updated
    }

    public void SwitchFloorToWall(Vector2Int posId, GameObject floorObject)
    {
        grid[posId.x][posId.y] = true;
        spawner.SpawnWall(wallsOn.transform, posId, this);
        Destroy(floorObject); // floor trigger gone: replaced by wall, runtime grid updated
    }

    public void Start()
    {
        Generate();
        SetupSpawner();
        Spawn();
    }
}
