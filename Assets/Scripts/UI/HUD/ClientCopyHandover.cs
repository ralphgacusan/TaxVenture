using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Marie's follow-through as a walk back to her with her approved return
/// (Campaign Foundation T08, replacing the paper follow-through card).
///
/// FLOW (story runs of Level 1 only; practice, replays and Editor runs end
/// the case as before):
/// 1. The case is approved: the case result is still committed quietly at
///    approval (LevelCompleteUI), but the finale and the case result card wait.
///    The working folder leaves the icon (count 0, icon disabled); a floating
///    Client Copy appears beside the Auditor (ClientCopyPickup, the plaza
///    paper's pop-up and hover) and the Auditor says so, with a "!".
/// 2. Tapping it flies it into the folder icon (count 1, enabled). The copy
///    is the same folder, read-only (CaseFolderFieldRow.ReadOnly), stamped
///    CLIENT COPY on its front page, its drag item and the icon. The
///    objective points at Marie, who now waits in the conference room with
///    her available marker and a "!".
/// 3. Talking to her pauses the conversation on the handover line until the
///    copy is dragged onto her or the folder icon is tapped (DialogueUI's
///    awaited action; any other drop returns the copy to the icon).
/// 4. Only then is the follow-through committed (PlayerProgressManager.
///    CommitFollowThrough: trust +4 claimed once, the activity, and until T16
///    Chapter 1's completion). After the write: the icon empties, a heart
///    burst over Marie, the trust toast and the HUD trust bar easing to the
///    saved value. A failed write shows nothing, keeps the copy in the icon
///    and asks to try again.
/// 5. Elijah explains, Marie answers by result (clientFinale great / good /
///    okay, with her emote), then the finale (without her lines) and the case
///    result card run, then the career.
///
/// SAVE / RESUME: the phase (copy beside the Auditor / carried / handed over)
/// is part of the Level 1 field checkpoint; Level1FieldCheckpointer restores
/// it quietly (Restore) and never puts the approved case back to "case
/// received". A conversation interrupted before the drop starts again from
/// its beginning. After the drop PLAY resumes the saved finale/result flow.
///
/// LEVEL 2 (consistency pass): Rosa's approved review uses this same flow.
/// The Auditor's hand-in conversation continues with the approval and drops
/// Rosa's copy; it is carried in the same Case Folder slot (read-only, CLIENT
/// COPY stamp and tag), and Rosa takes it in the conference room
/// (Level2RosaConsultation owns her placement and passes taps here). Delivery
/// commits PlayerProgressManager.CommitLevelTwoFollowThrough (+4 trust and
/// Chapter 2 once, story runs only; an isolated development run commits no
/// claim). The phase is saved in Rosa's review (clientCopyPhase) with her
/// checkpoint, which is kept until she has her copy.
///
/// Marie's move to the conference room belongs to ClientConsultation (T23):
/// she moves after the side quest, and this handover only asks for the move
/// again at approval and on resume. ClientConsultation also owns taps on her
/// and hands them here while the copy exists (OwnsClientVisits).
/// </summary>
public sealed class ClientCopyHandover : MonoBehaviour
{
    // DeskFolder/Visual is an empty transform; the folder mesh (SM_Prop_Folder_Manila_02) is its
    // sibling, so the whole DeskFolder is cloned (its collider is stripped by ClientCopyPickup).
    private const string FolderModelPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/StackedCaseFolder/DeskFolder";
    private const string FallbackFolderModelPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/StackedCaseFolder/CaseFolder";
    private const float AuditorLineWait = 4f;
    private const float TrustPeekSeconds = 3.2f;

    public static ClientCopyHandover Instance { get; private set; }

    private ClientCopyPhase phase = ClientCopyPhase.None;
    private LevelTotalResult pendingResult;
    private CaseDefinition definition;
    private ClientHandoverDefinition script;
    private TaxpayerNpcInteractable client;
    // Level 2 (consistency pass): Rosa, the same actor from her stall, now in the conference room.
    private RosaInteractable rosa;
    private bool restorePending;
    private ClientCopyPhase restorePhase;
    private Transform clientRoot;
    private Transform spot;
    private ClientCopyPickup pickup;
    private int clientAlert;
    private bool inConversation;
    private bool finishing;
    private RectTransform iconTag;

    /// <summary>Where the copy is now.</summary>
    public ClientCopyPhase Phase => phase;
    /// <summary>The copy is in the folder icon: the folder is read-only and stamped.</summary>
    public static bool CopyActive => Instance != null && Instance.phase == ClientCopyPhase.Carried;
    /// <summary>The copy is beside the Auditor or carried.</summary>
    public static bool InProgress => Instance != null &&
        (Instance.phase == ClientCopyPhase.AtAuditor || Instance.phase == ClientCopyPhase.Carried);
    /// <summary>True while only the folder icon should respond during the handover prompt.</summary>
    public static bool AwaitingHandoverAction => (Instance != null && Instance.AwaitingHandover()) || AuditorInteractable.AwaitingFolder;

