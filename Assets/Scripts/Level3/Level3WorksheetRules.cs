using System;
using System.Collections.Generic;

/// <summary>A worksheet row the player can lift from an original (resolved from the case content).</summary>
public sealed class Level3WorksheetRow
{
    public string RowId;
    public string SourceId;
    public string FieldKey;
    public string Label;
    public string Amount;
    public string TransactionId;
}

/// <summary>
/// T05: one pull of the machine's lever: the row, its chosen treatment, the Why?
/// reason (R6/R7 only, else empty) and the draft revision the choice was made on.
/// A value, never saved as such: Level3WorksheetRules.File writes it into the draft.
/// </summary>
public struct Level3Filing
{
    public string RowId;
    public string Treatment;
    public string Reason;
    public int BasedOn;
}

/// <summary>
/// PURPOSE:
/// Bea's worksheet draft (T05, "Worth the Tax?"), pure C# so the regression
/// suite drives the same commands the desk calls. Every command runs on a
/// COPY of the chapter snapshot (the caller writes, then publishes):
///
/// - Extract: lift row R1-R7 from its own source as a strip (the row id,
///   source id and printed amount are copied, never typed). A second lift
///   of the same row is a Duplicate (the desk focuses the existing strip);
///   a row from another case or not a worksheet row is Refused with a reason.
/// - Place: put a lifted strip into Income, Allowable expenses, Not
///   deductible or Supporting only. Every section accepts every strip: no
///   placement is judged here, so nothing leaks the answer.
/// - SetReason / SetRelated: the two claim notes (party purchases R6,
///   printing bill R7) take one of five neutral reasons; the printing note
///   also links any other worksheet row. Both notes are always editable,
///   wherever their strips are.
/// - Undo: puts the previous value of the last edit back.
/// Membership changes of Income/Allowable expenses mark their subtotal dirty
/// (T06's calculator tapes clear it). Grading, mastery and rewards are T06.
/// </summary>
public static class Level3WorksheetRules
{
    public const string Income = "income";
    public const string Expenses = "expenses";
    public const string NotDeductible = "not-deductible";
    public const string Supporting = "supporting";
    public static readonly string[] Sections = { Income, Expenses, NotDeductible, Supporting };

    public const string PartyNote = "note-R6";
    public const string PrintingNote = "note-R7";
    public static readonly string[] Notes = { PartyNote, PrintingNote };

    /// <summary>The seven strips (FIXTURE_CONTRACT: only R1-R7 are worksheet rows).</summary>
    public static readonly string[] RowIds = { "R1", "R2", "R3", "R4", "R5", "R6", "R7" };

    /// <summary>Neutral reason candidates, all offered on both notes (CONTENT).</summary>
    public static readonly string[] Reasons = { "business-purpose", "personal-purpose", "already-represented", "different-period", "need-support" };

    public static string SectionLabel(string section)
    {
        switch (section)
        {
            case Income: return "Income";
            case Expenses: return "Allowable expenses";
            case NotDeductible: return "Not deductible";
            case Supporting: return "Supporting only";
            default: return "Not placed";
        }
    }

    // ---------- Revision (3 November 2026): three destinations, three batches ----------
    // The saved draft keeps its four-section keys (lossless: Leave Out keeps
    // which reason section it came from); a revised view reads them through
    // these maps. Nothing here grades a row.

    public const string Deduct = "deduct";
    public const string LeaveOut = "leave-out";
    public const string EarningsBatch = "earnings";
    public const string CostsBatch = "costs";
    public const string QuestionsBatch = "questions";
    public static readonly string[] Batches = { EarningsBatch, CostsBatch, QuestionsBatch };

    /// <summary>A saved section -> Income / Deduct / Leave Out ("" while not placed or unknown).</summary>
    public static string RevisedDestination(string section)
    {
        switch (section)
        {
            case Income: return Income;
            case Expenses: return Deduct;
            case NotDeductible:
            case Supporting: return LeaveOut;
            default: return "";
        }
    }

    /// <summary>The batch a worksheet row is presented in (R1-R2, R3-R5, R6-R7); null for anything else. It describes the work, never the answer.</summary>
    public static string BatchOf(string rowId)
    {
        switch (rowId)
        {
            case "R1": case "R2": return EarningsBatch;
            case "R3": case "R4": case "R5": return CostsBatch;
            case "R6": case "R7": return QuestionsBatch;
            default: return null;
        }
    }

