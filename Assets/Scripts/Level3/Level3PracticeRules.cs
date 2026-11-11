using System;
using System.Collections.Generic;

/// <summary>A printed amount the calculator can take (a row on a paper, or an attached tape's result).</summary>
public sealed class Level3PracticeSource
{
    public string SourceId;
    public string Label;
    public string Amount;
    /// <summary>A rate printed as a percentage ("0.15" prints 15% and enters the calculator through its % path).</summary>
    public bool Percent;
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

/// <summary>What a desk card prints: its header, note, the amounts it offers and its tape slots (presentation reads it).</summary>
public sealed class Level3CardSpec
{
    public string Id;
    public string Title;
    public string Note;
    public Level3PracticeSource[] Sources;
    public Level3PracticeSlot[] Slots;
    /// <summary>A short demonstration card (first-use lessons): the card prints its compact lesson layout.</summary>
    public bool IsLesson;
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
///
/// Revision (3 November 2026): the budget practice is retired (only version
/// 1 tapes remain, as references). Its place is the FIRST-USE LESSON (T03):
/// at Bea's computation, before her sheet comes up, the same card shows one
/// unrelated example (income minus supported costs, 6 November 2026; it was
/// a supply fund minus ink) and the player does it once: tap an amount, −,
/// the other amount, =, attach. CompleteLesson saves only the chapter's
/// calculatorTaught marker (no tape, coin, XP, trust or grade). Two more
/// short demonstrations (tax table, withholding) reuse the same card and
/// rules later in her computation; see FIRST-USE LESSONS below.
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

    // =========================================================
    // FIRST-USE LESSONS (revision, T03; teaching pass 6 November 2026)
    // =========================================================
    //
    // Three short desk demonstrations on the same clipboard and calculator,
    // each at the moment it is needed and each before Bea's own step:
    //  1. Taxable income (first office use): income 10,000 - supported costs
    //     2,000 = 8,000. Saves calculatorTaught (unchanged marker).
    //  2. Tax table (after Bea's taxable income is attached, before she picks
    //     a band): the book's Practice Design Studio, 350,000 in the band over
    //     250,000 to 400,000: (350,000 - 250,000) x 15% + 0 = 15,000.
    //  3. Withholding (after her certificate is collected, before her credit):
    //     the same studio, tax due 15,000 - tax withheld 2,000 = 13,000; its
    //     income payment 20,000 is printed too and is never used.
    // None is Bea's money, a grade, a reward, Trust or a timer: a wrong tape
    // gets one plain hint and stays on the calculator to try again. Each
    // saves only its own marker (calculatorTaught, or a chapter milestone and
    // action id) through the existing signed chapter write. A chapter already
    // past a demonstration's step never shows it (old saves are not reset).

    public const string LessonFund = "LESSON-INCOME";
    public const string LessonSpent = "LESSON-COSTS";
    public const string LessonSlot = "lesson/taxable-income";
    /// <summary>Saved once, with calculatorTaught (never a reward claim).</summary>
    public const string TaughtAction = "Level_03/calculator-taught";
    public const string RefuseFromCard = "Tap the amounts on the card, so the tape shows where each one came from.";

    public static readonly Level3PracticeSource[] LessonSources =
    {
        new Level3PracticeSource { SourceId = LessonFund, Label = "Income", Amount = "10000.00" },
        new Level3PracticeSource { SourceId = LessonSpent, Label = "Costs (supported)", Amount = "2000.00" }
    };

    public static readonly Level3PracticeSlot LessonTarget = new Level3PracticeSlot
    {
        DestinationId = LessonSlot, Label = "Taxable income", Expected = "8000",
        Hint = "Taxable income is income minus supported costs. Start from Income."
    };

    /// <summary>The retired budget card (version 1 tapes stay readable on it).</summary>
    public static readonly Level3CardSpec PracticeCard = new Level3CardSpec
    {
        Id = "practice", Title = "PRACTICE\nOFFICE BUDGET", Note = "Practice only · No score", Sources = CardSources, Slots = Slots
    };

