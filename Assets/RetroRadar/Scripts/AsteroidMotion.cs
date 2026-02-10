using UnityEngine;

public class AsteroidMotion : MonoBehaviour
{
    private float rotationSpeed;
    private float moveSpeed;
    private Vector3 moveDirection;

    public void Init(float rotationSpeedDeg, float directionDeg, float speed)
    {
        rotationSpeed = rotationSpeedDeg;
        moveSpeed = speed;

        // Direction on XZ plane
        moveDirection = Quaternion.Euler(0f, directionDeg, 0f) * Vector3.forward;
    }

    void Update()
    {
        // Spin around Y (top-down spin)
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

        // Move in world space on XZ
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
}
