using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new Vector3(0f, 17f, -17f);
    [SerializeField] private float followSmooth = 8f;

    void LateUpdate()
    {
        if (!player) return;

        Vector3 targetPosition = player.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSmooth * Time.deltaTime
        );
    }
}
