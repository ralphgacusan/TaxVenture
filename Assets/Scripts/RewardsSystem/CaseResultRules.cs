using System;

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
/// - Next step: Marie's follow-through (T08, CompleteFollowThrough). Until
///   T16 adds the chapter wrap-up, finishing the follow-through also
///   completes Chapter 1 (a temporary wrap-up bridge).
/// </summary>
public static class CaseResultRules
{
    public enum Outcome { Applied, AlreadyRecorded, Rejected }

    /// <summary>The player-facing next step after the case result.</summary>
    public const string NextStepText = "Next: Marie's follow-through.";

    /// <summary>
    /// The card after Marie has her copy: the follow-through also completed
    /// Chapter 1 (T16 bridge, CF-41), so it reads Level Complete.
    /// </summary>
    public const string CompletedTitle = "Level Complete";
    public const string CompletedStatus = "LEVEL COMPLETE";
    public const string CompletedDetail = "Marie's return approved  ·  Client copy delivered";
    public const string CompletedSummary = "Chapter 1 is done. Your career board shows what's next.";

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
    /// Public Trust +4 (claimed once) and, as the TEMPORARY wrap-up bridge
    /// until T16, Chapter 1's completion, the "follow-through done" checkpoint
    /// phase and a pending finale/card marker, all in one write. The checkpoint
    /// remains until that end flow is acknowledged. Never without a recorded
    /// case result. Returns the trust change (Rejected when nothing was pending).
    /// </summary>
    public static CampaignRewards.Change CompleteFollowThrough(PlayerProgressData candidate)
    {
        if (!FollowThroughPending(candidate))
            return new CampaignRewards.Change { Outcome = CampaignRewards.Outcome.Rejected, Kind = CampaignRewards.TrustKind };
        CampaignRewards.CompleteActivity(candidate, CampaignRewards.Level1, CampaignRewards.FollowThrough);
        CampaignRewards.Change trust = CampaignRewards.Grant(candidate, CampaignRewards.Level1, CampaignRewards.FollowThrough, CampaignRewards.TrustKind);
        // T16 bridge: the chapter wrap-up does not exist yet, so the follow-through completes Chapter 1.
        candidate.campaign.MarkChapterCompleted(CampaignRecord.FirstChapterId);
        if (candidate.campaign.levelOne == null) candidate.campaign.levelOne = new Level1FieldCheckpoint();
        candidate.campaign.levelOne.handoverPhase = Level1FieldCheckpoint.HandoverDone;
        candidate.campaign.levelOneEndFlowPending = true;
        candidate.ClearCheckpoint(CampaignRecord.FirstChapterId);
        return trust;
    }
}
