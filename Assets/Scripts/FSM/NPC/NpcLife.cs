using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// One NPC's everyday behaviour (Level 1 Living Plaza), with one order of
/// priority so movement, animation, dialogue and quests never give
/// conflicting commands:
///   1. Quest travel (TravelForQuest / PlaceForQuest): a required move owned
///      by the quest code. Nothing below can cancel or redirect it; it always
///      ends at its destination (walking, or a staged move nobody sees).
///   2. The player's conversation with this NPC: what it is doing pauses on
///      the spot (a walk stops, a seated NPC that stands to talk gets up),
///      it turns smoothly to the player, and it resumes after a short beat.
///   3. Its activity: stand at home, sit on a seat, stroll a short patrol,
///      browse a counter, chat with a partner.
///   4. Idle: the clip plus NpcAnimationDriver's own idle variation.
///
/// It never starts or ends a dialogue and never changes quest, score, item
/// or save state; arriving somewhere is never a quest event by itself.
/// Paths are planned only when a trip starts (NpcLocomotion), never per frame.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-10)]
public sealed class NpcLife : MonoBehaviour
{
    public enum Activity { Stand, Patrol, Browse, Visit }

    /// <summary>Every NPC with a life in the scene (spacing and pair chats read this).</summary>
    public static readonly List<NpcLife> All = new List<NpcLife>();

    [Header("Activity")]
    [SerializeField] private Activity activity = Activity.Stand;
    [Tooltip("Where it stands; its +Z is the way it faces. Empty: where it is placed in the scene.")]
    [SerializeField] private Transform home;
    [Tooltip("Optional seat: a marker on the seat surface under the pelvis, +Z facing away from the backrest. Humanoid models sit here; others stand in front of it.")]
    [SerializeField] private Transform seat;
    [Tooltip("Patrol: the points it strolls between in order (and back); each point's +Z is where it looks while pausing.")]
    [SerializeField] private Transform[] route;
    [SerializeField] private Vector2 pauseSeconds = new Vector2(3f, 6f);
    [Tooltip("Patrol: wrap from the last route point to the first instead of walking back.")]
    [SerializeField] private bool loopRoute;
    [Tooltip("Patrol: first route point to use when this life starts.")]
    [SerializeField, Min(0)] private int startRouteIndex;
    [Tooltip("Browse: the counter point it looks at and reaches toward.")]
    [SerializeField] private Transform focus;
    [Tooltip("Browse: use the runtime Supply Counter's customer side (its built collider) instead of the home marker.")]
    [SerializeField] private bool browseSupplyCounter;
    [Tooltip("Ambient NPCs: put the feet on the ground under the authored spot when the scene starts.")]
    [SerializeField] private bool groundOnStart;

    [Header("Visits")]
    [Tooltip("Visit: plaza NPCs in the order to visit. Each target is sampled once when its trip starts.")]
    [SerializeField] private NpcLife[] visitTargets;
    [Tooltip("Visit: stop this far from the target's sampled feet position.")]
    [SerializeField, Min(0.5f)] private float visitDistance = 1.5f;
    [SerializeField] private Vector2 visitDurationSeconds = new Vector2(3f, 5f);
    [Tooltip("Visit: center of the plaza. Targets outside this radius are skipped, including NPCs moved indoors.")]
    [SerializeField] private Transform visitAreaCenter;
    [SerializeField, Min(1f)] private float visitAreaRadius = 18f;

    [Header("Chat partner")]
    [SerializeField] private NpcLife partner;
    [Tooltip("The one of the pair that keeps the chat going (exactly one should).")]
    [SerializeField] private bool leadsChat;

    [Header("Meet and wave")]
    [Tooltip("One member of a walking pair periodically meets and waves to its partner.")]
    [SerializeField] private bool meetAndWave;
    [Tooltip("Shared rendezvous point. Both NPCs walk to opposite sides; empty means they meet only when already close.")]
    [SerializeField] private Transform meetPoint;
    [SerializeField, Min(0.5f)] private float meetDistance = 2.5f;
    [SerializeField] private Vector2 meetCooldownSeconds = new Vector2(25f, 40f);
    [SerializeField] private Vector2 meetDurationSeconds = new Vector2(2.5f, 4f);

    [Header("The player")]
    [Tooltip("Turns its body to face the player in its own conversation.")]
    [SerializeField] private bool turnToTalk = true;
    [Tooltip("Seated: stands up for its own conversation.")]
    [SerializeField] private bool standToTalk = true;
    [Tooltip("Seated: stands when the player comes this close while it has something to say (marker shown). 0 = never.")]
    [SerializeField, Min(0f)] private float standForPlayerDistance;
    [Tooltip("Side-quest client: on a call (right hand at the ear, strolling the route) until the plaza rounds open.")]
    [SerializeField] private bool phoneUntilRounds;

