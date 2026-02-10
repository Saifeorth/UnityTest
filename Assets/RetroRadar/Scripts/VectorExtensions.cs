using UnityEngine;

public static class VectorExtensions
{
    public static Vector3 WithY(this Vector3 v, float y)
    {
        v.y = y;
        return v;
    }
}
