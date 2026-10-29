using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps an NPC's gameplay state to optional Animator states. The component
/// never owns dialogue or progression and safely does nothing when an NPC
/// has no Animator or the configured state is unavailable.
/// </summary>
[DisallowMultipleComponent]
public sealed class NpcAnimationDriver : MonoBehaviour
{
    public enum Behavior
    {
        Idle,
        Waiting,
        Interacting,
        Talking,
        Listening,
        Thinking,
        Gesturing,
        Pointing,
        Reacting,
        Sitting,
        Walking
    }

    [Serializable]
    private struct StateBinding
    {
        public Behavior behavior;
        [Tooltip("Animator state name on the configured layer. Leave empty when this behavior has no clip.")]
        public string stateName;
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NpcStateMachine stateMachine;
    [SerializeField] private DialogueUI dialogueUI;

    [Header("Animator State Names")]
    [SerializeField] private Behavior defaultBehavior = Behavior.Idle;
    [SerializeField] private string idleStateName = "Standing Idle";
    [SerializeField] private StateBinding[] stateBindings;
    [Tooltip("Slower walk used for ambient strolls and patrols (falls back to Walking when the controller has no such state).")]
    [SerializeField] private string strollStateName = "Strolling";
    [SerializeField, Min(0f)] private float transitionSeconds = 0.18f;
    [SerializeField, Min(0)] private int animatorLayer;

    [Header("Movement")]
    [SerializeField] private bool detectMovement = true;
    [SerializeField, Min(0f)] private float movementThreshold = 0.08f;

    [Header("Static mesh fallback (no skeletal gestures)")]
    [Tooltip("Subtle head movement for the existing unrigged office characters. Disabled when an active Animator owns the model.")]
    [SerializeField] private bool animateStaticHead = true;
    [Tooltip("Optional head, face, hair and accessory mesh transforms. Empty uses the existing model part names.")]
    [SerializeField] private Transform[] headParts;
    [SerializeField, Range(0f, 25f)] private float conversationTurnLimit = 18f;
    [SerializeField, Range(0f, 6f)] private float headMotionDegrees = 3.5f;
    [Tooltip("Slow look-around while idle.")]
    [SerializeField, Range(0f, 30f)] private float idleGlanceDegrees = 14f;
    [Tooltip("Breathing, sway and lean on the model container. Colliders on the NPC root never move.")]
    [SerializeField] private bool animateStaticBody = true;
    [Tooltip("Model container moved by the fallback. Empty uses the 'Visual'/'Visuals' child.")]
    [SerializeField] private Transform bodyRoot;
    [SerializeField, Range(0f, 6f)] private float bodySwayDegrees = 1.8f;
    [SerializeField, Range(0f, 0.05f)] private float breathingScale = 0.015f;

    [Header("Idle variation")]
    [Tooltip("Scales occasional idle actions: look around, weight shift, look down/up, glance at the player.")]
    [SerializeField, Range(0f, 2f)] private float idleActionStrength = 1f;
    [Tooltip("Seconds between idle actions (random in range).")]
    [SerializeField] private Vector2 idleActionInterval = new Vector2(2.5f, 6f);
    [Tooltip("Layer head/neck/spine motion on top of a Humanoid idle clip.")]
    [SerializeField] private bool animateRigBones = true;

    [Header("Conversation (procedural fallback; no talk clips exist yet)")]
    [Tooltip("Scales speech head motion, line beats, listening nods and the farewell nod.")]
    [SerializeField, Range(0f, 2f)] private float conversationStrength = 1f;
    [Tooltip("Upper-body turn toward the player beyond the head turn. Only the model turns; the NPC root never rotates here.")]
    [SerializeField, Range(0f, 60f)] private float maxConversationBodyTurn = 35f;

    private struct HeadPose
    {
        public Transform part;
        public Vector3 localPosition, rootPosition;
        public Quaternion localRotation, rootRotation;
    }
    private HeadPose[] headPoses;
    private Transform headSpace;
    private Vector3 headPivot;
    private bool headWasMoved;
    private float motionClock;
    private float motionSeed;