    /// <summary>The first-use example: not Bea's money, never scored.</summary>
    public static readonly Level3CardSpec LessonCard = new Level3CardSpec
    {
        Id = "lesson", Title = "EXAMPLE\nTAXABLE INCOME", Note = "Not Bea's · No score",
        Sources = LessonSources, Slots = new[] { LessonTarget }, IsLesson = true
    };

    // ---------- 2. the tax table (Practice Design Studio, Tax Code Book Sections 7-8) ----------

    public const string TaxLessonId = "tax-table";
    public const string TaxIncome = "LESSON-STUDIO-INCOME";
    public const string TaxThreshold = "LESSON-STUDIO-THRESHOLD";
    public const string TaxRate = "LESSON-STUDIO-RATE";
    public const string TaxBase = "LESSON-STUDIO-BASE";
    public const string TaxSlot = "lesson/studio-tax-due";
    public const string TaxTaughtMilestone = "teach-tax-table";
    public const string TaxTaughtAction = "Level_03/teach/tax-table";
    public const string TaxNeedsRate = "Multiply the excess by the rate: × Rate, then =.";
    public const string TaxWholeIncome = "Apply the rate only to the excess: subtract the threshold first.";
    public const string TaxNeedsBase = "Add the band's base tax last, even when it is 0: + Base tax, then =.";
    public const string TaxOrder = "Follow the order: taxable income - threshold =, × rate =, + base tax =.";

    public static readonly Level3PracticeSource[] TaxSources =
    {
        new Level3PracticeSource { SourceId = TaxIncome, Label = "Taxable income", Amount = "350000.00" },
        new Level3PracticeSource { SourceId = TaxThreshold, Label = "Threshold", Amount = "250000.00" },
        new Level3PracticeSource { SourceId = TaxRate, Label = "Rate", Amount = "0.15", Percent = true },
        new Level3PracticeSource { SourceId = TaxBase, Label = "Base tax", Amount = "0.00" }
    };

    public static readonly Level3PracticeSlot TaxTarget = new Level3PracticeSlot
    {
        DestinationId = TaxSlot, Label = "Tax due", Expected = "15000", Hint = TaxOrder
    };

    /// <summary>Section 8's worked example: the band is printed from Section 7, the steps are the player's.</summary>
    public static readonly Level3CardSpec TaxCard = new Level3CardSpec
    {
        Id = "lesson-tax", Title = "EXAMPLE\nDESIGN STUDIO", Note = "Band: over 250,000 to 400,000",
        Sources = TaxSources, Slots = new[] { TaxTarget }, IsLesson = true
    };

    // ---------- 3. withholding (the same studio's certificate, Tax Code Book Section 9) ----------

    public const string CreditLessonId = "withholding";
    public const string CreditTaxDue = "LESSON-STUDIO-TAXDUE";
    public const string CreditPayment = "LESSON-STUDIO-PAYMENT";
    public const string CreditWithheld = "LESSON-STUDIO-WITHHELD";
    public const string CreditSlot = "lesson/studio-still-to-pay";
    public const string CreditTaughtMilestone = "teach-withholding";
    public const string CreditTaughtAction = "Level_03/teach/withholding";
    public const string CreditUsedPayment = "The income payment is already in income. Take away only the tax withheld.";
    public const string CreditAdded = "Withheld tax is already paid: take it away from tax due.";
    public const string CreditOrder = "Start from tax due, then take away the tax withheld.";

    public static readonly Level3PracticeSource[] CreditSources =
    {
        new Level3PracticeSource { SourceId = CreditTaxDue, Label = "Tax due", Amount = "15000.00" },
        new Level3PracticeSource { SourceId = CreditPayment, Label = "Income payment", Amount = "20000.00" },
        new Level3PracticeSource { SourceId = CreditWithheld, Label = "Tax withheld", Amount = "2000.00" }
    };

