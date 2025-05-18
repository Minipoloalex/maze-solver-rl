using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;

/// <summary>
/// Generic platform agent parent class (to be inherited by others)
/// </summary>
public class PlatformAgent : Agent
{
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
}
