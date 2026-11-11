using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Plays one fixed-length chapter cutscene (Documentation/ChapterCutscenes).
/// The minimal shared presentation layer of T01, reused by T02/T03:
///
/// - Owns a fixed presentation clock (ChapterCinematicSession, 15 s) and
///   drives its explicitly bound elements (shots, actors, props, sounds)
///   from it. No Timeline director, no scene scanning, no global time-scale changes.
/// - Builds a screen-space overlay: bottom-centre subtitles (speaker prefix,
///   translucent dark panel, two-line budget), the intro's upper-right Skip
///   button (same HudTheme construction as Cutscene01Runtime.BuildSkipButton,
///   available on every viewing) and the black fade curtain.
/// - Skip (button or Android Back) and the natural end finish once; Finished
///   is raised once with Completed/Skipped/Aborted. Pauses on application
///   suspension/focus loss and resumes at the same presentation time.
///
/// CONTRACT FOR THE CALLER (T04): optionally set PendingContext, subscribe to
/// Finished, load the scene. On Completed/Skipped continue (the screen holds
/// black); Aborted is not a watched result. With no subscriber (standalone
/// preview in the Editor) the scene simply holds black and logs.
///
/// DOES NOT: write saves, mark intro/chapter viewing, award trust or rewards,
/// create or deliver documents, or load any scene.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChapterCinematicPlayer : MonoBehaviour
{
    // =========================================================
    // CONTRACT
    // =========================================================

    /// <summary>Raised exactly once per playback (Completed, Skipped or Aborted).</summary>
    public static event Action<ChapterCinematicResult> Finished;

    /// <summary>The playing cutscene, or null.</summary>
    public static ChapterCinematicPlayer Current { get; private set; }

    /// <summary>True while a chapter cutscene presents (for callers that hold gameplay clocks or music).</summary>
    public static bool IsPlaying => Current != null && Current.session != null && !Current.session.Done;

    /// <summary>Set by the caller before loading; consumed once by the next player. Null = default context.</summary>
    public static ChapterCinematicContext PendingContext { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Finished = null;
        Current = null;
        PendingContext = null;
    }

    // =========================================================
    // AUTHORED BINDINGS
    // =========================================================

    [Header("Script")]
    [Tooltip("Timing data: id, 15 s duration, fades, shot starts and subtitle cues (Assets/Cutscenes/ChapterNN/*_script.json).")]
    [SerializeField] private TextAsset scriptAsset;

    [Header("Bindings (stable, no scanning)")]
    [SerializeField] private Camera cinematicCamera;
    [Tooltip("Camera shots in order; the one containing the current time drives the camera.")]
    [SerializeField] private ChapterCinematicShot[] shots = new ChapterCinematicShot[0];
    [Tooltip("Actors, props and sounds evaluated every frame.")]
    [SerializeField] private ChapterCinematicElement[] elements = new ChapterCinematicElement[0];

    [Header("Overlay")]
    [SerializeField] private float subtitleFontSize = 38f;
    [SerializeField] private float subtitleMaxWidth = 1180f;
    [SerializeField] private float subtitleBottomMargin = 56f;
    [SerializeField] private Vector2 subtitlePadding = new Vector2(28f, 18f);
    [SerializeField, Range(0.5f, 0.85f)] private float subtitlePanelAlpha = 0.7f;
    [SerializeField] private bool androidBackSkips = true;

    // =========================================================
    // STATE
    // =========================================================

    private ChapterCinematicScript script;
    private ChapterCinematicSession session;
    private ChapterCinematicContext context = ChapterCinematicContext.Default;
    private bool prepared;
    private bool released;
    private bool focusPaused;
    private bool suspendPaused;
    private bool appliedPause;

    private CanvasGroup curtain;
    private RectTransform safeRoot;
    private Rect appliedSafeArea;
    private Vector2Int appliedScreen;
    private GameObject skipButton;
    private CanvasGroup skipGroup;
    private SubtitleView subtitles;
    private EventSystem ownedEventSystem;
    private AudioListener ownListener;
    private ChapterCinematicVoiceCatalogue voiceCatalogue;
    private readonly Dictionary<ChapterCinematicCue, AudioClip> voiceClips = new Dictionary<ChapterCinematicCue, AudioClip>();
    private AudioSource voiceSource;
    private ChapterCinematicCue voiceCue;
    private const float VoiceVolume = 0.9f;

    public string Id => script != null ? script.id : "";
    public ChapterCinematicScript Script => script;
    public ChapterCinematicContext Context => context;
    public double PresentationTime => session != null ? session.Time : 0d;
    public bool IsFinished => session != null && session.Done;
    public Camera CinematicCamera => cinematicCamera;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        script = LoadScript(scriptAsset);
        if (script == null)
        {
            Debug.LogError($"[ChapterCinematic] {name}: no valid script asset. The cutscene finishes immediately.");
            script = new ChapterCinematicScript { id = "missing", duration = 0.01f, fadeInSeconds = 0f, fadeOutSeconds = 0f };
        }
        else
        {
            foreach (string problem in script.Validate())
                Debug.LogWarning($"[ChapterCinematic] {script.id}: {problem}");
        }

        context = PendingContext ?? ChapterCinematicContext.Default;
        PendingContext = null;
        session = new ChapterCinematicSession(script);
        if (Current != null && Current != this)
            Debug.LogWarning($"[ChapterCinematic] {script.id} started while {Current.Id} is still current.");
        Current = this;

        ResolveAudioListener();
        BuildOverlay();
        EnsureEventSystem();
        ApplyFrame();
    }

    private void Start()
    {
        PrepareElements();
        PrepareVoice();
        Evaluate();
        Debug.Log($"[ChapterCinematic] Playing {script.id} \"{script.title}\" ({script.duration:0.###} s, variant '{context.Variant}').");
    }

    private void Update()
    {
        if (released) return;
        SyncPause();

        if (!session.Finishing && androidBackSkips && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Skip();

        double step = ChapterCinematicVoiceRules.LimitStep(session.Time, Time.unscaledDeltaTime,
            voiceCue, voiceSource != null && voiceSource.isPlaying, VoiceEnabled(), session.Finishing, script.cueFadeSeconds);
        session.Advance(step);
        Evaluate();

        if (session.TakeFinished(out ChapterCinematicResult result))
            Deliver(result);
    }

    private void LateUpdate() => ApplySafeArea();

    private void OnApplicationFocus(bool hasFocus)
    {
        focusPaused = !hasFocus;
        SyncPause();
    }

    private void OnApplicationPause(bool paused)
    {
        suspendPaused = paused;
        SyncPause();
    }

    private void OnDisable()
    {
        if (session != null && session.Teardown() && session.TakeFinished(out ChapterCinematicResult result))
            Deliver(result);
        ReleaseElements();
    }

    private void OnDestroy()
    {
        // Torn down before the finish (scene unloaded, quit): Aborted, not a watched result
        // (a Skip whose fade was still running completes as Skipped).
        if (session != null && session.Teardown() && session.TakeFinished(out ChapterCinematicResult result))
            Deliver(result);
        ReleaseElements();
        subtitles?.Dispose();
        if (ownedEventSystem != null) Destroy(ownedEventSystem.gameObject);
        if (Current == this) Current = null;
    }

    // =========================================================
    // PUBLIC CONTROL
    // =========================================================

    /// <summary>Skip button / Android Back: the same continuation as the natural end, once.</summary>
    public void Skip()
    {
        if (session == null || !session.RequestSkip()) return;
        if (skipButton != null) skipButton.SetActive(false);
        Debug.Log($"[ChapterCinematic] {script.id} skipped at {session.FinishTime:0.00} s.");
    }

    /// <summary>Ends without a watched result (the caller is leaving). Delivers Aborted once.</summary>
    public void Abort()
    {
        if (session != null && session.Abort() && session.TakeFinished(out ChapterCinematicResult result))
            Deliver(result);
    }

    // =========================================================
    // CLOCK -> ELEMENTS
    // =========================================================

    private void PrepareElements()
    {
        if (prepared) return;
        prepared = true;
        if (shots != null)
            foreach (ChapterCinematicShot shot in shots)
                if (shot != null) shot.Prepare(this);
        if (elements != null)
            foreach (ChapterCinematicElement element in elements)
                if (element != null) element.Prepare(this);
    }

    private void Evaluate()
    {
        if (released) return;
        var frame = new ChapterCinematicFrame((float)session.Time, session.CurtainAlpha, AudioScale(), session.Paused);

        ChapterCinematicShot active = null;
        if (shots != null)
            foreach (ChapterCinematicShot shot in shots)
                if (shot != null && frame.Time >= shot.StartTime) active = shot;
        if (active != null) active.Evaluate(frame);

        if (elements != null)
            foreach (ChapterCinematicElement element in elements)
                if (element != null) element.Evaluate(frame);

        EvaluateVoice();
        ApplyFrame();
    }

    private bool VoiceEnabled() => AudioManager.Instance != null
        ? AudioManager.Instance.IsVoiceEnabled()
        : PlayerPrefs.GetInt("VoiceEnabled", 1) == 1;

    private void PrepareVoice()
    {
        voiceCatalogue = Resources.Load<ChapterCinematicVoiceCatalogue>("Cutscenes/Voice/" + script.id.Replace('.', '_'));
        if (voiceCatalogue == null) return;
        foreach (ChapterCinematicCue cue in script.cues)
        {
            if (!cue.AppliesTo(context.Variant) || string.IsNullOrEmpty(cue.voice)) continue;
            AudioClip clip = voiceCatalogue.Find(cue);
            if (clip == null || !ChapterCinematicVoiceRules.Fits(cue, clip.length))
            {
                if (Debug.isDebugBuild) Debug.LogWarning($"[ChapterCinematic] {cue.voice}: missing, stale or too long; using subtitles only.");
                continue;
            }
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                if (Debug.isDebugBuild) Debug.LogWarning($"[ChapterCinematic] {cue.voice}: audio unavailable; using subtitles only.");
                continue;
            }
            voiceClips.Add(cue, clip);
        }
        var host = new GameObject("Cutscene Speech");
        host.transform.SetParent(transform, false);
        voiceSource = host.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.priority = 32;
        voiceSource.reverbZoneMix = 0f;
        voiceSource.volume = VoiceVolume * AudioManager.VoiceGain;
    }

    private void EvaluateVoice()
    {
        if (voiceSource == null) return;
        bool enabled = VoiceEnabled();
        // Muting consumes the current take; unmuting never restarts it mid-sentence.
        if (!enabled) voiceSource.Stop();
        voiceSource.volume = VoiceVolume * AudioManager.VoiceGain * (session.Finishing ? 1f - session.CurtainAlpha : 1f);
        if (session.Finishing || session.Paused) return;

        ChapterCinematicCue cue = script.CueAt(session.Time, context.Variant);
        if (cue == voiceCue) return;
        voiceCue = cue;
        voiceSource.Stop();
        voiceSource.clip = null;
        if (!enabled || cue == null || !voiceClips.TryGetValue(cue, out AudioClip clip)) return;
        // Always play from the first word. The clock guard holds the shot if the
        // device starts late; seeking to presentation time would swallow words.
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    private float AudioScale()
    {
        bool effectsOn = AudioManager.Instance != null
            ? AudioManager.Instance.IsSFXEnabled()
            : PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
        if (!effectsOn) return 0f;
        return AudioManager.SFXGain * (session.Finishing && session.Outcome != ChapterCinematicOutcome.Completed
            ? 1f - session.CurtainAlpha
            : 1f);
    }

    private void SyncPause()
    {
        if (session == null) return;
        bool paused = focusPaused || suspendPaused;
        session.Paused = paused;
        if (paused == appliedPause) return;
        appliedPause = paused;
        if (voiceSource != null)
        {
            if (paused) voiceSource.Pause();
            else voiceSource.UnPause();
        }
        if (shots != null)
            foreach (ChapterCinematicShot shot in shots)
                if (shot != null) shot.SetPaused(paused);
        if (elements != null)
            foreach (ChapterCinematicElement element in elements)
                if (element != null) element.SetPaused(paused);
    }

    private void Deliver(ChapterCinematicResult result)
    {
        if (skipButton != null) skipButton.SetActive(false);
        subtitles?.Hide();
        ReleaseElements();
        if (curtain != null)
        {
            curtain.alpha = 1f;
            curtain.blocksRaycasts = true;
        }

        Action<ChapterCinematicResult> listeners = Finished;
        if (listeners == null)
        {
            Debug.Log($"[ChapterCinematic] {result} - standalone preview: holding the finished (black) state; nothing is loaded or saved.");
            return;
        }
        Debug.Log($"[ChapterCinematic] {result} - handing control to the caller.");
        try
        {
            listeners(result);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
    }

    private void ReleaseElements()
    {
        if (released) return;
        released = true;
        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = null;
        }
        voiceClips.Clear();
        voiceCatalogue = null;
        if (shots != null)
            foreach (ChapterCinematicShot shot in shots)
                if (shot != null) shot.Release();
        if (elements != null)
            foreach (ChapterCinematicElement element in elements)
                if (element != null) element.Release();
    }

    // =========================================================
    // OVERLAY
    // =========================================================

    private void ApplyFrame()
    {
        if (session == null) return;
        if (curtain != null)
        {
            float alpha = session.CurtainAlpha;
            curtain.alpha = alpha;
            // Opaque black may hold input; the clear steady-state curtain never swallows Skip taps.
            curtain.blocksRaycasts = alpha > 0.98f;
        }

        if (skipGroup != null && skipButton != null && skipButton.activeSelf)
        {
            bool available = session.SkipAvailable;
            skipGroup.alpha = available ? 1f : 0f;
            skipGroup.interactable = available;
            skipGroup.blocksRaycasts = available;
        }

        ChapterCinematicCue cue = session.Done ? null : script.CueAt(session.Time, context.Variant);
        subtitles?.Set(cue?.speaker, cue?.text, cue != null ? script.CueAlpha(cue, session.Time) : 0f);
    }

    private void BuildOverlay()
    {
        var host = new GameObject("Cinematic Overlay", typeof(RectTransform));
        host.transform.SetParent(transform, false);
        host.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        host.AddComponent<GraphicRaycaster>();

        safeRoot = HudTheme.CreateRect("Safe Area", host.transform);
        HudTheme.Stretch(safeRoot);

        subtitles = new SubtitleView(safeRoot, subtitleFontSize, subtitleMaxWidth, subtitleBottomMargin, subtitlePadding, subtitlePanelAlpha);
        BuildSkipButton();

        // Full-screen black curtain above everything (not inset by the safe area).
        RectTransform fill = HudTheme.CreateRect("Curtain", host.transform);
        HudTheme.Stretch(fill);
        HudTheme.AddImage(fill, null, Color.black).raycastTarget = true;
        curtain = fill.gameObject.AddComponent<CanvasGroup>();
        curtain.alpha = 1f;
        curtain.blocksRaycasts = true;

        ApplySafeArea();
    }

    /// <summary>
    /// The existing chapter subtitle renderer, also used by the Timeline intro.
    /// Presentation only: each owner supplies its own clock, cue text and opacity.
    /// </summary>
    public sealed class SubtitleView : IDisposable
    {
        private readonly RectTransform subtitlePanel;
        private readonly CanvasGroup subtitleGroup;
        private readonly TextMeshProUGUI subtitleLabel;
        private readonly float subtitleMaxWidth;
        private readonly Vector2 subtitlePadding;
        private string shownSpeaker, shownText;

        public SubtitleView(RectTransform safeRoot, float subtitleFontSize = 38f,
            float subtitleMaxWidth = 1180f, float subtitleBottomMargin = 56f,
            Vector2? padding = null, float subtitlePanelAlpha = 0.7f)
        {
            this.subtitleMaxWidth = subtitleMaxWidth;
            subtitlePadding = padding ?? new Vector2(28f, 18f);
            subtitlePanel = HudTheme.CreateRect("Subtitle", safeRoot);
            subtitlePanel.anchorMin = subtitlePanel.anchorMax = new Vector2(0.5f, 0f);
            subtitlePanel.pivot = new Vector2(0.5f, 0f);
            subtitlePanel.anchoredPosition = new Vector2(0f, subtitleBottomMargin);
            subtitlePanel.sizeDelta = new Vector2(subtitleMaxWidth, 120f);
            HudTheme.AddImage(subtitlePanel, HudTheme.Rounded, new Color(0f, 0f, 0f, subtitlePanelAlpha), 14f);
            subtitleGroup = subtitlePanel.gameObject.AddComponent<CanvasGroup>();
            subtitleGroup.alpha = 0f;
            subtitleGroup.interactable = false;
            subtitleGroup.blocksRaycasts = false;

            subtitleLabel = HudTheme.CreateLabel("Line", subtitlePanel, "", subtitleFontSize, HudTheme.TextPrimary, TextAlignmentOptions.Center);
            RectTransform rect = (RectTransform)subtitleLabel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(subtitlePadding.x, subtitlePadding.y);
            rect.offsetMax = new Vector2(-subtitlePadding.x, -subtitlePadding.y);
            // Full cues, wrapped to at most two lines; never auto-shrunk to unreadable sizes.
            subtitleLabel.enableAutoSizing = false;
            subtitleLabel.fontSize = subtitleFontSize;
            subtitleLabel.textWrappingMode = TextWrappingModes.Normal;
            subtitleLabel.overflowMode = TextOverflowModes.Overflow;
            subtitleLabel.richText = true;
            subtitleLabel.lineSpacing = 4f;
            subtitleLabel.outlineWidth = 0.12f;
            subtitleLabel.outlineColor = new Color32(0, 0, 0, 160);
        }

        public void Set(string speaker, string text, float alpha)
        {
            if (!string.IsNullOrEmpty(text) && (speaker != shownSpeaker || text != shownText))
            {
                shownSpeaker = speaker;
                shownText = text;
                string color = ColorUtility.ToHtmlStringRGB(HudTheme.BrassSoft);
                subtitleLabel.text = string.IsNullOrEmpty(speaker)
                    ? text
                    : $"<color=#{color}><b>{speaker}</b></color>  {text}";
                Vector2 preferred = subtitleLabel.GetPreferredValues(subtitleLabel.text, subtitleMaxWidth, 0f);
                float width = Mathf.Min(subtitleMaxWidth, preferred.x);
                Vector2 wrapped = subtitleLabel.GetPreferredValues(subtitleLabel.text, width + 1f, 0f);
                subtitlePanel.sizeDelta = new Vector2(width + subtitlePadding.x * 2f, wrapped.y + subtitlePadding.y * 2f);
            }
            subtitleGroup.alpha = string.IsNullOrEmpty(text) ? 0f : alpha;
        }

        public void Hide() => subtitleGroup.alpha = 0f;

        public void Dispose()
        {
            if (subtitleLabel != null && subtitleLabel.fontMaterial != null && subtitleLabel.fontMaterial != subtitleLabel.fontSharedMaterial)
                UnityEngine.Object.Destroy(subtitleLabel.fontMaterial);
        }
    }

    /// <summary>Same construction as Cutscene01Runtime.BuildSkipButton; shown on every viewing of chapter scenes.</summary>
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
        skipGroup = frame.gameObject.AddComponent<CanvasGroup>();
        skipGroup.alpha = 0f;
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

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        var host = new GameObject("EventSystem (Cinematic)");
        host.transform.SetParent(transform, false);
        ownedEventSystem = host.AddComponent<EventSystem>();
        host.AddComponent<InputSystemUIInputModule>();
    }

    /// <summary>Keeps exactly one listener: the scene camera's own, unless a caller's listener is already live.</summary>
    private void ResolveAudioListener()
    {
        if (cinematicCamera == null) return;
        ownListener = cinematicCamera.GetComponent<AudioListener>();
        if (ownListener == null) return;
        foreach (AudioListener other in FindObjectsByType<AudioListener>())
        {
            if (other == ownListener || !other.isActiveAndEnabled) continue;
            ownListener.enabled = false;
            Debug.Log($"[ChapterCinematic] Using the existing AudioListener on '{other.name}'.");
            return;
        }
    }

    // =========================================================
    // DATA
    // =========================================================

    /// <summary>Parses a script asset; null when missing or invalid JSON.</summary>
    public static ChapterCinematicScript LoadScript(TextAsset asset)
    {
        if (asset == null) return null;
        try
        {
            ChapterCinematicScript parsed = JsonUtility.FromJson<ChapterCinematicScript>(asset.text);
            if (parsed != null && parsed.cues == null) parsed.cues = new List<ChapterCinematicCue>();
            return parsed;
        }
        catch (Exception error)
        {
            Debug.LogError($"[ChapterCinematic] {asset.name} is not valid JSON ({error.Message}).");
            return null;
        }
    }
}
