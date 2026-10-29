using System;
using System.Collections.Generic;

/// <summary>How a carried item behaves when the story enters Level 2 (T18 eligibility table).</summary>
public enum CarryEligibility
{
    /// <summary>Not in the catalogue: kept as it is (normalization reports it), never used.</summary>
    Unknown = 0,
    /// <summary>A read-only reference (Arthur's Reviewed File): readable anywhere; never Rosa's case, its answers or evidence.</summary>
    Reference = 1,
    /// <summary>A consumable that works on Rosa's case (review voucher: +180 active seconds, once per case, used up).</summary>
    Supply = 2,
    /// <summary>The bookmark set: still pins up to three owned records to the top of the Bag.</summary>
    Bookmark = 3,
    /// <summary>A keepsake: readable from the Bag (its display stand is Level 1's).</summary>
    Keepsake = 4,
    /// <summary>A tool this chapter's world hands out again (Notes, Case Folder, Tax Code Book; Rosa's marked records).</summary>
    ChapterTool = 5,
    /// <summary>Another chapter's quest object (Level 1's Client IDs): retired on entry, never Rosa evidence, no old task restarts.</summary>
    ChapterBound = 6
}

/// <summary>T19's single explicit choice with Rosa.</summary>
public enum Level2EthicsChoice { None, Responsible, UnsupportedPromise, DismissiveHold }

/// <summary>Rosa's review as the campaign rules see it (plain values read from CaseReviewState; never an answer).</summary>
public struct Level2CaseFacts
{
    public bool HasReview;
    public string CaseId;
    /// <summary>Mark the Money! is mastered after corrections and its award was given once.</summary>
    public bool QuestCorrected;
    /// <summary>The first complete check (frozen; corrections never change it).</summary>
    public bool FirstChecked;
    public int FirstCorrect;
    public int FirstTotal;
    /// <summary>The Auditor actually approved the corrected review.</summary>
    public bool Approved;
}

/// <summary>
/// PURPOSE:
/// Level 2 inside the campaign (Campaign Foundation T18), pure C# so the
/// regression suite checks it directly.
///
/// ELIGIBILITY: what each Level 1 item becomes in Rosa's chapter
/// (Eligibility / Describe). The campaign inventory, wallet, trust, mastery,
/// upgrades, decor and settings are the same records in both chapters;
/// nothing is cleared, reseeded or acquired again on entry. Only another
/// chapter's quest objects are retired (RetireOtherChapters).
///
/// CLAIMS (CampaignRewards catalogue, each claimed once by campaign + level +
/// activity + kind): the corrected income review (+2 trust, 60 coins) is
/// applied in the same candidate as the checkpoint that records it
/// (ApplyCheckpointClaims); the approved corrected case (100 coins) with the
/// level result (ApplyCaseResult); Rosa's follow-through (+4 trust) only after
/// that approval (ApplyFollowThrough). The chapter completes only when all
/// three are done; XP, badge tier and trust never gate it. Replays, practice
/// and Editor runs never reach these (the caller checks the run mode).
/// </summary>
public static class Level2CampaignRules
{
    public const string LevelId = CampaignRewards.Level2;
    public const string ResponsibleChoiceText = "I cannot approve it yet. Let’s check the payment together.";
    public const string UnsupportedChoiceText = "I’ll call it ready now so you can move forward.";
    public const string DismissiveChoiceText = "Not ready. Bring me complete records next time.";
    public const string ResponsibleStatus = "REVIEW OPEN — SUPPORTING RECORDS STILL REQUIRED";
    public const string UnsupportedStatus = "PROMISE REVISED — SUPPORT CHECKED";
    public const string ResponsibleFollowThroughStatus = "RECORDS CHECKED.";
    public const string DismissiveStatus = "HOLD — REVIEW REMAINS OPEN";

    public static bool EthicsChoiceCommitted(PlayerProgressData data, string caseId)
    {
        RosaEthicsRecord ethics = data?.campaign?.rosaEthics;
        return data != null && data.HasCampaign && ethics != null && ethics.committed &&
               string.Equals(ethics.caseId, caseId, StringComparison.Ordinal);
    }

    public static string EthicsStatus(PlayerProgressData data, string caseId)
    {
        if (!EthicsChoiceCommitted(data, caseId)) return "CHOICE NOT RECORDED";
        RosaEthicsRecord ethics = data.campaign.rosaEthics;
        if (ethics.choiceId.EndsWith("/responsible", StringComparison.Ordinal))
            return ethics.followThroughDone ? ResponsibleFollowThroughStatus : ResponsibleStatus;
        if (ethics.choiceId.EndsWith("/unsupported-promise", StringComparison.Ordinal))
            return ethics.followThroughDone ? UnsupportedStatus : "PROMISE RECORDED — SUPPORT CHECK PENDING";
        return DismissiveStatus;
    }

