/// <summary>
/// PURPOSE:
/// Bea's withholding certificate at the office credit step (Level 3 T08),
/// pure C# so the regression suite drives the same commands the conference
/// room calls. Every command runs on a COPY of the chapter snapshot; the
/// caller (Level3Checkpointer) writes it, then publishes it.
///
/// - CreditStepOpen: Bea's computation is open and her tax due has been on
///   the sheet (taxDueNoted). Only then does she hand over B-WHT.
/// - HandOver: B-WHT becomes an acquired source of her case under one stable
///   action id, in the candidate, before the paper can be seen or used.
///   A repeat (or a resume) is a Duplicate: nothing changes, nothing shows.
/// - The credit-versus-expense question (CONTENT "Withholding handover"):
///   Answer says what each option does. Only the right explanation is saved
///   (Explain); a wrong one opens the book's help and changes nothing (no
///   trust, ethics or grade), and "inspect" shows the certificate and comes
///   back to the same question. Explaining never fills in the credit: the
///   player links it on the sheet (Level3ComputationRules.SelectCredit).
/// No approval or reward here (T09).
/// </summary>
public static class Level3CertificateRules
{
    public const string HandOverAction = "Level_03/bea-certificate";
    public const string ExplainAction = "Level_03/bea-credit-explained";
    public const string ReceivedMilestone = "bea-certificate-received";

    /// <summary>The three answers to Bea's question, in CONTENT order.</summary>
    public const string AnswerCredit = "credit";
    public const string AnswerIncome = "income";
    public const string AnswerInspect = "inspect";
    public static readonly string[] Answers = { AnswerCredit, AnswerIncome, AnswerInspect };

    public enum AnswerKind { Unknown, Explains, NeedsHelp, Inspect }

    public const string RefuseNotYet = "Work out Bea's tax due on her sheet first. Then she'll show you one more paper.";
    public const string RefuseNotHere = "Bea's certificate cannot be handed over right now.";

    private static string Bea => Level3ChapterRules.BeaCaseId;

    /// <summary>Bea's case holds her certificate (acquired at the credit step).</summary>
    public static bool Acquired(Level3ChapterCheckpoint state) =>
        state?.Case(Bea)?.acquiredSourceIds?.Contains(Level3ChapterRules.BeaCreditSource) == true;

    /// <summary>The player explained the certificate as a credit (saved).</summary>
    public static bool Explained(Level3ChapterCheckpoint state) => state?.Case(Bea)?.computation?.creditExplained == true;

    /// <summary>The office credit step: her computation is open and her tax due has been on the sheet.</summary>
    public static bool CreditStepOpen(Level3ChapterCheckpoint state) =>
        Level3ComputationRules.Editable(state, Bea) && state.Case(Bea)?.computation?.taxDueNoted == true;

    /// <summary>The certificate handover is due now (the credit step is open and she still has it).</summary>
    public static bool HandOverDue(Level3ChapterCheckpoint state) => CreditStepOpen(state) && !Acquired(state);

    /// <summary>Her question is waiting for the right explanation (she handed it over; it was not explained yet).</summary>
    public static bool QuestionDue(Level3ChapterCheckpoint state) => Acquired(state) && !Explained(state) && Level3ComputationRules.Editable(state, Bea);

    public static AnswerKind Answer(string answerId) =>
        answerId == AnswerCredit ? AnswerKind.Explains : answerId == AnswerIncome ? AnswerKind.NeedsHelp :
        answerId == AnswerInspect ? AnswerKind.Inspect : AnswerKind.Unknown;

    /// <summary>B-WHT is handed over (acquired in the candidate). Refused outside the credit step; a repeat is a Duplicate.</summary>
    public static Level3CommandOutcome HandOver(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (candidate?.actionIds != null && candidate.actionIds.Contains(HandOverAction) || Acquired(candidate))
        {
            reason = "already handed over";
            return Level3CommandOutcome.Duplicate;
        }
        if (candidate == null || Level3ChapterRules.Problem(candidate) != null || !Level3ComputationRules.Editable(candidate, Bea))
        {
            reason = RefuseNotHere;
            return Level3CommandOutcome.Refused;
        }
        if (!CreditStepOpen(candidate)) { reason = RefuseNotYet; return Level3CommandOutcome.Refused; }
        Level3CaseProgress bea = candidate.Case(Bea);
        bea.acquiredSourceIds.Add(Level3ChapterRules.BeaCreditSource);
        candidate.actionIds.Add(HandOverAction);
        if (!candidate.milestones.Contains(ReceivedMilestone)) candidate.milestones.Add(ReceivedMilestone);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>The right explanation is saved (it fills in nothing). Refused without the certificate; a repeat is a Duplicate.</summary>
    public static Level3CommandOutcome Explain(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (Explained(candidate)) { reason = "already explained"; return Level3CommandOutcome.Duplicate; }
        if (!Acquired(candidate) || !Level3ComputationRules.Editable(candidate, Bea)) { reason = RefuseNotHere; return Level3CommandOutcome.Refused; }
        Level3CaseProgress bea = candidate.Case(Bea);
        if (bea.computation == null) bea.computation = new Level3ComputationRecord();
        bea.computation.creditExplained = true;
        if (!candidate.actionIds.Contains(ExplainAction)) candidate.actionIds.Add(ExplainAction);
        return Level3CommandOutcome.Applied;
    }
}
