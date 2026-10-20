using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// How the 3D Menu scene should open next time it loads. The main menu and
/// the progress / level select and the settings board live in one scene ("3D Menu"); a finished
/// level asks it to open directly on the level select instead of the title
/// view. ToggleCamera reads (and clears) the request in Start.
/// </summary>
public static class MenuEntry
{
    public const string MenuSceneName = "3D Menu";

    /// <summary>The full menu and career board appear after the first Level 1 finish.</summary>
    public static bool HasCareerAccess => PlayerProgressManager.Instance != null &&
        PlayerProgressManager.Instance.CurrentProgress != null && PlayerProgressManager.Instance.CurrentProgress.HasFinishedFirstLevel;

    /// <summary>Play starts the first story directly until Level 1 has been finished once.</summary>
    public static void Play(ToggleCamera cameraManager)
    {
        if (SceneFade.BlocksInput || (cameraManager != null && cameraManager.IsTransitioning)) return;
        PlayerProgressData progress = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null;
        if (progress == null)
        {
            Debug.LogWarning("[MenuEntry] Progress is not ready; try Play again.");
            return;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        if (!progress.HasFinishedFirstLevel)
        {
            OpenLevelSelect = false;
            OpenSettings = false;
            SceneFade.FadeOutThenLoad(Cutscene01Runtime.SceneName);
        }
        else if (cameraManager != null) cameraManager.SwitchToLevelSelector();
        else
        {
            OpenLevelSelect = true;
            SceneManager.LoadScene(MenuSceneName);
        }
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
}
