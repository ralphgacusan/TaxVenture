using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// The small shared presentation contract for the chapter cutscenes
/// (Documentation/ChapterCutscenes, T01 onwards). Plain C# with no Unity
/// calls, so the regression suite compiles it unchanged:
///
/// - ChapterCinematicScript: the authored timing data of one scene (stable ID,
///   fixed duration, local fades, shot starts and subtitle cues), read from
///   Assets/Cutscenes/ChapterNN/*_script.json by ChapterCinematicPlayer.
/// - ChapterCinematicSession: the fixed-duration presentation clock and the
///   once-only finish (natural end, Skip, or abort).
/// - ChapterCinematicResult: what the caller receives exactly once.
///
/// DOES NOT: load scenes, write saves, award anything or decide progression.
/// The caller (T04) maps Completed/Skipped to its own continuation.
/// </summary>
public enum ChapterCinematicOutcome
{
    /// <summary>The presentation reached its authored end (t = duration).</summary>
    Completed,
    /// <summary>The viewer tapped Skip (or Android Back); same continuation as Completed.</summary>
    Skipped,
    /// <summary>Torn down before finishing (scene unloaded, quit). Not a watched result.</summary>
    Aborted
}

/// <summary>Delivered once per playback through ChapterCinematicPlayer.Finished.</summary>
public readonly struct ChapterCinematicResult
{
    public readonly string Id;
    public readonly ChapterCinematicOutcome Outcome;
    /// <summary>Presentation time (seconds) when the finish was decided.</summary>
    public readonly double PresentationTime;

    public ChapterCinematicResult(string id, ChapterCinematicOutcome outcome, double presentationTime)
    {
        Id = id;
        Outcome = outcome;
        PresentationTime = presentationTime;
    }

    /// <summary>Completed and Skipped advance the caller's continuation; Aborted does not.</summary>
    public bool Watched => Outcome != ChapterCinematicOutcome.Aborted;

    public override string ToString() => $"{Id} {Outcome} at {PresentationTime:0.00}s";
}

/// <summary>Read-only presentation context a caller may hand over before loading the scene.</summary>
public sealed class ChapterCinematicContext
{
    public static readonly ChapterCinematicContext Default = new ChapterCinematicContext("");

    /// <summary>Cue variant to show (empty = only cues without a variant). T02's relationship-aware lines use it.</summary>
    public readonly string Variant;

    public ChapterCinematicContext(string variant)
    {
        Variant = variant ?? "";
    }
}

[Serializable]
public sealed class ChapterCinematicCue
{
    /// <summary>Compact speaker name shown before the line.</summary>
    public string speaker = "";
    public string text = "";
    /// <summary>Optional prerecorded line ID; missing clips leave the subtitle intact.</summary>
    public string voice = "";
    public float start;
    public float end;
    /// <summary>Empty: always shown. Otherwise only when the context's Variant matches.</summary>
    public string variant = "";

    public bool AppliesTo(string requestedVariant) =>
        string.IsNullOrEmpty(variant) || string.Equals(variant, requestedVariant ?? "", StringComparison.Ordinal);

    /// <summary>Characters the viewer reads (speaker prefix included).</summary>
    public int ReadLength => (text ?? "").Length + (string.IsNullOrEmpty(speaker) ? 0 : speaker.Length + 2);
}

[Serializable]
public sealed class ChapterCinematicScript
{
    public const float RequiredDuration = 15f;
    public const float MinCharsPerSecond = 4f;
    public const float MaxCharsPerSecond = 17f;
    public const int MaxLines = 2;

    public string id = "";
    public string title = "";
    public string scene = "";
    public float duration = RequiredDuration;
    public float fadeInSeconds = 0.25f;
    public float fadeOutSeconds = 0.25f;
    /// <summary>Opacity ease at each cue's start and end, counted inside the cue.</summary>
    public float cueFadeSeconds = 0.1f;
    /// <summary>Characters per subtitle line used for the two-line budget (38 pt at 1920x1080, 1180 px wide).</summary>
    public int charsPerLine = 46;
    /// <summary>Shot start times in order; the first is 0.</summary>
    public float[] shotStarts = new float[0];
    public List<ChapterCinematicCue> cues = new List<ChapterCinematicCue>();

    /// <summary>The cue on screen at this time for the variant, or null.</summary>
    public ChapterCinematicCue CueAt(double time, string variant)
    {
        if (cues == null) return null;
        foreach (ChapterCinematicCue cue in cues)
            if (cue != null && cue.AppliesTo(variant) && !string.IsNullOrEmpty(cue.text) && time >= cue.start && time < cue.end)
                return cue;
        return null;
    }

    /// <summary>Cue opacity: eased in over cueFadeSeconds after start, out before end.</summary>
    public float CueAlpha(ChapterCinematicCue cue, double time)
    {
        if (cue == null || time < cue.start || time >= cue.end) return 0f;
        float fade = Math.Max(1e-3f, cueFadeSeconds);
        double fromStart = (time - cue.start) / fade;
        double toEnd = (cue.end - time) / fade;
        return (float)Math.Max(0d, Math.Min(1d, Math.Min(fromStart, toEnd)));
    }

