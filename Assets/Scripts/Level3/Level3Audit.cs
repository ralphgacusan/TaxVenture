using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The Auditor's review of Bea's stamped computation (Level 3 T09), under
/// Level3Gameplay/Audit. The hand-in is the existing talk-first conversation
/// (AuditorInteractable, the same as Levels 1-2): tap the Auditor, he asks
/// for the Case Folder and waits; drag the carried folder from the bar onto
/// him (or tap its icon). This class is its ILevelHandIn, and its
/// ILevelFolder: Level 3's folder is the carried folder (revised T03: the real
/// desk folder in its HUD store, Level3CaseFolder), so "in hand" means it sits
/// in the bar and not on the desk stack; in the
/// Bag his existing "Take it out" prompt appears first.
///
/// - Refusal(): why he can't take it yet (Level3ReviewRules.Refusal: another
///   case, already approved, no stamp, today's window closed, the same sheet
///   and stamp as last time); nothing is decided or charged.
/// - Decide(): ONE chapter command (Level3Checkpointer.CommitReview), saved
///   first. T09 revision: he says ONE short line (OFFICE_CASE_PLAY): the
///   approval, or the earliest open line plus the next action (at most 24
///   words, never a value). The same Audit Summary card as Levels 1-2 follows
///   with the longer reason under "Why" (details on demand).
///   Approval: the approved sheet's own figures, 100 coins and the case XP
///   (written with it); Bea's copy waits with him (T10). Hold / returned: the
///   folder goes back, the marked line shows on the Case Folder's Computation
///   page (Level3ReviewRules.MarkedField), all work kept.
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

    private ClientCopyPickup pickup;
    private bool copyConversation;
    private float retryCopyAt;

    private void OnEnable()
    {
        AuditorInteractable.LevelHandIn = this;
        AuditorInteractable.TalkOverride = TalkCopy;
    }

    private void OnDisable()
    {
        if (ReferenceEquals(AuditorInteractable.LevelHandIn, this)) AuditorInteractable.LevelHandIn = null;
        if (AuditorInteractable.TalkOverride == (System.Func<AuditorInteractable, bool>)TalkCopy) AuditorInteractable.TalkOverride = null;
        if (pickup != null) Destroy(pickup.gameObject);
        pickup = null;
        copyConversation = false;
    }

    private static CaseDefinition Definition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    /// <summary>
    /// The carried Case Folder is in the bar. Revised T03: the bar holds the real
    /// desk folder (Level3CaseFolder), so one the player left on the desk stack
    /// must be taken back first.
    /// </summary>
    public bool FolderReady
    {
        get
        {
            if (Level3CaseFolder.Active != null && Level3CaseFolder.Active.Bound && !Level3CaseFolder.Active.Carried) return false;
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

        // T09 revision (OFFICE_CASE_PLAY): one short line each time. A return or hold names one earliest
        // problem and the next action; the longer reason is on the Audit Summary he hands over next.
        var builder = new DialogueBuilder("Auditor");
        builder.Npc(SpokenLine(decision, text), decision.Outcome == Level3ReviewOutcome.Approved ? "Auditor_Happy" : "Auditor_Default");
        decided = decision;
        var result = new AuditorInteractable.LevelHandInDecision
        {
            Approved = decision.Outcome == Level3ReviewOutcome.Approved,
            Summary = Summary(decision, review, grants, owner.State, text)
        };
        foreach (DialogueLine line in builder.Build())
            if (line != null && !string.IsNullOrWhiteSpace(line.Text)) result.Lines.Add(line);
        return result;
    }

    public void Approve()
    {
        // Already saved and paid at Decide; the chapter guard keeps the legacy completion route closed.
        decided = null;
        // The working folder leaves its icon; the same store receives the copy after deliberate pickup.
        Level2FolderHandoff.FindStore(gameObject.scene)?.ReleaseQuietly();
        GameplayEvents.RaiseGuideCue(CueApproved);
        AuditorInteractable auditor = AuditorInteractable.Current;
        copyConversation = true;
        if (auditor == null || !auditor.TryContinueApproval(CopyLines(), DropCopy))
        {
            copyConversation = false;
            DropCopy();
        }
    }

    public void Returned()
    {
        decided = null;
        AuditorInteractable auditor = AuditorInteractable.Current;
        Level2FolderHandoff.FindStore(gameObject.scene)?.RestoreStored();
        if (auditor != null) auditor.ReturnFolderForReview();
        GameplayEvents.RaiseGuideCue(CueReturned);
    }

    private static bool CopyWaiting => Level3Checkpointer.Active?.IsReady == true &&
        Level3Checkpointer.Active.Phase == Level3Phase.BeaDelivery &&
        Level3Checkpointer.Active.State.Case(Level3ChapterRules.BeaCaseId)?.copyPhase == Level3ReviewRules.CopyAtAuditor;

    private static List<DialogueLine> CopyLines() => new DialogueBuilder("Auditor")
        .Npc("Bea's case is approved. Keep this checked copy for her.", "Auditor_Happy")
        .Npc("Here is her client copy. Bring it to Bea and explain the balance.", "Auditor_Default").Build();

    private void DropCopy()
    {
        copyConversation = false;
        if (!CopyWaiting || pickup != null) return;
        pickup = ClientCopyHandover.SpawnAuditorCopy(gameObject.scene, true, PickedUpCopy, CollectCopy);
        if (pickup != null) AudioManager.Instance?.PlayPaperSFX();
        retryCopyAt = Time.unscaledTime + 1f;
    }

    private void Update()
    {
        // Saved approval interrupted during the summary or drop resumes quietly, with no second reward.
        if (CopyWaiting && pickup == null && !copyConversation && !DialogueUI.IsAnyDialogueActive &&
            !HudRewardScreen.AnyOpen && Time.unscaledTime >= retryCopyAt)
        {
            pickup = ClientCopyHandover.SpawnAuditorCopy(gameObject.scene, false, PickedUpCopy, CollectCopy);
            retryCopyAt = Time.unscaledTime + 1f;
        }
    }

    private bool TalkCopy(AuditorInteractable auditor)
    {
        if (!CopyWaiting) return false;
        if (pickup != null) pickup.TryPickUpFromAuditor();
        else DropCopy();
        return true;
    }

    private RectTransform CollectCopy()
    {
        RectTransform icon = OfficeHudRuntime.Instance?.CaseFolderIcon;
        var owner = Level3Checkpointer.Active;
        if (icon == null || owner == null) return null;
        var result = owner.CommitComputation(Level3ReviewRules.CollectCopy, out string hint);
        if (result != Level3Checkpointer.TapeCommit.Attached && result != Level3Checkpointer.TapeCommit.AlreadyAttached)
        { if (!string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint); return null; }
        return icon;
    }

    private void PickedUpCopy()
    {
        pickup = null;
        Level2FolderHandoff.FindStore(gameObject.scene)?.RestoreStored();
        GameplayEvents.RaiseClientCopyPickedUp();
    }

    private static string Money(string amount) => "₱" + Level3Calculator.Format(amount);

    /// <summary>What he says for a decided hand-in: the approval line, or one problem plus the next action (a hold is thanked first).</summary>
    public static string SpokenLine(Level3ReviewDecision decision, Level3AuditorLines text)
    {
        if (decision == null) return "";
        if (decision.Outcome == Level3ReviewOutcome.Approved)
        {
            string approved = text?.approved?.Find(l => !string.IsNullOrWhiteSpace(l));
            return string.IsNullOrWhiteSpace(approved) ? "Approved for this computation review." : approved;
        }
        return decision.Outcome == Level3ReviewOutcome.SupportedHold ? "Good hold. " + decision.Line : decision.Line;
    }

    /// <summary>The Audit Summary card (the same card as Levels 1-2), with Bea's computation.</summary>
    public static RewardScreenData Summary(Level3ReviewDecision decision, Level3ReviewRecord review, Level3ReviewGrants grants, Level3ChapterCheckpoint state,
        Level3AuditorLines lines = null)
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
            data.Fact("Tax due", Money(review.taxDue));
            data.Fact("Tax credits", Money(review.credits));
            data.Fact("Remaining payable", Money(review.remainingPayable));
            data.Footer = review.xp + " / " + Level3ReviewRules.MaxXp + " XP" + (grants.Coins.Applied ? "  ·  +" + grants.Coins.Amount + " coins" : "");
            data.Summary = "This is a computation review, not a filing or a payment. Bea's copy is waiting with the Auditor.";
            return data;
        }
        data.StatusText = hold ? "ON HOLD" : "RETURNED";
        data.Subtitle = hold ? "The Auditor acknowledged your hold" : "The Auditor returned Bea's folder";
        data.StatusDetail = "Marked: " + Level3ReviewRules.VisibleLabel(decision.Field);
        // The spoken line first; the longer reason behind it ("Why"), then where to start.
        data.Summary = decision.Line;
        data.IssuesTitle = "WHY";
        if (!string.IsNullOrWhiteSpace(decision.Note)) data.Issues.Add(decision.Note);
        lines ??= new Level3AuditorLines();
        data.Footer = decision.Outcome == Level3ReviewOutcome.SupportedHold ? lines.hold
            : decision.Outcome == Level3ReviewOutcome.FalseHold ? lines.falseHold
            : decision.Outcome == Level3ReviewOutcome.StaleStamp ? lines.staleStamp : lines.returned;
        return data;
    }
}