    public static readonly Level3PracticeSlot CreditTarget = new Level3PracticeSlot
    {
        DestinationId = CreditSlot, Label = "Still to pay", Expected = "13000", Hint = CreditOrder
    };

    public static readonly Level3CardSpec CreditCard = new Level3CardSpec
    {
        Id = "lesson-credit", Title = "EXAMPLE\nWITHHOLDING", Note = "A credit, not a cost",
        Sources = CreditSources, Slots = new[] { CreditTarget }, IsLesson = true
    };

    /// <summary>The three demonstrations in teaching order.</summary>
    public static readonly Level3CardSpec[] LessonCards = { LessonCard, TaxCard, CreditCard };

    public static bool Taught(Level3ChapterCheckpoint state) => state != null && state.calculatorTaught;
    public static bool TaxTaught(Level3ChapterCheckpoint state) => state?.milestones != null && state.milestones.Contains(TaxTaughtMilestone);
    public static bool CreditTaught(Level3ChapterCheckpoint state) => state?.milestones != null && state.milestones.Contains(CreditTaughtMilestone);

    /// <summary>The calculator's own first-use example: Bea's computation is open and the calculator was never taught.</summary>
    public static bool CalculatorLessonDue(Level3ChapterCheckpoint state) =>
        state != null && !state.calculatorTaught && Level3ChapterRules.PhaseOf(state) == Level3Phase.BeaComputation;

    /// <summary>
    /// Bea's sheet has not been handed in yet (a chapter already reviewed, e.g. a returned full redo, is past every
    /// demonstration). A stamp alone is not a hand-in: it can be pressed on an unfinished sheet and is re-stamped later.
    /// </summary>
    private static bool BeforeReview(Level3ChapterCheckpoint state)
    {
        Level3CaseProgress bea = state?.Case(Level3ChapterRules.BeaCaseId);
        return bea != null && (bea.review == null || bea.review.submissions == 0);
    }

    /// <summary>
    /// The tax-table example: her taxable income is attached and she has not picked a band, worked her tax
    /// due or been reviewed; never in a chapter already past that step.
    /// </summary>
    public static bool TaxLessonDue(Level3ChapterCheckpoint state)
    {
        if (state == null || !state.calculatorTaught || TaxTaught(state) || Level3ChapterRules.PhaseOf(state) != Level3Phase.BeaComputation || !BeforeReview(state)) return false;
        Level3ComputationRecord sheet = state.Case(Level3ChapterRules.BeaCaseId)?.computation;
        string bea = Level3ChapterRules.BeaCaseId;
        return Level3ComputationRules.TapeOf(state, bea, Level3ComputationRules.Taxable) != null &&
               (sheet == null || (string.IsNullOrEmpty(sheet.ruleId) && !sheet.taxDueNoted)) &&
               Level3ComputationRules.TapeOf(state, bea, Level3ComputationRules.TaxDue) == null;
    }

    /// <summary>The withholding example: her certificate is collected and no credit or balance is on her sheet yet.</summary>
    public static bool CreditLessonDue(Level3ChapterCheckpoint state)
    {
        if (state == null || !state.calculatorTaught || CreditTaught(state) || Level3ChapterRules.PhaseOf(state) != Level3Phase.BeaComputation ||
            !BeforeReview(state) || !Level3CertificateRules.Acquired(state)) return false;
        Level3ComputationRecord sheet = state.Case(Level3ChapterRules.BeaCaseId)?.computation;
        return sheet == null || (string.IsNullOrEmpty(sheet.creditChoice) && !sheet.payableNoted);
    }

