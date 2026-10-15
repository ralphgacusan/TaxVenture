using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// T03 checkpoint 1: the conversation-only, visual-only standing Elijah.
///
/// - Hidden outside an admitted NPC conversation; DialogueCameraDirector shows it.
/// - Built from the existing Elijah model asset at runtime. The model gets no
///   colliders, so it can never block movement, interaction raycasts or shots.
/// - Never touches the gameplay player root, Rigidbody or capsule, and never
///   edits the shared FBX importers. Scale and foot offset come from the posed
///   model's MEASURED geometry (recorded by Tools > TaxVenture > Configure
///   Dialogue Presentation) and a chosen standing height in world units, not
///   from the player capsule.
/// - Pose: the idle clip is sampled directly (no Animator controller asset),
///   either held on one frame or looped as a subtle idle. Walking clips are
///   deliberately not supported as a standing pose.
/// </summary>
[DisallowMultipleComponent]
public class ConversationActorVisual : MonoBehaviour
{
    [Header("Existing Elijah assets")]
    [Tooltip("Model asset to show during conversations (npc_elijah_Idle, or npc_elijah_static).")]
    [SerializeField] private GameObject modelPrefab;

    [Tooltip("Idle clip used for the standing pose. Leave empty for a static model.")]
    [SerializeField] private AnimationClip poseClip;

    [Tooltip("Clip time (seconds) of the held standing pose.")]
    [SerializeField, Min(0f)] private float poseTime;

    [Tooltip("Loop the idle clip while visible instead of holding one frame.")]
    [SerializeField] private bool animateIdle = true;

    [Tooltip("Idle sampling rate cap (Hz) to keep Android cost low.")]
    [SerializeField, Range(10f, 60f)] private float idleSampleRate = 30f;

    [Header("Fit (world units)")]
    [Tooltip("Standing height of the visual in world units. The setup tool proposes the median measured NPC height.")]
    [SerializeField, Min(0f)] private float standingHeight;

    [Header("Measured posed geometry (written by the setup tool)")]
    [Tooltip("Lowest point of the posed model in its own root space at scale 1 (feet/coat hem).")]
    [SerializeField] private float nativeMinY;

    [Tooltip("Highest point of the posed model in its own root space at scale 1.")]
    [SerializeField] private float nativeMaxY;

    [Tooltip("Eye/head height as a fraction of standing height; shots aim here.")]
    [SerializeField, Range(0.5f, 1f)] private float headHeightFraction = 0.9f;

    [Tooltip("Yaw (degrees) that turns the model to face +Z of this object.")]
    [SerializeField] private float forwardYawOffset;

    [Header("Rendering")]
    [SerializeField] private bool castShadows = true;

    [Header("Conversation motion (shared with NPCs)")]
    [Tooltip("Talk/listen head, neck and spine motion layered on the idle pose, the same ConversationMotion NPCs use. Bones are found by name (Mixamo Head/Neck/Spine).")]
    [SerializeField] private bool conversationMotion = true;
    [SerializeField, Range(0f, 2f)] private float conversationStrength = 1f;
    [SerializeField, Range(0f, 6f)] private float speechDegrees = 3.5f;
    [Tooltip("How far the head turns toward the NPC being spoken to.")]
    [SerializeField, Range(0f, 45f)] private float headTurnLimit = 25f;

    private Transform modelRoot;
    private bool shown;
    private float idleClock;
    private float nextSampleTime;
    private bool warnedMissingMeasure;

    private DialogueUI dialogueUI;
    private Transform[] motionBones; // spine, chest, neck, head
    private Quaternion[] sampledRotations;
    private bool poseCached;
    private bool hasLine, speaking, revealed;
    private int lineIndex;
    private ConversationMotion.LineMood lineMood;
    private float lineStart, revealTime, motionSeed;
    private Vector3 smoothedHead, smoothedBody;

    public float StandingHeight => standingHeight > 0f ? standingHeight : 1.7f;
    public float HeadHeight => StandingHeight * headHeightFraction;
    public bool IsShown => shown;
    public bool HasModel => modelRoot != null;
    public Vector3 HeadPosition => transform.position + Vector3.up * HeadHeight;

    private void Awake()
    {
        motionSeed = Random.Range(0f, 100f);
        EnsureModel();
        Hide();
    }

    private void OnEnable()
    {
        if (dialogueUI == null)
            dialogueUI = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (dialogueUI == null) return;
        dialogueUI.LineChanged += HandleLineChanged;
        dialogueUI.LineRevealed += HandleLineRevealed;
        dialogueUI.DialogueEnded += HandleDialogueEnded;
        if (dialogueUI.IsDialogueActive && dialogueUI.CurrentLine != null)
            HandleLineChanged(dialogueUI.CurrentLine);
    }

