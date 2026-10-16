using System;

/// <summary>
/// PURPOSE:
/// Pure maths for Office Hours (no Unity): the in-game clock, how much time
/// the Auditor gives the player, and how times are written. Checked by
/// Tests/GuideRegression. OfficeHours (runtime) uses it.
///
/// Times are seconds since midnight of the in-game day.
/// </summary>
public static class OfficeHoursMath
{
    public const int SecondsPerDay = 24 * 3600;

    /// <summary>"7:30" / "07:30" / "7:30 AM" / "4:05 PM" to seconds since midnight; fallback when unreadable.</summary>
    public static int ParseTime(string text, int fallback)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        string t = text.Trim().ToUpperInvariant();
        bool pm = t.EndsWith("PM"), am = t.EndsWith("AM");
        if (pm || am) t = t.Substring(0, t.Length - 2).Trim();
        string[] parts = t.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int h) || !int.TryParse(parts[1], out int m)) return fallback;
        if (m < 0 || m > 59 || h < 0 || h > 23) return fallback;
        if (pm && h < 12) h += 12;
        if (am && h == 12) h = 0;
        return h * 3600 + m * 60;
    }

    /// <summary>"9:05 AM" style (12-hour clock).</summary>
    public static string Format(int secondsOfDay)
    {
        int s = ((secondsOfDay % SecondsPerDay) + SecondsPerDay) % SecondsPerDay;
        int h = s / 3600, m = (s / 60) % 60;
        string suffix = h < 12 ? "AM" : "PM";
        int h12 = h % 12 == 0 ? 12 : h % 12;
        return $"{h12}:{m:00} {suffix}";
    }

    /// <summary>
    /// Real seconds the player gets for the case, adapted to how long the
    /// side quest took: base + factor x (sideQuest - typical), clamped.
    /// A quick player gets a tighter (but fair) deadline; a slower player
    /// gets more room, never an endless one.
    /// </summary>
    public static float AllowanceSeconds(float sideQuestSeconds, float baseSeconds, float typicalSideQuestSeconds,
        float factor, float minSeconds, float maxSeconds)
    {
        if (maxSeconds < minSeconds) maxSeconds = minSeconds;
        float allowance = baseSeconds + factor * (Math.Max(0f, sideQuestSeconds) - typicalSideQuestSeconds);
        return Math.Min(maxSeconds, Math.Max(minSeconds, allowance));
    }

    /// <summary>The leaving time: now + allowance in game seconds, rounded UP to the next 5 minutes (a "real" time).</summary>
    public static int Deadline(int nowSeconds, float allowanceRealSeconds, float gameSecondsPerRealSecond)
    {
        float game = Math.Max(0f, allowanceRealSeconds) * Math.Max(0.01f, gameSecondsPerRealSecond);
        int raw = nowSeconds + (int)Math.Ceiling(game);
        const int step = 5 * 60;
        return (raw + step - 1) / step * step;
    }

    /// <summary>Whole game minutes left before the deadline (0 when passed).</summary>
    public static int MinutesLeft(double nowSeconds, int deadlineSeconds) =>
        Math.Max(0, (int)Math.Ceiling((deadlineSeconds - nowSeconds) / 60.0));

    /// <summary>Real seconds left, given the clock speed.</summary>
    public static float RealSecondsLeft(double nowSeconds, int deadlineSeconds, float gameSecondsPerRealSecond) =>
        (float)Math.Max(0.0, (deadlineSeconds - nowSeconds) / Math.Max(0.01f, gameSecondsPerRealSecond));

    /// <summary>True when the minutes-left count crossed a warning mark this tick (e.g. 15 then 5).</summary>
    public static bool CrossedWarning(int previousMinutesLeft, int minutesLeft, int mark) =>
        previousMinutesLeft > mark && minutesLeft <= mark && minutesLeft > 0;
}
