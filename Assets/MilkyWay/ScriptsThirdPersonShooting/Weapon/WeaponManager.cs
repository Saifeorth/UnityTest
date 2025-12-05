using UnityEngine;
using System.Collections;

public class WeaponManager : MonoBehaviour
{
    public LayerMask aimLayerMask;
    private Camera mainCam;

    public HeatManager heatManager;
    public ShipMovementThirdPerson playerShip;

    private WeaponSubsystem currentSelected;

    public WeaponSubsystem[] subsystems;


    void Start()
    {
        mainCam = Camera.main;
    }


    void Update()
    {
        CheckNumberInput();
        HandleFiring();
    }

    private void CheckNumberInput()
    {
        for (int i = 0; i < subsystems.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                Select(subsystems[i]);
                break;
            }
        }
    }


    void HandleFiring()
    {
        if (Input.GetMouseButton(1) || playerShip.showGUI) return; 


        if (Input.GetMouseButton(0))
            FireSelectedWeapon();
    }

    void FireSelectedWeapon()
    {
        if (currentSelected ==null)
            return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 target = ray.origin + ray.direction * 1000f;

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, aimLayerMask, QueryTriggerInteraction.Ignore))
            target = hit.point;

        if(currentSelected.funcState == SubsystemFunctionalState.Idle || currentSelected.visualState == SubsystemVisualState.Disabled || currentSelected.isVenting)
            return;

        if (currentSelected.weapon.Fire(target))
        {
            heatManager.AddBurstHeat(currentSelected);
        }

        currentSelected.UpdateDescription();
    }

    public void Select(WeaponSubsystem weaponSubsystem)
    {
        // Prevent double selections
        if (currentSelected == weaponSubsystem)
            return;

        // Deselect previous
        if (currentSelected != null)
            currentSelected.Deselect();

        currentSelected = weaponSubsystem;

        weaponSubsystem.ApplySelectedState();
    }
}
