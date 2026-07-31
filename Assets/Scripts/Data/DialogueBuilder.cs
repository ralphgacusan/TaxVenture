using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Tiny fluent helper for building a List&lt;DialogueLine&gt; readably inside
/// *Interactable scripts, instead of each one hand-writing
/// "new List&lt;DialogueLine&gt; { new DialogueLine(...), ... }" boilerplate.
/// Purely a readability convenience — DialogueUI doesn't depend on this,
/// it just consumes the resulting List&lt;DialogueLine&gt;.
/// </summary>
public class DialogueBuilder
{
    private readonly List<DialogueLine> lines = new List<DialogueLine>();

    public DialogueBuilder Npc(string text, string portraitId = "default")
    {
        lines.Add(new DialogueLine(DialogueSpeaker.Npc, text, portraitId));
        return this;
    }

    public DialogueBuilder Player(string text)
    {
        lines.Add(new DialogueLine(DialogueSpeaker.Player, text));
        return this;
    }

    public List<DialogueLine> Build() => lines;
}