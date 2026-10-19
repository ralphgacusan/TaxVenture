using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Office Hours for Office_Level1: an in-game clock and a gentle deadline.
/// - The day starts at 7:30 AM; the clock runs only while the player can
///   actually play (it pauses for the guide's tap pages, reward cards and
///   the pause menu). The time shows at the top of the minimap.
/// - When the player reaches the office, the receptionist reveals that the
///   Auditor has an emergency and leaves at a set time. The time allowed
///   adapts to how long the side quest took (OfficeHoursMath).
/// - Arthur warns at 15 and 5 minutes left (game minutes).
/// - Missing it is never a fail: the Auditor goes home, it's "next
///   morning" with the case progress kept and a new, roomier deadline;
///   only the on-time bonus is lost.
/// - Handing in the case before the first deadline earns an on-time bonus
///   (a Bonus score entry; it does not raise the level maximum).
///
/// Settings: Resources/Levels/Level_01/office_hours.json (built-in defaults
/// below when missing). Observation-only towards gameplay: it reads states
/// and events and adds one score entry; it never changes the case or FSM.
/// </summary>
[DefaultExecutionOrder(120)]
public sealed class OfficeHours : MonoBehaviour
{
    private const string TargetSceneName = "Office_Level1";

    // Filled by JsonUtility at runtime, so the compiler sees no assignments.
#pragma warning disable 0649
    [Serializable]
    public class Settings
    {
        public bool enabled = true;
        public string startTime = "7:30 AM";
        public float gameSecondsPerRealSecond = 10f;
        public float baseSeconds = 300f;
        public float typicalSideQuestSeconds = 360f;
        public float factor = 0.4f;
        public float minSeconds = 270f;
        public float maxSeconds = 480f;
        public string nextMorningTime = "8:00 AM";
        public float nextMorningExtraFactor = 1.4f;
        public int[] warnMinutes = { 15, 5 };
        public float amberRealSeconds = 120f;
        public float redRealSeconds = 30f;
        public int onTimeBonusExp = 20;
        public int onTimeBonusHearts = 0;
        // Late case: taken off per day late (day 2 = 1 day), up to lateMaxDays.
        public int lateExpPerDay = 15;
        public int lateHeartsPerDay = 1;
        public int lateMaxDays = 3;
        public string[] receptionistLines =
        {
            "Oh, and one more thing: the Auditor has a family emergency and leaves at {deadline} sharp. Get your case to him before then!"
        };
        public string arthurWarning = "Tick-tock! The Auditor leaves in {minutes} minutes.";
        public string arthurMissed = "Oh no, the Auditor's gone home! Don't worry, he'll be back in the morning.";
        public string arthurNextMorning = "A fresh start! The Auditor is back, and today he leaves at {deadline}. Your work is right where you left it.";
        public string arthurOnTime = "Right on time! You got the folder to the Auditor before he leaves. If it's approved, that's an on-time bonus!";
        // Next morning the player starts outside the office front door.
        public bool nextMorningAtFrontDoor = true;
        public string frontDoor = "Environment/OfficeBuilding/Rooms/Reception/Doors";
        public string frontDoorInside = "Environment/OfficeBuilding/Rooms/Reception/Floor";
        public float frontDoorDistance = 2.2f;
        public string nextMorningSpawn = "";
    }
#pragma warning restore 0649

    public static OfficeHours Instance { get; private set; }

    /// <summary>Raised when the deadline is announced, missed (new day) or met.</summary>
    public event Action Changed;

    public Settings Config { get; private set; }
    public int Day { get; private set; } = 1;
    public double GameSeconds { get; private set; }
    public bool HasDeadline { get; private set; }
    public bool Announced { get; private set; }
    public int DeadlineSeconds { get; private set; }
    public bool CaseDone { get; private set; }
    public bool BonusAwarded { get; private set; }
    /// <summary>EXP / hearts actually taken off for a late case (0 when on time).</summary>
    public int LateExpLost { get; private set; }
    public int LateHeartsLost { get; private set; }
    public bool MissedOnce { get; private set; }
    public bool InNextMorning => nextMorning != null;

