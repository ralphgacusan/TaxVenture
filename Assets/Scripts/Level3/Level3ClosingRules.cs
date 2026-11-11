using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// The Bea-only chapter result (T14), read from what is already frozen in the
/// chapter snapshot: the worksheet's first check (twelve-decision v1 or
/// nine-decision v2, never regraded) and Bea's approved computation review.
/// Nothing is recalculated, and a grade that was never saved stays unknown
/// (0 XP, no badge), never invented.
/// </summary>
public struct Level3ChapterResult
{
    /// <summary>Worksheet (60) + Bea's case (100): the current Bea-only chapter maximum.</summary>
    public const int MaxExp = Level3WorksheetCheck.MaxXp + Level3ReviewRules.MaxXp;

    public bool WorksheetKnown;
    /// <summary>"L3-worksheet-v1" (twelve decisions) or "L3-worksheet-v2" (nine).</summary>
    public string WorksheetVersion;
    public int WorksheetCorrect;
    public int WorksheetTotal;
    /// <summary>BadgeTier as a number: 1 bronze, 2 silver, 3 gold (0 unknown).</summary>
    public int WorksheetTier;
    public int WorksheetXp;
    public bool CaseKnown;
    public int CaseXp;
    public string BadgeId;

    public int Exp => WorksheetXp + CaseXp;
}

/// <summary>What the acknowledged wrap-up wrote (T14), for presentation after the save.</summary>
public struct Level3WrapUpGrants
{
    public bool Changed;
    public bool AlreadyDone;
    public bool ChapterCompleted;
    /// <summary>How much the career EXP total grew (only an improvement on the chapter's best).</summary>
    public int ExpGain;
    public Level3ChapterResult Result;
}

/// <summary>
/// PURPOSE:
/// Level 3 T14: Bea's supported finding, Quax's message and the one chapter
/// result. Pure: no Unity calls. After Bea's explained delivery (T10) the
/// chapter waits in ClosingFinding with Bea still the active case:
/// - RecordFinding(): the player states what her own file shows. Only the
///   supported statement is saved (its action id and milestone; the phase
///   stays): her draft (DRAFT-B26) listed the private party purchases and the
///   printing bill a second time; her originals support the schedule total.
///   A statement about intent or a wider pattern is Refused (nothing saved,
///   no penalty): one file shows a discrepancy and its correction, not why it
///   happened, who approved it, or anything about other files.
/// - ReadMessage(): the receptionist delivered Quax's message after the saved
///   finding: ClosingFinding -> WrapUpPending (the result card is pending).
/// - Acknowledge(): the explicit Continue on the chapter result:
///   WrapUpPending -> CompletedOffice. ApplyWrapUp() puts the chapter's
///   completion, its wrap-up activity and the aggregated mastery record
///   (frozen worksheet XP + Bea's case XP, at most 160, the worth_the_tax_3
///   badge tier) on the same candidate save. No coins or Public Trust are
///   granted here: those were claimed once by their own activities; a repeat
///   or a completed chapter writes nothing more.
/// Every command validates phase, case (Bea active, company never admitted),
/// delivery, prerequisites and its action id; a repeat is a Duplicate.
/// The deferred company closing (CompanyDelivery -> ClosingFinding with the
/// company active) is not this route and is left untouched.
/// </summary>
public static class Level3ClosingRules
{
    private const string Bea = Level3ChapterRules.BeaCaseId;

    public const string FindingAction = "Level_03/bea-finding-recorded";
    public const string FindingMilestone = "bea-finding-recorded";
    public const string MessageAction = "Level_03/quax-message-read";
    public const string MessageMilestone = "quax-message-read";
    public const string AcknowledgeAction = "Level_03/chapter-acknowledged";
    public const string AcknowledgedMilestone = "chapter-acknowledged";
    /// <summary>The story activity the wrap-up records ("Level_03/chapter-wrap-up"; Level 1 uses the same activity name; no grant of its own).</summary>
    public const string WrapUpActivity = CaseResultRules.WrapUpActivity;

