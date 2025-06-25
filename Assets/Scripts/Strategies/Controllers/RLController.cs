using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

[System.Serializable]
public class RLController : IController
{
    [Header("RL Controller Settings")]
    [Tooltip("How close the ball needs to be to the goal to consider it reached.")]
    public float goalReachedThreshold = 1.0f;
    [Tooltip("Reward for reaching the goal given by the high-level planner.")]
    public float goalReward = 1f;

    [Header("Time Penalty Settings (Dynamic Budget)")]
    [Tooltip("The total negative reward budget for time for the entire episode.")]
    public float totalTimePenaltyBudget = -1.0f;
    [Tooltip("The number of steps the agent is 'allowed' per waypoint for budget calculation.")]
    public float stepsAllowedPerWaypoint = 75f;

    [Header("Reward Shaping")]
    [Tooltip("Multiplier for the reward based on velocity towards the goal.")]
    [Range(0f, 1f)]
    public float directionalRewardScale = 0.01f;

    private StrategicPlatformAgent _agent;
    private Vector3 _currentGoalPosition;
    private float _dynamicPerStepPenalty;

    public void SetEpisodePlan(List<Vector2Int> plan)
    {
        float totalAllowedSteps = (plan != null && plan.Count > 0) ? plan.Count * stepsAllowedPerWaypoint : 0f;
        _dynamicPerStepPenalty = totalAllowedSteps > 1f ? totalTimePenaltyBudget / totalAllowedSteps : 0f;
    }

    public void Initialize(StrategicPlatformAgent agent)
    {
        _agent = agent;
    }

    public void OnEpisodeBegin()
    {
        _dynamicPerStepPenalty = 0;
    }

    public void SetGoal(Vector3 goalPosition)
    {
        _currentGoalPosition = goalPosition;
    }

    public bool HasReachedGoal()
    {
        if (_agent == null || _agent.ball == null) return false;
        Vector3 ballPos = _agent.ball.transform.localPosition;
        float distanceToGoal = Vector2.Distance(
            new Vector2(ballPos.x, ballPos.z),
            new Vector2(_currentGoalPosition.x, _currentGoalPosition.z)
        );
        return distanceToGoal < goalReachedThreshold;
    }

    public void OnGoalReached()
    {
        // Reward for reaching the goal
        _agent.AddReward(goalReward);
    }

    public void CollectObservations(VectorSensor sensor)
    {
        if (_agent == null || _agent.ball == null) return;
        sensor.AddObservation(_currentGoalPosition - _agent.ball.transform.localPosition);
        sensor.AddObservation(_agent.m_BallRb.linearVelocity);
        sensor.AddObservation(_agent.transform.localRotation);
    }

    public void ProcessStep()
    {
        if (_agent == null || _agent.ball == null) return;

        //Dynamic Time Penalty
        _agent.AddReward(_dynamicPerStepPenalty);

        //Reward Shaping (Directional)
        Vector3 ballPos = _agent.ball.transform.localPosition;
        Vector3 directionToGoal = (_currentGoalPosition - ballPos);
        directionToGoal.y = 0;
        directionToGoal.Normalize();

        Vector3 ballVelocity = _agent.m_BallRb.linearVelocity;
        ballVelocity.y = 0;

        if (ballVelocity.sqrMagnitude > 0.01f)
        {
            float directionalReward = Vector3.Dot(ballVelocity.normalized, directionToGoal);
            if (directionalReward > 0)
            {
                _agent.AddReward(directionalRewardScale * directionalReward);
            }
        }
    }
}