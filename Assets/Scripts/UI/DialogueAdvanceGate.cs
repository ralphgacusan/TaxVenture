/// <summary>What one accepted dialogue gesture should do.</summary>
public enum DialogueGesture
{
    Ignored,
    RevealLine,
    Advance
}

/// <summary>
/// Single arbiter for dialogue-box taps, the Continue button and submit.
/// Every route calls Evaluate; at most one action is accepted per gesture.
/// While a line is still typing the accepted action is RevealLine, so one
/// gesture can never reveal and advance (or close) together. The opening
/// interaction and same-frame or near-simultaneous duplicates (a tap that
/// also reaches the button, a repeated submit) are rejected.
/// </summary>
public sealed class DialogueAdvanceGate
{
    private bool open;
    private int openFrame;
    private float openTime;
    private int lastFrame;
    private float lastTime;

    /// <summary>Seconds after opening during which gestures are ignored.</summary>
    public float OpeningGrace { get; set; } = 0.15f;

    /// <summary>Minimum seconds between two accepted gestures.</summary>
    public float MinimumInterval { get; set; } = 0.12f;

    public bool IsOpen => open;

    public void Open(int frame, float time)
    {
        open = true;
        openFrame = frame;
        openTime = time;
        lastFrame = int.MinValue;
        lastTime = float.NegativeInfinity;
    }

    public void Close() => open = false;

    public DialogueGesture Evaluate(int frame, float time, bool lineIsTyping)
    {
        if (!open) return DialogueGesture.Ignored;
        if (frame == openFrame || time - openTime < OpeningGrace) return DialogueGesture.Ignored;
        if (frame == lastFrame || time - lastTime < MinimumInterval) return DialogueGesture.Ignored;

        lastFrame = frame;
        lastTime = time;
        return lineIsTyping ? DialogueGesture.RevealLine : DialogueGesture.Advance;
    }
}
