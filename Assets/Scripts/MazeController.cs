using UnityEngine;
using UnityEngine.Assertions;

public class MazeController : MonoBehaviour
{
    [Header("Maze Parameters")]
    public bool generateRandomMaze = false;
    public bool allowRuntimeModifications = true; // TODO: in case clicker stuff is slow, do not spawn it (not implemented yet)

    [Header("Maze Random Generation Parameters")]
    public bool useRandomSeedForGenerator = true;
    public int mazeGeneratorSeed = 0;
    [Range(0f, 1f)]
    public float mazeGeneratorDifficulty = 0.2f;
    [Header("Maze width")]
    public int mazeGeneratorMinWidth = 5;
    public int mazeGeneratorMaxWidth = 10;
    [Header("Maze height")]
    public int mazeGeneratorMinHeight = 5;
    public int mazeGeneratorMaxHeight = 10;

    [Header("Generated Maze Information")]
    [Tooltip("Do not manually change these parameters")]
    public Vector2Int exitPosId;
    public MazeRuntimeGrid grid;

    [HideInInspector] public MazeSpawner spawner;
    [HideInInspector] public GameObject wallsOn;
    [HideInInspector] public GameObject wallsOff;
    private GameObject agent;
    private GameObject floor;
    private GameObject ball;
    private GameObject ballGridAnchor;
    
    public Vector2Int Generate()
    {
        Vector2Int ballPosId;

        // Generate the Maze
        if (generateRandomMaze)
        {
            int currentSeed = useRandomSeedForGenerator ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : mazeGeneratorSeed;
            UnityEngine.Random.InitState(currentSeed);

            int genWidth = UnityEngine.Random.Range(mazeGeneratorMinWidth, mazeGeneratorMaxWidth + 1);
            int genHeight = UnityEngine.Random.Range(mazeGeneratorMinHeight, mazeGeneratorMaxHeight + 1);

            this.grid = MazeGenerator.GenerateMazeForRuntimeGrid(
                genWidth,
                genHeight,
                currentSeed,
                mazeGeneratorDifficulty,
                out ballPosId,
                out exitPosId
            );
        }
        else
        {
            AsciiMazeLoader mazeLoader = gameObject.GetComponent<AsciiMazeLoader>();
            this.grid = mazeLoader.ReadGrid(out ballPosId, out exitPosId);
        }
        return ballPosId;
    }

    public void SetupSpawner()
    {
        spawner = gameObject.GetComponent<MazeSpawner>();
        Assert.IsNotNull(spawner, "Spawner must be assigned before calling SetupSpawner.");
        spawner.gridSize = new Vector2Int(grid[0].Length, grid.Length);
    }

    public void Spawn(Vector2Int ballPosId)
    {
        // For each of the maze's components,
        // create a view for them

        this.agent ??= spawner.SpawnPlatformAgent(transform, this, exitPosId);
        this.ball ??= spawner.SpawnBall(this.transform, ballPosId, this);
        this.ballGridAnchor ??= spawner.SpawnBallGridAnchor(agent.transform, ball.transform);

        this.floor = spawner.SpawnFloor(agent.transform);
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

        // Give the agent all the information
        // that it might need in the future
        PlatformAgent agentScript = agent.GetComponent<PlatformAgent>();
        agentScript.Init(this.ball, this.ballGridAnchor);
    }

    public void SwitchWallToFloor(Vector2Int posId, GameObject wallObject)
    {
        grid.RemoveWall(posId);
        spawner.SpawnFloorTrigger(wallsOff.transform, posId, this);
        Destroy(wallObject); // Wall visual disappears, runtime grid updated
    }

    public void SwitchFloorToWall(Vector2Int posId, GameObject floorObject)
    {
        grid.AddWall(posId);
        spawner.SpawnWall(wallsOn.transform, posId, this);
        Destroy(floorObject); // floor trigger gone: replaced by wall, runtime grid updated
    }
    private void MoveBall(Vector2Int newBallPosId)
    {
        Vector3 pos = spawner.GetWorldRelativePosition(newBallPosId);
        ball.transform.localPosition = pos;
    }
    private void GenerateAndSpawnNewMaze()
    {
        Vector2Int ballPosId = Generate();
        SetupSpawner();
        Spawn(ballPosId);
        MoveBall(ballPosId);
    }
    private void CleanUpMaze()
    {
        // Destroy every child of agent: floor, wallsOn, wallsOff (keep agent)
        if (agent != null)
        {
            Destroy(this.wallsOn);
            Destroy(this.wallsOff);
            Destroy(this.floor);
            this.agent.transform.localRotation = Quaternion.identity;
        }

        // nullify explicitly (though I think it makes no difference)
        this.wallsOn = null;
        this.wallsOff = null;
        this.floor = null;
        this.grid = null;
    }

    public void Start()
    {
        GenerateAndSpawnNewMaze();
    }
    public void ResetMaze()
    {
        CleanUpMaze();
        agent.transform.localRotation = Quaternion.identity;
        GenerateAndSpawnNewMaze();
    }
}
