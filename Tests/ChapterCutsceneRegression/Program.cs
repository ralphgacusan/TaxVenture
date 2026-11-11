using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

// Chapter cutscene regression (Documentation/ChapterCutscenes, T01 onwards).
// Compiles the plain-C# contract (ChapterCinematicScript.cs) and checks:
// - the shared presentation clock: fixed 15 s, fades, once-only natural/Skip/abort finish, pause, clamped steps;
// - the authored T01 data (chapter01_end_script.json) against PRODUCTION_STANDARD timing/subtitle rules;
// - CS_Chapter01_End.unity bindings read from the scene YAML (no Unity needed);
// - that the presentation code never saves, rewards, routes or marks viewing, and the original intro is untouched.

int checks = 0, failures = 0, warnings = 0;
void Check(bool ok, string message)
{
    checks++;
    if (ok) Console.WriteLine("PASS: " + message);
    else { failures++; Console.WriteLine("FAIL: " + message); }
}
void Warn(bool ok, string message)
{
    if (ok) Console.WriteLine("PASS: " + message);
    else { warnings++; Console.WriteLine("WARN: " + message); }
}

// The project root: above the build output (in-tree builds) or above the working directory (out-of-tree builds).
string FindRoot(string start)
{
    string dir = start;
    while (dir != null && !(Directory.Exists(Path.Combine(dir, "Assets")) && Directory.Exists(Path.Combine(dir, "ProjectSettings"))))
        dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
    return dir;
}
string root = FindRoot(AppContext.BaseDirectory) ?? FindRoot(Directory.GetCurrentDirectory());
if (root == null) { Console.WriteLine("FAIL: project root not found above the build output or the working directory"); return 1; }
string PathOf(params string[] parts) => Path.Combine(new[] { root }.Concat(parts).ToArray());
string Read(params string[] parts) => File.ReadAllText(PathOf(parts));
string GuidOf(params string[] parts) => Regex.Match(Read(parts) , @"guid: ([0-9a-f]{32})").Groups[1].Value;
string Sha(params string[] parts) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(PathOf(parts)))).ToLowerInvariant().Substring(0, 16);

var json = new JsonSerializerOptions { IncludeFields = true };
const double Eps = 1e-4;

// =====================================================================
// 1. Authored T01 script
// =====================================================================
string scriptPath = Path.Combine("Assets", "Cutscenes", "Chapter01", "chapter01_end_script.json");
ChapterCinematicScript s = JsonSerializer.Deserialize<ChapterCinematicScript>(Read(scriptPath), json);
Check(s != null, "chapter01_end_script.json parses");
Check(s.id == "chapter01.end" && s.scene == "CS_Chapter01_End" && s.title == "A First Case Closed", "stable id chapter01.end, scene CS_Chapter01_End, title \"A First Case Closed\"");
List<string> problems = s.Validate();
Check(problems.Count == 0, "the script passes every authoring rule" + (problems.Count > 0 ? ": " + string.Join(" | ", problems) : ""));
Check(Math.Abs(s.duration - 15f) < Eps && Math.Abs(s.fadeInSeconds - 0.25f) < Eps && Math.Abs(s.fadeOutSeconds - 0.25f) < Eps, "exactly 15.000 s, fade in 0.00-0.25, fade out 14.75-15.00");
Check(s.shotStarts.Length == 3 && Math.Abs(s.shotStarts[0]) < Eps && Math.Abs(s.shotStarts[1] - 4.8f) < Eps && Math.Abs(s.shotStarts[2] - 10.1f) < Eps, "three shots: 0.00-4.80, 4.80-10.10, 10.10-15.00");
Check(s.cues.Count == 2, "two subtitle cues; the folder shot is silent");
ChapterCinematicCue marie = s.cues.ElementAtOrDefault(0), arthur = s.cues.ElementAtOrDefault(1);
Check(marie != null && marie.speaker == "Marie" && marie.text == "Thank you. I understand it now." && Math.Abs(marie.start - 0.6f) < Eps && Math.Abs(marie.end - 4.6f) < Eps,
    "Marie: \"Thank you. I understand it now.\" 0.60-4.60");
