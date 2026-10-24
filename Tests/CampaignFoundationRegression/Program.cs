using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// Campaign Foundation regression. T01: the mastery boundary (MasteryBoundary.cs)
// must give exactly the answers the pre-T01 code gave. PreT01 below is a frozen
// copy of the replaced rules (LevelScoring.Percent/Passed,
// PlayerProgressData.RecordLevelResult/IsLevelUnlocked, the
// PlayerProgressManager.ApplyLevelResult record branches and the CareerProgress
// chapter unlock); every case runs both and compares the saved data. Compiles
// the real PlayerProgressData.cs / MasteryBoundary.cs / CareerProgress.cs.

int checks = 0, failures = 0;
void Check(bool ok, string message)
{
    checks++;
    if (ok) Console.WriteLine("PASS: " + message);
    else { failures++; Console.WriteLine("FAIL: " + message); }
}

var json = new JsonSerializerOptions { IncludeFields = true };
string Snap(PlayerProgressData data) => JsonSerializer.Serialize(data, json);
PlayerProgressData Clone(PlayerProgressData data) => JsonSerializer.Deserialize<PlayerProgressData>(Snap(data), json);
PlayerProgressData Fresh() { var d = new PlayerProgressData(); d.Normalize(); return d; }

// ---------- pass line ----------
bool passLineSame = true, percentSame = true;
int[] percents = { 1, 50, 79, 80, 81, 99, 100 };
int[] maxima = { -5, 0, 1, 7, 12, 150, 350 };
foreach (int pp in percents)
    foreach (int maxExp in maxima)
        foreach (int maxHearts in maxima)
            for (int exp = -2; exp <= Math.Max(2, maxExp) + 2; exp += Math.Max(1, maxExp / 25))
                for (int hearts = -2; hearts <= Math.Max(2, maxHearts) + 2; hearts++)
                {
                    if (MasteryBoundary.MeetsPassLine(pp, exp, hearts, maxExp, maxHearts) != PreT01.Passed(pp, exp, hearts, maxExp, maxHearts))
                        passLineSame = false;
                    if (MasteryBoundary.Percent(exp, maxExp) != PreT01.Percent(exp, maxExp)) percentSame = false;
                }
Check(percentSame, "Percent matches the pre-T01 formula (negative earned, zero/negative maximum, rounding down)");
Check(passLineSame, "pass line matches the pre-T01 formula across percents, maxima, EXP and hearts");

// Level 1 at the shipped 80%: 150 EXP / 12 hearts.
Check(MasteryBoundary.MeetsPassLine(80, 120, 10, 150, 12), "boundary tie: exactly 80% EXP (120/150) with 83% hearts passes");
Check(!MasteryBoundary.MeetsPassLine(80, 119, 12, 150, 12), "one EXP under the line (119/150 = 79%) fails");
Check(!MasteryBoundary.MeetsPassLine(80, 150, 9, 150, 12), "hearts under the line (9/12 = 75%) fail even with full EXP");
Check(MasteryBoundary.MeetsPassLine(80, 0, 0, 0, 0), "a level without maxima counts as 100% (unchanged)");

// ---------- first-time record ----------
var first = Fresh();
var firstGain = first.RecordLevelResult("Level_01", 100, 8, 150, 12, false, new[] { "know_your_taxpayer_1" });
LevelRecord firstRecord = first.GetRecord("Level_01");
Check(firstGain == (100, 8, true), "first-time record adds the whole score and is a new best");
Check(firstRecord != null && firstRecord.attempts == 1 && firstRecord.bestExp == 100 && firstRecord.bestHearts == 8 && !firstRecord.passed,
    "first-time record stores best EXP/hearts, one attempt and not passed");
Check(first.totalExp == 100 && first.totalReputation == 8 && first.levelsCompleted == 1 && !first.IsLevelUnlocked(2),
    "first below-line run completes Level 1 without unlocking Level 2");

// ---------- ties and replays ----------
var tie = first.RecordLevelResult("Level_01", 100, 8, 150, 12, false, null);
Check(tie == (0, 0, false) && first.GetRecord("Level_01").attempts == 2 && first.totalExp == 100 && first.totalReputation == 8,
    "an equal replay (tie) adds nothing, is not a new best, still counts an attempt");
var mixed = first.RecordLevelResult("Level_01", 90, 11, 150, 12, false, null);
Check(mixed == (0, 3, true) && first.totalExp == 100 && first.totalReputation == 11,
    "a replay better in hearts only adds only the heart improvement");
var pass = first.RecordLevelResult("Level_01", 140, 11, 150, 12, true, null);
Check(pass == (40, 0, true) && first.IsLevelPassed("Level_01") && first.IsLevelUnlocked(2) && !first.IsLevelUnlocked(3),
    "a passing replay adds the EXP improvement and unlocks Level 2 only");
first.RecordLevelResult("Level_01", 10, 1, 150, 12, false, null);
Check(first.IsLevelPassed("Level_01") && first.GetRecord("Level_01").passed && first.totalExp == 140,
    "a later worse run never un-passes the level or lowers totals");
var over = Fresh().RecordLevelResult("Level_02", 999, 99, 350, 22, true, null);
Check(over == (350, 22, true), "a run above its maximum is clamped (unchanged)");

// ---------- RecordLevelResult == pre-T01 over random sequences ----------
var rng = new Random(20261021);
string[] ids = { "Level_01", " Level_01 ", "Level_02", "Level_03", "", null };
bool recordSame = true, unlockSame = true;
for (int seq = 0; seq < 300 && recordSame; seq++)
{
    var now = Fresh();
    var old = Fresh();
    for (int step = 0; step < 12; step++)
    {
        string id = ids[rng.Next(ids.Length)];
        int maxExp = rng.Next(-1, 400), maxHearts = rng.Next(-1, 25);
        int exp = rng.Next(-10, 450), hearts = rng.Next(-3, 30);
        bool passed = rng.Next(2) == 0;
        string[] badges = rng.Next(3) == 0 ? null : new[] { "b" + rng.Next(3), rng.Next(2) == 0 ? " " : "b1" };
        var a = now.RecordLevelResult(id, exp, hearts, maxExp, maxHearts, passed, badges);
        var b = PreT01.RecordLevelResult(old, id, exp, hearts, maxExp, maxHearts, passed, badges);
        if (a != b || Snap(now) != Snap(old)) { recordSame = false; break; }
    }
    for (int level = -1; level <= 7; level++)
        if (now.IsLevelUnlocked(level) != PreT01.IsLevelUnlocked(old, level)) unlockSame = false;
}
Check(recordSame, "RecordLevelResult (now via MasteryBoundary) returns and saves exactly what pre-T01 did over 300 random replay sequences");
Check(unlockSame, "IsLevelUnlocked answers match pre-T01 for levels -1..7 on every sequence");
Check(MasteryBoundary.LegacyLevelUnlocked(null, 1) && !MasteryBoundary.LegacyLevelUnlocked(null, 2),
    "a missing save still opens only Level 1");

// ---------- ApplyRun == pre-T01 manager record branches ----------
bool applySame = true;
for (int seq = 0; seq < 300 && applySame; seq++)
{
    var now = Fresh();
    var old = Fresh();
    for (int step = 0; step < 10; step++)
    {
        int badgeCount = rng.Next(0, 3);
        var badges = Enumerable.Range(0, badgeCount).Select(i => "badge_" + rng.Next(3)).ToList();
        var tiers = Enumerable.Range(0, rng.Next(0, 3)).Select(i => rng.Next(-1, 5)).ToList();
        var run = new MasteryRun
        {
            LevelId = ids[rng.Next(4)],
            HasScore = rng.Next(4) != 0,
            Exp = rng.Next(-5, 400), Hearts = rng.Next(-2, 25),
            MaxExp = rng.Next(0, 350), MaxHearts = rng.Next(0, 22),
            Passed = rng.Next(2) == 0,
            Badges = badges, BadgeTiers = tiers,
            RunSeconds = rng.Next(4) == 0 ? 0f : (float)(rng.NextDouble() * 900),
            SideQuestSeconds = rng.Next(4) == 0 ? -1f : (float)(rng.NextDouble() * 300)
        };
        var a = MasteryBoundary.ApplyRun(now, run);
        var b = PreT01.ApplyLevelResult(old, run);
        if ((run.HasScore && a != b) || Snap(now) != Snap(old)) { applySame = false; break; }
    }
}
Check(applySame, "ApplyRun saves exactly what ApplyLevelResult's scored and unscored branches did (badge tiers, times, completion)");

var unscored = Fresh();
MasteryBoundary.ApplyRun(unscored, new MasteryRun { LevelId = "Level_01", Exp = 30, Hearts = 2, Badges = new List<string>() });
MasteryBoundary.ApplyRun(unscored, new MasteryRun { LevelId = "Level_01", Exp = 30, Hearts = 2, Badges = new List<string>() });
Check(unscored.IsLevelPassed("Level_01") && unscored.totalExp == 30 && unscored.totalReputation == 2,
    "an unscored run counts as passed and its replay never adds rewards again");
var before = Snap(unscored);
MasteryBoundary.ApplyRun(unscored, new MasteryRun { LevelId = "  " });
MasteryBoundary.ApplyRun(null, new MasteryRun { LevelId = "Level_01" });
Check(Snap(unscored) == before, "a run without a level id changes nothing");

// ---------- score hearts stay mastery data ----------
var scope = Fresh();
scope.levelOneIntroSeen = true;
scope.RecordStoryFact("fact");
scope.RecordTutorialSeen("Level_01/move");
string untouched = JsonSerializer.Serialize(new { scope.storyFacts, scope.tutorialsSeen, scope.trainingRecords, scope.levelOneIntroSeen, scope.activeCheckpoint, scope.saveVersion }, json);
MasteryBoundary.ApplyRun(scope, new MasteryRun { LevelId = "Level_01", HasScore = true, Exp = 150, Hearts = 12, MaxExp = 150, MaxHearts = 12, Passed = true,
    Badges = new List<string> { "know_your_taxpayer_1" }, BadgeTiers = new List<int> { 3 }, RunSeconds = 300, SideQuestSeconds = 60 });
Check(JsonSerializer.Serialize(new { scope.storyFacts, scope.tutorialsSeen, scope.trainingRecords, scope.levelOneIntroSeen, scope.activeCheckpoint, scope.saveVersion }, json) == untouched,
    "recording mastery touches only mastery fields (no story facts, tutorials, training, intro flag, checkpoint or save version)");
Check(scope.GetBadgeTier("know_your_taxpayer_1") == 3 && scope.GetRecord("Level_01").bestSeconds == 300 && scope.GetRecord("Level_01").sideQuestBestSeconds == 60,
    "a perfect scored run keeps its gold badge and times");
Check(PlayerProgressData.SignedSinceVersion == 3 && scope.saveVersion == PlayerProgressData.LegacySaveVersion,
    "signature cutoff unchanged; a save without a campaign stays at the pre-campaign version");

// ---------- career chapter unlock ----------
bool chapterSame = true;
foreach (var data in new[] { null, Fresh(), first, unscored, scope })
    foreach (string previous in new[] { null, "Level_01", "Level_02", " Level_01 ", "" })
        if (MasteryBoundary.LegacyChapterUnlocked(data, previous) != PreT01.ChapterUnlocked(data, previous)) chapterSame = false;
Check(chapterSame, "career chapter unlock matches the pre-T01 'previous chapter passed' rule");

var catalog = new CareerCatalog();
catalog.chapters.Add(new CareerChapterDef { id = "Level_01", number = 1, scene = "Cutscene2.0", cases = 1 });
catalog.chapters.Add(new CareerChapterDef { id = "Level_02", number = 2, scene = "Office_Level2", cases = 1 });
catalog.chapters.Add(new CareerChapterDef { id = "Level_03", number = 3, comingSoon = true });
CareerChapterRules Rules(CareerChapterDef def) => new CareerChapterRules { passingPercent = 80, maxExp = 150, maxHearts = 12 };
var below = Fresh();
below.RecordLevelResult("Level_01", 100, 8, 150, 12, false, null);
CareerSnapshot belowView = CareerProgress.Build(below, catalog, Rules);
CareerSnapshot passView = CareerProgress.Build(first, catalog, Rules);
Check(belowView.Chapters[1].Status == CareerChapterStatus.Locked && !belowView.Chapters[1].Launchable,
    "career: Level 2 stays locked after a below-line Level 1 (legacy gate unchanged in T01)");
Check(passView.Chapters[0].Status == CareerChapterStatus.Passed && passView.Chapters[1].Status == CareerChapterStatus.Available &&
      passView.Chapters[1].Launchable && passView.Chapters[2].Status == CareerChapterStatus.ComingSoon,
    "career: a passed Level 1 opens Level 2; Level 3 stays coming soon");

// =====================================================================
// T02: fresh campaign and v5 save boundary
// =====================================================================

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

// Same envelope as PlayerProgressManager.Encode/TryDecode ({ format, payload, signature }).
string EncodeSave(PlayerProgressData d)
{
    string payload = JsonSerializer.Serialize(d, json);
    return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) });
}
PlayerProgressData DecodeSave(string text)
{
    try
    {
        using JsonDocument doc = JsonDocument.Parse(text);
        string payload = doc.RootElement.GetProperty("payload").GetString();
        if (!SaveFileStore.SignatureMatches(payload, doc.RootElement.GetProperty("signature").GetString())) return null;
        return JsonSerializer.Deserialize<PlayerProgressData>(payload, json);
    }
    catch { return null; }
}

// ---------- fresh campaign defaults (one factory) ----------
var fresh = PlayerProgressData.CreateNewCampaign("c-1", "2026-10-21T00:00:00Z");
CampaignRecord fc = fresh.campaign;
Check(fresh.saveVersion == 5 && fresh.HasCampaign && fc.campaignId == "c-1", "new campaign: schema 5 with its own campaign id");
Check(fc.publicTrust == 40 && CampaignRecord.MaxTrust == 100 && fc.coins == 0, "new campaign: Public Trust 40/100 and 0 coins");
Check(fresh.totalExp == 0 && fresh.totalReputation == 0 && fresh.badgeRecords.Count == 0 && fresh.levelRecords.Count == 0 &&
      fresh.completedLevelIds.Count == 0 && fresh.passedLevelIds.Count == 0, "new campaign: 0 XP, no score hearts, badges or level results");
Check(fc.rewardClaims.Count == 0 && fc.items.Count == 0 && fc.ownedUpgradeIds.Count == 0 && fc.completedChapterIds.Count == 0,
    "new campaign: no claims, empty bag and drawer, no upgrades, no completed chapter");
Check(fc.currentChapterId == "Level_01" && fresh.IsLevelUnlocked(1) && !fresh.IsLevelUnlocked(2) && !fresh.activeCheckpoint.IsSet &&
      !fresh.levelOneIntroSeen, "new campaign: Level 1 current and available, no Level 2 launch, no checkpoint, intro not yet seen");
Check(!fresh.Normalize() && fresh.saveVersion == 5, "new campaign is already normalized and stays version 5");
var roundTrip = JsonSerializer.Deserialize<PlayerProgressData>(Snap(fresh), json);
Check(CampaignSave.Classify(true, roundTrip) == CampaignSaveState.Campaign && Snap(roundTrip) == Snap(fresh),
    "a saved campaign reloads as the same campaign");
Check(PlayerProgressData.CreateNewCampaign("c-2", "x").campaign.campaignId == "c-2" && Snap(PlayerProgressData.CreateNewCampaign("c-1", "2026-10-21T00:00:00Z")) == Snap(fresh),
    "every New Game builds the same defaults (only the id/time differ)");

// ---------- classification (before Normalize) ----------
var oldCareer = new PlayerProgressData { saveVersion = 4, totalExp = 140, totalReputation = 11, levelOneIntroSeen = true };
oldCareer.RecordLevelResult("Level_01", 140, 11, 150, 12, true, new[] { "know_your_taxpayer_1" });
oldCareer.RecordBadge("know_your_taxpayer_1", 3);
oldCareer.SetCheckpoint(new LevelCheckpoint { levelId = "Level_02", reviewJson = "{}" });
oldCareer.saveVersion = 4;
var oldDecoded = JsonSerializer.Deserialize<PlayerProgressData>(Snap(oldCareer), json);
Check(CampaignSave.Classify(false, null) == CampaignSaveState.NoCampaign && !CampaignSave.CanContinue(CampaignSaveState.NoCampaign),
    "fresh install: no campaign, Continue unavailable");
Check(CampaignSave.Classify(true, oldDecoded) == CampaignSaveState.RequiresNewStory &&
      CampaignSave.ContinueNotice(CampaignSaveState.RequiresNewStory) == "Start the updated story" &&
      !CampaignSave.CanContinue(CampaignSaveState.RequiresNewStory), "valid pre-v5 save: 'Start the updated story', Continue unavailable");
Check(CampaignSave.Classify(true, new PlayerProgressData { saveVersion = 2 }) == CampaignSaveState.RequiresNewStory &&
      CampaignSave.Classify(true, new PlayerProgressData { saveVersion = 5 }) == CampaignSaveState.RequiresNewStory,
    "older versions and a v5 file without a campaign never count as a campaign");
oldDecoded.Normalize();
Check(oldDecoded.saveVersion == 4 && !oldDecoded.HasCampaign && CampaignSave.Classify(true, oldDecoded) == CampaignSaveState.RequiresNewStory,
    "loading (normalizing) an old save keeps it version 4 without a campaign");
var future = new PlayerProgressData { saveVersion = 6, campaign = CampaignRecord.CreateFresh("future", "") };
Check(CampaignSave.Classify(true, future) == CampaignSaveState.UnsupportedFuture && !CampaignSave.AllowsOrdinarySave(CampaignSaveState.UnsupportedFuture) &&
      !CampaignSave.CanContinue(CampaignSaveState.UnsupportedFuture), "a newer game's save is unsupported: no Continue, no ordinary overwrite");
Check(CampaignSave.Classify(true, null) == CampaignSaveState.Unreadable && CampaignSave.AllowsOrdinarySave(CampaignSaveState.Unreadable),
    "unreadable files keep the existing start-fresh behavior");
string freshJson = Snap(fresh);
Check(!freshJson.Contains("know_your_taxpayer_1") && !freshJson.Contains("Level_02") && fresh.totalExp == 0,
    "nothing of the old save (badge, pass ids, checkpoint, totals) is in the new campaign");

// ---------- campaign repair ----------
var broken = PlayerProgressData.CreateNewCampaign(" c-3 ", "");
broken.campaign.publicTrust = 140;
broken.campaign.coins = -5;
broken.campaign.rewardClaims = new List<string> { "a", "a", " ", null };
broken.campaign.items = new List<CampaignItemStack> { new CampaignItemStack { itemId = "voucher", carried = 1, stored = -2 }, null,
    new CampaignItemStack { itemId = " voucher ", carried = 1, stored = 3 } };
broken.campaign.currentChapterId = "";
broken.Normalize();
Check(broken.campaign.campaignId == "c-3" && broken.campaign.publicTrust == 100 && broken.campaign.coins == 0 && broken.campaign.rewardClaims.Count == 1 &&
      broken.campaign.items.Count == 1 && broken.campaign.items[0].carried == 2 && broken.campaign.items[0].stored == 3 &&
      broken.campaign.currentChapterId == "Level_01", "campaign repair: trust 0..100, no negative coins/quantities, unique claims and stacks");
broken.campaign.publicTrust = -3;
broken.Normalize();
Check(broken.campaign.publicTrust == 0, "campaign repair: trust never below 0");

