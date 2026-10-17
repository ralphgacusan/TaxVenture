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
/// Runs the Level 1 intro cutscene scene "Cutscene1.0" (Career Chapter 1
/// START loads it). Same self-bootstrapping pattern as SceneFade /
/// OfficeHudRuntime: no scene edits and no serialized references; the scene's
/// PlayableDirectors, animation and audio are left exactly as authored.
///
/// - Fades in from black and, when the cutscene ends or Skip is tapped, fades
///   to black and loads Office_Level1 (the old Level1_Character_Intro_Cutscene
///   flow: 0.25 s fades, then SceneManager.LoadScene).
/// - The cutscene ends when the longest PlayableDirector stops. Without a
///   playable Timeline it ends after fallbackDurationSeconds.
/// - Shows subtitles from Resources/Cutscenes/cutscene01_subtitles.json at the
///   bottom of the safe area, timed to the Timeline clips when they can be
///   read, otherwise to the JSON times.
///
/// DOES NOT: play, pause or rebind any director, touch audio, or save progress.
/// </summary>
public sealed class Cutscene01Runtime : MonoBehaviour
{
    public const string SceneName = "Cutscene1.0";
    public const string SubtitlePath = "Cutscenes/cutscene01_subtitles";

    private const float SubtitleFadeSeconds = 0.2f;
    private const float StartGraceSeconds = 2f;

    private CutsceneSubtitleTrack track;
    private PlayableDirector master;
    private bool masterPlayed;
    private bool usingTimelineClock;
    private float elapsed;
    private bool leaving;

    private CanvasGroup black;
    private RectTransform safeRoot;
    private Rect appliedSafeArea;
    private Vector2Int appliedScreen;
    private CanvasGroup subtitleGroup;
    private TextMeshProUGUI subtitleLabel;
    private CanvasGroup titleGroup;
    private TextMeshProUGUI titleHeading;
    private TextMeshProUGUI titleCaption;
    private GameObject skipButton;
    private CutsceneSubtitleLine shownLine;
    private CutsceneSubtitleLine pendingLine;
    private bool hasPending;

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

        BuildUi();
        EnsureEventSystem();
        FindMasterDirector();
        ResolveClipTimes();
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

