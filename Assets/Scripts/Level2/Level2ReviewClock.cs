using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The review cutoff of Level 2 (T10), under Level2Gameplay/ReviewClock.
/// The Auditor stays all day; the cutoff only closes today's hand-ins.
/// </summary>
public sealed class Level2ReviewClock : MonoBehaviour
{
    public const string ChildName = "ReviewClock";
    private const string DeskPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk";
    private const string DeskSeatPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/DeskViewpoint";

    private OfficeHoursChip chip;
    private CanvasGroup fade;
    private TextMeshProUGUI fadeTitle;
    private TextMeshProUGUI fadeTime;
    private OfficeMinimapHUD minimap;
    private string shownClock;
    private float nextMinimapLookup;
    private Coroutine rollover;
    private bool rolling;

    private static CaseDefinition Definition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
    private static CaseReviewState CurrentReview => Level2OfficeStart.Review;
    private static ReviewClockState CurrentClock => CurrentReview != null ? CurrentReview.clock : null;
    private static ReviewClockDefinition ClockDefinition => Definition?.review?.clock;

    public bool Announced => CurrentClock != null && CurrentClock.started;
    public bool CaseDone => CurrentReview != null &&
        (CurrentReview.phase == ReviewPhase.Completed || (CurrentReview.audit != null && CurrentReview.audit.approved));
    public bool InNextMorning => rolling;
    public int Day => CurrentClock != null ? Math.Max(1, CurrentClock.day) : 1;
    public int MinutesLeft => Mathf.CeilToInt(Mathf.Max(0f, CurrentClock != null ? CurrentClock.remaining : 0f) *
                                               ReviewClock.GameSecondsPerActiveSecond / 60f);
    public double GameSeconds => ReviewClock.GameSeconds(CurrentClock, ClockDefinition);
    public float RealSecondsLeft => CurrentClock == null || CurrentClock.cutoffPending ? 0f : Mathf.Max(0f, CurrentClock.remaining);
    public float AmberSeconds => WarningSeconds(0, 120f);
    public float RedSeconds => WarningSeconds(1, 30f);
    public string DeadlineText => ReviewClock.FormatTimeOfDay(ReviewClock.DeadlineSeconds(CurrentClock, ClockDefinition));

    private void OnEnable()
    {
        GameplayEvents.OnNotesUnlockRequested += Announce;
        if (chip == null) chip = OfficeHoursChip.Create(this);
    }

    private void OnDisable()
    {
        GameplayEvents.OnNotesUnlockRequested -= Announce;
        if (rollover != null)
        {
            StopCoroutine(rollover);
            rollover = null;
        }
        rolling = false;
        PlayerControlHold.Release(this);
        DestroyPresentation();
    }

    private void OnDestroy() => PlayerControlHold.Release(this);

    private void DestroyPresentation()
    {
        if (chip != null)
        {
            Destroy(chip.gameObject);
            chip = null;
        }
        if (fade != null)
        {
            GameObject host = fade.gameObject;
            fade = null;
            fadeTitle = null;
            fadeTime = null;
            Destroy(host);
        }
    }

