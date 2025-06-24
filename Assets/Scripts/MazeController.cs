using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;
public class MazeController : MonoBehaviour
{
    [Header("Maze Parameters")]
    public bool generateRandomMaze = false;
    public bool allowRuntimeModifications = true; // TODO: in case clicker stuff is slow, do not spawn it (not implemented yet)
    [Tooltip("Adds a border of walls around the maze. Useful for Grid Sensors. Should depend on the size of the grid sensors")]
    public int mazePadding = 1;

    [Header("Maze Random Generation Parameters")]
    [Tooltip("Whether to generate a random maze or use the given seed")]
    public bool useRandomSeedForGenerator = true;
    [Tooltip("Seed to use for maze generation in case we should not generate a new random maze")]
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
    private GameObject _waypointsContainer; 
    private Dictionary<Vector2Int, GameObject> _waypointObjects;

    private GameObject agent;
    private GameObject floor;
    private GameObject ball;
    private GameObject ballGridAnchor;
    private GameObject exitPad;

    private Rigidbody ballRb; // for resetting ball velocity

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

        // Add padding to the generated grid if required
        if (this.mazePadding > 0)
        {
            this.grid = MazePadding.Pad(this.grid, this.mazePadding, ref ballPosId, ref exitPosId);
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
        if (this.agent == null)
        {
            this.agent = spawner.SpawnPlatformAgent(transform, this, exitPosId);
            this.ball ??= spawner.SpawnBall(this.transform, ballPosId, this);
            this.ballGridAnchor = agent.transform.Find("BallGridAnchor").gameObject;
            this.MoveBallAnchor();

            // Give the agent all the information
            // that it might need in the future
            PlatformAgent agentScript = agent.GetComponent<PlatformAgent>();
            agentScript.Init(this.ball, this.ballGridAnchor);
        }

        this.floor = spawner.SpawnFloor(agent.transform);
        this.wallsOn = spawner.SpawnWallsContainer(agent.transform);
        this.wallsOff = spawner.SpawnFloorTriggersContainer(agent.transform);
        this._waypointsContainer = spawner.SpawnWaypointsContainer(agent.transform);
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
        this.exitPad = spawner.SpawnExitPad(agent.transform, exitPosId);
    }

     public void SpawnWaypoints(List<Vector2Int> waypoints)
    {
        if (_waypointsContainer == null)
        {
            Debug.LogError("Waypoints container is not initialized!");
            return;
        }
        foreach (var waypoint in waypoints)
        {
            spawner.SpawnWaypointMarker(_waypointsContainer.transform, waypoint);
        }
    }

     public void RemoveWaypoint(Vector2Int waypointPos)
    {
        if (_waypointObjects != null && _waypointObjects.ContainsKey(waypointPos))
        {
            GameObject waypointObj = _waypointObjects[waypointPos];
            if (waypointObj != null)
            {
                Destroy(waypointObj);
            }
            _waypointObjects.Remove(waypointPos);
        }
    }

    public GameObject SpawnWaypointsContainer(Transform parent)
    {
        var container = new GameObject("Waypoints");
        container.transform.SetParent(parent);
        container.transform.position = parent.transform.position;
        return container;
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
    public void MoveBallAnchor()
    {
        Vector3 ballLocalPosOnPlatform = agent.transform.InverseTransformPoint(ball.transform.position);
        ballGridAnchor.transform.localPosition = ballLocalPosOnPlatform;
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
            Destroy(this.exitPad);
            Destroy(this._waypointsContainer);
            this.agent.transform.localRotation = Quaternion.identity;
        }
        ResetBallVelocity();

        // nullify explicitly (though I think it makes no difference)
        this.wallsOn = null;
        this.wallsOff = null;
        this.floor = null;
        this.exitPad = null;
        this.grid = null;
    }

    public void Start()
    {
        GenerateAndSpawnNewMaze();
        ballRb = ball.GetComponent<Rigidbody>();
    }
    public void ResetBallVelocity()
    {
        // Reset the ball's velocity and angular velocity
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }
    public void ResetMaze()
    {
        CleanUpMaze();
        agent.transform.localRotation = Quaternion.identity;
        GenerateAndSpawnNewMaze();
    }
}
