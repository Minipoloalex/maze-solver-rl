// FILE: Assets/Scripts/Strategies/BalanceToTargetStrategy.cs

using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[System.Serializable]
public class BalanceToTargetStrategy : IStrategy
{
    [Header("Target Settings")]
    public Vector3 targetPosition;
    public bool randomPositionOnBegin;

    private StrategicPlatformAgent _agent;
    private Rigidbody _ballRb;
    private GameObject _ball;
    private Transform _platform;
    private float maxDistSquared;

    public void Initialize(StrategicPlatformAgent agent)
    {
        _agent = agent;
        _ballRb = agent.m_BallRb;
        _ball = agent.ball;
        _platform = agent.gameObject.transform;

        Vector3 scale = _platform.localScale;
        maxDistSquared = (scale.x * scale.x) + (scale.z * scale.z);
    }

    public void OnEpisodeBegin()
    {
        _platform.rotation = Quaternion.identity;
        _ballRb.velocity = Vector3.zero;
        _ball.transform.position = _platform.position + new Vector3(0, 0.5f, 0);

        if (randomPositionOnBegin)
        {
            targetPosition = new Vector3(Random.Range(-4f, 4f), 0, Random.Range(-4f, 4f));
        }
    }

    public void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_platform.rotation.x);
        sensor.AddObservation(_platform.rotation.z);
        sensor.AddObservation(_ball.transform.position - _platform.position);
        sensor.AddObservation(_ballRb.linearVelocity);
        sensor.AddObservation(targetPosition.x);
        sensor.AddObservation(targetPosition.z);
    }

    public void ProcessActions()
    {
        Vector3 ballPosRelativeToPlatform = _ball.transform.position - _platform.position;

        if (ballPosRelativeToPlatform.y < -3f || Mathf.Abs(ballPosRelativeToPlatform.x) > 6f || Mathf.Abs(ballPosRelativeToPlatform.z) > 6f)
        {
            _agent.SetReward(-1f); // Use a simpler reward than -100f
            _agent.EndEpisode();
        }
        else
        {
            float reward = RewardBasedOnDistance(ballPosRelativeToPlatform.x, ballPosRelativeToPlatform.z);
            _agent.SetReward(reward);
        }
    }

    private float RewardBasedOnDistance(float x, float z)
    {
        float dx = x - targetPosition.x;
        float dz = z - targetPosition.z;
        float distSquared = dx * dx + dz * dz;

        // Reward is from 1 (at target) to 0 (at max distance). Invert to make it a penalty.
        float reward = 1.0f - Mathf.Clamp01(distSquared / maxDistSquared);
        return reward;
    }

    public void DecideHeuristicActions(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}