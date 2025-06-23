// FILE: Assets/Scripts/Controllers/IController.cs
using UnityEngine;

public interface IController
{
    void SetGoal(Vector2Int waypoint);
    bool HasReachedGoal();
    // In a real RL setup, this would also likely return an action.
    // For now, we'll keep it simple as the diagram suggests.
}