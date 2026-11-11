using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Revised T04 (3 November 2026 paper machine): what Bea's table shows once
/// her records are handed over, pure C# so the regression suite checks it
/// without Unity. Nothing here grades, places, saves or awards.
///
/// - Compute: the compact arrangement on her kraft mat, in table space
///   (metres; +X the seated player's right, +Z away from the player): her
///   originals in one stack on the opened envelope (back left), the reading
///   stand (front left), the small paper machine (right) and its review rack
///   along the machine's base. Every area stays inside the mat with a clear
///   margin, none overlaps another, and a small mat scales the whole
///   arrangement down together (never one part over another). Each piece
///   turns a little toward the seat; placement reserves that turn. Nothing
///   is lifted toward the camera: no 0.8-screen board, no outdoor calculator.
/// - TableSources: the acquired sources her table holds (B-WHT never: Bea
///   hands it over at the office credit step, T08).
/// - ActiveRow / Face: the stand's card is a view of the first row of the
///   open batch (T05: only the current batch) the saved draft has not filed
///   yet; RackRows are that batch's filed rows (recall tabs). The printed
///   label, the printed amount, the source's own purpose and its issuer,
///   period and reference. Source ids stay out of it.
/// - Stack: which original is out (one at a time, or one deliberate T05
///   Compare pair) and the pile order: the current stand source is on top,
///   followed by its compare partner; a returned original comes back on top.
/// - Machine (T05, 4 November 2026): the paper machine's local state (stand,
///   half-fed, saving, feeding), its pending treatment/Why? reason and the
///   one filing a lever pull may save. Never persisted.
/// - Encounter (T04 buildup, 4 November 2026): the order of Bea's first
///   encounter after her saved handover (presented envelope, opened stack,
///   one record read, her organizer offer, Help Bea), plus where her closed
///   envelope and her sketch lie before she presents it.
/// </summary>
public static class Level3TableStaging
{
    public static bool IntroDue(Level3ChapterCheckpoint state) =>
        state?.Case(Level3ChapterRules.BeaCaseId) != null &&
        Level3ChapterRules.PhaseOf(state) >= Level3Phase.WorksheetDraft &&
        Level3ChapterRules.PhaseOf(state) <= Level3Phase.WorksheetReady &&
        !state.Case(Level3ChapterRules.BeaCaseId).sideQuestIntroSeen;

    public static Level3CommandOutcome AcknowledgeIntro(Level3ChapterCheckpoint state, out string why)
    {
        why = null;
        if (state?.Case(Level3ChapterRules.BeaCaseId)?.sideQuestIntroSeen == true) return Level3CommandOutcome.Duplicate;
        if (!IntroDue(state)) { why = "Talk to Bea before starting her records."; return Level3CommandOutcome.Refused; }
        state.Case(Level3ChapterRules.BeaCaseId).sideQuestIntroSeen = true;
        return Level3CommandOutcome.Applied;
    }

    /// <summary>Clear kraft mat around the arrangement (metres, nominal scale).</summary>
    public const float EdgeMargin = 0.035f;
    /// <summary>The open lane between the left (papers) and right (machine) columns.</summary>
    public const float ColumnGap = 0.06f;
    /// <summary>Between the envelope stack and the stand, front to back.</summary>
    public const float RowGap = 0.015f;
    /// <summary>Between the machine's front and its review rack.</summary>
    public const float RackGap = 0.015f;
    /// <summary>Stand and machine keep this much more of the middle lane open.</summary>
    public const float InnerInset = 0.01f;
    public const float StandShiftLeft = 0.025f;

