using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SubsystemKeyboardNavigator : MonoBehaviour
{
    [Header("References")]
    public EnergyManager energyManager;
    public HeatManager heatManager;

    [Header("Settings")]
    public KeyCode uiToggleKey = KeyCode.P;
    public float highlightScale = 1.12f;
    public float highlightSpeed = 0.12f;

    private int selectedIndex = 0;
    private bool guiOpen = false;

    private void Start()
    {
        Validate();
        UpdateHighlight(true);
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
    }

    private void Update()
    {
        if (!Validate()) return;

        HandleMode();

        if (!IsSelectionModeActive())
            return;

        HandleNavigation();
        HandleAllocation();
    }

    // ---------------------------------------------------------
    private bool Validate()
    {
        if (energyManager == null || heatManager == null)
            return false;

        if (energyManager.subsystems == null || energyManager.subsystems.Length == 0)
            return false;

        selectedIndex = Mathf.Clamp(selectedIndex, 0, energyManager.subsystems.Length - 1);
        return true;
    }

    private bool IsSelectionModeActive()
    {
        return guiOpen || Input.GetKey(KeyCode.Space);
    }

    // ---------------------------------------------------------
    // GUI / SPACE MODE
    // ---------------------------------------------------------
    private void HandleMode()
    {
        if (Input.GetKeyDown(uiToggleKey))
        {
            guiOpen = !guiOpen;

            if (guiOpen)
                UpdateHighlight(true);
            else
                ResetAllScales();
        }

        if (Input.GetKeyDown(KeyCode.Space))
            UpdateHighlight(true);

        if (Input.GetKeyUp(KeyCode.Space) && !guiOpen)
            ResetAllScales();
    }

    // ---------------------------------------------------------
    // A/D navigation
    // ---------------------------------------------------------
    private void HandleNavigation()
    {
        if (heatManager.isVenting)
            return;

        if (Input.GetKeyDown(KeyCode.D))
        {
            selectedIndex = (selectedIndex + 1) % energyManager.subsystems.Length;
            UpdateHighlight();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = energyManager.subsystems.Length - 1;

            UpdateHighlight();
        }
    }

    // ---------------------------------------------------------
    // ENERGY / DIRECT TOGGLE (W/S)
    // ---------------------------------------------------------
    private void HandleAllocation()
    {
        if (heatManager.isVenting)
            return;

        var subsystem = energyManager.subsystems[selectedIndex];
        var button = subsystem.GetComponent<SubsystemEnergyButton>();
        bool energyMode = energyManager.energyDependencyEnabled;

        // ⭐⭐⭐ MODE 1: Energy dependency ON (use energy system)
        if (energyMode)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                bool success = energyManager.TryAllocateEnergy(subsystem);
                if (success && button != null) button.PlayAllocateSound();
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                bool success = energyManager.TryDeallocateEnergy(subsystem);
                if (success && button != null) button.PlayDeallocateSound();
            }

            return;
        }

        // ⭐⭐⭐ MODE 2: Energy dependency OFF (direct enable/disable)
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (!subsystem.isEnabled && !subsystem.isTransitioning)
            {
                subsystem.EnableSubsystem();
                if (button != null) button.PlayAllocateSound();
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (subsystem.isEnabled && !subsystem.isTransitioning)
            {
                subsystem.DisableSubsystem();
                if (button != null) button.PlayDeallocateSound();
            }
        }
    }

    // ---------------------------------------------------------
    // HIGHLIGHTING
    // ---------------------------------------------------------
    private void ResetAllScales()
    {
        foreach (var sub in energyManager.subsystems)
        {
            var rt = sub.GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.DOKill();
            rt.DOScale(1f, 0.12f)
              .SetEase(Ease.OutQuad)
              .SetId(this);
        }
    }

    private void UpdateHighlight(bool instant = false)
    {
        ResetAllScales();

        var target = energyManager.subsystems[selectedIndex];
        RectTransform rt = target.GetComponent<RectTransform>();

        if (rt == null) return;

        rt.DOKill();
        rt.DOScale(highlightScale, instant ? 0f : highlightSpeed)
            .SetEase(Ease.OutQuad)
            .SetId(this);
    }

    public void SetSelectedSubsystem(SubsystemController target)
    {
        for (int i = 0; i < energyManager.subsystems.Length; i++)
        {
            if (energyManager.subsystems[i] == target)
            {
                selectedIndex = i;
                UpdateHighlight();
                return;
            }
        }
    }
}
