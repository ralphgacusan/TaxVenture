using System.Collections.Generic;

/// <summary>
/// Aggregate totals for an entire level, built from all per-case
/// LevelResultData already stored in a LevelRewardAccumulator.
/// This is a pure data carrier — no calculation logic lives here.
/// </summary>
public class LevelTotalResult
{
    /// <summary>Unique per finished run, so saving it twice applies it once.</summary>
    public readonly string RunId = System.Guid.NewGuid().ToString("N");

    public string LevelId;
    public int TotalExp;
    public int TotalReputation;
    public int CasesCompleted;
    public string FormattedCompletionTime;
    public List<LevelResultData> CaseResults;

    // Level score (whole level: client talks, side quest, cases).
    public int MaxExp;
    public int MaxReputation;
    public int ExpPercent;
    public int ReputationPercent;
    public int PassingPercent;
    public bool Passed;
    public bool HasScore;
    public List<string> Badges = new List<string>();
    public List<string> BadgeNames = new List<string>();
    public List<BadgeTier> BadgeTiers = new List<BadgeTier>();

    // Seconds spent on the whole level and on its side quest (0 = not measured).
    public float RunSeconds;
    public float SideQuestSeconds;

    /// <summary>
    /// Whole-level totals from the level score: every reward source against the
    /// level maximum, the passing line and the badges.
    /// </summary>
    public static LevelTotalResult FromLedger(LevelScoreLedger ledger, LevelRewardAccumulator accumulator)
    {
        LevelTotalResult result = FromAccumulator(accumulator);
        if (ledger == null) return result;

        result.TotalExp = ledger.TotalExp;
        result.TotalReputation = ledger.TotalHearts;
        result.MaxExp = ledger.MaxExp;
        result.MaxReputation = ledger.MaxHearts;
        result.ExpPercent = ledger.ExpPercent;
        result.ReputationPercent = ledger.HeartsPercent;
        result.PassingPercent = ledger.Rules.passingPercent;
        result.Passed = ledger.Passed;
        result.HasScore = true;
        result.Badges = new List<string>(ledger.Badges);
        result.BadgeNames = new List<string>(ledger.BadgeNames);
        result.BadgeTiers = new List<BadgeTier>(ledger.BadgeTiers);
        result.RunSeconds = ledger.ElapsedSeconds;
        result.SideQuestSeconds = ledger.SideQuestScored ? ledger.SideQuestSeconds : 0f;
        return result;
    }

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
