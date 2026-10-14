using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// The tutorial NPC that appears only after all 4 taxpayer profiles have
/// been collected. Talking to them explains the corkboard sidequest and
/// then hands control to CorkboardSideQuestManager.
///
/// Place this NPC in the scene already, with its visual root INACTIVE.
/// It reveals itself automatically via TownEvents.OnAllProfilesCollected.
///
/// LIFECYCLE NOTE (T02 Phase 2):
/// Both of this guide's conversations go through explicit DialogueUI
/// admission and are owner-scoped, so a rejected or cancelled start
/// changes nothing here. Two one-shot outcomes are protected:
/// the board only opens after the intro conversation COMPLETES, and the
/// guide stays clickable if CorkboardSideQuestManager refuses to open;
/// and the final result panel runs at most once even if the side-quest
/// completion event were raised again.
/// </summary>
public class TutorialNpcController : MonoBehaviour, IPointerClickHandler, INpcAvailabilitySource
{
    [Header("References")]
    [Tooltip("The NPC's model/visuals. Hidden until all profiles are collected.")]
    [SerializeField] private GameObject visualRoot;

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private CorkboardSideQuestManager corkboardSideQuest;

    private bool hasAppeared;
    private bool hasStartedSideQuest;
    private bool pendingCompletionDialogue;
    private bool hasCompletedSideQuest;
    private bool hasShownCompletionPanel;
    private bool dialogueSessionActive;

    /// <summary>True once all four profiles are collected and this guide is visible.</summary>
    public bool HasAppeared => hasAppeared;

    /// <summary>True while this guide owns the shared conversation.</summary>
    public bool IsInConversation => dialogueSessionActive;

    /// <summary>True once the board has been opened for this guide.</summary>
    public bool HasStartedSideQuest => hasStartedSideQuest;

    /// <summary>True once the final result panel has been shown.</summary>
    public bool HasShownCompletionPanel => hasShownCompletionPanel;

    /// <summary>T04 marker state: intro available after all profiles; done after the result panel.</summary>
    public NpcAvailability Availability =>
        NpcAvailabilityRules.Guide(hasAppeared, hasStartedSideQuest, hasShownCompletionPanel);