    /// <summary>Authored black curtain: 1 at t=0 easing to 0 by fadeIn, 0 until duration-fadeOut, then to 1 at duration.</summary>
    public float CurtainAlpha(double time)
    {
        if (time <= 0d) return 1f;
        if (time >= duration) return 1f;
        if (fadeInSeconds > 0f && time < fadeInSeconds) return 1f - Smooth((float)(time / fadeInSeconds));
        double closeStart = duration - fadeOutSeconds;
        if (fadeOutSeconds > 0f && time > closeStart) return Smooth((float)((time - closeStart) / fadeOutSeconds));
        return 0f;
    }

    /// <summary>Index of the shot playing at this time (0-based; -1 without shots).</summary>
    public int ShotAt(double time)
    {
        if (shotStarts == null || shotStarts.Length == 0) return -1;
        int index = 0;
        for (int i = 0; i < shotStarts.Length; i++)
            if (time >= shotStarts[i]) index = i;
        return index;
    }

    /// <summary>Estimated wrapped line count of a cue at charsPerLine (word wrap).</summary>
    public int EstimatedLines(ChapterCinematicCue cue)
    {
        if (cue == null) return 0;
        string full = string.IsNullOrEmpty(cue.speaker) ? cue.text ?? "" : cue.speaker + "  " + cue.text;
        int width = Math.Max(8, charsPerLine);
        int lines = 1, current = 0;
        foreach (string word in full.Split(' '))
        {
            if (word.Length == 0) { current++; continue; }
            int needed = current == 0 ? word.Length : current + 1 + word.Length;
            if (needed > width && current > 0) { lines++; current = word.Length; }
            else current = needed;
        }
        return lines;
    }

    /// <summary>Every authoring rule this scene breaks (empty = valid). See PRODUCTION_STANDARD §3 and §6.</summary>
    public List<string> Validate()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(id)) problems.Add("Missing stable id.");
        if (Math.Abs(duration - RequiredDuration) > 1e-4f) problems.Add($"Duration is {duration} s; chapter scenes last exactly {RequiredDuration} s.");
        if (fadeInSeconds < 0f || fadeOutSeconds < 0f || fadeInSeconds + fadeOutSeconds >= duration) problems.Add("Fades must be non-negative and shorter than the scene.");

        if (shotStarts == null || shotStarts.Length == 0) problems.Add("No shots.");
        else
        {
            if (Math.Abs(shotStarts[0]) > 1e-4f) problems.Add("The first shot must start at 0.");
            for (int i = 1; i < shotStarts.Length; i++)
                if (shotStarts[i] <= shotStarts[i - 1] || shotStarts[i] >= duration) problems.Add($"Shot {i + 1} start {shotStarts[i]} is out of order or past the end.");
        }

        float readableFrom = fadeInSeconds;
        float readableTo = duration - fadeOutSeconds;
        var spans = new List<ChapterCinematicCue>();
        if (cues != null)
        {
            foreach (ChapterCinematicCue cue in cues)
            {
                if (cue == null) { problems.Add("Null cue."); continue; }
                string label = $"Cue \"{Trim(cue.text)}\"";
                if (string.IsNullOrWhiteSpace(cue.text)) problems.Add("A cue has no text.");
                if (string.IsNullOrWhiteSpace(cue.speaker)) problems.Add($"{label} has no speaker.");
                if (cue.end <= cue.start) problems.Add($"{label} ends before it starts.");
                if (cue.start < readableFrom || cue.end > readableTo) problems.Add($"{label} ({cue.start}-{cue.end}) is outside the readable interval {readableFrom}-{readableTo}.");
                float seconds = cue.end - cue.start;
                if (seconds > 0f)
                {
                    float cps = cue.ReadLength / seconds;
                    if (cps > MaxCharsPerSecond) problems.Add($"{label} reads at {cps:0.0} chars/s (max {MaxCharsPerSecond}).");
                    if (cps < MinCharsPerSecond) problems.Add($"{label} reads at {cps:0.0} chars/s (lingers; min {MinCharsPerSecond}).");
                }
                if (EstimatedLines(cue) > MaxLines) problems.Add($"{label} wraps to more than {MaxLines} lines.");
                if (cue.text != null && cue.text.IndexOf('\n') >= 0) problems.Add($"{label} contains a manual line break.");
                foreach (ChapterCinematicCue other in spans)
                    if (Overlaps(cue, other) && SameAudience(cue, other)) problems.Add($"{label} overlaps \"{Trim(other.text)}\".");
                spans.Add(cue);
            }
        }
        return problems;
    }

    private static bool Overlaps(ChapterCinematicCue a, ChapterCinematicCue b) => a.start < b.end && b.start < a.end;

    private static bool SameAudience(ChapterCinematicCue a, ChapterCinematicCue b) =>
        string.IsNullOrEmpty(a.variant) || string.IsNullOrEmpty(b.variant) || a.variant == b.variant;

    private static string Trim(string text) => text == null ? "" : text.Length <= 24 ? text : text.Substring(0, 24) + "...";

    /// <summary>Smoothstep ease (0..1).</summary>
    public static float Smooth(float t)
    {
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        return t * t * (3f - 2f * t);
    }
}

