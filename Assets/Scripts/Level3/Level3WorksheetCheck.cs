using System;
using System.Collections.Generic;

/// <summary>One graded worksheet decision at a check: its id, whether it was right, and the player's own checked value.</summary>
public sealed class Level3WorksheetDecision
{
    public string Id;
    public bool Correct;
    /// <summary>The player's key as checked ("expenses", "personal-purpose", "R5", a tape signature); never the expected answer.</summary>
    public string Value;
}

/// <summary>What one Check of Bea's worksheet found. Presentation reads it; only a complete check is ever saved.</summary>
public sealed class Level3WorksheetEvaluation
{
    /// <summary>Seven placed strips, two reasons, the link and two current subtotal tapes.</summary>
    public bool Complete;
    /// <summary>Where work is missing ("lift:R3", "place:R3", "strip:R3", "note-R6/reason", "note-R7/related", "subtotal:income").</summary>
    public readonly List<string> MissingPlaces = new List<string>();
    /// <summary>One plain line per missing place (same order).</summary>
    public readonly List<string> MissingLines = new List<string>();
    /// <summary>The twelve decisions, in DecisionIds order (only when complete).</summary>
    public readonly List<Level3WorksheetDecision> Decisions = new List<Level3WorksheetDecision>();
    public int Correct;
    /// <summary>The attached, current subtotal results (exact decimal text; null when missing).</summary>
    public string IncomeTotal;
    public string ExpensesTotal;
    /// <summary>Both attached totals equal the supported totals in the case content.</summary>
    public bool TotalsSupported;

    public int Wrong => Decisions.Count - Correct;

    /// <summary>Every decision right and both supported totals current: the worksheet can be collected.</summary>
    public bool AllCorrect => Complete && Decisions.Count == Level3WorksheetCheck.DecisionCount && Correct == Decisions.Count && TotalsSupported;

    public List<string> WrongIds()
    {
        var ids = new List<string>();
        foreach (Level3WorksheetDecision d in Decisions) if (!d.Correct) ids.Add(d.Id);
        return ids;
    }

    /// <summary>Correct decisions among those whose id starts with prefix ("dest-", "reason-", "related-", "subtotal-").</summary>
    public int CorrectIn(string prefix, out int total)
    {
        int correct = 0;
        total = 0;
        foreach (Level3WorksheetDecision d in Decisions)
        {
            if (!d.Id.StartsWith(prefix, StringComparison.Ordinal)) continue;
            total++;
            if (d.Correct) correct++;
        }
        return correct;
    }
}

/// <summary>What collecting the worksheet granted (presentation after the write).</summary>
public struct Level3WorksheetGrants
{
    /// <summary>The candidate changed (claims, mastery): a write is needed.</summary>
    public bool Changed;
    /// <summary>This campaign had already been paid for the worksheet (a repeat or a restore): nothing more.</summary>
    public bool AlreadyClaimed;
    public CampaignRewards.Change Trust;
    public CampaignRewards.Change Coins;
    /// <summary>The frozen first-check XP and tier (BadgeTier as a number).</summary>
    public int Xp;
    public int Tier;
    /// <summary>The once-only mastery claim was recorded by this write.</summary>
    public bool MasteryClaimed;
}

/// <summary>
/// PURPOSE:
/// Bea's worksheet check (Level 3 T06, "Worth the Tax?"), pure C# so the
/// regression suite drives the same commands the desk calls. Every command
/// runs on a COPY of the chapter snapshot; the caller writes, then publishes.
///
/// - AttachSubtotal: the calculator's printed tape goes onto the Income or
///   Allowable expenses subtotal. Its arithmetic is re-evaluated exactly; its
///   numbers must be the strips themselves (row identity and printed amount,
///   never typed), except an explicit 0 for an empty section. Any such tape is
///   accepted, whatever it adds up: the Check judges it. Attaching clears the
///   section's "entries changed" flag; moving a strip in or out sets it again
///   (Level3WorksheetRules), which leaves the old tape on the sheet as stale.
/// - Evaluate / Check: completeness first (seven placed strips, two reasons,
///   the link, two current tapes). An incomplete Check lists what is missing
///   and saves nothing. The FIRST complete Check freezes correct/wrong ids/
///   tier/XP (Gold 12, Silver 10-11, Bronze 0-9; XP 60 - wrong, floor 48)
///   once; later checks only refresh the marks. A subtotal is right when it
///   adds exactly the strips the player put in that section, each once, so a
///   treatment error is never charged twice. Every decision right AND both
///   totals equal to the content's supported totals -> WorksheetReady.
/// - Acquire: the player collects the ready worksheet (Ready -> BeaComputation,
///   "B-WORKSHEET" into Bea's folder). ApplyAcquisitionGrants adds the once-only
///   claims (60 coins, +2 Public Trust, the mastery award) and the badge tier
///   in the same candidate save (PlayerProgressManager.CommitLevelThreeWorksheet);
///   the frozen XP joins the career total with the chapter result (T14).
/// The answer key (worksheetDecisions, expectedFields) is read here and only
/// here; it is never copied into the snapshot, a tape or the Bag.
///
/// REVISED T06 (5 November 2026, L3-worksheet-v2; bottom of this file): the
/// paper machine's three batches are checked one at a time (CheckBatch): nine
/// decisions (seven treatments, the R6/R7 reasons), first outcomes frozen once
/// per batch, all-right batches accepted, the last one making the worksheet
/// ready; no tapes, no link choice. The collected B-WORKSHEET is presented as
/// Bea's Income & Expense Summary (SummaryRecord / SummaryTotals), whose exact
/// totals the office reads. The twelve-decision members above remain only for
/// legacy grades and the retired board; a legacy grade is never regraded.
/// </summary>
public static class Level3WorksheetCheck
{
    /// <summary>This check's assessment (twelve decisions), recorded with the frozen first grade; the revised nine-decision check (T06) records its own.</summary>
    public const string AssessmentVersion = "L3-worksheet-v1";
    public const int DecisionCount = 12;
    public const int MaxXp = 60;
    public const int MinXp = 48;
    public const int XpPerWrong = 1;
    public const int GoldAt = 12;
    public const int SilverAt = 10;
    /// <summary>The Worth the Tax? badge (Levels/Level_03/rewards.json sideQuest.badgeId).</summary>
    public const string BadgeId = "worth_the_tax_3";

    public const string IncomeTape = "bea/subtotal-income";
    public const string ExpensesTape = "bea/subtotal-expenses";
    /// <summary>A calculator number lifted from a worksheet strip: "strip:R1".</summary>
    public const string StripPrefix = "strip:";
    /// <summary>The collected worksheet in Bea's folder (Level3CaseProgress.workIds).</summary>
    public const string WorksheetWorkId = "B-WORKSHEET";

    public const string FirstCheckAction = "Level_03/worksheet-first-check";
    public const string ReadyAction = "Level_03/worksheet-ready";
    public const string AcquireAction = "Level_03/bea-worksheet-acquired";
    public const string CheckedMilestone = "worksheet-first-check";
    public const string AcquiredMilestone = "bea-worksheet-acquired";

    public const string RefuseNoTape = "Press = to print a tape first.";
    public const string RefuseTyped = "Use the strips themselves: tap each strip in the section, so every amount keeps its record. Press C to start again.";
    public const string RefuseForeign = "That tape has numbers that aren't Bea's strips. Press C, then tap the strips in the section.";

    /// <summary>The twelve first-check decisions (FIXTURE_CONTRACT), in reveal order.</summary>
    public static readonly string[] DecisionIds =
    {
        "dest-R1", "dest-R2", "dest-R3", "dest-R4", "dest-R5", "dest-R6", "dest-R7",
        "reason-R6", "reason-R7", "related-R7", "subtotal-income", "subtotal-expenses"
    };

