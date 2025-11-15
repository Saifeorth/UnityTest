using UnityEngine;

public class MissileHoming : MonoBehaviour
{
    public float turnSpeed = 5f;
    public bool HasTarget { get; private set; }

    private Vector3 targetPos;
    private Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    public void SetTarget(Vector3 pos, float tracking)
    {
        targetPos = pos;
        turnSpeed = tracking;
        HasTarget = true;
    }

    void FixedUpdate()
    {
        if (!HasTarget) return;

        Vector3 dir = (targetPos - transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
        rb.linearVelocity = transform.forward * rb.linearVelocity.magnitude;
    }
}
