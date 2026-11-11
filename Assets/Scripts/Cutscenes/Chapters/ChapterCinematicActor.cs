using System;
using UnityEngine;

/// <summary>
/// A visual-only character in a chapter cutscene. It owns exactly this actor
/// root and its model; the scene instance carries no gameplay, AI, collider
/// or save component, so nothing else drives it.
///
/// - Base motion: an existing idle, either an Animator state of the shared
///   NpcHumanoid controller (Humanoid rigs; referenced, never edited) or an
///   existing generic clip sampled directly (Elijah's npc_elijah_Idle, as
///   ConversationActorVisual does). Root motion is off: feet stay planted.
/// - Turns: small eased in-place yaw changes of the actor root at authored times.
/// - Attention beats: eased head/neck and chest offsets (look at the paper,
///   turn toward a partner) and short nods, layered after the idle pose.
/// - Held prop: a prop placed at the hand in character space on the first
///   posed frame, then parented to the hand bone so it follows the idle.
/// - Visibility window: renderers are shown only while the actor is on stage.
/// </summary>
public sealed class ChapterCinematicActor : ChapterCinematicElement
{
    [Serializable]
    public struct Turn
    {
        public float start;
        public float duration;
        [Tooltip("World yaw (degrees) the actor faces after this turn.")]
        public float yaw;
    }

    [Serializable]
    public struct Beat
    {
        public string label;
        public float start;
        public float easeIn;
        public float hold;
        public float easeOut;
        [Tooltip("Head offset in the character frame: x = pitch (down), y = yaw (right), z = roll.")]
        public Vector3 head;
        [Tooltip("Upper-body offset in the character frame, split over spine and chest.")]
        public Vector3 chest;
    }

    [Serializable]
    public struct Nod
    {
        public float start;
        public float duration;
        [Tooltip("Peak downward pitch (degrees).")]
        public float degrees;
    }

    [Header("Model")]
    [SerializeField] private Transform model;
    [Tooltip("Humanoid: plays this state of the assigned controller (no root motion).")]
    [SerializeField] private Animator animator;
    [SerializeField] private string stateName = "Standing Idle";
    [SerializeField, Range(0f, 1f)] private float stateOffset;
    [Tooltip("Generic rigs without a controller: this existing clip is sampled on the model each frame.")]
    [SerializeField] private AnimationClip sampledClip;
    [SerializeField, Min(0f)] private float sampledOffset;

    [Header("Staging")]
    [Tooltip("Presentation interval (seconds) in which the actor is visible.")]
    [SerializeField] private Vector2 visibleWindow = new Vector2(0f, 999f);
    [SerializeField] private Turn[] turns = new Turn[0];
    [SerializeField] private Beat[] beats = new Beat[0];
    [SerializeField] private Nod[] nods = new Nod[0];

    [Header("Held prop")]
    [SerializeField] private Transform heldProp;
    [SerializeField] private HumanBodyBones holdBone = HumanBodyBones.RightHand;
    [Tooltip("Grip point from the hand bone in the character frame (x right, y up, z forward), metres.")]
    [SerializeField] private Vector3 gripOffset = new Vector3(0.015f, -0.07f, 0.01f);
    [Tooltip("Prop rotation in the character frame (Euler degrees).")]
    [SerializeField] private Vector3 propEuler;

    private ChapterCinematicPlayer player;
    private Renderer[] renderers;
    private bool shown = true;
    private float baseYaw;
    private Transform spine, chest, neck, head;
    private Transform[] sampledBones;
    private Quaternion[] sampledRest;
    private bool sampledCached;
    private bool propAttached;
    private int preparedFrame = -1;

