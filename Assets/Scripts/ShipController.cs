using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed at which the ship moves toward waypoints (units/sec).")]
    [SerializeField] private float moveSpeed = 10f;

    [Tooltip("Rotation speed in degrees/sec.")]
    [SerializeField] private float rotationSpeed = 180f;

    [Tooltip("Distance from waypoint considered as 'arrived'.")]
    [SerializeField] private float stopDistance = 0.5f;

    [Tooltip("Rotation threshold in degrees for final alignment.")]
    [SerializeField] private float rotationThreshold = 1f;

    [Tooltip("Stopping distance when chasing a locked target.")]
    [SerializeField] private float targetLockStopDistance = 15f;

    private Rigidbody rb;
    private Queue<WaypointData> waypoints = new Queue<WaypointData>();
    private Coroutine followCoroutine;

    public bool IsMoving => followCoroutine != null;
    public event System.Action OnWaypointsUpdated;

    private WaypointData currentWaypoint = new WaypointData(Vector3.zero, Quaternion.identity);
    public WaypointData CurrentWaypoint => currentWaypoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // ================================
    // Public API
    // ================================
    public void SetWaypoints(List<WaypointData> newWaypoints)
    {
        if (newWaypoints == null || newWaypoints.Count == 0) return;

        waypoints.Clear();
        foreach (var w in newWaypoints)
            waypoints.Enqueue(w);

        OnWaypointsUpdated?.Invoke();
        RestartFollowCoroutine();
    }

    public void EnqueueWaypoint(WaypointData wp)
    {
        waypoints.Enqueue(wp);
        OnWaypointsUpdated?.Invoke();

        if (!IsMoving)
            RestartFollowCoroutine();
    }

    public void ClearWaypoints()
    {
        waypoints.Clear();
        StopFollowCoroutine();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        OnWaypointsUpdated?.Invoke();
        currentWaypoint = new WaypointData(Vector3.zero, Quaternion.identity);
    }

    public List<WaypointData> GetWaypoints() => new List<WaypointData>(waypoints);

    // ================================
    // Movement Core
    // ================================
    private void RestartFollowCoroutine()
    {
        StopFollowCoroutine();
        followCoroutine = StartCoroutine(FollowPath());
    }

    private void StopFollowCoroutine()
    {
        if (followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
    }

    private IEnumerator FollowPath()
    {
        while (waypoints.Count > 0)
        {
            currentWaypoint = waypoints.Peek(); // don't dequeue yet
            OnWaypointsUpdated?.Invoke();

            if (!currentWaypoint.IsValid())
            {
                waypoints.Dequeue();
                continue;
            }

            if (currentWaypoint.isTargetLock)
            {
                // Chase locked target indefinitely until overridden
                yield return FollowTargetLock(currentWaypoint);
                yield break;
            }
            else
            {
                yield return MoveToWaypoint(currentWaypoint);
                waypoints.Dequeue();
            }
        }

        currentWaypoint = new WaypointData(Vector3.zero, Quaternion.identity);
        followCoroutine = null;
    }

    private IEnumerator FollowTargetLock(WaypointData lockWaypoint)
    {
        while (true)
        {
            // Exit if target destroyed or player assigns a new waypoint
            if (!lockWaypoint.IsValid() || waypoints.Count == 0 || !waypoints.Peek().Equals(lockWaypoint))
                break;

            Vector3 targetPos = lockWaypoint.GetCurrentPosition();
            Vector3 toTarget = targetPos - transform.position;
            float distance = toTarget.magnitude;

            // Move toward target if beyond stopping distance
            if (distance > targetLockStopDistance)
                rb.MovePosition(rb.position + toTarget.normalized * moveSpeed * Time.fixedDeltaTime);

            // Face the target if exists
            if (lockWaypoint.target != null)
            {
                Vector3 toEnemy = (lockWaypoint.target.transform.position - transform.position);
                if (toEnemy.sqrMagnitude > 0.001f)
                {
                    Quaternion desired = Quaternion.LookRotation(toEnemy.normalized, Vector3.up);
                    rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, desired, rotationSpeed * Time.fixedDeltaTime));
                }
            }

            yield return new WaitForFixedUpdate();
        }

        currentWaypoint = new WaypointData(Vector3.zero, Quaternion.identity);
        followCoroutine = null;
    }

    private IEnumerator MoveToWaypoint(WaypointData target)
    {
        float stoppingDist = target.isTargetLock ? targetLockStopDistance : stopDistance;

        while (true)
        {
            if (!target.IsValid()) yield break;

            Vector3 targetPos = target.GetCurrentPosition();
            Vector3 toTarget = targetPos - transform.position;
            float distance = toTarget.magnitude;

            if (distance <= stoppingDist) break;

            rb.MovePosition(rb.position + toTarget.normalized * Mathf.Min(moveSpeed * Time.fixedDeltaTime, distance));

            Quaternion desired;
            if (target.isTargetLock && target.target != null)
            {
                Vector3 toEnemy = (target.target.transform.position - transform.position);
                desired = toEnemy.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(toEnemy.normalized, Vector3.up)
                    : rb.rotation;
            }
            else
            {
                float yaw = target.rotation.eulerAngles.y;
                desired = Quaternion.Euler(0f, yaw, 0f);
            }

            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, desired, rotationSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }

        // Final alignment
        if (target.isTargetLock && target.target != null)
        {
            while (target.IsValid() &&
                   Quaternion.Angle(rb.rotation, Quaternion.LookRotation((target.target.transform.position - transform.position).normalized, Vector3.up)) > rotationThreshold)
            {
                Quaternion lookAtEnemy = Quaternion.LookRotation((target.target.transform.position - transform.position).normalized, Vector3.up);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, lookAtEnemy, rotationSpeed * Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
            }
        }
        else
        {
            while (Quaternion.Angle(rb.rotation, target.rotation) > rotationThreshold)
            {
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target.rotation, rotationSpeed * Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
