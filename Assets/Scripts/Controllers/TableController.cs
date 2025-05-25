using UnityEngine;

/// <summary>
/// Receives two continuous actions (X and Z) in the range –1…1
/// and tilts the table while clamping it to ±maxTiltDeg.
/// </summary>
public class TableController : MonoBehaviour
{
    [Header("Tilt limits (degrees)")]
    [SerializeField] private float maxTiltDeg = 14f;        // ~= 0.25 rad
    [Header("Tilt step applied each FixedUpdate (degrees)")]
    [SerializeField] private float tiltStepDeg = 2f;        // matches your old “2f * action”

    private Quaternion _initialRot;

    void Awake() => _initialRot = transform.localRotation;

    /// <summary>
    /// Reset rotation to the startup pose (call from Agent.OnEpisodeBegin).
    /// </summary>
    public void ResetPose() => transform.localRotation = _initialRot;

    /// <summary>
    /// Incrementally tilt the table. The input values are expected
    /// to be the Agent’s continuous actions, already in –1…1.
    /// </summary>
    public void ApplyTilt(float inputX, float inputZ)
    {
        float dX = tiltStepDeg * Mathf.Clamp(inputX, -1f, 1f);
        float dZ = tiltStepDeg * Mathf.Clamp(inputZ, -1f, 1f);

        // Current local Euler, converted to signed –180..180
        Vector3 e = transform.localEulerAngles;
        e.x = Normalize180(e.x);
        e.z = Normalize180(e.z);

        // Rotate only if the new angle stays inside the clamp
        if ((e.x <  maxTiltDeg && dX > 0f) ||
            (e.x > -maxTiltDeg && dX < 0f))
            transform.Rotate(Vector3.right,  dX, Space.Self);

        if ((e.z <  maxTiltDeg && dZ > 0f) ||
            (e.z > -maxTiltDeg && dZ < 0f))
            transform.Rotate(Vector3.forward, dZ, Space.Self);
    }

    /// Signed-normalised view of the current tilt — handy for observations
    public float TiltX => Normalize180(transform.localEulerAngles.x) / maxTiltDeg;
    public float TiltZ => Normalize180(transform.localEulerAngles.z) / maxTiltDeg;

    // ----------------- helpers -----------------
    private static float Normalize180(float deg)
    {
        deg %= 360f;
        if (deg > 180f) deg -= 360f;
        return deg;
    }
}
