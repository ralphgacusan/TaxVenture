using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Runs the corkboard matching sidequest: spawns the 4 collected taxpayer
/// profiles as draggable cards on the left, tracks the 4 classification
/// corners on the corkboard on the right, and only ends once every corner
/// has the correct card (retry loop, no time limit, no fail state).
///
/// CONNECTS WITH:
/// - TutorialNpcController.BeginSideQuest() starts this
/// - TaxpayerProfileCollectionHUD.Instance.CollectedProfiles supplies the
///   4 profiles to turn into cards
/// - ProfileDragCard / CorkboardSlot handle the actual drag-and-drop
/// - TownEvents.OnSideQuestCompleted signals the office is unlocked
/// </summary>
public class CorkboardSideQuestManager : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The camera used for drag raycasting. Assign your actual game camera manually here — don't rely on Camera.main unless your camera is tagged MainCamera.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("Parent object holding the corkboard + card tray, placed at a fixed spot in the world (e.g. beside the Tutorial NPC). Hidden until the sidequest starts.")]
    [SerializeField] private GameObject sideQuestRoot;

    [Tooltip("Where the Main Camera should pan to and look from while the side quest is active. Same idea as your desk workstation viewpoint.")]
    [SerializeField] private Transform corkboardViewpoint;

    [Tooltip("Where the 4 cards spawn, on the left side.")]
    [SerializeField] private Transform cardTrayParent;

    [Tooltip("Prefab with a ProfileDragCard component + card visuals.")]
    [SerializeField] private GameObject profileCardPrefab;

    [Tooltip("Vertical gap between stacked cards in the tray. Increase this if cards overlap and become hard to click/drag individually.")]
    [SerializeField] private float cardTraySpacing = 0.5f;

    [Tooltip("The 4 corkboard corners, in any order.")]
    [SerializeField] private List<CorkboardSlot> slots;

    [Header("Reward")]
    [SerializeField] private int rewardExp = 50;
    [SerializeField] private int rewardReputation = 10;

    private readonly List<ProfileDragCard> spawnedCards = new List<ProfileDragCard>();
    private bool sideQuestActive;
    private bool sideQuestCompleted;

    private void Awake()
    {
        if (sideQuestRoot != null)
        {
            sideQuestRoot.SetActive(false);
        }
    }

    public void BeginSideQuest()
    {
        if (sideQuestActive || sideQuestCompleted)
        {
            return;
        }

        sideQuestActive = true;

        if (sideQuestRoot != null)
        {
            sideQuestRoot.SetActive(true);
        }

        // Pan the camera to the corkboard's fixed world position, the same
        // way CameraController pans to a desk workstation viewpoint. Cards
        // are spawned once the camera actually arrives, so nothing appears
        // mid-transition.
        if (CameraController.Instance != null)
        {
            CameraController.Instance.EnterCorkboardSideQuest(corkboardViewpoint, SpawnCards);
        }
        else
        {
            Debug.LogWarning("[CorkboardSideQuestManager] CameraController.Instance is NULL.");
            SpawnCards();
        }

        Debug.Log("[CorkboardSideQuestManager] Side quest started.");
    }

    private void SpawnCards()
    {
        if (TaxpayerProfileCollectionHUD.Instance == null)
        {
            Debug.LogError("[CorkboardSideQuestManager] No collection HUD found to pull profiles from.");
            return;
        }

        if (profileCardPrefab == null || cardTrayParent == null)
        {
            Debug.LogError("[CorkboardSideQuestManager] profileCardPrefab or cardTrayParent is not assigned.");
            return;
        }

        IReadOnlyList<TaxpayerProfile> profiles = TaxpayerProfileCollectionHUD.Instance.CollectedProfiles;

        for (int i = 0; i < profiles.Count; i++)
        {
            GameObject cardObj = Instantiate(profileCardPrefab, cardTrayParent);
            cardObj.transform.localPosition = new Vector3(0f, -i * cardTraySpacing, 0f);

            ProfileDragCard card = cardObj.GetComponent<ProfileDragCard>();

            if (card == null)
            {
                Debug.LogError("[CorkboardSideQuestManager] profileCardPrefab is missing a ProfileDragCard component.");
                continue;
            }

            card.Initialize(profiles[i], this, mainCamera); // sets name + description internally now
            spawnedCards.Add(card);
        }
    }

    /// <summary>
    /// Called by ProfileDragCard when the player releases it over a slot.
    /// Returns true if the placement was correct.
    /// </summary>
    public bool HandleCardDropped(ProfileDragCard card, CorkboardSlot slot)
    {
        bool correct = slot.TryPlaceCard(card);
        CheckForCompletion(); // check every time now, since wrong drops can also change state
        return correct;
    }

    // NEW: called when a card is picked up off a slot
    public void NotifyCardRemovedFromSlot()
    {
        // nothing required here for correctness (you can't be "done" if a card left),
        // but exposed in case you want to e.g. play a sound or reset a "solved" glow elsewhere
    }

    private void CheckForCompletion()
    {
        foreach (CorkboardSlot slot in slots)
        {
            if (!slot.IsSolved)
            {
                return;
            }
        }

        CompleteSideQuest();
    }

    private void CompleteSideQuest()
    {
        if (sideQuestCompleted)
        {
            return;
        }

        sideQuestCompleted = true;
        sideQuestActive = false;

        Debug.Log("[CorkboardSideQuestManager] All corners solved! Side quest complete.");

        GrantRewards();
        Debug.Log("[CorkboardSideQuestManager] Raising OnSideQuestCompleted now.");

        // Give the player a beat to see the fully-solved board before hiding it.
        Invoke(nameof(HideSideQuestRoot), 1.5f);
    }

    private void GrantRewards()
    {
        Debug.Log(
            $"[CorkboardSideQuestManager] Rewarding player: " +
            $"+{rewardExp} EXP, +{rewardReputation} Reputation."
        );

        // Hook this up to your real progression system, e.g.:
        // PlayerProgression.Instance.AddExp(rewardExp);
        // PlayerProgression.Instance.AddReputation(rewardReputation);
    }

    private void HideSideQuestRoot()
    {
        if (sideQuestRoot != null)
        {
            sideQuestRoot.SetActive(false);
        }

        if (CameraController.Instance != null)
        {
            CameraController.Instance.ExitCorkboardSideQuest();
        }

        Debug.Log("[CorkboardSideQuestManager] Raising OnSideQuestCompleted now.");
        TownEvents.RaiseSideQuestCompleted();
    }

    public SideQuestResult BuildResult() => new SideQuestResult(rewardExp, rewardReputation);
}