Check(arthur != null && arthur.speaker == "Arthur" && arthur.text == "Good work. Another file is waiting." && Math.Abs(arthur.start - 5.2f) < Eps && Math.Abs(arthur.end - 9.8f) < Eps,
    "Arthur: \"Good work. Another file is waiting.\" 5.20-9.80");
Check(s.cues.All(c => c.end <= s.shotStarts[2]), "no cue reaches the Bayview folder shot");
Check(s.cues.All(c => c.start >= s.fadeInSeconds && c.end <= s.duration - s.fadeOutSeconds), "no cue starts under black or outlives the last readable frame");
Check(s.cues.All(c => s.EstimatedLines(c) <= 2), "every cue fits two lines");
Check(s.cues.All(c => (c.text + c.speaker).All(ch => ch >= ' ' && ch <= '~')), "cue text is plain ASCII (no glyphs the UI font may lack)");
foreach (string word in new[] { "click", "mouse", "wasd", "New Horizon", "CoastCare", "pay", "tax paid" })
    Check(!s.cues.Any(c => c.text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0), "cues avoid \"" + word + "\"");
foreach (ChapterCinematicCue c in s.cues)
    Check(c.ReadLength / (c.end - c.start) <= ChapterCinematicScript.MaxCharsPerSecond, $"{c.speaker} reads at {c.ReadLength / (c.end - c.start):0.0} chars/s");

// =====================================================================
// 2. Presentation timing helpers
// =====================================================================
Check(Math.Abs(s.CurtainAlpha(0) - 1f) < Eps && Math.Abs(s.CurtainAlpha(0.25)) < Eps && Math.Abs(s.CurtainAlpha(7)) < Eps
      && Math.Abs(s.CurtainAlpha(14.75)) < Eps && Math.Abs(s.CurtainAlpha(15) - 1f) < Eps, "curtain: black, clear by 0.25, clear until 14.75, black at 15");
bool monotonic = true;
for (double t = 0; t < 0.25; t += 0.01) if (s.CurtainAlpha(t + 0.01) > s.CurtainAlpha(t) + 1e-6) monotonic = false;
for (double t = 14.75; t < 15; t += 0.01) if (s.CurtainAlpha(t + 0.01) < s.CurtainAlpha(t) - 1e-6) monotonic = false;
Check(monotonic, "fades are monotonic");
Check(s.CueAt(0.59, "") == null && s.CueAt(0.601, "") == marie && s.CueAt(4.59, "") == marie && s.CueAt(4.601, "") == null && s.CueAt(7, "") == arthur && s.CueAt(12, "") == null,
    "CueAt shows complete cues only inside their intervals");
Check(Math.Abs(s.CueAlpha(marie, 0.6)) < Eps && Math.Abs(s.CueAlpha(marie, 0.65) - 0.5f) < 1e-3 && Math.Abs(s.CueAlpha(marie, 2) - 1f) < Eps && s.CueAlpha(marie, 4.55) < 0.51f,
    "cue opacity eases over 0.1 s inside the cue");
Check(s.ShotAt(0) == 0 && s.ShotAt(4.79) == 0 && s.ShotAt(4.801) == 1 && s.ShotAt(10.09) == 1 && s.ShotAt(10.101) == 2 && s.ShotAt(15) == 2, "ShotAt follows the shot starts");

var variantScript = new ChapterCinematicScript { id = "t", cues = new List<ChapterCinematicCue> {
    new ChapterCinematicCue { speaker = "A", text = "default", start = 1, end = 3 },
    new ChapterCinematicCue { speaker = "B", text = "warm", start = 4, end = 6, variant = "warm" } }, shotStarts = new float[] { 0 } };
