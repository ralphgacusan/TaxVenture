/// <summary>Checks the player's proposed link without awarding points or changing the
/// case assessment. A failed comparison describes these selections, not the whole packet.</summary>
public static class FindingComparison
{
    public enum Result { Incomplete, NoDiscrepancy, Supported }

    public static Result Evaluate(CaseReviewState state, PreliminaryRuleDefinition rule)
    {
        if (state == null || rule == null || !rule.IsSet || !state.FolderRetrieved)
            return Result.Incomplete;
        ReviewFindingState finding = state.FindFinding(rule.findingId);
        if (finding == null || !finding.IsComplete) return Result.Incomplete;
        if (!state.HasDocument(finding.concernDocument) || !state.HasDocument(finding.evidenceDocument) ||
            finding.concernDocument != rule.concernDocument || finding.evidenceDocument != rule.evidenceDocument ||
            finding.reasonId != rule.reasonId || !HasMeaningfulConcern(finding, rule) || !HasMeaningfulEvidence(finding, rule))
            return Result.NoDiscrepancy;
        return Result.Supported;
    }

    public static bool HasMeaningfulEvidence(ReviewFindingState finding, PreliminaryRuleDefinition rule)
        => finding != null && rule != null && (rule.evidenceKeys == null || rule.evidenceKeys.Count == 0 ||
            rule.evidenceKeys.Contains(finding.evidenceKey));

    public static bool HasMeaningfulConcern(ReviewFindingState finding, PreliminaryRuleDefinition rule)
        => finding != null && rule != null && (rule.concernKeys == null || rule.concernKeys.Count == 0 ||
            rule.concernKeys.Contains(finding.concernKey));

    /// <summary>
    /// The first box that keeps a complete draft from linking (Concern, then
    /// Evidence, then Reason), or null when it links or is not complete.
    /// </summary>
    public static string FirstMismatch(CaseReviewState state, PreliminaryRuleDefinition rule)
    {
        if (state == null || rule == null || !rule.IsSet) return null;
        ReviewFindingState finding = state.FindFinding(rule.findingId);
        if (finding == null || !finding.IsComplete) return null;
        if (finding.concernDocument != rule.concernDocument || !HasMeaningfulConcern(finding, rule)) return ReviewFindingState.Concern;
        if (finding.evidenceDocument != rule.evidenceDocument || !HasMeaningfulEvidence(finding, rule)) return ReviewFindingState.Evidence;
        if (finding.reasonId != rule.reasonId) return ReviewFindingState.Reason;
        return null;
    }

    /// <summary>A nudge for the box to look at again (never the answer itself).</summary>
    public static string Clue(string part)
    {
        switch (part)
        {
            case ReviewFindingState.Concern:
                return "Clue: look again at Entry to check. Which Summary line should hold every payment Rosa earned in May?";
            case ReviewFindingState.Evidence:
                return "Clue: look again at your Clue. Which Payment Note line says what Rosa was paid for?";
            case ReviewFindingState.Reason:
                return "Clue: look again at the Reason. Book Section 7: which one fits money earned but left out of the total?";
            default:
                return "Clue: swap one box and compare again.";
        }
    }

    public static Result Check(CaseReviewState state, PreliminaryRuleDefinition rule)
    {
        Result result = Evaluate(state, rule);
        if (state != null && rule != null && state.CanEditPreliminaryFinding)
        {
            ReviewFindingState finding = state.FindFinding(rule.findingId);
            if (finding != null) finding.comparisonConfirmed = result == Result.Supported;
        }
        return result;
    }
}
