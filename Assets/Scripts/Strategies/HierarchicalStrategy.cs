using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

[System.Serializable]
public class HierarchicalStrategy : IStrategy
{
    [SerializeReference]
    public IPlanner planner = new AStarPlanner();

    [SerializeReference]
    public IController controller = new RLController();

    [Header("Inference Settings")]
    [Tooltip("When true, enables dynamic path updates during inference (not training).")]
    public bool enableInferenceUpdates = true;
    [Tooltip("If the ball is further than this world distance from the next waypoint, replan. (Requires Inference Updates)")]
    public float replanDistanceThreshold = 3.0f;

    private StrategicPlatformAgent _agent;
    private List<Vector2Int> _currentPlan;
    private int _currentWaypointIndex;

    public void Initialize(StrategicPlatformAgent agent)
    {
        _agent = agent;
        if (planner == null) planner = new AStarPlanner();
        if (controller == null) controller = new RLController();

        controller.Initialize(_agent);
    }

    public void OnEpisodeBegin()
    {
        controller.OnEpisodeBegin();
        _agent.floor.ResetPose();

        Vector2Int startPos = _agent.controller.spawner.GetPosIdFromWorldRelativePosition(_agent.ball.transform.localPosition);
        Vector2Int exitPos = _agent.controller.exitPosId;
        _currentPlan = planner.GeneratePlan(_agent.controller.grid, startPos, exitPos);
        if (controller is RLController rlController){
            rlController.SetEpisodePlan(_currentPlan);
        }
        
        if (_currentPlan != null && _currentPlan.Count > 1)
        {
            _agent.controller.SpawnWaypoints(_currentPlan.GetRange(1, _currentPlan.Count - 1));

            // The first waypoint (index 0) is the start pos, so we target the next one.
            _currentWaypointIndex = 1;
            SetCurrentGoal();
        }
        else
        {
            Debug.LogWarning("A valid plan could not be generated. Ending episode.");
            _agent.RecordFailure();
            _agent.EndEpisode();
        }
    }

    public void CollectObservations(VectorSensor sensor)
    {
        controller.CollectObservations(sensor);
    }

    public void ProcessActions()
    {
        bool isInference = !Academy.Instance.IsCommunicatorOn;

        if (isInference && enableInferenceUpdates && _currentPlan != null && _currentWaypointIndex < _currentPlan.Count)
        {
            Vector3 ballPos = _agent.ball.transform.localPosition;
            Vector3 currentWaypointWorldPos = _agent.controller.spawner.GetWorldRelativePosition(_currentPlan[_currentWaypointIndex]);
            float distanceToWaypoint = Vector3.Distance(ballPos, currentWaypointWorldPos);

            if (distanceToWaypoint > replanDistanceThreshold)
            {
                Debug.Log($"Too far from waypoint ({distanceToWaypoint}m > {replanDistanceThreshold}m). Replanning path.");
                RecalculatePlanFromCurrentPosition();
            }
        }

        controller.ProcessStep();

        if (controller.HasReachedGoal())
        {
            if (isInference && enableInferenceUpdates)
            {
                UpdateWaypointProgressInference();
            }
            else
            {
                UpdateWaypointProgressTraining();
            }
        }
        
        if (_agent.MaxStep > 0 && _agent.StepCount >= _agent.MaxStep)
        {
            _agent.RecordTimeOut();
            _agent.EndEpisode();
        }
    }
    
    private void RecalculatePlanFromCurrentPosition()
    {
        Vector2Int newStartPos = GetBallGridPosition();
        Vector2Int exitPos = _agent.controller.exitPosId;
        _currentPlan = planner.GeneratePlan(_agent.controller.grid, newStartPos, exitPos);

        if (_currentPlan != null && _currentPlan.Count > 1)
        {
            Debug.Log("Successfully replanned.");
            if (controller is RLController rlController)
            {
                rlController.SetEpisodePlan(_currentPlan);
            }

            _agent.controller.SpawnWaypoints(_currentPlan.GetRange(1, _currentPlan.Count - 1));
            _currentWaypointIndex = 1;
            SetCurrentGoal();
        }
        else
        {
            Debug.LogWarning("Replanning failed. Continuing with old plan.");
        }
    }

