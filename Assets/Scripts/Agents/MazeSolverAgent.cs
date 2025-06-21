using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;
using System.Diagnostics;

/// <summary>
/// Agent to solve the maze
/// </summary>
public class MazeSolverAgent : PlatformAgent
{
    // position relative to the plane (we take into account orientation)
    // relative position should have y = 0
    [HideInInspector] public Vector3 targetPosition;

    [Header("Distance threshold for defining if the ball reached the exit")]
    public float goalReachedDistanceThreshold = 0.5f;

    [Header("Reward scale for distance delta, with progress rewards")]
    public float progressRewardScale = 0.1f; // check code use

    [Header("Reward scale for distance, with simple distance rewards")]
    public float distanceMagnitudeRewardScale = 0.1f;     // check code use

    [Header("Max Reward value for bfs distance")]
    public float maxRewardBfs = 0.001f;  // should depend on number of timesteps and other set rewards

    private float prevDistanceToExit;
    private BfsResult bfsResult;

    public override void Initialize()
    {
        base.Initialize();
        Vector3 scale = gameObject.transform.localScale;
    }
    private void SetEnvParameters(EnvironmentParameters envParams)
    {
        UnityEngine.Debug.Log(envParams);
        var difficulty = envParams.GetWithDefault("difficulty", 0.5f);
        var seed = (int)envParams.GetWithDefault("maze_seed", 0f);

        int processId = Process.GetCurrentProcess().Id;
        int uniqueSeed = (int)seed + processId;

        controller.mazeGeneratorSeed = uniqueSeed;
        controller.mazeGeneratorDifficulty = difficulty;

        UnityEngine.Debug.Log($"Episode Start for training: Difficulty={difficulty}, MazeSeed={uniqueSeed}");
    }
    public override void OnEpisodeBegin()
    {
        if (Academy.Instance.IsCommunicatorOn)  // if we're training
        {
            // we will be custom-setting the seed with the environment parameters
            // it's still random, but requires the parameter be false
            controller.useRandomSeedForGenerator = false;

            // Get the EnvironmentParameters object from the Academy
            var envParameters = Academy.Instance.EnvironmentParameters;
            SetEnvParameters(envParameters);
        }
        controller.ResetMaze();
        bfsResult = MazePathfinderBFS.SearchBFS(controller.grid, controller.exitPosId.x, controller.exitPosId.y);
    }

    /// <summary>
    /// Gets the position difference between the ball and the exit position, as if the plane was not rotated
    /// </summary>
    /// <returns>Vector from the world exit position to the projected ball position (plane rotated back to rotation = 0)</returns>
    private Vector3 GetBallPositionDifferenceToExit()
    {
        Vector3 currentBallPosition = ball.transform.localPosition;
        Quaternion planeRot = gameObject.transform.localRotation;
        Vector3 projectedPoint = Quaternion.Inverse(planeRot) * currentBallPosition;

        // Add observations for the calculated projected point's coordinates
        Vector3 posDiff = projectedPoint - worldExitPosition;
        return posDiff;
    }
    private Vector2Int GetBallPositionIdDifferenceToExit()
    {
        Vector2Int cell = GetCellId(ball.transform.localPosition);
        Vector2Int exitCell = controller.exitPosId;
        return exitCell - cell;
    }
    private Vector2 GetShiftRelativeToCenterOfCell(Vector3 pos)
    {
        Vector2Int cell = GetCellId(pos);
        Vector3 centerPos = controller.spawner.GetWorldRelativePosition(cell);
        Vector3 actualPos = GetUnrotatedPosition(pos);
        return new Vector2(centerPos.x, centerPos.z) - new Vector2(actualPos.x, actualPos.z);
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // Rotation values from the platform (4d)
        sensor.AddObservation(gameObject.transform.localRotation);

        // Values from the ball: relative position and linear velocity
        Vector2 ballPositionShift = GetShiftRelativeToCenterOfCell(ball.transform.localPosition);
        sensor.AddObservation(ballPositionShift);   // 2d

        // sensor.AddObservation(ball.transform.localPosition); // 3d
        sensor.AddObservation(m_BallRb.linearVelocity); // 3d

        // relative position to the target (2d: x, z)
        // Add observations for the calculated projected point's coordinates
        // Vector3 posDiff = GetBallPositionDifferenceToExit();
        // sensor.AddObservation(new Vector2(posDiff.x, posDiff.z));

        Vector2Int posIdDiff = GetBallPositionIdDifferenceToExit();
        sensor.AddObservation(posIdDiff);
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);

