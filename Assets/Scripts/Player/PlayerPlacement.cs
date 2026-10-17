using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Puts the first-person player somewhere in a scene by scene path, the same
/// way Tutorial Hub office drills always have (moved here from
/// PracticeModeRuntime so the Level 2 office start shares it): stand on the
/// floor part way from "spawn" to "face", looking at "face", keeping the
/// player's own height above the ground, with physics and the look reset.
/// </summary>
public static class PlayerPlacement
{
    /// <summary>Default share of the way from spawn to face (office drills use 0.45).</summary>
    public const float DefaultBlend = 0.45f;

    /// <summary>Finds "Root/Child/..." among the scene's roots (inactive objects included).</summary>
    public static Transform Find(Scene scene, string path)
    {
        if (!scene.IsValid() || string.IsNullOrWhiteSpace(path)) return null;
        path = path.Trim();
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            Transform found = slash < 0 ? root.transform : root.transform.Find(path.Substring(slash + 1));
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>Centre of the object's renderers (or its position when it has none).</summary>
    public static bool TryCenter(Scene scene, string path, out Vector3 center)
    {
        center = default;
        Transform t = Find(scene, path);
        if (t == null) return false;
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            center = t.position;
            return true;
        }
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        center = bounds.center;
        return true;
    }

    /// <summary>
    /// Stands the player on the floor between "spawn" and "face", looking at
    /// "face". False (player untouched) when there is no player or no spawn.
    /// </summary>
    public static bool PlaceBetween(Scene scene, string spawnPath, string facePath, float blend = DefaultBlend)
    {
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (player == null || !TryCenter(scene, spawnPath, out Vector3 from)) return false;
        Vector3 look = TryCenter(scene, facePath, out Vector3 facePoint) ? facePoint : from + player.forward;
        Vector3 spot = Vector3.Lerp(from, look, blend);

        // Keep the player's own height above the ground they stand on now.
        float lift = Ground(player.position + Vector3.up * 0.5f, player, out float groundY) ? player.position.y - groundY : 1f;
        if (Ground(new Vector3(spot.x, spot.y + 1.2f, spot.z), player, out float floorY)) spot.y = floorY + lift + 0.02f;
        else spot.y = player.position.y;

        Vector3 flat = look - spot;
        flat.y = 0f;
        Quaternion facing = flat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flat.normalized, Vector3.up) : player.rotation;

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = spot;
            body.rotation = facing;
            body.linearVelocity = Vector3.zero;
        }
        player.SetPositionAndRotation(spot, facing);
        Physics.SyncTransforms();
        FirstPersonCameraController look3 = Object.FindAnyObjectByType<FirstPersonCameraController>();
        if (look3 != null) look3.ResetLook();
        return true;
    }

    /// <summary>Highest non-trigger ground below the origin (within 4 m), ignoring the player.</summary>
    public static bool Ground(Vector3 origin, Transform player, out float y)
    {
        y = 0f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.MinValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(player)) continue;
            if (hit.point.y > best) best = hit.point.y;
        }
        if (best == float.MinValue) return false;
        y = best;
        return true;
    }
}
