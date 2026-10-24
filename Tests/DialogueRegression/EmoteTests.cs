using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Campaign Foundation T22 contracts: the emote catalogue and shapes (pure),
/// per-line emote data through DialogueBuilder / DialogueSession / the town
/// library and the JSON content, and source contracts for the WorldEmote
/// system and the DialogueUI hook (an emote plays on line start, only when
/// the line is tagged).
/// </summary>
public static class EmoteTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string Src(string rel) => File.ReadAllText(Path.Combine(Root, "Assets", rel));

    public static void Run(Action<bool, string> check)
    {
        Catalogue(check);
        Shapes(check);
        Tags(check);
        BuilderAndSession(check);
        TownLibrary(check);
        Content(check);
        WorldEmoteSource(check);
        DialogueHookSource(check);
    }

    // ------------------------------------------------------------------
    private static void Catalogue(Action<bool, string> check)
    {
        IReadOnlyList<EmoteStyle> all = EmoteCatalogue.All;
        Emotion[] emotions = Enum.GetValues<Emotion>().Where(e => e != Emotion.None).ToArray();
        check(all.Count == 10 && emotions.Length == 10, "T22: the catalogue has the ten approved emotions.");
        foreach (Emotion e in emotions)
        {
            EmoteStyle style = EmoteCatalogue.Get(e);
            check(style.IsValid && style.Emotion == e, $"T22: {e} has a catalogue entry.");
            check(!string.IsNullOrWhiteSpace(style.Tag) && style.Tag == style.Tag.Trim().ToLowerInvariant(), $"T22: {e} has a lower-case tag.");
        }
        check(!EmoteCatalogue.Get(Emotion.None).IsValid, "T22: None has no style (no emote).");
        check(all.Select(s => s.Shape).Distinct().Count() == all.Count, "T22: every emotion has its own shape.");
        check(all.Select(s => s.Tag).Distinct().Count() == all.Count, "T22: every emotion has its own tag.");
        check(all.Select(s => (s.R, s.G, s.B)).Distinct().Count() == all.Count, "T22: every emotion has its own colour.");
        check(all.Select(s => s.Motion).Distinct().Count() == all.Count, "T22: every emotion has its own idle motion.");

        var expected = new Dictionary<Emotion, (EmoteShape, EmoteMotion)>
        {
            [Emotion.Thankful] = (EmoteShape.Heart, EmoteMotion.Pulse),
            [Emotion.Happy] = (EmoteShape.Sparkles, EmoteMotion.Twinkle),
            [Emotion.Surprise] = (EmoteShape.Exclaim, EmoteMotion.PopShake),
            [Emotion.Confused] = (EmoteShape.Question, EmoteMotion.Wobble),
            [Emotion.Thinking] = (EmoteShape.Dots, EmoteMotion.DotsInTurn),
            [Emotion.Idea] = (EmoteShape.Bulb, EmoteMotion.GlowPulse),
            [Emotion.Nervous] = (EmoteShape.SweatDrop, EmoteMotion.SlideDown),
            [Emotion.Worried] = (EmoteShape.RainCloud, EmoteMotion.Drip),
            [Emotion.Annoyed] = (EmoteShape.AngerPuff, EmoteMotion.DoublePulse),
            [Emotion.Cheerful] = (EmoteShape.MusicNote, EmoteMotion.SwayUp),
        };
        foreach (var pair in expected)
            check((EmoteCatalogue.Get(pair.Key).Shape, EmoteCatalogue.Get(pair.Key).Motion) == pair.Value,
                $"T22: {pair.Key} uses the approved icon and motion.");

        // The original WorldEmote kinds, by enum value (Cheer, Shrug, Wait, Worry).
        check(EmoteCatalogue.FromLegacyKind(0) == Emotion.Happy, "T22: Cheer maps to Happy (sparkles).");
        check(EmoteCatalogue.FromLegacyKind(1) == Emotion.Confused, "T22: Shrug maps to Confused (\"?\").");
        check(EmoteCatalogue.FromLegacyKind(2) == Emotion.Thinking, "T22: Wait maps to Thinking (\"...\").");
        check(EmoteCatalogue.FromLegacyKind(3) == Emotion.Worried, "T22: Worry maps to Worried.");
        check(EmoteCatalogue.FromLegacyKind(4) == Emotion.None && EmoteCatalogue.FromLegacyKind(-1) == Emotion.None,
            "T22: an unknown legacy kind maps to no emote.");

        check(EmoteCatalogue.BurstCount(1) == 5 && EmoteCatalogue.BurstCount(6) == 6 && EmoteCatalogue.BurstCount(20) == 8,
            "T22: a burst has 5-8 icons.");

        foreach (EmoteStyle style in all.Where(s => s.SoundId != null))
        {
            check(Directory.EnumerateFiles(Path.Combine(Root, "Assets"), style.SoundId + "_0*.mp3", SearchOption.AllDirectories).Any(),
                $"T22: {style.Emotion}'s soft sound {style.SoundId} exists in the project.");
            check(style.SoundVolume > 0f && style.SoundVolume <= 0.4f, $"T22: {style.Emotion}'s sound is soft.");
        }
    }

    // ------------------------------------------------------------------
    private static bool[] Mask(EmoteShape shape, int size)
    {
        var mask = new bool[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                mask[y * size + x] = EmoteShapes.Covers(shape, (x + 0.5f) / size, (y + 0.5f) / size);
        return mask;
    }

    private static void Shapes(Action<bool, string> check)
    {
        const int size = 32;
        EmoteShape[] shapes = Enum.GetValues<EmoteShape>();
        var masks = shapes.ToDictionary(s => s, s => Mask(s, size));
        foreach (EmoteShape shape in shapes)
        {
            bool[] m = masks[shape];
            float area = m.Count(b => b) / (float)m.Length;
            check(area > 0.04f && area < 0.65f, $"T22: the {shape} icon has a readable size ({area:P0} of its square).");
            bool edgeFree = true;
            for (int i = 0; i < size; i++)
                edgeFree &= !m[i] && !m[(size - 1) * size + i] && !m[i * size] && !m[i * size + size - 1];
            check(edgeFree, $"T22: the {shape} icon leaves room for its outline.");
        }
        for (int a = 0; a < shapes.Length; a++)
            for (int b = a + 1; b < shapes.Length; b++)
            {
                bool[] ma = masks[shapes[a]], mb = masks[shapes[b]];
                int differ = 0;
                for (int i = 0; i < ma.Length; i++) if (ma[i] != mb[i]) differ++;
                check(differ / (float)ma.Length >= 0.08f, $"T22: {shapes[a]} and {shapes[b]} are clearly different shapes.");
            }
        check(EmoteShapes.Glow(0.5f, 0.5f) > 0.99f && EmoteShapes.Glow(0.02f, 0.5f) < 0.01f, "T22: the bulb glow fades from the centre.");
    }

    // ------------------------------------------------------------------
    private static void Tags(Action<bool, string> check)
    {
        foreach (EmoteStyle style in EmoteCatalogue.All)
        {
            check(EmoteCatalogue.TryParseTag(style.Tag, out Emotion e) && e == style.Emotion, $"T22: tag \"{style.Tag}\" reads as {style.Emotion}.");
            check(EmoteCatalogue.TryParseTag("  " + style.Tag.ToUpperInvariant() + " ", out e) && e == style.Emotion,
                $"T22: tag \"{style.Tag}\" is read in any case, trimmed.");
        }
        (string, Emotion)[] aliases =
        {
            ("!", Emotion.Surprise), ("?", Emotion.Confused), ("...", Emotion.Thinking), ("lightbulb", Emotion.Idea),
            ("sad", Emotion.Worried), ("thankful", Emotion.Thankful), ("embarrassed", Emotion.Nervous), ("cheerful", Emotion.Cheerful)
        };
        foreach ((string tag, Emotion expected) in aliases)
            check(EmoteCatalogue.TryParseTag(tag, out Emotion e) && e == expected, $"T22: alias \"{tag}\" reads as {expected}.");
        foreach (string bad in new[] { null, "", "   ", "frown", "heart!", "hearts and stars", "Kind.Cheer", "cheer" })
            check(!EmoteCatalogue.TryParseTag(bad, out Emotion e) && e == Emotion.None, $"T22: tag \"{bad}\" is rejected.");
    }

    // ------------------------------------------------------------------
    private static void BuilderAndSession(Action<bool, string> check)
    {
        var builder = new DialogueBuilder("Marie");
        builder.Emote("heart"); // no line yet: ignored, no crash
        builder.Npc("Hello again!").Emote("music")
               .Player("I have your return.").Emote("idea")
               .Npc("Oh, thank you!").Emote("heart", burst: true)
               .Npc("Plain line!")
               .Npc("Odd line.").Emote("frown")
               .Npc("Empty tag.").Emote("  ");
        List<DialogueLine> lines = builder.Build();
        check(lines[0].Emote == Emotion.Cheerful && !lines[0].EmoteBurst, "T22: Emote() tags the previous NPC line.");
        check(lines[1].Emote == Emotion.Idea && lines[1].Speaker == DialogueSpeaker.Player, "T22: a player line can carry an emote.");
        check(lines[2].Emote == Emotion.Thankful && lines[2].EmoteBurst, "T22: the burst flag is kept.");
        check(lines[3].Emote == Emotion.None && !lines[3].EmoteBurst, "T22: an untagged line has no emote, even one ending in '!' (no automatic emotes).");
        check(lines[4].Emote == Emotion.None && builder.UnknownEmoteTags.SequenceEqual(new[] { "frown" }),
            "T22: an unknown tag is ignored and reported once.");
        check(lines[5].Emote == Emotion.None, "T22: an empty tag means no emote.");

        var session = new DialogueSession();
        check(session.TryStart(lines, () => { }), "T22: tagged lines start a conversation.");
        lines[0].Emote = Emotion.Annoyed;
        lines[2].EmoteBurst = false;
        check(session.CurrentLine.Emote == Emotion.Cheerful, "T22: the session keeps its own copy of the line's emote.");
        session.TryAdvance(out _);
        session.TryAdvance(out _);
        check(session.CurrentLine.Emote == Emotion.Thankful && session.CurrentLine.EmoteBurst, "T22: the session copy keeps the burst flag.");
        session.TryEnd(DialogueEndReason.Cancelled, out _);
    }

    // ------------------------------------------------------------------
    private static void TownLibrary(Action<bool, string> check)
    {
        TownNpcDialogueLibrary.ClearOverrides();
        List<DialogueLine> marie = TownNpcDialogueLibrary.GetDialogueFor("npc_marie", "Marie");
        check(marie[1].Emote == Emotion.Cheerful && marie[5].Emote == Emotion.Happy, "T22: Marie's built-in plaza talk has its sample emotes.");
        check(marie.Count(l => l.Emote != Emotion.None) == 2 && marie[0].Emote == Emotion.None, "T22: the other plaza lines stay untagged.");
        check(TownNpcDialogueLibrary.GetDialogueFor("npc_zorbix", "Zorbix").Where(l => l.Speaker == DialogueSpeaker.Player).All(l => l.Emote == Emotion.None),
            "T22: Elijah's plaza lines carry no emote (Elijah emotes sparingly).");

        var talk = new List<KeyValuePair<bool, string>> { new KeyValuePair<bool, string>(true, "Hi."), new KeyValuePair<bool, string>(false, "Hello!") };
        TownNpcDialogueLibrary.SetOverride("npc_marie", talk,
            new List<KeyValuePair<string, bool>> { new KeyValuePair<string, bool>(null, false), new KeyValuePair<string, bool>("sweat", true) });
        List<DialogueLine> custom = TownNpcDialogueLibrary.GetDialogueFor("npc_marie", "Marie");
        check(custom[0].Emote == Emotion.None && custom[1].Emote == Emotion.Nervous && custom[1].EmoteBurst, "T22: a town.json override applies its per-line emotes.");
        TownNpcDialogueLibrary.SetOverride("npc_marie", talk);
        check(TownNpcDialogueLibrary.GetDialogueFor("npc_marie", "Marie").All(l => l.Emote == Emotion.None), "T22: an override without emotes shows none.");
        TownNpcDialogueLibrary.SetOverride("npc_marie", talk,
            new List<KeyValuePair<string, bool>> { new KeyValuePair<string, bool>("heart", false) });
        TownNpcDialogueLibrary.ClearOverrides();
        check(TownNpcDialogueLibrary.GetDialogueFor("npc_marie", "Marie")[1].Emote == Emotion.Cheerful, "T22: clearing overrides also clears their emotes.");
    }

    // ------------------------------------------------------------------
    private static void Content(Action<bool, string> check)
    {
        using JsonDocument town = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Assets", "Resources", "Levels", "Level_01", "town.json")));
        int tagged = 0;
        foreach (JsonElement taxpayer in town.RootElement.GetProperty("taxpayers").EnumerateArray())
        {
            string npcId = taxpayer.GetProperty("npcId").GetString();
            List<DialogueLine> builtIn = TownNpcDialogueLibrary.GetDialogueFor(npcId, "Npc");
            int i = 0;
            foreach (JsonElement line in taxpayer.GetProperty("talk").EnumerateArray())
            {
                string tag = line.TryGetProperty("emote", out JsonElement e) ? e.GetString() : null;
                Emotion parsed = Emotion.None;
                if (tag != null)
                {
                    tagged++;
                    check(EmoteCatalogue.TryParseTag(tag, out parsed), $"T22: town.json {npcId} line {i + 1} emote \"{tag}\" is known.");
                }
                check(i < builtIn.Count && builtIn[i].Emote == parsed, $"T22: town.json {npcId} line {i + 1} emote matches the built-in copy.");
                i++;
            }
        }
        check(tagged >= 4, "T22: town.json carries the sample emotes.");

        foreach (string file in Directory.EnumerateFiles(Path.Combine(Root, "Assets", "Resources", "Levels"), "*.json", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text, "\"emote\"\\s*:\\s*\"([^\"]*)\""))
                check(EmoteCatalogue.TryParseTag(m.Groups[1].Value, out _), $"T22: {Path.GetFileName(file)} emote \"{m.Groups[1].Value}\" is known.");
        }

        using JsonDocument marieCase = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Assets", "Resources", "Levels", "Level_01", "cases", "case_001.json")));
        JsonElement failed = marieCase.RootElement.GetProperty("clientAfterFailedAudit");
        check(failed[0].GetProperty("emote").GetString() == "worried" && failed[failed.GetArrayLength() - 1].GetProperty("emote").GetString() == "heart",
            "T22: Marie's after-audit talk opens worried and ends thankful.");
    }

    // ------------------------------------------------------------------
    private static void WorldEmoteSource(Action<bool, string> check)
    {
        string src = Src("Scripts/UI/HUD/WorldEmote.cs");
        check(src.Contains("public enum Kind { Cheer, Shrug, Wait, Worry }"), "T22: the original WorldEmote kinds keep their names and values.");
        check(src.Contains("Show(target, EmoteCatalogue.FromLegacyKind((int)kind), seconds)") &&
              src.Contains("EmoteCatalogue.FromLegacyKind((int)kind), Mathf.Max(0.8f, seconds), 0f)"),
            "T22: the original kinds play through the new emotion set.");
        check(src.Contains("RenderMode.ScreenSpaceOverlay") && Regex.Matches(src, "blocksRaycasts = false").Count >= 2 && !src.Contains("raycastTarget = true") &&
              !src.Contains("AddComponent<Image>") && !src.Contains("AddComponent<Button>"),
            "T22: emotes are a screen-space overlay that never blocks touches.");
        check(src.Contains("Time.unscaledDeltaTime") && !Regex.IsMatch(src, @"Time\.deltaTime|Time\.time\b"), "T22: emotes run on unscaled time.");
        check(src.Contains("BubblePoolSize = 8") && src.Contains("SparkPoolSize = 16"), "T22: bubbles and burst icons are pooled.");

        int late = src.IndexOf("private void LateUpdate()", StringComparison.Ordinal);
        string perFrame = late >= 0 ? src.Substring(late) : "";
        check(late >= 0 && !Regex.IsMatch(perFrame, @"new (List|Bubble|Spark|Anchor|GameObject)\b|GetComponent|FindObject|\$""|string\.Format|\.ToString\("),
            "T22: the per-frame code allocates nothing.");
        check(src.Contains("HumanBodyBones.Head") && src.Contains("TryBounds(a.Renderers"), "T22: emotes sit above the head bone, else the renderers' top.");
        check(src.Contains("side * SideOffset") && src.Contains("minY + 8f") && src.Contains("sp.z > 0.05f"),
            "T22: emotes are offset to the side, kept above the dialogue box and hidden behind the camera.");
        check(src.Contains("EnterSeconds = 0.2f") && src.Contains("ExitSeconds = 0.25f") && src.Contains("1.15f * HudTheme.EaseOutCubic"),
            "T22: entrance 0 -> 1.15 -> 1 in 0.2 s; exit fades and drifts up in 0.25 s.");
        check(src.Contains("EmoteCatalogue.BurstCount(count)") && src.Contains("UnityEngine.Random.Range(0.9f, 1.1f)") && src.Contains("i * 0.06f"),
            "T22: bursts are staggered and last about 1.2-1.6 s.");
        check(src.Contains("ReducedMotionKey = \"ReducedMotion\"") && src.Contains("bool still = b.Reduced;") &&
              Regex.IsMatch(src, @"if \(ReducedMotion\)\s*\{\s*SpawnBubble\(follow, Vector3\.zero, emotion, 1\.4f, 0f\);"),
            "T22: reduced motion keeps the icon, drops idle motion and turns a burst into one still bubble.");
        check(src.Contains("AudioManager.Instance.PlayEffect(style.SoundId, style.SoundVolume)") &&
              Src("Scripts/AudioManager/AudioManager.cs").Contains("if (!sfxEnabled || sfxSource == null) return null;"),
            "T22: emote sounds go through AudioManager and never play with SFX off.");

        string reactions = Src("Scripts/UI/HUD/ConsequenceReactions.cs");
        check(reactions.Contains("correct ? Emotion.Happy : locked ? Emotion.Thinking : Emotion.Confused") &&
              reactions.Contains("WorldEmote.Show(npc.transform, Emotion.Happy, 5f)") && !reactions.Contains("WorldEmote.Kind"),
            "T22: corkboard and plaza reactions use the new emotion set with unchanged timing.");
    }

    // ------------------------------------------------------------------
    private static void DialogueHookSource(Action<bool, string> check)
    {
        string ui = Src("Scripts/UI/DialogueUI.cs");
        int render = ui.IndexOf("private void RenderCurrentLine()", StringComparison.Ordinal);
        int reveal = ui.IndexOf("BeginReveal();", render, StringComparison.Ordinal);
        int play = ui.IndexOf("DialogueEmotes.Play(line, sessionOwner, BoxTopScreenY())", render, StringComparison.Ordinal);
        int changed = ui.IndexOf("LineChanged?.Invoke(line);", render, StringComparison.Ordinal);
        check(render >= 0 && reveal > render && play > reveal && changed > play, "T22: DialogueUI plays the line's emote when the line starts typing.");
        int finish = ui.IndexOf("private void Finish(DialogueEndReason reason)", StringComparison.Ordinal);
        check(finish >= 0 && ui.IndexOf("DialogueEmotes.Clear();", finish, StringComparison.Ordinal) > finish, "T22: ending or cancelling a conversation clears its emote.");
        check(Regex.Matches(ui, @"DialogueEmotes\.").Count == 2, "T22: DialogueUI has exactly one play and one clear call.");

        string hook = Src("Scripts/UI/DialogueEmotes.cs");
        check(Regex.IsMatch(hook, @"Clear\(\);\s*if \(line == null \|\| line\.Emote == Emotion\.None\) return;"),
            "T22: an untagged line shows no emote (and ends the previous line's).");
        check(hook.Contains("line.Speaker == DialogueSpeaker.Player ? PlayerVisual() : NpcRoot(owner)") && hook.Contains("ConversationActorVisual"),
            "T22: NPC lines show over the NPC, player lines over the conversation Elijah visual.");
        check(hook.Contains("if (line.EmoteBurst) WorldEmote.Burst(target, line.Emote);") && hook.Contains("held = WorldEmote.Hold(target, line.Emote, minY)") &&
              hook.Contains("WorldEmote.Release(held)"),
            "T22: a bubble is held for its line and released; a burst plays once.");
        check(!Regex.IsMatch(hook, @"MoodOf|LineMood|EndsWith"), "T22: emotes are never chosen automatically from the text.");

        string factory = Src("Scripts/Data/CaseFactory.cs");
        check(factory.Contains("builder.Emote(line.emote, line.burst);") && factory.Contains("Unknown dialogue emote ignored"),
            "T22: case JSON lines load their emote; unknown tags are ignored with a warning.");
        string loader = Src("Scripts/SideQuest1/TownContentLoader.cs");
        check(loader.Contains("public string emote;") && loader.Contains("ToEmotes(t.talk, t.npcId, problems)") && loader.Contains("unknown emote"),
            "T22: town.json lines load their emote; unknown tags are reported.");
        check(Src("Scripts/Data/Definitions/CaseDefinition.cs").Contains("public string emote;"), "T22: the JSON line definition has the optional emote field.");
    }
}
