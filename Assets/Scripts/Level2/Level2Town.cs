using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A point of Level 2's town data: world metres and the way a person faces (degrees).</summary>
[Serializable]
public class TownPoint
{
    public float x, y, z, yaw;
    public Vector3 Position => new Vector3(x, y, z);
    public Quaternion Facing => Quaternion.Euler(0f, yaw, 0f);
    /// <summary>JsonUtility fills a missing object with zeros: such a point is "not set".</summary>
    public bool IsSet => x != 0f || y != 0f || z != 0f;
}

/// <summary>One Level 2 townsperson (Resources/Levels/Level_02/town_life.json).</summary>
[Serializable]
public class TownResident
{
    public string id;
    public int model;
    public float height = 1.7f;
    public string idle;
    public string activity;
    public bool loop;
    public float[] pauseSeconds;
    public TownPoint seat;
    public List<TownPoint> route = new List<TownPoint>();
    public string partnerId;
    public bool leadsChat;
    public List<string> visitIds = new List<string>();
    public string arriveOnPhase;
    public TownPoint visitSpot;
}

/// <summary>Rosa's walk from her stall to the conference room (town_life.json "rosa").</summary>
[Serializable]
public class RosaWalkDefinition
{
    public float leaveDelaySeconds = 2.5f;
    public float receptionWaitSeconds = 40f;
    public string idle = "Idle Relaxed";
    public List<TownPoint> toReception = new List<TownPoint>();
    public List<TownPoint> toRoom = new List<TownPoint>();
}

[Serializable]
public class TownLifeFile
{
    public List<TownResident> residents = new List<TownResident>();
    public RosaWalkDefinition rosa = new RosaWalkDefinition();
    public TownPoint visitCenter;
    public float visitRadius = 26f;
}

/// <summary>
/// PURPOSE:
/// Pure rules for Rosa's walk to the office (T32), on the same leg model as
/// Marie's (MarieTripRules.Leg): Plaza (her stall) -> ToReception ->
/// AtReception (a fixed wait at the front desk) -> ToRoom -> InRoom. Read
/// from conditions every time (never one-shot events), so a milestone that
/// already happened can never strand her. No Unity calls.
/// </summary>
public static class RosaTripRules
{
    /// <summary>Her field conversation is committed, or a legacy first check/past field phase already exists.</summary>
    public static bool MarkTheMoneyStarted(CaseReviewState review) => review != null && review.phase >= ReviewPhase.Challenge &&
        (review.rosaEthicsCommitted || (review.challenge != null && review.challenge.firstChecked) ||
         review.phase >= ReviewPhase.OfficeReconciliation);

    /// <summary>
    /// The leg to start now (the same leg = keep going). She leaves her stall once Mark the Money! has
    /// begun and her short leave beat is over; at the front desk she moves on when her wait is over, or at
    /// once when the approved case needs her in the room (hurry).
    /// </summary>
    public static MarieTripRules.Leg Next(MarieTripRules.Leg current, bool started, bool leaveBeatOver, bool waitOver, bool hurry)
    {
        switch (current)
        {
            case MarieTripRules.Leg.Plaza:
                if (hurry) return MarieTripRules.Leg.ToRoom;
                return started && leaveBeatOver ? MarieTripRules.Leg.ToReception : MarieTripRules.Leg.Plaza;
            case MarieTripRules.Leg.AtReception:
                return waitOver || hurry ? MarieTripRules.Leg.ToRoom : MarieTripRules.Leg.AtReception;
            default:
                return current;
        }
    }

    /// <summary>
    /// Where a resumed review puts her (no walk, under the fade): the room when she was there or the
    /// review is past Mark the Money!'s field work; the front desk (her wait starts again) when she had
    /// set off; otherwise her stall.
    /// </summary>
    public static MarieTripRules.Leg Resume(string savedLeg, CaseReviewState review)
    {
        // Older pre-choice saves may already put her inside: restore her at the stall under the load fade.
        if (review != null && review.phase < ReviewPhase.OfficeReconciliation && !review.rosaEthicsCommitted &&
            (review.challenge == null || !review.challenge.firstChecked)) return MarieTripRules.Leg.Plaza;
        MarieTripRules.Leg saved = Parse(savedLeg);
        if (saved == MarieTripRules.Leg.InRoom || (review != null && review.phase >= ReviewPhase.OfficeReconciliation))
            return MarieTripRules.Leg.InRoom;
        if (saved != MarieTripRules.Leg.Plaza || MarkTheMoneyStarted(review)) return MarieTripRules.Leg.AtReception;
        return MarieTripRules.Leg.Plaza;
    }

