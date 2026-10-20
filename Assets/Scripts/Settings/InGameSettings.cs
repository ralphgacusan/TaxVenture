using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// In-game settings: the same 3D settings board as the 3D Menu
/// (SettingsBoard3D, unchanged look and audio behaviour), opened from the
/// gameplay HUD's settings button. While it is open the game is paused (if
/// configured), the HUD canvases are hidden, and the player cannot walk, look
/// around or tap the world. BACK (or Escape / Android back) lifts the board
/// away and everything resumes.
///
/// SETUP: none. It adds itself (with its board) to every scene listed in
/// Resources/Settings/in_game_settings.json at load time. The old 2D
/// SettingsPanel (SettingsUI) stays in the scene but is kept closed:
/// SettingsUI.OpenSettings and SettingsButtonPlaceholder.OnSettingsPressed
/// open this board instead.
///
/// CONFIGURATION (Assets/Resources/Settings/in_game_settings.json):
/// - scenes: gameplay scenes that get the board.
/// - pauseGameWhenOpen: false = the game keeps running behind the board.
/// - pauseMode: "FreezeTime" = Time.timeScale 0, so NPCs, physics,
///   animations, the level timer and office hours all stop (the board runs on
///   unscaled time); "StoryOnly" = only GameStateMachine.Pause() (office hours
///   stop, the world keeps moving).
/// - hideHudWhileOpen: hide the screen-space HUD canvases while open.
/// A scene can also carry this component by hand: untick useResourcesConfig
/// to use the inspector values instead of the JSON.
/// </summary>
[DefaultExecutionOrder(-40)]
public sealed class InGameSettings : MonoBehaviour
{
    public const string ConfigResource = "Settings/in_game_settings";

    public enum PauseMode
    {
        /// <summary>Time.timeScale = 0 while open; restored to its old value on close.</summary>
        FreezeTime,
        /// <summary>GameStateMachine.Pause() only; time keeps running.</summary>
        StoryOnly,
    }

    // Filled by JsonUtility at runtime.
#pragma warning disable 0649
    [Serializable]
    public class Config
    {
        public string[] scenes = { "Office_Level1", "Office_Level2" };
        public bool pauseGameWhenOpen = true;
        public string pauseMode = "FreezeTime";
        public bool hideHudWhileOpen = true;
    }
#pragma warning restore 0649

    [Tooltip("Read pause settings from Resources/Settings/in_game_settings.json (untick to use the values below).")]
    [SerializeField] private bool useResourcesConfig = true;
    [Tooltip("Pause the game while the settings board is open.")]
    [SerializeField] private bool pauseGameWhenOpen = true;
    [Tooltip("FreezeTime: Time.timeScale 0 (everything on game time stops). StoryOnly: only the game state machine and office hours pause.")]
    [SerializeField] private PauseMode pauseMode = PauseMode.FreezeTime;
    [Tooltip("Hide the screen-space HUD canvases while the board is open.")]
    [SerializeField] private bool hideHudWhileOpen = true;

    private SettingsBoard3D board;
    private bool paused;
    private bool frozeTime;
    private float savedTimeScale = 1f;
    private bool pausedStateMachine;
    private bool holdingControls;

    private sealed class HiddenCanvas
    {
        public CanvasGroup Group;
        public bool Added;
        public float Alpha;
        public bool Interactable;
        public bool BlocksRaycasts;
    }

    private readonly List<HiddenCanvas> hiddenCanvases = new List<HiddenCanvas>();

    /// <summary>The in-game settings of the loaded gameplay scene (null in menus).</summary>
    public static InGameSettings Active { get; private set; }

    /// <summary>True while the in-game board is open (until it has lifted away).</summary>
    public static bool IsOpen => Active != null && Active.board != null && Active.board.IsOpen;

    /// <summary>True while this component is holding the game paused.</summary>
    public bool IsPaused => paused;

    public SettingsBoard3D Board => board;
    public bool PauseGameWhenOpen => pauseGameWhenOpen;
    public PauseMode Mode => pauseMode;

    /// <summary>Opens the in-game board if this scene has one. False = no in-game settings here.</summary>
    public static bool TryOpen()
    {
        if (Active == null || !Active.isActiveAndEnabled) return false;
        return Active.Open();
    }

