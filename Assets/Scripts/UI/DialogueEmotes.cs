using UnityEngine;

/// <summary>
/// PURPOSE:
/// Plays a dialogue line's emote (T22). DialogueUI calls Play when a line
/// starts typing and Clear when it ends or the conversation closes, so the
/// emote lasts exactly as long as its line. Only tagged lines show one (no
/// automatic emotes). NPC lines show it over the NPC (the dialogue owner's
/// speaker root); player lines over the conversation-only Elijah visual.
/// A bubble stays above the dialogue box; a burst (peak moment) plays once.
/// Presentation only: never changes the conversation or any game state.
/// </summary>
public static class DialogueEmotes
{
    private static int held;

    public static void Play(DialogueLine line, Object owner, float boxTopScreenY)
    {
        Clear();
        if (line == null || line.Emote == Emotion.None) return;
        Transform target = line.Speaker == DialogueSpeaker.Player ? PlayerVisual() : NpcRoot(owner);
        if (target == null) return;
        // A dialogue box taller than most of the screen is not a box to stay above.
        float minY = boxTopScreenY < Screen.height * 0.6f ? boxTopScreenY : 0f;
        if (line.EmoteBurst) WorldEmote.Burst(target, line.Emote);
        else held = WorldEmote.Hold(target, line.Emote, minY);
    }

    public static void Clear()
    {
        WorldEmote.Release(held);
        held = 0;
    }

    private static Transform NpcRoot(Object owner)
    {
        Component component = owner as Component;
        if (component == null) return null;
        return DialogueSpeakerFrames.TryResolve(component, out DialogueSpeakerFrame frame) && frame.Root != null
            ? frame.Root : component.transform;
    }

    private static Transform PlayerVisual()
    {
        ConversationActorVisual fallback = null;
        foreach (ConversationActorVisual visual in Object.FindObjectsByType<ConversationActorVisual>(FindObjectsInactive.Exclude))
        {
            if (visual.IsShown) return visual.transform;
            if (fallback == null && visual.HasModel) fallback = visual;
        }
        return fallback != null ? fallback.transform : null;
    }
}