    /// <summary>The saved value of a leg (walking legs save as the place they lead to, so a resume never starts mid-route).</summary>
    public static string ToSaved(MarieTripRules.Leg leg)
    {
        switch (leg)
        {
            case MarieTripRules.Leg.ToReception:
            case MarieTripRules.Leg.AtReception: return "reception";
            case MarieTripRules.Leg.ToRoom:
            case MarieTripRules.Leg.InRoom: return "room";
            default: return "";
        }
    }

    public static MarieTripRules.Leg Parse(string saved)
    {
        string value = saved == null ? "" : saved.Trim();
        if (value == "room") return MarieTripRules.Leg.InRoom;
        if (value == "reception") return MarieTripRules.Leg.AtReception;
        return MarieTripRules.Leg.Plaza;
    }

    /// <summary>Content checks shared with the regression suite (ids, models, points).</summary>
    public static List<string> Problems(TownLifeFile file, int modelCount)
    {
        var problems = new List<string>();
        if (file == null) { problems.Add("no file"); return problems; }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (file.residents != null)
            foreach (TownResident r in file.residents)
            {
                if (r == null) { problems.Add("an empty resident"); continue; }
                if (string.IsNullOrWhiteSpace(r.id) || !ids.Add(r.id)) problems.Add("a resident without a unique id");
                if (r.model < 0 || r.model >= modelCount) problems.Add(r.id + " names model " + r.model + " of " + modelCount);
                bool patrol = r.activity == "Patrol";
                if (patrol && (r.route == null || r.route.Count < 2)) problems.Add(r.id + " patrols fewer than two points");
                if (!patrol && (r.seat == null || !r.seat.IsSet) && (r.route == null || r.route.Count == 0)) problems.Add(r.id + " has no seat or spot");
            }
        RosaWalkDefinition rosa = file.rosa;
        if (rosa == null || rosa.toReception == null || rosa.toReception.Count == 0) problems.Add("Rosa has no walk to the front desk");
        if (rosa == null || rosa.toRoom == null || rosa.toRoom.Count == 0) problems.Add("Rosa has no walk to the conference room");
        if (rosa != null && (rosa.receptionWaitSeconds < 0f || rosa.leaveDelaySeconds < 0f)) problems.Add("Rosa's waits are negative");
        return problems;
    }
}

/// <summary>
/// PURPOSE:
/// Level 2's quieter town (T32), under Level2Gameplay/Town. The copied
/// Level 1 quest crowd stays off (office_start.json); the plaza holds Rosa
/// and a small authored cast from town_life.json, built once at
/// runtime on Level 1's own NPC components: NpcLife (one behaviour owner:
/// seated reading, or a slow patrol with pauses; it pauses and turns to the
/// player for a talk), NpcLocomotion, NpcAnimationDriver (the shared
/// Humanoid controller, idle variation and conversation motion), NpcBodyPose
/// (the seat), AmbientNpcInteractable + NpcStatusMarker (a replayable small
/// talk from Levels/Level_02/plaza_life.json, the shared marker and minimap
/// conventions). Talks are local colour only: no item, coins, trust, clue or
/// progress, and nothing is saved.
///
/// Also offers Rosa's walk data (Rosa) to Level2RosaConsultation.
/// Missing data or models leave the town quieter (one warning), never block
/// the investigation. Built once per scene; markers live under this object.
/// </summary>
public sealed class Level2Town : MonoBehaviour
{
    public const string ChildName = "Town";
    public const string ResourcePath = "Levels/Level_02/town_life";
    public const string WalkState = "Walking";
    public const string StrollState = "Strolling";

    public static Level2Town Instance { get; private set; }

    private TownLifeFile file;
    private bool built;
    private readonly List<AmbientNpcInteractable> residents = new List<AmbientNpcInteractable>();
    private readonly Dictionary<string, TownResident> residentData = new Dictionary<string, TownResident>(StringComparer.Ordinal);
    private readonly Dictionary<string, NpcLife> residentLives = new Dictionary<string, NpcLife>(StringComparer.Ordinal);
    private readonly Dictionary<string, Transform> residentHomes = new Dictionary<string, Transform>(StringComparer.Ordinal);
    private CaseReviewState subscribedReview;
    private string customerId;
    private bool customerAtRosa;
    private bool customerLeaving;
    private bool customerTripStarted;

