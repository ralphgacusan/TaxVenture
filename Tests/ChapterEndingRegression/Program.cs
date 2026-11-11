using System;
using System.IO;
using System.Linq;
using System.Text.Json;

int checks = 0, failures = 0;
void Check(bool ok, string message)
{
    checks++;
    if (!ok) { failures++; Console.WriteLine("FAIL: " + message); }
}
var json = new JsonSerializerOptions { IncludeFields = true, IgnoreReadOnlyProperties = true };
PlayerProgressData Fresh() => new PlayerProgressData { campaign = CampaignRecord.CreateFresh("ending-test", "2026-11-06") };
PlayerProgressData Clone(PlayerProgressData data) => JsonSerializer.Deserialize<PlayerProgressData>(JsonSerializer.Serialize(data, json), json);
string Snapshot(PlayerProgressData data) => JsonSerializer.Serialize(data, json);

// The menu route must play new openings without replaying over saved work or changing the save.
foreach (string office in new[] { "Office_Level2", "Office_Level3" })
{
    string chapter = office == "Office_Level2" ? "Level_02" : "Level_03";
    var entry = Fresh();
    string before = Snapshot(entry);
    Check(ChapterOpeningRules.ShouldPlay(entry, CampaignRunMode.Story, office, false), office + " fresh Story plays opening");
    foreach (CampaignRunMode mode in new[] { CampaignRunMode.None, CampaignRunMode.DirectEditor, CampaignRunMode.Practice })
        Check(!ChapterOpeningRules.ShouldPlay(entry, mode, office, false), office + " " + mode + " bypasses opening");
    Check(!ChapterOpeningRules.ShouldPlay(entry, CampaignRunMode.Story, office, true), office + " active drill bypasses opening");
    Check(Snapshot(entry) == before, office + " selection cannot write story/rewards/inventory");
    entry.activeCheckpoint = new LevelCheckpoint { levelId = chapter, reviewJson = "{}" };
    Check(!ChapterOpeningRules.ShouldPlay(entry, CampaignRunMode.Story, office, false), office + " saved review continues directly");
    entry.activeCheckpoint = new LevelCheckpoint();
    if (chapter == "Level_03")
    {
        entry.campaign.levelThree.version = 2;
        Check(!ChapterOpeningRules.ShouldPlay(entry, CampaignRunMode.Story, office, false), "L3 chapter checkpoint continues directly before review exists");
    }
    entry.campaign.MarkChapterCompleted(chapter);
    Check(!ChapterOpeningRules.ShouldPlay(entry, CampaignRunMode.Story, office, false), office + " old completed Story does not fabricate new entry");
    before = Snapshot(entry);
    Check(ChapterOpeningRules.ShouldPlay(entry, CampaignRunMode.MasteryReplay, office, false), office + " explicit Replay plays opening despite old saved work");
    Check(Snapshot(entry) == before, office + " Replay selection leaves live campaign unchanged");
    entry.campaign.ending = new ChapterEndingRecord { chapterId = "Level_01" };
    Check(!ChapterOpeningRules.ShouldPlay(entry, CampaignRunMode.Story, office, false) &&
          !ChapterOpeningRules.ShouldPlay(entry, CampaignRunMode.MasteryReplay, office, false), office + " pending ending takes priority");
    string id = ChapterOpeningRules.IdForScene(ChapterOpeningRules.SceneForOffice(office));
    string authoredPath = "Assets/Cutscenes/Chapter" + (chapter == "Level_02" ? "02/chapter02" : "03/chapter03") + "_start_script.json";
    using var authored = JsonDocument.Parse(File.ReadAllText(authoredPath));
    Check(authored.RootElement.GetProperty("id").GetString() == id, office + " continuation listens to actual authored opening ID");
}
Check(!ChapterOpeningRules.ShouldPlay(Fresh(), CampaignRunMode.Story, "Cutscene2.0", false) &&
      !ChapterOpeningRules.ShouldPlay(Fresh(), CampaignRunMode.MasteryReplay, "Office_Level1", false), "L1 intro route remains unchanged");
Check(!ChapterOpeningRules.ShouldPlay(Fresh(), CampaignRunMode.Story, "Office_Level4", false), "future chapters have no fabricated opening");

foreach (string chapter in new[] { "Level_01", "Level_02", "Level_03" })
{
    var data = Fresh();
    Check(!ChapterEndingRules.Begin(data, chapter), chapter + " cannot start before service prerequisites");
    Check(!ChapterEndingRules.Pending(data), chapter + " no pending movie fabricated");
    Check(!ChapterEndingRules.Consume(data, chapter, ChapterCinematicOutcome.Completed), chapter + " cannot consume missing movie");
}
Check(ChapterEndingRules.SceneFor("Level_04") == null && ChapterEndingRules.SceneFor(null) == null, "only authored ending routes exist");

