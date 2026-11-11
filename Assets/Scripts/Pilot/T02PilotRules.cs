using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Milestone T02 visual/contact pilot: the isolated review scene's name and
/// the pure, testable rules behind it (the action timeline, the reach solve
/// for Arthur's one standing position, and frame-time statistics). No scene
/// objects, no campaign state, no saves.
/// </summary>
public static class T02PilotIsolation
{
    public const string SceneName = "T02_PilotReview";

    /// <summary>The pilot review scene: campaign owners stay out of it (no save read or write).</summary>
    public static bool IsPilotScene(string sceneName) => sceneName == SceneName;
}

/// <summary>HumanPose getter returns normalized world body data; setter expects normalized root-local data.</summary>
public static class T02PilotPose
{
    public static Vector3 LocalBodyPosition(Vector3 normalizedWorld, Vector3 rootPosition, Quaternion rootRotation, Vector3 rootScale, float humanScale)
    {
        var inverse = new Quaternion(-rootRotation.x, -rootRotation.y, -rootRotation.z, rootRotation.w);
        Vector3 p = inverse * (normalizedWorld * humanScale - rootPosition);
        return new Vector3(p.x / rootScale.x, p.y / rootScale.y, p.z / rootScale.z) / humanScale;
    }

    public static Quaternion LocalBodyRotation(Quaternion world, Quaternion root) =>
        new Quaternion(-root.x, -root.y, -root.z, root.w) * world;
}

/// <summary>One deliberate world pointer owns a pilot drag until release/cancel.</summary>
public sealed class T02PilotOrbitGesture
{
    private int? owner;
    private Vector2 last;
    public bool Begin(int id, Vector2 point, bool overControls)
    {
        if (owner.HasValue || overControls) return false;
        owner = id; last = point; return true;
    }
    public Vector2 Move(int id, Vector2 point)
    {
        if (owner != id) return Vector2.zero;
        Vector2 delta = point - last; last = point; return delta;
    }
    public void End(int id) { if (owner == id) owner = null; }
    public void Cancel() => owner = null;
}

/// <summary>One beat of Arthur's demonstration. Durations are seconds of pilot time (speed/pause applied by the fixture).</summary>
public enum T02PilotBeat
{
    Idle, WalkIn, Settle, Point, Reach, Approach, Grip, Lift, Carry, Lower, Release, Back, Retract, Hold, WalkOut
}

public struct T02PilotStep
{
    public T02PilotBeat Beat;
    public float Seconds;
    /// <summary>0 = holder A (start), 1 = holder B (place target).</summary>
    public int From;
    public int To;
    public T02PilotStep(T02PilotBeat beat, float seconds, int from = 0, int to = 0) { Beat = beat; Seconds = seconds; From = from; To = to; }
    public override string ToString() => $"{Beat} {Seconds:0.##}s {From}->{To}";
}

public static class T02PilotPlan
{
    /// <summary>The step index the loop returns to after the first pass (walking in again, no reset or teleport).</summary>
    public const int LoopStart = 1;

    /// <summary>
    /// Walk in through the doorway, stop, point at the folder in holder A, reach, hook-grip, lift, carry,
    /// lower into holder B, release, retract; then the same back into A; walk out. Walking beats end on arrival.
    /// </summary>
    public static List<T02PilotStep> Build()
    {
        var steps = new List<T02PilotStep>
        {
            new T02PilotStep(T02PilotBeat.Idle, 1.2f),
            new T02PilotStep(T02PilotBeat.WalkIn, 0f),
            new T02PilotStep(T02PilotBeat.Settle, 0.6f),
        };
        AddTransfer(steps, 0, 1, 1.3f);
        steps.Add(new T02PilotStep(T02PilotBeat.Hold, 1.2f));
        AddTransfer(steps, 1, 0, 0.9f);
        steps.Add(new T02PilotStep(T02PilotBeat.Hold, 0.8f));
        steps.Add(new T02PilotStep(T02PilotBeat.WalkOut, 0f));
        steps.Add(new T02PilotStep(T02PilotBeat.Hold, 1.0f));
        return steps;
    }

