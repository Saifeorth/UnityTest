using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class ShipMovementThirdPerson : MonoBehaviour
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

    [Header("Vertical Movement Settings")]
    public float verticalThrust = 3f;
    public KeyCode upKey = KeyCode.LeftShift;
    public KeyCode downKey = KeyCode.LeftControl;

    [Header("Speed Limits")]
    public float maxLinearSpeed = 10f;

    [Header("GUI Settings")]
    public KeyCode toggleGUIKey = KeyCode.C;

    [Header("Thruster Particles (Single)")]
    public ParticleSystem forwardThruster;
    public ParticleSystem reverseThruster;

    [Header("Thruster Particles (Multiple)")]
    public ParticleSystem[] strafeLeftThrusters;
    public ParticleSystem[] strafeRightThrusters;
    public ParticleSystem[] rotateLeftThrusters;
    public ParticleSystem[] rotateRightThrusters;

    [Header("Camera References")]
    public CinemachineCamera thirdPersonCam;

    [Header("Third-Person Camera Settings")]
    public float thirdPersonZoom = 25f;
    public Vector3 thirdPersonPositionOffset = new Vector3(0f, 2.81f, -1.23f);
    public Vector3 thirdPersonRotationOffset = Vector3.zero;
    public Vector3 thirdPersonPositionDamping = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector2 thirdPersonRotDamping = Vector2.zero;

    [Header("Zoom Range")]
    public float minZoom = 1f;
    public float maxZoom = 100f;

    private Rigidbody rb;
    private float currentRotationSpeed = 0f;

    private bool showGUI = false;


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

    private void Update()
    {
        // Check for GUI toggle key
        if (Input.GetKeyDown(toggleGUIKey))
        {
            showGUI = !showGUI;
        }

        // Manage cursor visibility & locking
        if (showGUI)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void HandleThrust()
    {
        Vector3 force = Vector3.zero;

        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);
        bool q = Input.GetKey(KeyCode.Q);
        bool e = Input.GetKey(KeyCode.E);
        bool up = Input.GetKey(upKey);
        bool down = Input.GetKey(downKey);

        if (w) force += transform.forward * mainThrust;
        if (s) force -= transform.forward * reverseThrust;
        if (q) force -= transform.right * strafeThrust;
        if (e) force += transform.right * strafeThrust;
        if (up)
        {
            force += transform.up * verticalThrust;
        }
        else if (down)
        {
            force -= transform.up * verticalThrust;
        }
        else
        {
            // No key pressed → apply damping to vertical velocity
            Vector3 vel = rb.linearVelocity;

            // Smoothly reduce ONLY vertical axis over time
            vel.y = Mathf.MoveTowards(
                vel.y,
                0f,
                verticalThrust * Time.fixedDeltaTime   // Damping speed (tweakable)
            );

            rb.linearVelocity = vel;
        }

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

    private void UpdateCameraComposers()
    {
        if (thirdPersonCam != null)
        {
            var posComp = thirdPersonCam.GetComponent<CinemachinePositionComposer>();
            var rotComp = thirdPersonCam.GetComponent<CinemachineRotationComposer>();
            if (posComp != null)
            {
                posComp.Damping = thirdPersonPositionDamping;
                posComp.CameraDistance = thirdPersonZoom;
                posComp.TargetOffset = thirdPersonPositionOffset;
            }

            if (rotComp != null)
            {
                rotComp.Damping = thirdPersonRotDamping;
                rotComp.TargetOffset = thirdPersonRotationOffset;
            }
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

        GUI.Label(new Rect(Screen.width - 240, 20, 220, 40), $"Press {toggleGUIKey} to {(showGUI ? "hide" : "show")} controls", labelStyle);

        if (showGUI)
        {
            GUILayout.BeginArea(new Rect(20, 20, 440, 820), boxStyle);
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);

            GUILayout.Label("🚀 Thrust Controls", headerStyle);
            mainThrust = LabeledSlider("Main Thrust", mainThrust, 0, 100);
            strafeThrust = LabeledSlider("Strafe Thrust", strafeThrust, 0, 100);
            reverseThrust = LabeledSlider("Reverse Thrust", reverseThrust, 0, 100);
            verticalThrust = LabeledSlider("Vertical Thrust", verticalThrust, 0, 100);
            maxLinearSpeed = LabeledSlider("Max Linear Speed", maxLinearSpeed, 0, 100);
            GUILayout.Space(10);

            GUILayout.Label("⚙️ Rotation Controls", headerStyle);
            rotationThrust = LabeledSlider("Rotation Thrust", rotationThrust, 0, 200);
            rotationAcceleration = LabeledSlider("Rotation Accel", rotationAcceleration, 0, 50);
            maxAngularSpeed = LabeledSlider("Max Angular Speed", maxAngularSpeed, 0, 5);
            rotationDamping = LabeledSlider("Rotation Damping", rotationDamping, 0, 10);
            GUILayout.Space(15);

            GUILayout.Label("🎥 Camera Settings", headerStyle);
            GUILayout.Space(10);

            GUILayout.Label("Third-Person Camera", headerStyle);
            thirdPersonZoom = LabeledSlider("Camera Distance", thirdPersonZoom, minZoom, maxZoom);
            thirdPersonPositionOffset.x = LabeledSlider("Position Offset X", thirdPersonPositionOffset.x, -10, 10);
            thirdPersonPositionOffset.y = LabeledSlider("Position Offset Y", thirdPersonPositionOffset.y, -10, 10);
            thirdPersonPositionOffset.z = LabeledSlider("Position Offset Z", thirdPersonPositionOffset.z, -10, 10);
            thirdPersonRotationOffset.x = LabeledSlider("Rotation Offset X", thirdPersonRotationOffset.x, -90, 90);
            thirdPersonRotationOffset.y = LabeledSlider("Rotation Offset Y", thirdPersonRotationOffset.y, -180, 180);
            thirdPersonRotationOffset.z = LabeledSlider("Rotation Offset Z", thirdPersonRotationOffset.z, -180, 180);
            thirdPersonPositionDamping.x = LabeledSlider("Camera Position Damping X", thirdPersonPositionDamping.x, 0, 10);
            thirdPersonPositionDamping.y = LabeledSlider("Camera Position Damping Y", thirdPersonPositionDamping.y, 0, 10);
            thirdPersonPositionDamping.z = LabeledSlider("Camera Position Damping Z", thirdPersonPositionDamping.z, 0, 10);
            thirdPersonRotDamping.x = LabeledSlider("Camera Rotation Damping X", thirdPersonRotDamping.x, 0, 10);
            thirdPersonRotDamping.y = LabeledSlider("Camera Rotation Damping Y", thirdPersonRotDamping.y, 0, 10);

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

        thirdPersonZoom = 10f;
        thirdPersonPositionOffset = new Vector3(0f, 2.81f, -1.23f);
        thirdPersonRotationOffset = Vector3.zero;
        thirdPersonPositionDamping = new Vector3(0.5f,0.5f,0.5f);
        thirdPersonRotDamping = Vector2.zero;

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
