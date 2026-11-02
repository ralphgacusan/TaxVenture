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
/// Level3Checkpointer.CommitBeaRecords) her nine papers fan out of it onto the
/// mat as the existing physical records (ClientRecordPaper: ID card, background
/// notes, income/payment/agreement sheets, plain working papers), printed by
/// Level3RecordRules from the case content. B-WHT is never among them: Bea
/// hands it over at the office credit step (T08, Level3CertificatePaper).
///
/// T08: once her worksheet is collected she walks to the office conference
/// room (Level3BeaOfficeVisit on her actor) and waits there.
///
/// READING: tap the table to sit (the same desk view as the office,
/// CameraController.EnterFirstPerson, "Leave Table"); tap a paper to lift it
/// into a readable pose, tap/drop it on the table to put it back. Papers only
/// take touches while seated; leaving gathers them. This class is their
/// IClientRecordHolder; it remembers the record read last (Bea's questions
/// start from it). Nothing here grades, awards or saves.
///
/// WORKSHEET (T05): the originals lie in one row on the far half of the mat;
/// Bea's worksheet board (Level3WorksheetDesk) rests on the near half. While
/// the board is up her papers are gathered and take no touches (one input
/// owner); READ ORIGINAL puts the board down and lifts that paper.
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
    private const float PaperLayerDepth = 0.0004f;
    private static readonly float[] Radii = { 1.8f, 2.4f, 3.0f, 3.6f, 4.2f };
    private const float AngleStep = 30f;
    private const float FanSeconds = 0.45f;
    /// <summary>The far share of the mat holding the originals; the near rest holds the worksheet.</summary>
    private const float PapersBand = 0.42f;

    public static Level3BeaTable Instance { get; private set; }

    public bool Built { get; private set; }
    /// <summary>The player is sitting at the table (papers take touches).</summary>
    public bool Seated { get; private set; }
    public Level3BeaInteractable Bea { get; private set; }
    public Level3ClientContent Content { get; private set; }
    public Transform Viewpoint { get; private set; }
    /// <summary>The source id of the record read last (null before any).</summary>
    public string LastReadSource { get; private set; }
    /// <summary>Bea's worksheet on the near half of the mat (T05).</summary>
    public Level3WorksheetDesk Worksheet { get; private set; }
    /// <summary>T08: her walk to the office conference room.</summary>
    public Level3BeaOfficeVisit Visit { get; private set; }

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
    private readonly List<Vector3> slotRest = new List<Vector3>();
    private bool papersShown;
    private bool worksheetUp;

    private void Awake() => Instance = this;

    private void OnEnable() => CameraController.WorkstationExited += HandleLeftSeat;

    private void OnDisable()
    {
        CameraController.WorkstationExited -= HandleLeftSeat;
        GatherAll();
        Seated = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (workspace != null) Destroy(workspace.gameObject);
        props?.Destroy();
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

        Vector3 spot = ChooseSpot(center, footprint, doors, out Vector3 front, out bool free);
        tableRoot.SetPositionAndRotation(spot, Quaternion.LookRotation(-front, Vector3.up));
        topY = spot.y + TableTop;
        Debug.Log($"[Level3] Bea's worktable at {spot} facing {front} ({(free ? "free spot" : "no free spot found; nearest ring spot used")}).");

        BuildMatAndEnvelope(footprint);
        BuildViewpoint(footprint);
        BuildSeatTarget(footprint);
        BuildBea(assets, spot, front, footprint);
        BuildWorksheet();
        Built = true;
        SyncPapers(animate: false);
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
        bea.introduction.Add(new Level3TownLine { text = "Hi again, or properly hello! I'm Beatriz, Bea. I design logos and menus for shops around town." });
        bea.introduction.Add(new Level3TownLine { text = "I kept the receipts this time. That means I can subtract all of them, right?" });
        bea.introduction.Add(new Level3TownLine { speaker = "Player", text = "Let's look at what each record supports." });
        bea.introduction.Add(new Level3TownLine { text = "I'll spread my records out on my table. Sit down and read them, and ask me anything." });
        bea.inquiries.Add(new Level3Inquiry { id = "purpose", question = "What was this purchase for?", answer = "My birthday dinner and decorations. The studio wasn't involved.", contextSources = new List<string> { "B-PERSONAL", "B-DRAFT" } });
        bea.inquiries.Add(new Level3Inquiry { id = "invoice", question = "Does this invoice belong to the annual total?", answer = "Yes, that supplier bill is in the printing summary.", contextSources = new List<string> { "B-PRINT", "B-EXP" } });
        bea.inquiries.Add(new Level3Inquiry { id = "period", question = "Show me the period these records cover.", answer = "These are the records for the whole year shown on the sheets." });
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
        // A kraft work mat: the flat working surface the papers lie on (and T05's worksheet later).
        matSize = new Vector2(Mathf.Min(1.0f, footprint.size.x * 0.82f), Mathf.Min(0.62f, footprint.size.z * 0.78f));
        props.Box("Work Mat", tableRoot, new Vector3(0f, TableTop + 0.0015f, 0f), new Vector3(matSize.x, 0.003f, matSize.y), new Color32(196, 164, 118, 255));
        props.Box("Mat Edge", tableRoot, new Vector3(0f, TableTop + 0.0012f, 0f), new Vector3(matSize.x + 0.012f, 0.0024f, matSize.y + 0.012f), new Color32(120, 92, 60, 255));

        papersRoot = new GameObject("Papers").transform;
        papersRoot.SetParent(tableRoot, false);
        papersRoot.localPosition = new Vector3(0f, TableTop + 0.003f, 0f);
        workspace = new GameObject("Bea Papers Out").transform;
        SceneManager.MoveGameObjectToScene(workspace.gameObject, gameObject.scene);

        envelope = new GameObject("Envelope").transform;
        envelope.SetParent(tableRoot, false);
        envelope.localPosition = new Vector3(matSize.x * 0.28f, TableTop + 0.01f, matSize.y * 0.18f);
        envelope.localRotation = Quaternion.Euler(0f, -8f, 0f);
        var manila = new Color32(222, 186, 120, 255);
        props.Box("Body", envelope, Vector3.zero, new Vector3(0.3f, 0.016f, 0.22f), manila);
        envelopeFlap = new GameObject("Flap Hinge").transform;
        envelopeFlap.SetParent(envelope, false);
        envelopeFlap.localPosition = new Vector3(0f, 0.009f, 0.11f);
        props.Box("Flap", envelopeFlap, new Vector3(0f, 0f, -0.05f), new Vector3(0.3f, 0.004f, 0.1f), manila);
        // Printed toward the player (the envelope reads from the table's near side).
        props.Print("Label", envelope, new Vector3(0f, 0.0085f, -0.03f), 0.24f, 0.07f, Content.envelopeLabel, new Color32(40, 34, 30, 255), bold: true, wrap: true);
    }

    /// <summary>The worksheet's rest on the near half of the mat, fitted inside it (T05).</summary>
    private void BuildWorksheet()
    {
        float nearDepth = matSize.y * (1f - PapersBand);
        float scale = Mathf.Min(1f, matSize.x * 0.94f / Level3WorksheetModel.BoardWidth, nearDepth * 0.94f / Level3WorksheetModel.BoardDepth);
        var rest = new Vector3(0f, TableTop + 0.004f, -matSize.y * 0.5f + nearDepth * 0.5f);
        Worksheet = gameObject.AddComponent<Level3WorksheetDesk>();
        Worksheet.Init(this, tableRoot, rest, scale, matSize);
    }

    private void BuildViewpoint(Bounds footprint)
    {
        // Seated on the near side, looking down at the mat (the office desk view's angle).
        var seat = new GameObject("TableViewpoint").transform;
        seat.SetParent(tableRoot, false);
        float back = footprint.extents.z + 0.18f;
        seat.localPosition = new Vector3(0f, TableTop + 0.66f, -back);
        Vector3 look = new Vector3(0f, TableTop, -matSize.y * 0.05f) - seat.localPosition;
        seat.localRotation = Quaternion.LookRotation(look, Vector3.up);
        Viewpoint = seat;
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
        target.AddComponent<Level3BeaTableSeat>().Init(this);
    }

    private void BuildBea(Level3SceneAssets assets, Vector3 spot, Vector3 front, Bounds footprint)
    {
        var holder = new GameObject(Content.speakerName);
        holder.layer = InteractLayer;
        holder.transform.SetParent(transform, false);
        Vector3 at = spot - front * (footprint.extents.z + 0.38f);
        at.y = Ground(at, spot.y, out float y) ? y : spot.y;
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
    // PAPERS
    // =========================================================

    /// <summary>Shows the acquired papers (quiet on resume; fanned out of the envelope right after the handover).</summary>
    public void SyncPapers(bool animate)
    {
        if (!Built || papersShown) return;
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (chapter == null || definition == null || definition.caseId != Level3ChapterRules.BeaCaseId) return;
        List<string> acquired = chapter.Case(Level3ChapterRules.BeaCaseId)?.acquiredSourceIds;
        if (acquired == null || acquired.Count == 0) return;
        // Her table holds what she spread out there; the certificate came later, at the office (T08).
        List<string> onTable = acquired.FindAll(id => id != Level3ChapterRules.BeaCreditSource);
        List<ClientRecord> records = Level3RecordRules.Build(definition, onTable);
        if (records.Count == 0) return;
        papersShown = true;
        LayOut(records);
        envelopeFlap.localRotation = Quaternion.Euler(-150f, 0f, 0f);
        // The envelope stays for the fan-out (it then hides); a quiet restore shows the papers only.
        envelope.gameObject.SetActive(animate);
        if (animate) StartCoroutine(FanOut());
        SetPapersInteractive(Seated && !worksheetUp);
    }

    /// <summary>One row across the far band of the mat (T05: the near band holds the worksheet), in handover order.</summary>
    private void LayOut(List<ClientRecord> records)
    {
        float cellW = matSize.x / Mathf.Max(1, records.Count);
        float cellD = matSize.y * PapersBand;
        for (int i = 0; i < records.Count; i++)
        {
            ClientRecord record = records[i];
            Vector2 units = SupportingDocumentFace.SizeOf(record.type);
            float aspect = units.y / units.x;
            float width = Mathf.Min(cellW * 0.9f, (cellD * 0.9f) / aspect);
            var size = new Vector2(width, width * aspect);
            var slot = new GameObject("Slot " + record.source).transform;
            slot.gameObject.layer = gameObject.layer;
            slot.SetParent(papersRoot, false);
            Vector3 rest = new Vector3(-matSize.x * 0.5f + cellW * (i + 0.5f), PaperLayerDepth * (i + 1), matSize.y * 0.5f - cellD * 0.5f);
            slot.localPosition = rest;
            slot.localRotation = Quaternion.Euler(0f, (i % 3 - 1) * 2.5f, 0f);
            slotRest.Add(rest);
            papers.Add(ClientRecordPaper.Create(this, slot, workspace, record, size, gameObject.layer));
        }
    }

    private IEnumerator FanOut()
    {
        Vector3 from = papersRoot.InverseTransformPoint(envelope.position);
        var slots = new List<Transform>();
        foreach (ClientRecordPaper paper in papers) slots.Add(paper.transform.parent);
        for (int i = 0; i < slots.Count; i++) slots[i].localPosition = from;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX(false);
        float start = Time.unscaledTime;
        while (true)
        {
            bool done = true;
            for (int i = 0; i < slots.Count; i++)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - start - i * 0.06f) / FanSeconds);
                float eased = 1f - (1f - t) * (1f - t);
                slots[i].localPosition = Vector3.Lerp(from, slotRest[i], eased) + Vector3.up * (0.05f * Mathf.Sin(t * Mathf.PI));
                done &= t >= 1f;
            }
            if (done) break;
            yield return null;
        }
        if (envelope != null) envelope.gameObject.SetActive(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_place_01", 0.6f);
    }

    /// <summary>T05: the worksheet is up (papers gathered, no touches) or down again.</summary>
    public void HoldPapers(bool up)
    {
        worksheetUp = up;
        if (up) GatherAll();
        SetPapersInteractive(Seated && !up);
    }

    /// <summary>T05 READ ORIGINAL: lifts that source's original paper into its reading pose (front of the others if already out).</summary>
    public bool ReadOriginal(string sourceId)
    {
        if (!Seated || worksheetUp) return false;
        foreach (ClientRecordPaper paper in papers)
        {
            if (paper == null || paper.Record == null || paper.Record.source != sourceId) continue;
            if (paper.IsAttached) paper.TakeOut();
            else BroughtToFront(paper);
            return true;
        }
        return false;
    }

    private void SetPapersInteractive(bool on)
    {
        if (papersRoot == null) return;
        foreach (GraphicRaycaster raycaster in papersRoot.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = on;
        if (workspace != null)
            foreach (GraphicRaycaster raycaster in workspace.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = on;
    }

    // =========================================================
    // SEAT
    // =========================================================

    /// <summary>Sits the player at the table (after the handover). False before it, with a plain line.</summary>
    public bool TrySit()
    {
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        if (chapter == null || Level3ChapterRules.PhaseOf(chapter) < Level3Phase.WorksheetDraft || !papersShown)
        {
            Level3Say("Talk to " + Content.speakerName + " first. Her records are still in her envelope.");
            return false;
        }
        if (CameraController.Instance == null || Viewpoint == null || CameraController.Instance.CurrentMode != CameraController.CameraMode.FirstPerson) return false;
        CameraController.Instance.EnterFirstPerson(Viewpoint, "Leave Table");
        Seated = true;
        SetPapersInteractive(!worksheetUp);
        GameplayEvents.RaiseGuideCue("BeaTableSeated");
        return true;
    }

    private void HandleLeftSeat()
    {
        if (!Seated) return;
        GatherAll();
        Seated = false;
        SetPapersInteractive(false);
    }

    private void GatherAll()
    {
        foreach (ClientRecordPaper paper in papers)
            if (paper != null && !paper.IsAttached) paper.Gather();
        outOrder.Clear();
    }

    public static void Level3Say(string text)
    {
        if (!GuideVoice.TrySay(text, null, 4f)) LevelGate.NotifyBlocked(text);
    }

    // =========================================================
    // IClientRecordHolder
    // =========================================================

    public Camera ViewCamera => HeldOnScreen.ActiveCamera(Camera.main);

    public void ReadingPose(ClientRecordPaper paper, out Vector3 position, out Quaternion rotation, out float worldScale)
    {
        Camera camera = ViewCamera;
        bool card = paper.Record.IsCard;
        int rank = outOrder.Contains(paper) ? outOrder.IndexOf(paper) : outOrder.Count;
        bool focus = ClientRecordRules.FocusOne(camera.pixelWidth, camera.pixelHeight, Screen.dpi);
        // Read beside the table: the newest on the left, a second sheet on the right to compare (one large face on a small phone).
        bool right = !focus && rank % 2 == 1;
        Rect pixels = camera.pixelRect;
        Vector2 screen = focus
            ? new Vector2(pixels.x + pixels.width * 0.5f, pixels.y + pixels.height * 0.52f)
            : new Vector2(pixels.x + pixels.width * (right ? 0.74f : 0.28f), pixels.y + pixels.height * (card ? 0.62f : 0.5f));
        float depth = Mathf.Max(camera.nearClipPlane + 0.12f, 0.6f - 0.03f * rank);
        float heightShare = focus ? (card ? 0.46f : 0.8f) : (card ? 0.36f : 0.74f);
        float widthShare = focus ? 0.7f : 0.44f;
        position = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        rotation = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
        float visibleHeight = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float visibleWidth = visibleHeight * Mathf.Max(0.1f, camera.aspect);
        worldScale = Mathf.Min(visibleHeight * heightShare / paper.Size.y, visibleWidth * widthShare / paper.Size.x);
    }

    public void Extracted(ClientRecordPaper paper)
    {
        outOrder.Remove(paper);
        outOrder.Add(paper);
        paper.SetEventCamera(ViewCamera);
        paper.BringWindowToFront();
    }

    public void PaperSettled(ClientRecordPaper paper)
    {
        if (paper?.Record == null) return;
        LastReadSource = paper.Record.source;
        GameplayEvents.RaiseGuideCue("BeaRecordRead");
    }

    public void PaperLanded(ClientRecordPaper paper) => outOrder.Remove(paper);

    public bool WantsReturn(ClientRecordPaper paper, Vector2 screen, Action ready)
    {
        Camera camera = ViewCamera;
        if (camera == null || tableCollider == null) return false;
        if (!tableCollider.Raycast(camera.ScreenPointToRay(screen), out _, 10f)) return false;
        ready?.Invoke();
        return true;
    }

    public bool BroughtToFront(ClientRecordPaper paper)
    {
        paper.BringWindowToFront();
        outOrder.Remove(paper);
        outOrder.Add(paper);
        Camera camera = ViewCamera;
        for (int i = 0; i < outOrder.Count; i++) outOrder[i].SetFrontDepth(camera, Mathf.Max(camera.nearClipPlane + 0.12f, 0.6f - 0.03f * i));
        LastReadSource = paper.Record?.source;
        return true;
    }

    public void PaperFlipped(ClientRecordPaper paper, bool backUp) { }
    public void PointerDown(ClientRecordPaper paper, PointerEventData eventData) { }
    public void PointerUp(ClientRecordPaper paper, PointerEventData eventData) { }
    public void PointerMoved(ClientRecordPaper paper) { }
    public bool IsGrouped(ClientRecordPaper paper) => false;
    public void BeginGroupDrag(ClientRecordPaper paper, PointerEventData eventData) { }
    public void DragGroup(PointerEventData eventData) { }
    public void EndGroupDrag(PointerEventData eventData) { }
}
