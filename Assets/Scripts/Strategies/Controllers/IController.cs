// FILE: Assets/Scripts/Controllers/IController.cs
using UnityEngine;

public interface IController
{
    void SetGoal(Vector2Int waypoint);
    bool HasReachedGoal();
}