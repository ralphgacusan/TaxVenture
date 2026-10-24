using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The end-of-level scene for the case client (Level 1: Marie). After the
/// Audit Summary of the last case, the screen fades to black and opens on
/// the conference room: the client stands on the LEFT of the screen in a
/// waist-up shot (the same 3D model as in the plaza), talking, with a
/// speech bubble over her head; the Level Complete card (unchanged) sits on
/// the RIGHT half (HudRewardScreen.PreferSideLayout).
///
/// Her lines depend on how the case went (CaseFinaleRules): first try and
/// on time = great; one slip = good with a little "but"; more = okay but
/// still thankful. Lines and staging live in the case JSON ("clientFinale").
///
/// Presentation only: its own camera (above the main one) and a visual-only
/// copy of the plaza model (gameplay scripts and colliders stripped before
/// they ever run). Everything is removed when the card closes.
///
/// T08: in a story run Marie first receives her Client Copy in person
/// (ClientCopyHandover). The scene then waits for the handover and plays
/// afterwards with her farewell lines instead (she said her closing lines in
/// the conversation). Each line may carry an emote, shown on the bubble.
/// </summary>
public sealed class CaseFinale : MonoBehaviour
{

    public static CaseFinale Instance { get; private set; }

    /// <summary>True from the moment the finale is decided until it is cleaned up.</summary>
    public bool Running { get; private set; }

    private bool cardReleased = true;
    private bool finished;
    // T08: after the handover she has already said her closing lines; she says goodbye instead.
    private bool quietLines;

    /// <summary>
    /// True when the current case will end with the client scene (so the
    /// Level Complete card must wait for it), even before the finale starts.
    /// </summary>
    public static bool WillStage => Instance != null && Instance.WouldStage();

    /// <summary>False while the finale wants the Level Complete card hidden.</summary>
    public static bool CardReleased => Instance == null || Instance.cardReleased;

    /// <summary>True while the finale is running and has not yet let the Level Complete card show.</summary>
    public static bool HoldsCards => Instance != null && Instance.Running && !Instance.cardReleased;

    private bool WouldStage()
    {
        if (finished) return false;
        if (Running) return true;
        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (def == null || def.clientFinale == null || !def.clientFinale.enabled) return false;
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        return progression == null || progression.CurrentCaseNumber >= progression.TotalCasesInLevel;
    }

    private ClientFinaleDefinition settings;
    private GameObject clone;
    private Camera finaleCamera;
    private GameObject uiHost;
    private CanvasGroup black;
    private RectTransform bubble;
    private CanvasGroup bubbleGroup;
    private TextMeshProUGUI bubbleText;
    private TextMeshProUGUI bubbleName;
    private RectTransform emoteBadge;
    private Image emoteOutline;
    private Image emoteFill;
    private Transform headBone;
    private Transform spineBone;
    private Quaternion facingOffset = Quaternion.identity;
    private float height = 1.7f;
    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private readonly List<Behaviour> disabledMarkers = new List<Behaviour>();

    // Talking state for the procedural motion.
    private bool speaking;
    private float lineStart;
    private float revealAt = -1f;
    private int lineIndex;
    private ConversationMotion.LineMood mood;
    private float revealed;
    private int totalChars;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeForLoadedScenes()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            EnsureForScene(SceneManager.GetSceneAt(i));
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureForScene(scene);

