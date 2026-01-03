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

    public float targetLockTime = 2f;
    public float targetMaxDistance = 1500f;
    public float targetBreakDistance = 1600f;
    public float targetAimRadius = 0.03f; // screen-space radius

    private Transform currentTarget;
    private float lockTimer;
    private bool isLocking;

    public enum TargetLockState
    {
        None,       // no target / idle
        Acquiring, // T pressed, locking in progress
        Locked     // lock complete
    }

    public TargetLockState CurrentLockState { get; private set; }


    void Start()
    {
        mainCam = Camera.main;

        // Optionally set default selection to index 0:
        // if (subsystems != null && subsystems.Length > 0) Select(subsystems[0]);
    }

    void Update()
    {
        CheckNumberInput();
        HandleTargeting();
        HandleLockedTargetValidation();
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

    public void Select(WeaponSubsystem weaponSubsystem)
    {
        // Prevent re-selecting same
        if (currentSelected == weaponSubsystem)
            return;

        // Deselect previous
        if (currentSelected != null)
            currentSelected.Deselect();

        currentSelected = weaponSubsystem;
        currentSelected.ApplySelectedState();

        // Update description on selection
        currentSelected.UpdateDescription();
    }

    void HandleFiring()
    {
        if (Input.GetMouseButton(1) || playerShip.showGUI) return;

        if (Input.GetMouseButton(0))
            FireSelectedWeapon();
    }

    void HandleLockedTargetValidation()
    {
        if (CurrentLockState == TargetLockState.Locked)
        {
            if (!IsTargetStillValid())
            {
                CancelTargetLock();
            }
        }
    }

    private Transform DetectTargetUnderCursor()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (Physics.Raycast(ray, out RaycastHit hit, targetMaxDistance, aimLayerMask))
        {
            var targetable = hit.collider.GetComponentInParent<Targetable>();
            if (targetable)
                return targetable.transform;
        }

        return null;
    }

    void HandleTargeting()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Transform detected = DetectTargetUnderCursor();

            if (detected != null)
            {
                BeginTargetLock(detected);
            }
        }

        if (isLocking)
        {
            UpdateTargetLock();
        }
    }


    void BeginTargetLock(Transform target)
    {
        float distance = Vector3.Distance(
            playerShip.transform.position,
            target.position
        );

        if (distance > targetMaxDistance)
            return;

        if (currentTarget == target && IsTargetLocked())
            return;

        currentTarget = target;
        lockTimer = 0f;
        isLocking = true;
        CurrentLockState = TargetLockState.Acquiring;
    }


    bool IsTargetStillValid()
    {
        if (!currentTarget)
            return false;

        // ---- Distance check (always valid) ----
        float distance = Vector3.Distance(
            playerShip.transform.position,
            currentTarget.position
        );

        if (distance > targetBreakDistance)
            return false;

        // ---- Screen-space check ONLY while acquiring ----
        if (CurrentLockState == TargetLockState.Acquiring)
        {
            Vector3 screenPos = mainCam.WorldToViewportPoint(currentTarget.position);

            if (screenPos.z <= 0)
                return false;

            if (Mathf.Abs(screenPos.x - 0.5f) > targetAimRadius)
                return false;

            if (Mathf.Abs(screenPos.y - 0.5f) > targetAimRadius)
                return false;
        }

        return true;
    }


    public bool IsTargetLocked()
    {
        return currentTarget != null && lockTimer >= targetLockTime;
    }

    public Transform GetLockedTarget()
    {
        return IsTargetLocked() ? currentTarget : null;
    }

    public void CancelTargetLock()
    {
        currentTarget = null;
        lockTimer = 0f;
        isLocking = false;
        CurrentLockState = TargetLockState.None;
    }

    void UpdateTargetLock()
    {
        // Cancel if target lost
        if (!IsTargetStillValid())
        {
            CancelTargetLock();
            return;
        }

        lockTimer += Time.deltaTime;

        if (lockTimer >= targetLockTime)
        {
            lockTimer = targetLockTime;
            isLocking = false;
            CurrentLockState = TargetLockState.Locked;
            // Target locked
        }
    }

    void FireSelectedWeapon()
    {
        if (currentSelected == null)
            return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 target = ray.origin + ray.direction * 1000f;

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, aimLayerMask, QueryTriggerInteraction.Ignore))
            target = hit.point;

        if (currentSelected.funcState == SubsystemFunctionalState.Idle || currentSelected.visualState == SubsystemVisualState.Disabled || currentSelected.isVenting)
            return;

        if (currentSelected.weapon.Fire(target))
        {
            heatManager.AddBurstHeat(currentSelected);
        }

        currentSelected.UpdateDescription();
    }

    public bool CanLockTarget()
    {
        if (CurrentLockState != TargetLockState.None)
            return false;

        Transform detected = DetectTargetUnderCursor();
        if (!detected)
            return false;

        float distance = Vector3.Distance(
            playerShip.transform.position,
            detected.position
        );

        return distance <= targetMaxDistance;
    }
}
