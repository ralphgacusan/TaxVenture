using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>What one reward screen shows. Every value comes from an existing result; nothing is calculated here.</summary>
public sealed class RewardScreenData
{
    public string Title;
    public string Subtitle;

    public int Exp, MaxExp;
    public int Hearts, MaxHearts;
    /// <summary>Passing line drawn on the bars (0 = none).</summary>
    public int PassingPercent;

    // Hero (first that applies): audit findings, case verdict, level status, single big medal.
    public readonly List<string> Issues = new List<string>();
    public string IssuesTitle = "NEEDS ANOTHER LOOK";

    public bool HasVerdict;
    public bool VerdictCorrect;
    public string VerdictText;
    public bool Flawless;

    public bool HasStatus;
    public bool StatusGood;
    public string StatusText;
    public string StatusDetail;

    public readonly List<Sprite> Medals = new List<Sprite>();
    public string MedalLabel;

    public sealed class RewardTileData
    {
        public string Label;
        public Sprite Icon;
        public Color Accent;
        public int Earned;
        public string BalanceLabel;
        public int Balance;
        public int BalanceMax;
    }

    public readonly List<RewardTileData> RewardTiles = new List<RewardTileData>();
    public readonly List<KeyValuePair<string, string>> Facts = new List<KeyValuePair<string, string>>();
    public string Summary;
    public string Footer;
    /// <summary>Text of the screen's own button (null keeps the scene's label).</summary>
    public string ButtonLabel;

    /// <summary>
    /// A chapter's closing card (2026-10-26): on the right 60% of the screen over
    /// the client scene (light backdrop), drawn up to LargeScale bigger when that
    /// area has room, and it waits for the finale scene like the Level Complete card.
    /// </summary>
    public bool Large;
    /// <summary>&gt; 0: the button presses itself this long after the entrance (a strip drains on it).</summary>
    public float AutoContinueSeconds;
    /// <summary>Colour of the light rays and glow behind the hero (tier / verdict colour).</summary>
    public Color Accent = new Color32(241, 194, 108, 255);

    public void Fact(string label, string value) => Facts.Add(new KeyValuePair<string, string>(label, value));

    public void RewardTile(string label, Sprite icon, Color accent, int earned, string balanceLabel, int balance, int balanceMax = 0)
    {
        if (earned <= 0) return;
        RewardTiles.Add(new RewardTileData
        {
            Label = label,
            Icon = icon,
            Accent = accent,
            Earned = earned,
            BalanceLabel = balanceLabel,
            Balance = balance,
            BalanceMax = balanceMax
        });
    }
}

/// <summary>
/// PURPOSE:
/// Reward / result cards for Office_Level1 (Side Quest Complete, Case
/// Results, Level Complete, and the Auditor's findings). Built at runtime on
/// the scene's own panel:
/// - only the card shows: every other UI (joystick, minimap, icons, prompt,
///   HUD, other canvases) is hidden and put back exactly on close, and the
///   player cannot move or look around while it is open;
/// - the card sizes itself to its content (no empty gaps) and is scaled to
///   fit the screen at any aspect; the scene's button moves inside it,
///   bottom centre;
/// - brass ribbon banner, dividers, a hero (medal / verdict / pass banner /
///   findings list) with turning light rays, glow and sparkles, an EXP bar
///   that fills with a counting value (passing tick), hearts that pop in
///   and then stay put, fact chips, summary and footer.
///
/// The screens keep their serialized fields, button callbacks and saving:
/// they still write their own (now hidden) texts and pass the same values
/// here. Nothing is calculated here. Other scenes keep the plain panels.
/// </summary>
public sealed class HudRewardScreen : MonoBehaviour
{
    private const float ContentWidth = 980f;
    private const float CardWidth = 1060f;
    private const float TopPad = 24f;
    private const float ButtonHeight = 76f;
    private const float ButtonWidth = 380f;
    private const float ButtonBottom = 30f;
    private const float BarWidth = 600f;
    private const float BarHeight = 24f;
    private const float HeartSize = 44f;
    private const int MaxHeartIcons = 14;
    private const float ScreenMarginX = 80f;
    private const float ScreenMarginY = 60f;

    private static readonly List<HudRewardScreen> Open = new List<HudRewardScreen>();

    /// <summary>
    /// Set by the level guide while it explains an open card: the card's
    /// auto-continue timer waits (presentation only).
    /// </summary>
    public static bool HoldAutoContinue;

    /// <summary>
    /// Set by the case finale: the next card(s) sit on the right half of the
    /// screen with a lighter backdrop, so the scene on the left stays visible.
    /// The card itself is unchanged.
    /// </summary>
    public static bool PreferSideLayout;

    /// <summary>
    /// Set by the case finale while it fades into its scene: the next card
    /// stays invisible (its entry animation waiting) until this is cleared,
    /// then it animates in as usual.
    /// </summary>
    public static bool HoldReveal;

    /// <summary>All card text is drawn this much larger (same layout).</summary>
    private const float TextScale = 1.15f;

    private bool revealHeld;
    private float heldSince;

    // The whole panel (backdrop, card frame, content, button). `group` only
    // covers the card's content, so a held card used to show its empty frame.
    private CanvasGroup panelGroup;
    private GameObject backdropObject;

