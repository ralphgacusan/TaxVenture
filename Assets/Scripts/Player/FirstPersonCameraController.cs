
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// FIRST-PERSON CAMERA CONTROLLER
///
/// Controls the Main Camera ONLY during normal FirstPerson mode.
///
/// When CameraController changes to:
/// - Workstation
/// - Interview
///
/// this script stops controlling the camera even if the component
/// accidentally gets re-enabled by another script or Unity.
/// </summary>
public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Player transform used to position the first-person camera.")]
    [SerializeField] private Transform player;

    [Header("Camera Position")]
    [Tooltip("Height of the camera above the player's position.")]
    [SerializeField] private float cameraHeight = 0.8f;

    [Header("Look Settings")]
    [Tooltip("Touch look sensitivity.")]
    [SerializeField] private float sensitivity = 0.15f;

    [Header("Vertical Look")]
    [SerializeField] private float minPitch = -60f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Touch Look")]
    [SerializeField] private bool disableTouchLook = false;
    [SerializeField, Range(0.2f, 1f)] private float touchLookScreenWidth = 0.75f;

    [Header("Mouse Look")]
    [Tooltip("Mouse look sensitivity for Editor/Desktop testing.")]
    [SerializeField] private float mouseSensitivity = 3f;

    // Milestone T03: the same writer also places the outdoor third-person view
    // (Office_Level1 only). Look angles are shared, so changing view never
    // turns the player's look; CameraController decides which view is wanted.
    // Composition (distance, pivot, screen offset, pitch bias) is solved in ExplorationRules
    // from the canonical standing height and this camera's own vertical FOV.
    [Header("Third Person (outdoor exploration)")]
    [SerializeField] private float thirdPersonCollisionRadius = 0.18f;
    [SerializeField] private float viewBlendSeconds = 0.35f;

    private float yaw;
    private float pitch;

    private bool thirdPerson;
    private float viewWeight;          // 0 = first person, 1 = third person
    private float viewWeightFrom;
    private float viewBlendElapsed = -1f;
    private float orbitDistance = -1f;
    private float clearSeconds = float.MaxValue;
    private float trackedFloor = float.NaN;
    private bool hidingBodyClose;
    private Camera viewCamera;
    private readonly RaycastHit[] orbitHits = new RaycastHit[8];
    private readonly System.Collections.Generic.Dictionary<Collider, bool> personColliders = new System.Collections.Generic.Dictionary<Collider, bool>();

    private int activeLookFingerId = -1;

    // Re-locking the cursor (after a popup, conversation or tutorial page)
    // reports one huge mouse delta in the Editor/PC, which snapped the view
    // to the pitch limit. Ignore the mouse for a few frames after that.
    private int ignoreMouseFrames;
    private CursorLockMode lastLockState;

    private void OnEnable()
    {
        ignoreMouseFrames = 3;
        lastLockState = Cursor.lockState;
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError(
                "[FirstPersonCameraController] Player is not assigned."
            );

            return;
        }

        yaw = player.eulerAngles.y;
        pitch = 0f;

