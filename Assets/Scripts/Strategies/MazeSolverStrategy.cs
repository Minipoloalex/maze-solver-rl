// FILE: Assets/Scripts/Strategies/MazeSolverStrategy.cs

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Diagnostics;

[System.Serializable]
public class MazeSolverStrategy : IStrategy
{
    [Header("Rewards")]
    public float goalReachedDistanceThreshold = 1.0f;
    public float maxRewardBfs = 0.001f;

    private StrategicPlatformAgent _agent;
    private BfsResult _bfsResult;

    public void Initialize(StrategicPlatformAgent agent)
    {
        _agent = agent;
    }

    public void OnEpisodeBegin()
    {
        if (Academy.Instance.IsCommunicatorOn)  // if we're training
        {
            // we will be custom-setting the seed with the environment parameters
            // it's still random, but requires the parameter be false
            _agent.controller.useRandomSeedForGenerator = false;

            // Get the EnvironmentParameters object from the Academy
            var envParameters = Academy.Instance.EnvironmentParameters;
            SetEnvParameters(envParameters);
        }
        _agent.controller.ResetMaze();
        _bfsResult = MazePathfinderBFS.SearchBFS(_agent.controller.grid, _agent.controller.exitPosId.x, _agent.controller.exitPosId.y);
    }

    private void SetEnvParameters(EnvironmentParameters envParams)
    {
        UnityEngine.Debug.Log(envParams);
        var difficulty = envParams.GetWithDefault("difficulty", 0.5f);
        var seed = (int)envParams.GetWithDefault("maze_seed", 0f);

        int processId = Process.GetCurrentProcess().Id;
        int uniqueSeed = (int)seed + processId;

        _agent.controller.mazeGeneratorSeed = uniqueSeed;
        _agent.controller.mazeGeneratorDifficulty = difficulty;

        UnityEngine.Debug.Log($"Episode Start for training: Difficulty={difficulty}, MazeSeed={seed}");
    }

    public void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_agent.gameObject.transform.localRotation); // 4d
        sensor.AddObservation(GetShiftRelativeToCenterOfCell(_agent.ball.transform.localPosition)); // 2d
        sensor.AddObservation(_agent.m_BallRb.linearVelocity); // 3d
        sensor.AddObservation(GetBallPositionIdDifferenceToExit()); // 2d
    }

    public void ProcessActions()
    {
        Vector3 localBallPos = _agent.ball.transform.localPosition;
        if (localBallPos.y < -20f || Mathf.Abs(localBallPos.x) > 50f || Mathf.Abs(localBallPos.z) > 50f)
        {
            _agent.SetReward(-1.0f);
            _agent.EndEpisode();
            return;
        }

        float normalizedDistanceToExitBfs = GetNormalizedDistanceToExitBFS();
        if (normalizedDistanceToExitBfs == 0) // BFS distance of 0 means we are at the target cell
        {
            UnityEngine.Debug.Log("Goal Reached!");
            _agent.SetReward(10.0f);
            _agent.EndEpisode();
            return;
        }
        
        // Small negative reward based on BFS distance to encourage pathfinding and speed
        _agent.AddReward(-normalizedDistanceToExitBfs * maxRewardBfs);
    }

    #region Helper Methods
    private float GetNormalizedDistanceToExitBFS()
    {
        int maxDistance = _agent.controller.grid.RowCount * 2 + _agent.controller.grid.ColCount * 2;

        // Get the cell (r, c) where the ball is
        // Need to "un"-rotate the ball back (as if the plane had no rotation)
        Vector2Int ballCell = GetCellId(_agent.ball.transform.localPosition);

        int distance = _bfsResult.GetDistanceTo(ballCell);
        if (distance == -1)
        {
            UnityEngine.Debug.LogError($"Distance from BFS was -1 (not visited), position: {ballCell}");
        }
        return (float)distance / maxDistance;
    }


    private Vector2Int GetCellId(Vector3 localPos)
    {
        Quaternion planeRot = _agent.gameObject.transform.localRotation;
        Vector3 unrotatedPos = Quaternion.Inverse(planeRot) * localPos;
        return _agent.controller.spawner.GetPosIdFromWorldRelativePosition(unrotatedPos);
    }
    private Vector2Int GetBallPositionIdDifferenceToExit()
    {
        return _agent.controller.exitPosId - GetCellId(_agent.ball.transform.localPosition);
    }

    private Vector2 GetShiftRelativeToCenterOfCell(Vector3 pos)
    {
        Vector2Int cell = GetCellId(pos);
        Vector3 centerPos = _agent.controller.spawner.GetWorldRelativePosition(cell);
        Vector3 actualPos = GetUnrotatedPosition(pos);
        return new Vector2(centerPos.x, centerPos.z) - new Vector2(actualPos.x, actualPos.z);
    }

    private Vector3 GetUnrotatedPosition(Vector3 pos)
    {
        // Get the cell (r, c) where pos is
        // Need to "un"-rotate the position back (as if the plane had no rotation)
        Quaternion planeRot = _agent.transform.localRotation;
        Vector3 unrotatedPos = Quaternion.Inverse(planeRot) * pos;
        return unrotatedPos;
    }
    #endregion

    public void DecideHeuristicActions(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}