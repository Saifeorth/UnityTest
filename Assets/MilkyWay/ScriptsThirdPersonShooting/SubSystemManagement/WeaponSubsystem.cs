using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSubsystem : MonoBehaviour
{
    public Weapon weapon;
    public WeaponManager weaponManager;

    [Header("Heat Data")]
    public HeatSubsystemData heatData;

    [Header("UI References")]
    public Image topLabelImage;
    public Image bottomLabelImage;
    public Image bgImage;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI titleIndexText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI heatText;
    public TextMeshProUGUI descriptionText;

    // ---------------- Colors ----------------
    public Color normalColor = new(0.84f, 0.93f, 0.92f);
    public Color hoverColor = Color.black;

    public Color titleHover = Color.white;

    public Color readyColor = new(0f, 1f, 0.05f);
    public Color surgeColor = new(1f, 0.8f, 0f);

    private readonly Color damagedColor = new(1f, 0.8f, 0f); // yellow
    private readonly Color disabledColor = new(1f, 0f, 0f);  // red

    // ---------------- Runtime ----------------
    public SubsystemFunctionalState funcState = SubsystemFunctionalState.Idle;
    public SubsystemVisualState visualState = SubsystemVisualState.Normal;

    public bool isSelected = false;
    private bool isTransitioning = false;

    public event Action<WeaponSubsystem, SubsystemFunctionalState> OnFunctionalStateChanged;

    private Coroutine stateRoutine;

    private RectTransform rect;
    private Vector2 originalSize;

    private RectTransform bottomRect;
    private Vector2 bottomOriginalSize;

    public bool isVenting = false;     // when true → everything is paused
    private bool cachedIsSelected = false;
    private SubsystemFunctionalState cachedFuncState;



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
        originalSize = rect.sizeDelta;

        bottomRect = bottomLabelImage.GetComponent<RectTransform>();
        bottomOriginalSize = bottomRect.sizeDelta;

        titleText.text = name;

        if (descriptionText != null)
        {
            var c = descriptionText.color;
            descriptionText.color = new Color(c.r, c.g, c.b, 0f);
        }

        ApplyVisualState();
        UpdateStateInstant();
    }

    // --------------------------------------------------------
    // HEAT TEXT
    // --------------------------------------------------------
    private string GetHeatStringFunctional()
    {
        switch (funcState)
        {
            case SubsystemFunctionalState.Idle: return "0°";
            case SubsystemFunctionalState.Ready: return $"{heatData.passiveHeatReady}°";
            case SubsystemFunctionalState.Surge:
                float surgeHeat = heatData.passiveHeatSurge;
                return $"{surgeHeat}°";
        }
        return "0°";
    }

    private string GetHeatString()
    {
        if (visualState == SubsystemVisualState.Disabled)
            return "";

        return GetHeatStringFunctional();
    }

    private void UpdateHeatText()
    {
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
                ApplyNormalOrSelectedVisuals();
                break;
        }
    }

    private void ApplyNormalOrSelectedVisuals()
    {
        if (isSelected)
        {
            topLabelImage.color = hoverColor;
            titleText.color = Color.white;
            titleIndexText.color = Color.white;
            bgImage.color = normalColor;

            if (descriptionText != null)
                descriptionText.alpha = 1f;
        }
        else
        {
            topLabelImage.color = normalColor;
            titleText.color = Color.black;
            titleIndexText.color = Color.black;
            bgImage.color = normalColor;

            if (descriptionText != null)
                descriptionText.alpha = 0f;
        }
    }

    private void ApplyDamagedVisuals()
    {
        bgImage.color = damagedColor;
        topLabelImage.color = damagedColor;
        titleText.color = Color.black;
        titleIndexText.color = Color.black;
        titleIndexText.color = Color.black;
        descriptionText.alpha = 0f;
        heatText.text = GetHeatString();
    }

    private void ApplyDisabledVisuals()
    {
        bgImage.color = disabledColor;
        topLabelImage.color = disabledColor;
        titleText.color = Color.black;
        titleIndexText.color = Color.black;
        stateText.text = "DISABLED";
        heatText.text = "";
    }

    // --------------------------------------------------------
    // SELECTION
    // --------------------------------------------------------
    public void ApplySelectedState()
    {
        if (isVenting) return;             // ← NEW
        if (visualState == SubsystemVisualState.Disabled)
            return;

        isSelected = true;

        float newHeight = originalSize.y * 1.5f;
        rect.DOSizeDelta(new(originalSize.x, newHeight), 0.25f).SetEase(Ease.OutBack);

        float bottomHeight = bottomOriginalSize.y * 2f;
        bottomRect.DOSizeDelta(new(bottomOriginalSize.x, bottomHeight), 0.25f);

        ApplyVisualState();
        UpdateHeatText();
        UpdateDescription();
    }

    public void Deselect()
    {
        isSelected = false;

        rect.DOSizeDelta(originalSize, 0.25f);
        bottomRect.DOSizeDelta(bottomOriginalSize, 0.25f);

        ApplyVisualState();
        UpdateHeatText();
        UpdateDescription();
    }

    // --------------------------------------------------------
    // FUNCTIONAL STATE MACHINE
    // --------------------------------------------------------
    public void AdvanceFunctionalState()
    {
        // keep existing guard if you want keyboard-only or selection-only behavior
        // This method can be used for non-immediate (if you want) — but we'll
        // prefer AdvanceFunctionalStateImmediate() in UI now.
        if (isVenting) return;
        if (!isSelected) return;
        if (visualState == SubsystemVisualState.Disabled) return;
        if (isTransitioning) return;

        SubsystemFunctionalState next = GetNextFunctionalState(funcState);
        StartFunctionalTransition(next);
    }

    public void AdvanceFunctionalStateImmediate()
    {
        if (isVenting) return;
        if (visualState == SubsystemVisualState.Disabled) return;
        // We allow immediate toggles regardless of isSelected (user requested "on hover"),
        // but if you want only when selected, uncomment:
        // if (!isSelected) return;

        SubsystemFunctionalState next = GetNextFunctionalState(funcState);

        // Stop any running transition
        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        // Immediately set functional state to next
        funcState = next;


        // Update visuals/text instantly to show the new state and 0%
        UpdateStateInstant();
        ApplyVisualState();
        UpdateHeatText();

        // Start percentage animation from 0 -> 100 for the new state
        StartFunctionalTransition(next);
    }

    private void StartFunctionalTransition(SubsystemFunctionalState next)
    {
        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        stateRoutine = StartCoroutine(FunctionalTransitionRoutine(next));
    }

    private SubsystemFunctionalState GetNextFunctionalState(SubsystemFunctionalState current)
    {
        return current switch
        {
            SubsystemFunctionalState.Idle => SubsystemFunctionalState.Ready,
            SubsystemFunctionalState.Ready => SubsystemFunctionalState.Surge,
            SubsystemFunctionalState.Surge => SubsystemFunctionalState.Idle,
            _ => SubsystemFunctionalState.Idle
        };
    }


    IEnumerator FunctionalTransitionRoutine(SubsystemFunctionalState next)
    {
        // Animate percentage from 0 -> 100 while showing the "next" label
        isTransitioning = true;

        float duration = 0.5f;
        float timer = 0f;

        Color targetColor = GetStateColor(next);

        // Start at 0% with the "next" label
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float p = Mathf.Clamp01(timer / duration) * 100f;

            stateText.color = targetColor;
            heatText.color = targetColor;

            stateText.text = $"{next.ToString().ToUpper()} {Mathf.RoundToInt(p)}%";
            heatText.text = GetHeatStringFunctional();

            yield return null;
        }

        // At the end of the animation ensure the functional state is applied (safe)
        funcState = next;

        // Notify subscribers that a (final) state set happened at end-of-transition too
        OnFunctionalStateChanged?.Invoke(this, funcState);


        UpdateStateInstant();
        ApplyVisualState();
        UpdateHeatText();

        isTransitioning = false;
    }

    private void UpdateStateInstant()
    {
        if (visualState == SubsystemVisualState.Disabled)
        {
            stateText.text = "DISABLED";
            heatText.text = "";
            return;
        }

        stateText.text = funcState.ToString().ToUpper();
        stateText.color = GetStateColor(funcState);
        heatText.text = GetHeatString();
        heatText.color = GetStateColor(funcState);
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
    // DEBUG VISUAL CYCLER (RMB + K)
    // --------------------------------------------------------
    public void CycleVisualState()
    {
        if (isTransitioning) return;

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

    #region HOVER
    public void SetHover(bool isHovering, bool rightHeld)
    {
        if (isVenting) return;
        if (visualState == SubsystemVisualState.Disabled)
            return;

        // If right-click NOT held, simply apply current visual state
        if (!rightHeld)
        {
            if (!isSelected)
                ApplyVisualState();
            return;
        }

        // ----- Right-click held -----
        // Hover highlight should not override visual state permanently
        if (!isSelected && isHovering)
        {
            topLabelImage.color = hoverColor;
            titleText.color = titleHover;
            titleIndexText.color = titleHover;
        }
        else
        {
            ApplyVisualState();
        }
    }
    #endregion

    private void HandleVentingStart()
    {
        if (isVenting) return;

        isVenting = true;

        // Cache states so we can restore when venting ends
        cachedIsSelected = isSelected;
        cachedFuncState = funcState;

        // Stop transitions
        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        isTransitioning = false;

        // Freeze visuals exactly as they are
        ApplyVisualState();
        UpdateStateInstant();
        UpdateHeatText();
    }

    private void HandleVentingStop()
    {
        isVenting = false;

        // Restore previous selection
        if (cachedIsSelected)
            ApplySelectedState();
        else
            Deselect();

        // Restore functional state
        funcState = cachedFuncState;

        UpdateStateInstant();
        ApplyVisualState();
        UpdateHeatText();
    }

    public void UpdateDescription()
    {
        if (weapon == null || descriptionText == null)
            return;

        string line1;

        if (weapon.isReloading)
            line1 = "Reloading...";
        else
            line1 = $"Current Clip: {weapon.currentClipAmmo}";

        string line2 = $"Cargo: {weapon.cargoAmmo}";

        descriptionText.text = $"{line1}\n{line2}";
    }

}