        // Ball's local position relative to Maze
        float dx = ball.transform.localPosition.x;
        float dz = ball.transform.localPosition.z;
        float dy = ball.transform.localPosition.y;

        if (dy < -20f || dy > 20f || Mathf.Abs(dx) > 50f || Mathf.Abs(dz) > 50f)
        {
            SetReward(-1f);   // total steps: 1000
            EndEpisode();
            return;
        }
        else
        {
            // Vector3 posDiff = GetBallPositionDifferenceToExit();
            // float distanceToExit = new Vector2(posDiff.x, posDiff.z).magnitude;
            // if (distanceToExit < goalReachedDistanceThreshold) // If ball is within half a cell width of the exit
            // {
            //     Debug.Log("Goal Reached!");
            //     SetReward(1.0f); // Positive reward for reaching the goal
            //     EndEpisode();
            //     return;
            // }

            // AddReward(RewardBasedOnProgress(distanceToExit));
            // AddReward(-0.001f);

            float normalizedDistanceToExitBfs = GetNormalizedDistanceToExitBFS();
            if (normalizedDistanceToExitBfs == 0)
            {
                UnityEngine.Debug.Log("Goal Reached!");
                SetReward(10.0f); // Positive reward for reaching the goal
                EndEpisode();
                return;
            }
            // Should be negative to make the agent want to end as fast as possible
            AddReward(RewardBasedOnBFS(normalizedDistanceToExitBfs));
        }
    }

    private float RewardBasedOnDistance(float distanceToExit)
    {
        // The "reaching goal" reward should be handled by SetReward() in OnActionReceived
        // before EndEpisode(), as it's a terminal state reward.
        // This function should primarily provide shaping reward.
        float reward = (1.0f / (distanceToExit + 1e-6f)) * distanceMagnitudeRewardScale;
        return reward;
    }
    private float RewardBasedOnProgress(float curDistanceToExit)
    {
        float distanceDelta = prevDistanceToExit - curDistanceToExit;
        prevDistanceToExit = curDistanceToExit;
        return distanceDelta * progressRewardScale;
    }
    private Vector3 GetUnrotatedPosition(Vector3 pos)
    {
        // Get the cell (r, c) where pos is
        // Need to "un"-rotate the position back (as if the plane had no rotation)
        Quaternion planeRot = gameObject.transform.localRotation;
        Vector3 unrotatedPos = Quaternion.Inverse(planeRot) * pos;
        return unrotatedPos;
    }
    private Vector2Int GetCellId(Vector3 pos)
    {
        Vector3 unrotatedPos = GetUnrotatedPosition(pos);
        Vector2Int cell = controller.spawner.GetPosIdFromWorldRelativePosition(unrotatedPos);
        return cell;
    }
    private float GetNormalizedDistanceToExitBFS()
    {
        int maxDistance = controller.grid.RowCount * 2 + controller.grid.ColCount * 2;

        // Get the cell (r, c) where the ball is
        // Need to "un"-rotate the ball back (as if the plane had no rotation)
        Vector2Int ballCell = GetCellId(ball.transform.localPosition);

        int distance = bfsResult.GetDistanceTo(ballCell);
        if (distance == -1)
        {
            UnityEngine.Debug.LogError($"Distance from BFS was -1 (not visited), position: {ballCell}");
        }
        float normalizedDistance = (float)distance / maxDistance;
        return normalizedDistance;
    }
    private float RewardBasedOnBFS(float normalizedDistanceToExitBfs)
    {
        float reward = -normalizedDistanceToExitBfs * maxRewardBfs;
        return reward;
    }
}
