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
    }

    public virtual void Fire(Vector3 targetPosition)
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;

        if (firePoints == null || firePoints.Length == 0 || projectilePrefab == null)
            return;

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

    public void PlayRandomFireSound()
    {
        if (fireClips == null || fireClips.Length == 0 || !audioSource) return;

        // Random clip
        AudioClip clip = fireClips[Random.Range(0, fireClips.Length)];

        audioSource.PlayOneShot(clip, volume);
    }
}