    /// <summary>The townspeople built this visit (tests, guide observers).</summary>
    public IReadOnlyList<AmbientNpcInteractable> Residents => residents;

    /// <summary>Rosa's walk (never null; empty routes mean she is placed instead).</summary>
    public RosaWalkDefinition Rosa => Data.rosa ?? (Data.rosa = new RosaWalkDefinition());

    private TownLifeFile Data => file ?? (file = Load());

    private void OnEnable() => Instance = this;

    private void OnDisable()
    {
        UnsubscribeReview();
        if (Instance == this) Instance = null;
    }

    private void OnDestroy() => UnsubscribeReview();

    public static TownLifeFile Load()
    {
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        TownLifeFile loaded = null;
        if (asset != null)
        {
            try { loaded = JsonUtility.FromJson<TownLifeFile>(asset.text); }
            catch (Exception error) { Debug.LogWarning($"[Level2] {ResourcePath}.json is not valid JSON ({error.Message}); the town stays quiet."); }
        }
        else Debug.LogWarning($"[Level2] Resources/{ResourcePath}.json not found; the town stays quiet and Rosa is placed instead of walking.");
        loaded ??= new TownLifeFile();
        loaded.residents ??= new List<TownResident>();
        loaded.rosa ??= new RosaWalkDefinition();
        return loaded;
    }

    private IEnumerator Start()
    {
        float until = Time.unscaledTime + 6f;
        while (Time.unscaledTime < until && (CaseProgressionManager.Instance == null || !CaseProgressionManager.Instance.LevelStarted))
            yield return null;
        Build();
    }

    private void Build()
    {
        if (built) return;
        built = true;
        Level2SceneAssets assets = Level2SceneAssets.Load();
        if (assets == null || assets.npcController == null || assets.residentModels == null)
        {
            Debug.LogWarning("[Level2] scene_assets has no NPC controller or resident models; the town stays quiet.");
            return;
        }
        List<string> problems = RosaTripRules.Problems(Data, assets.residentModels.Length);
        if (problems.Count > 0) Debug.LogWarning("[Level2] town_life.json needs a look: " + string.Join("; ", problems));
        Transform receptionist = PlayerPlacement.Find(gameObject.scene, Level2RosaStall.ReceptionistPath);
        int layer = receptionist != null ? receptionist.gameObject.layer : gameObject.layer;
        foreach (TownResident resident in Data.residents)
        {
            if (resident == null || string.IsNullOrWhiteSpace(resident.id) || resident.model < 0 || resident.model >= assets.residentModels.Length ||
                assets.residentModels[resident.model] == null) continue;
            // One person per id, even if Build ran again.
            if (transform.Find("Resident " + resident.id) != null) continue;
            AmbientNpcInteractable built = BuildResident(resident, assets.residentModels[resident.model], assets.npcController, layer);
            if (built != null)
            {
                residents.Add(built);
                residentData[resident.id] = resident;
                residentLives[resident.id] = built.GetComponent<NpcLife>();
            }
        }
        ConfigureSocialRoutines();
        SubscribeReview();
        Debug.Log($"[Level2] Town: {residents.Count} townsperson/people ({string.Join(", ", residents.ConvertAll(r => r.AmbientId))}).");
    }

