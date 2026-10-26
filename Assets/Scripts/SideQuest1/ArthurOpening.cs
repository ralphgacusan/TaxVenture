using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Level 1's opening route (Campaign Foundation T30, replacing T24's
/// FirstClientRound): Arthur appears at ArthurStartPlacement after the stall.
/// Once the opening lessons (controls, Notes, Supply Counter) are behind the
/// player, a deliberate tap on him starts his short talk; only a completed
/// talk presents his Reviewed File beside him (ReviewedFilePickup, the
/// Auditor's Client Copy motion). Collecting it commits the item and its
/// handover in one save, then it flies into the Bag. Continue keeps it; a fresh
/// attempt recollects it after the inventory reset. Reading it
/// (ReviewedFileViewer): both records shown settled and put back, then the
/// completed first page seen again, opens the rounds; Arthur adds at most four
/// short lines on the way. Until then the four plaza clients wait (no marker;
/// a first tap on each gets their one waiting line, T24's). Afterwards they
/// talk in any order and give only their profiles.
///
/// OWNERSHIP: the item belongs to the campaign inventory (HudHotbar command);
/// this component holds the route milestones, which Level1FieldCheckpointer
/// saves (Capture / Restore). Guide pages and animations never grant or
/// complete anything. No coins, trust, XP or score here.
///
/// LIFETIME: added at runtime to Office_Level1 under the "Campaign
/// Foundation" root. Tutorial Hub practice keeps the old free order (Arthur
/// appears after four profiles; the two opening steps are ticked at once).
/// </summary>
public sealed class ArthurOpening : MonoBehaviour
{
    public const string ObjectName = "Level 1 Arthur Opening";
    public const string CuePresented = "ReviewedFilePresented";
    public const string CueCollected = "ReviewedFileCollected";
    public const string CueOpened = "ReviewedFileOpened";
    public const string CueRecordRead = "ReviewedRecordRead";

    public static ArthurOpening Active { get; private set; }

    /// <summary>A saved milestone changed (talk done, record shown / put back, page revisited, rounds open).</summary>
    public static event Action Changed;