/// <summary>
/// The presentation clock and its once-only finish. Advance with real
/// seconds (clamped so a hitch or a resume never jumps over a subtitle);
/// a paused session does not advance. Natural end: the authored fade-out
/// reaches black at t = duration. Skip: fades from the current opacity to
/// black over fadeOutSeconds of real time. The outcome is decided once and
/// reported once (TakeFinished).
/// </summary>
public sealed class ChapterCinematicSession
{
    /// <summary>Longest real step applied per frame (seconds).</summary>
    public const double MaxStep = 1d / 15d;

    private readonly ChapterCinematicScript script;
    private double skipElapsed;
    private float skipFromAlpha;
    private bool reported;

    public ChapterCinematicSession(ChapterCinematicScript script)
    {
        this.script = script ?? throw new ArgumentNullException(nameof(script));
    }

    public ChapterCinematicScript Script => script;
    /// <summary>Authored presentation time, 0..duration.</summary>
    public double Time { get; private set; }
    public bool Paused { get; set; }
    /// <summary>A finish (natural, skip or abort) has been decided.</summary>
    public bool Finishing { get; private set; }
    /// <summary>The curtain is fully black after the finish; the result can be delivered.</summary>
    public bool Done { get; private set; }
    public ChapterCinematicOutcome Outcome { get; private set; }
    /// <summary>Presentation time at which the finish was decided.</summary>
    public double FinishTime { get; private set; }
    public bool SkipRequested => Finishing && Outcome == ChapterCinematicOutcome.Skipped;

    /// <summary>Black curtain opacity for this moment (authored fades, or the Skip fade).</summary>
    public float CurtainAlpha
    {
        get
        {
            if (Finishing && Outcome != ChapterCinematicOutcome.Completed)
            {
                if (Outcome == ChapterCinematicOutcome.Aborted) return 1f;
                double fade = Math.Max(1e-3, script.fadeOutSeconds);
                double t = Math.Min(1d, skipElapsed / fade);
                return skipFromAlpha + (1f - skipFromAlpha) * (float)t;
            }
            return script.CurtainAlpha(Time);
        }
    }

    /// <summary>Skip is offered once the opening fade has cleared and nothing has finished.</summary>
    public bool SkipAvailable => !Finishing && Time >= script.fadeInSeconds;

    /// <summary>Advances by real seconds (clamped). Returns true on the frame the result becomes Done.</summary>
    public bool Advance(double realSeconds)
    {
        if (Done || Paused) return false;
        double step = Math.Max(0d, Math.Min(MaxStep, realSeconds));

        if (Finishing)
        {
            if (Outcome == ChapterCinematicOutcome.Skipped)
            {
                skipElapsed += step;
                // The scene keeps moving under the closing fade, never past its end.
                Time = Math.Min(script.duration, Time + step);
                if (skipElapsed >= Math.Max(1e-3, script.fadeOutSeconds)) Done = true;
            }
            return Done;
        }

        Time = Math.Min(script.duration, Time + step);
        if (Time >= script.duration)
        {
            Finishing = true;
            Outcome = ChapterCinematicOutcome.Completed;
            FinishTime = script.duration;
            Done = true;
        }
        return Done;
    }

    /// <summary>Requests the Skip finish. Ignored before the opening fade clears or after any finish.</summary>
    public bool RequestSkip()
    {
        if (!SkipAvailable) return false;
        skipFromAlpha = script.CurtainAlpha(Time);
        skipElapsed = 0d;
        Finishing = true;
        Outcome = ChapterCinematicOutcome.Skipped;
        FinishTime = Time;
        return true;
    }

    /// <summary>Tears the presentation down without a watched result. Ignored after any finish.</summary>
    public bool Abort()
    {
        if (Finishing) return false;
        Finishing = true;
        Outcome = ChapterCinematicOutcome.Aborted;
        FinishTime = Time;
        Done = true;
        return true;
    }

    /// <summary>
    /// Teardown: an unfinished presentation becomes Aborted; a Skip whose fade
    /// is still running completes as Skipped. Returns true when this made it Done.
    /// </summary>
    public bool Teardown()
    {
        if (Done) return false;
        if (!Finishing) return Abort();
        Done = true;
        return true;
    }

    /// <summary>True exactly once, when the finished result should be delivered.</summary>
    public bool TakeFinished(out ChapterCinematicResult result)
    {
        if (!Done || reported)
        {
            result = default;
            return false;
        }
        reported = true;
        result = new ChapterCinematicResult(script.id, Outcome, FinishTime);
        return true;
    }
}
