using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the contract for a high-level "manager" planner.
/// Its job is to determine the overall path or sequence of subgoals.
/// </summary>
public interface IPlanner
{
    /// <summary>
    /// Generates a plan from a start to an end point.
    /// </summary>
    /// <param name="grid">The maze grid.</param>
    /// <param name="start">The starting grid cell.</param>
    /// <param name="end">The ending grid cell.</param>
    /// <returns>True if a path was found, false otherwise.</returns>
    bool GeneratePlan(MazeRuntimeGrid grid, Vector2Int start, Vector2Int end);

    /// <summary>
    /// Gets the next waypoint from the plan.
    /// </summary>
    /// <returns>The Vector2Int grid coordinate of the next waypoint.</returns>
    Vector2Int GetNextWaypoint();

    /// <summary>
    /// Checks if the plan is complete (all waypoints have been issued).
    /// </summary>
    bool IsPlanComplete();
}