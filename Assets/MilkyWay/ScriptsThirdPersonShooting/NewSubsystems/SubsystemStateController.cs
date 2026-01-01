using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SubsystemFunctionalState
{
    Idle,
    Ready,
    Surge
}

public enum SubsystemVisualState
{
    Normal,
    Damaged,
    Disabled
}

public class SubsystemStateController : MonoBehaviour
{
    // ---------------- Header / References ----------------
    [Header("Heat Data")]
    public HeatSubsystemData heatData;

    [Header("UI References")]
    public Image topLabelImage;
    public Image bottomLabelImage;
    public Image bgImage;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI heatText;

    // ---------------- Colors ----------------
    public Color normalColor = new Color(0.84f, 0.93f, 0.92f);
    public Color hoverTopColor = Color.black;
    public Color titleHover = Color.white;

    public Color readyColor = new Color(0f, 1f, 0.05f);
    public Color surgeColor = new Color(1f, 0.8f, 0f);

    private readonly Color damagedColor = new Color(1f, 0.8f, 0f);
    private readonly Color disabledColor = new Color(1f, 0f, 0f);

    // ---------------- Runtime ----------------
    public SubsystemFunctionalState funcState = SubsystemFunctionalState.Idle;
    public SubsystemVisualState visualState = SubsystemVisualState.Normal;

    private bool isHovering = false;
    private bool isTransitioning = false;

    private Coroutine stateRoutine;

    private RectTransform rect;
    private Vector2 originalSize;

    private RectTransform bottomRect;
    private Vector2 bottomOriginalSize;

    public bool isVenting = false;     // when true → everything is paused
    private SubsystemFunctionalState cachedFuncState;

    // Event: fired immediately when functional state changes (Idle/Ready/Surge)
    public event Action<SubsystemStateController, SubsystemFunctionalState> OnFunctionalStateChanged;

    // --------------------------------------------------------
    private void OnEnable()
    {
        HeatManager.OnVentingStart += HandleVentingStart;
        HeatManager.OnVentingStop += HandleVentingStop;
    }

    private void OnDisable()
    {
        HeatManager.OnVentingStart -= HandleVentingStart;
        HeatManager.OnVentingStop -= HandleVentingStop;
    }

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        originalSize = rect != null ? rect.sizeDelta : Vector2.zero;

        if (bottomLabelImage != null)
        {
            bottomRect = bottomLabelImage.GetComponent<RectTransform>();
            if (bottomRect != null) bottomOriginalSize = bottomRect.sizeDelta;
        }

        if (titleText != null) titleText.text = name;

