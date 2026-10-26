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
Check(!CampaignMenu.ShopUnlocked(story) && !CampaignMenu.ShopUnlocked(level1Done) && CampaignMenu.ShopLockedMessage.Contains("Supply Counter"),
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
string acknowledge4 = Between(mgr4, "public bool CommitLevelOneWrapUp()", "public CaseResultRules.ChapterSummary LevelOneSummary()");
string completeWrap4 = Body(handoverRules4, "public static bool CompleteWrapUp(");
Check(handoverRules4.Contains("candidate.campaign.levelOne.handoverPhase = Level1FieldCheckpoint.HandoverDone;") &&
      handoverRules4.Contains("candidate.campaign.levelOneEndFlowPending = true;") &&
      Index(completeFollow4, "if (candidate.campaign.levelOne == null) candidate.campaign.levelOne = new Level1FieldCheckpoint();") <
          Index(completeFollow4, "candidate.campaign.levelOne.handoverPhase = Level1FieldCheckpoint.HandoverDone;") &&
      !Between(handoverRules4, "CompleteFollowThrough(PlayerProgressData candidate)", "public static bool WrapUpPending").Contains("MarkChapterCompleted") &&
      acknowledge4.Contains("PlayerProgressData candidate = CloneProgress();") &&
      Index(acknowledge4, "CaseResultRules.CompleteWrapUp(candidate)") < Index(acknowledge4, "TryWriteCandidate(candidate)") &&
      completeWrap4.Contains("candidate.campaign.MarkChapterCompleted(CampaignRecord.FirstChapterId);") &&
      completeWrap4.Contains("candidate.campaign.levelOneEndFlowPending = false;") &&
      completeWrap4.Contains("candidate.campaign.levelOne = new Level1FieldCheckpoint();") &&
      acknowledge4.Contains("return saved;"),
    "follow-through saves Done and pending end flow (not the chapter); the T16 wrap-up completes Chapter 1 and clears both in one candidate write");
string writeCandidate4 = Between(mgr4, "private bool TryWriteCandidate(", "public bool UsesLevelOneCheckpoint");
Check(acknowledge4.Contains("CaseResultRules.CompleteWrapUp(candidate)") &&
      !acknowledge4.Contains("progress.campaign.levelOneEndFlowPending =") &&
      !acknowledge4.Contains("progress.campaign.levelOne =") &&
      Index(writeCandidate4, "store.Write(Encode(candidate));") < Index(writeCandidate4, "progress = candidate;") &&
      Between(writeCandidate4, "catch (Exception e)", "progress = candidate;").Contains("return false;"),
    "a failed wrap-up write leaves the live saved Done phase, pending marker and uncompleted chapter intact for retry");
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
    bool bridged = CaseResultRules.CompleteFollowThrough(d).Applied && !d.campaign.IsChapterCompleted("Level_01") && d.campaign.levelOne.IsSet &&
                   d.campaign.levelOne.handoverPhase == Level1FieldCheckpoint.HandoverDone && d.campaign.levelOneEndFlowPending &&
                   d.campaign.levelOneCaseResult.recorded && d.campaign.levelOneCaseResult.firstCheckCorrect == correct &&
                   CaseResultRules.WrapUpPending(d);
    bool wrapped = CaseResultRules.CompleteWrapUp(d) && d.campaign.IsChapterCompleted("Level_01") && !d.campaign.levelOneEndFlowPending &&
                   !d.campaign.levelOne.IsSet && !CaseResultRules.CompleteWrapUp(d) &&
                   CampaignRewards.IsActivityCompleted(d, "Level_01", CaseResultRules.WrapUpActivity) && d.campaign.levelOneCaseResult.firstCheckCorrect == correct;
    allBridge &= pending && bridged && wrapped && CaseResultRules.CompleteFollowThrough(d).Outcome == CampaignRewards.Outcome.Rejected && d.campaign.publicTrust == 44;
}
Check(allBridge && nextSteps.Count == 1, "every tier waits for one +4 handover (chapter not yet complete), then the wrap-up completes Chapter 1 once and keeps the first result");
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
      ClientCopyRules.PhaseOf(doneResume) == ClientCopyPhase.Done && !doneResume.campaign.IsChapterCompleted(CampaignRewards.Level1) &&
      CampaignRewards.IsActivityCompleted(doneResume, CampaignRewards.Level1, CampaignRewards.FollowThrough) &&
      doneResume.campaign.levelOne.IsSet && doneResume.campaign.levelOne.handoverPhase == Level1FieldCheckpoint.HandoverDone &&
      doneResume.campaign.levelOneEndFlowPending,
    "the saved handover completes once, keeps a durable Done checkpoint and pending end flow (Chapter 1 waits for its wrap-up), and preserves 46 trust / 160 coins");
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
      !story8.campaign.IsChapterCompleted("Level_01") && !CaseResultRules.FollowThroughPending(story8) &&
      CampaignRewards.IsActivityCompleted(story8, "Level_01", CampaignRewards.FollowThrough) &&
      story8.campaign.levelOne.IsSet && story8.campaign.levelOne.handoverPhase == Level1FieldCheckpoint.HandoverDone &&
      story8.campaign.levelOneEndFlowPending,
    "Marie's saved handover gives trust 46/100 and keeps Chapter 1's finale/card flow resumable until Continue");
// ---------- T16: the Chapter 1 wrap-up ----------
var earlyWrap16 = Clone(story8);
earlyWrap16.campaign.levelOneEndFlowPending = false;
Check(!CaseResultRules.CompleteWrapUp(earlyWrap16) && !earlyWrap16.campaign.IsChapterCompleted("Level_01"),
    "no wrap-up without the pending end flow (a result-card click or shop visit alone completes nothing)");
var noFollow16 = PlayerProgressData.CreateNewCampaign("t16-early", "");
noFollow16.campaign.levelOneEndFlowPending = true;
Check(!CaseResultRules.WrapUpPending(noFollow16) && !CaseResultRules.CompleteWrapUp(noFollow16) && !noFollow16.campaign.IsChapterCompleted("Level_01"),
    "no wrap-up before Marie's follow-through, even with a stray end-flow marker");
CaseResultRules.ChapterSummary summary16 = CaseResultRules.BuildChapterSummary(story8);
bool wrapped16 = CaseResultRules.CompleteWrapUp(story8);
Check(wrapped16 && story8.campaign.IsChapterCompleted("Level_01") && !story8.campaign.levelOneEndFlowPending && !story8.campaign.levelOne.IsSet &&
      story8.campaign.coins == 160 && story8.campaign.publicTrust == 46 && !CaseResultRules.CompleteWrapUp(story8),
    "the wrap-up completes Chapter 1 once, ends the end flow and checkpoint, and changes no coins or trust (160 / 46)");
Check(summary16.CoinsEarned == 160 && summary16.Coins.Count == 2 && summary16.Coins[0].Amount == 60 && summary16.Coins[1].Amount == 100 &&
      summary16.TrustEarned == 6 && summary16.Trust.Count == 2 && summary16.TrustNow == 46 && summary16.Wallet == 160 &&
      summary16.TrustTarget == 65 && summary16.FirstCheckCorrect == story8.campaign.levelOneCaseResult.firstCheckCorrect,
    "the wrap-up sheet reads the committed ledger: coins +60 and +100 (160), trust +2 and +4 (46 / 100, target 65), the first check");
string storyAfter = Snap(story8);
CaseResultRules.Apply(story8, CaseResultRules.Build("Level_01", story8.campaign.levelOne, "case_marie", null, true, 150, 150, 12, 12, 1f), CaseRun(150));
Check(CaseResultRules.CompleteFollowThrough(story8).Outcome == CampaignRewards.Outcome.Rejected && Snap(story8) == storyAfter,
    "repeating the result or the follow-through (reload, replay) adds no trust or coins");
var storyBack = Clone(story8);
Check(storyBack.campaign.ledger.Count == 4 && storyBack.campaign.completedActivityIds.Count == 4 && !storyBack.Normalize(),
    "trust, coins, claims, reasons and the four Level 1 activities (with the T16 wrap-up) survive the save");

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
      fieldRow8.Contains("private bool Locked => ReadOnly || Doc3DLayer.BlocksValueTransfer(transform);") &&
      Between(fieldRow8, "public void OnPointerClick(", "public void OnDrop(").Contains("if (Locked) return;") &&
      Between(fieldRow8, "public void OnDrop(", "public DataValue GetDataValue()").Contains("if (Locked) return;") &&
      Between(fieldRow8, "public bool CanAccept(", "public bool TryReceiveValue(").Contains("if (Locked || !isDestination)") &&
      Between(fieldRow8, "public bool WouldAccept(", "public bool CanAccept(").Contains("return !Locked"),
    "read-only client fields (and the folder away from the desk) reject clicks and drops that would edit or transfer values");
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
Check(onReturn8.Contains("manager.CommitLevelOneWrapUp()") && !levelUi.Contains("ChapterWrapUpView") &&
      !File.Exists(Path.Combine(root, "Assets", "Scripts", "UI", "HUD", "ChapterWrapUpView.cs")) &&
      levelUi.Contains("if (delivered) return BuildChapterScreen(result, career, caseResult, rules);") &&
      Between(levelUi, "private static RewardScreenData BuildChapterScreen(", "private static RewardScreenData BuildScreen(").Contains("Large = true") &&
      !levelUi.Contains("FollowThroughView") &&
      !File.Exists(Path.Combine(root, "Assets", "Scripts", "UI", "HUD", "FollowThroughView.cs")) &&
      !Src("RewardsSystem", "CampaignRewards.cs").Contains("class FollowThroughScript"),
    "after the handover the case card is the Chapter 1 card (Large, coins, trust, first check, time); its Continue commits the wrap-up, then the career board (no separate sheet); the replaced follow-through card and script are removed");
string screen16 = Src("UI", "HUD", "HudRewardScreen.cs");
Check(screen16.Contains("public bool Large;") && screen16.Contains("sideLayout = shownLarge || PreferSideLayout || finaleCard;") &&
      screen16.Contains("float share = shownLarge ? LargeSideShare : 0.5f;") && screen16.Contains("public const float LargeSideShare = 0.6f;") &&
      screen16.Contains("shownLarge || (shownTitle"),
    "the Chapter 1 card sits on the right 60% over the client scene, may grow within it, and waits for the finale scene");
Check(Src("CaseFolder", "CaseFolder3DDrag.cs").Contains("if (FolderToHUDIcon.AnyStoredOrViewing() ||") &&
      Src("UI", "HUD", "CaseFolder3DView.cs").Contains("folder.gameObject.SetActive(store == null && savedActive);") &&
      Src("UI", "HUD", "CaseFolder3DView.cs").Contains("if (phase == Phase.Opening || phase == Phase.Open) Close();") &&
      Src("Level2", "Level2TaxCodeBookIcon.cs").Contains("(phase == Phase.Opening || phase == Phase.Open))"),
    "exit desk: a folder or book that belongs to the bar flies back into its icon; only a desk-opened one returns to the stack (never both)");
Check(Src("TaxCodeBook", "TaxCodeBookFloatingModel3D.cs").Contains("DeskDocumentAnimator.CompleteAll(taxCodeBook);") &&
      Src("TaxCodeBook", "TaxCodeBookFloatingModel3D.cs").Contains("originalRotation = taxCodeBook.rotation;") &&
      Src("CaseFolder", "FloatingModel3D.cs").Contains("DeskDocumentAnimator.CompleteAll(model);"),
    "a grabbed open book/folder keeps its current (upright) pose and its open motion lands first, so it stays under the finger");
string doc27 = Src("UI", "HUD", "Doc3DLayer.cs");
Check(doc27.Contains("public bool ShowsAt(Vector2 screen, out bool known)") && !doc27.Contains("BringTouchedToFront") &&
      Src("UI", "HUD", "HudWindowFocus.cs").Contains("bool shown = layer.ShowsAt(screenPosition, out bool known);"),
    "whichever desk document is drawn under the touch comes to the front (folder over book or book over folder), from one focus system");
Check(Src("Level2", "Level2TaxCodeBookIcon.cs").Contains("pinPosition = grabPin + ScreenToLocal(main, held.Clamp(screen), grabDepth) - ScreenToLocal(main, grabScreen, grabDepth);") &&
      Src("TaxCodeBook", "TaxCodeBookFloatingModel3D.cs").Contains("page != null ? page.TransformPoint(page.rect.center) : taxCodeBook.position"),
    "the Tax Code Book (desk and floating) moves exactly with the finger at its page's depth and stops at the screen edge");
string drag27 = Src("CaseFolder", "CaseFolder3DDrag.cs");
Check(drag27.Contains("folderToHUDIcon = openFolder.GetComponent<FolderToHUDIcon>();") && drag27.Contains("StartCoroutine(FlyClosedIntoBar(eventData.position))") &&
      Src("Interactables", "DeskDocumentAnimator.cs").Contains("public void PlayDrop(") && Src("CaseFolder", "OpenFolderReturnToDesk.cs").Contains("deskAnimator.PlayDrop(originalClosedFolder.transform);"),
    "the closed folder dropped on the bar flies into its slot (never opens); a folder put back on the stack drops onto it");
Check(doc27.Contains("public static bool BlocksValueTransfer(Transform t)") && Src("Data", "DragDrop", "ValueTransferManager.cs").Contains("Doc3DLayer.BlocksValueTransfer(held.transform)") &&
      Src("Data", "DragDrop", "ValueDragSource.cs").Contains("if (Doc3DLayer.BlocksValueTransfer(transform))") && Src("Data", "DragDrop", "CaseFolderFieldRow.cs").Contains("private bool Locked => ReadOnly || Doc3DLayer.BlocksValueTransfer(transform);"),
    "away from the desk the open folder and book are read-only: no value is held, dragged or placed");
string cam27 = Src("Player", "CameraController.cs");
Check(cam27.Contains("public bool InputHeld => activeTransition != null || Time.unscaledTime < inputHeldUntil;") &&
      cam27.Contains("canvas.sortingOrder = 32000;") && !Between(cam27, "CurrentMode = CameraMode.Workstation;", "activeTransition = StartCoroutine(").Contains("workstationInteractor.enabled = true;") &&
      cam27.Contains("if (CurrentMode == CameraMode.Workstation && workstationInteractor != null)") &&
      Src("Mobile", "MobileInputGuard.cs").Contains("CameraController.Instance.InputHeld"),
    "nothing can be tapped while the camera moves into the desk (or any view); desk taps start only once it arrived (2026-10-26)");
Check(Src("UI", "HUD", "OfficeHudRuntime.cs").Contains("AddComponent<HudSortingKeeper>().Order = ExitDeskSortingOrder;") && Src("UI", "HUD", "OfficeHudRuntime.cs").Contains("public const int ExitDeskSortingOrder = 2400;") &&
      Src("UI", "HUD", "HudSortingKeeper.cs").Contains("if (!own.overrideSorting) own.overrideSorting = true;"),
    "EXIT DESK draws above the open desk documents (their layers sort 1101-1900)");
Check(!Src("UI", "HUD", "HeldOnScreen.cs").Contains("slack") && Src("TaxCodeBook", "TaxCodeBookFloatingModel3D.cs").Contains("// The open book sits exactly under the finger") &&
      Src("CaseFolder", "FloatingModel3D.cs").Contains("// The open folder sits exactly under the finger"),
    "open documents follow the finger exactly while dragged; they only stop at the screen edge");
Check(Src("UI", "HUD", "NotesPaperView.cs").Contains("bool chapters = manager != null;") && Src("UI", "HUD", "NotesPaperView.cs").Contains("const float size = 46f;") &&
      !Src("UI", "HUD", "ClientCopyHandover.cs").Contains("WorldEmote.Hold(clientRoot"),
    "Notes page by heading in every level with bigger text; Marie shows no icon while she waits");
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

// =====================================================================
// T10: hotbar, upgradable bag, Desk Drawer and evidence journal
// =====================================================================
InventoryCatalogue cat10 = InventoryCatalogue.BuiltIn();
string Inv10(CampaignInventoryRecord r) => JsonSerializer.Serialize(r, json);
CampaignInventoryRecord Fresh10() { var r = new CampaignInventoryRecord(); r.Repair(); return r; }
string Act10(string what) => what + "/" + Guid.NewGuid().ToString("N");
InventoryCatalogue Stack10()
{
    InventoryCatalogue c = InventoryCatalogue.BuiltIn();
    c.items.Add(new InventoryItemDefinition { id = "voucher", label = "Review Voucher", category = InventoryCatalogue.ToolsAndSupplies, icon = "papers", stackLimit = 3 });
    c.items.Add(new InventoryItemDefinition { id = "postcard", label = "Postcard", category = InventoryCatalogue.Keepsakes, icon = "papers" });
    c.items.Add(new InventoryItemDefinition { id = "pencil", label = "Pencil", category = InventoryCatalogue.ToolsAndSupplies, icon = "papers" });
    return c;
}

// ---------- record, capacities and save round trip ----------
var fresh10 = new CampaignInventoryRecord();
Check(fresh10.hotbarCapacity == 3 && fresh10.bagCapacity == 5 && fresh10.hotbar.Count == 3 && fresh10.bag.Count == 5 &&
      fresh10.hotbar.All(s => s.IsEmpty) && fresh10.bag.All(s => s.IsEmpty) && fresh10.hand.IsEmpty && fresh10.drawer.Count == 0 && !fresh10.Repair(),
    "a fresh inventory has 3 empty bar slots, 5 empty bag slots, an empty drawer and needs no repair");
PlayerProgressData camp10 = PlayerProgressData.CreateNewCampaign("t10", "");
Check(camp10.campaign.inventory != null && camp10.campaign.inventory.hotbarCapacity == 3 && !camp10.Normalize(),
    "New Game starts with the empty starting inventory inside the v5 campaign record");
var clamp10 = new CampaignInventoryRecord { hotbarCapacity = 9, bagCapacity = 40 };
var low10 = new CampaignInventoryRecord { hotbarCapacity = 1, bagCapacity = 0 };
Check(clamp10.Repair() && clamp10.hotbarCapacity == 5 && clamp10.bagCapacity == 12 && clamp10.hotbar.Count == 5 && clamp10.bag.Count == 12 &&
      low10.Repair() && low10.hotbarCapacity == 3 && low10.bagCapacity == 5 && low10.hotbar.Count == 3 && low10.bag.Count == 5,
    "capacities clamp to 3-5 (bar) and 5-12 (bag) and the slot lists follow");
var over10 = new CampaignInventoryRecord();
for (int i = 0; i < 5; i++) over10.bag[i] = new InventorySlot { itemId = "pencil", quantity = 1 };
over10.hotbar[0] = new InventorySlot { itemId = InventoryCatalogue.Notes, quantity = 1 };
over10.hotbar.Add(new InventorySlot { itemId = "postcard", quantity = 1 });
over10.bag.Add(new InventorySlot { itemId = "voucher", quantity = 2 });
over10.Repair();
Check(over10.hotbar.Count == 3 && over10.bag.Count == 5 && over10.hotbar.Count(s => s.itemId == "postcard" || s.itemId == "voucher") == 2 &&
      over10.hotbar[0].itemId == InventoryCatalogue.Notes && over10.drawer.Count == 0 && clampSpill10(),
    "items in slots beyond capacity move to a free slot (or the record's overflow list when none is free); nothing is lost");
var dirty10 = new CampaignInventoryRecord
{
    hotbar = null, hand = null, drawer = new List<InventoryDrawerEntry> { new InventoryDrawerEntry { itemId = " pencil ", quantity = 2 }, new InventoryDrawerEntry { itemId = "pencil", quantity = 3 }, null, new InventoryDrawerEntry { itemId = "x", quantity = 0 } },
    items = new List<InventoryItemRecord> { new InventoryItemRecord { itemId = "notes" }, new InventoryItemRecord { itemId = "notes" }, null },
    selectedSlot = null, journalSeenFactIds = new List<string> { "a", " a ", "" }
};
dirty10.bag[0] = new InventorySlot { itemId = "notes", quantity = -4 };
for (int i = 0; i < 80; i++) dirty10.appliedActionIds.Add("a" + i);
Check(dirty10.Repair() && dirty10.hotbar.Count == 3 && dirty10.hand.IsEmpty && dirty10.drawer.Count == 1 && dirty10.drawer[0].quantity == 5 &&
      dirty10.items.Count == 1 && dirty10.bag[0].IsEmpty && dirty10.selectedSlot == "" && dirty10.journalSeenFactIds.SequenceEqual(new[] { "a" }) &&
      dirty10.appliedActionIds.Count == CampaignInventoryRecord.MaxActionIds && dirty10.appliedActionIds[0] == "a16",
    "repair: null lists, negative quantities, one overflow entry and one record per item, clean ids, last 64 action ids");

