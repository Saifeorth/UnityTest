using UnityEngine;

public class AimTargetForward : MonoBehaviour
{
    public Camera cam;
    public float distance = 50f;

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;

        // Position the target straight ahead of the camera
        transform.position = cam.transform.position + cam.transform.forward * distance;
        transform.rotation = cam.transform.rotation;
    }
}