    /// <summary>Commits one T19 choice and its first trust consequence on a candidate campaign.</summary>
    public static bool CommitEthicsChoice(PlayerProgressData candidate, Level2EthicsChoice choice, string caseId)
    {
        if (candidate == null || !candidate.HasCampaign || string.IsNullOrWhiteSpace(caseId)) return false;
        if (choice != Level2EthicsChoice.Responsible && choice != Level2EthicsChoice.UnsupportedPromise &&
            choice != Level2EthicsChoice.DismissiveHold) return false;
        RosaEthicsRecord ethics = candidate.campaign.rosaEthics ?? (candidate.campaign.rosaEthics = new RosaEthicsRecord());
        if (ethics.committed) return false;
        int delta = choice == Level2EthicsChoice.Responsible ? -1 : choice == Level2EthicsChoice.UnsupportedPromise ? 3 : -3;
        string suffix = choice == Level2EthicsChoice.Responsible ? "responsible-refusal" :
            choice == Level2EthicsChoice.UnsupportedPromise ? "unsupported-promise" : "dismissive-hold";
        string choiceName = choice == Level2EthicsChoice.Responsible ? "responsible" :
            choice == Level2EthicsChoice.UnsupportedPromise ? "unsupported-promise" : "dismissive-hold";
        CampaignRewards.Change change = CampaignRewards.ApplyEthicsDelta(candidate,
            $"rosa-ethics/{caseId}/{suffix}", delta,
            choice == Level2EthicsChoice.Responsible ? "Rosa heard that the payment must be checked first" :
            choice == Level2EthicsChoice.UnsupportedPromise ? "Unsupported promise to Rosa" : "Dismissive hold on Rosa's review");
        if (!change.Applied) return false;
        ethics.caseId = caseId;
        ethics.commitmentId = CommitmentId(caseId);
        ethics.choiceId = $"rosa-ethics/{caseId}/{choiceName}";
        ethics.choiceText = choice == Level2EthicsChoice.Responsible ? ResponsibleChoiceText :
            choice == Level2EthicsChoice.UnsupportedPromise ? UnsupportedChoiceText : DismissiveChoiceText;
        ethics.committed = true;
        ethics.followThroughRequired = choice != Level2EthicsChoice.DismissiveHold;
        ethics.followThroughDone = !ethics.followThroughRequired;
        ethics.promiseTrustDelta = choice == Level2EthicsChoice.UnsupportedPromise ? change.Amount : 0;
        RecordEthicsConsequence(ethics, candidate.campaign.campaignId, $"rosa-ethics/{caseId}/{suffix}");
        return true;
    }

    public static string ChoiceText(Level2EthicsChoice choice) =>
        choice == Level2EthicsChoice.Responsible ? ResponsibleChoiceText :
        choice == Level2EthicsChoice.UnsupportedPromise ? UnsupportedChoiceText :
        choice == Level2EthicsChoice.DismissiveHold ? DismissiveChoiceText : "";

    public static string CommitmentId(string caseId) => $"rosa-ethics/{caseId}/commitment";

    private static void RecordEthicsConsequence(RosaEthicsRecord ethics, string campaignId, string activityId)
    {
        string claim = CampaignRewards.ClaimId(campaignId, LevelId, activityId, CampaignRewards.TrustKind);
        if (ethics.consequenceIds == null) ethics.consequenceIds = new List<string>();
        if (!ethics.consequenceIds.Contains(claim)) ethics.consequenceIds.Add(claim);
    }

    // =========================================================
    // ELIGIBILITY
    // =========================================================

