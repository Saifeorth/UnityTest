using UnityEngine;
using UnityEngine.UI;

public class WeaponUIButton : MonoBehaviour
{
    public int weaponIndex;
    public WeaponManager manager;

    [Header("UI References")]
    public UnityEngine.UI.Outline highlightImage;
    public Image loadingOverlay; // ← Assign an Image overlay (e.g. gray, 60% alpha)

    public void OnClick()
    {
        manager.SelectWeapon(weaponIndex);
    }

    public void SetSelected(bool selected)
    {
        if (highlightImage)
            highlightImage.enabled = selected;
    }

    public void SetLoading(bool isLoading)
    {
        if (loadingOverlay)
            loadingOverlay.gameObject.SetActive(isLoading);
    }
}
