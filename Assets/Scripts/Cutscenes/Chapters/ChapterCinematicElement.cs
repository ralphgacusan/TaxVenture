using UnityEngine;

/// <summary>One frame of a chapter cutscene, as the player hands it to its elements.</summary>
public readonly struct ChapterCinematicFrame
{
    /// <summary>Authored presentation time (seconds, 0..duration).</summary>
    public readonly float Time;
    /// <summary>Black curtain opacity (fades, or the Skip fade).</summary>
    public readonly float Curtain;
    /// <summary>Effects volume scale: 0 when effects are off, eased to 0 under the closing fade.</summary>
    public readonly float AudioScale;
    public readonly bool Paused;

    public ChapterCinematicFrame(float time, float curtain, float audioScale, bool paused)
    {
        Time = time;
        Curtain = curtain;
        AudioScale = audioScale;
        Paused = paused;
    }
}

/// <summary>
/// A scene part driven by ChapterCinematicPlayer from its fixed presentation
/// clock (camera shot, actor, prop move, sound). The player lists its elements
/// explicitly (stable bindings); nothing scans the scene. Elements own only
/// the transforms and components they are bound to.
/// </summary>
public abstract class ChapterCinematicElement : MonoBehaviour
{
    /// <summary>Called once before the first Evaluate.</summary>
    public virtual void Prepare(ChapterCinematicPlayer player) { }

    /// <summary>Applies the authored state at this presentation time (called from the player's Update).</summary>
    public abstract void Evaluate(in ChapterCinematicFrame frame);

    /// <summary>Application focus/suspension: freeze or resume anything that runs on its own clock.</summary>
    public virtual void SetPaused(bool paused) { }

    /// <summary>The presentation finished or is being torn down: stop sounds, release what this element owns.</summary>
    public virtual void Release() { }

    /// <summary>Smootherstep (zero velocity and acceleration at both ends).</summary>
    public static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    /// <summary>Ease-out (decelerating to rest), cubic.</summary>
    public static float EaseOut(float t)
    {
        t = 1f - Mathf.Clamp01(t);
        return 1f - t * t * t;
    }

    /// <summary>0..1 progress of time through [start, end].</summary>
    public static float Progress(float time, float start, float end) =>
        end <= start ? (time >= end ? 1f : 0f) : Mathf.Clamp01((time - start) / (end - start));
}
