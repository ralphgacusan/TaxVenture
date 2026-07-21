using UnityEngine;

/// <summary>
/// PURPOSE:
/// Pure logic determining final EXP and Reputation Hearts earned for
/// completing a case, based on CaseData.auditMistakeCount. Zero Unity/UI
/// dependency beyond Mathf, same pattern as TaxComputationCalculator and
/// ComplianceChecker.
///
/// FORMULA (simplified for Phase 1 greybox):
/// EXP = baseExp - (mistakeCount * expPenaltyPerMistake), floored at minExp
/// Reputation = baseReputationHearts - mistakeCount, floored at 0
///
/// CONNECTS WITH:
/// - RewardsUI: calls Calculate() to populate the reward screen
/// </summary>
public static class RewardsCalculator
{
    private const int BaseExp = 100;
    private const int ExpPenaltyPerMistake = 10;
    private const int MinExp = 10;

    private const int BaseReputationHearts = 5;

    public static RewardResult Calculate(int mistakeCount)
    {
        int exp = Mathf.Max(MinExp, BaseExp - (mistakeCount * ExpPenaltyPerMistake));
        int reputation = Mathf.Max(0, BaseReputationHearts - mistakeCount);

        return new RewardResult
        {
            ExpEarned = exp,
            ReputationHeartsEarned = reputation,
            MistakeCount = mistakeCount
        };
    }
}

public class RewardResult
{
    public int ExpEarned;
    public int ReputationHeartsEarned;
    public int MistakeCount;
}