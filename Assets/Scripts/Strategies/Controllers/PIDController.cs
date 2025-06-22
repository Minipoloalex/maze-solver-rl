using UnityEngine;

[System.Serializable]
public class PIDController : IController
{
    [Header("Gains")]
    public float Kp = 1.0f;
    public float Ki = 0.1f;
    public float Kd = 0.2f;

    [Header("Controller Settings")]
    public float goalReachedThreshold = 0.5f;

    private Vector3 _targetPosition;

    // State for X-axis controller
    private float _integralX = 0f;
    private float _lastErrorX = 0f;

    // State for Z-axis controller
    private float _integralZ = 0f;
    private float _lastErrorZ = 0f;

    public void SetGoal(Vector3 goal)
    {
        _targetPosition = goal;
        Reset();
    }

    public void Reset()
    {
        _integralX = 0f;
        _lastErrorX = 0f;
        _integralZ = 0f;
        _lastErrorZ = 0f;
    }

    public bool HasReachedGoal(Vector3 currentPosition)
    {
        return Vector3.Distance(currentPosition, _targetPosition) < goalReachedThreshold;
    }

    public Vector2 CalculateTilt(Vector3 currentPosition, Vector3 currentVelocity)
    {
        // The error is the difference between where we want to be and where we are.
        Vector3 error = _targetPosition - currentPosition;

        // --- Z-axis Controller (maps to inputX for tilting) ---
        // Proportional term
        float pTermZ = Kp * error.z;
        // Integral term
        _integralZ += error.z * Time.fixedDeltaTime;
        float iTermZ = Ki * _integralZ;
        // Derivative term (on error)
        // Note: A common improvement is derivative on measurement (velocity) to avoid "derivative kick"
        float derivativeZ = (error.z - _lastErrorZ) / Time.fixedDeltaTime;
        _lastErrorZ = error.z;
        float dTermZ = Kd * derivativeZ;

        // The desired tilt on X-axis controls movement on Z-axis. The negative sign is due to coordinate system conventions.
        float tiltX = -(pTermZ + iTermZ + dTermZ);

        // --- X-axis Controller (maps to inputZ for tilting) ---
        // Proportional term
        float pTermX = Kp * error.x;
        // Integral term
        _integralX += error.x * Time.fixedDeltaTime;
        float iTermX = Ki * _integralX;
        // Derivative term
        float derivativeX = (error.x - _lastErrorX) / Time.fixedDeltaTime;
        _lastErrorX = error.x;
        float dTermX = Kd * derivativeX;

        // The desired tilt on Z-axis controls movement on X-axis.
        float tiltZ = (pTermX + iTermX + dTermX);

        // Return the required tilt actions
        return new Vector2(tiltX, tiltZ);
    }
}