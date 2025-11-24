using UnityEngine;

[CreateAssetMenu(menuName = "Ships/Heat Subsystem Data")]
public class HeatSubsystemData : ScriptableObject
{
    [Header("Heat Generation")]
    public float heatPerSecondActive = 0f;     // continuous heat
    public float burstHeat = 0f;               // per fire or per action

    [Header("Optional")]
    public bool onlyGenerateWhenEnabled = true;
}
