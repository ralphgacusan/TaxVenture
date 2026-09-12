using System.Collections.Generic;

/// <summary>
/// Accumulates per-case LevelResultData for the current level session.
/// Reward calculation still happens once per case (in RewardsState via
/// RewardsCalculator). This class only stores and sums those results —
/// it never recalculates rewards itself.
/// </summary>
public class LevelRewardAccumulator
{
    private readonly List<LevelResultData> caseResults = new List<LevelResultData>();

    public IReadOnlyList<LevelResultData> CaseResults => caseResults;

    public void AddCaseResult(LevelResultData result)
    {
        caseResults.Add(result);
    }

    public void Reset()
    {
        caseResults.Clear();
    }

    public int GetTotalExp()
    {
        int total = 0;
        foreach (var r in caseResults)
        {
            total += r.ExpEarned;
        }
        return total;
    }

    public int GetTotalReputation()
    {
        int total = 0;
        foreach (var r in caseResults)
        {
            total += r.ReputationEarned;
        }
        return total;
    }

    public int CaseCount => caseResults.Count;
}