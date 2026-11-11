using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// PURPOSE:
/// Milestone T03: Elijah's one visible body during outdoor third-person
/// exploration (Office_Level1). Visual only: it follows the gameplay player,
/// never changes its Rigidbody, capsule or scale, and has no colliders.
///
/// - Scale: one canonical world height (ExplorationRules.ElijahRestHeight,
///   calibrated against the adult plaza cast) applied ONCE to the visual
///   parent from rest-pose geometry, so animation can never feed back into
///   scale. Soles are grounded from the foot bones on the capsule bottom.
/// - Motion: walk and run share one distance-driven phase (sync group, heel
///   contacts aligned), so feet match the ground and blends never jump. The
///   jump is an explicit windup/rise/fall/land track driven by physics events.
///   Footsteps play on real heel contacts and landings, never in the air.
/// - Visibility: shown only in the third-person view and hidden while any
///   owner (a conversation's own Elijah, a task view, a camera pressed close)
///   hides it; the first-person view never shows it.
/// - Evaluated only while shown; no per-frame searches.
/// </summary>
[DisallowMultipleComponent]
public sealed class ExplorationBody : MonoBehaviour
{
    public const string AssetsPath = "Player/ExplorationBody";

    public static ExplorationBody Current { get; private set; }

    private PlayerMovement movement;
    private CapsuleCollider capsule;
    private Transform model;
    private Animator animator;
    private Transform leftFoot, rightFoot;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable idlePlayable, walkPlayable, runPlayable, jumpPlayable;
    private AnimationClip idleClip, walkClip, runClip, jumpClip;
    private Vector3 fittedPosition, fittedScale, previousPosition;
    private Quaternion fittedRotation;
    private float worldScale = 1f, facingYaw, previousYaw;
    private float walkStride = 1.35f, runStride = 2.3f, posedReference;
    private float phase, idleTime, speed, stepWeight;
    private ExplorationRules.JumpTrack jump;
    private int poseCheckFrames;
    private bool perspectiveShown;
    private readonly HashSet<object> hiders = new HashSet<object>();

