using UnityEngine;

public class ShipMovementController : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float speedIncrease = 10f;
    [SerializeField] private float speedDecrease = 15f;
    public float maxForwardSpeed = 100f;
    [SerializeField] private float maxReverseSpeed = -30f;

    [Header("Heat")]
    [SerializeField] private float speedUpHeat = 5f;
    [SerializeField] private float speedDownHeat = 3f;
    [SerializeField] private float heatCooldown = 4f;

    [Header("Turning")]
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField, Range(0f, 1f)] private float turnSpeedReduced = 0.5f;

    [Header("References")]
    [SerializeField] private Transform worldRoot;

    public float CurrentSpeed { get; private set; }
    public float Heat { get; private set; }

    void Update()
    {
        HandleSpeed();
        HandleTurn();
        HandleHeatCooldown();
        MoveWorld();
    }

    void HandleSpeed()
    {
        if (Input.GetKey(KeyCode.W))
        {
            CurrentSpeed += speedIncrease * Time.deltaTime;
            Heat += speedUpHeat * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            CurrentSpeed -= speedDecrease * Time.deltaTime;
            Heat += speedDownHeat * Time.deltaTime;
        }

        CurrentSpeed = Mathf.Clamp(CurrentSpeed, maxReverseSpeed, maxForwardSpeed);
    }

    void HandleTurn()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input = 1f;
        if (Input.GetKey(KeyCode.D)) input = -1f;

        float speedRatio = Mathf.Abs(CurrentSpeed) / Mathf.Max(Mathf.Abs(maxForwardSpeed), Mathf.Abs(maxReverseSpeed));
        float reduction = turnSpeedReduced * speedRatio;
        float finalTurnSpeed = turnSpeed * (1f - reduction);

        worldRoot.Rotate(Vector3.up, input * finalTurnSpeed * Time.deltaTime, Space.World);
    }

    void HandleHeatCooldown()
    {
        Heat = Mathf.Max(0f, Heat - heatCooldown * Time.deltaTime);
    }

    void MoveWorld()
    {
        worldRoot.Translate(Vector3.back * CurrentSpeed * Time.deltaTime, Space.Self);
    }
}
