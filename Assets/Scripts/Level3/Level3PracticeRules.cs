using System;
using System.Collections.Generic;

/// <summary>A printed amount the calculator can take (a row on a paper, or an attached tape's result).</summary>
public sealed class Level3PracticeSource
{
    public string SourceId;
    public string Label;
    public string Amount;
}

/// <summary>A practice card slot a tape can be attached to.</summary>
public sealed class Level3PracticeSlot
{
    public string DestinationId;
    public string Label;
    /// <summary>The slot that must hold a tape first (null for the first slot).</summary>
    public string Requires;
    /// <summary>Plain hint shown when a tape does not fit this slot yet (never a grade).</summary>
    public string Hint;
    internal string Expected;
}

/// <summary>
/// PURPOSE:
/// The protected Level 3 calculator practice (T03, CONTENT "Protected tool
/// example"), pure C#: a fictional office budget card, 100 − paper order 20
/// = 80, then 80 − courier 8 = 72. It is NOT the player's wallet, not a tax
/// rule and never scored: no coins, trust, XP or first check change here.
///
/// ATTACH (AttachTape on a copy of the chapter snapshot): the slot must be
/// open (in order), the tape must be a real calculation (at least one
/// operation, re-evaluated exactly, not just a typed final number) whose
/// result fits the slot. A tape that does not fit is refused with a plain
/// hint and nothing changes. Attaching the second tape completes practice:
/// ToolPractice -> BeaRecords in the same candidate, so the save that keeps
/// the tape is the save that advances the chapter.
/// </summary>
public static class Level3PracticeRules
{
    public const string CaseId = "PRACTICE";
    public const string BudgetSource = "PRACTICE-BUDGET";
    public const string PaperSource = "PRACTICE-PAPER";
    public const string CourierSource = "PRACTICE-COURIER";
    public const string FirstSlot = "practice/after-paper";
    public const string SecondSlot = "practice/after-courier";
    /// <summary>The first attached tape, offered as a source for the second step.</summary>
    public const string FirstTapeSource = "tape:" + FirstSlot;
    public const string CompleteAction = "Level_03/practice-attached";
    public const string FirstMilestone = "practice-first-tape";
    public const string CompleteMilestone = "practice-complete";

    public const string RefuseNotNow = "The practice card is not in use right now.";
    public const string RefuseNoTape = "Press = to print a tape first.";
    public const string RefuseNoWorking = "Work it out on the calculator: use the card's amounts and an operation, then press =.";

    public static readonly Level3PracticeSource[] CardSources =
    {
        new Level3PracticeSource { SourceId = BudgetSource, Label = "Budget", Amount = "100.00" },
        new Level3PracticeSource { SourceId = PaperSource, Label = "Paper order", Amount = "20.00" },
        new Level3PracticeSource { SourceId = CourierSource, Label = "Courier", Amount = "8.00" }
    };

    public static readonly Level3PracticeSlot[] Slots =
    {
        new Level3PracticeSlot
        {
            DestinationId = FirstSlot, Label = "Left after paper order", Expected = "80",
            Hint = "This slot holds what is left of the budget after the paper order."
        },
        new Level3PracticeSlot
        {
            DestinationId = SecondSlot, Label = "Left after courier", Requires = FirstSlot, Expected = "72",
            Hint = "Start from what was left after the paper order, then take away the courier."
        }
    };

    public static Level3PracticeSlot Slot(string destinationId)
    {
        foreach (Level3PracticeSlot slot in Slots)
            if (slot.DestinationId == destinationId) return slot;
        return null;
    }

    /// <summary>The slot the player works on next (null when practice is complete).</summary>
    public static Level3PracticeSlot NextSlot(Level3ChapterCheckpoint state)
    {
        foreach (Level3PracticeSlot slot in Slots)
            if (state == null || state.Tape(slot.DestinationId) == null) return slot;
        return null;
    }

    public static bool IsComplete(Level3ChapterCheckpoint state) =>
        state != null && (Level3ChapterRules.PhaseOf(state) > Level3Phase.ToolPractice || NextSlot(state) == null);