    private bool IsLevelCompleteCard =>
        shownLarge || (shownTitle ?? "").IndexOf("Level Complete", System.StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>The most a Large card is scaled up when its area has room.</summary>
    public const float LargeScale = 1.3f;
    /// <summary>How much of the screen width (on the right) a Large card's side layout uses.</summary>
    public const float LargeSideShare = 0.6f;
    private bool shownLarge;

    /// <summary>True once this card is actually on screen (not waiting for a scene to fade in).</summary>
    public bool IsRevealed => !revealHeld && group != null && group.alpha > 0.5f;

    private static TextMeshProUGUI CardLabel(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal) =>
        HudTheme.CreateLabel(name, parent, text, size * TextScale, color, alignment, style);

    /// <summary>The newest open card (null when none).</summary>
    public static HudRewardScreen Top
    {
        get
        {
            for (int i = Open.Count - 1; i >= 0; i--)
                if (Open[i] != null && Open[i].isActiveAndEnabled && Open[i].gameObject.activeInHierarchy) return Open[i];
            return null;
        }
    }

    /// <summary>Title of this card (e.g. "Audit Summary", "Level Complete").</summary>
    public string Title => shownTitle;

    /// <summary>The card itself and its Continue button (for pointing at them).</summary>
    public RectTransform Card => card;
    public RectTransform ContinueButton => continueButton;

    /// <summary>True while any reward screen is showing (the HUD steps aside).</summary>
    public static bool AnyOpen
    {
        get
        {
            for (int i = 0; i < Open.Count; i++)
                if (Open[i] != null && Open[i].isActiveAndEnabled && Open[i].gameObject.activeInHierarchy) return true;
            return false;
        }
    }

    private RectTransform panel;
    private RectTransform card;
    private RectTransform content;
    private CanvasGroup group;
    private RectTransform continueButton;
    private Button sceneButton;
    private int lastButtonClickFrame = -1;
    private bool pressStartedOnButton;
    private string shownTitle;
    private bool sideLayout;
    private Image backdropImage;
    private TextMeshProUGUI buttonLabel;
    private RectTransform autoStrip;
    private float autoSeconds;
    private float autoElapsed;
    private readonly List<GameObject> built = new List<GameObject>();

    // Animation state.
    private float t;
    private bool animating;
    private float fitScale = 1f;
    private float cardHeight = 800f;
    private Vector2 laidOutFor;
    private RectTransform heroPop;
    private RectTransform expFill;
    private TextMeshProUGUI expValue;
    private int expTarget, expMax;
    private readonly List<RectTransform> hearts = new List<RectTransform>();
    private readonly List<RectTransform> issueRows = new List<RectTransform>();
    private RectTransform rays;
    private Image glow;
    private readonly List<RectTransform> sparkles = new List<RectTransform>();
    private bool lockedPlayer;
    private bool requestedCompletionMusic;

    // Everything else on screen is hidden while a card is open.
    private struct HiddenUi
    {
        public CanvasGroup Group;
        public bool Added;
        public float Alpha;
        public bool Blocks;
        public bool Interactable;
    }
    private readonly List<HiddenUi> hiddenUi = new List<HiddenUi>();

    // =========================================================
    // API
    // =========================================================

    /// <summary>
    /// Shows `data` on the panel as a reward card. Returns false outside
    /// Office_Level1, where the caller's plain text stays as it was.
    /// </summary>
    public static bool Present(GameObject panelRoot, RewardScreenData data)
    {
        if (panelRoot == null || data == null || OfficeHudRuntime.Instance == null) return false;
        // T08: in a campaign every heart means Public Trust; score hearts are mastery data and are not drawn.
        if (TrustDisplay.Active)
        {
            data.Hearts = 0;
            data.MaxHearts = 0;
        }
        HudRewardScreen screen = panelRoot.GetComponent<HudRewardScreen>();
        if (screen == null)
        {
            screen = panelRoot.AddComponent<HudRewardScreen>();
            screen.Setup();
        }
        screen.Build(data);
        return true;
    }

    /// <summary>Drains the strip on the button, then presses it (same callback as a tap).</summary>
    private void TickAutoContinue()
    {
        if (autoSeconds <= 0f || sceneButton == null || HoldAutoContinue) return;
        autoElapsed += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(autoElapsed / autoSeconds);
        if (autoStrip != null)
        {
            RectTransform host = (RectTransform)autoStrip.parent;
            float full = host.rect.width - 36f;
            autoStrip.offsetMax = new Vector2(18f + full * (1f - k), 11f);
        }
        if (k < 1f) return;
        autoSeconds = 0f;
        if (autoStrip != null) autoStrip.gameObject.SetActive(false);
        if (sceneButton.isActiveAndEnabled && sceneButton.interactable) sceneButton.onClick.Invoke();
    }

    /// <summary>
    /// Never stuck behind a card: a tap that starts and ends on Continue
    /// presses it even if another overlay swallowed the UI click. Runs after
    /// the EventSystem, so a normal click is never pressed twice.
    /// </summary>
    private void LateUpdate()
    {
        if (sceneButton == null || continueButton == null || card == null || animating || !card.gameObject.activeInHierarchy ||
            !sceneButton.isActiveAndEnabled || !sceneButton.interactable)
        {
            pressStartedOnButton = false;
            return;
        }
        Canvas canvas = continueButton.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 pointer = Input.mousePosition;
        bool over = RectTransformUtility.RectangleContainsScreenPoint(continueButton, pointer, eventCamera);
        if (Input.GetMouseButtonDown(0)) pressStartedOnButton = over;
        if (!Input.GetMouseButtonUp(0)) return;
        bool press = pressStartedOnButton && over;
        pressStartedOnButton = false;
        if (press && lastButtonClickFrame != Time.frameCount) sceneButton.onClick.Invoke();
    }

    /// <summary>Hides or shows the whole panel (backdrop, frame, content and button) at once.</summary>
    private void ShowPanel(bool visible)
    {
        // Switched off outright (not just faded) so nothing can show an empty
        // frame: the backdrop and the whole card (frame, content, button).
        if (backdropObject != null && backdropObject.activeSelf != visible) backdropObject.SetActive(visible);
        if (card != null && card.gameObject.activeSelf != visible) card.gameObject.SetActive(visible);
        if (panelGroup == null) return;
        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.blocksRaycasts = visible;
        panelGroup.interactable = visible;
    }

    /// <summary>Card scale that fits a card of this height on a canvas of this size (layout checks).</summary>
    public static float FitScale(float canvasWidth, float canvasHeight, float height, float max = 1f) =>
        Mathf.Clamp(Mathf.Min((canvasWidth - ScreenMarginX) / CardWidth, (canvasHeight - ScreenMarginY) / height), 0.5f, max);

    private void OnEnable() { if (!Open.Contains(this)) Open.Add(this); }

    private void OnDisable()
    {
        Open.Remove(this);
        RestoreOtherUi();
        ReleasePlayer();
        if (requestedCompletionMusic && AudioManager.Instance != null)
        {
            AudioManager.Instance.SetCompletionMusic(false);
            requestedCompletionMusic = false;
        }
    }

    private void OnDestroy()
    {
        Open.Remove(this);
        RestoreOtherUi();
    }

    // =========================================================
    // FOCUS: HIDE EVERYTHING ELSE, HOLD THE PLAYER
    // =========================================================

    private void HideOtherUi()
    {
        if (hiddenUi.Count > 0) return;
        Canvas own = GetComponentInParent<Canvas>();
        Canvas root = own != null ? own.rootCanvas : null;
        Transform exempt = OfficeHudRuntime.Instance != null ? OfficeHudRuntime.Instance.SafeRoot : null;

        Transform node = transform;
        while (node != null && root != null && node != root.transform)
        {
            Transform parent = node.parent;
            if (parent == null) break;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform sibling = parent.GetChild(i);
                if (sibling != node && sibling != exempt) HideUi(sibling);
            }
            node = parent;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isRootCanvas || c == root || c.renderMode == RenderMode.WorldSpace) continue;
            if (c.GetComponent<HudKeepVisibleDuringRewards>() != null) continue;
            if (c.gameObject.scene != gameObject.scene) continue;
            HideUi(c.transform);
        }
    }

    private void HideUi(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;
        if (target.GetComponentInChildren<HudRewardScreen>() != null) return;
        CanvasGroup g = target.GetComponent<CanvasGroup>();
        bool added = g == null;
        if (added) g = target.gameObject.AddComponent<CanvasGroup>();
        hiddenUi.Add(new HiddenUi { Group = g, Added = added, Alpha = g.alpha, Blocks = g.blocksRaycasts, Interactable = g.interactable });
        Conceal(g);
    }

    private static void Conceal(CanvasGroup g)
    {
        g.alpha = 0f;
        g.blocksRaycasts = false;
        g.interactable = false;
    }

    private void RestoreOtherUi()
    {
        for (int i = 0; i < hiddenUi.Count; i++)
        {
            HiddenUi h = hiddenUi[i];
            if (h.Group == null) continue;
            if (h.Added)
            {
                Destroy(h.Group);
                continue;
            }
            h.Group.alpha = h.Alpha;
            h.Group.blocksRaycasts = h.Blocks;
            h.Group.interactable = h.Interactable;
        }
        hiddenUi.Clear();
    }

    /// <summary>No walking or looking around while the card is up.</summary>
    private void HoldPlayer()
    {
        if (lockedPlayer || CameraController.Instance == null) return;
        CameraController.Instance.LockPlayerControls();
        lockedPlayer = true;
    }

    private void ReleasePlayer()
    {
        if (!lockedPlayer) return;
        lockedPlayer = false;
        if (CameraController.Instance != null) CameraController.Instance.UnlockPlayerControls();
    }

    // =========================================================
    // ONE-TIME SKIN
    // =========================================================

    private void Setup()
    {
        panel = (RectTransform)transform;
        panelGroup = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();

        // The plain panel image and texts are replaced (texts keep receiving values).
        Image plain = GetComponent<Image>();
        if (plain != null) plain.enabled = false;
        Button button = GetComponentInChildren<Button>(true);
        foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            if (button == null || !text.transform.IsChildOf(button.transform)) text.enabled = false;

        // Dim the game behind the card; blocks touches to the world like the old panel did.
        RectTransform backdrop = HudTheme.CreateRect("Reward Backdrop", panel);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = new Vector2(-4000f, -4000f);
        backdrop.offsetMax = new Vector2(4000f, 4000f);
        backdropImage = HudTheme.AddImage(backdrop, null, new Color(0.02f, 0.03f, 0.05f, 0.76f));
        backdropImage.raycastTarget = true;
        backdrop.SetSiblingIndex(0);
        backdropObject = backdrop.gameObject;

        // The card: centred, sized to its content in Build().
        card = HudTheme.CreateRect("Reward Card", panel);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = new Vector2(0f, 25f);   // the panel sits 25 below the canvas centre
        card.SetSiblingIndex(1);

        RectTransform halo = HudTheme.CreateRect("Halo", card);
        halo.anchorMin = Vector2.zero;
        halo.anchorMax = Vector2.one;
        halo.offsetMin = new Vector2(-90f, -90f);
        halo.offsetMax = new Vector2(90f, 90f);
        HudTheme.AddImage(halo, Glow, new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.16f));

        RectTransform cardBody = HudTheme.CreatePanel("Frame", card, 24f);
        RectTransform frame = (RectTransform)cardBody.parent;
        HudTheme.Stretch(frame);
        frame.GetComponent<Image>().raycastTarget = true;
        // Opaque body (the brass frame must not show through), lit from the top.
        cardBody.GetComponent<Image>().color = new Color32(16, 25, 38, 255);
        RectTransform sheen = HudTheme.CreateRect("Sheen", cardBody);
        sheen.anchorMin = new Vector2(0f, 0.4f);
        sheen.anchorMax = Vector2.one;
        sheen.offsetMin = new Vector2(4f, 0f);
        sheen.offsetMax = new Vector2(-4f, -4f);
        HudTheme.AddImage(sheen, Gradient, new Color(0.33f, 0.45f, 0.62f, 0.24f));
        // A thin inner brass line for a framed feel.
        RectTransform inner = HudTheme.CreateRect("Inner Line", cardBody);
        HudTheme.Stretch(inner, 10f);
        Image line = HudTheme.AddImage(inner, HudTheme.Rounded, new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.35f), 16f);
        line.fillCenter = false;

        content = HudTheme.CreateRect("Reward Content", card);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = new Vector2(0f, -TopPad);
        group = content.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        // The scene's own button moves into the card, bottom centre (same callbacks).
        if (button != null)
        {
            continueButton = (RectTransform)button.transform;
            continueButton.SetParent(card, false);
            continueButton.anchorMin = continueButton.anchorMax = new Vector2(0.5f, 0f);
            continueButton.pivot = new Vector2(0.5f, 0f);
            continueButton.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            continueButton.anchoredPosition = new Vector2(0f, ButtonBottom);
            continueButton.SetAsLastSibling();
            SkinButton(button);
            sceneButton = button;
            button.onClick.AddListener(() => lastButtonClickFrame = Time.frameCount);
            buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);

            // Auto-continue progress: a thin strip along the bottom of the button.
            autoStrip = HudTheme.CreateRect("Auto Continue", button.transform);
            autoStrip.anchorMin = new Vector2(0f, 0f);
            autoStrip.anchorMax = new Vector2(0f, 0f);
            autoStrip.pivot = new Vector2(0f, 0f);
            autoStrip.offsetMin = new Vector2(18f, 6f);
            autoStrip.offsetMax = new Vector2(18f, 11f);
            HudTheme.AddImage(autoStrip, HudTheme.Rounded, new Color(HudTheme.PanelInner.r, HudTheme.PanelInner.g, HudTheme.PanelInner.b, 0.55f), 2.5f);
            autoStrip.gameObject.SetActive(false);
        }
    }

    private static void SkinButton(Button button)
    {
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = HudTheme.Rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 20f / 38f;
            image.color = HudTheme.Brass;
            var shadow = image.gameObject.GetComponent<Shadow>() ?? image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -5f);
        }
        RectTransform shine = HudTheme.CreateRect("Shine", button.transform);
        shine.anchorMin = new Vector2(0f, 0.5f);
        shine.anchorMax = Vector2.one;
        shine.offsetMin = new Vector2(8f, 0f);
        shine.offsetMax = new Vector2(-8f, -5f);
        HudTheme.AddImage(shine, HudTheme.Rounded, new Color(1f, 0.96f, 0.85f, 0.28f), 16f);
        shine.SetAsFirstSibling();

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.06f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.76f, 0.68f, 1f);
        button.colors = colors;
        foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            label.font = HudTheme.Font;
            label.color = HudTheme.PanelInner;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 6f;
            label.enableAutoSizing = true;
            label.fontSizeMax = 32f;
            label.fontSizeMin = 18f;
            label.text = label.text.ToUpperInvariant();
            label.transform.SetAsLastSibling();
        }
    }

    // =========================================================
    // CONTENT (top-down flow; the card height follows)
    // =========================================================

    private void Build(RewardScreenData data)
    {
        for (int i = 0; i < built.Count; i++) if (built[i] != null) Destroy(built[i]);
        built.Clear();
        shownTitle = data.Title;
        shownLarge = data.Large;
        // The Level Complete card waits for the client scene even if this
        // card is built before the finale has started.
        bool finaleCard = (shownTitle ?? "").IndexOf("Level Complete", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                          CaseFinale.WillStage;
        // A Large (chapter) card sits on the right 60% over the client scene, like the Level Complete card (2026-10-26).
        sideLayout = shownLarge || PreferSideLayout || finaleCard;
        if (backdropImage != null) backdropImage.color = new Color(0.02f, 0.03f, 0.05f, sideLayout ? 0.18f : 0.76f);
        laidOutFor = Vector2.zero;
        hearts.Clear();
        issueRows.Clear();
        sparkles.Clear();
        heroPop = null;
        expFill = null;
        rays = null;
        glow = null;

        if (buttonLabel != null && !string.IsNullOrEmpty(data.ButtonLabel)) buttonLabel.text = data.ButtonLabel.ToUpperInvariant();
        autoSeconds = sceneButton != null ? Mathf.Max(0f, data.AutoContinueSeconds) : 0f;
        autoElapsed = 0f;
        if (autoStrip != null) autoStrip.gameObject.SetActive(autoSeconds > 0f);

        HideOtherUi();
        HoldPlayer();

        float y = 0f;
        Banner(data.Title?.ToUpperInvariant() ?? string.Empty, y);
        y += 64f + 12f;
        if (!string.IsNullOrEmpty(data.Subtitle))
        {
            Label(data.Subtitle, 26f, HudTheme.BrassSoft, y, 32f, FontStyles.Italic);
            y += 32f + 6f;
        }
        Divider(y);
        y += 14f + 16f;

        y = BuildHero(data, y);
        y = BuildBars(data, y);
        y = BuildRewardTiles(data, y);
        y = BuildFacts(data, y);

        if (!string.IsNullOrEmpty(data.Summary))
        {
            Divider(y);
            y += 14f + 12f;
            TextMeshProUGUI summary = Label(data.Summary, 26f, HudTheme.TextPrimary, y, 40f);
            summary.textWrappingMode = TextWrappingModes.Normal;
            summary.enableAutoSizing = false;
            float h = Mathf.Clamp(summary.GetPreferredValues(data.Summary, ContentWidth - 120f, 0f).y + 6f, 36f, 110f);
            RectTransform sr = (RectTransform)summary.transform;
            sr.offsetMin = new Vector2(60f, 0f);
            sr.offsetMax = new Vector2(-60f, 0f);
            ((RectTransform)summary.transform.parent).sizeDelta = new Vector2(ContentWidth, h);
            y += h + 8f;
        }
        if (!string.IsNullOrEmpty(data.Footer))
        {
            Label(data.Footer, 21f, HudTheme.TextMuted, y, 28f);
            y += 28f + 4f;
        }

        content.sizeDelta = new Vector2(ContentWidth, y);
        cardHeight = TopPad + y + 18f + ButtonHeight + ButtonBottom;
        card.sizeDelta = new Vector2(CardWidth, cardHeight);

        t = 0f;
        animating = true;
        revealHeld = HoldReveal || (IsLevelCompleteCard && (CaseFinale.WillStage || CaseFinale.HoldsCards));
        heldSince = Time.unscaledTime;
        ShowPanel(!revealHeld);
        if (!revealHeld) StartCompletionMusic();
        enabled = true;
        group.alpha = 0f;
        laidOutFor = Vector2.zero;
        Fit();
    }

    private float BuildHero(RewardScreenData data, float y)
    {
        float heroHeight;
        Vector2 glowAt;
        RectTransform hero;

        if (data.Issues.Count > 0)
        {
            int rows = Mathf.Min(5, data.Issues.Count);
            heroHeight = 84f + rows * 54f;
            hero = Rect("Hero", y, heroHeight);
            heroPop = Pill(hero, data.IssuesTitle, Coral, 28f, 64f, 34f, null, "!");
            for (int i = 0; i < rows; i++)
            {
                RectTransform row = Place(HudTheme.CreateRect("Issue " + (i + 1), hero), 0f, -(84f + i * 54f + 23f), 760f, 46f);
                HudTheme.AddImage(row, HudTheme.Rounded, new Color(1f, 1f, 1f, 0.05f), 12f);
                RectTransform dot = HudTheme.CreateRect("Mark", row);
                dot.anchorMin = dot.anchorMax = new Vector2(0f, 0.5f);
                dot.sizeDelta = new Vector2(30f, 30f);
                dot.anchoredPosition = new Vector2(26f, 0f);
                HudTheme.AddImage(dot, HudTheme.Circle, Coral);
                TextMeshProUGUI bang = CardLabel("Bang", dot, "!", 22f, HudTheme.PanelInner, TextAlignmentOptions.Center, FontStyles.Bold);
                HudTheme.Stretch((RectTransform)bang.transform);
                TextMeshProUGUI text = CardLabel("Text", row, data.Issues[i], 24f, HudTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
                RectTransform tr = (RectTransform)text.transform;
                HudTheme.Stretch(tr);
                tr.offsetMin = new Vector2(56f, 0f);
                tr.offsetMax = new Vector2(-16f, 0f);
                row.localScale = new Vector3(1f, 0f, 1f);
                issueRows.Add(row);
            }
            glowAt = new Vector2(0f, -34f);
        }
        else if (data.HasVerdict)
        {
            heroHeight = data.Flawless ? 190f : 130f;
            hero = Rect("Hero", y, heroHeight);
            heroPop = Pill(hero, (data.VerdictCorrect ? "CORRECT" : "INCORRECT") +
                (string.IsNullOrEmpty(data.VerdictText) ? string.Empty : "  ·  " + data.VerdictText),
                data.VerdictCorrect ? HudTheme.Complete : Coral, 30f, 76f, 56f, data.VerdictCorrect ? HudTheme.Check : null);
            if (data.Flawless)
            {
                RectTransform flawless = Pill(hero, "FLAWLESS", HudTheme.Exp, 26f, 58f, 150f, HudTheme.Star);
                flawless.localRotation = Quaternion.Euler(0f, 0f, -3f);
            }
            glowAt = new Vector2(0f, -56f);
        }
        else if (data.HasStatus)
        {
            bool medals = data.Medals.Count > 0;
            heroHeight = medals ? 300f : 130f;
            hero = Rect("Hero", y, heroHeight);
            heroPop = Pill(hero, data.StatusText, data.StatusGood ? HudTheme.Complete : Coral, 34f, 76f, 40f,
                data.StatusGood ? HudTheme.Check : null);
            if (!string.IsNullOrEmpty(data.StatusDetail))
            {
                TextMeshProUGUI detail = CardLabel("Status Detail", hero, data.StatusDetail, 23f, HudTheme.TextMuted,
                    TextAlignmentOptions.Center);
                Place((RectTransform)detail.transform, 0f, -100f, 900f, 32f);
            }
            const float size = 170f;
            for (int i = 0; i < data.Medals.Count; i++)
            {
                float x = (i - (data.Medals.Count - 1) * 0.5f) * (size + 24f);
                Medal(hero, data.Medals[i], size, x, -212f);
            }
            glowAt = medals ? new Vector2(0f, -212f) : new Vector2(0f, -40f);
        }
        else if (data.Medals.Count > 0)
        {
            bool labelled = !string.IsNullOrEmpty(data.MedalLabel);
            heroHeight = labelled ? 286f : 246f;
            hero = Rect("Hero", y, heroHeight);
            heroPop = Medal(hero, data.Medals[0], 240f, 0f, -120f);
            if (labelled)
            {
                TextMeshProUGUI label = CardLabel("Medal Label", hero, data.MedalLabel.ToUpperInvariant(), 28f,
                    HudTheme.BrassSoft, TextAlignmentOptions.Center, FontStyles.Bold);
                label.characterSpacing = 8f;
                Place((RectTransform)label.transform, 0f, -264f, 700f, 36f);
            }
            glowAt = new Vector2(0f, -120f);
        }
        else
        {
            return y;
        }

        // Light rays and a soft glow behind the hero, plus a few twinkles.
        Color accent = data.Accent;
        bool wide = data.Issues.Count > 0 || data.HasVerdict;
        glow = HudTheme.AddImage(Place(HudTheme.CreateRect("Glow", hero), glowAt.x, glowAt.y, wide ? 520f : 360f, wide ? 220f : 360f),
            Glow, new Color(accent.r, accent.g, accent.b, wide ? 0.32f : 0.5f));
        glow.transform.SetAsFirstSibling();
        if (!wide)
        {
            rays = Place(HudTheme.CreateRect("Rays", hero), glowAt.x, glowAt.y, 500f, 500f);
            HudTheme.AddImage(rays, Rays, new Color(accent.r, accent.g, accent.b, 0.34f));
            rays.SetAsFirstSibling();
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 25f) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? 150f : 118f;
                RectTransform spark = Place(HudTheme.CreateRect("Sparkle", hero), glowAt.x + Mathf.Cos(a) * r * 1.3f,
                    glowAt.y + Mathf.Sin(a) * r * 0.78f, 22f, 22f);
                HudTheme.AddImage(spark, HudTheme.Star, new Color(1f, 0.95f, 0.8f, 0.9f));
                spark.localScale = Vector3.zero;
                sparkles.Add(spark);
            }
        }
        return y + heroHeight + 18f;
    }

    private float BuildBars(RewardScreenData data, float y)
    {
        float block = 48f + BarWidth + 18f + 150f;
        float left = -block * 0.5f;
        if (data.MaxExp > 0)
        {
            RectTransform row = Rect("EXP Row", y, 50f);
            RectTransform icon = Place(HudTheme.CreateRect("Icon", row), left + 20f, -25f, 42f, 42f);
            HudTheme.AddImage(icon, HudTheme.Star, HudTheme.Exp);
            RectTransform well = Place(HudTheme.CreateRect("Well", row), left + 46f, -25f, BarWidth + 6f, BarHeight + 6f, 0f);
            HudTheme.AddImage(well, HudTheme.Rounded, new Color(0f, 0f, 0f, 0.35f), (BarHeight + 6f) * 0.5f);
            RectTransform track = Place(HudTheme.CreateRect("Track", row), left + 49f, -25f, BarWidth, BarHeight, 0f);
            HudTheme.AddImage(track, HudTheme.Rounded, HudTheme.Track, BarHeight * 0.5f);
            expFill = HudTheme.CreateRect("Fill", track);
            expFill.anchorMin = Vector2.zero;
            expFill.anchorMax = new Vector2(0f, 1f);
            expFill.offsetMin = expFill.offsetMax = Vector2.zero;
            HudTheme.AddImage(expFill, HudTheme.Rounded, HudTheme.Exp, BarHeight * 0.5f);
            RectTransform shine = HudTheme.CreateRect("Shine", expFill);
            shine.anchorMin = new Vector2(0f, 0.55f);
            shine.anchorMax = Vector2.one;
            shine.offsetMin = new Vector2(6f, 0f);
            shine.offsetMax = new Vector2(-6f, -3f);
            HudTheme.AddImage(shine, HudTheme.Rounded, new Color(1f, 1f, 1f, 0.35f), 5f);
            expFill.gameObject.SetActive(false);
            if (data.PassingPercent > 0 && data.PassingPercent < 100)
            {
                RectTransform mark = HudTheme.CreateRect("Passing Mark", track);
                float px = data.PassingPercent / 100f;
                mark.anchorMin = new Vector2(px, 0f);
                mark.anchorMax = new Vector2(px, 1f);
                mark.offsetMin = new Vector2(-2f, -7f);
                mark.offsetMax = new Vector2(2f, 7f);
                bool reached = data.Exp * 100 >= data.PassingPercent * data.MaxExp;
                HudTheme.AddImage(mark, HudTheme.Rounded, reached ? HudTheme.Complete : HudTheme.BrassSoft, 2f);
            }
            expValue = CardLabel("Value", row, string.Empty, 28f, HudTheme.TextPrimary,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Place((RectTransform)expValue.transform, left + 49f + BarWidth + 18f, -25f, 150f, 40f, 0f);
            expTarget = data.Exp;
            expMax = data.MaxExp;
            expValue.SetText("0 / {0}", expMax);
            y += 50f + 10f;
        }

        if (data.MaxHearts > 0)
        {
            RectTransform row = Rect("Hearts Row", y, 52f);
            if (data.MaxHearts <= MaxHeartIcons)
            {
                float step = HeartSize + 6f;
                float width = data.MaxHearts * step - 6f + 18f + 110f;
                float x0 = -width * 0.5f + HeartSize * 0.5f;
                for (int i = 0; i < data.MaxHearts; i++)
                {
                    bool filled = i < data.Hearts;
                    RectTransform slot = Place(HudTheme.CreateRect("Heart Slot " + (i + 1), row), x0 + i * step, -26f, HeartSize, HeartSize);
                    HudTheme.AddImage(slot, HudTheme.Heart, filled ? new Color(0f, 0f, 0f, 0.35f) : HudTheme.Empty);
                    if (!filled) continue;
                    // The heart pops into its slot, then stays put.
                    RectTransform heart = Place(HudTheme.CreateRect("Heart", slot), 0f, 2f, HeartSize, HeartSize);
                    heart.anchorMin = heart.anchorMax = new Vector2(0.5f, 0.5f);
                    heart.anchoredPosition = new Vector2(0f, 2f);
                    HudTheme.AddImage(heart, HudTheme.Heart, HudTheme.Reputation);
                    heart.localScale = Vector3.zero;
                    hearts.Add(heart);
                }
                TextMeshProUGUI value = CardLabel("Value", row, $"{data.Hearts} / {data.MaxHearts}", 28f,
                    HudTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                Place((RectTransform)value.transform, x0 + data.MaxHearts * step - HeartSize * 0.5f + 12f, -26f, 110f, 40f, 0f);
            }
            else
            {
                TextMeshProUGUI value = CardLabel("Value", row, $"{data.Hearts} / {data.MaxHearts} hearts", 28f,
                    HudTheme.Reputation, TextAlignmentOptions.Center, FontStyles.Bold);
                Place((RectTransform)value.transform, 0f, -26f, 600f, 40f);
            }
            y += 52f + 18f;
        }
        else if (data.MaxExp > 0)
        {
            y += 8f;
        }
        return y;
    }

    private float BuildFacts(RewardScreenData data, float y)
    {
        if (data.Facts.Count == 0) return y;
        RectTransform row = Rect("Facts", y, 52f);
        var chips = new List<RectTransform>();
        float total = 0f;
        foreach (var fact in data.Facts)
        {
            RectTransform chip = HudTheme.CreateRect("Fact " + fact.Key, row);
            HudTheme.AddImage(chip, HudTheme.Rounded, new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.55f), 14f);
            RectTransform chipFill = HudTheme.CreateRect("Fill", chip);
            HudTheme.Stretch(chipFill, 2f);
            HudTheme.AddImage(chipFill, HudTheme.Rounded, HudTheme.PanelInner, 13f);
            TextMeshProUGUI text = CardLabel("Text", chip,
                $"<color=#A8B4C0><size=70%>{fact.Key.ToUpperInvariant()}</size></color>   {fact.Value}", 24f,
                HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            text.enableAutoSizing = false;
            float w = text.GetPreferredValues(text.text).x + 44f;
            HudTheme.Stretch((RectTransform)text.transform);
            chip.anchorMin = chip.anchorMax = new Vector2(0.5f, 1f);
            chip.pivot = new Vector2(0f, 1f);
            chip.sizeDelta = new Vector2(w, 52f);
            chips.Add(chip);
            total += w;
        }
        total += (chips.Count - 1) * 16f;
        // Larger text must never push the chips past the card.
        if (total > ContentWidth) row.localScale = new Vector3(ContentWidth / total, ContentWidth / total, 1f);
        float x = -total * 0.5f;
        foreach (RectTransform chip in chips)
        {
            chip.anchoredPosition = new Vector2(x, 0f);
            x += chip.sizeDelta.x + 16f;
        }
        return y + 52f + 20f;
    }

    private float BuildRewardTiles(RewardScreenData data, float y)
    {
        int count = data.RewardTiles.Count;
        if (count == 0) return y;

        const float height = 112f;
        const float gap = 18f;
        float width = Mathf.Min(420f, (ContentWidth - gap * (count - 1)) / count);
        RectTransform row = Rect("Rewards", y, height);

        for (int i = 0; i < count; i++)
        {
            RewardScreenData.RewardTileData reward = data.RewardTiles[i];
            float x = (i - (count - 1) * 0.5f) * (width + gap);
            RectTransform tile = Place(HudTheme.CreateRect(reward.Label + " Reward", row), x, -height * 0.5f, width, height);
            HudTheme.AddImage(tile, HudTheme.Rounded,
                new Color(reward.Accent.r, reward.Accent.g, reward.Accent.b, 0.42f), 14f);
            RectTransform fill = HudTheme.CreateRect("Fill", tile);
            HudTheme.Stretch(fill, 2f);
            HudTheme.AddImage(fill, HudTheme.Rounded, HudTheme.PanelInner, 13f);

            RectTransform icon = Place(HudTheme.CreateRect("Icon", tile), -width * 0.5f + 42f, -height * 0.5f, 42f, 42f);
            HudTheme.AddImage(icon, reward.Icon, reward.Accent);
            TextMeshProUGUI title = CardLabel("Title", tile, reward.Label.ToUpperInvariant(), 23f,
                HudTheme.TextMuted, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Place((RectTransform)title.transform, -width * 0.5f + 74f, -22f, width - 92f, 28f, 0f);

            TextMeshProUGUI earned = CardLabel("Earned", tile, $"+{reward.Earned} earned", 27f,
                reward.Accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Place((RectTransform)earned.transform, -width * 0.5f + 74f, -53f, width - 92f, 30f, 0f);

            string balance = reward.BalanceMax > 0
                ? $"{reward.BalanceLabel} {reward.Balance} / {reward.BalanceMax}"
                : $"{reward.BalanceLabel} {reward.Balance}";
            TextMeshProUGUI totalLabel = CardLabel("Balance", tile, balance, 20f,
                HudTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            Place((RectTransform)totalLabel.transform, 0f, -87f, width - 28f, 24f);
        }

        return y + height + 18f;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static readonly Color Coral = new Color32(226, 112, 96, 255);

    /// <summary>Brass ribbon banner with folded tails, title in navy capitals.</summary>
    private void Banner(string title, float y)
    {
        RectTransform row = Rect("Title Banner", y, 64f);
        TextMeshProUGUI text = CardLabel("Title", row, title, 40f, HudTheme.PanelInner,
            TextAlignmentOptions.Center, FontStyles.Bold);
        text.characterSpacing = 8f;
        text.enableAutoSizing = false;
        float w = Mathf.Min(ContentWidth - 160f, text.GetPreferredValues(title).x + 120f);
        Color dark = new Color32(150, 116, 58, 255);
        for (int side = -1; side <= 1; side += 2)
        {
            RectTransform tail = Place(HudTheme.CreateRect("Tail", row), side * (w * 0.5f + 14f), -40f, 92f, 48f);
            HudTheme.AddImage(tail, HudTheme.Rounded, dark, 8f);
            tail.localRotation = Quaternion.Euler(0f, 0f, side * -6f);
        }
        RectTransform band = Place(HudTheme.CreateRect("Band", row), 0f, -32f, w, 64f);
        HudTheme.AddImage(band, HudTheme.Rounded, HudTheme.Brass, 10f);
        var bandShadow = band.gameObject.AddComponent<Shadow>();
        bandShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        bandShadow.effectDistance = new Vector2(0f, -4f);
        RectTransform shine = HudTheme.CreateRect("Shine", band);
        shine.anchorMin = new Vector2(0f, 0.5f);
        shine.anchorMax = Vector2.one;
        shine.offsetMin = new Vector2(6f, 0f);
        shine.offsetMax = new Vector2(-6f, -4f);
        HudTheme.AddImage(shine, HudTheme.Rounded, new Color(1f, 0.95f, 0.8f, 0.25f), 8f);
        text.transform.SetAsLastSibling();
        Place((RectTransform)text.transform, 0f, -32f, w, 64f);
    }

    /// <summary>Thin brass rule with a small diamond in the middle.</summary>
    private void Divider(float y)
    {
        RectTransform row = Rect("Divider", y, 14f);
        Color line = new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.5f);
        HudTheme.AddImage(Place(HudTheme.CreateRect("Rule", row), 0f, -7f, 460f, 2f), null, line);
        RectTransform diamond = Place(HudTheme.CreateRect("Diamond", row), 0f, -7f, 12f, 12f);
        HudTheme.AddImage(diamond, null, HudTheme.Brass);
        diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    /// <summary>A full-width row at `y` from the top of the content.</summary>
    private RectTransform Rect(string name, float y, float height)
    {
        RectTransform rect = HudTheme.CreateRect(name, content);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(ContentWidth, height);
        rect.anchoredPosition = new Vector2(0f, -y);
        built.Add(rect.gameObject);
        return rect;
    }

    private TextMeshProUGUI Label(string text, float size, Color color, float y, float height,
        FontStyles style = FontStyles.Normal, float spacing = 0f)
    {
        RectTransform row = Rect("Label", y, height);
        TextMeshProUGUI label = CardLabel("Text", row, text ?? string.Empty, size, color,
            TextAlignmentOptions.Center, style);
        label.characterSpacing = spacing;
        HudTheme.Stretch((RectTransform)label.transform);
        return label;
    }

    /// <summary>Positions a child of a row: x from the row centre, y from the row top (negative down).</summary>
    private static RectTransform Place(RectTransform rect, float x, float y, float w, float h, float pivotX = 0.5f)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(pivotX, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    private static RectTransform Pill(RectTransform parent, string text, Color color, float size, float height, float centreY,
        Sprite icon = null, string mark = null)
    {
        RectTransform pill = HudTheme.CreateRect("Pill", parent);
        HudTheme.AddImage(pill, HudTheme.Rounded, color, height * 0.5f);
        var shadow = pill.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        shadow.effectDistance = new Vector2(0f, -4f);
        TextMeshProUGUI label = CardLabel("Text", pill, text, size, HudTheme.PanelInner,
            TextAlignmentOptions.Center, FontStyles.Bold);
        label.characterSpacing = 4f;
        label.enableAutoSizing = false;
        float textW = Mathf.Min(820f, label.GetPreferredValues(text).x);
        bool hasMark = icon != null || mark != null;
        float iconW = hasMark ? height * 0.62f + 12f : 0f;
        float width = textW + iconW + height;
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(textW + 4f, height);
        labelRect.anchoredPosition = new Vector2(iconW * 0.5f, 0f);
        if (hasMark)
        {
            RectTransform glyph = HudTheme.CreateRect("Icon", pill);
            glyph.anchorMin = glyph.anchorMax = new Vector2(0.5f, 0.5f);
            glyph.sizeDelta = new Vector2(height * 0.62f, height * 0.62f);
            glyph.anchoredPosition = new Vector2(-textW * 0.5f - 6f, 0f);
            if (icon != null) HudTheme.AddImage(glyph, icon, HudTheme.PanelInner);
            else
            {
                HudTheme.AddImage(glyph, HudTheme.Circle, HudTheme.PanelInner);
                TextMeshProUGUI m = CardLabel("Mark", glyph, mark, size * 0.9f, color, TextAlignmentOptions.Center, FontStyles.Bold);
                HudTheme.Stretch((RectTransform)m.transform);
            }
        }
        Place(pill, 0f, -centreY, width, height);
        return pill;
    }

    private static RectTransform Medal(RectTransform parent, Sprite sprite, float size, float x, float y)
    {
        RectTransform medal = Place(HudTheme.CreateRect("Medal", parent), x, y, size, size);
        HudTheme.AddImage(medal, sprite, Color.white).preserveAspect = true;
        return medal;
    }

    private static Sprite gradient, glowSprite, raysSprite;

    /// <summary>Vertical fade: opaque at the top, clear at the bottom.</summary>
    private static Sprite Gradient => gradient != null ? gradient : gradient = SoftSprite(8, 64, (u, v) => v * v, "RewardGradient");

    /// <summary>Soft round glow.</summary>
    private static Sprite Glow => glowSprite != null ? glowSprite : glowSprite = SoftSprite(96, 96, (u, v) =>
    {
        float dx = u - 0.5f, dy = v - 0.5f;
        float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
        return Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
    }, "RewardGlow");

    /// <summary>Twelve soft light rays fading outwards.</summary>
    private static Sprite Rays => raysSprite != null ? raysSprite : raysSprite = SoftSprite(160, 160, (u, v) =>
    {
        float dx = u - 0.5f, dy = v - 0.5f;
        float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
        if (r > 1f) return 0f;
        float ray = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(Mathf.Atan2(dy, dx) * 6f)), 4f);
        return ray * Mathf.Pow(1f - r, 1.4f) * Mathf.Clamp01(r * 4f);
    }, "RewardRays");

    private static Sprite SoftSprite(int w, int h, System.Func<float, float, float> alpha, string name)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color32(255, 255, 255,
                    (byte)Mathf.Clamp(Mathf.RoundToInt(255f * alpha((x + 0.5f) / w, (y + 0.5f) / h)), 0, 255));
        tex.SetPixels32(px);
        tex.Apply(false, true);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        return sprite;
    }

    // =========================================================
    // FIT + ANIMATION
    // =========================================================

    private void Fit()
    {
        Canvas own = GetComponentInParent<Canvas>();
        RectTransform canvasRect = own != null ? (RectTransform)own.rootCanvas.transform : panel;
        Vector2 size = canvasRect.rect.size;
        if (size == laidOutFor) return;
        laidOutFor = size;
        if (sideLayout)
        {
            // Right half of the screen (the right 60% for a Large chapter card, which may grow to fill it);
            // the card keeps its own look and the client scene stays visible on the left.
            float share = shownLarge ? LargeSideShare : 0.5f;
            fitScale = FitScale(size.x * share + ScreenMarginX * 0.5f, size.y, cardHeight, shownLarge ? LargeScale : 1f);
            card.anchoredPosition = new Vector2(size.x * (0.5f - share * 0.5f) - ScreenMarginX * 0.25f, 25f);
        }
        else
        {
            fitScale = FitScale(size.x, size.y, cardHeight, shownLarge ? LargeScale : 1f);
            card.anchoredPosition = new Vector2(0f, 25f);
        }
    }

    private void Update()
    {
        Fit();

        // Other scripts may fade their UI back in; keep it hidden while open.
        for (int i = 0; i < hiddenUi.Count; i++)
            if (hiddenUi[i].Group != null && hiddenUi[i].Group.alpha > 0f) Conceal(hiddenUi[i].Group);

        float now = Time.unscaledTime;
        if (rays != null) rays.localRotation = Quaternion.Euler(0f, 0f, now * 9f);
        if (glow != null)
        {
            float g = 1f + 0.06f * Mathf.Sin(now * 2.2f);
            glow.rectTransform.localScale = new Vector3(g, g, 1f);
        }
        for (int i = 0; i < sparkles.Count; i++)
        {
            float k = Mathf.Clamp01((t - 0.5f - i * 0.07f) / 0.3f);
            float tw = 0.55f + 0.45f * Mathf.Sin(now * 3.1f + i * 1.7f);
            float s = HudTheme.EaseOutBack(k) * tw;
            sparkles[i].localScale = new Vector3(s, s, 1f);
            sparkles[i].localRotation = Quaternion.Euler(0f, 0f, now * 40f + i * 30f);
        }

        // The client scene started after this card was built: hide it again
        // and restart its entry animation once the scene lets it show.
        if (!revealHeld && IsLevelCompleteCard && (HoldReveal || CaseFinale.HoldsCards))
        {
            revealHeld = true;
            heldSince = Time.unscaledTime;
            t = 0f;
            animating = true;
        }

        // Waiting for a scene to fade in: invisible, entry animation not started.
        if (revealHeld)
        {
            // Never stuck: give up waiting after 10 s.
            if ((HoldReveal || CaseFinale.HoldsCards || !CaseFinale.CardReleased) && Time.unscaledTime - heldSince < 10f)
            {
                ShowPanel(false);
                group.alpha = 0f;
                t = 0f;
                return;
            }
            revealHeld = false;
            ShowPanel(true);   // the backdrop and frame appear with the card's own pop-in
            StartCompletionMusic();
        }

        if (!animating)
        {
            TickAutoContinue();
            card.localScale = new Vector3(fitScale, fitScale, 1f);
            if (continueButton != null)
            {
                float p = 1f + 0.035f * Mathf.Sin(now * 3f);
                continueButton.localScale = new Vector3(p, p, 1f);
            }
            return;
        }

        t += Time.unscaledDeltaTime;

        float c = Mathf.Clamp01(t / 0.3f);
        group.alpha = HudTheme.EaseOutCubic(c);
        float cs = Mathf.LerpUnclamped(0.92f, 1f, HudTheme.EaseOutBack(c)) * fitScale;
        card.localScale = new Vector3(cs, cs, 1f);

        if (heroPop != null)
        {
            float k = Mathf.Clamp01((t - 0.15f) / 0.5f);
            float s = Mathf.LerpUnclamped(0.3f, 1f, HudTheme.EaseOutBack(k));
            heroPop.localScale = new Vector3(s, s, 1f);
            heroPop.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-12f, 0f, HudTheme.EaseOutCubic(k)));
        }

        for (int i = 0; i < issueRows.Count; i++)
        {
            float k = HudTheme.EaseOutCubic(Mathf.Clamp01((t - 0.45f - i * 0.1f) / 0.25f));
            issueRows[i].localScale = new Vector3(1f, k, 1f);
        }

        if (expFill != null && expMax > 0)
        {
            float k = HudTheme.EaseOutCubic(Mathf.Clamp01((t - 0.4f) / 0.9f));
            float shown = expTarget * k;
            float fraction = Mathf.Clamp01(shown / expMax);
            expFill.anchorMax = new Vector2(fraction, 1f);
            expFill.gameObject.SetActive(fraction > 0.03f);
            expValue.SetText("{0} / {1}", Mathf.RoundToInt(shown), expMax);
        }

        for (int i = 0; i < hearts.Count; i++)
        {
            float k = Mathf.Clamp01((t - 0.65f - i * 0.07f) / 0.28f);
            float s = HudTheme.EaseOutBack(k);
            hearts[i].localScale = new Vector3(s, s, 1f);
        }

        float end = Mathf.Max(1.4f, 0.65f + hearts.Count * 0.07f + 0.3f, 0.45f + issueRows.Count * 0.1f + 0.3f);
        if (t >= end)
        {
            // Settle everything exactly: nothing keeps drifting once shown.
            animating = false;
            group.alpha = 1f;
            card.localScale = new Vector3(fitScale, fitScale, 1f);
            if (heroPop != null) { heroPop.localScale = Vector3.one; heroPop.localRotation = Quaternion.identity; }
            for (int i = 0; i < hearts.Count; i++) hearts[i].localScale = Vector3.one;
            for (int i = 0; i < issueRows.Count; i++) issueRows[i].localScale = Vector3.one;
            if (expFill != null && expMax > 0)
            {
                float fraction = Mathf.Clamp01((float)expTarget / expMax);
                expFill.anchorMax = new Vector2(fraction, 1f);
                expFill.gameObject.SetActive(fraction > 0.03f);
                expValue.SetText("{0} / {1}", expTarget, expMax);
            }
        }
    }

    private void StartCompletionMusic()
    {
        if (!IsLevelCompleteCard || requestedCompletionMusic || AudioManager.Instance == null) return;
        AudioManager.Instance.SetCompletionMusic(true);
        requestedCompletionMusic = true;
    }
}
