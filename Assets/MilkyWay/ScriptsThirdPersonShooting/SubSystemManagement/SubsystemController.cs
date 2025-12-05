using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class SubsystemController : MonoBehaviour
{
    [Header("Subsystem Data")]
    public EnergySubsystem energyData;
    public HeatSubsystemData heatData;
    public EnergyManager energyManager;

    [Header("UI")]
    public TextMeshProUGUI subsystemNameText;
    public Image[] energyBars;
    public Image[] energyFills;
    public Image enabledIcon;

    [Header("Runtime")]
    public int currentAllocated = 0;
    public bool isEnabled = false;
    public bool isVenting = false;
    public bool isTransitioning = false;

    // EVENTS
    public event Action<int> onEnergyChanged;
    public Action<SubsystemController> onSubsystemEnabled;
    public Action<SubsystemController> onSubsystemDisabled;

    private void Awake()
    {
        HeatManager.OnVentingStart += StopSubsystem;
        HeatManager.OnVentingStop += ResumeSubsystem;
    }

    private void OnDestroy()
    {
        HeatManager.OnVentingStart -= StopSubsystem;
        HeatManager.OnVentingStop -= ResumeSubsystem;
    }


    private void StopSubsystem()
    {
        isVenting = true;
    }

    private void ResumeSubsystem()
    {
        isVenting = false;
    }

    private void Start()
    {
        InitializeUI();
        UpdateBars(false);
        UpdateState();
    }

    private void InitializeUI()
    {
        subsystemNameText.text = energyData.systemName;


        //old code to set active based on required energy

        if (energyManager.energyDependencyEnabled)
        {
            for (int i = 0; i < energyBars.Length; i++)
            {
                bool active = i < energyData.requiredEnergy;
                energyBars[i].gameObject.SetActive(active);

                if (i < energyFills.Length)
                {
                    energyFills[i].gameObject.SetActive(active);
                    energyFills[i].transform.localScale = Vector3.zero;
                }
            }
        }
        else 
        {

            for (int i = 0; i < energyBars.Length; i++)
            {
                energyBars[i].gameObject.SetActive(false);
            }
        }

        enabledIcon.enabled = true;       // must be enabled so fillAmount is visible
        enabledIcon.fillAmount = 0f;      // ✔ ensures smooth first activation
        enabledIcon.gameObject.SetActive(true);
    }

    public void UpdateBars(bool animate = true)
    {
        for (int i = 0; i < energyData.requiredEnergy; i++)
        {
            Image fill = energyFills[i];
            bool shouldBeFilled = (i < currentAllocated);

            if (animate)
            {
                fill.DOKill();
                fill.transform.DOScale(
                    shouldBeFilled ? Vector3.one : Vector3.zero,
                    shouldBeFilled ? 0.2f : 0.15f
                ).SetEase(shouldBeFilled ? Ease.OutBack : Ease.InBack);
            }
            else
            {
                fill.transform.localScale = shouldBeFilled ? Vector3.one : Vector3.zero;
            }
        }
    }

    public void UpdateState()
    {

        if (isVenting)
        {
            // Force disable UI animation but keep icon visible at 0
            isEnabled = false;
            enabledIcon.DOKill();
            enabledIcon.fillAmount = 0f;
            enabledIcon.enabled = true;
            return;
        }

        bool shouldBeEnabled = currentAllocated >= energyData.requiredEnergy;

        // No change → do nothing
        if (shouldBeEnabled == isEnabled)
        {
            onEnergyChanged?.Invoke(currentAllocated);
            return;
        }

        // State changed
        if (shouldBeEnabled)
            EnableSubsystem();
        else
            DisableSubsystem();

        onEnergyChanged?.Invoke(currentAllocated);
    }

    public void EnableSubsystem()
    {
        if (isTransitioning || isEnabled || isVenting)
            return;

        isTransitioning = true;
        isEnabled = true;

        enabledIcon.DOKill();
        enabledIcon.enabled = true;

        // Fill 0 → 1 in 1 sec
        enabledIcon.DOFillAmount(1f, 1f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                isTransitioning = false;
                onSubsystemEnabled?.Invoke(this);
            });
    }

    public void DisableSubsystem()
    {
        if (isTransitioning || !isEnabled || isVenting)
            return;

        isTransitioning = true;
        isEnabled = false;

        enabledIcon.DOKill();

        // Fill 1 → 0 in 1 sec
        enabledIcon.DOFillAmount(0f, 1f)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                enabledIcon.enabled = true;
                isTransitioning = false;
                onSubsystemDisabled?.Invoke(this);
            });
    }
    public void ToggleEnergyUI(bool show)
    {
        if (energyManager.energyDependencyEnabled)
        {
            for (int i = 0; i < energyBars.Length; i++)
            {
                bool active = i < energyData.requiredEnergy;
                energyBars[i].gameObject.SetActive(active);

                if (i < energyFills.Length)
                {
                    energyFills[i].gameObject.SetActive(active);
                    energyFills[i].transform.localScale = Vector3.zero;
                }
            }

            UpdateBars();
            UpdateState();
        }
        else 
        {
            for (int i = 0; i < energyBars.Length; i++)
                energyBars[i].gameObject.SetActive(show);
        }
    }

}
