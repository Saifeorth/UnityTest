using UnityEngine;

/// <summary>
/// Holds position, rotation, and targeting data for a single waypoint.
/// </summary>
[System.Serializable]
public struct WaypointData
{
    public Vector3 position;
    public Quaternion rotation;
    public bool isTargetLock;
    public ShipController target;

    public WaypointData(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
        isTargetLock = false;
        target = null;
    }

    /// <summary>
    /// Returns the current world-space position for this waypoint.
    /// If it's a target lock, it dynamically follows the target ship.
    /// </summary>
    public Vector3 GetCurrentPosition()
    {
        if (isTargetLock && target != null)
            return target.transform.position;

        return position;
    }

    /// <summary>
    /// Returns true if this waypoint is currently valid (not null or destroyed target).
    /// </summary>
    public bool IsValid()
    {
        return !isTargetLock || (isTargetLock && target != null);
    }
}
