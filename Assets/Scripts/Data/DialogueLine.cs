/// <summary>
/// PURPOSE:
/// One line of a sequential conversation. Speaker distinguishes NPC vs
/// Player lines (affects portrait/side styling later); PortraitId is a
/// placeholder hook for future per-line portrait/emotion art — currently
/// unused visually (all portraits are the same placeholder circle), but
/// present in the data model so a later milestone can wire it without
/// touching every call site that builds dialogue.
/// </summary>
public enum DialogueSpeaker { Npc, Player }

/// <summary>
/// How the conversation camera may frame a line. Auto lets the camera decide
/// from what is being said; CloseUp forces a solo close-up of the speaker;
/// OverShoulder forbids one.
/// </summary>
public enum DialogueCameraHint { Auto, OverShoulder, CloseUp }

public class DialogueLine
{
    public DialogueSpeaker Speaker;
    public string Text;
    public string PortraitId;
    public string SpeakerName;
    public DialogueCameraHint CameraHint = DialogueCameraHint.Auto;
    /// <summary>Optional emote shown over the speaker while this line lasts (T22); None = no emote.</summary>
    public Emotion Emote = Emotion.None;
    /// <summary>Peak moment: the emote plays as a burst of icons instead of a bubble.</summary>
    public bool EmoteBurst;
    /// <summary>
    /// Optional awaited action (T08): the conversation pauses on this line
    /// until the action with this id is completed (e.g. "client-copy");
    /// taps do not advance it. Null = an ordinary line.
    /// </summary>
    public string AwaitAction;
    /// <summary>What the player is asked to do while the line waits ("Drag the client copy to Marie").</summary>
    public string ActionPrompt;

    public DialogueLine(
        DialogueSpeaker speaker,
        string text,
        string portraitId = "default",
        string speakerName = "NPC")
    {
        Speaker = speaker;
        Text = text;
        PortraitId = portraitId;
        SpeakerName = speakerName;
    }
}