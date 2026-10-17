/// <summary>
/// PURPOSE:
/// The guided initial review (Level 2, ticket T03), as pure rules: given the
/// player's finding and the stamp they pressed, is this a supported initial
/// Not Ready hold? If not, which one thing should they fix? Pure C# so the
/// regression suite can test it. A wrong try costs nothing: it is not
/// recorded, scored or rewarded.
///
/// The preliminary hold is its own record (CaseReviewState.preliminary). It
/// never sets the case assessment, filing status, game state, audit or
/// completion; the final assessment is a later, separate step.
/// </summary>
public static class PreliminaryReview
{
    public enum Outcome
    {
        Recorded,
        NotRetrieved,
        AlreadyStamped,
        FindingIncomplete,
        WrongConcern,
        WrongEvidence,
        WrongReason,
        ReadyWhileOpen,
        NoRule
    }

    /// <summary>Checks a stamp against the rule without changing anything.</summary>
    public static Outcome Evaluate(CaseReviewState state, PreliminaryRuleDefinition rule, bool notReadyStamp)
    {
        if (rule == null || !rule.IsSet) return Outcome.NoRule;
        if (state == null || !state.FolderRetrieved) return Outcome.NotRetrieved;
        if (state.preliminary != null) return Outcome.AlreadyStamped;

        ReviewFindingState finding = state.FindFinding(rule.findingId);
        if (finding == null || !finding.IsComplete) return Outcome.FindingIncomplete;
        if (!notReadyStamp) return Outcome.ReadyWhileOpen;
        if (finding.concernDocument != rule.concernDocument) return Outcome.WrongConcern;
        if (finding.evidenceDocument != rule.evidenceDocument) return Outcome.WrongEvidence;
        if (finding.reasonId != rule.reasonId) return Outcome.WrongReason;
        return Outcome.Recorded;
    }

    /// <summary>
    /// Records the initial Not Ready hold when the stamp is supported, once.
    /// Moves the review to MarketVisit. Returns the outcome either way.
    /// </summary>
    public static Outcome TryStamp(CaseReviewState state, PreliminaryRuleDefinition rule, bool notReadyStamp)
    {
        Outcome outcome = Evaluate(state, rule, notReadyStamp);
        if (outcome != Outcome.Recorded) return outcome;

        state.preliminary = new PreliminaryAssessmentState
        {
            verdict = PreliminaryAssessmentState.NotReady,
            findingId = rule.findingId,
            reasonId = rule.reasonId,
            revision = state.packetRevision
        };
        state.TryAdvance(ReviewPhase.MarketVisit);
        return outcome;
    }

    /// <summary>Arthur's guided line for each outcome (touch wording; no amounts or totals).</summary>
    public static string Message(Outcome outcome)
    {
        switch (outcome)
        {
            case Outcome.Recorded: return "Initial review stamped Not Ready. Now let's visit Rosa at Stall 04 and see her records.";
            case Outcome.NotRetrieved: return "Get Rosa's folder from the market-client drawer first.";
            case Outcome.AlreadyStamped: return "The initial review is already stamped. Visit Rosa at Stall 04 next.";
            case Outcome.FindingIncomplete: return "Fill in your finding first: a concern, a related record and a reason.";
            case Outcome.WrongConcern: return "For the concern, pick a line from Rosa's May summary.";
            case Outcome.WrongEvidence: return "For the related record, pick a line from the referral note.";
            case Outcome.WrongReason: return "Pick the reason that explains why you can't approve this yet.";
            case Outcome.ReadyWhileOpen: return "Not yet! With an open concern, the initial review is Not Ready.";
            default: return null;
        }
    }
}