    private void Awake()
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        TownEvents.OnAllProfilesCollected += HandleAllProfilesCollected;
        TownEvents.OnSideQuestCompleted += HandleSideQuestCompleted;
    }

    private void OnDisable()
    {
        if (dialogueSessionActive && dialogueUI != null)
        {
            dialogueUI.CancelDialogueFor(this);
        }

        TownEvents.OnAllProfilesCollected -= HandleAllProfilesCollected;
        TownEvents.OnSideQuestCompleted -= HandleSideQuestCompleted;
    }

    private void Update()
    {
        TryStartPendingCompletionDialogue();
    }

    private void HandleSideQuestCompleted()
    {
        // The board raises this once. Keep the final conversation one-shot
        // anyway so a repeated raise cannot replay the result panel.
        if (hasCompletedSideQuest)
        {
            return;
        }

        hasCompletedSideQuest = true;
        pendingCompletionDialogue = true;

        if (DialogueUI.IsAnyDialogueActive)
        {
            return;
        }

        TryStartPendingCompletionDialogue();
    }

    private void TryStartPendingCompletionDialogue()
    {
        if (!pendingCompletionDialogue || dialogueSessionActive ||
            dialogueUI == null || !dialogueUI.CanStartDialogue)
        {
            return;
        }

        var lines = new DialogueBuilder("Tutorial Guide")
            .Npc("Excellent work! Every profile is in its correct spot.")
            .Npc("You've completed your first side quest — and you're already learning how to tell your taxpayers apart!")
            .Npc("That's exactly the kind of instinct you'll need in the office. Let's head back.")
            .Build();

        if (dialogueUI.TryStartDialogue(lines, ShowSideQuestCompletePanel,
            OnCompletionDialogueCancelled, this))
        {
            pendingCompletionDialogue = false;
            dialogueSessionActive = true;
        }
    }

    private void OnCompletionDialogueCancelled()
    {
        dialogueSessionActive = false;

        // The completion event is one-shot. Retain the request across
        // cancellation or disable so the final result/exit panel cannot
        // become unreachable.
        if (!hasShownCompletionPanel)
        {
            pendingCompletionDialogue = true;
        }
    }

    private void ShowSideQuestCompletePanel()
    {
        dialogueSessionActive = false;

        if (hasShownCompletionPanel)
        {
            return;
        }

        hasShownCompletionPanel = true;

        if (SideQuestCompleteUI.Instance != null && corkboardSideQuest != null)
        {
            SideQuestCompleteUI.Instance.Show(corkboardSideQuest.BuildResult());
        }
        else
        {
            Debug.LogWarning(
                "[TutorialNpcController] Side quest complete panel or CorkboardSideQuestManager " +
                "is missing; the result popup was skipped."
            );
        }

        if (CollectedProfilesWorldPanel.Instance != null)
        {
            CollectedProfilesWorldPanel.Instance.LockToSingleSurvivingProfile(); // NEW
        }
    }

    private void HandleAllProfilesCollected()
    {
        if (hasAppeared)
        {
            return;
        }

        hasAppeared = true;

        if (visualRoot != null)
        {
            visualRoot.SetActive(true);
        }

        Debug.Log("[TutorialNpcController] Tutorial NPC has appeared.");

        TownEvents.RaiseTutorialNpcReady();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DialogueUI.IsAnyDialogueActive) return;
        if (eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) || VirtualJoystick.BlocksWorldInput ||
            MobileInputGuard.IsPointerOverUI(eventData.position)) return;
        if (!hasAppeared || hasStartedSideQuest || dialogueSessionActive)
        {
            return;
        }

        if (dialogueUI == null)
        {
            Debug.LogError("[TutorialNpcController] DialogueUI is not assigned.");
            return;
        }

        if (corkboardSideQuest == null)
        {
            Debug.LogError(
                "[TutorialNpcController] CorkboardSideQuestManager reference is missing. " +
                "Refusing to start the intro so the board cannot be skipped."
            );
            return;
        }

        var lines = new DialogueBuilder("Tutorial Guide")
            .Npc("Good work collecting all four taxpayer profiles!")
            .Npc("Before we head back to the office, let's sort them properly.")
            .Npc("I have a small corkboard here. Each corner represents a taxpayer classification.")
            .Player("Alright, what do I do?")
            .Npc("Simple — drag each profile to the corner that matches how that taxpayer earns their income.")
            .Npc("Don't worry about getting it wrong — you can keep trying until every corner is correct.")
            .Build();

        if (!dialogueUI.TryStartDialogue(lines, BeginSideQuest, OnGuideDialogueCancelled, this))
        {
            return;
        }

        dialogueSessionActive = true;
    }

    private void OnGuideDialogueCancelled()
    {
        // The board starts only after successful completion of the guide
        // dialogue, so cancelling simply leaves this guide available.
        dialogueSessionActive = false;
    }

    private void BeginSideQuest()
    {
        dialogueSessionActive = false;

        if (hasStartedSideQuest)
        {
            return;
        }

        if (corkboardSideQuest == null)
        {
            Debug.LogError("[TutorialNpcController] CorkboardSideQuestManager reference is missing.");
            return;
        }

        corkboardSideQuest.BeginSideQuest();

        // BeginSideQuest refuses to open without its camera, viewpoint,
        // slots, prefab, tray and four collected profiles. Only retire this
        // guide once the board is actually running (or already finished),
        // otherwise the side quest would become unreachable.
        if (corkboardSideQuest.CanInteract || hasCompletedSideQuest)
        {
            hasStartedSideQuest = true;
            return;
        }

        Debug.LogWarning(
            "[TutorialNpcController] The corkboard side quest did not open. The guide stays " +
            "available so the player can retry."
        );
    }
}