#if UNITY_EDITOR || UNITY_STANDALONE
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        // ============================================================
        // IMPORTANT:
        // This controller is allowed to control the camera ONLY
        // during normal FirstPerson mode.
        // ============================================================

        if (CameraController.Instance != null &&
            CameraController.Instance.CurrentMode !=
            CameraController.CameraMode.FirstPerson)
        {
            return;
        }

        HandleMouseLook();
        HandleTouchLook();

        if (!thirdPerson && viewWeight <= 0f && viewBlendElapsed < 0f)
        {
            transform.position =
                player.position +
                Vector3.up * cameraHeight;

            transform.rotation =
                Quaternion.Euler(
                    pitch,
                    yaw,
                    0f
                );
            return;
        }

        // T03: third person, or the short blend between the two views.
        AdvanceViewBlend(Time.deltaTime);
        if (thirdPerson) pitch = ClampThirdPersonLook(pitch);
        Pose fp = FirstPersonPose();
        Pose tp = ThirdPersonPose(Time.deltaTime, true);
        float k = Mathf.SmoothStep(0f, 1f, viewWeight);
        transform.SetPositionAndRotation(Vector3.Lerp(fp.position, tp.position, k), Quaternion.Slerp(fp.rotation, tp.rotation, k));
    }

    // =============================================================
    // T03: THIRD-PERSON VIEW (same writer, shared look angles)
    // =============================================================

    /// <summary>The view this controller is heading to (true = outdoor third person).</summary>
    public bool ThirdPerson => thirdPerson;

    /// <summary>A view change is still blending; the next change waits for it.</summary>
    public bool IsBlending => viewBlendElapsed >= 0f;

    /// <summary>Distance from the camera's view to the player's eye in the current view (0 in first person).</summary>
    public float ViewToEyeDistance => player != null ? Vector3.Distance(transform.position, player.position + Vector3.up * cameraHeight) : 0f;

    /// <summary>The player's eye (first-person camera point), used for reach and line of sight.</summary>
    public Vector3 EyePosition => player != null ? player.position + Vector3.up * cameraHeight : transform.position;

    /// <summary>Asks for the outdoor third-person (true) or indoor first-person view; instant skips the blend (load/reload).</summary>
    public void SetThirdPerson(bool on, bool instant)
    {
        if (on == thirdPerson && !instant) return;
        thirdPerson = on;
        if (on) { orbitDistance = -1f; trackedFloor = float.NaN; clearSeconds = float.MaxValue; }
        if (on) pitch = ClampThirdPersonLook(pitch);
        if (!on) ReleaseCloseHide();
        if (instant || viewBlendSeconds <= 0f)
        {
            viewWeight = on ? 1f : 0f;
            viewBlendElapsed = -1f;
            return;
        }
        viewWeightFrom = viewWeight;
        viewBlendElapsed = 0f;
    }

    private void AdvanceViewBlend(float dt)
    {
        if (viewBlendElapsed < 0f) return;
        viewBlendElapsed += Mathf.Max(0f, dt);
        float t = Mathf.Clamp01(viewBlendElapsed / Mathf.Max(0.01f, viewBlendSeconds));
        viewWeight = Mathf.Lerp(viewWeightFrom, thirdPerson ? 1f : 0f, t);
        if (t >= 1f) viewBlendElapsed = -1f;
    }

    private Pose FirstPersonPose() =>
        new Pose(player.position + Vector3.up * cameraHeight, Quaternion.Euler(pitch, yaw, 0f));

    /// <summary>The shared look pitch kept inside the third-person camera limits (after the neutral bias).</summary>
    private static float ClampThirdPersonLook(float look) =>
        Mathf.Clamp(look, ExplorationRules.CameraMinPitch - ExplorationRules.CameraPitchBias, ExplorationRules.CameraMaxPitch - ExplorationRules.CameraPitchBias);

    private float VerticalFov
    {
        get
        {
            if (viewCamera == null) viewCamera = GetComponent<Camera>() != null ? GetComponent<Camera>() : Camera.main;
            return viewCamera != null ? viewCamera.fieldOfView : 60f;
        }
    }

    /// <summary>
    /// The full standing body in the lower middle of the view, the town ahead above it. The
    /// follow pivot (hips over the soles, on a floor height that ignores jump bob) is separate
    /// from the composition, which raises the camera along its own up axis so the pivot stays
    /// below centre at any pitch. Obstacles pull the camera in along pivot->camera at once;
    /// it eases back out only after the way has stayed clear.
    /// </summary>
    private Pose ThirdPersonPose(float dt, bool advance)
    {
        CameraController owner = CameraController.Instance;
        ExplorationBody body = owner != null && owner.MixedExplorationAdopted ? owner.Body : null;
        PlayerMovement mover = owner != null ? owner.Movement : null;
        float height = ExplorationRules.ElijahRestHeight;
        Vector3 feet = body != null ? body.Feet : player.position;
        bool grounded = mover == null || !mover.SprintAndJumpEnabled || mover.IsGrounded;
        float takeoff = mover != null ? mover.LastGroundY : feet.y;
        float floor = float.IsNaN(trackedFloor) ? feet.y : ExplorationRules.TrackFloor(trackedFloor, feet.y, grounded, takeoff, dt);
        if (advance || float.IsNaN(trackedFloor)) trackedFloor = floor;
        Vector3 pivot = new Vector3(feet.x, floor + height * ExplorationRules.CameraPivotFraction, feet.z);

        Quaternion rotation = Quaternion.Euler(Mathf.Clamp(pitch + ExplorationRules.CameraPitchBias,
            ExplorationRules.CameraMinPitch, ExplorationRules.CameraMaxPitch), yaw, 0f);
        Vector3 desired = ExplorationRules.CameraPosition(pivot, rotation, height * ExplorationRules.CameraDistanceHeights,
            ExplorationRules.CameraScreenOffset, VerticalFov);
        Vector3 toCamera = desired - pivot;
        float length = toCamera.magnitude;
        Vector3 direction = length > 0.001f ? toCamera / length : -player.forward;
        float? hit = NearestObstacle(pivot, direction, length);
        float allowed = ExplorationRules.CollisionDistance(length, hit);
        if (orbitDistance < 0f) orbitDistance = allowed;
        if (advance) clearSeconds = allowed < length - 0.01f ? 0f : (clearSeconds >= float.MaxValue ? clearSeconds : clearSeconds + Mathf.Max(0f, dt));
        float distance = ExplorationRules.FollowDistance(orbitDistance, allowed, dt, clearSeconds);
        if (advance || distance < orbitDistance) orbitDistance = distance;
        if (advance && body != null) KeepBodyReadable(body, orbitDistance);
        return new Pose(pivot + direction * orbitDistance, rotation);
    }

    /// <summary>
    /// Squeezed against a wall the camera would sit inside Elijah's head: hide the body until there is
    /// room again (the existing ownership-based visibility), with hysteresis so it never flickers.
    /// </summary>
    private void KeepBodyReadable(ExplorationBody body, float distance)
    {
        bool tooClose = hidingBodyClose ? distance < 0.75f : distance < 0.55f;
        if (tooClose == hidingBodyClose) return;
        hidingBodyClose = tooClose;
        if (tooClose) body.HideFor(this); else body.ReleaseHide(this);
    }

    private void ReleaseCloseHide()
    {
        if (!hidingBodyClose) return;
        hidingBodyClose = false;
        if (ExplorationBody.Current != null) ExplorationBody.Current.ReleaseHide(this);
    }

    private float? NearestObstacle(Vector3 from, Vector3 direction, float distance)
    {
        int count = Physics.SphereCastNonAlloc(from, thirdPersonCollisionRadius, direction, orbitHits, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float? nearest = null;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = orbitHits[i];
            if (h.collider == null || h.collider.transform.IsChildOf(player)) continue;
            // People (NPCs walking or standing near the player) never zoom the view onto a face.
            if (IsPerson(h.collider)) continue;
            // The camera path rises behind the player: benches, curbs and low hedges below it are passed over.
            if (h.distance > 0f && ExplorationRules.PassesOver(h.collider.bounds.max.y, from.y + direction.y * h.distance, thirdPersonCollisionRadius)) continue;
            // A cast starting inside a collider reports distance 0: treat it as fully blocked.
            float d = h.distance;
            if (!nearest.HasValue || d < nearest.Value) nearest = d;
        }
        return nearest;
    }

    private bool IsPerson(Collider collider)
    {
        if (personColliders.TryGetValue(collider, out bool person)) return person;
        if (personColliders.Count > 512) personColliders.Clear();
        person = collider.GetComponentInParent<INpcAvailabilitySource>() != null || collider.GetComponentInParent<NpcLife>() != null ||
                 collider.GetComponentInParent<NpcLocomotion>() != null || collider.GetComponentInParent<Animator>() != null;
        personColliders[collider] = person;
        return person;
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null)
            return;
        if (VirtualJoystick.BlocksWorldInput || MobileInputGuard.HasTouchInput ||
            (Cursor.lockState != CursorLockMode.Locked && MobileInputGuard.IsPointerOverUI(Mouse.current.position.ReadValue()))) return;

        if (Cursor.lockState != lastLockState)
        {
            lastLockState = Cursor.lockState;
            ignoreMouseFrames = 3;
        }
        if (ignoreMouseFrames > 0)
        {
            ignoreMouseFrames--;
            return;
        }

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        if (mouseDelta.sqrMagnitude < 0.000001f)
            return;

        yaw +=
            mouseDelta.x *
            mouseSensitivity;

        pitch -=
            mouseDelta.y *
            mouseSensitivity;

        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );
    }

    private void HandleTouchLook()
    {
        if (disableTouchLook)
            return;

        if (Touchscreen.current == null)
            return;

        var touches =
            Touchscreen.current.touches;

        // Find a new look finger
        if (activeLookFingerId == -1)
        {
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];

                if (!touch.press.wasPressedThisFrame ||
                    touch.position.ReadValue().x < Screen.width * (1f - touchLookScreenWidth))
                    continue;

                int fingerId =
                    touch.touchId.ReadValue();

                // Ignore UI touches
                if (MobileInputGuard.IsPointerOverUI(touch.position.ReadValue()))
                    continue;

                activeLookFingerId =
                    fingerId;

                break;
            }
        }

        if (activeLookFingerId == -1)
            return;

        // Process active look finger
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];

            int fingerId =
                touch.touchId.ReadValue();

            if (fingerId != activeLookFingerId)
                continue;

            // Finger released
            if (!touch.press.isPressed)
            {
                activeLookFingerId = -1;
                return;
            }

            Vector2 touchDelta =
                touch.delta.ReadValue() * (1080f / Mathf.Max(1, Screen.height));

            yaw +=
                touchDelta.x *
                sensitivity;

            pitch -=
                touchDelta.y *
                sensitivity;

            pitch =
                Mathf.Clamp(
                    pitch,
                    minPitch,
                    maxPitch
                );

            return;
        }

        activeLookFingerId = -1;
    }

    private void OnDisable()
    {
        activeLookFingerId = -1;
        ReleaseCloseHide();
    }

    private void OnApplicationFocus(bool focused) { if (!focused) OnDisable(); }
    private void OnApplicationPause(bool paused) { if (paused) OnDisable(); }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;

        if (player != null)
        {
            yaw =
                player.eulerAngles.y;

            pitch = 0f;
        }
    }

    /// <summary>
    /// The exact pose this controller writes in normal first-person mode. Used by
    /// the conversation presentation to return precisely to it before input resumes.
    /// </summary>
    public bool TryGetViewPose(out Vector3 position, out Quaternion rotation)
    {
        if (player == null)
        {
            position = default;
            rotation = Quaternion.identity;
            return false;
        }

        // T03: the view it is settling on (outdoor third person or first person), never a mid-blend pose.
        if (thirdPerson)
        {
            Pose tp = ThirdPersonPose(0f, false);
            position = tp.position;
            rotation = tp.rotation;
            return true;
        }

        position = player.position + Vector3.up * cameraHeight;
        rotation = Quaternion.Euler(pitch, yaw, 0f);
        return true;
    }

    /// <summary>The current look angles (degrees).</summary>
    public float Yaw => yaw;
    public float Pitch => pitch;

    /// <summary>
    /// Sets the look angles (pitch kept inside the limits), e.g. after a guided
    /// turn toward the Supply Counter (ViewTurn), so control resumes from there.
    /// </summary>
    public void SetLook(float newYaw, float newPitch)
    {
        yaw = newYaw;
        pitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
    }

    public void ResetLook()
    {
        if (player == null)
            return;

        yaw =
            player.eulerAngles.y;

        pitch = 0f;
    }
}

