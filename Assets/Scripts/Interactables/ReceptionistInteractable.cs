
using UnityEngine;

[RequireComponent(typeof(HighlightEffect))]
[RequireComponent(typeof(NpcStateMachine))]
public class ReceptionistInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueUI dialogueUI;

    private HighlightEffect highlight;
    private NpcStateMachine npcState;

    // Stores which case this receptionist has already spoken to.
    // This automatically allows interaction again when a new case loads.
    private string spokenForCaseId = null;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
        npcState = GetComponent<NpcStateMachine>();
    }

    public void OnFocus()
    {
        highlight.Highlight();

        if (npcState.CurrentState is NpcIdleState)
        {
            npcState.ChangeState(new NpcWaitingState());
        }
    }

    public void OnUnfocus()
    {
        highlight.Unhighlight();

        if (npcState.CurrentState is NpcWaitingState)
        {
            npcState.ChangeState(new NpcIdleState());
        }
    }

    public void OnInteract()
    {
        Debug.Log("[Receptionist] OnInteract started.");

        // ---------------------------------------------------------
        // Validate required systems
        // ---------------------------------------------------------

        if (npcState == null)
        {
            Debug.LogError("[Receptionist] npcState is NULL.");
            return;
        }

        if (CameraController.Instance == null)
        {
            Debug.LogError("[Receptionist] CameraController.Instance is NULL.");
            return;
        }

        if (CaseManager.Instance == null)
        {
            Debug.LogError("[Receptionist] CaseManager.Instance is NULL.");
            return;
        }

        if (dialogueUI == null)
        {
            Debug.LogError(
                "[Receptionist] dialogueUI is NULL. Assign it in Inspector."
            );
            return;
        }

        CaseDefinition def = CaseManager.Instance.CurrentDefinition;

        if (def == null)
        {
            Debug.LogError(
                "[Receptionist] CurrentDefinition is NULL. " +
                "The case has not been loaded yet."
            );
            return;
        }

        // ---------------------------------------------------------
        // CASE-AWARE INTERACTION CHECK
        // ---------------------------------------------------------
        //
        // If the receptionist already spoke to the player for THIS
        // case, prevent another interaction.
        //
        // If a new case was loaded, def.caseId will be different,
        // so interaction becomes available again automatically.
        // ---------------------------------------------------------

        if (spokenForCaseId == def.caseId)
        {
            Debug.Log(
                $"[Receptionist] Already spoken to for case '{def.caseId}'."
            );
            return;
        }

        Debug.Log($"[Receptionist] Active case: {def.caseId}");
        Debug.Log($"[Receptionist] Client: {def.fullName}");

        if (def.receptionistGreeting == null)
        {
            Debug.LogError(
                $"[Receptionist] receptionistGreeting is NULL " +
                $"in case '{def.caseId}'."
            );
            return;
        }

        Debug.Log(
            $"[Receptionist] Greeting lines: " +
            $"{def.receptionistGreeting.Count}"
        );

        // ---------------------------------------------------------
        // NPC STATE
        // ---------------------------------------------------------

        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        // ---------------------------------------------------------
        // LOCK PLAYER CONTROLS DURING CONVERSATION
        // ---------------------------------------------------------

        CameraController.Instance.LockPlayerControls();

        // ---------------------------------------------------------
        // BUILD DIALOGUE
        // ---------------------------------------------------------

        var lines = CaseFactory.BuildDialogue(
            def.receptionistGreeting,
            "Receptionist"
        );

        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning(
                $"[Receptionist] No receptionist greeting found " +
                $"for case '{def.caseId}'."
            );

            OnConversationConcluded(def.caseId);
            return;
        }

        dialogueUI.StartDialogue(
            lines,
            () => OnConversationConcluded(def.caseId)
        );
    }

    private void OnConversationConcluded(string caseId)
    {
        // Mark receptionist as spoken to for THIS case only.
        spokenForCaseId = caseId;

        Debug.Log(
            $"[Receptionist] Conversation completed for case '{caseId}'."
        );

        npcState.ChangeState(new NpcCompletedState());

        GameStateMachine.Instance.ChangeState(
            new ReceiveCaseState()
        );

        GameplayEvents.RaiseNotesUnlockRequested();
    }

    public string GetPromptText()
    {
        // Determine whether the receptionist has already been
        // spoken to for the CURRENT case.
        if (CaseManager.Instance == null ||
            CaseManager.Instance.CurrentDefinition == null)
        {
            return "Receptionist";
        }

        string currentCaseId =
            CaseManager.Instance.CurrentDefinition.caseId;

        if (spokenForCaseId == currentCaseId)
        {
            return "Receptionist";
        }

        return "Click to talk to Receptionist";
    }
}
