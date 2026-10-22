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
string cont = Body(menuEntry, "public static bool Continue(");
cont = cont.Substring(0, Index(cont, "public static bool OpenLevelSelect"));
Check(cont.Contains("cameraManager.SwitchToLevelSelector()") && !cont.Contains("FadeOutThenLoad") && !cont.Contains("CampaignRun.Begin"),
    "Continue opens the saved-progress module and never launches a level itself");
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
    Between(npcSrc, "public void RestoreHandover", "public void OnFocus"),
    Between(hudSrc, "public void RestoreCollected", "public bool TryGetCollectionScreenPoint"),
    Between(guideSrc, "public void RestoreProgress", "private void Awake"),
    Between(cardSrc, "public void RestorePinned", "private void UpdateSway"),
    Between(boardSrc, "private void RestorePins", "private bool TryGetTrayView")
};
Check(quietBodies.All(b => b.Length > 0 && !b.Contains("Raise") && !b.Contains("PlayEffect") && !b.Contains("AwardConversation") &&
                         !b.Contains("AddSideQuest") && !b.Contains("TryStartDialogue") && !b.Contains("HandleCardDropped") && !b.Contains("CardPlaced")),
    "owner restore methods play no sound, dialogue, award, drop or event");
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
Check(mgr4.Contains("levelTotal.LevelId == CampaignRecord.FirstChapterId)") && mgr4.Contains("progress.campaign.levelOne = new Level1FieldCheckpoint();"),
    "finishing Level 1 in the story drops its field checkpoint in the same write");
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
