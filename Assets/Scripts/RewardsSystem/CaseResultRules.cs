using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Level 1's story result after Marie's case (Campaign Foundation T07), pure
/// C# so the regression suite checks it directly.
///
/// - Story progress follows CORRECTED WORK, never score: the case result is
///   recorded only for a story run of a campaign when the classification
///   quest was corrected (all four right) and the main case was approved.
///   The frozen first-check tier (T06), XP and badges are mastery facts on
///   the result; none of them can block or allow the story.
/// - Once only: a stable scoped action id ("Level_01/case-result/<case id>")
///   is claimed in the same write as the result and its mastery numbers. A
///   repeated input, reload or relaunch finds the claim and changes nothing.
///   A replay is not a story run and never reaches Apply.
/// - Callers apply this to a CANDIDATE copy of the save, write it, and only
///   then make it live (PlayerProgressManager.CommitLevelOneCaseResult), so a
///   failed write leaves the old state and shows no success.
/// - Next step: Marie's follow-through (T08, CompleteFollowThrough), then
///   the Chapter 1 wrap-up (T16, CompleteWrapUp): only the wrap-up completes
///   the chapter, after the follow-through, in one write with the end of the
///   Level 1 checkpoint. A result-card click or a shop visit never does.
/// </summary>
public static class CaseResultRules
{
    public enum Outcome { Applied, AlreadyRecorded, Rejected }

    /// <summary>The player-facing next step after the case result.</summary>
    public const string NextStepText = "Next: Marie's follow-through.";

    /// <summary>
    /// The card after Marie has her copy (T16, revised 2026-10-26): it is the
    /// Chapter 1 card. Its Continue completes Chapter 1 (CompleteWrapUp, one
    /// write) and goes to the career board.
    /// </summary>
    public const string CompletedTitle = "Chapter 1 Complete";
    public const string CompletedStatus = "CHAPTER 1 COMPLETE";
    public const string CompletedDetail = "Marie's return approved  ·  Her copy delivered";
    public const string CompletedSummary = "Your career board shows what's next.";

    /// <summary>The story activity the Chapter 1 wrap-up records (T16; no grant of its own).</summary>
    public const string WrapUpActivity = "chapter-wrap-up";

    /// <summary>The stable once-only id for a chapter's case result.</summary>
    public static string ActionId(string chapterId, string caseId) =>
        $"{(chapterId ?? "").Trim()}/case-result/{(caseId ?? "").Trim()}";

    /// <summary>The classification quest is corrected once its board was solved (all four right).</summary>
    public static bool QuestCorrected(Level1FieldCheckpoint checkpoint) =>
        checkpoint != null && (checkpoint.boardPhase == Level1FieldCheckpoint.PhaseSolved ||
                               checkpoint.boardPhase == Level1FieldCheckpoint.PhaseRewarded);

    /// <summary>Story runs only, after the corrected quest and the approved case. Score never enters.</summary>
    public static bool CanRecord(bool storyRun, bool questCorrected, bool caseApproved) =>
        storyRun && questCorrected && caseApproved;

    /// <summary>The frozen first-check tier as a BadgeTier value (0 when the board has no first check).</summary>
    public static int TierFor(Level1FieldCheckpoint checkpoint) =>
        checkpoint != null && checkpoint.checkSubmitted && checkpoint.checkCorrectCount >= 0
            ? ClassificationCheck.TierFor(checkpoint.checkCorrectCount) : 0;

