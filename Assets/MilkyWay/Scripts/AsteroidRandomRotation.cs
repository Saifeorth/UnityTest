using UnityEngine;

public class AsteroidRandomRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Minimum rotation speed (degrees per second) per axis.")]
    public float minRotationSpeed = 5f;

    [Tooltip("Maximum rotation speed (degrees per second) per axis.")]
    public float maxRotationSpeed = 30f;

    [Tooltip("If true, rotation speed and direction are randomized on Start.")]
    public bool randomizeOnStart = true;

    private Vector3 rotationSpeed;

    private void Start()
    {
        if (randomizeOnStart)
        {
            rotationSpeed = new Vector3(
                Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value > 0.5f ? 1f : -1f),
                Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value > 0.5f ? 1f : -1f),
                Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value > 0.5f ? 1f : -1f)
            );
        }
    }

    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }

    /// <summary>
    /// Optionally call this method to re-randomize rotation mid-game.
    /// </summary>
    public void RandomizeRotation()
    {
        rotationSpeed = new Vector3(
            Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value > 0.5f ? 1f : -1f),
            Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value > 0.5f ? 1f : -1f),
            Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value > 0.5f ? 1f : -1f)
        );
    }
}
