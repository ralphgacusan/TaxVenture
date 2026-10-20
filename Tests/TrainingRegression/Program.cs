// Tutorial Hub (training binder) regression: the save's version 4 training
// fields, the card rules, every drill composed against the real Level 1
// guide.json, scene paths, touch wording and the scene / menu wiring.
// Run from this folder:  dotnet run --no-restore
// (build the scratch Assembly-CSharp first; see TrainingRegression.csproj).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

int checks = 0, failures = 0;
void Check(bool ok, string message)
{
    checks++;
    if (!ok) { failures++; Console.WriteLine("FAIL: " + message); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string PathOf(string rel) => Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
string Read(string rel) => File.ReadAllText(PathOf(rel));
var json = new JsonSerializerOptions { IncludeFields = true, ReadCommentHandling = JsonCommentHandling.Skip };
T Load<T>(string rel) => JsonSerializer.Deserialize<T>(Read(rel), json);

// =========================================================
// SAVE (version 4)
// =========================================================
Check(PlayerProgressData.CurrentSaveVersion == 4 && PlayerProgressData.SignedSinceVersion == 3, "save version 4, signed since 3");

foreach (int version in new[] { 1, 2, 3 })
{
    var old = new PlayerProgressData
    {
        saveVersion = version, totalExp = 140, totalReputation = 11, levelsCompleted = 1,
        completedLevelIds = new List<string> { "Level_01" }, passedLevelIds = new List<string> { "Level_01" },
        tutorialsSeen = null, trainingRecords = null
    };
    old.Normalize();
    Check(old.saveVersion == 4 && old.tutorialsSeen != null && old.trainingRecords != null &&
          old.tutorialsSeen.Count == 0 && old.trainingRecords.Count == 0,
          $"v{version} save upgrades to v4 with empty training lists");
    Check(old.totalExp == 140 && old.totalReputation == 11 && old.completedLevelIds.SequenceEqual(new[] { "Level_01" }),
          $"v{version} upgrade keeps EXP, hearts and levels");
}

var save = new PlayerProgressData();
save.Normalize();
Check(save.RecordTutorialSeen("Level_01/move") && !save.RecordTutorialSeen("Level_01/move"), "a lesson is recorded once");
Check(!save.RecordTutorialSeen("  ") && !save.RecordTutorialSeen(null), "blank lessons are ignored");
Check(save.HasSeenTutorial("Level_01/move") && !save.HasSeenTutorial("Level_01/look"), "seen lookup");
Check(PlayerProgressData.TutorialKey("Level_01", "move") == "Level_01/move" && PlayerProgressData.TutorialKey("", "x") == null, "tutorial key");

int expBefore = save.totalExp, heartsBefore = save.totalReputation;
Check(!save.RecordTrainingRun("basics.moving", false, 30f), "a guided run never masters");
TrainingRecord moving = save.GetTrainingRecord("basics.moving");
Check(moving != null && moving.practiceRuns == 1 && !moving.Mastered, "a guided run counts as practised");
Check(save.RecordTrainingRun("basics.moving", true, 42.4f), "the first mastery run masters the card");
Check(!save.RecordTrainingRun("basics.moving", true, 50f), "a second mastery run is not new");
Check(save.RecordTrainingRun("basics.moving", true, 31f) == false && moving.bestMasterySeconds == 31 && moving.masteryRuns == 3,
      "the fastest mastery time is kept");
Check(save.totalExp == expBefore && save.totalReputation == heartsBefore && save.badgeRecords.Count == 0 && save.completedLevelIds.Count == 0,
      "practice never touches EXP, hearts, badges or levels");

var broken = new PlayerProgressData
{
    saveVersion = 4,
    tutorialsSeen = new List<string> { " Level_01/move ", "Level_01/move", "", null },
    trainingRecords = new List<TrainingRecord>
    {
        null,
        new TrainingRecord { cardId = "" },
        new TrainingRecord { cardId = "case.desk", practiceRuns = -3, masteryRuns = 1, bestMasterySeconds = 90 },
        new TrainingRecord { cardId = " case.desk", practiceRuns = 2, masteryRuns = 0, bestMasterySeconds = 70 }
    }
};
Check(broken.Normalize(), "a damaged training save is repaired");
Check(broken.tutorialsSeen.SequenceEqual(new[] { "Level_01/move" }), "lesson list cleaned (trim, blanks, duplicates)");
Check(broken.trainingRecords.Count == 1 && broken.trainingRecords[0].practiceRuns == 2 && broken.trainingRecords[0].masteryRuns == 1 &&
      broken.trainingRecords[0].bestMasterySeconds == 70, "duplicate records merge (negatives cleared, best time kept)");

string manager = Read("Assets/Scripts/RewardsSystem/PlayerProgressManager.cs");
Check(manager.Contains("data.saveVersion >= PlayerProgressData.SignedSinceVersion"), "plain files claiming a signed version stay rejected");
Check(Regex.Matches(manager, @"if \(IgnoredForPractice\(levelTotal\)\) return;").Count == 2 && manager.Contains("TrainingSession.IsPracticing"),
      "level results are ignored while practising (pending and apply)");

// =========================================================
// CATALOG (training.json)
// =========================================================
var guide = Load<GuideScript>("Assets/Resources/Tutorials/Level_01/guide.json");
var guideIds = new HashSet<string>(guide.lessons.Select(l => l.id));
string originalAfter = guide.lessons.First(l => l.id == "move").after;

var catalog = Load<TrainingCatalogFile>("Assets/Resources/Tutorials/Training/training.json");
string catalogProblems = catalog.Validate();
Check(string.IsNullOrEmpty(catalogProblems), "training.json is valid: " + catalogProblems);
Check(catalog.tabs.Select(t => t.id).SequenceEqual(new[] { "basics", "sidequest", "case" }), "tabs: Basics, Side Quest, Case");
Check(catalog.tabs.Count(t => t.alwaysOpen) == 1 && catalog.FindTab("basics").alwaysOpen, "only Basics is open from the start");
Check(catalog.cards.Count(c => c.tab == "basics") == 3 && catalog.cards.Count(c => c.tab == "sidequest") == 1 &&
      catalog.cards.Count(c => c.tab == "case") == 3, "3 basics cards, 1 per side quest, 3 case cards");
foreach (TrainingCardDef card in catalog.cards)
{
    Check(card.lessons.Length > 0, $"{card.id}: covers at least one lesson");
    foreach (string lesson in card.lessons)
        Check(lesson.StartsWith("Level_01/") && guideIds.Contains(lesson.Substring(9)), $"{card.id}: lesson {lesson} exists in guide.json");
    Check(File.Exists(PathOf($"Assets/Resources/Tutorials/Training/Drills/{card.drill}.json")), $"{card.id}: drill file {card.drill}.json exists");
    Check(card.skills.Length > 0 && !string.IsNullOrWhiteSpace(card.title), $"{card.id}: has a title and skill lines");
}
var allLessons = catalog.cards.SelectMany(c => c.lessons).ToList();
Check(allLessons.Count == allLessons.Distinct().Count(), "no lesson belongs to two cards");

// Card rules.
var fresh = new PlayerProgressData();
fresh.Normalize();
TrainingHubView first = TrainingCatalog.Build(catalog, fresh);
TrainingTabView Tab(TrainingHubView v, string id) => v.Tabs.First(t => t.Def.id == id);
Check(Tab(first, "basics").Visible && Tab(first, "basics").Cards.All(c => c.State == TrainingCardState.New),
      "first launch: Basics is open and every basics card is NEW");
Check(!Tab(first, "sidequest").Visible && !Tab(first, "case").Visible, "first launch: Side Quest and Case are not shown");
Check(Tab(first, "case").Cards.All(c => c.State == TrainingCardState.Locked), "first launch: case cards are locked");
Check(first.Total == 7 && first.Unlocked == 3 && first.Mastered == 0, "first launch totals");

var midLevel = new PlayerProgressData();
midLevel.Normalize();
midLevel.RecordTutorialSeen("Level_01/corkboard");
TrainingHubView mid = TrainingCatalog.Build(catalog, midLevel);
Check(Tab(mid, "sidequest").Visible && Tab(mid, "sidequest").Cards[0].State == TrainingCardState.New &&
      Tab(mid, "sidequest").Cards[0].Seen == 1 && Tab(mid, "sidequest").Cards[0].Total == 3,
      "finishing the corkboard lesson in game opens Side Quest 1 (seen 1/3)");
Check(!Tab(mid, "case").Visible, "the Case tab stays hidden until a case lesson is finished");

midLevel.RecordTutorialSeen("Level_01/value-transfer");
TrainingHubView mid2 = TrainingCatalog.Build(catalog, midLevel);
Check(Tab(mid2, "case").Visible && mid2.FindCard("case.values").Unlocked && !mid2.FindCard("case.desk").Unlocked,
      "a case card opens on its own lesson; the others stay locked");

var finished = new PlayerProgressData { saveVersion = 3, levelsCompleted = 1, completedLevelIds = new List<string> { "Level_01" } };
finished.Normalize();
TrainingHubView done = TrainingCatalog.Build(catalog, finished);
Check(done.Tabs.All(t => t.Visible) && done.Unlocked == 7 && done.Tabs.SelectMany(t => t.Cards).All(c => c.Seen == c.Total),
      "an old save that finished Chapter 1 opens every Chapter 1 card");

finished.RecordTrainingRun("basics.screen", false, 20f);
finished.RecordTrainingRun("case.desk", true, 80f);
TrainingHubView after = TrainingCatalog.Build(catalog, finished);
Check(after.FindCard("basics.screen").State == TrainingCardState.Practised && after.FindCard("basics.screen").CanMaster,
      "a guided run makes the card PRACTISED and offers MASTERY");
Check(after.FindCard("case.desk").State == TrainingCardState.Mastered && after.Mastered == 1, "a mastery run stamps the card MASTERED");
Check(!first.FindCard("basics.screen").CanMaster, "a never practised card offers PRACTICE only");

// =========================================================
// DRILLS (composed against the real guide.json)
// =========================================================
Check(TrainingDrillScript.Chain("lesson:notes", "intro", new HashSet<string> { "intro" }) == "lesson:intro",
      "chain: a lesson outside the drill is replaced by the previous step");
Check(TrainingDrillScript.Chain("lesson:welcome", null, new HashSet<string>()) == "start", "chain: the first step starts with the guide");
Check(TrainingDrillScript.Chain("near:3:type:TaxpayerNpcInteractable", "intro", new HashSet<string>()) == "near:3:type:TaxpayerNpcInteractable",
      "chain: a step's own condition is kept");
Check(TrainingDrillScript.Chain("", "intro", new HashSet<string>()) == "lesson:intro", "chain: an empty when waits for the previous step");
Check(TrainingDrillScript.Chain("event:A|lesson:gone", "x", new HashSet<string> { "x" }) == "event:A|lesson:x", "chain: alternatives are kept");

string scene = Read("Assets/Scenes/Office_Level1.unity");
var sceneNames = new HashSet<string>(Regex.Matches(scene, @"m_Name: (.+)").Select(m => m.Groups[1].Value.Trim()));
string[] banned = { "click", "mouse", "wasd", "keyboard", "press space", "press enter" };
bool TouchOnly(string text) => text == null || !banned.Any(b => text.ToLowerInvariant().Contains(b));

foreach (TrainingCardDef card in catalog.cards)
{
    string rel = $"Assets/Resources/Tutorials/Training/Drills/{card.drill}.json";
    if (!File.Exists(PathOf(rel))) continue;
    TrainingDrill drill = Load<TrainingDrill>(rel);
    Check(drill.id == card.drill, $"{card.drill}: id matches the file name");
    Check(drill.scene == "Office_Level1" && drill.levelId == "Level_01", $"{card.drill}: practises in Office_Level1");
    Check(drill.start == "plaza" || drill.start == "office", $"{card.drill}: start is plaza or office");
    if (drill.start == "office")
        Check(!string.IsNullOrEmpty(drill.spawn) && !string.IsNullOrEmpty(drill.face), $"{card.drill}: office start has spawn and face");
    if (!string.IsNullOrEmpty(drill.practiceCase))
        Check(File.Exists(PathOf($"Assets/Resources/Levels/{drill.levelId}/cases/{drill.practiceCase}.json")), $"{card.drill}: practice case exists");
    if (drill.profile != null && !string.IsNullOrEmpty(drill.profile.name))
        Check(Enum.TryParse(drill.profile.classification, out TaxpayerClassification _), $"{card.drill}: profile classification is valid");

    var paths = new List<string>(drill.hide);
    if (!string.IsNullOrEmpty(drill.spawn)) paths.Add(drill.spawn);
    if (!string.IsNullOrEmpty(drill.face)) paths.Add(drill.face);
    foreach (string path in paths)
        Check(sceneNames.Contains(path.Split('/').Last()), $"{card.drill}: scene object {path} exists in Office_Level1");
    Check(!drill.hide.Any(p => p.Contains("Reception")), $"{card.drill}: never hides the reception (office door gate)");
    foreach (string step in drill.objectivesDone)
        Check(Read("Assets/Resources/Levels/Level_01/objectives.json").Contains(step.Split('|')[0].Replace("area:", "")) ||
              Read("Assets/Resources/Levels/Level_01/objectives.json").Contains(step), $"{card.drill}: objectivesDone {step} is a Level 1 step");

    foreach (bool mastery in new[] { false, true })
    {
        string name = $"{card.drill} ({(mastery ? "mastery" : "guided")})";
        TrainingDrillScript.Result run = TrainingDrillScript.Compose(guide, drill, mastery);
        TrainingRun source = mastery ? drill.mastery : drill.guided;
        Check(string.IsNullOrEmpty(run.Problems), $"{name}: composes cleanly: {run.Problems}");
        Check(run.Steps.Count == source.steps.Length && run.Steps.Count >= 3, $"{name}: every step survives ({run.Steps.Count})");
        Check(run.FinalStep == run.Steps.Last() && run.Script.lessons.Any(l => l.id == run.FinalStep), $"{name}: ends with its last step");
        GuideLesson firstStep = run.Script.lessons.First(l => l.id == run.Steps[0]);
        Check(firstStep.when == "start" && firstStep.after == null, $"{name}: the first step starts with the guide");
        for (int i = 1; i < run.Steps.Count; i++)
            Check(run.Script.lessons.First(l => l.id == run.Steps[i]).after == run.Steps[i - 1], $"{name}: step {i + 1} waits for step {i}");
        Check(!run.Script.rememberFinishedLessons && run.Script.enabled, $"{name}: plays every run");
        foreach (GuideLesson lesson in run.Script.lessons)
            foreach (GuidePage page in lesson.pages)
            {
                Check(TouchOnly(page.text) && TouchOnly(page.textTouch), $"{name}: touch wording in {lesson.id}");
                if (page.point != null && page.point.StartsWith("world:"))
                    Check(sceneNames.Contains(page.point.Split('/').Last()), $"{name}: target {page.point} exists");
            }
        if (mastery)
            Check(source.steps.Where(s => string.IsNullOrEmpty(s.use)).All(s => s.pages.All(p => string.IsNullOrEmpty(p.point))) &&
                  source.steps.All(s => string.IsNullOrEmpty(s.use)),
                  $"{name}: mastery has no hand or spotlight and reuses no guided lesson");
        else
            Check(source.steps.Any(s => !string.IsNullOrEmpty(s.use)), $"{name}: guided reuses the in-game lessons");
    }
}
Check(guide.lessons.First(l => l.id == "move").after == originalAfter, "composing never changes the level's own guide.json lessons");
Check(TouchOnly(Read("Assets/Resources/Tutorials/Training/training.json")), "training.json uses touch wording");

// =========================================================
// WIRING
// =========================================================
string mainMenu = Read("Assets/Scripts/MainMenu.cs");
Check(mainMenu.Contains("case \"tutorial_button\":") && mainMenu.Contains("OpenTraining();") &&
      mainMenu.Contains("TrainingBinder3D.Instance.Open()") && !mainMenu.Contains("\"TutorialPage\""),
      "the tutorial button opens the training binder in the 3D Menu");
Check(mainMenu.Contains("if (TrainingBinder3D.Instance != null && TrainingBinder3D.Instance.IsOpen) return;"),
      "the menu ignores touches while the binder is up");
string build = Read("ProjectSettings/EditorBuildSettings.asset");
Check(!build.Contains("TutorialHub") && !File.Exists(PathOf("Assets/Scenes/TutorialHub.unity")), "no separate TutorialHub scene any more");
string binder = Read("Assets/Scripts/Training/TrainingBinder3D.cs");
Check(binder.Contains("scene.name != MenuEntry.MenuSceneName") && binder.Contains("host.AddComponent<TrainingBinder3D>()"),
      "the binder adds itself to the 3D Menu (no scene edit)");
Check(binder.Contains("private const int BinderLayer = 29;") && Read("Assets/Scripts/Settings/SettingsBoard3D.cs").Contains("private const int BoardLayer = 30;"),
      "the binder has its own overlay layer (settings uses 30)");
Check(binder.Contains("settingsBoard.IsOpen) return false;") && binder.Contains("cameraManager.IsTransitioning || cameraManager.IsLevelSelectorVisible"),
      "the binder only opens on the idle main menu view");
string menuScene = Read("Assets/Scenes/3D Menu.unity");
foreach (string prop in new[] { "SM_Prop_Chair_07", "SM_Prop_Desk_03" })
    Check(binder.Contains($"\"{prop}\"") && menuScene.Contains("value: " + prop), $"the lean finds {prop} in the 3D Menu");
string session0 = Read("Assets/Scripts/Training/TrainingSession.cs");
Check(session0.Contains("openBinder = true;") && session0.Contains("SceneManager.LoadScene(MenuEntry.MenuSceneName);") &&
      binder.Contains("TrainingSession.ConsumeOpenBinder()"), "back from a drill, the 3D Menu opens straight on the binder");

string director = Read("Assets/Scripts/UI/Guide/GuideDirector.cs");
int endLesson = director.IndexOf("private void EndLesson()", StringComparison.Ordinal);
Check(endLesson > 0 && director.IndexOf("LessonFinished?.Invoke(levelId, ended)", endLesson, StringComparison.Ordinal) > endLesson,
      "a finished lesson is announced from EndLesson");
int skipAll = director.IndexOf("public void SkipAll()", StringComparison.Ordinal);
int skipEnd = director.IndexOf("// HOLDING THE PLAYER STILL", skipAll, StringComparison.Ordinal);
Check(skipAll > 0 && director.Substring(skipAll, skipEnd - skipAll).IndexOf("LessonFinished", StringComparison.Ordinal) < 0,
      "SKIP TUTORIAL does not count as seeing the lessons");
string runtime = Read("Assets/Scripts/UI/Guide/GuideRuntime.cs");
Check(runtime.Contains("TrainingSession.ComposeFor(levelId)") && runtime.Contains("view.SetSkipVisible(false)"),
      "practice plays the drill instead of the level tutorial, without SKIP TUTORIAL");
string session = Read("Assets/Scripts/Training/TrainingSession.cs");
Check(session.Contains("if (TrainingSession.IsPracticing) return;") && session.Contains("if (scene.name != Drill.scene) End();"),
      "practice never records lessons, and leaving the drill's scene ends the session");

// Every new asset has a .meta with a unique GUID, and CRLF line endings.
string[] newFiles =
{
    "Assets/Scripts/Training", "Assets/Scripts/Training/TrainingData.cs", "Assets/Scripts/Training/TrainingSession.cs",
    "Assets/Scripts/Training/PracticeModeRuntime.cs", "Assets/Scripts/Training/TrainingBinderCard.cs",
    "Assets/Scripts/Training/TrainingBinder3D.cs", "Assets/Editor/TutorialHubSetup.cs",
    "Assets/Resources/Tutorials/Training", "Assets/Resources/Tutorials/Training/training.json", "Assets/Resources/Tutorials/Training/Drills"
};
var guids = new HashSet<string>();
foreach (string meta in Directory.GetFiles(PathOf("Assets"), "*.meta", SearchOption.AllDirectories))
{
    string g = Regex.Match(File.ReadAllText(meta), @"guid: ([0-9a-f]{32})").Groups[1].Value;
    if (g.Length > 0 && !guids.Add(g) && meta.Contains("Training")) Check(false, "duplicate GUID in " + meta);
}
foreach (string file in newFiles.Concat(Directory.GetFiles(PathOf("Assets/Resources/Tutorials/Training/Drills"), "*.json")
             .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))))
{
    Check(File.Exists(PathOf(file + ".meta")), file + " has a .meta");
    if (File.Exists(PathOf(file)))
    {
        string text = Read(file);
        Check(!Regex.IsMatch(text, "(?<!\r)\n"), file + " uses CRLF line endings");
    }
}

Check(Read("Assets/Scripts/Training/TrainingBinder3D.cs").Contains("if (!MenuEntry.HasCareerAccess) return false;"), "fresh careers cannot open the tutorial binder through a stale event");

Console.WriteLine($"{checks - failures}/{checks} checks passed.");
return failures == 0 ? 0 : 1;