    private Vector3 bodyRestPosition, bodyRestScale, bodyPivot;
    private Quaternion bodyRestRotation;
    private bool bodyCached;
    private Vector3 smoothedHead, smoothedBody;
    private float amplitude = 1f, tempo = 1f;
    private IdleAction currentAction, lastAction;
    private float actionStart, actionDuration, actionSign, nextActionTime;
    private Transform headBone, neckBone, spineBone, chestBone;
    private bool bonesCachedLive;
    private int lineIndex;
    private ConversationMotion.LineMood lineMood;
    private bool lineRevealed;
    private float lineStartTime, revealTime, farewellStart = -1f;
    private Transform ambientPartner;
    private bool ambientTalking;
    private int ambientTurn;
    private float ambientTurnStart;
    private ConversationMotion.LineMood ambientMood;

    private readonly Dictionary<Behavior, int> stateHashes = new Dictionary<Behavior, int>();
    private readonly Dictionary<Behavior, bool> availableStates = new Dictionary<Behavior, bool>();

    private int idleStateHash;
    private int activeStateHash;
    private Vector3 previousPosition;
    private Behavior requestedBehavior = Behavior.Idle;
    private Behavior stateBehavior = Behavior.Idle;
    private bool isMoving;
    private bool locomotionRequested;
    private bool strollRequested;
    private int strollStateHash;
    private readonly Dictionary<int, float> locomotionSpeeds = new Dictionary<int, float>();
    private readonly List<AnimatorClipInfo> clipInfos = new List<AnimatorClipInfo>(2);
    private bool hasPosition;
    private bool ownsCurrentConversation;

    private void Awake()
    {
        CacheReferences();
        CacheStateNames();
        CacheBody();
        CacheHeadParts();
        motionSeed = UnityEngine.Random.Range(0f, 100f);
        motionClock = motionSeed;
        // Per-NPC variation so neighbours never move in sync.
        amplitude = UnityEngine.Random.Range(0.85f, 1.2f);
        tempo = UnityEngine.Random.Range(0.85f, 1.15f);
        nextActionTime = Time.time + UnityEngine.Random.Range(0.5f, 4f);
        CacheBones();
        CacheFacing();
        stateBehavior = requestedBehavior = defaultBehavior;
        previousPosition = transform.position;
        hasPosition = true;
    }

    private void OnEnable()
    {
        CacheReferences();
        previousPosition = transform.position;
        hasPosition = true;
        isMoving = false;
        activeStateHash = 0;
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += HandleNpcStateChanged;

            // Start only runs once. Re-enabling this component must restore
            // the animation for an FSM state that was entered while disabled.
            if (stateMachine.CurrentState != null)
                HandleNpcStateChanged(stateMachine.CurrentState);
        }
        else
        {
            ApplyBehavior(requestedBehavior);
        }

        if (dialogueUI != null)
        {
            dialogueUI.DialogueStarted += HandleDialogueStarted;
            dialogueUI.LineChanged += HandleDialogueLineChanged;
            dialogueUI.LineRevealed += HandleDialogueLineRevealed;
            dialogueUI.DialogueEnded += HandleDialogueEnded;
            SyncWithActiveDialogue();
        }
    }

    private void Start()
    {
        if (stateMachine != null && stateMachine.CurrentState != null)
            HandleNpcStateChanged(stateMachine.CurrentState);
        else
            SetBehavior(defaultBehavior);
        SyncWithActiveDialogue();

        // Desynchronize identical idle clips (Jhun and Zorbix share one).
        if (AnimatorOwnsPose && idleStateHash != 0 && animator.HasState(animatorLayer, idleStateHash))
        {
            animator.speed = UnityEngine.Random.Range(0.9f, 1.1f);
            animator.Play(idleStateHash, animatorLayer, UnityEngine.Random.value);
        }
    }

    private void OnDisable()
    {
        if (stateMachine != null)
            stateMachine.OnStateChanged -= HandleNpcStateChanged;
        if (dialogueUI != null)
        {
            dialogueUI.DialogueStarted -= HandleDialogueStarted;
            dialogueUI.LineChanged -= HandleDialogueLineChanged;
            dialogueUI.LineRevealed -= HandleDialogueLineRevealed;
            dialogueUI.DialogueEnded -= HandleDialogueEnded;
        }
        ownsCurrentConversation = false;
        farewellStart = -1f;
        RestoreHead();
    }