    public string ClockText => OfficeHoursMath.Format((int)GameSeconds);
    public string DeadlineText => OfficeHoursMath.Format(DeadlineSeconds);
    public int MinutesLeft => OfficeHoursMath.MinutesLeft(GameSeconds, DeadlineSeconds);
    public float RealSecondsLeft => OfficeHoursMath.RealSecondsLeft(GameSeconds, DeadlineSeconds, Config.gameSecondsPerRealSecond);

    /// <summary>True when the case was handed in during the first day, before the deadline.</summary>
    public bool OnTime => submittedDay == 1 && submittedAt >= 0 && submittedAt <= deadlineFirstDay;

    private float activeSeconds;
    private float sideQuestSeconds = -1f;
    private float allowance;
    private int deadlineFirstDay;
    private double submittedAt = -1;
    private int submittedDay;
    private int previousMinutesLeft = int.MaxValue;
    private bool onTimeSaid;
    private GameStateMachine stateMachine;
    private OfficeMinimapHUD minimap;
    private string shownClock;
    private float nextMinimapLookup;
    private Coroutine nextMorning;
    private OfficeHoursChip chip;
    private GameObject overlayHost;

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
        if (!scene.IsValid() || !scene.isLoaded || scene.name != TargetSceneName) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<OfficeHours>(true) != null) return;
        var host = new GameObject("Office Hours Runtime");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<OfficeHours>();
    }

    public static string ResourcePath(string levelId) => $"Levels/{levelId}/office_hours";

    private void Awake()
    {
        Instance = this;
        Config = LoadSettings(TaskListProvider.Level1Id);
        if (!Config.enabled)
        {
            enabled = false;
            return;
        }
        GameSeconds = OfficeHoursMath.ParseTime(Config.startTime, 7 * 3600 + 30 * 60);
        chip = OfficeHoursChip.Create(this);
    }

    private static Settings LoadSettings(string levelId)
    {
        TextAsset file = Resources.Load<TextAsset>(ResourcePath(levelId));
        if (file == null) return new Settings();
        try
        {
            Settings parsed = JsonUtility.FromJson<Settings>(file.text);
            if (parsed == null) return new Settings();
            parsed.gameSecondsPerRealSecond = Mathf.Max(0.1f, parsed.gameSecondsPerRealSecond);
            parsed.minSeconds = Mathf.Max(30f, parsed.minSeconds);
            parsed.maxSeconds = Mathf.Max(parsed.minSeconds, parsed.maxSeconds);
            parsed.nextMorningExtraFactor = Mathf.Max(1f, parsed.nextMorningExtraFactor);
            if (parsed.warnMinutes == null) parsed.warnMinutes = new int[0];
            return parsed;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OfficeHours] {ResourcePath(levelId)}.json is not valid JSON ({e.Message}). Using defaults.");
            return new Settings();
        }
    }

    private void OnEnable()
    {
        TownEvents.OnSideQuestCompleted += HandleSideQuestCompleted;
        GameplayEvents.OnNotesUnlockRequested += HandleReceptionistDone;
        GameplayEvents.OnCaseCompleted += HandleCaseCompleted;
    }

    private void OnDisable()
    {
        TownEvents.OnSideQuestCompleted -= HandleSideQuestCompleted;
        GameplayEvents.OnNotesUnlockRequested -= HandleReceptionistDone;
        GameplayEvents.OnCaseCompleted -= HandleCaseCompleted;
        if (stateMachine != null) stateMachine.OnStateChanged -= HandleStateChanged;
        stateMachine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (chip != null) Destroy(chip.gameObject);
        if (overlayHost != null) Destroy(overlayHost);
        PlayerControlHold.Release(this);
    }

    // =========================================================
    // CLOCK
    // =========================================================

    /// <summary>The clock waits while the player can't act (tutorial tap pages, reward cards, pause).</summary>
    private bool Paused()
    {
        if (Time.timeScale <= 0f || HudRewardScreen.AnyOpen || nextMorning != null || SideQuestSplash.IsShowing) return true;
        if (GameStateMachine.Instance != null && GameStateMachine.Instance.IsPaused) return true;
        GuideRuntime guide = GuideRuntime.Instance;
        return guide != null && guide.Director != null && guide.Director.IsBlocking;
    }

    private void Update()
    {
        if (stateMachine == null && GameStateMachine.Instance != null)
        {
            stateMachine = GameStateMachine.Instance;
            stateMachine.OnStateChanged += HandleStateChanged;
        }

        if (!Paused())
        {
            float dt = Time.unscaledDeltaTime;
            activeSeconds += dt;
            GameSeconds += dt * Config.gameSecondsPerRealSecond;
        }

        UpdateMinimapClock();
        if (!Announced || CaseDone || nextMorning != null) return;

        int left = MinutesLeft;
        if (previousMinutesLeft != int.MaxValue)
        {
            for (int i = 0; i < Config.warnMinutes.Length; i++)
            {
                if (!OfficeHoursMath.CrossedWarning(previousMinutesLeft, left, Config.warnMinutes[i])) continue;
                GuideVoice.TrySay(Config.arthurWarning.Replace("{minutes}", left.ToString()), "confused", 4.5f);
                if (chip != null) chip.Pop();
            }
        }
        previousMinutesLeft = left;

        if (GameSeconds >= DeadlineSeconds && CanCloseTheDay())
            nextMorning = StartCoroutine(NextMorning());
    }

    private void UpdateMinimapClock()
    {
        if (minimap == null && Time.unscaledTime >= nextMinimapLookup)
        {
            nextMinimapLookup = Time.unscaledTime + 1f;
            minimap = FindAnyObjectByType<OfficeMinimapHUD>(FindObjectsInactive.Include);
            shownClock = null;
        }
        if (minimap == null) return;
        string clock = ClockText;
        if (clock == shownClock) return;
        shownClock = clock;
        minimap.SetTitle(clock);
    }

    // =========================================================
    // DEADLINE
    // =========================================================

    private void HandleSideQuestCompleted()
    {
        if (sideQuestSeconds < 0f) sideQuestSeconds = activeSeconds;
    }

    /// <summary>
    /// The receptionist's extra lines revealing the deadline (computed on
    /// first use). Empty when Office Hours is off or already announced.
    /// </summary>
    public List<DialogueLine> AnnouncementLines(string speakerName)
    {
        var lines = new List<DialogueLine>();
        if (!enabled || Announced || CaseDone) return lines;
        if (!HasDeadline)
        {
            float sideQuest = sideQuestSeconds >= 0f ? sideQuestSeconds : activeSeconds;
            allowance = OfficeHoursMath.AllowanceSeconds(sideQuest, Config.baseSeconds, Config.typicalSideQuestSeconds,
                Config.factor, Config.minSeconds, Config.maxSeconds);
            DeadlineSeconds = OfficeHoursMath.Deadline((int)GameSeconds, allowance, Config.gameSecondsPerRealSecond);
            deadlineFirstDay = DeadlineSeconds;
            HasDeadline = true;
        }
        var builder = new DialogueBuilder(speakerName);
        foreach (string line in Config.receptionistLines ?? new string[0])
            if (!string.IsNullOrWhiteSpace(line)) builder.Npc(line.Replace("{deadline}", DeadlineText));
        return builder.Build();
    }

    // The receptionist's talk finished: the deadline is now known to the player.
    private void HandleReceptionistDone()
    {
        if (!HasDeadline || Announced) return;
        Announced = true;
        previousMinutesLeft = int.MaxValue;
        Changed?.Invoke();
    }

    private void HandleStateChanged(IGameState state)
    {
        if (state is AuditSubmittedState)
        {
            submittedAt = GameSeconds;
            submittedDay = Day;

            // Arthur cheers the moment the folder reaches the Auditor in time
            // (once), not later over the result cards.
            if (Announced && !CaseDone && !onTimeSaid && Day == 1 && GameSeconds <= DeadlineSeconds)
            {
                onTimeSaid = true;
                GuideVoice.TrySay(Config.arthurOnTime, "wow", 5f);
            }
        }
    }

    private void HandleCaseCompleted()
    {
        if (CaseDone) return;
        CaseDone = true;
        if (Announced && OnTime && !BonusAwarded)
        {
            BonusAwarded = true;
            CaseProgressionManager progression = CaseProgressionManager.Instance;
            if (progression != null && (Config.onTimeBonusExp > 0 || Config.onTimeBonusHearts > 0))
                progression.Score.AddBonus("On-time bonus", Config.onTimeBonusExp, Config.onTimeBonusHearts);
        }
        else if (Announced && Day > 1)
        {
            // Late: the case is still done, but it costs a little per day late.
            int daysLate = Mathf.Clamp(Day - 1, 0, Mathf.Max(0, Config.lateMaxDays));
            CaseProgressionManager progression = CaseProgressionManager.Instance;
            ScoreEntry penalty = progression != null
                ? progression.Score.AddPenalty("Late case", Config.lateExpPerDay * daysLate, Config.lateHeartsPerDay * daysLate)
                : null;
            if (penalty != null)
            {
                LateExpLost = -penalty.Exp;
                LateHeartsLost = -penalty.Hearts;
            }
        }
        Changed?.Invoke();
    }

    /// <summary>Never cut a conversation, a card or an audit in progress.</summary>
    private bool CanCloseTheDay()
    {
        if (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen) return false;
        IGameState state = GameStateMachine.Instance != null ? GameStateMachine.Instance.CurrentState : null;
        return !(state is AuditSubmittedState || state is CaseCompleteState || state is LevelCompleteState);
    }

    // =========================================================
    // NEXT MORNING (missing the deadline is never a fail)
    // =========================================================

    private IEnumerator NextMorning()
    {
        MissedOnce = true;
        PlayerControlHold.Acquire(this);
        GuideVoice.TrySay(Config.arthurMissed, "sad", 3.5f);
        yield return WaitReal(2.4f);

        CanvasGroup fade = BuildOverlay(out TextMeshProUGUI title, out TextMeshProUGUI time);
        title.text = "NEXT MORNING";
        yield return Fade(fade, 0f, 1f, 0.7f);

        if (Config.nextMorningAtFrontDoor) yield return ReturnToFrontDoor();

        Day++;
        GameSeconds = OfficeHoursMath.ParseTime(Config.nextMorningTime, 8 * 3600);
        DeadlineSeconds = OfficeHoursMath.Deadline((int)GameSeconds, allowance * Config.nextMorningExtraFactor,
            Config.gameSecondsPerRealSecond);
        previousMinutesLeft = int.MaxValue;
        time.text = $"DAY {Day}  ·  {ClockText}";
        Changed?.Invoke();
        yield return WaitReal(1.9f);
        yield return Fade(fade, 1f, 0f, 0.7f);

        PlayerControlHold.Release(this);
        nextMorning = null;
        GuideVoice.TrySay(Config.arthurNextMorning.Replace("{deadline}", DeadlineText), "default", 6f);
    }

    // =========================================================
    // NEXT MORNING: back at the office front door
    // =========================================================

    /// <summary>
    /// While the screen is dark: close the paper windows, leave the desk,
    /// then stand the player just outside the office front door, facing it,
    /// ready to walk in again. Case progress and the side quest are kept.
    /// </summary>
    private IEnumerator ReturnToFrontDoor()
    {
        NotesPanelUI notes = FindAnyObjectByType<NotesPanelUI>(FindObjectsInactive.Include);
        if (notes != null && notes.IsOpen) notes.Hide();
        HudProfileCards cards = FindAnyObjectByType<HudProfileCards>(FindObjectsInactive.Include);
        if (cards != null) cards.CloseCards();
        CaseFolder3DView folder = FindAnyObjectByType<CaseFolder3DView>(FindObjectsInactive.Include);
        if (folder != null && folder.IsOpen) folder.Close();

        CameraController controller = CameraController.Instance;
        if (controller == null) yield break;
        if (controller.CurrentMode == CameraController.CameraMode.Workstation) controller.ExitFirstPerson();

        // Let the desk / window transitions finish (the screen is dark).
        float until = Time.unscaledTime + 2f;
        while (controller.CurrentMode != CameraController.CameraMode.FirstPerson && Time.unscaledTime < until)
            yield return null;
        yield return WaitReal(0.6f);

        Transform player = controller.PlayerRoot;
        if (player == null || !TryFrontDoorSpot(player, out Vector3 spot, out Quaternion facing)) yield break;

        float pivotHeight = player.position.y - GroundBelow(player.position + Vector3.up * 0.5f, player);
        Vector3 target = new Vector3(spot.x, spot.y + Mathf.Max(0f, pivotHeight), spot.z);
        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.position = target;
            body.rotation = facing;
        }
        player.SetPositionAndRotation(target, facing);
        Physics.SyncTransforms();

        FirstPersonCameraController look = FindAnyObjectByType<FirstPersonCameraController>(FindObjectsInactive.Include);
        if (look != null) look.ResetLook();
    }

    private bool TryFrontDoorSpot(Transform player, out Vector3 spot, out Quaternion facing)
    {
        spot = default;
        facing = Quaternion.identity;

        // An explicit spawn object wins (its position and facing).
        Transform custom = FindInScene(Config.nextMorningSpawn);
        if (custom != null)
        {
            spot = new Vector3(custom.position.x, GroundBelow(custom.position + Vector3.up * 2f, player), custom.position.z);
            Vector3 f = custom.forward;
            f.y = 0f;
            facing = f.sqrMagnitude > 0.001f ? Quaternion.LookRotation(f) : Quaternion.identity;
            return true;
        }

        if (!TryBounds(FindInScene(Config.frontDoor), out Bounds door)) return false;
        Vector3 outward;
        if (TryBounds(FindInScene(Config.frontDoorInside), out Bounds inside))
        {
            outward = door.center - inside.center;
            outward.y = 0f;
        }
        else
        {
            outward = player.position - door.center;
            outward.y = 0f;
        }
        if (outward.sqrMagnitude < 0.001f) outward = Vector3.back;
        outward.Normalize();

        // Step outward until there is room to stand (doors, steps, signs).
        float distance = Mathf.Max(1f, Config.frontDoorDistance);
        for (int i = 0; i < 6; i++, distance += 0.6f)
        {
            Vector3 probe = door.center + outward * distance;
            // From just above the door's base, so an awning over the entrance is never 'ground'.
            float ground = GroundBelow(new Vector3(probe.x, door.min.y + 1.2f, probe.z), player);
            Vector3 feet = new Vector3(probe.x, ground, probe.z);
            if (Physics.CheckCapsule(feet + Vector3.up * 0.6f, feet + Vector3.up * 1.6f, 0.3f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            spot = feet;
            facing = Quaternion.LookRotation(-outward);
            return true;
        }
        return false;
    }

    private readonly RaycastHit[] groundHits = new RaycastHit[12];

    /// <summary>Highest walkable surface under a point (the player's own colliders ignored).</summary>
    private float GroundBelow(Vector3 from, Transform player)
    {
        int n = Physics.RaycastNonAlloc(from, Vector3.down, groundHits, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            if (player != null && groundHits[i].transform.IsChildOf(player)) continue;
            if (groundHits[i].normal.y < 0.5f) continue;
            if (groundHits[i].point.y > best) best = groundHits[i].point.y;
        }
        return best > float.MinValue ? best : from.y - 0.5f;
    }

    private Transform FindInScene(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        path = path.Trim().Trim('/');
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
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
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        if (!any)
            foreach (Collider c in t.GetComponentsInChildren<Collider>())
            {
                if (!any) { bounds = c.bounds; any = true; }
                else bounds.Encapsulate(c.bounds);
            }
        return any;
    }

    private static IEnumerator WaitReal(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) yield return null;
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
    {
        yield return OfficeMorningOverlay.Fade(group, from, to, seconds);
        group.blocksRaycasts = to > 0.5f;
    }

    private CanvasGroup BuildOverlay(out TextMeshProUGUI title, out TextMeshProUGUI time)
    {
        if (overlayHost != null) Destroy(overlayHost);
        CanvasGroup group = OfficeMorningOverlay.Create(gameObject.scene, 6000, out title, out time);
        overlayHost = group.gameObject;
        return group;
    }
}
