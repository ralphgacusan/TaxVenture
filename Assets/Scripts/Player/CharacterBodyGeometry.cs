using UnityEngine;

/// <summary>Posed character measurements, independent of imported renderer and parent scales.</summary>
public static class CharacterBodyGeometry
{
    public static string Problem(Transform model, Animator animator)
    {
        if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
            return "missing/invalid Humanoid avatar";
        var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skins.Length == 0) return "no imported skinned renderers";
        foreach (SkinnedMeshRenderer skin in skins)
        {
            if (skin.sharedMesh == null || skin.sharedMesh.vertexCount == 0 || skin.sharedMesh.subMeshCount == 0)
                return "empty imported mesh: " + skin.name;
            if (!skin.enabled || !skin.gameObject.activeInHierarchy || skin.bones.Length == 0)
                return "inactive/unbound renderer: " + skin.name;
            if (skin.sharedMaterials.Length == 0) return "no imported material: " + skin.name;
            foreach (Material material in skin.sharedMaterials)
                if (material == null) return "missing imported material: " + skin.name;
        }
        return "";
    }

    // BakeMesh(true) includes scale. Applying localToWorldMatrix here would scale it again.
    public static Vector3 BakedPointToWorld(Vector3 position, Quaternion rotation, Vector3 point) => position + rotation * point;

    public static void Fit(float height, Bounds native, Vector3 origin, out float scale, out Vector3 offset)
    {
        scale = height / native.size.y;
        offset = new Vector3(origin.x - native.center.x, origin.y - native.min.y, origin.z - native.center.z) * scale;
    }

    /// <summary>Detect a lost fit or separated body parts after the native animation has evaluated.</summary>
    public static bool MatchesFit(Bounds posed, float height, Vector3 feet) =>
        Mathf.Abs(posed.size.y - height) <= height * 0.12f &&
        Mathf.Abs(posed.min.y - feet.y) <= height * 0.12f &&
        posed.size.x < height * 1.5f && posed.size.z < height;

    /// <summary>
    /// Rest-pose bounds, independent of animation and of BakeMesh: each skinned mesh's own
    /// bounds through its (non-animated) renderer transform. At rest the bind poses map mesh
    /// space exactly onto the renderer, so this is the model's authored geometry. Used to
    /// calibrate world scale once; knee bends can never feed back into scale.
    /// (Elijah: 6.694 native units here, matching an independent FBX node composition and the
    /// setup tool's bone-skinned measurement; the posed BakeMesh route read 5.946 and
    /// over-scaled him by ~12%.)
    /// </summary>
    public static bool TryMeasureRest(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(root.position, Vector3.zero);
        bool any = false;
        foreach (SkinnedMeshRenderer skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Mesh mesh = skin.sharedMesh;
            if (mesh == null || mesh.vertexCount == 0) continue;
            Bounds local = mesh.bounds;
            Matrix4x4 toWorld = skin.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? local.min.x : local.max.x,
                    (i & 2) == 0 ? local.min.y : local.max.y, (i & 4) == 0 ? local.min.z : local.max.z);
                Vector3 world = toWorld.MultiplyPoint3x4(corner);
                if (!any) { bounds = new Bounds(world, Vector3.zero); any = true; }
                else bounds.Encapsulate(world);
            }
        }
        return any && bounds.size.y > 0.01f && !float.IsInfinity(bounds.size.y) && !float.IsNaN(bounds.size.y);
    }

    public static bool TryMeasure(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(root.position, Vector3.zero);
        bool any = false;
        var baked = new Mesh();
        foreach (SkinnedMeshRenderer skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (skin.sharedMesh == null || skin.sharedMesh.vertexCount == 0) continue;
            skin.BakeMesh(baked, true);
            foreach (Vector3 p in baked.vertices)
            {
                Vector3 world = BakedPointToWorld(skin.transform.position, skin.transform.rotation, p);
                if (!any) { bounds = new Bounds(world, Vector3.zero); any = true; }
                else bounds.Encapsulate(world);
            }
        }
        if (Application.isPlaying) Object.Destroy(baked); else Object.DestroyImmediate(baked);
        return any && bounds.size.y > 0.01f && !float.IsInfinity(bounds.size.y) && !float.IsNaN(bounds.size.y);
    }
}
