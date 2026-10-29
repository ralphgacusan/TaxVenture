using System;
using System.Collections.Generic;

/// <summary>The review cutoff of a case (Level 2, T10): active time left today, the day, and the one late consequence.</summary>
[Serializable]
public class ReviewClockState
{
    public bool started;
    public int day = 1;
    public float remaining;
    /// <summary>Warnings already given today (seconds marks).</summary>
    public List<float> warned = new List<float>();
    /// <summary>The window closed and waits for a safe moment to roll to the next morning.</summary>
    public bool cutoffPending;
    /// <summary>The first cutoff was missed (the only late consequence, applied once).</summary>
    public bool missed;
    /// <summary>T18: active seconds a review voucher added on extensionDay (0 = none). Later days get a fresh allowance only.</summary>
    public int extensionSeconds;
    public int extensionDay;
}

/// <summary>
/// PURPOSE:
/// The review cutoff clock, pure C# (deterministic for tests).
///
/// RULES:
/// - Starts once, at the opening announcement, with allowanceSeconds of ACTIVE time.
/// - Ticks only while not paused: the caller passes a pause predicate that is
///   true for compulsory guide lessons, conversations, blocking result cards
///   and the menu. Independent reading and solving consume time.
/// - Warnings fire once per mark per day (e.g. 2 minutes and 30 seconds).
/// - At zero the submission window closes (cutoffPending); the rollover waits
///   until no action owns the player (stamp, drag, hand-in), then happens once:
///   next morning, a fresh allowance, all work kept.
/// - Only the first missed cutoff is a late consequence; later days add none.
///   The Auditor never leaves. On-time means approved before the first cutoff.
/// </summary>
public static class ReviewClock
{
    public enum Tick { None, Warning, Cutoff }
    public const float GameSecondsPerActiveSecond = 10f;

    private const int FirstMorningStart = 7 * 3600 + 30 * 60;
    private const int LaterMorningStart = 8 * 3600;

    public static bool Start(ReviewClockState clock, ReviewClockDefinition definition)
    {
        if (clock == null || definition == null || !definition.IsSet || clock.started) return false;
        clock.started = true;
        clock.day = Math.Max(1, clock.day);
        clock.remaining = definition.allowanceSeconds;
        clock.warned = new List<float>();
        clock.cutoffPending = false;
        return true;
    }

    /// <summary>
    /// Advances active time. Returns Warning (with the mark crossed) or Cutoff
    /// once; None otherwise. Paused or stopped clocks never move.
    /// </summary>
    public static Tick Advance(ReviewClockState clock, ReviewClockDefinition definition, float seconds, bool paused, out float mark)
    {
        mark = 0f;
        if (clock == null || definition == null || !clock.started || clock.cutoffPending || paused || seconds <= 0f) return Tick.None;
        float before = clock.remaining;
        clock.remaining = Math.Max(0f, clock.remaining - seconds);
        if (clock.remaining <= 0f)
        {
            clock.cutoffPending = true;
            return Tick.Cutoff;
        }
        if (clock.warned == null) clock.warned = new List<float>();
        if (definition.warnSeconds != null)
            foreach (float w in definition.warnSeconds)
                if (before > w && clock.remaining <= w && !clock.warned.Contains(w))
                {
                    clock.warned.Add(w);
                    mark = w;
                    return Tick.Warning;
                }
        return Tick.None;
    }

    /// <summary>Submissions are open: the clock has not closed today's window.</summary>
    public static bool WindowOpen(ReviewClockState clock) => clock == null || !clock.cutoffPending;

    /// <summary>
    /// Rolls to the next morning once the window is closed and nothing owns an
    /// action. Returns true when it rolled; firstMiss tells whether this is the
    /// one late consequence.
    /// </summary>
    public static bool TryRollover(ReviewClockState clock, ReviewClockDefinition definition, bool actionInFlight, bool caseDone, out bool firstMiss)
    {
        firstMiss = false;
        if (clock == null || definition == null || !clock.cutoffPending || actionInFlight) return false;
        clock.cutoffPending = false;
        if (caseDone) return false;
        firstMiss = !clock.missed;
        clock.missed = true;
        clock.day++;
        clock.remaining = definition.allowanceSeconds;
        clock.warned = new List<float>();
        return true;
    }

