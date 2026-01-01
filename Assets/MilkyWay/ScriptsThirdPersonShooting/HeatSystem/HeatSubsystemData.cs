using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Heat Subsystem Data")]
public class HeatSubsystemData : ScriptableObject
{
    [Header("Heat Generation")]
    public float passiveHeatReady = 0f;
    public float passiveHeatSurge = 1f;
    public float usageHeat = 0f;   
}