    /// <summary>The demonstration standing in for Bea's sheet now (null: none). At most one is due at a time.</summary>
    public static Level3CardSpec CurrentLesson(Level3ChapterCheckpoint state) =>
        CalculatorLessonDue(state) ? LessonCard : TaxLessonDue(state) ? TaxCard : CreditLessonDue(state) ? CreditCard : null;

    /// <summary>A demonstration stands in for Bea's sheet: her sheet is read-only until it is done.</summary>
    public static bool LessonDue(Level3ChapterCheckpoint state) => CurrentLesson(state) != null;

    /// <summary>The demonstration card with this id (null for anything else).</summary>
    public static Level3CardSpec LessonById(string id) => Array.Find(LessonCards, c => c.Id == id);

    /// <summary>The one slot of a demonstration card (its tape's destination).</summary>
    public static string SlotOf(Level3CardSpec lesson) => lesson?.Slots != null && lesson.Slots.Length > 0 ? lesson.Slots[0].DestinationId : null;

    /// <summary>
    /// The first-use example (kept signature): its tape attached to its slot. See CompleteLesson(lesson, ...).
    /// </summary>
    public static Level3CommandOutcome CompleteLesson(Level3ChapterCheckpoint candidate, string destinationId,
        IReadOnlyList<Level3CalcToken> expression, string result, out string reason)
    {
        reason = null;
        if (destinationId != LessonSlot) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        return CompleteLesson(candidate, LessonCard, expression, result, out reason);
    }

    /// <summary>
    /// A demonstration's one required action, on a copy: a printed tape (a chain for the tax table) whose
    /// numbers were all taken from that card, with the demonstrated operations, re-evaluated exactly and
    /// equal to what the slot holds. Applied: only its marker and action id are in the candidate (the
    /// calculator lesson: calculatorTaught; the others: a chapter milestone). Duplicate: already taught (a
    /// repeat or a restore). Refused: nothing changed; the reason is one plain hint, never a grade.
    /// </summary>
    public static Level3CommandOutcome CompleteLesson(Level3ChapterCheckpoint candidate, Level3CardSpec lesson,
        IReadOnlyList<Level3CalcToken> expression, string result, out string reason)
    {
        reason = null;
        if (lesson == null || Array.IndexOf(LessonCards, lesson) < 0) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        if (lesson == LessonCard ? Taught(candidate) : lesson == TaxCard ? TaxTaught(candidate) : CreditTaught(candidate))
        {
            reason = "already taught";
            return Level3CommandOutcome.Duplicate;
        }
        string problem = Level3ChapterRules.Problem(candidate);
        if (problem != null) { reason = problem; return Level3CommandOutcome.Refused; }
        if (CurrentLesson(candidate) != lesson) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        if (expression == null || expression.Count == 0 || result == null) { reason = RefuseNoTape; return Level3CommandOutcome.Refused; }

        int numbers = 0;
        var ops = new HashSet<string>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (Level3CalcToken token in expression)
        {
            if (token == null) { reason = RefuseNoWorking; return Level3CommandOutcome.Refused; }
            if (token.IsOperator) { if (token.op != Level3Calculator.ChainStep) ops.Add(token.op); continue; }
            if (!token.IsNumber) continue;
            numbers++;
            if (token.manual || Array.Find(lesson.Sources, s => s.SourceId == token.sourceId) == null) { reason = RefuseFromCard; return Level3CommandOutcome.Refused; }
            used.Add(token.sourceId);
        }
        if (numbers < 2 || ops.Count == 0) { reason = RefuseNoWorking; return Level3CommandOutcome.Refused; }
        // The tape's own arithmetic, recomputed exactly: a printed result is never trusted as given.
        if (Level3Calculator.EvaluateChain(expression, out decimal value) != null ||
            !Level3Calculator.TryParse(result, out decimal printed) || value != printed)
        {
            reason = RefuseNoWorking;
            return Level3CommandOutcome.Refused;
        }
        Level3PracticeSlot slot = lesson.Slots[0];
        Level3Calculator.TryParse(slot.Expected, out decimal expected);
        string hint = Diagnose(lesson, ops, used, value, expected);
        if (hint != null) { reason = hint; return Level3CommandOutcome.Refused; }

        if (lesson == LessonCard)
        {
            candidate.calculatorTaught = true;
            if (!candidate.actionIds.Contains(TaughtAction)) candidate.actionIds.Add(TaughtAction);
        }
        else
        {
            string milestone = lesson == TaxCard ? TaxTaughtMilestone : CreditTaughtMilestone;
            string action = lesson == TaxCard ? TaxTaughtAction : CreditTaughtAction;
            if (!candidate.milestones.Contains(milestone)) candidate.milestones.Add(milestone);
            if (!candidate.actionIds.Contains(action)) candidate.actionIds.Add(action);
        }
        return Level3CommandOutcome.Applied;
    }

