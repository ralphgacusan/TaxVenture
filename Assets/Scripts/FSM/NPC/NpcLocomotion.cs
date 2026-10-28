using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Moves one NPC on foot along a short authored route (Level 1 Living Plaza):
/// Marie's trip to the office, Arthur's walk to the corkboard, ambient
/// strolls. Presentation and placement only; whoever starts a trip owns what
/// arriving means (arrival is never quest completion by itself).
///
/// HOW IT WALKS:
/// - Route: the caller gives stops (doorway points on both sides of every
///   door, then the destination). Each leg is planned when it starts with the
///   scene's physics grid A* (GuideGridPath; the scene has no NavMesh), never
///   per frame, and replanned only when the NPC is stuck.
/// - Feet: the walk clip's own ground speed (NpcAnimationDriver.AnimatedWalkSpeed,
///   scaled to the avatar) drives the move, so feet do not slide. Large turns
///   pivot in place first; the NPC only ever moves forward. The root keeps
///   its height above the ground under it (a ray per frame while walking).
/// - Others: the player or another NPC close ahead makes it slow and wait,
///   then take a small step aside. It never pushes through anyone.
/// - Pause: Suspended (its own conversation) stops it on the spot; it resumes
///   from there afterwards.
/// - Failure: a leg that cannot be planned or stays stuck after two replans
///   ends the trip with done(false); the owner may then StageWhenUnseen,
///   which places the NPC only while neither it nor the spot is on screen.
/// </summary>
[DisallowMultipleComponent]
public sealed class NpcLocomotion : MonoBehaviour
{
    public enum Priority { Ambient = 0, Quest = 1 }

    [Tooltip("Used until the walk clip's own speed is known (and for models without a walk clip).")]
    [SerializeField, Min(0.2f)] private float fallbackWalkSpeed = 1.1f;
    [SerializeField, Min(30f)] private float walkTurnDegreesPerSecond = 200f;
    [SerializeField, Min(30f)] private float pivotDegreesPerSecond = 240f;
    [Tooltip("Larger turns than this pivot in place before walking on.")]
    [SerializeField, Range(20f, 120f)] private float pivotAngle = 70f;
    [SerializeField, Min(0.02f)] private float stoppingDistance = 0.1f;
    [SerializeField, Min(0.1f)] private float bodyRadius = 0.3f;

    private const float CornerRadius = 0.45f;
    private const float Acceleration = 3.5f;          // speed share per second
    private const float AvoidAhead = 1.05f;
    private const float WaitBeforeSidestep = 1.2f;
    private const float StuckWindow = 2.5f;

    /// <summary>NPC movers in the scene (other NPCs read them for spacing).</summary>
    public static readonly List<NpcLocomotion> All = new List<NpcLocomotion>();

    private static GuideGridPath planner;

    private NpcAnimationDriver driver;
    private readonly List<Vector3> stops = new List<Vector3>();
    private readonly List<Vector3> path = new List<Vector3>();
    private int stopIndex = -1, pathIndex;
    private Quaternion? finalFacing;
    private Action<bool> done;
    private bool stroll;
    private float speedShare;
    private float feetOffset;
    private bool feetMeasured;
    private float blockedSince = -1f;
    private Vector3? sidestep;
    private Vector3 progressMark;
    private float progressTime;
    private int replans;
    private bool turningToFinal;
    private Vector3 stagePosition;
    private Quaternion stageFacing;
    private Action staged;
    private bool stagePending;
    private readonly RaycastHit[] hits = new RaycastHit[12];
    private readonly Plane[] frustum = new Plane[6];

    /// <summary>A trip is under way (walking, turning, waiting or paused).</summary>
    public bool IsTraveling => stopIndex >= 0;

    /// <summary>The feet are moving this frame.</summary>
    public bool IsWalking { get; private set; }

    /// <summary>The current trip's priority (Ambient when idle).</summary>
    public Priority CurrentPriority { get; private set; }

    /// <summary>Paused on the spot (its own conversation, or its owner); resumes when cleared.</summary>
    public bool Suspended { get; set; }

    /// <summary>The trip's last stop (the destination), or the NPC's position when idle.</summary>
    public Vector3 Destination => stops.Count > 0 ? stops[stops.Count - 1] : Feet;

    /// <summary>Where the feet are (the root keeps its own height above them).</summary>
    public Vector3 Feet
    {
        get
        {
            MeasureFeet();
            return transform.position - Vector3.up * feetOffset;
        }
    }

    /// <summary>The way the character's face points (models may sit rotated inside the root).</summary>
    public Vector3 Facing => transform.rotation * FacingOffset * Vector3.forward;

    private Quaternion FacingOffset => driver != null ? driver.FacingOffset : Quaternion.identity;

