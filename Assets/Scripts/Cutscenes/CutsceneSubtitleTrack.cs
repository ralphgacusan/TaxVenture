using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Data and timing for cutscene subtitles, read from
/// Resources/Cutscenes/*_subtitles.json (see Cutscene01Runtime). Plain C#
/// with no Unity calls, so the regression suite compiles it as is.
///
/// Each line names the cutscene clip it belongs to (1-based, as counted on
/// the Timeline) and carries its own start / end seconds. When the scene's
/// Timeline can be read at runtime, ApplyClipTimes replaces those seconds
/// with the real clip times: the lines of one clip share that clip evenly,
/// in the order they are listed.
/// </summary>
[Serializable]
public sealed class CutsceneSubtitleLine
{
    public int clip;
    public string text = "";
    public float start;
    public float end;
    /// <summary>True when start / end are a guess, not read from the Timeline.</summary>
    public bool estimate;
    /// <summary>Show as the large centred title card instead of a subtitle.</summary>
    public bool title;
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
