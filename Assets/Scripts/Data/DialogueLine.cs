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

public class DialogueLine
{
    public DialogueSpeaker Speaker;
    public string Text;
    public string PortraitId;
    public string SpeakerName;

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