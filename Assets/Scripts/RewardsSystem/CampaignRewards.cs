using System;

/// <summary>
/// PURPOSE:
/// The campaign's once-only story rewards (Campaign Foundation T08 Public
/// Trust, T09 coins), pure C# so the regression suite checks them directly.
///
/// - Fixed catalogue, never values supplied by UI: Level 1's corrected field
///   assignment gives Public Trust +2 and 60 coins; Marie's approved case
///   gives 100 coins; Marie's follow-through gives Public Trust +4. A new
///   campaign starts at trust 40/100 and 0 coins, so the first story ends at
///   46/100 and 160 coins. The community support target (65) is a future goal
///   only and unlocks nothing.
/// - T18, Level 2 (Level2CampaignRules decides when each is due): Rosa's
///   corrected income review gives Public Trust +2 and 60 coins; her approved
///   corrected case gives 100 coins; her follow-through gives Public Trust +4
///   (T19 adds its authored ethics on top, never these again): 6 trust and
///   160 coins for the chapter.
/// - Each grant is claimed once by the stable tuple
///   campaignId/levelId/activityId/kind (never a run, scene or animation id).
///   A claimed grant returns AlreadyClaimed and changes nothing, so a replay,
///   repeated result, relaunch or restarted unfinished story cannot farm it.
/// - Trust is clamped to 0..100; a coin grant that would overflow is
///   rejected. Unknown ids and non-positive amounts are rejected. Each
///   applied grant adds a ledger entry with its reason (shown to the player).
/// - Callers apply grants to a CANDIDATE copy, write it, and only then make
///   it live (PlayerProgressManager); a failed write shows nothing.
/// - Ordinary mistakes (wrong cards, field errors, lateness, audits) never
///   reach this class: there is no trust loss in Level 1.
/// </summary>
public static class CampaignRewards
{
    public const string Level1 = "Level_01";
    public const string FieldAssignment = "field-assignment";
    public const string CaseApproved = "case-approved";
    public const string FollowThrough = "marie-follow-through";
    public const string Level2 = "Level_02";
    /// <summary>Level 2's required field work: Rosa's income review, corrected (Mark the Money! mastered).</summary>
    public const string IncomeReview = "income-review";
    public const string RosaFollowThrough = "rosa-follow-through";
    public const string TrustKind = "trust";
    public const string CoinsKind = "coins";
    public const string RepairKind = "repair";

    /// <summary>Future community support target (presentation only; it unlocks nothing).</summary>
    public const int SupportTarget = 65;

    public enum Outcome { Applied, AlreadyClaimed, Rejected }

    /// <summary>One applied (or refused) grant, for presentation after the commit.</summary>
    public struct Change
    {
        public Outcome Outcome;
        public string Kind;
        public int Amount;
        public int Before;
        public int After;
        public string Reason;
        public bool Applied => Outcome == Outcome.Applied;
    }

    private struct Reward
    {
        public string LevelId, ActivityId, Kind, Reason;
        public int Amount;
        public Reward(string levelId, string activityId, string kind, int amount, string reason)
        {
            LevelId = levelId; ActivityId = activityId; Kind = kind; Amount = amount; Reason = reason;
        }
    }

    private static readonly Reward[] Catalog =
    {
        new Reward(Level1, FieldAssignment, TrustKind, 2, "Town field assignment completed"),
        new Reward(Level1, FieldAssignment, CoinsKind, 60, "Corrected field assignment"),
        new Reward(Level1, CaseApproved, CoinsKind, 100, "Marie's corrected case approved"),
        new Reward(Level1, FollowThrough, TrustKind, 4, "Marie Bautista: follow-through"),
        new Reward(Level2, IncomeReview, TrustKind, 2, "Rosa's income review completed"),
        new Reward(Level2, IncomeReview, CoinsKind, 60, "Corrected income review"),
        new Reward(Level2, CaseApproved, CoinsKind, 100, "Rosa's corrected case approved"),
        new Reward(Level2, RosaFollowThrough, TrustKind, 4, "Aling Rosa Santos: follow-through")
    };

    /// <summary>The catalogued amount (0 when there is no such reward).</summary>
    public static int Amount(string levelId, string activityId, string kind) =>
        TryFind(levelId, activityId, kind, out Reward reward) ? reward.Amount : 0;

    /// <summary>The player-facing reason of a catalogued reward ("" when unknown).</summary>
    public static string Reason(string levelId, string activityId, string kind) =>
        TryFind(levelId, activityId, kind, out Reward reward) ? reward.Reason : "";

    /// <summary>Everything one level's first story can grant of a kind (Level 1: trust 6, coins 160).</summary>
    public static int Total(string levelId, string kind)
    {
        int total = 0;
        foreach (Reward reward in Catalog)
            if (reward.LevelId == levelId && reward.Kind == kind) total += reward.Amount;
        return total;
    }

    /// <summary>The stable claim id: campaign + level + activity + reward kind.</summary>
    public static string ClaimId(string campaignId, string levelId, string activityId, string kind) =>
        $"{(campaignId ?? "").Trim()}/{(levelId ?? "").Trim()}/{(activityId ?? "").Trim()}/{(kind ?? "").Trim()}";

