using UnityEngine;

/// <summary>
/// PURPOSE:
/// Procedural body poses layered on a Humanoid NPC's playing clip, for the
/// activities the project has no clips for (Level 1 Living Plaza):
/// - Sit: the pelvis settles onto a seat, the legs bend with two-bone IK so
///   the feet stay planted, and the hands come to rest on the thighs.
///   Sitting down and standing up lean the chest forward like a real rise.
/// - Reach: one hand to a world point (browsing a counter).
/// - Phone: the right hand held at the ear (on a call).
///
/// Runs in LateUpdate after the Animator wrote this frame's clip pose and
/// before NpcAnimationDriver layers its head/spine offsets, so nothing
/// accumulates. Every layer eases in and out; at zero weight the clip plays
/// untouched. Works on any Humanoid avatar (Mixamo and Synty rigs): bones
/// are found through the avatar and moved in world space, never by name.
///
/// Presentation only: it never moves the NPC root, colliders, dialogue or
/// quest state. A model without a Humanoid avatar reports !Supported and
/// every request is ignored (the caller falls back to standing).
/// </summary>
[DefaultExecutionOrder(-20)]
[DisallowMultipleComponent]
public sealed class NpcBodyPose : MonoBehaviour
{
    [Tooltip("Seconds to sit down or stand up.")]
    [SerializeField, Min(0.2f)] private float sitSeconds = 1.1f;
    [Tooltip("Chest lean (degrees) at the middle of sitting down or standing up.")]
    [SerializeField, Range(0f, 45f)] private float riseLean = 26f;
    [Tooltip("Slight recline (degrees) while seated.")]
    [SerializeField, Range(0f, 15f)] private float seatedRecline = 4f;
    [Tooltip("How much the hands settle onto the thighs while seated.")]
    [SerializeField, Range(0f, 1f)] private float handsOnLap = 0.8f;
    [SerializeField, Min(0.05f)] private float reachSeconds = 0.55f;

    private Animator animator;
    private Transform hips, spine, chest, head;
    private Transform upperLegL, lowerLegL, footL, upperLegR, lowerLegR, footR;
    private Transform upperArmL, lowerArmL, handL, upperArmR, lowerArmR, handR;
    private bool supported;
    private int recacheTries;
    private float legLength;

    private bool hasSeat;
    private Vector3 seatPoint, seatForward;
    private float seatGroundY;
    private float ankleHeight = -1f;
    private float sitWeight, sitTarget;

    private Vector3 reachPoint;
    private bool reachRight = true;
    private float reachWeight, reachTarget;
    private float phoneWeight, phoneTarget;
    private float waveWeight, waveTarget, waveElapsed;

    /// <summary>The model has a Humanoid avatar with legs, arms and a spine.</summary>
    public bool Supported
    {
        get
        {
            if (animator == null) Cache();
            return supported;
        }
    }

    /// <summary>Fully seated (the sit has finished).</summary>
    public bool IsSeated => sitTarget > 0.5f && sitWeight >= 0.999f;

    /// <summary>Fully standing (no sit weight left).</summary>
    public bool IsStanding => sitTarget < 0.5f && sitWeight <= 0.001f;

    /// <summary>Sitting down or standing up right now.</summary>
    public bool IsRising => !IsSeated && !IsStanding;

    /// <summary>Wants to be (or is) seated.</summary>
    public bool WantsSeat => sitTarget > 0.5f;

    /// <summary>
    /// The seat: <paramref name="point"/> is the seat surface under the pelvis,
    /// <paramref name="forward"/> the way a seated person faces (away from the backrest).
    /// </summary>
    public void SetSeat(Vector3 point, Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
        seatPoint = point;
        seatForward = forward.normalized;
        hasSeat = true;
        seatGroundY = GroundBelow(point + seatForward * 0.45f, point.y - 0.2f);
    }

    /// <summary>Sit down (true) or stand up (false); eases over sitSeconds from where the body is now.</summary>
    public void Sit(bool seated)
    {
        if (seated && (!hasSeat || !Supported)) return;
        sitTarget = seated ? 1f : 0f;
    }

