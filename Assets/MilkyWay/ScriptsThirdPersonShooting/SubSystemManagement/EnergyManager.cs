using DG.Tweening;
using DG.Tweening.Core.Easing;
using System;
using UnityEngine;
using UnityEngine.UI;

public class EnergyManager : MonoBehaviour
{
    public static Action onAnyEnergyChanged;

    [Header("Energy")]
    public int totalEnergy = 10;
    private int remainingEnergy;

    [Header("UI")]
    public Image[] globalBars;

    [Header("Subsystems")]
    public SubsystemController[] subsystems;

    private void Start()
    {
        remainingEnergy = totalEnergy;
        RefreshUI();
    }


    private void Awake()
    {
        HeatManager.OnVentingStart += ForceDeallocate;
    }

    private void OnDestroy()
    {
        HeatManager.OnVentingStart -= ForceDeallocate;
    }

    public bool TryAllocateEnergy(SubsystemController s)
    {
        if (remainingEnergy == 0) return false;
        if (s.currentAllocated >= s.energyData.requiredEnergy) return false;

        s.currentAllocated++;
        remainingEnergy--;

        s.UpdateBars();
        s.UpdateState();
        RefreshUI();

        onAnyEnergyChanged?.Invoke();
        return true;
    }

    public bool TryDeallocateEnergy(SubsystemController s)
    {
        if (s.currentAllocated <= 0) return false;

        s.currentAllocated--;
        remainingEnergy++;

        s.UpdateBars();
        s.UpdateState();
        RefreshUI();

        onAnyEnergyChanged?.Invoke();
        return true;
    }

    private void RefreshUI()
    {
        for (int i = 0; i < globalBars.Length; i++)
        {
            Image img = globalBars[i];

            if (i < remainingEnergy)
            {
                img.enabled = true;
                img.DOFade(1f, 0.15f);
                img.transform.DOPunchScale(Vector3.one * 0.15f, 0.15f);
            }
            else
            {
                img.DOFade(0f, 0.15f)
                    .OnComplete(() => img.enabled = false);
            }
        }
    }

    public void ForceDeallocate()
    {
        foreach (var subsystem in subsystems)
        {
            if (subsystem == null) continue;

            if (subsystem.currentAllocated > 0)
            {
                remainingEnergy += subsystem.currentAllocated;
                subsystem.currentAllocated = 0;

                subsystem.UpdateBars();
                subsystem.UpdateState();
            }
        }

        RefreshUI();
    }
}
