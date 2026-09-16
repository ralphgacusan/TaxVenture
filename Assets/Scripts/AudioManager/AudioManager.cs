using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }


    // =========================================================
    // AUDIO SOURCES
    // =========================================================

    [Header("Audio Sources")]

    [SerializeField]
    private AudioSource musicSource;

    [SerializeField]
    private AudioSource sfxSource;


    // =========================================================
    // MUSIC
    // =========================================================

    [Header("Music")]

    [SerializeField]
    private AudioClip backgroundMusic;


    // =========================================================
    // SFX
    // =========================================================

    [Header("SFX")]

    [SerializeField]
    private AudioClip achievementSFX;

    [SerializeField]
    private AudioClip keyboardSFX;

    [SerializeField]
    private AudioClip mouseClickSelectSFX;

    [SerializeField]
    private AudioClip npcDialogueSFX;

    [SerializeField]
    private AudioClip paper2SFX;

    [SerializeField]
    private AudioClip paperSFX;

    [SerializeField]
    private AudioClip selectSFX;

    [SerializeField]
    private AudioClip footstepSFX;

    [SerializeField]
    [Range(0f, 1f)]
    private float footstepVolume = 0.3f;


    // =========================================================
    // SETTINGS
    // =========================================================

    private bool musicEnabled;

    private bool sfxEnabled;

    private const string MusicEnabledKey =
        "MusicEnabled";

    private const string SFXEnabledKey =
        "SFXEnabled";


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // Prevent duplicate AudioManagers.
        // -----------------------------------------------------

        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "[AudioManager] Duplicate AudioManager detected. " +
                "Destroying duplicate."
            );

            Destroy(gameObject);

            return;
        }


        Instance = this;

        DontDestroyOnLoad(gameObject);


        LoadSettings();

        SceneManager.sceneLoaded += OnSceneLoaded;


        Debug.Log(
            "[AudioManager] Initialized."
        );
    }


    private void Start()
    {
        SetupMusic();
    }


    // =========================================================
    // SCENE LOADED
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        SetupMusic();
    }


    // =========================================================
    // MUSIC
    // =========================================================

    private void SetupMusic()
    {
        if (musicSource == null)
        {
            Debug.LogWarning(
                "[AudioManager] Music AudioSource is not assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // Only play music in MainMenu and Progress.
        // -----------------------------------------------------

        if (!IsMusicScene())
        {
            musicSource.Stop();

            return;
        }


        musicSource.clip =
            backgroundMusic;

        musicSource.loop =
            true;

        musicSource.mute =
            !musicEnabled;


        if (musicEnabled &&
            backgroundMusic != null &&
            !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }


    private bool IsMusicScene()
    {
        string sceneName =
            SceneManager.GetActiveScene().name;

        return sceneName == "MainMenu" ||
               sceneName == "Progress" ||
               sceneName == "Settings";
    }


    public void SetMusicEnabled(
        bool enabled
    )
    {
        musicEnabled =
            enabled;


        PlayerPrefs.SetInt(
            MusicEnabledKey,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();


        if (musicSource == null)
            return;


        musicSource.mute =
            !enabled;


        if (enabled)
        {
            // -------------------------------------------------
            // Only start music if the current scene allows it.
            // -------------------------------------------------

            if (IsMusicScene() &&
                !musicSource.isPlaying &&
                backgroundMusic != null)
            {
                musicSource.Play();
            }
        }
        else
        {
            musicSource.Stop();
        }
    }


    public bool IsMusicEnabled()
    {
        return musicEnabled;
    }


    // =========================================================
    // GENERIC SFX
    // =========================================================

    private void PlaySFX(
        AudioClip clip
    )
    {
        if (!sfxEnabled)
            return;


        if (sfxSource == null)
        {
            Debug.LogWarning(
                "[AudioManager] " +
                "SFX AudioSource is not assigned."
            );

            return;
        }


        if (clip == null)
        {
            Debug.LogWarning(
                "[AudioManager] " +
                "SFX clip is not assigned."
            );

            return;
        }


        sfxSource.PlayOneShot(
            clip
        );
    }


    // =========================================================
    // ACHIEVEMENT
    // =========================================================

    public void PlayAchievementSFX()
    {
        PlaySFX(
            achievementSFX
        );
    }


    // =========================================================
    // KEYBOARD
    // =========================================================

    public void PlayKeyboardSFX()
    {
        PlaySFX(
            keyboardSFX
        );
    }


    // =========================================================
    // MOUSE CLICK
    // =========================================================

    public void PlayMouseClickSelectSFX()
    {
        PlaySFX(
            mouseClickSelectSFX
        );
    }


    // =========================================================
    // NPC DIALOGUE
    // =========================================================

    public void PlayNPCDialogueSFX()
    {
        PlaySFX(
            npcDialogueSFX
        );
    }


    // =========================================================
    // PAPER 2
    // =========================================================

    public void PlayPaper2SFX()
    {
        PlaySFX(
            paper2SFX
        );
    }


    // =========================================================
    // PAPER
    // =========================================================

    public void PlayPaperSFX()
    {
        PlaySFX(
            paperSFX
        );
    }


    // =========================================================
    // SELECT
    // =========================================================

    public void PlaySelectSFX()
    {
        PlaySFX(
            selectSFX
        );
    }


    // =========================================================
    // FOOTSTEP
    // =========================================================

    public void PlayFootstepSFX()
    {
        if (!sfxEnabled)
            return;

        if (sfxSource == null)
            return;

        if (footstepSFX == null)
            return;

        sfxSource.PlayOneShot(
            footstepSFX,
            footstepVolume
        );
    }

    // =========================================================
    // SFX SETTINGS
    // =========================================================

    public void SetSFXEnabled(
        bool enabled
    )
    {
        sfxEnabled =
            enabled;


        PlayerPrefs.SetInt(
            SFXEnabledKey,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();
    }


    public bool IsSFXEnabled()
    {
        return sfxEnabled;
    }


    // =========================================================
    // TEST
    // =========================================================

    public void TestSelectSFX()
    {
        PlaySelectSFX();
    }


    // =========================================================
    // LOAD SETTINGS
    // =========================================================

    private void LoadSettings()
    {
        musicEnabled =
            PlayerPrefs.GetInt(
                MusicEnabledKey,
                1
            ) == 1;


        sfxEnabled =
            PlayerPrefs.GetInt(
                SFXEnabledKey,
                1
            ) == 1;
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            Instance = null;
        }
    }
}