    /// <summary>Snaps the sit weight (restore, first frame) without the rise motion.</summary>
    public void SnapSit(bool seated)
    {
        if (seated && (!hasSeat || !Supported)) seated = false;
        sitTarget = sitWeight = seated ? 1f : 0f;
    }

    /// <summary>Reaches one hand toward a world point (null lowers it).</summary>
    public void Reach(Vector3? point, bool rightHand = true)
    {
        if (point.HasValue)
        {
            reachPoint = point.Value;
            reachRight = rightHand;
            reachTarget = 1f;
        }
        else reachTarget = 0f;
    }

    /// <summary>Holds (true) or lowers (false) the right hand at the ear.</summary>
    public void Phone(bool onCall) => phoneTarget = onCall ? 1f : 0f;

    /// <summary>Raises and gently waves the right hand (false lowers it).</summary>
    public void Wave(bool waving)
    {
        if (waving && waveTarget < 0.5f) waveElapsed = 0f;
        waveTarget = waving ? 1f : 0f;
    }

    private void Awake() => Cache();

    private void Cache()
    {
        animator = null;
        foreach (Animator candidate in GetComponentsInChildren<Animator>(true))
            if (candidate.runtimeAnimatorController != null && candidate.isHuman && candidate.avatar != null && candidate.avatar.isValid)
            {
                animator = candidate;
                break;
            }
        supported = false;
        if (animator == null) return;
        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        head = animator.GetBoneTransform(HumanBodyBones.Head);
        upperLegL = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        lowerLegL = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        footL = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        upperLegR = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        lowerLegR = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        footR = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        upperArmL = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        lowerArmL = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        handL = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        upperArmR = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        lowerArmR = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        handR = animator.GetBoneTransform(HumanBodyBones.RightHand);
        supported = hips != null && spine != null && head != null &&
                    upperLegL != null && lowerLegL != null && footL != null &&
                    upperLegR != null && lowerLegR != null && footR != null &&
                    upperArmL != null && lowerArmL != null && handL != null &&
                    upperArmR != null && lowerArmR != null && handR != null;
        if (supported)
            legLength = Vector3.Distance(upperLegL.position, lowerLegL.position) +
                        Vector3.Distance(lowerLegL.position, footL.position);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        sitWeight = Mathf.MoveTowards(sitWeight, sitTarget, dt / Mathf.Max(0.05f, sitSeconds));
        reachWeight = Mathf.MoveTowards(reachWeight, reachTarget, dt / reachSeconds);
        phoneWeight = Mathf.MoveTowards(phoneWeight, phoneTarget, dt / reachSeconds);
        waveWeight = Mathf.MoveTowards(waveWeight, waveTarget, dt / reachSeconds);
        if (waveTarget > 0.001f || waveWeight > 0.001f) waveElapsed += dt;
        // A model shown later had no live avatar at Awake: look again now and then (for a while) until it does.
        if (!supported && recacheTries < 40 && Time.frameCount % 30 == 0)
        {
            recacheTries++;
            Cache();
        }
        if (!supported || animator == null || !animator.isActiveAndEnabled) return;

        if (ankleHeight < 0f && sitWeight <= 0.001f)
        {
            // Measured once from the standing clip: how high the ankle sits above the ground.
            float ground = GroundBelow(footL.position, footL.position.y - 0.3f);
            ankleHeight = Mathf.Clamp(footL.position.y - ground, 0.02f, 0.2f);
        }

        float sit = Smooth(sitWeight);
        if (sit > 0.0001f && hasSeat) ApplySit(sit);
        if (reachWeight > 0.0001f) ApplyReach(Smooth(reachWeight));
        if (waveWeight > 0.0001f) ApplyWave(Smooth(waveWeight));
        if (phoneWeight > 0.0001f) ApplyPhone(Smooth(phoneWeight));
    }

    private void OnDisable()
    {
        waveTarget = waveWeight = waveElapsed = 0f;
    }

