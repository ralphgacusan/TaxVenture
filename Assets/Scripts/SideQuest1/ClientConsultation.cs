using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Marie in the conference room during her case (Campaign Foundation T23).
///
/// The one owner of her relocation (Living Plaza: she walks, never teleports
/// in view). Completing the side quest is her cue: she stops her plaza
/// activity and walks into the office (MarieOfficeRoute: through the front
/// doors once they open) to wait beside the front desk. When the
/// receptionist's hand-over for her case is done she walks on to the
/// conference room's client spot; if it was done before she got there she
/// continues without waiting. Arrival there is presence only, never progress.
/// The room's stand-in client steps aside. ClientCopyHandover asks this owner
/// to move her (approval, resume). Her stage is never saved separately: on
/// resume it follows from the saved flags (in the room, the side quest, the
/// receptionist) once the checkpoint has restored them.
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
    private NpcLife life;
    private MarieOfficeRoute route;
    private ReceptionistInteractable receptionist;
    private bool resolved;
    private bool warnedResolve;
    private bool inRoom;
    private bool inVisit;
    private bool restorePending, restoreInRoom;

    private MarieTripRules.Leg leg = MarieTripRules.Leg.Plaza;
    private float nextLegAt;

    /// <summary>Her trip's current leg (presentation; derived again on resume).</summary>
    public MarieTripRules.Leg CurrentLeg => leg;

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
        if (restorePending)
        {
            // The receptionist's hand-over is restored after this record: wait for the whole checkpoint.
            if (Level1FieldCheckpointer.Active != null && !Level1FieldCheckpointer.Active.IsReady) return;
            ApplyRestore();
        }
        if (inRoom || !LevelGate.SideQuestDone) return;
        // The side quest is her cue (the front doors open with it); the hand-over is read, not awaited.
        MarieTripRules.Leg next = MarieTripRules.Next(leg, true, OfficeDoorGate.IsClosed, ReceptionDone, Time.time >= nextLegAt);
        if (next != leg && (next == MarieTripRules.Leg.ToReception || next == MarieTripRules.Leg.ToRoom)) StartLeg(next);
    }

    // =========================================================
    // RELOCATION (single owner): she walks
    // =========================================================

    /// <summary>Moves Marie to the conference room: now (resume, under a fade) or on foot from where she is.</summary>
    public void MoveToRoom(bool immediate)
    {
        if (inRoom || !Resolve()) return;
        if (immediate) PlaceInRoom(save: false);
        else if (leg != MarieTripRules.Leg.ToRoom) StartLeg(MarieTripRules.Leg.ToRoom);
    }

    /// <summary>The receptionist has handed over Marie's case (the condition is read, never waited on as an event).</summary>
    private bool ReceptionDone
    {
        get
        {
            if (receptionist == null) receptionist = FindAnyObjectByType<ReceptionistInteractable>(FindObjectsInactive.Include);
            return receptionist == null || (definition != null && receptionist.HasSpokenFor(definition.caseId));
        }
    }

    private void StartLeg(MarieTripRules.Leg next)
    {
        bool fromPlaza = MarieTripRules.StartsOutside(leg);
        if (next == MarieTripRules.Leg.ToReception)
        {
            List<Vector3> stops = route != null ? route.ToReception() : new List<Vector3>();
            if (stops.Count == 0)
            {
                StartLeg(MarieTripRules.Leg.ToRoom);
                return;
            }
            leg = MarieTripRules.Leg.ToReception;
            Debug.Log("[Consultation] Marie walks to the front desk.");
            life.TravelForQuest(stops, ReceptionFacing(), () =>
            {
                leg = MarieTripRules.Leg.AtReception;
                // A moment at the desk, even when the hand-over already happened.
                nextLegAt = Time.time + 1.5f;
            });
            return;
        }

        List<Vector3> toRoom = route != null ? route.ToRoom(fromPlaza) : new List<Vector3>();
        toRoom.Add(RoomFeet());
        leg = MarieTripRules.Leg.ToRoom;
        Debug.Log("[Consultation] Marie walks to the conference room.");
        life.TravelForQuest(toRoom, RoomFacing(), () => ArriveInRoom(save: true));
    }

    private void ArriveInRoom(bool save)
    {
        StandInStepsAside();
        inRoom = true;
        leg = MarieTripRules.Leg.InRoom;
        state.clientInRoom = true;
        Debug.Log("[Consultation] Marie is in the conference room.");
        if (save) SaveCheckpoint();
    }

    /// <summary>Puts her at the room's client spot at once (resume under the fade, or a handover resume).</summary>
    private void PlaceInRoom(bool save)
    {
        if (inRoom || clientRoot == null || spot == null) return;
        life.PlaceForQuest(RoomFeet(), RoomFacing());
        ArriveInRoom(save);
    }

    private void PlaceAtReception()
    {
        Transform wait = route != null ? route.ReceptionWait : null;
        if (wait == null) return;
        life.PlaceForQuest(NpcLife.Ground(wait.position), ReceptionFacing());
        leg = MarieTripRules.Leg.AtReception;
        nextLegAt = Time.time + 1.5f;
    }

    private void StandInStepsAside() => StandInStepsAside(spot);

    /// <summary>The conference room's stand-in client at this spot steps aside for the case client (Marie; Level 2: Rosa).</summary>
    public static void StandInStepsAside(Transform spot)
    {
        // The conference room's stand-in client steps aside for Marie (restored with the scene).
        if (spot == null) return;
        foreach (Renderer r in spot.GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider c in spot.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (NpcStatusMarker m in spot.GetComponentsInChildren<NpcStatusMarker>()) m.enabled = false;
        foreach (ClientInteractable c in spot.GetComponentsInChildren<ClientInteractable>()) c.enabled = false;
    }

    private Vector3 RoomFeet()
    {
        Vector3 feet = spot.position;
        if (Physics.Raycast(spot.position + Vector3.up * 1f, Vector3.down, out RaycastHit floor, 2.5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            feet.y = floor.point.y;
        return feet;
    }

    /// <summary>She faces the way the room's client spot faces (into the room, toward the door).</summary>
    private Quaternion RoomFacing() => Quaternion.Euler(0f, spot.eulerAngles.y, 0f);

    /// <summary>At the front desk she faces the receptionist.</summary>
    private Quaternion ReceptionFacing()
    {
        Transform wait = route != null ? route.ReceptionWait : null;
        if (receptionist == null) receptionist = FindAnyObjectByType<ReceptionistInteractable>(FindObjectsInactive.Include);
        if (wait != null && receptionist != null)
        {
            Vector3 look = receptionist.transform.position - wait.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) return Quaternion.LookRotation(look.normalized);
        }
        return wait != null ? Quaternion.Euler(0f, wait.eulerAngles.y, 0f) : Quaternion.identity;
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
        if (npc != client) return false;
        if (!inRoom && LevelGate.SideQuestDone && leg != MarieTripRules.Leg.Plaza) return StartTravelAside();
        if (!CanConsult) return false;
        return StartConsultation();
    }

    /// <summary>A tap on her on the way: one friendly line (talk only); the walk pauses and then goes on.</summary>
    private bool StartTravelAside()
    {
        if (inVisit) return false;
        MarieTravelDefinition travel = PlazaLifeContent.Current.marieTravel;
        List<DialogueLineDefinition> source = leg == MarieTripRules.Leg.AtReception ? travel.atReception
            : leg == MarieTripRules.Leg.ToRoom ? travel.toRoom : travel.toOffice;
        List<DialogueLine> lines = PlazaLifeContent.Build(source, ClientName);
        if (lines == null || lines.Count == 0) return false;
        inVisit = client.TryStartVisit(lines, EndVisit, EndVisit);
        return inVisit;
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
    /// Resume: the saved visits and markers come back. Where Marie stands is
    /// decided once the whole checkpoint is back (ApplyRestore): in the room
    /// when she was there or her hand-over is done, else beside the front desk
    /// when the side quest is done. No line, no event, no save.
    /// </summary>
    public void Restore(Level1ConsultationRecord saved, bool sideQuestDone)
    {
        state = Copy(saved);
        state.Repair();
        restoreInRoom = state.clientInRoom;
        if (state.clientInRoom || sideQuestDone) restorePending = true;
    }

    /// <summary>Places her for a resumed run, under the level's fade-in (no walk, no line, no save).</summary>
    private void ApplyRestore()
    {
        restorePending = false;
        if (inRoom) return;
        switch (MarieTripRules.Resume(restoreInRoom, LevelGate.SideQuestDone, ReceptionDone))
        {
            case MarieTripRules.Leg.InRoom: PlaceInRoom(save: false); break;
            case MarieTripRules.Leg.AtReception: PlaceAtReception(); break;
        }
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
        route = MarieOfficeRoute.Find(gameObject.scene);
        if (route == null) Debug.LogWarning("[Consultation] No MarieOfficeRoute in the scene; Marie walks straight to the conference room.");
        if (client != null)
        {
            life = client.GetComponent<NpcLife>();
            if (life == null) life = client.gameObject.AddComponent<NpcLife>();
        }
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