    /// <summary>Sources on the card, plus the first tape's result once it is attached.</summary>
    public static List<Level3PracticeSource> Sources(Level3ChapterCheckpoint state)
    {
        var sources = new List<Level3PracticeSource>(CardSources);
        Level3TapeRecord first = state != null ? state.Tape(FirstSlot) : null;
        if (first != null)
            sources.Add(new Level3PracticeSource { SourceId = FirstTapeSource, Label = "Tape 1 (left after paper order)", Amount = first.result });
        return sources;
    }

    /// <summary>Stable action id of one slot's attachment (once per campaign chapter).</summary>
    public static string ActionFor(string destinationId) => "Level_03/attach/" + destinationId;

    /// <summary>
    /// Attaches the printed tape to a practice slot on the candidate. Applied:
    /// the tape (and, for the second slot, ToolPractice -> BeaRecords) are in
    /// the candidate, ready to write. Duplicate: that slot already holds a
    /// tape (a repeat or a restore). Refused: nothing changed; reason is a
    /// plain hint, never a grade.
    /// </summary>
    public static Level3CommandOutcome AttachTape(Level3ChapterCheckpoint candidate, string destinationId,
        IReadOnlyList<Level3CalcToken> expression, string result, out string reason)
    {
        reason = null;
        Level3PracticeSlot slot = Slot(destinationId);
        if (slot == null) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        if (candidate != null && candidate.Tape(destinationId) != null) { reason = "already attached"; return Level3CommandOutcome.Duplicate; }
        string problem = Level3ChapterRules.Problem(candidate);
        if (problem != null) { reason = problem; return Level3CommandOutcome.Refused; }
        if (Level3ChapterRules.PhaseOf(candidate) != Level3Phase.ToolPractice) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        if (slot.Requires != null && candidate.Tape(slot.Requires) == null) { reason = Slot(slot.Requires).Hint; return Level3CommandOutcome.Refused; }
        if (expression == null || expression.Count == 0 || result == null) { reason = RefuseNoTape; return Level3CommandOutcome.Refused; }

        int numbers = 0, operators = 0;
        foreach (Level3CalcToken token in expression)
        {
            if (token == null) { reason = RefuseNoWorking; return Level3CommandOutcome.Refused; }
            if (token.IsNumber) numbers++;
            else if (token.IsOperator) operators++;
        }
        if (numbers < 2 || operators < 1) { reason = RefuseNoWorking; return Level3CommandOutcome.Refused; }
        // The tape's own arithmetic, recomputed exactly: a printed result is never trusted as given.
        if (Level3Calculator.Evaluate(expression, out decimal value) != null ||
            !Level3Calculator.TryParse(result, out decimal printed) || value != printed)
        {
            reason = RefuseNoWorking;
            return Level3CommandOutcome.Refused;
        }
        if (!Level3Calculator.TryParse(slot.Expected, out decimal expected) || value != expected)
        {
            reason = slot.Hint;
            return Level3CommandOutcome.Refused;
        }

        var tape = new Level3TapeRecord
        {
            tapeId = "tape-" + destinationId.Replace('/', '-'),
            caseId = CaseId,
            destinationId = destinationId,
            result = Level3Calculator.Canonical(value),
            actionId = ActionFor(destinationId)
        };
        foreach (Level3CalcToken token in expression) tape.expression.Add(token.Copy());
        candidate.tapes.Add(tape);
        candidate.actionIds.Add(tape.actionId);
        if (destinationId == FirstSlot && !candidate.milestones.Contains(FirstMilestone)) candidate.milestones.Add(FirstMilestone);

        if (NextSlot(candidate) == null)
        {
            Level3CommandOutcome advanced = Level3ChapterRules.TryAdvance(candidate, Level3Phase.ToolPractice, Level3Phase.BeaRecords, CompleteAction, out reason);
            if (advanced != Level3CommandOutcome.Applied) return Level3CommandOutcome.Refused;
            if (!candidate.milestones.Contains(CompleteMilestone)) candidate.milestones.Add(CompleteMilestone);
        }
        return Level3CommandOutcome.Applied;
    }
}
