using UnityEngine;
using TMPro;

public class ShipHUD : MonoBehaviour
{
    [SerializeField] private ShipMovementController ship;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text heatText;

    void Update()
    {
        if (Mathf.Abs(ship.CurrentSpeed) >= ship.maxForwardSpeed)
            speedText.text = "MAX";
        else
            speedText.text = ship.CurrentSpeed.ToString("0.0");

        heatText.text = ship.Heat.ToString("0.0");
    }
}
