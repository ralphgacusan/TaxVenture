using System.Collections.Generic;

/// <summary>
/// Aggregate totals for an entire level, built from all per-case
/// LevelResultData already stored in a LevelRewardAccumulator.
/// This is a pure data carrier — no calculation logic lives here.
/// </summary>
public class LevelTotalResult
{
    public int TotalExp;
    public int TotalReputation;
    public int CasesCompleted;
    public string FormattedCompletionTime;
    public List<LevelResultData> CaseResults;

    public static LevelTotalResult FromAccumulator(LevelRewardAccumulator accumulator)
    {
        return new LevelTotalResult
        {
            TotalExp = accumulator.GetTotalExp(),
            TotalReputation = accumulator.GetTotalReputation(),
            CasesCompleted = accumulator.CaseCount,
            FormattedCompletionTime = LevelTimer.Instance != null
                ? LevelTimer.Instance.GetFormattedTime()
                : "00:00:00",
            CaseResults = new List<LevelResultData>(accumulator.CaseResults)
        };
    }
}