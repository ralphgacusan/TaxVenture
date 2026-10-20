using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// The 3D Menu level select: two real corkboards on the wall (the Synty office
/// corkboard used in Office Level 1). The left one is the career record: a
/// CAREER card (career EXP and hearts with bars, chapters passed, badge count)
/// and a NEXT GOAL card (the chapter to play next against its pass line). The
/// right one holds a pinned paper per chapter, red strings linking every
/// unlocked chapter to the one before it, and the side-quest badge area: a
/// fixed number of badge spaces, each earned badge pinned on a
/// small paper, the rest waiting empty. Everything is 3D geometry and world
/// lettering; only the progress bars use tiny world-space canvases.
///
/// Data comes from the career module (CareerProgress / CareerProgressService)
/// on the live save, refreshes when the save changes, and celebrates what
/// changed since the last visit: EXP and hearts count up, a newly passed
/// chapter gets its PASSED stamp slammed on, a new or better badge pops.
///
/// Tap a chapter or badge paper: it lifts off the pin and becomes the chapter
/// sheet with START / REPLAY. Back (button or Escape / Android back) closes the
/// sheet first, then returns to the main menu. Input is ignored while the
/// camera is moving or another view is in front.
/// </summary>
public sealed class CareerBoardPresenter : MonoBehaviour
{
    private const float DesignAspect = 16f / 9f;

    private static readonly Vector2 ProgressBoardCenter = new Vector2(-560f, -65f);
    private static readonly Vector2 ProgressBoardSize = new Vector2(420f, 790f);
    private static readonly Vector2 StoryBoardCenter = new Vector2(225f, -40f);
    private static readonly Vector2 StoryBoardSize = new Vector2(1110f, 840f);
    private static readonly Vector2 ChapterPaperSize = new Vector2(200f, 240f);
    private static readonly Vector2 BadgePaperSize = new Vector2(116f, 168f);
    private const float CardWidth = 340f;
    private const float CareerCardHeight = 290f;
    private const float GoalCardHeight = 180f;
    private const float BadgeCardHeight = 112f;

    // Pin positions keep the 3 + 2 arrangement, nudged up and down so the board looks hand-pinned.
    private static readonly Vector2[] ChapterPins =
    {
        new Vector2(-115f, 292f), new Vector2(240f, 260f), new Vector2(560f, 274f),
        new Vector2(250f, -18f), new Vector2(555f, -52f)
    };
    private static readonly float[] ChapterTilts = { -4f, 2.5f, -2f, 3f, -3.5f };

    // Side-quest badge spaces (bottom left of the story board): 3 + 2.
    private static readonly Vector2[] BadgePins =
    {
        new Vector2(-216f, -2f), new Vector2(-82f, -14f), new Vector2(52f, 0f),
        new Vector2(-149f, -182f), new Vector2(-15f, -192f)
    };
    private static readonly float[] BadgeTilts = { -1.5f, 1.2f, -1f, 1.5f, -1.2f };

    [Header("Scene")]
    [SerializeField] private ToggleCamera cameraManager;
    [SerializeField] private Camera viewCamera;
    [Tooltip("Board origin: the boards are built in front of this transform, facing the level-select camera.")]
    [SerializeField] private Transform mount;
    [Tooltip("Old level-select objects hidden at runtime (never deleted).")]
    [SerializeField] private GameObject[] legacyObjects;

    [Header("Art (reused from Office Level 1)")]
    [Tooltip("Plain corkboard prop (Synty SM_Prop_CorkBoard_03, the original level-select board; CorkBoard_01 has papers baked into its mesh). Empty = simple built-in board.")]
    [SerializeField] private GameObject corkboardPrefab;
    [Tooltip("Name of the old level board in this scene; hidden once the new boards are built.")]
    [SerializeField] private string originalBoardName = "Corkboard Levels";

    [Header("Size")]
    [Tooltip("World size of one layout pixel at 16:9 (the layout is 1500 x 850 px).")]
    [SerializeField, Min(0.0001f)] private float boardScale = 0.0058f;

    private readonly List<Material> materials = new List<Material>();
    private readonly List<CareerPinnedPaper> chapterPapers = new List<CareerPinnedPaper>();
    private readonly List<CareerPinnedPaper> cards = new List<CareerPinnedPaper>();
    private readonly List<ChapterPaperView> chapterViews = new List<ChapterPaperView>();
    private readonly List<BadgeSlotView> badgeSlots = new List<BadgeSlotView>();
    private readonly List<Transform> chapterPips = new List<Transform>();

    private Transform boardRoot;
    private Transform strings;
    private Material embossed;
    private Material yarnMaterial;
    private TextMeshPro badgeAreaTitle;
    private CareerBoardButton backButton;
    private CareerChapterSheet sheet;
    private CareerPinnedPaper liftedPaper;

    private TextMeshPro expTotalLabel, heartsTotalLabel, expCareerValue, heartsCareerValue, chaptersLabel, badgeCountLabel;
    private RectTransform careerExpFill, careerHeartsFill;
    private TextMeshPro goalTitle, goalExpValue, goalHeartsValue, goalFooter;

    private CareerSnapshot snapshot;
    private float lastAspect = -1f;
    private bool wasVisible;
    private bool launchStarted;
    private bool progressDirty;

    // Count-up and bar animation.
    private float countElapsed = -1f;
    private int expFrom, heartsFrom;
    private float barElapsed = -1f;
    private float careerExpTarget, careerHeartsTarget;

    private CareerBoardTarget hoverTarget;
    private CareerBoardTarget pressTarget;
    private bool pressStartedOnNothing;

    private sealed class ChapterPaperView
    {
        public CareerPinnedPaper Paper;
        public TextMeshPro Heading, Number, Title, RecordExp, RecordHearts, Time, Status;
        public SpriteRenderer StarIcon, HeartIcon;
        public Transform Padlock;
    }

    private sealed class BadgeSlotView
    {
        public CareerPinnedPaper Paper;
        public CareerBadgeMedal Medal;
        public TextMeshPro Number, Name, Detail;
        public Transform Vacant;
        public TextMeshPro VacantNumber;
    }

