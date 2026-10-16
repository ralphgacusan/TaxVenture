using System;

/// <summary>
/// PURPOSE:
/// How the client feels about the finished case (pure, no Unity; checked by
/// Tests/GuideRegression). The case finale picks her lines by this tier.
///
///   Great : approved on the first audit, on time.
///   Good  : one slip (one failed audit, or a day late).
///   Okay  : more than that (several failed audits, late with mistakes,
///           or more than a day late). Still done, still thankful.
/// </summary>
public static class CaseFinaleRules
{
    public enum Tier { Great, Good, Okay }

    /// <summary>Slips: each failed audit, being late, and every extra day beyond the first late one.</summary>
    public static int Slips(int failedAudits, bool onTime, int day)
    {
        int slips = Math.Max(0, failedAudits);
        if (!onTime) slips += 1;
        if (day > 2) slips += day - 2;
        return slips;
    }

    public static Tier TierFor(int failedAudits, bool onTime, int day)
    {
        int slips = Slips(failedAudits, onTime, day);
        if (slips == 0) return Tier.Great;
        if (slips == 1) return Tier.Good;
        return Tier.Okay;
    }
}
