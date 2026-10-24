using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Marie in the conference room during her case (Campaign Foundation T23).
///
/// The one owner of her relocation: once the side quest is completed her
/// plaza model moves to the conference room's client spot while neither place
/// is on screen (no visible teleport), and the room's stand-in client steps
/// aside. ClientCopyHandover asks this owner to move her (approval, resume).
///
/// The one owner of taps on her after her plaza talk: during the case a tap
/// starts a short consultation chosen by ClientConsultationRules (a clue that
/// fits, a special moment after a failed audit or on a new Office Hours day,
/// or the visit ladder). From approval on, the Client Copy handover takes the
/// taps. The relocation and the consultation state are saved in the Level 1
/// checkpoint and restored quietly.
///
/// Visiting is talk only: it never calls a trust, coin, XP, score, clock,
/// objective or story command. Missing or malformed data falls back to a
/// short neutral line (or no consultation) with one warning.
/// </summary>
public sealed class ClientConsultation : MonoBehaviour
{
    public static ClientConsultation Instance { get; private set; }

    private Level1ConsultationRecord state = new Level1ConsultationRecord();
    private CaseDefinition definition;
    private ClientConsultationDefinition consultation;
    private TaxpayerNpcInteractable client;
    private Transform clientRoot;
    private Transform spot;
    private bool resolved;
    private bool warnedResolve;
    private bool inRoom;
    private bool movePending;
    private bool inVisit;
    private readonly Plane[] frustum = new Plane[6];
    private readonly List<Renderer> renderers = new List<Renderer>();