    /// <summary>The statements the player can make about Bea's file (only the first is supported by it).</summary>
    public const string Supported = "supported";
    public const string Intent = "intent";
    public const string Pattern = "pattern";
    public static readonly string[] Statements = { Supported, Intent, Pattern };

    /// <summary>The papers of her file the finding rests on (her draft, the private receipt, the printing bill, the cost schedule).</summary>
    public static readonly string[] FindingSources = { "B-DRAFT", "B-PERSONAL", "B-PRINT", "B-EXP" };
    public const string DraftSource = "B-DRAFT";
    public const string ScheduleSource = "B-EXP";

    public const string RefuseNotNow = "There's nothing to close on Bea's file yet.";
    public const string RefuseUnsupported = "Her papers don't show that.";
    public const string RefuseNoFile = "Bea's draft and receipts aren't in her file.";

    /// <summary>Where the chapter's closing stands.</summary>
    public enum Stage { None, Finding, Message, Result, Completed }

    private static bool Has(Level3ChapterCheckpoint state, string action) => state?.actionIds != null && state.actionIds.Contains(action);

    /// <summary>The Bea-only closing (delivered copy, Bea active, the company never admitted).</summary>
    public static Stage StageOf(Level3ChapterCheckpoint state)
    {
        if (state == null || !Level3DeliveryRules.Delivered(state) || state.activeCaseId != Bea) return Stage.None;
        if (state.Case(Level3ChapterRules.CompanyCaseId)?.admitted == true) return Stage.None;
        bool finding = Has(state, FindingAction), message = Has(state, MessageAction), acknowledged = Has(state, AcknowledgeAction);
        switch (Level3ChapterRules.PhaseOf(state))
        {
            case Level3Phase.ClosingFinding: return message || acknowledged ? Stage.None : finding ? Stage.Message : Stage.Finding;
            case Level3Phase.WrapUpPending: return finding && message && !acknowledged ? Stage.Result : Stage.None;
            case Level3Phase.CompletedOffice: return finding && message && acknowledged ? Stage.Completed : Stage.None;
            default: return Stage.None;
        }
    }

    public static bool FindingRecorded(Level3ChapterCheckpoint state) => StageOf(state) >= Stage.Message;
    public static bool MessageRead(Level3ChapterCheckpoint state) => StageOf(state) >= Stage.Result;
    public static bool Completed(Level3ChapterCheckpoint state) => StageOf(state) == Stage.Completed;

    /// <summary>A usable snapshot at the expected closing stage.</summary>
    private static string Problem(Level3ChapterCheckpoint candidate, Stage expected) =>
        candidate == null || Level3ChapterRules.Problem(candidate) != null || StageOf(candidate) != expected ? RefuseNotNow : null;

