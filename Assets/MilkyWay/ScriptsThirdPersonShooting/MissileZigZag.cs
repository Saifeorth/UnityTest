using UnityEngine;

public class MissileZigZag : MonoBehaviour
{
    public float speed = 20f;
    public float zigZagAmplitude = 2f;
    public float zigZagFrequency = 3f;

    private Vector3 startPoint;
    private Vector3 controlPoint;
    private Vector3 endPoint;

    private float t = 0f;
    private float curveLengthApprox;

    private Vector3 zigZagDirection;

    public void InitCurve(Vector3 start, Vector3 control, Vector3 end, float missileSpeed)
    {
        startPoint = start;
        controlPoint = control;
        endPoint = end;
        speed = missileSpeed;

        t = 0f;

        // Approximate curve length
        curveLengthApprox = (Vector3.Distance(start, control) + Vector3.Distance(control, end)) * 0.5f;

        // Random zigzag offset direction
        zigZagDirection = Random.insideUnitSphere;
        zigZagDirection.y = Mathf.Abs(zigZagDirection.y);
        zigZagDirection.Normalize();

        // FIX: Force correct initial rotation
        Vector3 initialDir = (controlPoint - startPoint).normalized;
        transform.rotation = Quaternion.LookRotation(initialDir, Vector3.up);
    }


    private void Update()
    {
        if (t >= 1f)
        {
            gameObject.SetActive(false);
            return;
        }

        t += (speed / curveLengthApprox) * Time.deltaTime;

        // Bezier main position
        Vector3 basePos =
            Mathf.Pow(1 - t, 2) * startPoint +
            2 * (1 - t) * t * controlPoint +
            t * t * endPoint;

        // Zig-zag offset
        float zigzag = Mathf.Sin(Time.time * zigZagFrequency) * zigZagAmplitude;

        Vector3 finalPos = basePos + zigZagDirection * zigzag;

        transform.position = finalPos;

        // Face the next position for smooth rotation
        if (t < 0.99f)
        {
            float t2 = Mathf.Min(t + 0.02f, 1f);
            Vector3 nextPos =
                Mathf.Pow(1 - t2, 2) * startPoint +
                2 * (1 - t2) * t2 * controlPoint +
                t2 * t2 * endPoint;

            transform.rotation = Quaternion.LookRotation(nextPos - finalPos);
        }
    }
}
