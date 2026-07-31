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
    public string PortraitId; // placeholder hook — e.g. "client_neutral", "client_happy" later

    public DialogueLine(DialogueSpeaker speaker, string text, string portraitId = "default")
    {
        Speaker = speaker;
        Text = text;
        PortraitId = portraitId;
    }
}