using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Milestone T02 pilot actor: Arthur (any of his model variants) on a
/// derived copy of the shared NpcHumanoid controller with the IK pass on.
/// Isolated review only: no campaign, save, inventory or reward code.
///
/// - Body: walks in through the doorway on the walk clip at the clip's own
///   ground speed (feet match the floor), turns along the path, stops into
///   the standing idle at one station, walks out again; loops without reset.
/// - Hand: the rig's right hand is an index-only mitten (no thumb bones), so
///   the honest grasp is a hook grip: palm on the folder's cover, mitten curled
///   over its top edge. The hand frame is calibrated from the actual bones:
///   IK goal offset, finger direction, and the palm side found by curling the
///   real finger muscles. Point, reach, approach, grip (curl), lift, carry,
///   lower, release (uncurl), back off, retract.
/// - Folder: attached to the hand bone only from the end of Grip to the start
///   of Release, keeping its world pose at both moments (no snap); after
///   release it settles onto its holder rest, and the residual is reported.
///
/// The fixture calls Tick once per frame with pilot time; every bay runs the
/// same beats on the same frame.
/// </summary>
public sealed class T02PilotArthur : MonoBehaviour
{
    [Header("Hand frame (fractions of the measured hand length)")]
    public float palmSurface = 0.12f;
    public float gripAlongFinger = 0.42f;
    [Header("Hook grip (Humanoid finger muscles, -1 curled .. 1 stretched)")]
    public float curl1 = -0.35f;
    public float curl2 = -0.9f;
    public float curl3 = -0.9f;
    [Header("Motion")]
    public float maxTurnDegrees = 150f;
    public float settleTurnDegrees = 90f;
    public float liftClearance = 0.06f;

    private const string IdleState = "Standing Idle";
    private const string WalkState = "Walking";

    // ---- what the fixture and its readout see ----
    public bool Calibrated { get; private set; }
    public string Problem { get; private set; } = "";
    public T02PilotBeat Beat => steps != null && steps.Count > 0 ? steps[index].Beat : T02PilotBeat.Idle;
    public float BeatProgress => steps != null && steps.Count > 0 && steps[index].Seconds > 0f ? Mathf.Clamp01(time / steps[index].Seconds) : 0f;
    public bool HoldsFolder { get; private set; }
    public bool AudioActive { get; set; }
    public float ArmLength { get; private set; }
    public float HandLength { get; private set; }
    public float GripErrorMm { get; private set; } = -1f;
    public float PlaceErrorMm { get; private set; } = -1f;
    public float PlaceAngleDegrees { get; private set; } = -1f;
    public float ReachRatio { get; private set; }
    public float WalkSpeed { get; private set; }
    public float FootGroundOffsetMm { get; private set; }
    public Vector3 DesiredPalm { get; private set; }
    public Vector3 ActualPalm => hand != null ? hand.TransformPoint(gripLocal) : Vector3.zero;
    public Animator Animator => animator;

    private Animator animator;
    private Transform model, bay, folder;
    private Vector3 folderGripLocal;
    private Transform folderShape;
    private float folderHalfHeight = 0.2f;
    private readonly Pose[] rests = new Pose[2];
    private List<Vector3> enterPath, exitPath;
    private Quaternion stationFacing;
    private float holderTopY;

    private List<T02PilotStep> steps;
    private int index;
    private float time;

    private List<Vector3> path;
    private int waypoint;
    private float speed;
    private bool arriving;
    private bool walkDone;
    private float lastWalkPhase = -1f;

    private float ikWeight, fromWeight, targetWeight;
    private Vector3 handPos, fromPos, targetPos;
    private Quaternion handRot = Quaternion.identity, fromRot = Quaternion.identity, targetRot = Quaternion.identity;
    private float lookWeight, fromLook, targetLook;
    private Vector3 lookTarget;
    private float curl, fromCurl, targetCurl;
    private Vector3 activePalmNormal = Vector3.forward;

