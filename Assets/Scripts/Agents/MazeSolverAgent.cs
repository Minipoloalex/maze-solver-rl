using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;


/// <summary>
/// Agent to solve the maze
/// </summary>
public class MazeSolverAgent : PlatformAgent
{
    // position relative to the plane (we take into account orientation)
    // relative position should have y = 0
    [HideInInspector] public Vector3 targetPosition;

    [Header("Distance threshold for defining if the ball reached the exit")]
    public float goalReachedDistanceThreshold = 0.5f;

    public override void Initialize()
    {
        base.Initialize();
        Vector3 scale = gameObject.transform.localScale;
    }
    private void SetEnvParameters(EnvironmentParameters envParams)
    {
        var difficulty = envParams.GetWithDefault("difficulty", 0.5f);
        var seed = (int)envParams.GetWithDefault("maze_seed", 0f);
        controller.mazeGeneratorSeed = seed;
        controller.mazeGeneratorDifficulty = difficulty;

        Debug.Log($"Episode Start: Difficulty={difficulty}, MazeSeed={seed}");
    }
    public override void OnEpisodeBegin()
    {
        // we will be custom-setting the seed with the environment parameters
        // it's still random, but requires the parameter be false
        controller.useRandomSeedForGenerator = false;

        // Get the EnvironmentParameters object from the Academy
        var envParameters = Academy.Instance.EnvironmentParameters;
        SetEnvParameters(envParameters);

        controller.ResetMaze();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // Rotation values from the platform (4d)
        sensor.AddObservation(gameObject.transform.localRotation);

        // Values from the ball: relative position and linear velocity
        sensor.AddObservation(ball.transform.localPosition); // 3d
        sensor.AddObservation(m_BallRb.linearVelocity); // 3d

        // relative position to the target (2d: x, z)
        Vector3 currentBallPosition = ball.transform.localPosition;
        Quaternion planeRot = gameObject.transform.localRotation;
        Vector3 projectedPoint = Quaternion.Inverse(planeRot) * currentBallPosition;

        // Add observations for the calculated projected point's coordinates
        Vector3 posDiff = projectedPoint - worldExitPosition;
        sensor.AddObservation(new Vector2(posDiff.x, posDiff.z)); // 2d

        Debug.Log(projectedPoint);
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);

        float dx = ball.transform.position.x - gameObject.transform.position.x;
        float dz = ball.transform.position.z - gameObject.transform.position.z;
        float dy = ball.transform.position.y - gameObject.transform.position.y;
        if (dy < -10f || dy > 10f || Mathf.Abs(dx) > 50f || Mathf.Abs(dz) > 50f)
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

    private float RewardBasedOnDistance(float x, float z)
    {
        // float dx = (x - targetPosition.x);
        // float dz = (z - targetPosition.z);

        // // the larger the distance, the least reward should be given
        // float distSquared = dx * dx + dz * dz;

        // // Normalize and clamp reward to range [0, 1]
        // return -Mathf.Clamp01(distSquared / maxDistSquared);
        return 0f;
    }
    // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mathf.Sqrt.html
    private float HypotenuseLength(float a, float b)
    {
        return Mathf.Sqrt(a * a + b * b);
    }
}
