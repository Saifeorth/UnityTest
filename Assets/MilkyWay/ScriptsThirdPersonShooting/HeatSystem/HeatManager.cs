using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeatManager : MonoBehaviour
{
    public static HeatManager Instance;

    [Header("Subsystems")]
    //public List<SubsystemController> subsystems;
    public List<SubsystemStateController> subsystemStateControllers;
    public List<WeaponSubsystem> weaponSubsystems;

    [Header("Heat Settings")]
    public float maxHeat = 100f;
    public float passiveCooling = 5f;

    [Header("Venting Settings")]
    public float manualVentCooling = 30f;   // cooling per second during manual venting
    public float overheatVentCooling = 50f; // cooling per second during emergency overheat venting



    [Header("Runtime Heat")]
    public float baselineHeat = 0f;
    public float activeHeat = 0f;
    public float TotalHeat => baselineHeat + activeHeat;

    public bool isVenting = false;
    public bool isOverheated = false;

    [Header("UI")]
    public Slider baselineHeatSlider;           // Only shows active heat
    public Image totalHeatColor;             // Color changes based on total heat
    public RectTransform totalHeatBar;        // Vertical resized bar
    public TMPro.TextMeshProUGUI heatLabelText;
    public TMPro.TextMeshProUGUI heatText;
    public TMPro.TextMeshProUGUI ventStatusText;
    public List<Image> heatVisualImages;
    public float totalBarMaxHeight = 200f;    // Adjust based on UI

    public static event System.Action OnVentingStart;
    public static event System.Action OnVentingStop;
    public static event System.Action OnOverheated;

    public KeyCode toggleHeatGUIKey = KeyCode.Space;
    private bool showHeatGUI = false; 
    private Vector2 heatScroll;
    private GUIStyle heatBoxStyle, heatLabelStyle, heatHeaderStyle;


    private void Awake()
    {
        Instance = this;
        baselineHeatSlider.maxValue = maxHeat;
    }

    private void Start()
    {
        foreach (var s in subsystemStateControllers)
        {
            s.OnFunctionalStateChanged += HandleSubsystemFunctionalChanged;
        }

        foreach (var s in weaponSubsystems)
        {
            s.OnFunctionalStateChanged += HandleWeaponSubsystemFunctionalChanged;
        }


    }

    private void OnDestroy()
    {
        foreach (var s in subsystemStateControllers)
        {
            s.OnFunctionalStateChanged -= HandleSubsystemFunctionalChanged;
        }

        foreach (var s in weaponSubsystems)
        {
            s.OnFunctionalStateChanged -= HandleWeaponSubsystemFunctionalChanged;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleHeatGUIKey)) 
            showHeatGUI = !showHeatGUI;

        float dt = Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.V)) ManualVentingInput();

        if (isVenting)
        {
            // Determine correct vent cooling
            float ventRate = (isOverheated) ? overheatVentCooling : manualVentCooling;

            // Reduce active heat
            activeHeat -= ventRate * dt;

            if (activeHeat <= 0)
            {
                activeHeat = 0;
                isVenting = false;
                isOverheated = false;
                OnVentingStop?.Invoke();
            }
        }
        else
        {
            GenerateActiveHeat(dt);

            activeHeat -= passiveCooling * dt;
            if (activeHeat < 0) activeHeat = 0;

            if (TotalHeat >= maxHeat) StartOverheat();
        }

        UpdateUI();
    }

    // -------------------------------------------------------------------
    // HEAT GENERATION
    // -------------------------------------------------------------------

    private void GenerateActiveHeat(float dt)
    {
        foreach (var subsystem in subsystemStateControllers)
        {
            if (subsystem == null || subsystem.heatData == null || subsystem.visualState == SubsystemVisualState.Disabled) continue;

            HeatSubsystemData hd = subsystem.heatData;
            ////

            //if (subsystem.funcState == SubsystemFunctionalState.Ready)
            //{
            //    activeHeat += hd.heatPerSecondActive * dt;
            //}

            //if (subsystem.funcState == SubsystemFunctionalState.Surge)
            //{
            //    activeHeat += hd.heatPerSecondActive * hd.passiveHeatSUrge * dt;
            //}
        }

        if (activeHeat < 0) activeHeat = 0;
    }

    public void AddBurstHeat(SubsystemController subsystem)
    {
        if (subsystem == null || subsystem.heatData == null) return;

        HeatSubsystemData hd = subsystem.heatData;

        if (!subsystem.isEnabled) return;

        activeHeat += hd.usageHeat;

        if (TotalHeat >= maxHeat)
        {
            activeHeat = Mathf.Clamp(maxHeat - baselineHeat, 0, maxHeat);
            StartOverheat();
        }
    }

    public void AddBurstHeat(SubsystemStateController subsystem)
    {
        if (subsystem == null || subsystem.heatData == null) return;

        HeatSubsystemData hd = subsystem.heatData;

        if (subsystem.funcState == SubsystemFunctionalState.Idle || subsystem.visualState == SubsystemVisualState.Disabled) return;

        if(subsystem.funcState == SubsystemFunctionalState.Ready || subsystem.funcState == SubsystemFunctionalState.Surge)
        activeHeat += hd.usageHeat;

        if (TotalHeat >= maxHeat)
        {
            activeHeat = Mathf.Clamp(maxHeat - baselineHeat, 0, maxHeat);
            StartOverheat();
        }
    }

    public void AddBurstHeat(WeaponSubsystem subsystem)
    {
        if (subsystem == null || subsystem.heatData == null) return;

        HeatSubsystemData hd = subsystem.heatData;

        if (subsystem.funcState == SubsystemFunctionalState.Idle || subsystem.visualState == SubsystemVisualState.Disabled) return;

        if (subsystem.funcState == SubsystemFunctionalState.Ready || subsystem.funcState == SubsystemFunctionalState.Surge)
            activeHeat += hd.usageHeat;

        if (TotalHeat >= maxHeat)
        {
            activeHeat = Mathf.Clamp(maxHeat - baselineHeat, 0, maxHeat);
            StartOverheat();
        }
    }

    // -------------------------------------------------------------------
    // BASELINE HEAT EVENTS
    // -------------------------------------------------------------------

    private void OnWeaponActivated(WeaponSubsystem s)
    {
        baselineHeat += s.heatData.passiveHeatReady;
        baselineHeat = Mathf.Clamp(baselineHeat, 0, maxHeat);
    }

    private void OnWeaponDeactivated(WeaponSubsystem s)
    {
        baselineHeat -= s.heatData.passiveHeatReady;
        baselineHeat = Mathf.Clamp(baselineHeat, 0, maxHeat);
    }

    private void HandleSubsystemFunctionalChanged(SubsystemStateController s, SubsystemFunctionalState st)
    {
        switch (st)
        {
            case SubsystemFunctionalState.Idle:
                // remove initial heat
                baselineHeat -= s.heatData.passiveHeatSurge; // if you still use initialHeat
                break;
            case SubsystemFunctionalState.Ready:
                // add initial heat
                baselineHeat += s.heatData.passiveHeatReady;
                break;
            case SubsystemFunctionalState.Surge:
                baselineHeat -= s.heatData.passiveHeatReady;
                baselineHeat += s.heatData.passiveHeatSurge;
                // no change to baseline initial heat
                break;
        }
        baselineHeat = Mathf.Clamp(baselineHeat, 0, maxHeat);
    }

    private void HandleWeaponSubsystemFunctionalChanged(WeaponSubsystem s, SubsystemFunctionalState st)
    {
        switch (st)
        {
            case SubsystemFunctionalState.Idle:
                // remove initial heat
                baselineHeat -= s.heatData.passiveHeatSurge; // if you still use initialHeat
                break;
            case SubsystemFunctionalState.Ready:
                // add initial heat
                baselineHeat += s.heatData.passiveHeatReady;
                break;
            case SubsystemFunctionalState.Surge:
                baselineHeat -= s.heatData.passiveHeatReady;
                baselineHeat += s.heatData.passiveHeatSurge;
                // no change to baseline initial heat
                break;
        }
        baselineHeat = Mathf.Clamp(baselineHeat, 0, maxHeat);
    }

    // -------------------------------------------------------------------
    // VENTING
    // -------------------------------------------------------------------

    private void ManualVentingInput()
    {
        if (!isVenting)
        {
            isVenting = true;
            OnVentingStart?.Invoke();
        }
    }

    private void StartOverheat()
    {
        isOverheated = true;
        isVenting = true;

        OnOverheated?.Invoke();
        OnVentingStart?.Invoke();
    }

    // -------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------

    private void UpdateUI()
    {
        // Baseline heat → slider
        if (baselineHeatSlider)
            baselineHeatSlider.value = baselineHeat;

        // TOTAL heat vertical bar
        if (totalHeatBar)
        {
            float normalized = Mathf.InverseLerp(0, maxHeat, TotalHeat);
            float targetHeight = normalized * totalBarMaxHeight;

            Vector2 size = totalHeatBar.sizeDelta;
            size.y = targetHeight;
            totalHeatBar.sizeDelta = size;
        }

        // Update heatText
        if (heatText)
        {
            float displayedHeat = Mathf.Clamp(TotalHeat, 0f, 100f);
            heatText.text = $"{displayedHeat:F1}°C";
        }

        // Determine ventStatusText
        if (ventStatusText)
        {
            if (isOverheated && isVenting)
                ventStatusText.text = "EMRG VENT";
            else if (isVenting)
                ventStatusText.text = "VENTING";
            else if (TotalHeat >= maxHeat * 0.6f)
                ventStatusText.text = "[V] VENT";
            else
                ventStatusText.text = "";
        }

        // Update colors
        Color color = Color.white;

        if (isOverheated && isVenting)
            color = Color.red;
        else if (isVenting)
        {
            color = (TotalHeat >= maxHeat * 0.6f) ? Color.yellow : Color.white;
        }
        else
        {
            color = (TotalHeat >= maxHeat * 0.6f) ? Color.yellow : Color.white;
        }

        // Apply color to all images and heatText
        foreach (var img in heatVisualImages)
            if (img != null)
                img.color = color;

        if(heatLabelText)
            heatLabelText.color = color;

        if (heatText)
            heatText.color = color;

        if (ventStatusText)
            ventStatusText.color = (isOverheated || isVenting) ? Color.red : color;



        //// COLOR
        //if (totalHeatColor)
        //{
        //    Color target =
        //        TotalHeat < maxHeat * 0.33f ? Color.green :
        //        TotalHeat < maxHeat * 0.66f ? Color.yellow :
        //        Color.red;

        //    totalHeatColor.DOColor(target, 0.25f);
        //}
    }

    private void OnGUI()
    {
        InitHeatStyles();

        if (!showHeatGUI) return;

        int width = 460;
        int height = 500;

        GUILayout.BeginArea(new Rect(Screen.width - width - 20, 70, width, height), heatBoxStyle);

        heatScroll = GUILayout.BeginScrollView(heatScroll);

        GUILayout.Space(10);


        GUILayout.Label("🔥 HEAT DEBUG PANEL", heatHeaderStyle);
        GUILayout.Space(10);

        GUILayout.Label("GLOBAL HEAT", heatHeaderStyle);
        GUILayout.BeginVertical("box");

        GUI.color = GetHeatColor();
        GUILayout.Label($"Total Heat: {TotalHeat:F1}", heatLabelStyle);
        GUILayout.Label($"Baseline: {baselineHeat:F1}", heatLabelStyle);
        GUILayout.Label($"Active: {activeHeat:F1}", heatLabelStyle);
        GUI.color = Color.white;

        maxHeat = LabeledInputField("Max Heat", maxHeat, 10f, 300f, 20);
        passiveCooling = LabeledInputField("Passive Cooling", passiveCooling, 0f, 20f, 20);
        manualVentCooling = LabeledInputField("Manual Vent Cooling", manualVentCooling, 0f, 50f, 20);
        overheatVentCooling = LabeledInputField("EMRG Vent Cooling", overheatVentCooling, 0f, 50f, 20);

        GUILayout.EndVertical();
        GUILayout.Space(10);

        GUILayout.Label("SHIP SYSTEM HEAT", heatHeaderStyle);

        foreach (var s in subsystemStateControllers)
        {
            if (s == null || s.heatData == null) continue;

            GUILayout.BeginVertical("box");

            GUILayout.Label("Subsystem: " + s.name, heatLabelStyle);
            GUILayout.Space(5);

            s.heatData.passiveHeatReady =
               LabeledInputField("Passive Heat Ready",
               s.heatData.passiveHeatReady, 0f, 20f, 20);

            s.heatData.usageHeat =
                LabeledInputField("Usage Heat",
                s.heatData.usageHeat, 0f, 20f, 20);


            s.heatData.passiveHeatSurge =
                LabeledInputField("Passive Heat Surge",
                s.heatData.passiveHeatSurge, 0f, 20f,20);

            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        GUILayout.Label("WEAPON SYSTEM HEAT", heatHeaderStyle);

        foreach (var s in weaponSubsystems)
        {
            if (s == null || s.heatData == null) continue;

            GUILayout.BeginVertical("box");

            GUILayout.Label("Subsystem: " + s.name, heatLabelStyle);
            GUILayout.Space(5);

            s.heatData.passiveHeatReady =
               LabeledInputField("Passive Heat Ready",
               s.heatData.passiveHeatReady, 0f, 200f, 20);

            s.heatData.passiveHeatSurge =
                LabeledInputField("Passive Heat Surge",
                s.heatData.passiveHeatSurge, 0f, 200f, 20);

            s.heatData.usageHeat =
                LabeledInputField("Usage Heat",
                s.heatData.usageHeat, 0f, 200f, 20);




            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void InitHeatStyles()
    {
        if (heatHeaderStyle != null) 
            return;
        
        heatHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.cyan }
            }; 
            heatLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
            heatBoxStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.75f)) }, padding = new RectOffset(10, 10, 10, 10) };
    }

    private Texture2D MakeTex(int w, int h, Color c) 
    {
        Color[] p = new Color[w * h]; 
        for (int i = 0; i < p.Length; i++)
            p[i] = c; 
        Texture2D tex = new Texture2D(w, h);
        tex.SetPixels(p);
            tex.Apply();
        return tex;
    }

    private float LabeledSlider(string label, float value, float min = 0f, float max = 200f)
    {
        GUILayout.Label($"{label}: {value:F2}", heatLabelStyle); 
        return GUILayout.HorizontalSlider(value, min, max);
    }

    private Color GetHeatColor()
    {
        float n = TotalHeat / maxHeat;
        if (n < 0.33f) return Color.green;
        if (n < 0.66f) return Color.yellow;
        return Color.red;
    }

    private float LabeledInputField(string label, float current, float min, float max, int fontSize = 16)
    {
        // Create styles
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;

        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.fontSize = fontSize;

        GUILayout.BeginHorizontal();

        GUILayout.Label(label, labelStyle, GUILayout.Width(180));

        string newVal = GUILayout.TextField(current.ToString("F2"),
                                            textFieldStyle,
                                            GUILayout.Width(80));

        float parsed;
        if (float.TryParse(newVal, out parsed))
        {
            current = Mathf.Clamp(parsed, min, max);
        }

        GUILayout.Label($"[{min} - {max}]", labelStyle, GUILayout.Width(120));

        GUILayout.EndHorizontal();

        return current;
    }


}
