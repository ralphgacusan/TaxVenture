using System.Diagnostics;

/// <summary>
/// PURPOSE:
/// Chatty interaction logs (taps, drags, window focus, camera mode) that used
/// to run Debug.Log on every touch. Debug.Log is slow (it captures a stack
/// trace), which made drags stutter. These calls are compiled out unless the
/// TAXVENTURE_TRACE scripting define is set (Player Settings > Scripting
/// Define Symbols), so warnings and errors still show but touches stay smooth.
/// </summary>
public static class DebugTrace
{
    [Conditional("TAXVENTURE_TRACE")]
    public static void Log(object message) => UnityEngine.Debug.Log(message);

    [Conditional("TAXVENTURE_TRACE")]
    public static void Log(object message, UnityEngine.Object context) => UnityEngine.Debug.Log(message, context);
}