var save10 = PlayerProgressData.CreateNewCampaign("t10-save", "");
CampaignInventoryRecord inv10 = save10.campaign.inventory;
InventoryRules.Acquire(inv10, cat10, InventoryCatalogue.ProfileBinder, 1, "a1", null, new InventoryItemRecord { source = "plaza", contents = new List<string> { "profile_marie" } });
InventoryRules.Acquire(inv10, cat10, InventoryCatalogue.Notes, 1, "a2");
InventoryRules.Acquire(inv10, cat10, InventoryCatalogue.CaseFolder, 1, "a3", null, new InventoryItemRecord { originalDocumentId = "case_001/original", workingDocumentId = "case_001/working", revision = 2 });
InventoryRules.Acquire(inv10, cat10, InventoryCatalogue.TaxCodeBook, 1, "a4");
InventoryRules.UpgradeCapacity(inv10, InventoryArea.Bag, 8, "u1");
InventoryRules.MarkJournalSeen(inv10, new[] { "met-npc_marie" });
InventoryRules.SetSelected(inv10, SlotRef.Hotbar(2));
inv10.bagIconUsed = true;
PlayerProgressData back10 = Clone(save10);
CampaignInventoryRecord backInv10 = back10.campaign.inventory;
Check(!back10.Normalize() && backInv10.bagCapacity == 8 && backInv10.bag.Count == 8 && backInv10.hotbar[0].itemId == InventoryCatalogue.ProfileBinder &&
      backInv10.hotbar[2].itemId == InventoryCatalogue.CaseFolder && backInv10.bag[0].itemId == InventoryCatalogue.TaxCodeBook &&
      backInv10.selectedSlot == "hotbar:2" && backInv10.journalSeenFactIds.Contains("met-npc_marie") && backInv10.bagIconUsed &&
      InventoryRules.Record(backInv10, InventoryCatalogue.CaseFolder).originalDocumentId == "case_001/original" &&
      InventoryRules.Record(backInv10, InventoryCatalogue.CaseFolder).workingDocumentId == "case_001/working" &&
      InventoryRules.Record(backInv10, InventoryCatalogue.CaseFolder).revision == 2 &&
      InventoryRules.Record(backInv10, InventoryCatalogue.ProfileBinder).source == "plaza",
    "slot layout, capacities, journal state, selected slot and original/working identity round-trip through the save");
var old10 = JsonSerializer.Deserialize<PlayerProgressData>("{\"saveVersion\":5,\"campaign\":{\"campaignId\":\"old\",\"currentChapterId\":\"Level_01\",\"publicTrust\":40}}", json);
old10.Normalize();
Check(old10.campaign.inventory != null && old10.campaign.inventory.hotbarCapacity == 3 && old10.campaign.inventory.bag.Count == 5,
    "a v5 save written before T10 loads with the empty starting inventory");

// ---------- catalogue and content rule ----------
Check(cat10.Problems().Count == 0 && cat10.items.Count(i => i.scope == "Level_01") == 4 && cat10.IsRequired(InventoryCatalogue.Notes) && cat10.IsRequired(InventoryCatalogue.CaseFolder) &&
      cat10.IsRequired(InventoryCatalogue.ProfileBinder) && !cat10.IsRequired(InventoryCatalogue.TaxCodeBook) &&
      cat10.Find(InventoryCatalogue.ProfileBinder).container && cat10.Find(InventoryCatalogue.CaseFolder).container &&
      cat10.Find(InventoryCatalogue.TaxCodeBook).returnsToWorld && cat10.items.Where(i => i.required).All(i => i.scope == "Level_01"),
    "built-in catalogue: binder and folder are required containers, Notes required, the book optional and returnable");
