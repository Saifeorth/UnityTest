using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EnergyManager : MonoBehaviour
{
    [Header("Global Energy Bars")]
    public int totalEnergy = 10;
    private int remainingEnergy;

    public Image[] globalEnergyFills;

    [Header("Subsystems")]
    public SubsystemController[] subsystems;

    // Animation values
    private const float fadeDuration = 0.15f;
    private const float popScale = 1.12f;
    private const float popTime = 0.12f;

    private void Start()
    {
        remainingEnergy = totalEnergy;
        InitializeBars();
        UpdateGlobalBars();
    }

    private void InitializeBars()
    {
        for (int i = 0; i < globalEnergyFills.Length; i++)
        {
            if (globalEnergyFills[i] != null)
            {
                globalEnergyFills[i].enabled = false;
                globalEnergyFills[i].transform.localScale = Vector3.one;
            }
        }
    }

    public bool TryAllocateEnergy(SubsystemController target)
    {
        if (remainingEnergy <= 0 || target.currentAllocated >= target.data.requiredEnergy)
            return false;

        target.currentAllocated++;
        remainingEnergy--;

        UpdateGlobalBars();
        target.UpdateBars();
        target.UpdateSubsystemState();

        return true;
    }

    public bool TryDeallocateEnergy(SubsystemController target)
    {
        if (target.currentAllocated <= 0)
            return false;

        target.currentAllocated--;
        remainingEnergy++;

        UpdateGlobalBars();
        target.UpdateBars();
        target.UpdateSubsystemState();

        return true;
    }

    private void UpdateGlobalBars()
    {
        for (int i = 0; i < globalEnergyFills.Length; i++)
        {
            Image fill = globalEnergyFills[i];

            if (i < remainingEnergy)
            {
                // Enable + fade in
                fill.enabled = true;
                fill.DOFade(1f, fadeDuration);

                // POP animation
                fill.transform.localScale = Vector3.one;
                fill.transform.DOPunchScale(Vector3.one * (popScale - 1f), popTime);
            }
            else
            {
                // Fade out and then disable
                fill.DOFade(0f, fadeDuration)
                    .OnComplete(() => fill.enabled = false);
            }
        }
    }
}
