using System.Collections.Generic;
using UnityEngine;

public interface IPlanner
{
    /// <summary>
    /// Generates a plan from a start to a goal position.
    /// </summary>
    /// <param name="grid">The maze grid to plan on.</param>
    /// <param name="start">The starting grid coordinate.</param>
    /// <param name="goal">The goal grid coordinate.</param>
    /// <returns>A list of Vector2Int waypoints representing the plan. Returns an empty list if no path is found.</returns>
    List<Vector2Int> GeneratePlan(MazeRuntimeGrid grid, Vector2Int start, Vector2Int goal);
}