    private void ResolveClipTimes()
    {
        if (!track.useTimelineClipTimes || master == null) return;
        var timeline = master.playableAsset as TimelineAsset;
        if (timeline == null) return;

        TrackAsset numbered = null;
        int mostClips = -1;
        foreach (TrackAsset candidate in timeline.GetOutputTracks())
        {
            if (candidate == null) continue;
            int count = 0;
            foreach (TimelineClip _ in candidate.GetClips()) count++;
            bool named = !string.IsNullOrWhiteSpace(track.timelineTrack) && candidate.name == track.timelineTrack;
            if (named) { numbered = candidate; break; }
            if (string.IsNullOrWhiteSpace(track.timelineTrack) && count > mostClips)
            {
                mostClips = count;
                numbered = candidate;
            }
        }
        if (numbered == null)
        {
            Debug.LogWarning($"[Cutscene01] Timeline track '{track.timelineTrack}' not found. Using the JSON subtitle times.");
            return;
        }

        var clips = new List<TimelineClip>(numbered.GetClips());
        clips.Sort((a, b) => a.start.CompareTo(b.start));
        var starts = new List<double>(clips.Count);
        var ends = new List<double>(clips.Count);
        var report = new System.Text.StringBuilder();
        for (int i = 0; i < clips.Count; i++)
        {
            starts.Add(clips[i].start);
            ends.Add(clips[i].end);
            report.Append($"\n  clip {i + 1}: {clips[i].start:0.00} - {clips[i].end:0.00}  ({clips[i].displayName})");
        }
        if (track.ApplyClipTimes(starts, ends))
            Debug.Log($"[Cutscene01] Subtitles timed to track '{numbered.name}':{report}");
        else
            Debug.LogWarning($"[Cutscene01] Track '{numbered.name}' has {clips.Count} clips but the subtitles name clip {track.HighestClip}. Using the JSON subtitle times.{report}");
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

        double time = elapsed;
        if (master != null)
        {
            if (master.state == PlayState.Playing) masterPlayed = true;
            usingTimelineClock = masterPlayed;
            if (masterPlayed)
            {
                time = master.time;
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

        UpdateSubtitle(usingTimelineClock || master == null ? track.LineAt(time) : null);
    }

    private void LateUpdate() => ApplySafeArea();

    // =========================================================
    // SUBTITLES
    // =========================================================

    private void UpdateSubtitle(CutsceneSubtitleLine wanted)
    {
        float step = Time.unscaledDeltaTime / SubtitleFadeSeconds;
        if (wanted != shownLine && !hasPending)
        {
            pendingLine = wanted;
            hasPending = true;
        }
        else if (hasPending && wanted != pendingLine)
        {
            pendingLine = wanted;
        }

        CanvasGroup current = shownLine != null && shownLine.title ? titleGroup : subtitleGroup;
        if (hasPending)
        {
            if (shownLine != null && current.alpha > 0f)
            {
                current.alpha = Mathf.MoveTowards(current.alpha, 0f, step);
                return;
            }
            Show(pendingLine);
            hasPending = false;
        }

        if (shownLine != null)
        {
            CanvasGroup group = shownLine.title ? titleGroup : subtitleGroup;
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, step);
        }
    }

    private void Show(CutsceneSubtitleLine line)
    {
        shownLine = line;
        subtitleGroup.alpha = 0f;
        titleGroup.alpha = 0f;
        subtitleGroup.gameObject.SetActive(line != null && !line.title);
        titleGroup.gameObject.SetActive(line != null && line.title);
        if (line == null) return;

        if (line.title)
        {
            string text = line.text ?? "";
            int split = text.IndexOf('\n');
            titleHeading.text = split < 0 ? text : text.Substring(0, split).Trim();
            titleCaption.text = split < 0 ? "" : text.Substring(split + 1).Trim();
        }
        else
        {
            subtitleLabel.text = line.text;
        }
    }

    // =========================================================
    // ENDING
    // =========================================================

    /// <summary>Skip button: same exit as the cutscene ending.</summary>
    public void Skip() => Finish();

    private void Finish()
    {
        if (leaving) return;
        leaving = true;
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

        // Subtitle: brass-edged navy panel, bottom centre of the safe area.
        RectTransform subtitleFrame = HudTheme.CreateRect("Subtitle", safeRoot);
        subtitleFrame.anchorMin = new Vector2(0.5f, 0f);
        subtitleFrame.anchorMax = new Vector2(0.5f, 0f);
        subtitleFrame.pivot = new Vector2(0.5f, 0f);
        subtitleFrame.anchoredPosition = new Vector2(0f, 48f);
        subtitleFrame.sizeDelta = new Vector2(1360f, 150f);
        subtitleGroup = subtitleFrame.gameObject.AddComponent<CanvasGroup>();
        subtitleGroup.blocksRaycasts = false;
        subtitleGroup.interactable = false;
        RectTransform body = HudTheme.CreatePanel("Panel", subtitleFrame, 14f);
        HudTheme.Stretch((RectTransform)body.parent);
        subtitleLabel = HudTheme.CreateLabel("Text", body, "", 44f, HudTheme.TextPrimary, TextAlignmentOptions.Center);
        HudTheme.Stretch((RectTransform)subtitleLabel.transform, 18f);
        subtitleLabel.textWrappingMode = TextWrappingModes.Normal;
        subtitleLabel.fontSizeMin = 30f;
        subtitleFrame.gameObject.SetActive(false);

        // Title card: large centred heading with a caption, no panel.
        RectTransform titleRect = HudTheme.CreateRect("Title Card", safeRoot);
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(1500f, 300f);
        titleGroup = titleRect.gameObject.AddComponent<CanvasGroup>();
        titleGroup.blocksRaycasts = false;
        titleGroup.interactable = false;
        titleHeading = HudTheme.CreateLabel("Heading", titleRect, "", 120f, HudTheme.BrassSoft, TextAlignmentOptions.Center, FontStyles.Bold);
        PlaceBand((RectTransform)titleHeading.transform, 0.42f, 1f);
        titleCaption = HudTheme.CreateLabel("Caption", titleRect, "", 52f, HudTheme.TextPrimary, TextAlignmentOptions.Center);
        PlaceBand((RectTransform)titleCaption.transform, 0f, 0.4f);
        foreach (TextMeshProUGUI label in new[] { titleHeading, titleCaption })
        {
            Shadow shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = HudTheme.ShadowColor;
            shadow.effectDistance = new Vector2(3f, -4f);
        }
        titleRect.gameObject.SetActive(false);

        if (track.showSkip) BuildSkipButton();

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

    private static void PlaceBand(RectTransform rect, float bottom, float top)
    {
        rect.anchorMin = new Vector2(0f, bottom);
        rect.anchorMax = new Vector2(1f, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
