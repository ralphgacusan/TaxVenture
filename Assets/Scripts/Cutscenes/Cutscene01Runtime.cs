using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Runs the Level 1 intro cutscene scene "Cutscene2.0" (Career Chapter 1
/// START loads it). Same self-bootstrapping pattern as SceneFade /
/// OfficeHudRuntime: no scene edits and no serialized references; the scene's
/// PlayableDirectors, animation and audio are left exactly as authored.
///
/// - Fades in from black and, when the cutscene ends or Skip is tapped, fades
///   to black and loads Office_Level1 (the old Level1_Character_Intro_Cutscene
///   flow: 0.25 s fades, then SceneManager.LoadScene).
/// - The cutscene ends when the longest PlayableDirector stops. Without a
///   playable Timeline it ends after fallbackDurationSeconds.
/// - Reuses chapter subtitles, timed from the existing dialogue AudioTrack.
///   Authored scene text/audio/animation stay intact.
/// - Remembers a completed viewing; first viewing cannot be skipped.
/// DOES NOT: play, pause or rebind any director, or touch authored audio.
/// </summary>
public sealed class Cutscene01Runtime : MonoBehaviour
{
    public const string SceneName = "Cutscene2.0";
    public const string SubtitlePath = "Cutscenes/cutscene01_subtitles";

    private const float StartGraceSeconds = 2f;

    private CutsceneSubtitleTrack track;
    private PlayableDirector master;
    private PlayableDirector subtitleDirector;
    private ChapterCinematicScript subtitleScript;
    private ChapterCinematicPlayer.SubtitleView subtitles;
    private bool masterPlayed;
    private float elapsed;
    private bool leaving;
    private bool canSkip;

    private CanvasGroup black;
    private RectTransform safeRoot;
    private Rect appliedSafeArea;
    private Vector2Int appliedScreen;
    private GameObject skipButton;
    private List<Camera> shotCameras;
    private Camera backdropCamera;
    private int backdropMask;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeForLoadedScenes()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            EnsureForScene(SceneManager.GetSceneAt(i));
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureForScene(scene);

    private static void EnsureForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<Cutscene01Runtime>(true) != null) return;
        var host = new GameObject("Cutscene01 Runtime", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<Cutscene01Runtime>();
    }

    /// <summary>Reads the subtitle JSON; null when it is missing or invalid.</summary>
    public static CutsceneSubtitleTrack LoadTrack()
    {
        TextAsset asset = Resources.Load<TextAsset>(SubtitlePath);
        if (asset == null)
        {
            Debug.LogWarning($"[Cutscene01] Resources/{SubtitlePath}.json not found. Playing without subtitles.");
            return null;
        }
        try
        {
            return JsonUtility.FromJson<CutsceneSubtitleTrack>(asset.text);
        }
        catch (System.Exception error)
        {
            Debug.LogWarning($"[Cutscene01] Resources/{SubtitlePath}.json is not valid JSON ({error.Message}). Playing without subtitles.");
            return null;
        }
    }

    private void Awake()
    {
        track = LoadTrack();
        if (track == null) track = new CutsceneSubtitleTrack();
        if (track.lines == null) track.lines = new List<CutsceneSubtitleLine>();
        if (string.IsNullOrWhiteSpace(track.nextScene)) track.nextScene = "Office_Level1";

        PlayerProgressData progress = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null;
        bool forceOpening = MenuEntry.ConsumeUnskippableOpening();
        canSkip = !forceOpening && track.showSkip && progress != null && progress.HasSeenLevelOneIntro;
        BuildUi();
        EnsureEventSystem();
        FindMasterDirector();
        BindSubtitlesToAudio();
        StartCoroutine(Fade(1f, 0f, track.fadeInSeconds));
    }

    private void OnDestroy()
    {
        if (master != null) master.stopped -= HandleMasterStopped;
        subtitles?.Dispose();
    }

    // =========================================================
    // TIMELINE
    // =========================================================

    private void FindMasterDirector()
    {
        double longest = -1d;
        foreach (PlayableDirector director in FindObjectsByType<PlayableDirector>())
        {
            if (director == null || director.gameObject.scene != gameObject.scene || director.playableAsset == null) continue;
            if (director.duration > longest)
            {
                longest = director.duration;
                master = director;
            }
        }
        if (master == null)
        {
            Debug.LogWarning($"[Cutscene01] No PlayableDirector with a Timeline in {SceneName}. Ending after {track.fallbackDurationSeconds:0.#} s.");
            return;
        }
        master.stopped += HandleMasterStopped;
        Debug.Log($"[Cutscene01] Following director '{master.name}' ({master.duration:0.00} s).");
    }

    private void BindSubtitlesToAudio()
    {
        if (track.useTimelineClipTimes)
            foreach (PlayableDirector director in FindObjectsByType<PlayableDirector>())
            {
                if (director == null || director.gameObject.scene != gameObject.scene
                    || !(director.playableAsset is TimelineAsset timeline)) continue;
                var clips = new List<CutsceneAudioSpan>();
                foreach (TrackAsset output in timeline.GetOutputTracks())
                {
                    if (!(output is AudioTrack) || output.mutedInHierarchy
                        || (!string.IsNullOrEmpty(track.timelineTrack) && output.name != track.timelineTrack)) continue;
                    foreach (TimelineClip clip in output.GetClips())
                        if (clip.asset is AudioPlayableAsset audio && audio.clip != null)
                            clips.Add(new CutsceneAudioSpan(audio.clip.name, clip.start, clip.duration,
                                clip.clipIn, clip.timeScale, audio.clip.length));
                }
                if (!track.ApplyAudioClipTimes(clips)) continue;
                subtitleDirector = director;
                Debug.Log($"[Cutscene01] Subtitles follow dialogue audio in '{director.name}' ({track.lines.Count} cues).");
                break;
            }
        if (subtitleDirector == null)
            Debug.LogWarning("[Cutscene01] No matching dialogue Timeline; using the authored audio-timed subtitle fallback.");
        subtitleScript = track.SubtitlePresentation();
    }

    private void UpdateSubtitle()
    {
        double time = subtitleDirector != null ? subtitleDirector.time : master != null ? master.time : elapsed;
        ChapterCinematicCue cue = subtitleScript?.CueAt(time, "");
        subtitles?.Set(cue?.speaker, cue?.text, cue != null ? subtitleScript.CueAlpha(cue, time) : 0f);
    }

    private void HandleMasterStopped(PlayableDirector director)
    {
        if (masterPlayed) Finish();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (leaving) return;

        if (master != null)
        {
            if (master.state == PlayState.Playing) masterPlayed = true;
            if (masterPlayed)
            {
                bool atEnd = master.duration > 0d && master.time >= master.duration - 0.001d;
                if (atEnd)
                {
                    Finish();
                    return;
                }
            }
            else if (elapsed > StartGraceSeconds)
            {
                // The director never started: run on our own clock instead.
                master.stopped -= HandleMasterStopped;
                if (subtitleDirector == master) subtitleDirector = null;
                master = null;
            }
        }
        else if (elapsed >= track.fallbackDurationSeconds)
        {
            Finish();
            return;
        }

        UpdateSubtitle();
    }

    private void LateUpdate()
    {
        ApplySafeArea();
        HideCoveredBackdropCamera();
    }

    /// <summary>
    /// The scene's Main Camera sits under every shot camera, so it would draw the whole office
    /// each frame only to be painted over. While a shot covers it, it draws nothing. It stays
    /// enabled so Camera.main keeps working, and returns when no shot is active.
    /// </summary>
    private void HideCoveredBackdropCamera()
    {
        if (shotCameras == null) CollectCameras();
        if (backdropCamera == null) return;
        bool covered = false;
        for (int i = 0; i < shotCameras.Count && !covered; i++)
            covered = shotCameras[i] != null && shotCameras[i].isActiveAndEnabled;
        int mask = covered ? 0 : backdropMask;
        if (backdropCamera.cullingMask != mask) backdropCamera.cullingMask = mask;
    }

    private void CollectCameras()
    {
        shotCameras = new List<Camera>();
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
        foreach (Camera camera in cameras)
            if (camera.gameObject.scene == gameObject.scene && camera.CompareTag("MainCamera"))
            {
                backdropCamera = camera;
                backdropMask = camera.cullingMask;
                break;
            }
        if (backdropCamera == null) return;
        foreach (Camera camera in cameras)
            if (camera != backdropCamera && camera.gameObject.scene == gameObject.scene &&
                camera.targetTexture == null && camera.depth > backdropCamera.depth &&
                camera.rect == new Rect(0f, 0f, 1f, 1f) &&
                (camera.clearFlags == CameraClearFlags.Skybox || camera.clearFlags == CameraClearFlags.SolidColor))
                shotCameras.Add(camera);
    }

    // =========================================================
    // ENDING
    // =========================================================

    /// <summary>Skip button: same exit as the cutscene ending.</summary>
    public void Skip()
    {
        if (canSkip) Finish();
    }

    private void Finish()
    {
        if (leaving) return;
        leaving = true;
        subtitles?.Hide();
        if (PlayerProgressManager.Instance != null) PlayerProgressManager.Instance.MarkLevelOneIntroSeen();
        if (skipButton != null) skipButton.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        yield return Fade(black.alpha, 1f, track.fadeOutSeconds);
        Debug.Log($"[Cutscene01] Cutscene finished. Loading {track.nextScene}.");
        MobilePerformance.LoadScene(track.nextScene);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        black.blocksRaycasts = true;
        float t = 0f;
        black.alpha = from;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            black.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        black.alpha = to;
        black.blocksRaycasts = to > 0.01f;
    }

    // =========================================================
    // UI
    // =========================================================

    private void BuildUi()
    {
        gameObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        safeRoot = HudTheme.CreateRect("Safe Area", transform);
        HudTheme.Stretch(safeRoot);

        subtitles = new ChapterCinematicPlayer.SubtitleView(safeRoot);
        if (canSkip) BuildSkipButton();

        // Black overlay over everything, full screen (not inset).
        RectTransform fill = HudTheme.CreateRect("Black", transform);
        HudTheme.Stretch(fill);
        HudTheme.AddImage(fill, null, Color.black).raycastTarget = true;
        black = fill.gameObject.AddComponent<CanvasGroup>();
        black.alpha = 1f;
        black.blocksRaycasts = true;

        ApplySafeArea();
    }

    private void BuildSkipButton()
    {
        RectTransform frame = HudTheme.CreateRect("Skip", safeRoot);
        frame.anchorMin = frame.anchorMax = new Vector2(1f, 1f);
        frame.pivot = new Vector2(1f, 1f);
        frame.anchoredPosition = new Vector2(-32f, -32f);
        frame.sizeDelta = new Vector2(200f, 84f);
        RectTransform body = HudTheme.CreatePanel("Panel", frame);
        HudTheme.Stretch((RectTransform)body.parent);
        TextMeshProUGUI label = HudTheme.CreateLabel("Label", body, "Skip", 36f, HudTheme.BrassSoft, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch((RectTransform)label.transform, 8f);

        // A transparent hit area covering the whole button.
        Image hit = frame.gameObject.AddComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f);
        hit.raycastTarget = true;
        Button button = frame.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        button.onClick.AddListener(Skip);
        skipButton = frame.gameObject;
    }

    private void ApplySafeArea()
    {
        if (safeRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        var size = new Vector2Int(Screen.width, Screen.height);
        if (area == appliedSafeArea && size == appliedScreen) return;
        safeRoot.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
        safeRoot.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;
        appliedSafeArea = area;
        appliedScreen = size;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var host = new GameObject("EventSystem");
        host.AddComponent<EventSystem>();
        host.AddComponent<InputSystemUIInputModule>();
    }
}