    public bool IsBuilt => boardRoot != null;
    public CareerChapterSheet Sheet => sheet;
    public IReadOnlyList<CareerPinnedPaper> ChapterPapers => chapterPapers;
    public CareerBoardButton BackButton => backButton;
    public CareerSnapshot Snapshot => snapshot;
    public int BadgeSlotCount => badgeSlots.Count;
    public int StringCount => strings != null ? strings.childCount / 2 : 0;

    /// <summary>The badge paper in a slot, or null when the slot is empty (editor checks).</summary>
    public CareerPinnedPaper BadgePaper(int slot) =>
        slot >= 0 && slot < badgeSlots.Count && badgeSlots[slot].Paper.gameObject.activeSelf ? badgeSlots[slot].Paper : null;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void OnEnable()
    {
        PlayerProgressManager.ProgressChanged += HandleProgressChanged;
        if (Application.isPlaying) BuildBoard();
    }

    private void OnDisable()
    {
        PlayerProgressManager.ProgressChanged -= HandleProgressChanged;
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
    }

    private void HandleProgressChanged(PlayerProgressData _) => progressDirty = true;

    private void Update()
    {
        if (!Application.isPlaying || boardRoot == null) return;
        ApplyAspect();

        // The papers drop in as soon as the level-select camera shows the board;
        // touch only works once the camera has settled.
        bool visible = IsBoardVisible();
        bool ready = visible && CanInteractNow();
        if (visible && !wasVisible) ShowBoard(animate: true);
        if (!visible && wasVisible) sheet.HideNow();
        if (!ready) ClearPointer();
        wasVisible = visible;

        if (progressDirty && ready)
        {
            progressDirty = false;
            CareerSnapshot fresh = CareerProgressService.Current();
            CareerChanges changes = CareerProgress.Diff(CareerProgressService.LoadSeen(), fresh);
            Apply(fresh, changes, celebrate: true, introDelay: -1f);
            CareerProgressService.SaveSeen(fresh);
        }

        ApplyInputGate(ready);
        if (ready && !launchStarted)
        {
            HandlePointer();
            if (BackPressed()) GoBack();
        }
        TickCounters(Time.unscaledDeltaTime);
    }

    /// <summary>Builds the boards once (also from the component menu, for previews).</summary>
    [ContextMenu("Build / Refresh Career Board")]
    public void BuildBoard()
    {
        HideLegacy();
        if (boardRoot != null)
        {
            ShowBoard(animate: false);
            return;
        }

        Transform parent = mount != null ? mount : transform;
        Transform stale = parent.Find("Career Board 3D");
        if (stale != null) CareerBoardKit.Dispose(stale.gameObject);

        boardRoot = CareerBoardKit.Node("Career Board 3D", parent, Vector3.zero);
        boardRoot.localRotation = Quaternion.identity;
        lastAspect = -1f;
        ApplyAspect();

        embossed = CareerBoardKit.EmbossedMaterial(HudTheme.Font, materials);
        yarnMaterial = CareerBoardKit.Lit(new Color32(176, 36, 44, 255), materials, 0.1f);
        BuildProgressBoard();
        BuildStoryBoard();
        backButton = CareerBoardButton.Create(boardRoot, "Back Button", "<  BACK", new Vector3(-690f, 420f, -10f),
            new Vector2(150f, 56f), HudTheme.Brass, new Color32(40, 30, 14, 255), 22f);
        backButton.Tapped += _ => GoBack();
        sheet = CareerChapterSheet.Create(boardRoot);
        sheet.StartRequested += LaunchChapter;
        sheet.Closed += HandleSheetClosed;

        HideOriginalBoard();
        ShowBoard(animate: false);
    }

    /// <summary>Editor captures: fills the board from sample progress (no save is read or written).</summary>
    public void RefreshPreview(PlayerProgressData previewProgress, int openChapter = 0)
    {
        if (boardRoot == null) BuildBoard();
        CareerSnapshot preview = CareerProgressService.Build(previewProgress);
        Apply(preview, CareerProgress.Diff(null, preview), celebrate: false, introDelay: -1f);
        foreach (CareerPinnedPaper paper in AllPapers()) paper.SnapToRest();
        sheet.HideNow();
        if (openChapter >= 1 && openChapter <= chapterPapers.Count)
        {
            OpenChapter(chapterPapers[openChapter - 1]);
            sheet.SnapOpen();
        }
        ApplyInputGate(true);
    }

    // =========================================================
    // BUILD: CAREER BOARD
    // =========================================================

