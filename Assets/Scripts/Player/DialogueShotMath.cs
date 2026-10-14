using System;
using System.Numerics;

/// <summary>Scale-relative framing settings; every distance is a fraction of character height.</summary>
public struct DialogueShotSettings
{
    public float BackDistance;      // behind the foreground (listener) head
    public float ShoulderOffset;    // sideways from the listener's head
    public float HeightOffset;      // above the listener's head
    public float FramingBias;       // 0 aims at the speaker; >0 pulls aim toward the listener
    public float TwoShotDistance;   // sideways from the midpoint for the two-shot fallback
    public float CloseUpDistance;   // in front of the speaker's face, toward the listener
    public float CloseUpSideOffset; // sideways, on the same side of the axis as the other shots
    public float CloseUpHeight;     // above the speaker's head point

    // Side offset 0.40 / back 0.50 puts the camera ~38 degrees off the line
    // between the two characters (was ~24 degrees with 0.24 / 0.55), so the
    // foreground listener no longer hides the speaker.
    public static DialogueShotSettings Default => new DialogueShotSettings
    {
        BackDistance = 0.5f,
        ShoulderOffset = 0.4f,
        HeightOffset = 0.04f,
        FramingBias = 0.12f,
        TwoShotDistance = 1.25f,
        CloseUpDistance = 0.3f,
        CloseUpSideOffset = 0.1f,
        CloseUpHeight = 0f
    };
}

/// <summary>A camera placement: where it is and what it looks at.</summary>
public readonly struct DialogueShotPose
{
    public readonly Vector3 Position;
    public readonly Vector3 LookAt;

    public DialogueShotPose(Vector3 position, Vector3 lookAt)
    {
        Position = position;
        LookAt = lookAt;
    }
}

/// <summary>
/// Pure shot/reverse-shot geometry in Unity's convention (+Y up, left-handed,
/// right of +Z forward is +X). Uses System.Numerics so it is testable outside
/// Unity; the director converts at the boundary.
///
/// Both over-the-shoulder shots stay on the same side of the Elijah→NPC axis
/// (the 180-degree rule): the NPC-line shot sits behind Elijah's RIGHT shoulder,
/// the player-line shot behind the NPC's LEFT shoulder, so Elijah stays
/// screen-left and the NPC screen-right in both, as in the reference frame.
/// </summary>
public static class DialogueShotMath
{
    public static readonly Vector3 Up = new Vector3(0f, 1f, 0f);

    public static Vector3 Horizontal(Vector3 v) => new Vector3(v.X, 0f, v.Z);

    /// <summary>Right-hand perpendicular of a horizontal forward vector (Unity convention).</summary>
    public static Vector3 RightOf(Vector3 forward) => new Vector3(forward.Z, 0f, -forward.X);

    /// <summary>Normalized horizontal direction from a to b, or the fallback when they coincide.</summary>
    public static Vector3 HorizontalDirection(Vector3 from, Vector3 to, Vector3 fallback)
    {
        Vector3 d = Horizontal(to - from);
        float len = d.Length();
        if (len > 1e-4f) return d / len;
        Vector3 f = Horizontal(fallback);
        float fl = f.Length();
        return fl > 1e-4f ? f / fl : new Vector3(0f, 0f, 1f);
    }

    /// <summary>Yaw in degrees that makes +Z face along a horizontal direction.</summary>
    public static float YawDegrees(Vector3 direction)
    {
        Vector3 d = Horizontal(direction);
        return (float)(Math.Atan2(d.X, d.Z) * 180.0 / Math.PI);
    }

    /// <summary>
    /// Over-the-shoulder camera behind <paramref name="listenerHead"/> framing
    /// <paramref name="speakerHead"/>. <paramref name="side"/> +1 = listener's right.
    /// <paramref name="backScale"/> shortens the pull-back for tight spaces.
    /// </summary>
    public static DialogueShotPose OverShoulder(Vector3 listenerHead, Vector3 speakerHead,
        float characterHeight, DialogueShotSettings s, float side, float backScale = 1f)
    {
        float h = Math.Max(0.01f, characterHeight);
        Vector3 forward = HorizontalDirection(listenerHead, speakerHead, new Vector3(0f, 0f, 1f));
        Vector3 right = RightOf(forward);
        Vector3 position = listenerHead
            - forward * (s.BackDistance * backScale * h)
            + right * (Math.Sign(side == 0 ? 1 : side) * s.ShoulderOffset * h)
            + Up * (s.HeightOffset * h);
        Vector3 lookAt = Vector3.Lerp(speakerHead, listenerHead, Clamp01(s.FramingBias));
        return new DialogueShotPose(position, lookAt);
    }

    /// <summary>NPC speaking: over Elijah's right shoulder toward the NPC.</summary>
    public static DialogueShotPose NpcLineShot(Vector3 elijahHead, Vector3 npcHead,
        float characterHeight, DialogueShotSettings s, float backScale = 1f) =>
        OverShoulder(elijahHead, npcHead, characterHeight, s, +1f, backScale);

