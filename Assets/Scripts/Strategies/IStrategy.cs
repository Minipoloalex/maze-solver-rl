// FILE: Assets/Scripts/Strategies/IStrategy.cs

using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public interface IStrategy
{
    /// <summary>
    /// Called once when the agent initializes to set up the strategy.
    /// </summary>
    void Initialize(StrategicPlatformAgent agent);

    /// <summary>
    /// Logic to execute when a new episode begins (e.g., reset paths, goals).
    /// </summary>
    void OnEpisodeBegin();

    /// <summary>
    /// Defines what observations the agent collects for this specific strategy.
    /// </summary>
    void CollectObservations(VectorSensor sensor);

    /// <summary>
    /// Defines the rewards and episode termination conditions for this strategy.
    /// This is where the core logic of the algorithm is implemented.
    /// </summary>
    void ProcessActions();
}