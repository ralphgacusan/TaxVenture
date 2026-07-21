using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Minimal Main Menu controller — one Start Game button, loads the
/// gameplay scene. Per original brief: "Main Menu: Start Game."
///
/// CONNECTS WITH:
/// - Office_Level1 scene (or whatever your gameplay scene is named): loaded
///   via SceneManager on Start Game
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Office_Level1";

    /// <summary>Wired to the Start Game button.</summary>
    public void OnStartGamePressed()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
}