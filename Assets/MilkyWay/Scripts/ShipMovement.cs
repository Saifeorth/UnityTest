using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class ShipMovement : MonoBehaviour
{
    [Header("Thrust Settings")]
    public float mainThrust = 3.5f;
    public float strafeThrust = 2f;
    public float reverseThrust = 3.5f;

    [Header("Rotation Settings")]
    public float rotationThrust = 30f;
    public float rotationAcceleration = 10f;
    public float maxAngularSpeed = 0.5f;
    public float rotationDamping = 4f;

    [Header("Speed Limits")]
    public float maxLinearSpeed = 10f;

    [Header("Thruster Particles (Single)")]
    public ParticleSystem forwardThruster;
    public ParticleSystem reverseThruster;

    [Header("Thruster Particles (Multiple)")]
    public ParticleSystem[] strafeLeftThrusters;
    public ParticleSystem[] strafeRightThrusters;
    public ParticleSystem[] rotateLeftThrusters;
    public ParticleSystem[] rotateRightThrusters;

    [Header("Camera References")]
    public CinemachineCamera topDownCam;
    public CinemachineCamera thirdPersonCam;

    private enum CameraMode { TopDown, ThirdPerson }
    private CameraMode currentCamera = CameraMode.TopDown;

    [Header("Top-Down Camera Settings")]
    public float topDownZoom = 25f;
    public float topDownOrthoSize = 25f;
    public Vector3 topDownDamping = new Vector3(1f, 1f, 1f);
    public Vector3 topDownRotationOffset = new Vector3(45f, 45f, 0f);

    [Header("Third-Person Camera Settings")]
    public float thirdPersonZoom = 25f;
    public Vector3 thirdPersonRotationOffset = new Vector3(0f, -40f, 46.67f);
    public Vector3 thirdPersonPositionDamping = new Vector3(1f, 1f, 1f);
    public Vector2 thirdPersonRotDamping = new Vector2(1f, 1f);

    [Header("Zoom Range")]
    public float minZoom = 1f;
    public float maxZoom = 100f;

    private Rigidbody rb;
    private float currentRotationSpeed = 0f;

    private bool showGUI = true;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    private void FixedUpdate()
    {
        HandleThrust();
        HandleRotation();
        LimitVelocity();
        KeepUpright();
        UpdateCameraComposers();
    }

    private void HandleThrust()
    {
        Vector3 force = Vector3.zero;

        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);
        bool q = Input.GetKey(KeyCode.Q);
        bool e = Input.GetKey(KeyCode.E);

        if (w) force += transform.forward * mainThrust;
        if (s) force -= transform.forward * reverseThrust;
        if (q) force -= transform.right * strafeThrust;
        if (e) force += transform.right * strafeThrust;

        rb.AddForce(force, ForceMode.Acceleration);

        ToggleThruster(forwardThruster, w);
        ToggleThruster(reverseThruster, s);
        ToggleThrusters(strafeLeftThrusters, e);
        ToggleThrusters(strafeRightThrusters, q);
    }

    private void HandleRotation()
    {
        bool a = Input.GetKey(KeyCode.A);
        bool d = Input.GetKey(KeyCode.D);

        float targetSpeed = 0f;
        if (a) targetSpeed = -maxAngularSpeed;
        else if (d) targetSpeed = maxAngularSpeed;

        currentRotationSpeed = Mathf.MoveTowards(currentRotationSpeed, targetSpeed, rotationAcceleration * Time.fixedDeltaTime);

        rb.angularVelocity = new Vector3(0f, currentRotationSpeed * rotationThrust * Time.fixedDeltaTime, 0f);

        ToggleThrusters(rotateLeftThrusters, d);
        ToggleThrusters(rotateRightThrusters, a);
    }

    private void LimitVelocity()
    {
        if (rb.linearVelocity.magnitude > maxLinearSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxLinearSpeed;

        Vector3 angularVel = rb.angularVelocity;
        angularVel.y = Mathf.Clamp(angularVel.y, -maxAngularSpeed, maxAngularSpeed);
        rb.angularVelocity = new Vector3(0f, angularVel.y, 0f);
    }

    private void KeepUpright()
    {
        Vector3 euler = rb.rotation.eulerAngles;
        rb.rotation = Quaternion.Euler(0f, euler.y, 0f);
    }

    private void SetActiveCamera(CameraMode mode)
    {
        currentCamera = mode;

        if (topDownCam != null && thirdPersonCam != null)
        {
            topDownCam.Priority = (mode == CameraMode.TopDown) ? 20 : 10;
            thirdPersonCam.Priority = (mode == CameraMode.ThirdPerson) ? 20 : 10;
        }
    }

    private void UpdateCameraComposers()
    {
        if (topDownCam != null)
        {
            topDownCam.Lens.OrthographicSize = topDownOrthoSize;
            var posComp = topDownCam.GetComponent<CinemachinePositionComposer>();
            if (posComp != null)
            {
                posComp.CameraDistance = topDownZoom;
                posComp.Damping = topDownDamping;
            }
        }

        if (thirdPersonCam != null)
        {
            var posComp = thirdPersonCam.GetComponent<CinemachinePositionComposer>();
            var rotComp = thirdPersonCam.GetComponent<CinemachineRotationComposer>();
            if (posComp != null)
            {
                posComp.Damping = thirdPersonPositionDamping;
                posComp.CameraDistance = thirdPersonZoom;
            }

            if (rotComp != null)
            {
                rotComp.Damping = thirdPersonRotDamping;
                rotComp.TargetOffset = thirdPersonRotationOffset;
            }

            topDownCam.transform.rotation = Quaternion.Euler(topDownRotationOffset);
        }
    }

    private void CenterPlayer()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    private void ToggleThruster(ParticleSystem ps, bool active)
    {
        if (ps == null) return;
        var emission = ps.emission;
        emission.enabled = active;
    }

    private void ToggleThrusters(ParticleSystem[] thrusters, bool active)
    {
        if (thrusters == null) return;
        foreach (var ps in thrusters)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            emission.enabled = active;
        }
    }

    // ---------------- GUI ----------------
    private Vector2 scrollPos;
    private GUIStyle headerStyle, labelStyle, boxStyle;

    private void OnGUI()
    {
        InitStyles();

        if (showGUI)
        {
            GUILayout.BeginArea(new Rect(20, 70, 440, 820), boxStyle);
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);

            GUILayout.Label("🚀 Thrust Controls", headerStyle);
            mainThrust = LabeledSlider("Main Thrust", mainThrust, 0, 100);
            strafeThrust = LabeledSlider("Strafe Thrust", strafeThrust, 0, 100);
            reverseThrust = LabeledSlider("Reverse Thrust", reverseThrust, 0, 100);
            maxLinearSpeed = LabeledSlider("Max Linear Speed", maxLinearSpeed, 0, 100);
            GUILayout.Space(10);

            GUILayout.Label("⚙️ Rotation Controls", headerStyle);
            rotationThrust = LabeledSlider("Rotation Thrust", rotationThrust, 0, 200);
            rotationAcceleration = LabeledSlider("Rotation Accel", rotationAcceleration, 0, 50);
            maxAngularSpeed = LabeledSlider("Max Angular Speed", maxAngularSpeed, 0, 5);
            rotationDamping = LabeledSlider("Rotation Damping", rotationDamping, 0, 10);
            GUILayout.Space(15);

            GUILayout.Label("🎥 Camera Mode", headerStyle);
            CameraMode newMode = (CameraMode)GUILayout.Toolbar((int)currentCamera, new[] { "Top-Down", "Third-Person" }, GUILayout.Height(40));
            if (newMode != currentCamera)
                SetActiveCamera(newMode);
            GUILayout.Space(10);

            if (currentCamera == CameraMode.TopDown)
            {
                GUILayout.Label("Top-Down Camera", headerStyle);
                topDownOrthoSize = LabeledSlider("Camera Zoom", topDownOrthoSize, 1, 100);
                topDownZoom = LabeledSlider("Camera Distance", topDownZoom, minZoom, maxZoom);
                topDownRotationOffset.x = LabeledSlider("Rotation Offset X", topDownRotationOffset.x, -90, 90);
                topDownRotationOffset.y = LabeledSlider("Rotation Offset Y", topDownRotationOffset.y, -180, 180);
                topDownRotationOffset.z = LabeledSlider("Rotation Offset Z", topDownRotationOffset.z, -180, 180);

                topDownDamping.x = LabeledSlider("Camera Position Damping X", topDownDamping.x, 0, 10);
                topDownDamping.y = LabeledSlider("Camera Position Damping Y", topDownDamping.y, 0, 10);
                topDownDamping.z = LabeledSlider("Camera Position Damping Z", topDownDamping.z, 0, 10);
            }

            if (currentCamera == CameraMode.ThirdPerson)
            {
                GUILayout.Label("Third-Person Camera", headerStyle);
                thirdPersonZoom = LabeledSlider("Camera Distance", thirdPersonZoom, minZoom, maxZoom);
                thirdPersonRotationOffset.x = LabeledSlider("Rotation Offset X", thirdPersonRotationOffset.x, -90, 90);
                thirdPersonRotationOffset.y = LabeledSlider("Rotation Offset Y", thirdPersonRotationOffset.y, -180, 180);
                thirdPersonRotationOffset.z = LabeledSlider("Rotation Offset Z", thirdPersonRotationOffset.z, -180, 180);
                thirdPersonPositionDamping.x = LabeledSlider("Camera Position Damping X", thirdPersonPositionDamping.x, 0, 10);
                thirdPersonPositionDamping.y = LabeledSlider("Camera Position Damping Y", thirdPersonPositionDamping.y, 0, 10);
                thirdPersonPositionDamping.z = LabeledSlider("Camera Position Damping Z", thirdPersonPositionDamping.z, 0, 10);
                thirdPersonRotDamping.x = LabeledSlider("Camera Rotation Damping X", thirdPersonRotDamping.x, 0, 10);
                thirdPersonRotDamping.y = LabeledSlider("Camera Rotation Damping Y", thirdPersonRotDamping.y, 0, 10);
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Center Player at (0,0,0)", GUILayout.Height(40)))
                CenterPlayer();

            if (GUILayout.Button("Reset Defaults", GUILayout.Height(40)))
                ResetDefaults();

            if (GUILayout.Button("Reload Scene", GUILayout.Height(40)))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // --- Toggle button ---
        if (GUI.Button(new Rect(20, 20, 160, 40), showGUI ? "Hide Controls ▲" : "Show Controls ▼"))
        {
            showGUI = !showGUI;
        }
    }

    private void ResetDefaults()
    {
        mainThrust = 5.97f;
        strafeThrust = 2.62f;
        reverseThrust = 3.50f;
        rotationThrust = 53.70f;
        rotationAcceleration = 12.81f;
        maxAngularSpeed = 0.56f;
        rotationDamping = 5.09f;
        maxLinearSpeed = 10f;

        topDownZoom = 60f;
        topDownOrthoSize = 32.47f;
        topDownRotationOffset = new Vector3(45f, 45f, 0f);
        topDownDamping = Vector3.one;
        thirdPersonZoom = 25f;
        thirdPersonRotationOffset = new Vector3(0f, -40f, 46.67f);
        thirdPersonPositionDamping = Vector3.one;
        thirdPersonRotDamping = Vector2.one;

        UpdateCameraComposers();
    }

    private float LabeledSlider(string label, float value, float min, float max)
    {
        GUILayout.Label($"{label}: {value:F2}", labelStyle);
        return GUILayout.HorizontalSlider(value, min, max);
    }

    private void InitStyles()
    {
        if (headerStyle != null) return;
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan },
            alignment = TextAnchor.MiddleCenter
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            normal = { textColor = Color.white },
            fontSize = 17
        };
        boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.85f)) },
            padding = new RectOffset(10, 10, 10, 10)
        };
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
