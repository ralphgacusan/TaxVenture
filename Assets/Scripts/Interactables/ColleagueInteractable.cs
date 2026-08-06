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
public class ColleagueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueUI dialogueUI;

    private HighlightEffect highlight;
    private bool hasSpoken = false;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
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
        if (hasSpoken) return;
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
        dialogueUI.StartDialogue(lines, OnDialogueFinished);
    }

    private void OnDialogueFinished()
    {
        hasSpoken = true;
    }

    public string GetPromptText()
    {
        return hasSpoken ? "Colleague" : "Click to talk to Colleague";
    }
}