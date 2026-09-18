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
/// </summary>
public class TutorialNpcController : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [Tooltip("The NPC's model/visuals. Hidden until all profiles are collected.")]
    [SerializeField] private GameObject visualRoot;

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private CorkboardSideQuestManager corkboardSideQuest;

    private bool hasAppeared;
    private bool hasStartedSideQuest;

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
        TownEvents.OnAllProfilesCollected -= HandleAllProfilesCollected;
        TownEvents.OnSideQuestCompleted -= HandleSideQuestCompleted;
    }

    private void HandleSideQuestCompleted()
    {
        if (dialogueUI == null) return;

        var lines = new DialogueBuilder("Tutorial Guide")
            .Npc("Excellent work! Every profile is in its correct spot.")
            .Npc("You've completed your first side quest — and you're already learning how to tell your taxpayers apart!")
            .Npc("That's exactly the kind of instinct you'll need in the office. Let's head back.")
            .Build();

        dialogueUI.StartDialogue(lines, ShowSideQuestCompletePanel);
    }

    private void ShowSideQuestCompletePanel()
    {
        if (SideQuestCompleteUI.Instance != null && corkboardSideQuest != null)
        {
            SideQuestCompleteUI.Instance.Show(corkboardSideQuest.BuildResult());
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
        if (!hasAppeared || hasStartedSideQuest)
        {
            return;
        }

        if (dialogueUI == null)
        {
            Debug.LogError("[TutorialNpcController] DialogueUI is not assigned.");
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

        dialogueUI.StartDialogue(lines, BeginSideQuest);
    }

    private void BeginSideQuest()
    {
        hasStartedSideQuest = true;

        if (corkboardSideQuest != null)
        {
            corkboardSideQuest.BeginSideQuest();
        }
        else
        {
            Debug.LogError("[TutorialNpcController] CorkboardSideQuestManager reference is missing.");
        }
    }
}
