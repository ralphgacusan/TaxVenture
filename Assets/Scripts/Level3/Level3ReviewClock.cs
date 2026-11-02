using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Bea's review clock (Level 3 T09), under Level3Gameplay/ReviewClock: the
/// existing review cutoff (ReviewClock rules, the Office Hours chip, the
/// minimap clock, the next-morning overlay and the review voucher card) bound
/// to Bea's case instead of Rosa's. The first adopter of a case-aware clock;
/// the company case reuses it (T12).
///
/// - Starts once, when the protected calculator practice is done (saved:
///   Level3ReviewRules.StartClock), with 16 ACTIVE minutes; the receptionist's
///   announcement style line follows when nothing is speaking.
/// - Ticks only outside the existing pauses (Level2ReviewClock.ClockPaused:
///   conversations, settings, result cards, blocking lessons, the catalogue);
///   reading and working use time. Active time lives on the chapter snapshot
///   and is written with the next save.
/// - At zero today's hand-ins close; once nothing owns the player the screen
///   fades to the next morning (saved: Level3ReviewRules.Rollover): a fresh
///   allowance, every bit of work kept, the player back at the desk. The
///   first miss is the one punctuality mark (inside the case XP cap); Public
///   Trust never moves for time. A failed save keeps the closed window and
///   tries again.
/// - A review voucher (VoucherUseCard) adds 180 active seconds once per case:
///   the voucher, its per-case claim and this extended clock in one write.
/// - Stops for good when Bea's computation is approved.
/// </summary>
public sealed class Level3ReviewClock : MonoBehaviour, IReviewCutoffClock
{
    public const string ChildName = "ReviewClock";
    private const string DeskPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk";
    private const string DeskSeatPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/DeskViewpoint";
    private const float RetrySeconds = 3f;

    public static Level3ReviewClock Active { get; private set; }

    private OfficeHoursChip chip;
    private CanvasGroup fade;
    private TextMeshProUGUI fadeTitle;
    private TextMeshProUGUI fadeTime;
    private OfficeMinimapHUD minimap;
    private string shownClock;
    private float nextMinimapLookup;
    private float nextStartTry;
    private float nextRolloverTry;
    private bool announcePending;
    private Coroutine rollover;
    private bool rolling;

    private static Level3Checkpointer Owner => Level3Checkpointer.Active;
    private static ReviewClockState CurrentClock => Owner != null && Owner.State != null ? Owner.State.Case(Level3ChapterRules.BeaCaseId)?.clock : null;
    private static ReviewClockDefinition ClockDefinition => Level3Checkpointer.BeaClock;

    // ---------- IReviewCutoffClock (the chip and the voucher card) ----------

    public string LevelId => Level3ChapterRules.LevelId;
    public bool Announced => CurrentClock != null && CurrentClock.started;
    public bool CaseDone => Owner != null && Level3ReviewRules.CaseDone(Owner.State);
    public bool InNextMorning => rolling;
    public int Day => CurrentClock != null ? Math.Max(1, CurrentClock.day) : 1;
    public int MinutesLeft => Mathf.CeilToInt(Mathf.Max(0f, CurrentClock != null ? CurrentClock.remaining : 0f) * ReviewClock.GameSecondsPerActiveSecond / 60f);
    public string DeadlineText => ReviewClock.FormatTimeOfDay(ReviewClock.DeadlineSeconds(CurrentClock, ClockDefinition));
    public double GameSeconds => ReviewClock.GameSeconds(CurrentClock, ClockDefinition);
    public float RealSecondsLeft => CurrentClock == null || CurrentClock.cutoffPending ? 0f : Mathf.Max(0f, CurrentClock.remaining);
    public float AmberSeconds => WarningSeconds(0, 120f);
    public float RedSeconds => WarningSeconds(1, 30f);
    public bool HasDeadline => Announced && ClockDefinition != null && ClockDefinition.IsSet;
    public int DeadlineSeconds => (int)Math.Floor(ReviewClock.DeadlineSeconds(CurrentClock, ClockDefinition));
    public int ExtendedDeadlineSeconds => DeadlineSeconds + VoucherRules.CutoffShift(ReviewClock.GameSecondsPerActiveSecond);
    public int ExtensionSeconds => ReviewClock.ExtensionToday(CurrentClock);