    // Nominal footprints (scale 1). The stand's depth follows its card leaning back at StandTilt.
    public const float EnvelopeWidth = 0.3f;
    public const float EnvelopeDepth = 0.22f;
    public const float StandWidth = Level3PaperMachineModel.CardWidth + 0.03f;
    public const float StandDepth = 0.22f;
    /// <summary>T05: wide enough for the stand's 0.24 m card to feed without shrinking (body 0.34 m) plus the lever.</summary>
    public const float MachineWidth = 0.38f;
    /// <summary>Bring the wider control face toward the middle lane without moving the reading stand.</summary>
    public const float MachineShiftLeft = 0.075f;
    public const float MachineDepth = 0.2f;
    public const float RackWidth = 0.30f;
    public const float RackDepth = 0.055f;
    /// <summary>The width of a sheet in the stack (cards and slips keep their own proportions).</summary>
    public const float StackPaperWidth = 0.18f;
    /// <summary>The seated eye above the table top, and how far behind the mat's near edge it sits.</summary>
    public const float SeatHeight = 0.6f;
    public const float SeatBehindMat = 0.18f;
    /// <summary>Stand, envelope and machine turn toward the seat at most this far (placement reserves that turn).</summary>
    public const float MaxYaw = 10f;
    /// <summary>T04 buildup: before she presents it, Bea's closed envelope lies nearer her and the middle, turned toward her.</summary>
    public const float BeaShift = 0.05f;
    public const float BeaReach = 0.06f;
    public const float BeaTurn = 28f;
    /// <summary>T04 buildup: her sketch (a logo draft and a pencil) behind the machine, on her side of the mat.</summary>
    public const float SketchWidth = 0.12f;
    public const float SketchDepth = 0.085f;

    public struct Layout
    {
        /// <summary>Uniform scale of the whole arrangement (up to 1.2 where the mat permits).</summary>
        public float Scale;
        public Vector2 Mat;
        public Rect Envelope;
        public Rect Stand;
        /// <summary>The machine and its rack together: one rigid piece (the rack sits along the machine's base).</summary>
        public Rect Column;
        public Rect Machine;
        public Rect Rack;
        public float PaperWidth;
        /// <summary>Degrees about Y turning each part's front (-Z) toward the seat (the column turns about its own centre).</summary>
        public float EnvelopeYaw;
        public float StandYaw;
        public float MachineYaw;
        public Vector3 Seat;

        /// <summary>Each piece's axis-aligned footprint once turned toward the seat.</summary>
        public IEnumerable<Rect> Footprints
        {
            get
            {
                yield return Footprint(Envelope, EnvelopeYaw);
                yield return Footprint(Stand, StandYaw);
                yield return Footprint(Column, MachineYaw);
            }
        }
    }

    /// <summary>The seated viewpoint in table space (y above the table top): behind the mat's near edge and past the table's own edge.</summary>
    public static Vector3 SeatOf(Vector2 mat, float tableHalfDepth) =>
        new Vector3(0f, SeatHeight, -Mathf.Max(tableHalfDepth + 0.1f, mat.y * 0.5f + SeatBehindMat));

    /// <summary>
    /// Areas are Rects in the mat's x/z plane (Rect.y is z); the mat is centred on the origin.
    /// Each piece is placed by the footprint it has turned the most it may turn (MaxYaw),
    /// so its real footprint toward any seat stays inside its own place.
    /// </summary>
    public static Layout Compute(Vector2 mat, Vector3 seat)
    {
        float m = EdgeMargin;
        float columnWidth = Mathf.Max(0.01f, (mat.x - ColumnGap) * 0.5f - m);
        float usableDepth = Mathf.Max(0.01f, mat.y - m * 2f);
        float rightDepth = MachineDepth + RackGap + RackDepth;
        Vector2 envelope = Turned(EnvelopeWidth, EnvelopeDepth), stand = Turned(StandWidth, StandDepth);
        Vector2 column = Turned(Mathf.Max(MachineWidth, RackWidth), rightDepth);
        float s = Mathf.Min(1.2f, Mathf.Min(
            Mathf.Min(columnWidth / Mathf.Max(envelope.x, stand.x + InnerInset + StandShiftLeft), usableDepth / (envelope.y + RowGap + Mathf.Max(stand.y, column.y))),
            Mathf.Min(columnWidth / (column.x + InnerInset), usableDepth / column.y)));

        float leftInner = -ColumnGap * 0.5f, rightInner = ColumnGap * 0.5f;
        float back = mat.y * 0.5f - m, front = -mat.y * 0.5f + m;
        var layout = new Layout { Scale = s, Mat = mat, PaperWidth = StackPaperWidth * s, Seat = seat };
        // Put originals in the rear centre gap; keep the machine forward so neither upright prop hides the stack.
        // Stand remains near the middle lane; enlarge all props together only within the available mat.
        var standCentre = new Vector2(leftInner - (stand.x * 0.5f + InnerInset + StandShiftLeft) * s, front + stand.y * s * 0.5f);
        layout.Stand = Area(standCentre, StandWidth * s, StandDepth * s);
        var envelopeCentre = new Vector2(-mat.x * 0.5f + m + envelope.x * s * 0.5f, back - envelope.y * s * 0.5f);
        layout.Envelope = Area(envelopeCentre, EnvelopeWidth * s, EnvelopeDepth * s);
        // Right: machine and rack centred front to back (rack in front, at the machine's base).
        var columnCentre = new Vector2(rightInner + (column.x * 0.5f + InnerInset - MachineShiftLeft) * s,
            front + column.y * s * 0.5f);
        layout.Column = Area(columnCentre, Mathf.Max(MachineWidth, RackWidth) * s, rightDepth * s);
        layout.Machine = Area(new Vector2(columnCentre.x, columnCentre.y + (rightDepth - MachineDepth) * s * 0.5f), MachineWidth * s, MachineDepth * s);
        layout.Rack = Area(new Vector2(columnCentre.x, columnCentre.y - (rightDepth - RackDepth) * s * 0.5f), RackWidth * s, RackDepth * s);
        layout.EnvelopeYaw = YawToward(layout.Envelope.center, seat);
        layout.StandYaw = YawToward(layout.Stand.center, seat);
        layout.MachineYaw = YawToward(layout.Column.center, seat);
        return layout;
    }

