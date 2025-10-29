using UnityEngine;
using Unity.Cinemachine;

public class FreeLookOrbitCinemachine3 : MonoBehaviour
{
    [Header("Camera Target")]
    public Transform currentTarget;

    [Header("Orbit Settings")]
    public float orbitSpeedX = 300f;
    public float orbitSpeedY = 2f;
    public float transitionSpeed = 4f; // faster transition now
    public float verticalInputSensitivity = 0.5f;

    [Header("Rig Heights")]
    public float defaultXAxisValue = 0f;

    private CinemachineCamera cinemachineCamera;
    private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private bool isOrbiting = false;
    [SerializeField] private bool isTransitioning = false;

    private float topDownYValue;
    private float orbitYValue;  // midpoint between center & bottom
    private float currentOrbitY;

    void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
        orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
    }

    void Start()
    {
        if (currentTarget)
        {
            cinemachineCamera.Follow = currentTarget;
            cinemachineCamera.LookAt = currentTarget;
        }

        topDownYValue = orbitalFollow.Orbits.Top.Height;
        orbitYValue = Mathf.Lerp(orbitalFollow.Orbits.Bottom.Height, orbitalFollow.Orbits.Center.Height, 0.25f);
        currentOrbitY = orbitYValue;

        orbitalFollow.VerticalAxis.Value = topDownYValue;
        orbitalFollow.HorizontalAxis.Value = defaultXAxisValue;
    }

    void Update()
    {
        if (currentTarget == null || orbitalFollow == null)
            return;

        bool middleMouse = Input.GetMouseButton(2);

        if (middleMouse)
        {
            if (!isOrbiting)
            {
                isOrbiting = true;
                isTransitioning = true;
            }

            // Always move quickly toward orbit height
            if(isTransitioning)
            orbitalFollow.VerticalAxis.Value = Mathf.Lerp(
                orbitalFollow.VerticalAxis.Value,
                orbitYValue,
                Time.deltaTime * transitionSpeed
            );

            // Begin orbit control as soon as we’re near orbit height
            if (Mathf.Abs(orbitalFollow.VerticalAxis.Value - orbitYValue) < 0.2f)
                isTransitioning = false;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            orbitalFollow.HorizontalAxis.Value += mouseX * orbitSpeedX * Time.deltaTime;

            // Adjust height between center ↔ bottom based on Y input
            currentOrbitY += mouseY * verticalInputSensitivity;
            currentOrbitY = Mathf.Clamp(currentOrbitY, orbitalFollow.Orbits.Bottom.Height, orbitalFollow.Orbits.Center.Height);

            orbitalFollow.VerticalAxis.Value = Mathf.Lerp(
                orbitalFollow.VerticalAxis.Value,
                currentOrbitY,
                Time.deltaTime * orbitSpeedY
            );
        }
        else
        {
            if (isOrbiting)
            {
                isOrbiting = false;
                isTransitioning = true;
            }

            // Smoothly go back to top-down
            orbitalFollow.VerticalAxis.Value = Mathf.Lerp(
                orbitalFollow.VerticalAxis.Value,
                topDownYValue,
                Time.deltaTime * transitionSpeed
            );

            orbitalFollow.HorizontalAxis.Value = Mathf.LerpAngle(
                orbitalFollow.HorizontalAxis.Value,
                defaultXAxisValue,
                Time.deltaTime * transitionSpeed * 0.5f
            );

            if (Mathf.Abs(orbitalFollow.VerticalAxis.Value - topDownYValue) < 0.05f)
                isTransitioning = false;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        currentTarget = newTarget;
        cinemachineCamera.Follow = newTarget;
        cinemachineCamera.LookAt = newTarget;
    }
}