// ---------- commit with failure injection (disposable files only) ----------
string saveDir = Path.Combine(root, "Temp", "CampaignFoundationRegression", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(saveDir);
try
{
    var store = new SaveFileStore(Path.Combine(saveDir, "progress.json"));
    store.Write(EncodeSave(oldCareer));
    var olderGeneration = Clone(oldCareer);
    olderGeneration.totalExp = 100;
    store.Write(EncodeSave(olderGeneration));
    byte[] mainBefore = File.ReadAllBytes(store.FilePath), backupBefore = File.ReadAllBytes(store.BackupPath);
    bool FilesUnchanged() => File.ReadAllBytes(store.FilePath).SequenceEqual(mainBefore) && File.ReadAllBytes(store.BackupPath).SequenceEqual(backupBefore) && !File.Exists(store.TempPath);

    var candidate = PlayerProgressData.CreateNewCampaign("commit-1", "2026-10-21T00:00:00Z");
    string candidateSnap = Snap(candidate);

    bool ok = CampaignSave.TryCommit(candidate, _ => throw new InvalidOperationException("injected encode"), store.Reset,
        () => store.Restore(store.Capture()), out string error);
    Check(!ok && error.Contains("prepare") && FilesUnchanged(), "encode failure: nothing written, error reported");

    SaveFileStore.Generations previousFiles = store.Capture();
    Directory.CreateDirectory(store.TempPath);
    ok = CampaignSave.TryCommit(candidate, EncodeSave, store.Reset, () => store.Restore(previousFiles), out error);
    Directory.Delete(store.TempPath);
    Check(!ok && error.Contains("save") && FilesUnchanged(), "first write failure: both old generations intact, error reported");

    previousFiles = store.Capture();
    ok = CampaignSave.TryCommit(candidate, EncodeSave, text => { store.Write(text); throw new IOException("injected after one generation"); },
        () => store.Restore(previousFiles), out error);
    Check(!ok && FilesUnchanged(), "failure after the main file was replaced: the previous main and backup are put back");
    Check(CampaignSave.Classify(true, DecodeSave(store.Read(t => DecodeSave(t) != null, out _, out _))) == CampaignSaveState.RequiresNewStory,
        "after failed attempts the device still holds the old save (no partial campaign)");
    Check(Snap(candidate) == candidateSnap, "a failed commit does not change the candidate (nothing half-published)");

    File.WriteAllText(store.RejectedPath, "old rejected save");
    previousFiles = store.Capture();
    ok = CampaignSave.TryCommit(candidate, EncodeSave, store.Reset, () => store.Restore(previousFiles), out error);
    PlayerProgressData main = DecodeSave(File.ReadAllText(store.FilePath)), backup = DecodeSave(File.ReadAllText(store.BackupPath));
    Check(ok && error == null && main != null && backup != null && main.campaign.campaignId == "commit-1" && backup.campaign.campaignId == "commit-1" &&
          !File.Exists(store.RejectedPath), "retry commits the fresh campaign once: main and backup both hold it, old rejected file removed");
    Check(CampaignSave.Classify(true, main) == CampaignSaveState.Campaign && Snap(main) == candidateSnap && main.totalExp == 0 && main.badgeRecords.Count == 0,
        "the committed file is exactly the fresh candidate (no old XP, badges, pass ids or checkpoint)");

    // Ordinary saving and backup recovery continue for the campaign.
    main.RecordTutorialSeen("Level_01/move");
    store.Write(EncodeSave(main));
    File.WriteAllText(store.FilePath, "damaged");
    string recovered = store.Read(t => DecodeSave(t) != null, out bool fromBackup, out _);
    PlayerProgressData recoveredData = DecodeSave(recovered);
    Check(fromBackup && CampaignSave.Classify(true, recoveredData) == CampaignSaveState.Campaign && recoveredData.campaign.campaignId == "commit-1",
        "a damaged campaign save recovers the campaign from its ordinary backup");
    File.Delete(store.RejectedPath);
    File.WriteAllText(store.FilePath, EncodeSave(main).Replace(SaveFileStore.Sign(JsonSerializer.Serialize(main, json)), SaveFileStore.Sign("other")));
    recovered = store.Read(t => DecodeSave(t) != null, out fromBackup, out _);
    Check(fromBackup && File.Exists(store.RejectedPath) && DecodeSave(recovered).campaign.coins == 0,
        "an invalid signature is rejected (kept aside) and the backup is used");

    Check(!CampaignSave.TryCommit(oldDecoded, EncodeSave, store.Reset, null, out error) && error != null,
        "only a complete v5 campaign candidate can be committed");
}
finally { Directory.Delete(saveDir, true); }

// ---------- save owner wiring (source contract; Unity runtime not run here) ----------
string manager = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "RewardsSystem", "PlayerProgressManager.cs"));
int Index(string body, string text) => body.IndexOf(text, StringComparison.Ordinal);
string newGame = manager.Substring(Index(manager, "public bool StartNewCampaign"));
newGame = newGame.Substring(0, Index(newGame, "private void Publish"));
Check(Index(newGame, "store.Capture()") > 0 && Index(newGame, "store.Capture()") < Index(newGame, "CampaignSave.TryCommit(") &&
      Index(newGame, "CampaignSave.TryCommit(") < Index(newGame, "Publish(candidate") && Index(newGame, "progress =") < 0,
    "New Game publishes the candidate only after the commit succeeded");
string load = manager.Substring(Index(manager, "private PlayerProgressData LoadOrCreate"));
Check(Index(load, "campaignState = CampaignSave.Classify(true, data);") > 0 &&
      Index(load, "campaignState = CampaignSave.Classify(true, data);") < Index(load, "data.Normalize()"),
    "the save is classified previousFiles Normalize (old saves are not turned into campaigns)");
Check(Index(load, "UnsupportedFuture") < Index(load, "data.Normalize()") && manager.Contains("if (!CampaignSave.AllowsOrdinarySave(campaignState))"),
    "a newer game's save is neither normalized nor rewritten; ordinary Save refuses to overwrite it");
Check(manager.Contains("data.saveVersion >= PlayerProgressData.SignedSinceVersion") && manager.Contains("SaveFileStore.Sign(payload)") &&
      !manager.Contains("PlayerPrefs.DeleteAll"), "decoder/signature policy unchanged; settings preferences are never wiped");

// =====================================================================
// T03: New Game, Continue and run modes
// =====================================================================

// ---------- run modes ----------
CampaignRun.EndRun();
Check(CampaignRun.Resolve(CampaignRun.Chosen, false) == CampaignRunMode.DirectEditor, "a level loaded with no menu route is a direct Editor run");
Check(CampaignRun.Resolve(CampaignRunMode.Story, true) == CampaignRunMode.Practice, "a practice drill always resolves to Practice");
CampaignRun.Begin(CampaignRunMode.DirectEditor);
Check(CampaignRun.Chosen == CampaignRunMode.None, "a route cannot pick the direct Editor mode explicitly");
CampaignRun.Begin(CampaignRunMode.MasteryReplay);
Check(CampaignRun.Resolve(CampaignRun.Chosen, false) == CampaignRunMode.MasteryReplay, "career replay keeps the replay mode");
CampaignRun.EndRun();
bool onlyStory = true;
foreach (CampaignRunMode m in Enum.GetValues(typeof(CampaignRunMode)))
    if (CampaignRun.AllowsStoryWrites(m) != (m == CampaignRunMode.Story) || CampaignRun.AllowsStoryCheckpoint(m) != (m == CampaignRunMode.Story)) onlyStory = false;
Check(onlyStory, "only Story may write story progress or the story checkpoint (replay, practice, editor never)");
Check(CampaignRun.WhatChanges(CampaignRunMode.MasteryReplay).Contains("best XP and badges") && CampaignRun.Label(CampaignRunMode.Practice) == "PRACTICE",
    "each mode explains what may change");

// ---------- menu model per save state (user layout 2026-10-22) ----------
bool OnlyPlay(CampaignMenuModel m) => m.ShowPlay && !m.ShowNewGameAndContinue && !m.ShowTutorials && !m.HasStory;
Check(OnlyPlay(CampaignMenu.Build(CampaignSaveState.NoCampaign, Fresh())), "fresh install: a single Play, Tutorials hidden");
Check(OnlyPlay(CampaignMenu.Build(CampaignSaveState.RequiresNewStory, oldDecoded)),
    "an older development save (XP, badges, passed Level 1, checkpoint) is not a started story: Play only");
Check(OnlyPlay(CampaignMenu.Build(CampaignSaveState.UnsupportedFuture, future)) && OnlyPlay(CampaignMenu.Build(CampaignSaveState.Unreadable, null)),
    "newer-version and unreadable saves are not a started story either");
Check(OnlyPlay(CampaignMenu.Build(CampaignSaveState.Campaign, oldDecoded)) && OnlyPlay(CampaignMenu.Build(CampaignSaveState.Campaign, null)),
    "the state must hold an actual committed campaign record, not just the flag");
var story = PlayerProgressData.CreateNewCampaign("menu-1", "");
CampaignMenuModel started = CampaignMenu.Build(CampaignSaveState.Campaign, story);
Check(started.HasStory && started.ShowPlay && !started.ShowNewGameAndContinue && !started.ShowTutorials,
    "committed unfinished campaign: Play only; campaign creation never unlocks the returning menu");
Check(started.ConfirmTitle == "Start a New Game?" && started.ConfirmButton == "Start New Game" && started.CancelButton == "Cancel" &&
      started.ConfirmBody.Contains("XP, badges, public trust, coins, collected items, and purchases") && started.ConfirmBody.Contains("settings will stay the same"),
    "New Game confirmation text names what is replaced and that settings stay");
var level1Done = PlayerProgressData.CreateNewCampaign("menu-2", "");
level1Done.campaign.MarkChapterCompleted("Level_01");
Check(CampaignMenu.Build(CampaignSaveState.Campaign, level1Done).ShowNewGameAndContinue &&
      CampaignMenu.Build(CampaignSaveState.Campaign, level1Done).ShowTutorials &&
      !CampaignMenu.Build(CampaignSaveState.Campaign, level1Done).ShowPlay,
    "saved Level 1 story completion unlocks returning menu");
Check(CampaignMenu.Build(CampaignSaveState.Campaign, level1Done, false).ShowPlay,
    "unsaved completion mutation cannot unlock the menu when its write failed");
var interrupted = PlayerProgressData.CreateNewCampaign("interrupted", "");
interrupted.levelOneIntroSeen = true;
interrupted.RecordLevelResult("Level_01", 150, 12, 150, 12, true, null);
Check(CampaignMenu.Build(CampaignSaveState.Campaign, interrupted).ShowPlay,
    "intro history and even mastery completion/XP never substitute for story completion");
interrupted.campaign.MarkChapterCompleted("Level_02");
Check(CampaignMenu.Build(CampaignSaveState.Campaign, interrupted).ShowPlay,
    "another chapter cannot substitute for Level 1");
Check(CampaignMenu.Build(CampaignSaveState.Campaign, Clone(level1Done)).ShowNewGameAndContinue,
    "serialized Level 1 story completion survives reload");
Check(CampaignMenu.Build(CampaignSaveState.Campaign, PlayerProgressData.CreateNewCampaign("reset", "")).ShowPlay,
    "New Game resets returning-menu access");
Check(!CampaignMenu.ShopUnlocked(story) && !CampaignMenu.ShopUnlocked(level1Done) && CampaignMenu.ShopLockedMessage.Contains("first client case"),
    "the Shop entry stays locked until the shop is introduced (moving it does not unlock it)");
Check(story.campaign.MarkChapterCompleted("Level_01") && !story.campaign.MarkChapterCompleted("Level_01") && story.campaign.IsChapterCompleted(" Level_01 "),
    "a chapter's story completion is recorded once");
Check(!new CampaignRecord().MarkChapterCompleted("Level_01"), "no chapter can complete without a campaign");
Check(!PlayerProgressData.CreateNewCampaign("menu-3", "").HasSeenLevelOneIntro,
    "a fresh campaign has not seen the opening, so the opening after Play / New Game cannot be skipped");

// ---------- career: Level 2 pending for the campaign, launch modes ----------
CareerCatalog storyCatalog = Catalog2();
CareerCatalog Catalog2()
{
    var c = new CareerCatalog();
    c.chapters.Add(new CareerChapterDef { id = "Level_01", number = 1, scene = "Cutscene2.0", cases = 1 });
    c.chapters.Add(new CareerChapterDef { id = "Level_02", number = 2, scene = "Office_Level2", cases = 1, campaignPending = true });
    c.chapters.Add(new CareerChapterDef { id = "Level_03", number = 3, comingSoon = true });
    return c;
}
var passedCampaign = PlayerProgressData.CreateNewCampaign("career-1", "");
passedCampaign.RecordLevelResult("Level_01", 150, 12, 150, 12, true, null);
passedCampaign.campaign.MarkChapterCompleted("Level_01");
CareerSnapshot campaignView = CareerProgress.Build(passedCampaign, storyCatalog, Rules);
Check(campaignView.Chapters[1].PendingIntegration && campaignView.Chapters[1].Status == CareerChapterStatus.ComingSoon && !campaignView.Chapters[1].Launchable,
    "campaign: Level 2 is pending integration and cannot launch even after a passed Level 1");
Check(!CareerProgress.Build(oldDecoded, storyCatalog, Rules).Chapters[1].PendingIntegration,
    "pending only applies to campaigns (Level 2's own scene/build data is untouched)");
Check(CareerProgress.LaunchMode(campaignView.Chapters[0]) == CampaignRunMode.MasteryReplay,
    "a chapter the story already completed launches as a mastery replay");
CareerSnapshot newView = CareerProgress.Build(PlayerProgressData.CreateNewCampaign("career-2", ""), storyCatalog, Rules);
Check(newView.Chapters[0].Launchable && CareerProgress.LaunchMode(newView.Chapters[0]) == CampaignRunMode.Story && !newView.Chapters[1].Launchable,
    "new campaign: Chapter 1 starts the story; Chapter 2 cannot launch");