    /// <summary>The axis-aligned size of a width x depth piece turned MaxYaw (the most it may turn).</summary>
    private static Vector2 Turned(float width, float depth) => Footprint(new Rect(0f, 0f, width, depth), MaxYaw).size;

    private static Rect Area(Vector2 centre, float width, float depth) => new Rect(centre.x - width * 0.5f, centre.y - depth * 0.5f, width, depth);

    /// <summary>Degrees about Y that turn a part's front (-Z) at centre (x, z) toward the seat, clamped to MaxYaw.</summary>
    public static float YawToward(Vector2 centre, Vector3 seat)
    {
        float dx = seat.x - centre.x, dz = seat.z - centre.y;
        if (dx * dx + dz * dz < 1e-6f) return 0f;
        return Mathf.Clamp(Mathf.Atan2(-dx, -dz) * Mathf.Rad2Deg, -MaxYaw, MaxYaw);
    }

    /// <summary>The axis-aligned box around an area turned yaw degrees about its centre.</summary>
    public static Rect Footprint(Rect area, float yaw)
    {
        float a = yaw * Mathf.Deg2Rad, c = Mathf.Abs(Mathf.Cos(a)), sn = Mathf.Abs(Mathf.Sin(a));
        float halfX = c * area.width * 0.5f + sn * area.height * 0.5f, halfZ = sn * area.width * 0.5f + c * area.height * 0.5f;
        return new Rect(area.center.x - halfX, area.center.y - halfZ, halfX * 2f, halfZ * 2f);
    }

    /// <summary>True when the area lies inside the mat with at least margin metres of mat around it.</summary>
    public static bool Inside(Rect area, Vector2 mat, float margin) =>
        area.xMin >= -mat.x * 0.5f + margin - 1e-5f && area.xMax <= mat.x * 0.5f - margin + 1e-5f &&
        area.yMin >= -mat.y * 0.5f + margin - 1e-5f && area.yMax <= mat.y * 0.5f - margin + 1e-5f;

    /// <summary>
    /// T04 buildup: where Bea keeps her closed envelope before presenting it (centre, and its yaw):
    /// a little toward the middle and toward her, turned her way, never past the table's back edge.
    /// Presenting slides it from here to its place on the mat (Layout.Envelope) in one short move.
    /// </summary>
    public static Vector2 EnvelopeWithBea(Layout layout, float tableHalfDepth, out float yaw)
    {
        yaw = layout.EnvelopeYaw + BeaTurn;
        Rect turned = Footprint(layout.Envelope, yaw);
        float room = Mathf.Max(0f, tableHalfDepth - 0.01f - turned.yMax);
        return layout.Envelope.center + new Vector2(BeaShift * layout.Scale, Mathf.Min(BeaReach * layout.Scale, room));
    }

    /// <summary>
    /// T04 buildup: Bea's sketch lies behind the machine on her side of the mat (inside it, clear of every
    /// staged part); an empty Rect when a small mat leaves no room (then no sketch is laid out).
    /// </summary>
    public static Rect SketchArea(Layout layout)
    {
        float s = layout.Scale;
        Rect column = Footprint(layout.Column, layout.MachineYaw);
        Vector2 size = Turned(SketchWidth, SketchDepth) * s;
        float back = layout.Mat.y * 0.5f - EdgeMargin, front = column.yMax + RowGap * s;
        if (back - front < size.y) return new Rect();
        return Area(new Vector2(layout.Mat.x * 0.5f - EdgeMargin - size.x * 0.5f, back - size.y * 0.5f), SketchWidth * s, SketchDepth * s);
    }

