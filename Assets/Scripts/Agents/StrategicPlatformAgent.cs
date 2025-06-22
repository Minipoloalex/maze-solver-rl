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
        MazeSolver,
        Hierarchical
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

     void FixedUpdate()
    {
        UnityEngine.Debug.Log("StrategicPlatformAgent FixedUpdate called");

        // This is a core agent function to keep the grid sensor anchor updated.
        if (ball.transform != null && ball.activeInHierarchy && _ballGridAnchorTransform != null)
        {
            controller.MoveBallAnchor();
        }

        if (strategy is HierarchicalStrategy hierarchicalStrategy)
        {
            // 1. Get the required tilt action from the PID controller
            Vector2 tilt = hierarchicalStrategy.GetTiltControl();

            // 2. Apply that tilt to the floor's Rigidbody
            floor.ApplyTilt(tilt.x, tilt.y);

            // 3. Manually call ProcessActions to update the waypoint logic (state machine)
            //    We do this here because we are not using the standard OnActionReceived loop.
            strategy.ProcessActions();

            UnityEngine.Debug.Log($"StrategicPlatformAgent FixedUpdate: Applied tilt ({tilt.x}, {tilt.y})");
        }
    }
    
}