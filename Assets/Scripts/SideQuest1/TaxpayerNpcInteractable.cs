using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// Attach to each of the 4 town taxpayer NPCs. Drives this NPC's own
/// NpcStateMachine (Idle -> Waiting -> Interact -> Dialogue -> Completed),
/// starts hardcoded dialogue via DialogueUI, and drops a physical
/// TaxpayerProfileWorldPickup beside the NPC once the conversation ends.
///
/// REQUIRES:
/// - NpcStateMachine on the same GameObject (already in your project)
/// - A Collider on this GameObject/child + a PhysicsRaycaster on the
///   camera, same setup your other clickable 3D objects (e.g. the case
///   folder) already use.
///
/// CONNECTS WITH:
/// - DialogueUI (assign the same shared DialogueUI used everywhere else)
/// - TownNpcDialogueLibrary (hardcoded lines, keyed by npcId)
/// - TaxpayerProfileWorldPickup (the paper that appears after talking)
/// - ArthurOpening (T30): the plaza order (WaitingForRound / WaitingTapped):
///   all four clients wait until Arthur's reviewed file was looked over.
///   ProfileHandover / ReleaseHeldProfile (T24) stay as a generic hook with no
///   owner since T30: every talk drops its profile directly.
///
/// LIFECYCLE NOTE (T02 Phase 2):
/// Admission through DialogueUI.TryStartDialogue happens BEFORE any NPC
/// state change, so a rejected start leaves this NPC exactly as it was
/// and emits no state-change events. This NPC's completion is one-shot
/// and gates the four-profile collection, so the conversation is refused
/// outright when its npcId, profile data or pickup cannot satisfy that
/// outcome - completing on broken configuration would retire the NPC
/// without ever dropping its profile and strand the side quest.
/// </summary>
[RequireComponent(typeof(NpcStateMachine))]
public class TaxpayerNpcInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, INpcAvailabilitySource
{
    [Header("Identity")]
    [Tooltip("Must match a case in TownNpcDialogueLibrary and a NpcId in TaxpayerProfileDatabase.")]
    [SerializeField] private string npcId;

    [SerializeField] private string npcDisplayName = "Taxpayer";

    [Header("References")]
    [SerializeField] private DialogueUI dialogueUI;

    [Tooltip("The 3D paper object that appears once dialogue ends. Leave it placed beside the NPC in the scene, inactive by default.")]
    [SerializeField] private TaxpayerProfileWorldPickup profilePickup;

    private NpcStateMachine stateMachine;
    private NpcLife npcLife;
    private HighlightEffect pointerHighlight;
    private bool hasBeenTalkedTo;
    private bool dialogueSessionActive;
    private bool profileHeld;

    /// <summary>True once this NPC's conversation completed (its profile dropped, or is held for the records step, T24).</summary>
    public bool HasBeenTalkedTo => hasBeenTalkedTo;

    /// <summary>Stable id (TownNpcDialogueLibrary / TaxpayerProfileDatabase).</summary>
    public string NpcId => npcId;

    /// <summary>The name shown on this NPC's dialogue lines.</summary>
    public string DisplayName => npcDisplayName;

    /// <summary>T24 hook: the talk is done but the profile waits for its owner (none since T30).</summary>
    public bool IsProfileHeld => profileHeld;

    /// <summary>True while this client still has an uncollected profile handover.</summary>
    public bool HasPendingHandover => profileHeld ||
        (hasBeenTalkedTo && profilePickup != null && !profilePickup.IsCollected);

    /// <summary>
    /// The plaza rounds' order (T24; ArthurOpening since T30). True = this client is
    /// not ready for a first talk yet: no marker, no highlight, and a tap goes
    /// to WaitingTapped instead of starting the talk. Cleared with its owner.
    /// </summary>
    public static System.Func<TaxpayerNpcInteractable, bool> WaitingForRound;

    /// <summary>A tap on a waiting client (a short line the first time, nothing after).</summary>
    public static System.Action<TaxpayerNpcInteractable> WaitingTapped;

    /// <summary>
    /// T24: asked when a first talk completes, before the profile drops. True =
    /// the owner takes the handover and drops the profile later through
    /// ReleaseHeldProfile. Cleared with its owner. No owner since T30.
    /// </summary>
    public static System.Func<TaxpayerNpcInteractable, bool> ProfileHandover;

    private bool WaitingForTurn => !hasBeenTalkedTo && WaitingForRound != null && WaitingForRound(this);

    /// <summary>Not this client's turn yet (the plaza rounds have not opened). Read-only, for presentation.</summary>
    public bool IsWaitingForRound => WaitingForTurn;