    // ---------- T05 (revised, 4 November 2026): the paper machine's filing ----------
    // Read -> half-feed -> select -> Why? (R6/R7) -> lever. Loading, turning the
    // selector and choosing a reason are presentation only; the lever files one
    // row's treatment and (R6/R7) its reason together in ONE command (File),
    // through the existing CommitWorksheet seam. Every treatment is accepted for
    // every row: nothing here compares a choice with an answer.

    /// <summary>The selector's three detents, in their fixed order (Income / Deduct / Leave Out).</summary>
    public static readonly string[] Treatments = { Income, Deduct, LeaveOut };

    public static string TreatmentLabel(string treatment)
    {
        switch (treatment)
        {
            case Income: return "Income";
            case Deduct: return "Deduct";
            case LeaveOut: return "Leave Out";
            default: return "";
        }
    }

    /// <summary>The claim note whose reason a row's filing writes (R6 party purchases, R7 printing bill); null for R1-R5.</summary>
    public static string NoteForRow(string rowId) => rowId == "R6" ? PartyNote : rowId == "R7" ? PrintingNote : null;

    /// <summary>R6/R7 ask Why? after any treatment (never only after the expected one).</summary>
    public static bool NeedsReason(string rowId) => NoteForRow(rowId) != null;

    /// <summary>
    /// The saved section key for a treatment. Lossless: a row already left out
    /// keeps whichever Leave Out section it had (a legacy "supporting" stays).
    /// </summary>
    public static string SectionFor(string treatment, string currentSection)
    {
        switch (treatment)
        {
            case Income: return Income;
            case Deduct: return Expenses;
            case LeaveOut: return currentSection == NotDeductible || currentSection == Supporting ? currentSection : NotDeductible;
            default: return null;
        }
    }

    private static readonly string[] EarningsRows = { "R1", "R2" };
    private static readonly string[] CostsRows = { "R3", "R4", "R5" };
    private static readonly string[] QuestionsRows = { "R6", "R7" };

