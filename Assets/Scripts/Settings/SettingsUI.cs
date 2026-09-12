using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Back Button")]
    [SerializeField] private Button backButton;

    [Header("Audio Toggles")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("SettingsUI: AudioManager.Instance is NULL.");
            return;
        }

        // Start with the settings panel closed.
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("SettingsUI: Settings Panel is not assigned.");
        }

        // Setup the Back button.
        if (backButton != null)
        {
            backButton.onClick.AddListener(CloseSettings);
            backButton.onClick.AddListener(GoToMainMenu);
        }
        else
        {
            Debug.LogWarning("SettingsUI: Back Button is not assigned.");
        }

        // Refresh toggle states.
        RefreshUI();

        // Setup audio toggle listeners.
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
        }
    }

    private void OnDestroy()
    {
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
        }

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveListener(OnSFXToggleChanged);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(CloseSettings);
            backButton.onClick.RemoveListener(GoToMainMenu);
        }
    }

    // =========================================================
    // OPEN / CLOSE SETTINGS
    // =========================================================

    public void OpenSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("SettingsUI: Settings Panel is not assigned.");
            return;
        }

        settingsPanel.SetActive(true);

        // Make sure the toggles show the current saved state.
        RefreshUI();
    }

    public void CloseSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("SettingsUI: Settings Panel is not assigned.");
            return;
        }

        settingsPanel.SetActive(false);
    }

    // =========================================================
    // RETURN TO MAIN MENU
    // =========================================================

    public void GoToMainMenu()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectSFX();
        }

        SceneManager.LoadScene("MainMenu");
    }

    // =========================================================
    // UI REFRESH
    // =========================================================

    private void RefreshUI()
    {
        if (AudioManager.Instance == null)
            return;

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(
                AudioManager.Instance.IsMusicEnabled()
            );
        }

        if (sfxToggle != null)
        {
            sfxToggle.SetIsOnWithoutNotify(
                AudioManager.Instance.IsSFXEnabled()
            );
        }
    }

    // =========================================================
    // MUSIC
    // =========================================================

    private void OnMusicToggleChanged(bool enabled)
    {
        // Play select sound before changing the music setting.
        AudioManager.Instance.PlaySelectSFX();

        AudioManager.Instance.SetMusicEnabled(enabled);
    }

    // =========================================================
    // SFX
    // =========================================================

    private void OnSFXToggleChanged(bool enabled)
    {
        if (enabled)
        {
            // Enable SFX first so the click can be heard.
            AudioManager.Instance.SetSFXEnabled(true);

            AudioManager.Instance.PlaySelectSFX();
        }
        else
        {
            // SFX is still enabled, so the OFF click can be heard.
            AudioManager.Instance.PlaySelectSFX();

            // Disable SFX after the click.
            AudioManager.Instance.SetSFXEnabled(false);
        }
    }
}