    private void OnDisable()
    {
        if (dialogueUI != null)
        {
            dialogueUI.LineChanged -= HandleLineChanged;
            dialogueUI.LineRevealed -= HandleLineRevealed;
            dialogueUI.DialogueEnded -= HandleDialogueEnded;
        }
        hasLine = false;

        // A disabled/unloaded visual must not stay visible in exploration.
        if (modelRoot != null) modelRoot.gameObject.SetActive(false);
        shown = false;
    }

    /// <summary>Instantiates the model once, strips physics and applies the fit.</summary>
    public bool EnsureModel()
    {
        if (modelRoot != null) return true;
        if (modelPrefab == null)
        {
            Debug.LogWarning("[ConversationActorVisual] No Elijah model assigned. Run Tools > TaxVenture > Configure Dialogue Presentation.", this);
            return false;
        }

        GameObject instance = Instantiate(modelPrefab, transform, false);
        instance.name = "ElijahConversationModel";
        modelRoot = instance.transform;

        foreach (Collider c in instance.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true)) Destroy(body);
        foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
            animator.enabled = false; // Pose is sampled directly; no controller asset is required.
        foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            if (r is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
        }

        CacheMotionBones();
        SamplePose(poseTime);
        ApplyFit();
        instance.SetActive(false);
        return true;
    }

    /// <summary>
    /// Stands the (still hidden) visual on <paramref name="floorPosition"/> facing
    /// <paramref name="yawDegrees"/>. Visibility is separate so the director can
    /// keep it hidden while the camera is inside Elijah's head.
    /// </summary>
    public bool Place(Vector3 floorPosition, float yawDegrees)
    {
        if (!isActiveAndEnabled || !EnsureModel()) return false;
        transform.SetPositionAndRotation(floorPosition, Quaternion.Euler(0f, yawDegrees, 0f));
        transform.localScale = Vector3.one;
        idleClock = 0f;
        nextSampleTime = 0f;
        return true;
    }

    public void SetVisible(bool visible)
    {
        if (visible == shown) return;
        if (!visible) { Hide(); return; }
        if (!isActiveAndEnabled || modelRoot == null) return;
        modelRoot.gameObject.SetActive(true);
        SamplePose(poseTime);
        smoothedHead = smoothedBody = Vector3.zero;
        shown = true;
    }

    public void Hide()
    {
        if (modelRoot != null) modelRoot.gameObject.SetActive(false);
        shown = false;
    }

    private void LateUpdate()
    {
        if (!shown || modelRoot == null) return;
        if (animateIdle && poseClip != null)
        {
            idleClock += Time.unscaledDeltaTime;
            if (idleClock >= nextSampleTime)
            {
                nextSampleTime = idleClock + 1f / idleSampleRate;
                float length = Mathf.Max(0.01f, poseClip.length);
                SamplePose((poseTime + idleClock) % length);
            }
        }
        ApplyConversationMotion();
    }

    // Elijah takes part in every admitted conversation: his own lines are
    // speaking, the NPC's lines are listening.
    private void HandleLineChanged(DialogueLine line)
    {
        hasLine = line != null;
        if (!hasLine) return;
        speaking = line.Speaker == DialogueSpeaker.Player;
        lineIndex++;
        lineStart = Time.time;
        revealed = false;
        lineMood = ConversationMotion.MoodOf(line.Text);
    }

    private void HandleLineRevealed(DialogueLine line)
    {
        if (!hasLine || revealed) return;
        revealed = true;
        revealTime = Time.time;
    }

    private void HandleDialogueEnded(DialogueEndReason reason)
    {
        hasLine = false;
    }

    private void CacheMotionBones()
    {
        Transform spine = null, chest = null, upperChest = null, neck = null, head = null;
        foreach (Transform bone in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            string boneName = bone.name;
            int colon = boneName.LastIndexOf(':');
            if (colon >= 0) boneName = boneName.Substring(colon + 1);
            switch (boneName)
            {
                case "Spine": spine = bone; break;
                case "Spine1": chest = bone; break;
                case "Spine2": upperChest = bone; break;
                case "Neck": neck = bone; break;
                case "Head": head = bone; break;
            }
        }
        if (head == null) return;
        motionBones = new[] { spine, upperChest != null ? upperChest : chest, neck, head };
        sampledRotations = new Quaternion[motionBones.Length];
    }

    /// <summary>
    /// Layers the shared NPC talk/listen motion on the sampled idle pose.
    /// The idle is sampled at a capped rate, so bones are reset to the last
    /// sampled pose each frame before the offset; offsets never accumulate.
    /// </summary>
    private void ApplyConversationMotion()
    {
        if (!conversationMotion || motionBones == null) return;
        for (int i = 0; i < motionBones.Length; i++)
        {
            Transform bone = motionBones[i];
            if (bone == null) continue;
            if (poseCached) bone.localRotation = sampledRotations[i];
            else sampledRotations[i] = bone.localRotation;
        }
        poseCached = true;

        float now = Time.time;
        Vector3 head = new Vector3(Mathf.Sin(now * 1.1f) * 0.8f,
            (Mathf.PerlinNoise(motionSeed, now * 0.15f) - 0.5f) * 6f, 0f);
        Vector3 body = Vector3.zero;
        head.y += Mathf.Clamp(PartnerYaw(), -headTurnLimit, headTurnLimit);
        if (hasLine)
            ConversationMotion.Apply(speaking, now, now - lineStart, revealed ? now - revealTime : -1f,
                lineMood, lineIndex, motionSeed, speechDegrees, conversationStrength, ref head, ref body);

        float k = 1f - Mathf.Exp(-10f * Time.deltaTime);
        smoothedHead = Vector3.Lerp(smoothedHead, head, k);
        smoothedBody = Vector3.Lerp(smoothedBody, body, k);

        // This object faces +Z toward the partner; the model's yaw offset is
        // already applied to modelRoot, so this is the character frame.
        Quaternion character = transform.rotation;
        float spineShare = motionBones[1] != null ? 0.5f : 1f;
        ConversationMotion.RotateBone(motionBones[0], character, smoothedBody * spineShare);
        ConversationMotion.RotateBone(motionBones[1], character, smoothedBody * (1f - spineShare));
        float headShare = motionBones[2] != null ? 0.65f : 1f;
        ConversationMotion.RotateBone(motionBones[2], character, smoothedHead * (1f - headShare));
        ConversationMotion.RotateBone(motionBones[3], character, smoothedHead * headShare);
    }

    /// <summary>Yaw toward the NPC that owns the conversation, in this object's frame.</summary>
    private float PartnerYaw()
    {
        Component owner = dialogueUI != null ? dialogueUI.CurrentOwner as Component : null;
        if (owner == null) return 0f;
        Vector3 local = transform.InverseTransformDirection(owner.transform.position - transform.position);
        local.y = 0f;
        return local.sqrMagnitude < 0.0001f ? 0f : Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
    }

    private void SamplePose(float time)
    {
        if (poseClip == null || modelRoot == null) return;
        // Sampling moves bones only; keep the fitted root transform authoritative.
        Vector3 p = modelRoot.localPosition;
        Quaternion r = modelRoot.localRotation;
        Vector3 s = modelRoot.localScale;
        poseClip.SampleAnimation(modelRoot.gameObject, time);
        poseCached = false;
        modelRoot.localPosition = p;
        modelRoot.localRotation = r;
        modelRoot.localScale = s;
    }

    private void ApplyFit()
    {
        if (modelRoot == null) return;
        float minY = nativeMinY;
        float maxY = nativeMaxY;
        if (maxY - minY <= 1e-4f && !MeasureRuntimeFallback(out minY, out maxY)) return;

        ComputeModelLocal(StandingHeight, minY, maxY, forwardYawOffset,
            out Vector3 localPosition, out Quaternion localRotation, out float scale);
        modelRoot.localPosition = localPosition;
        modelRoot.localRotation = localRotation;
        modelRoot.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// Shared with the Editor preview: scale = height / native height, and the
    /// lowest posed point lands exactly on this object's origin (the floor).
    /// </summary>
    public static void ComputeModelLocal(float height, float minY, float maxY, float yawOffset,
        out Vector3 localPosition, out Quaternion localRotation, out float scale)
    {
        float native = Mathf.Max(1e-4f, maxY - minY);
        scale = Mathf.Max(1e-4f, height) / native;
        localPosition = new Vector3(0f, -minY * scale, 0f);
        localRotation = Quaternion.Euler(0f, yawOffset, 0f);
    }

    // Approximation only when the setup tool has not recorded measurements.
    private bool MeasureRuntimeFallback(out float minY, out float maxY)
    {
        minY = float.MaxValue;
        maxY = float.MinValue;
        var renderers = new List<Renderer>();
        modelRoot.GetComponentsInChildren(true, renderers);
        Matrix4x4 toRoot = modelRoot.worldToLocalMatrix;
        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                float y = toRoot.MultiplyPoint3x4(corner).y;
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }
        bool ok = maxY > minY;
        if (!warnedMissingMeasure)
        {
            warnedMissingMeasure = true;
            Debug.LogWarning("[ConversationActorVisual] Posed geometry was not measured by the setup tool; using approximate renderer bounds. Run Tools > TaxVenture > Configure Dialogue Presentation.", this);
        }
        return ok;
    }
}
