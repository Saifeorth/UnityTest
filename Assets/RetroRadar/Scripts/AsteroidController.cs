using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class AsteroidController : MonoBehaviour
{
    // ---------------------------------------------------------------------
    // Spawn Settings
    // ---------------------------------------------------------------------
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] asteroidPrefabs;
    [SerializeField] private Vector2Int asteroidCount = new Vector2Int(15, 25);
    [SerializeField] private float spawnRadius = 40f;

    // ---------------------------------------------------------------------
    // Asteroid Properties
    // ---------------------------------------------------------------------
    [Header("Asteroid Properties")]
    [SerializeField] private Vector2 sizeRange = new Vector2(0.6f, 2f);
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(10f, 40f);
    [SerializeField] private Vector2 movementSpeedRange = new Vector2(0.5f, 2f);
    [SerializeField] private Vector2 movementDirectionRange = new Vector2(0f, 360f);

    // ---------------------------------------------------------------------
    // Radar
    // ---------------------------------------------------------------------
    [Header("Radar")]
    [SerializeField] private float radarRadius = 21.5f;
    [SerializeField] private float radarSpawnBias = 0.7f;

    // ---------------------------------------------------------------------
    // Occlusion
    // ---------------------------------------------------------------------
    [Header("Occlusion")]
    [SerializeField] private LayerMask asteroidLayer;
    [SerializeField] private float occlusionCheckRadius = 0.5f;



    // ---------------------------------------------------------------------
    // Runtime Data
    // ---------------------------------------------------------------------
    private readonly List<AsteroidData> asteroids = new();
    private Transform player;

    // ---------------------------------------------------------------------
    // Debug GUI
    // ---------------------------------------------------------------------
    [SerializeField] private KeyCode toggleKey = KeyCode.P;
    private bool showGUI;
    private bool stylesInitialized;
    private GUIStyle labelStyle;
    private GUIStyle fieldStyle;
    private GUIStyle headerStyle;

    // Cached tuning values
    private int debugCountMin;
    private int debugCountMax;

    private float debugSizeMin;
    private float debugSizeMax;

    private float debugRotationMin;
    private float debugRotationMax;

    private float debugSpeedMin;
    private float debugSpeedMax;

    private Vector3 lastPlayerPos;
    private float lastWorldY;

    private Vector2 scrollPos;

    // ---------------------------------------------------------------------
    // Internal Data Structure
    // ---------------------------------------------------------------------
    private class AsteroidData
    {
        public Transform transform;
        public Transform visualChild;
        public SpriteRenderer sprite;
        public AsteroidMovement movement;
        public float size;
        public float directionDeg;
    }

    // ---------------------------------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------------------------------
    private void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;

        SyncDebugValues();     // MUST come first
        ValidateRanges();      // Safety
        SpawnAsteroids();      // Now values are valid

        lastPlayerPos = player.position;
        lastWorldY = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showGUI = !showGUI;

        UpdateRadarVisibilityAndOcclusion();
        HandleWorldWrapping();
    }

    private void LateUpdate()
    {
        if (!player) return;

        Vector3 playerDelta = player.position - lastPlayerPos;

        if (playerDelta.sqrMagnitude > 0f)
        {
            foreach (var a in asteroids)
            {
                if (a.transform)
                    a.transform.position -= playerDelta;
            }
        }

        float currentWorldY = transform.eulerAngles.y;
        float deltaY = Mathf.DeltaAngle(lastWorldY, currentWorldY);

        if (Mathf.Abs(deltaY) > 0.001f)
        {
            Quaternion rot = Quaternion.AngleAxis(deltaY, Vector3.up);

            foreach (var a in asteroids)
            {
                // Rotate asteroid position around player
                Vector3 offset = a.transform.position - player.position;
                offset = rot * offset;
                a.transform.position = player.position + offset;

                // Rotate asteroid drift direction
                if (a.movement)
                    a.movement.driftDirection = rot * a.movement.driftDirection;
            }
        }

        lastWorldY = currentWorldY;
        lastPlayerPos = player.position;
    }

    // ---------------------------------------------------------------------
    // Spawning
    // ---------------------------------------------------------------------
    private void SpawnAsteroids()
    {
        ClearAsteroids();

        int spawnCount = Random.Range(debugCountMin, debugCountMax + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
            //GameObject asteroid = Instantiate(prefab, transform);

            float angle = Random.value * Mathf.PI * 2f;
            float maxSpawn = spawnRadius * radarSpawnBias;
            float dist = Random.Range(3f, maxSpawn);

            //asteroid.transform.localPosition =
            //    new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            Vector3 worldPos = player.position +
    new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            GameObject asteroid = Instantiate(prefab, worldPos, Quaternion.identity, transform);

            float size = Random.Range(debugSizeMin, debugSizeMax);
            asteroid.transform.localScale = Vector3.one * size;

            Transform visual = null;
            SpriteRenderer sr = null;

            foreach (Transform c in asteroid.transform)
            {
                sr = c.GetComponent<SpriteRenderer>();
                if (sr)
                {
                    visual = c;
                    break;
                }
            }

            float randomDeg = Random.Range(
                movementDirectionRange.x,
                movementDirectionRange.y
            );

            Vector3 dir = DirFromDegrees(randomDeg);

            AsteroidMovement move = asteroid.AddComponent<AsteroidMovement>();
            move.driftDirection = dir;
            move.driftSpeed = Random.Range(debugSpeedMin, debugSpeedMax);
            move.rotationSpeed = Random.Range(debugRotationMin, debugRotationMax);

            asteroids.Add(new AsteroidData
            {
                transform = asteroid.transform,
                visualChild = visual,
                sprite = sr,
                movement = move,
                size = size,
                directionDeg = randomDeg
            });
        }
    }

    private void ClearAsteroids()
    {
        foreach (var a in asteroids)
            if (a.transform)
                Destroy(a.transform.gameObject);

        asteroids.Clear();
    }

    private void SyncDebugValues()
    {
        debugCountMin = asteroidCount.x;
        debugCountMax = asteroidCount.y;

        debugSizeMin = sizeRange.x;
        debugSizeMax = sizeRange.y;

        debugRotationMin = rotationSpeedRange.x;
        debugRotationMax = rotationSpeedRange.y;

        debugSpeedMin = movementSpeedRange.x;
        debugSpeedMax = movementSpeedRange.y;
    }

    // ---------------------------------------------------------------------
    // Radar & Occlusion
    // ---------------------------------------------------------------------
    private void UpdateRadarVisibilityAndOcclusion()
    {
        if (!player) return;

        foreach (var a in asteroids)
        {
            if (!a.sprite) continue;

            float d = Vector3.Distance(a.transform.position, player.position);
            a.sprite.enabled = d <= radarRadius;

            //if (a.visualChild && a.sprite.enabled)
            //    a.visualChild.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    // ---------------------------------------------------------------------
    // World Wrapping
    // ---------------------------------------------------------------------
    private void HandleWorldWrapping()
    {
        float wrapDistance = spawnRadius * 1.5f;

        foreach (var a in asteroids)
        {
            Vector3 offset = a.transform.position - player.position;
            Vector2 planar = new Vector2(offset.x, offset.z);

            if (planar.magnitude > wrapDistance)
            {
                Vector2 respawnDir = -planar.normalized * spawnRadius;
                a.transform.position =
                    player.position + new Vector3(respawnDir.x, 0f, respawnDir.y);
            }
        }
    }

    // ---------------------------------------------------------------------
    // IMGUI
    // ---------------------------------------------------------------------
    private void OnGUI()
    {
        if (!showGUI) return;
        InitStyles();

        Rect box = new Rect(Screen.width - 560, 20, 540, 520);
        GUI.Box(box, "ASTEROID TUNING (Runtime)");

        GUILayout.BeginArea(new Rect(
            box.x + 10,
            box.y + 35,
            box.width - 20,
            box.height - 45
        ));

        scrollPos = GUILayout.BeginScrollView(
            scrollPos,
            false,
            true,
            GUILayout.Width(box.width - 20),
            GUILayout.Height(box.height - 45)
        );

        DrawSection("Asteroid Count");
        DrawIntField("Min Count", ref debugCountMin, ApplyDensity);
        DrawIntField("Max Count", ref debugCountMax, ApplyDensity);

        DrawSection("Asteroid Size");
        DrawFloatField("Min Size", ref debugSizeMin, ApplyDensity);
        DrawFloatField("Max Size", ref debugSizeMax, ApplyDensity);

        DrawSection("Rotation Speed");
        DrawFloatField("Min Rotation", ref debugRotationMin, ApplyDensity);
        DrawFloatField("Max Rotation", ref debugRotationMax, ApplyDensity);

        DrawSection("Movement Speed");
        DrawFloatField("Min Speed", ref debugSpeedMin, ApplyDensity);
        DrawFloatField("Max Speed", ref debugSpeedMax, ApplyDensity);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }


    private void InitStyles()
    {
        if (stylesInitialized) return;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            normal = { textColor = Color.white }
        };

        fieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 17,
            fixedHeight = 30,
            alignment = TextAnchor.MiddleCenter
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };

        stylesInitialized = true;
    }

    private void DrawSection(string title)
    {
        GUILayout.Space(8);
        GUILayout.Label(title, headerStyle);
        GUILayout.Space(6);
    }

    private void DrawFloatField(string label, ref float value, System.Action onChange)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(260));

        string t = value.ToString("F3", CultureInfo.InvariantCulture);
        string n = GUILayout.TextField(t, fieldStyle, GUILayout.Width(160));

        if (float.TryParse(n, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
        {
            if (!Mathf.Approximately(value, v))
            {
                value = v;
                onChange?.Invoke();
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    private void DrawIntField(string label, ref int value, System.Action onChange)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(260));

        string n = GUILayout.TextField(value.ToString(), fieldStyle, GUILayout.Width(160));
        if (int.TryParse(n, out int v) && v != value)
        {
            value = Mathf.Max(0, v);
            onChange?.Invoke();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    // ---------------------------------------------------------------------
    // Apply Methods
    // ---------------------------------------------------------------------
    private void ApplyDensity()
    {
        ValidateRanges();
        SpawnAsteroids();
    }


    private Vector3 DirFromDegrees(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(r), 0f, Mathf.Sin(r)).normalized;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Use world root position
        Vector3 center = player ? player.position : transform.position;

        // Radar radius
        Gizmos.color = Color.cyan;
        DrawCircle(center, radarRadius);

        // Full spawn radius
        Gizmos.color = Color.yellow;
        DrawCircle(center, spawnRadius);

        // Biased spawn radius
        Gizmos.color = Color.green;
        DrawCircle(center, spawnRadius * radarSpawnBias);

        // Wrap boundary
        Gizmos.color = Color.red;
        DrawCircle(center, spawnRadius * 1.5f);

        // Spawned asteroid positions
        Gizmos.color = Color.white;
        foreach (var a in asteroids)
        {
            if (a.transform)
                Gizmos.DrawSphere(a.transform.position, 0.3f);
        }
    }
#endif

#if UNITY_EDITOR
    private void DrawCircle(Vector3 center, float radius, int segments = 64)
    {
        float step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i;
            Vector3 next = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif

    private void ValidateRanges()
    {
        debugCountMin = Mathf.Max(0, debugCountMin);
        debugCountMax = Mathf.Max(debugCountMin, debugCountMax);

        debugSizeMin = Mathf.Max(0.01f, debugSizeMin);
        debugSizeMax = Mathf.Max(debugSizeMin, debugSizeMax);

        debugRotationMin = Mathf.Max(0f, debugRotationMin);
        debugRotationMax = Mathf.Max(debugRotationMin, debugRotationMax);

        debugSpeedMin = Mathf.Max(0f, debugSpeedMin);
        debugSpeedMax = Mathf.Max(debugSpeedMin, debugSpeedMax);
    }
}
