using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ProgressUI : MonoBehaviour
{
    [System.Serializable]
    public class LevelButtonEntry
    {
        public Button button;
        public Renderer note;
    }

    [Header("Scene Names")]
    [SerializeField] private string levelOneCutscene = "Level1_Character_Intro_Cutscene";
    [SerializeField] private string levelTwoCutscene = "Office_Level2";
    [SerializeField] private string mainMenuSceneName = "3D Menu";

    [Header("Progress Display")]
    [SerializeField] private TextMeshProUGUI totalExpText;
    [SerializeField] private TextMeshProUGUI totalReputationText;
    [SerializeField] private TextMeshProUGUI levelsCompletedText;

    [Header("Level Buttons — Order: Level 1 to Level 5")]
    [SerializeField]
    private LevelButtonEntry[] levels =
        new LevelButtonEntry[5];

    [Header("3D Note Materials")]
    [SerializeField] private Material lockedMaterial;
    [SerializeField] private Material unlockedMaterial;

    [Header("2D Note Sprites")]
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    private void Start()
    {
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (PlayerProgressManager.Instance == null)
        {
            Debug.LogError("[ProgressUI] PlayerProgressManager.Instance is NULL.");
            return;
        }

        PlayerProgressData progress =
            PlayerProgressManager.Instance.CurrentProgress;

        if (totalExpText != null)
            totalExpText.text = $"Total EXP: {progress.totalExp}";

        if (totalReputationText != null)
            totalReputationText.text =
                $"Total Reputation: {progress.totalReputation}";

        if (levelsCompletedText != null)
            levelsCompletedText.text =
                $"Levels Completed: {progress.levelsCompleted}";

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
                continue;

            bool isUnlocked = progress.IsLevelUnlocked(i + 1);

            SetLevelButtonState(
                levels[i].button,
                levels[i].note,
                isUnlocked
            );
        }
    }

    private void SetLevelButtonState(
        Button button, Renderer note, bool isUnlocked)
    {
        if (button != null)
        {
            button.interactable = isUnlocked;
            Image image = button.GetComponent<Image>();
            Sprite sprite = isUnlocked ? unlockedSprite : lockedSprite;
            if (image != null && sprite != null)
                image.sprite = sprite;
        }

        Material material =
            isUnlocked ? unlockedMaterial : lockedMaterial;

        if (note != null && material != null)
            note.sharedMaterial = material;
    }

    public void OnLevelOneGameplayButtonPressed()
    {
        PlaySelectSound();
        SceneManager.LoadScene(levelOneCutscene);
    }

    public void OnLevelTwoGameplayButtonPressed()
    {
        if (PlayerProgressManager.Instance == null)
        {
            Debug.LogError("[ProgressUI] Cannot check Level 2 unlock state.");
            return;
        }

        if (!PlayerProgressManager.Instance.CurrentProgress.IsLevelUnlocked(2))
        {
            Debug.LogWarning("[ProgressUI] Level 2 is locked. Ignoring press.");
            return;
        }

        PlaySelectSound();
        SceneManager.LoadScene(levelTwoCutscene);
    }

    public void OnBackToMainMenuButtonPressed()
    {
        PlaySelectSound();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void PlaySelectSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySelectSFX();
    }
}
