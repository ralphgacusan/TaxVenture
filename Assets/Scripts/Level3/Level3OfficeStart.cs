using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Owns the Level 3 start inside Office_Level3 (a one-time copy of the saved
/// Office_Level2, Documentation/Level3TheNumbersTellAStory/SCENE_REUSE.md).
/// Same self-bootstrapping pattern as Level2OfficeStart: no scene edits and
/// no serialized references.
///
/// - Builds (or reuses) the single owned root "Level3Gameplay"; it is never
///   duplicated on reload and later tickets add their parts under it.
/// - Rosa's Level 2 parts never exist here: they are built at runtime only
///   in Office_Level2. Before any copied Level 1 Start() runs, switches off
///   what the copy still holds (Level 1's plaza quest crowd, the corkboard
///   side quest, the Client IDs icon, the waiting-area Marie and the old
///   popup tutorial) from Resources/Levels/Level_03/office_start.json, and
///   hides the desk Case Folder so Level 1's folder flow cannot start.
///   Objects are deactivated, never deleted; Levels 1 and 2 are untouched.
/// - Keeps the shared office: environment, doors, player rig, cameras,
///   joysticks, EventSystem, audio, desk, book and service actors, with the
///   same runtime lighting balance and receptionist/Auditor rigs Level 2
///   applies to this copied office (mirrored here; Level 2's code is untouched).
/// - Fills the receptionist greeting's {history} line with the line for the
///   player's saved Rosa history (read only; ReceptionistInteractable.
///   GreetingFilter), then stands the player in the reception facing the
///   receptionist (Level 2's placement).
/// - Adds Level3Checkpointer (chapter phase, save/resume, legacy guard) and
///   Level3DeskCalculator (T03: the physical calculator and practice card)
///   and Level3BeaTable (T04: Bea, her plaza worktable and her papers).
///
/// DOES NOT: grade, award, acquire records, run the calculator or complete a case.
/// </summary>
[DefaultExecutionOrder(90)]
public sealed class Level3OfficeStart : MonoBehaviour
{
    public const string RootName = "Level3Gameplay";
    /// <summary>Bump when this start changes what it builds, so a later session can tell setups apart.</summary>
    public const int SetupVersion = 3;
    public const string ConfigPath = "Levels/Level_03/office_start";
    public const string HistoryToken = "{history}";
    private const float StartTimeout = 6f;

    public static Level3OfficeStart Instance { get; private set; }

    /// <summary>The setup version this root was built with.</summary>
    public int AppliedSetupVersion { get; private set; }

    /// <summary>True once the player has been stood in the reception.</summary>
    public bool PlayerPlaced { get; private set; }

    /// <summary>How many objects / components the start switched off.</summary>
    public int HiddenCount { get; private set; }
    public int DisabledCount { get; private set; }

    /// <summary>The desk Case Folder was hidden (Level 1's folder flow cannot start).</summary>
    public bool DeskFolderHidden { get; private set; }

#pragma warning disable 0649
    [Serializable]
    private class HistoryLine
    {
        public string id;
        public string text;
    }

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
        public bool hideDeskFolder = true;
        public List<HistoryLine> receptionHistory = new List<HistoryLine>();
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
        if (OfficeLevelProfile.LevelIdFor(scene) != OfficeLevelCatalog.Level3Id) return;

        GameObject root = null;
        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            if (candidate.GetComponentInChildren<Level3OfficeStart>(true) != null) return;
            if (candidate.name == RootName && root == null) root = candidate;
        }

        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        // Awake runs here, before any scene object's Start.
        root.AddComponent<Level3OfficeStart>();
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
        EnsureOfficeActors();
        EnsurePart<Level3Checkpointer>(Level3Checkpointer.ChildName);
        // T03: the shared desk calculator and its protected practice card.
        EnsurePart<Level3DeskCalculator>(Level3DeskCalculator.ChildName);
        // T04: Bea, her plaza worktable and her papers.
        EnsurePart<Level3BeaTable>(Level3BeaTable.ChildName);
        // T09: the Auditor's review of Bea's stamped computation and her review clock.
        EnsurePart<Level3Audit>(Level3Audit.ChildName);
        EnsurePart<Level3ReviewClock>(Level3ReviewClock.ChildName);
        Debug.Log($"[Level3] Office start v{SetupVersion}: switched off {HiddenCount} inherited object(s) and {DisabledCount} component(s).");
    }

    private void OnEnable() => ReceptionistInteractable.GreetingFilter = FillHistory;

    private void OnDisable()
    {
        if (ReceptionistInteractable.GreetingFilter == (Func<string, string>)FillHistory) ReceptionistInteractable.GreetingFilter = null;
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
            Debug.LogWarning("[Level3] The level did not start; Bea's case content did not load (see the Console). No placeholder case is played.");

        if (config.hideDeskFolder) HideDeskFolder();

        PlayerPlaced = PlayerPlacement.PlaceBetween(gameObject.scene, config.spawn, config.face, config.blend);
        if (!PlayerPlaced && !string.IsNullOrWhiteSpace(config.fallbackSpawn))
            PlayerPlaced = PlayerPlacement.PlaceBetween(gameObject.scene, config.fallbackSpawn, config.face,
                config.fallbackBlend > 0f && config.fallbackBlend <= 1f ? config.fallbackBlend : PlayerPlacement.DefaultBlend);
        if (!PlayerPlaced)
            Debug.LogWarning($"[Level3] Could not stand the player at '{config.spawn}'; the player keeps the scene start.");
    }

    /// <summary>One child per Level 3 part under the root (reused, never duplicated).</summary>
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
    // SHARED OFFICE LOOK (same values as Level2OfficeStart)
    // =========================================================

    /// <summary>Level 2's runtime balance for this office: one daylight key and a gentle office fill.</summary>
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

    /// <summary>The copied office predates Level 1's static-arm rig; Level 2 adds it at runtime, so Level 3 does too.</summary>
    private void EnsureOfficeActors()
    {
        foreach (ReceptionistInteractable actor in FindObjectsByType<ReceptionistInteractable>(FindObjectsInactive.Include))
            if (actor.gameObject.scene == gameObject.scene) EnsureOfficeActor(actor.gameObject);
        foreach (AuditorInteractable actor in FindObjectsByType<AuditorInteractable>(FindObjectsInactive.Include))
            if (actor.gameObject.scene == gameObject.scene) EnsureOfficeActor(actor.gameObject);
    }

    private static void EnsureOfficeActor(GameObject actor)
    {
        if (actor.GetComponent<NpcAnimationDriver>() == null) actor.AddComponent<NpcAnimationDriver>();
        StaticArmRig rig = actor.GetComponent<StaticArmRig>();
        if (rig == null) rig = actor.AddComponent<StaticArmRig>();
        rig.enabled = true;
    }

    // =========================================================
    // RECEPTION
    // =========================================================

    /// <summary>
    /// The receptionist's {history} line follows the saved Rosa history
    /// (CONTENT "First reception"), read when she speaks. Read only: no Rosa
    /// consequence is applied again and the case definition is not changed.
    /// </summary>
    private string FillHistory(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains(HistoryToken)) return text;
        PlayerProgressManager progress = PlayerProgressManager.Instance;
        string variant = Level3ChapterRules.HistoryVariant(progress != null ? progress.CurrentProgress : null);
        return text.Replace(HistoryToken, HistoryText(variant) ?? HistoryText("neutral") ?? "");
    }

    private string HistoryText(string id)
    {
        if (config.receptionHistory == null) return null;
        foreach (HistoryLine line in config.receptionHistory)
            if (line != null && line.id == id && !string.IsNullOrWhiteSpace(line.text)) return line.text;
        return null;
    }

    // =========================================================
    // INHERITED LEVEL 1 PARTS
    // =========================================================

    private void HideDeskFolder()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            CaseFolder3DDrag folder = root.GetComponentInChildren<CaseFolder3DDrag>(true);
            if (folder == null) continue;
            folder.HideAndDisableFolder();
            DeskFolderHidden = true;
            return;
        }
        Debug.LogWarning("[Level3] No desk CaseFolder3DDrag found; nothing to hide.");
    }

    private void HideUnusedParts()
    {
        foreach (string path in config.hide)
        {
            Transform part = PlayerPlacement.Find(gameObject.scene, path);
            if (part == null)
            {
                Debug.LogWarning($"[Level3] office_start hide '{path}' not found.");
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
            Type type = string.IsNullOrWhiteSpace(typeName) ? null : typeof(Level3OfficeStart).Assembly.GetType(typeName.Trim(), false);
            if (type == null || !typeof(Behaviour).IsAssignableFrom(type))
            {
                Debug.LogWarning($"[Level3] office_start disableComponents '{typeName}' is not a component type.");
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
                    if (loaded.receptionHistory == null) loaded.receptionHistory = new List<HistoryLine>();
                    if (loaded.blend <= 0f || loaded.blend > 1f) loaded.blend = PlayerPlacement.DefaultBlend;
                    return loaded;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Level3] Resources/{ConfigPath}.json is not valid JSON ({error.Message}). Using the built-in start.");
            }
        }
        else
        {
            Debug.LogWarning($"[Level3] Resources/{ConfigPath}.json not found. Using the built-in start.");
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
            "UI/HUD_Canvas/PermanentHUD/IconButtons/TaxpayerProfileIcon",
            "Environment/OfficeBuilding/Rooms/Reception/WaitingArea/Client2"
        },
        disableComponents = new List<string> { "TutorialController", "StampSetInteractable" },
        hideDeskFolder = true,
        receptionHistory = new List<HistoryLine>
        {
            new HistoryLine { id = "neutral", text = "Bea has booked a computation appointment. Her records are ready in the plaza." }
        }
    };
}