    /// <summary>
    /// Resume (T04): this NPC's conversation already happened in the saved run.
    /// Retires the NPC without the dialogue, the drop animation or the score
    /// award; the profile paper lies beside the NPC again unless it was
    /// already collected, or (T24) is still held for the records step. Raises no events.
    /// </summary>
    public void RestoreHandover(bool profileCollected, bool holdProfile = false)
    {
        if (hasBeenTalkedTo) return;
        hasBeenTalkedTo = true;
        if (stateMachine == null) stateMachine = GetComponent<NpcStateMachine>();
        if (stateMachine != null) stateMachine.ChangeState(new NpcCompletedState());
        if (profilePickup == null) return;
        TaxpayerProfile data = TaxpayerProfileDatabase.GetByNpcId(npcId);
        if (data == null) return;
        // The paper always knows its profile (the profile viewer and the n/1
        // counter read Marie's paper after the side quest); it only lies in the
        // world while it still has to be collected.
        profilePickup.Initialize(data, profileCollected);
        profileHeld = holdProfile && !profileCollected;
        profilePickup.gameObject.SetActive(!profileCollected && !profileHeld);
    }

    /// <summary>True while this NPC owns the shared conversation.</summary>
    public bool IsInConversation => dialogueSessionActive;

    /// <summary>
    /// T08: a visit owner (Marie's Client Copy handover) may take a tap on a
    /// taxpayer whose plaza talk is over. It returns true when it handled the
    /// tap (usually by calling TryStartVisit). Cleared with its owner.
    /// </summary>
    public static System.Func<TaxpayerNpcInteractable, bool> VisitHandler;

    /// <summary>The visit owner's marker state for this taxpayer (null = the plaza state).</summary>
    public static System.Func<TaxpayerNpcInteractable, NpcAvailability?> VisitAvailability;

    private bool VisitOpen => hasBeenTalkedTo && VisitAvailability != null && VisitAvailability(this) == NpcAvailability.Available;

    private bool CanOfferFollowUpSmallTalk => hasBeenTalkedTo && !HasPendingHandover &&
        npcLife != null && npcLife.TryGetFollowUpSmallTalkLine(out _);

    /// <summary>
    /// T08: a visit conversation after the plaza talk (the shared dialogue
    /// flow, this NPC as its owner, the usual talk / listen states). It never
    /// changes the plaza talk, the profile or the level score.
    /// </summary>
    public bool TryStartVisit(List<DialogueLine> lines, System.Action completed, System.Action cancelled)
    {
        if (dialogueSessionActive || dialogueUI == null || stateMachine == null || lines == null || lines.Count == 0) return false;
        INpcState prior = stateMachine.CurrentState;
        if (!dialogueUI.TryStartDialogue(lines,
                () => { dialogueSessionActive = false; stateMachine.ChangeState(prior is NpcCompletedState ? prior : new NpcCompletedState()); completed?.Invoke(); },
                () => { dialogueSessionActive = false; stateMachine.ChangeState(prior is NpcCompletedState ? prior : new NpcCompletedState()); cancelled?.Invoke(); },
                this))
            return false;
        AudioManager.Instance?.QueueInteractionSFX("npc");
        dialogueSessionActive = true;
        stateMachine.ChangeState(new NpcInteractState());
        stateMachine.ChangeState(new NpcDialogueState());
        ClearPointerFocus();
        return true;
    }

    private bool TryStartFollowUpSmallTalk()
    {
        if (!hasBeenTalkedTo || HasPendingHandover || npcLife == null ||
            !npcLife.TryGetFollowUpSmallTalkLine(out string line)) return false;
        var lines = new List<DialogueLine>
        {
            new DialogueLine(DialogueSpeaker.Npc, line, "default", npcDisplayName)
        };
        return TryStartVisit(lines, null, null);
    }

    /// <summary>
    /// T24: the held profile drops beside the NPC now (the records step is
    /// done). False when nothing is held or the drop cannot happen (it stays held).
    /// </summary>
    public bool ReleaseHeldProfile()
    {
        if (!hasBeenTalkedTo || !profileHeld) return false;
        if (!TryDropTaxpayerProfile()) return false;
        profileHeld = false;
        return true;
    }

    /// <summary>
    /// T24: a short aside (a waiting client's line) through the shared dialogue
    /// flow. It never changes the plaza talk, the profile or the level score;
    /// the NPC goes back to how it was.
    /// </summary>
    public bool TryStartAside(List<DialogueLine> lines)
    {
        if (hasBeenTalkedTo || dialogueSessionActive || dialogueUI == null || stateMachine == null || lines == null || lines.Count == 0) return false;
        INpcState prior = stateMachine.CurrentState;
        if (!dialogueUI.TryStartDialogue(lines,
                () => { dialogueSessionActive = false; RestoreNpcState(prior); },
                () => { dialogueSessionActive = false; RestoreNpcState(prior); },
                this))
            return false;
        AudioManager.Instance?.QueueInteractionSFX("npc");
        dialogueSessionActive = true;
        stateMachine.ChangeState(new NpcInteractState());
        stateMachine.ChangeState(new NpcDialogueState());
        ClearPointerFocus();
        return true;
    }

