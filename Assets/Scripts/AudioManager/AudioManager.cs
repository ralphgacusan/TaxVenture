using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MusicEnabledKey = "MusicEnabled";
    private const string SFXEnabledKey = "SFXEnabled";
    private const float MusicVolume = 0.035f;
    private const float AmbienceVolume = 0.45f;
    private const float FamilyDebounce = 0.08f;
    private const float TransitionSpeed = 0.8f;
    private static readonly AudioClip[] NoClips = new AudioClip[0];

    private AudioLibrary library;
    private readonly Dictionary<Button, UnityAction> wiredButtons = new Dictionary<Button, UnityAction>();
    private readonly Dictionary<string, AudioClip[]> familyCache = new Dictionary<string, AudioClip[]>();
    private readonly Dictionary<string, int> familyCursors = new Dictionary<string, int>();
    private readonly Dictionary<string, float> familyLastPlayed = new Dictionary<string, float>();

    private float nextButtonScan;
    private string pendingInteraction;
    private int feedbackFrame = -1;
    private AudioSource ambienceSource;
    private bool musicEnabled;
    private bool sfxEnabled;
    private bool isOutdoor;
    private bool ambienceContextActive;
    private bool completionMusicRequested;
    private bool musicChangePending;
    private AudioClip pendingMusicClip;
    private AudioClip[] activeMusicPlaylist = new AudioClip[0];
    private int musicPlaylistIndex;
    private string musicContextKey = string.Empty;
    private float musicTargetVolume;
    private bool ambienceChangePending;
    private AudioClip pendingAmbienceClip;
    private float ambienceTargetVolume;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambienceSourceOverride;
    [SerializeField] private AudioClip backgroundMusic;

    [Header("SFX Fallbacks")]
    [SerializeField] private AudioClip achievementSFX;
    [SerializeField] private AudioClip keyboardSFX;
    [SerializeField] private AudioClip mouseClickSelectSFX;
    [SerializeField] private AudioClip paper2SFX;
    [SerializeField] private AudioClip paperSFX;
    [SerializeField] private AudioClip selectSFX;
    [SerializeField] private AudioClip footstepSFX;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAudio()
    {
        if (Instance == null) new GameObject("AudioManager").AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (backgroundMusic != null && IsMenuScene(SceneManager.GetActiveScene().name))
            {
                Instance.backgroundMusic = backgroundMusic;
                Instance.SetupMusic();
            }
            Debug.LogWarning("[AudioManager] Duplicate AudioManager detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null || sfxSource == musicSource) sfxSource = gameObject.AddComponent<AudioSource>();
        if (ambienceSourceOverride != null) ambienceSource = ambienceSourceOverride;
        else ambienceSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
        musicSource.volume = MusicVolume;
        musicSource.mute = !musicEnabled;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.mute = !sfxEnabled;

        ambienceSource.playOnAwake = false;
        ambienceSource.loop = true;
        ambienceSource.spatialBlend = 0f;
        ambienceSource.volume = AmbienceVolume;
        ambienceSource.mute = !sfxEnabled;


        library = Resources.Load<AudioLibrary>("TaxVentureAudioLibrary");
        if (library != null)
        {
            achievementSFX = library.Find("sfx_reward_achievement_01");
            keyboardSFX = library.Find("sfx_keyboard_type_01");
            mouseClickSelectSFX = library.Find("sfx_ui_click_01");
            paper2SFX = library.Find("sfx_page_turn_01");
            paperSFX = library.Find("sfx_paper_rustle_01");
            selectSFX = library.Find("sfx_ui_select_01");
            footstepSFX = library.Find("sfx_footstep_office_01");
        }

        ApplySceneDefaults(SceneManager.GetActiveScene().name);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[AudioManager] Initialized.");
    }

    private void Start()
    {
        SetupMusic();
        SetupAmbience();
        WireButtons();
    }

    private void Update()
    {
        UpdateMusic();
        UpdateAmbience();

        if (Time.unscaledTime < nextButtonScan) return;
        nextButtonScan = Time.unscaledTime + 0.25f;
        WireButtons();
    }

    private void LateUpdate()
    {
        if (pendingInteraction != null && feedbackFrame != Time.frameCount)
            PlayEffect(pendingInteraction, 0.6f);
        pendingInteraction = null;
    }

    private void WireButtons()
    {
        var removed = new List<Button>();
        foreach (var entry in wiredButtons)
            if (entry.Key == null) removed.Add(entry.Key);
        foreach (Button button in removed) wiredButtons.Remove(button);
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (wiredButtons.ContainsKey(button)) continue;
            UnityAction click = () => QueueInteractionSFX(button.name);
            wiredButtons.Add(button, click);
            button.onClick.AddListener(click);
        }
    }

    // Queued feedback fills silent interaction paths; an explicit action cue wins.
    public void PlayInteractionSFX(Component target)
    {
        if (target != null) QueueInteractionSFX(target.GetType().Name);
    }

    public void QueueInteractionSFX(string targetKind)
    {
        if (!sfxEnabled) return;
        string kind = (targetKind ?? string.Empty).ToLowerInvariant();
        string clip = "sfx_ui_click_01";
        bool document = kind.Contains("notes") || kind.Contains("folder") || kind.Contains("profile") ||
                        kind.Contains("annotat") || kind.Contains("book") || kind.Contains("binder") || kind.Contains("paper");
        if (document)
            clip = kind.Contains("close") || kind.Contains("return") || kind.Contains("place") || kind.Contains("drop")
                ? "document_close" : "document_open";
        else if (kind.Contains("continue") || kind.Contains("next")) clip = "sfx_ui_click_01";
        else if (kind.Contains("back") || kind.Contains("return")) clip = "sfx_ui_back_01";
        else if (kind.Contains("close") || kind.Contains("cancel")) clip = "sfx_ui_panel_close_01";
        else if (kind.Contains("npc") || kind.Contains("auditor") || kind.Contains("receptionist") || kind.Contains("colleague") || kind.Contains("client") || kind.Contains("rosa")) clip = "sfx_ui_select_01";
        else if (kind.Contains("stamp")) clip = "sfx_stamp_pickup_01";
        else if (kind.Contains("value") || kind.Contains("field"))
            clip = kind.Contains("place") || kind.Contains("drop") ? "sfx_paper_place_01" : "sfx_paper_pickup_01";
        else if (kind.Contains("door")) clip = "sfx_door_open_01";
        else if (kind.Contains("settings") || kind.Contains("open")) clip = "sfx_ui_panel_open_01";
        else if (kind.Contains("select") || kind.Contains("desk") || kind.Contains("chair")) clip = "sfx_ui_select_01";
        // A late generic Button listener must not overwrite a specific world cue.
        if (pendingInteraction == null || clip != "sfx_ui_click_01") pendingInteraction = clip;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        completionMusicRequested = false;
        ApplySceneDefaults(scene.name);
        SetupMusic();
        SetupAmbience();
        WireButtons();
    }

    private void ApplySceneDefaults(string sceneName)
    {
        if (sceneName == "Office_Level1")
        {
            ambienceContextActive = true;
            isOutdoor = true;
        }
        else if (sceneName == "Office_Level2")
        {
            ambienceContextActive = true;
            isOutdoor = false;
        }
        else
        {
            ambienceContextActive = false;
        }
    }

    public void PlayEffect(string clipName, float volume = 1f)
    {
        if (string.IsNullOrEmpty(clipName)) return;
        PlayFamily(clipName, volume);
    }

    /// <summary>Uses the named recording exactly, retaining family-level duplicate protection.</summary>
    public void PlayExactEffect(string clipName, float volume = 1f)
    {
        if (!string.IsNullOrEmpty(clipName)) PlayFamily(clipName, volume, exact: true);
    }

    public void PlayDocumentSFX(bool closing = false) => PlayFamily(closing ? "document_close" : "document_open", 0.6f);

    public void PlayAchievementSFX() => PlayFamily("sfx_reward_achievement_01", 1f, achievementSFX);
    public void PlayKeyboardSFX() => PlayFamily("sfx_keyboard_type_01", 1f, keyboardSFX);
    public void PlayMouseClickSelectSFX() => PlayFamily("sfx_ui_click_01", 1f, mouseClickSelectSFX);
    public void PlayPaper2SFX() => PlayFamily("sfx_page_turn_01", 1f, paper2SFX);
    public void PlayPaperSFX() => PlayFamily("sfx_paper_rustle_01", 1f, paperSFX);
    public void PlaySelectSFX() => PlayFamily("sfx_ui_select_01", 1f, selectSFX);

    private AudioClip PlayFamily(string requestedName, float volume = 1f, AudioClip fallback = null, bool exact = false)
    {
        if (!sfxEnabled || sfxSource == null) return null;
        if (requestedName.IndexOf("gib", StringComparison.OrdinalIgnoreCase) >= 0 ||
            requestedName.StartsWith("npc dialogue", StringComparison.OrdinalIgnoreCase)) return null;

        string family = FamilyName(requestedName);
        if (familyLastPlayed.TryGetValue(family, out float last) && Time.unscaledTime - last < FamilyDebounce)
            return null;

        AudioClip[] variants = exact
            ? (library != null && library.Find(requestedName) != null ? new[] { library.Find(requestedName) } : NoClips)
            : GetFamily(family, requestedName);
        if (variants.Length == 0 && fallback != null) variants = new[] { fallback };
        if (variants.Length == 0)
        {
            Debug.LogWarning("[AudioManager] SFX clip is not assigned: " + requestedName);
            return null;
        }

        int index = familyCursors.TryGetValue(family, out int previous) ? (previous + 1) % variants.Length : 0;
        familyCursors[family] = index;
        AudioClip clip = variants[index];
        familyLastPlayed[family] = Time.unscaledTime;
        sfxSource.PlayOneShot(clip, volume);
        // Autonomous feedback must not consume a tap's own tactile cue.
        if (!family.StartsWith("sfx_footstep_", StringComparison.Ordinal) &&
            !family.StartsWith("sfx_reward_", StringComparison.Ordinal) &&
            !family.StartsWith("sfx_objective_", StringComparison.Ordinal) &&
            !family.StartsWith("sfx_deadline_", StringComparison.Ordinal) &&
            !family.StartsWith("sfx_review_cutoff", StringComparison.Ordinal) &&
            !family.StartsWith("sfx_hint_received", StringComparison.Ordinal) &&
            !family.StartsWith("sfx_profile_collected", StringComparison.Ordinal))
            feedbackFrame = Time.frameCount;
        return clip;
    }

    private AudioClip[] GetFamily(string family, string requestedName)
    {
        if (!familyCache.TryGetValue(family, out AudioClip[] variants))
        {
            if (library != null && (family == "document_open" || family == "document_close"))
            {
                string[] names = family == "document_open"
                    ? new[] { "sfx_paper_pickup_01", "sfx_paper_rustle_01", "sfx_page_turn_01", "sfx_folder_open_01" }
                    : new[] { "sfx_paper_place_01", "sfx_paper_rustle_01", "sfx_page_turn_01", "sfx_folder_close_01" };
                var papers = new List<AudioClip>();
                foreach (string name in names)
                    if (library.Find(name) != null) papers.Add(library.Find(name));
                variants = papers.ToArray();
            }
            else variants = library != null ? library.FindFamily(requestedName) : NoClips;
            familyCache[family] = variants;
        }
        return variants;
    }

    private static string FamilyName(string clipName)
    {
        int underscore = clipName.LastIndexOf('_');
        if (underscore >= 0 && clipName.Length - underscore == 3 &&
            char.IsDigit(clipName[underscore + 1]) && char.IsDigit(clipName[underscore + 2]))
            return clipName.Substring(0, underscore);
        return clipName;
    }

    // Kept for serialized legacy UnityEvents; dialogue voices are disabled.
    public void PlayDialogueSFX(bool female = false, float duration = 0.65f) { }
    public void PlayNPCDialogueSFX() { }
    public void StopDialogueSFX() { }

    public void PlayFootstepSFX()
    {
        string name = isOutdoor ? "sfx_footstep_pavement_01" : "sfx_footstep_office_01";
        PlayFamily(name, footstepVolume, footstepSFX);
    }

    public void SetOutdoor(bool outdoor)
    {
        isOutdoor = outdoor;
        ambienceContextActive = true;
        SetupAmbience();
        SetupMusic();
    }

    public void SetCompletionMusic(bool enabled)
    {
        completionMusicRequested = enabled;
        SetupMusic();
    }

    private void SetupMusic()
    {
        if (musicSource == null) return;
        musicSource.mute = !musicEnabled;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Cutscene1.0" || sceneName == "Cutscene2.0")
        {
            activeMusicPlaylist = new AudioClip[0];
            musicContextKey = "cutscene";
            musicChangePending = false;
            pendingMusicClip = null;
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = MusicVolume;
            musicTargetVolume = 0f;
            return;
        }

        AudioClip[] playlist = SelectMusicPlaylist(sceneName, out string contextKey);
        if (contextKey != musicContextKey || !SamePlaylist(activeMusicPlaylist, playlist))
        {
            musicContextKey = contextKey;
            activeMusicPlaylist = playlist;
            int currentIndex = FindIndex(playlist, musicSource.clip);
            musicPlaylistIndex = currentIndex >= 0 ? currentIndex : 0;
        }

        AudioClip desired = activeMusicPlaylist.Length == 0
            ? null
            : activeMusicPlaylist[musicPlaylistIndex % activeMusicPlaylist.Length];
        RequestMusic(desired);
    }

    private AudioClip[] SelectMusicPlaylist(string sceneName, out string contextKey)
    {
        if (library == null)
        {
            contextKey = "none";
            return new AudioClip[0];
        }

        if (completionMusicRequested && HasClips(library.completionMusic))
        {
            contextKey = "completion";
            return library.completionMusic;
        }

        if (IsMenuScene(sceneName))
        {
            contextKey = "menu";
            AudioClip[] legacyMenuMusic = backgroundMusic != null ? new[] { backgroundMusic } : NoClips;
            return FirstAvailable(library.menuMusic, legacyMenuMusic, library.gameplayMusic);
        }

        if (isOutdoor && HasClips(library.plazaMusic))
        {
            contextKey = "plaza";
            return library.plazaMusic;
        }

        if (!isOutdoor && HasClips(library.officeMusic))
        {
            contextKey = "office";
            return library.officeMusic;
        }

        contextKey = isOutdoor ? "plaza-default" : "office-default";
        return FirstAvailable(library.gameplayMusic, library.officeMusic, library.plazaMusic);
    }

    private void RequestMusic(AudioClip clip)
    {
        if (musicSource.clip == clip)
        {
            musicChangePending = false;
            pendingMusicClip = null;
            musicTargetVolume = musicEnabled ? MusicVolume : 0f;
            if (clip != null && musicEnabled && !musicSource.isPlaying)
            {
                musicSource.loop = activeMusicPlaylist.Length <= 1;
                musicSource.volume = MusicVolume;
                musicSource.Play();
            }
            return;
        }

        pendingMusicClip = clip;
        musicChangePending = true;
        musicTargetVolume = 0f;
        if (!musicSource.isPlaying) FinishMusicChange();
    }

    private void UpdateMusic()
    {
        if (musicSource == null) return;

        if (musicChangePending)
        {
            MoveVolume(musicSource, 0f);
            if (!musicSource.isPlaying || musicSource.volume <= 0.001f) FinishMusicChange();
        }
        else if (musicEnabled && activeMusicPlaylist.Length > 1 && musicSource.clip != null && !musicSource.isPlaying)
        {
            musicPlaylistIndex = (musicPlaylistIndex + 1) % activeMusicPlaylist.Length;
            RequestMusic(activeMusicPlaylist[musicPlaylistIndex]);
        }

        musicTargetVolume = musicEnabled && musicSource.clip != null
            ? MusicVolume
            : 0f;
        if (!musicChangePending) MoveVolume(musicSource, musicTargetVolume);
    }

    private void FinishMusicChange()
    {
        musicSource.Stop();
        musicSource.clip = pendingMusicClip;
        musicSource.loop = activeMusicPlaylist.Length <= 1;
        musicSource.volume = 0f;
        musicChangePending = false;
        pendingMusicClip = null;
        if (musicSource.clip != null && musicEnabled)
        {
            musicSource.Play();
            musicTargetVolume = MusicVolume;
        }
    }

    private void SetupAmbience()
    {
        if (ambienceSource == null) return;
        ambienceSource.mute = !sfxEnabled;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Cutscene1.0" || sceneName == "Cutscene2.0")
        {
            ambienceChangePending = false;
            pendingAmbienceClip = null;
            ambienceSource.Stop();
            ambienceSource.clip = null;
            ambienceSource.volume = AmbienceVolume;
            ambienceTargetVolume = 0f;
            return;
        }

        AudioClip desired = null;
        if (ambienceContextActive && library != null)
        {
            AudioClip[] clips = isOutdoor ? library.outdoorAmbience : library.officeAmbience;
            if (HasClips(clips)) desired = clips[0];
        }

        RequestAmbience(desired);
    }

    private void RequestAmbience(AudioClip clip)
    {
        if (ambienceSource.clip == clip)
        {
            ambienceChangePending = false;
            pendingAmbienceClip = null;
            ambienceTargetVolume = clip != null ? AmbienceVolume : 0f;
            if (clip != null && !ambienceSource.isPlaying)
            {
                ambienceSource.loop = true;
                ambienceSource.volume = AmbienceVolume;
                ambienceSource.Play();
            }
            return;
        }

        pendingAmbienceClip = clip;
        ambienceChangePending = true;
        ambienceTargetVolume = 0f;
        if (!ambienceSource.isPlaying) FinishAmbienceChange();
    }

    private void UpdateAmbience()
    {
        if (ambienceSource == null) return;
        if (ambienceChangePending)
        {
            MoveVolume(ambienceSource, 0f);
            if (!ambienceSource.isPlaying || ambienceSource.volume <= 0.001f) FinishAmbienceChange();
        }
        if (!ambienceChangePending)
        {
            ambienceTargetVolume = ambienceSource.clip != null ? AmbienceVolume : 0f;
            MoveVolume(ambienceSource, ambienceTargetVolume);
        }
    }

    private void FinishAmbienceChange()
    {
        ambienceSource.Stop();
        ambienceSource.clip = pendingAmbienceClip;
        ambienceSource.loop = true;
        ambienceSource.volume = 0f;
        ambienceChangePending = false;
        pendingAmbienceClip = null;
        if (ambienceSource.clip != null)
        {
            ambienceSource.Play();
            ambienceTargetVolume = AmbienceVolume;
        }
    }

    private static void MoveVolume(AudioSource source, float target)
    {
        float step = TransitionSpeed * Time.unscaledDeltaTime;
        if (source.volume < target) source.volume = Math.Min(source.volume + step, target);
        else if (source.volume > target) source.volume = Math.Max(source.volume - step, target);
    }

    private static AudioClip[] FirstAvailable(params AudioClip[][] choices)
    {
        foreach (AudioClip[] clips in choices)
            if (HasClips(clips)) return clips;
        return new AudioClip[0];
    }

    private static bool HasClips(AudioClip[] clips)
    {
        if (clips != null)
            foreach (AudioClip clip in clips)
                if (clip != null) return true;
        return false;
    }

    private static bool SamePlaylist(AudioClip[] first, AudioClip[] second)
    {
        if (first == second) return true;
        if (first == null || second == null || first.Length != second.Length) return false;
        for (int i = 0; i < first.Length; i++)
            if (first[i] != second[i]) return false;
        return true;
    }

    private static int FindIndex(AudioClip[] clips, AudioClip target)
    {
        if (clips != null)
            for (int i = 0; i < clips.Length; i++)
                if (clips[i] == target) return i;
        return -1;
    }

    private static bool IsMenuScene(string sceneName)
    {
        return sceneName == "3D Menu" || sceneName == "MainMenu" || sceneName == "Progress" || sceneName == "Settings";
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        if (musicSource == null) return;
        musicSource.mute = !enabled;
        if (enabled) SetupMusic();
        else musicTargetVolume = 0f;
    }

    public bool IsMusicEnabled() => musicEnabled;

    public void SetSFXEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        if (sfxSource != null)
        {
            sfxSource.mute = !enabled;
            if (!enabled) sfxSource.Stop();
        }
        if (!enabled) pendingInteraction = null;
        if (ambienceSource != null) ambienceSource.mute = !enabled;

        PlayerPrefs.SetInt(SFXEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsSFXEnabled() => sfxEnabled;
    public void TestSelectSFX() => PlaySelectSFX();

    private void LoadSettings()
    {
        musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt(SFXEnabledKey, 1) == 1;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        foreach (var entry in wiredButtons)
            if (entry.Key != null) entry.Key.onClick.RemoveListener(entry.Value);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }
}
