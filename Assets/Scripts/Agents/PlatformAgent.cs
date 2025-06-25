using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

/// <summary>
/// Generic platform agent parent class. Holds common references.
/// </summary>
[RequireComponent(typeof(FloorController))]
public abstract class PlatformAgent : Agent
{
    [Header("Agent References")]
    public MazeController controller;
    public Vector3 worldExitPosition;
    public GameObject ball;
    public Transform _ballGridAnchorTransform;
    public FloorController floor;

    [HideInInspector]
    public Rigidbody m_BallRb;
    [HideInInspector]
    protected StatsRecorder m_StatsRecorder;

    public override void Initialize()
    {
        if (ball != null)
        {
            SetBall(ball);
        }
        floor = GetComponent<FloorController>();
        m_StatsRecorder = Academy.Instance.StatsRecorder;
    }

    public virtual void Init(GameObject ball, GameObject ballGridAnchor)
    {
        SetBall(ball);
        _ballGridAnchorTransform = ballGridAnchor.transform;
    }

    private void SetBall(GameObject b)
    {
        ball = b;
        m_BallRb = ball.GetComponent<Rigidbody>();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // This part is common to all strategies: applying the physical tilt.
        float inputX = actionBuffers.ContinuousActions[0];
        float inputZ = actionBuffers.ContinuousActions[1];
        floor.ApplyTilt(inputX, inputZ);
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

}