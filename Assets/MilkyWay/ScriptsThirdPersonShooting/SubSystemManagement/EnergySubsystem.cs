using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Energy Subsystem")]
public class EnergySubsystem : ScriptableObject
{
    public string systemName;
    public int requiredEnergy = 1;
    public Sprite icon; // optional for UI
}