    public VoucherClockView VoucherView
    {
        get
        {
            VoucherClockView view = ReviewClock.VoucherView(CurrentClock, ClockDefinition, CaseDone);
            if (rolling) view.DayClosing = true;
            return view;
        }
    }

    /// <summary>The chapter as it will be after one voucher (a copy; null when the window is closed or already extended today).</summary>
    public Level3ChapterCheckpoint ExtendedChapter()
    {
        Level3Checkpointer owner = Owner;
        if (owner == null || !owner.IsReady || owner.State == null || rolling) return null;
        ReviewClockState extended = Level3ReviewRules.ExtendedClock(owner.State);
        if (extended == null) return null;
        Level3ChapterCheckpoint chapter = owner.State.Copy();
        chapter.Case(Level3ChapterRules.BeaCaseId).clock = extended;
        return chapter;
    }

    /// <summary>After the voucher is saved (or kept, outside a story run): the extended chapter becomes live; the chip follows.</summary>
    public void ApplyExtension(Level3ChapterCheckpoint chapter)
    {
        Owner?.PublishCommitted(chapter);
        shownClock = null;
    }

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void OnEnable()
    {
        Active = this;
        if (chip == null) chip = OfficeHoursChip.Create(this);
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        if (rollover != null)
        {
            StopCoroutine(rollover);
            rollover = null;
        }
        rolling = false;
        PlayerControlHold.Release(this);
        if (chip != null) { Destroy(chip.gameObject); chip = null; }
        if (fade != null)
        {
            GameObject host = fade.gameObject;
            fade = null;
            Destroy(host);
        }
    }

    private void OnDestroy() => PlayerControlHold.Release(this);

    /// <summary>The existing pauses (conversations, settings, result cards, blocking lessons, the catalogue); a held certificate is reading time.</summary>
    public static bool ClockPaused => Level2ReviewClock.ClockPaused;

    /// <summary>Actions and blocking surfaces must finish before the day can roll over.</summary>
    private static bool ActionInFlight =>
        ClockPaused || Input.GetMouseButton(0) || Input.touchCount > 0 || ValueDragSource.IsAnyDragActive ||
        (Level3DeskCalculator.Instance != null && Level3DeskCalculator.Instance.Busy) ||
        (Level3CertificatePaper.Instance != null && Level3CertificatePaper.Instance.IsUp) ||
        (AuditorInteractable.Current != null && AuditorInteractable.AwaitingFolder);