var file10 = JsonSerializer.Deserialize<InventoryCatalogue>(File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Campaign", "inventory.json")), json);
bool sameCatalogue10 = file10 != null && file10.items.Count == cat10.items.Count && file10.journal.Count == cat10.journal.Count &&
    file10.items.All(f => { var b = cat10.Find(f.id); return b != null && b.label == f.label && b.category == f.category && b.icon == f.icon && b.required == f.required &&
        b.stackLimit == f.stackLimit && b.container == f.container && b.scope == f.scope && b.returnsToWorld == f.returnsToWorld && b.carryLimit == f.carryLimit && b.description == f.description; }) &&
    file10.journal.All(f => cat10.journal.Any(b => b.id == f.id && b.text == f.text && b.sourceItemId == f.sourceItemId));
Check(sameCatalogue10 && file10.Problems().Count == 0, "Resources/Campaign/inventory.json matches the built-in fallback and has no problems");
var bad10 = new InventoryCatalogue();
bad10.items.Add(new InventoryItemDefinition { id = "a", label = "A", category = "Snacks" });
bad10.items.Add(new InventoryItemDefinition { id = "a", label = "", category = InventoryCatalogue.Keepsakes, stackLimit = 0 });
bad10.items.Add(new InventoryItemDefinition { id = "box", label = "Box", category = InventoryCatalogue.CaseFiles, container = true, stackLimit = 3 });
bad10.journal.Add(new InventoryJournalFact { id = "f", text = "x", sourceItemId = "ghost" });
List<string> badProblems10 = bad10.Problems();
Check(badProblems10.Any(p => p.Contains("unknown category")) && badProblems10.Any(p => p.Contains("duplicate item")) && badProblems10.Any(p => p.Contains("no label")) &&
      badProblems10.Any(p => p.Contains("stack limit 0")) && badProblems10.Any(p => p.Contains("cannot stack")) && badProblems10.Any(p => p.Contains("unknown source")),
    "catalogue problems are reported (category, duplicate, label, stack limit, stacking container, journal source)");
var heavy10 = InventoryCatalogue.BuiltIn();
for (int i = 0; i < 6; i++) heavy10.items.Add(new InventoryItemDefinition { id = "req" + i, label = "R", category = InventoryCatalogue.CaseFiles, required = true, scope = "Level_01" });
Check(InventoryRules.RequiredCapacityProblems(cat10, "Level_01").Count == 0 && InventoryRules.RequiredCapacityProblems(heavy10, "Level_01").Count == 1 &&
      InventoryRules.RequiredCapacityProblems(heavy10, "Level_02").Count == 0,
    "content rule: Level 1's required items (3) fit the starting 3 + 5 slots; 9 required items in one level are refused");

// ---------- acquire: auto-placement, drop to slot, full pickup ----------
var acq10 = Fresh10();
InventoryResult binder10 = InventoryRules.Acquire(acq10, cat10, InventoryCatalogue.ProfileBinder, 1, "b1", null, new InventoryItemRecord { source = "plaza" });
InventoryResult notes10 = InventoryRules.Acquire(acq10, cat10, InventoryCatalogue.Notes, 1, "b2");
InventoryResult folder10 = InventoryRules.Acquire(acq10, cat10, InventoryCatalogue.CaseFolder, 1, "b3");
InventoryResult book10 = InventoryRules.Acquire(acq10, cat10, InventoryCatalogue.TaxCodeBook, 1, "b4");
Check(binder10.Location == SlotRef.Hotbar(0) && notes10.Location == SlotRef.Hotbar(1) && folder10.Location == SlotRef.Hotbar(2) &&
      book10.Location == SlotRef.Bag(0) && book10.WentToBagBecauseBarFull && !folder10.WentToBagBecauseBarFull,
    "auto-placement: first empty bar slot, then the bag; the fourth Level 1 item lands in the bag (the bag lesson moment)");
string before10 = Inv10(acq10);
Check(InventoryRules.Acquire(acq10, cat10, InventoryCatalogue.Notes, 1, "b5").Outcome == InventoryOutcome.NoChange &&
      InventoryRules.Acquire(acq10, cat10, InventoryCatalogue.TaxCodeBook, 1, "b4").Outcome == InventoryOutcome.AlreadyApplied &&
      InventoryRules.Acquire(acq10, cat10, "ghost", 1, "b6").Outcome == InventoryOutcome.Rejected &&
      InventoryRules.Acquire(acq10, cat10, InventoryCatalogue.Notes, 0, "b7").Outcome == InventoryOutcome.Rejected && Inv10(acq10) == before10,
    "owned once, idempotent per action id, unknown or empty acquisitions refused, and none of them change anything");

var drop10 = Fresh10();
InventoryRules.Acquire(drop10, cat10, InventoryCatalogue.Notes, 1, "d1");
InventoryResult dropEmpty10 = InventoryRules.Acquire(drop10, cat10, InventoryCatalogue.CaseFolder, 1, "d2", SlotRef.Hotbar(2));
InventoryResult dropSwap10 = InventoryRules.Acquire(drop10, cat10, InventoryCatalogue.TaxCodeBook, 1, "d3", SlotRef.Hotbar(0));
Check(dropEmpty10.Location == SlotRef.Hotbar(2) && dropSwap10.Location == SlotRef.Hotbar(0) && dropSwap10.DisplacedItemId == InventoryCatalogue.Notes &&
      dropSwap10.DisplacedTo == SlotRef.Bag(0) && dropSwap10.WentToBagBecauseBarFull && drop10.hotbar[0].itemId == InventoryCatalogue.TaxCodeBook &&
      drop10.hotbar[1].IsEmpty && drop10.bag[0].itemId == InventoryCatalogue.Notes,
    "a drop on an empty slot places the item there; a drop on an occupied slot swaps and the old item goes to the bag");

var full10 = Fresh10();
var stack10 = Stack10();
InventoryRules.Acquire(full10, stack10, InventoryCatalogue.Notes, 1, "f1");
InventoryRules.Acquire(full10, stack10, InventoryCatalogue.CaseFolder, 1, "f2");
InventoryRules.Acquire(full10, stack10, InventoryCatalogue.ProfileBinder, 1, "f3");
for (int i = 0; i < 5; i++) full10.bag[i] = new InventorySlot { itemId = "pencil", quantity = 1 };
InventoryResult swapFull10 = InventoryRules.Acquire(Clone10(full10), stack10, InventoryCatalogue.TaxCodeBook, 1, "f4", SlotRef.Hotbar(1));
InventoryResult handPick10 = InventoryRules.Acquire(full10, stack10, InventoryCatalogue.TaxCodeBook, 1, "f5");
string fullSnap10 = Inv10(full10);
InventoryResult busy10 = InventoryRules.Acquire(full10, stack10, "postcard", 1, "f6");
Check(handPick10.Outcome == InventoryOutcome.NeedsRoom && full10.hand.itemId == InventoryCatalogue.TaxCodeBook && full10.handSource == InventoryRules.HandSourcePickup &&
      swapFull10.Outcome == InventoryOutcome.NeedsRoom && swapFull10.DisplacedTo == SlotRef.Hand &&
      busy10.Outcome == InventoryOutcome.Rejected && Inv10(full10) == fullSnap10,
    "full pickup: the item waits in the hand (Make room); a swap with a full bag puts the old item in the hand; a second pickup waits (unchanged)");
bool clampSpill10()
{
    // With every slot taken, items beyond capacity wait in the record's overflow list (save repair only).
    var spill = new CampaignInventoryRecord();
    for (int i = 0; i < 3; i++) spill.hotbar[i] = new InventorySlot { itemId = "pencil", quantity = 1 };
    for (int i = 0; i < 5; i++) spill.bag[i] = new InventorySlot { itemId = "pencil", quantity = 1 };
    spill.hotbar.Add(new InventorySlot { itemId = "postcard", quantity = 1 });
    spill.bag.Add(new InventorySlot { itemId = "voucher", quantity = 2 });
    spill.Repair();
    return spill.hotbar.Count == 3 && spill.bag.Count == 5 && InventoryRules.DrawerCount(spill, "postcard") == 1 && InventoryRules.DrawerCount(spill, "voucher") == 2;
}
CampaignInventoryRecord Clone10(CampaignInventoryRecord r) => JsonSerializer.Deserialize<CampaignInventoryRecord>(Inv10(r), json);

// ---------- move, swap, stacks, quick move ----------
var mv10 = Clone10(acq10);
string mvBefore10 = Inv10(mv10);
Check(InventoryRules.Move(mv10, cat10, SlotRef.Bag(3), SlotRef.Bag(1), "m0").Outcome == InventoryOutcome.Rejected &&
      InventoryRules.Move(mv10, cat10, SlotRef.Hotbar(0), SlotRef.Hotbar(3), "m1").Message == InventoryRules.ShopUpgrade &&
      InventoryRules.Move(mv10, cat10, SlotRef.Hotbar(0), SlotRef.Bag(5), "m2").Message == InventoryRules.ShopUpgrade &&
      InventoryRules.Move(mv10, cat10, SlotRef.Hotbar(0), SlotRef.Hotbar(0), "m3").Outcome == InventoryOutcome.NoChange && Inv10(mv10) == mvBefore10,
    "moving from an empty slot or into a locked slot (Shop upgrade) is refused; a move onto itself changes nothing");
InventoryResult toBar10 = InventoryRules.Move(mv10, cat10, SlotRef.Bag(0), SlotRef.Hotbar(1), "m4");
Check(toBar10.Changed && toBar10.MovedBagToHotbar && toBar10.DisplacedItemId == InventoryCatalogue.Notes && mv10.hotbar[1].itemId == InventoryCatalogue.TaxCodeBook &&
      mv10.bag[0].itemId == InventoryCatalogue.Notes,
    "tap-tap / drag onto an occupied slot swaps the two items (bag to bar completes the bag lesson)");
InventoryResult toBag10 = InventoryRules.Move(mv10, cat10, SlotRef.Hotbar(1), SlotRef.Bag(0), "m5");
Check(toBag10.Changed && toBag10.MovedBagToHotbar && mv10.hotbar[1].itemId == InventoryCatalogue.Notes,
    "swapping a bar item with a bag item also brings the bag item into the bar");
Check(InventoryRules.Move(mv10, cat10, SlotRef.Bag(0), SlotRef.Bag(4), "m5").Outcome == InventoryOutcome.AlreadyApplied && mv10.bag[0].itemId == InventoryCatalogue.TaxCodeBook,
    "a repeated move action id is a no-op (double taps and retries apply once)");

var st10 = Fresh10();
st10.bag[0] = new InventorySlot { itemId = "voucher", quantity = 2 };
st10.bag[1] = new InventorySlot { itemId = "voucher", quantity = 2 };
InventoryRules.Move(st10, stack10, SlotRef.Bag(1), SlotRef.Bag(0), "s1");
Check(st10.bag[0].quantity == 3 && st10.bag[1].quantity == 1 && InventoryRules.Move(st10, stack10, SlotRef.Bag(1), SlotRef.Bag(0), "s2").Message == "That stack is full",
    "stacks merge up to their limit (3) and the rest stays; a full stack refuses more");
var autoStack10 = Fresh10();
autoStack10.hotbar[0] = new InventorySlot { itemId = "voucher", quantity = 1 };
InventoryResult nextVoucher10 = InventoryRules.Acquire(autoStack10, stack10, "voucher", 1, "v1");
for (int i = 1; i < 3; i++) autoStack10.hotbar[i] = new InventorySlot { itemId = "pencil", quantity = 1 };
for (int i = 0; i < 5; i++) if (autoStack10.bag[i].IsEmpty) autoStack10.bag[i] = new InventorySlot { itemId = "postcard", quantity = 1 };
InventoryResult mergeVoucher10 = InventoryRules.Acquire(autoStack10, stack10, "voucher", 1, "v2");
Check(nextVoucher10.Location == SlotRef.Hotbar(1) && mergeVoucher10.Location == SlotRef.Hotbar(0) && autoStack10.hotbar[0].quantity == 2,
    "ticket order for supplies: an empty bar slot, then an empty bag slot, then an existing stack with room");
var qm10 = Clone10(acq10);
InventoryResult quickDown10 = InventoryRules.QuickMove(qm10, cat10, SlotRef.Hotbar(1), "q1");
InventoryResult quickUp10 = InventoryRules.QuickMove(qm10, cat10, SlotRef.Bag(0), "q2");
var qmFull10 = Clone10(full10);
qmFull10.hand.Clear();
qmFull10.handSource = "";
string qmFullSnap10 = Inv10(qmFull10);
Check(quickDown10.Location == SlotRef.Bag(1) && quickUp10.Location == SlotRef.Hotbar(1) && quickUp10.MovedBagToHotbar &&
      InventoryRules.QuickMove(qmFull10, stack10, SlotRef.Hotbar(0), "q3").Message == InventoryRules.NoFreeSlot && Inv10(qmFull10) == qmFullSnap10,
    "quick move sends the selected item to the first free slot of the other area; with no free slot it is refused unchanged");

// ---------- take it out, the old drawer list, hand ----------
// The Desk Drawer left play (2026-10-25): everything owned is carried in the bar or the bag.
Check(!typeof(InventoryRules).GetMethods().Any(m => m.Name == "Store" || m.Name == "Retrieve"),
    "no storing or retrieving: the Desk Drawer is gone from the inventory rules");
var tk10 = Clone10(acq10);
InventoryResult take10 = InventoryRules.TakeOut(tk10, cat10, InventoryCatalogue.TaxCodeBook, "t1");
Check(take10.Changed && tk10.hotbar[2].itemId == InventoryCatalogue.TaxCodeBook && tk10.bag[0].itemId == InventoryCatalogue.CaseFolder,
    "Take it out from the bag is one swap into the bar (works anywhere; only required items in the bar: the last one steps aside)");
var tkFolder10 = Clone10(acq10);
InventoryRules.Move(tkFolder10, cat10, SlotRef.Hotbar(2), SlotRef.Bag(1), "t2");
InventoryResult takeFolder10 = InventoryRules.TakeOut(tkFolder10, cat10, InventoryCatalogue.CaseFolder, "t3");
Check(takeFolder10.Changed && InventoryRules.InHotbar(tkFolder10, InventoryCatalogue.CaseFolder) && takeFolder10.Location == SlotRef.Hotbar(2),
    "the Auditor / Marie case: a Case Folder in the bag comes back to a free bar slot in one tap");
var rt10 = Fresh10();
rt10.drawer.Add(new InventoryDrawerEntry { itemId = "voucher", quantity = 7 });
rt10.hotbar[0] = new InventorySlot { itemId = InventoryCatalogue.Notes, quantity = 1 };
rt10.hotbar[1] = new InventorySlot { itemId = "pencil", quantity = 1 };
InventoryRules.Normalize(rt10, stack10, new List<string>());
Check(rt10.drawer.Count == 0 && InventoryRules.CarriedCount(rt10, "voucher") == 7 && rt10.hotbar[1].itemId == "pencil" &&
      rt10.hotbar.Concat(rt10.bag).All(s => s.itemId != "voucher" || s.quantity <= 3),
    "an older save's drawer items come back into free slots on load (stacks respect their limit; nothing else moves)");
var rtLimit10 = Fresh10();
rtLimit10.drawer.Add(new InventoryDrawerEntry { itemId = InventoryCatalogue.ReviewVoucher, quantity = 3 });
InventoryRules.Normalize(rtLimit10, cat10, new List<string>());
Check(InventoryRules.CarriedCount(rtLimit10, InventoryCatalogue.ReviewVoucher) == 2 && InventoryRules.DrawerCount(rtLimit10, InventoryCatalogue.ReviewVoucher) == 1,
    "the carry limit still holds when old drawer vouchers come back: two carried, the extra stays held in the record (never lost)");

var rel10 = Clone10(full10);
InventoryResult relBook10 = InventoryRules.ReleaseHand(rel10, stack10, true, "h1");
Check(relBook10.Changed && rel10.hand.IsEmpty && !InventoryRules.Owns(rel10, InventoryCatalogue.TaxCodeBook) && relBook10.Location.IsNone,
    "closing Make room with the desk book in hand returns it to its world source (the desk)");
var relReq10 = Fresh10();
relReq10.hand = new InventorySlot { itemId = InventoryCatalogue.CaseFolder, quantity = 1 };
relReq10.handSource = InventoryRules.HandSourcePickup;
string relSnap10 = Inv10(relReq10);
var relDisp10 = Fresh10();
relDisp10.hand = new InventorySlot { itemId = "pencil", quantity = 1 };
relDisp10.handSource = InventoryRules.HandSourceDisplaced;
Check(InventoryRules.ReleaseHand(relReq10, stack10, true, "h2").Message == InventoryRules.NeededForCase && Inv10(relReq10) == relSnap10 &&
      InventoryRules.ReleaseHand(relDisp10, stack10, true, "h3").Changed && InventoryRules.Find(relDisp10, "pencil").IsSlot && relDisp10.drawer.Count == 0,
    "a required item cannot be left behind (refused); a displaced item with no world source takes a free slot");
var place10 = Clone10(full10);
InventoryResult placeHand10 = InventoryRules.Move(place10, stack10, SlotRef.Hand, SlotRef.Bag(2), "h4");
string placeSnap10 = Inv10(place10);
Check(placeHand10.Changed && place10.bag[2].itemId == InventoryCatalogue.TaxCodeBook && place10.hand.itemId == "pencil" && place10.handSource == InventoryRules.HandSourceDisplaced &&
      InventoryRules.ReleaseHand(place10, stack10, true, "h5").Message == InventoryRules.BagFull && Inv10(place10) == placeSnap10,
    "Make room: placing the held item on an occupied slot swaps (the old item is held); with nothing free it stays in hand (\"Your bar and bag are full\")");

// ---------- remove, containers, upgrades ----------
var rm10 = Clone10(acq10);
Check(InventoryRules.Remove(rm10, cat10, InventoryCatalogue.TaxCodeBook, "x1").Changed && !InventoryRules.Owns(rm10, InventoryCatalogue.TaxCodeBook) &&
      InventoryRules.Record(rm10, InventoryCatalogue.TaxCodeBook) == null &&
      InventoryRules.Remove(rm10, cat10, InventoryCatalogue.CaseFolder, "x2").Changed && rm10.hotbar[2].IsEmpty &&
      InventoryRules.Remove(rm10, cat10, InventoryCatalogue.CaseFolder, "x3").Outcome == InventoryOutcome.NoChange,
    "returning an item to the world empties its slot (folder and book back on the desk) from anywhere, once");
var ct10 = Clone10(acq10);
var profiles10 = new[] { "profile_marie", "profile_jhunjhun", "profile_milo", "profile_zorbix" };
InventoryRules.SetContents(ct10, InventoryCatalogue.ProfileBinder, profiles10);
InventoryRules.SetContents(ct10, InventoryCatalogue.CaseFolder, new[] { "case_001/papers", "case_001/client-copy" });
Check(ct10.hotbar.Count(s => s.itemId == InventoryCatalogue.ProfileBinder) == 1 && InventoryRules.Record(ct10, InventoryCatalogue.ProfileBinder).contents.SequenceEqual(profiles10) &&
      InventoryRules.Record(ct10, InventoryCatalogue.CaseFolder).contents.Contains("case_001/client-copy") &&
      InventoryRules.SetContents(ct10, InventoryCatalogue.ProfileBinder, profiles10).Outcome == InventoryOutcome.NoChange,
    "containers: all four profiles sit in one binder slot; the folder holds its papers and the Client Copy in one slot");
var up10 = Fresh10();
InventoryResult belt10 = InventoryRules.UpgradeCapacity(up10, InventoryArea.Hotbar, 4, "belt-pouch");
Check(belt10.Changed && up10.hotbarCapacity == 4 && up10.hotbar.Count == 4 && InventoryRules.IsUsable(up10, SlotRef.Hotbar(3)) &&
      InventoryRules.UpgradeCapacity(up10, InventoryArea.Hotbar, 4, "belt-pouch").Outcome == InventoryOutcome.AlreadyApplied &&
      InventoryRules.UpgradeCapacity(up10, InventoryArea.Hotbar, 3, "lower").Outcome == InventoryOutcome.NoChange &&
      InventoryRules.UpgradeCapacity(up10, InventoryArea.Hotbar, 9, "huge").Changed && up10.hotbarCapacity == 5 &&
      InventoryRules.UpgradeCapacity(up10, InventoryArea.Bag, 8, "satchel").Changed && up10.bagCapacity == 8 && up10.bag.Count == 8 &&
      InventoryRules.UpgradeCapacity(up10, InventoryArea.Drawer, 9, "nope").Outcome == InventoryOutcome.Rejected,
    "UpgradeCapacity (T12's Belt Pouch / Satchel): raises once per action id, never lowers, caps at 5 / 12, new slots unlock at once");

// ---------- normalize with the catalogue ----------
var nm10 = Fresh10();
nm10.hotbar[0] = new InventorySlot { itemId = "mystery_box", quantity = 1 };
nm10.hotbar[1] = new InventorySlot { itemId = InventoryCatalogue.Notes, quantity = 1 };
nm10.bag[0] = new InventorySlot { itemId = InventoryCatalogue.Notes, quantity = 1 };
nm10.bag[1] = new InventorySlot { itemId = "voucher", quantity = 8 };
nm10.drawer.Add(new InventoryDrawerEntry { itemId = InventoryCatalogue.CaseFolder, quantity = 1 });
nm10.drawer.Add(new InventoryDrawerEntry { itemId = "gone", quantity = 2 });
nm10.hand = new InventorySlot { itemId = InventoryCatalogue.ProfileBinder, quantity = 1 };
nm10.items.Add(new InventoryItemRecord { itemId = InventoryCatalogue.TaxCodeBook });
nm10.selectedSlot = "bag:11";
var warnings10 = new List<string>();
bool nmChanged10 = InventoryRules.Normalize(nm10, stack10, warnings10);
Check(nmChanged10 && warnings10.Count == 1 && warnings10[0].Contains("mystery_box") && warnings10[0].Contains("gone") &&
      nm10.hotbar.Concat(nm10.bag).Count(s => s.itemId == InventoryCatalogue.Notes) == 1 &&
      InventoryRules.Find(nm10, InventoryCatalogue.CaseFolder).IsSlot && InventoryRules.DrawerCount(nm10, InventoryCatalogue.CaseFolder) == 0 &&
      InventoryRules.Find(nm10, InventoryCatalogue.ProfileBinder).IsSlot && nm10.hand.IsEmpty &&
      nm10.bag.Where(s => s.itemId == "voucher").Sum(s => s.quantity) == 8 && nm10.bag.All(s => s.itemId != "voucher" || s.quantity <= 3) &&
      InventoryRules.Record(nm10, InventoryCatalogue.TaxCodeBook) == null && InventoryRules.Record(nm10, InventoryCatalogue.CaseFolder) != null &&
      nm10.selectedSlot == "" && !InventoryRules.Normalize(nm10, stack10, new List<string>()),
    "normalize: unknown ids empty with one warning, duplicates keep their first place, required items return from the old drawer list and the hand, stacks spill, records follow ownership");
var crowd10 = Fresh10();
for (int i = 0; i < 3; i++) crowd10.hotbar[i] = new InventorySlot { itemId = "voucher", quantity = 3 };
for (int i = 0; i < 5; i++) crowd10.bag[i] = new InventorySlot { itemId = "voucher", quantity = 3 };
crowd10.hotbar[0] = new InventorySlot { itemId = InventoryCatalogue.Notes, quantity = 1 };
crowd10.hand = new InventorySlot { itemId = InventoryCatalogue.CaseFolder, quantity = 1 };
InventoryRules.Normalize(crowd10, stack10, new List<string>());
Check(InventoryRules.Find(crowd10, InventoryCatalogue.CaseFolder).IsSlot && crowd10.hand.IsEmpty && InventoryRules.Owns(crowd10, InventoryCatalogue.Notes) &&
      InventoryRules.DrawerCount(crowd10, "voucher") == 3,
    "never stuck: a required item in hand with no free slot takes a non-required item's place (that one is held in the record, never lost)");

// ---------- journal, slot refs ----------
var learned10 = new HashSet<string> { "met-npc_marie", "case-received", "rosa-desk-provenance" };
List<InventoryJournalFact> journal10 = InventoryRules.Journal(cat10, learned10);
Check(journal10.Count == 2 && journal10[0].id == "met-npc_marie" && journal10[1].id == "case-received" && journal10.All(f => f.sourceItemId.Length > 0) &&
      InventoryRules.Journal(cat10, new HashSet<string>()).Count == 0 && learned10.Count == 3,
    "the journal lists only learned catalogue facts with their source record, and reading it creates none");
Check(cat10.journal.All(f => !f.text.Contains("Resident") && !f.text.Contains("Citizen") && !f.text.Contains("Alien") && !f.text.Contains("₱")),
    "journal facts are neutral: no classification or amount");
bool slotRefs10 = true;
foreach (SlotRef r10 in new[] { SlotRef.Hotbar(0), SlotRef.Hotbar(4), SlotRef.Bag(11), SlotRef.Hand, SlotRef.Drawer })
    slotRefs10 &= SlotRef.TryParse(r10.ToString(), out SlotRef p10) && p10 == r10;
Check(slotRefs10 && !SlotRef.TryParse("pocket:1", out _) && !SlotRef.TryParse("bag:-1", out _) && !SlotRef.TryParse("", out _),
    "slot references round-trip as text (hotbar:i, bag:i, hand, drawer) and junk is refused");

// ---------- wiring (source contracts; the HUD, panel and world drags are Unity runtime paths) ----------
string rules10 = Src("RewardsSystem", "CampaignInventory.cs");
string manager10 = Src("RewardsSystem", "PlayerProgressManager.cs");
string hotbar10 = Src("UI", "HUD", "HudHotbar.cs");
string screen10 = Src("UI", "HUD", "InventoryScreen.cs");
string hud10 = Src("UI", "HUD", "OfficeHudRuntime.cs");
Check(!rules10.Contains("UnityEngine") && !rules10.Contains("Trust") && !rules10.Contains("coins") && !rules10.Contains("rewardClaims"),
    "the inventory rules are pure and know nothing of trust, coins or reward claims");
string commit10 = Between(manager10, "public InventoryResult CommitInventory(", "public bool NormalizeInventory(");
Check(commit10.Contains("if (!result.Changed) return result;") && Index(commit10, "TryWriteCandidate(next)") >= 0 &&
      Index(commit10, "TryWriteCandidate(next)") < Index(commit10, "InventoryChanged?.Invoke()") && commit10.Contains("InventoryOutcome.SaveFailed") &&
      commit10.Contains("else sessionInventory = candidate;"),
    "inventory commands run on a candidate copy, are written before they are published, and a failed write changes nothing");
Check(manager10.Contains("CampaignRun.AllowsStoryWrites(CurrentRunMode) && !TrainingSession.IsPracticing") && manager10.Contains("A temporary run never sees or spends the story's items."),
    "practice, mastery replays and Editor runs use a temporary inventory; only story runs write the campaign's");
string[] forbidden10 = { "CommitFieldAssignment", "CommitFollowThrough", "CampaignRewards", "LevelScoreLedger", "Time.timeScale", "OfficeHours.", "LevelGate.Mark", "AddExp" };
Check(forbidden10.All(w => hotbar10.IndexOf(w, StringComparison.Ordinal) < 0 && screen10.IndexOf(w, StringComparison.Ordinal) < 0),
    "opening or arranging the inventory grants no rewards, trust, XP or clock change and never pauses time");
Check(screen10.Contains("HudWindowLock.Acquire(this);") && screen10.Contains("HudWindowLock.Release(this);") && screen10.Contains("shade.raycastTarget = true;") &&
      Index(screen10, "DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen || InGameSettings.IsOpen") > 0,
    "the panel holds movement while open, blocks world taps behind it, and closes cleanly when a conversation, card or settings takes over");
Check(Regex10(screen10, @"Run\(rec => InventoryRules\.") >= 4 && screen10.Contains("InventoryResult result = hotbar.Commit(command);") &&
      !screen10.Contains("PlayerProgressManager.Instance.CommitInventory") && !screen10.Contains("campaign.inventory"),
    "every panel change goes through the inventory command path (the panel only renders snapshots)");
Check(screen10.Contains("private void BuildSidePanel(RectTransform body)") && screen10.Contains("\"DETAILS\"") &&
      Index(screen10, "ShowSheet(chosen,") > Index(screen10, "private void Rebuild()") && !screen10.Contains("DESK DRAWER") && !screen10.Contains("TO DRAWER"),
    "the Bag shows the chosen item in a DETAILS side panel (no drawer panel, no TO DRAWER button)");
Check(hotbar10.Contains("WorldOwns(def.id, trustInventory: false) == true") && hotbar10.Contains("if (trustInventory && InventoryRules.Owns(Inventory, InventoryCatalogue.Notes)) return true;"),
    "a fresh run (replay) drops the previous attempt's Notes: the reset asks the scene, not the bag (2026-10-26)");
Check(hotbar10.Contains("result = Manager.CommitPickup(claimId, onceOnly: false, inventory =>") && hotbar10.Contains("\"notes-chest\""),
    "the chest gives Notes again on every fresh run (its claim is not once per campaign; an owned Notes is a no-change)");
string auditor26 = Src("Interactables", "AuditorInteractable.cs");
string tray26 = Src("Data", "AuditorSubmissionTray.cs");
string handIn26 = Between(auditor26, "private bool TryHandInFolder()", "private void OnHandInLine(");
Check(auditor26.Contains(".AwaitAction(SubmitAction, \"Drag the Case Folder to the Auditor\")") &&
      auditor26.Contains("if (TalkFirst && !requiresTaxReturn && FolderInHand() != null)") &&
      handIn26.Contains("if (!AwaitingFolder") && handIn26.Contains("folder.ReleaseQuietly();") &&
      Index(handIn26, "folder.ReleaseQuietly();") < Index(handIn26, "dialogueUI.ContinueWith(SubmitAction, lines);") &&
      tray26.Contains("if (AuditorInteractable.TalkFirst)") && tray26.Contains("public bool AcceptFromConversation()") &&
      Src("UI", "HUD", "OfficeHudRuntime.cs").Contains("AuditorInteractable.ResolveFolderDrop(screen) ??"),
    "Level 1 hand-in: talk first; he takes the folder only on his waiting line (drag or icon tap), the icon empties, and the tray refuses drops");
Check(auditor26.Contains(".AwaitAction(SummaryAction, \"Read the Audit Summary\")") &&
      Between(auditor26, "private void OnHandInLine(", "private void OnHandInSummaryClosed()").Contains("summaryPopupUI.Show(handInResult.MissedIssues, OnHandInSummaryClosed);") &&
      Between(auditor26, "private void OnHandInSummaryClosed()", "public bool TryContinueApproval(").Contains(".AwaitAction(ApprovalAction, null)") &&
      Src("UI", "HUD", "ClientCopyHandover.cs").Contains("if (auditor != null && auditor.TryContinueApproval(ApprovalLines(), DropCopy)) yield break;"),
    "the summary card opens inside his conversation, and on approval the sign-off and the Client Copy drop continue it");
Check(auditor26.Contains("GameplayEvents.RaiseGuideCue(result.MissedIssues.Count == 0 ? CueApproved : failedAudits > 1 ? CueNeedsFixAgain : CueNeedsFix);") &&
      !Src("UI", "HUD", "ConsequenceReactions.cs").Contains("clientFailedBubble"),
    "the guide explains the card actually shown (approved / needs a fix); a failed audit shows no client reaction");
string card26 = Src("SideQuest1", "ProfileDragCard.cs");
Check(card26.Contains("settleAnimated = true;") && card26.Contains("settleAnimated = false;   // a restore appears already settled") &&
      card26.Contains("private void AnimatePinHead(float t)") && !Between(card26, "private void UpdateSway()", "private void AnimatePinHead(").Contains("IsCorrectlyPlaced"),
    "every pinned card gets the same pin-in (squash, swing, pin pop; no hint of right or wrong) and a restore shows it settled");
string folderView10 = Src("UI", "HUD", "CaseFolder3DView.cs");
Check(folderView10.Contains("HudSlotDrop.Active ? HudSlotDrop.IsOver(data.position)") && folderView10.Contains("deskReturn.IsOverDeskStack(data.position, MainCamera())") &&
      folderView10.Contains("owner.ResetToUnstored();") && folderView10.Contains("toPosition = screenPosition.HasValue && deskDrag != null ? deskDrag.OpenPositionFor(screen) : restPosition;") &&
      folderView10.Contains("toPosition = HeldOnScreen.FitOnScreen(folder, main);") && Src("CaseFolder", "CaseFolder3DDrag.cs").Contains("HeldOnScreen.FitOnScreen(openFolder.transform, view)") &&
      hotbar10.Contains("OfficeHudRuntime.Instance.CaseFolderViewOut"),
    "the floating Case Folder uses the desk-open pose, closes into any slot or the Bag, goes back on the desk stack, and its icon dims while it is out (2026-10-26)");
Check(hotbar10.Contains("public static bool UsesHotbar(string levelId) => levelId == OfficeLevelCatalog.Level1Id;") &&
      hud10.Contains("if (HudHotbar.UsesHotbar(OfficeLevelProfile.LevelIdFor(this)))") &&
      hotbar10.Contains("Wrap(InventoryCatalogue.Notes, buttons.Find(\"NotesIconButton\"));") &&
      hotbar10.Contains("Wrap(InventoryCatalogue.CaseFolder, buttons.Find(\"CaseFolderIconButton\"));") &&
      hotbar10.Contains("Wrap(InventoryCatalogue.ProfileBinder, buttons.Find(\"TaxpayerProfileIcon\"));") &&
      hotbar10.Contains("public static readonly Vector2 FirstSlot = new Vector2(10f, -20f);") && hotbar10.Contains("public const float SlotStep = 140f;"),
    "Level 1 only: the three scene icons keep their places (x 10, y -20 / -160 / -300) and travel with their items inside slot wrappers");
Check(hud10.Contains("caseFolderIcon.IsChildOf(sibling)") && hotbar10.Contains("if (awaiting)"),
    "Marie's handover gate keeps the folder's slot wrapper visible and the hotbar leaves the other icons to the gate");
string folderStore10 = Src("CaseFolder", "FolderToHUDIcon.cs");
string closedDrag10 = Src("CaseFolder", "CaseFolder3DDrag.cs");
Check(folderStore10.Contains("bool pointerInsideIcon = IsOverDropTarget(eventData.position);") && folderStore10.Contains("if (HudSlotDrop.Active) return HudSlotDrop.IsOver(screenPosition);") &&
      closedDrag10.Contains("return Icon.IsOverDropTarget(eventData.position);") &&
      folderStore10.Contains("HudSlotDrop.Note(InventoryCatalogue.CaseFolder, eventData.position);") && closedDrag10.Contains("HudSlotDrop.Note(InventoryCatalogue.CaseFolder, drop);"),
    "the open and closed folder use one shared slot drop test (any slot or the row) and report which slot they landed on");
string book10src = Src("Level2", "Level2TaxCodeBookIcon.cs");
Check(book10src.Contains("public static IBookCarrier Carrier;") && book10src.Contains("Carrier != null ? Carrier.Carried : Level2OfficeStart.Review?.taxBookStored == true") &&
      book10src.Contains("review.taxBookStored = true") && book10src.Contains("review.taxBookStored = false;") && hotbar10.Contains("Level2TaxCodeBookIcon.Carrier = this;"),
    "the Tax Code Book is carried through Level 2's book icon with Level 1's inventory as its store (Level 2 keeps its review flag)");
Check(hotbar10.Contains("GameplayEvents.RaiseTaxCodeBookFirstOpened();") && hotbar10.Contains("if (showing && !bookWasShowing && !BookOpenedOnce)") &&
      Src("SideQuest1", "Level1FieldCheckpointer.cs").Contains("if (HudHotbar.BookOpenedOnce) into.taxCodeBookOpened = true;") &&
      Src("SideQuest1", "Level1FieldCheckpointer.cs").Contains("HudHotbar.RestoreBookOpened();"),
    "the book raises its first-open event once per run, saved and restored with the Level 1 checkpoint");
Check(hotbar10.Contains("CueOverflow = \"BagOverflow\"") && hotbar10.Contains("if (!quiet && res.WentToBagBecauseBarFull && !r.bagLessonShown)") &&
      hotbar10.Contains("if (result.MovedBagToHotbar) GameplayEvents.RaiseGuideCue(CueToBar);"),
    "the bag lesson cue is raised once, from the real overflow, and completes on a real bag-to-bar move");
string guide10 = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Tutorials", "Level_01", "guide.json"));
using (JsonDocument guideDoc10 = JsonDocument.Parse(guide10))
{
    var lessons10 = guideDoc10.RootElement.GetProperty("lessons").EnumerateArray().ToList();
    JsonElement L10(string id) => lessons10.First(l => l.GetProperty("id").GetString() == id);
    var bag10 = L10("bag-full");
    var notesLesson10 = L10("notes");
    Check(bag10.GetProperty("when").GetString() == "cue:BagOverflow" && bag10.GetProperty("pages")[0].GetProperty("waitFor").GetString() == "cue:BagItemToBar" &&
          bag10.GetProperty("pages")[0].GetProperty("text").GetString() == "Your bar is full, so it went in your bag. Tap the bag and drag it to your bar." &&
          L10("bag-full-known").GetProperty("pages")[0].GetProperty("gesture").GetString() == "none" && !L10("bag-full-known").GetProperty("pages")[0].TryGetProperty("point", out _),
        "guide: the bag lesson says the agreed line, waits for the real move, and shows no arrow once the Bag icon has been used");
    // T16 chest revision: the Notes paper comes from the plaza chest, not the receptionist.
    Check(notesLesson10.GetProperty("when").GetString() == "lesson:chest-find|owns:notes" && notesLesson10.GetProperty("after").GetString() == "chest-find" &&
          L10("chest-find").GetProperty("after").GetString() == "bag-empty" &&
          notesLesson10.GetProperty("pages")[0].GetProperty("waitFor").GetString() == "not:active:type:NotesChestPaperPickup" &&
          L10("move").GetProperty("after").GetString() == "hud-tour" && !guide10.Contains("onto your Case Folder icon") && guide10.Contains("\"to\": \"hud:slot\""),
        "guide: Notes are taught once the chest paper is picked up (after the empty bag); the folder is stored on any slot");
}
string scene16 = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Office_Level1.unity"));
string chest16 = Src("SideQuest1", "NotesOnboardingChest.cs");
// The Synty lid pivots on its rear edge with its latch toward +Z, so a negative X turn lifts the front edge.
Check(scene16.Contains("m_EditorClassIdentifier: Assembly-CSharp::NotesOnboardingChest") && scene16.Contains("openEuler: {x: -105, y: 0, z: 0}") &&
      chest16.Contains("new Vector3(-105f, 0f, 0f)") && chest16.Contains("checkpointer != null && !checkpointer.RequestSave()") &&
      System.Text.RegularExpressions.Regex.IsMatch(scene16, @"target: \{fileID: 287133798820718090, guid: 62dba8a4955e4f1aa8fc50aab52d2f06, type: 3\}\s+propertyPath: m_IsActive\s+value: 0"),
    "the plaza Notes chest is in Level 1, saves before it opens, lifts its lid upward on the rear hinge, and its paper starts hidden");
string paper16 = Src("SideQuest1", "NotesChestPaperPickup.cs");
string paperPrefab16 = File.ReadAllText(Path.Combine(root, "Assets", "Prefabs", "NotesChestPaper.prefab"));
Check(scene16.Contains("m_Name: Notes Chest Placement") && scene16.Contains("m_EditorClassIdentifier: Assembly-CSharp::NotesChestPlacement") &&
      chest16.Contains("if (placement != null) placement.Apply(transform);") &&
      paper16.Contains("NotesPaperView.BuildSheet(") && Src("UI", "HUD", "NotesPaperView.cs").Contains("BuildPage(root, width, height);") &&
      !paper16.Contains("ProfileCardTextLayout") && !paperPrefab16.Contains("NameText") && !paperPrefab16.Contains("DescriptionText"),
    "the Notes chest follows its scene placement marker, and its paper is drawn with the Notes panel's own page, not a profile card");
Check(guide10Welcome16(), "Level 1's welcome card lists today's objectives again");
bool guide10Welcome16() => File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Tutorials", "Level_01", "guide.json")).Contains("\"listObjectives\": true,");
Check(GuideTargetsNames10().Length == 3, "hud:bag, hud:slot and hud:book are valid guide targets");
Check(Src("SideQuest1", "TaxpayerProfileCollectionHUD.cs").Contains("public static System.Func<Vector2?> CollectionPointOverride;") &&
      hotbar10.Contains("TaxpayerProfileCollectionHUD.CollectionPointOverride = BinderLanding;") &&
      Src("Interactables", "AuditorInteractable.cs").Contains("if (BagReminder != null && BagReminder())") &&
      hotbar10.Contains("AuditorInteractable.BagReminder = AuditorBagReminder;"),
    "profiles fly to the binder's slot (or the Bag); asking the Auditor with the folder in the bag shows Take it out instead of the reminder");
