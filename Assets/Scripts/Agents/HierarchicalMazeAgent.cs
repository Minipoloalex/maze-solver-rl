using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class HierarchicalMazeAgent : PlatformAgent
{
    [Header("Hierarchical Agent Settings")]
    [Tooltip("How many cells to look ahead in the path for the next sub-goal.")]
    public int subGoalLookahead = 5;
    [Tooltip("Distance threshold to consider a sub-goal as reached.")]
    public float goalReachedThreshold = 1.0f;

    // The full path from start to exit
    private List<Vector2Int> _path;
    // The current waypoint the agent is trying to reach
    private Vector3 _currentSubGoalWorldPos;
    private int _currentPathIndex;
    
    // For progress-based rewards
    private float _distanceToSubGoal;

    /// <summary>
    /// Called at the beginning of each episode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // 1. Reset the maze environment
        controller.ResetMaze();

        // 2. Find the start and end points from the controller
        // Note: ResetMaze() generates the grid and ball/exit positions.
        Vector2Int ballStartGridPos = new Vector2Int(-1, -1);
        for(int r = 0; r < controller.grid.Length; r++)
        {
            for(int c = 0; c < controller.grid[r].Length; c++)
            {
                // This is a simplification; a more robust way to get ball pos is needed.
                // We'll assume the ball's initial transform position can be mapped back to grid coords.
            }
        }
        // For this example, let's get start/exit directly from the controller after generation
        Vector2Int startPos = controller.spawner.WorldToGridPos(ball.transform.position); // You'll need to implement WorldToGridPos
        Vector2Int exitPos = controller.exitPosId;

        // 3. Use the Pathfinder to get the list of waypoints
        _path = Pathfinder.FindPath(controller.grid, startPos, exitPos);

        if (_path == null || _path.Count == 0)
        {
            Debug.LogWarning("No path found. Ending episode.");
            EndEpisode();
            return;
        }

        // 4. Set the first sub-goal
        _currentPathIndex = 0;
        SetNextSubGoal();

        _distanceToSubGoal = Vector3.Distance(GetProjectedBallPosition(), _currentSubGoalWorldPos);
    }

    /// <summary>
    /// Sets the next sub-goal for the agent to target.
    /// </summary>
    private void SetNextSubGoal()
    {
        // Move the index ahead
        _currentPathIndex = Mathf.Min(_currentPathIndex + subGoalLookahead, _path.Count - 1);
        
        // Get the grid position and convert to world coordinates
        Vector2Int subGoalGridPos = _path[_currentPathIndex];
        _currentSubGoalWorldPos = controller.spawner.GetWorldRelativePosition(subGoalGridPos);

        Debug.Log($"New sub-goal set at index {_currentPathIndex}: {subGoalGridPos}");
    }

    /// <summary>
    /// Gets the ball's position projected onto the horizontal plane of the maze.
    /// </summary>
    private Vector3 GetProjectedBallPosition()
    {
        // This helper calculates the ball's position as if the platform were not tilted.
        Quaternion inversePlatformRot = Quaternion.Inverse(transform.localRotation);
        return inversePlatformRot * ball.transform.localPosition;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Platform rotation (4 observations)
        sensor.AddObservation(transform.localRotation);

        // Ball's local position and velocity (6 observations)
        sensor.AddObservation(ball.transform.localPosition);
        sensor.AddObservation(m_BallRb.linearVelocity);

        // Vector from projected ball position to the CURRENT sub-goal (3 observations)
        Vector3 projectedBallPos = GetProjectedBallPosition();
        Vector3 toSubGoal = _currentSubGoalWorldPos - projectedBallPos;
        sensor.AddObservation(toSubGoal);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Apply tilt to the platform
        base.OnActionReceived(actions);

        // Check for failure (ball fell off)
        if (ball.transform.localPosition.y < -5f)
        {
            SetReward(-1.0f);
            EndEpisode();
            return;
        }

        // Calculate distance to the current sub-goal
        float currentDist = Vector3.Distance(GetProjectedBallPosition(), _currentSubGoalWorldPos);

        // --- REWARD LOGIC ---
        
        // 1. Reward for making progress towards the current sub-goal
        float progressReward = (_distanceToSubGoal - currentDist);
        AddReward(progressReward * 0.1f);
        _distanceToSubGoal = currentDist;
        
        // 2. Small penalty for existing to encourage speed
        AddReward(-0.001f);

        // 3. Check if the sub-goal is reached
        if (currentDist < goalReachedThreshold)
        {
            // Check if this is the FINAL goal
            if (_currentPathIndex == _path.Count - 1)
            {
                Debug.Log("Final Goal Reached!");
                SetReward(1.0f);
                EndEpisode();
            }
            else
            {
                Debug.Log("Sub-goal reached!");
                AddReward(0.5f); // Reward for reaching an intermediate step
                SetNextSubGoal(); // Set the next target
            }
        }
    }
}