    /// <summary>The eligibility of one catalogued item in a story level.</summary>
    public static CarryEligibility Eligibility(InventoryItemDefinition item, string levelId)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.id)) return CarryEligibility.Unknown;
        if (item.reference) return CarryEligibility.Reference;
        if (!item.CampaignWide) return item.InScope(levelId) ? CarryEligibility.ChapterTool : CarryEligibility.ChapterBound;
        if (item.id == InventoryCatalogue.ReviewVoucher) return CarryEligibility.Supply;
        if (item.id == InventoryCatalogue.BookmarkSet) return CarryEligibility.Bookmark;
        if (item.category == InventoryCatalogue.Keepsakes) return CarryEligibility.Keepsake;
        return CarryEligibility.Unknown;
    }

    public static CarryEligibility Eligibility(InventoryCatalogue catalogue, string itemId, string levelId) =>
        Eligibility(catalogue?.Find(itemId), levelId);

    /// <summary>Shared carried tools keep their saved slot on Chapter 2 entry; case evidence follows its own world.</summary>
    public static bool ResetsOnFreshAttempt(InventoryItemDefinition item, string levelId)
    {
        if (item == null) return false;
        if (levelId == LevelId && (item.id == InventoryCatalogue.Notes || item.id == InventoryCatalogue.TaxCodeBook))
            return false;
        return item.InScope(levelId) || (levelId == CampaignRewards.Level1 && item.id == InventoryCatalogue.ReviewedFile);
    }

    /// <summary>The table row in plain words (documentation and diagnostics).</summary>
    public static string Describe(CarryEligibility eligibility)
    {
        switch (eligibility)
        {
            case CarryEligibility.Reference: return "Readable reference; never the active case, its answers or evidence";
            case CarryEligibility.Supply: return "Usable on Rosa's case (+180 active review seconds, once per case, used up)";
            case CarryEligibility.Bookmark: return "Still pins up to three owned records";
            case CarryEligibility.Keepsake: return "Readable from the Bag";
            case CarryEligibility.ChapterTool: return "Handed out again by this chapter's world";
            case CarryEligibility.ChapterBound: return "Retired on entry: another chapter's quest object";
            default: return "Kept as it is; not used";
        }
    }

    /// <summary>Owned items another chapter owns (they leave the bar, Bag and drawer on entry), in catalogue order.</summary>
    public static List<string> ChapterBoundOwned(CampaignInventoryRecord r, InventoryCatalogue c, string levelId)
    {
        var ids = new List<string>();
        if (r == null || c?.items == null) return ids;
        foreach (InventoryItemDefinition item in c.items)
            if (item != null && Eligibility(item, levelId) == CarryEligibility.ChapterBound && InventoryRules.Owns(r, item.id))
                ids.Add(item.id);
        return ids;
    }

    /// <summary>
    /// Entering a story level retires another chapter's quest objects (one
    /// command, so a failed save keeps them all). Everything else stays exactly
    /// where it is; a repeat entry finds nothing to retire (NoChange).
    /// </summary>
    public static InventoryResult RetireOtherChapters(CampaignInventoryRecord r, InventoryCatalogue c, string levelId, string actionId)
    {
        List<string> retire = ChapterBoundOwned(r, c, levelId);
        if (retire.Count == 0) return new InventoryResult { Outcome = InventoryOutcome.NoChange };
        foreach (string id in retire) InventoryRules.Remove(r, c, id, (actionId ?? "retire") + "/" + id);
        return new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = retire[0] };
    }

    // =========================================================
    // STORY CLAIMS
    // =========================================================

    /// <summary>What one Level 2 story commit changed (presentation after the write).</summary>
    public struct Grants
    {
        /// <summary>The candidate changed (a write is needed).</summary>
        public bool Changed;
        public CampaignRewards.Change Trust;
        public CampaignRewards.Change Coins;
        public bool ChapterCompleted;
    }

    public static bool QuestClaimed(PlayerProgressData data) =>
        CampaignRewards.IsActivityCompleted(data, LevelId, CampaignRewards.IncomeReview);

    public static bool CaseClaimed(PlayerProgressData data) =>
        CampaignRewards.IsActivityCompleted(data, LevelId, CampaignRewards.CaseApproved);

    public static bool FollowThroughDone(PlayerProgressData data) =>
        CampaignRewards.IsActivityCompleted(data, LevelId, CampaignRewards.RosaFollowThrough);

    /// <summary>Rosa's approved case is saved and her follow-through is still to come.</summary>
    public static bool FollowThroughPending(PlayerProgressData data) => CaseClaimed(data) && !FollowThroughDone(data);

    /// <summary>Chapter 2's story is complete: corrected field review, approved corrected case and follow-through.</summary>
    public static bool ChapterComplete(PlayerProgressData data) => QuestClaimed(data) && CaseClaimed(data) && FollowThroughDone(data) &&
        (data?.campaign?.rosaEthics == null || !data.campaign.rosaEthics.followThroughRequired || data.campaign.rosaEthics.followThroughDone);

    /// <summary>
    /// The claims a saved checkpoint has earned, applied to the same candidate
    /// (so the checkpoint and its claims are one milestone): the corrected
    /// income review gives +2 trust and 60 coins once. Nothing else is earned
    /// by saving; an earlier claim changes nothing (restore and repeats are quiet).
    /// </summary>
    public static Grants ApplyCheckpointClaims(PlayerProgressData candidate, Level2CaseFacts facts)
    {
        var grants = new Grants();
        if (candidate == null || !candidate.HasCampaign || !facts.HasReview || !facts.QuestCorrected) return grants;
        if (!CampaignRewards.CompleteActivity(candidate, LevelId, CampaignRewards.IncomeReview)) return grants;
        grants.Changed = true;
        grants.Trust = CampaignRewards.Grant(candidate, LevelId, CampaignRewards.IncomeReview, CampaignRewards.TrustKind);
        grants.Coins = CampaignRewards.Grant(candidate, LevelId, CampaignRewards.IncomeReview, CampaignRewards.CoinsKind);
        return grants;
    }

    /// <summary>
    /// The approved corrected case (with the level result): 100 coins once.
    /// Refused (no change) unless the income review is corrected and the
    /// Auditor actually approved; the review claim is completed first when a
    /// failed earlier save left it out, so one write records both.
    /// </summary>
    public static Grants ApplyCaseResult(PlayerProgressData candidate, Level2CaseFacts facts)
    {
        var grants = new Grants();
        if (candidate == null || !candidate.HasCampaign || !facts.HasReview || !facts.QuestCorrected || !facts.Approved) return grants;
        Grants quest = ApplyCheckpointClaims(candidate, facts);
        grants.Changed = quest.Changed;
        grants.Trust = quest.Trust;
        if (CampaignRewards.CompleteActivity(candidate, LevelId, CampaignRewards.CaseApproved))
        {
            grants.Changed = true;
            grants.Coins = CampaignRewards.Grant(candidate, LevelId, CampaignRewards.CaseApproved, CampaignRewards.CoinsKind);
        }
        else grants.Coins = quest.Coins;
        grants.ChapterCompleted = CompleteChapterIfDone(candidate, ref grants.Changed);
        return grants;
    }

    /// <summary>
    /// Rosa's required follow-through: +4 trust once, only after the approved
    /// case is saved. It completes the chapter when everything required is done.
    /// </summary>
    public static Grants ApplyFollowThrough(PlayerProgressData candidate)
    {
        var grants = new Grants();
        if (candidate == null || !candidate.HasCampaign || !CaseClaimed(candidate)) return grants;
        if (CampaignRewards.CompleteActivity(candidate, LevelId, CampaignRewards.RosaFollowThrough))
        {
            grants.Changed = true;
            grants.Trust = CampaignRewards.Grant(candidate, LevelId, CampaignRewards.RosaFollowThrough, CampaignRewards.TrustKind);
        }

        RosaEthicsRecord ethics = candidate.campaign.rosaEthics;
        if (ethics != null && ethics.committed && ethics.followThroughRequired && !ethics.followThroughDone)
        {
            if (ethics.choiceId.EndsWith("/responsible", StringComparison.Ordinal))
            {
                CampaignRewards.Change support = CampaignRewards.ApplyEthicsDelta(candidate,
                    $"rosa-ethics/{ethics.caseId}/responsible-support-checked", 3,
                    "Rosa's supporting records were checked together");
                if (!support.Applied) return new Grants();
                RecordEthicsConsequence(ethics, candidate.campaign.campaignId, $"rosa-ethics/{ethics.caseId}/responsible-support-checked");
                grants.Trust = support;
            }
            else if (ethics.choiceId.EndsWith("/unsupported-promise", StringComparison.Ordinal))
            {
                string reversalId = $"rosa-ethics/{ethics.caseId}/unsupported-reversal";
                CampaignRewards.Change reversal = CampaignRewards.ApplyEthicsDelta(candidate, reversalId, -ethics.promiseTrustDelta,
                    "Rosa's unsupported promise was reversed");
                if (!reversal.Applied) return new Grants();
                RecordEthicsConsequence(ethics, candidate.campaign.campaignId, reversalId);
                string consequenceId = $"rosa-ethics/{ethics.caseId}/unsupported-consequence";
                CampaignRewards.Change consequence = CampaignRewards.ApplyEthicsDelta(candidate, consequenceId, -8,
                    "Rosa's records did not support the promise");
                if (!consequence.Applied) return new Grants();
                RecordEthicsConsequence(ethics, candidate.campaign.campaignId, consequenceId);
                grants.Trust = consequence;
            }
            ethics.followThroughDone = true;
            grants.Changed = true;
        }
        else if (ethics != null && ethics.committed && !ethics.followThroughRequired && !ethics.followThroughDone)
        {
            // Dismissive holds have no T19 follow-through bonus; the review remains open.
            ethics.followThroughDone = true;
        }

        if (!grants.Changed) return grants;
        grants.ChapterCompleted = CompleteChapterIfDone(candidate, ref grants.Changed);
        return grants;
    }

    private static bool CompleteChapterIfDone(PlayerProgressData candidate, ref bool changed)
    {
        if (!ChapterComplete(candidate) || !candidate.campaign.MarkChapterCompleted(LevelId)) return false;
        changed = true;
        return true;
    }
}