using (JsonDocument chaptersDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Career", "chapters.json"))))
{
    var level2 = chaptersDoc.RootElement.GetProperty("chapters").EnumerateArray().First(c => c.GetProperty("id").GetString() == "Level_02");
    Check(level2.TryGetProperty("campaignPending", out JsonElement pendingFlag) && pendingFlag.GetBoolean() && level2.GetProperty("scene").GetString() == "Office_Level2",
        "chapters.json marks Level 2 campaignPending and keeps its scene for isolated development");
}

// ---------- wiring (source contracts; the Unity menu itself is a manual check) ----------
string Src(params string[] parts) => File.ReadAllText(Path.Combine(new[] { root, "Assets", "Scripts" }.Concat(parts).ToArray()));
string Body(string source, string start) { int i = Index(source, start); return i < 0 ? "" : source.Substring(i); }
string menuEntry = Src("UI", "MenuEntry.cs");
string startNew = Body(menuEntry, "public static bool StartNewGame");
Check(Index(startNew, "manager.StartNewCampaign(") > 0 && Index(startNew, "manager.StartNewCampaign(") < Index(startNew, "SceneFade.FadeOutThenLoad(Cutscene01Runtime.SceneName") &&
      Index(startNew, "return false;") < Index(startNew, "SceneFade.FadeOutThenLoad") && Index(startNew, "storyLaunching = true;") < Index(startNew, "SceneFade.FadeOutThenLoad"),
    "Play / New Game load the opening only after the campaign was saved, and lock further launches");
Check(Body(menuEntry, "private static bool Blocked").Contains("storyLaunching ||") && Body(menuEntry, "public static void MenuOpened()").Contains("storyLaunching = false;"),
    "repeated taps cannot create a second campaign or a second scene transition");
string play = Body(menuEntry, "public static void Play(");
play = play.Substring(0, Index(play, "public static bool StartNewGame"));
Check(Index(play, "StartNewGame(") > 0 && play.Contains("if (!CurrentModel().HasStory)") && play.Contains("resumeLevel ? \"Office_Level1\" : Cutscene01Runtime.SceneName"), "Play (no story) goes straight to the opening (no progress-module detour)");
string cont = Between(menuEntry, "public static bool Continue(", "private static void ResumeLevelOneEndFlow()");
Check(cont.Contains("PendingLevelOneEndFlow") && Index(cont, "ResumeLevelOneEndFlow()") < Index(cont, "if (!HasCareerAccess)") &&
      cont.Contains("cameraManager.SwitchToLevelSelector()") && !cont.Contains("CampaignRun.Begin"),
    "Continue resumes a pending Level 1 end flow first; otherwise it opens the saved-progress module without starting a story run");
Check(menuEntry.Contains("return open && HasCareerAccess;") && menuEntry.Contains("public static bool HasCareerAccess => CurrentModel().HasCompletedFirstChapter;"),
    "menu access derives from saved Level 1 completion (refreshes on load and successful saves)");
string manager2 = Src("RewardsSystem", "PlayerProgressManager.cs");
Check(manager2.Contains("mode != CampaignRunMode.MasteryReplay && (levelTotal.LevelId") && manager2.Contains("CampaignRun.AllowsStoryWrites(mode) && progress.HasCampaign"),
    "a replay never clears the story checkpoint; only story runs complete chapters");
Check(manager2.Contains("CurrentRunMode == CampaignRunMode.MasteryReplay) return false;"), "replays cannot save over the story checkpoint");
string inGame = Src("Settings", "InGameSettings.cs");
string leave = Body(inGame, "public bool LeaveLevel");
Check(Index(leave, "SaveBeforeLeaving()) return false;") > 0 && Index(leave, "SaveBeforeLeaving()) return false;") < Index(leave, "SceneFade.FadeOutThenLoad"),
    "leaving a story run saves first; a failed save keeps the player in the run");
string settingsBoard = Src("Settings", "SettingsBoard3D.cs");
string confirm = Body(settingsBoard, "private void ConfirmAction()");
Check(settingsBoard.Contains("public bool OpenNewGameConfirmation()") && settingsBoard.Contains("RequestAction(SettingsAction.NewGame);") &&
      confirm.Contains("success = MenuEntry.StartNewGame(cameraManager, out newGameError);") && !settingsBoard.Contains("ResetProgress()"),
    "New Game reuses the confirmation card and the single atomic StartNewGame route");
Check(confirm.Contains("pendingAction == SettingsAction.NewGame)) return;") &&
      confirm.Contains("newGameError ?? CampaignMenu.NewGameFailed"),
    "a confirmed New Game keeps the card locked while the opening loads; a failed save shows an error and allows retry");
Check(Body(settingsBoard, "private void CancelConfirmation()").Contains("if (closeBoard) GoBack();"), "cancelling the menu's New Game card closes it and returns to the menu");
Check(settingsBoard.Contains("hideWhileOpen = MenuEntry.WithStoryButtons(hideWhileOpen);") &&
      Src("Training", "TrainingBinder3D.cs").Contains("hideWhileOpen = MenuEntry.WithStoryButtons(hideWhileOpen);"),
    "New Game / Continue hide with the other notes behind Settings and the training binder");
string mainMenu = Src("MainMenu.cs");
Check(mainMenu.Contains("case MenuEntry.NewGameButtonName:") && mainMenu.Contains("case MenuEntry.ContinueButtonName:") &&
      mainMenu.Contains("settingsBoard.OpenNewGameConfirmation()") && mainMenu.Contains("MenuEntry.MenuOpened();") &&
      Index(Body(mainMenu, "private void RefreshMenu"), "campaignPanel.Apply(MenuEntry.CurrentModel())") > 0,
    "the corkboard routes Play / New Game / Continue and re-applies the state on every save change");
string panel = Src("UI", "Menu", "MainMenuCampaignPanel.cs");
Check(panel.Contains("[SerializeField] private GameObject playNote;") && !panel.Contains("Instantiate(") && panel.Contains("Find(scene, SettingsButtonName)") &&
      panel.Contains("SetActive(playNote, model.ShowPlay)") && panel.Contains("SetActive(newGameNote, model.ShowNewGameAndContinue)") &&
      !panel.Contains("StartNewCampaign") && !panel.Contains(".Save("),
    "menu hides inactive notes including colliders and remains an observer without save writes");
Check(Index(startNew, "storyLaunching = true;") < Index(startNew, "manager.StartNewCampaign(") &&
      mainMenu.Contains("actionPending || MenuEntry.IsStoryLaunching || (settingsBoard != null && settingsBoard.MenuButtonsHidden)") && panel.Contains("if (MenuEntry.IsStoryLaunching) return;"),
    "launch lock precedes save publication and both menu observers freeze during scene exit");
Check(mainMenu.Contains("button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();") && mainMenu.Contains("button.enabled = false;"),
    "copied Settings persistent callbacks cannot open Settings or duplicate physics actions");
Check(panel.Contains("Resources.LoadAll<Mesh>(\"MenuActionLettering\")") &&
      panel.Contains("label.sharedMesh = mesh;") &&
      !panel.Contains("TextMeshPro") && !panel.Contains("fontSharedMaterial") &&
      panel.Contains("renderer.sharedMaterials = original.GetComponent<MeshRenderer>().sharedMaterials;") && !panel.Contains("wordMesh.gameObject.SetActive(false)"),
    "notes keep Settings typography/materials and reuse matching 3D glyph meshes");
Check(panel.Contains("[ExecuteAlways]") && panel.Contains("[SerializeField] private GameObject newGameNote;") &&
      panel.Contains("[SerializeField] private GameObject continueNote;") &&
      !panel.Contains("Copy(settings, MenuEntry.NewGameButtonName") && !panel.Contains("Copy(settings, MenuEntry.ContinueButtonName") &&
      !panel.Contains("newGameNote.transform.localPosition =") && !panel.Contains("continueNote.transform.localScale ="),
    "New Game and Continue bind saved editable scene objects without runtime recreation or layout overrides");
Check(menuEntry.Contains("NewGameButtonName = \"new_game_button\"") && mainMenu.Contains("case MenuEntry.LegacyNewGameButtonName:"),
    "scene-authored New Game name and older duplicate names both route to the established confirmation");
Check(mainMenu.Contains("Press(note, () => MenuEntry.Play(cameraManager))") &&
      mainMenu.Contains("Press(note, OpenNewGameConfirmation)") && mainMenu.Contains("Press(note, () => MenuEntry.Continue(cameraManager))") &&
      mainMenu.Contains("Press(note, OpenTraining)") && mainMenu.Contains("Press(note, OpenSettings)") &&
      mainMenu.Contains("actionPending = true;") && panel.Contains("presses.Count != 0) return false;"),
    "all five authored buttons share one press-completion dispatcher and reject repeated activation");
string pressTick = Body(panel, "private void LateUpdate()");
Check(Index(pressTick, "press.Note.localPosition = press.Rest;") < Index(pressTick, "press.Completed?.Invoke();") &&
      Index(pressTick, "presses.RemoveAt(i);") < Index(pressTick, "press.Completed?.Invoke();"),
    "press restores its authored resting position and removes its callback before executing the menu action");
Check(panel.Contains("private float idleSwayDegrees = 0.65f;") && panel.Contains("2f * Mathf.PI / 3.8f") &&
      panel.Contains("Quaternion.AngleAxis(") && panel.Contains("sway.Note.localRotation = sway.Rest;") &&
      !panel.Contains("sway.Note.localScale =") && !panel.Contains("sway.Note.localPosition ="),
    "subtle idle sway rotates authored notes only and restores their rest rotations on disable");
Check(panel.Contains("settingsBoard.IsOpen || settingsBoard.MenuButtonsHidden") &&
      settingsBoard.Contains("menuHiddenAction != null && pops.Count == 0"),
    "visibility observers cannot resurrect a note hidden by the disappearance transition");
string letteringBuilder = File.ReadAllText(Path.Combine(root, "Tests", "BuildMenuActionLettering.py"));
Check(letteringBuilder.Contains("curve.size = 0.904") && letteringBuilder.Contains("curve.bevel_depth = 0.018") &&
      letteringBuilder.Contains("len(source.data.vertices) == 35412") && letteringBuilder.Contains("assert error < 0.00001") &&
      File.Exists(Path.Combine(root, "Assets", "Resources", "MenuActionLettering.fbx")) &&
      File.Exists(Path.Combine(root, "Assets", "Resources", "MenuActionLettering.fbx.meta")),
    "imported label asset exists and its builder proves the original Settings font/bevel against every source vertex");
Check(Index(Body(manager2, "public bool Save()"), "store.Write(Encode(progress));") <
      Index(Body(manager2, "public bool Save()"), "RememberSavedCompletion();") &&
      menuEntry.Contains("manager.HasSavedFirstChapterCompletion") &&
      Src("UI", "LevelCompleteUI.cs").Contains("if (!PlayerProgressManager.Instance.SaveBeforeLeaving())"),
    "only successful save/load publishes completion; results refuse exit when saving fails");
Check(menuEntry.Contains("RuntimeInitializeLoadType.SubsystemRegistration") &&
      Src("Cutscenes", "Cutscene01Runtime.cs").Contains("canSkip = !forceOpening &&"),
    "Play Mode static state resets and the requested opening cannot be skipped");
string presenter = Src("UI", "Menu", "CareerBoardPresenter.cs");
Check(Index(presenter, "CampaignRun.Begin(CareerProgress.LaunchMode(chapter));") > 0 &&
      Index(presenter, "CampaignRun.Begin(CareerProgress.LaunchMode(chapter));") < Index(presenter, "SceneManager.LoadScene(chapter.Def.scene);"),
    "career launches set Story or Replay before loading");
string shop = Body(presenter, "private void OpenShop()");
shop = shop.Substring(0, Index(shop, "private void ShowShopNotice"));
Check(presenter.Contains("\"Shop Button\", \"SHOP\", new Vector3(-525f, 420f, -10f)") && shop.Contains("CampaignMenu.ShopUnlocked(") &&
      !shop.Contains("Save(") && !shop.Contains("campaign."), "the Shop entry sits beside BACK in the progress module, respects the lock and changes nothing");

// =====================================================================
// T04: Level 1 field checkpoint (town conversations, profiles, board)
// =====================================================================

var profileMap = Level1FieldCheckpointRules.ProfileMap(TaxpayerProfileDatabase.All);
var areaIds = Enum.GetValues(typeof(TaxpayerClassification)).Cast<TaxpayerClassification>().Select(Level1FieldCheckpointRules.SlotId).ToList();
string L1(Level1FieldCheckpoint c) => JsonSerializer.Serialize(c, json);
Level1FieldCheckpoint Field(string phase, int talked, int collected, bool intro)
{
    var c = new Level1FieldCheckpoint { version = Level1FieldCheckpoint.CurrentVersion, boardPhase = phase, guideIntroDone = intro };
    foreach (TaxpayerProfile p in TaxpayerProfileDatabase.All.Take(talked)) c.talkedNpcIds.Add(p.NpcId);
    foreach (TaxpayerProfile p in TaxpayerProfileDatabase.All.Take(collected)) c.collectedProfileIds.Add(p.ProfileId);
    c.objectiveTriggers.Add("event:ProfileCollected");
    return c;
}
Check(profileMap.Count == 4 && areaIds.Count == 4 && profileMap["profile_marie"] == "npc_marie", "content map: four profiles with their NPCs, four board areas");

// ---------- every town phase round-trips through the signed save ----------
var activeBoard = Field(Level1FieldCheckpoint.PhaseActive, 4, 4, true);
activeBoard.placementAttempts = 5;
activeBoard.wrongPlacements = 2;
activeBoard.placements.Add(new Level1Placement { profileId = "profile_marie", slotId = "ResidentCitizen" });
activeBoard.placements.Add(new Level1Placement { profileId = "profile_milo", slotId = "NonResidentAlien" });
activeBoard.slotAttempts.Add(new Level1SlotAttempts { slotId = "ResidentCitizen", attempts = 1, firstTrySolved = true });
activeBoard.slotAttempts.Add(new Level1SlotAttempts { slotId = "NonResidentAlien", attempts = 3 });
activeBoard.guideLessonsDone.AddRange(new[] { "welcome", "hud-tour", "notes", "move", "look", "follow", "corkboard" });
var phases = new (string name, Level1FieldCheckpoint cp)[]
{
    ("first talk, profile on the ground", Field("", 1, 0, false)),
    ("two collected, third talked", Field("", 3, 2, false)),
    ("all four collected, guide waiting", Field("", 4, 4, false)),
    ("board open with pins and attempts", activeBoard),
    ("board solved, wrap-up due", Field(Level1FieldCheckpoint.PhaseSolved, 4, 4, true)),
    ("side quest rewarded", Field(Level1FieldCheckpoint.PhaseRewarded, 4, 4, true)),
};
string phaseDir = Path.Combine(root, "Temp", "CampaignFoundationRegression", "t04-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(phaseDir);
try
{
    foreach (var (name, cp) in phases)
    {
        var data = PlayerProgressData.CreateNewCampaign("t04-" + name.GetHashCode(), "");
        data.campaign.levelOne = Clone2(cp);
        var store = new SaveFileStore(Path.Combine(phaseDir, "progress-" + Math.Abs(name.GetHashCode()) + ".json"));
        store.Write(EncodeSave(data));
        PlayerProgressData loaded = DecodeSave(File.ReadAllText(store.FilePath));
        bool repaired = loaded.Normalize();
        Level1FieldCheckpoint back = loaded.campaign.levelOne;
        string expected = L1(cp);
        Level1RestoreOutcome outcome = Level1FieldCheckpointRules.Validate(back, profileMap, areaIds, 4);
        Check(!repaired && outcome == Level1RestoreOutcome.Restored && L1(back) == expected && loaded.saveVersion == 5 && loaded.HasCampaign,
            $"signed save round trip restores exactly: {name}");
    }
}
finally { Directory.Delete(phaseDir, true); }
Level1FieldCheckpoint Clone2(Level1FieldCheckpoint c) => JsonSerializer.Deserialize<Level1FieldCheckpoint>(L1(c), json);

// ---------- normalization: deterministic, nearest safe phase ----------
var messy = new Level1FieldCheckpoint
{
    version = 1, boardPhase = " active ", guideIntroDone = false, placementAttempts = -3, wrongPlacements = 9,
    talkedNpcIds = new List<string> { "npc_marie", " npc_marie ", "npc_ghost", null, "" },
    collectedProfileIds = new List<string> { "profile_marie", "profile_milo", "profile_jhunjhun", "profile_zorbix", "profile_zorbix", "profile_unknown" },
    placements = new List<Level1Placement>
    {
        new Level1Placement { profileId = "profile_marie", slotId = "ResidentCitizen" },
        new Level1Placement { profileId = "profile_marie", slotId = "ResidentAlien" },      // same card twice: first kept
        new Level1Placement { profileId = "profile_milo", slotId = "ResidentCitizen" },     // same area twice: first kept
        new Level1Placement { profileId = "profile_zorbix", slotId = "Moon" },              // unknown area
        null
    },
    slotAttempts = new List<Level1SlotAttempts>
    {
        new Level1SlotAttempts { slotId = "ResidentCitizen", attempts = -1, firstTrySolved = true },
        new Level1SlotAttempts { slotId = "ResidentCitizen", attempts = 4 },
        new Level1SlotAttempts { slotId = "Moon", attempts = 2 }
    }
};
var messyCopy = Clone2(messy);
Level1RestoreOutcome messyOutcome = Level1FieldCheckpointRules.Validate(messy, profileMap, areaIds, 4);
Check(messyOutcome == Level1RestoreOutcome.Repaired && messy.boardPhase == "active" && messy.guideIntroDone,
    "repairs report Repaired; an open board implies the guide's intro");
Check(messy.talkedNpcIds.Count == 4 && messy.talkedNpcIds.Contains("npc_zorbix") && !messy.talkedNpcIds.Contains("npc_ghost") &&
      messy.collectedProfileIds.Count == 4 && !messy.collectedProfileIds.Contains("profile_unknown"),
    "unknown and duplicate ids are dropped; a collected profile implies its NPC's handover");
Check(messy.placements.Count == 1 && messy.placements[0].profileId == "profile_marie" && messy.placements[0].slotId == "ResidentCitizen" &&
      messy.slotAttempts.Count == 1 && messy.slotAttempts[0].attempts == 0 && !messy.slotAttempts[0].firstTrySolved,
    "one pin per card and per area (first kept), unknown areas dropped, attempt rows repaired");
Check(messy.placementAttempts == 0 && messy.wrongPlacements == 0, "counters are never negative and wrong placements never exceed attempts");
var lessons = new Level1FieldCheckpoint { version = 1, guideLessonsDone = new List<string> { "welcome", " welcome ", "", null, "hud-tour" } };
lessons.Repair();
Check(lessons.guideLessonsDone.SequenceEqual(new[] { "welcome", "hud-tour" }), "finished guide lessons are kept as clean, unique ids");
Level1FieldCheckpointRules.Validate(messyCopy, profileMap, areaIds, 4);
var messyAgain = Clone2(messy);
Check(L1(messyCopy) == L1(messy) && Level1FieldCheckpointRules.Validate(messyAgain, profileMap, areaIds, 4) == Level1RestoreOutcome.Restored,
    "normalization is deterministic and idempotent");

var tooEarly = Field(Level1FieldCheckpoint.PhaseActive, 3, 3, true);
tooEarly.placements.Add(new Level1Placement { profileId = "profile_marie", slotId = "ResidentCitizen" });
tooEarly.placementAttempts = 2;
Check(Level1FieldCheckpointRules.Validate(tooEarly, profileMap, areaIds, 4) == Level1RestoreOutcome.Repaired &&
      tooEarly.boardPhase == "" && !tooEarly.guideIntroDone && tooEarly.placements.Count == 0 && tooEarly.placementAttempts == 0 &&
      tooEarly.collectedProfileIds.Count == 3, "a board without all four profiles falls back to the town (profiles kept, board cleared)");
var introOnly = Field("", 4, 4, true);
Check(Level1FieldCheckpointRules.Validate(introOnly, profileMap, areaIds, 4) == Level1RestoreOutcome.Repaired && !introOnly.guideIntroDone,
    "a guide intro without an open board is undone, so the guide can open the board again");
var pinsUncollected = Field(Level1FieldCheckpoint.PhaseActive, 4, 4, true);
pinsUncollected.collectedProfileIds.Remove("profile_milo");
pinsUncollected.placements.Add(new Level1Placement { profileId = "profile_milo", slotId = "ResidentAlien" });
Level1FieldCheckpointRules.Validate(pinsUncollected, profileMap, areaIds, 4);
Check(pinsUncollected.placements.Count == 0 && pinsUncollected.boardPhase == "", "pins only for collected profiles");
var oddPhase = Field("halfway", 4, 4, false);
Check(Level1FieldCheckpointRules.Validate(oddPhase, profileMap, areaIds, 4) == Level1RestoreOutcome.Repaired && oddPhase.boardPhase == "",
    "an unknown board phase becomes 'not opened'");
var newer = Field("", 2, 1, false);
newer.version = Level1FieldCheckpoint.CurrentVersion + 1;
Check(Level1FieldCheckpointRules.Validate(newer, profileMap, areaIds, 4) == Level1RestoreOutcome.Reset,
    "a checkpoint from a newer format is not half-restored (Reset)");
Check(Level1FieldCheckpointRules.Validate(null, profileMap, areaIds, 4) == Level1RestoreOutcome.None &&
      Level1FieldCheckpointRules.Validate(new Level1FieldCheckpoint(), profileMap, areaIds, 4) == Level1RestoreOutcome.None,
    "no checkpoint (absent or never written) means a normal level start");

// ---------- older v5 data and stored shape ----------
string oldV5 = Snap(PlayerProgressData.CreateNewCampaign("v5-before-t04", ""));
using (JsonDocument oldDoc = JsonDocument.Parse(oldV5))
{
    var trimmed = new Dictionary<string, object>();
    foreach (JsonProperty prop in oldDoc.RootElement.EnumerateObject())
    {
        if (prop.Name != "campaign") { trimmed[prop.Name] = prop.Value; continue; }
        var campaignFields = new Dictionary<string, object>();
        foreach (JsonProperty field in prop.Value.EnumerateObject()) if (field.Name != "levelOne") campaignFields[field.Name] = field.Value;
        trimmed[prop.Name] = campaignFields;
    }
    var beforeT04 = JsonSerializer.Deserialize<PlayerProgressData>(JsonSerializer.Serialize(trimmed), json);
    beforeT04.Normalize();
    Check(beforeT04.HasCampaign && beforeT04.campaign.levelOne != null && !beforeT04.campaign.levelOne.IsSet &&
          CampaignSave.Classify(true, beforeT04) == CampaignSaveState.Campaign, "a v5 save written before T04 (no levelOne) loads safely with no field checkpoint");
}
var fieldNames = typeof(Level1FieldCheckpoint).GetFields().Select(f => f.Name)
    .Concat(typeof(Level1Placement).GetFields().Select(f => f.Name)).Concat(typeof(Level1SlotAttempts).GetFields().Where(f => !f.IsLiteral).Select(f => f.Name)).ToList();
Check(!fieldNames.Any(n => n.IndexOf("classification", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("position", StringComparison.OrdinalIgnoreCase) >= 0 ||
      n.IndexOf("camera", StringComparison.OrdinalIgnoreCase) >= 0 || n == "isCorrect" || n == "correct"),
    "the checkpoint stores ids and counters only: no answer key, correctness flag, position or camera");
Check(PlayerProgressData.CreateNewCampaign("t04-fresh", "").campaign.levelOne.IsSet == false, "New Game starts with no field checkpoint");

// ---------- wiring (source contracts; restore is a Unity runtime path) ----------
string cpSrc = Src("SideQuest1", "Level1FieldCheckpointer.cs");
string restore = Body(cpSrc, "private void TryRestore()");
restore = restore.Substring(0, Index(restore, "private static void Tell"));
Check(!restore.Contains("TownEvents.Raise") && !restore.Contains("GameplayEvents.Raise") && !restore.Contains("CollectProfile(") &&
      !restore.Contains("BeginSideQuest()") && restore.Contains("score.Restoring = true;") && restore.Contains("finally { score.Restoring = false; }") &&
      restore.Contains("OfficeObjectives.Instance.RestoreTriggers(") && restore.Contains("director.Fire("),
    "restore raises no gameplay events, never re-collects or re-opens through the normal paths, and re-earns score quietly");
string Between(string source, string start, string end) { string b = Body(source, start); int e = Index(b, end); return e > 0 ? b.Substring(0, e) : b; }
string npcSrc = Src("SideQuest1", "TaxpayerNpcInteractable.cs");
string hudSrc = Src("SideQuest1", "TaxpayerProfileCollectionHUD.cs");
string guideSrc = Src("SideQuest1", "TutorialNpcController.cs");
string cardSrc = Src("SideQuest1", "ProfileDragCard.cs");
string boardSrc = Src("SideQuest1", "CorkboardSideQuestManager.cs");
string[] quietBodies =
{
    Between(npcSrc, "public void RestoreHandover", "public bool IsInConversation"),
    Between(hudSrc, "public void RestoreCollected", "public bool TryGetCollectionScreenPoint"),
    Between(guideSrc, "public void RestoreProgress", "private void Awake"),
    Between(cardSrc, "public void RestorePinned", "private void UpdateSway"),
    Between(boardSrc, "private void RestorePins", "private bool TryGetTrayView")
};
string[] quietNames = { "taxpayer NPC", "profile HUD", "guide", "profile card", "corkboard" };
string[] quietFailures = quietBodies.Select((b, i) =>
{
    var issues = new List<string>();
    if (b.Length == 0) issues.Add("empty");
    if (b.Contains("Raise")) issues.Add("Raise");
    if (b.Contains("PlayEffect")) issues.Add("PlayEffect");
    if (b.Contains("AwardConversation")) issues.Add("AwardConversation");
    if (b.Contains("AddSideQuest")) issues.Add("AddSideQuest");
    if (b.Contains("TryStartDialogue")) issues.Add("TryStartDialogue");
    if (b.Contains("HandleCardDropped")) issues.Add("HandleCardDropped");
    if (b.Contains("CardPlaced")) issues.Add("CardPlaced");
    return issues.Count == 0 ? null : quietNames[i] + ": " + string.Join("/", issues);
}).Where(name => name != null).ToArray();
Check(quietFailures.Length == 0, "owner restore methods play no sound, dialogue, award, drop or event: " + string.Join(", ", quietFailures));
Check(Between(cardSrc, "public void RestorePinned", "private void UpdateSway").Contains("pendingSlot = null;") &&
      Between(cardSrc, "public void RestorePinned", "private void UpdateSway").Contains("state = MotionState.Placed;"),
    "a restored card is pinned (no held drag or pending drop)");
Check(Between(boardSrc, "private void Open(", "private void OnCameraArrived").Contains("revealHeld = saved == null && SideQuestSplash.Active;") &&
      Body(boardSrc, "bool animateFromHud").Contains("resuming == null"), "a resumed board skips the title card and the fly-in from the HUD");
string ledgerSrc = Src("RewardsSystem", "LevelScoreLedger.cs");
Check(Index(Body(ledgerSrc, "private ScoreEntry Add("), "if (Restoring) return entry;") < Index(Body(ledgerSrc, "private ScoreEntry Add("), "EntryAdded?.Invoke(entry)"),
    "quietly restored score entries still count but are never presented again");
Check(Src("UI", "HUD", "HudFeedback.cs").Contains("if (!ready || objectives.Restoring)"), "restored objective steps are not announced again");
string mgr4 = Src("RewardsSystem", "PlayerProgressManager.cs");
string saveL1 = Between(mgr4, "public bool SaveLevelOneCheckpoint", "public Level1FieldCheckpoint GetLevelOneCheckpoint");
Check(Index(saveL1, "if (Save()) return true;") > 0 && Index(saveL1, "progress.campaign.levelOne = previous;") > Index(saveL1, "if (Save()) return true;") &&
      Between(mgr4, "public bool UsesLevelOneCheckpoint", "public bool SaveLevelOneCheckpoint").Contains("CampaignRun.AllowsStoryCheckpoint(CurrentRunMode)"),
    "a failed write keeps the previous checkpoint in memory and on disk; only story runs of a campaign save it");
string handoverRules4 = Src("RewardsSystem", "CaseResultRules.cs").Replace("\r\n", "\n");
string completeFollow4 = Body(handoverRules4, "public static CampaignRewards.Change CompleteFollowThrough(");
string acknowledge4 = Between(mgr4, "public bool AcknowledgeLevelOneEndFlow()", "private CampaignRewards.Change LastLedger(");
Check(handoverRules4.Contains("candidate.campaign.levelOne.handoverPhase = Level1FieldCheckpoint.HandoverDone;") &&
      handoverRules4.Contains("candidate.campaign.levelOneEndFlowPending = true;") &&
      Index(completeFollow4, "if (candidate.campaign.levelOne == null) candidate.campaign.levelOne = new Level1FieldCheckpoint();") <
          Index(completeFollow4, "candidate.campaign.levelOne.handoverPhase = Level1FieldCheckpoint.HandoverDone;") &&
      acknowledge4.Contains("PlayerProgressData candidate = CloneProgress();") &&
      Index(acknowledge4, "candidate.campaign.levelOneEndFlowPending = false;") < Index(acknowledge4, "TryWriteCandidate(candidate)") &&
      Index(acknowledge4, "candidate.campaign.levelOne = new Level1FieldCheckpoint();") < Index(acknowledge4, "TryWriteCandidate(candidate)") &&
      acknowledge4.Contains("return TryWriteCandidate(candidate);"),
    "follow-through saves Done and pending end flow; Continue clears both only through one candidate write");
string writeCandidate4 = Between(mgr4, "private bool TryWriteCandidate(", "public bool UsesLevelOneCheckpoint");
Check(acknowledge4.Contains("candidate.campaign.levelOneEndFlowPending = false;") &&
      acknowledge4.Contains("candidate.campaign.levelOne = new Level1FieldCheckpoint();") &&
      !acknowledge4.Contains("progress.campaign.levelOneEndFlowPending =") &&
      !acknowledge4.Contains("progress.campaign.levelOne =") &&
      Index(writeCandidate4, "store.Write(Encode(candidate));") < Index(writeCandidate4, "progress = candidate;") &&
      Between(writeCandidate4, "catch (Exception e)", "progress = candidate;").Contains("return false;"),
    "failed Continue acknowledgement leaves the live saved Done phase and pending marker intact for retry");
Check(cpSrc.Contains("private void OnApplicationPause(bool paused)") && cpSrc.Contains("private void OnApplicationQuit() => SaveNow();") &&
      Src("Settings", "InGameSettings.cs").Contains("!Level1FieldCheckpointer.Active.PrepareSettingsExit(replay)) return false;") &&
      Between(cpSrc, "private bool SaveCurrent", "private Level1FieldCheckpoint Capture").Contains("nextSave = Time.unscaledTime + RetryAfterFailure;"),
    "pause, quit and menu exit save; a failed save is retried and leaving waits for success");

// ---------- resumed runs do not replay finished guide lessons (user report 2026-10-23) ----------
string guideDir = Src("UI", "Guide", "GuideDirector.cs");
string loadFinished = Between(guideDir, "private void LoadFinished()", "private static void RevealArrows");
Check(Index(loadFinished, "ResumedLessons(levelId)") > 0 && Index(loadFinished, "ResumedLessons(levelId)") < Index(loadFinished, "bool willReveal") &&
      loadFinished.Contains("Fire(GuideTriggers.LessonKey(id));") && Index(guideDir, "director.LoadFinished();") < Index(guideDir, "public static bool ResolveTouch"),
    "the guide takes the saved run's finished lessons when it is created (before its first lesson, so no welcome card or tour replays) and their lesson prerequisites count again");
Check(cpSrc.Contains("GuideDirector.ResumedLessons = ResumedLessons;") &&
      Between(cpSrc, "private static IEnumerable<string> ResumedLessons", "private static void HandleSceneLoaded").Contains("levelId != OfficeLevelCatalog.Level1Id || !InUse") &&
      cpSrc.Contains("GuideDirector.LessonFinished += HandleLessonFinished;") && cpSrc.Contains("checkpoint.guideLessonsDone.AddRange(director.FinishedLessons());"),
    "only a Level 1 story run supplies lessons; every finished lesson is saved with the field checkpoint");

// =====================================================================
// T05: Level 1 case and clock in the same checkpoint
// =====================================================================

Level1CaseRecord CaseRec(string caseId, string fingerprint)
{
    var c = new Level1CaseRecord
    {
        caseId = caseId, caseFingerprint = fingerprint, receptionistDone = true, folder = Level1CaseRecord.FolderStored,
        caseAssessment = "ReadyForFiling", assessmentStamped = true, computationStatus = "Computed", hasPrintedReturn = true,
        encodedFormJson = "{\"tin\":\"123\"}", taxCodeBookOpened = true, auditAttempts = 2, auditMistakes = 3, failedAudits = 2,
        lastFailedAnswers = "ResidentAlien|SelfEmployed|BusinessIncome"
    };
    c.fields.Add(new Level1CaseField { key = "ResidencyStatus", value = "ResidentCitizen" });
    c.fields.Add(new Level1CaseField { key = "GrossIncome", value = "412345.75" });
    c.reviewedDocuments.Add("Valid Government ID");
    return c;
}
Level1ClockRecord Clock() => new Level1ClockRecord
{
    isSet = true, day = 2, gameSeconds = 29520.5, activeSeconds = 811.25f, sideQuestSeconds = 402.5f, allowance = 330f, hasDeadline = true,
    announced = true, deadlineSeconds = 31440, deadlineFirstDay = 30960, submittedAt = -1, submittedDay = 0, onTimeSaid = false,
    missedOnce = true, extensionSeconds = 180
};

// ---------- round trip of every case phase and the clock through the signed save ----------
var casePhases = new (string name, Action<Level1CaseRecord> shape)[]
{
    ("before the receptionist (clock running)", c => { c.receptionistDone = false; c.folder = "desk"; c.fields.Clear(); c.auditAttempts = c.auditMistakes = c.failedAudits = 0; c.lastFailedAnswers = null; }),
    ("case received, document review", c => { c.folder = "desk"; c.fields.Clear(); c.auditAttempts = c.auditMistakes = c.failedAudits = 0; c.lastFailedAnswers = null; }),
    ("after transfers, folder stored", c => { c.auditAttempts = c.auditMistakes = c.failedAudits = 0; c.lastFailedAnswers = null; }),
    ("after an audit return (retry guard kept)", c => { }),
};
string caseDir = Path.Combine(root, "Temp", "CampaignFoundationRegression", "t05-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(caseDir);
try
{
    foreach (var (name, shape) in casePhases)
    {
        var data = PlayerProgressData.CreateNewCampaign("t05", "");
        Level1FieldCheckpoint cp = Field(Level1FieldCheckpoint.PhaseRewarded, 4, 4, true);
        cp.caseRecord = CaseRec("case_marie_001", "0123456789abcdef");
        shape(cp.caseRecord);
        cp.clock = Clock();
        data.campaign.levelOne = cp;
        string expected = L1(cp);
        var store = new SaveFileStore(Path.Combine(caseDir, "p" + Math.Abs(name.GetHashCode()) + ".json"));
        store.Write(EncodeSave(data));
        PlayerProgressData loaded = DecodeSave(File.ReadAllText(store.FilePath));
        bool repaired = loaded.Normalize();
        Level1FieldCheckpoint back = loaded.campaign.levelOne;
        Check(!repaired && L1(back) == expected &&
              Level1FieldCheckpointRules.ValidateCase(back, "case_marie_001", "0123456789abcdef", new[] { "ResidencyStatus", "GrossIncome" }) == Level1RestoreOutcome.Restored,
            $"case + clock round trip through the signed save: {name}");
    }
}
finally { Directory.Delete(caseDir, true); }
Level1ClockRecord clockBack = Clone2(new Level1FieldCheckpoint { clock = Clock() }).clock;
Check(clockBack.day == 2 && clockBack.gameSeconds == 29520.5 && clockBack.deadlineSeconds == 31440 && clockBack.deadlineFirstDay == 30960 &&
      clockBack.extensionSeconds == 180 && clockBack.missedOnce && clockBack.announced && clockBack.activeSeconds == 811.25f,
    "the clock keeps day, time, both deadlines, missed day and the applied extension exactly (never recomputed)");

// ---------- repair rules ----------
var brokenCase = new Level1FieldCheckpoint { version = 1, caseRecord = CaseRec(" case_marie_001 ", "f") };
brokenCase.caseRecord.folder = "under the sofa";
brokenCase.caseRecord.fields.Add(new Level1CaseField { key = "ResidencyStatus", value = "NonResidentAlien" });
brokenCase.caseRecord.fields.Add(new Level1CaseField { key = "TaxDue", value = null });
brokenCase.caseRecord.auditAttempts = 1;
brokenCase.caseRecord.failedAudits = 3;
brokenCase.caseRecord.auditMistakes = -2;
brokenCase.Repair();
Check(brokenCase.caseRecord.caseId == "case_marie_001" && brokenCase.caseRecord.folder == "desk" && brokenCase.caseRecord.fields.Count == 2 &&
      brokenCase.caseRecord.fields[0].value == "ResidentCitizen" && brokenCase.caseRecord.failedAudits == 1 && brokenCase.caseRecord.auditMistakes == 0,
    "case repair: unknown folder place -> desk, one value per key (first kept), failed audits within attempts, no negative counters");
var noFails = new Level1FieldCheckpoint { version = 1, caseRecord = CaseRec("case_marie_001", "f") };
noFails.caseRecord.auditAttempts = 0;
noFails.Repair();
Check(noFails.caseRecord.failedAudits == 0 && noFails.caseRecord.lastFailedAnswers == null, "no failed audit, no retry guard");
var brokenClock = new Level1FieldCheckpoint { version = 1, clock = new Level1ClockRecord
{
    isSet = true, day = 0, gameSeconds = double.NaN, activeSeconds = -4f, hasDeadline = false, announced = true, deadlineSeconds = 500,
    submittedDay = 5, submittedAt = 100, extensionSeconds = 999
} };
brokenClock.Repair();
Level1ClockRecord rc = brokenClock.clock;
Check(rc.day == 1 && rc.gameSeconds == 0 && rc.activeSeconds == 0f && !rc.announced && rc.deadlineSeconds == 0 && rc.submittedDay == 0 && rc.submittedAt == -1 &&
      rc.extensionSeconds == Level1ClockRecord.MaxExtensionSeconds,
    "clock repair: day >= 1, finite times, no announcement without a deadline, hand-in day within the days played, one extension at most");
var keepDeadline = new Level1FieldCheckpoint { version = 1, clock = Clock() };
string clockBefore = JsonSerializer.Serialize(keepDeadline.clock, json);
Check(!keepDeadline.Repair() && JsonSerializer.Serialize(keepDeadline.clock, json) == clockBefore, "a valid clock is never changed by repair (deadline cannot move)");

// ---------- case identity and gating ----------
var otherCase = new Level1FieldCheckpoint { version = 1, boardPhase = Level1FieldCheckpoint.PhaseRewarded, caseRecord = CaseRec("case_other", "0123456789abcdef") };
Check(Level1FieldCheckpointRules.ValidateCase(otherCase, "case_marie_001", "0123456789abcdef", null) == Level1RestoreOutcome.Reset &&
      otherCase.setAsideCase.caseId == "case_other" && !otherCase.caseRecord.IsSet, "another case's snapshot is set aside, never applied");
var oldRevision = new Level1FieldCheckpoint { version = 1, boardPhase = Level1FieldCheckpoint.PhaseSolved, caseRecord = CaseRec("case_marie_001", "aaaaaaaaaaaaaaaa") };
Check(Level1FieldCheckpointRules.ValidateCase(oldRevision, "case_marie_001", "0123456789abcdef", null) == Level1RestoreOutcome.Reset &&
      oldRevision.setAsideCase.caseFingerprint == "aaaaaaaaaaaaaaaa", "an obsolete revision is set aside (kept for recovery)");
var officeShut = new Level1FieldCheckpoint { version = 1, boardPhase = Level1FieldCheckpoint.PhaseActive, caseRecord = CaseRec("case_marie_001", "0123456789abcdef") };
Check(Level1FieldCheckpointRules.ValidateCase(officeShut, "case_marie_001", "0123456789abcdef", null) == Level1RestoreOutcome.Repaired && !officeShut.caseRecord.IsSet,
    "case work before the office opened cannot exist and is dropped");
var strayKey = new Level1FieldCheckpoint { version = 1, boardPhase = Level1FieldCheckpoint.PhaseRewarded, caseRecord = CaseRec("case_marie_001", "0123456789abcdef") };
strayKey.caseRecord.fields.Add(new Level1CaseField { key = "ShoeSize", value = "42" });
Check(Level1FieldCheckpointRules.ValidateCase(strayKey, "case_marie_001", "0123456789abcdef", new[] { "ResidencyStatus", "GrossIncome" }) == Level1RestoreOutcome.Repaired &&
      strayKey.caseRecord.fields.Count == 2, "unknown folder keys are dropped");
Check(Level1FieldCheckpointRules.ValidateCase(new Level1FieldCheckpoint { version = 1 }, "case_marie_001", "x", null) == Level1RestoreOutcome.None,
    "no case snapshot means the case starts normally");
Check(Level1FieldCheckpointRules.Fingerprint(new[] { "a", "b" }) == Level1FieldCheckpointRules.Fingerprint(new[] { "a", "b" }) &&
      Level1FieldCheckpointRules.Fingerprint(new[] { "a", "b" }) != Level1FieldCheckpointRules.Fingerprint(new[] { "ab", "" }) &&
      Level1FieldCheckpointRules.Fingerprint(new[] { "a" }).Length == 16, "fingerprints are deterministic, separator-safe, 16 hex characters");

var t04Only = new Level1FieldCheckpoint { version = 1 };
string t04Json;
using (JsonDocument doc = JsonDocument.Parse(L1(t04Only)))
{
    var trimmedFields = doc.RootElement.EnumerateObject().Where(p => p.Name != "caseRecord" && p.Name != "setAsideCase" && p.Name != "clock")
        .ToDictionary(p => p.Name, p => (object)p.Value);
    t04Json = JsonSerializer.Serialize(trimmedFields);
}
var t04Back = JsonSerializer.Deserialize<Level1FieldCheckpoint>(t04Json, json);
t04Back.Repair();
Check(t04Back.caseRecord != null && !t04Back.caseRecord.IsSet && t04Back.clock != null && !t04Back.clock.isSet,
    "a T04 checkpoint without case or clock loads with neither");
var caseFieldNames = typeof(Level1CaseRecord).GetFields().Where(f => !f.IsLiteral).Select(f => f.Name)
    .Concat(typeof(Level1CaseField).GetFields().Select(f => f.Name)).ToList();
Check(!caseFieldNames.Any(n => n.IndexOf("answerKey", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("correct", StringComparison.OrdinalIgnoreCase) >= 0 ||
      n.IndexOf("position", StringComparison.OrdinalIgnoreCase) >= 0), "the case record has no answer key, correctness or position field (lastFailedAnswers is the player's own failed attempt)");

// ---------- wiring (source contracts; restore is a Unity runtime path) ----------
string cp5 = Src("SideQuest1", "Level1FieldCheckpointer.cs");
string restoreCase = Between(cp5, "private void RestoreCase(", "/// <summary>The level score gets back");
string approvedStateGuard = Between(restoreCase, "if (record.receptionistDone || approvedResultRecorded)", "AuditorSubmissionTray tray =");
Check(!restoreCase.Contains("Raise") && !restoreCase.Contains("TryReceiveValue") && !restoreCase.Contains("BeginFinalAudit") &&
      !restoreCase.Contains("RecordAuditAttempt") && !restoreCase.Contains("StoreFolderFromExternalSource") && !restoreCase.Contains("PlayEffect") &&
      restoreCase.Contains("RestoreAuditCounters(") && restoreCase.Contains("RestoreRetryGuard(") && restoreCase.Contains("tray.Folder.RestoreStored()") &&
      Index(approvedStateGuard, "!approvedResultRecorded && machine != null") < Index(approvedStateGuard, "machine.ChangeState(new ReceiveCaseState())"),
    "case restore replays no transfer, hand-in, audit, charge, sound or event; saved approved results never return to Receive Case");
string tryRestore5 = Between(cp5, "private void TryRestore()", "private void RestoreCase(");
Check(tryRestore5.Contains("RestoreTriggers(") && tryRestore5.Contains("RestoreClock(saved.clock)") &&
      tryRestore5.Contains("RestoreApprovedResult()") && tryRestore5.Contains("RestoreCase(saved, caseResultRecorded)") &&
      tryRestore5.Contains("new LevelCompleteState(restoredApprovedResult: true)") && tryRestore5.Contains("ClientCopyHandover.Instance.Restore(") &&
      Index(tryRestore5, "RestoreTriggers(") < Index(tryRestore5, "RestoreClock(") &&
      Index(tryRestore5, "RestoreClock(saved.clock)") < Index(tryRestore5, "RestoreApprovedResult()") &&
      Index(tryRestore5, "RestoreApprovedResult()") < Index(tryRestore5, "RestoreCase(saved, caseResultRecorded)") &&
      Index(tryRestore5, "RestoreCase(saved, caseResultRecorded)") < Index(tryRestore5, "new LevelCompleteState(restoredApprovedResult: true)") &&
      Index(tryRestore5, "new LevelCompleteState(restoredApprovedResult: true)") < Index(tryRestore5, "ClientCopyHandover.Instance.Restore("),
    "restore quietly reapplies field/objectives and saved clock, then case data, then restores the approved result and copy phase");
string hours = Src("UI", "HUD", "OfficeHours.cs");
string restoreClock = Between(hours, "public void RestoreClock(", "// DEADLINE");
Check(!restoreClock.Contains("OfficeHoursMath.Deadline") && !restoreClock.Contains("AllowanceSeconds") && !restoreClock.Contains("GuideVoice") &&
      !restoreClock.Contains("StartCoroutine") && restoreClock.Contains("DeadlineSeconds = saved.deadlineSeconds;"),
    "the clock restores saved deadlines without recomputing them or replaying warnings / next morning");
Check(cp5.Contains("GameplayEvents.OnNotesUnlockRequested += SaveNow;") && cp5.Contains("GameplayEvents.OnCaseFolderStored += SaveNow;") &&
      cp5.Contains("GameplayEvents.OnCaseFolderReturned += SaveNow;") && cp5.Contains("watchedTransfers.OnTransferSucceeded += HandleTransfer;") &&
      cp5.Contains("watchedClock.Changed += SaveNow;") && cp5.Contains("watchedMachine.OnStateChanged += HandleStateChanged;"),
    "case milestones (hand-over, folder stored/returned, value transfer, clock announcement / new day, state change) save");
Check(Between(cp5, "private static void CaptureCase(", "// RESUME").Contains("if (!LevelGate.SideQuestDone"), "case work is only captured once the office is open");
Check(Src("CaseFolder", "FolderToHUDIcon.cs").Contains("if (!restoring) GameplayEvents.RaiseCaseFolderStored();") &&
      Src("CaseFolder", "FolderToHUDIcon.cs").Contains("if (!restoring && AudioManager.Instance != null)"), "a restored folder is stored without sound or event");

// ---------- resume fixes (user report 2026-10-23: folder icon showed 2, profiles 0/1) ----------
string folderIcon = Src("CaseFolder", "FolderToHUDIcon.cs");
Check(!folderIcon.Contains("private static int storedFolderCount = 0;") && folderIcon.Contains("HashSet<FolderToHUDIcon> storedFolders") &&
      folderIcon.Contains("private void OnDestroy() => storedFolders.Remove(this);") && folderIcon.Contains("if (mode == UnityEngine.SceneManagement.LoadSceneMode.Single) storedFolders.Clear();"),
    "the folder icon count is the live stored folders of this scene, so leaving the level can never leave a ghost count behind");
Check(Between(folderIcon, "private void StoreFolder(bool restoring)", "private void OnDestroy").Contains("storedFolders.Add(this);") &&
      Between(folderIcon, "public void ResetToUnstored()", "DebugLog(\"Folder reset").Contains("storedFolders.Remove(this);"),
    "storing adds the folder once and a retry/reset removes it");
Check(Between(cp5, "private static void CaptureCase(", "// RESUME").Contains("FindObjectsByType<FolderToHUDIcon>(FindObjectsInactive.Include)"),
    "either folder component (closed desk folder or open folder) counts as stored when saving");
string npcFix = Between(Src("SideQuest1", "TaxpayerNpcInteractable.cs"), "public void RestoreHandover", "public void OnFocus");
Check(Index(npcFix, "profilePickup.Initialize(data);") > 0 && Index(npcFix, "profilePickup.Initialize(data);") < Index(npcFix, "if (!profileCollected) profilePickup.gameObject.SetActive(true);"),
    "a restored NPC's profile paper always knows its profile (Marie's 1/1 counter and viewer) and only lies in the world while uncollected");

string restoreStored = Between(Src("CaseFolder", "FolderToHUDIcon.cs"), "public void RestoreStored()", "private void StoreFolder(bool restoring)");
Check(Index(restoreStored, "StoreFolder(restoring: true);") > 0 && Index(restoreStored, "openFolderReturnToDesk.HideClosedFolderForRestore();") > Index(restoreStored, "StoreFolder(restoring: true);") &&
      Between(Src("CaseFolder", "OpenFolderReturnToDesk.cs"), "public void HideClosedFolderForRestore()", "public void ForceReturnToDesk()").Contains("originalClosedFolder.SetActive(false);"),
    "a folder restored into the icon also hides the closed desk copy (never on the desk and in the icon at once)");

// ---------- case folder fixes (user report 2026-10-23: folder on the desk while in the icon, open folder cut by the desk) ----------
string folderView = Src("UI", "HUD", "CaseFolder3DView.cs");
Check(Index(Body(folderView, "public bool Open("), "layer.AlwaysComposite = true;") > 0 &&
      Between(folderView, "private void Restore()", "// UPDATE").Contains("layer.AlwaysComposite = layerWasAlwaysComposite;"),
    "the folder floating in front of the camera is composited over the world (never cut by the desk) and the layer setting is restored on close");
string stackSync = Src("CaseFolder", "DeskFolderStackSync.cs");
Check(stackSync.Contains("bool onDesk = caseFolder.activeInHierarchy;") && stackSync.Contains("deskFolderVisual.SetActive(onDesk)") &&
      stackSync.Contains("stack.Find(\"DeskFolder/Visual\")") && !stackSync.Contains("Collider"),
    "the desk drop-target folder model shows only while the real folder is on the desk; its drop collider is untouched");
string trayFix = Src("Data", "AuditorSubmissionTray.cs");
string returnStored = Between(trayFix, "private void ReturnStoredFolders()", "private void CheckIfSubmissionComplete()");
Check(returnStored.Contains("FindObjectsByType<FolderToHUDIcon>(FindObjectsInactive.Include)") && returnStored.Contains("folder.IsStoredOrViewing") &&
      Between(trayFix, "public void ResetForRetryWithoutNewCase()", "Debug.Log").Contains("ReturnStoredFolders();") &&
      Between(trayFix, "public void ResetSubmission()", "Debug.Log").Contains("ReturnStoredFolders();"),
    "an audit return or new case brings back whichever folder component (closed or open) holds the folder");

// =====================================================================
// T06: classification draft, then the first check when the board is full
// =====================================================================

// ---------- pure rules ----------
Check(ClassificationCheck.TierFor(4) == ClassificationCheck.Gold && ClassificationCheck.TierFor(3) == ClassificationCheck.Silver &&
      ClassificationCheck.TierFor(2) == ClassificationCheck.Bronze && ClassificationCheck.TierFor(1) == ClassificationCheck.Bronze &&
      ClassificationCheck.TierFor(0) == ClassificationCheck.Bronze,
    "first-check tier: Gold 4/4, Silver 3/4, Bronze 0-2/4");
string scoringSource = Src("RewardsSystem", "LevelScoring.cs");
Check(ClassificationCheck.Gold == 3 && ClassificationCheck.Silver == 2 && ClassificationCheck.Bronze == 1 &&
      scoringSource.Contains("Bronze = 1,") && scoringSource.Contains("Silver = 2,") && scoringSource.Contains("Gold = 3"),
    "tier values match BadgeTier");
Check(ClassificationCheck.SettledExp(50, 5, 0, 0) == 50 && ClassificationCheck.SettledExp(50, 5, 0, 1) == 45 &&
      ClassificationCheck.SettledExp(50, 5, 0, 4) == 30 && ClassificationCheck.SettledExp(50, 20, 15, 4) == 15 &&
      ClassificationCheck.SettledExp(10, 20, -5, 4) == 0 && ClassificationCheck.SettledExp(50, 5, 0, -2) == 50,
    "settled XP: questExp minus expPerWrong per wrong, never below minExp or zero");
Check(!ClassificationCheck.CanCheck(0) && !ClassificationCheck.CanCheck(3) && ClassificationCheck.CanCheck(4),
    "a check needs all four areas filled");
Check(ClassificationCheck.RemainingMessage(3) == "Place 1 more card and the board checks itself." &&
      ClassificationCheck.RemainingMessage(0) == "Place 4 more cards and the board checks itself." &&
      ClassificationCheck.RemainingMessage(-1) == "Place 4 more cards and the board checks itself.",
    "the remaining-cards line only says how many cards remain");
foreach (string text in new[] { ClassificationCheck.DraftMessage(2), ClassificationCheck.DraftMessage(4), ClassificationCheck.RemainingMessage(2), ClassificationCheck.LastCardPrompt })
    Check(!text.Contains("correct", StringComparison.OrdinalIgnoreCase) && !text.Contains("wrong", StringComparison.OrdinalIgnoreCase) &&
          !text.Contains("right", StringComparison.OrdinalIgnoreCase),
        $"draft wording reveals nothing: \"{text}\"");

// ---------- checkpoint fields: round trip and repair ----------
Level1FieldCheckpoint Checked(int correct)
{
    var c = Field(Level1FieldCheckpoint.PhaseActive, 4, 4, true);
    c.placementAttempts = 6; c.wrongPlacements = 4 - correct;
    c.checkSubmitted = true; c.checkCorrectCount = correct; c.checkSettledExp = 50 - 5 * (4 - correct); c.checkCount = 1;
    foreach (TaxpayerProfile p in TaxpayerProfileDatabase.All.Take(4 - correct)) c.checkWrongProfileIds.Add(p.ProfileId);
    return c;
}
var checkedThree = Checked(3);
string checkedBefore = L1(checkedThree);
Check(Level1FieldCheckpointRules.Validate(checkedThree, profileMap, areaIds, 4) == Level1RestoreOutcome.Restored && L1(checkedThree) == checkedBefore,
    "a valid first check (3/4) is restored unchanged");
var checkBack = JsonSerializer.Deserialize<Level1FieldCheckpoint>(L1(checkedThree), json);
Check(checkBack.checkSubmitted && checkBack.checkCorrectCount == 3 && checkBack.checkSettledExp == 45 && checkBack.checkWrongProfileIds.Count == 1,
    "check fields survive serialization");

var badWrong = Checked(2); badWrong.wrongPlacements = 7; badWrong.checkWrongProfileIds.Add("profile_unknown");
Check(Level1FieldCheckpointRules.Validate(badWrong, profileMap, areaIds, 4) == Level1RestoreOutcome.Repaired &&
      badWrong.wrongPlacements == 2 && badWrong.checkWrongProfileIds.Count == 2 && !badWrong.checkWrongProfileIds.Contains("profile_unknown"),
    "the wrong count follows the frozen correct count; unknown wrong ids are dropped");
var notSubmitted = Field(Level1FieldCheckpoint.PhaseActive, 4, 4, true);
notSubmitted.checkCorrectCount = 2; notSubmitted.checkSettledExp = 40; notSubmitted.checkWrongProfileIds.Add("profile_marie");
notSubmitted.Repair();
Check(notSubmitted.checkCorrectCount == -1 && notSubmitted.checkSettledExp == -1 && notSubmitted.checkWrongProfileIds.Count == 0,
    "check values without a submitted check are cleared");
var overCount = Checked(4); overCount.checkCorrectCount = 9; overCount.Repair();
var submittedNoCount = Checked(4); submittedNoCount.checkCorrectCount = -1; submittedNoCount.Repair();
Check(overCount.checkCorrectCount == 4 && !submittedNoCount.checkSubmitted, "correct count clamps to 4; a submitted flag without a count is dropped");
var closedBoard = Checked(1); closedBoard.boardPhase = Level1FieldCheckpoint.PhaseNone;
Level1FieldCheckpointRules.Validate(closedBoard, profileMap, areaIds, 4);
Check(!closedBoard.checkSubmitted && closedBoard.checkCorrectCount == -1 && closedBoard.checkWrongProfileIds.Count == 0 && closedBoard.wrongPlacements == 0,
    "a board that was never opened cannot carry a check");

// ---------- wiring (source contracts; the board itself is a Unity runtime path) ----------
int Occurrences(string source, string text) { int n = 0; for (int i = Index(source, text); i >= 0; i = source.IndexOf(text, i + text.Length, StringComparison.Ordinal)) n++; return n; }
string board = Src("SideQuest1", "CorkboardSideQuestManager.cs").Replace("\r\n", "\n");
string drop = Between(board, "public bool HandleCardDropped(", "public void NotifyCardRemovedFromSlot()");
Check(!drop.Contains("PlayEffect") && !drop.Contains("GuideVoice") && !drop.Contains("CardPlaced") && !drop.Contains("LockedUntil") &&
      !drop.Contains("wrongPlacements") && !drop.Contains("CheckForCompletion") && !drop.Contains("IsSolved") && drop.Contains("HandleDraftChanged();"),
    "a drop is a neutral draft: no correctness sound, voice, reaction, lock, score or completion");
string card = Src("SideQuest1", "ProfileDragCard.cs");
string cardDrop = Between(card, "// T06: a drop is only a draft", "manager.HandleCardDropped(this, target);");
Check(cardDrop.Contains("SetColor(neutralColor);") && !cardDrop.Contains("correctColor") && !cardDrop.Contains("wrongColor") &&
      Between(card, "public void RestorePinned(", "ShowPinMarker();").Contains("SetColor(neutralColor);"),
    "a dropped or restored card stays neutral until a check marks it");
string request = Between(board, "public void RequestCheck()", "private void MarkPlacedCards(");
Check(request.Contains("if (!ClassificationCheck.CanCheck(placed))") && request.Contains("if (!checkSubmitted)") &&
      Index(request, "frozenCorrect = correct;") < Index(request, "MarkPlacedCards(raiseEvents: true);") &&
      !Between(request, "MarkPlacedCards(raiseEvents: true);", "}\n    /// <summary>Marks").Contains("frozenCorrect ="),
    "the first complete check freezes the count; later checks never change it");
// Auto-check (user decision 2026-10-24): the full board checks itself after a short beat; no stamp.
string draftChanged = Between(board, "private void HandleDraftChanged()", "/// <summary>Pins each saved card");
string autoCheck = Between(board, "private void UpdateAutoCheck()", "/// <summary>Which card is in which area");
Check(draftChanged.Contains("autoCheckAt = ClassificationCheck.CanCheck(placed) ? Time.unscaledTime + AutoCheckDelay : -1f;") &&
      !draftChanged.Contains("RequestCheck") && autoCheck.Contains("RequestCheck();") &&
      autoCheck.Contains("if (!CanInteract || !ClassificationCheck.CanCheck(CountOccupied())) { autoCheckAt = -1f; return; }") &&
      autoCheck.Contains("if (card != null && card.IsBeingMoved) return;") && Occurrences(board, "RequestCheck();") == 1,
    "the board checks itself only when all four areas hold a card, after the pin lands (a card taken off cancels it)");
Check(autoCheck.Contains("if (DraftSignature() == lastCheckedDraft) return;") && request.Contains("lastCheckedDraft = DraftSignature();"),
    "a re-check needs a changed board (picking a card up and putting it back does not re-check)");
Check(!File.Exists(Path.Combine(root, "Assets", "Scripts", "SideQuest1", "CheckBoardTarget.cs")) && !board.Contains("CheckBoardTarget") &&
      !board.Contains("Stamp"), "the CHECK BOARD stamp is removed");
Check(Occurrences(board, "ClassificationCheck.LastCardPrompt") == 1 &&
      draftChanged.Contains("if (placed == ClassificationCheck.Profiles - 1 && !checkSubmitted && !lastCardWarned)"),
    "the guide warns once at 3/4, before the first check, that the next card starts it");
string guideTargets = Src("UI", "Guide", "GuideTargets.cs");
string useful = Between(guideTargets, "public static bool IsUseful(", "private Hit Best(");
Check(!useful.Contains("IsSolved") && !useful.Contains("IsCorrectlyPlaced") && useful.Contains("card.NeedsAttention"),
    "guide hints never point by correctness during drafting");
Check(Src("SideQuest1", "TutorialNpcController.cs").Contains("corkboardSideQuest.CheckSubmitted ? corkboardSideQuest.FrozenTier : (BadgeTier?)null") &&
      Src("SideQuest1", "Level1FieldCheckpointer.cs").Contains("saved.checkSubmitted ? (BadgeTier)ClassificationCheck.TierFor(saved.checkCorrectCount) : (BadgeTier?)null"),
    "the reward and its quiet restore use the frozen first-check tier");
string checkpointer = Src("SideQuest1", "Level1FieldCheckpointer.cs");
Check(checkpointer.Contains("CorkboardSideQuestManager.DraftChanged += SaveNow;") && checkpointer.Contains("CorkboardSideQuestManager.BoardChecked += HandleBoardChecked;") &&
      !checkpointer.Contains("CardPlaced"),
    "drafts and checks are saved; verdict events are not needed to save");
string capture = Between(board, "public void CaptureState(", "into.placements.Clear();");
Check(capture.Contains("into.checkSubmitted = checkSubmitted;") && capture.Contains("into.checkCorrectCount") && capture.Contains("into.checkSettledExp") &&
      capture.Contains("into.checkWrongProfileIds"),
    "the board saves the frozen check");
Check(Between(board, "if (checkSubmitted)\n            {\n                // The check already happened", "return;").Contains("MarkPlacedCards(raiseEvents: false);"),
    "a resumed check shows its marks quietly (no sound, reaction or event)");
Check(board.Contains("slot.OccupyingCard.transform != card && slot.OccupyingCard.IsLockedCorrect") && card.Contains("if (IsLocked || lockedCorrect) return;"),
    "cards a check marked right stay pinned and their area takes no other card");
// ---------- hint ladder and layout (user report 2026-10-23) ----------
Check(!ClassificationCheck.RevealsAnswers(1) && !ClassificationCheck.RevealsAnswers(2) && ClassificationCheck.RevealsAnswers(3) && ClassificationCheck.RevealsAnswers(5),
    "checks 1-2 give clues; answers from check 3");
string ladder = Body(request, "// Hint ladder");
Check(ladder.Contains("ClassificationCheck.RevealsAnswers(checkCount)") && ladder.Contains("reveal ? AnswerLine(card) : ClueLine(card)") &&
      Index(request, "checkCount++;") > 0 && Index(request, "checkCount++;") < Index(request, "// Hint ladder"),
    "every complete check is counted and picks clue or answer by its number");
string clue = Between(board, "private static string ClueLine(", "private static string AnswerLine(");
Check(clue.Contains("CorkboardTeaching.ReactionFor(card.Profile, slot.ExpectedClassification)") && !clue.Contains("CorrectClassification") && !clue.Contains("EnumDisplayFormatter"),
    "a clue names the missed question for the card's current corner, never the right corner");
string correction = Between(board, "private string CorrectionText(", "private static string ClueLine(");
Check(Index(correction, "if (reveal)") > 0 && Body(correction, "if (reveal)").IndexOf("CorrectClassification", StringComparison.Ordinal) <
      Index(Body(correction, "if (reveal)"), "string names ="),
    "the board line lists right corners only in the reveal branch");
Check(capture.Contains("into.checkCount"), "the check count is saved");
var counted = Checked(2); counted.checkCount = 0; counted.Repair();
var uncounted = Field(Level1FieldCheckpoint.PhaseActive, 4, 4, true); uncounted.checkCount = 3; uncounted.Repair();
Check(counted.checkCount == 1 && uncounted.checkCount == 0, "check count: at least 1 after a check, 0 before it");
// Tray (user request 2026-10-24): four cards centred vertically, equal padding, clear gaps.
float[] tray = ClassificationCheck.TrayCentres(4, 0.21f, 0.04f);
float topPad = 1f - (tray[0] + 0.105f), bottomPad = tray[3] - 0.105f;
Check(Math.Abs(topPad - bottomPad) < 0.0001f && topPad > 0f && Math.Abs((tray[0] - tray[1]) - 0.25f) < 0.0001f &&
      board.Contains("private const float TrayCardHeight = 0.21f;") && board.Contains("private const float TrayCardGap = 0.04f;") &&
      board.Contains("ClassificationCheck.TrayCentres(ClassificationCheck.Profiles, cardHeight, TrayCardGap)"),
    $"tray: equal top/bottom padding ({topPad:0.000}) and a 0.04 gap between the same-size cards");
Check(!board.Contains("Card Tray Rail"), "no panel is built behind the tray (user report 2026-10-23)");
// The HUD profiles icon is empty and locked while the cards are in the tray (user request 2026-10-24).
string worldPanel = Src("SideQuest1", "CollectedProfilesWorldPanel.cs");
Check(worldPanel.Contains("if (profilesOnBoard) return new List<TaxpayerProfile>();") && worldPanel.Contains("public int CollectedCount => profilesOnBoard ? 0 :") &&
      worldPanel.Contains("if (profilesOnBoard && !isOpen) return;") && worldPanel.Contains("if (onBoard && isOpen && !isClosing) BeginReturnToFolder();"),
    "while on the board the profiles icon shows 0, opens nothing and gives nothing to drag (open viewer closes)");
Check(Occurrences(board, "SetProfilesOnBoard(true)") == 1 && Index(board, "SetProfilesOnBoard(true)") > Index(board, "card.AnimateFromHud(collectionScreenPoint") &&
      Between(board, "private void FinishSideQuest()", "private void ClearRuntimeObjects()").Contains("SetProfilesOnBoard(false)") &&
      Body(board, "private void ClearRuntimeObjects()").Contains("SetProfilesOnBoard(false)") &&
      Between(board, "private void Open(", "resuming = saved;").Contains("hudCards.CloseCards();"),
    "the icon empties when the cards fly into the tray and refills when the board closes or the quest ends");
// Tray cards look like the HUD profile cards (user request 2026-10-24).
string skin = Src("UI", "HUD", "ProfileCardSkin.cs");
Check(skin.Contains("bool corkboard = face.GetComponentInParent<ProfileDragCard>(true) != null;") && skin.Contains("\"Drop Shadow\"") &&
      skin.Contains("corkboard ? CorkboardTextBoost : TextBoost, corkboard, !corkboard);"),
    "corkboard cards use the full HUD card (paperclip, header, Classification line) with a drop shadow and tilt; world papers unchanged");
Check(board.Contains("LayoutFeedbackUnderBoard();") && Between(board, "private void LayoutFeedbackUnderBoard()", "// =====").Contains("feedbackPanel.anchorMin = new Vector2(minX, 0f);"),
    "the feedback line is centred under the board, spanning its width");
string teaching = Src("SideQuest1", "CorkboardTeaching.cs") + File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Levels", "Level_01", "town.json")) + Src("SideQuest1", "SideQuestSplash.cs");
Check(!teaching.Contains("bounce back") && !teaching.Contains("CHECK BOARD") && teaching.Contains("The board checks itself"),
    "Arthur's intro and the title card describe draft-then-check");
using (JsonDocument guideDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Tutorials", "Level_01", "guide.json"))))
{
    string lesson = guideDoc.RootElement.GetProperty("lessons").EnumerateArray().First(l => l.GetProperty("id").GetString() == "corkboard").GetRawText();
    Check(!lesson.Contains("CheckBoardTarget") && lesson.Contains("the board checks itself") && !lesson.Contains("After every card") && !lesson.Contains("bounces back"),
        "the corkboard lesson teaches draft-then-check");
}

// =====================================================================
// T07: corrected work moves the story; scores stay mastery
// =====================================================================

// ---------- rules ----------
Level1FieldCheckpoint CorrectedBoard(int firstCheckCorrect)
{
    var c = Checked(firstCheckCorrect);
    c.boardPhase = Level1FieldCheckpoint.PhaseRewarded;
    c.caseRecord.caseId = "case_marie"; c.caseRecord.caseFingerprint = "fp-1";
    return c;
}
MasteryRun CaseRun(int exp) => new MasteryRun { LevelId = "Level_01", HasScore = true, Exp = exp, Hearts = 3, MaxExp = 150, MaxHearts = 12,
    Passed = false, Badges = new List<string> { "badge_sq1" }, BadgeTiers = new List<int> { 1 }, RunSeconds = 300 };
Check(CaseResultRules.ActionId("Level_01", " case_marie ") == "Level_01/case-result/case_marie", "stable scoped action id per chapter and case");
Check(CaseResultRules.CanRecord(true, true, true) && !CaseResultRules.CanRecord(false, true, true) &&
      !CaseResultRules.CanRecord(true, false, true) && !CaseResultRules.CanRecord(true, true, false),
    "a case result needs a story run, the corrected quest and the approved case");
var partialBoard = Checked(2);   // checked but not yet all right
Check(!CaseResultRules.QuestCorrected(partialBoard) && CaseResultRules.QuestCorrected(CorrectedBoard(2)),
    "the quest counts as corrected only once its board is solved (all four right)");
Check(CaseResultRules.TierFor(CorrectedBoard(4)) == 3 && CaseResultRules.TierFor(CorrectedBoard(3)) == 2 &&
      CaseResultRules.TierFor(CorrectedBoard(1)) == 1 && CaseResultRules.TierFor(Field(Level1FieldCheckpoint.PhaseNone, 0, 0, false)) == 0,
    "the case result carries the frozen first-check tier");

Level1CaseResult built = CaseResultRules.Build("Level_01", CorrectedBoard(3), null, null, true, 80, 150, 3, 12, 300f);
Check(built.recorded && built.questCorrected && built.caseApproved && built.firstCheckCorrect == 3 && built.tier == 2 &&
      built.caseId == "case_marie" && built.caseFingerprint == "fp-1" && built.actionId == "Level_01/case-result/case_marie" &&
      built.exp == 80 && built.nextStep == Level1CaseResult.NextFollowThrough,
    "the result records the approved case's identity, the frozen tier and the run's mastery numbers; next step: follow-through");

// ---------- once only ----------
PlayerProgressData CaseCampaign(int firstCheckCorrect)
{
    var d = PlayerProgressData.CreateNewCampaign("t07-" + firstCheckCorrect, "");
    d.campaign.levelOne = CorrectedBoard(firstCheckCorrect);
    return d;
}
var live = CaseCampaign(1);
string liveBefore = Snap(live);
var caseCandidate = Clone(live);
Check(CaseResultRules.Apply(caseCandidate, CaseResultRules.Build("Level_01", caseCandidate.campaign.levelOne, null, null, true, 40, 150, 2, 12, 200f), CaseRun(40)) ==
      CaseResultRules.Outcome.Applied && Snap(live) == liveBefore,
    "the result is applied to a candidate copy; the live save is untouched until the write succeeds (a failed write publishes nothing)");
Check(caseCandidate.campaign.levelOneCaseResult.recorded && caseCandidate.campaign.rewardClaims.Contains("Level_01/case-result/case_marie") &&
      caseCandidate.GetRecord("Level_01") != null && caseCandidate.GetRecord("Level_01").attempts == 1 && caseCandidate.totalExp == 40,
    "result, claim and mastery record land in the same write");
string afterFirst = Snap(caseCandidate);
var repeat = CaseResultRules.Apply(caseCandidate, CaseResultRules.Build("Level_01", caseCandidate.campaign.levelOne, null, null, true, 150, 150, 12, 12, 100f), CaseRun(150));
Check(repeat == CaseResultRules.Outcome.AlreadyRecorded && Snap(caseCandidate) == afterFirst,
    "a repeated input, reload or later run cannot record the result again or rewrite it (no second mastery apply)");
var claimOnly = CaseCampaign(4); claimOnly.campaign.rewardClaims.Add("Level_01/case-result/case_marie");
Check(CaseResultRules.Apply(claimOnly, CaseResultRules.Build("Level_01", claimOnly.campaign.levelOne, null, null, true, 1, 1, 1, 1, 1f), null) ==
      CaseResultRules.Outcome.AlreadyRecorded && !claimOnly.campaign.levelOneCaseResult.recorded,
    "an existing claim alone also blocks a second record");
var uncorrected = PlayerProgressData.CreateNewCampaign("t07-x", ""); uncorrected.campaign.levelOne = Checked(2);
Check(CaseResultRules.Apply(uncorrected, CaseResultRules.Build("Level_01", uncorrected.campaign.levelOne, "case_marie", null, true, 1, 1, 1, 1, 1f), null) ==
      CaseResultRules.Outcome.Rejected && CaseResultRules.Apply(Fresh(), built, null) == CaseResultRules.Outcome.Rejected,
    "no record without the corrected quest or without a campaign");

// ---------- every tier continues the same way (T08: Marie's follow-through, then the T16 wrap-up bridge) ----------
var nextSteps = new HashSet<string>();
bool allBridge = true;
foreach (int correct in new[] { 0, 2, 3, 4 })
{
    var d = CaseCampaign(correct);
    CaseResultRules.Apply(d, CaseResultRules.Build("Level_01", d.campaign.levelOne, null, null, true, correct * 10, 150, 1, 12, 1f), CaseRun(correct * 10));
    nextSteps.Add(d.campaign.levelOneCaseResult.nextStep);
    bool pending = CaseResultRules.FollowThroughPending(d) && !d.campaign.IsChapterCompleted("Level_01");
    bool bridged = CaseResultRules.CompleteFollowThrough(d).Applied && d.campaign.IsChapterCompleted("Level_01") && d.campaign.levelOne.IsSet &&
                   d.campaign.levelOne.handoverPhase == Level1FieldCheckpoint.HandoverDone && d.campaign.levelOneEndFlowPending &&
                   d.campaign.levelOneCaseResult.recorded && d.campaign.levelOneCaseResult.firstCheckCorrect == correct;
    allBridge &= pending && bridged && CaseResultRules.CompleteFollowThrough(d).Outcome == CampaignRewards.Outcome.Rejected && d.campaign.publicTrust == 44;
}
Check(allBridge && nextSteps.Count == 1, "every tier waits for one +4 handover, then keeps a Done checkpoint/end-flow marker while preserving the first result");
var noResult = CaseCampaign(4);
Check(!CaseResultRules.FollowThroughPending(noResult) && CaseResultRules.CompleteFollowThrough(noResult).Outcome == CampaignRewards.Outcome.Rejected &&
      !noResult.campaign.IsChapterCompleted("Level_01") && noResult.campaign.publicTrust == 40,
    "the follow-through (and Chapter 1) never completes without a recorded case result");

// ---------- save shape ----------
var savedResult = Clone(caseCandidate);
Check(savedResult.campaign.levelOneCaseResult.recorded && savedResult.campaign.levelOneCaseResult.tier == 1 && !savedResult.Normalize(),
    "the case result survives serialization and a valid one needs no repair");
var junk = PlayerProgressData.CreateNewCampaign("t07-j", "");
junk.campaign.levelOneCaseResult = new Level1CaseResult { recorded = false, actionId = "x", exp = 9, tier = 2 };
var overTier = Clone(caseCandidate); overTier.campaign.levelOneCaseResult.tier = 7; overTier.campaign.levelOneCaseResult.exp = -5;
junk.Normalize(); overTier.Normalize();
Check(junk.campaign.levelOneCaseResult.actionId == "" && junk.campaign.levelOneCaseResult.exp == 0 &&
      overTier.campaign.levelOneCaseResult.tier == 3 && overTier.campaign.levelOneCaseResult.exp == 0,
    "repair: an unrecorded result is empty; a recorded one keeps sane values");

// ---------- career: story status follows completed chapters, not scores ----------
CareerCatalog openCatalog = new CareerCatalog();
openCatalog.chapters.Add(new CareerChapterDef { id = "Level_01", number = 1, scene = "Cutscene2.0", cases = 1 });
openCatalog.chapters.Add(new CareerChapterDef { id = "Level_02", number = 2, scene = "Office_Level2", cases = 1 });
openCatalog.chapters.Add(new CareerChapterDef { id = "Level_03", number = 3, comingSoon = true });
var lowButDone = PlayerProgressData.CreateNewCampaign("t07-c1", "");
lowButDone.RecordLevelResult("Level_01", 40, 2, 150, 12, false, null);
lowButDone.campaign.MarkChapterCompleted("Level_01");
CareerSnapshot lowView = CareerProgress.Build(lowButDone, openCatalog, Rules);
Check(lowView.Chapters[0].Status == CareerChapterStatus.Passed && CareerProgress.StatusLabel(lowView.Chapters[0]) == "COMPLETED" &&
      lowView.Chapters[1].Status == CareerChapterStatus.Available && lowView.Chapters[1].Launchable,
    "campaign: a completed chapter reads COMPLETED and opens the next one even with a low score (no TRY AGAIN)");
var highNotDone = PlayerProgressData.CreateNewCampaign("t07-c2", "");
highNotDone.RecordLevelResult("Level_01", 150, 12, 150, 12, true, null);
CareerSnapshot highView = CareerProgress.Build(highNotDone, openCatalog, Rules);
Check(highView.Chapters[0].Status == CareerChapterStatus.Available && highView.Chapters[1].Status == CareerChapterStatus.Locked &&
      CareerProgress.LockedText(highView.Chapters[1]).Contains("story"),
    "campaign: a high score without the completed story opens nothing");
Check(CareerProgress.NextStep(highView).StartsWith("Finish Chapter 1's story") && CareerProgress.MasteryLine(highView.Chapters[0]).StartsWith("Mastery goal (optional)") &&
      CareerProgress.StatusLabel(belowView.Chapters[0]) == "TRY AGAIN" && CareerProgress.MasteryLine(belowView.Chapters[0]).StartsWith("Pass line"),
    "campaign wording describes mastery as optional; non-campaign (development) saves keep the old pass line");

// ---------- T08 Client Copy phase, save/resume and once-only completion ----------
var copyStory = CaseCampaign(2);
CampaignRewards.CompleteActivity(copyStory, CampaignRewards.Level1, CampaignRewards.FieldAssignment);
CampaignRewards.Grant(copyStory, CampaignRewards.Level1, CampaignRewards.FieldAssignment, CampaignRewards.TrustKind);
CampaignRewards.Grant(copyStory, CampaignRewards.Level1, CampaignRewards.FieldAssignment, CampaignRewards.CoinsKind);
Check(CaseResultRules.Apply(copyStory,
        CaseResultRules.Build(CampaignRewards.Level1, copyStory.campaign.levelOne, null, null, true, 80, 150, 3, 12, 300f), CaseRun(80)) ==
      CaseResultRules.Outcome.Applied && copyStory.campaign.publicTrust == 42 && copyStory.campaign.coins == 160,
    "the approved result and the earlier assignment reward are committed before a Client Copy pickup");

Level1FieldCheckpoint copyCheckpoint = copyStory.campaign.levelOne;
copyCheckpoint.handoverPhase = Level1FieldCheckpoint.HandoverNone;
Check(Level1FieldCheckpointRules.ValidateHandover(copyCheckpoint, caseResultRecorded: true, followThroughDone: false) ==
      Level1RestoreOutcome.Repaired && copyCheckpoint.handoverPhase == Level1FieldCheckpoint.HandoverAtAuditor &&
      ClientCopyRules.PhaseOf(copyStory) == ClientCopyPhase.AtAuditor,
    "a saved case result with no phase safely resumes with the copy beside the Auditor");
var auditorResume = Clone(copyStory);
Check(Level1FieldCheckpointRules.ValidateHandover(auditorResume.campaign.levelOne, caseResultRecorded: true, followThroughDone: false) ==
      Level1RestoreOutcome.Restored && ClientCopyRules.PhaseOf(auditorResume) == ClientCopyPhase.AtAuditor &&
      auditorResume.campaign.levelOneCaseResult.recorded && auditorResume.campaign.coins == 160 && auditorResume.campaign.publicTrust == 42,
    "the pre-pickup phase, approved result, coins and trust survive a save reload without another reward");

ClientCopyPhase copyPhase = ClientCopyRules.PhaseOf(auditorResume);
int ledgerBeforePickup = auditorResume.campaign.ledger.Count;
int claimsBeforePickup = auditorResume.campaign.rewardClaims.Count;
Check(!ClientCopyRules.CanHandOver(copyPhase) && ClientCopyRules.TryPickUp(ref copyPhase) && copyPhase == ClientCopyPhase.Carried &&
      !ClientCopyRules.TryPickUp(ref copyPhase) &&
      auditorResume.campaign.publicTrust == 42 && auditorResume.campaign.coins == 160 &&
      auditorResume.campaign.ledger.Count == ledgerBeforePickup && auditorResume.campaign.rewardClaims.Count == claimsBeforePickup,
    "pickup changes only the copy phase, cannot award or be applied twice, and cannot hand over before carrying");
copyCheckpoint = auditorResume.campaign.levelOne;
copyCheckpoint.handoverPhase = ClientCopyRules.ToSaved(copyPhase);
Check(Level1FieldCheckpointRules.ValidateHandover(copyCheckpoint, caseResultRecorded: true, followThroughDone: false) ==
      Level1RestoreOutcome.Restored && copyCheckpoint.handoverPhase == Level1FieldCheckpoint.HandoverCarried &&
      !ClientCopyRules.RestoresCaseWork(copyPhase),
    "the carried phase restores before Marie's drop and keeps the approved case out of Receive Case");
var carriedResume = Clone(auditorResume);
Check(ClientCopyRules.PhaseOf(carriedResume) == ClientCopyPhase.Carried && carriedResume.campaign.levelOneCaseResult.recorded &&
      carriedResume.campaign.coins == 160 && carriedResume.campaign.publicTrust == 42,
    "a save reload during the pre-drop conversation restores the carried copy and unchanged rewards");

int ledgerBeforeFailedHandover = carriedResume.campaign.ledger.Count;
Check(ClientCopyRules.AfterHandover(ClientCopyPhase.Carried, saved: false) == ClientCopyPhase.Carried &&
      ClientCopyRules.CanHandOver(ClientCopyRules.PhaseOf(carriedResume)) && carriedResume.campaign.publicTrust == 42 &&
      carriedResume.campaign.coins == 160 && carriedResume.campaign.ledger.Count == ledgerBeforeFailedHandover,
    "a failed handover write leaves the copy carried and publishes no trust, coins or ledger change");

var doneStory = Clone(carriedResume);
CampaignRewards.Change handoverTrust = CaseResultRules.CompleteFollowThrough(doneStory);
var doneResume = Clone(doneStory);
Check(handoverTrust.Applied && handoverTrust.Amount == 4 && doneResume.campaign.publicTrust == 46 && doneResume.campaign.coins == 160 &&
      ClientCopyRules.PhaseOf(doneResume) == ClientCopyPhase.Done && doneResume.campaign.IsChapterCompleted(CampaignRewards.Level1) &&
      CampaignRewards.IsActivityCompleted(doneResume, CampaignRewards.Level1, CampaignRewards.FollowThrough) &&
      doneResume.campaign.levelOne.IsSet && doneResume.campaign.levelOne.handoverPhase == Level1FieldCheckpoint.HandoverDone &&
      doneResume.campaign.levelOneEndFlowPending,
    "the saved handover completes once, keeps a durable Done checkpoint and pending end flow, and preserves 46 trust / 160 coins");
Check(Level1FieldCheckpointRules.ValidateHandover(doneResume.campaign.levelOne, caseResultRecorded: true, followThroughDone: true) ==
      Level1RestoreOutcome.Restored && ClientCopyRules.ToSaved(ClientCopyPhase.Done) == Level1FieldCheckpoint.HandoverDone &&
      ClientCopyRules.PhaseOf(Clone(doneResume)) == ClientCopyPhase.Done,
    "the post-handover phase and end-flow marker survive serialization and validate as the saved finale/card state");
string doneSnapshot = Snap(doneResume);
Check(ClientCopyRules.AfterHandover(ClientCopyPhase.Carried, saved: true) == ClientCopyPhase.Done &&
      CaseResultRules.CompleteFollowThrough(doneResume).Outcome == CampaignRewards.Outcome.Rejected && Snap(doneResume) == doneSnapshot,
    "a repeated drop after reload changes no completion, trust, coin or claim state");

var noResultHandover = new Level1FieldCheckpoint { handoverPhase = Level1FieldCheckpoint.HandoverCarried };
Check(Level1FieldCheckpointRules.ValidateHandover(noResultHandover, caseResultRecorded: false, followThroughDone: false) ==
      Level1RestoreOutcome.Repaired && noResultHandover.handoverPhase == Level1FieldCheckpoint.HandoverNone &&
      ClientCopyRules.PhaseOf(Fresh()) == ClientCopyPhase.None &&
      ClientCopyRules.Parse("unknown-phase") == ClientCopyPhase.None,
    "a Client Copy phase without a result is dropped, and unknown phase text is not treated as a carried copy");
var invalidHandover = new Level1FieldCheckpoint { handoverPhase = "future-phase" };
Check(Level1FieldCheckpointRules.ValidateHandover(invalidHandover, caseResultRecorded: true, followThroughDone: false) ==
      Level1RestoreOutcome.Repaired && invalidHandover.handoverPhase == Level1FieldCheckpoint.HandoverAtAuditor &&
      !ClientCopyRules.RestoresCaseWork(ClientCopyPhase.AtAuditor) && !ClientCopyRules.RestoresCaseWork(ClientCopyPhase.Done),
    "an unknown phase falls back to the Auditor and every post-result phase avoids restoring case work");
var staleDonePhase = new Level1FieldCheckpoint { handoverPhase = Level1FieldCheckpoint.HandoverCarried };
Check(Level1FieldCheckpointRules.ValidateHandover(staleDonePhase, caseResultRecorded: true, followThroughDone: true) ==
      Level1RestoreOutcome.Repaired && staleDonePhase.handoverPhase == Level1FieldCheckpoint.HandoverDone,
    "an older carried checkpoint repairs to Done once the saved follow-through activity is complete");

// ---------- wiring (source contracts; the result card and scene flow are Unity runtime paths) ----------
string ppm = Src("RewardsSystem", "PlayerProgressManager.cs").Replace("\r\n", "\n");
string applyLevel = Between(ppm, "public void ApplyLevelResult(", "public static CampaignRunMode CurrentRunMode");
Check(applyLevel.Contains("levelTotal.LevelId != CampaignRecord.FirstChapterId &&") && !applyLevel.Contains("progress.campaign.levelOne = new Level1FieldCheckpoint();"),
    "a level result never completes Chapter 1 directly (only the case result + follow-through bridge do)");
string commit = Between(ppm, "public CaseResultCommit CommitLevelOneCaseResult(", "public CampaignRewards.Change LastCaseCoins");
Check(commit.Contains("CaseResultRules.CanRecord(true, result.questCorrected, result.caseApproved)") && commit.Contains("PlayerProgressData candidate = CloneProgress();") &&
      Index(commit, "CaseResultRules.Apply(candidate") < Index(commit, "TryWriteCandidate(candidate)") && commit.Contains("if (!IsStoryCaseResult(levelTotal)) return CaseResultCommit.NotStory;"),
    "the case result is a story-only command applied to a candidate, written, then published");
string write = Between(ppm, "private bool TryWriteCandidate(", "public bool UsesLevelOneCheckpoint");
Check(Index(write, "store.Write(Encode(candidate));") < Index(write, "progress = candidate;") && write.Contains("return false;"),
    "a failed write leaves the live progress as it was");
Check(ppm.Contains("CampaignRun.AllowsStoryWrites(CurrentRunMode)") && Between(ppm, "public bool IsStoryCaseResult(", "/// <summary>").Contains("levelTotal.LevelId == CampaignRecord.FirstChapterId"),
    "practice, mastery replay and direct Editor runs never reach the case result");
string checkpointUse8 = Between(ppm, "public bool UsesLevelOneCheckpoint", "public bool SaveLevelOneCheckpoint");
Check(checkpointUse8.Contains("CaseResultRules.FollowThroughPending(progress)") && checkpointUse8.Contains("levelOneEndFlowPending"),
    "the Level 1 checkpoint stays writable through the Client Copy handover and pending end card");
string clearCheckpoint8 = Between(ppm, "public bool ClearLevelOneCheckpoint()", "public bool MarkLevelOneIntroSeen()");
Check(CaseResultRules.FollowThroughPending(auditorResume) && CaseResultRules.FollowThroughPending(carriedResume) &&
      doneResume.campaign.levelOneEndFlowPending &&
      Index(clearCheckpoint8, "FollowThroughPending(progress)") < Index(clearCheckpoint8, "progress.campaign.levelOne = new Level1FieldCheckpoint();") &&
      Index(clearCheckpoint8, "levelOneEndFlowPending") < Index(clearCheckpoint8, "progress.campaign.levelOne = new Level1FieldCheckpoint();") &&
      clearCheckpoint8.Contains("return false;"),
    "Settings replay refuses to erase the checkpoint at Auditor, carried, or Done/end-flow phases until the save can continue");
string settingsBoard8 = Src("Settings", "SettingsBoard3D.cs").Replace("\r\n", "\n");
string copyReplayPrompt8 = Between(settingsBoard8, "private void RequestAction(SettingsAction action)", "private bool MustFinishClientCopy()");
string mustFinishCopy8 = Between(settingsBoard8, "private bool MustFinishClientCopy()", "private void CancelConfirmation()");
string confirmSettings8 = Between(settingsBoard8, "private void ConfirmAction()", "private void BuildPlate(");
Check(mustFinishCopy8.Contains("CampaignRun.AllowsStoryWrites(PlayerProgressManager.CurrentRunMode)") &&
      mustFinishCopy8.Contains("manager.FollowThroughPending || manager.PendingLevelOneEndFlow") &&
      copyReplayPrompt8.Contains("SettingsAction.ResumeClientCopy") && copyReplayPrompt8.Contains("CASE STILL IN PROGRESS") &&
      copyReplayPrompt8.Contains("RESUME LEVEL") && copyReplayPrompt8.Contains("STAY") &&
      confirmSettings8.Contains("case SettingsAction.ResumeClientCopy:") && confirmSettings8.Contains("simply close settings"),
    "replay during the story copy or pending card offers Stay/Resume Level, keeps the save and closes settings without restarting");
string awake8 = Between(ppm, "private void Awake()", "private void OnDestroy()");
Check(!awake8.Contains("CommitFollowThrough(") && !awake8.Contains("AcknowledgeLevelOneEndFlow("),
    "a result saved before closing is never completed or acknowledged automatically during load");
string levelUi = Src("UI", "LevelCompleteUI.cs").Replace("\r\n", "\n");
string show = Between(levelUi, "public void Show(LevelTotalResult result)", "private const string CaseResultSaveFailedText");
Check(Index(show, "caseResultSaved = storyCaseResult && TryCommitCaseResult();") < Index(show, "PlayAchievementSFX") &&
      show.Contains("if (AudioManager.Instance != null && (!storyCaseResult || caseResultSaved))") && show.Contains("if (manager != null && result != null && !storyCaseResult)"),
    "the case result is committed before any celebration; an unsaved result plays no fanfare");
string resultCard = Between(levelUi, "private static RewardScreenData BuildCaseResultScreen(", "/// <summary>The reward card for the level");
Check(resultCard.Contains("Title = delivered ? CaseResultRules.CompletedTitle : \"Case Approved\"") && resultCard.Contains("MaxHearts = 0,") && resultCard.Contains("PassingPercent = 0,") &&
      resultCard.Contains("CaseResultRules.NextStepText") && resultCard.Contains("CampaignRewards.IsActivityCompleted(career, CampaignRewards.Level1, CampaignRewards.FollowThrough)") &&
      !resultCard.Contains("Chapter Complete") && !resultCard.Contains("PASSED"),
    "the case result card: Case Approved before the handover (next step follow-through), Level Complete once Marie has her copy; frozen tier, no pass line, no score hearts");
Check(levelUi.Contains("BeginAfterApproval(") && levelUi.Contains("ShowAfterHandover(") &&
      !levelUi.Contains("FollowThroughView") && !levelUi.Contains("CompleteChapterBridge"),
    "case approval saves quietly, while the result card and finale wait for the Client Copy handover");

// =====================================================================
// T08: Public Trust through service; T09: completion coins and wallet
// =====================================================================

// ---------- catalogue ----------
Check(CampaignRewards.Amount("Level_01", CampaignRewards.FieldAssignment, CampaignRewards.TrustKind) == 2 &&
      CampaignRewards.Amount("Level_01", CampaignRewards.FollowThrough, CampaignRewards.TrustKind) == 4 &&
      CampaignRewards.Amount("Level_01", CampaignRewards.FieldAssignment, CampaignRewards.CoinsKind) == 60 &&
      CampaignRewards.Amount("Level_01", CampaignRewards.CaseApproved, CampaignRewards.CoinsKind) == 100 &&
      CampaignRewards.Total("Level_01", CampaignRewards.CoinsKind) == 160 && CampaignRewards.Total("Level_01", CampaignRewards.TrustKind) == 6 &&
      CampaignRewards.SupportTarget == 65 && CampaignRecord.StartingTrust == 40,
    "Level 1 story8 rewards: trust +2 field assignment, +4 follow-through (40 -> 46); coins 60 + 100 = 160; support target 65");
Check(CampaignRewards.ClaimId("camp-1", "Level_01", "field-assignment", "coins") == "camp-1/Level_01/field-assignment/coins",
    "claims are keyed by campaign + level + activity + kind (never a run id)");
var fresh09 = PlayerProgressData.CreateNewCampaign("t09-fresh", "");
Check(fresh09.campaign.coins == 0 && fresh09.campaign.publicTrust == 40 && fresh09.campaign.ledger.Count == 0 && fresh09.campaign.completedActivityIds.Count == 0,
    "a new campaign starts with 0 coins, trust 40, an empty ledger and no completed activities");

// ---------- grants: once, bounded, reasons ----------
var grants = PlayerProgressData.CreateNewCampaign("t08-g", "");
var trust2 = CampaignRewards.Grant(grants, "Level_01", CampaignRewards.FieldAssignment, CampaignRewards.TrustKind);
var coins60 = CampaignRewards.Grant(grants, "Level_01", CampaignRewards.FieldAssignment, CampaignRewards.CoinsKind);
Check(trust2.Applied && trust2.Before == 40 && trust2.After == 42 && trust2.Amount == 2 && coins60.Applied && coins60.After == 60 &&
      grants.campaign.ledger.Count == 2 && grants.campaign.ledger[0].reason == "Town field assignment completed" && grants.campaign.ledger[1].balanceAfter == 60,
    "a grant changes the balance, claims it and records a reason in the ledger");
string grantsAfter = Snap(grants);
Check(CampaignRewards.Grant(grants, "Level_01", CampaignRewards.FieldAssignment, CampaignRewards.TrustKind).Outcome == CampaignRewards.Outcome.AlreadyClaimed &&
      CampaignRewards.Grant(grants, "Level_01", CampaignRewards.FieldAssignment, CampaignRewards.CoinsKind).Outcome == CampaignRewards.Outcome.AlreadyClaimed &&
      Snap(grants) == grantsAfter,
    "a repeated grant (duplicate callback, replay, relaunch, restarted story8) changes nothing");
Check(CampaignRewards.Grant(grants, "Level_01", "wrong-drop", CampaignRewards.TrustKind).Outcome == CampaignRewards.Outcome.Rejected &&
      CampaignRewards.Grant(grants, "Level_01", CampaignRewards.FieldAssignment, "hearts").Outcome == CampaignRewards.Outcome.Rejected &&
      CampaignRewards.Grant(Fresh(), "Level_01", CampaignRewards.FieldAssignment, CampaignRewards.CoinsKind).Outcome == CampaignRewards.Outcome.Rejected &&
      Snap(grants) == grantsAfter,
    "unknown ids or kinds and non-campaign saves are rejected with no change (no trust from mistakes or tutorials)");
var nearMax = PlayerProgressData.CreateNewCampaign("t08-max", ""); nearMax.campaign.publicTrust = 98;
Check(CampaignRewards.Grant(nearMax, "Level_01", CampaignRewards.FollowThrough, CampaignRewards.TrustKind).After == 100 && nearMax.campaign.publicTrust == 100,
    "trust is clamped to 100");
var rich = PlayerProgressData.CreateNewCampaign("t09-rich", ""); rich.campaign.coins = int.MaxValue - 10;
string richBefore = Snap(rich);
Check(CampaignRewards.Grant(rich, "Level_01", CampaignRewards.CaseApproved, CampaignRewards.CoinsKind).Outcome == CampaignRewards.Outcome.Rejected && Snap(rich) == richBefore,
    "a coin grant that would overflow the wallet is rejected with no change");
var negative = PlayerProgressData.CreateNewCampaign("t09-neg", ""); negative.campaign.coins = -25;
negative.Normalize();
Check(negative.campaign.coins == 0 && negative.campaign.ledger.Count == 1 && negative.campaign.ledger[0].kind == "repair",
    "a corrupt negative balance is repaired to 0 with an explicit repair record");
var scored = PlayerProgressData.CreateNewCampaign("t09-score", "");
scored.RecordLevelResult("Level_01", 150, 12, 150, 12, true, null);
Check(scored.campaign.coins == 0 && scored.campaign.publicTrust == 40, "XP, score hearts and passes never produce coins or trust");

// ---------- the Level 1 story8: 40 -> 46 trust, 0 -> 160 coins ----------
var story8 = CaseCampaign(2);
CampaignRewards.CompleteActivity(story8, "Level_01", CampaignRewards.FieldAssignment);
CampaignRewards.Grant(story8, "Level_01", CampaignRewards.FieldAssignment, CampaignRewards.TrustKind);
CampaignRewards.Grant(story8, "Level_01", CampaignRewards.FieldAssignment, CampaignRewards.CoinsKind);
Check(story8.campaign.publicTrust == 42 && story8.campaign.coins == 60, "corrected field assignment (any tier, here Bronze): trust 42, coins 60");
CaseResultRules.Apply(story8, CaseResultRules.Build("Level_01", story8.campaign.levelOne, null, null, true, 50, 150, 2, 12, 1f), CaseRun(50));
Check(story8.campaign.coins == 160 && CampaignRewards.IsActivityCompleted(story8, "Level_01", CampaignRewards.CaseApproved) &&
      CaseResultRules.FollowThroughPending(story8) && story8.campaign.publicTrust == 42,
    "the approved case adds 100 coins in the same write as its result (160); trust waits for the follow-through");
var followed = CaseResultRules.CompleteFollowThrough(story8);
Check(followed.Applied && followed.Amount == 4 && story8.campaign.publicTrust == 46 && story8.campaign.coins == 160 &&
      story8.campaign.IsChapterCompleted("Level_01") && !CaseResultRules.FollowThroughPending(story8) &&
      CampaignRewards.IsActivityCompleted(story8, "Level_01", CampaignRewards.FollowThrough) &&
      story8.campaign.levelOne.IsSet && story8.campaign.levelOne.handoverPhase == Level1FieldCheckpoint.HandoverDone &&
      story8.campaign.levelOneEndFlowPending,
    "Marie's saved handover gives trust 46/100 and keeps Chapter 1's finale/card flow resumable until Continue");
string storyAfter = Snap(story8);
CaseResultRules.Apply(story8, CaseResultRules.Build("Level_01", story8.campaign.levelOne, "case_marie", null, true, 150, 150, 12, 12, 1f), CaseRun(150));
Check(CaseResultRules.CompleteFollowThrough(story8).Outcome == CampaignRewards.Outcome.Rejected && Snap(story8) == storyAfter,
    "repeating the result or the follow-through (reload, replay) adds no trust or coins");
var storyBack = Clone(story8);
Check(storyBack.campaign.ledger.Count == 4 && storyBack.campaign.completedActivityIds.Count == 3 && !storyBack.Normalize(),
    "trust, coins, claims and reasons survive the save");

// ---------- wiring (source contracts; the follow-through scene, HUD and boards are Unity runtime paths) ----------
string ppm8 = Src("RewardsSystem", "PlayerProgressManager.cs").Replace("\r\n", "\n");
string fieldGrant = Between(ppm8, "public StoryGrants CommitFieldAssignment(", "public StoryGrants CommitFollowThrough()");
Check(fieldGrant.Contains("CampaignRun.AllowsStoryWrites(CurrentRunMode)") && fieldGrant.Contains("PlayerProgressData candidate = CloneProgress();") &&
      Index(fieldGrant, "CampaignRewards.Grant(candidate") < Index(fieldGrant, "TryWriteCandidate(candidate)"),
    "the field-assignment grants are a story8-only command on a candidate, written before anything is shown");
string follow = Between(ppm8, "public StoryGrants CommitFollowThrough()", "public bool AcknowledgeLevelOneEndFlow()");
Check(follow.Contains("if (!FollowThroughPending)") && Index(follow, "CaseResultRules.CompleteFollowThrough(candidate)") < Index(follow, "TryWriteCandidate(candidate)"),
    "the follow-through commits only while pending, as one candidate write");
Check(Index(follow, "!CampaignRun.AllowsStoryWrites(CurrentRunMode)") < Index(follow, "if (!FollowThroughPending)") &&
      follow.Contains("TrainingSession.IsPracticing"),
    "follow-through calls in replay, editor or practice cannot mutate or report story success");
string npc8 = Src("SideQuest1", "TutorialNpcController.cs");
Check(Index(npc8, "CommitFieldAssignment(corkboardSideQuest.IsCompleted)") < Index(npc8, "SideQuestCompleteUI.Instance.Show(result)") &&
      npc8.Contains("if (grants.Saved && !grants.AlreadyDone) HudFeedback.AnnounceCampaign("),
    "the guide's side-quest reward saves the grants first and shows/announces them only when saved");
Check(Src("SideQuest1", "Level1FieldCheckpointer.cs").Contains("if (rewarded && Progress != null) Progress.CommitFieldAssignment(questCorrected: true);"),
    "a resumed rewarded board gets missing grants quietly (claimed once)");
string handover8 = Src("UI", "HUD", "ClientCopyHandover.cs").Replace("\r\n", "\n");
string enterCopy8 = Between(handover8, "private void EnterCopyMode()", "private void ExitCopyMode()");
string exitCopy8 = Between(handover8, "private void ExitCopyMode()", "private static void DecoratePage(");
string decoratePage8 = Between(handover8, "private static void DecoratePage(", "private static void RefreshOpenPages()");
string handoverLines8 = Between(handover8, "private List<DialogueLine> HandoverLines()", "private string TierEmote(");
string iconTap8 = Between(handover8, "public bool TryIconTap()", "public HudDragOutcome? ResolveDrop(");
string resolveDrop8 = Between(handover8, "public HudDragOutcome? ResolveDrop(", "private bool OverClient(");
string handoverCommit8 = Between(handover8, "private bool TryHandOver()", "private void OnConversationEnded()");
string blockedCase8 = Between(handover8, "public static bool CaseActionsBlocked =>", "public static bool Defers =>");
Check(enterCopy8.Contains("CaseFolderFieldRow.ReadOnly = true;") && exitCopy8.Contains("CaseFolderFieldRow.ReadOnly = false;") &&
      decoratePage8.Contains("ClientCopyStamp.Build(page.Paper") && handover8.Contains("ClientCopyStamp.BuildTag(icon)"),
    "the carried Client Copy locks the shared case folder and stamps its front page and icon");
Check(blockedCase8.Contains("InProgress") && blockedCase8.Contains("ClientCopyPhase.Done") &&
      blockedCase8.Contains("FollowThroughPending") && blockedCase8.Contains("PendingLevelOneEndFlow") &&
      blockedCase8.Contains("CampaignRun.AllowsStoryWrites(PlayerProgressManager.CurrentRunMode)"),
    "approved case editing stays blocked from result commit through handover and result-card acknowledgement, including reloads");
string fieldRow8 = Src("Data", "DragDrop", "CaseFolderFieldRow.cs");
Check(fieldRow8.Contains("public static bool ReadOnly;") &&
      Between(fieldRow8, "public void OnPointerClick(", "public void OnDrop(").Contains("if (ReadOnly) return;") &&
      Between(fieldRow8, "public void OnDrop(", "public DataValue GetDataValue()").Contains("if (ReadOnly) return;") &&
      Between(fieldRow8, "public bool CanAccept(", "public bool TryReceiveValue(").Contains("if (ReadOnly || !isDestination)") &&
      Between(fieldRow8, "public bool WouldAccept(", "public bool CanAccept(").Contains("return !ReadOnly"),
    "read-only client fields reject clicks and drops that would edit or transfer values");
string stampSet8 = Src("Interactables", "StampSetInteractable.cs");
string stampUi8 = Src("UI", "StampUI.cs");
string stampEntry8 = Between(stampSet8, "public void OnInteract()", "public string GetPromptText()");
string stampApply8 = Between(stampUi8, "public void ApplyStampType(StampType type)", "private void ApplyStamp(");
string submitTray8 = Src("Data", "AuditorSubmissionTray.cs");
Check(Index(stampEntry8, "if (ClientCopyHandover.CaseActionsBlocked) return;") < Index(stampEntry8, "caseFolderUI.ShowForStamping()") &&
      Index(stampApply8, "if (ClientCopyHandover.CaseActionsBlocked) return;") < Index(stampApply8, "ApplyStamp(") &&
      Between(submitTray8, "public bool CanAccept(DataValue value)", "public bool TryReceiveValue(DataValue value)").Contains("!ClientCopyHandover.CaseActionsBlocked") &&
      Between(submitTray8, "public bool TryReceiveValue(DataValue value)", "// ACCEPT SUBMISSION").Contains("if (!CanAccept(value))"),
    "the carried copy cannot enter stamping or mutate case assessment, and the submission tray independently rejects it");
string officeHud8 = Src("UI", "HUD", "OfficeHudRuntime.cs");
Check(officeHud8.Contains("folderIcon.DragVisual = HudDragItem.Create") && officeHud8.Contains("if (ClientCopyHandover.CopyActive)") &&
      officeHud8.Contains("ClientCopyStamp.Build(root,") &&
      resolveDrop8.Contains("if (!OverClient(screen)) return HudDragOutcome.Returned;") &&
      resolveDrop8.Contains("TryHandOver() ? HudDragOutcome.Consumed : HudDragOutcome.Returned;"),
    "the drag ghost carries the Client Copy stamp, and every non-Marie drop returns the copy to the icon");
string gateIcons8 = Between(officeHud8, "private void GateNonFolderIcons(bool active)", "private bool IsCaseFolderControl(");
string ungateIcons8 = Between(gateIcons8, "if (!awaitedControlsApplied) return;", "private bool IsCaseFolderControl(");
Check(gateIcons8.Contains("if (icon != null)") && gateIcons8.Contains("awaitedIconStates.Add(icon, icon.enabled)") &&
      gateIcons8.Contains("icon.enabled = false;") && gateIcons8.Contains("handler is HudSubmittableIcon") &&
      gateIcons8.Contains("awaitedPointerHandlerStates.Add(handler, handler.enabled)") &&
      ungateIcons8.Contains("foreach (KeyValuePair<HudSubmittableIcon, bool> saved in awaitedIconStates)") &&
      ungateIcons8.Contains("saved.Key.enabled = saved.Value;") && ungateIcons8.Contains("awaitedIconStates.Clear();"),
    "awaited dialogue saves each submittable icon state once and restores it through the same dedicated map");
string dialogueUi8 = Src("UI", "DialogueUI.cs");
string gesture8 = Between(dialogueUi8, "private void HandleGesture()", "// ------------------------------------------------------------------\n    // Awaited actions");
string completeAction8 = Between(dialogueUi8, "public bool CompleteAwaitedAction(", "/// <summary>Replaces the waiting line's prompt");
Check(gesture8.Contains("if (session.IsAwaitingAction && !reveal.IsTyping) return;") &&
      completeAction8.Contains("session.TryCompleteAction(actionId)") && completeAction8.Contains("session.TryAdvance(out bool completed)") &&
      handoverLines8.Contains("wait.AwaitAction = ClientCopyRules.HandoverAction;") && handoverLines8.Contains("wait.ActionPrompt ="),
    "dialogue taps stay blocked on the awaiting line; only the matching action advances to Marie's closing lines");
Check(officeHud8.Contains("TryIconTap()") && officeHud8.Contains("DropResolver") &&
      iconTap8.Contains("if (!AwaitingHandover()) return false;") && iconTap8.Contains("TryHandOver();") &&
      officeHud8.Contains("ClientCopyStamp.Build(root,") &&
      handoverCommit8.Contains("if (!grants.Saved)") &&
      Index(handoverCommit8, "if (!grants.Saved)") < Index(handoverCommit8, "WorldEmote.Burst") &&
      Index(handoverCommit8, "if (!grants.Saved)") < Index(handoverCommit8, "HudFeedback.AnnounceCampaign("),
    "the folder icon tap is a handover fallback, and Marie's burst/trust toast wait for a saved commit");
string pickup8 = Src("CaseFolder", "ClientCopyPickup.cs");
Check(pickup8.Contains("ClientCopyStamp.Build(") && handover8.Contains("HudDragOutcome.Returned"),
    "the floating folder copy is stamped and failed/incorrect handover attempts preserve it");
string entry8 = Src("UI", "MenuEntry.cs");
Check(Between(entry8, "public static void Play(", "public static bool StartNewGame(").Contains("PendingLevelOneEndFlow") &&
      entry8.Contains("Progress.CheckpointFor(CampaignRecord.FirstChapterId) != null") &&
      Between(entry8, "private static void ResumeLevelOneEndFlow()", "/// <summary>True:").Contains("Office_Level1"),
    "PLAY resumes the saved copy or pending finale/card in Office_Level1");
string onReturn8 = Between(levelUi, "public void OnReturnToMainModulePressed()", "private void LeaveForCareer()");
Check(onReturn8.Contains("AcknowledgeLevelOneEndFlow()") && !levelUi.Contains("FollowThroughView") &&
      !File.Exists(Path.Combine(root, "Assets", "Scripts", "UI", "HUD", "FollowThroughView.cs")) &&
      !Src("RewardsSystem", "CampaignRewards.cs").Contains("class FollowThroughScript"),
    "the result card Continue acknowledges the durable end flow; the replaced follow-through card and script are removed");
string trustDisplay = Src("UI", "HUD", "TrustDisplay.cs");
string feedback8 = Src("UI", "HUD", "HudFeedback.cs");
Check(trustDisplay.Contains("manager.CurrentProgress.HasCampaign") && Src("UI", "HUD", "HudRewardScreen.cs").Contains("if (TrustDisplay.Active)") &&
      feedback8.Contains("int hearts = TrustDisplay.Active ? 0 :") && feedback8.Contains("if (hearts > 0 && !TrustDisplay.Active)") &&
      Src("UI", "HUD", "HudProgressionPanel.cs").Contains("reputation = TrustDisplay.PublicTrust;"),
    "in a campaign every heart shows Public Trust: no score-heart toasts, flights, losses or card rows; the HUD bar is trust/100");
string board8 = Src("UI", "Menu", "CareerBoardPresenter.cs");
Check(board8.Contains("heartsCaption.text = snapshot.Campaign ? \"PUBLIC TRUST\"") && board8.Contains("{snapshot.Coins:N0} COINS") &&
      Src("UI", "Menu", "CareerChapterSheet.cs").Contains("row.gameObject.SetActive(!chapter.Campaign)"),
    "the career board shows Public Trust (target 65) and the wallet; chapter score hearts are hidden in a campaign");
CareerSnapshot trustView = CareerProgress.Build(story8, openCatalog, Rules);
Check(trustView.Campaign && trustView.PublicTrust == 46 && trustView.Coins == 160, "the career snapshot carries the saved trust and wallet");

// =====================================================================
// T23: Marie's consultation visits in the conference room
// =====================================================================

// ---------- selector: priority, ladder, no repeats, resets ----------
ConsultationVariant V23(string id, string text) => new ConsultationVariant { id = id, lines = new List<DialogueLineDefinition> { new DialogueLineDefinition { speaker = "Npc", text = text } } };
List<DialogueLineDefinition> Lines23(string text) => new List<DialogueLineDefinition> { new DialogueLineDefinition { speaker = "Npc", text = text } };
ClientConsultationDefinition Ladder23(int variantsPerStep)
{
    var d = new ClientConsultationDefinition();
    for (int step = 0; step < 5; step++)
    {
        var s = new ConsultationStep { mood = ClientConsultationRules.Moods[step] };
        for (int v = 0; v < variantsPerStep; v++) s.variants.Add(V23($"s{step}v{v}", $"step {step} variant {v}"));
        d.ladder.Add(s);
    }
    d.failedAudit.Add(V23("fa1", "oh no")); d.failedAudit.Add(V23("fa2", "not good"));
    d.laterDay.Add(V23("day1", "hoping yesterday")); d.laterDay.Add(V23("day2", "another day"));
    d.fallback = Lines23("hello again");
    return d;
}
ConsultationContext Ctx23(int failed = 0, int day = 1, bool open = false, string unread = null, string emptyField = null, string wrongField = null) => new ConsultationContext
{
    FailedAudits = failed, Day = day, CaseOpen = open,
    DocumentUnread = doc => doc == unread,
    Field = key => key == emptyField ? ConsultationFieldState.Empty : key == wrongField ? ConsultationFieldState.Wrong : ConsultationFieldState.Filled
};

var def23 = Ladder23(2);
var st23 = new Level1ConsultationRecord();
var steps23 = new List<int>(); var ids23 = new List<string>(); bool noRepeat23 = true;
for (int i = 0; i < 9; i++)
{
    ConsultationPick pick = ClientConsultationRules.Next(def23, st23, Ctx23());
    steps23.Add(pick.Step); if (ids23.Count > 0 && ids23[^1] == pick.Id) noRepeat23 = false; ids23.Add(pick.Id);
}
Check(string.Join(",", steps23) == "0,1,2,3,4,4,4,4,4" && st23.visits == 9 && st23.ladderVisits == 9,
    "ladder thresholds: visits 1-4 use steps 1-4 (helpful, friendly, teasing, playful) and 5+ stay on the redirect step");
Check(noRepeat23, "the ladder never says the same line twice in a row, including the repeating redirect step");

var one23 = Ladder23(1);
var stOne23 = new Level1ConsultationRecord();
var kinds23 = new List<ConsultationKind>(); var idsOne23 = new List<string>();
for (int i = 0; i < 7; i++) { ConsultationPick p = ClientConsultationRules.Next(one23, stOne23, Ctx23()); kinds23.Add(p.Kind); idsOne23.Add(p.Id); }
Check(Enumerable.Range(1, idsOne23.Count - 1).All(i => idsOne23[i] != idsOne23[i - 1]) && kinds23.Contains(ConsultationKind.Fallback),
    "a redirect step with one variant alternates with the neutral line instead of repeating it");

var clued23 = Ladder23(2);
clued23.clues.Add(new ConsultationClue { id = "clue-doc", document = "Employment Records", lines = Lines23("my records are in there") });
clued23.clues.Add(new ConsultationClue { id = "clue-field", field = "ResidencyStatus", when = "empty", lines = Lines23("my ID has my address") });
var stClue23 = new Level1ConsultationRecord();
ConsultationContext busy23 = Ctx23(failed: 1, open: true, unread: "Employment Records", emptyField: "ResidencyStatus");
ConsultationPick p1 = ClientConsultationRules.Next(clued23, stClue23, busy23);
ConsultationPick p2 = ClientConsultationRules.Next(clued23, stClue23, busy23);
ConsultationPick p3 = ClientConsultationRules.Next(clued23, stClue23, busy23);
ConsultationPick p4 = ClientConsultationRules.Next(clued23, stClue23, busy23);
Check(p1.Kind == ConsultationKind.Clue && p1.Id == "clue-doc" && p2.Kind == ConsultationKind.Clue && p2.Id == "clue-field" &&
      p3.Kind == ConsultationKind.FailedAudit && p4.Kind == ConsultationKind.Ladder && p4.Step == 0,
    "priority: fitting clues first (each once), then the failed-audit moment, then the ladder from its first step");

var stClosed23 = new Level1ConsultationRecord();
Check(ClientConsultationRules.Next(clued23, stClosed23, Ctx23(unread: "Employment Records")).Kind == ConsultationKind.Ladder,
    "no clue before the case folder is in the player's hands");
var stFit23 = new Level1ConsultationRecord();
Check(ClientConsultationRules.Next(clued23, stFit23, Ctx23(open: true)).Kind == ConsultationKind.Ladder,
    "a clue whose document is read and whose field is filled does not fit");
var wrongClue23 = new ConsultationClue { id = "w", field = "IncomeSource", when = "wrong", lines = Lines23("about my pay") };
Check(ClientConsultationRules.ClueFits(wrongClue23, Ctx23(wrongField: "IncomeSource")) && !ClientConsultationRules.ClueFits(wrongClue23, Ctx23(emptyField: "IncomeSource")),
    "a field clue fits only its own condition (wrong vs empty)");

var stReset23 = new Level1ConsultationRecord();
for (int i = 0; i < 3; i++) ClientConsultationRules.Next(def23, stReset23, Ctx23());
ConsultationPick afterFail23 = ClientConsultationRules.Next(def23, stReset23, Ctx23(failed: 1));
ConsultationPick nextFail23 = ClientConsultationRules.Next(def23, stReset23, Ctx23(failed: 1));
Check(afterFail23.Kind == ConsultationKind.FailedAudit && nextFail23.Kind == ConsultationKind.Ladder && nextFail23.Step == 0 &&
      stReset23.seenFailedAudits == 1 && stReset23.pendingSpecial == "",
    "a failed audit: she reacts once, then the ladder starts over");
ConsultationPick newDay23 = ClientConsultationRules.Next(def23, stReset23, Ctx23(failed: 1, day: 2));
ConsultationPick nextDay23 = ClientConsultationRules.Next(def23, stReset23, Ctx23(failed: 1, day: 2));
Check(newDay23.Kind == ConsultationKind.LaterDay && nextDay23.Kind == ConsultationKind.Ladder && nextDay23.Step == 0 && stReset23.seenDay == 2,
    "a new Office Hours day: she reacts once, then the ladder starts over");
var stBoth23 = new Level1ConsultationRecord();
ConsultationPick both23 = ClientConsultationRules.Next(def23, stBoth23, Ctx23(failed: 1, day: 2));
ConsultationPick bothNext23 = ClientConsultationRules.Next(def23, stBoth23, Ctx23(failed: 1, day: 2));
Check(both23.Kind == ConsultationKind.FailedAudit && stBoth23.seenDay == 2 && bothNext23.Kind == ConsultationKind.Ladder,
    "a failed audit and a new day together: one reaction (the failed audit), both markers noted");
var stClueReset23 = new Level1ConsultationRecord();
ClientConsultationRules.Next(clued23, stClueReset23, Ctx23(open: true, unread: "Employment Records"));
bool clueAgainBefore23 = ClientConsultationRules.Next(clued23, stClueReset23, Ctx23(open: true, unread: "Employment Records")).Kind == ConsultationKind.Clue;
ClientConsultationRules.Next(clued23, stClueReset23, Ctx23(open: true, day: 2));
ClientConsultationRules.Next(clued23, stClueReset23, Ctx23(open: true, day: 2));
bool clueAfterReset23 = ClientConsultationRules.Next(clued23, stClueReset23, Ctx23(open: true, day: 3, unread: "Employment Records")).Kind == ConsultationKind.Clue;
Check(!clueAgainBefore23 && clueAfterReset23, "a given clue waits for the next reset before it is given again (never twice in a row)");

// ---------- data: the authored block, fallbacks and malformed entries ----------
string caseJson23 = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Levels", "Level_01", "cases", "case_001.json"));
using var caseDoc23 = JsonDocument.Parse(caseJson23);
var marie23 = JsonSerializer.Deserialize<ClientConsultationDefinition>(caseDoc23.RootElement.GetProperty("consultation").GetRawText(), json);
var documents23 = caseDoc23.RootElement.GetProperty("documents").EnumerateArray().Select(d => d.GetProperty("documentName").GetString()).ToList();
string caseCheckpointSrc23 = Src("Data", "Level1CaseCheckpoint.cs");
var fieldKeys23 = System.Text.RegularExpressions.Regex.Matches(Between(caseCheckpointSrc23, "FieldKeys =", "};"), "\"(\\w+)\"").Select(m => m.Groups[1].Value).ToList();
var answers23 = new List<string>();
foreach (JsonElement field in caseDoc23.RootElement.GetProperty("requiredFields").EnumerateArray())
{
    string answer = caseDoc23.RootElement.GetProperty("answerKey").GetProperty(field.GetProperty("key").GetString()).GetString();
    answers23.Add(answer);
    answers23.Add(System.Text.RegularExpressions.Regex.Replace(answer, "(?<=[a-z])(?=[A-Z])", " "));
}
List<string> problems23 = ClientConsultationRules.Problems(marie23, documents23, fieldKeys23, answers23);
Check(problems23.Count == 0, "Marie's authored consultation is clean: unique ids, clues name a real document or folder field key" +
      (problems23.Count > 0 ? " -> " + string.Join("; ", problems23) : ""));
List<string> texts23 = ClientConsultationRules.AllTexts(marie23);
Check(fieldKeys23.Count == 13 && answers23.Count == 6 && texts23.Count > 20 &&
      texts23.All(text => answers23.All(a => text.IndexOf(a, StringComparison.OrdinalIgnoreCase) < 0)),
    "no consultation line contains an answer-key value (Resident Citizen, Compensation Earner, Employment Income)");
Check(marie23.ladder.Count == 5 && marie23.ladder.All(step => step.variants.Count >= 2 && step.variants.Count <= 3) &&
      marie23.failedAudit.Count >= 2 && marie23.laterDay.Count >= 2 && marie23.clues.Count >= 3,
    "Marie has five ladder steps with two or three variants each, plus clues and both special moments");
var leak23 = new ClientConsultationDefinition();
leak23.ladder.Add(new ConsultationStep { variants = { V23("x", "You are a Resident Citizen!") } });
Check(ClientConsultationRules.Problems(leak23, documents23, fieldKeys23, answers23).Any(p => p.Contains("gives the answer")),
    "the validator catches a line that gives an answer");

Check(!ClientConsultationRules.HasContent(new ClientConsultationDefinition { enabled = false }) &&
      !ClientConsultationRules.HasContent(new ClientConsultationDefinition()) && !ClientConsultationRules.HasContent(null),
    "a missing or empty consultation means no consultation");
var broken23 = new ClientConsultationDefinition();
broken23.clues.Add(new ConsultationClue { id = "both", document = "Employment Records", field = "TaxpayerType", lines = Lines23("x") });
broken23.clues.Add(new ConsultationClue { id = "ghost", document = "Secret Diary", lines = Lines23("y") });
broken23.ladder.Add(new ConsultationStep { variants = { new ConsultationVariant { id = "", lines = Lines23("no id") }, V23("dup", "a"), V23("dup", "b") } });
List<string> brokenProblems23 = ClientConsultationRules.Problems(broken23, documents23, fieldKeys23, answers23);
ConsultationPick brokenPick23 = ClientConsultationRules.Next(broken23, new Level1ConsultationRecord(), Ctx23(open: true, unread: "Employment Records"));
ConsultationPick emptyPick23 = ClientConsultationRules.Next(new ClientConsultationDefinition(), new Level1ConsultationRecord(), Ctx23());
Check(brokenProblems23.Any(p => p.Contains("exactly one")) && brokenProblems23.Any(p => p.Contains("unknown document")) &&
      brokenProblems23.Any(p => p.Contains("missing id")) && brokenProblems23.Any(p => p.Contains("duplicate id")) &&
      brokenPick23.Kind == ConsultationKind.Ladder && brokenPick23.Id == "dup" &&
      emptyPick23.Kind == ConsultationKind.Fallback && emptyPick23.Lines == null,
    "malformed entries are reported and skipped; nothing usable falls back to the built-in neutral line (no exception)");

// ---------- checkpoint: round trip and repair ----------
var cp23 = new Level1FieldCheckpoint { version = Level1FieldCheckpoint.CurrentVersion };
cp23.consultation = new Level1ConsultationRecord { clientInRoom = true, visits = 6, ladderVisits = 2, lastLineId = "redirect-folder",
    seenFailedAudits = 1, seenDay = 2, pendingSpecial = "laterDay", givenClueIds = new List<string> { "clue-form-2316" } };
var back23 = JsonSerializer.Deserialize<Level1FieldCheckpoint>(JsonSerializer.Serialize(cp23, json), json);
Check(!back23.Repair() && back23.consultation.clientInRoom && back23.consultation.visits == 6 && back23.consultation.ladderVisits == 2 &&
      back23.consultation.lastLineId == "redirect-folder" && back23.consultation.seenFailedAudits == 1 && back23.consultation.seenDay == 2 &&
      back23.consultation.pendingSpecial == "laterDay" && back23.consultation.givenClueIds.SequenceEqual(new[] { "clue-form-2316" }),
    "checkpoint round trip keeps the relocation, visit counts, last line and reset markers");
var old23 = JsonSerializer.Deserialize<Level1FieldCheckpoint>("{\"version\":1,\"boardPhase\":\"rewarded\"}", json);
old23.Repair();
Check(old23.consultation != null && !old23.consultation.clientInRoom && old23.consultation.visits == 0 && old23.consultation.seenDay == 1 &&
      !new Level1FieldCheckpoint().Repair(), "a checkpoint saved before T23 gets an empty consultation record; a fresh one needs no repair");
var bad23 = new Level1FieldCheckpoint();
bad23.consultation = new Level1ConsultationRecord { visits = -3, ladderVisits = 9, seenFailedAudits = -1, seenDay = 0, lastLineId = null,
    pendingSpecial = "party", givenClueIds = new List<string> { " a ", "a", "", null } };
Check(bad23.Repair() && bad23.consultation.visits == 0 && bad23.consultation.ladderVisits == 0 && bad23.consultation.seenFailedAudits == 0 &&
      bad23.consultation.seenDay == 1 && bad23.consultation.lastLineId == "" && bad23.consultation.pendingSpecial == "" &&
      bad23.consultation.givenClueIds.SequenceEqual(new[] { "a" }), "repair clamps counters, the day and unknown specials, and cleans clue ids");

// ---------- wiring (source contracts; relocation, taps and dialogue are Unity runtime paths) ----------
string consult23 = Src("SideQuest1", "ClientConsultation.cs");
string handover23 = Src("UI", "HUD", "ClientCopyHandover.cs");
string checkpointer23 = Src("SideQuest1", "Level1FieldCheckpointer.cs");
string[] forbidden23 = { "PlayerProgressManager", "CampaignRewards", "Trust", "Coins", "LevelScoreLedger", "OfficeObjectives", "GameplayEvents.Raise",
    "RestoreClock", "Score.", "AddExp", "Award", "CommitFollowThrough", "LevelGate.Mark" };
Check(forbidden23.All(word => consult23.IndexOf(word, StringComparison.Ordinal) < 0),
    "visiting calls no trust, coin, XP, score, objective, clock or story command");
string restore23 = Between(consult23, "public void Restore(", "private static Level1ConsultationRecord Copy(");
Check(!restore23.Contains("SaveCheckpoint") && !restore23.Contains("TryStartVisit") && !restore23.Contains("GameplayEvents") &&
      restore23.Contains("MoveToRoom(immediate: true)") && Between(consult23, "public void MoveToRoom(", "/// <summary>Marie or her spot").Contains("Move(save: false)"),
    "restore puts Marie back quietly: no line, no event, no save");
Check(consult23.Contains("TaxpayerNpcInteractable.VisitHandler = HandleVisit;") && !handover23.Contains("TaxpayerNpcInteractable.VisitHandler =") &&
      consult23.Contains("ClientCopyHandover.Instance.HandleClientVisit(npc)") && handover23.Contains("public bool OwnsClientVisits => phase != ClientCopyPhase.None;") &&
      handover23.Contains("ClientConsultation.Instance.MoveToRoom(immediate)") && !handover23.Contains("SetPositionAndRotation(feet"),
    "one owner: ClientConsultation moves Marie and takes taps, handing them to the Client Copy handover from approval on");
Check(consult23.Contains("LevelGate.SideQuestDone") && consult23.Contains("if (movePending && !Seen()) Move(save: true);"),
    "Marie moves after the side quest, only while neither she nor the spot is seen");
Check(checkpointer23.Contains("checkpoint.consultation = ClientConsultation.Instance.Capture();") &&
      checkpointer23.Contains("ClientConsultation.Instance.Restore(saved.consultation, solved || rewarded);") &&
      Index(checkpointer23, "ClientConsultation.Instance.Restore(") < Index(checkpointer23, "ClientCopyHandover.Instance.Restore("),
    "the checkpointer saves the consultation and restores it before the Client Copy phase");

Console.WriteLine();
Console.WriteLine($"{checks - failures}/{checks} checks passed");
return failures == 0 ? 0 : 1;

/// <summary>Frozen copy of the rules T01 moved into MasteryBoundary (pre-T01 source, 2026-10-21).</summary>
static class PreT01
{
    public static int Percent(int earned, int max) =>
        max <= 0 ? 100 : (int)Math.Floor(100.0 * Math.Max(0, earned) / max);

    public static bool Passed(int passingPercent, int exp, int hearts, int maxExp, int maxHearts) =>
        Percent(exp, maxExp) >= passingPercent && Percent(hearts, maxHearts) >= passingPercent;

    public static bool IsLevelUnlocked(PlayerProgressData d, int levelNumber)
    {
        if (levelNumber == 1) return true;
        if (levelNumber < 1 || d.passedLevelIds == null) return false;
        return d.passedLevelIds.Contains($"Level_{levelNumber - 1:00}");
    }

    public static bool ChapterUnlocked(PlayerProgressData data, string previousId) =>
        previousId == null || (data != null && data.IsLevelPassed(previousId));

    public static (int expGain, int heartsGain, bool newBest) RecordLevelResult(PlayerProgressData d,
        string levelId, int exp, int hearts, int maxExp, int maxHearts, bool passed, IEnumerable<string> badges)
    {
        if (string.IsNullOrWhiteSpace(levelId)) return (0, 0, false);
        d.Normalize();
        string id = levelId.Trim();
        maxExp = Math.Max(0, maxExp);
        maxHearts = Math.Max(0, maxHearts);
        exp = Math.Max(0, maxExp > 0 ? Math.Min(exp, maxExp) : exp);
        hearts = Math.Max(0, maxHearts > 0 ? Math.Min(hearts, maxHearts) : hearts);
        if (!d.completedLevelIds.Contains(id)) d.completedLevelIds.Add(id);
        d.levelsCompleted = d.completedLevelIds.Count;
        if (passed && !d.passedLevelIds.Contains(id)) d.passedLevelIds.Add(id);
        LevelRecord record = d.GetRecord(id);
        bool first = record == null;
        if (first) { record = new LevelRecord { levelId = id }; d.levelRecords.Add(record); }
        int expGain = Math.Max(0, exp - record.bestExp);
        int heartsGain = Math.Max(0, hearts - record.bestHearts);
        bool newBest = first || exp > record.bestExp || hearts > record.bestHearts;
        record.bestExp = Math.Max(record.bestExp, Math.Max(0, exp));
        record.bestHearts = Math.Max(record.bestHearts, Math.Max(0, hearts));
        record.maxExp = maxExp;
        record.maxHearts = maxHearts;
        record.passed = record.passed || passed;
        record.attempts++;
        if (record.badges == null) record.badges = new List<string>();
        if (badges != null)
            foreach (string badge in badges)
                if (!string.IsNullOrWhiteSpace(badge) && !record.badges.Contains(badge)) record.badges.Add(badge);
        d.totalExp += expGain;
        d.totalReputation += heartsGain;
        return (expGain, heartsGain, newBest);
    }

    /// <summary>The record part of PlayerProgressManager.ApplyLevelResult previousFiles T01.</summary>
    public static (int, int, bool) ApplyLevelResult(PlayerProgressData d, MasteryRun t)
    {
        if (string.IsNullOrWhiteSpace(t.LevelId)) return (0, 0, false);
        if (t.HasScore)
        {
            var gain = RecordLevelResult(d, t.LevelId, t.Exp, t.Hearts, t.MaxExp, t.MaxHearts, t.Passed, t.Badges);
            for (int i = 0; i < t.Badges.Count && i < t.BadgeTiers.Count; i++)
                d.RecordBadge(t.Badges[i], t.BadgeTiers[i]);
            d.RecordLevelTime(t.LevelId, t.RunSeconds, t.SideQuestSeconds);
            return gain;
        }
        d.RecordLevelCompletion(t.LevelId);
        RecordLevelResult(d, t.LevelId, t.Exp, t.Hearts, 0, 0, true, t.Badges);
        return (0, 0, false);
    }
}
