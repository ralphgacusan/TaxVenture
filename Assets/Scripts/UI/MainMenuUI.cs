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
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string progressSceneName = "Progress";

    /// <summary>Wired to the Start Game button.</summary>
    public void OnStartGamePressed()
    {
        SceneManager.LoadScene(progressSceneName);
    }
}