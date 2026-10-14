using UnityEngine;

/// <summary>
/// PURPOSE:
/// Simple colleague NPC interaction using the shared DialogueUI system.
///
/// CONNECTS WITH:
/// - DialogueUI
/// - NpcStateMachine (optional future expansion)
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class ColleagueInteractable : MonoBehaviour, IInteractable, INpcAvailabilitySource
{
    [SerializeField] private DialogueUI dialogueUI;

    private HighlightEffect highlight;
    private bool hasSpoken = false;
    private bool dialogueSessionActive;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
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
        highlight.Highlight();
    }

    public void OnUnfocus()
    {
        highlight.Unhighlight();
    }

    public void OnInteract()
    {
        if (hasSpoken || dialogueSessionActive || dialogueUI == null) return;

        var lines = new DialogueBuilder("Colleague")
            .Npc(
                "Hey! How's your workload today?",
                "Tutorial_NPC_Default")
            .Player(
                "Pretty busy. I'm reviewing client cases and preparing tax returns.",
                "Auditor_Happy")
            .Npc(
                "Sounds like a lot of work. Make sure you double-check the details.",
                "Tutorial_NPC_Default")
            .Player(
                "Thanks for the reminder. I'll keep that in mind.",
                "Auditor_Happy")
            .Npc(
                "Good luck with your cases!",
                "Tutorial_NPC_Wow")
            .Build();

        if (!dialogueUI.TryStartDialogue(
            lines,
            OnDialogueFinished,
            OnDialogueCancelled,
            this)) return;

        dialogueSessionActive = true;
    }

    private void OnDialogueFinished()
    {
        dialogueSessionActive = false;
        hasSpoken = true;
    }

    private void OnDialogueCancelled()
    {
        dialogueSessionActive = false;
    }

    /// <summary>T04 marker state: one-shot small talk.</summary>
    public NpcAvailability Availability => NpcAvailabilityRules.Colleague(hasSpoken);

    public string GetPromptText()
    {
        return hasSpoken ? "Colleague" : "Click to talk to Colleague";
    }
}