    /// <summary>T04 buildup: the record brought forward when the envelope opens: the first worksheet row's original (R1, logo work) that is on the table.</summary>
    public static string FeaturedSource(IList<Level3WorksheetRow> rows, IList<string> onTable)
    {
        if (onTable == null || onTable.Count == 0) return null;
        if (rows != null)
            foreach (string rowId in Level3WorksheetRules.RowIds)
                foreach (Level3WorksheetRow row in rows)
                    if (row != null && row.RowId == rowId && onTable.Contains(row.SourceId)) return row.SourceId;
        return onTable[0];
    }

    // =========================================================
    // WHAT THE TABLE HOLDS
    // =========================================================

    /// <summary>The acquired sources on Bea's table, in acquisition order, without her certificate (T08) and without repeats.</summary>
    public static List<string> TableSources(IEnumerable<string> acquired)
    {
        var sources = new List<string>();
        if (acquired == null) return sources;
        foreach (string id in acquired)
            if (!string.IsNullOrEmpty(id) && id != Level3ChapterRules.BeaCreditSource && !sources.Contains(id)) sources.Add(id);
        return sources;
    }

    /// <summary>
    /// The first row of the OPEN batch (T05: only the current batch is available) the saved draft has not
    /// filed (a treatment, plus the Why? reason on R6/R7); null when that batch is filed, every batch is
    /// accepted or there are no rows. A later batch is never peeked. Reads only.
    /// </summary>
    public static Level3WorksheetRow ActiveRow(Level3ChapterCheckpoint chapter, IList<Level3WorksheetRow> rows)
    {
        if (rows == null) return null;
        Level3WorksheetDraft draft = chapter?.worksheet;
        foreach (string rowId in Level3WorksheetRules.RowsInBatch(Level3WorksheetRules.OpenBatch(chapter)))
        {
            Level3WorksheetRow row = RowOf(rows, rowId);
            if (row != null && !Level3WorksheetRules.RowFiled(draft, rowId)) return row;
        }
        return null;
    }

    /// <summary>T05: the current batch's filed rows, one recall tab each (at most three), in batch order. Reads only.</summary>
    public static List<Level3WorksheetRow> RackRows(Level3ChapterCheckpoint chapter, IList<Level3WorksheetRow> rows)
    {
        var filed = new List<Level3WorksheetRow>();
        if (rows == null) return filed;
        foreach (string rowId in Level3WorksheetRules.RowsInBatch(Level3WorksheetRules.OpenBatch(chapter)))
        {
            Level3WorksheetRow row = RowOf(rows, rowId);
            if (row != null && Level3WorksheetRules.RowFiled(chapter?.worksheet, rowId)) filed.Add(row);
        }
        return filed;
    }

    public static Level3WorksheetRow RowOf(IList<Level3WorksheetRow> rows, string rowId)
    {
        if (rows == null || rowId == null) return null;
        foreach (Level3WorksheetRow row in rows)
            if (row != null && row.RowId == rowId) return row;
        return null;
    }

    /// <summary>"Record 1 of 2": a row's place in its batch (it describes the work, never the answer); "" for anything else.</summary>
    public static string BatchPosition(string rowId)
    {
        string batch = Level3WorksheetRules.BatchOf(rowId);
        if (batch == null) return "";
        int index = 0, count = 0;
        foreach (string id in Level3WorksheetRules.RowIds)
        {
            if (Level3WorksheetRules.BatchOf(id) != batch) continue;
            count++;
            if (id == rowId) index = count;
        }
        return "Record " + index + " of " + count;
    }

    public struct CardFace
    {
        public string Position;
        public string Title;
        public string Amount;
        public string Purpose;
        public string Details;
    }

