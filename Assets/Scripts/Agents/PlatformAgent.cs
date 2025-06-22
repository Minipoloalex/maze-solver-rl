// FILE: Assets/Scripts/Agent/PlatformAgent.cs

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

    [HideInInspector]
    public Rigidbody m_BallRb;
    [HideInInspector]
    public FloorController floor;

    public override void Initialize()
    {
        if (ball != null)
        {
            SetBall(ball);
        }
        floor = GetComponent<FloorController>();
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
}