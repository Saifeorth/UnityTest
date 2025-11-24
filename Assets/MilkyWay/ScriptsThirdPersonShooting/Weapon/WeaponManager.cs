using UnityEngine;
using System.Collections;

public class WeaponManager : MonoBehaviour
{
    public LayerMask aimLayerMask;
    private Camera mainCam;

    public WeaponSubsystem[] weaponSubsystems;
    public SubsystemController weaponSubsystemController;
    public HeatManager heatManager;
    public ShipMovementThirdPerson playerShip;

    public int totalEnergyFromSubsystem = 0;
    public int totalAllocatedToWeapons = 0;

    private void OnEnable()
    {
        weaponSubsystemController.onEnergyChanged += SetTotalEnergy;
    }

    private void OnDestroy()
    {
        weaponSubsystemController.onEnergyChanged -= SetTotalEnergy;
    }

    void Start()
    {
        mainCam = Camera.main;

        // Initialize UI
        foreach (var ws in weaponSubsystems)
            ws.InitializeUI();
    }

    public void SetTotalEnergy(int newTotalEnergy)
    {
        totalEnergyFromSubsystem = newTotalEnergy;
        ClampWeaponAllocations();
    }

    private void ClampWeaponAllocations()
    {
        // Ensure weapon allocations never exceed available energy
        int sum = 0;
        foreach (var ws in weaponSubsystems)
            sum += ws.currentAllocated;

        totalAllocatedToWeapons = sum;

        while (totalAllocatedToWeapons > totalEnergyFromSubsystem)
        {
            // remove from last enabled weapon
            for (int i = weaponSubsystems.Length - 1; i >= 0; i--)
            {
                if (weaponSubsystems[i].currentAllocated > 0)
                {
                    weaponSubsystems[i].currentAllocated--;
                    weaponSubsystems[i].UpdateBars();
                    weaponSubsystems[i].UpdateSubsystemState();
                    totalAllocatedToWeapons--;
                    break;
                }
            }
        }
    }

    void Update()
    {
        HandleFiring();
    }

    void HandleFiring()
    {
        if (Input.GetKey(KeyCode.Space) || playerShip.showGUI) return; // Disable firing when space is held

        if (Input.GetMouseButton(0))
            FireAllEnabledWeapons();
    }

    void FireAllEnabledWeapons()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 target = ray.origin + ray.direction * 1000f;

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, aimLayerMask, QueryTriggerInteraction.Ignore))
            target = hit.point;

        foreach (var ws in weaponSubsystems)
        {
            if (ws.isEnabled)  // fully powered
            {
                ws.weapon.Fire(target);
                heatManager.AddBurstHeat(weaponSubsystemController);
            }
        }
    }
    public bool TryAllocateEnergy(WeaponSubsystem target)
    {
        if (totalAllocatedToWeapons >= totalEnergyFromSubsystem)
            return false;

        if (target.currentAllocated >= target.weapon.requiredEnergy)
            return false;

        target.currentAllocated++;
        target.UpdateBars();
        target.UpdateSubsystemState();

        totalAllocatedToWeapons++;
        return true;
    }

    public bool TryDeallocateEnergy(WeaponSubsystem target)
    {
        if (target.currentAllocated <= 0)
            return false;

        target.currentAllocated--;
        target.UpdateBars();
        target.UpdateSubsystemState();

        totalAllocatedToWeapons--;
        return true;
    }
}