    private void BuildProgressBoard()
    {
        Vector2 c = ProgressBoardCenter;
        Vector2 s = ProgressBoardSize;
        BuildCorkboard("Progress Corkboard", c, s);
        BuildPlate("Career Plate", "CAREER", new Vector3(c.x, c.y + s.y * 0.5f, -14f), 220f);
        const float z = -3f;
        const float left = -CardWidth * 0.5f + 20f;
        const float right = CardWidth * 0.5f - 20f;

        // CAREER (the big card): totals with bars against every open chapter's maximum, and chapters passed.
        CareerPinnedPaper career = Card("Career Card", new Vector2(c.x, 246f), CareerCardHeight, -1.2f, "CAREER");
        Transform a = career.Body;
        CareerBoardKit.Sprite(a, "EXP icon", HudTheme.Star, new Vector3(left + 18f, 92f, z), 36f, HudTheme.Exp, 5);
        expTotalLabel = InkText(a, "EXP total", "", 40f, new Vector3(left + 46f + 130f, 92f, z), 260f, 46f, TextAlignmentOptions.MidlineLeft);
        careerExpFill = CareerBoardKit.Bar(a, "Career EXP bar", new Vector3(0f, 56f, -3.5f), right - left, 20f, HudTheme.Exp, out _);
        InkText(a, "EXP caption", "EXP COLLECTED", 14f, new Vector3(left + 80f, 30f, z), 160f, 20f, TextAlignmentOptions.MidlineLeft,
            CareerBoardKit.InkCard).characterSpacing = 2f;
        expCareerValue = InkText(a, "EXP of max", "", 16f, new Vector3(right - 70f, 30f, z), 140f, 22f, TextAlignmentOptions.MidlineRight);

        CareerBoardKit.Sprite(a, "Hearts icon", HudTheme.Heart, new Vector3(left + 18f, -8f, z), 34f, HudTheme.Reputation, 5);
        heartsTotalLabel = InkText(a, "Hearts total", "", 40f, new Vector3(left + 46f + 130f, -8f, z), 260f, 46f, TextAlignmentOptions.MidlineLeft);
        careerHeartsFill = CareerBoardKit.Bar(a, "Career hearts bar", new Vector3(0f, -44f, -3.5f), right - left, 20f, HudTheme.Reputation, out _);
        InkText(a, "Hearts caption", "REPUTATION HEARTS", 14f, new Vector3(left + 90f, -70f, z), 180f, 20f, TextAlignmentOptions.MidlineLeft,
            CareerBoardKit.InkCard).characterSpacing = 2f;
        heartsCareerValue = InkText(a, "Hearts of max", "", 16f, new Vector3(right - 60f, -70f, z), 120f, 22f, TextAlignmentOptions.MidlineRight);

        CareerBoardKit.Box(a, "Divider", new Vector3(0f, -92f, -2.4f), new Vector3(right - left, 1.6f, 0.5f), new Color32(206, 200, 184, 255), materials);
        chaptersLabel = InkText(a, "Chapters", "", 17f, new Vector3(left + 80f, -116f, z), 160f, 24f, TextAlignmentOptions.MidlineLeft);
        for (int i = 0; i < 5; i++)
        {
            Transform pip = CareerBoardKit.Pushpin(a, new Vector3(right - 128f + i * 32f, -116f, z), HudTheme.Empty, materials, 0.7f);
            pip.name = "Chapter pip " + (i + 1);
            chapterPips.Add(pip);
        }

        // NEXT GOAL (smaller): the chapter to play next and the EXP / hearts it needs, as counts.
        CareerPinnedPaper goal = Card("Goal Card", new Vector2(c.x + 4f, -60f), GoalCardHeight, 1.2f, "NEXT GOAL");
        Transform b = goal.Body;
        float top = GoalCardHeight * 0.5f;
        goalTitle = InkText(b, "Goal title", "", 22f, new Vector3(0f, top - 50f, z), right - left, 30f, TextAlignmentOptions.MidlineLeft);
        CareerBoardKit.Sprite(b, "Goal EXP icon", HudTheme.Star, new Vector3(left + 14f, top - 86f, z), 28f, HudTheme.Exp, 5);
        goalExpValue = InkText(b, "Goal EXP value", "", 24f, new Vector3(left + 36f + 130f, top - 86f, z), 260f, 30f, TextAlignmentOptions.MidlineLeft);
        CareerBoardKit.Sprite(b, "Goal hearts icon", HudTheme.Heart, new Vector3(left + 14f, top - 120f, z), 26f, HudTheme.Reputation, 5);
        goalHeartsValue = InkText(b, "Goal hearts value", "", 24f, new Vector3(left + 36f + 130f, top - 120f, z), 260f, 30f, TextAlignmentOptions.MidlineLeft);
        goalFooter = InkText(b, "Goal footer", "", 14f, new Vector3(0f, top - 156f, z), right - left, 36f, TextAlignmentOptions.TopLeft,
            CareerBoardKit.InkCard, FontStyles.Normal);

        // BADGES (smallest): a plain sample coin (not any real side-quest badge) and the count, centred.
        CareerPinnedPaper badges = Card("Badges Card", new Vector2(c.x - 3f, -256f), BadgeCardHeight, -0.8f, "BADGES");
        Transform d = badges.Body;
        BuildSampleMedal(d, new Vector3(-26f, -12f, z), 56f);
        badgeCountLabel = InkText(d, "Badge count", "", 44f, new Vector3(48f, -12f, z), 90f, 52f, TextAlignmentOptions.MidlineLeft);
    }

    private CareerPinnedPaper Card(string name, Vector2 pin, float height, float tilt, string heading)
    {
        CareerPinnedPaper card = CareerPinnedPaper.Create(boardRoot, name, new Vector3(pin.x, pin.y, -8f),
            new Vector2(CardWidth, height), tilt, CareerBoardKit.PaperWhite, CareerBoardKit.PinRed, interactive: false);
        card.SetSway(0.35f);
        TextMeshPro title = InkText(card.Body, "Heading", heading, 17f, new Vector3(-CardWidth * 0.5f + 100f, height * 0.5f - 15f, -2.6f),
            160f, 24f, TextAlignmentOptions.MidlineLeft, CareerBoardKit.InkCard);
        title.characterSpacing = 4f;
        cards.Add(card);
        return card;
    }

    /// <summary>Generic gold coin with a star: stands for "badges" on the career card only.</summary>
    private void BuildSampleMedal(Transform parent, Vector3 center, float size)
    {
        CareerBoardKit.Sprite(parent, "Sample medal rim", HudTheme.Circle, center + new Vector3(1f, -1.5f, 0.4f), size, new Color32(150, 104, 30, 255), 4);
        CareerBoardKit.Sprite(parent, "Sample medal", HudTheme.Circle, center, size * 0.92f, HudTheme.Exp, 5);
        CareerBoardKit.Sprite(parent, "Sample medal face", HudTheme.Circle, center + new Vector3(0f, 0f, -0.2f), size * 0.7f, new Color32(214, 150, 50, 255), 6);
        CareerBoardKit.Sprite(parent, "Sample medal star", HudTheme.Star, center + new Vector3(0f, 0f, -0.4f), size * 0.48f, new Color32(255, 244, 210, 255), 7);
    }

    // =========================================================
    // BUILD: STORY BOARD
    // =========================================================

    private void BuildStoryBoard()
    {
        Vector2 c = StoryBoardCenter;
        Vector2 s = StoryBoardSize;
        BuildCorkboard("Story Corkboard", c, s);
        BuildPlate("Story Plate", "STORY", new Vector3(c.x, c.y + s.y * 0.5f, -14f), 220f);
        CareerBoardKit.Text(boardRoot, "Hint", "Tap a pinned paper", 17f, new Vector3(c.x + s.x * 0.5f - 175f, c.y + s.y * 0.5f - 52f, -4f),
            280f, 26f, CareerBoardKit.Cream, TextAlignmentOptions.MidlineRight, FontStyles.Bold, null, embossed);

        strings = CareerBoardKit.Node("Chapter Strings", boardRoot, Vector3.zero);

        for (int i = 0; i < ChapterPins.Length; i++)
        {
            Vector2 pin = ChapterPins[i];
            CareerPinnedPaper paper = CareerPinnedPaper.Create(boardRoot, $"Chapter {i + 1} Paper", new Vector3(pin.x, pin.y, -8f),
                ChapterPaperSize, ChapterTilts[i], CareerBoardKit.PaperYellow, CareerBoardKit.PinRed, interactive: true);
            paper.Tapped += target => OpenChapter((CareerPinnedPaper)target);
            chapterPapers.Add(paper);
            chapterViews.Add(BuildChapterFace(paper));
        }

        badgeAreaTitle = CareerBoardKit.Text(boardRoot, "Badge area title", "SIDE QUEST BADGES", 16f, new Vector3(-155f, 36f, -4f),
            260f, 24f, CareerBoardKit.Cream, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, null, embossed);
        badgeAreaTitle.characterSpacing = 3f;
        for (int i = 0; i < BadgePins.Length; i++) badgeSlots.Add(BuildBadgeSlot(i));
    }

