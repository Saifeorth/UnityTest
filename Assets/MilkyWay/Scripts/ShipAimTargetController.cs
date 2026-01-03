using UnityEngine;

public class ShipAimTargetController : MonoBehaviour
{
    [Header("References")]
    public Transform ship;
    public Camera cam;

    [Header("Orbit Settings")]
    public float aimDistance = 60f;
    public float orbitSpeedX = 100f;
    public float orbitSpeedY = 80f;
    public float minVerticalAngle = -60f;
    public float maxVerticalAngle = 60f;

    private float yaw;
    private float pitch;
    public ShipMovementThirdPerson shipMovement;

    void Start()
    {
        if (ship == null)
        {
            Debug.LogWarning("Ship reference not assigned.");
            return;
        }

        // Initialize orbit angles based on current relative position
        Vector3 offset = transform.position - ship.position;
        if (offset != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(offset.normalized, Vector3.up);
            yaw = rot.eulerAngles.y;
            pitch = rot.eulerAngles.x;
        }
    }

    void LateUpdate()
    {
        if (!ship) return;

        if (Input.GetMouseButton(1) || shipMovement.showGUI || Input.GetKey(KeyCode.Space)) return;

        // Mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * orbitSpeedX * Time.deltaTime;
        pitch -= mouseY * orbitSpeedY * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        // Compute direction from angles
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direction = rotation * Vector3.forward;

        // Set position at orbit radius around ship
        transform.position = ship.position + direction * aimDistance;

        // Make the cursor face the ship (optional)
        transform.LookAt(ship);
    }

    void OnDrawGizmos()
    {
        if (ship)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(ship.position, transform.position);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
