using UnityEngine;
using System.Globalization;

public class PlayerDebugTuningGUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private KeyCode toggleKey = KeyCode.P;

    private bool showGUI;

    private GUIStyle labelStyle;
    private GUIStyle fieldStyle;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        // Label style
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = 16;
        labelStyle.alignment = TextAnchor.MiddleLeft;

        // Field style
        fieldStyle = new GUIStyle(GUI.skin.textField);
        fieldStyle.fontSize = 16;
        fieldStyle.fixedHeight = 30;
        fieldStyle.alignment = TextAnchor.MiddleCenter;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showGUI = !showGUI;
    }

    private void OnGUI()
    {
        if (!showGUI || player == null)
            return;

        GUI.Box(new Rect(10, 10, 360, 400), "PLAYER TUNING (Runtime)");

        GUILayout.BeginArea(new Rect(20, 40, 340, 360));

        DrawFloatField("Speed Increase (W)", ref player.speedIncrease);
        DrawFloatField("Speed Up Heat (W)", ref player.speedUpHeat);
        DrawFloatField("Max Forward Speed", ref player.maxForwardSpeed);

        GUILayout.Space(10);

        DrawFloatField("Speed Decrease (S)", ref player.speedDecrease);
        DrawFloatField("Speed Down Heat (S)", ref player.speedDownHeat);
        DrawFloatField("Max Reverse Speed", ref player.maxReverseSpeed);

        GUILayout.Space(10);

        DrawFloatField("Turn Speed", ref player.turnSpeed);
        DrawFloatField("Turn Speed Reduced (%)", ref player.turnSpeedReduced);

        GUILayout.EndArea();
    }

    private void DrawFloatField(string label, ref float value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(200));

        string text = value.ToString("F3", CultureInfo.InvariantCulture);
        string newText = GUILayout.TextField(text, fieldStyle, GUILayout.Width(120));

        if (float.TryParse(newText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            value = parsed;
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(4);
    }
}
