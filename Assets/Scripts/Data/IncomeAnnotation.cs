using System;
using System.Collections.Generic;

/// <summary>A deliberate sweep in either direction, independent of frame rate and pointer speed.</summary>
public sealed class HighlighterSweep
{
    public string LineId { get; private set; }
    public bool Locked { get; private set; }
    public float Start { get; private set; }
    public float End { get; private set; }
    public float Min { get; private set; }
    public float Max { get; private set; }
    public bool Reverse { get; private set; }
    public float Coverage { get; private set; }
    public bool Complete => Coverage >= 0.22f;

    public void Begin(float x)
    {
        LineId = null;
        Locked = false;
        Start = End = Min = Max = x;
        Reverse = false;
        Coverage = 0f;
    }

    /// <summary>Allow aiming across rows until a short deliberate sweep locks the selected row.</summary>
    public bool Aim(string lineId, float x)
    {
        if (string.IsNullOrWhiteSpace(lineId) || (Locked && LineId != lineId)) return false;
        if (LineId == lineId) return true;
        Begin(x);
        LineId = lineId;
        return true;
    }

    public bool CanFinish(bool blocked, bool canceled) => LineId != null && Locked && Complete && !blocked && !canceled;

    public void Move(float x, float lineWidth, bool allowLock = true)
    {
        if (lineWidth <= 0f) return;
        // Ink is retained when the pointer moves back over the same section.
        if (x != End) Reverse = x < End;
        End = x;
        Min = Math.Min(Min, x);
        Max = Math.Max(Max, x);
        Coverage = Math.Min(1f, (Max - Min) / lineWidth);
        if (allowLock && Coverage >= 0.06f) Locked = true;
    }
}

/// <summary>The three note places on an annotated record. Values may be saved: never renumber.</summary>
public enum AnnotationSlot
{
    Type = 0,
    Treatment = 1,
    Evidence = 2,
    Highlight = 3
}

/// <summary>
/// What the player has put on one annotated record: the highlighted source
/// line, the TYPE note, the TREATMENT note and the clipped evidence
/// references. Their own work, never an answer key.
/// </summary>
[Serializable]
public class AnnotationEntryState
{
    public string entryId;
    public string highlightLineId;
    public string typeId;
    public string treatmentId;
    public List<string> evidenceIds = new List<string>();

    public bool IsComplete =>
        !string.IsNullOrEmpty(highlightLineId) && !string.IsNullOrEmpty(typeId) &&
        !string.IsNullOrEmpty(treatmentId) && evidenceIds != null && evidenceIds.Count > 0;

    public string Get(AnnotationSlot slot) =>
        slot == AnnotationSlot.Type ? typeId : slot == AnnotationSlot.Treatment ? treatmentId : null;

    public bool HasEvidence(string id) => evidenceIds != null && evidenceIds.Contains(id);
}

/// <summary>
/// PURPOSE:
/// The single set of annotation actions shared by the toolkit tutorial (T05)
/// and the scored challenge (T06). Pure C#, so the regression suite tests it
/// outside Unity. The 3D toolkit only renders the state and calls these.
///
/// RULES:
/// - A note only fits its own kind of place: TYPE cards in the TYPE slot,
///   TREATMENT cards in the TREATMENT slot, record references on the clip.
///   A fitting place says nothing about whether the tax answer is right.
/// - Placing a note in a filled slot replaces it (the old card goes back).
/// - The evidence clip holds up to the entry's maxEvidence references; a full
///   single clip swaps its reference.
/// - Only lines printed on the record can be highlighted.
/// Every action reports whether something actually changed.
/// </summary>
public static class AnnotationActions
{
    public static AnnotationEntryState Ensure(ref AnnotationEntryState state, AnnotationEntryDefinition entry)
    {
        if (state == null || state.entryId != entry?.id)
            state = new AnnotationEntryState { entryId = entry?.id };
        if (state.evidenceIds == null) state.evidenceIds = new List<string>();
        return state;
    }

    public static bool Highlight(AnnotationEntryState state, AnnotationEntryDefinition entry, string lineId)
    {
        if (state == null || entry == null || entry.lines == null || string.IsNullOrEmpty(lineId)) return false;
        if (!entry.lines.Exists(l => l != null && l.id == lineId)) return false;
        if (state.highlightLineId == lineId) return false;
        state.highlightLineId = lineId;
        return true;
    }

    /// <summary>True when an option of <paramref name="kind"/> may be put in <paramref name="slot"/>.</summary>
    public static bool Fits(AnnotationSlot kind, AnnotationSlot slot) => kind == slot;

