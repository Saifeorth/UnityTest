using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipMovement : MonoBehaviour
{
    [Header("Thrust Settings")]
    public float mainThrust = 20f;
    public float strafeThrust = 10f;
    public float reverseThrust = 10f;
    public float rotationThrust = 50f;

    [Header("Limits")]
    public float maxLinearSpeed = 25f;
    public float maxAngularSpeed = 2f;
    public float rotationDamping = 4f;

    [Header("Thruster Particles")]
    public ParticleSystem forwardThruster;
    public ParticleSystem reverseThruster;
    public ParticleSystem strafeLeftThruster;
    public ParticleSystem strafeRightThruster;
    public ParticleSystem rotateLeftThruster;
    public ParticleSystem rotateRightThruster;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f; // we handle damping manually
    }

    private void FixedUpdate()
    {
        HandleThrust();
        HandleRotation();
        LimitVelocity();
        KeepUpright();
    }

    private void HandleThrust()
    {
        Vector3 force = Vector3.zero;

        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);
        bool q = Input.GetKey(KeyCode.Q);
        bool e = Input.GetKey(KeyCode.E);

        if (w) force += transform.forward * mainThrust;
        if (s) force -= transform.forward * reverseThrust;
        if (q) force -= transform.right * strafeThrust;
        if (e) force += transform.right * strafeThrust;

        rb.AddForce(force, ForceMode.Acceleration);

        // Thruster visuals
        ToggleThruster(forwardThruster, w);
        ToggleThruster(reverseThruster, s);
        ToggleThruster(strafeLeftThruster, e);
        ToggleThruster(strafeRightThruster, q);
    }

    private void HandleRotation()
    {
        float rotationInput = 0f;
        bool a = Input.GetKey(KeyCode.A);
        bool d = Input.GetKey(KeyCode.D);

        if (a) rotationInput = -1f;
        else if (d) rotationInput = 1f;

        if (rotationInput != 0f)
        {
            rb.AddTorque(Vector3.up * rotationInput * rotationThrust, ForceMode.Acceleration);
        }
        else
        {
            // Apply counter torque for smooth stop
            if (Mathf.Abs(rb.angularVelocity.y) > 0.01f)
            {
                float counterTorque = -rb.angularVelocity.y * rotationDamping;
                rb.AddTorque(Vector3.up * counterTorque, ForceMode.Acceleration);
            }
        }

        // Rotation thrusters
        ToggleThruster(rotateLeftThruster, d);
        ToggleThruster(rotateRightThruster, a);
    }

    private void LimitVelocity()
    {
        // Cap linear speed
        if (rb.linearVelocity.magnitude > maxLinearSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxLinearSpeed;

        // Cap angular velocity (Y only)
        Vector3 angularVel = rb.angularVelocity;
        angularVel.y = Mathf.Clamp(angularVel.y, -maxAngularSpeed, maxAngularSpeed);
        rb.angularVelocity = new Vector3(0f, angularVel.y, 0f);
    }

    private void KeepUpright()
    {
        // Lock ship rotation to upright Y-axis only
        Vector3 euler = rb.rotation.eulerAngles;
        rb.rotation = Quaternion.Euler(0f, euler.y, 0f);
    }

    private void ToggleThruster(ParticleSystem ps, bool active)
    {
        if (ps == null) return;
        var emission = ps.emission;
        emission.enabled = active;
    }
}
