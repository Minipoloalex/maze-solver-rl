using UnityEngine;

/// <summary>
/// Defines the contract for a low-level "worker" controller.
/// Its job is to take a specific goal (like a waypoint) and generate
/// the necessary actions to achieve it.
/// </summary>
public interface IController
{
    /// <summary>
    /// Sets the current target for the controller.
    /// </summary>
    /// <param name="goal">The world-space Vector3 position of the target.</param>
    void SetGoal(Vector3 goal);

    /// <summary>
    /// Checks if the controller has successfully reached its current goal.
    /// </summary>
    /// <param name="currentPosition">The current world-space position of the object being controlled.</param>
    /// <returns>True if the goal has been reached, false otherwise.</returns>
    bool HasReachedGoal(Vector3 currentPosition);

    /// <summary>
    /// Calculates the control action required to move towards the goal.
    /// </summary>
    /// <param name="currentPosition">The current world-space position of the object.</param>
    /// <param name="currentVelocity">The current velocity of the object.</param>
    /// <returns>A Vector2 containing the tilt actions (x, z) for the platform.</returns>
    Vector2 CalculateTilt(Vector3 currentPosition, Vector3 currentVelocity);

    /// <summary>
    /// Resets any internal state of the controller (e.g., integral term for PID).
    /// </summary>
    void Reset();
}