using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Minimal Main Menu controller — one Start Game button, loads the
/// progress scene. Per original brief: "Main Menu: Start Game."
///
/// CONNECTS WITH:
/// - Progress scene (or whatever your progress scene is named): loaded
///   via SceneManager on Start Game
/// </summary>
public class ProgressUI : MonoBehaviour
{
    [SerializeField] private string levelOneCutscene = "Level1_Character_Intro_Cutscene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";


    /// <summary>Wired to the Start Game button.</summary>
    public void OnLevelOneGameplayButtonPressed()
    {
        SceneManager.LoadScene(levelOneCutscene);
    }

    /// <summary>Wired to the Main Menu button.</summary>
    public void OnBackToMainMenuButtonPressed()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}