    // ---------- Arthur first (follow-up, 6 November 2026: Level 2's watch-then-try pattern) ----------
    // Each example opens with Arthur doing it himself on the same card and calculator (the amounts light up,
    // keys press, the tape prints and clips on) while he explains; then everything is put back and he says
    // "your turn". The player repeats the same example (CompleteLesson above), then Bea's own step follows.
    // Watching to the end saves a "demo-<card id>" milestone, so it is not shown again; an interrupted
    // demonstration replays on the next lift. Nothing in a demonstration is a tape, grade, reward or Bea's work.

    /// <summary>One beat of Arthur's demonstration: a line to say, then (optionally) an amount, a key or the attach.</summary>
    public struct LessonDemoStep
    {
        public string Line;
        public string SourceId;
        public bool HasKey;
        public Level3CalcKey Key;
        public bool Attach;
    }

    private static LessonDemoStep Say(string line) => new LessonDemoStep { Line = line };
    private static LessonDemoStep Take(string sourceId, string line = null) => new LessonDemoStep { Line = line, SourceId = sourceId };
    private static LessonDemoStep Press(Level3CalcKey key, string line = null) => new LessonDemoStep { Line = line, HasKey = true, Key = key };
    private static LessonDemoStep AttachTo(string line) => new LessonDemoStep { Line = line, Attach = true };

    public static string DemoMilestone(Level3CardSpec lesson) => lesson != null ? "demo-" + lesson.Id : null;

    /// <summary>Arthur's demonstration of this example was watched to the end, or the example itself is done.</summary>
    public static bool DemoSeen(Level3ChapterCheckpoint state, Level3CardSpec lesson)
    {
        if (state == null || lesson == null) return false;
        bool taught = lesson == LessonCard ? Taught(state) : lesson == TaxCard ? TaxTaught(state) : CreditTaught(state);
        return taught || state.milestones != null && state.milestones.Contains(DemoMilestone(lesson));
    }

