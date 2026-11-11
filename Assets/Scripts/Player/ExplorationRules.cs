using UnityEngine;

/// <summary>
/// PURPOSE:
/// Milestone T03 mixed-camera exploration: the pure, testable decisions
/// behind the outdoor third-person / indoor first-person loop. No scene
/// objects, saves or input reads here; CameraController,
/// FirstPersonCameraController, PlayerMovement and Interactor apply them.
///
/// - Active baseline: first-person exploration and sprint in Levels 1–3.
/// - Stored pilot: Office_Level1 mixed camera/jump is disabled by default in
///   ExplorationBody.asset; tentative T22 owns future adoption. Smoothing is independent.
/// - Doorway: one threshold line per doorway with a hysteresis band, so
///   standing on the boundary can never flip the view back and forth.
/// - Switching waits while a task, dialogue, viewer, hold or transition
///   owns the controls, and for the previous blend to finish.
/// - Sprint: the movement stick's outer ring (enter/exit thresholds).
/// - Jump: one grounded jump per deliberate press (short grace only).
/// - Camera: collision-shortened orbit distance that recovers smoothly.
/// - Targeting: the player's own reach and line of sight decide, never the
///   follow camera's distance.
/// - Recovery: a fall far below the last grounded safe point, or a long
///   time airborne, returns the body to that point.
/// </summary>
public static class ExplorationRules
{
    public enum Region { Indoor, Outdoor }

    public const string AdoptedScene = "Office_Level1";

