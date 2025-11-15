using UnityEngine;
using System.Collections;

public class WeaponManager : MonoBehaviour
{
    public Weapon[] weapons;
    public int currentWeaponIndex = 0;

    [Header("UI References")]
    public WeaponUIButton[] weaponButtons;
    public AudioClip[] weaponReloadSound;

    public AudioSource audioSource;

    public LayerMask aimLayerMask;

    private Camera mainCam;
    private bool isSwitchingOrReloading = false;
    private Coroutine reloadCoroutine; // prevent multiple coroutines

    void Start()
    {
        mainCam = Camera.main;
        audioSource = GetComponent<AudioSource>();
        SelectWeapon(currentWeaponIndex);
    }

    void Update()
    {
        HandleWeaponSwitch();
        HandleFireInput();
    }

    void HandleWeaponSwitch()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                if (currentWeaponIndex != i)
                {
                    // Cancel any previous coroutine before starting new one
                    if (reloadCoroutine != null)
                        StopCoroutine(reloadCoroutine);

                    AudioClip clip = (i < weaponReloadSound.Length) ? weaponReloadSound[i] : null;

                    if (clip != null)
                        reloadCoroutine = StartCoroutine(PlayReloadSound(clip, i));
                    else
                        SelectWeapon(i); // switch instantly if no sound
                }
            }
        }
    }

    void HandleFireInput()
    {
        if (isSwitchingOrReloading)
            return;

        if (Input.GetMouseButton(0))
        {
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 target = ray.origin + ray.direction * 1000f;

            if (Physics.Raycast(ray, out RaycastHit hit, 2000f, aimLayerMask, QueryTriggerInteraction.Ignore))
                target = hit.point;

            weapons[currentWeaponIndex].Fire(target);
        }
    }

    IEnumerator PlayReloadSound(AudioClip clip, int targetWeaponIndex)
    {
        isSwitchingOrReloading = true;

        // Mark all buttons unselected and gray out the target one
        if (weaponButtons != null && targetWeaponIndex < weaponButtons.Length)
            weaponButtons[targetWeaponIndex].SetLoading(true);

        audioSource.PlayOneShot(clip);
        yield return new WaitForSeconds(clip.length);

        // Select new weapon after reload delay
        SelectWeapon(targetWeaponIndex);

        // Restore button visuals
        if (weaponButtons != null && targetWeaponIndex < weaponButtons.Length)
            weaponButtons[targetWeaponIndex].SetLoading(false);

        isSwitchingOrReloading = false;
        reloadCoroutine = null;
    }

    public void SelectWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length) return;

        currentWeaponIndex = index;

        if (weaponButtons != null)
        {
            for (int i = 0; i < weaponButtons.Length; i++)
                weaponButtons[i].SetSelected(i == index);
        }
    }
}