    private AmbientNpcInteractable BuildResident(TownResident data, GameObject prefab, RuntimeAnimatorController controller, int layer)
    {
        bool patrol = data.activity == "Patrol" && data.route != null && data.route.Count >= 2;
        bool visit = data.activity == "Visit";
        bool seated = !patrol && data.seat != null && data.seat.IsSet;
        TownPoint start = patrol ? data.route[0] : seated ? data.seat : data.route != null && data.route.Count > 0 ? data.route[0] : null;
        if (start == null) return null;
        // A seated person starts standing on the ground in front of the seat (NpcLife lowers them onto it).
        Vector3 feet = seated ? start.Position + start.Facing * Vector3.forward * 0.42f : start.Position;

        var holder = new GameObject("Resident " + data.id);
        holder.layer = layer;
        holder.transform.SetParent(transform, false);
        holder.transform.SetPositionAndRotation(NpcLife.Ground(feet), start.Facing);
        holder.SetActive(false);   // components configure before any of them runs a frame

        var visual = new GameObject("Visual").transform;
        visual.SetParent(holder.transform, false);
        GameObject model;
        try
        {
            model = Instantiate(prefab, visual);
        }
        catch (InvalidCastException error)
        {
            Debug.LogError($"[Level2] Could not instantiate resident '{data.id}' from model index {data.model}; skipping this resident. {error.Message}");
            Destroy(holder);
            return null;
        }
        model.name = prefab.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        FitHeight(model, holder.transform, Mathf.Clamp(data.height, 1.3f, 2f));
        foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
        {
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        var capsule = holder.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, data.height * 0.5f, 0f);
        capsule.height = data.height;
        capsule.radius = 0.32f;

        AmbientNpcInteractable talk = holder.AddComponent<AmbientNpcInteractable>();
        AmbientNpcDefinition words = PlazaLifeContent.Find(data.id);
        talk.Configure(data.id, words != null && !string.IsNullOrWhiteSpace(words.name) ? words.name : "Townsperson");
        // AmbientNpcInteractable requires NpcStateMachine, which in turn
        // requires NpcAnimationDriver. Reuse that required component instead
        // of trying to add a duplicate (NpcAnimationDriver disallows multiples).
        NpcAnimationDriver driver = holder.GetComponent<NpcAnimationDriver>();
        if (driver == null) driver = holder.AddComponent<NpcAnimationDriver>();
        driver.ConfigureStates(string.IsNullOrWhiteSpace(data.idle) ? "Standing Idle" : data.idle, WalkState, StrollState);
        NpcLife life = holder.AddComponent<NpcLife>();
        Vector2 pause = data.pauseSeconds != null && data.pauseSeconds.Length >= 2 ? new Vector2(data.pauseSeconds[0], data.pauseSeconds[1]) : new Vector2(3f, 6f);
        Transform home = Marker(data.id + " Home", start);
        residentHomes[data.id] = home;
        if (patrol)
        {
            var points = new Transform[data.route.Count];
            for (int i = 0; i < points.Length; i++) points[i] = Marker(data.id + " Route " + (i + 1), data.route[i]);
            life.ConfigureRuntime(NpcLife.Activity.Patrol, points[0], null, points, pause, data.loop, true);
        }
        else if (visit)
        {
            life.ConfigureRuntime(NpcLife.Activity.Visit, home, null, null, pause, false, true);
        }
        else if (seated)
        {
            life.ConfigureRuntime(NpcLife.Activity.Stand, null, Marker(data.id + " Seat", data.seat), null, pause, false, true);
        }
        else
        {
            life.ConfigureRuntime(NpcLife.Activity.Stand, home, null, null, pause, false, true);
        }
        holder.SetActive(true);
        return talk;
    }

    private void ConfigureSocialRoutines()
    {
        Transform visitCenter = Data.visitCenter != null && Data.visitCenter.IsSet
            ? Marker("Guard Visit Center", Data.visitCenter)
            : transform;

        foreach (KeyValuePair<string, TownResident> pair in residentData)
        {
            TownResident data = pair.Value;
            NpcLife life = residentLives[pair.Key];
            if (life == null) continue;

            if (!string.IsNullOrWhiteSpace(data.partnerId) && residentLives.TryGetValue(data.partnerId, out NpcLife partner))
                life.ConfigureRuntimeChat(partner, data.leadsChat);

            if (data.activity == "Visit")
            {
                var targets = new List<NpcLife>();
                if (data.visitIds != null)
                    foreach (string id in data.visitIds)
                        if (id != pair.Key && residentLives.TryGetValue(id, out NpcLife target) && target != null)
                            targets.Add(target);
                life.ConfigureRuntimeVisits(targets.ToArray(), visitCenter, Data.visitRadius, 1.5f, new Vector2(3f, 5f));
            }
        }
    }

    private void SubscribeReview()
    {
        UnsubscribeReview();
        subscribedReview = Level2OfficeStart.Review;
        if (subscribedReview == null) return;
        subscribedReview.PhaseChanged += OnReviewPhaseChanged;
        if (subscribedReview.phase == ReviewPhase.MarketVisit) BeginRosaCustomerVisit();
    }

