using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The income toolkit on Rosa's market records desk: the hands-on tutorial
/// (T05) and the scored Mark the Money! challenge (T06), built from Unity
/// primitives. A navy case whose lid opens (~100 degrees, 0.45 s) to show a
/// scoreboard inside; a big highlighter; TYPE and TREATMENT note cards and
/// record tags dealt out beside a clipboard; an evidence clip; record tabs
/// and a CHECK button for the challenge.
///
/// PLAY:
/// - Highlight: press on a printed line and swipe across it (or pick up the
///   highlighter and sweep it over the line). The ink appears only where the
///   nib passes; a deliberate ~quarter-line sweep finishes the stroke with a
///   burst and a SOURCE flag. A plain tap draws it for you (accessible).
/// - Notes: tap a card then its place, or drag it there. Fitting places
///   light up while a card is held (fits, never "correct"). Landing squashes,
///   sparkles and ticks; a wrong-kind place shakes the card back.
/// - Tutorial (T0 payslip): Arthur demonstrates once, then the player repeats
///   it with an immediate rule line per action. Nothing is scored.
/// - Challenge (E1-E4): switch records with the tabs (each shows three
///   progress pips). CHECK with anything missing only says what is missing.
///   The first complete check is frozen (IncomeChallenge): pips flip green or
///   red one by one, the lid scoreboard counts up and shows the medal tier.
///   Right notes lock; red notes give a hint when tapped; checking again is
///   free. All sixteen right: confetti, the Annotated Income List, everything
///   packs into the case, the lid shuts, then the award (once) and the medal card.
///
/// OWNERSHIP: input is read directly (like the desk stamps) only while
/// seated at this desk, no demonstration or sequence is running and nothing
/// is paused. Leaving the desk closes the case and cancels a held card or
/// stroke; the work stays in the review state and is restored on return.
/// Teaching waits on TeachingPaused, never on Time.timeScale.
/// </summary>
public sealed class IncomeToolkit : MonoBehaviour
{
    public const string ChildName = "IncomeToolkit";

    private const float LidOpenSeconds = 0.45f;
    private const float LidOpenAngle = 100f;
    private const float CardFlySeconds = 0.22f;
    private const float SettleSeconds = 0.12f;
    private const float MarkerMoveSeconds = 0.16f;
    private const float TraceSeconds = 0.22f;
    private const float MarkerScale = 1.8f;
    private const float NibOffset = 0.064f;
    private const float DragThreshold = 12f;
    private const float CardLift = 0.03f;
    private const float DragHeight = 0.07f;
    public const float PlacedScale = 0.70f;
    public const float PaperScale = 1.15f;
    public const float NoteCardDepth = 0.072f;
    public const float CardVisualScale = 1.32f;
    // Cards kept in the case shrink to nothing and hide, so no tiny cards sit in the open case.
    private const float BoxScale = 0.02f;
    // Raised a little (toward the top of the view) so Arthur's bubble at the bottom never covers the paper or pen.
    private static readonly Vector3 PaperCenter = new Vector3(0.04f, 0.052f, 0.025f);
    private static readonly Vector3 CheckButtonHome = new Vector3(0.5f, 0f, -0.3f);

    private static readonly Color CaseColor = new Color32(30, 52, 74, 255);
    private static readonly Color Felt = new Color32(40, 92, 70, 255);
    private static readonly Color Brass = new Color32(206, 170, 96, 255);
    private static readonly Color CardColor = new Color32(252, 249, 240, 255);
    private static readonly Color TagColor = new Color32(240, 222, 182, 255);
    private static readonly Color Ink = new Color32(36, 32, 30, 255);
    private static readonly Color PipEmpty = new Color32(205, 198, 184, 255);
    private static readonly Color PipFilled = new Color32(120, 112, 100, 255);

    private enum Mode { None, Tutorial, Challenge, Done }

    private ToolkitBuilder builder;
    private ToolkitFx fx;
    private Transform caseRoot;
    private Transform lid;
    private Transform marker;
    private Vector3 markerRestPosition;
    private Quaternion markerRestRotation;
    private Vector3 markerPickupPosition;
    private Quaternion markerPickupRotation;
    private bool markerPickedUp;
    private BoxCollider markerHit;
    private Transform clip;
    private Transform clipJaw;
    private Vector3 clipRestPosition;
    private Quaternion clipRestRotation;
    private Transform clipboard;
    private readonly Dictionary<string, AnnotationPaper> papers = new Dictionary<string, AnnotationPaper>();
    private AnnotationPaper paper;
    private readonly List<AnnotationCard> cards = new List<AnnotationCard>();

    // Challenge furniture.
    private readonly List<Tab> tabs = new List<Tab>();
    private Transform checkButton;
    private Transform checkCap;
    private Material checkCapMaterial;
    private Material checkGlowMaterial;
    private Transform listSheet;
    private TextMeshPro scoreTitle, scoreStatus, scoreCount, scoreTier;
    private readonly Renderer[] scorePips = new Renderer[16];
    private readonly Material[] scorePipMaterials = new Material[16];
    private SpriteRenderer scoreMedal;

    private sealed class Tab
    {
        public string entryId;
        public Transform root;
        public Transform body;
        public Material face;
        public Material[] pips = new Material[4];
        public Transform done;
        public float flashUntil;
    }

    private CaseDefinition definition;
    private AnnotationDefinition options;
    private AnnotationEntryDefinition entry;
    private Mode mode;

    private bool wantOpen;
    private bool open;
    private bool demoRunning;
    private SideQuestSplash challengeSplash;
    private int busy;
    private Coroutine flow;
    private Coroutine pageMotion;
    private Coroutine lidMotion;
    private float lidAngle;
    private float placedUntil = -1f;
    private float idleSince;

    // Pointer.
    private bool pressValid;
    private Vector2 pressScreen;
    private ToolkitTarget pressTarget;
    private AnnotationCard dragging;
    private AnnotationCard selected;
    private AnnotationSlot? hoverSlot;

    // Highlighter.
    private readonly HighlighterSweep sweep = new HighlighterSweep();
    private bool inking;
    private bool inkFromMarker;
    private bool inkMoved;
    private float lastTickCoverage;
    private Coroutine markerMotion;

    public bool IsOpen => open;

    private static IncomeToolkit active;

    /// <summary>The practice or the challenge is under way at an open records desk (not after it is done).</summary>
    public static bool QuestInProgress => active != null && active.open && (active.mode == Mode.Tutorial || active.mode == Mode.Challenge);

    private static bool AtRecordsDesk() => active != null && active.open;

    /// <summary>The Side Quest Complete card or the annotated-paper collection is on screen: Exit Desk must not sit over them.</summary>
    private static bool HoldingExitAfterQuest =>
        AtRecordsDesk() && (HudRewardScreen.AnyOpen || (Level2AnnotatedPapers.Instance != null && Level2AnnotatedPapers.Instance.IsCollecting));
    public bool DemoRunning => demoRunning;

    /// <summary>Corrected per-record annotations, with no reconciled total.</summary>
    public List<string> GetAnnotatedRows() => IncomeChallenge.AnnotatedList(Challenge, options);

    /// <summary>The guide's compulsory teaching waits while any of these is true (no Time.timeScale change).</summary>
    public static bool TeachingPaused =>
        InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive ||
        (GameStateMachine.Instance != null && GameStateMachine.Instance.IsPaused);

    private bool AtDesk => CameraController.Instance != null &&
                           CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation;

    private bool Paused => TeachingPaused || SideQuestSplash.IsShowing ||
                           (Level2AnnotatedPapers.Instance != null && Level2AnnotatedPapers.Instance.IsCollecting) || !AtDesk;

    private static CaseReviewState Review => Level2OfficeStart.Review;

    // =========================================================
    // LAYOUT (toolkit-local, +z away from the seat; tested)
    // =========================================================

    /// <summary>Card resting spots: TYPE and TREATMENT columns left of the clipboard, record tags right of it.</summary>
    public static Vector3 CardHome(AnnotationSlot kind, int row) => kind == AnnotationSlot.Evidence
        ? new Vector3(row < 3 ? 0.38f : 0.59f, 0.004f, 0.105f - (row % 3) * 0.105f)
        : new Vector3(kind == AnnotationSlot.Type ? -0.53f : -0.315f, 0.004f, 0.115f - row * 0.108f);

    /// <summary>Two-line captions keep the complete labels at one readable size.</summary>
    public static string CardCaption(string id, string fallback)
    {
        switch (id)
        {
            case "compensation": return "Compensation\nincome";
            case "business-service": return "Business /\nservice income";
            case "rental": return "Rental\nincome";
            case "passive-interest": return "Passive income\n(interest)";
            case "borrowed": return "Borrowed\nfunds";
            case "regular-schedule": return "Regular income\nschedule";
            case "separate-rule": return "Separate\nrule";
            case "statutory-exclusion": return "Statutory\nexclusion";
            case "not-income": return "Not\nincome";
            case "payslip-ps0430": return "Payslip\nPS-0430";
            case "company-id": return "Company ID\ncard";
            case "bank-passbook": return "Bank passbook\npage";
            case "D1": return "D1 Collection\nsheet";
            case "D2": return "D2 Bank\nstatement";
            case "D3": return "D3 Receipt\nCM-0526";
            case "D4": return "D4 Rental receipt\nRA-0520";
            case "D5": return "D5 Loan agreement\nLN-0510";
            case "D6": return "D6 Cash receipt\nRC-0531";
            default: return fallback ?? string.Empty;
        }
    }

    // =========================================================
    // BUILD
    // =========================================================

    /// <summary>Builds the case, marker, clip and clipboard on the records desk.</summary>
    public static IncomeToolkit Create(Transform desk, float deskTop)
    {
        Transform existing = desk.Find(ChildName);
        if (existing != null && existing.GetComponent<IncomeToolkit>() != null) return existing.GetComponent<IncomeToolkit>();
        var root = new GameObject(ChildName);
        root.layer = Level2DeskStamps.DragLayer;
        root.transform.SetParent(desk, false);
        root.transform.localPosition = new Vector3(0f, deskTop, 0f);
        IncomeToolkit toolkit = root.AddComponent<IncomeToolkit>();
        toolkit.builder = new ToolkitBuilder(Level2DeskStamps.DragLayer);
        toolkit.fx = new ToolkitFx(root.transform, toolkit.builder);
        toolkit.BuildCase();
        return toolkit;
    }

    private void BuildCase()
    {
        ToolkitBuilder b = builder;
        Material shell = b.Lit(CaseColor, "Case"), brass = b.Lit(Brass, "Brass"), felt = b.Lit(Felt, "Felt");
        caseRoot = b.Node("Case", transform, new Vector3(-0.5f, 0f, 0.27f), Quaternion.identity);
        const float w = 0.26f, h = 0.07f, d = 0.15f, wall = 0.008f;
        b.Box("Bottom", caseRoot, new Vector3(0f, 0.004f, 0f), new Vector3(w, 0.008f, d), shell);
        b.Box("Front", caseRoot, new Vector3(0f, h * 0.5f, -d * 0.5f + wall * 0.5f), new Vector3(w, h, wall), shell);
        b.Box("Back", caseRoot, new Vector3(0f, h * 0.5f, d * 0.5f - wall * 0.5f), new Vector3(w, h, wall), shell);
        b.Box("Left", caseRoot, new Vector3(-w * 0.5f + wall * 0.5f, h * 0.5f, 0f), new Vector3(wall, h, d), shell);
        b.Box("Right", caseRoot, new Vector3(w * 0.5f - wall * 0.5f, h * 0.5f, 0f), new Vector3(wall, h, d), shell);
        b.Box("Felt", caseRoot, new Vector3(0f, 0.01f, 0f), new Vector3(w - wall * 2f, 0.002f, d - wall * 2f), felt);
        b.Box("Latch", caseRoot, new Vector3(0f, h * 0.75f, -d * 0.5f - 0.002f), new Vector3(0.03f, 0.014f, 0.004f), brass);
        b.Box("Trim", caseRoot, new Vector3(0f, h - 0.002f, -d * 0.5f), new Vector3(w, 0.004f, 0.004f), brass);

        // Lid hinged along the back top edge. It opens UP and back (positive
        // angle about +x), so its underside faces the seat: the scoreboard.
        lid = b.Node("LidHinge", caseRoot, new Vector3(0f, h, d * 0.5f), Quaternion.identity);
        b.Box("Lid", lid, new Vector3(0f, 0.005f, -d * 0.5f), new Vector3(w + 0.004f, 0.01f, d + 0.004f), shell);
        b.Box("LidTrim", lid, new Vector3(0f, 0.0105f, -d * 0.5f), new Vector3(w * 0.7f, 0.001f, 0.006f), brass);
        b.Text(lid, new Vector3(0f, 0.0112f, -d * 0.62f), new Vector2(w * 0.8f, 0.03f), "INCOME TOOLKIT", Brass, 0.35f, TMPro.TextAlignmentOptions.Center, true);
        BuildScoreboard(b, d, w);

        // Big yellow highlighter lying in front of the clipboard.
        marker = b.Node("Marker", transform, new Vector3(0.04f, 0.023f, -0.27f), Quaternion.Euler(0f, 0f, 90f));
        marker.localScale = Vector3.one * MarkerScale;
        b.Shape(PrimitiveType.Cylinder, "Barrel", marker, Vector3.zero, new Vector3(0.022f, 0.05f, 0.022f), b.Lit(new Color32(250, 206, 50, 255), "Marker Barrel"));
        b.Shape(PrimitiveType.Cylinder, "Cap", marker, new Vector3(0f, 0.055f, 0f), new Vector3(0.024f, 0.012f, 0.024f), b.Lit(new Color32(40, 40, 44, 255), "Marker Cap"));
        b.Shape(PrimitiveType.Cylinder, "Grip", marker, new Vector3(0f, -0.03f, 0f), new Vector3(0.0235f, 0.012f, 0.0235f), b.Lit(new Color32(40, 40, 44, 255), "Marker Grip"));
        b.Shape(PrimitiveType.Cylinder, "Tip", marker, new Vector3(0f, -0.056f, 0f), new Vector3(0.012f, 0.008f, 0.012f), b.Lit(new Color32(255, 230, 40, 255), "Marker Tip"));
        markerRestPosition = marker.localPosition;
        markerRestRotation = marker.localRotation;
        // A tap can lift the pen into an easy-to-grab staging pose beside the paper.
        markerPickupPosition = markerRestPosition + new Vector3(0f, 0.065f, 0.025f);
        markerPickupRotation = Quaternion.Euler(50f, 0f, -24f);
        markerHit = b.Hit(marker, Vector3.zero, new Vector3(0.06f, 0.16f, 0.06f), ToolkitTarget.Kind.Marker, "Highlighter", AnnotationSlot.Type);

        // Two-piece evidence clip.
        clip = b.Node("EvidenceClip", transform, new Vector3(0.33f, 0.006f, -0.3f), Quaternion.identity);
        b.Box("Base", clip, Vector3.zero, new Vector3(0.05f, 0.004f, 0.022f), brass);
        clipJaw = b.Node("JawHinge", clip, new Vector3(0f, 0.002f, 0.011f), Quaternion.Euler(-35f, 0f, 0f));
        b.Box("Jaw", clipJaw, new Vector3(0f, 0.002f, -0.011f), new Vector3(0.05f, 0.003f, 0.022f), brass);
        clipRestPosition = clip.localPosition;
        clipRestRotation = clip.localRotation;

        // Layered sheet edges give the active record depth without a wooden backing.
        clipboard = b.Node("Paper Stack", transform, PaperCenter, Quaternion.identity);
        for (int i = 0; i < 8; i++)
        {
            Color shade = i % 2 == 0 ? new Color32(236, 230, 214, 255) : new Color32(250, 247, 235, 255);
            Vector3 offset = new Vector3((i - 3.5f) * 0.003f, -0.003f - i * 0.004f, -i * 0.003f);
            b.Box("Sheet Edge " + i, clipboard, offset,
                new Vector3(AnnotationPaper.Width * PaperScale, 0.003f, AnnotationPaper.Depth * PaperScale), b.Lit(new Color32(172, 166, 151, 255), "Paper Edge " + i));
            b.Box("Sheet Face " + i, clipboard, offset + Vector3.up * 0.0016f,
                new Vector3(AnnotationPaper.Width * PaperScale - 0.0008f, 0.0003f, AnnotationPaper.Depth * PaperScale - 0.0008f), b.Lit(shade, "Stack Sheet " + i));
        }
    }

