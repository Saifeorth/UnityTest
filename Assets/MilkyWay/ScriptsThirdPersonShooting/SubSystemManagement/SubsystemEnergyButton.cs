using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

public class SubsystemEnergyButton : MonoBehaviour, IPointerClickHandler
{
    public EnergyManager manager;
    public SubsystemController subsystem;

    public AudioSource audioSource;

    public AudioClip allocateEnergyClip;
    public AudioClip deallocateEnergyClip;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            bool success = manager.TryAllocateEnergy(subsystem);
            if (success)
            {
                audioSource.PlayOneShot(allocateEnergyClip);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            bool success = manager.TryDeallocateEnergy(subsystem);
            if (success)
            {
                audioSource.PlayOneShot(deallocateEnergyClip);
            }
        }
    }
}
