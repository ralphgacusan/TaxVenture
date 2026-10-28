using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Scene-authored walk for Marie's trip to her case (Level 1 Living Plaza):
/// the points on both sides of each door she walks through, and where she
/// waits at the front desk. ClientConsultation (the one owner of her move)
/// reads it; the conference-room spot itself stays the case's clientFinale
/// spotPath. Move the child markers outside Play Mode to retune the route;
/// with Gizmos on it draws the whole walk.
///
/// A door's pair of points must sit a little outside and inside its opening,
/// in line with it, so each doorway is crossed straight through the gap.
/// </summary>
[DisallowMultipleComponent]
public sealed class MarieOfficeRoute : MonoBehaviour
{
    [Header("Plaza to the front desk")]
    [SerializeField] private Transform entranceOutside;
    [SerializeField] private Transform entranceInside;
    [Tooltip("Where she waits beside the front desk; +Z is the way she faces (toward the receptionist).")]
    [SerializeField] private Transform receptionWait;

    [Header("Front desk to the conference room")]
    [SerializeField] private Transform innerDoorNorth;
    [SerializeField] private Transform innerDoorSouth;
    [SerializeField] private Transform conferenceDoorOutside;
    [SerializeField] private Transform conferenceDoorInside;

    public static MarieOfficeRoute Find(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MarieOfficeRoute route in root.GetComponentsInChildren<MarieOfficeRoute>())
                if (route.isActiveAndEnabled) return route;
        return null;
    }

    /// <summary>The front-desk waiting spot (null when not authored).</summary>
    public Transform ReceptionWait => receptionWait;

    /// <summary>Stops from the plaza to the front-desk waiting spot.</summary>
    public List<Vector3> ToReception()
    {
        var stops = new List<Vector3>();
        Add(stops, entranceOutside, entranceInside, receptionWait);
        return stops;
    }

    /// <summary>Stops to the conference room's door (the client spot is added by the caller).</summary>
    public List<Vector3> ToRoom(bool fromPlaza)
    {
        var stops = new List<Vector3>();
        if (fromPlaza) Add(stops, entranceOutside, entranceInside);
        Add(stops, innerDoorNorth, innerDoorSouth, conferenceDoorOutside, conferenceDoorInside);
        return stops;
    }

    private static void Add(List<Vector3> stops, params Transform[] points)
    {
        foreach (Transform point in points)
            if (point != null) stops.Add(NpcLife.Ground(point.position));
    }

    private void OnDrawGizmos()
    {
        Transform[] walk = { entranceOutside, entranceInside, receptionWait, innerDoorNorth, innerDoorSouth, conferenceDoorOutside, conferenceDoorInside };
        Gizmos.color = new Color(0.95f, 0.55f, 0.75f, 1f);
        Transform previous = null;
        foreach (Transform point in walk)
        {
            if (point == null) continue;
            Gizmos.DrawWireSphere(point.position + Vector3.up * 0.05f, 0.18f);
            if (previous != null) Gizmos.DrawLine(previous.position + Vector3.up * 0.05f, point.position + Vector3.up * 0.05f);
            previous = point;
        }
        if (receptionWait != null)
            Gizmos.DrawLine(receptionWait.position + Vector3.up * 0.05f, receptionWait.position + Vector3.up * 0.05f + receptionWait.forward * 0.7f);
    }
}
