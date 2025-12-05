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
    public bool energyDependencyEnabled = false;
    private int remainingEnergy;

    [Header("UI")]
    public GameObject energyBarsContainer;
    public Image[] globalBars;

    [Header("Subsystems")]
    public SubsystemController[] subsystems;

    private void Start()
    {
        remainingEnergy = totalEnergy;
        SetEnergyDependency(energyDependencyEnabled);
        RefreshUI();
    }


    private void Awake()
    {
        HeatManager.OnVentingStart += PauseInputs;
    }

    private void OnDestroy()
    {
        HeatManager.OnVentingStart -= PauseInputs;
    }

    public bool TryAllocateEnergy(SubsystemController s)
    {
        if (!energyDependencyEnabled) return false;

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
        if (!energyDependencyEnabled) return false;

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

    public void SetEnergyDependency(bool enabled)
    {
        energyDependencyEnabled = enabled;
        energyBarsContainer.SetActive(energyDependencyEnabled);

        foreach (var subsystem in subsystems)
            subsystem.ToggleEnergyUI(energyDependencyEnabled);

    }

   



    private void PauseInputs()
    {
        // Do nothing to energy values
        // Subsystem buttons will lock themselves (via SubsystemEnergyButton)
    }
}
