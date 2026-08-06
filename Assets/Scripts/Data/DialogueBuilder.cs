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

    public List<DialogueLine> Build() => lines;
}