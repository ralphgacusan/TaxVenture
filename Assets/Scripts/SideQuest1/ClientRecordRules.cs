using System;
using System.Collections.Generic;

/// <summary>One record's place on a folder page (page metres: x right of centre, z above centre; width x height of its face).</summary>
[Serializable]
public struct RecordSlot
{
    public string type;
    public float x, z, width, height;
}

/// <summary>
/// Where attached records sit on a folder's supporting page (T31; T25 gives Marie's
/// ID, background and 2316 their own page with this). Shares of the page: the
/// area below the heading and above the page number, ID cards as wide as
/// cardWidth (1.6 : 1), sheets as wide as sheetWidth (1 : 1.4), stacked top to
/// bottom in the given order with a gap, everything scaled down together when
/// the stack would not fit. Centred, so there is room either side to take them out.
/// </summary>
[Serializable]
public class RecordPageLayout
{
    public float topShare = 0.24f;
    public float bottomShare = 0.85f;
    public float gapShare = 0.035f;
    public float cardWidth = 0.6f;
    public float sheetWidth = 0.5f;
    /// <summary>Neighbouring sheets share one row, side by side (T25: Marie's background beside her 2316).</summary>
    public bool sheetsInRow;
    public const float CardAspect = 1.6f;
    public const float SheetAspect = 1.4f;

    /// <summary>Arthur's Reviewed File (T30): his ID card above his background sheet.</summary>
    public static RecordPageLayout Default => new RecordPageLayout();

    /// <summary>Marie's case (T25): her ID card on top, her two work papers side by side below it.</summary>
    public static RecordPageLayout CaseRecords => new RecordPageLayout { cardWidth = 0.5f, sheetWidth = 0.43f, sheetsInRow = true };
}

/// <summary>
/// PURPOSE:
/// Pure rules for physical supporting records (Campaign Foundation T31): the
/// optional two-second hold that groups taken-out records, the front-to-back
/// order, which records may group (one folder only, stable original order and
/// a slight stagger), returning each grouped record exactly once, focusing one
/// face at a time on a small phone, and the page layout. No Unity: the
/// ClientRecordPaper / AttachedRecordsPage components follow these.
/// </summary>
public static class ClientRecordRules
{
    /// <summary>Seconds a finger must rest still on a taken-out record before grouping begins.</summary>
    public const float GroupHoldSeconds = 2f;
    /// <summary>The progress cue appears only after this much of the hold (an ordinary tap never sees it).</summary>
    public const float HoldCueDelay = 0.35f;
    /// <summary>A screen smaller than this many inches tall focuses one record face at a time.</summary>
    public const float SmallScreenInches = 3.2f;

    public enum HoldPhase { None, Holding, Grouped, Cancelled }

    /// <summary>
    /// The hold's state now: cancelled by moving past the drag threshold, lifting the finger,
    /// a cancelled pointer or lost focus; grouped once the finger rested GroupHoldSeconds.
    /// </summary>
    public static HoldPhase Hold(float heldSeconds, float movedPixels, float dragThreshold, bool pointerDown, bool focused)
    {
        if (!pointerDown || !focused || movedPixels > dragThreshold) return HoldPhase.Cancelled;
        if (heldSeconds >= GroupHoldSeconds) return HoldPhase.Grouped;
        return heldSeconds > 0f ? HoldPhase.Holding : HoldPhase.None;
    }

    /// <summary>0 .. 1 progress shown by the cue (0 before the cue delay).</summary>
    public static float HoldProgress(float heldSeconds) =>
        heldSeconds <= HoldCueDelay ? 0f : Math.Min(1f, (heldSeconds - HoldCueDelay) / (GroupHoldSeconds - HoldCueDelay));

    /// <summary>Front order after touching id: it moves to the front (last), the others keep their order.</summary>
    public static List<string> BringToFront(IEnumerable<string> order, string id)
    {
        var result = new List<string>();
        if (order != null)
            foreach (string item in order)
                if (item != id && !result.Contains(item)) result.Add(item);
        if (!string.IsNullOrEmpty(id)) result.Add(id);
        return result;
    }

    /// <summary>
    /// The records a hold groups: only those out of the same folder as the held one, in the folder's
    /// own (canonical) order. Fewer than two: nothing to group (empty).
    /// </summary>
    public static List<string> GroupMembers(IList<string> canonicalOrder, IDictionary<string, string> ownerOf, ICollection<string> outIds, string heldId)
    {
        var members = new List<string>();
        if (canonicalOrder == null || ownerOf == null || outIds == null || heldId == null || !ownerOf.TryGetValue(heldId, out string owner)) return members;
        foreach (string id in canonicalOrder)
            if (outIds.Contains(id) && ownerOf.TryGetValue(id, out string o) && o == owner && !members.Contains(id)) members.Add(id);
        return members.Count >= 2 ? members : new List<string>();
    }

