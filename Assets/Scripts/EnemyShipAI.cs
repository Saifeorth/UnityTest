using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ShipController))]
public class EnemyShipAI : MonoBehaviour
{
    [Header("Roaming Settings")]
    public float roamRadius = 30f;
    public float waitTimeBetweenMoves = 2f;
    public LayerMask groundLayer;

    [Header("Rotation Settings")]
    public bool randomizeRotation = true;
    public Vector2 rotationRangeY = new Vector2(0f, 360f);

    [Header("Debug")]
    public bool showDebugGizmos = true;

    private ShipController ship;
    private Vector3 originPosition;
    private bool isRoaming = true;

    void Start()
    {
        ship = GetComponent<ShipController>();
        originPosition = transform.position;

        // Start roaming behavior
        StartCoroutine(RoamRoutine());
    }

    IEnumerator RoamRoutine()
    {
        while (isRoaming)
        {
            // Pick a new random target on ground
            Vector3 randomPoint = GetRandomPointOnGround();

            if (randomPoint != Vector3.zero)
            {
                // Random Y rotation
                Quaternion randomRot = Quaternion.identity;
                if (randomizeRotation)
                {
                    float yaw = Random.Range(rotationRangeY.x, rotationRangeY.y);
                    randomRot = Quaternion.Euler(0f, yaw, 0f);
                }

                WaypointData wp = new WaypointData(randomPoint, randomRot);
                ship.SetWaypoints(new System.Collections.Generic.List<WaypointData> { wp });
            }

            // Wait until the ship reaches it
            yield return new WaitUntil(() => !ship.IsMoving);

            // Pause for a moment before next move
            yield return new WaitForSeconds(waitTimeBetweenMoves);
        }
    }

    private Vector3 GetRandomPointOnGround()
    {
        Vector3 randomOffset = Random.insideUnitSphere * roamRadius;
        randomOffset.y = 0f;
        Vector3 targetPos = originPosition + randomOffset;

        // Raycast down to find ground level
        if (Physics.Raycast(targetPos + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, groundLayer))
        {
            return hit.point;
        }

        return Vector3.zero;
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(originPosition == Vector3.zero ? transform.position : originPosition, roamRadius);
    }
}
