/// <summary>
/// PURPOSE:
/// A short Level 2 message to the player. Arthur says it in his bubble, which
/// shows over the desk view too (the HUD fades out at the desk); without the
/// guide it falls back to the HUD blocked-action toast.
/// </summary>
public static class Level2Say
{
    public static void Line(string text, string mood = null, float seconds = 5f)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!GuideVoice.TrySay(text, mood, seconds)) LevelGate.NotifyBlocked(text);
    }
}
