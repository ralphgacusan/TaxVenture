using System;

/// <summary>
/// PURPOSE:
/// The timing of a staged page turn (PageTurnStage), kept free of Unity so
/// the regression suite can check it at any frame rate. Progress is
/// normalized time (0..1) built from frame steps capped at MaxStep, so a
/// slow frame (a page rebuilt) slows the turn for a moment instead of
/// skipping it, and a turn lasts its duration at every normal frame rate.
/// </summary>
public static class PageTurnMath
{
    /// <summary>Longest single frame step counted (seconds).</summary>
    public const float MaxStep = 0.1f;

    /// <summary>Share of the turn over which the leaf fades (out when landing forward, in when lifting backward).</summary>
    public const float FadeShare = 0.3f;

    /// <summary>Ink starts to fade before the page foreshortens enough to squeeze its text.</summary>
    public const float PrintFadeStartAngle = 34f;

    /// <summary>Ink is fully gone while the page still has a visible face.</summary>
    public const float PrintFadeEndAngle = 76f;

    /// <summary>The frame step counted toward a turn.</summary>
    public static float Step(float unscaledDeltaTime) => Math.Max(0f, Math.Min(unscaledDeltaTime, MaxStep));

    /// <summary>Normalized progress (0..1) after <paramref name="elapsed"/> seconds of a turn lasting <paramref name="duration"/>.</summary>
    public static float Progress(float elapsed, float duration) =>
        duration <= 0f ? 1f : Clamp01(elapsed / duration);

    /// <summary>Smooth start and finish (smoothstep).</summary>
    public static float Ease(float t)
    {
        t = Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// The moving leaf's angle around the spine in degrees: forward turns go
    /// 0 to 180 (the page lifts off to the left), backward turns 180 to 0
    /// (the previous page comes back over the spine).
    /// </summary>
    public static float Angle(int direction, float t) => direction >= 0 ? 180f * Ease(t) : 180f * (1f - Ease(t));

    /// <summary>The opacity of the print on the front face, faded before it becomes edge-on.</summary>
    public static float PrintAlpha(float angle) =>
        1f - SmoothStep((angle - PrintFadeStartAngle) / (PrintFadeEndAngle - PrintFadeStartAngle));

    /// <summary>The leaf still has some visible print on its front face.</summary>
    public static bool FrontVisible(float angle) => PrintAlpha(angle) > 0f;

    /// <summary>The leaf's opacity: a forward leaf fades as it lands, a backward one appears as it lifts.</summary>
    public static float Alpha(int direction, float t)
    {
        t = Clamp01(t);
        return direction >= 0
            ? 1f - SmoothStep((t - (1f - FadeShare)) / FadeShare)
            : SmoothStep(t / FadeShare);
    }

    private static float SmoothStep(float x)
    {
        x = Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : x;
}
