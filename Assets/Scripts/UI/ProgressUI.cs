using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// Progress / Level Select screen. Displays the player's permanent
/// progression (Total EXP, Total Reputation) and gates level buttons
/// based on PlayerProgressData.levelsCompleted.
///
/// LOCK RULE:
/// Level N is unlocked once levelsCompleted >= N - 1.
/// Level 1 is always unlocked. Level 2 unlocks after 1 level completed.
///
/// VISUALS:
/// Each level button's own Image swaps between a locked sprite
/// (notepad_white) and an unlocked sprite (notepad_yellow) based on
/// unlock state — no lock icon overlay, no CanvasGroup dimming.
/// </summary>
public class ProgressUI : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string levelOneCutscene = "Level1_Character_Intro_Cutscene";
    [SerializeField] private string levelTwoCutscene = "Office_Level2";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Progress Display")]
    [SerializeField] private TextMeshProUGUI totalExpText;
    [SerializeField] private TextMeshProUGUI totalReputationText;
    [SerializeField] private TextMeshProUGUI levelsCompletedText;

    [Header("Level Buttons")]
    [SerializeField] private Button levelOneButton;
    [SerializeField] private Button levelTwoButton;

    [Header("Lock/Unlock Sprites")]
    [SerializeField] private Sprite lockedSprite;   // notepad_white
    [SerializeField] private Sprite unlockedSprite; // notepad_yellow

    private void Start()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (PlayerProgressManager.Instance == null)
        {
            Debug.LogError("[ProgressUI] PlayerProgressManager.Instance is NULL.");
            return;
        }

        PlayerProgressData progress = PlayerProgressManager.Instance.CurrentProgress;

        if (totalExpText != null)
            totalExpText.text = $"Total EXP: {progress.totalExp}";

        if (totalReputationText != null)
            totalReputationText.text = $"Total Reputation: {progress.totalReputation}";

        if (levelsCompletedText != null)
            levelsCompletedText.text = $"Levels Completed: {progress.levelsCompleted}";

        // Level 1 is always unlocked.
        SetLevelButtonState(levelOneButton, isUnlocked: true);

        // Level 2 unlocks once at least 1 level has been completed.
        bool levelTwoUnlocked = progress.levelsCompleted >= 1;
        SetLevelButtonState(levelTwoButton, levelTwoUnlocked);
    }

    private void SetLevelButtonState(Button button, bool isUnlocked)
    {
        if (button == null) return;

        button.interactable = isUnlocked;

        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = isUnlocked ? unlockedSprite : lockedSprite;
        }
    }

    public void OnLevelOneGameplayButtonPressed()
    {
        // Play button click sound before changing scenes.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectSFX();
        }

        SceneManager.LoadScene(levelOneCutscene);
    }

    public void OnLevelTwoGameplayButtonPressed()
    {
        if (PlayerProgressManager.Instance != null &&
            PlayerProgressManager.Instance.CurrentProgress.levelsCompleted < 1)
        {
            Debug.LogWarning("[ProgressUI] Level 2 is locked. Ignoring press.");
            return;
        }

        // Play button click sound before changing scenes.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectSFX();
        }

        SceneManager.LoadScene(levelTwoCutscene);
    }

    public void OnBackToMainMenuButtonPressed()
    {
        // Play button click sound before changing scenes.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectSFX();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}