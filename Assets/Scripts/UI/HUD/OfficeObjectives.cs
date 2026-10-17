using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Always-on objective progress for Office_Level1, built by OfficeHudRuntime.
/// Loads the editable objectives (TaskListProvider.LoadObjectives), records
/// which triggers have happened, and publishes one shared snapshot that the
/// HUD objective tracker and the Notes paper both render.
///
/// LISTENS TO (observation only — nothing here changes gameplay):
/// - GameStateMachine.OnStateChanged      -> "state:&lt;Name&gt;"
/// - TownEvents / GameplayEvents          -> "event:&lt;Name&gt;" (see ObjectiveTriggers)
/// - Player position, only while the ACTIVE step is an "area:" trigger,
///   checked 4 times a second, so walking through early does not count.
///
/// The record lives on this scene object, so it starts empty on every
/// load of the level.
/// </summary>
public sealed class OfficeObjectives : MonoBehaviour
{
    private const float AreaCheckInterval = 0.25f;

    public static OfficeObjectives Instance { get; private set; }

    /// <summary>Raised whenever the snapshot or a live counter may have changed.</summary>
    public event Action Changed;

    public IReadOnlyList<TaskDefinition> Tasks => tasks;
    public TaskProgressSnapshot Snapshot { get; private set; }

    private List<TaskDefinition> tasks = new List<TaskDefinition>();
    private readonly ObjectiveLog log = new ObjectiveLog();
    private GameStateMachine stateMachine;

    private string areaKey;
    private Bounds areaBounds;
    private bool areaValid;
    private float nextAreaCheck;
    private bool seeded;

    public static OfficeObjectives Create(GameObject host, string levelId)
    {
        var objectives = host.AddComponent<OfficeObjectives>();
        objectives.tasks = TaskListProvider.LoadObjectives(levelId);
        objectives.Reevaluate(notify: false);
        return objectives;
    }

    private void Awake() => Instance = this;

    private void Start() => SeedFromGameState();