    private static void EnsureForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !OfficeLevelProfile.Allows(scene, OfficeFeature.Finale)) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<CaseFinale>(true) != null) return;
        var host = new GameObject("Case Finale");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<CaseFinale>();
    }

    private void Awake() => Instance = this;

    private void OnEnable() => GameplayEvents.OnCaseCompleted += HandleCaseCompleted;

    private void OnDisable() => GameplayEvents.OnCaseCompleted -= HandleCaseCompleted;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Cleanup();
    }

    // =========================================================
    // START
    // =========================================================

    private void HandleCaseCompleted()
    {
        // T08: the Client Copy handover comes first; it plays this scene when Marie has her copy.
        if (ClientCopyHandover.Defers) return;
        Begin(quiet: false);
    }

    /// <summary>T08: Marie has her copy; the scene and the result card follow, with her farewell lines.</summary>
    public void PlayAfterHandover()
    {
        finished = false;
        Begin(quiet: true);
    }

    private void Begin(bool quiet)
    {
        if (Running) return;
        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        settings = def != null ? def.clientFinale : null;
        if (settings == null || !settings.enabled) return;

        // Only when this case ends the level (the Level Complete card follows).
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        if (progression != null && progression.CurrentCaseNumber < progression.TotalCasesInLevel) return;

        quietLines = quiet;
        Running = true;
        cardReleased = false;
        HudRewardScreen.PreferSideLayout = true;
        HudRewardScreen.HoldReveal = true;
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        BuildUi();
        yield return Fade(0f, 1f, 0.35f);

        // While the screen is dark: stage the conference room.
        bool staged = false;
        try { staged = Stage(); }
        catch (Exception e) { Debug.LogWarning($"[CaseFinale] Could not stage the client: {e.Message}"); }
        if (!staged)
        {
            // Nothing to show: the card appears once the black is gone.
            HudRewardScreen.PreferSideLayout = false;
            yield return Fade(1f, 0f, 0.35f);
            ReleaseCard();
            Cleanup();
            yield break;
        }

        yield return WaitReal(0.35f);
        // The room fades in; halfway through, the Level Complete card starts
        // its own pop-in animation.
        yield return Fade(1f, 0.5f, 0.4f);
        ReleaseCard();
        yield return Fade(0.5f, 0f, 0.4f);
        yield return WaitReal(0.4f);

        // Her reaction, one line after another. After the handover she said her
        // closing lines in person, so beside the card she says goodbye instead.
        CaseFinaleRules.Tier tier = TierNow();
        List<DialogueLineDefinition> lines = quietLines ? FarewellLines(settings, tier) : WithTierEmote(ClosingLines(settings, tier), tier);
        for (int i = 0; i < lines.Count; i++)
        {
            Say(i, lines[i].text, lines[i].emote);
            while (revealed < totalChars) yield return null;
            yield return WaitReal(2.6f);
        }
        speaking = false;

        // Stay until the card is closed (Continue leaves the scene).
        float seenCardUntil = Time.unscaledTime + 4f;
        while (HudRewardScreen.AnyOpen || Time.unscaledTime < seenCardUntil) yield return null;
        Cleanup();
    }

    /// <summary>How the current case went (first try and on time / one slip / more), for her lines.</summary>
    public static CaseFinaleRules.Tier TierNow()
    {
        // A review case (Level 2) knows its own returned submissions and cutoff.
        CaseReviewState review = CaseManager.Instance != null && CaseManager.Instance.CurrentCase != null
            ? CaseManager.Instance.CurrentCase.review : null;
        if (review != null)
        {
            bool reviewOnTime = review.clock == null || !review.clock.missed;
            return CaseFinaleRules.TierFor(review.audit != null ? review.audit.failedSubmissions : 0, reviewOnTime,
                review.clock != null ? Mathf.Max(1, review.clock.day) : 1);
        }
        AuditorInteractable auditor = FindAnyObjectByType<AuditorInteractable>(FindObjectsInactive.Include);
        int failed = auditor != null ? auditor.FailedAudits : 0;
        OfficeHours hours = OfficeHours.Instance;
        bool timed = hours != null && hours.Announced;
        bool onTime = !timed || hours.OnTime;
        int day = timed ? hours.Day : 1;
        return CaseFinaleRules.TierFor(failed, onTime, day);
    }

    /// <summary>Her lines for a result: the case JSON's great / good / okay, or built-in ones.</summary>
    public static List<string> ClosingLines(ClientFinaleDefinition settings, CaseFinaleRules.Tier tier)
    {
        if (settings == null) settings = new ClientFinaleDefinition();
        List<string> chosen = tier == CaseFinaleRules.Tier.Great ? settings.great
            : tier == CaseFinaleRules.Tier.Good ? settings.good : settings.okay;
        if (chosen != null && chosen.Count > 0) return chosen;
        switch (tier)
        {
            case CaseFinaleRules.Tier.Great:
                return new List<string> { "You did it! First try, and right on time!", "My file is all sorted. Thank you so much!" };
            case CaseFinaleRules.Tier.Good:
                return new List<string> { "My file's approved! Thank you.", "It took a little extra work, but you got there!" };
            default:
                return new List<string> { "Phew... it's finally approved.", "That was a bit of a journey, but thank you for sticking with it." };
        }
    }

    /// <summary>Her goodbye beside the card after the handover: the case JSON's farewell lines for a result, or built-in ones.</summary>
    public static List<DialogueLineDefinition> FarewellLines(ClientFinaleDefinition settings, CaseFinaleRules.Tier tier)
    {
        if (settings == null) settings = new ClientFinaleDefinition();
        List<DialogueLineDefinition> chosen = tier == CaseFinaleRules.Tier.Great ? settings.farewellGreat
            : tier == CaseFinaleRules.Tier.Good ? settings.farewellGood : settings.farewellOkay;
        var lines = new List<DialogueLineDefinition>();
        if (chosen != null)
            foreach (DialogueLineDefinition line in chosen)
                if (line != null && !string.IsNullOrWhiteSpace(line.text)) lines.Add(line);
        if (lines.Count > 0) return lines;
        switch (tier)
        {
            case CaseFinaleRules.Tier.Great:
                lines.Add(Line("Thank you again! I'll keep this copy somewhere safe.", "heart"));
                lines.Add(Line("I'm telling everyone at the plaza about you!", "sparkles"));
                break;
            case CaseFinaleRules.Tier.Good:
                lines.Add(Line("Thank you for walking me through it.", "heart"));
                lines.Add(Line("I feel so much lighter now. See you next filing!", "music"));
                break;
            default:
                lines.Add(Line("Thank you for not giving up on my file.", "sweat"));
                lines.Add(Line("I'll come by early next time. Promise!", "music"));
                break;
        }
        return lines;
    }

    /// <summary>The in-person closing lines (no handover) with the result's emote on the first one.</summary>
    private static List<DialogueLineDefinition> WithTierEmote(List<string> texts, CaseFinaleRules.Tier tier)
    {
        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        ClientHandoverDefinition handover = def != null && def.clientHandover != null ? def.clientHandover : new ClientHandoverDefinition();
        string emote = tier == CaseFinaleRules.Tier.Great ? handover.greatEmote
            : tier == CaseFinaleRules.Tier.Good ? handover.goodEmote : handover.okayEmote;
        var lines = new List<DialogueLineDefinition>();
        for (int i = 0; i < texts.Count; i++) lines.Add(Line(texts[i], i == 0 ? emote : null));
        return lines;
    }

    private static DialogueLineDefinition Line(string text, string emote) =>
        new DialogueLineDefinition { speaker = "Npc", text = text, emote = emote };

    // =========================================================
    // STAGING
    // =========================================================

    private bool Stage()
    {
        Scene scene = gameObject.scene;
        Transform original = FindInScene(scene, settings.modelPath);
        Transform spot = FindInScene(scene, settings.spotPath);
        Camera main = GuideTargets.ViewCamera;
        if (original == null || spot == null || main == null)
        {
            Debug.LogWarning("[CaseFinale] Client model, spot or camera not found; finale skipped.");
            return false;
        }

        clone = MakeVisualCopy(original.gameObject, scene);
        if (clone == null) return false;
        // T08: after the handover the real Marie stands at this spot; she steps aside for her copy's shot.
        foreach (Renderer r in original.GetComponentsInChildren<Renderer>())
            if (r.enabled) { r.enabled = false; hiddenRenderers.Add(r); }
        foreach (NpcStatusMarker m in original.GetComponentsInChildren<NpcStatusMarker>())
            if (m.enabled) { m.enabled = false; disabledMarkers.Add(m); }

        // The real conference client (and its marker) steps aside for the shot.
        foreach (Renderer r in spot.GetComponentsInChildren<Renderer>())
            if (r.enabled) { r.enabled = false; hiddenRenderers.Add(r); }
        foreach (NpcStatusMarker m in spot.GetComponentsInChildren<NpcStatusMarker>())
            if (m.enabled) { m.enabled = false; disabledMarkers.Add(m); }

        Vector3 roomCenter = TryBounds(FindInScene(scene, settings.roomPath), out Bounds room) ? room.center : spot.position + spot.forward * 2f;

        // The room's floor level (never the roof above it or a table top).
        float floorTop = TryBounds(FindInScene(scene, settings.floorPath), out Bounds floors) ? floors.max.y
            : room.size.y > 0f ? room.min.y : spot.position.y - 1f;
        if (!FindStandingSpot(spot, floorTop, out Vector3 feet)) feet = new Vector3(spot.position.x, floorTop, spot.position.z);
        Vector3 toRoom = roomCenter - feet;
        toRoom.y = 0f;
        if (toRoom.sqrMagnitude < 0.01f) toRoom = spot.forward;
        toRoom.Normalize();

        // Stand her visible feet exactly on the floor (whatever the model's pivot).
        clone.transform.position = feet;
        CacheModel();
        TryBounds(clone.transform, out Bounds body);
        clone.transform.position += Vector3.up * (feet.y - body.min.y);
        TryBounds(clone.transform, out body);
        height = Mathf.Max(0.8f, body.size.y);

        // Camera: from the room toward her, waist-up, with her on the left.
        finaleCamera = new GameObject("Case Finale Camera").AddComponent<Camera>();
        SceneManager.MoveGameObjectToScene(finaleCamera.gameObject, scene);
        finaleCamera.CopyFrom(main);
        finaleCamera.depth = main.depth + 5f;
        finaleCamera.fieldOfView = Mathf.Clamp(settings.fieldOfView, 20f, 70f);
        finaleCamera.nearClipPlane = 0.03f;
        finaleCamera.targetTexture = null;
        UniversalAdditionalCameraData mainData = main.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData data = finaleCamera.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Base;
        data.renderPostProcessing = mainData.renderPostProcessing;
        data.antialiasing = mainData.antialiasing;

        // Waist-up, a little farther back, with her head at about 70% of the
        // screen height so her speech bubble fits above it.
        float floor = body.min.y;
        float tanHalf = Mathf.Tan(finaleCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float visible = 0.55f * height / 0.65f;                     // waist (45%) .. head top (100%) fills 65% of the frame
        float baseDistance = Mathf.Max(0.5f * visible / tanHalf, 1.1f) * Mathf.Clamp(settings.distanceScale, 0.6f, 2f);
        Vector3 eye = Vector3.up * (floor + 0.45f * height + 0.45f * visible);
        float[] turns = { 0f, 20f, -20f, 40f, -40f, 60f, -60f };
        float[] scales = { 1f, 0.85f, 1.15f };
        foreach (float s in scales)
        foreach (float turn in turns)
        {
            Vector3 dir = Quaternion.Euler(0f, turn, 0f) * toRoom;
            Vector3 camPos = new Vector3(feet.x, 0f, feet.z) + dir * baseDistance * s + eye;
            Vector3 target = new Vector3(feet.x, eye.y, feet.z);
            if (Physics.CheckSphere(camPos, 0.18f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (LineBlocked(camPos, target, spot)) continue;
            PlaceShot(camPos, target, dir);
            return true;
        }

        // No clear angle found: use the first one anyway.
        Vector3 fallback = new Vector3(feet.x, 0f, feet.z) + toRoom * baseDistance + eye;
        PlaceShot(fallback, new Vector3(feet.x, eye.y, feet.z), toRoom);
        return true;
    }

    /// <summary>Faces her to the camera and aims the camera so she stands at settings.screenLeft.</summary>
    private void PlaceShot(Vector3 camPos, Vector3 target, Vector3 dirToCamera)
    {
        Quaternion face = Quaternion.LookRotation(dirToCamera);
        clone.transform.rotation = face * Quaternion.Inverse(facingOffset);

        float distance = Vector3.Distance(new Vector3(camPos.x, 0f, camPos.z), new Vector3(target.x, 0f, target.z));
        float aspect = Mathf.Max(0.5f, (float)Screen.width / Mathf.Max(1, Screen.height));
        float halfWidth = distance * Mathf.Tan(finaleCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect;
        Vector3 right = Vector3.Cross(Vector3.up, -dirToCamera).normalized;   // camera's right
        float shift = (0.5f - Mathf.Clamp(settings.screenLeft, 0.1f, 0.5f)) * 2f * halfWidth;
        Vector3 aim = target + right * shift;
        finaleCamera.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(aim - camPos));
    }

    /// <summary>A clear place to stand near the spot, on the room floor.</summary>
    private bool FindStandingSpot(Transform spot, float floorTop, out Vector3 feet)
    {
        Vector3 basePos = new Vector3(spot.position.x, floorTop, spot.position.z);
        float[] radii = { 0f, 0.6f, 1.0f, 1.4f };
        foreach (float r in radii)
        for (int a = 0; a < (r == 0f ? 1 : 8); a++)
        {
            Vector3 p = basePos + Quaternion.Euler(0f, a * 45f, 0f) * Vector3.forward * r;
            // From just above the floor: a table top or the roof is never "ground".
            p.y = Ground(new Vector3(p.x, floorTop + 0.3f, p.z), floorTop);
            if (Mathf.Abs(p.y - floorTop) > 0.3f) continue;
            if (!Blocked(p, spot)) { feet = p; return true; }
        }
        feet = basePos;
        return false;
    }

    private readonly RaycastHit[] lineHits = new RaycastHit[16];

    /// <summary>Something between the camera and her (the hidden real client at the spot doesn't count).</summary>
    private bool LineBlocked(Vector3 from, Vector3 to, Transform ignore)
    {
        Vector3 d = to - from;
        int n = Physics.RaycastNonAlloc(from, d.normalized, lineHits, d.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (ignore == null || !lineHits[i].transform.IsChildOf(ignore)) return true;
        return false;
    }

    private readonly Collider[] overlaps = new Collider[16];

    private bool Blocked(Vector3 feet, Transform ignore)
    {
        int n = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * 0.55f, feet + Vector3.up * 1.5f, 0.28f, overlaps,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Transform t = overlaps[i].transform;
            if (ignore != null && t.IsChildOf(ignore)) continue;
            Transform player = GuideTargets.Player;
            if (player != null && t.IsChildOf(player)) continue;
            return true;
        }
        return false;
    }

    private readonly RaycastHit[] hits = new RaycastHit[12];

    private float Ground(Vector3 from, float fallback)
    {
        int n = Physics.RaycastNonAlloc(from, Vector3.down, hits, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.MinValue;
        for (int i = 0; i < n; i++)
            if (hits[i].normal.y > 0.5f && hits[i].point.y > best) best = hits[i].point.y;
        return best > float.MinValue ? best : fallback;
    }

    /// <summary>
    /// A copy of the plaza model that is only a picture: built under an
    /// inactive parent so no script ever runs, then every script, collider
    /// and rigidbody is removed. The Animator stays (idle clip).
    /// </summary>
    private static GameObject MakeVisualCopy(GameObject original, Scene scene)
    {
        var holder = new GameObject("Finale Holder");
        SceneManager.MoveGameObjectToScene(holder, scene);
        holder.SetActive(false);
        GameObject copy = Instantiate(original, holder.transform);
        copy.name = original.name + " (Finale)";

        foreach (TaxpayerProfileWorldPickup pickup in copy.GetComponentsInChildren<TaxpayerProfileWorldPickup>(true))
            DestroyImmediate(pickup.gameObject);

        var scripts = new List<MonoBehaviour>(copy.GetComponentsInChildren<MonoBehaviour>(true));
        scripts.RemoveAll(m => m == null);
        for (int pass = 0; pass < 8 && scripts.Count > 0; pass++)
        {
            for (int i = scripts.Count - 1; i >= 0; i--)
            {
                MonoBehaviour m = scripts[i];
                if (m == null) { scripts.RemoveAt(i); continue; }
                if (RequiredByOther(m, scripts)) continue;
                DestroyImmediate(m);
                scripts.RemoveAt(i);
            }
        }
        foreach (Collider c in copy.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
        foreach (Rigidbody b in copy.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(b);
        foreach (AudioSource a in copy.GetComponentsInChildren<AudioSource>(true)) DestroyImmediate(a);

        copy.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(copy, scene);
        Destroy(holder);
        copy.SetActive(true);
        return copy;
    }

    private static bool RequiredByOther(MonoBehaviour target, List<MonoBehaviour> others)
    {
        Type type = target.GetType();
        foreach (MonoBehaviour other in others)
        {
            if (other == null || other == target || other.gameObject != target.gameObject) continue;
            foreach (RequireComponent req in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                if ((req.m_Type0 != null && req.m_Type0.IsAssignableFrom(type)) ||
                    (req.m_Type1 != null && req.m_Type1.IsAssignableFrom(type)) ||
                    (req.m_Type2 != null && req.m_Type2.IsAssignableFrom(type)))
                    return true;
        }
        return false;
    }

    /// <summary>Finds the head/spine bones and the model's facing inside the root (plaza models are rotated).</summary>
    private void CacheModel()
    {
        Animator animator = clone.GetComponentInChildren<Animator>(true);
        Transform model = animator != null ? animator.transform : null;
        if (model == null)
        {
            SkinnedMeshRenderer skin = clone.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin != null) model = skin.transform.parent;
        }
        if (model != null && model != clone.transform)
        {
            Vector3 f = Vector3.ProjectOnPlane(model.forward, Vector3.up);
            if (f.sqrMagnitude > 0.0001f)
                facingOffset = Quaternion.Euler(0f, Vector3.SignedAngle(clone.transform.forward, f, Vector3.up), 0f);
        }

        if (animator != null && animator.isHuman)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            spineBone = animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                        animator.GetBoneTransform(HumanBodyBones.Chest) ?? animator.GetBoneTransform(HumanBodyBones.Spine);
        }
        if (headBone == null) headBone = FindBone(clone.transform, "head");
        if (spineBone == null) spineBone = FindBone(clone.transform, "spine");
    }

    private static Transform FindBone(Transform root, string part)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0 &&
                t.name.IndexOf("end", StringComparison.OrdinalIgnoreCase) < 0) return t;
        return null;
    }

    // =========================================================
    // TALKING
    // =========================================================

    private void Say(int index, string text, string emoteTag)
    {
        ShowEmote(emoteTag);
        lineIndex = index;
        lineStart = Time.unscaledTime;
        revealAt = -1f;
        speaking = true;
        mood = ConversationMotion.MoodOf(text);
        bubbleText.text = text;
        bubbleText.maxVisibleCharacters = 0;
        bubbleText.ForceMeshUpdate();
        totalChars = bubbleText.textInfo.characterCount;
        revealed = 0f;
    }

    private void Update()
    {
        if (bubbleText == null || !speaking) return;
        if (revealed < totalChars)
        {
            revealed = Mathf.Min(totalChars, revealed + 42f * Time.unscaledDeltaTime);
            bubbleText.maxVisibleCharacters = Mathf.FloorToInt(revealed);
            if (revealed >= totalChars && revealAt < 0f) revealAt = Time.unscaledTime;
        }
    }

    private void LateUpdate()
    {
        if (clone == null || finaleCamera == null) return;
        float now = Time.unscaledTime;

        // Talk/listen choreography layered on the idle clip (same maths as conversations).
        Vector3 head = Vector3.zero, chest = Vector3.zero;
        ConversationMotion.Apply(speaking, now, now - lineStart, revealAt < 0f ? -1f : now - revealAt, mood,
            lineIndex, 3.7f, 6f, 1f, ref head, ref chest);
        Quaternion character = clone.transform.rotation * facingOffset;
        ConversationMotion.RotateBone(spineBone, character, chest);
        ConversationMotion.RotateBone(headBone, character, head);

        // Speech bubble over her head.
        if (bubble != null)
        {
            Vector3 top = headBone != null ? headBone.position + Vector3.up * 0.32f * height / 1.7f
                : clone.transform.position + Vector3.up * height * 1.05f;
            Vector3 sp = finaleCamera.WorldToScreenPoint(top);
            float scale = ((RectTransform)uiHost.transform).lossyScale.x;
            if (scale <= 0f) scale = 1f;
            Vector2 pos = new Vector2(sp.x, sp.y) / scale;
            Vector2 canvasSize = ((RectTransform)uiHost.transform).rect.size;
            Vector2 size = bubble.sizeDelta;
            pos.x = Mathf.Clamp(pos.x, size.x * bubble.pivot.x + 16f, canvasSize.x * 0.5f - size.x * (1f - bubble.pivot.x));
            pos.y = Mathf.Clamp(pos.y, 16f, canvasSize.y - size.y - 24f);
            bubble.anchoredPosition = pos;
            bubbleGroup.alpha = Mathf.MoveTowards(bubbleGroup.alpha, speaking || revealed > 0f ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            float pop = 1f + 0.04f * Mathf.Sin(Mathf.Clamp01((now - lineStart) / 0.3f) * Mathf.PI);
            bubble.localScale = Vector3.one * pop;
        }

        // Her emote on the bubble's corner: pops in with the line (0 -> 1.15 -> 1), then breathes gently.
        if (emoteBadge != null && emoteBadge.gameObject.activeSelf)
        {
            float t = now - lineStart;
            float enter = t < 0.2f ? Mathf.Lerp(0f, 1.15f, t / 0.2f) : t < 0.32f ? Mathf.Lerp(1.15f, 1f, (t - 0.2f) / 0.12f) : 1f;
            float idle = t < 0.32f ? 0f : 0.06f * Mathf.Sin((t - 0.32f) * 4f);
            emoteBadge.localScale = Vector3.one * (enter + idle);
        }
    }

    /// <summary>Shows the line's emote on the bubble (finale camera: a world emote cannot sit over her, CF-48).</summary>
    private void ShowEmote(string emoteTag)
    {
        if (emoteBadge == null) return;
        if (!EmoteCatalogue.TryParseTag(emoteTag, out Emotion emotion) || emotion == Emotion.None)
        {
            if (!string.IsNullOrWhiteSpace(emoteTag)) Debug.LogWarning($"[CaseFinale] Unknown emote tag '{emoteTag}'.");
            emoteBadge.gameObject.SetActive(false);
            return;
        }
        EmoteStyle style = EmoteCatalogue.Get(emotion);
        emoteOutline.sprite = EmoteSprites.Outline(style.Shape);
        emoteFill.sprite = EmoteSprites.Fill(style.Shape);
        emoteFill.color = new Color32(style.R, style.G, style.B, 255);
        emoteBadge.localScale = Vector3.zero;
        emoteBadge.gameObject.SetActive(true);
    }

    // =========================================================
    // UI: black fade and speech bubble
    // =========================================================

    private void BuildUi()
    {
        uiHost = new GameObject("Case Finale UI", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(uiHost, gameObject.scene);
        uiHost.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = uiHost.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6500;
        CanvasScaler scaler = uiHost.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        uiHost.AddComponent<HudKeepVisibleDuringRewards>();
        var root = (RectTransform)uiHost.transform;

        // Speech bubble (below the black layer in draw order).
        bubble = HudTheme.CreateRect("Speech Bubble", root);
        bubble.anchorMin = bubble.anchorMax = Vector2.zero;
        bubble.pivot = new Vector2(0.35f, 0f);
        bubble.sizeDelta = new Vector2(560f, 170f);
        bubbleGroup = bubble.gameObject.AddComponent<CanvasGroup>();
        bubbleGroup.alpha = 0f;
        bubbleGroup.blocksRaycasts = false;

        RectTransform tail = HudTheme.CreateRect("Tail", bubble);
        tail.anchorMin = tail.anchorMax = new Vector2(0.35f, 0f);
        tail.sizeDelta = new Vector2(30f, 30f);
        tail.anchoredPosition = new Vector2(0f, 12f);
        tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
        HudTheme.AddImage(tail, null, new Color32(22, 34, 50, 255));
        RectTransform tailFill = HudTheme.CreateRect("Fill", tail);
        HudTheme.Stretch(tailFill, 3f);
        HudTheme.AddImage(tailFill, null, new Color32(251, 246, 232, 255));

        RectTransform body = HudTheme.CreateRect("Body", bubble);
        HudTheme.Stretch(body);
        body.offsetMin = new Vector2(0f, 22f);
        HudTheme.AddImage(body, HudTheme.Rounded, new Color32(22, 34, 50, 255), 24f);
        RectTransform paper = HudTheme.CreateRect("Paper", body);
        HudTheme.Stretch(paper, 3f);
        HudTheme.AddImage(paper, HudTheme.Rounded, new Color32(251, 246, 232, 255), 21f);

        bubbleText = HudTheme.CreateLabel("Text", paper, "", 30f, new Color32(29, 43, 60, 255), TextAlignmentOptions.MidlineLeft);
        bubbleText.textWrappingMode = TextWrappingModes.Normal;
        bubbleText.fontSizeMin = 20f;
        RectTransform tr = bubbleText.rectTransform;
        HudTheme.Stretch(tr);
        tr.offsetMin = new Vector2(24f, 12f);
        tr.offsetMax = new Vector2(-24f, -14f);

        RectTransform chip = HudTheme.CreateRect("Name", bubble);
        chip.anchorMin = chip.anchorMax = new Vector2(0f, 1f);
        chip.pivot = new Vector2(0f, 0.5f);
        chip.anchoredPosition = new Vector2(22f, 0f);
        chip.sizeDelta = new Vector2(170f, 40f);
        HudTheme.AddImage(chip, HudTheme.Rounded, HudTheme.Brass, 20f);
        bubbleName = HudTheme.CreateLabel("Label", chip, (settings.name ?? "").ToUpperInvariant(), 21f,
            new Color32(29, 32, 40, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(bubbleName.rectTransform, 4f);
        bubbleName.characterSpacing = 6f;

        // Emote badge on the bubble's top-right corner (hidden on lines without an emote).
        emoteBadge = HudTheme.CreateRect("Emote", bubble);
        emoteBadge.anchorMin = emoteBadge.anchorMax = new Vector2(1f, 1f);
        emoteBadge.pivot = new Vector2(0.5f, 0.5f);
        emoteBadge.anchoredPosition = new Vector2(-30f, 4f);
        emoteBadge.sizeDelta = new Vector2(84f, 84f);
        RectTransform outline = HudTheme.CreateRect("Outline", emoteBadge);
        HudTheme.Stretch(outline);
        emoteOutline = HudTheme.AddImage(outline, null, new Color32(22, 34, 50, 255));
        emoteOutline.preserveAspect = true;
        emoteOutline.raycastTarget = false;
        RectTransform fill = HudTheme.CreateRect("Fill", emoteBadge);
        HudTheme.Stretch(fill);
        emoteFill = HudTheme.AddImage(fill, null, Color.white);
        emoteFill.preserveAspect = true;
        emoteFill.raycastTarget = false;
        emoteBadge.gameObject.SetActive(false);

        // Black layer on top for the fade.
        RectTransform blackRect = HudTheme.CreateRect("Black", root);
        HudTheme.Stretch(blackRect);
        HudTheme.AddImage(blackRect, null, Color.black).raycastTarget = true;
        black = blackRect.gameObject.AddComponent<CanvasGroup>();
        black.alpha = 0f;
        black.blocksRaycasts = false;
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        if (black == null) yield break;
        black.blocksRaycasts = true;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            black.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        black.alpha = to;
        black.blocksRaycasts = to > 0.5f;
    }

    private static IEnumerator WaitReal(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) yield return null;
    }

    private void ReleaseCard()
    {
        cardReleased = true;
        HudRewardScreen.HoldReveal = false;
    }

    private void Cleanup()
    {
        if (Running) finished = true;
        cardReleased = true;
        HudRewardScreen.PreferSideLayout = false;
        HudRewardScreen.HoldReveal = false;
        for (int i = 0; i < hiddenRenderers.Count; i++) if (hiddenRenderers[i] != null) hiddenRenderers[i].enabled = true;
        hiddenRenderers.Clear();
        for (int i = 0; i < disabledMarkers.Count; i++) if (disabledMarkers[i] != null) disabledMarkers[i].enabled = true;
        disabledMarkers.Clear();
        if (clone != null) Destroy(clone);
        if (finaleCamera != null) Destroy(finaleCamera.gameObject);
        if (uiHost != null) Destroy(uiHost);
        clone = null;
        finaleCamera = null;
        uiHost = null;
        Running = false;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static Transform FindInScene(Scene scene, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !scene.IsValid()) return null;
        path = path.Trim().Trim('/');
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            if (slash < 0) return root.transform;
            Transform child = root.transform.Find(path.Substring(slash + 1));
            if (child != null) return child;
        }
        return null;
    }

    private static bool TryBounds(Transform t, out Bounds bounds)
    {
        bounds = default;
        if (t == null) return false;
        bool any = false;
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }
}
