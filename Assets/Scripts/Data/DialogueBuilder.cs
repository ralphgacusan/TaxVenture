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

    public DialogueBuilder Player(string text)
    {
        lines.Add(new DialogueLine(
            DialogueSpeaker.Player,
            text
        ));

        return this;
    }

    public List<DialogueLine> Build() => lines;
}