    private void Update()
    {
        // A model revealed later (Arthur) starts on the controller's default state: give it this NPC's own.
        if (activeStateHash == 0 && AnimatorOwnsPose)
            ApplyBehavior(isMoving && !ownsCurrentConversation ? Behavior.Walking : requestedBehavior);
        MeasureWalkSpeed();
        if (!detectMovement && !locomotionRequested && !isMoving)
            return;

        Vector3 position = transform.position;
        float deltaTime = Time.deltaTime;
        float speed = 0f;
        if (hasPosition && deltaTime > 0f)
        {
            Vector3 displacement = position - previousPosition;
            displacement.y = 0f;
            speed = displacement.magnitude / deltaTime;
        }

        previousPosition = position;
        hasPosition = true;

        bool moving = locomotionRequested || (detectMovement && speed >= movementThreshold);
        if (moving == isMoving)
            return;

        isMoving = moving;
        if (ownsCurrentConversation || (stateMachine != null && stateMachine.CurrentState is NpcDialogueState))
            return;

        ApplyBehavior(moving ? Behavior.Walking : requestedBehavior);
    }

    /// <summary>
    /// True when moving this NPC will look like walking: an assigned Walking
    /// state on an active Animator, or the static body step bob.
    /// </summary>
    public bool SupportsWalking => AnimatorOwnsPose ? HasAnimatedWalk : animateStaticBody && bodyCached;

    /// <summary>True when an active Animator has an assigned Walking state.</summary>
    public bool HasAnimatedWalk
    {
        get
        {
            if (!AnimatorOwnsPose) return false;
            int hash = GetStateHash(Behavior.Walking);
            return hash != 0 && HasState(Behavior.Walking, hash);
        }
    }

    /// <summary>
    /// World speed (m/s) the playing walk (or stroll) clip covers, so movers
    /// can match the feet. Zero until that state has been entered once.
    /// </summary>
    public float AnimatedWalkSpeed { get; private set; }

    /// <summary>After an instant placement: the jump is not movement (no one-frame walk).</summary>
    public void ResetMotion()
    {
        previousPosition = transform.position;
        hasPosition = true;
    }

    /// <summary>This NPC is saying its own dialogue line right now (not listening).</summary>
    public bool IsSpeaking => ownsCurrentConversation && requestedBehavior == Behavior.Talking;

    /// <summary>
    /// A background chat with another NPC (Living Plaza pairs): the head turns
    /// to <paramref name="partner"/> and plays the same talk or listen motion
    /// as a dialogue, without any text. Null ends it. The player's own
    /// conversation with this NPC always takes over.
    /// </summary>
    public void SetAmbientConversation(Transform partner, bool talking)
    {
        if (partner == ambientPartner && talking == ambientTalking) return;
        ambientPartner = partner;
        ambientTalking = talking;
        ambientTurn++;
        ambientTurnStart = Time.time;
        // A varied but deterministic cadence: mostly statements, some questions and exclamations.
        ambientMood = ambientTurn % 5 == 2 ? ConversationMotion.LineMood.Question
            : ambientTurn % 7 == 3 ? ConversationMotion.LineMood.Exclaim : ConversationMotion.LineMood.Statement;
    }

    /// <summary>Lets a mover start the walk before its displacement is detectable.</summary>
    public void SetLocomotion(bool walking)
    {
        locomotionRequested = walking;
    }

    /// <summary>Walks at the slower stroll pace (patrols, pacing) instead of the purposeful walk.</summary>
    public void SetLocomotion(bool walking, bool stroll)
    {
        bool paceChanged = stroll != strollRequested;
        strollRequested = stroll;
        locomotionRequested = walking;
        if (paceChanged && isMoving && !ownsCurrentConversation && !(stateMachine != null && stateMachine.CurrentState is NpcDialogueState))
            ApplyBehavior(Behavior.Walking);
    }

    private void MeasureWalkSpeed()
    {
        if (!AnimatorOwnsPose || !isMoving) return;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        int hash = state.shortNameHash;
        if (hash != GetStateHash(Behavior.Walking) && (strollStateHash == 0 || hash != strollStateHash)) return;
        if (!locomotionSpeeds.TryGetValue(hash, out float speed))
        {
            animator.GetCurrentAnimatorClipInfo(animatorLayer, clipInfos);
            if (clipInfos.Count == 0 || clipInfos[0].clip == null) return;
            Vector3 velocity = clipInfos[0].clip.averageSpeed;
            velocity.y = 0f;
            // Humanoid root motion is stored normalized to the avatar's size.
            float scale = (animator.isHuman ? animator.humanScale : 1f) * animator.transform.lossyScale.y;
            speed = velocity.magnitude * scale * Mathf.Abs(state.speed * state.speedMultiplier);
            clipInfos.Clear();
            locomotionSpeeds[hash] = speed;
        }
        AnimatedWalkSpeed = speed * Mathf.Max(0.01f, animator.speed);
    }