    public static string TapeFor(string section) =>
        section == Level3WorksheetRules.Income ? IncomeTape : section == Level3WorksheetRules.Expenses ? ExpensesTape : null;

    public static string StripSource(string rowId) => StripPrefix + rowId;

    public static string StripRow(string sourceId) =>
        sourceId != null && sourceId.StartsWith(StripPrefix, StringComparison.Ordinal) ? sourceId.Substring(StripPrefix.Length) : null;

    /// <summary>The board place a decision is marked at: "strip:R3", "note-R6/reason", "note-R7/related", "subtotal:income".</summary>
    public static string PlaceOf(string decisionId)
    {
        if (decisionId == null) return null;
        if (decisionId.StartsWith("dest-", StringComparison.Ordinal)) return "strip:" + decisionId.Substring(5);
        if (decisionId == "reason-R6") return Level3WorksheetRules.PartyNote + "/reason";
        if (decisionId == "reason-R7") return Level3WorksheetRules.PrintingNote + "/reason";
        if (decisionId == "related-R7") return Level3WorksheetRules.PrintingNote + "/related";
        if (decisionId == "subtotal-income") return "subtotal:" + Level3WorksheetRules.Income;
        if (decisionId == "subtotal-expenses") return "subtotal:" + Level3WorksheetRules.Expenses;
        return null;
    }

    public static int XpFor(int wrong) => Math.Max(MinXp, MaxXp - Math.Max(0, wrong) * XpPerWrong);

    /// <summary>BadgeTier as a number: 3 Gold (12/12), 2 Silver (10-11), 1 Bronze (0-9).</summary>
    public static int TierFor(int correct) => correct >= GoldAt ? 3 : correct >= SilverAt ? 2 : 1;

    public static string TierName(int tier) => tier >= 3 ? "Gold" : tier == 2 ? "Silver" : tier == 1 ? "Bronze" : "";

    // =========================================================
    // TAPES
    // =========================================================

    public static Level3TapeRecord SubtotalTape(Level3ChapterCheckpoint state, string section) =>
        state != null && TapeFor(section) != null ? state.Tape(TapeFor(section)) : null;

    /// <summary>The section's membership changed since its tape was attached ("entries changed: recalculate").</summary>
    public static bool Dirty(Level3WorksheetDraft draft, string section) =>
        draft != null && (section == Level3WorksheetRules.Income ? draft.incomeDirty : section == Level3WorksheetRules.Expenses && draft.expensesDirty);

    /// <summary>A tape is on the section but its strips changed since: it stays on the sheet, marked stale, until replaced.</summary>
    public static bool IsStale(Level3ChapterCheckpoint state, string section) =>
        SubtotalTape(state, section) != null && Dirty(state.worksheet, section);

    public static bool IsCurrent(Level3ChapterCheckpoint state, string section) =>
        SubtotalTape(state, section) != null && !Dirty(state.worksheet, section);

    /// <summary>
    /// Attaches the printed tape to a money section's subtotal on the candidate.
    /// Applied: the tape replaces any older one and the section is current.
    /// Duplicate: the same current tape is already there. Refused: nothing
    /// changed; reason is a plain sentence, never a grade.
    /// </summary>
    public static Level3CommandOutcome AttachSubtotal(Level3ChapterCheckpoint candidate, IList<Level3WorksheetRow> rows, string section,
        IReadOnlyList<Level3CalcToken> expression, string result, out string reason)
    {
        reason = null;
        string destination = TapeFor(section);
        if (destination == null) { reason = "Only Income and Allowable expenses take a subtotal tape."; return Level3CommandOutcome.Refused; }
        if (candidate == null || !Level3WorksheetRules.Editable(candidate)) { reason = "The worksheet cannot be changed right now."; return Level3CommandOutcome.Refused; }
        if (candidate.worksheet == null) candidate.worksheet = new Level3WorksheetDraft();
        if (expression == null || expression.Count == 0 || result == null) { reason = RefuseNoTape; return Level3CommandOutcome.Refused; }
        // The tape's own arithmetic, recomputed exactly: a printed result is never trusted as given.
        if (Level3Calculator.Evaluate(expression, out decimal value) != null || !Level3Calculator.TryParse(result, out decimal printed) || value != printed)
        {
            reason = "Press = again to print the whole calculation.";
            return Level3CommandOutcome.Refused;
        }

        Level3WorksheetDraft draft = candidate.worksheet;
        bool emptySection = Level3WorksheetRules.InSection(draft, section).Count == 0;
        foreach (Level3CalcToken token in expression)
        {
            if (token == null) { reason = RefuseForeign; return Level3CommandOutcome.Refused; }
            if (!token.IsNumber) continue;
            string rowId = StripRow(token.sourceId);
            if (rowId != null)
            {
                if (draft.Strip(rowId) == null || Find(rows, rowId) == null) { reason = "That strip is not on the worksheet any more. Press C and start again."; return Level3CommandOutcome.Refused; }
                continue;
            }
            // An empty section takes an explicit zero; otherwise a typed number has no record behind it.
            if (token.manual && emptySection && expression.Count == 1 && value == 0m) continue;
            reason = token.manual ? RefuseTyped : RefuseForeign;
            return Level3CommandOutcome.Refused;
        }

        Level3TapeRecord existing = candidate.Tape(destination);
        if (existing != null && !Dirty(draft, section) && SameTape(existing, expression, value)) { reason = "already attached"; return Level3CommandOutcome.Duplicate; }
        if (existing != null) candidate.tapes.Remove(existing);
        var tape = new Level3TapeRecord
        {
            tapeId = "tape-" + destination.Replace('/', '-'),
            caseId = Level3ChapterRules.BeaCaseId,
            destinationId = destination,
            result = Level3Calculator.Canonical(value),
            actionId = "Level_03/attach/" + destination + "@" + (draft.revision + 1)
        };
        foreach (Level3CalcToken token in expression) tape.expression.Add(token.Copy());
        candidate.tapes.Add(tape);
        if (section == Level3WorksheetRules.Income) draft.incomeDirty = false;
        else draft.expensesDirty = false;
        // A new tape is a change a later check must read again.
        draft.revision++;
        return Level3CommandOutcome.Applied;
    }

    private static bool SameTape(Level3TapeRecord tape, IReadOnlyList<Level3CalcToken> expression, decimal value)
    {
        if (tape.expression == null || tape.expression.Count != expression.Count) return false;
        if (!Level3Calculator.TryParse(tape.result, out decimal result) || result != value) return false;
        for (int i = 0; i < expression.Count; i++)
        {
            Level3CalcToken a = tape.expression[i], b = expression[i];
            if (a == null || b == null || a.kind != b.kind || a.op != b.op || a.sourceId != b.sourceId || a.manual != b.manual || a.percent != b.percent) return false;
            if (a.IsNumber && (!Level3Calculator.TryParse(a.value, out decimal x) || !Level3Calculator.TryParse(b.value, out decimal y) || x != y)) return false;
        }
        return true;
    }

    /// <summary>A short, stable description of a tape for marks ("R1+R2=900000"; "" when none).</summary>
    public static string Signature(Level3TapeRecord tape)
    {
        if (tape?.expression == null) return "";
        var parts = new List<string>();
        foreach (Level3CalcToken token in tape.expression)
        {
            if (token == null) continue;
            if (token.IsOperator) parts.Add(token.op);
            else parts.Add((StripRow(token.sourceId) ?? ((token.manual ? "n:" : "s:") + token.value)) + (token.percent ? "%" : ""));
        }
        return string.Join("", parts) + "=" + tape.result;
    }

    // =========================================================
    // EVALUATE (completeness, then the twelve decisions)
    // =========================================================