    /// <summary>The opening announcement: start the clock once and say the cutoff.</summary>
    private void Announce()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        ReviewClockDefinition clock = ClockDefinition;
        if (review == null || clock == null) return;
        if (review.clock == null) review.clock = new ReviewClockState();
        if (!ReviewClock.Start(review.clock, clock)) return;
        StartCoroutine(SayLater(Fill(clock.announceLine, review.clock, clock, review.clock.remaining), 1.2f));
    }

    private static IEnumerator SayLater(string line, float delay)
    {
        float until = Time.unscaledTime + delay;
        while (Time.unscaledTime < until || DialogueUI.IsAnyDialogueActive) yield return null;
        Level2Say.Line(line, null, 6f);
    }

    /// <summary>Compulsory teaching, conversations, results and menus stop the clock.</summary>
    public static bool ClockPaused
    {
        get
        {
            if (Time.timeScale <= 0f || InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive ||
                HudRewardScreen.AnyOpen || SideQuestSplash.IsShowing ||
                (CaseResultPopupUI.Instance != null && CaseResultPopupUI.Instance.IsVisible) ||
                (Level2AnnotatedPapers.Instance != null && Level2AnnotatedPapers.Instance.IsCollecting)) return true;
            if (GameStateMachine.Instance != null && GameStateMachine.Instance.IsPaused) return true;

            GuideDirector guide = GuideRuntime.Instance != null ? GuideRuntime.Instance.Director : null;
            if (guide != null && guide.IsBlocking) return true;

            Level2RosaStall stall = Level2OfficeStart.Instance != null
                ? Level2OfficeStart.Instance.GetComponentInChildren<Level2RosaStall>() : null;
            IncomeToolkit toolkit = stall != null && stall.Desk != null ? stall.Desk.Toolkit : null;
            return toolkit != null && toolkit.DemoRunning;
        }
    }

    /// <summary>Actions and blocking surfaces must finish before the day can roll over.</summary>
    private static bool ActionInFlight
    {
        get
        {
            if (ClockPaused || Input.GetMouseButton(0) || Input.touchCount > 0 || ValueDragSource.IsAnyDragActive) return true;

            IGameState state = GameStateMachine.Instance != null ? GameStateMachine.Instance.CurrentState : null;
            if (state is AuditSubmittedState || state is CaseCompleteState || state is LevelCompleteState) return true;

            Level2SupportingStack stack = Level2OfficeStart.Instance != null
                ? Level2OfficeStart.Instance.GetComponentInChildren<Level2SupportingStack>() : null;
            if (stack != null && stack.IsAnimating) return true;
            if (Level2TaxCodeBookIcon.Instance != null && Level2TaxCodeBookIcon.Instance.IsBusy) return true;

            Level2DeskStamps stamps = Level2OfficeStart.Instance != null
                ? Level2OfficeStart.Instance.GetComponentInChildren<Level2DeskStamps>() : null;
            return stamps != null && stamps.Busy;
        }
    }

    private void Update()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        ReviewClockDefinition clock = ClockDefinition;
        UpdateMinimapClock();

        bool show = review?.clock != null && review.clock.started && !CaseDone && clock != null;
        if (!show || rolling) return;

        ReviewClock.Tick tick = ReviewClock.Advance(review.clock, clock, Time.unscaledDeltaTime, ClockPaused, out float mark);
        if (tick == ReviewClock.Tick.Warning)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayEffect(mark <= 30f ? "sfx_deadline_urgent_01" : "sfx_deadline_warning_01");
            Level2Say.Line(Fill(clock.warnLine, review.clock, clock, mark), null, 4f);
        }
        else if (tick == ReviewClock.Tick.Cutoff)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_review_cutoff_01");
            Level2Say.Line(Fill(clock.missedLine, review.clock, clock, 0f), null, 5f);
        }

        if (review.clock.cutoffPending && !ActionInFlight) rollover = StartCoroutine(Rollover(review, clock));
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

        string clock = ReviewClock.FormatTimeOfDay(GameSeconds);
        if (clock == shownClock) return;
        shownClock = clock;
        minimap.SetTitle(clock);
    }

    private IEnumerator Rollover(CaseReviewState review, ReviewClockDefinition clock)
    {
        rolling = true;
        EnsureFade();
        fade.gameObject.SetActive(true);
        fade.blocksRaycasts = true;
        PlayerControlHold.Acquire(this);
        yield return OfficeMorningOverlay.Fade(fade, fade.alpha, 1f, 0.7f);

        // A new blocking modal may have opened after the fade began. Back out
        // while keeping the cutoff pending so the next safe boundary can retry.
        if (ActionInFlight)
        {
            yield return OfficeMorningOverlay.Fade(fade, 1f, 0f, 0.7f);
            fade.blocksRaycasts = false;
            fade.gameObject.SetActive(false);
            PlayerControlHold.Release(this);
            rolling = false;
            rollover = null;
            yield break;
        }

        bool done = CaseDone;
        bool rolled = ReviewClock.TryRollover(review.clock, clock, false, done, out bool firstMiss);
        if (rolled)
        {
            if (firstMiss && CaseProgressionManager.Instance != null)
            {
                // The one late consequence, inside the case caps (rewards.json maxExpLoss / maxHeartLoss).
                CaseProgressionManager.Instance.CaseExtraExpLoss = Mathf.Max(0, clock.lateExp);
                CaseProgressionManager.Instance.CaseExtraHeartLoss = Mathf.Max(0, clock.lateHearts);
            }

            fadeTitle.text = "NEXT MORNING";
            fadeTime.text = "DAY " + Day + "  ·  " + ReviewClock.FormatTimeOfDay(GameSeconds);

            // Clear a tap-selected transfer under the blackout so it cannot
            // unexpectedly apply after the day has changed.
            ValueTransferManager.Instance?.ClearSelection();

            // Next morning at the consultant desk (leave any desk view first).
            CameraController camera = CameraController.Instance;
            if (camera != null && camera.CurrentMode == CameraController.CameraMode.Workstation) camera.ExitFirstPerson();
            PlayerPlacement.PlaceBetween(gameObject.scene, DeskSeatPath, DeskPath, 0.15f);
            GameplayEvents.RaiseReviewDayRolled();
        }

        yield return new WaitForSecondsRealtime(1.9f);
        yield return OfficeMorningOverlay.Fade(fade, 1f, 0f, 0.7f);
        fade.blocksRaycasts = false;
        fade.gameObject.SetActive(false);
        PlayerControlHold.Release(this);
        rolling = false;
        rollover = null;

        if (rolled) Level2Say.Line(Fill(clock.nextMorningLine, review.clock, clock, review.clock.remaining), null, 6f);
    }

    private static string Fill(string line, ReviewClockState state, ReviewClockDefinition clock, float seconds)
    {
        if (string.IsNullOrEmpty(line)) return null;
        int gameMinutes = Mathf.CeilToInt(Mathf.Max(0f, seconds) * ReviewClock.GameSecondsPerActiveSecond / 60f);
        return line.Replace("{time}", ReviewClock.Format(seconds))
            .Replace("{minutes}", gameMinutes.ToString())
            .Replace("{deadline}", ReviewClock.FormatTimeOfDay(ReviewClock.DeadlineSeconds(state, clock)));
    }

    private float WarningSeconds(int index, float fallback)
    {
        ReviewClockDefinition clock = ClockDefinition;
        return clock?.warnSeconds != null && clock.warnSeconds.Count > index ? clock.warnSeconds[index] : fallback;
    }

    private void EnsureFade()
    {
        if (fade != null) return;
        fade = OfficeMorningOverlay.Create(gameObject.scene, 5000, out fadeTitle, out fadeTime);
    }
}
