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