        ApplyVisualState();
        UpdateStateInstant();
    }

    // --------------------------------------------------------
    // HEAT TEXT helpers
    // --------------------------------------------------------

    private string GetHeatStringFunctional(SubsystemFunctionalState s)
    {
        // exact same logic as helper but ensures using provided s
        switch (s)
        {
            case SubsystemFunctionalState.Idle: return "0°";
            case SubsystemFunctionalState.Ready:
                return heatData != null ? $"{Mathf.RoundToInt(heatData.passiveHeatReady)}°" : "0°";
            case SubsystemFunctionalState.Surge:
                return heatData != null ? $"{Mathf.RoundToInt(heatData.passiveHeatSurge)}°" : "0°";
        }
        return "0°";
    }

    private string GetHeatString()
    {
        if (visualState == SubsystemVisualState.Disabled) return "";

        return GetHeatStringFunctional(funcState);
    }

    private void UpdateHeatText()
    {
        if (heatText == null) return;
        heatText.text = GetHeatString();
    }

    // --------------------------------------------------------
    // VISUAL STATE APPLICATION
    // --------------------------------------------------------
    private void ApplyVisualState()
    {
        switch (visualState)
        {
            case SubsystemVisualState.Disabled:
                ApplyDisabledVisuals();
                break;

            case SubsystemVisualState.Damaged:
                ApplyDamagedVisuals();
                break;

            case SubsystemVisualState.Normal:
                ApplyNormalVisuals();
                break;
        }
    }

    private void ApplyNormalVisuals()
    {
        bgImage.color = normalColor;
        topLabelImage.color = normalColor;
        if (titleText != null) titleText.color = Color.black;
        // no description UI per spec
    }

    private void ApplyDamagedVisuals()
    {
        bgImage.color = damagedColor;
        topLabelImage.color = damagedColor;
        if (titleText != null) titleText.color = Color.black;
        UpdateHeatText();
    }

    private void ApplyDisabledVisuals()
    {
        bgImage.color = disabledColor;
        topLabelImage.color = disabledColor;
        if (titleText != null) titleText.color = Color.black;
        if (stateText != null) stateText.text = "DISABLED";
        if (heatText != null) heatText.text = "";
    }

    // --------------------------------------------------------
    // HOVER visuals (called every frame by SubsystemButton.Update)
    // --------------------------------------------------------
    public void SetHover(bool hovering, bool rightHeld)
    {
        isHovering = hovering;

        // Venting blocks hover visuals
        if (isVenting) return;

        // If disabled visual skin, do not show hover previews (but still allow visual cycling via K)
        if (visualState == SubsystemVisualState.Disabled)
        {
            // keep disabled visuals
            ApplyVisualState();
            return;
        }

        // If RMB not held, revert visuals to current visual state
        if (!rightHeld)
        {
            ApplyVisualState();
            return;
        }

        // RMB held: preview hover top bar when hovering
        if (isHovering)
        {
            topLabelImage.color = hoverTopColor;
            if (titleText != null) titleText.color = titleHover;
        }
        else
        {
            ApplyVisualState();
        }
    }

    // --------------------------------------------------------
    // VISUAL STATE CYCLE (RMB + K) while hovering
    // --------------------------------------------------------
    public void CycleVisualState()
    {
        if (isVenting) return; // ignore while venting

        // Cycle even when visualState == Disabled (user requested hover-based cycling)
        visualState = visualState switch
        {
            SubsystemVisualState.Normal => SubsystemVisualState.Damaged,
            SubsystemVisualState.Damaged => SubsystemVisualState.Disabled,
            SubsystemVisualState.Disabled => SubsystemVisualState.Normal,
            _ => SubsystemVisualState.Normal
        };

        ApplyVisualState();
        UpdateStateInstant();
    }

    // --------------------------------------------------------
    // FUNCTIONAL STATE CHANGE (RMB + LMB while hovering)
    // - Immediate change: set new functional state immediately, notify HeatManager,
    //   then start percent animation from 0 → 100. If a new request arrives mid-animation,
    //   it cancels and restarts (overwrite).
    // --------------------------------------------------------
    public void AdvanceFunctionalStateImmediate()
    {
        if (isVenting) return;
        if (!isHovering) return;                          // only while hovering
        if (visualState == SubsystemVisualState.Disabled) return;

        // Determine next functional state relative to current funcState (works regardless of transition)
        SubsystemFunctionalState next = funcState switch
        {
            SubsystemFunctionalState.Idle => SubsystemFunctionalState.Ready,
            SubsystemFunctionalState.Ready => SubsystemFunctionalState.Surge,
            SubsystemFunctionalState.Surge => SubsystemFunctionalState.Idle,
            _ => SubsystemFunctionalState.Idle
        };

        // Immediately set functional state and notify HeatManager BEFORE animation
        funcState = next;
        OnFunctionalStateChanged?.Invoke(this, funcState);

        // Stop current animation and start a fresh one
        if (stateRoutine != null) StopCoroutine(stateRoutine);
        stateRoutine = StartCoroutine(FunctionalProgressRoutine(funcState));
    }

    // This coroutine purely animates the percent display (0 → 100%) and updates the heat text/color.
    IEnumerator FunctionalProgressRoutine(SubsystemFunctionalState forState)
    {
        isTransitioning = true;

        float duration = 0.5f;
        float timer = 0f;

        // small UX punch
        //if (stateText != null) stateText.transform.DOPunchScale(Vector3.one * 0.12f, 0.35f, 8, 1f);
        //if (heatText != null) heatText.transform.DOPunchScale(Vector3.one * 0.12f, 0.35f, 8, 1f);

        Color targetColor = GetStateColor(forState);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float p = Mathf.Clamp01(timer / duration) * 100f;

            if (stateText != null)
            {
                stateText.color = targetColor;
                stateText.text = $"{forState.ToString().ToUpper()} {Mathf.RoundToInt(p)}%";
            }

            if (heatText != null)
            {
                heatText.color = targetColor;
                heatText.text = GetHeatStringFunctional(forState);
            }

            yield return null;
        }

        // end-of-animation: ensure proper instant text
        UpdateStateInstant();

        isTransitioning = false;
        stateRoutine = null;
    }

    // --------------------------------------------------------
    // Update display instantly to reflect current funcState/visualState
    // --------------------------------------------------------
    private void UpdateStateInstant()
    {
        // Visual disabled overrides everything display-wise
        if (visualState == SubsystemVisualState.Disabled)
        {
            if (stateText != null) stateText.text = "DISABLED";
            if (heatText != null) heatText.text = "";
            ApplyVisualState();
            return;
        }

        // Normal/damaged: show functional state & heat
        if (stateText != null)
        {
            stateText.text = funcState.ToString().ToUpper();
            stateText.color = GetStateColor(funcState);
        }

        if (heatText != null)
        {
            heatText.text = GetHeatStringFunctional(funcState);
            heatText.color = GetStateColor(funcState);
        }

        ApplyVisualState();
    }

    private Color GetStateColor(SubsystemFunctionalState state)
    {
        return state switch
        {
            SubsystemFunctionalState.Ready => readyColor,
            SubsystemFunctionalState.Surge => surgeColor,
            _ => Color.white
        };
    }

   

    // --------------------------------------------------------
    // VENTING handlers (block interactions)
    // --------------------------------------------------------
    private void HandleVentingStart()
    {
        if (isVenting) return;
        isVenting = true;

        // Cache functional state so we can restore it
        cachedFuncState = funcState;

        // Stop any running progress coroutine
        if (stateRoutine != null) StopCoroutine(stateRoutine);
        stateRoutine = null;
        isTransitioning = false;

        // freeze display
        ApplyVisualState();
        UpdateStateInstant();
    }

    private void HandleVentingStop()
    {
        isVenting = false;

        // restore functional state
        funcState = cachedFuncState;

        UpdateStateInstant();
        ApplyVisualState();
    }
}
