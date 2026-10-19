using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The Auditor's review of Rosa's stamped folder (Level 2, T09), under
/// Level2Gameplay/Audit. Plugs into the existing hand-in through
/// AuditorInteractable.LevelAudit; Level 1 keeps its own audit.
///
/// FLOW: the tray accepts the folder (only with a current final stamp,
/// changed since the last returned/held review, inside the review window),
/// then FinalAudit.Submit decides and records the outcome ONCE, before any
/// dialogue (so a cancelled conversation, a cutoff or a second tap can never
/// produce a second result). The Auditor's lines point to the record or rule;
/// Rosa's short reaction follows.
/// - Approved: case reward once (capped deductions from charged root causes,
///   returned submissions and a late day), then the existing completion states.
/// - Supported hold: acknowledged; folder back, no reward, no cost.
/// - Premature approval / false hold / reason error: folder back with
///   targeted feedback; new root causes charged once each.
/// All review work (records, quest, schedule, findings) is kept.
/// After the Auditor's lines, the same Audit Summary card as Office Level 1
/// shows the outcome (approved, with totals and timing, before the level
/// completes; or what to fix) before the flow continues.
/// </summary>
public sealed class Level2Audit : MonoBehaviour
{
    public const string ChildName = "Audit";

    private const string Speaker = "Auditor";
    private const float DialogueWaitSeconds = 10f;

    private static Level2Audit instance;

    private void OnEnable()
    {
        instance = this;
        AuditorInteractable.LevelAudit = Handle;
    }

    private void OnDisable()
    {
        if (instance == this) instance = null;
        if (AuditorInteractable.LevelAudit == (System.Func<AuditorInteractable, bool>)Handle)
            AuditorInteractable.LevelAudit = null;
    }

    private static bool Handle(AuditorInteractable auditor)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        if (review == null || definition == null || !definition.HasReview || data == null) return false;

        if (!Level2FolderHandoff.CanHandInReview(review, true))
        {
            auditor.ReturnFolderForReview();
            return true;
        }

        var fieldKeys = new List<string>();
        foreach (ComplianceIssue issue in ComplianceChecker.RunCheck(data, definition))
            if (issue != null && !string.IsNullOrEmpty(issue.FieldKey)) fieldKeys.Add(issue.FieldKey);

        // Decide and record once.
        FinalAudit.Result result = FinalAudit.Submit(review, definition.review, fieldKeys);
        AuditLinesDefinition text = definition.review.audit ?? new AuditLinesDefinition();
        Debug.Log($"[Level2] Audit: {result.Outcome}; defects [{string.Join(", ", result.Defects)}]; newly charged {result.NewlyCharged.Count}.");

        var builder = new DialogueBuilder(Speaker);
        string rosa = null;
        switch (result.Outcome)
        {
            case FinalAudit.Outcome.Approved:
                if (text.approved != null)
                    foreach (string line in text.approved)
                        if (!string.IsNullOrWhiteSpace(line)) builder.Npc(line, "Auditor_Happy");
                // Explain the result, as the Level 1 Auditor does, before the summary card.
                builder.Npc("I checked your Reviewed Schedule against Rosa's records: May comes to " + Money(result.MayTotal) +
                    ", and the year to " + Money(result.AnnualTotal) + ".", "Auditor_Happy");
                if (CaseProgressionManager.Instance != null) CaseProgressionManager.Instance.RecordAuditAttempt(0, true);
                break;
            case FinalAudit.Outcome.SupportedHold:
                builder.Npc(text.justifiedHold, "Auditor_Default");
                rosa = text.rosaHold;
                break;
            case FinalAudit.Outcome.PrematureApproval:
                builder.Npc(text.prematureApproval, "Auditor_Disappointed");
                foreach (string line in FinalAudit.Feedback(result, text)) builder.Npc(line, "Auditor_Default");
                rosa = text.rosaPremature;
                break;
            case FinalAudit.Outcome.FalseHold:
                builder.Npc(text.falseHold, "Auditor_Disappointed");
                rosa = text.rosaFalseHold;
                break;
            case FinalAudit.Outcome.ReasonError:
                builder.Npc(text.reasonError, "Auditor_Default");
                foreach (string line in FinalAudit.Feedback(result, text)) builder.Npc(line, "Auditor_Default");
                break;
            default:
                auditor.ReturnFolderForReview();
                return true;
        }
        if (result.Returned && CaseProgressionManager.Instance != null)
            CaseProgressionManager.Instance.RecordAuditAttempt(result.NewlyCharged.Count, false);

