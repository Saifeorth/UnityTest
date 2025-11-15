using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    public GameObject[] asteroidPrefabs;
    public int asteroidCount = 200;
    public float spawnRadius = 200f;
    public float yVariation = 10f;
    public Vector2 asteroidScaleRange = new Vector2(1f, 4f);

    void Start()
    {
        for (int i = 0; i < asteroidCount; i++)
        {
            Vector3 pos = Random.insideUnitSphere * spawnRadius;
            pos.y = Random.Range(-yVariation, yVariation); // small Y variation
            GameObject asteroid = Instantiate(asteroidPrefabs[Random.Range(0,asteroidPrefabs.Length)], pos, Random.rotation);
            float scale = Random.Range(asteroidScaleRange.x, asteroidScaleRange.y);
            asteroid.transform.localScale = Vector3.one * scale;
        }
    }
}