    private bool AnimatorOwnsPose =>
        animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;

    /// <summary>Requests an optional configured behavior for dialogue or gameplay presentation.</summary>
    public void SetBehavior(Behavior behavior)
    {
        requestedBehavior = behavior;
        if (isMoving && !ownsCurrentConversation && !(stateMachine != null && stateMachine.CurrentState is NpcDialogueState))
        {
            ApplyBehavior(Behavior.Walking);
            return;
        }

        ApplyBehavior(requestedBehavior);
    }

    private void ApplyBehavior(Behavior behavior)
    {
        if (animator == null)
            return;

        if (animatorLayer < 0 || animatorLayer >= animator.layerCount || animator.runtimeAnimatorController == null)
            return;

        int hash = GetStateHash(behavior);
        bool stroll = behavior == Behavior.Walking && strollRequested && strollStateHash != 0 &&
                      animator.HasState(animatorLayer, strollStateHash);
        if (stroll)
            hash = strollStateHash;
        if (hash == 0 || (!stroll && !HasState(behavior, hash)))
        {
            hash = idleStateHash;
            if (hash == 0 || !animator.HasState(animatorLayer, hash))
                return;
        }

        // Inactive Animators warn on CrossFade, and re-entering the playing
        // state would restart the idle loop with a visible pop.
        if (hash == activeStateHash || !animator.isActiveAndEnabled)
            return;

        activeStateHash = hash;
        if (animator.GetCurrentAnimatorStateInfo(animatorLayer).shortNameHash == hash)
            return;

        animator.CrossFadeInFixedTime(hash, transitionSeconds, animatorLayer);
    }

    private void HandleNpcStateChanged(INpcState state)
    {
        if (state is NpcWaitingState)
            stateBehavior = Behavior.Waiting;
        else if (state is NpcInteractState)
            stateBehavior = Behavior.Interacting;
        else if (state is NpcDialogueState)
            stateBehavior = Behavior.Listening;
        else
            stateBehavior = Behavior.Idle;

        requestedBehavior = stateBehavior;
        if (!isMoving || state is NpcDialogueState)
            ApplyBehavior(stateBehavior);
        if (ownsCurrentConversation)
            SyncWithActiveDialogue();
    }

    private void CacheReferences()
    {
        if (stateMachine == null)
            stateMachine = GetComponent<NpcStateMachine>();

        if (animator == null)
        {
            // Imported FBX instances carry an Animator even without a
            // controller; prefer the one that can actually play states.
            foreach (Animator candidate in GetComponentsInChildren<Animator>(true))
            {
                if (animator == null)
                    animator = candidate;
                if (candidate.runtimeAnimatorController != null)
                {
                    animator = candidate;
                    break;
                }
            }
        }

        if (dialogueUI == null)
            dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
    }

    /// <summary>
    /// A runtime-built NPC (Level 2's town and Rosa): the controller state names a scene NPC sets in the
    /// Inspector (idle, walk, stroll), given once after AddComponent. The animator plays the idle next frame.
    /// </summary>
    public void ConfigureStates(string idle, string walk, string stroll)
    {
        idleStateName = idle;
        strollStateName = stroll;
        stateBindings = string.IsNullOrWhiteSpace(walk)
            ? null : new[] { new StateBinding { behavior = Behavior.Walking, stateName = walk } };
        animator = null;
        CacheReferences();
        CacheStateNames();
        activeStateHash = 0;
    }

    private void CacheStateNames()
    {
        stateHashes.Clear();
        availableStates.Clear();
        idleStateHash = string.IsNullOrWhiteSpace(idleStateName)
            ? 0
            : Animator.StringToHash(idleStateName);
        strollStateHash = string.IsNullOrWhiteSpace(strollStateName) ? 0 : Animator.StringToHash(strollStateName);
        locomotionSpeeds.Clear();

        if (stateBindings == null)
            return;

        for (int i = 0; i < stateBindings.Length; i++)
        {
            string name = stateBindings[i].stateName;
            if (!string.IsNullOrWhiteSpace(name))
                stateHashes[stateBindings[i].behavior] = Animator.StringToHash(name);
        }
    }