    private void BuildScoreboard(ToolkitBuilder b, float d, float w)
    {
        // Underside of the lid: readable from the seat when the lid is open.
        Quaternion under = Quaternion.LookRotation(Vector3.up, Vector3.back);
        Transform board = b.Node("Scoreboard", lid, new Vector3(0f, -0.0008f, -d * 0.5f), Quaternion.identity);
        b.Box("Panel", board, Vector3.zero, new Vector3(w - 0.02f, 0.0006f, d - 0.016f), b.Lit(new Color32(250, 246, 234, 255), "Scoreboard"));
        scoreTitle = b.Text(board, new Vector3(0f, -0.001f, -0.058f), new Vector2(w - 0.04f, 0.02f), "MARK THE MONEY!", new Color32(46, 158, 106, 255), 0.3f, TextAlignmentOptions.Center, true, under);
        scoreStatus = b.Text(board, new Vector3(0f, -0.001f, -0.036f), new Vector2(w - 0.04f, 0.014f), "Practice first", new Color32(96, 90, 82, 255), 0.18f, TextAlignmentOptions.Center, false, under);
        scoreCount = b.Text(board, new Vector3(-0.06f, -0.001f, 0.004f), new Vector2(0.1f, 0.04f), "-/16", Ink, 0.55f, TextAlignmentOptions.Center, true, under);
        scoreTier = b.Text(board, new Vector3(-0.06f, -0.001f, 0.04f), new Vector2(0.1f, 0.016f), "FIRST CHECK", new Color32(96, 90, 82, 255), 0.16f, TextAlignmentOptions.Center, true, under);
        for (int i = 0; i < 16; i++)
        {
            int row = i / 4, column = i % 4;
            scorePipMaterials[i] = b.Lit(PipEmpty, "Score Pip " + i);
            GameObject pip = b.Disc("Pip", board, new Vector3(0.022f + column * 0.016f, -0.0008f, -0.012f + row * 0.016f), 0.011f, 0.001f, scorePipMaterials[i]);
            pip.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            scorePips[i] = pip.GetComponent<Renderer>();
        }
        var medal = new GameObject("Medal");
        medal.layer = b.Layer;
        medal.transform.SetParent(board, false);
        medal.transform.localPosition = new Vector3(0.095f, -0.0015f, 0.008f);
        medal.transform.localRotation = under;
        scoreMedal = medal.AddComponent<SpriteRenderer>();
        scoreMedal.enabled = false;
    }

    /// <summary>The records, cards and challenge furniture come from the case data: built on first open.</summary>
    private bool EnsureContent()
    {
        if (paper != null) return true;
        definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        options = definition != null && definition.HasReview ? definition.review.annotation : null;
        if (options == null || !options.HasTutorial) return false;

        AddPaper(options.tutorial, "T0");
        if (options.entries != null)
            foreach (AnnotationEntryDefinition e in options.entries)
                if (e != null && !string.IsNullOrEmpty(e.id)) AddPaper(e, e.id);
        entry = options.tutorial;
        paper = papers[entry.id];

        int row = 0;
        foreach (AnnotationOptionDefinition option in options.typeOptions)
            AddCard(AnnotationSlot.Type, option, CardHome(AnnotationSlot.Type, row++));
        row = 0;
        foreach (AnnotationOptionDefinition option in options.treatmentOptions)
            AddCard(AnnotationSlot.Treatment, option, CardHome(AnnotationSlot.Treatment, row++));
        // Record tags: the tutorial's own, then the challenge's (shared ids made once).
        var tags = new List<AnnotationOptionDefinition>();
        if (options.tutorial.evidenceOptions != null) tags.AddRange(options.tutorial.evidenceOptions);
        if (options.entries != null)
            foreach (AnnotationEntryDefinition e in options.entries)
                if (e?.evidenceOptions != null)
                    foreach (AnnotationOptionDefinition option in e.evidenceOptions)
                        if (!tags.Exists(t => t.id == option.id)) tags.Add(option);
        foreach (AnnotationOptionDefinition option in tags) AddCard(AnnotationSlot.Evidence, option, Vector3.zero);

        if (options.entries != null && options.entries.Count > 0) BuildChallengeFurniture();
        return true;
    }

    private void AddPaper(AnnotationEntryDefinition e, string badge)
    {
        if (papers.ContainsKey(e.id)) return;
        AnnotationPaper p = AnnotationPaper.Build(builder, transform, PaperCenter, e, badge);
        p.Hinge.localScale = Vector3.one * PaperScale;
        p.Hinge.localPosition = PaperCenter + new Vector3(0f, 0f, AnnotationPaper.Depth * 0.5f * PaperScale);
        p.SetVisible(false);
        papers[e.id] = p;
    }

    private void AddCard(AnnotationSlot kind, AnnotationOptionDefinition option, Vector3 home)
    {
        if (option == null || string.IsNullOrEmpty(option.id)) return;
        ToolkitBuilder b = builder;
        bool tag = kind == AnnotationSlot.Evidence;
        float w = tag ? 0.13f : 0.14f, d = tag ? 0.066f : NoteCardDepth;
        var card = new AnnotationCard { kind = kind, optionId = option.id, label = option.label };
        card.root = b.Node("Card " + option.id, transform, home, Quaternion.Euler(0f, tag ? -4f : 3f, 0f));
        BuildCardVisual(b, card, kind, option);
        Transform face = card.body;

        // Generous touch target: wider than the card face.
        card.hit = b.Hit(face, new Vector3(0f, 0.005f, 0f), new Vector3(w + 0.014f, 0.02f, d + 0.012f), ToolkitTarget.Kind.Card, option.id, kind, card);

        // Soft shadow on the surface under the card (kept apart from the card so it stays down).
        card.shadowMaterial = b.Clear(new Color(0f, 0f, 0f, 0.22f), "Card Shadow");
        card.shadow = b.Node("Shadow " + option.id, transform, home, Quaternion.identity);
        b.Box("Shade", card.shadow, Vector3.zero, new Vector3((w + 0.01f) * CardVisualScale, 0.0004f, (d + 0.01f) * CardVisualScale), card.shadowMaterial);

        card.homePosition = home;
        card.homeRotation = card.root.localRotation;
        card.boxPosition = new Vector3(-0.5f + (cards.Count % 3 - 1) * 0.06f, 0.012f + cards.Count * 0.0015f, 0.27f);
        Snap(card, transform.TransformPoint(card.boxPosition), transform.rotation * card.homeRotation, BoxScale);
        cards.Add(card);
    }

    /// <summary>
    /// The visible note card / record tag (body under card.root): border, face,
    /// coloured header band, caption and hidden lock badge. Shared by the market
    /// desk and the collected annotated papers, so both show the same card.
    /// </summary>
    public static void BuildCardVisual(ToolkitBuilder b, AnnotationCard card, AnnotationSlot kind, AnnotationOptionDefinition option)
    {
        bool tag = kind == AnnotationSlot.Evidence;
        float w = tag ? 0.13f : 0.14f, d = tag ? 0.066f : NoteCardDepth;
        Color color = AnnotationPaper.KindColor(kind);
        card.body = b.Node("Body", card.root, Vector3.zero, Quaternion.identity);
        card.body.localScale = Vector3.one * CardVisualScale;
        Transform face = card.body;

        // Glow (picked up) under a coloured border, cream face, header band with its word.
        card.glowMaterial = b.Clear(new Color(color.r, color.g, color.b, 0f), "Card Glow " + kind);
        b.Box("Glow", face, new Vector3(0f, -0.001f, 0f), new Vector3(w + 0.026f, 0.0006f, d + 0.026f), card.glowMaterial);
        b.Box("Border", face, new Vector3(0f, -0.0002f, 0f), new Vector3(w + 0.005f, 0.0026f, d + 0.005f), b.Lit(Color.Lerp(color, Color.black, 0.25f), "Card Border " + kind));
        b.Box("Face", face, Vector3.zero, new Vector3(w, 0.003f, d), b.Lit(tag ? TagColor : CardColor, tag ? "Tag" : "Card"));
        Material band = b.Lit(color, "Card Band " + kind);
        float bandDepth = 0.017f;
        b.Box("Band", face, new Vector3(0f, 0.0018f, d * 0.5f - bandDepth * 0.5f), new Vector3(w, 0.0006f, bandDepth), band);
        string header = kind == AnnotationSlot.Type ? "TYPE" : kind == AnnotationSlot.Treatment ? "TREATMENT" : "RECORD";
        b.Text(face, new Vector3(-0.006f, 0.0024f, d * 0.5f - bandDepth * 0.5f), new Vector2(w - 0.03f, 0.012f), header, Color.white, 0.12f, TextAlignmentOptions.Left, true, null, false);
        if (tag)
        {
            // Punched hole with a ring, like a luggage tag.
            b.Disc("Ring", face, new Vector3(w * 0.5f - 0.012f, 0.0024f, d * 0.5f - bandDepth * 0.5f), 0.011f, 0.0006f, b.Lit(Color.white, "Tag Ring"));
            b.Disc("Hole", face, new Vector3(w * 0.5f - 0.012f, 0.0028f, d * 0.5f - bandDepth * 0.5f), 0.006f, 0.0006f, b.Lit(new Color32(60, 50, 40, 255), "Tag Hole"));
        }
        b.Text(face, new Vector3(0f, 0.0022f, -bandDepth * 0.5f), new Vector2(w - 0.014f, d - bandDepth - 0.008f),
            CardCaption(option.id, option.label), Ink, 0.18f, TextAlignmentOptions.Center, false, null, false);

        // Lock badge (shown when a check confirmed this note): green disc and tick.
        card.lockBadge = b.Node("Lock", face, new Vector3(w * 0.5f - 0.006f, 0.004f, -d * 0.5f + 0.006f), Quaternion.identity);
        b.Disc("Disc", card.lockBadge, Vector3.zero, 0.02f, 0.001f, b.Lit(AnnotationPaper.FitColor, "Lock Badge"));
        b.CheckIcon(card.lockBadge, new Vector3(0f, 0.0008f, 0f), 0.016f, b.Lit(Color.white, "Lock Tick"));
        card.lockBadge.gameObject.SetActive(false);
    }

    private void BuildChallengeFurniture()
    {
        ToolkitBuilder b = builder;
        // Record tabs along the clipboard's top edge, with one pip per scored decision.
        float top = PaperCenter.z + AnnotationPaper.Depth * 0.5f * PaperScale;
        for (int i = 0; i < options.entries.Count; i++)
        {
            AnnotationEntryDefinition e = options.entries[i];
            var tab = new Tab { entryId = e.id };
            tab.root = b.Node("Tab " + e.id, transform, new Vector3(PaperCenter.x - 0.15f + i * 0.1f, PaperCenter.y + 0.001f, top + 0.028f), Quaternion.identity);
            tab.body = b.Node("Body", tab.root, Vector3.zero, Quaternion.identity);
            tab.face = b.Lit(new Color32(232, 214, 170, 255), "Tab " + i);
            b.Box("Face", tab.body, Vector3.zero, new Vector3(0.09f, 0.004f, 0.064f), tab.face);
            // Give the fixed-size numeral a full line height so TMP cannot truncate it away.
            b.Text(tab.body, new Vector3(-0.012f, 0.0026f, 0.011f), new Vector2(0.05f, 0.036f), (i + 1).ToString(), Ink, 0.24f, TextAlignmentOptions.Center, true, null, false);
            for (int p = 0; p < IncomeChallenge.ScoredSlots.Length; p++)
            {
                tab.pips[p] = b.Lit(PipEmpty, "Tab Pip");
                b.Disc("Pip", tab.body, new Vector3(-0.028f + p * 0.015f, 0.0024f, -0.020f), 0.007f, 0.001f, tab.pips[p]);
            }
            tab.done = b.Node("Done", tab.body, new Vector3(0.029f, 0.004f, 0.009f), Quaternion.identity);
            b.Disc("Disc", tab.done, Vector3.zero, 0.013f, 0.001f, b.Lit(AnnotationPaper.FitColor, "Tab Done"));
            b.CheckIcon(tab.done, new Vector3(0f, 0.0008f, 0f), 0.010f, b.Lit(Color.white, "Tab Tick"));
            tab.done.gameObject.SetActive(false);
            b.Hit(tab.body, new Vector3(0f, 0.006f, 0f), new Vector3(0.095f, 0.016f, 0.066f), ToolkitTarget.Kind.Tab, e.id, AnnotationSlot.Type);
            tab.root.gameObject.SetActive(false);
            tabs.Add(tab);
        }

        // CHECK button: a chunky push button that glows when every record is ready.
        checkButton = b.Node("CheckButton", transform, CheckButtonHome, Quaternion.identity);
        b.Disc("Base", checkButton, new Vector3(0f, 0.008f, 0f), 0.12f, 0.016f, b.Lit(new Color32(36, 40, 48, 255), "Button Base"));
        checkGlowMaterial = b.Clear(new Color(0.3f, 0.9f, 0.5f, 0f), "Button Glow");
        b.Disc("Glow", checkButton, new Vector3(0f, 0.0165f, 0f), 0.14f, 0.0006f, checkGlowMaterial);
        checkCap = b.Node("Cap", checkButton, new Vector3(0f, 0.02f, 0f), Quaternion.identity);
        checkCapMaterial = b.Lit(new Color32(150, 150, 150, 255), "Button Cap");
        b.Disc("Cap", checkCap, new Vector3(0f, 0.006f, 0f), 0.095f, 0.012f, checkCapMaterial);
        b.Text(checkCap, new Vector3(0f, 0.0125f, 0.004f), new Vector2(0.085f, 0.03f), "CHECK", Color.white, 0.36f, TextAlignmentOptions.Center, true);
        b.Text(checkCap, new Vector3(0f, 0.0125f, -0.022f), new Vector2(0.08f, 0.012f), "annotations", new Color(1f, 1f, 1f, 0.85f), 0.14f, TextAlignmentOptions.Center);
        b.Hit(checkButton, new Vector3(0f, 0.02f, 0f), new Vector3(0.13f, 0.05f, 0.13f), ToolkitTarget.Kind.Button, "check", AnnotationSlot.Type);
        checkButton.localScale = Vector3.zero;
        checkButton.gameObject.SetActive(false);
    }

