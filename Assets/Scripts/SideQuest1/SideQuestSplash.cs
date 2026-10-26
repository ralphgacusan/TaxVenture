using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The title card that opens Side Quest 1, right after Arthur's corkboard
/// talk: the board dims behind a cinematic card with the quest's badge
/// medal, "SIDE QUEST 1", the big title (letters popping in), a short
/// description and the instruction at the bottom, and a pulsing
/// "TAP TO CONTINUE". Tap anywhere (or Space/Enter) to start sorting.
///
/// While it shows, the board can't be touched, the clock waits and the
/// guide holds its lessons (IsShowing). Text: town.json "sideQuestSplash"
/// (TownContentLoader), with the built-in defaults below.
/// </summary>
public sealed class SideQuestSplash : MonoBehaviour
{
    private const string TargetSceneName = "Office_Level1";

    [Serializable]
    public class SplashContent
    {
        public string kicker = "SIDE QUEST 1";
        public string title = "Know Your Taxpayer";
        public string description = "Four townsfolk, four different stories. Pin each ID onto the corner of Arthur's corkboard where it belongs.";
        public string instruction = "Pin all four IDs  ·  Tap one to turn it over  ·  The board checks itself";
        public string button = "TAP TO CONTINUE";
    }

    /// <summary>Text for the card (town.json may replace it).</summary>
    public static SplashContent Content = new SplashContent();

    /// <summary>True while the title card is on screen.</summary>
    public static bool IsShowing { get; private set; }

    private static SideQuestSplash instance;

    private SplashContent instanceContent;
    private Action continueCallback;
    private bool releaseBoardOnClose;

    /// <summary>True when a title card will open the side quest (the board waits for it).</summary>
    public static bool Active => instance != null && instance.isActiveAndEnabled;

    // Backdrop: solid with a hint of the board behind it.
    private const float ShadeAlpha = 0.92f;
    private const float FadeIn = 0.5f;
    private const float FadeOut = 0.5f;

