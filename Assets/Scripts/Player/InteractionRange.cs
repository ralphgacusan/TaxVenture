using UnityEngine;

/// <summary>
/// PURPOSE:
/// One reach rule for everything the player can tap directly in the world
/// (plaza clients, the guide, profile papers). The centre-screen Interactor
/// already limits itself to 2.5 m, but taps on objects go through the
/// EventSystem's physics raycaster, which has no distance limit, so a tiny
/// far-away NPC could be tapped from across the plaza.
///
/// Distance is measured on the floor plan from the player to the closest
/// point of the target's colliders (or its position), so big objects and
/// small NPCs feel the same.
/// </summary>
public static class InteractionRange
{
    /// <summary>How close the player must be to tap a world object (metres).</summary>
    public const float Reach = 3f;

    private static readonly Collider[] colliderBuffer = new Collider[16];

    /// <summary>True when the player is close enough (or no player exists, e.g. in tests/menus).</summary>
    public static bool PlayerCanReach(Component target, float reach = Reach)
    {
        if (target == null) return false;
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (player == null) return true;
        if (FlatDistance(player.position, target) > reach) return false;
        // T03: with the follow camera outdoors, a tap must also be in the player's own line of sight.
        CameraController owner = CameraController.Instance;
        if (owner != null && owner.IsThirdPersonView)
            return !Occluded(owner.PlayerEye, ClosestPoint(owner.PlayerEye, target), target.transform, player);
        return true;
    }

    private static readonly RaycastHit[] sightHits = new RaycastHit[8];

    /// <summary>
    /// T03: something solid (not the target, not the player, not a trigger) lies between the
    /// player's eye and the point. One bounded cast.
    /// </summary>
    public static bool Occluded(Vector3 eye, Vector3 point, Transform target, Transform player)
    {
        Vector3 to = point - eye;
        float distance = to.magnitude;
        if (distance < 0.05f) return false;
        int count = Physics.RaycastNonAlloc(eye, to / distance, sightHits, distance - 0.03f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform hit = sightHits[i].collider != null ? sightHits[i].collider.transform : null;
            if (hit == null) continue;
            if (target != null && (hit.IsChildOf(target) || target.IsChildOf(hit))) continue;
            if (player != null && hit.IsChildOf(player)) continue;
            return true;
        }
        return false;
    }

    private static Vector3 ClosestPoint(Vector3 from, Component target)
    {
        Vector3 best = target.transform.position;
        float bestDistance = (best - from).sqrMagnitude;
        foreach (Collider c in target.GetComponentsInChildren<Collider>(false))
        {
            if (c == null || !c.enabled) continue;
            Vector3 p = c is MeshCollider mc && !mc.convex ? c.bounds.ClosestPoint(from) : c.ClosestPoint(from);
            float d = (p - from).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = p; }
        }
        return best;
    }

    public static float FlatDistance(Vector3 from, Component target)
    {
        Vector3 closest = target.transform.position;
        float best = Flat(from - closest);
        Collider[] colliders = target.GetComponentsInChildren<Collider>(false);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null || !c.enabled) continue;
            Vector3 p = c is MeshCollider mc && !mc.convex ? c.bounds.ClosestPoint(from) : c.ClosestPoint(from);
            float d = Flat(from - p);
            if (d < best) best = d;
        }
        return best;
    }

    private static float Flat(Vector3 v)
    {
        v.y = 0f;
        return v.magnitude;
    }
}
