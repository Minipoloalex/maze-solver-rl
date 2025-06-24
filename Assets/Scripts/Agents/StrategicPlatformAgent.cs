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
    private StatsRecorder m_StatsRecorder;

    public override void Initialize()
    {
        base.Initialize();
        strategy = StrategyFactory.Create(selectedStrategy);
        strategy.Initialize(this);
        m_StatsRecorder = Academy.Instance.StatsRecorder;
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

    // All Record methods should be called before EndEpisode() is called
    // This method should be called from the strategy when the agent succeeds.
    public void RecordSuccess()
    {
        // Record the number of steps taken for this successful episode
        m_StatsRecorder.Add("Episode/StepsToCompletion", StepCount);
        // Record that a success occurred (value 1)
        m_StatsRecorder.Add("Episode/SuccessRate", 1f);
        m_StatsRecorder.Add("Episode/Timeout", 0f);
    }
    // This method should be called from the strategy when the agent fails.
    public void RecordFailure()
    {
        // Record that a failure occurred (value 0)
        m_StatsRecorder.Add("Episode/SuccessRate", 0f);
        m_StatsRecorder.Add("Episode/Timeout", 0f);
    }
    // This method should be called from the strategy when the agent times out.
    public void RecordTimeOut()
    {
        m_StatsRecorder.Add("Episode/SuccessRate", 0f);
        m_StatsRecorder.Add("Episode/Timeout", 1f);
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