    /// <summary>The slight stagger of a group (screen fractions of the lead's size), first member at the back.</summary>
    public static float[] Stagger(int count, float step)
    {
        var offsets = new float[Math.Max(0, count)];
        for (int i = 0; i < offsets.Length; i++) offsets[i] = (i - (offsets.Length - 1)) * step;
        return offsets;
    }

    /// <summary>World scale that preserves a paper's apparent size when its camera depth changes.</summary>
    internal static float PreserveApparentScale(float scale, float currentDepth, float targetDepth) =>
        currentDepth > 0f && targetDepth > 0f ? scale * targetDepth / currentDepth : scale;

    /// <summary>A dropped group goes back record by record: each id once, attached ones skipped.</summary>
    public static List<string> ReturnOnce(IEnumerable<string> group, ICollection<string> attached)
    {
        var result = new List<string>();
        if (group == null) return result;
        foreach (string id in group)
            if (!string.IsNullOrEmpty(id) && !result.Contains(id) && (attached == null || !attached.Contains(id))) result.Add(id);
        return result;
    }

    /// <summary>Records of one folder may never go into another folder (gentle refusal, no transfer).</summary>
    public static bool MayAttach(string recordOwner, string folderOwner) =>
        !string.IsNullOrEmpty(recordOwner) && recordOwner == folderOwner;

    /// <summary>A small phone (under SmallScreenInches tall; without a dpi, a narrow screen) reads one face at a time.</summary>
    public static bool FocusOne(float widthPixels, float heightPixels, float dpi)
    {
        if (widthPixels <= 0f || heightPixels <= 0f) return false;
        if (dpi > 0f) return heightPixels / dpi < SmallScreenInches;
        return widthPixels / heightPixels < 1.2f;
    }

    /// <summary>
    /// The records' places on a page width x height (metres), top to bottom in types' order
    /// (ClientRecord.Identity = card, anything else = sheet).
    /// </summary>
    public static List<RecordSlot> LayoutPage(float width, float height, IList<string> types, RecordPageLayout layout)
    {
        var slots = new List<RecordSlot>();
        if (types == null || types.Count == 0 || width <= 0f || height <= 0f) return slots;
        layout = layout ?? RecordPageLayout.Default;
        float top = height * (0.5f - layout.topShare);
        float available = height * (layout.bottomShare - layout.topShare);
        float gap = height * layout.gapShare;
        var sizes = new List<(float w, float h)>();
        foreach (string type in types)
        {
            bool card = type == ClientRecord.Identity;
            float w = width * (card ? layout.cardWidth : layout.sheetWidth);
            float h = card ? w / RecordPageLayout.CardAspect : w * RecordPageLayout.SheetAspect;
            sizes.Add((w, h));
        }
        // Rows top to bottom: each card alone; with sheetsInRow, neighbouring sheets side by side.
        var rows = new List<List<int>>();
        for (int i = 0; i < types.Count; i++)
        {
            bool sheet = types[i] != ClientRecord.Identity;
            List<int> last = rows.Count > 0 ? rows[rows.Count - 1] : null;
            if (layout.sheetsInRow && sheet && last != null && types[last[0]] != ClientRecord.Identity) last.Add(i);
            else rows.Add(new List<int> { i });
        }
        // A row never wider than the page (with a margin); then everything scales together to fit the height.
        var rowScale = new float[rows.Count];
        float total = gap * (rows.Count - 1);
        for (int r = 0; r < rows.Count; r++)
        {
            float rowWidth = gap * (rows[r].Count - 1), rowHeight = 0f;
            foreach (int i in rows[r]) { rowWidth += sizes[i].w; rowHeight = Math.Max(rowHeight, sizes[i].h); }
            rowScale[r] = rowWidth > width * 0.96f ? width * 0.96f / rowWidth : 1f;
            total += rowHeight * rowScale[r];
        }
        float fit = total > available ? Math.Max(0.05f, (available - gap * (rows.Count - 1)) / (total - gap * (rows.Count - 1))) : 1f;
        var placed = new RecordSlot[types.Count];
        float z = top;
        for (int r = 0; r < rows.Count; r++)
        {
            float scale = fit * rowScale[r];
            float rowWidth = gap * (rows[r].Count - 1), rowHeight = 0f;
            foreach (int i in rows[r]) { rowWidth += sizes[i].w * scale; rowHeight = Math.Max(rowHeight, sizes[i].h * scale); }
            float x = -rowWidth * 0.5f;
            foreach (int i in rows[r])
            {
                float w = sizes[i].w * scale, h = sizes[i].h * scale;
                placed[i] = new RecordSlot { type = types[i], x = rows[r].Count == 1 ? 0f : x + w * 0.5f, z = z - h * 0.5f, width = w, height = h };
                x += w + gap;
            }
            z -= rowHeight + gap;
        }
        slots.AddRange(placed);
        return slots;
    }
}