    [Header("Optional follow-up talk")]
    [Tooltip("Allow a replayable, no-reward line after this taxpayer's initial talk.")]
    [SerializeField] private bool enableFollowUpSmallTalk;
    [SerializeField, TextArea(1, 3)] private string followUpSmallTalkLine = "Good to see you again. I hope your day is going well.";

    private const float StandOffset = 0.42f;
    private const float FaceSpeed = 150f;
    private const float MinimumSocialApproachSeconds = 30f;
    private const float MaximumSocialApproachSeconds = 90f;

    private NpcLocomotion mover;
    private NpcBodyPose pose;
    private NpcAnimationDriver driver;
    private DialogueUI dialogue;
    private INpcAvailabilitySource availability;
    private TaxpayerNpcInteractable taxpayer;

    private Vector3 homeFeet;
    private Quaternion homeFacing;
    private bool started;
    private bool questPlaced;

    private bool questLocked, questPending;
    private readonly List<Vector3> questRoute = new List<Vector3>();
    private Quaternion questFacing;
    private Action questArrived;

    private bool inDialogue;
    private float resumeAt;

    private int routeIndex;
    private int routeStep = 1;
    private float pauseUntil;
    private bool counterResolved;
    private float counterGiveUp;
    private Vector3 reachPoint;
    private float nextReachAt, reachUntil;

    private bool playerNear;
    private float farSince;
    private float chatSwitchAt;
    private bool chatLeaderTalks, chatPause;

    private enum SocialKind { None, Meet, Visit }
    private enum SocialPhase { None, Approaching, Paused }
    private static NpcLife socialOwner;
    private static NpcLife socialPartner;
    private SocialKind socialKind;
    private SocialPhase socialPhase;
    private NpcLife socialTarget;
    private Vector3 socialOwnerStop, socialTargetStop, visitSamplePosition;
    private Quaternion socialOwnerFacing, socialTargetFacing;
    private bool socialOwnerArrived, socialTargetArrived;
    private float socialApproachDeadline, socialUntil, socialSwitchAt;
    private bool socialOwnerTalking;
    private int visitIndex;
    private float nextVisitRetryAt, nextMeetAt;

    /// <summary>A required quest move is under way (nothing ambient runs).</summary>
    public bool QuestLocked => questLocked;

    /// <summary>The player is talking with this NPC right now.</summary>
    public bool InConversation => inDialogue;

    /// <summary>Whether this NPC is currently reserved for a visit or meet.</summary>
    public bool InSocialPause => IsSocialReserved(this);

    /// <summary>Where it stands when idle (feet), and the way it faces there.</summary>
    public Vector3 HomeFeet => homeFeet;

    /// <summary>The scene-authored seat, or null.</summary>
    public Transform Seat => seat;