    // =========================================================
    // STATE ACCESS
    // =========================================================

    private Mode DecideMode(CaseReviewState review)
    {
        if (review == null) return Mode.None;
        if (review.TutorialActive) return Mode.Tutorial;
        if (options.entries == null || options.entries.Count == 0) return Mode.Done;
        if (review.phase >= ReviewPhase.Challenge && IncomeChallenge.Ensure(review, definition?.review) != null &&
            (!review.challenge.mastered || !review.challenge.awarded)) return Mode.Challenge;
        return Mode.Done;
    }

    private AnnotationEntryState Work(AnnotationEntryDefinition e)
    {
        CaseReviewState review = Review;
        if (review == null || e == null) return null;
        if (e == options.tutorial) return review.TutorialWork(e);
        ChallengeState challenge = IncomeChallenge.Ensure(review, definition?.review);
        return IncomeChallenge.Work(challenge, e);
    }

    private ChallengeState Challenge => Review?.challenge;

    private bool CanEdit
    {
        get
        {
            CaseReviewState review = Review;
            if (review == null || entry == null) return false;
            return mode == Mode.Tutorial ? review.TutorialActive : mode == Mode.Challenge && IncomeChallenge.CanEdit(review);
        }
    }

    private bool Locked(AnnotationSlot slot) =>
        mode == Mode.Challenge && IncomeChallenge.IsLocked(Challenge, entry.id, slot);

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    /// <summary>The player sat at the records desk; the case opens once the desk view is active.</summary>
    public void RequestOpen() => wantOpen = true;

    /// <summary>Park the tools for Rosa's conversation; draft/current record remain in the review.</summary>
    public void SuspendForRosaDiscussion()
    {
        wantOpen = false;
        if (open) Close();
    }

    private void Update()
    {
        if (!AtDesk)
        {
            wantOpen = false;
            if (open) Close();
            return;
        }
        if (wantOpen && !open) Open();
        UpdateSkipButton();
        if (!open) return;

        if (placedUntil > 0f && Time.unscaledTime > placedUntil)
        {
            placedUntil = -1f;
            if (paper != null) paper.ShowPlaced(null);
        }
        AnimateFurniture();

        bool modalPaused = TeachingPaused || SideQuestSplash.IsShowing;
        bool blocked = modalPaused || (Level2AnnotatedPapers.Instance != null && Level2AnnotatedPapers.Instance.IsCollecting) ||
                       demoRunning || busy > 0 || !CanEdit;
        if (blocked)
        {
            if (modalPaused)
            {
                CancelDrag();
                CancelInk();
            }
            pressValid = false;
            return;
        }
        PollPointer();
        Nudge();
    }

    private void Open()
    {
        // This records desk owns the exit hold and the bottom hint while it is the one in use.
        active = this;
        WorkstationUI.ExitBlocked = () => HoldingExitAfterQuest ||
            (AtRecordsDesk() && (active.busy > 0 || active.demoRunning || SideQuestSplash.IsShowing));
        GuideCoachView.HintAtBottom = () => AtRecordsDesk() && active.mode != Mode.Done;
        if (!EnsureContent())
        {
            wantOpen = false;
            return;
        }
        open = true;
        CaseReviewState review = Review;
        mode = DecideMode(review);
        AnnotationEntryDefinition first = options.tutorial;
        if ((mode == Mode.Challenge || mode == Mode.Done) && options.entries != null && options.entries.Count > 0)
        {
            ChallengeState challenge = Challenge;
            first = FindEntry(challenge?.currentEntryId) ?? options.entries[0];
        }
        ShowEntry(first);
        ApplyState(instant: true, onlyPlaced: true);
        RefreshResults();
        RefreshTabs();
        RefreshScoreboard();
        if (flow != null) StopCoroutine(flow);
        flow = StartCoroutine(OpenFlow(review));
    }

    private IEnumerator OpenFlow(CaseReviewState review)
    {
        busy++;
        if (review != null && !review.sideQuestIntroSeen && !review.tutorialCompleted)
        {
            SideQuestSplash splash = SideQuestSplash.GetOrCreateForScene(gameObject.scene);
            if (splash != null)
            {
                bool continued = false;
                var intro = new SideQuestSplash.SplashContent
                {
                    kicker = "SIDE QUEST 2",
                    title = "Mark the Money!",
                    description = "Every payment has a story. Learn the tools, then mark Rosa's four records and collect your annotated papers.",
                    instruction = "First, practise with Arthur  ·  Then review Rosa's records",
                    button = "TAP TO BEGIN"
                };
                if (splash.Show(intro, () => { review.sideQuestIntroSeen = true; continued = true; }))
                {
                    challengeSplash = splash;
                    while (open && !continued && SideQuestSplash.IsShowing) yield return null;
                    challengeSplash = null;
                }
            }
        }
        if (!open) { Release(); yield break; }
        if (mode == Mode.Done)
        {
            // Finished: the case stays shut and the papers are already in their icon.
            if (paper != null) paper.SetVisible(false);
            Release();
            flow = null;
            yield break;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        SetLid(LidOpenAngle, LidOpenSeconds);
        yield return Wait(LidOpenSeconds * 0.45f, ignorePause: true);
        yield return DealCards();
        if (mode == Mode.Challenge || mode == Mode.Done) yield return ShowChallengeFurniture();
        Release();

        if (review == null) yield break;
        idleSince = Time.unscaledTime;
        if (mode == Mode.Tutorial && !review.toolkitDemoSeen) yield return Demo();
        else if (mode == Mode.Tutorial) Level2Say.Line(entry.demo.yourTurnLine);
        else if (mode == Mode.Challenge)
        {
            ChallengeState challenge = Challenge;
            if (challenge.mastered && !challenge.awarded) yield return Mastery();
            else if (!challenge.introSeen)
            {
                challenge.introSeen = true;
                Level2Say.Line("Now Rosa's four records. Mark each one, add its notes, then press CHECK. Your first check sets the badge.", "wow", 6f);
            }
            else if (challenge.firstChecked) Level2Say.Line("Fix the red notes, then CHECK again. Tap a red one for a hint.", null, 5f);
            else Level2Say.Line(Level2Checkpoint.EthicsChoiceCommitted(review, definition.caseId)
                ? "Your conversation is recorded. Check your work when ready."
                : "Finish Rosa's four records. CHECK will open her conversation before grading.", null, 6f);
        }
        flow = null;
    }

    private void Close()
    {
        if (challengeSplash != null) challengeSplash.Cancel();
        challengeSplash = null;
        open = false;
        if (flow != null) StopCoroutine(flow);
        flow = null;
        if (pageMotion != null) StopCoroutine(pageMotion);
        pageMotion = null;
        foreach (AnnotationPaper record in papers.Values) record.Hinge.localRotation = Quaternion.identity;
        demoRunning = false; // an interrupted demonstration replays next visit
        busy = 0;
        CancelInk(instant: true);
        CancelDrag();
        Deselect();
        pressValid = false;
        if (markerMotion != null) StopCoroutine(markerMotion);
        markerMotion = null;
        if (paper != null)
        {
            ApplyState(instant: true, onlyPlaced: true);
            paper.Animate(null, false);
            paper.ShowPlaced(null);
            paper.SetHover(null);
        }
        ReturnMarker();
        // Loose cards go back in the case; notes on the paper stay attached.
        foreach (AnnotationCard card in cards)
            if (!card.placed) Fly(card, transform.TransformPoint(card.boxPosition), transform.rotation, BoxScale, 0.25f, 0.04f, 0f);
        SetLid(0f, LidOpenSeconds * 0.8f);
    }

    private IEnumerator DealCards()
    {
        int i = 0;
        LayoutPalette();
        foreach (AnnotationCard card in cards)
        {
            if (card.placed || !card.available) continue;
            StartCard(card, DealOne(card, 0.035f * i++));
        }
        yield return Wait(CardFlySeconds + 0.1f + 0.035f * i, ignorePause: true);
    }

    /// <summary>Out of the case in an arc, flipping over as it lands.</summary>
    private IEnumerator DealOne(AnnotationCard card, float delay)
    {
        if (delay > 0f) yield return Wait(delay, ignorePause: true);
        Transform t = card.root;
        Vector3 from = t.position, to = transform.TransformPoint(card.homePosition);
        Quaternion toRot = transform.rotation * card.homeRotation;
        float fromScale = t.localScale.x;
        if (AudioManager.Instance != null && Random.value < 0.35f) AudioManager.Instance.PlayPaperSFX();
        for (float u = 0f; u < 1f; u += Time.unscaledDeltaTime / (CardFlySeconds + 0.1f))
        {
            float e = EaseOutCubic(u);
            t.position = Vector3.Lerp(from, to, e) + transform.up * Mathf.Sin(e * Mathf.PI) * 0.09f;
            t.rotation = toRot * Quaternion.Euler(0f, 0f, (1f - e) * 180f);
            t.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, e);
            yield return null;
        }
        Snap(card, to, toRot, 1f);
        card.motion = null;
    }

    private void SetLid(float angle, float seconds)
    {
        if (lidMotion != null) StopCoroutine(lidMotion);
        lidMotion = StartCoroutine(LidTo(angle, seconds));
    }

    private IEnumerator LidTo(float target, float seconds)
    {
        float from = lidAngle;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
        {
            float e = target > from ? EaseOutBack(t) : EaseOutBounce(t);
            lidAngle = Mathf.LerpUnclamped(from, target, e);
            lid.localRotation = Quaternion.Euler(lidAngle, 0f, 0f);
            yield return null;
        }
        lidAngle = target;
        lid.localRotation = Quaternion.Euler(lidAngle, 0f, 0f);
        if (target <= 0f && AudioManager.Instance != null) AudioManager.Instance.PlayPaper2SFX();
        lidMotion = null;
    }

    // =========================================================
    // RECORDS (one visible on the clipboard)
    // =========================================================

    private AnnotationEntryDefinition FindEntry(string id)
    {
        if (string.IsNullOrEmpty(id) || options?.entries == null) return null;
        return options.entries.Find(e => e != null && e.id == id);
    }

    private void ShowEntry(AnnotationEntryDefinition e)
    {
        entry = e;
        foreach (KeyValuePair<string, AnnotationPaper> pair in papers) pair.Value.SetVisible(pair.Key == e.id);
        paper = papers[e.id];
        paper.Hinge.localRotation = Quaternion.identity;
        if (mode == Mode.Challenge && Challenge != null) Challenge.currentEntryId = e.id;
        // Only this record's tags are dealt out.
        foreach (AnnotationCard card in cards)
            if (card.kind == AnnotationSlot.Evidence)
                card.available = e.evidenceOptions != null && e.evidenceOptions.Exists(o => o.id == card.optionId);
    }

    /// <summary>Visits each intervening record, keeping input locked for the whole page journey.</summary>
    private IEnumerator SwitchEntry(AnnotationEntryDefinition next)
    {
        if (next == null || next == entry) yield break;
        busy++;
        Deselect();
        int current = options.entries.IndexOf(entry);
        int destination = options.entries.IndexOf(next);
        if (current < 0 || destination < 0) yield return TurnPage(next);
        else
        {
            int direction = destination > current ? 1 : -1;
            while (current != destination)
            {
                current += direction;
                yield return TurnPage(options.entries[current]);
            }
        }
        Release();
        pageMotion = null;
    }

