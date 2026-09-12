
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Controls the Main Menu buttons.
///
/// CONNECTS WITH:
/// - Progress scene: loaded when Start Game is pressed
/// - Settings scene: loaded when Settings is pressed
/// - AudioManager: plays button click SFX
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string progressSceneName = "Progress";
    [SerializeField] private string settingsSceneName = "Settings";

    /// <summary>
    /// Wired to the Start Game button.
    /// </summary>
    public void OnStartGamePressed()
    {
        // Play button click sound before changing scenes.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectSFX();
        }

        SceneManager.LoadScene(progressSceneName);
    }

    /// <summary>
    /// Wired to the Settings button.
    /// </summary>
    public void OnSettingsPressed()
    {
        // Play button click sound before changing scenes.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectSFX();
        }

        SceneManager.LoadScene(settingsSceneName);
    }
}

