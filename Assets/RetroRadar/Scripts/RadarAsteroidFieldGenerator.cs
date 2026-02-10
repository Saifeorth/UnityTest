using UnityEngine;

public class RadarAsteroidFieldGenerator : MonoBehaviour
{
    [Header("Asteroid Prefabs (All Types)")]
    [SerializeField] private GameObject[] asteroidPrefabs;

    [Header("Asteroid Count")]
    [SerializeField] private Vector2Int asteroidDensityRange = new Vector2Int(15, 30);

    [Header("Asteroid Size")]
    [SerializeField] private Vector2 asteroidSizeRange = new Vector2(0.8f, 2.5f);

    [Header("Rotation (Deg / Sec)")]
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(10f, 60f);

    [Header("Movement Direction (Degrees)")]
    [SerializeField] private Vector2 movementDirectionRange = new Vector2(0f, 360f);

    [Header("Movement Speed")]
    [SerializeField] private Vector2 movementSpeedRange = new Vector2(2f, 8f);

    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 200f;

    private Transform asteroidParent;

    private void Awake()
    {
        asteroidParent = new GameObject("RadarAsteroids").transform;
        asteroidParent.SetParent(transform);
    }

    private void Start()
    {
        SpawnField();
    }

    private void SpawnField()
    {
        if (asteroidPrefabs == null || asteroidPrefabs.Length == 0)
        {
            Debug.LogError("RadarAsteroidFieldGenerator: No asteroid prefabs assigned.");
            return;
        }

        int count = Random.Range(asteroidDensityRange.x, asteroidDensityRange.y + 1);

        for (int i = 0; i < count; i++)
        {
            SpawnSingleAsteroid();
        }
    }

    private void SpawnSingleAsteroid()
    {
        GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];

        Vector3 position = Random.insideUnitSphere * spawnRadius;
        position.y = 0f;

        GameObject asteroid = Instantiate(prefab, position, Quaternion.identity, asteroidParent);

        float size = Random.Range(asteroidSizeRange.x, asteroidSizeRange.y);
        asteroid.transform.localScale = Vector3.one * size;

        if (!asteroid.TryGetComponent(out RadarAsteroid radarAsteroid))
            asteroid.AddComponent<RadarAsteroid>();

        if (!asteroid.TryGetComponent(out AsteroidMotion asteroidMotion))
            asteroidMotion = asteroid.AddComponent<AsteroidMotion>();

        float rotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        float directionDeg = Random.Range(movementDirectionRange.x, movementDirectionRange.y);
        float moveSpeed = Random.Range(movementSpeedRange.x, movementSpeedRange.y);

        asteroidMotion.Init(rotationSpeed, directionDeg, moveSpeed);
    }
}
