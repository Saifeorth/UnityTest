using UnityEngine;
using UnityEngine.EventSystems;

public class SubsystemEnergyButton : MonoBehaviour, IPointerClickHandler
{
    public EnergyManager manager;
    public SubsystemController subsystem;
    public SubsystemKeyboardNavigator navigator;

    public AudioSource audioSource;

    public AudioClip allocateEnergyClip;
    public AudioClip deallocateEnergyClip;

    private bool isLocked = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Lock when venting
        HeatManager.OnVentingStart += LockButton;
        HeatManager.OnVentingStop += UnlockButton;
    }

    private void OnDestroy()
    {
        HeatManager.OnVentingStart -= LockButton;
        HeatManager.OnVentingStop -= UnlockButton;
    }

    private void LockButton()
    {
        isLocked = true;
    }

    private void UnlockButton()
    {
        isLocked = false;
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLocked)
            return; // No interaction during venting

        if (navigator != null)
            navigator.SetSelectedSubsystem(subsystem);

        bool energyMode = manager.energyDependencyEnabled;

        // --------------------------------------------------------
        // MODE 1: ENERGY DEPENDENCY ENABLED (FULL ENERGY SYSTEM)
        // --------------------------------------------------------
        if (energyMode)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                bool allocated = manager.TryAllocateEnergy(subsystem);
                if (allocated)
                    PlayAllocateSound();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                bool deallocated = manager.TryDeallocateEnergy(subsystem);
                if (deallocated)
                    PlayDeallocateSound();
            }
            return;
        }

        // --------------------------------------------------------
        // MODE 2: ENERGY DEPENDENCY DISABLED (DIRECT TOGGLE)
        // --------------------------------------------------------
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!subsystem.isEnabled && !subsystem.isTransitioning)
            {
                subsystem.EnableSubsystem();
                PlayAllocateSound();
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (subsystem.isEnabled && !subsystem.isTransitioning)
            {
                subsystem.DisableSubsystem();
                PlayDeallocateSound();
            }
        }
    }


    public void PlayAllocateSound()
    {
        if (audioSource != null && allocateEnergyClip != null)
            audioSource.PlayOneShot(allocateEnergyClip);
    }

    public void PlayDeallocateSound()
    {
        if (audioSource != null && deallocateEnergyClip != null)
            audioSource.PlayOneShot(deallocateEnergyClip);
    }
}
