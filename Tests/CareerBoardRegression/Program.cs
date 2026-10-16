using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

// Career module regression: the projection the 3D Menu corkboards show, the
// "what changed since the last visit" diff, and chapters.json. Compiles the
// real CareerProgress.cs / PlayerProgressData.cs sources (no Unity needed).

int checks = 0, failures = 0;
void Check(bool ok, string message)
{
    checks++;
    if (ok) Console.WriteLine("PASS: " + message);
    else { failures++; Console.WriteLine("FAIL: " + message); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

// Level 1: 1 case -> 50 + 100 EXP and 4 + 3 + 5 hearts at 80% (rewards.json).
CareerChapterRules Rules(CareerChapterDef def) => def.id == "Level_01"
    ? new CareerChapterRules { passingPercent = 80, maxExp = 150, maxHearts = 12, badgeId = "know_your_taxpayer_1",
        badgeName = "Know Your Taxpayer", badgeEmblem = "taxpayer_id", badgeRibbon = "#2F8FD8" }
    : new CareerChapterRules { passingPercent = 80, maxExp = 350, maxHearts = 22 };

CareerCatalog Catalog()
{
    var catalog = new CareerCatalog();
    catalog.chapters.Add(new CareerChapterDef { id = "Level_01", number = 1, noteTitle = "Tutorial", title = "Tutorial Level",
        scene = "Level1_Character_Intro_Cutscene", cases = 1 });
    catalog.chapters.Add(new CareerChapterDef { id = "Level_02", number = 2, noteTitle = "Three cases", scene = "Office_Level2", cases = 3 });
    for (int n = 3; n <= 5; n++) catalog.chapters.Add(new CareerChapterDef { id = $"Level_{n:00}", number = n, comingSoon = true });
    catalog.sideQuests.Add(new CareerSideQuestDef { chapterId = "Level_01", title = "Know Your Taxpayer" });
    return catalog;
}

CareerSnapshot Build(PlayerProgressData data) => CareerProgress.Build(data, Catalog(), Rules);

// ---------- fresh save ----------
var fresh = Build(new PlayerProgressData());
Check(fresh.Available && fresh.ChapterCount == 5, "fresh save: five chapters, progress available");
Check(fresh.Chapters[0].Status == CareerChapterStatus.Available && fresh.Chapters[0].Launchable, "fresh: Chapter 1 playable");
Check(fresh.Chapters[0].Def.scene == "Level1_Character_Intro_Cutscene", "Chapter 1 routes to the intro cutscene");
Check(fresh.Chapters[1].Status == CareerChapterStatus.Locked && !fresh.Chapters[1].Launchable, "fresh: Chapter 2 locked");
Check(fresh.Chapters.Skip(2).All(c => c.Status == CareerChapterStatus.ComingSoon && !c.Launchable), "Chapters 3-5 coming soon, never launch");
Check(fresh.Completed == 0 && fresh.Passed == 0 && fresh.TotalExp == 0, "fresh: zero totals");
Check(fresh.Goal == fresh.Chapters[0], "fresh: goal is Chapter 1");
Check(fresh.Chapters[0].TargetExp == 120 && fresh.Chapters[0].TargetHearts == 10, "Level 1 pass line is 120 EXP + 10 hearts (80%, rounded up)");
Check(fresh.Badges.Count == 1 && fresh.Badges[0].Tier == 0 && fresh.Badges[0].Chapter == fresh.Chapters[0], "badge listed, not earned, tied to Chapter 1");
Check(CareerProgress.NextStep(fresh).Contains("120 EXP + 10 hearts") && CareerProgress.NextStep(fresh).Contains("unlock Chapter 2"),
    "next step names the pass line and the unlock");

// ---------- unavailable save ----------
var none = Build(null);
Check(!none.Available && none.Chapters[0].Status == CareerChapterStatus.Available && !none.Chapters[0].Launchable,
    "unreadable save: Chapter 1 shown but cannot launch");
Check(none.Chapters[1].Status == CareerChapterStatus.Locked, "unreadable save: Chapter 2 stays locked");
Check(CareerProgress.NextStep(none).Contains("could not be read"), "unreadable save explains itself");

// ---------- finished below the line ----------
var failedData = new PlayerProgressData();
failedData.RecordLevelResult("Level_01", 119, 12, 150, 12, false, null);
var failed = Build(failedData);
Check(failed.Chapters[0].Status == CareerChapterStatus.Finished && failed.Chapters[0].Launchable, "below the line: Finished, replayable");
Check(failed.Chapters[1].Status == CareerChapterStatus.Locked, "below the line: Chapter 2 stays locked");
Check(failed.Completed == 1 && failed.Passed == 0 && failed.Goal == failed.Chapters[0], "below the line: goal stays Chapter 1");
Check(Math.Abs(failed.Chapters[0].ExpToTarget - 119f / 120f) < 1e-4 && failed.Chapters[0].HeartsToTarget == 1f, "goal bars show best vs pass line");

// ---------- passed ----------
var passedData = new PlayerProgressData();
passedData.RecordLevelResult("Level_01", 150, 12, 150, 12, true, null);
passedData.RecordBadge("know_your_taxpayer_1", 3);
var passed = Build(passedData);
Check(passed.Chapters[0].Status == CareerChapterStatus.Passed && passed.Chapters[0].Launchable, "passed: Chapter 1 replayable");
Check(passed.Chapters[1].Status == CareerChapterStatus.Available && passed.Chapters[1].Launchable &&
      passed.Chapters[1].Def.scene == "Office_Level2", "passed: Chapter 2 unlocked, routes to Office_Level2");
Check(passed.Goal == passed.Chapters[1] && passed.Passed == 1, "passed: goal moves to Chapter 2");
Check(passed.Badges[0].Tier == 3 && passed.EarnedBadges == 1, "gold badge shown");
Check(passed.Chapters[1].TargetExp == 280 && passed.Chapters[1].TargetHearts == 18, "Chapter 2 pass line from its rules");
Check(passed.Chapters[0].MaxExp == 150 && passed.Chapters[0].BestExp == 150 && passed.Chapters[0].Attempts == 1, "record values carried");

var allData = new PlayerProgressData();
allData.RecordLevelResult("Level_01", 150, 12, 150, 12, true, null);
allData.RecordLevelResult("Level_02", 300, 20, 350, 22, true, null);
var all = Build(allData);
Check(all.Goal == all.Chapters[1] && all.Goal.Status == CareerChapterStatus.Passed, "all open chapters passed: goal is the last one");
Check(CareerProgress.NextStep(all).Contains("coming soon"), "all passed: next step says new chapters are coming");

// A saved maximum beats the rules (older runs keep their own maximum).
var savedMax = new PlayerProgressData();
savedMax.RecordLevelResult("Level_01", 100, 10, 200, 10, false, null);
Check(Build(savedMax).Chapters[0].TargetExp == 160, "pass line uses the saved maximum when a result exists");

// Version-1 saves (no pass line) count completed levels as passed.
var legacy = new PlayerProgressData { saveVersion = 1 };
legacy.completedLevelIds.Add("Level_01");
legacy.Normalize();
Check(Build(legacy).Chapters[1].Status == CareerChapterStatus.Available, "migrated v1 save keeps Chapter 2 unlocked");

// ---------- celebrations since the last visit ----------
Check(!CareerProgress.Diff(null, passed).Any(passed), "first visit: nothing to celebrate");
var seenFresh = CareerProgress.Remember(fresh);
var changes = CareerProgress.Diff(seenFresh, passed);
Check(changes.ExpFrom == 0 && changes.HeartsFrom == 0 && changes.Any(passed), "after a run: totals count up from last seen");
Check(changes.NewlyPassed.SequenceEqual(new[] { "Level_01" }), "after a run: Chapter 1 newly passed (stamp slams)");
Check(changes.UpgradedBadges.SequenceEqual(new[] { "know_your_taxpayer_1" }), "after a run: badge pops");
var again = CareerProgress.Diff(CareerProgress.Remember(passed), passed);
Check(!again.Any(passed), "second visit: nothing new");
var bronze = new PlayerProgressData();
bronze.RecordBadge("know_your_taxpayer_1", 1);
Check(CareerProgress.Diff(CareerProgress.Remember(Build(bronze)), passed).UpgradedBadges.Count == 1, "bronze to gold counts as an upgrade");
var finishedChanges = CareerProgress.Diff(seenFresh, failed);
Check(finishedChanges.NewlyFinished.SequenceEqual(new[] { "Level_01" }) && finishedChanges.NewlyPassed.Count == 0,
    "a run below the line is finished, not passed");
var stale = new CareerSeenState { exp = 99999, hearts = 99999 };
var clamped = CareerProgress.Diff(stale, passed);
Check(clamped.ExpFrom == passed.TotalExp && clamped.HeartsFrom == passed.TotalHearts, "a stale last-seen above the save never counts down");

// ---------- career bars, badge spaces, strings ----------
Check(fresh.CareerMaxExp == 150 + 350 && fresh.CareerMaxHearts == 12 + 22, "career bars measure against every non-coming-soon chapter");
Check(fresh.BadgeSlots == 5 && fresh.Badges[0].Number == 1, "five badge spaces; Know Your Taxpayer is side quest 1");
Check(fresh.Chapters.Count(c => c.IsOpen) == 1, "fresh: only Chapter 1 is open (no string)");
Check(passed.Chapters[1].IsOpen && !passed.Chapters[2].IsOpen, "after a pass: Chapter 2 open (string 1-2), Chapter 3 not");
Check(!failed.Chapters[1].IsOpen, "a run below the line adds no string");

// ---------- time spent ----------
Check(CareerProgress.FormatTime(0) == "--:--" && CareerProgress.FormatTime(179) == "02:59" && CareerProgress.FormatTime(3723) == "1:02:03",
    "time format");
var timed = new PlayerProgressData();
Check(!timed.RecordLevelTime("Level_01", 100f, 20f), "time needs a saved result first");
timed.RecordLevelResult("Level_01", 100, 8, 150, 12, false, null);
timed.RecordLevelTime("Level_01", 300.4f, 90f);
timed.RecordLevelResult("Level_01", 150, 12, 150, 12, true, null);
timed.RecordLevelTime("Level_01", 240f, 120f);
timed.RecordLevelTime("Level_01", 0f, -5f);
LevelRecord timedRecord = timed.GetRecord("Level_01");
Check(timedRecord.bestSeconds == 240 && timedRecord.lastSeconds == 240 && timedRecord.totalSeconds == 540,
    "chapter time keeps best, last and total; zero times are ignored");
Check(timedRecord.sideQuestBestSeconds == 90 && timedRecord.sideQuestLastSeconds == 120, "side quest keeps its best and last time");
var timedView = Build(timed);
Check(timedView.Chapters[0].BestSeconds == 240 && timedView.Chapters[0].TotalSeconds == 540 && timedView.Badges[0].BestSeconds == 90,
    "times reach the chapter and badge views");
var brokenTimes = new PlayerProgressData();
brokenTimes.levelRecords.Add(new LevelRecord { levelId = "Level_01", bestSeconds = -4, totalSeconds = -1, sideQuestBestSeconds = 50 });
brokenTimes.levelRecords.Add(new LevelRecord { levelId = "Level_01", bestSeconds = 30, totalSeconds = 70, sideQuestBestSeconds = 40 });
brokenTimes.Normalize();
LevelRecord merged = brokenTimes.GetRecord("Level_01");
Check(brokenTimes.levelRecords.Count == 1 && merged.bestSeconds == 30 && merged.totalSeconds == 70 && merged.sideQuestBestSeconds == 40,
    "repair clears negative times and merges duplicates (fastest, summed total)");
var oldJson = "{\"saveVersion\":3,\"levelRecords\":[{\"levelId\":\"Level_01\",\"bestExp\":120,\"maxExp\":150,\"passed\":true}]}";
var oldSave = JsonSerializer.Deserialize<PlayerProgressData>(oldJson, new JsonSerializerOptions { IncludeFields = true });
Check(oldSave.GetRecord("Level_01").bestSeconds == 0 && CareerProgress.FormatTime(oldSave.GetRecord("Level_01").bestSeconds) == "--:--",
    "older saves without times load with no time (shown as --:--)");

// ---------- chapters.json ----------
string jsonPath = Path.Combine(root, "Assets", "Resources", "Career", "chapters.json");
using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(jsonPath)))
{
    var chapters = doc.RootElement.GetProperty("chapters").EnumerateArray().ToList();
    Check(chapters.Count == 5, "chapters.json lists five chapters");
    Check(chapters[0].GetProperty("id").GetString() == "Level_01" &&
          chapters[0].GetProperty("scene").GetString() == "Level1_Character_Intro_Cutscene", "chapters.json: Chapter 1 scene");
    Check(chapters[1].GetProperty("scene").GetString() == "Office_Level2", "chapters.json: Chapter 2 scene");
    Check(chapters.Skip(2).All(c => c.GetProperty("comingSoon").GetBoolean()), "chapters.json: 3-5 coming soon");
    Check(doc.RootElement.GetProperty("sideQuests")[0].GetProperty("chapterId").GetString() == "Level_01",
        "chapters.json: side quest belongs to Chapter 1");
    Check(doc.RootElement.GetProperty("badgeSlots").GetInt32() == 5 && doc.RootElement.GetProperty("sideQuests")[0].GetProperty("number").GetInt32() == 1,
        "chapters.json: five badge spaces, side quest 1");
    foreach (var c in chapters.Take(2))
    {
        string scene = c.GetProperty("scene").GetString();
        Check(File.Exists(Path.Combine(root, "Assets", "Scenes", scene + ".unity")), $"scene {scene} exists");
    }
}

