using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardThrust = 5f;
    public float rotationThrust = 1.2f;
    public float maxLinearSpeed = 8f;
    public float maxAngularSpeed = 1f;

    [Header("Random Steering")]
    public float directionChangeInterval = 10f;
    public float randomAngleRange = 90f;
    private float nextDirectionChangeTime;
    private float targetAngularSpeed;
    private float currentAngular = 0f;
    public float turnSmooth = 2f;

    [Header("Health")]
    public float maxHealth = 50f;
    private float currentHealth;

    [Header("Thruster Particles")]
    public ParticleSystem forwardThruster;
    public ParticleSystem[] rotateLeftThrusters;
    public ParticleSystem[] rotateRightThrusters;

    public FloatingHealthBar healthBar;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        currentHealth = maxHealth;
    }

    private void Start()
    {
        PickNewDirection();

        if (healthBar != null)
            healthBar.Initialize(maxHealth);
    }

    private void FixedUpdate()
    {
        MoveForward();
        RotateTowardsTarget();
        LimitVelocity();

        if (Time.time >= nextDirectionChangeTime)
            PickNewDirection();
    }

    // -----------------------------
    // MOVEMENT
    // -----------------------------

    private void MoveForward()
    {
        rb.AddForce(transform.forward * forwardThrust, ForceMode.Acceleration);

        if (forwardThruster != null)
        {
            var emission = forwardThruster.emission;
            emission.enabled = true;
        }
    }

    private void RotateTowardsTarget()
    {
        currentAngular = Mathf.Lerp(currentAngular, targetAngularSpeed, Time.fixedDeltaTime * turnSmooth);
        rb.angularVelocity = new Vector3(0f, currentAngular, 0f);

        // Thrusters
        if (currentAngular > 0.05f)
        {
            ToggleThrusters(rotateLeftThrusters, false);
            ToggleThrusters(rotateRightThrusters, true);
        }
        else if (currentAngular < -0.05f)
        {
            ToggleThrusters(rotateLeftThrusters, true);
            ToggleThrusters(rotateRightThrusters, false);
        }
        else
        {
            ToggleThrusters(rotateLeftThrusters, false);
            ToggleThrusters(rotateRightThrusters, false);
        }
    }

    private void LimitVelocity()
    {
        if (rb.linearVelocity.magnitude > maxLinearSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxLinearSpeed;

        Vector3 ang = rb.angularVelocity;
        ang.y = Mathf.Clamp(ang.y, -maxAngularSpeed, maxAngularSpeed);
        rb.angularVelocity = new Vector3(0, ang.y, 0);
    }

    // -----------------------------
    // RANDOM STEERING
    // -----------------------------

    private void PickNewDirection()
    {
        nextDirectionChangeTime = Time.time + directionChangeInterval;

        float choice = Random.value; // 0–1

        if (choice < 0.5f)
        {
            // Go straight 50%
            targetAngularSpeed = 0f;
        }
        else if (choice < 0.75f)
        {
            // Turn left 25%
            targetAngularSpeed = -rotationThrust;
        }
        else
        {
            // Turn right 25%
            targetAngularSpeed = rotationThrust;
        }
    }

    // -----------------------------
    // HEALTH
    // -----------------------------

    public void TakeDamage(float value)
    {
        currentHealth -= value;

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (healthBar != null)
            Destroy(healthBar.gameObject);


        Destroy(gameObject);
    }

    // -----------------------------
    // HELPER: THRUSTER TOGGLE
    // -----------------------------

    private void ToggleThrusters(ParticleSystem[] arr, bool active)
    {
        if (arr == null) return;

        foreach (var ps in arr)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            emission.enabled = active;
        }
    }
}
