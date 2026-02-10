using UnityEngine;
using TMPro;
using System.Globalization;

public class PlayerController : MonoBehaviour
{
    [Header("Speed")]
    public float speedIncrease = 6f;
    public float speedDecrease = 8f;
    public float maxForwardSpeed = 12f;
    public float maxReverseSpeed = -6f;

    public float speedDecayAmount = 3f;
    public bool autoSpeedDecay = true;

    [Header("Strafe")]
    public float strafeSpeed = 6f;
    public float strafeHeat = 0.3f;

    [Header("Heat")]
    public float speedUpHeat = 0.5f;
    public float speedDownHeat = 0.5f;
    public float heatCooldown = 1f;
    public float maxHeat = 100f;

    [Header("Turning")]
    public float turnSpeed = 90f;
    public float turnSpeedReduced = 50f;

    [Header("References")]
    [SerializeField] private Transform worldRoot;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI heatText;

    private float currentSpeed;
    private float heat;

    public float CurrentSpeed => currentSpeed;
    public float Heat => heat;

    [SerializeField] private KeyCode toggleKey = KeyCode.P;
    private bool showGUI;

    // ---------- GUI ----------
    private GUIStyle labelStyle;
    private GUIStyle fieldStyle;
    private GUIStyle headerStyle;
    private bool stylesInitialized;
    private Vector2 scroll;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showGUI = !showGUI;

        HandleMovement();

        if (worldRoot != null)
            worldRoot.position = transform.position;


        HandleStrafe();
        HandleWorldRotation();
        HandleHeat();
        UpdateUI();


    }

    // --------------------------------------------------
    // Movement
    // --------------------------------------------------
    void HandleMovement()
    {
        if (Input.GetKey(KeyCode.W))
        {
            currentSpeed += speedIncrease * Time.deltaTime;
            heat += speedUpHeat * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            currentSpeed -= speedDecrease * Time.deltaTime;
            heat += speedDownHeat * Time.deltaTime;
        }
        else if (autoSpeedDecay)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                0f,
                speedDecayAmount * Time.deltaTime
            );
        }

        currentSpeed = Mathf.Clamp(currentSpeed, maxReverseSpeed, maxForwardSpeed);
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    void HandleStrafe()
    {
        float strafeInput = 0f;

        if (Input.GetKey(KeyCode.Q)) strafeInput = -1f;
        else if (Input.GetKey(KeyCode.E)) strafeInput = 1f;

        if (Mathf.Abs(strafeInput) < 0.01f) return;

        Vector3 strafeDir = transform.right * strafeInput;
        transform.position += strafeDir * strafeSpeed * Time.deltaTime;

        heat += strafeHeat * Time.deltaTime;
    }

    // --------------------------------------------------
    // World Rotation
    // --------------------------------------------------
    void HandleWorldRotation()
    {
        float turnInput = 0f;
        if (Input.GetKey(KeyCode.A)) turnInput = 1f;
        else if (Input.GetKey(KeyCode.D)) turnInput = -1f;

        if (Mathf.Abs(turnInput) < 0.01f) return;

        float speedRatio = Mathf.Abs(currentSpeed) / maxForwardSpeed;
        float reduction = 1f - (turnSpeedReduced / 100f * speedRatio);
        float effectiveTurnSpeed = turnSpeed * reduction;

        float rotationAmount = turnInput * effectiveTurnSpeed * Time.deltaTime;

        worldRoot.RotateAround(
            transform.position,
            Vector3.up,
            rotationAmount
        );
    }

    // --------------------------------------------------
    // Heat
    // --------------------------------------------------
    void HandleHeat()
    {
        if (!Input.GetKey(KeyCode.W) &&
            !Input.GetKey(KeyCode.S) &&
            !Input.GetKey(KeyCode.Q) &&
            !Input.GetKey(KeyCode.E))
        {
            heat -= heatCooldown * Time.deltaTime;
        }

        heat = Mathf.Clamp(heat, 0f, maxHeat);
    }

    void UpdateUI()
    {
        if (speedText)
            speedText.text =
                currentSpeed >= maxForwardSpeed ? "MAX" :
                currentSpeed <= maxReverseSpeed ? "MIN" :
                currentSpeed.ToString("F1");

        if (heatText)
            heatText.text = heat.ToString("F1");
    }

    // --------------------------------------------------
    // IMGUI
    // --------------------------------------------------
    private void OnGUI()
    {
        if (!showGUI) return;
        InitStyles();

        Rect windowRect = new Rect(20, 20, 520, 620);
        GUI.Box(windowRect, "PLAYER TUNING (Runtime)");

        GUILayout.BeginArea(new Rect(30, 55, 500, 555));
        scroll = GUILayout.BeginScrollView(scroll);

        DrawSection("Speed");
        DrawFloatField("Speed Increase (W)", ref speedIncrease);
        DrawFloatField("Speed Decrease (S)", ref speedDecrease);
        DrawFloatField("Max Forward Speed", ref maxForwardSpeed);
        DrawFloatField("Max Reverse Speed", ref maxReverseSpeed);

        DrawFloatField("Speed Decay Amount", ref speedDecayAmount);
        DrawToggleField("Auto Speed Decay", ref autoSpeedDecay);

        DrawSection("Strafe");
        DrawFloatField("Strafe Speed (Q/E)", ref strafeSpeed);
        DrawFloatField("Strafe Heat", ref strafeHeat);

        DrawSection("Heat");
        DrawFloatField("Speed Up Heat", ref speedUpHeat);
        DrawFloatField("Speed Down Heat", ref speedDownHeat);
        DrawFloatField("Heat Cooldown", ref heatCooldown);
        DrawFloatField("Max Heat", ref maxHeat);

        DrawSection("Turning");
        DrawFloatField("Turn Speed", ref turnSpeed);
        DrawFloatField("Turn Speed Reduced (%)", ref turnSpeedReduced);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // --------------------------------------------------
    // GUI Helpers
    // --------------------------------------------------
    private void InitStyles()
    {
        if (stylesInitialized) return;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            normal = { textColor = Color.white }
        };

        fieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 17,
            fixedHeight = 30,
            alignment = TextAnchor.MiddleCenter
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };

        stylesInitialized = true;
    }

    private void DrawSection(string title)
    {
        GUILayout.Space(10);
        GUILayout.Label(title, headerStyle);
        GUILayout.Space(6);
    }

    private void DrawFloatField(string label, ref float value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(260));

        string t = value.ToString("F3", CultureInfo.InvariantCulture);
        string n = GUILayout.TextField(t, fieldStyle, GUILayout.Width(150));

        if (float.TryParse(n, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            value = v;

        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    private void DrawToggleField(string label, ref bool value)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(label, labelStyle, GUILayout.Width(260));
        value = GUILayout.Toggle(value, value ? "ON" : "OFF", GUILayout.Width(150));

        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }
}