Check(variantScript.CueAt(5, "") == null && variantScript.CueAt(5, "warm")?.text == "warm" && variantScript.CueAt(2, "warm")?.text == "default", "variant cues show only for their context");

// Validation catches broken authoring.
ChapterCinematicScript Broken(Action<ChapterCinematicScript> edit)
{
    ChapterCinematicScript b = JsonSerializer.Deserialize<ChapterCinematicScript>(Read(scriptPath), json);
    edit(b);
    return b;
}
Check(Broken(b => b.duration = 16).Validate().Any(p => p.Contains("Duration")), "Validate rejects a non-15 s duration");
Check(Broken(b => b.cues[0].start = 0.1f).Validate().Any(p => p.Contains("readable interval")), "Validate rejects a cue under the opening fade");
Check(Broken(b => b.cues[1].start = 4.0f).Validate().Any(p => p.Contains("overlaps")), "Validate rejects overlapping cues");
Check(Broken(b => b.cues[0].text = string.Concat(Enumerable.Repeat("A long line of words ", 6))).Validate().Any(p => p.Contains("chars/s") || p.Contains("lines")), "Validate rejects an unreadable cue");
Check(Broken(b => b.cues[0].speaker = "").Validate().Any(p => p.Contains("no speaker")), "Validate requires a speaker");
Check(Broken(b => b.shotStarts = new float[] { 0, 10.1f, 4.8f }).Validate().Any(p => p.Contains("Shot")), "Validate rejects out-of-order shots");

// =====================================================================
// 3. Session: once-only finish, Skip, pause, abort
// =====================================================================
{
    var session = new ChapterCinematicSession(s);
    int doneFrames = 0; double doneAt = -1;
    for (int frame = 0; frame < 60 * 20; frame++)
        if (session.Advance(1d / 60d)) { doneFrames++; doneAt = session.Time; }
    Check(doneFrames == 1 && Math.Abs(doneAt - 15) < Eps && session.Outcome == ChapterCinematicOutcome.Completed, "natural run finishes once at t = 15.000 (Completed)");
    Check(session.TakeFinished(out ChapterCinematicResult r) && r.Id == "chapter01.end" && r.Outcome == ChapterCinematicOutcome.Completed && r.Watched, "the Completed result is delivered with the stable id");
    Check(!session.TakeFinished(out _), "the result is delivered only once");
    Check(!session.RequestSkip() && !session.Abort() && session.Outcome == ChapterCinematicOutcome.Completed, "Skip/abort after the end change nothing");
}
{
    var session = new ChapterCinematicSession(s);
    session.Advance(5);
    Check(Math.Abs(session.Time - ChapterCinematicSession.MaxStep) < Eps, "a long frame (resume/hitch) advances at most one clamped step");
    session.Paused = true;
    session.Advance(0.05);
    Check(Math.Abs(session.Time - ChapterCinematicSession.MaxStep) < Eps, "a paused session does not advance");
    session.Paused = false;
}
{
    var session = new ChapterCinematicSession(s);
    session.Advance(0.1);
    Check(!session.SkipAvailable && !session.RequestSkip(), "Skip is not offered under the opening fade");
    while (session.Time < 6.0) session.Advance(1d / 60d);
    Check(session.SkipAvailable && session.RequestSkip(), "Skip is offered mid-scene (every viewing; no seen-flag gate)");
    Check(!session.RequestSkip(), "a second Skip tap is ignored");
    double skippedAt = session.FinishTime;
    float last = session.CurtainAlpha; bool rising = true; int doneFrames = 0, frames = 0;
    while (!session.Done && frames < 600) { if (session.Advance(1d / 60d)) doneFrames++; if (session.CurtainAlpha < last - 1e-6) rising = false; last = session.CurtainAlpha; frames++; }
    Check(doneFrames == 1 && rising && Math.Abs(last - 1f) < Eps, "Skip fades from the current opacity to black, once");
    Check(frames <= 16 && frames >= 14, $"the Skip fade lasts fadeOutSeconds ({frames} frames at 60 fps)");
    Check(session.TakeFinished(out ChapterCinematicResult r) && r.Outcome == ChapterCinematicOutcome.Skipped && Math.Abs(r.PresentationTime - skippedAt) < Eps && r.Watched,
        "Skipped result carries the skip time and counts as watched (same continuation)");
    Check(!session.TakeFinished(out _), "the Skipped result is delivered only once");
}
{
    var session = new ChapterCinematicSession(s);
    while (session.Time < 14.85) session.Advance(1d / 60d);
    float before = session.CurtainAlpha;
    session.RequestSkip();
    Check(Math.Abs(session.CurtainAlpha - before) < 0.02f, "Skip during the closing fade continues from the current opacity");
    while (!session.Done) session.Advance(1d / 60d);
    Check(session.Outcome == ChapterCinematicOutcome.Skipped, "late Skip still finishes once");
}
{
    var session = new ChapterCinematicSession(s);
    session.Advance(1d / 60d);
    Check(session.Abort() && session.Done && session.TakeFinished(out ChapterCinematicResult r) && r.Outcome == ChapterCinematicOutcome.Aborted && !r.Watched,
        "abort delivers Aborted (not a watched result)");
    Check(!session.Abort() && !session.Teardown(), "abort is once-only");
}
{
    var session = new ChapterCinematicSession(s);
    while (session.Time < 3) session.Advance(1d / 60d);
    session.RequestSkip();
    session.Advance(0.05);
    Check(session.Teardown() && session.TakeFinished(out ChapterCinematicResult r) && r.Outcome == ChapterCinematicOutcome.Skipped, "teardown during the Skip fade completes as Skipped");
}
{
    var session = new ChapterCinematicSession(s);
    while (session.Time < 8) session.Advance(1d / 60d);
    Check(session.Teardown() && session.Outcome == ChapterCinematicOutcome.Aborted, "teardown before any finish is Aborted");
}