    private int GetStateHash(Behavior behavior)
    {
        if (stateHashes.TryGetValue(behavior, out int hash))
            return hash;

        if (behavior == Behavior.Idle)
            return idleStateHash;

        return 0;
    }

    private bool HasState(Behavior behavior, int hash)
    {
        if (availableStates.TryGetValue(behavior, out bool available))
            return available;

        available = animator != null && animatorLayer >= 0 && animatorLayer < animator.layerCount &&
            animator.runtimeAnimatorController != null && animator.HasState(animatorLayer, hash);
        availableStates[behavior] = available;
        return available;
    }

    private void OnValidate()
    {
        transitionSeconds = Mathf.Max(0f, transitionSeconds);
        movementThreshold = Mathf.Max(0f, movementThreshold);
        activeStateHash = 0;
        if (Application.isPlaying)
            CacheStateNames();
    }

    private void HandleDialogueStarted()
    {
        SyncWithActiveDialogue();
    }

    private void HandleDialogueLineChanged(DialogueLine line)
    {
        if (!IsCurrentDialogueOwner())
        {
            ownsCurrentConversation = false;
            return;
        }

        ownsCurrentConversation = true;
        if (line == null)
            return;

        lineIndex++;
        lineStartTime = Time.time;
        lineRevealed = false;
        lineMood = ConversationMotion.MoodOf(line.Text);
        currentAction = IdleAction.None;
        SetBehavior(line.Speaker == DialogueSpeaker.Player ? Behavior.Listening : Behavior.Talking);
    }

    private void HandleDialogueLineRevealed(DialogueLine line)
    {
        if (!ownsCurrentConversation || lineRevealed)
            return;
        lineRevealed = true;
        revealTime = Time.time;
    }

    private void HandleDialogueEnded(DialogueEndReason reason)
    {
        if (!ownsCurrentConversation)
            return;

        ownsCurrentConversation = false;
        // A finished conversation gets a farewell nod; a cancelled one simply
        // settles. Idle actions resume after a short pause either way.
        float now = Time.time;
        if (reason == DialogueEndReason.Completed)
            farewellStart = now;
        currentAction = IdleAction.None;
        nextActionTime = now + UnityEngine.Random.Range(1.5f, 3f);
        if (stateMachine != null && stateMachine.CurrentState != null)
            HandleNpcStateChanged(stateMachine.CurrentState);
        else
            ApplyBehavior(stateBehavior);
    }

    private void SyncWithActiveDialogue()
    {
        if (!IsCurrentDialogueOwner())
            return;

        ownsCurrentConversation = true;
        if (dialogueUI.CurrentLine != null)
            HandleDialogueLineChanged(dialogueUI.CurrentLine);
    }

    private bool IsCurrentDialogueOwner()
    {
        if (dialogueUI == null || !dialogueUI.IsDialogueActive)
            return false;

        Component owner = dialogueUI.CurrentOwner as Component;
        return owner != null && (owner.transform == transform || owner.transform.IsChildOf(transform));
    }

    private void CacheHeadParts()
    {
        var parts = new List<Transform>();
        Transform head = null;
        foreach (MeshRenderer mesh in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (mesh.GetComponentInParent<Canvas>() != null) continue;
            string partName = mesh.name.ToLowerInvariant();
            if (partName == "head") head = mesh.transform;
            int suffix = partName.IndexOf('.');
            if (suffix >= 0) partName = partName.Substring(0, suffix);
            if (partName == "head" || partName == "face" || partName == "hair" ||
                partName == "eyes" || partName == "ears" || partName == "mouth" ||
                partName == "glasses" || partName == "beard" || partName == "stache" || partName == "stuache")
                parts.Add(mesh.transform);
        }
        if (headParts != null && headParts.Length > 0)
        {
            parts.Clear();
            foreach (Transform part in headParts)
                if (part != null && part != transform && part.IsChildOf(transform) && !parts.Contains(part))
                    parts.Add(part);
        }
        if (parts.Count == 0) return;
        Bounds bounds = default;
        bool measured = false;
        foreach (Transform part in parts)
        {
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer == null) continue;
            if (head != null && part != head) continue;
            if (!measured) { bounds = renderer.bounds; measured = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!measured) return;
        // Head parts follow the body container when they live under it, so
        // breathing/sway and the head motion compose instead of fighting.
        headSpace = transform;
        if (bodyCached)
        {
            bool allUnderBody = true;
            foreach (Transform part in parts)
                if (!part.IsChildOf(bodyRoot)) { allUnderBody = false; break; }
            if (allUnderBody) headSpace = bodyRoot;
        }
        // Rotate around the base of the head, not each Blender mesh's origin.
        headPivot = headSpace.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
        var poses = new List<HeadPose>();
        foreach (Transform part in parts)
        {
            // Physics and nested part hierarchies stay with their existing owner.
            if (part.GetComponentInChildren<Collider>(true) != null) continue;
            bool nested = false;
            foreach (Transform ancestor in parts)
                if (ancestor != part && part.IsChildOf(ancestor)) { nested = true; break; }
            if (nested) continue;
            poses.Add(new HeadPose {
                part = part, localPosition = part.localPosition, localRotation = part.localRotation,
                rootPosition = headSpace.InverseTransformPoint(part.position),
                rootRotation = Quaternion.Inverse(headSpace.rotation) * part.rotation
            });
        }
        headPoses = poses.ToArray();
    }

