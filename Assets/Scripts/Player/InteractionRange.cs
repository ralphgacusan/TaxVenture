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
        return FlatDistance(player.position, target) <= reach;
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
