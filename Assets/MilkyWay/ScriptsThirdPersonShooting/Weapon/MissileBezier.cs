using UnityEngine;

public class MissileBezier : MonoBehaviour
{
    public Vector3 p0, p1, p2;
    public float speed = 20f; // missile speed
    private float t = 0f;
    private float arcLength = 1f; // approximate

    public void InitCurve(Vector3 start, Vector3 control, Vector3 end, float newSpeed)
    {
        p0 = start;
        p1 = control;
        p2 = end;
        speed = newSpeed;

        t = 0f;

        // Estimate curve length (simple)
        arcLength = (Vector3.Distance(p0, p1) + Vector3.Distance(p1, p2)) * 0.5f;
    }

    void Update()
    {
        if (t >= 1f)
        {
            gameObject.SetActive(false);
            return;
        }

        // Move based on actual distance
        t += (speed / arcLength) * Time.deltaTime;

        Vector3 pos =
            Mathf.Pow(1 - t, 2) * p0 +
            2 * (1 - t) * t * p1 +
            t * t * p2;

        transform.position = pos;

        // Forward facing
        if (t < 0.99f)
        {
            float t2 = Mathf.Min(t + 0.01f, 1f);
            Vector3 next =
                Mathf.Pow(1 - t2, 2) * p0 +
                2 * (1 - t2) * t2 * p1 +
                t2 * t2 * p2;

            transform.rotation = Quaternion.LookRotation(next - pos);
        }
    }
}
