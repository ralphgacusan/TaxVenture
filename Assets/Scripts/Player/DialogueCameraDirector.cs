using System;
using UnityEngine;
using NVec = System.Numerics.Vector3;

/// <summary>
/// T03 checkpoint 2: conversation-only shot/reverse-shot and the return to the
/// exact first-person view. Lives on the CameraController object and acts only
/// while DialogueUI has admitted a conversation and holds the controls
/// (FirstPersonCameraController is disabled then, so this is the single camera
/// writer). Exploration never runs this code.
///
/// Order, driven by DialogueUI through IDialoguePresentation:
///   BeginConversation → capture first-person pose/FOV, hands and carried
///     document; resolve the NPC; stand the Elijah visual; precompute shots.
///   ShowLine → NPC lines frame over Elijah's right shoulder at the NPC, player
///     lines over the NPC's left shoulder at Elijah (same side of the axis).
///     Lines that "land" (or are marked CloseUp) get a solo close-up of the
///     speaker instead (DialogueCloseUpPolicy). Blocked shots shorten the
///     pull-back, then fall back to a two-shot.
///   BeginReturn → back to the captured first-person view; DialogueUI holds
///     control release and every completion/cancellation callback until then.
///
/// CAMERA CHANGES ARE INSTANT CUTS (by request). The smooth blends are kept,
/// commented out, in ShowLine and BeginReturn: uncomment them there to bring
/// the transitions back. The blend durations below are kept for that.
/// If this component is disabled or the scene unloads, the view, hands and
/// visual are restored immediately and a pending return completes at once.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class DialogueCameraDirector : MonoBehaviour, IDialoguePresentation
{
    [Header("Wiring")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private ConversationActorVisual playerVisual;

    [Header("Shots (fractions of character height)")]
    [SerializeField, Range(0.2f, 1.2f)] private float backDistance = 0.5f;
    [Tooltip("How far to the side of the listener the camera sits. Larger = more side-on, so the listener hides less of the speaker.")]
    [SerializeField, Range(0f, 0.8f)] private float shoulderOffset = 0.4f;
    [SerializeField, Range(-0.3f, 0.3f)] private float heightOffset = 0.04f;
    [SerializeField, Range(0f, 0.5f)] private float framingBias = 0.12f;
    [SerializeField, Range(0.6f, 2.5f)] private float twoShotDistance = 1.25f;
    [Tooltip("Field of view during shots. 0 keeps the first-person field of view.")]
    [SerializeField, Range(0f, 90f)] private float shotFieldOfView;

    [Header("Elijah placement (fractions of character height)")]
    [SerializeField, Range(0.2f, 1.5f)] private float minSeparation = 0.5f;
    [SerializeField, Range(0.3f, 2f)] private float maxSeparation = 0.85f;

    [Header("Close-ups (solo shot of the speaker)")]
    [SerializeField] private bool enableCloseUps = true;
    [Tooltip("Distance in front of the speaker's face, as a fraction of height.")]
    [SerializeField, Range(0.15f, 0.6f)] private float closeUpDistance = 0.3f;
    [SerializeField, Range(0f, 0.3f)] private float closeUpSideOffset = 0.1f;
    [Tooltip("Content score needed for a close-up (see DialogueCloseUpPolicy.Score).")]
    [SerializeField, Range(0.5f, 5f)] private float closeUpThreshold = 2f;
    [Tooltip("Chance of a close-up on a borderline line.")]
    [SerializeField, Range(0f, 1f)] private float closeUpRandomChance = 0.15f;
    [Tooltip("Other lines required between two automatic close-ups.")]
    [SerializeField, Range(0, 6)] private int closeUpMinLinesBetween = 2;

    // Blends are currently unused: camera changes are instant cuts by request.
    // They stay serialized so the commented-out blend calls can be restored.
#pragma warning disable 0414
    [Header("Blends (seconds, unscaled) - unused while instant cuts are active")]
    [SerializeField, Range(0f, 2f)] private float entryBlend = 0.45f;
    [SerializeField, Range(0f, 2f)] private float shotBlend = 0.35f;
    [SerializeField, Range(0f, 2f)] private float returnBlend = 0.45f;
#pragma warning restore 0414

    [Header("Obstruction")]
    [SerializeField] private LayerMask obstructionMask = Physics.DefaultRaycastLayers;
    [Tooltip("Camera clearance radius as a fraction of character height.")]
    [SerializeField, Range(0.01f, 0.2f)] private float clearanceRadius = 0.05f;
    [Tooltip("Hide Elijah while the camera is closer than this (fraction of height) to his head.")]
    [SerializeField, Range(0.1f, 0.6f)] private float headClearance = 0.3f;

    // Bumped when default framing changes so scenes saved with the old
    // defaults pick up the new ones (values the user changed are kept).
    private const int CurrentSettingsVersion = 1;
    [SerializeField, HideInInspector] private int settingsVersion;

    private static readonly float[] BackScales = { 1f, 0.75f, 0.5f };

    private enum Phase { Idle, Presenting, Returning }

    private Phase phase;
    private Camera cam;
    private Vector3 fpPosition;
    private Quaternion fpRotation;
    private float fpFov;
    private float fpNear = -1f;

    // Close-ups used to sit ~0.5 m from the head centre while the main camera
    // clips everything nearer than 0.3 m, so low-poly heads were sliced open.
    // During a conversation the near plane is pulled in, and a close-up never
    // comes closer than this (fraction of character height) to the head.
    private const float ConversationNearClip = 0.03f;
    private const float MinCloseUpDistance = 0.55f;
    private const float MinCameraToHead = 0.42f;
    private FirstPersonHands hands;
    private bool handsHidden;
    private bool handsWereShowing;
    private bool documentWasShowing;
    private Transform playerRoot;

    private bool framed;
    private DialogueSpeakerFrame npc;
    private Vector3 elijahHead;
    private float scale;
    private Pose npcLinePose;
    private Pose playerLinePose;
    private Pose npcCloseUpPose;
    private Pose playerCloseUpPose;
    private bool hasNpcCloseUp;
    private bool hasPlayerCloseUp;
    private readonly DialogueCloseUpPolicy closeUpPolicy = new DialogueCloseUpPolicy();
    private bool hasTarget;
    private Pose currentTarget;

    private bool blending;
    private Vector3 fromPos;
    private Quaternion fromRot;
    private float fromFov;
    private Vector3 toPos;
    private Quaternion toRot;
    private float toFov;
    private float blendTime;
    private float blendDuration;

    private Action onReturned;
    private bool ownershipLost;
    private bool registered;

    private readonly RaycastHit[] hits = new RaycastHit[16];
    private readonly Collider[] overlaps = new Collider[16];

    /// <summary>Which shot the last line selected (for diagnostics and tests in Play mode).</summary>
    public string LastShotDescription { get; private set; } = "none";
    public bool IsPresenting => phase != Phase.Idle;

    private void Awake() => MigrateSettings();

    private void OnValidate() => MigrateSettings();

    private void MigrateSettings()
    {
        if (settingsVersion >= CurrentSettingsVersion) return;
        // v1: wider side angle (old defaults 0.55 / 0.24 / 0.18 put the camera too
        // far behind the listener, who then hid the speaker).
        if (Mathf.Approximately(backDistance, 0.55f)) backDistance = 0.5f;
        if (Mathf.Approximately(shoulderOffset, 0.24f)) shoulderOffset = 0.4f;
        if (Mathf.Approximately(framingBias, 0.18f)) framingBias = 0.12f;
        settingsVersion = CurrentSettingsVersion;
    }

    private void OnEnable()
    {
        if (dialogueUI == null) dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (dialogueUI != null)
        {
            dialogueUI.RegisterPresentation(this);
            registered = true;
        }
        else
        {
            Debug.LogWarning("[DialogueCameraDirector] No DialogueUI found; conversations stay first person.", this);
        }
    }

    private void OnDisable()
    {
        if (registered && dialogueUI != null) dialogueUI.UnregisterPresentation(this);
        registered = false;

        // A disabled or unloading director cannot finish a blend: restore now.
        if (phase == Phase.Returning) FinishReturn(true);
        else if (phase == Phase.Presenting) RestoreImmediately();
    }

    // ------------------------------------------------------------------
    // IDialoguePresentation
    // ------------------------------------------------------------------

    public void BeginConversation(object owner)
    {
        if (phase != Phase.Idle) RestoreImmediately();
        if (!isActiveAndEnabled) return;

        CameraController controller = CameraController.Instance;
        if (controller == null) return;
        cam = controller.MainCamera;
        if (cam == null) return;

        // Capture the LOGICAL first-person view (what the first-person controller
        // will resume on release), not just the camera transform: a conversation
        // can start in the same frame another mode handed the camera back.
        if (!controller.TryGetFirstPersonView(out fpPosition, out fpRotation))
        {
            fpPosition = cam.transform.position;
            fpRotation = cam.transform.rotation;
        }
        fpFov = cam.fieldOfView;
        if (fpNear < 0f) fpNear = cam.nearClipPlane;
        cam.nearClipPlane = Mathf.Min(fpNear, ConversationNearClip);
        hands = controller.Hands;
        playerRoot = controller.PlayerRoot;
        ownershipLost = false;
        phase = Phase.Presenting;
        closeUpPolicy.Threshold = closeUpThreshold;
        closeUpPolicy.RandomChance = closeUpRandomChance;
        closeUpPolicy.MinLinesBetween = closeUpMinLinesBetween;
        closeUpPolicy.Reset(unchecked(Environment.TickCount * 31 + (owner != null ? owner.GetHashCode() : 0)));

        framed = TryFrame(owner as Component);
        if (framed && hands != null)
        {
            handsWereShowing = hands.IsShowing;
            documentWasShowing = hands.IsShowingCarriedDocument;
            hands.Hide();
            hands.HideCarriedDocument();
            handsHidden = true;
        }
    }

    public void ShowLine(DialogueLine line, int index)
    {
        if (phase != Phase.Presenting || cam == null || !framed || line == null || ownershipLost) return;

        bool playerLine = line.Speaker == DialogueSpeaker.Player;
        bool closeUpAvailable = playerLine ? hasPlayerCloseUp : hasNpcCloseUp;
        bool closeUp = enableCloseUps && closeUpAvailable && closeUpPolicy.Decide(line, index);
        Pose target = closeUp
            ? (playerLine ? playerCloseUpPose : npcCloseUpPose)
            : (playerLine ? playerLinePose : npcLinePose);
        LastShotDescription = (playerLine ? "player " : "npc ") + (closeUp ? "close-up" : "over-shoulder");
        if (hasTarget && target.position == currentTarget.position && target.rotation == currentTarget.rotation) return;

        float fov = shotFieldOfView > 0f ? shotFieldOfView : fpFov;

        // --- Smooth transition between camera angles (disabled: instant cuts) ---
        // To bring the transition back, uncomment the next line and delete the
        // CutTo(...) line after it.
        // StartBlend(target.position, target.rotation, fov, hasTarget ? shotBlend : entryBlend);
        CutTo(target.position, target.rotation, fov);

        currentTarget = target;
        hasTarget = true;
    }

    public bool BeginReturn(DialogueEndReason reason, Action returned)
    {
        if (phase != Phase.Presenting) return false;
        if (!isActiveAndEnabled || cam == null || ownershipLost)
        {
            RestoreImmediately();
            return false;
        }

        // Nothing moved (no framable speaker): release without an empty wait.
        bool atFirstPerson = (cam.transform.position - fpPosition).sqrMagnitude < 1e-6f &&
                             Quaternion.Angle(cam.transform.rotation, fpRotation) < 0.05f &&
                             Mathf.Abs(cam.fieldOfView - fpFov) < 0.01f;
        if (atFirstPerson && !framed)
        {
            RestoreImmediately();
            return false;
        }

        // --- Smooth return to first person (disabled: instant cuts) ---
        // To bring it back, uncomment these four lines and delete the two
        // instant-cut lines below them. DialogueUI already waits for the blend.
        // onReturned = returned;
        // phase = Phase.Returning;
        // StartBlend(fpPosition, fpRotation, fpFov, returnBlend);
        // return true;
        RestoreImmediately();
        return false;
    }

    public void AbortConversation()
    {
        if (phase == Phase.Idle) return;
        onReturned = null;
        RestoreImmediately();
    }

    // ------------------------------------------------------------------
    // Per-frame camera writing (only while a conversation owns the controls)
    // ------------------------------------------------------------------

    private void LateUpdate()
    {
        if (phase == Phase.Idle) return;
        if (cam == null)
        {
            if (phase == Phase.Returning) FinishReturn(false);
            else ResetState();
            return;
        }

        if (!OwnsCamera())
        {
            // Another camera mode took over; never restore over its owner.
            ownershipLost = true;
            blending = false;
            if (playerVisual != null) playerVisual.Hide();
            if (phase == Phase.Returning) FinishReturn(false);
            return;
        }

        if (blending)
        {
            blendTime += Time.unscaledDeltaTime;
            float t = blendDuration <= 0f ? 1f : DialogueShotMath.Ease(blendTime / blendDuration);
            cam.transform.SetPositionAndRotation(
                Vector3.LerpUnclamped(fromPos, toPos, t),
                Quaternion.SlerpUnclamped(fromRot, toRot, t));
            cam.fieldOfView = Mathf.LerpUnclamped(fromFov, toFov, t);
            if (blendTime >= blendDuration) blending = false;
        }

        UpdateVisualVisibility();

        if (phase == Phase.Returning && !blending) FinishReturn(true);
    }

    private bool OwnsCamera()
    {
        CameraController controller = CameraController.Instance;
        return controller != null && controller.CurrentMode == CameraController.CameraMode.FirstPerson;
    }

    /// <summary>Instant camera change (no transition).</summary>
    private void CutTo(Vector3 position, Quaternion rotation, float fov)
    {
        blending = false;
        cam.transform.SetPositionAndRotation(position, rotation);
        cam.fieldOfView = fov;
        UpdateVisualVisibility();
    }

    private void StartBlend(Vector3 position, Quaternion rotation, float fov, float duration)
    {
        fromPos = cam.transform.position;
        fromRot = cam.transform.rotation;
        fromFov = cam.fieldOfView;
        toPos = position;
        toRot = rotation;
        toFov = fov;
        blendTime = 0f;
        blendDuration = Mathf.Max(0f, duration);
        blending = true;
    }

    private void UpdateVisualVisibility()
    {
        if (!framed || playerVisual == null) return;
        bool clear = Vector3.Distance(cam.transform.position, elijahHead) > headClearance * scale;
        playerVisual.SetVisible(clear && !ownershipLost);
    }

    private void FinishReturn(bool restoreCamera)
    {
        if (restoreCamera && cam != null && !ownershipLost)
        {
            cam.transform.SetPositionAndRotation(fpPosition, fpRotation);
            cam.fieldOfView = fpFov;
        }
        RestoreNearClip();
        RestoreHandsAndVisual();
        Action callback = onReturned;
        ResetState();
        // State is clear before the callback: it may start the next conversation.
        callback?.Invoke();
    }

    private void RestoreImmediately()
    {
        if (cam != null && !ownershipLost && OwnsCamera())
        {
            cam.transform.SetPositionAndRotation(fpPosition, fpRotation);
            cam.fieldOfView = fpFov;
        }
        RestoreNearClip();
        RestoreHandsAndVisual();
        ResetState();
    }

    private void RestoreNearClip()
    {
        if (cam != null && fpNear >= 0f) cam.nearClipPlane = fpNear;
        fpNear = -1f;
    }

    private void RestoreHandsAndVisual()
    {
        if (playerVisual != null) playerVisual.Hide();
        if (!handsHidden || hands == null || ownershipLost) return;
        if (handsWereShowing) hands.Show();
        if (documentWasShowing) hands.ShowCarriedDocument();
    }

    private void ResetState()
    {
        phase = Phase.Idle;
        framed = false;
        hasTarget = false;
        hasNpcCloseUp = false;
        hasPlayerCloseUp = false;
        blending = false;
        handsHidden = false;
        onReturned = null;
        npc = default;
        LastShotDescription = "none";
    }

    // ------------------------------------------------------------------
    // Framing
    // ------------------------------------------------------------------

    private bool TryFrame(Component owner)
    {
        if (owner == null || playerVisual == null) return false;
        if (!DialogueSpeakerFrames.TryResolve(owner, out npc)) return false;

        float elijahHeight = playerVisual.StandingHeight;
        scale = Mathf.Max(0.05f, (npc.Height + elijahHeight) * 0.5f);

        Vector3 playerPosition = playerRoot != null ? playerRoot.position : fpPosition;
        Vector3 playerFloor = FindFloor(playerPosition, npc.Floor.y);
        Vector3 placed = ToU(DialogueShotMath.PlaceListener(ToN(playerFloor), ToN(npc.Floor),
            minSeparation * scale, maxSeparation * scale, out bool moved));
        if (moved)
        {
            Vector3 lift = Vector3.up * (0.5f * scale);
            if (SegmentBlocked(playerFloor + lift, placed + lift)) placed = playerFloor;
            else placed.y = FindFloor(placed + lift, playerFloor.y).y;
        }

        DialogueShotSettings s = Settings();
        Pose npcPose = default, playerPose = default, twoPose = default;
        bool npcOts = false, playerOts = false, two = false;
        // Props and walls on the approach side can block every shot. Try a few
        // spots around the NPC before falling back to the first-person look;
        // the first candidate is the original placement.
        Vector3 fromNpc = placed - npc.Floor;
        fromNpc.y = 0f;
        Vector3 bodyLift = Vector3.up * (0.5f * scale);
        for (int i = 0; i < PlacementYawOffsets.Length; i++)
        {
            Vector3 candidate = placed;
            if (i > 0)
            {
                if (fromNpc.sqrMagnitude < 1e-4f) break;
                candidate = npc.Floor + Quaternion.Euler(0f, PlacementYawOffsets[i], 0f) * fromNpc;
                if (SegmentBlocked(npc.Floor + bodyLift, candidate + bodyLift) || IsOccupied(candidate + bodyLift)) continue;
                candidate.y = FindFloor(candidate + bodyLift, placed.y).y;
            }

            float yaw = DialogueShotMath.YawDegrees(ToN(npc.Floor - candidate));
            if (!playerVisual.Place(candidate, yaw)) return false;
            elijahHead = candidate + Vector3.up * playerVisual.HeadHeight;
            npcOts = TryOverShoulder(true, s, out npcPose);
            playerOts = TryOverShoulder(false, s, out playerPose);
            two = TryTwoShot(s, out twoPose);
            if (npcOts || playerOts || two) break;
        }
        if (!npcOts && !playerOts && !two && PlacementYawOffsets.Length > 1)
        {
            // Nothing cinematic anywhere: keep the original placement.
            float yaw = DialogueShotMath.YawDegrees(ToN(npc.Floor - placed));
            if (!playerVisual.Place(placed, yaw)) return false;
            elijahHead = placed + Vector3.up * playerVisual.HeadHeight;
        }
        Pose firstPersonLook = LookPose(fpPosition, npc.Head);

        hasNpcCloseUp = TryCloseUp(true, s, out npcCloseUpPose);
        hasPlayerCloseUp = TryCloseUp(false, s, out playerCloseUpPose);

        npcLinePose = npcOts ? npcPose : two ? twoPose : firstPersonLook;
        playerLinePose = playerOts ? playerPose : two ? twoPose : npcLinePose;
        LastShotDescription = $"npc:{(npcOts ? "ots" : two ? "two-shot" : "first-person")} " +
                              $"player:{(playerOts ? "ots" : two ? "two-shot" : "npc-shot")}";
        return true;
    }

    private DialogueShotSettings Settings() => new DialogueShotSettings
    {
        BackDistance = backDistance,
        ShoulderOffset = shoulderOffset,
        HeightOffset = heightOffset,
        FramingBias = framingBias,
        TwoShotDistance = twoShotDistance,
        CloseUpDistance = closeUpDistance,
        CloseUpSideOffset = closeUpSideOffset,
        CloseUpHeight = 0f
    };

    private bool TryOverShoulder(bool npcLine, DialogueShotSettings s, out Pose pose)
    {
        Vector3 subject = npcLine ? npc.Head : elijahHead;
        Vector3 shoulder = npcLine ? elijahHead : npc.Head;
        foreach (float back in BackScales)
        {
            DialogueShotPose shot = npcLine
                ? DialogueShotMath.NpcLineShot(ToN(elijahHead), ToN(npc.Head), scale, s, back)
                : DialogueShotMath.PlayerLineShot(ToN(elijahHead), ToN(npc.Head), scale, s, back);
            Vector3 position = ToU(shot.Position);
            if (!IsClear(position, subject, shoulder)) continue;
            pose = LookPose(position, ToU(shot.LookAt));
            return true;
        }
        pose = default;
        return false;
    }

    // A close-up is only used when the listener is genuinely out of frame
    // (behind the camera) and nothing blocks the speaker's face.
    private bool TryCloseUp(bool npcLine, DialogueShotSettings s, out Pose pose)
    {
        s.CloseUpDistance = Mathf.Max(s.CloseUpDistance, MinCloseUpDistance);
        DialogueShotPose shot = npcLine
            ? DialogueShotMath.NpcCloseUp(ToN(elijahHead), ToN(npc.Head), scale, s)
            : DialogueShotMath.PlayerCloseUp(ToN(elijahHead), ToN(npc.Head), scale, s);
        Vector3 subject = npcLine ? npc.Head : elijahHead;
        Vector3 listener = npcLine ? elijahHead : npc.Head;
        Vector3 position = ToU(shot.Position);
        // Never inside or right against the speaker's head.
        Vector3 fromHead = position - subject;
        float minDistance = MinCameraToHead * scale;
        if (fromHead.sqrMagnitude < minDistance * minDistance)
            position = subject + (fromHead.sqrMagnitude > 1e-6f ? fromHead.normalized : Vector3.back) * minDistance;
        if (!DialogueShotMath.IsBehindCamera(shot, ToN(listener), 0.08f * scale) ||
            !IsClear(position, subject, subject))
        {
            pose = default;
            return false;
        }
        pose = LookPose(position, ToU(shot.LookAt));
        return true;
    }

    private bool TryTwoShot(DialogueShotSettings s, out Pose pose)
    {
        // Same side as the over-the-shoulder shots first, then the other side.
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? 1f : -1f;
            DialogueShotPose shot = DialogueShotMath.TwoShot(ToN(elijahHead), ToN(npc.Head), scale, s, side);
            Vector3 position = ToU(shot.Position);
            if (!IsClear(position, npc.Head, elijahHead) || SegmentBlocked(position, elijahHead)) continue;
            pose = LookPose(position, ToU(shot.LookAt));
            return true;
        }
        pose = default;
        return false;
    }

    private static Pose LookPose(Vector3 position, Vector3 lookAt)
    {
        Vector3 forward = lookAt - position;
        Quaternion rotation = forward.sqrMagnitude > 1e-8f
            ? Quaternion.LookRotation(forward, Vector3.up)
            : Quaternion.identity;
        return new Pose(position, rotation);
    }

    private Vector3 FindFloor(Vector3 origin, float fallbackY)
    {
        float reach = 3f * Mathf.Max(scale, 0.5f);
        Vector3 start = origin + Vector3.up * (0.1f * Mathf.Max(scale, 0.5f));
        int count = Physics.RaycastNonAlloc(start, Vector3.down, hits, reach, obstructionMask, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Vector3 floor = new Vector3(origin.x, fallbackY, origin.z);
        for (int i = 0; i < count; i++)
        {
            if (IsIgnored(hits[i].collider)) continue;
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                floor = hits[i].point;
            }
        }
        return floor;
    }

    private bool IsClear(Vector3 cameraPosition, Vector3 subject, Vector3 shoulder)
    {
        float radius = clearanceRadius * scale;
        int count = Physics.OverlapSphereNonAlloc(cameraPosition, radius, overlaps, obstructionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (!IsIgnored(overlaps[i])) return false;
        // The camera must not sit behind a wall relative to the foreground shoulder,
        // and must see the speaking face.
        return !SegmentBlocked(shoulder, cameraPosition) && !SegmentBlocked(cameraPosition, subject);
    }

    private static readonly float[] PlacementYawOffsets = { 0f, 40f, -40f, 80f, -80f, 120f, -120f };

    /// <summary>True when a standing Elijah at this body point would intersect scene geometry.</summary>
    private bool IsOccupied(Vector3 bodyPoint)
    {
        int count = Physics.OverlapSphereNonAlloc(bodyPoint, 0.3f * scale, overlaps, obstructionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (!IsIgnored(overlaps[i])) return true;
        return false;
    }

    private bool SegmentBlocked(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 1e-3f) return false;
        int count = Physics.RaycastNonAlloc(from, delta / distance, hits, distance, obstructionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (!IsIgnored(hits[i].collider)) return true;
        return false;
    }

    private bool IsIgnored(Collider c)
    {
        if (c == null) return true;
        Transform t = c.transform;
        return (npc.Root != null && t.IsChildOf(npc.Root)) ||
               (playerRoot != null && t.IsChildOf(playerRoot)) ||
               (playerVisual != null && t.IsChildOf(playerVisual.transform));
    }

    private static NVec ToN(Vector3 v) => new NVec(v.x, v.y, v.z);
    private static Vector3 ToU(NVec v) => new Vector3(v.X, v.Y, v.Z);
}
