using Unity.MLAgents.Sensors;
using UnityEngine;

public interface IController
{
    /// <summary>
    /// Initializes the controller with a reference to the agent.
    /// </summary>
    void Initialize(StrategicPlatformAgent agent);

    /// <summary>
    /// Called at the beginning of an episode for any necessary resets.
    /// </summary>
    void OnEpisodeBegin();

    /// <summary>
    /// Sets the immediate goal for the controller to pursue.
    /// </summary>
    void SetGoal(Vector3 goalPosition);

    /// <summary>
    /// Checks if the controller has successfully reached its current goal.
    /// </summary>
    bool HasReachedGoal();
    
    /// <summary>
    /// Called by the strategy when the goal has been successfully reached,
    /// allowing the controller to issue a final reward.
    /// </summary>
    void OnGoalReached();

    /// <summary>
    /// Collects observations specific to this controller's logic for the RL agent.
    /// </summary>
    void CollectObservations(VectorSensor sensor);

    /// <summary>
    /// Processes a single step of the control loop. This includes
    /// reward calculation and any other per-step logic.
    /// </summary>
    void ProcessStep();
}