// The Unity fallback catalog mirrors the JSON.
string service = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "RewardsSystem", "CareerProgressService.cs"));
Check(service.Contains("\"Level1_Character_Intro_Cutscene\"") && service.Contains("\"Office_Level2\""), "built-in catalog routes the same scenes");

// The scene board points at the Office Level 1 corkboard prop.
string sceneText = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "3D Menu.unity"));
Check(Regex.IsMatch(sceneText, @"corkboardPrefab: \{fileID: \d+, guid: dc93c1371b25a3f41be9b77daf61961c"), "3D Menu board uses SM_Prop_CorkBoard_01");

// ---------- story board layout: papers never overlap and stay on the cork ----------
string presenter = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "UI", "Menu", "CareerBoardPresenter.cs"));
float[] Vec(string name)
{
    Match m = Regex.Match(presenter, name + @" = new Vector2\((-?[\d.]+)f, (-?[\d.]+)f\)");
    return new[] { float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                   float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) };
}
float[][] Pins(string name)
{
    Match block = Regex.Match(presenter, name + @" =\s*\{(.*?)\};", RegexOptions.Singleline);
    return Regex.Matches(block.Groups[1].Value, @"new Vector2\((-?[\d.]+)f, (-?[\d.]+)f\)").Select(m => new[]
    {
        float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
        float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture)
    }).ToArray();
}
// A paper hangs from a pin 18 px below its top edge; 6 px of slack each side covers the small tilts.
float[] Rect(float[] pin, float[] size) => new[] { pin[0] - size[0] / 2 - 6, pin[1] + 18 + 6 - size[1], pin[0] + size[0] / 2 + 6, pin[1] + 18 };
bool Overlap(float[] a, float[] b) => a[0] < b[2] && b[0] < a[2] && a[1] < b[3] && b[1] < a[3];
var chapterSize = Vec("ChapterPaperSize");
var badgeSize = Vec("BadgePaperSize");
var storyCenter = Vec("StoryBoardCenter");
var storySize = Vec("StoryBoardSize");
var rects = Pins("ChapterPins").Select(p => Rect(p, chapterSize)).Concat(Pins("BadgePins").Select(p => Rect(p, badgeSize))).ToList();
Check(rects.Count == 10, "layout: five chapter papers and five badge spaces");
bool anyOverlap = false;
for (int i = 0; i < rects.Count; i++)
    for (int j = i + 1; j < rects.Count; j++)
        if (Overlap(rects[i], rects[j])) { anyOverlap = true; Console.WriteLine($"  overlap: paper {i + 1} and {j + 1}"); }
