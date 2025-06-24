using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;
using UnityEditor.ShaderGraph.Internal;
using Unity.VisualScripting;

public class TargetBalancePlatformAgent : PlatformAgent
{
    [Tooltip("Set automatically for training and inference")]
    public Vector2 targetPosition;

    [Tooltip("The maximum absolute coordinate for the target position")]
    [Range(0f, 3f)]
    public float maxAbsolutePosition = 3f;
    private readonly float maxDistance = 20f;
    private readonly float failureReward = -100f;

    [Header("Point visualization of the target position")]
    public GameObject point;
    public override void OnEpisodeBegin()
    {
        m_BallRb.linearVelocity = Vector3.zero;
        ball.transform.localPosition = new Vector3(Random.Range(-1.5f, 1.5f), 4f, Random.Range(-1.5f, 1.5f));
        gameObject.transform.localEulerAngles = Vector3.zero;

        float randX = Random.Range(-maxAbsolutePosition, maxAbsolutePosition);
        float randZ = Random.Range(-maxAbsolutePosition, maxAbsolutePosition);
        targetPosition = new Vector2(randX, randZ);
        point.transform.localPosition = new Vector3(targetPosition.x, 0.5f, targetPosition.y);
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);

        float dx = ball.transform.localPosition.x;
        float dy = ball.transform.localPosition.y;
        float dz = ball.transform.localPosition.z;
        if (dy < -3f || Mathf.Abs(dx) > 6f || Mathf.Abs(dz) > 6f)    // if outside the platform (depends on the platform size)
        {
            SetReward(failureReward);
            RecordFailure();
            EndEpisode();
            return;
        }
        else
        {
            Vector3 unrotatedPosition = GetUnrotatedPosition(ball.transform.localPosition);
            float errX = Mathf.Abs(unrotatedPosition.x - targetPosition.x);
            float errZ = Mathf.Abs(unrotatedPosition.z - targetPosition.y);
            float errNormalized = (errX + errZ) / maxDistance;  // normalized to [0, 1]
            if (errNormalized > 1)
            {
                // The ball is outside the plane
                SetReward(failureReward);
                RecordFailure();
                EndEpisode();
                return;
            }
            SetReward(1 - errNormalized);   // positive to ensure agent wants to live as long as possible
        }

        if (MaxStep > 0 && StepCount >= MaxStep)
        {
            RecordSuccess();    // Did not fall
        }
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // Rotation values from the platform
        sensor.AddObservation(gameObject.transform.localRotation.x);
        sensor.AddObservation(gameObject.transform.localRotation.z);
        // Values from the ball: relative position and linear velocity
        sensor.AddObservation(ball.transform.localPosition);    // 3d
        sensor.AddObservation(targetPosition);                  // 2d
        sensor.AddObservation(m_BallRb.linearVelocity);         // 3d
    }
    private Vector3 GetUnrotatedPosition(Vector3 pos)
    {
        // "un"-rotate the position back (as if the plane had no rotation)
        // we the ball to go to a specific position (without considering rotation)
        Quaternion planeRot = transform.localRotation;
        Vector3 unrotatedPos = Quaternion.Inverse(planeRot) * pos;
        return unrotatedPos;
    }
}