    /// <summary>Marie's step only (the Auditor's folder hand-in is AuditorInteractable.AwaitingFolder).</summary>
    public static bool AwaitingClientCopy => Instance != null && Instance.AwaitingHandover();
    /// <summary>Case editing and submission stay disabled after approval, through the result-card acknowledgement.</summary>
    public static bool CaseActionsBlocked => InProgress || (Instance != null && Instance.phase == ClientCopyPhase.Done) ||
        (PlayerProgressManager.Instance != null &&
         CampaignRun.AllowsStoryWrites(PlayerProgressManager.CurrentRunMode) &&
         (PlayerProgressManager.Instance.FollowThroughPending || PlayerProgressManager.Instance.PendingLevelOneEndFlow));
    /// <summary>This story run ends its case with the handover (the finale and the card wait for it).</summary>
    public static bool Defers => Instance != null && Instance.WouldDefer();
    /// <summary>The checkpoint value of the current phase.</summary>
    public string SavedPhase => ClientCopyRules.ToSaved(phase);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => CaseFolderFieldRow.ReadOnly = false;

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
        if (!scene.IsValid() || !scene.isLoaded) return;
        string level = OfficeLevelProfile.LevelIdFor(scene);
        if (level != OfficeLevelCatalog.Level1Id && level != OfficeLevelCatalog.Level2Id) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<ClientCopyHandover>(true) != null) return;
        var host = new GameObject("Client Copy Handover");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ClientCopyHandover>();
    }

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        AuditorInteractable.TalkOverride = HandleAuditorTalk;
        AuditorInteractable.AvailabilityOverride = AuditorAvailability;
    }

    private void OnDisable()
    {
        if (AuditorInteractable.TalkOverride == (System.Func<AuditorInteractable, bool>)HandleAuditorTalk) AuditorInteractable.TalkOverride = null;
        if (AuditorInteractable.AvailabilityOverride == (System.Func<NpcAvailability?>)AuditorAvailability) AuditorInteractable.AvailabilityOverride = null;
        ExitCopyMode();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private bool WouldDefer()
    {
        if (phase == ClientCopyPhase.Done) return false;
        CaseDefinition review = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (review != null && review.HasReview) return LevelTwoExpected(review);
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        if (manager == null || !manager.ClientCopyHandoverExpected) return false;
        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (def == null || def.HasReview || def.clientHandover == null || !def.clientHandover.enabled) return false;
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        return progression == null || progression.CurrentCaseNumber >= progression.TotalCasesInLevel;
    }

    // =========================================================
    // START (approval) AND RESUME
    // =========================================================

    /// <summary>
    /// The case result was saved (LevelCompleteUI): the copy appears beside
    /// the Auditor and the case result card waits. False when this run has
    /// no handover (the card shows now), or once Marie has her copy.
    /// </summary>
    public bool BeginAfterApproval(LevelTotalResult result)
    {
        if (phase == ClientCopyPhase.Done) return false;
        if (phase != ClientCopyPhase.None) return true;
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        bool expected = def != null && def.HasReview
            ? LevelTwoExpected(def)
            : manager != null && manager.SavedClientCopyPhase != ClientCopyPhase.None;
        if (!expected || !Resolve()) return false;

        pendingResult = result;
        phase = ClientCopyPhase.AtAuditor;
        CaseFolderFieldRow.ReadOnly = true;
        ReleaseWorkingFolder();
        RefreshIcons();
        RelocateClient(immediate: false);
        // Saved as "beside the Auditor": quitting during his lines resumes with the copy already floating.
        SaveCheckpoint();
        StartCoroutine(AuditorApproves());
        Debug.Log("[ClientCopy] Case approved: the Auditor explains the approval, then drops Marie's client copy.");
        return true;
    }

    /// <summary>
    /// Resume (Level1FieldCheckpointer): puts a saved phase back without any
    /// event, line or animation. The approved case stays approved: the
    /// receptionist is retired, the folder is off the desk, and the copy is
    /// beside the Auditor or in the icon.
    /// </summary>
    public void Restore(ClientCopyPhase saved)
    {
        if ((saved != ClientCopyPhase.AtAuditor && saved != ClientCopyPhase.Carried && saved != ClientCopyPhase.Done) ||
            phase != ClientCopyPhase.None) return;
        if (!Resolve())
        {
            // Level 2: Rosa is built a moment after the checkpoint; the copy comes back once she exists.
            restorePending = true;
            restorePhase = saved;
            return;
        }
        restorePending = false;
        phase = saved;
        pendingResult = LevelTwo ? null : LevelCompleteUI.CreateSavedCaseResult();

        ReceptionistInteractable receptionist = FindAnyObjectByType<ReceptionistInteractable>(FindObjectsInactive.Include);
        if (receptionist != null && definition != null) receptionist.RestoreSpoken(definition.caseId);
        CaseFolderFieldRow.ReadOnly = true;
        if (phase == ClientCopyPhase.Done)
        {
            ReleaseWorkingFolder();
            SetIconTag(false);
            RefreshIcons();
            RelocateClient(immediate: true);
            FinishLevel();
            Debug.Log("[ClientCopy] Resumed the saved end-of-level flow after Marie received her copy.");
            return;
        }

        StoreWorkingFolderQuietly();
        if (phase == ClientCopyPhase.AtAuditor)
        {
            ReleaseWorkingFolder();
            SpawnPickup(animated: false);
        }
        else EnterCopyMode();
        RefreshIcons();
        RelocateClient(immediate: true);
        Debug.Log($"[ClientCopy] Resumed: the client copy is {(phase == ClientCopyPhase.AtAuditor ? "beside the Auditor" : "in the folder icon")}.");
    }

    private bool Resolve()
    {
        definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (definition == null || definition.clientHandover == null || !definition.clientHandover.enabled) return false;
        script = definition.clientHandover;
        ClientFinaleDefinition staging = definition.clientFinale ?? new ClientFinaleDefinition();
        Scene scene = gameObject.scene;
        spot = FindInScene(scene, staging.spotPath);
        if (definition.HasReview)
        {
            // Level 2: Rosa herself (one actor; Level2RosaConsultation moves her to the room).
            rosa = Level2RosaConsultation.Instance != null ? Level2RosaConsultation.Instance.Rosa : null;
            clientRoot = rosa != null ? rosa.transform : null;
            client = null;
            return rosa != null && spot != null;
        }
        clientRoot = FindInScene(scene, staging.modelPath);
        client = clientRoot != null ? clientRoot.GetComponentInChildren<TaxpayerNpcInteractable>(true) : null;
        if (client == null || spot == null)
        {
            Debug.LogWarning("[ClientCopy] The client or her conference-room spot was not found; the case result card shows instead.");
            return false;
        }
        return true;
    }

    /// <summary>The current case is Level 2's review (Rosa).</summary>
    private static bool LevelTwo
    {
        get
        {
            CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
            return def != null && def.HasReview;
        }
    }

    /// <summary>
    /// Level 2 (consistency pass): Rosa's approved review ends with her Client Copy, unless she
    /// already has it (a story run's follow-through activity, or this review's saved phase).
    /// Practice and mastery replays never get one.
    /// </summary>
    private static bool LevelTwoExpected(CaseDefinition def)
    {
        if (def == null || !def.HasReview || def.clientHandover == null || !def.clientHandover.enabled) return false;
        CaseReviewState review = CaseManager.Instance != null && CaseManager.Instance.CurrentCase != null ? CaseManager.Instance.CurrentCase.review : null;
        if (review == null || review.audit == null || !review.audit.approved) return false;
        if (ClientCopyRules.Parse(review.clientCopyPhase) == ClientCopyPhase.Done) return false;
        if (TrainingSession.IsPracticing || PlayerProgressManager.CurrentRunMode == CampaignRunMode.MasteryReplay) return false;
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        return manager == null || !manager.UsesLevelTwoStory || !Level2CampaignRules.FollowThroughDone(manager.CurrentProgress);
    }

    /// <summary>The client's name for player-facing prompts ("Marie", "Rosa").</summary>
    public static string ClientDisplayName
    {
        get
        {
            CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
            return def != null && def.clientFinale != null && !string.IsNullOrWhiteSpace(def.clientFinale.name) ? def.clientFinale.name : "your client";
        }
    }

    /// <summary>
    /// After the Audit Summary's Continue: the Auditor explains the approval
    /// (lines by result), hands over the copy, and only when the conversation
    /// ends does the copy drop beside him, like a plaza NPC's profile. If he
    /// cannot talk in time, the copy drops anyway.
    /// </summary>
    private IEnumerator AuditorApproves()
    {
        AuditorInteractable auditor = FindAnyObjectByType<AuditorInteractable>(FindObjectsInactive.Exclude);
        // 2026-10-26: his hand-in conversation is still open (waiting to sign off): the approval and the
        // drop continue it, the copy dropping on his hand-over line.
        if (auditor != null && auditor.TryContinueApproval(ApprovalLines(), DropCopy)) yield break;
        float until = Time.unscaledTime + AuditorLineWait;
        while (auditor != null && CopyNotDropped && Time.unscaledTime < until)
        {
            if (!DialogueUI.IsAnyDialogueActive && !HudRewardScreen.AnyOpen && auditor.TrySay(ApprovalLines(), DropCopy))
                yield break;
            yield return null;
        }
        DropCopy();
    }

    /// <summary>Approved, but the Auditor has not dropped the copy yet (his conversation is still coming or running).</summary>
    private bool CopyNotDropped => phase == ClientCopyPhase.AtAuditor && pickup == null;

    /// <summary>The copy pops out beside the Auditor (once).</summary>
    private void DropCopy()
    {
        if (!CopyNotDropped) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        SpawnPickup(animated: true);
    }

    /// <summary>His approval lines for how the case went (first try / one slip / more), then the handover line.</summary>
    private List<DialogueLine> ApprovalLines()
    {
        CaseFinaleRules.Tier tier = CaseFinale.TierNow();
        List<DialogueLineDefinition> authored = tier == CaseFinaleRules.Tier.Great ? script.approvalGreat
            : tier == CaseFinaleRules.Tier.Good ? script.approvalGood : script.approvalOkay;
        string[] fallback = tier == CaseFinaleRules.Tier.Great ? DefaultApprovalGreat
            : tier == CaseFinaleRules.Tier.Good ? DefaultApprovalGood : DefaultApprovalOkay;
        List<DialogueLine> lines = Lines(authored, fallback, "Auditor");
        lines.AddRange(Lines(script.auditorGive, DefaultGive, "Auditor"));
        return lines;
    }

    // =========================================================
    // THE WORKING FOLDER AND THE COPY
    // =========================================================

    /// <summary>The approved folder leaves the icon quietly (no return to the desk, no event).</summary>
    private void ReleaseWorkingFolder()
    {
        foreach (FolderToHUDIcon folder in FindObjectsByType<FolderToHUDIcon>(FindObjectsInactive.Include))
            if (folder != null && folder.gameObject.scene == gameObject.scene && folder.IsStoredOrViewing) folder.ReleaseQuietly();
    }

    /// <summary>The same folder goes back into the icon quietly (the copy is picked up, or a resume).</summary>
    private void StoreWorkingFolderQuietly()
    {
        AuditorSubmissionTray tray = AuditorSubmissionTray.Instance;
        if (tray != null && tray.Folder != null)
        {
            tray.Folder.RestoreStored();
            return;
        }
        foreach (FolderToHUDIcon folder in FindObjectsByType<FolderToHUDIcon>(FindObjectsInactive.Include))
            if (folder != null && folder.gameObject.scene == gameObject.scene)
            {
                folder.RestoreStored();
                return;
            }
    }

    private void RefreshIcons()
    {
        foreach (HudIconButton icon in FindObjectsByType<HudIconButton>(FindObjectsInactive.Include)) icon.RefreshFolderState();
    }

    private void SpawnPickup(bool animated)
    {
        Scene scene = gameObject.scene;
        Transform model = WithMesh(FindInScene(scene, FolderModelPath)) ?? WithMesh(FindInScene(scene, FallbackFolderModelPath));
        AuditorInteractable auditor = FindAnyObjectByType<AuditorInteractable>(FindObjectsInactive.Exclude);
        RectTransform icon = OfficeHudRuntime.Instance != null ? OfficeHudRuntime.Instance.CaseFolderIcon : null;
        if (model == null || auditor == null)
        {
            // Nothing to float: the copy goes straight into the icon.
            Debug.LogWarning("[ClientCopy] The folder model or the Auditor was not found; the copy goes straight to the folder icon.");
            OnPickedUp();
            return;
        }

        Vector3 floor = auditor.transform.position;
        float height = 1.7f;
        if (DialogueSpeakerFrames.TryResolve(auditor, out DialogueSpeakerFrame frame))
        {
            floor = frame.Floor;
            height = frame.Height;
        }
        Vector3 toPlayer = auditor.transform.forward;
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (animated && player != null) toPlayer = player.position - floor;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) toPlayer = Vector3.forward;
        toPlayer.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, toPlayer).normalized;

        Vector3 rest = floor + Vector3.up * (height * 0.62f) + toPlayer * 0.55f + side * 0.5f;
        // Standing up like a held document: the cover (the model's +Y, up when it lies on the desk)
        // faces the player, tipped back a little toward the camera, and the folder's top edge
        // (the model's +Z, also the stamp's "up") points up so CLIENT COPY reads the right way.
        const float tipBack = 15f * Mathf.Deg2Rad;
        Vector3 cover = toPlayer * Mathf.Cos(tipBack) + Vector3.up * Mathf.Sin(tipBack);
        Vector3 top = Vector3.up * Mathf.Cos(tipBack) - toPlayer * Mathf.Sin(tipBack);
        Quaternion restRotation = Quaternion.LookRotation(top, cover);
        Vector3? from = animated ? floor + Vector3.up * (height * 0.55f) + toPlayer * 0.2f : (Vector3?)null;
        pickup = ClientCopyPickup.Create(scene, model.gameObject, rest, restRotation, from, icon, OnPickedUp);
        if (pickup == null) OnPickedUp();
    }

    /// <summary>The candidate only when it has a mesh to show (an empty clone would float the stamp alone).</summary>
    private static Transform WithMesh(Transform candidate)
    {
        if (candidate == null) return null;
        foreach (MeshFilter filter in candidate.GetComponentsInChildren<MeshFilter>(true))
            if (filter.sharedMesh != null) return candidate;
        return null;
    }

    private void OnPickedUp()
    {
        pickup = null;
        if (!ClientCopyRules.TryPickUp(ref phase)) return;
        StoreWorkingFolderQuietly();
        EnterCopyMode();
        RefreshIcons();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        GameplayEvents.RaiseClientCopyPickedUp();
        SaveCheckpoint();
        Debug.Log("[ClientCopy] Picked up: Marie's client copy is in the folder icon.");
    }

    private void EnterCopyMode()
    {
        CaseFolderFieldRow.ReadOnly = true;
        CaseFolderUI.PageDecorator -= DecoratePage;
        CaseFolderUI.PageDecorator += DecoratePage;
        SetIconTag(true);
        RefreshOpenPages();
        if (!inConversation) HoldClientAlert();
    }

    private void ExitCopyMode()
    {
        CaseFolderFieldRow.ReadOnly = false;
        CaseFolderUI.PageDecorator -= DecoratePage;
        SetIconTag(false);
        RefreshOpenPages();
        ReleaseClientAlert();
    }

    /// <summary>The CLIENT COPY stamp on the folder's front page (top right).</summary>
    private static void DecoratePage(int index, CaseFolderUI.PageBuilder page)
    {
        if (index != 0 || !CopyActive || page == null || page.Paper == null) return;
        Rect paper = page.Paper.rect;
        float width = Mathf.Max(240f, paper.width * 0.38f);
        RectTransform stamp = ClientCopyStamp.Build(page.Paper, width, new Vector2(paper.width * 0.22f, paper.height * 0.38f), -10f);
        page.AddOverlay(stamp.gameObject);
    }

    private static void RefreshOpenPages()
    {
        foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Exclude)) folder.RefreshCurrentPage();
    }

    private void SetIconTag(bool shown)
    {
        RectTransform icon = OfficeHudRuntime.Instance != null ? OfficeHudRuntime.Instance.CaseFolderIcon : null;
        if (iconTag == null && shown && icon != null) iconTag = ClientCopyStamp.BuildTag(icon);
        if (iconTag != null) iconTag.gameObject.SetActive(shown);
    }

    /// <summary>
    /// 2026-10-26 (user request): no icon over Marie while she waits (people show
    /// emotes only on dialogue lines); her NPC marker still shows she has
    /// something to say. Kept as a no-op so the call sites stay unchanged.
    /// </summary>
    private void HoldClientAlert()
    {
    }

    private void ReleaseClientAlert()
    {
        WorldEmote.Release(clientAlert);
        clientAlert = 0;
    }

    // =========================================================
    // MARIE IN THE CONFERENCE ROOM
    // =========================================================

    /// <summary>The client stands in the conference room (ClientConsultation owns Marie's move, T23; Level2RosaConsultation Rosa's).</summary>
    private static bool Relocated => LevelTwo
        ? Level2RosaConsultation.Instance != null && Level2RosaConsultation.Instance.InRoom
        : ClientConsultation.Instance != null && ClientConsultation.Instance.InRoom;

    private static void RelocateClient(bool immediate)
    {
        if (LevelTwo)
        {
            if (Level2RosaConsultation.Instance != null) Level2RosaConsultation.Instance.MoveToRoom(immediate);
            return;
        }
        if (ClientConsultation.Instance != null) ClientConsultation.Instance.MoveToRoom(immediate);
    }

    private void Update()
    {
        if (restorePending && Time.frameCount % 10 == 0) Restore(restorePhase);
        // Her "!" once she is in the room with the copy carried (she may arrive after the pickup).
        if (clientAlert == 0 && !inConversation && phase == ClientCopyPhase.Carried && Relocated) HoldClientAlert();
    }

    /// <summary>From approval on, taps on Marie belong to the handover (ClientConsultation passes them here).</summary>
    public bool OwnsClientVisits => phase != ClientCopyPhase.None;

    public NpcAvailability? ClientVisitAvailability(TaxpayerNpcInteractable npc)
    {
        if (npc != client || !Relocated) return null;
        switch (phase)
        {
            case ClientCopyPhase.Carried: return NpcAvailability.Available;
            case ClientCopyPhase.AtAuditor: return NpcAvailability.Unavailable;
            case ClientCopyPhase.Done: return NpcAvailability.Completed;
            default: return null;
        }
    }

    public bool HandleClientVisit(TaxpayerNpcInteractable npc)
    {
        if (npc != client || !Relocated || inConversation) return false;
        return HandleVisit();
    }

    /// <summary>Level 2: a tap on Rosa in the conference room once her review is approved (Level2RosaConsultation passes it here).</summary>
    public bool HandleRosaVisit(RosaInteractable npc)
    {
        if (npc == null || npc != rosa || !Relocated || inConversation) return false;
        return HandleVisit();
    }

    /// <summary>Level 2: Rosa's marker while the copy exists (null = her consultation's own state).</summary>
    public NpcAvailability? RosaVisitAvailability(RosaInteractable npc)
    {
        if (npc == null || npc != rosa || !Relocated) return null;
        return phase == ClientCopyPhase.Carried ? NpcAvailability.Available
            : phase == ClientCopyPhase.AtAuditor ? NpcAvailability.Unavailable
            : phase == ClientCopyPhase.Done ? NpcAvailability.Completed : (NpcAvailability?)null;
    }

    private bool HandleVisit()
    {
        if (phase == ClientCopyPhase.Carried)
        {
            StartHandoverConversation();
            return true;
        }
        if (phase == ClientCopyPhase.AtAuditor)
        {
            TryStartClientVisit(Lines(script.waiting, DefaultWaiting, ClientName), null, null);
            return true;
        }
        if (phase == ClientCopyPhase.Done && LevelTwo)
        {
            // Level 2: after the handover she answers briefly (talk only; nothing is committed again).
            TryStartClientVisit(Lines(script.afterDelivery, DefaultAfterDelivery, ClientName), null, null);
            return true;
        }
        return false;
    }

    /// <summary>A conversation with the client through the shared dialogue flow (Marie's visit or Rosa's).</summary>
    private bool TryStartClientVisit(List<DialogueLine> lines, System.Action completed, System.Action cancelled)
    {
        if (client != null) return client.TryStartVisit(lines, completed, cancelled);
        return rosa != null && rosa.TryStartVisit(lines, completed, cancelled);
    }

    /// <summary>The client's own component (speaker frame, emotes).</summary>
    private Component ClientComponent => client != null ? (Component)client : rosa;

    private bool HandleAuditorTalk(AuditorInteractable auditor)
    {
        // Before the drop his approval conversation is (re)played and ends with the drop.
        if (CopyNotDropped) auditor.TrySay(ApprovalLines(), DropCopy);
        // The copy floats beside him: a tap on him picks it up directly (no repeated line).
        else if (phase == ClientCopyPhase.AtAuditor) pickup.TryPickUpFromAuditor();
        else if (phase == ClientCopyPhase.Carried) auditor.TrySay(Lines(script.auditorRemind, DefaultRemind, "Auditor"), null);
        // After the handover the case is over: nothing more to say.
        return phase != ClientCopyPhase.None;
    }

    private NpcAvailability? AuditorAvailability()
    {
        if (phase == ClientCopyPhase.AtAuditor) return NpcAvailability.Available;
        if (phase == ClientCopyPhase.Carried || phase == ClientCopyPhase.Done) return NpcAvailability.Completed;
        return null;
    }

    // =========================================================
    // THE HANDOVER CONVERSATION
    // =========================================================

    private void StartHandoverConversation()
    {
        if (OfficeHudRuntime.Instance != null) OfficeHudRuntime.Instance.CloseCaseFolderView();
        ReleaseClientAlert();
        inConversation = TryStartClientVisit(HandoverLines(), OnConversationEnded, OnConversationEnded);
        if (!inConversation) HoldClientAlert();
    }

    private List<DialogueLine> HandoverLines()
    {
        var lines = new List<DialogueLine>();
        lines.AddRange(Lines(script.opening, DefaultOpening, ClientName));
        List<DialogueLine> handover = Lines(script.handover != null && !string.IsNullOrWhiteSpace(script.handover.text)
            ? new List<DialogueLineDefinition> { script.handover } : null, DefaultHandover, ClientName);
        DialogueLine wait = handover[handover.Count - 1];
        wait.AwaitAction = ClientCopyRules.HandoverAction;
        wait.ActionPrompt = string.IsNullOrWhiteSpace(script.handoverPrompt) ? "Drag the client copy to " + ClientName : script.handoverPrompt;
        lines.AddRange(handover);
        lines.AddRange(Lines(script.explain, DefaultExplain, ClientName));

        CaseFinaleRules.Tier tier = CaseFinale.TierNow();
        List<string> closing = CaseFinale.ClosingLines(definition.clientFinale, tier);
        string portrait = string.IsNullOrWhiteSpace(definition.clientApprovedPortrait) ? "Client1_Marie_Happy" : definition.clientApprovedPortrait;
        for (int i = 0; i < closing.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(closing[i])) continue;
            var line = new DialogueLine(DialogueSpeaker.Npc, closing[i], portrait, ClientName);
            if (i == 0 && EmoteCatalogue.TryParseTag(TierEmote(tier), out Emotion emotion)) line.Emote = emotion;
            lines.Add(line);
        }
        return lines;
    }

    private string TierEmote(CaseFinaleRules.Tier tier) =>
        tier == CaseFinaleRules.Tier.Great ? script.greatEmote : tier == CaseFinaleRules.Tier.Good ? script.goodEmote : script.okayEmote;

    private string ClientName =>
        definition != null && definition.clientFinale != null && !string.IsNullOrWhiteSpace(definition.clientFinale.name)
            ? definition.clientFinale.name : "Marie";

    private bool AwaitingHandover() =>
        inConversation && DialogueUI.ActiveAwaitingAction == ClientCopyRules.HandoverAction;

    /// <summary>Folder icon tap (OfficeHudRuntime): during the handover step it hands the copy over (small screens, accessibility).</summary>
    public bool TryIconTap()
    {
        if (!AwaitingHandover()) return false;
        TryHandOver();
        return true;
    }

    /// <summary>
    /// A folder drag released (OfficeHudRuntime / HudSubmittableIcon). While
    /// the copy is carried it never reaches the Auditor's tray: back on the
    /// icon it returns; during the handover step a drop on Marie hands it over
    /// and any other drop returns it; otherwise it opens where it was dropped.
    /// </summary>
    public HudDragOutcome? ResolveDrop(Vector2 screen)
    {
        if (phase != ClientCopyPhase.Carried) return null;
        RectTransform icon = OfficeHudRuntime.Instance != null ? OfficeHudRuntime.Instance.CaseFolderIcon : null;
        if (icon != null && RectTransformUtility.RectangleContainsScreenPoint(icon, screen, null)) return HudDragOutcome.Returned;
        if (!AwaitingHandover()) return HudDragOutcome.Opened;
        if (!OverClient(screen)) return HudDragOutcome.Returned;
        return TryHandOver() ? HudDragOutcome.Consumed : HudDragOutcome.Returned;
    }

    private bool OverClient(Vector2 screen)
    {
        Camera cam = Camera.main;
        if (cam == null || clientRoot == null) return false;
        Ray ray = cam.ScreenPointToRay(screen);
        foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            if (hit.collider != null && hit.collider.transform.IsChildOf(clientRoot)) return true;
        return false;
    }

    /// <summary>
    /// The handover itself: commits the follow-through (trust +4, claimed
    /// once). Only after a successful write does anything show or change; a
    /// failed write keeps the copy in the icon and asks to try again.
    /// </summary>
    private bool TryHandOver()
    {
        if (!ClientCopyRules.CanHandOver(phase)) return false;
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        // Level 2: Rosa's follow-through (+4 trust and Chapter 2, once, story runs only). An isolated
        // development run has no campaign claim to write: the handover completes and saves nothing more.
        bool claims = !LevelTwo || (manager != null && manager.UsesLevelTwoStory && !Level2Checkpointer.IsDirectEditorAttempt);
        PlayerProgressManager.StoryGrants grants;
        if (!LevelTwo) grants = manager != null ? manager.CommitFollowThrough() : default;
        else if (claims) grants = manager.CommitLevelTwoFollowThrough();
        else grants = new PlayerProgressManager.StoryGrants { Saved = true };
        DialogueUI dialogue = DialogueUI.Active;
        if (!grants.Saved)
        {
            Debug.LogWarning("[ClientCopy] The follow-through could not be saved; the copy stays in the folder icon.");
            if (dialogue != null) dialogue.SetActionPrompt(string.IsNullOrWhiteSpace(script.retryPrompt)
                ? "It didn't save. Drag the client copy to " + ClientName + " again." : script.retryPrompt);
            return false;
        }

        if (LevelTwo) Level2Checkpointer.Active?.PublishEthicsFollowThrough();

        phase = ClientCopyRules.AfterHandover(phase, saved: true);
        ReleaseWorkingFolder();
        ExitCopyMode();
        // The approved case stays locked while the finale/result card is pending.
        CaseFolderFieldRow.ReadOnly = true;
        RefreshIcons();
        GameplayEvents.RaiseClientCopyHandedOver();
        if (LevelTwo) SaveCheckpoint();
        if (!grants.AlreadyDone)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
            CaseReviewState ethicsReview = LevelTwo && CaseManager.Instance != null ? CaseManager.Instance.CurrentCase?.review : null;
            bool unsupportedPromise = ethicsReview != null && ethicsReview.rosaEthicsChoiceId != null &&
                                      ethicsReview.rosaEthicsChoiceId.EndsWith("/unsupported-promise", System.StringComparison.Ordinal);
            if (!unsupportedPromise) WorldEmote.Burst(clientRoot, Emotion.Thankful, 7);
            Vector3 head = clientRoot.position + Vector3.up * 1.6f;
            if (DialogueSpeakerFrames.TryResolve(ClientComponent, out DialogueSpeakerFrame frame)) head = frame.Head;
            if (claims)
            {
                HudFeedback.AnnounceCampaign(grants.Trust, default, head);
                if (OfficeHudRuntime.Instance != null) OfficeHudRuntime.Instance.PeekDuringDialogue(TrustPeekSeconds);
            }
        }
        Debug.Log($"[ClientCopy] Handed over: {ClientName} has the copy (trust {grants.Trust.After}/100).");
        if (dialogue != null)
        {
            List<DialogueLine> response = new List<DialogueLine>();
            CaseReviewState review = LevelTwo && CaseManager.Instance != null ? CaseManager.Instance.CurrentCase?.review : null;
            if (review != null && review.rosaEthicsCommitted &&
                (review.rosaEthicsChoiceId.EndsWith("/responsible", System.StringComparison.Ordinal) ||
                 review.rosaEthicsChoiceId.EndsWith("/unsupported-promise", System.StringComparison.Ordinal)))
            {
                if (review.rosaEthicsChoiceId.EndsWith("/responsible", System.StringComparison.Ordinal))
                    response.Add(new DialogueLine(DialogueSpeaker.Npc,
                        "Thank you for checking the payment with me. I'm relieved we found the records that support it.", "Rosa_Default", ClientName));
                else
                {
                    response.Add(new DialogueLine(DialogueSpeaker.Player,
                        "I called it ready before the records supported it. I've revised that promise; the supporting records are checked now.", "Auditor_Default", "You"));
                    response.Add(new DialogueLine(DialogueSpeaker.Npc,
                        "I understand. Thank you for explaining and bringing me the updated copy.", "Rosa_Default", ClientName));
                }
            }
            if (response.Count > 0) dialogue.ContinueWith(ClientCopyRules.HandoverAction, response);
            else dialogue.CompleteAwaitedAction(ClientCopyRules.HandoverAction);
        }
        return true;
    }

    private void OnConversationEnded()
    {
        inConversation = false;
        if (phase == ClientCopyPhase.Done) FinishLevel();
        else if (phase == ClientCopyPhase.Carried) HoldClientAlert();
    }

    /// <summary>Marie has her copy: the finale (without her lines) and the case result card, then the career.</summary>
    private void FinishLevel()
    {
        if (finishing) return;
        finishing = true;
        pendingResult ??= LevelTwo ? LevelCompleteUI.CreateLevelResult() : LevelCompleteUI.CreateSavedCaseResult();
        if (CaseFinale.Instance != null) CaseFinale.Instance.PlayAfterHandover();
        if (LevelCompleteUI.Instance != null) LevelCompleteUI.Instance.ShowAfterHandover(pendingResult);
        else Debug.LogWarning("[ClientCopy] LevelCompleteUI is missing; the case result card cannot show.");
    }

    private void SaveCheckpoint()
    {
        if (LevelTwo)
        {
            // Level 2: the phase lives in Rosa's review, saved with her checkpoint (kept until she has her copy).
            CaseReviewState review = CaseManager.Instance != null && CaseManager.Instance.CurrentCase != null ? CaseManager.Instance.CurrentCase.review : null;
            if (review != null) review.clientCopyPhase = ClientCopyRules.ToSaved(phase);
            if (Level2Checkpointer.Active != null) Level2Checkpointer.Active.RequestSave();
            return;
        }
        if (Level1FieldCheckpointer.Active != null) Level1FieldCheckpointer.Active.RequestSave();
    }

    // =========================================================
    // LINES
    // =========================================================

    private static readonly string[] DefaultGive = { "Npc", "Here is Marie's client copy. Bring it to her and walk her through it.", "alert" };
    private static readonly string[] DefaultApprovalGreat = {
        "Npc", "Marie's return is approved. First try, and on time. Nicely done.", "sparkles",
        "Npc", "Her residency status, taxpayer type and income source all match her documents.", null };
    private static readonly string[] DefaultApprovalGood = {
        "Npc", "It took one more look, but Marie's return is approved.", null,
        "Npc", "Her residency status, taxpayer type and income source now match her documents.", null };
    private static readonly string[] DefaultApprovalOkay = {
        "Npc", "That took a few tries, but Marie's return is approved now.", "sweat",
        "Npc", "Her residency status, taxpayer type and income source finally match her documents.", null };
    private static readonly string[] DefaultRemind = { "Npc", "Marie is waiting in the conference room. Walk her through her copy.", null };
    private static readonly string[] DefaultWaiting = { "Npc", "Oh, hello! Is my file ready yet?", "question", "Player", "Almost. The Auditor has your copy for me.", null };
    private static readonly string[] DefaultOpening = { "Player", "Marie! Good news: your return is approved.", "alert", "Npc", "Really? Already?", null };
    private static readonly string[] DefaultHandover = { "Player", "Here is your copy. The Auditor checked every line.", null };
    private static readonly string[] DefaultExplain = { "Player", "Keep it with your records. It shows what was filed for you and when.", null,
        "Player", "If your work or your pay ever changes, come and see me before the next filing.", "idea" };
    private static readonly string[] DefaultAfterDelivery = { "Npc", "Thank you again. I'll keep my copy with my records.", "heart" };

    /// <summary>The case JSON's lines, or the built-in ones (speaker, text, emote triples) when it has none.</summary>
    private static List<DialogueLine> Lines(List<DialogueLineDefinition> authored, string[] fallback, string npcName)
    {
        List<DialogueLine> built = authored != null && authored.Count > 0 ? CaseFactory.BuildDialogue(authored, npcName) : null;
        if (built != null && built.Count > 0) return built;
        var builder = new DialogueBuilder(npcName);
        for (int i = 0; i + 2 < fallback.Length; i += 3)
        {
            if (fallback[i] == "Npc") builder.Npc(fallback[i + 1], npcName == "Auditor" ? "Auditor_Default" : "default");
            else builder.Player(fallback[i + 1], "Auditor_Default");
            builder.Emote(fallback[i + 2]);
        }
        return builder.Build();
    }

    private static Transform FindInScene(Scene scene, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            if (slash < 0) return root.transform;
            Transform found = root.transform.Find(path.Substring(slash + 1));
            if (found != null) return found;
        }
        return null;
    }
}