    public static string ActivityKey(string levelId, string activityId) => $"{(levelId ?? "").Trim()}/{(activityId ?? "").Trim()}";

    public static bool IsActivityCompleted(PlayerProgressData data, string levelId, string activityId) =>
        data != null && data.HasCampaign && data.campaign.completedActivityIds != null &&
        data.campaign.completedActivityIds.Contains(ActivityKey(levelId, activityId));

    /// <summary>Records a completed story activity once. True when it is new.</summary>
    public static bool CompleteActivity(PlayerProgressData candidate, string levelId, string activityId)
    {
        if (candidate == null || !candidate.HasCampaign || string.IsNullOrWhiteSpace(levelId) || string.IsNullOrWhiteSpace(activityId)) return false;
        if (IsActivityCompleted(candidate, levelId, activityId)) return false;
        candidate.campaign.completedActivityIds.Add(ActivityKey(levelId, activityId));
        return true;
    }

    public static bool IsClaimed(PlayerProgressData data, string levelId, string activityId, string kind) =>
        data != null && data.HasCampaign &&
        data.campaign.rewardClaims.Contains(ClaimId(data.campaign.campaignId, levelId, activityId, kind));

    /// <summary>
    /// Applies one catalogued grant to a candidate save: the balance, the
    /// claim and a ledger line together. Claimed: no change. Unknown id,
    /// non-positive amount, no campaign or a coin overflow: Rejected, no change.
    /// </summary>
    public static Change Grant(PlayerProgressData candidate, string levelId, string activityId, string kind)
    {
        var change = new Change { Outcome = Outcome.Rejected, Kind = kind };
        if (candidate == null || !candidate.HasCampaign || !TryFind(levelId, activityId, kind, out Reward reward) || reward.Amount <= 0)
            return change;
        CampaignRecord campaign = candidate.campaign;
        change.Reason = reward.Reason;
        change.Before = kind == TrustKind ? campaign.publicTrust : campaign.coins;
        change.After = change.Before;
        string claim = ClaimId(campaign.campaignId, levelId, activityId, kind);
        if (campaign.rewardClaims.Contains(claim))
        {
            change.Outcome = Outcome.AlreadyClaimed;
            return change;
        }

        if (kind == TrustKind)
        {
            change.After = Math.Max(0, Math.Min(CampaignRecord.MaxTrust, campaign.publicTrust + reward.Amount));
            campaign.publicTrust = change.After;
        }
        else
        {
            long sum = (long)Math.Max(0, campaign.coins) + reward.Amount;
            if (sum > int.MaxValue) return change;
            change.After = (int)sum;
            campaign.coins = change.After;
        }
        change.Amount = change.After - change.Before;
        campaign.rewardClaims.Add(claim);
        campaign.ledger.Add(new CampaignLedgerEntry
        {
            claimId = claim, kind = kind, amount = change.Amount, balanceAfter = change.After, reason = reward.Reason
        });
        change.Outcome = Outcome.Applied;
        return change;
    }

    /// <summary>
    /// T19's authored Rosa ethics consequence. It shares the campaign's
    /// claim/ledger and clamp rules, but permits signed trust deltas; the
    /// caller owns the fixed consequence IDs and applies them to a candidate.
    /// </summary>
    public static Change ApplyEthicsDelta(PlayerProgressData candidate, string activityId, int delta, string reason)
    {
        var change = new Change { Outcome = Outcome.Rejected, Kind = TrustKind };
        if (candidate == null || !candidate.HasCampaign || string.IsNullOrWhiteSpace(activityId) ||
            !activityId.StartsWith("rosa-ethics/", StringComparison.Ordinal) || delta < -8 || delta > 3)
            return change;

        CampaignRecord campaign = candidate.campaign;
        change.Reason = reason ?? "Rosa's records follow-through";
        change.Before = campaign.publicTrust;
        change.After = change.Before;
        string claim = ClaimId(campaign.campaignId, Level2, activityId.Trim(), TrustKind);
        if (campaign.rewardClaims.Contains(claim))
        {
            change.Outcome = Outcome.AlreadyClaimed;
            return change;
        }

        change.After = Math.Max(0, Math.Min(CampaignRecord.MaxTrust, campaign.publicTrust + delta));
        campaign.publicTrust = change.After;
        change.Amount = change.After - change.Before;
        campaign.rewardClaims.Add(claim);
        campaign.ledger.Add(new CampaignLedgerEntry
        {
            claimId = claim, kind = TrustKind, amount = change.Amount, balanceAfter = change.After, reason = change.Reason
        });
        change.Outcome = Outcome.Applied;
        return change;
    }

    private static bool TryFind(string levelId, string activityId, string kind, out Reward found)
    {
        foreach (Reward reward in Catalog)
            if (reward.LevelId == levelId && reward.ActivityId == activityId && reward.Kind == kind)
            {
                found = reward;
                return true;
            }
        found = default;
        return false;
    }
}