    private static void AddTransfer(List<T02PilotStep> steps, int from, int to, float point)
    {
        steps.Add(new T02PilotStep(T02PilotBeat.Point, point, from, to));
        steps.Add(new T02PilotStep(T02PilotBeat.Reach, 0.9f, from, to));
        steps.Add(new T02PilotStep(T02PilotBeat.Approach, 0.4f, from, to));
        steps.Add(new T02PilotStep(T02PilotBeat.Grip, 0.35f, from, to));
        steps.Add(new T02PilotStep(T02PilotBeat.Lift, 0.6f, from, to));
        steps.Add(new T02PilotStep(T02PilotBeat.Carry, 1.1f, from, to));
        steps.Add(new T02PilotStep(T02PilotBeat.Lower, 0.6f, from, to));
        steps.Add(new T02PilotStep(T02PilotBeat.Release, 0.35f, from, to));
        steps.Add(new T02PilotStep(T02PilotBeat.Back, 0.3f, from, to));
        steps.Add(new T02PilotStep(T02PilotBeat.Retract, 0.8f, from, to));
    }

    /// <summary>The folder is in the hand from the end of Grip until the start of Release (inclusive of the beats between).</summary>
    public static bool Holding(T02PilotBeat beat) => beat == T02PilotBeat.Lift || beat == T02PilotBeat.Carry || beat == T02PilotBeat.Lower;

    /// <summary>Beats whose hand target is driven by IK (walking and idle leave the arm to its clip).</summary>
    public static bool ArmActive(T02PilotBeat beat) => beat >= T02PilotBeat.Point && beat <= T02PilotBeat.Retract;

    public static int Next(int index, int count) => index + 1 < count ? index + 1 : LoopStart;

    /// <summary>Smooth 0..1 easing (no overshoot), the same curve every bay uses.</summary>
    public static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}

/// <summary>Where Arthur stands so both grips are inside a comfortable fraction of his real arm length.</summary>
public static class T02PilotReach
{
    /// <summary>
    /// The station depth (feet z, facing +z) for a shoulder at shoulderLateral/shoulderHeight relative to the feet:
    /// the deepest position whose farther grip needs at most reachFraction of armLength, but never closer to the
    /// surface front than clearance. Returns the depth and the resulting worst reach ratio.
    /// </summary>
    public static float StationDepth(Vector3 gripA, Vector3 gripB, float stationX, float shoulderLateral, float shoulderHeight, float shoulderForward,
        float armLength, float reachFraction, float surfaceFrontZ, float bodyFront, float clearance, out float worstRatio)
    {
        float limit = Mathf.Max(0.05f, armLength * reachFraction);
        float maxZ = surfaceFrontZ - clearance - bodyFront;
        float best = maxZ - 1.5f;
        for (float z = maxZ; z >= maxZ - 1.5f; z -= 0.005f)
        {
            Vector3 shoulder = new Vector3(stationX + shoulderLateral, shoulderHeight, z + shoulderForward);
            float need = Mathf.Max(Vector3.Distance(shoulder, gripA), Vector3.Distance(shoulder, gripB));
            if (need <= limit) { best = z; break; }
        }
        Vector3 at = new Vector3(stationX + shoulderLateral, shoulderHeight, best + shoulderForward);
        worstRatio = armLength > 0f ? Mathf.Max(Vector3.Distance(at, gripA), Vector3.Distance(at, gripB)) / armLength : float.PositiveInfinity;
        return best;
    }
}

/// <summary>Frame-time samples over a fixed window; percentiles for the pilot's measurement sheet (measured, never assumed).</summary>
public sealed class T02PilotFrameStats
{
    private readonly float[] samples;
    private int count, next;

    public T02PilotFrameStats(int capacity) { samples = new float[Math.Max(8, capacity)]; }

    public int Count => count;

    public void Add(float milliseconds)
    {
        if (float.IsNaN(milliseconds) || float.IsInfinity(milliseconds) || milliseconds < 0f) return;
        samples[next] = milliseconds;
        next = (next + 1) % samples.Length;
        if (count < samples.Length) count++;
    }

    public void Clear() { count = 0; next = 0; }

    /// <summary>Nearest-rank percentile (0..100) of the current window; 0 when empty.</summary>
    public float Percentile(float p)
    {
        if (count == 0) return 0f;
        var copy = new float[count];
        Array.Copy(samples, copy, count);
        Array.Sort(copy);
        int rank = Mathf.Clamp(Mathf.CeilToInt(p / 100f * count) - 1, 0, count - 1);
        return copy[rank];
    }

    public float Max
    {
        get { float m = 0f; for (int i = 0; i < count; i++) m = Mathf.Max(m, samples[i]); return m; }
    }

    /// <summary>Frames slower than the given budget (e.g. 33.3 ms at 30 FPS) in the window.</summary>
    public int Over(float milliseconds)
    {
        int n = 0;
        for (int i = 0; i < count; i++) if (samples[i] > milliseconds) n++;
        return n;
    }
}
