using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

[System.Serializable]
public class HierarchicalStrategy : IStrategy
{
    [SerializeReference]
    public IPlanner planner = new AStarPlanner();

    [SerializeReference]
    public IController controller = new RLController();

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

        //Use the planner to generate a path
        Vector2Int startPos = _agent.controller.spawner.GetPosIdFromWorldRelativePosition(_agent.ball.transform.localPosition);
        Vector2Int exitPos = _agent.controller.exitPosId;
        _currentPlan = planner.GeneratePlan(_agent.controller.grid, startPos, exitPos);
        if (controller is RLController rlController){
            rlController.SetEpisodePlan(_currentPlan);
        }
        
        if (_currentPlan != null && _currentPlan.Count > 1)
        {
            _agent.controller.SpawnWaypoints(_currentPlan);
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
        controller.ProcessStep();

        if (controller.HasReachedGoal())
        {
            controller.OnGoalReached();
            
            if (_currentWaypointIndex >= _currentPlan.Count - 1)
            {
                Debug.Log("Success! Final goal reached.");
                _agent.RecordSuccess();
                _agent.EndEpisode();
            }
            else 
            {
                _currentWaypointIndex++;
                Debug.Log($"Waypoint reached! New target is waypoint {_currentWaypointIndex}.");
                SetCurrentGoal();
            }
        }
        
        if (_agent.MaxStep > 0 && _agent.StepCount >= _agent.MaxStep)
        {
            _agent.RecordTimeOut();
            _agent.EndEpisode();
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
}