    /// <summary>Elijah speaking: over the NPC's left shoulder toward Elijah (same side of the axis).</summary>
    public static DialogueShotPose PlayerLineShot(Vector3 elijahHead, Vector3 npcHead,
        float characterHeight, DialogueShotSettings s, float backScale = 1f) =>
        OverShoulder(npcHead, elijahHead, characterHeight, s, -1f, backScale);

    /// <summary>
    /// Fallback: both characters side by side, camera perpendicular to the axis.
    /// <paramref name="side"/> +1 keeps it on the same side as the OTS shots.
    /// </summary>
    public static DialogueShotPose TwoShot(Vector3 elijahHead, Vector3 npcHead,
        float characterHeight, DialogueShotSettings s, float side)
    {
        float h = Math.Max(0.01f, characterHeight);
        Vector3 mid = (elijahHead + npcHead) * 0.5f;
        Vector3 forward = HorizontalDirection(elijahHead, npcHead, new Vector3(0f, 0f, 1f));
        Vector3 right = RightOf(forward);
        float gap = Horizontal(npcHead - elijahHead).Length();
        float distance = Math.Max(s.TwoShotDistance * h, gap * 1.1f);
        Vector3 position = mid + right * (Math.Sign(side == 0 ? 1 : side) * distance) + Up * (s.HeightOffset * h);
        return new DialogueShotPose(position, mid);
    }

    /// <summary>
    /// Solo close-up: camera in front of the speaker's face, between the two
    /// characters and looking away from the listener, so only the speaker is in
    /// frame. Offset to the right of the Elijah→NPC axis like every other shot.
    /// </summary>
    public static DialogueShotPose NpcCloseUp(Vector3 elijahHead, Vector3 npcHead,
        float characterHeight, DialogueShotSettings s) =>
        CloseUp(npcHead, elijahHead, characterHeight, s, -1f);

    /// <summary>Solo close-up of Elijah speaking.</summary>
    public static DialogueShotPose PlayerCloseUp(Vector3 elijahHead, Vector3 npcHead,
        float characterHeight, DialogueShotSettings s) =>
        CloseUp(elijahHead, npcHead, characterHeight, s, +1f);

    private static DialogueShotPose CloseUp(Vector3 speakerHead, Vector3 listenerHead,
        float characterHeight, DialogueShotSettings s, float side)
    {
        float h = Math.Max(0.01f, characterHeight);
        Vector3 toListener = HorizontalDirection(speakerHead, listenerHead, new Vector3(0f, 0f, 1f));
        Vector3 right = RightOf(toListener);
        Vector3 position = speakerHead
            + toListener * (s.CloseUpDistance * h)
            + right * (Math.Sign(side == 0 ? 1 : side) * s.CloseUpSideOffset * h)
            + Up * (s.CloseUpHeight * h);
        return new DialogueShotPose(position, speakerHead);
    }

    /// <summary>True when <paramref name="point"/> is behind the camera (out of frame).</summary>
    public static bool IsBehindCamera(DialogueShotPose shot, Vector3 point, float margin = 0f)
    {
        Vector3 forward = shot.LookAt - shot.Position;
        float len = forward.Length();
        if (len < 1e-5f) return false;
        return Vector3.Dot(point - shot.Position, forward / len) < -margin;
    }

    /// <summary>
    /// Which side of the directed axis a→b point p lies on: +1 right, -1 left, 0 on it.
    /// Used to check the 180-degree rule.
    /// </summary>
    public static int SideOfAxis(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 forward = HorizontalDirection(a, b, new Vector3(0f, 0f, 1f));
        float d = Vector3.Dot(Horizontal(p - a), RightOf(forward));
        return d > 1e-5f ? 1 : d < -1e-5f ? -1 : 0;
    }

    /// <summary>
    /// Visual-only placement of the conversation Elijah on the line toward the NPC,
    /// keeping a readable separation. Never moves the gameplay player.
    /// </summary>
    public static Vector3 PlaceListener(Vector3 playerFloor, Vector3 speakerFloor,
        float minSeparation, float maxSeparation, out bool moved)
    {
        Vector3 toPlayer = Horizontal(playerFloor - speakerFloor);
        float distance = toPlayer.Length();
        float lo = Math.Max(0f, Math.Min(minSeparation, maxSeparation));
        float hi = Math.Max(minSeparation, maxSeparation);
        float clamped = Math.Min(Math.Max(distance, lo), hi);
        moved = Math.Abs(clamped - distance) > 1e-4f;
        if (!moved) return playerFloor;
        Vector3 dir = distance > 1e-4f ? toPlayer / distance : new Vector3(0f, 0f, -1f);
        Vector3 p = speakerFloor + dir * clamped;
        return new Vector3(p.X, playerFloor.Y, p.Z);
    }

    /// <summary>Smoothstep easing used by every blend.</summary>
    public static float Ease(float t)
    {
        t = Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
