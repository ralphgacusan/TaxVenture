using System.Collections.Generic;

public class DialogueBuilder
{
    private readonly List<DialogueLine> lines = new List<DialogueLine>();
    private readonly List<string> unknownEmoteTags = new List<string>();

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

    /// <summary>
    /// Gives the previous line an emote by its dialogue tag ("heart", "idea"...; see EmoteCatalogue).
    /// An empty tag means no emote; an unknown tag is skipped and listed in UnknownEmoteTags.
    /// </summary>
    public DialogueBuilder Emote(string tag, bool burst = false)
    {
        if (lines.Count == 0 || string.IsNullOrWhiteSpace(tag)) return this;
        if (!EmoteCatalogue.TryParseTag(tag, out Emotion emotion))
        {
            unknownEmoteTags.Add(tag.Trim());
            return this;
        }
        lines[lines.Count - 1].Emote = emotion;
        lines[lines.Count - 1].EmoteBurst = burst;
        return this;
    }

    /// <summary>
    /// Makes the previous line wait for an action (T08): taps do not advance
    /// it until the owner calls DialogueUI.CompleteAwaitedAction(actionId).
    /// </summary>
    public DialogueBuilder AwaitAction(string actionId, string prompt)
    {
        if (lines.Count == 0 || string.IsNullOrWhiteSpace(actionId)) return this;
        lines[lines.Count - 1].AwaitAction = actionId.Trim();
        lines[lines.Count - 1].ActionPrompt = prompt;
        return this;
    }

    /// <summary>Tags Emote() did not recognise (the caller warns once per load).</summary>
    public IReadOnlyList<string> UnknownEmoteTags => unknownEmoteTags;

    public List<DialogueLine> Build() => lines;
}