    private ChapterPaperView BuildChapterFace(CareerPinnedPaper paper)
    {
        Transform body = paper.Body;
        const float z = -2.6f;
        var view = new ChapterPaperView { Paper = paper };
        view.Heading = InkText(body, "Heading", "CHAPTER", 13f, new Vector3(0f, 78f, z), 170f, 20f, TextAlignmentOptions.Center,
            CareerBoardKit.InkSoft);
        view.Heading.characterSpacing = 5f;
        view.Number = CareerBoardKit.Text(body, "Number", "1", 80f, new Vector3(0f, 36f, z), 170f, 96f, CareerBoardKit.Ink,
            TextAlignmentOptions.Center, FontStyles.Bold, HudTheme.HandwritingFont);
        view.Title = InkText(body, "Title", "", 21f, new Vector3(0f, -14f, z), 180f, 30f, TextAlignmentOptions.Center);
        view.StarIcon = CareerBoardKit.Sprite(body, "Record EXP icon", HudTheme.Star, new Vector3(-74f, -42f, z), 18f, HudTheme.Exp, 5);
        view.RecordExp = InkText(body, "Record EXP", "", 16f, new Vector3(-26f, -42f, z), 74f, 22f, TextAlignmentOptions.MidlineLeft,
            CareerBoardKit.InkSoft);
        view.HeartIcon = CareerBoardKit.Sprite(body, "Record hearts icon", HudTheme.Heart, new Vector3(10f, -42f, z), 17f,
            HudTheme.Reputation, 5);
        view.RecordHearts = InkText(body, "Record hearts", "", 16f, new Vector3(58f, -42f, z), 74f, 22f, TextAlignmentOptions.MidlineLeft,
            CareerBoardKit.InkSoft);
        view.Time = InkText(body, "Time", "", 14f, new Vector3(0f, -64f, z), 180f, 20f, TextAlignmentOptions.Center, CareerBoardKit.InkSoft);
        view.Status = InkText(body, "Status", "", 17f, new Vector3(0f, -94f, z), 180f, 26f, TextAlignmentOptions.Center);
        view.Status.characterSpacing = 3f;
        view.Padlock = CareerBoardKit.Padlock(body, new Vector3(70f, 78f, -8f), 30f, materials);
        return view;
    }