    /// <summary>
    /// One-time catch-up from public game state, so a milestone that happened
    /// before this listener existed (script order, scene setup) still counts.
    /// Observation only: reads flags, never changes them.
    /// </summary>
    private void SeedFromGameState()
    {
        if (seeded) return;
        seeded = true;

        if (FolderToHUDIcon.GetStoredFolderCount() > 0)
            Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderStored));

        TaxpayerProfileCollectionHUD profiles = TaxpayerProfileCollectionHUD.Instance;
        if (profiles != null && profiles.AllProfilesCollected)
            Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.AllProfilesCollected));

        TutorialNpcController[] guides = FindObjectsByType<TutorialNpcController>(FindObjectsInactive.Include);
        for (int i = 0; i < guides.Length; i++)
        {
            if (guides[i].HasAppeared) Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.TutorialGuideAppeared));
            if (guides[i].HasStartedSideQuest) Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.TutorialGuideTalkedTo));
            if (guides[i].HasShownCompletionPanel) Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.SideQuestCompleted));
        }

        // The profile stack locks to one card once the side quest is solved.
        CollectedProfilesWorldPanel panel = CollectedProfilesWorldPanel.Instance;
        if (panel != null && panel.IsLockedToSingleProfile)
            Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.SideQuestCompleted));

        if (GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentState != null)
            HandleStateChanged(GameStateMachine.Instance.CurrentState);
    }

    private void OnEnable()
    {
        TownEvents.OnProfileCollected += HandleProfileCollected;
        TownEvents.OnAllProfilesCollected += HandleAllProfilesCollected;
        TownEvents.OnTutorialNpcReady += HandleGuideAppeared;
        TownEvents.OnTutorialIntroCompleted += HandleGuideTalkedTo;
        TownEvents.OnSideQuestCompleted += HandleSideQuestCompleted;
        GameplayEvents.OnNotesUnlockRequested += HandleReceptionistTalkedTo;
        GameplayEvents.OnCaseFolderOpened += HandleCaseFolderOpened;
        GameplayEvents.OnTaxCodeBookFirstOpened += HandleTaxCodeBookOpened;
        GameplayEvents.OnTaxReturnCollected += HandleTaxReturnCollected;
        GameplayEvents.OnCaseCompleted += HandleCaseCompleted;
        GameplayEvents.OnCaseFolderStored += HandleCaseFolderStored;
        GameplayEvents.OnCaseFolderReturned += HandleCaseFolderReturned;
        GameplayEvents.OnCaseFolderRetrieved += HandleCaseFolderRetrieved;
        GameplayEvents.OnPreliminaryHoldStamped += HandlePreliminaryHoldStamped;
        GameplayEvents.OnFindingRecorded += HandleFindingRecorded;
        GameplayEvents.OnMarketDeskOpened += HandleMarketDeskOpened;
        GameplayEvents.OnRosaRecordsCollected += HandleRosaRecordsCollected;

        stateMachine = GameStateMachine.Instance;
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += HandleStateChanged;
            if (stateMachine.CurrentState != null) HandleStateChanged(stateMachine.CurrentState);
        }
    }

    private void OnDisable()
    {
        TownEvents.OnProfileCollected -= HandleProfileCollected;
        TownEvents.OnAllProfilesCollected -= HandleAllProfilesCollected;
        TownEvents.OnTutorialNpcReady -= HandleGuideAppeared;
        TownEvents.OnTutorialIntroCompleted -= HandleGuideTalkedTo;
        TownEvents.OnSideQuestCompleted -= HandleSideQuestCompleted;
        GameplayEvents.OnNotesUnlockRequested -= HandleReceptionistTalkedTo;
        GameplayEvents.OnCaseFolderOpened -= HandleCaseFolderOpened;
        GameplayEvents.OnTaxCodeBookFirstOpened -= HandleTaxCodeBookOpened;
        GameplayEvents.OnTaxReturnCollected -= HandleTaxReturnCollected;
        GameplayEvents.OnCaseCompleted -= HandleCaseCompleted;
        GameplayEvents.OnCaseFolderStored -= HandleCaseFolderStored;
        GameplayEvents.OnCaseFolderReturned -= HandleCaseFolderReturned;
        GameplayEvents.OnCaseFolderRetrieved -= HandleCaseFolderRetrieved;
        GameplayEvents.OnPreliminaryHoldStamped -= HandlePreliminaryHoldStamped;
        GameplayEvents.OnFindingRecorded -= HandleFindingRecorded;
        GameplayEvents.OnMarketDeskOpened -= HandleMarketDeskOpened;
        GameplayEvents.OnRosaRecordsCollected -= HandleRosaRecordsCollected;

        if (stateMachine != null) stateMachine.OnStateChanged -= HandleStateChanged;
        stateMachine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // =========================================================
    // EVENT HANDLERS (named so they unsubscribe cleanly)
    // =========================================================

    private void HandleStateChanged(IGameState state)
    {
        if (state != null) Record(ObjectiveTriggers.StateKey(state.GetType()));
    }

    private void HandleProfileCollected(string profileId)
    {
        // Always notify: the "2 / 4" counter changes on every pickup.
        if (!Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.ProfileCollected))) Changed?.Invoke();
    }

    private void HandleAllProfilesCollected() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.AllProfilesCollected));
    private void HandleGuideAppeared() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.TutorialGuideAppeared));
    private void HandleGuideTalkedTo() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.TutorialGuideTalkedTo));
    private void HandleSideQuestCompleted() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.SideQuestCompleted));
    private void HandleReceptionistTalkedTo() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.ReceptionistTalkedTo));
    private void HandleCaseFolderOpened() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderOpened));
    private void HandleTaxCodeBookOpened() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.TaxCodeBookOpened));
    private void HandleTaxReturnCollected() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.TaxReturnCollected));
    private void HandleCaseCompleted() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseCompleted));
    private void HandleCaseFolderStored() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderStored));
    private void HandleCaseFolderReturned() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderReturned));
    private void HandleCaseFolderRetrieved() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderRetrieved));
    private void HandlePreliminaryHoldStamped() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.PreliminaryHoldStamped));
    private void HandleFindingRecorded() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.FindingRecorded));
    private void HandleMarketDeskOpened() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.MarketDeskOpened));
    private void HandleRosaRecordsCollected() => Record(ObjectiveTriggers.EventKey(ObjectiveTriggers.RosaRecordsCollected));

    /// <summary>Records a trigger (and any "undoneBy" it causes); notifies when anything changed.</summary>
    private bool Record(string key)
    {
        if (!log.Record(key, tasks)) return false;
        Reevaluate(notify: true);
        return true;
    }

    public bool IsDone(TaskDefinition task) => log.IsDone(task);

    /// <summary>
    /// Marks a step's trigger as happened (Tutorial Hub practice: a drill that
    /// starts in the office marks the side quest steps done, so the tracker
    /// and arrows point at the drill). True when anything changed.
    /// </summary>
    public bool MarkDone(string trigger) =>
        !string.IsNullOrWhiteSpace(trigger) && Record(ObjectiveTriggers.Normalize(trigger.Trim()));

    private void Reevaluate(bool notify)
    {
        Snapshot = TaskProgress.Evaluate(tasks, IsDone);
        PrepareAreaCheck();
        if (notify) Changed?.Invoke();
    }

    // =========================================================
    // LIVE COUNTERS
    // =========================================================

    /// <summary>Live counter for a step (e.g. profiles 2 / 4). False when the step has none.</summary>
    public bool TryGetProgress(TaskDefinition task, out int current, out int total)
    {
        current = total = 0;
        if (task == null || task.ProgressSource != ObjectiveTriggers.ProfilesProgress) return false;

        TaxpayerProfileCollectionHUD profiles = TaxpayerProfileCollectionHUD.Instance;
        if (profiles == null) return false;

        total = profiles.RequiredProfiles;
        current = IsDone(task) ? total : Mathf.Min(profiles.CollectedProfiles.Count, total);
        return total > 0;
    }

    // =========================================================
    // AREA TRIGGER (only for the active step)
    // =========================================================

    private void PrepareAreaCheck()
    {
        TaskDefinition active = Snapshot != null && Snapshot.ActiveIndex >= 0 ? tasks[Snapshot.ActiveIndex] : null;
        string key = active != null && ObjectiveTriggers.TryGetArea(active.TriggerKey, out _) ? active.TriggerKey : null;
        if (key == areaKey) return;

        areaKey = key;
        areaValid = key != null && TryResolveArea(key, out areaBounds);
        // Never toggle `enabled` here: disabling this component would run
        // OnDisable and unsubscribe every objective event.
    }

    private bool TryResolveArea(string key, out Bounds bounds) => ResolveArea(gameObject.scene, key, out bounds);

    /// <summary>World bounds of an "area:" key (A|B joined); false when nothing usable is found.</summary>
    public static bool ResolveArea(Scene scene, string key, out Bounds bounds)
    {
        bounds = default;
        if (!ObjectiveTriggers.TryGetAreaPaths(key, out string[] paths)) return false;

        bool any = false;
        for (int p = 0; p < paths.Length; p++)
        {
            GameObject target = FindInScene(scene, paths[p]);
            if (target == null)
            {
                Debug.LogWarning($"[OfficeObjectives] Area \"{paths[p]}\" not found in the scene.");
                continue;
            }

            bool found = false;
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>())
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
                found = true;
            }
            if (!found)
            {
                foreach (Collider c in target.GetComponentsInChildren<Collider>())
                {
                    if (!any) { bounds = c.bounds; any = true; }
                    else bounds.Encapsulate(c.bounds);
                    found = true;
                }
            }
            if (!found) Debug.LogWarning($"[OfficeObjectives] Area \"{paths[p]}\" has no renderers or colliders.");
        }

        if (!any) Debug.LogWarning("[OfficeObjectives] No usable area for \"" + key + "\"; that step cannot complete.");
        return any;
    }

    private static GameObject FindInScene(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        string rest = slash < 0 ? null : path.Substring(slash + 1);

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name != rootName) continue;
            if (rest == null) return roots[i];
            Transform child = roots[i].transform.Find(rest);
            if (child != null) return child.gameObject;
        }
        return null;
    }

    /// <summary>True when a point stands over the area (floor footprint, generous height).</summary>
    public static bool IsInsideArea(Bounds area, Vector3 point)
    {
        const float margin = 0.25f;
        return point.x >= area.min.x - margin && point.x <= area.max.x + margin &&
               point.z >= area.min.z - margin && point.z <= area.max.z + margin &&
               point.y >= area.min.y - 1.5f && point.y <= area.max.y + 5f;
    }

    private void LateUpdate()
    {
        if (areaKey == null || !areaValid) return;

        if (Time.unscaledTime < nextAreaCheck) return;
        nextAreaCheck = Time.unscaledTime + AreaCheckInterval;

        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (player != null && IsInsideArea(areaBounds, player.position))
            Record(areaKey);
    }
}
