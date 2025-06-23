// FILE: Assets/Scripts/Controllers/RLController.cs
using UnityEngine;

public class RLController : IController
{
    private Vector2Int _currentGoal;

    public void SetGoal(Vector2Int waypoint)
    {
        _currentGoal = waypoint;
        Debug.Log($"RLController: New goal set to {waypoint}");
    }

    public bool HasReachedGoal()
    {
        // Placeholder logic: for now, we will pretend we reach the goal instantly
        // to allow the high-level planner to step through its waypoints for visualization.
        return true;
    }
}