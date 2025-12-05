using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public abstract class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public string weaponName;
    public float fireRate = 0.5f;
    public float projectileSpeed = 100f;
    public Vector3 projectileRotationOffset;
    [HideInInspector] public float fireCooldownPercent = 0f;

    [Header("Ammo Settings")]
    public int cargoAmmo = 200;        // total ammo in storage
    public int clipSize = 20;          // bullets per reload
    public float reloadTime = 2f;      // seconds
    public int currentClipAmmo;        // runtime
    public bool isReloading = false;
    private Coroutine reloadRoutine;

    [Header("Energy Settings")]
    public int requiredEnergy = 1;

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform[] firePoints; // ← multiple barrels supported

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip[] fireClips; // ← assign multiple in Inspector
    public float volume = 1f;

    [Header("Pooling Settings")]
    public int poolSize = 30;
    public bool autoExpandPool = true;

    protected float nextFireTime;
    private Queue<GameObject> projectilePool;

    public WeaponSubsystem weaponSubsystem;

    protected virtual void Awake()
    {
        // Initialize pool
        projectilePool = new Queue<GameObject>();

        if (projectilePrefab)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject proj = Instantiate(projectilePrefab);
                proj.SetActive(false);
                projectilePool.Enqueue(proj);
            }
        }

        currentClipAmmo = Mathf.Min(clipSize, cargoAmmo);
    }

    protected virtual void Update()
    {
        UpdateFireCooldown();
    }

    private void UpdateFireCooldown()
    {
        if (Time.time >= nextFireTime)
        {
            fireCooldownPercent = 1f;     // fully ready
            return;
        }

        float timeLeft = nextFireTime - Time.time;
        fireCooldownPercent = 1f - (timeLeft / fireRate);
    }

    public virtual bool Fire(Vector3 targetPosition)
    {
        if (isReloading) return false;
        if (Time.time < nextFireTime) return false;

        // Clip empty → reload automatically
        if (currentClipAmmo <= 0)
        {
            TryReload();
            return false;
        }

        // No cargo left = no ammo left at all
        if (cargoAmmo <= 0 && currentClipAmmo <= 0)
            return false;


        nextFireTime = Time.time + fireRate;
        fireCooldownPercent = 0f;

        currentClipAmmo--;


        if (firePoints == null || firePoints.Length == 0 || projectilePrefab == null)
            return false;

        // 🔊 Play sound once per shot
        PlayRandomFireSound();

        foreach (Transform point in firePoints)
        {
            Vector3 dir = (targetPosition - point.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(projectileRotationOffset);

            GameObject proj = GetPooledProjectile(point.position, rot);
            if (!proj) continue;

            Rigidbody rb = proj.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = dir * projectileSpeed;
            }
        }

        // After firing → if clip is empty, auto reload
        if (currentClipAmmo <= 0)
            TryReload();


        return true;
    }

    public GameObject GetPooledProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject proj = null;

        // Find inactive projectile in pool
        if (projectilePool.Count > 0 && !projectilePool.Peek().activeSelf)
        {
            proj = projectilePool.Dequeue();
            projectilePool.Enqueue(proj); // cycle it back
        }
        else if (autoExpandPool)
        {
            proj = Instantiate(projectilePrefab);
            projectilePool.Enqueue(proj);
        }

        if (proj)
        {
            proj.transform.SetPositionAndRotation(position, rotation);
            proj.SetActive(true);
        }

        return proj;
    }

    public bool TryReload()
    {
        if (isReloading) return false;
        if (cargoAmmo <= 0) return false;

        reloadRoutine = StartCoroutine(ReloadRoutine());
        return true;
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        yield return new WaitForSeconds(reloadTime);

        int ammoNeeded = clipSize - currentClipAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, cargoAmmo);

        cargoAmmo -= ammoToLoad;
        currentClipAmmo += ammoToLoad;

        isReloading = false;
        weaponSubsystem.UpdateDescription();
    }

    public void PlayRandomFireSound()
    {
        if (fireClips == null || fireClips.Length == 0 || !audioSource) return;

        // Random clip
        AudioClip clip = fireClips[Random.Range(0, fireClips.Length)];

        audioSource.PlayOneShot(clip, volume);
    }
}
