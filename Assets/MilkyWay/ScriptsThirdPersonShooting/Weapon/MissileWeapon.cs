using UnityEngine;

public class MissileWeapon : Weapon
{
    public float trackingStrength = 5f;

    private void Start()
    {
        projectileSpeed = 60f; // slower than lasers
    }

    public override void Fire(Vector3 targetPosition)
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;

        if (firePoints == null || firePoints.Length == 0 || projectilePrefab == null)
            return;

        // Play sound once per shot
        base.PlayRandomFireSound();

        foreach (Transform point in firePoints)
        {
            Vector3 dir = (targetPosition - point.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(projectileRotationOffset);

            GameObject proj = GetPooledProjectile(point.position, rot);
            if (!proj) continue;

            //Rigidbody rb = proj.GetComponent<Rigidbody>();
            //if (rb)
            //{
            //    rb.linearVelocity = dir * projectileSpeed;
            //}
        }

        foreach (Transform firePoint in firePoints)
        {
            Collider[] nearby = Physics.OverlapSphere(firePoint.position, 2f);

            foreach (var col in nearby)
            {
                var zigzag = col.GetComponent<MissileZigZag>();
                if (zigzag != null)
                {
                    Vector3 start = firePoint.position;

                    // control point forward & upward for arc
                    Vector3 control = start +
                                      firePoint.forward * 15f +
                                      firePoint.up * Random.Range(2f, 6f);

                    Vector3 end = targetPosition;

                    zigzag.InitCurve(start, control, end, projectileSpeed);
                }
            }
        }
    }

}