    // =========================================================
    // SIT
    // =========================================================

    private void ApplySit(float w)
    {
        Vector3 up = Vector3.up;
        Vector3 f = seatForward;
        Vector3 r = Vector3.Cross(up, f);
        float ankle = ankleHeight > 0f ? ankleHeight : 0.08f;

        // The clip's own feet, before anything moves (standing feet stay where they are).
        Vector3 clipFootL = footL.position, clipFootR = footR.position;
        Quaternion footRotL = footL.rotation, footRotR = footR.rotation;
        float thigh = Vector3.Distance(upperLegL.position, lowerLegL.position);

        // The pelvis settles onto the seat.
        Vector3 hipSeat = seatPoint + up * (legLength * 0.12f);
        hips.position = Vector3.Lerp(hips.position, hipSeat, w);

        // Rising and settling lean the chest forward; seated, a slight recline.
        float lean = riseLean * Mathf.Sin(Mathf.PI * w) - seatedRecline * w * w * w;
        if (Mathf.Abs(lean) > 0.01f)
        {
            Transform torso = chest != null ? chest : spine;
            spine.rotation = Quaternion.AngleAxis(lean * 0.6f, r) * spine.rotation;
            if (torso != spine) torso.rotation = Quaternion.AngleAxis(lean * 0.4f, r) * torso.rotation;
        }

        // Feet planted in front of the seat, the knees bending forward.
        Vector3 hipCentre = hips.position;
        LegToSeat(upperLegL, lowerLegL, footL, clipFootL, footRotL, hipCentre, hipSeat, f, r, thigh, ankle, w);
        LegToSeat(upperLegR, lowerLegR, footR, clipFootR, footRotR, hipCentre, hipSeat, f, r, thigh, ankle, w);

        // Hands come to rest on the thighs.
        float lap = handsOnLap * w * w;
        if (lap > 0.001f)
        {
            ArmToLap(upperArmL, lowerArmL, handL, upperLegL, lowerLegL, f, r, up, -1f, lap);
            ArmToLap(upperArmR, lowerArmR, handR, upperLegR, lowerLegR, f, r, up, 1f, lap);
        }
    }

    private void LegToSeat(Transform a, Transform b, Transform c, Vector3 clipFoot, Quaternion footRotation,
        Vector3 hipCentre, Vector3 hipSeat, Vector3 f, Vector3 r, float thigh, float ankle, float w)
    {
        float lateral = Vector3.Dot(a.position - hipCentre, r);
        Vector3 seated = hipSeat + f * (thigh * 0.95f) + r * (lateral * 1.15f);
        seated.y = seatGroundY + ankle;
        Vector3 target = Vector3.Lerp(clipFoot, seated, w);
        Vector3 pole = a.position + f * 1.2f + Vector3.up * 0.2f;
        SolveTwoBone(a, b, c, target, pole);
        c.rotation = footRotation;
    }

    private static void ArmToLap(Transform shoulder, Transform elbow, Transform hand, Transform legTop, Transform knee,
        Vector3 f, Vector3 r, Vector3 up, float side, float weight)
    {
        Vector3 onThigh = Vector3.Lerp(legTop.position, knee.position, 0.6f) + up * 0.09f + r * (side * 0.04f);
        Vector3 target = Vector3.Lerp(hand.position, onThigh, weight);
        Vector3 pole = shoulder.position - f * 0.5f + r * (side * 0.35f) - up * 0.4f;
        SolveTwoBone(shoulder, elbow, hand, target, pole);
    }

    // =========================================================
    // REACH AND PHONE
    // =========================================================

    private void ApplyReach(float w)
    {
        Transform a = reachRight ? upperArmR : upperArmL;
        Transform b = reachRight ? lowerArmR : lowerArmL;
        Transform c = reachRight ? handR : handL;
        Vector3 f = CharacterForward();
        Vector3 r = Vector3.Cross(Vector3.up, f);
        float side = reachRight ? 1f : -1f;
        Vector3 target = Vector3.Lerp(c.position, reachPoint, w);
        Vector3 pole = a.position - f * 0.3f + r * (side * 0.45f) - Vector3.up * 0.5f;
        SolveTwoBone(a, b, c, target, pole);
    }