    /// <summary>Lift one record above the stack, then lower the next sheet into place.</summary>
    private IEnumerator TurnPage(AnnotationEntryDefinition next)
    {
        AnnotationPaper old = paper;
        foreach (AnnotationCard card in cards)
            if (card.placed) Fly(card, transform.TransformPoint(card.homePosition), transform.rotation * card.homeRotation, 1f, 0.18f, 0.05f, 0f);
        SnapClip(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.18f)
        {
            old.Hinge.localRotation = Quaternion.Euler(170f * EaseInCubic(t), 0f, 0f);
            yield return null;
        }
        old.Hinge.localRotation = Quaternion.identity;
        ShowEntry(next);
        foreach (AnnotationCard card in cards) card.placed = false;
        LayoutPalette();
        paper.Hinge.localRotation = Quaternion.Euler(120f, 0f, 0f);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.22f)
        {
            paper.Hinge.localRotation = Quaternion.Euler(120f * (1f - EaseOutCubic(t)), 0f, 0f);
            yield return null;
        }
        paper.Hinge.localRotation = Quaternion.identity;
        foreach (AnnotationCard card in cards)
            if (!card.available) Fly(card, transform.TransformPoint(card.boxPosition), transform.rotation, BoxScale, 0.2f, 0.04f, 0f);
            else if (!PlacedIn(card)) Fly(card, transform.TransformPoint(card.homePosition), transform.rotation * card.homeRotation, 1f, 0.2f, 0.05f, 0f);
        ApplyState(instant: false, onlyPlaced: true);
        RefreshResults();
        RefreshTabs();
        yield return Wait(0.25f, ignorePause: true);
    }

    /// <summary>Evidence tag spots depend on how many tags this record offers.</summary>
    private void LayoutPalette()
    {
        int row = 0;
        foreach (AnnotationCard card in cards)
        {
            if (card.kind != AnnotationSlot.Evidence || !card.available) continue;
            card.homePosition = CardHome(AnnotationSlot.Evidence, row++);
        }
    }

    private bool PlacedIn(AnnotationCard card)
    {
        AnnotationEntryState work = Work(entry);
        if (work == null) return false;
        return card.kind == AnnotationSlot.Evidence ? work.HasEvidence(card.optionId) : work.Get(card.kind) == card.optionId;
    }

    private IEnumerator ShowChallengeFurniture()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            Tab tab = tabs[i];
            if (tab.root.gameObject.activeSelf) continue;
            tab.root.gameObject.SetActive(true);
            StartCoroutine(PopIn(tab.root, 0.06f * i, 0.25f));
        }
        if (checkButton != null && !checkButton.gameObject.activeSelf)
        {
            checkButton.gameObject.SetActive(true);
            StartCoroutine(PopIn(checkButton, 0.25f, 0.3f));
        }
        yield return Wait(0.45f, ignorePause: true);
    }

    // =========================================================
    // DEMONSTRATION (presentation only)
    // =========================================================

    private IEnumerator Demo()
    {
        demoRunning = true;
        AnnotationDemoDefinition demo = entry.demo;
        yield return Wait(0.4f);
        Level2Say.Line(demo.introLine, "wow", 4.5f);
        yield return Wait(3.2f);

        Level2Say.Line(demo.highlightLine, null, 4f);
        yield return Trace(demo.highlightLineId);
        yield return Wait(2.2f);

        foreach (AnnotationSlot slot in new[] { AnnotationSlot.Type, AnnotationSlot.Treatment })
        {
            string id = slot == AnnotationSlot.Type ? demo.typeId : demo.treatmentId;
            AnnotationCard card = FindCard(slot, id);
            AnnotationOptionDefinition option = options.FindOption(slot, id, entry);
            if (card == null || option == null) continue;
            Level2Say.Line(option.label + ": " + option.rule, null, 4.5f);
            yield return Wait(0.5f);
            yield return FlyToPlace(card, slot, 0, 1);
            yield return Wait(2.6f);
        }

        Level2Say.Line(demo.evidenceLine, null, 4f);
        yield return Wait(0.5f);
        for (int i = 0; i < demo.evidenceIds.Count; i++)
        {
            AnnotationCard card = FindCard(AnnotationSlot.Evidence, demo.evidenceIds[i]);
            if (card != null) yield return FlyToPlace(card, AnnotationSlot.Evidence, i, demo.evidenceIds.Count);
        }
        yield return ClipTo(true);
        yield return Wait(2.6f);

        // Put everything back so the player can repeat it.
        Level2Say.Line(demo.yourTurnLine, null, 5f);
        yield return Wait(0.6f);
        paper.ClearStrokes();
        CaseReviewState review = Review;
        ApplyState(instant: false, onlyPlaced: false);
        yield return Wait(0.45f, ignorePause: true);

        demoRunning = false;
        idleSince = Time.unscaledTime;
        if (review != null && review.TryMarkToolkitDemoSeen()) GameplayEvents.RaiseToolkitDemoFinished();
    }

    /// <summary>Real time that only runs while teaching is not paused.</summary>
    private IEnumerator Wait(float seconds, bool ignorePause = false)
    {
        for (float t = 0f; t < seconds;)
        {
            if (ignorePause || !Paused) t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // =========================================================
    // POINTER
    // =========================================================

    private void PollPointer()
    {
        if (!Level2DeskStamps.TryReadPointer(out Vector2 screen, out bool down, out bool up)) return;
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (view == null) return;

        if (down)
        {
            pressValid = !OverUi(screen);
            if (!pressValid) return;
            idleSince = Time.unscaledTime;
            pressScreen = screen;
            pressTarget = Pick(view, screen);
            if (pressTarget != null && (pressTarget.kind == ToolkitTarget.Kind.Line || pressTarget.kind == ToolkitTarget.Kind.Marker))
                BeginInk(view, screen, pressTarget);
        }
        if (!pressValid) return;

        if (inking)
        {
            UpdateInk(view, screen);
            if (up)
            {
                pressValid = false;
                EndInk();
            }
            return;
        }

        if (dragging == null && pressTarget != null && pressTarget.kind == ToolkitTarget.Kind.Card &&
            (screen - pressScreen).sqrMagnitude > DragThreshold * DragThreshold)
            BeginDrag(pressTarget.card);

        if (dragging != null)
        {
            FollowPointer(view, screen);
            hoverSlot = OverUi(screen) ? null : SlotUnder(Pick(view, screen));
        }

        if (!up) return;
        pressValid = false;
        if (dragging != null)
        {
            EndDrag(view, screen);
            return;
        }
        ToolkitTarget released = Pick(view, screen);
        if (released != null && pressTarget != null && released != pressTarget) released = null; // slid off: no tap
        Tap(released);
    }

    private static ToolkitTarget Pick(Camera view, Vector2 screen)
    {
        Ray ray = view.ScreenPointToRay(screen);
        RaycastHit[] hits = Physics.RaycastAll(ray, 6f, 1 << Level2DeskStamps.DragLayer, QueryTriggerInteraction.Collide);
        ToolkitTarget best = null;
        float bestDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            ToolkitTarget target = hit.collider.GetComponent<ToolkitTarget>();
            if (target == null) continue;
            // Cards and tools lie on top of the paper: prefer them over the line beneath.
            float bias = target.kind == ToolkitTarget.Kind.Card || target.kind == ToolkitTarget.Kind.Marker ? 0.05f : 0f;
            float distance = hit.distance - bias;
            if (distance >= bestDistance) continue;
            best = target;
            bestDistance = distance;
        }
        return best;
    }

    /// <summary>True when the press lands on a HUD button, an open paper window or a 3D document in front.</summary>
    private static bool OverUi(Vector2 screen)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;
        var data = new PointerEventData(events) { position = screen };
        var results = new List<RaycastResult>();
        events.RaycastAll(data, results);
        foreach (RaycastResult result in results)
        {
            GameObject go = result.gameObject;
            if (go == null) continue;
            if (go.GetComponentInParent<Selectable>() != null || go.GetComponentInParent<ScrollRect>() != null) return true;
            FloatingWindow window = go.GetComponentInParent<FloatingWindow>();
            if (window != null && window.IsOpen()) return true;
            foreach (Doc3DLayer layer in Doc3DLayer.All)
                if (layer != null && layer.IsVisible && layer.Source != null && go.transform.IsChildOf(layer.Source)) return true;
        }
        return false;
    }

    /// <summary>The note place under the pointer: a place itself, or a card already lying on one.</summary>
    private static AnnotationSlot? SlotUnder(ToolkitTarget target)
    {
        if (target == null) return null;
        if (target.kind == ToolkitTarget.Kind.Slot) return target.slot;
        if (target.kind == ToolkitTarget.Kind.Card && target.card != null && target.card.placed) return target.card.kind;
        return null;
    }

    private void Tap(ToolkitTarget target)
    {
        if (target == null)
        {
            Deselect();
            return;
        }
        switch (target.kind)
        {
            case ToolkitTarget.Kind.Card:
                AnnotationCard card = target.card;
                if (card == null) break;
                if (card.placed && selected != null && selected.kind == card.kind) DoPlace(selected, card.kind); // swap
                else if (card.placed)
                {
                    if (mode == Mode.Challenge && IncomeChallenge.IsFlagged(Challenge, entry.id, card.kind)) SayHint(card.kind);
                    DoRemove(card);
                }
                else if (selected == card) Deselect();
                else Select(card);
                break;
            case ToolkitTarget.Kind.Slot:
                if (selected != null) DoPlace(selected, target.slot);
                else if (mode == Mode.Challenge && IncomeChallenge.IsFlagged(Challenge, entry.id, target.slot)) SayHint(target.slot);
                else Level2Say.Line("Pick a " + AnnotationPaper.SlotHints[(int)target.slot] + " first, then tap this place.", null, 3f);
                break;
            case ToolkitTarget.Kind.Tab:
                AnnotationEntryDefinition next = FindEntry(target.id);
                if (next != null && next != entry) pageMotion = StartCoroutine(SwitchEntry(next));
                break;
            case ToolkitTarget.Kind.Button:
                StartCoroutine(PressCheck());
                break;
        }
    }

    private void Select(AnnotationCard card)
    {
        Deselect();
        selected = card;
        card.selected = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayExactEffect("sfx_card_pickup_02", 0.6f);
        Vector3 lifted = transform.TransformPoint(card.homePosition + Vector3.up * CardLift);
        Fly(card, lifted, transform.rotation * card.homeRotation * Quaternion.Euler(-10f, 0f, 0f), 1.08f, 0.12f, 0f, 0f);
    }

    private void Deselect()
    {
        if (selected == null) return;
        AnnotationCard card = selected;
        selected = null;
        card.selected = false;
        if (!card.placed && dragging != card) ReturnHome(card);
    }

    private void BeginDrag(AnnotationCard card)
    {
        if (card == null) return;
        if (card.placed && Locked(card.kind))
        {
            RefuseLocked(card);
            pressValid = false;
            return;
        }
        if (selected != null && selected != card) Deselect();
        selected = null;
        card.selected = true;
        dragging = card;
        if (card.motion != null) StopCoroutine(card.motion);
        card.motion = null;
        card.hit.enabled = false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayExactEffect("sfx_card_pickup_02", 0.6f);
    }

    private void FollowPointer(Camera view, Vector2 screen)
    {
        var plane = new Plane(transform.up, transform.position + transform.up * DragHeight);
        Ray ray = view.ScreenPointToRay(screen);
        if (!plane.Raycast(ray, out float enter)) return;
        Vector3 target = ray.GetPoint(enter);
        float k = 1f - Mathf.Exp(-24f * Time.unscaledDeltaTime);
        Transform t = dragging.root;
        Vector3 before = t.position;
        t.position = Vector3.Lerp(t.position, target, k);
        // Lean into the motion.
        Vector3 velocity = transform.InverseTransformVector(t.position - before) / Mathf.Max(1e-4f, Time.unscaledDeltaTime);
        Quaternion lean = Quaternion.Euler(Mathf.Clamp(velocity.z * 40f, -14f, 14f), 0f, Mathf.Clamp(-velocity.x * 40f, -14f, 14f));
        t.rotation = Quaternion.Slerp(t.rotation, transform.rotation * dragging.homeRotation * lean, k);
        t.localScale = Vector3.Lerp(t.localScale, Vector3.one * 1.1f, k);
    }

    private void EndDrag(Camera view, Vector2 screen)
    {
        if (OverUi(screen))
        {
            CancelDrag();
            return;
        }
        AnnotationCard card = dragging;
        // Resolve the release while the held card cannot intercept its own ray.
        AnnotationSlot? slot = SlotUnder(Pick(view, screen));
        dragging = null;
        hoverSlot = null;
        card.hit.enabled = true;
        card.selected = false;
        if (slot.HasValue) DoPlace(card, slot.Value);
        else if (card.placed) DoRemove(card);
        else ReturnHome(card);
    }

    private void CancelDrag()
    {
        if (dragging == null) return;
        AnnotationCard card = dragging;
        dragging = null;
        hoverSlot = null;
        card.hit.enabled = true;
        card.selected = false;
        if (card.placed) LayoutPlaced(card.kind, animate: true);
        else ReturnHome(card);
    }

    // =========================================================
    // HIGHLIGHTER (swipe to ink)
    // =========================================================

    private void BeginInk(Camera view, Vector2 screen, ToolkitTarget target)
    {
        if (mode == Mode.Challenge && IncomeChallenge.IsLocked(Challenge, entry.id, AnnotationSlot.Highlight))
        {
            Level2Say.Line("That highlighted line is already right. Fix the red notes.", null, 3f);
            return;
        }
        Deselect();
        inking = true;
        inkFromMarker = target.kind == ToolkitTarget.Kind.Marker;
        inkMoved = false;
        lastTickCoverage = 0f;
        sweep.Begin(0f);
        if (!inkFromMarker && TryPaperPoint(view, screen, out Vector3 world))
        {
            float x = paper.ProgressAt(world);
            sweep.Aim(target.id, x);
            sweep.Begin(x);
            sweep.Aim(target.id, x);
        }
        if (markerMotion != null) StopCoroutine(markerMotion);
        markerMotion = null;
        markerHit.enabled = false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayExactEffect("sfx_card_pickup_02", 0.5f);
    }

    private bool TryPaperPoint(Camera view, Vector2 screen, out Vector3 world)
    {
        world = default;
        var plane = new Plane(paper.Root.up, paper.Root.position + paper.Root.up * 0.002f);
        Ray ray = view.ScreenPointToRay(screen);
        if (!plane.Raycast(ray, out float enter)) return false;
        world = ray.GetPoint(enter);
        return true;
    }

    private void UpdateInk(Camera view, Vector2 screen)
    {
        if (!TryPaperPoint(view, screen, out Vector3 world)) return;
        if ((screen - pressScreen).sqrMagnitude > DragThreshold * DragThreshold) inkMoved = true;
        string aimed = paper.AimLine(world);
        float x = paper.ProgressAt(world);
        if (aimed != null && sweep.LineId != aimed && !sweep.Locked)
        {
            if (sweep.LineId != null) paper.SetStroke(sweep.LineId, 0f, 0f);
            sweep.Aim(aimed, x);
        }
        bool onLine = sweep.LineId != null && (aimed == sweep.LineId || sweep.Locked);
        if (onLine) sweep.Move(x, 1f, allowLock: aimed == sweep.LineId);

        // Ink only where the nib has actually passed.
        if (sweep.LineId != null && sweep.Locked) paper.SetStroke(sweep.LineId, sweep.Min, sweep.Max);
        paper.SetHover(aimed);
        paper.SetNib(onLine && sweep.Locked ? sweep.LineId : null, x);
        if (sweep.Coverage - lastTickCoverage > 0.12f)
        {
            bool firstSwipe = lastTickCoverage <= 0f;
            lastTickCoverage = sweep.Coverage;
            if (firstSwipe && AudioManager.Instance != null) AudioManager.Instance.PlayExactEffect("sfx_card_return_02", 0.5f);
        }

        // The pen follows the finger: nib on the paper, leaning back.
        Vector3 nib = world;
        if (sweep.Locked && paper.TryLineEnds(sweep.LineId, out Vector3 a, out Vector3 b))
            nib = Vector3.Lerp(a, b, x);
        Quaternion pen = HeldPen(x);
        Vector3 rootTarget = nib + pen * (Vector3.up * NibOffset * MarkerScale) + paper.Root.up * (onLine ? 0.002f : 0.03f);
        float k = 1f - Mathf.Exp(-30f * Time.unscaledDeltaTime);
        marker.position = Vector3.Lerp(marker.position, rootTarget, k);
        marker.rotation = Quaternion.Slerp(marker.rotation, pen, k);
    }

    private Quaternion HeldPen(float progress) =>
        paper.Root.rotation * Quaternion.Euler(50f, 0f, Mathf.Lerp(-30f, -18f, progress));

    private void EndInk()
    {
        inking = false;
        paper.SetNib(null, 0f);
        paper.SetHover(null);
        markerHit.enabled = true;
        string line = sweep.LineId;
        if (sweep.CanFinish(false, false))
        {
            StartCoroutine(Busy(FinishStroke(line, sweep.Min, sweep.Max)));
            return;
        }
        if (!inkFromMarker && !inkMoved && line != null)
        {
            AnnotationEntryState work = Work(entry);
            if (mode == Mode.Challenge && work != null && work.highlightLineId == line &&
                IncomeChallenge.IsFlagged(Challenge, entry.id, AnnotationSlot.Highlight))
            {
                SayHint(AnnotationSlot.Highlight);
                ReturnMarkerAnimated();
                return;
            }
            // A plain tap on a line: the pen draws it for you.
            if (CommitHighlight(line)) StartCoroutine(Busy(TraceAndReturn(line)));
            else ReturnMarkerAnimated();
            return;
        }
        if (inkFromMarker && !inkMoved)
        {
            // Picking up the pen is its own action. The next drag begins at
            // the raised pen and can travel onto any printed line.
            if (mode == Mode.Challenge && IncomeChallenge.IsFlagged(Challenge, entry.id, AnnotationSlot.Highlight))
                SayHint(AnnotationSlot.Highlight);
            LiftMarkerAnimated();
            return;
        }
        // Too short: the wet ink fades and the pen goes home.
        if (line != null && sweep.Locked) StartCoroutine(FadeStroke(line, sweep.Min, sweep.Max));
        if (inkFromMarker) LiftMarkerAnimated();
        else ReturnMarkerAnimated();
        if (line != null && sweep.Locked) Level2Say.Line("Keep going: swipe further across the line.", null, 2.5f);
        else if (inkFromMarker) Level2Say.Line("Drag the highlighter across a line on the paper.", null, 2.5f);
    }

    private void CancelInk(bool instant = false)
    {
        if (!inking) return;
        inking = false;
        markerHit.enabled = true;
        if (paper != null)
        {
            paper.SetNib(null, 0f);
            paper.SetHover(null);
            ApplyHighlight(instant: true);
        }
        if (instant) ReturnMarker();
        else ReturnMarkerAnimated();
    }

    /// <summary>Records the selected source line. True when it changed.</summary>
    private bool CommitHighlight(string lineId)
    {
        if (mode == Mode.Challenge && IncomeChallenge.IsLocked(Challenge, entry.id, AnnotationSlot.Highlight))
        {
            Level2Say.Line("That highlighted line is already right. Fix the red notes.", null, 3f);
            return false;
        }
        AnnotationEntryState work = Work(entry);
        if (work == null || !AnnotationActions.Highlight(work, entry, lineId)) return false;
        paper.ClearStrokes(lineId);
        if (mode == Mode.Challenge)
        {
            IncomeChallenge.NoteEdited(Challenge, entry.id, AnnotationSlot.Highlight);
            paper.SetHighlightResult(lineId, 0);
            RefreshResults();
        }
        if (mode == Mode.Tutorial)
            Level2Say.Line(lineId == entry.demo.highlightLineId
                ? "Good. That line shows the money and why it was paid."
                : "That line doesn't show the money paid. Mark the line I marked.", null, 4f);
        RefreshTabs();
        CheckTutorialDone();
        return true;
    }

    private IEnumerator FinishStroke(string lineId, float from, float to)
    {
        bool changed = CommitHighlight(lineId);
        if (!changed && mode == Mode.Challenge && IncomeChallenge.IsLocked(Challenge, entry.id, AnnotationSlot.Highlight))
        {
            paper.ClearStrokes();
            ApplyHighlight(instant: true);
            ReturnMarkerAnimated();
            yield break;
        }
        // Sweep the rest of the line quickly, then flourish.
        float a = from, b = to;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.12f)
        {
            float e = EaseOutCubic(t);
            paper.SetStroke(lineId, Mathf.Lerp(a, 0f, e), Mathf.Lerp(b, 1f, e));
            yield return null;
        }
        paper.SetStroke(lineId, 0f, 1f);
        yield return StrokeFlourish(lineId, changed);
        ReturnMarkerAnimated();
    }

    private IEnumerator TraceAndReturn(string lineId)
    {
        yield return Trace(lineId);
    }

    private IEnumerator StrokeFlourish(string lineId, bool sound)
    {
        if (paper.TryLineEnds(lineId, out Vector3 start, out Vector3 end))
        {
            fx.Sparkle(end, transform.up, AnnotationPaper.InkYellow, 16);
            fx.Sparkle(start, transform.up, AnnotationPaper.InkYellow, 8);
        }
        if (sound && AudioManager.Instance != null) AudioManager.Instance.PlayExactEffect("sfx_card_return_02", 0.55f);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.28f)
        {
            paper.SetSourceTag(lineId, EaseOutBack(t));
            paper.BumpValue(lineId, 1f + 0.18f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        paper.SetSourceTag(lineId, 1f);
        paper.BumpValue(lineId, 1f);
    }

    private IEnumerator FadeStroke(string lineId, float from, float to)
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
        {
            paper.SetStroke(lineId, Mathf.Lerp(from, (from + to) * 0.5f, t), Mathf.Lerp(to, (from + to) * 0.5f, t));
            yield return null;
        }
        ApplyHighlight(instant: true);
    }

    /// <summary>Marker lifts, traces the line (~0.2 s) leaving the ink, then goes back.</summary>
    private IEnumerator Trace(string lineId)
    {
        if (paper == null || !paper.TryLineEnds(lineId, out Vector3 start, out Vector3 end)) yield break;
        paper.ClearStrokes(lineId);
        paper.SetStroke(lineId, 0f, 0f);
        Quaternion pen = HeldPen(0f);
        Vector3 offset = pen * (Vector3.up * NibOffset * MarkerScale);
        Vector3 fromPos = marker.position;
        Quaternion fromRot = marker.rotation;
        Vector3 above = start + offset + transform.up * 0.04f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / MarkerMoveSeconds)
        {
            float e = EaseOutCubic(t);
            marker.position = Vector3.Lerp(fromPos, above, e) + transform.up * Mathf.Sin(e * Mathf.PI) * 0.05f;
            marker.rotation = Quaternion.Slerp(fromRot, pen, e);
            yield return null;
        }
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.06f)
        {
            marker.position = Vector3.Lerp(above, start + offset, t);
            yield return null;
        }
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / TraceSeconds)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            marker.position = Vector3.Lerp(start, end, e) + HeldPen(e) * (Vector3.up * NibOffset * MarkerScale);
            marker.rotation = HeldPen(e);
            paper.SetStroke(lineId, 0f, e);
            paper.SetNib(lineId, e);
            yield return null;
        }
        paper.SetNib(null, 0f);
        paper.SetStroke(lineId, 0f, 1f);
        yield return StrokeFlourish(lineId, true);
        yield return ReturnMarkerRoutine();
    }

    private void ReturnMarkerAnimated()
    {
        markerPickedUp = false;
        if (markerMotion != null) StopCoroutine(markerMotion);
        markerMotion = StartCoroutine(ReturnMarkerRoutine());
    }

    private void LiftMarkerAnimated()
    {
        markerPickedUp = true;
        if (markerMotion != null) StopCoroutine(markerMotion);
        markerHit.enabled = false;
        markerMotion = StartCoroutine(LiftMarkerRoutine());
    }

    private IEnumerator LiftMarkerRoutine()
    {
        Vector3 fromPos = marker.localPosition;
        Quaternion fromRot = marker.localRotation;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.18f)
        {
            float e = EaseOutBack(t);
            marker.localPosition = Vector3.Lerp(fromPos, markerPickupPosition, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.018f;
            marker.localRotation = Quaternion.Slerp(fromRot, markerPickupRotation, e);
            yield return null;
        }
        marker.localPosition = markerPickupPosition;
        marker.localRotation = markerPickupRotation;
        markerHit.enabled = true;
        markerMotion = null;
    }

    private IEnumerator ReturnMarkerRoutine()
    {
        Vector3 fromPos = marker.localPosition;
        Quaternion fromRot = marker.localRotation;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.22f)
        {
            float e = EaseOutCubic(t);
            marker.localPosition = Vector3.Lerp(fromPos, markerRestPosition, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.04f;
            marker.localRotation = Quaternion.Slerp(fromRot, markerRestRotation, e);
            yield return null;
        }
        ReturnMarker();
        markerMotion = null;
    }

    private void ReturnMarker()
    {
        markerPickedUp = false;
        marker.localPosition = markerRestPosition;
        marker.localRotation = markerRestRotation;
        if (markerHit != null) markerHit.enabled = true;
    }

    // =========================================================
    // ACTIONS (write the player's work through AnnotationActions)
    // =========================================================

    private void DoPlace(AnnotationCard card, AnnotationSlot slot)
    {
        if (card == null) return;
        selected = null;
        card.selected = false;
        AnnotationEntryState work = Work(entry);
        if (work == null) return;
        if (!AnnotationActions.Fits(card.kind, slot))
        {
            // Wrong kind of place: shake back, no penalty.
            paper.Flash(slot, AnnotationPaper.WrongColor, 0.5f);
            StartCard(card, ShakeHome(card));
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            Level2Say.Line(card.kind == AnnotationSlot.Evidence
                ? "Record tags go on the EVIDENCE place."
                : "This card goes on the " + AnnotationPaper.SlotNames[(int)card.kind] + " place.", null, 3f);
            return;
        }
        if (Locked(slot))
        {
            RefuseLocked(FindPlacedCard(slot));
            if (card.placed) LayoutPlaced(card.kind, animate: true);
            else ReturnHome(card);
            return;
        }
        if (slot == AnnotationSlot.Evidence && !card.placed && work.evidenceIds != null &&
            work.evidenceIds.Count >= System.Math.Max(1, entry.maxEvidence) && entry.maxEvidence > 1)
            Level2Say.Line("The clip holds " + entry.maxEvidence + " records. The oldest one comes back.", null, 3f);
        if (!AnnotationActions.Place(work, options, entry, card.kind, slot, card.optionId, out string displaced))
        {
            if (card.placed) LayoutPlaced(card.kind, animate: true);
            else ReturnHome(card);
            return;
        }
        if (mode == Mode.Challenge)
        {
            IncomeChallenge.NoteEdited(Challenge, entry.id, slot);
            paper.SetResult(slot, 0);
        }
        if (displaced != null)
        {
            AnnotationCard old = FindCard(card.kind, displaced);
            if (old != null)
            {
                old.placed = false;
                ReturnHome(old);
            }
        }
        card.placed = true;
        LayoutPlaced(slot, animate: true, landing: card);
        if (slot == AnnotationSlot.Evidence) StartCoroutine(ClipTo(true));

        if (mode == Mode.Tutorial)
        {
            AnnotationOptionDefinition option = options.FindOption(card.kind, card.optionId, entry);
            bool repeats = AnnotationActions.MatchesDemonstration(entry, slot, card.optionId);
            string rule = option != null && !string.IsNullOrWhiteSpace(option.rule) ? option.label + ": " + option.rule : null;
            Level2Say.Line(repeats ? rule : (rule != null ? rule + " " : string.Empty) + "That isn't what I placed for this payslip. Try another.", null, 5f);
            CheckTutorialDone();
        }
        RefreshTabs();
        AnnounceReady();
    }

    private void DoRemove(AnnotationCard card)
    {
        if (card == null) return;
        if (Locked(card.kind))
        {
            RefuseLocked(card);
            return;
        }
        AnnotationEntryState work = Work(entry);
        if (work == null || !AnnotationActions.Remove(work, card.kind, card.optionId)) return;
        if (mode == Mode.Challenge)
        {
            IncomeChallenge.NoteEdited(Challenge, entry.id, card.kind);
            paper.SetResult(card.kind, 0);
        }
        card.placed = false;
        ReturnHome(card);
        LayoutPlaced(card.kind, animate: true);
        if (card.kind == AnnotationSlot.Evidence && (work.evidenceIds == null || work.evidenceIds.Count == 0))
            StartCoroutine(ClipTo(false));
        RefreshTabs();
    }

    private void RefuseLocked(AnnotationCard card)
    {
        if (card != null) StartCard(card, Nod(card));
        Level2Say.Line("That note is already right. Fix the red ones.", null, 3f);
    }

    private void SayHint(AnnotationSlot slot)
    {
        AnnotationKeyDefinition key = definition?.review?.FindKey(entry.id);
        string hint = IncomeChallenge.Hint(key, slot, Challenge != null ? Challenge.checks : 1);
        string label = slot == AnnotationSlot.Highlight ? "HIGHLIGHT" : AnnotationPaper.SlotNames[(int)slot];
        if (!string.IsNullOrEmpty(hint)) Level2Say.Line(label + ": " + hint, null, 6f);
    }

    private void CheckTutorialDone()
    {
        CaseReviewState review = Review;
        if (mode != Mode.Tutorial || review == null || !review.TryCompleteTutorial(entry)) return;
        StartCoroutine(TutorialDone());
    }

    private IEnumerator TutorialDone()
    {
        busy++;
        yield return Wait(0.45f, ignorePause: true);
        fx.Confetti(paper.Root.position + transform.up * 0.08f, transform.up, 60);
        Level2Say.Line(entry.demo.doneLine, "wow", 5f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayAchievementSFX();
        GameplayEvents.RaiseAnnotationTutorialCompleted();
        yield return Wait(2.8f);
        Release();
        if (options.entries != null && options.entries.Count > 0 && open) yield return BeginChallenge();
    }

    /// <summary>The practice page flips away and Rosa's four records are dealt out.</summary>
    private IEnumerator BeginChallenge()
    {
        CaseReviewState review = Review;
        if (review == null || IncomeChallenge.Ensure(review, definition?.review) == null) yield break;
        busy++;
        mode = Mode.Challenge;
        foreach (AnnotationCard card in cards)
            if (card.placed) Fly(card, transform.TransformPoint(card.homePosition), transform.rotation * card.homeRotation, 1f, 0.2f, 0.05f, 0f);
        foreach (AnnotationCard card in cards) card.placed = false;
        SnapClip(false);
        Release();
        yield return SwitchEntry(options.entries[0]);
        busy++;

        if (!open)
        {
            Release();
            yield break;
        }

        yield return ShowChallengeFurniture();
        RefreshScoreboard();
        review.challenge.introSeen = true;
        Level2Say.Line("Now Rosa's four records. Mark each one, add its notes, then press CHECK. Your first check sets the badge.", "wow", 6f);
        Release();
    }

    /// <summary>Every record is marked for the first time: the CHECK button invites a press.</summary>
    private void AnnounceReady()
    {
        if (mode != Mode.Challenge || Challenge == null || Challenge.firstChecked) return;
        if (IncomeChallenge.Missing(Challenge, options).Count == 0)
            Level2Say.Line("All four records are marked. Press CHECK when you're sure.", null, 4f);
    }

    // =========================================================
    // CHECK, RESULTS AND THE AWARD
    // =========================================================

    private IEnumerator PressCheck()
    {
        if (mode != Mode.Challenge) yield break;
        CaseReviewState ethicsReview = Review;
        string caseId = definition != null ? definition.caseId : "";
        PlayerProgressManager ethicsProgress = PlayerProgressManager.Instance;
        bool signedEthicsChoice = ethicsProgress != null && ethicsProgress.UsesLevelTwoStory && !Level2Checkpointer.IsDirectEditorAttempt &&
                                  Level2CampaignRules.EthicsChoiceCommitted(ethicsProgress.CurrentProgress, caseId);
        if (ethicsReview != null && Challenge != null && !Challenge.firstChecked &&
            IncomeChallenge.Missing(Challenge, options).Count == 0 &&
            !Level2Checkpoint.EthicsChoiceCommitted(ethicsReview, caseId) &&
            !signedEthicsChoice)
        {
            MarketRecordsDesk desk = GetComponentInParent<MarketRecordsDesk>();
            if (Level2RosaConsultation.Instance == null || !Level2RosaConsultation.Instance.TryDiscussFromDesk(desk))
                Level2Say.Line("Rosa's conversation could not start. Your marks are kept; try CHECK again or use Exit Desk.", "thinking", 6f);
            yield break;
        }
        busy++;
        // Physical press: the cap sinks and springs back.
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.08f)
        {
            checkCap.localPosition = new Vector3(0f, 0.02f - 0.008f * t, 0f);
            yield return null;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.16f)
        {
            checkCap.localPosition = new Vector3(0f, 0.012f + 0.008f * EaseOutBack(t), 0f);
            yield return null;
        }
        checkCap.localPosition = new Vector3(0f, 0.02f, 0f);

        CaseReviewState review = Review;
        bool first = review != null && review.challenge != null && !review.challenge.firstChecked;
        IncomeChallenge.CheckResult result = IncomeChallenge.TryCheck(review, definition.review, out List<IncomeChallenge.Decision> decisions, out List<string> missing);
        if (result == IncomeChallenge.CheckResult.Incomplete)
        {
            yield return ShowMissing(missing);
            Release();
            yield break;
        }
        if (result != IncomeChallenge.CheckResult.Checked)
        {
            Release();
            yield break;
        }
        if (first) GameplayEvents.RaiseAnnotationsChecked();
        yield return RevealResults(decisions, first);
        Release();
        if (review.challenge.mastered) yield return Mastery();
    }

    /// <summary>Unscored: the button shakes and names what is still missing.</summary>
    private IEnumerator ShowMissing(List<string> missing)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        var byEntry = new Dictionary<string, List<string>>();
        foreach (string item in missing)
        {
            int colon = item.IndexOf(':');
            string id = colon > 0 ? item.Substring(0, colon) : item;
            string what = colon > 0 ? item.Substring(colon + 1).Trim() : item;
            if (!byEntry.TryGetValue(id, out List<string> list)) byEntry[id] = list = new List<string>();
            list.Add(what);
        }
        foreach (Tab tab in tabs) if (byEntry.ContainsKey(tab.entryId)) tab.flashUntil = Time.unscaledTime + 1.2f;
        if (byEntry.TryGetValue(entry.id, out List<string> here))
        {
            if (here.Exists(w => w.StartsWith("highlight", System.StringComparison.OrdinalIgnoreCase)))
                StartCoroutine(Pop(marker, 1.15f, 0.3f));
            foreach (AnnotationSlot slot in IncomeChallenge.ScoredSlots)
                if (slot != AnnotationSlot.Highlight &&
                    here.Exists(w => w.StartsWith(AnnotationPaper.SlotNames[(int)slot]) || (slot == AnnotationSlot.Evidence && w.StartsWith("EVIDENCE"))))
                    paper.Flash(slot, AnnotationPaper.WrongColor, 1.2f);
        }
        string firstId = byEntry.Count > 0 ? new List<string>(byEntry.Keys)[0] : null;
        if (firstId != null)
            Level2Say.Line("Not yet: " + firstId + " still needs " + string.Join(", ", byEntry[firstId]) + ". Nothing was scored.", null, 5f);
        Vector3 home = checkButton.localPosition;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
        {
            checkButton.localPosition = home + Vector3.right * Mathf.Sin(t * Mathf.PI * 6f) * 0.008f * (1f - t);
            yield return null;
        }
        checkButton.localPosition = home;
    }

    private IEnumerator RevealResults(List<IncomeChallenge.Decision> decisions, bool first)
    {
        // Tab pips flip one by one.
        for (int i = 0; i < decisions.Count; i++)
        {
            IncomeChallenge.Decision d = decisions[i];
            Tab tab = tabs.Find(t => t.entryId == d.EntryId);
            if (tab != null)
            {
                tab.pips[(int)d.Slot].color = d.Correct ? AnnotationPaper.FitColor : AnnotationPaper.WrongColor;
                StartCoroutine(Pop(tab.body, 1.12f, 0.12f));
                if (d.Correct) fx.Sparkle(tab.root.position + transform.up * 0.01f, transform.up, AnnotationPaper.FitColor, 6, 0.3f);
            }
            if (AudioManager.Instance != null && i % 2 == 0) AudioManager.Instance.PlayMouseClickSelectSFX();
            yield return Wait(0.08f, ignorePause: true);
        }
        RefreshResults(animate: true);
        RefreshTabs();
        foreach (AnnotationCard card in cards)
            if (card.placed && !Locked(card.kind) && IncomeChallenge.IsFlagged(Challenge, entry.id, card.kind))
                StartCard(card, Shake(card));

        int correct = 0;
        foreach (IncomeChallenge.Decision d in decisions) if (d.Correct) correct++;
        if (first)
        {
            yield return CountUp(Challenge.firstCorrect);
            BadgeTier tier = Tier();
            Level2Say.Line(TierLine(tier, Challenge.firstCorrect, Challenge.firstTotal), tier == BadgeTier.Gold ? "wow" : null, 6f);
        }
        else if (correct < decisions.Count)
        {
            Level2Say.Line((decisions.Count - correct) + " still need another look. Tap a red note for a hint. Your badge stays as it is.", null, 5f);
        }
    }

    private IEnumerator CountUp(int target)
    {
        RefreshScoreboard();
        for (int i = 0; i <= target; i++)
        {
            scoreCount.text = i + "/16";
            scoreCount.transform.localScale = Vector3.one * 1.25f;
            if (AudioManager.Instance != null && i > 0) AudioManager.Instance.PlayKeyboardSFX();
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.07f)
            {
                scoreCount.transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, t);
                yield return null;
            }
        }
        RefreshScoreboard();
        if (scoreMedal.enabled)
        {
            StartCoroutine(Pop(scoreMedal.transform, 1.4f, 0.4f));
            fx.Sparkle(scoreMedal.transform.position, transform.up, HudBadgeArt.TierColor(Tier()), 24, 0.5f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAchievementSFX();
        }
    }

    private BadgeTier Tier()
    {
        LevelRewardRules rules = CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.Score.Rules : null;
        int correct = Challenge != null ? Challenge.firstCorrect : 0;
        if (rules != null && LevelScoring.UsesAccuracyBadge(rules)) return LevelScoring.AccuracyBadge(rules, correct);
        return correct >= 16 ? BadgeTier.Gold : correct >= 14 ? BadgeTier.Silver : BadgeTier.Bronze;
    }

    private static string TierLine(BadgeTier tier, int correct, int total)
    {
        switch (tier)
        {
            case BadgeTier.Gold: return correct + " out of " + total + " on your first check: Gold! Every note is right.";
            case BadgeTier.Silver: return correct + "/" + total + " on your first check: Silver. Fix the red notes to finish.";
            default: return correct + "/" + total + " on your first check: Bronze. Tap a red note for a hint, fix it, then CHECK again.";
        }
    }

    /// <summary>Everything right: confetti, the annotated list, pack up, then the award (once).</summary>
    private IEnumerator Mastery()
    {
        busy++;
        ChallengeState challenge = Challenge;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayAchievementSFX();
        fx.Confetti(paper.Root.position + transform.up * 0.1f, transform.up, 140);
        Level2Say.Line("Every note is right! Here is your Annotated Income List.", "wow", 5f);
        for (int i = 0; i < tabs.Count; i++)
        {
            StartCoroutine(Pop(tabs[i].body, 1.18f, 0.2f));
            yield return Wait(0.08f, ignorePause: true);
        }
        yield return Wait(0.6f, ignorePause: true);

        // The Annotated Income List flips onto the clipboard.
        foreach (AnnotationCard card in cards)
            if (card.placed) Fly(card, transform.TransformPoint(card.boxPosition), transform.rotation, BoxScale, 0.3f, 0.08f, Random.Range(0f, 0.15f));
        foreach (AnnotationCard card in cards) card.placed = false;
        SnapClip(false);
        paper.SetVisible(false);
        ShowList(challenge);
        yield return Wait(3.2f, ignorePause: true);

        // Pack up: list and cards into the case, lid shuts.
        Vector3 from = listSheet.position;
        Vector3 into = caseRoot.TransformPoint(new Vector3(0f, 0.03f, 0f));
        Quaternion fromRot = listSheet.rotation;
        foreach (AnnotationCard card in cards)
            Fly(card, transform.TransformPoint(card.boxPosition), transform.rotation, BoxScale, 0.3f, 0.08f, Random.Range(0f, 0.2f));
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
        {
            float e = EaseInOutCubic(t);
            listSheet.position = Vector3.Lerp(from, into, e) + transform.up * Mathf.Sin(e * Mathf.PI) * 0.12f;
            listSheet.rotation = Quaternion.Slerp(fromRot, transform.rotation, e);
            listSheet.localScale = Vector3.one * Mathf.Lerp(PaperScale, 0.5f, e);
            yield return null;
        }
        listSheet.gameObject.SetActive(false);
        SetLid(0f, 0.5f);
        yield return Wait(0.55f, ignorePause: true);
        fx.Sparkle(caseRoot.position + transform.up * 0.08f, transform.up, new Color32(241, 194, 108, 255), 30, 0.6f);

        Award(challenge);
        mode = Mode.Done;
        Release();
    }

    private void ShowList(ChallengeState challenge)
    {
        if (listSheet == null)
        {
            ToolkitBuilder b = builder;
            listSheet = b.Node("Annotated Income List", transform, PaperCenter + new Vector3(0f, 0.004f, 0f), Quaternion.identity);
            b.Box("Paper", listSheet, Vector3.zero, new Vector3(AnnotationPaper.Width, 0.002f, AnnotationPaper.Depth), b.Lit(new Color32(252, 249, 240, 255), "List Paper"));
            b.Box("Header", listSheet, new Vector3(0f, 0.0011f, 0.18f), new Vector3(AnnotationPaper.Width, 0.0004f, 0.06f), b.Lit(new Color32(46, 158, 106, 255), "List Header"));
            b.Text(listSheet, new Vector3(0f, 0.0015f, 0.185f), new Vector2(AnnotationPaper.Width - 0.04f, 0.03f), "ANNOTATED INCOME LIST", Color.white, 0.36f, TextAlignmentOptions.Center, true);
            b.Text(listSheet, new Vector3(0f, 0.0015f, 0.13f), new Vector2(AnnotationPaper.Width - 0.04f, 0.016f), "Each record with its type, treatment and evidence", new Color32(96, 90, 82, 255), 0.18f);
            List<string> rows = IncomeChallenge.AnnotatedList(challenge, options);
            for (int i = 0; i < rows.Count; i++)
                b.Text(listSheet, new Vector3(0f, 0.0015f, 0.085f - i * 0.065f), new Vector2(AnnotationPaper.Width - 0.04f, 0.055f), rows[i], Ink, 0.24f, TextAlignmentOptions.Left);
            listSheet.localScale = Vector3.one * PaperScale;
        }
        listSheet.gameObject.SetActive(true);
        listSheet.localPosition = PaperCenter + new Vector3(0f, 0.004f, 0f);
        listSheet.localRotation = Quaternion.identity;
        StartCoroutine(Pop(listSheet, 1.06f, 0.3f));
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    /// <summary>
    /// The quest award, exactly once (IncomeChallenge.TryAward): EXP from the
    /// frozen first check, completion hearts and the accuracy medal through
    /// the existing ledger, then the existing reward card.
    /// </summary>
    private void Award(ChallengeState challenge)
    {
        CaseReviewState review = Review;
        if (!IncomeChallenge.TryAward(review)) return;
        BadgeTier tier = Tier();
        int wrong = IncomeChallenge.FirstWrongCount(challenge);
        ScoreEntry entryScore = CaseProgressionManager.Instance != null
            ? CaseProgressionManager.Instance.Score.AddSideQuest(wrong, transform.position + transform.up * 0.3f, tier)
            : null;
        GameplayEvents.RaiseIncomeQuestCompleted();
        Level2Say.Line("Your annotations are ready to take back. Now check what the prepared summary actually accounts for.", null, 7f);

        LevelRewardRules rules = CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.Score.Rules : null;
        var data = new RewardScreenData
        {
            Title = "Side Quest Complete",
            Subtitle = rules != null ? rules.sideQuest.badgeName : "Mark the Money!",
            Exp = entryScore != null ? entryScore.Exp : 0,
            MaxExp = entryScore != null ? entryScore.MaxExp : 0,
            Hearts = entryScore != null ? entryScore.Hearts : 0,
            MaxHearts = entryScore != null ? entryScore.MaxHearts : 0,
            Summary = "You can tell where money comes from, how it is treated and which record proves it. Rosa's summary still needs checking at the office.",
            MedalLabel = tier + " badge",
            Accent = HudBadgeArt.TierColor(tier)
        };
        Sprite medal = HudBadgeArt.For(rules, tier);
        if (medal != null) data.Medals.Add(medal);
        data.Fact("First check", challenge.firstCorrect + "/" + challenge.firstTotal);
        data.Fact("Badge", tier.ToString());
        if (tier != BadgeTier.Gold) data.Footer = "Gold needs 16/16 on the first check.";
        List<string> annotatedRows = IncomeChallenge.AnnotatedList(challenge, options);
        System.Action showAnnotatedPapers = () =>
        {
            Level2AnnotatedPapers collection = Level2AnnotatedPapers.Instance;
            if (collection == null)
                collection = FindAnyObjectByType<Level2AnnotatedPapers>(FindObjectsInactive.Include);
            if (collection != null) collection.PresentAwarded(annotatedRows);
        };
        System.Action afterContinue = () =>
        {
            showAnnotatedPapers();
            StartCoroutine(LeaveDeskAfterCollection());
        };
        if (SideQuestCompleteUI.Instance != null) SideQuestCompleteUI.Instance.ShowScreen(data, afterContinue);
        else afterContinue();
        RefreshScoreboard();
    }

    /// <summary>
    /// After Continue on Side Quest Complete: let the annotated papers fly
    /// into their icon, then stand up from the records desk automatically
    /// (the quest is over; the office review is next).
    /// </summary>
    private IEnumerator LeaveDeskAfterCollection()
    {
        // One frame for the collection to start, then wait for it (and any card) to finish.
        yield return null;
        for (float waited = 0f; waited < 8f; waited += Time.unscaledDeltaTime)
        {
            bool collecting = Level2AnnotatedPapers.Instance != null && Level2AnnotatedPapers.Instance.IsCollecting;
            if (!collecting && !HudRewardScreen.AnyOpen && !SideQuestSplash.IsShowing) break;
            yield return null;
        }
        yield return Wait(0.25f, ignorePause: true);
        CameraController camera = CameraController.Instance;
        if (open && camera != null && camera.CurrentMode == CameraController.CameraMode.Workstation) camera.ExitFirstPerson();
    }

    // =========================================================
    // VIEW REFRESH
    // =========================================================

    /// <summary>Cards, highlight and clip where the player's work says (onlyPlaced: leave loose cards where they are).</summary>
    private void ApplyState(bool instant, bool onlyPlaced)
    {
        AnnotationEntryState work = Work(entry);
        foreach (AnnotationCard card in cards)
        {
            bool on = work != null && (card.kind == AnnotationSlot.Evidence ? work.HasEvidence(card.optionId) : work.Get(card.kind) == card.optionId);
            card.placed = on;
            if (!on && !onlyPlaced)
            {
                if (instant) Snap(card, transform.TransformPoint(open && card.available ? card.homePosition : card.boxPosition), transform.rotation * card.homeRotation, open && card.available ? 1f : BoxScale);
                else ReturnHome(card);
            }
        }
        for (int s = 0; s < 3; s++) LayoutPlaced((AnnotationSlot)s, animate: !instant);
        ApplyHighlight(instant);
        bool clipped = work != null && work.evidenceIds != null && work.evidenceIds.Count > 0;
        if (instant) SnapClip(clipped);
        else StartCoroutine(ClipTo(clipped));
    }

    private void ApplyHighlight(bool instant)
    {
        if (paper == null) return;
        AnnotationEntryState work = Work(entry);
        string line = work != null ? work.highlightLineId : null;
        paper.ClearStrokes(line);
        if (line != null)
        {
            paper.SetStroke(line, 0f, 1f);
            paper.SetSourceTag(line, 1f);
        }
    }

    private void RefreshResults(bool animate = false)
    {
        if (paper == null) return;
        AnnotationEntryState work = Work(entry);
        foreach (AnnotationSlot slot in IncomeChallenge.ScoredSlots)
        {
            int result = mode == Mode.Challenge || mode == Mode.Done
                ? IncomeChallenge.IsLocked(Challenge, entry.id, slot) ? 1 : IncomeChallenge.IsFlagged(Challenge, entry.id, slot) ? -1 : 0
                : 0;
            if (slot == AnnotationSlot.Highlight)
            {
                string lineId = work != null ? work.highlightLineId : null;
                paper.SetHighlightResult(lineId, result);
                Transform badge = paper.HighlightResultBadge(lineId);
                if (animate && result != 0 && badge != null) StartCoroutine(PopIn(badge, 1f, 0.25f));
                continue;
            }
            bool was = paper.Result(slot) != 0;
            paper.SetResult(slot, result);
            if (animate && result != 0 && !was) StartCoroutine(PopIn(paper.ResultBadge(slot), 0f, 0.25f));
        }
        foreach (AnnotationCard card in cards)
        {
            bool locked = card.placed && Locked(card.kind);
            if (card.lockBadge.gameObject.activeSelf != locked) card.lockBadge.gameObject.SetActive(locked);
            card.locked = locked;
        }
    }

    private void RefreshTabs()
    {
        if (tabs.Count == 0 || options?.entries == null) return;
        ChallengeState challenge = Challenge;
        foreach (Tab tab in tabs)
        {
            AnnotationEntryState work = challenge != null ? challenge.Find(tab.entryId) : null;
            for (int p = 0; p < IncomeChallenge.ScoredSlots.Length; p++)
            {
                var slot = IncomeChallenge.ScoredSlots[p];
                Color c;
                if (IncomeChallenge.IsLocked(challenge, tab.entryId, slot)) c = AnnotationPaper.FitColor;
                else if (IncomeChallenge.IsFlagged(challenge, tab.entryId, slot)) c = AnnotationPaper.WrongColor;
                else
                {
                    bool filled = work != null && (slot == AnnotationSlot.Highlight ? !string.IsNullOrEmpty(work.highlightLineId) :
                        slot == AnnotationSlot.Evidence ? work.evidenceIds != null && work.evidenceIds.Count > 0 : !string.IsNullOrEmpty(work.Get(slot)));
                    c = filled ? PipFilled : PipEmpty;
                }
                tab.pips[p].color = c;
            }
            bool allRight = IncomeChallenge.IsLocked(challenge, tab.entryId, AnnotationSlot.Type) &&
                            IncomeChallenge.IsLocked(challenge, tab.entryId, AnnotationSlot.Treatment) &&
                            IncomeChallenge.IsLocked(challenge, tab.entryId, AnnotationSlot.Evidence) &&
                            IncomeChallenge.IsLocked(challenge, tab.entryId, AnnotationSlot.Highlight);
            bool wasDone = tab.done.gameObject.activeSelf;
            // The tick only ever means "confirmed right by a check", never just "filled in".
            tab.done.gameObject.SetActive(allRight);
            if (!wasDone && tab.done.gameObject.activeSelf) StartCoroutine(PopIn(tab.done, 0f, 0.25f));
        }
    }

    private void RefreshScoreboard()
    {
        if (scoreStatus == null) return;
        ChallengeState challenge = Challenge;
        bool checkedOnce = challenge != null && challenge.firstChecked;
        scoreTitle.color = challenge != null && challenge.awarded ? new Color32(206, 170, 96, 255) : new Color32(46, 158, 106, 255);
        scoreStatus.text = mode == Mode.Tutorial ? "Practice: not scored"
            : challenge != null && challenge.awarded ? "Complete!"
            : checkedOnce ? "Fix the red notes" : "Annotate E1-E4, then CHECK";
        scoreCount.text = checkedOnce ? challenge.firstCorrect + "/" + challenge.firstTotal : "-/16";
        for (int i = 0; i < 16; i++)
        {
            Color c = PipEmpty;
            if (checkedOnce && options?.entries != null && i / IncomeChallenge.ScoredSlots.Length < options.entries.Count)
            {
                string id = IncomeChallenge.DecisionId(options.entries[i / IncomeChallenge.ScoredSlots.Length].id,
                    IncomeChallenge.ScoredSlots[i % IncomeChallenge.ScoredSlots.Length]);
                c = challenge.firstWrong != null && challenge.firstWrong.Contains(id) ? AnnotationPaper.WrongColor : AnnotationPaper.FitColor;
            }
            scorePipMaterials[i].color = c;
        }
        if (checkedOnce)
        {
            BadgeTier tier = Tier();
            LevelRewardRules rules = CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.Score.Rules : null;
            Sprite sprite = HudBadgeArt.For(rules, tier);
            scoreMedal.sprite = sprite;
            scoreMedal.enabled = sprite != null;
            if (sprite != null)
            {
                float size = Mathf.Max(0.001f, sprite.bounds.size.x);
                scoreMedal.transform.localScale = Vector3.one * (0.07f / size);
            }
            scoreTier.text = tier.ToString().ToUpperInvariant();
            scoreTier.color = HudBadgeArt.TierColor(tier);
        }
    }

    /// <summary>Per-frame furniture: card glow and shadows, place hints, tab flashes, CHECK glow.</summary>
    private void AnimateFurniture()
    {
        if (paper != null)
            paper.Animate(selected != null ? selected.kind : dragging != null ? dragging.kind : (AnnotationSlot?)null,
                dragging != null && hoverSlot.HasValue && dragging.kind == hoverSlot.Value);
        float now = Time.unscaledTime;
        if (markerPickedUp && !inking && markerMotion == null)
            marker.localPosition = markerPickupPosition + Vector3.up * (0.0025f * Mathf.Sin(now * 3.5f));
        foreach (AnnotationCard card in cards)
        {
            float target = card.selected ? 0.55f + 0.2f * Mathf.Sin(now * 8f) : 0f;
            card.glow = Mathf.MoveTowards(card.glow, target, Time.unscaledDeltaTime * 4f);
            Color g = card.glowMaterial.color;
            card.glowMaterial.color = new Color(g.r, g.g, g.b, card.glow);
            // Shadow on the desk or the paper, softer and wider the higher the card is.
            Vector3 local = transform.InverseTransformPoint(card.root.position);
            bool overPaper = Mathf.Abs(local.x - PaperCenter.x) < AnnotationPaper.Width * 0.5f * PaperScale &&
                             Mathf.Abs(local.z - PaperCenter.z) < AnnotationPaper.Depth * 0.5f * PaperScale;
            float surface = overPaper ? PaperCenter.y + 0.0026f : 0.0006f;
            float height = Mathf.Max(0f, local.y - surface);
            bool stowed = card.root.localScale.x <= BoxScale * 1.5f;
            if (card.body != null && card.body.gameObject.activeSelf == stowed) card.body.gameObject.SetActive(!stowed);
            bool show = card.root.gameObject.activeInHierarchy && card.root.localScale.x > 0.5f;
            if (card.shadow.gameObject.activeSelf != show) card.shadow.gameObject.SetActive(show);
            card.shadow.localPosition = new Vector3(local.x + height * 0.25f, surface, local.z - height * 0.35f);
            card.shadow.localRotation = Quaternion.Euler(0f, (Quaternion.Inverse(transform.rotation) * card.root.rotation).eulerAngles.y, 0f);
            card.shadow.localScale = Vector3.one * card.root.localScale.x * (1f + height * 4f);
            card.shadowMaterial.color = new Color(0f, 0f, 0f, Mathf.Clamp(0.24f - height * 2.4f, 0.05f, 0.24f));
            // Idle cards breathe very slightly so the table feels alive.
            if (!card.placed && !card.selected && card.motion == null && dragging != card && card.available && open)
                card.root.localPosition = card.homePosition + Vector3.up * (0.0015f * Mathf.Sin(now * 2f + card.homePosition.z * 20f));
        }
        float glowTarget = 0f;
        if (mode == Mode.Challenge && Challenge != null && !Challenge.mastered && IncomeChallenge.Missing(Challenge, options).Count == 0)
        {
            glowTarget = 0.35f + 0.3f * Mathf.Sin(now * 5f);
            checkCapMaterial.color = new Color32(46, 170, 96, 255);
        }
        else if (checkCapMaterial != null) checkCapMaterial.color = new Color32(150, 150, 150, 255);
        if (checkGlowMaterial != null) checkGlowMaterial.color = new Color(0.3f, 0.95f, 0.55f, glowTarget);
        foreach (Tab tab in tabs)
        {
            bool current = entry != null && tab.entryId == entry.id;
            Color face = current ? new Color32(255, 252, 240, 255) : new Color32(232, 214, 170, 255);
            if (now < tab.flashUntil) face = Color.Lerp(face, AnnotationPaper.WrongColor, Mathf.PingPong(now * 8f, 1f));
            tab.face.color = face;
            tab.root.localPosition = new Vector3(tab.root.localPosition.x, PaperCenter.y + (current ? 0.004f : 0.001f), tab.root.localPosition.z);
        }
    }

    /// <summary>After a long pause, gently show what to do next (no answers).</summary>
    private void Nudge()
    {
        if (Time.unscaledTime - idleSince < 9f || selected != null || dragging != null || inking) return;
        idleSince = Time.unscaledTime;
        AnnotationEntryState work = Work(entry);
        if (work == null) return;
        if (string.IsNullOrEmpty(work.highlightLineId))
        {
            StartCoroutine(Pop(marker, 1.12f, 0.3f));
            Level2Say.Line("Start by swiping the line that shows the money.", null, 3f);
            return;
        }
        if (mode == Mode.Challenge && IncomeChallenge.IsFlagged(Challenge, entry.id, AnnotationSlot.Highlight))
        {
            StartCoroutine(Pop(marker, 1.12f, 0.3f));
            return;
        }
        foreach (AnnotationSlot slot in IncomeChallenge.ScoredSlots)
        {
            if (slot == AnnotationSlot.Highlight) continue;
            bool empty = slot == AnnotationSlot.Evidence ? work.evidenceIds == null || work.evidenceIds.Count == 0 : string.IsNullOrEmpty(work.Get(slot));
            if (!empty) continue;
            paper.Flash(slot, AnnotationPaper.KindColor(slot), 1f);
            foreach (AnnotationCard card in cards)
                if (card.kind == slot && card.available && !card.placed && card.motion == null) StartCard(card, Wiggle(card, Random.Range(0f, 0.2f)));
            return;
        }
        if (mode == Mode.Challenge && checkButton != null && IncomeChallenge.Missing(Challenge, options).Count == 0)
            StartCoroutine(Pop(checkButton, 1.12f, 0.35f));
    }

    // =========================================================
    // CARD MOTION
    // =========================================================

    private void LayoutPlaced(AnnotationSlot slot, bool animate, AnnotationCard landing = null)
    {
        var placedCards = cards.FindAll(c => c.placed && c.kind == slot);
        paper.SetFilled(slot, placedCards.Count > 0);
        for (int i = 0; i < placedCards.Count; i++)
        {
            paper.SlotPose(slot, i, placedCards.Count, out Vector3 position, out Quaternion rotation);
            if (animate) StartCard(placedCards[i], SettleInto(placedCards[i], position, rotation, slot, placedCards[i] == landing));
            else Snap(placedCards[i], position, rotation, PlacedScale);
        }
    }

    private IEnumerator FlyToPlace(AnnotationCard card, AnnotationSlot slot, int index, int count)
    {
        // Presentation only: prevent desk idling from pulling the demo card home.
        // ApplyState restores these flags from the player's work after the demonstration.
        card.placed = true;
        paper.SetFilled(slot, true);
        paper.SlotPose(slot, index, count, out Vector3 position, out Quaternion rotation);
        StartCard(card, SettleInto(card, position, rotation, slot, true));
        yield return card.motion;
    }

    /// <summary>Fly onto the place, squash on landing and spring back, sparkle, tick and "Placed!".</summary>
    private IEnumerator SettleInto(AnnotationCard card, Vector3 position, Quaternion rotation, AnnotationSlot slot, bool celebrate)
    {
        yield return FlyRoutine(card, position + transform.up * 0.016f, rotation, PlacedScale * 1.08f, CardFlySeconds, 0.06f);
        Vector3 from = card.root.position;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / SettleSeconds)
        {
            card.root.position = Vector3.LerpUnclamped(from, position, EaseInCubic(t));
            yield return null;
        }
        card.root.SetPositionAndRotation(position, rotation);
        if (celebrate)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayExactEffect("sfx_card_place_01", 0.65f);
            fx.Sparkle(position + transform.up * 0.005f, transform.up, AnnotationPaper.KindColor(card.kind), 14);
            paper.ShowPlaced(slot);
            placedUntil = Time.unscaledTime + 0.9f;
        }
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.22f)
        {
            float wave = Mathf.Exp(-9f * t) * Mathf.Cos(t * 26f);
            card.root.localScale = new Vector3(PlacedScale * (1f + 0.12f * wave), PlacedScale, PlacedScale * (1f - 0.1f * wave));
            if (celebrate) paper.ShowPlaced(slot, 1f + 0.25f * Mathf.Exp(-8f * t));
            yield return null;
        }
        Snap(card, position, rotation, PlacedScale);
        card.motion = null;
    }

    private void ReturnHome(AnnotationCard card)
    {
        card.selected = false;
        bool dealt = open && card.available;
        Vector3 target = transform.TransformPoint(dealt ? card.homePosition : card.boxPosition);
        Fly(card, target, transform.rotation * card.homeRotation, dealt ? 1f : BoxScale, 0.2f, 0.04f, 0f);
    }

    private IEnumerator ShakeHome(AnnotationCard card)
    {
        Quaternion baseRot = card.root.rotation;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
        {
            card.root.rotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t * Mathf.PI * 6f) * 12f * (1f - t), 0f);
            yield return null;
        }
        bool wasPlaced = card.placed;
        if (wasPlaced)
        {
            card.motion = null;
            LayoutPlaced(card.kind, animate: true);
            yield break;
        }
        Vector3 target = transform.TransformPoint(card.homePosition);
        yield return FlyRoutine(card, target, transform.rotation * card.homeRotation, 1f, 0.2f, 0.04f);
        card.motion = null;
    }

    private IEnumerator Shake(AnnotationCard card)
    {
        Quaternion baseRot = card.root.rotation;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
        {
            card.root.rotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t * Mathf.PI * 6f) * 8f * (1f - t), 0f);
            yield return null;
        }
        card.root.rotation = baseRot;
        card.motion = null;
    }

    private IEnumerator Nod(AnnotationCard card)
    {
        Vector3 basePos = card.root.position;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
        {
            card.root.position = basePos + transform.up * Mathf.Sin(t * Mathf.PI) * 0.01f;
            yield return null;
        }
        card.root.position = basePos;
        card.motion = null;
    }

    private IEnumerator Wiggle(AnnotationCard card, float delay)
    {
        if (delay > 0f) yield return Wait(delay, ignorePause: true);
        Quaternion baseRot = card.root.rotation;
        Vector3 basePos = card.root.position;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
        {
            card.root.rotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t * Mathf.PI * 4f) * 6f * (1f - t), 0f);
            card.root.position = basePos + transform.up * Mathf.Sin(t * Mathf.PI) * 0.008f;
            yield return null;
        }
        card.root.SetPositionAndRotation(basePos, baseRot);
        card.motion = null;
    }

    private void Fly(AnnotationCard card, Vector3 position, Quaternion rotation, float scale, float seconds, float arc, float delay)
    {
        StartCard(card, DelayedFly(card, position, rotation, scale, seconds, arc, delay));
    }

    private void StartCard(AnnotationCard card, IEnumerator routine)
    {
        if (card == null) return;
        if (card.motion != null) StopCoroutine(card.motion);
        card.motion = StartCoroutine(routine);
    }

    private IEnumerator DelayedFly(AnnotationCard card, Vector3 position, Quaternion rotation, float scale, float seconds, float arc, float delay)
    {
        if (delay > 0f) yield return Wait(delay, ignorePause: true);
        yield return FlyRoutine(card, position, rotation, scale, seconds, arc);
        card.motion = null;
    }

    private IEnumerator FlyRoutine(AnnotationCard card, Vector3 position, Quaternion rotation, float scale, float seconds, float arc)
    {
        Transform t = card.root;
        Vector3 from = t.position;
        Quaternion fromRot = t.rotation;
        float fromScale = t.localScale.x;
        for (float u = 0f; u < 1f; u += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds))
        {
            float e = EaseOutCubic(u);
            t.position = Vector3.Lerp(from, position, e) + transform.up * (Mathf.Sin(e * Mathf.PI) * arc);
            t.rotation = Quaternion.Slerp(fromRot, rotation, e);
            t.localScale = Vector3.one * Mathf.Lerp(fromScale, scale, e);
            yield return null;
        }
        Snap(card, position, rotation, scale);
    }

    private static void Snap(AnnotationCard card, Vector3 position, Quaternion rotation, float scale)
    {
        card.root.SetPositionAndRotation(position, rotation);
        card.root.localScale = Vector3.one * scale;
    }

    /// <summary>The clip flies onto the EVIDENCE place and bites shut, or goes back open to its rest.</summary>
    private IEnumerator ClipTo(bool onPaper)
    {
        if (paper == null) yield break;
        Vector3 fromPos = clip.position;
        Quaternion fromRot = clip.rotation;
        Vector3 toPos;
        Quaternion toRot;
        if (onPaper) paper.ClipPose(out toPos, out toRot);
        else
        {
            toPos = transform.TransformPoint(clipRestPosition);
            toRot = transform.rotation * clipRestRotation;
        }
        clipJaw.localRotation = Quaternion.Euler(-35f, 0f, 0f);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.2f)
        {
            float e = EaseOutCubic(t);
            clip.position = Vector3.Lerp(fromPos, toPos, e) + transform.up * Mathf.Sin(e * Mathf.PI) * 0.05f;
            clip.rotation = Quaternion.Slerp(fromRot, toRot, e);
            yield return null;
        }
        clip.SetPositionAndRotation(toPos, toRot);
        if (!onPaper) yield break;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.08f)
        {
            clipJaw.localRotation = Quaternion.Euler(Mathf.Lerp(-35f, 0f, t * t), 0f, 0f);
            yield return null;
        }
        clipJaw.localRotation = Quaternion.identity;
        fx.Sparkle(toPos, transform.up, new Color32(206, 170, 96, 255), 8, 0.3f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
    }

    private void SnapClip(bool onPaper)
    {
        if (onPaper && paper != null)
        {
            paper.ClipPose(out Vector3 position, out Quaternion rotation);
            clip.SetPositionAndRotation(position, rotation);
            clipJaw.localRotation = Quaternion.identity;
        }
        else
        {
            clip.localPosition = clipRestPosition;
            clip.localRotation = clipRestRotation;
            clipJaw.localRotation = Quaternion.Euler(-35f, 0f, 0f);
        }
    }

    private IEnumerator PopIn(Transform target, float delay, float seconds)
    {
        if (target == null) yield break;
        target.localScale = Vector3.zero;
        if (delay > 0f) yield return Wait(delay, ignorePause: true);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
        {
            target.localScale = Vector3.one * EaseOutBack(t);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private IEnumerator Pop(Transform target, float peak, float seconds)
    {
        if (target == null) yield break;
        Vector3 rest = target.localScale;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
        {
            target.localScale = rest * (1f + (peak - 1f) * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        target.localScale = rest;
    }

    private void Release() => busy = Mathf.Max(0, busy - 1);

    // =========================================================
    // SKIP PRACTICE (Arthur's demonstration and the player's try)
    // =========================================================

    private GameObject skipCanvas;
    private CanvasGroup skipGroup;
    private bool skipping;

    /// <summary>Shown at the open records desk while the payslip demonstration or practice is on.</summary>
    private void UpdateSkipButton()
    {
        bool show = open && mode == Mode.Tutorial && !skipping && !SideQuestSplash.IsShowing && !HudRewardScreen.AnyOpen &&
                    Review != null && Review.TutorialActive;
        if (show && skipCanvas == null) BuildSkipButton();
        if (skipGroup == null) return;
        skipGroup.alpha = Mathf.MoveTowards(skipGroup.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 5f);
        skipGroup.blocksRaycasts = show && skipGroup.alpha > 0.5f;
        skipGroup.interactable = skipGroup.blocksRaycasts;
    }

    private void BuildSkipButton()
    {
        skipCanvas = new GameObject("Records Desk Skip Practice", typeof(RectTransform));
        var canvas = skipCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        var scaler = skipCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        skipCanvas.AddComponent<GraphicRaycaster>();
        skipGroup = skipCanvas.AddComponent<CanvasGroup>();
        skipGroup.alpha = 0f;

        RectTransform button = HudTheme.CreateRect("Skip Practice", skipCanvas.transform);
        button.anchorMin = button.anchorMax = button.pivot = new Vector2(0f, 1f);
        button.anchoredPosition = new Vector2(40f, -150f);
        button.sizeDelta = new Vector2(300f, 84f);
        Image back = HudTheme.AddImage(button, HudTheme.Rounded, new Color32(32, 40, 54, 235), 18f);
        back.raycastTarget = true;
        var outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        TextMeshProUGUI label = HudTheme.CreateLabel("Label", button, "SKIP PRACTICE  ▶▶", 28f, HudTheme.BrassSoft,
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 8f);
        label.raycastTarget = false;
        var b = button.gameObject.AddComponent<Button>();
        b.targetGraphic = back;
        b.onClick.AddListener(SkipPractice);
    }

    /// <summary>Skips Arthur's payslip demonstration and the practice try, straight to Rosa's four records.</summary>
    public void SkipPractice()
    {
        CaseReviewState review = Review;
        if (!open || mode != Mode.Tutorial || skipping || review == null || !review.TutorialActive) return;
        bool demoWasSeen = review.toolkitDemoSeen;
        if (!review.TrySkipTutorial()) return;
        skipping = true;
        if (flow != null) StopCoroutine(flow);
        flow = null;
        if (markerMotion != null) StopCoroutine(markerMotion);
        markerMotion = null;
        demoRunning = false;
        busy = 0;
        CancelInk(instant: true);
        CancelDrag();
        Deselect();
        ReturnMarker();
        if (paper != null) paper.ClearStrokes();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        if (!demoWasSeen) GameplayEvents.RaiseToolkitDemoFinished();
        GameplayEvents.RaiseAnnotationTutorialCompleted();
        Level2Say.Line("Practice skipped. Straight to Rosa's four records!", "wow", 4f);
        StartCoroutine(SkipToChallenge());
    }

    private IEnumerator SkipToChallenge()
    {
        if (options.entries != null && options.entries.Count > 0 && open) yield return BeginChallenge();
        skipping = false;
    }

    private IEnumerator Busy(IEnumerator routine)
    {
        busy++;
        try
        {
            yield return routine;
        }
        finally
        {
            Release();
        }
    }

    private AnnotationCard FindCard(AnnotationSlot kind, string optionId) =>
        cards.Find(c => c.kind == kind && c.optionId == optionId);

    private AnnotationCard FindPlacedCard(AnnotationSlot slot) =>
        cards.Find(c => c.kind == slot && c.placed);

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private static float EaseInOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }

    private static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseOutBounce(float t)
    {
        t = Mathf.Clamp01(t);
        const float n = 7.5625f, d = 2.75f;
        if (t < 1f / d) return n * t * t;
        if (t < 2f / d) return n * (t -= 1.5f / d) * t + 0.75f;
        if (t < 2.5f / d) return n * (t -= 2.25f / d) * t + 0.9375f;
        return n * (t -= 2.625f / d) * t + 0.984375f;
    }

    private void OnDisable()
    {
        if (active == this)
        {
            active = null;
            WorkstationUI.ExitBlocked = null;
            GuideCoachView.HintAtBottom = null;
        }
        if (challengeSplash != null && SideQuestSplash.IsShowing) challengeSplash.Cancel();
        challengeSplash = null;
        if (skipCanvas != null) Destroy(skipCanvas);
        skipCanvas = null;
        skipGroup = null;
        skipping = false;
        if (dragging != null) dragging.hit.enabled = true;
        StopAllCoroutines();
        if (marker != null) ReturnMarker();
        flow = pageMotion = lidMotion = markerMotion = null;
        foreach (AnnotationCard card in cards) card.motion = null;
        demoRunning = false;
        busy = 0;
        dragging = null;
        selected = null;
        inking = false;
        pressValid = false;
        open = false;
        wantOpen = false;
    }

    private void OnDestroy()
    {
        if (builder != null) builder.ReleaseMaterials();
    }
}