    private void UnsubscribeReview()
    {
        if (subscribedReview != null) subscribedReview.PhaseChanged -= OnReviewPhaseChanged;
        subscribedReview = null;
    }

    private void OnReviewPhaseChanged(ReviewPhase from, ReviewPhase to)
    {
        if (to == ReviewPhase.MarketVisit) BeginRosaCustomerVisit();
    }

    private void BeginRosaCustomerVisit()
    {
        if (customerTripStarted) return;
        StartCoroutine(BeginRosaCustomerVisitWhenReady());
    }

    private IEnumerator BeginRosaCustomerVisitWhenReady()
    {
        customerTripStarted = true;
        TownResident customerData = null;
        NpcLife customer = null;
        Transform home = null;
        foreach (KeyValuePair<string, TownResident> pair in residentData)
        {
            if (pair.Value.arriveOnPhase != nameof(ReviewPhase.MarketVisit) || pair.Value.visitSpot == null || !pair.Value.visitSpot.IsSet) continue;
            if (!residentLives.TryGetValue(pair.Key, out customer) || customer == null || !residentHomes.TryGetValue(pair.Key, out home)) continue;
            customerData = pair.Value;
            customerId = pair.Key;
            break;
        }
        if (customerData == null) yield break;

        Level2RosaStall stall = transform.parent != null ? transform.parent.GetComponentInChildren<Level2RosaStall>(true) : null;
        float until = Time.unscaledTime + 8f;
        while (Time.unscaledTime < until && (stall == null || stall.Rosa == null))
        {
            if (stall == null && transform.parent != null) stall = transform.parent.GetComponentInChildren<Level2RosaStall>(true);
            yield return null;
        }
        RosaInteractable rosa = stall != null ? stall.Rosa : null;
        if (rosa == null)
        {
            customerTripStarted = false;
            Debug.LogWarning("[Level2] Rosa's customer could not find her at the stall; the optional visit was skipped.");
            yield break;
        }

        Vector3 destination = customerData.visitSpot.Position;
        Vector3 towardRosa = rosa.transform.position - destination;
        Quaternion facing = towardRosa.sqrMagnitude > 0.01f ? Quaternion.LookRotation(towardRosa.normalized) : rosa.transform.rotation;
        customerAtRosa = false;
        customerLeaving = false;
        customer.TravelForQuest(new[] { destination }, facing, () =>
        {
            customerAtRosa = true;
            NpcLife rosaLife = rosa.GetComponent<NpcLife>();
            if (rosaLife != null)
            {
                customer.ConfigureRuntimeChat(rosaLife, true);
                rosaLife.ConfigureRuntimeChat(customer, false);
            }
        });
    }

    /// <summary>Called only after the player's conversation with Rosa has been admitted.</summary>
    public void OnRosaDialogueStarted(RosaInteractable rosa)
    {
        if (!customerAtRosa || customerLeaving || string.IsNullOrWhiteSpace(customerId) || rosa == null) return;
        if (!residentLives.TryGetValue(customerId, out NpcLife customer) || customer == null || !residentHomes.TryGetValue(customerId, out Transform home)) return;

        customerLeaving = true;
        customerAtRosa = false;
        customer.ConfigureRuntimeChat(null, false);
        NpcLife rosaLife = rosa.GetComponent<NpcLife>();
        if (rosaLife != null) rosaLife.ConfigureRuntimeChat(null, false);
        Vector3 direction = home.position - customer.transform.position;
        Quaternion facing = direction.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direction.normalized) : home.rotation;
        customer.TravelForQuest(new[] { home.position }, facing, () => customerLeaving = false);
    }

    /// <summary>A data point as a marker transform (NpcLife reads markers: position, +Z the facing).</summary>
    private Transform Marker(string name, TownPoint point)
    {
        var marker = new GameObject(name).transform;
        marker.SetParent(transform, false);
        marker.SetPositionAndRotation(point.Position, point.Facing);
        return marker;
    }

    /// <summary>Scales the model to height metres and stands its soles on the holder's origin (whatever its import scale).</summary>
    private static void FitHeight(GameObject model, Transform holder, float height)
    {
        Bounds b = RendererBounds(model);
        if (b.size.y < 0.01f) return;
        model.transform.localScale *= height / b.size.y;
        b = RendererBounds(model);
        model.transform.position += Vector3.up * (holder.position.y - b.min.y);
    }

    private static Bounds RendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