// =====================================================================
// 4. Scene bindings (CS_Chapter01_End.unity, read as YAML)
// =====================================================================
string scenePath = Path.Combine("Assets", "Scenes", "Cutscenes", "CS_Chapter01_End.unity");
Check(File.Exists(PathOf(scenePath)) && File.Exists(PathOf(scenePath + ".meta")), "CS_Chapter01_End.unity and its meta exist");
string scene = Read(scenePath);
var docs = new Dictionary<string, (int cls, string body, bool stripped)>();
foreach (Match m in Regex.Matches(scene, @"^--- !u!(\d+) &(-?\d+)( stripped)?\n(.*?)(?=^--- |\z)", RegexOptions.Multiline | RegexOptions.Singleline))
    docs[m.Groups[2].Value] = (int.Parse(m.Groups[1].Value), m.Groups[4].Value, m.Groups[3].Success);
Check(docs.Count > 1000, $"scene parses ({docs.Count} objects)");

string G(string folder, string file) => GuidOf(folder, file + ".meta");
string playerGuid = G(Path.Combine("Assets", "Scripts", "Cutscenes", "Chapters"), "ChapterCinematicPlayer.cs");
string shotGuid = G(Path.Combine("Assets", "Scripts", "Cutscenes", "Chapters"), "ChapterCinematicShot.cs");
string actorGuid = G(Path.Combine("Assets", "Scripts", "Cutscenes", "Chapters"), "ChapterCinematicActor.cs");
string moveGuid = G(Path.Combine("Assets", "Scripts", "Cutscenes", "Chapters"), "ChapterCinematicPropMove.cs");
string soundGuid = G(Path.Combine("Assets", "Scripts", "Cutscenes", "Chapters"), "ChapterCinematicSound.cs");
var allowedScripts = new HashSet<string> { playerGuid, shotGuid, actorGuid, moveGuid, soundGuid,
    "a79441f348de89743a2939f4d699eac1" /* URP camera data */, "474bcb49853aa07438625e644c072ee6" /* URP light data */ };
