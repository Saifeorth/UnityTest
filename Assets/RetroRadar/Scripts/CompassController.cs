using UnityEngine;

public class CompassController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform worldRoot;
    [SerializeField] private Transform textRoot;

    private Transform[] textChildren;
    private float lastWorldY;

    private void Awake()
    {
        CacheChildren();
    }

    private void OnValidate()
    {
        CacheChildren();
    }

    void LateUpdate()
    {
        if (!worldRoot || textChildren == null) return;

        float worldY = worldRoot.eulerAngles.y;

        // Skip if rotation hasn't changed
        if (Mathf.Approximately(worldY, lastWorldY))
            return;

        lastWorldY = worldY;

        // Rotate ring (positions)
        textRoot.localRotation = Quaternion.Euler(0f, 0f, -worldY);

        // Counter-rotate children (orientation)
        Quaternion childRotation = Quaternion.Euler(0f, 0f, worldY);
        for (int i = 0; i < textChildren.Length; i++)
        {
            textChildren[i].localRotation = childRotation;
        }
    }

    private void CacheChildren()
    {
        if (!textRoot)
        {
            textChildren = null;
            return;
        }

        int count = textRoot.childCount;
        textChildren = new Transform[count];

        for (int i = 0; i < count; i++)
            textChildren[i] = textRoot.GetChild(i);
    }
}
