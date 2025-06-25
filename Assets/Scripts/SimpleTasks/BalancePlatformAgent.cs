using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;

public class BalancePlatformAgent : PlatformAgent
{
    public override void OnEpisodeBegin()
    {
        m_BallRb.linearVelocity = Vector3.zero;
        ball.transform.localPosition = new Vector3(Random.Range(-1.5f, 1.5f), 4f, Random.Range(-1.5f, 1.5f));
        gameObject.transform.localEulerAngles = Vector3.zero;
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);

        float dx = ball.transform.localPosition.x;
        float dy = ball.transform.localPosition.y;
        float dz = ball.transform.localPosition.z;
        if (dy < -3f || Mathf.Abs(dx) > 6f || Mathf.Abs(dz) > 6f)    // if outside the platform (depends on the platform size)
        {
            SetReward(-1f);
            RecordFailure();
            EndEpisode();
            return;
        }
        else
        {
            SetReward(0.1f);    // reward for surviving
        }

        if (MaxStep > 0 && StepCount >= MaxStep)
        {
            RecordSuccess();
        }
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // Rotation values from the platform
        sensor.AddObservation(gameObject.transform.localRotation.x);
        sensor.AddObservation(gameObject.transform.localRotation.z);
        // Values from the ball: relative position and linear velocity
        sensor.AddObservation(ball.transform.localPosition);
        sensor.AddObservation(m_BallRb.linearVelocity);
    }
}