    /// <summary>Marie stands at the conference room's client spot.</summary>
    public bool InRoom => inRoom;

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
        if (!scene.IsValid() || !scene.isLoaded || OfficeLevelProfile.LevelIdFor(scene) != OfficeLevelCatalog.Level1Id) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<ClientConsultation>(true) != null) return;
        var host = new GameObject("Client Consultation");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ClientConsultation>();
    }

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        TaxpayerNpcInteractable.VisitHandler = HandleVisit;
        TaxpayerNpcInteractable.VisitAvailability = VisitAvailability;
    }

    private void OnDisable()
    {
        if (TaxpayerNpcInteractable.VisitHandler == (System.Func<TaxpayerNpcInteractable, bool>)HandleVisit) TaxpayerNpcInteractable.VisitHandler = null;
        if (TaxpayerNpcInteractable.VisitAvailability == (System.Func<TaxpayerNpcInteractable, NpcAvailability?>)VisitAvailability)
            TaxpayerNpcInteractable.VisitAvailability = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!Resolve()) return;
        if (!inRoom && !movePending && LevelGate.SideQuestDone) movePending = true;
        if (movePending && !Seen()) Move(save: true);
    }

    // =========================================================
    // RELOCATION (single owner)
    // =========================================================

    /// <summary>Moves Marie to the conference room: now (resume, under a fade) or as soon as nobody sees it.</summary>
    public void MoveToRoom(bool immediate)
    {
        if (inRoom || !Resolve()) return;
        if (immediate) Move(save: false);
        else movePending = true;
    }

    /// <summary>Marie or her spot is on screen and not behind a wall.</summary>
    private bool Seen()
    {
        Camera cam = GuideTargets.ViewCamera;
        if (cam == null) return false;
        GeometryUtility.CalculateFrustumPlanes(cam, frustum);
        return Visible(cam, clientRoot) || Visible(cam, spot);
    }

    private bool Visible(Camera cam, Transform target)
    {
        renderers.Clear();
        target.GetComponentsInChildren(false, renderers);
        bool seen = false;
        for (int i = 0; i < renderers.Count && !seen; i++)
        {
            Renderer r = renderers[i];
            if (!r.enabled || !(r is MeshRenderer || r is SkinnedMeshRenderer) || !GeometryUtility.TestPlanesAABB(frustum, r.bounds)) continue;
            Vector3 from = cam.transform.position;
            Vector3 to = r.bounds.center;
            seen = !Physics.Linecast(from, to, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ||
                   hit.collider.transform.IsChildOf(target);
        }
        renderers.Clear();
        return seen;
    }

    private void Move(bool save)
    {
        movePending = false;
        if (inRoom || clientRoot == null || spot == null) return;

        // The conference room's stand-in client steps aside for Marie (restored with the scene).
        foreach (Renderer r in spot.GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider c in spot.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (NpcStatusMarker m in spot.GetComponentsInChildren<NpcStatusMarker>()) m.enabled = false;
        foreach (ClientInteractable c in spot.GetComponentsInChildren<ClientInteractable>()) c.enabled = false;

        Vector3 feet = spot.position;
        if (Physics.Raycast(spot.position + Vector3.up * 1f, Vector3.down, out RaycastHit floor, 2.5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            feet.y = floor.point.y;
        clientRoot.SetPositionAndRotation(feet, spot.rotation);
        inRoom = true;
        state.clientInRoom = true;
        Debug.Log("[Consultation] Marie is in the conference room.");
        if (save) SaveCheckpoint();
    }

    // =========================================================
    // VISITS
    // =========================================================

    private bool HandoverOwnsVisits => ClientCopyHandover.Instance != null && ClientCopyHandover.Instance.OwnsClientVisits;

    /// <summary>A consultation is possible now: Marie is in the room, the case is not approved yet, and there is something to say.</summary>
    private bool CanConsult => resolved && inRoom && !inVisit && LevelGate.SideQuestDone &&
                               !ClientCopyHandover.CaseActionsBlocked && ClientConsultationRules.HasContent(consultation);

    private bool HandleVisit(TaxpayerNpcInteractable npc)
    {
        if (HandoverOwnsVisits) return ClientCopyHandover.Instance.HandleClientVisit(npc);
        if (npc != client || !CanConsult) return false;
        return StartConsultation();
    }

    private NpcAvailability? VisitAvailability(TaxpayerNpcInteractable npc)
    {
        if (HandoverOwnsVisits) return ClientCopyHandover.Instance.ClientVisitAvailability(npc);
        if (npc != client || !inRoom) return null;
        return CanConsult || inVisit ? NpcAvailability.Available : (NpcAvailability?)null;
    }

    private bool StartConsultation()
    {
        Level1ConsultationRecord before = Copy(state);
        ConsultationPick pick = ClientConsultationRules.Next(consultation, state, Context());
        List<DialogueLine> lines = pick.Lines != null ? CaseFactory.BuildDialogue(pick.Lines, ClientName) : null;
        if (lines == null || lines.Count == 0) lines = NeutralLine();
        inVisit = client.TryStartVisit(lines, EndVisit, EndVisit);
        if (!inVisit)
        {
            state = before;
            return false;
        }
        Debug.Log($"[Consultation] Visit {state.visits}: {pick.Kind} '{pick.Id}'{(pick.Step >= 0 ? $" (step {pick.Step + 1})" : "")}.");
        SaveCheckpoint();
        return true;
    }

    private void EndVisit() => inVisit = false;

    private List<DialogueLine> NeutralLine() =>
        new DialogueBuilder(ClientName).Npc("Hello again! Let me know if you need anything about my file.", "Client1_Marie_Default").Build();

    /// <summary>The case as the consultation sees it: counters, document reads and field states (never answers).</summary>
    private ConsultationContext Context()
    {
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        AuditorInteractable auditor = FindAnyObjectByType<AuditorInteractable>(FindObjectsInactive.Include);
        ReceptionistInteractable receptionist = FindAnyObjectByType<ReceptionistInteractable>(FindObjectsInactive.Include);
        OfficeHours hours = OfficeHours.Instance;
        return new ConsultationContext
        {
            FailedAudits = auditor != null ? auditor.FailedAudits : 0,
            Day = hours != null && hours.Announced ? Mathf.Max(1, hours.Day) : 1,
            CaseOpen = receptionist != null && definition != null && receptionist.HasSpokenFor(definition.caseId),
            DocumentUnread = name => Unread(data, name),
            Field = key => FieldState(data, key)
        };
    }

    private static bool Unread(CaseData data, string documentName)
    {
        if (data?.supportingDocuments == null) return false;
        foreach (SupportingDocument document in data.supportingDocuments)
            if (document != null && document.documentName == documentName) return !document.isReviewed;
        return false;
    }

    private ConsultationFieldState FieldState(CaseData data, string key)
    {
        if (data == null) return ConsultationFieldState.Filled;
        if (data.folderTransferredFields == null || !data.folderTransferredFields.Contains(key) || Level1CaseCheckpoint.Read(data, key) == null)
            return ConsultationFieldState.Empty;
        string graded = GradedKey(key);
        if (graded != null && definition != null)
            foreach (RequiredFieldDefinition field in definition.GradedFields())
                if (field.key == graded)
                    return CaseFieldRules.IsCorrect(data, definition.answerKey, graded) ? ConsultationFieldState.Filled : ConsultationFieldState.Wrong;
        return ConsultationFieldState.Filled;
    }

    /// <summary>The Auditor's key for a folder field key (ResidencyStatus -> residencyStatus).</summary>
    private static string GradedKey(string folderKey)
    {
        if (string.IsNullOrEmpty(folderKey)) return null;
        if (folderKey == "EmployerCount") return "numberOfEmployers";
        return char.ToLowerInvariant(folderKey[0]) + folderKey.Substring(1);
    }

    // =========================================================
    // SAVE AND RESUME
    // =========================================================

    /// <summary>The state to save in the Level 1 checkpoint.</summary>
    public Level1ConsultationRecord Capture()
    {
        Level1ConsultationRecord copy = Copy(state);
        copy.clientInRoom = inRoom;
        return copy;
    }

    /// <summary>
    /// Resume: the saved visits and markers come back, and Marie stands in the
    /// room again when she was there (or the side quest is done). No line, no
    /// event, no save.
    /// </summary>
    public void Restore(Level1ConsultationRecord saved, bool sideQuestDone)
    {
        state = Copy(saved);
        state.Repair();
        if (state.clientInRoom || sideQuestDone) MoveToRoom(immediate: true);
    }

    private static Level1ConsultationRecord Copy(Level1ConsultationRecord record) =>
        record == null ? new Level1ConsultationRecord() : JsonUtility.FromJson<Level1ConsultationRecord>(JsonUtility.ToJson(record));

    private static void SaveCheckpoint()
    {
        if (Level1FieldCheckpointer.Active != null) Level1FieldCheckpointer.Active.RequestSave();
    }

    // =========================================================
    // DATA
    // =========================================================

    private string ClientName =>
        definition != null && definition.clientFinale != null && !string.IsNullOrWhiteSpace(definition.clientFinale.name)
            ? definition.clientFinale.name : "Marie";

    private bool Resolve()
    {
        if (resolved) return true;
        definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (definition == null || definition.HasReview) return false;
        ClientFinaleDefinition staging = definition.clientFinale ?? new ClientFinaleDefinition();
        clientRoot = FindInScene(gameObject.scene, staging.modelPath);
        spot = FindInScene(gameObject.scene, staging.spotPath);
        client = clientRoot != null ? clientRoot.GetComponentInChildren<TaxpayerNpcInteractable>(true) : null;
        if (client == null || spot == null)
        {
            if (!warnedResolve) Debug.LogWarning("[Consultation] The client or her conference-room spot was not found; she stays where she is.");
            warnedResolve = true;
            return false;
        }
        consultation = Effective(definition);
        WarnProblems();
        resolved = true;
        return true;
    }

    /// <summary>The case's consultation, with clientAfterFailedAudit as its failed-audit moment when it has none.</summary>
    private static ClientConsultationDefinition Effective(CaseDefinition def)
    {
        ClientConsultationDefinition source = def.consultation ?? new ClientConsultationDefinition { enabled = false };
        var effective = new ClientConsultationDefinition
        {
            enabled = source.enabled,
            clues = source.clues ?? new List<ConsultationClue>(),
            failedAudit = source.failedAudit ?? new List<ConsultationVariant>(),
            laterDay = source.laterDay ?? new List<ConsultationVariant>(),
            ladder = source.ladder ?? new List<ConsultationStep>(),
            fallback = source.fallback ?? new List<DialogueLineDefinition>()
        };
        if (effective.failedAudit.Count == 0 && def.clientAfterFailedAudit != null && def.clientAfterFailedAudit.Count > 0)
            effective.failedAudit = new List<ConsultationVariant> { new ConsultationVariant { id = "clientAfterFailedAudit", lines = def.clientAfterFailedAudit } };
        return effective;
    }

    private void WarnProblems()
    {
        if (!ClientConsultationRules.HasContent(consultation)) return;
        var documents = new List<string>();
        if (definition.documents != null)
            foreach (DocumentDefinition document in definition.documents)
                if (document != null && !string.IsNullOrWhiteSpace(document.documentName)) documents.Add(document.documentName);
        var answers = new List<string>();
        foreach (RequiredFieldDefinition field in definition.GradedFields())
        {
            string value = CaseFieldRules.AnswerValue(definition.answerKey, field.key);
            if (string.IsNullOrWhiteSpace(value)) continue;
            answers.Add(value.Trim());
            answers.Add(System.Text.RegularExpressions.Regex.Replace(value.Trim(), "(?<=[a-z])(?=[A-Z])", " "));
        }
        List<string> problems = ClientConsultationRules.Problems(consultation, documents, Level1CaseCheckpoint.FieldKeys, answers);
        if (problems.Count > 0)
            Debug.LogWarning($"[Consultation] {problems.Count} consultation entries were skipped or need a look: {string.Join("; ", problems)}");
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
