using UnityEngine;


/// <summary>
/// Simple experimenting script that allows controlling the platform using the arrow keys
/// </summary>
public class PlatformControllerScript : MonoBehaviour
{
    public float tiltSpeed = 30f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float tiltX = Input.GetAxis("Vertical") * tiltSpeed * Time.deltaTime;
        float tiltZ = -Input.GetAxis("Horizontal") * tiltSpeed * Time.deltaTime;

        transform.Rotate(tiltX, 0f, tiltZ, Space.Self);
    }
}
