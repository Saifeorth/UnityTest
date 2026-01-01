
using TMPro;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class ShipMovementThirdPerson : MonoBehaviour
{
    // ---- Linear (acceleration) settings ----
    [Header("Linear Acceleration")]
    public float mainThrust = 3.5f;
    public float strafeThrust = 2f;
    public float reverseThrust = 3.5f;
    public float verticalThrust = 3f;

    // ---- Per-axis dampening ----
    [Header("Per-axis Dampening")]
    public float mainDampening = 1.5f;
    public float reverseDampening = 1.5f;
    public float strafeDampening = 1.2f;
    public float verticalDampening = 2f;

    [Header("Per-axis Stop Thresholds")]
    public float mainStopThreshold = 0.05f;
    public float reverseStopThreshold = 0.05f;
    public float strafeStopThreshold = 0.05f;
    public float verticalStopThreshold = 0.05f;

    [Header("Global speed limit")]
    public float maxLinearSpeed = 10f;

    [Header("Other linear helpers")]
    public float linearDamping = 0.1f;

    // ---- Rotation settings (FIXED: separated angular drag and damping) ----
    [Header("Rotation Settings")]
    public float rotationTorque = 30f;           // Renamed from rotationThrust for clarity
    public float angularDrag = 4f;              // Renamed from rotationDamping
    public float maxAngularSpeed = 2f;          // ADDED: Limit rotation speed

    // ---- Subsystems ----
    [Header("Energy Subsystems")]
    public SubsystemStateController engineSubsystem;
    public SubsystemStateController thrusterSubsystem;

    [Header("Heat")]
    public HeatManager heatManager;

    // ---- Input / GUI / camera ----
    [Header("Input / GUI")]
    public KeyCode upKey = KeyCode.LeftShift;
    public KeyCode downKey = KeyCode.LeftControl;
    public KeyCode toggleGUIKey = KeyCode.P;

    [Header("Thruster Visuals")]
    public ParticleSystem forwardThruster;
    public ParticleSystem reverseThruster;
    public ParticleSystem[] strafeLeftThrusters;
    public ParticleSystem[] strafeRightThrusters;
    public ParticleSystem[] rotateLeftThrusters;
    public ParticleSystem[] rotateRightThrusters;

    [Header("Camera References")]
    public CinemachineCamera thirdPersonCam;
    public Vector3 thirdPersonPositionOffset = new Vector3(0f, 2.81f, -1.23f);
    public Vector3 thirdPersonRotationOffset = Vector3.zero;
    public Vector3 thirdPersonPositionDamping = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector2 thirdPersonRotDamping = Vector2.zero;
    public float thirdPersonZoom = 25f;
    public float minZoom = 1f;
    public float maxZoom = 100f;

    [Header("G-Force UI")]
    public Slider gSlider;
    public TextMeshProUGUI gLabelText;
    public TextMeshProUGUI gValueText;
    public List<Image> gVisualImages;

    // ---- Derived / runtime values ----
    private Rigidbody rb;
    public bool showGUI = false;
    private CinemachineRotationComposer rotComp;

    // G display
    [Header("G Display (read-only)")]
    public float currentGs = 0f;
    public float maxGs = 10f;

    // GUI internals
    private Vector2 scrollPos;
    private GUIStyle headerStyle, labelStyle, boxStyle;

    // FIXED: Added to track movement state for G calculation
    private bool isAccelerating = false;
    private Vector3 accelerationVector = Vector3.zero;
    private Vector3 lastVelocity = Vector3.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = angularDrag;  // FIXED: Use angular drag for smooth rotation damping

        if (thirdPersonCam != null)
            rotComp = thirdPersonCam.GetComponent<CinemachineRotationComposer>();

        if (gSlider != null)
        {
            gSlider.minValue = 0f;
            gSlider.maxValue = maxGs;
            gSlider.value = 0f;
        }

        if (gLabelText != null) gLabelText.text = "G";

        lastVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        // FIXED: Calculate acceleration before applying forces
        if (Time.fixedDeltaTime > 0)
        {
            accelerationVector = (rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
            lastVelocity = rb.linearVelocity;
        }

        HandleRotation();
        HandleThrust();
        LimitVelocity();
        UpdateCameraComposers();
        ComputeGs();  // Compute in FixedUpdate for consistency
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleGUIKey))
            showGUI = !showGUI;

        ManageCursor();
        UpdateGDisplay();  // Only update UI in Update
    }

    private void ManageCursor()
    {
        bool blockControl = Input.GetKey(KeyCode.Mouse1) || showGUI;

        Cursor.visible = blockControl;
        Cursor.lockState = blockControl ? CursorLockMode.None : CursorLockMode.Locked;

        if (rotComp != null)
            rotComp.enabled = !blockControl;
    }

    // -------------------------
    // HandleThrust
    // -------------------------
    private void HandleThrust()
    {
        if (showGUI ||
            (engineSubsystem != null && (engineSubsystem.isVenting ||
                                         engineSubsystem.funcState == SubsystemFunctionalState.Idle ||
                                         engineSubsystem.visualState == SubsystemVisualState.Disabled)))
        {
            ToggleThruster(forwardThruster, false);
            ToggleThruster(reverseThruster, false);
            ToggleThrusters(strafeLeftThrusters, false);
            ToggleThrusters(strafeRightThrusters, false);
            return;
        }

        // Inputs
        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);
        bool q = Input.GetKey(KeyCode.Q);
        bool e = Input.GetKey(KeyCode.E);
        bool up = Input.GetKey(upKey);
        bool down = Input.GetKey(downKey);

        // Track if we're accelerating (for G calculation)
        isAccelerating = w || s || q || e || up || down;

        // Engine multiplier
        float engineMultiplier = 1f;
        if (engineSubsystem != null &&
            engineSubsystem.funcState == SubsystemFunctionalState.Surge)
        {
            engineMultiplier = 2f;
        }

        // Apply thrust
        Vector3 thrust = Vector3.zero;

        if (w) thrust += transform.forward * mainThrust * engineMultiplier;
        if (s) thrust -= transform.forward * reverseThrust * engineMultiplier;
        if (q) thrust -= transform.right * strafeThrust * engineMultiplier;
        if (e) thrust += transform.right * strafeThrust * engineMultiplier;
        if (up) thrust += transform.up * verticalThrust * engineMultiplier;
        if (down) thrust -= transform.up * verticalThrust * engineMultiplier;

        if (thrust != Vector3.zero)
        {
            rb.AddForce(thrust, ForceMode.Force);

            if (heatManager != null && engineSubsystem != null)
                heatManager.AddBurstHeat(engineSubsystem);
        }

        // Thruster visuals
        ToggleThruster(forwardThruster, w);
        ToggleThruster(reverseThruster, s);
        ToggleThrusters(strafeLeftThrusters, e);
        ToggleThrusters(strafeRightThrusters, q);

        // Local-space damping
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        bool modified = false;

        // Forward / reverse
        if (!w && !s)
        {
            float damp = (localVel.z >= 0f) ? mainDampening : reverseDampening;
            localVel.z = Mathf.MoveTowards(localVel.z, 0f, damp * Time.fixedDeltaTime);

            if (Mathf.Abs(localVel.z) < ((localVel.z >= 0f) ? mainStopThreshold : reverseStopThreshold))
                localVel.z = 0f;

            modified = true;
        }

        // Strafe
        if (!q && !e)
        {
            localVel.x = Mathf.MoveTowards(localVel.x, 0f, strafeDampening * Time.fixedDeltaTime);
            if (Mathf.Abs(localVel.x) < strafeStopThreshold)
                localVel.x = 0f;

            modified = true;
        }

        // Vertical
        if (!up && !down)
        {
            localVel.y = Mathf.MoveTowards(localVel.y, 0f, verticalDampening * Time.fixedDeltaTime);
            if (Mathf.Abs(localVel.y) < verticalStopThreshold)
                localVel.y = 0f;

            modified = true;
        }

        if (modified)
        {
            rb.linearVelocity = transform.TransformDirection(localVel);
        }
    }

    // -------------------------
    // HandleRotation (FIXED: Simpler approach)
    // -------------------------
    //private void HandleRotation()
    //{
    //    if (showGUI ||
    //        (thrusterSubsystem != null && (
    //            thrusterSubsystem.isVenting ||
    //            thrusterSubsystem.funcState == SubsystemFunctionalState.Idle ||
    //            thrusterSubsystem.visualState == SubsystemVisualState.Disabled)))
    //    {
    //        ToggleThrusters(rotateLeftThrusters, false);
    //        ToggleThrusters(rotateRightThrusters, false);
    //        return;
    //    }

    //    float input = 0f;
    //    if (Input.GetKey(KeyCode.A)) input -= 1f;
    //    if (Input.GetKey(KeyCode.D)) input += 1f;

    //    float turnMultiplier = 1f;
    //    if (thrusterSubsystem != null &&
    //        thrusterSubsystem.funcState == SubsystemFunctionalState.Surge)
    //    {
    //        turnMultiplier = 2f;
    //    }

    //    if (Mathf.Abs(input) > 0f)
    //    {
    //        // Apply torque based on input
    //        float torque = input * rotationTorque * turnMultiplier;
    //        rb.AddRelativeTorque(Vector3.up * torque, ForceMode.Force);

    //        // FIXED: Limit angular velocity
    //        Vector3 angularVel = rb.angularVelocity;
    //        if (Mathf.Abs(angularVel.y) > maxAngularSpeed)
    //        {
    //            angularVel.y = Mathf.Sign(angularVel.y) * maxAngularSpeed;
    //            rb.angularVelocity = angularVel;
    //        }

    //        if (heatManager != null && thrusterSubsystem != null)
    //            heatManager.AddBurstHeat(thrusterSubsystem);
    //    }

    //    // Visuals
    //    ToggleThrusters(rotateLeftThrusters, input < 0f);  // A key - left turn
    //    ToggleThrusters(rotateRightThrusters, input > 0f); // D key - right turn
    //}

    private void HandleRotation()
    {
        // [Same input/subsystem checks...]

        if (showGUI ||
    (thrusterSubsystem != null && (
        thrusterSubsystem.isVenting ||
        thrusterSubsystem.funcState == SubsystemFunctionalState.Idle ||
        thrusterSubsystem.visualState == SubsystemVisualState.Disabled)))
        {
            ToggleThrusters(rotateLeftThrusters, false);
            ToggleThrusters(rotateRightThrusters, false);
            return;
        }

        float rawInput = 0f;
        if (Input.GetKey(KeyCode.A)) rawInput -= 1f;
        if (Input.GetKey(KeyCode.D)) rawInput += 1f;

        if (Mathf.Abs(rawInput) > 0.1f)
        {
            // Use a tiny bit of physics torque for "weight" feel
            float turnMultiplier = (thrusterSubsystem != null &&
                                   thrusterSubsystem.funcState == SubsystemFunctionalState.Surge) ? 2f : 1f;

            // Small physics torque (just 10% of before)
            rb.AddRelativeTorque(Vector3.up * rawInput * rotationTorque * 0.1f * turnMultiplier, ForceMode.Force);

            // Main rotation with transform.Rotate for responsiveness
            float rotationThisFrame = rawInput * rotationTorque * turnMultiplier * Time.fixedDeltaTime;
            transform.Rotate(0f, rotationThisFrame, 0f);

            // Sync immediately
            //rb.MoveRotation(transform.rotation);

            if (heatManager != null && thrusterSubsystem != null)
                heatManager.AddBurstHeat(thrusterSubsystem);
        }
        else
        {
            // Let physics handle damping naturally
            // But we'll still sync
            rb.MoveRotation(transform.rotation);
        }

        // Visuals based on input, not physics
        ToggleThrusters(rotateLeftThrusters, rawInput < -0.1f);
        ToggleThrusters(rotateRightThrusters, rawInput > 0.1f);
    }


    private void LimitVelocity()
    {
        Vector3 vel = rb.linearVelocity;

        if (vel.sqrMagnitude <= maxLinearSpeed * maxLinearSpeed)
            return;

        rb.linearVelocity = vel.normalized * maxLinearSpeed;
    }

    private void UpdateCameraComposers()
    {
        if (thirdPersonCam != null)
        {
            var posComp = thirdPersonCam.GetComponent<CinemachinePositionComposer>();
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

    // -------------------------
    // ComputeGs (FIXED: Based on actual acceleration/speed)
    // -------------------------
    private void ComputeGs()
    {
        // Method 1: Speed-based (0-10 scale)
        float speed = rb.linearVelocity.magnitude;

        if (maxLinearSpeed <= 0f)
        {
            currentGs = 0f;
            return;
        }

        // Normalize speed to 0-1 range, then scale to 0-10
        float normalizedSpeed = Mathf.Clamp01(speed / maxLinearSpeed);
        currentGs = normalizedSpeed * maxGs;

        //Alternative: If you want Gs to reflect acceleration instead of speed:
        // float accelerationMagnitude = accelerationVector.magnitude;
        //currentGs = Mathf.Clamp(accelerationMagnitude / 9.81f, 0f, maxGs);
    }



    private void UpdateGDisplay()
    {
        if (gSlider != null)
            gSlider.value = currentGs;

        if (gValueText != null)
            gValueText.text = currentGs.ToString("0.0");

        Color c = currentGs < 6f ? Color.white :
                 currentGs < 9f ? Color.yellow :
                 Color.red;

        if (gLabelText != null) gLabelText.color = c;
        if (gValueText != null) gValueText.color = c;

        if (gVisualImages != null)
        {
            for (int i = 0; i < gVisualImages.Count; i++)
            {
                if (gVisualImages[i] != null)
                    gVisualImages[i].color = c;
            }
        }
    }

    // -------------------------
    // Utilities (unchanged)
    // -------------------------
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

    // GUI code remains the same...
    // [Rest of your GUI code remains unchanged]

// ---------------- GUI ----------------
private void OnGUI()
    {
        InitStyles();

        GUI.Label(new Rect(Screen.width - 340, 20, 320, 40),
            $"Press {toggleGUIKey} to {(showGUI ? "hide" : "show")} controls  |  Speed: {rb.linearVelocity.magnitude:F2} m/s  |  G: {currentGs:F2}/{maxGs}", labelStyle);

        if (!showGUI) return;

        GUILayout.BeginArea(new Rect(20, 20, 540, 820), boxStyle);
        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);

        GUILayout.Label("🚀 Linear Acceleration (accelerations)", headerStyle);
        mainThrust = LabeledInputField("Main (forward) Accel", mainThrust, 0f, 200f, 20);
        mainDampening = LabeledInputField("Main Dampening (when released)", mainDampening, 0f, 50f, 20);
        mainStopThreshold = LabeledInputField("Main Stop Threshold", mainStopThreshold, 0f, 1f, 20);

        reverseThrust = LabeledInputField("Reverse (back) Accel", reverseThrust, 0f, 200f, 20);
        reverseDampening = LabeledInputField("Reverse Dampening", reverseDampening, 0f, 50f, 20);
        reverseStopThreshold = LabeledInputField("Reverse Stop Threshold", reverseStopThreshold, 0f, 1f, 20);

        strafeThrust = LabeledInputField("Strafe Accel", strafeThrust, 0f, 200f, 20);
        strafeDampening = LabeledInputField("Strafe Dampening", strafeDampening, 0f, 50f, 20);
        strafeStopThreshold = LabeledInputField("Strafe Stop Threshold", strafeStopThreshold, 0f, 1f, 20);

        verticalThrust = LabeledInputField("Vertical Accel", verticalThrust, 0f, 200f, 20);
        verticalDampening = LabeledInputField("Vertical Dampening", verticalDampening, 0f, 50f, 20);
        verticalStopThreshold = LabeledInputField("Vertical Stop Threshold", verticalStopThreshold, 0f, 1f, 20);

        GUILayout.Space(8);
        maxLinearSpeed = LabeledInputField("Max Linear Speed (universal)", maxLinearSpeed, 0f, 200f, 20);
        linearDamping = LabeledInputField("Fallback Linear Damping (drag)", linearDamping, 0f, 5f, 20);

        GUILayout.Space(12);
        GUILayout.Label("⚙️ Rotation Controls", headerStyle);
        rotationTorque = LabeledInputField("Rotation Acceleration", rotationTorque, 0f, 200f, 20);
        angularDrag = LabeledInputField("Rotation Damping", angularDrag, 0f, 50f, 20);

        GUILayout.Space(12);
        GUILayout.Label("🎛 Subsystem / Heat", headerStyle);
        GUILayout.Label($"Engine subsystem state: {(engineSubsystem != null ? engineSubsystem.funcState.ToString() : "NONE")}", labelStyle);
        GUILayout.Label($"Thruster subsystem state: {(thrusterSubsystem != null ? thrusterSubsystem.funcState.ToString() : "NONE")}", labelStyle);

        GUILayout.Space(12);
        GUILayout.Label("🎥 Camera Settings", headerStyle);
        thirdPersonZoom = LabeledInputField("Camera Distance", thirdPersonZoom, minZoom, maxZoom, 20);
        thirdPersonPositionOffset.x = LabeledInputField("Cam Offset X", thirdPersonPositionOffset.x, -10f, 10f, 20);
        thirdPersonPositionOffset.y = LabeledInputField("Cam Offset Y", thirdPersonPositionOffset.y, -10f, 10f, 20);
        thirdPersonPositionOffset.z = LabeledInputField("Cam Offset Z", thirdPersonPositionOffset.z, -10f, 10f, 20);
        thirdPersonRotationOffset.x = LabeledInputField("Cam Rot X", thirdPersonRotationOffset.x, -180f, 180f, 20);
        thirdPersonRotationOffset.y = LabeledInputField("Cam Rot Y", thirdPersonRotationOffset.y, -180f, 180f, 20);
        thirdPersonRotationOffset.z = LabeledInputField("Cam Rot Z", thirdPersonRotationOffset.z, -180f, 180f, 20);
        thirdPersonPositionDamping.x = LabeledInputField("Cam Pos Damp X", thirdPersonPositionDamping.x, 0f, 10f, 20);
        thirdPersonPositionDamping.y = LabeledInputField("Cam Pos Damp Y", thirdPersonPositionDamping.y, 0f, 10f, 20);
        thirdPersonPositionDamping.z = LabeledInputField("Cam Pos Damp Z", thirdPersonPositionDamping.z, 0f, 10f, 20);
        thirdPersonRotDamping.x = LabeledInputField("Cam Rot Damp X", thirdPersonRotDamping.x, 0f, 10f, 20);
        thirdPersonRotDamping.y = LabeledInputField("Cam Rot Damp Y", thirdPersonRotDamping.y, 0f, 10f, 20);

        GUILayout.Space(10);
        GUILayout.Label($"Current speed: {rb.linearVelocity.magnitude:F2} m/s", labelStyle);
        GUILayout.Label($"Current Gs: {currentGs:F2} / {maxGs}", labelStyle);

        GUILayout.Space(10);
        if (GUILayout.Button("Center Player at (0,0,0)", GUILayout.Height(40)))
            CenterPlayer();

        if (GUILayout.Button("Reset Defaults", GUILayout.Height(40)))
            ResetDefaults();
        
        if (GUILayout.Button("Reload Scene", GUILayout.Height(40)))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void ResetDefaults()
    {
        mainThrust = 3f;
        strafeThrust = 3f;
        reverseThrust = 3f;
        verticalThrust = 3f;

        mainDampening = 0.0f;
        reverseDampening = 0.0f;
        strafeDampening = 3.0f;
        verticalDampening = 3.0f;

        mainStopThreshold = 0.05f;
        reverseStopThreshold = 0.05f;
        strafeStopThreshold = 0.05f;
        verticalStopThreshold = 0.05f;

        maxLinearSpeed = 30f;
        linearDamping = 0.0f;

        rotationTorque = 12.0f;
        angularDrag = 0.1f;

        thirdPersonZoom = 12f;
        thirdPersonPositionOffset = new Vector3(0f, 3f, -2f);
        thirdPersonRotationOffset = Vector3.zero;
        thirdPersonPositionDamping = new Vector3(0.0f, 0.0f, 0.0f);
        thirdPersonRotDamping = Vector2.zero;

        UpdateCameraComposers();
    }

    // Helper UI input field (you had this already)
    private float LabeledInputField(string label, float current, float min, float max, int fontSize = 16)
    {
        GUIStyle localLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = fontSize };

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, localLabelStyle, GUILayout.Width(220));
        string newVal = GUILayout.TextField(current.ToString("F2"), textFieldStyle, GUILayout.Width(100));
        float parsed;
        if (float.TryParse(newVal, out parsed))
            current = Mathf.Clamp(parsed, min, max);
        GUILayout.Label($"[{min} - {max}]", localLabelStyle, GUILayout.Width(120));
        GUILayout.EndHorizontal();

        return current;
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
