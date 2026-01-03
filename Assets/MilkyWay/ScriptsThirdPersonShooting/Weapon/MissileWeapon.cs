using UnityEngine;

public class MissileWeapon : Weapon
{
    public float zigzagRadius = 2f;
    public float arcForward = 15f;
    public Vector2 arcUpRange = new Vector2(2f, 6f);

    protected override void Awake()
    {
        base.Awake();
        projectileSpeed = 60f;
    }

    public override bool Fire(Vector3 targetPosition)
    {
        if (!weaponSubsystem.weaponManager.IsTargetLocked())
            return false;

        Transform target = weaponSubsystem.weaponManager.GetLockedTarget();
        if (!target) return false;

        return FireAtTarget(target);
    }

    private bool FireAtTarget(Transform target)
    {
        if (!CanFireInternal())
            return false;

        ApplyFireInternal();

        foreach (Transform point in firePoints)
        {
            Quaternion lookRot = Quaternion.LookRotation(target.position - point.position);
            GameObject proj = GetPooledProjectile(point.position, lookRot);

            if (!proj) continue;

            var zigzag = proj.GetComponent<MissileZigZag>();
            if (zigzag)
            {
                Vector3 start = point.position;
                Vector3 control =
                    start +
                    point.forward * arcForward +
                    point.up * Random.Range(arcUpRange.x, arcUpRange.y);

                zigzag.InitCurve(start, control, target.position, projectileSpeed);
            }
        }

        return true;
    }

    // ---------------------------------------------------
    // INTERNAL FIRE LOGIC from base class, but separated
    // ---------------------------------------------------
    private bool CanFireInternal()
    {
        if (isReloading) return false;
        if (Time.time < nextFireTime) return false;

        if (currentClipAmmo <= 0)
        {
            TryReload();
            return false;
        }

        if (cargoAmmo <= 0 && currentClipAmmo <= 0)
            return false;

        return true;
    }

    private void ApplyFireInternal()
    {
        nextFireTime = Time.time + fireRate;
        fireCooldownPercent = 0f;
        currentClipAmmo--;

        PlayRandomFireSound();

        if (currentClipAmmo <= 0)
            TryReload();
    }
}
