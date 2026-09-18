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
/// </summary>
[RequireComponent(typeof(NpcStateMachine))]
public class TaxpayerNpcInteractable : MonoBehaviour, IPointerClickHandler
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
        if (hasBeenTalkedTo) return;

        if (stateMachine.CurrentState is NpcIdleState)
        {
            stateMachine.ChangeState(new NpcWaitingState());
        }
    }

    public void OnUnfocus()
    {
        if (hasBeenTalkedTo) return;

        if (stateMachine.CurrentState is NpcWaitingState)
        {
            stateMachine.ChangeState(new NpcIdleState());
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (hasBeenTalkedTo)
        {
            return;
        }

        if (dialogueUI == null)
        {
            Debug.LogError($"[TaxpayerNpcInteractable:{name}] Cannot start dialogue, DialogueUI missing.");
            return;
        }

        stateMachine.ChangeState(new NpcInteractState());
        stateMachine.ChangeState(new NpcDialogueState());

        var lines = TownNpcDialogueLibrary.GetDialogueFor(npcId, npcDisplayName);

        dialogueUI.StartDialogue(lines, OnDialogueFinished);
    }

    private void OnDialogueFinished()
    {
        stateMachine.ChangeState(new NpcCompletedState());
        hasBeenTalkedTo = true;

        DropTaxpayerProfile();
    }

    private void DropTaxpayerProfile()
    {
        if (profilePickup == null)
        {
            Debug.LogWarning($"[TaxpayerNpcInteractable:{name}] No profile pickup assigned, nothing will drop.");
            return;
        }

        TaxpayerProfile data = TaxpayerProfileDatabase.GetByNpcId(npcId);

        if (data == null)
        {
            Debug.LogError($"[TaxpayerNpcInteractable:{name}] No TaxpayerProfile found for npcId '{npcId}'.");
            return;
        }

        profilePickup.Initialize(data);
        profilePickup.gameObject.SetActive(true);

        Debug.Log($"[TaxpayerNpcInteractable:{name}] Dropped profile for '{data.TaxpayerName}'.");
    }
}
