using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

// Intro cutscene regression: Career Chapter 1 START routes to Cutscene2.0,
// the scene is in the build exactly once, the old intro stays on disk, and
// Resources/Cutscenes/cutscene01_subtitles.json parses with the expected lines.

int checks = 0, failures = 0;
void Check(bool ok, string message)
{
    checks++;
    if (ok) Console.WriteLine("PASS: " + message);
    else { failures++; Console.WriteLine("FAIL: " + message); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
const string Scene = "Cutscene2.0";

// ---------- routing ----------
using (JsonDocument doc = JsonDocument.Parse(Read("Assets", "Resources", "Career", "chapters.json")))
{
    JsonElement first = doc.RootElement.GetProperty("chapters")[0];
    Check(first.GetProperty("id").GetString() == "Level_01" && first.GetProperty("scene").GetString() == Scene,
        "chapters.json: Chapter 1 START loads " + Scene);
}
string service = Read("Assets", "Scripts", "RewardsSystem", "CareerProgressService.cs");
Check(Regex.IsMatch(service, "id = \"Level_01\"[^}]*scene = \"" + Regex.Escape(Scene) + "\""), "CareerProgressService fallback: Chapter 1 loads " + Scene);
Check(!service.Contains("Level1_Character_Intro_Cutscene"), "CareerProgressService no longer routes to the old intro");
Check(Read("Assets", "Scripts", "UI", "ProgressUI.cs").Contains("levelOneCutscene = \"" + Scene + "\""), "ProgressUI default routes to " + Scene);
foreach (string menu in new[] { "3D Menu.unity", "Progress.unity" })
    Check(Read("Assets", "Scenes", menu).Contains("levelOneCutscene: " + Scene), menu + ": serialized ProgressUI routes to the current intro");

// ---------- build settings ----------
string build = Read("ProjectSettings", "EditorBuildSettings.asset");
string sceneGuid = Regex.Match(Read("Assets", "Scenes", Scene + ".unity.meta"), @"guid: ([0-9a-f]{32})").Groups[1].Value;
var entries = Regex.Matches(build, @"- enabled: (\d)\s+path: (.+?)\r?\n\s+guid: ([0-9a-f]{32})");
var cutsceneEntries = entries.Where(m => m.Groups[2].Value.Trim() == "Assets/Scenes/" + Scene + ".unity").ToList();
Check(cutsceneEntries.Count == 1, "EditorBuildSettings lists " + Scene + " exactly once");
Check(cutsceneEntries.Count == 1 && cutsceneEntries[0].Groups[1].Value == "1" && cutsceneEntries[0].Groups[3].Value == sceneGuid,
    "the build entry is enabled and carries the scene's GUID");
Check(entries.Any(m => m.Groups[2].Value.Trim() == "Assets/Scenes/Office_Level1.unity" && m.Groups[1].Value == "1"), "Office_Level1 is still in the build");
Check(File.Exists(Path.Combine(root, "Assets", "Scenes", "Level1_Character_Intro_Cutscene.unity")), "the old intro scene is kept on disk");

// ---------- runtime ----------
string runtime = Read("Assets", "Scripts", "Cutscenes", "Cutscene01Runtime.cs");
Check(runtime.Contains("SceneName = \"" + Scene + "\""), "Cutscene01Runtime is gated on " + Scene);
Check(runtime.Contains("SubtitlePath = \"Cutscenes/cutscene01_subtitles\""), "Cutscene01Runtime reads the subtitle JSON");
foreach (string word in new[] { "click", "mouse", "wasd" })
    Check(!Regex.IsMatch(Read("Assets", "Resources", "Cutscenes", "cutscene01_subtitles.json"), word, RegexOptions.IgnoreCase), "subtitles avoid \"" + word + "\"");

// ---------- subtitle JSON ----------
var options = new JsonSerializerOptions { IncludeFields = true };
CutsceneSubtitleTrack track = JsonSerializer.Deserialize<CutsceneSubtitleTrack>(Read("Assets", "Resources", "Cutscenes", "cutscene01_subtitles.json"), options);
Check(track != null && track.lines.Count == 11, "subtitle JSON parses with 11 readable audio-aligned groups");
Check(track.nextScene == "Office_Level1", "the cutscene ends in Office_Level1");
Check(Math.Abs(track.fadeInSeconds - 0.25f) < 1e-4 && Math.Abs(track.fadeOutSeconds - 0.25f) < 1e-4, "fades match the old intro (0.25 s)");
int[] expectedClips = { 1, 2, 3, 5, 6, 7, 8, 10, 10, 10, 14 };
Check(track.lines.Select(l => l.clip).SequenceEqual(expectedClips), "lines belong to clips 1, 2, 3, 5, 6, 7, 8, 10 (x3), 14");
Check(track.lines[0].title && track.lines[0].text.StartsWith("Coast Tales."), "clip 1 retains its title content in the shared subtitle treatment");
Check(track.lines[7].text.Contains("every case comes across my desk") && track.lines[9].text.Contains("decide what should be done"), "clip 10 lines in order");
Check(track.lines[10].text.Contains("Let's get to work."), "the last line is \"Let's get to work.\"");
bool ordered = true;
for (int i = 0; i < track.lines.Count; i++)
{
    if (track.lines[i].end <= track.lines[i].start) ordered = false;
    if (i > 0 && track.lines[i].start < track.lines[i - 1].end) ordered = false;
}
Check(ordered, "JSON times are positive and never overlap");
Check(track.lines.All(l => l.end <= track.fallbackDurationSeconds), "every line ends before the fallback length");
Check(track.LineAt(1.0)?.clip == 1 && track.LineAt(14.0)?.clip == 5 && track.LineAt(38.0)?.text.Contains("Check the numbers") == true, "LineAt picks the line on screen");

// ---------- timing from Timeline clips ----------
var starts = new List<double>(); var ends = new List<double>();
for (int i = 0; i < 14; i++) { starts.Add(i * 5.0); ends.Add(i * 5.0 + 5.0); }
var timed = JsonSerializer.Deserialize<CutsceneSubtitleTrack>(Read("Assets", "Resources", "Cutscenes", "cutscene01_subtitles.json"), options);
Check(timed.ApplyClipTimes(starts, ends), "14 Timeline clips time every line");
Check(Math.Abs(timed.lines[1].start - 5.0) < 1e-4 && Math.Abs(timed.lines[1].end - 10.0) < 1e-4 && !timed.lines[1].estimate, "a one-line clip fills its clip");
var clip10 = timed.lines.Where(l => l.clip == 10).ToList();
Check(Math.Abs(clip10[0].start - 45.0) < 1e-4 && Math.Abs(clip10[2].end - 50.0) < 1e-4 && clip10.Zip(clip10.Skip(1)).All(p => p.First.end < p.Second.start),
    "legacy clip retiming remains compatible for grouped captions");
var few = JsonSerializer.Deserialize<CutsceneSubtitleTrack>(Read("Assets", "Resources", "Cutscenes", "cutscene01_subtitles.json"), options);
Check(!few.ApplyClipTimes(starts.Take(10).ToList(), ends.Take(10).ToList()) && few.lines[1].estimate == track.lines[1].estimate && Math.Abs(few.lines[1].start - track.lines[1].start) < 1e-4,
    "too few clips keeps the JSON times");

// ---------- actual dialogue audio and shared cue presentation ----------
var spans = new List<CutsceneAudioSpan>();
var timelineDocs = Regex.Matches(Read("Assets", "EnvTimeline.playable"), @"^--- !u!\d+ &(-?\d+)\r?\n(.*?)(?=^--- |\z)", RegexOptions.Multiline | RegexOptions.Singleline)
    .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Replace("\r", ""));
string audioTrack = timelineDocs.Values.Single(b => b.Contains("m_Name: Audio Track\n"));
foreach (string clip in Regex.Split(audioTrack, @"  - m_Version: ").Skip(1))
{
    double N(string field) => double.Parse(Regex.Match(clip, @"^    " + field + @": ([\d.eE+-]+)$", RegexOptions.Multiline).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    string asset = Regex.Match(clip, @"m_Asset: \{fileID: (-?\d+)\}").Groups[1].Value;
    string guid = Regex.Match(timelineDocs[asset], @"m_Clip: \{fileID: \d+, guid: ([a-f0-9]+)").Groups[1].Value;
    string meta = Directory.GetFiles(Path.Combine(root, "Assets", "Clip Audios"), "*.meta").Single(p => File.ReadAllText(p).Contains("guid: " + guid));
    string name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(meta));
    // Existing MP3s have one extra 24ms frame in the Timeline duration. The
    // runtime uses Unity's decoded length, not that padding (measured via afinfo).
    spans.Add(new CutsceneAudioSpan(name, N("m_Start"), N("m_Duration"), N("m_ClipIn"), N("m_TimeScale"), N("m_Duration") - .024));
}
Check(spans.Count == 9 && spans.Select(c => c.Name).SequenceEqual(new[] { "CLIP 01", "CLIP 02", "CLIP 03", "CLIP 05", "CLIP 06", "CLIP 07", "CLIP 08", "CLIP 10", "CLIP 14" }), "actual Timeline has nine named dialogue recordings (music excluded)");
Check(track.timelineTrack == "Audio Track" && track.lines.All(l => !l.estimate && !string.IsNullOrEmpty(l.audioClip)), "intro opts into real dialogue track with measured fallback, no numbered-track guessing");
Check(track.ApplyAudioClipTimes(spans), "all captions bind to actual dialogue audio");
foreach (var audio in spans)
{
    var cues = track.lines.Where(l => l.audioClip == audio.Name).ToList();
    Check(cues.Count > 0 && cues.All(c => c.start >= audio.Start - .0001 && c.end <= audio.Start + audio.SourceDuration + .0001), audio.Name + " captions stay within the recording");
    Check(Math.Abs(cues.Last().end - (audio.Start + audio.SourceDuration)) < .0001, audio.Name + " last caption ends with the audio, not an arbitrary hold");
}
var shared = track.SubtitlePresentation();
Check(shared.cues.Count == track.lines.Count && shared.cueFadeSeconds == .1f, "intro adapts existing timing to the same chapter cue opacity contract");
foreach (var cue in shared.cues)
{
    Check(shared.EstimatedLines(cue) <= 2 && cue.ReadLength / (cue.end - cue.start) <= ChapterCinematicScript.MaxCharsPerSecond,
        "readable two-line dialogue cue: " + cue.text);
    Check(shared.CueAt(cue.start + .01, "") == cue && shared.CueAlpha(cue, cue.start) == 0 && shared.CueAlpha(cue, cue.end) == 0,
        "shared complete-cue selection and within-audio fades: " + cue.text);
}
var phrase = new CutsceneSubtitleTrack { lines = new List<CutsceneSubtitleLine> {
    new CutsceneSubtitleLine { audioClip = "voice", audioStart = 2, audioEnd = 6, start = 90, end = 91 } } };
Check(phrase.ApplyAudioClipTimes(new[] { new CutsceneAudioSpan("voice", 10, 5, 1, 2, 20) })
    && phrase.lines[0].start == 10.5 && phrase.lines[0].end == 12.5, "source phrase offsets respect Timeline trim and playback speed");
Check(phrase.ApplyAudioClipTimes(new[] { new CutsceneAudioSpan("voice", 10, 1, 3, 1, 20) })
    && phrase.lines[0].start == 10 && phrase.lines[0].end == 11, "partially trimmed phrase clamps inside audible Timeline window");
foreach (var missing in new[] {
    new CutsceneAudioSpan[0],
    new[] { new CutsceneAudioSpan("other", 0, 9, 0, 1, 9) },
    new[] { new CutsceneAudioSpan("voice", 0, 9, 0, 1, 9), new CutsceneAudioSpan("voice", 20, 9, 0, 1, 9) },
    new[] { new CutsceneAudioSpan("voice", 0, 9, 0, 0, 9) },
    new[] { new CutsceneAudioSpan("voice", double.NaN, 9, 0, 1, 9) } })
{
    float before = phrase.lines[0].start, after = phrase.lines[0].end;
    Check(!phrase.ApplyAudioClipTimes(missing) && phrase.lines[0].start == before && phrase.lines[0].end == after,
        "missing/ambiguous/invalid dialogue preserves fallback atomically");
}
var partial = new CutsceneSubtitleTrack { lines = new List<CutsceneSubtitleLine> {
    new CutsceneSubtitleLine { audioClip = "voice", start = 4, end = 5 },
    new CutsceneSubtitleLine { audioClip = "absent", start = 6, end = 7 } } };
Check(!partial.ApplyAudioClipTimes(new[] { new CutsceneAudioSpan("voice", 30, 5, 0, 1, 5) }) && partial.lines[0].start == 4,
    "failed multi-clip binding cannot partially retime earlier lines");

// ---------- first story entry and replay eligibility (production save data) ----------
var fresh = new PlayerProgressData();
fresh.Normalize();
Check(!fresh.HasFinishedFirstLevel && !fresh.HasSeenLevelOneIntro, "fresh/reset career starts directly in the intro without Skip");
fresh.RecordTutorialSeen("Level_01/move");
fresh.RecordTrainingRun("move", false, 30f);
Check(!fresh.HasFinishedFirstLevel && !fresh.HasSeenLevelOneIntro, "training and tutorial history do not unlock career entry or intro Skip");
fresh.levelOneIntroSeen = true;
Check(fresh.HasSeenLevelOneIntro && !fresh.HasFinishedFirstLevel, "watched intro enables Skip on another attempt while Play still starts Level 1 directly");
var saved = JsonSerializer.Deserialize<PlayerProgressData>(JsonSerializer.Serialize(fresh, options), options);
Check(saved.HasSeenLevelOneIntro && !saved.HasFinishedFirstLevel, "intro viewing persists across save/load independently of completion");
fresh.RecordLevelResult("Level_01", 0, 0, 150, 12, false, null);
Check(fresh.HasFinishedFirstLevel && !fresh.IsLevelPassed("Level_01"), "finishing at any score reveals the career board without falsely passing Level 1");
var previousCareer = new PlayerProgressData();
previousCareer.RecordLevelCompletion("Level_01");
Check(!previousCareer.levelOneIntroSeen && previousCareer.HasSeenLevelOneIntro, "existing completed careers can skip without the new viewing flag");
var oldCareer = new PlayerProgressData { saveVersion = 0, levelsCompleted = 1 };
oldCareer.Normalize();
Check(oldCareer.HasFinishedFirstLevel && oldCareer.HasSeenLevelOneIntro, "legacy migrated completions keep normal Play routing and Skip");
var reset = new PlayerProgressData();
Check(!reset.HasFinishedFirstLevel && !reset.HasSeenLevelOneIntro, "Reset Progress restores the first-story flow and mandatory first viewing");
string menuEntry = Read("Assets", "Scripts", "UI", "MenuEntry.cs");
string mainMenuSource = Read("Assets", "Scripts", "MainMenu.cs");
string menuUi = Read("Assets", "Scripts", "UI", "MainMenuUI.cs");
Check(mainMenuSource.Contains("MenuEntry.Play(cameraManager)") && menuUi.Contains("MenuEntry.Play(FindAnyObjectByType<ToggleCamera>())"), "touch and UI Play share the first-story routing policy");
Check(menuEntry.Contains("SceneFade.FadeOutThenLoad(Cutscene01Runtime.SceneName, fadeInDestination: false)") && menuEntry.Contains("cameraManager.SwitchToLevelSelector()") &&
      menuEntry.Contains("if (HasCareerAccess)") && menuEntry.Contains("if (!CurrentModel().HasStory)"),
    "T03 menu: Play / New Game go straight to the opening; only saved Level 1 completion permits Continue; interrupted campaigns are reused");
Check(runtime.Contains("canSkip = !forceOpening && track.showSkip && progress != null && progress.HasSeenLevelOneIntro") && runtime.Contains("if (canSkip) BuildSkipButton()") && runtime.Contains("if (canSkip) Finish()"), "first viewing hides Skip and rejects direct Skip calls; replay respects showSkip config");
Check(runtime.IndexOf("canSkip =", StringComparison.Ordinal) < runtime.IndexOf("BuildUi();", StringComparison.Ordinal) && runtime.IndexOf("MarkLevelOneIntroSeen()", StringComparison.Ordinal) > runtime.IndexOf("private void Finish()", StringComparison.Ordinal), "Skip policy is captured before UI builds and watching is recorded only on ending");
string resultUi = Read("Assets", "Scripts", "UI", "LevelCompleteUI.cs");
Check(resultUi.Contains("MenuEntry.OpenLevelSelect = true") && resultUi.Contains("SceneFade.FadeOutThenLoad(MenuEntry.MenuSceneName, fadeInDestination: true)"), "first completion reveals progress directly with a destination fade in");
string fade = Read("Assets", "Scripts", "UI", "HUD", "SceneFade.cs");
Check(fade.Contains("EnsureForScene(SceneManager.GetActiveScene(), force: true)") && fade.Contains("instance.black.alpha = 0f"), "initial Play creates an outgoing fade on the menu instead of cutting to the intro");

Check(!runtime.Contains("track.lines.Clear();") && runtime.Contains("new ChapterCinematicPlayer.SubtitleView(safeRoot)"),
    "intro retains cues and reuses the existing chapter subtitle renderer");
Check(runtime.Contains("audio.clip.name, clip.start, clip.duration") && runtime.Contains("clip.clipIn, clip.timeScale, audio.clip.length")
    && runtime.Contains("track.ApplyAudioClipTimes(clips)"), "intro follows actual named dialogue audio windows, trims, speed and decoded length");
Check(runtime.Contains("subtitleDirector.time") && runtime.Contains("subtitleScript.CueAlpha(cue, time)") && runtime.Contains("subtitles?.Hide()")
    && runtime.Contains("subtitles?.Dispose()"), "shared cue fades use the audio director clock and clean up on exit");
string chapters = Read("Assets", "Scripts", "Cutscenes", "Chapters", "ChapterCinematicPlayer.cs");
Check(chapters.Contains("subtitles = new SubtitleView(safeRoot, subtitleFontSize") && chapters.Contains("public sealed class SubtitleView")
    && !runtime.Contains("HudTheme.CreateLabel(\"Line\""), "both presentations use one subtitle layout implementation");
Check(!runtime.Contains("master.state != PlayState.Playing || atEnd"), "paused Timeline does not prematurely finish the intro");
Console.WriteLine($"{checks - failures}/{checks} checks passed");
return failures == 0 ? 0 : 1;
