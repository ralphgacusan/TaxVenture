using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// How the 3D Menu scene should open next time it loads, and the main menu's
/// routes (Campaign Foundation T03). The main menu and
/// the progress / level select and the settings board live in one scene ("3D Menu"); a finished
/// level asks it to open directly on the level select instead of the title
/// view. ToggleCamera reads (and clears) the request in Start.
///
/// ROUTES (thin commands; PlayerProgressManager owns the save, SceneFade the
/// scene change; CampaignMenu decides which notes show):
/// - Play (unfinished Level 1): reuses a campaign or commits one, then enters the
///   opening (unskippable: the fresh record has not seen it).
/// - New Game (saved Level 1 completion): the Settings board's confirmation card, then
///   StartNewGame, the same route as Play.
/// - Continue: the saved-progress module (career board camera); it never
///   launches a level by itself.
/// Story routes set CampaignRun to Story; the career board sets Story or MasteryReplay.
/// </summary>
public static class MenuEntry
{
    public const string MenuSceneName = "3D Menu";

    /// <summary>Names of the saved NEW GAME / CONTINUE scene objects (Settings duplicates).</summary>
    public const string NewGameButtonName = "new_game_button";
    public const string LegacyNewGameButtonName = "newgame_button";
    public const string ContinueButtonName = "continue_button";

    private static PlayerProgressData Progress =>
        PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null;

    /// <summary>One New Game / Play at a time: set before the fresh campaign is committed, cleared when the menu loads again.</summary>
    private static bool storyLaunching;
    public static bool IsStoryLaunching => storyLaunching;
    private static bool openingUnskippable;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        storyLaunching = false;
        openingUnskippable = false;
        OpenLevelSelect = false;
        OpenSettings = false;
        CampaignRun.EndRun();
    }

    public static bool ConsumeUnskippableOpening()
    {
        bool value = openingUnskippable;
        openingUnskippable = false;
        return value;
    }

    /// <summary>Saved story completion of Level 1: returning notes, tutorials and progress are available.</summary>
    public static bool HasCareerAccess => CurrentModel().HasCompletedFirstChapter;

    /// <summary>The menu model for the current save.</summary>
    public static CampaignMenuModel CurrentModel()
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        CampaignSaveState state = manager != null ? manager.CampaignState : CampaignSaveState.Unreadable;
        return CampaignMenu.Build(state, Progress, manager != null && manager.HasSavedFirstChapterCompletion);
    }

    /// <summary>The 3D Menu has (re)loaded: no run is in progress and a new launch may start.</summary>
    public static void MenuOpened()
    {
        CampaignRun.EndRun();
        storyLaunching = false;
        openingUnskippable = false;
    }

    private static bool Blocked(ToggleCamera cameraManager) =>
        storyLaunching || SceneFade.BlocksInput || (cameraManager != null && cameraManager.IsTransitioning);

    /// <summary>PLAY: start/restart the opening, or resume unfinished Level 1 directly. Never reset an existing campaign.</summary>
    public static void Play(ToggleCamera cameraManager)
    {
        if (Blocked(cameraManager)) return;
        if (HasCareerAccess)
        {
            Continue(cameraManager);
            return;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        if (!CurrentModel().HasStory)
        {
            if (!StartNewGame(cameraManager, out string error))
                Debug.LogWarning("[MenuEntry] Play could not start the story: " + error);
            return;
        }
        // An interrupted campaign keeps its id and progress. Before the intro finishes,
        // restart it; after it finishes, enter the level and let its checkpoint owner resume.
        storyLaunching = true;
        CampaignRun.Begin(CampaignRunMode.Story);
        OpenLevelSelect = false;
        OpenSettings = false;
        bool resumeLevel = Progress.HasSeenLevelOneIntro || Progress.CheckpointFor(CampaignRecord.FirstChapterId) != null;
        openingUnskippable = !resumeLevel;
        SceneFade.FadeOutThenLoad(resumeLevel ? "Office_Level1" : Cutscene01Runtime.SceneName, fadeInDestination: resumeLevel);
    }

    /// <summary>
    /// Commits a fresh campaign through the save owner, then loads the opening.
    /// False (with a message for the player) when it could not be saved; nothing changed then and retrying is safe.
    /// </summary>
    public static bool StartNewGame(ToggleCamera cameraManager, out string error)
    {
        error = null;
        if (Blocked(cameraManager)) { error = "Please wait a moment and try again."; return false; }
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        if (manager == null) { error = "Your save is not ready yet. Try again."; return false; }
        // Lock before Publish fires ProgressChanged; keep the current notes stable until scene exit.
        storyLaunching = true;
        if (!manager.StartNewCampaign(out string problem))
        {
            storyLaunching = false;
            Debug.LogWarning("[MenuEntry] New Game failed: " + problem);
            error = CampaignMenu.NewGameFailed;
            return false;
        }
        openingUnskippable = true;
        CampaignRun.Begin(CampaignRunMode.Story);
        OpenLevelSelect = false;
        OpenSettings = false;
        // The intro owns its own fade-in.
        SceneFade.FadeOutThenLoad(Cutscene01Runtime.SceneName, fadeInDestination: false);
        return true;
    }

    /// <summary>CONTINUE: the saved-progress module (career board). Opening it changes nothing in the save.</summary>
    public static bool Continue(ToggleCamera cameraManager)
    {
        if (Blocked(cameraManager) || !HasCareerAccess) return false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        if (cameraManager != null)
        {
            cameraManager.SwitchToLevelSelector();
            return true;
        }
        OpenLevelSelect = true;
        SceneManager.LoadScene(MenuSceneName);
        return true;
    }

    /// <summary>True: the next 3D Menu load opens on the level select (progress) view.</summary>
    public static bool OpenLevelSelect;

    /// <summary>Returns the pending request once, then clears it.</summary>
    public static bool ConsumeOpenLevelSelect()
    {
        bool open = OpenLevelSelect;
        OpenLevelSelect = false;
        return open && HasCareerAccess;
    }

    /// <summary>True: the next 3D Menu load opens the settings board (settings live in the 3D Menu; there is no Settings scene).</summary>
    public static bool OpenSettings;

    /// <summary>Returns the pending settings request once, then clears it.</summary>
    public static bool ConsumeOpenSettings()
    {
        bool open = OpenSettings;
        OpenSettings = false;
        return open;
    }

    /// <summary>Menu-object name lists (settings / training "hide while open") plus the runtime story notes.</summary>
    public static string[] WithStoryButtons(string[] names)
    {
        if (names == null || names.Length == 0) return names;
        var list = new System.Collections.Generic.List<string>(names);
        if (!list.Contains(NewGameButtonName)) list.Add(NewGameButtonName);
        if (!list.Contains(LegacyNewGameButtonName)) list.Add(LegacyNewGameButtonName);
        if (!list.Contains(ContinueButtonName)) list.Add(ContinueButtonName);
        return list.ToArray();
    }
}
