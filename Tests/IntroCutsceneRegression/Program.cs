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
Check(track != null && track.lines.Count == 14, "subtitle JSON parses with 14 lines");
Check(track.nextScene == "Office_Level1", "the cutscene ends in Office_Level1");
Check(Math.Abs(track.fadeInSeconds - 0.25f) < 1e-4 && Math.Abs(track.fadeOutSeconds - 0.25f) < 1e-4, "fades match the old intro (0.25 s)");
int[] expectedClips = { 1, 2, 3, 5, 6, 7, 8, 10, 10, 10, 10, 10, 14, 14 };
Check(track.lines.Select(l => l.clip).SequenceEqual(expectedClips), "lines belong to clips 1, 2, 3, 5, 6, 7, 8, 10 (x5), 14 (x2)");
Check(track.lines[0].title && track.lines[0].text.StartsWith("COAST TALES."), "clip 1 is the COAST TALES title card");
Check(track.lines[7].text.Contains("every case comes across my desk") && track.lines[11].text.Contains("decide what should be done"), "clip 10 lines in order");
Check(track.lines[13].text.Contains("Let's get to work."), "the last line is \"Let's get to work.\"");
bool ordered = true;
for (int i = 0; i < track.lines.Count; i++)
{
    if (track.lines[i].end <= track.lines[i].start) ordered = false;
    if (i > 0 && track.lines[i].start < track.lines[i - 1].end) ordered = false;
}
Check(ordered, "JSON times are positive and never overlap");
Check(track.lines.All(l => l.end <= track.fallbackDurationSeconds), "every line ends before the fallback length");
Check(track.LineAt(1.0)?.clip == 1 && track.LineAt(14.0) == null && track.LineAt(43.0)?.text.Contains("Check the numbers") == true, "LineAt picks the line on screen");

// ---------- timing from Timeline clips ----------
var starts = new List<double>(); var ends = new List<double>();
for (int i = 0; i < 14; i++) { starts.Add(i * 5.0); ends.Add(i * 5.0 + 5.0); }
var timed = JsonSerializer.Deserialize<CutsceneSubtitleTrack>(Read("Assets", "Resources", "Cutscenes", "cutscene01_subtitles.json"), options);
Check(timed.ApplyClipTimes(starts, ends), "14 Timeline clips time every line");
Check(Math.Abs(timed.lines[1].start - 5.0) < 1e-4 && Math.Abs(timed.lines[1].end - 10.0) < 1e-4 && !timed.lines[1].estimate, "a one-line clip fills its clip");
var clip10 = timed.lines.Where(l => l.clip == 10).ToList();
Check(Math.Abs(clip10[0].start - 45.0) < 1e-4 && Math.Abs(clip10[4].end - 50.0) < 1e-4 && clip10.Zip(clip10.Skip(1)).All(p => p.First.end < p.Second.start),
    "clip 10's five lines share the clip in order with gaps");
var few = JsonSerializer.Deserialize<CutsceneSubtitleTrack>(Read("Assets", "Resources", "Cutscenes", "cutscene01_subtitles.json"), options);
Check(!few.ApplyClipTimes(starts.Take(10).ToList(), ends.Take(10).ToList()) && few.lines[1].estimate && Math.Abs(few.lines[1].start - track.lines[1].start) < 1e-4,
    "too few clips keeps the JSON times");

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
Check(menuEntry.Contains("if (!progress.HasFinishedFirstLevel)") && menuEntry.Contains("SceneFade.FadeOutThenLoad(Cutscene01Runtime.SceneName)") && menuEntry.Contains("cameraManager.SwitchToLevelSelector()"), "first-story Play bypasses the board, subsequent Play opens it");
Check(runtime.Contains("canSkip = track.showSkip && progress != null && progress.HasSeenLevelOneIntro") && runtime.Contains("if (canSkip) BuildSkipButton()") && runtime.Contains("if (canSkip) Finish()"), "first viewing hides Skip and rejects direct Skip calls; replay respects showSkip config");
Check(runtime.IndexOf("canSkip =", StringComparison.Ordinal) < runtime.IndexOf("BuildUi();", StringComparison.Ordinal) && runtime.IndexOf("MarkLevelOneIntroSeen()", StringComparison.Ordinal) > runtime.IndexOf("private void Finish()", StringComparison.Ordinal), "Skip policy is captured before UI builds and watching is recorded only on ending");
string resultUi = Read("Assets", "Scripts", "UI", "LevelCompleteUI.cs");
Check(resultUi.Contains("MenuEntry.OpenLevelSelect = true") && resultUi.Contains("SceneFade.FadeOutThenLoad(MenuEntry.MenuSceneName, fadeInDestination: true)"), "first completion reveals progress directly with a destination fade in");
string fade = Read("Assets", "Scripts", "UI", "HUD", "SceneFade.cs");
Check(fade.Contains("EnsureForScene(SceneManager.GetActiveScene(), force: true)") && fade.Contains("instance.black.alpha = 0f"), "initial Play creates an outgoing fade on the menu instead of cutting to the intro");

Check(runtime.Contains("track.lines.Clear();") && !runtime.Contains("UpdateSubtitle(usingTimelineClock"),
    "Cutscene 2.0 does not display legacy overlay subtitles");
Console.WriteLine($"{checks - failures}/{checks} checks passed");
return failures == 0 ? 0 : 1;