    private void CacheBody()
    {
        if (bodyRoot == null)
        {
            Transform visual = transform.Find("Visual");
            bodyRoot = visual != null ? visual : transform.Find("Visuals");
        }
        // Moving the root would move colliders and interaction targets.
        if (bodyRoot == null || bodyRoot == transform || !bodyRoot.IsChildOf(transform)) return;

        Bounds bounds = default;
        bool measured = false;
        foreach (Renderer renderer in bodyRoot.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled) continue;
            if (!measured) { bounds = renderer.bounds; measured = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!measured) return;

        Transform parent = bodyRoot.parent;
        bodyPivot = parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
        bodyRestPosition = bodyRoot.localPosition;
        bodyRestRotation = bodyRoot.localRotation;
        bodyRestScale = bodyRoot.localScale;
        bodyCached = true;
    }

    private enum IdleAction { None, LookAround, WeightShift, LookDown, LookUp, GlancePlayer }

    private void ScheduleIdleAction(float now)
    {
        if (currentAction != IdleAction.None && now < actionStart + actionDuration)
            return;
        if (currentAction != IdleAction.None)
        {
            currentAction = IdleAction.None;
            nextActionTime = now + UnityEngine.Random.Range(idleActionInterval.x, Mathf.Max(idleActionInterval.x, idleActionInterval.y));
        }
        if (now < nextActionTime || ownsCurrentConversation || ambientPartner != null)
            return;

        IdleAction next;
        do next = (IdleAction)UnityEngine.Random.Range(1, 6);
        while (next == lastAction);
        lastAction = currentAction = next;
        actionStart = now;
        actionDuration = UnityEngine.Random.Range(2.2f, 3.6f) / tempo;
        actionSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
    }

