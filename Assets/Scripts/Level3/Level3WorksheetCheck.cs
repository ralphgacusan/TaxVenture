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
/// </summary>
public static class Level3WorksheetCheck
{
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
        if (!Evaluate(candidate, definition).AllCorrect) { reason = "The worksheet needs another check before you collect it."; return Level3CommandOutcome.Refused; }
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
}