var one = Fresh();
one.campaign.levelOneEndFlowPending = true;
Check(!ChapterEndingRules.Begin(one, "Level_01"), "L1 pending flag alone is not delivered service");
CampaignRewards.CompleteActivity(one, "Level_01", CampaignRewards.FollowThrough);
Check(ChapterEndingRules.Begin(one, "Level_01"), "L1 actual delivered/pending result creates ending");
Check(ChapterEndingRules.Pending(one, "Level_01"), "L1 pending chapter identified");
Check(!ChapterEndingRules.Begin(one, "Level_01"), "duplicate begin keeps original handoff");
Check(!ChapterEndingRules.Consume(one, "Level_02", ChapterCinematicOutcome.Completed), "other movie cannot consume pending chapter");
Check(!ChapterEndingRules.Consume(one, "Level_01", ChapterCinematicOutcome.Aborted) && !one.campaign.ending.consumed, "abort remains pending");

foreach (ChapterCinematicOutcome outcome in new[] { ChapterCinematicOutcome.Completed, ChapterCinematicOutcome.Skipped })
{
    var live = Clone(one);
    string before = Snapshot(live);
    var candidate = Clone(live);
    Check(ChapterEndingRules.Consume(candidate, "Level_01", outcome), outcome + " can consume candidate");
    Check(Snapshot(live) == before, outcome + " unpublished/failed candidate leaves live state unchanged");
    var restored = Clone(candidate);
    Check(restored.campaign.ending.consumed && ChapterEndingRules.Pending(restored, "Level_01"), outcome + " roundtrip retains pending result after movie");
    Check(restored.campaign.publicTrust == live.campaign.publicTrust && restored.campaign.coins == live.campaign.coins && restored.totalExp == live.totalExp,
        outcome + " does not reward or alter trust/mastery");
    Check(ChapterEndingRules.Consume(restored, "Level_01", outcome), outcome + " repeat is idempotent");
    ChapterEndingRules.Acknowledge(restored, "Level_01");
    Check(!ChapterEndingRules.Pending(restored), outcome + " acknowledgment ends handoff");
}

var two = Fresh();
CampaignRewards.CompleteActivity(two, "Level_02", CampaignRewards.IncomeReview);
CampaignRewards.CompleteActivity(two, "Level_02", CampaignRewards.CaseApproved);
Check(!ChapterEndingRules.Begin(two, "Level_02"), "L2 audit approval alone cannot start ending");
CampaignRewards.CompleteActivity(two, "Level_02", CampaignRewards.RosaFollowThrough);
two.campaign.rosaEthics = new RosaEthicsRecord { committed = true, choiceId = "case/responsible", followThroughRequired = true };
Check(!ChapterEndingRules.Begin(two, "Level_02"), "L2 unresolved ethical follow-through blocks ending");
two.campaign.rosaEthics.followThroughDone = true;
two.campaign.MarkChapterCompleted("Level_02");
Check(ChapterEndingRules.Begin(two, "Level_02", "{\"LevelId\":\"Level_02\",\"TotalExp\":123}"), "L2 handoff coexists with already committed completion");
var backTwo = Clone(two);
Check(backTwo.campaign.ending.resultJson == two.campaign.ending.resultJson && backTwo.campaign.ending.variant == "rosa.responsible_completed", "L2 result snapshot and actual context survive roundtrip");
Check(!ChapterEndingRules.Begin(backTwo, "Level_03"), "pending handoff cannot be overwritten by another chapter");

Check(ChapterEndingRules.RosaVariant(null) == "rosa.neutral", "missing Rosa context is neutral");
Check(ChapterEndingRules.RosaVariant(new RosaEthicsRecord { choiceId = "case/responsible", followThroughDone = true }) == "rosa.neutral", "uncommitted words are not history");
Check(ChapterEndingRules.RosaVariant(new RosaEthicsRecord { committed = true, choiceId = "case/unsupported-promise" }) == "rosa.neutral", "unresolved promise not described as resolved");
Check(ChapterEndingRules.RosaVariant(new RosaEthicsRecord { committed = true, choiceId = "case/unsupported-promise", followThroughDone = true }) == "rosa.promise_resolved", "resolved promise selects matching authored variant");
Check(ChapterEndingRules.RosaVariant(new RosaEthicsRecord { committed = true, choiceId = "case/dismissive-hold" }) == "rosa.dismissive_completed", "actual dismissive ID matches authored variant");