    private Transform hand, upperArm, lowerArm, tip;
    private Vector3 fingerLocal = Vector3.up, palmLocal = Vector3.forward, gripLocal;
    private Quaternion goalOffsetRot = Quaternion.identity;
    private Vector3 goalOffsetPos;
    private bool goalSampled;
    private Vector3 sampledGoalPos;
    private Quaternion sampledGoalRot;
    private int calibrationFrames;
    private float fittedHeight;

    private HumanPoseHandler poseHandler;
    private HumanPose pose;
    private int muscle1 = -1, muscle2 = -1, muscle3 = -1;
    private Transform[] poseTransforms;
    private Vector3[] posePositions, poseScales;
    private Quaternion[] poseRotations, fingerRotations;
    private Transform[] fingerBones;

    private Vector3 folderInHandPos;
    private Quaternion folderInHandRot = Quaternion.identity;
    private float settle = -1f;
    private Pose settleFrom, settleTo;

    // =========================================================
    // SETUP
    // =========================================================

    /// <summary>Binds the model (a child of this root), the pilot controller, the folder, its two holder rests and the paths.</summary>
    public void Init(Transform modelRoot, RuntimeAnimatorController controller, Transform bayRoot, Transform folderRoot, Pose restA, Pose restB,
        float holderTop, List<Vector3> enter, List<Vector3> exit, Quaternion facing)
    {
        model = modelRoot;
        bay = bayRoot;
        folder = folderRoot;
        rests[0] = restA;
        rests[1] = restB;
        holderTopY = holderTop;
        enterPath = enter;
        exitPath = exit;
        stationFacing = facing;
        animator = model != null ? model.GetComponentInChildren<Animator>(true) : null;
        Problem = CharacterBodyGeometry.Problem(model, animator);
        if (!string.IsNullOrEmpty(Problem)) return;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.enabled = true;
        animator.Rebind();
        // Unity sends OnAnimatorIK to components on the Animator object, not its parent.
        animator.gameObject.AddComponent<T02PilotAnimatorIK>().Actor = this;
        foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            skin.updateWhenOffscreen = true;
        hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        upperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        lowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        Transform distal = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal) ?? animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate) ??
                           animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        tip = distal != null && distal.childCount > 0 ? distal.GetChild(0) : distal;
        if (hand == null || upperArm == null || lowerArm == null) { Problem = "right arm bones are not mapped"; return; }
        if (tip == null) Problem = "no right index finger bones are mapped: no grip curl possible";
        muscle1 = Array.IndexOf(HumanTrait.MuscleName, "Right Index 1 Stretched");
        muscle2 = Array.IndexOf(HumanTrait.MuscleName, "Right Index 2 Stretched");
        muscle3 = Array.IndexOf(HumanTrait.MuscleName, "Right Index 3 Stretched");
        MeshFilter shape = folder != null ? folder.GetComponentInChildren<MeshFilter>(true) : null;
        folderShape = shape != null ? shape.transform : folder;
        Renderer body = folder != null ? folder.GetComponentInChildren<Renderer>(true) : null;
        if (body != null) folderHalfHeight = body.bounds.extents.y; // the folder already stands upright in holder A
        if (shape != null && shape.sharedMesh != null)
        {
            // Palm contact: the cover side (+y), just below the top edge (+x), across the middle (z).
            Bounds b = shape.sharedMesh.bounds;
            Vector3 local = new Vector3(b.max.x - b.size.x * 0.11f, b.max.y, b.center.z);
            folderGripLocal = folder.InverseTransformPoint(shape.transform.TransformPoint(local));
        }
        steps = T02PilotPlan.Build();
        index = 0;
        time = 0f;
        animator.Play(IdleState, 0, 0f);
    }

    /// <summary>Fit the visual parent, rebind at its final scale, then initialize hand-pose state.</summary>
    public void FitStandingHeight(float height)
    {
        if (animator == null || !string.IsNullOrEmpty(Problem)) return;
        fittedHeight = height;
        // Each variant is measured in its own posed space. Never reuse another bay's scale.
        for (int pass = 0; pass < 3; pass++)
        {
            Bounds measured = MeasureBody();
            if (measured.size.y <= 0.01f) return;
            transform.localScale = transform.localScale * (height / measured.size.y);
            animator.Rebind();
            animator.Play(IdleState, 0, 0f);
            animator.Update(0f);
        }
        Bounds fitted = MeasureBody();
        model.position += new Vector3(transform.position.x - fitted.center.x, transform.position.y - fitted.min.y, transform.position.z - fitted.center.z);
        RebuildPoseHandler();
    }

    private void RebuildPoseHandler()
    {
        poseHandler?.Dispose();
        poseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
        pose = new HumanPose();
        poseTransforms = model.GetComponentsInChildren<Transform>(true);
        posePositions = new Vector3[poseTransforms.Length];
        poseScales = new Vector3[poseTransforms.Length];
        poseRotations = new Quaternion[poseTransforms.Length];
        fingerBones = new[] {
            animator.GetBoneTransform(HumanBodyBones.RightIndexProximal),
            animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate),
            animator.GetBoneTransform(HumanBodyBones.RightIndexDistal) };
        fingerRotations = new Quaternion[fingerBones.Length];
    }

    /// <summary>World bounds of the skinned body in its current pose (setup only: bakes once).</summary>
    public Bounds MeasureBody()
    {
        if (!CharacterBodyGeometry.TryMeasure(model, out Bounds bounds)) Problem = "Arthur has no measurable imported geometry";
        return bounds;
    }

    private void OnDestroy()
    {
        poseHandler?.Dispose();
        poseHandler = null;
    }

    private void OnDisable()
    {
        // Never leave the folder riding a disabled hand: it goes back to its bay where it is.
        if (HoldsFolder && folder != null && bay != null) folder.SetParent(bay, true);
        HoldsFolder = false;
    }

    // =========================================================
    // PER FRAME (pilot time from the fixture)
    // =========================================================

    public void Tick(float dt)
    {
        if (steps == null || animator == null || !string.IsNullOrEmpty(Problem) && hand == null) return;
        if (!Calibrated) return;
        T02PilotStep step = steps[index];
        time += dt;
        switch (step.Beat)
        {
            case T02PilotBeat.WalkIn:
            case T02PilotBeat.WalkOut:
                WalkTick(dt);
                if (walkDone) Advance();
                return;
            case T02PilotBeat.Settle:
                TurnToward(stationFacing, settleTurnDegrees * dt);
                break;
        }
        float k = T02PilotPlan.Ease(step.Seconds > 0f ? time / step.Seconds : 1f);
        if (T02PilotPlan.ArmActive(step.Beat))
        {
            handPos = Vector3.Lerp(fromPos, targetPos, k);
            handRot = Quaternion.Slerp(fromRot, targetRot, k);
            ikWeight = Mathf.Lerp(fromWeight, targetWeight, k);
            lookWeight = Mathf.Lerp(fromLook, targetLook, k);
            curl = Mathf.Lerp(fromCurl, targetCurl, k);
            lookTarget = folder != null ? folder.position : lookTarget;
        }
        if (time >= step.Seconds) Advance();
    }

    private void Advance()
    {
        T02PilotStep finished = steps[index];
        EndBeat(finished);
        index = T02PilotPlan.Next(index, steps.Count);
        time = 0f;
        BeginBeat(steps[index]);
    }

    private void BeginBeat(T02PilotStep step)
    {
        fromPos = handPos; fromRot = handRot; fromWeight = ikWeight; fromLook = lookWeight; fromCurl = curl;
        targetPos = handPos; targetRot = handRot; targetWeight = ikWeight; targetLook = lookWeight; targetCurl = curl;
        switch (step.Beat)
        {
            case T02PilotBeat.WalkIn: BeginWalk(enterPath); break;
            case T02PilotBeat.WalkOut: BeginWalk(exitPath); break;
            case T02PilotBeat.Point:
            {
                // The arm starts from where the clip has it (weight 0), so nothing jumps.
                fromPos = hand.position; fromRot = hand.rotation; fromWeight = 0f;
                Vector3 shoulder = upperArm.position;
                Vector3 toFolder = (FolderAt(step.From).position - shoulder).normalized;
                Vector3 palm = Vector3.ProjectOnPlane(Vector3.down, toFolder).normalized;
                Quaternion rot = HandRotation(toFolder, palm);
                Vector3 palmPoint = shoulder + toFolder * (ArmLength * 0.72f);
                Target(palmPoint, rot, 1f, 0.85f, 0f);
                break;
            }
            case T02PilotBeat.Reach:
            {
                Pose grip = GripPose(step.From, out Vector3 palmPoint, out Vector3 inward, out Vector3 finger);
                Target(palmPoint - inward * 0.09f - finger * 0.03f, grip.rotation, 1f, 0.85f, 0f);
                break;
            }
            case T02PilotBeat.Approach:
            {
                Pose grip = GripPose(step.From, out Vector3 palmPoint, out _, out _);
                Target(palmPoint, grip.rotation, 1f, 0.85f, 0f);
                break;
            }
            case T02PilotBeat.Grip: targetCurl = 1f; break;
            case T02PilotBeat.Lift:
            {
                targetPos = handPos + Vector3.up * LiftHeight(step.From);
                break;
            }
            case T02PilotBeat.Carry:
            {
                HandPoseForFolder(rests[step.To], out Vector3 pos, out Quaternion rot);
                targetPos = pos + Vector3.up * LiftHeight(step.To);
                targetRot = rot;
                break;
            }
            case T02PilotBeat.Lower:
            {
                HandPoseForFolder(rests[step.To], out Vector3 pos, out Quaternion rot);
                targetPos = pos;
                targetRot = rot;
                break;
            }
            case T02PilotBeat.Release:
                Detach(step.To);
                targetCurl = 0f;
                if (AudioActive) AudioManager.Instance?.PlayPaperSFX();
                break;
            case T02PilotBeat.Back:
                targetPos = handPos - activePalmNormal * 0.08f;
                break;
            case T02PilotBeat.Retract:
                targetWeight = 0f; targetLook = 0f;
                break;
        }
        if (step.Beat == T02PilotBeat.Approach || step.Beat == T02PilotBeat.Lower) ReachRatio = ArmLength > 0f ? Vector3.Distance(upperArm.position, targetPos) / ArmLength : 0f;
    }

    private void EndBeat(T02PilotStep step)
    {
        switch (step.Beat)
        {
            case T02PilotBeat.Approach:
                GripErrorMm = Vector3.Distance(ActualPalm, DesiredPalm) * 1000f;
                break;
            case T02PilotBeat.Grip:
                Attach();
                if (AudioActive) AudioManager.Instance?.PlayPaperSFX();
                break;
            case T02PilotBeat.Settle:
                transform.rotation = stationFacing;
                break;
        }
    }

    private void Target(Vector3 palmPoint, Quaternion rot, float weight, float look, float curlTo)
    {
        DesiredPalm = palmPoint;
        targetRot = rot;
        targetPos = palmPoint - rot * (gripLocal * HandScale);
        targetWeight = weight;
        targetLook = look;
        targetCurl = curlTo;
    }

    private float HandScale => hand != null ? hand.lossyScale.x : 1f;

    /// <summary>The hand bone rotation that points the mitten along finger with the palm facing palmWorld.</summary>
    private Quaternion HandRotation(Vector3 fingerWorld, Vector3 palmWorld)
    {
        Quaternion world = Quaternion.LookRotation(fingerWorld, -palmWorld);
        Quaternion local = Quaternion.LookRotation(fingerLocal, -palmLocal);
        return world * Quaternion.Inverse(local);
    }

    private Transform FolderAt(int rest) => folder;

    /// <summary>The hook grip on the folder as it stands now: palm flat on the cover below the top edge, mitten pointing up over it.</summary>
    private Pose GripPose(int rest, out Vector3 palmPoint, out Vector3 inward, out Vector3 finger)
    {
        palmPoint = folder.TransformPoint(folderGripLocal);
        inward = folderShape.rotation * Vector3.down;   // into the cover (the mesh's own axes)
        finger = folderShape.rotation * Vector3.right;  // toward the top edge (mesh +x stands up in a holder)
        activePalmNormal = inward;
        return new Pose(palmPoint, HandRotation(finger, inward));
    }

    /// <summary>Hand bone pose that puts the attached folder exactly at rest (inverse of the grip captured at attach).</summary>
    private void HandPoseForFolder(Pose rest, out Vector3 pos, out Quaternion rot)
    {
        rot = rest.rotation * Quaternion.Inverse(folderInHandRot);
        pos = rest.position - rot * (folderInHandPos * HandScale);
    }

    /// <summary>Raise the held folder until its bottom clears the holder rim (rest centre minus half height), plus a margin.</summary>
    private float LiftHeight(int rest) => Mathf.Max(0.12f, holderTopY - (RestCentreY(rest) - folderHalfHeight) + liftClearance);

    private float RestCentreY(int rest)
    {
        Renderer body = folder != null ? folder.GetComponentInChildren<Renderer>(true) : null;
        if (body == null) return rests[rest].position.y;
        // Centre offset of the upright folder relative to its pivot (same in both holders).
        Vector3 offset = body.bounds.center - folder.position;
        return rests[rest].position.y + offset.y;
    }

    private void Attach()
    {
        if (folder == null || hand == null) return;
        folderInHandPos = hand.InverseTransformPoint(folder.position);
        folderInHandRot = Quaternion.Inverse(hand.rotation) * folder.rotation;
        folder.SetParent(hand, true);
        HoldsFolder = true;
        settle = -1f;
    }

    private void Detach(int rest)
    {
        if (!HoldsFolder || folder == null) return;
        folder.SetParent(bay, true);
        HoldsFolder = false;
        settleFrom = new Pose(folder.position, folder.rotation);
        settleTo = rests[rest];
        PlaceErrorMm = Vector3.Distance(settleFrom.position, settleTo.position) * 1000f;
        PlaceAngleDegrees = Quaternion.Angle(settleFrom.rotation, settleTo.rotation);
        settle = 0f;
    }

    // =========================================================
    // WALKING
    // =========================================================

    private void BeginWalk(List<Vector3> waypoints)
    {
        path = waypoints;
        waypoint = 0;
        arriving = false;
        walkDone = path == null || path.Count == 0;
        ikWeight = 0f; lookWeight = 0f;
        if (!walkDone) animator.CrossFadeInFixedTime(WalkState, 0.25f, 0);
    }

    private void WalkTick(float dt)
    {
        if (walkDone) return;
        Vector3 target = path[waypoint];
        Vector3 to = target - transform.position;
        to.y = 0f;
        float distance = to.magnitude;
        bool last = waypoint == path.Count - 1;
        if (!last && distance < 0.3f) { waypoint++; return; }
        float clip = ClipSpeed();
        float desired = clip;
        if (last)
        {
            if (!arriving && distance < 0.45f) { arriving = true; animator.CrossFadeInFixedTime(IdleState, 0.4f, 0); }
            if (arriving) desired = clip * Mathf.Clamp01(distance / 0.45f);
        }
        speed = Mathf.MoveTowards(speed, desired, 2.5f * dt);
        if (distance > 0.001f) TurnToward(Quaternion.LookRotation(to / distance, Vector3.up), maxTurnDegrees * dt);
        Vector3 step = transform.forward * speed * dt;
        if (last && step.magnitude >= distance) { transform.position = new Vector3(target.x, transform.position.y, target.z); walkDone = true; speed = 0f; return; }
        transform.position += step;
        if (last && distance < 0.015f) { walkDone = true; speed = 0f; }
        Footsteps();
    }

    private void TurnToward(Quaternion facing, float maxDegrees) =>
        transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, maxDegrees);

    private float clipSpeed;
    /// <summary>The walk clip's own ground speed at this size (NpcAnimationDriver's measure), so feet do not slide.</summary>
    private float ClipSpeed()
    {
        if (clipSpeed > 0f) return clipSpeed;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName(WalkState))
        {
            var infos = new List<AnimatorClipInfo>();
            animator.GetCurrentAnimatorClipInfo(0, infos);
            if (infos.Count > 0 && infos[0].clip != null)
            {
                Vector3 v = infos[0].clip.averageSpeed;
                v.y = 0f;
                clipSpeed = v.magnitude * animator.humanScale * animator.transform.lossyScale.y * Mathf.Abs(state.speed * state.speedMultiplier);
            }
        }
        WalkSpeed = clipSpeed > 0f ? clipSpeed : 1f;
        return clipSpeed > 0f ? clipSpeed : 1f;
    }

    private void Footsteps()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName(WalkState)) { lastWalkPhase = -1f; return; }
        float phase = state.normalizedTime % 1f;
        if (lastWalkPhase >= 0f && AudioActive && (phase < lastWalkPhase || lastWalkPhase < 0.5f && phase >= 0.5f))
            AudioManager.Instance?.PlayFootstepSFX();
        lastWalkPhase = phase;
    }

    // =========================================================
    // ANIMATOR IK AND FINGERS
    // =========================================================

    public void ApplyAnimatorIK(int layer)
    {
        if (animator == null || hand == null) return;
        if (!Calibrated)
        {
            sampledGoalPos = animator.GetIKPosition(AvatarIKGoal.RightHand);
            sampledGoalRot = animator.GetIKRotation(AvatarIKGoal.RightHand);
            goalSampled = true;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            return;
        }
        // Bone pose -> IK goal (the avatar's goal frame differs from the bone by a fixed offset, measured at calibration).
        Quaternion goalRot = handRot * Quaternion.Inverse(goalOffsetRot);
        Vector3 goalPos = handPos - goalRot * goalOffsetPos;
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
        animator.SetIKPosition(AvatarIKGoal.RightHand, goalPos);
        animator.SetIKRotation(AvatarIKGoal.RightHand, goalRot);
        animator.SetLookAtWeight(lookWeight, 0.15f, 0.8f, 0f, 0.5f);
        animator.SetLookAtPosition(lookTarget);
    }

    private void LateUpdate()
    {
        if (animator == null || hand == null) return;
        if (!Calibrated) { Calibrate(); return; }
        ApplyCurl();
        if (settle >= 0f && folder != null && !HoldsFolder)
        {
            settle += Time.deltaTime * Mathf.Max(0f, animator.speed);
            float k = T02PilotPlan.Ease(settle / 0.15f);
            folder.SetPositionAndRotation(Vector3.Lerp(settleFrom.position, settleTo.position, k), Quaternion.Slerp(settleFrom.rotation, settleTo.rotation, k));
            if (settle >= 0.15f) settle = -1f;
        }
    }

    /// <summary>Third idle frame: IK goal offset, facing, foot contact, arm/hand lengths and the palm side from a real finger curl.</summary>
    private void Calibrate()
    {
        if (!goalSampled || ++calibrationFrames < 3) return;
        // The model's own facing (humanoid body forward) becomes this root's forward.
        Vector3 body = animator.bodyRotation * Vector3.forward;
        body.y = 0f;
        float facing = body.sqrMagnitude > 0.001f ? Vector3.SignedAngle(transform.forward, body, Vector3.up) : 0f;
        if (Mathf.Abs(facing) > 1f)
        {
            model.rotation = Quaternion.AngleAxis(-facing, Vector3.up) * model.rotation;
            animator.Update(0f);
            RebuildPoseHandler();
        }
        goalOffsetRot = Quaternion.Inverse(sampledGoalRot) * hand.rotation;
        goalOffsetPos = Quaternion.Inverse(sampledGoalRot) * (hand.position - sampledGoalPos);

        ArmLength = Vector3.Distance(upperArm.position, lowerArm.position) + Vector3.Distance(lowerArm.position, hand.position);
        if (tip != null && poseHandler != null && muscle1 >= 0 && muscle2 >= 0 && muscle3 >= 0)
        {
            ReadLocalPose();
            float[] saved = (float[])pose.muscles.Clone();
            SetFingers(1f, 1f, 1f);
            Vector3 open = hand.InverseTransformPoint(tip.position);
            SetFingers(-1f, -1f, -1f);
            Vector3 closed = hand.InverseTransformPoint(tip.position);
            Array.Copy(saved, pose.muscles, saved.Length);
            WriteFingerPose();
            fingerLocal = open.normalized;
            Vector3 side = Vector3.ProjectOnPlane(closed - open, fingerLocal);
            if (side.sqrMagnitude < 1e-8f) Problem = "finger curl does not move the fingertip: palm side unknown";
            else palmLocal = side.normalized;
            HandLength = open.magnitude * HandScale;
            gripLocal = fingerLocal * (open.magnitude * gripAlongFinger) + palmLocal * (open.magnitude * palmSurface);
        }
        else if (string.IsNullOrEmpty(Problem)) Problem = "finger muscles unavailable: no grip curl possible";
        float foot = float.PositiveInfinity;
        foreach (HumanBodyBones b in new[] { HumanBodyBones.LeftToes, HumanBodyBones.RightToes, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
        {
            Transform t = animator.GetBoneTransform(b);
            if (t != null) foot = Mathf.Min(foot, t.position.y);
        }
        FootGroundOffsetMm = float.IsInfinity(foot) ? 0f : (foot - transform.position.y) * 1000f;
        handPos = hand.position;
        handRot = hand.rotation;
        Calibrated = true;
        Bounds posed = MeasureBody();
        if (!CharacterBodyGeometry.MatchesFit(posed, fittedHeight, transform.position))
        {
            Problem = $"post-animation body fit invalid: height {posed.size.y:0.000} m (wanted {fittedHeight:0.000}), floor offset {posed.min.y - transform.position.y:0.000} m";
            Debug.LogError("[T02 pilot] " + Problem, this);
        }
        BeginBeat(steps[index]);
    }

    private void SetFingers(float a, float b, float c)
    {
        pose.muscles[muscle1] = a;
        pose.muscles[muscle2] = b;
        pose.muscles[muscle3] = c;
        WriteFingerPose();
    }

    private void ReadLocalPose()
    {
        poseHandler.GetHumanPose(ref pose);
        pose.bodyPosition = T02PilotPose.LocalBodyPosition(pose.bodyPosition, animator.transform.position, animator.transform.rotation, animator.transform.lossyScale, animator.humanScale);
        pose.bodyRotation = T02PilotPose.LocalBodyRotation(pose.bodyRotation, animator.transform.rotation);
    }

    private void WriteFingerPose()
    {
        // HumanPose rewrites the entire skeleton, including imported nonuniform scales.
        // Retain ONLY the requested finger rotations; restore every other transform exactly.
        for (int i = 0; i < poseTransforms.Length; i++)
        {
            posePositions[i] = poseTransforms[i].localPosition;
            poseRotations[i] = poseTransforms[i].localRotation;
            poseScales[i] = poseTransforms[i].localScale;
        }
        poseHandler.SetHumanPose(ref pose);
        for (int i = 0; i < fingerBones.Length; i++)
            if (fingerBones[i] != null) fingerRotations[i] = fingerBones[i].localRotation;
        for (int i = 0; i < poseTransforms.Length; i++)
        {
            poseTransforms[i].localPosition = posePositions[i];
            poseTransforms[i].localRotation = poseRotations[i];
            poseTransforms[i].localScale = poseScales[i];
        }
        for (int i = 0; i < fingerBones.Length; i++)
            if (fingerBones[i] != null) fingerBones[i].localRotation = fingerRotations[i];
    }

    /// <summary>After the animator and IK: the mitten curls by the grip amount (the clip's own finger pose when open).</summary>
    private void ApplyCurl()
    {
        if (curl <= 0.001f || poseHandler == null || muscle1 < 0 || muscle2 < 0 || muscle3 < 0) return;
        ReadLocalPose();
        pose.muscles[muscle1] = Mathf.Lerp(pose.muscles[muscle1], curl1, curl);
        pose.muscles[muscle2] = Mathf.Lerp(pose.muscles[muscle2], curl2, curl);
        pose.muscles[muscle3] = Mathf.Lerp(pose.muscles[muscle3], curl3, curl);
        WriteFingerPose();
    }
}

// Runtime-only relay; the fixture creates it on each model's Animator object.
public sealed class T02PilotAnimatorIK : MonoBehaviour
{
    public T02PilotArthur Actor;
    private void OnAnimatorIK(int layer) { if (Actor != null) Actor.ApplyAnimatorIK(layer); }
}
