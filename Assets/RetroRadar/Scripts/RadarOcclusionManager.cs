using UnityEngine;
using System.Collections.Generic;

public class RadarOcclusionManager : MonoBehaviour
{
    [Header("Radar Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float radarRange = 50f; // Maximum distance for radar visibility

    private List<RadarAsteroid> asteroids = new();

    void Awake()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        asteroids.AddRange(FindObjectsOfType<RadarAsteroid>());
    }

    void LateUpdate()
    {
        if (asteroids.Count == 0 || player == null) return;

        // 1️⃣ Sort asteroids by distance from player (nearest first)
        asteroids.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));

        Vector3 blockedDir = Vector3.zero;

        foreach (var asteroid in asteroids)
        {
            Vector3 dir = asteroid.DirectionFromPlayer();
            float distance = asteroid.DistanceToPlayer();

            // 2️⃣ Check if inside radar range
            bool inRadar = distance <= radarRange;

            // 3️⃣ Apply occlusion: hidden if behind a closer asteroid
            bool visible = inRadar &&
                           (blockedDir == Vector3.zero || Vector3.Dot(dir, blockedDir) <= 0.98f);

            asteroid.SetVisible(visible);

            // Update blocked direction if this asteroid is visible
            if (visible)
                blockedDir = dir;
        }
    }
}