        // The summary (or the return) follows only when the Auditor's last line is closed and his box has left the screen.
        System.Action conclude = () => Conclude(auditor, review, data, result, rosa, definition.review);
        System.Action after = () =>
        {
            if (instance != null) instance.StartCoroutine(AfterDialogueCloses(conclude));
            else conclude();
        };
        List<DialogueLine> lines = builder.Build();
        if (!auditor.RunLevelDialogue(lines, after))
        {
            // Another conversation is still closing: the decision stands, and
            // the Auditor explains it as soon as he can (as in Level 1).
            if (instance != null) instance.StartCoroutine(instance.TalkWhenFree(auditor, lines, after, result.Outcome));
            else Fallback(result.Outcome, after);
        }
        return true;
    }

    private System.Collections.IEnumerator TalkWhenFree(AuditorInteractable auditor, List<DialogueLine> lines, System.Action after,
        FinalAudit.Outcome outcome)
    {
        for (float waited = 0f; waited < DialogueWaitSeconds; waited += Time.unscaledDeltaTime)
        {
            if (!DialogueUI.IsAnyDialogueActive && auditor != null && auditor.RunLevelDialogue(lines, after)) yield break;
            yield return null;
        }
        Fallback(outcome, after);
    }

    private static System.Collections.IEnumerator AfterDialogueCloses(System.Action next)
    {
        yield return null;
        for (float waited = 0f; waited < 3f; waited += Time.unscaledDeltaTime)
        {
            if (!DialogueUI.IsAnyDialogueActive && !AnyDialoguePanelShowing()) break;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.25f);
        next();
    }

    private static bool AnyDialoguePanelShowing()
    {
        foreach (DialogueUI ui in Object.FindObjectsByType<DialogueUI>(FindObjectsInactive.Exclude))
            if (ui != null && ui.IsPanelShowing) return true;
        return false;
    }

    private static void Fallback(FinalAudit.Outcome outcome, System.Action after)
    {
        Level2Say.Line(outcome == FinalAudit.Outcome.Approved ? "The Auditor approved Rosa's review." : "The Auditor returned Rosa's folder.", null, 5f);
        after();
    }

    private static string Money(float amount) => "₱" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    private static void Conclude(AuditorInteractable auditor, CaseReviewState review, CaseData data, FinalAudit.Result result,
        string rosa, CaseReviewDefinition definition)
    {
        if (result.Outcome == FinalAudit.Outcome.Approved)
        {
            if (!FinalAudit.TryReward(review)) return;
            // On time is recorded and shown on the Audit Summary (no separate Arthur line).
            if (review.clock != null && review.clock.started && !review.clock.missed) review.audit.onTime = true;
            void Finish()
            {
                if (CaseProgressionManager.Instance != null) CaseProgressionManager.Instance.CalculateAndStoreCaseReward(data);
                // One neutral story fact (no accusation): Rosa confirms who prepared the first summary.
                if (PlayerProgressManager.Instance != null) PlayerProgressManager.Instance.RecordStoryFact(Level2Checkpoint.StoryFactDeskProvenance);
                auditor.CompleteLevelReview();
            }
            // The Audit Summary first, exactly like Office Level 1; the reward and level completion follow when it closes.
            if (!auditor.ShowLevelSummary(Summary(result, review, definition), "Audit Summary", Finish)) Finish();
            return;
        }
        void Return()
        {
            auditor.ReturnFolderForReview();
            GameplayEvents.RaiseAuditReturned();
            if (!string.IsNullOrWhiteSpace(rosa)) Level2Say.Line(rosa, null, 6f);
        }
        if (!auditor.ShowLevelSummary(Summary(result, review, definition), "Audit Summary", Return)) Return();
    }

    /// <summary>The Audit Summary card for Rosa's review, in the same layout as Office Level 1's.</summary>
    public static RewardScreenData Summary(FinalAudit.Result result, CaseReviewState review, CaseReviewDefinition definition)
    {
        AuditLinesDefinition text = definition?.audit ?? new AuditLinesDefinition();
        bool approved = result.Outcome == FinalAudit.Outcome.Approved;
        var data = new RewardScreenData
        {
            Title = "Audit Summary",
            ButtonLabel = approved ? "Continue" : "Review the case",
            Accent = approved ? new Color32(110, 201, 140, 255)
                : result.Outcome == FinalAudit.Outcome.SupportedHold ? new Color32(241, 194, 108, 255) : new Color32(226, 112, 96, 255),
            HasStatus = true,
            StatusGood = approved
        };
        string Money(float amount) => "₱" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        if (approved)
        {
            data.Subtitle = "The Auditor reviewed Rosa's case";
            data.StatusText = "APPROVED";
            data.StatusDetail = "Every earned payment has a supported place";
            data.Fact("May total", Money(result.MayTotal));
            data.Fact("Year total", Money(result.AnnualTotal));
            data.Fact("Returned reviews", (review?.audit?.failedSubmissions ?? 0).ToString());
            ReviewClockState clock = review?.clock;
            if (clock != null && clock.started)
                data.Fact("Review cutoff", clock.missed ? "Day " + clock.day + "  ·  late" : "On time");
            data.Summary = "Rosa's summary now lists every earned payment once, and the borrowed money stays out.";
            // A success needs no decision: it moves on by itself (tap to go now), as in Level 1.
            data.AutoContinueSeconds = 3.5f;
            return data;
        }

        data.StatusText = result.Outcome == FinalAudit.Outcome.SupportedHold ? "ON HOLD" : "RETURNED";
        switch (result.Outcome)
        {
            case FinalAudit.Outcome.SupportedHold:
                data.Subtitle = "The Auditor acknowledged your hold";
                data.StatusDetail = "A real problem is still open";
                data.Summary = text.justifiedHold;
                break;
            case FinalAudit.Outcome.FalseHold:
                data.Subtitle = "The Auditor returned Rosa's folder";
                data.StatusDetail = "The review was already supported";
                data.Summary = text.falseHold;
                break;
            default:
                data.Subtitle = "The Auditor returned Rosa's folder";
                data.StatusDetail = "Something still needs another look";
                data.Summary = result.Outcome == FinalAudit.Outcome.ReasonError ? text.reasonError : text.prematureApproval;
                break;
        }
        foreach (string line in FinalAudit.Feedback(result, text))
            if (!string.IsNullOrWhiteSpace(line)) data.Issues.Add(line);
        data.Footer = "Your work is kept. Fix what the Auditor pointed to, stamp again, then hand the folder back.";
        return data;
    }
}
