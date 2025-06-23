// FILE: Assets/Scripts/Strategies/AStarPlanner.cs
using System.Collections.Generic;
using UnityEngine;

public class AStarPlanner : IPlanner
{
    private List<Vector2Int> _plan;
    private int _currentWaypointIndex;

    public void GeneratePlan(MazeRuntimeGrid grid, Vector2Int start, Vector2Int goal)
    {
        _plan = AStar.FindPath(grid, start, goal);
        _currentWaypointIndex = 0;

        if (_plan == null || _plan.Count == 0)
        {
            Debug.LogWarning("A* planner could not find a path.");
        }
    }

    public List<Vector2Int> GetPlan()
    {
        return _plan;
    }

    public Vector2Int GetNextWaypoint()
    {
        if (IsPlanFinished())
        {
            // Return the last waypoint if the plan is finished.
            return _plan[_plan.Count - 1];
        }
        return _plan[_currentWaypointIndex++];
    }

    public bool IsPlanFinished()
    {
        return _plan == null || _currentWaypointIndex >= _plan.Count;
    }
}