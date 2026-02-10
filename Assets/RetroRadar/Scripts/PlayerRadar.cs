using UnityEngine;

public class PlayerRadar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;       // Player transform
    [SerializeField] private Transform radarRoot;    // Empty parent for radar sprites
    [SerializeField] private Vector3 radarOffset = new Vector3(0f, 0f, 0f);

    void LateUpdate()
    {
        if (player == null || radarRoot == null) return;

        // Move radar with player
        radarRoot.position = player.position + radarOffset;

        // Rotate radar to match player rotation (Y-axis only)
        radarRoot.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
    }
}