var three = Fresh();
var state = three.campaign.levelThree;
state.phase = Level3Phase.WrapUpPending.ToString();
state.activeCaseId = Level3ChapterRules.BeaCaseId;
state.cases.Add(new Level3CaseProgress { caseId = Level3ChapterRules.BeaCaseId, admitted = true, copyPhase = Level3DeliveryRules.CopyDone,
    review = new Level3ReviewRecord { approved = true } });
state.actionIds.Add(Level3DeliveryRules.DeliverAction);
Check(!ChapterEndingRules.Begin(three, "Level_03"), "L3 delivery alone cannot bypass finding/message");
state.actionIds.Add(Level3ClosingRules.FindingAction);
Check(!ChapterEndingRules.Begin(three, "Level_03"), "L3 finding alone cannot bypass mayor message");
state.actionIds.Add(Level3ClosingRules.MessageAction);
Check(ChapterEndingRules.Begin(three, "Level_03"), "L3 full real closing prerequisites create movie");
Check(Clone(three).campaign.ending.chapterId == "Level_03", "L3 interrupted movie roundtrips pending");
var older = Fresh();
older.campaign.MarkChapterCompleted("Level_01"); older.campaign.MarkChapterCompleted("Level_02"); older.campaign.MarkChapterCompleted("Level_03");
Check(!ChapterEndingRules.Pending(older), "old completed campaign with no markers has no automatic replay");

// The guard behind "the ending always reaches the result": exceptions end the body, never the caller.
System.Collections.IEnumerator Inner(System.Collections.Generic.List<string> log)
{
    log.Add("inner");
    yield return 2;
    throw new InvalidOperationException("nested failure");
}
System.Collections.IEnumerator Outer(System.Collections.Generic.List<string> log)
{
    log.Add("outer");
    yield return 1;
    yield return Inner(log);
    log.Add("unreachable");
}
var guardLog = new System.Collections.Generic.List<string>();
var guardFailures = new System.Collections.Generic.List<Exception>();
var guardSteps = new System.Collections.Generic.List<object>();
System.Collections.IEnumerator guarded = ChapterEndingRules.Guarded(Outer(guardLog), guardFailures.Add);
while (guarded.MoveNext()) guardSteps.Add(guarded.Current);
Check(guardLog.SequenceEqual(new[] { "outer", "inner" }) && guardFailures.Count == 1 && guardFailures[0].Message == "nested failure",
    "guard stops at a nested exception, reports it once and does not rethrow");
Check(guardSteps.SequenceEqual(new object[] { 1, 2 }), "guard forwards the body's yields, including nested ones");
System.Collections.IEnumerator Clean() { yield return "a"; yield return "b"; }
System.Collections.IEnumerator cleanGuard = ChapterEndingRules.Guarded(Clean(), null);
var cleanSteps = new System.Collections.Generic.List<object>();
while (cleanGuard.MoveNext()) cleanSteps.Add(cleanGuard.Current);
Check(cleanSteps.SequenceEqual(new object[] { "a", "b" }), "guard is transparent for a body that does not fail");
System.Collections.IEnumerator Throws() { throw new InvalidOperationException("first step"); yield break; }
var firstStep = new System.Collections.Generic.List<Exception>();
Check(!ChapterEndingRules.Guarded(Throws(), firstStep.Add).MoveNext() && firstStep.Count == 1, "guard survives a throw on the very first step (the freeze's shape)");

string Root()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
    return dir.FullName;
}
string Read(string path) => File.ReadAllText(Path.Combine(Root(), path));
string builds = Read("ProjectSettings/EditorBuildSettings.asset");
foreach (string chapter in new[] { "01", "02", "03" })
{
    string scene = "Assets/Scenes/Cutscenes/CS_Chapter" + chapter + "_End.unity";
    Check(builds.Split(new[] { "path: " + scene }, StringSplitOptions.None).Length == 2, scene + " registered exactly once");
}
Check(builds.Contains("CS_Chapter02_Start") && builds.Contains("CS_Chapter03_Start"), "requested L2/L3 openings are registered for level entry");
string careerLaunch = Read("Assets/Scripts/UI/Menu/CareerBoardPresenter.cs");
Check(careerLaunch.Contains("ChapterOpeningSequence.LoadChapter(chapter.Def.scene)"), "career launch routes through the existing opening scenes");
Check(Read("Assets/Scripts/UI/ProgressUI.cs").Contains("ChapterOpeningSequence.LoadChapter(levelTwoCutscene)"), "legacy Level 2 entry uses the same opening handoff");
string openingPath = Path.Combine(Root(), "Assets/Scripts/Cutscenes/Chapters/ChapterOpeningSequence.cs");
Check(File.Exists(openingPath), "opening completion/Skip has an owned gameplay continuation");
string opening = Read("Assets/Scripts/Cutscenes/Chapters/ChapterOpeningSequence.cs");
Check(opening.Contains("result.Id == expectedId && result.Watched") && opening.Contains("if (returning) return;") &&
      opening.Contains("returning = true;"), "opening handoff filters abort/unrelated movies and latches completion/Skip once");
