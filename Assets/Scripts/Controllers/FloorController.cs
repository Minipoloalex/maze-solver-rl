using UnityEngine;

/// <summary>
/// Tilts the table using Rigidbody.MoveRotation with clamped X and Z tilt angles.
/// Suitable for ML-Agents and physics-based control.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FloorController : MonoBehaviour 
{
    [Header("Tilt limits (degrees)")]
    public float maxTiltDeg = 14f;

    [Header("Tilt speed (degrees per FixedUpdate)")]
    public float tiltStepDeg = 2f;

    private Rigidbody rb;
    private Quaternion initialRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;  // should already be set in the prefab (it's required)
        initialRotation = transform.localRotation;
    }

    /// <summary>
    /// Resets the table to its starting rotation.
    /// </summary>
    public void ResetPose()
    {
        rb.MoveRotation(initialRotation);
    }

    /// <summary>
    /// Applies a clamped tilt to the table using Rigidbody.MoveRotation.
    /// </summary>
    public void ApplyTilt(float inputX, float inputZ)
    {
        // Clamp input to [-1, 1] and compute delta
        float dX = tiltStepDeg * Mathf.Clamp(inputX, -1f, 1f);
        float dZ = tiltStepDeg * Mathf.Clamp(inputZ, -1f, 1f);

        // Current local rotation: Euler
        Vector3 currentEuler = NormalizeEulerAngles(transform.localEulerAngles);

        // Compute proposed angles
        float newX = Mathf.Clamp(currentEuler.x + dX, -maxTiltDeg, maxTiltDeg);
        float newZ = Mathf.Clamp(currentEuler.z + dZ, -maxTiltDeg, maxTiltDeg);

        Quaternion targetRotation = Quaternion.Euler(newX, 0f, newZ);
        rb.MoveRotation(targetRotation);
    }

    /// <summary>
    /// Normalized tilt values (–1 to 1) for observations.
    /// </summary>
    public float TiltX => NormalizeEulerAngle(transform.localEulerAngles.x) / maxTiltDeg;
    public float TiltZ => NormalizeEulerAngle(transform.localEulerAngles.z) / maxTiltDeg;

    // --- Helpers ---

    private Vector3 NormalizeEulerAngles(Vector3 euler)
    {
        return new Vector3(NormalizeEulerAngle(euler.x), euler.y, NormalizeEulerAngle(euler.z));
    }

    private float NormalizeEulerAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