    /// <summary>The stand card of a row: every value is the case content's own printed text (no source id, no treatment).</summary>
    public static CardFace Face(CaseDefinition definition, Level3WorksheetRow row)
    {
        var face = new CardFace();
        if (row == null) return face;
        Level3CaseContent content = definition?.level3;
        Level3SourceDefinition source = content?.sources?.Find(s => s != null && s.sourceId == row.SourceId);
        DocumentDefinition document = source != null ? definition.documents?.Find(d => d != null && d.documentName == source.documentName) : null;
        DocumentFieldDefinition field = document?.fields?.Find(f => f != null && f.semanticKey == row.FieldKey);
        face.Position = BatchPosition(row.RowId);
        face.Title = row.Label ?? "";
        face.Amount = !string.IsNullOrEmpty(field?.displayValue) ? field.displayValue : "₱" + Level3Calculator.Format(row.Amount);
        face.Purpose = source != null ? Sentence(source.status) : "";
        if (source != null)
        {
            string period = source.periodStart == source.periodEnd ? source.periodStart
                : content.taxYear > 0 ? content.taxYear.ToString() : source.periodStart + " to " + source.periodEnd;
            var parts = new List<string>();
            foreach (string part in new[] { source.issuer, period, source.reference })
                if (!string.IsNullOrWhiteSpace(part)) parts.Add(part.Trim());
            face.Details = string.Join(" · ", parts);
        }
        else face.Details = "";
        return face;
    }

