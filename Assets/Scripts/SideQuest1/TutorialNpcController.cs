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
public class TutorialNpcController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, INpcAvailabilitySource
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
    private HighlightEffect pointerHighlight;

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
        pointerHighlight = HighlightEffect.For(this);

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
        ClearPointerFocus();

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

        // Arthur's wrap-up (CorkboardTeaching; editable in town.json).
        var lines = CorkboardTeaching.BuildDone("Arthur");

        if (dialogueUI.TryStartDialogue(lines, ShowSideQuestCompletePanel,
            OnCompletionDialogueCancelled, this))
        {
            pendingCompletionDialogue = false;
            dialogueSessionActive = true;
            ClearPointerFocus();
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

        // Level score: the guide hands over the side-quest reward (EXP, hearts
        // and badge, from rewards.json); the hearts float from the guide.
        ScoreEntry award = null;
        if (CaseProgressionManager.Instance != null && corkboardSideQuest != null)
        {
            award = CaseProgressionManager.Instance.Score.AddSideQuest(
                corkboardSideQuest.WrongPlacements, transform.position + Vector3.up * 1.6f);
        }

        if (SideQuestCompleteUI.Instance != null && corkboardSideQuest != null)
        {
            SideQuestCompleteUI.Instance.Show(corkboardSideQuest.BuildResult(award));
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

        // Only within reach (taps through the EventSystem have no distance limit).
        if (!InteractionRange.PlayerCanReach(this)) return;

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

        // Arthur explains each corner indirectly, one line per kind of
        // taxpayer, without naming any (CorkboardTeaching; town.json).
        var lines = CorkboardTeaching.BuildIntro("Arthur");

        if (!dialogueUI.TryStartDialogue(lines, BeginSideQuest, OnGuideDialogueCancelled, this))
        {
            return;
        }

        dialogueSessionActive = true;
        ClearPointerFocus();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pointerHighlight != null) pointerHighlight.SetFocused(CanFocusPointer(eventData));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearPointerFocus();
    }

    private bool CanFocusPointer(PointerEventData eventData)
    {
        return eventData != null && hasAppeared && !hasStartedSideQuest && !dialogueSessionActive &&
               InteractionRange.PlayerCanReach(this) &&
               !DialogueUI.IsAnyDialogueActive && !VirtualJoystick.BlocksWorldInput &&
               !MobileInputGuard.IsPointerOverUI(eventData.position);
    }

    private void ClearPointerFocus()
    {
        if (pointerHighlight != null) pointerHighlight.SetFocused(false);
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
            TownEvents.RaiseTutorialIntroCompleted();
            return;
        }

        Debug.LogWarning(
            "[TutorialNpcController] The corkboard side quest did not open. The guide stays " +
            "available so the player can retry."
        );
    }
}