Check(opening.Contains("if (destination == null || scene.name != opening) return;") &&
      !opening.Contains("RuntimeInitializeLoadType.AfterSceneLoad"), "direct scene preview cannot acquire a menu continuation");
Check(opening.Contains("Application.CanStreamedLevelBeLoaded(destination)") && opening.Contains("returning to Career.") &&
      opening.Contains("MenuEntry.OpenLevelSelect = true;"), "missing office recovers to existing Career UI with a named error");
string[] expectedScenes = { "3D Menu", "Office_Level1", "Office_Level2", "Cutscene2.0", "Office_Level3",
    "Cutscenes/CS_Chapter01_End", "Cutscenes/CS_Chapter02_End", "Cutscenes/CS_Chapter03_End",
    "Cutscenes/CS_Chapter02_Start", "Cutscenes/CS_Chapter03_Start" };
var registrations = System.Text.RegularExpressions.Regex.Matches(builds, @"- enabled: 1\s+path: (\S[^\r\n]+)\s+guid: (\w+)");
Check(registrations.Select(m => m.Groups[1].Value).SequenceEqual(expectedScenes.Select(n => "Assets/Scenes/" + n + ".unity")),
    "two enabled openings append after all eight existing scene indices");
Check(registrations.All(m => Read(m.Groups[1].Value + ".meta").Contains("guid: " + m.Groups[2].Value)),
    "every build entry uses its preserved scene GUID");
string menu = Read("Assets/Scripts/UI/MenuEntry.cs");
Check(menu.Contains("if (TryResumeEnding()) return;") && menu.Contains("if (TryResumeEnding()) return true;") &&
      menu.Contains("ChapterEndingRules.Pending(manager.CurrentProgress)") && menu.Contains("CampaignRun.Begin(CampaignRunMode.Story)"),
      "Play/Continue route durable pending endings back to their Story scene");
Check(Read("Assets/Scripts/UI/Menu/CareerBoardPresenter.cs").Contains("MenuEntry.Continue(null); // Finish the saved ending/result"),
      "Career cannot launch another run over a pending ending/result");
Check(Read("Assets/Scripts/Level2/Level2Checkpointer.cs").Contains("progress.UsesLevelTwoStory && Level2CampaignRules.FollowThroughDone(progress.CurrentProgress)) return true;"),
      "L2 result-only restore cannot autosave a new unfinished review");
string player = Read("Assets/Scripts/Cutscenes/Chapters/ChapterCinematicPlayer.cs");
Check(!player.Contains("Save(") && !player.Contains("LoadScene") && !player.Contains("CommitEnding"), "authored player still has no domain or routing calls");
string manager = Read("Assets/Scripts/RewardsSystem/PlayerProgressManager.cs");
Check(manager.IndexOf("ChapterEndingRules.Begin(candidate, \"Level_02\"") < manager.IndexOf("Rosa's follow-through done"), "L2 presentation is prepared before service write reports success");
Check(manager.Contains("if (copyAlreadyDelivered) return true;"), "restored Rosa result does not create another mastery attempt");
Check(Read("Assets/Scripts/UI/LevelCompleteUI.cs").Contains("if (ChapterEndingSequence.BlocksGameplay) return;"), "Skip cannot activate result Continue in same frame");
string sequence = Read("Assets/Scripts/Cutscenes/Chapters/ChapterEndingSequence.cs");
Check(sequence.Contains("behaviour != null && behaviour.enabled"), "hiding gameplay tolerates missing-script components");
Check(sequence.Contains("StartCoroutine(active.Run())") && sequence.Contains("Guarded(Present()") && sequence.Contains("Guarded(RecordHandled()"),
    "every ending runs through the guard, so a failed presentation still reaches the result");
Check(sequence.Contains("player.Abort()") && !sequence.Contains("The ending presentation is unavailable") && !sequence.Contains("Couldn't load the ending"),
    "a stalled or unloadable movie continues to the result instead of waiting on Retry");
