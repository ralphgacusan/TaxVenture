using System;

/// <summary>
/// PURPOSE:
/// The Level 1 classification board's first-check rules (Campaign
/// Foundation T06), pure C# so the regression suite checks them directly.
///
/// - Drafting never grades: the board is graded only when all four areas are
///   filled (it checks itself then; user decision 2026-10-24).
/// - The FIRST complete check freezes the correct count; the tier comes from
///   it: Gold 4/4, Silver 3/4, Bronze 0-2/4. Corrections never change it.
/// - Quest XP is settled once from that frozen count: questEXP minus
///   expPerWrong per incorrect profile, never below minExp (the same maths as
///   LevelScoring.SideQuest), so 30..50 with today's 50 / 5 defaults.
/// Tier values match BadgeTier (1 Bronze, 2 Silver, 3 Gold).
/// </summary>
public static class ClassificationCheck
{
    public const int Profiles = 4;
    public const int Bronze = 1;
    public const int Silver = 2;
    public const int Gold = 3;

    /// <summary>The frozen first-check tier: Gold 4/4, Silver 3/4, Bronze otherwise.</summary>
    public static int TierFor(int correct) =>
        correct >= Profiles ? Gold : correct == Profiles - 1 ? Silver : Bronze;

    /// <summary>Quest XP settled from the frozen count (never below minExp, never negative).</summary>
    public static int SettledExp(int questExp, int expPerWrong, int minExp, int wrong) =>
        Math.Max(Math.Max(0, minExp), questExp - Math.Max(0, wrong) * expPerWrong);

    /// <summary>A board can be checked only with every area holding a card.</summary>
    public static bool CanCheck(int placed) => placed >= Profiles;

    /// <summary>How many cards are still to place (reveals nothing).</summary>
    public static string RemainingMessage(int placed)
    {
        int left = Math.Max(0, Profiles - Math.Max(0, placed));
        return left == 1 ? "Place 1 more card and the board checks itself." : $"Place {left} more cards and the board checks itself.";
    }

    /// <summary>The neutral draft prompt (reveals nothing).</summary>
    public static string DraftMessage(int placed) =>
        CanCheck(placed)
            ? "All four placed. Checking the board..."
            : $"{Math.Max(0, placed)}/{Profiles} placed. Move cards freely; the board checks once all four are pinned.";

    /// <summary>The guide's one heads-up at 3/4 before the first check (reveals nothing).</summary>
    public const string LastCardPrompt = "One more card and I'll check the board. Your first check counts, so be sure!";

    /// <summary>
    /// Viewport y centres for the tray cards: centred on the view with equal
    /// top and bottom padding and `gap` between cards (all in view heights).
    /// </summary>
    public static float[] TrayCentres(int count, float cardHeight, float gap)
    {
        int n = Math.Max(1, count);
        float step = cardHeight + gap;
        float top = 0.5f + step * (n - 1) * 0.5f;
        var centres = new float[n];
        for (int i = 0; i < n; i++) centres[i] = top - step * i;
        return centres;
    }

    /// <summary>
    /// Hint ladder after a check (user decision 2026-10-23): checks 1 and 2 give
    /// a clue per red card (which of the two questions was missed, never the
    /// corner); from check 3 on, the red cards' right corners are shown.
    /// The reward is already frozen at check 1, so answers never change it.
    /// </summary>
    public const int AnswerRevealCheck = 3;

    /// <summary>True when the given check number (1-based) shows the answers.</summary>
    public static bool RevealsAnswers(int checkNumber) => checkNumber >= AnswerRevealCheck;
}
