using UnityEngine;

public class LaserWeapon : Weapon
{
    private void Start()
    {
        projectileSpeed = 150f; // override default if needed
    }

    public override bool Fire(Vector3 targetPosition)
    {
        // Optionally: add sound, flash, or laser visual effects here
        return base.Fire(targetPosition);
    }
}