Check(screen10.Contains("Close(force: false)") && screen10.Contains("InventoryRules.ReleaseHand(r, Catalogue, true,") && screen10.Contains("hotbar.ReturnedToWorld(held)"),
    "closing Make room returns a new pickup to its world source and refuses to leave a required item behind");

int Regex10(string text, string pattern) => System.Text.RegularExpressions.Regex.Matches(text, pattern).Count;
string[] GuideTargetsNames10()
{
    string targets = Src("UI", "Guide", "GuideTargets.cs");
    string list = Between(targets, "public static readonly string[] HudNames =", "};");
    return System.Text.RegularExpressions.Regex.Matches(list, "\"([a-z]+)\"").Select(m => m.Groups[1].Value).Where(n => n == "bag" || n == "slot" || n == "book").ToArray();
}

// =====================================================================
// T11: world records and optional keepsakes
// =====================================================================
InventoryCatalogue cat11 = InventoryCatalogue.BuiltIn();
string Inv11(CampaignInventoryRecord r) => JsonSerializer.Serialize(r, json);
CampaignInventoryRecord Fresh11() { var r = new CampaignInventoryRecord(); r.Repair(); return r; }

// ---------- catalogue: the two optional items ----------
InventoryItemDefinition postcard11 = cat11.Find(InventoryCatalogue.Postcard);
InventoryItemDefinition voucher11 = cat11.Find(InventoryCatalogue.ReviewVoucher);
Check(postcard11 != null && postcard11.category == InventoryCatalogue.Keepsakes && !postcard11.required && postcard11.stackLimit == 1 && postcard11.scope == "" &&
      voucher11 != null && voucher11.category == InventoryCatalogue.ToolsAndSupplies && !voucher11.required && voucher11.stackLimit == 2 && voucher11.carryLimit == 2 &&
      voucher11.scope == "" && cat11.Problems().Count == 0,
    "the Coast Tales postcard is an optional keepsake and the review voucher an optional supply (stack 2, carry 2), kept for the whole campaign");
var badCarry11 = InventoryCatalogue.BuiltIn();
badCarry11.items.Add(new InventoryItemDefinition { id = "seal", label = "Seal", category = InventoryCatalogue.CaseFiles, required = true, carryLimit = 1 });
badCarry11.items.Add(new InventoryItemDefinition { id = "odd", label = "Odd", category = InventoryCatalogue.Keepsakes, carryLimit = -1 });
List<string> carryProblems11 = badCarry11.Problems();
Check(carryProblems11.Any(p => p.Contains("cannot have a carry limit")) && carryProblems11.Any(p => p.Contains("carry limit -1")),
    "a required item can never have a carry limit (it could be refused), and limits are not negative");

// ---------- carry limit: a third voucher is refused (the Desk Drawer left play) ----------
var carry11 = Fresh11();
InventoryResult firstVoucher11 = InventoryRules.Acquire(carry11, cat11, InventoryCatalogue.ReviewVoucher, 1, "v1");
InventoryResult secondVoucher11 = InventoryRules.Acquire(carry11, cat11, InventoryCatalogue.ReviewVoucher, 1, "v2");
int over11 = InventoryRules.OverCarryLimit(carry11, cat11, InventoryCatalogue.ReviewVoucher, 1);
string carrySnap11 = Inv11(carry11);
InventoryResult thirdVoucher11 = InventoryRules.Acquire(carry11, cat11, InventoryCatalogue.ReviewVoucher, 1, "v3");
Check(firstVoucher11.Changed && secondVoucher11.Changed && over11 == 1 &&
      thirdVoucher11.Outcome == InventoryOutcome.Rejected && thirdVoucher11.Message == InventoryRules.CarryFull && Inv11(carry11) == carrySnap11 &&
      InventoryRules.CarriedCount(carry11, InventoryCatalogue.ReviewVoucher) == 2 && carry11.drawer.Count == 0,
    "with two vouchers carried, a third is refused (\"You can't carry more of these\", known before collecting) and nothing changes");
var split11 = Fresh11();
InventoryResult three11 = InventoryRules.Acquire(split11, cat11, InventoryCatalogue.ReviewVoucher, 3, "v4");
Check(three11.Changed && InventoryRules.CarriedCount(split11, InventoryCatalogue.ReviewVoucher) == 2 && split11.hotbar[0].quantity == 2 &&
      split11.drawer.Count == 0,
    "a bigger delivery fills the carry limit (one stack of two); the rest is not taken");
var routeFull11 = Fresh11();
for (int i = 0; i < 3; i++) routeFull11.hotbar[i] = new InventorySlot { itemId = InventoryCatalogue.Notes, quantity = 1 };
for (int i = 0; i < 5; i++) routeFull11.bag[i] = new InventorySlot { itemId = InventoryCatalogue.ReviewVoucher, quantity = 1 };
routeFull11.hand = new InventorySlot { itemId = InventoryCatalogue.TaxCodeBook, quantity = 1 };
string routeSnap11 = Inv11(routeFull11);
InventoryResult routeBusy11 = InventoryRules.Acquire(routeFull11, cat11, InventoryCatalogue.Postcard, 1, "v5");
Check(routeBusy11.Outcome == InventoryOutcome.Rejected && Inv11(routeFull11) == routeSnap11,
    "a refused pickup (hand busy, nothing free) changes nothing");

// ---------- office display ----------
var disp11 = Fresh11();
InventoryRules.Acquire(disp11, cat11, InventoryCatalogue.Postcard, 1, "d1");
string anchor11 = PickupCatalogue.DeskDisplay;
InventoryResult show11 = InventoryRules.Display(disp11, cat11, InventoryCatalogue.Postcard, anchor11, "d2");
Check(show11.Changed && show11.Location == SlotRef.Display && disp11.hotbar[0].IsEmpty && InventoryRules.DisplayedAt(disp11, anchor11) == InventoryCatalogue.Postcard &&
      InventoryRules.Owns(disp11, InventoryCatalogue.Postcard) && InventoryRules.Find(disp11, InventoryCatalogue.Postcard).IsNone &&
      InventoryRules.Acquire(disp11, cat11, InventoryCatalogue.Postcard, 1, "d3").Outcome == InventoryOutcome.NoChange,
    "displaying the postcard moves it from its slot onto the desk stand; it stays owned and is never granted again");
string dispSnap11 = Inv11(disp11);
InventoryRules.Acquire(disp11, cat11, InventoryCatalogue.ReviewVoucher, 1, "d4");
string dispSnap11b = Inv11(disp11);
Check(InventoryRules.Display(disp11, cat11, InventoryCatalogue.ReviewVoucher, "other", "d5").Message == "Only keepsakes go on display" &&
      InventoryRules.Display(disp11, cat11, InventoryCatalogue.Postcard, "other", "d6").Message == "It is already on display" &&
      Inv11(disp11) == dispSnap11b,
    "only keepsakes go on display, one place at a time (refusals change nothing)");
var occupied11 = Fresh11();
occupied11.displays.Add(new InventoryDisplay { anchorId = anchor11, itemId = "other_keepsake" });
occupied11.drawer.Add(new InventoryDrawerEntry { itemId = InventoryCatalogue.Postcard, quantity = 1 });
Check(InventoryRules.Display(occupied11, cat11, InventoryCatalogue.Postcard, anchor11, "d7").Message == "Something is already on display there",
    "one keepsake per display anchor");
var fromDrawer11 = Fresh11();
fromDrawer11.drawer.Add(new InventoryDrawerEntry { itemId = InventoryCatalogue.Postcard, quantity = 1 });
Check(InventoryRules.Display(fromDrawer11, cat11, InventoryCatalogue.Postcard, anchor11, "d8").Changed && fromDrawer11.drawer.Count == 0 &&
      InventoryRules.DisplayedAt(fromDrawer11, anchor11) == InventoryCatalogue.Postcard,
    "a keepsake still held in an older save's drawer list can go on display straight from there");
InventoryResult down11 = InventoryRules.TakeDown(disp11, cat11, anchor11, "d9");
Check(down11.Changed && down11.Location.Area == InventoryArea.Hotbar && InventoryRules.DisplayedAt(disp11, anchor11) == "" &&
      InventoryRules.TakeDown(disp11, cat11, anchor11, "d9").Outcome == InventoryOutcome.AlreadyApplied &&
      InventoryRules.TakeDown(disp11, cat11, anchor11, "d10").Outcome == InventoryOutcome.NoChange,
    "taking it back puts it in the first free slot, once");
var downFull11 = Fresh11();
for (int i = 0; i < 3; i++) downFull11.hotbar[i] = new InventorySlot { itemId = InventoryCatalogue.Notes, quantity = 1 };
for (int i = 0; i < 5; i++) downFull11.bag[i] = new InventorySlot { itemId = InventoryCatalogue.ReviewVoucher, quantity = 1 };
downFull11.displays.Add(new InventoryDisplay { anchorId = anchor11, itemId = InventoryCatalogue.Postcard });
InventoryResult downFullResult11 = InventoryRules.TakeDown(downFull11, cat11, anchor11, "d11");
Check(downFullResult11.Outcome == InventoryOutcome.Rejected && downFullResult11.Message == InventoryRules.BagFull &&
      InventoryRules.DisplayedAt(downFull11, anchor11) == InventoryCatalogue.Postcard && downFull11.drawer.Count == 0,
    "with no free slot a keepsake stays on display (\"Your bar and bag are full\"; never lost)");
var removeShown11 = Fresh11();
removeShown11.displays.Add(new InventoryDisplay { anchorId = anchor11, itemId = InventoryCatalogue.Postcard });
Check(InventoryRules.Remove(removeShown11, cat11, InventoryCatalogue.Postcard, "d12").Changed && removeShown11.displays.Count == 0,
    "Remove also clears a display placement");

var dispRepair11 = new CampaignInventoryRecord { displays = new List<InventoryDisplay>
{
    new InventoryDisplay { anchorId = " desk ", itemId = InventoryCatalogue.Postcard }, new InventoryDisplay { anchorId = "desk", itemId = "x" },
    new InventoryDisplay { anchorId = "shelf", itemId = InventoryCatalogue.Postcard }, null, new InventoryDisplay { anchorId = "", itemId = "y" }
} };
Check(dispRepair11.Repair() && dispRepair11.displays.Count == 1 && dispRepair11.displays[0].anchorId == "desk" &&
      new CampaignInventoryRecord { displays = null }.Repair(),
    "repair keeps one keepsake per anchor and one anchor per keepsake");
var save11 = PlayerProgressData.CreateNewCampaign("t11", "");
InventoryRules.Acquire(save11.campaign.inventory, cat11, InventoryCatalogue.Postcard, 1, "s1");
InventoryRules.Display(save11.campaign.inventory, cat11, InventoryCatalogue.Postcard, anchor11, "s2");
InventoryRules.Acquire(save11.campaign.inventory, cat11, InventoryCatalogue.ReviewVoucher, 3, "s3");
save11.campaign.rewardClaims.Add(PickupRules.ClaimId("t11", "Level_01", PickupCatalogue.PostcardPickup));
PlayerProgressData back11 = Clone(save11);
Check(!back11.Normalize() && InventoryRules.DisplayedAt(back11.campaign.inventory, anchor11) == InventoryCatalogue.Postcard &&
      InventoryRules.CarriedCount(back11.campaign.inventory, InventoryCatalogue.ReviewVoucher) == 2 &&
      InventoryRules.DrawerCount(back11.campaign.inventory, InventoryCatalogue.ReviewVoucher) == 0 &&
      back11.campaign.rewardClaims.Contains("t11/Level_01/coast_tales_postcard/pickup"),
    "display placement, carried vouchers (two; the third was not taken) and the pickup claim round-trip through the save");
var nmDisp11 = Fresh11();
nmDisp11.displays.Add(new InventoryDisplay { anchorId = "a", itemId = "ghost_keepsake" });
nmDisp11.displays.Add(new InventoryDisplay { anchorId = "b", itemId = InventoryCatalogue.Notes });
nmDisp11.displays.Add(new InventoryDisplay { anchorId = "c", itemId = InventoryCatalogue.Postcard });
nmDisp11.bag[0] = new InventorySlot { itemId = InventoryCatalogue.Postcard, quantity = 1 };
var nmWarn11 = new List<string>();
InventoryRules.Normalize(nmDisp11, cat11, nmWarn11);
Check(nmDisp11.displays.Count == 0 && nmWarn11.Count == 1 && nmWarn11[0].Contains("ghost_keepsake") &&
      InventoryRules.Find(nmDisp11, InventoryCatalogue.Notes).IsSlot && nmDisp11.bag.Count(s => s.itemId == InventoryCatalogue.Postcard) == 1 &&
      InventoryRules.DrawerCount(nmDisp11, InventoryCatalogue.Notes) == 0,
    "normalize: unknown or non-keepsake displays are removed (a required item returns to a slot) and a keepsake is kept once");

// ---------- pickup catalogue, availability, claims, placement ----------
PickupCatalogue picks11 = PickupCatalogue.BuiltIn();
PickupDefinition postcardPick11 = picks11.Find(PickupCatalogue.PostcardPickup);
PickupDefinition voucherPick11 = picks11.Find(PickupCatalogue.VoucherPickup);
Check(picks11.Problems(cat11).Count == 0 && postcardPick11.itemId == InventoryCatalogue.Postcard && voucherPick11.itemId == InventoryCatalogue.ReviewVoucher &&
      postcardPick11.availableAfter == "" && voucherPick11.availableAfter == "event:ReceptionistTalkedTo" &&
      picks11.FindDisplay(PickupCatalogue.DeskDisplay).category == InventoryCatalogue.Keepsakes &&
      picks11.pickups.All(p => p.spot.parent.StartsWith("Environment/OfficeBuilding/Rooms/Reception")) && postcardPick11.spot.near != voucherPick11.spot.near,
    "built-in pickups: the postcard from the start and the voucher after the receptionist's introduction, on separate reception trays; the desk keepsake stand");
