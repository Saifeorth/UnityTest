using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ShipPathRenderer : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public ShipController ship;

    private List<GameObject> waypointMarkers = new List<GameObject>();
    public GameObject markerPrefab;

    void Awake()
    {
        if (!lineRenderer)
            lineRenderer = GetComponent<LineRenderer>();
    }

    public void Initialize(ShipController targetShip)
    {
        ship = targetShip;
        ship.OnWaypointsUpdated += UpdatePath;
        UpdatePath();
    }

    void UpdatePath()
    {
        if (ship == null)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        var waypoints = ship.GetWaypoints();
        List<Vector3> points = new List<Vector3> { ship.transform.position };

        foreach (var wp in waypoints)
            points.Add(wp.position);

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());

        // Optional: update waypoint markers
        foreach (var m in waypointMarkers)
            if (m) Destroy(m);
        waypointMarkers.Clear();

        if (markerPrefab != null)
        {
            foreach (var wp in waypoints)
            {
                var m = Instantiate(markerPrefab, wp.position, wp.rotation);
                waypointMarkers.Add(m);
            }
        }
    }

    private void OnDestroy()
    {
        if (ship != null)
            ship.OnWaypointsUpdated -= UpdatePath;
    }
}