    /// <summary>
    /// Puts a note of <paramref name="kind"/> into <paramref name="slot"/>.
    /// False when it does not fit there, the option is unknown, or nothing changes.
    /// <paramref name="displaced"/> is the note that came back off the paper (or null).
    /// </summary>
    public static bool Place(AnnotationEntryState state, AnnotationDefinition options, AnnotationEntryDefinition entry,
        AnnotationSlot kind, AnnotationSlot slot, string optionId, out string displaced)
    {
        displaced = null;
        if (state == null || options == null || entry == null || !Fits(kind, slot)) return false;
        if (options.FindOption(kind, optionId, entry) == null) return false;
        switch (slot)
        {
            case AnnotationSlot.Type:
                if (state.typeId == optionId) return false;
                displaced = state.typeId;
                state.typeId = optionId;
                return true;
            case AnnotationSlot.Treatment:
                if (state.treatmentId == optionId) return false;
                displaced = state.treatmentId;
                state.treatmentId = optionId;
                return true;
            default:
                if (state.evidenceIds == null) state.evidenceIds = new List<string>();
                if (state.evidenceIds.Contains(optionId)) return false;
                int max = Math.Max(1, entry.maxEvidence);
                if (state.evidenceIds.Count >= max)
                {
                    displaced = state.evidenceIds[0];
                    state.evidenceIds.RemoveAt(0);
                }
                state.evidenceIds.Add(optionId);
                return true;
        }
    }

    /// <summary>Takes a note back off the paper. False when it was not there.</summary>
    public static bool Remove(AnnotationEntryState state, AnnotationSlot slot, string optionId)
    {
        if (state == null || string.IsNullOrEmpty(optionId)) return false;
        switch (slot)
        {
            case AnnotationSlot.Type:
                if (state.typeId != optionId) return false;
                state.typeId = null;
                return true;
            case AnnotationSlot.Treatment:
                if (state.treatmentId != optionId) return false;
                state.treatmentId = null;
                return true;
            default:
                return state.evidenceIds != null && state.evidenceIds.Remove(optionId);
        }
    }

    /// <summary>Tutorial: the player's work repeats everything the guide demonstrated.</summary>
    public static bool MatchesDemonstration(AnnotationEntryState state, AnnotationEntryDefinition entry)
    {
        AnnotationDemoDefinition demo = entry?.demo;
        if (state == null || demo == null || !demo.IsSet || !state.IsComplete) return false;
        if (state.highlightLineId != demo.highlightLineId || state.typeId != demo.typeId ||
            state.treatmentId != demo.treatmentId) return false;
        if (state.evidenceIds.Count != demo.evidenceIds.Count) return false;
        foreach (string id in demo.evidenceIds)
            if (!state.evidenceIds.Contains(id)) return false;
        return true;
    }

    /// <summary>The next concrete correction in practice, instead of leaving a full but incorrect sheet silent.</summary>
    public static string PracticeHint(AnnotationEntryState state, AnnotationDefinition options, AnnotationEntryDefinition entry)
    {
        AnnotationDemoDefinition demo = entry?.demo;
        if (demo == null || options == null) return "Practice is not available.";
        if (state == null || state.highlightLineId != demo.highlightLineId) return "Highlight the salary line from the example.";
        if (state.typeId != demo.typeId)
            return "TYPE: choose " + (options.FindOption(AnnotationSlot.Type, demo.typeId, entry)?.label ?? demo.typeId) + ".";
        if (state.treatmentId != demo.treatmentId)
            return "TREATMENT: choose " + (options.FindOption(AnnotationSlot.Treatment, demo.treatmentId, entry)?.label ?? demo.treatmentId) + ".";
        if (!MatchesDemonstration(state, entry))
        {
            string evidence = demo.evidenceIds != null && demo.evidenceIds.Count > 0 ? demo.evidenceIds[0] : null;
            return "EVIDENCE: attach " + (options.FindOption(AnnotationSlot.Evidence, evidence, entry)?.label ?? "the demonstrated record") + ".";
        }
        return "Practice complete.";
    }

    /// <summary>Tutorial: does this one note repeat the demonstration (for the immediate explanation)?</summary>
    public static bool MatchesDemonstration(AnnotationEntryDefinition entry, AnnotationSlot slot, string optionId)
    {
        AnnotationDemoDefinition demo = entry?.demo;
        if (demo == null || string.IsNullOrEmpty(optionId)) return false;
        return slot == AnnotationSlot.Type ? demo.typeId == optionId :
               slot == AnnotationSlot.Treatment ? demo.treatmentId == optionId :
               demo.evidenceIds != null && demo.evidenceIds.Contains(optionId);
    }
}
