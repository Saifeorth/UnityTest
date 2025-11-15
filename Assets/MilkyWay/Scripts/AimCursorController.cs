using UnityEngine;

public class AimCursorController : MonoBehaviour
{
    [Header("References")]
    public Transform ship;       // The ship to orbit around
    public float distance = 20f; // Radius from ship
    public float orbitSpeed = 80f;
    public float verticalSpeed = 50f;
    public float verticalLimit = 60f;

    private float yaw;
    private float pitch;

    void Start()
    {
        Vector3 dir = (transform.position - ship.position).normalized;
        yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * orbitSpeed * Time.deltaTime;
        pitch -= mouseY * verticalSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -verticalLimit, verticalLimit);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * Vector3.forward * distance;
        transform.position = ship.position + offset;
    }
}
