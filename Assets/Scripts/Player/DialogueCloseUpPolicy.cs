using System;

/// <summary>
/// Decides when a line gets a solo close-up of its speaker (listener out of
/// frame). Pure logic so it is testable outside Unity.
///
/// - Intentional: a line marked DialogueCameraHint.CloseUp always gets one;
///   OverShoulder never does (see DialogueBuilder.CloseUp()/OverShoulder()).
/// - By content: Score() rates how much a line "lands" — exclamations,
///   shouted ALL-CAPS words, short punchy lines ("Wow."), interjections,
///   dramatic ellipses, short questions and weighty words ("important",
///   "penalty", "excellent"…). At or above Threshold it is a close-up.
/// - Random: borderline lines (at least RandomFloor) get one by chance.
/// - Pacing: never on the first line (establish the two-shot space first) and
///   at least MinLinesBetween other lines between automatic close-ups.
/// </summary>
public sealed class DialogueCloseUpPolicy
{
    public float Threshold { get; set; } = 2f;
    public float RandomFloor { get; set; } = 1f;
    public float RandomChance { get; set; } = 0.15f;
    public int MinLinesBetween { get; set; } = 2;

    private Random random = new Random(1);
    private int lastCloseUp = int.MinValue / 2;

    private static readonly string[] Interjections =
    {
        "wow", "whoa", "really", "seriously", "wait", "huh", "oh", "hmm", "oops", "yikes", "hey"
    };

    private static readonly string[] WeightyWords =
    {
        "important", "must", "never", "always", "careful", "warning", "remember", "penalty", "penalties",
        "fraud", "evade", "evasion", "illegal", "wrong", "correct", "excellent", "congratulations",
        "perfect", "sorry", "unfortunately", "honestly", "truth", "secret", "promise", "audit", "deadline"
    };

    /// <summary>Starts a new conversation: clears pacing and reseeds the random draw.</summary>
    public void Reset(int seed)
    {
        random = new Random(seed);
        lastCloseUp = int.MinValue / 2;
    }

    public bool Decide(DialogueLine line, int index)
    {
        if (line == null) return false;
        if (line.CameraHint == DialogueCameraHint.OverShoulder) return false;
        if (line.CameraHint == DialogueCameraHint.CloseUp)
        {
            lastCloseUp = index;
            return true;
        }

        if (index <= 0) return false;
        if (index - lastCloseUp <= MinLinesBetween) return false;

        float score = Score(line.Text);
        bool closeUp = score >= Threshold ||
                       (score >= RandomFloor && random.NextDouble() < RandomChance);
        if (closeUp) lastCloseUp = index;
        return closeUp;
    }

    /// <summary>How strongly a line calls for a close-up. Rich-text tags are ignored.</summary>
    public static float Score(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0f;
        string plain = StripTags(text);

        int words = 0, capsWords = 0;
        bool interjection = false;
        int weighty = 0;
        int i = 0;
        while (i < plain.Length)
        {
            if (!char.IsLetter(plain[i])) { i++; continue; }
            int start = i;
            int letters = 0, upper = 0;
            while (i < plain.Length && (char.IsLetter(plain[i]) || plain[i] == '\'' || plain[i] == '’'))
            {
                if (char.IsLetter(plain[i]))
                {
                    letters++;
                    if (char.IsUpper(plain[i])) upper++;
                }
                i++;
            }
            words++;
            if (letters >= 4 && upper == letters) capsWords++;
            string word = plain.Substring(start, i - start).ToLowerInvariant();
            if (!interjection && Array.IndexOf(Interjections, word) >= 0) interjection = true;
            if (Array.IndexOf(WeightyWords, word) >= 0) weighty++;
        }

        float score = 0f;
        if (plain.IndexOf('!') >= 0) score += 1.5f;
        if (plain.Contains("!!") || plain.Contains("?!") || plain.Contains("!?")) score += 0.5f;
        if (capsWords > 0) score += 2f;
        if (words >= 1 && words <= 3) score += 1.5f;
        if (interjection) score += 1f;
        if (plain.Contains("...") || plain.IndexOf('…') >= 0) score += 0.75f;
        if (plain.IndexOf('?') >= 0 && words <= 4) score += 0.5f;
        score += Math.Min(2, weighty);
        return score;
    }

    private static string StripTags(string text)
    {
        if (text.IndexOf('<') < 0) return text;
        var sb = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '<' && i + 1 < text.Length && (char.IsLetter(text[i + 1]) || text[i + 1] == '/' || text[i + 1] == '#'))
            {
                int close = text.IndexOf('>', i + 1);
                if (close > i)
                {
                    i = close;
                    continue;
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