    private static string Sentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string t = text.Trim();
        return char.ToUpperInvariant(t[0]) + t.Substring(1);
    }

    // =========================================================
    // ONE ORIGINAL AT A TIME
    // =========================================================

    /// <summary>The pile of originals (top first) and the one that is out. Opening never adds or removes a source.</summary>
    public sealed class Stack
    {
        private readonly List<string> order = new List<string>();

        public Stack(IEnumerable<string> sources)
        {
            foreach (string id in TableSources(sources)) order.Add(id);
        }

        /// <summary>Top of the pile first. An original keeps its place while it is out.</summary>
        public IReadOnlyList<string> Order => order;
        public int Count => order.Count;
        /// <summary>The one original being read, or null.</summary>
        public string Out { get; private set; }

        /// <summary>The top original still resting on the pile.</summary>
        public string Top
        {
            get
            {
                foreach (string id in order) if (id != Out && id != Partner) return id;
                return null;
            }
        }

        public void Prioritize(string first, string second = null)
        {
            if (Contains(second)) { order.Remove(second); order.Insert(0, second); }
            if (Contains(first)) { order.Remove(first); order.Insert(0, first); }
        }

        public bool Contains(string sourceId) => sourceId != null && order.Contains(sourceId);

        /// <summary>
        /// The original sourceId comes out without changing its pile order. closing is
        /// the one that was out before and must go back first (null if none).
        /// False for a source not on the table or the one already out.
        /// </summary>
        public bool Open(string sourceId, out string closing)
        {
            closing = null;
            if (!Contains(sourceId) || (sourceId == Out && Partner == null)) return false;
            // A single read ends a Compare pair: its other paper goes back too (the table puts back both).
            closing = Out != sourceId ? Out : Partner;
            Partner = null;
            Out = sourceId;
            return true;
        }

        /// <summary>T05 Compare: the second original read beside Out (null when none). Only OpenPair sets it.</summary>
        public string Partner { get; private set; }

        /// <summary>
        /// T05 Compare: two originals out together (first beside second, e.g. B-PRINT with B-EXP).
        /// closing lists what was out before and must go back first. False when either is not on the
        /// table, they are the same, or that pair is already out.
        /// </summary>
        public bool OpenPair(string first, string second, out List<string> closing)
        {
            closing = new List<string>();
            if (!Contains(first) || !Contains(second) || first == second || (Out == first && Partner == second)) return false;
            if (Out != null && Out != first && Out != second) closing.Add(Out);
            if (Partner != null && Partner != first && Partner != second) closing.Add(Partner);
            Out = first;
            Partner = second;
            return true;
        }

        /// <summary>Its paper is back on the pile (no-op for any other source). A Compare pair ends when either returns.</summary>
        public void Close(string sourceId)
        {
            if (sourceId == null) return;
            if (sourceId == Out) { Out = Partner; Partner = null; }
            else if (sourceId == Partner) Partner = null;
            Prioritize(sourceId);
        }

        /// <summary>Everything gathered (leaving the table, an interruption): nothing is out; the pile order stays.</summary>
        public void CloseAll()
        {
            Out = null;
            Partner = null;
        }
    }

    // =========================================================
    // THE FIRST ENCOUNTER (T04 buildup)
    // =========================================================

    /// <summary>The next step of Bea's first encounter at her table. The order is fixed; nothing is skipped or repeated within a session.</summary>
    public enum EncounterStep
    {
        /// <summary>Her records are not handed over (she still has her envelope).</summary>
        Waiting,
        /// <summary>Saved handover: the closed envelope lies in reach ("Open Bea's records").</summary>
        Presented,
        /// <summary>The stack is out with one record forward ("Inspect a record").</summary>
        Opened,
        /// <summary>That record was read and put back: Bea's organizer offer is due.</summary>
        Inspected,
        /// <summary>She introduced her organizer (Help Bea / Later): her seat is open.</summary>
        Offered,
        /// <summary>The player sat to help, or the chapter was restored with her records already handed over.</summary>
        Ready
    }

    /// <summary>
    /// T04 buildup (4 November 2026): Bea's first encounter after her saved
    /// handover, as one forward-only order: Presented -> Opened -> Inspected
    /// -> Offered -> Ready. Presentation only and this session only: nothing
    /// here saves, acquires, grades, files, rewards or starts her review clock.
    /// The saved chapter decides the start: Waiting before the handover; with
    /// her records already acquired (a reload, a legacy or progressed save) an
    /// encounter that never started this session is Ready, a concise
    /// continuation that replays no introduction, presentation or acquisition.
    /// </summary>
    public sealed class Encounter
    {
        private bool featuredShown;

        public EncounterStep Step { get; private set; } = EncounterStep.Waiting;
        /// <summary>Her introduction was heard to the end once (a failed handover save retries with its last line only).</summary>
        public bool IntroHeard { get; private set; }
        /// <summary>The record brought forward when the envelope opens (null until she presents it).</summary>
        public string Featured { get; private set; }

        /// <summary>The introduction played out (whether or not its handover then saved).</summary>
        public void HeardIntroduction() => IntroHeard = true;

        /// <summary>Follows the saved chapter. Not handed over: Waiting. Handed over: Ready unless this session's encounter is already under way.</summary>
        public void Sync(bool handedOver)
        {
            if (!handedOver)
            {
                Step = EncounterStep.Waiting;
                Featured = null;
                featuredShown = false;
            }
            else if (Step == EncounterStep.Waiting) Step = EncounterStep.Ready;
        }

        /// <summary>Right after this session's saved handover: the envelope is presented. False on any other step (a restore never presents).</summary>
        public bool Present(string featured)
        {
            if (Step != EncounterStep.Waiting || string.IsNullOrEmpty(featured)) return false;
            Step = EncounterStep.Presented;
            Featured = featured;
            featuredShown = false;
            return true;
        }

        /// <summary>The player opens the presented envelope.</summary>
        public bool Open()
        {
            if (Step != EncounterStep.Presented) return false;
            Step = EncounterStep.Opened;
            return true;
        }

        /// <summary>A paper came to rest in the reader. Only the featured record, after the envelope opened, counts.</summary>
        public void Shown(string sourceId)
        {
            if (Step == EncounterStep.Opened && sourceId != null && sourceId == Featured) featuredShown = true;
        }

        /// <summary>A paper is back on the pile. The featured record, once it was shown, completes the inspection (true: her offer is due).</summary>
        public bool Returned(string sourceId)
        {
            if (Step != EncounterStep.Opened || !featuredShown || sourceId != Featured) return false;
            Step = EncounterStep.Inspected;
            return true;
        }

        /// <summary>Her organizer offer is on screen (Help Bea / Later).</summary>
        public bool Offer()
        {
            if (Step != EncounterStep.Inspected) return false;
            Step = EncounterStep.Offered;
            return true;
        }

        /// <summary>The player sits to help (Help Bea, or her table after Later). Never before her offer.</summary>
        public bool Accept()
        {
            if (Step != EncounterStep.Offered && Step != EncounterStep.Ready) return false;
            Step = EncounterStep.Ready;
            return true;
        }

        /// <summary>Her offer was made (or skipped by a restore): the seat and the continuation open.</summary>
        public bool Done => Step >= EncounterStep.Offered;
        /// <summary>The seat target is live: before the handover (it says to talk to her first) and once the encounter is done.</summary>
        public bool SeatOpen => Step == EncounterStep.Waiting || Done;
        /// <summary>The envelope lies open with the stack out (any step after the player opened it, and every restore).</summary>
        public bool EnvelopeOpen => Step >= EncounterStep.Opened;
        /// <summary>The featured record lies forward of the pile, waiting to be inspected.</summary>
        public bool FeaturedForward => Step == EncounterStep.Opened;
        /// <summary>The stand shows the current record only in the seated workspace's turn (after Help Bea, or a restore).</summary>
        public bool CardShown => Step == EncounterStep.Ready;
    }

    // =========================================================
    // THE PAPER MACHINE (T05): local presentation state, never saved
    // =========================================================

    /// <summary>Where the machine's one record is (SIDE_QUEST_MACHINE state table). Local presentation only: restores derive it from the saved draft.</summary>
    public enum MachineStep
    {
        /// <summary>No record to work on (none presented yet, or the open batch is filed).</summary>
        Empty,
        /// <summary>A record's card is on the stand: read, inspect, feed it (drag, or tap card then slot).</summary>
        Ready,
        /// <summary>Half-fed: choose a treatment (and Why? on R6/R7), pull the lever, or back out. Nothing is saved yet.</summary>
        Loaded,
        /// <summary>The lever was pulled past its threshold (or tapped): exactly one filing is being saved.</summary>
        Saving,
        /// <summary>Saved: the cosmetic full feed plays. An interruption settles it; nothing is filed again.</summary>
        Feeding
    }

    /// <summary>
    /// T05 (4 November 2026): the paper machine's local state, pure C# (the
    /// regression suite drives it). It only decides which mechanical action is
    /// possible now: half-feeding, turning the selector and choosing a reason
    /// are pending choices; the lever produces at most one Level3Filing at a
    /// time (Saving, then Feeding blocks every other action); a failed save
    /// keeps the record loaded with the same choices. A new record starts
    /// neutral; a recalled (or partly filed) record starts at its saved
    /// choices. Nothing here saves, grades or compares a choice with an answer.
    /// </summary>
    public sealed class Machine
    {
        public MachineStep Step { get; private set; } = MachineStep.Empty;
        /// <summary>The record on the stand or in the machine (null when Empty).</summary>
        public string Row { get; private set; }
        /// <summary>The stand's card was tapped: the slot shows its neutral outline (tap the slot to feed).</summary>
        public bool Armed { get; private set; }
        /// <summary>The loaded record came back from its review tab: backing out feeds it back in unchanged.</summary>
        public bool Recalled { get; private set; }
        /// <summary>Pending treatment ("" = the selector's neutral home).</summary>
        public string Treatment { get; private set; } = "";
        /// <summary>Pending Why? reason (R6/R7 only; "" = none).</summary>
        public string Reason { get; private set; } = "";
        /// <summary>The saved draft's revision when the record was loaded (the filing's action identity).</summary>
        public int BasedOn { get; private set; }

        /// <summary>Saving or feeding: every other machine action waits (a tap only settles a feed).</summary>
        public bool Busy => Step == MachineStep.Saving || Step == MachineStep.Feeding;
        public bool NeedsReason => Level3WorksheetRules.NeedsReason(Row) || Level3SortingPractice.NeedsReason(Row);
        /// <summary>The Why? picker may be used: R6/R7 loaded with any treatment chosen.</summary>
        public bool CanAskWhy => Step == MachineStep.Loaded && NeedsReason && Treatment != "";
        /// <summary>The lever's one prompt while a choice is missing (null when it may be pulled or nothing is loaded).</summary>
        public string Missing => Step != MachineStep.Loaded ? null
            : Treatment == "" ? Level3WorksheetRules.ChooseCategory
            : NeedsReason && Reason == "" ? Level3WorksheetRules.ChooseReason : null;

        /// <summary>The stand shows this record (null: nothing). Refused while one is loaded, saving or feeding.</summary>
        public bool Present(string rowId)
        {
            if (Busy || Step == MachineStep.Loaded) return false;
            Clear();
            Row = rowId;
            Step = rowId == null ? MachineStep.Empty : MachineStep.Ready;
            return true;
        }

        /// <summary>Tap on the stand's card: arm (or disarm) the slot. Only on the stand.</summary>
        public bool ToggleArm()
        {
            if (Step != MachineStep.Ready) return false;
            Armed = !Armed;
            return true;
        }

        /// <summary>
        /// The stand's card goes halfway in (dragged onto the slot, or card then slot). It
        /// starts at the row's saved choices: neutral for a new row; a partly filed (legacy)
        /// row keeps what was saved. No grading, no totals, no save.
        /// </summary>
        public bool Feed(string savedTreatment, string savedReason, int revision)
        {
            if (Step != MachineStep.Ready || Row == null) return false;
            Load(Row, savedTreatment, savedReason, revision, recalled: false);
            return true;
        }

        /// <summary>A filed record's tab is tapped: it comes back half-fed with its saved choices (kept saved until a replacement saves).</summary>
        public bool Recall(string rowId, string savedTreatment, string savedReason, int revision)
        {
            if (Busy || Step == MachineStep.Loaded || rowId == null || Array.IndexOf(Level3WorksheetRules.Treatments, savedTreatment) < 0) return false;
            Load(rowId, savedTreatment, savedReason, revision, recalled: true);
            return true;
        }

        /// <summary>
        /// Back out before filing (tap the loaded card, leave, an interruption that drops it): pending
        /// choices are dropped, nothing is saved. A new record returns to the stand; a recalled one is
        /// fed back in unchanged (its saved filing still stands). toStand tells the view which.
        /// </summary>
        public bool Withdraw(out bool toStand)
        {
            toStand = false;
            if (Step != MachineStep.Loaded) return false;
            toStand = !Recalled;
            string row = Row;
            Clear();
            Row = toStand ? row : null;
            Step = toStand ? MachineStep.Ready : MachineStep.Empty;
            return true;
        }

        /// <summary>A selector detent (any treatment for any row; no correctness snap). False when unchanged or not loaded.</summary>
        public bool Choose(string treatment)
        {
            if (Step != MachineStep.Loaded || Array.IndexOf(Level3WorksheetRules.Treatments, treatment) < 0 || treatment == Treatment) return false;
            Treatment = treatment;
            return true;
        }

        /// <summary>
        /// The selector stepped one detent (drag): +1 clockwise, -1 the other way. From the neutral home
        /// +1 reaches the first detent and -1 the last; it stops at either end (no cycling).
        /// </summary>
        public string Stepped(int direction)
        {
            string[] t = Level3WorksheetRules.Treatments;
            int at = Array.IndexOf(t, Treatment);
            int next = at < 0 ? (direction > 0 ? 0 : t.Length - 1) : Math.Max(0, Math.Min(t.Length - 1, at + Math.Sign(direction)));
            return t[next];
        }

        /// <summary>A Why? reason (after a treatment, whichever it is). Changing the treatment never picks or clears one silently.</summary>
        public bool ChooseReason(string reason)
        {
            if (!CanAskWhy || Array.IndexOf(Level3WorksheetRules.Reasons, reason) < 0 || reason == Reason) return false;
            Reason = reason;
            return true;
        }

        /// <summary>
        /// The lever, released past its threshold or tapped: the ONE filing to save (Saving), or
        /// false (Missing says why, or the machine is busy / nothing is loaded).
        /// </summary>
        public bool BeginFile(out Level3Filing filing)
        {
            filing = default;
            if (Step != MachineStep.Loaded || Missing != null) return false;
            filing = new Level3Filing { RowId = Row, Treatment = Treatment, Reason = NeedsReason ? Reason : "", BasedOn = BasedOn };
            Step = MachineStep.Saving;
            return true;
        }

        /// <summary>The save's answer. Saved (or already saved exactly as chosen): the feed plays. Not saved: loaded again, same choices, retryable.</summary>
        public void Saved(bool committed)
        {
            if (Step == MachineStep.Saving) Step = committed ? MachineStep.Feeding : MachineStep.Loaded;
        }

        /// <summary>The full feed finished or was settled: nothing is loaded; the table presents the next record. False unless feeding.</summary>
        public bool Fed()
        {
            if (Step != MachineStep.Feeding) return false;
            Clear();
            Step = MachineStep.Empty;
            return true;
        }

        /// <summary>Leaving the table / disable: any uncommitted load is dropped, a feed counts as finished. Never a command.</summary>
        public void Reset()
        {
            Clear();
            Step = MachineStep.Empty;
        }

        private void Load(string row, string savedTreatment, string savedReason, int revision, bool recalled)
        {
            Row = row;
            Step = MachineStep.Loaded;
            Armed = false;
            Recalled = recalled;
            Treatment = Array.IndexOf(Level3WorksheetRules.Treatments, savedTreatment) >= 0 ? savedTreatment : "";
            Reason = Level3WorksheetRules.NeedsReason(row) && Array.IndexOf(Level3WorksheetRules.Reasons, savedReason) >= 0 ? savedReason : "";
            BasedOn = revision;
        }

        private void Clear()
        {
            Row = null;
            Armed = false;
            Recalled = false;
            Treatment = "";
            Reason = "";
            BasedOn = 0;
        }
    }
}
