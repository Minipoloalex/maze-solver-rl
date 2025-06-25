using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AStarPlanner : IPlanner
{
    public List<Vector2Int> GeneratePlan(MazeRuntimeGrid grid, Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> plan = AStar.FindPath(grid, start, goal);
        if (plan == null || plan.Count == 0)
        {
            Debug.LogWarning("A* planner could not find a path.");
            return new List<Vector2Int>(); 
        }
        return plan;
    }
}