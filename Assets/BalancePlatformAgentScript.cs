using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;

public class BalancePlatformAgentScript : Agent
{
    public GameObject ball;
    Rigidbody m_BallRb;

    public override void Initialize()
    {
        m_BallRb = ball.GetComponent<Rigidbody>();
        // m_ResetParams = Academy.Instance.EnvironmentParameters;
        // SetResetParameters();
    }
    public override void OnEpisodeBegin()
    {
        gameObject.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        gameObject.transform.Rotate(new Vector3(1, 0, 0), Random.Range(-10f, 10f));
        gameObject.transform.Rotate(new Vector3(0, 0, 1), Random.Range(-10f, 10f));
        m_BallRb.linearVelocity = new Vector3(0f, 0f, 0f);
        ball.transform.position = new Vector3(Random.Range(-1.5f, 1.5f), 4f, Random.Range(-1.5f, 1.5f))
            + gameObject.transform.position;
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
        if ((ball.transform.position.y - gameObject.transform.position.y) < -3f ||
            Mathf.Abs(ball.transform.position.x - gameObject.transform.position.x) > 6f ||
            Mathf.Abs(ball.transform.position.z - gameObject.transform.position.z) > 6f)
        {
            SetReward(-1f);
            EndEpisode();
        }
        else
        {
            SetReward(0.1f);
        }
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // Rotation values from the platform
        sensor.AddObservation(gameObject.transform.rotation.x);
        sensor.AddObservation(gameObject.transform.rotation.z);
        // Values from the ball: relative position and linear velocity
        sensor.AddObservation(ball.transform.position - gameObject.transform.position);
        sensor.AddObservation(m_BallRb.linearVelocity);
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = -Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}
