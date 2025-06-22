// FILE: Assets/Scripts/Agent/StrategicPlatformAgent.cs

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class StrategicPlatformAgent : PlatformAgent
{
    // In the Unity Inspector, you can select which strategy to use for this agent.
    public enum StrategyType
    {
        MazeSolver
    }

    [Header("Strategy Selection")]
    [Tooltip("Select the algorithm the agent will use.")]
    public StrategyType selectedStrategy;

    // We use [SerializeReference] to allow the strategy's properties to be edited in the Inspector.
    [SerializeReference]
    public IStrategy strategy;

    public override void Initialize()
    {
        base.Initialize();
        strategy = StrategyFactory.Create(selectedStrategy);
        strategy.Initialize(this);
    }

    public override void OnEpisodeBegin()
    {
        // The maze controller handles resetting the maze itself
        controller.ResetMaze();
        // Delegate strategy-specific reset logic
        strategy.OnEpisodeBegin();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        strategy.CollectObservations(sensor);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // The base class handles the physical tilting
        base.OnActionReceived(actions);
        // The strategy handles rewards and termination
        strategy.ProcessActions();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        strategy.DecideHeuristicActions(in actionsOut);
    }

    void FixedUpdate()
    {
        // This is a core agent function, so it lives here.
        if (ball.transform != null && ball.activeInHierarchy && _ballGridAnchorTransform != null)
        {
            controller.MoveBallAnchor();
        }
    }
}