using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeatManager : MonoBehaviour
{
    [Header("References")]
    public List<SubsystemController> subsystems;

    [Header("Heat Settings")]
    public float maxHeat = 100f;
    public float passiveCooling = 5f;
    public float ventCooling = 30f;

    [Header("Runtime")]
    public float currentHeat = 0f;
    public bool isVenting = false;
    public bool isOverheated = false;

    [Header("UI")]
    public Slider heatFill;
    public Image heatFillColor;

    public static HeatManager Instance;

    // ACTIONS for communication
    public static event System.Action OnVentingStart;
    public static event System.Action OnVentingStop;
    public static event System.Action OnOverheated;

    public KeyCode toggleHeatGUIKey = KeyCode.P;
    private bool showHeatGUI = false;
    private Vector2 heatScroll;
    private GUIStyle heatBoxStyle, heatLabelStyle, heatHeaderStyle;

    private void Awake()
    {
        Instance = this;
        heatFill.maxValue = maxHeat;
    }

    private void Start()
    {
        // Subscribe to subsystem events
        foreach (var s in subsystems)
        {
            s.onEnergyChanged += OnSubsystemEnergyChanged;
            s.onSubsystemEnabled += OnSubsystemEnabled;
            s.onSubsystemDisabled += OnSubsystemDisabled;
        }
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.V))
        {
            ManualVentingInput();
        }


        if (Input.GetKeyDown(toggleHeatGUIKey))
        {
            showHeatGUI = !showHeatGUI;
        }


        float dt = Time.deltaTime;

        if (isVenting)
        {
            currentHeat -= ventCooling * dt;

            if (currentHeat <= 0f)
            {
                currentHeat = 0f;
                isVenting = false;
                isOverheated = false;
                OnVentingStop?.Invoke();
            }
        }
        else
        {
            GenerateHeat(dt);

            // Passive cooling
            currentHeat -= passiveCooling * dt;
            if (currentHeat < 0f) currentHeat = 0f;
        }

        if (currentHeat >= maxHeat && !isVenting)
        {
            StartOverheat();
        }

        UpdateUI();
    }

    private void GenerateHeat(float dt)
    {
        foreach (var subsystem in subsystems)
        {
            if (subsystem == null || subsystem.heatData == null)
                continue;

            HeatSubsystemData heat = subsystem.heatData;

            // --- IMPORTANT NEW LOGIC ---
            if (heat.onlyGenerateWhenEnabled)
            {
                if (subsystem.isEnabled)
                    currentHeat += heat.heatPerSecondActive * dt;
            }
            else
            {
                // Heat proportional to allocated bars
                if (subsystem.currentAllocated > 0)
                {
                    float ratio = (float)subsystem.currentAllocated / subsystem.energyData.requiredEnergy;
                    currentHeat += heat.heatPerSecondActive * ratio * dt;
                }
            }
        }

        if (currentHeat < 0f)
            currentHeat = 0f;
    }

    public void AddBurstHeat(SubsystemController subsystem)
    {
        if (subsystem == null || subsystem.heatData == null)
            return;

        HeatSubsystemData heat = subsystem.heatData;

        if (heat.onlyGenerateWhenEnabled && !subsystem.isEnabled)
            return;

        if (subsystem.currentAllocated <= 0)
            return;

        float ratio = 1f;
        if (subsystem.energyData.requiredEnergy > 0)
            ratio = (float)subsystem.currentAllocated / subsystem.energyData.requiredEnergy;

        float burst = heat.burstHeat * ratio;

        currentHeat += burst;
        if (currentHeat >= maxHeat)
        {
            currentHeat = maxHeat;

            if (!isVenting)   // avoid double-calls
                StartOverheat();

            return;
        }
    }

    public void ManualVentingInput()
    {
        if (!isVenting)
            StartManualVenting();
    }

    private void StartManualVenting()
    {
        isVenting = true;
        OnVentingStart?.Invoke();
    }

    private void StartOverheat()
    {
        isOverheated = true;
        isVenting = true;

        OnOverheated?.Invoke();
        OnVentingStart?.Invoke();
    }

    private void UpdateUI()
    {
        if (heatFill != null)
            heatFill.value = currentHeat;

        if (heatFillColor != null)
        {
            Color targetColor =
                currentHeat < maxHeat * 0.33f ? Color.green :
                currentHeat < maxHeat * 0.66f ? Color.yellow :
                Color.red;

            heatFillColor.DOColor(targetColor, 0.25f);
        }
    }


    // ---------------------
    // EVENTS FROM SUBSYSTEM
    // ---------------------

    private void OnSubsystemEnabled(SubsystemController s)
    {
        // If only generate when enabled → start generating heat
        // No direct logic required, Update() handles this now
    }

    private void OnSubsystemDisabled(SubsystemController s)
    {
        // If disabled, stop generating heat
    }

    private void OnSubsystemEnergyChanged(int energy)
    {
        // Can be used later for dynamic heat feedback
    }

    private void OnGUI()
    {
        InitHeatStyles();

        if (!showHeatGUI) return;

        int width = 320;
        int height = 500;

        // Right Panel
        GUILayout.BeginArea(
            new Rect(Screen.width - width - 20, 70, width, height),
            heatBoxStyle
        );

        heatScroll = GUILayout.BeginScrollView(heatScroll);

        GUILayout.Label("🔥 HEAT DEBUG PANEL", heatHeaderStyle);
        GUILayout.Space(10);

        // GLOBAL VALUES
        GUILayout.Label("GLOBAL HEAT SETTINGS", heatHeaderStyle);

        GUILayout.BeginVertical("box");
        GUI.color = GetHeatColor();
        GUILayout.Label($"Current Heat: {currentHeat:F1}", heatLabelStyle);
        GUI.color = Color.white;

        maxHeat = LabeledSlider("Max Heat", maxHeat, 10f, 300f);
        passiveCooling = LabeledSlider("Passive Cooling", passiveCooling, 0f, 20f);
        ventCooling = LabeledSlider("Vent Cooling", ventCooling, 0f, 50f);

        GUILayout.EndVertical();

        GUILayout.Space(10);

        // SUBSYSTEM VALUES
        GUILayout.Label("SUBSYSTEM HEAT DATA", heatHeaderStyle);

        foreach (var s in subsystems)
        {
            if (s == null || s.heatData == null) continue;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Subsystem: " + s.name, heatLabelStyle);

            GUILayout.Space(5);

            s.heatData.heatPerSecondActive =
                LabeledSlider("Heat / sec Active", s.heatData.heatPerSecondActive, -20, 20f);

            GUILayout.Space(3);

            s.heatData.burstHeat =
                LabeledSlider("Burst Heat", s.heatData.burstHeat, 0.0f, 1.0f);

            GUILayout.Space(3);

            bool newToggle = GUILayout.Toggle(
                s.heatData.onlyGenerateWhenEnabled,
                "Only Generate When Enabled"
            );
            s.heatData.onlyGenerateWhenEnabled = newToggle;

            GUILayout.Space(3);

            GUILayout.EndVertical();

            GUILayout.Space(10);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void InitHeatStyles()
    {
        if (heatHeaderStyle != null) return;

        heatHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.cyan }   // Same highlight color as movement GUI
        };

        heatLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal = { textColor = Color.white }
        };

        heatBoxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.75f)) },
            padding = new RectOffset(10, 10, 10, 10)
        };
    }


    private Texture2D MakeTex(int w, int h, Color c)
    {
        Color[] p = new Color[w * h];
        for (int i = 0; i < p.Length; i++) p[i] = c;

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
        float t = currentHeat / maxHeat;

        if (t < 0.33f) return Color.green;
        if (t < 0.66f) return Color.yellow;
        return Color.red;
    }



}
