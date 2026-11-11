using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("How quickly the player rotates to face movement direction.")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Mobile Input")]
    [SerializeField] private VirtualJoystick mobileJoystick;

    [Header("Editor Testing")]
    [SerializeField] private bool useKeyboardInEditor = true;

    // First-person sprint is independent of the stored T03 mixed-camera/jump pilot.
    // T22 owns any future adoption of that disabled pilot.
    [Header("Sprint and Jump (adopted scenes)")]
    [Tooltip("Apex height of a jump in world units: clears the ~0.2 m practice curb with margin.")]
    [SerializeField] private float jumpHeight = ExplorationRules.JumpHeight;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Vector3 moveDirection;
    private bool hasFocus = true;
    private bool isPaused;

    [Header("Footstep Audio")]
    [SerializeField] private float footstepInterval = 0.7f;

    private float footstepTimer;

    private bool sprintEnabled;
    private bool sprintAndJump;
    private bool sprinting;
    private float stick;
    private bool jumpRequested;
    private float jumpRequestedAt = -10f;
    private float lastJumpAt = -10f;
    private float lastGroundedAt;
    private float groundedSince = -1f;
    private float airborneSince = -1f;
    private bool grounded = true;
    private float groundNormalY = 1f;
    // T03 jump presentation: committed windup, the current jump, the floor it left and the probe below.
    private float windupStartedAt = -1f;
    private bool jumping, jumpedSinceGrounded;
    private float launchSpeed, lastGroundY, groundBelow = -1f, airVerticalSpeed;
    private Vector3 safePoint;
    private bool hasSafePoint;
    private bool sprintCued, jumpCued;
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    /// <summary>T03: the player stands on something (bounded sphere cast under the capsule).</summary>
    public bool IsGrounded => grounded;
    /// <summary>T03: the held stick is in its outer ring (adopted scenes only).</summary>
    public bool IsSprinting => sprinting && enabled;
    /// <summary>Held sprint is available independently of the exploration perspective.</summary>
    public bool SprintEnabled => sprintEnabled;
    /// <summary>Stored pilot: ground/recovery/jump support is active only with mixed-camera opt-in.</summary>
    public bool SprintAndJumpEnabled => sprintAndJump;
    /// <summary>Horizontal speed actually travelled (body animation reads it).</summary>
    public float PlanarSpeed => rb != null ? new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude : 0f;
    /// <summary>The existing traversal pace, used to synchronize the visual walk without changing physics.</summary>
    public float WalkSpeed => moveSpeed;
    public float VerticalSpeed => rb != null ? rb.linearVelocity.y : 0f;
    public Vector3 SafePoint => safePoint;
    /// <summary>T03: the wanted planar direction scaled by stick travel (0..1); zero without input.</summary>
    public Vector3 MoveDirection => moveDirection;
    /// <summary>T03: seconds into a committed jump windup, or -1.</summary>
    public float WindupSeconds => windupStartedAt >= 0f ? Time.time - windupStartedAt : -1f;
    /// <summary>T03: launch speed of the current jump (0 when airborne without jumping).</summary>
    public float LaunchSpeed => jumping ? launchSpeed : 0f;
    /// <summary>T03: reliable probe distance from the soles to the floor while descending, or -1.</summary>
    public float GroundBelow => groundBelow;
    /// <summary>T03: the floor height the player last stood on (the camera holds it through a hop).</summary>
    public float LastGroundY => lastGroundY;
    /// <summary>T03: off the ground after a jump, or for longer than a ground-cast flicker.</summary>
    public bool AirborneForAnimation => sprintAndJump && !grounded && (jumping || (airborneSince >= 0f && Time.time - airborneSince > ExplorationRules.FallConfirm));
    /// <summary>T03: the visible third-person body plays footsteps on its heel contacts; the timer stays for first person.</summary>
    public bool BodyDrivesFootsteps { get; set; }
    /// <summary>Raised after a fall/airborne recovery returns the body to its last safe point.</summary>
    public static event System.Action Recovered;

    public const string CueSprinted = "PlayerSprinted";
    public const string CueJumped = "PlayerJumped";

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        // This stays enabled when a lesson or workstation holds movement.
        if (GetComponent<AudioEnvironment>() == null)
            gameObject.AddComponent<AudioEnvironment>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
        sprintEnabled = ExplorationRules.AdoptsSprint(gameObject.scene.name);
        sprintAndJump = ExplorationRules.AdoptsMixedCamera(gameObject.scene.name);
        lastGroundY = transform.position.y;
    }

    private void Update()
    {
        Vector2 input = GetMovementInput();

        Vector3 inputDir = new Vector3(
            input.x,
            0f,
            input.y
        );

        inputDir = Vector3.ClampMagnitude(inputDir, 1f);
        stick = inputDir.magnitude;

        if (Camera.main != null && inputDir.sqrMagnitude > 0.0001f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            moveDirection =
                (camForward * inputDir.z +
                 camRight * inputDir.x).normalized * inputDir.magnitude;
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        UpdateSprint();
        if (sprintAndJump && KeyboardJumpPressed()) RequestJump();

        // =========================================================
        // FOOTSTEP AUDIO
        // =========================================================

        // T03: no steps in the air; quicker steps while sprinting. The visible body steps on its own contacts.
        if (moveDirection.sqrMagnitude > 0.01f && (!sprintAndJump || grounded) && !BodyDrivesFootsteps)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayFootstepSFX();
                }

                footstepTimer = sprinting ? footstepInterval / ExplorationRules.SprintMultiplier : footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private Vector2 GetMovementInput()
    {
        if (!hasFocus || isPaused) return Vector2.zero;
        // Mobile joystick takes priority when it exists.
        if (mobileJoystick != null)
        {
            Vector2 joystickInput = mobileJoystick.Input;

            if (mobileJoystick.IsHeld || joystickInput.sqrMagnitude > 0f)
                return joystickInput;
        }

        // Keep keyboard input temporarily for editor testing.
#if UNITY_EDITOR || UNITY_STANDALONE
        if (useKeyboardInEditor)
        {
            return new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
        }
#endif

        return Vector2.zero;
    }

    // =========================================================
    // T03: SPRINT, JUMP, GROUND AND RECOVERY
    // =========================================================

    private void UpdateSprint()
    {
        bool was = sprinting;
        if (!sprintEnabled || !hasFocus || isPaused || moveDirection.sqrMagnitude < 0.01f) sprinting = false;
        else if (mobileJoystick != null && mobileJoystick.IsHeld) sprinting = ExplorationRules.Sprinting(sprinting, true, mobileJoystick.RawMagnitude);
        else sprinting = KeyboardSprintHeld();
        if (mobileJoystick != null) mobileJoystick.SetSprintLit(sprinting);
        if (sprinting && !was && !sprintCued)
        {
            sprintCued = true;
            GameplayEvents.RaiseGuideCue(CueSprinted);
        }
    }

    /// <summary>
    /// The right-side jump control (or Space in the Editor): one jump per press. The press is kept
    /// for <see cref="ExplorationRules.JumpBuffer"/> and used once at the next valid moment.
    /// </summary>
    public void RequestJump()
    {
        if (!sprintAndJump || !enabled || !hasFocus || isPaused) return;
        jumpRequested = true;
        jumpRequestedAt = Time.time;
    }

    /// <summary>T03: third-person outdoor exploration owns the pace (walk/run speeds).</summary>
    private bool ThirdPersonPace => sprintAndJump && CameraController.Instance != null && CameraController.Instance.IsThirdPersonView;

    private static bool KeyboardSprintHeld()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
        return false;
#endif
    }

    private bool KeyboardJumpPressed()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return useKeyboardInEditor && Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    private void SenseGround()
    {
        if (capsule == null) { grounded = true; return; }
        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        float radius = capsule.radius * scale;
        float half = Mathf.Max(radius, capsule.height * Mathf.Abs(transform.lossyScale.y) * 0.5f);
        Vector3 centre = transform.TransformPoint(capsule.center);
        Vector3 origin = centre + Vector3.down * (half - radius) + Vector3.up * 0.05f;
        int count = Physics.SphereCastNonAlloc(origin, radius * 0.9f, Vector3.down, groundHits, 0.05f + 0.12f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bool hit = false;
        float normalY = 0f;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = groundHits[i];
            if (h.collider == null || h.collider.transform.IsChildOf(transform)) continue;
            // Walls and steep faces touched by the sphere's side are not floor.
            if (h.normal.y < 0.55f) continue;
            hit = true;
            normalY = Mathf.Max(normalY, h.normal.y);
        }
        bool was = grounded;
        // A ground cast can still reach the floor during the first ascending frames.
        grounded = hit && rb.linearVelocity.y <= 0.1f && !(jumping && Time.time - lastJumpAt < 0.1f);
        groundNormalY = hit ? normalY : 0f;
        Vector3 feet = centre + Vector3.down * half;
        if (grounded)
        {
            lastGroundY = feet.y;
            groundBelow = -1f;
            // Genuine contact after real flight: one landing sound, the jump is over.
            if (!was && (jumping || (airborneSince >= 0f && Time.time - airborneSince > ExplorationRules.FallConfirm)) &&
                AudioManager.Instance != null) AudioManager.Instance.PlayFootstepSFX();
            jumping = false;
            jumpedSinceGrounded = false;
        }
        else
        {
            airVerticalSpeed = rb.linearVelocity.y;
            groundBelow = -1f;
            // Descending: a bounded probe lets the legs reach for an approaching floor.
            if (airVerticalSpeed < 0f && Physics.SphereCastNonAlloc(centre, radius * 0.8f, Vector3.down, groundHits, half - radius * 0.8f + 1.5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) is int n && n > 0)
            {
                float best = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    RaycastHit h = groundHits[i];
                    if (h.collider == null || h.collider.transform.IsChildOf(transform) || h.normal.y < 0.55f || h.distance <= 0f) continue;
                    best = Mathf.Min(best, h.distance - (half - radius * 0.8f));
                }
                if (best < float.MaxValue) groundBelow = Mathf.Max(0f, best);
            }
        }
    }

    private void TrackSafety()
    {
        float now = Time.time;
        if (grounded)
        {
            if (groundedSince < 0f) groundedSince = now;
            lastGroundedAt = now;
            airborneSince = -1f;
            if (ExplorationRules.SafeGround(true, groundNormalY, rb.linearVelocity.y, now - groundedSince))
            {
                safePoint = transform.position;
                hasSafePoint = true;
            }
            return;
        }
        groundedSince = -1f;
        if (airborneSince < 0f) airborneSince = now;
        if (hasSafePoint && ExplorationRules.NeedsRecovery(transform.position.y, safePoint.y, now - airborneSince)) Recover();
    }

    /// <summary>Back to the last grounded safe point, still and upright; nothing carried or saved changes.</summary>
    private void Recover()
    {
        if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        rb.position = safePoint;
        transform.position = safePoint;
        airborneSince = -1f;
        jumpRequested = false;
        windupStartedAt = -1f;
        jumping = false;
        lastGroundY = safePoint.y;
        Debug.Log("[PlayerMovement] Recovered to the last safe point after a fall.");
        Recovered?.Invoke();
    }

    private void TryJump()
    {
        // A committed windup launches once it has been seen (a few frames), keeping forward momentum.
        if (windupStartedAt >= 0f)
        {
            if (Time.time - windupStartedAt >= ExplorationRules.JumpWindup) Launch();
            return;
        }
        if (!jumpRequested) return;
        // A press older than the buffer is dropped, never stored for later; it is used at most once.
        if (Time.time - jumpRequestedAt > ExplorationRules.JumpBuffer) { jumpRequested = false; return; }
        if (!ExplorationRules.CanJump(true, grounded, Time.time - lastGroundedAt, Time.time - lastJumpAt, jumpedSinceGrounded)) return;
        jumpRequested = false;
        lastJumpAt = Time.time;
        jumpedSinceGrounded = true;
        // Off an edge already (coyote): no crouch in mid-air, launch now.
        if (grounded) windupStartedAt = Time.time; else Launch();
    }

    private void Launch()
    {
        windupStartedAt = -1f;
        lastJumpAt = Time.time;
        Vector3 v = rb.linearVelocity;
        v.y = ExplorationRules.JumpSpeed(jumpHeight, Physics.gravity.y);
        rb.linearVelocity = v;
        launchSpeed = v.y;
        jumping = true;
        jumpedSinceGrounded = true;
        grounded = false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstepSFX();
        if (!jumpCued)
        {
            jumpCued = true;
            GameplayEvents.RaiseGuideCue(CueJumped);
        }
    }

    private void OnDisable()
    {
        moveDirection = Vector3.zero;
        footstepTimer = 0f;
        // T03: holds, transitions and focus loss cancel sprint and any pending jump.
        sprinting = false;
        jumpRequested = false;
        windupStartedAt = -1f;
        stick = 0f;
        if (mobileJoystick != null) mobileJoystick.SetSprintLit(false);
        if (rb != null && !rb.isKinematic)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private void OnApplicationFocus(bool focused)
    {
        hasFocus = focused;
        if (!focused) OnDisable();
    }

    private void OnApplicationPause(bool paused)
    {
        isPaused = paused;
        if (paused) OnDisable();
    }

    private void FixedUpdate()
    {
        if (sprintAndJump)
        {
            SenseGround();
            TrackSafety();
            TryJump();
        }

        if (sprintAndJump)
        {
            // T03: outdoors the pace matches Elijah's walk/run; indoors keeps the first-person pace.
            // Controlled acceleration/braking; limited, momentum-keeping steering in the air.
            Vector3 wanted = moveDirection.sqrMagnitude > 1e-6f
                ? moveDirection.normalized * ExplorationRules.TargetSpeed(ThirdPersonPace, stick, sprinting, moveSpeed)
                : Vector3.zero;
            // Indoors on the ground the first-person pace stays as it was (immediate).
            Vector3 planar = ThirdPersonPace || !grounded
                ? ExplorationRules.ApproachVelocity(rb.linearVelocity, wanted, Time.fixedDeltaTime, grounded)
                : wanted;
            rb.linearVelocity = new Vector3(planar.x, rb.linearVelocity.y, planar.z);
        }
        else
        {
            float speed = sprinting ? moveSpeed * ExplorationRules.SprintMultiplier : moveSpeed;
            Vector3 targetVelocity = moveDirection * speed;

            rb.linearVelocity = new Vector3(
                targetVelocity.x,
                rb.linearVelocity.y,
                targetVelocity.z
            );
        }

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
    }
}