    private GameObject host;
    private CanvasGroup group;
    private Image shadeImage;
    private CanvasGroup content;
    private RectTransform medal;
    private RectTransform kicker;
    private TextMeshProUGUI title;
    private RectTransform divider;
    private CanvasGroup lower;
    private TextMeshProUGUI tapLabel;
    private RectTransform glow;
    private float age;
    private bool closing;
    private float closeAge;
    private bool pending;
    private float showAt;

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
        if (scene.name != TargetSceneName) return;
        GetOrCreateForScene(scene);
    }

    /// <summary>Returns this scene's splash runner, creating one when needed.</summary>
    public static SideQuestSplash GetOrCreateForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            SideQuestSplash existing = root.GetComponentInChildren<SideQuestSplash>(true);
            if (existing != null) return existing;
        }
        var runner = new GameObject("Side Quest Splash");
        SceneManager.MoveGameObjectToScene(runner, scene);
        return runner.AddComponent<SideQuestSplash>();
    }

    private void OnEnable()
    {
        instance = this;
        TownEvents.OnTutorialIntroCompleted += HandleSideQuestStarted;
    }

    private void OnDisable()
    {
        if (instance == this) instance = null;
        TownEvents.OnTutorialIntroCompleted -= HandleSideQuestStarted;
        IsShowing = false;
        if (releaseBoardOnClose) ReleaseBoard();
        ClearRequest();
    }

    /// <summary>The card is gone: the board fills in and the profiles fly in.</summary>
    private static void ReleaseBoard()
    {
        CorkboardSideQuestManager board = FindAnyObjectByType<CorkboardSideQuestManager>(FindObjectsInactive.Include);
        if (board != null) board.ReleaseReveal();
    }

    private void OnDestroy()
    {
        IsShowing = false;
        if (host != null) Destroy(host);
        ClearRequest();
    }

    // Arthur's talk ended and the board opened: show the card a moment later
    // (IsShowing is set now, so nothing else starts in between).
    private void HandleSideQuestStarted()
    {
        StartRequest(Content, null, releaseBoard: true);
    }

    /// <summary>Shows this same cinematic card with scene-specific text and a one-shot continue callback.</summary>
    public bool Show(SplashContent content, Action onContinue)
    {
        if (host != null || pending || IsShowing) return false;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!enabled) enabled = true;
        StartRequest(content, onContinue, releaseBoard: false);
        return true;
    }

    private void StartRequest(SplashContent content, Action onContinue, bool releaseBoard)
    {
        if (host != null || pending || IsShowing) return;
        instance = this;
        age = closeAge = 0f;
        closing = false;
        instanceContent = content;
        continueCallback = onContinue;
        releaseBoardOnClose = releaseBoard;
        pending = true;
        IsShowing = true;
        showAt = Time.unscaledTime + 0.35f;
    }

    /// <summary>Dismisses a card owned by a scene object that is being torn down.</summary>
    public void Cancel()
    {
        if (host != null) Destroy(host);
        host = null;
        pending = false;
        closing = false;
        IsShowing = false;
        if (releaseBoardOnClose) ReleaseBoard();
        ClearRequest();
    }

    private void ClearRequest()
    {
        instanceContent = null;
        continueCallback = null;
        releaseBoardOnClose = false;
    }

    private void Update()
    {
        if (pending && Time.unscaledTime >= showAt)
        {
            pending = false;
            Build();
        }
        if (host == null) return;

        float dt = Time.unscaledDeltaTime;
        if (closing)
        {
            closeAge += dt;
            float contentOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(closeAge / (FadeOut * 0.6f)));
            float shadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((closeAge - FadeOut * 0.25f) / (FadeOut * 0.75f)));
            content.alpha = 1f - contentOut;
            shadeImage.color = new Color(0.02f, 0.04f, 0.09f, ShadeAlpha * (1f - shadeOut));
            title.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.05f, contentOut);
            if (shadeOut >= 1f)
            {
                Destroy(host);
                host = null;
                IsShowing = false;
                if (releaseBoardOnClose) ReleaseBoard();
                Action callback = continueCallback;
                ClearRequest();
                callback?.Invoke();
            }
            return;
        }

        age += dt;
        float shadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / FadeIn));
        shadeImage.color = new Color(0.02f, 0.04f, 0.09f, ShadeAlpha * shadeIn);
        content.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - FadeIn * 0.3f) / (FadeIn * 0.8f)));

        float m = HudTheme.EaseOutBack(Mathf.Clamp01((age - 0.1f) / 0.45f));
        medal.localScale = Vector3.one * m;
        medal.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 1.6f) * 4f);

        float k1 = HudTheme.EaseOutCubic(Mathf.Clamp01((age - 0.25f) / 0.4f));
        kicker.anchoredPosition = new Vector2(0f, Mathf.Lerp(250f, 215f, k1));
        kicker.GetComponent<CanvasGroup>().alpha = k1;

        AnimateTitle();

        float d = HudTheme.EaseOutCubic(Mathf.Clamp01((age - 1.0f) / 0.5f));
        divider.sizeDelta = new Vector2(Mathf.Lerp(0f, 900f, d), 4f);
        lower.alpha = Mathf.Clamp01((age - 1.15f) / 0.5f);

        float pulse = 0.55f + 0.45f * Mathf.Sin(age * 4f);
        tapLabel.alpha = Mathf.Clamp01((age - 1.6f) / 0.4f) * pulse;
        glow.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(age * 2f));

        if (age > 0.9f && Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            Close();
    }

    private void Close()
    {
        if (closing || age < 0.9f) return;
        closing = true;
        closeAge = 0f;
        MobileInputGuard.SuppressWorldTapsThisFrame();
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void Build()
    {
        SplashContent c = instanceContent ?? Content ?? new SplashContent();
        host = new GameObject("Side Quest Splash Canvas", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, gameObject.scene);
        host.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;   // above every HUD, window and card
        CanvasScaler scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        host.AddComponent<GraphicRaycaster>();
        group = host.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        var canvasRoot = (RectTransform)host.transform;

        // A nearly solid backdrop; tapping anywhere continues.
        RectTransform shade = HudTheme.CreateRect("Shade", canvasRoot);
        HudTheme.Stretch(shade);
        shadeImage = HudTheme.AddImage(shade, null, new Color(0.02f, 0.04f, 0.09f, 0f));
        shadeImage.raycastTarget = true;
        shade.gameObject.AddComponent<GuideTapCatcher>().Tapped = Close;

        // Everything else fades in after the backdrop.
        RectTransform root = HudTheme.CreateRect("Content", canvasRoot);
        HudTheme.Stretch(root);
        content = root.gameObject.AddComponent<CanvasGroup>();
        content.alpha = 0f;
        content.blocksRaycasts = false;

        // Top and bottom cinematic bands.
        Band(root, true);
        Band(root, false);

        glow = HudTheme.CreateRect("Title Glow", root);
        glow.sizeDelta = new Vector2(1500f, 520f);
        glow.anchoredPosition = new Vector2(0f, 70f);
        HudTheme.AddImage(glow, GuideArt.Glow, new Color32(80, 170, 255, 70));

        // Badge medal for this side quest.
        medal = HudTheme.CreateRect("Medal", root);
        medal.sizeDelta = new Vector2(130f, 130f);
        medal.anchoredPosition = new Vector2(0f, 330f);
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        Sprite medalSprite = HudBadgeArt.For(progression != null ? progression.Score.Rules : null, BadgeTier.Gold);
        Image medalImage = HudTheme.AddImage(medal, medalSprite, Color.white);
        medalImage.preserveAspect = true;
        medal.localScale = Vector3.zero;

        kicker = HudTheme.CreateRect("Kicker", root);
        kicker.sizeDelta = new Vector2(900f, 60f);
        kicker.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
        TextMeshProUGUI kickerText = HudTheme.CreateLabel("Text", kicker, c.kicker, 44f, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
        HudTheme.Stretch(kickerText.rectTransform);
        kickerText.characterSpacing = 10f;
        kickerText.outlineWidth = 0.22f;
        kickerText.outlineColor = new Color32(18, 70, 130, 255);

        title = HudTheme.CreateLabel("Title", root, c.title, 128f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        title.rectTransform.sizeDelta = new Vector2(1700f, 300f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 70f);
        title.textWrappingMode = TextWrappingModes.Normal;
        title.enableAutoSizing = true;
        title.fontSizeMin = 70f;
        title.fontSizeMax = 128f;
        title.outlineWidth = 0.24f;
        title.outlineColor = new Color32(26, 96, 176, 255);
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(Color.white, Color.white,
            new Color32(200, 232, 255, 255), new Color32(200, 232, 255, 255));

        divider = HudTheme.CreateRect("Divider", root);
        divider.anchoredPosition = new Vector2(0f, -110f);
        divider.sizeDelta = new Vector2(0f, 4f);
        HudTheme.AddImage(divider, null, HudTheme.Brass);

        RectTransform lowerRect = HudTheme.CreateRect("Lower", root);
        lowerRect.sizeDelta = new Vector2(1400f, 260f);
        lowerRect.anchoredPosition = new Vector2(0f, -250f);
        lower = lowerRect.gameObject.AddComponent<CanvasGroup>();
        lower.alpha = 0f;

        TextMeshProUGUI description = HudTheme.CreateLabel("Description", lowerRect, c.description, 34f,
            new Color32(245, 240, 226, 255), TextAlignmentOptions.Center);
        description.textWrappingMode = TextWrappingModes.Normal;
        description.fontSizeMin = 24f;
        RectTransform dr = description.rectTransform;
        dr.anchorMin = new Vector2(0f, 0.45f);
        dr.anchorMax = new Vector2(1f, 1f);
        dr.offsetMin = dr.offsetMax = Vector2.zero;

        TextMeshProUGUI instruction = HudTheme.CreateLabel("Instruction", lowerRect, c.instruction, 26f,
            HudTheme.BrassSoft, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
        instruction.characterSpacing = 4f;
        RectTransform ir = instruction.rectTransform;
        ir.anchorMin = new Vector2(0f, 0.12f);
        ir.anchorMax = new Vector2(1f, 0.42f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;

        tapLabel = HudTheme.CreateLabel("Tap", root, c.button, 24f, Color.white, TextAlignmentOptions.Center,
            FontStyles.Bold | FontStyles.UpperCase);
        tapLabel.rectTransform.anchorMin = tapLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        tapLabel.rectTransform.sizeDelta = new Vector2(700f, 40f);
        tapLabel.rectTransform.anchoredPosition = new Vector2(0f, 50f);
        tapLabel.characterSpacing = 12f;
        tapLabel.alpha = 0f;

        age = 0f;
        closing = false;
    }

    private static void Band(RectTransform root, bool top)
    {
        RectTransform band = HudTheme.CreateRect(top ? "Top Band" : "Bottom Band", root);
        band.anchorMin = new Vector2(0f, top ? 1f : 0f);
        band.anchorMax = new Vector2(1f, top ? 1f : 0f);
        band.pivot = new Vector2(0.5f, top ? 1f : 0f);
        band.sizeDelta = new Vector2(0f, 70f);
        HudTheme.AddImage(band, null, new Color(0f, 0f, 0f, 0.55f));
    }

    /// <summary>Each letter of the title pops in after the previous one.</summary>
    private void AnimateTitle()
    {
        title.ForceMeshUpdate();
        TMP_TextInfo info = title.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;
            float k = Mathf.Clamp01((age - 0.45f - i * 0.04f) / 0.35f);
            float s = HudTheme.EaseOutBack(k);
            Vector3[] v = info.meshInfo[ch.materialReferenceIndex].vertices;
            int vi = ch.vertexIndex;
            Vector3 center = (v[vi] + v[vi + 2]) * 0.5f;
            for (int j = 0; j < 4; j++)
                v[vi + j] = center + (v[vi + j] - center) * s + new Vector3(0f, (1f - k) * -24f, 0f);
        }
        title.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