    private int dataValidity; // 0 unknown, 1 valid, -1 invalid (static data, checked once)

    /// <summary>T04 marker state: one conversation that drops this NPC's profile.</summary>
    public NpcAvailability Availability
    {
        get
        {
            NpcAvailability? visit = hasBeenTalkedTo && VisitAvailability != null ? VisitAvailability(this) : null;
            if (visit.HasValue) return visit.Value;
            if (WaitingForTurn) return NpcAvailability.Unavailable;
            if (dataValidity == 0)
                dataValidity = TaxpayerProfileDatabase.GetByNpcId(npcId) != null &&
                               TownNpcDialogueLibrary.HasDialogueFor(npcId) ? 1 : -1;
            return NpcAvailabilityRules.Taxpayer(dataValidity > 0 && profilePickup != null, hasBeenTalkedTo);
        }
    }

    private void Awake()
    {
        stateMachine = GetComponent<NpcStateMachine>();
        npcLife = GetComponent<NpcLife>();
        pointerHighlight = HighlightEffect.For(this);

        if (dialogueUI == null)
        {
            Debug.LogError($"[TaxpayerNpcInteractable:{name}] DialogueUI is not assigned.");
        }

        if (profilePickup != null)
        {
            profilePickup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Call from whatever focus/highlight system you use when the player
    /// looks at or approaches this NPC (same idea as your other
    /// *Interactable scripts' OnFocus()).
    /// </summary>
    public void OnFocus()
    {
        if (hasBeenTalkedTo || dialogueSessionActive || WaitingForTurn) return;

        if (stateMachine.CurrentState is NpcIdleState)
        {
            stateMachine.ChangeState(new NpcWaitingState());
        }
    }

    public void OnUnfocus()
    {
        if (hasBeenTalkedTo || dialogueSessionActive) return;

        if (stateMachine.CurrentState is NpcWaitingState)
        {
            stateMachine.ChangeState(new NpcIdleState());
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        bool focused = CanFocusPointer(eventData);
        if (pointerHighlight != null) pointerHighlight.SetFocused(focused);
        if (focused) OnFocus();
        else OnUnfocus();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearPointerFocus();
        OnUnfocus();
    }

    private bool CanFocusPointer(PointerEventData eventData)
    {
        return eventData != null && (!hasBeenTalkedTo || VisitOpen || CanOfferFollowUpSmallTalk) && !dialogueSessionActive && !WaitingForTurn &&
               InteractionRange.PlayerCanReach(this) &&
               !DialogueUI.IsAnyDialogueActive && !VirtualJoystick.BlocksWorldInput &&
               !MobileInputGuard.IsPointerOverUI(eventData.position);
    }

    private void ClearPointerFocus()
    {
        if (pointerHighlight != null) pointerHighlight.SetFocused(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DialogueUI.IsAnyDialogueActive) return;
        if (eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) || VirtualJoystick.BlocksWorldInput ||
            MobileInputGuard.IsPointerOverUI(eventData.position)) return;
        if (dialogueSessionActive) return;

        // Only within reach (taps through the EventSystem have no distance limit).
        if (!InteractionRange.PlayerCanReach(this)) return;

        // T08: after the plaza talk, a visit owner may take the tap (Marie's handover).
        if (hasBeenTalkedTo)
        {
            if (VisitHandler != null && VisitHandler(this)) return;
            TryStartFollowUpSmallTalk();
            return;
        }

        // T24: not this client's turn yet; the talk (and its profile) waits.
        if (WaitingForTurn)
        {
            WaitingTapped?.Invoke(this);
            return;
        }

        if (dialogueUI == null)
        {
            Debug.LogError($"[TaxpayerNpcInteractable:{name}] Cannot start dialogue, DialogueUI missing.");
            return;
        }

        if (stateMachine == null)
        {
            Debug.LogError($"[TaxpayerNpcInteractable:{name}] NpcStateMachine is missing.");
            return;
        }

        // The conversation's only successful outcome is this NPC's profile
        // drop, so refuse to spend it when that outcome cannot be produced.
        if (!CanDeliverProfile())
        {
            return;
        }

        var lines = TownNpcDialogueLibrary.GetDialogueFor(npcId, npcDisplayName, out bool recognized);

        if (!recognized)
        {
            Debug.LogError(
                $"[TaxpayerNpcInteractable:{name}] TownNpcDialogueLibrary has no dialogue for " +
                $"npcId '{npcId}'. Refusing to start so this NPC is not retired on placeholder content."
            );
            return;
        }

        INpcState priorNpcState = stateMachine.CurrentState;

        if (!dialogueUI.TryStartDialogue(
            lines,
            () => OnDialogueFinished(priorNpcState),
            () => OnDialogueCancelled(priorNpcState),
            this))
        {
            return;
        }

        AudioManager.Instance?.QueueInteractionSFX("npc");
        dialogueSessionActive = true;
        stateMachine.ChangeState(new NpcInteractState());
        stateMachine.ChangeState(new NpcDialogueState());
        ClearPointerFocus();
    }

    private void OnDisable()
    {
        ClearPointerFocus();
        if (dialogueSessionActive && dialogueUI != null)
        {
            dialogueUI.CancelDialogueFor(this);
        }

        OnUnfocus();
    }

    private void OnDialogueFinished(INpcState priorNpcState)
    {
        dialogueSessionActive = false;

        if (hasBeenTalkedTo) return;

        // T24: the round owner may take the handover (records first); the
        // paper knows its profile but stays out of the world until released.
        if (ProfileHandover != null && CanDeliverProfile() && ProfileHandover(this))
        {
            profilePickup.Initialize(TaxpayerProfileDatabase.GetByNpcId(npcId));
            profileHeld = true;
        }
        // Drop first: the profile is this conversation's outcome. If the
        // drop cannot happen the NPC stays available instead of silently
        // retiring with no paper for the player to collect.
        else if (!TryDropTaxpayerProfile())
        {
            RestoreNpcState(priorNpcState);
            return;
        }

        hasBeenTalkedTo = true;

        if (stateMachine != null)
        {
            stateMachine.ChangeState(new NpcCompletedState());
        }

        // Conversation bonus (+hearts, once per client) for the level score;
        // the HUD floats the heart from this NPC to the hearts bar.
        if (CaseProgressionManager.Instance != null)
        {
            TaxpayerProfile talked = TaxpayerProfileDatabase.GetByNpcId(npcId);
            CaseProgressionManager.Instance.AwardConversation(npcId,
                talked != null ? talked.TaxpayerName : name,
                transform.position + Vector3.up * 1.6f);
        }
    }

    private void OnDialogueCancelled(INpcState priorNpcState)
    {
        dialogueSessionActive = false;
        RestoreNpcState(priorNpcState);
    }

    private void RestoreNpcState(INpcState priorNpcState)
    {
        if (stateMachine == null || hasBeenTalkedTo)
        {
            return;
        }

        if (priorNpcState is NpcCompletedState)
        {
            stateMachine.ChangeState(priorNpcState);
            return;
        }

        stateMachine.ChangeState(priorNpcState is NpcWaitingState
            ? (INpcState)new NpcWaitingState()
            : new NpcIdleState());
    }

    /// <summary>
    /// Checks everything the successful outcome needs, without changing
    /// any state, so a misconfigured NPC refuses the conversation instead
    /// of completing it and stranding the four-profile collection.
    /// </summary>
    private bool CanDeliverProfile()
    {
        if (profilePickup == null)
        {
            Debug.LogError(
                $"[TaxpayerNpcInteractable:{name}] No profile pickup assigned. Refusing to start " +
                $"so this NPC is not marked complete without dropping its profile."
            );
            return false;
        }

        if (TaxpayerProfileDatabase.GetByNpcId(npcId) == null)
        {
            Debug.LogError(
                $"[TaxpayerNpcInteractable:{name}] No TaxpayerProfile found for npcId '{npcId}'. " +
                $"Refusing to start so this NPC is not marked complete without dropping its profile."
            );
            return false;
        }

        return true;
    }

    private bool TryDropTaxpayerProfile()
    {
        if (profilePickup == null)
        {
            Debug.LogError($"[TaxpayerNpcInteractable:{name}] Profile pickup went missing during the conversation.");
            return false;
        }

        TaxpayerProfile data = TaxpayerProfileDatabase.GetByNpcId(npcId);

        if (data == null)
        {
            Debug.LogError($"[TaxpayerNpcInteractable:{name}] No TaxpayerProfile found for npcId '{npcId}'.");
            return false;
        }

        profilePickup.Initialize(data);
        profilePickup.DropFrom(transform.position + Vector3.up * 1f);
        profilePickup.gameObject.SetActive(true);

        Debug.Log($"[TaxpayerNpcInteractable:{name}] Dropped profile for '{data.TaxpayerName}'.");
        return true;
    }
}