    /// <summary>
    /// Builds the result of the approved case from the saved Level 1
    /// checkpoint and the run's mastery numbers (no score decides anything here).
    /// </summary>
    public static Level1CaseResult Build(string chapterId, Level1FieldCheckpoint checkpoint, string caseId, string caseFingerprint,
        bool caseApproved, int exp, int maxExp, int scoreHearts, int maxScoreHearts, float runSeconds)
    {
        string id = string.IsNullOrWhiteSpace(caseId) ? (checkpoint?.caseRecord?.caseId ?? "") : caseId.Trim();
        return new Level1CaseResult
        {
            recorded = true,
            actionId = ActionId(chapterId, id),
            caseId = id,
            caseFingerprint = string.IsNullOrWhiteSpace(caseFingerprint) ? (checkpoint?.caseRecord?.caseFingerprint ?? "") : caseFingerprint,
            questCorrected = QuestCorrected(checkpoint),
            caseApproved = caseApproved,
            firstCheckCorrect = checkpoint != null && checkpoint.checkSubmitted ? checkpoint.checkCorrectCount : -1,
            tier = TierFor(checkpoint),
            exp = Math.Max(0, exp),
            maxExp = Math.Max(0, maxExp),
            scoreHearts = Math.Max(0, scoreHearts),
            maxScoreHearts = Math.Max(0, maxScoreHearts),
            runSeconds = Math.Max(0f, runSeconds),
            nextStep = Level1CaseResult.NextFollowThrough
        };
    }

    /// <summary>
    /// Applies the first case result to a candidate save: the result, its
    /// claim and the run's mastery record together. Already recorded (same
    /// claim or any earlier result): no change. Rejected when the result does
    /// not meet the corrected-work rule or there is no campaign.
    /// </summary>
    public static Outcome Apply(PlayerProgressData candidate, Level1CaseResult result, MasteryRun? mastery)
    {
        if (candidate == null || !candidate.HasCampaign || result == null || !result.recorded ||
            !result.questCorrected || !result.caseApproved || string.IsNullOrWhiteSpace(result.actionId))
            return Outcome.Rejected;

        CampaignRecord campaign = candidate.campaign;
        if (campaign.levelOneCaseResult == null) campaign.levelOneCaseResult = new Level1CaseResult();
        if (campaign.levelOneCaseResult.recorded || campaign.rewardClaims.Contains(result.actionId))
            return Outcome.AlreadyRecorded;

        campaign.levelOneCaseResult = result;
        campaign.rewardClaims.Add(result.actionId);
        if (mastery.HasValue) MasteryBoundary.ApplyRun(candidate, mastery.Value);
        // T09: the approved corrected case pays its 100 coins once, in the same write.
        CampaignRewards.CompleteActivity(candidate, CampaignRewards.Level1, CampaignRewards.CaseApproved);
        CampaignRewards.Grant(candidate, CampaignRewards.Level1, CampaignRewards.CaseApproved, CampaignRewards.CoinsKind);
        return Outcome.Applied;
    }

    /// <summary>A recorded case result waits for Marie's follow-through (T08) until it is done.</summary>
    public static bool FollowThroughPending(PlayerProgressData data) =>
        data != null && data.HasCampaign && data.campaign.levelOneCaseResult != null && data.campaign.levelOneCaseResult.recorded &&
        !CampaignRewards.IsActivityCompleted(data, CampaignRewards.Level1, CampaignRewards.FollowThrough);

    /// <summary>
    /// Marie's follow-through (T08), on a candidate save: the activity, its
    /// Public Trust +4 (claimed once), the "follow-through done" checkpoint
    /// phase and a pending finale/card marker, all in one write. It does NOT
    /// complete Chapter 1 (T16: the wrap-up does, CompleteWrapUp). Never
    /// without a recorded case result. Returns the trust change (Rejected
    /// when nothing was pending).
    /// </summary>
    public static CampaignRewards.Change CompleteFollowThrough(PlayerProgressData candidate)
    {
        if (!FollowThroughPending(candidate))
            return new CampaignRewards.Change { Outcome = CampaignRewards.Outcome.Rejected, Kind = CampaignRewards.TrustKind };
        CampaignRewards.CompleteActivity(candidate, CampaignRewards.Level1, CampaignRewards.FollowThrough);
        CampaignRewards.Change trust = CampaignRewards.Grant(candidate, CampaignRewards.Level1, CampaignRewards.FollowThrough, CampaignRewards.TrustKind);
        if (candidate.campaign.levelOne == null) candidate.campaign.levelOne = new Level1FieldCheckpoint();
        candidate.campaign.levelOne.handoverPhase = Level1FieldCheckpoint.HandoverDone;
        candidate.campaign.levelOneEndFlowPending = true;
        candidate.ClearCheckpoint(CampaignRecord.FirstChapterId);
        return trust;
    }

