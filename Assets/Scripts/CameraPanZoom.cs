using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraPanZoom : MonoBehaviour
{
    [Header("Panning")]
    public float panSpeed = 10f;
    public float panSmoothness = 0.1f;

    [Header("Zooming")]
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 60f;

    private Camera cam;
    private Vector3 lastMousePos;
    private Vector3 targetPosition;
    private float targetZoom;

    void Awake()
    {
        cam = GetComponent<Camera>();
        targetPosition = transform.position;
        targetZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
    }

    void Update()
    {
        HandlePan();
        HandleZoom();
    }

    void HandlePan()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            // Move opposite to drag direction
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * panSpeed * Time.deltaTime;

            // Apply movement relative to camera orientation
            targetPosition += transform.TransformDirection(move * 0.1f);
        }

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, targetPosition, panSmoothness);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (cam.orthographic)
            {
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * 10f);
            }
            else
            {
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetZoom, Time.deltaTime * 10f);
            }
        }
    }
}
