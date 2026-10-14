using System;

/// <summary>
/// Typewriter timing over a line's VISIBLE characters. The caller supplies the
/// characters TMP actually renders (rich-text tags already removed), so tags
/// are never typed out and TMP's maxVisibleCharacters maps 1:1 to Visible.
/// Sentence punctuation followed by a space (or the end) and clause punctuation
/// add short pauses; runs like "..." pause once, after the last mark, and a
/// decimal point such as "3.5" does not pause.
/// </summary>
public sealed class DialogueRevealSchedule
{
    private float[] thresholds = new float[64];

    public int Total { get; private set; }
    public int Visible { get; private set; }
    public float Elapsed { get; private set; }
    public bool IsTyping => Visible < Total;

    public void Begin(string visibleCharacters, float charactersPerSecond,
        float sentencePause, float clausePause)
    {
        string text = visibleCharacters ?? string.Empty;
        char[] buffer = text.ToCharArray();
        Begin(buffer, buffer.Length, charactersPerSecond, sentencePause, clausePause);
    }

    public void Begin(char[] visibleCharacters, int count, float charactersPerSecond,
        float sentencePause, float clausePause)
    {
        if (visibleCharacters == null) count = 0;
        count = Math.Max(0, Math.Min(count, visibleCharacters?.Length ?? 0));
        if (thresholds.Length < count) thresholds = new float[Math.Max(count, thresholds.Length * 2)];

        float interval = 1f / Math.Max(1f, charactersPerSecond);
        sentencePause = Math.Max(0f, sentencePause);
        clausePause = Math.Max(0f, clausePause);

        float time = 0f;
        for (int i = 0; i < count; i++)
        {
            time += interval;
            thresholds[i] = time;
            time += PauseAfter(visibleCharacters, count, i, sentencePause, clausePause);
        }

        Total = count;
        Visible = 0;
        Elapsed = 0f;
    }

    /// <summary>Advances time and returns the number of visible characters.</summary>
    public int Tick(float deltaTime)
    {
        if (!IsTyping) return Visible;
        if (deltaTime > 0f) Elapsed += deltaTime;
        while (Visible < Total && thresholds[Visible] <= Elapsed) Visible++;
        return Visible;
    }

    public void RevealAll()
    {
        Visible = Total;
        if (Total > 0) Elapsed = Math.Max(Elapsed, thresholds[Total - 1]);
    }

    public void Clear()
    {
        Total = 0;
        Visible = 0;
        Elapsed = 0f;
    }

    /// <summary>Time at which character <paramref name="index"/> appears.</summary>
    public float RevealTimeOf(int index) =>
        index >= 0 && index < Total ? thresholds[index] : float.NaN;

    private static float PauseAfter(char[] chars, int count, int i,
        float sentencePause, float clausePause)
    {
        char c = chars[i];
        bool last = i == count - 1;
        if (last) return 0f;
        char next = chars[i + 1];

        if (IsSentenceMark(c))
        {
            // Pause once after a run of marks ("...", "?!"), and only when the
            // sentence actually breaks: "3.5" or "e.g" keep typing.
            if (IsSentenceMark(next)) return 0f;
            return char.IsWhiteSpace(next) || IsClosingQuote(next) ? sentencePause : 0f;
        }

        if (IsClauseMark(c))
            return char.IsWhiteSpace(next) ? clausePause : 0f;

        return 0f;
    }

    private static bool IsSentenceMark(char c) => c == '.' || c == '!' || c == '?' || c == '…';
    private static bool IsClauseMark(char c) => c == ',' || c == ';' || c == ':' || c == '—';
    private static bool IsClosingQuote(char c) => c == '"' || c == '\'' || c == '”' || c == '’' || c == ')';
}
