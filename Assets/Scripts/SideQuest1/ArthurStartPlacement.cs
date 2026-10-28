using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-authored spot for Arthur in Level 1's plaza (Campaign Foundation
/// T30): where he stands during the opening and client round. Move/turn it
/// outside Play Mode and save the scene; at scene load ArthurOpening puts the
/// guide's root (NPC_TutorialGuide: model, collider, marker and talk target)
/// here, upright, facing this marker's yaw, with his feet on the ground below.
/// After all profiles are collected, the guide restores his original corkboard pose.
/// Scale and pitch/roll are ignored.
/// With Gizmos on it draws his model, his talk reach and the way he faces.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArthurStartPlacement : MonoBehaviour
{
    public static ArthurStartPlacement Find(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (ArthurStartPlacement placement in root.GetComponentsInChildren<ArthurStartPlacement>())
                if (placement.enabled && placement.gameObject.activeInHierarchy) return placement;
        return null;
    }

    /// <summary>The upright pose Arthur stands in (marker position, yaw only).</summary>
    public Pose StandPose => new Pose(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

    /// <summary>Moves the guide's root here (its children: model, collider, marker, speaker anchor all follow).</summary>
    public void Apply(Transform guideRoot)
    {
        if (guideRoot == null) return;
        Pose pose = StandPose;
        guideRoot.SetPositionAndRotation(pose.position, pose.rotation);
        GroundModel(guideRoot, pose.position);
    }

    /// <summary>Grounds the actual geometry, including Arthur's hidden startup rig.</summary>
    public static void GroundModel(Transform guideRoot, Vector3 groundSpot)
    {
        if (guideRoot == null) return;
        // The marker chooses the spot, not the model's pivot height. Read mesh
        // geometry (also works while the model is hidden), not UI bounds.
        TutorialNpcController guide = guideRoot.GetComponent<TutorialNpcController>();
        Transform model = guide != null ? guide.ModelRoot : guideRoot;
        float feetY = float.PositiveInfinity;
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            feetY = Mathf.Min(feetY, LowestPoint(filter.transform, filter.sharedMesh.bounds));
        }
        foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (skin.sharedMesh == null) continue;
            // localBounds is an animation/culling envelope, not the soles. Its
            // root-bone space can also differ from the renderer's space. Bake
            // a snapshot with renderer scale compensation instead (Unity 6's
            // useScale=true); TransformPoint then applies the scene transform.
            Mesh snapshot = new Mesh();
            try
            {
                skin.BakeMesh(snapshot, true);
                foreach (Vector3 vertex in snapshot.vertices)
                    feetY = Mathf.Min(feetY, skin.transform.TransformPoint(vertex).y);
            }
            finally
            {
                if (Application.isPlaying) Destroy(snapshot);
                else DestroyImmediate(snapshot);
            }
        }
        Physics.SyncTransforms();
        if (!float.IsPositiveInfinity(feetY) &&
            PlayerPlacement.Ground(groundSpot + Vector3.up * 0.5f, guideRoot, out float groundY))
        {
            guideRoot.position += Vector3.up * (groundY - feetY);
            // Walking must keep this measured sole offset, not infer one from
            // a root that may have been positioned above the raycast range.
            NpcLocomotion mover = guideRoot.GetComponent<NpcLocomotion>();
            if (mover != null) mover.SetFeetOffset(guideRoot.position.y - groundY);
        }
        Physics.SyncTransforms();
    }

    private static float LowestPoint(Transform meshTransform, Bounds bounds)
    {
        float y = float.PositiveInfinity;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            y = Mathf.Min(y, meshTransform.TransformPoint(corner).y);
        }
        return y;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        TutorialNpcController guide = FindAnyObjectByType<TutorialNpcController>(FindObjectsInactive.Include);
        Matrix4x4 previous = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Pose pose = StandPose;
        Matrix4x4 at = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
        if (guide != null && guide.gameObject.scene == gameObject.scene)
        {
            Transform source = guide.transform;
            Gizmos.color = new Color(0.36f, 0.62f, 0.62f, 1f);
            foreach (MeshFilter mesh in source.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mesh.sharedMesh == null) continue;
                Gizmos.matrix = at * source.worldToLocalMatrix * mesh.transform.localToWorldMatrix;
                Gizmos.DrawMesh(mesh.sharedMesh);
            }
        }
        // Where a tap reaches him (InteractionRange.Reach), and the way he faces (+Z).
        Gizmos.matrix = at;
        Gizmos.color = Color.cyan;
        const int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments;
            Gizmos.DrawLine(new Vector3(Mathf.Sin(a) * InteractionRange.Reach, 0.05f, Mathf.Cos(a) * InteractionRange.Reach),
                new Vector3(Mathf.Sin(b) * InteractionRange.Reach, 0.05f, Mathf.Cos(b) * InteractionRange.Reach));
        }
        Gizmos.DrawLine(new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0.05f, 1.2f));
        Gizmos.matrix = previous;
        Gizmos.color = previousColor;
    }
}