    /// <summary>
    /// Shared idle choreography in character space: continuous breathing,
    /// sway and head drift, plus an occasional randomized action.
    /// Euler angles: x = pitch (down), y = yaw, z = roll.
    /// </summary>
    private void ComputeIdlePose(float t, float now, out Vector3 head, out Vector3 body)
    {
        head = new Vector3(
            Mathf.Sin(t * 1.1f * tempo) * 1.2f,
            (Mathf.PerlinNoise(motionSeed, t * 0.15f) - 0.5f) * idleGlanceDegrees,
            Mathf.Sin(t * 0.7f * tempo)) * amplitude;
        body = new Vector3(
            Mathf.Sin(t * 0.31f * tempo + 1.3f) * bodySwayDegrees * 0.5f,
            0f,
            Mathf.Sin(t * 0.45f * tempo) * bodySwayDegrees) * amplitude;

        if (ownsCurrentConversation)
        {
            ComputeConversationPose(t, now, ref head, ref body);
            return;
        }

        if (ambientPartner != null)
        {
            // Background chat: face the partner, alternate talking and listening (no text).
            float yaw = PartnerYaw(conversationTurnLimit + maxConversationBodyTurn, float.MaxValue, 0f, ambientPartner);
            float headYaw = Mathf.Clamp(yaw, -conversationTurnLimit, conversationTurnLimit);
            head = new Vector3(head.x * 0.5f, headYaw, head.z * 0.5f);
            body = new Vector3(body.x * 0.5f, yaw - headYaw, body.z * 0.5f);
            float sinceTurn = now - ambientTurnStart;
            ConversationMotion.Apply(ambientTalking, t, sinceTurn, sinceTurn > 1.8f ? sinceTurn - 1.8f : -1f, ambientMood,
                ambientTurn, motionSeed, headMotionDegrees, conversationStrength * 0.8f, ref head, ref body);
            return;
        }

        if (farewellStart >= 0f)
        {
            float since = now - farewellStart;
            if (since > 0.9f) farewellStart = -1f;
            else head.x += 8f * ConversationMotion.Beat(since, 0.9f) * conversationStrength;
        }

        if (currentAction == IdleAction.None)
            return;
        float p = Mathf.Clamp01((now - actionStart) / actionDuration);
        float e = Mathf.SmoothStep(0f, 1f, Mathf.Min(p / 0.3f, 1f)) *
            Mathf.SmoothStep(0f, 1f, Mathf.Min((1f - p) / 0.3f, 1f)) * idleActionStrength * amplitude;
        float s = actionSign;
        switch (currentAction)
        {
            case IdleAction.LookAround:
                head += new Vector3(-2f, 30f * s, 3f * s) * e;
                body += new Vector3(0f, 0f, 1f * s) * e;
                break;
            case IdleAction.WeightShift:
                head += new Vector3(0f, 8f * s, -4f * s) * e;
                body += new Vector3(1f, 0f, 4.5f * s) * e;
                break;
            case IdleAction.LookDown:
                head += new Vector3(18f, 6f * s, 0f) * e;
                body += new Vector3(3f, 0f, 0f) * e;
                break;
            case IdleAction.LookUp:
                // Stretch-like look up that settles into a slow nod.
                head += new Vector3(-12f + Mathf.Max(0f, p - 0.6f) * 30f, 0f, 2f * s) * e;
                body += new Vector3(-2.5f, 0f, 0f) * e;
                break;
            case IdleAction.GlancePlayer:
                head += new Vector3(-1f, PartnerYaw(40f, 9f, 30f * s), 2f * s) * e;
                break;
        }
    }

    /// <summary>
    /// Procedural talk/listen fallback (shared ConversationMotion), layered on
    /// whatever idle clip plays.
    /// </summary>
    private void ComputeConversationPose(float t, float now, ref Vector3 head, ref Vector3 body)
    {
        // The head turns first; the upper body takes the rest of the turn.
        float yaw = PartnerYaw(conversationTurnLimit + maxConversationBodyTurn);
        float headYaw = Mathf.Clamp(yaw, -conversationTurnLimit, conversationTurnLimit);
        head = new Vector3(head.x * 0.5f, headYaw, head.z * 0.5f);
        body = new Vector3(body.x * 0.5f, yaw - headYaw, body.z * 0.5f);

        ConversationMotion.Apply(requestedBehavior == Behavior.Talking, t, now - lineStartTime,
            lineRevealed ? now - revealTime : -1f, lineMood, lineIndex, motionSeed,
            headMotionDegrees, conversationStrength, ref head, ref body);
    }

    /// <summary>Head yaw toward the player, clamped; fallback when out of range.</summary>
    /// <summary>
    /// Imported models are often rotated inside the NPC root (Plaza NPCs by
    /// about 141 degrees), so the root's forward is not where the face points.
    /// The model root's forward is the character's facing.
    /// </summary>
    public Quaternion FacingOffset { get; private set; } = Quaternion.identity;

    private Quaternion CharacterRotation => transform.rotation * FacingOffset;

    private void CacheFacing()
    {
        Transform model = animator != null ? animator.transform : null;
        if (model == null && bodyRoot != null)
            foreach (Transform child in bodyRoot)
                if (child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null ||
                    child.childCount > 0) { model = child; break; }
        if (model == null || model == transform) return;
        Vector3 forward = Vector3.ProjectOnPlane(model.forward, transform.up);
        if (forward.sqrMagnitude < 0.0001f) return;
        float yaw = Vector3.SignedAngle(transform.forward, forward, transform.up);
        FacingOffset = Quaternion.Euler(0f, yaw, 0f);
    }

    private float PartnerYaw(float limit, float maxDistance = float.MaxValue, float fallback = 0f, Transform partner = null)
    {
        if (partner == null) partner = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (partner == null) return fallback;
        Vector3 toPartner = Quaternion.Inverse(CharacterRotation) * (partner.position - transform.position);
        toPartner.y = 0f;
        if (toPartner.magnitude > maxDistance) return fallback;
        return Mathf.Clamp(Mathf.Atan2(toPartner.x, toPartner.z) * Mathf.Rad2Deg, -limit, limit);
    }

