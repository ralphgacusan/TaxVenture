using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Controls the Main Menu buttons.
///
/// CONNECTS WITH:
/// - Play starts Level 1 directly until its first finish, then opens the career board
/// - SettingsBoard3D: Settings opens the 3D settings board in the 3D Menu.
///   From any other scene it loads the 3D Menu with the board open
///   (MenuEntry.OpenSettings); there is no separate Settings scene.
///   Gameplay scenes with InGameSettings open the same board in place.
/// - AudioManager: plays button click SFX
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    /// <summary>
    /// Wired to the Start Game button.
    /// </summary>
    public void OnStartGamePressed()
    {
        MenuEntry.Play(FindAnyObjectByType<ToggleCamera>());
    }

    /// <summary>
    /// Wired to the Settings button.
    /// </summary>
    public void OnSettingsPressed()
    {
        // In a gameplay scene: the in-game settings board (pauses the game).
        if (InGameSettings.TryOpen()) return;

        // In the 3D Menu: drop the settings board in front of the main menu (it plays the click).
        SettingsBoard3D board = FindAnyObjectByType<SettingsBoard3D>();
        if (board != null)
        {
            board.Open();
            return;
        }

        // Anywhere else: go to the 3D Menu and open settings there.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectSFX();
        }

        MenuEntry.OpenSettings = true;
        SceneManager.LoadScene(MenuEntry.MenuSceneName);
    }
}