var scriptsInScene = Regex.Matches(scene, @"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})").Select(m => m.Groups[1].Value).ToList();
Check(scriptsInScene.All(allowedScripts.Contains), "the scene carries no gameplay, save, AI or HUD component (only cinematic + URP data scripts)");
string Field(string body, string name) => Regex.Match(body, @"^  " + Regex.Escape(name) + @": (.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
List<string> Refs(string body, string list) =>
    Regex.Match(body, @"^  " + list + @":\n((?:  - \{fileID: -?\d+\}\n)*)", RegexOptions.Multiline).Groups[1].Value
        .Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => Regex.Match(l, @"-?\d+").Value).ToList();
string LocalRef(string value) => Regex.Match(value, @"\{fileID: (-?\d+)\}").Groups[1].Value;
var monos = docs.Where(d => d.Value.cls == 114).ToList();
var players = monos.Where(d => d.Value.body.Contains(playerGuid)).ToList();
Check(players.Count == 1, "exactly one ChapterCinematicPlayer");
string player = players.FirstOrDefault().Value.body ?? "";
Check(Field(player, "scriptAsset").Contains(GuidOf(scriptPath + ".meta")), "the player reads chapter01_end_script.json");
string cameraId = LocalRef(Field(player, "cinematicCamera"));
Check(docs.TryGetValue(cameraId, out var cam) && cam.cls == 20, "the player is bound to the scene camera");
Check(docs.Values.Count(d => d.cls == 20) == 1 && docs.Values.Count(d => d.cls == 81) == 1, "one camera and one AudioListener in the scene");
var shotIds = Refs(player, "shots");
var shotBodies = shotIds.Select(id => docs.TryGetValue(id, out var d) ? d.body : "").ToList();
Check(shotIds.Count == 3 && shotBodies.All(b => b.Contains(shotGuid)), "three ChapterCinematicShot bindings");
float F(string body, string name) => float.Parse(Field(body, name), System.Globalization.CultureInfo.InvariantCulture);
Check(shotBodies.Count == 3 && shotBodies.Select(b => F(b, "startTime")).SequenceEqual(s.shotStarts), "shot start times match the script");
bool shotsOk = true;
for (int i = 0; i < shotBodies.Count; i++)
{
    string b = shotBodies[i];
    float next = i + 1 < shotBodies.Count ? F(shotBodies[i + 1], "startTime") : s.duration;
    float fov = F(b, "fieldOfView");
    if (fov < 34 || fov > 50) shotsOk = false;
    if (F(b, "moveStart") < F(b, "startTime") - Eps || F(b, "moveEnd") > next + Eps) shotsOk = false;
    if (LocalRef(Field(b, "targetCamera")) != cameraId) shotsOk = false;
    foreach (string pose in new[] { "from", "to" })
        if (!docs.TryGetValue(LocalRef(Field(b, pose)), out var t) || t.cls != 4) shotsOk = false;
}
Check(shotsOk, "each shot: one move inside its own interval, steady 34-50 degree lens, bound poses and camera");

var elementIds = Refs(player, "elements");
var elementBodies = elementIds.Select(id => docs.TryGetValue(id, out var d) ? d.body : "").ToList();
Check(elementIds.Count == 6 && elementBodies.Count(b => b.Contains(actorGuid)) == 3 && elementBodies.Count(b => b.Contains(moveGuid)) == 1 && elementBodies.Count(b => b.Contains(soundGuid)) == 2,
    "elements: three actors, one prop move, two sounds");