    /// <summary>The player's current value for a decision (their own key; "" when not set or the tape is not current).</summary>
    public static string CurrentValue(Level3ChapterCheckpoint state, string decisionId)
    {
        Level3WorksheetDraft draft = state?.worksheet;
        if (draft == null || decisionId == null) return "";
        if (decisionId.StartsWith("dest-", StringComparison.Ordinal)) return draft.Strip(decisionId.Substring(5))?.section ?? "";
        if (decisionId == "reason-R6") return draft.Note(Level3WorksheetRules.PartyNote)?.reason ?? "";
        if (decisionId == "reason-R7") return draft.Note(Level3WorksheetRules.PrintingNote)?.reason ?? "";
        if (decisionId == "related-R7") return draft.Note(Level3WorksheetRules.PrintingNote)?.related ?? "";
        string section = decisionId == "subtotal-income" ? Level3WorksheetRules.Income : decisionId == "subtotal-expenses" ? Level3WorksheetRules.Expenses : null;
        return section != null && IsCurrent(state, section) ? Signature(SubtotalTape(state, section)) : "";
    }

    /// <summary>
    /// Completeness and (when complete) every decision against the content's
    /// answer key. Reads only; the state is not changed.
    /// </summary>
    public static Level3WorksheetEvaluation Evaluate(Level3ChapterCheckpoint state, CaseDefinition definition)
    {
        var eval = new Level3WorksheetEvaluation();
        List<Level3WorksheetRow> rows = Level3WorksheetRules.RowsOf(definition);
        Level3WorksheetDraft draft = state?.worksheet ?? new Level3WorksheetDraft();
        if (rows.Count != Level3WorksheetRules.RowIds.Length)
        {
            Missing(eval, "content", "Bea's worksheet content did not load.");
            return eval;
        }
        var byId = new Dictionary<string, Level3WorksheetRow>();
        foreach (Level3WorksheetRow row in rows) byId[row.RowId] = row;

        foreach (string rowId in Level3WorksheetRules.RowIds)
        {
            Level3WorksheetRow row = byId[rowId];
            Level3StripRecord strip = draft.Strip(rowId);
            if (strip == null) Missing(eval, "lift:" + rowId, row.Label + " is still on its record (" + Level3WorksheetRules.SourceTabLabel(row.SourceId) + ").");
            else if (strip.sourceId != row.SourceId || !SameAmount(strip.amount, row.Amount)) Missing(eval, "strip:" + rowId, row.Label + "'s strip does not match its record.");
            else if (Array.IndexOf(Level3WorksheetRules.Sections, strip.section) < 0) Missing(eval, "place:" + rowId, row.Label + " is lifted but not placed in a section.");
        }
        foreach (string noteId in Level3WorksheetRules.Notes)
        {
            Level3NoteRecord note = draft.Note(noteId);
            string claim = Level3WorksheetRules.ClaimOf(definition, noteId)?.Label ?? noteId;
            if (note == null || Array.IndexOf(Level3WorksheetRules.Reasons, note.reason) < 0)
                Missing(eval, noteId + "/reason", "The " + claim.ToLowerInvariant() + " note needs a reason.");
            if (Level3WorksheetRules.NoteHasRelated(noteId) && (note == null || !Level3WorksheetRules.RelatedCandidates(noteId).Contains(note.related)))
                Missing(eval, noteId + "/related", "The " + claim.ToLowerInvariant() + " note needs its related entry.");
        }
        foreach (string section in new[] { Level3WorksheetRules.Income, Level3WorksheetRules.Expenses })
        {
            string name = Level3WorksheetRules.SectionLabel(section);
            Level3TapeRecord tape = SubtotalTape(state, section);
            if (tape == null) Missing(eval, "subtotal:" + section, name + " needs a calculator tape.");
            else if (Dirty(draft, section)) Missing(eval, "subtotal:" + section, name + "'s tape is out of date: its entries changed, so recalculate.");
        }
        if (eval.MissingPlaces.Count > 0) return eval;
        eval.Complete = true;

        foreach (string id in DecisionIds)
        {
            string value = CurrentValue(state, id);
            bool correct;
            if (id.StartsWith("subtotal-", StringComparison.Ordinal))
                correct = SupportedSubtotal(state, id == "subtotal-income" ? Level3WorksheetRules.Income : Level3WorksheetRules.Expenses, byId);
            else
            {
                string expected = Expected(definition, id);
                correct = expected != null && expected == AnswerFor(id, value, byId);
            }
            eval.Decisions.Add(new Level3WorksheetDecision { Id = id, Correct = correct, Value = value });
            if (correct) eval.Correct++;
        }
        eval.IncomeTotal = SubtotalTape(state, Level3WorksheetRules.Income)?.result;
        eval.ExpensesTotal = SubtotalTape(state, Level3WorksheetRules.Expenses)?.result;
        eval.TotalsSupported = SameAmount(eval.IncomeTotal, ExpectedField(definition, "grossIncome")) &&
                               SameAmount(eval.ExpensesTotal, ExpectedField(definition, "allowableExpenses"));
        return eval;
    }