    private readonly List<string> presented = new List<string>();
    private readonly List<string> returned = new List<string>();
    private TutorialNpcController guide;
    private ReviewedFilePickup pickup;
    private readonly HashSet<string> waitingSaid = new HashSet<string>();
    private bool practice, introDone, revisited, roundsOpen, metPending, ready;
    private bool pickupSpawnPending, pickupSpawnAnimated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || OfficeLevelProfile.LevelIdFor(scene) != OfficeLevelCatalog.Level1Id) return;
        // Arthur stands at his marker before any Start runs (one actor, never a visible move).
        ArthurStartPlacement placement = ArthurStartPlacement.Find(scene);
        TutorialNpcController found = null;
        foreach (TutorialNpcController candidate in FindObjectsByType<TutorialNpcController>(FindObjectsInactive.Include))
            if (candidate.gameObject.scene == scene) { found = candidate; break; }
        if (placement != null && found != null) placement.Apply(found.transform);

        GameObject root = null;
        foreach (GameObject candidate in scene.GetRootGameObjects())
            if (candidate.name == MainMenuCampaignPanel.RootName) { root = candidate; break; }
        if (root == null)
        {
            root = new GameObject(MainMenuCampaignPanel.RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        if (root.GetComponentInChildren<ArthurOpening>(true) != null) return;
        var host = new GameObject(ObjectName);
        host.transform.SetParent(root.transform, false);
        host.AddComponent<ArthurOpening>();
    }

    private void OnEnable()
    {
        Active = this;
        practice = TrainingSession.IsPracticing;
        if (practice) return;
        GuideFlags.Source = ReadingFlag;
        TaxpayerNpcInteractable.WaitingForRound = IsWaiting;
        TaxpayerNpcInteractable.WaitingTapped = HandleWaitingTap;
        TutorialNpcController.OpeningAvailability = OpeningAvailability;
        TutorialNpcController.OpeningTapped = HandleOpeningTap;
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        if (GuideFlags.Source == (Func<string, bool>)ReadingFlag) GuideFlags.Source = null;
        if (TaxpayerNpcInteractable.WaitingForRound == (Func<TaxpayerNpcInteractable, bool>)IsWaiting) TaxpayerNpcInteractable.WaitingForRound = null;
        if (TaxpayerNpcInteractable.WaitingTapped == (Action<TaxpayerNpcInteractable>)HandleWaitingTap) TaxpayerNpcInteractable.WaitingTapped = null;
        if (TutorialNpcController.OpeningAvailability == (Func<TutorialNpcController, NpcAvailability?>)OpeningAvailability) TutorialNpcController.OpeningAvailability = null;
        if (TutorialNpcController.OpeningTapped == (Func<TutorialNpcController, bool>)HandleOpeningTap) TutorialNpcController.OpeningTapped = null;
    }

    private void OnDestroy()
    {
        if (pickup != null) Destroy(pickup.gameObject);
        if (ReviewedFileViewer.Instance != null && ReviewedFileViewer.Instance.gameObject.scene == gameObject.scene)
            Destroy(ReviewedFileViewer.Instance.gameObject);
    }

    private IEnumerator Start()
    {
        foreach (TutorialNpcController candidate in FindObjectsByType<TutorialNpcController>(FindObjectsInactive.Include))
            if (candidate.gameObject.scene == gameObject.scene) { guide = candidate; break; }
        // Reading works in every run (the item may be owned); only the route is story-only.
        ReviewedFileViewer.Create(gameObject.scene, this);

        float until = Time.unscaledTime + 8f;
        while (Time.unscaledTime < until && (OfficeObjectives.Instance == null ||
               (HudHotbar.Instance != null && !HudHotbar.Instance.IsReady) ||
               (Level1FieldCheckpointer.Active != null && !Level1FieldCheckpointer.Active.IsReady)))
            yield return null;
        ready = true;
        if (practice)
        {
            MarkStepsBehind(ReviewedFileRules.Stage.Open);
            yield break;
        }
        // Only trust ownership after the hotbar reset/restore has finished. Continue keeps its file; a restart recollects it.
        if (FileOwned()) introDone = true;
        else if (introDone || pickupSpawnPending) SpawnPickup(pickupSpawnAnimated);
        MarkStepsBehind(Stage);
    }

    private void LateUpdate()
    {
        if (pickupSpawnPending && ready)
        {
            ResolveGuide();
            if (guide != null) SpawnPickup(pickupSpawnAnimated);
        }
        if (!practice && guide != null && !guide.IsShownInPlaza && OpeningLessonsDone()) guide.ShowInPlaza();
        if (!metPending) return;
        metPending = false;
        TownEvents.RaiseArthurMet();
        Changed?.Invoke();
    }

    private ReviewedFileRules.Stage Stage =>
        ReviewedFileRules.StageOf(introDone, FileOwned(), ReviewedFileRules.LookedOver(returned, revisited), roundsOpen);

    private static bool FileOwned()
    {
        HudHotbar hotbar = HudHotbar.Instance;
        return hotbar != null && InventoryRules.Owns(hotbar.Inventory, ReviewedFileRules.ItemId);
    }

    private static void MarkStepsBehind(ReviewedFileRules.Stage stage)
    {
        if (OfficeObjectives.Instance == null) return;
        foreach (string key in ReviewedFileRules.ReachedSteps(stage)) OfficeObjectives.Instance.MarkDone(key);
    }

    // =========================================================
    // PLAZA ORDER AND ARTHUR'S TALK
    // =========================================================

    private bool IsWaiting(TaxpayerNpcInteractable npc) =>
        !practice && !roundsOpen && !LevelGate.SideQuestDone && npc != null && !npc.HasBeenTalkedTo &&
        ReviewedFileRules.IsWaiting(npc.NpcId, false);

    /// <summary>The first tap on a waiting client: their one waiting line (never repeated in this run). Later taps do nothing.</summary>
    private void HandleWaitingTap(TaxpayerNpcInteractable npc)
    {
        if (npc == null || !waitingSaid.Add(npc.NpcId)) return;
        List<DialogueLine> line = TownNpcDialogueLibrary.GetWaitingLine(npc.NpcId, npc.DisplayName);
        if (line == null || !npc.TryStartAside(line)) waitingSaid.Remove(npc.NpcId);
    }

    /// <summary>The opening lessons (controls, Notes, Supply Counter) are behind the player, or there is no guide.</summary>
    private bool OpeningLessonsDone()
    {
        GuideDirector director = GuideRuntime.Instance != null ? GuideRuntime.Instance.Director : null;
        return introDone || roundsOpen || (director != null && (director.HasFinished("shop-intro") || director.HasFinished("follow")));
    }

    private NpcAvailability? OpeningAvailability(TutorialNpcController npc) =>
        practice ? (NpcAvailability?)null
        : ReviewedFileRules.OpeningAvailable(introDone, OpeningLessonsDone()) ? NpcAvailability.Available : (NpcAvailability?)null;

    /// <summary>A deliberate tap on Arthur before (or, in an older run, after) the board: his opening talk.</summary>
    private bool HandleOpeningTap(TutorialNpcController npc)
    {
        if (practice || !ReviewedFileRules.OpeningAvailable(introDone, OpeningLessonsDone())) return false;
        var builder = new DialogueBuilder("Arthur");
        foreach (string line in ReviewedFileRules.Content().arthurIntro) builder.Npc(line);
        return npc.TryStartOpeningTalk(builder.Build(), OnIntroCompleted, null);
    }

    /// <summary>Only a completed talk presents the file (a cancelled one changes nothing).</summary>
    private void OnIntroCompleted()
    {
        if (introDone) return;
        introDone = true;
        StartCoroutine(SpawnAfterTalk());
        metPending = true;
    }

    private IEnumerator SpawnAfterTalk()
    {
        // Dialogue restores the gameplay camera before completion; let its
        // transforms settle before choosing the visible side of Arthur.
        yield return null;
        while (DialogueUI.IsAnyDialogueActive) yield return null;
        SpawnPickup(animated: true);
    }

    /// <summary>The file floats beside Arthur, toward the player (the Client Copy's pose rules).</summary>
    private void SpawnPickup(bool animated)
    {
        if (practice || FileOwned())
        {
            pickupSpawnPending = false;
            return;
        }
        if (pickup != null)
        {
            pickupSpawnPending = false;
            return;
        }
        if (!ready || guide == null)
        {
            pickupSpawnPending = true;
            pickupSpawnAnimated |= animated;
            return;
        }
        pickupSpawnPending = false;
        pickupSpawnAnimated = false;
        Vector3 floor = guide.transform.position;
        float height = 1.7f;
        if (DialogueSpeakerFrames.TryResolve(guide, out DialogueSpeakerFrame frame))
        {
            floor = frame.Floor;
            height = frame.Height;
        }
        Vector3 toPlayer = guide.transform.forward;
        Camera camera = HeldOnScreen.ActiveCamera(Camera.main);
        if (camera != null) toPlayer = camera.transform.position - floor;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) toPlayer = Vector3.forward;
        toPlayer.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, toPlayer).normalized;
        Vector3 rest = floor + Vector3.up * (height * 0.62f) + toPlayer * 0.55f + side * 0.5f;
        const float tipBack = 15f * Mathf.Deg2Rad;
        Vector3 cover = toPlayer * Mathf.Cos(tipBack) + Vector3.up * Mathf.Sin(tipBack);
        Vector3 top = Vector3.up * Mathf.Cos(tipBack) - toPlayer * Mathf.Sin(tipBack);
        Vector3? from = animated ? floor + Vector3.up * (height * 0.55f) + toPlayer * 0.2f : (Vector3?)null;
        pickup = ReviewedFilePickup.Create(gameObject.scene, rest, Quaternion.LookRotation(top, cover), from, CommitFromPickup, OnPickupLanded);
        if (pickup == null) pickupSpawnPending = true;
    }

    private void ResolveGuide()
    {
        if (guide != null) return;
        foreach (TutorialNpcController candidate in FindObjectsByType<TutorialNpcController>(FindObjectsInactive.Include))
            if (candidate.gameObject.scene == gameObject.scene) { guide = candidate; break; }
    }

    /// <summary>The pickup asks before it flies: the item and its claim are saved first. Null = stays.</summary>
    private RectTransform CommitFromPickup()
    {
        HudHotbar hotbar = HudHotbar.Instance;
        if (hotbar == null) return null;
        InventoryResult result = hotbar.CollectReviewedFile();
        if (result.Outcome == InventoryOutcome.SaveFailed)
        {
            hotbar.Notice("That could not be saved just now. Tap the file again.");
            return null;
        }
        if (result.Outcome == InventoryOutcome.Rejected)
        {
            hotbar.Notice(string.IsNullOrEmpty(result.Message) ? "That can't be taken right now." : result.Message + ".");
            return null;
        }
        return hotbar.ItemTarget(ReviewedFileRules.ItemId) ?? hotbar.BagIcon;
    }

    private void OnPickupLanded()
    {
        pickup = null;
        GameplayEvents.RaiseGuideCue(CueCollected);
        Changed?.Invoke();
    }

    // =========================================================
    // READING (ReviewedFileViewer)
    // =========================================================

    internal void ViewerOpened() => GameplayEvents.RaiseGuideCue(CueOpened);

    public bool HasInspectedRecords => presented.Contains(ReviewedFileRules.IdentityId) && presented.Contains(ReviewedFileRules.BackgroundId);
    public bool HasReturnedRecords => ReviewedFileRules.AllReturned(returned);

    // 2026-10-27: the guided steps of the first reading (page turned, one record out, the ID
    // turned over, the optional stack). Hand-holding only: never saved, never required by the
    // route (shown + put back + page one still opens the rounds).
    private bool pageTwoSeen, cardFlipped, recordsGrouped;

    private bool ReadingFlag(string name)
    {
        bool done = revisited || roundsOpen;
        switch (name)
        {
            case "reviewed-page2": return pageTwoSeen || presented.Count > 0 || done;
            case "reviewed-one-out": return presented.Count > 0 || done;
            case "reviewed-inspected": return HasInspectedRecords || done;
            case "reviewed-flipped": return cardFlipped || HasReturnedRecords || done;
            case "reviewed-grouped": return recordsGrouped || HasReturnedRecords || done;
            case "reviewed-returned": return HasReturnedRecords || done;
            case "reviewed-complete": return done;
            default: return false;
        }
    }

    internal void RecordFlipped(bool backUp)
    {
        if (backUp) cardFlipped = true;
    }

    internal void RecordsGrouped() => recordsGrouped = true;

    internal void RecordShown(string recordId)
    {
        if (practice || Array.IndexOf(ReviewedFileRules.RecordIds, recordId) < 0) return;
        if (!presented.Contains(recordId))
        {
            presented.Add(recordId);
            Changed?.Invoke();
        }
        GameplayEvents.RaiseGuideCue(CueRecordRead);
    }

    internal void RecordPutBack(string recordId)
    {
        if (practice || !presented.Contains(recordId) || returned.Contains(recordId)) return;
        returned.Add(recordId);
        Changed?.Invoke();
    }

    /// <summary>Page one seen again after both records were put back: the file was looked over and the rounds open.</summary>
    internal void PageShown(int page)
    {
        if (page == ReviewedFileViewer.SupportingPageIndex) pageTwoSeen = true;
        if (practice || page != 0 || revisited || !ReviewedFileRules.AllReturned(returned)) return;
        revisited = true;
        bool firstTime = !roundsOpen;
        roundsOpen = true;
        Changed?.Invoke();
        if (!firstTime) return;
        TownEvents.RaiseReviewedFileLookedOver();
    }

    // =========================================================
    // CHECKPOINT (Level1FieldCheckpointer)
    // =========================================================

    public void Capture(Level1FieldCheckpoint into)
    {
        if (practice || into == null) return;
        into.arthurIntroDone = introDone;
        into.presentedRecordIds.Clear();
        into.presentedRecordIds.AddRange(presented);
        into.returnedRecordIds.Clear();
        into.returnedRecordIds.AddRange(returned);
        into.reviewedPageRevisited = revisited;
        into.roundsOpen = roundsOpen;
    }

    /// <summary>Resume (already migrated and repaired): milestones back, the file floating again if it was presented but not taken. Nothing plays.</summary>
    public void Restore(Level1FieldCheckpoint saved, bool fileOwned)
    {
        if (practice || saved == null) return;
        introDone = saved.arthurIntroDone || fileOwned;
        presented.Clear();
        presented.AddRange(saved.presentedRecordIds);
        returned.Clear();
        returned.AddRange(saved.returnedRecordIds);
        revisited = saved.reviewedPageRevisited;
        roundsOpen = saved.roundsOpen;
        if (introDone && !fileOwned) SpawnPickup(animated: false);
    }
}
