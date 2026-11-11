using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Bea's plaza worktable (T04), under Level3Gameplay/BeaTable, built once at
/// runtime from supplied art (Level3SceneAssets): Palmov Island's park
/// "wooden table" fitted to working height, a kraft work mat, Bea herself
/// (Synty SM_Chr_Developer_Female_01 on the shared NpcHumanoid controller,
/// one NpcLife owner) and her papers.
///
/// PLACEMENT (measured, not guessed): beside Bea's familiar Level 1 bench
/// (town_life.json anchorPath), on rings around it, the first spot whose
/// table + standing room is free of solid colliders, nearest Bea's Level 1
/// spot; the table's near side faces the office doors (the player's way in)
/// and Bea stands behind it. The Console logs the chosen spot.
///
/// PAPERS: before the handover a closed manila envelope; after it (saved by
/// Level3Checkpointer.CommitBeaRecords) her nine papers come out of it into
/// one compact stack on the opened envelope (revised T04, 3 November 2026:
/// no fan of nine): the existing physical records (ClientRecordPaper: ID
/// card, background notes, income/payment/agreement sheets, plain working
/// papers), printed by Level3RecordRules from the case content, each acquired
/// once and never copied. B-WHT is never among them: Bea hands it over at the
/// office credit step (T08, Level3CertificatePaper).
///
/// STAGING (revised T04, Level3TableStaging + Level3PaperMachineModel): the
/// stack back left, a low reading stand front left with the card of her
/// current record (title, amount, purpose, issuer/period/reference), and on
/// the right the small paper machine and its review rack. All of it lies on
/// the mat inside clear edges; there is no worksheet board or calculator at
/// this table. T06 adds the batch checks and the summary.
///
/// FILING (T05, 4 November 2026): read -> half-feed -> select -> Why? (R6/R7)
/// -> lever -> next. Drag the stand's card onto the slot, or tap card then
/// slot; turn the dial or tap a label; tap Why? on the note card; pull the
/// lever past its threshold or tap it. Only the open batch is offered; new
/// records start neutral, recalled ones at their saved choices. The machine's
/// local state is Level3TableStaging.Machine; the lever is the ONE command:
/// Level3Checkpointer.CommitWorksheet(Level3WorksheetRules.File) writes the
/// treatment and reason together (candidate -> signed write -> publish), and
/// only then do the rollers, full feed, tab, totals and next card play,
/// identically for every choice. A failed save keeps the card half-fed with
/// its choices (retry). Review tabs recall a filed record (its saved filing
/// stands until a replacement saves; backing out feeds it back unchanged).
/// INSPECT ORIGINAL and COMPARE (rows that share a transaction) use the one
/// reader. Leaving, disable, settings, a conversation, focus loss or the
/// camera moving end drags undecided and settle motions; leaving also returns
/// an unfiled card to the stand. Nothing here grades.
///
/// BUILDUP (T04 follow-up, 4 November 2026; Level3TableStaging.Encounter):
/// before the handover her closed envelope lies on her side beside her
/// sketch. After the saved handover it slides into the player's reach
/// ("Open Bea's records"); tapping it lifts the flap and the stack slides
/// out with her logo-work record forward ("Inspect a record"), read standing
/// in the same reader under the existing HudWindowLock. When it is put back
/// she introduces her organizer (Help Bea / Later, Level3BeaInteractable);
/// Help Bea is the existing seat path, and the stand's card arrives once the
/// camera has settled. Exactly one table target is live at a time (the seat
/// box would cover the envelope). Nothing in the buildup saves, grades,
/// files or starts her clock; a restore with her records handed over skips
/// straight to the seat (no replayed presentation).
///
/// CHECK, CORRECT, COLLECT (revised T06, 5 November 2026): with every record
/// of the open batch filed, the stand shows the batch card and its one key,
/// CHECK BATCH. Only that key grades: Level3Checkpointer.CheckBatch saves first
/// (the batch's first outcomes frozen once), and only then do the tabs show
/// their marks (brass tick / graphite ?) with one short look-again line. A ?
/// tab recalls its record into the same half-feed/choose/pull loop (refiling is
/// neutral: its mark simply clears); CHECK AGAIN confirms. An all-right batch
/// is accepted (saved), its tabs gather into the rack and the next batch's
/// first record arrives; accepted records stay readable (originals, the total
/// windows' read-only list) but are never recalled. After the last batch the
/// actual Income & Expense Summary takes the stand. Opening it is a preview;
/// dragging it into the Bag or hotbar collects its separate inventory item.
/// The existing chapter/reward owner saves B-WORKSHEET, the item, 60 coins,
/// +2 Trust and the frozen result together before any completion feedback.
/// The same physical paper returns to the stand or opens beside the Case
/// Folder through the shared reader and document layers.
///
/// T08: once her worksheet is collected she walks to the office conference
/// room (Level3BeaOfficeVisit on her actor) and waits there.
///
/// PRACTICE PACKET (teaching pass, 6 November 2026; Level3SortingPractice):
/// before Bea's first record is filed, four fictional practice slips take the
/// stand first and go through the same feed / choose / Why? / lever. A pull
/// is answered at once: a wrong choice springs the lever back with one
/// "Look again" line on the note card (the slip stays loaded to try again);
/// a right one feeds in with a short why. No practice pull saves, files,
/// grades, rewards or starts her clock; after the fourth is right ONE saved
/// milestone (Level3Checkpointer.CommitSortingPractice) ends the packet and
/// her own first record arrives. Progress through the four slips lasts while
/// the scene is loaded (leaving and coming back resumes); a reload restarts it.
/// ARTHUR FIRST (follow-up, the Level 2 Mark the Money! pattern): before the
/// player's turn Arthur sorts the same four slips himself in his bubble while
/// the machine moves on its own (PracticeDemo: slip to the stand, half-feed,
/// dial, Why?, lever, full feed); player input is off and the clock is paused.
/// Then everything is back on the stand, he says "your turn", and the player
/// sorts the same slips, Arthur answering each pull (not printed on the stand).
/// Watching to the end saves Level3SortingPractice.DemoMilestone; leaving the
/// seat mid-demonstration stops it and it replays on the next visit.
///
/// READING: tap the table to sit in the existing desk view. The current
/// stand source is physically on top of the originals, followed by its compare
/// partner. Tap or drag an exposed original into the movable ClientRecordPaper
/// reader; a compare pair may be open together. Only a deliberate stack drop
/// returns a paper to the top. Originals stay on this table, outside inventory.
/// Settings, conversation and pause suspend input; leaving gathers the papers.
/// This class is their
/// IClientRecordHolder; it remembers the record read last (Bea's questions
/// start from it). Nothing here grades or awards; the lever's filing is its
/// only save (through the checkpointer).
/// </summary>
public sealed class Level3BeaTable : MonoBehaviour, IClientRecordHolder
{
    public const string ChildName = "BeaTable";
    public const string OfficeDoorsPath = "Environment/OfficeBuilding/Rooms/Reception/Doors";
    /// <summary>Bea's Level 1 spot beside the bench (Office_Level1 "Bea Spot"), used to rank free spots.</summary>
    public static readonly Vector3 LevelOneSpot = new Vector3(9.75f, -0.42f, 124.75f);
    public const float TableTop = 0.76f;
    /// <summary>The first-person Interactor's layer ("Interactable").</summary>
    public const int InteractLayer = 3;
    /// <summary>One paper's height in the stack (thick enough for an ID card's photo corners).</summary>
    private const float StackLayer = 0.0012f;
    private static readonly float[] Radii = { 1.8f, 2.4f, 3.0f, 3.6f, 4.2f };
    private const float AngleStep = 30f;
    private const float StackSeconds = 0.25f;
    private const float StackStagger = 0.012f;
    // T04 buildup motion (starting points; overlapped, never waited on by a command).
    private const float PresentSeconds = 0.35f;
    private const float FlapSeconds = 0.25f;
    private const float FlapOpenDegrees = -150f;
    private const float StackDelay = 0.08f;
    private const float FeatureYaw = -6f;
    /// <summary>Her organizer offer follows the returned record after this pause, if nothing else holds the player within the window.</summary>
    private const float OfferDelay = 0.35f;
    private const float OfferWindow = 4f;
    private const float OfferDistance = 4f;
    /// <summary>The one look of uncertainty, the first time the player comes this near before the handover.</summary>
    private const float NoticeDistance = 6f;
    public const string InspectPrompt = "Inspect a record";
    /// <summary>The single original's reading pose: centred, at this camera depth and share of the view.</summary>
    private const float ReadDepth = 0.32f;
    private const float ReadSheetHeight = 0.74f;
    private const float ReadCardHeight = 0.46f;
    private const float ReadWidth = 0.7f;

    public static Level3BeaTable Instance { get; private set; }

    public bool Built { get; private set; }
    /// <summary>The player is sitting at the table (papers take touches).</summary>
    public bool Seated { get; private set; }
    public bool CanTouchPapers => !introPending && !demoRunning && restacking == null && (Seated && FreeToTouch() || ReadingStanding &&
        CameraController.Instance?.IsTransitioning != true && !InGameSettings.IsOpen && !DialogueUI.IsAnyDialogueActive) && !HudRewardScreen.AnyOpen;
    public Level3BeaInteractable Bea { get; private set; }
    public Level3ClientContent Content { get; private set; }
    public Transform Viewpoint { get; private set; }
    /// <summary>The source id of the record read last (null before any).</summary>
    public string LastReadSource { get; private set; }
    /// <summary>T08: her walk to the office conference room.</summary>
    public Level3BeaOfficeVisit Visit { get; private set; }
    /// <summary>Revised T04/T05: the stand, the paper machine and its rack.</summary>
    public Level3PaperMachineModel Machine { get; private set; }
    /// <summary>T05: the machine's local state (never saved).</summary>
    public Level3TableStaging.Machine Flow => flow;
    /// <summary>T05 guide: seated with every record of the open batch filed (Check batch is T06's).</summary>
    public bool BatchFiledHere
    {
        get
        {
            Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
            string batch = Level3WorksheetRules.OpenBatch(chapter);
            return Seated && encounter.CardShown && flow.Step == Level3TableStaging.MachineStep.Empty && batch != null &&
                   Level3WorksheetRules.BatchFiled(chapter, batch);
        }
    }
    /// <summary>Revised T06 guide: seated, the open batch's last check marked a decision "look again" that is still as checked.</summary>
    public bool LookAgainHere
    {
        get
        {
            Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
            string batch = Level3WorksheetRules.OpenBatch(chapter);
            return Seated && batch != null && Level3WorksheetCheck.LookAgainIn(chapter, batch).Count > 0;
        }
    }
    /// <summary>The final summary is available on its stand, in its own window or in inventory.</summary>
    public bool SummaryOut => SummaryAvailable;
    public bool SummaryAvailable => summaryDocument != null && !reviewing &&
        (Level3ChapterRules.PhaseOf(Level3Checkpointer.Active?.State) == Level3Phase.WorksheetReady || Level3WorksheetCheck.Collected(Level3Checkpointer.Active?.State));
    public Level3TableStaging.Layout Layout { get; private set; }
    /// <summary>The worksheet row on the stand card or in the machine (null when none).</summary>
    public Level3WorksheetRow ActiveRow { get; private set; }
    /// <summary>The original being read (one at a time), or null.</summary>
    public string OpenSource => stack != null ? stack.Out : null;
    /// <summary>T04 buildup: the next step of Bea's first encounter (this session; the saved chapter decides Waiting or Ready).</summary>
    public Level3TableStaging.EncounterStep Step => encounter.Step;
    /// <summary>Her introduction was heard to the end (a failed handover save retries with its last line).</summary>
    public bool IntroHeard => encounter.IntroHeard;
    /// <summary>Her organizer offer was made (or a restore skipped it): guide flag beaIntroDone.</summary>
    public bool IntroDone => encounter.Done;
    /// <summary>The record standing in the reader during the buildup is the featured one (one hold, released when it is back).</summary>
    public bool ReadingStanding { get; private set; }

    /// <summary>Bea's talk offers Help Bea first: her worksheet's turn at the table, the offer made, the player standing beside it.</summary>
    public bool CanOfferHelp
    {
        get
        {
            Level3Phase phase = Level3ChapterRules.PhaseOf(Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null);
            return Built && papersShown && encounter.Done && !Seated && !ReadingStanding &&
                   phase >= Level3Phase.WorksheetDraft && phase < Level3Phase.BeaComputation && (Visit == null || !Visit.OnTheWay);
        }
    }

    private Level3DeskProps props;
    private Transform tableRoot;
    private Transform papersRoot;
    private Transform workspace;
    private Transform envelope;
    private Transform envelopeFlap;
    private Collider tableCollider;
    private float topY;
    private Vector2 matSize;
    private readonly List<ClientRecordPaper> papers = new List<ClientRecordPaper>();
    private readonly List<ClientRecordPaper> outOrder = new List<ClientRecordPaper>();
    private readonly Dictionary<string, ClientRecordPaper> bySource = new Dictionary<string, ClientRecordPaper>();
    private readonly Dictionary<string, Transform> slotBySource = new Dictionary<string, Transform>();
    private Level3TableStaging.Stack stack;
    private string stackedRow;
    private Coroutine restacking;
    private bool originalHintShown;
    private bool paperCanvasBound;
    private List<Level3WorksheetRow> rows = new List<Level3WorksheetRow>();
    private bool papersShown;
    private bool touchOn;
    private ClientRecordPaper dragStarting;
    private Level3Phase shownPhase = (Level3Phase)(-1);
    private int shownRevision = -1;
    // T04 buildup: the encounter's order, its two step targets and the envelope's two resting poses (table space).
    private readonly Level3TableStaging.Encounter encounter = new Level3TableStaging.Encounter();
    private readonly Dictionary<string, Quaternion> slotTurn = new Dictionary<string, Quaternion>();
    private Level3BeaTableTarget envelopeTarget;
    private Level3BeaTableTarget recordTarget;
    private Vector3 envelopeRestPosition, envelopeBeaPosition;
    private Quaternion envelopeRestRotation, envelopeBeaRotation;
    private Coroutine presenting;
    private Coroutine revealing;
    private Level3TableStaging.EncounterStep shownStep = (Level3TableStaging.EncounterStep)(-1);
    private float offerAt = -1f;
    private bool noticed;
    /// <summary>Help Bea: the stand's card arrives once the camera has settled at the seat (not during the move).</summary>
    private bool presentCard;
    // T05: the paper machine.
    private readonly Level3TableStaging.Machine flow = new Level3TableStaging.Machine();
    /// <summary>The row whose tab arrives once its feed finishes (a short settle; null otherwise).</summary>
    private string settleTab;
    /// <summary>COMPARE: the two originals read side by side (null when not comparing).</summary>
    private string compareFirst, compareSecond;
    // Revised T06: check, review, totals list and summary (local presentation; the saved chapter decides).
    private const float KeyRepeatSeconds = 0.3f;
    private const float AcceptHoldSeconds = 0.3f;
    private const float RewardDelaySeconds = 0.5f;
    private int shownChecks = -1;
    private float keyPressedAt = -1f;
    /// <summary>An accepted batch's marks and gather are playing (the next batch waits for it).</summary>
    private bool reviewing;
    private Coroutine acceptRoutine;
    /// <summary>One plain sentence for the batch card after an unsaved key press (unchanged, incomplete, failed save).</summary>
    private string batchNote;
    /// <summary>Teaching pass: practice slips answered right on this visit (the next slip's index); never saved.</summary>
    private int practiceDone;
    /// <summary>Retained for the note card; Arthur now says a practice slip's hint, so it stays null.</summary>
    private string practiceHint;
    /// <summary>Follow-up: Arthur's demonstration of the practice slips (presentation only, never saved work).</summary>
    private Coroutine practiceDemo;
    private bool demoRunning;
    /// <summary>Arthur is demonstrating the practice slips: the player watches (no input, clock paused).</summary>
    public bool DemoRunning => demoRunning;
    /// <summary>The read-only totals list on the stand: 0 Income, 1 Deductions, -1 none.</summary>
    private int ledger = -1;
    /// <summary>This session's look-again count per decision: the second time, the idea replaces the pointer (hints stay free, unsaved).</summary>
    private readonly Dictionary<string, int> lookAgainSeen = new Dictionary<string, int>();
    private bool marksPop;
    // The summary as the existing reader's paper (preview only) and the saved collection's presentation.
    private Level3SummaryDocument summaryDocument;
    private bool collecting;
    private SideQuestSplash introSplash;
    private Coroutine introRoutine;
    private bool introPending;
    public bool IntroPending => introPending;
    private bool awardShown;
    private float rewardAt = -1f;
    private Level3WorksheetGrants collectGrants;
    private Vector3 collectOrigin;

