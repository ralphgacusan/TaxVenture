using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projects workstation document drags onto the top of the desk collider,
/// clamps them to its usable local-space rectangle, and keeps them at their
/// authored height. Invalid overlap candidates leave the item at its last
/// valid position.
/// </summary>
[DisallowMultipleComponent]
public sealed class DeskPlacementSurface : MonoBehaviour
{
    [SerializeField] private BoxCollider surfaceCollider;
    [SerializeField, Min(0f)] private float edgeInset = 0.035f;
    [SerializeField] private Collider[] placementObstacles;

    private readonly List<Collider> cachedObstacles = new List<Collider>();

    private void Awake()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        edgeInset = Mathf.Max(0f, edgeInset);
        if (surfaceCollider == null)
            surfaceCollider = GetComponent<BoxCollider>();
    }

    /// <summary>Returns the pointer ray intersection with the desk top.</summary>
    public bool TryGetPointerPoint(Ray pointerRay, out Vector3 point)
    {
        point = default;
        if (surfaceCollider == null || !surfaceCollider.enabled)
            return false;

        Transform surface = surfaceCollider.transform;
        Vector3 topLocal = surfaceCollider.center + Vector3.up * (surfaceCollider.size.y * 0.5f);
        Plane topPlane = new Plane(surface.up, surface.TransformPoint(topLocal));
        if (!topPlane.Raycast(pointerRay, out float distance) || distance < 0f)
            return false;

        point = pointerRay.GetPoint(distance);
        return true;
    }

    /// <summary>
    /// Clamps a proposed item position to the desk top while preserving its
    /// current local height. Returns false for an oversized item or overlap.
    /// </summary>
    public bool TryClampPosition(Transform item, Collider itemCollider, Vector3 proposedPosition,
        out Vector3 clampedPosition)
    {
        clampedPosition = item != null ? item.position : proposedPosition;
        if (item == null || itemCollider == null || surfaceCollider == null || !surfaceCollider.enabled)
            return false;

        Transform surface = surfaceCollider.transform;
        Vector3 currentLocal = surface.InverseTransformPoint(item.position);
        Vector3 candidateLocal = surface.InverseTransformPoint(proposedPosition);
        GetLocalFootprint(itemCollider, surface, currentLocal, out float extentX, out float extentZ);

        Vector3 half = surfaceCollider.size * 0.5f;
        float minX = surfaceCollider.center.x - half.x + edgeInset + extentX;
        float maxX = surfaceCollider.center.x + half.x - edgeInset - extentX;
        float minZ = surfaceCollider.center.z - half.z + edgeInset + extentZ;
        float maxZ = surfaceCollider.center.z + half.z - edgeInset - extentZ;
        if (minX > maxX || minZ > maxZ)
            return false;

        candidateLocal.x = Mathf.Clamp(candidateLocal.x, minX, maxX);
        candidateLocal.z = Mathf.Clamp(candidateLocal.z, minZ, maxZ);
        candidateLocal.y = currentLocal.y;
        Vector3 candidateWorld = surface.TransformPoint(candidateLocal);

        Bounds candidateBounds = itemCollider.bounds;
        candidateBounds.center += candidateWorld - item.position;
        for (int i = 0; i < cachedObstacles.Count; i++)
        {
            Collider obstacle = cachedObstacles[i];
            if (obstacle == null || !obstacle.enabled || obstacle == itemCollider ||
                obstacle.transform == item || obstacle.transform.IsChildOf(item))
                continue;
            if (candidateBounds.Intersects(obstacle.bounds))
                return false;
        }

        clampedPosition = candidateWorld;
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

        // The current office scene keeps both document colliders under the desk.
        // This fallback supports that scene until references are serialized in the Inspector.
        if (cachedObstacles.Count == 0 && surfaceCollider != null)
        {
            Collider[] children = surfaceCollider.GetComponentsInChildren<Collider>(true);
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

    private static void GetLocalFootprint(Collider itemCollider, Transform surface,
        Vector3 itemLocalPosition, out float extentX, out float extentZ)
    {
        Bounds bounds = itemCollider.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        extentX = 0f;
        extentZ = 0f;

        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
        {
            Vector3 corner = new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z);
            Vector3 local = surface.InverseTransformPoint(corner);
            extentX = Mathf.Max(extentX, Mathf.Abs(local.x - itemLocalPosition.x));
            extentZ = Mathf.Max(extentZ, Mathf.Abs(local.z - itemLocalPosition.z));
        }
    }
}
