using UnityEngine;

[RequireComponent(typeof(NpcStatusMarker))]
[RequireComponent(typeof(NpcStateMachine))]
public class ReceptionistInteractable : MonoBehaviour, IInteractable, INpcAvailabilitySource
{
    [SerializeField] private DialogueUI dialogueUI;

    private NpcStateMachine npcState;
    private bool dialogueSessionActive;

    // Stores which case this receptionist has already spoken to.
    // This automatically allows interaction again when a new case loads.
    private string spokenForCaseId = null;

    private void Awake()
    {
        npcState = GetComponent<NpcStateMachine>();
    }

    private void OnDisable()
    {
        if (dialogueSessionActive && dialogueUI != null)
        {
            dialogueUI.CancelDialogueFor(this);
        }
    }

    public void OnFocus()
    {

        if (npcState.CurrentState is NpcIdleState)
        {
            npcState.ChangeState(new NpcWaitingState());
        }
    }

    public void OnUnfocus()
    {

        if (npcState.CurrentState is NpcWaitingState)
        {
            npcState.ChangeState(new NpcIdleState());
        }
    }

    public void OnInteract()
    {
        if (dialogueSessionActive) return;

        // Level order: the case starts only after the side quest (rewards.json).
        if (!LevelGate.TryCase()) return;

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

            return;
        }

        // Office Hours: she adds that the Auditor leaves early today.
        if (OfficeHours.Instance != null)
            lines.AddRange(OfficeHours.Instance.AnnouncementLines("Receptionist"));

        INpcState priorNpcState = npcState.CurrentState;
        bool admitted = dialogueUI.TryStartDialogue(
            lines,
            () => OnConversationConcluded(def.caseId),
            () => OnConversationCancelled(priorNpcState),
            this
        );

        if (!admitted) return;

        dialogueSessionActive = true;
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());
    }

    private void OnConversationConcluded(string caseId)
    {
        dialogueSessionActive = false;

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

        // Tutorial hook: no-op if no tutorial is running or no step is
        // waiting on "receptionist". Safe to leave in permanently.
        if (TutorialController.Instance != null)
        {
            TutorialController.Instance.ReportInteraction("receptionist");
        }
    }

    private void OnConversationCancelled(INpcState priorNpcState)
    {
        dialogueSessionActive = false;

        if (npcState == null) return;

        npcState.ChangeState(priorNpcState is NpcCompletedState
            ? priorNpcState
            : new NpcIdleState());
    }

    /// <summary>T04 marker state: greets once per loaded case.</summary>
    public NpcAvailability Availability
    {
        get
        {
            CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
            return NpcAvailabilityRules.Receptionist(
                def != null,
                def != null && def.receptionistGreeting != null && def.receptionistGreeting.Count > 0,
                def != null && spokenForCaseId == def.caseId);
        }
    }

    public string GetPromptText()
    {
        if (!LevelGate.CaseAllowed) return LevelGate.SideQuestFirstMessage;

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