var filePicks11 = JsonSerializer.Deserialize<PickupCatalogue>(File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Campaign", "pickups.json")), json);
bool samePicks11 = filePicks11 != null && filePicks11.pickups.Count == picks11.pickups.Count && filePicks11.displays.Count == picks11.displays.Count &&
    filePicks11.pickups.All(f => { var b = picks11.Find(f.id); return b != null && b.itemId == f.itemId && b.levelId == f.levelId && b.quantity == f.quantity &&
        b.availableAfter == f.availableAfter && b.title == f.title && b.plaque == f.plaque && b.lines.SequenceEqual(f.lines) &&
        b.spot.parent == f.spot.parent && b.spot.surface == f.spot.surface && b.spot.near == f.spot.near; }) &&
    filePicks11.displays.All(f => { var b = picks11.FindDisplay(f.id); return b != null && b.category == f.category && b.title == f.title && b.spot.surface == f.spot.surface; });
Check(samePicks11 && filePicks11.Problems(cat11).Count == 0, "Resources/Campaign/pickups.json matches the built-in fallback and has no problems");
var badPicks11 = new PickupCatalogue();
badPicks11.pickups.Add(new PickupDefinition { id = "p", itemId = "ghost", levelId = "Level_01", title = "T", spot = new PickupSpot { parent = "A", surface = "B" } });
badPicks11.pickups.Add(new PickupDefinition { id = "p", itemId = InventoryCatalogue.CaseFolder, levelId = "", title = "", quantity = 0, availableAfter = "later" });
badPicks11.displays.Add(new DisplayDefinition { id = "d", category = "Furniture" });
List<string> badPickProblems11 = badPicks11.Problems(cat11);
Check(badPickProblems11.Any(p => p.Contains("unknown item")) && badPickProblems11.Any(p => p.Contains("duplicate pickup")) &&
      badPickProblems11.Any(p => p.Contains("required item")) && badPickProblems11.Any(p => p.Contains("quantity 0")) &&
      badPickProblems11.Any(p => p.Contains("no level")) && badPickProblems11.Any(p => p.Contains("needs a spot")) &&
      badPickProblems11.Any(p => p.Contains("objectives key")) && badPickProblems11.Any(p => p.Contains("unknown category")),
    "pickup problems: unknown or required items, duplicates, quantity, level, spot, waiting key, display category");
string[] answerWords11 = { "Resident", "Citizen", "Alien", "Non-resident", "₱", "tax due", "TaxDue", "Gross" };
Check(picks11.pickups.All(p => answerWords11.All(w => !p.title.Contains(w) && p.lines.All(l => !l.Contains(w)))) &&
      cat11.items.All(i => answerWords11.All(w => !i.description.Contains(w))),
    "pickup and item text is neutral lore: no classification, amount or case value");
var reached11 = new List<string> { "event:SideQuestCompleted" };
Check(PickupRules.IsAvailable(postcardPick11, false, reached11) && !PickupRules.IsAvailable(voucherPick11, false, reached11) &&
      PickupRules.IsAvailable(voucherPick11, false, new List<string> { "event:ReceptionistTalkedTo" }) &&
      !PickupRules.IsAvailable(postcardPick11, true, reached11) && !PickupRules.IsAvailable(null, false, reached11),
    "a pickup appears once its introduction happened and never after it is claimed");
Check(PickupRules.ClaimId("c1", "Level_01", "coast_tales_postcard") == "c1/Level_01/coast_tales_postcard/pickup" &&
      PickupRules.ProfileClaimId("c1", "Level_01", "profile_marie") == "c1/Level_01/profile/profile_marie/pickup" &&
      PickupRules.ClaimId("c2", "Level_01", "coast_tales_postcard") != PickupRules.ClaimId("c1", "Level_01", "coast_tales_postcard"),
    "claims are campaign + level + collectible (CampaignRewards format); profile cards have their own claim ids");

var surface11 = new PickupRules.Box(-0.5f, -0.3f, 0.5f, 0.3f);
bool emptyOk11 = PickupRules.BestSpot(surface11, 0.1f, 0.08f, new List<PickupRules.Box>(), 0.02f, out float ex11, out float ez11);
var middle11 = new List<PickupRules.Box> { new PickupRules.Box(-0.15f, -0.3f, 0.15f, 0.3f) };
bool midOk11 = PickupRules.BestSpot(surface11, 0.1f, 0.08f, middle11, 0.02f, out float mx11, out float mz11);
PickupRules.BestSpot(surface11, 0.1f, 0.08f, middle11, 0.02f, out float mx11b, out float mz11b);
var blocked11 = new List<PickupRules.Box> { new PickupRules.Box(-0.6f, -0.4f, 0.6f, 0.4f) };
Check(emptyOk11 && Math.Abs(ex11) < 0.01f && Math.Abs(ez11) < 0.01f &&
      midOk11 && Math.Abs(mx11) >= 0.25f && Math.Abs(mx11) + 0.1f <= 0.48f + 1e-4f && mx11 == mx11b && mz11 == mz11b &&
      !PickupRules.BestSpot(surface11, 0.1f, 0.08f, blocked11, 0.02f, out _, out _) &&
      !PickupRules.BestSpot(new PickupRules.Box(0f, 0f, 0.1f, 0.1f), 0.1f, 0.1f, null, 0.02f, out _, out _),
    "placement: an empty table gives its centre; props push the tray to the side with most room, inside the edge margin, the same every time; no room = no pickup");
Check(PickupRules.Gap(new PickupRules.Box(0, 0, 1, 1), new PickupRules.Box(2, 0, 3, 1)) == 1f &&
      PickupRules.Gap(new PickupRules.Box(0, 0, 1, 1), new PickupRules.Box(0.5f, 0.5f, 2, 2)) <= 0f &&
      Math.Abs(PickupRules.Gap(new PickupRules.Box(0, 0, 1, 1), new PickupRules.Box(4, 5, 5, 6)) - 5f) < 1e-4f,
    "box gap: side distance, overlap at or below zero, corner distance");

// ---------- wiring (source contracts; the world objects and taps are Unity runtime paths) ----------
string manager11 = Src("RewardsSystem", "PlayerProgressManager.cs");
string commitPickup11 = Between(manager11, "public InventoryResult CommitPickup(", "public System.Collections.Generic.List<string> CollectedProfileIds()");
Check(commitPickup11.Contains("if (claimed && onceOnly) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied };") &&
      Index(commitPickup11, "candidate.campaign.inventory = inventory;") < Index(commitPickup11, "TryWriteCandidate(candidate)") &&
      Index(commitPickup11, "candidate.campaign.rewardClaims.Add(claimId);") < Index(commitPickup11, "TryWriteCandidate(candidate)") &&
      Index(commitPickup11, "TryWriteCandidate(candidate)") < Index(commitPickup11, "InventoryChanged?.Invoke()") &&
      commitPickup11.Contains("result.Outcome = InventoryOutcome.SaveFailed;") && commitPickup11.Contains("SessionClaims.Add(claimId);"),
    "a pickup's item and claim are written in one candidate before anything is published; a failed write changes neither; temporary runs keep session claims");
string pickup11 = Src("SideQuest1", "TaxpayerProfileWorldPickup.cs");
string click11 = Between(pickup11, "public void OnPointerClick(", "public TaxpayerProfile Profile =>");
Check(Index(click11, "if (CollectGate != null && !CollectGate(profileData)) return;") > 0 &&
      Index(click11, "if (CollectGate != null && !CollectGate(profileData)) return;") < Index(click11, "QueueInteractionSFX(\"paper\")") &&
      Index(click11, "if (CollectGate != null && !CollectGate(profileData)) return;") < Index(click11, "isCollecting = true;") &&
      click11.Contains("if (isCollecting && collectionHUD != null)") && pickup11.Contains("private float collectionDuration = 0.45f;") &&
      pickup11.Contains("private const float LiftHeight = 0.08f;"),
    "a profile card is committed before any sound or motion (a failed save leaves it touchable); it lifts 8 cm and glides in 0.45 s; a second tap lands it");
string hotbar11 = Src("UI", "HUD", "HudHotbar.cs");
string commitProfile11 = Between(hotbar11, "private bool CommitProfile(TaxpayerProfile profile)", "public InventoryResult CollectPickup(");
Check(commitProfile11.Contains("if (held != null && held.contents.Contains(id) && InventoryRules.Owns(current, InventoryCatalogue.ProfileBinder)) return true;") &&
      commitProfile11.Contains("Manager.ProfileClaimId(OfficeLevelCatalog.Level1Id, id), false") && commitProfile11.Contains("return false;") &&
      !commitProfile11.Contains("CorrectClassification") && hotbar11.Contains("TaxpayerProfileWorldPickup.CollectGate = CommitProfile;"),
    "all four profile cards go through one owner: the Profile Binder records the id and claim once (no second copy, no answer key)");
string collectibles11 = Src("SideQuest1", "WorldCollectibles.cs");
string collect11 = Between(collectibles11, "private void Collect(WorldCollectible pickup)", "private void Leave(WorldCollectible pickup)");
Check(Index(collect11, "hotbar.CollectPickup(pickup.Definition)") < Index(collect11, "pickup.FlyTo(") &&
      Index(collect11, "InventoryOutcome.SaveFailed") < Index(collect11, "pickup.FlyTo(") && Between(collect11, "InventoryOutcome.SaveFailed", "return;").Contains("card.SetMessage("),
    "an optional pickup flies away only after its save succeeded; a failed save keeps it in place with a message");
Check(collectibles11.Contains("bool claimed = manager.IsPickupClaimed(manager.PickupClaimId(pickup.levelId, pickup.id));") &&
      collectibles11.Contains("PickupRules.IsAvailable(pickup, false, reached)") && collectibles11.Contains("collider.size") &&
      collectibles11.Contains("You already carry two vouchers. Use one, then come back for this one.") && !collectibles11.Contains("Desk Drawer"),
    "claimed pickups never appear again, unclaimed ones wait for their introduction, each has a collider, and a voucher past the carry limit is refused before collecting");
string[] forbidden11 = { "CampaignRewards.Grant", "CommitFieldAssignment", "LevelScoreLedger", "AddExp", "RecordBadge", "publicTrust", "campaign.coins" };
Check(forbidden11.All(w => collectibles11.IndexOf(w, StringComparison.Ordinal) < 0 && commitPickup11.IndexOf(w, StringComparison.Ordinal) < 0),
    "optional pickups grant no coins, trust, XP or badge");
string restore11 = Src("SideQuest1", "Level1FieldCheckpointer.cs");
Check(restore11.Contains("foreach (string id in Progress.CollectedProfileIds())") &&
      restore11.Contains("saved.talkedNpcIds.Contains(owned.NpcId) && !saved.collectedProfileIds.Contains(id)"),
    "re-entry presents binder-owned profile cards as collected (only after their conversation), never as a new copy");
string sceneText11 = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Office_Level1.unity"));
string pickupGuid11 = "46517ecf6af34ed40b4bfb64ba4e6f41";
string prefabText11 = File.ReadAllText(Path.Combine(root, "Assets", "Prefabs", "ProfileWorldPickup_TEMP.prefab"));
int pickupBlock11 = Index(prefabText11, pickupGuid11);
string pickupGo11 = pickupBlock11 > 0 ? System.Text.RegularExpressions.Regex.Matches(prefabText11.Substring(0, pickupBlock11), @"m_GameObject: \{fileID: (\d+)\}").Last().Groups[1].Value : "";
string goBody11 = pickupGo11.Length > 0 ? Between(prefabText11, "&" + pickupGo11 + "\n", "--- !u!") : "";
if (goBody11.Length == 0 && pickupGo11.Length > 0) goBody11 = Between(prefabText11, "&" + pickupGo11 + "\r\n", "--- !u!");
var colliderIds11 = System.Text.RegularExpressions.Regex.Matches(goBody11, @"component: \{fileID: (\d+)\}").Select(m => m.Groups[1].Value)
    .Where(id => prefabText11.Contains("--- !u!65 &" + id) || prefabText11.Contains("--- !u!64 &" + id) || prefabText11.Contains("--- !u!135 &" + id)).ToList();
Check(System.Text.RegularExpressions.Regex.Matches(sceneText11, pickupGuid11).Count == 4 && colliderIds11.Count >= 1,
    "static scene check: the four profile pickups in Office_Level1 are instances of a prefab whose pickup object has a collider");

// =====================================================================
// T12: Supply Counter, purchases, slot upgrades and bookmark pins
// =====================================================================
ShopCatalogue shop12 = ShopCatalogue.BuiltIn();
InventoryCatalogue items12 = InventoryCatalogue.BuiltIn();
string Save12(PlayerProgressData d) => JsonSerializer.Serialize(d, json);
PlayerProgressData Wallet12(int coins, bool introduced = true)
{
    PlayerProgressData d = PlayerProgressData.CreateNewCampaign("t12", "");
    d.campaign.coins = coins;
    d.campaign.shopIntroduced = introduced;
    return d;
}

// ---------- catalogue ----------
ShopProduct bookmarks12 = shop12.Find(ShopCatalogue.BookmarkSet), satchel12 = shop12.Find(ShopCatalogue.Satchel), pouch12 = shop12.Find(ShopCatalogue.BeltPouch);
ShopProduct big12 = shop12.Find(ShopCatalogue.BigSatchel), second12 = shop12.Find(ShopCatalogue.SecondPouch);
Check(shop12.Problems(items12).Count == 0 && bookmarks12.price == 40 && bookmarks12.IsItem && bookmarks12.itemId == InventoryCatalogue.BookmarkSet &&
      satchel12.price == 50 && satchel12.area == "Bag" && satchel12.capacity == 8 && pouch12.price == 60 && pouch12.area == "Hotbar" && pouch12.capacity == 4 &&
      big12.requires == "chapter:Level_02" && big12.requiresLabel == "Requires corrected Level 2" && big12.capacity == 12 &&
      second12.requires == "chapter:Level_02" && second12.capacity == 5 && second12.after == ShopCatalogue.BeltPouch,
    "catalogue: Bookmark Set 40, Satchel 50 (bag 8), Belt Pouch 60 (bar 4); Large Satchel (12) and Second Pouch (5) are teasers needing corrected Level 2");
var file12 = JsonSerializer.Deserialize<ShopCatalogue>(File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Campaign", "shop.json")), json);
bool sameShop12 = file12 != null && file12.products.Count == shop12.products.Count &&
    file12.products.All(f => { var b = shop12.Find(f.id); return b != null && b.kind == f.kind && b.itemId == f.itemId && b.area == f.area && b.capacity == f.capacity &&
        b.price == f.price && b.title == f.title && b.tagline == f.tagline && b.effect == f.effect && b.icon == f.icon && b.requires == f.requires &&
        b.requiresLabel == f.requiresLabel && b.after == f.after; }) &&
    file12.counter.entrance.SequenceEqual(shop12.counter.entrance) && file12.counter.fallback == shop12.counter.fallback && file12.counter.toward == shop12.counter.toward &&
    file12.counter.distances.SequenceEqual(shop12.counter.distances) && file12.counter.offsets.SequenceEqual(shop12.counter.offsets) &&
    file12.counter.flip == shop12.counter.flip && file12.counter.useFixed == shop12.counter.useFixed;
Check(sameShop12 && file12.Problems(items12).Count == 0, "Resources/Campaign/shop.json matches the built-in fallback (products and counter placement) and has no problems");
var badShop12 = new ShopCatalogue();
badShop12.products.Add(new ShopProduct { id = "x", kind = "item", itemId = "ghost", price = 10, title = "X" });
badShop12.products.Add(new ShopProduct { id = "x", kind = "upgrade", area = "Bag", capacity = 40, price = 0, title = "" });
badShop12.products.Add(new ShopProduct { id = "y", kind = "upgrade", area = "Roof", capacity = 4, price = 5, title = "Y", requires = "level:2", after = "nope" });
badShop12.products.Add(new ShopProduct { id = "z", kind = "item", itemId = InventoryCatalogue.CaseFolder, price = 5, title = "Z", requires = "chapter:Level_02" });
List<string> badShopProblems12 = badShop12.Problems(items12);
Check(badShopProblems12.Any(p => p.Contains("unknown item")) && badShopProblems12.Any(p => p.Contains("duplicate product")) && badShopProblems12.Any(p => p.Contains("price 0")) &&
      badShopProblems12.Any(p => p.Contains("raises the bag to 40")) && badShopProblems12.Any(p => p.Contains("unknown area")) && badShopProblems12.Any(p => p.Contains("use chapter:")) &&
      badShopProblems12.Any(p => p.Contains("unknown product \"nope\"")) && badShopProblems12.Any(p => p.Contains("required item")) && badShopProblems12.Any(p => p.Contains("requiresLabel")) &&
      badShopProblems12.Any(p => p.Contains("counter placement needs")),
    "catalogue problems: unknown/required items, duplicates, prices, capacity ceilings, areas, requirement syntax, chains, teaser labels, counter placement");
Check(items12.Find(InventoryCatalogue.BookmarkSet) is InventoryItemDefinition set12 && set12.category == InventoryCatalogue.ToolsAndSupplies && !set12.required && set12.stackLimit == 1 && set12.scope == "",
    "the bookmark packet is a Tools & Supplies item kept for the whole campaign");

// ---------- status and reasons ----------
PlayerProgressData closed12 = Wallet12(160, introduced: false);
PlayerProgressData broke12 = Wallet12(0);
PlayerProgressData rich12 = Wallet12(160);
Check(ShopRules.StatusOf(closed12, shop12, satchel12, true) == ShopStatus.ShopClosed &&
      ShopRules.StatusOf(rich12, shop12, satchel12, false) == ShopStatus.BrowseOnly &&
      ShopRules.StatusOf(broke12, shop12, bookmarks12, true) == ShopStatus.TooExpensive &&
      ShopRules.ReasonFor(ShopStatus.TooExpensive, bookmarks12, broke12, shop12) == "You need 40 more coins." &&
      ShopRules.StatusOf(rich12, shop12, satchel12, true) == ShopStatus.Available &&
      ShopRules.StatusOf(rich12, shop12, big12, true) == ShopStatus.Teaser && ShopRules.ReasonFor(ShopStatus.Teaser, big12, rich12, shop12) == "Requires corrected Level 2",
    "status: closed before the introduction, browse-only outside the story, too expensive says how many more coins, teasers say what they need");
PlayerProgressData later12 = Wallet12(500);
later12.campaign.MarkChapterCompleted("Level_02");
Check(ShopRules.StatusOf(later12, shop12, second12, true) == ShopStatus.NeedsPrevious && ShopRules.StatusOf(later12, shop12, big12, true) == ShopStatus.NeedsPrevious &&
      ShopRules.ReasonFor(ShopStatus.NeedsPrevious, second12, later12, shop12) == "Buy the Belt Pouch first.",
    "after corrected Level 2 the second tier waits for the first (Belt Pouch, Satchel)");

// ---------- purchases ----------
PlayerProgressData buy12 = Wallet12(120);
int trust12 = buy12.campaign.publicTrust, exp12 = buy12.totalExp;
PurchaseResult satchelBuy12 = ShopRules.Purchase(buy12, shop12, items12, ShopCatalogue.Satchel, "r1");
Check(satchelBuy12.Outcome == InventoryOutcome.Applied && satchelBuy12.CoinsBefore == 120 && satchelBuy12.CoinsAfter == 70 && buy12.campaign.coins == 70 &&
      buy12.campaign.inventory.bagCapacity == 8 && buy12.campaign.inventory.bag.Count == 8 && buy12.campaign.ownedUpgradeIds.Contains(ShopCatalogue.Satchel) &&
      buy12.campaign.ledger.Last().amount == -50 && buy12.campaign.ledger.Last().balanceAfter == 70 && buy12.campaign.ledger.Last().reason == "Bought Satchel" &&
      buy12.campaign.rewardClaims.Contains(ShopRules.PurchaseClaimId("t12", "r1")),
    "buying the Satchel debits 50 with a ledger line, records ownership and the receipt, and the bag has 8 slots in the same candidate");
string afterSatchel12 = Save12(buy12);
PurchaseResult again12 = ShopRules.Purchase(buy12, shop12, items12, ShopCatalogue.Satchel, "r2");
PurchaseResult retry12 = ShopRules.Purchase(buy12, shop12, items12, ShopCatalogue.Satchel, "r1");
Check(again12.Outcome == InventoryOutcome.Rejected && again12.Message == "You already own this." && retry12.Outcome == InventoryOutcome.AlreadyApplied &&
      Save12(buy12) == afterSatchel12,
    "a second Satchel is refused and a retry of the same request is already applied: never a second charge or slot");
PurchaseResult pouchBuy12 = ShopRules.Purchase(buy12, shop12, items12, ShopCatalogue.BeltPouch, "r3");
PurchaseResult setBuy12 = ShopRules.Purchase(buy12, shop12, items12, ShopCatalogue.BookmarkSet, "r4");
Check(pouchBuy12.Outcome == InventoryOutcome.Applied && buy12.campaign.inventory.hotbarCapacity == 4 && buy12.campaign.coins == 10 &&
      setBuy12.Outcome == InventoryOutcome.Rejected && setBuy12.Message == "You need 30 more coins." && buy12.campaign.publicTrust == trust12 && buy12.totalExp == exp12,
    "the Belt Pouch gives a fourth bar slot; with 10 coins left the bookmark set says 30 more are needed; buying never changes trust or XP");
PlayerProgressData set12buy = Wallet12(40);
PurchaseResult setOk12 = ShopRules.Purchase(set12buy, shop12, items12, ShopCatalogue.BookmarkSet, "b1");
Check(setOk12.Outcome == InventoryOutcome.Applied && setOk12.Item != null && setOk12.Item.Location == SlotRef.Hotbar(0) && set12buy.campaign.coins == 0 &&
      InventoryRules.Owns(set12buy.campaign.inventory, InventoryCatalogue.BookmarkSet) && InventoryRules.Record(set12buy.campaign.inventory, InventoryCatalogue.BookmarkSet).source == "shop:bookmark_set",
    "the bookmark packet lands in the first free bar slot as an owned item, paid in the same candidate");
PlayerProgressData full12 = Wallet12(100);
CampaignInventoryRecord fullInv12 = full12.campaign.inventory;
for (int i = 0; i < 3; i++) fullInv12.hotbar[i] = new InventorySlot { itemId = InventoryCatalogue.Notes, quantity = 1 };
for (int i = 0; i < 5; i++) fullInv12.bag[i] = new InventorySlot { itemId = InventoryCatalogue.ReviewVoucher, quantity = 1 };
PlayerProgressData busy12 = JsonSerializer.Deserialize<PlayerProgressData>(Save12(full12), json);
busy12.campaign.inventory.hand = new InventorySlot { itemId = InventoryCatalogue.TaxCodeBook, quantity = 1 };
string busySnap12 = Save12(busy12);
PurchaseResult roomless12 = ShopRules.Purchase(busy12, shop12, items12, ShopCatalogue.BookmarkSet, "f1");
PurchaseResult makeRoom12 = ShopRules.Purchase(full12, shop12, items12, ShopCatalogue.BookmarkSet, "f2");
Check(roomless12.Outcome == InventoryOutcome.Rejected && Save12(busy12) == busySnap12 &&
      makeRoom12.Outcome == InventoryOutcome.Applied && makeRoom12.Item.Outcome == InventoryOutcome.NeedsRoom && full12.campaign.inventory.hand.itemId == InventoryCatalogue.BookmarkSet,
    "full bar and bag: the packet is held for Make room; with the hand also busy the purchase is refused and nothing is spent");
PlayerProgressData closedBuy12 = Wallet12(160, introduced: false);
string closedSnap12 = Save12(closedBuy12);
PlayerProgressData teaserBuy12 = Wallet12(500);
string teaserSnap12 = Save12(teaserBuy12);
PlayerProgressData brokeBuy12 = Wallet12(39);
string brokeSnap12 = Save12(brokeBuy12);
Check(ShopRules.Purchase(closedBuy12, shop12, items12, ShopCatalogue.Satchel, "c1").Message == ShopRules.ShopClosedMessage && Save12(closedBuy12) == closedSnap12 &&
      ShopRules.Purchase(teaserBuy12, shop12, items12, ShopCatalogue.BigSatchel, "c2").Message == "Requires corrected Level 2" && Save12(teaserBuy12) == teaserSnap12 &&
      ShopRules.Purchase(brokeBuy12, shop12, items12, ShopCatalogue.BookmarkSet, "c3").Outcome == InventoryOutcome.Rejected && Save12(brokeBuy12) == brokeSnap12 &&
      ShopRules.Purchase(teaserBuy12, shop12, items12, "ghost", "c4").Outcome == InventoryOutcome.Rejected &&
      ShopRules.Purchase(new PlayerProgressData(), shop12, items12, ShopCatalogue.Satchel, "c5").Outcome == InventoryOutcome.Rejected &&
      ShopRules.Purchase(teaserBuy12, shop12, items12, ShopCatalogue.Satchel, " ").Outcome == InventoryOutcome.Rejected,
    "refusals change nothing: shop not introduced, teaser, too few coins, unknown product, no campaign, no request id");
PlayerProgressData roundTrip12 = JsonSerializer.Deserialize<PlayerProgressData>(Save12(buy12), json);
Check(!roundTrip12.Normalize() && roundTrip12.campaign.shopIntroduced && roundTrip12.campaign.inventory.bagCapacity == 8 &&
      roundTrip12.campaign.inventory.hotbarCapacity == 4 && roundTrip12.campaign.coins == 10 && roundTrip12.campaign.ownedUpgradeIds.Count == 2,
    "the introduction, coins, ownership and upgraded capacities survive the save round trip");

// ---------- bookmark pins ----------
var pins12 = new CampaignInventoryRecord();
pins12.Repair();
InventoryRules.Acquire(pins12, items12, InventoryCatalogue.Notes, 1, "p1");
InventoryRules.Acquire(pins12, items12, InventoryCatalogue.CaseFolder, 1, "p2");
InventoryRules.Acquire(pins12, items12, InventoryCatalogue.ProfileBinder, 1, "p3");
InventoryRules.Acquire(pins12, items12, InventoryCatalogue.TaxCodeBook, 1, "p4");
Check(InventoryRules.Pin(pins12, items12, InventoryCatalogue.Notes).Outcome == InventoryOutcome.Rejected && pins12.pins.Count == 0,
    "without the Bookmark Set nothing can be pinned");
InventoryRules.Acquire(pins12, items12, InventoryCatalogue.BookmarkSet, 1, "p5");
InventoryRules.Acquire(pins12, items12, InventoryCatalogue.ReviewVoucher, 1, "p6");
bool pinThree12 = InventoryRules.Pin(pins12, items12, InventoryCatalogue.Notes).Changed && InventoryRules.Pin(pins12, items12, InventoryCatalogue.CaseFolder).Changed &&
                  InventoryRules.Pin(pins12, items12, InventoryCatalogue.ProfileBinder).Changed;
InventoryResult fourth12 = InventoryRules.Pin(pins12, items12, InventoryCatalogue.TaxCodeBook);
InventoryResult replace12 = InventoryRules.Pin(pins12, items12, InventoryCatalogue.TaxCodeBook, 1);
Check(pinThree12 && fourth12.Message == InventoryRules.PinReplace && replace12.Changed &&
      pins12.pins.SequenceEqual(new[] { InventoryCatalogue.Notes, InventoryCatalogue.TaxCodeBook, InventoryCatalogue.ProfileBinder }) &&
      InventoryRules.Find(pins12, InventoryCatalogue.CaseFolder).IsSlot,
    "three pins; a fourth asks which ribbon to replace; replacing changes only the pin (the record stays where it is)");
Check(InventoryRules.Pin(pins12, items12, InventoryCatalogue.ReviewVoucher).Outcome == InventoryOutcome.Rejected &&
      InventoryRules.Pin(pins12, items12, InventoryCatalogue.BookmarkSet).Outcome == InventoryOutcome.Rejected &&
      InventoryRules.Pin(pins12, items12, InventoryCatalogue.Postcard).Outcome == InventoryOutcome.Rejected &&
      InventoryRules.Pin(pins12, items12, InventoryCatalogue.Notes).Outcome == InventoryOutcome.NoChange &&
      InventoryRules.Unpin(pins12, InventoryCatalogue.TaxCodeBook).Changed && pins12.pins.Count == 2 &&
      InventoryRules.Unpin(pins12, InventoryCatalogue.TaxCodeBook).Outcome == InventoryOutcome.NoChange,
    "supplies, the set itself and unowned records cannot be pinned; pinning twice is a no-change; unpin frees the ribbon");
InventoryRules.Remove(pins12, items12, InventoryCatalogue.CaseFolder, "p7");
pins12.pins.Add(InventoryCatalogue.CaseFolder);
pins12.pins.Add("ghost_record");
var pinWarn12 = new List<string>();
InventoryRules.Normalize(pins12, items12, pinWarn12);
var pinRepair12 = new CampaignInventoryRecord { pins = new List<string> { "a", " a ", "b", "c", "d", "" } };
Check(!pins12.pins.Contains(InventoryCatalogue.CaseFolder) && !pins12.pins.Contains("ghost_record") && pins12.pins.Contains(InventoryCatalogue.Notes) &&
      pinRepair12.Repair() && pinRepair12.pins.SequenceEqual(new[] { "a", "b", "c" }),
    "a returned or unknown record normalizes to unpinned; repair keeps at most three clean pins");

// ---------- menu entry and wiring ----------
PlayerProgressData menu12 = Wallet12(0, introduced: false);
Check(!CampaignMenu.ShopUnlocked(menu12) && !CampaignMenu.ShopUnlocked(null) && CampaignMenu.ShopUnlocked(Wallet12(0)) &&
      CampaignMenu.ShopLockedMessage.Contains("Supply Counter"),
    "the menu's SHOP unlocks once the counter was introduced in the story, and says where the shop is");
string manager12 = Src("RewardsSystem", "PlayerProgressManager.cs");
string commitBuy12 = Between(manager12, "public PurchaseResult CommitPurchase(", "public bool MarkDecorArrived(");
Check(commitBuy12.Contains("if (!CanSpendCoins)") && Index(commitBuy12, "ShopRules.Purchase(candidate") < Index(commitBuy12, "TryWriteCandidate(candidate)") &&
      Index(commitBuy12, "TryWriteCandidate(candidate)") < Index(commitBuy12, "PurchaseCommitted?.Invoke(result)") && commitBuy12.Contains("Purchase not saved; nothing spent."),
    "a purchase is applied to a candidate and written before anything is published; practice/replay/Editor runs only browse; a failed write spends nothing");
Check(Between(manager12, "public bool MarkShopIntroduced()", "public PurchaseResult CommitPurchase(").Contains("TryWriteCandidate(candidate)"),
    "the introduction is its own small save (story runs only)");
string screen12 = Src("UI", "HUD", "SupplyShopScreen.cs");
string buyFlow12 = Between(screen12, "private void OnPrimary()", "private void CancelConfirm()");
Check(Index(buyFlow12, "if (!confirming)") < Index(buyFlow12, "Manager.CommitPurchase(") && Index(buyFlow12, "Manager.CommitPurchase(") < Index(buyFlow12, "Celebrate(product, result);") &&
      Between(buyFlow12, "if (result.Outcome != InventoryOutcome.Applied)", "Celebrate(product, result);").Contains("shakeAt = Time.unscaledTime;") &&
      !Between(buyFlow12, "if (result.Outcome != InventoryOutcome.Applied)", "Celebrate(product, result);").Contains("PlayEffect"),
    "BUY asks first, commits, and only a saved purchase celebrates; a refusal shakes the button silently");
Check(screen12.Contains("HudWindowLock.Acquire(this);") && screen12.Contains("public static bool PausesClock => Instance != null && Instance.isOpen;") &&
      Src("UI", "HUD", "OfficeHours.cs").Contains("if (SupplyShopScreen.PausesClock) return true;") && screen12.Contains("GameplayEvents.RaiseGuideCue(SupplyCounter.CueOpened);"),
    "the catalogue holds the player, pauses the review clock while open, and tells the guide when it opens and closes");
string[] forbidden12 = { "publicTrust", "AddExp", "CampaignRewards.Grant", "CommitFieldAssignment", "LevelScoreLedger" };
Check(forbidden12.All(w => screen12.IndexOf(w, StringComparison.Ordinal) < 0 && Src("Interactables", "SupplyCounter.cs").IndexOf(w, StringComparison.Ordinal) < 0 &&
      Src("RewardsSystem", "CampaignShop.cs").IndexOf(w, StringComparison.Ordinal) < 0),
    "browsing or buying never awards trust or XP and never repeats a story reward");
string counter12 = Src("Interactables", "SupplyCounter.cs");
Check(counter12.Contains("CampaignShopAssets.Load()") && counter12.Contains("Instantiate(assets.counterPrefab, root.transform)") &&
      counter12.Contains("manager.MarkShopIntroduced()") && counter12.Contains("ViewTurn.Toward(lookTarget.position, TurnSeconds)") &&
      counter12.Contains("public string GetPromptText() => \"Click to browse supplies\";") && counter12.Contains("collider.size = body.size"),
    "the counter is a scoped copy of the Synty prop with one collider; the first story visit introduces the shop; the view turns to it before the catalogue opens");
string assetText12 = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Campaign", "shop_assets.asset"));
var prefabRefs12 = System.Text.RegularExpressions.Regex.Matches(assetText12, @"Prefab: \{fileID: (\d+), guid: ([0-9a-f]{32}), type: 3\}")
    .ToDictionary(m => m.Groups[2].Value, m => m.Groups[1].Value);
var prefabFound12 = new Dictionary<string, string>();
foreach (string meta in Directory.EnumerateFiles(Path.Combine(root, "Assets", "Synty"), "*.prefab.meta", SearchOption.AllDirectories))
{
    var g = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(meta), @"guid: ([0-9a-f]{32})");
    if (g.Success && prefabRefs12.ContainsKey(g.Groups[1].Value)) prefabFound12[g.Groups[1].Value] = meta.Substring(0, meta.Length - 5);
}
bool refsOk12 = prefabRefs12.Count == 5 && prefabRefs12.All(r => prefabFound12.TryGetValue(r.Key, out string prefab) && File.ReadAllText(prefab).Contains("--- !u!1 &" + r.Value));
Check(refsOk12 && assetText12.Contains("counterPrefab: {fileID: 1810563028744556, guid: 8673173cb192e5448943d51b5e2df9bf"),
    "static check: shop_assets.asset points at five real Synty prefabs (counter, shelf, open book, crate, books) by their root objects");
string guide12 = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Tutorials", "Level_01", "guide.json"));
using (JsonDocument guideDoc12 = JsonDocument.Parse(guide12))
{
    var lessons12 = guideDoc12.RootElement.GetProperty("lessons").EnumerateArray().ToList();
    int intro12 = lessons12.FindIndex(l => l.GetProperty("id").GetString() == "shop-intro");
    int follow12 = lessons12.FindIndex(l => l.GetProperty("id").GetString() == "follow");
    int look12 = lessons12.FindIndex(l => l.GetProperty("id").GetString() == "look");
    JsonElement introLesson12 = lessons12[intro12];
    var introPages12 = introLesson12.GetProperty("pages").EnumerateArray().ToList();
    int notes12 = lessons12.FindIndex(l => l.GetProperty("id").GetString() == "notes");
    Check(look12 < notes12 && notes12 < intro12 && intro12 < follow12 && introLesson12.GetProperty("when").GetString() == "lesson:notes" &&
          introPages12[0].GetProperty("face").GetString() == "type:SupplyCounter" && introPages12.Any(p => p.TryGetProperty("waitFor", out var w) && w.GetString() == "cue:ShopOpened") &&
          introPages12.Any(p => p.TryGetProperty("waitFor", out var w2) && w2.GetString() == "cue:ShopClosed") &&
          lessons12[follow12].GetProperty("when").GetString() == "lesson:shop-intro|not:active:type:SupplyCounter" &&
          lessons12[follow12].GetProperty("pages")[0].GetProperty("face").GetString() == "waypoint",
        "guide: the store is introduced right after the chest Notes and before the townsfolk, the view turns to the counter and back to the plaza, and a missing counter never blocks the tutorial");
}
Check(Src("UI", "Guide", "GuideScript.cs").Contains("public string face;") && Src("UI", "Guide", "GuideDirector.cs").Contains("if (faced.HasWorld) ViewTurn.Toward(faced.World, 0.8f);") &&
      Src("Player", "ViewTurn.cs").Contains("view.SetLook(yaw, pitch);") && Src("Player", "FirstPersonCameraController.cs").Contains("public void SetLook(float newYaw, float newPitch)") &&
      Src("SideQuest1", "Level1FieldCheckpointer.cs").Contains("if (lessons.Contains(\"follow\") && !lessons.Contains(\"shop-intro\")) lessons.Add(\"shop-intro\");"),
    "the guide page 'face' turns the view smoothly and hands the angles back to the first-person camera; older saves skip the new lesson");
string pinsUi12 = Src("UI", "HUD", "InventoryScreen.cs");
Check(pinsUi12.Contains("pinButton = MakeButton(body, \"Pin\"") && pinsUi12.Contains("InventoryRules.Pin(rec, Catalogue, item, index)") &&
      pinsUi12.Contains("bool owned = InventoryRules.HasBookmarks(r);") && pinsUi12.Contains("pinTabLabels[i].text = def != null ? def.label : \"(empty)\";"),
    "the Bag shows PINNED ribbons (icon and name) and a PIN / UNPIN action once the set is owned; a fourth pin chooses a ribbon to replace");

// =====================================================================
// T13: one shop in the menu and at the counter; office decor
// =====================================================================
ShopCatalogue shop13 = ShopCatalogue.BuiltIn();
InventoryCatalogue items13 = InventoryCatalogue.BuiltIn();
ShopProduct cup13 = shop13.Find(ShopCatalogue.PencilCup), print13 = shop13.Find(ShopCatalogue.IslandPrint);
ShopProduct standTeaser13 = shop13.Find(ShopCatalogue.ComparisonStand), computer13 = shop13.Find(ShopCatalogue.OfficeComputer);

// ---------- catalogue ----------
Check(cup13.IsDecor && cup13.price == 30 && cup13.section == ShopCatalogue.SectionOffice && cup13.anchors.SequenceEqual(new[] { "desk_corner" }) &&
      print13.IsDecor && print13.price == 60 && print13.anchors.SequenceEqual(new[] { "wall_above_filing" }) &&
      standTeaser13.requires.StartsWith("pending:") && standTeaser13.requiresLabel == "Requires ready Level 2" && standTeaser13.price == 120 &&
      computer13.requires.StartsWith("pending:") && computer13.requiresLabel == "Requires corrected Level 3" && computer13.price == 250 &&
      shop13.InSection(ShopCatalogue.SectionSupplies).Select(p => p.id).SequenceEqual(new[] { "bookmark_set", "review_voucher", "satchel", "belt_pouch", "big_satchel", "second_pouch" }) &&
      shop13.InSection(ShopCatalogue.SectionOffice).Select(p => p.id).SequenceEqual(new[] { "pencil_cup", "island_print", "comparison_stand", "office_computer" }) &&
      shop13.FindAnchor("desk_corner").kind == ShopDecorAnchor.KindSurface && shop13.FindAnchor("desk_corner").side == "right" &&
      shop13.FindAnchor("wall_above_filing").kind == ShopDecorAnchor.KindWall && shop13.Problems(items13).Count == 0,
    "catalogue: pencil cup 30 (desk corner) and island print 60 (wall above the filing shelf) on the OFFICE tab; comparison stand and computer are teasers; supplies keep their tab");
var file13 = JsonSerializer.Deserialize<ShopCatalogue>(File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Campaign", "shop.json")), json);
bool sameShop13 = file13 != null && file13.products.Count == shop13.products.Count && file13.anchors.Count == shop13.anchors.Count &&
    file13.products.All(f => { var b = shop13.Find(f.id); return b != null && b.section == f.section && b.kind == f.kind && b.anchors.SequenceEqual(f.anchors) &&
        b.price == f.price && b.title == f.title && b.tagline == f.tagline && b.effect == f.effect && b.icon == f.icon && b.requires == f.requires &&
        b.requiresLabel == f.requiresLabel; }) &&
    file13.anchors.All(f => { var b = shop13.FindAnchor(f.id); return b != null && b.label == f.label && b.kind == f.kind && b.parent == f.parent &&
        b.target == f.target && b.side == f.side && Math.Abs(b.above - f.above) < 1e-4f; });
Check(sameShop13 && file13.Problems(items13).Count == 0, "shop.json (sections, decor, anchors) matches the built-in fallback and has no problems");
var bad13 = ShopCatalogue.BuiltIn();
bad13.products.Add(new ShopProduct { id = "globe", kind = "decor", section = "office", price = 20, title = "Globe" });
bad13.products.Add(new ShopProduct { id = "lamp", kind = "decor", section = "office", price = 20, title = "Lamp", anchors = new List<string> { "ceiling" } });
bad13.products.Add(new ShopProduct { id = "mat", kind = "decor", section = "lobby", price = 20, title = "Mat", requires = "level:3", requiresLabel = "x" });
bad13.products.Add(new ShopProduct { id = "rug", kind = "decor", section = "office", price = 20, title = "Rug", anchors = new List<string> { "desk_corner" } });
bad13.anchors.Add(new ShopDecorAnchor { id = "odd", label = "", kind = "porch", parent = "", target = "" });
bad13.anchors.Add(new ShopDecorAnchor { id = "high", label = "High", kind = "wall", parent = "a", target = "b", above = 4f });
List<string> badProblems13 = bad13.Problems(items13);
Check(badProblems13.Any(p => p.Contains("decor \"globe\" has no display anchor")) && badProblems13.Any(p => p.Contains("unknown display anchor \"ceiling\"")) &&
      badProblems13.Any(p => p.Contains("unknown section \"lobby\"")) && badProblems13.Any(p => p.Contains("use chapter:<id> or pending:<feature>")) &&
      badProblems13.Any(p => p.Contains("anchor \"odd\" has no label")) && badProblems13.Any(p => p.Contains("unknown kind \"porch\"")) &&
      badProblems13.Any(p => p.Contains("needs a parent and a target")) && badProblems13.Any(p => p.Contains("sits 4 m above")) &&
      badProblems13.Any(p => p.Contains("section \"office\" has more than 6 products")) &&
      !badProblems13.Any(p => p.Contains("comparison_stand")) && !badProblems13.Any(p => p.Contains("office_computer")),
    "catalogue problems: decor anchors, unknown anchors and sections, requirement syntax, anchor fields, six cards a page; pending teasers need no anchor");

// ---------- teasers never sell ----------
PlayerProgressData rich13 = Wallet12(1000);
rich13.campaign.MarkChapterCompleted("Level_02");
rich13.campaign.MarkChapterCompleted("Level_03");
string richSnap13 = Save12(rich13);
PurchaseResult standBuy13 = ShopRules.Purchase(rich13, shop13, items13, ShopCatalogue.ComparisonStand, "s1");
PurchaseResult computerBuy13 = ShopRules.Purchase(rich13, shop13, items13, ShopCatalogue.OfficeComputer, "s2");
Check(ShopRules.StatusOf(rich13, shop13, standTeaser13, true) == ShopStatus.Teaser && ShopRules.StatusOf(rich13, shop13, computer13, true) == ShopStatus.Teaser &&
      standBuy13.Outcome == InventoryOutcome.Rejected && standBuy13.Message == "Requires ready Level 2" &&
      computerBuy13.Outcome == InventoryOutcome.Rejected && computerBuy13.Message == "Requires corrected Level 3" && Save12(rich13) == richSnap13,
    "the comparison stand and computer stay locked teasers (even with 1000 coins and later chapters done) and cannot be bought");

// ---------- decor purchases ----------
PlayerProgressData decor13 = Wallet12(100);
int trust13 = decor13.campaign.publicTrust, exp13 = decor13.totalExp;
string invBefore13 = JsonSerializer.Serialize(decor13.campaign.inventory, json);
PurchaseResult cupBuy13 = ShopRules.Purchase(decor13, shop13, items13, ShopCatalogue.PencilCup, "d1");
PurchaseResult printBuy13 = ShopRules.Purchase(decor13, shop13, items13, ShopCatalogue.IslandPrint, "d2");
Check(cupBuy13.Outcome == InventoryOutcome.Applied && cupBuy13.AnchorId == "desk_corner" && cupBuy13.CoinsAfter == 70 &&
      printBuy13.Outcome == InventoryOutcome.Applied && printBuy13.AnchorId == "wall_above_filing" && decor13.campaign.coins == 10 &&
      DecorRules.AnchorOf(decor13.campaign, ShopCatalogue.PencilCup) == "desk_corner" && DecorRules.ItemAt(decor13.campaign, "wall_above_filing") == ShopCatalogue.IslandPrint &&
      decor13.campaign.decor.All(d => !d.arrived) && decor13.campaign.ledger.Last().reason == "Bought Framed Island Print" && decor13.campaign.ledger.Last().amount == -60 &&
      decor13.campaign.ownedUpgradeIds.Contains(ShopCatalogue.PencilCup) && decor13.campaign.rewardClaims.Contains(ShopRules.PurchaseClaimId("t12", "d2")) &&
      JsonSerializer.Serialize(decor13.campaign.inventory, json) == invBefore13 && decor13.campaign.publicTrust == trust13 && decor13.totalExp == exp13,
    "buying decor debits, records ownership, the receipt and its display anchor in one candidate; it is installed, never put in the bag, and changes no trust or XP");
string decorSnap13 = Save12(decor13);
Check(ShopRules.Purchase(decor13, shop13, items13, ShopCatalogue.PencilCup, "d1").Outcome == InventoryOutcome.AlreadyApplied &&
      ShopRules.Purchase(decor13, shop13, items13, ShopCatalogue.PencilCup, "d3").Message == "You already own this." && Save12(decor13) == decorSnap13,
    "a retried request is already applied and a second pencil cup is refused: never two charges or two cups");
PlayerProgressData reload13 = JsonSerializer.Deserialize<PlayerProgressData>(decorSnap13, json);
Check(!reload13.Normalize() && reload13.campaign.decor.Count == 2 && DecorRules.AnchorOf(reload13.campaign, ShopCatalogue.IslandPrint) == "wall_above_filing" &&
      DecorRules.Resolve(reload13.campaign, shop13).Select(p => p.itemId + "@" + p.anchorId).SequenceEqual(new[] { "pencil_cup@desk_corner", "island_print@wall_above_filing" }),
    "ownership and placement survive the save round trip and resolve to the same anchors");
Check(DecorRules.MarkArrived(reload13.campaign, ShopCatalogue.PencilCup) && !DecorRules.MarkArrived(reload13.campaign, ShopCatalogue.PencilCup) &&
      !DecorRules.MarkArrived(reload13.campaign, "ghost") && reload13.campaign.decor.First(d => d.itemId == ShopCatalogue.PencilCup).arrived,
    "the office arrival is remembered once (presentation only)");

// ---------- anchor occupancy ----------
ShopCatalogue occ13 = ShopCatalogue.BuiltIn();
occ13.anchors.Add(new ShopDecorAnchor { id = "shelf_top", label = "the top of your filing shelf", kind = "surface", parent = "p", target = "t", side = "left" });
occ13.products.Add(new ShopProduct { id = "globe", kind = "decor", section = "office", price = 10, title = "Globe", anchors = new List<string> { "desk_corner" } });
occ13.products.Remove(occ13.Find(ShopCatalogue.OfficeComputer));
PlayerProgressData occupied13 = Wallet12(200);
ShopRules.Purchase(occupied13, occ13, items13, ShopCatalogue.PencilCup, "o1");
string occSnap13 = Save12(occupied13);
PurchaseResult blocked13 = ShopRules.Purchase(occupied13, occ13, items13, "globe", "o2");
Check(blocked13.Outcome == InventoryOutcome.Rejected && blocked13.Message == DecorRules.NoSpotMessage && Save12(occupied13) == occSnap13,
    "a decor piece whose only anchor is taken is refused before any coin moves (never charged and hidden)");
occ13.Find("globe").anchors.Add("shelf_top");
PurchaseResult moved13 = ShopRules.Purchase(occupied13, occ13, items13, "globe", "o3");
Check(moved13.Outcome == InventoryOutcome.Applied && moved13.AnchorId == "shelf_top" && DecorRules.ItemAt(occupied13.campaign, "desk_corner") == ShopCatalogue.PencilCup,
    "with its first anchor taken, a piece goes to its next free anchor; the installed one stays put");
var messy13 = Wallet12(0);
messy13.campaign.ownedUpgradeIds.AddRange(new[] { ShopCatalogue.PencilCup, "globe" });
messy13.campaign.decor.Add(new DecorPlacement { itemId = "globe", anchorId = "desk_corner" });
messy13.campaign.decor.Add(new DecorPlacement { itemId = ShopCatalogue.PencilCup, anchorId = "desk_corner", arrived = true });
messy13.campaign.decor.Add(new DecorPlacement { itemId = ShopCatalogue.IslandPrint, anchorId = "wall_above_filing" });
var unplaced13 = new List<string>();
List<DecorPlacement> resolved13 = DecorRules.Resolve(messy13.campaign, occ13, unplaced13);
occ13.Find(ShopCatalogue.PencilCup).anchors.Add("shelf_top");
List<DecorPlacement> resolvedAgain13 = DecorRules.Resolve(messy13.campaign, occ13);
Check(resolved13.Select(p => p.itemId + "@" + p.anchorId).SequenceEqual(new[] { "globe@desk_corner" }) && unplaced13.SequenceEqual(new[] { ShopCatalogue.PencilCup }) &&
      resolvedAgain13.Select(p => p.itemId + "@" + p.anchorId + (p.arrived ? "!" : "")).SequenceEqual(new[] { "globe@desk_corner", "pencil_cup@shelf_top!" }),
    "resolve: two pieces on one anchor keep save order, the later one moves to its next free anchor (else it is reported), and unowned decor is never shown");
var repair13 = new PlayerProgressData { campaign = CampaignRecord.CreateFresh("r13", "") };
repair13.campaign.decor = new List<DecorPlacement> { null, new DecorPlacement { itemId = " pencil_cup ", anchorId = " desk_corner " },
    new DecorPlacement { itemId = "pencil_cup", anchorId = "x" }, new DecorPlacement { itemId = " " } };
var repairNull13 = new PlayerProgressData { campaign = CampaignRecord.CreateFresh("r13b", "") };
repairNull13.campaign.decor = null;
Check(repair13.Normalize() && repair13.campaign.decor.Count == 1 && repair13.campaign.decor[0].itemId == "pencil_cup" && repair13.campaign.decor[0].anchorId == "desk_corner" &&
      repairNull13.Normalize() && repairNull13.campaign.decor != null,
    "repair: one trimmed placement per piece, blanks dropped, a missing list restored");

// ---------- desk corner placement (pure) ----------
var desk13 = new PickupRules.Box(-0.6f, -0.9f, 0.6f, 0.9f);
var plant13 = new PickupRules.Box(0.2f, -0.9f, 0.45f, -0.7f);
bool rightOk13 = DecorRules.CornerSpot(desk13, 0.045f, 0.045f, new[] { plant13 }, 0.03f, 0.015f, 1f, 0f, 1f, out float cx13, out float cz13);
bool again13 = DecorRules.CornerSpot(desk13, 0.045f, 0.045f, new[] { plant13 }, 0.03f, 0.015f, 1f, 0f, 1f, out float cx13b, out float cz13b);
bool leftOk13 = DecorRules.CornerSpot(desk13, 0.045f, 0.045f, new[] { plant13 }, 0.03f, 0.015f, 1f, 0f, -1f, out float lx13, out float lz13);
var foot13 = new PickupRules.Box(cx13 - 0.045f, cz13 - 0.045f, cx13 + 0.045f, cz13 + 0.045f);
bool noRoom13 = DecorRules.CornerSpot(new PickupRules.Box(0f, 0f, 0.1f, 0.1f), 0.045f, 0.045f, null, 0.03f, 0.015f, 1f, 0f, 1f, out _, out _);
var rightFull13 = new[] { new PickupRules.Box(-0.6f, -0.9f, 0.6f, 0.02f) };
bool jumped13 = DecorRules.CornerSpot(desk13, 0.045f, 0.045f, rightFull13, 0.03f, 0.015f, 1f, 0f, 1f, out _, out _);
Check(rightOk13 && again13 && cx13 == cx13b && cz13 == cz13b && cz13 < 0f && cx13 > 0.3f && PickupRules.Gap(foot13, plant13) >= 0.015f &&
      leftOk13 && lz13 > 0.7f && lx13 > 0.4f && !noRoom13 && !jumped13,
    "desk corner: seated looking along +x, 'right' is the -z side; the cup takes the free spot nearest the far corner, clear of the plant, the same every time; 'left' mirrors it; a full side or no room = no spot (never the other side)");

// ---------- the menu: same catalogue, same wallet, refused when full ----------
var full13 = new CampaignInventoryRecord();
full13.Repair();
for (int i = 0; i < full13.hotbarCapacity; i++) full13.hotbar[i] = new InventorySlot { itemId = InventoryCatalogue.Notes, quantity = 1 };
for (int i = 0; i < full13.bagCapacity; i++) full13.bag[i] = new InventorySlot { itemId = InventoryCatalogue.ReviewVoucher, quantity = 1 };
string fullSnap13 = JsonSerializer.Serialize(full13, json);
InventoryResult fullBuy13 = InventoryRules.AcquireWithoutHand(full13, items13, InventoryCatalogue.BookmarkSet, 1, "m1");
var roomy13 = new CampaignInventoryRecord();
roomy13.Repair();
InventoryResult slotted13 = InventoryRules.AcquireWithoutHand(roomy13, items13, InventoryCatalogue.BookmarkSet, 1, "m2");
Check(fullBuy13.Outcome == InventoryOutcome.Rejected && fullBuy13.Message == InventoryRules.BagFull && JsonSerializer.Serialize(full13, json) == fullSnap13 &&
      full13.hand.IsEmpty && full13.drawer.Count == 0 && !InventoryRules.Owns(full13, InventoryCatalogue.BookmarkSet) &&
      slotted13.Location == SlotRef.Hotbar(0),
    "with no hand to hold it (the menu), a full bar and bag refuse a bought item (\"Your bar and bag are full\"); with room it takes a slot as usual");
PlayerProgressData menuBuy13 = Wallet12(40);
menuBuy13.campaign.inventory = JsonSerializer.Deserialize<CampaignInventoryRecord>(fullSnap13, json);
PlayerProgressData counterBuy13 = JsonSerializer.Deserialize<PlayerProgressData>(Save12(menuBuy13), json);
string menuSnap13 = Save12(menuBuy13);
PurchaseResult viaMenu13 = ShopRules.Purchase(menuBuy13, shop13, items13, ShopCatalogue.BookmarkSet, "q1", noHand: true);
PurchaseResult viaCounter13 = ShopRules.Purchase(counterBuy13, shop13, items13, ShopCatalogue.BookmarkSet, "q1");
Check(viaMenu13.Outcome == InventoryOutcome.Rejected && viaMenu13.Message == InventoryRules.BagFull + "." && Save12(menuBuy13) == menuSnap13 && menuBuy13.campaign.coins == 40 &&
      viaCounter13.Outcome == InventoryOutcome.Applied && viaCounter13.Item.Outcome == InventoryOutcome.NeedsRoom && counterBuy13.campaign.coins == 0,
    "a full bag blocks the menu purchase (no coins spent, nothing changes); the counter holds it in hand for Make room");
string manager13 = Src("RewardsSystem", "PlayerProgressManager.cs");
string canShop13 = Between(manager13, "public bool CanShopFromMenu", "public CampaignInventoryRecord Inventory");
string commit13 = Between(manager13, "public PurchaseResult CommitPurchase(", "public bool MarkDecorArrived(");
Check(canShop13.Contains("CampaignSave.CanContinue(campaignState)") && canShop13.Contains("CampaignRun.Chosen == CampaignRunMode.None") &&
      canShop13.Contains("!TrainingSession.IsPracticing") && canShop13.Contains("!MenuEntry.IsStoryLaunching") && canShop13.Contains("MenuEntry.MenuSceneName") &&
      canShop13.Contains("public bool CanSpendCoins => UsesStoryInventory || CanShopFromMenu;") &&
      commit13.Contains("if (!CanSpendCoins)") && commit13.Contains("noHand: !UsesStoryInventory") &&
      Index(commit13, "ShopRules.Purchase(candidate") < Index(commit13, "TryWriteCandidate(candidate)"),
    "the menu between runs may spend the same campaign wallet (no run chosen, nothing launching, no drill); level scenes still need a story run");
Check(Between(manager13, "public bool MarkDecorArrived(", "/// <summary>The profile ids").Contains("if (!UsesStoryInventory) return false;"),
    "only story runs remember a decor arrival");
string presenter13 = Src("UI", "Menu", "CareerBoardPresenter.cs");
string openShop13 = Between(presenter13, "private void OpenShop()", "private void ShowShopNotice(");
Check(!presenter13.Contains("ShopAtCounterMessage") && openShop13.Contains("ShowShopNotice(CampaignMenu.ShopLockedMessage);") &&
      Index(openShop13, "CampaignMenu.ShopUnlocked(") < Index(openShop13, "SupplyShopScreen.CreateStandalone()") &&
      openShop13.Contains("menuShop.Open(firstVisit: false);") && presenter13.Contains("ApplyInputGate(ready && !ShopOpen);") &&
      presenter13.Contains("if (progressDirty && ready && !ShopOpen)") && presenter13.Contains("if (menuShop != null) Destroy(menuShop.gameObject);"),
    "the menu's SHOP opens the same catalogue book once unlocked (locked: the requirement only); the board waits behind it and catches up after");
var introducers13 = Directory.EnumerateFiles(Path.Combine(root, "Assets", "Scripts"), "*.cs", SearchOption.AllDirectories)
    .Where(f => File.ReadAllText(f).Contains("MarkShopIntroduced()") && !f.EndsWith("PlayerProgressManager.cs")).Select(Path.GetFileName).ToList();
Check(introducers13.SequenceEqual(new[] { "SupplyCounter.cs" }) && !File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Tutorials", "Level_01", "guide.json")).Contains("shopIntroduced"),
    "only a real visit to the Supply Counter introduces the shop (never a guide page, a camera turn or an old result)");

// ---------- the catalogue book and the office ----------
string screen13 = Src("UI", "HUD", "SupplyShopScreen.cs");
Check(screen13.Contains("public static SupplyShopScreen CreateStandalone()") && screen13.Contains("scaler.referenceResolution = new Vector2(1920f, 1080f);") &&
      screen13.Contains("events.AddComponent<InputSystemUIInputModule>();") && screen13.Contains("private bool CanSpend => Manager != null && Manager.CanSpendCoins;") &&
      screen13.Contains("button.onClick.AddListener(() => SelectSection(id, playSound: true));") && screen13.Contains("Keyboard.current.escapeKey.wasPressedThisFrame") &&
      Between(screen13, "private void Celebrate(", "private void StartFlight(").Contains("if (stage != null) stage.Settle();") &&
      Index(Between(screen13, "private void OnPrimary()", "private void CancelConfirm()"), "Manager.CommitPurchase(") <
      Index(Between(screen13, "private void OnPrimary()", "private void CancelConfirm()"), "Celebrate(product, result);"),
    "the book: a menu version with its own canvas, two tabs, Back steps out, and the miniature settles only after a saved purchase");
string stage13 = Src("UI", "HUD", "ShopPreviewStage.cs");
Check(stage13.Contains("private const float SwingDegrees = 40f;") && stage13.Contains("Settle() => settleAt") && stage13.Contains("(now - settleAt) / 0.3f") &&
      stage13.Contains("stageCamera.enabled = false;") && stage13.Contains("stageCamera.cullingMask = 1 << layer;") && stage13.Contains("texture.Release();"),
    "the miniature turns at most a quarter turn (+/-40 degrees), settles in 0.3 s, renders only while shown, on its own layer, and frees its texture");
string office13 = Src("Interactables", "OfficeDecor.cs");
string landed13 = Between(office13, "private void Animate(", "private static void SetVisible(");
Check(office13.Contains("private const float ArrivalSeconds = 0.3f;") && office13.Contains("PlayerProgressManager.ProgressChanged += OnProgress;") &&
      office13.Contains("DecorRules.Resolve(data.campaign, catalogue, unplaced)") && office13.Contains("DecorRules.CornerSpot(") &&
      office13.Contains("WorldCollectibles.ResolveSpot(scene, display.spot, KeepsakeStand.Footprint") &&
      Index(landed13, "if (t < 1f) return;") < Index(landed13, "PlayEffect(\"sfx_card_place_02\"") &&
      Index(landed13, "PlayEffect(\"sfx_card_place_02\"") < Index(landed13, "manager.MarkDecorArrived(piece.ItemId)") &&
      Src("UI", "HUD", "HudHotbar.cs").Contains("decor = OfficeDecor.Create(gameObject.scene);") &&
      Src("SideQuest1", "WorldCollectibles.cs").Contains("if (renderer.GetComponentInParent<OfficeDecorPiece>() != null) continue;"),
    "the office follows the committed save: pieces resolve to their anchors, avoid the keepsake stand, arrive in 0.3 s with one soft sound as they land, then are remembered");
string[] forbidden13 = { "publicTrust", "AddExp", "CampaignRewards.Grant", "LevelScoreLedger", "MarkChapterCompleted" };
Check(forbidden13.All(w => office13.IndexOf(w, StringComparison.Ordinal) < 0 && stage13.IndexOf(w, StringComparison.Ordinal) < 0 &&
      Src("Interactables", "DecorModels.cs").IndexOf(w, StringComparison.Ordinal) < 0 && screen13.IndexOf(w, StringComparison.Ordinal) < 0),
    "decor grants no trust, XP, badge or chapter completion");
string assets13 = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Campaign", "shop_assets.asset"));
string material13 = File.ReadAllText(Path.Combine(root, "Assets", "3D Models", "CaseFolder", "OpenCaseFolder", "Materials", "Material_0.001.mat"));
string materialMeta13 = File.ReadAllText(Path.Combine(root, "Assets", "3D Models", "CaseFolder", "OpenCaseFolder", "Materials", "Material_0.001.mat.meta"));
string scene13 = File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Office_Level1.unity"));
Check(assets13.Contains("solidMaterial: {fileID: 2100000, guid: 56075b5ff19276a4eb803a05821c08f0, type: 2}") && materialMeta13.Contains("guid: 56075b5ff19276a4eb803a05821c08f0") &&
      material13.Contains("m_Shader: {fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}") &&
      scene13.Contains("m_Name: DeskViewpoint") && scene13.Contains("m_Name: ConsultantOffice") && scene13.Contains("m_Name: MCRoom") &&
      scene13.Contains("m_Name: ShelfProps") && scene13.Contains("value: SM_Prop_Shelf_06"),
    "static check: the decor material is a real URP Lit material, and the desk, its seat and the filing shelf named by the anchors exist in Office_Level1");

// =====================================================================
// T14: review vouchers - buy, store, retrieve and use on a case
// =====================================================================
ShopCatalogue shop14 = ShopCatalogue.BuiltIn();
InventoryCatalogue items14 = InventoryCatalogue.BuiltIn();
ShopProduct voucherProduct14 = shop14.Find(ShopCatalogue.ReviewVoucher);
InventoryItemDefinition voucherItem14 = items14.Find(InventoryCatalogue.ReviewVoucher);
string V14 = InventoryCatalogue.ReviewVoucher;

// ---------- catalogue and printed text ----------
var file14 = JsonSerializer.Deserialize<ShopCatalogue>(File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Campaign", "shop.json")), json);
Check(voucherProduct14 != null && voucherProduct14.consumable && voucherProduct14.IsItem && voucherProduct14.itemId == V14 && voucherProduct14.price == 30 &&
      shop14.InSection(ShopCatalogue.SectionSupplies)[1].id == ShopCatalogue.ReviewVoucher && shop14.Problems(items14).Count == 0 &&
      file14.products.All(f => shop14.Find(f.id) != null && shop14.Find(f.id).consumable == f.consumable) && file14.Find(ShopCatalogue.ReviewVoucher).price == 30 &&
      shop14.FindAnchor("desk_drawer") == null && file14.FindAnchor("desk_drawer") == null && file14.Problems(items14).Count == 0,
    "catalogue: the Review Voucher sells for 30 as a repeatable supply on the SUPPLIES tab; the Desk Drawer unit's anchor is gone; shop.json matches");
var badVoucher14 = ShopCatalogue.BuiltIn();
badVoucher14.products.Add(new ShopProduct { id = "pin", kind = "item", section = "office", itemId = InventoryCatalogue.Postcard, price = 5, title = "Pin", consumable = true });
badVoucher14.anchors.Add(new ShopDecorAnchor { id = "floor2", label = "x", kind = "floor", parent = "a", target = "b", side = "" });
Check(badVoucher14.Problems(items14).Any(p => p.Contains("consumable but does not sell a stackable item")) &&
      badVoucher14.Problems(items14).Any(p => p.Contains("anchor \"floor2\" needs side right or left")),
    "catalogue problems: a consumable must sell a stackable item; a floor anchor needs a side");
PickupDefinition voucherPickup14 = PickupCatalogue.BuiltIn().Find(PickupCatalogue.VoucherPickup);
Check(voucherItem14.stackLimit == 2 && voucherItem14.carryLimit == 2 && voucherItem14.description.Contains("+180 REVIEW SECONDS") &&
      voucherItem14.description.Contains("One per case") && voucherPickup14.title == "REVIEW VOUCHER" && voucherPickup14.lines[0].Contains("+180 active review seconds") &&
      !voucherItem14.description.Contains("+3:00") && !voucherPickup14.lines.Any(l => l.Contains("+3:00")),
    "the voucher reads +180 active review seconds, one per case, carry two (no older +3:00 wording)");

// ---------- buying: carry up to two, then refused (the Desk Drawer left play) ----------
PlayerProgressData buyer14 = Wallet12(200);
PurchaseResult v1 = ShopRules.Purchase(buyer14, shop14, items14, ShopCatalogue.ReviewVoucher, "v1");
PurchaseResult v2 = ShopRules.Purchase(buyer14, shop14, items14, ShopCatalogue.ReviewVoucher, "v2");
string twoSnap14 = Save12(buyer14);
PurchaseResult v3 = ShopRules.Purchase(buyer14, shop14, items14, ShopCatalogue.ReviewVoucher, "v3");
CampaignInventoryRecord buyerInv14 = buyer14.campaign.inventory;
Check(v1.Outcome == InventoryOutcome.Applied && v1.Item.Location.IsSlot && v2.Outcome == InventoryOutcome.Applied && v2.Item.Location.IsSlot &&
      v3.Outcome == InventoryOutcome.Rejected && v3.Message == InventoryRules.CarryFull + "." && Save12(buyer14) == twoSnap14 &&
      InventoryRules.CarriedCount(buyerInv14, V14) == 2 && buyerInv14.drawer.Count == 0 && buyer14.campaign.coins == 140 &&
      !buyer14.campaign.ownedUpgradeIds.Contains(ShopCatalogue.ReviewVoucher) && buyer14.campaign.ledger.Count(e => e.reason == "Bought Review Voucher" && e.amount == -30) == 2 &&
      ShopRules.StatusOf(buyer14, shop14, voucherProduct14, true) == ShopStatus.Available,
    "vouchers: two carried (T10 placement order); a third is refused with no charge (the screen says so before confirming); 30 coins each, never 'owned', still buyable");
string buyerSnap14 = Save12(buyer14);
Check(ShopRules.Purchase(buyer14, shop14, items14, ShopCatalogue.ReviewVoucher, "v2").Outcome == InventoryOutcome.AlreadyApplied && Save12(buyer14) == buyerSnap14 &&
      ShopRules.Purchase(Wallet12(29), shop14, items14, ShopCatalogue.ReviewVoucher, "v4").Message == "You need 1 more coin.",
    "a retried purchase request is already applied (no second charge); too few coins is refused");
PlayerProgressData menu14 = Wallet12(90);
ShopRules.Purchase(menu14, shop14, items14, ShopCatalogue.ReviewVoucher, "m1", noHand: true);
ShopRules.Purchase(menu14, shop14, items14, ShopCatalogue.ReviewVoucher, "m2", noHand: true);
PurchaseResult m3 = ShopRules.Purchase(menu14, shop14, items14, ShopCatalogue.ReviewVoucher, "m3", noHand: true);
Check(m3.Outcome == InventoryOutcome.Rejected && InventoryRules.CarriedCount(menu14.campaign.inventory, V14) == 2 && menu14.campaign.coins == 30,
    "the menu shop follows the same rule: carry two, then refused (no charge)");

// ---------- an older save's drawer vouchers ----------
InventoryResult consumed14 = InventoryRules.ConsumeCarried(buyerInv14, items14, V14, "c1");
Check(consumed14.Outcome == InventoryOutcome.Applied && InventoryRules.CarriedCount(buyerInv14, V14) == 1 &&
      ShopRules.Purchase(buyer14, shop14, items14, ShopCatalogue.ReviewVoucher, "v5").Outcome == InventoryOutcome.Applied && InventoryRules.CarriedCount(buyerInv14, V14) == 2,
    "after using one, a voucher can be bought again (back to two carried)");
var away14 = new CampaignInventoryRecord();
away14.Repair();
away14.AddToDrawer(V14, 2);
InventoryRules.Normalize(away14, items14, new List<string>());
Check(InventoryRules.CarriedCount(away14, V14) == 2 && away14.drawer.Count == 0,
    "vouchers an older save kept in the Desk Drawer are carried again after loading (up to the carry limit)");

// ---------- using up one carried voucher ----------
var use14 = new CampaignInventoryRecord();
use14.Repair();
use14.hotbar[0] = new InventorySlot { itemId = V14, quantity = 1 };
use14.bag[2] = new InventorySlot { itemId = V14, quantity = 1 };
InventoryResult fromBag14 = InventoryRules.ConsumeCarried(use14, items14, V14, "u1");
InventoryResult fromBar14 = InventoryRules.ConsumeCarried(use14, items14, V14, "u2");
Check(fromBag14.Location == SlotRef.Bag(2) && use14.bag[2].IsEmpty && fromBar14.Location == SlotRef.Hotbar(0) && use14.hotbar[0].IsEmpty &&
      InventoryRules.ConsumeCarried(use14, items14, V14, "u3").Outcome == InventoryOutcome.Rejected &&
      InventoryRules.ConsumeCarried(use14, items14, V14, "u2").Outcome == InventoryOutcome.AlreadyApplied,
    "using one takes it from the bag first, then the bar; with none carried nothing changes; a repeated action is already applied");

// ---------- when a voucher can be used ----------
var clock14 = new VoucherClockView { HasDeadline = true, Announced = true, GameSeconds = 9 * 3600, DeadlineSeconds = 9 * 3600 + 600 };
VoucherClockView Clock14(Func<VoucherClockView, VoucherClockView> change) => change(clock14);
Check(VoucherRules.Check(1, "marie", clock14, false) == VoucherUseStatus.Ready &&
      VoucherRules.Check(1, " ", clock14, false) == VoucherUseStatus.NoCase &&
      VoucherRules.Check(1, "marie", clock14, true) == VoucherUseStatus.AlreadyUsed &&
      VoucherRules.Check(1, "marie", Clock14(c => { c.ExtensionSeconds = 180; return c; }), false) == VoucherUseStatus.AlreadyUsed &&
      VoucherRules.Check(1, "marie", Clock14(c => { c.CaseDone = true; return c; }), false) == VoucherUseStatus.CaseDone &&
      VoucherRules.Check(1, "marie", Clock14(c => { c.Announced = false; return c; }), false) == VoucherUseStatus.NoDeadline &&
      VoucherRules.Check(1, "marie", Clock14(c => { c.HasDeadline = false; return c; }), false) == VoucherUseStatus.NoDeadline &&
      VoucherRules.Check(1, "marie", Clock14(c => { c.DayClosing = true; return c; }), false) == VoucherUseStatus.DayClosing &&
      VoucherRules.Check(1, "marie", Clock14(c => { c.GameSeconds = c.DeadlineSeconds; return c; }), false) == VoucherUseStatus.CutoffPassed &&
      VoucherRules.Check(1, "marie", Clock14(c => { c.GameSeconds = c.DeadlineSeconds - 1; return c; }), false) == VoucherUseStatus.Ready &&
      VoucherRules.Check(0, "marie", clock14, false) == VoucherUseStatus.NoVoucher &&
      Enum.GetValues(typeof(VoucherUseStatus)).Cast<VoucherUseStatus>().All(s => s == VoucherUseStatus.Ready || VoucherRules.ReasonFor(s).Length > 0) &&
      VoucherRules.ReasonFor(VoucherUseStatus.CutoffPassed).Contains("can't bring back a missed cutoff"),
    "a voucher is usable only on an active case with an announced cutoff not yet reached (one second before: yes; at it: no), not on an approved case, once per case, with one carried");

// ---------- the extended clock ----------
var before14 = new Level1ClockRecord { isSet = true, day = 1, gameSeconds = 32400, activeSeconds = 400f, allowance = 300f, hasDeadline = true, announced = true,
    deadlineSeconds = 33000, deadlineFirstDay = 33000, submittedAt = -1, onTimeSaid = false };
string beforeJson14 = JsonSerializer.Serialize(before14, json);
Level1ClockRecord after14 = VoucherRules.Extend(before14, 10f);
var day2_14 = new Level1ClockRecord { isSet = true, day = 2, gameSeconds = 30000, hasDeadline = true, announced = true, deadlineSeconds = 31000, deadlineFirstDay = 33000 };
Level1ClockRecord day2After14 = VoucherRules.Extend(day2_14, 10f);
Check(VoucherRules.CutoffShift(10f) == 1800 && after14.deadlineSeconds == 34800 && after14.deadlineFirstDay == 34800 && after14.extensionSeconds == 180 &&
      after14.gameSeconds == 32400 && after14.day == 1 && after14.allowance == 300f && after14.activeSeconds == 400f &&
      JsonSerializer.Serialize(before14, json) == beforeJson14 && day2After14.deadlineSeconds == 32800 && day2After14.deadlineFirstDay == 33000,
    "one voucher moves today's cutoff by 180 active seconds at the clock's rate (30 game minutes) and only that; day 1's on-time cutoff moves with it");

// ---------- one transaction, then resume ----------
PlayerProgressData case14 = Wallet12(0);
case14.campaign.inventory.hotbar[1] = new InventorySlot { itemId = V14, quantity = 2 };
case14.campaign.levelOne = new Level1FieldCheckpoint { version = 1, writes = 3 };
case14.campaign.levelOne.clock = before14;
PlayerProgressData candidate14 = JsonSerializer.Deserialize<PlayerProgressData>(Save12(case14), json);
VoucherUseResult used14 = VoucherRules.Use(candidate14.campaign, items14, "Level_01", "marie_case", "act1");
candidate14.campaign.levelOne.clock = VoucherRules.Extend(candidate14.campaign.levelOne.clock, 10f);
PlayerProgressData resumed14 = JsonSerializer.Deserialize<PlayerProgressData>(Save12(candidate14), json);
resumed14.Normalize();
Check(used14.Outcome == InventoryOutcome.Applied && used14.ClaimId == VoucherRules.UseClaimId("t12", "Level_01", "marie_case") && used14.From == SlotRef.Hotbar(1) &&
      resumed14.campaign.rewardClaims.Contains(used14.ClaimId) && InventoryRules.CarriedCount(resumed14.campaign.inventory, V14) == 1 &&
      resumed14.campaign.levelOne.clock.deadlineSeconds == 34800 && resumed14.campaign.levelOne.clock.extensionSeconds == 180 &&
      InventoryRules.CarriedCount(case14.campaign.inventory, V14) == 2 && !case14.campaign.rewardClaims.Contains(used14.ClaimId) &&
      case14.campaign.levelOne.clock.deadlineSeconds == 33000,
    "one candidate holds the used voucher, the per-case claim and the extended clock; it resumes at the changed cutoff, and the live save is untouched until it is written");
string usedSnap14 = Save12(resumed14);
VoucherUseResult again14 = VoucherRules.Use(resumed14.campaign, items14, "Level_01", "marie_case", "act2");
Check(again14.Outcome == InventoryOutcome.AlreadyApplied && Save12(resumed14) == usedSnap14 &&
      VoucherRules.Check(1, "marie_case", new VoucherClockView { HasDeadline = true, Announced = true, GameSeconds = 1, DeadlineSeconds = 99999 },
          resumed14.campaign.rewardClaims.Contains(VoucherRules.UseClaimId("t12", "Level_01", "marie_case"))) == VoucherUseStatus.AlreadyUsed &&
      VoucherRules.Use(resumed14.campaign, items14, "Level_01", "", "act3").Outcome == InventoryOutcome.Rejected &&
      VoucherRules.Use(Wallet12(0).campaign, items14, "Level_01", "marie_case", "act4").Outcome == InventoryOutcome.Rejected,
    "after a resume, a new day or a repeated tap the claim holds: never a second voucher on the same case; no voucher or no case changes nothing");
var tooMuch14 = new Level1ClockRecord { isSet = true, extensionSeconds = 900 };
var repairClock14 = new PlayerProgressData { campaign = CampaignRecord.CreateFresh("rc", "") };
repairClock14.campaign.levelOne = new Level1FieldCheckpoint { version = 1, clock = tooMuch14, caseRecord = new Level1CaseRecord { caseId = "x" } };
repairClock14.Normalize();
Check(repairClock14.campaign.levelOne.clock.extensionSeconds <= 180, "a saved extension above one voucher is repaired to 180");

// ---------- save owner, command and presentation contracts ----------
string manager14 = Src("RewardsSystem", "PlayerProgressManager.cs");
string commit14 = Between(manager14, "public VoucherUseResult CommitVoucherUse(", "/// <summary>\r\n    /// T13: the office has shown");
if (commit14.Length == 0) commit14 = Between(manager14, "public VoucherUseResult CommitVoucherUse(", "public bool MarkDecorArrived(");
Check(Index(commit14, "VoucherRules.Use(candidate.campaign") < Index(commit14, "candidate.campaign.levelOne = checkpoint;") &&
      Index(commit14, "candidate.campaign.levelOne = checkpoint;") < Index(commit14, "TryWriteCandidate(candidate)") &&
      Index(commit14, "TryWriteCandidate(candidate)") < Index(commit14, "InventoryChanged?.Invoke()") && commit14.Contains("Outcome = InventoryOutcome.SaveFailed") &&
      commit14.Contains("SessionClaims.Add(claim);") && commit14.Contains("sessionInventory = inventory;"),
    "story: voucher, claim and checkpoint clock are written in one candidate before anything is shown (a failed write changes nothing); other runs use temporary supplies");
string card14 = Src("UI", "HUD", "VoucherUseCard.cs");
string useCmd14 = Between(card14, "public static VoucherUseResult Use(HudHotbar hotbar)", "/// <summary>\r\n/// PURPOSE:");
if (useCmd14.Length == 0) useCmd14 = Between(card14, "public static VoucherUseResult Use(HudHotbar hotbar)", "public sealed class VoucherUseCard");
string onPrimary14 = Between(card14, "private void OnPrimary()", "private void OnSecondary()");
string animate14 = Between(card14, "private void Animate(float t)", "// =========================================================");
Check(Index(useCmd14, "checkpoint.clock = extended;") < Index(useCmd14, "CommitVoucherUse(") && Index(useCmd14, "CommitVoucherUse(") < Index(useCmd14, "hours.ApplyExtension(extended)") &&
      useCmd14.Contains("if (result.Outcome == InventoryOutcome.Applied) hours.ApplyExtension(extended);") &&
      Index(onPrimary14, "ReviewVoucherCommand.Use(hotbar)") < Index(onPrimary14, "stage = Stage.Applying;") &&
      Between(onPrimary14, "if (result.Outcome != InventoryOutcome.Applied)", "stage = Stage.Applying;").IndexOf("PlayEffect", StringComparison.Ordinal) < 0 &&
      animate14.Contains("-180f * eased") && animate14.Contains("PlayEffect(\"sfx_ui_click_02\"") && animate14.Contains("if (t >= 0.72f) FinishApply();") &&
      card14.Contains("+180 active review seconds") && !Src("UI", "HUD", "HudHotbar.cs").Contains("OfficeHours."),
    "the card commits first, then turns the copper hand half a turn with one tick in about 0.7 s; a refusal is silent; the Bag itself never touches the clock");
string hours14 = Src("UI", "HUD", "OfficeHours.cs");
string apply14 = Between(hours14, "public void ApplyExtension(Level1ClockRecord extended)", "/// <summary>\r\n    /// Quietly restores");
if (apply14.Length == 0) apply14 = Between(hours14, "public void ApplyExtension(Level1ClockRecord extended)", "public void RestoreApprovedResult()");
string nextMorning14 = Between(hours14, "private IEnumerator NextMorning()", "private IEnumerator ReturnToFrontDoor()");
Check(apply14.Contains("if (extended == null || CaseDone || nextMorning != null) return;") && apply14.Contains("DeadlineSeconds = extended.deadlineSeconds;") &&
      !apply14.Contains("GameSeconds =") && !apply14.Contains(" Day =") && !nextMorning14.Contains("ExtensionSeconds") &&
      hours14.Contains("public VoucherClockView VoucherView") && Src("SideQuest1", "Level1FieldCheckpointer.cs").Contains("public Level1FieldCheckpoint CaptureForCommit()"),
    "the existing clock only takes a later cutoff (never the time, the day, a missed cutoff or the next morning's recovery); no second clock");
Check(!File.Exists(Path.Combine(root, "Assets", "Scripts", "Interactables", "DeskSupplyDrawer.cs")) &&
      !Src("UI", "HUD", "HudHotbar.cs").Contains("DeskSupplyDrawer") && !Src("UI", "HUD", "InventoryScreen.cs").Contains("DrawerDropZone"),
    "the Desk Drawer left play: no drawer unit at the desk and no drawer panel in the Bag");
string face14 = Src("SideQuest1", "WorldCollectibles.cs");
Check(face14.Contains("public static void BuildVoucherFace(RectTransform root, string title, int carried, int limit)") && face14.Contains("\"ONE PER CASE\"") &&
      face14.Contains("\"REVIEW SECONDS\"") && face14.Contains("\"Clock Seal\"") && face14.Contains("\"CARRY\"") && !face14.Contains("\"+3:00\"") &&
      Src("UI", "HUD", "HudHotbar.cs").Contains("voucherCard.Open();"),
    "the printed ticket: copper clock seal, +180 REVIEW SECONDS, ONE PER CASE and a carry tab; tapping a carried voucher opens its card");
string[] forbidden14 = { "publicTrust", "AddExp", "campaign.coins", "LevelScoreLedger", "AddBonus", "CampaignRewards.Grant" };
Check(forbidden14.All(w => card14.IndexOf(w, StringComparison.Ordinal) < 0 &&
      Src("RewardsSystem", "CampaignVouchers.cs").IndexOf(w, StringComparison.Ordinal) < 0),
    "a voucher brings no trust, coins, XP or score: only review time");

// =====================================================================
// T15 reverted (user decision 2026-10-25): the guide is Arthur again and
// there is no practice board or demonstration seam.
// =====================================================================
string guide15 = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Tutorials", "Level_01", "guide.json"));
string[] arthurText15 =
{
    guide15,
    File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Levels", "Level_01", "town.json")),
    File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Levels", "Level_01", "objectives.json")),
    File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Levels", "Level_01", "office_hours.json")),
    File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Tutorials", "Training", "training.json")),
    Src("SideQuest1", "CorkboardTeaching.cs"), Src("SideQuest1", "CorkboardSideQuestManager.cs"), Src("SideQuest1", "SideQuestSplash.cs"),
    Src("SideQuest1", "TutorialNpcController.cs"), Src("Data", "TaskListProvider.cs"), Src("Training", "TrainingData.cs"),
    Src("UI", "Guide", "GuideDirector.cs"), Src("UI", "Guide", "GuideCoachView.cs"), Src("UI", "HUD", "HudHotbar.cs"),
};
Check(arthurText15.All(t => t.IndexOf("Quax", StringComparison.OrdinalIgnoreCase) < 0) &&
      guide15.Contains("\"name\": \"Arthur\"") && guide15.Contains("\"role\": \"Senior Tax Consultant\"") &&
      guide15.Contains("\"label\": \"Arthur\"") && Src("Data", "TaskListProvider.cs").Contains("\"Talk to Arthur in the plaza.\", false, null)"),
    "Level 1's guide is Arthur again (name, role, waypoint, objective, town text, binder, corkboard lines) and nothing names Mayor Quax");
string[] t15Files = { "SideQuest1/QuaxContent.cs", "SideQuest1/QuaxPresence.cs", "SideQuest1/QuaxPracticeBoard.cs", "UI/Guide/GuideDemo.cs" };
Check(t15Files.All(f => !File.Exists(Path.Combine(root, "Assets", "Scripts", f))) &&
      !File.Exists(Path.Combine(root, "Assets", "Resources", "Campaign", "quax.json")) &&
      !guide15.Contains("\"demo\"") && !guide15.Contains("skippable") && !guide15.Contains("quax-") &&
      !Src("UI", "Guide", "GuideScript.cs").Contains("public string demo;") && !Src("UI", "Guide", "GuideCoachView.cs").Contains("SKIP DEMO") &&
      !Src("FSM", "NPC", "NpcAnimationDriver.cs").Contains("SetAttention"),
    "the practice board, the guide's demonstrations, SKIP DEMO and the actor's point are gone");

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
