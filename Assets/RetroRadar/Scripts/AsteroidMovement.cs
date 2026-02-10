using UnityEngine;

public class AsteroidMovement : MonoBehaviour
{
    public float rotationSpeed = 20f;
    public float driftSpeed = 0.5f;
    public Vector3 driftDirection = Vector3.forward;

    void Update()
    {
        // Rotate around Y axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

        // Drift movement
        transform.localPosition += driftDirection * driftSpeed * Time.deltaTime;
    }
}