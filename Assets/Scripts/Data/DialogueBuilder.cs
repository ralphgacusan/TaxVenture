using System.Collections.Generic;

public class DialogueBuilder
{
    private readonly List<DialogueLine> lines = new List<DialogueLine>();

    private readonly string defaultNpcName;

    public DialogueBuilder(string npcName = "NPC")
    {
        defaultNpcName = npcName;
    }

    public DialogueBuilder Npc(
        string text,
        string portraitId = "default")
    {
        lines.Add(new DialogueLine(
            DialogueSpeaker.Npc,
            text,
            portraitId,
            defaultNpcName
        ));

        return this;
    }

    public DialogueBuilder Player(
        string text,
        string portraitId = "Auditor_Happy")
    {
        lines.Add(new DialogueLine(
            DialogueSpeaker.Player,
            text,
            portraitId,
            "You"
        ));

        return this;
    }

    /// <summary>Marks the previous line for a solo close-up of its speaker.</summary>
    public DialogueBuilder CloseUp() => Hint(DialogueCameraHint.CloseUp);

    /// <summary>Keeps the previous line on the over-the-shoulder shot (never a close-up).</summary>
    public DialogueBuilder OverShoulder() => Hint(DialogueCameraHint.OverShoulder);

    private DialogueBuilder Hint(DialogueCameraHint hint)
    {
        if (lines.Count > 0) lines[lines.Count - 1].CameraHint = hint;
        return this;
    }

    public List<DialogueLine> Build() => lines;
}