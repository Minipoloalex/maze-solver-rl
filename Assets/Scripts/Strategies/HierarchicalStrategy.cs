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
    public float waypointReward = 0.2f;
    [Tooltip("Final reward for reaching the exit.")]
    public float finalGoalReward = 10.0f;
    [Tooltip("Penalty for the ball falling off the maze.")]
    public float fallOffPenalty = -1.0f;

    private StrategicPlatformAgent _agent;
    private IPlanner _planner;
    
    // State for the current plan
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
        Vector3 toTarget = targetWaypointWorldPos - _agent.ball.transform.position;
        sensor.AddObservation(toTarget);

        // 2. Ball's linear velocity (3 observations)
        // Helps the agent learn to control the ball's momentum.
        sensor.AddObservation(_agent.m_BallRb.linearVelocity);

        // 3. Platform's current rotation (4 observations)
        // Lets the agent know the current tilt of the platform.
        sensor.AddObservation(_agent.transform.localRotation);
    }

    // In HierarchicalStrategy.cs, replace the existing ProcessActions method with this one.

// In HierarchicalStrategy.cs, replace the existing ProcessActions method with this one.

        public void ProcessActions()
        {
            // --- Termination & Directional Reward (unchanged) ---
            Vector3 localBallPos = _agent.ball.transform.localPosition;
            if (localBallPos.y < -10f)
            {
                _agent.SetReward(fallOffPenalty);
                _agent.EndEpisode();
                return;
            }
            Vector3 targetWaypointWorldPos = GetCurrentWaypointWorldPosition();
            Vector3 directionToTarget = (targetWaypointWorldPos - _agent.ball.transform.position).normalized;
            Vector3 ballVelocity = _agent.m_BallRb.linearVelocity;
            float directionalReward = Vector3.Dot(ballVelocity.normalized, directionToTarget);
            _agent.AddReward(0.01f * directionalReward);

            // --- NEW: Smarter Waypoint Checking ---
            // Instead of only checking the current target, we loop through all future waypoints.
            // This allows the agent to "skip" waypoints if it gets ahead of its plan.
            for (int i = _currentWaypointIndex; i < _currentPlan.Count; i++)
            {
                Vector2Int waypointGridPos = _currentPlan[i];
                Vector3 waypointWorldPos = _agent.controller.spawner.GetWorldRelativePosition(waypointGridPos);
                float distanceToWaypoint = Vector3.Distance(_agent.ball.transform.position, waypointWorldPos);

                if (distanceToWaypoint < waypointReachedThreshold)
                {
                    // The agent has reached waypoint 'i'.
                    
                    // Check if this was the final waypoint in the entire plan.
                    if (i == _currentPlan.Count - 1)
                    {
                        Debug.Log("Success! Final goal reached.");
                        _agent.SetReward(finalGoalReward);
                        _agent.EndEpisode();
                        return; // Exit the method immediately since the episode is over.
                    }
                    else
                    {
                        // This was an intermediate waypoint.
                        // We update the index to the one AFTER the one we just reached.
                        int newWaypointIndex = i + 1;
                        
                        // Optional: Give a reward for each waypoint skipped.
                        int waypointsSkipped = newWaypointIndex - _currentWaypointIndex;
                        _agent.AddReward(waypointsSkipped * waypointReward);

                        _currentWaypointIndex = newWaypointIndex;
                        Debug.Log($"Waypoint {i} reached! New target is waypoint {_currentWaypointIndex}.");
                        
                        // We've found the closest future waypoint, no need to check further this frame.
                        // We'll continue the loop from the new index on the next frame.
                        break; 
                    }
                }
            }
        }
    private Vector3 GetCurrentWaypointWorldPosition()
    {
        if (_currentPlan == null || _currentWaypointIndex >= _currentPlan.Count)
        {
            // If something is wrong with the plan, return the ball's own position
            // to prevent errors.
            return _agent.ball.transform.position;
        }
        
        // Get the grid position of the waypoint
        Vector2Int waypointGridPos = _currentPlan[_currentWaypointIndex];
        // Convert grid position to world position
        return _agent.controller.spawner.GetWorldRelativePosition(waypointGridPos);
    }
}