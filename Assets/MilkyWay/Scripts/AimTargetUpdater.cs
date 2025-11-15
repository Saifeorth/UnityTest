using UnityEngine;

[ExecuteAlways]
public class AimTargetUpdater : MonoBehaviour
{
    [Tooltip("Camera used to determine screen center forward. If null, Camera.main is used.")]
    public Camera cam;
    [Tooltip("Distance in world units in front of camera where the aim target sits.")]
    public float distance = 60f;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Place the AimTarget in front of the camera along its forward, at fixed distance.
        transform.position = cam.transform.position + cam.transform.forward * distance;

        // Optional: face the camera (only needed if you have a visible gizmo)
        transform.rotation = cam.transform.rotation;
    }

    // Draw gizmo in editor for debugging
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            Gizmos.DrawWireSphere(cam.transform.position + cam.transform.forward * distance, 0.5f);
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * distance);
        }
    }
}