    private void ApplyWave(float w)
    {
        Vector3 f = CharacterForward();
        Vector3 r = Vector3.Cross(Vector3.up, f);
        float scale = legLength > 0.1f ? legLength / 0.85f : 1f;
        Vector3 center = upperArmR.position + Vector3.up * (0.34f * scale) + f * (0.1f * scale) + r * (0.1f * scale);
        float cycle = waveElapsed * 8f;
        Vector3 waving = center + r * (Mathf.Sin(cycle) * 0.08f * scale) + Vector3.up * (Mathf.Abs(Mathf.Sin(cycle)) * 0.025f * scale);
        Vector3 target = Vector3.Lerp(handR.position, waving, w);
        Vector3 pole = upperArmR.position - f * 0.2f + r * 0.35f - Vector3.up * 0.25f;
        SolveTwoBone(upperArmR, lowerArmR, handR, target, pole);
    }

    private void ApplyPhone(float w)
    {
        Vector3 f = CharacterForward();
        Vector3 r = Vector3.Cross(Vector3.up, f);
        float scale = legLength > 0.1f ? legLength / 0.85f : 1f;
        Vector3 ear = head.position + r * (0.085f * scale) - Vector3.up * (0.05f * scale) + f * (0.02f * scale);
        Vector3 target = Vector3.Lerp(handR.position, ear, w);
        Vector3 pole = upperArmR.position - Vector3.up * 0.6f + r * 0.3f + f * 0.25f;
        SolveTwoBone(upperArmR, lowerArmR, handR, target, pole);
    }

    private Vector3 CharacterForward()
    {
        Vector3 f = animator.transform.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    /// <summary>
    /// Two-bone IK by aiming: the middle joint is placed on the circle the
    /// chain allows, in the plane of the root, target and pole; then the
    /// lower bone aims at the (reachable) target. Rig-agnostic (world space).
    /// </summary>
    private static void SolveTwoBone(Transform a, Transform b, Transform c, Vector3 target, Vector3 pole)
    {
        Vector3 aPos = a.position, bPos = b.position, cPos = c.position;
        float upper = (bPos - aPos).magnitude, lower = (cPos - bPos).magnitude;
        if (upper < 1e-4f || lower < 1e-4f) return;
        Vector3 toTarget = target - aPos;
        float reach = toTarget.magnitude;
        if (reach < 1e-4f) return;
        Vector3 dir = toTarget / reach;
        reach = Mathf.Clamp(reach, Mathf.Abs(upper - lower) + 1e-3f, upper + lower - 1e-3f);

        Vector3 bend = Vector3.ProjectOnPlane(pole - aPos, dir);
        if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(bPos - aPos, dir);
        if (bend.sqrMagnitude < 1e-6f) return;
        bend.Normalize();

        float cosA = (upper * upper + reach * reach - lower * lower) / (2f * upper * reach);
        float angle = Mathf.Acos(Mathf.Clamp(cosA, -1f, 1f));
        Vector3 middle = aPos + (dir * Mathf.Cos(angle) + bend * Mathf.Sin(angle)) * upper;

        a.rotation = Quaternion.FromToRotation(bPos - aPos, middle - aPos) * a.rotation;
        Vector3 newB = b.position, newC = c.position;
        Vector3 end = aPos + dir * reach;
        b.rotation = Quaternion.FromToRotation(newC - newB, end - newB) * b.rotation;
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    private float GroundBelow(Vector3 point, float fallback)
    {
        var hits = Physics.RaycastAll(point + Vector3.up * 1.2f, Vector3.down, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (hit.normal.y < 0.6f || hit.transform.IsChildOf(transform)) continue;
            if (hit.point.y > point.y + 0.6f) continue;
            if (hit.point.y > best) best = hit.point.y;
        }
        return float.IsNegativeInfinity(best) ? fallback : best;
    }
}
