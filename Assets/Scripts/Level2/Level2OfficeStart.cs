using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Owns the Level 2 start inside Office_Level2 (a one-time copy of
/// Office_Level1, see Documentation/Level2SomethingDoesNotMatch/SCENE_REUSE.md).
/// Same self-bootstrapping pattern as the other office runtimes: no scene
/// edits and no serialized references.
///
/// - Builds (or reuses) the single owned root "Level2Gameplay". Later Level 2
///   tickets add their parts under it; it is never duplicated on reload.
/// - Before any copied Level 1 Start() runs, switches off what Level 2 never
///   uses (plaza profile clients, Arthur, the corkboard side quest, the old
///   popup tutorial), from Resources/Levels/Level_02/office_start.json.
///   Objects are deactivated, never deleted; Level 1's own scene is untouched.
/// - Once the level has started, stands the player in the reception facing
///   the receptionist (the Tutorial Hub office-drill placement).
/// - Moves Rosa's review from OfficeWelcome to RetrieveFolder when the
///   receptionist conversation finishes (observes the existing event).
/// - Adds the Level 2 parts as children: CaseRetrieval (T02 drawer) and
///   FolderHandoff (T02: folder waits in the HUD icon until put on the desk),
///   FindingsPage and PreliminaryStamp (T03: finding note, initial Not Ready),
///   RosaStall (T04: store, Rosa, records desk, envelope and records).
///
/// DOES NOT: grade, award, archive, complete the case or drive the tutorial.
/// </summary>
[DefaultExecutionOrder(90)]
public sealed class Level2OfficeStart : MonoBehaviour
{
    public const string RootName = "Level2Gameplay";
    /// <summary>Bump when this start changes what it builds, so a later session can tell setups apart.</summary>
    public const int SetupVersion = 13;
    public const string ConfigPath = "Levels/Level_02/office_start";
    private const float StartTimeout = 6f;

    public static Level2OfficeStart Instance { get; private set; }

    /// <summary>The setup version this root was built with.</summary>
    public int AppliedSetupVersion { get; private set; }

    /// <summary>True once the player has been stood in the reception.</summary>
    public bool PlayerPlaced { get; private set; }

    /// <summary>How many objects / components the start switched off.</summary>
    public int HiddenCount { get; private set; }
    public int DisabledCount { get; private set; }

#pragma warning disable 0649
    [Serializable]
    private class StartConfig
    {
        public string spawn;
        public string face;
        public float blend = PlayerPlacement.DefaultBlend;
        public string fallbackSpawn;
        public float fallbackBlend = PlayerPlacement.DefaultBlend;
        public List<string> hide = new List<string>();
        public List<string> disableComponents = new List<string>();
    }
#pragma warning restore 0649

    private StartConfig config;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeForLoadedScenes()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            EnsureForScene(SceneManager.GetSceneAt(i));
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureForScene(scene);

    private static void EnsureForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (OfficeLevelProfile.LevelIdFor(scene) != OfficeLevelCatalog.Level2Id) return;

        GameObject root = null;
        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            if (candidate.GetComponentInChildren<Level2OfficeStart>(true) != null) return;
            if (candidate.name == RootName && root == null) root = candidate;
        }

        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        // Awake runs here, before any scene object's Start.
        root.AddComponent<Level2OfficeStart>();
    }

    private void Awake()
    {
        Instance = this;
        AppliedSetupVersion = SetupVersion;
        FolderToHUDIcon.ResetStoredFolderCount();
        config = LoadConfig();
        BalanceLighting();
        HideUnusedParts();
        DisableUnusedComponents();
        EnsurePart<Level2CaseRetrieval>(Level2CaseRetrieval.ChildName);
        EnsurePart<Level2FolderHandoff>(Level2FolderHandoff.ChildName);
        EnsurePart<Level2FindingsPage>(Level2FindingsPage.ChildName);
        EnsurePart<Level2PreliminaryStamp>(Level2PreliminaryStamp.ChildName);
        EnsurePart<Level2DeskStamps>(Level2DeskStamps.ChildName);
        EnsurePart<Level2RosaStall>(Level2RosaStall.ChildName);
        EnsurePart<Level2OfficeReview>(Level2OfficeReview.ChildName);
        EnsurePart<Level2Audit>(Level2Audit.ChildName);
        EnsurePart<Level2ReviewClock>(Level2ReviewClock.ChildName);
        EnsurePart<Level2Checkpointer>(Level2Checkpointer.ChildName);
        EnsurePart<Level2AnnotatedPapers>(Level2AnnotatedPapers.ChildName);
        EnsurePart<Level2TaxCodeBookIcon>(Level2TaxCodeBookIcon.ChildName);
        Debug.Log($"[Level2] Office start v{SetupVersion}: switched off {HiddenCount} Level 1 object(s) and {DisabledCount} component(s).");
    }

    /// <summary>Keep one daylight key and a gentle office fill; both previously lit the whole scene at full strength.</summary>
    private void BalanceLighting()
    {
        Transform sun = PlayerPlacement.Find(gameObject.scene, "Env/Directional Light");
        Light daylight = sun != null ? sun.GetComponent<Light>() : null;
        if (daylight != null)
        {
            daylight.intensity = 0.95f;
            daylight.color = new Color(1f, 0.97f, 0.92f);
            daylight.shadowStrength = 0.7f;
        }
        Transform inside = PlayerPlacement.Find(gameObject.scene, "Environment/OfficeBuilding/Inside/Directional Light (2)");
        Light fill = inside != null ? inside.GetComponent<Light>() : null;
        if (fill != null)
        {
            fill.intensity = 0.28f;
            fill.color = new Color(0.92f, 0.95f, 1f);
        }
    }

    private void OnEnable()
    {
        GameplayEvents.OnNotesUnlockRequested += HandleReceptionistTalkedTo;
        GuideFlags.Source = Flag;
    }

    private void OnDisable()
    {
        GameplayEvents.OnNotesUnlockRequested -= HandleReceptionistTalkedTo;
        if (GuideFlags.Source == (Func<string, bool>)Flag) GuideFlags.Source = null;
    }

    /// <summary>Level 2 facts for guide.json "flag:" conditions (read only).</summary>
    public static bool Flag(string name)
    {
        CaseReviewState review = Review;
        if (review == null) return false;
        switch (name)
        {
            case "folderRetrieved": return review.FolderRetrieved;
            case "bookStored": return review.taxBookStored;
            case "initialStamped": return review.preliminary != null;
            case "recordsCollected": return review.HandoverDone;
            case "questDone": return review.challenge != null && review.challenge.awarded;
            case "officeReview": return review.phase >= ReviewPhase.OfficeReconciliation;
            case "inFileRoom": return Instance != null && Instance.PlayerInFileRoom();
            case "findingRecorded": return review.preliminary != null || (review.findings != null && review.findings.Exists(f => f != null && f.comparisonConfirmed));
            case "tutorialDone": return review.tutorialCompleted;
            case "firstChecked": return review.challenge != null && review.challenge.firstChecked;
            case "redNotes": return review.challenge != null && review.challenge.firstChecked && !review.challenge.mastered;
            case "officeDone": return review.officeReviewAnnounced;
            case "finalStamped": return review.finalStamp != null && !string.IsNullOrEmpty(review.finalStamp.verdict) && FinalAssessment.IsCurrent(review);
            case "stampsUp":
            {
                Level2DeskStamps stamps = Instance != null ? Instance.GetComponentInChildren<Level2DeskStamps>() : null;
                return stamps != null && stamps.Active;
            }
            case "auditApproved": return review.audit != null && review.audit.approved;
            default: return false;
        }
    }

    private const string FileRoomFloorPath = "Environment/OfficeBuilding/Rooms/FileStorageRoom/Floors";
    private Bounds fileRoom;
    private bool fileRoomKnown;
    private bool fileRoomLooked;

    /// <summary>The player stands on the file storage room floor (its renderers' footprint).</summary>
    private bool PlayerInFileRoom()
    {
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (player == null) return false;
        if (!fileRoomLooked)
        {
            fileRoomLooked = true;
            Transform floors = PlayerPlacement.Find(gameObject.scene, FileRoomFloorPath);
            if (floors != null)
                foreach (Renderer r in floors.GetComponentsInChildren<Renderer>(true))
                {
                    if (!fileRoomKnown) { fileRoom = r.bounds; fileRoomKnown = true; }
                    else fileRoom.Encapsulate(r.bounds);
                }
        }
        if (!fileRoomKnown) return false;
        Vector3 p = player.position;
        return p.x > fileRoom.min.x + 0.3f && p.x < fileRoom.max.x - 0.3f && p.z > fileRoom.min.z + 0.3f && p.z < fileRoom.max.z - 0.3f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private IEnumerator Start()
    {
        float until = Time.unscaledTime + StartTimeout;
        while (Time.unscaledTime < until &&
               (CaseProgressionManager.Instance == null || !CaseProgressionManager.Instance.LevelStarted ||
                CameraController.Instance == null))
            yield return null;
        yield return null;

        if (CaseProgressionManager.Instance == null || !CaseProgressionManager.Instance.LevelStarted)
            Debug.LogWarning("[Level2] The level did not start; Rosa's case is not loaded (see the Console).");

        PlayerPlaced = PlayerPlacement.PlaceBetween(gameObject.scene, config.spawn, config.face, config.blend);
        if (!PlayerPlaced && !string.IsNullOrWhiteSpace(config.fallbackSpawn))
            PlayerPlaced = PlayerPlacement.PlaceBetween(gameObject.scene, config.fallbackSpawn, config.face,
                config.fallbackBlend > 0f && config.fallbackBlend <= 1f ? config.fallbackBlend : PlayerPlacement.DefaultBlend);
        if (!PlayerPlaced)
            Debug.LogWarning($"[Level2] Could not stand the player at '{config.spawn}'; the player keeps the scene start.");
    }

    /// <summary>One child per Level 2 part under the root (reused, never duplicated).</summary>
    private T EnsurePart<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            child = new GameObject(childName).transform;
            child.SetParent(transform, false);
        }
        T part = child.GetComponent<T>();
        return part != null ? part : child.gameObject.AddComponent<T>();
    }

    // =========================================================
    // REVIEW PHASE
    // =========================================================

    /// <summary>Rosa's review state on the current case (null before it loads).</summary>
    public static CaseReviewState Review =>
        CaseManager.Instance != null && CaseManager.Instance.CurrentCase != null
            ? CaseManager.Instance.CurrentCase.review
            : null;

    private void HandleReceptionistTalkedTo()
    {
        CaseReviewState review = Review;
        if (review != null && review.TryAdvance(ReviewPhase.RetrieveFolder))
            Debug.Log("[Level2] Review phase: RetrieveFolder (receptionist assigned Rosa's folder).");
    }

    // =========================================================
    // INHERITED LEVEL 1 PARTS
    // =========================================================

    private void HideUnusedParts()
    {
        foreach (string path in config.hide)
        {
            Transform part = PlayerPlacement.Find(gameObject.scene, path);
            if (part == null)
            {
                Debug.LogWarning($"[Level2] office_start hide '{path}' not found.");
                continue;
            }
            if (!part.gameObject.activeSelf) continue;
            part.gameObject.SetActive(false);
            HiddenCount++;
        }
    }

    private void DisableUnusedComponents()
    {
        foreach (string typeName in config.disableComponents)
        {
            Type type = string.IsNullOrWhiteSpace(typeName) ? null : typeof(Level2OfficeStart).Assembly.GetType(typeName.Trim(), false);
            if (type == null || !typeof(Behaviour).IsAssignableFrom(type))
            {
                Debug.LogWarning($"[Level2] office_start disableComponents '{typeName}' is not a component type.");
                continue;
            }
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                foreach (Component component in root.GetComponentsInChildren(type, true))
                {
                    var behaviour = (Behaviour)component;
                    if (!behaviour.enabled) continue;
                    behaviour.enabled = false;
                    DisabledCount++;
                }
        }
    }

    private static StartConfig LoadConfig()
    {
        TextAsset file = Resources.Load<TextAsset>(ConfigPath);
        if (file != null)
        {
            try
            {
                StartConfig loaded = JsonUtility.FromJson<StartConfig>(file.text);
                if (loaded != null)
                {
                    if (loaded.hide == null) loaded.hide = new List<string>();
                    if (loaded.disableComponents == null) loaded.disableComponents = new List<string>();
                    if (loaded.blend <= 0f || loaded.blend > 1f) loaded.blend = PlayerPlacement.DefaultBlend;
                    return loaded;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Level2] Resources/{ConfigPath}.json is not valid JSON ({error.Message}). Using the built-in start.");
            }
        }
        else
        {
            Debug.LogWarning($"[Level2] Resources/{ConfigPath}.json not found. Using the built-in start.");
        }
        return BuiltInConfig();
    }

    /// <summary>Mirrors office_start.json.</summary>
    private static StartConfig BuiltInConfig() => new StartConfig
    {
        spawn = "Environment/OfficeBuilding/Rooms/Reception/Doors/Office_Entrance_Door_2",
        face = "Environment/OfficeBuilding/Rooms/Reception/Receptionist",
        blend = 0.16f,
        fallbackSpawn = "Environment/OfficeBuilding/Rooms/Reception/Doors",
        fallbackBlend = 0.12f,
        hide = new List<string>
        {
            "Environment/Outside/Plaza/NPCS",
            "Environment/Outside/Plaza/CorkboardSideQuestRoot",
            "UI/HUD_Canvas/PermanentHUD/IconButtons/TaxpayerProfileIcon"
        },
        disableComponents = new List<string> { "TutorialController" }
    };
}
