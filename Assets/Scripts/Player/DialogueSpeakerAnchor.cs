using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional per-NPC framing override. Add it to an NPC (or its parent) only when
/// the automatic renderer-bounds measurement frames it badly, e.g. when the NPC
/// shares a parent with furniture. Shots aim at <see cref="head"/>; the floor
/// comes from <see cref="feet"/> (or this transform). Nothing else reads it.
/// </summary>
[DisallowMultipleComponent]
public class DialogueSpeakerAnchor : MonoBehaviour
{
    [Tooltip("Eye/head point the camera frames for this speaker.")]
    public Transform head;

    [Tooltip("Floor point under the speaker. Defaults to this transform.")]
    public Transform feet;
}

/// <summary>Measured framing data for one conversation participant.</summary>
public struct DialogueSpeakerFrame
{
    public Transform Root;     // colliders under this root are ignored by shot obstruction tests
    public Vector3 Head;       // world point shots aim at
    public Vector3 Floor;      // world floor point under the speaker
    public float Height;       // world-unit standing height
}

/// <summary>
/// Resolves where a speaking NPC's head and feet are without any new NPC
/// contract: an optional DialogueSpeakerAnchor, otherwise the visible
/// renderers of the dialogue owner (walking up at most two parents when the
/// owner component sits on a renderer-less collider child), otherwise its
/// colliders. Paper pickups, particles, trails and world-space UI are ignored.
/// Shared by the runtime director and the Editor setup report.
/// </summary>
public static class DialogueSpeakerFrames
{
    private const float HeadFraction = 0.9f;
    private const int MaxParentClimb = 2;
    private static readonly List<Renderer> renderers = new List<Renderer>(32);
    private static readonly List<Collider> colliders = new List<Collider>(16);

    public static bool TryResolve(Component owner, out DialogueSpeakerFrame frame)
    {
        frame = default;
        if (owner == null) return false;

        DialogueSpeakerAnchor anchor = owner.GetComponentInParent<DialogueSpeakerAnchor>();
        if (anchor == null) anchor = owner.GetComponentInChildren<DialogueSpeakerAnchor>();
        if (anchor != null && anchor.head != null)
        {
            Vector3 floor = anchor.feet != null ? anchor.feet.position : anchor.transform.position;
            frame.Root = anchor.transform;
            frame.Head = anchor.head.position;
            frame.Floor = floor;
            frame.Height = Mathf.Max(0.05f, (anchor.head.position.y - floor.y) / HeadFraction);
            return true;
        }

        Transform candidate = owner.transform;
        for (int climb = 0; climb <= MaxParentClimb && candidate != null; climb++)
        {
            if (TryRendererBounds(candidate, out Bounds bounds))
            {
                FromBounds(candidate, bounds, out frame);
                return true;
            }
            candidate = candidate.parent;
        }

        if (TryColliderBounds(owner.transform, out Bounds colliderBounds))
        {
            FromBounds(owner.transform, colliderBounds, out frame);
            return true;
        }
        return false;
    }

    private static void FromBounds(Transform root, Bounds b, out DialogueSpeakerFrame frame)
    {
        float height = Mathf.Max(0.05f, b.size.y);
        frame.Root = root;
        frame.Height = height;
        frame.Floor = new Vector3(b.center.x, b.min.y, b.center.z);
        frame.Head = new Vector3(b.center.x, b.min.y + height * HeadFraction, b.center.z);
    }

    private static bool TryRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        renderers.Clear();
        root.GetComponentsInChildren(false, renderers);
        foreach (Renderer r in renderers)
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if (r.GetComponentInParent<Canvas>() != null) continue;
            if (r.GetComponentInParent<TaxpayerProfileWorldPickup>() != null) continue;
            if (r.GetComponentInParent<ConversationActorVisual>() != null) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        renderers.Clear();
        return any && bounds.size.y > 1e-3f;
    }

    private static bool TryColliderBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        colliders.Clear();
        root.GetComponentsInChildren(false, colliders);
        foreach (Collider c in colliders)
        {
            if (!c.enabled) continue;
            if (!any) { bounds = c.bounds; any = true; }
            else bounds.Encapsulate(c.bounds);
        }
        colliders.Clear();
        return any && bounds.size.y > 1e-3f;
    }
}
