using UnityEngine;

public class LaserWeapon : Weapon
{
    private void Start()
    {
        projectileSpeed = 150f; // override default if needed
    }

    public override void Fire(Vector3 targetPosition)
    {
        // Optionally: add sound, flash, or laser visual effects here
        base.Fire(targetPosition);
    }
}
