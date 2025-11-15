using UnityEngine;

public class WeaponYRotator : MonoBehaviour
{
    [Tooltip("Assign the camera (usually your Cinemachine virtual camera or main camera).")]
    public Camera cam;

    [Tooltip("Rotation speed for smooth tracking.")]
    public float rotationSpeed = 10f;

    [Tooltip("Optional offset to fine-tune forward aim.")]
    public float yOffset = 0f;

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        // Target point far along the camera's forward direction
        Vector3 targetPoint = cam.transform.position + cam.transform.forward * 100f;

        // Direction from weapon to target, ignoring vertical difference
        Vector3 direction = targetPoint - transform.position;
        direction.y = yOffset; // keep y fixed or slightly offset

        if (direction.sqrMagnitude > 0.001f)
        {
            // Calculate target rotation (only Y-axis)
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Vector3 euler = targetRot.eulerAngles;
            euler.x = 0f; // lock X rotation
            euler.z = 0f; // lock Z rotation

            // Smoothly rotate toward the target
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(euler), Time.deltaTime * rotationSpeed);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5f);
    }
}