    /// <summary>"11:42" style minutes:seconds of active time left.</summary>
    public static string Format(float seconds)
    {
        int s = Math.Max(0, (int)Math.Ceiling(seconds));
        return (s / 60) + ":" + (s % 60).ToString("00");
    }

    /// <summary>Game time of day, derived from the saved active allowance remaining.</summary>
    public static double GameSeconds(ReviewClockState clock, ReviewClockDefinition definition)
    {
        int day = clock != null ? Math.Max(1, clock.day) : 1;
        if (clock == null || !clock.started || definition == null) return MorningStart(day);
        double elapsed = Math.Max(0.0, definition.allowanceSeconds + ExtensionToday(clock) - (double)clock.remaining);
        return MorningStart(day) + elapsed * GameSecondsPerActiveSecond;
    }

    /// <summary>Today's displayed cutoff time for the saved review clock (a voucher used today moves it later).</summary>
    public static double DeadlineSeconds(ReviewClockState clock, ReviewClockDefinition definition)
    {
        int day = clock != null ? Math.Max(1, clock.day) : 1;
        double allowance = definition != null ? Math.Max(0.0, definition.allowanceSeconds) : 0.0;
        return MorningStart(day) + (allowance + ExtensionToday(clock)) * GameSecondsPerActiveSecond;
    }

    /// <summary>Active seconds a voucher added to today's window (0 on any other day).</summary>
    public static int ExtensionToday(ReviewClockState clock) =>
        clock != null && clock.extensionSeconds > 0 && clock.extensionDay == Math.Max(1, clock.day) ? clock.extensionSeconds : 0;

    /// <summary>
    /// T18: the review voucher rules' view of this clock. The cutoff only
    /// exists once the clock started; a closed window (cutoff pending) is the
    /// day closing; an approved case is done.
    /// </summary>
    public static VoucherClockView VoucherView(ReviewClockState clock, ReviewClockDefinition definition, bool caseDone)
    {
        bool started = clock != null && clock.started && definition != null && definition.IsSet;
        return new VoucherClockView
        {
            HasDeadline = started,
            Announced = started,
            CaseDone = caseDone,
            DayClosing = started && clock.cutoffPending,
            GameSeconds = started ? GameSeconds(clock, definition) : 0.0,
            DeadlineSeconds = started ? (int)Math.Floor(DeadlineSeconds(clock, definition)) : 0,
            ExtensionSeconds = ExtensionToday(clock)
        };
    }

    /// <summary>
    /// T18: the clock after one review voucher (a copy): today's window gets
    /// `seconds` more active time, recorded for today, so the shown time of
    /// day keeps moving forward and the cutoff moves later. Null when the
    /// window is closed, the clock has not started or today already has one.
    /// </summary>
    public static ReviewClockState Extend(ReviewClockState clock, int seconds)
    {
        if (clock == null || !clock.started || clock.cutoffPending || seconds <= 0 || ExtensionToday(clock) > 0) return null;
        int day = Math.Max(1, clock.day);
        return new ReviewClockState
        {
            started = true,
            day = day,
            remaining = clock.remaining + seconds,
            warned = new List<float>(clock.warned ?? new List<float>()),
            cutoffPending = false,
            missed = clock.missed,
            extensionSeconds = seconds,
            extensionDay = day
        };
    }

    /// <summary>Formats game seconds since midnight as a 12-hour office clock.</summary>
    public static string FormatTimeOfDay(double gameSeconds) =>
        OfficeHoursMath.Format((int)Math.Floor(gameSeconds));

    private static int MorningStart(int day) => day <= 1 ? FirstMorningStart : LaterMorningStart;
}
