using UnityEngine;

public class ProjectileAutoDisable : MonoBehaviour
{
    public float lifetime = 5f;

    void OnEnable() => Invoke(nameof(Disable), lifetime);
    void Disable() => gameObject.SetActive(false);
}
