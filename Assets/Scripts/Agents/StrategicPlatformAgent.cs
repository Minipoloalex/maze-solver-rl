using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class StrategicPlatformAgent : PlatformAgent
{
    public enum StrategyType
    {
        EndToEnd,
        Hierarchical
    }

    [Header("Strategy Selection")]
    [Tooltip("Select the algorithm the agent will use.")]
    public StrategyType selectedStrategy;

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
        controller.ResetMaze();
        strategy.OnEpisodeBegin();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        strategy.CollectObservations(sensor);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        base.OnActionReceived(actions);
        strategy.ProcessActions();
    }

    void FixedUpdate()
    {
        if (ball.transform != null && ball.activeInHierarchy && _ballGridAnchorTransform != null)
        {
            controller.MoveBallAnchor();
        }
    }
}