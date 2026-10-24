using System;
using System.Collections.Generic;

/// <summary>What a character feels; shown by WorldEmote as an icon (Campaign Foundation T22).</summary>
public enum Emotion
{
    None,
    Thankful,   // heart
    Happy,      // sparkles
    Surprise,   // "!"
    Confused,   // "?"
    Thinking,   // "..."
    Idea,       // light bulb
    Nervous,    // sweat drop
    Worried,    // rain cloud
    Annoyed,    // anger puff (playful, never hostile)
    Cheerful    // music note
}

/// <summary>The drawn icon of an emotion. Every emotion has its own shape (colour-blind and dim-screen safe).</summary>
public enum EmoteShape { Heart, Sparkles, Exclaim, Question, Dots, Bulb, SweatDrop, RainCloud, AngerPuff, MusicNote }

/// <summary>How an icon moves while it is visible.</summary>
public enum EmoteMotion { Pulse, Twinkle, PopShake, Wobble, DotsInTurn, GlowPulse, SlideDown, Drip, DoublePulse, SwayUp }

/// <summary>One catalogue entry. Colours are HudTheme-style RGB bytes (no Unity types, so tests can read them).</summary>
public readonly struct EmoteStyle
{
    public readonly Emotion Emotion;
    public readonly string Tag;
    public readonly EmoteShape Shape;
    public readonly EmoteMotion Motion;
    public readonly byte R, G, B;
    public readonly string SoundId;
    public readonly float SoundVolume;

    public EmoteStyle(Emotion emotion, string tag, EmoteShape shape, EmoteMotion motion, byte r, byte g, byte b,
        string soundId = null, float soundVolume = 0f)
    {
        Emotion = emotion;
        Tag = tag;
        Shape = shape;
        Motion = motion;
        R = r;
        G = g;
        B = b;
        SoundId = soundId;
        SoundVolume = soundVolume;
    }

    public bool IsValid => Emotion != Emotion.None;
}

/// <summary>
/// PURPOSE:
/// The one emotion catalogue (T22): stable dialogue tags ("emote": "heart"),
/// the shape, colour and idle motion of each emotion, accepted aliases, and
/// the mapping of WorldEmote's original four kinds. Pure: no Unity calls.
/// The same icon always means the same thing; nothing here picks an emotion
/// automatically.
/// </summary>
public static class EmoteCatalogue
{
    // Order matches the Emotion enum (index = (int)emotion - 1).
    private static readonly EmoteStyle[] styles =
    {
        new EmoteStyle(Emotion.Thankful, "heart", EmoteShape.Heart, EmoteMotion.Pulse, 236, 93, 98),
        new EmoteStyle(Emotion.Happy, "sparkles", EmoteShape.Sparkles, EmoteMotion.Twinkle, 241, 194, 108),
        new EmoteStyle(Emotion.Surprise, "alert", EmoteShape.Exclaim, EmoteMotion.PopShake, 232, 128, 52),
        new EmoteStyle(Emotion.Confused, "question", EmoteShape.Question, EmoteMotion.Wobble, 58, 150, 214),
        new EmoteStyle(Emotion.Thinking, "thinking", EmoteShape.Dots, EmoteMotion.DotsInTurn, 112, 128, 146),
        new EmoteStyle(Emotion.Idea, "idea", EmoteShape.Bulb, EmoteMotion.GlowPulse, 250, 212, 84, "sfx_hint_received", 0.25f),
        new EmoteStyle(Emotion.Nervous, "sweat", EmoteShape.SweatDrop, EmoteMotion.SlideDown, 128, 198, 238),
        new EmoteStyle(Emotion.Worried, "worried", EmoteShape.RainCloud, EmoteMotion.Drip, 92, 120, 178),
        new EmoteStyle(Emotion.Annoyed, "annoyed", EmoteShape.AngerPuff, EmoteMotion.DoublePulse, 206, 58, 66),
        new EmoteStyle(Emotion.Cheerful, "music", EmoteShape.MusicNote, EmoteMotion.SwayUp, 96, 178, 122),
    };

    // Extra words authors may type; the canonical tag above always works too.
    private static readonly Dictionary<string, Emotion> aliases = new Dictionary<string, Emotion>(StringComparer.OrdinalIgnoreCase)
    {
        ["thankful"] = Emotion.Thankful, ["affection"] = Emotion.Thankful, ["love"] = Emotion.Thankful, ["hearts"] = Emotion.Thankful,
        ["happy"] = Emotion.Happy, ["excited"] = Emotion.Happy, ["sparkle"] = Emotion.Happy,
        ["surprise"] = Emotion.Surprise, ["surprised"] = Emotion.Surprise, ["!"] = Emotion.Surprise, ["exclaim"] = Emotion.Surprise,
        ["confused"] = Emotion.Confused, ["asking"] = Emotion.Confused, ["?"] = Emotion.Confused,
        ["waiting"] = Emotion.Thinking, ["dots"] = Emotion.Thinking, ["..."] = Emotion.Thinking,
        ["realization"] = Emotion.Idea, ["bulb"] = Emotion.Idea, ["lightbulb"] = Emotion.Idea,
        ["nervous"] = Emotion.Nervous, ["embarrassed"] = Emotion.Nervous, ["sweatdrop"] = Emotion.Nervous,
        ["sad"] = Emotion.Worried, ["tear"] = Emotion.Worried, ["cloud"] = Emotion.Worried,
        ["anger"] = Emotion.Annoyed, ["puff"] = Emotion.Annoyed,
        ["cheerful"] = Emotion.Cheerful, ["relaxed"] = Emotion.Cheerful, ["note"] = Emotion.Cheerful,
    };

    /// <summary>Every emotion, in enum order.</summary>
    public static IReadOnlyList<EmoteStyle> All => styles;

    public static EmoteStyle Get(Emotion emotion)
    {
        int index = (int)emotion - 1;
        return index >= 0 && index < styles.Length ? styles[index] : default;
    }

    /// <summary>Reads a dialogue tag (canonical tag or alias, any case). Empty = no emote; unknown = false.</summary>
    public static bool TryParseTag(string tag, out Emotion emotion)
    {
        emotion = Emotion.None;
        if (tag == null) return false;
        string key = tag.Trim();
        if (key.Length == 0) return false;
        for (int i = 0; i < styles.Length; i++)
            if (string.Equals(styles[i].Tag, key, StringComparison.OrdinalIgnoreCase))
            {
                emotion = styles[i].Emotion;
                return true;
            }
        return aliases.TryGetValue(key, out emotion);
    }

    /// <summary>
    /// WorldEmote's original kinds, by their enum value: Cheer (0) -> Happy,
    /// Shrug (1) -> Confused, Wait (2) -> Thinking, Worry (3) -> Worried.
    /// </summary>
    public static Emotion FromLegacyKind(int kind)
    {
        switch (kind)
        {
            case 0: return Emotion.Happy;
            case 1: return Emotion.Confused;
            case 2: return Emotion.Thinking;
            case 3: return Emotion.Worried;
            default: return Emotion.None;
        }
    }

    /// <summary>A burst's icon count, kept inside the 5-8 range.</summary>
    public static int BurstCount(int requested) => Math.Max(5, Math.Min(8, requested));
}