// The chapter card keeps its conference-room staging (client on the left) after the cutscene.
string resultCard = Read("Assets/Scripts/UI/LevelCompleteUI.cs");
string finale = Read("Assets/Scripts/UI/HUD/CaseFinale.cs");
Check(!resultCard.Contains("HasHandled(") && resultCard.Split(new[] { "CaseFinale.Instance?.PlayAfterHandover();" }, StringSplitOptions.None).Length == 3,
    "L1/L2 and L3 result cards always stage the client, cutscene or not");
Check(!finale.Substring(finale.IndexOf("private bool WouldStage()"), 200).Contains("HasHandled"), "a handled cutscene does not cancel the finale's card hold");
Check(finale.Contains("while (ChapterEndingSequence.CinematicLoaded) yield return null;"), "the finale stages only after the cutscene's duplicate set unloads");
Check(resultCard.IndexOf("CaseFinale.Instance?.PlayAfterHandover();") < resultCard.IndexOf("HudRewardScreen.Present(panelRoot, BuildChapterThreeScreen"), "L3 staging starts before its card is built");
Check(finale.Contains("private bool OutOfShot(") && finale.Contains("realClient = original;") && !finale.Contains("Physics.CheckSphere"),
    "the real client and the player cannot push the story shot off the replay framing");
Check(finale.Contains("GuideTargets.Player.GetComponentsInChildren<Renderer>()"), "the player's body is hidden from the finale shot");
foreach (string level in new[] { "Level_01/cases/case_001.json", "Level_02/cases/case_rosa_001.json", "Level_03/cases/case_bea_001.json" })
{
    using JsonDocument caseFile = JsonDocument.Parse(Read("Assets/Resources/Levels/" + level));
    JsonElement staging = caseFile.RootElement.GetProperty("clientFinale");
    Check(staging.GetProperty("enabled").GetBoolean() && staging.GetProperty("spotPath").GetString().EndsWith("ConferenceRoom/Client"), level + " stages its client in the conference room");
}
// Exercise the production atomic store with a disposable signed envelope, including an actual I/O failure.
string temp = Path.Combine(Root(), "Temp", "ChapterEndingRegression", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
try
{
    string Envelope(PlayerProgressData data)
    {
        string payload = Snapshot(data);
        return JsonSerializer.Serialize(new { format = 1, payload, signature = SaveFileStore.Sign(payload) });
    }
    bool Valid(string text)
    {
        using var doc = JsonDocument.Parse(text);
        return SaveFileStore.SignatureMatches(doc.RootElement.GetProperty("payload").GetString(), doc.RootElement.GetProperty("signature").GetString());
    }
    var store = new SaveFileStore(Path.Combine(temp, "progress.json"));
    var live = Clone(two);
    store.Write(Envelope(live));
    byte[] previous = File.ReadAllBytes(store.FilePath);
    var candidate = Clone(live);
    ChapterEndingRules.Consume(candidate, "Level_02", ChapterCinematicOutcome.Skipped);
    Directory.CreateDirectory(store.TempPath);
    bool failed = false;
    try { store.Write(Envelope(candidate)); }
    catch (IOException) { failed = true; }
    catch (UnauthorizedAccessException) { failed = true; }
    Check(failed && File.ReadAllBytes(store.FilePath).SequenceEqual(previous) && !live.campaign.ending.consumed,
        "failed real store write preserves prior disk and unpublished live presentation");
    Directory.Delete(store.TempPath);
    store.Write(Envelope(candidate));
    live = candidate;
    string persisted = store.Read(Valid, out bool backup, out string problem);
    using var restoredEnvelope = JsonDocument.Parse(persisted);
    var persistedState = JsonSerializer.Deserialize<PlayerProgressData>(restoredEnvelope.RootElement.GetProperty("payload").GetString(), json);
    Check(!backup && problem == null && persistedState.campaign.ending.consumed && persistedState.campaign.ending.resultJson == two.campaign.ending.resultJson,
        "successful retry and signed reload retain consumed movie plus exact pending result");
    Check(persistedState.campaign.publicTrust == two.campaign.publicTrust && persistedState.campaign.coins == two.campaign.coins,
        "real signed presentation write changes no service rewards");
    ChapterEndingRules.Acknowledge(persistedState, "Level_02");
    store.Write(Envelope(persistedState));
    Check(!ChapterEndingRules.Pending(persistedState) && File.Exists(store.BackupPath), "acknowledged result clears handoff using existing backup store");
}
finally { Directory.Delete(temp, true); }

Console.WriteLine($"ChapterEndingRegression: {checks - failures}/{checks} checks passed");
Environment.ExitCode = failures == 0 ? 0 : 1;