var actors = elementBodies.Where(b => b.Contains(actorGuid)).ToList();
string ActorNamed(string state) => actors.FirstOrDefault(a => Field(a, "stateName") == state) ?? "";
string marieActor = ActorNamed("Idle Relaxed"), arthurActor = ActorNamed("Briefcase Idle"), elijahActor = actors.FirstOrDefault(a => Field(a, "sampledClip").Contains("4d8c6cb1568c845489cd64e97a4ff7e6")) ?? "";
Check(marieActor.Length > 0 && arthurActor.Length > 0 && elijahActor.Length > 0, "Marie (Idle Relaxed), Arthur (Briefcase Idle) and Elijah (sampled npc_elijah_Idle) are bound");
Check(Field(marieActor, "visibleWindow") == "{x: 0, y: 4.8}" && Field(arthurActor, "visibleWindow") == "{x: 4.8, y: 10.1}", "Marie is on stage in shot 1 only; Arthur enters at the cut to shot 2");
Check(!LocalRef(Field(marieActor, "heldProp")).Equals("0") && docs.ContainsKey(LocalRef(Field(marieActor, "heldProp"))), "Marie already holds her delivered copy (held prop bound; no handover action)");
foreach (string a in actors)
{
    string animator = LocalRef(Field(a, "animator"));
    if (animator != "0") Check(docs.TryGetValue(animator, out var an) && an.cls == 95, "humanoid actor bound to its Animator (" + Field(a, "stateName") + ")");
}
Check(scene.Contains("objectReference: {fileID: 9100000, guid: 2e8c12097e1e471a86e5e5d07745decd, type: 2}"), "humanoids use the shared NpcHumanoid controller by reference");
Check(!Regex.IsMatch(scene, @"propertyPath: m_ApplyRootMotion\n\s*value: 1"), "no actor uses root motion");
var times = Regex.Matches(string.Join("\n", actors), @"^\s+(?:- )?start: (-?[\d.]+)\n\s+duration: ([\d.]+)", RegexOptions.Multiline)
    .Select(m => double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) + double.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
Check(times.All(t => t <= 15 + Eps), "every authored turn/nod ends inside the 15 s presentation");

string move = elementBodies.First(b => b.Contains(moveGuid));
Check(F(move, "start") >= s.shotStarts[2] && F(move, "end") <= s.duration - s.fadeOutSeconds - 3f, "the folder settles early in shot 3 and holds for at least 3 s before the fade");
string folderPrefabGuid = GuidOf("Assets", "Cutscenes", "Shared", "Props", "BayviewFolder.prefab.meta");
string target = LocalRef(Field(move, "target"));
Check(docs.TryGetValue(target, out var folderT) && folderT.stripped && folderT.body.Contains(folderPrefabGuid), "the prop move drives the shared Bayview folder prefab instance");
var sounds = elementBodies.Where(b => b.Contains(soundGuid)).ToList();
Check(sounds.All(b => F(b, "start") >= 0 && F(b, "end") <= s.duration + Eps), "sounds live inside the presentation");
Check(sounds.Count(b => Field(b, "loop") == "0") == 1 && sounds.Where(b => Field(b, "loop") == "0").All(b => F(b, "start") >= F(move, "start") && F(b, "start") <= F(move, "end")),
    "exactly one paper cue, at the folder placement");
Check(sounds.Any(b => Field(b, "clip").Contains("8b6bf1cc7df71cc4bbb9f462f5e1df28")) && sounds.Any(b => Field(b, "clip").Contains("c6e31739e87f89c44bac99ca23c86a8d")),
    "reuses the existing office ambience and sfx_paper_place_01");

string prefab = Read("Assets", "Cutscenes", "Shared", "Props", "BayviewFolder.prefab");
Check(prefab.Contains("Bayview") && prefab.Contains("Property Services") && !Regex.IsMatch(prefab, "New Horizon|CoastCare|logo", RegexOptions.IgnoreCase),
    "the Bayview folder reads \"Bayview Property Services\" with no ownership mark");
Check(prefab.Contains(GuidOf("Assets", "Cutscenes", "Shared", "Materials", "M_BayviewFolder_BlueGray.mat.meta")) && prefab.Contains("8f586378b4e144a9851e7b34d9b748ee"),
    "the folder uses the shared blue-gray material and the HUD font (LiberationSans SDF)");