    /// <summary>The wrap-up is next: Marie's follow-through is done and its finale/card end flow is still pending.</summary>
    public static bool WrapUpPending(PlayerProgressData data) =>
        data != null && data.HasCampaign && data.campaign.levelOneEndFlowPending &&
        CampaignRewards.IsActivityCompleted(data, CampaignRewards.Level1, CampaignRewards.FollowThrough);

    /// <summary>
    /// The Chapter 1 wrap-up (T16), on a candidate save: the wrap-up activity,
    /// Chapter 1's completion, the end of the pending end flow and of the
    /// Level 1 checkpoint, in one write. Only after the follow-through; a
    /// second call changes nothing. True when it applied.
    /// </summary>
    public static bool CompleteWrapUp(PlayerProgressData candidate)
    {
        if (!WrapUpPending(candidate)) return false;
        CampaignRewards.CompleteActivity(candidate, CampaignRewards.Level1, WrapUpActivity);
        candidate.campaign.MarkChapterCompleted(CampaignRecord.FirstChapterId);
        candidate.campaign.levelOneEndFlowPending = false;
        candidate.campaign.levelOne = new Level1FieldCheckpoint();
        candidate.ClearCheckpoint(CampaignRecord.FirstChapterId);
        return true;
    }

    /// <summary>One earned line on the wrap-up sheet.</summary>
    public sealed class SummaryLine
    {
        public string Kind = "";
        public string Reason = "";
        public int Amount;
    }

    /// <summary>What the Chapter 1 wrap-up sheet shows, read from the committed save (never from presentation).</summary>
    public sealed class ChapterSummary
    {
        public readonly List<SummaryLine> Coins = new List<SummaryLine>();
        public readonly List<SummaryLine> Trust = new List<SummaryLine>();
        public int CoinsEarned;
        public int Wallet;
        public int TrustEarned;
        public int TrustNow;
        public int TrustTarget = CampaignRewards.SupportTarget;
        public int Tier;
        public int FirstCheckCorrect = -1;
    }

    /// <summary>
    /// The chapter's earned coins and trust (this level's ledger lines, in
    /// order; purchases and repairs are not earnings), the live wallet and
    /// trust, and the frozen first-check tier (mastery, optional).
    /// </summary>
    public static ChapterSummary BuildChapterSummary(PlayerProgressData data, string levelId = CampaignRewards.Level1)
    {
        var summary = new ChapterSummary();
        if (data == null || !data.HasCampaign) return summary;
        CampaignRecord campaign = data.campaign;
        string prefix = (campaign.campaignId ?? "").Trim() + "/" + (levelId ?? "").Trim() + "/";
        if (campaign.ledger != null)
            foreach (CampaignLedgerEntry entry in campaign.ledger)
            {
                if (entry == null || entry.amount <= 0 || entry.claimId == null || !entry.claimId.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var line = new SummaryLine { Kind = entry.kind, Reason = entry.reason ?? "", Amount = entry.amount };
                if (entry.kind == CampaignRewards.CoinsKind) { summary.Coins.Add(line); summary.CoinsEarned += entry.amount; }
                else if (entry.kind == CampaignRewards.TrustKind) { summary.Trust.Add(line); summary.TrustEarned += entry.amount; }
            }
        summary.Wallet = campaign.coins;
        summary.TrustNow = campaign.publicTrust;
        if (campaign.levelOneCaseResult != null && campaign.levelOneCaseResult.recorded)
        {
            summary.Tier = campaign.levelOneCaseResult.tier;
            summary.FirstCheckCorrect = campaign.levelOneCaseResult.firstCheckCorrect;
        }
        return summary;
    }
}
