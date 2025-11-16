using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WeaponSubsystem : MonoBehaviour
{
    public Weapon weapon;

    [Header("UI References")]
    public Image[] energyBars;
    public Image[] energyFills;
    public UnityEngine.UI.Outline subWeaponEnabledImage;

    [Header("Subsystem UI")]
    public TextMeshProUGUI subWeaponNameText;

    [Header("Runtime State")]
    public int currentAllocated = 0;
    public bool isEnabled = false;

    [Header("Fire Rate UI")]
    public Image fireRateFill;

    // Simple animation values
    private const float popScale = 1.12f;
    private const float popTime = 0.12f;

    private void Update()
    {
        if (!isEnabled || weapon == null || fireRateFill == null)
            return;

        // Smooth UI fill update
        fireRateFill.DOFillAmount(weapon.fireCooldownPercent, 0.1f);
    }

    public void InitializeUI()
    {
        if (subWeaponNameText != null)
            subWeaponNameText.text = weapon.weaponName;

        for (int i = 0; i < energyBars.Length; i++)
        {
            bool show = i < weapon.requiredEnergy;

            if (energyBars[i] != null)
                energyBars[i].gameObject.SetActive(show);

            if (i < energyFills.Length && energyFills[i] != null)
            {
                energyFills[i].gameObject.SetActive(show);
                energyFills[i].enabled = false;
                energyFills[i].transform.localScale = Vector3.one;
            }
        }

        if (fireRateFill != null)
        {
            fireRateFill.gameObject.SetActive(false);
            fireRateFill.fillAmount = 0f;
        }

        if (subWeaponEnabledImage != null)
            subWeaponEnabledImage.enabled = false;
    }

    public void UpdateBars()
    {
        for (int i = 0; i < weapon.requiredEnergy; i++)
        {
            if (i >= energyFills.Length)
            {
                Debug.LogWarning($"{weapon.weaponName}: Missing fill images!");
                return;
            }

            Image fill = energyFills[i];

            if (i < currentAllocated)
            {
                // Enable instantly
                fill.enabled = true;

                // Pop animation
                fill.transform.localScale = Vector3.one;
                fill.transform.DOPunchScale(Vector3.one * (popScale - 1f), popTime);
            }
            else
            {
                fill.enabled = false;
            }
        }
    }

    public void UpdateSubsystemState()
    {
        bool shouldEnable = currentAllocated >= weapon.requiredEnergy;

        if (shouldEnable != isEnabled)
        {
            isEnabled = shouldEnable;

            if (isEnabled)
                OnSubWeaponEnabled();
            else
                OnSubWeaponDisabled();
        }

        if (subWeaponEnabledImage != null)
            subWeaponEnabledImage.enabled = isEnabled;
    }

    private void OnSubWeaponEnabled()
    {
        Debug.Log($"{weapon.weaponName} ENABLED");

        if (fireRateFill != null)
        {
            fireRateFill.gameObject.SetActive(true);
            fireRateFill.fillAmount = 1f;
        }
    }

    private void OnSubWeaponDisabled()
    {
        Debug.Log($"{weapon.weaponName} DISABLED");

        if (fireRateFill != null)
        {
            fireRateFill.gameObject.SetActive(false);
            fireRateFill.fillAmount = 0f;
        }
    }
}