    // =========================================================
    // BOOTSTRAP
    // =========================================================

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
        Config config = LoadConfig();
        if (config.scenes == null || !config.scenes.Contains(scene.name)) return;

        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<InGameSettings>(true) != null) return;

        var host = new GameObject("In-Game Settings");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<InGameSettings>();
    }

    public static Config LoadConfig()
    {
        TextAsset file = Resources.Load<TextAsset>(ConfigResource);
        if (file == null) return new Config();
        try
        {
            return JsonUtility.FromJson<Config>(file.text) ?? new Config();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[InGameSettings] {ConfigResource}.json is not valid JSON ({e.Message}). Using defaults.");
            return new Config();
        }
    }

    /// <summary>"FreezeTime" / "StoryOnly" (any case); anything else is FreezeTime.</summary>
    public static PauseMode ParsePauseMode(string value)
    {
        return string.Equals(value?.Trim(), "StoryOnly", StringComparison.OrdinalIgnoreCase)
            ? PauseMode.StoryOnly : PauseMode.FreezeTime;
    }

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        Active = this;
        if (useResourcesConfig)
        {
            Config config = LoadConfig();
            pauseGameWhenOpen = config.pauseGameWhenOpen;
            pauseMode = ParsePauseMode(config.pauseMode);
            hideHudWhileOpen = config.hideHudWhileOpen;
        }

        var boardHost = new GameObject("Settings Board 3D (In-Game)");
        boardHost.transform.SetParent(transform, false);
        board = boardHost.AddComponent<SettingsBoard3D>();
        board.ConfigureInGame();
        board.Opened += HandleOpened;
        board.Closed += HandleClosed;
    }

    private void Start()
    {
        // The old 2D panel stays in the scene (never deleted) but is kept closed.
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (SettingsUI legacy in root.GetComponentsInChildren<SettingsUI>(true))
                legacy.CloseSettings();
    }

    private void Update()
    {
        if (holdingControls) PlayerControlHold.Refresh();
    }

    private void OnDestroy()
    {
        if (board != null)
        {
            board.Opened -= HandleOpened;
            board.Closed -= HandleClosed;
        }
        // Never leave the next scene frozen.
        Resume();
        ShowHud();
        ReleaseControls();
        if (Active == this) Active = null;
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    public bool Open() => board != null && board.Open();

    /// <summary>Confirmed scene exit. Keep time frozen until the old scene is destroyed.</summary>
    public bool LeaveLevel(bool replay)
    {
        bool replayFirstStory = replay && !TrainingSession.IsPracticing && gameObject.scene.name == "Office_Level1";
        string destination = replayFirstStory ? Cutscene01Runtime.SceneName : replay ? gameObject.scene.name : MenuEntry.MenuSceneName;
        if (!Application.CanStreamedLevelBeLoaded(destination)) return false;
        if (!TrainingSession.IsPracticing)
        {
            Level2Checkpointer checkpoint = FindAnyObjectByType<Level2Checkpointer>();
            if (checkpoint != null && !checkpoint.PrepareSettingsExit(replay)) return false;
        }
        if (!replay)
        {
            MenuEntry.OpenLevelSelect = false;
            MenuEntry.OpenSettings = false;
        }
        // The intro owns its own fade-in; other destinations use the shared curtain.
        SceneFade.FadeOutThenLoad(destination, fadeInDestination: destination != Cutscene01Runtime.SceneName);
        return true;
    }

    private void HandleOpened()
    {
        Pause();
        if (hideHudWhileOpen) HideHud();
        holdingControls = true;
        PlayerControlHold.Acquire(this);
    }

    private void HandleClosed()
    {
        Resume();
        ShowHud();
        ReleaseControls();
        // The BACK tap must not reach the world in the frame the board goes.
        MobileInputGuard.SuppressWorldTapsThisFrame();
    }

    private void ReleaseControls()
    {
        if (!holdingControls) return;
        holdingControls = false;
        PlayerControlHold.Release(this);
    }

    // =========================================================
    // PAUSE
    // =========================================================

    private void Pause()
    {
        if (!pauseGameWhenOpen || paused) return;
        paused = true;
        if (pauseMode == PauseMode.FreezeTime)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            frozeTime = true;
        }
        else
        {
            GameStateMachine machine = GameStateMachine.Instance;
            if (machine != null && !machine.IsPaused)
            {
                machine.Pause();
                pausedStateMachine = true;
            }
        }
    }

    private void Resume()
    {
        if (!paused) return;
        paused = false;
        if (frozeTime)
        {
            frozeTime = false;
            Time.timeScale = savedTimeScale;
        }
        if (pausedStateMachine)
        {
            pausedStateMachine = false;
            if (GameStateMachine.Instance != null) GameStateMachine.Instance.Resume();
        }
    }

    // =========================================================
    // HUD
    // =========================================================

    /// <summary>Hides this scene's screen-space canvases (the HUD, joystick, prompts) without touching their objects.</summary>
    private void HideHud()
    {
        ShowHud();
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(false))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                if (canvas.GetComponent<SceneFade>() != null) continue;
                if (canvas.transform.IsChildOf(transform)) continue;
                CanvasGroup group = canvas.GetComponent<CanvasGroup>();
                var entry = new HiddenCanvas { Group = group, Added = group == null };
                if (entry.Added) entry.Group = canvas.gameObject.AddComponent<CanvasGroup>();
                else
                {
                    entry.Alpha = group.alpha;
                    entry.Interactable = group.interactable;
                    entry.BlocksRaycasts = group.blocksRaycasts;
                }
                entry.Group.alpha = 0f;
                entry.Group.interactable = false;
                entry.Group.blocksRaycasts = false;
                hiddenCanvases.Add(entry);
            }
    }

    private void ShowHud()
    {
        foreach (HiddenCanvas entry in hiddenCanvases)
        {
            if (entry.Group == null) continue;
            entry.Group.alpha = entry.Added ? 1f : entry.Alpha;
            entry.Group.interactable = entry.Added || entry.Interactable;
            entry.Group.blocksRaycasts = entry.Added || entry.BlocksRaycasts;
            if (entry.Added) Destroy(entry.Group);
        }
        hiddenCanvases.Clear();
    }
}