Check(!anyOverlap, "layout: no two papers overlap (Chapter 2 and 4 keep a vertical gap)");
float inset = 22f;
Check(rects.All(r => r[0] >= storyCenter[0] - storySize[0] / 2 + inset && r[2] <= storyCenter[0] + storySize[0] / 2 - inset &&
                     r[1] >= storyCenter[1] - storySize[1] / 2 + inset && r[3] <= storyCenter[1] + storySize[1] / 2 - inset),
    "layout: every paper stays inside the story corkboard");
Check(badgeSize[0] >= 116 && Regex.IsMatch(presenter, @"CareerBadgeMedal.Create\(body, ""Medal"", [^;]*, 8\d+f\)"), "layout: badge papers and medals are the larger size");

// Every paper flies out the same way: a negative tilt must not read as ~356 degrees (that spun Chapters 1, 3 and 5).
Check(presenter.Contains("Mathf.DeltaAngle(0f, sourcePaper.transform.localEulerAngles.z)"), "sheet fly-out uses the paper tilt in -180..180");
Check(Pins("ChapterPins")[0][1] > Pins("ChapterPins")[2][1], "Chapter 1 hangs a little higher than the others");

Console.WriteLine($"\n{checks - failures}/{checks} checks passed");
return failures == 0 ? 0 : 1;
