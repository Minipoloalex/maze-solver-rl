// FILE: Assets/Scripts/Strategies/IPlanner.cs
using System.Collections.Generic;
using UnityEngine;

public interface IPlanner
{
    void GeneratePlan(MazeRuntimeGrid grid, Vector2Int start, Vector2Int goal);
    List<Vector2Int> GetPlan();
    Vector2Int GetNextWaypoint();
    bool IsPlanFinished();
}