    public override void Prepare(ChapterCinematicPlayer owner)
    {
        player = owner;
        baseYaw = transform.eulerAngles.y;
        renderers = GetComponentsInChildren<Renderer>(true);
        if (model == null && transform.childCount > 0) model = transform.GetChild(0);

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(stateName))
                animator.Play(stateName, 0, stateOffset);
        }
        FindBones();
        preparedFrame = Time.frameCount;
    }

    public override void Evaluate(in ChapterCinematicFrame frame)
    {
        bool visible = frame.Time >= visibleWindow.x && frame.Time < visibleWindow.y;
        if (visible != shown)
        {
            shown = visible;
            if (renderers != null)
                foreach (Renderer r in renderers)
                    if (r != null) r.enabled = visible;
        }
        transform.rotation = Quaternion.Euler(0f, YawAt(frame.Time), 0f);
    }

    public override void SetPaused(bool paused)
    {
        if (animator != null) animator.speed = paused ? 0f : 1f;
    }

    private void LateUpdate()
    {
        if (player == null) return;
        float time = (float)player.PresentationTime;

        if (sampledClip != null && model != null) SamplePose(time);
        if (!propAttached && Time.frameCount > preparedFrame) AttachProp();
        ApplyAttention(time);
    }

    // =========================================================
    // ROOT TURNS
    // =========================================================

    /// <summary>World yaw at this time after the authored eased turns.</summary>
    public float YawAt(float time)
    {
        float yaw = baseYaw;
        if (turns == null) return yaw;
        foreach (Turn turn in turns)
        {
            if (time <= turn.start) break;
            float t = Ease(Progress(time, turn.start, turn.start + Mathf.Max(0.01f, turn.duration)));
            yaw = yaw + Mathf.DeltaAngle(yaw, turn.yaw) * t;
        }
        return yaw;
    }

    // =========================================================
    // POSE LAYERS
    // =========================================================

    private void ApplyAttention(float time)
    {
        if (head == null) return;
        Vector3 headOffset = Vector3.zero, chestOffset = Vector3.zero;
        if (beats != null)
            foreach (Beat beat in beats)
            {
                float w = BeatWeight(beat, time);
                if (w <= 0f) continue;
                headOffset += beat.head * w;
                chestOffset += beat.chest * w;
            }
        if (nods != null)
            foreach (Nod nod in nods)
                if (time > nod.start && time < nod.start + nod.duration)
                    headOffset.x += nod.degrees * Mathf.Sin(Progress(time, nod.start, nod.start + nod.duration) * Mathf.PI);

        if (headOffset == Vector3.zero && chestOffset == Vector3.zero) return;
        Quaternion character = transform.rotation;
        // Spread the motion over the chain so no single joint snaps.
        if (chest != null)
        {
            ConversationMotion.RotateBone(spine, character, chestOffset * 0.4f);
            ConversationMotion.RotateBone(chest, character, chestOffset * 0.6f);
        }
        else ConversationMotion.RotateBone(spine, character, chestOffset);
        ConversationMotion.RotateBone(neck, character, headOffset * 0.4f);
        ConversationMotion.RotateBone(head, character, headOffset * (neck != null ? 0.6f : 1f));
    }

    /// <summary>0..1 eased weight of a beat (attack, hold, release).</summary>
    public static float BeatWeight(Beat beat, float time)
    {
        float inEnd = beat.start + Mathf.Max(0.01f, beat.easeIn);
        float holdEnd = inEnd + Mathf.Max(0f, beat.hold);
        float outEnd = holdEnd + Mathf.Max(0.01f, beat.easeOut);
        if (time <= beat.start || time >= outEnd) return 0f;
        if (time < inEnd) return Ease(Progress(time, beat.start, inEnd));
        if (time <= holdEnd) return 1f;
        return 1f - Ease(Progress(time, holdEnd, outEnd));
    }

    private void SamplePose(float time)
    {
        // Restore the sampled base before sampling again, so the attention
        // offsets never accumulate on bones the clip leaves untouched.
        if (sampledCached)
            for (int i = 0; i < sampledBones.Length; i++)
                if (sampledBones[i] != null) sampledBones[i].localRotation = sampledRest[i];

        Vector3 p = model.localPosition;
        Quaternion r = model.localRotation;
        Vector3 s = model.localScale;
        float length = Mathf.Max(0.01f, sampledClip.length);
        sampledClip.SampleAnimation(model.gameObject, Mathf.Repeat(time + sampledOffset, length));
        model.localPosition = p;
        model.localRotation = r;
        model.localScale = s;

        if (sampledBones == null) sampledBones = new[] { spine, chest, neck, head };
        if (sampledRest == null) sampledRest = new Quaternion[sampledBones.Length];
        for (int i = 0; i < sampledBones.Length; i++)
            if (sampledBones[i] != null) sampledRest[i] = sampledBones[i].localRotation;
        sampledCached = true;
    }

    private void AttachProp()
    {
        propAttached = true;
        if (heldProp == null) return;
        Transform hand = animator != null && animator.isHuman ? animator.GetBoneTransform(holdBone) : null;
        if (hand == null)
        {
            Debug.LogWarning($"[ChapterCinematic] {name}: no {holdBone} bone; the held prop stays where it was authored.");
            return;
        }
        Quaternion character = transform.rotation;
        heldProp.SetPositionAndRotation(hand.position + character * gripOffset, character * Quaternion.Euler(propEuler));
        heldProp.SetParent(hand, true);
    }

    private void FindBones()
    {
        if (animator != null && animator.isHuman)
        {
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            return;
        }
        if (model == null) return;
        Transform spine1 = null;
        foreach (Transform bone in model.GetComponentsInChildren<Transform>(true))
        {
            string boneName = bone.name;
            int colon = boneName.LastIndexOf(':');
            if (colon >= 0) boneName = boneName.Substring(colon + 1);
            switch (boneName)
            {
                case "Spine": spine = bone; break;
                case "Spine1": spine1 = bone; break;
                case "Spine2": chest = bone; break;
                case "Neck": neck = bone; break;
                case "Head": head = bone; break;
            }
        }
        if (chest == null) chest = spine1;
    }
}
