using UnityEngine;

/// <summary>
/// Ambient back-and-forth stroll for idle NPCs (walking currently disabled by
/// default) and turning to face the player during this NPC's own dialogue. Presentation only: it stops
/// while the NPC is not Idle/Completed (the player walking into range makes it
/// Waiting) and while this NPC's own dialogue is open, when it turns to face
/// the player. Other NPCs' dialogue does not stop it. It never changes
/// dialogue, quest or FSM state.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NpcStateMachine))]
public sealed class NpcIdlePacing : MonoBehaviour
{
    [Tooltip("Walking is off until the walk animation problems are fixed (see Ticket 01 known issues). Facing the player in dialogue still works.")]
    [SerializeField] private bool enableWalking;
    [SerializeField, Min(0f)] private float pacingDistance = 3f;
    [Tooltip("Stroll direction relative to the NPC's starting facing (right = side to side).")]
    [SerializeField] private Vector3 localDirection = Vector3.right;
    [Tooltip("Used only until the Walking clip's own speed is known, and for static models.")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 1f;
    [SerializeField, Min(1f)] private float turnDegreesPerSecond = 120f;
    [SerializeField] private Vector2 pauseSeconds = new Vector2(1.5f, 4f);
    [Tooltip("Static meshes have no legs, so walking only slides them. Off by default.")]
    [SerializeField] private bool allowStaticModels;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private const float WalkWhileTurningDegrees = 45f;
    private const float AccelerationSeconds = 0.3f;

    private NpcStateMachine stateMachine;
    private NpcAnimationDriver driver;
    private DialogueUI dialogueUI;
    private Vector3 pointA, pointB;
    private bool towardB = true;
    private float pauseUntil;
    private float speedBlend;

    private void Awake()
    {
        stateMachine = GetComponent<NpcStateMachine>();
        driver = GetComponent<NpcAnimationDriver>();
        dialogueUI = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);

        Vector3 home = transform.position;
        Vector3 direction = transform.rotation * localDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) direction = transform.right;
        direction.Normalize();
        float half = pacingDistance * 0.5f;
        pointA = home - direction * ClearDistance(home, -direction, half);
        pointB = home + direction * ClearDistance(home, direction, half);
        pauseUntil = Time.time + Random.Range(0f, pauseSeconds.y);
    }

    private void OnDisable()
    {
        SetWalking(false);
    }

    private void Update()
    {
        if (IsOwnDialogueActive())
        {
            SetWalking(false);
            Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
            if (player != null)
                TurnToward(player.position - transform.position, turnDegreesPerSecond * 2f);
            return;
        }

        if (!enableWalking || !CanPace() || Time.time < pauseUntil)
        {
            SetWalking(false);
            return;
        }

        Vector3 position = transform.position;
        Vector3 toTarget = (towardB ? pointB : pointA) - position;
        toTarget.y = 0f;
        float remaining = toTarget.magnitude;
        // Arrived, or walked just past the end point.
        if (remaining < 0.15f || (remaining < 0.6f && Vector3.Dot(toTarget, Facing) <= 0f))
        {
            towardB = !towardB;
            pauseUntil = Time.time + Random.Range(pauseSeconds.x, Mathf.Max(pauseSeconds.x, pauseSeconds.y));
            SetWalking(false);
            return;
        }

        // Turn in place for large angles; walk forward (never sideways) while
        // finishing smaller turns so feet follow the body.
        float angle = TurnToward(toTarget, turnDegreesPerSecond);
        bool walking = angle < WalkWhileTurningDegrees;
        SetWalking(walking);
        if (!walking)
            return;

        speedBlend = Mathf.MoveTowards(speedBlend, 1f, Time.deltaTime / AccelerationSeconds);
        float speed = (driver != null && driver.AnimatedWalkSpeed > 0f ? driver.AnimatedWalkSpeed : walkSpeed) * speedBlend;
        transform.position = position + Facing * Mathf.Min(speed * Time.deltaTime, remaining);
    }

    private void SetWalking(bool walking)
    {
        if (!walking) speedBlend = 0f;
        if (driver != null) driver.SetLocomotion(walking);
    }

    private bool CanPace()
    {
        if (pacingDistance < 0.1f || stateMachine == null)
            return false;
        if (!(stateMachine.CurrentState is NpcIdleState || stateMachine.CurrentState is NpcCompletedState))
            return false;
        if (driver == null)
            return allowStaticModels;
        return driver.HasAnimatedWalk || (allowStaticModels && driver.SupportsWalking);
    }

    private bool IsOwnDialogueActive()
    {
        if (dialogueUI == null || !dialogueUI.IsDialogueActive)
            return false;
        Component owner = dialogueUI.CurrentOwner as Component;
        return owner != null && (owner.transform == transform || owner.transform.IsChildOf(transform));
    }

    /// <summary>Rotates toward a flat direction and returns the remaining angle.</summary>
    private float TurnToward(Vector3 direction, float degreesPerSecond)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return 0f;
        // Aim the model's face, not the root's +Z (models sit rotated inside).
        Quaternion target = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Inverse(FacingOffset);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * Time.deltaTime);
        return Quaternion.Angle(transform.rotation, target);
    }

    private Quaternion FacingOffset => driver != null ? driver.FacingOffset : Quaternion.identity;

    private Vector3 Facing => transform.rotation * FacingOffset * Vector3.forward;

    private float ClearDistance(Vector3 home, Vector3 direction, float distance)
    {
        const float clearance = 0.5f;
        Vector3 origin = home + Vector3.up * 0.9f;
        float clear = distance;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, direction, distance + clearance, obstacleMask, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform))
                clear = Mathf.Min(clear, hit.distance - clearance);
        return Mathf.Max(0f, clear);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 direction = transform.rotation * localDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();
        Vector3 home = Application.isPlaying ? (pointA + pointB) * 0.5f : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(home - direction * pacingDistance * 0.5f, home + direction * pacingDistance * 0.5f);
    }
}
