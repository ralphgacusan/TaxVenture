using System;

/// <summary>Speech stays inside its authored shot; an audio-device delay holds the picture, not a word.</summary>
public static class ChapterCinematicVoiceRules
{
    public const float TailSeconds = 0.25f;

    public static bool Matches(ChapterCinematicCue cue, string id, string speaker, string text, string variant) =>
        cue != null && !string.IsNullOrEmpty(cue.voice) &&
        string.Equals(cue.voice, id, StringComparison.Ordinal) &&
        string.Equals(cue.speaker, speaker, StringComparison.Ordinal) &&
        string.Equals(cue.text, text, StringComparison.Ordinal) &&
        string.Equals(cue.variant ?? "", variant ?? "", StringComparison.Ordinal);

    public static bool Fits(ChapterCinematicCue cue, double seconds) =>
        cue != null && seconds > 0 && !double.IsNaN(seconds) &&
        seconds <= cue.end - cue.start - TailSeconds;

    // Clamp before the subtitle's closing fade. Normal takes finish well before this;
    // only a delayed native audio start can hold the master presentation here.
    public static double LimitStep(double time, double requested, ChapterCinematicCue cue,
        bool playing, bool voiceEnabled, bool finishing, float cueFade)
    {
        if (cue == null || !playing || !voiceEnabled || finishing) return requested;
        double holdAt = Math.Max(cue.start, cue.end - Math.Max(0.15f, cueFade));
        return Math.Min(requested, Math.Max(0, holdAt - time));
    }
}