    /// <summary>
    /// The player stated what Bea's file shows. Only the supported statement
    /// is saved (its action id and milestone; the phase stays ClosingFinding).
    /// Intent or a wider pattern is Refused: nothing saved, no penalty.
    /// </summary>
    public static Level3CommandOutcome RecordFinding(Level3ChapterCheckpoint candidate, string statementId, out string reason)
    {
        reason = null;
        if (Has(candidate, FindingAction)) { reason = "already recorded"; return Level3CommandOutcome.Duplicate; }
        reason = Problem(candidate, Stage.Finding);
        if (reason != null) return Level3CommandOutcome.Refused;
        List<string> file = candidate.Case(Bea).acquiredSourceIds;
        if (file == null || !Array.TrueForAll(FindingSources, file.Contains)) { reason = RefuseNoFile; return Level3CommandOutcome.Refused; }
        if (Array.IndexOf(Statements, statementId) < 0 || statementId != Supported) { reason = RefuseUnsupported; return Level3CommandOutcome.Refused; }
        candidate.actionIds.Add(FindingAction);
        if (!candidate.milestones.Contains(FindingMilestone)) candidate.milestones.Add(FindingMilestone);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>Quax's message was delivered at reception after the saved finding: ClosingFinding -> WrapUpPending.</summary>
    public static Level3CommandOutcome ReadMessage(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (Has(candidate, MessageAction)) { reason = "already read"; return Level3CommandOutcome.Duplicate; }
        reason = Problem(candidate, Stage.Message);
        if (reason != null) return Level3CommandOutcome.Refused;
        Level3CommandOutcome step = Level3ChapterRules.TryAdvance(candidate, Level3Phase.ClosingFinding, Level3Phase.WrapUpPending, MessageAction, out reason);
        if (step != Level3CommandOutcome.Applied) return step;
        if (!candidate.milestones.Contains(MessageMilestone)) candidate.milestones.Add(MessageMilestone);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>The explicit Continue on the pending chapter result: WrapUpPending -> CompletedOffice.</summary>
    public static Level3CommandOutcome Acknowledge(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (Has(candidate, AcknowledgeAction)) { reason = "already acknowledged"; return Level3CommandOutcome.Duplicate; }
        reason = Problem(candidate, Stage.Result);
        if (reason != null) return Level3CommandOutcome.Refused;
        Level3CommandOutcome step = Level3ChapterRules.TryAdvance(candidate, Level3Phase.WrapUpPending, Level3Phase.CompletedOffice, AcknowledgeAction, out reason);
        if (step != Level3CommandOutcome.Applied) return step;
        if (!candidate.milestones.Contains(AcknowledgedMilestone)) candidate.milestones.Add(AcknowledgedMilestone);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>
    /// The chapter result from the frozen saved results: the worksheet's first
    /// check (v1 or v2 exactly as graded) and Bea's approved case XP. A grade
    /// that was never frozen stays unknown (0 XP, no badge tier).
    /// </summary>
    public static Level3ChapterResult Result(Level3ChapterCheckpoint state)
    {
        var result = new Level3ChapterResult { BadgeId = Level3WorksheetCheck.BadgeId, WorksheetVersion = "" };
        Level3WorksheetResult check = state?.worksheetCheck;
        if (Level3WorksheetCheck.Collected(state) && check != null && check.firstChecked && check.firstTier >= 1 && check.firstTier <= 3)
        {
            result.WorksheetKnown = true;
            result.WorksheetVersion = check.assessmentVersion ?? "";
            result.WorksheetCorrect = Math.Max(0, check.firstCorrect);
            result.WorksheetTotal = Math.Max(0, check.firstTotal);
            result.WorksheetTier = check.firstTier;
            result.WorksheetXp = Math.Max(0, Math.Min(Level3WorksheetCheck.MaxXp, check.firstXp));
        }
        Level3ReviewRecord review = state?.Case(Bea)?.review;
        if (review != null && review.approved)
        {
            result.CaseKnown = true;
            result.CaseXp = Math.Max(0, Math.Min(Level3ReviewRules.MaxXp, review.xp));
        }
        return result;
    }

    /// <summary>
    /// On the candidate save of the acknowledged chapter: the wrap-up activity,
    /// Level 3's completion and its mastery record (aggregated XP, best kept,
    /// badge tier), once. Coins and Public Trust are never touched. A chapter
    /// already completed is AlreadyDone (nothing more is written).
    /// </summary>
    public static Level3WrapUpGrants ApplyWrapUp(PlayerProgressData candidate, Level3ChapterCheckpoint state)
    {
        var grants = new Level3WrapUpGrants { Result = Result(state) };
        if (candidate == null || !candidate.HasCampaign || StageOf(state) != Stage.Completed) return grants;
        if (candidate.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) ||
            CampaignRewards.IsActivityCompleted(candidate, CampaignRewards.Level3, WrapUpActivity))
        {
            grants.AlreadyDone = true;
            return grants;
        }
        CampaignRewards.CompleteActivity(candidate, CampaignRewards.Level3, WrapUpActivity);
        grants.ChapterCompleted = candidate.campaign.MarkChapterCompleted(Level3ChapterRules.LevelId);
        Level3ChapterResult result = grants.Result;
        var badges = new List<string>();
        var tiers = new List<int>();
        if (result.WorksheetKnown) { badges.Add(result.BadgeId); tiers.Add(result.WorksheetTier); }
        var run = new MasteryRun
        {
            LevelId = Level3ChapterRules.LevelId,
            HasScore = true,
            Exp = result.Exp,
            MaxExp = Level3ChapterResult.MaxExp,
            Hearts = 0,
            MaxHearts = 0,
            Passed = true,
            Badges = badges,
            BadgeTiers = tiers
        };
        grants.ExpGain = MasteryBoundary.ApplyRun(candidate, run).expGain;
        grants.Changed = true;
        return grants;
    }

    /// <summary>
    /// T16: a mastery replay's acknowledged local chapter on a candidate save of a campaign that completed
    /// Chapter 3 in the story: its mastery record only (best XP kept, career total grows by an improvement,
    /// badge tier kept at its best; each worksheet grade's tier comes from its own assessment version, so a
    /// nine-decision v2 replay is compared to a twelve-decision v1 story grade by tier and the shared 60-point
    /// worksheet XP scale, never by raw counts, and the two are never added). The story's snapshot, frozen
    /// first grades, completion, activities, coins and Trust are untouched.
    /// </summary>
    public static Level3WrapUpGrants ApplyReplayBest(PlayerProgressData candidate, Level3ChapterCheckpoint replay)
    {
        var grants = new Level3WrapUpGrants { Result = Result(replay) };
        if (candidate == null || !candidate.HasCampaign || !candidate.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) || StageOf(replay) != Stage.Completed)
            return grants;
        Level3ChapterResult result = grants.Result;
        var badges = new List<string>();
        var tiers = new List<int>();
        if (result.WorksheetKnown) { badges.Add(result.BadgeId); tiers.Add(result.WorksheetTier); }
        grants.ExpGain = MasteryBoundary.ApplyRun(candidate, new MasteryRun
        {
            LevelId = Level3ChapterRules.LevelId, HasScore = true, Exp = result.Exp, MaxExp = Level3ChapterResult.MaxExp,
            Hearts = 0, MaxHearts = 0, Passed = true, Badges = badges, BadgeTiers = tiers
        }).expGain;
        grants.Changed = true;
        return grants;
    }

    /// <summary>The share of a frozen worksheet first check that was right (correct / its own decision count: 12 for v1, 9 for v2); 0 when unknown.</summary>
    public static double NormalizedAccuracy(Level3ChapterResult result) =>
        result.WorksheetKnown && result.WorksheetTotal > 0 ? (double)result.WorksheetCorrect / result.WorksheetTotal : 0d;

    // =========================================================
    // THE FINDING'S FIGURES (from Bea's own case file, exact)
    // =========================================================

    /// <summary>Her draft's proposed costs minus the supported cost schedule ("" when the file lacks either total).</summary>
    public static string DraftOverstatement(Level3CaseContent content)
    {
        if (!TryTotal(content, DraftSource, out decimal draft) || !TryTotal(content, ScheduleSource, out decimal schedule)) return "";
        return (draft - schedule).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>The draft's lines that claim a cost the originals do not support (relation "claim:"), in file order.</summary>
    public static List<Level3RowDefinition> UnsupportedDraftLines(Level3CaseContent content)
    {
        var lines = new List<Level3RowDefinition>();
        if (content?.rows == null) return lines;
        foreach (Level3RowDefinition row in content.rows)
            if (row != null && row.sourceId == DraftSource && row.relation != null && row.relation.StartsWith("claim:", StringComparison.Ordinal))
                lines.Add(row);
        return lines;
    }

    private static bool TryTotal(Level3CaseContent content, string sourceId, out decimal value)
    {
        value = 0m;
        Level3TotalDefinition total = content?.totals?.Find(t => t != null && t.sourceId == sourceId);
        return total != null && decimal.TryParse(total.amount, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