    private void Awake() => Instance = this;

    private void OnEnable() => CameraController.WorkstationExited += HandleLeftSeat;

    private void OnDisable()
    {
        CameraController.WorkstationExited -= HandleLeftSeat;
        CancelIntro();
        GatherAll();
        Seated = false;
        SetTouch(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (workspace != null) Destroy(workspace.gameObject);
        props?.Destroy();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) CancelDrags();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) CancelDrags();
    }

    private IEnumerator Start()
    {
        float until = Time.unscaledTime + 10f;
        while (Time.unscaledTime < until &&
               (Level3Checkpointer.Active == null || !Level3Checkpointer.Active.IsReady || CaseManager.Instance?.CurrentDefinition == null))
            yield return null;
        if (Level3Checkpointer.Active == null || CaseManager.Instance?.CurrentDefinition == null)
        {
            Debug.LogWarning("[Level3] Bea's table was not built: the chapter or Bea's case did not load (see the Console).");
            yield break;
        }
        Build();
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void Build()
    {
        Content = LoadContent();
        Level3SceneAssets assets = Level3SceneAssets.Load();
        props = new Level3DeskProps(gameObject.layer);
        Scene scene = gameObject.scene;
        Transform anchor = PlayerPlacement.Find(scene, Content.anchorPath);
        Vector3 center = anchor != null ? anchor.position : LevelOneSpot;
        Transform doors = PlayerPlacement.Find(scene, OfficeDoorsPath);

        tableRoot = new GameObject("Worktable").transform;
        tableRoot.SetParent(transform, false);
        Bounds footprint = BuildTable(assets != null ? assets.worktablePrefab : null);

        Level3BeaPlacement placement = Level3BeaPlacement.Find(scene);
        Vector3 spot, front;
        bool free;
        if (placement != null)
        {
            // One scene handle moves the entire outdoor setup; its authored feet and yaw are authoritative.
            spot = placement.transform.position;
            front = -(Quaternion.Euler(0f, placement.transform.eulerAngles.y, 0f) * Vector3.forward);
            free = true;
        }
        else spot = ChooseSpot(center, footprint, doors, out front, out free);
        tableRoot.SetPositionAndRotation(spot, Quaternion.LookRotation(-front, Vector3.up));
        topY = spot.y + TableTop;
        Debug.Log($"[Level3] Bea's worktable at {spot} facing {front} ({(free ? "free spot" : "no free spot found; nearest ring spot used")}).");

        BuildMatAndEnvelope(footprint);
        BuildViewpoint();
        BuildStaging();
        BuildSketch();
        BuildSeatTarget(footprint);
        BuildEncounterTargets();
        BuildBea(assets, spot, front, footprint, placement != null);
        Built = true;
        SyncPapers();
        ApplyEncounterPose();
    }

    private static Level3ClientContent LoadContent()
    {
        TextAsset file = Resources.Load<TextAsset>(Level3TownContent.ResourcePath);
        if (file != null)
        {
            try
            {
                Level3TownFile parsed = JsonUtility.FromJson<Level3TownFile>(file.text);
                List<string> problems = Level3TownContent.Validate(parsed);
                if (problems.Count == 0) return parsed.bea;
                Debug.LogWarning($"[Level3] Resources/{Level3TownContent.ResourcePath}.json: {string.Join("; ", problems)}. Using the built-in Bea.");
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Level3] Resources/{Level3TownContent.ResourcePath}.json is not valid JSON ({error.Message}). Using the built-in Bea.");
            }
        }
        return BuiltInBea();
    }

    /// <summary>Mirrors town_life.json's bea (CONTENT lines).</summary>
    private static Level3ClientContent BuiltInBea()
    {
        var bea = new Level3ClientContent();
        bea.beforeAssignment.Add("Hi! I have a computation appointment with your office. Check in with your receptionist first.");
        bea.duringPractice.Add("Take your time with the calculator. I'll be right here with my records.");
        bea.introduction.Add(new Level3TownLine { text = "Hi again, or properly hello! I'm Beatriz, Bea. I design logos and menus for shops around town.", emote = "sparkles" });
        bea.introduction.Add(new Level3TownLine { text = "I'm saving for a studio. Could you help me check my records?" });
        bea.introduction.Add(new Level3TownLine { speaker = "Player", text = "Let's look at what each record supports." });
        bea.introduction.Add(new Level3TownLine { text = "I kept every receipt. Can I count them all? I'm unsure what belongs as business costs.", emote = "thinking" });
        bea.organizerIntro.Add(new Level3TownLine { text = "See my little organizer? We can use it to prepare a clear summary.", emote = "idea" });
        bea.inquiries.Add(new Level3Inquiry { id = "purpose", question = "What was this purchase for?", answer = "My birthday dinner and decorations. The studio wasn't involved.", contextSources = new List<string> { "B-PERSONAL", "B-DRAFT" } });
        bea.inquiries.Add(new Level3Inquiry { id = "invoice", question = "Does this invoice belong to the annual total?", answer = "Yes, that supplier bill is in the printing summary.", contextSources = new List<string> { "B-PRINT", "B-EXP" } });
        bea.inquiries.Add(new Level3Inquiry { id = "period", question = "Show me the period these records cover.", answer = "These are the records for the whole year shown on the sheets." });
        bea.inquiries.Add(new Level3Inquiry { id = "work", question = "What are you working on?", answer = "Sketches for a new shop sign. Drawing is the easy part; receipts, not so much." });
        bea.certificateHandover.Add(new Level3TownLine { text = "Oh! Before you finish, one more paper. Harbor Menu withheld some tax when they paid me for their menus." });
        bea.certificateHandover.Add(new Level3TownLine { text = "They gave me this certificate for it. That payment is already in my menu fees, by the way." });
        bea.creditAnswers.Add(new Level3CreditAnswer { id = Level3CertificateRules.AnswerCredit, text = "It reduces the remaining tax when supported by the certificate.",
            reply = "So it comes off the tax at the very end, as long as the certificate backs it. I'll leave the numbers to you." });
        bea.creditAnswers.Add(new Level3CreditAnswer { id = Level3CertificateRules.AnswerIncome, text = "It reduces the income amount before the tax calculation.",
            reply = "Hmm, I'm not sure. Let's see what your Tax Code Book says about it." });
        bea.creditAnswers.Add(new Level3CreditAnswer { id = Level3CertificateRules.AnswerInspect, text = "Let's inspect the certificate together.", reply = "Sure, here it is again." });
        return bea;
    }

    /// <summary>The supplied table fitted to TableTop high (base on y = 0), or a plain wooden stand-in; returns its local footprint.</summary>
    private Bounds BuildTable(GameObject prefab)
    {
        if (prefab != null)
        {
            GameObject table = Instantiate(prefab, tableRoot, false);
            table.name = "Table (" + prefab.name + ")";
            if (TryRendererBounds(table, tableRoot, out Bounds raw) && raw.size.y > 0.05f)
            {
                table.transform.localScale *= TableTop / raw.size.y;
                TryRendererBounds(table, tableRoot, out Bounds fitted);
                table.transform.localPosition -= new Vector3(fitted.center.x, fitted.min.y, fitted.center.z);
                // Its own colliders (a box, if any) keep it solid; one table collider is the seat tap target below.
                foreach (Collider c in table.GetComponentsInChildren<Collider>(true)) c.gameObject.layer = 0;
                TryRendererBounds(table, tableRoot, out fitted);
                return fitted;
            }
            Destroy(table);
            Debug.LogWarning("[Level3] The worktable prefab has no measurable renderer; a plain table stands in.");
        }
        else Debug.LogWarning($"[Level3] Resources/{Level3SceneAssets.ResourcePath} has no worktable prefab; a plain table stands in.");
        const float w = 1.3f, d = 0.8f;
        props.Box("Top", tableRoot, new Vector3(0f, TableTop - 0.025f, 0f), new Vector3(w, 0.05f, d), new Color32(150, 104, 66, 255));
        foreach (float x in new[] { -w * 0.44f, w * 0.44f })
            foreach (float z in new[] { -d * 0.4f, d * 0.4f })
                props.Box("Leg", tableRoot, new Vector3(x, (TableTop - 0.05f) * 0.5f, z), new Vector3(0.06f, TableTop - 0.05f, 0.06f), new Color32(92, 62, 40, 255));
        return new Bounds(new Vector3(0f, TableTop * 0.5f, 0f), new Vector3(w, TableTop, d));
    }

    private static bool TryRendererBounds(GameObject model, Transform space, out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Vector3 c = mesh.center, e = mesh.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                Vector3 local = space.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!any) { bounds = new Bounds(local, Vector3.zero); any = true; }
                else bounds.Encapsulate(local);
            }
        }
        return any;
    }

    /// <summary>The first free ring spot around the bench, nearest Bea's Level 1 spot; front faces the office doors.</summary>
    private Vector3 ChooseSpot(Vector3 center, Bounds footprint, Transform doors, out Vector3 front, out bool free)
    {
        var candidates = new List<Vector3>();
        foreach (float radius in Radii)
            for (float angle = 0f; angle < 360f; angle += AngleStep)
                candidates.Add(center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius);
        candidates.Sort((a, b) => (a - LevelOneSpot).sqrMagnitude.CompareTo((b - LevelOneSpot).sqrMagnitude));

        Vector3 FrontFor(Vector3 p)
        {
            Vector3 toDoors = doors != null ? doors.position - p : Vector3.back;
            toDoors.y = 0f;
            return toDoors.sqrMagnitude > 0.01f ? toDoors.normalized : Vector3.back;
        }

        foreach (Vector3 candidate in candidates)
        {
            if (!Ground(candidate, center.y, out float y)) continue;
            Vector3 p = new Vector3(candidate.x, y, candidate.z);
            Vector3 f = FrontFor(p);
            // Table, Bea's standing room behind it and the player's room in front, above the ground.
            var halfExtents = new Vector3(footprint.extents.x + 0.25f, 0.5f, footprint.extents.z + 0.85f);
            if (Physics.CheckBox(p + Vector3.up * 0.65f, halfExtents, Quaternion.LookRotation(-f, Vector3.up), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                continue;
            front = f;
            free = true;
            return p;
        }
        Vector3 fallback = candidates.Count > 0 ? candidates[0] : center;
        fallback.y = Ground(fallback, center.y, out float fy) ? fy : center.y;
        front = FrontFor(fallback);
        free = false;
        return fallback;
    }

    /// <summary>The walkable ground under a point (highest upward face near plaza height).</summary>
    private static bool Ground(Vector3 point, float nearY, out float y)
    {
        y = nearY;
        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(point + Vector3.up * 2.5f, Vector3.down, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (hit.normal.y > 0.8f && Mathf.Abs(hit.point.y - nearY) < 0.6f && hit.point.y > best) best = hit.point.y;
        if (float.IsNegativeInfinity(best)) return false;
        y = best;
        return true;
    }

    private void BuildMatAndEnvelope(Bounds footprint)
    {
        // A kraft work mat: the flat working surface the stack, stand and machine stand on.
        matSize = new Vector2(Mathf.Min(1.2f, footprint.size.x * 0.94f), Mathf.Min(0.72f, footprint.size.z * 0.9f));
        props.Box("Work Mat", tableRoot, new Vector3(0f, TableTop + 0.0015f, 0f), new Vector3(matSize.x, 0.003f, matSize.y), new Color32(196, 164, 118, 255));
        props.Box("Mat Edge", tableRoot, new Vector3(0f, TableTop + 0.0012f, 0f), new Vector3(matSize.x + 0.012f, 0.0024f, matSize.y + 0.012f), new Color32(120, 92, 60, 255));
        Layout = Level3TableStaging.Compute(matSize, Level3TableStaging.SeatOf(matSize, footprint.extents.z));
        Level3TableStaging.Layout layout = Layout;

        // The envelope (back left) is also the stack's tray once it is open.
        float s = layout.Scale;
        envelope = new GameObject("Envelope").transform;
        envelope.SetParent(tableRoot, false);
        envelope.localPosition = new Vector3(layout.Envelope.center.x, TableTop + 0.003f + 0.008f * s, layout.Envelope.center.y);
        envelope.localRotation = Quaternion.Euler(0f, layout.EnvelopeYaw, 0f);
        envelope.localScale = Vector3.one * s;
        var manila = new Color32(222, 186, 120, 255);
        props.Box("Body", envelope, Vector3.zero, new Vector3(Level3TableStaging.EnvelopeWidth, 0.016f, Level3TableStaging.EnvelopeDepth), manila);
        envelopeFlap = new GameObject("Flap Hinge").transform;
        envelopeFlap.SetParent(envelope, false);
        envelopeFlap.localPosition = new Vector3(0f, 0.009f, Level3TableStaging.EnvelopeDepth * 0.5f);
        props.Box("Flap", envelopeFlap, new Vector3(0f, 0f, -0.05f), new Vector3(Level3TableStaging.EnvelopeWidth, 0.004f, 0.1f), manila);
        // Printed toward the player (the envelope reads from the table's near side).
        props.Print("Label", envelope, new Vector3(0f, 0.0085f, -0.03f), 0.24f, 0.07f, Content.envelopeLabel, new Color32(40, 34, 30, 255), bold: true, wrap: true);

        // The stack lies on the envelope's top (one paper above another; nothing is ever copied).
        papersRoot = new GameObject("Papers").transform;
        papersRoot.SetParent(tableRoot, false);
        workspace = new GameObject("Bea Papers Out").transform;
        SceneManager.MoveGameObjectToScene(workspace.gameObject, gameObject.scene);

        // T04 buildup: its place on the mat (staging) and, before she presents it, on her side of the table.
        envelopeRestPosition = envelope.localPosition;
        envelopeRestRotation = envelope.localRotation;
        Vector2 withBea = Level3TableStaging.EnvelopeWithBea(layout, footprint.max.z, out float beaYaw);
        envelopeBeaPosition = new Vector3(withBea.x, envelopeRestPosition.y, withBea.y);
        envelopeBeaRotation = Quaternion.Euler(0f, beaYaw, 0f);
        PlaceEnvelope(envelopeBeaPosition, envelopeBeaRotation);
    }

    /// <summary>The envelope and the stack lying on it move as one (table space).</summary>
    private void PlaceEnvelope(Vector3 position, Quaternion rotation)
    {
        envelope.localPosition = position;
        envelope.localRotation = rotation;
        papersRoot.localPosition = position + Vector3.up * (0.0085f * Layout.Scale);
        papersRoot.localRotation = rotation;
    }

    /// <summary>
    /// T04 buildup: her work beside her records, on her side of the mat behind the machine: a logo draft and a
    /// pencil from the existing Level3DeskProps blocks (no new asset, no collider). Skipped when the mat has no room.
    /// </summary>
    private void BuildSketch()
    {
        Rect area = Level3TableStaging.SketchArea(Layout);
        if (area.width <= 0f) return;
        float s = Layout.Scale;
        var sketch = new GameObject("Bea Sketch").transform;
        sketch.gameObject.layer = gameObject.layer;
        sketch.SetParent(tableRoot, false);
        // Turned toward Bea: it is her drawing, read from her side.
        sketch.localPosition = new Vector3(area.center.x, TableTop + 0.003f, area.center.y);
        sketch.localRotation = Quaternion.Euler(0f, 172f, 0f);
        sketch.localScale = Vector3.one * s;
        props.Box("Sheet", sketch, new Vector3(0f, 0.0008f, 0f), new Vector3(Level3TableStaging.SketchWidth, 0.0016f, Level3TableStaging.SketchDepth), Level3DeskProps.Paper);
        props.Shape(PrimitiveType.Cylinder, "Logo Mark", sketch, new Vector3(-0.03f, 0.0019f, 0.008f), new Vector3(0.042f, 0.0004f, 0.042f), Level3DeskProps.Terracotta);
        props.Shape(PrimitiveType.Cylinder, "Logo Centre", sketch, new Vector3(-0.03f, 0.0024f, 0.008f), new Vector3(0.024f, 0.0004f, 0.024f), Level3DeskProps.Cream);
        props.Box("Lettering", sketch, new Vector3(0.025f, 0.0019f, 0.016f), new Vector3(0.045f, 0.0004f, 0.005f), Level3DeskProps.InkMuted);
        props.Box("Lettering 2", sketch, new Vector3(0.02f, 0.0019f, 0.002f), new Vector3(0.035f, 0.0004f, 0.004f), Level3DeskProps.InkMuted);
        GameObject pencil = props.Roll("Pencil", sketch, new Vector3(0.012f, 0.005f, -0.028f), 0.085f, 0.007f, Level3DeskProps.Brass);
        pencil.transform.localRotation = Quaternion.Euler(0f, 18f, 90f);
    }

    private void BuildViewpoint()
    {
        // Seated on the near side, looking down at the arrangement (the office desk view's angle, a little closer than the old fan).
        var seat = new GameObject("TableViewpoint").transform;
        seat.SetParent(tableRoot, false);
        Vector3 focus = new Vector3(0f, TableTop, -matSize.y * 0.08f);
        // Bring the anchored tabletop view closer so the stand, stack and passive machine read larger.
        seat.localPosition = Vector3.Lerp(focus, Layout.Seat + Vector3.up * TableTop, 0.76f);
        Vector3 look = focus - seat.localPosition;
        seat.localRotation = Quaternion.LookRotation(look, Vector3.up);
        Viewpoint = seat;
    }

    /// <summary>Revised T04: the reading stand (left) and the passive paper machine with its rack (right), on the mat.</summary>
    private void BuildStaging()
    {
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        rows = Level3WorksheetRules.RowsOf(definition);
        Machine = Level3PaperMachineModel.Build(props, tableRoot, Layout, TableTop + 0.003f, gameObject.layer, OnMachineInput);
        Debug.Log($"[Level3] Bea's table staging: mat {matSize.x:0.00} x {matSize.y:0.00} m, scale {Layout.Scale:0.00}; stack, stand and machine inside the mat.");
    }

    private void BuildSeatTarget(Bounds footprint)
    {
        var target = new GameObject("Sit At Table");
        target.layer = InteractLayer;
        target.transform.SetParent(tableRoot, false);
        var box = target.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, TableTop * 0.5f + 0.05f, 0f);
        box.size = new Vector3(footprint.size.x, TableTop + 0.1f, footprint.size.z);
        tableCollider = box;
        target.AddComponent<Level3BeaTableSeat>().Init(this, tableRoot);
    }

    /// <summary>
    /// T04 buildup: "Open Bea's records" on the envelope and "Inspect a record" on the record she brings forward,
    /// on the first-person Interactable layer. Both start off; the encounter switches on only its next step.
    /// </summary>
    private void BuildEncounterTargets()
    {
        envelope.gameObject.layer = InteractLayer;
        var box = envelope.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.006f, 0f);
        box.size = new Vector3(Level3TableStaging.EnvelopeWidth + 0.04f, 0.04f, Level3TableStaging.EnvelopeDepth + 0.04f);
        envelopeTarget = envelope.gameObject.AddComponent<Level3BeaTableTarget>();
        envelopeTarget.Init(box, "Open " + Content.speakerName + "'s records", OpenEnvelope);

        var record = new GameObject("Inspect Record");
        record.layer = InteractLayer;
        record.transform.SetParent(papersRoot, false);
        recordTarget = record.AddComponent<Level3BeaTableTarget>();
        recordTarget.Init(record.AddComponent<BoxCollider>(), InspectPrompt, InspectFeatured);
    }

    private void BuildBea(Level3SceneAssets assets, Vector3 spot, Vector3 front, Bounds footprint, bool authored)
    {
        var holder = new GameObject(Content.speakerName);
        holder.layer = InteractLayer;
        holder.transform.SetParent(transform, false);
        Vector3 at = spot - front * (footprint.extents.z + 0.38f);
        at.y = !authored && Ground(at, spot.y, out float y) ? y : spot.y;
        holder.transform.SetPositionAndRotation(at, Quaternion.LookRotation(front, Vector3.up));

        GameObject prefab = assets != null ? assets.beaPrefab : null;
        if (prefab == null)
        {
            Debug.LogWarning($"[Level3] Resources/{Level3SceneAssets.ResourcePath} has no Bea model; she is not shown (her table still works).");
        }
        else
        {
            GameObject model = Instantiate(prefab, holder.transform, false);
            model.name = "BeaModel";
            FitHeight(model, at.y, Content.height);
            Animator animator = model.GetComponentInChildren<Animator>();
            if (animator != null && assets.npcController != null)
            {
                animator.runtimeAnimatorController = assets.npcController;
                animator.applyRootMotion = false;
                // One owner for her body (as Level 2's Rosa): NpcLife stands her at the table and turns her to the
                // player in a conversation; the driver adds idle variation and speaking/listening motion.
                holder.SetActive(false);
                holder.AddComponent<NpcAnimationDriver>().ConfigureStates(Content.idle, Level2Town.WalkState, Level2Town.StrollState);
                holder.AddComponent<NpcLife>().ConfigureRuntime(NpcLife.Activity.Stand, null, null, null, new Vector2(3f, 6f), false, true);
                holder.SetActive(true);
            }
            else Debug.LogWarning("[Level3] Bea's model has no Animator or the shared controller is missing; she holds her rest pose.");
        }
        var capsule = holder.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.85f, 0f);
        capsule.height = 1.7f;
        capsule.radius = 0.32f;
        Bea = holder.AddComponent<Level3BeaInteractable>();
        Bea.Init(this);
        // T08: her walk to the office once her worksheet is collected (the same actor, the same NpcLife).
        Visit = holder.AddComponent<Level3BeaOfficeVisit>();
        Visit.Init(this);
    }

    private static void FitHeight(GameObject model, float groundY, float height)
    {
        if (!TryWorldBounds(model, out Bounds b) || b.size.y < 0.01f) return;
        model.transform.localScale *= height / b.size.y;
        TryWorldBounds(model, out b);
        model.transform.position += Vector3.up * (groundY - b.min.y);
    }

    private static bool TryWorldBounds(GameObject model, out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    // =========================================================
    // PAPERS: ONE COMPACT STACK, ONE ORIGINAL OUT
    // =========================================================

    /// <summary>
    /// Quietly shows the acquired papers (a restore, a reload, a repeated handover): the envelope open on the mat,
    /// the stack on it, the encounter Ready. No presentation, reveal or acquisition replays.
    /// </summary>
    public void SyncPapers()
    {
        if (!Built || !BuildPapers(featuredFirst: null)) return;
        encounter.Sync(handedOver: true);
        ApplyEncounterPose();
        RefreshStand(animate: false);
    }

    /// <summary>
    /// T04 buildup, right after this session's SAVED handover (Level3BeaInteractable.HandOver): the papers are
    /// built inside the closed envelope and she slides it into the player's reach. Nothing more is acquired here.
    /// </summary>
    public void Present()
    {
        if (!Built) return;
        List<string> onTable = OnTable();
        string featured = Level3TableStaging.FeaturedSource(rows, onTable);
        if (!BuildPapers(featured) || !encounter.Present(featured))
        {
            // Not this session's first presentation (or nothing to show): the quiet restore's view.
            SyncPapers();
            return;
        }
        ApplyEncounterPose();
        PlaceEnvelope(envelopeBeaPosition, envelopeBeaRotation);
        presenting = StartCoroutine(PresentMotion());
    }

    /// <summary>"Open Bea's records": the flap lifts, the stack slides out, her logo-work record comes forward. Reveals only; acquires nothing.</summary>
    private void OpenEnvelope()
    {
        if (presenting != null || !encounter.Open()) return;
        MobileInputGuard.SuppressWorldTapsThisFrame();
        envelopeTarget.Active = false;
        papersRoot.gameObject.SetActive(true);
        revealing = StartCoroutine(Reveal());
    }

    /// <summary>"Inspect a record": the featured original rises into the existing reader while the player stands. One hold on walking/looking until it is back.</summary>
    private void InspectFeatured()
    {
        if (!encounter.FeaturedForward || revealing != null || ReadingStanding || Seated) return;
        if (!bySource.TryGetValue(encounter.Featured, out ClientRecordPaper paper) || paper == null || !paper.IsAttached) return;
        HudWindowLock.Acquire(this);
        if (!HudWindowLock.IsHeld)
        {
            // Something else holds the player's controls (a conversation): nothing opens.
            HudWindowLock.Release(this);
            return;
        }
        MobileInputGuard.SuppressWorldTapsThisFrame();
        ReadingStanding = true;
        recordTarget.Active = false;
        SetPaperTouch(paper, true);
        paper.SetEventCamera(ViewCamera);
        paper.TakeOut();
    }

    /// <summary>The standing read is over (put back, gathered, disabled): its touches off, the hold released.</summary>
    private void EndStandingRead()
    {
        if (!ReadingStanding) return;
        ReadingStanding = false;
        if (encounter.Featured != null && bySource.TryGetValue(encounter.Featured, out ClientRecordPaper paper) && paper != null)
            SetPaperTouch(paper, Seated && touchOn);
        HudWindowLock.Release(this);
    }

    private static void SetPaperTouch(ClientRecordPaper paper, bool on)
    {
        foreach (GraphicRaycaster raycaster in paper.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = on;
    }

    /// <summary>Her table holds what she brought there; the certificate came later, at the office (T08).</summary>
    private static List<string> OnTable()
    {
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        List<string> acquired = chapter?.Case(Level3ChapterRules.BeaCaseId)?.acquiredSourceIds;
        return Level3TableStaging.TableSources(acquired);
    }

    /// <summary>Builds the acquired originals once (the featured one on top when she presents them). False before the handover.</summary>
    private bool BuildPapers(string featuredFirst)
    {
        if (papersShown) return true;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (definition == null || definition.caseId != Level3ChapterRules.BeaCaseId) return false;
        List<string> onTable = OnTable();
        if (onTable.Count == 0) return false;
        List<ClientRecord> records = Level3RecordRules.Build(definition, onTable);
        if (records.Count == 0) return false;
        papersShown = true;
        // Built active, as before the buildup; ApplyEncounterPose then hides them inside a closed envelope.
        if (!papersRoot.gameObject.activeSelf) papersRoot.gameObject.SetActive(true);
        StackUp(records, featuredFirst);
        SetPapersInteractive(Seated);
        return true;
    }

    /// <summary>The envelope, its flap and the stack as the encounter stands (also how every interrupted motion settles).</summary>
    private void ApplyEncounterPose()
    {
        if (envelope == null) return;
        bool waiting = encounter.Step == Level3TableStaging.EncounterStep.Waiting;
        PlaceEnvelope(waiting ? envelopeBeaPosition : envelopeRestPosition, waiting ? envelopeBeaRotation : envelopeRestRotation);
        envelopeFlap.localRotation = Quaternion.Euler(encounter.EnvelopeOpen ? FlapOpenDegrees : 0f, 0f, 0f);
        bool open = papersShown && encounter.EnvelopeOpen;
        if (papersRoot.gameObject.activeSelf != open) papersRoot.gameObject.SetActive(open);
        Restack();
    }

    /// <summary>Bea's presentation: a short slide and turn across her table to its place on the mat, one quiet paper cue as it settles.</summary>
    private IEnumerator PresentMotion()
    {
        float start = Time.unscaledTime;
        while (true)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - start) / PresentSeconds);
            float eased = t * t * (3f - 2f * t);
            PlaceEnvelope(Vector3.Lerp(envelopeBeaPosition, envelopeRestPosition, eased), Quaternion.Slerp(envelopeBeaRotation, envelopeRestRotation, eased));
            if (t >= 1f) break;
            yield return null;
        }
        presenting = null;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_place_01", 0.45f);
    }

    /// <summary>
    /// The opening: the flap lifts while the papers slide out of the envelope into their pile with a short
    /// stagger; the top one, her featured record, comes out last and settles forward of the pile, turned a
    /// little toward the player. About half a second, one quiet paper cue.
    /// </summary>
    private IEnumerator Reveal()
    {
        var slots = new List<Transform>();
        var rests = new List<Vector3>();
        var turns = new List<Quaternion>();
        var piles = new List<Quaternion>();
        Restack();
        foreach (string id in stack.Order)
        {
            if (!slotBySource.TryGetValue(id, out Transform slot) || slot == null) continue;
            slots.Add(slot);
            rests.Add(slot.localPosition);
            turns.Add(slot.localRotation);
            piles.Add(slotTurn[id]);
        }
        Vector3 inside = new Vector3(0f, -0.004f, Level3TableStaging.EnvelopeDepth * Layout.Scale * 0.3f);
        for (int i = 0; i < slots.Count; i++) slots[i].localPosition = rests[i] + inside;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX(false);
        float start = Time.unscaledTime;
        while (true)
        {
            float elapsed = Time.unscaledTime - start;
            float flap = Mathf.Clamp01(elapsed / FlapSeconds);
            envelopeFlap.localRotation = Quaternion.Euler(FlapOpenDegrees * (1f - (1f - flap) * (1f - flap)), 0f, 0f);
            bool done = flap >= 1f;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;
                // Bottom paper first, the top one last.
                float t = Mathf.Clamp01((elapsed - StackDelay - (slots.Count - 1 - i) * StackStagger) / StackSeconds);
                float eased = 1f - (1f - t) * (1f - t);
                slots[i].localPosition = Vector3.Lerp(rests[i] + inside, rests[i], eased) + Vector3.up * (0.012f * Mathf.Sin(t * Mathf.PI));
                slots[i].localRotation = Quaternion.Slerp(piles[i], turns[i], eased);
                done &= t >= 1f;
            }
            if (done) break;
            yield return null;
        }
        revealing = null;
        Restack();
    }

    /// <summary>Interruption (leave, disable): every cosmetic move simply ends at the encounter's pose; no command reruns.</summary>
    private void SettleMotion()
    {
        if (presenting != null) { StopCoroutine(presenting); presenting = null; }
        if (revealing != null) { StopCoroutine(revealing); revealing = null; }
        ApplyEncounterPose();
    }

    /// <summary>One neat pile on the envelope, in handover order (top first; the featured record on top when she presents them). Each source is one existing paper.</summary>
    private void StackUp(List<ClientRecord> records, string featuredFirst)
    {
        float width = Layout.PaperWidth;
        var order = new List<string>();
        for (int i = 0; i < records.Count; i++)
        {
            ClientRecord record = records[i];
            if (record == null || bySource.ContainsKey(record.source)) continue;
            Vector2 units = SupportingDocumentFace.SizeOf(record.type);
            float aspect = units.y / units.x;
            float w = record.IsCard ? width * 0.9f : aspect > 1.6f ? width * 0.85f : width;
            var slot = new GameObject("Slot " + record.source).transform;
            slot.gameObject.layer = gameObject.layer;
            slot.SetParent(papersRoot, false);
            // A loose pile, not a fan: tiny alternating turns so the edges read as separate papers.
            slot.localRotation = Quaternion.Euler(0f, (i % 3 - 1) * 1.5f, 0f);
            ClientRecordPaper paper = ClientRecordPaper.Create(this, slot, workspace, record, new Vector2(w, w * aspect), gameObject.layer);
            // Opt in only this table's original touch surface. Shared L1/L2
            // ClientRecordPaper and its reader/drag handlers remain unchanged.
            foreach (RecordTouchForwarder touch in paper.GetComponentsInChildren<RecordTouchForwarder>(true))
            {
                // Match the full paper face plus a small forgiving border; follows its actual pose and scale.
                touch.transform.localScale *= paper.Record.IsCard ? 1.15f : 1.125f;
                GraphicRaycaster previous = touch.GetComponent<GraphicRaycaster>();
                if (previous != null) { previous.enabled = false; Destroy(previous); }
                touch.gameObject.AddComponent<Level3TablePaperRaycaster>();
            }
            paper.WindowCanvas = HudHotbar.Instance?.DocumentCanvas;
            paper.DragLiftSeconds = .16f;
            papers.Add(paper);
            bySource[record.source] = paper;
            slotBySource[record.source] = slot;
            slotTurn[record.source] = slot.localRotation;
            if (record.source == featuredFirst) order.Insert(0, record.source);
            else order.Add(record.source);
        }
        stack = new Level3TableStaging.Stack(order);
        Restack();
    }

    /// <summary>
    /// Heights follow the pile order (the top paper highest); slots of papers that are out keep their place in it.
    /// While it waits to be inspected the featured record lies forward of the pile, toward the player and the middle.
    /// </summary>
    private void SyncOriginalStack()
    {
        string rowId = flow.Row ?? ActiveRow?.RowId;
        // A practice slip has no original on Bea's table.
        if (stack == null || rowId == null || rowId == stackedRow || Level3SortingPractice.IsPracticeRow(rowId)) return;
        stackedRow = rowId;
        Level3WorksheetRow row = Level3TableStaging.RowOf(rows, rowId);
        string partnerRow = Level3WorksheetRules.ComparePartner(rows, rowId);
        string partner = Level3TableStaging.RowOf(rows, partnerRow)?.SourceId;
        var from = new Dictionary<Transform, Vector3>();
        foreach (Transform slot in slotBySource.Values) if (slot != null) from[slot] = slot.localPosition;
        if (restacking != null) { StopCoroutine(restacking); restacking = null; }
        bool changed = stack.Order.Count > 0 && (stack.Order[0] != row?.SourceId ||
            partner != null && (stack.Order.Count < 2 || stack.Order[1] != partner));
        stack.Prioritize(row?.SourceId, partner);
        compareFirst = partner != null ? row?.SourceId : null;
        compareSecond = partner;
        Restack();
        if (changed && Seated && !WorldEmote.ReducedMotion) restacking = StartCoroutine(ReorderPile(from));
    }

    private System.Collections.IEnumerator ReorderPile(Dictionary<Transform, Vector3> from)
    {
        var to = new Dictionary<Transform, Vector3>();
        foreach (Transform slot in from.Keys) if (slot != null) to[slot] = slot.localPosition;
        for (float elapsed = 0f; elapsed < .22f; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / .22f), ease = WorldPickupMotion.Smooth(t);
            foreach (var pair in to)
                if (pair.Key != null) pair.Key.localPosition = Vector3.Lerp(from[pair.Key], pair.Value, ease) +
                    Vector3.up * (Mathf.Sin(t * Mathf.PI) * StackLayer * 6f);
            yield return null;
        }
        foreach (var pair in to) if (pair.Key != null) pair.Key.localPosition = pair.Value;
        restacking = null;
    }

    private void Restack()
    {
        if (stack == null) return;
        int count = stack.Count;
        for (int i = 0; i < count; i++)
        {
            string id = stack.Order[i];
            if (!slotBySource.TryGetValue(id, out Transform slot) || slot == null) continue;
            bool forward = LiesForward(id);
            slot.localPosition = forward ? FeaturedRest(id) : StackRest(i, count);
            if (!forward && i == 1 && compareSecond != null)
                slot.localPosition += new Vector3(-0.018f * Layout.Scale, 0f, 0.03f * Layout.Scale);
            slot.localRotation = forward ? slotTurn[id] * Quaternion.Euler(0f, FeatureYaw, 0f) : slotTurn[id];
        }
        PlaceRecordTarget();
        if (touchOn) SetPapersInteractive(true);
    }

    private Vector3 StackRest(int fromTop, int count) => new Vector3(0f, StackLayer * (count - fromTop), 0f);

    private Vector3 PileRest(string sourceId)
    {
        int index = stack != null ? IndexInPile(sourceId) : 0;
        return StackRest(index, stack != null ? stack.Count : 1);
    }

    private int IndexInPile(string sourceId)
    {
        for (int i = 0; i < stack.Count; i++)
            if (stack.Order[i] == sourceId) return i;
        return 0;
    }

    /// <summary>The featured record forward of the pile: a little toward the player (-Z) and the middle lane (+X), just above the top paper.</summary>
    private Vector3 FeaturedRest(string sourceId)
    {
        Vector2 size = bySource.TryGetValue(sourceId, out ClientRecordPaper paper) && paper != null ? paper.Size : new Vector2(Layout.PaperWidth, Layout.PaperWidth);
        return PileRest(sourceId) + new Vector3(size.x * 0.16f, StackLayer, -size.y * 0.16f);
    }

    private bool LiesForward(string sourceId) =>
        encounter.FeaturedForward && sourceId == encounter.Featured && bySource.TryGetValue(sourceId, out ClientRecordPaper paper) && paper != null && paper.IsAttached;

    /// <summary>"Inspect a record" covers the featured record where it lies, with a generous margin (its own collider, no paper changes).</summary>
    private void PlaceRecordTarget()
    {
        if (recordTarget == null || encounter.Featured == null || !slotBySource.TryGetValue(encounter.Featured, out Transform slot) || slot == null) return;
        Vector2 size = bySource.TryGetValue(encounter.Featured, out ClientRecordPaper paper) && paper != null ? paper.Size : new Vector2(Layout.PaperWidth, Layout.PaperWidth);
        recordTarget.SetVisual(paper != null ? paper.transform : null);
        Transform target = recordTarget.transform;
        target.localPosition = slot.localPosition;
        target.localRotation = slot.localRotation;
        var box = (BoxCollider)target.GetComponent<Collider>();
        box.center = new Vector3(0f, 0.008f, 0f);
        box.size = new Vector3(size.x + 0.04f, 0.03f, size.y + 0.04f);
    }

    /// <summary>Lifts that source's original into the reader (the one already out goes back). False when it is not on the table or the player is not seated.</summary>
    public bool ReadOriginal(string sourceId)
    {
        if (!Seated || sourceId == null || !bySource.TryGetValue(sourceId, out ClientRecordPaper paper) || paper == null) return false;
        if (paper.IsAttached) paper.TakeOut();
        else paper.BringWindowToFront();
        return true;
    }

    /// <summary>
    /// The retired worksheet board's seam: revised T04 no longer builds
    /// Level3WorksheetDesk at this table (no board, no calculator here). Kept
    /// only so that unbuilt class still compiles until T05/T17 retire it.
    /// </summary>
    public void HoldPapers(bool up)
    {
        if (up) GatherAll();
    }

    /// <summary>INSPECT ORIGINAL (stand card or note card): its record's original comes out; a second tap while it is out puts it back.</summary>
    private void InspectActive()
    {
        if (!Seated) return;
        // Resolve the row from the interaction owner, including a recalled final record when the stand is empty.
        Level3WorksheetRow row = Level3TableStaging.RowOf(rows, flow.Row) ?? ActiveRow;
        if (row == null || !bySource.TryGetValue(row.SourceId, out ClientRecordPaper paper) || paper == null)
        {
            Level3Say("Choose a record to inspect.");
            return;
        }
        MobileInputGuard.SuppressWorldTapsThisFrame();
        Machine.CloseWhy(animate: false);
        if (paper.IsSettledOut) { paper.PutBack(); return; }
        // A repeat while returning must reopen the same original, rather than silently doing nothing.
        if (!paper.IsAttached) paper.Gather();
        paper.SetEventCamera(ViewCamera);
        SetPaperTouch(paper, touchOn);
        paper.TakeOut();
    }

    private void SetPapersInteractive(bool on)
    {
        if (papersRoot == null) return;
        // T05: while the Why? card is up in the reader, no original may rise into the same place.
        on &= Machine == null || !Machine.WhyUp;
        foreach (ClientRecordPaper paper in papers)
        {
            int index = IndexInPile(paper.Record.source);
            SetPaperTouch(paper, on && (!paper.IsAttached || index == 0 || (compareSecond != null && index == 1)));
        }
        if (workspace != null)
            foreach (GraphicRaycaster raycaster in workspace.GetComponentsInChildren<GraphicRaycaster>(true))
                if (!(raycaster is Level3TablePaperRaycaster independent && independent.IndependentDocument)) raycaster.enabled = on;
    }

    // =========================================================
    // STAND CARD AND INTERRUPTIONS
    // =========================================================

    private void Update()
    {
        if (!Built || Machine == null) return;
        if (!paperCanvasBound && HudHotbar.Instance?.DocumentCanvas != null)
        {
            foreach (ClientRecordPaper original in papers) if (original != null) original.WindowCanvas = HudHotbar.Instance.DocumentCanvas;
            paperCanvasBound = true;
        }
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        if (chapter != null)
        {
            Level3Phase phase = Level3ChapterRules.PhaseOf(chapter);
            int revision = chapter.worksheet != null ? chapter.worksheet.revision : 0;
            int checks = chapter.worksheetCheck != null ? chapter.worksheetCheck.checks : 0;
            // A published change (or a quiet restore) redraws the card; nothing here writes. The T06 acceptance and
            // collection presentations own the view until they finish (they settle on any interruption).
            if (!reviewing && !collecting && (phase != shownPhase || revision != shownRevision || checks != shownChecks || encounter.Step != shownStep))
                RefreshStand(animate: false);
        }
        TickCollect();
        // Help Bea: the card arrives on the stand once the seat's camera has settled, never during the move.
        if (presentCard && Seated && !introPending && CameraController.Instance != null && !CameraController.Instance.IsTransitioning)
        {
            presentCard = false;
            RefreshStand(animate: true);
        }
        // Follow-up (Arthur first): once the first practice slip is on the stand, Arthur sorts the four himself.
        if (practiceDemo == null && Seated && !introPending && !presentCard && practiceDone == 0 && encounter.CardShown &&
            CameraController.Instance != null && !CameraController.Instance.IsTransitioning && FreeToTouch() &&
            flow.Step == Level3TableStaging.MachineStep.Ready && Level3SortingPractice.IsPracticeRow(flow.Row) &&
            Level3SortingPractice.Due(chapter) && !Level3SortingPractice.DemoSeen(chapter))
            practiceDemo = StartCoroutine(PracticeDemo());
        Machine.Tick();
        SyncOriginalStack();
        UpdateEncounter(chapter);

        bool free = CanTouchPapers;
        if (free != touchOn)
        {
            // Settings, a conversation, a pause or the camera moving: no touches, and no drag keeps going behind them.
            if (!free) CancelDrags();
            SetTouch(free);
            if (Seated || ReadingStanding) SetPapersInteractive(free);
        }
    }

    /// <summary>
    /// T04 buildup, every frame (cheap: flags and two colliders): exactly one table target is live (the seat box
    /// covers the envelope, so it is off until her offer), Bea's one look of uncertainty, a tap off the standing
    /// reader puts the record back, and her organizer offer follows the returned record when nothing holds the player.
    /// </summary>
    private void UpdateEncounter(Level3ChapterCheckpoint chapter)
    {
        bool moving = presenting != null || revealing != null;
        // The seat box stays solid; off the Interactable layer the first-person ray passes it to the envelope or record.
        int seatLayer = encounter.SeatOpen ? InteractLayer : 0;
        if (tableCollider != null && tableCollider.gameObject.layer != seatLayer) tableCollider.gameObject.layer = seatLayer;
        envelopeTarget.Active = encounter.Step == Level3TableStaging.EncounterStep.Presented && !moving;
        envelopeTarget.Affordance(envelopeTarget.Active);
        recordTarget.Active = encounter.FeaturedForward && !moving && !ReadingStanding && LiesForward(encounter.Featured);

        if (!noticed && encounter.Step == Level3TableStaging.EncounterStep.Waiting && Level3ChapterRules.PhaseOf(chapter) == Level3Phase.BeaRecords &&
            Bea != null && Camera.main != null && !DialogueUI.IsAnyDialogueActive &&
            (Camera.main.transform.position - Bea.transform.position).sqrMagnitude < NoticeDistance * NoticeDistance)
        {
            noticed = true;
            WorldEmote.Show(Bea.transform, Emotion.Thinking, 2.5f);
        }

        if (encounter.Step != Level3TableStaging.EncounterStep.Inspected || offerAt < 0f || Time.unscaledTime < offerAt) return;
        if (CanOfferNow() && Bea.OfferHelp()) offerAt = -1f;
        // Not now (settings, a card, walked off): her offer waits for the next talk instead.
        else if (Time.unscaledTime > offerAt + OfferWindow) offerAt = -1f;
    }

    private bool CanOfferNow() =>
        Bea != null && Bea.isActiveAndEnabled && !Seated && !ReadingStanding && !HudWindowLock.IsHeld && !InGameSettings.IsOpen && !HudRewardScreen.AnyOpen &&
        CameraController.Instance != null && CameraController.Instance.CanStartDialogue && !DialogueUI.IsAnyDialogueActive &&
        (GameStateMachine.Instance == null || !GameStateMachine.Instance.IsPaused) && Camera.main != null &&
        (Camera.main.transform.position - Bea.transform.position).sqrMagnitude < OfferDistance * OfferDistance;

    /// <summary>Level3BeaInteractable: her organizer offer reached its choice (Help Bea / Later). The seat opens.</summary>
    public void OfferShown()
    {
        if (encounter.Offer()) offerAt = -1f;
    }

    /// <summary>Help Bea (or her table after Later): the deliberate step into the existing seated workspace. False before her offer.</summary>
    /// <summary>
    /// The machine part the guide's hand may point at: only while the player can really use it now
    /// (never during the splash, Arthur's demonstration, a camera move or a modal). Never names a right answer.
    /// </summary>
    public Transform GuidePart(string part)
    {
        if (Machine == null || !Seated || introPending || demoRunning || !FreeToTouch() || HudRewardScreen.AnyOpen) return null;
        switch (part)
        {
            case "card": return flow.Step == Level3TableStaging.MachineStep.Ready ? Machine.Card : null;
            case "intake": return flow.Step == Level3TableStaging.MachineStep.Ready ? Machine.IntakeSlot : null;
            case "dial": return flow.Step == Level3TableStaging.MachineStep.Loaded ? Machine.DialPointer : null;
            case "lever": return flow.Step == Level3TableStaging.MachineStep.Loaded && flow.Treatment != "" ? Machine.LeverHinge : null;
            case "why": return flow.CanAskWhy ? Machine.WhyRow : null;
            case "batch": return BatchFiledHere ? Machine.BatchKey : null;
            case "summary": return SummaryAvailable && Machine.SummaryShown ? Machine.SummaryCard : null;
            default: return null;
        }
    }

    public bool AcceptHelp(Pose? fromView = null) => encounter.Done && TrySit(fromView);

    /// <summary>Level3BeaInteractable: her introduction played to the end (a failed save then retries with its last line).</summary>
    public void HeardIntroduction() => encounter.HeardIntroduction();

    private bool FreeToTouch() => CameraController.Instance != null && CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation &&
        !CameraController.Instance.InputHeld && !SideQuestSplash.IsShowing && !InGameSettings.IsOpen && !DialogueUI.IsAnyDialogueActive &&
        (GameStateMachine.Instance == null || !GameStateMachine.Instance.IsPaused);

    private void SetTouch(bool on)
    {
        touchOn = on;
        Machine?.SetTouch(on, ViewCamera);
    }

    /// <summary>
    /// The stand shows the first record of the open batch the saved draft has not filed (worksheet phases only).
    /// A record half-fed, saving or feeding stays where it is; the windows and tabs follow the published draft
    /// (after a feed has finished). Nothing is placed or saved here.
    /// </summary>
    private void RefreshStand(bool animate)
    {
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        shownPhase = chapter != null ? Level3ChapterRules.PhaseOf(chapter) : (Level3Phase)(-1);
        shownRevision = chapter?.worksheet != null ? chapter.worksheet.revision : 0;
        shownChecks = chapter?.worksheetCheck != null ? chapter.worksheetCheck.checks : 0;
        shownStep = encounter.Step;
        // Follow-up: Arthur's demonstration owns the stand and machine until it finishes or is stopped.
        if (demoRunning) { RefreshMachine(chapter, animate: false); return; }
        // T04 buildup: one record in focus at a time, so the stand stays empty until the seated workspace's turn.
        bool working = papersShown && encounter.CardShown && !presentCard && chapter != null &&
                       shownPhase >= Level3Phase.WorksheetDraft && shownPhase < Level3Phase.BeaComputation;
        if (!working && (flow.Step == Level3TableStaging.MachineStep.Loaded || flow.Busy))
        {
            // The worksheet left the editable phases under a loaded record (a restore, T06's acceptance): settle, file nothing.
            Machine?.Settle();
            flow.Reset();
            Machine?.Rest();
        }
        if (flow.Step == Level3TableStaging.MachineStep.Loaded || flow.Busy)
        {
            RefreshMachine(chapter, animate: false);
            return;
        }
        // Teaching pass: the practice packet's slips come before Bea's first record (never once anything of hers is filed).
        Level3WorksheetRow next = !working ? null
            : Level3SortingPractice.Due(chapter) ? Level3SortingPractice.Row(Level3SortingPractice.Next(Math.Min(practiceDone, Level3SortingPractice.RowIds.Length - 1)))
            : Level3TableStaging.ActiveRow(chapter, rows);
        if (next?.RowId != flow.Row || flow.Step == Level3TableStaging.MachineStep.Empty) { flow.Present(next?.RowId); practiceHint = null; }
        ActiveRow = next;
        if (!working) ledger = -1;
        // T06: the read-only totals list borrows the stand; the record card comes back when it closes.
        if (next != null && ledger < 0) Machine?.ShowCard(FaceOf(definition, next), animate);
        else Machine?.ShowCard(null, false);
        RefreshBatchCard(chapter, working, animate);
        RefreshSummary(chapter, working);
        RefreshMachine(chapter, animate);
    }

    /// <summary>The machine's windows, tabs, selector, note card and live surfaces as the published draft and local step say.</summary>
    private void RefreshMachine(Level3ChapterCheckpoint chapter, bool animate)
    {
        if (Machine == null) return;
        bool working = papersShown && encounter.CardShown && !presentCard && chapter != null &&
                       shownPhase >= Level3Phase.WorksheetDraft && shownPhase < Level3Phase.BeaComputation;
        if (flow.Step != Level3TableStaging.MachineStep.Feeding)
        {
            Level3WorksheetRules.DraftTotals(chapter, out decimal income, out decimal deductions);
            Machine.SetTotals(working || SummaryAvailable ? Money(income) : "—", working || SummaryAvailable ? Money(deductions) : "—", animate);
            // T06: while an accepted batch's ticks and gather play, its tabs stay as shown (the saved draft has already
            // opened the next batch); the gather then empties the rack.
            if (!reviewing)
            {
                var tabs = new List<(string, string, string)>();
                if (working)
                    foreach (Level3WorksheetRow row in Level3TableStaging.RackRows(chapter, rows))
                        tabs.Add((row.RowId, row.Label, Level3WorksheetRules.TreatmentLabel(Level3WorksheetRules.SavedTreatment(chapter.worksheet, row.RowId))));
                string hidden = flow.Recalled ? flow.Row : null;
                Machine.SetTabs(tabs, hidden, settleTab, animate);
                settleTab = null;
            }
            // T06: each tab's mark from the last saved check (only while its value is as checked).
            var marks = new Level3WorksheetCheck.Mark[3];
            for (int slot = 0; slot < marks.Length; slot++)
            {
                string tabRow = working ? Machine.TabRow(slot) : null;
                marks[slot] = tabRow != null ? Level3WorksheetCheck.RowMark(chapter, tabRow) : Level3WorksheetCheck.Mark.None;
            }
            Machine.SetTabMarks(marks, marksPop);
            marksPop = false;
        }
        bool loaded = flow.Step == Level3TableStaging.MachineStep.Loaded;
        Machine.SetView(new Level3PaperMachineModel.View
        {
            CardOnStand = flow.Step == Level3TableStaging.MachineStep.Ready,
            Loaded = loaded,
            Busy = flow.Busy,
            Recalled = flow.Recalled,
            NeedsWhy = flow.CanAskWhy,
            LeverReady = loaded && flow.Missing == null,
            TabsLive = working && !reviewing && !collecting && (flow.Step == Level3TableStaging.MachineStep.Ready || flow.Step == Level3TableStaging.MachineStep.Empty),
            WindowsLive = (working || SummaryAvailable) && !reviewing && !collecting && (flow.Step == Level3TableStaging.MachineStep.Ready || flow.Step == Level3TableStaging.MachineStep.Empty),
            SummaryLive = false
        });
        Machine.SetWhyAvailable(flow.CanAskWhy);
        Machine.SetTouch(touchOn, ViewCamera);
    }

    private static string Money(decimal value) => "₱" + Level3Calculator.Format(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    // =========================================================
    // T05: THE PAPER MACHINE (intents -> local flow -> one filing)
    // =========================================================

    /// <summary>A touch on the machine, stand card, note card, Why? card or a tab. During a feed any touch only finishes it.</summary>
    private void OnMachineInput(Level3MachineInput input, int index)
    {
        if (!Seated || Machine == null || demoRunning) return;
        // T06: during a saved collection's flight nothing else starts; a touch during an accepted batch's
        // gather or the summary's arrival only finishes it (the save is already done).
        if (collecting) return;
        if (reviewing) { FinishReview(); return; }
        if (Machine.SummaryMoving) { Machine.Settle(); return; }
        if (flow.Step == Level3TableStaging.MachineStep.Feeding)
        {
            // Skip the cosmetic feed: it settles to the committed state; this touch does nothing else.
            if (input != Level3MachineInput.CardDropMissed) Machine.Settle();
            return;
        }
        if (flow.Busy) return;
        switch (input)
        {
            case Level3MachineInput.Card:
                if (flow.ToggleArm())
                {
                    Machine.Arm(flow.Armed);
                    if (flow.Armed) Cue("sfx_card_pickup_01", 0.5f);
                }
                break;
            case Level3MachineInput.Inspect:
                Machine.CloseWhy(animate: true);
                InspectActive();
                break;
            case Level3MachineInput.Compare:
                CompareOriginals();
                break;
            case Level3MachineInput.Intake:
                FeedCard();
                break;
            case Level3MachineInput.CardDropped:
                if (!FeedCard()) Machine.ReturnToStand(null);
                break;
            case Level3MachineInput.CardDropMissed:
                // Off target (or a cancelled drag): back onto the stand, nothing loaded or saved.
                Machine.ReturnToStand(() => Cue("sfx_card_return_01", 0.5f));
                if (flow.Armed) flow.ToggleArm();
                RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
                break;
            case Level3MachineInput.Header:
                BackOut();
                break;
            case Level3MachineInput.HeaderDragged:
                if (flow.Step == Level3TableStaging.MachineStep.Loaded && !flow.Recalled && BackOutKeepingDrag()) Machine.PullOut();
                break;
            case Level3MachineInput.Detent:
                if (index >= 0 && index < Level3WorksheetRules.Treatments.Length) Choose(Level3WorksheetRules.Treatments[index]);
                break;
            case Level3MachineInput.DialStep:
                if (flow.Step == Level3TableStaging.MachineStep.Loaded) Choose(flow.Stepped(index));
                break;
            case Level3MachineInput.Why:
                OpenWhy();
                break;
            case Level3MachineInput.WhyOption:
                if (index >= 0 && index < Level3WorksheetRules.Reasons.Length && flow.ChooseReason(Level3WorksheetRules.Reasons[index]))
                {
                    Cue("sfx_ui_select_01", 0.5f);
                    GameplayEvents.RaiseGuideCue("MachineReasonChosen");
                }
                CloseWhy();
                break;
            case Level3MachineInput.WhyClose:
                CloseWhy();
                break;
            case Level3MachineInput.LeverPulled:
            case Level3MachineInput.LeverTapped:
                PullLever();
                break;
            case Level3MachineInput.Tab:
                RecallTab(index);
                break;
            case Level3MachineInput.BatchKey:
                BatchKey();
                break;
            case Level3MachineInput.Window:
                ToggleLedger(index);
                break;
            case Level3MachineInput.Summary:
                OpenSummary();
                break;
        }
    }

    /// <summary>Half-feeds the stand's card (tap on the slot, or a drop over it). Its saved choices (none for a new row) come with it. Saves nothing.</summary>
    private bool FeedCard()
    {
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        bool practice = Level3SortingPractice.IsPracticeRow(flow.Row);
        if (chapter == null || flow.Step != Level3TableStaging.MachineStep.Ready ||
            !(practice ? Level3SortingPractice.Due(chapter) : Level3WorksheetRules.RowOpen(chapter, flow.Row))) return false;
        string row = flow.Row;
        // A practice slip always starts neutral (it is never saved).
        if (!(practice ? flow.Feed("", "", chapter.worksheet.revision)
                       : flow.Feed(Level3WorksheetRules.SavedTreatment(chapter.worksheet, row), Level3WorksheetRules.SavedReason(chapter.worksheet, row), chapter.worksheet.revision)))
            return false;
        practiceHint = null;
        PutOriginalsAway();
        Machine.HalfFeed(() => Cue("sfx_paper_place_01", 0.55f));
        ShowLoaded(animate: true, delay: Level3PaperMachineModel.HalfFeedSeconds * 0.5f);
        GameplayEvents.RaiseGuideCue("MachineFed");
        return true;
    }

    /// <summary>The loaded record's selector and note card (context, Why?, INSPECT, COMPARE).</summary>
    private void ShowLoaded(bool animate, float delay)
    {
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        bool practice = Level3SortingPractice.IsPracticeRow(flow.Row);
        Level3WorksheetRow row = practice ? Level3SortingPractice.Row(flow.Row) : Level3TableStaging.RowOf(rows, flow.Row);
        ActiveRow = row;
        Machine.SetSelector(Array.IndexOf(Level3WorksheetRules.Treatments, flow.Treatment), animate: false, loaded: true);
        Machine.HideBatchCard(animate: false);
        Machine.ShowNote(FaceOf(definition, row), flow.NeedsReason, !practice && Level3WorksheetRules.ComparePartner(rows, flow.Row) != null,
            Level3WorksheetRules.ReasonLabel(flow.Reason), animate, delay, practice ? practiceHint : LookAgainLine(Level3Checkpointer.Active?.State, flow.Row));
        RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
    }

    /// <summary>Tap on the loaded card: a new record goes back to the stand (nothing saved); a recalled one feeds back in unchanged.</summary>
    private void BackOut()
    {
        if (!flow.Withdraw(out bool toStand)) return;
        Machine.CloseWhy(animate: false);
        Machine.HideNote(animate: true);
        Machine.SetSelector(-1, animate: true, loaded: false);
        if (toStand)
        {
            Machine.ReturnToStand(() => Cue("sfx_card_return_01", 0.5f));
            RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
        }
        else
        {
            // Cancel the recall: the saved filing stands; the card goes back in and its tab returns.
            string row = ActiveRow?.RowId;
            Machine.FeedIn(() =>
            {
                settleTab = row;
                RefreshStand(animate: true);
            });
            Cue("sfx_paper_rustle_01", 0.45f);
            RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
        }
    }

    /// <summary>The loaded card dragged by its header: backed out (nothing saved) and carried on by the same drag.</summary>
    private bool BackOutKeepingDrag()
    {
        if (!flow.Withdraw(out bool toStand) || !toStand) return false;
        Machine.CloseWhy(animate: false);
        Machine.HideNote(animate: true);
        Machine.SetSelector(-1, animate: true, loaded: false);
        RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
        return true;
    }

    /// <summary>A selector detent: any treatment for any row, neutral feedback only (no correctness snap or sound difference).</summary>
    private void Choose(string treatment)
    {
        bool first = flow.Treatment == "";
        if (!flow.Choose(treatment)) return;
        Machine.SetSelector(Array.IndexOf(Level3WorksheetRules.Treatments, treatment), animate: true, loaded: true);
        Cue("sfx_ui_click_01", 0.45f);
        if (first) GameplayEvents.RaiseGuideCue("MachineChosen");
        RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
    }

    private void OpenWhy()
    {
        if (!flow.CanAskWhy) return;
        PutOriginalsAway();
        SetPapersInteractive(false);
        Machine.OpenWhy(ViewCamera, flow.Reason);
        AudioManager.Instance?.PlayDocumentSFX(false);
        RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
    }

    private void CloseWhy()
    {
        if (!Machine.WhyUp) return;
        Machine.SetReasonLabel(Level3WorksheetRules.ReasonLabel(flow.Reason));
        Machine.CloseWhy(animate: true);
        if (Seated) SetPapersInteractive(touchOn);
        RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
    }

    /// <summary>
    /// The lever (released past its threshold, or tapped): ONE filing of the loaded record's treatment and
    /// reason through the existing CommitWorksheet seam. Saved (or already saved exactly): the identical
    /// neutral feed. Not saved: the card stays half-fed with its choices and the lever springs back.
    /// </summary>
    private void PullLever()
    {
        if (flow.Step != Level3TableStaging.MachineStep.Loaded) return;
        if (flow.Missing != null)
        {
            Machine.LeverResist();
            Level3Say(flow.Missing);
            return;
        }
        if (Level3SortingPractice.IsPracticeRow(flow.Row))
        {
            PullPractice();
            return;
        }
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null || !flow.BeginFile(out Level3Filing filing)) return;
        MobileInputGuard.SuppressWorldTapsThisFrame();
        string row = filing.RowId;
        List<Level3WorksheetRow> current = rows;
        Level3Checkpointer.TapeCommit result = owner.CommitWorksheet(
            (Level3ChapterCheckpoint candidate, out string reason) => Level3WorksheetRules.File(candidate, current, filing, out reason), out string hint);
        bool committed = result == Level3Checkpointer.TapeCommit.Attached || result == Level3Checkpointer.TapeCommit.AlreadyAttached;
        flow.Saved(committed);
        if (!committed)
        {
            // Nothing changed: the same choices stay loaded for another pull.
            Machine.LeverBack();
            Level3Say(string.IsNullOrEmpty(hint) ? "Couldn't file that. Try the lever again." : hint);
            RefreshMachine(owner.State, animate: false);
            return;
        }
        Machine.CloseWhy(animate: false);
        Machine.HideNote(animate: true);
        Machine.LeverStroke();
        Machine.SetSelector(-1, animate: true, loaded: false);
        Cue("sfx_paper_rustle_01", 0.45f);
        settleTab = row;
        Machine.FeedIn(FeedDone);
        RefreshMachine(owner.State, animate: false);
        if (result == Level3Checkpointer.TapeCommit.Attached) GameplayEvents.RaiseGuideCue("WorksheetFiled");
    }

    /// <summary>
    /// Teaching pass: a practice slip's pull, answered at once and never filed. Wrong: the lever springs back
    /// and the note card's "Look again" line names the question to ask; the same slip waits. Right: the same
    /// neutral feed as a filing, one short why, and the next slip; after the fourth the packet's one saved
    /// milestone comes first (a failed save keeps the slip loaded for another pull).
    /// </summary>
    private void PullPractice()
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null || flow.Step != Level3TableStaging.MachineStep.Loaded || flow.Missing != null) return;
        MobileInputGuard.SuppressWorldTapsThisFrame();
        string row = flow.Row;
        bool right = Level3SortingPractice.Judge(row, flow.Treatment, flow.Reason, out string feedback);
        if (!right)
        {
            // Arthur explains, as in Level 2's practice; the slip stays loaded with its choices for another try.
            Machine.LeverBack();
            practiceHint = null;
            Level3Say(feedback, null, 5f);
            GameplayEvents.RaiseGuideCue("PracticeSlipRetry");
            return;
        }
        bool last = Level3SortingPractice.Next(practiceDone + 1) == null;
        if (last)
        {
            Level3Checkpointer.TapeCommit saved = owner.CommitSortingPractice(out string hint);
            if (saved != Level3Checkpointer.TapeCommit.Attached && saved != Level3Checkpointer.TapeCommit.AlreadyAttached)
            {
                Machine.LeverBack();
                Level3Say(string.IsNullOrEmpty(hint) ? "Couldn't save. Pull the lever again." : hint);
                return;
            }
        }
        if (!flow.BeginFile(out _)) return;
        flow.Saved(true);
        practiceDone++;
        practiceHint = null;
        Machine.CloseWhy(animate: false);
        Machine.HideNote(animate: true);
        Machine.LeverStroke();
        Machine.SetSelector(-1, animate: true, loaded: false);
        Cue("sfx_paper_rustle_01", 0.45f);
        Machine.FeedIn(FeedDone);
        Level3Say(last ? feedback + Level3SortingPractice.DoneLine : feedback, last ? "wow" : null, last ? 6f : 4f);
        RefreshMachine(owner.State, animate: false);
        GameplayEvents.RaiseGuideCue("PracticeSlipFiled");
    }

    /// <summary>
    /// Follow-up (Arthur first, as Level 2's IncomeToolkit.Demo): Arthur sorts the same four practice slips while
    /// the player watches. Each slip comes to the stand, half-feeds, the dial turns, P3/P4 open Why? and take their
    /// reason, and the lever files it, with one line from Arthur per step. Nothing is filed, checked or saved except,
    /// at the end, the "seen" marker; then the first slip is back on the stand for the player's own turn.
    /// </summary>
    private IEnumerator PracticeDemo()
    {
        demoRunning = true;
        CancelDrags();
        PutOriginalsAway();
        SetPapersInteractive(false);
        SetTouch(false);
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active?.State;
        int revision = chapter?.worksheet != null ? chapter.worksheet.revision : 0;
        RefreshMachine(chapter, animate: false);
        yield return DemoWait(0.5f);
        foreach (Level3SortingPractice.DemoStep step in Level3SortingPractice.Demo())
        {
            if (flow.Row != step.RowId || flow.Step != Level3TableStaging.MachineStep.Ready)
            {
                flow.Present(step.RowId);
                Machine.ShowCard(Level3SortingPractice.Face(step.RowId), animate: true);
                yield return DemoWait(0.5f);
            }
            yield return DemoSay(step.Read);
            // Halfway into the slot (the same motion as the player's feed).
            if (!flow.Feed("", "", revision)) break;
            Machine.HalfFeed(() => Cue("sfx_paper_place_01", 0.55f));
            ShowLoaded(animate: true, delay: Level3PaperMachineModel.HalfFeedSeconds * 0.5f);
            yield return DemoWait(0.8f);
            // The dial.
            flow.Choose(step.Treatment);
            Machine.SetSelector(Array.IndexOf(Level3WorksheetRules.Treatments, step.Treatment), animate: true, loaded: true);
            Cue("sfx_ui_click_01", 0.45f);
            RefreshMachine(chapter, animate: false);
            yield return DemoSay(step.Choose);
            if (step.Reason.Length > 0)
            {
                Machine.SetWhyAvailable(true);
                Machine.OpenWhy(ViewCamera, "");
                AudioManager.Instance?.PlayDocumentSFX(false);
                yield return DemoWait(0.7f);
                flow.ChooseReason(step.Reason);
                Machine.MarkReason(step.Reason);
                Cue("sfx_ui_select_01", 0.5f);
                yield return DemoSay(step.Why);
                Machine.SetReasonLabel(Level3WorksheetRules.ReasonLabel(step.Reason));
                Machine.CloseWhy(animate: true);
                RefreshMachine(chapter, animate: false);
                yield return DemoWait(0.4f);
            }
            // The lever, and the same neutral full feed as any filing (nothing is filed: it is Arthur's example).
            if (!flow.BeginFile(out _)) break;
            flow.Saved(true);
            Machine.CloseWhy(animate: false);
            Machine.HideNote(animate: true);
            Machine.LeverStroke();
            Machine.SetSelector(-1, animate: true, loaded: false);
            Cue("sfx_paper_rustle_01", 0.45f);
            bool fed = false;
            Machine.FeedIn(() => { flow.Fed(); fed = true; Cue("sfx_card_place_02", 0.3f); });
            RefreshMachine(chapter, animate: false);
            float until = Time.unscaledTime + 3f;
            while (!fed && Time.unscaledTime < until) yield return null;
            if (!fed) Machine.Settle();
            yield return DemoWait(0.5f);
        }
        // The same four slips, back on the stand for the player.
        Level3Say(Level3SortingPractice.YourTurnLine, "wow", 5f);
        Level3Checkpointer.Active?.CommitDemoSeen(null);
        if (flow.Step != Level3TableStaging.MachineStep.Empty && flow.Step != Level3TableStaging.MachineStep.Ready) { flow.Reset(); Machine.Rest(); }
        practiceDone = 0;
        practiceHint = null;
        demoRunning = false;
        practiceDemo = null;
        bool free = CanTouchPapers;
        SetTouch(free);
        if (Seated) SetPapersInteractive(free);
        RefreshStand(animate: true);
    }

    /// <summary>Stops Arthur's demonstration (leaving the seat, disable): nothing was saved; it plays again next visit.</summary>
    private void StopPracticeDemo()
    {
        if (practiceDemo != null) StopCoroutine(practiceDemo);
        practiceDemo = null;
        if (!demoRunning) return;
        demoRunning = false;
        practiceDone = 0;
        practiceHint = null;
        Machine?.CloseWhy(animate: false);
    }

    /// <summary>One line from Arthur, held for a reading time that grows with its length (paused time does not count).</summary>
    private IEnumerator DemoSay(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) yield break;
        float seconds = Mathf.Clamp(1.4f + line.Split(' ').Length * 0.3f, 2.5f, 6.5f);
        Level3Say(line, null, seconds);
        yield return DemoWait(seconds);
    }

    /// <summary>Real time that only runs while teaching is not paused (settings, a conversation, the pause menu).</summary>
    private static IEnumerator DemoWait(float seconds)
    {
        for (float t = 0f; t < seconds;)
        {
            if (!InGameSettings.IsOpen && !DialogueUI.IsAnyDialogueActive && !SideQuestSplash.IsShowing &&
                (GameStateMachine.Instance == null || !GameStateMachine.Instance.IsPaused)) t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    /// <summary>The stand/note card face of a row: a practice slip's own facts, or Bea's record (case content).</summary>
    private static Level3TableStaging.CardFace FaceOf(CaseDefinition definition, Level3WorksheetRow row) =>
        row != null && Level3SortingPractice.IsPracticeRow(row.RowId) ? Level3SortingPractice.Face(row.RowId) : Level3TableStaging.Face(definition, row);

    /// <summary>The saved record is inside: its tab settles, the windows tick, and the next record arrives on the stand.</summary>
    private void FeedDone()
    {
        flow.Fed();
        Cue("sfx_card_place_02", 0.3f);
        RefreshStand(animate: true);
    }

    /// <summary>A review tab: that filed record comes back half-fed with its saved choices (no save; the next card waits).</summary>
    private void RecallTab(int slot)
    {
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        string row = Machine.TabRow(slot);
        if (chapter == null || row == null || !Level3WorksheetRules.RowOpen(chapter, row)) return;
        if (!flow.Recall(row, Level3WorksheetRules.SavedTreatment(chapter.worksheet, row), Level3WorksheetRules.SavedReason(chapter.worksheet, row), chapter.worksheet.revision))
            return;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        PutOriginalsAway();
        ledger = -1;
        batchNote = null;
        Machine.HideBatchCard(animate: true);
        Machine.Emerge(Level3TableStaging.Face(definition, Level3TableStaging.RowOf(rows, row)), () =>
        {
            RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
            // Review means read the full source, not just a title hidden above the intake.
            SyncOriginalStack();
        });
        Cue("sfx_paper_rustle_01", 0.4f);
        ShowLoaded(animate: true, delay: Level3PaperMachineModel.CardAwaySeconds);
        GameplayEvents.RaiseGuideCue("MachineRecalled");
    }

    // =========================================================
    // REVISED T06: CHECK BATCH, REVIEW, TOTALS LIST, SUMMARY, COLLECT
    // =========================================================

    private static string BatchTitle(string batch) =>
        batch == Level3WorksheetRules.EarningsBatch ? "Earnings" : batch == Level3WorksheetRules.CostsBatch ? "Business costs"
        : batch == Level3WorksheetRules.QuestionsBatch ? "Questionable records" : "";

    private static string BatchPosition(string batch) =>
        "Batch " + (Array.IndexOf(Level3WorksheetRules.Batches, batch) + 1) + " of " + Level3WorksheetRules.Batches.Length;

    /// <summary>The stand's batch card for the current state (or none): check, review, accepted, summary or the totals list.</summary>
    private void RefreshBatchCard(Level3ChapterCheckpoint chapter, bool working, bool animate)
    {
        if (Machine == null) return;
        if (collecting) return;
        if (!working || flow.Step == Level3TableStaging.MachineStep.Loaded || flow.Busy)
        {
            Machine.HideBatchCard(animate: false);
            return;
        }
        if (ledger >= 0)
        {
            Machine.ShowBatchCard(LedgerFace(chapter, ledger), animate);
            return;
        }
        string batch = Level3WorksheetRules.OpenBatch(chapter);
        if (batch == null)
        {
            Machine.HideBatchCard(animate);
            return;
        }
        if (ActiveRow != null || !Level3WorksheetRules.BatchFiled(chapter, batch))
        {
            Machine.HideBatchCard(animate);
            return;
        }
        Machine.ShowBatchCard(CheckFace(chapter, batch), animate);
    }

    /// <summary>Filed, checked with marks, or refiled since the check: the status in words and the one key.</summary>
    private Level3PaperMachineModel.BatchFace CheckFace(Level3ChapterCheckpoint chapter, string batch)
    {
        Level3WorksheetResult result = chapter.worksheetCheck;
        List<string> look = Level3WorksheetCheck.LookAgainIn(chapter, batch);
        bool checkedHere = result != null && result.checks > 0 && result.lastBatch == batch;
        var face = new Level3PaperMachineModel.BatchFace
        {
            Position = BatchPosition(batch), Title = BatchTitle(batch), Key = checkedHere ? "CHECK AGAIN" : "CHECK BATCH",
            KeyLive = !reviewing && !collecting
        };
        int total = Level3WorksheetCheck.DecisionsInBatch(batch).Count;
        if (look.Count > 0)
        {
            int right = total - (result.lastWrong != null ? result.lastWrong.Count : 0);
            face.Status = right + " of " + total + " right · " + look.Count + " marked ?";
            face.Body = LookAgainBody(look[0], repeated: false) + (look.Count > 1 ? "\nTap a ? tab to look again." : "");
        }
        else if (checkedHere)
        {
            face.Status = "Refiled. Check again to confirm.";
            face.Body = "Your first check stays as it was.";
        }
        else
        {
            int filed = Level3WorksheetRules.RowsInBatch(batch).Count;
            face.Status = filed + " of " + filed + " filed";
            face.Body = "Ready to check. A tab brings a record back first.";
        }
        if (!string.IsNullOrEmpty(batchNote)) face.Body = batchNote;
        return face;
    }

    private static Level3PaperMachineModel.BatchFace AcceptedFace(string batch, int total) => new Level3PaperMachineModel.BatchFace
    {
        Position = BatchPosition(batch), Title = BatchTitle(batch), Status = "All " + total + " right · accepted",
        Body = "Its records are read only now. " + Level3PaperMachineModel.WindowPrompt, Key = "", KeyLive = false
    };

    /// <summary>The read-only list behind a total window: what it adds now (accepted rows, then the open batch's filed rows).</summary>
    private Level3PaperMachineModel.BatchFace LedgerFace(Level3ChapterCheckpoint chapter, int window)
    {
        string treatment = window == 0 ? Level3WorksheetRules.Income : Level3WorksheetRules.Deduct;
        Level3WorksheetRules.DraftTotals(chapter, out decimal income, out decimal deductions);
        string open = Level3WorksheetRules.OpenBatch(chapter);
        var lines = new List<string>();
        int count = 0;
        foreach (Level3WorksheetRow row in rows)
        {
            string batch = Level3WorksheetRules.BatchOf(row.RowId);
            bool counted = batch == open || Level3WorksheetRules.BatchAccepted(chapter, batch);
            if (!counted || Level3WorksheetRules.SavedTreatment(chapter.worksheet, row.RowId) != treatment) continue;
            count++;
            if (lines.Count < 4) lines.Add(row.Label + " ₱" + Level3Calculator.Format(row.Amount) + (batch == open ? " · this batch" : ""));
        }
        if (count > lines.Count) lines.Add("and " + (count - lines.Count) + " more");
        if (count == 0) lines.Add("No filed record is counted here yet.");
        return new Level3PaperMachineModel.BatchFace
        {
            Position = "Read only", Title = window == 0 ? "Income total" : "Deductions total",
            Status = Level3WorksheetCheck.Peso(window == 0 ? income : deductions) + " · " + count + (count == 1 ? " record" : " records"),
            Body = string.Join("\n", lines), Key = "CLOSE", KeyLive = true
        };
    }

    /// <summary>One short look-again line for a marked decision: the record, then where to look (the idea after a repeat). Never the answer.</summary>
    private string LookAgainBody(string decisionId, bool repeated)
    {
        string rowId = Level3WorksheetCheck.RowOfDecision(decisionId);
        Level3WorksheetRow row = Level3TableStaging.RowOf(rows, rowId);
        bool again = repeated || (lookAgainSeen.TryGetValue(decisionId, out int seen) && seen >= 2);
        string label = row != null ? row.Label : rowId;
        if (Level3WorksheetCheck.IsReasonDecision(decisionId)) label += " (reason)";
        return label + ": " + Level3WorksheetHints.LookAgain(decisionId, again);
    }

    /// <summary>The note card's look-again line for a recalled record whose last check marked it (null otherwise).</summary>
    private string LookAgainLine(Level3ChapterCheckpoint chapter, string rowId)
    {
        if (chapter == null || rowId == null) return null;
        foreach (string id in new[] { "dest-" + rowId, "reason-" + rowId })
        {
            if (Level3WorksheetCheck.BatchMark(chapter, id) != Level3WorksheetCheck.Mark.LookAgain) continue;
            bool again = lookAgainSeen.TryGetValue(id, out int seen) && seen >= 2;
            return Level3WorksheetHints.LookAgain(id, again);
        }
        return null;
    }

    /// <summary>The batch key: CHECK BATCH / CHECK AGAIN, or CLOSE for the totals list.</summary>
    private void BatchKey()
    {
        if (keyPressedAt >= 0f && Time.unscaledTime - keyPressedAt < KeyRepeatSeconds) return;
        keyPressedAt = Time.unscaledTime;
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        if (chapter == null || flow.Busy || flow.Step == Level3TableStaging.MachineStep.Loaded || reviewing || collecting) return;
        MobileInputGuard.SuppressWorldTapsThisFrame();
        Machine.PressBatchKey();
        Cue("sfx_ui_click_02", 0.45f);
        if (ledger >= 0) { ToggleLedger(ledger); return; }
        if (Level3WorksheetRules.OpenBatch(chapter) == null) return;
        CheckOpenBatch(chapter);
    }

    /// <summary>
    /// CHECK BATCH: one saved command. Only a saved check shows marks; incomplete, unchanged, refused or failed
    /// checks show one plain sentence and keep everything as it was (no buzz, nothing frozen).
    /// </summary>
    private void CheckOpenBatch(Level3ChapterCheckpoint chapter)
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        string batch = Level3WorksheetRules.OpenBatch(chapter);
        if (owner == null || batch == null) return;
        PutOriginalsAway();
        Level3Checkpointer.CheckCommit result = owner.CheckBatch(batch, chapter.worksheet != null ? chapter.worksheet.revision : 0,
            out Level3BatchEvaluation evaluation, out string hint);
        if (result != Level3Checkpointer.CheckCommit.Checked || evaluation == null)
        {
            batchNote = string.IsNullOrEmpty(hint) ? "Couldn't check that. Try again." : hint;
            if (result == Level3Checkpointer.CheckCommit.SaveFailed) Level3Say(batchNote);
            RefreshStand(animate: false);
            return;
        }
        batchNote = null;
        foreach (string id in evaluation.WrongIds()) lookAgainSeen[id] = (lookAgainSeen.TryGetValue(id, out int n) ? n : 0) + 1;
        if (evaluation.Accepted)
        {
            GameplayEvents.RaiseGuideCue("MachineBatchAccepted");
            acceptRoutine = StartCoroutine(AcceptSequence(batch, evaluation.Decisions.Count, evaluation.Ready));
            return;
        }
        GameplayEvents.RaiseGuideCue("MachineBatchChecked");
        Cue("sfx_paper_place_01", 0.4f);
        marksPop = true;
        RefreshStand(animate: false);
    }

    /// <summary>
    /// A saved acceptance: the ticks pop in, a short hold, the tabs gather into the rack; then the next batch's
    /// first record arrives (or, after the last batch, the summary comes out). Any touch or interruption finishes it.
    /// </summary>
    private IEnumerator AcceptSequence(string batch, int total, bool ready)
    {
        reviewing = true;
        Machine.ShowBatchCard(AcceptedFace(batch, total), animate: false);
        marksPop = true;
        RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
        // The machine's own accepting clunk (never a filing cue: filings stay identical for every choice).
        Cue("sfx_stamp_press_01", 0.3f);
        float hold = WorldEmote.ReducedMotion ? 0f
            : Level3PaperMachineModel.MarkSeconds + Level3PaperMachineModel.MarkStagger * 2f + AcceptHoldSeconds;
        float until = Time.unscaledTime + hold;
        while (Time.unscaledTime < until) yield return null;
        bool gathered = false;
        Machine.GatherTabs(() => gathered = true);
        while (!gathered) yield return null;
        acceptRoutine = null;
        reviewing = false;
        // The next batch takes the stand; after the final acceptance the real summary takes its place.
        if (!ready) Machine.HideBatchCard(animate: true);
        RefreshStand(animate: true);
    }

    /// <summary>Settles an accepted batch's presentation at once (the acceptance is already saved).</summary>
    private void FinishReview()
    {
        if (acceptRoutine != null)
        {
            StopCoroutine(acceptRoutine);
            acceptRoutine = null;
        }
        if (!reviewing) return;
        reviewing = false;
        Machine?.Settle();
        if (Built) RefreshStand(animate: false);
    }

    /// <summary>A total window: its read-only list on the stand (tap again, or CLOSE, to put it away).</summary>
    private void ToggleLedger(int window)
    {
        if (SummaryAvailable)
        {
            summaryDocument.Open(details: true);
            return;
        }
        if (flow.Busy || flow.Step == Level3TableStaging.MachineStep.Loaded || window < 0 || window > 1) return;
        MobileInputGuard.SuppressWorldTapsThisFrame();
        PutOriginalsAway();
        bool closing = ledger == window;
        ledger = closing ? -1 : window;
        batchNote = null;
        Machine.HideBatchCard(animate: false);
        AudioManager.Instance?.PlayDocumentSFX(closing);
        RefreshStand(animate: true);
    }

    // ---------- one physical summary on the stand / in inventory ----------

    private void RefreshSummary(Level3ChapterCheckpoint chapter, bool working)
    {
        if (Machine == null) return;
        Machine.HideSummary();
        bool ready = !reviewing && Level3ChapterRules.PhaseOf(chapter) == Level3Phase.WorksheetReady;
        if (summaryDocument == null && (ready || Level3WorksheetCheck.Collected(chapter)))
        {
            CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
            if (definition == null || definition.caseId != Level3ChapterRules.BeaCaseId)
            {
                TextAsset asset = Resources.Load<TextAsset>("Levels/Level_03/cases/case_bea_001");
                if (asset != null) definition = JsonUtility.FromJson<CaseDefinition>(asset.text);
            }
            ClientRecord record = Level3WorksheetCheck.SummaryRecord(definition, chapter);
            if (record == null) return;
            summaryDocument = gameObject.AddComponent<Level3SummaryDocument>();
            summaryDocument.Bind(this, Machine.StandDocumentAnchor, workspace, record, StartCollect);
        }
        summaryDocument?.Sync(ready);
    }

    private void OpenSummary()
    {
        if (summaryDocument != null && summaryDocument.Open()) GameplayEvents.RaiseGuideCue("MachineSummaryRead");
    }

    private void StartCollect(Level3WorksheetGrants grants)
    {
        collecting = true;
        awardShown = false;
        rewardAt = -1f;
        collectGrants = grants;
        collectOrigin = Machine.Card != null ? Machine.Card.position : transform.position;
        ledger = -1;
        Machine.HideBatchCard(animate: false);
        RefreshMachine(Level3Checkpointer.Active?.State, animate: false);
        LandCollect();
    }

    /// <summary>The summary is in the Case Folder: the saved result and rewards announce themselves, exactly once.</summary>
    private void LandCollect()
    {
        if (awardShown) return;
        awardShown = true;
        Level3WorksheetResult result = Level3Checkpointer.Active?.State?.worksheetCheck;
        int wrong = result?.firstWrong != null ? result.firstWrong.Count : 0;
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        if (progression != null && progression.Score != null) progression.Score.AddSideQuest(wrong, collectOrigin, (BadgeTier)collectGrants.Tier);
        // Public Trust and coins: only what the save actually applied, with their named reasons.
        HudFeedback.AnnounceCampaign(collectGrants.Trust, collectGrants.Coins, collectOrigin);
        if (HudHotbar.Instance != null) HudHotbar.Instance.Notice("Bea's summary is stored in your inventory.");
        rewardAt = Time.unscaledTime + (WorldEmote.ReducedMotion ? 0f : RewardDelaySeconds);
    }

    /// <summary>The one reward card, once nothing else is in front (settings, a conversation).</summary>
    private void TickCollect()
    {
        if (!collecting || !awardShown || rewardAt < 0f || Time.unscaledTime < rewardAt) return;
        if (InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen) return;
        ShowRewardCard();
    }

    private void ShowRewardCard()
    {
        rewardAt = -1f;
        Level3WorksheetResult result = Level3Checkpointer.Active?.State?.worksheetCheck;
        bool legacy = Level3WorksheetCheck.LegacyGraded(Level3Checkpointer.Active?.State);
        LevelRewardRules rules = CaseProgressionManager.Instance != null && CaseProgressionManager.Instance.Score != null
            ? CaseProgressionManager.Instance.Score.Rules : LevelRewardRules.Load(Level3ChapterRules.LevelId);
        var tier = (BadgeTier)collectGrants.Tier;
        var data = new RewardScreenData
        {
            Title = "Side Quest Complete",
            Subtitle = rules != null && !string.IsNullOrEmpty(rules.sideQuest.badgeName) ? rules.sideQuest.badgeName : "Worth the Tax?",
            Exp = collectGrants.Xp,
            MaxExp = Level3WorksheetCheck.MaxXp,
            Summary = "Bea's summary keeps what her records support: her income, her backed costs and the two left out. Her computation is next, at the office.",
            MedalLabel = tier + " badge",
            Accent = HudBadgeArt.TierColor(tier)
        };
        Sprite medal = collectGrants.Tier > 0 && rules != null ? HudBadgeArt.For(rules, tier) : null;
        if (medal != null) data.Medals.Add(medal);
        if (result != null) data.Fact(legacy ? "First check" : "First checks", result.firstCorrect + "/" + result.firstTotal);
        data.Fact("Badge", tier.ToString());
        if (tier != BadgeTier.Gold) data.Footer = legacy ? "Gold needs 12/12 on the first check." : "Gold needs 9/9 on the first checks.";
        if (collectGrants.Trust.Applied)
            data.RewardTile("Public Trust", HudTheme.Heart, HudTheme.Reputation, collectGrants.Trust.Amount, "Trust", collectGrants.Trust.After, CampaignRecord.MaxTrust);
        if (collectGrants.Coins.Applied)
            data.RewardTile("Coins", HudTheme.Coin, HudTheme.CoinGold, collectGrants.Coins.Amount, "Wallet", collectGrants.Coins.After);
        Action afterContinue = () => { if (isActiveAndEnabled) StartCoroutine(LeaveTableAfterCard()); };
        collecting = false;
        if (SideQuestCompleteUI.Instance != null) SideQuestCompleteUI.Instance.ShowScreen(data, afterContinue);
        else afterContinue();
        if (Built) RefreshStand(animate: false);
    }

    /// <summary>After Continue: once the card and flights are done, the player stands up (Bea heads to the office).</summary>
    private IEnumerator LeaveTableAfterCard()
    {
        yield return null;
        for (float waited = 0f; waited < 8f; waited += Time.unscaledDeltaTime)
        {
            if (!HudRewardScreen.AnyOpen && !SideQuestSplash.IsShowing) break;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.25f);
        CameraController camera = CameraController.Instance;
        if (Seated && camera != null && camera.CurrentMode == CameraController.CameraMode.Workstation) camera.ExitFirstPerson();
    }

    /// <summary>Leaving, disabling or an interruption during the collection: its presentation finishes at once (the save already happened), once.</summary>
    private void FinishCollectNow()
    {
        if (!collecting) return;
        Machine?.Settle();
        if (!awardShown) LandCollect();
        if (rewardAt >= 0f && !InGameSettings.IsOpen) ShowRewardCard();
    }

    /// <summary>COMPARE: the loaded record's original beside the other original of the same transaction (B-PRINT with B-EXP), in the one reader.</summary>
    private void CompareOriginals()
    {
        string own = ActiveRow?.SourceId;
        string partner = ActiveRow != null ? Level3WorksheetRules.ComparePartner(rows, ActiveRow.RowId) : null;
        if (!Seated || own == null || partner == null || stack == null ||
            !bySource.TryGetValue(own, out ClientRecordPaper first) || !bySource.TryGetValue(partner, out ClientRecordPaper second)) return;
        if (compareFirst == own && compareSecond == partner)
        {
            // A second tap closes the pair.
            if (first.IsSettledOut) first.PutBack();
            if (second.IsSettledOut) second.PutBack();
            return;
        }
        Machine.CloseWhy(animate: false);
        PutOriginalsAway();
        if (!stack.OpenPair(own, partner, out _)) return;
        compareFirst = own;
        compareSecond = partner;
        MobileInputGuard.SuppressWorldTapsThisFrame();
        first.TakeOut();
        second.TakeOut();
        Restack();
    }

    /// <summary>Every original back on the pile (a compare pair too), without touching the machine.</summary>
    private void PutOriginalsAway()
    {
        compareFirst = compareSecond = null;
        stackedRow = null;
        foreach (ClientRecordPaper paper in papers)
            if (paper != null && !paper.IsAttached) paper.Gather();
        outOrder.Clear();
        stack?.CloseAll();
        Restack();
    }

    /// <summary>A short existing cue (AudioManager's family debounce keeps repeats from stacking; sound settings apply).</summary>
    public static void Cue(string clip, float volume)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect(clip, volume);
    }

    private void CancelDrags()
    {
        foreach (ClientRecordPaper paper in papers)
            if (paper != null) paper.CancelDrag();
        dragStarting = null;
        // Machine: a card/lever/dial drag ends undecided (the card goes back where it was) and every motion settles,
        // a feed included (its record is already saved). A half-fed record keeps its pending choices.
        // T06: an accepted batch's gather, the summary's arrival and a collected summary's flight finish too.
        Machine?.Settle();
        FinishReview();
    }

    // =========================================================
    // SEAT
    // =========================================================

    /// <summary>
    /// Sits the player at the table (after the handover). False before it, with a plain line. T04 buildup: the
    /// seat opens once Bea has offered her organizer (or on any restore); the first sit after her offer presents
    /// the stand's card once the camera has settled.
    /// </summary>
    public bool TrySit(Pose? fromView = null)
    {
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        if (chapter == null || Level3ChapterRules.PhaseOf(chapter) < Level3Phase.WorksheetDraft || !papersShown)
        {
            Level3Say("Talk to " + Content.speakerName + " first. Her records are still in her envelope.");
            return false;
        }
        if (!encounter.Done || ReadingStanding || introSplash != null || SideQuestSplash.IsShowing) return false;
        if (CameraController.Instance == null || Viewpoint == null || CameraController.Instance.CurrentMode != CameraController.CameraMode.FirstPerson) return false;
        return SitAtTable(fromView);
    }

    /// <summary>
    /// The shared title card rises in the same frame as the camera move toward the table (both entry paths).
    /// Its acknowledgment is saved once; input and guidance wait for BOTH that and the settled camera.
    /// </summary>
    private void BeginIntroSplash()
    {
        SideQuestSplash splash = SideQuestSplash.GetOrCreateForScene(gameObject.scene);
        var content = new SideQuestSplash.SplashContent
        {
            kicker = "SIDE QUEST 3",
            title = "The Numbers Tell a Story",
            description = "Help Bea sort her records into income, deductions and items to leave out.",
            instruction = "Watch Arthur, then try it  ·  Then sort Bea's records",
            button = "TAP TO BEGIN"
        };
        if (splash != null && splash.Show(content, IntroAcknowledged, withCamera: true)) { introSplash = splash; return; }
        // No card can show: nothing is saved and the table simply opens when the camera arrives.
        introRoutine = StartCoroutine(OpenAfterIntro());
    }

    private void IntroAcknowledged()
    {
        introSplash = null;
        var result = Level3Checkpointer.Active.CommitComputation(Level3TableStaging.AcknowledgeIntro, out string hint);
        if (result == Level3Checkpointer.TapeCommit.Attached || result == Level3Checkpointer.TapeCommit.AlreadyAttached)
        {
            // The same paper cue Mark the Money! plays as its card clears.
            if (result == Level3Checkpointer.TapeCommit.Attached) AudioManager.Instance?.PlayPaperSFX();
            introRoutine = StartCoroutine(OpenAfterIntro());
            return;
        }
        // Not saved: the same card asks again; the camera stays where it is.
        Level3Say(hint);
        BeginIntroSplash();
    }

    /// <summary>A quick acknowledgment never exposes the table while the camera is still moving.</summary>
    private IEnumerator OpenAfterIntro()
    {
        while (Seated && CameraController.Instance != null &&
            (CameraController.Instance.IsTransitioning || CameraController.Instance.InputHeld || InGameSettings.IsOpen ||
             DialogueUI.IsAnyDialogueActive || SideQuestSplash.IsShowing || Time.timeScale <= 0f)) yield return null;
        introRoutine = null;
        introPending = false;
        if (Seated && CameraController.Instance != null && CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation)
            GameplayEvents.RaiseGuideCue("BeaTableSeated");
    }

    private void CancelIntro()
    {
        if (introRoutine != null) { StopCoroutine(introRoutine); introRoutine = null; }
        if (introSplash != null) { introSplash.Cancel(); introSplash = null; }
        introPending = false;
    }

    private bool SitAtTable(Pose? fromView)
    {
        if (!isActiveAndEnabled || ReadingStanding || CameraController.Instance == null || Viewpoint == null ||
            CameraController.Instance.CurrentMode != CameraController.CameraMode.FirstPerson) return false;
        introPending = Level3TableStaging.IntroDue(Level3Checkpointer.Active?.State);
        presentCard = encounter.Step != Level3TableStaging.EncounterStep.Ready;
        encounter.Accept();
        // Help Bea leaves the conversation's shot directly for the table: no visible return to first person between them.
        if (fromView.HasValue && CameraController.Instance.MainCamera != null)
            CameraController.Instance.MainCamera.transform.SetPositionAndRotation(fromView.Value.position, fromView.Value.rotation);
        CameraController.Instance.EnterFirstPerson(Viewpoint, "Leave Table");
        Seated = true;
        SetPapersInteractive(!introPending);
        foreach (ClientRecordPaper paper in papers)
            if (paper != null) paper.SetEventCamera(ViewCamera);
        if (introPending) BeginIntroSplash();
        else GameplayEvents.RaiseGuideCue("BeaTableSeated");
        return true;
    }

    private void HandleLeftSeat()
    {
        if (!Seated) return;
        CancelIntro();
        GatherAll();
        Seated = false;
        summaryDocument?.CloseForTravel();
        presentCard = false;
        SetPapersInteractive(false);
        SetTouch(false);
    }

    /// <summary>Every original straight back onto the pile (nothing is lost or copied; the pile order stays); any presentation motion ends at its pose and a standing read releases its hold.</summary>
    private void GatherAll()
    {
        StopPracticeDemo();
        if (restacking != null) { StopCoroutine(restacking); restacking = null; }
        // T06: a saved collection's presentation finishes first (once), then everything settles.
        FinishCollectNow();
        foreach (ClientRecordPaper paper in papers)
            if (paper != null && !paper.IsAttached) paper.Gather();
        outOrder.Clear();
        stack?.CloseAll();
        compareFirst = compareSecond = null;
        dragStarting = null;
        EndStandingRead();
        SettleMotion();
        Machine?.Settle();
        FinishReview();
        bool hadLedger = ledger >= 0;
        ledger = -1;
        batchNote = null;
        if (hadLedger && Built) RefreshStand(animate: false);
        // T05: an unfiled half-fed record returns to the stand (nothing was saved); a feed has already settled.
        if (flow.Step != Level3TableStaging.MachineStep.Empty && flow.Step != Level3TableStaging.MachineStep.Ready)
        {
            flow.Reset();
            settleTab = null;
            Machine?.Rest();
            if (Built) RefreshStand(animate: false);
        }
        else if (flow.Armed)
        {
            flow.ToggleArm();
            Machine?.Arm(false);
        }
    }

    public static void Level3Say(string text) => Level3Say(text, null, 4f);

    /// <summary>Arthur says it in his bubble (the Level 2 Level2Say seam); without the guide, the HUD toast.</summary>
    public static void Level3Say(string text, string mood, float seconds)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!GuideVoice.TrySay(text, mood, seconds)) LevelGate.NotifyBlocked(text);
    }

    // =========================================================
    // IClientRecordHolder
    // =========================================================

    public Camera ViewCamera => HeldOnScreen.ActiveCamera(Camera.main);

    /// <summary>The single reader: centred over the table at a readable size (the stand and machine stay where they are).</summary>
    public void ReadingPose(ClientRecordPaper paper, out Vector3 position, out Quaternion rotation, out float worldScale)
    {
        Camera camera = ViewCamera;
        bool card = paper.Record.IsCard;
        Rect pixels = camera.pixelRect;
        string source = paper.Record.source;
        if (compareFirst != null && (source == compareFirst || source == compareSecond))
        {
            // COMPARE: the two originals side by side at the reader's depth (left: the loaded record's own).
            float side = source == compareFirst ? 0.27f : 0.73f;
            float compareDepth = Mathf.Max(camera.nearClipPlane + 0.04f, ReadDepth);
            position = camera.ScreenToWorldPoint(new Vector3(pixels.x + pixels.width * side, pixels.y + pixels.height * 0.46f, compareDepth));
            rotation = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
            float h = 2f * compareDepth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float w = h * Mathf.Max(0.1f, camera.aspect);
            worldScale = Mathf.Min(h * ReadSheetHeight / paper.Size.y, w * 0.44f / paper.Size.x);
            return;
        }
        var screen = new Vector2(pixels.x + pixels.width * 0.5f, pixels.y + pixels.height * (card ? 0.51f : 0.46f));
        float depth = Mathf.Max(camera.nearClipPlane + 0.04f, ReadDepth);
        position = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        rotation = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
        float visibleHeight = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float visibleWidth = visibleHeight * Mathf.Max(0.1f, camera.aspect);
        worldScale = Mathf.Min(visibleHeight * (card ? ReadCardHeight : ReadSheetHeight) / paper.Size.y, visibleWidth * ReadWidth / paper.Size.x);
    }

    /// <summary>Lift the exposed original or compare pair without changing its resting stack order.</summary>
    public void Extracted(ClientRecordPaper paper)
    {
        string source = paper?.Record?.source;
        if (!originalHintShown)
        {
            originalHintShown = true;
            Level3Say("Originals stay on this table. Drag the paper back onto the stack when you finish reading.");
        }
        if (compareFirst != null && (source == compareFirst || source == compareSecond))
        {
            // Either exposed original may be lifted independently; both use the same reader and drag behavior.
            stack?.OpenPair(compareFirst, compareSecond, out _);
            paper.SetEventCamera(ViewCamera);
            paper.BringWindowToFront();
            return;
        }
        compareFirst = compareSecond = null;
        if (stack != null && stack.Open(source, out string closing) && closing != null &&
            bySource.TryGetValue(closing, out ClientRecordPaper previous) && previous != null && previous != paper)
        {
            if (previous.IsSettledOut) previous.PutBack();
            else previous.Gather();
        }
        foreach (ClientRecordPaper other in papers)
            if (other != null && other != paper && (other.IsSettledOut || other.IsOut)) other.Gather();
        Restack();
        outOrder.Remove(paper);
        outOrder.Add(paper);
        paper.SetEventCamera(ViewCamera);
        paper.BringWindowToFront();
    }

    public void PaperSettled(ClientRecordPaper paper)
    {
        if (paper?.Record == null) return;
        LastReadSource = paper.Record.source;
        encounter.Shown(paper.Record.source);
        GameplayEvents.RaiseGuideCue("BeaRecordRead");
    }

    public void PaperLanded(ClientRecordPaper paper)
    {
        outOrder.Remove(paper);
        string source = paper?.Record?.source;
        stack?.Close(source);
        if (ReadingStanding && source == encounter.Featured) EndStandingRead();
        // T04 buildup: the featured record read and back on the pile: her organizer offer follows shortly.
        if (encounter.Returned(source)) offerAt = Time.unscaledTime + OfferDelay;
        Restack();
    }

    public bool WantsReturn(ClientRecordPaper paper, Vector2 screen, Action ready)
    {
        Camera camera = ViewCamera;
        if (camera == null || tableCollider == null) return false;
        if (HudSlotDrop.IsOver(screen)) { Level3Say("These originals stay on Bea's table. Return them to the stack."); return false; }
        Matrix4x4 plane = Matrix4x4.TRS(papersRoot.position, papersRoot.rotation * Quaternion.Euler(90f, 0f, 0f), papersRoot.lossyScale).inverse;
        if (!Level3TablePaperRaycaster.HitRect(camera.ScreenPointToRay(screen), plane,
            new Rect(-Layout.PaperWidth * .65f, -Layout.PaperWidth * .85f, Layout.PaperWidth * 1.3f, Layout.PaperWidth * 1.7f), out _)) return false;
        stack?.Close(paper.Record.source);
        Restack();
        ready?.Invoke();
        return true;
    }

    /// <summary>
    /// A touch on the original that is out. Starting a drag only brings it
    /// forward; a tap puts a sheet back (an ID card turns to its profile first,
    /// then the next tap puts it back).
    /// </summary>
    public bool BroughtToFront(ClientRecordPaper paper)
    {
        paper.BringWindowToFront();
        LastReadSource = paper.Record?.source;
        if (dragStarting == paper)
        {
            dragStarting = null;
            return true;
        }
        return true;
    }

    public void PaperFlipped(ClientRecordPaper paper, bool backUp) { }
    public void PointerDown(ClientRecordPaper paper, PointerEventData eventData) => dragStarting = null;
    public void PointerUp(ClientRecordPaper paper, PointerEventData eventData) { }
    public void PointerMoved(ClientRecordPaper paper) => dragStarting = paper;
    public bool IsGrouped(ClientRecordPaper paper) => false;
    public void BeginGroupDrag(ClientRecordPaper paper, PointerEventData eventData) { }
    public void DragGroup(PointerEventData eventData) { }
    public void EndGroupDrag(PointerEventData eventData) { }
}
