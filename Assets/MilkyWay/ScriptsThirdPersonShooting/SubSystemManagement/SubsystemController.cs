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

    [Header("UI")]
    public TextMeshProUGUI subsystemNameText;
    public Image[] energyBars;
    public Image[] energyFills;
    public Image enabledIcon;

    [Header("Runtime")]
    public int currentAllocated = 0;
    public bool isEnabled = false;

    // EVENTS
    public event Action<int> onEnergyChanged;
    public Action<SubsystemController> onSubsystemEnabled;
    public Action<SubsystemController> onSubsystemDisabled;

    private void Start()
    {
        InitializeUI();
        UpdateBars(false);
        UpdateState();
    }

    private void InitializeUI()
    {
        subsystemNameText.text = energyData.systemName;

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

        enabledIcon.enabled = false;
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
        bool newState = currentAllocated >= energyData.requiredEnergy;

        if (newState != isEnabled)
        {
            isEnabled = newState;

            if (isEnabled)
            {
                enabledIcon.enabled = true;
                enabledIcon.transform.localScale = Vector3.one * 0.5f;
                enabledIcon.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

                onSubsystemEnabled?.Invoke(this);
            }
            else
            {
                enabledIcon.transform.DOScale(0f, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => enabledIcon.enabled = false);

                onSubsystemDisabled?.Invoke(this);
            }
        }

        onEnergyChanged?.Invoke(currentAllocated);
    }
}
