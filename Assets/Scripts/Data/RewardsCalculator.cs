using UnityEngine;

/// <summary>
/// PURPOSE:
/// Pure logic computing final EXP/Reputation per R13's four-tier rules,
/// and assembling the complete LevelResultData record. Zero Unity/UI
/// dependency beyond Mathf, same pattern as every other Calculator/
/// Checker/Evaluator in this project.
///
/// Every number comes from the level's rewards.json (caseFile section), so
/// a level tunes its case rewards without touching code:
/// Correct + No Issues   -> exp, hearts
/// Correct + Issues      -> minus expPerMistake / heartsPerMistake per issue
/// Wrong + No Issues     -> wrongVerdictExp, no hearts
/// Wrong + Issues        -> nothing
///
/// A case that went through the Auditor is re-scored from its audits by
/// LevelScoreLedger.AddCase (CaseProgressionManager); this result supplies
/// the verdict, issues and summary for it.
/// </summary>
public static class RewardsCalculator
{
    /// <summary>Default case maximum (rewards.json caseFile.exp) when no rules are loaded.</summary>
    public static int MaxExp => Defaults.exp;

    /// <summary>Default case maximum (rewards.json caseFile.hearts) when no rules are loaded.</summary>
    public static int MaxReputation => Defaults.hearts;

    private static readonly LevelRewardRules.CaseRules Defaults = new LevelRewardRules.CaseRules();

    /// <summary>The case rules of the running level, or the built-in defaults.</summary>
    public static LevelRewardRules.CaseRules CurrentRules
    {
        get
        {
            CaseProgressionManager progression = CaseProgressionManager.Instance;
            LevelRewardRules.CaseRules rules = progression != null ? progression.Score.Rules.caseFile : null;
            return rules ?? Defaults;
        }
    }

    public static LevelResultData BuildResult(CaseData data) => BuildResult(data, CurrentRules);

    public static LevelResultData BuildResult(CaseData data, LevelRewardRules.CaseRules rules)
    {
        if (rules == null) rules = Defaults;
        ClientOutcomeBranch branch = data != null ? ClientOutcomeEvaluator.Determine(data) : ClientOutcomeBranch.WrongVerdictWithIssues;
        int issues = data != null ? Mathf.Max(0, data.finalMissedIssueCount) : 0;

        int exp;
        int reputation;
        string summary;

        switch (branch)
        {
            case ClientOutcomeBranch.CorrectNoIssues:
                exp = rules.exp;
                reputation = rules.hearts;
                summary = "Flawless work. The case was correctly assessed with no errors found.";
                break;

            case ClientOutcomeBranch.CorrectWithIssues:
                exp = Mathf.Max(0, rules.exp - issues * rules.expPerMistake);
                reputation = Mathf.Max(0, rules.hearts - issues * rules.heartsPerMistake);
                summary = $"The case assessment was correct, but {issues} issue(s) reduced your final score.";
                break;

            case ClientOutcomeBranch.WrongVerdict:
                exp = Mathf.Clamp(rules.wrongVerdictExp, 0, rules.exp);
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
            PlayerVerdict = data != null ? data.caseAssessment : CaseAssessment.NotReadyForFiling,
            CorrectVerdict = CaseVerdictEvaluator.DetermineCorrectVerdict(data),
            VerdictWasCorrect = data != null && data.finalVerdictWasCorrect,
            IssuesFound = issues,
            ExpEarned = exp,
            ReputationEarned = reputation,
            SummaryText = summary
        };
    }
}
