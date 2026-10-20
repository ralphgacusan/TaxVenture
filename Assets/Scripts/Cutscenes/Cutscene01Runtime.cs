using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
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
/// - Keeps authored scene text; creates no subtitle or title overlay.
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
    private bool masterPlayed;
    private float elapsed;
    private bool leaving;
    private bool canSkip;

    private CanvasGroup black;
    private RectTransform safeRoot;
    private Rect appliedSafeArea;
    private Vector2Int appliedScreen;
    private GameObject skipButton;

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
        track.lines.Clear(); // Cutscene 2.0 uses its authored visuals, with no added subtitles.
        if (string.IsNullOrWhiteSpace(track.nextScene)) track.nextScene = "Office_Level1";

        PlayerProgressData progress = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null;
        canSkip = track.showSkip && progress != null && progress.HasSeenLevelOneIntro;
        BuildUi();
        EnsureEventSystem();
        FindMasterDirector();
        StartCoroutine(Fade(1f, 0f, track.fadeInSeconds));
    }

    private void OnDestroy()
    {
        if (master != null) master.stopped -= HandleMasterStopped;
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
                if (master.state != PlayState.Playing || atEnd)
                {
                    Finish();
                    return;
                }
            }
            else if (elapsed > StartGraceSeconds)
            {
                // The director never started: run on our own clock instead.
                master.stopped -= HandleMasterStopped;
                master = null;
            }
        }
        else if (elapsed >= track.fallbackDurationSeconds)
        {
            Finish();
            return;
        }

    }

    private void LateUpdate() => ApplySafeArea();

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
        if (PlayerProgressManager.Instance != null) PlayerProgressManager.Instance.MarkLevelOneIntroSeen();
        if (skipButton != null) skipButton.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        yield return Fade(black.alpha, 1f, track.fadeOutSeconds);
        Debug.Log($"[Cutscene01] Cutscene finished. Loading {track.nextScene}.");
        SceneManager.LoadScene(track.nextScene);
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