    /// <summary>The demonstration of the example that is due now was watched (on a copy): its milestone only.</summary>
    public static Level3CommandOutcome MarkDemoSeen(Level3ChapterCheckpoint candidate, Level3CardSpec lesson, out string reason)
    {
        reason = null;
        if (lesson == null || Array.IndexOf(LessonCards, lesson) < 0) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        if (DemoSeen(candidate, lesson)) { reason = "already seen"; return Level3CommandOutcome.Duplicate; }
        if (Level3ChapterRules.Problem(candidate) != null || CurrentLesson(candidate) != lesson) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        candidate.milestones.Add(DemoMilestone(lesson));
        string action = "Level_03/demo/" + lesson.Id;
        if (!candidate.actionIds.Contains(action)) candidate.actionIds.Add(action);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>Arthur's demonstration of an example: his lines and the moves he makes on the real card and calculator.</summary>
    public static List<LessonDemoStep> Demo(Level3CardSpec lesson)
    {
        if (lesson == TaxCard)
            return new List<LessonDemoStep>
            {
                Say("Bea's tax comes from the annual table. Watch me do the studio example first."),
                Say("Its taxable income is 350,000. In Section 7 that's the band over 250,000 to 400,000."),
                Take(TaxIncome, "First the excess: taxable income..."),
                Press(Level3CalcKey.Subtract),
                Take(TaxThreshold, "...minus the band's threshold."),
                Press(Level3CalcKey.Equals, "That's 100,000 over the threshold."),
                Press(Level3CalcKey.Multiply),
                Take(TaxRate, "The rate applies only to that excess: times 15 percent."),
                Press(Level3CalcKey.Equals),
                Press(Level3CalcKey.Add),
                Take(TaxBase, "Last, add the band's base tax. In this band it's zero."),
                Press(Level3CalcKey.Equals, "Tax due: 15,000."),
                AttachTo("I clip the tape on, so the working stays with the answer.")
            };
        if (lesson == CreditCard)
            return new List<LessonDemoStep>
            {
                Say("Bea's certificate shows tax already withheld. Watch this example first."),
                Say("The 20,000 payment is already in the studio's income. We never add it again."),
                Take(CreditTaxDue, "Start from tax due..."),
                Press(Level3CalcKey.Subtract),
                Take(CreditWithheld, "...minus the tax withheld. It's a credit: it comes off the tax, not off income."),
                Press(Level3CalcKey.Equals, "13,000 is still to pay."),
                AttachTo("I clip the tape on.")
            };
        return new List<LessonDemoStep>
        {
            Say("Taxable income is income minus supported costs. Watch me do this example once."),
            Take(LessonFund, "I take Income from the card..."),
            Press(Level3CalcKey.Subtract),
            Take(LessonSpent, "...minus the supported costs."),
            Press(Level3CalcKey.Equals, "Equals prints the tape: 8,000 taxable income."),
            AttachTo("Then I clip the tape on, so the working stays with the answer.")
        };
    }

    /// <summary>Arthur hands the same example to the player.</summary>
    public static string YourTurnLine(Level3CardSpec lesson) =>
        lesson == TaxCard ? "Your turn. Same studio, same three steps: minus threshold, times rate, plus base tax. Then attach."
        : lesson == CreditCard ? "Your turn. Tax due minus tax withheld, then attach. Leave the payment out."
        : "Your turn. Income, minus, costs, equals. Then drag the tape onto the card.";

    /// <summary>Arthur, once the player's own tape of the example is saved (then Bea's step).</summary>
    public static string DoneLine(Level3CardSpec lesson) =>
        lesson == TaxCard ? "That's it: the rate taxed only the excess. Now find Bea's own band."
        : lesson == CreditCard ? "Right. A credit comes off the tax, not off income. Now Bea's credit."
        : "Well done. Now Bea's own figures.";

    /// <summary>One plain hint for a worked tape that does not show the demonstrated step (null when it does).</summary>
    private static string Diagnose(Level3CardSpec lesson, HashSet<string> ops, HashSet<string> used, decimal value, decimal expected)
    {
        if (lesson == LessonCard)
        {
            if (!ops.Contains("-")) return RefuseNoWorking;
            return value == expected ? null : LessonTarget.Hint;
        }
        if (lesson == TaxCard)
        {
            if (!used.Contains(TaxRate) || !ops.Contains("*")) return TaxNeedsRate;
            if (!used.Contains(TaxThreshold) || !ops.Contains("-")) return TaxWholeIncome;
            if (!used.Contains(TaxBase) || !ops.Contains("+")) return TaxNeedsBase;
            return value == expected ? null : TaxOrder;
        }
        if (used.Contains(CreditPayment)) return CreditUsedPayment;
        if (!used.Contains(CreditWithheld) || !used.Contains(CreditTaxDue)) return CreditOrder;
        if (!ops.Contains("-")) return CreditAdded;
        return value == expected ? null : CreditOrder;
    }
}
