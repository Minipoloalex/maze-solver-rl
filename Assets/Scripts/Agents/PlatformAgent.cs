using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;

/// <summary>
/// Generic platform agent parent class (to be inherited by others)
/// </summary>
public abstract class PlatformAgent : Agent
{
    public MazeController controller;
    public Vector3 worldExitPosition;
    public GameObject ball;
    protected Rigidbody m_BallRb;
    public override void Initialize()
    {
        if (ball != null)
        {
            SetBall(ball);
        }
    }
    public void Init(GameObject b)
    {
        SetBall(b);
    }
    private void SetBall(GameObject b)
    {
        ball = b;
        m_BallRb = ball.GetComponent<Rigidbody>();
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var actionZ = 2f * Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        var actionX = 2f * Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);

        if ((gameObject.transform.rotation.z < 0.25f && actionZ > 0f) ||
            (gameObject.transform.rotation.z > -0.25f && actionZ < 0f))
        {
            gameObject.transform.Rotate(new Vector3(0, 0, 1), actionZ);
        }

        if ((gameObject.transform.rotation.x < 0.25f && actionX > 0f) ||
            (gameObject.transform.rotation.x > -0.25f && actionX < 0f))
        {
            gameObject.transform.Rotate(new Vector3(1, 0, 0), actionX);
        }
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Allows testing the balance platform with the arrow keys
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = -Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}
