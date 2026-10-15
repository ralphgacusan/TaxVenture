using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared placement contract for workstation documents (case folder and tax
/// code book). Pointer rays are projected onto a plane parallel to the desk
/// top at the item's resting height, then clamped to the desk collider's
/// usable local-space rectangle. Overlaps with other desk items slide along
/// the free axis; if no slide is possible the caller keeps its last valid
/// position.
/// </summary>
[DisallowMultipleComponent]
public sealed class DeskPlacementSurface : MonoBehaviour
{
    [Tooltip("Box collider whose top face is the usable desk top.")]
    [SerializeField] private BoxCollider surfaceCollider;

    [Tooltip("Distance kept between a dragged item and the desk edge.")]
    [SerializeField, Min(0f)] private float edgeInset = 0.035f;

    [Tooltip("Height a dragged item is raised above the desk while held.")]
    [SerializeField, Min(0f)] private float dragLiftHeight = 0.015f;

    [Tooltip("Other desk items that dragged documents must not overlap.")]
    [SerializeField] private Collider[] placementObstacles;

    private readonly List<Collider> cachedObstacles = new List<Collider>();

    private void Awake()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        edgeInset = Mathf.Max(0f, edgeInset);
        dragLiftHeight = Mathf.Max(0f, dragLiftHeight);
        if (surfaceCollider == null)
            surfaceCollider = GetComponent<BoxCollider>();
    }

    /// <summary>World offset applied to an item while it is held.</summary>
    public Vector3 DragLiftOffset =>
        surfaceCollider != null ? surfaceCollider.transform.up * dragLiftHeight : Vector3.zero;

    /// <summary>
    /// Returns where the pointer ray meets the plane parallel to the desk top
    /// that passes through <paramref name="restPosition"/>, so the held item
    /// stays exactly under the pointer.
    /// </summary>
    public bool TryGetPointerPoint(Ray pointerRay, Vector3 restPosition, out Vector3 point)
    {
        point = default;
        if (surfaceCollider == null || !surfaceCollider.enabled)
            return false;

        Plane plane = new Plane(surfaceCollider.transform.up, restPosition);
        if (!plane.Raycast(pointerRay, out float distance) || distance < 0f)
            return false;

        point = pointerRay.GetPoint(distance);
        return true;
    }

    /// <summary>
    /// Clamps a proposed resting position to the desk top. The proposed
    /// height is kept. Returns false when the item cannot fit or every
    /// candidate overlaps another desk item.
    /// </summary>
    public bool TryClampPosition(Transform item, Collider itemCollider, Vector3 proposedPosition,
        out Vector3 clampedPosition)
    {
        clampedPosition = proposedPosition;
        if (item == null || itemCollider == null || surfaceCollider == null || !surfaceCollider.enabled)
            return false;

        Transform surface = surfaceCollider.transform;
        Vector3 currentLocal = surface.InverseTransformPoint(item.position);
        Vector3 candidateLocal = surface.InverseTransformPoint(proposedPosition);
        GetLocalFootprint(itemCollider, surface, currentLocal, out Vector2 footprintMin, out Vector2 footprintMax);

        Vector3 half = surfaceCollider.size * 0.5f;
        float minX = surfaceCollider.center.x - half.x + edgeInset - footprintMin.x;
        float maxX = surfaceCollider.center.x + half.x - edgeInset - footprintMax.x;
        float minZ = surfaceCollider.center.z - half.z + edgeInset - footprintMin.y;
        float maxZ = surfaceCollider.center.z + half.z - edgeInset - footprintMax.y;
        if (minX > maxX || minZ > maxZ)
            return false;

        candidateLocal.x = Mathf.Clamp(candidateLocal.x, minX, maxX);
        candidateLocal.z = Mathf.Clamp(candidateLocal.z, minZ, maxZ);

        // Try the full move first, then slide along each desk axis.
        Vector3 slideX = new Vector3(candidateLocal.x, candidateLocal.y,
            Mathf.Clamp(currentLocal.z, minZ, maxZ));
        Vector3 slideZ = new Vector3(Mathf.Clamp(currentLocal.x, minX, maxX), candidateLocal.y,
            candidateLocal.z);

        if (IsFree(item, itemCollider, surface.TransformPoint(candidateLocal), out clampedPosition) ||
            IsFree(item, itemCollider, surface.TransformPoint(slideX), out clampedPosition) ||
            IsFree(item, itemCollider, surface.TransformPoint(slideZ), out clampedPosition))
            return true;

        clampedPosition = item.position;
        return false;
    }

    private bool IsFree(Transform item, Collider itemCollider, Vector3 candidateWorld, out Vector3 position)
    {
        position = candidateWorld;
        Bounds currentBounds = itemCollider.bounds;
        Bounds candidateBounds = currentBounds;
        candidateBounds.center += candidateWorld - item.position;

        for (int i = 0; i < cachedObstacles.Count; i++)
        {
            Collider obstacle = cachedObstacles[i];
            if (obstacle == null || !obstacle.enabled || !obstacle.gameObject.activeInHierarchy ||
                obstacle == itemCollider || obstacle.transform == item ||
                obstacle.transform.IsChildOf(item))
                continue;

            Bounds obstacleBounds = obstacle.bounds;
            // An item that already touches an obstacle may still move away from it.
            if (currentBounds.Intersects(obstacleBounds))
                continue;
            if (candidateBounds.Intersects(obstacleBounds))
                return false;
        }

        return true;
    }

    private void CacheReferences()
    {
        if (surfaceCollider == null)
            surfaceCollider = GetComponent<BoxCollider>();

        cachedObstacles.Clear();
        if (placementObstacles != null)
        {
            for (int i = 0; i < placementObstacles.Length; i++)
                AddObstacle(placementObstacles[i]);
        }

        // Fallback for scenes without serialized obstacles: both document
        // colliders sit under the desk object that owns this component.
        if (cachedObstacles.Count == 0)
        {
            Collider[] children = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < children.Length; i++)
            {
                string itemName = children[i].gameObject.name;
                if (itemName == "CaseFolder" || itemName == "TaxCodeBook")
                    AddObstacle(children[i]);
            }
        }
    }

    private void AddObstacle(Collider obstacle)
    {
        if (obstacle != null && obstacle != surfaceCollider && !cachedObstacles.Contains(obstacle))
            cachedObstacles.Add(obstacle);
    }

    /// <summary>
    /// Desk-local X/Z offsets of the item's bounds from its pivot. Kept
    /// asymmetric because document pivots are not centred on their colliders.
    /// </summary>
    private static void GetLocalFootprint(Collider itemCollider, Transform surface,
        Vector3 itemLocalPosition, out Vector2 footprintMin, out Vector2 footprintMax)
    {
        Bounds bounds = itemCollider.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        footprintMin = Vector2.zero;
        footprintMax = Vector2.zero;

        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
        {
            Vector3 corner = new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z);
            Vector3 local = surface.InverseTransformPoint(corner) - itemLocalPosition;
            footprintMin = Vector2.Min(footprintMin, new Vector2(local.x, local.z));
            footprintMax = Vector2.Max(footprintMax, new Vector2(local.x, local.z));
        }
    }
}
