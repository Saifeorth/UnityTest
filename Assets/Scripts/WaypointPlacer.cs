using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class WaypointPlacer : MonoBehaviour
{
    [Header("References")]
    public LayerMask groundLayer;
    public GameObject waypointMarkerPrefab;
    public ShipSelector selector;

    [Header("Line Settings")]
    public Color lineColor = Color.cyan;
    public float lineWidth = 0.2f;
    public float removeThreshold = 0.5f;

    private Camera cam;
    private LineRenderer lineRenderer;

    // Preview and placed markers
    private GameObject previewMarker;
    private List<GameObject> placedMarkers = new List<GameObject>();

    // Rotation state
    private bool isRotating = false;
    private float currentYaw = 0f;
    private Vector3 lastMousePos;

    private ShipController selectedShip;

    // Track dynamic target-locked markers
    private Dictionary<GameObject, ShipController> dynamicMarkers = new Dictionary<GameObject, ShipController>();

    private AudioSource audioSource;
    public AudioClip waypointPlacementSetSound;
    public AudioClip waypointRotationSetSound;
    public AudioClip lockTargetSound;

    void Awake()
    {
        cam = Camera.main;
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.useWorldSpace = true;

        selector.OnSelectionChanged += OnShipSelectionChanged;

        audioSource = GetComponent<AudioSource>();
    }

    void OnDestroy()
    {
        selector.OnSelectionChanged -= OnShipSelectionChanged;
        UnsubscribeFromShipEvents();
    }

    void Update()
    {
        if (selectedShip == null) return;

        CheckForPlacementStart();
        HandleRotation();
        HandleClear();

        // Auto-remove markers when reached
        RemoveReachedMarkers();

        // 🔹 Update any dynamic enemy markers every frame
        UpdateDynamicMarkers();
    }

    void LateUpdate()
    {
        UpdateLineRenderer();
    }

    // ================================
    // Marker Placement
    // ================================
    private void CheckForPlacementStart()
    {
        if (!isRotating && Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                // Check if we clicked an enemy ship
                var shipHit = hit.collider.GetComponentInParent<ShipController>();
                if (shipHit != null && shipHit.CompareTag("Enemy"))
                {
                    if (selector.SelectedShip != null && !selector.SelectedShip.CompareTag("Enemy"))
                    {
                        audioSource.PlayOneShot(lockTargetSound);
                        CreateTargetLockWaypoint(shipHit);                       
                    }
                        
                    return;
                }
                else if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0)
                {
                    StartPlacingMarker(hit.point);
                }
            }
        }
    }

    private void CreateTargetLockWaypoint(ShipController enemyShip)
    {
        var playerShip = selector?.SelectedShip;
        if (playerShip == null) return;

        WaypointData lockWaypoint = new WaypointData(enemyShip.transform.position, Quaternion.identity)
        {
            isTargetLock = true,
            target = enemyShip
        };

        bool holdShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!holdShift)
        {
            playerShip.ClearWaypoints();
            ClearMarkers();
            playerShip.SetWaypoints(new List<WaypointData> { lockWaypoint });
        }
        else
        {
            playerShip.EnqueueWaypoint(lockWaypoint);
        }

        // 🔹 Create marker that follows the enemy dynamically
        //GameObject marker = CreateMarker(enemyShip.transform.position, Quaternion.identity);
        //dynamicMarkers[marker] = enemyShip; // track dynamic marker
        Debug.Log($"{playerShip.name} locked target {enemyShip.name}");
    }

    private void StartPlacingMarker(Vector3 position)
    {
        if (previewMarker != null) Destroy(previewMarker);

        previewMarker = Instantiate(waypointMarkerPrefab, position, Quaternion.identity);
        audioSource.PlayOneShot(waypointPlacementSetSound);
        lastMousePos = Input.mousePosition;
        currentYaw = 0f;
        isRotating = true;
    }

    private void HandleRotation()
    {
        if (!isRotating || previewMarker == null) return;

        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            currentYaw += delta.x * 0.3f;
            previewMarker.transform.rotation = Quaternion.Euler(0, currentYaw, 0);
            lastMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
            ConfirmPlacement();
        }
    }

    private void ConfirmPlacement()
    {
        var ship = selector?.SelectedShip;
        if (ship == null || previewMarker == null) return;

        bool holdShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Vector3 pos = previewMarker.transform.position;
        Quaternion rot = previewMarker.transform.rotation;

        WaypointData newWaypoint = new WaypointData(pos, rot);

        if (!holdShift)
        {
            ship.ClearWaypoints();
            ClearMarkers();
            placedMarkers.Clear();
            ship.SetWaypoints(new List<WaypointData> { newWaypoint });
        }
        else
        {
            ship.EnqueueWaypoint(newWaypoint);
        }

        CreateMarker(pos, rot);
        Destroy(previewMarker);
        previewMarker = null;

        audioSource.PlayOneShot(waypointRotationSetSound);
    }

    private void HandleClear()
    {
        if (Input.GetMouseButtonDown(2))
        {
            selectedShip?.ClearWaypoints();
            ClearMarkers();
        }
    }

    // ================================
    // Marker Management
    // ================================
    private GameObject CreateMarker(Vector3 pos, Quaternion rot)
    {
        if (waypointMarkerPrefab == null) return null;

        var marker = Instantiate(waypointMarkerPrefab, pos, rot);
        placedMarkers.Add(marker);
        return marker;
    }

    private void ClearMarkers()
    {
        foreach (var m in placedMarkers)
            if (m) Destroy(m);
        placedMarkers.Clear();

        foreach (var kvp in dynamicMarkers)
            if (kvp.Key) Destroy(kvp.Key);
        dynamicMarkers.Clear();

        if (previewMarker)
        {
            Destroy(previewMarker);
            previewMarker = null;
        }
    }

    private void RemoveReachedMarkers()
    {
        if (selectedShip == null) return;

        for (int i = placedMarkers.Count - 1; i >= 0; i--)
        {
            if (placedMarkers[i] == null) continue;

            if (Vector3.Distance(placedMarkers[i].transform.position, selectedShip.transform.position) < removeThreshold)
            {
                Destroy(placedMarkers[i]);
                placedMarkers.RemoveAt(i);
            }
        }
    }

    // 🔹 NEW: Update dynamic (enemy-locked) markers every frame
    private void UpdateDynamicMarkers()
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in dynamicMarkers)
        {
            GameObject marker = kvp.Key;
            ShipController enemy = kvp.Value;

            if (marker == null || enemy == null)
            {
                toRemove.Add(marker);
                continue;
            }

            marker.transform.position = enemy.transform.position;
        }

        // Clean up invalid ones
        foreach (var m in toRemove)
            dynamicMarkers.Remove(m);
    }

    // ================================
    // Line Rendering
    // ================================
    private void UpdateLineRenderer()
    {
        if (selectedShip == null)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        List<Vector3> points = new List<Vector3>();
        points.Add(selectedShip.transform.position);

        WaypointData currentTarget = selectedShip.CurrentWaypoint;

        bool hasDynamicTarget = currentTarget.IsValid() && currentTarget.isTargetLock;

        if (selectedShip.IsMoving && currentTarget.IsValid())
            points.Add(currentTarget.GetCurrentPosition());

        var waypoints = selectedShip.GetWaypoints();
        if (waypoints != null)
        {
            foreach (var wp in waypoints)
            {
                if (wp.position != currentTarget.position)
                    points.Add(wp.GetCurrentPosition());
            }
        }

        if (previewMarker != null)
            points.Add(previewMarker.transform.position);

        if (points.Count >= 2)
        {
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());

            // 🔹 Switch line color depending on waypoint type
            Color targetColor = hasDynamicTarget ? Color.red : lineColor;
            lineRenderer.startColor = targetColor;
            lineRenderer.endColor = targetColor;
        }
        else
        {
            lineRenderer.positionCount = 0;
        }
    }

    // ================================
    // Ship Selection
    // ================================
    private void OnShipSelectionChanged(ShipController ship)
    {
        UnsubscribeFromShipEvents();
        ClearMarkers();
        previewMarker = null;
        isRotating = false;

        StartCoroutine(SwitchShip(ship));
    }

    private IEnumerator SwitchShip(ShipController ship)
    {
        yield return new WaitForEndOfFrame();
        selectedShip = ship;

        if (selectedShip != null)
            selectedShip.OnWaypointsUpdated += UpdateLineRenderer;
    }

    private void UnsubscribeFromShipEvents()
    {
        if (selectedShip != null)
            selectedShip.OnWaypointsUpdated -= UpdateLineRenderer;
    }
}
