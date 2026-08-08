using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// "LEVEL COMPLETE / Return to Main Module" screen, shown once the FSM
/// enters LevelCompleteState.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    public static LevelCompleteUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private string mainModuleSceneName = "Progress";

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show() => panelRoot.SetActive(true);
    private void Hide() => panelRoot.SetActive(false);

    public void OnReturnToMainModulePressed()
    {
        SceneManager.LoadScene(mainModuleSceneName);
    }
}