    public bool IsShown => model != null && model.gameObject.activeSelf;
    public bool HasModel => model != null;
    /// <summary>Elijah's left hand (carried paper in third person), or null.</summary>
    public Transform LeftHand => animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.LeftHand) : null;
    /// <summary>The canonical full standing height in world metres (same for every Elijah presentation).</summary>
    public float StandingHeight => ExplorationRules.ElijahRestHeight;
    /// <summary>Where the soles stand: the gameplay capsule's bottom.</summary>
    public Vector3 Feet => FeetPosition();
    public Vector3 FocusPoint => FeetPosition() + Vector3.up * (StandingHeight * ExplorationRules.CameraPivotFraction);
    public float WorldScale => worldScale;
    public ExplorationRules.JumpPhase JumpPhase => jump.Phase;
    public string Problem { get; private set; } = "";

    /// <summary>The body for this player (built once from Resources/Player/ExplorationBody).</summary>
    public static ExplorationBody Ensure(PlayerMovement player)
    {
        if (player == null) return null;
        ExplorationBody body = player.GetComponentInChildren<ExplorationBody>(true);
        if (body == null)
        {
            Transform visual = player.transform.Find("Visual");
            var host = new GameObject("ExplorationBody");
            host.transform.SetParent(visual != null ? visual : player.transform, false);
            body = host.AddComponent<ExplorationBody>();
        }
        body.Build(player);
        return body;
    }

    private void OnEnable() { Current = this; Apply(); }

    private void OnDisable()
    {
        if (Current == this) Current = null;
        if (model != null) model.gameObject.SetActive(false);
        if (movement != null) movement.BodyDrivesFootsteps = false;
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }

    private void Build(PlayerMovement player)
    {
        movement = player;
        capsule = player.GetComponent<CapsuleCollider>();
        if (model != null) return;
        facingYaw = previousYaw = player.transform.eulerAngles.y;
        var assets = Resources.Load<ExplorationBodyAssets>(AssetsPath);
        if (assets == null || assets.model == null) { Problem = "Resources/" + AssetsPath + " or its model is missing"; return; }
        idleClip = assets.idle;
        walkClip = assets.walk;
        runClip = assets.sprint;
        jumpClip = assets.jump;
        GameObject instance = Instantiate(assets.model, transform, false);
        instance.name = "ElijahExplorationModel";
        model = instance.transform;
        foreach (Collider c in instance.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (Rigidbody r in instance.GetComponentsInChildren<Rigidbody>(true)) Destroy(r);
        foreach (SkinnedMeshRenderer s in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)) s.updateWhenOffscreen = true;
        animator = instance.GetComponentInChildren<Animator>(true);
        Problem = CharacterBodyGeometry.Problem(model, animator);
        if (!string.IsNullOrEmpty(Problem) || idleClip == null || walkClip == null || runClip == null || jumpClip == null)
        {
            if (string.IsNullOrEmpty(Problem)) Problem = "Elijah idle/walk/run/jump clip is missing";
            instance.SetActive(false);
            return;
        }
        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.enabled = true;
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        // 1) Unit world scale, rest pose: measure the authored geometry (not a BakeMesh of a pose).
        worldScale = 1f;
        Follow();
        if (!CharacterBodyGeometry.TryMeasureRest(model, out Bounds rest) || leftFoot == null || rightFoot == null)
        {
            Problem = "Elijah has no measurable rest geometry or foot bones";
            instance.SetActive(false);
            return;
        }
        // Ankle height above the sole in the rest pose (feet flat), at unit scale.
        float ankle = Mathf.Min(leftFoot.position.y, rightFoot.position.y) - rest.min.y;

        // 2) Apply the canonical height exactly once, on the visual parent; rebind at the final scale.
        worldScale = ExplorationRules.CalibratedScale(rest.size.y);
        Follow();
        animator.Rebind();

        graph = PlayableGraph.Create("ElijahExplorationBody");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        mixer = AnimationMixerPlayable.Create(graph, 4);
        idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        walkPlayable = AnimationClipPlayable.Create(graph, walkClip);
        runPlayable = AnimationClipPlayable.Create(graph, runClip);
        jumpPlayable = AnimationClipPlayable.Create(graph, jumpClip);
        // Explicit clocks own every clip: native advancement stays off.
        idlePlayable.SetSpeed(0d);
        walkPlayable.SetSpeed(0d);
        runPlayable.SetSpeed(0d);
        jumpPlayable.SetSpeed(0d);
        idlePlayable.SetApplyFootIK(true);
        walkPlayable.SetApplyFootIK(true);
        runPlayable.SetApplyFootIK(false);
        jumpPlayable.SetApplyFootIK(false);
        graph.Connect(idlePlayable, 0, mixer, 0);
        graph.Connect(walkPlayable, 0, mixer, 1);
        graph.Connect(runPlayable, 0, mixer, 2);
        graph.Connect(jumpPlayable, 0, mixer, 3);
        mixer.SetInputWeight(0, 1f);
        AnimationPlayableOutput.Create(graph, "Body", animator).SetSourcePlayable(mixer);
        graph.Play();
        graph.Evaluate(0f);

        // 3) Ground the posed soles (foot bones minus the scaled ankle height) on the capsule bottom.
        float sole = Mathf.Min(leftFoot.position.y, rightFoot.position.y) - ankle * worldScale;
        model.position += Vector3.up * (FeetPosition().y - sole);
        fittedPosition = model.localPosition;
        fittedRotation = model.localRotation;
        fittedScale = model.localScale;

        // 4) Strides at this scale. The walk's own root travel (Elijah's clip: 0.857 standing heights
        //    per cycle); the authored run cycle covers RunStrideHeights. Feet then match the ground.
        float expectedWalk = 0.857f * ExplorationRules.ElijahRestHeight;
        Vector3 v = walkClip.averageSpeed;
        v.y = 0f;
        float measured = v.magnitude * walkClip.length * animator.humanScale * model.lossyScale.y;
        walkStride = measured > expectedWalk * 0.75f && measured < expectedWalk * 1.25f ? measured : expectedWalk;
        runStride = ExplorationRules.RunStrideHeights * ExplorationRules.ElijahRestHeight;
        previousPosition = movement.transform.position;
        posedReference = CharacterBodyGeometry.TryMeasure(model, out Bounds posed) ? posed.size.y : 0f;
        Debug.Log($"[ExplorationBody] Bound {instance.name}: rest {rest.size.y:0.000} -> {ExplorationRules.ElijahRestHeight:0.000} m (world scale {worldScale:0.00000}, applied once); " +
                  $"walk stride {walkStride:0.000} m/cycle (clip {measured:0.000}), run stride {runStride:0.000} m/cycle.");
        instance.SetActive(false);
    }

    private void EvaluatePose()
    {
        graph.Evaluate(0f);
        // Imported clips may write root tracks. The visual fit owns this root;
        // animation owns the bones, while the existing movement owns the player.
        model.localPosition = fittedPosition;
        model.localRotation = fittedRotation;
        model.localScale = fittedScale;
    }

    /// <summary>The camera owner's choice: shown in third person only.</summary>
    public void SetPerspectiveShown(bool shown)
    {
        perspectiveShown = shown;
        Apply();
    }

    /// <summary>Another presentation (the conversation's Elijah, a task) hides the body until it lets go.</summary>
    public void HideFor(object owner)
    {
        if (owner == null) return;
        hiders.Add(owner);
        Apply();
    }

    public void ReleaseHide(object owner)
    {
        if (owner == null) return;
        hiders.Remove(owner);
        Apply();
    }

    private void Apply()
    {
        hiders.RemoveWhere(o => o is Object u && u == null);
        bool show = perspectiveShown && hiders.Count == 0 && model != null && graph.IsValid();
        if (model != null && model.gameObject.activeSelf != show)
        {
            model.gameObject.SetActive(show);
            if (movement != null) movement.BodyDrivesFootsteps = show;
            if (show)
            {
                // Returning from a conversation, task or doorway: no phase burst, no stale jump.
                previousPosition = movement.transform.position;
                jump = default;
                speed = 0f;
                stepWeight = 0f;
                facingYaw = previousYaw = movement.MoveDirection.sqrMagnitude > 0.01f
                    ? Mathf.Atan2(movement.MoveDirection.x, movement.MoveDirection.z) * Mathf.Rad2Deg : facingYaw;
                Follow();
                EvaluatePose();
            }
        }
    }

    private void LateUpdate()
    {
        Apply();
        if (!IsShown || movement == null) return;
        float dt = Time.deltaTime;
        Vector3 travel = movement.transform.position - previousPosition;
        previousPosition = movement.transform.position;
        travel.y = 0f;
        // Do not turn or animate a stride across a teleport, fall recovery or task return.
        if (!movement.isActiveAndEnabled || dt <= 0f || travel.magnitude > ExplorationRules.RunSpeed * 2f * dt + 0.05f)
            travel = Vector3.zero;
        float distance = travel.magnitude;
        float rawSpeed = dt > 0f ? distance / dt : 0f;
        speed += (rawSpeed - speed) * (1f - Mathf.Exp(-14f * dt));

        bool airborne = movement.SprintAndJumpEnabled && movement.AirborneForAnimation;
        jump.Step(movement.WindupSeconds, airborne, movement.VerticalSpeed, movement.LaunchSpeed, movement.GroundBelow, speed > 0.3f, dt);

        // Heading: toward the wanted direction (no moonwalk on reversal), never while idle.
        facingYaw = ExplorationRules.FacingYaw(facingYaw, travel, dt, movement.MoveDirection);
        float turnRate = dt > 0f ? Mathf.Abs(Mathf.DeltaAngle(previousYaw, facingYaw)) / dt : 0f;
        float turnStep = Mathf.Abs(Mathf.DeltaAngle(previousYaw, facingYaw));
        previousYaw = facingYaw;
        // Turning on the spot steps the feet instead of sliding the idle around.
        bool turningInPlace = !airborne && speed < 0.3f && turnRate > 90f;
        stepWeight = Mathf.MoveTowards(stepWeight, turningInPlace ? 0.35f : 0f, dt * 4f);

        float walkW = Mathf.Max(ExplorationRules.WalkWeight(speed), stepWeight);
        float runW = ExplorationRules.RunWeight(speed);
        if (!airborne && jump.Phase != ExplorationRules.JumpPhase.Windup)
        {
            float before = phase;
            float stride = Mathf.Lerp(walkStride, runStride, runW);
            phase = ExplorationRules.AdvancePhase(phase, distance, stride);
            if (turningInPlace) phase = Mathf.Repeat(phase + turnStep / 540f, 1f);
            // Footsteps on actual heel contacts (not a timer); landings play their own.
            if (ExplorationRules.CrossedContact(before, phase, out _) && walkW > 0.5f && jump.Weight < 0.5f && AudioManager.Instance != null)
                AudioManager.Instance.PlayFootstepSFX();
        }
        idleTime += dt;
        idlePlayable.SetTime(idleTime % Mathf.Max(0.05f, idleClip.length));
        walkPlayable.SetTime(phase * walkClip.length);
        runPlayable.SetTime(phase * runClip.length);
        jumpPlayable.SetTime(jump.ClipTime * jumpClip.length);
        float ground = 1f - jump.Weight;
        mixer.SetInputWeight(0, (1f - walkW) * ground);
        mixer.SetInputWeight(1, walkW * (1f - runW) * ground);
        mixer.SetInputWeight(2, walkW * runW * ground);
        mixer.SetInputWeight(3, jump.Weight);
        Follow();
        EvaluatePose();
        // Guard (once, standing idle): a lost fit or separated parts, compared with the same
        // measurement taken at bind time. Bent knees never rescale anything.
        if (speed < 0.05f && jump.Weight < 0.01f && ++poseCheckFrames == 3 && posedReference > 0f &&
            CharacterBodyGeometry.TryMeasure(model, out Bounds posed) &&
            (Mathf.Abs(posed.size.y / posedReference - 1f) > 0.15f || posed.size.x > posedReference * 1.5f))
        {
            Problem = $"post-animation body fit invalid: posed height {posed.size.y:0.000} (bound {posedReference:0.000})";
            Debug.LogError("[ExplorationBody] " + Problem, this);
        }
    }

    /// <summary>Feet on the capsule bottom, upright, facing the player's heading, at the fitted world scale.</summary>
    private Vector3 FeetPosition()
    {
        if (movement == null) return transform.position;
        Transform root = movement.transform;
        Vector3 feet = root.position;
        if (capsule != null)
        {
            float scaleY = Mathf.Abs(root.lossyScale.y);
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(root.lossyScale.x), Mathf.Abs(root.lossyScale.z));
            float half = Mathf.Max(radius, capsule.height * scaleY * 0.5f);
            feet = root.TransformPoint(capsule.center) + Vector3.down * half;
        }
        return feet;
    }

    private void Follow()
    {
        transform.SetPositionAndRotation(FeetPosition(), Quaternion.Euler(0f, facingYaw, 0f));
        Vector3 parent = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(worldScale / Mathf.Max(1e-4f, Mathf.Abs(parent.x)),
            worldScale / Mathf.Max(1e-4f, Mathf.Abs(parent.y)), worldScale / Mathf.Max(1e-4f, Mathf.Abs(parent.z)));
    }
}
