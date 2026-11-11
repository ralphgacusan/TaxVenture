using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Data and timing for cutscene subtitles, read from
/// Resources/Cutscenes/*_subtitles.json (see Cutscene01Runtime). Plain C#
/// with no Unity calls, so the regression suite compiles it as is.
///
/// Each line names its existing dialogue AudioClip and optional phrase offsets.
/// ApplyAudioClipTimes follows the actual Timeline audio windows. Legacy clip
/// numbering/ApplyClipTimes remain compatible for older content only.
/// </summary>
[Serializable]
public sealed class CutsceneSubtitleLine
{
    public int clip;
    public string speaker = "";
    /// <summary>Exact existing AudioClip name, independent of track/clip ordinal.</summary>
    public string audioClip = "";
    /// <summary>Source-audio seconds for a phrase; -1 end uses the full audio clip.</summary>
    public double audioStart;
    public double audioEnd = -1d;
    public string text = "";
    public float start;
    public float end;
    /// <summary>True when start / end are a guess, not read from the Timeline.</summary>
    public bool estimate;
    /// <summary>Show as the large centred title card instead of a subtitle.</summary>
    public bool title;
}

/// <summary>An existing Timeline audio clip's authored playback window. No playback ownership.</summary>
public readonly struct CutsceneAudioSpan
{
    public readonly string Name;
    public readonly double Start, Duration, ClipIn, TimeScale, SourceDuration;

    public CutsceneAudioSpan(string name, double start, double duration, double clipIn, double timeScale, double sourceDuration)
    {
        Name = name;
        Start = start;
        Duration = duration;
        ClipIn = clipIn;
        TimeScale = timeScale;
        SourceDuration = sourceDuration;
    }
}

[Serializable]
public sealed class CutsceneSubtitleTrack
{
    public string nextScene = "Office_Level1";
    public float fadeInSeconds = 0.25f;
    public float fadeOutSeconds = 0.25f;
    public bool showSkip = true;
    public bool useTimelineClipTimes = true;
    /// <summary>Timeline track whose clips are numbered; empty = the track with the most clips.</summary>
    public string timelineTrack = "";
    /// <summary>Length used when the scene has no playable Timeline.</summary>
    public float fallbackDurationSeconds = 66f;
    /// <summary>Gap left between consecutive lines that share a clip.</summary>
    public float lineGapSeconds = 0.15f;
    public List<CutsceneSubtitleLine> lines = new List<CutsceneSubtitleLine>();

    /// <summary>
    /// Binds captions to named dialogue audio, not activation-track indices or
    /// equal shares. Source phrase offsets follow trims and playback speed.
    /// Missing/ambiguous/invalid audio leaves every JSON fallback time intact.
    /// </summary>
    public bool ApplyAudioClipTimes(IList<CutsceneAudioSpan> clips)
    {
        if (lines == null || lines.Count == 0 || clips == null) return false;
        var times = new List<(CutsceneSubtitleLine line, float start, float end)>();
        foreach (CutsceneSubtitleLine line in lines)
        {
            if (line == null || string.IsNullOrEmpty(line.audioClip)) return false;
            CutsceneAudioSpan match = default;
            int matches = 0;
            foreach (CutsceneAudioSpan clip in clips)
                if (string.Equals(clip.Name, line.audioClip, StringComparison.Ordinal))
                { match = clip; matches++; }
            if (matches != 1 || !Finite(match.Start) || !Finite(match.Duration) || !Finite(match.ClipIn)
                || !Finite(match.TimeScale) || !Finite(match.SourceDuration) || match.Start < 0 || match.Duration <= 0
                || match.ClipIn < 0 || match.TimeScale <= 0 || match.SourceDuration <= match.ClipIn
                || !Finite(line.audioStart) || !Finite(line.audioEnd) || line.audioStart < 0) return false;
            double sourceEnd = line.audioEnd < 0 ? match.SourceDuration : line.audioEnd;
            if (sourceEnd <= line.audioStart) return false;
            double start = match.Start + Math.Max(0, (line.audioStart - match.ClipIn) / match.TimeScale);
            double end = match.Start + Math.Min(match.Duration,
                (Math.Min(sourceEnd, match.SourceDuration) - match.ClipIn) / match.TimeScale);
            if (end <= start) return false;
            times.Add((line, (float)start, (float)end));
        }
        foreach (var time in times)
        {
            time.line.start = time.start;
            time.line.end = time.end;
            time.line.estimate = false;
        }
        return true;
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    /// <summary>Adapt existing intro timing to the shared chapter cue/fade renderer.</summary>
    public ChapterCinematicScript SubtitlePresentation()
    {
        var presentation = new ChapterCinematicScript { duration = fallbackDurationSeconds };
        if (lines != null)
            foreach (CutsceneSubtitleLine line in lines)
                if (line != null)
                    presentation.cues.Add(new ChapterCinematicCue {
                        speaker = line.speaker, text = line.text, start = line.start, end = line.end });
        return presentation;
    }

    /// <summary>Highest clip number any line refers to.</summary>
    public int HighestClip
    {
        get
        {
            int highest = 0;
            if (lines != null)
                foreach (CutsceneSubtitleLine line in lines)
                    if (line != null && line.clip > highest) highest = line.clip;
            return highest;
        }
    }

    /// <summary>The line showing at this time, or null between lines.</summary>
    public CutsceneSubtitleLine LineAt(double time)
    {
        if (lines == null) return null;
        foreach (CutsceneSubtitleLine line in lines)
            if (line != null && !string.IsNullOrEmpty(line.text) && time >= line.start && time < line.end)
                return line;
        return null;
    }

    /// <summary>
    /// Replaces line times with real clip times. clipStarts / clipEnds are in
    /// Timeline order (index 0 = clip 1). Returns false and changes nothing
    /// when there are fewer clips than the highest clip a line names.
    /// </summary>
    public bool ApplyClipTimes(IList<double> clipStarts, IList<double> clipEnds)
    {
        if (lines == null || clipStarts == null || clipEnds == null) return false;
        int count = Math.Min(clipStarts.Count, clipEnds.Count);
        if (count == 0 || HighestClip > count) return false;

        var byClip = new Dictionary<int, List<CutsceneSubtitleLine>>();
        foreach (CutsceneSubtitleLine line in lines)
        {
            if (line == null || line.clip < 1) continue;
            if (!byClip.TryGetValue(line.clip, out List<CutsceneSubtitleLine> group))
                byClip[line.clip] = group = new List<CutsceneSubtitleLine>();
            group.Add(line);
        }

        foreach (KeyValuePair<int, List<CutsceneSubtitleLine>> pair in byClip)
        {
            double clipStart = clipStarts[pair.Key - 1];
            double clipEnd = Math.Max(clipStart, clipEnds[pair.Key - 1]);
            double share = (clipEnd - clipStart) / pair.Value.Count;
            for (int i = 0; i < pair.Value.Count; i++)
            {
                CutsceneSubtitleLine line = pair.Value[i];
                double start = clipStart + share * i;
                double end = clipStart + share * (i + 1);
                if (i < pair.Value.Count - 1) end = Math.Max(start, end - lineGapSeconds);
                line.start = (float)start;
                line.end = (float)end;
                line.estimate = false;
            }
        }
        return true;
    }
}
