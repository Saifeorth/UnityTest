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
        // Kill all tweens associated with this script
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
    // Validation (prevents crashes)
    // ---------------------------------------------------------
    private bool Validate()
    {
        if (energyManager == null || heatManager == null)
            return false;

        if (energyManager.subsystems == null || energyManager.subsystems.Length == 0)
            return false;

        // Clamp index safely
        selectedIndex = Mathf.Clamp(selectedIndex, 0, energyManager.subsystems.Length - 1);
        return true;
    }

    // ---------------------------------------------------------
    // SELECTION MODE:
    // - Active when: GUI open OR space is held
    // ---------------------------------------------------------
    private bool IsSelectionModeActive()
    {
        return guiOpen || Input.GetKey(KeyCode.Space);
    }

    // ---------------------------------------------------------
    // MODE TOGGLING
    // ---------------------------------------------------------
    private void HandleMode()
    {
        // Toggle GUI mode
        if (Input.GetKeyDown(uiToggleKey))
        {
            guiOpen = !guiOpen;

            if (guiOpen)
                UpdateHighlight(true);
            else
                ResetAllScales(); // Clear selection highlight
        }

        // Space key opens temporary selection mode
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdateHighlight(true);
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            // If GUI isn't open, remove highlight
            if (!guiOpen)
                ResetAllScales();
        }
    }

    // ---------------------------------------------------------
    // NAVIGATION (A/D)
    // ---------------------------------------------------------
    private void HandleNavigation()
    {
        // Disable navigation during venting
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
            if (selectedIndex < 0)
                selectedIndex = energyManager.subsystems.Length - 1;

            UpdateHighlight();
        }
    }

    // ---------------------------------------------------------
    // ALLOCATION (W/S)
    // ---------------------------------------------------------
    private void HandleAllocation()
    {
        if (heatManager.isVenting)
            return;

        var subsystem = energyManager.subsystems[selectedIndex];

        // Get the button component on this subsystem
        var button = subsystem.GetComponent<SubsystemEnergyButton>();

        if (Input.GetKeyDown(KeyCode.W))
        {
            bool success = energyManager.TryAllocateEnergy(subsystem);
            if (success && button != null)
                button.PlayAllocateSound();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            bool success = energyManager.TryDeallocateEnergy(subsystem);
            if (success && button != null)
                button.PlayDeallocateSound();
        }
    }

    // ---------------------------------------------------------
    // HIGHLIGHT UI
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

        // scale up selected
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