    private void Update()
    {
        Level3Checkpointer owner = Owner;
        if (owner == null || !owner.IsReady || owner.Blocked || owner.State == null) return;
        UpdateMinimapClock();

        // Her 16 active minutes start once the protected practice is done (saved; retried after a failed write).
        if (Level3ReviewRules.ClockDue(owner.State) && Time.unscaledTime >= nextStartTry)
        {
            nextStartTry = Time.unscaledTime + RetrySeconds;
            Level3Checkpointer.TapeCommit started = owner.CommitComputation((Level3ChapterCheckpoint c, out string r) =>
                Level3ReviewRules.StartClock(c, Level3Checkpointer.ReviewContent, out r), out _);
            if (started == Level3Checkpointer.TapeCommit.Attached)
            {
                announcePending = true;
                Debug.Log("[Level3] Bea's review clock started (" + ReviewClock.Format(CurrentClock.remaining) + " active).");
            }
        }
        if (announcePending && !DialogueUI.IsAnyDialogueActive && !HudRewardScreen.AnyOpen)
        {
            announcePending = false;
            Say(Fill(ClockDefinition?.announceLine, CurrentClock.remaining), 6f);
        }

        if (!Announced || CaseDone || rolling) return;
        ReviewClock.Tick tick = owner.AdvanceClock(Time.unscaledDeltaTime, ClockPaused, out float mark);
        if (tick == ReviewClock.Tick.Warning)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect(mark <= 30f ? "sfx_deadline_urgent_01" : "sfx_deadline_warning_01");
            Say(Fill(ClockDefinition.warnLine, mark), 4f);
        }
        else if (tick == ReviewClock.Tick.Cutoff)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_review_cutoff_01");
            Say(Fill(ClockDefinition.missedLine, 0f), 5f);
        }
        if (CurrentClock.cutoffPending && !ActionInFlight && Time.unscaledTime >= nextRolloverTry) rollover = StartCoroutine(Rollover());
    }

    private void UpdateMinimapClock()
    {
        if (!Announced) return;
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

    private IEnumerator Rollover()
    {
        rolling = true;
        if (fade == null) fade = OfficeMorningOverlay.Create(gameObject.scene, 5000, out fadeTitle, out fadeTime);
        fade.gameObject.SetActive(true);
        fade.blocksRaycasts = true;
        PlayerControlHold.Acquire(this);
        yield return OfficeMorningOverlay.Fade(fade, fade.alpha, 1f, 0.7f);

        // Something opened while it faded, or the morning could not be saved: back out, the window stays closed, try again later.
        bool rolled = false;
        if (!ActionInFlight && Owner != null)
        {
            Level3Checkpointer.TapeCommit saved = Owner.CommitComputation((Level3ChapterCheckpoint c, out string r) =>
                Level3ReviewRules.Rollover(c, Level3Checkpointer.ReviewContent, out _, out r), out _);
            rolled = saved == Level3Checkpointer.TapeCommit.Attached;
        }
        if (rolled)
        {
            fadeTitle.text = "NEXT MORNING";
            fadeTime.text = "DAY " + Day + "  ·  " + ReviewClock.FormatTimeOfDay(GameSeconds);
            ValueTransferManager.Instance?.ClearSelection();
            CameraController camera = CameraController.Instance;
            if (camera != null && camera.CurrentMode == CameraController.CameraMode.Workstation) camera.ExitFirstPerson();
            PlayerPlacement.PlaceBetween(gameObject.scene, DeskSeatPath, DeskPath, 0.15f);
            GameplayEvents.RaiseReviewDayRolled();
            yield return new WaitForSecondsRealtime(1.9f);
        }
        else nextRolloverTry = Time.unscaledTime + RetrySeconds;
        yield return OfficeMorningOverlay.Fade(fade, 1f, 0f, 0.7f);
        fade.blocksRaycasts = false;
        fade.gameObject.SetActive(false);
        PlayerControlHold.Release(this);
        rolling = false;
        rollover = null;
        if (rolled) Say(Fill(ClockDefinition.nextMorningLine, CurrentClock.remaining), 6f);
    }

    private static void Say(string line, float seconds)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        if (!GuideVoice.TrySay(line, null, seconds)) LevelGate.NotifyBlocked(line);
    }

    private static string Fill(string line, float seconds)
    {
        if (string.IsNullOrEmpty(line)) return null;
        int gameMinutes = Mathf.CeilToInt(Mathf.Max(0f, seconds) * ReviewClock.GameSecondsPerActiveSecond / 60f);
        return line.Replace("{time}", ReviewClock.Format(seconds))
            .Replace("{minutes}", gameMinutes.ToString())
            .Replace("{deadline}", ReviewClock.FormatTimeOfDay(ReviewClock.DeadlineSeconds(CurrentClock, ClockDefinition)));
    }

    private static float WarningSeconds(int index, float fallback)
    {
        ReviewClockDefinition clock = ClockDefinition;
        return clock?.warnSeconds != null && clock.warnSeconds.Count > index ? clock.warnSeconds[index] : fallback;
    }
}
