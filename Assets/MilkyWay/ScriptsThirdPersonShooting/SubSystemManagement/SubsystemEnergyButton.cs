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

    private bool isLocked = false;   // NEW FLAG

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Subscribe to venting events
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
            return; //No interaction during venting

        if (navigator != null)
            navigator.SetSelectedSubsystem(subsystem);

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            bool success = manager.TryAllocateEnergy(subsystem);
            if (success)
                PlayAllocateSound();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            bool success = manager.TryDeallocateEnergy(subsystem);
            if (success)
                PlayDeallocateSound();
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