    /// <summary>A batch's rows in presentation order (empty for an unknown batch).</summary>
    public static IReadOnlyList<string> RowsInBatch(string batch) =>
        batch == EarningsBatch ? EarningsRows : batch == CostsBatch ? CostsRows : batch == QuestionsBatch ? QuestionsRows : (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>
    /// A batch the deliberate check has accepted (T06 writes worksheetCheck.acceptedBatches),
    /// or every batch once the worksheet is ready or collected. Accepted rows are read-only.
    /// </summary>
    public static bool BatchAccepted(Level3ChapterCheckpoint state, string batch)
    {
        if (state == null || Array.IndexOf(Batches, batch) < 0) return false;
        if (Level3ChapterRules.PhaseOf(state) >= Level3Phase.WorksheetReady) return true;
        return state.worksheetCheck?.acceptedBatches != null && state.worksheetCheck.acceptedBatches.Contains(batch);
    }

    /// <summary>The one batch the machine works on: the first not yet accepted (null once all are). Later batches stay closed.</summary>
    public static string OpenBatch(Level3ChapterCheckpoint state)
    {
        foreach (string batch in Batches)
            if (!BatchAccepted(state, batch)) return batch;
        return null;
    }

    /// <summary>The row belongs to the open batch (it may be fed, recalled and filed now).</summary>
    public static bool RowOpen(Level3ChapterCheckpoint state, string rowId)
    {
        string batch = BatchOf(rowId);
        return batch != null && batch == OpenBatch(state);
    }

    /// <summary>The saved treatment of a row ("" while unfiled). Reads only.</summary>
    public static string SavedTreatment(Level3WorksheetDraft draft, string rowId) => RevisedDestination(draft?.Strip(rowId)?.section);

    /// <summary>The saved Why? reason of R6/R7 ("" when none or for R1-R5). Reads only.</summary>
    public static string SavedReason(Level3WorksheetDraft draft, string rowId)
    {
        string note = NoteForRow(rowId);
        string reason = note != null ? draft?.Note(note)?.reason : null;
        return reason != null && Array.IndexOf(Reasons, reason) >= 0 ? reason : "";
    }

    /// <summary>The row has a saved treatment, and (R6/R7) a saved reason. Structural only, never a grade.</summary>
    public static bool RowFiled(Level3WorksheetDraft draft, string rowId) =>
        SavedTreatment(draft, rowId) != "" && (!NeedsReason(rowId) || SavedReason(draft, rowId) != "");

    /// <summary>T06 seam: every row of the batch is filed (Check batch may become the primary action). Says nothing about correctness.</summary>
    public static bool BatchFiled(Level3ChapterCheckpoint state, string batch)
    {
        IReadOnlyList<string> rows = RowsInBatch(batch);
        if (state == null || rows.Count == 0) return false;
        foreach (string row in rows)
            if (!RowFiled(state.worksheet, row)) return false;
        return true;
    }

    /// <summary>All seven rows filed with both reasons (the revised "drafted"; no link choice in the machine).</summary>
    public static bool AllFiled(Level3WorksheetDraft draft)
    {
        if (draft == null) return false;
        foreach (string row in RowIds)
            if (!RowFiled(draft, row)) return false;
        return true;
    }

    /// <summary>
    /// The machine's Compare partner: the source of another worksheet row with the same
    /// transaction (R5 and R7 share PRINT-26), read from the case content; null when none.
    /// Offered on both rows of such a pair, so it never singles out an answer.
    /// </summary>
    public static string ComparePartner(IList<Level3WorksheetRow> rows, string rowId)
    {
        Level3WorksheetRow own = Find(rows, rowId);
        if (own == null || string.IsNullOrEmpty(own.TransactionId)) return null;
        foreach (Level3WorksheetRow row in rows)
            if (row != null && row.RowId != own.RowId && Array.IndexOf(RowIds, row.RowId) >= 0 &&
                row.TransactionId == own.TransactionId && row.SourceId != own.SourceId) return row.SourceId;
        return null;
    }

    /// <summary>
    /// The two compact windows: exact decimal sums of the saved slips' own amounts by their
    /// current treatment, wrong choices included (no hidden correction). Only batches the
    /// machine has shown count (accepted or open); a half-fed choice is never in a saved slip.
    /// </summary>
    public static void DraftTotals(Level3ChapterCheckpoint state, out decimal income, out decimal deductions)
    {
        income = 0m;
        deductions = 0m;
        Level3WorksheetDraft draft = state?.worksheet;
        if (draft?.strips == null) return;
        string open = OpenBatch(state);
        foreach (Level3StripRecord strip in draft.strips)
        {
            string batch = strip != null ? BatchOf(strip.rowId) : null;
            if (batch == null || (batch != open && !BatchAccepted(state, batch))) continue;
            if (!decimal.TryParse(strip.amount, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal amount)) continue;
            string treatment = RevisedDestination(strip.section);
            if (treatment == Income) income += amount;
            else if (treatment == Deduct) deductions += amount;
        }
    }

    public static string ReasonLabel(string reason)
    {
        switch (reason)
        {
            case "business-purpose": return "Business purpose";
            case "personal-purpose": return "Personal purpose";
            case "already-represented": return "Already represented";
            case "different-period": return "Different reporting period";
            case "need-support": return "Need supporting record";
            default: return "";
        }
    }

    /// <summary>The claim a note is about (B-DRAFT's claim rows) and the strip it concerns.</summary>
    public static string NoteClaimRow(string noteId) => noteId == PartyNote ? "B-CLAIM-PERSONAL" : noteId == PrintingNote ? "B-CLAIM-PRINT" : null;
    public static string NoteStrip(string noteId) => noteId == PartyNote ? "R6" : noteId == PrintingNote ? "R7" : null;
    public static bool NoteHasRelated(string noteId) => noteId == PrintingNote;

    /// <summary>Rows the printing note may link to: any other worksheet row (offered equally).</summary>
    public static List<string> RelatedCandidates(string noteId)
    {
        var rows = new List<string>();
        if (!NoteHasRelated(noteId)) return rows;
        string own = NoteStrip(noteId);
        foreach (string row in RowIds) if (row != own) rows.Add(row);
        return rows;
    }

    /// <summary>The source papers the worksheet rows come from, in tab order, with a short name for each tab (it describes the paper, never its treatment).</summary>
    public static readonly string[] SourceTabs = { "B-REV", "B-EXP", "B-PERSONAL", "B-PRINT" };

    public static string SourceTabLabel(string sourceId)
    {
        switch (sourceId)
        {
            case "B-REV": return "Studio fees";
            case "B-EXP": return "Cost schedule";
            case "B-PERSONAL": return "Party shop";
            case "B-PRINT": return "Print bill";
            default: return sourceId ?? "";
        }
    }

    /// <summary>The worksheet rows of a case, resolved from its content (label/amount as printed); empty when it has none.</summary>
    public static List<Level3WorksheetRow> RowsOf(CaseDefinition definition)
    {
        var rows = new List<Level3WorksheetRow>();
        if (!IsBea(definition)) return rows;
        foreach (string rowId in RowIds)
        {
            Level3WorksheetRow row = Resolve(definition, rowId);
            if (row != null) rows.Add(row);
        }
        return rows;
    }

    /// <summary>The draft claim a note asks about (B-DRAFT's printed label and amount), or null.</summary>
    public static Level3WorksheetRow ClaimOf(CaseDefinition definition, string noteId) =>
        IsBea(definition) && NoteClaimRow(noteId) != null ? Resolve(definition, NoteClaimRow(noteId)) : null;

    private static bool IsBea(CaseDefinition definition) =>
        definition?.level3 != null && definition.level3.IsSet && definition.caseId == Level3ChapterRules.BeaCaseId;

    private static Level3WorksheetRow Resolve(CaseDefinition definition, string rowId)
    {
        Level3CaseContent content = definition.level3;
        Level3RowDefinition row = content.rows.Find(r => r != null && r.rowId == rowId);
        if (row == null) return null;
        Level3SourceDefinition source = content.sources.Find(s => s != null && s.sourceId == row.sourceId);
        DocumentDefinition document = source != null ? definition.documents?.Find(d => d != null && d.documentName == source.documentName) : null;
        DocumentFieldDefinition field = document?.fields?.Find(f => f != null && f.semanticKey == row.fieldKey);
        return new Level3WorksheetRow
        {
            RowId = row.rowId, SourceId = row.sourceId, FieldKey = row.fieldKey, TransactionId = row.transactionId,
            Label = field != null ? field.label : row.fieldKey, Amount = row.amount
        };
    }

    /// <summary>The worksheet can be edited (drafting, and correcting after T06's first check).</summary>
    public static bool Editable(Level3ChapterCheckpoint state)
    {
        Level3Phase phase = Level3ChapterRules.PhaseOf(state);
        return (phase == Level3Phase.WorksheetDraft || phase == Level3Phase.WorksheetCorrections) && Level3ChapterRules.Problem(state) == null;
    }

    // =========================================================
    // COMMANDS (on a copy)
    // =========================================================

    public static Level3CommandOutcome Extract(Level3ChapterCheckpoint candidate, IList<Level3WorksheetRow> rows, string rowId, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        Level3WorksheetRow row = Find(rows, rowId);
        if (row == null) { reason = "That row is not part of Bea's worksheet."; return Level3CommandOutcome.Refused; }
        if (!candidate.Case(Level3ChapterRules.BeaCaseId).acquiredSourceIds.Contains(row.SourceId))
        {
            reason = "That paper is not in Bea's records.";
            return Level3CommandOutcome.Refused;
        }
        Level3WorksheetDraft draft = candidate.worksheet;
        if (draft.Strip(rowId) != null)
        {
            reason = "already on the worksheet";
            draft.selectedRow = rowId;
            return Level3CommandOutcome.Duplicate;
        }
        draft.strips.Add(new Level3StripRecord { rowId = row.RowId, sourceId = row.SourceId, amount = row.Amount, section = "" });
        draft.selectedSource = row.SourceId;
        draft.selectedRow = row.RowId;
        Edited(draft, "extract", rowId, "");
        return Level3CommandOutcome.Applied;
    }

    public static Level3CommandOutcome Place(Level3ChapterCheckpoint candidate, string rowId, string section, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        if (Array.IndexOf(Sections, section) < 0) { reason = "That is not a worksheet section."; return Level3CommandOutcome.Refused; }
        Level3WorksheetDraft draft = candidate.worksheet;
        Level3StripRecord strip = draft.Strip(rowId);
        if (strip == null) { reason = "Lift that row from its record first."; return Level3CommandOutcome.Refused; }
        if (strip.section == section) { reason = "already there"; return Level3CommandOutcome.Duplicate; }
        string previous = strip.section;
        strip.section = section;
        draft.selectedRow = rowId;
        MarkDirty(draft, previous, section);
        Edited(draft, "place", rowId, previous);
        return Level3CommandOutcome.Applied;
    }

    public static Level3CommandOutcome SetReason(Level3ChapterCheckpoint candidate, string noteId, string reasonKey, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        if (Array.IndexOf(Notes, noteId) < 0 || Array.IndexOf(Reasons, reasonKey) < 0) { reason = "That is not one of the note's reasons."; return Level3CommandOutcome.Refused; }
        Level3NoteRecord note = NoteOf(candidate.worksheet, noteId);
        if (note.reason == reasonKey) { reason = "already chosen"; return Level3CommandOutcome.Duplicate; }
        string previous = note.reason;
        note.reason = reasonKey;
        Edited(candidate.worksheet, "reason", noteId, previous);
        return Level3CommandOutcome.Applied;
    }

    public static Level3CommandOutcome SetRelated(Level3ChapterCheckpoint candidate, string noteId, string relatedRow, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        if (!RelatedCandidates(noteId).Contains(relatedRow)) { reason = "That entry cannot be linked to this note."; return Level3CommandOutcome.Refused; }
        Level3NoteRecord note = NoteOf(candidate.worksheet, noteId);
        if (note.related == relatedRow) { reason = "already linked"; return Level3CommandOutcome.Duplicate; }
        string previous = note.related;
        note.related = relatedRow;
        Edited(candidate.worksheet, "related", noteId, previous);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>Puts back the previous value of the last edit (an extraction is put back into its record).</summary>
    public static Level3CommandOutcome Undo(Level3ChapterCheckpoint candidate, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        Level3WorksheetDraft draft = candidate.worksheet;
        if (draft.undo.Count == 0) { reason = "Nothing to undo."; return Level3CommandOutcome.Refused; }
        Level3WorksheetUndo last = draft.undo[draft.undo.Count - 1];
        draft.undo.RemoveAt(draft.undo.Count - 1);
        switch (last.kind)
        {
            case "extract":
                Level3StripRecord lifted = draft.Strip(last.target);
                if (lifted != null) { MarkDirty(draft, lifted.section, ""); draft.strips.Remove(lifted); }
                break;
            case "place":
                Level3StripRecord moved = draft.Strip(last.target);
                if (moved != null) { MarkDirty(draft, moved.section, last.previous); moved.section = last.previous; }
                break;
            case "reason":
                NoteOf(draft, last.target).reason = last.previous;
                break;
            case "related":
                NoteOf(draft, last.target).related = last.previous;
                break;
        }
        draft.selectedRow = last.kind == "extract" || last.kind == "place" ? last.target : draft.selectedRow;
        draft.revision++;
        return Level3CommandOutcome.Applied;
    }

    /// <summary>
    /// T05: the lever. Files one row of the open batch with its treatment and, for
    /// R6/R7, its reason, together (never half a classification). Validates the
    /// case's own acquired source, the row's batch (open, not accepted or future),
    /// the treatment/reason vocabulary and the draft revision the choice was made
    /// on (a late or repeated pull cannot apply over a newer change). The saved
    /// slip copies the row's printed amount and source exactly; the note keeps any
    /// earlier link. Duplicate: the slip already holds exactly this choice (a
    /// double pull, an unchanged refile) and nothing is written. Every treatment
    /// is accepted for every row: nothing is graded here (T06's Check does that).
    /// </summary>
    public static Level3CommandOutcome File(Level3ChapterCheckpoint candidate, IList<Level3WorksheetRow> rows, Level3Filing filing, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        Level3WorksheetRow row = Find(rows, filing.RowId);
        if (row == null) { reason = "That record is not part of Bea's worksheet."; return Level3CommandOutcome.Refused; }
        Level3CaseProgress bea = candidate.Case(Level3ChapterRules.BeaCaseId);
        if (bea?.acquiredSourceIds == null || !bea.acquiredSourceIds.Contains(row.SourceId)) { reason = "That paper is not in Bea's records."; return Level3CommandOutcome.Refused; }
        string batch = BatchOf(row.RowId);
        if (BatchAccepted(candidate, batch)) { reason = "That batch is already accepted: you can read it, not change it."; return Level3CommandOutcome.Refused; }
        if (batch != OpenBatch(candidate)) { reason = "That record's batch is not open yet."; return Level3CommandOutcome.Refused; }
        if (Array.IndexOf(Treatments, filing.Treatment) < 0) { reason = ChooseCategory; return Level3CommandOutcome.Refused; }
        bool needs = NeedsReason(row.RowId);
        if (needs && Array.IndexOf(Reasons, filing.Reason) < 0) { reason = ChooseReason; return Level3CommandOutcome.Refused; }
        if (!needs && !string.IsNullOrEmpty(filing.Reason)) { reason = "That record takes no reason."; return Level3CommandOutcome.Refused; }

        Level3WorksheetDraft draft = candidate.worksheet;
        Level3StripRecord strip = draft.Strip(row.RowId);
        if (strip != null && (strip.sourceId != row.SourceId || strip.amount != row.Amount))
        {
            reason = "That record's saved slip does not match its original.";
            return Level3CommandOutcome.Refused;
        }
        if (strip != null && RevisedDestination(strip.section) == filing.Treatment && (!needs || SavedReason(draft, row.RowId) == filing.Reason))
        {
            reason = "already filed";
            return Level3CommandOutcome.Duplicate;
        }
        if (filing.BasedOn != draft.revision) { reason = "Bea's worksheet changed meanwhile. Look at the record and file it again."; return Level3CommandOutcome.Refused; }

        string previous = strip != null ? strip.section : "";
        if (strip == null)
        {
            strip = new Level3StripRecord { rowId = row.RowId, sourceId = row.SourceId, amount = row.Amount, section = "" };
            draft.strips.Add(strip);
        }
        strip.section = SectionFor(filing.Treatment, previous);
        if (needs) NoteOf(draft, NoteForRow(row.RowId)).reason = filing.Reason;
        MarkDirty(draft, previous, strip.section);
        draft.selectedSource = row.SourceId;
        draft.selectedRow = row.RowId;
        draft.revision++;
        if (!draft.drafted && AllFiled(draft)) draft.drafted = true;
        return Level3CommandOutcome.Applied;
    }

    /// <summary>The lever's one-line prompts while a choice is missing (no buzz, no grade).</summary>
    public const string ChooseCategory = "Choose a category";
    public const string ChooseReason = "Choose a reason";

    /// <summary>
    /// The draft's structural state for presentation and T06: every strip lifted and placed,
    /// both reasons chosen and the link chosen. Says nothing about correctness.
    /// </summary>
    public static bool AllFilledIn(Level3WorksheetDraft draft)
    {
        if (draft == null) return false;
        foreach (string row in RowIds)
        {
            Level3StripRecord strip = draft.Strip(row);
            if (strip == null || Array.IndexOf(Sections, strip.section) < 0) return false;
        }
        foreach (string note in Notes)
        {
            Level3NoteRecord record = draft.Note(note);
            if (record == null || Array.IndexOf(Reasons, record.reason) < 0) return false;
            if (NoteHasRelated(note) && !RelatedCandidates(note).Contains(record.related)) return false;
        }
        return true;
    }

    /// <summary>Every strip lifted and placed in some section (the notes are the next step).</summary>
    public static bool AllPlaced(Level3WorksheetDraft draft)
    {
        if (draft == null) return false;
        foreach (string row in RowIds)
        {
            Level3StripRecord strip = draft.Strip(row);
            if (strip == null || Array.IndexOf(Sections, strip.section) < 0) return false;
        }
        return true;
    }

    /// <summary>The strips in a section, in lift order.</summary>
    public static List<Level3StripRecord> InSection(Level3WorksheetDraft draft, string section)
    {
        var strips = new List<Level3StripRecord>();
        if (draft?.strips == null) return strips;
        foreach (Level3StripRecord strip in draft.strips) if (strip != null && strip.section == section) strips.Add(strip);
        return strips;
    }

    private static bool Ready(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (candidate == null || !Editable(candidate)) { reason = "The worksheet cannot be changed right now."; return false; }
        if (candidate.worksheet == null) candidate.worksheet = new Level3WorksheetDraft();
        return true;
    }

    private static Level3WorksheetRow Find(IList<Level3WorksheetRow> rows, string rowId)
    {
        if (rows == null || Array.IndexOf(RowIds, rowId) < 0) return null;
        foreach (Level3WorksheetRow row in rows) if (row != null && row.RowId == rowId) return row;
        return null;
    }

    private static Level3NoteRecord NoteOf(Level3WorksheetDraft draft, string noteId)
    {
        Level3NoteRecord note = draft.Note(noteId);
        if (note != null) return note;
        note = new Level3NoteRecord { noteId = noteId };
        draft.notes.Add(note);
        return note;
    }

    private static void MarkDirty(Level3WorksheetDraft draft, string from, string to)
    {
        if (from == Income || to == Income) draft.incomeDirty = true;
        if (from == Expenses || to == Expenses) draft.expensesDirty = true;
    }

    private static void Edited(Level3WorksheetDraft draft, string kind, string target, string previous)
    {
        draft.undo.Add(new Level3WorksheetUndo { kind = kind, target = target, previous = previous ?? "" });
        if (draft.undo.Count > Level3WorksheetDraft.MaxUndo) draft.undo.RemoveAt(0);
        draft.revision++;
        if (!draft.drafted && AllFilledIn(draft)) draft.drafted = true;
    }
}
