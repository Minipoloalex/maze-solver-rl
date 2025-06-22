using UnityEngine;
using Unity.MLAgents.Sensors;

[System.Serializable]
public class HierarchicalStrategy : IStrategy
{
    [Header("Sub-components")]
    [SerializeReference] public IPlanner Planner = new AStarPlanner();
    [SerializeReference] public IController Controller = new PIDController();

    private StrategicPlatformAgent _agent;
    private Vector3 _currentWaypointWorldPos;

    public void Initialize(StrategicPlatformAgent agent)
    {
        _agent = agent;
    }

    public void OnEpisodeBegin()
    {
        Controller.Reset();

        // Find the grid cell for the ball's starting position
        Vector3 unrotatedPos = Quaternion.Inverse(_agent.transform.localRotation) * _agent.ball.transform.localPosition;
        Vector2Int startPosId = _agent.controller.spawner.GetPosIdFromWorldRelativePosition(unrotatedPos);
        
        // Generate a plan from start to exit
        bool pathFound = Planner.GeneratePlan(_agent.controller.grid, startPosId, _agent.controller.exitPosId);

        if (pathFound)
        {
            // Get the first waypoint
            AdvanceToNextWaypoint();
        }
        else
        {
            UnityEngine.Debug.LogError("Hierarchical Strategy: No path found from start to exit. Ending episode.");
            _agent.EndEpisode();
        }
    }
    
    // For a deterministic strategy, this can be empty as it doesn't use RL observations.
    public void CollectObservations(VectorSensor sensor) { }
    
    // For a deterministic strategy, this handles the state machine (waypoint progression)
    public void ProcessActions()
    {
        // Check if the low-level controller has reached its goal
        if (Controller.HasReachedGoal(_agent.ball.transform.localPosition))
        {
            if (Planner.IsPlanComplete())
            {
                // We've reached the final waypoint in the plan. Success!
                UnityEngine.Debug.Log("Hierarchical Strategy: Goal Reached!");
                _agent.SetReward(1.0f); // Positive reward for success
                _agent.EndEpisode();
            }
            else
            {
                // We reached an intermediate waypoint, advance to the next one
                AdvanceToNextWaypoint();
            }
        }
        
        // Handle falling off the platform
        if (_agent.ball.transform.localPosition.y < -5f)
        {
             UnityEngine.Debug.Log("Hierarchical Strategy: Ball fell off.");
            _agent.SetReward(-1.0f);
            _agent.EndEpisode();
        }
    }

    private void AdvanceToNextWaypoint()
    {
        Vector2Int nextWaypointGridPos = Planner.GetNextWaypoint();
        _currentWaypointWorldPos = _agent.controller.spawner.GetWorldRelativePosition(nextWaypointGridPos);
        Controller.SetGoal(_currentWaypointWorldPos);
        
        UnityEngine.Debug.Log($"Hierarchical Strategy: Advancing to waypoint {nextWaypointGridPos} at position {_currentWaypointWorldPos}");
    }
    
    // This method will be called from the Agent's FixedUpdate to get the control action
    public Vector2 GetTiltControl()
    {
        if (Planner.IsPlanComplete() && Controller.HasReachedGoal(_agent.ball.transform.localPosition))
        {
             // If we've finished, just keep the platform stable at the goal
            return Controller.CalculateTilt(_agent.ball.transform.localPosition, _agent.m_BallRb.linearVelocity);
        }
        
        return Controller.CalculateTilt(_agent.ball.transform.localPosition, _agent.m_BallRb.linearVelocity);
    }
}