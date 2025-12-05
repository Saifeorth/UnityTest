using UnityEngine;

public class ProjectileAutoDisable : MonoBehaviour
{
    public float lifetime = 5f;
    public float damage = 10f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if hit AI ship by layer
        if (other.gameObject.layer == LayerMask.NameToLayer("AIShip"))
        {
            AIShipController ai = other.GetComponent<AIShipController>();
            if (ai != null)
            {
                ai.TakeDamage(damage);
            }

            Disable();
        }
    }

    void OnEnable() => Invoke(nameof(Disable), lifetime);
    void Disable() => gameObject.SetActive(false);
}
