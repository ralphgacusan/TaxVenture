using UnityEngine;

/// <summary>
/// PURPOSE:
/// Pure logic computing final EXP/Reputation per R13's four-tier rules,
/// and assembling the complete LevelResultData record. Zero Unity/UI
/// dependency beyond Mathf, same pattern as every other Calculator/
/// Checker/Evaluator in this project.
///
/// SCORING RULES (per R13 spec):
/// Correct + No Issues   -> Max EXP, Max Reputation
/// Correct + Issues      -> EXP/Reputation reduced per issue
/// Wrong + No Issues     -> Small EXP, No Reputation
/// Wrong + Issues        -> No Rewards
/// </summary>
public static class RewardsCalculator
{
    private const int MaxExp = 100;
    private const int MaxReputation = 5;
    private const int ExpPenaltyPerIssue = 15;
    private const int ReputationPenaltyPerIssue = 1;
    private const int WrongVerdictSmallExp = 20;

    public static LevelResultData BuildResult(CaseData data)
    {
        ClientOutcomeBranch branch = ClientOutcomeEvaluator.Determine(data);

        int exp;
        int reputation;
        string summary;

        switch (branch)
        {
            case ClientOutcomeBranch.CorrectNoIssues:
                exp = MaxExp;
                reputation = MaxReputation;
                summary = "Flawless work. The case was correctly assessed with no errors found.";
                break;

            case ClientOutcomeBranch.CorrectWithIssues:
                exp = Mathf.Max(0, MaxExp - (data.finalMissedIssueCount * ExpPenaltyPerIssue));
                reputation = Mathf.Max(0, MaxReputation - (data.finalMissedIssueCount * ReputationPenaltyPerIssue));
                summary = $"The case assessment was correct, but {data.finalMissedIssueCount} issue(s) reduced your final score.";
                break;

            case ClientOutcomeBranch.WrongVerdict:
                exp = WrongVerdictSmallExp;
                reputation = 0;
                summary = "The case was completed, but the final assessment was incorrect.";
                break;

            case ClientOutcomeBranch.WrongVerdictWithIssues:
            default:
                exp = 0;
                reputation = 0;
                summary = "The case assessment was incorrect and multiple issues were found. No rewards earned.";
                break;
        }

        return new LevelResultData
        {
            Branch = branch,
            FormattedCompletionTime = LevelTimer.Instance != null ? LevelTimer.Instance.GetFormattedTime() : "00:00:00",
            PlayerVerdict = data.caseAssessment,
            CorrectVerdict = CaseVerdictEvaluator.DetermineCorrectVerdict(data),
            VerdictWasCorrect = data.finalVerdictWasCorrect,
            IssuesFound = data.finalMissedIssueCount,
            ExpEarned = exp,
            ReputationEarned = reputation,
            SummaryText = summary
        };
    }
}