    private void Awake()
    {
        driver = GetComponent<NpcAnimationDriver>();
        // Placement is scripted here; clip root motion must never move the NPC.
        foreach (Animator animator in GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        SetWalking(false);
    }

    // =========================================================
    // COMMANDS
    // =========================================================

    /// <summary>
    /// Walks through <paramref name="route"/> (doorway points, then the destination)
    /// and turns to <paramref name="facing"/> on arrival. A trip of the same or
    /// higher priority replaces the current one (whose callback is not called);
    /// a lower one is refused. <paramref name="arrived"/> gets true on arrival,
    /// false when the route failed.
    /// </summary>
    public bool Go(IList<Vector3> route, Quaternion? facing, Priority priority, bool slowPace, Action<bool> arrived)
    {
        if (route == null || route.Count == 0) return false;
        if (IsTraveling && priority < CurrentPriority) return false;
        stagePending = false;
        staged = null;
        stops.Clear();
        stops.AddRange(route);
        finalFacing = facing;
        CurrentPriority = priority;
        stroll = slowPace;
        done = arrived;
        turningToFinal = false;
        replans = 0;
        if (priority == Priority.Quest && planner != null) planner.Clear();   // doors and gates may have changed
        BeginLeg(0);
        return true;
    }

    /// <summary>Ends the trip where the NPC stands (no callback). Quest trips survive Ambient stops.</summary>
    public void Stop(Priority priority = Priority.Quest)
    {
        if (IsTraveling && priority < CurrentPriority) return;
        stopIndex = -1;
        stops.Clear();
        path.Clear();
        done = null;
        sidestep = null;
        CurrentPriority = Priority.Ambient;
        SetWalking(false);
    }

    /// <summary>Sets how high the root sits above the feet (0 for models whose pivot is at the soles).</summary>
    public void SetFeetOffset(float offset)
    {
        feetOffset = offset;
        feetMeasured = true;
    }

    /// <summary>Puts the NPC at a spot at once (restore, or a staged move nobody sees). Ends any trip.</summary>
    public void PlaceAt(Vector3 feet, Quaternion facing)
    {
        Stop();
        MeasureFeet();
        transform.SetPositionAndRotation(feet + Vector3.up * feetOffset, FaceRotation(facing * Vector3.forward));
        Physics.SyncTransforms();
        if (driver != null) driver.ResetMotion();
    }

    /// <summary>
    /// The intentional fallback when walking is impossible: the NPC is placed
    /// at the spot only while neither the NPC nor the spot is on screen, then
    /// <paramref name="placed"/> runs. Never a visible jump.
    /// </summary>
    public void StageWhenUnseen(Vector3 feet, Quaternion facing, Action placed)
    {
        Stop();
        stagePosition = feet;
        stageFacing = facing;
        staged = placed;
        stagePending = true;
    }

    /// <summary>Turns the character in place toward a direction (pivots when idle). Returns the angle left.</summary>
    public float TurnToward(Vector3 direction, float degreesPerSecond)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return 0f;
        Quaternion target = FaceRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * Time.deltaTime);
        return Quaternion.Angle(transform.rotation, target);
    }

    /// <summary>The root rotation that makes the character face <paramref name="direction"/>.</summary>
    public Quaternion FaceRotation(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return transform.rotation;
        return Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Inverse(FacingOffset);
    }

    // =========================================================
    // TRIP
    // =========================================================

    private void BeginLeg(int index)
    {
        stopIndex = index;
        path.Clear();
        pathIndex = 0;
        sidestep = null;
        blockedSince = -1f;
        progressMark = Feet;
        progressTime = Time.time;
        if (planner == null) planner = new GuideGridPath();
        Vector3 from = Feet;
        Vector3 to = stops[index];
        bool reached = planner.FindPath(from, to, transform, path);
        if (path.Count == 0 || (!reached && (path[path.Count - 1] - to).sqrMagnitude > 0.8f * 0.8f))
        {
            // A leg the grid cannot reach: go straight when nothing is in the way, else fail.
            path.Clear();
            if (!ClearLine(from, to))
            {
                Debug.LogWarning($"[NpcLocomotion:{name}] No walkable route to {to}; the trip ends here.", this);
                Finish(false);
                return;
            }
            path.Add(from);
            path.Add(to);
        }
        // The first point is where the NPC already stands.
        if (path.Count > 1 && (Flat(path[0] - from)).sqrMagnitude < CornerRadius * CornerRadius) pathIndex = 1;
    }

    private void Update()
    {
        if (stagePending)
        {
            TryStage();
            return;
        }
        if (!IsTraveling) return;
        if (Suspended)
        {
            Brake();
            progressTime = Time.time;
            return;
        }

        if (turningToFinal)
        {
            SetWalking(false);
            Vector3 want = finalFacing.HasValue ? finalFacing.Value * Vector3.forward : Facing;
            if (TurnToward(want, pivotDegreesPerSecond * 0.7f) < 2f) Finish(true);
            return;
        }

        Vector3 feet = Feet;
        Vector3 target = sidestep ?? path[Mathf.Min(pathIndex, path.Count - 1)];
        Vector3 toTarget = Flat(target - feet);
        bool lastPoint = sidestep == null && pathIndex >= path.Count - 1;
        float reach = lastPoint ? stoppingDistance : CornerRadius;
        if (toTarget.magnitude <= reach)
        {
            if (sidestep.HasValue) { sidestep = null; return; }
            if (!lastPoint) { pathIndex++; return; }
            // This stop is reached: the next leg, or the end of the trip.
            if (stopIndex < stops.Count - 1) { BeginLeg(stopIndex + 1); return; }
            Brake();
            turningToFinal = true;
            return;
        }

        // Turn first when the corner is sharp; walk only forward.
        Vector3 facing = Facing;
        float angle = Vector3.Angle(Flat(facing), toTarget);
        if (angle > pivotAngle)
        {
            Brake();
            TurnToward(toTarget, pivotDegreesPerSecond);
            progressTime = Time.time;
            return;
        }

        if (Blocked(feet, toTarget.normalized, out Vector3 blocker))
        {
            Brake();
            progressTime = Time.time;
            if (blockedSince < 0f) blockedSince = Time.time;
            else if (Time.time - blockedSince > WaitBeforeSidestep && sidestep == null) PlanSidestep(feet, toTarget.normalized, blocker);
            return;
        }
        blockedSince = -1f;

        TurnToward(toTarget, walkTurnDegreesPerSecond);
        speedShare = Mathf.MoveTowards(speedShare, 1f, Acceleration * Time.deltaTime);
        float clipSpeed = driver != null && driver.AnimatedWalkSpeed > 0.3f ? driver.AnimatedWalkSpeed : fallbackWalkSpeed * (stroll ? 0.8f : 1f);
        // Ease into the trip's final stop (doorway points are walked through at full stride);
        // a short, gentle ease keeps the clip's steps close to the ground speed.
        float remaining = lastPoint ? toTarget.magnitude : float.MaxValue;
        float slow = lastPoint && stopIndex == stops.Count - 1 ? Mathf.Clamp(remaining / 0.4f, 0.6f, 1f) : 1f;
        float step = Mathf.Min(clipSpeed * speedShare * slow * Time.deltaTime, remaining);
        SetWalking(true);
        Vector3 next = feet + Flat(Facing).normalized * step;
        next.y = GroundAt(next, feet.y);
        transform.position = next + Vector3.up * feetOffset;

        // Stuck: little progress while trying to walk.
        if (Time.time - progressTime > StuckWindow)
        {
            if ((Flat(Feet - progressMark)).magnitude < 0.25f)
            {
                if (++replans > 2)
                {
                    Debug.LogWarning($"[NpcLocomotion:{name}] Stuck on the way to {stops[stops.Count - 1]}; the trip ends here.", this);
                    Finish(false);
                    return;
                }
                planner?.Clear();
                BeginLeg(stopIndex);
                return;
            }
            progressMark = Feet;
            progressTime = Time.time;
        }
    }

    private void Finish(bool arrived)
    {
        Action<bool> callback = done;
        stopIndex = -1;
        stops.Clear();
        path.Clear();
        done = null;
        sidestep = null;
        turningToFinal = false;
        CurrentPriority = Priority.Ambient;
        SetWalking(false);
        callback?.Invoke(arrived);
    }

    private void Brake()
    {
        speedShare = 0f;
        SetWalking(false);
    }

    private void SetWalking(bool walking)
    {
        if (walking == IsWalking && (!walking || driver == null)) return;
        IsWalking = walking;
        if (driver != null) driver.SetLocomotion(walking, stroll);
    }

    // =========================================================
    // OTHERS
    // =========================================================

    /// <summary>The player or another NPC close ahead.</summary>
    private bool Blocked(Vector3 feet, Vector3 heading, out Vector3 blocker)
    {
        blocker = Vector3.zero;
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (player != null && Ahead(feet, heading, player.position, 0.35f)) { blocker = player.position; return true; }
        foreach (NpcLife other in NpcLife.All)
        {
            if (other == null || other.transform == transform || !other.isActiveAndEnabled) continue;
            if (Ahead(feet, heading, other.transform.position, 0.3f)) { blocker = other.transform.position; return true; }
        }
        return false;
    }

    private bool Ahead(Vector3 feet, Vector3 heading, Vector3 other, float otherRadius)
    {
        Vector3 to = Flat(other - feet);
        float distance = to.magnitude;
        if (distance > AvoidAhead + otherRadius || distance < 0.01f) return false;
        // Only what is in front of the way we are going, and not what we are about to reach.
        if (Vector3.Dot(to / distance, heading) < 0.55f) return false;
        Vector3 goal = Flat(Destination - feet);
        return goal.magnitude > distance - 0.2f;
    }

    private void PlanSidestep(Vector3 feet, Vector3 heading, Vector3 blocker)
    {
        Vector3 right = Vector3.Cross(Vector3.up, heading);
        float side = Vector3.Dot(Flat(blocker - feet), right) > 0f ? -1f : 1f;
        foreach (float sign in new[] { side, -side })
        {
            Vector3 aside = feet + right * (sign * 0.75f) + heading * 0.6f;
            aside.y = GroundAt(aside, feet.y);
            if (ClearLine(feet, aside) && !Occupied(aside))
            {
                sidestep = aside;
                blockedSince = -1f;
                return;
            }
        }
    }

    private bool Occupied(Vector3 point)
    {
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (player != null && Flat(player.position - point).magnitude < 0.6f) return true;
        foreach (NpcLife other in NpcLife.All)
            if (other != null && other.transform != transform && Flat(other.transform.position - point).magnitude < 0.6f) return true;
        return false;
    }

    // =========================================================
    // GEOMETRY
    // =========================================================

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    private void MeasureFeet()
    {
        if (feetMeasured) return;
        feetMeasured = true;
        float ground = GroundAt(transform.position, transform.position.y);
        feetOffset = Mathf.Clamp(transform.position.y - ground, -0.5f, 1.5f);
    }

    /// <summary>The walkable ground height under a point (fallback when none is found).</summary>
    private float GroundAt(Vector3 point, float fallback)
    {
        int count = Physics.RaycastNonAlloc(point + Vector3.up * 1.0f, Vector3.down, hits, 3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.normal.y < 0.6f || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<INpcAvailabilitySource>() != null) continue;
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body != null && !body.isKinematic) continue;
            if (hit.point.y > fallback + 0.5f) continue;
            if (hit.point.y > best) best = hit.point.y;
        }
        return float.IsNegativeInfinity(best) ? fallback : best;
    }

    /// <summary>A body-wide straight line between two feet positions is free of walls and furniture.</summary>
    private bool ClearLine(Vector3 from, Vector3 to)
    {
        Vector3 delta = Flat(to - from);
        float distance = delta.magnitude;
        if (distance < 0.01f) return true;
        Vector3 bottom = from + Vector3.up * (0.45f + bodyRadius);
        Vector3 top = from + Vector3.up * 1.5f;
        int count = Physics.CapsuleCastNonAlloc(bottom, top, bodyRadius * 0.9f, delta / distance, hits, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i].collider;
            if (c.transform.IsChildOf(transform) || c.GetComponentInParent<INpcAvailabilitySource>() != null) continue;
            Rigidbody body = c.attachedRigidbody;
            if (body != null && !body.isKinematic) continue;
            return false;
        }
        return true;
    }

    // =========================================================
    // STAGED FALLBACK
    // =========================================================

    private void TryStage()
    {
        Camera camera = GuideTargets.ViewCamera;
        if (camera != null)
        {
            GeometryUtility.CalculateFrustumPlanes(camera, frustum);
            if (Seen(camera, transform.position + Vector3.up) || Seen(camera, stagePosition + Vector3.up)) return;
        }
        stagePending = false;
        Action callback = staged;
        staged = null;
        PlaceAt(stagePosition, stageFacing);
        Debug.Log($"[NpcLocomotion:{name}] Placed at {stagePosition} out of view (route unavailable).", this);
        callback?.Invoke();
    }

    private bool Seen(Camera camera, Vector3 point)
    {
        if (!GeometryUtility.TestPlanesAABB(frustum, new Bounds(point, Vector3.one * 1.8f))) return false;
        Vector3 from = camera.transform.position;
        return !Physics.Linecast(from, point, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ||
               hit.collider.transform.IsChildOf(transform);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || path.Count == 0) return;
        Gizmos.color = new Color(0.3f, 0.9f, 0.6f, 1f);
        for (int i = 1; i < path.Count; i++) Gizmos.DrawLine(path[i - 1] + Vector3.up * 0.1f, path[i] + Vector3.up * 0.1f);
        foreach (Vector3 stop in stops) Gizmos.DrawWireSphere(stop + Vector3.up * 0.1f, 0.2f);
    }
}