    private void LateUpdate()
    {
        bool rigged = AnimatorOwnsPose;
        // A model revealed later (Arthur) had an inactive Animator at Awake: find its bones once it plays.
        if (rigged && animateRigBones && headBone == null && !bonesCachedLive)
        {
            bonesCachedLive = true;
            CacheBones();
        }
        bool bodyActive = !rigged && animateStaticBody && bodyCached;
        bool headActive = !rigged && animateStaticHead && headPoses != null;
        bool bonesActive = rigged && animateRigBones && headBone != null;
        if (!bodyActive && !headActive && !bonesActive)
        {
            if (headWasMoved) RestoreHead();
            return;
        }

        float deltaTime = Time.deltaTime;
        float now = Time.time;
        motionClock += deltaTime;
        ScheduleIdleAction(now);
        ComputeIdlePose(motionClock, now, out Vector3 headTarget, out Vector3 bodyTarget);
        float k = 1f - Mathf.Exp(-(ownsCurrentConversation ? 10f : 6f) * deltaTime);
        smoothedHead = Vector3.Lerp(smoothedHead, headTarget, k);
        smoothedBody = Vector3.Lerp(smoothedBody, bodyTarget, k);

        if (bonesActive)
        {
            // Runs after the Animator wrote this frame's pose, so offsets
            // layer on top of the idle clip without accumulating.
            float spineShare = chestBone != null ? 0.5f : 1f;
            RotateBone(spineBone, smoothedBody * spineShare);
            RotateBone(chestBone, smoothedBody * (1f - spineShare));
            RotateBone(neckBone, smoothedHead * 0.35f);
            RotateBone(headBone, smoothedHead * (neckBone != null ? 0.65f : 1f));
            return;
        }

        if (bodyActive)
        {
            float breath = Mathf.Sin(motionClock * 1.7f * tempo) * breathingScale * amplitude;
            // Lean in the character's frame; bodyRoot's parent is the NPC root.
            Quaternion sway = FacingOffset * Quaternion.Euler(smoothedBody) * Quaternion.Inverse(FacingOffset);
            bodyRoot.localPosition = bodyPivot + sway * (bodyRestPosition - bodyPivot);
            bodyRoot.localRotation = sway * bodyRestRotation;
            bodyRoot.localScale = new Vector3(bodyRestScale.x, bodyRestScale.y * (1f + breath), bodyRestScale.z);
        }

        if (headActive)
        {
            // The pose is in character space; express it in the head container.
            // The head turns relative to the (possibly turned) body.
            Quaternion character = CharacterRotation * Quaternion.Euler(0f, smoothedBody.y, 0f);
            Quaternion offset = Quaternion.Inverse(headSpace.rotation) * character *
                Quaternion.Euler(smoothedHead) * Quaternion.Inverse(character) * headSpace.rotation;
            foreach (HeadPose pose in headPoses)
            {
                if (pose.part == null) continue;
                pose.part.SetPositionAndRotation(headSpace.TransformPoint(headPivot + offset * (pose.rootPosition - headPivot)),
                    headSpace.rotation * offset * pose.rootRotation);
            }
        }
        headWasMoved = true;
    }

    private void RotateBone(Transform bone, Vector3 euler)
    {
        if (bone == null) return;
        Quaternion root = CharacterRotation;
        bone.rotation = root * Quaternion.Euler(euler) * Quaternion.Inverse(root) * bone.rotation;
    }

    private void CacheBones()
    {
        if (animator == null || !animator.isHuman) return;
        headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        neckBone = animator.GetBoneTransform(HumanBodyBones.Neck);
        spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);
        chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
        // Offscreen culling would skip the pose write and let offsets pile up.
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void RestoreHead()
    {
        if (headPoses != null)
            foreach (HeadPose pose in headPoses)
                if (pose.part != null)
                {
                    pose.part.localPosition = pose.localPosition;
                    pose.part.localRotation = pose.localRotation;
                }
        if (bodyCached && bodyRoot != null)
        {
            bodyRoot.localPosition = bodyRestPosition;
            bodyRoot.localRotation = bodyRestRotation;
            bodyRoot.localScale = bodyRestScale;
        }
        smoothedHead = smoothedBody = Vector3.zero;
        headWasMoved = false;
    }
}
