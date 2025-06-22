using System.Collections.Generic;
using UnityEngine;

public class AStarPlanner : IPlanner
{
    private List<Vector2Int> _path;
    private int _currentWaypointIndex;

    public bool GeneratePlan(MazeRuntimeGrid grid, Vector2Int start, Vector2Int end)
    {
        _path = AStar.Search(grid, start, end);
        _currentWaypointIndex = 0;
        return _path != null && _path.Count > 0;
    }

    public Vector2Int GetNextWaypoint()
    {
        if (IsPlanComplete())
        {
            // Return the last waypoint if requested again
            return _path[_path.Count - 1];
        }
        return _path[_currentWaypointIndex++];
    }

    public bool IsPlanComplete()
    {
        return _path == null || _currentWaypointIndex >= _path.Count;
    }
}