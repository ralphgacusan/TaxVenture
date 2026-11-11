// Level 3 ("The Numbers Tell a Story") regression, T02: chapter entry and
// resume rules, the chapter snapshot inside the signed campaign save (round
// trip, carried possessions, failed write), run-mode isolation, the Level 3
// content contract loaded from the real case files, Level_03 configuration,
// and the Level 1/2 defaults these additions must not change. Run from this
// folder:   dotnet run
// (build the scratch Assembly-CSharp first; see Level3Regression.csproj).
// T03-T05 add the calculator practice, Bea's records and her worksheet.
// Scene files are checked by Tests/VerifyLevel3Scene.py. Unity, Play Mode and
// rendering are NOT exercised here.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

int fails = 0, checks = 0;
void Check(bool ok, string what)
{
    checks++;
    if (!ok) { fails++; Console.WriteLine("FAIL: " + what); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string Res(string rel) => Path.Combine(root, "Assets", "Resources", rel);
string Src(string rel) => File.ReadAllText(Path.Combine(root, "Assets", rel)).Replace("\r\n", "\n");
JsonElement Json(string rel) => JsonDocument.Parse(File.ReadAllText(Res(rel))).RootElement;
int Index(string body, string text) => body.IndexOf(text, StringComparison.Ordinal);
var json = new JsonSerializerOptions { IncludeFields = true };
string Snap<T>(T value) => JsonSerializer.Serialize(value, json);
T Clone<T>(T value) => JsonSerializer.Deserialize<T>(Snap(value), json);

// ================= Office level profile =================
Check(OfficeLevelCatalog.LevelIdForScene("Office_Level3") == "Level_03" && OfficeLevelCatalog.Level3Id == Level3ChapterRules.LevelId,
    "Office_Level3 plays Level_03 (the chapter rules use the same id)");
Check(OfficeLevelCatalog.LevelIdForScene("Office_Level1") == "Level_01" && OfficeLevelCatalog.LevelIdForScene("Office_Level2") == "Level_02",
    "Levels 1 and 2 keep their scene mapping");
var level3 = Json("Levels/Level_03/level.json");
var l3Features = OfficeLevelCatalog.FeaturesForScene("Office_Level3",
    level3.GetProperty("officeFeatures").EnumerateArray().Select(f => f.GetString()), out string l3Problems);
Check(l3Problems == null && l3Features.SetEquals(new[] { OfficeFeature.Hud, OfficeFeature.Minimap, OfficeFeature.Guide, OfficeFeature.Fade }),
    "Office_Level3 opts into HUD, minimap, guide and fade, not the old single-case finale");
Check(level3.GetProperty("levelId").GetString() == "Level_03" &&
      level3.GetProperty("caseIds").EnumerateArray().Select(c => c.GetString()).SequenceEqual(new[] { "case_bea_001", "case_marine_001" }),
    "level.json lists Bea's case, then the company's (resource stems)");
Check(Level3ChapterRules.CaseIdForResource("case_bea_001") == "L3-BEA-001" && Level3ChapterRules.CaseIdForResource("case_marine_001") == "L3-MARINE-001" &&
      Level3ChapterRules.CaseIdForResource("case_rosa_001") == null, "resource stems map to the two business case ids only");
var level2 = Json("Levels/Level_02/level.json");
Check(OfficeLevelCatalog.FeaturesForScene("Office_Level2", level2.GetProperty("officeFeatures").EnumerateArray().Select(f => f.GetString()), out _)
      .SetEquals(new[] { OfficeFeature.Hud, OfficeFeature.Minimap, OfficeFeature.Guide, OfficeFeature.Fade, OfficeFeature.Finale }),
    "Level 2's office features are unchanged");

// ================= Chapter entry =================
Level3ChapterCheckpoint entry = Level3ChapterRules.CreateEntry();
Check(entry.IsSet && entry.version == Level3ChapterCheckpoint.CurrentVersion && entry.phase == "Reception" && entry.activeCaseId == "L3-BEA-001",
    "a fresh chapter starts at reception with Bea's case active");
Check(entry.cases.Select(c => c.caseId).SequenceEqual(new[] { "L3-BEA-001", "L3-MARINE-001" }) && entry.cases.All(c => !c.admitted && c.acquiredSourceIds.Count == 0),
    "both case subrecords exist from the start; nothing is admitted or acquired yet");
Check(Level3ChapterRules.Problem(entry) == null && entry.contentRevision == "L3-2026-v1", "the fresh chapter is usable against content revision L3-2026-v1");
Check(Level3ChapterRules.Decide(null) == Level3EntryDecision.Fresh && Level3ChapterRules.Decide(new Level3ChapterCheckpoint()) == Level3EntryDecision.Fresh,
    "nothing saved = fresh chapter");
foreach (string bad in new[] { "", " Reception", "reception", "Reception ", "99", "Practice" })
    Check(!Level3ChapterRules.TryParsePhase(bad, out _), $"phase name '{bad}' is refused (exact saved names only)");
foreach (Level3Phase phase in Enum.GetValues(typeof(Level3Phase)))
    Check(Level3ChapterRules.TryParsePhase(phase.ToString(), out Level3Phase back) && back == phase, $"phase {phase} round-trips by name");
Check(Enum.GetValues(typeof(Level3Phase)).Length == 16 && (int)Level3Phase.Reception == 0 && (int)Level3Phase.CompletedOffice == 15,
    "the sixteen SPEC phases keep their order");

// ---------- transitions ----------
Check(Level3ChapterRules.IsAllowed(Level3Phase.Reception, Level3Phase.BeaRecords), "revision: Reception -> BeaRecords is the first step");
Check(!Level3ChapterRules.IsAllowed(Level3Phase.Reception, Level3Phase.ToolPractice) && !Level3ChapterRules.IsAllowed(Level3Phase.ToolPractice, Level3Phase.WorksheetDraft) &&
      Level3ChapterRules.IsAllowed(Level3Phase.ToolPractice, Level3Phase.BeaRecords) && Enum.IsDefined(typeof(Level3Phase), "ToolPractice") && (int)Level3Phase.ToolPractice == 1,
    "the retired budget practice is never entered; its saved identity and legacy exit are kept");
Check(!Level3ChapterRules.IsAllowed(Level3Phase.BeaReview, Level3Phase.CompanyAppointment) && Level3ChapterRules.IsAllowed(Level3Phase.BeaDelivery, Level3Phase.CompanyAppointment),
    "the company appointment follows Bea's delivery only");
Check(!Level3ChapterRules.IsAllowed(Level3Phase.BeaDelivery, Level3Phase.WrapUpPending) && !Level3ChapterRules.IsAllowed(Level3Phase.CompanyDelivery, Level3Phase.CompletedOffice),
    "no single-case shortcut to wrap-up or completion");
Check(Level3ChapterRules.ExpectedActiveCase(Level3Phase.CompanyAppointment) == "L3-BEA-001" && Level3ChapterRules.ExpectedActiveCase(Level3Phase.CompanyComputation) == "L3-MARINE-001",
    "the company case binds at its admission, after the appointment");

// ---------- reception assignment command ----------
string entryBefore = Snap(entry);
Level3ChapterCheckpoint assigned = entry.Copy();
Check(Level3ChapterRules.AssignBea(assigned, out string reason) == Level3CommandOutcome.Applied && reason == null, "the reception assignment applies once");
Check(Snap(entry) == entryBefore, "a command changes only its copy (the live chapter is untouched until saved)");
Check(assigned.phase == "BeaRecords" && assigned.activeCaseId == "L3-BEA-001" && assigned.Case("L3-BEA-001").admitted &&
      !assigned.Case("L3-MARINE-001").admitted && assigned.milestones.SequenceEqual(new[] { "reception-assigned" }) &&
      assigned.actionIds.SequenceEqual(new[] { "Level_03/reception-assigned" }) && assigned.tapes.Count == 0 && !assigned.calculatorTaught,
    "revision: the assignment admits Bea, records the action and makes her records the next task (no budget tapes, no teaching marker)");
string assignedSnap = Snap(assigned);
Check(Level3ChapterRules.AssignBea(assigned, out _) == Level3CommandOutcome.Duplicate && Snap(assigned) == assignedSnap,
    "a repeated assignment (second talk, restore) is a quiet duplicate");
Level3ChapterCheckpoint skip = entry.Copy();
Check(Level3ChapterRules.TryAdvance(skip, Level3Phase.Reception, Level3Phase.WorksheetDraft, "skip", out _) == Level3CommandOutcome.Refused &&
      Level3ChapterRules.TryAdvance(skip, Level3Phase.Reception, Level3Phase.ToolPractice, "practice", out _) == Level3CommandOutcome.Refused && Snap(skip) == entryBefore,
    "an unlisted transition (skipping the records, entering the retired practice) is refused with no change");
Level3ChapterCheckpoint wrongFrom = assigned.Copy();
Check(Level3ChapterRules.TryAdvance(wrongFrom, Level3Phase.Reception, Level3Phase.ToolPractice, "other-id", out _) == Level3CommandOutcome.Refused,
    "a command from the wrong phase is refused");
Check(Level3ChapterRules.TryAdvance(null, Level3Phase.Reception, Level3Phase.ToolPractice, "x", out _) == Level3CommandOutcome.Refused &&
      Level3ChapterRules.TryAdvance(entry.Copy(), Level3Phase.Reception, Level3Phase.ToolPractice, " ", out _) == Level3CommandOutcome.Refused,
    "no record or no action id: refused");

// ---------- resume decisions ----------
Check(Level3ChapterRules.Decide(assigned) == Level3EntryDecision.Resume && Level3ChapterRules.PhaseOf(assigned) == Level3Phase.BeaRecords,
    "a saved revised BeaRecords chapter resumes there (no reception replay)");
Level3ChapterCheckpoint Mutate(Action<Level3ChapterCheckpoint> change) { var c = assigned.Copy(); change(c); return c; }
var blocked = new (string what, Level3ChapterCheckpoint state)[]
{
    ("a newer snapshot version", Mutate(c => c.version = Level3ChapterCheckpoint.CurrentVersion + 1)),
    ("an unknown phase", Mutate(c => c.phase = "Filing")),
    ("another content revision", Mutate(c => c.contentRevision = "L3-2027-v1")),
    ("a missing company subrecord", Mutate(c => c.cases.RemoveAt(1))),
    ("a third case", Mutate(c => c.cases.Add(new Level3CaseProgress { caseId = "L2-ROSA-001" }))),
    ("the company active before its admission", Mutate(c => c.activeCaseId = "L3-MARINE-001")),
    ("the company admitted before Bea's delivery", Mutate(c => c.Case("L3-MARINE-001").admitted = true)),
    ("Bea past reception but never assigned", Mutate(c => c.Case("L3-BEA-001").admitted = false)),
    ("a revised chapter waiting in the retired practice", Mutate(c => c.phase = "ToolPractice"))
};
foreach (var (what, state) in blocked)
    Check(Level3ChapterRules.Decide(state) == Level3EntryDecision.Blocked && Level3ChapterRules.Problem(state) != null,
        $"{what}: kept untouched (Blocked), never resumed or overwritten");

// ---------- quiet restore observers ----------
Check(Level3ChapterRules.ImpliedTriggers(Level3Phase.Reception).Count == 0 && Level3ChapterRules.ImpliedLessons(Level3Phase.Reception).Count == 0,
    "at reception nothing is implied (the welcome and check-in still happen)");
Check(Level3ChapterRules.ImpliedTriggers(Level3Phase.BeaRecords).Take(2).SequenceEqual(new[] { "event:ReceptionistTalkedTo", "event:ChapterAssignmentReceived" }) &&
      Level3ChapterRules.ImpliedLessons(Level3Phase.BeaRecords).Contains("welcome") && !Level3ChapterRules.ImpliedLessons(Level3Phase.BeaRecords).Contains(Level3ChapterRules.AssignmentLesson),
    "a resumed BeaRecords marks check-in done, never replays the welcome, and still points to Bea's table once");
Check(Level3ChapterRules.ImpliedLessons(Level3Phase.WorksheetDraft).Contains(Level3ChapterRules.AssignmentLesson),
    "past the handover, the go-to-Bea hint is implied (never replayed)");
Check(Level3ChapterRules.CoarseState(Level3Phase.ToolPractice) == "ReceiveCaseState" && Level3ChapterRules.CoarseState(Level3Phase.BeaReview) == null,
    "coarse FSM mapping: entry phases sit in ReceiveCaseState; later phases belong to their tickets");

// ---------- legacy completion guard ----------
foreach (string legacy in new[] { "CaseCompleteState", "LevelCompleteState", "ArchiveCaseState", "PrepareReturnState" })
    Check(!Level3ChapterRules.LegacyTransitionAllowed(legacy), $"{legacy} is refused in Level 3 (no single-case completion)");
foreach (string ok in new[] { "ReceiveCaseState", "IdleGameState", "ReviewDocumentsState", "AuditSubmittedState", "" })
    Check(Level3ChapterRules.LegacyTransitionAllowed(ok), $"'{ok}' stays available");
Check(Level3ChapterRules.LegacyCompletionStates.All(n => Type.GetType(n + ", Assembly-CSharp") != null),
    "every guarded name is a real game state class");

// ================= Campaign fixture (a finished Chapter 2 story) =================
PlayerProgressData Fixture(bool chapterTwoDone)
{
    var d = PlayerProgressData.CreateNewCampaign("fixture-campaign-l3", "2026-10-30T00:00:00Z");
    CampaignRecord c = d.campaign;
    c.currentChapterId = chapterTwoDone ? "Level_03" : "Level_02";
    c.completedChapterIds.Add("Level_01");
    if (chapterTwoDone) c.completedChapterIds.Add("Level_02");
    c.publicTrust = 63;
    c.coins = 245;
    c.rewardClaims.AddRange(new[] { "fixture-campaign-l3/Level_02/income-review/coins", "fixture-campaign-l3/Level_02/case-approved/coins" });
    c.completedActivityIds.AddRange(new[] { "Level_01/marie-follow-through", "Level_02/rosa-follow-through" });
    c.ownedUpgradeIds.AddRange(new[] { "bag_plus_one", "bookmark_set" });
    c.decor.Add(new DecorPlacement { itemId = "desk_plant", anchorId = "desk-left", arrived = true });
    c.ledger.Add(new CampaignLedgerEntry { claimId = "fixture-campaign-l3/Level_02/case-approved/coins", kind = "coins", amount = 100, balanceAfter = 245, reason = "Rosa's approved case" });
    c.rosaEthics = new RosaEthicsRecord
    {
        caseId = "L2-ROSA-001", commitmentId = "rosa-ethics/L2-ROSA-001/commitment", choiceId = "rosa-ethics/L2-ROSA-001/responsible",
        choiceText = "I cannot approve it yet. Let’s check the payment together.", committed = true, followThroughRequired = true, followThroughDone = true
    };
    c.rosaEthics.consequenceIds.Add("fixture-campaign-l3/Level_02/rosa-ethics/L2-ROSA-001/responsible-refusal/trust");
    c.inventory.hotbar[0] = new InventorySlot { itemId = "notes", quantity = 1 };
    c.inventory.hotbar[1] = new InventorySlot { itemId = "tax_code_book", quantity = 1 };
    c.inventory.bag[0] = new InventorySlot { itemId = "review_voucher", quantity = 2 };
    c.inventory.bag[1] = new InventorySlot { itemId = "marked_records", quantity = 1 };
    c.inventory.pins.Add("L2-ROSA-001/marked/R1");
    d.totalExp = 310;
    d.levelRecords.Add(new LevelRecord { levelId = "Level_02", bestExp = 150, bestHearts = 8, maxExp = 160, maxHearts = 9, passed = true, attempts = 1 });
    d.badgeRecords.Add(new BadgeRecord { badgeId = "mark_the_money_2", tier = 2 });
    d.tutorialsSeen.Add("Level_02/welcome");
    d.storyFacts.Add("rosa-desk-provenance");
    d.Normalize();
    return d;
}
PlayerProgressData live = Fixture(chapterTwoDone: true);
string WithoutChapter(PlayerProgressData d)
{
    JsonNode node = JsonNode.Parse(Snap(d));
    node["campaign"].AsObject().Remove("levelThree");
    return node.ToJsonString();
}
string liveSnap = Snap(live), liveWithout = WithoutChapter(live);
Check(!live.campaign.levelThree.IsSet, "a finished Chapter 2 campaign has no Level 3 chapter yet");

// ---------- mode isolation ----------
Check(Level3ChapterRules.StoryEntryAllowed(live) && !Level3ChapterRules.StoryEntryAllowed(Fixture(false)) && !Level3ChapterRules.StoryEntryAllowed(new PlayerProgressData()),
    "the story chapter continues only from a completed Chapter 2");
Check(Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.Story, false, live), "a story run of that campaign writes the chapter");
foreach (CampaignRunMode mode in new[] { CampaignRunMode.None, CampaignRunMode.MasteryReplay, CampaignRunMode.Practice, CampaignRunMode.DirectEditor })
    Check(!Level3ChapterRules.UsesStoryCheckpoint(mode, false, live), $"{mode} never writes the story chapter");
Check(!Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.Story, true, live), "practice inside a story route never writes it");
Check(!Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.Story, false, Fixture(false)), "a story campaign without Chapter 2 complete never writes it");
Check(Level3ChapterRules.WithCheckpoint(Clone(Fixture(false)), assigned) != null && Level3ChapterRules.WithCheckpoint(new PlayerProgressData(), assigned) == null &&
      Level3ChapterRules.WithCheckpoint(Clone(live), Mutate(c => c.phase = "Filing")) == null,
    "a candidate needs a campaign and a usable chapter (the manager adds the story/Chapter 2 gate)");

Check(Level3ChapterRules.UsesInventoryPreview(CampaignRunMode.DirectEditor, false, live, "Level_03"), "direct Level 3 development can preview completed Chapter 2 inventory locally");
foreach (CampaignRunMode mode in new[] { CampaignRunMode.Story, CampaignRunMode.MasteryReplay, CampaignRunMode.Practice, CampaignRunMode.None })
    Check(!Level3ChapterRules.UsesInventoryPreview(mode, false, live, "Level_03"), "only direct Level 3 tests preview carried inventory: " + mode);
Check(!Level3ChapterRules.UsesInventoryPreview(CampaignRunMode.DirectEditor, true, live, "Level_03") &&
      !Level3ChapterRules.UsesInventoryPreview(CampaignRunMode.DirectEditor, false, live, "Level_02") &&
      !Level3ChapterRules.UsesInventoryPreview(CampaignRunMode.DirectEditor, false, Fixture(false), "Level_03"),
      "practice, other chapters and unfinished Chapter 2 do not seed the development preview");
var previewPossessions = Clone(live.campaign.inventory);
InventoryRules.Remove(previewPossessions, InventoryCatalogue.BuiltIn(), InventoryCatalogue.ReviewVoucher, "preview-use");
Check(Snap(live) == liveSnap, "changing local preview possessions leaves live campaign and supplies untouched");

// ---------- candidate, round trip, carried possessions ----------
PlayerProgressData candidate = Level3ChapterRules.WithCheckpoint(Clone(live), assigned);
Check(candidate != null && Snap(live) == liveSnap, "the snapshot goes into a candidate copy; the live save is untouched");
PlayerProgressData reloaded = JsonSerializer.Deserialize<PlayerProgressData>(Snap(candidate), json);
bool repaired = reloaded.Normalize();
Check(!repaired, "a saved chapter reloads without repairs (no rewrite on load)");
Check(WithoutChapter(reloaded) == liveWithout,
    "wallet, Public Trust, Rosa history, claims, ledger, mastery, badges, tutorials, inventory, pins, upgrades and decor are carried exactly");
Level3ChapterCheckpoint restored = reloaded.campaign.levelThree;
Check(restored.phase == "BeaRecords" && restored.version == 2 && restored.writes == 1 && Level3ChapterRules.Decide(restored) == Level3EntryDecision.Resume &&
      restored.Case("L3-BEA-001").admitted && !restored.Case("L3-MARINE-001").admitted,
    "the chapter round-trips: version 2, BeaRecords, Bea admitted, company waiting, first write");
PlayerProgressData second = Level3ChapterRules.WithCheckpoint(Clone(reloaded), restored);
Check(second.campaign.levelThree.writes == 2 && WithoutChapter(second) == liveWithout, "a repeated save counts one more write and still changes nothing else");
string savedChapter = Snap(restored);
foreach (string answer in new[] { "expected", "worksheetDecisions", "level3", "62500", "600000", "50000", "1500000", "Personal purpose", "documentName" })
    Check(!savedChapter.Contains(answer), $"the chapter snapshot holds no answer/content data ('{answer}')");
var oldV5 = JsonSerializer.Deserialize<PlayerProgressData>(liveWithout, json);
Check(!oldV5.Normalize() && !oldV5.campaign.levelThree.IsSet, "a v5 save written before T02 loads unchanged (no Level 3 data invented, no rewrite)");
var dirty = Clone(candidate);
dirty.campaign.levelThree.actionIds.AddRange(new[] { " Level_03/reception-assigned ", "Level_03/reception-assigned", null, "" });
dirty.campaign.levelThree.cases.Add(new Level3CaseProgress { caseId = "L3-BEA-001" });
dirty.campaign.levelThree.cases.Add(null);
Check(dirty.Normalize() && dirty.campaign.levelThree.actionIds.SequenceEqual(new[] { "Level_03/reception-assigned" }) &&
      dirty.campaign.levelThree.cases.Count == 2 && dirty.campaign.levelThree.Case("L3-BEA-001").admitted,
    "structural repair trims and de-duplicates ids and keeps the first subrecord per case");
var future = Clone(candidate);
future.campaign.levelThree.version = 9;
future.Normalize();
Check(future.campaign.levelThree.version == 9 && Level3ChapterRules.Decide(future.campaign.levelThree) == Level3EntryDecision.Blocked,
    "a newer chapter version survives repair untouched and is refused on restore");

// ---------- failed write (disposable store) ----------
string sandbox = Path.Combine(Path.GetTempPath(), "TaxVentureL3Regression-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox);
try
{
    string Encode(PlayerProgressData d)
    {
        string payload = JsonSerializer.Serialize(d, json);
        return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) });
    }
    PlayerProgressData Decode(string text)
    {
        using JsonDocument doc = JsonDocument.Parse(text);
        string payload = doc.RootElement.GetProperty("payload").GetString();
        return SaveFileStore.SignatureMatches(payload, doc.RootElement.GetProperty("signature").GetString())
            ? JsonSerializer.Deserialize<PlayerProgressData>(payload, json) : null;
    }
    // Mirrors PlayerProgressManager.CommitLevelThreeCheckpoint -> TryWriteCandidate: write, then publish.
    bool Commit(SaveFileStore s, ref PlayerProgressData published, Level3ChapterCheckpoint state)
    {
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(published), state);
        if (next == null) return false;
        try { next.Normalize(); s.Write(Encode(next)); }
        catch (Exception) { return false; }
        published = next;
        return true;
    }

    var store = new SaveFileStore(Path.Combine(sandbox, "progress.json"));
    PlayerProgressData published = Clone(live);
    store.Write(Encode(published));
    store.Write(Encode(published));
    byte[] mainBefore = File.ReadAllBytes(store.FilePath), backupBefore = File.ReadAllBytes(store.BackupPath);

    Directory.CreateDirectory(store.TempPath); // injected failure: the temp file cannot be created
    bool saved = Commit(store, ref published, assigned);
    Check(!saved && !published.campaign.levelThree.IsSet && Snap(published) == liveSnap,
        "failed write: nothing is published (chapter still at reception, possessions unchanged)");
    Check(File.ReadAllBytes(store.FilePath).SequenceEqual(mainBefore) && File.ReadAllBytes(store.BackupPath).SequenceEqual(backupBefore),
        "failed write: both save generations on disk are unchanged");
    Directory.Delete(store.TempPath);

    saved = Commit(store, ref published, assigned);
    PlayerProgressData onDisk = Decode(File.ReadAllText(store.FilePath));
    Check(saved && onDisk != null && onDisk.campaign.levelThree.phase == "BeaRecords" && WithoutChapter(onDisk) == liveWithout,
        "retry: the signed save holds the chapter at BeaRecords with every carried possession intact");
    Check(Decode(File.ReadAllText(store.BackupPath)) is PlayerProgressData backup && !backup.campaign.levelThree.IsSet,
        "the backup keeps the previous generation");
    PlayerProgressData before = published;
    Check(Commit(store, ref published, Mutate(c => c.phase = "Filing")) == false && ReferenceEquals(before, published),
        "an unusable chapter state is never written");
}
finally
{
    try { Directory.Delete(sandbox, true); } catch (IOException) { }
}

// ---------- Rosa history line (read only) ----------
Check(Level3ChapterRules.HistoryVariant(live) == "responsible", "responsible Rosa choice -> its reception line");
PlayerProgressData Ethics(string choice, bool done) { var d = Fixture(true); d.campaign.rosaEthics.choiceId = "rosa-ethics/L2-ROSA-001/" + choice; d.campaign.rosaEthics.followThroughDone = done; return d; }
Check(Level3ChapterRules.HistoryVariant(Ethics("unsupported-promise", true)) == "unsupported-resolved" &&
      Level3ChapterRules.HistoryVariant(Ethics("unsupported-promise", false)) == "neutral" &&
      Level3ChapterRules.HistoryVariant(Ethics("dismissive-hold", true)) == "dismissive" &&
      Level3ChapterRules.HistoryVariant(new PlayerProgressData()) == "neutral",
    "unsupported (resolved) / dismissive / no history map to CONTENT's reception variants");
string historyBefore = Snap(live);
Level3ChapterRules.HistoryVariant(live);
Check(Snap(live) == historyBefore, "reading the history changes nothing");

// ================= Level 3 content contract (real case files) =================
CaseDefinition LoadCase(string file) => JsonSerializer.Deserialize<CaseDefinition>(File.ReadAllText(Res("Levels/Level_03/cases/" + file)), json);
CaseDefinition bea = LoadCase("case_bea_001.json");
CaseDefinition marine = LoadCase("case_marine_001.json");
List<string> beaProblems = Level3ContentValidator.Validate(bea, "L3-BEA-001");
List<string> marineProblems = Level3ContentValidator.Validate(marine, "L3-MARINE-001");
Check(beaProblems.Count == 0, "Bea's case content validates: " + string.Join("; ", beaProblems));
Check(marineProblems.Count == 0, "the company's case content validates: " + string.Join("; ", marineProblems));
Check(bea.caseId == "L3-BEA-001" && bea.HasLevel3 && bea.level3.sources.Count == 10 && bea.level3.rows.Count == 14 && bea.level3.totals.Count == 3 &&
      bea.level3.creditLinks.Count == 1 && bea.level3.rules.Count == 6 && bea.level3.worksheetDecisions.Count == 12 && bea.level3.expectedFields.Count == 8,
    "Bea: 10 sources, 14 rows, 3 totals, 1 credit link, 6 bands, 12 decisions, 8 expected fields (FIXTURE_CONTRACT)");
Check(marine.caseId == "L3-MARINE-001" && marine.level3.sources.Count == 7 && marine.level3.rows.Count == 15 && marine.level3.totals.Count == 3 &&
      marine.level3.corporateFacts.IsSet && marine.level3.worksheetDecisions.Count == 0 && marine.level3.expectedFields.Count == 6,
    "company: 7 sources, 15 rows, 3 totals, explicit corporate facts, no second worksheet, 6 expected fields");
Check(Level3ContentValidator.Validate(bea, "L3-MARINE-001").Count > 0 && Level3ContentValidator.Validate(marine, "L3-BEA-001").Count > 0,
    "a case loaded under the other stem is refused (requested/internal identity)");
foreach (CaseDefinition l3 in new[] { bea, marine })
{
    AnswerKeyDefinition key = l3.answerKey;
    Check(key.grossIncome == 0f && key.taxDue == 0f && key.finalTaxPayable == 0f && string.IsNullOrEmpty(key.taxpayerType),
        $"{l3.caseId}: the legacy float answer key stays blank (Level 3 answers live in level3 only)");
    Check(string.IsNullOrEmpty(l3.clientId), $"{l3.caseId}: no conference-room client is shown when the case loads");
}
foreach (string shown in new[] { "₱600,000.00", "₱62,500.00", "₱50,000.00", "₱200,000.00", "₱40,000.00" })
    Check(!bea.documents.SelectMany(d => d.fields).Any(f => f.displayValue == shown), $"Bea's papers never print the answer {shown}");
foreach (string shown in new[] { "₱6,000,000.00", "₱1,500,000.00" })
    Check(!marine.documents.SelectMany(d => d.fields).Any(f => f.displayValue == shown), $"the company's papers never print the answer {shown}");
Check(bea.receptionistGreeting.Count >= 2 && bea.receptionistGreeting[0].text == "{history}" &&
      bea.receptionistGreeting.Any(l => l.text.Contains("plaza") && l.text.Contains("records")) && !bea.receptionistGreeting.Any(l => l.text.Contains("calculator")),
    "revision: Bea's greeting opens with the history line and sends the player to her records (no calculator task)");

// ---------- each broken binding fails closed ----------
var mutations = new (string what, Action<CaseDefinition> change)[]
{
    ("no level3 block", c => c.level3 = new Level3CaseContent()),
    ("unknown schema version", c => c.level3.schemaVersion = 2),
    ("other content revision", c => c.level3.contentRevision = "L3-2026-v0"),
    ("internal case id mismatch", c => c.level3.caseId = "L3-MARINE-001"),
    ("period outside the tax year", c => c.level3.periodEnd = "2027-01-31"),
    ("unknown rounding policy", c => c.level3.roundingPolicyId = "WHOLE-PESO"),
    ("row amount differs from content", c => c.level3.rows[0].amount = "500000.01"),
    ("printed amount differs", c => c.documents.First(d => d.documentName == "B-REV").fields.First(f => f.semanticKey == "BLogoFees").displayValue = "₱500,001.00"),
    ("float-like money", c => c.level3.rows[0].amount = "500000"),
    ("amount beyond the bound", c => c.level3.rows[0].amount = "100000000.01"),
    ("row from an unknown source", c => c.level3.rows[0].sourceId = "C-REV"),
    ("source without its document", c => c.documents.RemoveAll(d => d.documentName == "B-PRINT")),
    ("unsupported paper type", c => c.level3.sources[0].paperType = 3),
    ("duplicate row id", c => c.level3.rows[1].rowId = c.level3.rows[0].rowId),
    ("support with another amount", c => c.level3.rows.First(r => r.rowId == "R7").amount = "31000.00"),
    ("support to another transaction", c => c.level3.rows.First(r => r.rowId == "R7").relation = "supports:R4"),
    ("relation loop", c => c.level3.rows.First(r => r.rowId == "R5").relation = "supports:R7"),
    ("subset larger than its parent", c => c.level3.rows.First(r => r.rowId == "B-WHT-INCOME").amount = "400000.01"),
    ("total that does not add up", c => c.level3.totals[1].amount = "354000.00"),
    ("total over another paper's row", c => c.level3.totals[0].componentRowIds.Add("R3")),
    ("credit link to a non-certificate", c => c.level3.creditLinks[0].creditRowId = "R1"),
    ("rule band gap", c => c.level3.rules[2].lower = "400000.01"),
    ("rule base not continuous", c => c.level3.rules[3].baseTax = "102000.00"),
    ("rate above one", c => c.level3.rules[1].rate = "1.5"),
    ("a paper repeating a field key", c => c.documents.First(d => d.documentName == "B-BG").fields.Last().semanticKey = "B-BG/period"),
    ("missing worksheet decision", c => c.level3.worksheetDecisions.RemoveAt(11)),
    ("expected tax off the rule", c => c.level3.expectedFields.First(f => f.field == "taxDue").amount = "120000.00"),
    ("expected balance off the chain", c => c.level3.expectedFields.First(f => f.field == "finalTaxPayable").amount = "62500.00"),
    ("corporate facts on an individual", c => c.level3.corporateFacts = new Level3CorporateFacts { ordinaryRate = "0.25" })
};
foreach (var (what, change) in mutations)
{
    CaseDefinition broken = Clone(bea);
    change(broken);
    Check(Level3ContentValidator.Validate(broken, "L3-BEA-001").Count > 0, $"content refused: {what}");
}
var companyMutations = new (string what, Action<CaseDefinition> change)[]
{
    ("missing corporate facts", c => c.level3.corporateFacts = new Level3CorporateFacts()),
    ("eligibility as OR", c => c.level3.corporateFacts.eligibilityOperator = "OR"),
    ("first MCIT year off its rule", c => c.level3.corporateFacts.firstMCITYear = 2027),
    ("asset row unknown", c => c.level3.corporateFacts.assetRowId = "C-NONE"),
    ("company with a scored worksheet", c => c.level3.worksheetDecisions.Add(new Level3DecisionDefinition { decisionId = "dest-R1", expected = "Income" })),
    ("company tax at the small rate", c => c.level3.expectedFields.First(f => f.field == "taxDue").amount = "1200000.00")
};
foreach (var (what, change) in companyMutations)
{
    CaseDefinition broken = Clone(marine);
    change(broken);
    Check(Level3ContentValidator.Validate(broken, "L3-MARINE-001").Count > 0, $"company content refused: {what}");
}
Check(Level3ContentValidator.Cents(22500.006m) == 22500.01m && Level3ContentValidator.Cents(0.0045m) == 0.00m && Level3ContentValidator.Cents(0.015m) == 0.02m,
    "cents policy rounds only the completed tax due, half up (FIXTURE_CONTRACT midpoints)");

// ---------- other levels keep their loader path ----------
CaseDefinition rosa = JsonSerializer.Deserialize<CaseDefinition>(File.ReadAllText(Res("Levels/Level_02/cases/case_rosa_001.json")), json);
CaseDefinition marie = JsonSerializer.Deserialize<CaseDefinition>(File.ReadAllText(Res("Levels/Level_01/cases/case_001.json")), json);
Check(!rosa.HasLevel3 && !marie.HasLevel3, "Level 1/2 cases carry no Level 3 block (their validation path is unchanged)");
string loader = Src("Scripts/Data/Loading/JsonCaseLoader.cs");
Check(loader.Contains("bool level3 = levelId == Level3ChapterRules.LevelId || def.HasLevel3;") &&
      loader.Contains("Level3ContentValidator.Validate(def,") &&
      loader.Contains("level3 ? new System.Collections.Generic.List<RequiredFieldDefinition>() : def.GradedFields()"),
    "JsonCaseLoader validates Level 3 content and skips the individual-only default graded fields only for Level 3");
string caseManager = Src("Scripts/Managers/CaseManager.cs");
Check(Index(caseManager, "levelToLoad == Level3ChapterRules.LevelId") > 0 && Index(caseManager, "levelToLoad == Level3ChapterRules.LevelId") < Index(caseManager, "CreateFallbackDefinition();") &&
      caseManager.Contains("CurrentDefinition = null;") && caseManager.Contains("CurrentCase = null;"),
    "a Level 3 load failure leaves no case (fail closed) instead of the placeholder");

// ================= Level_03 configuration =================
var start = Json("Levels/Level_03/office_start.json");
var start2 = Json("Levels/Level_02/office_start.json");
var hide3 = start.GetProperty("hide").EnumerateArray().Select(h => h.GetString()).ToList();
Check(start2.GetProperty("hide").EnumerateArray().All(h => hide3.Contains(h.GetString())) &&
      start.GetProperty("disableComponents").EnumerateArray().Select(c => c.GetString()).SequenceEqual(new[] { "TutorialController", "StampSetInteractable" }) &&
      start.GetProperty("hideDeskFolder").GetBoolean(),
    "office_start switches off Level 1's plaza crowd, corkboard quest, profile icon, waiting-area Marie, popup tutorial, desk folder flow and (T09) the inherited stamp set");
Check(!hide3.Any(h => h.Contains("Receptionist") || h.Contains("Auditor") || h.Contains("Player") || h.Contains("Camera") ||
                      h.Contains("Joystick") || h.Contains("EventSystem") || h.Contains("Door") || h.Contains("Light")),
    "no shared office service, control, camera, door or light is hidden");
var history = start.GetProperty("receptionHistory").EnumerateArray().ToDictionary(h => h.GetProperty("id").GetString(), h => h.GetProperty("text").GetString());
Check(history.Count == 4 &&
      history["responsible"] == "Rosa appreciated getting her checked copy. Bea has booked a computation appointment." &&
      history["unsupported-resolved"] == "Rosa's corrected copy is delivered. Bea needs a checked computation before making plans." &&
      history["dismissive"] == "Rosa's case is closed. Bea has brought her records and would like you to explain the result." &&
      history["neutral"] == "Bea has booked a computation appointment. Her records are ready in the plaza.",
    "the four reception lines are CONTENT's exact 'First reception' variants");
var objectives = Json("Levels/Level_03/objectives.json").GetProperty("objectives");
var steps = objectives.EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps.Count >= 2 && steps[0].GetProperty("trigger").GetString() == Level3ChapterRules.AssignmentTrigger &&
      steps[1].GetProperty("trigger").GetString() == Level3ChapterRules.RecordsTrigger &&
      !steps.Any(st => st.GetProperty("trigger").GetString() == Level3ChapterRules.CalculatorTrigger || st.GetProperty("text").GetString().Contains("Practise")),
    "revision objectives: check in at reception, then Bea's records (no budget practice step)");
Check(steps.All(s => ObjectiveTriggers.Validate(s.GetProperty("trigger").GetString()) == null), "every Level 3 objective trigger is a known event");
var rewards = Json("Levels/Level_03/rewards.json");
Check(!rewards.GetProperty("requireSideQuestBeforeOffice").GetBoolean() && rewards.GetProperty("officeDoors").GetArrayLength() == 0,
    "the office is open from the start (the receptionist is not gated by an old side quest)");
GuideScript guide = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
string guideReport = guide.Validate();
Check(guideReport == "" && guide.lessons.Select(l => l.id).Take(2).SequenceEqual(new[] { "welcome", Level3ChapterRules.AssignmentLesson }),
    "the Level 3 guide validates: welcome, then Bea's records (" + guideReport + ")");
Check(guide.lessons[1].when == "flag:assignmentReceived" && guide.lessons[1].after == "welcome" && guide.guide.name == "Arthur" &&
      guide.lessons[1].pages.Count() == 1 && guide.lessons[1].pages[0].IsHint && guide.lessons[1].pages[0].TextFor(true).Contains("Bea"),
    "Arthur points to Bea only after the assignment is committed (one short hint)");
Check(!guide.lessons.Any(l => new[] { "calculator-intro", "calculator-practice", "calculator-own-turn", "calculator-done" }.Contains(l.id) ||
          (l.when ?? "").Contains("practice") || (l.when ?? "").Contains("toolPractice")),
    "no retired budget-practice lesson remains in the guide");
var settings = Json("Settings/in_game_settings.json").GetProperty("scenes").EnumerateArray().Select(s => s.GetString()).ToList();
Check(settings.SequenceEqual(new[] { "Office_Level1", "Office_Level2", "Office_Level3" }), "the settings board serves Office_Level3 too");

// ---------- carried tools: scopes extended deliberately ----------
InventoryCatalogue builtIn = InventoryCatalogue.BuiltIn();
var shipped = Json("Campaign/inventory.json").GetProperty("items").EnumerateArray().ToDictionary(i => i.GetProperty("id").GetString(), i => i.GetProperty("scope").GetString());
foreach (string tool in new[] { InventoryCatalogue.Notes, InventoryCatalogue.CaseFolder, InventoryCatalogue.TaxCodeBook })
{
    Check(shipped[tool] == "Level_01,Level_02,Level_03" && builtIn.Find(tool).scope == shipped[tool], $"{tool} is scoped to Levels 1-3 (file and built-in copy agree)");
    Check(Level2CampaignRules.Eligibility(builtIn, tool, "Level_03") == CarryEligibility.ChapterTool, $"{tool} stays a chapter tool on Level 3 entry (never retired)");
    Check(Level2CampaignRules.Eligibility(builtIn, tool, "Level_02") == CarryEligibility.ChapterTool, $"{tool} is still a chapter tool in Level 2");
}
Check(!Level2CampaignRules.ResetsOnFreshAttempt(builtIn.Find(InventoryCatalogue.Notes), "Level_03") &&
      !Level2CampaignRules.ResetsOnFreshAttempt(builtIn.Find(InventoryCatalogue.TaxCodeBook), "Level_03"),
    "Notes and the Tax Code Book keep their saved slots on a fresh Level 3 attempt");
Check(Level2CampaignRules.Eligibility(builtIn, InventoryCatalogue.MarkedRecords, "Level_03") == CarryEligibility.ChapterBound &&
      Level2CampaignRules.Eligibility(builtIn, InventoryCatalogue.ProfileBinder, "Level_03") == CarryEligibility.ChapterBound,
    "Rosa's marked records and Level 1's Client IDs are other chapters' quest objects in Level 3 (never Bea's evidence)");
Check(Level2CampaignRules.Eligibility(builtIn, InventoryCatalogue.ReviewVoucher, "Level_03") == CarryEligibility.Supply &&
      Level2CampaignRules.Eligibility(builtIn, InventoryCatalogue.BookmarkSet, "Level_03") == CarryEligibility.Bookmark &&
      Level2CampaignRules.Eligibility(builtIn, InventoryCatalogue.ReviewedFile, "Level_03") == CarryEligibility.Reference &&
      Level2CampaignRules.Eligibility(builtIn, InventoryCatalogue.Postcard, "Level_03") == CarryEligibility.Keepsake,
    "supplies, bookmarks, Arthur's reference and keepsakes carry into Level 3 unchanged");
Check(Level2CampaignRules.ResetsOnFreshAttempt(builtIn.Find(InventoryCatalogue.Notes), "Level_01") &&
      !Level2CampaignRules.ResetsOnFreshAttempt(builtIn.Find(InventoryCatalogue.Notes), "Level_02") &&
      !Level2CampaignRules.ResetsOnFreshAttempt(builtIn.Find(InventoryCatalogue.CaseFolder), "Level_02") &&
      Level2CampaignRules.ResetsOnFreshAttempt(builtIn.Find(InventoryCatalogue.ReviewedFile), "Level_01") &&
      !Level2CampaignRules.ResetsOnFreshAttempt(builtIn.Find(InventoryCatalogue.ReviewedFile), "Level_02"),
    "Level 1 fresh attempt stays isolated; later chapters keep every owned tool");
Check(HudHotbar.UsesHotbar("Level_03") && HudHotbar.UsesHotbar("Level_01") && HudHotbar.UsesHotbar("Level_02"),
    "Level 3 uses the shared Bag/hotbar with chapter-aware world bindings");

// User continuation contract: entering later chapters must retain every owned item.
var carried = new CampaignInventoryRecord();
InventoryRules.Acquire(carried, builtIn, InventoryCatalogue.ProfileBinder, 1, "keep-ids", null,
    new InventoryItemRecord { itemId = InventoryCatalogue.ProfileBinder, contents = new List<string> { "profile_marie", "profile_milo" } });
InventoryRules.Acquire(carried, builtIn, InventoryCatalogue.MarkedRecords, 1, "keep-rosa");
InventoryRules.Acquire(carried, builtIn, InventoryCatalogue.CaseFolder, 1, "keep-folder");
foreach (string chapter in new[] { "Level_02", "Level_03" })
{
    string beforeEntry = Snap(carried);
    Check(Level2CampaignRules.RetireOtherChapters(carried, builtIn, chapter, "enter-" + chapter).Outcome == InventoryOutcome.NoChange,
        "chapter transition never retires collected items: " + chapter);
    Check(Snap(carried) == beforeEntry, "chapter transition preserves slots, quantities, records and action IDs exactly: " + chapter);
    Check(InventoryRules.Owns(carried, InventoryCatalogue.ProfileBinder) && InventoryRules.Owns(carried, InventoryCatalogue.MarkedRecords),
        "IDs and Rosa records remain owned: " + chapter);
    Check(!Level2CampaignRules.ResetsOnFreshAttempt(builtIn.Find(InventoryCatalogue.CaseFolder), chapter),
        "later chapter entry does not reset carried folder: " + chapter);
}

// Retained pages store display/player work only and preserve all earlier case identities.
var savedPage = new InventoryReferencePage { id = "L2-ROSA-001/D3", title = "Rosa's receipt", lines = new List<string> { "Blanket washing: 5,000", "Paid in May" } };
Check(InventoryRules.KeepReference(carried, InventoryCatalogue.MarkedRecords, savedPage).Changed, "acquired marked-record text retained");
savedPage.lines[0] = "caller changed";
Check(InventoryRules.Record(carried, InventoryCatalogue.MarkedRecords).referencePages[0].lines[0] == "Blanket washing: 5,000", "retained pages copy source lines instead of sharing mutable case state");
var roundTrip = Clone(carried);
Check(InventoryRules.Record(roundTrip, InventoryCatalogue.MarkedRecords).referencePages[0].lines[1] == "Paid in May", "retained record text survives serialized round trip");
Check(!InventoryRules.KeepReference(roundTrip, InventoryCatalogue.MarkedRecords,
    new InventoryReferencePage { id = "L2-ROSA-001/D3", title = "Rosa's receipt", lines = new List<string> { "Blanket washing: 5,000", "Paid in May" } }).Changed, "unchanged saved page is a no-op");
Check(InventoryRules.KeepReference(roundTrip, InventoryCatalogue.MarkedRecords,
    new InventoryReferencePage { id = "L2-ROSA-001/D4", title = "Rental", lines = new List<string> { "Rental: 3,000" } }).Changed &&
    InventoryRules.Record(roundTrip, InventoryCatalogue.MarkedRecords).referencePages.Count == 2, "new paper appends without replacing the previous one");
Check(!InventoryRules.KeepReference(roundTrip, "not-owned", savedPage).Changed, "reference capture cannot grant an unowned item");
string hotbarContinuation = Src("Scripts/UI/HUD/HudHotbar.cs");
Check(hotbarContinuation.Contains("Level3Checkpointer chapter = LevelThree ? Level3Checkpointer.Active : null;") &&
    hotbarContinuation.Contains("chapter == null || !chapter.IsReady || chapter.Blocked") &&
    hotbarContinuation.Contains("if (LevelThree) resumedRun = chapter.Resumed;"), "hotbar waits for Level 3 restore and never uses the Level 1 checkpoint there");
Check(hotbarContinuation.Contains("if (LevelOne && !resumedRun)") && !hotbarContinuation.Contains("else if (!folderCarried && folderOwned) RemoveWorld") &&
    !hotbarContinuation.Contains("RemoveWorld(InventoryCatalogue.ProfileBinder)"), "chapter entry and quiet world sync do not delete retained folder or IDs");
Check(Src("Scripts/UI/HUD/InventoryScreen.cs").Contains("SAVED PAPERS") && hotbarContinuation.Contains("OpenSavedRecords(itemId)"), "retained papers have a real Bag reader entry point");
Check(Src("Scripts/UI/HUD/HudProfileCards.cs").Contains("TaxpayerProfileDatabase.GetById(id)") &&
    hotbarContinuation.Contains("return TaxpayerProfileCollectionHUD.IconClickOverride();"), "retained IDs reuse the actual read-only ID-card viewer without collecting profiles again");

foreach (string chapter in new[] { "Level_02", "Level_03" })
    foreach (InventoryItemDefinition item in builtIn.items)
        Check(!Level2CampaignRules.ResetsOnFreshAttempt(item, chapter), "later chapter never clears carried item " + item.id + " in " + chapter);
Check(!Snap(roundTrip).Contains("correctValue") && !Snap(roundTrip).Contains("answerKey"), "read-only retained snapshot contains display lines, never tax answer keys");
string folderPointerContinuation = Src("Scripts/UI/HUD/HudSubmittableIcon.cs");
Check(folderPointerContinuation.Contains("private bool CanUseIcon() => isAvailable && (CanDragOut() ||") &&
    folderPointerContinuation.Contains("if (CanDragOut())") &&
    folderPointerContinuation.Contains("FolderToHUDIcon.GetStoredFolderCount() > 0"), "retained folder can be tapped for reading but submission drag still requires the actual current folder");
Check(Src("Scripts/UI/HUD/OfficeHudRuntime.cs").Contains("!HudHotbar.Instance.HasCurrentFolder") &&
    Src("Scripts/SideQuest1/WorldCollectibles.cs").Contains("card.ShowReferences(pages);"), "retained folder click reaches read-only pages without swapping the active case");

// ---------- career gate ----------
var chapter3 = Json("Career/chapters.json").GetProperty("chapters").EnumerateArray().Single(c => c.GetProperty("id").GetString() == "Level_03");
Check(!chapter3.GetProperty("comingSoon").GetBoolean() && !chapter3.GetProperty("campaignPending").GetBoolean() &&
    chapter3.GetProperty("scene").GetString() == "Office_Level3", "Career Chapter 3 enables its normal APK story route for playtesting");
Check(Src("Scripts/RewardsSystem/CareerProgressService.cs").Contains("scene = \"Office_Level3\", cases = 1, comingSoon = false, campaignPending = false"), "built-in career fallback also enables the Level 3 route");

var playtestCatalog = JsonSerializer.Deserialize<CareerCatalog>(File.ReadAllText(Res("Career/chapters.json")), json);
var l3CareerBefore = Fixture(false);
var l3CareerAfter = Fixture(true);
l3CareerAfter.levelRecords.Clear();
l3CareerAfter.campaign.publicTrust = 0;
l3CareerAfter.campaign.coins = 0;
string l3CareerSaveBefore = Snap(l3CareerAfter);
CareerChapterRules PlaytestRules(CareerChapterDef def) => new CareerChapterRules { maxExp = 100, maxHearts = 0, passingPercent = 80 };
var l3BeforeView = CareerProgress.Build(l3CareerBefore, playtestCatalog, PlaytestRules).FindChapter("Level_03");
var l3AfterView = CareerProgress.Build(l3CareerAfter, playtestCatalog, PlaytestRules).FindChapter("Level_03");
Check(l3BeforeView.Status == CareerChapterStatus.Locked && !l3BeforeView.Launchable,
    "APK entry: incomplete Level 2 keeps Level 3 locked, including delivery still pending");
Check(l3AfterView.Status == CareerChapterStatus.Available && l3AfterView.Launchable && l3AfterView.Def.scene == "Office_Level3" &&
    CareerProgress.LaunchMode(l3AfterView) == CampaignRunMode.Story && Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.Story, false, l3CareerAfter),
    "APK entry: completed Level 2 opens Level 3 as a real saved Story run, without score/trust/coin gates");
Check(Snap(l3CareerAfter) == l3CareerSaveBefore && l3CareerAfter.campaign.levelThree?.IsSet != true,
    "APK entry: Career projection preserves the campaign and possessions without creating/resetting a checkpoint");
var l3FallbackView = CareerProgress.Build(l3CareerAfter, CareerProgressService.BuiltInCatalog(), PlaytestRules).FindChapter("Level_03");
Check(l3FallbackView.Launchable && l3FallbackView.Def.scene == l3AfterView.Def.scene && l3FallbackView.Title == l3AfterView.Title,
    "APK entry: authored and fallback catalogues route to the same chapter");
Check(CareerProgress.Build(l3CareerAfter, playtestCatalog, PlaytestRules).Chapters.Where(v => v.Def.number >= 4).All(v => v.Status == CareerChapterStatus.ComingSoon && !v.Launchable),
    "APK entry: Chapters 4 and 5 remain Coming Soon");
var resumedPlaytest = l3CareerAfter.campaign.levelThree;
l3CareerAfter.campaign.levelThree = Level3ChapterRules.CreateEntry();
var resumeCareerView = CareerProgress.Build(l3CareerAfter, playtestCatalog, PlaytestRules).FindChapter("Level_03");
Check(resumeCareerView.Launchable && CareerProgress.LaunchMode(resumeCareerView) == CampaignRunMode.Story &&
    Level3ChapterRules.Decide(l3CareerAfter.campaign.levelThree) == Level3EntryDecision.Resume,
    "APK entry: unfinished Level 3 reopens in Story with its existing checkpoint");
l3CareerAfter.campaign.levelThree = resumedPlaytest;
Check(File.ReadAllText(Path.Combine(root, "ProjectSettings/EditorBuildSettings.asset")).Replace("\r\n", "\n").Contains("enabled: 1\n    path: Assets/Scenes/Office_Level3.unity"),
    "APK entry: Office_Level3 is enabled in the actual build scene list");

// ================= Runtime wiring (source checks; Unity behaviour is user-verified) =================
string start3 = Src("Scripts/Level3/Level3OfficeStart.cs");
Check(start3.Contains("if (OfficeLevelProfile.LevelIdFor(scene) != OfficeLevelCatalog.Level3Id) return;") &&
      start3.Contains("public const string RootName = \"Level3Gameplay\";") &&
      start3.Contains("if (candidate.GetComponentInChildren<Level3OfficeStart>(true) != null) return;"),
    "Level3OfficeStart runs only in Office_Level3 and builds one Level3Gameplay root (reload never duplicates it)");
string start2Src = Src("Scripts/Level2/Level2OfficeStart.cs");
double Light(string src, string name) => double.Parse(System.Text.RegularExpressions.Regex.Match(src, name + @"\.intensity = ([\d.]+)f;").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
Check(Light(start3, "daylight") == Light(start2Src, "daylight") && Light(start3, "fill") == Light(start2Src, "fill") &&
      start3.Contains("daylight.color = new Color(1f, 0.97f, 0.92f);") && start3.Contains("fill.color = new Color(0.92f, 0.95f, 1f);") &&
      !start3.Contains("RenderSettings."),
    "Level 3 keeps Level 2's runtime office lighting balance on its own scene lights (no global render settings)");
Check(start3.Contains("EnsureOfficeActors();") && start3.Contains("rig.enabled = true;") &&
      start3.Contains("EnsurePart<Level3Checkpointer>(Level3Checkpointer.ChildName);") && start3.Contains("EnsurePart<Level2DeskStamps>(Level2DeskStamps.ChildName);") && !start3.Contains("EnsurePart<Level2Audit>") && !start3.Contains("EnsurePart<Level2Checkpointer>"),
    "Level 3 adds the same receptionist/Auditor rigs, adds its checkpointer, and builds no Rosa part");
Check(start3.Contains("ReceptionistInteractable.GreetingFilter = FillHistory") &&
      start3.Contains("if (ReceptionistInteractable.GreetingFilter == (Func<string, string>)FillHistory) ReceptionistInteractable.GreetingFilter = null;") &&
      start3.Contains("folder.HideAndDisableFolder();"),
    "the history line is a scoped greeting filter (released on disable); the desk folder uses Level 2's existing hide");
Check(start2Src.Contains("BalanceLighting();") && start2Src.Contains("EnsureOfficeActors();") &&
      start2Src.Contains("if (OfficeLevelProfile.LevelIdFor(scene) != OfficeLevelCatalog.Level2Id) return;") &&
      start2Src.Contains("public const int SetupVersion = 16;") && !start2Src.Contains("Level3"),
    "Level 2's start is untouched (same version, gate, lighting and rigs; no Level 3 code)");
string cp = Src("Scripts/Level3/Level3Checkpointer.cs");
Check(Index(cp, "PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate)") > 0 &&
      Index(cp, "PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate)") < Index(cp, "Publish(candidate);") &&
      Index(cp, "pendingCandidate = candidate;") < Index(cp, "Publish(candidate);"),
    "story: the assignment is written before it is published; a failed write keeps it pending for retry");
Check(cp.Contains("if (before == Level3Phase.Reception && Phase > Level3Phase.Reception) GameplayEvents.RaiseChapterAssignmentReceived();") &&
      cp.Split("RaiseChapterAssignmentReceived").Length == 2,
    "ChapterAssignmentReceived is raised once, only by a committed transition");
int restoreStart = Index(cp, "private void RestoreQuietly(bool welcomeBack = true)"), restoreEnd = Index(cp, "private static string ActiveDefinitionId");
string restoreBody = cp.Substring(restoreStart, restoreEnd - restoreStart);
Check(!restoreBody.Contains("GameplayEvents.Raise") && restoreBody.Contains("RestoreSpoken(") && restoreBody.Contains("SetAvailable(true)") &&
      restoreBody.Contains("MarkDone(trigger)"),
    "resume is quiet: receptionist done, Notes available, objectives caught up, no gameplay event raised");
Check(cp.Contains("!IsDirectEditorAttempt && !Blocked && PlayerProgressManager.Instance != null && PlayerProgressManager.Instance.UsesLevelThreeStory"),
    "only a story run (not a direct Editor attempt, not a blocked snapshot) saves");
Check(cp.Contains("GameStateMachine.TransitionGuard = AllowsTransition;") &&
      cp.Contains("if (GameStateMachine.TransitionGuard == (System.Func<IGameState, bool>)AllowsTransition) GameStateMachine.TransitionGuard = null;") &&
      cp.Contains("GuideFlags.Add(Flag);") && cp.Contains("GuideFlags.Remove(Flag);") &&
      cp.Contains("GameplayEvents.OnNotesUnlockRequested += HandleReceptionistDone;") && cp.Contains("GameplayEvents.OnNotesUnlockRequested -= HandleReceptionistDone;"),
    "the legacy guard, guide flags and receptionist subscription are scoped to the enabled owner");
string fsm = Src("Scripts/FSM/GameStateMachine.cs");
Check(Index(fsm, "if (TransitionGuard != null && !TransitionGuard(newState))") < Index(fsm, "if (transitioning)") &&
      fsm.Contains("if (TransitionGuard != null && !TransitionGuard(next))"),
    "GameStateMachine asks the guard before a transition and before a queued one");
string manager = Src("Scripts/RewardsSystem/PlayerProgressManager.cs");
int commitAt = Index(manager, "public bool CommitLevelThreeCheckpoint(");
string commitBody = manager.Substring(commitAt, Index(manager, "public bool ClearLevelThreeCheckpoint(") - commitAt);
Check(commitBody.Contains("if (!UsesLevelThreeStory) return false;") && commitBody.Contains("Level3ChapterRules.WithCheckpoint(CloneProgress(), state)") &&
      commitBody.Contains("TryWriteCandidate(candidate)"),
    "the save owner composes a candidate and publishes only through its existing signed write");
Check(manager.Contains("Level3ChapterRules.UsesStoryCheckpoint(CurrentRunMode, TrainingSession.IsPracticing, progress)") &&
      manager.Contains("CampaignSave.CanContinue(campaignState)"),
    "a newer/unreadable save is never written by Level 3 (existing future-version policy)");
string director = Src("Scripts/UI/Guide/GuideDirector.cs");
Check(director.Contains("Level3Checkpointer chapter = Level3Checkpointer.Active;") && director.Contains("if (chapter == null || !chapter.IsReady) return;") &&
      director.Contains("Level3ChapterRules.ImpliedLessons(chapter.State)"),
    "the guide waits for the chapter restore and skips lessons a resumed phase already passed");
string objectivesSrc = Src("Scripts/UI/HUD/OfficeObjectives.cs");
Check(objectivesSrc.Contains("GameplayEvents.OnChapterAssignmentReceived += HandleChapterAssignmentReceived;") &&
      objectivesSrc.Contains("GameplayEvents.OnChapterAssignmentReceived -= HandleChapterAssignmentReceived;") &&
      objectivesSrc.Contains("GameplayEvents.OnCalculatorPracticeAttached -= HandleCalculatorPracticeAttached;"),
    "objectives observe the committed Level 3 events and unsubscribe cleanly");
string settingsSrc = Src("Scripts/Settings/InGameSettings.cs");
Check(Index(settingsSrc, "Level3Checkpointer.Active.PrepareSettingsExit(replay)") < Index(settingsSrc, "SaveBeforeLeaving()"),
    "leaving through settings saves the chapter first; a failed write keeps the player in the level");

// =====================================================================
// T03: the desk calculator and its protected practice
// =====================================================================
int t03Start = checks, t03Fails = fails;
Level3Calculator Calc() => new Level3Calculator();
void Keys(Level3Calculator c, string keys)
{
    foreach (char k in keys)
    {
        Level3CalcKey key = k switch
        {
            '+' => Level3CalcKey.Add, '-' => Level3CalcKey.Subtract, '*' => Level3CalcKey.Multiply, '/' => Level3CalcKey.Divide,
            '%' => Level3CalcKey.Percent, '=' => Level3CalcKey.Equals, '.' => Level3CalcKey.Point, 'C' => Level3CalcKey.Clear,
            'E' => Level3CalcKey.ClearEntry, '<' => Level3CalcKey.Backspace, _ => (Level3CalcKey)((int)Level3CalcKey.D0 + (k - '0'))
        };
        c.Press(key);
    }
}
Level3PracticeSource Src3(string id) => Level3PracticeRules.Sources(null).First(s => s.SourceId == id);

// ---------- revision: the budget practice is retired; its states survive only as version 1 fixtures ----------
// The calculator itself is unchanged (exact decimals, labelled sources, printed tapes). 'practice',
// 'attach1' and 'legacyPracticed' are disposable version 1 snapshots built exactly as the pre-revision
// practice rules saved them; no revised chapter enters ToolPractice, so AttachTape only guards it now.
Level3ChapterCheckpoint V1(Level3ChapterCheckpoint s) { var c = s.Copy(); c.version = Level3ChapterCheckpoint.LegacyVersion; return c; }
Level3TapeRecord LegacyPracticeTape(string slot, Level3Calculator c)
{
    var t = new Level3TapeRecord { tapeId = "tape-" + slot.Replace('/', '-'), caseId = Level3PracticeRules.CaseId, destinationId = slot, result = c.DraftResult, actionId = Level3PracticeRules.ActionFor(slot) };
    foreach (Level3CalcToken token in c.DraftExpression) t.expression.Add(token.Copy());
    return t;
}
Level3ChapterCheckpoint Continued(Level3ChapterCheckpoint legacy) => Level3ChapterRules.TryContinueLegacy(legacy, out Level3ChapterCheckpoint c, out _) ? c : null;
Level3ChapterCheckpoint practice = V1(entry);
practice.phase = "ToolPractice";
practice.Case("L3-BEA-001").admitted = true;
practice.actionIds.Add(Level3ChapterRules.AssignBeaAction);
practice.milestones.Add(Level3ChapterRules.ReceptionAssignedMilestone);
string practiceBefore = Snap(practice);
var calc = Calc();
Level3PracticeSource budget = Src3(Level3PracticeRules.BudgetSource), paper = Src3(Level3PracticeRules.PaperSource), courier = Src3(Level3PracticeRules.CourierSource);
calc.SelectSource(budget.SourceId, budget.Label, budget.Amount);
Check(calc.DisplayText == "100.00" && calc.Caption == "from Budget", "tapping Budget puts 100.00 on the display, labelled with its source");
calc.Press(Level3CalcKey.Subtract);
calc.SelectSource(paper.SourceId, paper.Label, paper.Amount);
Level3CalcResult printed = calc.Press(Level3CalcKey.Equals);
Check(printed.Printed && calc.HasDraft && calc.DraftResult == "80" && calc.DisplayText == "80.00", "= prints a draft tape with 80");
Check(calc.DraftExpression.Count == 3 && calc.DraftExpression[0].sourceId == Level3PracticeRules.BudgetSource && !calc.DraftExpression[0].manual &&
      calc.DraftExpression[1].op == "-" && calc.DraftExpression[2].sourceId == Level3PracticeRules.PaperSource,
    "the tape keeps each number's source (Budget, Paper order) and the operation");
Check(Level3Calculator.TapeLines(calc.DraftExpression, calc.DraftResult).SequenceEqual(new[] { "100.00 BUDGET", "− 20.00 PAPER", "= 80.00" }),
    "the tape prints 100.00 BUDGET / − 20.00 PAPER / = 80.00");
Level3ChapterCheckpoint freshPractice = assigned.Copy();
string freshPracticeBefore = Snap(freshPractice);
Check(Level3PracticeRules.AttachTape(freshPractice, Level3PracticeRules.FirstSlot, calc.DraftExpression, calc.DraftResult, out string freshHint) == Level3CommandOutcome.Refused &&
      freshHint == Level3PracticeRules.RefuseNotNow && Snap(freshPractice) == freshPracticeBefore,
    "revision: a fresh chapter has no budget practice to attach to (nothing changes)");
Level3ChapterCheckpoint v1Attach = practice.Copy();
Check(Level3PracticeRules.AttachTape(v1Attach, Level3PracticeRules.FirstSlot, calc.DraftExpression, calc.DraftResult, out _) == Level3CommandOutcome.Refused && Snap(v1Attach) == practiceBefore,
    "a version 1 practice chapter is never written to before it is continued");
Level3ChapterCheckpoint attach1 = practice.Copy();
attach1.tapes.Add(LegacyPracticeTape(Level3PracticeRules.FirstSlot, calc));
attach1.actionIds.Add(Level3PracticeRules.ActionFor(Level3PracticeRules.FirstSlot));
attach1.milestones.Add(Level3PracticeRules.FirstMilestone);
Check(Level3PracticeRules.Sources(attach1).Any(s => s.SourceId == Level3PracticeRules.FirstTapeSource && s.Amount == "80"),
    "a kept legacy tape stays a readable source (reopen tape)");
calc.ConsumeDraft();
Check(!calc.HasDraft && calc.DisplayText == "80.00", "the attached tape leaves the calculator (the display keeps the result)");
Level3PracticeSource tape1 = Level3PracticeRules.Sources(attach1).First(s => s.SourceId == Level3PracticeRules.FirstTapeSource);
calc.SelectSource(tape1.SourceId, tape1.Label, tape1.Amount);
calc.Press(Level3CalcKey.Subtract);
calc.SelectSource(courier.SourceId, courier.Label, courier.Amount);
calc.Press(Level3CalcKey.Equals);
Check(calc.DraftResult == "72" && Level3Calculator.TapeLines(calc.DraftExpression, calc.DraftResult).SequenceEqual(new[] { "80.00 TAPE", "− 8.00 COURIER", "= 72.00" }),
    "tape 1 minus the courier prints 72");
Level3ChapterCheckpoint legacyPracticed = attach1.Copy();
legacyPracticed.tapes.Add(LegacyPracticeTape(Level3PracticeRules.SecondSlot, calc));
legacyPracticed.actionIds.AddRange(new[] { Level3PracticeRules.ActionFor(Level3PracticeRules.SecondSlot), Level3PracticeRules.CompleteAction });
legacyPracticed.milestones.Add(Level3PracticeRules.CompleteMilestone);
legacyPracticed.phase = "BeaRecords";
Check(new[] { practice, attach1, legacyPracticed }.All(l => Level3ChapterRules.Decide(l) == Level3EntryDecision.Continue && Level3ChapterRules.Problem(l) != null),
    "every legacy practice state is continued on entry (never resumed raw, blocked or reset)");
Level3ChapterCheckpoint attach2 = Continued(legacyPracticed);
Check(attach2 != null && attach2.version == 2 && attach2.phase == "BeaRecords" && attach2.tapes.Count == 2 && attach2.calculatorTaught &&
      attach2.actionIds.SequenceEqual(legacyPracticed.actionIds) && attach2.milestones.SequenceEqual(legacyPracticed.milestones) &&
      Level3ChapterRules.Problem(attach2) == null && Level3PracticeRules.IsComplete(attach2),
    "a legacy chapter that finished practice continues at BeaRecords: both tapes kept as references, the calculator counted as taught, nothing else added");
PlayerProgressData practiced = Level3ChapterRules.WithCheckpoint(Clone(live), attach2);
Check(practiced != null && WithoutChapter(practiced) == liveWithout && practiced.campaign.coins == live.campaign.coins &&
      practiced.campaign.publicTrust == live.campaign.publicTrust && practiced.totalExp == live.totalExp &&
      practiced.campaign.rewardClaims.SequenceEqual(live.campaign.rewardClaims),
    "continuing changes no wallet, trust, XP, claims, inventory or first check: only the chapter snapshot");

// ---------- manual entry; legacy tapes never duplicated or resumed as a task ----------
var typed = Calc();
Keys(typed, "100-20=");
Check(typed.DraftExpression.All(t => !t.IsNumber || t.manual) && typed.DraftResult == "80",
    "typing 100 − 20 by hand works (manual entry stays possible; marked as typed)");
Level3ChapterCheckpoint again = Continued(attach1);
string againBefore = Snap(again);
Check(Level3PracticeRules.AttachTape(again, Level3PracticeRules.FirstSlot, typed.DraftExpression, typed.DraftResult, out _) == Level3CommandOutcome.Duplicate &&
      Snap(again) == againBefore, "a kept legacy tape is a quiet duplicate (no second tape)");
Check(Level3PracticeRules.AttachTape(again, Level3PracticeRules.SecondSlot, calc.DraftExpression, calc.DraftResult, out string againWhy) == Level3CommandOutcome.Refused &&
      againWhy == Level3PracticeRules.RefuseNotNow && Snap(again) == againBefore,
    "a half-finished legacy practice is never resumed as a task after continuing");
Level3ChapterCheckpoint finished = attach2.Copy();
Check(Level3PracticeRules.AttachTape(finished, Level3PracticeRules.SecondSlot, calc.DraftExpression, calc.DraftResult, out _) == Level3CommandOutcome.Duplicate &&
      finished.tapes.Count == 2, "after practice no tape is duplicated");

// ---------- arithmetic: exact, bounded, deterministic ----------
string Run(string keys) { var c = Calc(); Keys(c, keys); return c.Issue ?? c.DraftResult; }
Check(Run("22500+.2*200000=") == "62500", "precedence: 22,500 + 0.20 × 200,000 = 62,500 (as written on a tape)");
Check(Run("200000*20%=") == "40000", "% means A × 20% = A × 0.20");
Check(Run("100+20%=") == "100.2", "A + 20% is A + 0.20: no consumer 'add a percentage' shortcut");
Check(Run(".1+.2=") == "0.3", "0.1 + 0.2 is exactly 0.3 (decimal, never float)");
Check(Run("10/4=") == "2.5" && Level3Calculator.Format("2.5") == "2.50", "10 ÷ 4 = 2.50 exactly");
var third = Calc(); Keys(third, "1/3=");
Check(Level3Calculator.Format(third.DraftResult).EndsWith("…") && third.DisplayText.StartsWith("0.333333"), "1 ÷ 3 shows 0.333333… (display rounding only; the value stays exact)");
var zero = Calc(); Keys(zero, "5/0");
var zeroTokens = zero.Tokens.Count;
zero.Press(Level3CalcKey.Equals);
Check(zero.Issue == Level3Calculator.IssueDivideByZero && !zero.HasDraft && zero.Tokens.Count == zeroTokens && zero.Entry == "0",
    "divide by zero: an input problem on the display, the expression kept for correction, no tape");
Check(Run("999999999999*10=") == Level3Calculator.IssueTooLarge, "overflow is reported, never truncated");
var longNumber = Calc(); Keys(longNumber, "1234567890123");
Check(longNumber.Issue == Level3Calculator.IssueTooLong && longNumber.Entry == "123456789012", "a 13th digit is refused; the 12 digits stay");
var decimals = Calc(); Keys(decimals, "1.1234567");
Check(decimals.Entry == "1.123456" && decimals.Issue == Level3Calculator.IssueTooLong, "at most six decimal places");
var trailing = Calc(); Keys(trailing, "5+=");
Check(trailing.Issue == Level3Calculator.IssueUnfinished && !trailing.HasDraft, "= after an operator asks to finish the calculation");
var leading = Calc(); Keys(leading, "*");
Check(leading.Issue == Level3Calculator.IssueNoNumber && leading.Tokens.Count == 0, "an operator first asks for a number");
Check(Run("-5+2=") == "-3" && Level3Calculator.Format("-3") == "-3.00", "a leading − starts a negative number; negative results stay visible");
Check(Run("5-8=") == "-3", "a negative balance is shown, never clamped to zero");
Check(Run("5+*2=") == "10", "two operators in a row: the newer replaces the older");
Check(Run("12<3=") == "13" && Run("12+<<4=") == "14", "backspace edits the number, and steps back over an operator");
var keep = Calc(); Keys(keep, "100-20=C");
Check(keep.HasDraft && keep.DraftResult == "80" && keep.DisplayText == "0", "C clears the expression; the printed draft tape stays until attached or replaced");
var entryClear = Calc(); Keys(entryClear, "100-25E20=");
Check(entryClear.DraftResult == "80", "CE clears only the number being entered");
var twice = Calc(); Keys(twice, "11");
Check(twice.Entry == "11", "each tap is one input (two taps = two digits; the desk enters on tap-release, a held finger never repeats)");
var cont = Calc(); Keys(cont, "100-20=-8=");
Check(cont.DraftResult == "72" && cont.DraftExpression[0].sourceId == Level3Calculator.PreviousResultSource,
    "an operator after = continues from the result (labelled as the previous result)");
var swap = Calc(); Keys(swap, "123"); swap.SelectSource(budget.SourceId, budget.Label, budget.Amount);
Check(swap.Entry == "100" && swap.EntrySource.sourceId == Level3PracticeRules.BudgetSource, "tapping a source replaces a half-typed number");
Keys(swap, "5");
Check(swap.Entry == "5" && swap.EntrySource == null, "typing after a source starts a new typed number");
Check(Level3Calculator.Evaluate(new List<Level3CalcToken>(), out _) != null && Level3Calculator.Evaluate(null, out _) != null,
    "an empty or missing expression never evaluates");
Check(Level3Calculator.Tag("Paper order") == "PAPER" && Level3Calculator.Tag("Tape 1 (left after paper order)") == "TAPE" && Level3Calculator.Tag("Courier") == "COURIER",
    "tape tags are short source words");

// ---------- keys on the physical calculator ----------
var layoutKeys = new HashSet<Level3CalcKey>();
foreach (Level3CalcKey key in Level3CalculatorModel.Layout) layoutKeys.Add(key);
Check(layoutKeys.Count == 20 && layoutKeys.SetEquals((Level3CalcKey[])Enum.GetValues(typeof(Level3CalcKey))) &&
      Level3CalculatorModel.Layout.GetLength(0) == 5 && Level3CalculatorModel.Layout.GetLength(1) == 4,
    "every calculator key has exactly one cap on the 4 × 5 keypad");
Check(((Level3CalcKey[])Enum.GetValues(typeof(Level3CalcKey))).All(k => !string.IsNullOrWhiteSpace(Level3CalculatorModel.Label(k))),
    "every keycap carries a printed label");

// ---------- the snapshot with legacy tapes: round trip, repair, quiet resume ----------
PlayerProgressData tapeSave = JsonSerializer.Deserialize<PlayerProgressData>(Snap(Level3ChapterRules.WithCheckpoint(Clone(live), Continued(attach1))), json);
Check(!tapeSave.Normalize() && Snap(tapeSave.campaign.levelThree.tapes) == Snap(attach1.tapes) &&
      Level3ChapterRules.Decide(tapeSave.campaign.levelThree) == Level3EntryDecision.Resume,
    "a continued chapter with its legacy tape reloads exactly, without repairs, and resumes");
Level3ChapterCheckpoint deep = attach1.Copy();
deep.tapes[0].expression[0].value = "999";
Check(attach1.tapes[0].expression[0].value == "100", "Copy() is deep (a command never edits the live tape)");
Level3ChapterCheckpoint doubled = attach1.Copy();
doubled.tapes.Add(attach1.tapes[0].Copy());
doubled.tapes.Add(null);
Check(doubled.Repair() && doubled.tapes.Count == 1, "repair keeps one tape per destination (no duplicated tape after a bad write)");
Check(Level3ChapterRules.ImpliedLessons(attach2).Contains(Level3ChapterRules.PracticeDoneLesson) &&
      Level3ChapterRules.ImpliedTriggers(Level3Phase.BeaRecords).Contains(Level3ChapterRules.CalculatorTrigger),
    "resuming after practice: the old objective counts as done and no practice lesson replays");
foreach (string answer in new[] { "62500", "600000", "expected" })
    Check(!Snap(attach2).Contains(answer), $"the practice snapshot carries no case answer ('{answer}')");

// ---------- continuing a legacy chapter: candidate -> signed write -> publish; a failed write changes nothing ----------
string sandbox3 = Path.Combine(Path.GetTempPath(), "TaxVentureL3T02-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox3);
try
{
    string payloadOf(PlayerProgressData d) => JsonSerializer.Serialize(d, json);
    string Encode3(PlayerProgressData d) => JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payloadOf(d), ["signature"] = SaveFileStore.Sign(payloadOf(d)) });
    PlayerProgressData Decode3(string text)
    {
        using JsonDocument doc = JsonDocument.Parse(text);
        string payload = doc.RootElement.GetProperty("payload").GetString();
        return SaveFileStore.SignatureMatches(payload, doc.RootElement.GetProperty("signature").GetString()) ? JsonSerializer.Deserialize<PlayerProgressData>(payload, json) : null;
    }
    var store3 = new SaveFileStore(Path.Combine(sandbox3, "progress.json"));
    PlayerProgressData published3 = Clone(live);
    published3.campaign.levelThree = attach1.Copy();
    published3.campaign.levelThree.writes = 4;
    Check(!published3.Normalize() && Level3ChapterRules.WithCheckpoint(Clone(published3), attach1) == null,
        "a version 1 chapter loads without repair, and is never written back as it is");
    store3.Write(Encode3(published3));
    store3.Write(Encode3(published3));
    byte[] onDisk = File.ReadAllBytes(store3.FilePath), backupDisk = File.ReadAllBytes(store3.BackupPath);
    string publishedBefore = Snap(published3);
    // Mirrors Level3Checkpointer.TryContinue -> PlayerProgressManager.CommitLevelThreeCheckpoint -> TryWriteCandidate.
    bool ContinueOnce(ref PlayerProgressData published)
    {
        if (!Level3ChapterRules.TryContinueLegacy(published.campaign.levelThree, out Level3ChapterCheckpoint cont, out _)) return false;
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(published), cont);
        if (next == null) return false;
        try { next.Normalize(); store3.Write(Encode3(next)); }
        catch (Exception) { return false; }
        published = next;
        return true;
    }
    Directory.CreateDirectory(store3.TempPath); // injected failure: the temp file cannot be created
    bool wrote = ContinueOnce(ref published3);
    Check(!wrote && Snap(published3) == publishedBefore && File.ReadAllBytes(store3.FilePath).SequenceEqual(onDisk) &&
          File.ReadAllBytes(store3.BackupPath).SequenceEqual(backupDisk) && published3.campaign.levelThree.version == 1 &&
          published3.campaign.levelThree.phase == "ToolPractice" && published3.campaign.levelThree.tapes.Count == 1 &&
          Level3ChapterRules.Decide(published3.campaign.levelThree) == Level3EntryDecision.Continue,
        "a failed continuation write publishes nothing; both generations and the version 1 chapter stay byte-identical, ready to retry");
    Directory.Delete(store3.TempPath);
    wrote = ContinueOnce(ref published3);
    PlayerProgressData reread = Decode3(File.ReadAllText(store3.FilePath));
    Level3ChapterCheckpoint onDiskChapter = reread?.campaign.levelThree;
    Check(wrote && onDiskChapter != null && onDiskChapter.version == 2 && onDiskChapter.phase == "BeaRecords" && onDiskChapter.writes == 5 &&
          Snap(onDiskChapter.tapes) == Snap(attach1.tapes) && onDiskChapter.milestones.Contains(Level3ChapterRules.PracticeRetiredMilestone) &&
          !onDiskChapter.actionIds.Contains(Level3PracticeRules.CompleteAction) && !onDiskChapter.calculatorTaught && WithoutChapter(reread) == liveWithout,
        "retry: the signed save holds the continued chapter (BeaRecords, tape kept, retired gate only, not taught) with every possession intact");
    Check(Decode3(File.ReadAllText(store3.BackupPath)) is PlayerProgressData prev && prev.campaign.levelThree.version == 1,
        "the backup keeps the version 1 generation");
    string continuedSnap = Snap(published3);
    Check(Level3ChapterRules.Decide(reread.campaign.levelThree) == Level3EntryDecision.Resume && !ContinueOnce(ref published3) && Snap(published3) == continuedSnap,
        "continuing is idempotent: the written chapter resumes and is never continued again");
}
finally
{
    try { Directory.Delete(sandbox3, true); } catch (IOException) { }
}

// ---------- guide, assets and wiring ----------
GuideScript guide3 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
string guide3Report = guide3.Validate();
Check(guide3Report == "" && !guide3.lessons.Any(l => l.pages.Any(p => new[] { "flag:practiceFirstTape", "flag:practiceDone", "flag:toolPractice" }.Any((p.waitFor ?? "").Contains) ||
          (p.TextFor(true) ?? "").Contains("practice budget"))),
    "revision: the guide validates and no lesson waits on the retired practice card (" + guide3Report + ")");
Check(guide3.lessons.All(l => l.pages.All(p => !(p.TextFor(true) ?? "").Contains("click"))), "touch wording only (no 'click')");
string assetText = File.ReadAllText(Res("Levels/Level_03/scene_assets.asset"));
string sceneAssetsGuid = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(Path.Combine(root, "Assets/Scripts/Level3/Level3SceneAssets.cs.meta")), "guid: (\\w+)").Groups[1].Value;
string prefabPath = Path.Combine(root, "Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Calculator_01.prefab");
Check(assetText.Contains("m_Script: {fileID: 11500000, guid: " + sceneAssetsGuid + ", type: 3}") &&
      assetText.Contains("calculatorPrefab: {fileID: 1708030912986460, guid: 350c2278df0895f4bb6f90f92b314913, type: 3}") &&
      File.ReadAllText(prefabPath + ".meta").Contains("guid: 350c2278df0895f4bb6f90f92b314913") &&
      File.ReadAllText(prefabPath).Contains("--- !u!1 &1708030912986460"),
    "scene_assets binds Level3SceneAssets to the supplied Synty SM_Prop_Calculator_01 root (the prefab itself is untouched)");
string deskSrc = Src("Scripts/Level3/Level3DeskCalculator.cs");
string pollBody = deskSrc.Substring(Index(deskSrc, "private void PollPointer()"), Index(deskSrc, "private Collider Pick(") - Index(deskSrc, "private void PollPointer()"));
// The tool itself must be registered as an EventSystem raycaster. An
// independent transparent Graphic can be absent from native hit results.
Check(typeof(UnityEngine.UI.GraphicRaycaster).IsAssignableFrom(typeof(Level3DeskToolHitTarget)),
    "calculator input registers its own rendered-tool raycast path with EventSystem");
string toolEventsRepair = Src("Scripts/Level3/Level3DeskToolHitTarget.cs");
Check(toolEventsRepair.Contains("Owner.ToolAt(e.position) == pressed") && toolEventsRepair.Contains("Owner.ClickTool(pressed)") &&
      toolEventsRepair.Contains("e.eligibleForClick = false") && deskSrc.Contains("if (mode == Mode.Resting) PollPointer();"),
    "focused content uses EventSystem tap/drag ownership; certificate never blocks the calculator");
Check(pollBody.Contains("MobileInputGuard.SuppressWorldTapsThisFrame();") && deskSrc.Contains("[DefaultExecutionOrder(-40)]") &&
      deskSrc.Contains("Level3DeskToolHitTarget.HitBox(ray, target.transform.worldToLocalMatrix") && pollBody.Contains("PointerBlocked(screen)"),
    "a tap on the tools is theirs: HUD buttons win, and the desk behind never reacts to the same tap");
Check(deskSrc.Contains("if (!AtDesk)") && deskSrc.Contains("if (mode == Mode.Lifted || mode == Mode.Lifting) PutAway();") &&
      deskSrc.Contains("FinishAttachFlight();") && deskSrc.Contains("if (!focused) { ReleasePointer(); PutAway(); }") &&
      deskSrc.Contains("if (paused) { ReleasePointer(); PutAway(); }") && deskSrc.Contains("!InGameSettings.IsOpen && !DialogueUI.IsAnyDialogueActive"),
    "Exit Desk, settings, dialogue, pause, focus loss and disable settle the tools and release the pressed key");
int attachAt = Index(deskSrc, "private void Attach(string destinationId)");
string attachBody = deskSrc.Substring(attachAt, Index(deskSrc, "private void Disarm()") - attachAt);
Check(Index(attachBody, "owner.CommitPracticeTape(") < Index(attachBody, "StartAttachFlight(") &&
      Index(attachBody, "case Level3Checkpointer.TapeCommit.Attached:") < Index(attachBody, "StartAttachFlight("),
    "the tape flies to the card only after the chapter command saved it");
string cp3 = Src("Scripts/Level3/Level3Checkpointer.cs");
int commitAt3 = Index(cp3, "public TapeCommit CommitPracticeTape(");
string commitBody3 = cp3.Substring(commitAt3, Index(cp3, "private void Update()") - commitAt3);
Check(Index(commitBody3, "Level3PracticeRules.AttachTape(candidate") < Index(commitBody3, "CommitLevelThreeCheckpoint(candidate)") &&
      Index(commitBody3, "CommitLevelThreeCheckpoint(candidate)") < Index(commitBody3, "Publish(candidate);") &&
      cp3.Contains("if (before == Level3Phase.ToolPractice && Phase == Level3Phase.BeaRecords) GameplayEvents.RaiseCalculatorPracticeAttached();"),
    "attach: candidate, signed write, then publish; the objective ticks only from the saved transition");
Check(Src("Scripts/Level3/Level3OfficeStart.cs").Contains("EnsurePart<Level3DeskCalculator>(Level3DeskCalculator.ChildName);") &&
      Level3OfficeStart.SetupVersion >= 2,
    "the calculator is one owned part under Level3Gameplay (setup v2+, never duplicated)");
foreach (string file in new[] { "Level3Calculator.cs", "Level3PracticeRules.cs", "Level3DeskCalculator.cs", "Level3CalculatorModel.cs", "Level3PracticeCardModel.cs", "Level3DeskProps.cs" })
{
    string code = System.Text.RegularExpressions.Regex.Replace(Src("Scripts/Level3/" + file), @"///.*|//.*", "");
    Check(!code.Contains("ComputerUI") && !code.Contains("TaxComputationCalculator") && !code.Contains("float.Parse") && !code.Contains("double"),
        $"{file}: no route into Level 1's computer/flat-rate prototype, no float money");
    Check(!code.Contains("coins") && !code.Contains("publicTrust") && !code.Contains("CommitPurchase") && !code.Contains("AwardConversation") && !code.Contains("Score."),
        $"{file}: practice touches no wallet, trust, purchase or score");
}
Check(System.Text.RegularExpressions.Regex.Matches(Src("Scripts/Level3/Level3CalculatorModel.cs"), @"props\.(Box|Roll|Print)\(").Count >= 10 &&
      Src("Scripts/Level3/Level3CalculatorModel.cs").Contains("Object.Instantiate(prefab, Root, false)") &&
      Src("Scripts/Level3/Level3PracticeCardModel.cs").Contains("props.Box(\"Clipboard\""),
    "the calculator, tape and card are real 3D bodies (supplied Synty body + boxes/rolls with thickness), not a flat overlay");
Console.WriteLine($"T03 checks: {checks - t03Start}, failures {fails - t03Fails} (calculator/practice rules, save, wiring; Unity rendering and touch not run).");

// =====================================================================
// T04: Bea, her worktable and her original records
// =====================================================================
int t04Start = checks, t04Fails = fails;
Level3TownFile town = JsonSerializer.Deserialize<Level3TownFile>(File.ReadAllText(Res("Levels/Level_03/town_life.json")), json);
List<string> townProblems = Level3TownContent.Validate(town);
Check(townProblems.Count == 0, "town_life.json validates: " + string.Join("; ", townProblems));
Level3ClientContent beaTalk = town.bea;
Check(beaTalk.fullName == "Beatriz Mendoza" && beaTalk.speakerName == "Bea" && beaTalk.anchorPath == "Env/Town/bench (1)",
    "Bea keeps her Level 1 identity and works beside her familiar bench");
// Teaching pass (6 November 2026): her concern names business costs explicitly (the requested Meet Bea beat).
Check(beaTalk.introduction.Any(l => l.speaker == "Npc" && l.text == "I kept every receipt. Can I count them all? I'm unsure what belongs as business costs.") &&
      beaTalk.introduction.Any(l => l.speaker == "Npc" && l.text.Contains("saving for a studio")) &&
      beaTalk.introduction.Any(l => l.speaker == "Player" && l.text == "Let's look at what each record supports."),
    "her first talk keeps CONTENT's concern (kept receipts: can all of them count?) and B-BG's studio goal (T04 buildup wording)");
Check(beaTalk.introduction[0].speaker == "Npc" && beaTalk.introduction[0].text.Contains("I'm Beatriz") && beaTalk.introduction[0].text.Contains("design logos and menus"),
    "she introduces herself fully (a player who never met her in Level 1 still knows who she is)");
var inquiryText = beaTalk.inquiries.ToDictionary(i => i.question, i => i.answer);
Check(inquiryText.Count == 4 && inquiryText["What are you working on?"].StartsWith("Sketches for a new shop sign.") &&
      inquiryText["What was this purchase for?"] == "My birthday dinner and decorations. The studio wasn't involved." &&
      inquiryText["Does this invoice belong to the annual total?"] == "Yes, that supplier bill is in the printing summary." &&
      inquiryText["Show me the period these records cover."] == "These are the records for the whole year shown on the sheets." &&
      beaTalk.leaveChoice == "I'll review the papers first.",
    "the source inquiries and the leave option are CONTENT's exact words (plus the T04 buildup's optional question about her work)");
Check(Level3TownContent.Ordered(beaTalk, "B-PERSONAL")[0].id == "purpose" && Level3TownContent.Ordered(beaTalk, "B-PRINT")[0].id == "invoice" &&
      Level3TownContent.Ordered(beaTalk, null).Select(i => i.id).SequenceEqual(new[] { "purpose", "invoice", "period", "work" }) &&
      Level3TownContent.Ordered(beaTalk, "B-PERSONAL").Count == 4,
    "questions follow the record read last, in any order, and every question stays available");
var townBroken = JsonSerializer.Deserialize<Level3TownFile>(File.ReadAllText(Res("Levels/Level_03/town_life.json")), json);
townBroken.bea.inquiries[1].id = "purpose";
townBroken.bea.introduction.Clear();
Check(Level3TownContent.Validate(townBroken).Count >= 2, "a broken town file is reported (and the built-in Bea is used)");

// ---------- the handover command ----------
Level3ChapterCheckpoint beaRecords = attach2.Copy();
string beaBefore = Snap(beaRecords);
Level3ChapterCheckpoint handed = beaRecords.Copy();
Check(Level3ChapterRules.CollectBeaRecords(handed, out string handReason) == Level3CommandOutcome.Applied && handReason == null &&
      handed.phase == "WorksheetDraft" && handed.activeCaseId == "L3-BEA-001" &&
      handed.Case("L3-BEA-001").acquiredSourceIds.SequenceEqual(Level3ChapterRules.BeaHandoverSources) &&
      handed.milestones.Contains(Level3ChapterRules.BeaRecordsMilestone) && handed.actionIds.Contains(Level3ChapterRules.BeaRecordsAction),
    "completing Bea's introduction hands over her nine papers and admits the worksheet (BeaRecords -> WorksheetDraft) in one candidate");
Check(Snap(beaRecords) == beaBefore && !handed.Case("L3-BEA-001").acquiredSourceIds.Contains("B-WHT") && handed.Case("L3-MARINE-001").acquiredSourceIds.Count == 0,
    "the live chapter is untouched until saved; B-WHT is not handed over; the company case has nothing");
string handedSnap = Snap(handed);
Check(Level3ChapterRules.CollectBeaRecords(handed, out _) == Level3CommandOutcome.Duplicate && Snap(handed) == handedSnap,
    "a second completed talk (or a restore) is a quiet duplicate: no second acquisition");
foreach (var (what, state) in new[] { ("from a version 1 practice chapter not yet continued", attach1.Copy()), ("before the reception", entry.Copy()) })
{
    string before = Snap(state);
    Check(Level3ChapterRules.CollectBeaRecords(state, out _) == Level3CommandOutcome.Refused && Snap(state) == before,
        $"no handover {what} (records open only after the assignment, on a revised chapter)");
}
Level3ChapterCheckpoint earlyCredit = handed.Copy();
earlyCredit.Case("L3-BEA-001").acquiredSourceIds.Add("B-WHT");
Check(Level3ChapterRules.Decide(earlyCredit) == Level3EntryDecision.Blocked, "a snapshot holding B-WHT before the credit step is refused (never secretly viewable)");
PlayerProgressData handedSave = Level3ChapterRules.WithCheckpoint(Clone(live), handed);
Check(WithoutChapter(handedSave) == liveWithout && handedSave.campaign.coins == live.campaign.coins && handedSave.campaign.publicTrust == live.campaign.publicTrust,
    "the handover gives no coins, trust or XP (record acquisition is not the side quest's reward)");
PlayerProgressData handedBack = JsonSerializer.Deserialize<PlayerProgressData>(Snap(handedSave), json);
Check(!handedBack.Normalize() && handedBack.campaign.levelThree.Case("L3-BEA-001").acquiredSourceIds.SequenceEqual(Level3ChapterRules.BeaHandoverSources) &&
      Level3ChapterRules.Decide(handedBack.campaign.levelThree) == Level3EntryDecision.Resume,
    "the handed-over sources round-trip and resume at WorksheetDraft");
Check(Level3ChapterRules.ImpliedTriggers(Level3Phase.WorksheetDraft).Contains(Level3ChapterRules.RecordsTrigger) &&
      Level3ChapterRules.ImpliedLessons(Level3Phase.WorksheetDraft).Contains(Level3ChapterRules.BeaRecordsLesson) &&
      !Level3ChapterRules.ImpliedTriggers(Level3Phase.BeaRecords).Contains(Level3ChapterRules.RecordsTrigger),
    "resuming after the handover: the records objective is done and its lesson does not replay; before it, it still runs");
Check(Level3ChapterRules.CoarseState(Level3Phase.WorksheetDraft) == "ReceiveCaseState", "the coarse game state stays ReceiveCaseState through the handover");

// ---------- the papers as the fixed physical types print them ----------
List<ClientRecord> papers = Level3RecordRules.Build(bea, Level3ChapterRules.BeaHandoverSources);
var bySource = papers.ToDictionary(p => p.source);
Check(papers.Count == 9 && papers.Select(p => p.source).OrderBy(s => s).SequenceEqual(Level3ChapterRules.BeaHandoverSources.OrderBy(s => s)),
    "the nine acquired sources print as nine papers, one each");
Check(bySource["B-ID"].type == ClientRecord.Identity && bySource["B-BG"].type == ClientRecord.Background &&
      bySource["B-REV"].type == ClientRecord.Income && bySource["B-RENT"].type == ClientRecord.Agreement &&
      new[] { "B-SUB", "B-PRINT", "B-PERSONAL" }.All(s => bySource[s].type == ClientRecord.Payment) &&
      new[] { "B-EXP", "B-DRAFT" }.All(s => bySource[s].type == ClientRecord.Plain),
    "fixed types: ID card (1), background notes (2), income (4), payments (5), agreement (7), plain working papers (0)");
Check(papers[0].type == ClientRecord.Identity && papers[1].type == ClientRecord.Background && papers.Skip(2).TakeWhile(p => !p.IsSlip).All(p => !p.IsSlip),
    "reading order: ID, background, sheets, then slips");
Check(papers.All(p => p.id == "L3-BEA-001/" + p.source && p.npcId == "L3-BEA-001"),
    "each paper's identity is case/source (the same id the Bag's Case Folder lists)");
Check(bySource["B-ID"].name == "Beatriz Mendoza" && bySource["B-ID"].reference == "312-845-679-000" && bySource["B-ID"].title == "TIN ID",
    "the ID card prints her name and the supplied fictional TIN");
Check(bySource["B-BG"].period == "Calendar year 2026" && bySource["B-BG"].work == "Freelance illustrator",
    "the background notes print the annual period and her work");
foreach (string s in new[] { "B-REV", "B-EXP", "B-RENT", "B-SUB", "B-PRINT", "B-DRAFT" })
    Check(bySource[s].date == "2026-01-01 to 2026-12-31", $"{s} prints its annual period");
Check(bySource["B-PERSONAL"].date == "2026-06-15" && bySource["B-PERSONAL"].issuer == "Island Party Shop", "the party receipt prints its own date and issuer");
string AllRows(ClientRecord r) => string.Join(" | ", r.rows.Concat(r.backRows).Select(x => x.label + ": " + x.value)) + " | " + r.totalLabel + ": " + r.totalValue;
Check(bySource["B-REV"].totalValue == "₱900,000.00" && bySource["B-EXP"].totalValue == "₱300,000.00" && bySource["B-DRAFT"].totalValue == "₱354,000.00",
    "readable totals print in their bands: revenue 900,000, cost schedule 300,000, rough claims 354,000");
Check(AllRows(bySource["B-EXP"]).Contains("PRINT-26") && AllRows(bySource["B-PRINT"]).Contains("PRINT-26") && AllRows(bySource["B-DRAFT"]).Contains("PRINT-26"),
    "the printing bill's shared reference PRINT-26 shows on the cost schedule, the supplier record and the rough claims (the duplicate is findable)");
Check(AllRows(bySource["B-PERSONAL"]).Contains("private birthday", StringComparison.OrdinalIgnoreCase) &&
      AllRows(bySource["B-RENT"]).Contains("business-only", StringComparison.OrdinalIgnoreCase),
    "purpose/status facts are printed (every graded fact is on paper, not only in dialogue)");
Check(!papers.Any(p => AllRows(p).Contains("600,000") || AllRows(p).Contains("62,500") || AllRows(p).Contains("50,000.00")),
    "no paper prints a computation answer");
// T08 changed this pin: B-WHT now prints as its Type 8 certificate, and only once acquired (never on her table: see the table pin below).
Check(Level3RecordRules.Build(bea, new List<string> { "B-WHT" }).Select(r => r.type).SequenceEqual(new[] { ClientRecord.Withholding }) &&
      Level3RecordRules.Build(bea, new List<string>()).Count == 0,
    "B-WHT prints only as its Type 8 certificate, and nothing shows before the handover");
Check(Level3RecordRules.Build(marine, Level3ChapterRules.BeaHandoverSources).Count == 0, "Bea's ids never open the company's papers");

// ---------- a failed handover write (disposable store) ----------
string sandbox4 = Path.Combine(Path.GetTempPath(), "TaxVentureL3T04-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox4);
try
{
    string Encode4(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var store4 = new SaveFileStore(Path.Combine(sandbox4, "progress.json"));
    PlayerProgressData published4 = Level3ChapterRules.WithCheckpoint(Clone(live), beaRecords);
    store4.Write(Encode4(published4));
    byte[] disk4 = File.ReadAllBytes(store4.FilePath);
    string before4 = Snap(published4);
    Directory.CreateDirectory(store4.TempPath);
    bool wrote4 = false;
    try { PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(published4), handed); next.Normalize(); store4.Write(Encode4(next)); published4 = next; wrote4 = true; }
    catch (Exception) { }
    Check(!wrote4 && Snap(published4) == before4 && File.ReadAllBytes(store4.FilePath).SequenceEqual(disk4) &&
          published4.campaign.levelThree.phase == "BeaRecords" && published4.campaign.levelThree.Case("L3-BEA-001").acquiredSourceIds.Count == 0,
        "a failed handover write keeps BeaRecords and no sources: Bea still holds her records and the next talk offers them again");
    Directory.Delete(store4.TempPath);
}
finally
{
    try { Directory.Delete(sandbox4, true); } catch (IOException) { }
}

// ---------- config, assets and wiring ----------
var objectiveSteps = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(objectiveSteps.Count >= 2 && objectiveSteps[1].GetProperty("trigger").GetString() == Level3ChapterRules.RecordsTrigger &&
      ObjectiveTriggers.Validate(Level3ChapterRules.RecordsTrigger) == null,
    "objective 2 (revision; was 3): meet Bea and collect her records (a known event raised only by the saved handover)");
GuideScript guide4 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
Check(guide4.Validate() == "" && guide4.lessons.Any(l => l.id == Level3ChapterRules.BeaRecordsLesson && l.when == "flag:beaIntroDone" && l.onlyIn == "mode:FirstPerson" && l.pages.All(p => p.IsHint)),
    "Arthur's records hint waits for Bea's organizer offer and a standing player (T04 buildup: never over her) and never blocks");
Check(guide4.waypoints.Length == 1 && guide4.waypoints[0].target == "Level3Gameplay/BeaTable/Bea" && guide4.waypoints[0].step == Level3ChapterRules.RecordsTrigger &&
      guide4.waypoints.All(w => w.target.StartsWith("Level3Gameplay/")),
    "the only arrow leads to Bea's new table; the known office has none");
string assets4 = File.ReadAllText(Res("Levels/Level_03/scene_assets.asset"));
string beaPrefabPath = Path.Combine(root, "Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Developer_Female_01.prefab");
string tablePrefabPath = Path.Combine(root, "Assets/Palmov Island/Low Poly Environment Park/Prefabs/Environment/wooden table.prefab");
Check(assets4.Contains("beaPrefab: {fileID: 1464604485386400, guid: 1a96e91537e0d7f48809b9484dff7f3e, type: 3}") &&
      File.ReadAllText(beaPrefabPath + ".meta").Contains("1a96e91537e0d7f48809b9484dff7f3e") && File.ReadAllText(beaPrefabPath).Contains("--- !u!1 &1464604485386400") &&
      assets4.Contains("worktablePrefab: {fileID: 5507815067105631450, guid: ee79087d0378f68499e00c8f9bb31663, type: 3}") &&
      File.ReadAllText(tablePrefabPath + ".meta").Contains("ee79087d0378f68499e00c8f9bb31663") && File.ReadAllText(tablePrefabPath).Contains("--- !u!1 &5507815067105631450") &&
      assets4.Contains("npcController: {fileID: 9100000, guid: 2e8c12097e1e471a86e5e5d07745decd, type: 2}") &&
      File.ReadAllText(Path.Combine(root, "Assets/AssetIntegration/NpcHumanoid.controller.meta")).Contains("2e8c12097e1e471a86e5e5d07745decd"),
    "scene_assets binds Bea's Level 1 model, the park wooden table and the shared NPC controller (sources untouched)");
string beaSrc = Src("Scripts/Level3/Level3BeaInteractable.cs");
Check(beaSrc.Contains("else if (phase == Level3Phase.BeaRecords) Talk(table.IntroHeard ? EnvelopeLine(bea) : Introduction(bea), HandOver);") &&
      beaSrc.Contains("() => { End(); completed?.Invoke(); },") && beaSrc.Contains("            End,\n            this);"),
    "the handover runs only when the introduction completes; cancelling only ends the talk");
int handAt = Index(beaSrc, "private void HandOver()");
string handBody = beaSrc.Substring(handAt, Index(beaSrc, "// QUESTIONS") - handAt);
Check(Index(handBody, "owner.CommitBeaRecords(") < Index(handBody, "table.Present()") &&
      Index(handBody, "case Level3Checkpointer.TapeCommit.Attached:") < Index(handBody, "table.Present()"),
    "the envelope is presented only after the handover saved (T04 buildup; was: the papers fan out)");
Check(!System.Text.RegularExpressions.Regex.IsMatch(beaSrc, @"void Start\([^)]") && !System.Text.RegularExpressions.Regex.IsMatch(Src("Scripts/Level3/Level3BeaTable.cs"), @"void Start\([^)]"),
    "no Unity message name is reused with parameters");
Check(beaSrc.Contains("dialogue.DockChoices(choicePanel)") && beaSrc.Contains("dialogue.CancelDialogueFor(this)") && beaSrc.Contains("ContinueWith(InquiryAction, lines)"),
    "questions use the existing dialogue session (docked choices, Talk later, continue-with); no new dialogue framework");
string cp4 = Src("Scripts/Level3/Level3Checkpointer.cs");
int c4 = Index(cp4, "public TapeCommit CommitBeaRecords(");
string c4Body = cp4.Substring(c4, Index(cp4, "/// <summary>What attaching a practice tape did") - c4);
Check(Index(c4Body, "Level3ChapterRules.CollectBeaRecords(candidate") < Index(c4Body, "CommitLevelThreeCheckpoint(candidate)") &&
      Index(c4Body, "CommitLevelThreeCheckpoint(candidate)") < Index(c4Body, "Publish(candidate);") &&
      cp4.Contains("if (before == Level3Phase.BeaRecords && Phase == Level3Phase.WorksheetDraft) GameplayEvents.RaiseClientRecordsCollected();"),
    "handover: candidate, signed write, publish; the objective event comes only from the saved transition");
string table4 = Src("Scripts/Level3/Level3BeaTable.cs");
// Revised T04: one compact stack (no fan, no board) of the same existing physical records.
Check(table4.Contains("ClientRecordPaper.Create(this, slot, workspace, record, new Vector2(w, w * aspect), gameObject.layer)") && table4.Contains("Level3RecordRules.Build(definition, onTable)") &&
      table4.Contains("return Level3TableStaging.TableSources(acquired);") &&
      table4.Contains("SetPapersInteractive(Seated);") && table4.Contains("CameraController.WorkstationExited += HandleLeftSeat;") &&
      table4.Contains("CameraController.WorkstationExited -= HandleLeftSeat;") && table4.Contains("if (paper != null && !paper.IsAttached) paper.Gather();"),
    "the papers are the existing physical records, take touches only while seated, and gather when the player leaves");
Check(table4.Contains("holder.AddComponent<NpcLife>().ConfigureRuntime(NpcLife.Activity.Stand") && table4.Contains("Physics.CheckBox(") &&
      Src("Scripts/Level3/Level3OfficeStart.cs").Contains("EnsurePart<Level3BeaTable>(Level3BeaTable.ChildName);") &&
      Src("Scripts/Level3/Level3OfficeStart.cs").Contains("public const int SetupVersion = 3;"),
    "one Bea owned by NpcLife, placed on a measured free spot; one BeaTable part (setup v3)");
string hotbar4 = Src("Scripts/UI/HUD/HudHotbar.cs");
Check(hotbar4.Contains("private bool HoldsCasePapers() => LevelThree ? InventoryRules.Owns(Inventory, InventoryCatalogue.CaseFolder) : FolderCarried();") &&
      hotbar4.Contains("if (HoldsCasePapers() && definition.documents != null)"),
    "Level 3's carried Case Folder lists and keeps readable pages of the handed-over sources (Levels 1/2 unchanged)");
Console.WriteLine($"T04 checks: {checks - t04Start}, failures {fails - t04Fails} (encounter content, handover, record bindings, wiring; Unity rendering, NPC motion and touch not run).");

// =====================================================================
// T05: Bea's worksheet: source-linked strips, four sections, claim notes
// =====================================================================
int t05Start = checks, t05Fails = fails;
List<Level3WorksheetRow> wsRows = Level3WorksheetRules.RowsOf(bea);
var wsRow = wsRows.ToDictionary(wsR => wsR.RowId);
Check(wsRows.Select(wsR => wsR.RowId).SequenceEqual(new[] { "R1", "R2", "R3", "R4", "R5", "R6", "R7" }) &&
      wsRow["R1"].SourceId == "B-REV" && wsRow["R2"].SourceId == "B-REV" && wsRow["R3"].SourceId == "B-EXP" && wsRow["R4"].SourceId == "B-EXP" &&
      wsRow["R5"].SourceId == "B-EXP" && wsRow["R6"].SourceId == "B-PERSONAL" && wsRow["R7"].SourceId == "B-PRINT",
    "the worksheet offers exactly R1-R7, each from its own record");
Check(wsRow["R5"].TransactionId == "PRINT-26" && wsRow["R7"].TransactionId == "PRINT-26" && wsRow["R5"].SourceId != wsRow["R7"].SourceId &&
      wsRow["R5"].Amount == "30000.00" && wsRow["R7"].Amount == "30000.00" && wsRow["R5"].Label == "Printing and supplies" && wsRow["R7"].Label == "Printing bill, paid",
    "R5 (annual printing on the cost schedule) and R7 (the supplier bill) are distinct rows sharing PRINT-26, so double counting stays possible");
Check(wsRows.All(wsR => !string.IsNullOrEmpty(wsR.Label) && wsR.Amount.Contains('.')) && Level3WorksheetRules.RowsOf(marine).Count == 0 && Level3WorksheetRules.RowsOf(null).Count == 0,
    "labels/amounts come from the printed content; the company case has no Bea worksheet");
Check(Level3WorksheetRules.ClaimOf(bea, "note-R6").Label == "Party purchases" && Level3WorksheetRules.ClaimOf(bea, "note-R7").Label == "Printing bill" &&
      Level3WorksheetRules.ClaimOf(bea, "note-R6").Amount == "24000.00" && Level3WorksheetRules.ClaimOf(bea, "note-X") == null,
    "the two claim notes quote Bea's draft claims (B-DRAFT's printed label and amount)");
Check(Level3WorksheetRules.SourceTabs.SequenceEqual(new[] { "B-REV", "B-EXP", "B-PERSONAL", "B-PRINT" }) &&
      Level3WorksheetRules.SourceTabs.All(s => Level3ChapterRules.BeaHandoverSources.Contains(s)),
    "the record tabs are the four papers the rows come from (all handed over)");

// ---------- extraction: wsOnce per wsRw, from its own acquired record ----------
Level3ChapterCheckpoint ws = handed.Copy();
string handedLive = Snap(handed);
foreach (Level3WorksheetRow wsRw in wsRows)
    Check(Level3WorksheetRules.Extract(ws, wsRows, wsRw.RowId, out string wsWhy) == Level3CommandOutcome.Applied && wsWhy == null, $"{wsRw.RowId} lifts as a strip");
Check(ws.worksheet.strips.Count == 7 && ws.worksheet.strips.All(s => s.sourceId == wsRow[s.rowId].SourceId && s.amount == wsRow[s.rowId].Amount && s.section == ""),
    "each strip keeps its row id, source id and printed amount, and starts unplaced");
Check(Snap(handed) == handedLive, "commands change only the candidate copy (the live chapter waits for the save)");
foreach (Level3WorksheetRow wsRw in wsRows)
    Check(Level3WorksheetRules.Extract(ws, wsRows, wsRw.RowId, out string wsDup) == Level3CommandOutcome.Duplicate && wsDup == "already on the worksheet", $"{wsRw.RowId} cannot be lifted twice");
Check(ws.worksheet.strips.Count == 7 && ws.worksheet.revision == 7 && ws.worksheet.selectedRow == "R7",
    "re-extraction makes no copy (it only focuses the existing strip)");
Check(Level3ChapterRules.Problem(ws) == null && ws.phase == "WorksheetDraft", "drafting keeps the chapter usable and in WorksheetDraft");
foreach (string wsForeign in new[] { "B-RENT-PROOF", "B-CLAIM-PRINT", "B-WHT-CREDIT", "R8", "", null })
{
    Level3ChapterCheckpoint wsF = handed.Copy();
    Check(Level3WorksheetRules.Extract(wsF, wsRows, wsForeign, out string wsR) == Level3CommandOutcome.Refused && !string.IsNullOrEmpty(wsR) && wsF.worksheet.strips.Count == 0,
        $"'{wsForeign}' is not a worksheet row: explicit refusal, nothing lifted");
}
Check(Level3WorksheetRules.Extract(handed.Copy(), Level3WorksheetRules.RowsOf(marine), "R1", out string foreignCase) == Level3CommandOutcome.Refused && foreignCase != null,
    "a row from another case's content is refused");
Level3ChapterCheckpoint noPrint = handed.Copy();
noPrint.Case("L3-BEA-001").acquiredSourceIds.Remove("B-PRINT");
Check(Level3WorksheetRules.Extract(noPrint, wsRows, "R7", out string notHeld) == Level3CommandOutcome.Refused && notHeld == "That paper is not in Bea's records.",
    "a strip can come only from a record actually handed over");
foreach (var (wsWhat, wsState) in new[] { ("before the handover", attach2.Copy()), ("at reception", entry.Copy()) })
    Check(Level3WorksheetRules.Extract(wsState, wsRows, "R1", out _) == Level3CommandOutcome.Refused && (wsState.worksheet == null || wsState.worksheet.strips.Count == 0),
        $"no worksheet edits {wsWhat}");

// ---------- placement: every wsSection accepts every strip, neutrally ----------
foreach (Level3WorksheetRow wsRw in wsRows)
    foreach (string wsSection in Level3WorksheetRules.Sections)
    {
        Level3ChapterCheckpoint wsP = ws.Copy();
        Check(Level3WorksheetRules.Place(wsP, wsRw.RowId, wsSection, out string wsR) == Level3CommandOutcome.Applied && wsR == null && wsP.worksheet.Strip(wsRw.RowId).section == wsSection,
            $"{wsRw.RowId} -> {wsSection} is accepted with the same neutral outcome");
    }
foreach (string wsBad in new[] { "Income", "income ", "Allowable expenses", "deductible", "", null })
{
    Level3ChapterCheckpoint wsP = ws.Copy();
    Check(Level3WorksheetRules.Place(wsP, "R1", wsBad, out string wsR) == Level3CommandOutcome.Refused && wsR == "That is not a worksheet section." && wsP.worksheet.Strip("R1").section == "",
        $"'{wsBad}' is not a section: refused, nothing moves");
}
Check(Level3WorksheetRules.Place(handed.Copy(), "R1", "income", out string unlifted) == Level3CommandOutcome.Refused && unlifted == "Lift that row from its record first.",
    "a row must be lifted before it is placed");
Level3ChapterCheckpoint wsDirty = ws.Copy();
Level3WorksheetRules.Place(wsDirty, "R6", "not-deductible", out _);
Check(!wsDirty.worksheet.incomeDirty && !wsDirty.worksheet.expensesDirty, "placing outside Income/Allowable expenses leaves both subtotals clean");
Level3WorksheetRules.Place(wsDirty, "R1", "income", out _);
Check(wsDirty.worksheet.incomeDirty && !wsDirty.worksheet.expensesDirty, "a change in Income marks only its subtotal for a new tape");
Level3WorksheetRules.Place(wsDirty, "R1", "expenses", out _);
Check(wsDirty.worksheet.incomeDirty && wsDirty.worksheet.expensesDirty, "moving between Income and Allowable expenses marks both");
Check(Level3WorksheetRules.Place(wsDirty, "R1", "expenses", out string wsSame) == Level3CommandOutcome.Duplicate && wsSame == "already there", "placing into its own section is a quiet duplicate");

// ---------- claim notes: reachable on every destination, neutral candidates ----------
Check(Level3WorksheetRules.Reasons.Select(Level3WorksheetRules.ReasonLabel).SequenceEqual(new[] { "Business purpose", "Personal purpose", "Already represented", "Different reporting period", "Need supporting record" }),
    "the five reason labels are CONTENT's neutral alternatives, offered on both notes");
Check(Level3WorksheetRules.RelatedCandidates("note-R7").SequenceEqual(new[] { "R1", "R2", "R3", "R4", "R5", "R6" }) && Level3WorksheetRules.RelatedCandidates("note-R6").Count == 0,
    "the printing note may link any other worksheet row (R5 is one of six equal choices); the party note asks for a reason only");
foreach (string wsSection in Level3WorksheetRules.Sections.Append(""))
    foreach (string wsReason in Level3WorksheetRules.Reasons)
    {
        Level3ChapterCheckpoint wsN = ws.Copy();
        string where = wsSection == "" ? "no section" : wsSection;
        if (wsSection != "") { Level3WorksheetRules.Place(wsN, "R6", wsSection, out _); Level3WorksheetRules.Place(wsN, "R7", wsSection, out _); }
        Check(Level3WorksheetRules.SetReason(wsN, "note-R6", wsReason, out _) == Level3CommandOutcome.Applied &&
              Level3WorksheetRules.SetReason(wsN, "note-R7", wsReason, out _) == Level3CommandOutcome.Applied &&
              Level3WorksheetRules.SetRelated(wsN, "note-R7", "R3", out _) == Level3CommandOutcome.Applied,
            $"with R6/R7 in '{where}', both notes still take '{wsReason}' and the link");
    }
Check(Level3WorksheetRules.SetReason(handed.Copy(), "note-R6", "personal-purpose", out _) == Level3CommandOutcome.Applied,
    "a note can be answered before its strip is even lifted (fields never hide)");
Level3ChapterCheckpoint badNote = ws.Copy();
Check(Level3WorksheetRules.SetReason(badNote, "note-R5", "personal-purpose", out _) == Level3CommandOutcome.Refused &&
      Level3WorksheetRules.SetReason(badNote, "note-R6", "Personal purpose", out _) == Level3CommandOutcome.Refused &&
      Level3WorksheetRules.SetRelated(badNote, "note-R7", "R7", out _) == Level3CommandOutcome.Refused &&
      Level3WorksheetRules.SetRelated(badNote, "note-R7", "B-RENT-PROOF", out _) == Level3CommandOutcome.Refused &&
      Level3WorksheetRules.SetRelated(badNote, "note-R6", "R1", out _) == Level3CommandOutcome.Refused && badNote.worksheet.notes.Count == 0,
    "unknown notes, labels typed as keys, self-links, non-rows and a link on the party note are refused");

// ---------- no correctness leak ----------
Level3ChapterCheckpoint wsWrong = ws.Copy(), wsRight = ws.Copy();
string[] wrongPlan = { "expenses", "expenses", "income", "not-deductible", "supporting", "expenses", "expenses" };
string[] rightPlan = { "income", "income", "expenses", "expenses", "expenses", "not-deductible", "supporting" };
var wrongOutcomes = new List<string>();
var rightOutcomes = new List<string>();
for (int i = 0; i < 7; i++)
{
    wrongOutcomes.Add(Level3WorksheetRules.Place(wsWrong, "R" + (i + 1), wrongPlan[i], out string wr) + "/" + wr);
    rightOutcomes.Add(Level3WorksheetRules.Place(wsRight, "R" + (i + 1), rightPlan[i], out string rr) + "/" + rr);
}
wrongOutcomes.Add(Level3WorksheetRules.SetReason(wsWrong, "note-R6", "business-purpose", out string w1) + "/" + w1);
rightOutcomes.Add(Level3WorksheetRules.SetReason(wsRight, "note-R6", "personal-purpose", out string r1) + "/" + r1);
wrongOutcomes.Add(Level3WorksheetRules.SetReason(wsWrong, "note-R7", "different-period", out string w2) + "/" + w2);
rightOutcomes.Add(Level3WorksheetRules.SetReason(wsRight, "note-R7", "already-represented", out string r2) + "/" + r2);
wrongOutcomes.Add(Level3WorksheetRules.SetRelated(wsWrong, "note-R7", "R2", out string w3) + "/" + w3);
rightOutcomes.Add(Level3WorksheetRules.SetRelated(wsRight, "note-R7", "R5", out string r3) + "/" + r3);
Check(wrongOutcomes.SequenceEqual(rightOutcomes), "a wrong draft and the right draft get identical outcomes and messages, edit by edit");
Check(wsWrong.worksheet.drafted && wsRight.worksheet.drafted && Level3WorksheetRules.AllFilledIn(wsWrong.worksheet) && Level3WorksheetRules.AllFilledIn(wsRight.worksheet),
    "'filled in' is structural: a wrong draft counts the same as a right one (grading is T06's Check)");
string wrongSnap = Snap(wsWrong.worksheet), rightSnap = Snap(wsRight.worksheet);
foreach (string leak in new[] { "expected", "correct", "dest-", "Allowable expenses", "Not deductible", "Personal purpose", "Already represented", "R5/B-EXP" })
    Check(!wrongSnap.Contains(leak) && !rightSnap.Contains(leak), $"the saved draft holds only the player's own keys (no '{leak}')");
Check(wsWrong.worksheet.revision == wsRight.worksheet.revision && wsWrong.worksheet.drafted == wsRight.worksheet.drafted,
    "revision and the filled-in flag move the same way for right and wrong drafts");
string wsModelSrc = Src("Scripts/Level3/Level3WorksheetModel.cs"), wsDeskSrc = Src("Scripts/Level3/Level3WorksheetDesk.cs"), wsRulesSrc = Src("Scripts/Level3/Level3WorksheetRules.cs");
Check(!wsModelSrc.Contains("worksheetDecisions") && !wsDeskSrc.Contains("worksheetDecisions") && !wsRulesSrc.Contains("worksheetDecisions") &&
      !wsModelSrc.Contains("expectedFields") && !wsDeskSrc.Contains("expectedFields") && !wsModelSrc.Contains("HudTheme.Complete") && !wsDeskSrc.Contains("HudTheme.Complete"),
    "the worksheet code never reads the answer key and uses no success colour");

// ---------- undo ----------
Level3ChapterCheckpoint wsU = handed.Copy();
Level3WorksheetRules.Extract(wsU, wsRows, "R5", out _);
Level3WorksheetRules.Place(wsU, "R5", "income", out _);
Level3WorksheetRules.Place(wsU, "R5", "expenses", out _);
Level3WorksheetRules.SetReason(wsU, "note-R7", "need-support", out _);
Level3WorksheetRules.SetReason(wsU, "note-R7", "already-represented", out _);
Check(Level3WorksheetRules.Undo(wsU, out _) == Level3CommandOutcome.Applied && wsU.worksheet.Note("note-R7").reason == "need-support", "Undo puts the previous reason back");
Check(Level3WorksheetRules.Undo(wsU, out _) == Level3CommandOutcome.Applied && wsU.worksheet.Note("note-R7").reason == "", "Undo clears a first reason");
Check(Level3WorksheetRules.Undo(wsU, out _) == Level3CommandOutcome.Applied && wsU.worksheet.Strip("R5").section == "income", "Undo moves the strip back to its previous section");
Check(Level3WorksheetRules.Undo(wsU, out _) == Level3CommandOutcome.Applied && wsU.worksheet.Strip("R5").section == "", "Undo puts a first placement back on the pile");
Check(Level3WorksheetRules.Undo(wsU, out _) == Level3CommandOutcome.Applied && wsU.worksheet.strips.Count == 0 && wsU.worksheet.selectedRow == "R5",
    "Undo returns a lifted strip to its record (the original was never changed)");
Check(Level3WorksheetRules.Undo(wsU, out string wsNothing) == Level3CommandOutcome.Refused && wsNothing == "Nothing to undo." && wsU.worksheet.revision == 10,
    "with nothing left, Undo says so; every real edit and undo counted a revision");
Level3ChapterCheckpoint wsMany = ws.Copy();
for (int i = 0; i < 40; i++) Level3WorksheetRules.Place(wsMany, "R1", Level3WorksheetRules.Sections[i % 4], out _);
Check(wsMany.worksheet.undo.Count == Level3WorksheetDraft.MaxUndo, "the undo history is bounded (24 steps)");
Level3ChapterCheckpoint wsOnce = wsRight.Copy();
Level3WorksheetRules.Undo(wsOnce, out _);
Check(wsOnce.worksheet.drafted && !Level3WorksheetRules.AllFilledIn(wsOnce.worksheet), "'drafted' is raised once and stays (undo cannot replay the objective)");

// ---------- phases ----------
Level3ChapterCheckpoint wsCorrections = ws.Copy();
wsCorrections.phase = "WorksheetCorrections";
Check(Level3WorksheetRules.Editable(wsCorrections) && Level3WorksheetRules.Place(wsCorrections, "R1", "income", out _) == Level3CommandOutcome.Applied,
    "the same draft stays editable while correcting after T06's check");
Level3ChapterCheckpoint wsReady = ws.Copy();
wsReady.phase = "WorksheetReady";
Check(!Level3WorksheetRules.Editable(wsReady) && Level3WorksheetRules.Place(wsReady, "R1", "income", out string notNow) == Level3CommandOutcome.Refused && notNow != null,
    "a worksheet that is ready (or later) cannot be changed");

// ---------- save round trip, repair, resume ----------
PlayerProgressData wsSave = Level3ChapterRules.WithCheckpoint(Clone(live), wsWrong);
PlayerProgressData wsBack = JsonSerializer.Deserialize<PlayerProgressData>(Snap(wsSave), json);
Check(!wsBack.Normalize() && Snap(wsBack.campaign.levelThree.worksheet) == Snap(wsWrong.worksheet) && Level3ChapterRules.Decide(wsBack.campaign.levelThree) == Level3EntryDecision.Resume,
    "a draft (placements, notes, undo, selection, flags) round-trips through the signed save and resumes");
Check(WithoutChapter(wsSave) == liveWithout && wsSave.campaign.coins == live.campaign.coins && wsSave.campaign.publicTrust == live.campaign.publicTrust,
    "drafting gives no coins, trust, XP or mastery");
var wsBroken = new Level3WorksheetDraft { strips = null, notes = null, undo = null, selectedRow = null, revision = -3 };
Check(wsBroken.Repair() && wsBroken.strips != null && wsBroken.notes != null && wsBroken.undo != null && wsBroken.selectedRow == "" && wsBroken.revision == 0, "a damaged draft is repaired to empty lists");
var wsTwice = new Level3WorksheetDraft();
wsTwice.strips.Add(new Level3StripRecord { rowId = "R1", sourceId = "B-REV", amount = "500000.00", section = "income" });
wsTwice.strips.Add(new Level3StripRecord { rowId = "R1", sourceId = "B-REV", amount = "500000.00", section = "expenses" });
wsTwice.strips.Add(null);
Check(wsTwice.Repair() && wsTwice.strips.Count == 1 && wsTwice.strips[0].section == "income" && !wsTwice.Repair(), "duplicate or empty strips are dropped (the first kept); a clean draft repairs nothing");
PlayerProgressData oldSave = Level3ChapterRules.WithCheckpoint(Clone(live), handed);
JsonNode oldNode = JsonNode.Parse(Snap(oldSave));
oldNode["campaign"]["levelThree"].AsObject().Remove("worksheet");
PlayerProgressData oldBack = JsonSerializer.Deserialize<PlayerProgressData>(oldNode.ToJsonString(), json);
oldBack.Normalize();
Check(oldBack.campaign.levelThree.worksheet != null && oldBack.campaign.levelThree.worksheet.strips.Count == 0 && Level3ChapterRules.Decide(oldBack.campaign.levelThree) == Level3EntryDecision.Resume,
    "a T04 save without a worksheet still resumes (an empty draft)");
Check(Level3ChapterRules.ImpliedTriggers(wsWrong).Contains(Level3ChapterRules.WorksheetTrigger) && !Level3ChapterRules.ImpliedTriggers(ws).Contains(Level3ChapterRules.WorksheetTrigger) &&
      Level3ChapterRules.ImpliedLessons(ws).Contains(Level3ChapterRules.WorksheetLesson) && !Level3ChapterRules.ImpliedLessons(handed).Contains(Level3ChapterRules.WorksheetLesson) &&
      Level3ChapterRules.ImpliedLessons(wsWrong).Contains(Level3ChapterRules.WorksheetDraftedLesson) && !Level3ChapterRules.ImpliedLessons(ws).Contains(Level3ChapterRules.WorksheetDraftedLesson),
    "resume: a filled-in draft keeps its objective ticked; started/filled drafts do not replay their lessons; a fresh one still teaches");

// ---------- a failed draft write (disposable store) ----------
string sandbox5 = Path.Combine(Path.GetTempPath(), "TaxVentureL3T05-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox5);
try
{
    string Encode5(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var store5 = new SaveFileStore(Path.Combine(sandbox5, "progress.json"));
    PlayerProgressData published5 = Level3ChapterRules.WithCheckpoint(Clone(live), ws);
    store5.Write(Encode5(published5));
    byte[] disk5 = File.ReadAllBytes(store5.FilePath);
    string before5 = Snap(published5);
    Level3ChapterCheckpoint candidate5 = published5.campaign.levelThree.Copy();
    Level3WorksheetRules.Place(candidate5, "R1", "income", out _);
    Directory.CreateDirectory(store5.TempPath);
    bool wrote5 = false;
    try { PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(published5), candidate5); next.Normalize(); store5.Write(Encode5(next)); published5 = next; wrote5 = true; }
    catch (Exception) { }
    Check(!wrote5 && Snap(published5) == before5 && File.ReadAllBytes(store5.FilePath).SequenceEqual(disk5) && published5.campaign.levelThree.worksheet.Strip("R1").section == "",
        "a failed draft write keeps the last saved placement on disk and live (the edit can be repeated)");
    Directory.Delete(store5.TempPath);
}
finally
{
    try { Directory.Delete(sandbox5, true); } catch (IOException) { }
}

// ---------- config and wiring ----------
var steps5 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps5.Count >= 3 && steps5[2].GetProperty("trigger").GetString() == Level3ChapterRules.WorksheetTrigger && ObjectiveTriggers.Validate(Level3ChapterRules.WorksheetTrigger) == null &&
      ObjectiveTriggers.Events.Contains(ObjectiveTriggers.WorksheetDrafted),
    "objective 3 (revision; was 4): build Bea's worksheet (a known event raised only by a saved, filled-in draft)");
GuideScript guide5 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var intro5 = guide5.lessons.FirstOrDefault(l => l.id == Level3ChapterRules.WorksheetLesson);
var notes5 = guide5.lessons.FirstOrDefault(l => l.id == Level3ChapterRules.WorksheetNotesLesson);
Check(guide5.Validate() == "" && intro5 != null && notes5 != null && new[] { intro5, notes5 }.All(l => l.pages.All(wsP => !string.IsNullOrEmpty(wsP.waitFor))),
    "the worksheet lessons wait for the player's own action (they never block the board)");
// Revised T05 (4 November 2026) pin: the table lesson (same id) is the machine's feed/select/pull, each step
// waiting on the player's own action; the notes lesson (same id) is the Why? hint, waiting on a chosen reason.
Check(intro5.pages.Length == 3 && intro5.pages[0].waitFor == "cue:MachineFed" && intro5.pages[1].waitFor == "cue:MachineChosen" &&
      intro5.pages[2].waitFor == "cue:WorksheetFiled|cue:PracticeSlipFiled" && notes5.when == "flag:machineWhy" && notes5.pages[0].waitFor == "cue:MachineReasonChosen" &&
      intro5.onlyIn == "mode:Workstation",
    "hands-on: the table lesson waits for the player's own feed, choice and saved filing; the Why? hint waits for a chosen reason");
Check(!new[] { intro5, notes5 }.SelectMany(l => l.pages).Any(wsP => wsP.text.Contains("Personal purpose") || wsP.text.Contains("Already represented") || wsP.text.Contains("Not deductible") || wsP.text.Contains("click", StringComparison.OrdinalIgnoreCase)),
    "Arthur names no answer and speaks touch (tap/drag), never mouse words");
string cp5 = Src("Scripts/Level3/Level3Checkpointer.cs");
int c5 = Index(cp5, "public TapeCommit CommitWorksheet(");
string c5Body = cp5.Substring(c5, Index(cp5, "/// T04: Bea's handover") - c5);
Check(Index(c5Body, "State.Copy()") < Index(c5Body, "edit(candidate, out string reason)") && Index(c5Body, "edit(candidate, out string reason)") < Index(c5Body, "CommitLevelThreeCheckpoint(candidate)") &&
      Index(c5Body, "CommitLevelThreeCheckpoint(candidate)") < Index(c5Body, "Publish(candidate);") && c5Body.Contains("return TapeCommit.SaveFailed;"),
    "every worksheet edit: candidate, signed write, then publish; a failed write returns before publishing");
Check(cp5.Contains("if (!wasDrafted && committed.worksheet != null && committed.worksheet.drafted) GameplayEvents.RaiseWorksheetDrafted();") &&
      cp5.Contains("foreach (string trigger in Level3ChapterRules.ImpliedTriggers(State)) OfficeObjectives.Instance.MarkDone(trigger);") &&
      Src("Scripts/UI/Guide/GuideDirector.cs").Contains("Level3ChapterRules.ImpliedTriggers(chapter.State)"),
    "the drafted event is raised once from a published change; a restore marks it quietly");
foreach (string call in new[] { "Level3WorksheetRules.Extract(", "Level3WorksheetRules.Place(", "Level3WorksheetRules.SetRelated(", "Level3WorksheetRules.SetReason(", "Level3WorksheetRules.Undo(" })
    Check(wsDeskSrc.Contains(call), $"the desk sends {call.Replace("Level3WorksheetRules.", "").TrimEnd('(')} through the chapter command");
Check(!wsDeskSrc.Contains("CommitLevelThreeCheckpoint") && wsDeskSrc.Contains("owner.CommitWorksheet(edit, out hint)") &&
      System.Text.RegularExpressions.Regex.Matches(wsDeskSrc, @"Commit\(\(Level3ChapterCheckpoint c, out string r\)").Count == 4,
    "the desk never writes the save itself; tap-place and drag-drop share one Place command");
int dropAt = Index(wsDeskSrc, "if (dragging)\n        {");
Check(dropAt > 0 && wsDeskSrc.Substring(dropAt, 600).Contains("if (section != null) Place(row, section);") && wsDeskSrc.Substring(dropAt, 600).Contains("sfx_card_return_02"),
    "a drop on a tray runs the same Place; off every tray the strip glides back");
Check(wsDeskSrc.Contains("if (!AtTable)") && wsDeskSrc.Contains("if (Up) Settle();") && wsDeskSrc.Contains("private void OnApplicationFocus(bool focused)") &&
      wsDeskSrc.Contains("table.HoldPapers(true);") && wsDeskSrc.Contains("table.HoldPapers(false);") && wsDeskSrc.Contains("MobileInputGuard.SuppressWorldTapsThisFrame();"),
    "leaving, settings, dialogue, pause or lost focus settle the board and the held strip; papers take no touches while it is up");
Check(wsDeskSrc.Contains("new Level3DeskProps(Level2DeskStamps.DragLayer)") && wsDeskSrc.Contains("1 << Level2DeskStamps.DragLayer") && wsDeskSrc.Contains("model.TryHit(hit.collider, out _)"),
    "the board is a physical 3D object on the 3D-interactable layer and takes only its own targets");
string table5 = Src("Scripts/Level3/Level3BeaTable.cs");
Check(!table5.Contains("AddComponent<Level3WorksheetDesk>") && !table5.Contains("BuildWorksheet") && table5.Contains("Level3PaperMachineModel.Build(props, tableRoot, Layout,") &&
      table5.Contains("public bool ReadOriginal(string sourceId)") && table5.Contains("if (paper.IsAttached) paper.TakeOut();"),
    "revised T04: the table builds no worksheet board; the stand and passive machine stage it, and ReadOriginal lifts the real paper");
Check(wsModelSrc.Contains("\"WORTH THE TAX? · BEA 2026\"") && Level3WorksheetRules.Sections.Select(Level3WorksheetRules.SectionLabel).SequenceEqual(new[] { "Income", "Allowable expenses", "Not deductible", "Supporting only" }),
    "the sheet is titled for Bea's year and has the four labelled sections");
Check(Src("Scripts/Level3/Level3OfficeStart.cs").Contains("public const int SetupVersion = 3;"), "no new part: the worksheet lives on the existing BeaTable (setup v3 unchanged)");
Console.WriteLine($"T05 checks: {checks - t05Start}, failures {fails - t05Fails} (worksheet rules, neutrality, undo, save round trip/failure, wiring; Unity rendering, touch and readability not run).");

// =====================================================================
// T06: subtotal tapes, the deliberate frozen check, corrections, collection
// =====================================================================
int t06Start = checks, t06Fails = fails;

// ---------- helpers: the real calculator, the real commands ----------
(List<Level3CalcToken> expr, string result) Tape6(params string[] rowIds)
{
    var calc6 = new Level3Calculator();
    for (int i = 0; i < rowIds.Length; i++)
    {
        if (i > 0) calc6.Press(Level3CalcKey.Add);
        calc6.SelectSource(Level3WorksheetCheck.StripSource(rowIds[i]), wsRow[rowIds[i]].Label, wsRow[rowIds[i]].Amount);
    }
    calc6.Press(Level3CalcKey.Equals);
    return (calc6.DraftExpression, calc6.DraftResult);
}
Level3ChapterCheckpoint Draft6(string[] plan, string r6, string r7, string related)
{
    Level3ChapterCheckpoint d = handed.Copy();
    for (int i = 0; i < 7; i++)
    {
        Level3WorksheetRules.Extract(d, wsRows, "R" + (i + 1), out _);
        Level3WorksheetRules.Place(d, "R" + (i + 1), plan[i], out _);
    }
    Level3WorksheetRules.SetReason(d, "note-R6", r6, out _);
    Level3WorksheetRules.SetReason(d, "note-R7", r7, out _);
    Level3WorksheetRules.SetRelated(d, "note-R7", related, out _);
    return d;
}
Level3CommandOutcome Attach6(Level3ChapterCheckpoint c, string section, params string[] rowIds)
{
    var (e, r) = Tape6(rowIds);
    return Level3WorksheetCheck.AttachSubtotal(c, wsRows, section, e, r, out _);
}
string[] right6 = { "income", "income", "expenses", "expenses", "expenses", "not-deductible", "supporting" };
Level3ChapterCheckpoint Right6()
{
    Level3ChapterCheckpoint d = Draft6(right6, "personal-purpose", "already-represented", "R5");
    Attach6(d, "income", "R1", "R2");
    Attach6(d, "expenses", "R3", "R4", "R5");
    return d;
}

// ---------- configuration: rewards, badge, hints, book, objectives ----------
LevelRewardRules rules6 = JsonSerializer.Deserialize<LevelRewardRules>(File.ReadAllText(Res("Levels/Level_03/rewards.json")), json);
LevelRewardRules.SideQuestRules sq6 = rules6.sideQuest;
Check(sq6.exp == Level3WorksheetCheck.MaxXp && sq6.minExp == Level3WorksheetCheck.MinXp && sq6.expPerWrong == Level3WorksheetCheck.XpPerWrong &&
      sq6.goldAtCorrect == Level3WorksheetCheck.GoldAt && sq6.silverAtCorrect == Level3WorksheetCheck.SilverAt && sq6.hearts == 0 &&
      sq6.badgeId == Level3WorksheetCheck.BadgeId && sq6.badgeName == "Worth the Tax?" && sq6.badgeEmblem == "receipt" && sq6.badgeRibbon == "#D9962B" &&
      !rules6.requireSideQuestBeforeOffice && rules6.officeDoors.Length == 0,
    "rewards.json: Worth the Tax? (amber receipt badge), 60 XP minus wrong, floor 48, Gold 12 / Silver 10, no score hearts, office open");
bool xpSame6 = true, tierSame6 = true;
for (int w = 0; w <= 12; w++)
{
    if (LevelScoring.SideQuest(rules6, w).Exp != Level3WorksheetCheck.XpFor(w)) xpSame6 = false;
    if ((int)LevelScoring.AccuracyBadge(rules6, 12 - w) != Level3WorksheetCheck.TierFor(12 - w)) tierSame6 = false;
}
Check(xpSame6 && tierSame6 && Level3WorksheetCheck.XpFor(0) == 60 && Level3WorksheetCheck.XpFor(1) == 59 && Level3WorksheetCheck.XpFor(12) == 48 &&
      Level3WorksheetCheck.TierFor(12) == 3 && Level3WorksheetCheck.TierFor(11) == 2 && Level3WorksheetCheck.TierFor(10) == 2 && Level3WorksheetCheck.TierFor(9) == 1 && Level3WorksheetCheck.TierFor(0) == 1,
    "the frozen XP/tier equal the existing ledger's side-quest maths for every wrong count (60..48; Gold 12, Silver 10-11, Bronze 0-9)");
Check(CampaignRewards.Amount("Level_03", "bea-worksheet", "coins") == 60 && CampaignRewards.Amount("Level_03", "bea-worksheet", "trust") == 2 &&
      CampaignRewards.Reason("Level_03", "bea-worksheet", "trust") == "Bea's records organized and checked" &&
      CampaignRewards.Total("Level_01", "coins") == 160 && CampaignRewards.Total("Level_01", "trust") == 6 &&
      CampaignRewards.Total("Level_02", "coins") == 160 && CampaignRewards.Total("Level_02", "trust") == 6,
    "catalogue: Bea's worksheet pays 60 coins and +2 trust ('Bea's records organized and checked'); Levels 1 and 2 are unchanged");
Level3HintFile hints6 = JsonSerializer.Deserialize<Level3HintFile>(File.ReadAllText(Res("Levels/Level_03/worksheet.json")), json);
List<string> hintProblems6 = Level3WorksheetHints.Validate(hints6);
Check(hintProblems6.Count == 0, "worksheet.json: every decision has exactly three hints: " + string.Join("; ", hintProblems6));
Check(Level3WorksheetCheck.DecisionIds.All(id => Enumerable.Range(1, 3).All(l => Level3WorksheetHints.For(hints6, id, l) == Level3WorksheetHints.For(Level3WorksheetHints.BuiltIn(), id, l))) &&
      Level3WorksheetHints.Validate(Level3WorksheetHints.BuiltIn()).Count == 0,
    "the built-in hints mirror worksheet.json word for word");
Check(Level3WorksheetCheck.DecisionIds.All(id => Level3WorksheetHints.For(hints6, id, 1).Length > 0 && !Level3WorksheetHints.For(hints6, id, 1).Contains("Choose") && !Level3WorksheetHints.For(hints6, id, 1).Contains(" go in ")) &&
      hints6.hints.SelectMany(h => h.lines).All(l => !l.Contains("900,000") && !l.Contains("300,000") && !l.Contains("click", StringComparison.OrdinalIgnoreCase)),
    "the first hint only says where to look (never the answer); no hint gives a total or uses mouse words");
var book6 = Json("Levels/Level_03/book.json").GetProperty("sections").EnumerateArray().ToList();
Check(book6.Count >= 2 && book6[0].GetProperty("heading").GetString() == "Section 4 – Costs and Purpose" && book6[1].GetProperty("heading").GetString() == "Section 5 – Count Once" &&
      book6.Where(s => !s.GetProperty("heading").GetString().StartsWith("Section 7")).All(s => s.GetProperty("body").GetString().Split("\n\n").Length == s.GetProperty("clickableValues").GetArrayLength() &&
                     s.GetProperty("clickableValues").EnumerateArray().All(v => v.GetProperty("readOnly").GetBoolean())),
    "book.json adds the two leaves the hints name (L3-33), one paragraph per read-only term");
string bookText6 = File.ReadAllText(Res("Levels/Level_03/book.json"));
foreach (string leak in new[] { "500,000", "400,000", "180,000", "90,000", "30,000", "24,000", "900,000", "300,000", "Bea", "PRINT-26", "birthday" })
    Check(!book6.Take(2).Any(s => s.GetRawText().Contains(leak)), $"the worksheet leaves (Sections 4-5) state no client answer or figure ('{leak}')");
var steps6 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps6.Count >= 5 && steps6[3].GetProperty("trigger").GetString() == Level3ChapterRules.WorksheetReadyTrigger &&
      steps6[4].GetProperty("trigger").GetString() == Level3ChapterRules.WorksheetCollectedTrigger &&
      ObjectiveTriggers.Validate(Level3ChapterRules.WorksheetReadyTrigger) == null && ObjectiveTriggers.Validate(Level3ChapterRules.WorksheetCollectedTrigger) == null,
    "objectives 4-5 (revision; were 5-6): check and fix the worksheet (WorksheetReady), collect it (WorksheetCollected), both known events");

// ---------- subtotal tapes: the strips themselves, any set, stale on change ----------
Level3ChapterCheckpoint tp = Draft6(right6, "personal-purpose", "already-represented", "R5");
int revBefore6 = tp.worksheet.revision;
var (incExpr, incResult) = Tape6("R1", "R2");
Check(incResult == "900000" && Level3Calculator.TapeLines(incExpr, incResult).SequenceEqual(new[] { "500,000.00 LOGO", "+ 400,000.00 MENU", "= 900,000.00" }),
    "tapping strips on the real calculator prints their amounts with their row tags (500,000.00 LOGO + 400,000.00 MENU = 900,000.00)");
string tpBefore = Snap(tp);
Level3ChapterCheckpoint tpA = tp.Copy();
Check(Level3WorksheetCheck.AttachSubtotal(tpA, wsRows, "income", incExpr, incResult, out string attachWhy) == Level3CommandOutcome.Applied && attachWhy == null &&
      tpA.Tape(Level3WorksheetCheck.IncomeTape).result == "900000" && tpA.Tape(Level3WorksheetCheck.IncomeTape).caseId == "L3-BEA-001" &&
      tpA.Tape(Level3WorksheetCheck.IncomeTape).expression.Count(t => t.IsNumber) == 2 && !tpA.worksheet.incomeDirty && tpA.worksheet.revision == revBefore6 + 1 &&
      Level3WorksheetCheck.IsCurrent(tpA, "income") && Snap(tp) == tpBefore,
    "the Income tape attaches (on the candidate only): its own expression and exact result, the section current, one revision");
Check(Level3WorksheetCheck.AttachSubtotal(tpA, wsRows, "income", incExpr, incResult, out string dupWhy) == Level3CommandOutcome.Duplicate && dupWhy == "already attached",
    "the same current tape again is a quiet duplicate");
var typed6 = Calc();
Keys(typed6, "500000+400000=");
Check(Level3WorksheetCheck.AttachSubtotal(tp.Copy(), wsRows, "income", typed6.DraftExpression, typed6.DraftResult, out string typedWhy) == Level3CommandOutcome.Refused &&
      typedWhy == Level3WorksheetCheck.RefuseTyped, "typed amounts are refused with a plain reason: a subtotal uses the strips themselves");
var foreign6 = Calc();
Level3PracticeSource budget6 = Level3PracticeRules.Sources(null).First();
foreign6.SelectSource(budget6.SourceId, budget6.Label, budget6.Amount);
foreign6.Press(Level3CalcKey.Add);
foreign6.SelectSource(Level3WorksheetCheck.StripSource("R1"), wsRow["R1"].Label, wsRow["R1"].Amount);
foreign6.Press(Level3CalcKey.Equals);
Check(Level3WorksheetCheck.AttachSubtotal(tp.Copy(), wsRows, "income", foreign6.DraftExpression, foreign6.DraftResult, out string foreignWhy) == Level3CommandOutcome.Refused &&
      foreignWhy == Level3WorksheetCheck.RefuseForeign, "the practice card's numbers are not Bea's strips: refused");
var chained6 = Calc();
chained6.SelectSource(Level3WorksheetCheck.StripSource("R1"), "Logo fees", "500000.00");
chained6.Press(Level3CalcKey.Equals);
chained6.Press(Level3CalcKey.Add);
chained6.SelectSource(Level3WorksheetCheck.StripSource("R2"), "Menu", "400000.00");
chained6.Press(Level3CalcKey.Equals);
Check(Level3WorksheetCheck.AttachSubtotal(tp.Copy(), wsRows, "income", chained6.DraftExpression, chained6.DraftResult, out _) == Level3CommandOutcome.Refused,
    "a tape continued from an earlier result has lost its strips' records: refused");
Check(Level3WorksheetCheck.AttachSubtotal(tp.Copy(), wsRows, "not-deductible", incExpr, incResult, out _) == Level3CommandOutcome.Refused &&
      Level3WorksheetCheck.AttachSubtotal(tp.Copy(), wsRows, "income", incExpr, "900001", out _) == Level3CommandOutcome.Refused &&
      Level3WorksheetCheck.AttachSubtotal(tp.Copy(), wsRows, "income", new List<Level3CalcToken>(), "0", out string noTape) == Level3CommandOutcome.Refused && noTape == Level3WorksheetCheck.RefuseNoTape,
    "only money sections take a tape; a printed result is re-checked exactly; no tape, no attach");
Level3ChapterCheckpoint noR1 = handed.Copy();
Level3WorksheetRules.Extract(noR1, wsRows, "R2", out _);
Check(Level3WorksheetCheck.AttachSubtotal(noR1, wsRows, "income", incExpr, incResult, out _) == Level3CommandOutcome.Refused, "a strip not on the worksheet cannot be on a tape");
Level3ChapterCheckpoint emptyIncome = Draft6(new[] { "expenses", "expenses", "expenses", "expenses", "expenses", "not-deductible", "supporting" }, "personal-purpose", "already-represented", "R5");
var zero6 = Calc();
Keys(zero6, "0=");
var five6 = Calc();
Keys(five6, "5=");
Check(Level3WorksheetCheck.AttachSubtotal(emptyIncome.Copy(), wsRows, "income", zero6.DraftExpression, zero6.DraftResult, out _) == Level3CommandOutcome.Applied &&
      Level3WorksheetCheck.AttachSubtotal(emptyIncome.Copy(), wsRows, "income", five6.DraftExpression, five6.DraftResult, out _) == Level3CommandOutcome.Refused &&
      Level3WorksheetCheck.AttachSubtotal(tp.Copy(), wsRows, "income", zero6.DraftExpression, zero6.DraftResult, out _) == Level3CommandOutcome.Refused,
    "an empty section takes an explicit 0 tape; any other typed number, or 0 for a section with strips, is refused");
Check(Attach6(tp.Copy(), "income", "R1", "R3") == Level3CommandOutcome.Applied && Attach6(tp.Copy(), "expenses", "R3") == Level3CommandOutcome.Applied,
    "a tape that adds the wrong strips is still accepted (no answer leaks at attach; the Check judges it)");
Level3ChapterCheckpoint stale6 = tpA.Copy();
Attach6(stale6, "expenses", "R3", "R4", "R5");
Level3WorksheetRules.Place(stale6, "R7", "not-deductible", out _);
Check(Level3WorksheetCheck.IsCurrent(stale6, "income") && Level3WorksheetCheck.IsCurrent(stale6, "expenses"),
    "moving a strip between non-money sections leaves both tapes current");
Level3WorksheetRules.Place(stale6, "R6", "income", out _);
Check(Level3WorksheetCheck.IsStale(stale6, "income") && !Level3WorksheetCheck.IsCurrent(stale6, "income") && stale6.Tape(Level3WorksheetCheck.IncomeTape) != null &&
      Level3WorksheetCheck.IsCurrent(stale6, "expenses"),
    "a strip moved into Income makes only Income's tape stale; the old tape stays on the sheet until replaced");
Level3WorksheetRules.Undo(stale6, out _);
Check(Level3WorksheetCheck.IsStale(stale6, "income"), "undoing the move still asks for a fresh tape (stale until explicitly replaced)");
Check(Attach6(stale6, "income", "R1", "R2") == Level3CommandOutcome.Applied && Level3WorksheetCheck.IsCurrent(stale6, "income"), "a new tape replaces the stale one");

// ---------- completeness before any grade ----------
Level3WorksheetEvaluation fresh6 = Level3WorksheetCheck.Evaluate(handed, bea);
Check(!fresh6.Complete && fresh6.Decisions.Count == 0 && fresh6.MissingPlaces.Count(p => p.StartsWith("lift:")) == 7 &&
      fresh6.MissingPlaces.Contains("note-R6/reason") && fresh6.MissingPlaces.Contains("note-R7/related") && fresh6.MissingPlaces.Contains("subtotal:income") &&
      fresh6.MissingLines.Count == fresh6.MissingPlaces.Count && fresh6.MissingLines.All(l => l.Length > 0),
    "an empty worksheet lists every missing place with a plain line and grades nothing");
Check(fresh6.MissingLines.Any(l => l == "Printing bill, paid is still on its record (Print bill)."), "a missing row names its record tab, never its section");
Level3ChapterCheckpoint noTapes6 = Draft6(right6, "personal-purpose", "already-represented", "R5");
Level3WorksheetEvaluation noTapeEval = Level3WorksheetCheck.Evaluate(noTapes6, bea);
Check(!noTapeEval.Complete && noTapeEval.MissingPlaces.SequenceEqual(new[] { "subtotal:income", "subtotal:expenses" }),
    "filling in the last field never makes it complete: both subtotal tapes are still needed");
Level3ChapterCheckpoint staleCheck = Right6();
Level3WorksheetRules.Place(staleCheck, "R6", "expenses", out _);
Level3WorksheetEvaluation staleEval = Level3WorksheetCheck.Evaluate(staleCheck, bea);
Check(!staleEval.Complete && staleEval.MissingPlaces.SequenceEqual(new[] { "subtotal:expenses" }) && staleEval.MissingLines[0].Contains("out of date"),
    "a stale tape makes the worksheet incomplete: 'out of date: its entries changed, so recalculate'");
string incompleteSnap = Snap(staleCheck);
Check(Level3WorksheetCheck.Check(staleCheck, bea, out Level3WorksheetEvaluation incompleteEval, out _) == Level3WorksheetCheck.CheckOutcome.Incomplete &&
      Snap(staleCheck) == incompleteSnap && staleCheck.worksheetCheck.checks == 0 && !staleCheck.worksheetCheck.firstChecked && staleCheck.phase == "WorksheetDraft" &&
      !incompleteEval.Complete,
    "an incomplete Check freezes nothing: no grade, no write, still WorksheetDraft");

// ---------- the twelve decisions and FIXTURE_CONTRACT's fairness examples ----------
(int correct, int tier, int xp, string wrong) Grade6(Level3ChapterCheckpoint s)
{
    Level3ChapterCheckpoint g = s.Copy();
    Level3WorksheetCheck.CheckOutcome o = Level3WorksheetCheck.Check(g, bea, out Level3WorksheetEvaluation e, out string why);
    if (o != Level3WorksheetCheck.CheckOutcome.Checked) return (-1, -1, -1, o + ": " + why);
    return (g.worksheetCheck.firstCorrect, g.worksheetCheck.firstTier, g.worksheetCheck.firstXp, string.Join(",", g.worksheetCheck.firstWrong));
}
Check(Grade6(Right6()) == (12, 3, 60, ""), "the supported worksheet: 12/12, Gold, 60 XP");
Level3ChapterCheckpoint r6Wrong = Draft6(new[] { "income", "income", "expenses", "expenses", "expenses", "expenses", "supporting" }, "personal-purpose", "already-represented", "R5");
Attach6(r6Wrong, "income", "R1", "R2");
Attach6(r6Wrong, "expenses", "R3", "R4", "R5", "R6");
Check(Grade6(r6Wrong) == (11, 2, 59, "dest-R6") && Level3WorksheetCheck.Evaluate(r6Wrong, bea).ExpensesTotal == "324000",
    "only R6 moved to expenses, reasons/link right, sums 900,000/324,000: 11/12 Silver 59 XP (the matching sum is not charged again)");
Level3ChapterCheckpoint r7Wrong = Draft6(new[] { "income", "income", "expenses", "expenses", "expenses", "not-deductible", "expenses" }, "personal-purpose", "already-represented", "R5");
Attach6(r7Wrong, "income", "R1", "R2");
Attach6(r7Wrong, "expenses", "R3", "R4", "R5", "R7");
Check(Grade6(r7Wrong) == (11, 2, 59, "dest-R7") && Level3WorksheetCheck.Evaluate(r7Wrong, bea).ExpensesTotal == "330000",
    "only R7 also deducted, sum 330,000: 11/12 Silver 59 XP (R5 and R7 each counted once earns the arithmetic)");
Level3ChapterCheckpoint bothWrong = Draft6(new[] { "income", "income", "expenses", "expenses", "expenses", "expenses", "expenses" }, "personal-purpose", "already-represented", "R5");
Attach6(bothWrong, "income", "R1", "R2");
Attach6(bothWrong, "expenses", "R3", "R4", "R5", "R6", "R7");
Check(Grade6(bothWrong) == (10, 2, 58, "dest-R6,dest-R7") && Level3WorksheetCheck.Evaluate(bothWrong, bea).ExpensesTotal == "354000",
    "both wrong placements with sum 354,000: 10/12 Silver 58 XP");
Level3ChapterCheckpoint bothWrongShort = Draft6(new[] { "income", "income", "expenses", "expenses", "expenses", "expenses", "expenses" }, "personal-purpose", "already-represented", "R5");
Attach6(bothWrongShort, "income", "R1", "R2");
Attach6(bothWrongShort, "expenses", "R3", "R4", "R5");
Check(Grade6(bothWrongShort) == (9, 1, 57, "dest-R6,dest-R7,subtotal-expenses"),
    "those placements with an expense tape of 300,000 (strips left out): 9/12 Bronze 57 XP, an independent subtotal error");
Level3ChapterCheckpoint allWrong = Draft6(new[] { "expenses", "not-deductible", "income", "supporting", "not-deductible", "income", "expenses" }, "business-purpose", "different-period", "R2");
Attach6(allWrong, "income", "R3");
Attach6(allWrong, "expenses", "R1", "R7", "R4");
Check(Grade6(allWrong) == (0, 1, 48, string.Join(",", Level3WorksheetCheck.DecisionIds)), "all twelve wrong: 0/12, Bronze, the 48 XP floor");
Level3ChapterCheckpoint reasonsOnly = r6Wrong.Copy();
Level3WorksheetRules.SetReason(reasonsOnly, "note-R6", "business-purpose", out _);
Check(Grade6(reasonsOnly) == (10, 2, 58, "dest-R6,reason-R6"), "a wrong destination and an independently wrong reason are two separate judgments");
Level3ChapterCheckpoint twice6 = Draft6(right6, "personal-purpose", "already-represented", "R5");
Attach6(twice6, "income", "R1", "R2");
Attach6(twice6, "expenses", "R3", "R4", "R5", "R5");
Check(Grade6(twice6).wrong == "subtotal-expenses", "a strip added twice is not the section's subtotal (each once)");
Level3ChapterCheckpoint minus6 = Draft6(right6, "personal-purpose", "already-represented", "R5");
var minusCalc = new Level3Calculator();
minusCalc.SelectSource(Level3WorksheetCheck.StripSource("R1"), "Logo", "500000.00");
minusCalc.Press(Level3CalcKey.Subtract);
minusCalc.SelectSource(Level3WorksheetCheck.StripSource("R2"), "Menu", "400000.00");
minusCalc.Press(Level3CalcKey.Equals);
Level3WorksheetCheck.AttachSubtotal(minus6, wsRows, "income", minusCalc.DraftExpression, minusCalc.DraftResult, out _);
Attach6(minus6, "expenses", "R3", "R4", "R5");
Check(Grade6(minus6).wrong == "subtotal-income", "subtracting the section's strips is not its subtotal");
Level3ChapterCheckpoint wrongLink = Draft6(right6, "personal-purpose", "already-represented", "R3");
Attach6(wrongLink, "income", "R1", "R2");
Attach6(wrongLink, "expenses", "R3", "R4", "R5");
Check(Grade6(wrongLink) == (11, 2, 59, "related-R7"), "linking the printing bill to the wrong row is the one relationship decision");
Level3ChapterCheckpoint emptyRight = Draft6(new[] { "expenses", "expenses", "expenses", "expenses", "expenses", "not-deductible", "supporting" }, "personal-purpose", "already-represented", "R5");
Level3WorksheetCheck.AttachSubtotal(emptyRight, wsRows, "income", zero6.DraftExpression, zero6.DraftResult, out _);
Attach6(emptyRight, "expenses", "R1", "R2", "R3", "R4", "R5");
Check(Grade6(emptyRight) == (10, 2, 58, "dest-R1,dest-R2"), "an explicit 0 tape on an empty section earns its arithmetic; only the treatments are wrong");

// ---------- freeze once, correct, ready ----------
Level3ChapterCheckpoint fr = r6Wrong.Copy();
Check(Level3WorksheetCheck.Check(fr, bea, out Level3WorksheetEvaluation frEval, out _) == Level3WorksheetCheck.CheckOutcome.Checked &&
      fr.phase == "WorksheetCorrections" && fr.worksheetCheck.firstChecked && fr.worksheetCheck.firstRevision == r6Wrong.worksheet.revision &&
      fr.worksheetCheck.firstTotal == 12 && fr.worksheetCheck.checks == 1 && fr.actionIds.Contains(Level3WorksheetCheck.FirstCheckAction) &&
      fr.milestones.Contains(Level3WorksheetCheck.CheckedMilestone) && fr.worksheetCheck.lastWrong.SequenceEqual(new[] { "dest-R6" }) && fr.worksheetCheck.lastValues.Count == 12,
    "the first complete Check freezes 11/12 once and opens WorksheetCorrections (the same draft stays editable)");
string frozen6(Level3ChapterCheckpoint s) => $"{s.worksheetCheck.firstChecked}/{s.worksheetCheck.firstRevision}/{s.worksheetCheck.firstCorrect}/{s.worksheetCheck.firstTotal}/{string.Join(",", s.worksheetCheck.firstWrong)}/{s.worksheetCheck.firstTier}/{s.worksheetCheck.firstXp}";
string frozenFirst = frozen6(fr);
Check(Level3WorksheetCheck.MarkFor(fr, "dest-R6") == Level3WorksheetCheck.Mark.LookAgain && Level3WorksheetCheck.MarkFor(fr, "dest-R1") == Level3WorksheetCheck.Mark.Confirmed &&
      Level3WorksheetCheck.MarkFor(fr, "subtotal-expenses") == Level3WorksheetCheck.Mark.Confirmed && Level3WorksheetCheck.MarkFor(handed, "dest-R6") == Level3WorksheetCheck.Mark.None,
    "after the check: the wrong place says look again, right ones (and the arithmetic of the chosen set) are confirmed; nothing is marked before a check");
string frSnap = Snap(fr);
Check(Level3WorksheetCheck.Check(fr, bea, out _, out string sameWhy) == Level3WorksheetCheck.CheckOutcome.Unchanged && Snap(fr) == frSnap && sameWhy.Contains("Nothing has changed"),
    "checking an unchanged worksheet again changes and saves nothing");
Level3WorksheetRules.Place(fr, "R6", "not-deductible", out _);
Check(Level3WorksheetCheck.MarkFor(fr, "dest-R6") == Level3WorksheetCheck.Mark.None && Level3WorksheetCheck.MarkFor(fr, "subtotal-expenses") == Level3WorksheetCheck.Mark.None &&
      Level3WorksheetCheck.IsStale(fr, "expenses"),
    "a corrected place loses its mark until the next check; its section's tape turns stale");
Check(Level3WorksheetCheck.Check(fr, bea, out _, out _) == Level3WorksheetCheck.CheckOutcome.Incomplete && frozen6(fr) == frozenFirst && fr.phase == "WorksheetCorrections",
    "a check with a stale tape is incomplete; the frozen first result is untouched");
Attach6(fr, "expenses", "R3", "R4", "R5");
Check(Level3WorksheetCheck.Check(fr, bea, out Level3WorksheetEvaluation readyEval, out _) == Level3WorksheetCheck.CheckOutcome.Checked && readyEval.AllCorrect &&
      fr.phase == "WorksheetReady" && fr.actionIds.Contains(Level3WorksheetCheck.ReadyAction) && frozen6(fr) == frozenFirst &&
      fr.worksheetCheck.checks == 2 && fr.worksheetCheck.lastWrong.Count == 0 && readyEval.IncomeTotal == "900000" && readyEval.ExpensesTotal == "300000",
    "corrected and re-tallied: every decision right with current 900,000 / 300,000 -> WorksheetReady; the first grade stays 11/12 Silver 59");
Check(Level3WorksheetCheck.DecisionIds.All(id => Level3WorksheetCheck.MarkFor(fr, id) == Level3WorksheetCheck.Mark.Confirmed), "ready: every place is confirmed");
Check(Level3WorksheetRules.Place(fr.Copy(), "R1", "expenses", out _) == Level3CommandOutcome.Refused && Attach6(fr.Copy(), "income", "R1", "R2") == Level3CommandOutcome.Refused &&
      Level3WorksheetCheck.Check(fr.Copy(), bea, out _, out _) == Level3WorksheetCheck.CheckOutcome.Refused,
    "a ready worksheet is set: no edits, no new tape, no further check");
Level3ChapterCheckpoint gold6 = Right6();
Level3WorksheetCheck.Check(gold6, bea, out _, out _);
Check(gold6.phase == "WorksheetReady" && gold6.worksheetCheck.firstTier == 3 && gold6.worksheetCheck.firstXp == 60 && !gold6.actionIds.Contains(Level3WorksheetCheck.FirstCheckAction),
    "a perfect first check goes straight to WorksheetReady (Gold, 60 XP)");
Level3ChapterCheckpoint fromBronze = allWrong.Copy();
Level3WorksheetCheck.Check(fromBronze, bea, out _, out _);
string bronzeFrozen = frozen6(fromBronze);
Level3ChapterCheckpoint fixedAll = fromBronze.Copy();
for (int i = 0; i < 7; i++) Level3WorksheetRules.Place(fixedAll, "R" + (i + 1), right6[i], out _);
Level3WorksheetRules.SetReason(fixedAll, "note-R6", "personal-purpose", out _);
Level3WorksheetRules.SetReason(fixedAll, "note-R7", "already-represented", out _);
Level3WorksheetRules.SetRelated(fixedAll, "note-R7", "R5", out _);
Attach6(fixedAll, "income", "R1", "R2");
Attach6(fixedAll, "expenses", "R3", "R4", "R5");
Check(Level3WorksheetCheck.Check(fixedAll, bea, out _, out _) == Level3WorksheetCheck.CheckOutcome.Checked && fixedAll.phase == "WorksheetReady" &&
      frozen6(fixedAll) == bronzeFrozen && fixedAll.worksheetCheck.firstXp == 48,
    "correcting everything after a 0/12 first check reaches Ready but the frozen grade stays Bronze, 48 XP (no XP farming by corrections)");
Level3ChapterCheckpoint corrected11 = fixedAll.Copy();

// ---------- collection: the chapter, the claims and the mastery together ----------
Check(Level3WorksheetCheck.Acquire(r6Wrong.Copy(), bea, out string notReady) == Level3CommandOutcome.Refused && notReady.Contains("Check the worksheet first") &&
      Level3WorksheetCheck.Acquire(Right6(), bea, out _) == Level3CommandOutcome.Refused,
    "nothing can be collected before a check finds it ready (a perfect but unchecked draft included)");
Level3ChapterCheckpoint acq = fr.Copy();
string readySnap = Snap(fr);
Check(Level3WorksheetCheck.Acquire(acq, bea, out _) == Level3CommandOutcome.Applied && Snap(fr) == readySnap &&
      acq.phase == "BeaComputation" && acq.activeCaseId == "L3-BEA-001" && acq.Case("L3-BEA-001").workIds.SequenceEqual(new[] { Level3WorksheetCheck.WorksheetWorkId }) &&
      acq.milestones.Contains(Level3WorksheetCheck.AcquiredMilestone) && Level3WorksheetCheck.Collected(acq) && Level3ChapterRules.Problem(acq) == null &&
      !acq.Case("L3-BEA-001").acquiredSourceIds.Contains("B-WHT") && frozen6(acq) == frozenFirst,
    "collecting the ready worksheet: WorksheetReady -> BeaComputation, the worksheet in Bea's folder, the grade kept (B-WHT still waits for T08)");
string acqSnap = Snap(acq);
Check(Level3WorksheetCheck.Acquire(acq, bea, out _) == Level3CommandOutcome.Duplicate && Snap(acq) == acqSnap, "a second collect (double tap, restore) is a quiet duplicate");
PlayerProgressData paid6 = Level3ChapterRules.WithCheckpoint(Clone(live), acq);
Level3WorksheetGrants g6 = Level3WorksheetCheck.ApplyAcquisitionGrants(paid6, acq);
CampaignRecord pc = paid6.campaign;
Check(g6.Changed && !g6.AlreadyClaimed && g6.Coins.Applied && g6.Coins.Amount == 60 && g6.Coins.Before == 245 && g6.Coins.After == 305 &&
      g6.Trust.Applied && g6.Trust.Amount == 2 && g6.Trust.After == 65 && g6.Trust.Reason == "Bea's records organized and checked" &&
      pc.coins == 305 && pc.publicTrust == 65,
    "the collection pays 60 coins (245 -> 305) and +2 Public Trust (63 -> 65) in the same candidate");
Check(pc.rewardClaims.Contains("fixture-campaign-l3/Level_03/bea-worksheet/coins") && pc.rewardClaims.Contains("fixture-campaign-l3/Level_03/bea-worksheet/trust") &&
      pc.completedActivityIds.Contains("Level_03/bea-worksheet") && pc.ledger.Count == live.campaign.ledger.Count + 2 &&
      pc.ledger.Any(l => l.kind == "trust" && l.amount == 2 && l.reason == "Bea's records organized and checked") && pc.ledger.Any(l => l.kind == "coins" && l.amount == 60),
    "each grant is a distinct campaign/level/activity/kind claim with a ledger line and its named reason");
Check(g6.Xp == 59 && g6.Tier == 2 && g6.MasteryClaimed && pc.rewardClaims.Contains("fixture-campaign-l3/Level_03/bea-worksheet/mastery") &&
      Level3WorksheetCheck.MasteryClaimId(paid6) == "fixture-campaign-l3/Level_03/bea-worksheet/mastery" &&
      paid6.badgeRecords.Any(b => b.badgeId == "worth_the_tax_3" && b.tier == 2) && paid6.campaign.levelThree.worksheetCheck.firstXp == 59,
    "the mastery award is saved with them: a once-only mastery claim, the Silver Worth the Tax? badge, the frozen 59 XP in the chapter");
Check(paid6.GetRecord("Level_03") == null && paid6.totalExp == live.totalExp && !paid6.completedLevelIds.Contains("Level_03") &&
      !paid6.passedLevelIds.Contains("Level_03") && !pc.IsChapterCompleted("Level_03") && pc.ledger.Count(l => l.kind == "mastery") == 0,
    "no level record is written early (a level record means a completed level): Level 3 is not completed or passed; the XP joins the chapter result (T14)");
Check(pc.currentChapterId == live.campaign.currentChapterId && pc.inventory.hotbar[0].itemId == "notes" && pc.inventory.bag[0].quantity == 2 &&
      pc.rosaEthics.choiceId == live.campaign.rosaEthics.choiceId && pc.ownedUpgradeIds.SequenceEqual(live.campaign.ownedUpgradeIds),
    "the collection keeps every carried item, Rosa's history and the upgrades");
string paidSnap = Snap(paid6);
Level3WorksheetGrants again6 = Level3WorksheetCheck.ApplyAcquisitionGrants(paid6, acq);
Check(!again6.Changed && again6.AlreadyClaimed && Snap(paid6) == paidSnap, "applying the collection again (reload, retry after success) pays nothing more");
Check(!Level3WorksheetCheck.ApplyAcquisitionGrants(Level3ChapterRules.WithCheckpoint(Clone(live), fr), fr).Changed &&
      !Level3WorksheetCheck.ApplyAcquisitionGrants(new PlayerProgressData(), acq).Changed,
    "no claim before the worksheet is collected, and none without a campaign");
PlayerProgressData nearCap = Fixture(true);
nearCap.campaign.publicTrust = 99;
Level3WorksheetGrants capped6 = Level3WorksheetCheck.ApplyAcquisitionGrants(Level3ChapterRules.WithCheckpoint(nearCap, acq), acq);
Check(capped6.Trust.After == 100 && capped6.Trust.Amount == 1, "trust is clamped at 100 and the actual delta is recorded");
PlayerProgressData bronzePaid = Level3ChapterRules.WithCheckpoint(Clone(live), corrected11);
Level3ChapterCheckpoint bronzeAcq = corrected11.Copy();
Level3WorksheetCheck.Acquire(bronzeAcq, bea, out _);
bronzePaid = Level3ChapterRules.WithCheckpoint(bronzePaid, bronzeAcq);
Level3WorksheetGrants bronzeGrants = Level3WorksheetCheck.ApplyAcquisitionGrants(bronzePaid, bronzeAcq);
Check(bronzeGrants.Coins.Amount == 60 && bronzeGrants.Trust.Amount == 2 && bronzeGrants.Xp == 48 && bronzeGrants.Tier == 1,
    "a Bronze worksheet still pays the full 60 coins and +2 trust once corrected (the fixed first-story coins never shrink)");
PlayerProgressData written6 = Clone(paid6);
written6.Normalize();
PlayerProgressData paidBack = JsonSerializer.Deserialize<PlayerProgressData>(Snap(written6), json);
Check(!paidBack.Normalize() && Level3ChapterRules.Decide(paidBack.campaign.levelThree) == Level3EntryDecision.Resume &&
      Level3WorksheetCheck.Collected(paidBack.campaign.levelThree) && frozen6(paidBack.campaign.levelThree) == frozenFirst &&
      paidBack.campaign.levelThree.Tape(Level3WorksheetCheck.IncomeTape).result == "900000" &&
      Level3WorksheetCheck.ApplyAcquisitionGrants(paidBack, paidBack.campaign.levelThree).AlreadyClaimed,
    "the collected chapter, its frozen grade, tapes and claims round-trip without repair and can never pay twice after reload");
PlayerProgressData normalized6 = Clone(paid6);
normalized6.Normalize();
Check(!normalized6.completedLevelIds.Contains("Level_03") && normalized6.GetRecord("Level_03") == null && normalized6.badgeRecords.Any(b => b.badgeId == "worth_the_tax_3"),
    "the save's own repair (run before every write) still never treats Level 3 as completed after the collection");
string acqChapter = Snap(acq);
foreach (string leak in new[] { "expected", "worksheetDecisions", "expectedFields", "Personal purpose", "Already represented", "Allowable expenses", "Not deductible", "Supporting only", "R5/B-EXP", "level3" })
    Check(!acqChapter.Contains(leak), $"the checked and collected chapter holds no answer key or label ('{leak}')");
var resultBroken = new Level3WorksheetResult { firstWrong = null, lastValues = null, lastWrong = null, checks = -2, firstTier = 7 };
Check(resultBroken.Repair() && resultBroken.firstWrong != null && resultBroken.lastValues != null && resultBroken.checks == 0 && resultBroken.firstTier == 3 && !resultBroken.Repair(),
    "a damaged check record repairs to empty lists and bounded counters");
JsonNode oldT05 = JsonNode.Parse(Snap(Level3ChapterRules.WithCheckpoint(Clone(live), noTapes6)));
oldT05["campaign"]["levelThree"].AsObject().Remove("worksheetCheck");
foreach (JsonNode caseNode6 in oldT05["campaign"]["levelThree"]["cases"].AsArray()) caseNode6.AsObject().Remove("workIds");
PlayerProgressData oldT05Back = JsonSerializer.Deserialize<PlayerProgressData>(oldT05.ToJsonString(), json);
Check(!oldT05Back.Normalize() && !oldT05Back.campaign.levelThree.worksheetCheck.firstChecked && oldT05Back.campaign.levelThree.Case("L3-BEA-001").workIds.Count == 0 &&
      Level3ChapterRules.Decide(oldT05Back.campaign.levelThree) == Level3EntryDecision.Resume,
    "a T05 save (no check record, no work ids) resumes unchecked without a rewrite");

// ---------- failure injection: a failed check or collection changes nothing ----------
string sandbox6 = Path.Combine(Path.GetTempPath(), "TaxVentureL3T06-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox6);
try
{
    string Encode6(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    PlayerProgressData Decode6(string text)
    {
        using JsonDocument doc = JsonDocument.Parse(text);
        string payload = doc.RootElement.GetProperty("payload").GetString();
        return SaveFileStore.SignatureMatches(payload, doc.RootElement.GetProperty("signature").GetString()) ? JsonSerializer.Deserialize<PlayerProgressData>(payload, json) : null;
    }
    // Mirrors PlayerProgressManager.CommitLevelThreeWorksheet: chapter + claims + mastery in ONE candidate, written before it is live.
    bool Collect6(SaveFileStore s, ref PlayerProgressData published, Level3ChapterCheckpoint state, out Level3WorksheetGrants granted)
    {
        granted = default;
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(published), state);
        if (next == null) return false;
        Level3WorksheetGrants due = Level3WorksheetCheck.ApplyAcquisitionGrants(next, state);
        try { next.Normalize(); s.Write(Encode6(next)); }
        catch (Exception) { return false; }
        published = next;
        granted = due;
        return true;
    }
    var store6 = new SaveFileStore(Path.Combine(sandbox6, "progress.json"));
    PlayerProgressData pub6 = Level3ChapterRules.WithCheckpoint(Clone(live), r6Wrong);
    store6.Write(Encode6(pub6));
    store6.Write(Encode6(pub6));

    // A check whose write fails grades nothing (the player can check again).
    Level3ChapterCheckpoint checkCandidate = pub6.campaign.levelThree.Copy();
    Level3WorksheetCheck.Check(checkCandidate, bea, out _, out _);
    byte[] diskBeforeCheck = File.ReadAllBytes(store6.FilePath);
    Directory.CreateDirectory(store6.TempPath);
    bool checkSaved = false;
    try { PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub6), checkCandidate); next.Normalize(); store6.Write(Encode6(next)); pub6 = next; checkSaved = true; }
    catch (Exception) { }
    Check(!checkSaved && !pub6.campaign.levelThree.worksheetCheck.firstChecked && pub6.campaign.levelThree.phase == "WorksheetDraft" &&
          File.ReadAllBytes(store6.FilePath).SequenceEqual(diskBeforeCheck),
        "a failed check write: no frozen grade in memory or on disk, still WorksheetDraft");
    Directory.Delete(store6.TempPath);
    PlayerProgressData retryCheck = Level3ChapterRules.WithCheckpoint(Clone(pub6), checkCandidate);
    retryCheck.Normalize();
    store6.Write(Encode6(retryCheck));
    pub6 = retryCheck;
    Check(Decode6(File.ReadAllText(store6.FilePath)).campaign.levelThree.worksheetCheck.firstCorrect == 11, "retrying the check saves the same first grade");

    // Correct and reach Ready (saved), then a collection whose write fails.
    Level3ChapterCheckpoint readyState = pub6.campaign.levelThree.Copy();
    Level3WorksheetRules.Place(readyState, "R6", "not-deductible", out _);
    Attach6(readyState, "expenses", "R3", "R4", "R5");
    Level3WorksheetCheck.Check(readyState, bea, out _, out _);
    pub6 = Level3ChapterRules.WithCheckpoint(Clone(pub6), readyState);
    pub6.Normalize();
    store6.Write(Encode6(pub6));
    Level3ChapterCheckpoint collectCandidate = pub6.campaign.levelThree.Copy();
    Check(Level3WorksheetCheck.Acquire(collectCandidate, bea, out _) == Level3CommandOutcome.Applied, "the saved ready worksheet can be collected");
    string pubBefore = Snap(pub6);
    byte[] diskBefore = File.ReadAllBytes(store6.FilePath);
    Directory.CreateDirectory(store6.TempPath);
    bool collected6 = Collect6(store6, ref pub6, collectCandidate, out Level3WorksheetGrants failedGrants);
    Check(!collected6 && Snap(pub6) == pubBefore && pub6.campaign.levelThree.phase == "WorksheetReady" && pub6.campaign.coins == 245 && pub6.campaign.publicTrust == 63 &&
          !pub6.campaign.rewardClaims.Any(claim => claim.Contains("Level_03")) && pub6.GetRecord("Level_03") == null && failedGrants.Coins.Amount == 0 && failedGrants.Trust.Amount == 0,
        "a failed collection write: the worksheet is still ready on the table, no coins, trust, claim or mastery, and no grant to show");
    Check(File.ReadAllBytes(store6.FilePath).SequenceEqual(diskBefore), "a failed collection write leaves the signed save on disk unchanged");
    Directory.Delete(store6.TempPath);
    bool retried6 = Collect6(store6, ref pub6, collectCandidate, out Level3WorksheetGrants retryGrants);
    PlayerProgressData disk6 = Decode6(File.ReadAllText(store6.FilePath));
    Check(retried6 && retryGrants.Coins.Amount == 60 && retryGrants.Trust.Amount == 2 && disk6 != null && disk6.campaign.levelThree.phase == "BeaComputation" &&
          disk6.campaign.coins == 305 && disk6.campaign.publicTrust == 65 && disk6.campaign.rewardClaims.Contains(Level3WorksheetCheck.MasteryClaimId(disk6)) &&
          disk6.badgeRecords.Any(b => b.badgeId == "worth_the_tax_3" && b.tier == 2) && !disk6.completedLevelIds.Contains("Level_03") &&
          Level3WorksheetCheck.Collected(disk6.campaign.levelThree) && disk6.campaign.levelThree.worksheetCheck.firstCorrect == 11,
        "the retry writes the collected chapter, 60 coins, +2 trust and the mastery award (claim, badge, frozen 59 XP) together in one signed save");
    bool repeat6 = Collect6(store6, ref pub6, collectCandidate, out Level3WorksheetGrants repeatGrants);
    Check(repeat6 && repeatGrants.AlreadyClaimed && !repeatGrants.Changed && pub6.campaign.coins == 305 && pub6.campaign.publicTrust == 65,
        "a repeated collect after success (duplicate callback) pays nothing more");
}
finally
{
    try { Directory.Delete(sandbox6, true); } catch (IOException) { }
}

// ---------- resume: quiet observers, coarse state, the reference page ----------
Check(Level3ChapterRules.ImpliedTriggers(Level3Phase.WorksheetReady).Contains(Level3ChapterRules.WorksheetReadyTrigger) &&
      !Level3ChapterRules.ImpliedTriggers(Level3Phase.WorksheetCorrections).Contains(Level3ChapterRules.WorksheetReadyTrigger) &&
      Level3ChapterRules.ImpliedTriggers(Level3Phase.BeaComputation).Contains(Level3ChapterRules.WorksheetCollectedTrigger) &&
      !Level3ChapterRules.ImpliedTriggers(Level3Phase.WorksheetReady).Contains(Level3ChapterRules.WorksheetCollectedTrigger),
    "resume: Ready ticks the check objective, a collected worksheet ticks both; earlier phases do not");
Check(Level3ChapterRules.ImpliedLessons(acq).Contains(Level3ChapterRules.CollectedLesson) && Level3ChapterRules.ImpliedLessons(fr).Contains(Level3ChapterRules.CorrectionsLesson) &&
      Level3ChapterRules.ImpliedLessons(r6Wrong).Contains(Level3ChapterRules.SubtotalsLesson) && !Level3ChapterRules.ImpliedLessons(noTapes6).Contains(Level3ChapterRules.SubtotalsLesson) &&
      !Level3ChapterRules.ImpliedLessons(r6Wrong).Contains(Level3ChapterRules.CheckLesson),
    "resume: taught steps never replay (an attached tape, a check, a collection); untaught ones still teach");
Check(Level3ChapterRules.CoarseState(Level3Phase.WorksheetCorrections) == "ReceiveCaseState" && Level3ChapterRules.CoarseState(Level3Phase.WorksheetReady) == "ReceiveCaseState" &&
      Level3ChapterRules.CoarseState(Level3Phase.BeaComputation) == "ComputeTaxesState",
    "the coarse game state stays ReceiveCaseState through the check; BeaComputation shows Compute Taxes (T07)");
List<string> page6 = Level3WorksheetCheck.ReferenceLines(acq, wsRows);
Check(page6.Count >= 8 && page6[0].StartsWith("Income: Logo fees 500,000.00 (B-REV)") && page6.Any(l => l.StartsWith("Income tape: ") && l.Contains("= 900,000.00")) &&
      page6.Any(l => l.StartsWith("Not deductible: Birthday dinner, decor 24,000.00 (B-PERSONAL)")) && page6.Any(l => l.Contains("Printing bill, paid note: Already represented, related to Printing and supplies")) &&
      page6.Last() == "First check: 11/12 (Silver)",
    "the Case Folder's read-only worksheet page is the player's own checked work, section by section, with tapes and the first grade");
Check(Enum.GetValues(typeof(Level3Phase)).Cast<Level3Phase>().All(ph => !Level3DeskCalculator.AwayAtBeaTable(ph)),
    "revised T04: one calculator, always on the office desk (Bea's table has none in any phase)");

// ---------- guide: hands-on, no answers ----------
GuideScript guide6 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var t06Lessons = new[] { Level3ChapterRules.CheckLesson, Level3ChapterRules.CorrectionsLesson, Level3ChapterRules.ReadyLesson, Level3ChapterRules.CollectedLesson }
    .Select(id => guide6.lessons.FirstOrDefault(l => l.id == id)).ToList();
Check(guide6.Validate() == "" && t06Lessons.All(l => l != null) && !guide6.lessons.Any(l => l.id == Level3ChapterRules.SubtotalsLesson), "the T06 lessons exist; the retired board's tape lesson is gone");
Check(t06Lessons[0].when == "flag:machineLookAgain" && t06Lessons[1].when == "flag:machineAccepted" && t06Lessons[2].when == "flag:worksheetReady",
    "hands-on (revised T06): the look-again, accepted and ready lessons follow saved machine checks");
Check(!t06Lessons.SelectMany(l => l.pages).Any(p => p.text.Contains("Personal purpose") || p.text.Contains("Already represented") || p.text.Contains("Not deductible") ||
      p.text.Contains("Supporting only") || p.text.Contains("900,000") || p.text.Contains("click", StringComparison.OrdinalIgnoreCase)),
    "Arthur names no answer or total and speaks touch only");
Check(!guide6.lessons.Any(l => l.id == "calculator-done") && guide6.lessons.First(l => l.id == Level3ChapterRules.AssignmentLesson).pages[0].text.Contains("plaza table"),
    "revision: no practice-done lesson; after the assignment Arthur points to Bea's plaza table");

// ---------- wiring (commands own candidate -> save -> publish; effects only after) ----------
string cp6 = Src("Scripts/Level3/Level3Checkpointer.cs");
string Body6(string src, string start, string end) { int at6 = Index(src, start); return at6 < 0 ? "" : src.Substring(at6, Index(src.Substring(at6), end) < 0 ? src.Length - at6 : Index(src.Substring(at6), end)); }
string collectBody6 = Body6(cp6, "public CollectCommit CollectWorksheet(", "private static CaseDefinition ActiveDefinition");
Check(Index(collectBody6, "Level3WorksheetCheck.Acquire(candidate") < Index(collectBody6, "CommitLevelThreeWorksheet(candidate, out grants, target)") &&
      Index(collectBody6, "return CollectCommit.SaveFailed;") < Index(collectBody6, "Publish(candidate);") && collectBody6.Contains("if (SavesStory)") &&
      !Body6(collectBody6, "else", "Publish(candidate);").Contains("Grant("),
    "collect: candidate, ONE story write of chapter + claims + mastery, then publish; other attempts never touch the campaign");
string checkBody6 = Body6(cp6, "public CheckCommit CheckWorksheet(", "/// <summary>What collecting the worksheet did");
Check(Index(checkBody6, "return CheckCommit.Incomplete;") < Index(checkBody6, "CommitLevelThreeCheckpoint(candidate)") &&
      Index(checkBody6, "CommitLevelThreeCheckpoint(candidate)") < Index(checkBody6, "Publish(candidate);"),
    "check: an incomplete check returns before any write; a complete one is saved before it is published");
Check(cp6.Contains("if (before < Level3Phase.WorksheetReady && Phase == Level3Phase.WorksheetReady) GameplayEvents.RaiseWorksheetReady();") &&
      Index(cp6, "if (before == Level3Phase.WorksheetReady && Phase == Level3Phase.BeaComputation)") < Index(cp6, "GameplayEvents.RaiseWorksheetCollected();") &&
      cp6.Contains("progression.Score.Restoring = true;"),
    "the objectives tick only from saved transitions; a resume restores the side-quest score without a toast");
string mgr6 = Src("Scripts/RewardsSystem/PlayerProgressManager.cs");
string mgrBody6 = Body6(mgr6, "public bool CommitLevelThreeWorksheet(", "/// <summary>Replay from the start");
Check(mgrBody6.Contains("if (!UsesLevelThreeStory) return false;") && Index(mgrBody6, "ApplyAcquisitionGrants(candidate, state)") < Index(mgrBody6, "TryWriteCandidate(candidate)") &&
      Index(mgrBody6, "TryWriteCandidate(candidate)") < Index(mgrBody6, "grants = due;"),
    "the save owner applies the grants to the candidate, writes once, and reports them only after the write");
string review6 = Src("Scripts/Level3/Level3WorksheetDeskReview.cs");
Check(Index(review6, "case Level3Checkpointer.CollectCommit.Collected:") < Index(review6, "StartCollect(grants);") &&
      Body6(review6, "private void LandCollect()", "private void ShowCard()").Contains("HudFeedback.AnnounceCampaign(collectGrants.Trust, collectGrants.Coins") &&
      Body6(review6, "private void LandCollect()", "private void ShowCard()").Contains("progression.Score.AddSideQuest(") &&
      System.Text.RegularExpressions.Regex.Matches(review6, @"AnnounceCampaign\(").Count == 1 && System.Text.RegularExpressions.Regex.Matches(review6, @"AddSideQuest\(").Count == 1 &&
      review6.Contains("if (!awardShown) LandCollect();") && review6.Contains("if (awardShown) return;"),
    "rewards present only after the saved collection, exactly once, even when the player leaves mid-flight");
Check(Index(review6, "if (!preview.Complete) { ShowIncomplete(preview); return; }") < Index(review6, "checkArmed = true;") &&
      Index(review6, "checkArmed = true;") < Index(review6, "owner.CheckWorksheet(out"),
    "the first complete check asks once (CHECK NOW?) after completeness, before anything is graded");
string desk6 = Src("Scripts/Level3/Level3WorksheetDesk.cs");
string attachBody6 = Body6(desk6, "private void AttachTape(string section)", "private static string AfterAttachHint()");
Check(Index(attachBody6, "owner.CommitWorksheetTape(section, expression, result, out string hint)") < Index(attachBody6, "StartTapeFlight(section);") &&
      Index(attachBody6, "case Level3Checkpointer.TapeCommit.Attached:") < Index(attachBody6, "StartTapeFlight(section);"),
    "the tape flies to its SUBTOTAL slot only after the chapter command saved it");
Check(desk6.Contains("calcModel = Level3CalculatorModel.Build(holder, body, props);") && desk6.Contains("Level3DeskCalculator.Instance.Calculator") &&
      desk6.Contains("hit.collider.transform.IsChildOf(calcModel.Root) ? hit.collider : null") && desk6.Contains("if (CalculatorUp && calcModel.TryKey(target, out Level3CalcKey key))"),
    "the table calculator is the supplied Synty body with the shared logic; its keys share the board's single input owner and enter on release");
string folder6 = Src("Scripts/UI/HUD/HudHotbar.cs");
Check(folder6.Contains("KeepReferencePages(pages, InventoryCatalogue.BeaSummary") && folder6.Contains("Level3WorksheetCheck.ReferenceLines(chapter, definition)") &&
      folder6.Contains("Level3WorksheetCheck.BoardPage(chapter) ? \"Checked worksheet\" : Level3WorksheetCheck.SummaryTitle"),
    "the separate summary keeps its read-only page (a board-collected worksheet retains its historical layout)");
foreach (string file in Directory.GetFiles(Path.Combine(root, "Assets/Scripts/Level3"), "*.cs"))
{
    string fileName6 = Path.GetFileName(file);
    if (fileName6 == "Level3WorksheetCheck.cs" || fileName6 == "Level3ContentValidator.cs") continue;
    string code6 = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(file), @"///.*|//.*", "");
    Check(!code6.Contains("worksheetDecisions") && !code6.Contains("expectedFields"), $"{fileName6}: never reads the answer key (only Level3WorksheetCheck grades)");
}
string model6 = Src("Scripts/Level3/Level3WorksheetModel.cs");
Check(model6.Contains("\"CHANGED · RECALCULATE\"") && model6.Contains("props.Box(\"Clip\", tape") && model6.Contains("\"CHECKED\"") && model6.Contains("BuildPencil();") &&
      System.Text.RegularExpressions.Regex.Matches(model6, @"props\.(Box|Shape|Frame)\(").Count >= 40,
    "slots, clipped tapes, stale tags, marks, receipt, stamp and pencil are real 3D bodies with thickness, not overlays");
Console.WriteLine($"T06 checks: {checks - t06Start}, failures {fails - t06Fails} (tapes, completeness, 12 decisions, freeze, correction, collection/claims/mastery, failure, resume, wiring; Unity rendering, touch and feel not run).");

// =====================================================================
// T07: Bea's taxable income and graduated tax on the office computation sheet
// =====================================================================
int t07Start = checks, t07Fails = fails;
const string B7 = "L3-BEA-001";
string Fs7(string f) => Level3ComputationRules.FieldSource(B7, f);
string Ss7(string f) => Level3ComputationRules.SheetSource(B7, f);
Level3RuleDefinition Band7(string id) => Level3ComputationRules.FindRule(bea, id);
void Total7(Level3Calculator c, string field, Level3ChapterCheckpoint s)
{
    Level3ComputationRecord r = Level3ComputationRules.Record(s, B7);
    c.SelectSource(Fs7(field), field == "gross" ? "Gross income" : "Expenses", field == "gross" ? r.grossAmount : r.expensesAmount);
}
void Const7(Level3Calculator c, Level3ChapterCheckpoint s, string part)
{
    Level3RuleDefinition r = Band7(Level3ComputationRules.Record(s, B7).ruleId);
    string sid = Level3ComputationRules.RuleSource(r.ruleId, part);
    if (part == "rate") c.SelectSourcePercent(sid, "Rate", r.rate);
    else c.SelectSource(sid, part == "lower" ? "Floor" : "Base tax", part == "lower" ? r.lower : r.baseTax);
}
void Sheet7(Level3Calculator c, Level3ChapterCheckpoint s, string field) =>
    c.SelectSource(Ss7(field), Level3ComputationRules.SectionLabel(field), Level3ComputationRules.TapeOf(s, B7, field).result);
Level3CommandOutcome Put7(Level3ChapterCheckpoint s, string field, Level3Calculator c) =>
    Level3ComputationRules.AttachTape(s, bea, field, c.DraftChain, c.DraftResult, out _);
Level3ChapterCheckpoint Sheet7Base(string gross = "worksheet:income", string expenses = "worksheet:expenses", string rule = "IND-20")
{
    Level3ChapterCheckpoint s = acq.Copy();
    Level3ComputationRules.SelectTotal(s, bea, "gross", gross, out _);
    Level3ComputationRules.SelectTotal(s, bea, "expenses", expenses, out _);
    var c = new Level3Calculator();
    Total7(c, "gross", s); c.Press(Level3CalcKey.Subtract); Total7(c, "expenses", s); c.Press(Level3CalcKey.Equals);
    Put7(s, "taxable-income", c);
    if (rule != null) Level3ComputationRules.SelectRule(s, bea, rule, out _);
    return s;
}
Level3ComputationFacts Facts7(Level3ChapterCheckpoint s) => Level3ComputationRules.Evaluate(s, bea, B7);

// ---------- choices: explicit, alike, from the collected worksheet ----------
List<Level3TotalChoice> choices7 = Level3ComputationRules.TotalChoices(acq, bea);
Check(choices7.Select(c => c.ChoiceId).SequenceEqual(new[] { "worksheet:income", "worksheet:expenses", "paper:B-DRAFT/BDraftTotal" }) &&
      choices7.Select(c => c.Amount).SequenceEqual(new[] { "900000", "300000", "354000.00" }) &&
      choices7[0].Label == "Bea's summary · Income" && choices7[1].Label == "Bea's summary · Supported deductions" && choices7[2].Label == "Bea's draft · Proposed costs",
    "the totals on offer are the collected worksheet's two exact totals (revised T06: its corrected slips, equal to a legacy worksheet's tapes) and Bea's draft total (a plausible wrong choice), the same list for both lines");
Check(Level3ComputationRules.TotalChoices(fr, bea).Count == 0 && Level3ComputationRules.Editable(acq, B7) && !Level3ComputationRules.Editable(fr, B7) &&
      !Level3ComputationRules.Editable(acq, "L3-MARINE-001") && !Level3ComputationRules.Editable(handed, B7),
    "the sheet opens only once the worksheet is collected (BeaComputation), for Bea's case only");
Level3ChapterCheckpoint pick7 = acq.Copy();
string acqSnap7 = Snap(acq);
Check(Level3ComputationRules.SelectTotal(pick7, bea, "gross", "worksheet:income", out string pickWhy) == Level3CommandOutcome.Applied && pickWhy == null &&
      Level3ComputationRules.Record(pick7, B7).grossChoice == "worksheet:income" && Level3ComputationRules.Record(pick7, B7).grossAmount == "900000" &&
      Level3ComputationRules.Record(pick7, B7).revision == 1 && Snap(acq) == acqSnap7,
    "picking Gross income copies the worksheet's Income subtotal (900,000) on the candidate only; nothing was filled in before");
Check(Level3ComputationRules.SelectTotal(pick7, bea, "gross", "worksheet:income", out string samePick) == Level3CommandOutcome.Duplicate && samePick == "already chosen" &&
      Level3ComputationRules.SelectTotal(pick7.Copy(), bea, "gross", "paper:B-REV/BRevenueTotal", out _) == Level3CommandOutcome.Refused &&
      Level3ComputationRules.SelectTotal(pick7.Copy(), bea, "taxable-income", "worksheet:income", out _) == Level3CommandOutcome.Refused &&
      Level3ComputationRules.SelectTotal(fr.Copy(), bea, "gross", "worksheet:income", out _) == Level3CommandOutcome.Refused,
    "a repeat is quiet; an unknown total, a tape line or a sheet that is not open yet is refused");
Check(Level3ComputationRules.SelectTotal(pick7.Copy(), bea, "gross", "paper:B-DRAFT/BDraftTotal", out _) == Level3CommandOutcome.Applied &&
      Level3ComputationRules.SelectTotal(pick7.Copy(), bea, "expenses", "worksheet:income", out _) == Level3CommandOutcome.Applied,
    "a plausible wrong total is accepted like any other (draft work until the review)");
Check(Level3ComputationRules.Rules(bea).Select(r => r.ruleId).SequenceEqual(new[] { "IND-0", "IND-15", "IND-20", "IND-25", "IND-30", "IND-35" }) &&
      Level3ComputationRules.SelectRule(pick7.Copy(), bea, "IND-99", out _) == Level3CommandOutcome.Refused &&
      Level3ComputationRules.SelectRule(pick7.Copy(), bea, "IND-25", out _) == Level3CommandOutcome.Applied && Level3ComputationRules.Record(acq, B7).ruleId == "",
    "the band is chosen from the content's six-row annual table; nothing picks it automatically");

// ---------- the annual table, its boundaries and the cents policy (FIXTURE_CONTRACT) ----------
var bands7 = new (string at, string below, string atBand, string above)[]
{
    ("250000", "IND-0", "IND-0", "IND-15"), ("400000", "IND-15", "IND-15", "IND-20"), ("800000", "IND-20", "IND-20", "IND-25"),
    ("2000000", "IND-25", "IND-25", "IND-30"), ("8000000", "IND-30", "IND-30", "IND-35")
};
foreach (var (at, below, atBand, above) in bands7)
{
    decimal b = decimal.Parse(at);
    Check(Level3ComputationRules.ContainingRule(bea, b - 0.01m)?.ruleId == below && Level3ComputationRules.ContainingRule(bea, b)?.ruleId == atBand &&
          Level3ComputationRules.ContainingRule(bea, b + 0.01m)?.ruleId == above,
        $"boundary {at}: -0.01 / exact stay in the ending band, +0.01 moves up (upper inclusive, lower exclusive)");
}
var raw7 = new (decimal taxable, decimal expected)[]
{
    (249999.99m, 0m), (250000m, 0m), (250000.01m, 0.0015m), (399999.99m, 22499.9985m), (400000m, 22500m), (400000.01m, 22500.0020m),
    (799999.99m, 102499.9980m), (800000m, 102500m), (800000.01m, 102500.0025m), (1999999.99m, 402499.9975m), (2000000m, 402500m),
    (2000000.01m, 402500.0030m), (7999999.99m, 2202499.9970m), (8000000m, 2202500m), (8000000.01m, 2202500.0035m)
};
foreach (var (taxable, expected) in raw7)
    Check(Level3ComputationRules.TaxDueExact(Level3ComputationRules.ContainingRule(bea, taxable), taxable) == expected, $"exact raw tax at {taxable} is {expected}");
Check(Level3ComputationRules.ContainingRule(bea, 0m)?.ruleId == "IND-0" && Level3ComputationRules.ContainingRule(bea, -0.01m) == null &&
      Level3ComputationRules.ContainingRule(bea, 600000m)?.ruleId == "IND-20" && Level3ComputationRules.TaxDueExact(Band7("IND-20"), 600000m) == 62500m,
    "zero is the zero band, a negative amount has no band (never clamped), and Bea's 600,000 is 22,500 + 20% x 200,000 = 62,500");
Check(new[] { (1.004m, 1.00m), (1.005m, 1.01m), (1.006m, 1.01m), (1.49m, 1.49m), (1.50m, 1.50m), (1.51m, 1.51m) }.All(p => Level3ComputationRules.RoundCents(p.Item1) == p.Item2) &&
      Level3ComputationRules.RoundCents(Level3ComputationRules.TaxDueExact(Band7("IND-15"), 250000.10m)) == 0.02m &&
      Level3ComputationRules.RoundCents(Level3ComputationRules.TaxDueExact(Band7("IND-15"), 250000.03m)) == 0.00m &&
      Level3ComputationRules.RoundCents(Level3ComputationRules.TaxDueExact(Band7("IND-20"), 400000.03m)) == 22500.01m,
    "cents policy: only the finished tax due rounds, half away from zero (0.015 -> 0.02, 0.0045 -> 0.00, 22500.006 -> 22500.01)");
Check(Level3ComputationRules.BandLabel(Band7("IND-0")) == "Up to 250,000" && Level3ComputationRules.BandFormula(Band7("IND-0")) == "no tax" &&
      Level3ComputationRules.BandFormula(Band7("IND-15")) == "15% of the excess over 250,000" &&
      Level3ComputationRules.BandLabel(Band7("IND-20")) == "Over 400,000 to 800,000" && Level3ComputationRules.BandFormula(Band7("IND-20")) == "22,500 + 20% of the excess over 400,000" &&
      Level3ComputationRules.BandLabel(Band7("IND-35")) == "Over 8,000,000",
    "the chooser prints each row as the book does: its range and its base-plus-excess formula");

// ---------- the calculator chain (continuing from a result keeps its working) ----------
var chain7 = new Level3Calculator();
Keys(chain7, "100-20=");
Keys(chain7, "-8=");
Check(chain7.DraftResult == "72" && chain7.DraftExpression.Count == 3 && chain7.DraftExpression[0].sourceId == Level3Calculator.PreviousResultSource &&
      chain7.DraftChain.Count == 6 && chain7.DraftChain[3].op == Level3Calculator.ChainStep &&
      Level3Calculator.EvaluateChain(chain7.DraftChain, out decimal chainValue) == null && chainValue == 72m,
    "continuing after = keeps the whole working as one chain (100 - 20 = - 8 = 72); the last step alone stays the T03/T06 draft");
Check(Level3Calculator.ChainLines(chain7.DraftChain).SequenceEqual(new[] { "100.00", "− 20.00", "= 80.00", "− 8.00", "= 72.00" }),
    "a chain prints each step with its own result line");
var fresh7 = new Level3Calculator();
Keys(fresh7, "5+5=");
Keys(fresh7, "C3*3=");
Check(fresh7.DraftChain.Count == 3 && fresh7.DraftChain.All(t => t.op != Level3Calculator.ChainStep), "after C a new calculation starts a new chain");
var pct7 = new Level3Calculator();
pct7.SelectSourcePercent("rule:IND-20/rate", "Rate", "0.20");
Check(pct7.DisplayText == "20.00%" && pct7.Caption == "from Rate", "the band's rate goes on the calculator as 20% from its source, without typing");
Level3ChapterCheckpoint practiceChain = assigned.Copy();
var pc7 = new Level3Calculator();
pc7.SelectSource(Level3PracticeRules.BudgetSource, "Budget", "100.00"); pc7.Press(Level3CalcKey.Subtract);
pc7.SelectSource(Level3PracticeRules.PaperSource, "Paper order", "20.00"); pc7.Press(Level3CalcKey.Equals);
Check(pc7.DraftResult == "80" && pc7.DraftExpression.Count == 3 && Snap(pc7.DraftExpression) == Snap(attach1.tapes[0].expression) &&
      Level3PracticeRules.AttachTape(practiceChain, Level3PracticeRules.FirstSlot, pc7.DraftExpression, pc7.DraftResult, out _) == Level3CommandOutcome.Refused,
    "the calculator still prints the legacy practice draft unchanged, but a revised chapter never attaches it");

// ---------- the real chain to 600,000 and 62,500 ----------
Level3ChapterCheckpoint s7 = acq.Copy();
Level3ComputationRules.SelectTotal(s7, bea, "gross", "worksheet:income", out _);
Level3ComputationRules.SelectTotal(s7, bea, "expenses", "worksheet:expenses", out _);
var c7 = new Level3Calculator();
Total7(c7, "gross", s7); c7.Press(Level3CalcKey.Subtract); Total7(c7, "expenses", s7); c7.Press(Level3CalcKey.Equals);
Check(c7.DraftResult == "600000" && Level3Calculator.ChainLines(c7.DraftChain).SequenceEqual(new[] { "900,000.00 GROSS", "− 300,000.00 EXPENSES", "= 600,000.00" }),
    "gross income minus allowable expenses prints 900,000.00 GROSS / - 300,000.00 EXPENSES / = 600,000.00");
Check(Put7(s7, "taxable-income", c7) == Level3CommandOutcome.Applied && Level3ComputationRules.IsCurrent(s7, bea, B7, "taxable-income") &&
      s7.Tape("bea/taxable-income").result == "600000" && s7.Tape("bea/taxable-income").caseId == B7 && s7.Tape("bea/taxable-income").expression.Count(t => t.IsNumber && !t.manual) == 2,
    "the taxable income tape attaches with its own sources (both picked totals) and exact result");
Level3ComputationRules.SelectRule(s7, bea, "IND-20", out _);
var e7 = new Level3Calculator();
Sheet7(e7, s7, "taxable-income"); e7.Press(Level3CalcKey.Subtract); Const7(e7, s7, "lower"); e7.Press(Level3CalcKey.Equals);
Check(e7.DraftResult == "200000" && Put7(s7, "excess", e7) == Level3CommandOutcome.Applied, "excess: taxable income - the band's floor = 200,000, attached");
var m7 = new Level3Calculator();
Sheet7(m7, s7, "excess"); m7.Press(Level3CalcKey.Multiply); Const7(m7, s7, "rate"); m7.Press(Level3CalcKey.Equals);
Check(m7.DraftResult == "40000" && Put7(s7, "marginal-tax", m7) == Level3CommandOutcome.Applied, "tax on the excess: 200,000 x 20% = 40,000, attached");
var d7 = new Level3Calculator();
Sheet7(d7, s7, "marginal-tax"); d7.Press(Level3CalcKey.Add); Const7(d7, s7, "base"); d7.Press(Level3CalcKey.Equals);
Check(d7.DraftResult == "62500" && Put7(s7, "tax-due", d7) == Level3CommandOutcome.Applied && Level3ComputationRules.Record(s7, B7).taxDueNoted,
    "tax due: 40,000 + base 22,500 = 62,500, attached; taxable income and tax due are now both on the sheet (TaxDueAttached once)");
Level3ComputationFacts f7 = Facts7(s7);
// T08 changed this pin: the facts now end with the credit and the remaining payable (not linked yet here).
Check(f7.TaxDueSupported && f7.Steps.Take(7).All(s => s.Supported) && f7.Step("taxable-income").Value == "600000" && f7.Step("tax-due").Value == "62500" &&
      f7.Step("rule").Value == "IND-20" &&
      f7.Steps.Select(s => s.Field).SequenceEqual(new[] { "gross", "expenses", "taxable-income", "rule", "excess", "marginal-tax", "tax-due", "credits", "remaining-payable" }) &&
      !f7.Step("credits").Present && !f7.Step("remaining-payable").Present && !f7.PayableSupported && !f7.Complete,
    "the review facts: every T07 step supported, taxable 600,000, band IND-20, tax due 62,500; credit and balance not there yet");
Check(Level3ComputationRules.AttachTape(s7.Copy(), bea, "tax-due", d7.DraftChain, d7.DraftResult, out string dupTape) == Level3CommandOutcome.Duplicate && dupTape == "already attached",
    "the same current tape again is a quiet duplicate");

Level3ChapterCheckpoint one7 = Sheet7Base();
var oc7 = new Level3Calculator();
Sheet7(oc7, one7, "taxable-income"); oc7.Press(Level3CalcKey.Subtract); Const7(oc7, one7, "lower"); oc7.Press(Level3CalcKey.Equals);
oc7.Press(Level3CalcKey.Multiply); Const7(oc7, one7, "rate"); oc7.Press(Level3CalcKey.Equals);
oc7.Press(Level3CalcKey.Add); Const7(oc7, one7, "base"); oc7.Press(Level3CalcKey.Equals);
Check(oc7.DraftResult == "62500" && Level3Calculator.ChainLines(oc7.DraftChain).SequenceEqual(new[]
          { "600,000.00 TAXABLE", "− 400,000.00 FLOOR", "= 200,000.00", "× 20.00% RATE", "= 40,000.00", "+ 22,500.00 BASE", "= 62,500.00" }) &&
      Put7(one7, "tax-due", oc7) == Level3CommandOutcome.Applied && Facts7(one7).TaxDueSupported,
    "the ticket's own chain on one tape (600,000 - 400,000 = 200,000, x 20% = 40,000, + 22,500 = 62,500) is supported without the working lines");
Level3ChapterCheckpoint fields7 = Sheet7Base();
var fc7 = new Level3Calculator();
Total7(fc7, "gross", fields7); fc7.Press(Level3CalcKey.Subtract); Total7(fc7, "expenses", fields7); fc7.Press(Level3CalcKey.Equals);
fc7.Press(Level3CalcKey.Subtract); Const7(fc7, fields7, "lower"); fc7.Press(Level3CalcKey.Equals);
fc7.Press(Level3CalcKey.Multiply); Const7(fc7, fields7, "rate"); fc7.Press(Level3CalcKey.Equals);
fc7.Press(Level3CalcKey.Add); Const7(fc7, fields7, "base"); fc7.Press(Level3CalcKey.Equals);
Check(Put7(fields7, "tax-due", fc7) == Level3CommandOutcome.Applied && Facts7(fields7).TaxDueSupported, "a chain straight from the two totals is an equivalent supported chain");
Level3ChapterCheckpoint mixed7 = Sheet7Base();
var xc7 = new Level3Calculator();
Const7(xc7, mixed7, "base"); xc7.Press(Level3CalcKey.Add); Sheet7(xc7, mixed7, "taxable-income"); xc7.Press(Level3CalcKey.Multiply); Const7(xc7, mixed7, "rate");
xc7.Press(Level3CalcKey.Subtract); Const7(xc7, mixed7, "lower"); xc7.Press(Level3CalcKey.Multiply); Const7(xc7, mixed7, "rate"); xc7.Press(Level3CalcKey.Equals);
Check(xc7.DraftResult == "62500" && Put7(mixed7, "tax-due", xc7) == Level3CommandOutcome.Applied && Facts7(mixed7).TaxDueSupported,
    "an equivalent combined expression (22,500 + 600,000 x 20% - 400,000 x 20%) is supported too");

// ---------- wrong work stays draft work, never supported ----------
(bool follows, bool supported, string value) Due7(Level3ChapterCheckpoint s, Action<Level3Calculator> build)
{
    var c = new Level3Calculator();
    build(c);
    Check(Put7(s, "tax-due", c) == Level3CommandOutcome.Applied, "a plausible wrong tax due is still accepted as draft work");
    Level3ComputationFact due = Facts7(s).Step("tax-due");
    return (due.Follows, due.Supported, due.Value);
}
Level3ChapterCheckpoint flat7 = Sheet7Base();
var flat = Due7(flat7, c => { Sheet7(c, flat7, "taxable-income"); c.Press(Level3CalcKey.Multiply); Const7(c, flat7, "rate"); c.Press(Level3CalcKey.Equals); });
Check(!flat.follows && !flat.supported && flat.value == "120000", "bea-flat: 20% of all taxable income (120,000) does not follow the band's formula");
Level3ChapterCheckpoint whole7 = Sheet7Base();
var whole = Due7(whole7, c => { Const7(c, whole7, "base"); c.Press(Level3CalcKey.Add); Sheet7(c, whole7, "taxable-income"); c.Press(Level3CalcKey.Multiply); Const7(c, whole7, "rate"); c.Press(Level3CalcKey.Equals); });
Check(!whole.follows && !whole.supported && whole.value == "142500", "base plus 20% of the WHOLE base (142,500) cannot approve");
Level3ChapterCheckpoint minusBase7 = Sheet7Base();
var minusBase = Due7(minusBase7, c => { Sheet7(c, minusBase7, "taxable-income"); c.Press(Level3CalcKey.Subtract); Const7(c, minusBase7, "lower"); c.Press(Level3CalcKey.Equals);
    c.Press(Level3CalcKey.Multiply); Const7(c, minusBase7, "rate"); c.Press(Level3CalcKey.Equals); c.Press(Level3CalcKey.Subtract); Const7(c, minusBase7, "base"); c.Press(Level3CalcKey.Equals); });
Check(!minusBase.follows && !minusBase.supported && minusBase.value == "17500",
    "every constant used but the base taken away (40,000 - 22,500 = 17,500): the formula check rejects it");
Level3ChapterCheckpoint swapped7 = Sheet7Base();
var swapped = Due7(swapped7, c => { Sheet7(c, swapped7, "taxable-income"); c.Press(Level3CalcKey.Multiply); Const7(c, swapped7, "rate"); c.Press(Level3CalcKey.Add);
    Const7(c, swapped7, "base"); c.Press(Level3CalcKey.Subtract); Const7(c, swapped7, "lower"); c.Press(Level3CalcKey.Equals); });
Check(!swapped.follows && !swapped.supported && swapped.value == "-257500",
    "every constant used but the floor taken from the tax (600,000 x 20% + 22,500 - 400,000): negative, kept visible, never supported");
Level3ChapterCheckpoint typed7 = Sheet7Base();
var typedDue = Due7(typed7, c => { Keys(c, "22500+40000="); });
Check(!typedDue.follows && !typedDue.supported && typedDue.value == "62500" && Facts7(typed7).Step("tax-due").Typed,
    "the right number typed by hand (22,500 + 40,000 = 62,500) is kept but supports nothing (no source link)");
Level3ChapterCheckpoint band25 = Sheet7Base(rule: "IND-25");
var b25 = Due7(band25, c => { Sheet7(c, band25, "taxable-income"); c.Press(Level3CalcKey.Subtract); Const7(c, band25, "lower"); c.Press(Level3CalcKey.Equals);
    c.Press(Level3CalcKey.Multiply); Const7(c, band25, "rate"); c.Press(Level3CalcKey.Equals); c.Press(Level3CalcKey.Add); Const7(c, band25, "base"); c.Press(Level3CalcKey.Equals); });
Level3ComputationFacts f25 = Facts7(band25);
Check(b25.follows && !b25.supported && b25.value == "52500" && !f25.Step("rule").Follows && f25.Step("rule").Root == "rule-selection",
    "the wrong band's formula done right (52,500) follows its own band but the rule selection is the root: not supported");
Level3ChapterCheckpoint draft7 = Sheet7Base(expenses: "paper:B-DRAFT/BDraftTotal");
var bd = Due7(draft7, c => { Sheet7(c, draft7, "taxable-income"); c.Press(Level3CalcKey.Subtract); Const7(c, draft7, "lower"); c.Press(Level3CalcKey.Equals);
    c.Press(Level3CalcKey.Multiply); Const7(c, draft7, "rate"); c.Press(Level3CalcKey.Equals); c.Press(Level3CalcKey.Add); Const7(c, draft7, "base"); c.Press(Level3CalcKey.Equals); });
Level3ComputationFacts fd = Facts7(draft7);
Check(fd.Step("taxable-income").Value == "546000" && bd.value == "51700" && fd.Step("taxable-income").Follows && !fd.Step("taxable-income").Supported &&
      !fd.Step("expenses").Supported && fd.Step("expenses").Root == "expense-sum" && bd.follows && !bd.supported,
    "bea-draft: Bea's 354,000 costs give 546,000 / 51,700; the arithmetic follows, the expense choice is the root, nothing is supported");
Level3ChapterCheckpoint rev7 = acq.Copy();
Level3ComputationRules.SelectTotal(rev7, bea, "gross", "worksheet:income", out _);
Level3ComputationRules.SelectTotal(rev7, bea, "expenses", "worksheet:expenses", out _);
var rc7 = new Level3Calculator();
Total7(rc7, "expenses", rev7); rc7.Press(Level3CalcKey.Subtract); Total7(rc7, "gross", rev7); rc7.Press(Level3CalcKey.Equals);
Check(Put7(rev7, "taxable-income", rc7) == Level3CommandOutcome.Applied && !Facts7(rev7).Step("taxable-income").Follows && Facts7(rev7).Step("taxable-income").Root == "taxable-operation" &&
      Facts7(rev7).Step("taxable-income").Value == "-600000",
    "expenses minus gross is kept (a negative -600,000, never clamped) and is a taxable-operation root");

// ---------- mechanical refusals (nothing changes) ----------
Level3ChapterCheckpoint ref7 = Sheet7Base();
string ref7Snap = Snap(ref7);
var practice7 = new Level3Calculator();
practice7.SelectSource(Level3PracticeRules.BudgetSource, "Budget", "100.00"); practice7.Press(Level3CalcKey.Equals);
var strip7 = new Level3Calculator();
strip7.SelectSource(Level3WorksheetCheck.StripSource("R1"), "Logo fees", "500000.00"); strip7.Press(Level3CalcKey.Equals);
var other7 = new Level3Calculator();
other7.SelectSource("rule:IND-25/lower", "Floor", "800000.00"); other7.Press(Level3CalcKey.Equals);
var later7 = new Level3Calculator();
later7.SelectSource(Ss7("excess"), "Excess", "200000"); later7.Press(Level3CalcKey.Equals);
var changed7 = new Level3Calculator();
changed7.SelectSource(Fs7("gross"), "Gross income", "900001"); changed7.Press(Level3CalcKey.Equals);
Check(Level3ComputationRules.AttachTape(ref7, bea, "excess", practice7.DraftChain, practice7.DraftResult, out string why7a) == Level3CommandOutcome.Refused && why7a == Level3ComputationRules.RefuseForeign &&
      Level3ComputationRules.AttachTape(ref7, bea, "excess", strip7.DraftChain, strip7.DraftResult, out string why7b) == Level3CommandOutcome.Refused && why7b == Level3ComputationRules.RefuseForeign &&
      Level3ComputationRules.AttachTape(ref7, bea, "excess", other7.DraftChain, other7.DraftResult, out string why7c) == Level3CommandOutcome.Refused && why7c == Level3ComputationRules.RefuseForeign &&
      Level3ComputationRules.AttachTape(ref7, bea, "taxable-income", later7.DraftChain, later7.DraftResult, out string why7d) == Level3CommandOutcome.Refused && why7d == Level3ComputationRules.RefuseLater &&
      Level3ComputationRules.AttachTape(ref7, bea, "taxable-income", changed7.DraftChain, changed7.DraftResult, out string why7e) == Level3CommandOutcome.Refused && why7e == Level3ComputationRules.RefuseChanged &&
      Level3ComputationRules.AttachTape(ref7, bea, "taxable-income", c7.DraftChain, "600001", out _) == Level3CommandOutcome.Refused &&
      Level3ComputationRules.AttachTape(ref7, bea, "rule", c7.DraftChain, c7.DraftResult, out _) == Level3CommandOutcome.Refused &&
      Level3ComputationRules.AttachTape(ref7, bea, "tax-due", new List<Level3CalcToken>(), "0", out string why7f) == Level3CommandOutcome.Refused && why7f == Level3ComputationRules.RefuseNoTape &&
      Snap(ref7) == ref7Snap,
    "practice numbers, worksheet strips, another band's constants, a later step, a changed amount, a mismatched result, a choice line or no tape: refused, nothing changes");
Check(Level3ComputationRules.AttachTape(fr.Copy(), bea, "taxable-income", c7.DraftChain, c7.DraftResult, out _) == Level3CommandOutcome.Refused,
    "no computation tape before the worksheet is collected");

// ---------- invalidation: only what depends on a change, old tapes kept ----------
Level3ChapterCheckpoint inv7 = s7.Copy();
Level3ComputationRules.SelectRule(inv7, bea, "IND-25", out _);
Check(Level3ComputationRules.IsCurrent(inv7, bea, B7, "taxable-income") && Level3ComputationRules.IsStale(inv7, bea, B7, "excess") &&
      Level3ComputationRules.IsStale(inv7, bea, B7, "marginal-tax") && Level3ComputationRules.IsStale(inv7, bea, B7, "tax-due") &&
      inv7.Tape("bea/tax-due") != null && inv7.Tape("bea/excess").result == "200000" && !Facts7(inv7).Step("tax-due").Current,
    "changing the band leaves taxable income current and marks excess, tax on the excess and tax due 'needs recalculation' (their tapes kept)");
Level3ComputationRules.SelectRule(inv7, bea, "IND-20", out _);
Check(Level3ComputationRules.IsCurrent(inv7, bea, B7, "tax-due") && Facts7(inv7).TaxDueSupported,
    "staleness follows the sources' values: back on the same band, the same tapes are current again (nothing silently rewritten)");
Level3ChapterCheckpoint inv7b = s7.Copy();
Level3ComputationRules.SelectTotal(inv7b, bea, "expenses", "paper:B-DRAFT/BDraftTotal", out _);
Check(Level3ComputationRules.IsStale(inv7b, bea, B7, "taxable-income") && Level3ComputationRules.IsStale(inv7b, bea, B7, "excess") &&
      Level3ComputationRules.IsStale(inv7b, bea, B7, "tax-due") && Level3ComputationRules.Record(inv7b, B7).grossChoice == "worksheet:income",
    "changing a total makes taxable income and everything built on it stale, through the chain; the other total is untouched");
string read7 = Snap(s7);
Level3ComputationRules.Evaluate(s7, bea, B7);
Level3ComputationRules.TotalChoices(s7, bea);
Level3ComputationRules.IsStale(s7, bea, B7, "tax-due");
Check(Snap(s7) == read7, "reading the sheet (facts, choices, staleness) changes nothing");
Level3ChapterCheckpoint redo7 = inv7b.Copy();
var redo = new Level3Calculator();
Total7(redo, "gross", redo7); redo.Press(Level3CalcKey.Subtract); Total7(redo, "expenses", redo7); redo.Press(Level3CalcKey.Equals);
Check(Put7(redo7, "taxable-income", redo) == Level3CommandOutcome.Applied && Level3ComputationRules.IsCurrent(redo7, bea, B7, "taxable-income") &&
      Level3ComputationRules.IsStale(redo7, bea, B7, "excess"),
    "a new taxable tape replaces the stale one; the steps after it still wait for their own new tapes");
var staleUse = new Level3Calculator();
Sheet7(staleUse, redo7, "excess"); staleUse.Press(Level3CalcKey.Multiply); Const7(staleUse, redo7, "rate"); staleUse.Press(Level3CalcKey.Equals);
Check(Level3ComputationRules.AttachTape(redo7.Copy(), bea, "marginal-tax", staleUse.DraftChain, staleUse.DraftResult, out string staleWhy) == Level3CommandOutcome.Refused &&
      staleWhy == Level3ComputationRules.RefuseStaleSource,
    "a tape that needs recalculating cannot be used as a source");

// ---------- persistence: round trip, old saves, failure ----------
PlayerProgressData save7 = Level3ChapterRules.WithCheckpoint(Clone(live), typed7);
save7.Normalize();
PlayerProgressData back7 = JsonSerializer.Deserialize<PlayerProgressData>(Snap(save7), json);
Check(!back7.Normalize() && Level3ChapterRules.Decide(back7.campaign.levelThree) == Level3EntryDecision.Resume &&
      Level3ComputationRules.Record(back7.campaign.levelThree, B7).ruleId == "IND-20" && Level3ComputationRules.Record(back7.campaign.levelThree, B7).grossChoice == "worksheet:income" &&
      back7.campaign.levelThree.Tape("bea/tax-due").expression.Any(t => t.manual) && Facts7(back7.campaign.levelThree).Step("tax-due").Typed &&
      Facts7(back7.campaign.levelThree).Step("taxable-income").Supported,
    "the sheet (choices, band, tapes with their sources and typed numbers) round-trips without repair and resumes at BeaComputation");
PlayerProgressData save7b = Level3ChapterRules.WithCheckpoint(Clone(live), s7);
save7b.Normalize();
PlayerProgressData back7b = JsonSerializer.Deserialize<PlayerProgressData>(Snap(save7b), json);
Check(!back7b.Normalize() && Facts7(back7b.campaign.levelThree).TaxDueSupported && WithoutChapter(back7b) == WithoutChapter(save7b) &&
      back7b.campaign.coins == live.campaign.coins && back7b.campaign.publicTrust == live.campaign.publicTrust,
    "computing changes no coins or trust and leaves every other part of the campaign as it was");
JsonNode oldT06 = JsonNode.Parse(Snap(Level3ChapterRules.WithCheckpoint(Clone(live), acq)));
foreach (JsonNode caseNode7 in oldT06["campaign"]["levelThree"]["cases"].AsArray()) caseNode7.AsObject().Remove("computation");
PlayerProgressData oldT06Back = JsonSerializer.Deserialize<PlayerProgressData>(oldT06.ToJsonString(), json);
oldT06Back.Normalize();
Check(Level3ComputationRules.Record(oldT06Back.campaign.levelThree, B7) != null && Level3ComputationRules.Record(oldT06Back.campaign.levelThree, B7).revision == 0 &&
      Level3ChapterRules.Decide(oldT06Back.campaign.levelThree) == Level3EntryDecision.Resume,
    "a T06 save without a computation sheet resumes with an empty sheet");
string sheetSnap7 = Snap(Level3ComputationRules.Record(s7, B7));
foreach (string leak in new[] { "expected", "taxDue\"", "62500", "600000", "taxableIncome", "level3" })
    Check(!sheetSnap7.Contains(leak), $"the saved sheet record holds only the player's choices ('{leak}')");
string sandbox7 = Path.Combine(Path.GetTempPath(), "TaxVentureL3T07-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox7);
try
{
    string Encode7(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var store7 = new SaveFileStore(Path.Combine(sandbox7, "progress.json"));
    PlayerProgressData pub7 = Level3ChapterRules.WithCheckpoint(Clone(live), one7.Copy());
    pub7.Normalize();
    store7.Write(Encode7(pub7));
    byte[] disk7 = File.ReadAllBytes(store7.FilePath);
    string pub7Snap = Snap(pub7);
    Level3ChapterCheckpoint cand7 = pub7.campaign.levelThree.Copy();
    Level3ComputationRules.SelectRule(cand7, bea, "IND-25", out _);
    Directory.CreateDirectory(store7.TempPath);
    bool wrote7 = false;
    try { PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub7), cand7); next.Normalize(); store7.Write(Encode7(next)); pub7 = next; wrote7 = true; }
    catch (Exception) { }
    Check(!wrote7 && Snap(pub7) == pub7Snap && File.ReadAllBytes(store7.FilePath).SequenceEqual(disk7) &&
          Level3ComputationRules.Record(pub7.campaign.levelThree, B7).ruleId == "IND-20" && Level3ComputationRules.IsCurrent(pub7.campaign.levelThree, bea, B7, "tax-due"),
        "a failed write of a sheet change keeps the saved band and tapes on disk and live (the change can be repeated)");
    Directory.Delete(store7.TempPath);
}
finally
{
    try { Directory.Delete(sandbox7, true); } catch (IOException) { }
}

// ---------- resume observers, game state, guard ----------
Check(Level3ChapterRules.ImpliedTriggers(s7).Contains(Level3ChapterRules.TaxDueTrigger) && !Level3ChapterRules.ImpliedTriggers(acq).Contains(Level3ChapterRules.TaxDueTrigger),
    "resume: tax due on the sheet keeps its objective ticked; an untouched sheet does not");
Check(Level3ChapterRules.ImpliedLessons(s7).Contains(Level3ChapterRules.ComputationTaxLesson) && Level3ChapterRules.ImpliedLessons(Sheet7Base(rule: null)).Contains(Level3ChapterRules.ComputationTaxableLesson) &&
      !Level3ChapterRules.ImpliedLessons(Sheet7Base(rule: null)).Contains(Level3ChapterRules.ComputationRuleLesson) && !Level3ChapterRules.ImpliedLessons(acq).Contains(Level3ChapterRules.ComputationIntroLesson),
    "resume: each computation lesson already taught by the saved sheet is skipped; the next one still teaches");
Check(Level3ChapterRules.CoarseState(Level3Phase.BeaComputation) == "ComputeTaxesState" && Level3ChapterRules.CoarseState(Level3Phase.BeaReview) == null &&
      Type.GetType("ComputeTaxesState, Assembly-CSharp") != null,
    "Bea's computation shows as Compute Taxes on the game state");
Check(!Level3ChapterRules.TransitionAllowed(Level3Phase.BeaComputation, "StampAssessmentState") && !Level3ChapterRules.TransitionAllowed(Level3Phase.BeaComputation, "AnalyzeEvidenceState") &&
      Level3ChapterRules.TransitionAllowed(Level3Phase.WorksheetDraft, "StampAssessmentState") && !Level3ChapterRules.TransitionAllowed(Level3Phase.BeaComputation, "LevelCompleteState") &&
      Level3ChapterRules.TransitionAllowed(Level3Phase.BeaComputation, "ComputeTaxesState") &&
      Level3ChapterRules.ComputationForeignStates.All(n => Type.GetType(n + ", Assembly-CSharp") != null),
    "while the computation is open the inherited stamp set and corkboard cannot move the game state on; the legacy completion stays refused");

// ---------- content: book, objective, guide ----------
var book7 = Json("Levels/Level_03/book.json").GetProperty("sections").EnumerateArray().ToList();
Check(book7.Count >= 5 && book7[2].GetProperty("heading").GetString() == "Section 6 – Taxable Income" &&
      book7[3].GetProperty("heading").GetString() == "Section 7 – Annual bands (2023 onwards)" && book7[4].GetProperty("heading").GetString() == "Section 8 – Base Plus Excess" &&
      book7.All(s => s.GetProperty("body").GetString().Split("\n\n").Length == s.GetProperty("clickableValues").GetArrayLength()),
    "book.json adds Taxable Income, the year-labelled annual table and Base Plus Excess, one paragraph per read-only term");
string table7 = book7[3].GetProperty("body").GetString();
Check(Level3ComputationRules.Rules(bea).All(r => r.baseTax == "0.00" || table7.Contains(r.baseTax.Replace(".00", "").Length > 3 ? decimal.Parse(r.baseTax).ToString("#,0") : r.baseTax)) &&
      table7.Contains("22,500 plus 20% of the excess over 400,000") && table7.Contains("2,202,500 plus 35% of the excess over 8,000,000") &&
      book7[3].GetProperty("clickableValues").EnumerateArray().Take(6).Select(v => v.GetProperty("displayText").GetString())
          .SequenceEqual(Level3ComputationRules.Rules(bea).Select(Level3ComputationRules.BandLabel)),
    "the book's table matches the content's six bands row for row (the chooser prints the same rows)");
string later7Text = string.Join(" ", book7.Skip(2).Select(s => s.GetProperty("body").GetString() + string.Join(" ", s.GetProperty("clickableValues").EnumerateArray().Select(v => v.GetProperty("displayText").GetString()))));
foreach (string leak in new[] { "600,000", "200,000", "40,000", "62,500", "900,000", "300,000", "Bea" })
    Check(!later7Text.Contains(leak), $"the computation leaves hold no client figure ('{leak}'); the worked example is Practice Design Studio");
Check(later7Text.Contains("350,000") && later7Text.Contains("15,000 tax due"), "Section 8's worked example is the separate Practice Design Studio (350,000 -> 15,000)");
var steps7 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps7.Count >= 6 && steps7[5].GetProperty("trigger").GetString() == Level3ChapterRules.TaxDueTrigger && ObjectiveTriggers.Validate(Level3ChapterRules.TaxDueTrigger) == null &&
      ObjectiveTriggers.Events.Contains(ObjectiveTriggers.TaxDueAttached),
    "objective 6 (revision; was 7): work out Bea's taxable income and tax due (a known event, raised from a saved, structural state)");
GuideScript guide7 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var t07Lessons = new[] { Level3ChapterRules.ComputationIntroLesson, Level3ChapterRules.ComputationTaxableLesson,
    Level3ChapterRules.ComputationRuleLesson, Level3ChapterRules.ComputationTaxLesson }.Select(id => guide7.lessons.FirstOrDefault(l => l.id == id)).ToList();
Check(guide7.Validate() == "" && t07Lessons.All(l => l != null && l.pages.All(p => p.IsHint)) &&
      t07Lessons[1].pages[0].waitFor == "flag:computationTaxable|cue:" + Level3DeskCalculator.CueSheetTape &&
      t07Lessons[2].pages[0].waitFor == "flag:computationRule|cue:" + Level3DeskCalculator.CueRuleChosen && t07Lessons[3].pages[0].waitFor == "flag:computationTaxDue",
    "the computation lessons are hints that wait for the player's own picks and tapes (no always-true wait, no doubled line)");
Check(!t07Lessons.SelectMany(l => l.pages).Any(p => p.text.Contains("IND-") || p.text.Contains("400,000") || p.text.Contains("600,000") || p.text.Contains("62,500") ||
      p.text.Contains("20%") || p.text.Contains("click", StringComparison.OrdinalIgnoreCase)),
    "Arthur names no band, floor, rate or value (the book carries the method) and speaks touch only");

// ---------- wiring ----------
string cp7 = Src("Scripts/Level3/Level3Checkpointer.cs");
string commit7 = Body6(cp7, "public TapeCommit CommitComputation(", "/// <summary>One worksheet command");
Check(Index(commit7, "State.Copy()") < Index(commit7, "edit(candidate, out string reason)") && Index(commit7, "edit(candidate, out string reason)") < Index(commit7, "CommitLevelThreeCheckpoint(candidate)") &&
      Index(commit7, "CommitLevelThreeCheckpoint(candidate)") < Index(commit7, "Publish(candidate);") && commit7.Contains("return TapeCommit.SaveFailed;"),
    "every sheet change: candidate, signed write, then publish; a failed write returns before publishing");
Check(cp7.Contains("GameplayEvents.RaiseTaxDueAttached();") && cp7.Contains("machine.ChangeState(new ComputeTaxesState());") &&
      cp7.Contains("Level3ChapterRules.TransitionAllowed(Phase, next.GetType().Name)"),
    "the tax-due objective is raised once from a published change; the game state enters Compute Taxes; the guard is phase-aware");
string dc7 = Src("Scripts/Level3/Level3DeskCalculatorComputation.cs");
string attachLeaf7 = Body6(dc7, "private void AttachLeaf(string field)", "private void LeafPrinted()");
Check(Index(attachLeaf7, "owner.CommitComputation(") < Index(attachLeaf7, "StartAttachFlight(") && Index(attachLeaf7, "case Level3Checkpointer.TapeCommit.Attached:") < Index(attachLeaf7, "StartAttachFlight(") &&
      dc7.Contains("Level3ComputationRules.SelectTotal(c, definition, field, id, out r)") && dc7.Contains("Level3ComputationRules.SelectRule(c, definition, id, out r)") &&
      !dc7.Contains("CommitLevelThreeCheckpoint"),
    "the desk sends picks and tapes through the chapter command and flies a tape only after it saved");
string desk7 = Src("Scripts/Level3/Level3DeskCalculator.cs");
Check(desk7.Contains("public sealed partial class Level3DeskCalculator") && desk7.Contains("bool visible = LessonOpen || ((phase == Level3Phase.ToolPractice || earlierTapes) && phase < Level3Phase.BeaComputation);") &&
      desk7.Contains("PickTool(leaf.Root, top, ray, ref nearest, ref distance)") && desk7.Contains("if (ActivateLeaf(target)) return;") && desk7.Contains("calculator.SetTape(DraftLines(), animate: true, TapeFeedSeconds);"),
    "from Bea's computation her sheet replaces the practice card on the one desk input owner; the tape prints the whole chain");
foreach (string file in new[] { "Level3ComputationRules.cs", "Level3ComputationLeafModel.cs", "Level3DeskCalculatorComputation.cs" })
{
    string code7 = System.Text.RegularExpressions.Regex.Replace(Src("Scripts/Level3/" + file), @"///.*|//.*", "");
    Check(!code7.Contains("TaxComputationCalculator") && !code7.Contains("float.Parse") && !code7.Contains("double") && !code7.Contains("coins") &&
          !code7.Contains("publicTrust") && !code7.Contains("expectedFields") && !code7.Contains("worksheetDecisions"),
        $"{file}: exact decimals only, no Level 1 flat-rate route, no wallet/trust, no answer key");
}
string leaf7 = Src("Scripts/Level3/Level3ComputationLeafModel.cs");
Check(leaf7.Contains("\"NEEDS RECALCULATION\"") && leaf7.Contains("\"TYPED · NO SOURCE\"") && leaf7.Contains("props.Box(\"Clip\", tape") &&
      System.Text.RegularExpressions.Regex.Matches(leaf7, @"props\.(Box|Frame|Print)\(").Count >= 30 && !leaf7.Contains("Correct") && !leaf7.Contains("Wrong"),
    "the sheet is a real clipboard with clipped tapes and pencil tags; it prints nothing about right or wrong");
Console.WriteLine($"T07 checks: {checks - t07Start}, failures {fails - t07Fails} (choices, table/boundaries/cents, chains, support facts, refusals, invalidation, save/failure, resume, content, wiring; Unity rendering, touch and feel not run).");

// =====================================================================
// T08: Bea's withholding certificate (Type 8), the credit and the remaining payable
// =====================================================================
int t08Start = checks, t08Fails = fails;
void Credit8(Level3Calculator c, Level3ChapterCheckpoint s) => c.SelectSource(Fs7("credits"), "Credits", Level3ComputationRules.Record(s, B7).creditAmount);
Level3ChapterCheckpoint Ready8(string credit = Level3ComputationRules.CertificateCredit)
{
    Level3ChapterCheckpoint s = s7.Copy();
    Level3CertificateRules.HandOver(s, out _);
    Level3CertificateRules.Explain(s, out _);
    if (credit != null) Level3ComputationRules.SelectCredit(s, bea, credit, out _);
    return s;
}
Level3CommandOutcome Pay8(Level3ChapterCheckpoint s, Action<Level3Calculator> build, out string why)
{
    var c = new Level3Calculator();
    build(c);
    return Level3ComputationRules.AttachTape(s, bea, "remaining-payable", c.DraftChain, c.DraftResult, out why);
}
void Due8(Level3Calculator c, Level3ChapterCheckpoint s) { Sheet7(c, s, "tax-due"); c.Press(Level3CalcKey.Subtract); Credit8(c, s); c.Press(Level3CalcKey.Equals); }

// ---------- the Type 8 paper: existing record system, only the missing presentation ----------
List<ClientRecord> wht8 = Level3RecordRules.Build(bea, new List<string> { "B-WHT" });
ClientRecord cert8 = wht8.Count == 1 ? wht8[0] : new ClientRecord();
Check(cert8.type == ClientRecord.Withholding && cert8.id == "L3-BEA-001/B-WHT" && cert8.source == "B-WHT" && cert8.tab == "B-WHT" && cert8.npcId == B7,
    "B-WHT prints as a Type 8 withholding certificate with the case's own source identity (case/source id, Case Folder tab)");
Check(cert8.title == "WITHHOLDING CERTIFICATE" && cert8.issuer == "Harbor Menu Client" && cert8.party == "Bea" && cert8.period == "2026-10-01 to 2026-12-31" &&
      cert8.reference == "WHT-B26" && cert8.footer == Level3RecordRules.WithholdingPurpose,
    "it prints payer, payee, period (inside 2026), reference and its purpose, all from the case document");
Check(cert8.rows.Count == 1 && cert8.rows[0].label == "Income payment" && cert8.rows[0].value == "₱250,000.00" &&
      cert8.totalLabel == "Tax withheld" && cert8.totalValue == "₱12,500.00" && cert8.backRows.Count == 0,
    "the income payment (250,000) is a row and the credit link's row is the TAX WITHHELD band (12,500)");
Check(cert8.includedIn == "Already part of Menu illustration fees ₱400,000.00 on income record REV-B26 (2026)." &&
      Level3RecordRules.IncludedIn(bea, "R2") == cert8.includedIn && Level3RecordRules.IncludedIn(bea, "R404") == null,
    "the paper says the 250,000 is already inside R2's 400,000 menu fees, same tax year (from the content's credit link, never another revenue line)");
Check(!Level3RecordRules.Build(bea, Level3ChapterRules.BeaHandoverSources).Any(r => r.type == ClientRecord.Withholding) &&
      Level3RecordRules.Build(bea, new List<string> { "C-WHT" }).Count == 0,
    "the table's nine papers hold no certificate; another case's certificate id never prints for Bea");
List<ClientRecord> cwht8 = Level3RecordRules.Build(marine, new List<string> { "C-WHT" });
Check(cwht8.Count == 1 && cwht8[0].type == ClientRecord.Withholding && cwht8[0].party == "Marine Services" && cwht8[0].totalValue == "₱300,000.00" &&
      cwht8[0].rows[0].value == "₱8,000,000.00" && cwht8[0].id == "L3-MARINE-001/C-WHT",
    "the same presentation already serves the company's certificate packet (its own payee, payments and withholding; that case is not implemented here)");
SupportingPaperStyle style8 = SupportingPaperTypes.Style(SupportingPaperType.Withholding2307);
Check(SupportingPaperTypes.Implemented(SupportingPaperType.Withholding2307) && style8.Type == SupportingPaperType.Withholding2307 && style8.Header == 0x346884 &&
      style8.Shape == SupportingPaperShape.Sheet && style8.Title == "WITHHOLDING CERTIFICATE" && style8.IssuerLabel == "Payer" && style8.PartyLabel == "Payee" && style8.HasTotalBand &&
      new[] { 4, 5, 6, 7, 0 }.All(t => SupportingPaperTypes.Style((SupportingPaperType)t).Header != style8.Header) &&
      !SupportingPaperTypes.Implemented(SupportingPaperType.Compensation2316),
    "the standard now implements Type 8: blue header of its own, payer/payee labels, a total band; Type 3 stays planned");
var review8 = new ReviewDocumentDefinition { id = "FX-8", kind = "record", paperType = 8, issuer = "Payer Co", reference = "R-1", date = "2026" };
SupportingPaperFace face8 = SupportingPaperTypes.Face(review8, "Client");
Check(!face8.Fallback && face8.Problem == "" && face8.Party == "Client" && face8.Title == "WITHHOLDING CERTIFICATE" && SupportingPaperTypes.MappingProblems(new[] { review8 }).Count == 0,
    "a Type 8 review record is no longer a fallback sheet (no diagnostic) and names its payee");
Check(CaseRecordRules.TypeForPaper(8) == ClientRecord.Withholding && CaseRecordRules.TypeForPaper(4) == ClientRecord.Income && CaseRecordRules.TypeForPaper(3) == ClientRecord.Plain &&
      CaseRecordRules.TypeTitle(ClientRecord.Withholding) == "WITHHOLDING CERTIFICATE",
    "the review path maps 8 to the certificate; 4/5/7 unchanged; 3 still prints plain");
var packet8 = new ReviewRecordInput { id = "P8", documentName = "P8", paperType = 8, totalKey = "credit" };
foreach (var (k, v) in new[] { ("q1", "₱1.00"), ("q2", "₱2.00"), ("q3", "₱3.00"), ("credit", "₱0.30") })
    packet8.fields.Add(new CaseRecordField { key = k, label = k, value = v });
ClientRecord packetRecord8 = CaseRecordRules.FromReviewRecord("CASE", "Client", packet8);
Check(packetRecord8.type == ClientRecord.Withholding && packetRecord8.rows.Count == CaseRecordRules.WithholdingRowsPerFace && packetRecord8.backRows.Count == 1 &&
      packetRecord8.totalValue == "₱0.30",
    "a certificate with more payments than fit continues them on its back; the withheld total keeps its own band (no tiny print)");
Check(SupportingDocumentFace.SizeOf(ClientRecord.Withholding) == SupportingDocumentFace.SheetSize &&
      SupportingDocumentFace.TypeColor(ClientRecord.Withholding) == SupportingDocumentFace.WithholdingBlue &&
      SupportingDocumentFace.TypeColor(ClientRecord.Withholding) != SupportingDocumentFace.CompensationBlue &&
      SupportingDocumentFace.TypeColor(ClientRecord.Withholding) != SupportingDocumentFace.IdentityBlue,
    "it is a full sheet with its own blue (not the ID card's or the 2316's)");
string face8Src = Src("Scripts/SideQuest1/SupportingDocumentFace.cs");
string build8 = Body6(face8Src, "private static void BuildWithholding(", "/// <summary>A small capital label over its value");
Check(face8Src.Contains("else if (record.type == ClientRecord.Withholding) BuildWithholding(root, record, physical);") &&
      new[] { "\"PAYER\"", "\"PAYEE\"", "\"PERIOD\"", "r.includedIn", "\"TAX WITHHELD\"", "\"Ref \"", "Value(r.footer)", "Backing(root, s, 18f, Paper)" }.All(build8.Contains) &&
      face8Src.Contains("else if (type == ClientRecord.Withholding)"),
    "the face builder prints the fixed Type 8 layout (header, purpose, payer, payee/period, payment with where it is counted, tax withheld, reference) on the physical paper's backing");

// ---------- admission: only at the office credit step, saved first ----------
Check(!Level3CertificateRules.CreditStepOpen(acq) && Level3CertificateRules.CreditStepOpen(s7) && !Level3CertificateRules.CreditStepOpen(fr) &&
      Level3CertificateRules.HandOverDue(s7) && !Level3CertificateRules.QuestionDue(s7),
    "the credit step opens once Bea's tax due is on the sheet (not before, not at her table)");
Level3ChapterCheckpoint early8 = acq.Copy();
string early8Snap = Snap(early8);
Check(Level3CertificateRules.HandOver(early8, out string early8Why) == Level3CommandOutcome.Refused && early8Why == Level3CertificateRules.RefuseNotYet &&
      Level3CertificateRules.HandOver(fr.Copy(), out string fr8Why) == Level3CommandOutcome.Refused && fr8Why == Level3CertificateRules.RefuseNotHere &&
      Snap(early8) == early8Snap,
    "before the tax due (or before the worksheet is collected) she keeps it: refused, nothing changes");
Level3ChapterCheckpoint hand8 = s7.Copy();
string s7Snap8 = Snap(s7);
Check(Level3CertificateRules.HandOver(hand8, out string hand8Why) == Level3CommandOutcome.Applied && hand8Why == null && Level3CertificateRules.Acquired(hand8) &&
      hand8.actionIds.Contains(Level3CertificateRules.HandOverAction) && hand8.milestones.Contains(Level3CertificateRules.ReceivedMilestone) &&
      hand8.Case(B7).acquiredSourceIds.Last() == "B-WHT" && Snap(s7) == s7Snap8 && Level3ChapterRules.Problem(hand8) == null,
    "the handover acquires B-WHT under one stable action id on the candidate only (the live state waits for the save)");
Check(Level3CertificateRules.HandOver(hand8, out string dup8) == Level3CommandOutcome.Duplicate && dup8 == "already handed over" &&
      hand8.Case(B7).acquiredSourceIds.Count(id => id == "B-WHT") == 1 && hand8.Case("L3-MARINE-001").acquiredSourceIds.Count == 0 &&
      Level3ComputationRules.Record(hand8, B7).revision == Level3ComputationRules.Record(s7, B7).revision,
    "a repeat or a resume is a quiet duplicate (one paper); the company case and the sheet are untouched");
Level3ChapterCheckpoint tooEarly8 = acq.Copy();
tooEarly8.Case(B7).acquiredSourceIds.Add("B-WHT");
tooEarly8.phase = "WorksheetReady";
Check(Level3ChapterRules.Problem(tooEarly8) != null, "a snapshot claiming the certificate before the computation is refused on restore (T02 guard)");

// ---------- the question: the right answer is saved and fills in nothing; the others change nothing ----------
Check(Level3CertificateRules.Answer("credit") == Level3CertificateRules.AnswerKind.Explains && Level3CertificateRules.Answer("income") == Level3CertificateRules.AnswerKind.NeedsHelp &&
      Level3CertificateRules.Answer("inspect") == Level3CertificateRules.AnswerKind.Inspect && Level3CertificateRules.Answer("x") == Level3CertificateRules.AnswerKind.Unknown &&
      Level3CertificateRules.Answers.SequenceEqual(new[] { "credit", "income", "inspect" }),
    "the three answers in CONTENT order: explains, needs help, inspect");
Check(Level3CertificateRules.Explain(s7.Copy(), out string noCert8) == Level3CommandOutcome.Refused && noCert8 == Level3CertificateRules.RefuseNotHere && Level3CertificateRules.QuestionDue(hand8),
    "no explanation without the certificate; with it, her question is due");
Level3ChapterCheckpoint said8 = hand8.Copy();
int tapes8 = said8.tapes.Count;
Check(Level3CertificateRules.Explain(said8, out _) == Level3CommandOutcome.Applied && Level3CertificateRules.Explained(said8) && !Level3CertificateRules.QuestionDue(said8) &&
      Level3ComputationRules.Record(said8, B7).creditChoice == "" && Level3ComputationRules.Record(said8, B7).creditAmount == "" && said8.tapes.Count == tapes8 &&
      Level3ComputationRules.Record(said8, B7).revision == Level3ComputationRules.Record(hand8, B7).revision && said8.actionIds.Contains(Level3CertificateRules.ExplainAction) &&
      Level3CertificateRules.Explain(said8, out _) == Level3CommandOutcome.Duplicate,
    "the right explanation is saved and never auto-fills the credit, a tape or a revision; a repeat is quiet");
string bea8 = Src("Scripts/Level3/Level3BeaInteractable.cs");
string help8 = Body6(bea8, "case Level3CertificateRules.AnswerKind.NeedsHelp:", "case Level3CertificateRules.AnswerKind.Inspect:");
string inspect8 = Body6(bea8, "case Level3CertificateRules.AnswerKind.Inspect:", "/// <summary>The Tax Code Book's Credit Versus Expense page");
Check(help8.Length > 0 && !help8.Contains("Commit") && help8.Contains("AwaitAction(HelpAction") && inspect8.Length > 0 && !inspect8.Contains("Commit") &&
      inspect8.Contains("AskLines(bea, answer.reply)") && bea8.Contains("case HelpAction: ShowHelp(); break;") &&
      bea8.Contains("dialogue.ContinueWith(HelpAction, AskLines(table.Content, null))"),
    "a wrong answer saves nothing and opens the book's help, then the same question; inspect holds the certificate up and returns to the same question");
foreach (string file in new[] { "Scripts/Level3/Level3BeaInteractable.cs", "Scripts/Level3/Level3CertificateRules.cs", "Scripts/Level3/Level3CertificatePaper.cs", "Scripts/Level3/Level3BeaOfficeVisit.cs" })
{
    string code8 = System.Text.RegularExpressions.Regex.Replace(Src(file), @"///.*|//.*", "");
    Check(!code8.Contains("publicTrust") && !code8.Contains("Ethics") && !code8.Contains("GrantCoins") && !code8.Contains("AddSideQuest") && !code8.Contains("coins") &&
          !code8.Contains("expectedFields") && !code8.Contains("taxWithheldOrCredits") && !code8.Contains("finalTaxPayable"),
        $"{file}: no trust, ethics, coins or grade from the credit step, and no answer key");
}

// ---------- the credit: explicit, from her own acquired certificate ----------
Check(Level3ComputationRules.CreditChoices(s7, bea).Count == 0 && Level3ComputationRules.TotalChoices(hand8, bea).Select(c => c.ChoiceId).SequenceEqual(choices7.Select(c => c.ChoiceId)),
    "no credit on offer before the handover; afterwards Gross income / Allowable expenses still offer only the worksheet's totals and Bea's draft (the certificate never becomes revenue)");
List<Level3TotalChoice> credit8 = Level3ComputationRules.CreditChoices(hand8, bea);
Check(credit8.Select(c => c.ChoiceId).SequenceEqual(new[] { Level3ComputationRules.CertificateIncome, Level3ComputationRules.CertificateCredit }) &&
      credit8.Select(c => c.Amount).SequenceEqual(new[] { "250000.00", "12500.00" }) &&
      credit8[0].Label == "Certificate WHT-B26 · Income payment" && credit8[1].Label == "Certificate WHT-B26 · Tax withheld",
    "Tax credits offers both printed amounts of her certificate alike (its income payment is a plausible wrong choice)");
Check(Level3ComputationRules.SelectCredit(hand8.Copy(), bea, Level3ComputationRules.CertificateCredit, out string expl8) == Level3CommandOutcome.Refused && expl8 == Level3ComputationRules.RefuseExplainFirst &&
      Level3ComputationRules.SelectCredit(s7.Copy(), bea, Level3ComputationRules.CertificateCredit, out string nc8) == Level3CommandOutcome.Refused && nc8 == Level3ComputationRules.RefuseNoCertificate &&
      Level3ComputationRules.SelectCredit(said8.Copy(), bea, "paper:C-WHT/CWithheldCredit", out string cross8) == Level3CommandOutcome.Refused && cross8 == Level3ComputationRules.RefuseNotCertificate &&
      Level3ComputationRules.SelectCredit(said8.Copy(), bea, "worksheet:income", out _) == Level3CommandOutcome.Refused &&
      Level3ComputationRules.SelectCredit(acq.Copy(), bea, Level3ComputationRules.CertificateCredit, out _) == Level3CommandOutcome.Refused,
    "no credit before the explanation or the handover; another case's certificate or a worksheet total is not a credit");
Level3ChapterCheckpoint link8 = said8.Copy();
int rev8 = Level3ComputationRules.Record(link8, B7).revision;
Check(Level3ComputationRules.SelectCredit(link8, bea, Level3ComputationRules.CertificateCredit, out _) == Level3CommandOutcome.Applied &&
      Level3ComputationRules.Record(link8, B7).creditChoice == Level3ComputationRules.CertificateCredit && Level3ComputationRules.Record(link8, B7).creditAmount == "12500.00" &&
      Level3ComputationRules.Record(link8, B7).revision == rev8 + 1 && !Level3ComputationRules.Record(link8, B7).payableNoted &&
      Level3ComputationRules.SelectCredit(link8, bea, Level3ComputationRules.CertificateCredit, out string dupCredit8) == Level3CommandOutcome.Duplicate && dupCredit8 == "already chosen",
    "linking the certificate's tax withheld copies 12,500 to Tax credits (a revision); a repeat is quiet");
Level3ComputationFacts lf8 = Facts7(link8);
Check(lf8.Step("credits").Present && lf8.Step("credits").Follows && lf8.Step("credits").Supported && lf8.Step("credits").Root == "credit-source" &&
      lf8.Step("credits").Value == "12500.00" && lf8.CertificateIncomeIncludedIn == "R2" && !lf8.Complete && !lf8.PayableSupported,
    "the credit fact: present, its own credit row from an acquired certificate of Bea's, same year, supported; the certificate income is counted in R2");
Level3ChapterCheckpoint wrongCredit8 = Ready8(Level3ComputationRules.CertificateIncome);
Check(!Facts7(wrongCredit8).Step("credits").Follows && !Facts7(wrongCredit8).Step("credits").Supported && Facts7(wrongCredit8).Step("credits").Value == "250000.00",
    "the certificate's income payment as the credit is accepted as draft work but does not follow (root credit-source)");
Level3ChapterCheckpoint otherYear8 = Ready8();
CaseDefinition beaShifted8 = LoadCase("case_bea_001.json");
beaShifted8.level3.sources.Find(s => s.sourceId == "B-WHT").periodEnd = "2027-01-31";
Level3ChapterCheckpoint otherPayee8 = Ready8();
CaseDefinition beaPayee8 = LoadCase("case_bea_001.json");
beaPayee8.level3.sources.Find(s => s.sourceId == "B-WHT").party = "Somebody Else";
Check(!Level3ComputationRules.Evaluate(otherYear8, beaShifted8, B7).Step("credits").Supported && !Level3ComputationRules.Evaluate(otherPayee8, beaPayee8, B7).Step("credits").Supported &&
      Level3ComputationRules.Evaluate(otherYear8, bea, B7).Step("credits").Supported,
    "a certificate outside the tax year or for another payee never supports the credit (the same amount is not enough)");

// ---------- the remaining payable: tax due - credits = 50,000 ----------
Level3ChapterCheckpoint pay8 = Ready8();
var p8 = new Level3Calculator();
Due8(p8, pay8);
Check(p8.DraftResult == "50000" && Level3Calculator.ChainLines(p8.DraftChain).Last() == "= 50,000.00" &&
      Level3ComputationRules.AttachTape(pay8, bea, "remaining-payable", p8.DraftChain, p8.DraftResult, out string pay8Why) == Level3CommandOutcome.Applied && pay8Why == null,
    "62,500 - 12,500 = 50,000 on the calculator from the sheet's own lines, attached to Remaining payable");
Level3ComputationFacts pf8 = Facts7(pay8);
Check(Level3ComputationRules.Record(pay8, B7).payableNoted && pf8.Step("remaining-payable").Value == "50000" && pf8.Step("remaining-payable").Follows &&
      pf8.Step("remaining-payable").Supported && pf8.Step("remaining-payable").Root == "balance-operation" && pf8.PayableSupported && pf8.TaxDueSupported && pf8.Complete &&
      !pf8.CreditInIncome && pf8.Steps.All(s => s.Supported) && pf8.Revision == Level3ComputationRules.Record(pay8, B7).revision,
    "the T09 facts: every step supported, remaining payable 50,000 (balance-operation), complete, no credit used as an expense, read at the sheet's revision");
Check(Level3ChapterRules.PhaseOf(pay8) == Level3Phase.BeaComputation && Level3ComputationRules.Editable(pay8, B7) && pay8.Case(B7).copyPhase == "" &&
      !pay8.milestones.Any(m => m.Contains("review") || m.Contains("approved")),
    "Bea's computation is complete but unreviewed: still her computation phase, editable, no review, approval or copy");
Check(Level3ComputationRules.AttachTape(pay8.Copy(), bea, "remaining-payable", p8.DraftChain, p8.DraftResult, out string dupPay8) == Level3CommandOutcome.Duplicate && dupPay8 == "already attached",
    "the same payable tape again is a quiet duplicate");
Level3ChapterCheckpoint chain8 = Ready8();
Check(Pay8(chain8, c => { Sheet7(c, chain8, "taxable-income"); c.Press(Level3CalcKey.Subtract); Const7(c, chain8, "lower"); c.Press(Level3CalcKey.Equals);
          c.Press(Level3CalcKey.Multiply); Const7(c, chain8, "rate"); c.Press(Level3CalcKey.Equals); c.Press(Level3CalcKey.Add); Const7(c, chain8, "base"); c.Press(Level3CalcKey.Equals);
          c.Press(Level3CalcKey.Subtract); Credit8(c, chain8); c.Press(Level3CalcKey.Equals); }, out _) == Level3CommandOutcome.Applied &&
      Facts7(chain8).Step("remaining-payable").Supported && Facts7(chain8).Step("remaining-payable").Value == "50000",
    "one chain from taxable income through the band's formula to the credit is an equivalent supported payable");
Level3ChapterCheckpoint typedPay8 = Ready8();
Check(Pay8(typedPay8, c => Keys(c, "62500-12500="), out _) == Level3CommandOutcome.Applied && Facts7(typedPay8).Step("remaining-payable").Typed &&
      !Facts7(typedPay8).Step("remaining-payable").Supported && Facts7(typedPay8).Step("remaining-payable").Value == "50000" && !Facts7(typedPay8).PayableSupported,
    "the right balance typed by hand is kept (TYPED · NO SOURCE) but supports nothing");
Check(Pay8(wrongCredit8, c => Due8(c, wrongCredit8), out _) == Level3CommandOutcome.Applied && Facts7(wrongCredit8).Step("remaining-payable").Value == "-187500" &&
      Facts7(wrongCredit8).Step("remaining-payable").Follows && !Facts7(wrongCredit8).Step("remaining-payable").Supported,
    "with the income payment as the credit the balance is -187,500: negative, kept (no clamp), it follows the player's own credit but is not supported");
Level3ChapterCheckpoint both8 = Ready8();
var tb8 = new Level3Calculator();
Total7(tb8, "gross", both8); tb8.Press(Level3CalcKey.Subtract); Total7(tb8, "expenses", both8); tb8.Press(Level3CalcKey.Subtract); Credit8(tb8, both8); tb8.Press(Level3CalcKey.Equals);
var db8 = new Level3Calculator();
bool bothTaxable8 = Put7(both8, "taxable-income", tb8) == Level3CommandOutcome.Applied;
Sheet7(db8, both8, "taxable-income"); db8.Press(Level3CalcKey.Subtract); Const7(db8, both8, "lower"); db8.Press(Level3CalcKey.Equals);
db8.Press(Level3CalcKey.Multiply); Const7(db8, both8, "rate"); db8.Press(Level3CalcKey.Equals); db8.Press(Level3CalcKey.Add); Const7(db8, both8, "base"); db8.Press(Level3CalcKey.Equals);
bool bothDue8 = Put7(both8, "tax-due", db8) == Level3CommandOutcome.Applied;
Level3ChapterCheckpoint expense8 = both8.Copy();
Check(bothTaxable8 && bothDue8 && Pay8(both8, c => Due8(c, both8), out _) == Level3CommandOutcome.Applied, "bea-credit-both is accepted as draft work (credit as a cost and again as a credit)");
Level3ComputationFacts bf8 = Facts7(both8);
Check(bf8.Step("taxable-income").Value == "587500" && bf8.Step("tax-due").Value == "60000" && bf8.Step("remaining-payable").Value == "47500" && bf8.CreditInIncome &&
      !bf8.Step("taxable-income").Follows && bf8.Step("tax-due").Follows && !bf8.Step("tax-due").Supported && bf8.Step("remaining-payable").Follows &&
      !bf8.Step("remaining-payable").Supported && !bf8.PayableSupported,
    "bea-credit-both (587,500 / 60,000 / 47,500): reported as the credit used as an expense; later steps follow it but nothing after it is supported");
Check(Pay8(expense8, c => { Sheet7(c, expense8, "tax-due"); c.Press(Level3CalcKey.Equals); }, out _) == Level3CommandOutcome.Applied &&
      Facts7(expense8).Step("remaining-payable").Value == "60000" && !Facts7(expense8).Step("remaining-payable").Follows && Facts7(expense8).CreditInIncome,
    "bea-credit-expense (587,500 / 60,000 / 60,000): the credit as a cost and no credit afterwards never follows");
Level3ChapterCheckpoint cancel8 = Ready8();
Check(Pay8(cancel8, c => { Sheet7(c, cancel8, "tax-due"); c.Press(Level3CalcKey.Subtract); Sheet7(c, cancel8, "tax-due"); c.Press(Level3CalcKey.Multiply); Const7(c, cancel8, "rate"); c.Press(Level3CalcKey.Equals); }, out _) == Level3CommandOutcome.Applied &&
      Facts7(cancel8).Step("remaining-payable").Value == "50000" && !Facts7(cancel8).Step("remaining-payable").Follows && !Facts7(cancel8).PayableSupported,
    "a matching 50,000 without the linked credit (tax due - tax due x 20%, a withholding-rate guess) never follows: the balance must use the certificate's credit");
var extra8 = new Level3Calculator();
Total7(extra8, "gross", pay8); extra8.Press(Level3CalcKey.Add); extra8.SelectSource(Level3ComputationRules.CertificateIncome, "Income payment", "250000.00"); extra8.Press(Level3CalcKey.Equals);
Check(Level3ComputationRules.SelectTotal(pay8.Copy(), bea, "gross", Level3ComputationRules.CertificateIncome, out _) == Level3CommandOutcome.Refused &&
      Level3ComputationRules.AttachTape(pay8.Copy(), bea, "taxable-income", extra8.DraftChain, extra8.DraftResult, out string extra8Why) == Level3CommandOutcome.Refused &&
      extra8Why == Level3ComputationRules.RefuseForeign && Level3WorksheetRules.RowsOf(bea).Count == 7,
    "bea-extra-income cannot happen: the certificate's 250,000 is never a total choice, a sheet source or another worksheet strip");

// ---------- refusals: only the sheet's own numbers, current, in order ----------
Level3ChapterCheckpoint ref8 = Ready8();
string ref8Snap = Snap(ref8);
var direct8 = new Level3Calculator();
Sheet7(direct8, ref8, "tax-due"); direct8.Press(Level3CalcKey.Subtract); direct8.SelectSource(Level3ComputationRules.CertificateCredit, "Tax withheld", "12500.00"); direct8.Press(Level3CalcKey.Equals);
var cross8c = new Level3Calculator();
Sheet7(cross8c, ref8, "tax-due"); cross8c.Press(Level3CalcKey.Subtract); cross8c.SelectSource("field:marine/credits", "Credits", "12500.00"); cross8c.Press(Level3CalcKey.Equals);
var self8 = new Level3Calculator();
self8.SelectSource(Ss7("remaining-payable"), "Remaining payable", "50000"); self8.Press(Level3CalcKey.Equals);
var changedCredit8 = new Level3Calculator();
Sheet7(changedCredit8, ref8, "tax-due"); changedCredit8.Press(Level3CalcKey.Subtract); changedCredit8.SelectSource(Fs7("credits"), "Credits", "12000.00"); changedCredit8.Press(Level3CalcKey.Equals);
Check(Level3ComputationRules.AttachTape(ref8, bea, "remaining-payable", direct8.DraftChain, direct8.DraftResult, out string d8w) == Level3CommandOutcome.Refused && d8w == Level3ComputationRules.RefuseForeign &&
      Level3ComputationRules.AttachTape(ref8, bea, "remaining-payable", cross8c.DraftChain, cross8c.DraftResult, out string c8w) == Level3CommandOutcome.Refused && c8w == Level3ComputationRules.RefuseForeign &&
      Level3ComputationRules.AttachTape(ref8, bea, "tax-due", self8.DraftChain, self8.DraftResult, out string s8w) == Level3CommandOutcome.Refused && s8w == Level3ComputationRules.RefuseLater &&
      Level3ComputationRules.AttachTape(ref8, bea, "remaining-payable", changedCredit8.DraftChain, changedCredit8.DraftResult, out string g8w) == Level3CommandOutcome.Refused && g8w == Level3ComputationRules.RefuseChanged &&
      Level3ComputationRules.AttachTape(ref8, bea, "credits", p8.DraftChain, p8.DraftResult, out _) == Level3CommandOutcome.Refused &&
      Snap(ref8) == ref8Snap,
    "the certificate's number straight off the paper, another case's credit, a later line, a changed credit or a tape on the Tax credits choice: refused, nothing changes");
Level3ChapterCheckpoint noCredit8 = Ready8(credit: null);
var nc8c = new Level3Calculator();
Sheet7(nc8c, noCredit8, "tax-due"); nc8c.Press(Level3CalcKey.Subtract); nc8c.SelectSource(Fs7("credits"), "Credits", "12500.00"); nc8c.Press(Level3CalcKey.Equals);
Check(Level3ComputationRules.AttachTape(noCredit8, bea, "remaining-payable", nc8c.DraftChain, nc8c.DraftResult, out string nc8w) == Level3CommandOutcome.Refused && nc8w == Level3ComputationRules.RefuseForeign,
    "Tax credits must be linked before it can be used (an empty credit line is not a source)");

// ---------- invalidation: a credit-only change marks only the balance ----------
Level3ChapterCheckpoint inv8 = pay8.Copy();
int invRev8 = Level3ComputationRules.Record(inv8, B7).revision;
Check(Level3ComputationRules.SelectCredit(inv8, bea, Level3ComputationRules.CertificateIncome, out _) == Level3CommandOutcome.Applied &&
      Level3ComputationRules.IsStale(inv8, bea, B7, "remaining-payable") && new[] { "taxable-income", "excess", "marginal-tax", "tax-due" }.All(f => Level3ComputationRules.IsCurrent(inv8, bea, B7, f)) &&
      Facts7(inv8).TaxDueSupported && !Facts7(inv8).Complete && Facts7(inv8).Revision == invRev8 + 1 && inv8.Tape("bea/remaining-payable").result == "50000" &&
      Level3ComputationRules.Record(inv8, B7).taxDueNoted && Level3ComputationRules.Record(inv8, B7).payableNoted,
    "changing only the credit marks the remaining payable 'needs recalculation' (kept), every tax line stays current, and the revision moves on (a stamp of the old one is out of date)");
Level3ComputationRules.SelectCredit(inv8, bea, Level3ComputationRules.CertificateCredit, out _);
Check(Level3ComputationRules.IsCurrent(inv8, bea, B7, "remaining-payable") && Facts7(inv8).PayableSupported && Facts7(inv8).Revision == invRev8 + 2,
    "back on the same credit the same payable tape is current again; the revision still counts both changes");
Level3ChapterCheckpoint band8 = pay8.Copy();
Level3ComputationRules.SelectRule(band8, bea, "IND-25", out _);
Check(Level3ComputationRules.IsStale(band8, bea, B7, "tax-due") && Level3ComputationRules.IsStale(band8, bea, B7, "remaining-payable") &&
      Level3ComputationRules.IsCurrent(band8, bea, B7, "taxable-income") && Facts7(band8).Step("credits").Supported,
    "changing the band marks tax due and the balance through the chain; taxable income and the credit stay");
var stalePay8 = new Level3Calculator();
Sheet7(stalePay8, band8, "tax-due"); stalePay8.Press(Level3CalcKey.Subtract); Credit8(stalePay8, band8); stalePay8.Press(Level3CalcKey.Equals);
Check(Level3ComputationRules.AttachTape(band8.Copy(), bea, "remaining-payable", stalePay8.DraftChain, stalePay8.DraftResult, out string stale8w) == Level3CommandOutcome.Refused &&
      stale8w == Level3ComputationRules.RefuseStaleSource,
    "a tax due that needs recalculating cannot feed the balance");
string read8 = Snap(pay8);
Level3ComputationRules.Evaluate(pay8, bea, B7);
Level3ComputationRules.CreditChoices(pay8, bea);
Level3RecordRules.Build(bea, pay8.Case(B7).acquiredSourceIds);
Check(Snap(pay8) == read8, "reading the certificate, the choices or the facts changes nothing");

// ---------- persistence: round trip, old saves, failure ----------
PlayerProgressData save8 = Level3ChapterRules.WithCheckpoint(Clone(live), pay8);
save8.Normalize();
PlayerProgressData back8 = JsonSerializer.Deserialize<PlayerProgressData>(Snap(save8), json);
Level3ChapterCheckpoint back8State = back8.campaign.levelThree;
Check(!back8.Normalize() && Level3ChapterRules.Decide(back8State) == Level3EntryDecision.Resume && Level3CertificateRules.Acquired(back8State) &&
      Level3CertificateRules.Explained(back8State) && Level3ComputationRules.Record(back8State, B7).creditChoice == Level3ComputationRules.CertificateCredit &&
      Level3ComputationRules.Record(back8State, B7).payableNoted && back8State.actionIds.Contains(Level3CertificateRules.HandOverAction) &&
      Facts7(back8State).PayableSupported && Facts7(back8State).Complete && back8State.Case(B7).acquiredSourceIds.Count(id => id == "B-WHT") == 1,
    "acquisition, explanation, credit, payable tape and their sources round-trip under Bea's case without repair; one certificate");
Check(WithoutChapter(back8) == WithoutChapter(save8) && back8.campaign.coins == live.campaign.coins && back8.campaign.publicTrust == live.campaign.publicTrust,
    "the credit step changes no coins, trust or anything else of the campaign");
JsonNode oldT07 = JsonNode.Parse(Snap(Level3ChapterRules.WithCheckpoint(Clone(live), s7)));
foreach (JsonNode caseNode8 in oldT07["campaign"]["levelThree"]["cases"].AsArray())
    foreach (string key in new[] { "creditExplained", "creditChoice", "creditAmount", "payableNoted" }) caseNode8["computation"].AsObject().Remove(key);
PlayerProgressData oldT07Back = JsonSerializer.Deserialize<PlayerProgressData>(oldT07.ToJsonString(), json);
oldT07Back.Normalize();
Check(Level3ComputationRules.Record(oldT07Back.campaign.levelThree, B7).creditChoice == "" && !Level3CertificateRules.Acquired(oldT07Back.campaign.levelThree) &&
      Level3ChapterRules.Decide(oldT07Back.campaign.levelThree) == Level3EntryDecision.Resume && Facts7(oldT07Back.campaign.levelThree).TaxDueSupported &&
      Level3CertificateRules.HandOverDue(oldT07Back.campaign.levelThree),
    "a T07 save without the credit fields resumes with its tax lines and the certificate still to be handed over");
string credit8Snap = Snap(Level3ComputationRules.Record(pay8, B7));
foreach (string leak in new[] { "expected", "taxWithheldOrCredits", "finalTaxPayable", "50000", "level3" })
    Check(!credit8Snap.Contains(leak), $"the saved sheet record holds only the player's choices ('{leak}')");
string sandbox8 = Path.Combine(Path.GetTempPath(), "TaxVentureL3T08-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox8);
try
{
    string Encode8(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var store8 = new SaveFileStore(Path.Combine(sandbox8, "progress.json"));
    PlayerProgressData pub8 = Level3ChapterRules.WithCheckpoint(Clone(live), s7.Copy());
    pub8.Normalize();
    store8.Write(Encode8(pub8));
    byte[] disk8 = File.ReadAllBytes(store8.FilePath);
    string pub8Snap = Snap(pub8);
    Level3ChapterCheckpoint cand8 = pub8.campaign.levelThree.Copy();
    Level3CertificateRules.HandOver(cand8, out _);
    Directory.CreateDirectory(store8.TempPath);
    bool wrote8 = false;
    try { PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub8), cand8); next.Normalize(); store8.Write(Encode8(next)); pub8 = next; wrote8 = true; }
    catch (Exception) { }
    Check(!wrote8 && Snap(pub8) == pub8Snap && File.ReadAllBytes(store8.FilePath).SequenceEqual(disk8) && !Level3CertificateRules.Acquired(pub8.campaign.levelThree) &&
          Level3CertificateRules.HandOver(pub8.campaign.levelThree.Copy(), out _) == Level3CommandOutcome.Applied,
        "a failed write of the handover keeps the certificate with Bea on disk and live; taking it again works (no duplicate paper)");
    Directory.Delete(store8.TempPath);
    store8.Write(Encode8(Level3ChapterRules.WithCheckpoint(Clone(pub8), Ready8(credit: null))));
    PlayerProgressData stored8 = JsonSerializer.Deserialize<PlayerProgressData>(JsonDocument.Parse(File.ReadAllText(store8.FilePath)).RootElement.GetProperty("payload").GetString(), json);
    Check(Level3CertificateRules.Acquired(stored8.campaign.levelThree) && Level3CertificateRules.Explained(stored8.campaign.levelThree) &&
          Level3ComputationRules.Record(stored8.campaign.levelThree, B7).creditChoice == "",
        "a signed save in the middle (handed over, explained, no credit yet) restores exactly that: the credit is never filled in for the player");
}
finally
{
    try { Directory.Delete(sandbox8, true); } catch (IOException) { }
}

// ---------- resume observers ----------
Check(Level3ChapterRules.ImpliedTriggers(hand8).Contains(Level3ChapterRules.CertificateTrigger) && !Level3ChapterRules.ImpliedTriggers(s7).Contains(Level3ChapterRules.CertificateTrigger) &&
      Level3ChapterRules.ImpliedTriggers(pay8).Contains(Level3ChapterRules.PayableTrigger) && !Level3ChapterRules.ImpliedTriggers(link8).Contains(Level3ChapterRules.PayableTrigger),
    "resume: a saved handover and a saved current balance keep their objectives ticked; nothing else ticks them");
Check(Level3ChapterRules.ImpliedLessons(hand8).Contains(Level3ChapterRules.CreditCertificateLesson) && !Level3ChapterRules.ImpliedLessons(s7).Contains(Level3ChapterRules.CreditCertificateLesson) &&
      Level3ChapterRules.ImpliedLessons(link8).Contains(Level3ChapterRules.CreditLinkLesson) && !Level3ChapterRules.ImpliedLessons(link8).Contains(Level3ChapterRules.CreditPayableLesson) &&
      Level3ChapterRules.ImpliedLessons(pay8).Contains(Level3ChapterRules.CreditDoneLesson),
    "resume: each credit lesson the saved sheet already shows taught is skipped; the next still teaches");

// ---------- content: Bea's lines, the book, objectives, guide ----------
Level3TownFile town8 = JsonSerializer.Deserialize<Level3TownFile>(File.ReadAllText(Res("Levels/Level_03/town_life.json")), json);
Level3ClientContent beaTown8 = town8.bea;
Check(Level3TownContent.Validate(town8).Count == 0 && beaTown8.creditQuestion == "A client withheld some tax from a payment. Is that another business expense?" &&
      Level3TownContent.CreditAnswer(beaTown8, "credit").text == "It reduces the remaining tax when supported by the certificate." &&
      Level3TownContent.CreditAnswer(beaTown8, "income").text == "It reduces the income amount before the tax calculation." &&
      Level3TownContent.CreditAnswer(beaTown8, "inspect").text == "Let's inspect the certificate together.",
    "her question and its three answers are CONTENT's exact lines (town_life.json validates)");
Check(beaTown8.toOffice.Count == 6 && beaTown8.toOffice.All(p => p.IsSet) && beaTown8.officeSpotPath == "Environment/OfficeBuilding/Rooms/ConferenceRoom/Client" &&
      beaTown8.certificateHandover.Count == 1 && beaTown8.certificateHandover[0].speaker == "Npc" && beaTown8.certificateHandover[0].text.Contains("menu fees") &&
      beaTown8.certificateHandover[0].text.Split(' ').Length <= 30 && beaTown8.certificateReadLine.Contains("leave it here") &&
      !string.Join(" ", beaTown8.certificateHandover.Select(l => l.text)).Contains("12,500") && !beaTown8.creditAnswers.Any(a => a.reply.Contains("12,500") || a.reply.Contains("50,000")),
    "she walks Rosa's doorway route to the conference room client spot; revised T08: one short turn (at most 30 words) says the tax was withheld from her menu fees, the closing line leaves the paper for collection; no credit or balance named");
var brokenTown8 = JsonSerializer.Deserialize<Level3TownFile>(File.ReadAllText(Res("Levels/Level_03/town_life.json")), json);
brokenTown8.bea.creditAnswers.RemoveAt(1);
Check(Level3TownContent.Validate(brokenTown8).Any(p => p.Contains("creditAnswers")), "a town file missing one answer is reported (the built-in Bea is used)");
var book8 = Json("Levels/Level_03/book.json").GetProperty("sections").EnumerateArray().ToList();
JsonElement leaf8 = book8.First(s => s.GetProperty("heading").GetString().StartsWith("Section 9"));
string leaf8Text = leaf8.GetProperty("body").GetString() + " " + string.Join(" ", leaf8.GetProperty("clickableValues").EnumerateArray().Select(v => v.GetProperty("displayText").GetString()));
Check(book8.Count >= 6 && book8[5].GetProperty("heading").GetString() == "Section 9 – Credit Versus Expense" && leaf8.GetProperty("heading").GetString() == "Section 9 – Credit Versus Expense" &&
      leaf8.GetProperty("heading").GetString().StartsWith(Level3BeaInteractable.HelpSectionPrefix) &&
      leaf8.GetProperty("body").GetString().Split("\n\n").Length == leaf8.GetProperty("clickableValues").GetArrayLength() &&
      leaf8Text.Contains("Tax due minus tax credits equals remaining payable") && leaf8Text.Contains("never add it again") && leaf8Text.Contains("negative balance's sign"),
    "book.json adds Section 9 – Credit Versus Expense (the help her wrong answer opens): credit at the end, count the payment once, keep a negative sign");
foreach (string leak in new[] { "12,500", "250,000", "62,500", "50,000", "400,000", "Bea", "Harbor", "WHT-B26" })
    Check(!leaf8Text.Contains(leak), $"the credit leaf holds no client figure or name ('{leak}'); its example is Practice Design Studio");
var steps8 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps8.Count >= 8 && steps8[6].GetProperty("trigger").GetString() == Level3ChapterRules.CertificateTrigger &&
      steps8[7].GetProperty("trigger").GetString() == Level3ChapterRules.PayableTrigger &&
      ObjectiveTriggers.Validate(Level3ChapterRules.CertificateTrigger) == null && ObjectiveTriggers.Validate(Level3ChapterRules.PayableTrigger) == null &&
      ObjectiveTriggers.Events.Contains(ObjectiveTriggers.WithholdingCertificateReceived) && ObjectiveTriggers.Events.Contains(ObjectiveTriggers.RemainingPayableAttached) &&
      !steps8[7].GetProperty("text").GetString().Contains("50,000"),
    "objectives 7-8 (revision; were 8-9): take Bea's certificate in the conference room, then link the credit and attach what she still has to pay (known events, no value named)");
GuideScript guide8 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var t08Lessons = new[] { Level3ChapterRules.CreditCertificateLesson, Level3ChapterRules.CreditLinkLesson, Level3ChapterRules.CreditPayableLesson }
    .Select(id => guide8.lessons.FirstOrDefault(l => l.id == id)).ToList();
Check(guide8.Validate() == "" && t08Lessons.All(l => l != null && l.pages.All(p => p.IsHint)) &&
      t08Lessons[0].pages[0].waitFor == "flag:certificateReceived" && t08Lessons[1].pages[0].waitFor == "flag:computationCredit|cue:" + Level3DeskCalculator.CueCreditChosen &&
      t08Lessons[2].pages[0].waitFor == "flag:computationPayable" && t08Lessons[1].when == "flag:creditStep",
    "the credit lessons are hints waiting for the player's own handover, link and tape");
Check(!t08Lessons.SelectMany(l => l.pages).Any(p => p.text.Contains("12,500") || p.text.Contains("50,000") || p.text.Contains("250,000") || p.text.Contains("62,500") ||
      p.text.Contains("expense", StringComparison.OrdinalIgnoreCase) || p.text.Contains("click", StringComparison.OrdinalIgnoreCase)) &&
      !guide8.waypoints.Any(w => (w.target ?? "").Contains("ConferenceRoom")),
    "Arthur names no amount and gives away no answer to her question; touch-only words; no arrow to the conference room (a known place)");

// ---------- wiring (source checks; Unity behaviour is the user's) ----------
string cp8 = Src("Scripts/Level3/Level3Checkpointer.cs");
foreach (var (start8, command) in new[] { ("public TapeCommit CommitCertificate(", "Level3CertificateRules.HandOver(candidate"), ("public TapeCommit CommitCreditExplanation(", "Level3CertificateRules.Explain(candidate") })
{
    string body8 = Body6(cp8, start8, "/// <summary>");
    string commitCall8 = start8.Contains("CommitCertificate(") ? "CommitLevelThreeCertificate(candidate)" : "CommitLevelThreeCheckpoint(candidate)";
    Check(Index(body8, "State.Copy()") < Index(body8, command) && Index(body8, command) < Index(body8, commitCall8) &&
          Index(body8, commitCall8) < Index(body8, "Publish(candidate);") && body8.Contains("return TapeCommit.SaveFailed;"),
        $"{start8.Trim('(')}: candidate, signed write, then publish; a failed write returns before publishing");
}
Check(cp8.Contains("if (!hadCertificate && Level3CertificateRules.Acquired(committed)) GameplayEvents.RaiseWithholdingCertificateReceived();") &&
      cp8.Contains("if (!wasPayable && committed.Case(Level3ChapterRules.BeaCaseId)?.computation?.payableNoted == true) GameplayEvents.RaiseRemainingPayableAttached();") &&
      !Body6(cp8, "private void RestoreQuietly(bool welcomeBack = true)", "private static string ActiveDefinitionId").Contains("RaiseWithholding"),
    "both objective events come only from a published, saved change; a quiet resume raises neither");
string take8 = Body6(bea8, "private void TakeCertificate()", "private void ShowAnswers()");
Check(bea8.Contains("CertificateLineReached(dialogue.CurrentLineIndex, certificateDropIndex)") && bea8.Contains("PresentFrom(transform)") &&
    !bea8.Contains("ShowTake()") && !bea8.Contains("AwaitAction = TakeAction"),
    "Certificate: mid-conversation reveal presents a world paper, no take menu or dialogue acquisition");
string explain8 = Body6(bea8, "case Level3CertificateRules.AnswerKind.Explains:", "case Level3CertificateRules.AnswerKind.NeedsHelp:");
Check(Index(explain8, "CommitCreditExplanation(") < Index(explain8, "dialogue.ContinueWith(CreditAction") && explain8.Contains("ShowAnswers();") &&
      !explain8.Contains("SelectCredit") && !bea8.Contains("SelectCredit"),
    "the right answer is saved before she replies; a failed save shows the same answers; the conversation never links the credit");
Check(bea8.Contains("Level3BeaOfficeVisit visit = table.Visit;") && bea8.Contains("Level3CertificateRules.HandOverDue(chapter)") && bea8.Contains("StartInquiry(settled ? bea.officeWaitPrompt : bea.onTheWayPrompt)") &&
      bea8.Contains("paper.Showing == Level3CertificatePaper.Context.Conversation) paper.Lower();") && bea8.Contains("Level3CertificatePaper.Instance.Gather();"),
    "on her way or waiting she answers questions; the handover waits until she stands still; an ended or disabled conversation never leaves the paper up");
string paper8 = Src("Scripts/Level3/Level3CertificatePaper.cs");
Check(paper8.Contains("ClientRecordPaper.Create(this, hand, workspace, record, size, gameObject.layer)") &&
      paper8.Contains("Level3RecordRules.Build(definition, new[] { Level3ChapterRules.BeaCreditSource })") &&
      paper8.Contains("if (!Level3CertificateRules.Acquired(chapter) || definition == null") && paper8.Contains("public sealed class Level3CertificatePaper : MonoBehaviour, IClientRecordHolder") &&
      paper8.Contains("public bool Open(Vector2? screen = null)") && paper8.Contains("Level3AmountSource.AttachTo(paper, definition)") &&
      paper8.Contains("if (!focused && paper != null) paper.CancelDrag();") && paper8.Contains("if (paper != null && !paper.IsAttached) paper.Gather();"),
    "one physical certificate (the standard record paper and Type 8 face), built only after the saved acquisition; Bag opening reuses desk amount bands, focus and disable guards");
string visit8 = Src("Scripts/Level3/Level3BeaOfficeVisit.cs");
Check(visit8.Contains("life.TravelForQuest(stops, RoomFacing(), Arrive);") && visit8.Contains("life.PlaceForQuest(RoomFeet(), RoomFacing());") &&
      visit8.Contains("ClientConsultation.StandInStepsAside(spot);") && visit8.Contains("if (!Visible() && !SpotVisible()) PlaceInRoomNow();") &&
      visit8.Contains("if (phase >= Level3Phase.BeaComputation)") && !visit8.Contains("CommitLevelThreeCheckpoint"),
    "Bea walks with NpcLife's quest travel (out-of-view placement on failure), resumes in the room at once, and her walk saves nothing");
string table8 = Src("Scripts/Level3/Level3BeaTable.cs");
Check(table8.Contains("Visit = holder.AddComponent<Level3BeaOfficeVisit>();") && table8.Contains("bea.creditAnswers.Add(new Level3CreditAnswer { id = Level3CertificateRules.AnswerCredit"),
    "her actor gets the office visit; the built-in Bea carries the credit lines too");
string dc8 = Src("Scripts/Level3/Level3DeskCalculatorComputation.cs");
string desk8 = Src("Scripts/Level3/Level3DeskCalculator.cs");
Check(dc8.Contains("Level3ComputationRules.SelectCredit(c, definition, id, out r)") && Body6(dc8, "private void OpenCredits()", "private void CloseLeafPicker()").Contains("Level3ComputationRules.RefuseExplainFirst") &&
      dc8.Contains("certificate = Level3CertificatePaper.Ensure(transform);") && !dc8.Contains("CommitLevelThreeCheckpoint") &&
      !desk8.Contains("if (CertificateUp)") && !desk8.Contains("certificate.Lower();") &&
      desk8.Contains("if (mode == Mode.Resting && IsCertificateTab(target)) RaiseCertificate();"),
    "the desk links the credit through the chapter command; while the certificate is up it owns the touches (a tap lowers it); the tab raises it without lifting the tools");
string leafSrc8 = Src("Scripts/Level3/Level3ComputationLeafModel.cs");
Check(leafSrc8.Contains("TotalRow(Level3ComputationRules.Credits, -0.090f);") && leafSrc8.Contains("SlotRow(Level3ComputationRules.Payable, -0.112f, false);") &&
      leafSrc8.Contains("HitKind.Certificate") && !leafSrc8.Contains("next: Bea's withholding certificate") && !leafSrc8.Contains("Still to pay") &&
      leafSrc8.Contains("(slot.Field == Level3ComputationRules.TaxDue || slot.Field == Level3ComputationRules.Payable)"),
    "the sheet's pending row became Tax credits (field + PICK) and a Remaining payable slot shown to cents, plus a certificate tab with its own target");
foreach (string file in new[] { "Level3CertificateRules.cs", "Level3CertificatePaper.cs", "Level3BeaOfficeVisit.cs" })
{
    string code8b = System.Text.RegularExpressions.Regex.Replace(Src("Scripts/Level3/" + file), @"///.*|//.*", "");
    Check(!code8b.Contains("float.Parse") && !code8b.Contains("double") && !code8b.Contains("worksheetDecisions"), $"{file}: no float money and no answer key");
}
Console.WriteLine($"T08 checks: {checks - t08Start}, failures {fails - t08Fails} (Type 8 paper, admission, question, credit, payable, negatives, refusals, invalidation, save/failure, resume, content, wiring; Unity rendering, touch and feel not run).");

// =====================================================================
// T09: stamp, the Auditor's review, case XP, 100 coins, Bea's review clock
// =====================================================================
int t09Start = checks, t09Fails = fails;
Level3ReviewContent rc9 = JsonSerializer.Deserialize<Level3ReviewContent>(File.ReadAllText(Res("Levels/Level_03/review.json")), json);
Level3CommandOutcome Stamp9(Level3ChapterCheckpoint s, string type) => Level3ReviewRules.Stamp(s, type, out _);
(Level3CommandOutcome outcome, Level3ReviewDecision decision, string reason) Submit9(Level3ChapterCheckpoint s)
{
    Level3CommandOutcome o = Level3ReviewRules.Submit(s, bea, rc9, out Level3ReviewDecision d, out string r);
    return (o, d, r);
}
Level3ReviewRecord Rev9(Level3ChapterCheckpoint s) => s.Case(B7).review;
const string R9 = Level3ReviewRules.StampReady, H9 = Level3ReviewRules.StampHold;

// ---------- the stamp: Ready for computation review / Not Ready, covering the current revision ----------
Level3ChapterCheckpoint st9 = pay8.Copy();
string pay8Snap9 = Snap(pay8);
int rev9 = Level3ComputationRules.Record(st9, B7).revision;
Check(Level3ReviewRules.Refusal(st9, B7) == Level3ReviewRules.RefuseNoStamp && Stamp9(st9, R9) == Level3CommandOutcome.Applied &&
      Level3ReviewRules.StampOf(st9, B7).type == R9 && Level3ReviewRules.StampOf(st9, B7).revision == rev9 && Level3ReviewRules.StampCurrent(st9, B7) &&
      Level3ComputationRules.Record(st9, B7).revision == rev9 && Snap(pay8) == pay8Snap9,
    "stamping READY covers the sheet's current revision on the candidate only; the stamp itself is not a sheet change; before it the Auditor asks for one");
Check(Stamp9(st9.Copy(), R9) == Level3CommandOutcome.Duplicate && Stamp9(st9.Copy(), "filed") == Level3CommandOutcome.Refused &&
      Stamp9(fr.Copy(), R9) == Level3CommandOutcome.Refused && Stamp9(acq.Copy(), H9) == Level3CommandOutcome.Applied,
    "the same stamp again is quiet; an unknown stamp or a sheet that is not open is refused; a hold can be stamped on any open sheet");
Level3ChapterCheckpoint stale9 = st9.Copy();
Level3ComputationRules.SelectCredit(stale9, bea, Level3ComputationRules.CertificateIncome, out _);
Level3ComputationRules.SelectCredit(stale9, bea, Level3ComputationRules.CertificateCredit, out _);
Check(Level3ReviewRules.StampStale(stale9, B7) && !Level3ReviewRules.StampCurrent(stale9, B7) && Level3ComputationRules.IsCurrent(stale9, bea, B7, "remaining-payable") &&
      Level3ReviewRules.StampOf(stale9, B7).type == R9,
    "any later change (even one changed back) makes the stamp out of date; the ink is kept, not replaced");

// ---------- refusals: nothing decided, nothing charged ----------
Level3ChapterCheckpoint closed9 = st9.Copy();
closed9.Case(B7).clock = new ReviewClockState { started = true, day = 1, remaining = 0f, cutoffPending = true };
Level3ChapterCheckpoint companyActive9 = st9.Copy();
string closed9Snap = Snap(closed9);
Check(Level3ReviewRules.Refusal(st9, "L3-MARINE-001") == Level3ReviewRules.RefuseWrongCase &&
      Level3ReviewRules.Refusal(closed9, B7) == Level3ReviewRules.RefuseClosed &&
      Level3ReviewRules.Submit(closed9, bea, rc9, out _, out string closed9Why) == Level3CommandOutcome.Refused && closed9Why == Level3ReviewRules.RefuseClosed &&
      Level3ReviewRules.Submit(st9.Copy(), marine, rc9, out _, out string wrong9) == Level3CommandOutcome.Refused && wrong9 == Level3ReviewRules.RefuseWrongCase &&
      Snap(closed9) == closed9Snap && Level3ReviewRules.Refusal(st9, B7) == null,
    "another case's folder, a closed review window or no stamp are refused before anything is recorded");

// ---------- approval: supported, current, stamped Ready ----------
Level3ChapterCheckpoint ok9 = st9.Copy();
var okRun9 = Submit9(ok9);
Level3ReviewRecord okRev9 = Rev9(ok9);
Check(okRun9.outcome == Level3CommandOutcome.Applied && okRun9.decision.Outcome == Level3ReviewOutcome.Approved && okRun9.decision.Root == "" &&
      okRev9.approved && okRev9.xp == 100 && okRev9.submissions == 1 && okRev9.returned == 0 && okRev9.chargedRoots.Count == 0 &&
      okRev9.taxableIncome == "600000" && okRev9.taxDue == "62500" && okRev9.credits == "12500.00" && okRev9.remainingPayable == "50000",
    "a READY stamp on the supported, current sheet is approved first time: case XP 100 and the approved sheet's own figures 600,000 / 62,500 / 12,500 / 50,000");
Check(Level3ChapterRules.PhaseOf(ok9) == Level3Phase.BeaDelivery && ok9.Case(B7).copyPhase == Level3ReviewRules.CopyAtAuditor &&
      ok9.milestones.Contains(Level3ReviewRules.ApprovedMilestone) && ok9.actionIds.Contains(Level3ReviewRules.SubmitAction) && ok9.actionIds.Contains(Level3ReviewRules.ApproveAction) &&
      Level3ChapterRules.Problem(ok9) == null && !Level3ComputationRules.Editable(ok9, B7) && Level3ReviewRules.CaseDone(ok9) &&
      ok9.activeCaseId == B7 && !ok9.Case("L3-MARINE-001").admitted && ok9.Case(B7).acquiredSourceIds.SequenceEqual(pay8.Case(B7).acquiredSourceIds),
    "approval moves BeaComputation -> BeaReview -> BeaDelivery with Bea's copy at the Auditor; her sheet is frozen (not editable); the company is untouched");
string ok9Snap = Snap(ok9);
Check(Level3ReviewRules.Submit(ok9, bea, rc9, out _, out string again9) == Level3CommandOutcome.Duplicate && again9 == Level3ReviewRules.RefuseApproved && Snap(ok9) == ok9Snap &&
      Stamp9(ok9.Copy(), H9) == Level3CommandOutcome.Refused && Level3ComputationRules.SelectRule(ok9.Copy(), bea, "IND-25", out _) == Level3CommandOutcome.Refused,
    "a second hand-in after approval is a quiet duplicate (no second result or copy); the approved sheet can no longer be stamped or edited");

// ---------- justified hold, premature approval, false hold, out-of-date stamp ----------
Level3ChapterCheckpoint open9 = link8.Copy();
Stamp9(open9, H9);
var hold9 = Submit9(open9);
Check(hold9.outcome == Level3CommandOutcome.Applied && hold9.decision.Outcome == Level3ReviewOutcome.SupportedHold && !hold9.decision.Returned &&
      hold9.decision.Root == "balance-operation" && hold9.decision.Field == "remaining-payable" && hold9.decision.Kind == Level3RootKind.Missing &&
      !hold9.decision.NewRoot && Rev9(open9).chargedRoots.Count == 0 && Rev9(open9).holds == 1 && Rev9(open9).lastOutcome == "hold" &&
      Level3ChapterRules.PhaseOf(open9) == Level3Phase.BeaComputation && Level3ComputationRules.Editable(open9, B7) && hold9.decision.Xp == 100,
    "NOT READY with an open line (no payable yet) is a justified hold: acknowledged, the open line named, nothing charged, the work stays open");
Check(Level3ReviewRules.Refusal(open9, B7) == Level3ReviewRules.RefuseUnchanged && Level3ReviewRules.Submit(open9.Copy(), bea, rc9, out _, out _) == Level3CommandOutcome.Refused,
    "the same sheet and stamp again is refused (no second decision, no charge)");
Level3ChapterCheckpoint early9 = link8.Copy();
Stamp9(early9, R9);
var prem9 = Submit9(early9);
Check(prem9.decision.Outcome == Level3ReviewOutcome.PrematureApproval && prem9.decision.Returned && prem9.decision.Root == "balance-operation" &&
      prem9.decision.NewRoot && Rev9(early9).chargedRoots.SequenceEqual(new[] { "balance-operation" }) && Rev9(early9).returned == 1 && prem9.decision.Xp == 98 &&
      prem9.decision.Note.StartsWith("Remaining payable is still empty.") && Level3ChapterRules.PhaseOf(early9) == Level3Phase.BeaComputation,
    "READY over an open line is a premature approval: returned, the root charged once (98 XP), the note names the line and its book section");
Level3ChapterCheckpoint falseHold9 = pay8.Copy();
Stamp9(falseHold9, H9);
var fh9 = Submit9(falseHold9);
Check(fh9.decision.Outcome == Level3ReviewOutcome.FalseHold && fh9.decision.Root == Level3ReviewRules.StampDecisionRoot && fh9.decision.Field == "stamp" &&
      Rev9(falseHold9).chargedRoots.SequenceEqual(new[] { "stamp-decision" }) && !Level3ReviewRules.CaseDone(falseHold9),
    "NOT READY on a fully supported sheet is a false hold: returned, charged as a stamp decision");
var stale9Run = Submit9(stale9.Copy());
Check(stale9Run.decision.Outcome == Level3ReviewOutcome.StaleStamp && stale9Run.decision.Root == Level3ReviewRules.StampRevisionRoot && stale9Run.decision.Returned,
    "an out-of-date stamp is returned first (stamp-revision), even on a correct sheet");

// ---------- the earliest root only; descendants are never charged ----------
string Root9(Level3ChapterCheckpoint s) { Level3ChapterCheckpoint c = s.Copy(); Stamp9(c, R9); var run = Submit9(c); return run.decision.Root + "/" + run.decision.Kind + "/" + Rev9(c).chargedRoots.Count; }
Level3ChapterCheckpoint draft9 = draft7.Copy();
Check(Root9(draft9) == "expense-sum/Wrong/1" && Root9(whole7) == "tax-formula/Wrong/1" && Root9(wrongCredit8) == "credit-source/Wrong/1" &&
      Root9(typedPay8) == "balance-operation/Typed/1" && Root9(band8) == "rule-selection/Wrong/1" && Root9(rev7) == "taxable-operation/Wrong/1",
    "the review stops at the earliest open line: Bea's draft costs (expense-sum, not its taxable/tax dependents), whole-base tax, the income payment as credit, a typed balance, a stale band change, expenses minus gross");
Level3ChapterCheckpoint both9 = both8.Copy();
Stamp9(both9, R9);
var both9Run = Submit9(both9);
Check(both9Run.decision.Root == "credit-source" && both9Run.decision.Kind == Level3RootKind.AsExpense && both9Run.decision.Field == "taxable-income" &&
      both9Run.decision.Note == rc9.roots.Find(r => r.id == "credit-as-expense").note && Rev9(both9).chargedRoots.Count == 1,
    "the credit used as an expense (bea-credit-both) is one credit-source root at taxable income, not four arithmetic mistakes");
foreach (Level3ReviewRootNote note in rc9.roots)
    Check(!new[] { "600,000", "62,500", "50,000", "12,500", "900,000", "300,000", "400,000", "250,000", "IND-" }.Any(note.note.Contains), $"root note '{note.id}' names the line and the book section, never a value");

// ---------- retries, unchanged hand-ins and the XP cap ----------
Level3ChapterCheckpoint seq9 = link8.Copy();
Stamp9(seq9, R9);
Submit9(seq9);
string seq9Snap = Snap(seq9);
Check(Level3ReviewRules.Submit(seq9, bea, rc9, out _, out string same9) == Level3CommandOutcome.Refused && same9 == Level3ReviewRules.RefuseUnchanged && Snap(seq9) == seq9Snap,
    "after a returned review the same sheet and stamp are refused: no retry, no second charge");
Stamp9(seq9, H9);
var seqHold9 = Submit9(seq9);
Check(seqHold9.decision.Outcome == Level3ReviewOutcome.SupportedHold && seqHold9.decision.Retry && Rev9(seq9).retries == 1 && Rev9(seq9).chargedRoots.Count == 1,
    "a changed hand-in after a return counts one retry (a hold now charges nothing new)");
Pay8(seq9, c => Due8(c, seq9), out _);
Stamp9(seq9, R9);
var seqOk9 = Submit9(seq9);
Check(seqOk9.decision.Outcome == Level3ReviewOutcome.Approved && !seqOk9.decision.Retry && Rev9(seq9).xp == 97 && Rev9(seq9).submissions == 3,
    "corrected and stamped: approved at 100 - 2 (one root) - 1 (one retry) = 97 XP; the hand-in after a hold is no retry");
var capped9 = new Level3ReviewRecord { retries = 4, late = true };
for (int i = 0; i < 9; i++) capped9.chargedRoots.Add("root-" + i);
Check(Level3ReviewRules.CaseXp(capped9, 2) == 85 && Level3ReviewRules.CaseXp(new Level3ReviewRecord { late = true }, 2) == 98 &&
      Level3ReviewRules.CaseXp(new Level3ReviewRecord(), rc9.Clock(B7).lateExp) == 100,
    "case XP = 100 - min(15, 2 x roots + retries + late): never below 85; the late mark is the one punctuality loss inside the cap");

// ---------- the 100 coins and the case mastery claim, once; no trust, no chapter completion ----------
PlayerProgressData pay9 = Level3ChapterRules.WithCheckpoint(Clone(live), ok9);
int coins9 = pay9.campaign.coins, trust9 = pay9.campaign.publicTrust;
Level3ReviewGrants g9 = Level3ReviewRules.ApplyApprovalGrants(pay9, ok9);
Check(g9.Changed && g9.Coins.Applied && g9.Coins.Amount == 100 && pay9.campaign.coins == coins9 + 100 && pay9.campaign.publicTrust == trust9 && g9.MasteryClaimed &&
      g9.Xp == 100 && pay9.campaign.rewardClaims.Contains(Level3ReviewRules.MasteryClaimId(pay9)) &&
      CampaignRewards.IsActivityCompleted(pay9, CampaignRewards.Level3, CampaignRewards.BeaCaseApproved),
    "approval pays the catalogue's 100 coins and records the case mastery claim, and no Public Trust");
Level3ReviewGrants g9b = Level3ReviewRules.ApplyApprovalGrants(pay9, ok9);
Check(g9b.AlreadyClaimed && !g9b.Changed && pay9.campaign.coins == coins9 + 100 && !pay9.campaign.IsChapterCompleted("Level_03") &&
      CampaignRewards.Amount(CampaignRewards.Level3, CampaignRewards.BeaCaseApproved, CampaignRewards.CoinsKind) == 100 &&
      CampaignRewards.Amount(CampaignRewards.Level3, CampaignRewards.BeaCaseApproved, CampaignRewards.TrustKind) == 0 &&
      Level3ReviewRules.ApplyApprovalGrants(Level3ChapterRules.WithCheckpoint(Clone(live), open9), open9).Coins.Amount == 0,
    "a repeat, replay or reload never pays again; the chapter is not completed; an unapproved review pays nothing");

// ---------- Bea's review clock: 16 active minutes once her record work is independent, pauses, free next morning, one voucher ----------
ReviewClockDefinition clock9 = rc9.Clock(B7);
Check(clock9 != null && clock9.allowanceSeconds == 960f && clock9.warnSeconds.SequenceEqual(new[] { 120f, 30f }) && clock9.lateExp == 2 && clock9.lateHearts == 0 &&
      !Level3ReviewRules.ClockDue(assigned) && !Level3ReviewRules.ClockDue(beaRecords) && Level3ReviewRules.ClockDue(pay8) && !Level3ReviewRules.ClockDue(ok9),
    "revision: Bea's review gets 16 active minutes, not at assignment or her records, and never after approval");
Level3ChapterCheckpoint clk9 = pay8.Copy();
Check(Level3ReviewRules.StartClock(clk9, rc9, out _) == Level3CommandOutcome.Applied && clk9.Case(B7).clock.started && clk9.Case(B7).clock.remaining == 960f &&
      Level3ReviewRules.StartClock(clk9, rc9, out _) == Level3CommandOutcome.Duplicate && clk9.Case("L3-MARINE-001").clock.started == false &&
      clk9.actionIds.Contains(Level3ReviewRules.ClockStartAction),
    "it starts once (a repeat is quiet) and belongs to Bea's case only (the company's clock is separate)");
ReviewClockState c9 = clk9.Case(B7).clock;
Check(ReviewClock.Advance(c9, clock9, 100f, true, out _) == ReviewClock.Tick.None && c9.remaining == 960f &&
      ReviewClock.Advance(c9, clock9, 841f, false, out float mark9) == ReviewClock.Tick.Warning && mark9 == 120f && ReviewClock.Advance(c9, clock9, 200f, false, out _) == ReviewClock.Tick.Cutoff &&
      c9.cutoffPending && Level3ReviewRules.Refusal(clk9, B7) != null,
    "a paused clock never moves; working time crosses the 2-minute warning, then the cutoff closes today's hand-ins");
Check(Level3ReviewRules.ExtendedClock(clk9) == null, "a voucher cannot bring back a closed window");
Level3ChapterCheckpoint morning9 = clk9.Copy();
string morningWork9 = Snap(morning9.tapes) + Snap(morning9.Case(B7).computation);
Check(Level3ReviewRules.Rollover(morning9, rc9, out bool first9, out _) == Level3CommandOutcome.Applied && first9 && morning9.Case(B7).clock.day == 2 &&
      morning9.Case(B7).clock.remaining == 960f && !morning9.Case(B7).clock.cutoffPending && Rev9(morning9).late &&
      Snap(morning9.tapes) + Snap(morning9.Case(B7).computation) == morningWork9 && Level3ReviewRules.Refusal(morning9, B7) == Level3ReviewRules.RefuseNoStamp,
    "the cutoff rolls to a free next morning: day 2, a fresh 16 minutes, every tape and choice kept; the first miss marks the review late once");
ReviewClock.Advance(morning9.Case(B7).clock, clock9, 960f, false, out _);
Check(Level3ReviewRules.Rollover(morning9, rc9, out bool second9, out _) == Level3CommandOutcome.Applied && !second9 && morning9.Case(B7).clock.day == 3 &&
      Level3ReviewRules.CaseXp(Rev9(morning9), clock9.lateExp) == 98 && Level3ReviewRules.Rollover(morning9.Copy(), rc9, out _, out _) == Level3CommandOutcome.Refused,
    "a later missed day adds nothing more; nothing rolls without a closed window");
Level3ChapterCheckpoint ext9 = pay8.Copy();
Level3ReviewRules.StartClock(ext9, rc9, out _);
ReviewClockState extended9 = Level3ReviewRules.ExtendedClock(ext9);
Check(extended9 != null && extended9.remaining == 960f + VoucherRules.ExtensionSeconds && extended9.extensionSeconds == 180 && extended9.extensionDay == 1 &&
      ext9.Case(B7).clock.remaining == 960f && ReviewClock.Extend(extended9, VoucherRules.ExtensionSeconds) == null,
    "one voucher adds 180 active seconds to today's window (a copy until saved); the same day cannot take another");
CampaignRecord vc9 = Clone(live).campaign;
vc9.inventory = vc9.inventory ?? new CampaignInventoryRecord();
string beaVoucher9 = VoucherRules.UseClaimId(vc9.campaignId, "Level_03", B7);
Check(beaVoucher9 != VoucherRules.UseClaimId(vc9.campaignId, "Level_02", "L2-ROSA-001") && beaVoucher9 != VoucherRules.UseClaimId(vc9.campaignId, "Level_03", "L3-MARINE-001") &&
      beaVoucher9.Contains("Level_03") && beaVoucher9.Contains(B7),
    "Bea's voucher claim is her own (Level_03 + her case id): separate from Rosa's and from the company's");
VoucherClockView view9 = ReviewClock.VoucherView(ext9.Case(B7).clock, clock9, false);
Check(VoucherRules.Check(1, B7, view9, false) == VoucherUseStatus.Ready && VoucherRules.Check(1, B7, view9, true) == VoucherUseStatus.AlreadyUsed &&
      VoucherRules.Check(1, B7, ReviewClock.VoucherView(ext9.Case(B7).clock, clock9, true), false) == VoucherUseStatus.CaseDone &&
      VoucherRules.Check(0, B7, view9, false) == VoucherUseStatus.NoVoucher,
    "the voucher card reads Bea's clock: ready with one carried, refused once used on her case, after approval, or with none carried");

// ---------- persistence: round trip, old saves, failed approval write ----------
PlayerProgressData save9 = Level3ChapterRules.WithCheckpoint(Clone(live), ok9);
save9.Normalize();
PlayerProgressData back9 = JsonSerializer.Deserialize<PlayerProgressData>(Snap(save9), json);
Check(!back9.Normalize() && Level3ChapterRules.Decide(back9.campaign.levelThree) == Level3EntryDecision.Resume &&
      Level3ChapterRules.PhaseOf(back9.campaign.levelThree) == Level3Phase.BeaDelivery && Rev9(back9.campaign.levelThree).approved &&
      Rev9(back9.campaign.levelThree).xp == 100 && back9.campaign.levelThree.Case(B7).copyPhase == "atAuditor" &&
      Level3ReviewRules.StampOf(back9.campaign.levelThree, B7).type == R9 && Level3ReviewRules.Refusal(back9.campaign.levelThree, B7) == Level3ReviewRules.RefuseApproved,
    "the approval, its frozen XP and figures, the stamp and the copy at the Auditor round-trip without repair; a reload cannot hand it in again");
PlayerProgressData saveClock9 = Level3ChapterRules.WithCheckpoint(Clone(live), morning9);
saveClock9.Normalize();
PlayerProgressData backClock9 = JsonSerializer.Deserialize<PlayerProgressData>(Snap(saveClock9), json);
Check(!backClock9.Normalize() && backClock9.campaign.levelThree.Case(B7).clock.day == 3 && Rev9(backClock9.campaign.levelThree).late &&
      backClock9.campaign.levelThree.Case(B7).clock.started,
    "the clock (day, remaining, the late mark) round-trips with the chapter");
JsonNode oldT08 = JsonNode.Parse(Snap(Level3ChapterRules.WithCheckpoint(Clone(live), pay8)));
foreach (JsonNode caseNode9 in oldT08["campaign"]["levelThree"]["cases"].AsArray())
    foreach (string key in new[] { "stamp", "review", "clock" }) caseNode9.AsObject().Remove(key);
PlayerProgressData oldT08Back = JsonSerializer.Deserialize<PlayerProgressData>(oldT08.ToJsonString(), json);
oldT08Back.Normalize();
Check(Level3ChapterRules.Decide(oldT08Back.campaign.levelThree) == Level3EntryDecision.Resume && Rev9(oldT08Back.campaign.levelThree) != null &&
      Level3ReviewRules.StampOf(oldT08Back.campaign.levelThree, B7).type == "" && Level3ReviewRules.ClockDue(oldT08Back.campaign.levelThree) &&
      Facts7(oldT08Back.campaign.levelThree).PayableSupported,
    "a T08 save without stamp, review or clock resumes with its sheet intact, unstamped, and its clock still to start");
string review9Snap = Snap(Rev9(seq9)) + Snap(Level3ReviewRules.StampOf(seq9, B7));
foreach (string leak in new[] { "expected", "taxWithheldOrCredits", "finalTaxPayable", "worksheetDecisions" })
    Check(!review9Snap.Contains(leak), $"the saved review and stamp hold no answer key ('{leak}')");
string sandbox9 = Path.Combine(Path.GetTempPath(), "TaxVentureL3T09-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox9);
try
{
    string Encode9(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var store9 = new SaveFileStore(Path.Combine(sandbox9, "progress.json"));
    PlayerProgressData pub9 = Level3ChapterRules.WithCheckpoint(Clone(live), st9.Copy());
    pub9.Normalize();
    store9.Write(Encode9(pub9));
    byte[] disk9 = File.ReadAllBytes(store9.FilePath);
    string pub9Snap = Snap(pub9);
    Level3ChapterCheckpoint cand9 = pub9.campaign.levelThree.Copy();
    Level3ReviewRules.Submit(cand9, bea, rc9, out _, out _);
    Directory.CreateDirectory(store9.TempPath);
    bool wrote9 = false;
    try
    {
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub9), cand9);
        Level3ReviewRules.ApplyApprovalGrants(next, cand9);
        next.Normalize();
        store9.Write(Encode9(next));
        pub9 = next;
        wrote9 = true;
    }
    catch (Exception) { }
    Check(!wrote9 && Snap(pub9) == pub9Snap && File.ReadAllBytes(store9.FilePath).SequenceEqual(disk9) && !Level3ReviewRules.CaseDone(pub9.campaign.levelThree) &&
          pub9.campaign.coins == live.campaign.coins && Level3ReviewRules.Refusal(pub9.campaign.levelThree, B7) == null,
        "a failed approval write keeps the unapproved review, the coins and the disk as they were; the folder can be handed in again");
    Directory.Delete(store9.TempPath);
}
finally
{
    try { Directory.Delete(sandbox9, true); } catch (IOException) { }
}

// ---------- resume observers, game state guard ----------
Check(Level3ChapterRules.ImpliedTriggers(ok9).Contains(Level3ChapterRules.ApprovedTrigger) && !Level3ChapterRules.ImpliedTriggers(open9).Contains(Level3ChapterRules.ApprovedTrigger) &&
      Level3ChapterRules.ImpliedLessons(st9).Contains(Level3ChapterRules.ReviewStampLesson) && !Level3ChapterRules.ImpliedLessons(pay8).Contains(Level3ChapterRules.ReviewStampLesson) &&
      Level3ChapterRules.ImpliedLessons(open9).Contains(Level3ChapterRules.ReviewHandInLesson) && Level3ChapterRules.ImpliedLessons(ok9).Contains(Level3ChapterRules.ReviewApprovedLesson),
    "resume: a saved approval keeps its objective ticked; each review lesson the saved state already shows taught is skipped");
Check(!Level3ChapterRules.TransitionAllowed(Level3Phase.BeaDelivery, "StampAssessmentState") && !Level3ChapterRules.TransitionAllowed(Level3Phase.BeaReview, "AnalyzeEvidenceState") &&
      !Level3ChapterRules.TransitionAllowed(Level3Phase.BeaDelivery, "LevelCompleteState") && !Level3ChapterRules.TransitionAllowed(Level3Phase.BeaDelivery, "CaseCompleteState") &&
      Level3ChapterRules.TransitionAllowed(Level3Phase.CompanyAppointment, "StampAssessmentState"),
    "after approval the inherited Level 1 stamp set and corkboard still cannot move the game state, and the legacy completion stays refused");

// ---------- content: review lines, book, objective, guide ----------
Check(rc9.auditor.approved.SequenceEqual(new[] { "Approved for this computation review. Give Bea her copy and explain the balance." }) &&
      rc9.auditor.returned == "Your work is kept. Start with the line I marked, then check the calculations that depend on it." &&
      new[] { "income-sum", "expense-sum", "taxable-operation", "rule-selection", "tax-formula", "credit-source", "credit-as-expense", "balance-operation", "stamp-revision", "stamp-decision" }
          .All(id => rc9.roots.Exists(r => r.id == id)),
    "review.json carries CONTENT's Auditor lines and a note for every FIXTURE root (plus the credit-as-expense note)");
var book9 = Json("Levels/Level_03/book.json").GetProperty("sections").EnumerateArray().ToList();
JsonElement leaf9 = book9.First(s => s.GetProperty("heading").GetString().StartsWith("Section 10"));
string leaf9Text = leaf9.GetProperty("body").GetString() + " " + string.Join(" ", leaf9.GetProperty("clickableValues").EnumerateArray().Select(v => v.GetProperty("displayText").GetString()));
Check(book9.Count == 8 && leaf9.GetProperty("heading").GetString() == "Section 10 – Checking a Computation" &&
      leaf9.GetProperty("body").GetString().Split("\n\n").Length == leaf9.GetProperty("clickableValues").GetArrayLength() &&
      leaf9Text.Contains("does not file a return or pay tax") && leaf9Text.Contains("Use Not Ready while a line needs work") &&
      !new[] { "600,000", "62,500", "50,000", "12,500", "Bea" }.Any(leaf9Text.Contains),
    "book.json adds Section 10 – Checking a Computation (CONTENT leaf): check in order, Ready is for computation review not filing/paying, an honest hold, stamp the current sheet; no client figure");
var steps9 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps9.Count == 14 && steps9[8].GetProperty("trigger").GetString() == Level3ChapterRules.ApprovedTrigger &&
      ObjectiveTriggers.Validate(Level3ChapterRules.ApprovedTrigger) == null && ObjectiveTriggers.Events.Contains(ObjectiveTriggers.BeaCaseApproved),
    "objective 9 (revision; was 10): stamp Bea's computation and hand her folder to the Auditor (raised only from a saved approval); T10 appends pickup and delivery, T14 the closing three");
GuideScript guide9 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var t09Lessons = new[] { Level3ChapterRules.ReviewStampLesson, Level3ChapterRules.ReviewHandInLesson, Level3ChapterRules.ReviewReturnedLesson, Level3ChapterRules.ReviewApprovedLesson }
    .Select(id => guide9.lessons.FirstOrDefault(l => l.id == id)).ToList();
Check(guide9.Validate() == "" && t09Lessons.All(l => l != null && l.pages.All(p => p.IsHint)) && t09Lessons[0].pages[0].waitFor == "flag:beaStamped|cue:" + Level3DeskCalculator.CueSheetStamped &&
      t09Lessons[1].when == "flag:beaStamped" && t09Lessons[2].when == "flag:beaReviewReturned" &&
      !t09Lessons.SelectMany(l => l.pages).Any(p => p.text.Contains("50,000") || p.text.Contains("62,500") || p.text.Contains("click", StringComparison.OrdinalIgnoreCase)) &&
      !guide9.waypoints.Any(w => (w.target ?? "").Contains("Auditor")),
    "the four review lessons are hints waiting for the player's own stamp and hand-in; no value, touch-only words, no arrow to the Auditor (a known place)");

// ---------- wiring (source checks; Unity behaviour is the user's) ----------
string cp9 = Src("Scripts/Level3/Level3Checkpointer.cs");
string commit9 = Body6(cp9, "public ReviewCommit CommitReview(", "/// <summary>Bea's clock settings");
Check(Index(commit9, "State.Copy()") < Index(commit9, "Level3ReviewRules.Submit(candidate") && Index(commit9, "Level3ReviewRules.Submit(candidate") < Index(commit9, "CommitLevelThreeApproval(candidate") &&
      Index(commit9, "CommitLevelThreeApproval(candidate") < Index(commit9, "Publish(candidate);") && commit9.Contains("return ReviewCommit.SaveFailed;") &&
      Index(commit9, "return ReviewCommit.SaveFailed;") < Index(commit9, "Publish(candidate);") &&
      cp9.Contains("if (!wasApproved && Level3ReviewRules.CaseDone(committed)) GameplayEvents.RaiseBeaCaseApproved();"),
    "a hand-in: candidate, the signed write (an approval together with its coins), then publish; the objective event only from the saved approval");
string ppm9 = Src("Scripts/RewardsSystem/PlayerProgressManager.cs");
string approve9 = Body6(ppm9, "public bool CommitLevelThreeApproval(", "/// <summary>");
Check(Index(approve9, "Level3ReviewRules.ApplyApprovalGrants(candidate, state)") < Index(approve9, "TryWriteCandidate(candidate)") && approve9.Contains("if (!UsesLevelThreeStory) return false;") &&
      ppm9.Contains("CommitVoucher(items, Level3ChapterRules.LevelId, caseId, actionId"),
    "the approval's grants go into the same candidate before its one write; a Level 3 voucher uses the existing voucher transaction");
string audit9 = Src("Scripts/Level3/Level3Audit.cs");
Check(audit9.Contains("AuditorInteractable.ILevelHandIn, AuditorInteractable.ILevelFolder") && audit9.Contains("owner.CommitReview(") && !audit9.Contains("CompleteLevelReview") &&
      !audit9.Contains("RecordAuditAttempt") && !audit9.Contains("CalculateAndStoreCaseReward") && audit9.Contains("auditor.ReturnFolderForReview()") &&
      audit9.Contains("InventoryRules.InHotbar(inventory, InventoryCatalogue.CaseFolder)"),
    "Level 3's hand-in is the existing talk-first Auditor conversation with the carried folder; it never runs Level 1's completion or score ledger");
string auditor9 = Src("Scripts/Interactables/AuditorInteractable.cs");
Check(auditor9.Contains("public interface ILevelFolder") && auditor9.Contains("FolderReadyToHand()") &&
      auditor9.Contains("LevelHandIn is Level3Audit ? Level2FolderHandoff.FindStore(gameObject.scene)") && auditor9.Contains("if (folder != null) folder.ReleaseQuietly();"),
    "the Auditor's narrow adapter: a level folder (Level 3) or a stored desk folder (Levels 1-2), the rest of his conversation unchanged");
string clockSrc9 = Src("Scripts/Level3/Level3ReviewClock.cs");
Check(clockSrc9.Contains("Level3ReviewClock : MonoBehaviour, IReviewCutoffClock") && clockSrc9.Contains("OfficeHoursChip.Create(this)") &&
      clockSrc9.Contains("Level2ReviewClock.ClockPaused") && clockSrc9.Contains("Level3ReviewRules.StartClock(c,") && clockSrc9.Contains("Level3ReviewRules.Rollover(c,") &&
      clockSrc9.Contains("OfficeMorningOverlay.Fade(") && !clockSrc9.Contains("CaseExtraHeartLoss") && !clockSrc9.Contains("publicTrust"),
    "Bea's clock reuses the review clock rules, its pauses, the chip and the next-morning overlay; its start and rollover are saved commands; time never touches trust");
string voucher9 = Src("Scripts/UI/HUD/VoucherUseCard.cs");
string chip9 = Src("Scripts/UI/HUD/OfficeHoursChip.cs");
Check(voucher9.Contains("PlayerProgressManager.Instance.CommitLevelThreeVoucherUse(hotbar.Catalogue, ActiveCaseId, chapter,") && voucher9.Contains("manager.IsVoucherUsed(rosa.LevelId, caseId)") &&
      voucher9.Contains("CommitLevelTwoVoucherUse(") && chip9.Contains("public interface IReviewCutoffClock") && chip9.Contains("private IReviewCutoffClock reviewClock;") &&
      Src("Scripts/Level2/Level2ReviewClock.cs").Contains("Level2ReviewClock : MonoBehaviour, IReviewCutoffClock"),
    "the chip and the voucher card read either case clock through one small interface; Rosa's path is unchanged");
string stamps9 = Src("Scripts/Level3/Level3DeskCalculatorReview.cs");
string folderCommit9 = Body6(Src("Scripts/Level3/Level3CaseFolder.cs"), "public bool Commit(StampType type, Vector2 local, float tilt, int page)", "private void DrawSavedInk()");
Check(Index(folderCommit9, "owner.CommitComputation(") < Index(folderCommit9, "DrawSavedInk();") && folderCommit9.Contains("Level3ReviewRules.StampAt(c, kind, page,") &&
      Index(folderCommit9, "if (result != Level3Checkpointer.TapeCommit.Attached)") < Index(folderCommit9, "DrawSavedInk();") &&
      !stamps9.Contains("BuildStamp") && !stamps9.Contains("class DeskStamp") && !stamps9.Contains("CommitLevelThreeCheckpoint"),
    "T09 revision: the shared stamp set's contact commits through the folder adapter (saved first, ink only after); the procedural L3 stamp bodies are gone");
string leaf9Src = Src("Scripts/Level3/Level3ComputationLeafModel.cs");
Check(leaf9Src.Contains("HitKind.StampBox") && leaf9Src.Contains("\"READY FOR\\nCOMPUTATION REVIEW\"") && leaf9Src.Contains("\"NOT READY\"") &&
      leaf9Src.Contains("\"STAMP OUT OF DATE\"") && leaf9Src.Contains("\"AUDITOR: START HERE\""),
    "Bea's sheet has a stamp box with both inks and an out-of-date tag, and the Auditor's START HERE mark for the returned line");
string desk9 = Src("Scripts/Level3/Level3DeskCalculator.cs");
Check(!desk9.Contains("ActivateStamp(") && Src("Scripts/Level3/Level3OfficeStart.cs").Contains("EnsurePart<Level2DeskStamps>(Level2DeskStamps.ChildName)") && !desk9.Contains("CaptureStamps(") &&
      !desk9.Contains("ComputeStampPoses(") && !Src("Scripts/Level3/Level3DeskCalculatorComputation.cs").Contains("TapStampBox()"),
    "T09 revision: one stamp input owner (the shared Level2DeskStamps on Level3Gameplay); the calculator no longer lifts or presses stamps of its own");
string start9 = Src("Scripts/Level3/Level3OfficeStart.cs");
Check(start9.Contains("EnsurePart<Level3Audit>(Level3Audit.ChildName);") && start9.Contains("EnsurePart<Level3ReviewClock>(Level3ReviewClock.ChildName);"),
    "the review and the clock are runtime parts of Level3Gameplay (no scene change)");
foreach (string file in new[] { "Level3ReviewRules.cs", "Level3Audit.cs", "Level3ReviewClock.cs", "Level3DeskCalculatorReview.cs" })
{
    string code9 = System.Text.RegularExpressions.Regex.Replace(Src("Scripts/Level3/" + file), @"///.*|//.*", "");
    Check(!code9.Contains("publicTrust") && !code9.Contains("TrustKind") && !code9.Contains("Ethics") && !code9.Contains("expectedFields") && !code9.Contains("float.Parse") &&
          !code9.Contains("MarkChapterCompleted"),
        $"{file}: no trust, ethics, chapter completion or answer key");
}
Console.WriteLine($"T09 checks: {checks - t09Start}, failures {fails - t09Fails} (stamp, refusals, outcomes, earliest root, retries/cap, coins once, clock/rollover/voucher, save/failure, resume, content, wiring; Unity rendering, touch and feel not run).");

// =====================================================================
// T02 revision (3 November 2026): fresh entry without the budget practice,
// version-aware continuation of every earlier chapter snapshot
// =====================================================================
int t02rStart = checks, t02rFails = fails;
const string LegacyAssessment = "L3-worksheet-v1";
// A pre-revision snapshot as it was written: version 1, without the revision's fields.
Level3ChapterCheckpoint LegacyOf(Level3ChapterCheckpoint s)
{
    Level3ChapterCheckpoint c = V1(s);
    c.worksheetCheck.assessmentVersion = "";
    c.calculatorTaught = false;
    return c;
}
// The continued snapshot with the revision's own fields put back to version 1: must equal the legacy one exactly.
string AsLegacy(Level3ChapterCheckpoint c)
{
    Level3ChapterCheckpoint x = c.Copy();
    x.version = Level3ChapterCheckpoint.LegacyVersion;
    x.worksheetCheck.assessmentVersion = "";
    x.calculatorTaught = false;
    return Snap(x);
}

// ---------- fresh revised route: reception -> Bea's records, no practice, clock after the first lesson ----------
Check(Level3ChapterCheckpoint.CurrentVersion == 2 && Level3ChapterCheckpoint.LegacyVersion == 1 && entry.version == 2 && !entry.calculatorTaught &&
      entry.worksheetCheck.assessmentVersion == "", "a fresh chapter is written as version 2, untaught, with no assessment recorded");
Level3ChapterCheckpoint fresh2 = entry.Copy();
Check(Level3ChapterRules.AssignBea(fresh2, out _) == Level3CommandOutcome.Applied && fresh2.phase == "BeaRecords" && fresh2.tapes.Count == 0 &&
      !fresh2.milestones.Contains(Level3PracticeRules.CompleteMilestone) && !fresh2.actionIds.Contains(Level3PracticeRules.CompleteAction) &&
      !fresh2.milestones.Contains(Level3ChapterRules.PracticeRetiredMilestone) && Level3ChapterRules.Problem(fresh2) == null,
    "fresh: the assignment goes straight to Bea's records; no practice completion is invented");
Check(!Level3ReviewRules.ClockDue(fresh2) && !Level3ChapterRules.BeaWorkIndependent(fresh2), "fresh: Bea's clock does not start at the assignment");
Level3ChapterCheckpoint freshHanded = fresh2.Copy();
Check(Level3ChapterRules.CollectBeaRecords(freshHanded, out _) == Level3CommandOutcome.Applied && freshHanded.phase == "WorksheetDraft" &&
      Level3ChapterRules.BeaHandoverSources.All(freshHanded.Case(B7).acquiredSourceIds.Contains),
    "fresh: Bea hands over her records from the revised BeaRecords phase");
Check(!Level3ReviewRules.ClockDue(freshHanded), "fresh: her clock still waits while the first worksheet lesson is compulsory");
Level3WorksheetRules.Extract(freshHanded, wsRows, "R1", out _);
Check(!Level3ReviewRules.ClockDue(freshHanded), "fresh: lifting a strip is still the lesson");
Check(Level3WorksheetRules.Place(freshHanded, "R1", "income", out _) == Level3CommandOutcome.Applied && Level3ChapterRules.BeaWorkIndependent(freshHanded) &&
      Level3ReviewRules.ClockDue(freshHanded),
    "fresh: once a row is placed (the lesson's last step) her work is independent and the clock is due");
Level3ChapterCheckpoint freshClock = freshHanded.Copy();
Check(Level3ReviewRules.StartClock(freshClock, rc9, out _) == Level3CommandOutcome.Applied && freshClock.Case(B7).clock.started &&
      freshClock.Case(B7).clock.remaining == 960f && Level3ReviewRules.StartClock(freshClock, rc9, out _) == Level3CommandOutcome.Duplicate,
    "fresh: her 16 active minutes start once and never restart");

// ---------- legacy fixtures at every documented boundary ----------
Level3ChapterCheckpoint approvedClock = ok9.Copy();
approvedClock.Case(B7).clock = new ReviewClockState { started = true, day = 2, remaining = 37.25f, warned = new List<float> { 120f, 30f }, missed = true, extensionSeconds = 180, extensionDay = 2 };
Level3ChapterCheckpoint companyFixture = approvedClock.Copy();
companyFixture.phase = "CompanyComputation";
companyFixture.activeCaseId = "L3-MARINE-001";
companyFixture.Case(B7).copyPhase = "done";
companyFixture.Case("L3-MARINE-001").admitted = true;
companyFixture.Case("L3-MARINE-001").acquiredSourceIds.AddRange(new[] { "C-ID", "C-REV" });
Level3ChapterCheckpoint completedFixture = companyFixture.Copy();
completedFixture.phase = "CompletedOffice";
completedFixture.Case("L3-MARINE-001").copyPhase = "done";
completedFixture.milestones.Add("wrap-up-acknowledged");
Level3ChapterCheckpoint frCorrecting = r6Wrong.Copy();
Level3WorksheetCheck.Check(frCorrecting, bea, out _, out _);
Level3ChapterCheckpoint runningClock = pay8.Copy();
Level3ReviewRules.StartClock(runningClock, rc9, out _);
ReviewClock.Advance(runningClock.Case(B7).clock, clock9, 301.5f, false, out _);
var boundaries = new (string what, Level3ChapterCheckpoint legacy, Level3Phase phase)[]
{
    ("reception", LegacyOf(entry), Level3Phase.Reception),
    ("Bea's records after the practice", LegacyOf(legacyPracticed), Level3Phase.BeaRecords),
    ("Bea's records handed over", LegacyOf(handed), Level3Phase.WorksheetDraft),
    ("a worksheet draft", LegacyOf(wsWrong), Level3Phase.WorksheetDraft),
    ("a frozen twelve-decision first check, correcting", LegacyOf(frCorrecting), Level3Phase.WorksheetCorrections),
    ("a checked worksheet ready to collect", LegacyOf(fr), Level3Phase.WorksheetReady),
    ("a collected worksheet (Bea's computation)", LegacyOf(acq), Level3Phase.BeaComputation),
    ("a computation with its tapes", LegacyOf(s7), Level3Phase.BeaComputation),
    ("a complete computation with the credit, clock running", LegacyOf(runningClock), Level3Phase.BeaComputation),
    ("a stamped sheet", LegacyOf(st9), Level3Phase.BeaComputation),
    ("Bea approved, her copy at the Auditor", LegacyOf(approvedClock), Level3Phase.BeaDelivery),
    ("a future-compatible company computation", LegacyOf(companyFixture), Level3Phase.CompanyComputation),
    ("a future-compatible completed office", LegacyOf(completedFixture), Level3Phase.CompletedOffice)
};
foreach (var (what, legacy, phase) in boundaries)
{
    string legacyBefore = Snap(legacy);
    Check(Level3ChapterRules.Decide(legacy) == Level3EntryDecision.Continue && Level3ChapterRules.Problem(legacy) != null,
        $"legacy {what}: recognised as version 1 and continued, never resumed raw or blocked");
    bool ok = Level3ChapterRules.TryContinueLegacy(legacy, out Level3ChapterCheckpoint contR, out string why);
    Check(ok && why == null && contR.version == 2 && Level3ChapterRules.PhaseOf(contR) == phase && Snap(legacy) == legacyBefore,
        $"legacy {what}: continued on a copy at {phase} (the saved snapshot is untouched)");
    if (!ok) continue;
    Check(AsLegacy(contR) == legacyBefore,
        $"legacy {what}: every phase, action, milestone, case paper, draft, check, tape, computation, stamp, review, copy and clock is carried exactly");
    Check(contR.worksheetCheck.assessmentVersion == (legacy.worksheetCheck.firstChecked ? LegacyAssessment : "") &&
          contR.worksheetCheck.firstCorrect == legacy.worksheetCheck.firstCorrect && contR.worksheetCheck.firstTotal == legacy.worksheetCheck.firstTotal &&
          contR.worksheetCheck.firstTier == legacy.worksheetCheck.firstTier && contR.worksheetCheck.firstXp == legacy.worksheetCheck.firstXp &&
          contR.worksheetCheck.firstWrong.SequenceEqual(legacy.worksheetCheck.firstWrong),
        $"legacy {what}: a frozen first grade keeps its correct/total/tier/XP under its own assessment; an unchecked draft has no grade inferred");
    bool usedCalculator = Level3PracticeRules.NextSlot(legacy) == null || legacy.tapes.Any(t => t.caseId != Level3PracticeRules.CaseId);
    Check(contR.calculatorTaught == usedCalculator, $"legacy {what}: calculatorTaught is {usedCalculator} from the tapes actually attached");
    Level3ChapterRules.TryContinueLegacy(legacy, out Level3ChapterCheckpoint again2, out _);
    Check(Snap(again2) == Snap(contR) && Level3ChapterRules.Decide(contR) == Level3EntryDecision.Resume &&
          !Level3ChapterRules.TryContinueLegacy(contR, out Level3ChapterCheckpoint none, out _) && none == null,
        $"legacy {what}: continuing is deterministic and idempotent (the result resumes, never continues twice)");
    Check(!new[] { "expected", "worksheetDecisions", "correctValue", "documentName", "semanticKey" }.Any(Snap(contR).Contains),
        $"legacy {what}: no answer key or content enters the continued snapshot");
    PlayerProgressData legacySaveR = Clone(live);
    legacySaveR.campaign.levelThree = legacy.Copy();
    PlayerProgressData contSaveR = Level3ChapterRules.WithCheckpoint(Clone(legacySaveR), contR);
    PlayerProgressData contBackR = JsonSerializer.Deserialize<PlayerProgressData>(Snap(contSaveR), json);
    Check(contSaveR != null && !contBackR.Normalize() && AsLegacy(contBackR.campaign.levelThree).Replace("\"writes\":" + contBackR.campaign.levelThree.writes, "\"writes\":" + legacy.writes) == legacyBefore &&
          WithoutChapter(contBackR) == WithoutChapter(legacySaveR) && Level3ChapterRules.Decide(contBackR.campaign.levelThree) == Level3EntryDecision.Resume,
        $"legacy {what}: the continued campaign save round-trips without repair, resumes, and carries every possession and claim unchanged");
}

// ---------- possessions, rewards and claims around a continued approved chapter ----------
PlayerProgressData legacyCampaign = Clone(pay9);
Level3WorksheetCheck.ApplyAcquisitionGrants(legacyCampaign, acq);
legacyCampaign.campaign.levelThree = LegacyOf(approvedClock);
legacyCampaign.campaign.levelThree.writes = 17;
Check(!legacyCampaign.Normalize(), "a version 1 approved chapter inside a paid campaign loads without repair");
string legacyCampaignWithout = WithoutChapter(legacyCampaign);
Level3ChapterRules.TryContinueLegacy(legacyCampaign.campaign.levelThree, out Level3ChapterCheckpoint approvedCont, out _);
PlayerProgressData continuedCampaign = Level3ChapterRules.WithCheckpoint(Clone(legacyCampaign), approvedCont);
Check(continuedCampaign != null && WithoutChapter(continuedCampaign) == legacyCampaignWithout && continuedCampaign.campaign.levelThree.writes == 18,
    "continuing writes only the chapter: wallet, trust, Rosa history, claims, ledger, mastery, badges, inventory, pins, upgrades and decor are unchanged");
int coinsBefore = continuedCampaign.campaign.coins, trustBefore = continuedCampaign.campaign.publicTrust, claimsBefore = continuedCampaign.campaign.rewardClaims.Count;
Level3ReviewGrants approveAgain = Level3ReviewRules.ApplyApprovalGrants(continuedCampaign, approvedCont);
Level3WorksheetGrants collectAgain = Level3WorksheetCheck.ApplyAcquisitionGrants(continuedCampaign, approvedCont);
Check(approveAgain.AlreadyClaimed && !approveAgain.Changed && collectAgain.AlreadyClaimed && continuedCampaign.campaign.coins == coinsBefore &&
      continuedCampaign.campaign.publicTrust == trustBefore && continuedCampaign.campaign.rewardClaims.Count == claimsBefore,
    "a continued chapter is never paid again: worksheet 60 coins/+2 trust/mastery and approval 100 coins/mastery stay claimed once");
Check(Rev9(approvedCont).approved && Rev9(approvedCont).xp == Rev9(approvedClock).xp && approvedCont.Case(B7).copyPhase == "atAuditor" &&
      Level3ReviewRules.Refusal(approvedCont, B7) == Level3ReviewRules.RefuseApproved && Level3ChapterRules.CoarseState(Level3Phase.BeaDelivery) == null &&
      Level3ChapterRules.ImpliedTriggers(approvedCont).Contains(Level3ChapterRules.ApprovedTrigger) && !Level3ReviewRules.ClockDue(approvedCont),
    "approved Bea continues straight into delivery: frozen XP and figures, copy at the Auditor, no second hand-in, clock stopped");
Check(approvedCont.Case(B7).clock.day == 2 && approvedCont.Case(B7).clock.remaining == 37.25f && approvedCont.Case(B7).clock.missed &&
      approvedCont.Case(B7).clock.extensionSeconds == 180 && approvedCont.Case(B7).clock.warned.SequenceEqual(new[] { 120f, 30f }),
    "an already-started legacy clock is kept exactly (day, remaining, warnings, miss, voucher extension)");
Level3ChapterRules.TryContinueLegacy(LegacyOf(runningClock), out Level3ChapterCheckpoint runningCont, out _);
Check(runningCont.Case(B7).clock.started && runningCont.Case(B7).clock.remaining == runningClock.Case(B7).clock.remaining &&
      !Level3ReviewRules.ClockDue(runningCont) && Level3ReviewRules.StartClock(runningCont.Copy(), rc9, out _) == Level3CommandOutcome.Duplicate,
    "a running legacy clock is neither restarted nor reset by continuing");
Level3ChapterCheckpoint legacyEarlyClock = LegacyOf(legacyPracticed);
legacyEarlyClock.Case(B7).clock = new ReviewClockState { started = true, day = 1, remaining = 900f, warned = new List<float>() };
Level3ChapterRules.TryContinueLegacy(legacyEarlyClock, out Level3ChapterCheckpoint earlyCont, out _);
Check(earlyCont.phase == "BeaRecords" && earlyCont.Case(B7).clock.started && earlyCont.Case(B7).clock.remaining == 900f && !Level3ReviewRules.ClockDue(earlyCont),
    "a clock the old rules started at Bea's records stays started (never reset to wait for the new lesson)");

// ---------- the retired practice: half-done and untouched legacy practice ----------
Level3ChapterRules.TryContinueLegacy(practice, out Level3ChapterCheckpoint practiceCont, out _);
Check(practiceCont.phase == "BeaRecords" && practiceCont.activeCaseId == B7 && practiceCont.actionIds.Contains(Level3ChapterRules.PracticeRetiredAction) &&
      practiceCont.milestones.Contains(Level3ChapterRules.PracticeRetiredMilestone) && !practiceCont.actionIds.Contains(Level3PracticeRules.CompleteAction) &&
      !practiceCont.milestones.Contains(Level3PracticeRules.CompleteMilestone) && !practiceCont.calculatorTaught && practiceCont.tapes.Count == 0 &&
      practiceCont.Case(B7).admitted && Level3ChapterRules.Problem(practiceCont) == null,
    "a legacy chapter waiting in practice moves to Bea's records under its own retired-gate id: no practice completion, teaching or reward");
Level3ChapterRules.TryContinueLegacy(attach1, out Level3ChapterCheckpoint halfCont, out _);
Check(halfCont.tapes.Count == 1 && Snap(halfCont.tapes) == Snap(attach1.tapes) && !halfCont.calculatorTaught &&
      !Level3ChapterRules.ImpliedLessons(halfCont).Contains(Level3ChapterRules.AssignmentLesson),
    "a half-done legacy practice keeps its tape as a reference, is not counted as taught, and still gets the go-to-Bea hint once");
Check(Level3ChapterRules.ImpliedTriggers(practiceCont).Contains(Level3ChapterRules.AssignmentTrigger) &&
      !Level3ChapterRules.ImpliedTriggers(practiceCont).Contains(Level3ChapterRules.RecordsTrigger),
    "the continued practice chapter restores check-in as done, Bea's records as still to do");

// ---------- unusable snapshots are kept untouched and blocked ----------
Level3ChapterCheckpoint LegacyMut(Level3ChapterCheckpoint s, Action<Level3ChapterCheckpoint> change) { Level3ChapterCheckpoint c = LegacyOf(s); change(c); return c; }
var unusable = new (string what, Level3ChapterCheckpoint state)[]
{
    ("a version 1 snapshot with an unknown phase", LegacyMut(acq, c => c.phase = "Filing")),
    ("a version 1 snapshot of other content", LegacyMut(acq, c => c.contentRevision = "L3-2027-v1")),
    ("a version 1 snapshot with the company admitted before Bea's delivery", LegacyMut(acq, c => c.Case("L3-MARINE-001").admitted = true)),
    ("a version 1 snapshot holding the certificate before the credit step", LegacyMut(handed, c => c.Case(B7).acquiredSourceIds.Add("B-WHT"))),
    ("a version 1 practice chapter whose Bea was never assigned", LegacyMut(practice, c => c.Case(B7).admitted = false)),
    ("a version 1 snapshot missing the company subrecord", LegacyMut(acq, c => c.cases.RemoveAt(1))),
    ("a future version 3 snapshot", Mutate(c => c.version = 3)),
    ("a version 2 snapshot waiting in the retired practice", Mutate(c => c.phase = "ToolPractice"))
};
foreach (var (what, state) in unusable)
{
    string before = Snap(state);
    bool continuedIt = Level3ChapterRules.TryContinueLegacy(state, out Level3ChapterCheckpoint contR, out string why);
    Check(Level3ChapterRules.Decide(state) == Level3EntryDecision.Blocked && !continuedIt && contR == null && !string.IsNullOrEmpty(why) && Snap(state) == before &&
          Level3ChapterRules.WithCheckpoint(Clone(live), state) == null,
        $"{what}: blocked and kept exactly as saved, never continued or written ('{why}')");
}
var futureSave = Clone(live);
futureSave.campaign.levelThree = Mutate(c => c.version = 3);
string futureBefore = Snap(futureSave.campaign.levelThree);
futureSave.Normalize();
Check(Snap(futureSave.campaign.levelThree) == futureBefore, "a newer chapter's known fields survive load repair untouched");

// ---------- revised mapping of the saved worksheet (lossless; nothing graded) ----------
Check(Level3WorksheetRules.RevisedDestination("income") == "income" && Level3WorksheetRules.RevisedDestination("expenses") == Level3WorksheetRules.Deduct &&
      Level3WorksheetRules.RevisedDestination("not-deductible") == Level3WorksheetRules.LeaveOut && Level3WorksheetRules.RevisedDestination("supporting") == Level3WorksheetRules.LeaveOut &&
      Level3WorksheetRules.RevisedDestination("") == "" && Level3WorksheetRules.RevisedDestination("deduct") == "",
    "old sections read as Income / Deduct / Leave Out (both Leave Out sections keep their saved key)");
Check(Level3WorksheetRules.RowIds.GroupBy(Level3WorksheetRules.BatchOf).Select(g => g.Key + ":" + string.Join(",", g)).SequenceEqual(new[] { "earnings:R1,R2", "costs:R3,R4,R5", "questions:R6,R7" }) &&
      Level3WorksheetRules.BatchOf("B-PRINT") == null && Level3WorksheetRules.Batches.SequenceEqual(new[] { "earnings", "costs", "questions" }),
    "rows fall into the revised batches 2/3/2 by row id only");
Level3ChapterRules.TryContinueLegacy(LegacyOf(wsWrong), out Level3ChapterCheckpoint draftCont, out _);
Check(draftCont.worksheet.strips.Count == 7 && draftCont.worksheet.strips.All(st => Level3WorksheetRules.BatchOf(st.rowId) != null &&
          (st.section == "" || Level3WorksheetRules.RevisedDestination(st.section) != "")) &&
      Snap(draftCont.worksheet) == Snap(wsWrong.worksheet) && !draftCont.worksheetCheck.firstChecked && draftCont.worksheetCheck.assessmentVersion == "",
    "a continued legacy draft keeps every placement, reason, link and undo step and maps row by row into the revised batches, with no grade");
Check(fr.worksheetCheck.assessmentVersion == LegacyAssessment && Level3WorksheetCheck.AssessmentVersion == LegacyAssessment,
    "the current (twelve-decision) check labels the grade it freezes, so a later nine-decision check can never regrade it");

// ---------- wiring: continuation is candidate -> signed write -> quiet restore ----------
string cpR = Src("Scripts/Level3/Level3Checkpointer.cs");
int contAt = Index(cpR, "private bool TryContinue(Level3ChapterCheckpoint continued)");
string contBody = cpR.Substring(contAt, Index(cpR, "private void RestoreQuietly(bool welcomeBack = true)") - contAt);
Check(Index(contBody, "PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(continued)") > 0 &&
      Index(contBody, "PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(continued)") < Index(contBody, "State = continued;") &&
      Index(contBody, "State = continued;") < Index(contBody, "RestoreQuietly();") && !contBody.Contains("GameplayEvents.Raise") && !contBody.Contains("Publish("),
    "continuation: written first, then live, then restored quietly (no gameplay event, reward or publish)");
Check(cpR.Contains("case Level3EntryDecision.Continue:") && Index(cpR, "while (pendingContinuation != null)") > 0 &&
      Index(cpR, "while (pendingContinuation != null)") < Index(cpR, "IsReady = true;") && cpR.Contains("if (settingsExit) yield break;"),
    "a failed continuation write keeps the chapter not ready (nothing published or saved over it) and retries; leaving stops the retry");
Check(Src("Scripts/Level3/Level3ReviewRules.cs").Contains("return Level3ChapterRules.BeaWorkIndependent(state) && phase <= Level3Phase.BeaComputation"),
    "Bea's clock start reads the independence rule, not the retired practice");
string deskR = Src("Scripts/Level3/Level3DeskCalculator.cs");
Check(deskR.Contains("bool earlierTapes = Level3PracticeRules.NextSlot(chapter) != Level3PracticeRules.Slots[0];") &&
      deskR.Contains("card.SetStatus(bothTapes ? \"PRACTICE COMPLETE\" : \"\");"),
    "the desk shows the practice card only for tapes attached before the revision, marked complete only with both");
Check(Src("Scripts/Level3/Level3WorksheetCheck.cs").Contains("result.assessmentVersion = AssessmentVersion;"),
    "the frozen first check records its assessment version");
Console.WriteLine($"T02 revision checks: {checks - t02rStart}, failures {fails - t02rFails} (fresh entry, legacy continuation at every boundary, idempotence, failure/unknown, claims once, clocks, mapping, wiring; Unity, Play Mode and real saves not run).");

// =====================================================================
// T03 revision (3 November 2026): the real Case Folder in Level 3, the
// calculator's first-use lesson, and the narrow Level 3 transfer boundary
// =====================================================================
int t03rStart = checks, t03rFails = fails;

// ---------- the disposable Editor fixture (real commands, never saved) ----------
Level3ChapterCheckpoint office = Level3EditorFixtures.FirstOfficeUse(bea);
Check(office != null && Level3ChapterRules.Problem(office) == null && office.phase == "BeaComputation" && office.version == 2 && !office.calculatorTaught &&
      Level3WorksheetCheck.Collected(office) && office.worksheetCheck.assessmentVersion == "L3-worksheet-v2" && office.worksheetCheck.firstCorrect == 9 &&
      office.worksheetCheck.firstTotal == 9 && office.worksheetCheck.firstTier == 3 && office.worksheetCheck.firstXp == 60 &&
      Level3ChapterRules.BeaHandoverSources.All(office.Case(B7).acquiredSourceIds.Contains) && !office.Case(B7).acquiredSourceIds.Contains("B-WHT"),
    "fixture: Bea's first office calculation, built by the real commands (assigned, handed over, three machine batches filed and checked 9/9, collected: revised T06), calculator untaught");
Check(Snap(Level3EditorFixtures.FirstOfficeUse(bea)) == Snap(office) && Level3EditorFixtures.Build(Level3EditorFixtures.FirstOfficeUseId, bea) != null &&
      Level3EditorFixtures.FirstOfficeUse(marine) == null && Level3EditorFixtures.Build("unknown", bea) == null && Level3EditorFixtures.FirstOfficeUse(null) == null,
    "fixture: deterministic; only Bea's case; unknown ids build nothing");
Check(!new[] { "expected", "worksheetDecisions", "correctValue", "semanticKey" }.Any(Snap(office).Contains),
    "fixture: the chapter it builds carries no answer key or content");
string fixtureSrc = Src("Scripts/Level3/Level3EditorFixtures.cs"), menuSrc = File.ReadAllText(Path.Combine(root, "Assets/Editor/Level3EditorFixtureMenu.cs"));
string cpT3 = Src("Scripts/Level3/Level3Checkpointer.cs");
// T16 (6 November 2026) supersedes "stripped from builds": two fixtures are also the Tutorial Hub's practice starts (never saved).
Check(!fixtureSrc.StartsWith("#if UNITY_EDITOR") && menuSrc.Contains("[MenuItem(FirstOfficeMenu)]") &&
      menuSrc.Contains("Level3Checkpointer.EditorFixture =") && cpT3.Contains("if (IsDirectEditorAttempt && EditorFixture != null)") &&
      cpT3.Split("EditorFixture(").Length == 2 && !Src("Scripts/RewardsSystem/PlayerProgressManager.cs").Contains("EditorFixture"),
    "fixture: chosen from a Tools menu; the EditorFixture hook is read only by a direct Editor attempt (which never saves); T16 practice starts use the builder, never a save");

// ---------- the first-use lesson: the one required action, unscored ----------
Check(Level3PracticeRules.LessonDue(office) && !Level3PracticeRules.LessonDue(assigned) && !Level3PracticeRules.LessonDue(attach2) &&
      !Level3PracticeRules.LessonDue(Continued(LegacyOf(acq))) && Continued(LegacyOf(acq)).calculatorTaught,
    "the lesson is due only at Bea's computation of a chapter never taught (a continued chapter that used the calculator is taught)");
Level3Calculator Lesson(params string[] steps)
{
    var c = new Level3Calculator();
    foreach (string step in steps)
    {
        if (step == "-") c.Press(Level3CalcKey.Subtract);
        else if (step == "+") c.Press(Level3CalcKey.Add);
        else if (step == "=") c.Press(Level3CalcKey.Equals);
        else if (step.StartsWith("#")) Keys(c, step.Substring(1));
        else { Level3PracticeSource src = Level3PracticeRules.LessonSources.First(x => x.SourceId == step); c.SelectSource(src.SourceId, src.Label, src.Amount); }
    }
    return c;
}
Level3Calculator good3 = Lesson(Level3PracticeRules.LessonFund, "-", Level3PracticeRules.LessonSpent, "=");
Check(good3.DraftResult == "8000" && Level3Calculator.TapeLines(good3.DraftExpression, good3.DraftResult).SequenceEqual(new[] { "10,000.00 INCOME", "− 2,000.00 COSTS", "= 8,000.00" }),
    "the example works exactly on the existing calculator (teaching pass: a taxable-income example): 10,000.00 − 2,000.00 = 8,000.00, each line labelled with its source");
string officeBefore = Snap(office);
Level3ChapterCheckpoint taught3 = office.Copy();
Check(Level3PracticeRules.CompleteLesson(taught3, Level3PracticeRules.LessonSlot, good3.DraftExpression, good3.DraftResult, out string why3) == Level3CommandOutcome.Applied &&
      why3 == null && taught3.calculatorTaught && taught3.actionIds.Last() == Level3PracticeRules.TaughtAction && Snap(office) == officeBefore,
    "attaching the example saves calculatorTaught and its action id on the candidate only");
Level3ChapterCheckpoint onlyMarker = taught3.Copy();
onlyMarker.calculatorTaught = false;
onlyMarker.actionIds.Remove(Level3PracticeRules.TaughtAction);
Check(Snap(onlyMarker) == officeBefore && taught3.tapes.Count == office.tapes.Count && taught3.milestones.SequenceEqual(office.milestones),
    "nothing else changes: no tape, milestone, worksheet, grade, computation, stamp, review or clock");
string taughtSnap3 = Snap(taught3);
Check(Level3PracticeRules.CompleteLesson(taught3, Level3PracticeRules.LessonSlot, good3.DraftExpression, good3.DraftResult, out _) == Level3CommandOutcome.Duplicate &&
      Snap(taught3) == taughtSnap3 && !Level3PracticeRules.LessonDue(taught3),
    "a repeated attach (double tap, restore) is a quiet duplicate: taught once, the lesson never returns");
Level3Calculator typed3 = Lesson("#10000-2000="), added3 = Lesson(Level3PracticeRules.LessonFund, "+", Level3PracticeRules.LessonSpent, "=");
Level3Calculator reversed3 = Lesson(Level3PracticeRules.LessonSpent, "-", Level3PracticeRules.LessonFund, "="), half3 = Lesson(Level3PracticeRules.LessonFund, "-", "#2000", "=");
var practiceTokens3 = new Level3Calculator();
practiceTokens3.SelectSource(Level3PracticeRules.BudgetSource, "Budget", "100.00"); practiceTokens3.Press(Level3CalcKey.Subtract);
practiceTokens3.SelectSource(Level3PracticeRules.PaperSource, "Paper order", "20.00"); practiceTokens3.Press(Level3CalcKey.Equals);
var lessonRefusals = new (string what, Level3ChapterCheckpoint state, string slot, Level3Calculator lessonCalc, string result, string hint)[]
{
    ("typed numbers (no source on the tape)", office.Copy(), Level3PracticeRules.LessonSlot, typed3, null, Level3PracticeRules.RefuseFromCard),
    ("one typed number", office.Copy(), Level3PracticeRules.LessonSlot, half3, null, Level3PracticeRules.RefuseFromCard),
    ("the retired budget card's amounts", office.Copy(), Level3PracticeRules.LessonSlot, practiceTokens3, null, Level3PracticeRules.RefuseFromCard),
    ("an addition (no subtraction)", office.Copy(), Level3PracticeRules.LessonSlot, added3, null, Level3PracticeRules.RefuseNoWorking),
    ("the amounts the wrong way round", office.Copy(), Level3PracticeRules.LessonSlot, reversed3, null, Level3PracticeRules.LessonTarget.Hint),
    ("a printed result that disagrees with its working", office.Copy(), Level3PracticeRules.LessonSlot, good3, "771", Level3PracticeRules.RefuseNoWorking),
    ("no tape printed", office.Copy(), Level3PracticeRules.LessonSlot, new Level3Calculator(), null, Level3PracticeRules.RefuseNoTape),
    ("another slot", office.Copy(), Level3PracticeRules.FirstSlot, good3, null, Level3PracticeRules.RefuseNotNow),
    ("before Bea's computation", assigned.Copy(), Level3PracticeRules.LessonSlot, good3, null, Level3PracticeRules.RefuseNotNow)
};
foreach (var (what, state, slot, lessonCalc, result, hint) in lessonRefusals)
{
    string before = Snap(state);
    Check(Level3PracticeRules.CompleteLesson(state, slot, lessonCalc.DraftExpression, result ?? lessonCalc.DraftResult, out string why) == Level3CommandOutcome.Refused &&
          why == hint && Snap(state) == before && !state.calculatorTaught,
        $"lesson refused without change, nothing taught: {what} ('{why}')");
}
Level3ChapterCheckpoint legacyOffice = V1(office);
Check(Level3PracticeRules.CompleteLesson(legacyOffice, Level3PracticeRules.LessonSlot, good3.DraftExpression, good3.DraftResult, out _) == Level3CommandOutcome.Refused &&
      !legacyOffice.calculatorTaught, "a version 1 chapter is never written to before it is continued");
Check(Level3ComputationRules.AttachTape(taught3.Copy(), bea, "taxable-income", good3.DraftChain, good3.DraftResult, out string foreign3) == Level3CommandOutcome.Refused &&
      foreign3 == Level3ComputationRules.RefuseForeign,
    "the example's numbers can never be attached to Bea's sheet (they are not her sources)");
var beaAmounts3 = bea.level3.rows.Select(r => r.amount).Concat(bea.level3.totals.Select(t => t.amount)).ToList();
Check(Level3PracticeRules.LessonSources.All(x => !beaAmounts3.Contains(x.Amount) && !beaAmounts3.Contains(x.Amount.Replace(".00", ""))) &&
      !beaAmounts3.Contains("8000.00") && !beaAmounts3.Contains("8000") && Level3PracticeRules.LessonSources.All(x => !x.SourceId.StartsWith("B-")),
    "the example is unrelated to Bea: none of its amounts or ids is hers");

// ---------- the cards: one model, two printed specs ----------
Check(Level3PracticeRules.PracticeCard.Sources == Level3PracticeRules.CardSources && Level3PracticeRules.PracticeCard.Slots == Level3PracticeRules.Slots &&
      Level3PracticeRules.LessonCard.Sources.Length == 2 && Level3PracticeRules.LessonCard.Slots.Length == 1 &&
      Level3PracticeRules.LessonCard.Note.Contains("Not Bea's") && Level3PracticeRules.LessonCard.Note.Contains("No score") &&
      Level3PracticeRules.LessonCard.Title.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length <= 6,
    "the retired budget card keeps its legacy content; the example card has two amounts, one slot, a short title and 'Not Bea's · No score'");
string cardSrc3 = Src("Scripts/Level3/Level3PracticeCardModel.cs");
Check(cardSrc3.Contains("foreach (Level3PracticeSource source in Spec.Sources)") && cardSrc3.Contains("Spec.Slots[i]") && !cardSrc3.Contains("Level3PracticeRules.CardSources") &&
      cardSrc3.Contains("props.Box(\"Clipboard\""), "one clipboard model prints either card (same body, targets, tapes); no second card class");

// ---------- save: round trip, quiet resume, failed write ----------
PlayerProgressData taughtSave3 = JsonSerializer.Deserialize<PlayerProgressData>(Snap(Level3ChapterRules.WithCheckpoint(Clone(live), taught3)), json);
Check(!taughtSave3.Normalize() && taughtSave3.campaign.levelThree.calculatorTaught && Level3ChapterRules.Decide(taughtSave3.campaign.levelThree) == Level3EntryDecision.Resume &&
      WithoutChapter(taughtSave3) == liveWithout,
    "the taught marker round-trips without repair, resumes, and changes no wallet, trust, claim, XP, inventory or history");
Check(Level3ChapterRules.ImpliedLessons(taught3).Contains(Level3ChapterRules.CalculatorLessonId) && !Level3ChapterRules.ImpliedLessons(office).Contains(Level3ChapterRules.CalculatorLessonId) &&
      Level3ChapterRules.ImpliedLessons(Continued(LegacyOf(acq))).Contains(Level3ChapterRules.CalculatorLessonId),
    "resume: a taught chapter never shows the lesson again; an untaught one still does");
string sandbox3r = Path.Combine(Path.GetTempPath(), "TaxVentureL3T03-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox3r);
try
{
    string Pay(PlayerProgressData d) => JsonSerializer.Serialize(d, json);
    string Enc(PlayerProgressData d) => JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = Pay(d), ["signature"] = SaveFileStore.Sign(Pay(d)) });
    var store3r = new SaveFileStore(Path.Combine(sandbox3r, "progress.json"));
    PlayerProgressData pub3 = Level3ChapterRules.WithCheckpoint(Clone(live), office);
    store3r.Write(Enc(pub3));
    byte[] disk3 = File.ReadAllBytes(store3r.FilePath);
    string pubBefore3 = Snap(pub3);
    // Mirrors Level3Checkpointer.CommitCalculatorLesson -> CommitLevelThreeCheckpoint -> TryWriteCandidate.
    bool CommitLesson(ref PlayerProgressData published)
    {
        Level3ChapterCheckpoint cand = published.campaign.levelThree.Copy();
        if (Level3PracticeRules.CompleteLesson(cand, Level3PracticeRules.LessonSlot, good3.DraftExpression, good3.DraftResult, out _) != Level3CommandOutcome.Applied) return false;
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(published), cand);
        try { next.Normalize(); store3r.Write(Enc(next)); } catch (Exception) { return false; }
        published = next;
        return true;
    }
    var tapeManagerRepair = (ValueTransferManager)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ValueTransferManager));
    var printPayloadRepair = new Level3CalculatorTransferValue("lesson/2026", "printed-tape", "Result", good3.DraftResult, draftKey: "current-print");
    var tapeEndpointRepair = new CalculatorTapeRepairEndpoint(printPayloadRepair, () => CommitLesson(ref pub3));
    tapeManagerRepair.SelectValue(tapeEndpointRepair, tapeEndpointRepair);
    Directory.CreateDirectory(store3r.TempPath);
    tapeManagerRepair.TryPlaceOnDestination(tapeEndpointRepair);
    Check(tapeManagerRepair.HasSelection && good3.HasDraft && Snap(pub3) == pubBefore3 && File.ReadAllBytes(store3r.FilePath).SequenceEqual(disk3),
        "shared tape drop with failed signed write retains selection, printed draft and prior state");
    Check(!CommitLesson(ref pub3) && Snap(pub3) == pubBefore3 && File.ReadAllBytes(store3r.FilePath).SequenceEqual(disk3) && !pub3.campaign.levelThree.calculatorTaught,
        "a failed lesson write teaches nothing: the published chapter and the file are unchanged (the example can be attached again)");
    Directory.Delete(store3r.TempPath);
    tapeManagerRepair.TryPlaceOnDestination(tapeEndpointRepair);
    Check(!tapeManagerRepair.HasSelection && pub3.campaign.levelThree.calculatorTaught && !CommitLesson(ref pub3),
        "shared tape retry: saved once, selection clears only after commit; a further attach changes nothing");
}
finally
{
    try { Directory.Delete(sandbox3r, true); } catch (IOException) { }
}

// ---------- the Case Folder: carried, Client page, Supporting Documents ----------
Check(!Level3CaseFolder.ShouldCarry(entry, true, true) && Level3CaseFolder.ShouldCarry(assigned, true, true) && !Level3CaseFolder.ShouldCarry(assigned, false, true) &&
      Level3CaseFolder.ShouldCarry(assigned, false, false) && Level3CaseFolder.ShouldCarry(office, true, true) && Level3CaseFolder.ShouldCarry(approvedClock, true, true) &&
      !Level3CaseFolder.ShouldCarry(null, true, false) && !Level3CaseFolder.ShouldCarry(Mutate(c => c.Case(B7).admitted = false), true, true),
    "carried from Bea's assignment on; a story run carries only a folder the campaign owns (a local attempt may carry one); never before reception or unassigned");
var client3 = Level3CaseFolder.ClientLines(bea, assigned);
Check(client3.Select(l => l.Key + "=" + l.Value).SequenceEqual(new[] { "Client=Beatriz Mendoza", "Request=2026 Annual Computation", "Tax year=2026 · 1 Jan – 31 Dec", "Case=L3-BEA-001" }),
    "Client page before the handover: name, request, tax year and case (no work line until her papers are acquired)");
var clientHanded3 = Level3CaseFolder.ClientLines(bea, handed);
Check(clientHanded3.Count == 5 && clientHanded3[1].Key == "Work" && clientHanded3[1].Value == "Freelance illustrator",
    "after the handover her acquired ID adds her work");
Check(!client3.Concat(clientHanded3).Any(l => l.Key.Contains("Classification") || l.Key.Contains("Type") || l.Key.Contains("Income") || l.Value.Any(char.IsDigit) && l.Value.Contains(",")),
    "no classification is redone and no amount is printed on the Client page");
Check(Level3CaseFolder.ClientLines(null, assigned).Count == 0 && Level3CaseFolder.ClientLines(marine, assigned)[0].Value == "Coast Tales Marine Services, Inc.",
    "the Client page reads the case's own content (the company's name for its case; nothing without content)");
Check(Level3CaseFolder.RecordKey(assigned) == "L3-BEA-001|" && Level3CaseFolder.RecordKey(handed) != Level3CaseFolder.RecordKey(assigned) &&
      Level3CaseFolder.RecordKey(pay8) != Level3CaseFolder.RecordKey(handed) && Level3CaseFolder.RecordKey(pay8).Contains("B-WHT"),
    "the Supporting Documents set is rebuilt when the active case acquires a paper (handover, certificate), not otherwise");
var papers3 = Level3RecordRules.Build(bea, handed.Case(B7).acquiredSourceIds);
var papersWht3 = Level3RecordRules.Build(bea, pay8.Case(B7).acquiredSourceIds);
Check(Level3RecordRules.Build(bea, assigned.Case(B7).acquiredSourceIds).Count == 0 && papers3.Count == 9 && !papers3.Any(r => r.source == "B-WHT") &&
      papersWht3.Count == 10 && papersWht3.Any(r => r.source == "B-WHT") && papers3[0].type == ClientRecord.Identity &&
      papers3.All(r => r.id == Level3RecordRules.RecordId(B7, r.source)),
    "Supporting Documents holds exactly the acquired papers by stable id (none before the handover, nine after, the certificate only once acquired)");
RecordPageLayout layout3 = Level3CaseFolder.Layout;
Check(layout3.sheetsInRow && layout3.maxPerRow == 2 && layout3.oneShapePerRow && layout3.cardWidth < RecordPageLayout.ReviewRecords.cardWidth &&
      layout3.sheetWidth == 0.4f, "Level 2's review layout (three sheets to a row, slips apart) with a smaller ID card");

// ---------- the narrow Level 3 transfer boundary ----------
Level3TransferValue credit3 = Level3ValueTransfer.FromPaper(bea, pay8, "B-WHT", "BWithheldCredit");
DataValue creditValue3 = Level3ValueTransfer.Wrap(credit3);
Check(credit3 != null && credit3.Amount == "12500" && credit3.SourceId == "paper:B-WHT/BWithheldCredit" && credit3.CaseId == B7 && credit3.TaxYear == 2026 &&
      credit3.ContentRevision == "L3-2026-v1" && creditValue3.Type == DataValueType.Number && creditValue3.SemanticKey == "Level3:paper:B-WHT/BWithheldCredit" &&
      creditValue3.Value is Level3TransferValue && creditValue3.AsFloat() == 0f && creditValue3.DisplayText == "12,500.00",
    "a paper's amount travels as an exact decimal with its case, source, year and revision; AsFloat() is never its value");
Check(Level3ValueTransfer.FromPaper(bea, handed, "B-WHT", "BWithheldCredit") == null && Level3ValueTransfer.FromPaper(bea, pay8, "B-WHT", "NoSuchField") == null,
    "no value from a paper the case has not acquired, or a field it does not print");
Check(Level3ValueTransfer.TryRead(creditValue3, pay8, out Level3TransferValue read3, out string r3a) && r3a == null && read3 == credit3,
    "a same-case value from a held paper is read as it is");
Level3TransferValue income3 = Level3ValueTransfer.FromPaper(bea, pay8, "B-WHT", "BWithheldIncome");
Check(income3 != null && income3.Amount == "250000" && Level3ValueTransfer.TryRead(Level3ValueTransfer.Wrap(income3), pay8, out _, out _),
    "a plausible wrong amount of the same case is read too (correctness is the review's, never the drop's)");
Level3TransferValue Variant(Action<Level3TransferValue> change)
{
    var v = new Level3TransferValue { CaseId = credit3.CaseId, SourceId = credit3.SourceId, Label = credit3.Label, Amount = credit3.Amount, ContentRevision = credit3.ContentRevision, TaxYear = credit3.TaxYear };
    change(v);
    return v;
}
var transferRefusals = new (string what, DataValue value, Level3ChapterCheckpoint chapter, string why)[]
{
    ("a legacy float", new DataValue(12500f, "₱12,500", DataValueType.Number, "TaxWithheld"), pay8, Level3ValueTransfer.RefuseFloat),
    ("a plain text value", new DataValue("12500", "12500", DataValueType.Number, "TaxWithheld"), pay8, Level3ValueTransfer.RefuseNotLevel3),
    ("nothing", null, pay8, Level3ValueTransfer.RefuseNotLevel3),
    ("another case's value", Level3ValueTransfer.Wrap(Variant(v => v.CaseId = "L3-MARINE-001")), pay8, Level3ValueTransfer.RefuseForeign),
    ("other content", Level3ValueTransfer.Wrap(Variant(v => v.ContentRevision = "L3-2027-v1")), pay8, Level3ValueTransfer.RefuseRevision),
    ("a formatted amount", Level3ValueTransfer.Wrap(Variant(v => v.Amount = "12,500.00")), pay8, Level3ValueTransfer.RefuseMalformed),
    ("a non-canonical amount", Level3ValueTransfer.Wrap(Variant(v => v.Amount = "12500.00")), pay8, Level3ValueTransfer.RefuseMalformed),
    ("a paper this chapter does not hold", creditValue3, handed, Level3ValueTransfer.RefuseNotHeld),
    ("an unknown source kind", Level3ValueTransfer.Wrap(Variant(v => v.SourceId = "typed:12500")), pay8, Level3ValueTransfer.RefuseNotHeld),
    ("a version 1 chapter", creditValue3, V1(pay8), Level3ValueTransfer.RefuseNotLevel3)
};
foreach (var (what, value, chapterState, expect) in transferRefusals)
    Check(!Level3ValueTransfer.TryRead(value, chapterState, out Level3TransferValue none3, out string why) && none3 == null && why == expect,
        $"transfer refused (mechanical, never a grade): {what} ('{why}')");
Check(Level3ValueTransfer.TryRead(Level3ValueTransfer.Wrap(Variant(v => v.SourceId = Level3ComputationRules.WorksheetIncome)), pay8, out _, out _) &&
      !Level3ValueTransfer.TryRead(Level3ValueTransfer.Wrap(Variant(v => v.SourceId = Level3ComputationRules.WorksheetIncome)), handed, out _, out _) &&
      Level3ValueTransfer.TryRead(Level3ValueTransfer.Wrap(Variant(v => { v.SourceId = "tape:" + Level3ComputationRules.Destination(B7, Level3ComputationRules.Taxable); v.Amount = "600000"; })), pay8, out _, out _) &&
      !Level3ValueTransfer.TryRead(Level3ValueTransfer.Wrap(Variant(v => v.SourceId = "tape:" + Level3PracticeRules.FirstSlot)), Continued(attach1), out _, out _),
    "worksheet values need the collected worksheet; tape values need this case's own attached tape (a practice tape is never one)");
Check(Level3ValueTransfer.TryRead(Level3ValueTransfer.Wrap(Variant(v => v.Amount = "0.1")), pay8, out Level3TransferValue tenth, out _) && tenth.Amount == "0.1" &&
      !Level3ValueTransfer.OwnsSelection(null, null), "amounts stay exact decimals (0.1 is 0.1); ownership needs a real owner");
string vtSrc = System.Text.RegularExpressions.Regex.Replace(Src("Scripts/Level3/Level3ValueTransfer.cs"), @"///.*|//.*", "");
Check(!vtSrc.Contains("AsFloat") && !vtSrc.Contains("float.Parse") && !vtSrc.Contains("(float)") && !vtSrc.Contains("TaxComputationCalculator"),
    "the boundary never reads a float or the old flat-rate calculator");

// ---------- the chapter guard keeps Level 1's folder step out of Level 3 only ----------
foreach (Level3Phase phase in Enum.GetValues(typeof(Level3Phase)))
    Check(!Level3ChapterRules.TransitionAllowed(phase, "ReviewDocumentsState"), $"{phase}: opening the folder never moves Level 3 into Level 1's ReviewDocuments step");
Check(Level3ChapterRules.LegacyTransitionAllowed("ReviewDocumentsState") && Type.GetType("ReviewDocumentsState, Assembly-CSharp") != null &&
      Level3ChapterRules.TransitionAllowed(Level3Phase.BeaRecords, "ReceiveCaseState"),
    "the shared legacy guard is unchanged (Levels 1-2 keep their folder step); Level 3's own states stay allowed");

// ---------- the guide: one lesson, the player's own actions, shown once ----------
GuideScript guide3r = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
GuideLesson calcLesson3 = guide3r.lessons.FirstOrDefault(l => l.id == Level3ChapterRules.CalculatorLessonId);
Check(guide3r.Validate() == "" && calcLesson3 != null && calcLesson3.when == "flag:calculatorLesson" && calcLesson3.after == "computation-intro" &&
      calcLesson3.onlyIn == "mode:Workstation" && calcLesson3.pages.Count() == 3 && calcLesson3.pages.All(p => p.IsHint) &&
      calcLesson3.pages.Select(p => p.waitFor).SequenceEqual(new[] { "cue:" + Level3DeskCalculator.CueSourceSelected, "cue:" + Level3DeskCalculator.CueTapePrinted, "flag:calculatorTaught|cue:" + Level3DeskCalculator.CueTapeAttached }),
    "one hint lesson at the desk, each page waiting for the player's own source tap, = and attach (never a page tap)");
Check(calcLesson3.pages.All(p => p.TextFor(true).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 14 && !p.TextFor(true).Contains("click")) &&
      guide3r.lessons.First(l => l.id == "computation-taxable").after == Level3ChapterRules.CalculatorLessonId,
    "each page is at most 14 words, touch wording; Bea's first computation hint waits until the lesson is done");
Check(cpT3.Contains("case \"calculatorLesson\": return Level3PracticeRules.CalculatorLessonDue(State) && Level3DeskCalculator.Instance != null && Level3DeskCalculator.Instance.Lifted && !Level3DeskCalculator.Instance.DemoRunning;") &&
      cpT3.Contains("case \"calculatorTaught\": return Level3PracticeRules.Taught(State);"), "the lesson's flags read saved state and the lifted tools only");

// ---------- wiring: owners, order, ownership, Levels 1-2 untouched ----------
int lessonAt3 = Index(cpT3, "public TapeCommit CommitCalculatorLesson(");
string lessonBody3 = cpT3.Substring(lessonAt3, Index(cpT3, "/// <summary>What attaching a practice tape did") - lessonAt3);
Check(Index(lessonBody3, "Level3PracticeRules.CompleteLesson(candidate") < Index(lessonBody3, "CommitLevelThreeCheckpoint(candidate)") &&
      Index(lessonBody3, "CommitLevelThreeCheckpoint(candidate)") < Index(lessonBody3, "Publish(candidate);") &&
      Index(lessonBody3, "Publish(candidate);") < Index(lessonBody3, "RaiseGuideCue") && !lessonBody3.Contains("Grant") && !lessonBody3.Contains("coins"),
    "lesson: candidate, signed write, publish, then the cue; no reward of any kind");
string deskT3 = Src("Scripts/Level3/Level3DeskCalculator.cs");
int attachLessonAt = Index(deskT3, "private void AttachLesson()");
string attachLesson3 = deskT3.Substring(attachLessonAt, Index(deskT3, "private void TickLessonHandOff()") - attachLessonAt);
Check(Index(attachLesson3, "owner.CommitCalculatorLesson(") < Index(attachLesson3, "StartAttachFlight(") &&
      attachLesson3.Contains("lessonHolding = false;") &&
      deskT3.Contains("public Level3Calculator Calculator => LessonOpen ? lessonCalculator : caseCalculator;") &&
      deskT3.Contains("if (!ComputationOpen && OfficeHudRuntime.Instance != null && OfficeHudRuntime.Instance.CaseFolderViewOut) OfficeHudRuntime.Instance.CloseCaseFolderView();") &&
      Src("Scripts/Level3/Level3DeskCalculatorComputation.cs").Contains(">= Level3Phase.BeaComputation && !LessonOpen;"),
    "desk: the tape flies only after the save; a refused/failed attach teaches nothing; the example's draft never touches the case's; lifting for the example closes the floating folder (revised T07: Bea's computation keeps it out); Bea's sheet waits for the lesson");
string folderT3 = Src("Scripts/Level3/Level3CaseFolder.cs");
Check(folderT3.Contains("store.RestoreStored();") && !folderT3.Contains("StoreFolderFromExternalSource") && !folderT3.Contains("CommitLevelThree") &&
      !folderT3.Contains("RaiseCaseFolder") && folderT3.Contains("if (CaseFolderUI.FrontPage == (Func<CaseData, CaseFolderPageContent>)ClientPage) CaseFolderUI.FrontPage = null;") &&
      folderT3.Contains("if (pages.InstancePages == (Func<CaseData, List<CaseFolderPageContent>>)SupportingPages) pages.InstancePages = null;") &&
      folderT3.Contains("!Level3ValueTransfer.OwnsSelection(manager.SelectedSourceComponent, folder)) return;") &&
      folderT3.Contains("if (records != null && records.AnyOut) return;") &&
      folderT3.Contains("pages.CurrentPageIndex == pages.FindPageIndexByRole(CaseFolderPageContent.SupportingRecordsRole))) return;") &&
      folderT3.Contains("AttachedRecordsPage.Create(") && folderT3.Contains("Level3RecordRules.Build("),
    "folder: the store's quiet resume path, no save or event of its own, hooks released only if its own, only its own selection cleared, records rebuilt only while idle and not on show (revised T07/T08)");
string folderUi3 = Src("Scripts/UI/CaseFolderUI.cs");
Check(folderUi3.Contains("public static System.Func<CaseData, CaseFolderPageContent> FrontPage;") &&
      folderUi3.Contains("CaseFolderPageContent front = !IsReference && FrontPage != null ? FrontPage(data) : null;") &&
      folderUi3.Contains("if (index == Page1Index && page.Render == null)") &&
      Directory.GetFiles(Path.Combine(root, "Assets", "Scripts"), "*.cs", SearchOption.AllDirectories)
          .Where(f => File.ReadAllText(f).Contains("CaseFolderUI.FrontPage =")).Select(Path.GetFileName).SequenceEqual(new[] { "Level3CaseFolder.cs" }),
    "the shared folder's front-page hook is opt-in: only Level 3 sets it; Levels 1-2 keep 'Taxpayer Information' and reference copies ignore it");
Check(Src("Scripts/Level3/Level3Audit.cs").Contains("if (Level3CaseFolder.Active != null && Level3CaseFolder.Active.Bound && !Level3CaseFolder.Active.Carried) return false;") &&
      Src("Scripts/Level3/Level3OfficeStart.cs").Contains("EnsurePart<Level3CaseFolder>(Level3CaseFolder.ChildName);"),
    "one folder part under Level3Gameplay; the Auditor takes only the carried folder (not one left on the desk)");
var newMetas3 = new[] { "Assets/Scripts/Level3/Level3CaseFolder.cs.meta", "Assets/Scripts/Level3/Level3ValueTransfer.cs.meta",
    "Assets/Scripts/Level3/Level3EditorFixtures.cs.meta", "Assets/Editor/Level3EditorFixtureMenu.cs.meta" };
var allGuids3 = Directory.GetFiles(Path.Combine(root, "Assets"), "*.meta", SearchOption.AllDirectories)
    .Select(f => System.Text.RegularExpressions.Regex.Match(File.ReadAllText(f), "guid: ([0-9a-f]{32})").Groups[1].Value).Where(g => g.Length > 0).ToList();
Check(newMetas3.All(m => File.Exists(Path.Combine(root, m))) &&
      newMetas3.Select(m => System.Text.RegularExpressions.Regex.Match(File.ReadAllText(Path.Combine(root, m)), "guid: ([0-9a-f]{32})").Groups[1].Value)
          .All(g => g.Length == 32 && allGuids3.Count(x => x == g) == 1),
    "each new script has its own unique .meta GUID");
Console.WriteLine($"T03 revision checks: {checks - t03rStart}, failures {fails - t03rFails} (fixture, lesson command/refusals/save/failure, cards, folder carry/pages/records, transfer boundary, guard, guide, wiring; Unity rendering, touch, Play Mode not run).");

// User visual follow-up: same folder fields and shared free-position stamp command.
var placedPolish = pay8.Copy();
string pristinePolish = Snap(placedPolish);
Check(Level3ReviewRules.StampAt(placedPolish, Level3ReviewRules.StampReady, -1, 30f, 70f, -8f, out _) == Level3CommandOutcome.Refused && Snap(placedPolish) == pristinePolish,
    "folder stamp refuses invalid page without touching review or computation");
Check(Level3ReviewRules.StampAt(placedPolish, Level3ReviewRules.StampReady, 0, float.NaN, 70f, -8f, out _) == Level3CommandOutcome.Refused && Snap(placedPolish) == pristinePolish,
    "folder stamp refuses nonfinite position without mutation");
Check(Level3ReviewRules.StampAt(placedPolish, Level3ReviewRules.StampReady, 0, 30f, 70f, -8f, out _) == Level3CommandOutcome.Applied,
    "shared stamp contact records readiness and free placement in the existing candidate");
var inkPolish = placedPolish.Case(Level3ChapterRules.BeaCaseId).stamp;
Check(inkPolish.placed && inkPolish.page == 0 && inkPolish.x == 30f && inkPolish.y == 70f && inkPolish.tilt == -8f && Level3ReviewRules.StampCurrent(placedPolish, Level3ChapterRules.BeaCaseId),
    "placed ink covers current computation and keeps exact paper coordinates/angle");
string repeatPolish = Snap(placedPolish);
Check(Level3ReviewRules.StampAt(placedPolish, Level3ReviewRules.StampReady, 0, 30f, 70f, -8f, out _) == Level3CommandOutcome.Duplicate && Snap(placedPolish) == repeatPolish,
    "same position and recommendation is duplicate, not a reward or revision");
Check(Level3ReviewRules.StampAt(placedPolish, Level3ReviewRules.StampReady, 1, -90f, 120f, 5f, out _) == Level3CommandOutcome.Applied && inkPolish.page == 1,
    "same current recommendation can be moved to another folder page without recomputation");
var inkCopyPolish = placedPolish.Copy().Case(Level3ChapterRules.BeaCaseId).stamp;
var inkBackPolish = Clone(placedPolish).Case(Level3ChapterRules.BeaCaseId).stamp;
Check(Snap(inkPolish) == Snap(inkCopyPolish) && Snap(inkPolish) == Snap(inkBackPolish), "stamp page/position/tilt survive copy and serialization");
Check(!Level3ReviewRules.CaseDone(placedPolish) && Snap(placedPolish.Case(Level3ChapterRules.BeaCaseId).computation) == Snap(pay8.Case(Level3ChapterRules.BeaCaseId).computation),
    "placing ink never approves or edits the actual calculation");
inkCopyPolish.x = float.PositiveInfinity;
Check(inkCopyPolish.Repair() && !inkCopyPolish.placed && inkCopyPolish.type == Level3ReviewRules.StampReady,
    "corrupt optional ink geometry is cleared without erasing the saved readiness decision");
var noFactsPolish = new CaseData();
var contextPolish = Level3ChapterRules.CreateEntry();
Level3CaseFolder.FillKnownClientData(noFactsPolish, bea, contextPolish);
Check(noFactsPolish.taxpayerType == null && noFactsPolish.incomeSource == null && noFactsPolish.folderTransferredFields.Count == 0,
    "unacquired client background cannot prefill classification controls");
contextPolish.Case(Level3ChapterRules.BeaCaseId).acquiredSourceIds.AddRange(new[] { "B-ID", "B-BG" });
Level3CaseFolder.FillKnownClientData(noFactsPolish, bea, contextPolish);
Check(noFactsPolish.taxpayerType == TaxpayerType.Professional && noFactsPolish.incomeSource == null &&
      noFactsPolish.folderTransferredFields.Contains("TaxpayerType") && noFactsPolish.folderTransferredFields.Contains("ResidencyStatus"),
    "supplied professional context fills the existing typed transfer fields, not a new summary");
Check(noFactsPolish.residencyStatus == ResidencyStatus.ResidentCitizen && noFactsPolish.tin == "312-845-679-000",
    "the folder retains unknown citizenship and fills the TIN from the acquired ID");
Check(Level3CaseFolder.ClientHeading == "Taxpayer Information", "front page keeps the familiar Level 2 title");

// Calculator presentation repair: actual transfer command with immutable snapshots.
int repairStart = checks;
var transferCalc = new Level3Calculator();
var transferSource = new Level3CalculatorTransferValue("lesson/2026/v1/rev0", "LESSON-FUND", "Supply fund", "1250.00");
DataValue Payload(Level3CalculatorTransferValue v) => new DataValue(v, "display text is not authority", DataValueType.Number, "Level3:Calculator/" + v.SourceId);
var transferValue = Payload(transferSource);
Check(Level3CalculatorTransferValue.TryInsert(transferValue, transferSource, transferCalc) &&
      transferCalc.EntrySource.sourceId == transferSource.SourceId && transferCalc.EntrySource.value == "1250" && !transferCalc.EntrySource.manual,
    "shared placement command inserts one exact sourced operand without parsing display text");
// Tap-select/place and drag/drop pass the same captured DataValue to this command.
var dragCalcRepair = new Level3Calculator();
Check(Level3CalculatorTransferValue.TryInsert(transferValue, transferSource, dragCalcRepair) &&
      Snap(transferCalc.EntrySource) == Snap(dragCalcRepair.EntrySource) && transferCalc.Tokens.Count == 0 && dragCalcRepair.Tokens.Count == 0,
    "tap and drop placement produce equivalent sourced operands, without appending another operand");
string beforeRepair = Snap(transferCalc.EntrySource);
foreach (var changed in new[] {
    new Level3CalculatorTransferValue("case/2026/v1/rev0", transferSource.SourceId, transferSource.Label, transferSource.Amount),
    new Level3CalculatorTransferValue("lesson/2026/v2/rev0", transferSource.SourceId, transferSource.Label, transferSource.Amount),
    new Level3CalculatorTransferValue("lesson/2026/v1/rev1", transferSource.SourceId, transferSource.Label, transferSource.Amount),
    new Level3CalculatorTransferValue(transferSource.Context, "different-source", transferSource.Label, transferSource.Amount),
    new Level3CalculatorTransferValue(transferSource.Context, transferSource.SourceId, transferSource.Label, "1250.01"),
    new Level3CalculatorTransferValue(transferSource.Context, transferSource.SourceId, transferSource.Label, transferSource.Amount, true),
    new Level3CalculatorTransferValue(transferSource.Context, transferSource.SourceId, transferSource.Label, transferSource.Amount, draftKey: "old-print") })
    Check(!Level3CalculatorTransferValue.TryInsert(transferValue, changed, transferCalc) && Snap(transferCalc.EntrySource) == beforeRepair,
        "changed case/content/revision/source/amount/kind is refused before mutation");
Check(!Level3CalculatorTransferValue.TryInsert(new DataValue(1250f, "1250", DataValueType.Number, "Level3:Calculator"), transferSource, transferCalc) &&
      !Level3CalculatorTransferValue.TryInsert(null, transferSource, transferCalc) && Snap(transferCalc.EntrySource) == beforeRepair,
    "float and canceled empty drop do not mutate the current operand");
Check(!Level3CalculatorTransferValue.TryInsert(new DataValue(transferSource, "1,250.00", DataValueType.Text, transferValue.SemanticKey), transferSource, transferCalc) &&
      !Level3CalculatorTransferValue.TryInsert(new DataValue(transferSource, "1,250.00", DataValueType.Number, "GrossIncome"), transferSource, transferCalc),
    "payloads with wrong data kind or legacy semantic keys cannot cross the calculator boundary");
var exactSourceRepair = new Level3CalculatorTransferValue("case", "penny-row", "Exact fraction", "0.123456");
Check(Level3CalculatorTransferValue.TryInsert(Payload(exactSourceRepair), exactSourceRepair, transferCalc) && transferCalc.Entry == "0.123456",
    "six exact fractional digits survive the shared transfer command");
transferCalc.Press(Level3CalcKey.D7);
Check(transferCalc.EntrySource == null, "typed keypad input stays distinct from transferred provenance");
var repairPrint = new Level3Calculator();
repairPrint.SelectSource("lesson-fund", "Supply fund", "1250");
repairPrint.Press(Level3CalcKey.Subtract);
repairPrint.SelectSource("lesson-ink", "Spent on ink", "480");
repairPrint.Press(Level3CalcKey.Equals);
long printedRevisionRepair = repairPrint.InputRevision;
var capturedPrintRepair = new Level3CalculatorTransferValue("lesson/rev0", "printed-tape", "Result", repairPrint.DraftResult, draftKey: printedRevisionRepair.ToString());
repairPrint.Press(Level3CalcKey.Clear);
repairPrint.SelectSource("other-fund", "Other", "1250");
repairPrint.Press(Level3CalcKey.Subtract);
repairPrint.SelectSource("other-ink", "Other ink", "480");
repairPrint.Press(Level3CalcKey.Equals);
Check(repairPrint.DraftResult == capturedPrintRepair.Amount && !capturedPrintRepair.Matches(
    new Level3CalculatorTransferValue(capturedPrintRepair.Context, "printed-tape", "Result", repairPrint.DraftResult, draftKey: repairPrint.InputRevision.ToString())),
    "changed draft with identical result rejects an earlier immutable tape payload");
Check(!Level3CalculatorTransferValue.TryInsert(Payload(capturedPrintRepair), capturedPrintRepair, transferCalc), "result tapes cannot masquerade as operand sources");
// Drive the actual shared mediator without a native Unity scene. Endpoints are
// pure interface implementations; no EventSystem/render/device claim follows.
var sharedRepair = (ValueTransferManager)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ValueTransferManager));
var sharedCalcRepair = new Level3Calculator();
var endpointRepair = new CalculatorRepairEndpoint(transferValue, transferSource, sharedCalcRepair);
int successRepair = 0, rejectedRepair = 0;
sharedRepair.OnTransferSucceeded += _ => successRepair++;
sharedRepair.OnTransferFailed += (_, _) => rejectedRepair++;
sharedRepair.SelectValue(endpointRepair, endpointRepair);
Check(sharedRepair.HasSelection && sharedCalcRepair.EntrySource == null, "native manager selection is visible/non-destructive before placement");
sharedRepair.TryPlaceOnDestination(endpointRepair);
Check(!sharedRepair.HasSelection && successRepair == 1 && endpointRepair.Received == 1 && sharedCalcRepair.EntrySource.sourceId == transferSource.SourceId,
    "tap-place through actual shared manager accepts once and clears selection");
sharedRepair.TryPlaceOnDestination(endpointRepair);
Check(endpointRepair.Received == 1 && successRepair == 1, "duplicate drop/click callback cannot place after the shared selection was consumed");
sharedRepair.SelectValue(endpointRepair, endpointRepair); // the drag source uses this same manager
sharedRepair.TryPlaceOnDestination(endpointRepair);
Check(endpointRepair.Received == 2 && successRepair == 2 && !sharedRepair.HasSelection,
    "drag placement enters the same manager and command as tap placement");
sharedRepair.SelectValue(endpointRepair, endpointRepair);
endpointRepair.Current = new Level3CalculatorTransferValue("other-case", transferSource.SourceId, transferSource.Label, transferSource.Amount);
sharedRepair.TryPlaceOnDestination(endpointRepair);
Check(sharedRepair.HasSelection && rejectedRepair == 1 && endpointRepair.Received == 2,
    "rejected stale/foreign drop keeps selection and has no partial placement");
sharedRepair.ClearSelection();
Check(!sharedRepair.HasSelection && endpointRepair.Received == 2, "cancel/off-target cleanup cannot place, consume or reward");
sharedRepair.SelectValue(endpointRepair, endpointRepair);
sharedRepair.SelectValue(endpointRepair, endpointRepair);
Check(!sharedRepair.HasSelection, "tapping selected source again explicitly cancels");

// Actual production hit math: targets follow their current rendered transform,
// independent of Physics.autoSyncTransforms, camera masks and capture layers.
var pointerRay = new UnityEngine.Ray(new UnityEngine.Vector3(0f, 0f, -1f), UnityEngine.Vector3.forward);
var pointerBox = new UnityEngine.Bounds(UnityEngine.Vector3.zero, new UnityEngine.Vector3(1f, 1f, 0.02f));
var pointerIdentity = UnityEngine.Matrix4x4.identity;
Check(Level3DeskToolHitTarget.HitBox(pointerRay, pointerIdentity, pointerBox, out float initialHit) && Math.Abs(initialHit - 0.99f) < 0.00001f,
    "current local-box picking hits the visible front face at the expected world distance");
var pointerMoved = pointerIdentity;
pointerMoved.m03 = -10f;
Check(!Level3DeskToolHitTarget.HitBox(pointerRay, pointerMoved, pointerBox, out _), "moving a tool immediately removes its previous hit position");
Check(Level3DeskToolHitTarget.HitBox(new UnityEngine.Ray(new UnityEngine.Vector3(10, 0, -1), UnityEngine.Vector3.forward), pointerMoved, pointerBox, out float movedHit) &&
    Math.Abs(initialHit - movedHit) < 0.00001f, "moved tool is immediately clickable at its new position without a fixed physics step");
var pointerScaleNear = pointerIdentity;
pointerScaleNear.m00 = pointerScaleNear.m11 = pointerScaleNear.m22 = 0.5f;
pointerScaleNear.m23 = -1f; // world centre z=2, scale=2
var pointerScaleFar = pointerIdentity;
pointerScaleFar.m00 = pointerScaleFar.m11 = pointerScaleFar.m22 = 2f;
pointerScaleFar.m23 = -8f; // world centre z=4, scale=0.5
var originRay = new UnityEngine.Ray(UnityEngine.Vector3.zero, UnityEngine.Vector3.forward);
Check(Level3DeskToolHitTarget.HitBox(originRay, pointerScaleNear, pointerBox, out float nearTool) &&
    Level3DeskToolHitTarget.HitBox(originRay, pointerScaleFar, pointerBox, out float farTool) && nearTool < farTool &&
    Math.Abs(nearTool - 1.98f) < 0.00001f && Math.Abs(farTool - 3.995f) < 0.00001f,
    "differently scaled content targets are ordered by world distance, not normalized local-ray distance");
var rotatedPointer = pointerIdentity;
rotatedPointer.m00 = rotatedPointer.m22 = 0f; rotatedPointer.m02 = -1f; rotatedPointer.m20 = 1f;
Check(Level3DeskToolHitTarget.HitBox(pointerRay, rotatedPointer, new UnityEngine.Bounds(UnityEngine.Vector3.zero, new UnityEngine.Vector3(0.02f, 1f, 1f)), out float rotatedHit) &&
    Math.Abs(rotatedHit - 0.99f) < 0.00001f, "oriented paper/calculator faces remain pickable without world-aligned collider bounds");
Check(!Level3DeskToolHitTarget.HitBox(new UnityEngine.Ray(new UnityEngine.Vector3(0.6f, 0, -1), UnityEngine.Vector3.forward), pointerIdentity, pointerBox, out _),
    "parallel ray outside the visible target is not a hit");
Check(!Level3DeskToolHitTarget.HitBox(new UnityEngine.Ray(new UnityEngine.Vector3(0, 0, 1), UnityEngine.Vector3.forward), pointerIdentity, pointerBox, out _),
    "targets behind the pointer ray cannot intercept a click");
Check(Level3DeskToolHitTarget.HitBox(new UnityEngine.Ray(new UnityEngine.Vector3(0.5f, 0, -1), UnityEngine.Vector3.forward), pointerIdentity, pointerBox, out _),
    "visible target edge is included rather than becoming a dead strip");
var tooFarPointer = pointerIdentity; tooFarPointer.m23 = -5f;
Check(!Level3DeskToolHitTarget.HitBox(originRay, tooFarPointer, pointerBox, out _), "desk picking stays within its bounded working distance");

// Exercise the actual model body/handle predicate without Unity object creation.
// Distinct non-null identity tokens avoid invoking native collider/transform APIs.
UnityEngine.Collider HitToken(int identity)
{
    var token = (UnityEngine.BoxCollider)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(UnityEngine.BoxCollider));
    var entityField = typeof(UnityEngine.Object).GetField("m_EntityId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var entity = Activator.CreateInstance(entityField.FieldType);
    foreach (var field in entityField.FieldType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        field.SetValue(entity, Convert.ChangeType(identity, field.FieldType));
    entityField.SetValue(token, entity);
    typeof(UnityEngine.Object).GetField("m_CachedPtr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(token, new IntPtr(1));
    return token;
}
foreach (var modelType in new[] { typeof(Level3CalculatorModel), typeof(Level3PracticeCardModel), typeof(Level3ComputationLeafModel) })
{
    var model = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(modelType);
    var bodyToken = HitToken(9001); var handleToken = HitToken(9002); var edgeToken = HitToken(9003); var contentToken = HitToken(9004);
    void SetModel(string name, object value) => modelType.GetField("<" + name + ">k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(model, value);
    SetModel("BodyTarget", bodyToken); SetModel("MoveHandle", handleToken); SetModel("MoveBorders", new[] { edgeToken });
    bool Movable(UnityEngine.Collider token) => (bool)modelType.GetMethod("IsMoveHandle").Invoke(model, new object[] { token });
    Check(Movable(bodyToken), modelType.Name + ": ordinary blank body starts physical movement");
    Check(Movable(handleToken) && Movable(edgeToken), modelType.Name + ": clip/roll/borders still move");
    Check(!Movable(contentToken), modelType.Name + ": actionable content never falls back to parent movement");
}

foreach (bool sourceSurface in new[] { false, true })
foreach (bool tapeSurface in new[] { false, true })
{
    var adapter = (Level3TapeTransfer)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Level3TapeTransfer));
    typeof(Level3TapeTransfer).GetField("source", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(adapter, sourceSurface);
    typeof(Level3TapeTransfer).GetField("<IsTape>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(adapter, tapeSurface);
    Check(adapter.IsPrintedStrip == (sourceSurface && tapeSurface) && adapter.ShowValueDragVisual == !(sourceSurface && tapeSurface),
        "only printer strip suppresses shared value ghost; numeric/attached-result sources retain it");
}
// Shared drag lifetime can be exercised without native Transform/rendering calls.
var dragLifetime = (ValueDragSource)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ValueDragSource));
var dragFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var activeField = typeof(ValueDragSource).GetField("activeDrag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
typeof(UnityEngine.Object).GetField("m_CachedPtr", dragFlags).SetValue(dragLifetime, new IntPtr(1));
foreach (bool cancelLifetime in new[] { false, true })
{
    typeof(ValueDragSource).GetField("isDragging", dragFlags).SetValue(dragLifetime, true);
    activeField.SetValue(null, dragLifetime);
    Check(ValueDragSource.IsAnyDragActive, "shared value drag holds its active marker during physical/numeric view");
    if (cancelLifetime) typeof(ValueDragSource).GetMethod("OnDisable", dragFlags).Invoke(dragLifetime, null);
    else dragLifetime.OnEndDrag(null);
    Check(!ValueDragSource.IsAnyDragActive, "end/disabled cancellation releases shared global drag marker");
}

// Gesture policy is called by the EventSystem owner, not a second input path.
for (int bits = 0; bits < 8; bits++)
{
    bool value = (bits & 1) != 0, physical = (bits & 2) != 0, body = (bits & 4) != 0;
    var role = Level3DeskToolHitTarget.Classify(value, physical, body);
    Check(value ? role == (physical ? Level3DeskToolHitTarget.GestureRole.PrintedStrip : Level3DeskToolHitTarget.GestureRole.Value)
        : role == (body ? Level3DeskToolHitTarget.GestureRole.Tool : Level3DeskToolHitTarget.GestureRole.Content),
        "press classification prioritizes source content over parent body");
    Check(!Level3DeskToolHitTarget.AllowsDrop(role, false), "press/slight wobble cannot become a drag drop");
    Check(Level3DeskToolHitTarget.AllowsDrop(role, true) == value, "only captured value gestures can place on drag release (never keys/body/display)");
}
Check(Level3DeskCalculator.PreferToolHit(false, 0.98f, true, 0.9f) &&
    !Level3DeskCalculator.PreferToolHit(true, 0.9f, false, 0.98f), "content beats overlapping broad body in both target enumeration orders");
Check(Level3DeskCalculator.PreferToolHit(false, 0.9f, false, 0.98f) &&
    !Level3DeskCalculator.PreferToolHit(false, 0.98f, false, 0.9f), "equal-priority content still picks the nearest face");
for (int bits = 0; bits < 16; bits++)
{
    bool captured = (bits & 1) != 0, committed = (bits & 2) != 0, canceled = (bits & 4) != 0, current = (bits & 8) != 0;
    var release = Level3DeskCalculator.ResolveStripRelease(captured, committed, canceled, current);
    Check(release == (!captured ? Level3DeskCalculator.StripRelease.None : committed ? Level3DeskCalculator.StripRelease.Flight :
        canceled || !current ? Level3DeskCalculator.StripRelease.Restore : Level3DeskCalculator.StripRelease.Return),
        "strip release preserves committed ownership, restores cancel/stale, returns invalid drop");
}
foreach (float baseSize in new[] { 0.001f, 0.5f, 1f })
{
    var rest = new UnityEngine.Vector3(baseSize, baseSize * 2f, baseSize * 0.75f);
    foreach (float t in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
    {
        var shown = Level3CalculatorModel.PlacementScale(rest, t);
        Check(shown.x >= rest.x - 0.000001f && shown.x <= rest.x * 1.046f &&
            Math.Abs(shown.y / shown.x - 2f) < 0.00001f && Math.Abs(shown.z / shown.x - 0.75f) < 0.00001f,
            "neutral arrival preserves actual nonuniform/point-to-meter scale, never grows from nothing");
    }
    Check(Level3CalculatorModel.PlacementScale(rest, 0f) == rest && Level3CalculatorModel.PlacementScale(rest, 1f) == rest,
        "arrival starts and finishes at the captured base scale");
}

// Tools may overlap and cross the former divider. Only the shared screen
// margin bounds them, including letterboxed and portrait viewports.
foreach (var workspaceView in new[] { new UnityEngine.Rect(0, 0, 1040, 580), new UnityEngine.Rect(32, 24, 1920, 1080), new UnityEngine.Rect(0, 0, 580, 1040) })
{
    var workspaceArea = Level3DeskCalculator.ToolMoveArea(workspaceView);
    Check(workspaceArea == workspaceView, "whole viewport is available, with no paper/calculator/HUD columns");
    var workspaceBox = new UnityEngine.Rect(workspaceView.center - workspaceView.size * 0.2f, workspaceView.size * 0.4f);
    var workspaceHeld = HeldOnScreen.FromScreenBox(workspaceBox, workspaceBox.center);
    foreach (var aim in new[] { new UnityEngine.Vector2(-9999, -9999), new UnityEngine.Vector2(9999, -9999), new UnityEngine.Vector2(-9999, 9999), new UnityEngine.Vector2(9999, 9999), workspaceView.center })
    {
        var sharedClamp = workspaceHeld.Clamp(aim, workspaceView);
        var actualClamp = Level3DeskCalculator.ClampToolMove(workspaceHeld, aim, workspaceArea);
        Check((actualClamp - sharedClamp).sqrMagnitude < 0.001f, "tools use the existing held-document clamp at every screen corner");
        var movedBox = new UnityEngine.Rect(actualClamp - workspaceBox.size * 0.5f, workspaceBox.size);
        Check(movedBox.xMin >= workspaceView.xMin + HeldOnScreen.Margin - 0.001f &&
            movedBox.xMax <= workspaceView.xMax - HeldOnScreen.Margin + 0.001f &&
            movedBox.yMin >= workspaceView.yMin + HeldOnScreen.Margin - 0.001f &&
            movedBox.yMax <= workspaceView.yMax - HeldOnScreen.Margin + 0.001f,
            "entire physical assembly remains inside screen with the shared edge margin");
    }
    var acrossScreen = Level3DeskCalculator.ClampToolMove(workspaceHeld, new UnityEngine.Vector2(9999, workspaceView.center.y), workspaceArea);
    Check(acrossScreen.x - workspaceBox.width * 0.5f > workspaceView.center.x,
        "paper can cross the old divider entirely onto the calculator side");
}

// Actual receipt motion/fit math: continuous pickup/release endpoints and a
// field-sized handoff, for either the lesson slot or computation rows.
var receiptFrom = new UnityEngine.Vector3(-0.2f, 0.15f, 0.6f);
var receiptTo = new UnityEngine.Vector3(0.18f, -0.1f, 0.6f);
Check(Level3DeskCalculator.ReceiptFlightPosition(receiptFrom, receiptTo, UnityEngine.Vector3.up, 0.012f, 0f) == receiptFrom &&
      Level3DeskCalculator.ReceiptFlightPosition(receiptFrom, receiptTo, UnityEngine.Vector3.up, 0.012f, 1f) == receiptTo,
    "drop flight starts at the released physical pose and ends exactly at its field");
float previousReceiptEase = -1f;
foreach (float receiptT in new[] { -1f, 0f, 0.001f, 0.1f, 0.25f, 0.5f, 0.75f, 0.9f, 0.999f, 1f, 2f })
{
    float ease = Level3DeskCalculator.ReceiptEase(receiptT);
    Check(ease >= previousReceiptEase - 0.000001f && ease >= 0f && ease <= 1.000001f, "receipt easing stays monotonic and bounded without overshoot");
    previousReceiptEase = ease;
}
var receiptFirstStep = Level3DeskCalculator.ReceiptFlightPosition(receiptFrom, receiptTo, UnityEngine.Vector3.up, 0.012f, 0.001f);
var receiptLastStep = Level3DeskCalculator.ReceiptFlightPosition(receiptFrom, receiptTo, UnityEngine.Vector3.up, 0.012f, 0.999f);
Check((receiptFirstStep - receiptFrom).magnitude < 0.000001f && (receiptLastStep - receiptTo).magnitude < 0.000001f,
    "receipt lift/flight eases at both ends, including the arch (no snap into/out of a sine arc)");
foreach (var printedSize in new[] { new UnityEngine.Vector2(0.105f, 0.042f), new UnityEngine.Vector2(0.105f, 0.085f) })
foreach (var slotSize in new[] { Level3PracticeCardModel.AttachedTapeSize, Level3ComputationLeafModel.AttachedTapeSize })
foreach (float destinationScale in new[] { 0.5f, 1f, 1.8f })
{
    var fit = Level3DeskCalculator.FitReceiptScale(printedSize, slotSize, destinationScale);
    Check(Math.Abs(printedSize.x * fit.x - slotSize.x * destinationScale) < 0.000001f &&
          Math.Abs(printedSize.y * fit.z - slotSize.y * destinationScale) < 0.000001f && fit.y == destinationScale,
        "full physical strip lands at actual lesson/computation slot dimensions, preserving paper thickness scale");
}
var receiptRestOffset = Level3CalculatorModel.PlacementOffset(1f);
Check(receiptRestOffset == UnityEngine.Vector3.zero && Level3CalculatorModel.PlacementOffset(0f).y > 0f,
    "landing begins slightly above the clip and settles exactly onto the stored paper plane");
float previousReceiptHeight = float.MaxValue;
foreach (float receiptT in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
{
    var offset = Level3CalculatorModel.PlacementOffset(receiptT);
    Check(offset.x == 0f && offset.z == 0f && offset.y <= previousReceiptHeight && offset.y >= 0f,
        "receiving paper settles vertically without sideways jumps or sinking below its rest pose");
    previousReceiptHeight = offset.y;
}

// A foreground reservation must survive more than a one-shot pointer-down.
// Native Canvas ordering/occlusion is checked by DeskToolLayerValidation in Unity.
var foregroundProperty = typeof(Doc3DLayer).GetProperty("KeepInFront");
Check(foregroundProperty != null && foregroundProperty.PropertyType == typeof(bool),
    "document layer exposes an opt-in foreground lifetime for captured tools and physical receipt");

foreach (bool pointerHeldForLayer in new[] { false, true })
foreach (bool receiptVisibleForLayer in new[] { false, true })
    Check(Level3DeskToolHitTarget.ReserveAssemblyForeground(pointerHeldForLayer, receiptVisibleForLayer) ==
        (pointerHeldForLayer && !receiptVisibleForLayer),
        "assembly keeps foreground only for its capture; physical receipt takes precedence through drag/return/flight");
var pinLayer = (Doc3DLayer)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Doc3DLayer));
Check(!pinLayer.KeepInFront, "shared documents keep their existing default ordering unless explicitly reserved");
pinLayer.KeepInFront = true;
Check(pinLayer.KeepInFront, "foreground reservation survives between frames until its owner releases it");
pinLayer.KeepInFront = false;
Check(!pinLayer.KeepInFront, "released reservation does not become a permanent always-on-top document");

Console.WriteLine($"Calculator repair: {checks - repairStart} command assertions (native gesture/rendering checks remain manual).");

// =====================================================================
// Revised T04 (4 November 2026): Bea's compact records and the paper-machine table
// =====================================================================
int t04rStart = checks, t04rFails = fails;
string[] nine4r = Level3ChapterRules.BeaHandoverSources.ToArray();

// ---------- acquisition once, through the signed store (disposable, under the project's Temp) ----------
string sandbox4r = Path.Combine(root, "Temp", "L3Repair", "T04-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox4r);
try
{
    string Encode4r(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    PlayerProgressData Decode4r(string text)
    {
        using JsonDocument doc = JsonDocument.Parse(text);
        string payload = doc.RootElement.GetProperty("payload").GetString();
        return SaveFileStore.SignatureMatches(payload, doc.RootElement.GetProperty("signature").GetString()) ? JsonSerializer.Deserialize<PlayerProgressData>(payload, json) : null;
    }
    // Level3Checkpointer.CommitBeaRecords: candidate copy -> CollectBeaRecords -> signed write -> publish (mirrored on a disposable store).
    string Hand4r(SaveFileStore st, ref PlayerProgressData pub)
    {
        Level3ChapterCheckpoint candidate = pub.campaign.levelThree.Copy();
        Level3CommandOutcome outcome = Level3ChapterRules.CollectBeaRecords(candidate, out _);
        if (outcome == Level3CommandOutcome.Duplicate) return "AlreadyAttached";
        if (outcome != Level3CommandOutcome.Applied) return "Refused";
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub), candidate);
        try { next.Normalize(); st.Write(Encode4r(next)); }
        catch (Exception) { return "SaveFailed"; }
        pub = next;
        return "Attached";
    }
    var store4r = new SaveFileStore(Path.Combine(sandbox4r, "progress.json"));
    PlayerProgressData pub4r = Level3ChapterRules.WithCheckpoint(Clone(live), fresh2);
    store4r.Write(Encode4r(pub4r));
    byte[] disk4r = File.ReadAllBytes(store4r.FilePath);
    string before4r = Snap(pub4r);

    Directory.CreateDirectory(store4r.TempPath); // injected failure
    string failed4r = Hand4r(store4r, ref pub4r);
    Check(failed4r == "SaveFailed" && Snap(pub4r) == before4r && File.ReadAllBytes(store4r.FilePath).SequenceEqual(disk4r) &&
          Level3TableStaging.TableSources(pub4r.campaign.levelThree.Case(B7).acquiredSourceIds).Count == 0 && pub4r.campaign.levelThree.phase == "BeaRecords",
        "revised T04: a failed handover write publishes nothing (no source on the table, Bea still at BeaRecords, disk bytes unchanged)");
    Directory.Delete(store4r.TempPath);

    string ok4r = Hand4r(store4r, ref pub4r);
    PlayerProgressData disk4rBack = Decode4r(File.ReadAllText(store4r.FilePath));
    List<string> got4r = pub4r.campaign.levelThree.Case(B7).acquiredSourceIds;
    Check(ok4r == "Attached" && got4r.SequenceEqual(nine4r) && !got4r.Contains("B-WHT") && pub4r.campaign.levelThree.phase == "WorksheetDraft" &&
          disk4rBack != null && Snap(disk4rBack.campaign.levelThree) == Snap(pub4r.campaign.levelThree) && WithoutChapter(disk4rBack) == liveWithout,
        "revised T04: the retry hands over exactly the nine originals (no B-WHT), saved before published, every carried possession intact");
    string afterOnce4r = Snap(pub4r);
    byte[] diskOnce4r = File.ReadAllBytes(store4r.FilePath);
    Check(Hand4r(store4r, ref pub4r) == "AlreadyAttached" && Hand4r(store4r, ref pub4r) == "AlreadyAttached" &&
          Snap(pub4r) == afterOnce4r && File.ReadAllBytes(store4r.FilePath).SequenceEqual(diskOnce4r),
        "revised T04: repeated completed talks or restores acquire nothing more and write nothing");
    Check(pub4r.campaign.coins == live.campaign.coins && pub4r.campaign.publicTrust == live.campaign.publicTrust &&
          pub4r.campaign.levelThree.worksheet.strips.Count == 0 && pub4r.campaign.levelThree.worksheet.revision == 0,
        "revised T04: admission grades, places and awards nothing (no coins/trust, empty draft)");

    // Restore (reload from disk): the table, the stack and the Case Folder all derive the same nine identities, once.
    Level3ChapterCheckpoint restored4r = disk4rBack.campaign.levelThree;
    List<string> table4r = Level3TableStaging.TableSources(restored4r.Case(B7).acquiredSourceIds);
    List<ClientRecord> recs4r = Level3RecordRules.Build(bea, table4r), again4r = Level3RecordRules.Build(bea, table4r);
    Check(table4r.SequenceEqual(nine4r) && recs4r.Count == 9 && recs4r.Select(r => r.id).Distinct().Count() == 9 &&
          recs4r.Select(r => r.id).SequenceEqual(again4r.Select(r => r.id)) &&
          Level3RecordRules.Build(bea, restored4r.Case(B7).acquiredSourceIds).Select(r => r.id).SequenceEqual(recs4r.Select(r => r.id)),
        "revised T04: a quiet restore rebuilds the same nine papers for the table and the Case Folder (reopening never creates another record)");
    Check(Level3CaseFolder.RecordKey(restored4r) == "L3-BEA-001|" + string.Join(",", nine4r.OrderBy(x => x, StringComparer.Ordinal)),
        "revised T04: the folder's record set is the same nine sources (Bag/folder access unchanged)");
}
finally
{
    try { Directory.Delete(sandbox4r, true); } catch (IOException) { }
}
List<string> withCredit4r = Level3TableStaging.TableSources(nine4r.Concat(new[] { "B-WHT", "B-REV", null, "" }));
Check(withCredit4r.SequenceEqual(nine4r), "revised T04: the table never holds B-WHT (office credit step, T08), repeats or blank ids");

// ---------- one original at a time: the stack model ----------
List<string> order4r = Level3RecordRules.Build(bea, nine4r).Select(r => r.source).ToList();
var stack4r = new Level3TableStaging.Stack(order4r.Concat(new[] { "B-WHT" }));
Check(stack4r.Count == 9 && stack4r.Order.SequenceEqual(order4r) && stack4r.Out == null && stack4r.Top == "B-ID" && !stack4r.Contains("B-WHT"),
    "revised T04: one compact stack of the nine originals, ID card on top, nothing out");
stack4r.Prioritize("B-PRINT", "B-EXP");
Check(stack4r.Order.Take(2).SequenceEqual(new[] { "B-PRINT", "B-EXP" }), "Current record and comparison source are the visible top two originals");
string pileBeforeLift = string.Join(",", stack4r.Order);
Check(stack4r.OpenPair("B-PRINT", "B-EXP", out _) && string.Join(",", stack4r.Order) == pileBeforeLift,
    "Lifting an original leaves its holder and its comparison partner in the same real pile order");
stack4r.Close("B-PRINT");
Check(stack4r.Order[0] == "B-PRINT" && stack4r.Out == "B-EXP", "Returning the first original puts it on top and leaves the partner independently open");
stack4r.Close("B-EXP");
Check(stack4r.Order[0] == "B-EXP" && stack4r.Out == null && stack4r.Count == 9, "The second original also returns to the visible top without losing a record");
stack4r.Prioritize("B-REV");
Check(stack4r.Order[0] == "B-REV" && !stack4r.Open("B-WHT", out _) && !stack4r.Open("unknown", out _),
    "The next current row becomes top; off-table sources cannot be lifted");
stack4r.Open("B-REV", out _); stack4r.CloseAll();
Check(stack4r.Count == 9 && stack4r.Out == null && stack4r.Order.OrderBy(x => x).SequenceEqual(order4r.OrderBy(x => x)), "Interruptions preserve every original exactly once");

// ---------- the stand card: a view of the first unfiled row (reads only) ----------
List<Level3WorksheetRow> rows4r = Level3WorksheetRules.RowsOf(bea);
Level3ChapterCheckpoint handed4r = freshHanded.Copy();
handed4r.worksheet = new Level3WorksheetDraft();
string handedSnap4r = Snap(handed4r);
Level3WorksheetRow first4r = Level3TableStaging.ActiveRow(handed4r, rows4r);
Level3TableStaging.CardFace face4r = Level3TableStaging.Face(bea, first4r);
Check(first4r?.RowId == "R1" && Snap(handed4r) == handedSnap4r, "revised T04: a fresh draft shows R1 on the stand; reading the card changes nothing");
Check(face4r.Title == "Logo fees" && face4r.Amount == "₱500,000.00" && face4r.Purpose == "Gross earned and collected fees" &&
      face4r.Details == "Bea studio · 2026 · REV-B26" && face4r.Position == "Record 1 of 2",
    "revised T04: the card shows the printed title, a prominent exact amount, the source's purpose, issuer/year/reference and its place in the batch");
var faces4r = rows4r.Select(r => (r, f: Level3TableStaging.Face(bea, r))).ToList();
string[] leaks4r = { "B-REV", "B-EXP", "B-PERSONAL", "B-PRINT", "Income", "Deduct", "Leave Out", "Personal purpose", "Already represented", "Not deductible", "duplicate" };
Check(faces4r.All(x => x.f.Amount == "₱" + Level3Calculator.Format(x.r.Amount)) &&
      faces4r.All(x => !leaks4r.Any(l => (x.f.Title + x.f.Amount + x.f.Purpose + x.f.Details + x.f.Position).Contains(l))),
    "revised T04: every card amount is the row's exact printed value; no card names a source id, a treatment or a reason");
Check(faces4r.Single(x => x.r.RowId == "R6").f.Details == "Island Party Shop · 2026-06-15 · PARTY-26" &&
      faces4r.Single(x => x.r.RowId == "R7").f.Title == "Printing bill, paid" && faces4r.Single(x => x.r.RowId == "R7").f.Details.EndsWith("PRINT-26") &&
      faces4r.Single(x => x.r.RowId == "R5").f.Position == "Record 3 of 3" && faces4r.Single(x => x.r.RowId == "R7").f.Position == "Record 2 of 2",
    "revised T04: single-date receipts show their own date; R7 keeps the shared PRINT-26 reference; positions follow the 2/3/2 batches");
Level3ChapterCheckpoint partial4r = handed4r.Copy();
foreach (string row in new[] { "R1", "R2" }) { Level3WorksheetRules.Extract(partial4r, rows4r, row, out _); Level3WorksheetRules.Place(partial4r, row, "income", out _); }
Level3WorksheetRules.Extract(partial4r, rows4r, "R3", out _);
// T05 pin: only the open batch is offered. Earnings filed, nothing accepted: no card (Check batch is T06's); never a peek at R3.
Check(Level3TableStaging.ActiveRow(partial4r, rows4r) == null, "revised T05: a restored draft with its open batch filed shows no later-batch card on the stand");
Level3ChapterCheckpoint accepted4r = partial4r.Copy();
accepted4r.worksheetCheck.acceptedBatches.Add(Level3WorksheetRules.EarningsBatch);
Check(Level3TableStaging.ActiveRow(accepted4r, rows4r)?.RowId == "R3", "revised T04/T05: once earnings is accepted, the first unfiled costs row (a lifted but unplaced row is still unfiled) is on the stand");
Level3ChapterCheckpoint all4r = handed4r.Copy();
foreach (string row in Level3WorksheetRules.RowIds) { Level3WorksheetRules.Extract(all4r, rows4r, row, out _); Level3WorksheetRules.Place(all4r, row, "supporting", out _); }
Check(Level3TableStaging.ActiveRow(all4r, rows4r) == null && Level3TableStaging.ActiveRow(handed4r, new List<Level3WorksheetRow>()) == null &&
      Level3TableStaging.ActiveRow(null, rows4r)?.RowId == "R1",
    "revised T04: nothing on the stand once every row is filed or without rows; no grading or auto-placement");

// ---------- table composition: compact, inside the mat, apart, turned toward the seat ----------
foreach (var mat4r in new[] { new UnityEngine.Vector2(1.2f, 0.72f), new UnityEngine.Vector2(1.0f, 0.62f), new UnityEngine.Vector2(0.9f, 0.55f), new UnityEngine.Vector2(0.82f, 0.5f), new UnityEngine.Vector2(0.7f, 0.45f), new UnityEngine.Vector2(0.5f, 0.32f) })
{
    var lay = Level3TableStaging.Compute(mat4r, Level3TableStaging.SeatOf(mat4r, mat4r.y / 0.78f * 0.5f));
    var prints = lay.Footprints.ToList();
    bool apart = true;
    for (int a = 0; a < prints.Count; a++) for (int b = a + 1; b < prints.Count; b++) apart &= !prints[a].Overlaps(prints[b]);
    float used = new[] { lay.Envelope, lay.Stand, lay.Machine, lay.Rack }.Sum(r => r.width * r.height);
    Check(prints.All(r => Level3TableStaging.Inside(r, mat4r, Level3TableStaging.EdgeMargin)) && apart && lay.Column.Contains(lay.Machine.center) && lay.Column.Contains(lay.Rack.center),
        $"revised T04 ({mat4r.x:0.00} x {mat4r.y:0.00} m mat): stack, stand, machine and rack stay inside the mat and apart, even turned toward the seat");
    Check(lay.Stand.xMax < 0f && lay.Envelope.center.x < lay.Stand.center.x && lay.Machine.center.x > 0f && lay.Rack.center.x > 0f &&
          lay.Envelope.yMin > lay.Stand.yMax && lay.Rack.yMax < lay.Machine.yMin && used <= mat4r.x * mat4r.y * 0.45f,
        $"revised T04 ({mat4r.x:0.00} m): stand left, stack behind and to the left of the stand, machine forward right behind its rack, under half the mat used");
    Check(lay.Scale > 0f && lay.Scale <= 1.2f && Math.Abs(lay.StandYaw) <= Level3TableStaging.MaxYaw && Math.Abs(lay.MachineYaw) <= Level3TableStaging.MaxYaw &&
          lay.StandYaw <= 0f && lay.MachineYaw >= 0f && lay.Seat.z < -mat4r.y * 0.5f && lay.Seat.y == Level3TableStaging.SeatHeight,
        $"revised T04 ({mat4r.x:0.00} m): parts turn gently toward the seat, which sits behind the mat's near edge");
}
Check(Level3TableStaging.Compute(new UnityEngine.Vector2(1.2f, 0.72f), Level3TableStaging.SeatOf(new UnityEngine.Vector2(1.2f, 0.72f), 0.4f)).Scale >= 1f,
    "revised T04: a full-size mat keeps every part at its designed size (no shrinking of the readable card)");
Check(Level3TableStaging.YawToward(new UnityEngine.Vector2(0f, 0f), new UnityEngine.Vector3(0f, 0.6f, -1f)) == 0f &&
      Math.Abs(Level3TableStaging.YawToward(new UnityEngine.Vector2(-0.5f, 0f), new UnityEngine.Vector3(0f, 0.6f, -0.1f)) + Level3TableStaging.MaxYaw) < 0.0001f,
    "revised T04: a part straight ahead keeps its front; one far to the side turns no further than the clamp");

// ---------- copy: short introduction, clear invitation, guide/objective agree ----------
Level3TownFile town4r = JsonSerializer.Deserialize<Level3TownFile>(File.ReadAllText(Res("Levels/Level_03/town_life.json")), json);
var intro4r = town4r.bea.introduction;
var organizer4r = town4r.bea.organizerIntro;
Check(Level3TownContent.Validate(town4r).Count == 0 && intro4r.Count <= 5 && intro4r.All(l => l.text.Length <= 110) &&
      intro4r.Last().speaker == "Npc" && intro4r.Last().text.Contains("receipt") && intro4r.Last().text.Contains("belongs") &&
      organizer4r.Last().speaker == "Npc" && organizer4r.Last().text.Contains("organizer") && organizer4r.Last().text.Contains("prepare") &&
      !intro4r.Concat(organizer4r).Any(l => l.text.Contains("spread", StringComparison.OrdinalIgnoreCase) || l.text.Contains("Deduct") || l.text.Contains("Leave Out") || l.text.Contains("personal", StringComparison.OrdinalIgnoreCase)),
    "revised T04 (buildup): Bea's introduction is short, gives no answers and ends with her envelope; her organizer line invites preparing a summary");
string table4rSrc = Src("Scripts/Level3/Level3BeaTable.cs");
Check(intro4r.Concat(organizer4r).Where(l => l.speaker != "Player").All(l => table4rSrc.Contains("\"" + l.text + "\"")) &&
      town4r.bea.inquiries.All(i => table4rSrc.Contains("\"" + i.question + "\"") && table4rSrc.Contains("\"" + i.answer + "\"")),
    "revised T04: the built-in Bea mirrors town_life.json's introduction, organizer line and questions word for word");
GuideScript guide4r = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var tableLesson4r = guide4r.lessons.Single(l => l.id == Level3ChapterRules.WorksheetLesson);
var recordsLesson4r = guide4r.lessons.Single(l => l.id == Level3ChapterRules.BeaRecordsLesson);
string tableText4r = string.Join(" ", tableLesson4r.pages.Select(pg => pg.text)) + " " + string.Join(" ", recordsLesson4r.pages.Select(pg => pg.text));
// T05 pin: the table lesson now teaches the machine (feed / choose / pull) instead of T04's reading pages.
Check(guide4r.Validate() == "" && tableLesson4r.pages.All(pg => !string.IsNullOrEmpty(pg.waitFor)) && tableText4r.Contains("card") && tableText4r.Contains("slot") && tableText4r.Contains("lever") &&
      !new[] { "board", "+ ADD", "UNDO", "SUBTOTAL", "tray", "strip", "calculator", "click" }.Any(w => tableText4r.Contains(w, StringComparison.OrdinalIgnoreCase)) &&
      !leaks4r.Skip(4).Any(w => tableText4r.Contains(w)) && tableLesson4r.pages.All(pg => pg.text.Split(' ').Length <= 14),
    "revised T04/T05: Arthur's table hints are short, non-blocking, describe the card/slot/lever and name no board, calculator or answer");
var steps4r = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps4r[1].GetProperty("trigger").GetString() == Level3ChapterRules.RecordsTrigger && steps4r[2].GetProperty("text").GetString().Contains("machine") &&
      !steps4r.Any(st => st.GetProperty("text").GetString().Contains("Practise")),
    "revised T04: objectives read records -> prepare them at the machine, with no budget practice step");

// ---------- wiring: existing owners, one reader, interruptions, UI never saves ----------
string model4r = Src("Scripts/Level3/Level3PaperMachineModel.cs"), staging4r = Src("Scripts/Level3/Level3TableStaging.cs");
string interact4r = Src("Scripts/Level3/Level3BeaInteractable.cs");
Check(Index(table4rSrc, "if (stack != null && stack.Open(source, out string closing)") > 0 && table4rSrc.Contains("if (previous.IsSettledOut) previous.PutBack();") &&
      table4rSrc.Contains("if (other != null && other != paper && (other.IsSettledOut || other.IsOut)) other.Gather();") &&
      table4rSrc.Contains("stack?.Close(source);"),
    "revised T04 wiring: taking an original out returns any other first (one reader through the existing ClientRecordPaper motions)");
int gather4r = Index(table4rSrc, "private void GatherAll()");
string gatherBody4r = table4rSrc.Substring(gather4r, Index(table4rSrc, "public static void Level3Say") - gather4r);
int left4r = Index(table4rSrc, "private void HandleLeftSeat()");
string leftBody4r = table4rSrc.Substring(left4r, gather4r - left4r);
Check(gatherBody4r.Contains("paper.Gather();") && gatherBody4r.Contains("stack?.CloseAll();") && gatherBody4r.Contains("Machine?.Settle();") &&
      leftBody4r.Contains("GatherAll();") && leftBody4r.Contains("SetTouch(false);") &&
      table4rSrc.Contains("if (!focused) CancelDrags();") && table4rSrc.Contains("if (paused) CancelDrags();") &&
      table4rSrc.Contains("if (!free) CancelDrags();") && table4rSrc.Contains("!InGameSettings.IsOpen && !DialogueUI.IsAnyDialogueActive"),
    "revised T04 wiring: leave/disable gather the reader; settings, dialogue, pause and camera moves take touches away; lost focus cancels a drag");
// T05 pin: one direct surface per part, live only for the current step while seated and free.
Check(model4r.Contains("MobileInputGuard.IsDragOrCancelled(eventData)") && model4r.Contains("InGameSettings.IsOpen") && model4r.Contains("AddComponent<Level3TablePaperRaycaster>()") &&
      model4r.Contains("bool on = touchOn;") && model4r.Contains("Live(cardTouch, stand || dragCard);"),
    "revised T04/T05 wiring: the stand card takes only real taps while seated and free (drags/cancels/settings/dialogue ignored)");
foreach (string src in new[] { model4r, staging4r })
    Check(!src.Contains("CommitLevelThreeCheckpoint") && !src.Contains("CommitWorksheet") && !src.Contains("Level3WorksheetRules.Place(") &&
          !src.Contains("LiftScreenHeight") && !src.Contains("Level3CalculatorModel"),
        "revised T04/T05: the staging model and view never save, file, lift a board or build a calculator");
Check(!table4rSrc.Contains("CommitLevelThreeCheckpoint") && table4rSrc.Split("owner.CommitWorksheet(").Length == 2 && table4rSrc.Contains("Level3WorksheetRules.File(candidate, current, filing, out reason)") &&
      !table4rSrc.Contains("Level3WorksheetRules.Place(") && !table4rSrc.Contains("LiftScreenHeight") && !table4rSrc.Contains("Level3CalculatorModel"),
    "revised T05: the table's only save is the lever's one filing (CommitWorksheet -> Level3WorksheetRules.File); no board or calculator");
Check(!model4r.Contains("BoxCollider") && !model4r.Contains("Physics."), "revised T05: the machine's targets are direct rect surfaces (no physics colliders or broadphase)");
int hand4r = Index(interact4r, "private void HandOver()");
string handBody4r = interact4r.Substring(hand4r, Index(interact4r, "// QUESTIONS") - hand4r);
Check(Index(handBody4r, "owner.CommitBeaRecords(") < Index(handBody4r, "table.Present()") && handBody4r.Contains("case Level3Checkpointer.TapeCommit.AlreadyAttached:") &&
      handBody4r.Contains("table.SyncPapers();") && table4rSrc.Contains("if (papersShown) return true;"),
    "revised T04 wiring: papers appear only after the saved handover; a duplicate or restore shows them quietly, once");
foreach (string guid in new[] { "Level3TableStaging.cs.meta", "Level3PaperMachineModel.cs.meta" }.Select(m => System.Text.RegularExpressions.Regex.Match(File.ReadAllText(Path.Combine(root, "Assets/Scripts/Level3", m)), @"guid: (\w+)").Groups[1].Value))
    Check(guid.Length == 32 && Directory.EnumerateFiles(Path.Combine(root, "Assets"), "*.meta", SearchOption.AllDirectories).Count(f => File.ReadAllText(f).Contains("guid: " + guid)) == 1,
        "revised T04: each new script has its own unique .meta GUID");
Check(typeof(Level3StandCardTouch).Assembly.GetType("Level3TablePaperRaycaster") != null,
    "table card and originals have direct EventSystem rectangle ingress independent of transparent graphic mesh culling");
// Execute the actual direct surface intersection math, including the
// tilted/scaled stand and current original-paper pose (no native Canvas).
var tableRect = new UnityEngine.Rect(-120f, -90f, 240f, 180f);
var tableIdentity = UnityEngine.Matrix4x4.identity;
Check(Level3TablePaperRaycaster.HitRect(new UnityEngine.Ray(new UnityEngine.Vector3(0, 0, -1), UnityEngine.Vector3.forward), tableIdentity, tableRect, out float tableAt) && Math.Abs(tableAt - 1f) < 0.00001f,
    "stand card receives a centre hit without a rendered Graphic/transparent mesh/physics target");
Check(Level3TablePaperRaycaster.HitRect(new UnityEngine.Ray(new UnityEngine.Vector3(120, 90, -1), UnityEngine.Vector3.forward), tableIdentity, tableRect, out _),
    "card edge is included so Inspect original has no untappable rim");
Check(!Level3TablePaperRaycaster.HitRect(new UnityEngine.Ray(new UnityEngine.Vector3(121, 0, -1), UnityEngine.Vector3.forward), tableIdentity, tableRect, out _) &&
      !Level3TablePaperRaycaster.HitRect(new UnityEngine.Ray(new UnityEngine.Vector3(0, 0, 1), UnityEngine.Vector3.forward), tableIdentity, tableRect, out _) &&
      !Level3TablePaperRaycaster.HitRect(new UnityEngine.Ray(new UnityEngine.Vector3(0, 0, -1), UnityEngine.Vector3.right), tableIdentity, tableRect, out _),
    "outside/behind/parallel pointers cannot activate a card or machine/table behind it");
foreach (float tableScale in new[] { 0.00024f, 0.001f, 0.5f })
foreach (float tableAngle in new[] { -35f, 0f, 55f })
{
    float sine = (float)Math.Sin(tableAngle * Math.PI / 180.0), cosine = (float)Math.Cos(tableAngle * Math.PI / 180.0);
    var toLocal = UnityEngine.Matrix4x4.identity;
    toLocal.m00 = 1f / tableScale; toLocal.m03 = -2f / tableScale;
    toLocal.m11 = cosine / tableScale; toLocal.m12 = sine / tableScale;
    toLocal.m13 = -(cosine + sine * 4f) / tableScale;
    toLocal.m21 = -sine / tableScale; toLocal.m22 = cosine / tableScale;
    toLocal.m23 = -(-sine + cosine * 4f) / tableScale;
    var centre = new UnityEngine.Vector3(2, 1, 4);
    var normal = new UnityEngine.Vector3(0, -sine, cosine);
    var ray = new UnityEngine.Ray(centre - normal * 1.5f, normal);
    Check(Level3TablePaperRaycaster.HitRect(ray, toLocal, tableRect, out float tableWorldHit) && Math.Abs(tableWorldHit - 1.5f) < 0.0001f,
        "world-space card hit follows current scale/tilt/translation and preserves real distance for stack ordering");
}
Check(typeof(UnityEngine.UI.GraphicRaycaster).IsAssignableFrom(typeof(Level3TablePaperRaycaster)),
    "table ingress remains an EventSystem GraphicRaycaster recognized by shared world-input blocking");

Console.WriteLine($"Revised T04 checks: {checks - t04rStart}, failures {fails - t04rFails} (signed-store acquisition/failure/idempotence, restore identities, one-reader stack, stand card, layout, copy, wiring; Unity rendering, touch, NPC motion not run).");

// =====================================================================
// T04 buildup (4 November 2026): notice -> talk -> envelope -> open -> inspect one record -> organizer -> Help Bea
// =====================================================================
int t04bStart = checks, t04bFails = fails;
const Level3TableStaging.EncounterStep EWaiting = Level3TableStaging.EncounterStep.Waiting, EPresented = Level3TableStaging.EncounterStep.Presented,
    EOpened = Level3TableStaging.EncounterStep.Opened, EInspected = Level3TableStaging.EncounterStep.Inspected,
    EOffered = Level3TableStaging.EncounterStep.Offered, EReady = Level3TableStaging.EncounterStep.Ready;
// At most one table target is live at a step: the seat, the envelope or the forward record (none while her offer is due).
int LiveTargets(Level3TableStaging.Encounter e) => (e.SeatOpen ? 1 : 0) + (e.Step == EPresented ? 1 : 0) + (e.FeaturedForward ? 1 : 0);

// ---------- the encounter order (pure; presentation only, this session only) ----------
var encB = new Level3TableStaging.Encounter();
var liveB = new List<int> { LiveTargets(encB) };
Check(encB.Step == EWaiting && !encB.IntroHeard && encB.Featured == null && encB.SeatOpen && !encB.Done && !encB.EnvelopeOpen && !encB.CardShown,
    "buildup: before the handover Bea still has her envelope; only the seat target (it says to talk to her first) is live");
encB.Sync(handedOver: false);
Check(encB.Step == EWaiting && !encB.Open() && !encB.Offer() && !encB.Accept() && !encB.Present(null) && !encB.Present("") && encB.Step == EWaiting,
    "buildup: nothing opens, offers or seats before a saved handover; a presentation needs its featured record");
encB.HeardIntroduction();
Check(encB.IntroHeard && encB.Step == EWaiting,
    "buildup: a completed introduction whose save failed keeps her envelope (Waiting); the retry plays the envelope line only");
string featuredB = Level3TableStaging.FeaturedSource(rows4r, nine4r);
Check(encB.Present(featuredB) && encB.Step == EPresented && encB.Featured == "B-REV" && !encB.SeatOpen && !encB.EnvelopeOpen && !encB.Present("B-EXP") && encB.Featured == "B-REV",
    "buildup: the saved handover presents the closed envelope once, with her logo-work original (R1, B-REV) to come forward; the seat is off");
liveB.Add(LiveTargets(encB));
Check(encB.Open() && encB.Step == EOpened && encB.EnvelopeOpen && encB.FeaturedForward && !encB.Open() && !encB.SeatOpen,
    "buildup: Open Bea's records reveals the stack once; the featured record waits forward of it (dialogue cannot open it)");
liveB.Add(LiveTargets(encB));
Check(!encB.Returned("B-REV") && encB.Step == EOpened, "buildup: a record gathered before it was ever shown (an interruption mid-lift) is not counted as read");
encB.Shown("B-EXP");
Check(!encB.Returned("B-EXP") && !encB.Returned("B-REV") && encB.Step == EOpened, "buildup: only the featured record counts; no tour through every document");
encB.Shown("B-REV");
Check(!encB.Accept() && encB.Returned("B-REV") && encB.Step == EInspected && !encB.Returned("B-REV") && !encB.FeaturedForward && !encB.SeatOpen && !encB.CardShown,
    "buildup: reading her record and putting it back makes her organizer offer due; still no seat before she offers it");
liveB.Add(LiveTargets(encB));
Check(encB.Offer() && encB.Step == EOffered && encB.Done && encB.SeatOpen && !encB.CardShown && !encB.Offer(),
    "buildup: her offer (Help Bea / Later) opens the seat; Later leaves it Offered: nothing saved, no Trust or other penalty");
liveB.Add(LiveTargets(encB));
encB.Sync(handedOver: true);
Check(encB.Step == EOffered && encB.IntroHeard, "buildup: re-entering the area keeps this session's step (no repeated introduction or presentation)");
Check(encB.Accept() && encB.Step == EReady && encB.CardShown && encB.Accept() && !encB.Present("B-REV") && !encB.Open() && !encB.Offer() && encB.Step == EReady,
    "buildup: Help Bea (or the seat after Later) is the deliberate entry; later table visits sit straight down and nothing replays");
liveB.Add(LiveTargets(encB));
Check(liveB.All(n => n <= 1) && liveB.SequenceEqual(new[] { 1, 1, 1, 0, 1, 1 }),
    "buildup: exactly one table action at a time (seat / Open / Inspect), none while her offer is due");

// ---------- restore, legacy and progressed chapters: a concise continuation ----------
Level3ChapterCheckpoint fixtureHandedB = Level3EditorFixtures.BeaEncounter(bea, handedOver: true), fixtureWaitingB = Level3EditorFixtures.BeaEncounter(bea, handedOver: false);
foreach (var (what, state) in new[] { ("a reload right after the handover", handed4r), ("a restored draft with filed rows", partial4r),
    ("a legacy/progressed collected worksheet", office), ("the Bea Table Restored fixture", fixtureHandedB) })
{
    var restoredB = new Level3TableStaging.Encounter();
    restoredB.Sync(Level3TableStaging.TableSources(state.Case(B7).acquiredSourceIds).Count > 0);
    Check(restoredB.Step == EReady && restoredB.Done && restoredB.SeatOpen && restoredB.EnvelopeOpen && restoredB.CardShown && !restoredB.Present("B-REV") && !restoredB.Open() && !restoredB.Offer(),
        $"buildup restore ({what}): straight to the seat with the stack out; no presentation, reveal, offer or acquisition replays");
}
var waitingB = new Level3TableStaging.Encounter();
waitingB.Sync(Level3TableStaging.TableSources(fixtureWaitingB.Case(B7).acquiredSourceIds).Count > 0);
Check(fixtureWaitingB.phase == "BeaRecords" && fixtureWaitingB.Case(B7).admitted && fixtureWaitingB.Case(B7).acquiredSourceIds.Count == 0 && waitingB.Step == EWaiting &&
      fixtureHandedB.phase == "WorksheetDraft" && fixtureHandedB.Case(B7).acquiredSourceIds.SequenceEqual(nine4r) && Level3ChapterRules.Problem(fixtureHandedB) == null &&
      Level3EditorFixtures.Build(Level3EditorFixtures.BeaEncounterId, bea)?.phase == "BeaRecords" && Level3EditorFixtures.Build(Level3EditorFixtures.BeaTableRestoredId, bea)?.phase == "WorksheetDraft" &&
      Level3EditorFixtures.BeaEncounter(marine, false) == null && Level3EditorFixtures.BeaEncounter(null, true) == null,
    "buildup fixtures (Editor only, never saved): Bea Encounter starts assigned with her envelope; Bea Table Restored starts handed over");
string menuB = File.ReadAllText(Path.Combine(root, "Assets/Editor/Level3EditorFixtureMenu.cs"));
Check(menuB.Contains("[MenuItem(EncounterMenu)]") && menuB.Contains("[MenuItem(RestoredMenu)]") && menuB.Contains("Level3EditorFixtures.BeaEncounterId") &&
      menuB.Contains("Level3EditorFixtures.BeaTableRestoredId"),
    "buildup fixtures are chosen from the existing Tools > TaxVenture > Level 3 menu (T16: the restored table is also the machine practice start)");

// ---------- acquisition stays the one existing command; the buildup saves, grades, rewards and starts nothing ----------
Level3ChapterCheckpoint handedB = fresh2.Copy();
Check(Level3ChapterRules.CollectBeaRecords(handedB, out _) == Level3CommandOutcome.Applied, "buildup: the handover is still CollectBeaRecords from BeaRecords");
string handedSnapB = Snap(handedB);
var walkB = new Level3TableStaging.Encounter();
walkB.Present(Level3TableStaging.FeaturedSource(rows4r, Level3TableStaging.TableSources(handedB.Case(B7).acquiredSourceIds)));
walkB.Open(); walkB.Shown(walkB.Featured); walkB.Returned(walkB.Featured); walkB.Offer(); walkB.Accept();
Check(walkB.Step == EReady && Snap(handedB) == handedSnapB && handedB.Case(B7).acquiredSourceIds.SequenceEqual(nine4r) && !handedB.Case(B7).acquiredSourceIds.Contains("B-WHT") &&
      handedB.worksheet.strips.Count == 0 && handedB.worksheet.revision == 0 && !handedB.worksheetCheck.firstChecked &&
      handedB.Case(B7).clock?.started != true && !Level3ReviewRules.ClockDue(handedB) && !Level3ChapterRules.BeaWorkIndependent(handedB) &&
      Level3ChapterRules.CollectBeaRecords(handedB, out _) == Level3CommandOutcome.Duplicate && Snap(handedB) == handedSnapB,
    "buildup: opening, inspecting, her offer and Help Bea acquire nothing more, file or grade nothing and leave her review clock unstarted");
// HandOver's switch over the signed store (disposable, under the project's Temp): only Attached presents the envelope.
string sandboxB = Path.Combine(root, "Temp", "L3Repair", "T04B-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandboxB);
try
{
    string EncodeB(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var storeB = new SaveFileStore(Path.Combine(sandboxB, "progress.json"));
    PlayerProgressData pubB = Level3ChapterRules.WithCheckpoint(Clone(live), fresh2);
    storeB.Write(EncodeB(pubB));
    var talkB = new Level3TableStaging.Encounter();
    string HandB()
    {
        talkB.HeardIntroduction();
        Level3ChapterCheckpoint candidate = pubB.campaign.levelThree.Copy();
        Level3CommandOutcome outcome = Level3ChapterRules.CollectBeaRecords(candidate, out _);
        if (outcome == Level3CommandOutcome.Duplicate) { talkB.Sync(true); return "AlreadyAttached"; }
        if (outcome != Level3CommandOutcome.Applied) return "Refused";
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pubB), candidate);
        try { next.Normalize(); storeB.Write(EncodeB(next)); }
        catch (Exception) { return "SaveFailed"; }
        pubB = next;
        talkB.Present(Level3TableStaging.FeaturedSource(rows4r, Level3TableStaging.TableSources(candidate.Case(B7).acquiredSourceIds)));
        return "Attached";
    }
    byte[] diskB = File.ReadAllBytes(storeB.FilePath);
    Directory.CreateDirectory(storeB.TempPath); // injected failure
    Check(HandB() == "SaveFailed" && talkB.Step == EWaiting && talkB.IntroHeard && File.ReadAllBytes(storeB.FilePath).SequenceEqual(diskB) &&
          pubB.campaign.levelThree.phase == "BeaRecords" && pubB.campaign.levelThree.Case(B7).acquiredSourceIds.Count == 0,
        "buildup failed save: nothing is published or presented (her envelope stays with her); the retry is the short envelope line");
    Directory.Delete(storeB.TempPath);
    Check(HandB() == "Attached" && talkB.Step == EPresented && pubB.campaign.levelThree.Case(B7).acquiredSourceIds.SequenceEqual(nine4r) &&
          HandB() == "AlreadyAttached" && talkB.Step == EPresented && pubB.campaign.levelThree.Case(B7).acquiredSourceIds.SequenceEqual(nine4r) &&
          pubB.campaign.coins == live.campaign.coins && pubB.campaign.publicTrust == live.campaign.publicTrust,
        "buildup retry: saved first, then presented once; a repeat acquires nothing and replays no presentation; no coins or Trust");
}
finally
{
    try { Directory.Delete(sandboxB, true); } catch (IOException) { }
}
string tableB = Src("Scripts/Level3/Level3BeaTable.cs"), beaB = Src("Scripts/Level3/Level3BeaInteractable.cs");
string stagingB = Src("Scripts/Level3/Level3TableStaging.cs"), seatB = Src("Scripts/Level3/Level3BeaTableSeat.cs");
int offerB = Index(beaB, "// T04 BUILDUP: HER ORGANIZER");
string offerBodyB = beaB.Substring(offerB, Index(beaB, "// QUESTIONS") - offerB);
string[] forbiddenB = { "CommitLevelThree", "CommitWorksheet", "CommitBeaRecords(", "CommitComputation(", "CheckWorksheet(", "CollectWorksheet(", "StartClock(",
    "Level3WorksheetRules.Place(", "Level3WorksheetRules.Extract(", "Level3WorksheetRules.SetReason(", "Level3WorksheetCheck.Check(", "Level3WorksheetCheck.Acquire(",
    "AddSideQuest", ".coins", "publicTrust", "InventoryRules." };
// T05 pin: the table's paper-machine section holds the lever's one filing; the buildup code around it still saves nothing.
int machineB = Index(tableB, "// T05: THE PAPER MACHINE");
string tableOutsideMachineB = string.Join("\n", (tableB.Substring(0, machineB) + tableB.Substring(Index(tableB, "private void CancelDrags()")))
    .Split('\n').Where(line => !line.TrimStart().StartsWith("//")));
foreach (var (what, src) in new[] { ("table", tableOutsideMachineB), ("staging", stagingB), ("table targets", seatB), ("Bea's offer", offerBodyB) })
    Check(!forbiddenB.Any(token => src.Replace("CommitComputation(Level3TableStaging.AcknowledgeIntro,", "AcknowledgeIntro(").Contains(token)),
        $"buildup ({what}): only the saved splash acknowledgment is allowed; no filing, grading, rewards, inventory or clock command");
int handB = Index(beaB, "private void HandOver()");
string handBodyB = beaB.Substring(handB, Index(beaB, "// T04 BUILDUP: HER ORGANIZER") - handB);
string attachedB = handBodyB.Substring(Index(handBodyB, "case Level3Checkpointer.TapeCommit.Attached:"), Index(handBodyB, "case Level3Checkpointer.TapeCommit.AlreadyAttached:") - Index(handBodyB, "case Level3Checkpointer.TapeCommit.Attached:"));
Check(Index(handBodyB, "table.HeardIntroduction();") < Index(handBodyB, "owner.CommitBeaRecords(") && attachedB.Contains("table.Present();") && !attachedB.Contains("Level3Say") &&
      beaB.Contains("bea.introduction.GetRange(bea.introduction.Count - 1, 1)"),
    "buildup: only a saved handover presents the envelope (no Arthur line over Bea); a failed save retries with her envelope line, never a second acquisition path");

// ---------- the inspected record is the real original; layout keeps everything on her table ----------
List<ClientRecord> revB = Level3RecordRules.Build(bea, new[] { "B-REV" });
string revTextB = JsonSerializer.Serialize(revB.FirstOrDefault(), json);
Check(revB.Count == 1 && revB[0].source == "B-REV" && revTextB.Contains("500,000") && revTextB.Contains("REV-B26") &&
      Level3TableStaging.FeaturedSource(rows4r, nine4r.Where(x => x != "B-REV").ToList()) == "B-EXP" && Level3TableStaging.FeaturedSource(rows4r, new List<string>()) == null &&
      Level3TableStaging.FeaturedSource(null, new List<string> { "B-ID" }) == "B-ID" && Level3TableStaging.TableSources(nine4r.Concat(new[] { "B-WHT" })).Last() != "B-WHT",
    "buildup: the inspected record is her existing logo-work original (canonical amount and reference), read in the existing viewer; never B-WHT");
foreach (var matB in new[] { new UnityEngine.Vector2(1.2f, 0.72f), new UnityEngine.Vector2(1.0f, 0.62f), new UnityEngine.Vector2(0.9f, 0.55f), new UnityEngine.Vector2(0.82f, 0.5f), new UnityEngine.Vector2(0.7f, 0.45f), new UnityEngine.Vector2(0.5f, 0.32f) })
{
    float halfDepth = matB.y / 0.78f * 0.5f;
    var lay = Level3TableStaging.Compute(matB, Level3TableStaging.SeatOf(matB, halfDepth));
    UnityEngine.Vector2 withBea = Level3TableStaging.EnvelopeWithBea(lay, halfDepth, out float beaYaw);
    var beaRect = new UnityEngine.Rect(withBea.x - lay.Envelope.width * 0.5f, withBea.y - lay.Envelope.height * 0.5f, lay.Envelope.width, lay.Envelope.height);
    UnityEngine.Rect beaPrint = Level3TableStaging.Footprint(beaRect, beaYaw);
    float slide = (withBea - lay.Envelope.center).magnitude;
    Check(Math.Abs(beaYaw - lay.EnvelopeYaw - Level3TableStaging.BeaTurn) < 0.001f && beaPrint.yMax <= halfDepth - 0.01f + 0.0001f && withBea.x > lay.Envelope.center.x &&
          withBea.y >= lay.Envelope.center.y && slide > 0f && slide <= 0.08f * lay.Scale + 0.0001f &&
          !beaPrint.Overlaps(Level3TableStaging.Footprint(lay.Column, lay.MachineYaw)) && !beaPrint.Overlaps(Level3TableStaging.Footprint(lay.Stand, lay.StandYaw)),
        $"buildup ({matB.x:0.00} m mat): before presenting, her envelope lies on her side within the table, clear of stand and machine; the slide is short");
    UnityEngine.Rect sketch = Level3TableStaging.SketchArea(lay);
    UnityEngine.Rect sketchPrint = Level3TableStaging.Footprint(sketch, 8f);
    Check(sketch.width == 0f || (Level3TableStaging.Inside(sketchPrint, matB, Level3TableStaging.EdgeMargin) && lay.Footprints.All(r => !r.Overlaps(sketchPrint)) && sketch.center.y > 0f),
        $"buildup ({matB.x:0.00} m mat): her sketch lies on her side of the mat behind the machine, apart from every staged part (or is left out)");
}
Check(Level3TableStaging.SketchArea(Level3TableStaging.Compute(new UnityEngine.Vector2(1.0f, 0.62f), Level3TableStaging.SeatOf(new UnityEngine.Vector2(1.0f, 0.62f), 0.4f))).width > 0f,
    "buildup: a full-size mat has room for her sketch beside her records");

// ---------- copy: short turns, short prompts, no answers ----------
Level3ClientContent beaB2 = town4r.bea;
List<string> turnsB = new List<string>();
foreach (var line in beaB2.introduction.Concat(beaB2.organizerIntro))
{
    if (line.speaker == "Npc" && turnsB.Count > 0 && turnsB[turnsB.Count - 1].StartsWith("Npc:")) turnsB[turnsB.Count - 1] += " " + line.text;
    else turnsB.Add((line.speaker == "Npc" ? "Npc:" : "Player:") + line.text);
}
int WordsB(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
string openPromptB = "Open " + beaB2.speakerName + "'s records";
string[] leaksB = { "Income", "Deduct", "Leave Out", "Personal purpose", "Already represented", "duplicate", "personal", "coins", "Trust", "badge", "reward" };
string copyB = string.Join(" ", beaB2.introduction.Concat(beaB2.organizerIntro).Select(l => l.text)) + " " + beaB2.continuePrompt + " " + beaB2.helpChoice + " " + beaB2.laterChoice + " " + Level3BeaTable.InspectPrompt;
Check(turnsB.All(t => WordsB(t.Substring(t.IndexOf(':') + 1)) <= 30) && beaB2.introduction.Count(l => l.speaker == "Npc") <= 3 && beaB2.organizerIntro.Count == 1 &&
      WordsB(beaB2.continuePrompt) <= 14 && new[] { openPromptB, Level3BeaTable.InspectPrompt, beaB2.helpChoice, beaB2.laterChoice }.All(p => WordsB(p) <= 10) &&
      beaB2.helpChoice == "Help Bea" && beaB2.laterChoice == "Later" && !leaksB.Any(copyB.Contains),
    "buildup copy: turns of 30 words or fewer, prompts of 10 or fewer (Open Bea's records / Inspect a record / Help Bea / Later), no treatment, reason or reward");
var brokenB = JsonSerializer.Deserialize<Level3TownFile>(File.ReadAllText(Res("Levels/Level_03/town_life.json")), json);
brokenB.bea.organizerIntro.Clear();
brokenB.bea.helpChoice = " ";
Check(Level3TownContent.Validate(brokenB).Count >= 2, "buildup copy: a town file without the organizer line or Help Bea is reported (the built-in Bea is used)");

// ---------- guide, flags and the T05 entry seam ----------
GuideScript guideB = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var recordsB = guideB.lessons.Single(l => l.id == Level3ChapterRules.BeaRecordsLesson);
var tableIntroB = guideB.lessons.Single(l => l.id == Level3ChapterRules.WorksheetLesson);
string guideTextB = string.Join(" ", guideB.lessons.SelectMany(l => l.pages).Select(pg => pg.text));
Check(guideB.Validate() == "" && recordsB.when == "flag:beaIntroDone" && recordsB.onlyIn == "mode:FirstPerson" && recordsB.delay >= 1f && recordsB.pages.All(pg => pg.IsHint) &&
      !guideB.lessons.Any(l => l.when == "flag:beaRecordsCollected") && !guideTextB.Contains(openPromptB) && !guideTextB.Contains(Level3BeaTable.InspectPrompt) &&
      Src("Scripts/Level3/Level3Checkpointer.cs").Contains("case \"beaIntroDone\": return Level3BeaTable.Instance != null && Level3BeaTable.Instance.IntroDone;"),
    "buildup guide: Arthur is silent from the handover to her offer (the envelope/record carry their own prompts) and never repeats them");
Check(tableIntroB.when == "flag:atBeaTable" && string.IsNullOrEmpty(tableIntroB.after) && tableIntroB.onlyIn == "mode:Workstation" &&
      Level3ChapterRules.ImpliedLessons(handed4r).Contains(Level3ChapterRules.BeaRecordsLesson) && !Level3ChapterRules.ImpliedLessons(handed4r).Contains(Level3ChapterRules.WorksheetLesson),
    "buildup -> T05 seam: the seated lesson starts at the first seat (no other prerequisite); a resumed chapter never replays the records hint");

// ---------- wiring: one owner per input, interruptions, the seat path ----------
int updateB = Index(tableB, "private void UpdateEncounter(");
string updateBodyB = tableB.Substring(updateB, Index(tableB, "private bool CanOfferNow()") - updateB);
Check(updateBodyB.Contains("int seatLayer = encounter.SeatOpen ? InteractLayer : 0;") &&
      updateBodyB.Contains("if (tableCollider != null && tableCollider.gameObject.layer != seatLayer) tableCollider.gameObject.layer = seatLayer;") &&
      updateBodyB.Contains("envelopeTarget.Active = encounter.Step == Level3TableStaging.EncounterStep.Presented && !moving;") &&
      updateBodyB.Contains("recordTarget.Active = encounter.FeaturedForward && !moving && !ReadingStanding && LiesForward(encounter.Featured);") &&
      !updateBodyB.Contains("MobileInputGuard.TryGetWorldTap(out _)"),
    "buildup wiring: the live table target follows the encounter (never during a move); a tap off the standing reader puts the record back");
int inspectB = Index(tableB, "private void InspectFeatured()");
string inspectBodyB = tableB.Substring(inspectB, Index(tableB, "private static void SetPaperTouch(") - inspectB);
int gatherB = Index(tableB, "private void GatherAll()");
string gatherBodyB = tableB.Substring(gatherB, Index(tableB, "public static void Level3Say") - gatherB);
int landedB = Index(tableB, "public void PaperLanded(");
string landedBodyB = tableB.Substring(landedB, Index(tableB, "public bool WantsReturn(") - landedB);
Check(Index(inspectBodyB, "HudWindowLock.Acquire(this);") < Index(inspectBodyB, "paper.TakeOut();") && inspectBodyB.Contains("if (!HudWindowLock.IsHeld)") &&
      inspectBodyB.Contains("HudWindowLock.Release(this);") && inspectBodyB.Split("HudWindowLock.Acquire(").Length == 2 &&
      gatherBodyB.Contains("EndStandingRead();") && gatherBodyB.Contains("SettleMotion();") && landedBodyB.Contains("if (ReadingStanding && source == encounter.Featured) EndStandingRead();") &&
      landedBodyB.Contains("if (encounter.Returned(source)) offerAt = Time.unscaledTime + OfferDelay;") && Body6(tableB, "private void OnDisable()", "private void OnDestroy()").Contains("GatherAll();") &&
      Body6(tableB, "private void OnDisable()", "private void OnDestroy()").Contains("CancelIntro();"),
    "buildup wiring: the standing read takes the existing HudWindowLock once and releases it when the record lands, or on leave/disable (no orphan paper or stuck hold)");
Check(tableB.Contains("CameraController.Instance.CanStartDialogue && !DialogueUI.IsAnyDialogueActive") && tableB.Contains("!HudWindowLock.IsHeld && !InGameSettings.IsOpen && !HudRewardScreen.AnyOpen") &&
      tableB.Contains("if (!encounter.Done || ReadingStanding || introSplash != null || SideQuestSplash.IsShowing) return false;") && tableB.Contains("presentCard = encounter.Step != Level3TableStaging.EncounterStep.Ready;") &&
      tableB.Contains("if (presentCard && Seated && !introPending && CameraController.Instance != null && !CameraController.Instance.IsTransitioning)") &&
      tableB.Contains("bool working = papersShown && encounter.CardShown && !presentCard"),
    "buildup wiring: her offer starts only when no reader, camera move, settings, card or conversation holds the player; the stand's card arrives after the seat camera settles");
Check(beaB.Contains("if (!Talk(lines, Concluded)) return false;") && beaB.Contains("helpChosen = true;") && beaB.Contains("if (dialogue == null || dialogue.ContinueWith(awaited, null) == -2)") &&
      beaB.Contains("private void Concluded()\n    {\n        if (!helpChosen) return;\n        helpChosen = false;\n        table.AcceptHelp(") &&
      beaB.Contains("talkLater: false") && beaB.Contains("case OfferAction: ShowOffer(); break;") && beaB.Contains("if (offerHelp && table.CanOfferHelp)") &&
      tableB.Contains("public bool AcceptHelp(Pose? fromView = null) => encounter.Done && TrySit(fromView);"),
    "buildup wiring: Help Bea ends the conversation first, then the existing seat path runs once (dialogue, camera and table input never overlap)");
Check(tableB.Split("AddComponent<NpcAnimationDriver>").Length == 2 && tableB.Split("AddComponent<NpcLife>").Length == 2 &&
      tableB.Contains("WorldEmote.Show(Bea.transform, Emotion.Thinking, 2.5f);") && tableB.Contains("noticed = true;") &&
      !tableB.Contains("RuntimeAnimatorController") && !tableB.Contains("animator.Play") && !tableB.Contains("CrossFade"),
    "buildup: Bea keeps one NpcLife/animation owner; her uncertainty is one existing thinking emote, once; no new clip or Animator owner");
Check(!tableB.Contains("tableCollider.enabled") && tableB.Contains("Level3TablePaperRaycaster.HitRect(camera.ScreenPointToRay(screen), plane,") &&
      Src("Scripts/Player/Interactor.cs").Contains("Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer)"),
    "buildup: the seat box stays solid and still catches a dropped reader; only its Interactable layer moves, so the first-person ray reaches the envelope/record");

Console.WriteLine($"T04 buildup checks: {checks - t04bStart}, failures {fails - t04bFails} (encounter order, restores/legacy, no-commit/no-clock, record/layout, copy, guide/T05 seam, wiring; Unity rendering, NPC motion, touch and audio not run).");

// =====================================================================
// Revised T05 (4 November 2026): feed, select, Why?, lever-file and recall on the paper machine
// =====================================================================
int t05mStart = checks, t05mFails = fails;
List<Level3WorksheetRow> rows5m = Level3WorksheetRules.RowsOf(bea);
Level3ChapterCheckpoint base5m = freshHanded.Copy();
base5m.worksheet = new Level3WorksheetDraft();
Level3CommandOutcome File5m(Level3ChapterCheckpoint c, string row, string treatment, string reason = "", int? basedOn = null) =>
    Level3WorksheetRules.File(c, rows5m, new Level3Filing { RowId = row, Treatment = treatment, Reason = reason ?? "", BasedOn = basedOn ?? c.worksheet.revision }, out _);
Level3ChapterCheckpoint Accepted5m(Level3ChapterCheckpoint c, params string[] batches) { var a = c.Copy(); a.worksheetCheck.acceptedBatches.AddRange(batches); return a; }

// ---------- pass 1a: the lever's one filing: atomic, neutral, exact, open batch only ----------
Check(Level3WorksheetRules.OpenBatch(base5m) == "earnings" && Level3WorksheetRules.RowOpen(base5m, "R1") && !Level3WorksheetRules.RowOpen(base5m, "R3") &&
      Level3WorksheetRules.Treatments.SequenceEqual(new[] { "income", "deduct", "leave-out" }) &&
      Level3WorksheetRules.Treatments.Select(Level3WorksheetRules.TreatmentLabel).SequenceEqual(Level3PaperMachineModel.DialLabels),
    "T05: a fresh draft opens the earnings batch only; the selector's three detents are Income / Deduct / Leave Out in a fixed order");
foreach (string t5m in Level3WorksheetRules.Treatments)
foreach (string r5m in new[] { "R1", "R2" })
{
    Level3ChapterCheckpoint any5m = base5m.Copy();
    Level3WorksheetRow row5m = rows5m.Single(r => r.RowId == r5m);
    Check(File5m(any5m, r5m, t5m) == Level3CommandOutcome.Applied && Level3WorksheetRules.SavedTreatment(any5m.worksheet, r5m) == t5m &&
          any5m.worksheet.strips.Count == 1 && any5m.worksheet.Strip(r5m).sourceId == row5m.SourceId && any5m.worksheet.Strip(r5m).amount == row5m.Amount &&
          any5m.worksheet.revision == 1 && any5m.worksheetCheck.firstChecked == false && any5m.worksheetCheck.checks == 0,
        $"T05: {r5m} filed as {t5m} is accepted neutrally (one slip, its own source and exact printed amount; nothing checked or graded)");
}
foreach (string t5m in Level3WorksheetRules.Treatments)
foreach (string r5m in new[] { "R6", "R7" })
{
    Level3ChapterCheckpoint q5m = Accepted5m(base5m, "earnings", "costs");
    Check(File5m(q5m, r5m, t5m, "") == Level3CommandOutcome.Refused && q5m.worksheet.strips.Count == 0 && q5m.worksheet.revision == 0,
        $"T05: {r5m} as {t5m} without a Why? reason is refused whole (no half classification is saved)");
    foreach (string why5m in Level3WorksheetRules.Reasons)
    {
        Level3ChapterCheckpoint qq5m = q5m.Copy();
        string note5m = Level3WorksheetRules.NoteForRow(r5m);
        Check(File5m(qq5m, r5m, t5m, why5m) == Level3CommandOutcome.Applied && Level3WorksheetRules.SavedTreatment(qq5m.worksheet, r5m) == t5m &&
              qq5m.worksheet.Note(note5m)?.reason == why5m && qq5m.worksheet.revision == 1 && Level3WorksheetRules.RowFiled(qq5m.worksheet, r5m),
            $"T05: {r5m} as {t5m} with '{why5m}' saves treatment and reason together in one edit (Why? is asked for every treatment)");
    }
}
Level3ChapterCheckpoint reason5m = base5m.Copy();
Check(File5m(reason5m, "R1", "income", "business-purpose") == Level3CommandOutcome.Refused && reason5m.worksheet.revision == 0 &&
      !Level3WorksheetRules.NeedsReason("R1") && !Level3WorksheetRules.NeedsReason("R5") && Level3WorksheetRules.NeedsReason("R6") && Level3WorksheetRules.NeedsReason("R7"),
    "T05: R1-R5 take no reason (a reason on them is refused); only R6/R7 ask Why?");
foreach (var (what5m, row5m, t5m) in new[] { ("an unknown row", "R8", "income"), ("a support row", "B-RENT-PROOF", "deduct"), ("the certificate's row", "B-WHT-CREDIT", "income"),
                                            ("no row", (string)null, "income"), ("an unknown treatment", "R1", "expenses"), ("no treatment", "R1", "") })
{
    Level3ChapterCheckpoint bad5m = base5m.Copy();
    string before5m = Snap(bad5m);
    Check(Level3WorksheetRules.File(bad5m, rows5m, new Level3Filing { RowId = row5m, Treatment = t5m, BasedOn = 0 }, out string why5m) == Level3CommandOutcome.Refused &&
          Snap(bad5m) == before5m && !string.IsNullOrEmpty(why5m), $"T05: {what5m} is refused and changes nothing");
}
Level3ChapterCheckpoint unacquired5m = base5m.Copy();
unacquired5m.Case(B7).acquiredSourceIds.Remove("B-REV");
Check(File5m(unacquired5m, "R1", "income") == Level3CommandOutcome.Refused && unacquired5m.worksheet.strips.Count == 0,
    "T05: a row whose original was not handed over cannot be filed");
foreach (var (what5m, state5m) in new[] { ("before the handover", fresh2.Copy()), ("at reception", entry.Copy()) })
    Check(File5m(state5m, "R1", "income") == Level3CommandOutcome.Refused, $"T05: nothing can be filed {what5m}");
Level3ChapterCheckpoint future5m = base5m.Copy();
Check(File5m(future5m, "R3", "deduct") == Level3CommandOutcome.Refused && File5m(future5m, "R6", "leave-out", "personal-purpose") == Level3CommandOutcome.Refused &&
      future5m.worksheet.revision == 0, "T05: a later batch cannot be filed early (no future-batch peek or path)");
Level3ChapterCheckpoint locked5m = Accepted5m(base5m.Copy(), "earnings");
Check(File5m(locked5m, "R1", "deduct") == Level3CommandOutcome.Refused && File5m(locked5m, "R3", "deduct") == Level3CommandOutcome.Applied &&
      Level3WorksheetRules.OpenBatch(locked5m) == "costs" && Level3WorksheetRules.BatchAccepted(locked5m, "earnings"),
    "T05: an accepted batch is read-only (no grading edits) and the next batch opens");
Level3ChapterCheckpoint ready5m = base5m.Copy();
ready5m.phase = "WorksheetReady";
Check(File5m(ready5m, "R1", "income") == Level3CommandOutcome.Refused && Level3WorksheetRules.OpenBatch(ready5m) == null &&
      Level3WorksheetRules.Batches.All(b => Level3WorksheetRules.BatchAccepted(ready5m, b)),
    "T05: a ready (or collected) worksheet accepts no filing; every batch reads as accepted");

// ---------- pass 1b: refile replaces, duplicates and stale pulls write nothing ----------
Level3ChapterCheckpoint refile5m = base5m.Copy();
File5m(refile5m, "R1", "income");
string afterFirst5m = Snap(refile5m);
Check(File5m(refile5m, "R1", "income") == Level3CommandOutcome.Duplicate && Snap(refile5m) == afterFirst5m,
    "T05: a double pull (or an unchanged refile) is a Duplicate: no revision, no write, no second slip");
Check(File5m(refile5m, "R1", "deduct", basedOn: 0) == Level3CommandOutcome.Refused && Snap(refile5m) == afterFirst5m,
    "T05: a stale pull (made on an older draft revision) is refused and changes nothing");
Check(File5m(refile5m, "R1", "leave-out") == Level3CommandOutcome.Applied && refile5m.worksheet.strips.Count(x => x.rowId == "R1") == 1 &&
      Level3WorksheetRules.SavedTreatment(refile5m.worksheet, "R1") == "leave-out" && refile5m.worksheet.Strip("R1").section == "not-deductible" && refile5m.worksheet.revision == 2,
    "T05: recall and refile replaces the same slip (never a duplicate row)");
Level3ChapterCheckpoint legacyLeave5m = base5m.Copy();
legacyLeave5m.worksheet.strips.Add(new Level3StripRecord { rowId = "R2", sourceId = "B-REV", amount = rows5m.Single(r => r.RowId == "R2").Amount, section = "supporting" });
Check(File5m(legacyLeave5m, "R2", "leave-out") == Level3CommandOutcome.Duplicate && legacyLeave5m.worksheet.Strip("R2").section == "supporting" &&
      Level3WorksheetRules.SectionFor("leave-out", "supporting") == "supporting" && Level3WorksheetRules.SectionFor("leave-out", "") == "not-deductible" &&
      Level3WorksheetRules.SectionFor("deduct", "supporting") == "expenses",
    "T05: a legacy Leave Out section is kept losslessly (refiling Leave Out writes nothing; other treatments map to their sections)");
Level3ChapterCheckpoint tampered5m = base5m.Copy();
tampered5m.worksheet.strips.Add(new Level3StripRecord { rowId = "R1", sourceId = "B-EXP", amount = "1.00", section = "" });
Check(File5m(tampered5m, "R1", "income") == Level3CommandOutcome.Refused && tampered5m.worksheet.Strip("R1").section == "",
    "T05: a saved slip whose provenance does not match its original is never overwritten (source/amount stay with the row)");
Level3ChapterCheckpoint link5m = Accepted5m(base5m, "earnings", "costs");
link5m.worksheet.notes.Add(new Level3NoteRecord { noteId = "note-R7", reason = "", related = "R5" });
Check(File5m(link5m, "R7", "deduct", "need-support") == Level3CommandOutcome.Applied && link5m.worksheet.Note("note-R7")?.related == "R5" &&
      link5m.worksheet.Note("note-R7")?.reason == "need-support",
    "T05: a legacy note's link is kept when the machine writes its reason (no automatic link is ever written)");

// ---------- pass 1c: totals follow the published choices exactly, wrong ones included ----------
Level3ChapterCheckpoint tot5m = base5m.Copy();
Level3WorksheetRules.DraftTotals(tot5m, out decimal inc0, out decimal ded0);
File5m(tot5m, "R1", "deduct");
File5m(tot5m, "R2", "income");
Level3WorksheetRules.DraftTotals(tot5m, out decimal inc1, out decimal ded1);
Check(inc0 == 0m && ded0 == 0m && inc1 == 400000.00m && ded1 == 500000.00m,
    "T05: the windows sum the saved slips' exact amounts by their chosen treatment, a wrong choice included (no hidden correction)");
Level3ChapterCheckpoint all5m = base5m.Copy();
File5m(all5m, "R1", "income"); File5m(all5m, "R2", "income");
all5m = Accepted5m(all5m, "earnings");
File5m(all5m, "R3", "deduct"); File5m(all5m, "R4", "deduct"); File5m(all5m, "R5", "deduct");
all5m = Accepted5m(all5m, "costs");
Check(!all5m.worksheet.drafted && !Level3WorksheetRules.AllFiled(all5m.worksheet), "T05: not drafted until the questions are filed with their reasons");
File5m(all5m, "R6", "deduct", "business-purpose"); File5m(all5m, "R7", "deduct", "need-support");
Level3WorksheetRules.DraftTotals(all5m, out decimal incAll, out decimal dedAll);
Check(incAll == 900000.00m && dedAll == 354000.00m && all5m.worksheet.drafted && Level3WorksheetRules.AllFiled(all5m.worksheet) &&
      all5m.worksheet.strips.Count == 7 && all5m.worksheet.strips.Select(x => x.rowId).Distinct().Count() == 7,
    "T05: seven slips, nine choices: wrong deductions give the wrong draft total (354,000.00 = Bea's own draft), and the draft is marked filled-in once");
Level3ChapterCheckpoint hidden5m = base5m.Copy();
hidden5m.worksheet.strips.Add(new Level3StripRecord { rowId = "R3", sourceId = "B-EXP", amount = "180000.00", section = "expenses" });
Level3WorksheetRules.DraftTotals(hidden5m, out decimal incH, out decimal dedH);
Check(incH == 0m && dedH == 0m && Level3TableStaging.RackRows(hidden5m, rows5m).Count == 0 && Level3TableStaging.ActiveRow(hidden5m, rows5m)?.RowId == "R1",
    "T05: a legacy slip in a later batch is neither totalled nor tabbed nor shown until its batch opens");
Check(Level3WorksheetRules.ComparePartner(rows5m, "R7") == "B-EXP" && Level3WorksheetRules.ComparePartner(rows5m, "R5") == "B-PRINT" &&
      new[] { "R1", "R2", "R3", "R4", "R6" }.All(r => Level3WorksheetRules.ComparePartner(rows5m, r) == null),
    "T05: COMPARE is offered on both rows of the one shared transaction (PRINT-26), from the content, never on R7 alone");

// ---------- pass 1d: the machine's local flow (pure): neutral start, pending choices, one filing ----------
var flow5m = new Level3TableStaging.Machine();
Check(flow5m.Step == Level3TableStaging.MachineStep.Empty && flow5m.Present("R1") && flow5m.Step == Level3TableStaging.MachineStep.Ready &&
      !flow5m.BeginFile(out _) && flow5m.ToggleArm() && flow5m.Armed && flow5m.ToggleArm() && !flow5m.Armed,
    "T05 flow: a record on the stand can be armed (tap card) and nothing can be filed before it is fed");
Check(flow5m.Feed("", "", 4) && flow5m.Step == Level3TableStaging.MachineStep.Loaded && flow5m.Treatment == "" && flow5m.Reason == "" &&
      flow5m.Missing == Level3WorksheetRules.ChooseCategory && !flow5m.BeginFile(out _) && !flow5m.Present("R2"),
    "T05 flow: a new record loads neutral; the lever names the missing category and files nothing; nothing replaces a loaded record");
Check(flow5m.Stepped(+1) == "income" && flow5m.Stepped(-1) == "leave-out" && flow5m.Choose("deduct") && flow5m.Stepped(+1) == "leave-out" &&
      flow5m.Stepped(-1) == "income" && flow5m.Choose("leave-out") && flow5m.Stepped(+1) == "leave-out" && !flow5m.Choose("leave-out") && !flow5m.Choose("expenses"),
    "T05 flow: dial steps move one detent each way from neutral and stop at the ends; labels choose exactly; repeats and unknowns change nothing");
Check(!flow5m.ChooseReason("personal-purpose") && !flow5m.CanAskWhy && flow5m.Missing == null && flow5m.BeginFile(out Level3Filing f5m) &&
      f5m.RowId == "R1" && f5m.Treatment == "leave-out" && f5m.Reason == "" && f5m.BasedOn == 4 && flow5m.Step == Level3TableStaging.MachineStep.Saving &&
      flow5m.Busy && !flow5m.BeginFile(out _) && !flow5m.Choose("income") && !flow5m.Present("R2") && !flow5m.Recall("R2", "income", "", 4),
    "T05 flow: R1 asks no Why?; the lever produces exactly one filing; while saving every other action (second pull, dial, next, recall) waits");
flow5m.Saved(false);
Check(flow5m.Step == Level3TableStaging.MachineStep.Loaded && flow5m.Treatment == "leave-out" && flow5m.BeginFile(out Level3Filing retry5m) &&
      retry5m.Treatment == "leave-out" && retry5m.BasedOn == 4,
    "T05 flow: a failed save keeps the record half-fed with the same choices; the same pull can be retried");
flow5m.Saved(true);
Check(flow5m.Step == Level3TableStaging.MachineStep.Feeding && flow5m.Busy && !flow5m.Withdraw(out _) && flow5m.Fed() &&
      flow5m.Step == Level3TableStaging.MachineStep.Empty && flow5m.Row == null && !flow5m.Fed(),
    "T05 flow: after the saved filing the feed plays (no back-out); finishing it once empties the machine for the next record");
Check(flow5m.Present("R6") && flow5m.Feed("", "", 7) && flow5m.Missing == Level3WorksheetRules.ChooseCategory && !flow5m.CanAskWhy &&
      !flow5m.ChooseReason("personal-purpose") && flow5m.Choose("income") && flow5m.CanAskWhy && flow5m.Missing == Level3WorksheetRules.ChooseReason &&
      flow5m.ChooseReason("need-support") && flow5m.Choose("deduct") && flow5m.Reason == "need-support" && flow5m.Missing == null,
    "T05 flow: R6 asks Why? after any treatment (never before one); changing the treatment neither picks nor clears a reason");
Check(flow5m.Withdraw(out bool toStand5m) && toStand5m && flow5m.Step == Level3TableStaging.MachineStep.Ready && flow5m.Row == "R6" &&
      flow5m.Treatment == "" && flow5m.Reason == "" && flow5m.Feed("", "", 7) && flow5m.Treatment == "",
    "T05 flow: backing out a new record returns it to the stand with its pending choices dropped (it loads neutral again)");
flow5m.Reset();
Check(flow5m.Recall("R2", "deduct", "", 9) && flow5m.Recalled && flow5m.Treatment == "deduct" && flow5m.BasedOn == 9 &&
      flow5m.Withdraw(out bool recalledToStand5m) && !recalledToStand5m && flow5m.Step == Level3TableStaging.MachineStep.Empty &&
      !flow5m.Recall("R2", "", "", 9) && flow5m.Present("R1") && flow5m.Feed("income", "", 9) && flow5m.Treatment == "income" &&
      flow5m.Recall("R7", "leave-out", "already-represented", 9) == false,
    "T05 flow: a recalled record keeps its saved choices; cancelling feeds it back (its filing stands); an unfiled row cannot be recalled; a partly filed row loads its saved choice");
flow5m.Reset();
Check(flow5m.Recall("R7", "leave-out", "already-represented", 3) && flow5m.Reason == "already-represented" && flow5m.Missing == null &&
      flow5m.BeginFile(out Level3Filing r7f5m) && r7f5m.Reason == "already-represented",
    "T05 flow: a recalled R7 comes back with its saved reason and refiles with it unless changed");
flow5m.Saved(true);
flow5m.Reset();
Check(flow5m.Step == Level3TableStaging.MachineStep.Empty && !flow5m.Busy && flow5m.Present("R1"),
    "T05 flow: an interruption during the feed settles to empty (the filing was already saved; nothing is filed again)");

// The whole loop against the real command: neutral start, wrong choice, failed save, retry, double pull, recall and refile.
Level3ChapterCheckpoint loop5m = base5m.Copy();
var flowLoop5m = new Level3TableStaging.Machine();
int writes5m = 0;
bool failNext5m = false;
Level3Checkpointer.TapeCommit Commit5m(Level3Filing filing)
{
    Level3ChapterCheckpoint candidate = loop5m.Copy();
    Level3CommandOutcome outcome = Level3WorksheetRules.File(candidate, rows5m, filing, out _);
    if (outcome == Level3CommandOutcome.Duplicate) return Level3Checkpointer.TapeCommit.AlreadyAttached;
    if (outcome != Level3CommandOutcome.Applied) return Level3Checkpointer.TapeCommit.Refused;
    if (failNext5m) { failNext5m = false; return Level3Checkpointer.TapeCommit.SaveFailed; }
    writes5m++;
    loop5m = candidate;
    return Level3Checkpointer.TapeCommit.Attached;
}
bool Pull5m()
{
    if (!flowLoop5m.BeginFile(out Level3Filing filing)) return false;
    var result = Commit5m(filing);
    bool ok = result == Level3Checkpointer.TapeCommit.Attached || result == Level3Checkpointer.TapeCommit.AlreadyAttached;
    flowLoop5m.Saved(ok);
    return ok;
}
flowLoop5m.Present(Level3TableStaging.ActiveRow(loop5m, rows5m).RowId);
flowLoop5m.Feed(Level3WorksheetRules.SavedTreatment(loop5m.worksheet, "R1"), "", loop5m.worksheet.revision);
string beforeChoice5m = Snap(loop5m);
flowLoop5m.Choose("deduct");
Check(Snap(loop5m) == beforeChoice5m && !Level3ChapterRules.BeaWorkIndependent(loop5m) && !Level3ReviewRules.ClockDue(loop5m),
    "T05 loop: feeding and turning the selector change no saved state and do not start Bea's clock");
failNext5m = true;
Check(!Pull5m() && writes5m == 0 && Snap(loop5m) == beforeChoice5m && flowLoop5m.Treatment == "deduct" && flowLoop5m.Step == Level3TableStaging.MachineStep.Loaded,
    "T05 loop: a failed save writes nothing and keeps the half-fed record with its choice");
Check(Pull5m() && writes5m == 1 && !Pull5m() && writes5m == 1 && Level3WorksheetRules.SavedTreatment(loop5m.worksheet, "R1") == "deduct" &&
      Level3ChapterRules.BeaWorkIndependent(loop5m) && Level3ReviewRules.ClockDue(loop5m),
    "T05 loop: the retry files once (a second pull while feeding does nothing); the first saved filing starts Bea's independent time");
flowLoop5m.Fed();
Check(flowLoop5m.Present(Level3TableStaging.ActiveRow(loop5m, rows5m)?.RowId) && flowLoop5m.Row == "R2" &&
      flowLoop5m.Feed(Level3WorksheetRules.SavedTreatment(loop5m.worksheet, "R2"), "", loop5m.worksheet.revision) && flowLoop5m.Treatment == "",
    "T05 loop: the next record arrives and starts neutral (the previous choice is not carried over)");
flowLoop5m.Choose("income");
Pull5m();
flowLoop5m.Fed();
Check(Level3TableStaging.ActiveRow(loop5m, rows5m) == null && Level3WorksheetRules.BatchFiled(loop5m, "earnings") &&
      Level3TableStaging.RackRows(loop5m, rows5m).Select(r => r.RowId).SequenceEqual(new[] { "R1", "R2" }),
    "T05 loop: once the batch is filed no card is spawned (Check batch is T06's) and both rows have recall tabs");
Check(flowLoop5m.Recall("R1", Level3WorksheetRules.SavedTreatment(loop5m.worksheet, "R1"), "", loop5m.worksheet.revision) && flowLoop5m.Treatment == "deduct",
    "T05 loop: recalling R1 brings back its saved (wrong) choice");
string recalledSaved5m = Snap(loop5m);
flowLoop5m.Choose("income");
Check(Snap(loop5m) == recalledSaved5m && Level3WorksheetRules.SavedTreatment(loop5m.worksheet, "R1") == "deduct",
    "T05 loop: the old saved filing stands until its replacement saves");
Check(Pull5m() && loop5m.worksheet.strips.Count == 2 && Level3WorksheetRules.SavedTreatment(loop5m.worksheet, "R1") == "income" && writes5m == 3,
    "T05 loop: refiling replaces R1's slip (two slips in all, one write)");
flowLoop5m.Fed();
Check(flowLoop5m.Recall("R2", "income", "", loop5m.worksheet.revision) && Pull5m() && writes5m == 3,
    "T05 loop: an unchanged refile of a recalled record feeds back with no write");

// ---------- pass 1e: signed save round trip, failure injection, duplicates (disposable store under Temp) ----------
string sandbox5m = Path.Combine(root, "Temp", "L3Repair", "T05-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox5m);
try
{
    string Encode5m(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    PlayerProgressData Decode5m(string text)
    {
        using JsonDocument doc = JsonDocument.Parse(text);
        string payload = doc.RootElement.GetProperty("payload").GetString();
        return SaveFileStore.SignatureMatches(payload, doc.RootElement.GetProperty("signature").GetString()) ? JsonSerializer.Deserialize<PlayerProgressData>(payload, json) : null;
    }
    // Level3Checkpointer.CommitWorksheet(File): candidate copy -> Level3WorksheetRules.File -> signed write -> publish (mirrored on a disposable store).
    string Lever5m(SaveFileStore st, ref PlayerProgressData pub, Level3Filing filing)
    {
        Level3ChapterCheckpoint candidate = pub.campaign.levelThree.Copy();
        Level3CommandOutcome outcome = Level3WorksheetRules.File(candidate, rows5m, filing, out _);
        if (outcome == Level3CommandOutcome.Duplicate) return "AlreadyAttached";
        if (outcome != Level3CommandOutcome.Applied) return "Refused";
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub), candidate);
        try { next.Normalize(); st.Write(Encode5m(next)); }
        catch (Exception) { return "SaveFailed"; }
        pub = next;
        return "Attached";
    }
    var store5m = new SaveFileStore(Path.Combine(sandbox5m, "progress.json"));
    PlayerProgressData pub5m = Level3ChapterRules.WithCheckpoint(Clone(live), base5m);
    store5m.Write(Encode5m(pub5m));
    byte[] disk5m = File.ReadAllBytes(store5m.FilePath);
    string before5m = Snap(pub5m);
    var r6Filing5m = new Level3Filing { RowId = "R1", Treatment = "income", BasedOn = 0 };
    Directory.CreateDirectory(store5m.TempPath); // injected failure
    Check(Lever5m(store5m, ref pub5m, r6Filing5m) == "SaveFailed" && Snap(pub5m) == before5m && File.ReadAllBytes(store5m.FilePath).SequenceEqual(disk5m),
        "T05 save: a failed lever write publishes nothing (draft, clock, disk bytes unchanged)");
    Directory.Delete(store5m.TempPath);
    Check(Lever5m(store5m, ref pub5m, r6Filing5m) == "Attached" && Lever5m(store5m, ref pub5m, r6Filing5m) == "AlreadyAttached",
        "T05 save: the retry files once; the same pull again is a Duplicate (no second write)");
    byte[] once5m = File.ReadAllBytes(store5m.FilePath);
    Check(Lever5m(store5m, ref pub5m, new Level3Filing { RowId = "R1", Treatment = "deduct", BasedOn = 0 }) == "Refused" && File.ReadAllBytes(store5m.FilePath).SequenceEqual(once5m),
        "T05 save: a late pull made on an older revision is refused before any write");
    PlayerProgressData back5m = Decode5m(File.ReadAllText(store5m.FilePath));
    Check(back5m != null && !back5m.Normalize() && Snap(back5m.campaign.levelThree) == Snap(pub5m.campaign.levelThree) && WithoutChapter(back5m) == liveWithout &&
          back5m.campaign.coins == live.campaign.coins && back5m.campaign.publicTrust == live.campaign.publicTrust &&
          Level3ChapterRules.Decide(back5m.campaign.levelThree) == Level3EntryDecision.Resume,
        "T05 save: the filed draft round-trips through the signed save without repair; possessions, coins and trust unchanged; it resumes");
    Check(!new[] { "expected", "worksheetDecisions", "correct\"", "Personal purpose", "documentName" }.Any(Snap(back5m.campaign.levelThree).Contains),
        "T05 save: no answer key, label or content enters the saved draft");
    Level3ChapterCheckpoint restored5m = back5m.campaign.levelThree;
    Check(Level3TableStaging.ActiveRow(restored5m, rows5m)?.RowId == "R2" && Level3TableStaging.RackRows(restored5m, rows5m).Single().RowId == "R1" &&
          Level3ChapterRules.BeaWorkIndependent(restored5m),
        "T05 save: a quiet restore derives the next card (R2), R1's tab and the started-work boundary from the saved draft alone");
    Level3WorksheetResult acceptedSave5m = new Level3WorksheetResult { acceptedBatches = new List<string> { "earnings", " earnings", null, "" } };
    Check(acceptedSave5m.Repair() && acceptedSave5m.acceptedBatches.SequenceEqual(new[] { "earnings" }) &&
          !ReferenceEquals(acceptedSave5m.Copy().acceptedBatches, acceptedSave5m.acceptedBatches) && acceptedSave5m.Copy().acceptedBatches.SequenceEqual(acceptedSave5m.acceptedBatches),
        "T05 save: accepted batches (T06's seam) are repaired, deep-copied and absent from older saves (empty)");
    var older5m = JsonSerializer.Deserialize<Level3WorksheetResult>("{\"firstChecked\":false}", json);
    Check(older5m.acceptedBatches != null && older5m.acceptedBatches.Count == 0, "T05 save: a pre-T05 check record reads with no accepted batch");
}
finally
{
    try { Directory.Delete(sandbox5m, true); } catch (IOException) { }
}

// ---------- pass 1f: legacy drafts and frozen first checks continue under the machine ----------
Level3ChapterRules.TryContinueLegacy(LegacyOf(frCorrecting), out Level3ChapterCheckpoint correcting5m, out _);
string frozen5m = Snap(correcting5m.worksheetCheck);
Check(correcting5m != null && correcting5m.worksheetCheck.assessmentVersion == LegacyAssessment && Level3WorksheetRules.OpenBatch(correcting5m) == "earnings",
    "T05 legacy: a frozen twelve-decision check continues in the corrections phase with the earnings batch open");
string savedR1Legacy5m = Level3WorksheetRules.SavedTreatment(correcting5m.worksheet, "R1");
string other5m = savedR1Legacy5m == "income" ? "deduct" : "income";
Check(savedR1Legacy5m != "" && File5m(correcting5m, "R1", other5m) == Level3CommandOutcome.Applied && Snap(correcting5m.worksheetCheck) == frozen5m &&
      correcting5m.worksheetCheck.firstTier > 0,
    "T05 legacy: a mapped legacy row can be recalled and refiled during corrections; the frozen first grade, tier and XP are untouched (no regrade)");
Level3ChapterRules.TryContinueLegacy(LegacyOf(wsWrong), out Level3ChapterCheckpoint draft5m, out _);
Check(draft5m != null && Level3TableStaging.RackRows(draft5m, rows5m).All(r => Level3WorksheetRules.RowFiled(draft5m.worksheet, r.RowId)) &&
      Level3WorksheetRules.Batches.All(b => !Level3WorksheetRules.BatchAccepted(draft5m, b)) && Level3ChapterRules.BeaWorkIndependent(draft5m) == Level3ChapterRules.BeaWorkIndependent(LegacyOf(wsWrong)),
    "T05 legacy: an unchecked legacy draft shows its mapped earnings choices as filed tabs, accepts nothing and keeps its started-work boundary");

// ---------- pass 2/3: the physical machine (geometry from the shipped constants; Unity rendering not run) ----------
float s5m = Level3TableStaging.Compute(new UnityEngine.Vector2(1.2f, 0.72f), Level3TableStaging.SeatOf(new UnityEngine.Vector2(1.2f, 0.72f), 0.4f)).Scale;
Check(s5m >= 1f && Level3PaperMachineModel.CardWidth + 0.006f < Level3PaperMachineModel.SlotWidth && Level3PaperMachineModel.SlotWidth + 0.016f < Level3PaperMachineModel.BodyWidth &&
      Level3PaperMachineModel.BodyWidth + 0.03f <= Level3TableStaging.MachineWidth,
    "T05 geometry: the card (0.24 m) fits the slot with clearance, the slot the body, the body plus lever the footprint; a full mat keeps scale >= 1");
// The paper path in machine space: the card plane through the slot line, tilt degrees from horizontal, exposed metres above the line.
bool Hidden5m(double y, double z) => (z >= Level3PaperMachineModel.BodyFrontZ && z <= Level3PaperMachineModel.BodyBackZ && y <= Level3PaperMachineModel.TopY) || y <= Level3PaperMachineModel.PlinthHeight;
bool PathHidden5m(double tilt, double exposed)
{
    double c = Math.Cos(tilt * Math.PI / 180), sn = Math.Sin(tilt * Math.PI / 180);
    for (double d = 0.001; d <= Level3PaperMachineModel.CardHeight - exposed + 1e-9; d += 0.002)
        if (!Hidden5m(Level3PaperMachineModel.TopY - d * sn, Level3PaperMachineModel.SlotZ - d * c)) return false;
    return true;
}
bool pathOk5m = PathHidden5m(Level3PaperMachineModel.FeedTilt, Level3PaperMachineModel.HalfFedExposed);
for (int k = 0; k <= 20; k++)
{
    double e = k / 20.0;
    pathOk5m &= PathHidden5m(Level3PaperMachineModel.FeedTilt + (Level3PaperMachineModel.FedTilt - Level3PaperMachineModel.FeedTilt) * e,
        Level3PaperMachineModel.HalfFedExposed + (Level3PaperMachineModel.FedExposed - Level3PaperMachineModel.HalfFedExposed) * e);
}
Check(pathOk5m, "T05 geometry: half-fed and at every point of the full feed, the part of the card below the slot stays inside the opaque body/plinth (no paper through a wall)");
double exposedRise5m = Level3PaperMachineModel.HalfFedExposed * Math.Sin(Level3PaperMachineModel.FeedTilt * Math.PI / 180);
double header5m = Level3PaperMachineModel.CardTop + Level3PaperMachineModel.PositionHeight + Level3PaperMachineModel.TitleHeight + Level3PaperMachineModel.AmountHeight + 2 * Level3PaperMachineModel.CardRowGap;
Check(Level3PaperMachineModel.HalfFedExposed >= header5m && Level3PaperMachineModel.FedExposed < 0f && Level3PaperMachineModel.HoverExposed > Level3PaperMachineModel.CardHeight &&
      Level3PaperMachineModel.FeedTilt == Level3PaperMachineModel.StandTilt && exposedRise5m > 0.04,
    "T05 geometry: half-fed, the position/title/amount header stays above the lip at the stand's reading angle; fully fed, the top edge is behind the lip; hovering, the card is clear of the slot");
double rollerTop5m = Level3PaperMachineModel.TopY - 0.003 + Level3PaperMachineModel.RollerDiameter * 0.5;
double atBackRoller5m = Level3PaperMachineModel.TopY + Level3PaperMachineModel.RollerGap * Math.Tan(Level3PaperMachineModel.FeedTilt * Math.PI / 180);
Check(atBackRoller5m - 0.0015 > rollerTop5m && Level3PaperMachineModel.RollerGap - Level3PaperMachineModel.RollerDiameter * 0.5 > (rollerTop5m - Level3PaperMachineModel.TopY) / Math.Tan(Level3PaperMachineModel.FeedTilt * Math.PI / 180),
    "T05 geometry: the two rollers straddle the paper where it is visible (no roller through the card above the body)");
Check(Level3PaperMachineModel.LeverTouchX - Level3PaperMachineModel.LeverTouchWidth * 0.5f > Level3PaperMachineModel.BodyOffsetX - 0.005f + Level3PaperMachineModel.IntakeTouchWidth * 0.5f + 0.01f &&
      Level3PaperMachineModel.BodyOffsetX + Level3PaperMachineModel.CardWidth * 0.5f < Level3PaperMachineModel.LeverTouchX - Level3PaperMachineModel.LeverTouchWidth * 0.5f,
    "T05 geometry: the lever's touch area is clear of the slot's and of the half-fed card's (no shared edge)");
var dial5m = (lo: Level3PaperMachineModel.DialTouchZ - Level3PaperMachineModel.DialTouchHeight * 0.5, hi: Level3PaperMachineModel.DialTouchZ + Level3PaperMachineModel.DialTouchHeight * 0.5);
var labels5m = (lo: Level3PaperMachineModel.LabelRowZ - (Level3PaperMachineModel.CategoryButtonHeight + 0.002) * 0.5, hi: Level3PaperMachineModel.LabelRowZ + (Level3PaperMachineModel.CategoryButtonHeight + 0.002) * 0.5);
double plateHalf5m = Math.Sqrt(Math.Pow(Level3PaperMachineModel.TopY - Level3PaperMachineModel.PlateBottomY, 2) + Math.Pow(Level3PaperMachineModel.BodyFrontZ - Level3PaperMachineModel.PlateBottomZ, 2)) * 0.5;
Check(dial5m.hi < labels5m.lo && labels5m.hi < plateHalf5m && Level3PaperMachineModel.ChosenZ - Level3PaperMachineModel.ChosenHeight * 0.5f > -plateHalf5m &&
      Level3PaperMachineModel.DialLabelWidth + 0.004 < Level3PaperMachineModel.DetentSpacing &&
      Level3PaperMachineModel.DetentSpacing + Level3PaperMachineModel.DialLabelWidth * 0.5 < Level3PaperMachineModel.BodyWidth * 0.48,
    "T05 geometry: raised category sockets have separate targets above the enlarged dial; the status, buttons and windows fit the taller face");
// Real angular drag mapping: every detent is reachable, the hub is stable and boundaries resist jitter.
Check(Level3PaperMachineModel.PointerAngle(new UnityEngine.Vector2(0f, 0f), -25f) == -25f &&
      Level3PaperMachineModel.PointerAngle(new UnityEngine.Vector2(0.002f, 0.003f), 31f) == 31f,
    "machine polish: rotating inside the hub dead zone retains the last pointer angle");
for (int i = 0; i < 3; i++)
{
    float angle = Level3PaperMachineModel.DetentAngles[i];
    var offset = new UnityEngine.Vector2((float)Math.Sin(angle * Math.PI / 180) * 0.04f, (float)Math.Cos(angle * Math.PI / 180) * 0.04f);
    float pointed = Level3PaperMachineModel.PointerAngle(offset, 0f);
    Check(Math.Abs(pointed - angle) < 0.001f && Level3PaperMachineModel.DetentForAngle(pointed, -1) == i,
        $"machine polish: angular input at {Level3PaperMachineModel.DialLabels[i]} selects that exact category");
}
Check(Level3PaperMachineModel.DetentForAngle(-29f, 0) == 0 && Level3PaperMachineModel.DetentForAngle(-29f, 1) == 1 &&
      Level3PaperMachineModel.DetentForAngle(-34f, 1) == 0 && Level3PaperMachineModel.DetentForAngle(-23f, 0) == 1 &&
      Level3PaperMachineModel.DetentForAngle(29f, 2) == 2 && Level3PaperMachineModel.DetentForAngle(29f, 1) == 1 &&
      Level3PaperMachineModel.DetentForAngle(34f, 1) == 2 && Level3PaperMachineModel.DetentForAngle(23f, 2) == 1,
    "machine polish: both category boundaries hold through jitter and release after deliberate rotation in either direction");
Check(Level3PaperMachineModel.PointerAngle(new UnityEngine.Vector2(-0.04f, 0f), 0f) == Level3PaperMachineModel.DetentAngles[0] &&
      Level3PaperMachineModel.PointerAngle(new UnityEngine.Vector2(0.04f, 0f), 0f) == Level3PaperMachineModel.DetentAngles[2],
    "machine polish: rotation stops at the end detents instead of wrapping category selections");
foreach (float held in new[] { Level3PaperMachineModel.NeutralAngle, -57f, 0f, 57f })
    Check(Level3PaperMachineModel.PointerAngle(new UnityEngine.Vector2(-0.001f, -0.03f), held) == held &&
          Level3PaperMachineModel.PointerAngle(new UnityEngine.Vector2(0.001f, -0.03f), held) == held,
        $"machine polish: the dial's rear stop retains {held} degrees across the bottom seam, without jumping endpoints or inventing a neutral choice");
for (int i = 0; i < 3; i++)
{
    double a = Level3PaperMachineModel.DetentAngles[i] * Math.PI / 180;
    double toLabel = Math.Atan2((i - 1) * Level3PaperMachineModel.DetentSpacing, Level3PaperMachineModel.LabelRowZ - Level3PaperMachineModel.DialZ) * 180 / Math.PI;
    Check(Math.Abs(toLabel - Level3PaperMachineModel.DetentAngles[i]) < 2.5 && Math.Abs(Level3PaperMachineModel.NeutralAngle - 180f) < 0.001,
        $"T05 geometry: the {Level3PaperMachineModel.DialLabels[i]} detent points at its own label; the neutral home points down the plate");
}
Check(Level3PaperMachineModel.LeverPulled < Level3PaperMachineModel.LeverRaised && Math.Sin(Level3PaperMachineModel.LeverPulled * Math.PI / 180) < 0 &&
      Level3PaperMachineModel.LeverThreshold >= 0.5f && Level3PaperMachineModel.LeverThreshold <= 0.7f,
    "T05 geometry: pulling brings the handle forward and down toward the player; a release past a deliberate threshold (half to seven tenths of the stroke) files");

// Projected touch targets at the default seat (pinhole camera, 60 degrees vertical; landscape 2400 x 1080, ~2.47 px per dp on a 6.1-inch phone).
{
    var mat = new UnityEngine.Vector2(1.2f, 0.72f);
    var lay = Level3TableStaging.Compute(mat, Level3TableStaging.SeatOf(mat, 0.4f));
    double sc = lay.Scale, top = Level3BeaTable.TableTop + 0.003;
    var focus = new double[] { 0, Level3BeaTable.TableTop, -mat.y * 0.08 };
    var seatTop = new double[] { lay.Seat.x, lay.Seat.y + Level3BeaTable.TableTop, lay.Seat.z };
    var cam = new double[3];
    for (int i = 0; i < 3; i++) cam[i] = focus[i] + (seatTop[i] - focus[i]) * 0.76;
    double[] Sub(double[] p, double[] q) => new[] { p[0] - q[0], p[1] - q[1], p[2] - q[2] };
    double Dot(double[] p, double[] q) => p[0] * q[0] + p[1] * q[1] + p[2] * q[2];
    double[] Norm(double[] p) { double l = Math.Sqrt(Dot(p, p)); return new[] { p[0] / l, p[1] / l, p[2] / l }; }
    double[] Cross(double[] p, double[] q) => new[] { p[1] * q[2] - p[2] * q[1], p[2] * q[0] - p[0] * q[2], p[0] * q[1] - p[1] * q[0] };
    double[] fwd = Norm(Sub(focus, cam)), right = Norm(Cross(new double[] { 0, 1, 0 }, fwd)), up = Cross(fwd, right);
    const double W = 2400, H = 1080, PxPerDp = 2.47;
    double f = H * 0.5 / Math.Tan(30 * Math.PI / 180);
    double[] Px(double[] p) { var d = Sub(p, cam); double z = Dot(d, fwd); return new[] { W * 0.5 + Dot(d, right) / z * f, H * 0.5 - Dot(d, up) / z * f }; }
    double[] Yaw(double deg, double[] p) { double a = deg * Math.PI / 180, c = Math.Cos(a), sn = Math.Sin(a); return new[] { p[0] * c + p[2] * sn, p[1], -p[0] * sn + p[2] * c }; }
    double[] Pitch(double deg, double[] p) { double a = deg * Math.PI / 180, c = Math.Cos(a), sn = Math.Sin(a); return new[] { p[0], p[1] * c - p[2] * sn, p[1] * sn + p[2] * c }; }
    double[] Add(double[] p, double[] q) => new[] { p[0] + q[0], p[1] + q[1], p[2] + q[2] };
    double[] Mul(double k, double[] p) => new[] { p[0] * k, p[1] * k, p[2] * k };
    // Machine space -> table space (column root: position, yaw, scale; machine child offset).
    double machineOffset = (lay.Machine.center.y - lay.Column.center.y) / sc, rackOffset = (lay.Rack.center.y - lay.Column.center.y) / sc;
    double[] Column(double[] local) => Add(new double[] { lay.Column.center.x, top, lay.Column.center.y }, Yaw(lay.MachineYaw, Mul(sc, local)));
    double[] Machine(double[] m) => Column(new[] { m[0], m[1], m[2] + machineOffset });
    double[] StandP(double[] local) => Add(new double[] { lay.Stand.center.x, top, lay.Stand.center.y }, Yaw(lay.StandYaw, Mul(sc, local)));
    // Plate frame (machine space).
    double[] bottom = { Level3PaperMachineModel.BodyOffsetX, Level3PaperMachineModel.PlateBottomY, Level3PaperMachineModel.PlateBottomZ };
    double[] upper = { Level3PaperMachineModel.BodyOffsetX, Level3PaperMachineModel.TopY, Level3PaperMachineModel.BodyFrontZ };
    double[] pzv = Norm(Sub(upper, bottom)), pyv = Norm(new[] { 0, pzv[2], -pzv[1] }), pcen = Mul(0.5, Add(bottom, upper));
    double[] Plate(double x, double y, double z) => Machine(Add(pcen, Add(Mul(x, new double[] { 1, 0, 0 }), Add(Mul(y, pyv), Mul(z, pzv)))));
    double hingeZ = -Level3TableStaging.StandDepth * 0.5 + 0.03;
    double[] Card(double x, double z) => StandP(Add(new[] { 0, 0.012, hingeZ }, Pitch(-Level3PaperMachineModel.StandTilt, new[] { x, 0.0045, 0.012 + Level3PaperMachineModel.CardHeight * 0.5 + z })));
    double[] Fed(double x, double z) => Machine(Add(new double[] { Level3PaperMachineModel.BodyOffsetX, Level3PaperMachineModel.TopY, Level3PaperMachineModel.SlotZ },
        Pitch(-Level3PaperMachineModel.FeedTilt, new[] { x, 0.0045, Level3PaperMachineModel.HalfFedExposed - Level3PaperMachineModel.CardHeight * 0.5 + z })));
    List<double[]> Quad(Func<double, double, double[]> at, double cx, double cz, double w, double h) =>
        new List<double[]> { Px(at(cx - w / 2, cz - h / 2)), Px(at(cx + w / 2, cz - h / 2)), Px(at(cx + w / 2, cz + h / 2)), Px(at(cx - w / 2, cz + h / 2)) };
    double cw = Level3PaperMachineModel.CardWidth, ch = Level3PaperMachineModel.CardHeight;
    double tabTop = -ch * 0.5 + Level3PaperMachineModel.TabHeight + 0.006, headerH = Level3PaperMachineModel.HalfFedExposed - 0.008;
    double bodyD = Level3PaperMachineModel.BodyBackZ - Level3PaperMachineModel.BodyFrontZ;
    var intake = Quad((x, z) => Machine(new[] { x, Level3PaperMachineModel.TopY + 0.009, z }), Level3PaperMachineModel.BodyOffsetX - 0.005,
        (Level3PaperMachineModel.BodyFrontZ + Level3PaperMachineModel.BodyBackZ) * 0.5 + 0.002, Level3PaperMachineModel.IntakeTouchWidth, bodyD - 0.004);
    var lever = Quad((x, y) => Machine(new[] { x, y, (double)Level3PaperMachineModel.LeverTouchZ }), Level3PaperMachineModel.LeverTouchX, Level3PaperMachineModel.LeverTouchY,
        Level3PaperMachineModel.LeverTouchWidth, Level3PaperMachineModel.LeverTouchHeight);
    var states = new Dictionary<string, List<(string name, List<double[]> quad)>>
    {
        ["on the stand"] = new List<(string, List<double[]>)>
        {
            ("card + INSPECT", Quad(Card, 0, 0, Level3PaperMachineModel.PaperTouchWidth, Level3PaperMachineModel.PaperTouchHeight)),
            ("slot", intake)
        },
        ["half-fed"] = new List<(string, List<double[]>)>
        {
            ("header", Quad(Fed, 0, ch * 0.5 - headerH * 0.5, cw, headerH)),
            ("Income", Quad((x, z) => Plate(x, 0.0135, z), -Level3PaperMachineModel.DetentSpacing, Level3PaperMachineModel.LabelRowZ, Level3PaperMachineModel.DialLabelWidth + 0.002, Level3PaperMachineModel.CategoryButtonHeight + 0.002)),
            ("Deduct", Quad((x, z) => Plate(x, 0.0135, z), 0, Level3PaperMachineModel.LabelRowZ, Level3PaperMachineModel.DialLabelWidth + 0.002, Level3PaperMachineModel.CategoryButtonHeight + 0.002)),
            ("Leave Out", Quad((x, z) => Plate(x, 0.0135, z), Level3PaperMachineModel.DetentSpacing, Level3PaperMachineModel.LabelRowZ, Level3PaperMachineModel.DialLabelWidth + 0.002, Level3PaperMachineModel.CategoryButtonHeight + 0.002)),
            ("dial", Quad((x, z) => Plate(x, 0.01, z), 0, Level3PaperMachineModel.DialTouchZ, Level3PaperMachineModel.DialTouchWidth, Level3PaperMachineModel.DialTouchHeight)),
            ("lever", lever),
            ("Why?", Quad(Card, 0, ch * 0.5 - Level3PaperMachineModel.CardTop - Level3PaperMachineModel.WhyRowHeight * 0.5, cw - 0.012, Level3PaperMachineModel.WhyRowHeight + 0.004)),
            ("note INSPECT", Quad(Card, -(cw - 0.024) * 0.25 - 0.004, -ch * 0.5 + Level3PaperMachineModel.NoteTabHeight * 0.5 + 0.006, (cw - 0.024) * 0.5, Level3PaperMachineModel.NoteTabHeight + 0.004)),
            ("COMPARE", Quad(Card, (cw - 0.024) * 0.25 + 0.004, -ch * 0.5 + Level3PaperMachineModel.NoteTabHeight * 0.5 + 0.006, (cw - 0.024) * 0.5, Level3PaperMachineModel.NoteTabHeight + 0.004))
        }
    };
    double[] Review(double x, double z, int slot) => Column(Add(new[] { (slot - 1) * Level3TableStaging.RackWidth * 0.33, 0.013, rackOffset - Level3TableStaging.RackDepth * 0.5 + 0.004 },
        Pitch(-Level3PaperMachineModel.RackTabTilt, new[] { x, 0.002, z })));
    states["review tabs"] = new List<(string, List<double[]>)>();
    for (int slot = 0; slot < 3; slot++)
    {
        int captured = slot;
        states["review tabs"].Add(($"tab {slot + 1}", Quad((x, z) => Review(x, z, captured), 0, Level3PaperMachineModel.RackTabDepth * 0.5,
            Level3PaperMachineModel.ReviewTouchWidth, Level3PaperMachineModel.ReviewTouchHeight)));
    }
    bool Overlap(List<double[]> a, List<double[]> b)
    {
        foreach (var poly in new[] { a, b })
            for (int i = 0; i < 4; i++)
            {
                double[] p = poly[i], q = poly[(i + 1) % 4];
                double nx = q[1] - p[1], ny = p[0] - q[0];
                double amin = double.MaxValue, amax = double.MinValue, bmin = double.MaxValue, bmax = double.MinValue;
                foreach (var v in a) { double d = v[0] * nx + v[1] * ny; amin = Math.Min(amin, d); amax = Math.Max(amax, d); }
                foreach (var v in b) { double d = v[0] * nx + v[1] * ny; bmin = Math.Min(bmin, d); bmax = Math.Max(bmax, d); }
                if (amax < bmin || bmax < amin) return false;
            }
        return true;
    }
    double MinSide(List<double[]> qd)
    {
        double Len(double[] p, double[] q) => Math.Sqrt((p[0] - q[0]) * (p[0] - q[0]) + (p[1] - q[1]) * (p[1] - q[1]));
        return Math.Min(Math.Min(Len(qd[0], qd[1]), Len(qd[2], qd[3])), Math.Min(Len(qd[1], qd[2]), Len(qd[3], qd[0])));
    }
    foreach (var (state, targets) in states)
    {
        var overlaps = new List<string>();
        for (int i = 0; i < targets.Count; i++)
            for (int j = i + 1; j < targets.Count; j++)
                if (Overlap(targets[i].quad, targets[j].quad)) overlaps.Add(targets[i].name + "/" + targets[j].name);
        bool onScreen = targets.All(t => t.quad.All(v => v[0] >= 0 && v[0] <= W && v[1] >= 0 && v[1] <= H));
        Check(overlaps.Count == 0 && onScreen, $"T05 projection ({state}): live touch targets do not overlap and stay on screen{(overlaps.Count > 0 ? " (overlap " + string.Join(", ", overlaps) + ")" : "")}");
        Console.WriteLine($"T05 projected targets {state} (min side, dp at 2.47 px/dp on 2400x1080): " +
            string.Join(", ", targets.Select(t => $"{t.name} {MinSide(t.quad) / PxPerDp:0}")));
        if (state == "half-fed")
            Check(targets.Where(t => new[] { "Income", "Deduct", "Leave Out", "dial", "lever" }.Contains(t.name)).All(t => MinSide(t.quad) / PxPerDp >= 45),
                "machine polish projection: category buttons, dial and lever retain at least 45 dp on their shorter side at the default phone seat");
        Check(targets.All(t => MinSide(t.quad) / PxPerDp >= 20), $"T05 projection ({state}): every live target is at least 20 dp on its shorter side (48 dp is the aim; see KNOWN_ISSUES)");
    }
}

Check(typeof(Level3TablePaperRaycaster).GetProperty("sortOrderPriority").DeclaringType == typeof(Level3TablePaperRaycaster),
    "table input arbitration: the seated table declares a pointer priority instead of losing to every overlay HUD graphic before hit distance is compared");
Check(Level3TablePaperRaycaster.PointerPriority > 0 && Level3TablePaperRaycaster.PointerPriority < 1100 &&
      Level3TablePaperRaycaster.PointerPriority < OfficeHudRuntime.ExitDeskSortingOrder,
    "table input arbitration: ordinary overlay HUD stays below the focused table; document windows and Leave Table keep precedence");
foreach (float x in new[] { -0.127f, 0.127f })
foreach (float z in new[] { -0.095f, 0.095f })
    Check(Level3TablePaperRaycaster.HitPlane(new UnityEngine.Ray(new UnityEngine.Vector3(x, z, -1f), UnityEngine.Vector3.forward), UnityEngine.Matrix4x4.identity,
        new UnityEngine.Vector2(Level3PaperMachineModel.PaperTouchWidth, Level3PaperMachineModel.PaperTouchHeight), out _),
        "paper touch padding: all four outside-border corners remain reachable without reducing the whole-paper gesture");
Check(Level3PaperMachineModel.DialTouchWidth >= Level3PaperMachineModel.DialDiameter + 0.03f &&
      Level3PaperMachineModel.ReviewTouchWidth < Level3TableStaging.RackWidth * 0.33f,
    "larger controls: the dial has generous invisible side padding and neighboring enlarged review targets remain separate");
// Physical-plane ingress is independent of Canvas rect size/position: the same path serves card, note buttons and tabs.
foreach (float x in new[] { -0.119f, 0f, 0.119f })
foreach (float z in new[] { -0.089f, -0.073f, 0f, 0.089f })
{
    var ray = new UnityEngine.Ray(new UnityEngine.Vector3(x, z, -1f), UnityEngine.Vector3.forward);
    Check(Level3TablePaperRaycaster.HitPlane(ray, UnityEngine.Matrix4x4.identity, new UnityEngine.Vector2(Level3PaperMachineModel.CardWidth, Level3PaperMachineModel.CardHeight), out float distance) && Math.Abs(distance - 1f) < 0.0001f,
        "paper input repair: any point across the complete paper enters its one physical hit plane");
    Check(Level3PaperMachineModel.StandCardAction(z) == Level3MachineInput.Card,
        "paper input repair: taps over the printed lower strip inspect; every other part arms the same draggable paper");
}
foreach (float x in new[] { -0.044f, 0f, 0.044f })
foreach (float z in new[] { -0.025f, 0f, 0.025f })
    Check(Level3TablePaperRaycaster.HitPlane(new UnityEngine.Ray(new UnityEngine.Vector3(x, z, -1f), UnityEngine.Vector3.forward), UnityEngine.Matrix4x4.identity,
          new UnityEngine.Vector2(Level3PaperMachineModel.RackTabWidth, Level3PaperMachineModel.RackTabDepth), out _),
        "tab input repair: the entire visible review tab receives hits in physical metres, independent of the Canvas rectangle");
Check(!Level3TablePaperRaycaster.HitPlane(new UnityEngine.Ray(new UnityEngine.Vector3(0.13f, 0f, -1f), UnityEngine.Vector3.forward), UnityEngine.Matrix4x4.identity,
      new UnityEngine.Vector2(Level3PaperMachineModel.CardWidth, Level3PaperMachineModel.CardHeight), out _),
    "paper input repair: outside the paper cannot trigger its full-face gesture");

// Screenshot repairs: text depth, relative rotary gestures, and a recalled record after the last card.
Check(Level3PaperMachineModel.WindowTextY - Level3PaperMachineModel.WindowSurfaceY >= 0.002f,
    "machine repair: amount glyphs have a real depth gap above the display surface (not coplanar/z-fighting)");
foreach (float held in new[] { -40f, 0f, 40f })
{
    float after = Level3PaperMachineModel.RotatePointer(held, new UnityEngine.Vector2(-0.001f, -0.03f), new UnityEngine.Vector2(0.001f, -0.03f));
    Check(Math.Abs(after - held) < 5f, "machine repair: a grabbed lower rim crosses the atan2 seam by a few degrees, not across all categories");
}
Check(Level3PaperMachineModel.RotatePointer(180f, new UnityEngine.Vector2(0f, 0f), new UnityEngine.Vector2(0.001f, 0.001f)) == 180f &&
      Level3PaperMachineModel.RotatePointer(0f, new UnityEngine.Vector2(0f, 0.03f), new UnityEngine.Vector2(0.03f, 0f)) == 57f &&
      Level3PaperMachineModel.RotatePointer(0f, new UnityEngine.Vector2(0f, 0.03f), new UnityEngine.Vector2(-0.03f, 0f)) == -57f,
    "machine repair: hub wobble stays neutral; a real clockwise/counterclockwise rim turn reaches either end stop");
var recalledLastRepair = new Level3TableStaging.Machine();
recalledLastRepair.Present(null);
Check(recalledLastRepair.Recall("R2", "income", "", 4) && recalledLastRepair.Recalled && recalledLastRepair.Row == "R2" &&
      recalledLastRepair.Step == Level3TableStaging.MachineStep.Loaded,
    "machine repair: the last completed batch's tab restores an inspectable row even with no stand card");

// ---------- pass 3: motion contract, one owner per part, lifecycle (source; Unity timing/feel not run) ----------
float[] range5m(float lo, float hi, float v) => v >= lo - 1e-4f && v <= hi + 1e-4f ? new[] { v } : null;
Check(range5m(0.15f, 0.25f, Level3PaperMachineModel.HalfFeedSeconds) != null && range5m(0.15f, 0.25f, Level3PaperMachineModel.ReturnSeconds) != null &&
      range5m(0.15f, 0.25f, 0.22f) != null && range5m(0.08f, 0.12f, Level3PaperMachineModel.DetentSeconds) != null &&
      range5m(0.12f, 0.18f, Level3PaperMachineModel.LeverDownSeconds) != null && range5m(0.20f, 0.30f, Level3PaperMachineModel.FullFeedSeconds) != null &&
      range5m(0.12f, 0.20f, Level3PaperMachineModel.LeverReturnSeconds) != null && range5m(0.12f, 0.20f, Level3PaperMachineModel.TabSettleSeconds) != null &&
      Level3PaperMachineModel.FullFeedSeconds + Math.Max(Level3PaperMachineModel.TabSettleSeconds, 0.22f) <= 0.6f,
    "T05 motion: card, half-feed, detent, lever, full feed and tab/next durations sit in the contract's ranges; the next card is ready about 0.48 s after the save");
string model5m = Src("Scripts/Level3/Level3PaperMachineModel.cs"), table5m = Src("Scripts/Level3/Level3BeaTable.cs");
string surfaceBuildRepair = model5m.Substring(Index(model5m, "private Surface AddSurface("), Index(model5m, "private void RefreshSurfaces()") - Index(model5m, "private Surface AddSurface("));
Check(Index(surfaceBuildRepair, "surface.Canvas.renderMode = RenderMode.WorldSpace;") < Index(surfaceBuildRepair, "hit.localPosition = localPosition;"),
    "machine input repair: world-space Canvas initializes before the final hit rectangle pose and dimensions are assigned");
int pull5m = Index(table5m, "private void PullLever()");
string pullBody5m = table5m.Substring(pull5m, Index(table5m, "private void FeedDone()") - pull5m);
Check(Index(pullBody5m, "owner.CommitWorksheet(") > 0 && Index(pullBody5m, "owner.CommitWorksheet(") < Index(pullBody5m, "Machine.FeedIn(FeedDone);") &&
      Index(pullBody5m, "owner.CommitWorksheet(") < Index(pullBody5m, "Machine.LeverStroke();") && Index(pullBody5m, "owner.CommitWorksheet(") < Index(pullBody5m, "Cue(\"sfx_paper_rustle_01\"") &&
      Index(pullBody5m, "flow.BeginFile(out Level3Filing filing)") < Index(pullBody5m, "owner.CommitWorksheet(") &&
      pullBody5m.Contains("if (!committed)") && pullBody5m.Contains("Machine.LeverBack();") &&
      pullBody5m.Contains("if (result == Level3Checkpointer.TapeCommit.Attached) GameplayEvents.RaiseGuideCue(\"WorksheetFiled\");"),
    "T05 wiring: one filing per pull (flow) -> candidate -> signed write -> publish, then the lever stroke, rollers, feed and cues; a failed save springs the lever back only");
Check(!pullBody5m.Contains("Treatment ==") && !pullBody5m.Contains("expected") && !pullBody5m.Contains("Correct") && !model5m.Contains("sfx_classification") &&
      !table5m.Contains("sfx_classification") && !model5m.Contains("sfx_ui_unavailable") && !table5m.Contains("WorldEmote.Show(Bea.transform, Emotion.Happy"),
    "T05 wiring: every valid filing gets the identical feed and sounds (no branch on the choice, no correctness sound/emote, no buzz)");
Check(model5m.Contains("private readonly Motion cardMotion = new Motion(), leverMotion = new Motion(), dialMotion = new Motion(), noteMotion = new Motion(),") &&
      model5m.Contains("motion.Start(settling || ReducedMotion ? 0f : seconds, apply, done);") && !model5m.Contains("WaitForSeconds") && !model5m.Contains("StartCoroutine") &&
      model5m.Contains("bool any = cardMotion.Finish() | leverMotion.Finish() | dialMotion.Finish()"),
    "T05 motion: one motion per part (a new one replaces it), reduced motion snaps, Settle finishes every motion at once; no coroutine waits");
Check(model5m.Contains("bool stand = on && !view.Busy && !WhyUp && cardPlace == CardPlace.Stand") && model5m.Contains("bool loaded = on && !view.Busy && !WhyUp && view.Loaded") &&
      model5m.Contains("bool why = on && WhyUp && !whyMotion.Running;") && model5m.Contains("bool tabsLive = on && view.TabsLive && !view.Busy && !view.Loaded && !WhyUp") &&
      model5m.Contains("if (owner != dragOwner) return;") && model5m.Contains("private void OnDisable() { SetHighlighted(false); Model?.OwnerDisabled(this); }"),
    "T05 input: only the current step's surfaces are live (stand / loaded / Why? / tabs), none while saving or feeding; a drag belongs to the surface that started it");
int cancel5m = Index(table5m, "private void CancelDrags()");
string cancelBody5m = table5m.Substring(cancel5m, Index(table5m, "// SEAT") - cancel5m) +
    table5m.Substring(Index(table5m, "private void GatherAll()"), Index(table5m, "public static void Level3Say") - Index(table5m, "private void GatherAll()"));
Check(cancelBody5m.Contains("Machine?.Settle();") && cancelBody5m.Contains("flow.Reset();") && cancelBody5m.Contains("Machine?.Rest();") &&
      table5m.Contains("if (!free) CancelDrags();") && table5m.Contains("if (!focused) CancelDrags();") && table5m.Contains("if (paused) CancelDrags();") &&
      model5m.Contains("if (!on) CancelDrag();"),
    "T05 lifecycle: settings/dialogue/pause/camera/focus end drags undecided and settle motions; leaving or disabling drops an unfiled card back to the stand");
Check(table5m.Contains("on &= Machine == null || !Machine.WhyUp;") && table5m.Contains("PutOriginalsAway();\n        SetPapersInteractive(false);\n        Machine.OpenWhy(ViewCamera, flow.Reason);"),
    "T05 reader: the Why? card and an original never rise into the reading place together");
foreach (string clip5m in new[] { "sfx_card_pickup_01", "sfx_paper_place_01", "sfx_card_return_01", "sfx_ui_click_01", "sfx_ui_select_01", "sfx_stamp_return_01", "sfx_paper_rustle_01", "sfx_card_place_02" })
    Check((table5m + model5m).Contains("\"" + clip5m + "\"") && Directory.EnumerateFiles(Path.Combine(root, "Assets"), clip5m + ".mp3", SearchOption.AllDirectories).Any(),
        $"T05 audio: the cue {clip5m} is an existing shipped clip (no new or downloaded sound)");
Check(Src("Scripts/Level3/Level3ChapterRules.cs").Contains("Level3WorksheetRules.RevisedDestination(s.section) != \"\"") &&
      Src("Scripts/Level3/Level3Checkpointer.cs").Contains("case \"machineWhy\":") && Src("Scripts/Level3/Level3Checkpointer.cs").Contains("case \"machineBatchFiled\":"),
    "T05 wiring: Bea's clock boundary is the first saved filing; the guide reads the machine through read-only flags");
GuideScript guide5m = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var intro5m = guide5m.lessons.Single(l => l.id == Level3ChapterRules.WorksheetLesson);
var batch5m = guide5m.lessons.Single(l => l.id == "machine-batch-filed");
Check(intro5m.when == "flag:atBeaTable" && string.IsNullOrEmpty(intro5m.after) && intro5m.onlyIn == "mode:Workstation",
    "T05 guide: the seated lesson starts at the first seat, in the workstation view, with no other prerequisite");
Check(intro5m.pages.Select(pg => pg.text.Split(':')[0]).SequenceEqual(new[] { "Feed the record", "Choose where it belongs", "Pull to file" }),
    "T05 guide: the seated lesson's steps are exactly Feed the record / Choose where it belongs / Pull to file");
Check(
      guide5m.lessons.Where(l => new[] { Level3ChapterRules.WorksheetLesson, Level3ChapterRules.WorksheetNotesLesson, "machine-batch-filed" }.Contains(l.id))
          .SelectMany(l => l.pages).All(pg => pg.text.Split(' ').Length <= 14 && !new[] { "Income", "Deduct", "Leave Out", "Personal purpose", "Already represented" }.Any(pg.text.Contains)) &&
      batch5m.when == "flag:machineBatchFiled" && !batch5m.pages.Any(pg => pg.text.Contains("Check")),
    "T05 guide: machine hints are 14 words or fewer and name no treatment or reason; the batch hint promises no check yet");
Check(!Level3ChapterRules.ImpliedLessons(base5m).Contains(Level3ChapterRules.WorksheetLesson) && Level3ChapterRules.ImpliedLessons(loop5m).Contains(Level3ChapterRules.WorksheetLesson) &&
      !Level3ChapterRules.ImpliedLessons(loop5m).Contains(Level3ChapterRules.WorksheetNotesLesson) && Level3ChapterRules.ImpliedLessons(all5m).Contains(Level3ChapterRules.WorksheetNotesLesson),
    "T05 guide: a restore with a filed record skips the machine lesson; the Why? hint is skipped only once a reason was saved");
var fixtureQ5m = Level3EditorFixtures.MachineBatch(bea, Level3WorksheetRules.QuestionsBatch);
var fixtureC5m = Level3EditorFixtures.MachineBatch(bea, Level3WorksheetRules.CostsBatch);
Check(fixtureQ5m != null && Level3TableStaging.ActiveRow(fixtureQ5m, rows5m)?.RowId == "R6" && fixtureC5m != null && Level3TableStaging.ActiveRow(fixtureC5m, rows5m)?.RowId == "R3" &&
      !fixtureQ5m.worksheetCheck.firstChecked && Level3EditorFixtures.Build(Level3EditorFixtures.BeaQuestionsBatchId, bea) != null,
    "T05 fixtures: the Editor-only costs/questions machine fixtures open on R3/R6 through the real lever command, graded nothing");

Console.WriteLine($"Revised T05 checks: {checks - t05mStart}, failures {fails - t05mFails} (filing/batches/totals/legacy/save failure/duplicates/flow/geometry/projection/motion/lifecycle; Unity rendering, native touch, audio and feel not run).");

// =====================================================================
// Revised T06 (5 November 2026): check machine batches, correct, collect Bea's summary
// =====================================================================
int t06mStart = checks, t06mFails = fails;
List<Level3WorksheetRow> rows6m = Level3WorksheetRules.RowsOf(bea);
Level3ChapterCheckpoint base6m = freshHanded.Copy();
base6m.worksheet = new Level3WorksheetDraft();
Level3ChapterCheckpoint Mutate6m(Level3ChapterCheckpoint s, Action<Level3ChapterCheckpoint> change) { Level3ChapterCheckpoint c = s.Copy(); change(c); return c; }
string Reason6m(string row) => row == "R6" ? "personal-purpose" : row == "R7" ? "already-represented" : "";
string Right6m(string row) => row == "R1" || row == "R2" ? "income" : row == "R6" || row == "R7" ? "leave-out" : "deduct";
Level3CommandOutcome File6m(Level3ChapterCheckpoint c, string row, string treatment, string reason = null) =>
    Level3WorksheetRules.File(c, rows6m, new Level3Filing { RowId = row, Treatment = treatment, Reason = reason ?? Reason6m(row), BasedOn = c.worksheet.revision }, out _);
Level3WorksheetCheck.CheckOutcome Check6m(Level3ChapterCheckpoint c, string batch, out Level3BatchEvaluation e) =>
    Level3WorksheetCheck.CheckBatch(c, bea, batch, c.worksheet.revision, out e, out _);
string First6m(Level3ChapterCheckpoint c) =>
    $"{c.worksheetCheck.assessmentVersion}/{c.worksheetCheck.firstChecked}/{c.worksheetCheck.firstCorrect}/{c.worksheetCheck.firstTotal}/" +
    $"{string.Join(",", c.worksheetCheck.firstWrong)}/{c.worksheetCheck.firstTier}/{c.worksheetCheck.firstXp}/{string.Join(",", c.worksheetCheck.checkedBatches)}";
// A whole machine run: file each batch with the given first choices, check (freezes), then correct and recheck until accepted.
Level3ChapterCheckpoint Run6m(Dictionary<string, (string t, string r)> first)
{
    Level3ChapterCheckpoint c = base6m.Copy();
    foreach (string batch in Level3WorksheetRules.Batches)
    {
        foreach (string row in Level3WorksheetRules.RowsInBatch(batch))
        {
            (string t, string r) pick = first != null && first.TryGetValue(row, out var p) ? p : (Right6m(row), Reason6m(row));
            if (File6m(c, row, pick.t, Level3WorksheetRules.NeedsReason(row) ? pick.r : "") != Level3CommandOutcome.Applied) return null;
        }
        if (Check6m(c, batch, out Level3BatchEvaluation e1) != Level3WorksheetCheck.CheckOutcome.Checked || !e1.Froze) return null;
        if (!e1.Accepted)
        {
            foreach (string row in Level3WorksheetRules.RowsInBatch(batch))
            {
                bool rowRight = Level3WorksheetRules.SavedTreatment(c.worksheet, row) == Right6m(row) &&
                                (!Level3WorksheetRules.NeedsReason(row) || Level3WorksheetRules.SavedReason(c.worksheet, row) == Reason6m(row));
                if (!rowRight && File6m(c, row, Right6m(row)) != Level3CommandOutcome.Applied) return null;
            }
            if (Check6m(c, batch, out Level3BatchEvaluation e2) != Level3WorksheetCheck.CheckOutcome.Checked || e2.Froze || !e2.Accepted) return null;
        }
    }
    return c;
}

// ---------- pass 1a: the nine-decision registry, tiers and XP ----------
Check(Level3WorksheetCheck.RevisedDecisionIds.Length == 9 && Level3WorksheetCheck.RevisedDecisionCount == 9 &&
      Level3WorksheetRules.Batches.Select(b => Level3WorksheetRules.RowsInBatch(b).Count).SequenceEqual(new[] { 2, 3, 2 }) &&
      Level3WorksheetRules.Batches.Select(b => Level3WorksheetCheck.DecisionsInBatch(b).Count).SequenceEqual(new[] { 2, 3, 4 }) &&
      Level3WorksheetRules.Batches.SelectMany(b => Level3WorksheetCheck.DecisionsInBatch(b)).OrderBy(x => x).SequenceEqual(Level3WorksheetCheck.RevisedDecisionIds.OrderBy(x => x)) &&
      Level3WorksheetCheck.RevisedDecisionIds.All(id => Level3WorksheetCheck.DecisionIds.Contains(id)) &&
      Level3WorksheetCheck.RevisedDecisionIds.All(id => Level3WorksheetCheck.DecisionsInBatch(Level3WorksheetCheck.BatchOfDecision(id)).Contains(id)) &&
      Level3WorksheetCheck.RevisedAssessmentVersion == "L3-worksheet-v2",
    "T06: three batches of 2/3/2 records carry 2/3/4 scored decisions: seven treatments plus the R6/R7 reasons (L3-worksheet-v2)");
var tiers6m = new[] { (0, 3, 60), (1, 2, 59), (2, 2, 58), (3, 1, 57), (4, 1, 56), (5, 1, 55), (6, 1, 54), (7, 1, 53), (8, 1, 52), (9, 1, 51) };
Check(tiers6m.All(t => Level3WorksheetCheck.RevisedTierFor(9 - t.Item1) == t.Item2 && Level3WorksheetCheck.RevisedXpFor(t.Item1) == t.Item3) &&
      Level3WorksheetCheck.RevisedMinXp == 51 && Level3WorksheetCheck.TierFor(12) == 3 && Level3WorksheetCheck.XpFor(12) == 48,
    "T06: Gold 9/9, Silver 7-8, Bronze 0-6; XP = 60 minus first-check wrong decisions (51-60), the REVISION_PLAN fixture table; v1 tiers unchanged");

// ---------- pass 1b: refusals and incomplete checks freeze nothing ----------
Level3ChapterCheckpoint none6m = base6m.Copy();
string noneSnap6m = Snap(none6m);
Check(Check6m(none6m, "earnings", out Level3BatchEvaluation inc6m) == Level3WorksheetCheck.CheckOutcome.Incomplete && Snap(none6m) == noneSnap6m &&
      !inc6m.Complete && inc6m.MissingRows.SequenceEqual(new[] { "R1", "R2" }) && inc6m.MissingLines.All(l => l.EndsWith("still needs filing.")) && inc6m.Decisions.Count == 0,
    "T06: an incomplete check names what is missing (both earnings records) and saves, freezes and marks nothing");
Level3ChapterCheckpoint half6m = base6m.Copy();
File6m(half6m, "R1", "income");
string halfSnap6m = Snap(half6m);
Check(Level3WorksheetCheck.CheckBatch(half6m, bea, "earnings", half6m.worksheet.revision, out Level3BatchEvaluation inc2, out string incWhy6m) == Level3WorksheetCheck.CheckOutcome.Incomplete &&
      Snap(half6m) == halfSnap6m && inc2.MissingRows.SequenceEqual(new[] { "R2" }) && incWhy6m.Contains("Menu illustration fees still needs filing"),
    "T06: one record still to file: Check batch says which, by its own label, and freezes nothing");
Level3ChapterCheckpoint whyless6m = Accepted5m(base6m, "earnings", "costs");
whyless6m.worksheet.strips.Add(new Level3StripRecord { rowId = "R6", sourceId = "B-PERSONAL", amount = "24000.00", section = "not-deductible" });
whyless6m.worksheet.strips.Add(new Level3StripRecord { rowId = "R7", sourceId = "B-PRINT", amount = "30000.00", section = "supporting" });
Check(Level3WorksheetCheck.EvaluateBatch(whyless6m, bea, "questions") is var q6m && !q6m.Complete && q6m.MissingLines.SequenceEqual(new[] { "Birthday dinner, decor needs a reason.", "Printing bill, paid needs a reason." }),
    "T06: a legacy questions slip without its Why? reason is incomplete (the reason is a scored decision), never graded as wrong");
foreach (var (what6m, state6m, batch6m) in new[] {
    ("a later batch", base6m.Copy(), "costs"), ("an unknown batch", base6m.Copy(), "everything"), ("before the handover", fresh2.Copy(), "earnings"),
    ("an accepted batch", Accepted5m(base6m, "earnings"), "earnings"), ("a ready worksheet", Mutate6m(base6m, c => c.phase = "WorksheetReady"), "questions") })
{
    string before6m = Snap(state6m);
    Check(Level3WorksheetCheck.CheckBatch(state6m, bea, batch6m, state6m.worksheet?.revision ?? 0, out _, out string refuse6m) == Level3WorksheetCheck.CheckOutcome.Refused &&
          Snap(state6m) == before6m && !string.IsNullOrEmpty(refuse6m), $"T06: checking {what6m} is refused and changes nothing");
}
Level3ChapterCheckpoint stale6m = base6m.Copy();
File6m(stale6m, "R1", "income"); File6m(stale6m, "R2", "income");
string staleSnap6m = Snap(stale6m);
Check(Level3WorksheetCheck.CheckBatch(stale6m, bea, "earnings", stale6m.worksheet.revision - 1, out _, out string staleWhy6m) == Level3WorksheetCheck.CheckOutcome.Refused &&
      Snap(stale6m) == staleSnap6m && staleWhy6m == Level3WorksheetCheck.RefuseBatchStale,
    "T06: a check made on an older draft revision (a late or repeated callback) is refused before anything freezes");

// ---------- pass 1c: first outcomes freeze once per batch; corrections never improve them ----------
Level3ChapterCheckpoint m6 = base6m.Copy();
File6m(m6, "R1", "income"); File6m(m6, "R2", "deduct");
int rev6m = m6.worksheet.revision;
Check(Check6m(m6, "earnings", out Level3BatchEvaluation e6m) == Level3WorksheetCheck.CheckOutcome.Checked && e6m.Froze && !e6m.Accepted && !e6m.Ready &&
      m6.worksheetCheck.assessmentVersion == "L3-worksheet-v2" && m6.worksheetCheck.checkedBatches.SequenceEqual(new[] { "earnings" }) &&
      m6.worksheetCheck.firstWrong.SequenceEqual(new[] { "dest-R2" }) && m6.worksheetCheck.firstCorrect == 1 && m6.worksheetCheck.firstTotal == 2 &&
      !m6.worksheetCheck.firstChecked && m6.worksheetCheck.firstRevision == rev6m && m6.worksheetCheck.acceptedBatches.Count == 0 &&
      m6.milestones.Contains(Level3WorksheetCheck.CheckedMilestone) && m6.phase == "WorksheetDraft" && Level3WorksheetRules.OpenBatch(m6) == "earnings" &&
      m6.worksheet.revision == rev6m,
    "T06: the first complete earnings check freezes 1/2 (dest-R2 wrong) once, accepts nothing, keeps the draft and its revision; the worksheet stays a draft");
Check(Level3WorksheetCheck.BatchMark(m6, "dest-R1") == Level3WorksheetCheck.Mark.Confirmed && Level3WorksheetCheck.BatchMark(m6, "dest-R2") == Level3WorksheetCheck.Mark.LookAgain &&
      Level3WorksheetCheck.RowMark(m6, "R2") == Level3WorksheetCheck.Mark.LookAgain && Level3WorksheetCheck.LookAgainIn(m6, "earnings").SequenceEqual(new[] { "dest-R2" }) &&
      Level3WorksheetCheck.BatchMark(m6, "dest-R3") == Level3WorksheetCheck.Mark.None && Level3WorksheetCheck.BatchMark(base6m, "dest-R1") == Level3WorksheetCheck.Mark.None,
    "T06: after the saved check its records are marked (right / look again); other batches and unchecked drafts carry no mark");
Level3ChapterCheckpoint mistake6m = m6.Copy();
string m6Snap = Snap(m6);
Check(Check6m(m6, "earnings", out _) == Level3WorksheetCheck.CheckOutcome.Unchanged && Snap(m6) == m6Snap,
    "T06: Check batch again with nothing refiled says so and saves nothing");
Check(File6m(m6, "R2", "deduct") == Level3CommandOutcome.Duplicate && Level3WorksheetCheck.RowMark(m6, "R2") == Level3WorksheetCheck.Mark.LookAgain,
    "T06: refiling the same choice is a duplicate; its mark stays");
Check(File6m(m6, "R2", "leave-out") == Level3CommandOutcome.Applied && Level3WorksheetCheck.RowMark(m6, "R2") == Level3WorksheetCheck.Mark.None &&
      Level3WorksheetCheck.RowMark(m6, "R1") == Level3WorksheetCheck.Mark.Confirmed && First6m(m6) == First6m(Clone(m6)),
    "T06: a refile is neutral: the recalled record's mark simply clears (no correctness cue until a deliberate recheck)");
string frozen6m = First6m(m6);
Check(Check6m(m6, "earnings", out Level3BatchEvaluation e6b) == Level3WorksheetCheck.CheckOutcome.Checked && !e6b.Froze && !e6b.Accepted &&
      First6m(m6) == frozen6m && Level3WorksheetCheck.RowMark(m6, "R2") == Level3WorksheetCheck.Mark.LookAgain,
    "T06: a recheck with a different wrong choice marks it again; the frozen first outcome is unchanged");
File6m(m6, "R2", "income");
Check(Check6m(m6, "earnings", out Level3BatchEvaluation e6c) == Level3WorksheetCheck.CheckOutcome.Checked && e6c.AllCorrect && e6c.Accepted && !e6c.Froze && !e6c.Ready &&
      First6m(m6) == frozen6m && m6.worksheetCheck.acceptedBatches.SequenceEqual(new[] { "earnings" }) && Level3WorksheetRules.OpenBatch(m6) == "costs" &&
      Level3WorksheetRules.BatchAccepted(m6, "earnings") && m6.worksheetCheck.firstCorrect == 1,
    "T06: corrected and rechecked: the batch is accepted and the costs open; the first result stays 1/2 (corrections never improve it)");
Check(File6m(m6, "R1", "deduct") == Level3CommandOutcome.Refused && Level3WorksheetRules.SavedTreatment(m6.worksheet, "R1") == "income" &&
      Level3WorksheetCheck.CheckBatch(m6.Copy(), bea, "earnings", m6.worksheet.revision, out _, out _) == Level3WorksheetCheck.CheckOutcome.Refused,
    "T06: an accepted batch is read only (no refile, no second check); its records remain readable");
foreach (string row in Level3WorksheetRules.RowsInBatch("costs")) File6m(m6, row, "deduct");
Check(Check6m(m6, "costs", out Level3BatchEvaluation e6d) == Level3WorksheetCheck.CheckOutcome.Checked && e6d.Froze && e6d.Accepted &&
      m6.worksheetCheck.firstCorrect == 4 && m6.worksheetCheck.firstTotal == 5 && !m6.worksheetCheck.firstChecked && Level3WorksheetRules.OpenBatch(m6) == "questions",
    "T06: an all-right first check freezes 3/3 and accepts the costs in the same saved command");
File6m(m6, "R6", "leave-out", "business-purpose"); File6m(m6, "R7", "leave-out", "already-represented");
Check(Check6m(m6, "questions", out Level3BatchEvaluation e6e) == Level3WorksheetCheck.CheckOutcome.Checked && e6e.Froze && !e6e.Accepted &&
      Level3WorksheetCheck.BatchMark(m6, "dest-R6") == Level3WorksheetCheck.Mark.Confirmed && Level3WorksheetCheck.BatchMark(m6, "reason-R6") == Level3WorksheetCheck.Mark.LookAgain &&
      Level3WorksheetCheck.RowMark(m6, "R7") == Level3WorksheetCheck.Mark.Confirmed && m6.worksheetCheck.firstChecked && m6.worksheetCheck.firstCorrect == 7 &&
      m6.worksheetCheck.firstTotal == 9 && m6.worksheetCheck.firstTier == 2 && m6.worksheetCheck.firstXp == 58 &&
      m6.worksheetCheck.firstWrong.SequenceEqual(new[] { "dest-R2", "reason-R6" }) && m6.phase == "WorksheetDraft",
    "T06: right treatment, wrong reason is marked on the reason alone; the third first check completes the one result: 7/9 Silver, 58 XP");
string final6m = First6m(m6);
File6m(m6, "R6", "leave-out", "personal-purpose");
Check(Check6m(m6, "questions", out Level3BatchEvaluation e6f) == Level3WorksheetCheck.CheckOutcome.Checked && e6f.Accepted && e6f.Ready && !e6f.Froze &&
      m6.phase == "WorksheetReady" && m6.actionIds.Contains(Level3WorksheetCheck.ReadyAction) && Level3WorksheetRules.OpenBatch(m6) == null &&
      First6m(m6) == final6m && Level3WorksheetCheck.AllBatchesCorrect(m6, bea) && Level3ChapterRules.Problem(m6) == null,
    "T06: the last accepted batch makes the worksheet ready (Draft -> WorksheetReady); the frozen 7/9 Silver 58 stays");
Check(Level3WorksheetCheck.Check(m6.Copy(), bea, out _, out _) == Level3WorksheetCheck.CheckOutcome.Refused &&
      Level3WorksheetCheck.Check(Mutate6m(m6, c => c.phase = "WorksheetDraft"), bea, out _, out string boardWhy6m) == Level3WorksheetCheck.CheckOutcome.Refused &&
      boardWhy6m.Contains("machine"),
    "T06: once the machine's checks began, the retired twelve-decision board can never grade the worksheet");
var runs6m = new[]
{
    (new Dictionary<string, (string, string)>(), 9, 3, 60),
    (new Dictionary<string, (string, string)> { ["R5"] = ("leave-out", "") }, 8, 2, 59),
    (new Dictionary<string, (string, string)> { ["R6"] = ("deduct", "business-purpose"), ["R7"] = ("deduct", "business-purpose") }, 5, 1, 56),
    (new Dictionary<string, (string, string)> { ["R1"] = ("deduct", ""), ["R2"] = ("leave-out", ""), ["R3"] = ("income", ""), ["R4"] = ("income", ""), ["R5"] = ("income", ""),
        ["R6"] = ("income", "need-support"), ["R7"] = ("deduct", "different-period") }, 0, 1, 51)
};
foreach (var (picks6m, correct6m, tier6m, xp6m) in runs6m)
{
    Level3ChapterCheckpoint run6m = Run6m(picks6m);
    Check(run6m != null && run6m.phase == "WorksheetReady" && run6m.worksheetCheck.firstCorrect == correct6m && run6m.worksheetCheck.firstTotal == 9 &&
          run6m.worksheetCheck.firstTier == tier6m && run6m.worksheetCheck.firstXp == xp6m && run6m.worksheetCheck.firstWrong.Count == 9 - correct6m &&
          Level3WorksheetCheck.AllBatchesCorrect(run6m, bea),
        $"T06: a full machine run with {9 - correct6m} first-check mistakes reaches Ready after corrections; frozen {correct6m}/9, tier {tier6m}, {xp6m} XP (the gate is correctness, not the medal)");
}

// ---------- pass 1d: legacy grades are never regraded ----------
Level3ChapterRules.TryContinueLegacy(LegacyOf(frCorrecting), out Level3ChapterCheckpoint leg6m, out _);
string legFirst6m = $"{leg6m.worksheetCheck.firstCorrect}/{leg6m.worksheetCheck.firstTotal}/{leg6m.worksheetCheck.firstTier}/{leg6m.worksheetCheck.firstXp}/{string.Join(",", leg6m.worksheetCheck.firstWrong)}";
foreach (string batch in Level3WorksheetRules.Batches)
{
    foreach (string row in Level3WorksheetRules.RowsInBatch(batch))
        if (Level3WorksheetRules.SavedTreatment(leg6m.worksheet, row) != Right6m(row) ||
            (Level3WorksheetRules.NeedsReason(row) && Level3WorksheetRules.SavedReason(leg6m.worksheet, row) != Reason6m(row)))
            File6m(leg6m, row, Right6m(row));
    Check(Check6m(leg6m, batch, out Level3BatchEvaluation le6m) == Level3WorksheetCheck.CheckOutcome.Checked && le6m.Accepted && !le6m.Froze,
        $"T06 legacy: a twelve-decision frozen chapter checks and accepts its {batch} through the machine without freezing a second grade");
}
Check(leg6m.phase == "WorksheetReady" && leg6m.worksheetCheck.assessmentVersion == "L3-worksheet-v1" && leg6m.worksheetCheck.checkedBatches.Count == 0 &&
      $"{leg6m.worksheetCheck.firstCorrect}/{leg6m.worksheetCheck.firstTotal}/{leg6m.worksheetCheck.firstTier}/{leg6m.worksheetCheck.firstXp}/{string.Join(",", leg6m.worksheetCheck.firstWrong)}" == legFirst6m &&
      leg6m.worksheetCheck.firstTotal == 12 && Level3WorksheetCheck.LegacyGraded(leg6m) && Level3WorksheetCheck.Acquire(leg6m.Copy(), bea, out _) == Level3CommandOutcome.Applied,
    "T06 legacy: corrected through mapped rows to Ready (Corrections -> Ready), the original 11/12 Silver and XP and v1 label are kept and it can be collected");
Level3ChapterRules.TryContinueLegacy(LegacyOf(wsWrong), out Level3ChapterCheckpoint legDraft6m, out _);
string legDraftBatch6m = Level3WorksheetRules.OpenBatch(legDraft6m);
foreach (string row in Level3WorksheetRules.RowsInBatch(legDraftBatch6m))
    if (!Level3WorksheetRules.RowFiled(legDraft6m.worksheet, row)) File6m(legDraft6m, row, Right6m(row));
Check(!Level3WorksheetCheck.LegacyGraded(legDraft6m) && legDraft6m.worksheetCheck.assessmentVersion == "" &&
      Check6m(legDraft6m, legDraftBatch6m, out Level3BatchEvaluation ld6m) == Level3WorksheetCheck.CheckOutcome.Checked && ld6m.Froze &&
      legDraft6m.worksheetCheck.assessmentVersion == "L3-worksheet-v2",
    "T06 legacy: an unchecked pre-revision draft keeps its choices and gets its first (v2) grade only from a deliberate machine check");
Level3ChapterCheckpoint legacyCollected6m = acq.Copy();
Check(Level3WorksheetCheck.BoardPage(legacyCollected6m) && Snap(Level3WorksheetCheck.ReferenceLines(legacyCollected6m, bea)) == Snap(Level3WorksheetCheck.ReferenceLines(legacyCollected6m, wsRows)) &&
      Level3WorksheetRules.OpenBatch(legacyCollected6m) == null && Level3WorksheetCheck.CheckBatch(legacyCollected6m.Copy(), bea, "earnings", legacyCollected6m.worksheet.revision, out _, out _) == Level3WorksheetCheck.CheckOutcome.Refused,
    "T06 legacy: a worksheet collected from the retired board keeps its page and skips the machine entirely (nothing to check)");

// ---------- pass 1e: collection, rewards once, preview grants nothing ----------
Level3ChapterCheckpoint ready6m = Run6m(new Dictionary<string, (string, string)> { ["R5"] = ("leave-out", "") });
Check(Level3WorksheetCheck.Acquire(Mutate6m(m6, c => c.phase = "WorksheetDraft"), bea, out _) == Level3CommandOutcome.Refused &&
      Level3WorksheetCheck.Acquire(Mutate6m(Accepted5m(base6m, "earnings", "costs"), c => c.phase = "WorksheetReady"), bea, out _) == Level3CommandOutcome.Refused,
    "T06: nothing can be collected before every batch is checked right (an unaccepted or wrong worksheet is refused)");
string preview6m = Snap(ready6m);
ClientRecord sum6m = Level3WorksheetCheck.SummaryRecord(bea, ready6m);
List<string> lines6m = Level3WorksheetCheck.ReferenceLines(ready6m, bea);
Check(Snap(ready6m) == preview6m && !Level3WorksheetCheck.Collected(ready6m) && ready6m.Case(B7).workIds.Count == 0,
    "T06: building and reading the summary (the ledge preview, its full paper, its page) changes nothing: no item, no grade, no collection");
Level3ChapterCheckpoint got6m = ready6m.Copy();
Check(Level3WorksheetCheck.Acquire(got6m, bea, out _) == Level3CommandOutcome.Applied && got6m.phase == "BeaComputation" &&
      got6m.Case(B7).workIds.SequenceEqual(new[] { Level3WorksheetCheck.WorksheetWorkId }) && Level3WorksheetCheck.Collected(got6m) &&
      Level3WorksheetCheck.Acquire(got6m.Copy(), bea, out _) == Level3CommandOutcome.Duplicate && got6m.worksheetCheck.firstCorrect == 8,
    "T06: Collect moves Ready -> BeaComputation with the existing B-WORKSHEET in Bea's case (once; a repeat is a duplicate)");
PlayerProgressData pay6m = Clone(live);
Level3WorksheetGrants g6m = Level3WorksheetCheck.ApplyAcquisitionGrants(pay6m, got6m);
Level3WorksheetGrants again6m = Level3WorksheetCheck.ApplyAcquisitionGrants(pay6m, got6m);
Check(g6m.Changed && g6m.Coins.Amount == 60 && g6m.Trust.Amount == 2 && g6m.Xp == 59 && g6m.Tier == 2 && g6m.MasteryClaimed &&
      pay6m.campaign.coins == 305 && pay6m.campaign.publicTrust == 65 && pay6m.badgeRecords.Any(b => b.badgeId == "worth_the_tax_3" && b.tier == 2) &&
      again6m.AlreadyClaimed && !again6m.Changed && pay6m.campaign.coins == 305 && pay6m.campaign.publicTrust == 65 && !pay6m.completedLevelIds.Contains("Level_03"),
    "T06: the collection grants 60 coins, +2 Trust, the mastery claim and the frozen Silver badge once (59 XP kept for the chapter); a repeat grants nothing");
Check(!Level3WorksheetCheck.ApplyAcquisitionGrants(new PlayerProgressData(), got6m).Changed && !Level3WorksheetCheck.ApplyAcquisitionGrants(Clone(live), ready6m).Changed,
    "T06: no campaign (replay/practice/Editor) or no collection: no grant");

// ---------- pass 1f: the summary, its page, the office and the folder ----------
Level3WorksheetCheck.SummaryTotals(got6m, out decimal inc6, out decimal ded6);
Check(inc6 == 900000m && ded6 == 300000m && Level3WorksheetCheck.Peso(inc6) == "₱900,000.00" && Level3WorksheetCheck.Peso(ded6) == "₱300,000.00",
    "T06: the summary's totals are exact decimals of the corrected slips: Income ₱900,000.00, Supported deductions ₱300,000.00");
Check(sum6m != null && sum6m.source == "B-WORKSHEET" && sum6m.type == ClientRecord.Plain && sum6m.HasBack &&
      sum6m.rows.Select(r => r.label + " " + r.value).SequenceEqual(new[] { "Income ₱900,000.00", "Supported deductions ₱300,000.00" }) &&
      sum6m.backRows.Select(r => r.label + " | " + r.value).SequenceEqual(new[]
      {
          "Income · REV-B26 | Logo fees ₱500,000.00", "Income · REV-B26 | Menu illustration fees ₱400,000.00",
          "Deduction · EXP-B26 · RENT-26 | Studio rent ₱180,000.00", "Deduction · EXP-B26 · SUB-26 | Design subscriptions ₱90,000.00",
          "Deduction · EXP-B26 · PRINT-26 | Printing and supplies ₱30,000.00",
          "Left out · Personal purpose | Birthday dinner, decor ₱24,000.00", "Left out · Already represented | Printing bill, paid ₱30,000.00"
      }),
    "T06: the summary paper: front = the two exact totals; back = each entry with its own and supporting originals, and the personal/duplicate exclusions with their reasons");
string sumText6m = Snap(sum6m) + string.Join("|", lines6m);
Check(!new[] { "600,000", "62,500", "12,500", "50,000", "taxable", "Tax due", "credit", "balance", "WHT" }.Any(sumText6m.Contains),
    "T06: neither the summary nor its page prints an office answer (taxable income, tax due, credit or balance)");
Check(lines6m.Take(2).SequenceEqual(new[] { "Income: ₱900,000.00", "Supported deductions: ₱300,000.00" }) &&
      lines6m.Contains("Studio rent ₱180,000.00 · EXP-B26 · RENT-26") && lines6m.Contains("Left out: Birthday dinner, decor ₱24,000.00 · Personal purpose") &&
      lines6m.Last() == "First checks: 8/9 (Silver)" && !Level3WorksheetCheck.BoardPage(got6m),
    "T06: the Bag's read-only page reads the same summary (totals, linked entries, exclusions, first-check result)");
List<Level3TotalChoice> office6m = Level3ComputationRules.TotalChoices(got6m, bea);
Check(office6m.Select(c => c.ChoiceId + "=" + c.Amount).SequenceEqual(new[] { "worksheet:income=900000", "worksheet:expenses=300000", "paper:B-DRAFT/BDraftTotal=354000.00" }) &&
      Level3ComputationRules.TotalChoices(ready6m, bea).Count == 0,
    "T06 -> T07: the office offers the collected summary's exact totals (no tape needed), Bea's draft as the plausible wrong one; nothing before collection");
Level3ChapterCheckpoint officeUse6m = got6m.Copy();
Check(Level3ComputationRules.SelectTotal(officeUse6m, bea, "gross", "worksheet:income", out _) == Level3CommandOutcome.Applied &&
      Level3ComputationRules.SelectTotal(officeUse6m, bea, "expenses", "worksheet:expenses", out _) == Level3CommandOutcome.Applied &&
      Level3ComputationRules.Evaluate(officeUse6m, bea, B7).Steps.Take(2).All(f => f.Present && f.Follows && f.Supported),
    "T06 -> T07: the summary's totals chosen at the office follow and are supported (exact, source-backed)");
Check(Level3ComputationRules.TotalChoices(acq, bea).Take(2).Select(c => c.Amount).SequenceEqual(new[] { "900000", "300000" }),
    "T06 -> T07: a legacy collected worksheet offers the same exact totals (its tapes added the same slips)");
List<ClientRecord> folderBefore6m = Level3CaseFolder.SupportingRecords(bea, ready6m), folderAfter6m = Level3CaseFolder.SupportingRecords(bea, got6m);
Check(folderBefore6m.All(r => r.source != "B-WORKSHEET") && folderAfter6m.Count(r => r.source == "B-WORKSHEET") == 1 &&
      folderAfter6m.Count == folderBefore6m.Count + 1,
    "T06 revision: the separate summary never duplicates a physical paper in Supporting Documents");
Check(Level3RecordRules.Build(bea, got6m.Case(B7).acquiredSourceIds).All(r => r.source != "B-WORKSHEET"),
    "T06: the summary is the player's own work, never an acquired original");

// ---------- pass 1g: the signed save: failed, duplicate and reloaded checks/collection ----------
string sandbox6m = Path.Combine(Path.GetTempPath(), "TaxVentureL3T06m-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox6m);
try
{
    string Encode6m(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    PlayerProgressData Decode6m(string text)
    {
        using JsonDocument doc = JsonDocument.Parse(text);
        string payload = doc.RootElement.GetProperty("payload").GetString();
        return SaveFileStore.SignatureMatches(payload, doc.RootElement.GetProperty("signature").GetString()) ? JsonSerializer.Deserialize<PlayerProgressData>(payload, json) : null;
    }
    // Mirrors Level3Checkpointer.CheckBatch -> CommitLevelThreeCheckpoint -> TryWriteCandidate: candidate, write, then publish.
    string CheckSave6m(SaveFileStore st, ref PlayerProgressData pub, string batch, out Level3BatchEvaluation shown)
    {
        shown = null;
        Level3ChapterCheckpoint candidate = pub.campaign.levelThree.Copy();
        var outcome = Level3WorksheetCheck.CheckBatch(candidate, bea, batch, candidate.worksheet.revision, out Level3BatchEvaluation eval, out _);
        if (outcome != Level3WorksheetCheck.CheckOutcome.Checked) return outcome.ToString();
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub), candidate);
        try { next.Normalize(); st.Write(Encode6m(next)); }
        catch (Exception) { return "SaveFailed"; }
        pub = next;
        shown = eval;
        return "Checked";
    }
    bool Collect6m(SaveFileStore st, ref PlayerProgressData pub, out Level3WorksheetGrants granted)
    {
        granted = default;
        Level3ChapterCheckpoint candidate = pub.campaign.levelThree.Copy();
        if (Level3WorksheetCheck.Acquire(candidate, bea, out _) != Level3CommandOutcome.Applied) return false;
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub), candidate);
        if (!Level3WorksheetCheck.AcquireSummary(next.campaign.inventory, InventoryCatalogue.BuiltIn(), candidate, SlotRef.Bag(2), bea).Ok) return false;
        Level3WorksheetGrants due = Level3WorksheetCheck.ApplyAcquisitionGrants(next, candidate);
        try { next.Normalize(); st.Write(Encode6m(next)); }
        catch (Exception) { return false; }
        pub = next;
        granted = due;
        return true;
    }
    var st6m = new SaveFileStore(Path.Combine(sandbox6m, "progress.json"));
    Level3ChapterCheckpoint filed6m = base6m.Copy();
    File6m(filed6m, "R1", "income"); File6m(filed6m, "R2", "deduct");
    PlayerProgressData pub6m = Level3ChapterRules.WithCheckpoint(Clone(live), filed6m);
    pub6m.Normalize();
    st6m.Write(Encode6m(pub6m));
    byte[] disk6m = File.ReadAllBytes(st6m.FilePath);
    string pubSnap6m = Snap(pub6m);
    Directory.CreateDirectory(st6m.TempPath); // injected write failure
    Check(CheckSave6m(st6m, ref pub6m, "earnings", out Level3BatchEvaluation failedShown6m) == "SaveFailed" && failedShown6m == null && Snap(pub6m) == pubSnap6m &&
          File.ReadAllBytes(st6m.FilePath).SequenceEqual(disk6m) && pub6m.campaign.levelThree.worksheetCheck.checkedBatches.Count == 0,
        "T06 save: a failed check write freezes nothing in memory or on disk and returns no marks to show");
    Directory.Delete(st6m.TempPath);
    Check(CheckSave6m(st6m, ref pub6m, "earnings", out Level3BatchEvaluation shown6m) == "Checked" && shown6m != null && !shown6m.Accepted &&
          Decode6m(File.ReadAllText(st6m.FilePath)).campaign.levelThree.worksheetCheck.firstWrong.SequenceEqual(new[] { "dest-R2" }),
        "T06 save: the retry freezes the same first outcome and only then shows its marks");
    byte[] once6m = File.ReadAllBytes(st6m.FilePath);
    Check(CheckSave6m(st6m, ref pub6m, "earnings", out _) == "Unchanged" && File.ReadAllBytes(st6m.FilePath).SequenceEqual(once6m),
        "T06 save: a duplicate check callback writes nothing");
    Level3ChapterCheckpoint fix6m = pub6m.campaign.levelThree.Copy();
    File6m(fix6m, "R2", "income");
    pub6m = Level3ChapterRules.WithCheckpoint(Clone(pub6m), fix6m); pub6m.Normalize(); st6m.Write(Encode6m(pub6m));
    Check(CheckSave6m(st6m, ref pub6m, "earnings", out Level3BatchEvaluation acc6m) == "Checked" && acc6m.Accepted &&
          CheckSave6m(st6m, ref pub6m, "earnings", out _) == "Refused",
        "T06 save: the corrected recheck accepts the batch once; a repeated acceptance is refused (no second write)");
    foreach (string batch in new[] { "costs", "questions" })
    {
        Level3ChapterCheckpoint more6m = pub6m.campaign.levelThree.Copy();
        foreach (string row in Level3WorksheetRules.RowsInBatch(batch)) File6m(more6m, row, Right6m(row));
        pub6m = Level3ChapterRules.WithCheckpoint(Clone(pub6m), more6m); pub6m.Normalize(); st6m.Write(Encode6m(pub6m));
        CheckSave6m(st6m, ref pub6m, batch, out _);
    }
    PlayerProgressData readyDisk6m = Decode6m(File.ReadAllText(st6m.FilePath));
    Check(readyDisk6m != null && !readyDisk6m.Normalize() && readyDisk6m.campaign.levelThree.phase == "WorksheetReady" &&
          Level3ChapterRules.Decide(readyDisk6m.campaign.levelThree) == Level3EntryDecision.Resume && readyDisk6m.campaign.levelThree.worksheetCheck.firstCorrect == 8 &&
          readyDisk6m.campaign.levelThree.worksheetCheck.acceptedBatches.SequenceEqual(Level3WorksheetRules.Batches) && WithoutChapter(readyDisk6m) == liveWithout,
        "T06 save: a reload at Ready resumes with three accepted batches and the frozen 8/9; possessions, coins and trust untouched until collection");
    Check(!new[] { "\"expected\"", "worksheetDecisions", "Personal purpose", "Already represented", "Allowable expenses\"", "₱" }.Any(Snap(readyDisk6m.campaign.levelThree).Contains),
        "T06 save: no answer key, label or printed amount text enters the saved check");
    string beforeCollect6m = Snap(pub6m);
    byte[] diskReady6m = File.ReadAllBytes(st6m.FilePath);
    Directory.CreateDirectory(st6m.TempPath);
    Check(!Collect6m(st6m, ref pub6m, out Level3WorksheetGrants failGrant6m) && Snap(pub6m) == beforeCollect6m && failGrant6m.Coins.Amount == 0 &&
          File.ReadAllBytes(st6m.FilePath).SequenceEqual(diskReady6m) && pub6m.campaign.coins == 245 && pub6m.campaign.publicTrust == 63 &&
          !pub6m.campaign.rewardClaims.Any(c => c.Contains("Level_03")) && pub6m.campaign.levelThree.phase == "WorksheetReady",
        "T06 save: a failed collection write grants nothing (no item, coins, trust, claim) and the summary stays retryable at Ready");
    Directory.Delete(st6m.TempPath);
    Check(Collect6m(st6m, ref pub6m, out Level3WorksheetGrants got6) && got6.Coins.Amount == 60 && got6.Trust.Amount == 2 && got6.Xp == 59 && got6.Tier == 2 &&
          !Collect6m(st6m, ref pub6m, out _) && pub6m.campaign.coins == 305 && pub6m.campaign.publicTrust == 65,
        "T06 save: the retry collects once (60 coins, +2 Trust, Silver 59); a duplicate collect callback is refused before any write");
    PlayerProgressData collectedDisk6m = Decode6m(File.ReadAllText(st6m.FilePath));
    Check(collectedDisk6m != null && !collectedDisk6m.Normalize() && Level3WorksheetCheck.Collected(collectedDisk6m.campaign.levelThree) &&
          collectedDisk6m.campaign.levelThree.phase == "BeaComputation" && collectedDisk6m.campaign.rewardClaims.Contains(Level3WorksheetCheck.MasteryClaimId(collectedDisk6m)) &&
          Level3ChapterRules.Decide(collectedDisk6m.campaign.levelThree) == Level3EntryDecision.Resume &&
          Level3WorksheetCheck.ApplyAcquisitionGrants(collectedDisk6m, collectedDisk6m.campaign.levelThree).AlreadyClaimed &&
          collectedDisk6m.campaign.inventory.hotbar[0].itemId == "notes" && collectedDisk6m.campaign.inventory.bag[0].itemId == "review_voucher" &&
          collectedDisk6m.campaign.inventory.bag[0].quantity == 2 && collectedDisk6m.campaign.inventory.bag[2].itemId == InventoryCatalogue.BeaSummary &&
          InventoryRules.Record(collectedDisk6m.campaign.inventory, InventoryCatalogue.BeaSummary).referencePages.Count > 0 &&
          collectedDisk6m.campaign.ownedUpgradeIds.SequenceEqual(live.campaign.ownedUpgradeIds),
        "T06 save: after a reload the collected chapter resumes at Bea's computation without a regrant or regrade; every campaign possession is kept");
}
finally
{
    try { Directory.Delete(sandbox6m, true); } catch (IOException) { }
}

// ---------- pass 2: resume, lessons, objectives, hints, fixtures ----------
Check(Level3ChapterRules.ImpliedTriggers(ready6m).Contains(Level3ChapterRules.WorksheetReadyTrigger) &&
      !Level3ChapterRules.ImpliedTriggers(mistake6m).Contains(Level3ChapterRules.WorksheetReadyTrigger) &&
      Level3ChapterRules.ImpliedTriggers(got6m).Contains(Level3ChapterRules.WorksheetCollectedTrigger),
    "T06 resume: Ready ticks the check objective, a collected summary both; a draft between batches neither");
Level3ChapterCheckpoint firstRight6m = base6m.Copy();
File6m(firstRight6m, "R1", "income"); File6m(firstRight6m, "R2", "income"); Check6m(firstRight6m, "earnings", out _);
Check(Level3ChapterRules.ImpliedLessons(firstRight6m).Contains(Level3ChapterRules.BatchFiledLesson) && Level3ChapterRules.ImpliedLessons(firstRight6m).Contains(Level3ChapterRules.CorrectionsLesson) &&
      !Level3ChapterRules.ImpliedLessons(firstRight6m).Contains(Level3ChapterRules.CheckLesson) &&
      Level3ChapterRules.ImpliedLessons(mistake6m).Contains(Level3ChapterRules.CheckLesson) && !Level3ChapterRules.ImpliedLessons(mistake6m).Contains(Level3ChapterRules.CorrectionsLesson) &&
      !Level3ChapterRules.ImpliedLessons(base6m).Contains(Level3ChapterRules.BatchFiledLesson) && !Level3ChapterRules.ImpliedLessons(base6m).Contains(Level3ChapterRules.SubtotalsLesson),
    "T06 resume: a saved check has taught Check batch; an acceptance the read-only lesson; look again only once a check found a mistake; a fresh draft keeps them all");
GuideScript guide6m = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var L6m = (Func<string, GuideLesson>)(id => guide6m.lessons.First(l => l.id == id));
var t06mLessons = new[] { "machine-batch-filed", Level3ChapterRules.CheckLesson, Level3ChapterRules.CorrectionsLesson, Level3ChapterRules.ReadyLesson, Level3ChapterRules.CollectedLesson }
    .Select(L6m).ToList();
Check(guide6m.Validate() == "" && t06mLessons.All(l => l.pages.All(p => p.IsHint || !string.IsNullOrEmpty(p.waitFor))) &&
      L6m("machine-batch-filed").pages[0].waitFor == "cue:MachineBatchChecked|cue:MachineBatchAccepted" && L6m("machine-batch-filed").pages[0].text.Contains("CHECK BATCH") &&
      L6m(Level3ChapterRules.CheckLesson).when == "flag:machineLookAgain" && L6m(Level3ChapterRules.CheckLesson).pages[0].waitFor == "cue:MachineRecalled" &&
      L6m(Level3ChapterRules.CorrectionsLesson).when == "flag:machineAccepted" && L6m(Level3ChapterRules.ReadyLesson).pages.Last().waitFor == "flag:worksheetCollected",
    "T06 guide: Check batch, look again, accepted, summary and collected lessons follow the player's own saved actions; tape lessons are legacy-only");
Check(!t06mLessons.SelectMany(l => l.pages).Any(p => new[] { "Personal purpose", "Already represented", "Leave Out", "Deduct", "Income ₱", "900,000", "300,000", "click" }
          .Any(w => p.text.Contains(w, StringComparison.OrdinalIgnoreCase))) &&
      t06mLessons.SelectMany(l => l.pages).All(p => p.text.Split(' ').Length <= 14),
    "T06 guide: Arthur names no treatment, reason or total, speaks touch only and stays within the 14-word instruction budget");
JsonElement objectives6m = Json("Levels/Level_03/objectives.json").GetProperty("objectives")[0].GetProperty("steps");
Check(objectives6m[3].GetProperty("text").GetString() == "Check each batch at the machine and fix anything marked." && objectives6m[3].GetProperty("trigger").GetString() == "event:WorksheetReady" &&
      objectives6m[4].GetProperty("text").GetString() == "Collect Bea's Income & Expense Summary." && objectives6m[4].GetProperty("trigger").GetString() == "event:WorksheetCollected",
    "T06 objectives: check each batch, then collect the summary (triggers unchanged, saved transitions only)");
Level3HintFile hintsJson6m = JsonSerializer.Deserialize<Level3HintFile>(File.ReadAllText(Res("Levels/Level_03/worksheet.json")), json);
Level3HintFile hintsBuilt6m = Level3WorksheetHints.BuiltIn();
Check(Level3WorksheetHints.Validate(hintsJson6m).Count == 0 && Level3WorksheetHints.Validate(hintsBuilt6m).Count == 0 &&
      Level3WorksheetCheck.RevisedDecisionIds.All(id => Level3WorksheetHints.LookAgain(hintsJson6m, id, false) == Level3WorksheetHints.LookAgain(hintsBuilt6m, id, false) &&
          Level3WorksheetHints.LookAgain(hintsJson6m, id, false).Length > 0 && Level3WorksheetHints.LookAgain(hintsJson6m, id, false) != Level3WorksheetHints.For(hintsJson6m, id, 3) &&
          Level3WorksheetHints.LookAgain(hintsJson6m, id, true) == Level3WorksheetHints.For(hintsJson6m, id, 2) &&
          Level3WorksheetHints.LookAgain(hintsJson6m, id, false).Split(' ').Length <= 14),
    "T06 hints: every scored decision has a brief look-again line (JSON = built-in), the idea after a repeat, never the answer line");
Check(!Level3WorksheetCheck.RevisedDecisionIds.Select(id => Level3WorksheetHints.LookAgain(hintsJson6m, id, false))
          .Any(h => new[] { "Personal purpose", "Already represented", "Leave Out", "Not deductible", "Supporting only", "goes in" }.Any(h.Contains)),
    "T06 hints: the brief lines point where to look and never name a treatment or reason");
Level3ChapterCheckpoint fixMistake6m = Level3EditorFixtures.EarningsMistake(bea), fixReady6m = Level3EditorFixtures.SummaryReady(bea),
    fixCosts6m = Level3EditorFixtures.MachineBatch(bea, "costs"), fixQuestions6m = Level3EditorFixtures.MachineBatch(bea, "questions");
Check(fixMistake6m != null && Level3WorksheetRules.BatchFiled(fixMistake6m, "earnings") && fixMistake6m.worksheetCheck.checks == 0 &&
      Level3WorksheetRules.SavedTreatment(fixMistake6m.worksheet, "R2") == "deduct" &&
      fixReady6m != null && fixReady6m.phase == "WorksheetReady" && !Level3WorksheetCheck.Collected(fixReady6m) && fixReady6m.worksheetCheck.firstCorrect == 9 &&
      fixCosts6m != null && fixCosts6m.worksheetCheck.checkedBatches.SequenceEqual(new[] { "earnings" }) && fixCosts6m.worksheetCheck.acceptedBatches.SequenceEqual(new[] { "earnings" }) &&
      fixQuestions6m != null && Level3WorksheetRules.OpenBatch(fixQuestions6m) == "questions" &&
      new[] { fixMistake6m, fixReady6m, fixCosts6m, fixQuestions6m }.All(f => Level3ChapterRules.Problem(f) == null) &&
      Level3EditorFixtures.Build(Level3EditorFixtures.BeaEarningsMistakeId, bea) != null && Level3EditorFixtures.Build(Level3EditorFixtures.BeaSummaryReadyId, bea) != null,
    "T06 fixtures (Editor only, never saved): an unchecked earnings mistake, a ready summary, and later batches accepted through the real Check batch (no stand-in)");

// ---------- standalone summary inventory: actual commands, capacity, snapshots and retries ----------
var summaryCatalogue = InventoryCatalogue.BuiltIn();
var summaryInv = new CampaignInventoryRecord();
string summaryBefore = Snap(summaryInv);
Check(!Level3WorksheetCheck.AcquireSummary(summaryInv, summaryCatalogue, ready6m, SlotRef.Hotbar(0), bea).Ok && Snap(summaryInv) == summaryBefore,
    "Summary: a ready preview is never inventory ownership before the chapter's Collect command");
InventoryResult summaryBag = Level3WorksheetCheck.AcquireSummary(summaryInv, summaryCatalogue, got6m, SlotRef.Bag(2), bea);
Check(summaryBag.Ok && summaryBag.Location == SlotRef.Bag(2) && summaryInv.hotbar.All(s => s.IsEmpty) &&
      InventoryRules.Record(summaryInv, InventoryCatalogue.BeaSummary).referencePages.SelectMany(p => p.lines).SequenceEqual(Level3WorksheetCheck.ReferenceLines(got6m, bea)),
    "Summary: dropping on Bag honors bag placement and retains every exact readable line at acquisition");
string summaryOnce = Snap(summaryInv);
Check(Level3WorksheetCheck.AcquireSummary(summaryInv, summaryCatalogue, got6m, SlotRef.Hotbar(0), bea).Ok && Snap(summaryInv) == summaryOnce,
    "Summary: repeated collection neither duplicates nor relocates the item");
Check(InventoryRules.Move(summaryInv, summaryCatalogue, SlotRef.Bag(2), SlotRef.Hotbar(0), "summary-test/move").Ok &&
      InventoryRules.Move(summaryInv, summaryCatalogue, SlotRef.Hotbar(0), SlotRef.Bag(2), "summary-test/return").Ok,
    "Summary: the same item moves between bag and hotbar through existing commands");
var fullSummaryInv = new CampaignInventoryRecord();
foreach (var slot in fullSummaryInv.bag) { slot.itemId = InventoryCatalogue.ReviewVoucher; slot.quantity = 2; }
foreach (var slot in fullSummaryInv.hotbar) { slot.itemId = InventoryCatalogue.Notes; slot.quantity = 1; }
string fullSummaryBefore = Snap(fullSummaryInv);
Check(!Level3WorksheetCheck.AcquireSummary(fullSummaryInv, summaryCatalogue, got6m, SlotRef.Bag(0), bea).Ok && Snap(fullSummaryInv) == fullSummaryBefore &&
      !Level3WorksheetCheck.AcquireSummary(fullSummaryInv, summaryCatalogue, got6m, SlotRef.Hotbar(0), bea).Ok && Snap(fullSummaryInv) == fullSummaryBefore &&
      !Level3WorksheetCheck.AcquireSummary(fullSummaryInv, summaryCatalogue, got6m, SlotRef.Hotbar(9), bea).Ok && Snap(fullSummaryInv) == fullSummaryBefore,
    "Summary: full bag, occupied bar with no displacement space and locked slot refuse without changing inventory");
var swapSummary = new CampaignInventoryRecord();
InventoryRules.Acquire(swapSummary, summaryCatalogue, InventoryCatalogue.Notes, 1, "summary-test/notes", SlotRef.Hotbar(0));
Check(Level3WorksheetCheck.AcquireSummary(swapSummary, summaryCatalogue, got6m, SlotRef.Hotbar(0), bea).Ok &&
      InventoryRules.Find(swapSummary, InventoryCatalogue.Notes) == SlotRef.Bag(0) && InventoryRules.Find(swapSummary, InventoryCatalogue.BeaSummary) == SlotRef.Hotbar(0),
    "Summary: an occupied hotbar drop uses the existing displacement into Bag");
var oldSummary = new CampaignInventoryRecord();
InventoryRules.Acquire(oldSummary, summaryCatalogue, InventoryCatalogue.CaseFolder, 1, "summary-test/folder");
var oldSummaryFolder = InventoryRules.Record(oldSummary, InventoryCatalogue.CaseFolder);
oldSummaryFolder.contents.Add(B7 + "/B-WORKSHEET");
oldSummaryFolder.referencePages.Add(new InventoryReferencePage { id = B7 + "/B-WORKSHEET/0", title = "Prior summary", lines = new List<string> { "kept checked work" } });
Check(Level3WorksheetCheck.AcquireSummary(oldSummary, summaryCatalogue, got6m).Ok && oldSummaryFolder.contents.Count == 0 && oldSummaryFolder.referencePages.Count == 0 &&
      InventoryRules.Record(oldSummary, InventoryCatalogue.BeaSummary).referencePages.Single().lines.Single() == "kept checked work",
    "Summary: quiet legacy upgrade moves the saved readable summary into its own item without duplicating folder pages");

// ---------- pass 2: wiring (saved first; marks, flights and rewards after) ----------
string cp6m = Src("Scripts/Level3/Level3Checkpointer.cs");
string Body6m(string src, string start, string end) { int at = Index(src, start); if (at < 0) return ""; int stop = Index(src.Substring(at), end); return src.Substring(at, stop < 0 ? src.Length - at : stop); }
string checkBody6m = Body6m(cp6m, "public CheckCommit CheckBatch(", "/// <summary>What collecting the worksheet did");
Check(Index(checkBody6m, "Level3WorksheetCheck.CheckBatch(candidate") > 0 && Index(checkBody6m, "return CheckCommit.Incomplete;") < Index(checkBody6m, "CommitLevelThreeCheckpoint(candidate)") &&
      Index(checkBody6m, "CommitLevelThreeCheckpoint(candidate)") < Index(checkBody6m, "Publish(candidate);") &&
      Body6m(checkBody6m, "if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))", "Publish(candidate);").Contains("evaluation = null;"),
    "T06 wiring: Check batch is candidate -> signed write -> publish; a failed write hands back no evaluation (no marks can show)");
string table6m = Src("Scripts/Level3/Level3BeaTable.cs");
string checkTable6m = Body6m(table6m, "private void CheckOpenBatch(", "private IEnumerator AcceptSequence(");
Check(Index(checkTable6m, "owner.CheckBatch(") > 0 && Index(checkTable6m, "owner.CheckBatch(") < Index(checkTable6m, "if (result != Level3Checkpointer.CheckCommit.Checked || evaluation == null)") &&
      Index(checkTable6m, "if (result != Level3Checkpointer.CheckCommit.Checked || evaluation == null)") < Index(checkTable6m, "marksPop = true;") &&
      Index(checkTable6m, "return;") < Index(checkTable6m, "AcceptSequence(") && !checkTable6m.Contains("Correct ==") && !checkTable6m.Contains("expected"),
    "T06 wiring: the table shows marks or accepts only after a saved check; anything else is one sentence and returns first");
string summaryView6m = Src("Scripts/Level3/Level3SummaryDocument.cs");
string summaryPaper6m = Src("Scripts/SideQuest1/ClientRecordPaper.cs");
Check(!summaryView6m.Contains("paper.ExtractionGhost") && !summaryView6m.Contains("private HudDragItem ghost;") &&
      summaryView6m.Contains("paper.DragLiftSeconds = 0.14f;") &&
      Src("Scripts/UI/HUD/HudHotbar.cs").Contains("HudDragItem.BuildSummaryPaper"),
    "Summary gestures: stand pickup uses the real printed sheet with a short lift; inventory keeps its existing ghost");
Check(summaryView6m.Contains("paper.TakeOutFromScreen(screen.Value);") &&
      summaryPaper6m.Contains("public void TakeOutFromScreen(Vector2 screen)") &&
      Body6m(summaryPaper6m, "public void TakeOutFromScreen(", "/// <summary>Back into").Contains("StartMotion(State.Moving,"),
    "Summary inventory release: the real paper starts at the screen handoff, never at the remote stand");

Check(Src("Scripts/UI/HUD/HudHotbar.cs").Contains("public Transform DocumentCanvas => canvasRoot;") &&
      summaryView6m.Contains("paper.WindowCanvas = HudHotbar.Instance.DocumentCanvas;") &&
      !summaryView6m.Contains("HudHotbar.Instance.GetComponentInParent<Canvas>()"),
    "Summary layering: resolve the HUD's bound canvas, not the separate runtime host's parents, so opening always joins Doc3DLayer");

string collectTable6m = Body6m(summaryView6m, "private bool Store(", "public void ReadingPose(");
Check(Index(collectTable6m, "CollectWorksheet(") > 0 && Index(collectTable6m, "CollectWorksheet(") < Index(collectTable6m, "pendingCollection = grants") &&
      Body6m(summaryView6m, "public void PaperLanded(", "public bool WantsReturn(").Contains("collected?.Invoke(grants)") &&
      Body6m(table6m, "private void LandCollect()", "private void TickCollect()").Contains("if (awardShown) return;"),
    "T06 revision: dropping into storage saves the item/chapter before once-only reward feedback");
string openSummary6m = Body6m(summaryView6m, "public bool Open(", "private bool showDetails;");
Check(openSummary6m.Contains("paper.TakeOut();") && !openSummary6m.Contains("Commit") && !openSummary6m.Contains("CollectWorksheet") && !openSummary6m.Contains("Grant"),
    "T06 revision: opening the real stand paper is a preview, without collection or rewards");
Check(!table6m.Contains("COLLECT SUMMARY") && Body6m(table6m, "private void RefreshSummary(", "private void OpenSummary()").Contains("Machine.HideSummary();") &&
      summaryView6m.Contains("ClientRecordPaper.Create(") && summaryView6m.Contains("HudWindowLock.Acquire(this)"),
    "T06 revision: one existing physical reader replaces the collect panel and ledge proxy");
string model6m = Src("Scripts/Level3/Level3PaperMachineModel.cs");
// The record card moves under FeedFrame and remains there after the final feed.
// A summary must use an independently anchored slot even when created in that state.
Check(model6m.Contains("public Transform StandDocumentAnchor") &&
      table6m.Contains("summaryDocument.Bind(this, Machine.StandDocumentAnchor,") &&
      summaryView6m.Contains("slot.SetParent(standAnchor, false);") &&
      !summaryView6m.Contains("standCard.parent"),
    "Summary stand regression: after the final card feeds into the machine, summary docking still uses the fixed stand anchor");

string pose6m = Body6m(model6m, "private void SetSummaryPose(float t)", "/// <summary>T06: the summary leaves the ledge at once");
Check(!pose6m.Contains("Scale") && Level3PaperMachineModel.OutputMouthWidth >= Level3PaperMachineModel.SummaryWidth && model6m.Contains("SpinFor(SummaryOutSeconds * 0.6f, 1f);") &&
      Level3PaperMachineModel.SummaryOutSeconds >= 0.3f && Level3PaperMachineModel.SummaryOutSeconds <= 0.45f && Level3PaperMachineModel.CollectFlySeconds <= 0.6f &&
      Level3PaperMachineModel.GatherSeconds <= 0.25f && Level3PaperMachineModel.MarkSeconds + Level3PaperMachineModel.MarkStagger * 2 <= 0.3f,
    "T06 motion: the summary leaves a mouth as wide as itself without shrinking (0.3-0.45 s, rollers turning); marks, gather and flight stay brief");
Check(model6m.Contains("bool any = cardMotion.Finish() | leverMotion.Finish() | dialMotion.Finish() | noteMotion.Finish() | whyMotion.Finish() |") &&
      model6m.Contains("gatherMotion.Finish() | summaryMotion.Finish();") && Body6m(table6m, "private void CancelDrags()", "// =====").Contains("FinishReview();") &&
      Body6m(table6m, "private void OnMachineInput(", "/// <summary>Half-feeds").Contains("if (collecting) return;"),
    "T06 lifecycle: an interruption settles the review, the summary's arrival and the flight to their saved state; no new action starts during a collection");
string refreshMachine6m = Body6m(table6m, "private void RefreshMachine(", "private static string Money(");
string accept6m = Body6m(table6m, "private IEnumerator AcceptSequence(", "/// <summary>Settles an accepted batch's presentation");
Check(Index(refreshMachine6m, "if (!reviewing)") > 0 && Index(refreshMachine6m, "if (!reviewing)") < Index(refreshMachine6m, "Machine.SetTabs(tabs, hidden, settleTab, animate);") &&
      Index(accept6m, "reviewing = true;") < Index(accept6m, "RefreshMachine(") && Index(accept6m, "Machine.GatherTabs(") < Index(accept6m, "reviewing = false;") &&
      Index(accept6m, "reviewing = false;") < Index(accept6m, "RefreshStand(animate: true);"),
    "T06 lifecycle: an accepted batch keeps its shown tabs for the ticks and the gather (the saved draft already opened the next batch), then the next record arrives");
Check(!table6m.Contains("sfx_classification") && !table6m.Contains("sfx_ui_unavailable") && !Body6m(table6m, "private void PullLever()", "private void FeedDone()").Contains("CheckBatch") && !Body6m(table6m, "private void PullLever()", "private void FeedDone()").Contains("Evaluate"),
    "T06 feel: no correctness buzz or filing-time grading cue; the lever still only files");
foreach (string id6 in new[] { "MachineBatchChecked", "MachineBatchAccepted", "MachineSummaryRead", "MachineRecalled" })
    Check(table6m.Contains("GameplayEvents.RaiseGuideCue(\"" + id6 + "\")"), $"T06 guide cue {id6} is raised by the table after its own saved action (or a recall)");
Check(cp6m.Contains("case \"machineLookAgain\":") && cp6m.Contains("case \"machineAccepted\":") && cp6m.Contains("case \"machineChecked\":") && cp6m.Contains("case \"machineSummary\":"),
    "T06 guide flags are read-only facts of the saved chapter or the table's view");
string hud6m = Src("Scripts/UI/HUD/HudHotbar.cs");
Check(hud6m.Contains("KeepReferencePages(pages, InventoryCatalogue.BeaSummary") && hud6m.Contains("Level3WorksheetCheck.ReferenceLines(chapter, definition)"),
    "T06 revision: the separate inventory summary keeps the same readable checked work");

// ---------- pass 3: layout and projected touch targets (shipped constants; Unity rendering not run) ----------
double batchUsed6m = Level3PaperMachineModel.CardTop + Level3PaperMachineModel.PositionHeight + Level3PaperMachineModel.TitleHeight + Level3PaperMachineModel.BatchStatusHeight +
    Level3PaperMachineModel.BatchBodyHeight + 3 * Level3PaperMachineModel.CardRowGap + 0.001 + Level3PaperMachineModel.CardRowGap;
double keyTop6m = Level3PaperMachineModel.BatchKeyHeight + 0.006;
Check(batchUsed6m + keyTop6m <= Level3PaperMachineModel.CardHeight && Level3PaperMachineModel.BatchKeyTouchHeight > Level3PaperMachineModel.BatchKeyHeight,
    "T06 layout: the batch card's rows, body and key fit the stand card without overlap (one card, one key)");
double lean6m = Level3PaperMachineModel.SummaryLean * Math.PI / 180;
double tentTopZ6m = Level3PaperMachineModel.SummaryFrontZ + Level3PaperMachineModel.SummaryHeight * Math.Cos(lean6m);
double rail6m = Level3TableStaging.RackDepth * 0.5 - 0.004;
double machineFrontInRack6m = (Level3TableStaging.RackDepth * 0.5) + Level3TableStaging.RackGap;
Check(Level3PaperMachineModel.SummaryFrontZ >= -Level3TableStaging.RackDepth * 0.5 && tentTopZ6m < rail6m && rail6m < machineFrontInRack6m &&
      Level3PaperMachineModel.SummaryX - Level3PaperMachineModel.SummaryWidth * 0.5 >= -Level3TableStaging.MachineWidth * 0.5 &&
      Level3PaperMachineModel.SummaryX + Level3PaperMachineModel.SummaryWidth * 0.5 <= Level3TableStaging.MachineWidth * 0.5 &&
      Level3PaperMachineModel.SummaryX - Level3PaperMachineModel.BodyOffsetX + Level3PaperMachineModel.OutputMouthWidth * 0.5 <= Level3PaperMachineModel.BodyWidth * 0.48,
    "T06 layout: the folded summary stands on the empty rack in front of the machine (front edge on the rack, back leg on its rail), inside the column; its mouth fits the plate");
{
    var mat = new UnityEngine.Vector2(1.2f, 0.72f);
    var lay = Level3TableStaging.Compute(mat, Level3TableStaging.SeatOf(mat, 0.4f));
    double sc = lay.Scale, top = Level3BeaTable.TableTop + 0.003;
    var focus = new double[] { 0, Level3BeaTable.TableTop, -mat.y * 0.08 };
    var seatTop = new double[] { lay.Seat.x, lay.Seat.y + Level3BeaTable.TableTop, lay.Seat.z };
    var cam = new double[3];
    for (int i = 0; i < 3; i++) cam[i] = focus[i] + (seatTop[i] - focus[i]) * 0.76;
    double[] Sub(double[] p, double[] q) => new[] { p[0] - q[0], p[1] - q[1], p[2] - q[2] };
    double Dot(double[] p, double[] q) => p[0] * q[0] + p[1] * q[1] + p[2] * q[2];
    double[] Norm(double[] p) { double l = Math.Sqrt(Dot(p, p)); return new[] { p[0] / l, p[1] / l, p[2] / l }; }
    double[] Cross(double[] p, double[] q) => new[] { p[1] * q[2] - p[2] * q[1], p[2] * q[0] - p[0] * q[2], p[0] * q[1] - p[1] * q[0] };
    double[] fwd = Norm(Sub(focus, cam)), right = Norm(Cross(new double[] { 0, 1, 0 }, fwd)), up = Cross(fwd, right);
    const double W = 2400, H = 1080, PxPerDp = 2.47;
    double f = H * 0.5 / Math.Tan(30 * Math.PI / 180);
    double[] Px(double[] p) { var d = Sub(p, cam); double z = Dot(d, fwd); return new[] { W * 0.5 + Dot(d, right) / z * f, H * 0.5 - Dot(d, up) / z * f }; }
    double[] Yaw(double deg, double[] p) { double a = deg * Math.PI / 180, c = Math.Cos(a), sn = Math.Sin(a); return new[] { p[0] * c + p[2] * sn, p[1], -p[0] * sn + p[2] * c }; }
    double[] Pitch(double deg, double[] p) { double a = deg * Math.PI / 180, c = Math.Cos(a), sn = Math.Sin(a); return new[] { p[0], p[1] * c - p[2] * sn, p[1] * sn + p[2] * c }; }
    double[] Add(double[] p, double[] q) => new[] { p[0] + q[0], p[1] + q[1], p[2] + q[2] };
    double[] Mul(double k, double[] p) => new[] { p[0] * k, p[1] * k, p[2] * k };
    double machineOffset = (lay.Machine.center.y - lay.Column.center.y) / sc, rackOffset = (lay.Rack.center.y - lay.Column.center.y) / sc;
    double[] Column(double[] local) => Add(new double[] { lay.Column.center.x, top, lay.Column.center.y }, Yaw(lay.MachineYaw, Mul(sc, local)));
    double[] Machine(double[] m) => Column(new[] { m[0], m[1], m[2] + machineOffset });
    double[] StandP(double[] local) => Add(new double[] { lay.Stand.center.x, top, lay.Stand.center.y }, Yaw(lay.StandYaw, Mul(sc, local)));
    double[] bottom = { Level3PaperMachineModel.BodyOffsetX, Level3PaperMachineModel.PlateBottomY, Level3PaperMachineModel.PlateBottomZ };
    double[] upper = { Level3PaperMachineModel.BodyOffsetX, Level3PaperMachineModel.TopY, Level3PaperMachineModel.BodyFrontZ };
    double[] pzv = Norm(Sub(upper, bottom)), pyv = Norm(new[] { 0, pzv[2], -pzv[1] }), pcen = Mul(0.5, Add(bottom, upper));
    double[] Plate(double x, double y, double z) => Machine(Add(pcen, Add(Mul(x, new double[] { 1, 0, 0 }), Add(Mul(y, pyv), Mul(z, pzv)))));
    double hingeZ = -Level3TableStaging.StandDepth * 0.5 + 0.03;
    double[] Card(double x, double z) => StandP(Add(new[] { 0, 0.012, hingeZ }, Pitch(-Level3PaperMachineModel.StandTilt, new[] { x, 0.0045, 0.012 + Level3PaperMachineModel.CardHeight * 0.5 + z })));
    double[] Review(double x, double z, int slot) => Column(Add(new[] { (slot - 1) * Level3TableStaging.RackWidth * 0.33, 0.013, rackOffset - Level3TableStaging.RackDepth * 0.5 + 0.004 },
        Pitch(-Level3PaperMachineModel.RackTabTilt, new[] { x, 0.002, z })));
    List<double[]> Quad(Func<double, double, double[]> at, double cx, double cz, double w, double h) =>
        new List<double[]> { Px(at(cx - w / 2, cz - h / 2)), Px(at(cx + w / 2, cz - h / 2)), Px(at(cx + w / 2, cz + h / 2)), Px(at(cx - w / 2, cz + h / 2)) };
    double ch = Level3PaperMachineModel.CardHeight, cw = Level3PaperMachineModel.CardWidth;
    var key = Quad(Card, 0, -ch * 0.5 + Level3PaperMachineModel.BatchKeyHeight * 0.5 + 0.006, cw - 0.008, Level3PaperMachineModel.BatchKeyTouchHeight);
    var windowIncome = Quad((x, z) => Plate(x, 0.009, z), -Level3PaperMachineModel.DetentSpacing, Level3PaperMachineModel.WindowTouchZ, Level3PaperMachineModel.WindowTouchWidth, Level3PaperMachineModel.WindowTouchHeight);
    var windowDeduct = Quad((x, z) => Plate(x, 0.009, z), Level3PaperMachineModel.DetentSpacing, Level3PaperMachineModel.WindowTouchZ, Level3PaperMachineModel.WindowTouchWidth, Level3PaperMachineModel.WindowTouchHeight);
    var summaryUnits = SupportingDocumentFace.SizeOf(ClientRecord.Plain);
    var standSummary = Quad(Card, 0, 0,
        Math.Min(Level3PaperMachineModel.CardWidth, Level3PaperMachineModel.CardHeight * summaryUnits.x / summaryUnits.y), Level3PaperMachineModel.CardHeight);
    var dial = Quad((x, z) => Plate(x, 0.01, z), 0, Level3PaperMachineModel.DialTouchZ, Level3PaperMachineModel.DialTouchWidth, Level3PaperMachineModel.DialTouchHeight);
    var keysRow = Quad((x, z) => Plate(x, 0.0135, z), 0, Level3PaperMachineModel.LabelRowZ, Level3PaperMachineModel.DetentSpacing * 2 + Level3PaperMachineModel.DialLabelWidth + 0.002, Level3PaperMachineModel.CategoryButtonHeight + 0.002);
    var states6 = new Dictionary<string, List<(string name, List<double[]> quad)>>
    {
        ["batch filed"] = new List<(string, List<double[]>)> { ("CHECK BATCH", key), ("INCOME window", windowIncome), ("DEDUCTIONS window", windowDeduct) },
        ["summary ready"] = new List<(string, List<double[]>)> { ("INCOME window", windowIncome), ("DEDUCTIONS window", windowDeduct), ("summary", standSummary) }
    };
    for (int slot = 0; slot < 3; slot++)
    {
        int captured = slot;
        states6["batch filed"].Add(($"tab {slot + 1}", Quad((x, z) => Review(x, z, captured), 0, Level3PaperMachineModel.RackTabDepth * 0.5,
            Level3PaperMachineModel.ReviewTouchWidth, Level3PaperMachineModel.ReviewTouchHeight)));
    }
    bool Overlap(List<double[]> a, List<double[]> b)
    {
        foreach (var poly in new[] { a, b })
            for (int i = 0; i < 4; i++)
            {
                double[] p = poly[i], q = poly[(i + 1) % 4];
                double nx = q[1] - p[1], ny = p[0] - q[0];
                double amin = double.MaxValue, amax = double.MinValue, bmin = double.MaxValue, bmax = double.MinValue;
                foreach (var v in a) { double d = v[0] * nx + v[1] * ny; amin = Math.Min(amin, d); amax = Math.Max(amax, d); }
                foreach (var v in b) { double d = v[0] * nx + v[1] * ny; bmin = Math.Min(bmin, d); bmax = Math.Max(bmax, d); }
                if (amax < bmin || bmax < amin) return false;
            }
        return true;
    }
    double MinSide(List<double[]> qd)
    {
        double Len(double[] p, double[] q) => Math.Sqrt((p[0] - q[0]) * (p[0] - q[0]) + (p[1] - q[1]) * (p[1] - q[1]));
        return Math.Min(Math.Min(Len(qd[0], qd[1]), Len(qd[2], qd[3])), Math.Min(Len(qd[1], qd[2]), Len(qd[3], qd[0])));
    }
    foreach (var (state, targets) in states6)
    {
        var overlaps = new List<string>();
        for (int i = 0; i < targets.Count; i++)
            for (int j = i + 1; j < targets.Count; j++)
                if (Overlap(targets[i].quad, targets[j].quad)) overlaps.Add(targets[i].name + "/" + targets[j].name);
        bool onScreen = targets.All(t => t.quad.All(v => v[0] >= 0 && v[0] <= W && v[1] >= 0 && v[1] <= H));
        Check(overlaps.Count == 0 && onScreen, $"T06 projection ({state}): live touch targets do not overlap and stay on screen{(overlaps.Count > 0 ? " (overlap " + string.Join(", ", overlaps) + ")" : "")}");
        Console.WriteLine($"T06 projected targets {state} (min side, dp at 2.47 px/dp on 2400x1080): " + string.Join(", ", targets.Select(t => $"{t.name} {MinSide(t.quad) / PxPerDp:0}")));
        Check(targets.Where(t => t.name == "CHECK BATCH" || t.name == "COLLECT" || t.name == "summary").All(t => MinSide(t.quad) / PxPerDp >= 48),
            $"T06 projection ({state}): the primary key (and the folded summary) reach the 48 dp aim on their shorter side");
        Check(targets.All(t => MinSide(t.quad) / PxPerDp >= 30), $"T06 projection ({state}): every live T06 target is at least 30 dp on its shorter side");
    }
    Check(!Overlap(windowIncome, dial) && !Overlap(windowDeduct, dial) && !Overlap(windowIncome, keysRow) && !Overlap(windowDeduct, keysRow),
        "T06 projection: the total windows' touch areas never overlap the dial or the category keys (they are live only when nothing is loaded anyway)");
}
Console.WriteLine($"Revised T06 checks: {checks - t06mStart}, failures {fails - t06mFails} (nine decisions, freeze once per batch, corrections, accepted locks, legacy v1, " +
                  "summary/page/folder/office, signed-store failure/duplicate/reload, guide/objectives/hints/fixtures, wiring, layout/projection; Unity rendering, native touch, audio and feel not run).");

// =====================================================================
// Revised T07 (5 November 2026): taxable income and tax due on the Case Folder's Computation page
// =====================================================================
int t07rStart = checks, t07rFails = fails;
const string B7r = "L3-BEA-001";
// The revised machine run's collected summary (T06), with the first-use lesson taught.
Level3ChapterCheckpoint o7r = got6m.Copy();
o7r.calculatorTaught = true;
// Teaching pass (6 November 2026): these stage tests start after all three desk examples were given (their own section below covers them).
o7r.milestones.Add(Level3PracticeRules.TaxTaughtMilestone);
o7r.milestones.Add(Level3PracticeRules.CreditTaughtMilestone);
DataValue Pick7r(Level3ChapterCheckpoint s, string sourceId) => Level3ValueTransfer.Wrap(Level3AmountSource.Resolve(s, bea, sourceId));
Level3CommandOutcome Place7r(Level3ChapterCheckpoint s, string field, DataValue v, out string why) => Level3ComputationRules.PlaceInput(s, bea, field, v, out why);
Level3CommandOutcome Attach7r(Level3ChapterCheckpoint s, string field, Level3Calculator c) => Level3ComputationRules.AttachTape(s, bea, field, c.DraftChain, c.DraftResult, out _);
Level3ComputationRecord Rec7r(Level3ChapterCheckpoint s) => Level3ComputationRules.Record(s, B7r);
Level3ChapterCheckpoint StageA7r(string op = "-", string gross = "worksheet:income", string expenses = "worksheet:expenses")
{
    Level3ChapterCheckpoint s = o7r.Copy();
    if (Place7r(s, "gross", Pick7r(s, gross), out _) != Level3CommandOutcome.Applied) return null;
    if (Place7r(s, "expenses", Pick7r(s, expenses), out _) != Level3CommandOutcome.Applied) return null;
    var c = new Level3Calculator();
    if (Level3ComputationRules.LoadStage(c, s, bea, Level3ComputationRules.StageTaxable, op) != null) return null;
    return Attach7r(s, "taxable-income", c) == Level3CommandOutcome.Applied ? s : null;
}
Level3ChapterCheckpoint StageB7r(string rule = "IND-20", Level3ChapterCheckpoint from = null)
{
    Level3ChapterCheckpoint s = (from ?? StageA7r()).Copy();
    if (Level3ComputationRules.SelectRule(s, bea, rule, out _) != Level3CommandOutcome.Applied) return null;
    var c = new Level3Calculator();
    if (Level3ComputationRules.LoadStage(c, s, bea, Level3ComputationRules.StageTaxDue, null) != null) return null;
    return Attach7r(s, "tax-due", c) == Level3CommandOutcome.Applied ? s : null;
}

// ---------- the collected summary's printed totals are the sources (exact, provenance, nothing earlier) ----------
Level3TransferValue inc7r = Level3AmountSource.Resolve(o7r, bea, "worksheet:income"), ded7r = Level3AmountSource.Resolve(o7r, bea, "worksheet:expenses");
Check(inc7r != null && inc7r.Amount == "900000" && inc7r.CaseId == B7r && inc7r.ContentRevision == o7r.contentRevision && inc7r.TaxYear == 2026 &&
      inc7r.SourceId == Level3ComputationRules.WorksheetIncome && ded7r != null && ded7r.Amount == "300000" && ded7r.Label.EndsWith("Supported deductions"),
    "T07r: Bea's collected summary offers its two exact totals (900,000 / 300,000) with case, source, tax year and content revision");
Level3ChapterCheckpoint untaught7r = got6m.Copy();
untaught7r.calculatorTaught = false;
Check(Level3AmountSource.Resolve(ready6m, bea, "worksheet:income") == null && Level3AmountSource.Resolve(untaught7r, bea, "worksheet:income") == null &&
      Level3AmountSource.Resolve(o7r, bea, "paper:B-WHT/BWithheldCredit") == null && Level3AmountSource.Resolve(o7r, bea, "paper:B-REV/nothing") == null,
    "T07r: nothing is offered before collection, while the first-use example is still due, or from a paper the case does not hold");
DataValue wrapped7r = Pick7r(o7r, "worksheet:income");
Check(wrapped7r.Type == DataValueType.Number && wrapped7r.SemanticKey == "Level3:worksheet:income" && wrapped7r.Value is Level3TransferValue && wrapped7r.AsFloat() == 0f &&
      wrapped7r.DisplayText == "900,000.00",
    "T07r: the shared DataValue carries the exact Level 3 payload (Level3: key, so legacy float slots never take it; AsFloat is never the authority)");
ClientRecord summary7r = Level3WorksheetCheck.SummaryRecord(bea, o7r);
var summaryTargets7r = Level3AmountSource.Targets(summary7r, bea);
Check(summaryTargets7r.Select(t => t.Value).SequenceEqual(new[] { "worksheet:income", "worksheet:expenses" }) &&
      summaryTargets7r[0].Key.SequenceEqual(new[] { "Summary Source Field 0", "Row Label 0", "Row Value 0" }) && summary7r.rows[0].label == "Income" && summary7r.rows[1].label == "Supported deductions",
    "T07r: the summary paper's printed Income and Supported deductions lines (rows 0/1) are its two transfer bands, in print order");
Check(Level3AmountSource.Targets(Level3RecordRules.Build(bea, new List<string> { "B-REV" })[0], bea).Count == 0,
    "T07r: the other originals stay read-only (no amount bands)");

// ---------- PlaceInput: same-case amounts accepted neutrally, mechanical problems refused unchanged ----------
Level3ChapterCheckpoint p7r = o7r.Copy();
string o7rSnap = Snap(o7r);
Check(Place7r(p7r, "gross", Pick7r(p7r, "worksheet:income"), out string why7r) == Level3CommandOutcome.Applied && why7r == null &&
      Rec7r(p7r).grossChoice == "worksheet:income" && Rec7r(p7r).grossAmount == "900000" && Rec7r(p7r).revision == 1 && Snap(o7r) == o7rSnap,
    "T07r: placing the summary's Income on Income copies its exact amount and source on the candidate only (revision 1)");
Check(Place7r(p7r, "gross", Pick7r(p7r, "worksheet:income"), out string dup7r) == Level3CommandOutcome.Duplicate && dup7r == "already placed" && Rec7r(p7r).revision == 1,
    "T07r: placing the same amount again is a quiet duplicate (no revision, nothing to save)");
Level3ChapterCheckpoint wrong7r = p7r.Copy();
Check(Place7r(wrong7r, "expenses", Pick7r(wrong7r, "worksheet:income"), out _) == Level3CommandOutcome.Applied && Rec7r(wrong7r).expensesAmount == "900000" &&
      Place7r(wrong7r.Copy(), "gross", Pick7r(wrong7r, "worksheet:expenses"), out _) == Level3CommandOutcome.Applied,
    "T07r: a plausible wrong same-case amount (income placed on Deductions, deductions on Income) is accepted as draft work, no correctness cue");
var bad7r = new (string what, DataValue value, string field, string why)[]
{
    ("a float value", new DataValue(900000f, "900,000", DataValueType.Number, "GrossIncome"), "gross", Level3ValueTransfer.RefuseFloat),
    ("no payload", new DataValue("900000", "900,000", DataValueType.Number, "Level3:worksheet:income"), "gross", Level3ValueTransfer.RefuseNotLevel3),
    ("another case", Level3ValueTransfer.Wrap(new Level3TransferValue { CaseId = "L3-MARINE-001", SourceId = "worksheet:income", Amount = "900000", ContentRevision = o7r.contentRevision }), "gross", Level3ValueTransfer.RefuseForeign),
    ("other content", Level3ValueTransfer.Wrap(new Level3TransferValue { CaseId = B7r, SourceId = "worksheet:income", Amount = "900000", ContentRevision = "L3-2025-v0" }), "gross", Level3ValueTransfer.RefuseRevision),
    ("an altered amount", Level3ValueTransfer.Wrap(new Level3TransferValue { CaseId = B7r, SourceId = "worksheet:income", Amount = "900001", ContentRevision = o7r.contentRevision }), "gross", Level3ComputationRules.RefuseChanged),
    ("a malformed amount", Level3ValueTransfer.Wrap(new Level3TransferValue { CaseId = B7r, SourceId = "worksheet:income", Amount = "900,000", ContentRevision = o7r.contentRevision }), "gross", Level3ValueTransfer.RefuseMalformed),
    ("an unheld paper", Level3ValueTransfer.Wrap(new Level3TransferValue { CaseId = B7r, SourceId = "paper:B-WHT/BWithheldCredit", Amount = "12500", ContentRevision = o7r.contentRevision }), "gross", Level3ValueTransfer.RefuseNotHeld),
    ("a printed result on an amount line", new DataValue(new Level3CalculatorTransferValue("ctx", "printed-tape", "Result", "600000", draftKey: "k"), "600,000.00", DataValueType.Number, "Level3:Calculator/printed-tape"), "gross", Level3ComputationRules.RefuseAmountField),
    ("an amount on the result slot", Pick7r(o7r, "worksheet:income"), "taxable-income", Level3ComputationRules.RefuseResultSlot),
    ("Tax credit before the certificate", Pick7r(o7r, "worksheet:income"), "credits", Level3ComputationRules.RefuseNoCertificateYet)
};
foreach (var (what7r, value7r, field7r, why7r2) in bad7r)
{
    Level3ChapterCheckpoint t = p7r.Copy();
    string before = Snap(t);
    Check(Place7r(t, field7r, value7r, out string r7r) == Level3CommandOutcome.Refused && r7r == why7r2 && Snap(t) == before, $"T07r: {what7r} is refused with its plain reason and nothing changes");
}
Check(Place7r(ready6m.Copy(), "gross", Pick7r(o7r, "worksheet:income"), out _) == Level3CommandOutcome.Refused,
    "T07r: the computation takes nothing before the summary is collected (WorksheetReady)");

// ---------- Stage A: chosen subtraction, Calculate on the existing calculator, attach ----------
Level3ChapterCheckpoint a7r = p7r.Copy();
Place7r(a7r, "expenses", Pick7r(a7r, "worksheet:expenses"), out _);
var calcA7r = new Level3Calculator();
Check(Level3ComputationRules.LoadStage(calcA7r, a7r, bea, Level3ComputationRules.StageTaxable, null) == "Choose subtract or add." && !calcA7r.HasDraft &&
      Level3ComputationRules.LoadStage(calcA7r, p7r, bea, Level3ComputationRules.StageTaxable, "-") == "Place both amounts first.",
    "T07r: Calculate waits for both amounts and the player's own operation choice (nothing is chosen for them)");
Check(Level3ComputationRules.LoadStage(calcA7r, a7r, bea, Level3ComputationRules.StageTaxable, "-") == null && calcA7r.DraftResult == "600000" &&
      calcA7r.DraftChain.Count == 3 && calcA7r.DraftChain[0].sourceId == Level3ComputationRules.FieldSource(B7r, "gross") && calcA7r.DraftChain[1].op == "-" &&
      calcA7r.DraftChain[2].sourceId == Level3ComputationRules.FieldSource(B7r, "expenses") && calcA7r.DraftChain.All(t => !t.manual),
    "T07r: Calculate prints Income − Deductions = 600,000 from the placed amounts (sourced tokens, no typing)");
string aSnap7r = Snap(a7r);
Check(Attach7r(a7r, "taxable-income", calcA7r) == Level3CommandOutcome.Applied && Level3ComputationRules.StageDone(a7r, bea, B7r, Level3ComputationRules.StageTaxable) &&
      Level3ComputationRules.ActiveStage(a7r, bea, B7r) == Level3ComputationRules.StageTaxDue && Level3ComputationRules.StageOperation(a7r, B7r, Level3ComputationRules.StageTaxable) == "-" &&
      Level3ComputationRules.TapeOf(a7r, B7r, "taxable-income").result == "600000",
    "T07r: one attached result completes stage A; stage B becomes active; the page shows the player's own subtract again on resume");
Check(Attach7r(a7r, "taxable-income", calcA7r) == Level3CommandOutcome.Duplicate, "T07r: attaching the same printed tape twice changes nothing (one attachment)");

// ---------- Stage B: the six-band reference, the selected formula, Calculate, attach ----------
var calcB7r = new Level3Calculator();
Check(Level3ComputationRules.LoadStage(calcB7r, a7r, bea, Level3ComputationRules.StageTaxDue, null) == "Choose a band first." && !calcB7r.HasDraft &&
      Level3ComputationRules.Record(a7r, B7r).ruleId == "" && Level3ComputationRules.Rules(bea).Count == 6,
    "T07r: no band is chosen for the player; the reference offers the content's six annual rows");
Level3ChapterCheckpoint b7r = a7r.Copy();
Level3ComputationRules.SelectRule(b7r, bea, "IND-20", out _);
Check(Level3ComputationRules.BandWorking(Level3ComputationRules.FindRule(bea, "IND-20"), "600000") == "22,500 + 20% × (600,000 − 400,000)" &&
      Level3ComputationRules.BandWorking(Level3ComputationRules.FindRule(bea, "IND-15"), "600000") == "15% × (600,000 − 250,000)" &&
      Level3ComputationRules.BandWorking(Level3ComputationRules.FindRule(bea, "IND-0"), "600000") == "No tax in this band" &&
      Level3ComputationRules.BandWorking(Level3ComputationRules.FindRule(bea, "IND-25"), null) == "102,500 + 25% × (taxable income − 800,000)",
    "T07r: the active stage shows the SELECTED band's base + rate × excess with current values (or words), never the expected band");
Check(Level3ComputationRules.LoadStage(calcB7r, b7r, bea, Level3ComputationRules.StageTaxDue, null) == null && calcB7r.DraftResult == "62500" &&
      Level3Calculator.ChainLines(calcB7r.DraftChain).Count >= 3 && Attach7r(b7r, "tax-due", calcB7r) == Level3CommandOutcome.Applied,
    "T07r: Calculate prints 600,000 − 400,000 = × 20% = + 22,500 = 62,500 as one chain, attached as Tax due");
Level3ComputationFacts f7r = Level3ComputationRules.Evaluate(b7r, bea, B7r);
Check(f7r.Step("gross").Supported && f7r.Step("expenses").Supported && f7r.Step("taxable-income").Supported && f7r.Step("rule").Supported &&
      f7r.Step("tax-due").Supported && f7r.Step("tax-due").Value == "62500" && f7r.TaxDueSupported && Rec7r(b7r).taxDueNoted &&
      Level3ComputationRules.TapeOf(b7r, B7r, "excess") == null && Level3ComputationRules.TapeOf(b7r, B7r, "marginal-tax") == null,
    "T07r: two attachments (taxable income, tax due) are a fully supported chain: 600,000 and 62,500; no separate excess/marginal tapes are needed");
Check(Level3ComputationRules.ActiveStage(b7r, bea, B7r) == Level3ComputationRules.StageBalance && !Level3ComputationRules.StageOpen(b7r, Level3ComputationRules.StageBalance) &&
      Level3CertificateRules.CreditStepOpen(b7r) && Level3CertificateRules.HandOverDue(b7r),
    "T07r: after tax due, stage C waits for Bea's certificate (the credit step opens; nothing is handed over yet)");

// ---------- plausible wrong drafts stay editable, sourced and unsupported ----------
Level3ChapterCheckpoint band7r = StageB7r("IND-25");
Level3ComputationFacts fb7r = Level3ComputationRules.Evaluate(band7r, bea, B7r);
Check(band7r != null && Level3ComputationRules.TapeOf(band7r, B7r, "tax-due").result == "52500" && !fb7r.Step("rule").Follows && !fb7r.Step("tax-due").Supported &&
      Level3ComputationRules.StageDone(band7r, bea, B7r, Level3ComputationRules.StageTaxDue) && Level3ComputationRules.ActiveStage(band7r, bea, B7r) == Level3ComputationRules.StageBalance &&
      Level3ComputationRules.Editable(band7r, B7r),
    "T07r: a wrong band (IND-25) calculates its own formula (52,500), attaches neutrally, is structurally done and stays editable; the review sees it is unsupported");
Level3ChapterCheckpoint add7r = StageA7r("+");
Check(add7r != null && Level3ComputationRules.TapeOf(add7r, B7r, "taxable-income").result == "1200000" &&
      !Level3ComputationRules.Evaluate(add7r, bea, B7r).Step("taxable-income").Follows && Level3ComputationRules.StageOperation(add7r, B7r, Level3ComputationRules.StageTaxable) == "+",
    "T07r: a wrong operation (+) prints 1,200,000 and attaches as a draft that does not follow");
Level3ChapterCheckpoint swap7r = StageA7r("-", "worksheet:expenses", "worksheet:income");
Check(swap7r != null && Level3ComputationRules.TapeOf(swap7r, B7r, "taxable-income").result == "-600000" &&
      !Level3ComputationRules.Evaluate(swap7r, bea, B7r).Step("gross").Supported && Level3ComputationRules.ContainingRule(bea, -600000m) == null,
    "T07r: swapped inputs give −600,000 (sign kept, no band contains it), still a sourced draft");
Level3ChapterCheckpoint typed7r = p7r.Copy();
Place7r(typed7r, "expenses", Pick7r(typed7r, "worksheet:expenses"), out _);
var calcT7r = new Level3Calculator();
foreach (Level3CalcKey key in new[] { Level3CalcKey.D6, Level3CalcKey.D0, Level3CalcKey.D0, Level3CalcKey.D0, Level3CalcKey.D0, Level3CalcKey.D0, Level3CalcKey.Equals }) calcT7r.Press(key);
Check(Attach7r(typed7r, "taxable-income", calcT7r) == Level3CommandOutcome.Applied && Level3ComputationRules.HasTyped(Level3ComputationRules.TapeOf(typed7r, B7r, "taxable-income")) &&
      !Level3ComputationRules.Evaluate(typed7r, bea, B7r).Step("taxable-income").Supported,
    "T07r: a typed 600,000 attaches (labelled typed) but can never support approval, even though it matches");

// ---------- stale dependencies: only dependents ----------
Level3ChapterCheckpoint st7r = StageB7r();
Check(Place7r(st7r, "expenses", Pick7r(st7r, "paper:B-DRAFT/BDraftTotal"), out _) == Level3CommandOutcome.Applied && Rec7r(st7r).expensesAmount == "354000" &&
      Level3ComputationRules.StageNeedsRecalculating(st7r, bea, B7r, Level3ComputationRules.StageTaxable) &&
      Level3ComputationRules.StageNeedsRecalculating(st7r, bea, B7r, Level3ComputationRules.StageTaxDue) &&
      Level3ComputationRules.ActiveStage(st7r, bea, B7r) == Level3ComputationRules.StageTaxable && Level3ComputationRules.TapeOf(st7r, B7r, "tax-due") != null,
    "T07r: changing an input (Bea's draft total on Deductions) marks taxable income and tax due Recalculate; their old tapes stay; stage A is active again");
Level3ChapterCheckpoint rb7r = StageB7r();
Level3ComputationRules.SelectRule(rb7r, bea, "IND-15", out _);
Check(Level3ComputationRules.IsCurrent(rb7r, bea, B7r, "taxable-income") && Level3ComputationRules.StageNeedsRecalculating(rb7r, bea, B7r, Level3ComputationRules.StageTaxDue) &&
      Level3ComputationRules.ActiveStage(rb7r, bea, B7r) == Level3ComputationRules.StageTaxDue,
    "T07r: changing only the band keeps taxable income current and stales tax due alone");

// ---------- signed save: failure keeps the prior sheet, retry once, duplicate writes nothing ----------
string sandbox7r = Path.Combine(Path.GetTempPath(), "TaxVentureL3T07r-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox7r);
try
{
    string Encode7r(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var store7r = new SaveFileStore(Path.Combine(sandbox7r, "progress.json"));
    PlayerProgressData pub7r = Level3ChapterRules.WithCheckpoint(Clone(live), o7r.Copy());
    pub7r.Normalize();
    store7r.Write(Encode7r(pub7r));
    int writes7r = 0;
    // Mirrors Level3Checkpointer.CommitComputation: candidate -> command -> (Applied only) signed write -> publish.
    Level3Checkpointer.TapeCommit Commit7r(Func<Level3ChapterCheckpoint, Level3CommandOutcome> edit)
    {
        Level3ChapterCheckpoint cand = pub7r.campaign.levelThree.Copy();
        Level3CommandOutcome outcome = edit(cand);
        if (outcome == Level3CommandOutcome.Duplicate) return Level3Checkpointer.TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) return Level3Checkpointer.TapeCommit.Refused;
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub7r), cand);
        try { next.Normalize(); store7r.Write(Encode7r(next)); } catch (Exception) { return Level3Checkpointer.TapeCommit.SaveFailed; }
        writes7r++;
        pub7r = next;
        return Level3Checkpointer.TapeCommit.Attached;
    }
    byte[] disk7r = File.ReadAllBytes(store7r.FilePath);
    string pubSnap7r = Snap(pub7r);
    Directory.CreateDirectory(store7r.TempPath);
    Check(Commit7r(c => Place7r(c, "gross", Pick7r(c, "worksheet:income"), out _)) == Level3Checkpointer.TapeCommit.SaveFailed && Snap(pub7r) == pubSnap7r &&
          File.ReadAllBytes(store7r.FilePath).SequenceEqual(disk7r) && Rec7r(pub7r.campaign.levelThree).grossChoice == "",
        "T07r: a failed write of a placed amount publishes nothing; the saved sheet on disk and live is as it was (the selection can be placed again)");
    Directory.Delete(store7r.TempPath);
    Check(Commit7r(c => Place7r(c, "gross", Pick7r(c, "worksheet:income"), out _)) == Level3Checkpointer.TapeCommit.Attached && writes7r == 1 &&
          Commit7r(c => Place7r(c, "gross", Pick7r(c, "worksheet:income"), out _)) == Level3Checkpointer.TapeCommit.AlreadyAttached && writes7r == 1,
        "T07r: the retry writes once; a duplicate placement writes nothing");
    Commit7r(c => Place7r(c, "expenses", Pick7r(c, "worksheet:expenses"), out _));
    var calcS7r = new Level3Calculator();
    Check(Level3ValueTransfer.InsertSummary(Pick7r(pub7r.campaign.levelThree, "worksheet:income"), pub7r.campaign.levelThree, bea, calcS7r), "Physical saved tape: income entered");
    calcS7r.Press(Level3CalcKey.Subtract);
    Check(Level3ValueTransfer.InsertSummary(Pick7r(pub7r.campaign.levelThree, "worksheet:expenses"), pub7r.campaign.levelThree, bea, calcS7r), "Physical saved tape: deductions entered");
    calcS7r.Press(Level3CalcKey.Equals);
    byte[] beforeTape7r = File.ReadAllBytes(store7r.FilePath);
    Directory.CreateDirectory(store7r.TempPath);
    Check(Commit7r(c => Attach7r(c, "taxable-income", calcS7r)) == Level3Checkpointer.TapeCommit.SaveFailed &&
          Level3ComputationRules.TapeOf(pub7r.campaign.levelThree, B7r, "taxable-income") == null && File.ReadAllBytes(store7r.FilePath).SequenceEqual(beforeTape7r) && calcS7r.HasDraft,
        "T07r: a failed write of an attachment keeps no tape and leaves the printed draft on the calculator, retryable");
    Directory.Delete(store7r.TempPath);
    Check(Commit7r(c => Attach7r(c, "taxable-income", calcS7r)) == Level3Checkpointer.TapeCommit.Attached &&
          Commit7r(c => Attach7r(c, "taxable-income", calcS7r)) == Level3Checkpointer.TapeCommit.AlreadyAttached,
        "T07r: the attachment retries once; a second press of the same tape is a duplicate");
    string reread7r = File.ReadAllText(store7r.FilePath);
    using (JsonDocument doc7r = JsonDocument.Parse(reread7r))
    {
        PlayerProgressData back7r = JsonSerializer.Deserialize<PlayerProgressData>(doc7r.RootElement.GetProperty("payload").GetString(), json);
        back7r.Normalize();
        Check(Level3ChapterRules.Decide(back7r.campaign.levelThree) == Level3EntryDecision.Resume && Rec7r(back7r.campaign.levelThree).grossChoice == "worksheet:income" &&
              Level3ComputationRules.StageDone(back7r.campaign.levelThree, bea, B7r, Level3ComputationRules.StageTaxable) &&
              Level3ComputationRules.ActiveStage(back7r.campaign.levelThree, bea, B7r) == Level3ComputationRules.StageTaxDue && WithoutChapter(back7r) == liveWithout,
            "T07r: reload resumes at stage B with stage A's sourced result and every carried possession intact");
        string savedSheet7r = Snap(Rec7r(back7r.campaign.levelThree));
        foreach (string leak in new[] { "IND-", "62500", "expected", "taxDue\"", "level3" })
            Check(!savedSheet7r.Contains(leak), $"T07r: the saved sheet holds only the player's own choices ('{leak}')");
    }
}
finally
{
    try { Directory.Delete(sandbox7r, true); } catch (IOException) { }
}

// ---------- mode isolation and command order (source pins at the real call sites) ----------
string page7r = Src("Scripts/Level3/Level3ComputationPage.cs");
string comp7r = Src("Scripts/Level3/Level3DeskCalculatorComputation.cs");
string folder7r = Src("Scripts/Level3/Level3CaseFolder.cs");
string desk7r = Src("Scripts/Level3/Level3DeskCalculator.cs");
string attach7rBody = Body6m(comp7r, "public bool AttachPrintedToFolder(", "/// <summary>Where a strip");
Check(Index(attach7rBody, "owner.CommitComputation(") > 0 && Index(attach7rBody, "owner.CommitComputation(") < Index(attach7rBody, "StartAttachFlight(") &&
      Index(attach7rBody, "StartAttachFlight(") < Index(attach7rBody, "draftOwner.ConsumeDraft()") && attach7rBody.Contains("Level3Calculator draftOwner = Calculator;") &&
      attach7rBody.Contains("CanPlaceTransfer(value, true)"),
    "T07r: a printed result attaches through the chapter command first (signed write, SavesStory-gated), then flies; consumed only after the save");
string receive7r = Body6m(page7r, "private bool ReceiveInput(", "private bool AcceptsResult(");
Check(receive7r.Contains("CommitComputation(") && receive7r.Contains("Level3ComputationRules.PlaceInput(c, Definition, field, value") &&
      page7r.Contains("PlaceInput(Chapter.Copy()") && Body6m(page7r, "private bool ReceiveBand(", "private void BandParts(").Contains("CommitComputation("),
    "Fixed page: certificate amounts and book bands commit through the existing save owner; acceptance is a dry run");
Check(!page7r.Contains("PlayerProgressManager") && !page7r.Contains("CommitLevelThreeCheckpoint") && !Src("Scripts/Level3/Level3AmountSource.cs").Contains("CommitComputation"),
    "T07r: the page and the paper bands never write saves themselves (replay/practice/Editor attempts stay in memory through the checkpointer)");
Check(comp7r.Contains("bool open = false;") && folder7r.Contains("list.Add(computation.Content())") && page7r.Contains("public const string Role = \"level3-computation\"") &&
      Body6m(desk7r, "private void Lift()", "/// <summary>Back onto the desk").Contains("if (!ComputationOpen && OfficeHudRuntime.Instance != null") &&
      !desk7r.Contains("ShowComputationBeside()") &&
      folder7r.Contains("!Level3DeskCalculator.Instance.SharesDeskWithFolder"),
    "T07r: one computation view: the clipboard leaf never shows; the real folder's Computation page is added after Supporting Documents and stays independently open with the lifted calculator");
Check(folder7r.Contains("Level3AmountSource.AttachTo(paper, definition)") && Src("Scripts/Level3/Level3SummaryDocument.cs").Contains("Level3AmountSource.AttachTo(paper") &&
      Src("Scripts/Level3/Level3PaperMachineModel.cs").Contains("Level3AmountSource.At(surface"),
    "T07r: the folder's summary paper and the Bag/stand summary both carry the transfer bands (through each paper's own touch surface)");
Check(Src("Scripts/UI/HUD/OfficeHudRuntime.cs").Contains("public bool OpenCaseFolderViewAt(Vector2 screen) => caseFolder3D != null && caseFolder3D.Open(screen);") &&
      !Src("Scripts/Level2/Level2OfficeReview.cs").Contains("OpenCaseFolderViewAt") && !Src("Scripts/UI/CaseFolderUI.cs").Contains("Level3"),
    "T07r: the one shared addition is an opt-in open-at-point on the existing folder view; CaseFolderUI and Level 2 code are untouched");

// ---------- copy budgets at the actual bindings (OFFICE_CASE_PLAY) ----------
Check(Level3ComputationRules.Stages.All(s => Level3ComputationRules.StageTitle(s).Split(' ').Length <= 4),
    "T07r: stage titles are at most four words (Taxable income / Tax due / Remaining payable)");
var instr7r = System.Text.RegularExpressions.Regex.Matches(page7r, "instruction\\.text = [^;]*;").Cast<System.Text.RegularExpressions.Match>()
    .SelectMany(m => System.Text.RegularExpressions.Regex.Matches(m.Value, "\"([^\"]*)\"").Cast<System.Text.RegularExpressions.Match>().Select(x => x.Groups[1].Value)).ToList();
Check(!page7r.Contains("DrawStage(") && !page7r.Contains("DrawReference(") && page7r.Contains("BandParts(root, 320f)"),
    "Fixed page: no active-stage replacement or band menu; band parts appear in reserved space");
Check(!page7r.Contains("Button(") && !page7r.Contains("LoadStage(") && !page7r.Contains("Calculate("),
    "Fixed page: all operations are manual physical keys; no Calculate/Help/retrieval buttons");
foreach (string leak in new[] { "600,000", "62,500", "IND-", "900,000", "300,000", "12,500", "50,000", "sourceId +", "revision +" })
    Check(!page7r.Contains(leak), $"T07r: the page's copy holds no answer, id or provenance dump ('{leak}')");
Check(Level3ComputationPage.SourceName("worksheet:income") == "Bea's summary" && Level3ComputationPage.SourceName("paper:B-WHT/BWithheldCredit") == "Certificate" &&
      Level3ComputationPage.HelpBand.Split(' ').Length <= 18 && Level3ComputationPage.HelpCredit.Split(' ').Length <= 18,
    "T07r: provenance labels stay short (no ids); the optional help lines are single sentences of at most 18 words");

// ---------- guide: brief, no answers, no retired PICK/clipboard words ----------
var guide7r = Json("Tutorials/Level_03/guide.json").GetProperty("lessons").EnumerateArray().ToList();
var comp7rLessons = guide7r.Where(l => l.GetProperty("id").GetString().StartsWith("computation-")).ToList();
string compText7r = string.Join(" ", comp7rLessons.SelectMany(l => l.GetProperty("pages").EnumerateArray()).Select(p => p.GetProperty("text").GetString()));
Check(comp7rLessons.Count == 4 && !compText7r.Contains("PICK") && !compText7r.Contains("clipboard") && compText7r.Contains("Case Folder") &&
      new[] { "600,000", "62,500", "400,000", "20%", "IND-" }.All(x => !compText7r.Contains(x)),
    "T07r: the computation hints point to the folder and summary, name no answer, and no retired PICK/clipboard step");
Check(!System.Text.RegularExpressions.Regex.IsMatch(page7r, "TextAlignmentOptions\\.[A-Za-z]+ \\| TextAlignmentOptions") &&
      page7r.Contains("label.enableAutoSizing = false;") && page7r.Contains("page.ContentColumn") && page7r.Contains("PaperInkStyle.FieldFontSize"),
    "T07r: page text uses single valid TMP alignments and fixed sizes (no auto-shrink), in the first-page content column with its native value font size");
Console.WriteLine($"Revised T07 checks: {checks - t07rStart}, failures {fails - t07rFails} (sources, PlaceInput refusals, stage A/B Calculate/attach, wrong drafts, stale dependents, " +
                  "signed-store failure/retry/duplicate/reload, wiring/order, copy budgets, guide; Unity rendering, native touch/drag, audio and feel not run).");

// =====================================================================
// Revised T08 (5 November 2026): the withholding certificate and stage C on the Computation page
// =====================================================================
int t08rStart = checks, t08rFails = fails;
const string Wht8r = "paper:B-WHT/BWithheldCredit", WhtIncome8r = "paper:B-WHT/BWithheldIncome";
Level3ChapterCheckpoint due8r = StageB7r();
Level3ChapterCheckpoint Handed8r(Level3ChapterCheckpoint from)
{
    Level3ChapterCheckpoint s = from.Copy();
    return Level3CertificateRules.HandOver(s, out _) == Level3CommandOutcome.Applied ? s : null;
}
Level3ChapterCheckpoint StageC8r(string credit = Wht8r, string op = "-", Level3ChapterCheckpoint from = null)
{
    Level3ChapterCheckpoint s = (from ?? Handed8r(due8r)).Copy();
    Level3CommandOutcome placed = Place7r(s, "credits", Pick7r(s, credit), out _);
    if (placed != Level3CommandOutcome.Applied && placed != Level3CommandOutcome.Duplicate) return null;
    var c = new Level3Calculator();
    if (Level3ComputationRules.LoadStage(c, s, bea, Level3ComputationRules.StageBalance, op) != null) return null;
    return Attach7r(s, "remaining-payable", c) == Level3CommandOutcome.Applied ? s : null;
}

// ---------- handover once, through the real command ----------
Check(Level3CertificateRules.HandOver(o7r.Copy(), out string early8r) == Level3CommandOutcome.Refused && early8r == Level3CertificateRules.RefuseNotYet,
    "T08r: before tax due is attached Bea keeps her certificate (the credit step is not open)");
Level3ChapterCheckpoint h8r = due8r.Copy();
string dueSnap8r = Snap(due8r);
Check(Level3CertificateRules.HandOver(h8r, out _) == Level3CommandOutcome.Applied && Snap(due8r) == dueSnap8r &&
      h8r.Case(B7r).acquiredSourceIds.Count(id => id == "B-WHT") == 1 && h8r.actionIds.Count(a => a == Level3CertificateRules.HandOverAction) == 1 &&
      h8r.milestones.Contains(Level3CertificateRules.ReceivedMilestone) && !Level3CertificateRules.Explained(h8r),
    "T08r: Take the certificate acquires B-WHT once (one action id and milestone) on the candidate; no explanation step is needed");
string h8rSnap = Snap(h8r);
Check(Level3CertificateRules.HandOver(h8r, out _) == Level3CommandOutcome.Duplicate && Snap(h8r) == h8rSnap,
    "T08r: a repeat handover (double tap, restore, another talk) is a quiet duplicate");

// ---------- the certificate in Supporting Documents and its two amounts ----------
ClientRecord cert8r = Level3RecordRules.Build(bea, new[] { "B-WHT" }).FirstOrDefault(r => r.source == "B-WHT");
var certTargets8r = Level3AmountSource.Targets(cert8r, bea);
Check(cert8r != null && cert8r.type == ClientRecord.Withholding && Level3CaseFolder.SupportingRecords(bea, due8r).All(r => r.source != "B-WHT") &&
      certTargets8r.Select(t => t.Value).SequenceEqual(new[] { WhtIncome8r, Wht8r }) &&
      certTargets8r[0].Key.SequenceEqual(new[] { "Income Payment Row" }) && certTargets8r[1].Key.SequenceEqual(new[] { "Tax Withheld Band" }) &&
      cert8r.rows.Count == 1 && cert8r.totalValue != null && !string.IsNullOrEmpty(cert8r.includedIn) && cert8r.party == "Bea" && cert8r.period == "2026-10-01 to 2026-12-31",
    "T08r: once handed over the existing Type 8 certificate is readable from its Bag item with its payee and both printed amounts as transfer bands (payment and tax withheld, alike)");
Level3TransferValue credit8r = Level3AmountSource.Resolve(h8r, bea, Wht8r), payment8r = Level3AmountSource.Resolve(h8r, bea, WhtIncome8r);
Check(credit8r != null && credit8r.Amount == "12500" && payment8r != null && payment8r.Amount == "250000" && credit8r.TaxYear == 2026 && credit8r.CaseId == B7r &&
      Level3AmountSource.Resolve(due8r, bea, Wht8r) == null,
    "T08r: the certificate's amounts are exact (12,500 withheld / 250,000 payment) with case and year, and only after the handover");

// ---------- stage C: tax due − tax credit, attach ----------
Level3ChapterCheckpoint c8r = h8r.Copy();
var calcC8r = new Level3Calculator();
Check(Level3ComputationRules.StageOpen(c8r, Level3ComputationRules.StageBalance) &&
      Level3ComputationRules.LoadStage(calcC8r, c8r, bea, Level3ComputationRules.StageBalance, "-") == "Place the tax credit first.",
    "T08r: stage C opens with the certificate and waits for the player's own tax credit");
Check(Place7r(c8r, "credits", Pick7r(c8r, Wht8r), out _) == Level3CommandOutcome.Applied && Rec7r(c8r).creditChoice == Wht8r && Rec7r(c8r).creditAmount == "12500" &&
      Level3ComputationRules.IsCurrent(c8r, bea, B7r, "taxable-income") && Level3ComputationRules.IsCurrent(c8r, bea, B7r, "tax-due"),
    "T08r: transferring the tax withheld onto Tax credit copies 12,500 exactly with its certificate source; stages A and B stay current");
Check(Level3ComputationRules.LoadStage(calcC8r, c8r, bea, Level3ComputationRules.StageBalance, null) == "Choose subtract or add." &&
      Level3ComputationRules.LoadStage(calcC8r, c8r, bea, Level3ComputationRules.StageBalance, "-") == null && calcC8r.DraftResult == "50000" &&
      calcC8r.DraftChain.Count == 3 && calcC8r.DraftChain[0].sourceId == Level3ComputationRules.SheetSource(B7r, "tax-due") &&
      calcC8r.DraftChain[2].sourceId == Level3ComputationRules.FieldSource(B7r, "credits") &&
      Attach7r(c8r, "remaining-payable", calcC8r) == Level3CommandOutcome.Applied,
    "T08r: Calculate prints Tax due − Tax credit = 50,000 (sourced) and it attaches as Remaining payable");
Level3ComputationFacts f8r = Level3ComputationRules.Evaluate(c8r, bea, B7r);
Check(f8r.Step("credits").Supported && f8r.Step("remaining-payable").Supported && f8r.Step("remaining-payable").Value == "50000" && f8r.PayableSupported &&
      f8r.TaxDueSupported && f8r.Complete && !f8r.CreditInIncome && f8r.CertificateIncomeIncludedIn == "R2" && Rec7r(c8r).payableNoted &&
      f8r.Step("gross").Value == "900000" && Level3ComputationRules.StageDone(c8r, bea, B7r, Level3ComputationRules.StageBalance),
    "T08r: the supported chain is complete: 62,500 − 12,500 = 50,000; income stays 900,000 (the certificate's payment is already in R2, never added again)");

// ---------- plausible wrong credits stay reviewable drafts; negative keeps its sign ----------
Level3ChapterCheckpoint pay8r = StageC8r(WhtIncome8r);
Level3ComputationFacts fp8r = pay8r != null ? Level3ComputationRules.Evaluate(pay8r, bea, B7r) : null;
Check(pay8r != null && Level3ComputationRules.TapeOf(pay8r, B7r, "remaining-payable").result == "-187500" && fp8r.Step("remaining-payable").Value == "-187500" &&
      !fp8r.Step("credits").Follows && !fp8r.PayableSupported && fp8r.Complete && Level3ComputationRules.Peso("-187500") == "−₱187,500.00",
    "T08r: the payment amount used as the credit gives −187,500, kept negative (shown −₱187,500.00), attached as an unsupported draft");
Level3ChapterCheckpoint sum8r = StageC8r("worksheet:income");
Check(sum8r != null && !Level3ComputationRules.Evaluate(sum8r, bea, B7r).Step("credits").Follows,
    "T08r: another same-case amount (the summary's income) on Tax credit is accepted neutrally and stays unsupported");
Level3ChapterCheckpoint add8r = StageC8r(Wht8r, "+");
Check(add8r != null && Level3ComputationRules.TapeOf(add8r, B7r, "remaining-payable").result == "75000" && !Level3ComputationRules.Evaluate(add8r, bea, B7r).Step("remaining-payable").Follows,
    "T08r: adding the credit (75,000) attaches as a draft that does not follow");
Level3ChapterCheckpoint exp8r = h8r.Copy();
Check(Place7r(exp8r, "expenses", Pick7r(exp8r, Wht8r), out _) == Level3CommandOutcome.Applied && !Level3ComputationRules.Evaluate(exp8r, bea, B7r).Step("expenses").Follows &&
      Level3ComputationRules.StageNeedsRecalculating(exp8r, bea, B7r, Level3ComputationRules.StageTaxable),
    "T08r: the credit placed as a deduction (credit-as-expense) is accepted as draft work, unsupported, and stales stage A");
Level3ChapterCheckpoint foreign8r = h8r.Copy();
string foreignSnap8r = Snap(foreign8r);
Check(Place7r(foreign8r, "credits", Level3ValueTransfer.Wrap(new Level3TransferValue { CaseId = "L3-MARINE-001", SourceId = Wht8r, Amount = "12500", ContentRevision = h8r.contentRevision }), out string fWhy8r) ==
      Level3CommandOutcome.Refused && fWhy8r == Level3ValueTransfer.RefuseForeign && Snap(foreign8r) == foreignSnap8r &&
      Place7r(foreign8r, "credits", new DataValue(12500f, "12,500", DataValueType.Number, "TaxCredit"), out string flt8r) == Level3CommandOutcome.Refused && flt8r == Level3ValueTransfer.RefuseFloat &&
      Place7r(foreign8r, "credits", Level3ValueTransfer.Wrap(new Level3TransferValue { CaseId = B7r, SourceId = Wht8r, Amount = "13000", ContentRevision = h8r.contentRevision }), out string chg8r) ==
      Level3CommandOutcome.Refused && chg8r == Level3ComputationRules.RefuseChanged && Snap(foreign8r) == foreignSnap8r,
    "T08r: another case's certificate, a float and an altered amount are refused on Tax credit; nothing changes");

// ---------- a credit-only change stales only stage C and the stamp ----------
Level3ChapterCheckpoint stamp8r = c8r.Copy();
Check(Level3ReviewRules.StampAt(stamp8r, Level3ReviewRules.StampReady, 2, 0.5f, 0.5f, 0f, out _) == Level3CommandOutcome.Applied && Level3ReviewRules.StampCurrent(stamp8r, B7r),
    "T08r: the complete sheet can be stamped at its current revision (T09's existing command)");
int revBefore8r = Rec7r(stamp8r).revision;
Check(Place7r(stamp8r, "credits", Pick7r(stamp8r, WhtIncome8r), out _) == Level3CommandOutcome.Applied && Rec7r(stamp8r).revision == revBefore8r + 1 &&
      Level3ComputationRules.IsCurrent(stamp8r, bea, B7r, "taxable-income") && Level3ComputationRules.IsCurrent(stamp8r, bea, B7r, "tax-due") &&
      Level3ComputationRules.StageDone(stamp8r, bea, B7r, Level3ComputationRules.StageTaxable) && Level3ComputationRules.StageDone(stamp8r, bea, B7r, Level3ComputationRules.StageTaxDue) &&
      Level3ComputationRules.StageNeedsRecalculating(stamp8r, bea, B7r, Level3ComputationRules.StageBalance) &&
      Level3ComputationRules.ActiveStage(stamp8r, bea, B7r) == Level3ComputationRules.StageBalance &&
      Level3ReviewRules.StampStale(stamp8r, B7r) && !Level3ReviewRules.StampCurrent(stamp8r, B7r) &&
      Level3ComputationRules.Evaluate(stamp8r, bea, B7r).TaxDueSupported && !Level3ComputationRules.Evaluate(stamp8r, bea, B7r).Complete,
    "T08r: changing only the credit keeps taxable income and tax due current and supported; Remaining payable shows Recalculate and the stamp goes out of date");

// ---------- legacy T08 selections continue quietly ----------
Level3ChapterCheckpoint legacy8r = h8r.Copy();
Level3CertificateRules.Explain(legacy8r, out _);
Check(Level3ComputationRules.SelectCredit(legacy8r, bea, Wht8r, out _) == Level3CommandOutcome.Applied && Level3CertificateRules.Explained(legacy8r) &&
      Place7r(legacy8r.Copy(), "credits", Pick7r(legacy8r, Wht8r), out string legDup8r) == Level3CommandOutcome.Duplicate && legDup8r == "already placed" &&
      Level3ComputationRules.Evaluate(legacy8r, bea, B7r).Step("credits").Supported,
    "T08r: a legacy explained/linked credit (old SelectCredit) is the same selection on the page: placing it again is a quiet duplicate; the explanation flag is kept");
Level3ChapterCheckpoint pending8r = h8r.Copy();
Check(Level3CertificateRules.QuestionDue(pending8r) && Place7r(pending8r, "credits", Pick7r(pending8r, Wht8r), out _) == Level3CommandOutcome.Applied,
    "T08r: a legacy save waiting for the retired question can link the credit directly (nothing asked again)");

// ---------- signed save: handover and credit failures keep the prior state, retry once ----------
string sandbox8r = Path.Combine(Path.GetTempPath(), "TaxVentureL3T08r-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox8r);
try
{
    string Encode8r(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var store8r = new SaveFileStore(Path.Combine(sandbox8r, "progress.json"));
    PlayerProgressData pub8r = Level3ChapterRules.WithCheckpoint(Clone(live), due8r.Copy());
    pub8r.Normalize();
    store8r.Write(Encode8r(pub8r));
    int writes8r = 0;
    Level3Checkpointer.TapeCommit Commit8r(Func<Level3ChapterCheckpoint, Level3CommandOutcome> edit)
    {
        Level3ChapterCheckpoint cand = pub8r.campaign.levelThree.Copy();
        Level3CommandOutcome outcome = edit(cand);
        if (outcome == Level3CommandOutcome.Duplicate) return Level3Checkpointer.TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) return Level3Checkpointer.TapeCommit.Refused;
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(pub8r), cand);
        try { next.Normalize(); store8r.Write(Encode8r(next)); } catch (Exception) { return Level3Checkpointer.TapeCommit.SaveFailed; }
        writes8r++;
        pub8r = next;
        return Level3Checkpointer.TapeCommit.Attached;
    }
    byte[] disk8r = File.ReadAllBytes(store8r.FilePath);
    Directory.CreateDirectory(store8r.TempPath);
    Check(Commit8r(c => Level3CertificateRules.HandOver(c, out _)) == Level3Checkpointer.TapeCommit.SaveFailed && !Level3CertificateRules.Acquired(pub8r.campaign.levelThree) &&
          File.ReadAllBytes(store8r.FilePath).SequenceEqual(disk8r) && Level3CertificateRules.HandOverDue(pub8r.campaign.levelThree),
        "T08r: a failed handover write acquires nothing (disk and live unchanged); Bea still offers it");
    Directory.Delete(store8r.TempPath);
    Check(Commit8r(c => Level3CertificateRules.HandOver(c, out _)) == Level3Checkpointer.TapeCommit.Attached && writes8r == 1 &&
          Commit8r(c => Level3CertificateRules.HandOver(c, out _)) == Level3Checkpointer.TapeCommit.AlreadyAttached && writes8r == 1 &&
          pub8r.campaign.levelThree.Case(B7r).acquiredSourceIds.Count(id => id == "B-WHT") == 1,
        "T08r: the retry hands it over once; a duplicate writes nothing and the source is acquired exactly once");
    byte[] handed8r = File.ReadAllBytes(store8r.FilePath);
    Directory.CreateDirectory(store8r.TempPath);
    Check(Commit8r(c => Place7r(c, "credits", Pick7r(c, Wht8r), out _)) == Level3Checkpointer.TapeCommit.SaveFailed && Rec7r(pub8r.campaign.levelThree).creditChoice == "" &&
          File.ReadAllBytes(store8r.FilePath).SequenceEqual(handed8r),
        "T08r: a failed credit write links nothing; the selection can be placed again");
    Directory.Delete(store8r.TempPath);
    Check(Commit8r(c => Place7r(c, "credits", Pick7r(c, Wht8r), out _)) == Level3Checkpointer.TapeCommit.Attached &&
          Commit8r(c => Place7r(c, "credits", Pick7r(c, Wht8r), out _)) == Level3Checkpointer.TapeCommit.AlreadyAttached,
        "T08r: the credit retry links once; a duplicate placement writes nothing");
    var calcS8r = new Level3Calculator();
    Level3ComputationRules.LoadStage(calcS8r, pub8r.campaign.levelThree, bea, Level3ComputationRules.StageBalance, "-");
    Check(Commit8r(c => Attach7r(c, "remaining-payable", calcS8r)) == Level3Checkpointer.TapeCommit.Attached, "T08r: Remaining payable attaches through the same save");
    using (JsonDocument doc8r = JsonDocument.Parse(File.ReadAllText(store8r.FilePath)))
    {
        PlayerProgressData back8r = JsonSerializer.Deserialize<PlayerProgressData>(doc8r.RootElement.GetProperty("payload").GetString(), json);
        back8r.Normalize();
        Level3ComputationFacts fr8r = Level3ComputationRules.Evaluate(back8r.campaign.levelThree, bea, B7r);
        Check(Level3ChapterRules.Decide(back8r.campaign.levelThree) == Level3EntryDecision.Resume && fr8r.Complete && fr8r.PayableSupported &&
              fr8r.Step("remaining-payable").Value == "50000" && back8r.campaign.levelThree.Case(B7r).acquiredSourceIds.Count(id => id == "B-WHT") == 1 &&
              WithoutChapter(back8r) == liveWithout && Level3ChapterRules.ImpliedTriggers(back8r.campaign.levelThree).Contains(Level3ChapterRules.TaxDueTrigger),
            "T08r: reload resumes with the certificate once, all three stages current (50,000) and every carried possession intact; nothing replays");
    }
}
finally
{
    try { Directory.Delete(sandbox8r, true); } catch (IOException) { }
}

// ---------- wiring and copy at the actual bindings ----------
string bea8r = Src("Scripts/Level3/Level3BeaInteractable.cs");
string office8r = Body6m(bea8r, "private void OfficeTalk(", "// =========");
string take8r = Body6m(bea8r, "private void TakeCertificate()", "private void ShowAnswers()");
Check(!office8r.Contains("StartQuestion(") && office8r.Contains("StartHandover(bea)") &&
    bea8r.Contains("certificateDropIndex = lines.Count - 1") && !bea8r.Contains("ShowTake()") &&
    Src("Scripts/Level3/Level3CertificatePaper.cs").Contains("ClientCopyPickup.Create"),
    "Certificate: mid-conversation world drop reuses commit-first pickup motion; no modal Take action");
Check(page7r.Contains("Level3ComputationRules.Credits") && page7r.Contains("ReceiveInput(Level3ComputationRules.Credits") &&
    page7r.Contains("Level3ComputationRules.Payable"), "Fixed page: credit and remaining payable stay visible on the same leaf, with existing certificate gates");
string rulesPlace8r = Body6m(Src("Scripts/Level3/Level3ComputationRules.cs"), "public static Level3CommandOutcome PlaceInput(", "/// <summary>\n    /// Calculate:");
Check(rulesPlace8r.Contains("Level3CertificateRules.Acquired(candidate)") && !rulesPlace8r.Contains("creditExplained") && rulesPlace8r.Contains("NotePayable(candidate, definition, record)"),
    "T08r: Tax credit requires the acquired certificate only (the retired explanation is not a gate); linking notes the payable once");
var guide8r = Json("Tutorials/Level_03/guide.json").GetProperty("lessons").EnumerateArray().Where(l => l.GetProperty("id").GetString().StartsWith("credit-")).ToList();
string credText8r = string.Join(" ", guide8r.SelectMany(l => l.GetProperty("pages").EnumerateArray()).Select(p => p.GetProperty("text").GetString()));
Check(guide8r.Count == 3 && guide8r.Single(l => l.GetProperty("id").GetString() == "credit-link").GetProperty("when").GetString() == "flag:creditStep" &&
      !credText8r.Contains("PICK") && !credText8r.Contains("blue tab") && new[] { "12,500", "50,000", "250,000", "62,500" }.All(x => !credText8r.Contains(x)),
    "T08r: the credit hints follow the saved handover, point to Supporting Documents and name no figure or retired control");
Console.WriteLine($"Revised T08 checks: {checks - t08rStart}, failures {fails - t08rFails} (handover once, certificate bands, stage C 50,000, wrong/negative credits, credit-only staleness and stamp, " +
                  "legacy selections, signed-store failure/retry/duplicate/reload, wiring, copy, guide; Unity rendering, native touch/drag, NPC/dialogue and feel not run).");

// ================= Stage A physical summary -> calculator -> folder =================
int physicalStart = checks, physicalFails = fails;
Level3Calculator PhysicalA(Level3ChapterCheckpoint state, Level3CalcKey operation)
{
    var calculator = new Level3Calculator();
    Check(Level3ValueTransfer.InsertSummary(Pick7r(state, Level3ComputationRules.WorksheetIncome), state, bea, calculator), "Physical A: summary income enters through SelectSource");
    calculator.Press(operation);
    Check(Level3ValueTransfer.InsertSummary(Pick7r(state, Level3ComputationRules.WorksheetExpenses), state, bea, calculator), "Physical A: summary deductions enter through SelectSource");
    Check(calculator.Press(Level3CalcKey.Equals).Printed, "Physical A: real equals prints the existing draft chain");
    return calculator;
}
var physicalState = o7r.Copy();
string physicalBefore = Snap(physicalState);
var physicalCalc = PhysicalA(physicalState, Level3CalcKey.Subtract);
Check(Snap(physicalState) == physicalBefore && Rec7r(physicalState).grossChoice == "" && Rec7r(physicalState).expensesChoice == "" && physicalCalc.DraftResult == "600000",
    "Physical A: manual input changes no source, folder field, inventory or saved computation");
Check(physicalCalc.DraftChain.Where(t => t.IsNumber).Select(t => t.sourceId).SequenceEqual(new[] { "worksheet:income", "worksheet:expenses" }) &&
      physicalCalc.DraftChain.Where(t => t.IsNumber).All(t => !t.manual), "Physical A: printed operands retain their original summary sources, without floats or typing");
Check(Attach7r(physicalState, Level3ComputationRules.Taxable, physicalCalc) == Level3CommandOutcome.Applied &&
      Level3ComputationRules.StageDone(physicalState, bea, B7r, Level3ComputationRules.StageTaxable),
    "Physical A: a current manual summary tape completes taxable income without removed legacy fields");
Check(Place7r(physicalState, Level3ComputationRules.Gross, Pick7r(physicalState, Level3ComputationRules.WorksheetIncome), out _) == Level3CommandOutcome.Applied &&
      Place7r(physicalState, Level3ComputationRules.Expenses, Pick7r(physicalState, Level3ComputationRules.WorksheetExpenses), out _) == Level3CommandOutcome.Applied &&
      Level3ComputationRules.Evaluate(physicalState, bea, B7r).Step(Level3ComputationRules.Taxable).Supported,
    "Physical A: the same reusable sources independently fill the fields after calculation; attached tape becomes supported");
Check(Level3ComputationRules.StageDone(physicalState, bea, B7r, Level3ComputationRules.StageTaxable) && StageB7r(from: physicalState) != null &&
      Level3ComputationRules.Evaluate(StageB7r(from: physicalState), bea, B7r).TaxDueSupported, "Physical A: existing Stage B accepts the new direct-summary tape unchanged");
var physicalWrong = o7r.Copy();
var physicalWrongCalc = PhysicalA(physicalWrong, Level3CalcKey.Add);
Place7r(physicalWrong, "gross", Pick7r(physicalWrong, "worksheet:income"), out _);
Place7r(physicalWrong, "expenses", Pick7r(physicalWrong, "worksheet:expenses"), out _);
Check(Attach7r(physicalWrong, "taxable-income", physicalWrongCalc) == Level3CommandOutcome.Applied && physicalWrongCalc.DraftResult == "1200000" &&
      !Level3ComputationRules.Evaluate(physicalWrong, bea, B7r).Step("taxable-income").Supported &&
      Level3ComputationRules.StageDone(physicalWrong, bea, B7r, Level3ComputationRules.StageTaxable),
    "Physical A: wrong addition remains a neutrally attached, structurally complete unsupported draft");
var physicalSwapped = physicalState.Copy();
Place7r(physicalSwapped, "expenses", Pick7r(physicalSwapped, "worksheet:income"), out _);
Check(Level3ComputationRules.IsCurrent(physicalSwapped, bea, B7r, "taxable-income") && Level3ComputationRules.Evaluate(physicalSwapped, bea, B7r).Step("taxable-income").Supported,
    "Physical A: original summary lineage is authoritative; removed legacy input fields do not override it");
var physicalC = StageC8r(from: Handed8r(StageB7r(from: physicalState)));
Check(physicalC != null && Level3ComputationRules.Evaluate(physicalC, bea, B7r).PayableSupported &&
    Level3ComputationRules.TapeOf(physicalC, B7r, "remaining-payable").result == "50000", "Physical A: exact direct-summary lineage remains supported through existing T08 balance");
foreach (var bad in new Action<Level3TransferValue>[] { v => v.CaseId = "foreign", v => v.ContentRevision = "old", v => v.TaxYear = 2025,
    v => v.Amount = "900000.01", v => v.SourceId = "paper:B-REV/nothing" })
{
    var payload = Level3AmountSource.Resolve(o7r, bea, "worksheet:income"); bad(payload);
    var target = new Level3Calculator(); string before = target.DisplayText;
    Check(!Level3ValueTransfer.InsertSummary(Level3ValueTransfer.Wrap(payload), o7r, bea, target) && target.DisplayText == before,
        "Physical A: foreign/stale/year/changed/receipt values cannot impersonate a summary operand");
}
Check(!Level3ValueTransfer.InsertSummary(new DataValue(900000f, "900000", DataValueType.Number, "Level3:worksheet:income"), o7r, bea, new Level3Calculator()) &&
      !Level3ValueTransfer.CanInsertSummary(Pick7r(o7r, "worksheet:income"), untaught7r, bea), "Physical A: no legacy float bridge or first-use lesson bypass");
foreach (float aspect in new[] { 16f / 9f, 20f / 9f, 9f / 16f })
{
    Level3ComputationPage.Workspace(aspect, out var source, out var folder, out var calculator);
    Check(!source.Overlaps(folder) && !source.Overlaps(calculator) && !folder.Overlaps(calculator) &&
        source.xMin >= 0f && source.yMin >= 0f && calculator.xMax <= 1f && folder.yMax <= 1f,
        "Physical A: phone/desktop workspace lanes keep source, fields and calculator apart");
    float pxWidth = aspect >= 1.3f ? 2400f : 1080f, pxHeight = pxWidth / aspect;
    float scale = Math.Min(pxWidth * source.width / SupportingDocumentFace.SheetSize.x, pxHeight * source.height / SupportingDocumentFace.SheetSize.y);
    Check((SupportingDocumentFace.SummaryAmountRowHeight - 4f) * scale / 2.47f >= 48f,
        "Physical A: summary amount band reaches 48dp in the supported projected phone layouts (native/render check remains manual)");
}
string physicalPage = Src("Scripts/Level3/Level3ComputationPage.cs");
string stageAView = Body6m(physicalPage, "if (stage == Level3ComputationRules.StageTaxable)", "else if (stage == Level3ComputationRules.StageTaxDue)");
Check(!physicalPage.Contains("DrawStage(") && !physicalPage.Contains("InputRow(root, \"Income\"") &&
    physicalPage.Contains("ResultRow(root, 24f") && physicalPage.Contains("PaymentContent()") && physicalPage.Contains("if (!payment)"),
    "Split pages: stable Computation and Tax/payment, no Income/Deductions or stage switch");
Check(Src("Scripts/SideQuest1/ClientRecordPaper.cs").Contains("windowLayer.AlwaysReadOnly = !OffersDeskValues;") &&
    Src("Scripts/Level3/Level3AmountSource.cs").Contains("paper.OffersDeskValues = true;") &&
    Src("Scripts/Level3/Level3DeskToolHitTarget.cs").Contains("amount.IsValueDragging"),
    "Physical A: actual Bag paper opts into shared desk drag; physical calculator accepts that same owned gesture");
var physicalManagerCalc = new Level3Calculator();
var physicalEndpoint = new SummaryCalculatorEndpoint(Pick7r(o7r, "worksheet:income"), o7r, bea, physicalManagerCalc);
sharedRepair.ClearSelection();
sharedRepair.SelectValue(physicalEndpoint, physicalEndpoint);
sharedRepair.TryPlaceOnDestination(physicalEndpoint);
Check(physicalEndpoint.Received == 1 && !sharedRepair.HasSelection && physicalManagerCalc.EntrySource.sourceId == "worksheet:income",
    "Physical A: actual shared select/place enters exact summary operand and clears only its consumed selection");
sharedRepair.TryPlaceOnDestination(physicalEndpoint);
Check(physicalEndpoint.Received == 1, "Physical A: duplicate UI callbacks cannot insert an operand twice");
Check(Body6m(Src("Scripts/Level3/Level3AmountSource.cs"), "public void OnPointerUp(", "public void OnInitializePotentialDrag").Contains("CancelOwned();") &&
    Body6m(physicalPage, "public void OnDrop(", "private void Place()").Contains("Level3DeskToolHitTarget.Canceled(eventData)"),
    "Physical A: canceled touch clears its amount before drop; field rejects canceled or unrelated source gestures");
Console.WriteLine($"Physical summary Stage A: {checks - physicalStart} checks, {fails - physicalFails} failures.");

// The compositor captures before the normal end-of-frame UI rebuild: page changes must dirty its glyph cache.
Check(Src("Scripts/UI/CaseFolderUI.cs").Contains("Doc3DLayer.For(transform)?.InvalidateText();") &&
    Body6m(Src("Scripts/UI/HUD/Doc3DLayer.cs"), "public void InvalidateText()", "private void RefreshText()").Contains("refreshedTextScale = -1f;") &&
    Src("Scripts/UI/HUD/CaseFolder3DView.cs").Contains("folderLayer.InvalidateText();"),
    "Computation presentation: opening/redrawing a folder invalidates captured text even at unchanged scale (native render still manual)");
Check(physicalPage.Contains("PaperInkStyle.StyleFieldRow(box.GetComponent<Image>(), text, true, null, null)") &&
    physicalPage.Contains("PaperInkStyle.FieldTextStacked(caption, value, blank)") && PaperInkStyle.FieldFontSize == 50f &&
    Src("Prefabs/CaseFolderFieldRow.prefab").Contains("m_fontSize: 50"),
    "Computation presentation: same 50-point printed caption/value field treatment and PaperPopIn as first-page prefab, no float row adapter");
Check(Body6m(Src("Scripts/SideQuest1/SupportingDocumentFace.cs"), "private static void Row(", "private static void TotalBand(").Contains("if (summaryTotal)") &&
    Src("Scripts/SideQuest1/SupportingDocumentFace.cs").Contains("PaperInkStyle.StyleSourceField") &&
    !Body6m(Src("Scripts/SideQuest1/SupportingDocumentFace.cs"), "private static void BuildContinued(", "private static void Row(").Contains("StyleSourceField"),
    "Computation presentation: warm source fields use the existing form styling on front summary totals only; back remains plain");

Check(!Src("Scripts/Level3/Level3SummaryDocument.cs").Contains("PrepareSummaryDesk") &&
    !desk7r.Contains("PrepareSummaryDesk") && !desk7r.Contains("IsOnOfficeDesk") &&
    !folder7r.Contains("ShowComputationBeside") && !stageAView.Contains("Button("),
    "Manual workspace: summary never opens/repositions other tools and Stage A has no retrieval button");
Check(Level3ComputationPage.FormHeight == 128f && Level3ComputationPage.FormGap == 20f &&
    physicalPage.Contains("FieldTextStacked(caption, value, blank)") && physicalPage.Contains("ResultRow(root, 24f"),
    "Manual workspace: stacked captions/values, equal padded fields and visible top inset");

// ---------- Fixed page + book bands + entirely manual physical arithmetic ----------
int fixedStart = checks, fixedFails = fails;
var fixedState = o7r.Copy();
var fixedA = PhysicalA(fixedState, Level3CalcKey.Subtract);
Check(Attach7r(fixedState, "taxable-income", fixedA) == Level3CommandOutcome.Applied &&
    Level3ComputationRules.Evaluate(fixedState, bea, B7r).Step("taxable-income").Supported && Rec7r(fixedState).grossChoice == "",
    "Fixed: supported taxable tape from reusable summary, no invisible saved field autofill");
DataValue bandPick = Level3ValueTransfer.Band(fixedState, bea, "IND-20");
Check(Level3ValueTransfer.ReadBand(bandPick, fixedState, bea, out string bandId) && bandId == "IND-20" &&
    !Level3ValueTransfer.CanInsertComputation(bandPick, fixedState, bea), "Fixed: complete band identity drops only into Annual band, never masquerades as a calculator operand");
Check(Level3ComputationRules.SelectRule(fixedState, bea, bandId, out _) == Level3CommandOutcome.Applied,
    "Fixed: band transfer records the chosen rule through the existing command");
DataValue FixedSource(Level3ChapterCheckpoint c, string id, string label) => Level3ValueTransfer.ComputationSource(c, bea, id, label);
var manualDue = new Level3Calculator();
Check(Level3ValueTransfer.InsertComputation(FixedSource(fixedState, Level3ComputationRules.SheetSource(B7r, "taxable-income"), "Taxable income"), fixedState, bea, manualDue), "Fixed: attached taxable income is an exact reusable source");
manualDue.Press(Level3CalcKey.Subtract);
Check(Level3ValueTransfer.InsertComputation(FixedSource(fixedState, Level3ComputationRules.RuleSource(bandId, "lower"), "Threshold"), fixedState, bea, manualDue), "Fixed: threshold enters manually");
Check(manualDue.Press(Level3CalcKey.Equals).Printed && manualDue.DraftResult == "200000", "Fixed: physical equals prints excess, without preloaded calculation");
manualDue.Press(Level3CalcKey.Multiply);
Check(Level3ValueTransfer.InsertComputation(FixedSource(fixedState, Level3ComputationRules.RuleSource(bandId, "rate"), "Rate"), fixedState, bea, manualDue), "Fixed: rate enters as an exact sourced percentage");
Check(manualDue.Press(Level3CalcKey.Equals).Printed && manualDue.DraftResult == "40000", "Fixed: multiplication/equals prints marginal amount");
manualDue.Press(Level3CalcKey.Add);
Check(Level3ValueTransfer.InsertComputation(FixedSource(fixedState, Level3ComputationRules.RuleSource(bandId, "base"), "Base tax"), fixedState, bea, manualDue), "Fixed: base tax enters manually");
Check(manualDue.Press(Level3CalcKey.Equals).Printed && manualDue.DraftResult == "62500" &&
    Attach7r(fixedState, "tax-due", manualDue) == Level3CommandOutcome.Applied && Level3ComputationRules.Evaluate(fixedState, bea, B7r).TaxDueSupported,
    "Fixed: all three manual steps retain exact provenance and attach one supported 62,500 chain");
Check(manualDue.DraftChain.Where(t => t.IsNumber).All(t => !t.manual) &&
    new[] { "lower", "rate", "base" }.All(part => manualDue.DraftChain.Any(t => t.sourceId == Level3ComputationRules.RuleSource(bandId, part))),
    "Fixed: tape preserves threshold, rate and base identities without floats or an automatic band operation");
var wrongBandState = fixedState.Copy();
Level3ComputationRules.SelectRule(wrongBandState, bea, "IND-15", out _);
var wrongBandCalc = new Level3Calculator();
Level3ValueTransfer.InsertComputation(FixedSource(wrongBandState, Level3ComputationRules.SheetSource(B7r, "taxable-income"), "Taxable income"), wrongBandState, bea, wrongBandCalc);
foreach (var step in new[] { (Level3CalcKey.Subtract, "lower"), (Level3CalcKey.Multiply, "rate"), (Level3CalcKey.Add, "base") })
{
    wrongBandCalc.Press(step.Item1);
    Level3ValueTransfer.InsertComputation(FixedSource(wrongBandState, Level3ComputationRules.RuleSource("IND-15", step.Item2), step.Item2), wrongBandState, bea, wrongBandCalc);
    wrongBandCalc.Press(Level3CalcKey.Equals);
}
Check(wrongBandCalc.DraftResult == "52500" && Attach7r(wrongBandState, "tax-due", wrongBandCalc) == Level3CommandOutcome.Applied &&
    !Level3ComputationRules.Evaluate(wrongBandState, bea, B7r).TaxDueSupported,
    "Fixed: plausible wrong band computes and attaches neutrally, preserving its unsupported rule provenance");
var wrongYearOperand = (Level3TransferValue)FixedSource(fixedState, Level3ComputationRules.RuleSource(bandId, "lower"), "Threshold").Value;
wrongYearOperand.TaxYear = 2025;
Check(!Level3ValueTransfer.CanInsertComputation(Level3ValueTransfer.Wrap(wrongYearOperand), fixedState, bea), "Fixed: rule operands retain tax-year validation");
var capturedRate = FixedSource(fixedState, Level3ComputationRules.RuleSource(bandId, "rate"), "Rate");
var changedBand = fixedState.Copy();
Level3ComputationRules.SelectRule(changedBand, bea, "IND-15", out _);
Check(!Level3ValueTransfer.CanInsertComputation(capturedRate, changedBand, bea) &&
    Level3ComputationRules.IsStale(changedBand, bea, B7r, "tax-due"), "Fixed: changing bands invalidates old drag payloads and old tax-due tapes");
foreach (var mutate in new Action<Level3BandTransferValue>[] { b => b.CaseId = "foreign", b => b.Year = 2025, b => b.Revision = "old", b => b.RuleId = "unknown" })
{
    var badBand = Level3ValueTransfer.Band(fixedState, bea, "IND-20"); mutate((Level3BandTransferValue)badBand.Value);
    Check(!Level3ValueTransfer.ReadBand(badBand, fixedState, bea, out _), "Fixed: foreign/year/revision/unknown book band refuses before save");
}
var fixedCredit = Handed8r(fixedState);
Check(Place7r(fixedCredit, "credits", Pick7r(fixedCredit, Wht8r), out _) == Level3CommandOutcome.Applied, "Fixed: certificate credit still transfers on the same page");
var manualBalance = new Level3Calculator();
Level3ValueTransfer.InsertComputation(FixedSource(fixedCredit, Level3ComputationRules.SheetSource(B7r, "tax-due"), "Tax due"), fixedCredit, bea, manualBalance);
manualBalance.Press(Level3CalcKey.Subtract);
Level3ValueTransfer.InsertComputation(FixedSource(fixedCredit, Level3ComputationRules.FieldSource(B7r, "credits"), "Tax credit"), fixedCredit, bea, manualBalance);
Check(manualBalance.Press(Level3CalcKey.Equals).Printed && manualBalance.DraftResult == "50000" &&
    Attach7r(fixedCredit, "remaining-payable", manualBalance) == Level3CommandOutcome.Applied && Level3ComputationRules.Evaluate(fixedCredit, bea, B7r).PayableSupported,
    "Fixed: same physical subtract/equals/tape path produces supported remaining payable");
var appendedBands = book9.First(s => s.GetProperty("heading").GetString().StartsWith("Section 7"));
Check(book9.Where(leaf => !leaf.GetProperty("heading").GetString().StartsWith("Section 7")).All(leaf => leaf.GetProperty("clickableValues").EnumerateArray().All(v => v.GetProperty("readOnly").GetBoolean())) &&
    appendedBands.GetProperty("clickableValues").EnumerateArray().Where(v => !v.GetProperty("readOnly").GetBoolean()).Select(v => v.GetProperty("enumValue").GetString()).SequenceEqual(Level3ComputationRules.Rules(bea).Select(r => r.ruleId)) &&
    book9.All(s => !s.GetProperty("heading").GetString().Contains("practice")),
    "Fixed: one numbered annual table supplies all six bands, with separate read-only explanations and no duplicate practice page");
Console.WriteLine($"Fixed manual computation: {checks - fixedStart} checks, {fails - fixedFails} failures.");

// ---------- Split leaf + mid-conversation paper pickup / atomic Bag acquisition ----------
int certPickupStart = checks, certPickupFails = fails;
var offerState = due8r.Copy();
string beforeOffer = Snap(offerState);
Check(Level3CertificateRules.Offer(offerState, out _) == Level3CommandOutcome.Applied && Level3CertificateRules.Offered(offerState) &&
    !Level3CertificateRules.Acquired(offerState), "Pickup: presentation is a saved offer, not certificate ownership");
Check(Level3CertificateRules.Offer(offerState, out _) == Level3CommandOutcome.Duplicate &&
    Level3CertificateRules.Offer(o7r.Copy(), out _) == Level3CommandOutcome.Refused,
    "Pickup: repeat offers are idempotent and pre-tax-due offers refuse");
var certificateCatalogue = InventoryCatalogue.BuiltIn();
var certInventory = new CampaignInventoryRecord();
Check(!Level3CertificateRules.AcquireItem(certInventory, certificateCatalogue, offerState, bea).Ok && !InventoryRules.Owns(certInventory, InventoryCatalogue.BeaCertificate),
    "Pickup: a displayed paper cannot enter inventory until the acquisition command");
var collectedCertificate = offerState.Copy();
Check(Level3CertificateRules.HandOver(collectedCertificate, out _) == Level3CommandOutcome.Applied &&
    Level3CertificateRules.AcquireItem(certInventory, certificateCatalogue, collectedCertificate, bea).Ok &&
    InventoryRules.Owns(certInventory, InventoryCatalogue.BeaCertificate) &&
    InventoryRules.Record(certInventory, InventoryCatalogue.BeaCertificate).referencePages.Count > 0,
    "Pickup: collection acquires one readable required certificate item with original source identity");
string ownedCertificateSnap = Snap(certInventory);
Check(Level3CertificateRules.AcquireItem(certInventory, certificateCatalogue, collectedCertificate, bea).Ok && Snap(certInventory) == ownedCertificateSnap &&
    Level3CertificateRules.HandOver(collectedCertificate, out _) == Level3CommandOutcome.Duplicate,
    "Pickup: duplicate inventory/domain callbacks do not duplicate evidence");
var fullCertInventory = new CampaignInventoryRecord();
foreach (var slot in fullCertInventory.hotbar) { slot.itemId = InventoryCatalogue.Postcard; slot.quantity = 1; }
foreach (var slot in fullCertInventory.bag) { slot.itemId = InventoryCatalogue.Postcard; slot.quantity = 1; }
Check(!Level3CertificateRules.AcquireItem(fullCertInventory, certificateCatalogue, collectedCertificate, bea).Ok,
    "Pickup: full inventory refuses before publish; paper remains retryable");
string certSandbox = Path.Combine(Path.GetTempPath(), "TaxVentureCertificatePickup-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(certSandbox);
try
{
    var certStore = new SaveFileStore(Path.Combine(certSandbox, "save.json"));
    var certPublished = Level3ChapterRules.WithCheckpoint(Clone(live), offerState.Copy());
    certPublished.campaign.inventory = new CampaignInventoryRecord();
    string EncodeCert(PlayerProgressData data) { string payload = JsonSerializer.Serialize(data, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    certStore.Write(EncodeCert(certPublished));
    bool CommitCertPickup()
    {
        var next = JsonSerializer.Deserialize<PlayerProgressData>(JsonSerializer.Serialize(certPublished, json), json);
        if (Level3CertificateRules.HandOver(next.campaign.levelThree, out _) != Level3CommandOutcome.Applied) return false;
        if (!Level3CertificateRules.AcquireItem(next.campaign.inventory, certificateCatalogue, next.campaign.levelThree, bea).Ok) return false;
        try { certStore.Write(EncodeCert(next)); } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
        certPublished = next; return true;
    }
    byte[] certDiskBefore = File.ReadAllBytes(certStore.FilePath);
    Directory.CreateDirectory(certStore.TempPath);
    Check(!CommitCertPickup() && !Level3CertificateRules.Acquired(certPublished.campaign.levelThree) &&
        !InventoryRules.Owns(certPublished.campaign.inventory, InventoryCatalogue.BeaCertificate) && File.ReadAllBytes(certStore.FilePath).SequenceEqual(certDiskBefore),
        "Pickup: injected signed-write failure publishes neither domain evidence nor Bag item");
    Directory.Delete(certStore.TempPath);
    Check(CommitCertPickup() && Level3CertificateRules.Acquired(certPublished.campaign.levelThree) && InventoryRules.Owns(certPublished.campaign.inventory, InventoryCatalogue.BeaCertificate),
        "Pickup: retry publishes certificate and inventory in one signed save");
    using var storedCert = JsonDocument.Parse(File.ReadAllText(certStore.FilePath));
    var reloadedCert = JsonSerializer.Deserialize<PlayerProgressData>(storedCert.RootElement.GetProperty("payload").GetString(), json);
    reloadedCert.Normalize();
    Check(Level3CertificateRules.Acquired(reloadedCert.campaign.levelThree) && InventoryRules.Owns(reloadedCert.campaign.inventory, InventoryCatalogue.BeaCertificate),
        "Pickup: reload retains evidence and its readable inventory item together");
}
finally { Directory.Delete(certSandbox, true); }
Check(physicalPage.Contains("PaymentContent()") && folder7r.Contains("list.Add(computation.PaymentContent())") &&
    Body6m(physicalPage, "if (!payment)", "Level3ComputationRecord record =").Contains("BandParts(root, 320f)") &&
    physicalPage.Contains("ResultRow(root, 172f, Level3ComputationRules.Payable"),
    "Split leaf: tax/payment has its own next-page role and two 24/172 credit/balance positions, above navigation");
Check(paper8.Contains("ClientCopyPickup.Create") && paper8.Contains("IdCardModel.Build") &&
    Src("Scripts/CaseFolder/ClientCopyPickup.cs").Contains("RectTransform landing = collectBeforeFlight();") &&
    Src("Scripts/RewardsSystem/PlayerProgressManager.cs").Contains("CommitLevelThreeCertificate") &&
    Level3CaseFolder.SupportingRecords(bea, collectedCertificate).Count(rec => rec.source == "B-WHT") == 1 &&
    Src("Scripts/Level3/Level3CaseFolder.cs").Contains("new System.Collections.Generic.HashSet<string>"),
    "Pickup: shared Auditor motion with paper look, commit before flight, no duplicate supporting paper");
Check(Src("Scripts/UI/HUD/HudHotbar.cs").Contains("AddCertificateDragOut(wrapper)") &&
    Src("Scripts/UI/HUD/InventoryScreen.cs").Contains("Level3CertificatePaper.Instance.Open(eventData.position)") &&
    paper8.Contains("Level3AmountSource.AttachTo(paper, definition)"),
    "Pickup: actual Bag/hotbar icon routes into same readable paper and exact desk amount bands");
Console.WriteLine($"Certificate pickup / split leaf: {checks - certPickupStart} checks, {fails - certPickupFails} failures.");

// Regression: DialogueSession snapshots lines, so authoring-line object identity cannot trigger a handover.
var dropAuthoredLine = new DialogueLine(DialogueSpeaker.Npc, "Here is the certificate.", "Bea", "Bea");
var dropSession = new DialogueSession();
Check(dropSession.TryStart(new[] { dropAuthoredLine }, () => { }) && !ReferenceEquals(dropSession.CurrentLine, dropAuthoredLine) && dropSession.LineIndex == 0,
    "Certificate callback: actual dialogue admission clones the authored line while preserving its index");
Check(!Src("Scripts/Level3/Level3BeaInteractable.cs").Contains("ReferenceEquals(line, certificateDropLine)"),
    "Certificate callback: handover follows admitted line index, never authored-line identity");

Check(!Level3BeaInteractable.CertificateLineReached(dropSession.LineIndex, 0) &&
    !Level3BeaInteractable.CertificateLineReached(dropSession.LineIndex, 1) &&
    !Level3BeaInteractable.CertificateLineReached(dropSession.LineIndex, -1),
    "Certificate callback: the handover line must finish before the paper drops");
var dropSequence = new DialogueSession();
dropSequence.TryStart(new[] { new DialogueLine(DialogueSpeaker.Npc, "Thanks for helping me work through the figures.", "Bea", "Bea"), dropAuthoredLine, new DialogueLine(DialogueSpeaker.Npc, "Take it after we finish.", "Bea", "Bea") }, () => { });
int armedDropIndex = 1, certificateDrops = 0;
void AdmitDropCallback()
{
    if (!Level3BeaInteractable.CertificateLineReached(dropSequence.LineIndex, armedDropIndex)) return;
    armedDropIndex = -1;
    certificateDrops++;
}
AdmitDropCallback(); AdmitDropCallback();
Check(certificateDrops == 0 && dropSequence.LineIndex == 0 &&
    Body6m(Src("Scripts/Level3/Level3BeaInteractable.cs"), "private void StartHandover(", "private void StartQuestion(").Contains("lines.Insert(0,"),
    "Certificate timing: opening/revealing the first line never drops paper; live handover inserts that opening line");
dropSequence.TryAdvance(out _); AdmitDropCallback(); AdmitDropCallback();
Check(certificateDrops == 0 && dropSequence.LineIndex == 1, "Certificate timing: the handover line does not drop the paper while Bea is speaking");
dropSequence.TryAdvance(out _); AdmitDropCallback();
Check(certificateDrops == 1 && dropSequence.CurrentLine.Text == "Take it after we finish.",
    "Certificate callback: show-line/reveal/next-line events present once before the closing line");
string actualPickup = Src("Scripts/CaseFolder/ClientCopyPickup.cs");
string startPaperFlight = actualPickup.Substring(actualPickup.IndexOf("private void StartFlight("));
Check(startPaperFlight.Contains("RectTransform landing = collectBeforeFlight();") &&
    Index(startPaperFlight, "if (landing == null) return;") < Index(startPaperFlight, "flying = true;") &&
    actualPickup.Contains("WorldPickupMotion.Fly(transform") && paper8.Contains("ClientCopyPickup.Create") &&
    !paper8.Contains("ReviewedFilePickup.Create"),
    "Auditor reuse: actual Level1 pickup, same flight math; failed collection cannot start the flight");
Check(Src("Scripts/Level3/Level3BeaInteractable.cs").Contains("Level3CertificatePaper.Ensure(transform).PresentFrom(transform)") &&
    Src("Scripts/Level3/Level3DeskCalculatorComputation.cs").Contains("Level3CertificatePaper.Ensure(transform)"),
    "Certificate callback: both Bea and desk share one lazily ensured paper owner; handover does not depend on desk initialization");

Check(Body6m(Src("Scripts/Level3/Level3CertificatePaper.cs"), "public void PointerDown(", "public void PointerUp(").Contains("record.BringWindowToFront();") &&
    Src("Scripts/Level3/Level3AmountSource.cs").Contains("OnPointerDown(PointerEventData eventData) => paper?.OnPointerDown(eventData)") &&
    Body6m(Src("Scripts/SideQuest1/ClientRecordPaper.cs"), "public void OnPointerDown(", "public void OnPointerUp(").Contains("holder.PointerDown(this, eventData)"),
    "Certificate focus: body and exact amount bands reach the same immediate pointer-down foreground call");
Check(Body6m(Src("Scripts/Level3/Level3CertificatePaper.cs"), "public bool BroughtToFront(", "public void PaperFlipped(").Contains("return !focusedByPress && previousOrder == record.WindowOrder;") &&
    !Body6m(Src("Scripts/Level3/Level3CertificatePaper.cs"), "public void PointerDown(", "public void PointerUp(").Contains("KeepInFront = true"),
    "Certificate focus: covered-paper tap focuses without turning it or permanently pinning it over other tools");

// =====================================================================
// Revised T09 (5 November 2026): shared stamps on the fixed Case Folder leaves, one-line Auditor returns, the mark
// =====================================================================
int t09rStart = checks, t09rFails = fails;
int Words9r(string t) => (t ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
bool NoValue9r(string t) => !string.IsNullOrEmpty(t) && !t.Any(char.IsDigit) && !t.Contains("₱");
(Level3CommandOutcome o, Level3ReviewDecision d, string r) Hand9r(Level3ChapterCheckpoint c, string stamp)
{
    Level3ReviewRules.StampAt(c, stamp, 2, 0.5f, 0.5f, -4f, out _);
    var o = Level3ReviewRules.Submit(c, bea, rc9, out Level3ReviewDecision d, out string r);
    return (o, d, r);
}

// ---------- the real fixed-leaf manual flow, stamped Ready: approved once ----------
var ok9r = fixedCredit.Copy();
var okHand9r = Hand9r(ok9r, R9);
Level3ReviewRecord okRev9r = ok9r.Case(B7r).review;
Check(okHand9r.o == Level3CommandOutcome.Applied && okHand9r.d.Outcome == Level3ReviewOutcome.Approved && okHand9r.d.Root == "" &&
      okRev9r.taxableIncome == "600000" && okRev9r.taxDue == "62500" && okRev9r.credits == "12500" && okRev9r.remainingPayable == "50000" &&
      okRev9r.xp == Level3ReviewRules.MaxXp && Level3ChapterRules.PhaseOf(ok9r) == Level3Phase.BeaDelivery &&
      ok9r.Case(B7r).copyPhase == Level3ReviewRules.CopyAtAuditor && ok9r.Case(B7r).clock?.cutoffPending != true,
    "T09r: the fixed page's own manual work (summary tape, book band chain, certificate credit) stamped Ready is approved; frozen 600,000/62,500/12,500/50,000, full XP, copy atAuditor, BeaDelivery");
var paid9r = Level3ChapterRules.WithCheckpoint(Clone(live), ok9r.Copy());
int coins9r = paid9r.campaign.coins;
var grants9r = Level3ReviewRules.ApplyApprovalGrants(paid9r, ok9r);
var again9r = Level3ReviewRules.ApplyApprovalGrants(paid9r, ok9r);
Check(grants9r.Changed && grants9r.Coins.Applied && grants9r.Coins.Amount == 100 && paid9r.campaign.coins == coins9r + 100 &&
      again9r.AlreadyClaimed && !again9r.Changed && paid9r.campaign.coins == coins9r + 100 && paid9r.campaign.publicTrust == live.campaign.publicTrust,
    "T09r: approval pays 100 coins once (a repeated grant pays nothing) and never moves Public Trust");
Check(Level3ReviewRules.Submit(ok9r.Copy(), bea, rc9, out _, out string done9r) == Level3CommandOutcome.Duplicate && done9r == Level3ReviewRules.RefuseApproved &&
      Level3ReviewRules.MarkedField(ok9r) == "" && Level3ReviewRules.MarkedLine(ok9r, rc9) == "" &&
      Level3Audit.SpokenLine(okHand9r.d, rc9.auditor) == rc9.auditor.approved[0] && Words9r(Level3Audit.SpokenLine(okHand9r.d, rc9.auditor)) <= Level3ReviewRules.LineWordBudget,
    "T09r: an approved case refuses another hand-in, shows no mark, and he says one short approval line");
var okCard9r = Level3Audit.Summary(okHand9r.d, okRev9r, grants9r, ok9r, rc9.auditor);
Check(okCard9r.StatusText == "APPROVED" && okCard9r.Facts.Any(f => f.Key == "Remaining payable" && f.Value.Contains("50,000")) && okCard9r.Issues.Count == 0 && okCard9r.Facts.Count <= 3 && okCard9r.AutoContinueSeconds == 0f,
    "T09r: the existing Audit Summary card carries the approved figures (dialogue no longer reads them out)");

// ---------- plausible wrong band: premature Ready returns ONE short line and marks the band ----------
var band9r = fixedCredit.Copy();
Level3ComputationRules.SelectRule(band9r, bea, "IND-15", out _);
var bandHand9r = Hand9r(band9r, R9);
Check(bandHand9r.d.Outcome == Level3ReviewOutcome.PrematureApproval && bandHand9r.d.Root == "rule-selection" && bandHand9r.d.Field == Level3ComputationRules.Rule &&
      bandHand9r.d.NewRoot && band9r.Case(B7r).review.lastKind == "Wrong" && Level3ReviewRules.MarkedField(band9r) == Level3ComputationRules.Rule &&
      Level3ReviewRules.MarkedLine(band9r, rc9) == bandHand9r.d.Line && Level3ChapterRules.PhaseOf(band9r) == Level3Phase.BeaComputation,
    "T09r: a wrong band stamped Ready is returned at its own earliest root (the band), charged once; the Case Folder marks Annual band; work stays open");
Check(Words9r(Level3Audit.SpokenLine(bandHand9r.d, rc9.auditor)) <= Level3ReviewRules.LineWordBudget && NoValue9r(bandHand9r.d.Line) &&
      bandHand9r.d.Line.StartsWith("Check your tax band.") && bandHand9r.d.Note.Contains("Section 7") && bandHand9r.d.Note != bandHand9r.d.Line,
    "T09r: he says one problem plus the next action (at most 24 words, no value); the longer reason is kept separately");
var bandCard9r = Level3Audit.Summary(bandHand9r.d, band9r.Case(B7r).review, default, band9r, rc9.auditor);
Check(bandCard9r.StatusText == "RETURNED" && bandCard9r.StatusDetail == "Marked: Annual band" && bandCard9r.Summary == bandHand9r.d.Line &&
      bandCard9r.Issues.SequenceEqual(new[] { bandHand9r.d.Note }) && bandCard9r.Footer == rc9.auditor.returned && bandCard9r.Facts.Count == 0,
    "T09r: the Audit Summary shows the marked line, the same short line, the longer reason under WHY and the 'work is kept' footer; no codes or figures");
Check(Level3ReviewRules.Refusal(band9r, B7r) == Level3ReviewRules.RefuseUnchanged && band9r.Case(B7r).review.chargedRoots.Count == 1,
    "T09r: the same sheet and stamp again is refused without a decision or a second charge");
var bandBack9r = Clone(band9r);
Check(bandBack9r.Case(B7r).review.lastKind == "Wrong" && Level3ReviewRules.MarkedLine(bandBack9r, rc9) == bandHand9r.d.Line &&
      Snap(band9r.Copy().Case(B7r).review) == Snap(band9r.Case(B7r).review),
    "T09r: the mark and its line survive copy and serialization (quiet restore)");

// ---------- an honest hold is thanked, not charged; a hold with nothing open is a false hold ----------
var hold9r = fixedCredit.Copy();
Level3ComputationRules.SelectRule(hold9r, bea, "IND-15", out _);
var holdHand9r = Hand9r(hold9r, H9);
Check(holdHand9r.d.Outcome == Level3ReviewOutcome.SupportedHold && !holdHand9r.d.NewRoot && hold9r.Case(B7r).review.chargedRoots.Count == 0 &&
      hold9r.Case(B7r).review.xp == Level3ReviewRules.MaxXp && Level3ReviewRules.MarkedField(hold9r) == Level3ComputationRules.Rule &&
      Level3Audit.SpokenLine(holdHand9r.d, rc9.auditor).StartsWith("Good hold. ") && Words9r(Level3Audit.SpokenLine(holdHand9r.d, rc9.auditor)) <= Level3ReviewRules.LineWordBudget &&
      Level3Audit.Summary(holdHand9r.d, hold9r.Case(B7r).review, default, hold9r, rc9.auditor).Footer == rc9.auditor.hold,
    "T09r: Not Ready on an open band is a justified hold: thanked in one short line, nothing charged, full XP, the band still marked for the player");
var falseHold9r = fixedCredit.Copy();
var falseHand9r = Hand9r(falseHold9r, H9);
Check(falseHand9r.d.Outcome == Level3ReviewOutcome.FalseHold && falseHand9r.d.Root == Level3ReviewRules.StampDecisionRoot &&
      Level3ReviewRules.MarkedField(falseHold9r) == "" && Level3ReviewRules.MarkedLine(falseHold9r, rc9) == falseHand9r.d.Line &&
      Words9r(falseHand9r.d.Line) <= Level3ReviewRules.LineWordBudget && Level3Audit.Summary(falseHand9r.d, falseHold9r.Case(B7r).review, default, falseHold9r, rc9.auditor).StatusDetail == "Marked: Your stamp",
    "T09r: Not Ready on a fully supported sheet is a false hold about the stamp: no field outline, the stamp named, one short line");
var stale9r = fixedCredit.Copy();
Level3ReviewRules.StampAt(stale9r, R9, 2, 0.5f, 0.5f, 0f, out _);
Check(Place7r(stale9r, "credits", Pick7r(stale9r, Level3ComputationRules.CertificateIncome), out _) == Level3CommandOutcome.Applied, "T09r: the credit is changed after stamping (the live placement path)");
var staleDecision9r = Level3ReviewRules.Submit(stale9r, bea, rc9, out Level3ReviewDecision staleD9r, out _);
Check(staleDecision9r == Level3CommandOutcome.Applied && staleD9r.Outcome == Level3ReviewOutcome.StaleStamp && staleD9r.Line.StartsWith("Your stamp is older") &&
      Level3ReviewRules.MarkedField(stale9r) == "",
    "T09r: a stamp older than the sheet (credit changed after stamping) is returned first, as the stamp's problem");

// ---------- fixed leaves have no Income/Deductions fields: the review judges the taxable tape itself ----------
var noTape9r = o7r.Copy();
var noTapeHand9r = Hand9r(noTape9r, R9);
Check(noTapeHand9r.d.Outcome == Level3ReviewOutcome.PrematureApproval && noTapeHand9r.d.Field == Level3ComputationRules.Taxable &&
      noTapeHand9r.d.Kind == Level3RootKind.Missing && noTapeHand9r.d.Line.StartsWith("Taxable income is still empty.") &&
      Level3ReviewRules.MarkedField(noTape9r) == Level3ComputationRules.Taxable,
    "T09r: an early Ready on an empty fixed page points to Taxable income (a line the player can fill), not a removed Gross income field");
var plus9r = o7r.Copy();
Check(Attach7r(plus9r, "taxable-income", PhysicalA(plus9r, Level3CalcKey.Add)) == Level3CommandOutcome.Applied, "T09r: a plausible wrong taxable tape (plus) attaches neutrally");
var plusHand9r = Hand9r(plus9r, R9);
Check(plusHand9r.d.Outcome == Level3ReviewOutcome.PrematureApproval && plusHand9r.d.Root == "taxable-operation" && plusHand9r.d.Field == Level3ComputationRules.Taxable &&
      plusHand9r.d.Kind == Level3RootKind.Wrong && plusHand9r.d.Line.StartsWith("Check your taxable income."),
    "T09r: Income plus Deductions is returned at the taxable operation, never as an empty input the page no longer has");
var minus9r = o7r.Copy();
Attach7r(minus9r, "taxable-income", PhysicalA(minus9r, Level3CalcKey.Subtract));
var minusFacts9r = Level3ComputationRules.Evaluate(minus9r, bea, B7r);
Check(minusFacts9r.Step("taxable-income").Supported && minusFacts9r.Step("gross").Supported && minusFacts9r.Step("expenses").Supported &&
      Level3ReviewRules.EarliestRoot(minusFacts9r, out _, out string minusField9r, out _) && minusField9r == Level3ComputationRules.Rule,
    "T09r: with the right summary tape the next open line is the band (inputs come from the collected summary itself)");
Check(Level3ReviewRules.VisibleField(Level3ComputationRules.Gross) == Level3ComputationRules.Taxable && Level3ReviewRules.VisibleField(Level3ComputationRules.Expenses) == Level3ComputationRules.Taxable &&
      Level3ReviewRules.VisibleField(Level3ComputationRules.Excess) == Level3ComputationRules.TaxDue && Level3ReviewRules.VisibleField(Level3ComputationRules.Marginal) == Level3ComputationRules.TaxDue &&
      Level3ReviewRules.VisibleLabel(Level3ComputationRules.Credits) == "Tax credit" && Level3ReviewRules.VisibleLabel(Level3ComputationRules.Rule) == "Annual band",
    "T09r: legacy roots (gross/expenses/working lines) mark the visible field that holds them, using the page's own labels");

// ---------- every spoken return fits the budget and names no value ----------
foreach (Level3ReviewRootNote root9r in rc9.roots)
    Check(Words9r(root9r.line) <= Level3ReviewRules.LineWordBudget && NoValue9r(root9r.line) && !root9r.line.Contains("Section") &&
          !string.IsNullOrWhiteSpace(root9r.note) && Level3ReviewRules.Line(rc9, root9r.id, "", root9r.id == "credit-as-expense" ? Level3RootKind.AsExpense : Level3RootKind.Wrong) == root9r.line,
        $"T09r: review.json '{root9r.id}': one short spoken line (at most 24 words, no value, no section code) plus its longer note");
foreach (string field9r in new[] { "gross", "expenses", "taxable-income", "rule", "excess", "marginal-tax", "tax-due", "credits", "remaining-payable" })
foreach (Level3RootKind kind9r in new[] { Level3RootKind.Missing, Level3RootKind.Stale, Level3RootKind.Typed })
{
    string generic9r = Level3ReviewRules.Line(rc9, "x", field9r, kind9r);
    Check(Words9r("Good hold. " + generic9r) <= Level3ReviewRules.LineWordBudget + 2 && Words9r(generic9r) <= Level3ReviewRules.LineWordBudget && NoValue9r(generic9r) &&
          !generic9r.Contains("Gross income") && !generic9r.Contains("Excess") && !generic9r.Contains("Tax on the excess"),
        $"T09r: {kind9r} line for '{field9r}' fits the budget and names only a visible field");
}
var legacyKind9r = JsonNode.Parse(JsonSerializer.Serialize(Level3ChapterRules.WithCheckpoint(Clone(live), band9r.Copy()), json));
foreach (JsonNode case9r in legacyKind9r["campaign"]["levelThree"]["cases"].AsArray()) case9r["review"]?.AsObject().Remove("lastKind");
var legacyBack9r = JsonSerializer.Deserialize<PlayerProgressData>(legacyKind9r.ToJsonString(), json);
legacyBack9r.Normalize();
Check(legacyBack9r.campaign.levelThree.Case(B7r).review.lastKind == "" && Level3ReviewRules.MarkedField(legacyBack9r.campaign.levelThree) == Level3ComputationRules.Rule &&
      NoValue9r(Level3ReviewRules.MarkedLine(legacyBack9r.campaign.levelThree, rc9)),
    "T09r: an older returned review without lastKind restores quietly, keeps its mark and shows a short line");

// ---------- clock: the first machine filing makes the record work independent ----------
var clock9r = base5m.Copy();
bool clockBefore9r = Level3ReviewRules.ClockDue(clock9r);
Check(!clockBefore9r && File5m(clock9r, "R1", "income") == Level3CommandOutcome.Applied && Level3ReviewRules.ClockDue(clock9r) &&
      Level3ReviewRules.StartClock(clock9r, rc9, out _) == Level3CommandOutcome.Applied && clock9r.Case(B7r).clock.remaining == 960f &&
      Level3ReviewRules.StartClock(clock9r, rc9, out _) == Level3CommandOutcome.Duplicate,
    "T09r: Bea's 16 active minutes start after the first lever filing on the paper machine (not at the lesson), once; a started clock is never restarted");

// ---------- wiring: shared stamp set, one line, page mark ----------
string folder9r = Src("Scripts/Level3/Level3CaseFolder.cs");
string drags9r = Src("Scripts/Level2/Level2DeskStamps.cs");
Check(folder9r.Contains("public sealed class Level3CaseFolder : MonoBehaviour, Level2DeskStamps.ICaseStamp") && folder9r.Contains("stamps.CaseBinding = this;") &&
      folder9r.Contains("public void Opening() { }") &&
      drags9r.Contains("? CaseBinding.Commit(type, local, tilt, page)") && drags9r.Contains("public bool RevisionCurrent(int revision) => CaseBinding != null"),
    "T09r: Level 3 binds the shared stamp stand/drag/ghost/ink to Bea's case and revision; calculator and stamps can stay open together");
string stampsRetired9r = Src("Scripts/Level3/Level3DeskCalculatorReview.cs");
Check(!stampsRetired9r.Contains("props.Box(") && !stampsRetired9r.Contains("PlayerPlacement.Find") && !stampsRetired9r.Contains("PlayEffect(") &&
      !Directory.GetFiles(Path.Combine(root, "Assets", "Scripts", "Level3"), "*.cs").Any(f => File.ReadAllText(f).Contains("new GameObject(\"Stamp \" +")),
    "T09r: no Level 3 file builds its own stamp bodies or hides the stand models any more (one stamp set)");
string audit9r = Src("Scripts/Level3/Level3Audit.cs");
Check(audit9r.Contains("builder.Npc(SpokenLine(decision, text)") && !audit9r.Contains("I checked the sheet") && audit9r.Split("builder.Npc(").Length == 2 &&
      audit9r.Contains("data.IssuesTitle = \"WHY\";"),
    "T09r: the hand-in adds exactly one Auditor line; the reason waits on the existing Audit Summary card");
string page9r = Src("Scripts/Level3/Level3ComputationPage.cs");
Check(page9r.Contains("marked = Level3ReviewRules.MarkedField(Chapter);") && page9r.Contains("Mark(box, field);") && page9r.Contains("Mark(box, Level3ComputationRules.Rule);") &&
      page9r.Contains("Mark(credit, Level3ComputationRules.Credits);") && page9r.Contains("Level3ReviewRules.MarkedLine(Chapter, Level3Checkpointer.ReviewContent)") &&
      page9r.Contains("review?.submissions") && !page9r.Contains("enableAutoSizing = true"),
    "T09r: the Computation leaves outline the marked line, tag its caption and show his short line in the leaf's one note; a hand-in redraws them");
GuideScript guide9r = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
Check(guide9r.lessons.First(l => l.id == Level3ChapterRules.ReviewReturnedLesson).pages.All(pg => Words9r(pg.text) <= 24 && pg.text.Contains("marked")),
    "T09r: the returned hint is one short sentence pair pointing to the mark");
Console.WriteLine($"Revised T09: {checks - t09rStart} checks, {fails - t09rFails} failures.");

// T09 user-reported layering/reopen/copy handover contracts (native rendering remains manual).
string polishFolder9 = Src("Scripts/Level3/Level3CaseFolder.cs");
string savedInk9 = Body6(polishFolder9, "private void DrawSavedInk()", "private void DrawClient(");
Check(!savedInk9.Contains("!pages.IsShowing"), "T09 polish: saved ink draws before the panel is activated, including icon opening away from the desk");
Check(polishFolder9.Contains("? \"READY\" : \"NOT READY\"") && !savedInk9.Contains("OUT OF DATE"), "T09 polish: the case verdict matches the stamp block; a stale impression fades without changing its words");
string toolsPolish9 = Src("Scripts/Level2/Level2DeskStamps.cs");
Check(toolsPolish9.Contains("[DefaultExecutionOrder(600)]") && toolsPolish9.Contains("toolLayer.KeepInFront = true;"), "T09 polish: shared stamps reserve foreground through press and return, after document captures");
Check(Src("Scripts/Level3/Level3Audit.cs").Contains("TryContinueApproval(CopyLines(), DropCopy)") && Src("Scripts/Level3/Level3Audit.cs").Contains("ClientCopyHandover.SpawnAuditorCopy("), "T09 polish: approval continues the existing Auditor dialogue and reuses the actual Levels 1/2 copy drop");

// The pickup changes only the copy phase; approval, stamp, frozen work and campaign possessions remain intact.
Check(Level3ReviewRules.CollectCopy(null, out _) == Level3CommandOutcome.Refused &&
      Level3ReviewRules.CollectCopy(fixedCredit.Copy(), out _) == Level3CommandOutcome.Refused,
    "T09 copy: missing or unapproved work cannot produce a collectible copy");
var copyCandidate9 = ok9r.Copy();
string frozenCopy9 = JsonSerializer.Serialize(copyCandidate9.Case(B7r).review, json);
Check(Level3ReviewRules.CollectCopy(copyCandidate9, out _) == Level3CommandOutcome.Applied &&
      copyCandidate9.Case(B7r).copyPhase == "carried" && ok9r.Case(B7r).copyPhase == Level3ReviewRules.CopyAtAuditor &&
      JsonSerializer.Serialize(copyCandidate9.Case(B7r).review, json) == frozenCopy9 &&
      Level3ReviewRules.CollectCopy(copyCandidate9, out _) == Level3CommandOutcome.Duplicate,
    "T09 copy: candidate-only pickup, frozen approval preserved, duplicate callbacks harmless");
var wrongCaseCopy9 = ok9r.Copy(); wrongCaseCopy9.activeCaseId = "L3-MARINE-001";
Check(Level3ReviewRules.CollectCopy(wrongCaseCopy9, out _) == Level3CommandOutcome.Refused,
    "T09 copy: another active case cannot collect Bea's copy");
string copySandbox9 = Path.Combine(Path.GetTempPath(), "TaxVentureCopy-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(copySandbox9);
try
{
    var copyStore9 = new SaveFileStore(Path.Combine(copySandbox9, "progress.json"));
    var publishedCopy9 = Level3ChapterRules.WithCheckpoint(Clone(paid9r), ok9r.Copy());
    string EncodeCopy9(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    copyStore9.Write(EncodeCopy9(publishedCopy9));
    string beforeCopy9 = Snap(publishedCopy9);
    byte[] beforeDiskCopy9 = File.ReadAllBytes(copyStore9.FilePath);
    bool PickCopy9()
    {
        var next = Clone(publishedCopy9);
        if (Level3ReviewRules.CollectCopy(next.campaign.levelThree, out _) != Level3CommandOutcome.Applied) return false;
        try { copyStore9.Write(EncodeCopy9(next)); } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
        publishedCopy9 = next;
        return true;
    }
    Directory.CreateDirectory(copyStore9.TempPath);
    Check(!PickCopy9() && Snap(publishedCopy9) == beforeCopy9 && File.ReadAllBytes(copyStore9.FilePath).SequenceEqual(beforeDiskCopy9),
        "T09 copy: failed signed save leaves copy at Auditor and every possession unchanged");
    Directory.Delete(copyStore9.TempPath);
    Check(PickCopy9() && publishedCopy9.campaign.levelThree.Case(B7r).copyPhase == "carried" &&
          publishedCopy9.campaign.coins == paid9r.campaign.coins && publishedCopy9.campaign.publicTrust == paid9r.campaign.publicTrust &&
          Snap(publishedCopy9.campaign.inventory) == Snap(paid9r.campaign.inventory) && !PickCopy9(),
        "T09 copy: retry collects once, no coins/Trust/inventory reward or second pickup");
    using var diskCopy9 = JsonDocument.Parse(File.ReadAllText(copyStore9.FilePath));
    string payloadCopy9 = diskCopy9.RootElement.GetProperty("payload").GetString();
    var reloadCopy9 = JsonSerializer.Deserialize<PlayerProgressData>(payloadCopy9, json);
    Check(diskCopy9.RootElement.GetProperty("signature").GetString() == SaveFileStore.Sign(payloadCopy9) &&
          reloadCopy9.campaign.levelThree.Case(B7r).copyPhase == "carried" && reloadCopy9.campaign.levelThree.Case(B7r).stamp.placed &&
          Level3ReviewRules.CollectCopy(reloadCopy9.campaign.levelThree, out _) == Level3CommandOutcome.Duplicate,
        "T09 copy: signed reload retains collected copy, case stamp, approval and idempotency");
}
finally { Directory.Delete(copySandbox9, true); }
Check(Src("Scripts/Level3/Level3Audit.cs").Contains("owner.CommitComputation(Level3ReviewRules.CollectCopy") &&
      Src("Scripts/UI/HUD/ClientCopyHandover.cs").Contains("collectBeforeFlight: collectBeforeFlight") &&
      Src("Scripts/Level3/Level3Audit.cs").Contains("SpawnAuditorCopy(gameObject.scene, false"),
    "T09 copy: actual shared pickup commits before flight and restores saved offers without replaying the drop");

Check(polishFolder9.Contains("ClientCopyHandover.DecorateCopyPage(index, page)") &&
      polishFolder9.Contains("copyTag = ClientCopyStamp.BuildTag(icon)") && polishFolder9.Contains("CaseFolderFieldRow.ReadOnly = true") &&
      Src("Scripts/Level3/Level3Audit.cs").Contains("FindStore(gameObject.scene)?.ReleaseQuietly()"),
    "T09 copy: collected copy reuses the original front-page mark and icon tag, is read-only, and replaces the working folder");
// =====================================================================
// T10: Bea's balance explanation and the actual copy delivery
// =====================================================================
int t10Start = checks, t10Fails = fails;
const string Pay10 = Level3DeliveryRules.Payable;
// ---------- prerequisites: no copy, no approval, copy still with the Auditor ----------
Check(Level3DeliveryRules.Explain(null, Pay10, out _) == Level3CommandOutcome.Refused &&
      Level3DeliveryRules.Explain(fixedCredit.Copy(), Pay10, out _) == Level3CommandOutcome.Refused &&
      Level3DeliveryRules.Explain(ok9r.Copy(), Pay10, out _) == Level3CommandOutcome.Refused &&
      Level3DeliveryRules.Deliver(ok9r.Copy(), out _) == Level3CommandOutcome.Refused &&
      Level3DeliveryRules.StageOf(ok9r) == Level3DeliveryRules.Stage.CopyAtAuditor && Level3DeliveryRules.StageOf(fixedCredit) == Level3DeliveryRules.Stage.None,
    "T10: nothing can be explained or delivered before approval or while the copy is still beside the Auditor");
var carried10 = ok9r.Copy();
Check(Level3ReviewRules.CollectCopy(carried10, out _) == Level3CommandOutcome.Applied && Level3DeliveryRules.StageOf(carried10) == Level3DeliveryRules.Stage.Explain,
    "T10: the collected (carried) copy opens Bea's one question");
string carriedSnap10 = Snap(carried10);
// ---------- meaningful evidence: only Remaining payable answers; wrong figures change nothing ----------
foreach (string wrong10 in new[] { Level3DeliveryRules.TaxDue, Level3DeliveryRules.Credit, "", "next", "B-WHT", null })
{
    var w10 = carried10.Copy();
    Check(Level3DeliveryRules.Explain(w10, wrong10, out _) == Level3CommandOutcome.Refused && Snap(w10) == carriedSnap10 && !Level3DeliveryRules.Answers(wrong10),
        $"T10: pointing to '{wrong10 ?? "null"}' saves nothing (copy carried, question still open, no penalty field)");
}
Check(Level3DeliveryRules.Figure(carried10, Level3DeliveryRules.TaxDue) == "62500" && Level3DeliveryRules.Figure(carried10, Level3DeliveryRules.Credit) == "12500" &&
      Level3DeliveryRules.Figure(carried10, Pay10) == "50000" && Level3DeliveryRules.Figure(fixedCredit, Pay10) == "" && Level3DeliveryRules.Figure(carried10, "x") == "",
    "T10: the docked figures are the approved copy's frozen values (62,500 / 12,500 / 50,000); nothing for unapproved work");
var early10 = carried10.Copy();
Check(Level3DeliveryRules.Deliver(early10, out string earlyWhy10) == Level3CommandOutcome.Refused && earlyWhy10 == Level3DeliveryRules.RefuseExplainFirst && Snap(early10) == carriedSnap10,
    "T10: the handover is refused until the explanation is saved (dialogue taps cannot skip the question)");
var other10 = carried10.Copy(); other10.activeCaseId = Level3ChapterRules.CompanyCaseId;
var revision10 = carried10.Copy(); revision10.contentRevision = "L3-old";
Check(Level3DeliveryRules.Explain(other10, Pay10, out _) == Level3CommandOutcome.Refused && Level3DeliveryRules.Explain(revision10, Pay10, out _) == Level3CommandOutcome.Refused,
    "T10: another active case or another content revision cannot explain Bea's copy");
var explained10 = carried10.Copy();
string frozen10 = Snap(explained10.Case(B7r).review);
Check(Level3DeliveryRules.Explain(explained10, Pay10, out _) == Level3CommandOutcome.Applied && explained10.Case(B7r).balanceExplained &&
      explained10.actionIds.Contains(Level3DeliveryRules.ExplainAction) && explained10.Case(B7r).copyPhase == "carried" &&
      Level3ChapterRules.PhaseOf(explained10) == Level3Phase.BeaDelivery && Snap(explained10.Case(B7r).review) == frozen10 &&
      Snap(explained10.Case(B7r).computation) == Snap(carried10.Case(B7r).computation) && Snap(explained10.tapes) == Snap(carried10.tapes) &&
      Level3DeliveryRules.StageOf(explained10) == Level3DeliveryRules.Stage.HandOver && !carried10.Case(B7r).balanceExplained,
    "T10: Remaining payable is saved as explained on the candidate only; approved figures, sheet, tapes and the carried copy are untouched");
string explainedSnap10 = Snap(explained10);
Check(Level3DeliveryRules.Explain(explained10, Pay10, out _) == Level3CommandOutcome.Duplicate && Level3DeliveryRules.Explain(explained10, Level3DeliveryRules.TaxDue, out _) == Level3CommandOutcome.Duplicate &&
      Snap(explained10) == explainedSnap10,
    "T10: a repeated explanation callback is a harmless duplicate");
// ---------- delivery: copy done, Bea's closing step, the company never admitted ----------
var delivered10 = explained10.Copy();
Check(Level3DeliveryRules.Deliver(delivered10, out _) == Level3CommandOutcome.Applied && delivered10.Case(B7r).copyPhase == Level3DeliveryRules.CopyDone &&
      Level3ChapterRules.PhaseOf(delivered10) == Level3Phase.ClosingFinding && delivered10.activeCaseId == B7r &&
      !delivered10.Case(Level3ChapterRules.CompanyCaseId).admitted && Level3ChapterRules.Problem(delivered10) == null &&
      delivered10.milestones.Contains(Level3DeliveryRules.DeliveredMilestone) && Snap(delivered10.Case(B7r).review) == frozen10 &&
      Level3DeliveryRules.Delivered(delivered10) && Level3ChapterRules.Decide(delivered10) == Level3EntryDecision.Resume,
    "T10: the actual handover moves Bea's chapter to her closing step (T14) with Bea still active, no company admission, frozen approval kept, resumable");
string deliveredSnap10 = Snap(delivered10);
Check(Level3DeliveryRules.Deliver(delivered10, out _) == Level3CommandOutcome.Duplicate && Snap(delivered10) == deliveredSnap10 &&
      Level3DeliveryRules.Explain(delivered10, Pay10, out _) == Level3CommandOutcome.Duplicate && Level3ReviewRules.CollectCopy(delivered10.Copy(), out _) == Level3CommandOutcome.Refused &&
      Level3ReviewRules.Submit(delivered10.Copy(), bea, rc9, out _, out _) == Level3CommandOutcome.Duplicate,
    "T10: duplicate drops/taps, a late pickup callback or another hand-in change nothing after delivery (no reapproval)");
Check(Level3ChapterRules.ExpectedActiveCase(Level3Phase.ClosingFinding, false) == B7r && Level3ChapterRules.ExpectedActiveCase(Level3Phase.ClosingFinding) == Level3ChapterRules.CompanyCaseId &&
      Level3ChapterRules.ExpectedActiveCase(Level3Phase.CompanyComputation, false) == Level3ChapterRules.CompanyCaseId &&
      Level3ChapterRules.IsAllowed(Level3Phase.BeaDelivery, Level3Phase.ClosingFinding) && Level3ChapterRules.IsAllowed(Level3Phase.BeaDelivery, Level3Phase.CompanyAppointment) &&
      !Level3ChapterRules.IsAllowed(Level3Phase.BeaDelivery, Level3Phase.WrapUpPending) && !Level3ChapterRules.IsAllowed(Level3Phase.BeaDelivery, Level3Phase.CompletedOffice),
    "T10: the Bea-only closing keeps Bea active; the deferred company route and its case binding are unchanged; delivery cannot skip to wrap-up or completion");
var fakeDone10 = carried10.Copy(); fakeDone10.Case(B7r).copyPhase = Level3DeliveryRules.CopyDone;
var fakeSave10 = Level3ChapterRules.WithCheckpoint(Clone(paid9r), fakeDone10);
Check(Level3DeliveryRules.StageOf(fakeDone10) == Level3DeliveryRules.Stage.None && !Level3DeliveryRules.Delivered(fakeDone10) &&
      !Level3DeliveryRules.ApplyDeliveryGrants(fakeSave10, fakeDone10).Changed && fakeSave10.campaign.publicTrust == paid9r.campaign.publicTrust,
    "T10: an unsupported saved 'done' without the delivery action is never treated as delivered or credited");
// ---------- +4 Public Trust once; no coins, approval or chapter completion ----------
var trustSave10 = Level3ChapterRules.WithCheckpoint(Clone(paid9r), delivered10.Copy());
int trust10 = trustSave10.campaign.publicTrust, coins10 = trustSave10.campaign.coins;
var notYet10 = Level3ChapterRules.WithCheckpoint(Clone(paid9r), explained10.Copy());
var noGrant10 = Level3DeliveryRules.ApplyDeliveryGrants(notYet10, explained10);
var grant10 = Level3DeliveryRules.ApplyDeliveryGrants(trustSave10, delivered10);
var again10 = Level3DeliveryRules.ApplyDeliveryGrants(trustSave10, delivered10);
var reapprove10 = Level3ReviewRules.ApplyApprovalGrants(trustSave10, delivered10);
Check(!noGrant10.Changed && !noGrant10.AlreadyClaimed && notYet10.campaign.publicTrust == trust10 &&
      grant10.Changed && grant10.Trust.Applied && grant10.Trust.Amount == 4 && again10.AlreadyClaimed && !again10.Changed &&
      trustSave10.campaign.publicTrust == trust10 + 4 && trustSave10.campaign.coins == coins10 && reapprove10.AlreadyClaimed && !reapprove10.Changed && reapprove10.Coins.Amount == 0 &&
      CampaignRewards.IsActivityCompleted(trustSave10, CampaignRewards.Level3, CampaignRewards.BeaFollowThrough) &&
      !trustSave10.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) && trustSave10.campaign.ledger.Last().reason == "Beatriz Mendoza: follow-through",
    "T10: explained delivery grants +4 Public Trust once (named reason), no coins, no second approval reward, no chapter completion; nothing before delivery");
Check(CampaignRewards.Total(CampaignRewards.Level3, CampaignRewards.TrustKind) == 6 && CampaignRewards.Total(CampaignRewards.Level3, CampaignRewards.CoinsKind) == 160 &&
      CampaignRewards.Amount(CampaignRewards.Level3, CampaignRewards.BeaFollowThrough, CampaignRewards.CoinsKind) == 0,
    "T10: the Bea-only Level 3 maximum is +6 Public Trust and 160 coins (REWARDS_AND_VALIDATION)");
// ---------- run-mode isolation ----------
Check(!Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.MasteryReplay, false, live) && !Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.Practice, false, live) &&
      !Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.DirectEditor, false, live) && !Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.Story, true, live) &&
      Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.Story, false, live),
    "T10: replay, practice and Editor attempts never use the story checkpoint (delivery there publishes locally with no campaign claim)");
string ppm10 = Src("Scripts/RewardsSystem/PlayerProgressManager.cs");
string commitDelivery10 = Body6(ppm10, "public bool CommitLevelThreeDelivery(", "/// <summary>");
Check(commitDelivery10.Contains("if (!UsesLevelThreeStory) return false;") && Index(commitDelivery10, "ApplyDeliveryGrants(candidate, state)") < Index(commitDelivery10, "TryWriteCandidate(candidate)") &&
      commitDelivery10.Contains("if (!due.Changed && !due.AlreadyClaimed) return false;"),
    "T10: the story delivery claim and chapter are one candidate, written before anything goes live; non-story runs write nothing");
// ---------- signed save: failure and reload at every boundary ----------
string sandbox10 = Path.Combine(Path.GetTempPath(), "TaxVentureDelivery-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox10);
try
{
    var store10 = new SaveFileStore(Path.Combine(sandbox10, "progress.json"));
    string Encode10(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    PlayerProgressData Reload10()
    {
        using var disk = JsonDocument.Parse(File.ReadAllText(store10.FilePath));
        string payload = disk.RootElement.GetProperty("payload").GetString();
        if (disk.RootElement.GetProperty("signature").GetString() != SaveFileStore.Sign(payload)) return null;
        var back = JsonSerializer.Deserialize<PlayerProgressData>(payload, json);
        back.Normalize();
        return back;
    }
    // Copy beside the Auditor (approved and paid).
    var published10 = Level3ChapterRules.WithCheckpoint(Clone(paid9r), ok9r.Copy());
    store10.Write(Encode10(published10));
    var atAuditor10 = Reload10();
    Check(atAuditor10 != null && Level3DeliveryRules.StageOf(atAuditor10.campaign.levelThree) == Level3DeliveryRules.Stage.CopyAtAuditor &&
          atAuditor10.campaign.coins == paid9r.campaign.coins && atAuditor10.campaign.levelThree.Case(B7r).review.approved,
        "T10 reload: saved atAuditor restores the waiting copy without another approval or payment");
    bool Step10(Func<Level3ChapterCheckpoint, Level3CommandOutcome> command, bool claim)
    {
        var next = Clone(published10);
        Level3ChapterCheckpoint chapter = next.campaign.levelThree.Copy();
        if (command(chapter) != Level3CommandOutcome.Applied) return false;
        next = Level3ChapterRules.WithCheckpoint(next, chapter);
        if (claim) { var due = Level3DeliveryRules.ApplyDeliveryGrants(next, chapter); if (!due.Changed && !due.AlreadyClaimed) return false; }
        try { store10.Write(Encode10(next)); } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
        published10 = next;
        return true;
    }
    Check(Step10(c => Level3ReviewRules.CollectCopy(c, out _), false) && Level3DeliveryRules.StageOf(Reload10().campaign.levelThree) == Level3DeliveryRules.Stage.Explain,
        "T10 reload: a saved carried copy restores to Bea's question (readable, read-only, nothing explained)");
    string beforeExplain10 = Snap(published10);
    byte[] diskExplain10 = File.ReadAllBytes(store10.FilePath);
    Directory.CreateDirectory(store10.TempPath);
    Check(!Step10(c => Level3DeliveryRules.Explain(c, Pay10, out _), false) && Snap(published10) == beforeExplain10 && File.ReadAllBytes(store10.FilePath).SequenceEqual(diskExplain10),
        "T10: a failed explanation save keeps the question open, the copy carried and the disk unchanged");
    Directory.Delete(store10.TempPath);
    Check(!Step10(c => Level3DeliveryRules.Explain(c, Level3DeliveryRules.Credit, out _), false) && Snap(published10) == beforeExplain10,
        "T10: a wrong figure never reaches the save");
    string beforeWrongSave = Snap(published10);
    byte[] beforeWrongDisk = File.ReadAllBytes(store10.FilePath);
    Directory.CreateDirectory(store10.TempPath);
    Check(!Step10(c => Level3DeliveryRules.RecordWrongAnswer(c, Level3DeliveryRules.Credit, 1, out _), false) &&
        Snap(published10) == beforeWrongSave && File.ReadAllBytes(store10.FilePath).SequenceEqual(beforeWrongDisk),
        "Follow-through: a failed wrong-answer write changes neither live attempts nor disk");
    Directory.Delete(store10.TempPath);
    Check(Step10(c => Level3DeliveryRules.RecordWrongAnswer(c, Level3DeliveryRules.Credit, 1, out _), false) &&
        Reload10().campaign.levelThree.Case(B7r).balanceWrongAnswers == 1,
        "Follow-through: retry persists one wrong choice in the real signed store");
    string onceWrongSave = Snap(published10);
    Check(!Step10(c => Level3DeliveryRules.RecordWrongAnswer(c, Level3DeliveryRules.Credit, 1, out _), false) && Snap(published10) == onceWrongSave,
        "Follow-through: duplicate wrong-answer callback never writes/counts again");
    Check(Step10(c => Level3DeliveryRules.Explain(c, Pay10, out _), false), "T10: the explanation retry saves");
    var explainedBack10 = Reload10();
    Check(explainedBack10.campaign.levelThree.Case(B7r).balanceExplained && Level3DeliveryRules.StageOf(explainedBack10.campaign.levelThree) == Level3DeliveryRules.Stage.HandOver &&
          explainedBack10.campaign.publicTrust == paid9r.campaign.publicTrust,
        "T10 reload: a saved explanation resumes straight at the handover line, with no Trust yet");
    string beforeDeliver10 = Snap(published10);
    byte[] diskDeliver10 = File.ReadAllBytes(store10.FilePath);
    Directory.CreateDirectory(store10.TempPath);
    Check(!Step10(c => Level3DeliveryRules.Deliver(c, out _), true) && Snap(published10) == beforeDeliver10 && File.ReadAllBytes(store10.FilePath).SequenceEqual(diskDeliver10) &&
          Reload10().campaign.levelThree.Case(B7r).copyPhase == "carried",
        "T10: a failed delivery save keeps the copy carried, the explanation, Trust and the disk as they were (the handover can be retried)");
    Directory.Delete(store10.TempPath);
    Check(Step10(c => Level3DeliveryRules.Deliver(c, out _), true) && !Step10(c => Level3DeliveryRules.Deliver(c, out _), true),
        "T10: the delivery retry saves once; a duplicate callback writes nothing");
    var deliveredBack10 = Reload10();
    Check(deliveredBack10 != null && Level3DeliveryRules.Delivered(deliveredBack10.campaign.levelThree) &&
          Level3ChapterRules.PhaseOf(deliveredBack10.campaign.levelThree) == Level3Phase.ClosingFinding &&
          deliveredBack10.campaign.publicTrust == paid9r.campaign.publicTrust + 4 && deliveredBack10.campaign.coins == paid9r.campaign.coins &&
          Snap(deliveredBack10.campaign.inventory) == Snap(paid9r.campaign.inventory) && Snap(deliveredBack10.campaign.rosaEthics) == Snap(paid9r.campaign.rosaEthics) &&
          deliveredBack10.campaign.levelThree.Case(B7r).review.remainingPayable == "50000" && deliveredBack10.campaign.levelThree.Case(B7r).stamp.placed &&
          !deliveredBack10.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) &&
          Level3DeliveryRules.Deliver(deliveredBack10.campaign.levelThree.Copy(), out _) == Level3CommandOutcome.Duplicate &&
          Level3DeliveryRules.ApplyDeliveryGrants(deliveredBack10, deliveredBack10.campaign.levelThree).AlreadyClaimed && deliveredBack10.campaign.publicTrust == paid9r.campaign.publicTrust + 4,
        "T10 reload after delivery: +4 exactly once, coins/inventory/Rosa history/approved figures/stamp kept, closing pending, no chapter completion, no repeat");
}
finally { Directory.Delete(sandbox10, true); }
// ---------- older saves ----------
var legacy10 = JsonNode.Parse(JsonSerializer.Serialize(Level3ChapterRules.WithCheckpoint(Clone(paid9r), carried10.Copy()), json));
foreach (JsonNode case10 in legacy10["campaign"]["levelThree"]["cases"].AsArray()) case10.AsObject().Remove("balanceExplained");
var legacyBack10 = JsonSerializer.Deserialize<PlayerProgressData>(legacy10.ToJsonString(), json);
legacyBack10.Normalize();
Check(!legacyBack10.campaign.levelThree.Case(B7r).balanceExplained && Level3DeliveryRules.StageOf(legacyBack10.campaign.levelThree) == Level3DeliveryRules.Stage.Explain &&
      Level3ChapterRules.Problem(legacyBack10.campaign.levelThree) == null,
    "T10: a carried copy saved before this field existed restores quietly to the question (no fabricated explanation)");
// ---------- objectives and quiet restore ----------
var steps10 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps10.Count == 14 && steps10[9].GetProperty("trigger").GetString() == Level3ChapterRules.CopyPickedUpTrigger &&
      steps10[10].GetProperty("trigger").GetString() == Level3ChapterRules.CopyDeliveredTrigger &&
      ObjectiveTriggers.Validate(Level3ChapterRules.CopyPickedUpTrigger) == null && ObjectiveTriggers.Validate(Level3ChapterRules.CopyDeliveredTrigger) == null &&
      steps10.Skip(9).All(st => st.GetProperty("text").GetString().Split(' ').Length <= 10),
    "T10: two short objective steps observe the saved pickup and the saved delivery");
Check(Level3ChapterRules.ImpliedTriggers(ok9r).Contains(Level3ChapterRules.ApprovedTrigger) && !Level3ChapterRules.ImpliedTriggers(ok9r).Contains(Level3ChapterRules.CopyPickedUpTrigger) &&
      Level3ChapterRules.ImpliedTriggers(explained10).Contains(Level3ChapterRules.CopyPickedUpTrigger) && !Level3ChapterRules.ImpliedTriggers(explained10).Contains(Level3ChapterRules.CopyDeliveredTrigger) &&
      Level3ChapterRules.ImpliedTriggers(delivered10).Contains(Level3ChapterRules.CopyDeliveredTrigger),
    "T10: a resume ticks pickup/delivery from the saved copy phase only");
// ---------- content ----------
Level3TownFile town10 = JsonSerializer.Deserialize<Level3TownFile>(File.ReadAllText(Res("Levels/Level_03/town_life.json")), json);
Level3ClientContent bea10 = town10.bea;
int Words10(string t) => (t ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
Check(Words10(bea10.balanceQuestion) <= 30 && bea10.balanceQuestion.Contains("left") && bea10.balanceQuestion.Contains("withheld") &&
      new[] { bea10.balanceQuestion, bea10.taxDueHint, bea10.creditHint, bea10.balanceReply, bea10.copyHandoverLine, bea10.deliveredLine }.All(t => !System.Text.RegularExpressions.Regex.IsMatch(t, "[0-9]")) &&
      Words10(bea10.taxDueHint) <= 20 && Words10(bea10.creditHint) <= 20 && Words10(bea10.balanceReply) <= 20 &&
      Level3TownContent.Validate(town10).Count == 0,
    "T10: one short question (<= 30 words), one contextual sentence per wrong figure, short reply; no figure or answer key in the copy text; content validates");
// ---------- wiring at the actual call sites ----------
string bea10src = Src("Scripts/Level3/Level3BeaInteractable.cs");
string point10 = Body6(bea10src, "private void PointTo(string id, int attempt)", "// ClientCopyHandover.ILevelDelivery");
string deliver10 = Body6(bea10src, "private bool DeliverCopy()", "private void PlayDelivered(");
Check(bea10src.Contains("public sealed class Level3BeaInteractable : MonoBehaviour, IInteractable, ClientCopyHandover.ILevelDelivery") &&
      bea10src.Contains("private void OnEnable() => ClientCopyHandover.LevelDelivery = this;") &&
      bea10src.Contains("else if (phase == Level3Phase.BeaDelivery || Level3DeliveryRules.Delivered(chapter)) DeliveryTalk(chapter, bea);") &&
      bea10src.Contains(".AwaitAction(BalanceAction, bea.balancePrompt)") && bea10src.Contains(".AwaitAction(ClientCopyRules.HandoverAction, bea.copyHandoverPrompt)") &&
      bea10src.Contains("case BalanceAction: ShowCopyFigures(); break;"),
    "T10: Bea's delivery talk, her question waiting on a figure and her handover line waiting on the existing client-copy action");
Check(point10.Contains("if (!Level3DeliveryRules.Answers(id))") && Index(point10, "CommitBalanceExplanation(id") < Index(point10, "if (!Level3DeliveryRules.Answers(id))") &&
      Index(point10, "CommitBalanceExplanation(id") < Index(point10, "builder.Npc(bea.balanceReply") && point10.Contains("ShowCopyFigures();\n            return;") &&
      !point10.Contains("Level3BeaTable.Level3Say(hint);\n            ShowCopyFigures"),
    "T10: each choice is saved before feedback; wrong figures retain the same question and save failures keep the figures up");
Check(Index(deliver10, "owner.CommitDelivery(") < Index(deliver10, "PlayDelivered(grants)") && Index(deliver10, "PlayDelivered(grants)") < Index(deliver10, "ContinueWith(ClientCopyRules.HandoverAction") &&
      bea10src.Contains("if (!grants.Changed || !grants.Trust.Applied || grants.Trust.Amount <= 0) return;") &&
      deliver10.Contains("if (!AwaitingCopy || delivering) return false;") && deliver10.Contains("table.Content.copyRetryPrompt") &&
      Body6(bea10src, "public HudDragOutcome? ResolveDrop(", "private bool OverBea(").Contains("if (!OverBea(screen)) return HudDragOutcome.Returned;") &&
      Body6(bea10src, "public bool AwaitingCopy =>", "public string ClientName").Contains("DialogueUI.ActiveAwaitingAction == ClientCopyRules.HandoverAction"),
    "T10: only the awaited handover accepts the copy; save first, then the icon empties and her thanks/emote/toast; a drop elsewhere returns it; a failed write keeps the line waiting");
string hud10 = Src("Scripts/UI/HUD/OfficeHudRuntime.cs");
string handover10 = Src("Scripts/UI/HUD/ClientCopyHandover.cs");
Check(hud10.Contains("ClientCopyHandover.LevelDelivery != null && ClientCopyHandover.LevelDelivery.TryIconTap()") && hud10.Contains("ClientCopyHandover.LevelDelivery?.ResolveDrop(screen);") &&
      handover10.Contains("|| AuditorInteractable.AwaitingFolder || LevelAwaiting;") && handover10.Contains("LevelDelivery = null;") &&
      handover10.Contains("if (level != OfficeLevelCatalog.Level1Id && level != OfficeLevelCatalog.Level2Id) return;"),
    "T10: the existing folder icon tap/drag and HUD peek reach Bea through one opt-in seam; Levels 1-2 keep their own handover owner");
string cp10 = Src("Scripts/Level3/Level3Checkpointer.cs");
string cd10 = Body6(cp10, "public TapeCommit CommitDelivery(", "/// <summary>");
Check(Index(cd10, "Level3DeliveryRules.Deliver(candidate") < Index(cd10, "CommitLevelThreeDelivery(candidate") && Index(cd10, "CommitLevelThreeDelivery(candidate") < Index(cd10, "Publish(candidate);") &&
      cp10.Contains("if (!wasDelivered && Level3DeliveryRules.Delivered(committed)) GameplayEvents.RaiseClientCopyHandedOver();") &&
      Src("Scripts/Level3/Level3CaseFolder.cs").Contains("copy == Level3ReviewRules.CopyAtAuditor || copy == Level3DeliveryRules.CopyDone"),
    "T10: candidate -> signed write -> publish -> objective; the delivered folder stays out of the bar on resume as in Levels 1-2");
foreach (string file in new[] { "Level3DeliveryRules.cs", "Level3BeaInteractable.cs" })
{
    string code10 = System.Text.RegularExpressions.Regex.Replace(Src("Scripts/Level3/" + file), @"///.*|//.*", "");
    Check(!code10.Contains("MarkChapterCompleted") && !code10.Contains("CompanyAppointment") && !code10.Contains("float.Parse") && !code10.Contains("CoinsKind") &&
          !code10.Contains("expectedFields") && !code10.Contains("finalTaxPayable"),
        $"{file}: no chapter completion, company admission, float money, coin grant or answer key");
}
Console.WriteLine($"T10: {checks - t10Start} checks, {fails - t10Fails} failures (prerequisites, evidence, duplicates, signed failure/reload per boundary, +4 once, isolation, content, wiring; Unity rendering, touch/drag, NPC/dialogue and feel not run).");
// =====================================================================
// T13: carried equipment, inventory continuity and source pins through Bea's case
// =====================================================================
int t13Start = checks, t13Fails = fails;
var cat13 = InventoryCatalogue.BuiltIn();
var file13 = JsonSerializer.Deserialize<InventoryCatalogue>(File.ReadAllText(Res("Campaign/inventory.json")), json);
Check(file13.items.Select(i => i.id).OrderBy(x => x).SequenceEqual(cat13.items.Select(i => i.id).OrderBy(x => x)) &&
      new[] { InventoryCatalogue.CaseFolder, InventoryCatalogue.BeaSummary, InventoryCatalogue.BeaCertificate, InventoryCatalogue.Notes }.All(id => file13.IsRequired(id) && cat13.IsRequired(id)) &&
      file13.Find(InventoryCatalogue.ReviewVoucher).carryLimit == 2 && file13.Find(InventoryCatalogue.ReviewVoucher).stackLimit == 2 &&
      InventoryRules.RequiredCapacityProblems(file13, "Level_03").Count == 0,
    "T13: inventory.json matches the built-in catalogue; Bea's summary/certificate and the folder are required; vouchers carry two; Level 3's required set fits the base 3 + 6");
string[] prior13 = { InventoryCatalogue.Notes, InventoryCatalogue.TaxCodeBook, InventoryCatalogue.CaseFolder, InventoryCatalogue.ProfileBinder, InventoryCatalogue.ReviewedFile,
                     InventoryCatalogue.MarkedRecords, InventoryCatalogue.Postcard, InventoryCatalogue.BookmarkSet, InventoryCatalogue.ReviewVoucher };
CampaignInventoryRecord Rich13(bool upgraded, bool withBook = true, bool keepsakes = true)
{
    var r = new CampaignInventoryRecord();
    foreach (string id in prior13)
    {
        if (id == InventoryCatalogue.TaxCodeBook && !withBook) continue;
        if (!keepsakes && (id == InventoryCatalogue.Postcard || id == InventoryCatalogue.BookmarkSet || id == InventoryCatalogue.ReviewVoucher)) continue;
        InventoryRules.Acquire(r, cat13, id, id == InventoryCatalogue.ReviewVoucher ? 2 : 1, "rich13/" + id);
    }
    InventoryRules.SetContents(r, InventoryCatalogue.CaseFolder, new[] { "L1-MARIE-001/papers", "L2-ROSA-001/papers", "L2-ROSA-001/client-copy" });
    InventoryRules.KeepReference(r, InventoryCatalogue.CaseFolder, new InventoryReferencePage { id = "L2-ROSA-001/D1/0", title = "Rosa's case · D1", lines = new List<string> { "Daily sales: 1,250.00" } });
    InventoryRules.SetContents(r, InventoryCatalogue.ProfileBinder, new[] { "profile-marie", "profile-rosa" });
    if (InventoryRules.Owns(r, InventoryCatalogue.MarkedRecords))
        InventoryRules.KeepReference(r, InventoryCatalogue.MarkedRecords, new InventoryReferencePage { id = "L2-ROSA-001/marked/0", title = "Rosa's Marked Records · 1", lines = new List<string> { "R1 kept: market sales" } });
    if (InventoryRules.HasBookmarks(r)) { InventoryRules.Pin(r, cat13, InventoryCatalogue.MarkedRecords); InventoryRules.Pin(r, cat13, InventoryCatalogue.ReviewedFile); }
    if (upgraded) { InventoryRules.UpgradeCapacity(r, InventoryArea.Hotbar, 5, "rich13/pouch"); InventoryRules.UpgradeCapacity(r, InventoryArea.Bag, 13, "rich13/satchel"); }
    return r;
}
// Every earlier possession exactly: slot, carried count, its record (contents, readable pages), pins and capacity.
string Kept13(CampaignInventoryRecord r, params string[] skip) =>
    string.Join("|", prior13.Where(id => !skip.Contains(id)).Select(id => id + "@" + InventoryRules.Find(r, id) + "x" + InventoryRules.CarriedCount(r, id) + ":" + Snap(InventoryRules.Record(r, id)))) +
    "|pins:" + string.Join(",", r.pins) + "|cap:" + r.hotbarCapacity + "/" + r.bagCapacity;
bool Full13(CampaignInventoryRecord r) => InventoryRules.FirstEmpty(r.hotbar, r.hotbarCapacity) < 0 && InventoryRules.FirstEmpty(r.bag, r.bagCapacity) < 0;

// ---------- the whole chapter on the shipped fixture and on a rich upgraded Bag ----------
foreach (bool upgraded13 in new[] { false, true })
{
    string label13 = upgraded13 ? "upgraded 5 + 13" : "base 3 + 6 (the shipped fixture)";
    PlayerProgressData walk = Clone(live);
    if (upgraded13) walk.campaign.inventory = Rich13(true);
    CampaignInventoryRecord inv = walk.campaign.inventory;
    string[] owned13 = prior13.Where(id => InventoryRules.Owns(inv, id)).ToArray();
    string before13 = Kept13(inv);
    string wallet13 = Snap(new object[] { walk.campaign.decor, walk.campaign.ownedUpgradeIds, walk.campaign.inventory.displays });
    int coins13 = walk.campaign.coins, trust13 = walk.campaign.publicTrust;
    Check(Level3ChapterRules.WithCheckpoint(walk, Level3ChapterRules.CreateEntry()) != null && Kept13(inv) == before13 &&
          Level3ChapterRules.WithCheckpoint(walk, o7r.Copy()) != null && Kept13(inv) == before13,
        $"T13 {label13}: entry and the outdoor/office chapter snapshots never touch the Bag");
    Check(Level3WorksheetCheck.AcquireSummary(inv, cat13, got6m, null, bea).Ok && InventoryRules.Owns(inv, InventoryCatalogue.BeaSummary) && Kept13(inv) == before13 &&
          Level3CertificateRules.HasRoom(inv) && Level3CertificateRules.AcquireItem(inv, cat13, fixedCredit, bea).Ok && !InventoryRules.Find(inv, InventoryCatalogue.BeaCertificate).IsNone &&
          Kept13(inv) == before13 && inv.hand.IsEmpty,
        $"T13 {label13}: Bea's summary and certificate land in free slots; every earlier item keeps its slot, count, contents, readable pages and pins");
    string afterPapers13 = Snap(inv);
    walk = Level3ChapterRules.WithCheckpoint(walk, ok9r.Copy());
    var approve13 = Level3ReviewRules.ApplyApprovalGrants(walk, ok9r);
    walk = Level3ChapterRules.WithCheckpoint(walk, delivered10.Copy());
    var deliver13 = Level3DeliveryRules.ApplyDeliveryGrants(walk, delivered10);
    walk.Normalize();
    Check(Snap(walk.campaign.inventory) == afterPapers13 && approve13.Coins.Amount == 100 && deliver13.Trust.Amount == 4 &&
          walk.campaign.coins == coins13 + 100 && walk.campaign.publicTrust == trust13 + 4 &&
          Snap(new object[] { walk.campaign.decor, walk.campaign.ownedUpgradeIds, walk.campaign.inventory.displays }) == wallet13 &&
          owned13.All(id => InventoryRules.Owns(walk.campaign.inventory, id)),
        $"T13 {label13}: review, copy pickup and delivery leave the Bag, decor and upgrades as they were; rewards are the fixed 100 coins and +4 regardless of capacity");
}

// ---------- base capacity, protected required papers and the existing Make room rules ----------
var full13 = Rich13(false);
Check(Full13(full13) && prior13.All(id => InventoryRules.Owns(full13, id)), "T13: a Chapter 2 player who kept everything fills the base 3 + 6 exactly");
string fullSnap13 = Snap(full13);
Check(!Level3WorksheetCheck.AcquireSummary(full13, cat13, got6m, null, bea).Ok && Snap(full13) == fullSnap13 &&
      !Level3CertificateRules.HasRoom(full13),
    "T13: on a full base Bag the summary is refused with nothing changed (it stays on the stand); the certificate refuses before any write (Level3CertificateRules.RefuseFull)");
Check(InventoryRules.Remove(full13, cat13, InventoryCatalogue.TaxCodeBook, "rich13/book-back").Changed && !InventoryRules.Owns(full13, InventoryCatalogue.TaxCodeBook) &&
      Level3WorksheetCheck.AcquireSummary(full13, cat13, got6m, null, bea).Ok && InventoryRules.Owns(full13, InventoryCatalogue.BeaSummary),
    "T13: putting the desk book back on its stand (the existing deliberate world return) frees the slot the summary needs");
var after13 = Rich13(false); InventoryRules.Remove(after13, cat13, InventoryCatalogue.TaxCodeBook, "rich13/book-back"); Level3WorksheetCheck.AcquireSummary(after13, cat13, got6m, null, bea);
Check(prior13.Where(id => id != InventoryCatalogue.TaxCodeBook).All(id => InventoryRules.Find(after13, id) == InventoryRules.Find(Rich13(false), id)) &&
      InventoryRules.Owns(after13, InventoryCatalogue.BeaSummary) && Full13(after13),
    "T13: after that put-back every other possession is where it was; the bar and Bag are full again");
int stuck13 = prior13.Count(id => id != InventoryCatalogue.TaxCodeBook) + 2;
Console.WriteLine($"T13 capacity note: kept items with no Level 3 exit + Bea's two papers = {stuck13} for {CampaignInventoryRecord.StartHotbar + CampaignInventoryRecord.StartBag} base slots (KNOWN_ISSUES L3-R106; a Satchel, using a lone voucher or not owning a keepsake avoids it).");
var lean13 = Rich13(false, withBook: true, keepsakes: false);
Check(Level3WorksheetCheck.AcquireSummary(lean13, cat13, got6m, null, bea).Ok && Level3CertificateRules.HasRoom(lean13) && Level3CertificateRules.AcquireItem(lean13, cat13, fixedCredit, bea).Ok &&
      !InventoryRules.Find(lean13, InventoryCatalogue.BeaCertificate).IsNone,
    "T13: a base Bag holding every required Chapter 1/2 record plus the book (no purchases) completes Bea's papers without buying anything");
foreach (string required13 in new[] { InventoryCatalogue.BeaCertificate, InventoryCatalogue.BeaSummary, InventoryCatalogue.CaseFolder, InventoryCatalogue.MarkedRecords, InventoryCatalogue.ProfileBinder })
{
    var hand13 = new CampaignInventoryRecord();
    hand13.hand = new InventorySlot { itemId = required13, quantity = 1 };
    hand13.handSource = "displaced";
    Check(InventoryRules.ReleaseHand(hand13, cat13, true, "rich13/release/" + required13).Outcome == InventoryOutcome.Rejected && hand13.hand.itemId == required13,
        $"T13: '{required13}' cannot be left behind by closing Make room (required records are never discarded)");
}

// ---------- pins: the existing ribbons, readers and stale/foreign states ----------
Check(Level3CertificateRules.AcquireItem(Rich13(true), cat13, fixedCredit, bea).Ok, "T13: (fixture) upgraded Bag takes the certificate");
var pinBag13 = Rich13(true); Level3WorksheetCheck.AcquireSummary(pinBag13, cat13, got6m, null, bea); Level3CertificateRules.AcquireItem(pinBag13, cat13, fixedCredit, bea);
Check(InventoryRules.IsPinnable(pinBag13, cat13, InventoryCatalogue.BeaCertificate) && InventoryRules.IsPinnable(pinBag13, cat13, InventoryCatalogue.BeaSummary) &&
      InventoryRules.Pin(pinBag13, cat13, InventoryCatalogue.BeaSummary).Changed &&
      InventoryRules.Pin(pinBag13, cat13, InventoryCatalogue.BeaCertificate).Message == InventoryRules.PinReplace &&
      InventoryRules.Pin(pinBag13, cat13, InventoryCatalogue.BeaCertificate, 0).Changed && pinBag13.pins.SequenceEqual(new[] { InventoryCatalogue.BeaCertificate, InventoryCatalogue.ReviewedFile, InventoryCatalogue.BeaSummary }) &&
      !InventoryRules.Pin(pinBag13, cat13, InventoryCatalogue.BookmarkSet).Changed && !InventoryRules.Pin(pinBag13, cat13, InventoryCatalogue.ReviewVoucher).Changed,
    "T13: Bea's summary and certificate take the existing three ribbons (replace asks which ribbon); the set itself and stacked supplies cannot be pinned");
var pinSave13 = Clone(live); pinSave13.campaign.inventory = Clone(pinBag13);
var pinBack13 = JsonSerializer.Deserialize<PlayerProgressData>(JsonSerializer.Serialize(pinSave13, json), json); pinBack13.Normalize();
Check(pinBack13.campaign.inventory.pins.SequenceEqual(pinBag13.pins) && InventoryRules.Unpin(pinBack13.campaign.inventory, InventoryCatalogue.ReviewedFile).Changed &&
      InventoryRules.Owns(pinBack13.campaign.inventory, InventoryCatalogue.ReviewedFile),
    "T13: pins survive save/reload; unpinning keeps the record in the Bag");
var stale13 = Clone(pinBag13); stale13.pins.Add("L2-ROSA-001/marked/R1"); stale13.pins.Add("gone_record");
var stalePd13 = Clone(live); stalePd13.campaign.inventory = stale13; stalePd13.Normalize();
Check(!stalePd13.campaign.inventory.pins.Contains("gone_record") && !stalePd13.campaign.inventory.pins.Contains("L2-ROSA-001/marked/R1") &&
      stalePd13.campaign.inventory.pins.SequenceEqual(pinBag13.pins),
    "T13: a pin to a record no longer owned (or a non-item id) is dropped quietly on load; valid pins keep their order");
string hotbar13 = Src("Scripts/UI/HUD/HudHotbar.cs"), viewer13 = Body6(hotbar13, "public bool OpenViewer(string itemId)", "public bool HasSavedRecords(");
string screen13 = Src("Scripts/UI/HUD/InventoryScreen.cs"), saved13 = Src("Scripts/SideQuest1/WorldCollectibles.cs");
Check(viewer13.Contains("case InventoryCatalogue.BeaCertificate:\n                return Level3CertificatePaper.Instance != null ? Level3CertificatePaper.Instance.Toggle() : OpenSavedRecords(itemId);") &&
      viewer13.Contains("case InventoryCatalogue.BeaSummary:\n                return Level3SummaryDocument.Instance != null ? Level3SummaryDocument.Instance.Toggle() : OpenSavedRecords(itemId);") &&
      viewer13.Contains("return LevelTwo && Level2AnnotatedPapers.Instance != null\n                    ? Level2AnnotatedPapers.Instance.OpenForReference() : OpenSavedRecords(itemId);") &&
      Body6(screen13, "private void OnPinTab(int index)", "private void FinishPin(").Contains("if (!hotbar.OpenViewer(pinned)) hotbar.OpenInventory(SlotRef.None, pinned);") &&
      saved13.Contains("This save kept the record references, but did not include a readable paper copy.") &&
      screen13.Contains("Reference: read only, always kept."),
    "T13: a ribbon opens the record's own reader (Bea's certificate/summary papers; earlier chapters' records as read-only saved pages); a record with no saved copy says so instead of inventing text");
// Foreign or unsupported values never enter Bea's sheet; nothing is rebound by matching amount.
DataValue L3v13(string caseId, string source, string amount, string revision = Level3ChapterRules.ContentRevision) =>
    new DataValue(new Level3TransferValue { CaseId = caseId, SourceId = source, Label = "x", Amount = amount, ContentRevision = revision, TaxYear = 2026 }, amount, DataValueType.Number, Level3ValueTransfer.KeyPrefix + source);
var held13 = fixedCredit.Copy();
string why13;
Check(Level3ValueTransfer.TryRead(L3v13(B7r, "worksheet:income", "600000"), held13, out _, out why13) && why13 == null,
    "T13: Bea's own collected summary amount reads into her case");
Check(!Level3ValueTransfer.TryRead(new DataValue(12500f, "12,500", DataValueType.Number, "Level2:Sales"), held13, out _, out why13) && why13 == Level3ValueTransfer.RefuseFloat &&
      !Level3ValueTransfer.TryRead(new DataValue("12500", "12,500", DataValueType.Text, "Rosa:R1"), held13, out _, out why13) && why13 == Level3ValueTransfer.RefuseNotLevel3,
    "T13: values from earlier chapters' readers (float or text fields) are refused with a plain reason");
Check(!Level3ValueTransfer.TryRead(L3v13("L2-ROSA-001", "worksheet:income", "600000"), held13, out _, out why13) && why13 == Level3ValueTransfer.RefuseForeign &&
      !Level3ValueTransfer.TryRead(L3v13(Level3ChapterRules.CompanyCaseId, "paper:M-INV/Total", "12500"), held13, out _, out why13) && why13 == Level3ValueTransfer.RefuseForeign &&
      !Level3ValueTransfer.TryRead(L3v13(B7r, "worksheet:income", "600000", "L3-old"), held13, out _, out why13) && why13 == Level3ValueTransfer.RefuseRevision,
    "T13: another client's (Rosa's, the deferred company's) or another content version's amount cannot transfer into Bea's case");
Check(!Level3ValueTransfer.TryRead(L3v13(B7r, "paper:B-NOTHELD/Amount", "12500"), held13, out _, out why13) && why13 == Level3ValueTransfer.RefuseNotHeld &&
      !Level3ValueTransfer.TryRead(L3v13(B7r, "paper:B-WHT/BWithheldCredit", "12500"), o7r, out _, out why13) && why13 == Level3ValueTransfer.RefuseNotHeld &&
      Level3ValueTransfer.FromPaper(bea, o7r, "B-WHT", "BWithheldCredit") == null,
    "T13: the same 12,500 from a paper Bea never handed over (or before she did) is refused, never rebound to a held source");

// ---------- vouchers: carry two, Bea's clock, one use, failure-atomic, reload ----------
var carry13 = Rich13(true);
string carrySnap13 = Snap(carry13);
Check(InventoryRules.Acquire(carry13, cat13, InventoryCatalogue.ReviewVoucher, 1, "rich13/third").Message == InventoryRules.CarryFull && Snap(carry13) == carrySnap13,
    "T13: a third voucher is refused (carry two) with nothing changed");
string voucherSandbox13 = Path.Combine(Path.GetTempPath(), "TaxVentureVoucher-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(voucherSandbox13);
try
{
    var store13 = new SaveFileStore(Path.Combine(voucherSandbox13, "progress.json"));
    string Encode13(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var published13 = Level3ChapterRules.WithCheckpoint(Clone(live), ext9.Copy());
    store13.Write(Encode13(published13));
    VoucherUseResult Use13(string action)
    {
        var next = Clone(published13);
        Level3ChapterCheckpoint chapter = next.campaign.levelThree.Copy();
        ReviewClockState extended = Level3ReviewRules.ExtendedClock(chapter);
        if (extended == null) return new VoucherUseResult { Message = VoucherRules.ReasonFor(VoucherUseStatus.AlreadyUsed) };
        chapter.Case(B7r).clock = extended;
        VoucherUseResult used = VoucherRules.Use(next.campaign, cat13, Level3ChapterRules.LevelId, B7r, action);
        if (used.Outcome != InventoryOutcome.Applied) return used;
        Level3ChapterRules.WithCheckpoint(next, chapter);
        try { store13.Write(Encode13(next)); } catch (IOException) { return new VoucherUseResult { Outcome = InventoryOutcome.SaveFailed }; } catch (UnauthorizedAccessException) { return new VoucherUseResult { Outcome = InventoryOutcome.SaveFailed }; }
        published13 = next;
        return used;
    }
    int carried13() => InventoryRules.CarriedCount(published13.campaign.inventory, InventoryCatalogue.ReviewVoucher);
    string claim13 = VoucherRules.UseClaimId(published13.campaign.campaignId, Level3ChapterRules.LevelId, B7r);
    VoucherClockView view13 = ReviewClock.VoucherView(published13.campaign.levelThree.Case(B7r).clock, clock9, false);
    Check(VoucherRules.Check(carried13(), B7r, view13, false) == VoucherUseStatus.Ready && carried13() == 2,
        "T13: Bea's running clock and two carried vouchers make the card ready");
    string beforeUse13 = Snap(published13);
    Directory.CreateDirectory(store13.TempPath);
    Check(Use13("voucher/a").Outcome == InventoryOutcome.SaveFailed && Snap(published13) == beforeUse13 && carried13() == 2 &&
          !published13.campaign.rewardClaims.Contains(claim13) && published13.campaign.levelThree.Case(B7r).clock.extensionSeconds == 0,
        "T13: a failed signed write uses no voucher, records no claim and grants no time");
    Directory.Delete(store13.TempPath);
    Check(Use13("voucher/b").Outcome == InventoryOutcome.Applied && carried13() == 1 && published13.campaign.rewardClaims.Contains(claim13) &&
          published13.campaign.levelThree.Case(B7r).clock.remaining == ext9.Case(B7r).clock.remaining + VoucherRules.ExtensionSeconds &&
          published13.campaign.levelThree.Case(B7r).clock.extensionSeconds == 180,
        "T13: the retry uses one voucher, records Bea's claim and adds exactly 180 active seconds in the same write");
    string afterUse13 = Snap(published13);
    var dupNext13 = Clone(published13);
    Check(Use13("voucher/c").Outcome != InventoryOutcome.Applied && Snap(published13) == afterUse13 &&
          VoucherRules.Use(dupNext13.campaign, cat13, Level3ChapterRules.LevelId, B7r, "voucher/d").Outcome == InventoryOutcome.AlreadyApplied &&
          InventoryRules.CarriedCount(dupNext13.campaign.inventory, InventoryCatalogue.ReviewVoucher) == 1,
        "T13: a second use (new request id, duplicate callback) consumes nothing and adds no time");
    using var disk13 = JsonDocument.Parse(File.ReadAllText(store13.FilePath));
    var back13 = JsonSerializer.Deserialize<PlayerProgressData>(disk13.RootElement.GetProperty("payload").GetString(), json); back13.Normalize();
    var backView13 = ReviewClock.VoucherView(back13.campaign.levelThree.Case(B7r).clock, clock9, false);
    Check(back13.campaign.rewardClaims.Contains(claim13) && InventoryRules.CarriedCount(back13.campaign.inventory, InventoryCatalogue.ReviewVoucher) == 1 &&
          VoucherRules.Check(1, B7r, backView13, back13.campaign.rewardClaims.Contains(claim13)) == VoucherUseStatus.AlreadyUsed &&
          VoucherRules.Check(1, B7r, ReviewClock.VoucherView(ok9r.Case(B7r).clock, clock9, true), false) == VoucherUseStatus.CaseDone &&
          back13.campaign.coins == live.campaign.coins && back13.campaign.publicTrust == live.campaign.publicTrust,
        "T13: after reload Bea's voucher is used once (one left, kept); an approved case refuses one; wallet and Trust untouched");
}
finally { Directory.Delete(voucherSandbox13, true); }
Check(Src("Scripts/UI/HUD/VoucherUseCard.cs").Contains("if (chapter == null) return new VoucherUseResult { Message = VoucherRules.ReasonFor(VoucherUseStatus.AlreadyUsed) };") &&
      Index(Src("Scripts/UI/HUD/VoucherUseCard.cs"), "VoucherUseStatus status = Status(hotbar);") < Index(Src("Scripts/UI/HUD/VoucherUseCard.cs"), "CommitLevelThreeVoucherUse("),
    "T13: the card checks Bea's eligibility and builds her extended chapter before any voucher is consumed");

// ---------- decor and the deferred stand/computer ----------
var shop13 = ShopCatalogue.BuiltIn();
var decor13 = Clone(live);
decor13.campaign.ownedUpgradeIds.AddRange(new[] { ShopCatalogue.PencilCup, ShopCatalogue.IslandPrint });
decor13.campaign.decor.Add(new DecorPlacement { itemId = ShopCatalogue.PencilCup, anchorId = "desk_corner", arrived = true });
decor13.campaign.decor.Add(new DecorPlacement { itemId = ShopCatalogue.IslandPrint, anchorId = "wall_above_filing", arrived = false });
string shown13 = Snap(DecorRules.Resolve(decor13.campaign, shop13));
var walked13 = Level3ChapterRules.WithCheckpoint(Level3ChapterRules.WithCheckpoint(Clone(decor13), ok9r.Copy()), delivered10.Copy());
Check(Snap(DecorRules.Resolve(decor13.campaign, shop13)) == shown13 && Snap(DecorRules.Resolve(walked13.campaign, shop13)) == shown13 &&
      DecorRules.Resolve(decor13.campaign, shop13).Select(p => p.itemId + "@" + p.anchorId).SequenceEqual(new[] { "pencil_cup@desk_corner", "island_print@wall_above_filing" }),
    "T13: owned decor resolves to the same two anchors on every load and through Bea's chapter (installed once, never duplicated)");
foreach (string pending13 in new[] { ShopCatalogue.ComparisonStand, ShopCatalogue.OfficeComputer })
{
    var buy13 = Clone(decor13); buy13.campaign.coins = 1000; buy13.campaign.shopIntroduced = true;
    string buyBefore13 = Snap(buy13);
    PurchaseResult bought13 = ShopRules.Purchase(buy13, shop13, cat13, pending13, "t13/" + pending13);
    Check(bought13.Outcome != InventoryOutcome.Applied && ShopRules.StatusOf(buy13, shop13, shop13.Find(pending13), true) == ShopStatus.Teaser && Snap(buy13) == buyBefore13 && DecorRules.Resolve(buy13.campaign, shop13).All(p => p.itemId != pending13),
        $"T13: '{pending13}' stays a pending teaser (no purchase, no coins spent, no playable stand/drawer or computer)");
}
var satchel13 = Clone(live); satchel13.campaign.coins = 200; satchel13.campaign.shopIntroduced = true;
PurchaseResult bag13 = ShopRules.Purchase(satchel13, shop13, cat13, ShopCatalogue.Satchel, "t13/satchel");
Check(bag13.Outcome == InventoryOutcome.Applied && satchel13.campaign.inventory.bagCapacity == 9 && Snap(satchel13.campaign.levelThree) == Snap(live.campaign.levelThree) &&
      satchel13.campaign.publicTrust == live.campaign.publicTrust,
    "T13: a capacity upgrade only adds slots (no chapter, grade or Trust change)");
Console.WriteLine($"T13: {checks - t13Start} checks, {fails - t13Fails} failures (possessions per transition, base/upgraded/full capacity, protected papers, pins/readers, foreign/stale values, vouchers with signed failure/reload, decor/pending purchases; Unity rendering, touch, layering and decor clearance not run).");
// =====================================================================
// T14: Bea's supported finding, Quax's message and the acknowledged Bea-only chapter result
// =====================================================================
int t14Start = checks, t14Fails = fails;
const string Sup14 = Level3ClosingRules.Supported;
// ---------- the real T10 chain: approved -> carried -> explained -> delivered (ClosingFinding) ----------
var carried14 = ok9r.Copy();
Check(Level3ReviewRules.CollectCopy(carried14, out _) == Level3CommandOutcome.Applied &&
      Level3DeliveryRules.Explain(carried14, Level3DeliveryRules.Payable, out _) == Level3CommandOutcome.Applied,
    "T14 fixture: Bea's copy collected and her balance explained (T10 commands)");
var explained14 = carried14.Copy();
var delivered14 = carried14.Copy();
Check(Level3DeliveryRules.Deliver(delivered14, out _) == Level3CommandOutcome.Applied && Level3ChapterRules.PhaseOf(delivered14) == Level3Phase.ClosingFinding &&
      Level3WorksheetCheck.Collected(delivered14) && Level3ReviewRules.CaseDone(delivered14),
    "T14 fixture: the actual delivery reaches ClosingFinding with a collected worksheet and approved computation");
// Frozen v2 first check (nine decisions, one wrong: 8/9 Silver, 59 XP), exactly as T06 freezes it.
void FreezeV2(Level3ChapterCheckpoint c) { var w = c.worksheetCheck; w.assessmentVersion = Level3WorksheetCheck.RevisedAssessmentVersion; w.firstChecked = true; w.firstCorrect = 8; w.firstTotal = 9; w.firstTier = Level3WorksheetCheck.RevisedTierFor(8); w.firstXp = Level3WorksheetCheck.RevisedXpFor(1); }
FreezeV2(delivered14); FreezeV2(explained14);
string deliveredSnap14 = Snap(delivered14);

// ---------- prerequisites: nothing closes before the explained, actual delivery ----------
Check(Level3ClosingRules.StageOf(ok9r) == Level3ClosingRules.Stage.None && Level3ClosingRules.StageOf(explained14) == Level3ClosingRules.Stage.None &&
      Level3ClosingRules.StageOf(null) == Level3ClosingRules.Stage.None && Level3ClosingRules.StageOf(delivered14) == Level3ClosingRules.Stage.Finding,
    "T14: the closing starts only after the explained delivery (approved/at the Auditor/carried/explained are not closable)");
foreach (var pre14 in new[] { ok9r.Copy(), explained14.Copy(), fixedCredit.Copy() })
{
    string before14 = Snap(pre14);
    Check(Level3ClosingRules.RecordFinding(pre14, Sup14, out _) == Level3CommandOutcome.Refused && Level3ClosingRules.ReadMessage(pre14, out _) == Level3CommandOutcome.Refused &&
          Level3ClosingRules.Acknowledge(pre14, out _) == Level3CommandOutcome.Refused && Snap(pre14) == before14,
        $"T14: at {Level3ChapterRules.PhaseOf(pre14)} no finding, message or acknowledgment can be saved");
}
var noFinding14 = delivered14.Copy();
Check(Level3ClosingRules.ReadMessage(noFinding14, out _) == Level3CommandOutcome.Refused && Level3ClosingRules.Acknowledge(noFinding14, out _) == Level3CommandOutcome.Refused &&
      Snap(noFinding14) == deliveredSnap14 && !Level3ChapterRules.IsAllowed(Level3Phase.ClosingFinding, Level3Phase.CompletedOffice),
    "T14: without the saved finding the message and the result cannot be saved; ClosingFinding cannot jump to completion");
var fakeDelivered14 = explained14.Copy(); fakeDelivered14.phase = Level3Phase.ClosingFinding.ToString(); fakeDelivered14.Case(B7r).copyPhase = Level3DeliveryRules.CopyDone;
Check(Level3ClosingRules.StageOf(fakeDelivered14) == Level3ClosingRules.Stage.None && Level3ClosingRules.RecordFinding(fakeDelivered14.Copy(), Sup14, out _) == Level3CommandOutcome.Refused,
    "T14: a ClosingFinding snapshot without the saved delivery action is never closable (no explanation/delivery shortcut)");

// ---------- the finding: only what Bea's own file supports ----------
foreach (string wrong14 in new[] { Level3ClosingRules.Intent, Level3ClosingRules.Pattern, "", null, "everything", "SUPPORTED" })
{
    var w14 = delivered14.Copy();
    Check(Level3ClosingRules.RecordFinding(w14, wrong14, out string why14) == Level3CommandOutcome.Refused && Snap(w14) == deliveredSnap14 &&
          why14 == Level3ClosingRules.RefuseUnsupported,
        $"T14: the statement '{wrong14 ?? "null"}' is not what her file shows: nothing saved, the question stays, no penalty field");
}
var noFile14 = delivered14.Copy(); noFile14.Case(B7r).acquiredSourceIds.Remove("B-DRAFT");
Check(Level3ClosingRules.RecordFinding(noFile14, Sup14, out string noFileWhy14) == Level3CommandOutcome.Refused && noFileWhy14 == Level3ClosingRules.RefuseNoFile,
    "T14: the finding rests on her retained draft and originals (a file without B-DRAFT cannot state it)");
var otherRev14 = delivered14.Copy(); otherRev14.contentRevision = "L3-2026-v0";
var companyActive14 = delivered14.Copy(); companyActive14.activeCaseId = Level3ChapterRules.CompanyCaseId;
Check(Level3ClosingRules.RecordFinding(otherRev14, Sup14, out _) == Level3CommandOutcome.Refused && Level3ClosingRules.RecordFinding(companyActive14, Sup14, out _) == Level3CommandOutcome.Refused,
    "T14: another content revision or another active case cannot record Bea's finding");
var found14 = delivered14.Copy();
Check(Level3ClosingRules.RecordFinding(found14, Sup14, out _) == Level3CommandOutcome.Applied && Level3ChapterRules.PhaseOf(found14) == Level3Phase.ClosingFinding &&
      found14.milestones.Contains(Level3ClosingRules.FindingMilestone) && found14.actionIds.Contains(Level3ClosingRules.FindingAction) &&
      Level3ClosingRules.StageOf(found14) == Level3ClosingRules.Stage.Message && Snap(found14.Case(B7r)) == Snap(delivered14.Case(B7r)) &&
      Snap(found14.worksheetCheck) == Snap(delivered14.worksheetCheck) && Level3ChapterRules.Problem(found14) == null && Level3ChapterRules.Decide(found14) == Level3EntryDecision.Resume,
    "T14: the supported finding is saved as a chapter fact only (phase, case, frozen figures and grades untouched; resumable)");
string foundSnap14 = Snap(found14);
Check(Level3ClosingRules.RecordFinding(found14, Sup14, out _) == Level3CommandOutcome.Duplicate && Level3ClosingRules.RecordFinding(found14, Level3ClosingRules.Intent, out _) == Level3CommandOutcome.Duplicate &&
      Snap(found14) == foundSnap14,
    "T14: a repeated finding callback is a harmless duplicate");
Check(Level3ClosingRules.Acknowledge(found14.Copy(), out _) == Level3CommandOutcome.Refused,
    "T14: the finding alone cannot acknowledge or complete the chapter (the message comes first)");

// ---------- Quax's message: ClosingFinding -> WrapUpPending (the result is pending) ----------
var messaged14 = found14.Copy();
Check(Level3ClosingRules.ReadMessage(messaged14, out _) == Level3CommandOutcome.Applied && Level3ChapterRules.PhaseOf(messaged14) == Level3Phase.WrapUpPending &&
      messaged14.activeCaseId == B7r && messaged14.milestones.Contains(Level3ClosingRules.MessageMilestone) &&
      Level3ClosingRules.StageOf(messaged14) == Level3ClosingRules.Stage.Result && Level3ChapterRules.Problem(messaged14) == null &&
      Level3ChapterRules.Decide(messaged14) == Level3EntryDecision.Resume && !Level3ClosingRules.Completed(messaged14),
    "T14: hearing the message moves the chapter to its pending result (Bea still active, resumable, not complete)");
string messagedSnap14 = Snap(messaged14);
Check(Level3ClosingRules.ReadMessage(messaged14, out _) == Level3CommandOutcome.Duplicate && Level3ClosingRules.RecordFinding(messaged14, Sup14, out _) == Level3CommandOutcome.Duplicate &&
      Snap(messaged14) == messagedSnap14,
    "T14: a repeated message callback changes nothing");

// ---------- the explicit acknowledgment: WrapUpPending -> CompletedOffice ----------
var acked14 = messaged14.Copy();
Check(Level3ClosingRules.Acknowledge(acked14, out _) == Level3CommandOutcome.Applied && Level3ChapterRules.PhaseOf(acked14) == Level3Phase.CompletedOffice &&
      acked14.activeCaseId == B7r && Level3ClosingRules.Completed(acked14) && acked14.milestones.Contains(Level3ClosingRules.AcknowledgedMilestone) &&
      Level3ChapterRules.Problem(acked14) == null && Level3ChapterRules.Decide(acked14) == Level3EntryDecision.Resume && Level3DeliveryRules.Delivered(acked14),
    "T14: Continue moves the chapter to the completed office (Bea's delivered case kept, resumable)");
string ackedSnap14 = Snap(acked14);
Check(Level3ClosingRules.Acknowledge(acked14, out _) == Level3CommandOutcome.Duplicate && Snap(acked14) == ackedSnap14 && Level3ChapterRules.IsAllowed(Level3Phase.CompletedOffice, Level3Phase.WrapUpPending) == false,
    "T14: a repeated Continue is a duplicate; the completed office has no way back into the result");

// ---------- review records / cancel: no command, so the pending step is kept ----------
Check(Level3ClosingRules.StageOf(delivered14.Copy()) == Level3ClosingRules.Stage.Finding && Level3ClosingRules.StageOf(found14.Copy()) == Level3ClosingRules.Stage.Message &&
      Level3ClosingRules.StageOf(messaged14.Copy()) == Level3ClosingRules.Stage.Result,
    "T14: leaving any closing step without its command (Review her records, Talk later, Back, a cancelled message) keeps that same step pending");

// ---------- the chapter result: frozen grades aggregated, never invented ----------
var res14 = Level3ClosingRules.Result(messaged14);
int caseXp14 = messaged14.Case(B7r).review.xp;
Check(res14.WorksheetKnown && res14.WorksheetVersion == Level3WorksheetCheck.RevisedAssessmentVersion && res14.WorksheetCorrect == 8 && res14.WorksheetTotal == 9 &&
      res14.WorksheetTier == 2 && res14.WorksheetXp == 59 && res14.CaseKnown && res14.CaseXp == caseXp14 && res14.Exp == 59 + caseXp14 &&
      Level3ChapterResult.MaxExp == 160 && res14.BadgeId == "worth_the_tax_3",
    $"T14: v2 result: worksheet 8/9 Silver 59 XP + Bea's case {caseXp14} XP of 160, the Worth the Tax? badge");
var legacy14 = messaged14.Copy();
{ var w = legacy14.worksheetCheck; w.assessmentVersion = Level3WorksheetCheck.AssessmentVersion; w.firstCorrect = 11; w.firstTotal = 12; w.firstTier = Level3WorksheetCheck.TierFor(11); w.firstXp = Level3WorksheetCheck.XpFor(1); }
var legacyRes14 = Level3ClosingRules.Result(legacy14);
Check(legacyRes14.WorksheetKnown && legacyRes14.WorksheetVersion == "L3-worksheet-v1" && legacyRes14.WorksheetCorrect == 11 && legacyRes14.WorksheetTotal == 12 &&
      legacyRes14.WorksheetTier == 2 && legacyRes14.WorksheetXp == 59 && legacyRes14.Exp == 59 + caseXp14,
    "T14: a legacy v1 grade keeps its original twelve-decision result (11/12 Silver 59 XP), never regraded as v2");
var unknown14 = messaged14.Copy(); unknown14.worksheetCheck.firstChecked = false; unknown14.worksheetCheck.firstTier = 0; unknown14.worksheetCheck.firstXp = 0;
var unknownRes14 = Level3ClosingRules.Result(unknown14);
var hacked14 = messaged14.Copy(); hacked14.worksheetCheck.firstXp = 999; hacked14.Case(B7r).review.xp = 999;
Check(!unknownRes14.WorksheetKnown && unknownRes14.WorksheetXp == 0 && unknownRes14.WorksheetTier == 0 && unknownRes14.Exp == caseXp14 &&
      Level3ClosingRules.Result(hacked14).Exp == 160,
    "T14: a missing historical grade stays unknown (no invented XP or badge); out-of-range saved values cannot exceed the 160 XP maximum");

// ---------- once-only wrap-up: completion + mastery; no coins, Trust or second payout ----------
PlayerProgressData Paid14(Level3ChapterCheckpoint state)
{
    var save = Level3ChapterRules.WithCheckpoint(Clone(live), state.Copy());
    Level3WorksheetCheck.ApplyAcquisitionGrants(save, state);
    Level3ReviewRules.ApplyApprovalGrants(save, state);
    Level3DeliveryRules.ApplyDeliveryGrants(save, state);
    return save;
}
var story14 = Paid14(acked14);
var sum14 = CaseResultRules.BuildChapterSummary(story14, CampaignRewards.Level3);
Check(sum14.CoinsEarned == 160 && sum14.TrustEarned == 6 && story14.campaign.coins == live.campaign.coins + 160 && story14.campaign.publicTrust == live.campaign.publicTrust + 6,
    "T14 fixture: the Bea-only activities already paid their chapter totals (160 coins, +6 Public Trust) before the wrap-up");
int coins14 = story14.campaign.coins, trust14 = story14.campaign.publicTrust, exp14 = story14.totalExp, ledger14 = story14.campaign.ledger.Count;
string claims14 = string.Join("|", story14.campaign.rewardClaims), inv14 = Snap(story14.campaign.inventory), rosa14 = Snap(story14.campaign.rosaEthics);
string company14 = Snap(acked14.Case(Level3ChapterRules.CompanyCaseId)), l2Record14 = Snap(story14.GetRecord("Level_02"));
var notYet14 = Paid14(messaged14);
var notYetGrant14 = Level3ClosingRules.ApplyWrapUp(notYet14, messaged14);
Check(!notYetGrant14.Changed && !notYetGrant14.AlreadyDone && !notYet14.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) && notYet14.GetRecord(Level3ChapterRules.LevelId) == null,
    "T14: the pending result (finding + message, no Continue) never completes the chapter or writes mastery");
var wrap14 = Level3ClosingRules.ApplyWrapUp(story14, acked14);
Check(wrap14.Changed && wrap14.ChapterCompleted && story14.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) &&
      CampaignRewards.IsActivityCompleted(story14, CampaignRewards.Level3, Level3ClosingRules.WrapUpActivity) &&
      story14.GetRecord(Level3ChapterRules.LevelId)?.bestExp == 59 + caseXp14 && story14.GetRecord(Level3ChapterRules.LevelId).maxExp == 160 &&
      story14.totalExp == exp14 + 59 + caseXp14 && wrap14.ExpGain == 59 + caseXp14 && story14.GetRecord(Level3ChapterRules.LevelId).badges.Contains("worth_the_tax_3"),
    "T14: Continue completes Level 3 once with one aggregated mastery record (worksheet + case XP, best kept, badge)");
Check(story14.campaign.coins == coins14 && story14.campaign.publicTrust == trust14 && story14.campaign.ledger.Count == ledger14 &&
      string.Join("|", story14.campaign.rewardClaims) == claims14 && Snap(story14.campaign.inventory) == inv14 && Snap(story14.campaign.rosaEthics) == rosa14 &&
      Snap(story14.GetRecord("Level_02")) == l2Record14 && Snap(story14.campaign.levelThree.Case(Level3ChapterRules.CompanyCaseId)) == company14 &&
      !story14.campaign.levelThree.Case(Level3ChapterRules.CompanyCaseId).admitted,
    "T14: the wrap-up pays no coins or Trust and never touches the wallet, ledger, claims, possessions, Rosa history, earlier records or the retained company subrecord");
var again14 = Level3ClosingRules.ApplyWrapUp(story14, acked14);
Check(!again14.Changed && again14.AlreadyDone && story14.totalExp == exp14 + 59 + caseXp14 && story14.campaign.coins == coins14 &&
      story14.GetRecord(Level3ChapterRules.LevelId).attempts == 1 &&
      Level3WorksheetCheck.ApplyAcquisitionGrants(story14, acked14).AlreadyClaimed && Level3ReviewRules.ApplyApprovalGrants(story14, acked14).AlreadyClaimed &&
      Level3DeliveryRules.ApplyDeliveryGrants(story14, acked14).AlreadyClaimed && story14.campaign.coins == coins14 && story14.campaign.publicTrust == trust14,
    "T14: a repeated Continue/reload adds no XP, attempt, coins or Trust; the worksheet, approval and delivery claims stay claimed");
var tierKept14 = Paid14(acked14); tierKept14.RecordBadge("worth_the_tax_3", 3);
Level3ClosingRules.ApplyWrapUp(tierKept14, acked14);
var bestKept14 = Paid14(acked14);
bestKept14.levelRecords.Add(new LevelRecord { levelId = Level3ChapterRules.LevelId, bestExp = 160, maxExp = 160, passed = true, attempts = 1 });
int bestExpBefore14 = bestKept14.totalExp;
var bestGrant14 = Level3ClosingRules.ApplyWrapUp(bestKept14, acked14);
Check(tierKept14.badgeRecords.Find(b => b.badgeId == "worth_the_tax_3").tier == 3 && bestGrant14.ExpGain == 0 && bestKept14.totalExp == bestExpBefore14 &&
      bestKept14.GetRecord(Level3ChapterRules.LevelId).bestExp == 160,
    "T14: an earned better badge tier or best XP already on record is preserved (no downgrade, no double count)");
var legacyStory14 = Paid14(Acked14(legacy14));
Level3ChapterCheckpoint Acked14(Level3ChapterCheckpoint pending) { var c = pending.Copy(); Level3ClosingRules.Acknowledge(c, out _); return c; }
int legacyExp14 = legacyStory14.totalExp;
Check(Level3ClosingRules.ApplyWrapUp(legacyStory14, Acked14(legacy14)).Changed && legacyStory14.totalExp == legacyExp14 + 59 + caseXp14 &&
      legacyStory14.campaign.IsChapterCompleted(Level3ChapterRules.LevelId),
    "T14: a legacy v1 chapter completes with its original grade aggregated once");
var noCampaign14 = new PlayerProgressData();
Check(!Level3ClosingRules.ApplyWrapUp(noCampaign14, acked14).Changed && !Level3ClosingRules.ApplyWrapUp(null, acked14).Changed,
    "T14: nothing is applied without a campaign");

// ---------- Bea-only: no company prerequisite; the deferred company closing is not this route ----------
Check(!acked14.Case(Level3ChapterRules.CompanyCaseId).admitted && !acked14.actionIds.Any(a => a.Contains("marine") || a.Contains("company")) &&
      !story14.campaign.completedActivityIds.Any(a => a.Contains("marine")),
    "T14: a fresh Bea-only run closes without any company admission, work or claim");
var companyRoute14 = delivered14.Copy(); companyRoute14.Case(Level3ChapterRules.CompanyCaseId).admitted = true; companyRoute14.activeCaseId = Level3ChapterRules.CompanyCaseId;
Check(Level3ClosingRules.StageOf(companyRoute14) == Level3ClosingRules.Stage.None && Level3ChapterRules.IsAllowed(Level3Phase.CompanyDelivery, Level3Phase.ClosingFinding),
    "T14: a (deferred) company-admitted closing snapshot is retained but never completed through Bea's route; its transition stays defined");

// ---------- run-mode isolation ----------
foreach (CampaignRunMode mode14 in new[] { CampaignRunMode.MasteryReplay, CampaignRunMode.Practice, CampaignRunMode.DirectEditor, CampaignRunMode.None })
    Check(!Level3ChapterRules.UsesStoryCheckpoint(mode14, false, live), $"T14: {mode14} never writes the closing (its acknowledgment publishes locally only)");
string ppm14 = Src("Scripts/RewardsSystem/PlayerProgressManager.cs");
string wrapBody14 = Body6(ppm14, "public bool CommitLevelThreeWrapUp(", "/// <summary>");
Check(wrapBody14.Contains("if (!UsesLevelThreeStory) return false;") && Index(wrapBody14, "ApplyWrapUp(candidate, state)") < Index(wrapBody14, "TryWriteCandidate(candidate)") &&
      wrapBody14.Contains("if (!due.Changed && !due.AlreadyDone) return false;") && !wrapBody14.Contains("Grant("),
    "T14: the story wrap-up is one candidate written before anything goes live; non-story runs write nothing; it grants nothing");

// ---------- signed save: failure, retry and reload at every closing boundary ----------
string sandbox14 = Path.Combine(Path.GetTempPath(), "TaxVentureClosing-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox14);
try
{
    var store14 = new SaveFileStore(Path.Combine(sandbox14, "progress.json"));
    string Encode14(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    PlayerProgressData Reload14()
    {
        using var disk = JsonDocument.Parse(File.ReadAllText(store14.FilePath));
        string payload = disk.RootElement.GetProperty("payload").GetString();
        if (disk.RootElement.GetProperty("signature").GetString() != SaveFileStore.Sign(payload)) return null;
        var back = JsonSerializer.Deserialize<PlayerProgressData>(payload, json);
        back.Normalize();
        return back;
    }
    var published14 = Paid14(delivered14);
    store14.Write(Encode14(published14));
    int pubCoins14 = published14.campaign.coins, pubTrust14 = published14.campaign.publicTrust, pubExp14 = published14.totalExp;
    // Mirrors Level3Checkpointer: candidate -> (wrap-up grants on the same candidate) -> signed write -> publish.
    bool Step14(Func<Level3ChapterCheckpoint, Level3CommandOutcome> command, bool wrapUp)
    {
        var next = Clone(published14);
        Level3ChapterCheckpoint chapter = next.campaign.levelThree.Copy();
        if (command(chapter) != Level3CommandOutcome.Applied) return false;
        next = Level3ChapterRules.WithCheckpoint(next, chapter);
        if (wrapUp) { var due = Level3ClosingRules.ApplyWrapUp(next, chapter); if (!due.Changed && !due.AlreadyDone) return false; }
        try { store14.Write(Encode14(next)); } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
        published14 = next;
        return true;
    }
    Check(Level3ClosingRules.StageOf(Reload14().campaign.levelThree) == Level3ClosingRules.Stage.Finding,
        "T14 reload: a saved delivery resumes at Bea's draft question (nothing closed yet)");
    // Finding
    string beforeFinding14 = Snap(published14);
    byte[] diskFinding14 = File.ReadAllBytes(store14.FilePath);
    Directory.CreateDirectory(store14.TempPath);
    Check(!Step14(c => Level3ClosingRules.RecordFinding(c, Sup14, out _), false) && Snap(published14) == beforeFinding14 && File.ReadAllBytes(store14.FilePath).SequenceEqual(diskFinding14),
        "T14: a failed finding save keeps her question open and the disk unchanged");
    Directory.Delete(store14.TempPath);
    Check(!Step14(c => Level3ClosingRules.RecordFinding(c, Level3ClosingRules.Intent, out _), false) && Snap(published14) == beforeFinding14,
        "T14: an unsupported statement never reaches the save");
    Check(Step14(c => Level3ClosingRules.RecordFinding(c, Sup14, out _), false) && !Step14(c => Level3ClosingRules.RecordFinding(c, Sup14, out _), false),
        "T14: the finding retry saves once; a duplicate writes nothing");
    var foundBack14 = Reload14();
    Check(Level3ClosingRules.StageOf(foundBack14.campaign.levelThree) == Level3ClosingRules.Stage.Message && Level3ChapterRules.ImpliedTriggers(foundBack14.campaign.levelThree).Contains(Level3ChapterRules.FindingTrigger) &&
          !Level3ChapterRules.ImpliedTriggers(foundBack14.campaign.levelThree).Contains(Level3ChapterRules.MessageTrigger),
        "T14 reload after the finding: the message waits at reception; Notes ticks the finding only");
    // Message
    string beforeMessage14 = Snap(published14);
    byte[] diskMessage14 = File.ReadAllBytes(store14.FilePath);
    Directory.CreateDirectory(store14.TempPath);
    Check(!Step14(c => Level3ClosingRules.ReadMessage(c, out _), false) && Snap(published14) == beforeMessage14 && File.ReadAllBytes(store14.FilePath).SequenceEqual(diskMessage14) &&
          Level3ClosingRules.StageOf(Reload14().campaign.levelThree) == Level3ClosingRules.Stage.Message,
        "T14: a failed message save keeps the message waiting at reception");
    Directory.Delete(store14.TempPath);
    Check(Step14(c => Level3ClosingRules.ReadMessage(c, out _), false) && !Step14(c => Level3ClosingRules.ReadMessage(c, out _), false),
        "T14: the message retry saves once; a duplicate writes nothing");
    var pendingBack14 = Reload14();
    Check(Level3ClosingRules.StageOf(pendingBack14.campaign.levelThree) == Level3ClosingRules.Stage.Result && !pendingBack14.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) &&
          pendingBack14.campaign.coins == pubCoins14 && pendingBack14.campaign.publicTrust == pubTrust14 && pendingBack14.totalExp == pubExp14,
        "T14 reload with the result pending: the card shows again; nothing is completed or paid yet");
    // Acknowledgment
    string beforeAck14 = Snap(published14);
    byte[] diskAck14 = File.ReadAllBytes(store14.FilePath);
    Directory.CreateDirectory(store14.TempPath);
    Check(!Step14(c => Level3ClosingRules.Acknowledge(c, out _), true) && Snap(published14) == beforeAck14 && File.ReadAllBytes(store14.FilePath).SequenceEqual(diskAck14) &&
          !Reload14().campaign.IsChapterCompleted(Level3ChapterRules.LevelId) && Level3ClosingRules.StageOf(Reload14().campaign.levelThree) == Level3ClosingRules.Stage.Result,
        "T14: a failed acknowledgment save leaves the result pending and retryable (no completion, XP or disk change)");
    Directory.Delete(store14.TempPath);
    Check(Step14(c => Level3ClosingRules.Acknowledge(c, out _), true) && !Step14(c => Level3ClosingRules.Acknowledge(c, out _), true),
        "T14: the acknowledgment retry saves once; a repeated Continue writes nothing");
    var doneBack14 = Reload14();
    Check(doneBack14 != null && Level3ClosingRules.Completed(doneBack14.campaign.levelThree) && doneBack14.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) &&
          doneBack14.totalExp == pubExp14 + 59 + caseXp14 && doneBack14.campaign.coins == pubCoins14 && doneBack14.campaign.publicTrust == pubTrust14 &&
          Snap(doneBack14.campaign.inventory) == Snap(published14.campaign.inventory) && Snap(doneBack14.campaign.rosaEthics) == Snap(live.campaign.rosaEthics) &&
          doneBack14.campaign.levelThree.milestones.Contains(Level3ClosingRules.FindingMilestone) && doneBack14.campaign.levelThree.milestones.Contains(Level3ClosingRules.MessageMilestone) &&
          Level3ClosingRules.Acknowledge(doneBack14.campaign.levelThree.Copy(), out _) == Level3CommandOutcome.Duplicate &&
          Level3ClosingRules.ApplyWrapUp(doneBack14, doneBack14.campaign.levelThree).AlreadyDone && doneBack14.totalExp == pubExp14 + 59 + caseXp14 &&
          Level3ChapterRules.ImpliedTriggers(doneBack14.campaign.levelThree).Contains(Level3ChapterRules.WrapUpTrigger),
        "T14 reload after Continue: completed once with finding/message facts, wallet/Trust/possessions/Rosa history kept, no repeat on reload");
}
finally { Directory.Delete(sandbox14, true); }

// ---------- the completed office: no review, copy, case editing, clock or reward loop ----------
Check(Level3ReviewRules.Submit(acked14.Copy(), bea, rc9, out _, out _) != Level3CommandOutcome.Applied && Level3ReviewRules.CollectCopy(acked14.Copy(), out _) == Level3CommandOutcome.Refused &&
      Level3DeliveryRules.Deliver(acked14.Copy(), out _) == Level3CommandOutcome.Duplicate && Level3DeliveryRules.Explain(acked14.Copy(), Level3DeliveryRules.Payable, out _) == Level3CommandOutcome.Duplicate &&
      Level3ComputationRules.SelectRule(acked14.Copy(), bea, "IND-20", out _) == Level3CommandOutcome.Refused &&
      Level3ReviewRules.StartClock(acked14.Copy(), rc9, out _) != Level3CommandOutcome.Applied && Level3ReviewRules.CaseDone(acked14) &&
      Level3DeliveryRules.StageOf(acked14) == Level3DeliveryRules.Stage.Delivered && acked14.Case(B7r).copyPhase == Level3DeliveryRules.CopyDone &&
      Level3ChapterRules.CoarseState(Level3Phase.CompletedOffice) == null,
    "T14: in the completed office no hand-in, copy pickup/delivery, computation edit or review clock can start again");
Check(Src("Scripts/Level3/Level3CaseFolder.cs").Contains("copy == Level3ReviewRules.CopyAtAuditor || copy == Level3DeliveryRules.CopyDone") &&
      Src("Scripts/Level3/Level3Checkpointer.cs").Contains("if (!IsReady || Blocked || State == null || Level3ReviewRules.CaseDone(State)) return ReviewClock.Tick.None;"),
    "T14: the delivered folder stays out of the bar and Bea's approved clock never ticks in the completed office");

// ---------- older saves: a T10 ClosingFinding save has nothing to migrate ----------
var oldClosing14 = JsonNode.Parse(JsonSerializer.Serialize(Level3ChapterRules.WithCheckpoint(Clone(live), delivered14.Copy()), json));
var oldBack14 = JsonSerializer.Deserialize<PlayerProgressData>(oldClosing14.ToJsonString(), json);
oldBack14.Normalize();
Check(Level3ClosingRules.StageOf(oldBack14.campaign.levelThree) == Level3ClosingRules.Stage.Finding && Level3ChapterRules.Problem(oldBack14.campaign.levelThree) == null,
    "T14: a T10 save at ClosingFinding resumes at Bea's draft question with no new field or migration (finding/message are action ids)");

// ---------- quiet restore: triggers and lessons from saved facts only ----------
Check(!Level3ChapterRules.ImpliedTriggers(delivered14).Contains(Level3ChapterRules.FindingTrigger) &&
      Level3ChapterRules.ImpliedTriggers(messaged14).Contains(Level3ChapterRules.MessageTrigger) && !Level3ChapterRules.ImpliedTriggers(messaged14).Contains(Level3ChapterRules.WrapUpTrigger) &&
      Level3ChapterRules.ImpliedLessons(found14).Contains(Level3ChapterRules.ClosingFindingLesson) && !Level3ChapterRules.ImpliedLessons(found14).Contains(Level3ChapterRules.ClosingMessageLesson) &&
      Level3ChapterRules.ImpliedLessons(messaged14).Contains(Level3ChapterRules.ClosingMessageLesson),
    "T14: a resume ticks the finding/message/result objectives and retires their hints only from the saved closing step");
var steps14 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps14.Count == 14 && steps14[11].GetProperty("trigger").GetString() == Level3ChapterRules.FindingTrigger &&
      steps14[12].GetProperty("trigger").GetString() == Level3ChapterRules.MessageTrigger && steps14[13].GetProperty("trigger").GetString() == Level3ChapterRules.WrapUpTrigger &&
      new[] { Level3ChapterRules.FindingTrigger, Level3ChapterRules.MessageTrigger, Level3ChapterRules.WrapUpTrigger }.All(t => ObjectiveTriggers.Validate(t) == null) &&
      steps14.Skip(11).All(st => st.GetProperty("text").GetString().Split(' ').Length <= 8),
    "T14: three short Notes steps (one active task each) observe the saved finding, message and acknowledgment");
var lessons14 = Json("Tutorials/Level_03/guide.json").GetProperty("lessons").EnumerateArray().ToList();
var finLesson14 = lessons14.First(l => l.GetProperty("id").GetString() == Level3ChapterRules.ClosingFindingLesson);
var msgLesson14 = lessons14.First(l => l.GetProperty("id").GetString() == Level3ChapterRules.ClosingMessageLesson);
Check(finLesson14.GetProperty("when").GetString() == "flag:closingFinding" && msgLesson14.GetProperty("when").GetString() == "flag:findingRecorded" &&
      new[] { finLesson14, msgLesson14 }.All(l => l.GetProperty("pages").GetArrayLength() == 1 && l.GetProperty("pages")[0].GetProperty("text").GetString().Split(' ').Length <= 14) &&
      Src("Scripts/Level3/Level3Checkpointer.cs").Contains("case \"closingFinding\": return Level3ClosingRules.StageOf(State) == Level3ClosingRules.Stage.Finding;"),
    "T14: two one-line guide hints (Arthur) wait on saved closing flags; no lecture, no new guide actor");

// ---------- content: what the file supports, with figures matching Bea's case ----------
Level3TownFile town14 = JsonSerializer.Deserialize<Level3TownFile>(File.ReadAllText(Res("Levels/Level_03/town_life.json")), json);
Level3ClientContent bea14 = town14.bea;
int Words14(string t) => (t ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
string Peso14(string canonical) => "₱" + decimal.Parse(canonical, System.Globalization.CultureInfo.InvariantCulture).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);
var draftLines14 = Level3ClosingRules.UnsupportedDraftLines(bea.level3);
var supported14 = bea14.findingChoices.First(c => c.id == Sup14);
Check(Level3TownContent.Validate(town14).Count == 0 && Level3ClosingRules.DraftOverstatement(bea.level3) == "54000.00" &&
      draftLines14.Count == 2 && draftLines14.All(l => supported14.text.Contains(Peso14(l.amount))) &&
      bea14.findingQuestion.Contains(Peso14(bea.level3.totals.First(t => t.sourceId == "B-DRAFT").amount)) &&
      bea14.findingQuestion.Contains(Peso14(bea.level3.totals.First(t => t.sourceId == "B-EXP").amount)) &&
      supported14.reply.Contains("₱54,000") && Words14(bea14.findingQuestion) <= 30,
    "T14: the finding's figures are her file's own (draft 354,000 vs schedule 300,000; the 24,000 and 30,000 draft lines; 54,000 corrected); content validates");
string closingText14 = string.Join(" ", new[] { bea14.findingQuestion, bea14.findingNext, bea14.afterFindingLine }.Concat(bea14.findingChoices.SelectMany(c => new[] { c.text, c.reply })))
    + " " + string.Join(" ", town14.reception.quaxMessage.Select(l => l.text));
Check(!System.Text.RegularExpressions.Regex.IsMatch(closingText14, "Assistance Desk|fraud|lied|cheat|intentional|deliberately|every office|all drafts are|Marine|Nina|company", System.Text.RegularExpressions.RegexOptions.IgnoreCase) &&
      bea14.findingChoices.First(c => c.id == Level3ClosingRules.Intent).reply.Contains("not why") &&
      bea14.findingChoices.First(c => c.id == Level3ClosingRules.Pattern).reply.Contains("only covers my own draft") && supported14.reply.Contains("not why"),
    "T14: the lines separate the discrepancy and its correction from intent and a wider pattern; no Assistance Desk/company claim, accusation or invented evidence");
var message14 = Level3TownContent.MessageDialogue(town14.reception);
Check(Level3TownContent.ValidateReception(town14.reception).Count == 0 && message14.Count == 4 && message14.Count(l => l.Speaker == DialogueSpeaker.Player) == 1 &&
      message14.Where(l => l.Speaker == DialogueSpeaker.Npc).All(l => l.SpeakerName == "Receptionist") && closingText14.Contains("Quax") &&
      town14.reception.quaxMessage.All(l => Words14(l.text) <= 20) &&
      !System.Text.RegularExpressions.Regex.IsMatch(string.Join(" ", town14.reception.quaxMessage.Select(l => l.text)), "Trust|coin|reward|Arthur", System.Text.RegularExpressions.RegexOptions.IgnoreCase) &&
      Level3TownContent.ValidateReception(new Level3ReceptionContent()).Count == 0,
    "T14: the receptionist passes on Quax's short message; one player acknowledgment, no choice, Trust or reward; built-in fallback valid");
var builtIn14 = new Level3ClientContent();
Check(builtIn14.findingQuestion == bea14.findingQuestion && builtIn14.findingChoices.Select(c => c.id + c.text + c.reply).SequenceEqual(bea14.findingChoices.Select(c => c.id + c.text + c.reply)) &&
      new Level3ReceptionContent().quaxMessage.Select(l => l.speaker + l.text).SequenceEqual(town14.reception.quaxMessage.Select(l => l.speaker + l.text)),
    "T14: the built-in fallback lines mirror town_life.json");

// ---------- wiring at the actual call sites ----------
string cp14 = Src("Scripts/Level3/Level3Checkpointer.cs");
string ackBody14 = Body6(cp14, "public TapeCommit CommitAcknowledgment(", "/// <summary>");
string findBody14 = Body6(cp14, "public TapeCommit CommitFinding(", "/// <summary>");
string msgBody14 = Body6(cp14, "public TapeCommit CommitMessage(", "/// <summary>");
Check(Index(ackBody14, "Level3ClosingRules.Acknowledge(candidate") < Index(ackBody14, "CommitLevelThreeWrapUp(candidate") && Index(ackBody14, "CommitLevelThreeWrapUp(candidate") < Index(ackBody14, "Publish(candidate);") &&
      Index(findBody14, "RecordFinding(candidate") < Index(findBody14, "CommitLevelThreeCheckpoint(candidate)") && Index(findBody14, "CommitLevelThreeCheckpoint(candidate)") < Index(findBody14, "Publish(candidate);") &&
      Index(msgBody14, "ReadMessage(candidate") < Index(msgBody14, "CommitLevelThreeCheckpoint(candidate)") && Index(msgBody14, "CommitLevelThreeCheckpoint(candidate)") < Index(msgBody14, "Publish(candidate);") &&
      cp14.Contains("if (!hadFinding && Level3ClosingRules.FindingRecorded(committed)) GameplayEvents.RaiseFindingRecorded();") &&
      cp14.Contains("if (before == Level3Phase.ClosingFinding && Phase == Level3Phase.WrapUpPending) GameplayEvents.RaiseChapterMessageRead();") &&
      cp14.Contains("if (before == Level3Phase.WrapUpPending && Phase == Level3Phase.CompletedOffice) GameplayEvents.RaiseChapterWrapUpConfirmed();"),
    "T14: finding, message and acknowledgment are candidate -> signed write -> publish -> objective/notes event");
string tick14 = Body6(cp14, "private void TickWrapUp()", "/// <summary>");
Check(tick14.Contains("Level3ClosingRules.StageOf(State) != Level3ClosingRules.Stage.Result") && tick14.Contains("DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen") &&
      tick14.Contains("resultShown = LevelCompleteUI.Instance.ShowLevelThreeChapter();") && Body6(cp14, "private void Update()", "private void OnApplicationPause").Contains("TickWrapUp();") &&
      Body6(cp14, "public bool MessagePending =>", "public System.Collections.Generic.List<DialogueLine> MessageLines()").Contains("pendingCandidate == null && Level3ClosingRules.StageOf(State) == Level3ClosingRules.Stage.Message"),
    "T14: the one result card appears only once nothing else holds the screen (also after a reload); the message waits only at its saved step");
string lcu14 = Src("Scripts/UI/LevelCompleteUI.cs");
string cont14 = Body6(lcu14, "private void ContinueLevelThree()", "/// <summary>");
Check(Index(cont14, "if (leaving) return;") < Index(cont14, "owner.CommitAcknowledgment(") && Index(cont14, "owner.CommitAcknowledgment(") < Index(cont14, "LeaveForCareer();") &&
      cont14.Contains("if (outcome == Level3Checkpointer.TapeCommit.SaveFailed || outcome == Level3Checkpointer.TapeCommit.Refused)") &&
      Body6(cont14, "if (outcome == Level3Checkpointer.TapeCommit.SaveFailed", "levelThreeChapter = false;").Contains("leaving = false;") &&
      Body6(lcu14, "public void OnReturnToMainModulePressed()", "private void LeaveForCareer()").Contains("if (levelThreeChapter) { ContinueLevelThree(); return; }") &&
      !Body6(lcu14, "private RewardScreenData BuildChapterThreeScreen(", "/// <summary>").Contains("STAY IN OFFICE") &&
      !Body6(lcu14, "private RewardScreenData BuildChapterThreeScreen(", "/// <summary>").Contains("Grant(") && !cont14.Contains("ApplyLevelResult"),
    "T14: the shared Continue acknowledges before Career; a failure keeps the card with a retry; no stay-in-office exit or reward mutation");
string hrs14 = Src("Scripts/UI/HUD/HudRewardScreen.cs");
Check(Body6(hrs14, "private void LayoutButtons(", "/// <summary>").Contains("bool two = continueButton != null && data.Secondary != null && !string.IsNullOrEmpty(data.SecondaryLabel);") &&
      Body6(hrs14, "private void LayoutButtons(", "/// <summary>").Contains("two ? (ButtonWidth + ButtonGap) * 0.5f : 0f") &&
      Body6(hrs14, "private void BuildSecondaryButton()", "private static void SkinButton").Contains("if (animating || revealHeld || secondaryAction == null) return;") &&
      Body6(hrs14, "private void BuildSecondaryButton()", "private static void SkinButton").Contains("enableAutoSizing = false"),
    "T14: the existing result card gains an optional second choice only when one is given (every other card keeps its single centred button)");
string rec14 = Src("Scripts/Interactables/ReceptionistInteractable.cs");
string startMsg14 = Body6(rec14, "private void StartLevelMessage()", "private void OnConversationCancelled(");
Check(Index(rec14, "if (MessageWaiting)") > Index(rec14, "if (dialogueSessionActive) return;") && Index(rec14, "if (MessageWaiting)") < Index(rec14, "if (spokenForCaseId == def.caseId)") &&
      startMsg14.Contains("if (ReferenceEquals(LevelMessage, owner)) owner.MessageHeard();") && startMsg14.Contains("() => OnConversationCancelled(priorNpcState)") &&
      !startMsg14.Contains("RaiseNotesUnlockRequested") && !startMsg14.Contains("ReceiveCaseState") &&
      cp14.Contains("ReceptionistInteractable.LevelMessage = this;") && cp14.Contains("if (ReferenceEquals(ReceptionistInteractable.LevelMessage, this)) ReceptionistInteractable.LevelMessage = null;"),
    "T14: the existing receptionist passes on the message through one opt-in seam; only a completed message is saved; cancelling saves nothing; no reception assignment replays");
foreach (string l12 in new[] { "Scripts/Level2/Level2OfficeStart.cs", "Scripts/Level2/Level2Checkpointer.cs", "Scripts/UI/Level1SceneController.cs" })
    Check(!Src(l12).Contains("LevelMessage") && !Src(l12).Contains("SecondaryLabel"), $"T14: {l12} never sets the receptionist message seam or a second card choice");
string bea14src = Src("Scripts/Level3/Level3BeaInteractable.cs");
string state14 = Body6(bea14src, "private void StateFinding(string id)", "/// <summary>");
string review14 = Body6(bea14src, "private void ReviewRecords()", "private IEnumerator OpenRecordsWhenFree()");
Check(bea14src.Contains("case Level3DeliveryRules.Stage.Delivered: ClosingTalk(chapter, bea); break;") && bea14src.Contains("case FindingAction: ShowFindingChoices(); break;") &&
      Index(state14, "if (id != Level3ClosingRules.Supported)") < Index(state14, "owner.CommitFinding(id") && Index(state14, "owner.CommitFinding(id") < Index(state14, "builder.Npc(choice.reply") &&
      state14.Contains("ShowFindingChoices();\n            return;") && !review14.Contains("Commit") && review14.Contains("reviewAfter = true;") &&
      Body6(bea14src, "private IEnumerator OpenRecordsWhenFree()", "// ClientCopyHandover.ILevelDelivery").Contains("bar.OpenViewer(InventoryCatalogue.CaseFolder)") &&
      Body6(bea14src, "private void End()", "/// <summary>").Contains("if (reviewAfter)") && Body6(bea14src, "private void OnDisable()", "public void OnInteract()").Contains("reviewAfter = false;"),
    "T14: her draft question waits on a statement; unsupported ones get one reply and the same question; the supported one is saved before her reply; Review her records saves nothing and opens the existing Case Folder reader after the talk ends");
foreach (string file14 in new[] { "Level3ClosingRules.cs", "Level3BeaInteractable.cs" })
{
    string code14 = System.Text.RegularExpressions.Regex.Replace(Src("Scripts/Level3/" + file14), @"///.*|//.*", "");
    Check(!code14.Contains("CoinsKind") && !code14.Contains("TrustKind") && !code14.Contains("CompanyAppointment") && !code14.Contains("float.Parse") &&
          !code14.Contains("expectedFields") && !code14.Contains("worksheetDecisions") && !code14.Contains("coins ="),
        $"T14: {file14}: no coin/Trust grant, company admission, float money or answer key");
}
Console.WriteLine($"T14: {checks - t14Start} checks, {fails - t14Fails} failures (prerequisites, finding/message/acknowledgment commands, frozen v1/v2 aggregation, once-only completion, signed failure/retry/reload per boundary, isolation, completed office, content, wiring; Unity rendering, NPC/dialogue, card layout, touch and audio not run).");
// =====================================================================
// T15: the optional office computer and Bea's verified register
// =====================================================================
int t15Start = checks, t15Fails = fails;
var shop15 = ShopCatalogue.BuiltIn();
var file15 = JsonSerializer.Deserialize<ShopCatalogue>(File.ReadAllText(Res("Campaign/shop.json")), json);
var items15 = InventoryCatalogue.BuiltIn();
ShopProduct pc15 = shop15.Find(ShopCatalogue.OfficeComputer), pcFile15 = file15.Find(ShopCatalogue.OfficeComputer);
Check(pc15.price == 250 && pc15.IsDecor && pc15.requires == "chapter:Level_03" && pc15.anchors.SequenceEqual(new[] { ShopCatalogue.PrinterDeskAnchor }) &&
      pcFile15.price == pc15.price && pcFile15.requires == pc15.requires && pcFile15.anchors.SequenceEqual(pc15.anchors) && pcFile15.effect == pc15.effect &&
      pcFile15.title == pc15.title && pcFile15.requiresLabel == pc15.requiresLabel && pcFile15.section == ShopCatalogue.SectionOffice &&
      Snap(file15.FindAnchor(ShopCatalogue.PrinterDeskAnchor)) == Snap(shop15.FindAnchor(ShopCatalogue.PrinterDeskAnchor)) &&
      shop15.FindAnchor(ShopCatalogue.PrinterDeskAnchor).kind == ShopDecorAnchor.KindSurface && shop15.FindAnchor(ShopCatalogue.PrinterDeskAnchor).target == "PrinterDesk" &&
      shop15.Problems(items15).Count == 0 && file15.Problems(items15).Count == 0 && pc15.effect.Contains("No filing or printing"),
    "T15: the existing office_computer (250, office tab) now requires completed Level 3 and installs on printer_desk; shop.json and the built-in fallback agree and validate");
foreach (string scene15 in new[] { "Office_Level1", "Office_Level2", "Office_Level3" })
    Check(File.ReadAllText(Path.Combine(root, "Assets", "Scenes", scene15 + ".unity")).Contains("m_Name: PrinterDesk"), $"T15: {scene15} has the printer desk the anchor resolves to");
// ---------- purchase gating ----------
PlayerProgressData Buyer15(PlayerProgressData from, int coins, bool introduced = true) { var b = Clone(from); b.campaign.coins = coins; b.campaign.shopIntroduced = introduced; return b; }
var done15 = Clone(story14);   // T14: Level 3 completed, wrap-up written, all Bea claims paid
Check(done15.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) && !ShopRules.IsOwned(done15, pc15), "T15 fixture: a completed Bea-only Level 3 campaign without the computer");
foreach (var (label15, save15, status15) in new[] {
    ("Level 3 not completed (result pending)", Buyer15(Paid14(messaged14), 1000), ShopStatus.Teaser),
    ("Chapter 2 only", Buyer15(live, 1000), ShopStatus.Teaser),
    ("completed, 249 coins", Buyer15(done15, 249), ShopStatus.TooExpensive),
    ("completed, shop not introduced", Buyer15(done15, 1000, false), ShopStatus.ShopClosed) })
{
    string before15 = Snap(save15);
    PurchaseResult r15 = ShopRules.Purchase(save15, shop15, items15, ShopCatalogue.OfficeComputer, "t15/" + label15);
    Check(ShopRules.StatusOf(save15, shop15, pc15, true) == status15 && r15.Outcome == InventoryOutcome.Rejected && Snap(save15) == before15,
        $"T15: {label15}: {status15}, nothing bought or spent");
}
Check(ShopRules.StatusOf(Buyer15(done15, 1000), shop15, pc15, false) == ShopStatus.BrowseOnly &&
      ShopRules.ReasonFor(ShopStatus.Teaser, pc15, live, shop15) == "Requires corrected Level 3",
    "T15: practice/replay runs only browse it; the teaser keeps its label until Level 3 is completed");
var noAnchor15 = ShopCatalogue.BuiltIn(); noAnchor15.anchors.RemoveAll(a => a.id == ShopCatalogue.PrinterDeskAnchor);
var noSpot15 = Buyer15(done15, 1000); string noSpotBefore15 = Snap(noSpot15);
Check(ShopRules.Purchase(noSpot15, noAnchor15, items15, ShopCatalogue.OfficeComputer, "t15/no-anchor").Message == DecorRules.NoSpotMessage && Snap(noSpot15) == noSpotBefore15,
    "T15: without a valid installation anchor it is refused before any coin moves");
// ---------- one atomic purchase ----------
var buy15 = Buyer15(done15, 300);
string chapter15 = Snap(buy15.campaign.levelThree), inv15 = Snap(buy15.campaign.inventory), rosa15 = Snap(buy15.campaign.rosaEthics);
int trust15 = buy15.campaign.publicTrust, exp15 = buy15.totalExp, ledger15 = buy15.campaign.ledger.Count, claims15 = buy15.campaign.rewardClaims.Count;
PurchaseResult bought15 = ShopRules.Purchase(buy15, shop15, items15, ShopCatalogue.OfficeComputer, "t15/buy");
Check(bought15.Outcome == InventoryOutcome.Applied && bought15.AnchorId == ShopCatalogue.PrinterDeskAnchor && bought15.CoinsBefore == 300 && bought15.CoinsAfter == 50 &&
      buy15.campaign.coins == 50 && ShopRules.IsOwned(buy15, pc15) && DecorRules.AnchorOf(buy15.campaign, ShopCatalogue.OfficeComputer) == ShopCatalogue.PrinterDeskAnchor &&
      !buy15.campaign.decor.Find(d => d.itemId == ShopCatalogue.OfficeComputer).arrived &&
      buy15.campaign.ledger.Count == ledger15 + 1 && buy15.campaign.ledger.Last().amount == -250 && buy15.campaign.ledger.Last().reason == "Bought Office Computer" &&
      buy15.campaign.rewardClaims.Count == claims15 + 1 && buy15.campaign.publicTrust == trust15 && buy15.totalExp == exp15 &&
      Snap(buy15.campaign.levelThree) == chapter15 && Snap(buy15.campaign.inventory) == inv15 && Snap(buy15.campaign.rosaEthics) == rosa15,
    "T15: one purchase debits 250 with one ledger line, records ownership and the printer-desk installation together; Trust, XP, chapter, possessions and Rosa history untouched");
string boughtSnap15 = Snap(buy15);
Check(ShopRules.Purchase(buy15, shop15, items15, ShopCatalogue.OfficeComputer, "t15/buy").Outcome == InventoryOutcome.AlreadyApplied && Snap(buy15) == boughtSnap15 &&
      ShopRules.Purchase(buy15, shop15, items15, ShopCatalogue.OfficeComputer, "t15/again").Outcome == InventoryOutcome.Rejected && Snap(buy15) == boughtSnap15 &&
      ShopRules.StatusOf(buy15, shop15, pc15, true) == ShopStatus.Owned,
    "T15: a repeated tap (same request) is AlreadyApplied and a second request finds it owned: one debit, one installation");
Check(DecorRules.Resolve(buy15.campaign, shop15).Count(p => p.itemId == ShopCatalogue.OfficeComputer) == 1 &&
      DecorRules.Resolve(buy15.campaign, shop15).First(p => p.itemId == ShopCatalogue.OfficeComputer).anchorId == ShopCatalogue.PrinterDeskAnchor,
    "T15: the office shows exactly one computer on the printer desk");
var decorBoth15 = Clone(buy15); decorBoth15.campaign.coins = 200;
Check(ShopRules.Purchase(decorBoth15, shop15, items15, ShopCatalogue.PencilCup, "t15/cup").Ok &&
      DecorRules.Resolve(decorBoth15.campaign, shop15).Select(p => p.itemId + "@" + p.anchorId).OrderBy(x => x).SequenceEqual(new[] { "office_computer@printer_desk", "pencil_cup@desk_corner" }),
    "T15: the computer and the pencil cup keep separate anchors (the consultant desk keeps its corner)");
// ---------- signed save: failure, retry, reload, no repurchase ----------
string sandbox15 = Path.Combine(Path.GetTempPath(), "TaxVentureComputer-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox15);
try
{
    var store15 = new SaveFileStore(Path.Combine(sandbox15, "progress.json"));
    string Encode15(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    PlayerProgressData Reload15()
    {
        using var disk = JsonDocument.Parse(File.ReadAllText(store15.FilePath));
        string payload = disk.RootElement.GetProperty("payload").GetString();
        if (disk.RootElement.GetProperty("signature").GetString() != SaveFileStore.Sign(payload)) return null;
        var back = JsonSerializer.Deserialize<PlayerProgressData>(payload, json); back.Normalize(); return back;
    }
    var published15 = Buyer15(done15, 300);
    store15.Write(Encode15(published15));
    bool Buy15(string request)
    {
        var next = Clone(published15);
        // Mirrors PlayerProgressManager.CommitPurchase: only an Applied candidate is written.
        if (ShopRules.Purchase(next, shop15, items15, ShopCatalogue.OfficeComputer, request).Outcome != InventoryOutcome.Applied) return false;
        try { store15.Write(Encode15(next)); } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
        published15 = next; return true;
    }
    byte[] disk15 = File.ReadAllBytes(store15.FilePath);
    Directory.CreateDirectory(store15.TempPath);
    Check(!Buy15("t15/disk") && File.ReadAllBytes(store15.FilePath).SequenceEqual(disk15) && Reload15().campaign.coins == 300 && !ShopRules.IsOwned(Reload15(), pc15),
        "T15: a failed purchase write keeps the 300 coins and no computer (live and on disk)");
    Directory.Delete(store15.TempPath);
    Check(Buy15("t15/disk") && !Buy15("t15/disk") && !Buy15("t15/disk-2"), "T15: the retry buys once; repeats and new requests do not charge again");
    var back15 = Reload15();
    Check(back15.campaign.coins == 50 && ShopRules.IsOwned(back15, pc15) && ShopRules.StatusOf(back15, shop15, pc15, true) == ShopStatus.Owned &&
          DecorRules.Resolve(back15.campaign, shop15).Count(p => p.itemId == ShopCatalogue.OfficeComputer) == 1 &&
          Snap(back15.campaign.levelThree) == Snap(done15.campaign.levelThree),
        "T15 reload: the owned computer restores on its desk without repurchase; the completed chapter is unchanged");
    Check(DecorRules.MarkArrived(back15.campaign, ShopCatalogue.OfficeComputer) && !DecorRules.MarkArrived(back15.campaign, ShopCatalogue.OfficeComputer),
        "T15: its arrival is shown once (a later visit does not replay it)");
}
finally { Directory.Delete(sandbox15, true); }
// ---------- the register: frozen, read-only, no answer key ----------
var reg15Data = Clone(buy15);
string regBefore15 = Snap(reg15Data);
var reg15 = Level3RegisterRules.Bea(reg15Data, Level3ClosingRules.DraftOverstatement(bea.level3));
Level3ReviewRecord frozen15 = reg15Data.campaign.levelThree.Case(B7r).review;
Check(reg15.Verified && reg15.Client == "Beatriz Mendoza" &&
      reg15.Lines.Select(l => l.Key + "=" + l.Value).SequenceEqual(new[] { "Taxable income=₱600,000.00", "Tax due=₱62,500.00", "Tax credit (withheld)=₱12,500.00",
                                                                           "Remaining payable=₱50,000.00", "Finding=Draft costs ₱54,000.00 above her originals" }) &&
      frozen15.taxableIncome == "600000" && frozen15.remainingPayable == "50000" && Snap(reg15Data) == regBefore15,
    "T15: the register shows Bea's frozen approved figures and finding exactly, and reading it changes nothing in the save");
var pending15 = Level3RegisterRules.Bea(Paid14(messaged14));
var tampered15 = Clone(done15); tampered15.campaign.levelThree.Case(B7r).review.remainingPayable = "";
Check(!pending15.Verified && pending15.Lines.Count == 0 && pending15.Status == Level3RegisterRules.NotVerified &&
      !Level3RegisterRules.Bea(tampered15).Verified && Level3RegisterRules.Bea(tampered15).Lines.Count == 0 &&
      !Level3RegisterRules.Bea(new PlayerProgressData()).Verified && !Level3RegisterRules.Bea(null).Verified &&
      Level3RegisterRules.Bea(done15).Lines.All(l => l.Key != "Finding"),
    "T15: an unfinished, incomplete or missing case is 'not verified' with no figures (nothing filled in from fixtures); the finding needs her case file");
foreach (string file15src in new[] { "Scripts/Level3/Level3RegisterRules.cs", "Scripts/Interactables/OfficeComputerTerminal.cs" })
{
    string code15 = System.Text.RegularExpressions.Regex.Replace(Src(file15src), @"///.*|//.*", "");
    Check(!code15.Contains("expectedFields") && !code15.Contains("worksheetDecisions") && !code15.Contains("Commit") && !code15.Contains("TryWrite") &&
          !code15.Contains(".Save(") && !code15.Contains("ComputerHomeUI") && !code15.Contains("ComputerUI") && !code15.Contains("FormSelectionUI") &&
          !code15.Contains("TaxComputationCalculator") && !code15.Contains("coins") && !code15.Contains("ownedUpgradeIds"),
        $"T15: {file15src}: reads only; no answer key, save, purchase, prototype calculator, filing or printing route");
}
// ---------- optional: never a gate ----------
Check(!ShopRules.IsOwned(story14, pc15) && story14.campaign.IsChapterCompleted(Level3ChapterRules.LevelId) && Level3ClosingRules.Completed(story14.campaign.levelThree) &&
      !Src("Scripts/Level3/Level3ClosingRules.cs").Contains("OfficeComputer") && !Src("Scripts/Level3/Level3Checkpointer.cs").Contains("OfficeComputer"),
    "T15: Bea's chapter completes without the computer; nothing in the chapter or practice requires it");
// ---------- wiring: original assets, installer, terminal ----------
string decor15 = Src("Scripts/Interactables/OfficeDecor.cs"), models15 = Src("Scripts/Interactables/DecorModels.cs"), term15 = Src("Scripts/Interactables/OfficeComputerTerminal.cs");
string assets15 = File.ReadAllText(Res("Campaign/shop_assets.asset"));
Check(assets15.Contains("computerMonitorPrefab: {fileID: 1911445299336768, guid: 830cb1fbbcb273c4f804922226ab2712, type: 3}") &&
      assets15.Contains("computerKeyboardPrefab: {fileID: 1324923166343894, guid: 38e262f87c3088345a95b8ff628f0ce5, type: 3}") &&
      File.ReadAllText(Path.Combine(root, "Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Computer_Monitor_01.prefab.meta")).Contains("guid: 830cb1fbbcb273c4f804922226ab2712") &&
      File.ReadAllText(Path.Combine(root, "Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Computer_Keyboard_01.prefab.meta")).Contains("guid: 38e262f87c3088345a95b8ff628f0ce5"),
    "T15: the computer is the supplied Synty monitor and keyboard, referenced (not copied) from the shop's asset registry");
Check(decor15.Contains("GameObject root = computer ? DecorModels.BuildComputer(transform) : DecorModels.Build(placement.itemId, transform);") &&
      decor15.Contains("if (computer) MakeTerminal(root);") && Body6(decor15, "private static void MakeTerminal(", "private void Warn(").Contains("root.AddComponent<OfficeComputerTerminal>();") &&
      Body6(decor15, "private static void MakeTerminal(", "private void Warn(").Contains("LayerMask.NameToLayer(\"Interactable\")") &&
      decor15.Contains("collider.enabled = visible;") && decor15.Contains("Vector2 footprint = DecorModels.Footprint(itemId);") &&
      DecorModels.Footprint(ShopCatalogue.PencilCup) == DecorModels.PencilCupFootprint && !DecorModels.Has(ShopCatalogue.OfficeComputer) &&
      models15.Contains("foreach (Collider collider in part.GetComponentsInChildren<Collider>(true)) Object.Destroy(collider);"),
    "T15: OfficeDecor installs it with its own footprint, one Interactable box and the terminal, unusable until it has arrived; the cup keeps its footprint; the catalogue shows its icon");
Check(Index(term15, "if (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen) return;") < Index(term15, "popup.ShowScreen(") &&
      term15.Contains("Level3RegisterRules.Bea(data, over)") && Body6(term15, "data.Secondary = () =>", "return data;").Contains("popup.OnCloseButtonPressed();") &&
      Body6(term15, "data.Secondary = () =>", "return data;").Contains("bar.OpenViewer(InventoryCatalogue.CaseFolder)"),
    "T15: the terminal opens the register on the existing Audit Summary card (its lock and Close); Her papers closes it first, then opens the existing Case Folder reader");
string buyBody15 = Body6(Src("Scripts/RewardsSystem/PlayerProgressManager.cs"), "public PurchaseResult CommitPurchase(", "// ---------- review vouchers");
Check(buyBody15.Contains("if (!CanSpendCoins)") && Index(buyBody15, "ShopRules.Purchase(candidate") < Index(buyBody15, "if (result.Outcome != InventoryOutcome.Applied) return result;") &&
      Index(buyBody15, "if (result.Outcome != InventoryOutcome.Applied) return result;") < Index(buyBody15, "TryWriteCandidate(candidate)") &&
      Index(buyBody15, "TryWriteCandidate(candidate)") < Index(buyBody15, "PurchaseCommitted?.Invoke(result)"),
    "T15: the existing purchase owner is candidate -> signed write -> publish; only an applied purchase is written; replay/practice cannot spend");
Console.WriteLine($"T15: {checks - t15Start} checks, {fails - t15Fails} failures (catalogue/fallback, gating, atomic debit/ownership/install, duplicates, signed failure/retry/reload, register fidelity and non-mutation, optionality, wiring; Unity rendering, placement on the printer desk, monitor facing, touch and audio not run).");
// =====================================================================
// T16: Level 3 practice drills and best-only mastery replay
// =====================================================================
int t16Start = checks, t16Fails = fails;
// ---------- practice starts: real commands, memory only ----------
var machineStart16 = Level3EditorFixtures.Build(Level3EditorFixtures.BeaTableRestoredId, bea);
var officeStart16 = Level3EditorFixtures.Build(Level3EditorFixtures.OfficePracticeId, bea);
Check(machineStart16 != null && Level3ChapterRules.PhaseOf(machineStart16) == Level3Phase.WorksheetDraft && Level3ChapterRules.Problem(machineStart16) == null &&
      officeStart16 != null && Level3ChapterRules.PhaseOf(officeStart16) == Level3Phase.BeaComputation && officeStart16.calculatorTaught &&
      Level3WorksheetCheck.Collected(officeStart16) && Level3ChapterRules.Problem(officeStart16) == null &&
      Level3EditorFixtures.Build(Level3EditorFixtures.OfficePracticeId, marine) == null && Level3EditorFixtures.Build(Level3EditorFixtures.OfficePracticeId, null) == null,
    "T16: the machine practice starts at Bea's handed-over records and the office practice at her collected summary with the calculator taught (real commands; Bea only)");
var drills16 = new[] { "l3_machine", "l3_office" }.Select(id => JsonSerializer.Deserialize<TrainingDrill>(File.ReadAllText(Res($"Tutorials/Training/Drills/{id}.json")), json)).ToList();
var training16 = JsonSerializer.Deserialize<TrainingCatalogFile>(File.ReadAllText(Res("Tutorials/Training/training.json")), json);
string trainingProblems16 = training16.Validate();
var guide16 = Json("Tutorials/Level_03/guide.json").GetProperty("lessons").EnumerateArray().Select(l => l.GetProperty("id").GetString()).ToHashSet();
string flags16 = Src("Scripts/Level3/Level3Checkpointer.cs");
Check(string.IsNullOrEmpty(trainingProblems16) && training16.FindTab("bea") != null && !training16.FindTab("bea").alwaysOpen &&
      training16.cards.Where(c => c.tab == "bea").Select(c => c.drill).SequenceEqual(new[] { "l3_machine", "l3_office" }) &&
      training16.cards.Where(c => c.tab == "bea").SelectMany(c => c.lessons).All(l => l.StartsWith("Level_03/") && guide16.Contains(l.Substring(9))),
    "T16: the existing Tutorial Hub gains one Bea's Case tab (hidden until Chapter 3) with the machine and office drills, unlocked by their own Level 3 lessons");
foreach (TrainingDrill drill16 in drills16)
{
    var steps16 = drill16.guided.steps.Concat(drill16.mastery.steps).ToList();
    var waits16 = steps16.Where(s => s.pages != null).SelectMany(s => s.pages).Select(p => p.waitFor).Where(w => !string.IsNullOrEmpty(w)).ToList();
    Check(drill16.levelId == "Level_03" && drill16.scene == "Office_Level3" && drill16.start == "plaza" && string.IsNullOrEmpty(drill16.practiceCase) &&
          Level3EditorFixtures.Build(drill16.fixture, bea) != null && drill16.hide.Length == 0 && drill16.objectivesDone.Length == 0 &&
          drill16.guided.steps.Where(s => !string.IsNullOrEmpty(s.use)).All(s => guide16.Contains(s.use)) &&
          waits16.Where(w => w.StartsWith("flag:")).All(w => flags16.Contains("case \"" + w.Substring(5) + "\":")) &&
          steps16.Where(s => s.pages != null).SelectMany(s => s.pages).All(p => (p.text ?? "").Split(' ').Length <= 14) &&
          !File.ReadAllText(Res($"Tutorials/Training/Drills/{drill16.id}.json")).Contains("Marine") && !File.ReadAllText(Res($"Tutorials/Training/Drills/{drill16.id}.json")).Contains("company"),
        $"T16: {drill16.id}: Level 3 in Office_Level3 from its practice start; every wait is a real Level 3 flag or lesson; short steps; no company drill");
}
// ---------- isolation: practice, replay and Editor never use the story ----------
foreach (CampaignRunMode mode16 in new[] { CampaignRunMode.Practice, CampaignRunMode.MasteryReplay, CampaignRunMode.DirectEditor })
    Check(!Level3ChapterRules.UsesStoryCheckpoint(mode16, false, story14) && !CampaignRun.AllowsStoryWrites(mode16) &&
          !Level3ChapterRules.UsesStoryCheckpoint(CampaignRunMode.Story, true, story14),
        $"T16: {mode16} never writes the Level 3 snapshot, wallet, Trust, claims or possessions");
Check(!Level3ChapterRules.UsesInventoryPreview(CampaignRunMode.Practice, true, live, "Level_03") && !Level3ChapterRules.UsesInventoryPreview(CampaignRunMode.MasteryReplay, false, live, "Level_03") &&
      Level3ChapterRules.UsesInventoryPreview(CampaignRunMode.DirectEditor, false, live, "Level_03") && !Level3ChapterRules.UsesInventoryPreview(CampaignRunMode.DirectEditor, false, Fixture(false), "Level_03"),
    "T16: only a direct Editor attempt of a Chapter 2 campaign previews a copy of its possessions; practice, replay and unsupported previews start with an empty local inventory");
string ppm16 = Src("Scripts/RewardsSystem/PlayerProgressManager.cs");
foreach (string commit16 in new[] { "CommitLevelThreeCheckpoint(", "CommitLevelThreeCertificate(", "CommitLevelThreeWorksheet(", "CommitLevelThreeApproval(", "CommitLevelThreeDelivery(", "CommitLevelThreeWrapUp(" })
    Check(Body6(ppm16, "public bool " + commit16, "/// <summary>").Contains("if (!UsesLevelThreeStory) return false;"), $"T16: {commit16.TrimEnd('(')} writes only in a story run");
string inv16 = Body6(ppm16, "public CampaignInventoryRecord Inventory", "/// <summary>");
Check(inv16.Contains("if (UsesStoryInventory) return progress.campaign.inventory;") && inv16.Contains(": new CampaignInventoryRecord();") &&
      ppm16.Contains("CampaignRun.AllowsStoryWrites(CurrentRunMode) && !TrainingSession.IsPracticing;") &&
      Body6(ppm16, "public VoucherUseResult CommitLevelThreeVoucherUse(", "/// <summary>").Contains("if (chapter != null && UsesLevelThreeStory)"),
    "T16: practice/replay use a temporary per-scene inventory, purchases only browse, vouchers are temporary for the visit (no real consumption)");
string tryPractice16 = Body6(flags16, "private bool TryPracticeStart()", "/// <summary>");
Check(flags16.Contains("if (!story && TryPracticeStart()) return;") && tryPractice16.Contains("TrainingSession.IsPracticing ? TrainingSession.Drill : null") &&
      tryPractice16.Contains("drill.levelId != Level3ChapterRules.LevelId") && tryPractice16.Contains("Level3EditorFixtures.Build(drill.fixture.Trim(), ActiveDefinition)") &&
      tryPractice16.Contains("RestoreQuietly(welcomeBack: false);") && !tryPractice16.Contains("CommitLevelThree") &&
      Src("Scripts/Training/TrainingData.cs").Contains("public string fixture = \"\";"),
    "T16: a Level 3 drill starts from its fixture only outside the story, quietly (no welcome-back line), and never saves");
// ---------- best-only mastery replay (v1/v2 aware) ----------
string Isolated16(PlayerProgressData d)
{
    JsonNode n = JsonNode.Parse(Snap(d));
    foreach (string key in new[] { "levelRecords", "badgeRecords", "totalExp", "totalReputation", "passedLevelIds", "completedLevelIds", "levelsCompleted" }) n.AsObject().Remove(key);
    return n.ToJsonString();
}
Level3ChapterCheckpoint Replay16(int correct, int xp, int tier, int caseXp)
{
    var r = acked14.Copy();
    var w = r.worksheetCheck; w.assessmentVersion = Level3WorksheetCheck.RevisedAssessmentVersion; w.firstCorrect = correct; w.firstTotal = 9; w.firstTier = tier; w.firstXp = xp;
    r.Case(B7r).review.xp = caseXp;
    return r;
}
var story16 = Clone(story14);
int storyBest16 = story16.GetRecord(Level3ChapterRules.LevelId).bestExp, storyExp16 = story16.totalExp;
string storyFrozen16 = Snap(story16.campaign.levelThree), storyIso16 = Isolated16(story16);
var low16 = Level3ClosingRules.ApplyReplayBest(story16, Replay16(6, 57, 1, 90));
Check(low16.Changed && low16.ExpGain == 0 && story16.GetRecord(Level3ChapterRules.LevelId).bestExp == storyBest16 && story16.totalExp == storyExp16 &&
      story16.badgeRecords.Find(b => b.badgeId == "worth_the_tax_3").tier == 2 && Isolated16(story16) == storyIso16 && Snap(story16.campaign.levelThree) == storyFrozen16,
    "T16: a weaker replay keeps the best XP and the Silver badge; everything outside the mastery record is byte-equal (snapshot, frozen first check, wallet, Trust, claims, possessions, history)");
var high16 = Level3ClosingRules.ApplyReplayBest(story16, Replay16(9, 60, 3, 100));
Check(high16.Changed && high16.ExpGain == 160 - storyBest16 && story16.GetRecord(Level3ChapterRules.LevelId).bestExp == 160 && story16.totalExp == storyExp16 + (160 - storyBest16) &&
      story16.badgeRecords.Find(b => b.badgeId == "worth_the_tax_3").tier == 3 && Isolated16(story16) == storyIso16 &&
      story16.campaign.levelThree.worksheetCheck.firstCorrect == 8 && story16.campaign.levelThree.worksheetCheck.firstTier == 2,
    "T16: a better replay raises the best to 160 and Gold once (only the improvement joins the career total); the story's frozen 8/9 Silver first check is unchanged");
int afterHigh16 = story16.totalExp;
var again16 = Level3ClosingRules.ApplyReplayBest(story16, Replay16(9, 60, 3, 100));
Check(again16.ExpGain == 0 && story16.totalExp == afterHigh16 && story16.campaign.coins == story14.campaign.coins && story16.campaign.publicTrust == story14.campaign.publicTrust &&
      story16.campaign.ledger.Count == story14.campaign.ledger.Count,
    "T16: repeating the same replay farms nothing (no XP, coins, Trust or ledger line)");
// v1 story grade against a v2 replay: tier and the shared worksheet XP scale, never raw counts, never summed.
var legacyStory16 = Paid14(Acked14(legacy14));
Level3ClosingRules.ApplyWrapUp(legacyStory16, Acked14(legacy14));
int legacyBest16 = legacyStory16.GetRecord(Level3ChapterRules.LevelId).bestExp, legacyTotal16 = legacyStory16.totalExp;
var v2Replay16 = Replay16(8, Level3WorksheetCheck.RevisedXpFor(1), Level3WorksheetCheck.RevisedTierFor(8), caseXp14);
var cross16 = Level3ClosingRules.ApplyReplayBest(legacyStory16, v2Replay16);
var v1Res16 = Level3ClosingRules.Result(legacyStory16.campaign.levelThree);
var v2Res16 = Level3ClosingRules.Result(v2Replay16);
Check(cross16.ExpGain == 0 && legacyStory16.totalExp == legacyTotal16 && legacyStory16.GetRecord(Level3ChapterRules.LevelId).bestExp == legacyBest16 &&
      legacyStory16.badgeRecords.Find(b => b.badgeId == "worth_the_tax_3").tier == 2 &&
      v1Res16.WorksheetVersion == "L3-worksheet-v1" && v2Res16.WorksheetVersion == "L3-worksheet-v2" && v1Res16.WorksheetTotal == 12 && v2Res16.WorksheetTotal == 9 &&
      Math.Abs(Level3ClosingRules.NormalizedAccuracy(v1Res16) - 11.0 / 12) < 1e-9 && Math.Abs(Level3ClosingRules.NormalizedAccuracy(v2Res16) - 8.0 / 9) < 1e-9 &&
      legacyStory16.campaign.levelThree.worksheetCheck.assessmentVersion == "L3-worksheet-v1" && legacyStory16.campaign.levelThree.worksheetCheck.firstCorrect == 11,
    "T16: a v2 replay (8/9 Silver, 59 XP) against a legacy v1 story (11/12 Silver, 59 XP) changes nothing: same tier, same 60-point worksheet XP, no summing; the v1 grade stays v1");
var notDone16 = Paid14(messaged14);
string notDoneSnap16 = Snap(notDone16);
Check(!Level3ClosingRules.ApplyReplayBest(notDone16, Replay16(9, 60, 3, 100)).Changed && Snap(notDone16) == notDoneSnap16 &&
      !Level3ClosingRules.ApplyReplayBest(Clone(story14), messaged14).Changed && !Level3ClosingRules.ApplyReplayBest(null, acked14).Changed,
    "T16: only a story-completed Chapter 3 and an acknowledged replay chapter are eligible");
string replayBody16 = Body6(ppm16, "public ReplayCommit CommitLevelThreeReplay(", "/// <summary>");
Check(replayBody16.Contains("TrainingSession.IsPracticing") && replayBody16.Contains("CurrentRunMode != CampaignRunMode.MasteryReplay") &&
      replayBody16.Contains("!progress.campaign.IsChapterCompleted(Level3ChapterRules.LevelId)") &&
      Index(replayBody16, "Level3ClosingRules.ApplyReplayBest(candidate, replay)") < Index(replayBody16, "TryWriteCandidate(candidate)") &&
      !replayBody16.Contains("WithCheckpoint") && !replayBody16.Contains("Grant("),
    "T16: the replay's best is one candidate written through the signed save; never in practice/Editor/story; it never writes the chapter snapshot or a grant");
string ack16 = Body6(flags16, "public TapeCommit CommitAcknowledgment(", "/// <summary>");
Check(Index(ack16, "if (!SavesStory)") < Index(ack16, "CommitLevelThreeReplay(candidate, out best)") &&
      ack16.Contains("== PlayerProgressManager.ReplayCommit.SaveFailed") && Index(ack16, "return TapeCommit.SaveFailed;") < Index(ack16, "Publish(candidate);"),
    "T16: the replay's Continue saves its best first; a failed write keeps the result card pending");
string card16 = Body6(Src("Scripts/UI/LevelCompleteUI.cs"), "private RewardScreenData BuildChapterThreeScreen(", "/// <summary>");
Check(card16.Contains("bool story = owner.SavesStory;") && card16.Contains("if (story && career != null && career.HasCampaign)") &&
      card16.Contains("\"REPLAY  ·  BEST ONLY\"") && card16.Contains("CampaignRun.WhatChanges(mode)"),
    "T16: replay/practice/Editor result cards show their own XP and badge with the run's mode, never the story's coins or Trust as earnings");
Console.WriteLine($"T16: {checks - t16Start} checks, {fails - t16Fails} failures (practice starts, drill/catalogue wiring, mode isolation, temporary inventory/vouchers/purchases, best-only v1/v2 replay with byte-equal isolation, eligibility, signed replay write; Unity binder rendering, drill play, touch and audio not run).");
// =====================================================================
// T17: integrated Bea chapter (one owner per seam, objectives/guide in step with saved phases)
// =====================================================================
int t17Start = checks, t17Fails = fails;
var steps17 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray())
    .Select(st => st.GetProperty("trigger").GetString()).ToList();
var chain17 = new (string label, Level3ChapterCheckpoint state)[]
{
    ("fresh", Level3ChapterRules.CreateEntry()), ("assigned", Level3EditorFixtures.BeaEncounter(bea, false)), ("records", Level3EditorFixtures.BeaEncounter(bea, true)),
    ("summary ready", Level3EditorFixtures.SummaryReady(bea)), ("office", Level3EditorFixtures.FirstOfficeUse(bea)), ("payable", fixedCredit), ("approved", ok9r),
    ("carried", explained14), ("delivered", delivered14), ("finding", found14), ("pending result", messaged14), ("completed office", acked14)
};
int previousTicked17 = -1;
foreach (var (label17, state17) in chain17)
{
    var implied17 = Level3ChapterRules.ImpliedTriggers(state17);
    int ticked17 = steps17.TakeWhile(t => implied17.Contains(t)).Count();
    bool prefix17 = steps17.Skip(ticked17).All(t => !implied17.Contains(t));
    Check(state17 != null && Level3ChapterRules.Problem(state17) == null && prefix17 && ticked17 >= previousTicked17,
        $"T17: {label17}: a resume ticks exactly the first {ticked17} of {steps17.Count} Notes steps (no later step before an earlier one; the active step is the next)");
    previousTicked17 = ticked17;
}
Check(previousTicked17 == steps17.Count, "T17: the completed office has every Notes step ticked (no stale task)");
var lessons17 = Json("Tutorials/Level_03/guide.json").GetProperty("lessons").EnumerateArray().ToList();
var impliedDone17 = Level3ChapterRules.ImpliedLessons(acked14);
Check(lessons17.All(l => impliedDone17.Contains(l.GetProperty("id").GetString())),
    "T17: resuming the completed office replays no guide lesson (every lesson is implied as shown)");
string cp17 = Src("Scripts/Level3/Level3Checkpointer.cs");
var flags17 = System.Text.RegularExpressions.Regex.Matches(cp17, "case \"(\\w+)\":").Select(m => m.Groups[1].Value).ToHashSet();
var guideText17 = File.ReadAllText(Res("Tutorials/Level_03/guide.json"));
var lessonOnly17 = string.Join(" ", lessons17.Select(l => l.GetRawText()));
var usedFlags17 = System.Text.RegularExpressions.Regex.Matches(lessonOnly17, "flag:(\\w+)").Select(m => m.Groups[1].Value).ToHashSet();
Check(usedFlags17.Count > 20 && usedFlags17.All(flags17.Contains), "T17: every guide flag a Level 3 lesson waits on is a real Level3Checkpointer fact (no dead or misspelt flag)");
var debug17 = new System.Text.RegularExpressions.Regex(@"L3-|B-[A-Z]{2,}|Level_0|\bR[0-9]\b|DRAFT-B26|PARTY-26|\{[a-z]+\}");
var shown17 = lessons17.SelectMany(l => l.GetProperty("pages").EnumerateArray()).Select(pg => pg.TryGetProperty("text", out JsonElement t) ? t.GetString() : "")
    .Concat(Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).Select(st => st.GetProperty("text").GetString()))
    .Concat(new[] { bea14.findingQuestion, bea14.findingNext, bea14.afterFindingLine, bea14.balanceQuestion, bea14.deliveredLine, bea14.afterDeliveryLine })
    .Concat(bea14.findingChoices.SelectMany(c => new[] { c.text, c.reply })).Concat(town14.reception.quaxMessage.Select(l => l.text)).ToList();
Check(shown17.All(t => !debug17.IsMatch(t ?? "")), "T17: no source/case/row identifiers or unfilled tokens in guide pages, Notes steps or the closing lines");
// One owner per seam.
string scripts17 = string.Join("\n", Directory.GetFiles(Path.Combine(root, "Assets", "Scripts"), "*.cs", SearchOption.AllDirectories).Select(f => File.ReadAllText(f)));
Check(System.Text.RegularExpressions.Regex.Matches(scripts17, @"ClientCopyHandover\.LevelDelivery = this").Count == 1 &&
      System.Text.RegularExpressions.Regex.Matches(scripts17, @"ReceptionistInteractable\.LevelMessage = this").Count == 1 &&
      System.Text.RegularExpressions.Regex.Matches(scripts17, @"\.ShowLevelThreeChapter\(\)").Count == 1 &&
      System.Text.RegularExpressions.Regex.Matches(scripts17, @"\.CommitAcknowledgment\(").Count == 1 &&
      !scripts17.Contains("AddComponent<Level3WorksheetDesk>") && !File.ReadAllText(Path.Combine(root, "Assets", "Scenes", "Office_Level3.unity")).Contains(File.ReadAllText(Path.Combine(root, "Assets/Scripts/Level3/Level3WorksheetDesk.cs.meta")).Split("guid: ")[1].Substring(0, 32)),
    "T17: one owner per seam (Bea's copy delivery, the receptionist's message, the chapter card and its acknowledgment); the retired worksheet board is never built");
foreach (Level3Phase phase17 in (Level3Phase[])Enum.GetValues(typeof(Level3Phase)))
    Check(Level3ChapterRules.LegacyCompletionStates.All(st => !Level3ChapterRules.TransitionAllowed(phase17, st)), $"T17: {phase17}: the legacy single-case completion route stays refused");
Console.WriteLine($"T17: {checks - t17Start} checks, {fails - t17Fails} failures (objective prefixes per saved phase, completed-office restore, guide flags, player-facing identifiers, single owners, legacy route; Unity play, rendering, touch, audio, timing and device performance not run).");
// November 6 playtest: range-based earned trust, durable attempts, duplicate callbacks and unchanged approved figures.
int playtestChecks = checks, playtestFails = fails;
foreach (var pair in new[] { (-1, 4), (0, 4), (1, 4), (2, 4), (3, 3), (5, 3), (6, 2), (8, 2), (9, 1), (12, 1), (int.MaxValue, 1) })
    Check(Level3DeliveryRules.FollowThroughTrust(pair.Item1) == pair.Item2, "Follow-through trust range " + pair.Item1);
for (int errors = 0; errors <= 10; errors++)
{
    var attemptState = carried10.Copy();
    string frozenReview = Snap(attemptState.Case(B7r).review);
    for (int attempt = 1; attempt <= errors; attempt++)
    {
        Check(Level3DeliveryRules.RecordWrongAnswer(attemptState, Level3DeliveryRules.TaxDue, attempt, out _) == Level3CommandOutcome.Applied,
            "Wrong figure attempt saves: " + attempt);
        string once = Snap(attemptState);
        Check(Level3DeliveryRules.RecordWrongAnswer(attemptState, Level3DeliveryRules.TaxDue, attempt, out _) == Level3CommandOutcome.Duplicate && Snap(attemptState) == once,
            "A duplicate callback cannot count another wrong answer");
        attemptState = Clone(attemptState);
        Check(attemptState.Case(B7r).balanceWrongAnswers == attempt && Level3ChapterRules.Problem(attemptState) == null,
            "Wrong choices survive serialization without invalidating the checkpoint");
    }
    string beforeInvalid = Snap(attemptState);
    Check(Level3DeliveryRules.RecordWrongAnswer(attemptState, "unknown", errors + 1, out _) == Level3CommandOutcome.Refused &&
        Level3DeliveryRules.RecordWrongAnswer(attemptState, Level3DeliveryRules.Credit, errors + 2, out _) == Level3CommandOutcome.Refused && Snap(attemptState) == beforeInvalid,
        "Unknown and stale/skipped callbacks never affect hearts");
    Check(Level3DeliveryRules.Explain(attemptState, Level3DeliveryRules.Payable, out _) == Level3CommandOutcome.Applied &&
        Level3DeliveryRules.Deliver(attemptState, out _) == Level3CommandOutcome.Applied && Snap(attemptState.Case(B7r).review) == frozenReview,
        "Every wrong-answer range can still explain and deliver the unchanged approved copy");
    var earnedSave = Clone(paid9r);
    int earnedTrustBefore = earnedSave.campaign.publicTrust;
    var earned = Level3DeliveryRules.ApplyDeliveryGrants(earnedSave, attemptState);
    int expected = Level3DeliveryRules.FollowThroughTrust(errors);
    Check(earned.Changed && earned.Trust.Applied && earned.Trust.Amount == expected && earnedSave.campaign.publicTrust == earnedTrustBefore + expected &&
        earnedSave.campaign.ledger.Last().amount == expected && Level3DeliveryRules.ApplyDeliveryGrants(earnedSave, attemptState).AlreadyClaimed,
        "Delivery and its ledger grant exactly the earned hearts once: " + errors);
}
Check(!book9.Any(section => section.GetProperty("heading").GetString().StartsWith("Annual bands")) &&
    Json("Levels/Level_03/book.json").GetProperty("omitSharedSections").EnumerateArray().Select(v => v.GetString()).SequenceEqual(new[] { "Section 1", "Section 2" }) &&
    book9.Last().GetProperty("heading").GetString() == "Section 11 – Explaining the Client Copy",
    "Shorter Chapter 3 book preserves section numbers and adds useful optional reading");
Console.WriteLine($"November 6 playtest: {checks - playtestChecks} checks, {fails - playtestFails} failures (native animation/render checks remain manual).");

// Reported second-pass bugs: supplied classification, deliberately blank income, and no plaza seat pose leak.
var blankIncomePolish = new CaseData();
Level3CaseFolder.FillKnownClientData(blankIncomePolish, bea, contextPolish);
Check(blankIncomePolish.taxpayerType == TaxpayerType.Professional && blankIncomePolish.incomeSource == null,
    "Playtest: the supplied taxpayer type stays filled, Income source waits for the player's transfer");
Check(!Body6(Src("Scripts/UI/HUD/CaseFolder3DView.cs"), "private void RememberSeat()", "private static readonly").Contains("cc.CurrentMode != CameraController.CameraMode.Workstation)"),
    "Playtest: the outdoor workstation cannot overwrite the office folder's camera reference");
// Second playtest: the runtime's captured front page and actual supporting-paper attachment flags.
int folderPassStart = checks, folderPassFails = fails;
var packetReady = fixedCredit.Copy();
var correctFront = new CaseData
{
    fullName = bea.fullName, tin = "312-845-679-000", residencyStatus = ResidencyStatus.ResidentCitizen,
    taxpayerType = TaxpayerType.Professional, incomeSource = IncomeSource.ProfessionalIncome
};
Level3ReviewRules.CaptureFolder(packetReady, correctFront);
Check(Level3ReviewRules.SetAttachment(packetReady, Level3WorksheetCheck.WorksheetWorkId, true, out _) == Level3CommandOutcome.Applied &&
    Level3ReviewRules.SetAttachment(packetReady, Level3ChapterRules.BeaCreditSource, true, out _) == Level3CommandOutcome.Applied,
    "The collected summary and acquired certificate attach individually to Bea's packet");
int attachmentRevision = packetReady.Case(B7r).computation.revision;
Check(Level3ReviewRules.SetAttachment(packetReady, Level3ChapterRules.BeaCreditSource, true, out _) == Level3CommandOutcome.Duplicate &&
    packetReady.Case(B7r).computation.revision == attachmentRevision &&
    Level3ReviewRules.SetAttachment(packetReady, "B-PRINT", true, out _) == Level3CommandOutcome.Refused,
    "Duplicate/foreign supporting drops cannot change a stamp revision");
Check(Level3ReviewRules.Stamp(packetReady, R9, out _) == Level3CommandOutcome.Applied &&
    Level3ReviewRules.Evaluate(packetReady, bea, rc9).Outcome == Level3ReviewOutcome.Approved,
    "A complete source-backed computation with the actual front page and both attached papers can be approved");
foreach (string missing in new[] { "name", "tin", "classification", "type", "income", "summary", "certificate" })
{
    var rejectedPacket = packetReady.Copy();
    var packet = rejectedPacket.Case(B7r).folder;
    switch (missing)
    {
        case "name": packet.fullName = ""; break;
        case "tin": packet.tin = "wrong"; break;
        case "classification": packet.classification = "NonResidentCitizen"; break;
        case "type": packet.taxpayerType = "CompensationEarner"; break;
        case "income": packet.incomeSource = ""; break;
        case "summary": packet.summaryAttached = false; break;
        case "certificate": packet.certificateAttached = false; break;
    }
    string frozenWorksheet = Snap(rejectedPacket.worksheetCheck);
    string expectedProblem = missing == "income" ? Level3ReviewRules.IncomeField : missing == "summary" || missing == "certificate"
        ? Level3ReviewRules.SupportingField : Level3ReviewRules.PersonalField;
    Check(Level3ReviewRules.Submit(rejectedPacket, bea, rc9, out var rejection, out _) == Level3CommandOutcome.Applied &&
        rejection.Outcome == Level3ReviewOutcome.PrematureApproval && rejection.Field == expectedProblem && !rejectedPacket.Case(B7r).review.approved,
        "The actual Auditor refuses the missing/wrong packet requirement: " + missing);
    Check(rejectedPacket.Case(B7r).folder.onDesk && rejectedPacket.Case(B7r).folder.incomeSource == "" &&
        !rejectedPacket.Case(B7r).folder.summaryAttached && !rejectedPacket.Case(B7r).folder.certificateAttached &&
        rejectedPacket.tapes.All(t => t.caseId != B7r) && rejectedPacket.Case(B7r).computation.ruleId == "" &&
        Snap(rejectedPacket.worksheetCheck) == frozenWorksheet && Level3ChapterRules.Problem(rejectedPacket) == null,
        "A field/support return resets the editable folder, preserves the worksheet, and resumes on the desk: " + missing);
}
var stampOnlyPacket = packetReady.Copy();
stampOnlyPacket.Case(B7r).stamp.revision--;
var expectedStampOnlyComputation = stampOnlyPacket.Case(B7r).computation.Copy();
expectedStampOnlyComputation.revision++;
string computationBeforeStampReturn = Snap(expectedStampOnlyComputation), tapesBeforeStampReturn = Snap(stampOnlyPacket.tapes);
Check(Level3ReviewRules.Submit(stampOnlyPacket, bea, rc9, out var stampRejection, out _) == Level3CommandOutcome.Applied && stampRejection.Field == "stamp" &&
    Snap(stampOnlyPacket.Case(B7r).computation) == computationBeforeStampReturn && Snap(stampOnlyPacket.tapes) == tapesBeforeStampReturn &&
    stampOnlyPacket.Case(B7r).folder.incomeSource == "ProfessionalIncome" && stampOnlyPacket.Case(B7r).folder.summaryAttached &&
    stampOnlyPacket.Case(B7r).folder.certificateAttached && stampOnlyPacket.Case(B7r).stamp.type == "",
    "A stamp-only return preserves every field, calculation and supporting paper; only a new stamp is needed");
var packetRoundTrip = Clone(packetReady);
Check(packetRoundTrip.Case(B7r).folder.summaryAttached && packetRoundTrip.Case(B7r).folder.certificateAttached &&
    packetRoundTrip.Case(B7r).folder.incomeSource == "ProfessionalIncome", "Folder fields and attachments survive serialization");
packetRoundTrip.Case(B7r).folder.incomeSource = "BusinessIncome";
Check(packetReady.Case(B7r).folder.incomeSource == "ProfessionalIncome", "Candidate folder edits cannot mutate the live packet through Copy");
Check(Level3ReviewRules.SetAttachment(packetReady, Level3ChapterRules.BeaCreditSource, false, out _) == Level3CommandOutcome.Applied &&
    Level3ReviewRules.Evaluate(packetReady, bea, rc9).Outcome != Level3ReviewOutcome.Approved,
    "Unclipping a real supporting paper invalidates the submitted stamp/approval readiness");
var wrongCalculationPacket = fixedCredit.Copy();
Level3ReviewRules.CaptureFolder(wrongCalculationPacket, correctFront);
Level3ReviewRules.SetAttachment(wrongCalculationPacket, Level3WorksheetCheck.WorksheetWorkId, true, out _);
Level3ReviewRules.SetAttachment(wrongCalculationPacket, Level3ChapterRules.BeaCreditSource, true, out _);
Level3ReviewRules.Stamp(wrongCalculationPacket, R9, out _);
Level3ComputationRules.SelectRule(wrongCalculationPacket, bea, "IND-15", out _);
Check(Level3ReviewRules.Submit(wrongCalculationPacket, bea, rc9, out var wrongCalculationReturn, out _) == Level3CommandOutcome.Applied &&
    wrongCalculationReturn.Field != "stamp" && wrongCalculationPacket.tapes.All(t => t.caseId != B7r) && wrongCalculationPacket.Case(B7r).folder.incomeSource == "",
    "A wrong calculation with a stale stamp requires the full redo; it is never disguised as a stamp-only return");
var missingPaperPacket = fixedCredit.Copy();
Level3ReviewRules.CaptureFolder(missingPaperPacket, correctFront);
Level3ReviewRules.SetAttachment(missingPaperPacket, Level3WorksheetCheck.WorksheetWorkId, true, out _);
Level3ReviewRules.SetAttachment(missingPaperPacket, Level3ChapterRules.BeaCreditSource, true, out _);
Level3ReviewRules.Stamp(missingPaperPacket, R9, out _);
Level3ReviewRules.SetAttachment(missingPaperPacket, Level3ChapterRules.BeaCreditSource, false, out _);
Check(Level3ReviewRules.Submit(missingPaperPacket, bea, rc9, out var missingPaperReturn, out _) == Level3CommandOutcome.Applied &&
    missingPaperReturn.Field == Level3ReviewRules.SupportingField && missingPaperReturn.Line.Contains("not attached"),
    "A certificate unclipped after stamping is reported as missing support, rather than only as a stale stamp");
string packetSandbox = Path.Combine(Path.GetTempPath(), "TaxVenturePacket-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(packetSandbox);
try
{
    var packetStore = new SaveFileStore(Path.Combine(packetSandbox, "progress.json"));
    string EncodePacket(PlayerProgressData value)
    {
        string payload = Snap(value);
        return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) });
    }
    var livePacketSave = Level3ChapterRules.WithCheckpoint(Clone(live), stampOnlyPacket);
    packetStore.Write(EncodePacket(livePacketSave));
    string untouchedPacketSave = Snap(livePacketSave);
    byte[] untouchedPacketDisk = File.ReadAllBytes(packetStore.FilePath);
    var candidatePacketSave = Clone(livePacketSave);
    candidatePacketSave.campaign.levelThree.Case(B7r).folder.incomeSource = "BusinessIncome";
    Directory.CreateDirectory(packetStore.TempPath);
    bool packetWriteFailed = false;
    try { packetStore.Write(EncodePacket(candidatePacketSave)); }
    catch (IOException) { packetWriteFailed = true; }
    catch (UnauthorizedAccessException) { packetWriteFailed = true; }
    Check(packetWriteFailed && Snap(livePacketSave) == untouchedPacketSave && File.ReadAllBytes(packetStore.FilePath).SequenceEqual(untouchedPacketDisk),
        "A failed signed packet write leaves the live fields, attachments and disk unchanged");
    Directory.Delete(packetStore.TempPath);
    packetStore.Write(EncodePacket(candidatePacketSave));
    using var packetEnvelope = JsonDocument.Parse(File.ReadAllText(packetStore.FilePath));
    string packetPayload = packetEnvelope.RootElement.GetProperty("payload").GetString();
    var reloadedPacketSave = JsonSerializer.Deserialize<PlayerProgressData>(packetPayload, json);
    Check(packetEnvelope.RootElement.GetProperty("signature").GetString() == SaveFileStore.Sign(packetPayload) &&
        reloadedPacketSave.campaign.levelThree.Case(B7r).folder.incomeSource == "BusinessIncome" &&
        reloadedPacketSave.campaign.levelThree.Case(B7r).folder.onDesk && reloadedPacketSave.campaign.levelThree.Case(B7r).folder.summaryAttached &&
        reloadedPacketSave.campaign.levelThree.Case(B7r).folder.certificateAttached,
        "A successful packet retry round-trips the actual chosen value, desk location and both supporting attachments");
}
finally { try { Directory.Delete(packetSandbox, true); } catch (IOException) { } }
Console.WriteLine($"Second playtest folder checks: {checks-folderPassStart}, failures {fails-folderPassFails}; native animation/placement remains manual.");

// Third playtest: actual packet handoffs free slots, calculator pagination follows the latest result,
// and the shared splash acknowledgment survives copy/reload without grading or rewarding.
int thirdStart = checks, thirdFails = fails;
var thirdCatalogue = InventoryCatalogue.BuiltIn();
var thirdState = fixedCredit.Copy();
Level3ReviewRules.CaptureFolder(thirdState, correctFront);
var thirdInventory = new CampaignInventoryRecord();
InventoryRules.Acquire(thirdInventory, thirdCatalogue, InventoryCatalogue.CaseFolder, 1, "third/folder");
Check(Level3WorksheetCheck.AcquireSummary(thirdInventory, thirdCatalogue, thirdState, definition: bea).Changed &&
    Level3CertificateRules.AcquireItem(thirdInventory, thirdCatalogue, thirdState, bea).Changed,
    "Both actual standalone papers begin as carried inventory items");
var summarySlot3 = InventoryRules.Find(thirdInventory, InventoryCatalogue.BeaSummary);
var certificateSlot3 = InventoryRules.Find(thirdInventory, InventoryCatalogue.BeaCertificate);
thirdInventory.pins.Add(InventoryCatalogue.BeaSummary);
Level3ReviewRules.SetAttachment(thirdState, Level3WorksheetCheck.WorksheetWorkId, true, out _);
Level3ReviewRules.SetAttachment(thirdState, Level3ChapterRules.BeaCreditSource, true, out _);
Check(Level3ReviewRules.SyncFolderInventory(thirdInventory, thirdCatalogue, thirdState).Changed &&
    InventoryRules.SlotAt(thirdInventory, summarySlot3).IsEmpty && InventoryRules.SlotAt(thirdInventory, certificateSlot3).IsEmpty &&
    !InventoryRules.Owns(thirdInventory, InventoryCatalogue.BeaSummary) && !InventoryRules.Owns(thirdInventory, InventoryCatalogue.BeaCertificate) &&
    InventoryRules.Record(thirdInventory, InventoryCatalogue.BeaSummary) == null && InventoryRules.Record(thirdInventory, InventoryCatalogue.BeaCertificate) == null &&
    !thirdInventory.pins.Contains(InventoryCatalogue.BeaSummary),
    "Physical folder attachment removes Bag/bar/collectible records and stale pins, leaving actual reusable empty slots");
Check(InventoryRules.Record(thirdInventory, InventoryCatalogue.CaseFolder).referencePages.Any(page => page.id.Contains("B-WORKSHEET")) &&
    InventoryRules.Record(thirdInventory, InventoryCatalogue.CaseFolder).referencePages.Any(page => page.id.Contains("B-WHT")),
    "The actual folder retains both readable paper copies through the existing reference-page system while their separate collectibles disappear");
string thirdAttachedInventory = Snap(thirdInventory);
Check(!Level3ReviewRules.SyncFolderInventory(thirdInventory, thirdCatalogue, thirdState).Changed && Snap(thirdInventory) == thirdAttachedInventory,
    "Repeated reconciliation neither saves changes nor manufactures standalone copies");
Check(InventoryRules.Acquire(thirdInventory, thirdCatalogue, InventoryCatalogue.Postcard, 1, "third/free-slot", summarySlot3).Changed &&
    InventoryRules.Find(thirdInventory, InventoryCatalogue.Postcard) == summarySlot3,
    "Another item really can occupy the slot vacated by the attached summary");
Level3ReviewRules.SetAttachment(thirdState, Level3WorksheetCheck.WorksheetWorkId, false, out _);
Check(Level3WorksheetCheck.AcquireSummary(thirdInventory, thirdCatalogue, thirdState, definition: bea, actionId: "third/summary-return").Changed &&
    InventoryRules.Owns(thirdInventory, InventoryCatalogue.BeaSummary) &&
    InventoryRules.Record(thirdInventory, InventoryCatalogue.BeaSummary).referencePages.Count > 0 &&
    InventoryRules.Find(thirdInventory, InventoryCatalogue.Postcard) == summarySlot3 &&
    !InventoryRules.Owns(thirdInventory, InventoryCatalogue.BeaCertificate),
    "Taking the summary out reuses normal acquisition into a free slot, keeps its readable pages, and never displaces the occupied slot or reclaims the still-attached certificate");
Level3ReviewRules.SetAttachment(thirdState, Level3ChapterRules.BeaCreditSource, false, out _);
Check(Level3CertificateRules.AcquireItem(thirdInventory, thirdCatalogue, thirdState, bea, "third/certificate-return").Changed &&
    InventoryRules.Record(thirdInventory, InventoryCatalogue.BeaCertificate).referencePages.Count > 0,
    "The detached certificate can be reacquired after its original once-only pickup claim, with its real readable source");
var thirdFullInventory = Rich13(false);
string thirdFullBefore = Snap(thirdFullInventory);
Check(!Level3CertificateRules.AcquireItem(thirdFullInventory, thirdCatalogue, thirdState, bea, "third/full-recover").Ok &&
    !Level3WorksheetCheck.AcquireSummary(thirdFullInventory, thirdCatalogue, thirdState, definition: bea, actionId: "third/full-summary").Ok &&
    Snap(thirdFullInventory) == thirdFullBefore,
    "Full-inventory recovery keeps both detached papers physically available and never invents an occupied hand or drops an existing possession");
for (int round = 0; round < 3; round++)
{
    Level3ReviewRules.SetAttachment(thirdState, Level3WorksheetCheck.WorksheetWorkId, true, out _);
    Check(Level3ReviewRules.SyncFolderInventory(thirdInventory, thirdCatalogue, thirdState).Changed,
        "Repeated attachment truly removes the summary, round " + round);
    Level3ReviewRules.SetAttachment(thirdState, Level3WorksheetCheck.WorksheetWorkId, false, out _);
    Check(Level3WorksheetCheck.AcquireSummary(thirdInventory, thirdCatalogue, thirdState, definition: bea, actionId: "third/reclaim/" + round).Changed &&
        thirdInventory.hotbar.Concat(thirdInventory.bag).Count(slot => slot.itemId == InventoryCatalogue.BeaSummary) == 1,
        "Repeated detach/reacquire creates exactly one summary, round " + round);
}
// Copy/sign/write is the production ordering: attachment and inventory leave together or not at all.
string thirdSandbox = Path.Combine(Path.GetTempPath(), "TaxVentureThird-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(thirdSandbox);
try
{
    var store = new SaveFileStore(Path.Combine(thirdSandbox, "progress.json"));
    string Encode3(PlayerProgressData value)
    {
        string payload = Snap(value);
        return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) });
    }
    var published = Level3ChapterRules.WithCheckpoint(Clone(live), thirdState);
    published.campaign.inventory = Clone(thirdInventory);
    store.Write(Encode3(published));
    string oldLive = Snap(published); byte[] oldDisk = File.ReadAllBytes(store.FilePath);
    var thirdCandidateSave = Clone(published);
    Level3ReviewRules.SetAttachment(thirdCandidateSave.campaign.levelThree, Level3WorksheetCheck.WorksheetWorkId, true, out _);
    Level3ReviewRules.SyncFolderInventory(thirdCandidateSave.campaign.inventory, thirdCatalogue, thirdCandidateSave.campaign.levelThree);
    Directory.CreateDirectory(store.TempPath);
    bool failed = false;
    try { store.Write(Encode3(thirdCandidateSave)); } catch (IOException) { failed = true; } catch (UnauthorizedAccessException) { failed = true; }
    Check(failed && Snap(published) == oldLive && oldDisk.SequenceEqual(File.ReadAllBytes(store.FilePath)) &&
        InventoryRules.Owns(published.campaign.inventory, InventoryCatalogue.BeaSummary),
        "Failed attachment write leaves both the physical packet flag and real carried slot untouched");
    Directory.Delete(store.TempPath);
    store.Write(Encode3(thirdCandidateSave));
    using var envelope = JsonDocument.Parse(File.ReadAllText(store.FilePath));
    string payload = envelope.RootElement.GetProperty("payload").GetString();
    var thirdReloadedSave = JsonSerializer.Deserialize<PlayerProgressData>(payload, json);
    Check(envelope.RootElement.GetProperty("signature").GetString() == SaveFileStore.Sign(payload) &&
        thirdReloadedSave.campaign.levelThree.Case(B7r).folder.summaryAttached && !InventoryRules.Owns(thirdReloadedSave.campaign.inventory, InventoryCatalogue.BeaSummary),
        "Successful retry reloads the attached actual paper and its vacated inventory together");
}
finally { try { Directory.Delete(thirdSandbox, true); } catch (IOException) { } }
for (int lineCount = 0; lineCount <= 40; lineCount++)
{
    int lastPage = Level3CalculatorModel.LastTapePage(lineCount);
    int thirdPageStart = Level3CalculatorModel.TapePageStart(lastPage, lineCount);
    Check(lastPage >= 0 && thirdPageStart == Math.Max(0, lineCount - 3),
        "Latest receipt page always includes the latest available operation/result: " + lineCount);
    var covered = new HashSet<int>();
    for (int page = 0; page <= lastPage; page++)
        for (int i = Level3CalculatorModel.TapePageStart(page, lineCount); i < Math.Min(lineCount, Level3CalculatorModel.TapePageStart(page, lineCount) + 3); i++) covered.Add(i);
    Check(covered.Count == lineCount, "All previous receipt steps remain accessible without gaps: " + lineCount);
}
var actualTaxLines3 = Level3Calculator.ChainLines(oc7.DraftChain);
Check(actualTaxLines3[Level3CalculatorModel.TapePageStart(Level3CalculatorModel.LastTapePage(actualTaxLines3.Count), actualTaxLines3.Count)] == "= 40,000.00" &&
    actualTaxLines3.Last() == "= 62,500.00" && actualTaxLines3.Contains("× 20.00% RATE"),
    "Bea's real subtraction/percentage/base chain opens on base plus final tax; earlier formula steps stay in the same receipt");
var intro3 = handed4r.Copy();
intro3.phase = Level3Phase.WorksheetDraft.ToString();
Check(Level3TableStaging.IntroDue(intro3), "First Help Bea/table entry requests the shared splash after the handover");
string beforeIntro3 = Snap(intro3);
var introCandidate3 = intro3.Copy();
Check(Level3TableStaging.AcknowledgeIntro(introCandidate3, out _) == Level3CommandOutcome.Applied && Snap(intro3) == beforeIntro3,
    "Splash acknowledgment changes only a thirdCandidateSave until the save succeeds");
Check(!Level3TableStaging.IntroDue(Clone(introCandidate3)) &&
    Level3TableStaging.AcknowledgeIntro(introCandidate3, out _) == Level3CommandOutcome.Duplicate &&
    !Level3TableStaging.IntroDue(Level3ChapterRules.CreateEntry()) && !Level3TableStaging.IntroDue(fixedCredit),
    "Splash acknowledgment survives reload, suppresses repeat cards, and cannot begin before Bea or at the office");
string thirdTableSource = Src("Scripts/Level3/Level3BeaTable.cs");
Check(thirdTableSource.Contains("SideQuestSplash.GetOrCreateForScene(gameObject.scene)") && thirdTableSource.Contains("splash.Show(content,") &&
    thirdTableSource.Contains("CommitComputation(Level3TableStaging.AcknowledgeIntro") && thirdTableSource.Contains("introSplash.Cancel();") &&
    thirdTableSource.Contains("public bool AcceptHelp(Pose? fromView = null) => encounter.Done && TrySit(fromView);"),
    "Help Bea and the later table tap share the actual existing splash, save its acknowledgment, and cancel owned presentation on teardown");
Check(Src("Scripts/RewardsSystem/PlayerProgressManager.cs").Contains("SyncFolderInventory(candidate.campaign.inventory") &&
    Body6(Src("Scripts/UI/HUD/HudHotbar.cs"), "private void SyncSummaryItem()", "/// <summary>").Contains("Level3CaseFolder.Attached(Level3WorksheetCheck.WorksheetWorkId)") &&
    Src("Scripts/Level3/Level3CertificatePaper.cs").Contains("!Level3CaseFolder.Attached(Level3ChapterRules.BeaCreditSource)"),
    "Runtime signed checkpoint owns inventory removal; both historical recovery paths respect physical attachment");
Console.WriteLine($"Third playtest: {checks-thirdStart} checks, {fails-thirdFails} failures; native hover, splash, scene pose and animation checks remain manual.");

// Fourth playtest: a real copy delivery consumes the inventory folder, and title timing follows desk arrival.
int fourthStart = checks, fourthFails = fails;
foreach (var fourthSlot in new[] { SlotRef.Hotbar(1), SlotRef.Bag(2) })
{
    var fourthItems = new CampaignInventoryRecord();
    InventoryRules.Acquire(fourthItems, thirdCatalogue, InventoryCatalogue.Notes, 1, "fourth/notes", SlotRef.Hotbar(0));
    InventoryRules.Acquire(fourthItems, thirdCatalogue, InventoryCatalogue.CaseFolder, 1, "fourth/copy", fourthSlot);
    fourthItems.pins.Add(InventoryCatalogue.CaseFolder);
    string fourthBefore = Snap(fourthItems);
    Check(!Level3DeliveryRules.ReleaseCopyInventory(fourthItems, thirdCatalogue, explained10).Changed && Snap(fourthItems) == fourthBefore,
        "An undelivered client copy keeps its actual inventory slot and pin: " + fourthSlot);
    Check(Level3DeliveryRules.ReleaseCopyInventory(fourthItems, thirdCatalogue, delivered10).Changed &&
        InventoryRules.SlotAt(fourthItems, fourthSlot).IsEmpty && !InventoryRules.Owns(fourthItems, InventoryCatalogue.CaseFolder) &&
        InventoryRules.Record(fourthItems, InventoryCatalogue.CaseFolder) == null && !fourthItems.pins.Contains(InventoryCatalogue.CaseFolder) &&
        InventoryRules.Find(fourthItems, InventoryCatalogue.Notes) == SlotRef.Hotbar(0),
        "Bea's delivered copy leaves a true empty slot and no Bag/collectible/pin entry; unrelated items stay placed: " + fourthSlot);
    string fourthRemoved = Snap(fourthItems);
    Check(!Level3DeliveryRules.ReleaseCopyInventory(fourthItems, thirdCatalogue, delivered10).Changed && Snap(fourthItems) == fourthRemoved,
        "Repeated delivery cleanup is an inventory no-op: " + fourthSlot);
    Check(InventoryRules.Acquire(fourthItems, thirdCatalogue, InventoryCatalogue.Postcard, 1, "fourth/reuse", fourthSlot).Changed &&
        InventoryRules.Find(fourthItems, InventoryCatalogue.Postcard) == fourthSlot,
        "The delivered folder's actual slot accepts another item: " + fourthSlot);
}
string fourthSandbox = Path.Combine(Path.GetTempPath(), "TaxVentureFourth-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fourthSandbox);
try
{
    var fourthStore = new SaveFileStore(Path.Combine(fourthSandbox, "progress.json"));
    string EncodeFourth(PlayerProgressData value)
    {
        string fourthPayload = Snap(value);
        return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = fourthPayload, ["signature"] = SaveFileStore.Sign(fourthPayload) });
    }
    var fourthPublished = Level3ChapterRules.WithCheckpoint(Clone(paid9r), explained10);
    fourthPublished.campaign.inventory = new CampaignInventoryRecord();
    InventoryRules.Acquire(fourthPublished.campaign.inventory, thirdCatalogue, InventoryCatalogue.CaseFolder, 1, "fourth/signed-copy");
    fourthPublished.campaign.inventory.pins.Add(InventoryCatalogue.CaseFolder);
    fourthStore.Write(EncodeFourth(fourthPublished));
    byte[] fourthDisk = File.ReadAllBytes(fourthStore.FilePath);
    string fourthLive = Snap(fourthPublished);
    var fourthCandidate = Clone(fourthPublished);
    Check(Level3DeliveryRules.Deliver(fourthCandidate.campaign.levelThree, out _) == Level3CommandOutcome.Applied,
        "The actual handover command moves Bea's explained copy to delivered");
    var fourthGrant = Level3DeliveryRules.ApplyDeliveryGrants(fourthCandidate, fourthCandidate.campaign.levelThree);
    Level3DeliveryRules.ReleaseCopyInventory(fourthCandidate.campaign.inventory, thirdCatalogue, fourthCandidate.campaign.levelThree);
    Directory.CreateDirectory(fourthStore.TempPath);
    bool fourthFailed = false;
    try { fourthStore.Write(EncodeFourth(fourthCandidate)); } catch (IOException) { fourthFailed = true; } catch (UnauthorizedAccessException) { fourthFailed = true; }
    Check(fourthFailed && Snap(fourthPublished) == fourthLive && fourthDisk.SequenceEqual(File.ReadAllBytes(fourthStore.FilePath)) &&
        fourthPublished.campaign.levelThree.Case(B7r).copyPhase == "carried" && InventoryRules.Owns(fourthPublished.campaign.inventory, InventoryCatalogue.CaseFolder),
        "Failed delivery save retains copy phase, actual carried folder, pins, Trust and disk together");
    Directory.Delete(fourthStore.TempPath);
    fourthStore.Write(EncodeFourth(fourthCandidate));
    using var fourthEnvelope = JsonDocument.Parse(File.ReadAllText(fourthStore.FilePath));
    string fourthPayloadBack = fourthEnvelope.RootElement.GetProperty("payload").GetString();
    var fourthReloaded = JsonSerializer.Deserialize<PlayerProgressData>(fourthPayloadBack, json);
    Check(fourthEnvelope.RootElement.GetProperty("signature").GetString() == SaveFileStore.Sign(fourthPayloadBack) &&
        Level3DeliveryRules.Delivered(fourthReloaded.campaign.levelThree) && !InventoryRules.Owns(fourthReloaded.campaign.inventory, InventoryCatalogue.CaseFolder) &&
        fourthReloaded.campaign.inventory.hotbar.All(slot => slot.IsEmpty) && fourthReloaded.campaign.inventory.bag.All(slot => slot.IsEmpty) &&
        !fourthReloaded.campaign.inventory.pins.Contains(InventoryCatalogue.CaseFolder) &&
        fourthReloaded.campaign.publicTrust == fourthPublished.campaign.publicTrust + fourthGrant.Trust.Amount,
        "Successful delivery retry/reload keeps both ordinary empty slots and the once-only earned Trust");
    string fourthAfterRetry = Snap(fourthReloaded);
    Check(!Level3DeliveryRules.ReleaseCopyInventory(fourthReloaded.campaign.inventory, thirdCatalogue, fourthReloaded.campaign.levelThree).Changed &&
        Level3DeliveryRules.ApplyDeliveryGrants(fourthReloaded, fourthReloaded.campaign.levelThree).AlreadyClaimed && Snap(fourthReloaded) == fourthAfterRetry,
        "Reload/duplicate cleanup neither recreates the client-copy collectible nor grants Trust twice");
}
finally { try { Directory.Delete(fourthSandbox, true); } catch (IOException) { } }
string fourthTable = Src("Scripts/Level3/Level3BeaTable.cs");
string fourthSeat = Body6(fourthTable, "private bool SitAtTable(", "private void HandleLeftSeat()");
string fourthIntro = Body6(fourthTable, "private void BeginIntroSplash()", "private void CancelIntro()");
// Current user request (6 November polish pass) supersedes the fourth playtest's camera-first order: both start together.
Check(Index(fourthSeat, "CameraController.Instance.EnterFirstPerson(Viewpoint") < Index(fourthSeat, "if (introPending) BeginIntroSplash();") &&
    !fourthSeat.Contains("yield") && !fourthSeat.Contains("StartCoroutine") && fourthIntro.Contains("splash.Show(content, IntroAcknowledged, withCamera: true)") &&
    Body6(Src("Scripts/SideQuest1/SideQuestSplash.cs"), "public bool Show(SplashContent content", "private void StartRequest(").Contains("Build();"),
    "Both entry paths start the camera and build the splash in one call (no wait on the camera, none on dismissal)");
Check(Index(fourthIntro, "while (Seated") < Index(fourthIntro, "GameplayEvents.RaiseGuideCue(\"BeaTableSeated\")") && fourthIntro.Contains("CameraController.Instance.IsTransitioning") &&
    fourthIntro.Contains("CameraController.Instance.InputHeld") && fourthIntro.Contains("CameraController.CameraMode.Workstation") && fourthIntro.Contains("SideQuestSplash.IsShowing") &&
    !fourthIntro.Contains("SitAtTable(") && !fourthIntro.Contains("EnterFirstPerson") && !fourthIntro.Contains("HudWindowLock.Acquire"),
    "A quick acknowledgment waits for the settled camera before the table or guide open; it never starts a second camera move or a viewer hold");
Check(fourthTable.Contains("public bool CanTouchPapers => !introPending") &&
    Src("Scripts/Level3/Level3Checkpointer.cs").Contains("Level3BeaTable.Instance.Seated && !Level3BeaTable.Instance.IntroPending") &&
    Body6(fourthTable, "private void HandleLeftSeat()", "private void GatherAll()").Contains("CancelIntro();") &&
    Body6(fourthTable, "private void CancelIntro()", "private bool SitAtTable(").Contains("StopCoroutine(introRoutine)"),
    "Pending entrance blocks table/guide input and leaving cancels both the wait and the owned splash");
string fourthDeliveryManager = Body6(Src("Scripts/RewardsSystem/PlayerProgressManager.cs"), "public bool CommitLevelThreeDelivery(", "/// <summary>");
Check(Index(fourthDeliveryManager, "ReleaseCopyInventory(candidate.campaign.inventory") < Index(fourthDeliveryManager, "TryWriteCandidate(candidate)") &&
    fourthDeliveryManager.Contains("InventoryChanged?.Invoke()") &&
    Body6(Src("Scripts/UI/HUD/HudHotbar.cs"), "private bool FolderCarried()", "private void SyncWorld()").Contains("Level3ReviewRules.FolderInWorld") &&
    Body6(Src("Scripts/UI/HUD/HudHotbar.cs"), "private void SyncWorld()", "private void SyncContents()").Contains("RemoveWorld(InventoryCatalogue.CaseFolder)"),
    "Runtime saves actual copy removal before publishing delivery; older saves repair through existing world-removal retry and cannot reacquire an invisible copy");
var fourthReadingState = delivered10.Copy();
string fourthReadingBefore = Snap(fourthReadingState);
var fourthReference = Level3DeliveryRules.ReadDeliveredRecords(fourthReadingState, bea);
Check(fourthReference != null && fourthReference.referencePages.Count > 0 && Snap(fourthReadingState) == fourthReadingBefore &&
    Level3DeliveryRules.ReadDeliveredRecords(explained10, bea) == null,
    "Review her records still uses the existing read-only reader after delivery, without recreating inventory ownership or changing the chapter");
var fourthPartialReading = delivered10.Copy();
fourthPartialReading.Case(B7r).acquiredSourceIds = new List<string> { "B-ID" };
var fourthPartialReference = Level3DeliveryRules.ReadDeliveredRecords(fourthPartialReading, bea);
Check(fourthPartialReference != null && fourthPartialReference.referencePages.All(page => page.id.StartsWith(B7r + "/B-ID/")),
    "The delivered-copy reference route exposes only acquired originals, never unacquired source papers");
Console.WriteLine($"Fourth playtest: {checks-fourthStart} checks, {fails-fourthFails} failures; native camera/splash and empty-slot presentation remain manual.");

// Fifth playtest: a failed audit removes the physical folder from inventory and restores a draggable stack folder.
int fifthStart = checks, fifthFails = fails;
var fifthReturned = fixedCredit.Copy();
Level3ReviewRules.CaptureFolder(fifthReturned, correctFront);
Level3ReviewRules.SetAttachment(fifthReturned, Level3WorksheetCheck.WorksheetWorkId, true, out _);
Level3ReviewRules.SetAttachment(fifthReturned, Level3ChapterRules.BeaCreditSource, true, out _);
fifthReturned.Case(B7r).folder.incomeSource = "BusinessIncome";
Level3ReviewRules.Stamp(fifthReturned, R9, out _);
Check(Level3ReviewRules.Submit(fifthReturned, bea, rc9, out var fifthReturnDecision, out _) == Level3CommandOutcome.Applied && fifthReturnDecision.Returned,
    "Fifth reproduction: an actual wrong front-page value returns the working folder");
var fifthInventory = new CampaignInventoryRecord();
InventoryRules.Acquire(fifthInventory, thirdCatalogue, InventoryCatalogue.CaseFolder, 1, "fifth/owned");
var fifthOldSlot = InventoryRules.Find(fifthInventory, InventoryCatalogue.CaseFolder);
Level3ReviewRules.SyncFolderInventory(fifthInventory, thirdCatalogue, fifthReturned);
Check(!InventoryRules.Owns(fifthInventory, InventoryCatalogue.CaseFolder) && InventoryRules.SlotAt(fifthInventory, fifthOldSlot).IsEmpty &&
    InventoryRules.Record(fifthInventory, InventoryCatalogue.CaseFolder) == null,
    "Fifth reproduction: the submitted/returned working folder vacates its real inventory slot, not only its view");
string fifthReturnSource = Body6(Src("Scripts/CaseFolder/OpenFolderReturnToDesk.cs"), "public void ForceReturnToDesk()", "private void UnusedEndMarker()");
Check(fifthReturnSource.Contains("RestoreAndEnableFolder()"),
    "Fifth reproduction: forcing the open folder back to the stack also re-enables the closed folder drag after storage disabled it");
bool FifthRebuild(Level3ChapterCheckpoint fifthState)
{
    Level3ReviewRules.CaptureFolder(fifthState, correctFront);
    fifthState.Case(B7r).folder.onDesk = false; // the existing saved FolderStored event
    var fifthA = PhysicalA(fifthState, Level3CalcKey.Subtract);
    if (Attach7r(fifthState, "taxable-income", fifthA) != Level3CommandOutcome.Applied) return false;
    if (Level3ComputationRules.SelectRule(fifthState, bea, "IND-20", out _) != Level3CommandOutcome.Applied) return false;
    var fifthDue = new Level3Calculator();
    bool FifthInsert(Level3Calculator fifthCalc, string fifthSource) =>
        Level3ValueTransfer.InsertComputation(FixedSource(fifthState, fifthSource, "Amount"), fifthState, bea, fifthCalc);
    if (!FifthInsert(fifthDue, Level3ComputationRules.SheetSource(B7r, "taxable-income"))) return false;
    fifthDue.Press(Level3CalcKey.Subtract);
    if (!FifthInsert(fifthDue, Level3ComputationRules.RuleSource("IND-20", "lower"))) return false;
    fifthDue.Press(Level3CalcKey.Equals); fifthDue.Press(Level3CalcKey.Multiply);
    if (!FifthInsert(fifthDue, Level3ComputationRules.RuleSource("IND-20", "rate"))) return false;
    fifthDue.Press(Level3CalcKey.Equals); fifthDue.Press(Level3CalcKey.Add);
    if (!FifthInsert(fifthDue, Level3ComputationRules.RuleSource("IND-20", "base"))) return false;
    fifthDue.Press(Level3CalcKey.Equals);
    if (Attach7r(fifthState, "tax-due", fifthDue) != Level3CommandOutcome.Applied) return false;
    if (Place7r(fifthState, "credits", Pick7r(fifthState, Wht8r), out _) != Level3CommandOutcome.Applied) return false;
    var fifthBalance = new Level3Calculator();
    if (!FifthInsert(fifthBalance, Level3ComputationRules.SheetSource(B7r, "tax-due"))) return false;
    fifthBalance.Press(Level3CalcKey.Subtract);
    if (!FifthInsert(fifthBalance, Level3ComputationRules.FieldSource(B7r, "credits"))) return false;
    fifthBalance.Press(Level3CalcKey.Equals);
    return Attach7r(fifthState, "remaining-payable", fifthBalance) == Level3CommandOutcome.Applied &&
        Level3ReviewRules.SetAttachment(fifthState, Level3WorksheetCheck.WorksheetWorkId, true, out _) == Level3CommandOutcome.Applied &&
        Level3ReviewRules.SetAttachment(fifthState, Level3ChapterRules.BeaCreditSource, true, out _) == Level3CommandOutcome.Applied &&
        Level3ComputationRules.Evaluate(fifthState, bea, B7r).PayableSupported;
}
string fifthWorksheetBefore = Snap(fifthReturned.worksheetCheck);
for (int fifthAttempt = 0; fifthAttempt < 3; fifthAttempt++)
{
    fifthReturned = Clone(fifthReturned); // reload at the physical desk return, including onDesk and prior review
    Check(Level3ReviewRules.FolderInWorld(fifthReturned) && Level3ComputationRules.Editable(fifthReturned, B7r) &&
        InventoryRules.Acquire(fifthInventory, thirdCatalogue, InventoryCatalogue.CaseFolder, 1, "fifth/reopen/" + fifthAttempt).Changed,
        "Returned/reloaded folder can be taken from the desk stack again, attempt " + fifthAttempt);
    Check(FifthRebuild(fifthReturned), "All actual summary/band/certificate/calculator/tape commands work after return, attempt " + fifthAttempt);
    if (fifthAttempt < 2) fifthReturned.Case(B7r).folder.incomeSource = "BusinessIncome";
    Check(Level3ReviewRules.Stamp(fifthReturned, R9, out _) == Level3CommandOutcome.Applied &&
        Level3ReviewRules.Refusal(fifthReturned, B7r) == null &&
        Level3ReviewRules.Submit(fifthReturned, bea, rc9, out var fifthNextDecision, out _) == Level3CommandOutcome.Applied &&
        (fifthAttempt < 2 ? fifthNextDecision.Returned : fifthNextDecision.Outcome == Level3ReviewOutcome.Approved),
        "Repeated changed submissions accept another correction; the final correct rebuilt folder approves, attempt " + fifthAttempt);
    Level3ReviewRules.SyncFolderInventory(fifthInventory, thirdCatalogue, fifthReturned);
    Check(!InventoryRules.Owns(fifthInventory, InventoryCatalogue.CaseFolder) && Snap(fifthReturned.worksheetCheck) == fifthWorksheetBefore &&
        fifthReturned.Case(B7r).review.submissions == fifthAttempt + 2,
        "Every hand-in frees the actual slot, keeps the frozen worksheet and records exactly one new review, attempt " + fifthAttempt);
}
Check(fifthReturned.Case(B7r).review.returned == 3 && fifthReturned.Case(B7r).review.approved &&
    fifthReturned.Case(B7r).copyPhase == Level3ReviewRules.CopyAtAuditor,
    "Three failed reviews followed by a full redo reach the normal Auditor client-copy pickup");
var fifthCopyCandidate = fifthReturned.Copy();
Check(Level3ReviewRules.CollectCopy(fifthCopyCandidate, out _) == Level3CommandOutcome.Applied &&
    Level3ReviewRules.SyncFolderInventory(fifthInventory, thirdCatalogue, fifthCopyCandidate).Changed &&
    InventoryRules.Owns(fifthInventory, InventoryCatalogue.CaseFolder) && !Level3ReviewRules.FolderInWorld(fifthCopyCandidate),
    "Deliberate client-copy pickup reacquires a real normal folder slot after the approved working-folder hand-in removed it");
var fifthFull = Rich13(false);
InventoryRules.Remove(fifthFull, thirdCatalogue, InventoryCatalogue.CaseFolder, null);
InventoryRules.Acquire(fifthFull, thirdCatalogue, InventoryCatalogue.BeaSummary, 1, "fifth/fill-copy-slot");
var fifthFullCopy = fifthCopyCandidate.Copy();
fifthFullCopy.Case(B7r).folder.summaryAttached = false;
fifthFullCopy.Case(B7r).folder.certificateAttached = false;
string fifthFullBefore = Snap(fifthFull);
Check(!Level3ReviewRules.SyncFolderInventory(fifthFull, thirdCatalogue, fifthFullCopy).Ok && Snap(fifthFull) == fifthFullBefore,
    "A full inventory refuses the new copy pickup without displacing prior possessions");
string fifthRestoreBody = Body6(Src("Scripts/CaseFolder/OpenFolderReturnToDesk.cs"), "public void ForceReturnToDesk()", "private void UnusedEndMarker()");
Check(Index(fifthRestoreBody, "RestoreAndEnableFolder()") < Index(fifthRestoreBody, "ReturnFolderToDesk(false)") &&
    Body6(Src("Scripts/CaseFolder/CaseFolder3DDrag.cs"), "public void RestoreAndEnableFolder()", "private void LateUpdate()").Contains("allowDragging = true;"),
    "The shared forced-return path restores the actual closed drag owner before placing it on the stack");
string fifthReviewCommit = Body6(Src("Scripts/Level3/Level3Checkpointer.cs"), "public ReviewCommit CommitReview(", "/// <summary>Bea's clock settings");
Check(fifthReviewCommit.Contains("SyncFolderInventory(inv, bar.Catalogue, candidate)") &&
    Body6(Src("Scripts/RewardsSystem/PlayerProgressManager.cs"), "public bool CommitLevelThreeApproval(", "/// <summary>").Contains("SyncFolderInventory(candidate.campaign.inventory"),
    "Signed approval and temporary returned/approved reviews use the same actual inventory handoff adapter");
var fifthRestamp = fixedCredit.Copy();
Level3ReviewRules.CaptureFolder(fifthRestamp, correctFront);
Level3ReviewRules.SetAttachment(fifthRestamp, Level3WorksheetCheck.WorksheetWorkId, true, out _);
Level3ReviewRules.SetAttachment(fifthRestamp, Level3ChapterRules.BeaCreditSource, true, out _);
Level3ReviewRules.Stamp(fifthRestamp, Level3ReviewRules.StampHold, out _);
Check(Level3ReviewRules.Submit(fifthRestamp, bea, rc9, out var fifthStampDecision, out _) == Level3CommandOutcome.Applied && fifthStampDecision.Field == "stamp",
    "A false hold is returned as a stamp-only correction");
Level3ReviewRules.Stamp(fifthRestamp, Level3ReviewRules.StampHold, out _);
Check(Level3ReviewRules.Refusal(fifthRestamp, B7r) == null,
    "Even the same wrong stamp can be deliberately retried after its physical return; the new hand-in is not permanently locked as unchanged");
string fifthSandbox = Path.Combine(Path.GetTempPath(), "TaxVentureFifth-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fifthSandbox);
try
{
    var fifthStore = new SaveFileStore(Path.Combine(fifthSandbox, "progress.json"));
    string EncodeFifth(PlayerProgressData fifthData)
    {
        string fifthPayload = Snap(fifthData);
        return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = fifthPayload, ["signature"] = SaveFileStore.Sign(fifthPayload) });
    }
    var fifthLiveSave = Level3ChapterRules.WithCheckpoint(Clone(live), fixedCredit.Copy());
    Level3ReviewRules.CaptureFolder(fifthLiveSave.campaign.levelThree, correctFront);
    fifthLiveSave.campaign.levelThree.Case(B7r).folder.incomeSource = "BusinessIncome";
    Level3ReviewRules.Stamp(fifthLiveSave.campaign.levelThree, R9, out _);
    fifthLiveSave.campaign.inventory = new CampaignInventoryRecord();
    InventoryRules.Acquire(fifthLiveSave.campaign.inventory, thirdCatalogue, InventoryCatalogue.CaseFolder, 1, "fifth/signed-folder", SlotRef.Bag(0));
    fifthStore.Write(EncodeFifth(fifthLiveSave));
    string fifthLiveBefore = Snap(fifthLiveSave); byte[] fifthDiskBefore = File.ReadAllBytes(fifthStore.FilePath);
    var fifthNextSave = Clone(fifthLiveSave);
    Level3ReviewRules.Submit(fifthNextSave.campaign.levelThree, bea, rc9, out _, out _);
    Level3ReviewRules.SyncFolderInventory(fifthNextSave.campaign.inventory, thirdCatalogue, fifthNextSave.campaign.levelThree);
    Directory.CreateDirectory(fifthStore.TempPath);
    bool fifthWriteFailed = false;
    try { fifthStore.Write(EncodeFifth(fifthNextSave)); } catch (IOException) { fifthWriteFailed = true; } catch (UnauthorizedAccessException) { fifthWriteFailed = true; }
    Check(fifthWriteFailed && Snap(fifthLiveSave) == fifthLiveBefore && fifthDiskBefore.SequenceEqual(File.ReadAllBytes(fifthStore.FilePath)) &&
        InventoryRules.Owns(fifthLiveSave.campaign.inventory, InventoryCatalogue.CaseFolder),
        "An unsaved failed audit retains the old carried folder and review; candidate removal cannot lose the actual item");
    Directory.Delete(fifthStore.TempPath);
    fifthStore.Write(EncodeFifth(fifthNextSave));
    using var fifthEnvelope = JsonDocument.Parse(File.ReadAllText(fifthStore.FilePath));
    string fifthPayloadBack = fifthEnvelope.RootElement.GetProperty("payload").GetString();
    var fifthReloadedSave = JsonSerializer.Deserialize<PlayerProgressData>(fifthPayloadBack, json);
    Check(fifthEnvelope.RootElement.GetProperty("signature").GetString() == SaveFileStore.Sign(fifthPayloadBack) &&
        fifthReloadedSave.campaign.levelThree.Case(B7r).folder.onDesk &&
        !InventoryRules.Owns(fifthReloadedSave.campaign.inventory, InventoryCatalogue.CaseFolder) &&
        fifthReloadedSave.campaign.inventory.bag[0].IsEmpty &&
        Level3ComputationRules.Editable(fifthReloadedSave.campaign.levelThree, B7r),
        "Signed retry/reload puts the wrong folder on the desk, its inventory slot genuinely empty, and the same case editable");
    var fifthOldGhostSave = Clone(fifthReloadedSave);
    InventoryRules.Acquire(fifthOldGhostSave.campaign.inventory, thirdCatalogue, InventoryCatalogue.CaseFolder, 1, "fifth/legacy-ghost");
    Check(Level3ReviewRules.SyncFolderInventory(fifthOldGhostSave.campaign.inventory, thirdCatalogue, fifthOldGhostSave.campaign.levelThree).Changed &&
        !InventoryRules.Owns(fifthOldGhostSave.campaign.inventory, InventoryCatalogue.CaseFolder),
        "Existing returned saves with a ghost carried folder repair using the same location-driven inventory removal");
}
finally { try { Directory.Delete(fifthSandbox, true); } catch (IOException) { } }
Level3ReviewRules.Stamp(fifthRestamp, R9, out _);
Check(Level3ReviewRules.Submit(fifthRestamp, bea, rc9, out var fifthRestampApproval, out _) == Level3CommandOutcome.Applied &&
    fifthRestampApproval.Outcome == Level3ReviewOutcome.Approved,
    "Correcting only the stamp after its return still approves the retained calculations and both supporting papers");
Console.WriteLine($"Fifth playtest: {checks-fifthStart} checks, {fails-fifthFails} failures; native stack drag and inventory appearance remain manual.");

// =====================================================================
// Teaching pass (6 November 2026): Worth the Tax? practice packet, the four-question reference,
// and three short desk examples (taxable income, tax table, withholding). Real rule commands,
// the real local machine and the signed store; no Unity scene, rendering, touch or learning claim.
// =====================================================================
int teachStart = checks, teachFails = fails;
const string TeachBea = "L3-BEA-001";

// ---------- practice packet: when it stands, and for whom ----------
Level3ChapterCheckpoint sortDue = Level3EditorFixtures.BeaEncounter(bea, handedOver: true);
var rowsTeach = Level3WorksheetRules.RowsOf(bea);
Level3ChapterCheckpoint filedOne = sortDue.Copy();
Check(Level3WorksheetRules.File(filedOne, rowsTeach, new Level3Filing { RowId = "R1", Treatment = Level3WorksheetRules.Income, BasedOn = filedOne.worksheet.revision }, out _) == Level3CommandOutcome.Applied,
    "teaching: a real first filing for the 'already past the packet' state");
Check(Level3SortingPractice.Due(sortDue) && Level3ChapterRules.PhaseOf(sortDue) == Level3Phase.WorksheetDraft && !Level3ChapterRules.BeaWorkIndependent(sortDue),
    "practice packet: due right after Bea's saved handover, before anything of hers is filed");
Check(!Level3SortingPractice.Due(assigned) && !Level3SortingPractice.Due(filedOne) && !Level3SortingPractice.Due(office) && !Level3SortingPractice.Due(V1(sortDue)) &&
      !Level3SortingPractice.Due(Level3EditorFixtures.MachineBatch(bea, Level3WorksheetRules.CostsBatch)) && !Level3SortingPractice.Due(null),
    "practice packet: never before the handover, never once a record of hers is filed or a batch accepted, never at the office, never on an uncontinued v1 chapter (older saves continue unchanged)");

// ---------- practice slips: content, isolation, judging ----------
Check(Level3SortingPractice.RowIds.SequenceEqual(new[] { "P1", "P2", "P3", "P4" }) && !Level3SortingPractice.RowIds.Intersect(Level3WorksheetRules.RowIds).Any() &&
      Level3SortingPractice.RowIds.All(id => Level3WorksheetRules.BatchOf(id) == null && Level3WorksheetRules.NoteForRow(id) == null && !Level3WorksheetRules.NeedsReason(id)) &&
      Level3SortingPractice.RowIds.Select(Level3SortingPractice.Row).All(r => r != null && r.SourceId == null && r.FieldKey == null && r.TransactionId == null),
    "practice slips are their own four rows: never one of Bea's rows, batches, notes or sources");
var beaMoneyTeach = bea.level3.rows.Select(r => r.amount.Replace(".00", "")).Concat(bea.level3.totals.Select(t => t.amount.Replace(".00", "")))
    .Concat(new[] { "600000", "62500", "50000", "12500", "900000", "300000", "354000" }).ToList();
Check(Level3SortingPractice.RowIds.Select(Level3SortingPractice.Row).All(r => !beaMoneyTeach.Contains(r.Amount)),
    "practice slips use none of Bea's amounts or answers");
var facesTeach = Level3SortingPractice.RowIds.Select(Level3SortingPractice.Face).ToList();
Check(facesTeach.Select(f => f.Position).SequenceEqual(new[] { "Practice 1 of 4", "Practice 2 of 4", "Practice 3 of 4", "Practice 4 of 4" }) &&
      facesTeach.All(f => f.Details.Contains("No score") && f.Amount.StartsWith("₱") && f.Title.Split(' ').Length <= 6 && f.Purpose.Split(' ').Length <= 10),
    "each stand card says Practice n of 4 and No score, with a short title and one short purpose line");
Check(Level3SortingPractice.NeedsReason("P3") && Level3SortingPractice.NeedsReason("P4") && !Level3SortingPractice.NeedsReason("P1") && !Level3SortingPractice.NeedsReason("P2") &&
      facesTeach[3].Purpose.Contains("PR-02") && facesTeach[1].Purpose.Contains("PR-02") && facesTeach[2].Purpose.Contains("Receipt"),
    "the concepts are on the cards: earnings, a supported cost (receipt PR-02), a personal purchase with a receipt, and a second record of PR-02");
var rightTeach = new (string row, string treatment, string reason)[]
{
    ("P1", Level3WorksheetRules.Income, ""), ("P2", Level3WorksheetRules.Deduct, ""),
    ("P3", Level3WorksheetRules.LeaveOut, "personal-purpose"), ("P4", Level3WorksheetRules.LeaveOut, "already-represented")
};
foreach (var (row, treatment, slipReason) in rightTeach)
{
    Check(Level3SortingPractice.Judge(row, treatment, slipReason, out string why) && !string.IsNullOrWhiteSpace(why) && why.Split(' ').Length <= 12,
        $"practice {row}: the right choice feeds in with one short why ('{why}')");
    foreach (string other in Level3WorksheetRules.Treatments.Where(t => t != treatment))
    {
        bool wrong = Level3SortingPractice.Judge(row, other, slipReason, out string hint);
        Check(!wrong && !string.IsNullOrWhiteSpace(hint) && hint.Contains("?") && hint.Split(' ').Length <= 12 &&
              !new[] { "Income", "Deduct", "Leave Out" }.Any(hint.Contains),
            $"practice {row} as {Level3WorksheetRules.TreatmentLabel(other)}: explained with the question to ask, no category named ('{hint}')");
    }
    if (slipReason.Length > 0)
        foreach (string otherReason in Level3WorksheetRules.Reasons.Where(x => x != slipReason))
        {
            bool wrong = Level3SortingPractice.Judge(row, treatment, otherReason, out string hint);
            Check(!wrong && !string.IsNullOrWhiteSpace(hint) && !hint.Contains(Level3WorksheetRules.ReasonLabel(slipReason)),
                $"practice {row}, reason {otherReason}: a hint for the reason that never names the right one ('{hint}')");
        }
}
Check(!Level3SortingPractice.Judge("R1", Level3WorksheetRules.Income, "", out _) && !Level3SortingPractice.Judge("P1", "", "", out _) &&
      !Level3SortingPractice.Judge("P1", "expenses", "", out _) && Level3SortingPractice.Next(0) == "P1" && Level3SortingPractice.Next(3) == "P4" && Level3SortingPractice.Next(4) == null,
    "practice judging refuses Bea's rows and unknown choices; the next slip follows the right answers");

// ---------- practice slips through the existing local machine (feed -> choose -> Why? -> lever) ----------
var mTeach = new Level3TableStaging.Machine();
Check(mTeach.Present("P3") && mTeach.Feed("", "", 0) && !mTeach.CanAskWhy && mTeach.Missing == Level3WorksheetRules.ChooseCategory &&
      mTeach.Choose(Level3WorksheetRules.Deduct) && mTeach.CanAskWhy && mTeach.Missing == Level3WorksheetRules.ChooseReason &&
      mTeach.ChooseReason("personal-purpose") && mTeach.Missing == null && mTeach.BeginFile(out Level3Filing practiceFiling) &&
      practiceFiling.RowId == "P3" && practiceFiling.Reason == "personal-purpose",
    "the real machine flow takes a practice slip: neutral start, treatment, then Why? on P3 exactly as on R6/R7");
mTeach.Saved(false);
Check(mTeach.Step == Level3TableStaging.MachineStep.Loaded && mTeach.Treatment == Level3WorksheetRules.Deduct && mTeach.Reason == "personal-purpose" &&
      mTeach.Choose(Level3WorksheetRules.LeaveOut) && mTeach.Missing == null,
    "a wrong practice pull keeps the slip loaded with its choices (retry); changing the treatment keeps the chosen reason");
var mTeach2 = new Level3TableStaging.Machine();
Check(mTeach2.Present("P1") && mTeach2.Feed("", "", 0) && !mTeach2.NeedsReason && mTeach2.Choose(Level3WorksheetRules.Income) && mTeach2.Missing == null,
    "P1/P2 never ask Why?");

// ---------- completing the packet: one marker, nothing else ----------
Level3ChapterCheckpoint sortDone = sortDue.Copy();
string sortDueSnap = Snap(sortDue);
Check(Level3SortingPractice.Complete(sortDone, out string sortWhy) == Level3CommandOutcome.Applied && sortWhy == null && Level3SortingPractice.Taught(sortDone) &&
      !Level3SortingPractice.Due(sortDone) && sortDone.actionIds.Contains(Level3SortingPractice.Action) && Snap(sortDue) == sortDueSnap,
    "the packet's completion saves its milestone and action id on the candidate only");
Level3ChapterCheckpoint sortMarkerOnly = sortDone.Copy();
sortMarkerOnly.milestones.Remove(Level3SortingPractice.Milestone);
sortMarkerOnly.actionIds.Remove(Level3SortingPractice.Action);
Check(Snap(sortMarkerOnly) == sortDueSnap,
    "nothing else changes: no filing, worksheet, check, grade, XP, coins, Trust, phase or clock");
string sortDoneSnap = Snap(sortDone);
Check(Level3SortingPractice.Complete(sortDone, out _) == Level3CommandOutcome.Duplicate && Snap(sortDone) == sortDoneSnap,
    "a repeated completion (double pull, restore) is a quiet duplicate");
foreach (var (what, state) in new[] { ("after a real filing", filedOne.Copy()), ("at the office", office.Copy()), ("before the handover", assigned.Copy()), ("an uncontinued v1 chapter", V1(sortDue)) })
{
    string snapBefore = Snap(state);
    Check(Level3SortingPractice.Complete(state, out string why) == Level3CommandOutcome.Refused && Snap(state) == snapBefore && !Level3SortingPractice.Taught(state),
        $"practice completion refused without change: {what} ('{why}')");
}
Check(Level3TableStaging.ActiveRow(sortDone, rowsTeach)?.RowId == "R1" && Level3TableStaging.ActiveRow(sortDue, rowsTeach)?.RowId == "R1",
    "Bea's own first record is still R1 and untouched by the packet");
Check(!Level3ReviewRules.ClockDue(sortDue) && !Level3ReviewRules.ClockDue(sortDone) && Level3ReviewRules.ClockDue(filedOne),
    "neither the practice packet nor its saved milestone starts Bea's review clock (her first saved filing still does)");
Check(Level3ReviewClock.TeachingPaused(sortDue) && !Level3ReviewClock.TeachingPaused(sortDone) && !Level3ReviewClock.TeachingPaused(filedOne),
    "the packet counts as compulsory teaching (paused time) only while it is due");
Check(Level3ChapterRules.ImpliedLessons(sortDone).Contains(Level3ChapterRules.SortingPracticeLesson) && Level3ChapterRules.ImpliedLessons(sortDone).Contains(Level3ChapterRules.WorksheetLesson) &&
      !Level3ChapterRules.ImpliedLessons(sortDue).Contains(Level3ChapterRules.SortingPracticeLesson) &&
      Level3ChapterRules.ImpliedLessons(filedOne).Contains(Level3ChapterRules.SortingPracticeLesson),
    "resume: a finished packet (or a chapter already past it) never replays its guide lessons; an unfinished one does");

// ---------- practice packet: signed store failure, retry, reload ----------
string sandboxTeach = Path.Combine(Path.GetTempPath(), "TaxVentureL3Teach-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandboxTeach);
try
{
    string PayT(PlayerProgressData d) => JsonSerializer.Serialize(d, json);
    string EncT(PlayerProgressData d) => JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = PayT(d), ["signature"] = SaveFileStore.Sign(PayT(d)) });
    var storeT = new SaveFileStore(Path.Combine(sandboxTeach, "progress.json"));
    PlayerProgressData pubT = Level3ChapterRules.WithCheckpoint(Clone(live), sortDue);
    storeT.Write(EncT(pubT));
    byte[] diskT = File.ReadAllBytes(storeT.FilePath);
    string pubBeforeT = Snap(pubT);
    // Mirrors Level3Checkpointer.CommitSortingPractice -> CommitLevelThreeCheckpoint -> TryWriteCandidate.
    bool CommitPacket(ref PlayerProgressData published)
    {
        Level3ChapterCheckpoint cand = published.campaign.levelThree.Copy();
        if (Level3SortingPractice.Complete(cand, out _) != Level3CommandOutcome.Applied) return false;
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(published), cand);
        try { next.Normalize(); storeT.Write(EncT(next)); } catch (Exception) { return false; }
        published = next;
        return true;
    }
    Directory.CreateDirectory(storeT.TempPath);
    Check(!CommitPacket(ref pubT) && Snap(pubT) == pubBeforeT && File.ReadAllBytes(storeT.FilePath).SequenceEqual(diskT) && !Level3SortingPractice.Taught(pubT.campaign.levelThree),
        "a failed packet write saves nothing: published chapter and file unchanged (the last slip stays loaded to pull again)");
    Directory.Delete(storeT.TempPath);
    Check(CommitPacket(ref pubT) && Level3SortingPractice.Taught(pubT.campaign.levelThree) && !CommitPacket(ref pubT),
        "retry saves once; a further completion writes nothing");
    using var envT = JsonDocument.Parse(File.ReadAllText(storeT.FilePath));
    string payloadT = envT.RootElement.GetProperty("payload").GetString();
    var backT = JsonSerializer.Deserialize<PlayerProgressData>(payloadT, json);
    Check(envT.RootElement.GetProperty("signature").GetString() == SaveFileStore.Sign(payloadT) && !backT.Normalize() &&
          Level3SortingPractice.Taught(backT.campaign.levelThree) && !Level3SortingPractice.Due(backT.campaign.levelThree) &&
          Level3ChapterRules.Decide(backT.campaign.levelThree) == Level3EntryDecision.Resume && WithoutChapter(backT) == liveWithout,
        "reload: the signed save resumes with the packet done (it never replays) and no wallet, Trust, claim, XP, inventory or history change");
}
finally { try { Directory.Delete(sandboxTeach, true); } catch (IOException) { } }

// ---------- the four-question reference and checked-error pointers ----------
var bookTeach = Json("Levels/Level_03/book.json").GetProperty("sections").EnumerateArray().ToList();
var costsTeach = bookTeach.First(s => s.GetProperty("heading").GetString().StartsWith("Section 4"));
string costsBodyTeach = costsTeach.GetProperty("body").GetString();
Check(new[] { "Was it earned?", "Was it for the business?", "Is it supported?", "Is it already counted?" }.All(costsBodyTeach.Contains) &&
      costsTeach.GetProperty("clickableValues").EnumerateArray().Any(v => v.GetProperty("displayText").GetString() == "Four Questions") &&
      costsBodyTeach.Split("\n\n").Length == costsTeach.GetProperty("clickableValues").GetArrayLength() &&
      bookTeach.Count(s => s.GetProperty("heading").GetString().StartsWith("Section ")) == 8,
    "the rereadable reference: Section 4 asks the four questions (one paragraph per term); no section added or renumbered");
var hintsTeach = Json("Levels/Level_03/worksheet.json").GetProperty("hints").EnumerateArray().ToList();
var builtInTeach = Level3WorksheetHints.BuiltIn();
foreach (var set in hintsTeach.Where(h => h.TryGetProperty("brief", out _)))
{
    string brief = set.GetProperty("brief").GetString();
    string first = set.GetProperty("decisions")[0].GetString();
    Check(brief.Contains("?") && !new[] { "Income", "Deduct", "Leave Out", "Allowable", "Not deductible", "Personal purpose", "Already represented" }.Any(brief.Contains) &&
          builtInTeach.hints.First(h => h.decisions.Contains(first)).brief == brief && Level3WorksheetHints.LookAgain(builtInTeach, first, false) == brief,
        $"checked-error pointer for {first}: a question plus where to look, never the category or reason; the built-in copy matches ('{brief}')");
}

// ---------- desk examples: one calculator helper over any example card ----------
Level3Calculator TeachCalc(Level3CardSpec spec, params string[] steps)
{
    var c = new Level3Calculator();
    foreach (string step in steps)
    {
        if (step == "-") c.Press(Level3CalcKey.Subtract);
        else if (step == "+") c.Press(Level3CalcKey.Add);
        else if (step == "*") c.Press(Level3CalcKey.Multiply);
        else if (step == "=") c.Press(Level3CalcKey.Equals);
        else if (step.StartsWith("#")) Keys(c, step.Substring(1));
        else
        {
            Level3PracticeSource src = spec.Sources.First(x => x.SourceId == step);
            if (src.Percent) c.SelectSourcePercent(src.SourceId, src.Label, src.Amount); else c.SelectSource(src.SourceId, src.Label, src.Amount);
        }
    }
    return c;
}
Level3ChapterCheckpoint Untaught(Level3ChapterCheckpoint s)
{
    if (s == null) return null;
    Level3ChapterCheckpoint c = s.Copy();
    c.milestones.Remove(Level3PracticeRules.TaxTaughtMilestone);
    c.milestones.Remove(Level3PracticeRules.CreditTaughtMilestone);
    return c;
}
// The Section 7 band floor 250,000 is public table data (it happens to equal her certificate's payment); every other example amount is unrelated to her file.
Check(Level3PracticeRules.LessonCards.All(c => c.IsLesson && c.Slots.Length == 1 && c.Note.Length > 0 && c.Title.StartsWith("EXAMPLE")) &&
      !Level3PracticeRules.PracticeCard.IsLesson && Level3PracticeRules.LessonCards.SelectMany(c => c.Sources).All(s => !s.SourceId.StartsWith("B-") &&
          (s.SourceId == Level3PracticeRules.TaxThreshold || !beaMoneyTeach.Contains(s.Amount.Replace(".00", "")))) &&
      new[] { "8000", "15000", "13000" }.All(e => !beaMoneyTeach.Contains(e)),
    "three example cards on the existing clipboard: labelled EXAMPLE, one slot each, none of Bea's sources, amounts or answers");
Check(Level3PracticeCardModel.LengthOf(Level3PracticeRules.LessonCard) == Level3PracticeCardModel.CardLength &&
      Level3PracticeCardModel.LengthOf(Level3PracticeRules.PracticeCard) == Level3PracticeCardModel.CardLength &&
      Level3PracticeCardModel.LengthOf(Level3PracticeRules.TaxCard) > Level3PracticeCardModel.CardLength &&
      Level3PracticeCardModel.LengthOf(Level3PracticeRules.CreditCard) > Level3PracticeCardModel.CardLength &&
      Level3PracticeCardModel.Printed(Level3PracticeRules.TaxSources[2]) == "15%" && Level3PracticeCardModel.Printed(Level3PracticeRules.TaxSources[0]) == "350,000.00",
    "two-amount cards keep their original size; three/four-amount examples lengthen the card instead of shrinking type; a rate prints as 15%");

// ---------- taxable-income example (first office use) ----------
Level3Calculator taxableTeach = TeachCalc(Level3PracticeRules.LessonCard, Level3PracticeRules.LessonFund, "-", Level3PracticeRules.LessonSpent, "=");
Check(Level3PracticeRules.CurrentLesson(office) == Level3PracticeRules.LessonCard && taxableTeach.DraftResult == "8000" &&
      Level3AmountSource.Resolve(office, bea, "worksheet:income") == null,
    "first office use: 10,000 − 2,000 = 8,000 on the real calculator, and Bea's summary waits until it is done");

// ---------- tax-table example (after Bea's taxable income, before her band) ----------
Level3ChapterCheckpoint taxDueT = Untaught(StageA7r());
Level3ChapterCheckpoint bandChosenT = Untaught(StageA7r());
Level3ComputationRules.SelectRule(bandChosenT, bea, "IND-20", out _);
Check(taxDueT != null && Level3PracticeRules.CurrentLesson(taxDueT) == Level3PracticeRules.TaxCard && Level3PracticeRules.TaxLessonDue(taxDueT) &&
      !Level3PracticeRules.CreditLessonDue(taxDueT) && Level3PracticeRules.LessonDue(taxDueT),
    "tax-table example: due once Bea's taxable income tape is attached and before she picks a band");
Check(!Level3PracticeRules.TaxLessonDue(Untaught(o7r)) && !Level3PracticeRules.TaxLessonDue(office) && !Level3PracticeRules.TaxLessonDue(bandChosenT) &&
      !Level3PracticeRules.TaxLessonDue(Untaught(StageB7r())) && !Level3PracticeRules.TaxLessonDue(StageA7r()),
    "tax-table example: never before her taxable income, never once a band or tax due is on her sheet (older saves), never once given");
DataValue bandTeach = Level3ValueTransfer.Band(taxDueT, bea, "IND-20");
string taxableSourceT = Level3ComputationRules.SheetSource(TeachBea, Level3ComputationRules.Taxable);
DataValue taxableValueT = Level3ValueTransfer.ComputationSource(StageA7r(), bea, taxableSourceT, "Taxable income");
Check(bandTeach != null && !Level3ValueTransfer.ReadBand(bandTeach, taxDueT, bea, out _) && Level3ValueTransfer.ComputationSource(taxDueT, bea, taxableSourceT, "Taxable income") == null &&
      taxableValueT != null && !Level3ValueTransfer.CanInsertComputation(taxableValueT, taxDueT, bea) && Level3ValueTransfer.CanInsertComputation(taxableValueT, StageA7r(), bea),
    "while it is due, Bea's band chips and sheet amounts wait (her sheet is read-only): the concept comes before her own step");
Level3Calculator taxGood = TeachCalc(Level3PracticeRules.TaxCard, Level3PracticeRules.TaxIncome, "-", Level3PracticeRules.TaxThreshold, "=", "*", Level3PracticeRules.TaxRate, "=", "+", Level3PracticeRules.TaxBase, "=");
Check(taxGood.DraftResult == "15000" && Level3Calculator.ChainLines(taxGood.DraftChain).Last() == "= 15,000.00",
    "the Practice Design Studio chain on the real calculator: (350,000 − 250,000) × 15% + 0 = 15,000, one sourced tape");
string taxDueSnapT = Snap(taxDueT), beaSheetSnapT = Snap(taxDueT.Case(TeachBea)), beaTapesSnapT = Snap(taxDueT.tapes);
Level3ChapterCheckpoint taxTaughtT = taxDueT.Copy();
Check(Level3PracticeRules.CompleteLesson(taxTaughtT, Level3PracticeRules.TaxCard, taxGood.DraftChain, taxGood.DraftResult, out string taxWhy) == Level3CommandOutcome.Applied &&
      taxWhy == null && Level3PracticeRules.TaxTaught(taxTaughtT) && !Level3PracticeRules.LessonDue(taxTaughtT) && Snap(taxDueT) == taxDueSnapT &&
      Snap(taxTaughtT.Case(TeachBea)) == beaSheetSnapT && Snap(taxTaughtT.tapes) == beaTapesSnapT && taxTaughtT.actionIds.Contains(Level3PracticeRules.TaxTaughtAction),
    "attaching the studio's tape saves only its milestone/action: Bea's sheet, tapes, band, stamp, review and clock are untouched");
Check(Level3ValueTransfer.ReadBand(Level3ValueTransfer.Band(taxTaughtT, bea, "IND-20"), taxTaughtT, bea, out string bandAfterT) && bandAfterT == "IND-20" &&
      Level3ValueTransfer.ComputationSource(taxTaughtT, bea, taxableSourceT, "Taxable income") != null && string.IsNullOrEmpty(Level3ComputationRules.Record(taxTaughtT, TeachBea).ruleId),
    "afterwards Bea's own band and amounts are hers to choose: nothing was prefilled or chosen for her");
var taxRefusalsT = new (string what, Level3Calculator tcalc, string result, string hint)[]
{
    ("no rate step", TeachCalc(Level3PracticeRules.TaxCard, Level3PracticeRules.TaxIncome, "-", Level3PracticeRules.TaxThreshold, "="), null, Level3PracticeRules.TaxNeedsRate),
    ("the rate on the whole income", TeachCalc(Level3PracticeRules.TaxCard, Level3PracticeRules.TaxIncome, "*", Level3PracticeRules.TaxRate, "=", "+", Level3PracticeRules.TaxBase, "="), null, Level3PracticeRules.TaxWholeIncome),
    ("no base-tax step", TeachCalc(Level3PracticeRules.TaxCard, Level3PracticeRules.TaxIncome, "-", Level3PracticeRules.TaxThreshold, "=", "*", Level3PracticeRules.TaxRate, "="), null, Level3PracticeRules.TaxNeedsBase),
    ("the subtraction reversed", TeachCalc(Level3PracticeRules.TaxCard, Level3PracticeRules.TaxThreshold, "-", Level3PracticeRules.TaxIncome, "=", "*", Level3PracticeRules.TaxRate, "=", "+", Level3PracticeRules.TaxBase, "="), null, Level3PracticeRules.TaxOrder),
    ("typed numbers", TeachCalc(Level3PracticeRules.TaxCard, "#350000-250000="), null, Level3PracticeRules.RefuseFromCard),
    ("a printed result that disagrees with its working", taxGood, "15001", Level3PracticeRules.RefuseNoWorking),
    ("no tape", new Level3Calculator(), null, Level3PracticeRules.RefuseNoTape)
};
foreach (var (what, tcalc, result, hint) in taxRefusalsT)
{
    Level3ChapterCheckpoint s = taxDueT.Copy();
    Check(Level3PracticeRules.CompleteLesson(s, Level3PracticeRules.TaxCard, tcalc.DraftChain, result ?? tcalc.DraftResult, out string why) == Level3CommandOutcome.Refused &&
          why == hint && Snap(s) == taxDueSnapT,
        $"tax-table example refused without change, with one plain hint and a retry: {what} ('{why}')");
}
Check(Level3PracticeRules.CompleteLesson(taxTaughtT, Level3PracticeRules.TaxCard, taxGood.DraftChain, taxGood.DraftResult, out _) == Level3CommandOutcome.Duplicate &&
      Level3PracticeRules.CompleteLesson(Untaught(o7r), Level3PracticeRules.TaxCard, taxGood.DraftChain, taxGood.DraftResult, out string taxEarly) == Level3CommandOutcome.Refused &&
      taxEarly == Level3PracticeRules.RefuseNotNow &&
      Level3PracticeRules.CompleteLesson(taxDueT.Copy(), Level3PracticeRules.LessonCard, taxableTeach.DraftExpression, taxableTeach.DraftResult, out _) == Level3CommandOutcome.Duplicate,
    "taught once (a repeat is quiet), never before its moment, and an earlier example cannot be replayed into its place");
Check(Level3ComputationRules.AttachTape(taxTaughtT.Copy(), bea, "tax-due", taxGood.DraftChain, taxGood.DraftResult, out string taxForeign) == Level3CommandOutcome.Refused &&
      taxForeign == Level3ComputationRules.RefuseForeign,
    "the studio's tape can never be attached to Bea's Tax due (not her sources)");

// ---------- withholding example (after her certificate, before her credit) ----------
Level3ChapterCheckpoint certBeforeT = Untaught(StageB7r());
Level3ChapterCheckpoint certT = certBeforeT.Copy();
Level3CertificateRules.Offer(certT, out _);
Check(Level3CertificateRules.HandOver(certT, out _) == Level3CommandOutcome.Applied && Level3CertificateRules.Acquired(certT) &&
      !Level3PracticeRules.CreditLessonDue(certBeforeT) && Level3PracticeRules.CurrentLesson(certT) == Level3PracticeRules.CreditCard,
    "withholding example: due once her certificate is collected (not before), as the only example on the card");
Check(Level3AmountSource.Resolve(certT, bea, "paper:B-WHT/BWithheldCredit") == null,
    "while it is due, the certificate's amounts wait for the example (her credit stays her own later step)");
Level3Calculator creditGood = TeachCalc(Level3PracticeRules.CreditCard, Level3PracticeRules.CreditTaxDue, "-", Level3PracticeRules.CreditWithheld, "=");
string certSnapT = Snap(certT), certSheetT = Snap(certT.Case(TeachBea));
Level3ChapterCheckpoint creditTaughtT = certT.Copy();
Check(creditGood.DraftResult == "13000" &&
      Level3PracticeRules.CompleteLesson(creditTaughtT, Level3PracticeRules.CreditCard, creditGood.DraftChain, creditGood.DraftResult, out string creditWhy) == Level3CommandOutcome.Applied &&
      creditWhy == null && Level3PracticeRules.CreditTaught(creditTaughtT) && !Level3PracticeRules.LessonDue(creditTaughtT) && Snap(certT) == certSnapT &&
      Snap(creditTaughtT.Case(TeachBea)) == certSheetT && creditTaughtT.actionIds.Contains(Level3PracticeRules.CreditTaughtAction),
    "15,000 − 2,000 = 13,000 saves only its milestone: Bea's certificate, sheet and Trust-free credit step are untouched");
Check(Level3AmountSource.Resolve(creditTaughtT, bea, "paper:B-WHT/BWithheldCredit")?.Amount == "12500" &&
      string.IsNullOrEmpty(Level3ComputationRules.Record(creditTaughtT, TeachBea).creditChoice),
    "afterwards her own credit is hers to transfer: nothing was placed for her");
var creditRefusalsT = new (string what, Level3Calculator ccalc, string hint)[]
{
    ("the income payment used as the credit", TeachCalc(Level3PracticeRules.CreditCard, Level3PracticeRules.CreditTaxDue, "-", Level3PracticeRules.CreditPayment, "="), Level3PracticeRules.CreditUsedPayment),
    ("the income payment added back", TeachCalc(Level3PracticeRules.CreditCard, Level3PracticeRules.CreditTaxDue, "+", Level3PracticeRules.CreditPayment, "-", Level3PracticeRules.CreditWithheld, "="), Level3PracticeRules.CreditUsedPayment),
    ("withheld tax added", TeachCalc(Level3PracticeRules.CreditCard, Level3PracticeRules.CreditTaxDue, "+", Level3PracticeRules.CreditWithheld, "="), Level3PracticeRules.CreditAdded),
    ("the subtraction reversed", TeachCalc(Level3PracticeRules.CreditCard, Level3PracticeRules.CreditWithheld, "-", Level3PracticeRules.CreditTaxDue, "="), Level3PracticeRules.CreditOrder),
    ("typed numbers", TeachCalc(Level3PracticeRules.CreditCard, "#15000-2000="), Level3PracticeRules.RefuseFromCard)
};
foreach (var (what, ccalc, hint) in creditRefusalsT)
{
    Level3ChapterCheckpoint s = certT.Copy();
    Check(Level3PracticeRules.CompleteLesson(s, Level3PracticeRules.CreditCard, ccalc.DraftChain, ccalc.DraftResult, out string why) == Level3CommandOutcome.Refused &&
          why == hint && Snap(s) == certSnapT,
        $"withholding example refused without change, one plain hint, retry: {what} ('{why}')");
}
Level3ChapterCheckpoint creditPastT = certT.Copy();
Level3ComputationRules.PlaceInput(creditPastT, bea, "credits", Level3ValueTransfer.Wrap(Level3AmountSource.Resolve(creditTaughtT, bea, "paper:B-WHT/BWithheldCredit")), out _);
Check(!string.IsNullOrEmpty(Level3ComputationRules.Record(creditPastT, TeachBea).creditChoice) && !Level3PracticeRules.CreditLessonDue(creditPastT) &&
      Level3ChapterRules.ImpliedLessons(creditPastT).Contains(Level3ChapterRules.WithholdingLesson) && !Level3ChapterRules.ImpliedLessons(certT).Contains(Level3ChapterRules.WithholdingLesson) &&
      Level3ChapterRules.ImpliedLessons(creditTaughtT).Contains(Level3ChapterRules.WithholdingLesson) &&
      Level3ChapterRules.ImpliedLessons(Untaught(StageB7r())).Contains(Level3ChapterRules.TaxTableLesson) && !Level3ChapterRules.ImpliedLessons(taxDueT).Contains(Level3ChapterRules.TaxTableLesson),
    "older saves already past a step (band or credit on the sheet) never get its example or its guide lesson; a given example never replays");
Level3ChapterCheckpoint reviewedT = Untaught(StageA7r());
reviewedT.Case(TeachBea).review = new Level3ReviewRecord { submissions = 1 };
Check(!Level3PracticeRules.LessonDue(reviewedT),
    "a chapter that was already handed to the Auditor (e.g. a returned full redo) never gets an example");
Check(new[] { office, taxDueT, certT, sortDue, StageA7r(), Untaught(o7r), assigned, filedOne }
          .All(s => new[] { Level3PracticeRules.CalculatorLessonDue(s), Level3PracticeRules.TaxLessonDue(s), Level3PracticeRules.CreditLessonDue(s), Level3SortingPractice.Due(s) }.Count(b => b) <= 1),
    "at most one demonstration stands at a time");

// ---------- the review clock: never advanced by compulsory teaching; a running clock keeps its time ----------
ReviewClockDefinition clockTeach = rc9.Clock(TeachBea);
Level3ChapterCheckpoint runningT = certT.Copy();
Level3ReviewRules.StartClock(runningT, rc9, out _);
ReviewClockState clockStateT = runningT.Case(TeachBea).clock;
float remainingT = clockStateT.remaining;
Check(clockStateT.started && Level3ReviewClock.TeachingPaused(runningT) &&
      ReviewClock.Advance(clockStateT, clockTeach, 45f, Level3ReviewClock.TeachingPaused(runningT), out _) == ReviewClock.Tick.None && clockStateT.remaining == remainingT,
    "while an example is due, Bea's running clock does not move (the same pause the guide and dialogue use)");
Level3ChapterCheckpoint runningDoneT = runningT.Copy();
Check(Level3PracticeRules.CompleteLesson(runningDoneT, Level3PracticeRules.CreditCard, creditGood.DraftChain, creditGood.DraftResult, out _) == Level3CommandOutcome.Applied &&
      runningDoneT.Case(TeachBea).clock.started && runningDoneT.Case(TeachBea).clock.remaining == remainingT && runningDoneT.Case(TeachBea).clock.day == clockStateT.day &&
      !Level3ReviewClock.TeachingPaused(runningDoneT),
    "finishing the example keeps her saved clock exactly (started, time and day); afterwards her own work uses time again");
Check(Level3ReviewRules.ClockDue(Untaught(o7r)) == Level3ReviewRules.ClockDue(o7r) && Level3ReviewRules.ClockDue(taxDueT) == Level3ReviewRules.ClockDue(taxTaughtT),
    "the examples never change when her clock is due to start (only her own record work does)");

// ---------- desk examples: signed save round trip ----------
PlayerProgressData teachSave = JsonSerializer.Deserialize<PlayerProgressData>(Snap(Level3ChapterRules.WithCheckpoint(Clone(live), creditTaughtT)), json);
Check(!teachSave.Normalize() && Level3PracticeRules.CreditTaught(teachSave.campaign.levelThree) && !Level3PracticeRules.LessonDue(teachSave.campaign.levelThree) &&
      WithoutChapter(teachSave) == liveWithout,
    "a given example's marker round-trips without repair and never replays; nothing outside the chapter changes");

// ---------- isolation: fixtures, practice runs ----------
Level3ChapterCheckpoint drillTeach = Level3EditorFixtures.OfficePractice(bea);
Check(drillTeach != null && drillTeach.calculatorTaught && Level3PracticeRules.TaxTaught(drillTeach) && Level3PracticeRules.CreditTaught(drillTeach) &&
      !Level3PracticeRules.TaxTaught(Level3EditorFixtures.FirstOfficeUse(bea)),
    "the Tutorial Hub's office drill (itself practice) skips all three examples; the Editor first-office fixture keeps them");

// ---------- wiring: owners, order, no reward, guide ----------
string cpTeach = Src("Scripts/Level3/Level3Checkpointer.cs");
string packetBody = Body6m(cpTeach, "public TapeCommit CommitSortingPractice(", "/// <summary>Guide cue: the practice packet");
Check(Index(packetBody, "Level3SortingPractice.Complete(candidate") < Index(packetBody, "CommitLevelThreeCheckpoint(candidate)") &&
      Index(packetBody, "CommitLevelThreeCheckpoint(candidate)") < Index(packetBody, "Publish(candidate);") &&
      Index(packetBody, "Publish(candidate);") < Index(packetBody, "RaiseGuideCue") && packetBody.Contains("if (SavesStory &&") &&
      !packetBody.Contains("Grant") && !packetBody.Contains("coins") && !packetBody.Contains("Trust"),
    "packet: candidate, signed write (story only), publish, then the cue; no reward of any kind");
string lessonBodyTeach = Body6m(cpTeach, "public TapeCommit CommitCalculatorLesson(Level3CardSpec lesson,", "/// <summary>\n    /// Teaching pass: Worth the Tax?'s practice packet");
Check(Index(lessonBodyTeach, "Level3PracticeRules.CompleteLesson(candidate, lesson,") < Index(lessonBodyTeach, "CommitLevelThreeCheckpoint(candidate)") &&
      lessonBodyTeach.Contains("if (SavesStory &&") && !lessonBodyTeach.Contains("Grant") && !lessonBodyTeach.Contains("coins"),
    "every desk example: candidate, signed write (story only), publish; no reward");
string tableTeach = Src("Scripts/Level3/Level3BeaTable.cs");
string pullPractice = Body6m(tableTeach, "private void PullPractice()", "/// <summary>The stand/note card face");
string pullLever = Body6m(tableTeach, "private void PullLever()", "/// <summary>\n    /// Teaching pass: a practice slip's pull");
Check(Index(pullLever, "PullPractice();") > 0 && Index(pullLever, "PullPractice();") < Index(pullLever, "owner.CommitWorksheet(") &&
      !pullPractice.Contains("CommitWorksheet") && !pullPractice.Contains("CheckBatch") && !pullPractice.Contains("Trust") && !pullPractice.Contains("Grant") &&
      Index(pullPractice, "Level3SortingPractice.Judge(") < Index(pullPractice, "Machine.LeverBack();") &&
      Index(pullPractice, "owner.CommitSortingPractice(") < Index(pullPractice, "Machine.FeedIn(FeedDone);"),
    "the lever routes practice slips away from Bea's filing: judged at once (wrong: lever back, retry), never filed/checked/rewarded; the packet's save precedes its last feed");
Check(tableTeach.Contains("Level3SortingPractice.Due(chapter) ? Level3SortingPractice.Row(") && Src("Scripts/Level3/Level3TableStaging.cs").Contains("|| Level3SortingPractice.NeedsReason(Row)"),
    "the stand shows practice slips only while the packet is due; the shared machine asks Why? on P3/P4");
string clockTeachSrc = Src("Scripts/Level3/Level3ReviewClock.cs");
Check(clockTeachSrc.Contains("public static bool ClockPaused => Level2ReviewClock.ClockPaused || TeachingPaused(") &&
      clockTeachSrc.Contains("Level3PracticeRules.LessonDue(state) || Level3SortingPractice.Due(state)"),
    "Bea's clock pause includes compulsory teaching, on top of the existing pauses");
string deskTeach = Src("Scripts/Level3/Level3DeskCalculator.cs");
string attachTeach = Body6m(deskTeach, "private void AttachLesson()", "private void TickLessonHandOff()");
Check(Index(attachTeach, "owner.CommitCalculatorLesson(lesson,") < Index(attachTeach, "StartAttachFlight(") && attachTeach.Contains("heldLesson = null;") &&
      deskTeach.Contains("TickLessonSwitch();") && deskTeach.Contains("if (next.IsLesson && !lessonHolding) lessonCalculator.Reset();"),
    "desk: each example's tape flies only after its save; a new example starts on a clear example calculator; one due while lifted settles and re-lifts with its card");
Check(Body6m(Src("Scripts/Level3/Level3DeskCalculatorComputation.cs"), "public bool AttachPrintedToFolder(", "/// <summary>Where a strip").Contains("draftOwner.ConsumeDraft();"),
    "attaching Bea's taxable income consumes her own draft even when the tax-table example becomes due in the same save");
Check(Body6m(Src("Scripts/Level3/Level3ComputationPage.cs"), "public int Signature()", "public bool NeedsRedraw").Contains("Level3PracticeRules.LessonDue(Chapter) ? 32 : 0"),
    "the folder's Computation page redraws when an example starts or finishes (read-only while it stands, editable again after)");
GuideScript guideTeach = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var teachLessons = new[] { Level3ChapterRules.SortingPracticeLesson, Level3ChapterRules.TaxTableLesson, Level3ChapterRules.WithholdingLesson }
    .Select(id => guideTeach.lessons.FirstOrDefault(l => l.id == id)).ToList();
Check(guideTeach.Validate() == "" && teachLessons.All(l => l != null && l.onlyIn == "mode:Workstation" && l.pages.All(p => p.IsHint && !string.IsNullOrEmpty(p.waitFor))) &&
      teachLessons[0].when == "flag:sortingPractice" && teachLessons[1].when == "flag:taxLesson" && teachLessons[2].when == "flag:creditLesson" &&
      teachLessons[1].pages.Last().waitFor == "flag:taxTaught|cue:" + Level3DeskCalculator.CueTapeAttached &&
      teachLessons[2].pages.Last().waitFor == "flag:creditTaught|cue:" + Level3DeskCalculator.CueTapeAttached &&
      guideTeach.lessons.First(l => l.id == "computation-rule").when == "flag:bandStep" && guideTeach.lessons.First(l => l.id == "credit-link").when == "flag:creditStep",
    "guide: one short hint lesson per example, each page waiting for the player's own action; Bea's band and credit hints wait for their example");
Check(teachLessons.SelectMany(l => l.pages).All(p => p.TextFor(true).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 14 &&
          !p.TextFor(true).Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w.Trim('.', ',', ':', ';')).Any(w => new[] { "600,000", "62,500", "12,500", "50,000", "IND-20" }.Contains(w)) &&
          !p.TextFor(true).Contains("over 400,000")),
    "each teaching page is at most 14 words and names none of Bea's figures or her band");
foreach (string flag in new[] { "taxLesson", "taxTaught", "creditLesson", "creditTaught", "bandStep", "creditStep", "sortingPractice", "sortingTaught" })
    Check(cpTeach.Contains("case \"" + flag + "\":"), $"guide flag {flag} reads saved state through the checkpointer");
// ---------- Follow-up: Arthur first (Level 2's watch-then-try pattern) ----------
int arthurStart = checks, arthurFails = fails;
var demoSlips = Level3SortingPractice.Demo();
Check(demoSlips.Select(d => d.RowId).SequenceEqual(Level3SortingPractice.RowIds) &&
      demoSlips.All(d => Level3SortingPractice.Judge(d.RowId, d.Treatment, d.Reason, out _)) &&
      demoSlips.All(d => (d.Why.Length > 0) == Level3SortingPractice.NeedsReason(d.RowId)) &&
      demoSlips.All(d => new[] { d.Read, d.Choose, d.Why }.Where(l => l.Length > 0).All(l => l.Split(' ').Length <= 20)),
    "Arthur demonstrates exactly the task the player then gets: the same four slips, each with its right choice and reason, one short line per step");
var demoMachine = new Level3TableStaging.Machine();
bool demoFlowOk = true;
foreach (var d in demoSlips)
{
    demoFlowOk &= demoMachine.Present(d.RowId) && demoMachine.Feed("", "", 0) && demoMachine.Choose(d.Treatment);
    if (d.Reason.Length > 0) demoFlowOk &= demoMachine.CanAskWhy && demoMachine.ChooseReason(d.Reason);
    demoFlowOk &= demoMachine.Missing == null && demoMachine.BeginFile(out _);
    demoMachine.Saved(true);
    demoFlowOk &= demoMachine.Fed();
}
Check(demoFlowOk && demoMachine.Step == Level3TableStaging.MachineStep.Empty,
    "his demonstration runs through the real local machine (present, half-feed, dial, Why?, lever, full feed) and leaves it empty for the player's turn");
Level3ChapterCheckpoint demoSeenT = sortDue.Copy();
string sortDueSnapA = Snap(sortDue);
Check(!Level3SortingPractice.DemoSeen(sortDue) && Level3SortingPractice.MarkDemoSeen(demoSeenT, out _) == Level3CommandOutcome.Applied &&
      Level3SortingPractice.DemoSeen(demoSeenT) && Level3SortingPractice.Due(demoSeenT) && !Level3SortingPractice.Taught(demoSeenT) && Snap(sortDue) == sortDueSnapA,
    "watching his demonstration saves only its 'seen' marker: the player's own turn is still due");
Level3ChapterCheckpoint demoSeenOnly = demoSeenT.Copy();
demoSeenOnly.milestones.Remove(Level3SortingPractice.DemoMilestone);
demoSeenOnly.actionIds.Remove(Level3SortingPractice.DemoAction);
Check(Snap(demoSeenOnly) == sortDueSnapA && Level3SortingPractice.MarkDemoSeen(demoSeenT, out _) == Level3CommandOutcome.Duplicate &&
      Level3SortingPractice.MarkDemoSeen(filedOne.Copy(), out _) == Level3CommandOutcome.Refused && Level3SortingPractice.DemoSeen(sortDone) &&
      !Level3ReviewRules.ClockDue(demoSeenT) && Level3ReviewClock.TeachingPaused(demoSeenT),
    "nothing else changes; a repeat is quiet; never after Bea's own filing; a finished packet counts as seen; the clock stays unstarted and paused");
Check(Level3SortingPractice.YourTurnLine.StartsWith("Your turn") && Level3SortingPractice.YourTurnLine.Contains("same four"),
    "then Arthur hands the same four slips to the player");
foreach (Level3CardSpec lessonA in Level3PracticeRules.LessonCards)
{
    var calcA = new Level3Calculator();
    foreach (Level3PracticeRules.LessonDemoStep step in Level3PracticeRules.Demo(lessonA))
    {
        if (!string.IsNullOrEmpty(step.SourceId))
        {
            Level3PracticeSource src = lessonA.Sources.First(s => s.SourceId == step.SourceId);
            if (src.Percent) calcA.SelectSourcePercent(src.SourceId, src.Label, src.Amount); else calcA.SelectSource(src.SourceId, src.Label, src.Amount);
        }
        if (step.HasKey) calcA.Press(step.Key);
    }
    Level3ChapterCheckpoint dueA = lessonA == Level3PracticeRules.LessonCard ? office.Copy() : lessonA == Level3PracticeRules.TaxCard ? taxDueT.Copy() : certT.Copy();
    string dueSnapA = Snap(dueA);
    Check(Level3PracticeRules.CurrentLesson(dueA) == lessonA &&
          Level3PracticeRules.CompleteLesson(dueA.Copy(), lessonA, calcA.DraftChain, calcA.DraftResult, out string demoWhyA) == Level3CommandOutcome.Applied &&
          Level3PracticeRules.Demo(lessonA).Count(s => s.Attach) == 1 && Level3PracticeRules.Demo(lessonA).Last().Attach &&
          Level3PracticeRules.Demo(lessonA).Where(s => !string.IsNullOrEmpty(s.Line)).All(s => s.Line.Split(' ').Length <= 22 &&
              !s.Line.Split(' ').Select(w => w.Trim('.', ',', ':')).Any(w => new[] { "600,000", "62,500", "12,500", "50,000", "900,000", "300,000" }.Contains(w))),
        $"{lessonA.Id}: Arthur's demonstration is the same example the player must then do (its chain passes the real rule), clips one tape last, short lines, none of Bea's figures");
    Level3ChapterCheckpoint markA = dueA.Copy();
    Check(!Level3PracticeRules.DemoSeen(dueA, lessonA) && Level3PracticeRules.MarkDemoSeen(markA, lessonA, out _) == Level3CommandOutcome.Applied &&
          Level3PracticeRules.DemoSeen(markA, lessonA) && Level3PracticeRules.LessonDue(markA) && Level3PracticeRules.CurrentLesson(markA) == lessonA &&
          Level3PracticeRules.MarkDemoSeen(markA, lessonA, out _) == Level3CommandOutcome.Duplicate && Snap(dueA) == dueSnapA &&
          Level3PracticeRules.MarkDemoSeen(dueA.Copy(), Level3PracticeRules.LessonCards.First(c => c != lessonA), out _) != Level3CommandOutcome.Applied,
        $"{lessonA.Id}: watching it saves only its seen marker (the player's own turn is still due); never for an example that is not up");
    Check(Level3PracticeRules.YourTurnLine(lessonA).StartsWith("Your turn") && !string.IsNullOrEmpty(Level3PracticeRules.DoneLine(lessonA)),
        $"{lessonA.Id}: then 'your turn', and Arthur closes the example before Bea's own step");
}
Check(Level3PracticeRules.DemoSeen(taxTaughtT, Level3PracticeRules.TaxCard) && Level3PracticeRules.DemoSeen(creditTaughtT, Level3PracticeRules.CreditCard) &&
      Level3PracticeRules.DemoSeen(taxTaughtT, Level3PracticeRules.LessonCard),
    "an example already done (or an older save past it) never replays Arthur's demonstration");
string cpA = Src("Scripts/Level3/Level3Checkpointer.cs");
string demoCommitA = Body6m(cpA, "public TapeCommit CommitDemoSeen(", "/// <summary>Guide cue: the practice packet");
Check(Index(demoCommitA, "MarkDemoSeen(candidate") < Index(demoCommitA, "CommitLevelThreeCheckpoint(candidate)") &&
      Index(demoCommitA, "CommitLevelThreeCheckpoint(candidate)") < Index(demoCommitA, "Publish(candidate);") && demoCommitA.Contains("if (SavesStory &&") &&
      !demoCommitA.Contains("Grant") && !demoCommitA.Contains("coins") && !demoCommitA.Contains("Trust"),
    "the seen marker: candidate, signed write (story only), publish; no reward");
string tableA = Src("Scripts/Level3/Level3BeaTable.cs");
string practiceDemoA = Body6m(tableA, "private IEnumerator PracticeDemo()", "/// <summary>Stops Arthur's demonstration");
Check(practiceDemoA.Contains("Level3SortingPractice.Demo()") && practiceDemoA.Contains("Machine.HalfFeed(") && practiceDemoA.Contains("Machine.LeverStroke();") &&
      practiceDemoA.Contains("Machine.OpenWhy(") && practiceDemoA.Contains("Machine.FeedIn(") && practiceDemoA.Contains("DemoSay(step.Read)") &&
      !practiceDemoA.Contains("CommitWorksheet") && !practiceDemoA.Contains("CommitSortingPractice") && !practiceDemoA.Contains("CheckBatch") &&
      Index(practiceDemoA, "Level3Say(Level3SortingPractice.YourTurnLine") < Index(practiceDemoA, "CommitDemoSeen(null)") &&
      practiceDemoA.Contains("practiceDone = 0;"),
    "the table's demonstration moves the real stand, machine, dial, Why? card and lever with Arthur's lines; it files, checks and saves nothing but its seen marker; then the player starts at slip 1");
Check(tableA.Contains("if (!Seated || Machine == null || demoRunning) return;") && tableA.Contains("public bool CanTouchPapers => !introPending && !demoRunning") &&
      Body6m(tableA, "private void GatherAll()", "if (restacking").Contains("StopPracticeDemo();") &&
      tableA.Contains("!Level3SortingPractice.DemoSeen(chapter))") && tableA.Contains("if (demoRunning) { RefreshMachine(chapter, animate: false); return; }"),
    "while Arthur demonstrates, the player only watches (no machine or paper input); leaving stops it and it replays next visit; it starts only until seen");
string pullA = Body6m(tableA, "private void PullPractice()", "/// <summary>\n    /// Follow-up (Arthur first");
Check(pullA.Contains("Level3Say(feedback, null, 5f);") && pullA.Contains("practiceHint = null;") && !pullA.Contains("practiceHint = feedback"),
    "in the player's turn Arthur says each practice tip (no longer printed on the stand's note card)");
string deskA = Src("Scripts/Level3/Level3DeskCalculator.cs");
string lessonDemoA = Body6m(deskA, "private System.Collections.IEnumerator LessonDemo(", "/// <summary>Putting the tools away");
Check(deskA.Contains("public bool ToolPointerActive => Lifted && AtDesk && !demoRunning;") && Body6m(deskA, "public void PutAway()", "ReleasePointer();").Contains("StopLessonDemo();") &&
      lessonDemoA.Contains("Level3PracticeRules.Demo(lesson)") && lessonDemoA.Contains("calculator.PressKey(") && lessonDemoA.Contains("card.ShowTape(") &&
      !lessonDemoA.Contains("CommitCalculatorLesson") && !lessonDemoA.Contains("CommitComputation") && lessonDemoA.Contains("lessonCalculator.") && !lessonDemoA.Contains("caseCalculator") &&
      Index(lessonDemoA, "ClearDemo();") < Index(lessonDemoA, "YourTurnLine(lesson)") && Index(lessonDemoA, "YourTurnLine(lesson)") < Index(lessonDemoA, "CommitDemoSeen(lesson)"),
    "desk: Arthur works each example on the real card and the example calculator (Bea's draft untouched), clears it, says your turn, then marks it seen; the player cannot touch the tools meanwhile; putting them away stops it");
Check(Body6m(deskA, "private void AttachLesson()", "private void TickLessonHandOff()").Contains("Level3BeaTable.Level3Say(hint, null, 5.5f);") &&
      Body6m(deskA, "private void AttachLesson()", "private void TickLessonHandOff()").Contains("Level3BeaTable.Level3Say(Level3PracticeRules.DoneLine(lesson)"),
    "desk: Arthur explains a wrong example tape and closes the finished example (the card only invites a retry)");
foreach (string gated in new[] { "calculatorLesson", "taxLesson", "creditLesson" })
    Check(cpA.Contains("case \"" + gated + "\":") && Body6m(cpA, "case \"" + gated + "\":", "\n").Contains("!Level3DeskCalculator.Instance.DemoRunning"),
        $"guide flag {gated}: its 'your turn' hints wait until Arthur's demonstration has finished");
Check(Body6m(cpA, "case \"atBeaTable\":", "\n").Contains("!Level3BeaTable.Instance.DemoRunning") && Body6m(cpA, "case \"sortingPractice\":", "\n").Contains("!Level3BeaTable.Instance.DemoRunning"),
    "guide: the machine's feed/choose/pull hints and the practice hint are for the player's turn, after Arthur's demonstration");
Console.WriteLine($"Arthur first: {checks - arthurStart} checks, {fails - arthurFails} failures (scripts, real machine/calculator runs, markers, wiring; Unity motion, bubble timing and feel not run).");

Console.WriteLine($"Teaching pass: {checks - teachStart} checks, {fails - teachFails} failures (rules, machine flow, signed store, clock, guide/book/hint wiring; Unity rendering, touch, timing and learning effect not run).");

// ---------- polish pass (6 November 2026): hotbar slot, splash + camera, guide, objectives, result card, plaza ----------
int polishStart = checks, polishFails = fails;
Check(HudHotbar.SlotFaceShown(empty: true, itemVisible: false) && HudHotbar.SlotFaceShown(empty: false, itemVisible: false) && !HudHotbar.SlotFaceShown(empty: false, itemVisible: true),
    "polish hotbar: a slot shows its normal + tile unless an icon really stands on it (no invisible occupied hole)");
string polishBar = Src("Scripts/UI/HUD/HudHotbar.cs");
string polishRefresh = Body6(polishBar, "private void Refresh()", "public static bool SlotFaceShown(");
Check(polishRefresh.Contains("SlotFaceShown(empty,") && polishRefresh.Contains("tile.Button.interactable = empty;") && polishRefresh.Contains("tile.Rect.gameObject.SetActive(unlocked)") &&
      !polishRefresh.Contains("RemoveAt"), "polish hotbar: the tile stays for every unlocked slot, only a truly empty slot is clickable, the row is never compacted");
Check(Src("Scripts/UI/HUD/OfficeHudRuntime.cs").Contains("HudHotbar.Instance?.PresentationChanged();"),
    "polish hotbar: the handover gate's saved states (from when the folder still occupied the slot) are re-read as soon as it ends");
{
    InventoryCatalogue polishCatalogue = InventoryCatalogue.BuiltIn();
    var polishBag = new CampaignInventoryRecord();
    polishBag.Repair();
    polishBag.hotbar[0].itemId = InventoryCatalogue.Notes; polishBag.hotbar[0].quantity = 1;
    polishBag.hotbar[1].itemId = InventoryCatalogue.CaseFolder; polishBag.hotbar[1].quantity = 1;
    polishBag.items.Add(new InventoryItemRecord { itemId = InventoryCatalogue.CaseFolder });
    polishBag.pins.Add(InventoryCatalogue.CaseFolder);
    int polishCapacity = polishBag.hotbarCapacity, polishSlots = polishBag.hotbar.Count;
    var polishState = Level3ChapterRules.CreateEntry();
    polishState.Case(Level3ChapterRules.BeaCaseId).copyPhase = Level3ReviewRules.CopyAtAuditor;
    polishState.Case(Level3ChapterRules.BeaCaseId).review.approved = true;
    InventoryResult polishGone = Level3ReviewRules.SyncFolderInventory(polishBag, polishCatalogue, polishState);
    Check(polishGone.Ok && polishBag.hotbar[1].IsEmpty && polishBag.hotbar[0].itemId == InventoryCatalogue.Notes && polishBag.hotbarCapacity == polishCapacity && polishBag.hotbar.Count == polishSlots &&
          !InventoryRules.Owns(polishBag, InventoryCatalogue.CaseFolder) && !polishBag.pins.Contains(InventoryCatalogue.CaseFolder),
        "polish hotbar: approval frees the folder's slot in place: no ownership or pin left, capacity, positions and other items unchanged");
    string polishAfter = JsonSerializer.Serialize(polishBag.hotbar.Select(s => s.itemId + s.quantity));
    Check(Level3ReviewRules.SyncFolderInventory(polishBag, polishCatalogue, polishState).Ok && JsonSerializer.Serialize(polishBag.hotbar.Select(s => s.itemId + s.quantity)) == polishAfter,
        "polish hotbar: a repeated callback changes nothing");
    polishState.Case(Level3ChapterRules.BeaCaseId).copyPhase = Level3DeliveryRules.CopyCarried;
    Check(Level3ReviewRules.SyncFolderInventory(polishBag, polishCatalogue, polishState).Ok && polishBag.hotbar.Count(s => s.itemId == InventoryCatalogue.CaseFolder) == 1 && polishBag.hotbar[1].itemId == InventoryCatalogue.CaseFolder,
        "polish hotbar: the separately collected approved copy takes the free slot once, without a duplicate");
}
string polishSplash = Src("Scripts/SideQuest1/SideQuestSplash.cs");
Check(polishSplash.Contains("bool withCamera = false") && polishSplash.Contains("CameraShieldOrder + 1"),
    "polish splash: the shared card gains one opt-in immediate start; Levels 1-2 keep their calls and timing");
string polishSeat = Body6(Src("Scripts/Level3/Level3BeaTable.cs"), "private bool SitAtTable(", "private void HandleLeftSeat()");
Check(Index(polishSeat, "SetPositionAndRotation(fromView.Value.position") < Index(polishSeat, "EnterFirstPerson(Viewpoint"),
    "polish splash: Help Bea starts the move from the conversation's shot, so no return to first person shows between them");
string polishBea = Src("Scripts/Level3/Level3BeaInteractable.cs");
Check(polishBea.Contains("table.AcceptHelp(helpView != default ? helpView : (Pose?)null);") && Src("Scripts/Level3/Level3BeaTableSeat.cs").Contains("table.TrySit();"),
    "polish splash: Help Bea and the later table tap run one seat path");
Check(Body6(polishBea, "private void Hook()", "/// <summary>The last line waits").IndexOf("Unhook();") < Body6(polishBea, "private void Hook()", "/// <summary>The last line waits").IndexOf("+="),
    "polish: a conversation's line callbacks subscribe once");
string polishGuideText = File.ReadAllText(Res("Tutorials/Level_03/guide.json"));
GuideScript polishGuide = JsonSerializer.Deserialize<GuideScript>(polishGuideText, json);
var polishPages = polishGuide.lessons.SelectMany(l => l.pages).ToList();
Check(polishPages.Where(p => !string.IsNullOrEmpty(p.point)).All(p => GuideTargets.Validate(p.point) == null && (string.IsNullOrEmpty(p.to) || GuideTargets.Validate(p.to) == null)) &&
      GuideTargets.Validate("level3:dial") == null && GuideTargets.Validate("level3:nothing") != null,
    "polish guide: every hand target is a known target");
var polishNew = new[] { "level3:card", "level3:dial", "level3:lever", "level3:why", "level3:batch", "level3:summary", "level3:calculator" };
Check(polishNew.All(k => polishPages.Any(p => p.point == k && !p.IsHint && !string.IsNullOrEmpty(p.waitFor))),
    "polish guide: the genuinely new machine and calculator steps use the shared coach box and hand, and advance on the real action");
Check(new[] { "worksheet-subtotals", "worksheet-drafted", "computation-totals", "computation-done", "credit-done" }.All(id => !polishGuide.lessons.Any(l => l.id == id)) &&
      polishGuide.lessons.Where(l => !string.IsNullOrEmpty(l.after)).All(l => polishGuide.lessons.Any(o => o.id == l.after)),
    "polish guide: retired and doubled lessons are gone and no lesson waits on a removed one");
Check(polishPages.All(p => p.text == null || p.text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 20) &&
      !polishPages.Any(p => p.text != null && new[] { "62,500", "12,500", "Personal purpose", "Already represented" }.Any(p.text.Contains)),
    "polish guide: Arthur stays short and names no answer");
string polishGuidePart = Body6(Src("Scripts/Level3/Level3BeaTable.cs"), "public Transform GuidePart(string part)", "public bool AcceptHelp(");
Check(polishGuidePart.Contains("introPending") && polishGuidePart.Contains("demoRunning") && polishGuidePart.Contains("!FreeToTouch()") && polishGuidePart.Contains("HudRewardScreen.AnyOpen"),
    "polish guide: the hand has no target during the splash, a demonstration, a camera move or a modal");
var polishSections = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().ToList();
var polishTriggers = polishSections.SelectMany(o => o.GetProperty("steps").EnumerateArray()).Select(s => s.GetProperty("trigger").GetString()).ToList();
Check(polishSections.Select(o => o.GetProperty("title").GetString()).SequenceEqual(new[] { "Finish Side Quest 3", "Finish Bea's Tax Case", "Follow Through with Bea" }),
    "polish objectives: three Level-1-style sections");
Check(polishTriggers.SequenceEqual(new[] { "ChapterAssignmentReceived", "ClientRecordsCollected", "WorksheetDrafted", "WorksheetReady", "WorksheetCollected", "TaxDueAttached", "WithholdingCertificateReceived",
        "RemainingPayableAttached", "BeaCaseApproved", "ClientCopyPickedUp", "ClientCopyHandedOver", "FindingRecorded", "ChapterMessageRead", "ChapterWrapUpConfirmed" }.Select(e => "event:" + e)),
    "polish objectives: every trigger identity and its order are unchanged, so saved progress reads the same");
Check(Src("Scripts/Data/CaseProgressionManager.cs").Contains("PresentedCasesInLevel => CurrentLevelId == Level3ChapterRules.LevelId ? 1 : TotalCasesInLevel") &&
      Src("Scripts/UI/HUD/HudObjectiveTracker.cs").Contains("manager.PresentedCasesInLevel") && Json("Levels/Level_03/level.json").GetProperty("caseIds").GetArrayLength() == 2,
    "polish objectives: no CASE 1 / 2 chip while only Bea's case is active; the deferred company case stays in the level");
{
    var polishResult = Level3ChapterRules.CreateEntry();
    LevelCompleteUI.LevelThreeAttemptRewards(polishResult, out int polishCoins0, out int polishTrust0);
    Check(polishCoins0 == 0 && polishTrust0 == 0, "polish result: an attempt that earned nothing shows nothing");
    polishResult.Case(Level3ChapterRules.BeaCaseId).review.approved = true;
    polishResult.phase = Level3Phase.ClosingFinding.ToString();
    polishResult.actionIds.Add(Level3DeliveryRules.DeliverAction);
    polishResult.Case(Level3ChapterRules.BeaCaseId).copyPhase = Level3DeliveryRules.CopyDone;
    polishResult.Case(Level3ChapterRules.BeaCaseId).balanceWrongAnswers = 3;
    LevelCompleteUI.LevelThreeAttemptRewards(polishResult, out int polishCoins1, out int polishTrust1);
    Check(polishCoins1 == 100 && polishTrust1 == Level3DeliveryRules.FollowThroughTrust(3), "polish result: the attempt's coins and Trust come from the same reward rules");
}
string polishCard = Body6(Src("Scripts/UI/LevelCompleteUI.cs"), "private RewardScreenData BuildChapterThreeScreen(", "/// <summary>");
Check(polishCard.Contains("\"Public Trust\"") && polishCard.Contains("result.Exp, Level3ChapterResult.MaxExp") && polishCard.Contains("data.Fact(\"Time\"") && polishCard.Contains("ChapterTemplate(\"Chapter 3 Complete\"") &&
      polishCard.Contains("mode == CampaignRunMode.DirectEditor") && !polishCard.Contains("Grant("),
    "polish result: the Chapter 3 card uses Level 1's template with EXP, coins, Public Trust and time; presentation only");
var polishBeaSpot = new System.Numerics.Vector3(8.73f, 0f, 168.48f);
float polishFar(UnityEngine.Vector3 v) => System.Numerics.Vector3.Distance(polishBeaSpot, new System.Numerics.Vector3(v.x, 0f, v.z));
Check(polishFar(Level3PlazaLife.PatrolA) > 30f && polishFar(Level3PlazaLife.PatrolB) > 30f && polishFar(Level3PlazaLife.BenchSeat) > 30f,
    "polish plaza: both NPCs keep Level 1's spots, far from Bea's table and its camera");
string polishLife = Src("Scripts/Level3/Level3PlazaLife.cs");
string polishAssets = File.ReadAllText(Res("Levels/Level_03/scene_assets.asset"));
Check(polishAssets.Contains("guid: 1d5338d27c0839b46892a6390262c9d4") && polishAssets.Contains("guid: 08a2f1fda95d8de42ba936230a0610fe") && polishLife.Contains("transform.Find(\"Ambient \" + id) != null") &&
      polishLife.Contains("AmbientNpcInteractable") && polishLife.Contains("NpcLife") && !polishLife.Contains("Grant(") && !polishLife.Contains("Commit") && !polishLife.Contains("RaiseGuideCue") &&
      Src("Scripts/Level3/Level3OfficeStart.cs").Contains("EnsurePart<Level3PlazaLife>"),
    "polish plaza: exactly Level 1's guard and Carlo models on the shared components, once per id, with no quest, save or reward");
Check(Src("Scripts/AudioManager/AudioManager.cs").Contains("public void PlayKeyboardSFX() => PlayMouseClickSelectSFX();") &&
      !Directory.GetFiles(Path.Combine(root, "Assets/Scripts"), "*.cs", SearchOption.AllDirectories).Any(f => File.ReadAllText(f).Contains("sfx_keyboard_type")),
    "polish audio: every key press is one short click; nothing can start the long typing recordings");
JsonElement careerChapters = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Assets/Resources/Career/chapters.json"))).RootElement;
Check(careerChapters.GetProperty("sideQuests").EnumerateArray().Any(q => q.GetProperty("chapterId").GetString() == "Level_03" &&
        q.GetProperty("number").GetInt32() == 3 && q.GetProperty("title").GetString() == "Worth the Tax?"),
    "the Career board lists Worth the Tax? as side quest 3, so its earned medal shows");
Console.WriteLine($"Polish pass: {checks - polishStart} checks, {fails - polishFails} failures (rules, source wiring; Unity rendering, timing, audio and touch not run).");
// =====================================================================
// Milestone T01 (Level1To3AdventureMilestone, 11 November 2026): finish required paperwork with a full base Bag.
// Bea's certificate clipped straight into her carried folder (L3-R106), the no-room unclip guard, failure/retry,
// duplicates, ineligible folder, reload, isolation and an extra retained letter slot. Unity rendering/touch not run.
// =====================================================================
int m01Start = checks, m01Fails = fails;
const string WhtSource01 = Level3ChapterRules.BeaCreditSource;
string Summary01 = Level3WorksheetCheck.WorksheetWorkId;
// Everything except the chapter snapshot and the carried inventory (wallet, Trust, claims, mastery, history, upgrades, decor, settings).
string Rest01(PlayerProgressData d) { var c = Clone(d); c.campaign.inventory = null; c.campaign.levelThree = null; return Snap(c); }
int Certificates01(CampaignInventoryRecord r, Level3ChapterCheckpoint s) =>
    InventoryRules.CarriedCount(r, InventoryCatalogue.BeaCertificate) + (s?.Case(B7r)?.folder?.certificateAttached == true ? 1 : 0);
// The L3-R106 loadout: every Chapter 1/2 possession kept at base 3 + 6, the desk book put back for the summary, the summary in the Bag.
CampaignInventoryRecord FullAtCertificate01(CampaignInventoryRecord from = null)
{
    var r = from ?? Rich13(false);
    if (InventoryRules.Owns(r, InventoryCatalogue.TaxCodeBook)) InventoryRules.Remove(r, cat13, InventoryCatalogue.TaxCodeBook, "m01/book-back");
    Level3WorksheetCheck.AcquireSummary(r, cat13, got6m, null, bea);
    return r;
}

// ---------- reproduce L3-R106 through the unchanged ordinary pickup ----------
var full01 = FullAtCertificate01();
string fullSnap01 = Snap(full01), kept01 = Kept13(full01, InventoryCatalogue.TaxCodeBook);
Level3ChapterCheckpoint ordinary01 = due8r.Copy();
Check(Full13(full01) && InventoryRules.Owns(full01, InventoryCatalogue.BeaSummary) && Level3CertificateRules.HandOverDue(due8r) &&
      Level3CertificateRules.HandOver(ordinary01, out _) == Level3CommandOutcome.Applied && !Level3CertificateRules.HasRoom(full01) &&
      Level3CertificateRules.ApplyToInventory(Clone(full01), cat13, ordinary01, bea, requireFolder: true).Message == Level3CertificateRules.RefuseFull &&
      Snap(full01) == fullSnap01,
    "M-T01 (L3-R106 reproduced): every kept Chapter 1/2 item + the summary fills base 3 + 6 at the credit step; the ordinary certificate pickup is refused before any write");

// ---------- the deliberate clip: one candidate, existing attachment, no slot, no credit ----------
Level3ChapterCheckpoint clip01 = due8r.Copy();
int revision01 = clip01.Case(B7r).computation.revision;
Check(Level3CertificateRules.ClipToFolder(clip01, folderCarried: true, out string clipWhy01) == Level3CommandOutcome.Applied && clipWhy01 == null &&
      Level3CertificateRules.Acquired(clip01) && Level3CertificateRules.Clipped(clip01) && clip01.Case(B7r).folder.certificateAttached &&
      clip01.actionIds.Contains(Level3CertificateRules.HandOverAction) && clip01.actionIds.Contains(Level3CertificateRules.ClipAction) &&
      clip01.milestones.Contains(Level3CertificateRules.ReceivedMilestone) && clip01.Case(B7r).computation.revision == revision01 + 1 &&
      clip01.Case(B7r).acquiredSourceIds.Count(id => id == WhtSource01) == 1,
    "M-T01: Clip to Bea's folder hands B-WHT over and attaches it in the same candidate (one source, the existing attachment flag and revision)");
var clipInv01 = Clone(full01);
InventoryResult clipped01 = Level3CertificateRules.ApplyToInventory(clipInv01, cat13, clip01, bea, requireFolder: true);
var folderPages01 = InventoryRules.Record(clipInv01, InventoryCatalogue.CaseFolder).referencePages;
// The same readable pages an ordinary pickup gives its own Bag item.
var ordinaryInv01 = Rich13(true);
Level3CertificateRules.AcquireItem(ordinaryInv01, cat13, clip01, bea);
var ordinaryPages01 = InventoryRules.Record(ordinaryInv01, InventoryCatalogue.BeaCertificate).referencePages;
Check(clipped01.Ok && !InventoryRules.Owns(clipInv01, InventoryCatalogue.BeaCertificate) && Full13(clipInv01) &&
      Kept13(clipInv01, InventoryCatalogue.TaxCodeBook, InventoryCatalogue.CaseFolder) == Kept13(full01, InventoryCatalogue.TaxCodeBook, InventoryCatalogue.CaseFolder) &&
      InventoryRules.Find(clipInv01, InventoryCatalogue.CaseFolder) == InventoryRules.Find(full01, InventoryCatalogue.CaseFolder) &&
      InventoryRules.Record(clipInv01, InventoryCatalogue.CaseFolder).contents.SequenceEqual(InventoryRules.Record(full01, InventoryCatalogue.CaseFolder).contents) &&
      folderPages01.Any(p => p.id == "L2-ROSA-001/D1/0") && ordinaryPages01.Count > 0 &&
      ordinaryPages01.All(o => folderPages01.Any(p => p.id == o.id && p.title == o.title && p.lines.SequenceEqual(o.lines))) &&
      Snap(InventoryRules.Record(clipInv01, InventoryCatalogue.BeaSummary)) == Snap(InventoryRules.Record(full01, InventoryCatalogue.BeaSummary)) &&
      clipInv01.pins.SequenceEqual(full01.pins) && clipInv01.hotbarCapacity == full01.hotbarCapacity && clipInv01.bagCapacity == full01.bagCapacity && clipInv01.hand.IsEmpty,
    "M-T01: the clipped certificate takes no slot; every possession keeps slot, count, contents, readable pages and pins; its own readable pages stay with the folder; capacity unchanged, nothing in hand");
Check(Certificates01(clipInv01, clip01) == 1 && Level3CaseFolder.SupportingRecords(bea, clip01).Count(r => r.source == WhtSource01) == 1,
    "M-T01: exactly one certificate exists, shown as the actual B-WHT paper in the folder's Supporting Documents slot");
var clipRecord01 = Level3ComputationRules.Record(clip01, B7r);
Check(clipRecord01.creditChoice == "" && string.IsNullOrEmpty(clipRecord01.creditAmount) && !clipRecord01.payableNoted && !clipRecord01.creditExplained &&
      !Level3ComputationRules.Evaluate(clip01, bea, B7r).PayableSupported,
    "M-T01: clipping enters no credit, explanation or balance (no premature financial value)");
Check(Pick7r(clip01, Wht8r) != null && Place7r(clip01.Copy(), "credits", Pick7r(clip01, Wht8r), out _) == Level3CommandOutcome.Applied &&
      StageC8r(from: clip01) != null && Level3ComputationRules.Evaluate(StageC8r(from: clip01), bea, B7r).PayableSupported,
    "M-T01: the clipped original is a held source: the player's own transfer places its exact credit and stage C reaches the supported balance");
Check(Level3ReviewRules.FolderProblem(clip01.Case(B7r).folder, bea) != Level3ReviewRules.SupportingField || !clip01.Case(B7r).folder.summaryAttached,
    "M-T01: the review sees the certificate attached (only the summary's own attachment can still be missing)");

// ---------- roomy Bag: the ordinary pickup is unchanged ----------
foreach (var (label01, roomy) in new[] { ("base, no keepsakes", Rich13(false, withBook: true, keepsakes: false)), ("upgraded 5 + 13", Rich13(true)) })
{
    Level3WorksheetCheck.AcquireSummary(roomy, cat13, got6m, null, bea);
    Level3ChapterCheckpoint roomyState01 = due8r.Copy();
    Level3CertificateRules.HandOver(roomyState01, out _);
    string roomyKept01 = Kept13(roomy);
    Check(Level3CertificateRules.HasRoom(roomy) && Level3CertificateRules.ApplyToInventory(roomy, cat13, roomyState01, bea, requireFolder: true).Ok &&
          !InventoryRules.Find(roomy, InventoryCatalogue.BeaCertificate).IsNone && !Level3CertificateRules.Clipped(roomyState01) &&
          Kept13(roomy) == roomyKept01 && Certificates01(roomy, roomyState01) == 1,
        $"M-T01 {label01}: with room the certificate still lands in its own slot (no clip, nothing else moves)");
}

// ---------- ineligible folder, stale and duplicate commands ----------
string due01 = Snap(due8r);
Level3ChapterCheckpoint away01 = due8r.Copy();
Check(Level3CertificateRules.ClipToFolder(away01, folderCarried: false, out string awayWhy01) == Level3CommandOutcome.Refused &&
      awayWhy01 == Level3CertificateRules.RefuseFolderAway && Snap(away01) == due01 && Level3CertificateRules.HandOverDue(away01),
    "M-T01: without the carried folder the clip is refused with one retry line; nothing changes and the paper stays with Bea");
Level3ChapterCheckpoint onDesk01 = due8r.Copy();
(onDesk01.Case(B7r).folder ??= new Level3FolderPacket()).onDesk = true;
string onDeskSnap01 = Snap(onDesk01);
Check(Level3CertificateRules.ClipToFolder(onDesk01, folderCarried: true, out string deskWhy01) == Level3CommandOutcome.Refused &&
      deskWhy01 == Level3CertificateRules.RefuseFolderAway && Snap(onDesk01) == onDeskSnap01,
    "M-T01: a folder left on the desk is not a destination (refused, unchanged)");
Level3ChapterCheckpoint early01 = o7r.Copy();
string earlySnap01 = Snap(early01);
Check(Level3CertificateRules.ClipToFolder(early01, true, out string earlyWhy01) == Level3CommandOutcome.Refused && earlyWhy01 == Level3CertificateRules.RefuseNotYet &&
      Snap(early01) == earlySnap01 &&
      Level3CertificateRules.ClipToFolder(ok9r.Copy(), true, out _) == Level3CommandOutcome.Duplicate,
    "M-T01: a stale clip before tax due is refused unchanged; after the handover (any later state) it is a quiet duplicate");
Level3ChapterCheckpoint again01 = clip01.Copy();
string againSnap01 = Snap(again01);
var againInv01 = Clone(clipInv01);
string againInvSnap01 = Snap(againInv01);
Check(Level3CertificateRules.ClipToFolder(again01, true, out _) == Level3CommandOutcome.Duplicate && Level3CertificateRules.HandOver(again01, out _) == Level3CommandOutcome.Duplicate &&
      Snap(again01) == againSnap01 && Level3CertificateRules.ApplyToInventory(againInv01, cat13, again01, bea, requireFolder: true).Outcome == InventoryOutcome.NoChange &&
      Snap(againInv01) == againInvSnap01 && Certificates01(againInv01, again01) == 1,
    "M-T01: a duplicate clip, a late ordinary pickup callback or a repeated inventory apply changes nothing (no second paper, page or action)");
Check(Level3CertificateRules.ApplyToInventory(Clone(full01), cat13, clip01, bea, requireFolder: true).Ok &&
      !Level3CertificateRules.ApplyToInventory(new CampaignInventoryRecord(), cat13, clip01, bea, requireFolder: true).Ok &&
      Level3CertificateRules.ApplyToInventory(new CampaignInventoryRecord(), cat13, clip01, bea, requireFolder: false).Ok,
    "M-T01: story refuses a clip whose folder the save does not own; isolated runs keep their local folder");

// ---------- unclip with no room is refused; with room it unclips and is reacquired once ----------
Level3ChapterCheckpoint u01 = clip01.Copy();
var uInv01 = Clone(clipInv01);
string uSnap01 = Snap(u01);
Check(Level3ReviewRules.SetAttachment(u01, WhtSource01, false, out string uWhy01, uInv01) == Level3CommandOutcome.Refused && uWhy01 == Level3ReviewRules.RefuseUnclipFull &&
      Snap(u01) == uSnap01 && Level3CertificateRules.Clipped(u01) && Level3CaseFolder.SupportingRecords(bea, u01).Any(r => r.source == WhtSource01),
    "M-T01: unclipping with a full bar and Bag is refused; the original stays attached and readable");
Check(Level3ReviewRules.SetAttachment(u01, Summary01, true, out _, uInv01) == Level3CommandOutcome.Applied &&
      Level3ReviewRules.SyncFolderInventory(uInv01, cat13, u01).Ok && !InventoryRules.Owns(uInv01, InventoryCatalogue.BeaSummary) && !Full13(uInv01) &&
      Level3ReviewRules.SetAttachment(u01, WhtSource01, false, out _, uInv01) == Level3CommandOutcome.Applied &&
      Level3CertificateRules.AcquireItem(uInv01, cat13, u01, bea, HudHotbar.NewAction("certificate-recover")).Ok &&
      InventoryRules.Owns(uInv01, InventoryCatalogue.BeaCertificate) && Full13(uInv01) && Certificates01(uInv01, u01) == 1,
    "M-T01: clipping the summary frees its slot; the certificate then unclips and is reacquired into that one slot (still one certificate)");
Check(Level3ReviewRules.SetAttachment(u01.Copy(), Summary01, false, out string sumWhy01, uInv01) == Level3CommandOutcome.Refused && sumWhy01 == Level3ReviewRules.RefuseUnclipFull &&
      Level3ReviewRules.SetAttachment(u01, WhtSource01, true, out _, uInv01) == Level3CommandOutcome.Applied &&
      Level3ReviewRules.SyncFolderInventory(uInv01, cat13, u01).Ok && !InventoryRules.Owns(uInv01, InventoryCatalogue.BeaCertificate) &&
      Level3ReviewRules.SetAttachment(u01, Summary01, false, out _, uInv01) == Level3CommandOutcome.Applied &&
      Level3WorksheetCheck.AcquireSummary(uInv01, cat13, u01, null, bea, HudHotbar.NewAction("summary-recover")).Ok && Full13(uInv01) && Certificates01(uInv01, u01) == 1 &&
      Kept13(uInv01, InventoryCatalogue.TaxCodeBook, InventoryCatalogue.CaseFolder) == Kept13(full01, InventoryCatalogue.TaxCodeBook, InventoryCatalogue.CaseFolder),
    "M-T01: the summary cannot be unclipped into nowhere either; repeated clip/unclip/reacquire keeps one of each paper and every possession");
Check(Level3ReviewRules.SetAttachment(clip01.Copy(), WhtSource01, false, out _) == Level3CommandOutcome.Applied,
    "M-T01: without an inventory (pure review/Editor callers) the existing unclip command is unchanged");

// ---------- a returned review on the full loadout: the folder on the desk leaves room for both papers ----------
Level3ChapterCheckpoint ret01 = clip01.Copy();
var retInv01 = Clone(clipInv01);
Level3ReviewRules.ResetReturnedFolder(ret01, new Level3ReviewDecision { Outcome = Level3ReviewOutcome.PrematureApproval, Field = Level3ReviewRules.PersonalField });
Check(!ret01.Case(B7r).folder.certificateAttached && Level3ReviewRules.SyncFolderInventory(retInv01, cat13, ret01).Ok &&
      !InventoryRules.Owns(retInv01, InventoryCatalogue.CaseFolder) && Level3CertificateRules.AcquireItem(retInv01, cat13, ret01, bea, "m01/return-recover").Ok &&
      Certificates01(retInv01, ret01) == 1 && Level3CertificateRules.Acquired(ret01) && InventoryRules.Owns(retInv01, InventoryCatalogue.BeaSummary),
    "M-T01: a returned review resets the packet; the folder goes to the desk, so the certificate reacquires into a slot (kept once, still acquired)");

// ---------- signed store: failed write, retry, reload, unchanged wallet/Trust/history ----------
string sandbox01 = Path.Combine(Path.GetTempPath(), "TaxVentureM01-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox01);
try
{
    string Encode01(PlayerProgressData d) { string payload = JsonSerializer.Serialize(d, json); return JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payload, ["signature"] = SaveFileStore.Sign(payload) }); }
    var store01 = new SaveFileStore(Path.Combine(sandbox01, "progress.json"));
    PlayerProgressData pub01 = Clone(live);
    pub01.campaign.inventory = FullAtCertificate01();
    pub01 = Level3ChapterRules.WithCheckpoint(pub01, due8r.Copy());
    pub01.Normalize();
    store01.Write(Encode01(pub01));
    byte[] disk01 = File.ReadAllBytes(store01.FilePath);
    string pubSnap01 = Snap(pub01), rest01 = Rest01(pub01);
    // Mirrors CommitCertificateToFolder -> PlayerProgressManager.CommitLevelThreeCertificate -> TryWriteCandidate.
    PlayerProgressData Candidate01(PlayerProgressData from, out bool ok)
    {
        Level3ChapterCheckpoint chapter = from.campaign.levelThree.Copy();
        ok = Level3CertificateRules.ClipToFolder(chapter, true, out _) == Level3CommandOutcome.Applied;
        PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(from), chapter);
        ok &= next != null && Level3CertificateRules.ApplyToInventory(next.campaign.inventory, cat13, chapter, bea, requireFolder: true).Ok;
        return next;
    }
    PlayerProgressData blocked01 = Candidate01(pub01, out bool built01);
    Directory.CreateDirectory(store01.TempPath);
    bool wrote01 = false;
    try { blocked01.Normalize(); store01.Write(Encode01(blocked01)); pub01 = blocked01; wrote01 = true; }
    catch (Exception) { }
    Check(built01 && !wrote01 && Snap(pub01) == pubSnap01 && File.ReadAllBytes(store01.FilePath).SequenceEqual(disk01) &&
          !Level3CertificateRules.Acquired(pub01.campaign.levelThree) && Level3CertificateRules.HandOverDue(pub01.campaign.levelThree) &&
          Snap(pub01.campaign.inventory) == Snap(FullAtCertificate01()),
        "M-T01: a failed write of the clip leaves disk and live state as they were: no source, no attachment, inventory unchanged; the paper is still Bea's to clip");
    Directory.Delete(store01.TempPath);
    PlayerProgressData retry01 = Candidate01(pub01, out bool rebuilt01);
    retry01.Normalize();
    store01.Write(Encode01(retry01));
    pub01 = retry01;
    PlayerProgressData back01 = JsonSerializer.Deserialize<PlayerProgressData>(JsonDocument.Parse(File.ReadAllText(store01.FilePath)).RootElement.GetProperty("payload").GetString(), json);
    back01.Normalize();
    Level3ChapterCheckpoint backChapter01 = back01.campaign.levelThree;
    Check(rebuilt01 && Level3CertificateRules.Clipped(backChapter01) && Certificates01(back01.campaign.inventory, backChapter01) == 1 &&
          Level3ChapterRules.Decide(backChapter01) == Level3EntryDecision.Resume &&
          Kept13(back01.campaign.inventory, InventoryCatalogue.TaxCodeBook, InventoryCatalogue.CaseFolder) == Kept13(FullAtCertificate01(), InventoryCatalogue.TaxCodeBook, InventoryCatalogue.CaseFolder) &&
          InventoryRules.Record(back01.campaign.inventory, InventoryCatalogue.CaseFolder).referencePages.Any(p => p.id.StartsWith(B7r + "/B-WHT/")) &&
          Rest01(back01) == rest01 && Level3ImpliedHas01(backChapter01),
        "M-T01: retry saves; reload restores one clipped original, every possession, and an unchanged wallet/Trust/claims/history (the certificate objective stays ticked)");
    bool recovers01 = Level3CertificateRules.Acquired(backChapter01) && !backChapter01.Case(B7r).folder.certificateAttached && !InventoryRules.Owns(back01.campaign.inventory, InventoryCatalogue.BeaCertificate);
    PlayerProgressData twice01 = Candidate01(back01, out bool second01);
    Check(!recovers01 && !second01 && Level3CertificateRules.ClipToFolder(backChapter01.Copy(), true, out _) == Level3CommandOutcome.Duplicate,
        "M-T01: after reload neither the quiet Bag recovery nor a late clip callback can acquire a second certificate");
}
finally
{
    try { Directory.Delete(sandbox01, true); } catch (IOException) { }
}
bool Level3ImpliedHas01(Level3ChapterCheckpoint s) => Level3ChapterRules.ImpliedTriggers(s).Contains(Level3ChapterRules.CertificateTrigger);

// ---------- an extra retained letter slot (Kanor's future letter, disposable catalogue entry) ----------
var catLetter01 = Clone(cat13);
const string Letter01 = "fixture_kanor_letter";
catLetter01.items.Add(new InventoryItemDefinition { id = Letter01, label = "Kanor's letter (fixture)", category = InventoryCatalogue.CaseFiles, icon = "papers", reference = true });
var letterInv01 = Rich13(false);
Check(InventoryRules.Display(letterInv01, catLetter01, InventoryCatalogue.Postcard, "fixture-stand", "m01/display").Changed &&
      InventoryRules.Owns(letterInv01, InventoryCatalogue.Postcard) &&
      InventoryRules.Acquire(letterInv01, catLetter01, Letter01, 1, "m01/letter", null, new InventoryItemRecord { itemId = Letter01, source = "Kanor" }).Changed,
    "M-T01 letter fixture: the postcard stays owned on its display stand; the letter occupies a normal retained slot");
SlotRef letterAt01 = InventoryRules.Find(letterInv01, Letter01);
FullAtCertificate01(letterInv01);
string letterKept01 = Snap(InventoryRules.Record(letterInv01, Letter01)) + letterAt01;
Level3ChapterCheckpoint letterState01 = due8r.Copy();
Check(Full13(letterInv01) && !Level3CertificateRules.HasRoom(letterInv01) &&
      Level3CertificateRules.ClipToFolder(letterState01, true, out _) == Level3CommandOutcome.Applied &&
      Level3CertificateRules.ApplyToInventory(letterInv01, catLetter01, letterState01, bea, requireFolder: true).Ok &&
      Snap(InventoryRules.Record(letterInv01, Letter01)) + InventoryRules.Find(letterInv01, Letter01) == letterKept01 &&
      InventoryRules.IsDisplayed(letterInv01, InventoryCatalogue.Postcard) && Certificates01(letterInv01, letterState01) == 1 &&
      Level3ComputationRules.Evaluate(StageC8r(from: letterState01), bea, B7r).PayableSupported,
    "M-T01 letter fixture: with the letter in a slot and the Bag full, the certificate still clips, reads as a source and completes stage C; the letter and display are untouched");
var tight01 = Rich13(false);
InventoryRules.Remove(tight01, cat13, InventoryCatalogue.TaxCodeBook, "m01/book-in-l2");
InventoryRules.Acquire(tight01, catLetter01, Letter01, 1, "m01/letter-tight", null, new InventoryItemRecord { itemId = Letter01, source = "Kanor" });
Console.WriteLine($"M-T01 note: letter + carried postcard + book already returned in Chapter 2 fills base {Full13(tight01)}; the summary pickup there is " +
                  $"{(Level3WorksheetCheck.AcquireSummary(Clone(tight01), cat13, got6m, null, bea).Ok ? "accepted" : "refused (no slot; T08's letter pickup must not create this loadout, KNOWN_ISSUES M01-SUMMARY)")}.");

// ---------- wiring (source; Unity behaviour is the user's) ----------
string cp01 = Src("Scripts/Level3/Level3Checkpointer.cs"), paper01 = Src("Scripts/Level3/Level3CertificatePaper.cs"), mgr01 = Src("Scripts/RewardsSystem/PlayerProgressManager.cs");
string clipBody01 = Body6(cp01, "public TapeCommit CommitCertificateToFolder(", "/// <summary>");
Check(Index(clipBody01, "State.Copy()") < Index(clipBody01, "Level3CertificateRules.ClipToFolder(candidate") &&
      Index(clipBody01, "Level3CertificateRules.ClipToFolder(candidate") < Index(clipBody01, "if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCertificate(candidate))") &&
      Index(clipBody01, "CommitLevelThreeCertificate(candidate)") < Index(clipBody01, "return TapeCommit.SaveFailed;") &&
      Index(clipBody01, "return TapeCommit.SaveFailed;") < Index(clipBody01, "Publish(candidate);") &&
      clipBody01.Contains("if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;") &&
      clipBody01.Contains("requireFolder: false") && !clipBody01.Contains("RewardsSystem") && !clipBody01.Contains("Grant"),
    "M-T01 wiring: CommitCertificateToFolder is candidate -> (story) signed write -> publish; duplicates are quiet; isolated runs touch only their local inventory; no grant");
string mgrBody01 = Body6(mgr01, "public bool CommitLevelThreeCertificate(", "/// <summary>");
Check(mgrBody01.Contains("if (!UsesLevelThreeStory) return false;") && Index(mgrBody01, "Level3CertificateRules.ApplyToInventory(candidate.campaign.inventory") < Index(mgrBody01, "TryWriteCandidate(candidate)") &&
      mgrBody01.Contains("requireFolder: true") && !mgrBody01.Contains("coins") && !mgrBody01.Contains("Trust"),
    "M-T01 wiring: only a story run writes; chapter and inventory are one candidate written before anything is live; no wallet/Trust change");
string collect01 = Body6(paper01, "private RectTransform CollectPickup()", "// =========================================================");
Check(Index(collect01, "if (clipChosen)") < Index(collect01, "CommitCertificateToFolder(carried, out string why)") &&
      Index(collect01, "CommitCertificateToFolder(carried, out string why)") < Index(collect01, "return bar.ItemTarget(InventoryCatalogue.CaseFolder);") &&
      collect01.Contains("if (!Level3CertificateRules.Acquired(Chapter) && !Level3CertificateRules.HasRoom(bar.Inventory))\n        {\n            ShowClipTag();\n            Level3BeaTable.Level3Say(Level3CertificateRules.OfferClip);\n            return null;\n        }") &&
      Index(collect01, "ShowClipTag();") < Index(collect01, "Level3Checkpointer.Active.CommitCertificate(out string hint)"),
    "M-T01 wiring: the paper only flies after the save; with no room it offers the tag at the paper and writes nothing; with room the ordinary pickup runs");
string tag01 = Body6(paper01, "public sealed class Level3CertificateClipTag", "\n}\n");
Check(paper01.Contains("pickup.TryPickUpFromAuditor();") && paper01.Contains("if (pickup == null || Level3CertificateRules.Acquired(Chapter)) { RemoveClipTag(); return; }") &&
      paper01.Contains("if (clipTag != null && (pickup == null || Level3CertificateRules.Acquired(Chapter) || Level3CertificateRules.HasRoom(HudHotbar.Instance?.Inventory))) RemoveClipTag();") &&
      tag01.Contains("MobileInputGuard.IsDragOrCancelled(eventData)") && tag01.Contains("DialogueUI.IsAnyDialogueActive || InGameSettings.IsOpen") &&
      tag01.Contains("InteractionRange.PlayerCanReach(this)") && tag01.Contains("text.raycastTarget = false;") && !tag01.Contains("Commit"),
    "M-T01 wiring: the tag is presentation only (reach, drag, joystick, UI, dialogue and settings guards); stale taps and a Bag with room remove it");
string detach01 = Body6(paper01, "private bool DetachFolder()", "public void ReleaseFolderSlot()");
string lower01 = Body6(paper01, "public void Lower()", "/// <summary>");
string returns01 = Body6(paper01, "public bool WantsReturn(ClientRecordPaper record", "/// <summary>");
Check(Index(detach01, "if (ReadsClipped)") < Index(detach01, "Level3CaseFolder.SetAttached(Level3ChapterRules.BeaCreditSource, false)") &&
      lower01.Contains("if (folderSlot != null) { paper.CancelDrag(); paper.PutBack(); }") &&
      returns01.Contains("HudHotbar.Instance?.Notice(Level3ReviewRules.RefuseUnclipFull);") &&
      Src("Scripts/Level3/Level3CaseFolder.cs").Contains("Level3ReviewRules.SetAttachment(c, source, attached, out why, carried)"),
    "M-T01 wiring: with no slot the clipped original is read in place and settles back into the folder; a Bag drop says why; the folder's unclip passes the carried inventory");
Console.WriteLine($"Milestone T01: {checks - m01Start} checks, {fails - m01Fails} failures (full-loadout reproduction, clip/attach/read/use, roomy pickup, ineligible/stale/duplicate, " +
                  "no-room unclip, returned review, signed failure/retry/reload, letter slot, wiring; Unity rendering, touch, motion and audio not run).");
Console.WriteLine($"Level3Regression: {checks - fails}/{checks} checks passed");
return fails == 0 ? 0 : 1;

// Exercises the production placement command through the actual shared manager.
sealed class CalculatorRepairEndpoint : IDataValueSource, IDataValueDestination
{
    private readonly DataValue value;
    private readonly Level3Calculator calculator;
    public Level3CalculatorTransferValue Current;
    public int Received;
    public CalculatorRepairEndpoint(DataValue value, Level3CalculatorTransferValue current, Level3Calculator calculator)
    { this.value = value; Current = current; this.calculator = calculator; }
    public DataValue GetDataValue() => value;
    public bool CanAccept(DataValue incoming) => incoming?.Value is Level3CalculatorTransferValue payload && payload.Matches(Current);
    public bool TryReceiveValue(DataValue incoming)
    {
        if (!Level3CalculatorTransferValue.TryInsert(incoming, Current, calculator)) return false;
        Received++; return true;
    }
}

sealed class CalculatorTapeRepairEndpoint : IDataValueSource, IDataValueDestination
{
    private readonly Level3CalculatorTransferValue print;
    private readonly Func<bool> commit;
    public CalculatorTapeRepairEndpoint(Level3CalculatorTransferValue print, Func<bool> commit) { this.print = print; this.commit = commit; }
    public DataValue GetDataValue() => new DataValue(print, "770.00", DataValueType.Number, "Level3:Calculator/" + print.SourceId);
    public bool CanAccept(DataValue value) => print.Matches(value);
    public bool TryReceiveValue(DataValue value) => CanAccept(value) && commit();
}


sealed class SummaryCalculatorEndpoint : IDataValueSource, IDataValueDestination
{
    private readonly DataValue value;
    private readonly Level3ChapterCheckpoint state;
    private readonly CaseDefinition definition;
    private readonly Level3Calculator calculator;
    public int Received;
    public SummaryCalculatorEndpoint(DataValue value, Level3ChapterCheckpoint state, CaseDefinition definition, Level3Calculator calculator)
    { this.value = value; this.state = state; this.definition = definition; this.calculator = calculator; }
    public DataValue GetDataValue() => value;
    public bool CanAccept(DataValue incoming) => Level3ValueTransfer.CanInsertSummary(incoming, state, definition);
    public bool TryReceiveValue(DataValue incoming)
    {
        if (!Level3ValueTransfer.InsertSummary(incoming, state, definition, calculator)) return false;
        Received++; return true;
    }
}
