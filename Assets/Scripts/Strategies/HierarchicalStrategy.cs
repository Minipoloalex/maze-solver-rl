// FILE: Assets/Scripts/Strategies/HierarchicalStrategy.cs
using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

[System.Serializable]
public class HierarchicalStrategy : IStrategy
{
    private StrategicPlatformAgent _agent;
    private IPlanner _planner;
    private IController _controller;
    private List<Vector2Int> _currentPlan;

    public void Initialize(StrategicPlatformAgent agent)
    {
        _agent = agent;
        _planner = new AStarPlanner();
        _controller = new RLController(); // Using the placeholder for now
    }

    public void OnEpisodeBegin()
    {
        // Reset and generate a new maze
        _agent.controller.ResetMaze();

        // Get start and end positions for the planner
        Vector2Int startPos = _agent.controller.spawner.GetPosIdFromWorldRelativePosition(_agent.ball.transform.localPosition);
        Vector2Int exitPos = _agent.controller.exitPosId;

        // Generate the high-level plan
        _planner.GeneratePlan(_agent.controller.grid, startPos, exitPos);
        _currentPlan = _planner.GetPlan();

        // --- Waypoint Visualization ---
        if (_currentPlan != null && _currentPlan.Count > 0)
        {
            Debug.Log($"A* Plan Generated with {_currentPlan.Count} waypoints.");
            _agent.controller.SpawnWaypoints(_currentPlan);
        }
        else
        {
            Debug.LogError("Failed to generate a plan. No waypoints to visualize.");
        }
    }

    public void CollectObservations(VectorSensor sensor)
    {
        // Observations will be defined in the next phase for the RL controller.
    }

    public void ProcessActions()
    {
        // High-level logic will be added here in the next phase.
        // For now, this method can remain empty as we are only visualizing the initial plan.
    }
}