// =====================================================================
// 5. Presentation code is read-only; the original intro is untouched
// =====================================================================
string chapters = string.Join("\n", Directory.GetFiles(PathOf("Assets", "Scripts", "Cutscenes", "Chapters"), "ChapterCinematic*.cs").Select(File.ReadAllText));
foreach (string forbidden in new[] { "PlayerProgressManager", "SceneManager.LoadScene", "LoadSceneAsync", "MarkLevelOneIntroSeen", "ApplyLevelResult", "CommitFollowThrough",
    "CommitLevelOneWrapUp", "PlayerPrefs.Set", "timeScale", "ClientCopyHandover", "CaseFinale", "LevelCompleteUI", "Trust" })
    Check(!chapters.Contains(forbidden), "chapter cinematic code never touches " + forbidden);
Check(!Regex.IsMatch(chapters, @"FindObjects?(ByType|OfType)<ChapterCinematic|Find\(\""|GameObject\.Find"), "no scene scanning for actors/cameras (bindings are serialized)");
string playerSource = Read("Assets", "Scripts", "Cutscenes", "Chapters", "ChapterCinematicPlayer.cs");
string introSource = Read("Assets", "Scripts", "Cutscenes", "Cutscene01Runtime.cs");
foreach (string token in new[] { "new Vector2(-32f, -32f)", "new Vector2(200f, 84f)", "\"Skip\", 36f, HudTheme.BrassSoft, TextAlignmentOptions.Center, FontStyles.Bold", "HudTheme.CreatePanel(\"Panel\", frame)" })
    Check(playerSource.Contains(token) && introSource.Contains(token), "Skip matches the intro's construction: " + token);
Check(!playerSource.Contains("HasSeenLevelOneIntro") && !playerSource.Contains("ConsumeUnskippableOpening"), "chapter scenes show Skip on the first viewing (no seen-flag gate)");
Check(playerSource.Contains("public static event Action<ChapterCinematicResult> Finished"), "completion contract: static Finished event");
Check(introSource.Contains("SceneName = \"Cutscene2.0\"") && introSource.Contains("MarkLevelOneIntroSeen"), "the original intro runtime keeps its own route and viewing rule");
string build = Read("ProjectSettings", "EditorBuildSettings.asset");
Check(build.Contains("CS_Chapter01_End") && build.Contains(GuidOf(scenePath + ".meta")), "T04 registers the Chapter 1 ending with its existing GUID");
Check(!Read("Assets", "Resources", "Career", "chapters.json").Contains("CS_Chapter"), "no campaign route points at chapter scenes yet");
// T01 authoring baseline (recorded 6 Nov 2026 before T01 files were written). A later, deliberate change elsewhere reports WARN, not FAIL.
Check(introSource.Contains("new ChapterCinematicPlayer.SubtitleView(safeRoot)") && !introSource.Contains("track.lines.Clear();"), "user-requested intro captions reuse the chapter renderer; legacy no-overlay policy superseded");
Warn(Sha("Assets", "Scenes", "Cutscene2.0.unity") == "6e1c56bb93c898af", "Cutscene2.0.unity unchanged since T01 authoring");
Warn(Sha("Assets", "EnvTimeline.playable") == "20e51540304425b4", "EnvTimeline.playable unchanged since T01 authoring");
Warn(Sha("Assets", "AssetIntegration", "NpcHumanoid.controller") == "cfa3af009de7b47e", "NpcHumanoid.controller unchanged since T01 authoring");

Chapter02Checks.Run(root, Check);
Chapter03Checks.Run(root, Check);
VoiceChecks.Run(root, Check);

Console.WriteLine($"\n{checks - failures}/{checks} checks passed, {warnings} warning(s).");
return failures == 0 ? 0 : 1;
