using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators; // For ActionBuffers

/// <summary>
/// Generic platform agent parent class (to be inherited by others)
/// </summary>
public abstract class PlatformAgent : Agent
{
    public GameObject ball; // This can be null initially, will be set
    protected Rigidbody m_BallRb;

    protected Vector3 initialBallMazePosition;
    protected bool initialBallPositionWasSet = false;

    // Called by MazeController after ball is spawned
    public virtual void SetInitialBallMazePosition(Vector3 worldPosition, GameObject ballInstance)
    {
        this.ball = ballInstance; // Assign the ball instance
        if (this.ball != null)
        {
            m_BallRb = this.ball.GetComponent<Rigidbody>();
            if (m_BallRb == null) Debug.LogError("Ball prefab is missing Rigidbody!", this.ball);
        }
        else
        {
            Debug.LogError("SetInitialBallMazePosition called with null ballInstance!", this);
        }

        initialBallMazePosition = worldPosition;
        initialBallPositionWasSet = true;

        if (this.ball != null)
        {
            this.ball.transform.position = initialBallMazePosition;
        }
        // The Initialize method is called by MLAgents system.
        // We ensure ball is set before it might be needed.
    }

    public override void Initialize()
    {
        // Base.Initialize() is fine. m_BallRb should be set if ball was assigned.
        // If ball is set here (e.g. from Inspector for non-dynamic setup), get Rigidbody.
        if (ball != null && m_BallRb == null)
        {
            m_BallRb = ball.GetComponent<Rigidbody>();
            if (m_BallRb == null) Debug.LogError("Ball prefab is missing Rigidbody!", ball);
        }
    }

    // Init is deprecated by SetInitialBallMazePosition
    // public void Init(GameObject b) 
    // {
    //     SetBall(b);
    // }

    // private void SetBall(GameObject b) // Now part of SetInitialBallMazePosition
    // {
    //    ball = b;
    //    m_BallRb = ball.GetComponent<Rigidbody>();
    // }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Allows testing the balance platform with the arrow keys
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = -Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    // Common reset logic for ball physics state
    protected void ResetBallPhysics()
    {
        if (m_BallRb != null)
        {
            m_BallRb.linearVelocity = Vector3.zero;
            m_BallRb.angularVelocity = Vector3.zero;
        }
    }

    // Common reset logic for platform orientation
    protected void ResetPlatformOrientation()
    {
        transform.rotation = Quaternion.identity; // Or initial maze rotation if any
        transform.Rotate(Vector3.right, Random.Range(-10f, 10f));
        transform.Rotate(Vector3.forward, Random.Range(-10f, 10f)); // Use forward for Z-axis tilt if that's the convention
    }
}