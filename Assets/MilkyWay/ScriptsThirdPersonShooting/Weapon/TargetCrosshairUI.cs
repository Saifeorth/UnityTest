using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TargetCrosshairUI : MonoBehaviour
{
    public WeaponManager weaponManager;

    public Image[] crosshairBorders; // 4 images

    [Header("Colors")]
    public Color idleColor = Color.white;
    public Color acquiringColor = Color.yellow;
    public Color lockedColor = Color.red;

    [Header("Scale")]
    public float idleScale = 1f;
    public float acquiringScale = 1.1f;
    public float lockedScale = 1.2f;

    [Header("Targeting Text")]
    public TMP_Text lockPromptText;

    [Header("Lerp")]
    public float visualLerpSpeed = 8f;

    Vector3 targetScale;
    Color targetColor;

    void Update()
    {
        UpdateVisualTargets();
        UpdateLockText();
        ApplyVisuals();
    }

    void UpdateVisualTargets()
    {
        switch (weaponManager.CurrentLockState)
        {
            case WeaponManager.TargetLockState.None:
                targetScale = Vector3.one * idleScale;
                targetColor = idleColor;
                break;

            case WeaponManager.TargetLockState.Acquiring:
                targetScale = Vector3.one * acquiringScale;
                targetColor = acquiringColor;
                break;

            case WeaponManager.TargetLockState.Locked:
                targetScale = Vector3.one * lockedScale;
                targetColor = lockedColor;
                break;
        }
    }

    void ApplyVisuals()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * visualLerpSpeed
        );

        foreach (var img in crosshairBorders)
        {
            img.color = Color.Lerp(
                img.color,
                targetColor,
                Time.deltaTime * visualLerpSpeed
            );
        }
    }

    void UpdateLockText()
    {
        if (!lockPromptText)
            return;

        switch (weaponManager.CurrentLockState)
        {
            case WeaponManager.TargetLockState.None:
                lockPromptText.text = weaponManager.CanLockTarget() ? "T" : "";
                break;

            case WeaponManager.TargetLockState.Acquiring:
                lockPromptText.text = "Targeting";
                break;

            case WeaponManager.TargetLockState.Locked:
                lockPromptText.text = "";
                break;
        }
    }
}
