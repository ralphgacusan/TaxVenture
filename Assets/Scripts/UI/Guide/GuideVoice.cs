using System;

/// <summary>
/// PURPOSE:
/// Lets gameplay code have Arthur say a short line in his bubble (with his
/// portrait) without depending on the guide: the corkboard reacts to each
/// placed card through this. GuideDirector registers the handler while it
/// runs; with no guide, TrySay returns false and the caller shows the line
/// its own way.
/// </summary>
public static class GuideVoice
{
    /// <summary>(text, mood, seconds) => shown?</summary>
    public static Func<string, string, float, bool> Handler;

    public static bool TrySay(string text, string mood = null, float seconds = 5f)
    {
        if (string.IsNullOrWhiteSpace(text) || Handler == null) return false;
        return Handler(text, mood, seconds);
    }
}