    public static float CapsuleStandingHeight(float height, float radius, Vector3 scale) =>
        Mathf.Max(height * Mathf.Abs(scale.y), 2f * radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));

    // ---------------------------------------------------------------
    // WORLD SCALE (one canonical Elijah, 11 Nov 2026 calibration)
    // ---------------------------------------------------------------

    /// <summary>
    /// Elijah's canonical full height in world metres, sole to hair top, measured in the
    /// model's rest pose (pose-independent). Adult plaza cohort, same rest-pose method:
    /// Marie 1.546, Jhun 1.568, Bea 1.572, Lorna 1.606, Milo 1.622, Arthur 1.643,
    /// Carlo 1.674 (median 1.606; guard Dante and Zorbix excluded). This equals the
    /// Level 2/3 conversation fit (setup-tool skinned measurement), so every Elijah
    /// presentation shares one world scale. Body (sole to scalp) is about 1.56 m.
    /// </summary>
    public const float ElijahRestHeight = 1.58f;

    /// <summary>World scale that gives a model of <paramref name="restHeightAtUnitScale"/> metres the canonical height.</summary>
    public static float CalibratedScale(float restHeightAtUnitScale, float target = ElijahRestHeight) =>
        restHeightAtUnitScale > 1e-4f ? target / restHeightAtUnitScale : 1f;

    /// <summary>
    /// A posed (animated) height is believable for the calibrated rest height. Bent knees and a
    /// lowered head legitimately shorten it; it never grows taller than the rest pose.
    /// </summary>
    public static bool PlausiblePosedHeight(float posed, float rest) =>
        rest > 0f && posed >= rest * 0.8f && posed <= rest * 1.03f;

    // ---------------------------------------------------------------
    // LOCOMOTION (outdoor third person; indoor first person keeps its pace)
    // ---------------------------------------------------------------

    /// <summary>Full walking-rim speed outdoors (m/s): Elijah's own walk at 1.21x its authored cadence.</summary>
    public const float WalkSpeed = 1.45f;
    /// <summary>Outer-ring run speed outdoors (m/s).</summary>
    public const float RunSpeed = 3.8f;
    /// <summary>Run stride (one left+right cycle) as a multiple of the calibrated rest height.</summary>
    public const float RunStrideHeights = 1.45f;
    /// <summary>Phases of left/right heel contact, measured on Elijah's walk and authored identically into the run.</summary>
    public const float LeftContact = 0.22f, RightContact = 0.71f;

    /// <summary>Target planar speed: analog walking up to the rim outdoors, the run in the outer ring.</summary>
    public static float TargetSpeed(bool thirdPerson, float stick, bool sprinting, float firstPersonSpeed)
    {
        stick = Mathf.Clamp01(stick);
        if (!thirdPerson) return firstPersonSpeed * stick * (sprinting ? SprintMultiplier : 1f);
        return sprinting ? RunSpeed : WalkSpeed * stick;
    }

    public const float GroundAccelerate = 14f, GroundBrake = 22f, AirSteer = 3f;

    /// <summary>
    /// Frame-rate independent approach to the wanted planar velocity: starts within ~0.1 s,
    /// stops within ~0.2 s, reverses through zero (no orbit). In the air, steering is limited
    /// and never adds speed beyond what the jump or the stick already had.
    /// </summary>
    public static Vector3 ApproachVelocity(Vector3 current, Vector3 target, float dt, bool grounded)
    {
        current.y = 0f; target.y = 0f;
        if (dt <= 0f) return current;
        if (!grounded)
        {
            float cap = Mathf.Max(current.magnitude, target.magnitude);
            return Vector3.ClampMagnitude(Vector3.Lerp(current, target, 1f - Mathf.Exp(-AirSteer * dt)), cap);
        }
        float rate = target.sqrMagnitude >= current.sqrMagnitude && Vector3.Dot(current, target) >= 0f ? GroundAccelerate : GroundBrake;
        Vector3 next = Vector3.Lerp(current, target, 1f - Mathf.Exp(-rate * dt));
        // Settle exactly: no endless sub-centimetre drift after release.
        return target.sqrMagnitude < 1e-6f && next.sqrMagnitude < 0.0025f ? Vector3.zero : next;
    }

    /// <summary>Idle -> walk weight from actual planar speed.</summary>
    public static float WalkWeight(float speed) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.45f, speed));

    /// <summary>Walk -> run weight from actual planar speed (rim walking never shows the run).</summary>
    public static float RunWeight(float speed) =>
        Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(WalkSpeed + 0.1f, WalkSpeed + 0.55f * (RunSpeed - WalkSpeed), speed));

    /// <summary>
    /// One shared locomotion phase for walk and run (sync group): it advances by distance
    /// actually travelled over the blended stride, so feet match the ground at any speed and
    /// blending never jumps the foot phase. Teleports are rejected by the caller.
    /// </summary>
    public static float AdvancePhase(float phase, float distance, float stride)
    {
        if (distance <= 0f || stride <= 1e-4f) return phase;
        return Mathf.Repeat(phase + distance / stride, 1f);
    }

    /// <summary>The phase passed a heel contact (forward wrap included); <paramref name="left"/> says which foot.</summary>
    public static bool CrossedContact(float before, float after, out bool left)
    {
        left = false;
        if (Mathf.Approximately(before, after)) return false;
        float span = Mathf.Repeat(after - before, 1f);
        if (span > 0.5f) return false;
        foreach (float c in new[] { LeftContact, RightContact })
            if (Mathf.Repeat(c - before, 1f) > 0f && Mathf.Repeat(c - before, 1f) <= span) { left = c == LeftContact; return true; }
        return false;
    }

    /// <summary>
    /// The visual's heading: toward the wanted direction while the stick is held (a reversal
    /// turns at once instead of moonwalking), otherwise toward real travel; never while idle.
    /// </summary>
    public static float FacingYaw(float yaw, Vector3 travel, float dt, Vector3 wanted = default)
    {
        travel.y = 0f; wanted.y = 0f;
        Vector3 aim = wanted.sqrMagnitude > 0.01f ? wanted : travel;
        if (aim.sqrMagnitude < 1e-6f || dt <= 0f) return yaw;
        return Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg, 600f * dt);
    }

    /// <summary>First-person sprint is available in the three playable office chapters.</summary>
    public static bool AdoptsSprint(string sceneName) =>
        sceneName == AdoptedScene || sceneName == "Office_Level2" || sceneName == "Office_Level3";

    /// <summary>Smoothing is independent of the deferred camera feature; retain its existing Level 1 scope.</summary>
    public static bool UsesSmoothedElijah(string sceneName) => sceneName == AdoptedScene;

    /// <summary>The stored mixed-camera pilot can only be opted into in its implemented Level 1 scene.</summary>
    public static bool CanAdoptMixedCamera(string sceneName, bool enabled) => enabled && sceneName == AdoptedScene;

    /// <summary>Disabled by default. T22 owns any future third-person/jump adoption and acceptance.</summary>
    public static bool AdoptsMixedCamera(string sceneName)
    {
        if (sceneName != AdoptedScene) return false;
        var assets = Resources.Load<ExplorationBodyAssets>(ExplorationBody.AssetsPath);
        return CanAdoptMixedCamera(sceneName, assets != null && assets.mixedCameraEnabled);
    }

    // ---------------------------------------------------------------
    // DOORWAY
    // ---------------------------------------------------------------

    /// <summary>Half-width of the hysteresis band either side of a doorway line (metres).</summary>
    public const float DoorBand = 0.6f;

    /// <summary>
    /// The region after the player stands at <paramref name="outward"/> metres past the doorway
    /// line (positive = outside) and <paramref name="lateral"/> metres from its centre along it.
    /// Inside the band, or beside the doorway, the current region holds.
    /// </summary>
    public static Region RegionAtDoor(Region current, float outward, float lateral, float halfWidth, float band = DoorBand)
    {
        if (Mathf.Abs(lateral) > halfWidth + 0.5f || Mathf.Abs(outward) > 3f) return current;
        if (outward > band) return Region.Outdoor;
        if (outward < -band) return Region.Indoor;
        return current;
    }

    /// <summary>
    /// Away from every doorway the floor under the player decides, but only after it has
    /// disagreed for a moment (spawns, guided placements, reloads), never on one sample.
    /// </summary>
    public static Region RegionFromFloor(Region current, Region? floor, float disagreeSeconds, float settle = 0.8f) =>
        floor.HasValue && floor.Value != current && disagreeSeconds >= settle ? floor.Value : current;

    /// <summary>The view may change now: exploration owns the camera and nothing else holds the controls.</summary>
    public static bool CanSwitchView(bool exploring, bool transitioning, bool dialogue, bool held, bool viewer, bool blending) =>
        exploring && !transitioning && !dialogue && !held && !viewer && !blending;

    public static bool ThirdPersonFor(Region region) => region == Region.Outdoor;

    // ---------------------------------------------------------------
    // SPRINT AND JUMP
    // ---------------------------------------------------------------

    // The visible walking rim is 1. Push deliberately beyond it to sprint.
    public const float SprintEnter = 1.45f;
    public const float SprintExit = 1.25f;
    public const float SprintTravelLimit = 1.65f;
    public const float SprintMultiplier = 1.4f;

    /// <summary>Sprinting while the held stick stays in its outer ring (no latch, no stamina).</summary>
    public static bool Sprinting(bool wasSprinting, bool held, float rawMagnitude) =>
        held && rawMagnitude >= (wasSprinting ? SprintExit : SprintEnter);

    /// <summary>Walked off an edge: a jump still counts this long afterwards (seconds).</summary>
    public const float CoyoteTime = 0.10f;
    /// <summary>A press this recent is kept once and used at the next valid moment, e.g. landing (seconds).</summary>
    public const float JumpBuffer = 0.13f;
    /// <summary>Readable crouch-and-push before the body leaves the ground (seconds).</summary>
    public const float JumpWindup = 0.05f;
    public const float JumpCooldown = 0.25f;
    /// <summary>Apex rise in metres (0.27 standing heights; clears the 0.2 m practice curb).</summary>
    public const float JumpHeight = 0.42f;

    /// <summary>
    /// One jump per press: on the ground, or within the coyote time after walking (not jumping)
    /// off it, and not right after the previous jump. A held button never repeats.
    /// </summary>
    public static bool CanJump(bool requested, bool grounded, float sinceGrounded, float sinceLastJump, bool jumpedSinceGrounded = false) =>
        requested && (grounded || (!jumpedSinceGrounded && sinceGrounded <= CoyoteTime)) && sinceLastJump >= JumpCooldown;

    /// <summary>Take-off speed that reaches <paramref name="height"/> metres under <paramref name="gravity"/> (m/s²).</summary>
    public static float JumpSpeed(float height, float gravity) => Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Max(0f, height));

    /// <summary>Airborne this long without a jump is a real fall; shorter is ground-cast flicker (curbs, seams).</summary>
    public const float FallConfirm = 0.08f;

    // Jump clip segments (normalized clip time), authored into Elijah_Jump.anim.
    public const float JumpClipTakeoff = 0.12f, JumpClipApex = 0.42f, JumpClipContact = 0.70f;

    public enum JumpPhase { None, Windup, Rise, Fall, Land }

    /// <summary>
    /// The jump presentation's explicit state: each phase has monotonic progress driven by
    /// physics events (windup clock, launch speed, apex, ground distance, real contact).
    /// The whole sequence is never scrubbed back and forth by raw velocity.
    /// </summary>
    public struct JumpTrack
    {
        public JumpPhase Phase;
        public float Progress;
        public float Weight;
        public float LandSeconds;
        public float AirSeconds;

        /// <summary>Normalized jump clip time for the current phase and progress.</summary>
        public float ClipTime
        {
            get
            {
                float p = Mathf.Clamp01(Progress);
                switch (Phase)
                {
                    case JumpPhase.Windup: return Mathf.Lerp(0f, JumpClipTakeoff, p);
                    case JumpPhase.Rise: return Mathf.Lerp(JumpClipTakeoff, JumpClipApex, p);
                    case JumpPhase.Fall: return Mathf.Lerp(JumpClipApex, JumpClipContact - 0.02f, p);
                    case JumpPhase.Land: return Mathf.Lerp(JumpClipContact, 1f, p);
                    default: return 1f;
                }
            }
        }

        /// <param name="windup">Seconds into a committed windup, or a negative value when none.</param>
        /// <param name="airborne">Off the ground after a jump, or for longer than <see cref="FallConfirm"/>.</param>
        /// <param name="vy">Vertical velocity.</param>
        /// <param name="launch">Launch speed of the current jump (0 when walking off an edge).</param>
        /// <param name="groundBelow">Reliable probe distance from the feet to the ground while descending, or negative.</param>
        /// <param name="moving">Planar speed is at least a slow walk (landings recover sooner).</param>
        public void Step(float windup, bool airborne, float vy, float launch, float groundBelow, bool moving, float dt)
        {
            if (dt < 0f) dt = 0f;
            if (windup >= 0f && !airborne)
            {
                if (Phase != JumpPhase.Windup) { Phase = JumpPhase.Windup; Progress = 0f; }
                Progress = Mathf.Max(Progress, Mathf.Clamp01(windup / JumpWindup));
            }
            else if (airborne)
            {
                AirSeconds += dt;
                if (Phase != JumpPhase.Rise && Phase != JumpPhase.Fall)
                {
                    // A real jump rises; walking off a ledge starts at the apex pose (no takeoff anticipation mid-air).
                    Phase = vy > 0.05f && launch > 0f ? JumpPhase.Rise : JumpPhase.Fall;
                    Progress = 0f;
                }
                if (Phase == JumpPhase.Rise)
                {
                    Progress = Mathf.Max(Progress, launch > 0f ? Mathf.Clamp01(1f - vy / launch) : 1f);
                    if (vy <= 0f) { Phase = JumpPhase.Fall; Progress = 0f; }
                }
                else
                {
                    // Legs reach toward the floor as it approaches; with no reliable probe they
                    // prepare gradually but never play the contact itself before contact.
                    float byTime = Mathf.Clamp01(AirSeconds > 0f ? Progress + dt / 0.35f : 0f);
                    float byGround = groundBelow >= 0f ? 1f - Mathf.Clamp01(groundBelow / 0.45f) : 0f;
                    Progress = Mathf.Max(Progress, Mathf.Min(0.92f, groundBelow >= 0f ? Mathf.Max(byGround, Mathf.Min(byTime, 0.6f)) : Mathf.Min(byTime, 0.75f)));
                }
            }
            else if (Phase == JumpPhase.Rise || Phase == JumpPhase.Fall)
            {
                // Genuine contact after real flight: a soft landing scaled to the fall.
                float impact = Mathf.Max(0f, -vy);
                float severity = Mathf.Clamp01((impact - 1f) / 4f);
                LandSeconds = moving ? Mathf.Lerp(0.12f, 0.18f, severity) : Mathf.Lerp(0.16f, 0.26f, severity);
                Phase = JumpPhase.Land;
                Progress = 0f;
                AirSeconds = 0f;
            }
            else if (Phase == JumpPhase.Land)
            {
                Progress = Mathf.Min(1f, Progress + dt / Mathf.Max(0.05f, LandSeconds));
                if (Progress >= 1f) Phase = JumpPhase.None;
            }
            else if (Phase == JumpPhase.Windup)
            {
                Phase = JumpPhase.None; // cancelled before launch (held by a task/dialogue)
            }
            if (!airborne && Phase != JumpPhase.Rise && Phase != JumpPhase.Fall) AirSeconds = 0f;

            float wanted = Phase == JumpPhase.None ? 0f
                : Phase == JumpPhase.Land ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(moving ? 0.35f : 0.55f, 1f, Progress))
                : 1f;
            // Rate-limited (not instant) so a running pose never pops into the jump layer.
            Weight = Mathf.MoveTowards(Weight, wanted, dt * (wanted > Weight ? 16f : 8f));
        }
    }

    public const float FallDropLimit = 3f;
    public const float AirborneLimit = 3f;

    /// <summary>Return to the last grounded safe point after a long drop or a long time in the air.</summary>
    public static bool NeedsRecovery(float y, float safeY, float airborneSeconds) =>
        y < safeY - FallDropLimit || airborneSeconds > AirborneLimit;

    /// <summary>A grounded, level, settled body is a safe point to return to.</summary>
    public static bool SafeGround(bool grounded, float groundNormalY, float verticalSpeed, float groundedSeconds) =>
        grounded && groundNormalY >= 0.8f && Mathf.Abs(verticalSpeed) < 0.5f && groundedSeconds >= 0.3f;

    // ---------------------------------------------------------------
    // THIRD-PERSON CAMERA
    // ---------------------------------------------------------------

    public const float CameraMinDistance = 0.35f;
    public const float CameraSkin = 0.08f;

    // Composition after the supplied adventure reference: the whole standing body centred in
    // the lower middle, generous room above/ahead, a gentle downward look. The shared Main
    // Camera keeps its 60° vertical FOV (dialogue shots, document overlays and task layouts
    // read it), so distance and screen offset are solved for 60°.
    /// <summary>Follow distance from the pivot, in canonical standing heights (3.0 m).</summary>
    public const float CameraDistanceHeights = 1.91f;
    /// <summary>Follow pivot height as a fraction of standing height (hips).</summary>
    public const float CameraPivotFraction = 0.55f;
    /// <summary>The pivot appears this far below the viewport centre (normalized device units).</summary>
    public const float CameraScreenOffset = 0.37f;
    /// <summary>Neutral downward pitch added to the shared look pitch in third person (degrees).</summary>
    public const float CameraPitchBias = 12f;
    /// <summary>Camera pitch limits in third person (degrees, positive = looking down).</summary>
    public const float CameraMinPitch = -8f, CameraMaxPitch = 48f;

    /// <summary>
    /// Third-person camera position: behind the pivot along the view, raised along the view's
    /// own up axis so the pivot sits <paramref name="screenOffset"/> below centre at any pitch.
    /// Pulling in along pivot->camera keeps that screen position (similar triangles).
    /// </summary>
    public static Vector3 CameraPosition(Vector3 pivot, Quaternion rotation, float distance, float screenOffset, float verticalFov) =>
        pivot - rotation * Vector3.forward * distance +
        rotation * Vector3.up * (screenOffset * distance * Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad));

    /// <summary>Viewport position (x from left, y from TOP, both 0..1) of a world point; z = depth.</summary>
    public static Vector3 ViewportFromTop(Vector3 world, Vector3 cameraPosition, Quaternion rotation, float verticalFov, float aspect)
    {
        Vector3 d = world - cameraPosition;
        Vector3 local = new Vector3(Vector3.Dot(d, rotation * Vector3.right), Vector3.Dot(d, rotation * Vector3.up), Vector3.Dot(d, rotation * Vector3.forward));
        if (local.z <= 1e-4f) return new Vector3(float.NaN, float.NaN, local.z);
        float t = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
        float x = local.x / (local.z * t * aspect);
        float y = local.y / (local.z * t);
        return new Vector3(0.5f + 0.5f * x, 0.5f - 0.5f * y, local.z);
    }

    /// <summary>Orbit distance allowed by the nearest obstacle hit (null = nothing in the way).</summary>
    public static float CollisionDistance(float desired, float? hitDistance) =>
        hitDistance.HasValue ? Mathf.Clamp(hitDistance.Value - CameraSkin, CameraMinDistance, desired) : desired;

    /// <summary>Clear for this long before the camera eases back out, so a flickering edge never pumps.</summary>
    public const float CameraRecoverDelay = 0.3f;

    /// <summary>
    /// Pull in at once (never see through a wall); ease back out only after the way has been
    /// clear for <see cref="CameraRecoverDelay"/>, exponentially (frame-rate independent).
    /// </summary>
    public static float FollowDistance(float current, float allowed, float dt, float clearSeconds = float.MaxValue, float easeOut = 3.5f)
    {
        if (allowed < current) return allowed;
        if (clearSeconds < CameraRecoverDelay) return current;
        float next = current + (allowed - current) * (1f - Mathf.Exp(-easeOut * Mathf.Max(0f, dt)));
        return allowed - next < 0.002f ? allowed : next;
    }

    /// <summary>
    /// A low obstacle (bench, curb, low hedge) whose top lies below the rising camera path at
    /// the hit is passed over, never a reason to zoom in on the player's back.
    /// </summary>
    public static bool PassesOver(float obstacleTop, float castCentreHeight, float radius) =>
        obstacleTop <= castCentreHeight + radius * 0.25f;

    /// <summary>
    /// The camera's tracked floor height: follows walking up/down smoothly, holds steady through
    /// a jump arc (no bob), follows a real drop below the take-off floor, snaps after teleports.
    /// </summary>
    public static float TrackFloor(float tracked, float feetY, bool grounded, float takeoffY, float dt)
    {
        float target = grounded ? feetY : Mathf.Min(feetY, takeoffY);
        if (Mathf.Abs(target - tracked) > 2.5f || dt <= 0f) return target;
        float rate = grounded ? 9f : 12f;
        return tracked + (target - tracked) * (1f - Mathf.Exp(-rate * dt));
    }

    // ---------------------------------------------------------------
    // TARGETING
    // ---------------------------------------------------------------

    /// <summary>
    /// A world target counts when the player (not the camera) is within reach, it is not
    /// behind the player's back relative to the view, and nothing blocks the player's line of sight.
    /// </summary>
    public static bool PlayerCanTarget(float flatDistanceFromPlayer, float reach, float aheadOfPlayer, bool occluded) =>
        flatDistanceFromPlayer <= reach && aheadOfPlayer > -0.3f && !occluded;
}
