using UnityEngine;
using UnityEngine.UI;

public class FloatingHealthBar : MonoBehaviour
{
    public Transform target;                  // Astronaut
    public Camera cam;                        // Player camera
    public Image fillImage;
    public Vector3 offset = new Vector3(0, 2f, 0); // Height above astronaut

    private float maxHealth = 100f;
    private float currentHealth = 100f;

    private void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null || cam == null) return;

        // Follow astronaut
        transform.position = target.position + offset;

        // Look at camera
        transform.LookAt(transform.position + cam.transform.forward);
    }

    public void SetHealth(float newHealth)
    {
        currentHealth = newHealth;
        fillImage.fillAmount = currentHealth / maxHealth;
    }

    public void Initialize(float maxHP)
    {
        maxHealth = maxHP;
        currentHealth = maxHP;
        fillImage.fillAmount = 1f;
    }
}