    /// <summary>One badge space: a small paper with its medal pinned on top (tap = open its chapter), or an empty spot.</summary>
    private BadgeSlotView BuildBadgeSlot(int index)
    {
        Vector2 pin = BadgePins[index];
        var view = new BadgeSlotView();
        CareerPinnedPaper paper = CareerPinnedPaper.Create(boardRoot, $"Badge {index + 1} Paper", new Vector3(pin.x, pin.y, -8f),
            BadgePaperSize, BadgeTilts[index], CareerBoardKit.PaperWhite, CareerBoardKit.PinBlue, interactive: true);
        paper.SetSway(0.7f);
        paper.Tapped += target => OpenBadge((CareerPinnedPaper)target);
        view.Paper = paper;

        Transform body = paper.Body;
        const float z = -2.6f;
        float top = BadgePaperSize.y * 0.5f;
        // The medal hangs from the paper's own pin, covering the top of the paper.
        view.Medal = CareerBadgeMedal.Create(body, "Medal", new Vector3(0f, top - 16f, -6f), 84f);
        view.Number = InkText(body, "Number", "", 12f, new Vector3(0f, top - 114f, z), 108f, 16f, TextAlignmentOptions.Center,
            new Color32(40, 110, 190, 255));
        view.Number.characterSpacing = 2f;
        view.Name = InkText(body, "Name", "", 14f, new Vector3(0f, top - 136f, z), 108f, 32f, TextAlignmentOptions.Center);
        view.Detail = InkText(body, "Detail", "", 12f, new Vector3(0f, top - 159f, z), 110f, 16f, TextAlignmentOptions.Center,
            CareerBoardKit.InkSoft);

        // Empty space: a pin hole, a faint outline and a question mark on the cork.
        Transform vacant = CareerBoardKit.Node($"Badge {index + 1} Empty", boardRoot, new Vector3(pin.x, pin.y - (top - 18f), -1f));
        vacant.localRotation = Quaternion.Euler(0f, 0f, BadgeTilts[index]);
        Material outline = CareerBoardKit.Lit(new Color32(96, 72, 46, 255), materials, 0.05f);
        float w = BadgePaperSize.x, h = BadgePaperSize.y, t = 3f;
        CareerBoardKit.Box(vacant, "Outline top", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, t, 1f), outline);
        CareerBoardKit.Box(vacant, "Outline bottom", new Vector3(0f, -h * 0.5f, 0f), new Vector3(w, t, 1f), outline);
        CareerBoardKit.Box(vacant, "Outline left", new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h, 1f), outline);
        CareerBoardKit.Box(vacant, "Outline right", new Vector3(w * 0.5f, 0f, 0f), new Vector3(t, h, 1f), outline);
        CareerBoardKit.Box(vacant, "Pin hole", new Vector3(0f, top - 18f, -0.6f), new Vector3(6f, 6f, 1f), new Color32(40, 28, 18, 255),
            materials, PrimitiveType.Sphere);
        CareerBoardKit.Text(vacant, "Question", "?", 56f, new Vector3(0f, 10f, -1f), 90f, 70f, CareerBoardKit.Cream,
            TextAlignmentOptions.Center, FontStyles.Bold, null, embossed);
        view.VacantNumber = CareerBoardKit.Text(vacant, "Label", "", 12f, new Vector3(0f, -58f, -1f), 108f, 34f, CareerBoardKit.Cream,
            TextAlignmentOptions.Center, FontStyles.Bold, null, embossed);
        view.Vacant = vacant;
        return view;
    }

    // =========================================================
    // BUILD: SHARED
    // =========================================================

    private void BuildCorkboard(string name, Vector2 center, Vector2 size)
    {
        Vector3 position = new Vector3(center.x, center.y, 18f);
        // 1. The scene's own plain level board (SM_Prop_CorkBoard_03): just its mesh, without the old notes
        //    pinned to it. This keeps the boards plain even if the scene still points at a papered prop.
        Transform board = OriginalBoardMesh(out Mesh mesh, out Material[] sharedMaterials)
            ? CareerBoardKit.MeshProp(mesh, sharedMaterials, boardRoot, name, position, size.x, size.y, 34f)
            : null;
        // 2. The corkboard prefab.
        if (board == null) board = CareerBoardKit.Prop(corkboardPrefab, boardRoot, name, position, size.x, size.y, 34f);
        if (board != null) return;

        // Built-in fallback: aluminium frame around a cork panel.
        Transform root = CareerBoardKit.Node(name, boardRoot, position);
        CareerBoardKit.Box(root, "Frame", Vector3.zero, new Vector3(size.x, size.y, 34f), new Color32(128, 132, 136, 255), materials);
        CareerBoardKit.Box(root, "Cork", new Vector3(0f, 0f, -14f), new Vector3(size.x - 44f, size.y - 44f, 10f),
            new Color32(150, 112, 70, 255), materials);
    }

    /// <summary>The mesh of the scene's original level board (its own renderer only, not its child notes).</summary>
    private bool OriginalBoardMesh(out Mesh mesh, out Material[] sharedMaterials)
    {
        mesh = null;
        sharedMaterials = null;
        if (string.IsNullOrEmpty(originalBoardName) || transform.parent == null) return false;
        foreach (Transform child in transform.parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != originalBoardName) continue;
            MeshFilter filter = child.GetComponent<MeshFilter>();
            Renderer renderer = child.GetComponent<Renderer>();
            if (filter == null || filter.sharedMesh == null || renderer == null) continue;
            mesh = filter.sharedMesh;
            sharedMaterials = renderer.sharedMaterials;
            return true;
        }
        return false;
    }

    /// <summary>Navy name plate with a brass rim (the Level 1 HUD panel look) sitting on a board's top edge.</summary>
    private void BuildPlate(string name, string text, Vector3 center, float width)
    {
        Transform plate = CareerBoardKit.Node(name, boardRoot, center);
        CareerBoardKit.Box(plate, "Shadow", new Vector3(3f, -5f, 6f), new Vector3(width, 54f, 4f), CareerBoardKit.CorkShadow, materials);
        CareerBoardKit.Box(plate, "Rim", Vector3.zero, new Vector3(width, 54f, 10f), CareerBoardKit.Lit(HudTheme.Brass, materials, 0.6f));
        CareerBoardKit.Box(plate, "Face", new Vector3(0f, 0f, -4f), new Vector3(width - 10f, 44f, 6f), HudTheme.PanelInner, materials);
        CareerBoardKit.Box(plate, "Screw left", new Vector3(-width * 0.5f + 16f, 0f, -7.5f), new Vector3(7f, 7f, 2f),
            HudTheme.BrassSoft, materials, PrimitiveType.Sphere);
        CareerBoardKit.Box(plate, "Screw right", new Vector3(width * 0.5f - 16f, 0f, -7.5f), new Vector3(7f, 7f, 2f),
            HudTheme.BrassSoft, materials, PrimitiveType.Sphere);
        TextMeshPro label = CareerBoardKit.Text(plate, "Label", text, 26f, new Vector3(0f, 0f, -7.5f), width - 50f, 40f,
            HudTheme.BrassSoft);
        label.characterSpacing = 8f;
    }

    /// <summary>Red string between two chapter pins, sagging a little.</summary>
    private void Yarn(Vector3 from, Vector3 to)
    {
        Vector3 mid = (from + to) * 0.5f + Vector3.down * Mathf.Clamp((to - from).magnitude * 0.05f, 6f, 26f);
        Segment(from, mid);
        Segment(mid, to);
    }

    private void Segment(Vector3 a, Vector3 b)
    {
        Vector3 delta = b - a;
        MeshRenderer piece = CareerBoardKit.Box(strings, "Yarn", (a + b) * 0.5f, new Vector3(2.6f, delta.magnitude * 0.5f, 2.6f),
            yarnMaterial, PrimitiveType.Cylinder);
        piece.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
    }

    private TextMeshPro InkText(Transform parent, string name, string text, float size, Vector3 center, float width, float height,
        TextAlignmentOptions alignment, Color? color = null, FontStyles style = FontStyles.Bold) =>
        CareerBoardKit.Text(parent, name, text, size, center, width, height, color ?? CareerBoardKit.Ink, alignment, style);

    // =========================================================
    // DATA
    // =========================================================

    /// <summary>Reads the save, fills every view and plays the board-opening animation.</summary>
    private void ShowBoard(bool animate)
    {
        CareerSnapshot fresh = CareerProgressService.Current();
        CareerChanges changes = CareerProgress.Diff(CareerProgressService.LoadSeen(), fresh);
        Apply(fresh, changes, celebrate: animate, introDelay: animate ? 0.12f : -1f);
        if (animate) CareerProgressService.SaveSeen(fresh);
        progressDirty = false;
    }

    /// <summary>introDelay &lt; 0 = no intro (papers already pinned).</summary>
    private void Apply(CareerSnapshot data, CareerChanges changes, bool celebrate, float introDelay)
    {
        snapshot = data ?? new CareerSnapshot();
        bool intro = introDelay >= 0f && Application.isPlaying;

        ApplyCareerCard(changes, celebrate);
        ApplyGoalCard();
        barElapsed = intro || celebrate ? 0f : -1f;
        ApplyBars(barElapsed < 0f ? 1f : 0f);

        ApplyChapters(changes, celebrate, intro, introDelay);
        ApplyStrings();
        ApplyBadges(changes, celebrate, intro, introDelay);

        if (intro)
            for (int i = 0; i < cards.Count; i++) cards[i].PlayIntro(introDelay + i * 0.08f, sound: i == 0);
    }

    private void ApplyCareerCard(CareerChanges changes, bool celebrate)
    {
        expFrom = celebrate ? changes.ExpFrom : snapshot.TotalExp;
        heartsFrom = celebrate ? changes.HeartsFrom : snapshot.TotalHearts;
        countElapsed = expFrom < snapshot.TotalExp || heartsFrom < snapshot.TotalHearts ? 0f : -1f;
        SetTotals(expFrom, heartsFrom);

        bool available = snapshot.Available;
        careerExpTarget = snapshot.CareerMaxExp > 0 ? Mathf.Clamp01(snapshot.TotalExp / (float)snapshot.CareerMaxExp) : 0f;
        careerHeartsTarget = snapshot.CareerMaxHearts > 0 ? Mathf.Clamp01(snapshot.TotalHearts / (float)snapshot.CareerMaxHearts) : 0f;
        expCareerValue.text = available ? $"{snapshot.TotalExp:N0} / {snapshot.CareerMaxExp:N0}" : "--";
        heartsCareerValue.text = available ? $"{snapshot.TotalHearts:N0} / {snapshot.CareerMaxHearts:N0}" : "--";
        chaptersLabel.text = available ? $"CHAPTERS  {snapshot.Passed} / {snapshot.ChapterCount}" : "CHAPTERS  --";
        badgeCountLabel.text = available ? $"{snapshot.EarnedBadges}x" : "--";
        for (int i = 0; i < chapterPips.Count; i++)
        {
            CareerChapterView chapter = i < snapshot.Chapters.Count ? snapshot.Chapters[i] : null;
            chapterPips[i].gameObject.SetActive(chapter != null);
            if (chapter != null) SetPinColor(chapterPips[i], PipColor(chapter.Status));
        }
    }

    private void ApplyGoalCard()
    {
        CareerChapterView goal = snapshot.Goal;
        if (goal != null && snapshot.Available && goal.Status == CareerChapterStatus.ComingSoon)
        {
            goalTitle.text = $"Chapter {goal.Def.number}  ·  Coming soon";
            goalExpValue.text = $"{snapshot.TotalExp:N0} <size=15><color=#262932>EXP SO FAR</color></size>";
            goalHeartsValue.text = $"{snapshot.TotalHearts:N0} <size=15><color=#262932>HEARTS SO FAR</color></size>";
        }
        else if (goal != null && snapshot.Available)
        {
            goalTitle.text = $"Chapter {goal.Def.number}  ·  {goal.NoteTitle}";
            goalExpValue.text = $"{goal.BestExp:N0} / {goal.TargetExp:N0} <size=15><color=#262932>EXP</color></size>";
            goalHeartsValue.text = $"{goal.BestHearts:N0} / {goal.TargetHearts:N0} <size=15><color=#262932>HEARTS</color></size>";
        }
        else
        {
            goalTitle.text = snapshot.Available ? "No chapter open" : "Progress unavailable";
            goalExpValue.text = goalHeartsValue.text = "--";
        }
        goalFooter.text = CareerProgress.NextStep(snapshot);
    }

    private void ApplyBars(float progress)
    {
        CareerBoardKit.SetFill(careerExpFill, careerExpTarget * progress);
        CareerBoardKit.SetFill(careerHeartsFill, careerHeartsTarget * progress);
    }

    private void ApplyChapters(CareerChanges changes, bool celebrate, bool intro, float introDelay)
    {
        for (int i = 0; i < chapterViews.Count; i++)
        {
            ChapterPaperView view = chapterViews[i];
            CareerChapterView chapter = i < snapshot.Chapters.Count ? snapshot.Chapters[i] : null;
            view.Paper.gameObject.SetActive(chapter != null);
            if (chapter == null) continue;
            view.Paper.Data = chapter;

            CareerChapterStatus status = chapter.Status;
            bool closed = !chapter.IsOpen;
            view.Paper.SetPaperColor(status == CareerChapterStatus.ComingSoon ? CareerBoardKit.PaperBlue
                : status == CareerChapterStatus.Locked ? CareerBoardKit.PaperGrey : CareerBoardKit.PaperYellow);
            bool isGoal = chapter == snapshot.Goal && status != CareerChapterStatus.Passed && snapshot.Available;
            view.Paper.SetPinColor(isGoal ? CareerBoardKit.PinGold : closed ? new Color32(120, 126, 134, 255) : CareerBoardKit.PinRed);
            view.Paper.SetPulse(isGoal);
            view.Paper.SetSway(isGoal ? 1.4f : closed ? 0.5f : 0.9f);

            view.Number.text = chapter.Def.number.ToString();
            view.Number.color = closed ? CareerBoardKit.InkSoft : CareerBoardKit.Ink;
            view.Title.text = chapter.NoteTitle;
            view.Padlock.gameObject.SetActive(status == CareerChapterStatus.Locked);

            bool hasRecord = chapter.HasResult && snapshot.Available;
            view.RecordExp.text = hasRecord ? $"{chapter.BestExp:N0}" : "";
            view.RecordHearts.text = hasRecord ? $"{chapter.BestHearts:N0}" : "";
            view.StarIcon.gameObject.SetActive(hasRecord);
            view.HeartIcon.gameObject.SetActive(hasRecord);
            view.Time.text = hasRecord && chapter.BestSeconds > 0
                ? $"BEST TIME  {CareerProgress.FormatTime(chapter.BestSeconds)}"
                : hasRecord ? "TIME  --:--" : "";

            bool stamped = status == CareerChapterStatus.Passed || status == CareerChapterStatus.Finished;
            view.Status.gameObject.SetActive(!stamped);
            if (stamped)
            {
                bool slam = celebrate && (status == CareerChapterStatus.Passed ? changes.NewlyPassed.Contains(chapter.Def.id)
                    : changes.NewlyFinished.Contains(chapter.Def.id));
                view.Paper.ShowStamp(status == CareerChapterStatus.Passed ? "PASSED" : "TRY AGAIN",
                    CareerChapterSheet.StampColor(status), slam && Application.isPlaying, intro ? introDelay + 1.1f : 0.3f);
            }
            else
            {
                view.Paper.HideStamp();
                view.Status.text = status == CareerChapterStatus.Available ? "TAP TO PLAY"
                    : status == CareerChapterStatus.Locked ? "LOCKED" : "COMING SOON";
                view.Status.color = status == CareerChapterStatus.Available ? CareerChapterSheet.StampColor(status) : CareerBoardKit.InkSoft;
            }
            if (intro) view.Paper.PlayIntro(introDelay + 0.2f + i * 0.08f, sound: i % 2 == 0);
        }
    }

    /// <summary>A red string from each chapter to the next one once that next chapter is unlocked.</summary>
    private void ApplyStrings()
    {
        for (int i = strings.childCount - 1; i >= 0; i--) CareerBoardKit.Dispose(strings.GetChild(i).gameObject);
        int count = Mathf.Min(snapshot.Chapters.Count, ChapterPins.Length);
        for (int i = 0; i + 1 < count; i++)
        {
            if (!snapshot.Chapters[i + 1].IsOpen) continue;
            Yarn(new Vector3(ChapterPins[i].x, ChapterPins[i].y, -20f), new Vector3(ChapterPins[i + 1].x, ChapterPins[i + 1].y, -20f));
        }
    }

    private void ApplyBadges(CareerChanges changes, bool celebrate, bool intro, float introDelay)
    {
        int slots = Mathf.Min(badgeSlots.Count, Mathf.Max(1, snapshot.BadgeSlots));
        badgeAreaTitle.text = $"SIDE QUEST BADGES  {snapshot.EarnedBadges} / {slots}";
        for (int i = 0; i < badgeSlots.Count; i++)
        {
            BadgeSlotView view = badgeSlots[i];
            bool inUse = i < slots;
            CareerBadgeView badge = inUse && i < snapshot.Badges.Count ? snapshot.Badges[i] : null;
            bool earned = badge != null && badge.Tier > 0;
            view.Paper.gameObject.SetActive(earned);
            view.Vacant.gameObject.SetActive(inUse && !earned);
            if (!earned)
            {
                view.VacantNumber.text = $"SIDE QUEST {i + 1}\nNot yet found";
                continue;
            }

            view.Paper.Data = badge;
            view.Medal.SetBadge(badge.Emblem, badge.Tier, badge.Ribbon);
            view.Number.text = $"SIDE QUEST {badge.Number}";
            view.Name.text = badge.Name;
            string tier = badge.Tier > 0 ? CareerProgress.TierLabel(badge.Tier).ToUpperInvariant() : "NOT EARNED";
            view.Detail.text = badge.BestSeconds > 0 ? $"{tier}  ·  {CareerProgress.FormatTime(badge.BestSeconds)}" : tier;
            view.Detail.color = badge.Tier > 0 ? TierInk(badge.Tier) : CareerBoardKit.InkSoft;
            if (celebrate && changes.UpgradedBadges.Contains(badge.BadgeId)) view.Medal.Celebrate(intro ? introDelay + 1.4f : 0.3f);
            if (intro) view.Paper.PlayIntro(introDelay + 0.6f + i * 0.07f, sound: i == 0);
        }
    }

    private static Color TierInk(int tier)
    {
        switch (tier)
        {
            case 3: return new Color32(176, 120, 18, 255);
            case 2: return new Color32(98, 110, 128, 255);
            case 1: return new Color32(160, 84, 40, 255);
            default: return CareerBoardKit.InkSoft;
        }
    }

    private static Color PipColor(CareerChapterStatus status)
    {
        switch (status)
        {
            case CareerChapterStatus.Passed: return HudTheme.Complete;
            case CareerChapterStatus.Finished: return new Color32(232, 140, 60, 255);
            case CareerChapterStatus.Available: return HudTheme.Exp;
            default: return new Color32(150, 156, 164, 255);
        }
    }

    private static void SetPinColor(Transform pin, Color color)
    {
        foreach (Renderer renderer in pin.GetComponentsInChildren<Renderer>())
            if (renderer.name != "Needle" && renderer.name != "Shine") CareerBoardKit.SetColor(renderer.sharedMaterial, color);
    }

    private void SetTotals(int exp, int hearts)
    {
        bool available = snapshot != null && snapshot.Available;
        expTotalLabel.text = available ? $"{exp:N0} <size=18><color=#262932>EXP</color></size>" : "-- <size=18>EXP</size>";
        heartsTotalLabel.text = available ? $"{hearts:N0} <size=18><color=#262932>HEARTS</color></size>" : "-- <size=18>HEARTS</size>";
    }

    private void TickCounters(float dt)
    {
        if (countElapsed >= 0f && snapshot != null)
        {
            countElapsed += dt;
            float t = Mathf.Clamp01((countElapsed - 0.7f) / 1.2f);
            float eased = HudTheme.EaseOutCubic(t);
            SetTotals(Mathf.RoundToInt(Mathf.Lerp(expFrom, snapshot.TotalExp, eased)),
                Mathf.RoundToInt(Mathf.Lerp(heartsFrom, snapshot.TotalHearts, eased)));
            if (t >= 1f) countElapsed = -1f;
        }
        if (barElapsed >= 0f)
        {
            barElapsed += dt;
            float t = Mathf.Clamp01((barElapsed - 0.6f) / 0.9f);
            ApplyBars(HudTheme.EaseOutCubic(t));
            if (t >= 1f) barElapsed = -1f;
        }
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void HandlePointer()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || viewCamera == null) return;
        Vector2 position = pointer.position.ReadValue();
        bool pressed = pointer.press.wasPressedThisFrame;
        bool released = pointer.press.wasReleasedThisFrame;
        bool canHover = pointer is Mouse || pointer is Pen;

        if (pressed && MobileInputGuard.IsPointerOverUI(position))
        {
            ClearPointer();
            return;
        }

        CareerBoardTarget target = RaycastTarget(position);
        SetHoverTarget(canHover || pointer.press.isPressed ? target : null);

        if (pressed)
        {
            pressTarget = target;
            pressStartedOnNothing = target == null;
            if (pressTarget != null) pressTarget.SetPressed(true);
        }
        if (released)
        {
            CareerBoardTarget pressedTarget = pressTarget;
            if (pressedTarget != null) pressedTarget.SetPressed(false);
            if (pressedTarget != null && pressedTarget == target) HandleTap(target);
            else if (pressStartedOnNothing && target == null && sheet.IsOpen && !sheet.IsAnimating) sheet.Close();
            pressTarget = null;
            pressStartedOnNothing = false;
            if (!canHover) SetHoverTarget(null);
        }
    }

    private void HandleTap(CareerBoardTarget target)
    {
        if (sheet.IsOpen)
        {
            if (sheet.IsAnimating) return;
            if (!target.transform.IsChildOf(sheet.transform)) { sheet.Close(); return; }
        }
        if (target.Interactable) PlaySelectSound();
        target.Tap();
    }

    private CareerBoardTarget RaycastTarget(Vector2 screen)
    {
        Ray ray = viewCamera.ScreenPointToRay(screen);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            return null;
        CareerBoardTarget target = hit.collider.GetComponentInParent<CareerBoardTarget>();
        return target != null && target.transform.IsChildOf(boardRoot) ? target : null;
    }

    private void SetHoverTarget(CareerBoardTarget target)
    {
        if (hoverTarget == target) return;
        if (hoverTarget != null) hoverTarget.SetHover(false);
        hoverTarget = target;
        if (hoverTarget != null) hoverTarget.SetHover(true);
    }

    private void ClearPointer()
    {
        SetHoverTarget(null);
        if (pressTarget != null) pressTarget.SetPressed(false);
        pressTarget = null;
        pressStartedOnNothing = false;
    }

    private static bool BackPressed() =>
        Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

    private void ApplyInputGate(bool ready)
    {
        bool open = sheet != null && (sheet.IsOpen || sheet.IsAnimating);
        bool boardInput = ready && !launchStarted && !open;
        foreach (CareerPinnedPaper paper in chapterPapers) paper.Interactable = boardInput;
        foreach (BadgeSlotView slot in badgeSlots) slot.Paper.Interactable = boardInput && slot.Paper.Data != null;
        if (backButton != null) backButton.Interactable = ready && !launchStarted;
        if (sheet != null) sheet.SetInputEnabled(ready && !launchStarted && !sheet.IsAnimating);
    }

    private bool IsBoardVisible() => !Application.isPlaying || (cameraManager != null && cameraManager.IsLevelSelectorVisible);

    private bool CanInteractNow()
    {
        if (launchStarted) return false;
        if (!Application.isPlaying) return true;
        return cameraManager != null && cameraManager.IsLevelSelectorVisible && !cameraManager.IsTransitioning;
    }

    // =========================================================
    // ACTIONS
    // =========================================================

    private void OpenChapter(CareerPinnedPaper chapterPaper)
    {
        if (chapterPaper == null || !(chapterPaper.Data is CareerChapterView chapter)) return;
        CareerBadgeView badge = null;
        foreach (CareerBadgeView candidate in snapshot.Badges)
            if (candidate.Chapter == chapter) { badge = candidate; break; }
        OpenSheet(chapter, badge, chapterPaper);
    }

    /// <summary>A badge paper opens the chapter its side quest belongs to.</summary>
    private void OpenBadge(CareerPinnedPaper badgePaper)
    {
        if (badgePaper == null || !(badgePaper.Data is CareerBadgeView badge) || badge.Chapter == null) return;
        OpenSheet(badge.Chapter, badge, badgePaper);
    }

    /// <summary>Opens the sheet for a chapter, flying out of sourcePaper.</summary>
    private void OpenSheet(CareerChapterView chapter, CareerBadgeView badge, CareerPinnedPaper sourcePaper)
    {
        Vector3 from = boardRoot.InverseTransformPoint(sourcePaper.Body.position);
        liftedPaper = sourcePaper;
        sheet.Open(chapter, badge, snapshot.Available, from, Mathf.DeltaAngle(0f, sourcePaper.transform.localEulerAngles.z), sourcePaper.Size.x);
        sourcePaper.SetLifted(true);
        ClearPointer();
        ApplyInputGate(CanInteractNow());
    }

    private void HandleSheetClosed()
    {
        if (liftedPaper != null) liftedPaper.SetLifted(false);
        liftedPaper = null;
    }

    private void LaunchChapter(CareerChapterView chapter)
    {
        if (!Application.isPlaying || launchStarted || !CanInteractNow() || chapter == null || !chapter.Launchable) return;
        if (string.IsNullOrWhiteSpace(chapter.Def.scene)) return;
        launchStarted = true;
        ApplyInputGate(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayAchievementSFX();
        SceneManager.LoadScene(chapter.Def.scene);
    }

    private void GoBack()
    {
        if (!Application.isPlaying || !CanInteractNow()) return;
        if (sheet.IsOpen || sheet.IsAnimating)
        {
            if (!sheet.IsAnimating) sheet.Close();
            return;
        }
        if (cameraManager == null) return;
        PlaySelectSound();
        ClearPointer();
        cameraManager.SwitchToMainMenu();
    }

    private static void PlaySelectSound()
    {
        if (Application.isPlaying && AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_back_01", 0.65f);
    }

    // =========================================================
    // SCENE
    // =========================================================

    private void ApplyAspect()
    {
        if (boardRoot == null) return;
        float aspect = viewCamera != null && viewCamera.aspect > 0f ? viewCamera.aspect : DesignAspect;
        if (Mathf.Abs(aspect - lastAspect) < 0.001f) return;
        lastAspect = aspect;
        // Narrower screens (4:3 tablets) shrink the boards so both stay in view.
        boardRoot.localScale = Vector3.one * boardScale * Mathf.Min(1f, aspect / DesignAspect);
    }

    private void HideLegacy()
    {
        if (legacyObjects == null) return;
        foreach (GameObject legacy in legacyObjects)
            if (legacy != null) legacy.SetActive(false);
    }

    private void HideOriginalBoard()
    {
        if (string.IsNullOrEmpty(originalBoardName) || transform.parent == null) return;
        foreach (Transform child in transform.parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != originalBoardName) continue;
            if (transform.IsChildOf(child) || (mount != null && mount.IsChildOf(child))) continue;
            if (cameraManager != null && cameraManager.levelSelectorCam != null &&
                cameraManager.levelSelectorCam.transform.IsChildOf(child)) continue;
            child.gameObject.SetActive(false);
        }
    }

    private IEnumerable<CareerPinnedPaper> AllPapers()
    {
        foreach (CareerPinnedPaper card in cards) yield return card;
        foreach (CareerPinnedPaper paper in chapterPapers) yield return paper;
        foreach (BadgeSlotView slot in badgeSlots) yield return slot.Paper;
    }
}
