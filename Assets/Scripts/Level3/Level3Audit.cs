using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The Auditor's review of Bea's stamped computation (Level 3 T09), under
/// Level3Gameplay/Audit. The hand-in is the existing talk-first conversation
/// (AuditorInteractable, the same as Levels 1-2): tap the Auditor, he asks
/// for the Case Folder and waits; drag the carried folder from the bar onto
/// him (or tap its icon). This class is its ILevelHandIn, and its
/// ILevelFolder: Level 3's folder is the carried inventory folder (the desk
/// folder stays hidden, T02), so "in hand" means it sits in the bar; in the
/// Bag his existing "Take it out" prompt appears first.
///
/// - Refusal(): why he can't take it yet (Level3ReviewRules.Refusal: another
///   case, already approved, no stamp, today's window closed, the same sheet
///   and stamp as last time); nothing is decided or charged.
/// - Decide(): ONE chapter command (Level3Checkpointer.CommitReview), saved
///   first. His lines name the earliest open line and its book section, never
///   a value to type; the same Audit Summary card as Levels 1-2 follows.
///   Approval: the approved sheet's own figures, 100 coins and the case XP
///   (written with it); Bea's copy waits with him (T10). Hold / returned: the
///   folder goes back, the marked line shows on Bea's sheet, all work kept.
///   A failed save decides nothing and the folder goes straight back.
/// - Approve() never runs the legacy completion route (the chapter guard
///   refuses it); Returned() gives the folder back through the existing path.
/// No trust, ethics or chapter completion.
/// </summary>
public sealed class Level3Audit : MonoBehaviour, AuditorInteractable.ILevelHandIn, AuditorInteractable.ILevelFolder
{
    public const string ChildName = "Audit";
    public const string CueReturned = "BeaReviewReturned";
    public const string CueApproved = "BeaReviewApproved";

    private Level3ReviewDecision decided;

    private void OnEnable() => AuditorInteractable.LevelHandIn = this;

    private void OnDisable()
    {
        if (ReferenceEquals(AuditorInteractable.LevelHandIn, this)) AuditorInteractable.LevelHandIn = null;
    }

    private static CaseDefinition Definition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    /// <summary>The carried Case Folder is in the bar (Level 3 has no stored desk folder).</summary>
    public bool FolderReady
    {
        get
        {
            HudHotbar hotbar = HudHotbar.Instance;
            if (hotbar == null) return true;
            CampaignInventoryRecord inventory = hotbar.Inventory;
            return InventoryRules.Owns(inventory, InventoryCatalogue.CaseFolder) && InventoryRules.InHotbar(inventory, InventoryCatalogue.CaseFolder);
        }
    }

