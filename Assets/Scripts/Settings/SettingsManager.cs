using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string MusicEnabledKey = "MusicEnabled";
    private const string SFXEnabledKey = "SFXEnabled";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool GetMusicEnabled()
    {
        return PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
    }

    public bool GetSFXEnabled()
    {
        return PlayerPrefs.GetInt(SFXEnabledKey, 1) == 1;
    }

    public void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicEnabled(enabled);
        }
    }

    public void SetSFXEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(SFXEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXEnabled(enabled);
        }
    }
}