    /// <summary>The movement component (added when missing).</summary>
    public NpcLocomotion Mover => mover != null ? mover : (mover = GetComponent<NpcLocomotion>() ?? gameObject.AddComponent<NpcLocomotion>());

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        CancelSocialPause(this);
        All.Remove(this);
        if (driver != null) driver.SetAmbientConversation(null, false);
        if (pose != null)
        {
            pose.Reach(null);
            pose.Phone(false);
        }
    }

    private void Start()
    {
        driver = GetComponent<NpcAnimationDriver>();
        dialogue = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        availability = GetComponent<INpcAvailabilitySource>();
        taxpayer = GetComponent<TaxpayerNpcInteractable>();
        pose = GetComponent<NpcBodyPose>();
        if (pose == null && (seat != null || activity == Activity.Browse || phoneUntilRounds || meetAndWave))
            pose = gameObject.AddComponent<NpcBodyPose>();
        _ = Mover;
        // A quest placement before this Start (resume ordering) wins over the authored home.
        if (questPlaced)
        {
            started = true;
            return;
        }

        if (groundOnStart)
        {
            // Ambient models have their pivot at the soles: stand exactly on the ground.
            Vector3 at = home != null ? home.position : transform.position;
            Mover.SetFeetOffset(0f);
            Mover.PlaceAt(Ground(at), home != null ? Yaw(home) : Quaternion.LookRotation(Flat(Mover.Facing)));
        }

        homeFeet = home != null ? Ground(home.position) : Mover.Feet;
        homeFacing = home != null ? Yaw(home) : Quaternion.LookRotation(Flat(Mover.Facing));

        if (seat != null)
        {
            // The NPC stands on the ground in front of the seat; the pose layer lowers it onto the seat.
            Vector3 forward = Flat(seat.forward).normalized;
            homeFeet = Ground(seat.position + forward * StandOffset);
            homeFacing = Quaternion.LookRotation(forward);
            Mover.PlaceAt(homeFeet, homeFacing);
            if (pose != null && pose.Supported)
            {
                pose.SetSeat(seat.position, forward);
                pose.SnapSit(true);
            }
        }

        if (route != null && route.Length > 0 && activity == Activity.Patrol)
            routeIndex = Mathf.Clamp(startRouteIndex, 0, route.Length - 1);
        if (browseSupplyCounter) counterGiveUp = Time.time + 12f;
        nextReachAt = Time.time + UnityEngine.Random.Range(2f, 5f);
        chatSwitchAt = Time.time + UnityEngine.Random.Range(0.5f, 2f);
        if (meetAndWave) nextMeetAt = Time.time + RandomRange(meetCooldownSeconds);
        started = true;
    }

    // =========================================================
    // QUEST TRAVEL (priority 1)
    // =========================================================

    /// <summary>
    /// A required move: walks <paramref name="routeStops"/> (doorway points,
    /// then the destination), turns to <paramref name="facing"/> and calls
    /// <paramref name="arrived"/> once. A seated NPC stands first. If the
    /// route fails it is placed out of view instead (never a visible jump).
    /// Its activity becomes standing at the destination.
    /// </summary>
    public void TravelForQuest(IList<Vector3> routeStops, Quaternion facing, Action arrived)
    {
        if (routeStops == null || routeStops.Count == 0) return;
        CancelSocialPause(this);
        questLocked = true;
        questPending = true;
        questRoute.Clear();
        questRoute.AddRange(routeStops);
        questFacing = facing;
        questArrived = arrived;
        Mover.Suspended = false;
        Mover.Stop(NpcLocomotion.Priority.Ambient);
        CalmBody();
    }

    /// <summary>
    /// Puts the NPC at a quest spot at once (restore under a fade, or an
    /// instant hand-over): no walk, no callback. Its activity becomes
    /// standing there.
    /// </summary>
    public void PlaceForQuest(Vector3 feet, Quaternion facing)
    {
        CancelSocialPause(this);
        questLocked = questPending = false;
        questArrived = null;
        questPlaced = true;
        SettleHome(feet, facing);
        if (pose != null) pose.SnapSit(false);
        CalmBody();
        Mover.PlaceAt(feet, facing);
    }

    private void UpdateQuest()
    {
        if (!questPending) return;
        // Stand up before walking off.
        if (pose != null && !pose.IsStanding)
        {
            pose.Sit(false);
            return;
        }
        questPending = false;
        Vector3 destination = questRoute[questRoute.Count - 1];
        Mover.Go(questRoute, questFacing, NpcLocomotion.Priority.Quest, false, arrivedOk =>
        {
            if (arrivedOk) FinishQuest(destination);
            else Mover.StageWhenUnseen(destination, questFacing, () => FinishQuest(destination));
        });
    }

    private void FinishQuest(Vector3 destination)
    {
        questLocked = false;
        SettleHome(destination, questFacing);
        Action callback = questArrived;
        questArrived = null;
        callback?.Invoke();
    }

    /// <summary>After a quest move the NPC simply stands at its new place.</summary>
    private void SettleHome(Vector3 feet, Quaternion facing)
    {
        homeFeet = feet;
        homeFacing = Quaternion.LookRotation(Flat(facing * Vector3.forward));
        activity = Activity.Stand;
        seat = null;
        partner = null;
        phoneUntilRounds = false;
        browseSupplyCounter = false;
    }

    private void CalmBody()
    {
        if (pose != null)
        {
            pose.Reach(null);
            pose.Phone(false);
        }
        if (driver != null) driver.SetAmbientConversation(null, false);
    }

    // =========================================================
    // FRAME
    // =========================================================

    private void Update()
    {
        if (!started) return;
        bool talking = OwnsDialogue();
        if (talking != inDialogue)
        {
            if (talking) BeginConversation();
            else EndConversation();
        }
        if (inDialogue)
        {
            UpdateConversation();
            return;
        }
        if (Time.time < resumeAt) return;

        if (socialOwner == this)
        {
            UpdateSocialPause();
            return;
        }
        if (socialPartner == this && socialOwner != null &&
            (socialOwner.socialKind == SocialKind.Meet || socialOwner.socialPhase == SocialPhase.Paused))
            return;

        if (partner != null && leadsChat && !IsSocialReserved(this) && !IsSocialReserved(partner)) UpdateChat();

        if (Mover.Suspended) Mover.Suspended = false;

        if (questLocked)
        {
            UpdateQuest();
            return;
        }

        // Keep taxpayer clients beside their uncollected paper, but let them
        // resume the route after collection is confirmed.
        if (!MayWalk && Mover.IsTraveling && Mover.CurrentPriority == NpcLocomotion.Priority.Ambient)
        {
            Mover.Stop(NpcLocomotion.Priority.Ambient);
            homeFeet = Mover.Feet;
            homeFacing = Quaternion.LookRotation(Flat(Mover.Facing));
        }

        if (meetAndWave && activity == Activity.Patrol && Time.time >= nextMeetAt && TryStartMeet()) return;

        switch (activity)
        {
            case Activity.Patrol: UpdatePatrol(); break;
            case Activity.Browse: UpdateBrowse(); break;
            case Activity.Visit: UpdateVisit(); break;
            default:
                if (seat != null) UpdateSeat();
                else KeepHome();
                break;
        }
    }

    // =========================================================
    // THE PLAYER'S CONVERSATION (priority 2)
    // =========================================================

    private bool OwnsDialogue()
    {
        if (dialogue == null || !dialogue.IsDialogueActive) return false;
        Component owner = dialogue.CurrentOwner as Component;
        return owner != null && (owner.transform == transform || owner.transform.IsChildOf(transform));
    }

    private void BeginConversation()
    {
        CancelSocialPause(this);
        inDialogue = true;
        Mover.Suspended = true;
        if (pose != null)
        {
            pose.Reach(null);
            // A waiting client stays on the call for her "just finishing a call" line.
            if (!(phoneUntilRounds && WaitingForRounds)) pose.Phone(false);
            if (seat != null && standToTalk) pose.Sit(false);
        }
        if (driver != null) driver.SetAmbientConversation(null, false);
    }

    private void UpdateConversation()
    {
        if (!turnToTalk) return;
        if (pose != null && !pose.IsStanding) return;   // never turn on a seat; turn once up
        Transform player = Player;
        if (player != null) Mover.TurnToward(player.position - transform.position, FaceSpeed);
    }

    private void EndConversation()
    {
        inDialogue = false;
        // A short beat (the driver's farewell nod) before resuming.
        resumeAt = Time.time + UnityEngine.Random.Range(0.8f, 1.4f);
        pauseUntil = Mathf.Max(pauseUntil, resumeAt + 1f);
        nextReachAt = Mathf.Max(nextReachAt, resumeAt + 2f);
    }

    // =========================================================
    // ACTIVITIES (priority 3)
    // =========================================================

    /// <summary>
    /// A side-quest client whose talk is done stays where it is until a quest
    /// move (its profile paper lies beside it, parented to its root).
    /// </summary>
    private bool MayWalk => taxpayer == null || !taxpayer.HasPendingHandover;

    /// <summary>Returns the configured repeatable small-talk line for a protected patrol client.</summary>
    public bool TryGetFollowUpSmallTalkLine(out string line)
    {
        line = followUpSmallTalkLine;
        return enableFollowUpSmallTalk && activity == Activity.Patrol && !questLocked &&
               !string.IsNullOrWhiteSpace(line);
    }

    /// <summary>Stand at home; face the chat partner, or the home facing.</summary>
    private void KeepHome()
    {
        if (Mover.IsTraveling) return;
        if (!MayWalk) homeFeet = Mover.Feet;
        if (Flat(Mover.Feet - homeFeet).magnitude > 0.35f)
        {
            Mover.Go(new[] { homeFeet }, homeFacing, NpcLocomotion.Priority.Ambient, true, null);
            return;
        }
        Vector3 want = partner != null ? partner.transform.position - transform.position : homeFacing * Vector3.forward;
        Mover.TurnToward(want, FaceSpeed * 0.5f);
    }

    private void UpdateSeat()
    {
        if (pose == null || !pose.Supported)
        {
            KeepHome();
            return;
        }
        // Stand to greet a player who comes close while there is something to say; sit back after.
        Transform player = Player;
        float distance = player != null ? Flat(player.position - homeFeet).magnitude : float.MaxValue;
        bool wantsToTalk = availability != null && availability.Availability == NpcAvailability.Available;
        if (standForPlayerDistance > 0f && wantsToTalk && distance <= standForPlayerDistance)
        {
            playerNear = true;
            farSince = -1f;
        }
        else if (playerNear)
        {
            if (farSince < 0f) farSince = Time.time;
            if (!wantsToTalk || (distance > standForPlayerDistance + 2f && Time.time - farSince > 3f)) playerNear = false;
        }

        if (playerNear)
        {
            pose.Sit(false);
            if (pose.IsStanding && player != null) Mover.TurnToward(player.position - transform.position, FaceSpeed * 0.6f);
            return;
        }

        // Back to the seat: face it squarely first, then sit.
        float angle = Mover.TurnToward(homeFacing * Vector3.forward, FaceSpeed * 0.6f);
        if (angle < 4f || !pose.IsStanding) pose.Sit(true);
    }

    private void UpdatePatrol()
    {
        bool onCall = phoneUntilRounds && WaitingForRounds;
        if (pose != null) pose.Phone(onCall);
        if (phoneUntilRounds && !onCall)
        {
            // Off the call: she stays put (her talk and profile happen here).
            KeepHome();
            return;
        }
        if (route == null || route.Length == 0 || !MayWalk)
        {
            KeepHome();
            return;
        }
        if (Mover.IsTraveling || Time.time < pauseUntil) return;

        Transform point = route[Mathf.Clamp(routeIndex, 0, route.Length - 1)];
        if (point == null)
        {
            Advance();
            return;
        }
        Vector3 target = Ground(point.position);
        if (Flat(Mover.Feet - target).magnitude < 0.3f)
        {
            Advance();
            return;
        }
        Mover.Go(new[] { target }, Yaw(point), NpcLocomotion.Priority.Ambient, true, ok =>
        {
            pauseUntil = Time.time + UnityEngine.Random.Range(pauseSeconds.x, Mathf.Max(pauseSeconds.x, pauseSeconds.y));
            Advance();
        });
    }

    private void Advance()
    {
        if (route.Length < 2)
        {
            routeIndex = 0;
            return;
        }
        if (loopRoute)
        {
            routeIndex = (routeIndex + 1) % route.Length;
            return;
        }
        if (routeIndex + routeStep >= route.Length || routeIndex + routeStep < 0) routeStep = -routeStep;
        routeIndex += routeStep;
    }

    private void UpdateBrowse()
    {
        if (browseSupplyCounter && !counterResolved && !ResolveCounter()) return;
        if (Mover.IsTraveling) return;
        if (Flat(Mover.Feet - homeFeet).magnitude > 0.35f)
        {
            Mover.Go(new[] { homeFeet }, homeFacing, NpcLocomotion.Priority.Ambient, true, null);
            return;
        }
        Mover.TurnToward(homeFacing * Vector3.forward, FaceSpeed * 0.5f);
        if (pose == null) return;

        // Now and then a hand goes to something on the counter, then back.
        float now = Time.time;
        if (reachUntil > 0f && now > reachUntil)
        {
            pose.Reach(null);
            reachUntil = 0f;
            nextReachAt = now + UnityEngine.Random.Range(4.5f, 8.5f);
        }
        else if (reachUntil <= 0f && now >= nextReachAt)
        {
            Vector3 along = Vector3.Cross(Vector3.up, homeFacing * Vector3.forward);
            reachPoint = FocusPoint + along * UnityEngine.Random.Range(-0.22f, 0.22f);
            pose.Reach(reachPoint, UnityEngine.Random.value > 0.25f);
            reachUntil = now + UnityEngine.Random.Range(1.3f, 2.2f);
        }
    }

    private Vector3 counterFocus;
    private bool hasCounterFocus;

    private Vector3 FocusPoint =>
        hasCounterFocus ? counterFocus : focus != null ? focus.position : homeFeet + homeFacing * Vector3.forward * 0.6f + Vector3.up * 1f;

    /// <summary>The customer side of the built Supply Counter (it is created at runtime).</summary>
    private bool ResolveCounter()
    {
        SupplyCounter counter = SupplyCounter.Instance;
        BoxCollider body = counter != null ? counter.GetComponent<BoxCollider>() : null;
        if (body == null)
        {
            if (Time.time < counterGiveUp) return false;
            counterResolved = true;   // no counter: browse at the authored marker
            return true;
        }
        Transform root = counter.transform;
        Vector3 c = body.center, s = body.size;
        // Toward the end without the crates (local -X), just outside the customer face (local +Z).
        Vector3 stand = root.TransformPoint(new Vector3(c.x - s.x * 0.36f, 0f, c.z + s.z * 0.5f + 0.5f));
        counterFocus = root.TransformPoint(new Vector3(c.x - s.x * 0.3f, c.y + s.y * 0.5f + 0.05f, c.z + s.z * 0.15f));
        hasCounterFocus = true;
        homeFeet = Ground(stand);
        homeFacing = Quaternion.LookRotation(Flat(-root.forward));
        counterResolved = true;
        // Placed at once while the level is still fading in; otherwise she walks over.
        if (Time.timeSinceLevelLoad < 4f) Mover.PlaceAt(homeFeet, homeFacing);
        return true;
    }

    // =========================================================
    // VISITS
    // =========================================================

    private void UpdateVisit()
    {
        if (Mover.IsTraveling || Time.time < nextVisitRetryAt || visitTargets == null || visitTargets.Length == 0)
            return;

        for (int offset = 0; offset < visitTargets.Length; offset++)
        {
            int index = (visitIndex + offset) % visitTargets.Length;
            NpcLife target = visitTargets[index];
            if (!CanVisitTarget(target)) continue;
            visitIndex = (index + 1) % visitTargets.Length;
            StartVisit(target);
            return;
        }

        nextVisitRetryAt = Time.time + 3f;
    }

    private void StartVisit(NpcLife target)
    {
        if (!TryReserveSocialPause(target, SocialKind.Visit)) return;

        // This position is deliberately a one-time sample. If the target moves
        // away during the trip, this visit is skipped instead of being chased.
        visitSamplePosition = target.Mover.Feet;
        Vector3 away = Flat(Mover.Feet - visitSamplePosition);
        if (away.sqrMagnitude < 0.001f) away = -Flat(target.Mover.Facing);
        if (away.sqrMagnitude < 0.001f) away = Vector3.back;
        socialOwnerStop = Ground(visitSamplePosition + away.normalized * visitDistance);
        Vector3 towardTarget = Flat(visitSamplePosition - socialOwnerStop);
        socialOwnerFacing = Quaternion.LookRotation(towardTarget);
        socialTargetFacing = Quaternion.LookRotation(-towardTarget);
        socialOwnerArrived = false;
        socialApproachDeadline = Time.time + SocialApproachSeconds(Flat(socialOwnerStop - Mover.Feet).magnitude);

        if (!Mover.Go(new[] { socialOwnerStop }, socialOwnerFacing, NpcLocomotion.Priority.Ambient, true, arrived =>
        {
            if (socialOwner != this) return;
            if (!arrived) EndSocialPause(false);
            else socialOwnerArrived = true;
        }))
            EndSocialPause(false);
    }

    private bool CanVisitTarget(NpcLife target, bool checkArea = true)
    {
        bool reservedByThisVisit = socialOwner == this && socialPartner == target && socialKind == SocialKind.Visit;
        if (target == null || target == this || !target.isActiveAndEnabled || target.questLocked ||
            target.inDialogue || target.OwnsDialogue() || Time.time < target.resumeAt ||
            (IsSocialReserved(target) && !reservedByThisVisit)) return false;

        if (target.taxpayer == null) target.taxpayer = target.GetComponent<TaxpayerNpcInteractable>();
        if (target.taxpayer != null && target.taxpayer.IsWaitingForRound) return false;

        TutorialNpcController guide = target.GetComponent<TutorialNpcController>();
        if (guide != null && !guide.ModelRoot.gameObject.activeInHierarchy) return false;

        if (!checkArea) return true;
        Vector3 center = visitAreaCenter != null ? visitAreaCenter.position : homeFeet;
        return Flat(target.transform.position - center).magnitude <= visitAreaRadius;
    }

    // =========================================================
    // MEET AND WAVE
    // =========================================================

    private bool TryStartMeet()
    {
        if (partner == null || !partner.isActiveAndEnabled || partner.activity != Activity.Patrol ||
            !MayWalk || !partner.MayWalk || questLocked || partner.questLocked || inDialogue || partner.inDialogue ||
            partner.OwnsDialogue() || Time.time < resumeAt || Time.time < partner.resumeAt ||
            Mover.Suspended || partner.Mover.Suspended || IsSocialReserved(this) || IsSocialReserved(partner))
            return false;

        Vector3 between = Flat(partner.transform.position - transform.position);
        if (meetPoint == null && between.magnitude > meetDistance) return false;
        if (!TryReserveSocialPause(partner, SocialKind.Meet)) return false;

        if (meetPoint == null)
        {
            Mover.Stop(NpcLocomotion.Priority.Ambient);
            partner.Mover.Stop(NpcLocomotion.Priority.Ambient);
            BeginSocialPause(true);
            return true;
        }

        Vector3 side = Flat(meetPoint.right);
        if (side.sqrMagnitude < 0.001f) side = Vector3.right;
        side.Normalize();
        Vector3 toward = side * (meetDistance * 0.5f);
        socialOwnerStop = Ground(meetPoint.position - toward);
        socialTargetStop = Ground(meetPoint.position + toward);
        Vector3 ownerToPartner = Flat(socialTargetStop - socialOwnerStop);
        socialOwnerFacing = Quaternion.LookRotation(ownerToPartner);
        socialTargetFacing = Quaternion.LookRotation(-ownerToPartner);
        socialOwnerArrived = socialTargetArrived = false;
        socialApproachDeadline = Time.time + SocialApproachSeconds(Mathf.Max(
            Flat(socialOwnerStop - Mover.Feet).magnitude,
            Flat(socialTargetStop - partner.Mover.Feet).magnitude));

        if (!Mover.Go(new[] { socialOwnerStop }, socialOwnerFacing, NpcLocomotion.Priority.Ambient, true, arrived =>
        {
            if (socialOwner != this) return;
            if (!arrived) EndSocialPause(false);
            else socialOwnerArrived = true;
        }) || socialOwner != this)
        {
            EndSocialPause(false);
            return true;
        }
        if (!partner.Mover.Go(new[] { socialTargetStop }, socialTargetFacing, NpcLocomotion.Priority.Ambient, true, arrived =>
        {
            if (socialOwner != this) return;
            if (!arrived) EndSocialPause(false);
            else socialTargetArrived = true;
        }))
            EndSocialPause(false);
        return true;
    }

    private void UpdateSocialPause()
    {
        NpcLife other = socialTarget;
        if (other == null || !other.isActiveAndEnabled || inDialogue || other.inDialogue ||
            OwnsDialogue() || other.OwnsDialogue() || questLocked || other.questLocked)
        {
            EndSocialPause(false);
            return;
        }

        if (socialPhase == SocialPhase.Approaching)
        {
            if (Time.time >= socialApproachDeadline)
            {
                EndSocialPause(false);
                return;
            }
            if (socialKind == SocialKind.Meet)
            {
                if (!MayWalk || !other.MayWalk)
                {
                    EndSocialPause(false);
                    return;
                }
                if (socialOwnerArrived && socialTargetArrived) BeginSocialPause(true);
                return;
            }

            if (!CanVisitTarget(other, checkArea: socialOwnerArrived))
            {
                EndSocialPause(false);
                return;
            }
            if (!socialOwnerArrived) return;
            float separation = Flat(other.Mover.Feet - Mover.Feet).magnitude;
            if (separation > visitDistance + 0.75f)
            {
                EndSocialPause(false);
                return;
            }
            other.Mover.Stop(NpcLocomotion.Priority.Ambient);
            BeginSocialPause(false);
            return;
        }

        Vector3 towardOther = other.transform.position - transform.position;
        if (pose == null || pose.IsStanding) Mover.TurnToward(towardOther, FaceSpeed * 0.65f);
        if (other.pose == null || other.pose.IsStanding) other.Mover.TurnToward(-towardOther, FaceSpeed * 0.65f);
        if (Time.time >= socialSwitchAt)
        {
            socialOwnerTalking = !socialOwnerTalking;
            socialSwitchAt = Time.time + UnityEngine.Random.Range(1.2f, 2f);
            SetSocialConversation(other);
        }
        if (Time.time >= socialUntil) EndSocialPause(true);
    }

    private void BeginSocialPause(bool wave)
    {
        if (socialTarget == null) { EndSocialPause(false); return; }
        socialPhase = SocialPhase.Paused;
        socialUntil = Time.time + RandomRange(socialKind == SocialKind.Meet ? meetDurationSeconds : visitDurationSeconds);
        socialSwitchAt = Time.time + UnityEngine.Random.Range(1.2f, 2f);
        socialOwnerTalking = true;
        Mover.Suspended = true;
        socialTarget.Mover.Suspended = true;
        if (wave)
        {
            EnsurePose();
            socialTarget.EnsurePose();
            if (pose != null) pose.Wave(true);
            if (socialTarget.pose != null) socialTarget.pose.Wave(true);
        }
        SetSocialConversation(socialTarget);
    }

    private void SetSocialConversation(NpcLife other)
    {
        if (driver != null) driver.SetAmbientConversation(other.transform, socialOwnerTalking);
        if (other.driver != null) other.driver.SetAmbientConversation(transform, !socialOwnerTalking);
    }

    private bool TryReserveSocialPause(NpcLife target, SocialKind kind)
    {
        if (socialOwner != null && socialOwner.isActiveAndEnabled) return false;
        socialOwner = this;
        socialPartner = target;
        socialTarget = target;
        socialKind = kind;
        socialPhase = SocialPhase.Approaching;
        ClearSocialConversation(this);
        ClearSocialConversation(target);
        return true;
    }

    private void EndSocialPause(bool completed)
    {
        NpcLife other = socialTarget;
        SocialKind endedKind = socialKind;
        bool stopOther = endedKind == SocialKind.Meet || socialPhase == SocialPhase.Paused;
        if (socialOwner == this)
        {
            socialOwner = null;
            socialPartner = null;
        }
        socialTarget = null;
        socialKind = SocialKind.None;
        socialPhase = SocialPhase.None;
        socialOwnerArrived = socialTargetArrived = false;

        if (pose != null) pose.Wave(false);
        if (Mover != null)
        {
            Mover.Stop(NpcLocomotion.Priority.Ambient);
            Mover.Suspended = OwnsDialogue();
        }
        ClearSocialConversation(this);
        if (other != null)
        {
            if (other.pose != null) other.pose.Wave(false);
            if (stopOther) other.Mover.Stop(NpcLocomotion.Priority.Ambient);
            other.Mover.Suspended = other.OwnsDialogue();
            ClearSocialConversation(other);
        }

        if (endedKind == SocialKind.Meet)
            nextMeetAt = Time.time + RandomRange(meetCooldownSeconds);
        else if (endedKind == SocialKind.Visit)
            nextVisitRetryAt = Time.time + (completed ? UnityEngine.Random.Range(4f, 6f) : 0.5f);
    }

    private void EnsurePose()
    {
        if (pose == null) pose = GetComponent<NpcBodyPose>();
        if (pose == null) pose = gameObject.AddComponent<NpcBodyPose>();
    }

    private static void CancelSocialPause(NpcLife participant)
    {
        if (socialOwner == null)
        {
            socialOwner = socialPartner = null;
            return;
        }
        if (participant == socialOwner || participant == socialPartner)
            socialOwner.EndSocialPause(false);
    }

    private static bool IsSocialReserved(NpcLife npc)
    {
        if (socialOwner == null || !socialOwner.isActiveAndEnabled)
        {
            socialOwner = socialPartner = null;
            return false;
        }
        return npc != null && (npc == socialOwner || npc == socialPartner);
    }

    private static void ClearSocialConversation(NpcLife npc)
    {
        if (npc == null) return;
        if (npc.driver != null) npc.driver.SetAmbientConversation(null, false);
        if (npc.partner != null && npc.partner.driver != null)
            npc.partner.driver.SetAmbientConversation(null, false);
    }

    // Allow detours and pivots at stroll pace, while bounding blocked approaches.
    private static float SocialApproachSeconds(float distance) =>
        Mathf.Clamp(distance / 0.5f + 10f, MinimumSocialApproachSeconds, MaximumSocialApproachSeconds);
    private static float RandomRange(Vector2 range)
    {
        float min = Mathf.Max(0f, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        return UnityEngine.Random.Range(min, max);
    }

    // =========================================================
    // PAIR CHAT (the leader drives both)
    // =========================================================

    private void UpdateChat()
    {
        NpcAnimationDriver other = partner.driver;
        bool busy = inDialogue || partner.inDialogue || questLocked || partner.questLocked ||
                    Mover.IsTraveling || partner.Mover.IsTraveling || !partner.isActiveAndEnabled;
        if (busy)
        {
            // One of them is talking to the player: the other just watches.
            if (driver != null) driver.SetAmbientConversation(partner.inDialogue ? partner.transform : null, false);
            if (other != null) other.SetAmbientConversation(inDialogue ? transform : null, false);
            chatSwitchAt = Time.time + 1.5f;
            return;
        }
        if (Time.time >= chatSwitchAt)
        {
            // Mostly turns, sometimes a quiet moment; never both talking at once.
            chatPause = !chatPause && UnityEngine.Random.value < 0.18f;
            if (!chatPause) chatLeaderTalks = !chatLeaderTalks;
            chatSwitchAt = Time.time + (chatPause ? UnityEngine.Random.Range(1.4f, 2.4f) : UnityEngine.Random.Range(2.6f, 5.2f));
        }
        if (driver != null) driver.SetAmbientConversation(partner.transform, !chatPause && chatLeaderTalks);
        if (other != null) other.SetAmbientConversation(transform, !chatPause && !chatLeaderTalks);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private bool WaitingForRounds => taxpayer != null && taxpayer.IsWaitingForRound;

    private static Transform Player => CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    private static Quaternion Yaw(Transform t) => Quaternion.Euler(0f, t.eulerAngles.y, 0f);

    private static readonly RaycastHit[] GroundHits = new RaycastHit[12];

    /// <summary>The walkable ground under a point (NPCs, the player and triggers ignored).</summary>
    public static Vector3 Ground(Vector3 point)
    {
        int count = Physics.RaycastNonAlloc(point + Vector3.up * 1.5f, Vector3.down, GroundHits, 4f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = GroundHits[i];
            if (hit.normal.y < 0.6f || hit.point.y > point.y + 1.0f) continue;
            if (hit.collider.GetComponentInParent<INpcAvailabilitySource>() != null) continue;
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body != null && !body.isKinematic) continue;
            if (hit.point.y > best) best = hit.point.y;
        }
        if (!float.IsNegativeInfinity(best)) point.y = best;
        return point;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.3f, 1f);
        if (home != null) Gizmos.DrawLine(home.position, home.position + home.forward * 0.6f);
        if (seat != null)
        {
            Gizmos.DrawWireSphere(seat.position, 0.12f);
            Gizmos.DrawLine(seat.position, seat.position + seat.forward * StandOffset);
        }
        if (route != null)
            for (int i = 0; i < route.Length; i++)
            {
                if (route[i] == null) continue;
                Gizmos.DrawWireSphere(route[i].position, 0.2f);
                if (i > 0 && route[i - 1] != null) Gizmos.DrawLine(route[i - 1].position, route[i].position);
            }
    }
}