    private void UpdateWaypointProgressTraining()
    {
        UnityEngine.Debug.Log("Goal reached in training mode.");
        controller.OnGoalReached();
        
        if (_currentPlan != null && _currentWaypointIndex < _currentPlan.Count)
        {
            _agent.controller.RemoveWaypoint(_currentPlan[_currentWaypointIndex]);
        }

        _currentWaypointIndex++;
        
        CheckIfFinalGoalReached();
    }
    
    private void UpdateWaypointProgressInference()
    {
        controller.OnGoalReached();
        
        if (_currentPlan == null || _currentPlan.Count == 0)
        {
            CheckIfFinalGoalReached();
            return;
        }

        Vector2Int currentBallPos = GetBallGridPosition();
        int closestWaypointIndex = -1;
        float minDistanceSqr = float.MaxValue;

        // Find the waypoint in the rest of the plan that is physically closest to the ball.
        for (int i = _currentWaypointIndex; i < _currentPlan.Count; i++)
        {
            float distSqr = (new Vector2(_currentPlan[i].x, _currentPlan[i].y) - new Vector2(currentBallPos.x, currentBallPos.y)).sqrMagnitude;
            UnityEngine.Debug.Log($"Distance to waypoint {i}: {distSqr}");
            if (distSqr < minDistanceSqr)
            {
                UnityEngine.Debug.Log($"New closest waypoint: {i} with distance {distSqr}");
                minDistanceSqr = distSqr;
                closestWaypointIndex = i;
            }
        }

        if (closestWaypointIndex != -1)
        {
            Debug.Log($"Waypoint goal reached. Ball is closest to plan index {closestWaypointIndex}. Updating progress.");
            for (int i = _currentWaypointIndex; i <= closestWaypointIndex; i++)
            {
                if (i < _currentPlan.Count)
                {
                    _agent.controller.RemoveWaypoint(_currentPlan[i]);
                }
            }
            _currentWaypointIndex = closestWaypointIndex + 1;
        }
        else
        {
            if (_currentPlan != null && _currentWaypointIndex < _currentPlan.Count)
            {
                _agent.controller.RemoveWaypoint(_currentPlan[_currentWaypointIndex]);
            }
            _currentWaypointIndex++;
        }
        
        CheckIfFinalGoalReached();
    }
    
    private void CheckIfFinalGoalReached()
    {
        if (_currentPlan == null || _currentWaypointIndex >= _currentPlan.Count)
        {
            Debug.Log("Success! Final goal reached.");
            _agent.RecordSuccess();
            _agent.EndEpisode();
        }
        else
        {
            Debug.Log($"New target is waypoint {_currentWaypointIndex} at {_currentPlan[_currentWaypointIndex]}.");
            SetCurrentGoal();
        }
    }

    private void SetCurrentGoal()
    {
        if (_currentPlan != null && _currentWaypointIndex < _currentPlan.Count)
        {
            Vector2Int nextWaypointGridPos = _currentPlan[_currentWaypointIndex];
            Vector3 nextWaypointWorldPos = _agent.controller.spawner.GetWorldRelativePosition(nextWaypointGridPos);
            controller.SetGoal(nextWaypointWorldPos);
        }
    }
    
    private Vector2Int GetBallGridPosition()
    {
        if (_agent == null || _agent.ball == null || _agent.controller?.spawner == null)
        {
            return new Vector2Int(-1, -1);
        }
        Quaternion planeRot = _agent.gameObject.transform.localRotation;
        Vector3 unrotatedPos = Quaternion.Inverse(planeRot) * _agent.ball.transform.localPosition;
        return _agent.controller.spawner.GetPosIdFromWorldRelativePosition(unrotatedPos);
    }
}