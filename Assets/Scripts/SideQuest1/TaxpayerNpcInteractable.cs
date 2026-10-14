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
public class TaxpayerNpcInteractable : MonoBehaviour, IPointerClickHandler, INpcAvailabilitySource
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
    private bool hasBeenTalkedTo;
    private bool dialogueSessionActive;

    /// <summary>True once this NPC's conversation completed and its profile dropped.</summary>
    public bool HasBeenTalkedTo => hasBeenTalkedTo;

    /// <summary>True while this NPC owns the shared conversation.</summary>
    public bool IsInConversation => dialogueSessionActive;

    private int dataValidity; // 0 unknown, 1 valid, -1 invalid (static data, checked once)

    /// <summary>T04 marker state: one conversation that drops this NPC's profile.</summary>
    public NpcAvailability Availability
    {
        get
        {
            if (dataValidity == 0)
                dataValidity = TaxpayerProfileDatabase.GetByNpcId(npcId) != null &&
                               TownNpcDialogueLibrary.HasDialogueFor(npcId) ? 1 : -1;
            return NpcAvailabilityRules.Taxpayer(dataValidity > 0 && profilePickup != null, hasBeenTalkedTo);
        }
    }

    private void Awake()
    {
        stateMachine = GetComponent<NpcStateMachine>();

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
        if (hasBeenTalkedTo || dialogueSessionActive) return;

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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DialogueUI.IsAnyDialogueActive) return;
        if (eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) || VirtualJoystick.BlocksWorldInput ||
            MobileInputGuard.IsPointerOverUI(eventData.position)) return;
        if (hasBeenTalkedTo || dialogueSessionActive)
        {
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

        dialogueSessionActive = true;
        stateMachine.ChangeState(new NpcInteractState());
        stateMachine.ChangeState(new NpcDialogueState());
    }

    private void OnDisable()
    {
        if (dialogueSessionActive && dialogueUI != null)
        {
            dialogueUI.CancelDialogueFor(this);
        }
    }

    private void OnDialogueFinished(INpcState priorNpcState)
    {
        dialogueSessionActive = false;

        if (hasBeenTalkedTo) return;

        // Drop first: the profile is this conversation's outcome. If the
        // drop cannot happen the NPC stays available instead of silently
        // retiring with no paper for the player to collect.
        if (!TryDropTaxpayerProfile())
        {
            RestoreNpcState(priorNpcState);
            return;
        }

        hasBeenTalkedTo = true;

        if (stateMachine != null)
        {
            stateMachine.ChangeState(new NpcCompletedState());
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
