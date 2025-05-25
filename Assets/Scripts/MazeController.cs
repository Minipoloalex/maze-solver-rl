using UnityEngine;

public class MazeController : MonoBehaviour
{
    [Header("Maze Source")]
    public bool generateRandomMaze = true;
    public TextAsset asciiMazeFile; // Used if generateRandomMaze is false

    [Header("Random Maze Generation Settings")]
    public int mazeWidth = 10;  // Number of cells for the generator (not final block grid size)
    public int mazeHeight = 10; // Number of cells for the generator
    public int randomSeed = 0;
    [Range(0f, 1f)]
    public float mazeDifficulty = 0.5f;

    [Header("References")]
    public MazeSpawner spawner;
    public MazeRuntimeGrid runtimeGrid;
    public AsciiMazeLoader asciiLoader; // We will refactor MazeFromAscii into this

    private GameObject platformAgentObject;
    private Vector2Int ballStartCellIndex; // In block grid coordinates (row, col)

    void Start()
    {
        if (spawner == null || runtimeGrid == null || asciiLoader == null)
        {
            Debug.LogError("MazeController is missing critical references!");
            return;
        }
        SetupMaze();
    }

    void SetupMaze()
    {
        spawner.ClearSpawnedChildren();

        platformAgentObject = spawner.SpawnPlatformAgent();
        spawner.platformAgent = platformAgentObject; // For MazeSpawner's internal reference if needed

        bool[,] blockGrid; // true = wall, false = empty
        int gridRows, gridCols;

        if (generateRandomMaze)
        {
            Debug.Log($"Generating random maze: {mazeWidth}x{mazeHeight}, Seed: {randomSeed}, Difficulty: {mazeDifficulty}");
            int[,] generatedMaze = MazeGenerator.Generate(mazeWidth, mazeHeight, randomSeed, mazeDifficulty);
            ConvertGeneratedMazeToBlockGrid(generatedMaze, out blockGrid, out gridRows, out gridCols, out ballStartCellIndex);
        }
        else
        {
            if (asciiMazeFile == null)
            {
                Debug.LogError("AsciiMazeFile is not assigned for loading maze from file!");
                return;
            }
            Debug.Log($"Loading maze from ASCII file: {asciiMazeFile.name}");
            blockGrid = asciiLoader.ParseAsciiMaze(asciiMazeFile, out gridRows, out gridCols, out Vector2Int? initialBallCell);
            if (initialBallCell.HasValue)
            {
                ballStartCellIndex = initialBallCell.Value;
            }
            else
            {
                Debug.LogWarning("No ball start position 'B' found in ASCII maze. Defaulting to (0,0) or first empty space if possible.");
                // Fallback: try to find any empty space, or default to a corner.
                // For simplicity, if (0,0) exists and is empty, use it. Otherwise, needs a robust search.
                ballStartCellIndex = FindFirstEmptyCell(blockGrid, gridRows, gridCols);
            }
        }

        runtimeGrid.Init(gridRows, gridCols);

        // Spawn common structures parented to the platform agent
        GameObject floorObject = spawner.SpawnFloor(gridRows, gridCols, platformAgentObject.transform);
        GameObject wallsContainerObject = spawner.SpawnWallsContainer(platformAgentObject.transform);
        GameObject triggersContainerObject = spawner.SpawnFloorTriggersContainer(platformAgentObject.transform);

        // Populate the grid with walls and floor triggers
        spawner.PopulateFromBlockGrid(blockGrid, gridRows, gridCols, runtimeGrid);

        // Spawn Ball
        Vector3 ballWorldPosition = spawner.GetWorldPosition(ballStartCellIndex, gridRows, gridCols, platformAgentObject.transform.position.y + 0.5f * spawner.ballScale.y); // Ensure Y is appropriate
        GameObject ballObject = spawner.SpawnBall(ballWorldPosition);

        // Initialize Agent with the ball
        PlatformAgent agentScript = platformAgentObject.GetComponent<PlatformAgent>();
        if (agentScript != null)
        {
            agentScript.SetInitialBallMazePosition(ballWorldPosition, ballObject);
        }
        else
        {
            Debug.LogError("PlatformAgent script not found on the spawned platform agent prefab!");
        }
    }
    
    private Vector2Int FindFirstEmptyCell(bool[,] blockGrid, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!blockGrid[r, c]) return new Vector2Int(r, c);
            }
        }
        Debug.LogWarning("No empty cell found in the maze for ball spawning! Defaulting to (0,0). This might place the ball inside a wall.");
        return Vector2Int.zero; // Fallback, could be problematic
    }

    private void ConvertGeneratedMazeToBlockGrid(int[,] generatedMaze, out bool[,] blockGrid, out int blockGridRows, out int blockGridCols, out Vector2Int ballSpawnCell)
    {
        int genHeight = generatedMaze.GetLength(1); // Corresponds to rows in generator's internal array
        int genWidth = generatedMaze.GetLength(0);  // Corresponds to columns in generator's internal array

        blockGridRows = 2 * genHeight + 1;
        blockGridCols = 2 * genWidth + 1;
        blockGrid = new bool[blockGridRows, blockGridCols];

        // Initialize blockGrid: true means wall, false means path. Start with all walls.
        for (int r = 0; r < blockGridRows; r++)
        {
            for (int c = 0; c < blockGridCols; c++)
            {
                blockGrid[r, c] = true; // Wall
            }
        }

        // Carve passages based on the generated maze
        for (int gr = 0; gr < genHeight; gr++) // Generator row
        {
            for (int gc = 0; gc < genWidth; gc++) // Generator col
            {
                int cell_block_r = 2 * gr + 1;
                int cell_block_c = 2 * gc + 1;

                // Carve the cell itself
                blockGrid[cell_block_r, cell_block_c] = false;

                // Carve passage to East if no wall
                if ((generatedMaze[gc, gr] & (int)MazeGenerator.Wall.East) == 0)
                {
                    if (gc < genWidth -1) // ensure not on edge, though generator should handle this
                       blockGrid[cell_block_r, cell_block_c + 1] = false;
                }

                // Carve passage to North if no wall
                // Note: MazeGenerator's +Y (North) corresponds to higher row index in its grid
                // Block grid: higher row index is further down visually if (0,0) is top-left.
                // Our spawner mapping: row 0 is +Z (top of file).
                // Generator North (+Y) -> increase gr. Block grid (2*gr+1, 2*gc+1) means (2*(gr+1)+1, 2*gc+1) is the cell "North"
                // So the passage wall is at (2*gr+1)+1 = 2*gr+2.
                if ((generatedMaze[gc, gr] & (int)MazeGenerator.Wall.North) == 0)
                {
                     if (gr < genHeight - 1) // ensure not on edge
                        blockGrid[cell_block_r + 1, cell_block_c] = false;
                }
            }
        }
        // Default ball spawn: cell (0,0) of generated maze, which is (1,1) in block grid.
        // Generator (col=0, row=0) -> block (r=1, c=1)
        ballSpawnCell = new Vector2Int(1, 1);
        if (blockGrid[1,1]) { // If (1,1) somehow remained a wall (e.g. 0x0 maze gen), find another
             ballSpawnCell = FindFirstEmptyCell(blockGrid, blockGridRows, blockGridCols);
        }
    }
}