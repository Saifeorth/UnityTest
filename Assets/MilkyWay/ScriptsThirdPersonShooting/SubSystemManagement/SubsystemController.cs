using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class SubsystemController : MonoBehaviour
{
    [Header("Subsystem Data (SO)")]
    public EnergySubsystem data;

    [Header("Subsystem UI")]
    public TextMeshProUGUI subsystemNameText;

    public Image[] energyBars;
    public Image[] energyFills;

    public Image subsystemEnabledImage;

    [Header("Runtime State")]
    public int currentAllocated = 0;
    public bool isEnabled = false;

    public event Action<int> onEnergyChanged;

    private void Start()
    {
        InitializeUI();
        UpdateBars(animated: false);
        UpdateSubsystemState();
    }

    private void InitializeUI()
    {
        if (subsystemNameText != null)
            subsystemNameText.text = data.systemName;

        for (int i = 0; i < energyBars.Length; i++)
        {
            bool shouldShow = i < data.requiredEnergy;

            if (energyBars[i] != null)
                energyBars[i].gameObject.SetActive(shouldShow);

            if (i < energyFills.Length && energyFills[i] != null)
            {
                energyFills[i].gameObject.SetActive(shouldShow);
                energyFills[i].transform.localScale = Vector3.zero; // hidden initially
            }
        }

        if (subsystemEnabledImage != null)
        {
            subsystemEnabledImage.enabled = false;
            subsystemEnabledImage.transform.localScale = Vector3.one;
        }
    }

    public void UpdateBars(bool animated = true)
    {
        for (int i = 0; i < data.requiredEnergy; i++)
        {
            if (i >= energyFills.Length)
            {
                Debug.LogWarning($"{data.systemName}: Not enough energyFills assigned!");
                return;
            }

            Image fill = energyFills[i];
            bool shouldBeFilled = i < currentAllocated;

            if (animated)
            {
                if (shouldBeFilled)
                    AnimateFillOn(fill);
                else
                    AnimateFillOff(fill);
            }
            else
            {
                fill.transform.localScale = shouldBeFilled ? Vector3.one : Vector3.zero;
            }
        }
    }

    public void UpdateSubsystemState()
    {
        bool shouldEnable = currentAllocated >= data.requiredEnergy;

        if (shouldEnable != isEnabled)
        {
            isEnabled = shouldEnable;

            if (isEnabled)
                PlaySubsystemEnabledFX();
            else
                PlaySubsystemDisabledFX();
        }

        onEnergyChanged?.Invoke(currentAllocated);

        if (subsystemEnabledImage != null)
            subsystemEnabledImage.enabled = isEnabled;
    }

    #region Fill Animations

    private void AnimateFillOn(Image fill)
    {
        fill.gameObject.SetActive(true);
        fill.DOKill();
        fill.transform.localScale = Vector3.one * 0.2f;

        fill.transform.DOScale(Vector3.one, 0.2f)
            .SetEase(Ease.OutBack);
    }

    private void AnimateFillOff(Image fill)
    {
        fill.DOKill();
        fill.transform.DOScale(Vector3.zero, 0.15f)
            .SetEase(Ease.InBack);
    }

    #endregion

    #region Subsystem FX

    private void PlaySubsystemEnabledFX()
    {
        if (subsystemEnabledImage == null) return;

        subsystemEnabledImage.enabled = true;
        subsystemEnabledImage.DOKill();
        subsystemEnabledImage.transform.localScale = Vector3.one * 0.6f;

        subsystemEnabledImage.transform.DOScale(Vector3.one, 0.35f)
            .SetEase(Ease.OutBack);
    }

    private void PlaySubsystemDisabledFX()
    {
        if (subsystemEnabledImage == null) return;

        subsystemEnabledImage.DOKill();
        subsystemEnabledImage.transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => subsystemEnabledImage.enabled = false);
    }

    #endregion
}