    /// <summary>The player's value in the answer key's own words (section/reason label, or "R5/B-EXP/PRINT-26" for a link).</summary>
    private static string AnswerFor(string decisionId, string value, Dictionary<string, Level3WorksheetRow> byId)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (decisionId.StartsWith("dest-", StringComparison.Ordinal)) return Level3WorksheetRules.SectionLabel(value);
        if (decisionId.StartsWith("reason-", StringComparison.Ordinal)) return Level3WorksheetRules.ReasonLabel(value);
        if (decisionId.StartsWith("related-", StringComparison.Ordinal))
            return byId.TryGetValue(value, out Level3WorksheetRow row) ? row.RowId + "/" + row.SourceId + "/" + row.TransactionId : null;
        return null;
    }

    /// <summary>
    /// The subtotal adds exactly the strips in its section, each once, with
    /// "+" only, using the strips' own printed amounts (an explicit 0 for an
    /// empty section). Whether those strips belong there is the destination
    /// decisions' business, never this one's.
    /// </summary>
    public static bool SupportedSubtotal(Level3ChapterCheckpoint state, string section, Dictionary<string, Level3WorksheetRow> byId)
    {
        if (!IsCurrent(state, section)) return false;
        Level3TapeRecord tape = SubtotalTape(state, section);
        List<Level3CalcToken> tokens = tape.expression;
        if (tokens == null || tokens.Count == 0) return false;
        if (Level3Calculator.Evaluate(tokens, out decimal value) != null || !Level3Calculator.TryParse(tape.result, out decimal result) || result != value) return false;
        List<Level3StripRecord> chosen = Level3WorksheetRules.InSection(state.worksheet, section);
        if (chosen.Count == 0) return tokens.Count == 1 && tokens[0] != null && tokens[0].IsNumber && value == 0m;

        var used = new List<string>();
        for (int i = 0; i < tokens.Count; i++)
        {
            Level3CalcToken token = tokens[i];
            if (token == null) return false;
            if (i % 2 == 1)
            {
                if (!token.IsOperator || token.op != "+") return false;
                continue;
            }
            string rowId = StripRow(token.sourceId);
            if (rowId == null || token.manual || token.percent || !byId.TryGetValue(rowId, out Level3WorksheetRow row) || !SameAmount(token.value, row.Amount)) return false;
            used.Add(rowId);
        }
        var wanted = new List<string>();
        foreach (Level3StripRecord strip in chosen) wanted.Add(strip.rowId);
        used.Sort(StringComparer.Ordinal);
        wanted.Sort(StringComparer.Ordinal);
        if (used.Count != wanted.Count) return false;
        for (int i = 0; i < used.Count; i++) if (used[i] != wanted[i]) return false;
        return true;
    }

    private static string Expected(CaseDefinition definition, string decisionId)
    {
        List<Level3DecisionDefinition> decisions = definition?.level3?.worksheetDecisions;
        if (decisions == null) return null;
        foreach (Level3DecisionDefinition d in decisions) if (d != null && d.decisionId == decisionId) return d.expected;
        return null;
    }

    private static string ExpectedField(CaseDefinition definition, string field)
    {
        List<Level3ExpectedField> fields = definition?.level3?.expectedFields;
        if (fields == null) return null;
        foreach (Level3ExpectedField f in fields) if (f != null && f.field == field) return f.amount;
        return null;
    }

    private static void Missing(Level3WorksheetEvaluation eval, string place, string line)
    {
        eval.MissingPlaces.Add(place);
        eval.MissingLines.Add(line);
    }

    private static bool SameAmount(string a, string b) =>
        Level3Calculator.TryParse(a, out decimal x) && Level3Calculator.TryParse(b, out decimal y) && x == y;

    private static Level3WorksheetRow Find(IList<Level3WorksheetRow> rows, string rowId)
    {
        if (rows == null) return null;
        foreach (Level3WorksheetRow row in rows) if (row != null && row.RowId == rowId) return row;
        return null;
    }

    // =========================================================
    // CHECK (deliberate; the first complete one is frozen)
    // =========================================================

    public enum CheckOutcome
    {
        /// <summary>Something is missing: nothing is graded or saved (eval lists what).</summary>
        Incomplete,
        /// <summary>A complete check of a worksheet unchanged since the last one: nothing to save.</summary>
        Unchanged,
        /// <summary>A complete check: the candidate holds the result (frozen first, refreshed marks, maybe Ready).</summary>
        Checked,
        /// <summary>Not allowed now: nothing changed.</summary>
        Refused
    }

    /// <summary>
    /// The deliberate Check on the candidate. The first complete check freezes
    /// the grade once (WorksheetDraft -> WorksheetCorrections, or straight to
    /// WorksheetReady when every decision is right); a later check refreshes
    /// only which places are marked, and moves to WorksheetReady when
    /// everything is right. Corrections never change the frozen first result.
    /// </summary>
    public static CheckOutcome Check(Level3ChapterCheckpoint candidate, CaseDefinition definition, out Level3WorksheetEvaluation eval, out string reason)
    {
        eval = null;
        reason = null;
        Level3Phase phase = Level3ChapterRules.PhaseOf(candidate);
        if (candidate != null && phase == Level3Phase.WorksheetReady) { reason = "Every decision checks out. Collect the worksheet."; return CheckOutcome.Refused; }
        if (candidate == null || !Level3WorksheetRules.Editable(candidate)) { reason = "The worksheet cannot be checked right now."; return CheckOutcome.Refused; }
        // T06 revision: once the machine's batch checks have started, the retired twelve-decision board never grades.
        if (MachineChecked(candidate)) { reason = "Check each batch at Bea's machine."; return CheckOutcome.Refused; }
        if (candidate.worksheet == null) candidate.worksheet = new Level3WorksheetDraft();
        if (candidate.worksheetCheck == null) candidate.worksheetCheck = new Level3WorksheetResult();

        eval = Evaluate(candidate, definition);
        if (!eval.Complete) { reason = "Not checked yet: something is still missing."; return CheckOutcome.Incomplete; }
        Level3WorksheetResult result = candidate.worksheetCheck;
        int revision = candidate.worksheet.revision;
        if (result.checks > 0 && result.lastRevision == revision) { reason = "Nothing has changed since your last check."; return CheckOutcome.Unchanged; }

        List<string> wrong = eval.WrongIds();
        if (!result.firstChecked)
        {
            result.firstChecked = true;
            result.assessmentVersion = AssessmentVersion;
            result.firstRevision = revision;
            result.firstCorrect = eval.Correct;
            result.firstTotal = DecisionCount;
            result.firstWrong = new List<string>(wrong);
            result.firstTier = TierFor(eval.Correct);
            result.firstXp = XpFor(wrong.Count);
            if (!candidate.milestones.Contains(CheckedMilestone)) candidate.milestones.Add(CheckedMilestone);
        }
        result.checks++;
        result.lastRevision = revision;
        result.lastWrong = wrong;
        result.lastValues = new List<string>();
        foreach (Level3WorksheetDecision d in eval.Decisions) result.lastValues.Add(d.Id + "=" + d.Value);

        Level3CommandOutcome moved = Level3CommandOutcome.Applied;
        if (eval.AllCorrect) moved = Level3ChapterRules.TryAdvance(candidate, phase, Level3Phase.WorksheetReady, ReadyAction, out reason);
        else if (phase == Level3Phase.WorksheetDraft) moved = Level3ChapterRules.TryAdvance(candidate, phase, Level3Phase.WorksheetCorrections, FirstCheckAction, out reason);
        if (moved == Level3CommandOutcome.Refused) return CheckOutcome.Refused;
        reason = null;
        return CheckOutcome.Checked;
    }

    /// <summary>How a decision's place is marked after the last check: nothing, confirmed, or "look again".</summary>
    public enum Mark { None, Confirmed, LookAgain }

    /// <summary>
    /// The mark the last complete check left on a decision, shown only while
    /// the value it read is unchanged (a changed value waits for the next check).
    /// </summary>
    public static Mark MarkFor(Level3ChapterCheckpoint state, string decisionId)
    {
        Level3WorksheetResult result = state?.worksheetCheck;
        if (result == null || result.checks == 0 || result.lastValues == null) return Mark.None;
        string prefix = decisionId + "=";
        foreach (string entry in result.lastValues)
        {
            if (entry == null || !entry.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (entry.Substring(prefix.Length) != CurrentValue(state, decisionId)) return Mark.None;
            return result.lastWrong != null && result.lastWrong.Contains(decisionId) ? Mark.LookAgain : Mark.Confirmed;
        }
        return Mark.None;
    }

    // =========================================================
    // COLLECT (the supported worksheet, once)
    // =========================================================

    public static bool Collected(Level3ChapterCheckpoint state) =>
        state != null && state.actionIds != null && state.actionIds.Contains(AcquireAction) &&
        state.Case(Level3ChapterRules.BeaCaseId) != null && state.Case(Level3ChapterRules.BeaCaseId).workIds.Contains(WorksheetWorkId);

    /// <summary>
    /// The player collects the ready worksheet on the candidate:
    /// WorksheetReady -> BeaComputation and the worksheet into Bea's folder.
    /// Refused unless it is ready and still checks out; Duplicate once collected.
    /// </summary>
    public static Level3CommandOutcome Acquire(Level3ChapterCheckpoint candidate, CaseDefinition definition, out string reason)
    {
        reason = null;
        if (candidate != null && candidate.actionIds != null && candidate.actionIds.Contains(AcquireAction)) { reason = "already collected"; return Level3CommandOutcome.Duplicate; }
        if (candidate == null || Level3ChapterRules.PhaseOf(candidate) != Level3Phase.WorksheetReady || Level3ChapterRules.Problem(candidate) != null ||
            candidate.worksheetCheck == null || !candidate.worksheetCheck.firstChecked)
        {
            reason = "Check the worksheet first: every decision must check out before you collect it.";
            return Level3CommandOutcome.Refused;
        }
        // T06 revision: every machine batch checks out (a pre-revision worksheet made ready on the old board does too).
        if (!AllBatchesCorrect(candidate, definition)) { reason = "The worksheet needs another check before you collect it."; return Level3CommandOutcome.Refused; }
        Level3CommandOutcome moved = Level3ChapterRules.TryAdvance(candidate, Level3Phase.WorksheetReady, Level3Phase.BeaComputation, AcquireAction, out reason);
        if (moved != Level3CommandOutcome.Applied) return moved;
        Level3CaseProgress bea = candidate.Case(Level3ChapterRules.BeaCaseId);
        if (!bea.workIds.Contains(WorksheetWorkId)) bea.workIds.Add(WorksheetWorkId);
        if (!candidate.milestones.Contains(AcquiredMilestone)) candidate.milestones.Add(AcquiredMilestone);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>
    /// The collection's campaign side, applied to the SAME candidate save as
    /// the collected chapter: the activity once, +2 Public Trust and 60 coins
    /// (CampaignRewards catalogue), a once-only mastery claim for the frozen
    /// first-check award, and the Worth the Tax? badge at its frozen tier.
    /// The frozen XP itself stays in the chapter snapshot and joins the career
    /// total with the chapter result (T14), as Level 2's quest XP does with its
    /// level result: no level record is written early (a level record means a
    /// completed level to the save's repair). A campaign already paid changes
    /// nothing (AlreadyClaimed).
    /// </summary>
    public static Level3WorksheetGrants ApplyAcquisitionGrants(PlayerProgressData candidate, Level3ChapterCheckpoint state)
    {
        var grants = new Level3WorksheetGrants();
        if (candidate == null || !candidate.HasCampaign || !Collected(state) || state.worksheetCheck == null || !state.worksheetCheck.firstChecked) return grants;
        grants.Xp = state.worksheetCheck.firstXp;
        grants.Tier = state.worksheetCheck.firstTier;
        if (!CampaignRewards.CompleteActivity(candidate, CampaignRewards.Level3, CampaignRewards.BeaWorksheet))
        {
            grants.AlreadyClaimed = true;
            return grants;
        }
        grants.Changed = true;
        grants.Trust = CampaignRewards.Grant(candidate, CampaignRewards.Level3, CampaignRewards.BeaWorksheet, CampaignRewards.TrustKind);
        grants.Coins = CampaignRewards.Grant(candidate, CampaignRewards.Level3, CampaignRewards.BeaWorksheet, CampaignRewards.CoinsKind);
        string mastery = MasteryClaimId(candidate);
        if (!candidate.campaign.rewardClaims.Contains(mastery))
        {
            candidate.campaign.rewardClaims.Add(mastery);
            grants.MasteryClaimed = true;
        }
        candidate.RecordBadge(BadgeId, grants.Tier);
        return grants;
    }

    /// <summary>Acquire the separate summary in the same candidate as chapter/rewards. Legacy folder snapshots are retained as historical records.</summary>
    public static InventoryResult AcquireSummary(CampaignInventoryRecord inventory, InventoryCatalogue catalogue, Level3ChapterCheckpoint state, SlotRef? target = null, CaseDefinition definition = null, string actionId = "Level_03/summary-item")
    {
        if (inventory == null || catalogue == null) return InventoryResult.Reject("Inventory is not ready.");
        if (!Collected(state)) return InventoryResult.Reject("Finish checking every batch first.");
        if (InventoryRules.Owns(inventory, InventoryCatalogue.BeaSummary))
            return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = InventoryCatalogue.BeaSummary, Location = InventoryRules.Find(inventory, InventoryCatalogue.BeaSummary) };
        if (target.HasValue)
        {
            if (!target.Value.IsSlot || !InventoryRules.IsUsable(inventory, target.Value)) return InventoryResult.Reject("Choose an unlocked slot.");
            InventorySlot slot = InventoryRules.SlotAt(inventory, target.Value);
            if (!slot.IsEmpty && (target.Value.Area == InventoryArea.Bag || InventoryRules.FirstEmpty(inventory.bag, inventory.bagCapacity) < 0))
                return InventoryResult.Reject("Make room in your Bag first.");
        }
        else if (InventoryRules.FirstEmpty(inventory.hotbar, inventory.hotbarCapacity) < 0 && InventoryRules.FirstEmpty(inventory.bag, inventory.bagCapacity) < 0) return InventoryResult.Reject(InventoryRules.BagFull);
        var info = new InventoryItemRecord { itemId = InventoryCatalogue.BeaSummary, source = "Bea's table", workingDocumentId = WorksheetWorkId };
        if (definition != null)
        {
            List<string> lines = ReferenceLines(state, definition);
            for (int start = 0; start < lines.Count; start += 6)
                info.referencePages.Add(new InventoryReferencePage { id = definition.caseId + "/" + WorksheetWorkId + "/" + start,
                    title = SummaryTitle, lines = lines.GetRange(start, Math.Min(6, lines.Count - start)) });
        }
        InventoryItemRecord folder = InventoryRules.Record(inventory, InventoryCatalogue.CaseFolder);
        string prefix = Level3ChapterRules.BeaCaseId + "/" + WorksheetWorkId;
        if (folder != null && info.referencePages.Count == 0)
            foreach (InventoryReferencePage page in folder.referencePages)
                if (page.id == prefix || page.id.StartsWith(prefix + "/", StringComparison.Ordinal)) info.referencePages.Add(page);
        InventoryResult result = InventoryRules.Acquire(inventory, catalogue, InventoryCatalogue.BeaSummary, 1, actionId, target, info);
        if (result.Ok)
            foreach (InventoryReferencePage page in info.referencePages) InventoryRules.KeepReference(inventory, InventoryCatalogue.BeaSummary, page);
        if (result.Ok && folder != null)
        {
            folder.contents.RemoveAll(id => id == prefix);
            folder.referencePages.RemoveAll(page => page.id == prefix || page.id.StartsWith(prefix + "/", StringComparison.Ordinal));
        }
        return result;
    }

    /// <summary>The worksheet's once-only mastery claim (campaign/Level_03/bea-worksheet/mastery).</summary>
    public static string MasteryClaimId(PlayerProgressData data) =>
        CampaignRewards.ClaimId(data?.campaign?.campaignId, CampaignRewards.Level3, CampaignRewards.BeaWorksheet, CampaignRewards.MasteryKind);

    // =========================================================
    // THE COLLECTED WORKSHEET AS A READ-ONLY PAPER (Bag reference page)
    // =========================================================

    /// <summary>The player's own checked worksheet, section by section, for its read-only page in the Case Folder.</summary>
    public static List<string> ReferenceLines(Level3ChapterCheckpoint state, IList<Level3WorksheetRow> rows)
    {
        var lines = new List<string>();
        Level3WorksheetDraft draft = state?.worksheet;
        if (draft == null || rows == null) return lines;
        foreach (string section in Level3WorksheetRules.Sections)
        {
            var parts = new List<string>();
            foreach (Level3StripRecord strip in Level3WorksheetRules.InSection(draft, section))
            {
                Level3WorksheetRow row = Find(rows, strip.rowId);
                if (row != null) parts.Add(row.Label + " " + Level3Calculator.Format(row.Amount) + " (" + row.SourceId + ")");
            }
            lines.Add(Level3WorksheetRules.SectionLabel(section) + ": " + (parts.Count > 0 ? string.Join("; ", parts) : "none"));
            Level3TapeRecord tape = SubtotalTape(state, section);
            if (tape != null) lines.Add(Level3WorksheetRules.SectionLabel(section) + " tape: " + string.Join("  ", Level3Calculator.TapeLines(tape.expression, tape.result)));
        }
        foreach (string noteId in Level3WorksheetRules.Notes)
        {
            Level3NoteRecord note = draft.Note(noteId);
            if (note == null) continue;
            Level3WorksheetRow strip = Find(rows, Level3WorksheetRules.NoteStrip(noteId));
            string line = (strip != null ? strip.Label : noteId) + " note: " + Level3WorksheetRules.ReasonLabel(note.reason);
            Level3WorksheetRow related = Find(rows, note.related);
            if (related != null) line += ", related to " + related.Label;
            lines.Add(line);
        }
        if (state.worksheetCheck != null && state.worksheetCheck.firstChecked)
            lines.Add("First check: " + state.worksheetCheck.firstCorrect + "/" + state.worksheetCheck.firstTotal + " (" + TierName(state.worksheetCheck.firstTier) + ")");
        return lines;
    }

    // =========================================================
    // T06 REVISED (5 November 2026): THREE MACHINE BATCHES, NINE DECISIONS
    // (assessment L3-worksheet-v2, REVISION_PLAN registry)
    // =========================================================
    //
    // The lever files; only a deliberate Check batch grades. Each batch's FIRST
    // complete check freezes its own outcomes once (checkedBatches, firstWrong,
    // firstCorrect, firstTotal); a recheck only refreshes the marks and never
    // improves them. A batch is accepted (read-only, the next one opens) only by
    // a saved check that finds every one of its decisions right; the last one
    // moves the chapter to WorksheetReady. A chapter whose first check was frozen
    // under the twelve-decision v1 board keeps that grade exactly: its batch
    // checks mark and accept but never freeze a second grade.

    public const string RevisedAssessmentVersion = "L3-worksheet-v2";
    public const int RevisedDecisionCount = 9;
    public const int RevisedGoldAt = 9;
    public const int RevisedSilverAt = 7;
    /// <summary>XP = 60 minus first-check wrong decisions: 51 at worst.</summary>
    public const int RevisedMinXp = MaxXp - RevisedDecisionCount * XpPerWrong;
    public const string RefuseBatchStale = "Bea's records changed meanwhile. Look at the tabs, then check again.";
    public const string RefuseBatchUnchanged = "Nothing has changed since your last check.";
    /// <summary>The collected item's presentation title (the same B-WORKSHEET identity; no second item).</summary>
    public const string SummaryTitle = "Bea's Income & Expense Summary";
    public const string SupportedDeductionsLabel = "Supported deductions";

    /// <summary>The nine scored decisions in reveal order: seven treatments, then the R6/R7 reasons.</summary>
    public static readonly string[] RevisedDecisionIds =
        { "dest-R1", "dest-R2", "dest-R3", "dest-R4", "dest-R5", "dest-R6", "dest-R7", "reason-R6", "reason-R7" };

    private static readonly string[] EarningsDecisions = { "dest-R1", "dest-R2" };
    private static readonly string[] CostsDecisions = { "dest-R3", "dest-R4", "dest-R5" };
    private static readonly string[] QuestionsDecisions = { "dest-R6", "dest-R7", "reason-R6", "reason-R7" };

    /// <summary>A batch's scored decisions: 2 (earnings), 3 (costs), 4 (questions: two treatments, two reasons).</summary>
    public static IReadOnlyList<string> DecisionsInBatch(string batch) =>
        batch == Level3WorksheetRules.EarningsBatch ? EarningsDecisions : batch == Level3WorksheetRules.CostsBatch ? CostsDecisions
        : batch == Level3WorksheetRules.QuestionsBatch ? QuestionsDecisions : (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>"dest-R6" / "reason-R6" -> "R6" (null for anything else).</summary>
    public static string RowOfDecision(string decisionId)
    {
        if (decisionId == null) return null;
        if (decisionId.StartsWith("dest-", StringComparison.Ordinal)) return decisionId.Substring(5);
        if (decisionId.StartsWith("reason-", StringComparison.Ordinal)) return decisionId.Substring(7);
        return null;
    }

    public static bool IsReasonDecision(string decisionId) => decisionId != null && decisionId.StartsWith("reason-", StringComparison.Ordinal);

    public static string BatchOfDecision(string decisionId) =>
        Array.IndexOf(RevisedDecisionIds, decisionId) < 0 ? null : Level3WorksheetRules.BatchOf(RowOfDecision(decisionId));

    /// <summary>Gold 9/9, Silver 7-8, Bronze 0-6 (BadgeTier as a number).</summary>
    public static int RevisedTierFor(int correct) => correct >= RevisedGoldAt ? 3 : correct >= RevisedSilverAt ? 2 : 1;

    public static int RevisedXpFor(int wrong) => Math.Max(RevisedMinXp, MaxXp - Math.Max(0, wrong) * XpPerWrong);

    /// <summary>The first check was frozen under the twelve-decision board (a continued pre-revision save): that grade is final.</summary>
    public static bool LegacyGraded(Level3ChapterCheckpoint state) =>
        state?.worksheetCheck != null && state.worksheetCheck.firstChecked && state.worksheetCheck.assessmentVersion != RevisedAssessmentVersion;

    /// <summary>A machine batch check has been saved (the revised assessment has begun).</summary>
    public static bool MachineChecked(Level3ChapterCheckpoint state)
    {
        Level3WorksheetResult r = state?.worksheetCheck;
        return r != null && (r.assessmentVersion == RevisedAssessmentVersion || (r.checkedBatches != null && r.checkedBatches.Count > 0) ||
                             (r.acceptedBatches != null && r.acceptedBatches.Count > 0) || !string.IsNullOrEmpty(r.lastBatch));
    }

    /// <summary>The batch's first outcomes are frozen.</summary>
    public static bool BatchFrozen(Level3ChapterCheckpoint state, string batch) =>
        state?.worksheetCheck?.checkedBatches != null && state.worksheetCheck.checkedBatches.Contains(batch);

    /// <summary>The player's own revised value: a treatment key ("income", "deduct", "leave-out") or a reason key; "" while not filed.</summary>
    public static string RevisedValue(Level3ChapterCheckpoint state, string decisionId)
    {
        string row = RowOfDecision(decisionId);
        if (row == null || state?.worksheet == null) return "";
        return IsReasonDecision(decisionId) ? Level3WorksheetRules.SavedReason(state.worksheet, row) : Level3WorksheetRules.SavedTreatment(state.worksheet, row);
    }

    /// <summary>The content's answer in revised keys, read from the same decisions the v1 check reads (never copied into a save).</summary>
    private static string RevisedExpected(CaseDefinition definition, string decisionId)
    {
        string expected = Expected(definition, decisionId);
        if (expected == null) return null;
        if (IsReasonDecision(decisionId))
        {
            foreach (string reason in Level3WorksheetRules.Reasons)
                if (Level3WorksheetRules.ReasonLabel(reason) == expected) return reason;
            return null;
        }
        foreach (string section in Level3WorksheetRules.Sections)
            if (Level3WorksheetRules.SectionLabel(section) == expected) return Level3WorksheetRules.RevisedDestination(section);
        return null;
    }

    /// <summary>
    /// One batch: completeness (every row filed, R6/R7 with their reason, slips matching their
    /// originals), then (when complete) its decisions against the content. Reads only.
    /// </summary>
    public static Level3BatchEvaluation EvaluateBatch(Level3ChapterCheckpoint state, CaseDefinition definition, string batch)
    {
        var eval = new Level3BatchEvaluation { Batch = batch };
        List<Level3WorksheetRow> rows = Level3WorksheetRules.RowsOf(definition);
        IReadOnlyList<string> batchRows = Level3WorksheetRules.RowsInBatch(batch);
        if (rows.Count != Level3WorksheetRules.RowIds.Length || batchRows.Count == 0)
        {
            eval.MissingRows.Add("content");
            eval.MissingLines.Add("Bea's records did not load.");
            return eval;
        }
        Level3WorksheetDraft draft = state?.worksheet ?? new Level3WorksheetDraft();
        foreach (string rowId in batchRows)
        {
            Level3WorksheetRow row = Find(rows, rowId);
            Level3StripRecord strip = draft.Strip(rowId);
            string missing = null;
            if (strip == null || Level3WorksheetRules.RevisedDestination(strip.section) == "") missing = row.Label + " still needs filing.";
            else if (strip.sourceId != row.SourceId || !SameAmount(strip.amount, row.Amount)) missing = row.Label + "'s slip does not match its original.";
            else if (Level3WorksheetRules.NeedsReason(rowId) && Level3WorksheetRules.SavedReason(draft, rowId) == "") missing = row.Label + " needs a reason.";
            if (missing == null) continue;
            eval.MissingRows.Add(rowId);
            eval.MissingLines.Add(missing);
        }
        if (eval.MissingRows.Count > 0) return eval;
        eval.Complete = true;
        foreach (string id in DecisionsInBatch(batch))
        {
            string value = RevisedValue(state, id);
            string expected = RevisedExpected(definition, id);
            bool correct = expected != null && expected == value;
            eval.Decisions.Add(new Level3WorksheetDecision { Id = id, Correct = correct, Value = value });
            if (correct) eval.Correct++;
        }
        return eval;
    }

    /// <summary>Every batch is complete and right (the summary may be collected). Reads only.</summary>
    public static bool AllBatchesCorrect(Level3ChapterCheckpoint state, CaseDefinition definition)
    {
        foreach (string batch in Level3WorksheetRules.Batches)
            if (!EvaluateBatch(state, definition, batch).AllCorrect) return false;
        return true;
    }

    /// <summary>
    /// The deliberate Check batch on the candidate (the open batch only, made on draft revision basedOn).
    /// Incomplete / Unchanged / Refused change nothing. Checked: the first complete check of the batch
    /// froze its outcomes (eval.Froze; never for a legacy grade), the marks were refreshed, and an
    /// all-right batch was accepted (eval.Accepted; the last one also made the worksheet ready).
    /// </summary>
    public static CheckOutcome CheckBatch(Level3ChapterCheckpoint candidate, CaseDefinition definition, string batch, int basedOn,
        out Level3BatchEvaluation eval, out string reason)
    {
        eval = null;
        reason = null;
        if (candidate == null || !Level3WorksheetRules.Editable(candidate)) { reason = "The batch cannot be checked right now."; return CheckOutcome.Refused; }
        if (Array.IndexOf(Level3WorksheetRules.Batches, batch) < 0) { reason = "That is not one of Bea's batches."; return CheckOutcome.Refused; }
        if (Level3WorksheetRules.BatchAccepted(candidate, batch)) { reason = "That batch is already accepted: you can read it, not change it."; return CheckOutcome.Refused; }
        if (batch != Level3WorksheetRules.OpenBatch(candidate)) { reason = "That batch is not open yet."; return CheckOutcome.Refused; }
        if (candidate.worksheet == null) candidate.worksheet = new Level3WorksheetDraft();
        if (candidate.worksheetCheck == null) candidate.worksheetCheck = new Level3WorksheetResult();
        if (basedOn != candidate.worksheet.revision) { reason = RefuseBatchStale; return CheckOutcome.Refused; }

        eval = EvaluateBatch(candidate, definition, batch);
        if (!eval.Complete) { reason = "Not checked yet: " + (eval.MissingLines.Count > 0 ? eval.MissingLines[0] : "something is missing."); return CheckOutcome.Incomplete; }
        Level3WorksheetResult result = candidate.worksheetCheck;
        int revision = candidate.worksheet.revision;
        if (result.checks > 0 && result.lastBatch == batch && result.lastRevision == revision) { reason = RefuseBatchUnchanged; return CheckOutcome.Unchanged; }

        List<string> wrong = eval.WrongIds();
        if (!LegacyGraded(candidate) && !BatchFrozen(candidate, batch))
        {
            if (result.checkedBatches == null) result.checkedBatches = new List<string>();
            if (result.firstWrong == null) result.firstWrong = new List<string>();
            if (result.checkedBatches.Count == 0)
            {
                result.assessmentVersion = RevisedAssessmentVersion;
                result.firstRevision = revision;
            }
            result.checkedBatches.Add(batch);
            foreach (string id in wrong) if (!result.firstWrong.Contains(id)) result.firstWrong.Add(id);
            result.firstCorrect += eval.Correct;
            result.firstTotal += eval.Decisions.Count;
            eval.Froze = true;
            bool allFrozen = true;
            foreach (string b in Level3WorksheetRules.Batches) allFrozen &= result.checkedBatches.Contains(b);
            if (allFrozen)
            {
                // The one final result: nine first-check decisions, Gold 9 / Silver 7-8 / Bronze 0-6, XP 60 - wrong.
                result.firstChecked = true;
                result.firstTier = RevisedTierFor(result.firstCorrect);
                result.firstXp = RevisedXpFor(result.firstWrong.Count);
            }
            if (!candidate.milestones.Contains(CheckedMilestone)) candidate.milestones.Add(CheckedMilestone);
        }
        result.checks++;
        result.lastRevision = revision;
        result.lastBatch = batch;
        result.lastWrong = wrong;
        result.lastValues = new List<string>();
        foreach (Level3WorksheetDecision d in eval.Decisions) result.lastValues.Add(d.Id + "=" + d.Value);

        if (eval.AllCorrect)
        {
            if (result.acceptedBatches == null) result.acceptedBatches = new List<string>();
            result.acceptedBatches.Add(batch);
            eval.Accepted = true;
            if (Level3WorksheetRules.OpenBatch(candidate) == null)
            {
                Level3Phase phase = Level3ChapterRules.PhaseOf(candidate);
                if (Level3ChapterRules.TryAdvance(candidate, phase, Level3Phase.WorksheetReady, ReadyAction, out reason) == Level3CommandOutcome.Refused)
                    return CheckOutcome.Refused;
                eval.Ready = true;
            }
        }
        reason = null;
        return CheckOutcome.Checked;
    }

    /// <summary>
    /// A decision's mark from the last saved machine check, shown only while its value is unchanged
    /// (a refile goes neutral until the next deliberate check).
    /// </summary>
    public static Mark BatchMark(Level3ChapterCheckpoint state, string decisionId)
    {
        Level3WorksheetResult result = state?.worksheetCheck;
        string batch = BatchOfDecision(decisionId);
        if (result == null || batch == null || result.checks == 0 || result.lastValues == null || result.lastBatch != batch) return Mark.None;
        string prefix = decisionId + "=";
        foreach (string entry in result.lastValues)
        {
            if (entry == null || !entry.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (entry.Substring(prefix.Length) != RevisedValue(state, decisionId)) return Mark.None;
            return result.lastWrong != null && result.lastWrong.Contains(decisionId) ? Mark.LookAgain : Mark.Confirmed;
        }
        return Mark.None;
    }

    /// <summary>A record's tab mark: look again when any of its decisions is; confirmed when all of them are; otherwise none.</summary>
    public static Mark RowMark(Level3ChapterCheckpoint state, string rowId)
    {
        Mark dest = BatchMark(state, "dest-" + rowId);
        if (!Level3WorksheetRules.NeedsReason(rowId)) return dest;
        Mark why = BatchMark(state, "reason-" + rowId);
        if (dest == Mark.LookAgain || why == Mark.LookAgain) return Mark.LookAgain;
        return dest == Mark.Confirmed && why == Mark.Confirmed ? Mark.Confirmed : Mark.None;
    }

    /// <summary>The decisions of a batch the last check marked "look again" and whose values are still as checked (reveal order).</summary>
    public static List<string> LookAgainIn(Level3ChapterCheckpoint state, string batch)
    {
        var ids = new List<string>();
        foreach (string id in DecisionsInBatch(batch))
            if (BatchMark(state, id) == Mark.LookAgain) ids.Add(id);
        return ids;
    }

    // ---------- the collected summary (existing B-WORKSHEET) ----------

    /// <summary>
    /// Exact totals of every saved slip's printed amount by its treatment (Income / Deduct; Leave Out
    /// counts in neither). The collected summary's figures and the office's worksheet totals.
    /// </summary>
    public static void SummaryTotals(Level3ChapterCheckpoint state, out decimal income, out decimal deductions)
    {
        income = 0m;
        deductions = 0m;
        Level3WorksheetDraft draft = state?.worksheet;
        if (draft?.strips == null) return;
        foreach (Level3StripRecord strip in draft.strips)
        {
            if (strip == null || Level3WorksheetRules.BatchOf(strip.rowId) == null || !Level3Calculator.TryParse(strip.amount, out decimal amount)) continue;
            string treatment = Level3WorksheetRules.RevisedDestination(strip.section);
            if (treatment == Level3WorksheetRules.Income) income += amount;
            else if (treatment == Level3WorksheetRules.Deduct) deductions += amount;
        }
    }

    /// <summary>"₱900,000.00": exact, from the decimal (never a float).</summary>
    public static string Peso(decimal value) => "₱" + Level3Calculator.Format(Level3Calculator.Canonical(value));

    /// <summary>One entry of the summary: its treatment, the record's printed label/amount and its source references (never an answer key).</summary>
    public struct SummaryEntry
    {
        public string RowId;
        public string Treatment;
        public string Label;
        public string Amount;
        /// <summary>The row's own source reference, then any original that supports it ("EXP-B26 · RENT-26").</summary>
        public string References;
        /// <summary>A left-out row's saved reason label ("" when none).</summary>
        public string Reason;
    }

    /// <summary>The player's filed rows in worksheet order (Income, then Deduct, then Leave Out), from the case content's printed facts.</summary>
    public static List<SummaryEntry> SummaryEntries(Level3ChapterCheckpoint state, CaseDefinition definition)
    {
        var entries = new List<SummaryEntry>();
        Level3CaseContent content = definition?.level3;
        List<Level3WorksheetRow> rows = Level3WorksheetRules.RowsOf(definition);
        Level3WorksheetDraft draft = state?.worksheet;
        if (content == null || draft == null || rows.Count == 0) return entries;
        foreach (string treatment in Level3WorksheetRules.Treatments)
            foreach (Level3WorksheetRow row in rows)
            {
                if (Level3WorksheetRules.SavedTreatment(draft, row.RowId) != treatment) continue;
                var refs = new List<string>();
                string own = content.sources?.Find(s => s != null && s.sourceId == row.SourceId)?.reference;
                if (!string.IsNullOrEmpty(own)) refs.Add(own);
                if (treatment == Level3WorksheetRules.Deduct && content.rows != null)
                    foreach (Level3RowDefinition support in content.rows)
                    {
                        if (support == null || support.relation != "supports:" + row.RowId) continue;
                        string reference = content.sources?.Find(s => s != null && s.sourceId == support.sourceId)?.reference;
                        if (!string.IsNullOrEmpty(reference) && !refs.Contains(reference)) refs.Add(reference);
                    }
                entries.Add(new SummaryEntry
                {
                    RowId = row.RowId, Treatment = treatment, Label = row.Label, Amount = Level3Calculator.Format(row.Amount),
                    References = string.Join(" · ", refs),
                    Reason = treatment == Level3WorksheetRules.LeaveOut ? Level3WorksheetRules.ReasonLabel(Level3WorksheetRules.SavedReason(draft, row.RowId)) : ""
                });
            }
        return entries;
    }

    /// <summary>
    /// The summary as an existing physical record (a plain working sheet, read in the one reader and in the
    /// Case Folder's Supporting Documents): front = the two exact totals; back = each entry with its
    /// supporting originals and the left-out records with their reasons. No taxable income, tax due,
    /// credit or balance: those are the office's work. Null without Bea's rows.
    /// </summary>
    public static ClientRecord SummaryRecord(CaseDefinition definition, Level3ChapterCheckpoint state)
    {
        Level3CaseContent content = definition?.level3;
        if (content == null || !content.IsSet || definition.caseId != Level3ChapterRules.BeaCaseId || state?.worksheet == null) return null;
        SummaryTotals(state, out decimal income, out decimal deductions);
        var record = new ClientRecord
        {
            id = Level3RecordRules.RecordId(definition.caseId, WorksheetWorkId),
            npcId = definition.caseId,
            type = ClientRecord.Plain,
            source = WorksheetWorkId,
            tab = "Summary",
            title = "INCOME & EXPENSE SUMMARY",
            name = content.taxpayer,
            issuer = content.taxpayer,
            reference = "Tax year " + content.taxYear,
            date = "Checked at Bea's table"
        };
        record.rows.Add(new ClientRecordRow { label = "Income", value = Peso(income) });
        record.rows.Add(new ClientRecordRow { label = SupportedDeductionsLabel, value = Peso(deductions) });
        foreach (SummaryEntry entry in SummaryEntries(state, definition))
        {
            string kind = entry.Treatment == Level3WorksheetRules.Income ? "Income" : entry.Treatment == Level3WorksheetRules.Deduct ? "Deduction" : "Left out";
            string why = entry.Treatment == Level3WorksheetRules.LeaveOut && entry.Reason.Length > 0 ? entry.Reason : entry.References;
            record.backRows.Add(new ClientRecordRow { label = kind + " · " + why, value = entry.Label + " ₱" + entry.Amount });
        }
        return record;
    }

    /// <summary>A worksheet graded on the retired board (v1) whose two subtotal tapes are current: its Bag page keeps the original layout.</summary>
    public static bool BoardPage(Level3ChapterCheckpoint state) =>
        LegacyGraded(state) && IsCurrent(state, Level3WorksheetRules.Income) && IsCurrent(state, Level3WorksheetRules.Expenses);

    /// <summary>
    /// The collected worksheet's read-only Bag page: the revised summary (totals, entries with their
    /// references, left-out records and reasons, the first-check result); a worksheet collected from the
    /// retired board (v1 grade, current tapes) keeps its original section-by-section page.
    /// </summary>
    public static List<string> ReferenceLines(Level3ChapterCheckpoint state, CaseDefinition definition)
    {
        List<Level3WorksheetRow> rows = Level3WorksheetRules.RowsOf(definition);
        if (BoardPage(state)) return ReferenceLines(state, rows);
        var lines = new List<string>();
        if (state?.worksheet == null || rows.Count == 0) return lines;
        SummaryTotals(state, out decimal income, out decimal deductions);
        lines.Add("Income: " + Peso(income));
        lines.Add(SupportedDeductionsLabel + ": " + Peso(deductions));
        foreach (SummaryEntry entry in SummaryEntries(state, definition))
        {
            if (entry.Treatment == Level3WorksheetRules.LeaveOut)
                lines.Add("Left out: " + entry.Label + " ₱" + entry.Amount + (entry.Reason.Length > 0 ? " · " + entry.Reason : ""));
            else
                lines.Add(entry.Label + " ₱" + entry.Amount + (entry.References.Length > 0 ? " · " + entry.References : ""));
        }
        Level3WorksheetResult result = state.worksheetCheck;
        if (result != null && result.firstChecked)
            lines.Add((LegacyGraded(state) ? "First check: " : "First checks: ") + result.firstCorrect + "/" + result.firstTotal + " (" + TierName(result.firstTier) + ")");
        return lines;
    }
}

/// <summary>What one Check batch found (T06 revised). Presentation reads it; only a complete check is saved.</summary>
public sealed class Level3BatchEvaluation
{
    public string Batch;
    /// <summary>Every row of the batch filed (R6/R7 with a reason) and matching its original.</summary>
    public bool Complete;
    public readonly List<string> MissingRows = new List<string>();
    public readonly List<string> MissingLines = new List<string>();
    /// <summary>The batch's decisions in reveal order (only when complete).</summary>
    public readonly List<Level3WorksheetDecision> Decisions = new List<Level3WorksheetDecision>();
    public int Correct;
    /// <summary>Set by CheckBatch: this check froze the batch's first outcomes (never on a recheck or for a legacy grade).</summary>
    public bool Froze;
    /// <summary>Set by CheckBatch: every decision right, the batch accepted (read-only; the next one opens).</summary>
    public bool Accepted;
    /// <summary>Set by CheckBatch: the last batch was accepted and the worksheet is ready to collect.</summary>
    public bool Ready;

    public int Wrong => Decisions.Count - Correct;
    public bool AllCorrect => Complete && Decisions.Count > 0 && Correct == Decisions.Count;

    public List<string> WrongIds()
    {
        var ids = new List<string>();
        foreach (Level3WorksheetDecision d in Decisions) if (!d.Correct) ids.Add(d.Id);
        return ids;
    }
}
