using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;


/// <summary>
/// Agent to balance the ball: considers how close the ball is to a target (and whether it fell down)
/// </summary>
public class BalancePlatformAgentToTarget : PlatformAgent
{
    // position relative to the plane (we take into account orientation)
    // relative position should have y = 0
    public Vector3 targetPosition;
    public bool randomPositionOnBegin;
    float maxDistSquared;
    double minHeight;
    double xMaxDist;
    double zMaxDist;

    public override void Initialize()
    {
        base.Initialize();

        Vector3 scale = gameObject.transform.localScale;
        maxDistSquared = (scale.x * scale.x) + (scale.z * scale.z);
        minHeight = -0.3 * scale.x;
        xMaxDist = 0.6 * scale.x;
        zMaxDist = 0.6 * scale.z;
    }
    public override void OnEpisodeBegin()
    {
        gameObject.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        gameObject.transform.Rotate(new Vector3(1, 0, 0), Random.Range(-10f, 10f));
        gameObject.transform.Rotate(new Vector3(0, 0, 1), Random.Range(-10f, 10f));
        m_BallRb.linearVelocity = new Vector3(0f, 0f, 0f);
        ball.transform.position = new Vector3(Random.Range(-1.5f, 1.5f), 4f, Random.Range(-1.5f, 1.5f))
            + gameObject.transform.position;

        if (randomPositionOnBegin)
        {
            targetPosition = new Vector3(Random.Range(-4f, 4f), 0, Random.Range(-4f, 4f));
        }
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

        float dx = ball.transform.position.x - gameObject.transform.position.x;
        float dz = ball.transform.position.z - gameObject.transform.position.z;
        float dy = ball.transform.position.y - gameObject.transform.position.y;
        if (dy < -3f || Mathf.Abs(dx) > 6f || Mathf.Abs(dz) > 6f)
        {
            SetReward(-100f);   // total steps: 1000
            EndEpisode();
        }
        else
        {
            float yAbs = Mathf.Abs(dy);
            float errX = dx - targetPosition.x;
            float errZ = dz - targetPosition.z;

            // find out the X and Z relative to the plane's reference
            float xDist = HypotenuseLength(errX, yAbs);
            float zDist = HypotenuseLength(errZ, yAbs);

            errX = errX > 0 ? xDist : -xDist;
            errZ = errZ > 0 ? zDist : -zDist;

            SetReward(RewardBasedOnDistance(errX, errZ));
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
        sensor.AddObservation(targetPosition.x);
        sensor.AddObservation(targetPosition.z);
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Allows testing the balance platform with the arrow keys
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = -Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private float RewardBasedOnDistance(float x, float z)
    {
        float dx = (x - targetPosition.x);
        float dz = (z - targetPosition.z);

        // the larger the distance, the least reward should be given
        float distSquared = dx * dx + dz * dz;

        // Normalize and clamp reward to range [0, 1]
        return -Mathf.Clamp01(distSquared / maxDistSquared);
    }
    // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mathf.Sqrt.html
    private float HypotenuseLength(float a, float b)
    {
        return Mathf.Sqrt(a * a + b * b);
    }
}
