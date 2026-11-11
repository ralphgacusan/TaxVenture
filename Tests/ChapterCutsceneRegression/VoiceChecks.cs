using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

public static class VoiceChecks
{
    public static void Run(string root, Action<bool, string> check)
    {
        var cue = new ChapterCinematicCue { voice = "test", speaker = "Marie", text = "Thank you.", start = .6f, end = 4.6f };
        check(ChapterCinematicVoiceRules.Matches(cue, "test", "Marie", "Thank you.", ""), "voice resolves exact source line");
        check(!ChapterCinematicVoiceRules.Matches(cue, "other", "Marie", "Thank you.", ""), "wrong recording ID stays text-only");
        check(!ChapterCinematicVoiceRules.Matches(cue, "test", "Rosa", "Thank you.", ""), "wrong speaker stays text-only");
        check(!ChapterCinematicVoiceRules.Matches(cue, "test", "Marie", "Changed text.", ""), "stale recording stays text-only");
        check(!ChapterCinematicVoiceRules.Matches(cue, "test", "Marie", "Thank you.", "rosa.neutral"), "alternate variant cannot use this recording");
        check(ChapterCinematicVoiceRules.Fits(cue, 2.7) && !ChapterCinematicVoiceRules.Fits(cue, 4.0), "voice fits with a quarter-second tail, never sped up");
        check(!ChapterCinematicVoiceRules.Fits(cue, 0) && !ChapterCinematicVoiceRules.Fits(cue, double.NaN), "empty or invalid recording rejected");
        check(ChapterCinematicVoiceRules.LimitStep(4.4, .066, cue, true, true, false, .1f) < .051,
            "delayed speech holds the master before subtitle fade and shot change");
        check(ChapterCinematicVoiceRules.LimitStep(4.46, .066, cue, true, true, false, .1f) == 0,
            "picture remains held while words are unfinished");
        foreach (var flags in new[] { (false, true, false), (true, false, false), (true, true, true) })
            check(ChapterCinematicVoiceRules.LimitStep(4.46, .066, cue, flags.Item1, flags.Item2, flags.Item3, .1f) == .066,
                "finished/muted/Skip speech cannot block continuation " + flags);
        check(ChapterCinematicVoiceRules.LimitStep(1, .016, cue, true, true, false, .1f) == .016, "normal voice never slows the clock");
        var delayed = new ChapterCinematicSession(new ChapterCinematicScript());
        // A deliberately delayed native take is still speaking after its normal
        // window. Both caption and camera must stay before the boundary.
        for (int frame = 0; frame < 160; frame++)
            delayed.Advance(ChapterCinematicVoiceRules.LimitStep(delayed.Time, .05, cue,
                delayed.Time >= cue.start, true, false, .1f));
        check(delayed.Time > 4.4 && delayed.Time < cue.end && !delayed.Done,
            "delayed native speech cannot advance into the next shot or disappear with its subtitle");
        delayed.Paused = true;
        double held = delayed.Time;
        delayed.Advance(.05);
        check(delayed.Time == held, "focus pause preserves a speech-held frame");
        delayed.Paused = false;
        int completions = 0;
        for (int frame = 0; frame < 400; frame++)
        {
            delayed.Advance(ChapterCinematicVoiceRules.LimitStep(delayed.Time, .05, cue, false, true, false, .1f));
            if (delayed.TakeFinished(out var result)) completions++;
        }
        check(completions == 1 && delayed.Time == 15, "speech ending releases the hold and completes exactly once at authored time 15");
        var ids = new HashSet<string>();
        int count = 0;
        foreach (string path in Directory.GetFiles(Path.Combine(root, "Assets/Cutscenes"), "*_script.json", SearchOption.AllDirectories))
        {
            var script = JsonSerializer.Deserialize<ChapterCinematicScript>(File.ReadAllText(path), new JsonSerializerOptions { IncludeFields = true });
            string catalogue = File.ReadAllText(Path.Combine(root, "Assets/Resources/Cutscenes/Voice", script.id.Replace('.', '_') + ".asset"));
            foreach (var line in script.cues)
            {
                count++;
                check(!string.IsNullOrEmpty(line.voice) && ids.Add(line.voice), "unique voice ID " + line.voice);
                check(catalogue.Contains("id: " + line.voice) && catalogue.Contains(JsonSerializer.Serialize(line.text,
                    new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })), "catalogue binds current line " + line.voice);
                check(File.Exists(Path.Combine(root, "Assets/AudioManager/Cutscene_Dialogues", line.voice + ".mp3.meta")), "import metadata exists " + line.voice);
            }
            check(script.Validate().Count == 0 && script.duration == 15, "voiced script preserves authoring rules " + script.id);
        }
        check(count == 14 && ids.Count == 14, "all 14 supplied recordings are mapped");
    }
}
