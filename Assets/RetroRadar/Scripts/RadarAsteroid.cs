using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RadarAsteroid : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Transform player;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        player = GameObject.FindWithTag("Player").transform;
    }

    public void SetVisible(bool visible)
    {
        sprite.enabled = visible;
    }

    public float DistanceToPlayer()
    {
        return Vector3.Distance(player.position, transform.position);
    }

    public Vector3 DirectionFromPlayer()
    {
        return (transform.position - player.position).normalized;
    }
}
