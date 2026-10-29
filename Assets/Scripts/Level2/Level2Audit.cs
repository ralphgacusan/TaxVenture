using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The Auditor's review of Rosa's stamped folder (Level 2, T09), under
/// Level2Gameplay/Audit. Level 2 consistency pass: the hand-in is Level 1's
/// own talk-first conversation (AuditorInteractable): tap the Auditor, he
/// asks for the folder and waits, the player drags Rosa's Case Folder onto
/// him (or taps its icon), then this review decides through
/// AuditorInteractable.ILevelHandIn. No tray drop, no second submission path.
///
/// FLOW: before he takes the folder, Refusal() says why it cannot be handed in
/// yet (no current final stamp, unchanged since a returned/held review, or the
/// review window closed); nothing is decided or charged. With the folder in his
/// hands, Decide() runs FinalAudit.Submit ONCE (so a cancelled conversation,
/// a cutoff or a second tap can never produce a second result). His lines point
/// to the record or rule; the same Audit Summary card as Level 1 follows.
/// - Approved: case reward once (capped deductions from charged root causes,
///   returned submissions and a late day), then the existing completion states;
///   Rosa's Client Copy continues his conversation (ClientCopyHandover).
/// - Supported hold / premature approval / false hold / reason error: the
///   folder goes back to the desk with targeted feedback; new root causes are
///   charged once each. All review work (records, quest, schedule, findings,
///   stamps) is kept.
/// </summary>
public sealed class Level2Audit : MonoBehaviour, AuditorInteractable.ILevelHandIn
{
    public const string ChildName = "Audit";

    private FinalAudit.Result decided;
    private string rosaReaction;

    private void OnEnable() => AuditorInteractable.LevelHandIn = this;

    private void OnDisable()
    {
        if (ReferenceEquals(AuditorInteractable.LevelHandIn, this)) AuditorInteractable.LevelHandIn = null;
    }

    private static CaseDefinition Definition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    // =========================================================
    // ILevelHandIn
    // =========================================================

    public string Refusal()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (review == null || definition == null || !definition.HasReview) return "I can't take this folder right now.";
        return Level2FolderHandoff.HandInRefusal(review);
    }

    public AuditorInteractable.LevelHandInDecision Decide()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        if (review == null || definition == null || !definition.HasReview || data == null) return null;
        if (Level2FolderHandoff.HandInRefusal(review) != null) return null;

        var fieldKeys = new List<string>();
        foreach (ComplianceIssue issue in ComplianceChecker.RunCheck(data, definition))
            if (issue != null && !string.IsNullOrEmpty(issue.FieldKey)) fieldKeys.Add(issue.FieldKey);

        // Decide and record once.
        FinalAudit.Result result = FinalAudit.Submit(review, definition.review, fieldKeys);
        AuditLinesDefinition text = definition.review.audit ?? new AuditLinesDefinition();
        Debug.Log($"[Level2] Audit: {result.Outcome}; defects [{string.Join(", ", result.Defects)}]; newly charged {result.NewlyCharged.Count}.");

        var builder = new DialogueBuilder("Auditor");
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
                // Not admitted after all (nothing was recorded): the folder goes back.
                return null;
        }
        if (result.Returned && CaseProgressionManager.Instance != null)
            CaseProgressionManager.Instance.RecordAuditAttempt(result.NewlyCharged.Count, false);

        decided = result;
        rosaReaction = rosa;
        var decision = new AuditorInteractable.LevelHandInDecision
        {
            Approved = result.Outcome == FinalAudit.Outcome.Approved,
            Summary = Summary(result, review, definition.review)
        };
        foreach (DialogueLine line in builder.Build())
            if (line != null && !string.IsNullOrWhiteSpace(line.Text)) decision.Lines.Add(line);
        return decision;
    }

    public void Approve()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        if (review == null || decided == null || decided.Outcome != FinalAudit.Outcome.Approved) return;
        decided = null;
        // The reward applies once, however often this is reached.
        if (!FinalAudit.TryReward(review)) return;
        // On time is recorded and shown on the Audit Summary (no separate Arthur line).
        if (review.clock != null && review.clock.started && !review.clock.missed) review.audit.onTime = true;
        if (CaseProgressionManager.Instance != null && data != null) CaseProgressionManager.Instance.CalculateAndStoreCaseReward(data);
        // One neutral story fact (no accusation): Rosa confirms who prepared the first summary.
        if (PlayerProgressManager.Instance != null) PlayerProgressManager.Instance.RecordStoryFact(Level2Checkpoint.StoryFactDeskProvenance);
        AuditorInteractable auditor = AuditorInteractable.Current;
        if (auditor != null) auditor.CompleteLevelReview();
    }

    public void Returned()
    {
        decided = null;
        AuditorInteractable auditor = AuditorInteractable.Current;
        if (auditor != null) auditor.ReturnFolderForReview();
        GameplayEvents.RaiseAuditReturned();
        string rosa = rosaReaction;
        rosaReaction = null;
        if (!string.IsNullOrWhiteSpace(rosa)) Level2Say.Line(rosa, null, 6f);
    }

    private static string Money(float amount) => "₱" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

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
