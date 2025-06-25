// FILE: Assets/Scripts/Strategies/HierarchicalStrategy.cs
using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

[System.Serializable]
public class HierarchicalStrategy : IStrategy
{
    [Header("Low-Level RL Settings")]
    [Tooltip("How close the ball needs to be to a waypoint to consider it reached.")]
    public float waypointReachedThreshold = 1.0f;
    [Tooltip("Reward for reaching a single waypoint.")]
    public float waypointReward = 1f;

    [Header("Time Penalty Settings")]
    [Tooltip("The total negative reward budget for time, applied if the agent uses its full time budget.")]
    public float totalTimePenaltyBudget = -1.0f;

    [Tooltip("The number of steps the agent is 'allowed' per waypoint before the time penalty becomes significant. Higher values are more forgiving.")]
    public float stepsAllowedPerWaypoint = 75f;


    private StrategicPlatformAgent _agent;
    private IPlanner _planner;
    private List<Vector2Int> _currentPlan;
    private int _currentWaypointIndex;

    // --- Core Methods ---

    public void Initialize(StrategicPlatformAgent agent)
    {
        _agent = agent;
        _planner = new AStarPlanner();
    }

    public void OnEpisodeBegin()
    {
        // 1. Reset the maze environment
        _agent.controller.ResetMaze();

        // 2. Get ball's current grid position and the exit position
        Vector2Int startPos = _agent.controller.spawner.GetPosIdFromWorldRelativePosition(_agent.ball.transform.localPosition);
        Vector2Int exitPos = _agent.controller.exitPosId;

        // 3. Generate the high-level plan using A*
        _planner.GeneratePlan(_agent.controller.grid, startPos, exitPos);
        _currentPlan = _planner.GetPlan();
        
        // 4. Set the first waypoint as the initial goal
        if (_currentPlan != null && _currentPlan.Count > 0)
        {
            _agent.controller.SpawnWaypoints(_currentPlan);
            _currentWaypointIndex = 0;
            // We start by targeting the second node in the path (index 1), since index 0 is the start position.
            if (_currentPlan.Count > 1)
            {
                _currentWaypointIndex = 1;
            }
        }
        else
        {
            Debug.LogError("Failed to generate a plan. The agent will not be able to solve the maze.");
            // If there's no plan, have the agent target its own position so it doesn't get stuck.
            _currentPlan = new List<Vector2Int> { startPos };
            _currentWaypointIndex = 0;
        }
    }

    public void CollectObservations(VectorSensor sensor)
    {
        // This is the input for the low-level RL controller.
        // It needs to know where the ball is relative to its immediate goal (the current waypoint).

        // Get the world position of the current target waypoint
        Vector3 targetWaypointWorldPos = GetCurrentWaypointWorldPosition();

        // 1. Vector from ball to target waypoint (3 observations)
        Vector3 waypointPos2D = new Vector3(targetWaypointWorldPos.x, 0, targetWaypointWorldPos.z);
        Vector3 ballPos2D = new Vector3(_agent.ball.transform.localPosition.x, 0, _agent.ball.transform.localPosition.z);
        sensor.AddObservation(waypointPos2D - ballPos2D);
        Debug.Log($"Direction to target waypoint (2D): {waypointPos2D - ballPos2D}");

        // 2. Ball's linear velocity (3 observations)
        // Helps the agent learn to control the ball's momentum.
        sensor.AddObservation(_agent.m_BallRb.linearVelocity);

        // 3. Platform's current rotation (4 observations)
        // Lets the agent know the current tilt of the platform.
        sensor.AddObservation(_agent.transform.localRotation);
    }

    // In Assets/Scripts/Strategies/HierarchicalStrategy.cs

    public void ProcessActions()
    {
        // --- 1. DYNAMIC Time Penalty ---
        if (_currentPlan != null && _currentPlan.Count > 0)
        {
            // Calculate a total "allowed" number of steps based on the plan length.
            float totalAllowedSteps = _currentPlan.Count * stepsAllowedPerWaypoint;

            // Ensure we don't divide by zero and that the budget is reasonable.
            if (totalAllowedSteps > 1)
            {
                // The penalty is the total budget distributed over the allowed steps.
                float dynamicPenalty = totalTimePenaltyBudget / totalAllowedSteps;
                _agent.AddReward(dynamicPenalty);
                //Debug.Log($"Dynamic Time Penalty Applied: {dynamicPenalty} per step. Total allowed steps: {totalAllowedSteps}");
            }
        }

        // --- 2. Reward Shaping ---
        // Give a small reward for moving the ball in the direction of the current target waypoint.
        Vector3 waypointWorldPos = GetCurrentWaypointWorldPosition();
        Vector3 ballWorldPos = _agent.ball.transform.localPosition;

        // Create 2D versions of the positions for accurate direction calculation
        Vector3 waypointPos2D = new Vector3(waypointWorldPos.x, 0, waypointWorldPos.z);
        Vector3 ballPos2D = new Vector3(ballWorldPos.x, 0, ballWorldPos.z);

        // The direction is now purely horizontal (on the X-Z plane)
        Vector3 directionToTarget = (waypointPos2D - ballPos2D).normalized;
        Vector3 ballVelocity = _agent.m_BallRb.linearVelocity;
        // We only care about horizontal velocity for this reward
        Vector3 ballVelocity2D = new Vector3(ballVelocity.x, 0, ballVelocity.z);

        float directionalReward = Vector3.Dot(ballVelocity2D.normalized, directionToTarget);
        _agent.AddReward(0.01f * directionalReward);

        //UnityEngine.Debug.Log($"Current waipoint pos: {waypointPos2D}, Ball pos: {ballPos2D}, Direction to target (2D): {directionToTarget}, Directional reward: {directionalReward}");

        // --- 3. Check Current Waypoint  ---
        // Only check if the agent has reached the current target waypoint
        float distanceToCurrentWaypoint = Vector2.Distance(
            new Vector2(ballWorldPos.x, ballWorldPos.z), 
            new Vector2(waypointWorldPos.x, waypointWorldPos.z)
        );

        if (distanceToCurrentWaypoint < waypointReachedThreshold)
        {
            // Check if this was the FINAL waypoint in the plan
            if (_currentWaypointIndex == _currentPlan.Count - 1)
            {
                Debug.Log("Success! Final goal reached.");
                _agent.AddReward(waypointReward);
                _agent.RecordSuccess();
                _agent.EndEpisode();
                return; // Episode is over, exit the method.
            }
            // This is an intermediate waypoint
            else
            {
                // Move to the next waypoint in sequence
                _currentWaypointIndex++;
                Debug.Log($"Waypoint {_currentWaypointIndex - 1} reached! New target is waypoint {_currentWaypointIndex}.");
                _agent.AddReward(waypointReward);
            }
        }
    }
    private Vector3 GetCurrentWaypointWorldPosition()
    {
        if (_currentPlan == null || _currentWaypointIndex >= _currentPlan.Count)
        {
            // If something is wrong with the plan, return the ball's own position
            // to prevent errors.
            UnityEngine.Debug.LogWarning("Current plan is null or index is out of bounds. Returning ball's position.");
            return _agent.ball.transform.localPosition;
        }
        
        // Get the grid position of the waypoint
        Vector2Int waypointGridPos = _currentPlan[_currentWaypointIndex];
        // Convert grid position to world position
        return _agent.controller.spawner.GetWorldRelativePosition(waypointGridPos);
    }
}