    public string Refusal()
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null || !owner.IsReady || owner.State == null || Definition == null) return Level3ReviewRules.RefuseNotNow;
        return Level3ReviewRules.Refusal(owner.State, Definition.caseId);
    }

    public AuditorInteractable.LevelHandInDecision Decide()
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null || Refusal() != null) return null;
        Level3Checkpointer.ReviewCommit commit = owner.CommitReview(out Level3ReviewDecision decision, out Level3ReviewGrants grants, out string hint);
        if (commit != Level3Checkpointer.ReviewCommit.Decided || decision == null)
        {
            // Nothing was decided or recorded: he gives the folder straight back.
            if (!string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint);
            return null;
        }
        Level3ReviewContent content = Level3Checkpointer.ReviewContent;
        Level3AuditorLines text = content.auditor ?? new Level3AuditorLines();
        Level3ReviewRecord review = owner.State.Case(Level3ChapterRules.BeaCaseId).review;
        Debug.Log($"[Level3] Review: {decision.Outcome}; root '{decision.Root}' ({decision.Kind}); new root {decision.NewRoot}, retry {decision.Retry}; case XP {decision.Xp}.");

        var builder = new DialogueBuilder("Auditor");
        switch (decision.Outcome)
        {
            case Level3ReviewOutcome.Approved:
                foreach (string line in text.approved ?? new List<string>())
                    if (!string.IsNullOrWhiteSpace(line)) builder.Npc(line, "Auditor_Happy");
                builder.Npc("I checked the sheet: taxable income " + Money(review.taxableIncome) + ", tax due " + Money(review.taxDue) +
                    ", less her " + Money(review.credits) + " credit leaves " + Money(review.remainingPayable) + " to pay.", "Auditor_Happy");
                break;
            case Level3ReviewOutcome.SupportedHold:
                builder.Npc(text.hold, "Auditor_Default").Npc(decision.Note, "Auditor_Default");
                break;
            case Level3ReviewOutcome.FalseHold:
                builder.Npc(text.falseHold, "Auditor_Disappointed").Npc(decision.Note, "Auditor_Default");
                break;
            case Level3ReviewOutcome.StaleStamp:
                builder.Npc(text.staleStamp, "Auditor_Default").Npc(decision.Note, "Auditor_Default");
                break;
            default:
                builder.Npc(text.returned, "Auditor_Default").Npc(decision.Note, "Auditor_Default");
                break;
        }
        decided = decision;
        var result = new AuditorInteractable.LevelHandInDecision
        {
            Approved = decision.Outcome == Level3ReviewOutcome.Approved,
            Summary = Summary(decision, review, grants, owner.State)
        };
        foreach (DialogueLine line in builder.Build())
            if (line != null && !string.IsNullOrWhiteSpace(line.Text)) result.Lines.Add(line);
        return result;
    }

    public void Approve()
    {
        // Already saved and paid at Decide; the chapter guard keeps the legacy completion route closed.
        decided = null;
        GameplayEvents.RaiseGuideCue(CueApproved);
    }

    public void Returned()
    {
        decided = null;
        AuditorInteractable auditor = AuditorInteractable.Current;
        if (auditor != null) auditor.ReturnFolderForReview();
        GameplayEvents.RaiseGuideCue(CueReturned);
    }

    private static string Money(string amount) => "₱" + Level3Calculator.Format(amount);

    /// <summary>The Audit Summary card (the same card as Levels 1-2), with Bea's computation.</summary>
    public static RewardScreenData Summary(Level3ReviewDecision decision, Level3ReviewRecord review, Level3ReviewGrants grants, Level3ChapterCheckpoint state)
    {
        bool approved = decision.Outcome == Level3ReviewOutcome.Approved;
        bool hold = decision.Outcome == Level3ReviewOutcome.SupportedHold;
        var data = new RewardScreenData
        {
            Title = "Audit Summary",
            ButtonLabel = approved ? "Continue" : "Back to Bea's sheet",
            Accent = approved ? new Color32(110, 201, 140, 255) : hold ? new Color32(241, 194, 108, 255) : new Color32(226, 112, 96, 255),
            HasStatus = true,
            StatusGood = approved
        };
        if (approved)
        {
            data.Subtitle = "The Auditor reviewed Bea's computation";
            data.StatusText = "APPROVED";
            data.StatusDetail = "Ready for computation review: supported and current";
            data.Fact("Taxable income", Money(review.taxableIncome));
            data.Fact("Tax due", Money(review.taxDue));
            data.Fact("Tax credits", Money(review.credits));
            data.Fact("Remaining payable", Money(review.remainingPayable));
            data.Fact("Returned reviews", review.returned.ToString());
            ReviewClockState clock = state?.Case(Level3ChapterRules.BeaCaseId)?.clock;
            if (clock != null && clock.started) data.Fact("Review time", review.late ? "Day " + clock.day + "  ·  late" : "On time");
            data.Fact("Case XP", review.xp + " / " + Level3ReviewRules.MaxXp);
            if (grants.Coins.Applied) data.Fact("Coins", "+" + grants.Coins.Amount);
            data.Summary = "This is a computation review, not a filing or a payment. Bea's copy is waiting with the Auditor.";
            data.AutoContinueSeconds = 3.5f;
            return data;
        }
        data.StatusText = hold ? "ON HOLD" : "RETURNED";
        data.Subtitle = hold ? "The Auditor acknowledged your hold" : "The Auditor returned Bea's folder";
        data.StatusDetail = hold ? "A line on the sheet is still open" : "Start with the line he marked";
        data.Summary = hold ? "Your hold is right: finish the open line, stamp the current sheet, then hand it in again." : decision.Note;
        if (!string.IsNullOrWhiteSpace(decision.Note)) data.Issues.Add(decision.Note);
        data.Footer = "Your work is kept. Start with the marked line, check what depends on it, stamp the current sheet, then hand the folder back.";
        return data;
    }
}
