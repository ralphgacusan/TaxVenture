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
Check(Level3ChapterRules.IsAllowed(Level3Phase.Reception, Level3Phase.ToolPractice), "Reception -> ToolPractice is the first step");
Check(!Level3ChapterRules.IsAllowed(Level3Phase.Reception, Level3Phase.BeaRecords) && !Level3ChapterRules.IsAllowed(Level3Phase.ToolPractice, Level3Phase.WorksheetDraft),
    "practice cannot be skipped");
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
Check(assigned.phase == "ToolPractice" && assigned.activeCaseId == "L3-BEA-001" && assigned.Case("L3-BEA-001").admitted &&
      !assigned.Case("L3-MARINE-001").admitted && assigned.milestones.Contains("reception-assigned") &&
      assigned.actionIds.SequenceEqual(new[] { "Level_03/reception-assigned" }),
    "the assignment admits Bea, records the action and makes the calculator the next task");
string assignedSnap = Snap(assigned);
Check(Level3ChapterRules.AssignBea(assigned, out _) == Level3CommandOutcome.Duplicate && Snap(assigned) == assignedSnap,
    "a repeated assignment (second talk, restore) is a quiet duplicate");
Level3ChapterCheckpoint skip = entry.Copy();
Check(Level3ChapterRules.TryAdvance(skip, Level3Phase.Reception, Level3Phase.BeaRecords, "skip", out _) == Level3CommandOutcome.Refused && Snap(skip) == entryBefore,
    "an unlisted transition is refused with no change");
Level3ChapterCheckpoint wrongFrom = assigned.Copy();
Check(Level3ChapterRules.TryAdvance(wrongFrom, Level3Phase.Reception, Level3Phase.ToolPractice, "other-id", out _) == Level3CommandOutcome.Refused,
    "a command from the wrong phase is refused");
Check(Level3ChapterRules.TryAdvance(null, Level3Phase.Reception, Level3Phase.ToolPractice, "x", out _) == Level3CommandOutcome.Refused &&
      Level3ChapterRules.TryAdvance(entry.Copy(), Level3Phase.Reception, Level3Phase.ToolPractice, " ", out _) == Level3CommandOutcome.Refused,
    "no record or no action id: refused");

// ---------- resume decisions ----------
Check(Level3ChapterRules.Decide(assigned) == Level3EntryDecision.Resume && Level3ChapterRules.PhaseOf(assigned) == Level3Phase.ToolPractice,
    "a saved ToolPractice chapter resumes at ToolPractice (no reception replay)");
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
    ("Bea past reception but never assigned", Mutate(c => c.Case("L3-BEA-001").admitted = false))
};
foreach (var (what, state) in blocked)
    Check(Level3ChapterRules.Decide(state) == Level3EntryDecision.Blocked && Level3ChapterRules.Problem(state) != null,
        $"{what}: kept untouched (Blocked), never resumed or overwritten");

// ---------- quiet restore observers ----------
Check(Level3ChapterRules.ImpliedTriggers(Level3Phase.Reception).Count == 0 && Level3ChapterRules.ImpliedLessons(Level3Phase.Reception).Count == 0,
    "at reception nothing is implied (the welcome and check-in still happen)");
Check(Level3ChapterRules.ImpliedTriggers(Level3Phase.ToolPractice).SequenceEqual(new[] { "event:ReceptionistTalkedTo", "event:ChapterAssignmentReceived" }) &&
      Level3ChapterRules.ImpliedLessons(Level3Phase.ToolPractice).SequenceEqual(new[] { "welcome", "calculator-intro" }),
    "a resumed ToolPractice marks check-in done and never replays the welcome or the calculator introduction");
Check(Level3ChapterRules.ImpliedTriggers(Level3Phase.BeaRecords).Contains("event:CalculatorPracticeAttached"), "a later phase also implies the finished practice");
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
Check(restored.phase == "ToolPractice" && restored.writes == 1 && Level3ChapterRules.Decide(restored) == Level3EntryDecision.Resume &&
      restored.Case("L3-BEA-001").admitted && !restored.Case("L3-MARINE-001").admitted,
    "the chapter round-trips: ToolPractice, Bea admitted, company waiting, first write");
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
    Check(saved && onDisk != null && onDisk.campaign.levelThree.phase == "ToolPractice" && WithoutChapter(onDisk) == liveWithout,
        "retry: the signed save holds the chapter at ToolPractice with every carried possession intact");
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
      bea.receptionistGreeting.Any(l => l.text.Contains("calculator")), "Bea's greeting opens with the history line and assigns the calculator first");

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
      steps[1].GetProperty("trigger").GetString() == Level3ChapterRules.CalculatorTrigger && steps[1].GetProperty("text").GetString().Contains("calculator"),
    "objectives: check in at reception, then the calculator introduction (first task)");
Check(steps.All(s => ObjectiveTriggers.Validate(s.GetProperty("trigger").GetString()) == null), "every Level 3 objective trigger is a known event");
var rewards = Json("Levels/Level_03/rewards.json");
Check(!rewards.GetProperty("requireSideQuestBeforeOffice").GetBoolean() && rewards.GetProperty("officeDoors").GetArrayLength() == 0,
    "the office is open from the start (the receptionist is not gated by an old side quest)");
GuideScript guide = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
string guideReport = guide.Validate();
Check(guideReport == "" && guide.lessons.Select(l => l.id).Take(2).SequenceEqual(new[] { "welcome", "calculator-intro" }),
    "the Level 3 guide validates: welcome, then the calculator task (" + guideReport + ")");
Check(guide.lessons[1].when == "flag:assignmentReceived" && guide.lessons[1].after == "welcome" && guide.guide.name == "Arthur",
    "Arthur presents the calculator task only after the assignment is committed");
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
Check(chapter3.GetProperty("comingSoon").GetBoolean() && !chapter3.TryGetProperty("scene", out _), "Career Chapter 3 stays coming soon with no scene route");
Check(!Src("Scripts/RewardsSystem/CareerProgressService.cs").Contains("Office_Level3"), "no built-in career route reaches Office_Level3");

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
      start3.Contains("EnsurePart<Level3Checkpointer>(Level3Checkpointer.ChildName);") && !start3.Contains("EnsurePart<Level2"),
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
Check(cp.Contains("if (before == Level3Phase.Reception && Phase == Level3Phase.ToolPractice) GameplayEvents.RaiseChapterAssignmentReceived();") &&
      cp.Split("RaiseChapterAssignmentReceived").Length == 2,
    "ChapterAssignmentReceived is raised once, only by a committed transition");
int restoreStart = Index(cp, "private void RestoreQuietly()"), restoreEnd = Index(cp, "private static string ActiveDefinitionId");
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

// ---------- the real practice path: source -> key -> source -> = -> attach, twice ----------
Level3ChapterCheckpoint practice = assigned.Copy();
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
Level3ChapterCheckpoint attach1 = practice.Copy();
Check(Level3PracticeRules.AttachTape(attach1, Level3PracticeRules.FirstSlot, calc.DraftExpression, calc.DraftResult, out string hint1) == Level3CommandOutcome.Applied && hint1 == null,
    "the 80 tape attaches to slot 1");
Check(Snap(practice) == practiceBefore, "attaching changes only the candidate copy");
Check(attach1.phase == "ToolPractice" && attach1.Tape(Level3PracticeRules.FirstSlot).result == "80" &&
      attach1.Tape(Level3PracticeRules.FirstSlot).expression.Count == 3 && attach1.milestones.Contains(Level3PracticeRules.FirstMilestone) &&
      attach1.actionIds.Contains("Level_03/attach/practice/after-paper"),
    "slot 1 holds the player's own tape (expression + exact result); practice continues");
Check(Level3PracticeRules.Sources(attach1).Any(s => s.SourceId == Level3PracticeRules.FirstTapeSource && s.Amount == "80"),
    "the attached tape becomes a source for the next step (reopen tape)");
calc.ConsumeDraft();
Check(!calc.HasDraft && calc.DisplayText == "80.00", "the attached tape leaves the calculator (the display keeps the result)");
Level3PracticeSource tape1 = Level3PracticeRules.Sources(attach1).First(s => s.SourceId == Level3PracticeRules.FirstTapeSource);
calc.SelectSource(tape1.SourceId, tape1.Label, tape1.Amount);
calc.Press(Level3CalcKey.Subtract);
calc.SelectSource(courier.SourceId, courier.Label, courier.Amount);
calc.Press(Level3CalcKey.Equals);
Check(calc.DraftResult == "72" && Level3Calculator.TapeLines(calc.DraftExpression, calc.DraftResult).SequenceEqual(new[] { "80.00 TAPE", "− 8.00 COURIER", "= 72.00" }),
    "tape 1 minus the courier prints 72");
Level3ChapterCheckpoint attach2 = attach1.Copy();
Check(Level3PracticeRules.AttachTape(attach2, Level3PracticeRules.SecondSlot, calc.DraftExpression, calc.DraftResult, out _) == Level3CommandOutcome.Applied &&
      attach2.phase == "BeaRecords" && attach2.actionIds.Contains(Level3PracticeRules.CompleteAction) && attach2.milestones.Contains(Level3PracticeRules.CompleteMilestone) &&
      Level3ChapterRules.Problem(attach2) == null && Level3PracticeRules.IsComplete(attach2),
    "the 72 tape completes practice in the same candidate: ToolPractice -> BeaRecords");
PlayerProgressData practiced = Level3ChapterRules.WithCheckpoint(Clone(live), attach2);
Check(practiced != null && WithoutChapter(practiced) == liveWithout && practiced.campaign.coins == live.campaign.coins &&
      practiced.campaign.publicTrust == live.campaign.publicTrust && practiced.totalExp == live.totalExp &&
      practiced.campaign.rewardClaims.SequenceEqual(live.campaign.rewardClaims),
    "practice changes no wallet, trust, XP, claims, inventory or first check: only the chapter snapshot");

// ---------- manual entry and refusals (no grade, nothing changes) ----------
var typed = Calc();
Keys(typed, "100-20=");
Level3ChapterCheckpoint manualAttach = practice.Copy();
Check(typed.DraftExpression.All(t => !t.IsNumber || t.manual) &&
      Level3PracticeRules.AttachTape(manualAttach, Level3PracticeRules.FirstSlot, typed.DraftExpression, typed.DraftResult, out _) == Level3CommandOutcome.Applied,
    "typing 100 − 20 by hand is accepted (manual entry stays possible; marked as typed)");
var refusals = new (string what, Level3ChapterCheckpoint state, string slot, string keys, string result, string expectHint)[]
{
    ("a typed final number with no working", practice.Copy(), Level3PracticeRules.FirstSlot, "80=", null, Level3PracticeRules.RefuseNoWorking),
    ("a result that does not fit the slot", practice.Copy(), Level3PracticeRules.FirstSlot, "100-25=", null, Level3PracticeRules.Slots[0].Hint),
    ("slot 2 before slot 1", practice.Copy(), Level3PracticeRules.SecondSlot, "80-8=", null, Level3PracticeRules.Slots[0].Hint),
    ("a printed result that disagrees with its own working", practice.Copy(), Level3PracticeRules.FirstSlot, "100-20=", "81", Level3PracticeRules.RefuseNoWorking),
    ("no tape printed", practice.Copy(), Level3PracticeRules.FirstSlot, "", null, Level3PracticeRules.RefuseNoTape),
    ("practice before the reception assignment", entry.Copy(), Level3PracticeRules.FirstSlot, "100-20=", null, Level3PracticeRules.RefuseNotNow),
    ("an unknown slot", practice.Copy(), "practice/elsewhere", "100-20=", null, Level3PracticeRules.RefuseNotNow)
};
foreach (var (what, state, slot, keys, result, expectHint) in refusals)
{
    var c = Calc();
    Keys(c, keys);
    string before = Snap(state);
    Level3CommandOutcome outcome = Level3PracticeRules.AttachTape(state, slot, c.DraftExpression, result ?? c.DraftResult, out string why);
    Check(outcome == Level3CommandOutcome.Refused && why == expectHint && Snap(state) == before, $"refused without change: {what} ('{why}')");
}
Level3ChapterCheckpoint again = attach1.Copy();
string againBefore = Snap(again);
Check(Level3PracticeRules.AttachTape(again, Level3PracticeRules.FirstSlot, typed.DraftExpression, typed.DraftResult, out _) == Level3CommandOutcome.Duplicate &&
      Snap(again) == againBefore, "attaching to a filled slot again is a quiet duplicate (no second tape)");
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

// ---------- the snapshot with tapes: round trip, repair, quiet resume ----------
PlayerProgressData tapeSave = JsonSerializer.Deserialize<PlayerProgressData>(Snap(Level3ChapterRules.WithCheckpoint(Clone(live), attach1)), json);
Check(!tapeSave.Normalize() && Snap(tapeSave.campaign.levelThree.tapes) == Snap(attach1.tapes) &&
      Level3ChapterRules.Decide(tapeSave.campaign.levelThree) == Level3EntryDecision.Resume,
    "a saved practice tape reloads exactly, without repairs, and resumes");
Level3ChapterCheckpoint deep = attach1.Copy();
deep.tapes[0].expression[0].value = "999";
Check(attach1.tapes[0].expression[0].value == "100", "Copy() is deep (a command never edits the live tape)");
Level3ChapterCheckpoint doubled = attach1.Copy();
doubled.tapes.Add(attach1.tapes[0].Copy());
doubled.tapes.Add(null);
Check(doubled.Repair() && doubled.tapes.Count == 1, "repair keeps one tape per destination (no duplicated tape after a bad write)");
Check(Level3ChapterRules.ImpliedLessons(attach1).Contains(Level3ChapterRules.PracticeLesson) &&
      !Level3ChapterRules.ImpliedLessons(practice).Contains(Level3ChapterRules.PracticeLesson),
    "resuming after tape 1 skips the guided practice lesson; before it, the lesson still runs");
Check(Level3ChapterRules.ImpliedLessons(attach2).Contains(Level3ChapterRules.PracticeDoneLesson) &&
      Level3ChapterRules.ImpliedTriggers(Level3Phase.BeaRecords).Contains(Level3ChapterRules.CalculatorTrigger),
    "resuming after practice: the objective is done and no practice lesson replays");
foreach (string answer in new[] { "62500", "600000", "expected" })
    Check(!Snap(attach2).Contains(answer), $"the practice snapshot carries no case answer ('{answer}')");

// ---------- failed write of the completing tape (disposable store) ----------
string sandbox3 = Path.Combine(Path.GetTempPath(), "TaxVentureL3T03-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox3);
try
{
    string payloadOf(PlayerProgressData d) => JsonSerializer.Serialize(d, json);
    string Encode3(PlayerProgressData d) => JsonSerializer.Serialize(new Dictionary<string, object> { ["format"] = 1, ["payload"] = payloadOf(d), ["signature"] = SaveFileStore.Sign(payloadOf(d)) });
    var store3 = new SaveFileStore(Path.Combine(sandbox3, "progress.json"));
    PlayerProgressData published3 = Level3ChapterRules.WithCheckpoint(Clone(live), attach1);
    store3.Write(Encode3(published3));
    byte[] onDisk = File.ReadAllBytes(store3.FilePath);
    string publishedBefore = Snap(published3);
    Directory.CreateDirectory(store3.TempPath);
    bool wrote = false;
    try { PlayerProgressData next = Level3ChapterRules.WithCheckpoint(Clone(published3), attach2); next.Normalize(); store3.Write(Encode3(next)); published3 = next; wrote = true; }
    catch (Exception) { }
    Check(!wrote && Snap(published3) == publishedBefore && File.ReadAllBytes(store3.FilePath).SequenceEqual(onDisk) &&
          published3.campaign.levelThree.phase == "ToolPractice" && published3.campaign.levelThree.tapes.Count == 1,
        "a failed write of the second tape keeps ToolPractice, one tape, and the file unchanged (attach can be retried)");
    Directory.Delete(store3.TempPath);
}
finally
{
    try { Directory.Delete(sandbox3, true); } catch (IOException) { }
}

// ---------- guide, assets and wiring ----------
GuideScript guide3 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
string guide3Report = guide3.Validate();
Check(guide3Report == "" && guide3.lessons.Select(l => l.id).Take(5).SequenceEqual(new[] { "welcome", "calculator-intro", "calculator-practice", "calculator-own-turn", "calculator-done" }),
    "the guide validates: welcome, calculator task, guided first tape, own turn, done (" + guide3Report + ")");
GuideLesson guided = guide3.lessons.First(l => l.id == "calculator-practice");
Check(guided.pages.Select(p => p.waitFor).SequenceEqual(new[] { "cue:" + Level3DeskCalculator.CueSourceSelected, "cue:" + Level3DeskCalculator.CueTapePrinted, "cue:" + Level3DeskCalculator.CueTapeAttached }) &&
      guided.when == "flag:calculatorUp" && guided.pages.All(p => p.IsHint),
    "each guided step waits for the player's own action (source tap, =, attach), never a page tap, and never blocks the desk");
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
Check(Index(pollBody, "if (down)") < Index(pollBody, "if (!up || pressed == null) return;") &&
      Index(pollBody, "if (!up || pressed == null) return;") < Index(pollBody, "Activate(target)") &&
      pollBody.Contains("bool tap = released == target") && !pollBody.Contains("unscaledTime"),
    "keys enter once on tap-release over the same key (press only lowers the cap; no time-based repeat)");
Check(pollBody.Contains("MobileInputGuard.SuppressWorldTapsThisFrame();") && deskSrc.Contains("[DefaultExecutionOrder(-40)]") &&
      deskSrc.Contains("1 << Level2DeskStamps.DragLayer") && pollBody.Contains("MobileInputGuard.IsPointerOverUI(screen)"),
    "a tap on the tools is theirs: HUD buttons win, and the desk behind never reacts to the same tap");
Check(deskSrc.Contains("if (!AtDesk)") && deskSrc.Contains("if (mode == Mode.Lifted || mode == Mode.Lifting) PutAway();") &&
      deskSrc.Contains("private void OnDisable()\n    {\n        ReleasePointer();\n        SettleStamps();") && deskSrc.Contains("if (!focused) ReleasePointer();") &&
      deskSrc.Contains("if (paused) ReleasePointer();") && deskSrc.Contains("!InGameSettings.IsOpen && !DialogueUI.IsAnyDialogueActive"),
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
Check(beaTalk.introduction.Any(l => l.speaker == "Npc" && l.text == "I kept the receipts this time. That means I can subtract all of them, right?") &&
      beaTalk.introduction.Any(l => l.speaker == "Player" && l.text == "Let's look at what each record supports."),
    "her first talk uses CONTENT's exact lines");
Check(beaTalk.introduction[0].speaker == "Npc" && beaTalk.introduction[0].text.Contains("I'm Beatriz") && beaTalk.introduction[0].text.Contains("design logos and menus"),
    "she introduces herself fully (a player who never met her in Level 1 still knows who she is)");
var inquiryText = beaTalk.inquiries.ToDictionary(i => i.question, i => i.answer);
Check(inquiryText.Count == 3 &&
      inquiryText["What was this purchase for?"] == "My birthday dinner and decorations. The studio wasn't involved." &&
      inquiryText["Does this invoice belong to the annual total?"] == "Yes, that supplier bill is in the printing summary." &&
      inquiryText["Show me the period these records cover."] == "These are the records for the whole year shown on the sheets." &&
      beaTalk.leaveChoice == "I'll review the papers first.",
    "the source inquiries and the leave option are CONTENT's exact words");
Check(Level3TownContent.Ordered(beaTalk, "B-PERSONAL")[0].id == "purpose" && Level3TownContent.Ordered(beaTalk, "B-PRINT")[0].id == "invoice" &&
      Level3TownContent.Ordered(beaTalk, null).Select(i => i.id).SequenceEqual(new[] { "purpose", "invoice", "period" }) &&
      Level3TownContent.Ordered(beaTalk, "B-PERSONAL").Count == 3,
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
foreach (var (what, state) in new[] { ("during calculator practice", attach1.Copy()), ("before the reception", entry.Copy()) })
{
    string before = Snap(state);
    Check(Level3ChapterRules.CollectBeaRecords(state, out _) == Level3CommandOutcome.Refused && Snap(state) == before,
        $"no handover {what} (records open only after practice)");
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
Check(bySource["B-ID"].name == "Beatriz Mendoza" && bySource["B-ID"].reference == "•••• B26",
    "the ID card prints her name and the masked identifier (no invented official TIN)");
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
Check(objectiveSteps.Count >= 3 && objectiveSteps[2].GetProperty("trigger").GetString() == Level3ChapterRules.RecordsTrigger &&
      ObjectiveTriggers.Validate(Level3ChapterRules.RecordsTrigger) == null,
    "objective 3: meet Bea and collect her records (a known event raised only by the saved handover)");
GuideScript guide4 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
Check(guide4.Validate() == "" && guide4.lessons.Any(l => l.id == Level3ChapterRules.BeaRecordsLesson && l.when == "flag:beaRecordsCollected" && l.pages.All(p => p.IsHint)),
    "Arthur's records hint follows the committed handover and never blocks");
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
Check(beaSrc.Contains("else if (phase == Level3Phase.BeaRecords) Talk(Introduction(bea), HandOver);") &&
      beaSrc.Contains("() => { End(); completed?.Invoke(); },") && beaSrc.Contains("            End,\n            this);"),
    "the handover runs only when the introduction completes; cancelling only ends the talk");
int handAt = Index(beaSrc, "private void HandOver()");
string handBody = beaSrc.Substring(handAt, Index(beaSrc, "// QUESTIONS") - handAt);
Check(Index(handBody, "owner.CommitBeaRecords(") < Index(handBody, "table.SyncPapers(animate: true)") &&
      Index(handBody, "case Level3Checkpointer.TapeCommit.Attached:") < Index(handBody, "table.SyncPapers(animate: true)"),
    "the papers fan out only after the handover saved");
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
Check(table4.Contains("ClientRecordPaper.Create(this, slot, workspace, record, size, gameObject.layer)") && table4.Contains("Level3RecordRules.Build(definition, onTable)") &&
      table4.Contains("acquired.FindAll(id => id != Level3ChapterRules.BeaCreditSource)") &&
      table4.Contains("SetPapersInteractive(Seated && !worksheetUp);") && table4.Contains("CameraController.WorkstationExited += HandleLeftSeat;") &&
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
Check(steps5.Count >= 4 && steps5[3].GetProperty("trigger").GetString() == Level3ChapterRules.WorksheetTrigger && ObjectiveTriggers.Validate(Level3ChapterRules.WorksheetTrigger) == null &&
      ObjectiveTriggers.Events.Contains(ObjectiveTriggers.WorksheetDrafted),
    "objective 4: build Bea's worksheet (a known event raised only by a saved, filled-in draft)");
GuideScript guide5 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var intro5 = guide5.lessons.FirstOrDefault(l => l.id == Level3ChapterRules.WorksheetLesson);
var notes5 = guide5.lessons.FirstOrDefault(l => l.id == Level3ChapterRules.WorksheetNotesLesson);
var done5 = guide5.lessons.FirstOrDefault(l => l.id == Level3ChapterRules.WorksheetDraftedLesson);
Check(guide5.Validate() == "" && intro5 != null && notes5 != null && done5 != null && new[] { intro5, notes5, done5 }.All(l => l.pages.All(wsP => wsP.IsHint)),
    "the worksheet lessons are valid hints (they never block the board)");
Check(intro5.pages.Select(wsP => wsP.waitFor).Take(3).SequenceEqual(new[] { "flag:worksheetUp|cue:WorksheetLifted", "cue:WorksheetStripLifted", "cue:WorksheetStripPlaced" }) &&
      notes5.pages[0].waitFor == "cue:WorksheetNoteAnswered" && intro5.onlyIn == "mode:Workstation",
    "hands-on: each step waits for the player's own lift, place and note answer");
Check(!new[] { intro5, notes5, done5 }.SelectMany(l => l.pages).Any(wsP => wsP.text.Contains("Personal purpose") || wsP.text.Contains("Already represented") || wsP.text.Contains("Not deductible") || wsP.text.Contains("click", StringComparison.OrdinalIgnoreCase)),
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
Check(table5.Contains("BuildWorksheet();") && table5.Contains("Worksheet = gameObject.AddComponent<Level3WorksheetDesk>();") &&
      table5.Contains("matSize.y * 0.5f - cellD * 0.5f") && table5.Contains("public bool ReadOriginal(string sourceId)") && table5.Contains("if (paper.IsAttached) paper.TakeOut();"),
    "the originals lie in one far row, the board rests on the near half, READ ORIGINAL lifts the real paper");
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
      book6.All(s => s.GetProperty("body").GetString().Split("\n\n").Length == s.GetProperty("clickableValues").GetArrayLength() &&
                     s.GetProperty("clickableValues").EnumerateArray().All(v => v.GetProperty("readOnly").GetBoolean())),
    "book.json adds the two leaves the hints name (L3-33), one paragraph per read-only term");
string bookText6 = File.ReadAllText(Res("Levels/Level_03/book.json"));
foreach (string leak in new[] { "500,000", "400,000", "180,000", "90,000", "30,000", "24,000", "900,000", "300,000", "Bea", "PRINT-26", "birthday" })
    Check(!book6.Take(2).Any(s => s.GetRawText().Contains(leak)), $"the worksheet leaves (Sections 4-5) state no client answer or figure ('{leak}')");
var steps6 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps6.Count >= 6 && steps6[4].GetProperty("trigger").GetString() == Level3ChapterRules.WorksheetReadyTrigger &&
      steps6[5].GetProperty("trigger").GetString() == Level3ChapterRules.WorksheetCollectedTrigger &&
      ObjectiveTriggers.Validate(Level3ChapterRules.WorksheetReadyTrigger) == null && ObjectiveTriggers.Validate(Level3ChapterRules.WorksheetCollectedTrigger) == null,
    "objectives 5-6: check and fix the worksheet (WorksheetReady), collect it (WorksheetCollected), both known events");

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
Check(Level3DeskCalculator.AwayAtBeaTable(Level3Phase.WorksheetDraft) && Level3DeskCalculator.AwayAtBeaTable(Level3Phase.WorksheetReady) &&
      !Level3DeskCalculator.AwayAtBeaTable(Level3Phase.BeaRecords) && !Level3DeskCalculator.AwayAtBeaTable(Level3Phase.BeaComputation),
    "one calculator: it is at Bea's table only while her worksheet is built, and back on the office desk for the computation");

// ---------- guide: hands-on, no answers ----------
GuideScript guide6 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var t06Lessons = new[] { Level3ChapterRules.SubtotalsLesson, Level3ChapterRules.CheckLesson, Level3ChapterRules.CorrectionsLesson, Level3ChapterRules.ReadyLesson, Level3ChapterRules.CollectedLesson }
    .Select(id => guide6.lessons.FirstOrDefault(l => l.id == id)).ToList();
Check(guide6.Validate() == "" && t06Lessons.All(l => l != null && l.pages.All(p => p.IsHint)), "the five T06 lessons exist and are hints (they never block the board)");
Check(t06Lessons[0].pages.Select(p => p.waitFor).Take(3).SequenceEqual(new[] { "flag:worksheetCalcUp|cue:" + Level3WorksheetDesk.CueCalcUp, "cue:" + Level3WorksheetDesk.CueTapePrinted, "cue:" + Level3WorksheetDesk.CueTapeAttached }) &&
      t06Lessons[1].when == "flag:worksheetTapes" && t06Lessons[2].when == "flag:worksheetCorrecting" && t06Lessons[3].when == "flag:worksheetReady",
    "hands-on: the subtotal lesson waits for the player's own calculator, tape and attach; the check lessons follow saved state");
Check(!t06Lessons.SelectMany(l => l.pages).Any(p => p.text.Contains("Personal purpose") || p.text.Contains("Already represented") || p.text.Contains("Not deductible") ||
      p.text.Contains("Supporting only") || p.text.Contains("900,000") || p.text.Contains("click", StringComparison.OrdinalIgnoreCase)),
    "Arthur names no answer or total and speaks touch only");
Check(guide6.lessons.First(l => l.id == "calculator-done").pages[0].text.Contains("Take the calculator along"), "practice ends by taking the calculator to Bea's table");

// ---------- wiring (commands own candidate -> save -> publish; effects only after) ----------
string cp6 = Src("Scripts/Level3/Level3Checkpointer.cs");
string Body6(string src, string start, string end) { int at6 = Index(src, start); return at6 < 0 ? "" : src.Substring(at6, Index(src.Substring(at6), end) < 0 ? src.Length - at6 : Index(src.Substring(at6), end)); }
string collectBody6 = Body6(cp6, "public CollectCommit CollectWorksheet(", "private static CaseDefinition ActiveDefinition");
Check(Index(collectBody6, "Level3WorksheetCheck.Acquire(candidate") < Index(collectBody6, "CommitLevelThreeWorksheet(candidate, out grants)") &&
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
Check(folder6.Contains("foreach (string id in work) papers.Add(caseId + \"/\" + id);") && folder6.Contains("Level3WorksheetCheck.ReferenceLines(chapter, Level3WorksheetRules.RowsOf(definition))"),
    "the Case Folder lists the collected worksheet (no extra Bag slot) with its read-only page");
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
      choices7[0].Label == "Worksheet · Income subtotal" && choices7[2].Label == "Bea's draft · Proposed costs",
    "the totals on offer are the collected worksheet's two subtotal tapes and Bea's draft total (a plausible wrong choice), the same list for both lines");
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
Check(Level3PracticeRules.AttachTape(practiceChain, Level3PracticeRules.FirstSlot, pc7.DraftExpression, pc7.DraftResult, out _) == Level3CommandOutcome.Applied,
    "the T03 practice still attaches its last-step draft unchanged");

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
      book7[3].GetProperty("heading").GetString() == "Section 7 – Annual Individual Table (2023 onwards)" && book7[4].GetProperty("heading").GetString() == "Section 8 – Base Plus Excess" &&
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
Check(steps7.Count >= 7 && steps7[6].GetProperty("trigger").GetString() == Level3ChapterRules.TaxDueTrigger && ObjectiveTriggers.Validate(Level3ChapterRules.TaxDueTrigger) == null &&
      ObjectiveTriggers.Events.Contains(ObjectiveTriggers.TaxDueAttached),
    "objective 7: work out Bea's taxable income and tax due (a known event, raised from a saved, structural state)");
GuideScript guide7 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var t07Lessons = new[] { Level3ChapterRules.ComputationIntroLesson, Level3ChapterRules.ComputationTotalsLesson, Level3ChapterRules.ComputationTaxableLesson,
    Level3ChapterRules.ComputationRuleLesson, Level3ChapterRules.ComputationTaxLesson, Level3ChapterRules.ComputationDoneLesson }.Select(id => guide7.lessons.FirstOrDefault(l => l.id == id)).ToList();
Check(guide7.Validate() == "" && t07Lessons.All(l => l != null && l.pages.All(p => p.IsHint)) &&
      t07Lessons[1].pages[0].waitFor == "flag:computationTotals" && t07Lessons[2].pages[0].waitFor == "flag:computationTaxable|cue:" + Level3DeskCalculator.CueSheetTape &&
      t07Lessons[3].pages[0].waitFor == "flag:computationRule|cue:" + Level3DeskCalculator.CueRuleChosen && t07Lessons[4].pages[0].waitFor == "flag:computationTaxDue",
    "the six computation lessons are hints that wait for the player's own picks and tapes");
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
Check(desk7.Contains("public sealed partial class Level3DeskCalculator") && desk7.Contains("bool visible = phase >= Level3Phase.ToolPractice && phase < Level3Phase.BeaComputation;") &&
      desk7.Contains("LeafOwns(collider)") && desk7.Contains("if (ActivateLeaf(target)) return;") && desk7.Contains("calculator.SetTape(DraftLines(), animate: true, TapeFeedSeconds);"),
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
      beaTown8.certificateHandover.Any(l => l.speaker == "Npc" && l.text.Contains("already in my menu fees")) &&
      !string.Join(" ", beaTown8.certificateHandover.Select(l => l.text)).Contains("12,500") && !beaTown8.creditAnswers.Any(a => a.reply.Contains("12,500") || a.reply.Contains("50,000")),
    "she walks Rosa's doorway route to the conference room client spot; her lines say the payment is already in her fees and name no credit or balance");
var brokenTown8 = JsonSerializer.Deserialize<Level3TownFile>(File.ReadAllText(Res("Levels/Level_03/town_life.json")), json);
brokenTown8.bea.creditAnswers.RemoveAt(1);
Check(Level3TownContent.Validate(brokenTown8).Any(p => p.Contains("creditAnswers")), "a town file missing one answer is reported (the built-in Bea is used)");
var book8 = Json("Levels/Level_03/book.json").GetProperty("sections").EnumerateArray().ToList();
JsonElement leaf8 = book8.First(s => s.GetProperty("heading").GetString().StartsWith("Section 9"));
string leaf8Text = leaf8.GetProperty("body").GetString() + " " + string.Join(" ", leaf8.GetProperty("clickableValues").EnumerateArray().Select(v => v.GetProperty("displayText").GetString()));
Check(book8.Count >= 6 && book8[5].GetProperty("heading").GetString() == "Section 9 – Credit Versus Expense" && leaf8.GetProperty("heading").GetString() == "Section 9 – Credit Versus Expense" &&
      leaf8.GetProperty("heading").GetString().StartsWith(Level3BeaInteractable.HelpSectionPrefix) &&
      leaf8.GetProperty("body").GetString().Split("\n\n").Length == leaf8.GetProperty("clickableValues").GetArrayLength() &&
      leaf8Text.Contains("tax due − credits = remaining payable") && leaf8Text.Contains("count it once") && leaf8Text.Contains("keep its sign"),
    "book.json adds Section 9 – Credit Versus Expense (the help her wrong answer opens): credit at the end, count the payment once, keep a negative sign");
foreach (string leak in new[] { "12,500", "250,000", "62,500", "50,000", "400,000", "Bea", "Harbor", "WHT-B26" })
    Check(!leaf8Text.Contains(leak), $"the credit leaf holds no client figure or name ('{leak}'); its example is Practice Design Studio");
var steps8 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps8.Count >= 9 && steps8[7].GetProperty("trigger").GetString() == Level3ChapterRules.CertificateTrigger &&
      steps8[8].GetProperty("trigger").GetString() == Level3ChapterRules.PayableTrigger &&
      ObjectiveTriggers.Validate(Level3ChapterRules.CertificateTrigger) == null && ObjectiveTriggers.Validate(Level3ChapterRules.PayableTrigger) == null &&
      ObjectiveTriggers.Events.Contains(ObjectiveTriggers.WithholdingCertificateReceived) && ObjectiveTriggers.Events.Contains(ObjectiveTriggers.RemainingPayableAttached) &&
      !steps8[8].GetProperty("text").GetString().Contains("50,000"),
    "objectives 8-9: take Bea's certificate in the conference room, then link the credit and attach what she still has to pay (known events, no value named)");
GuideScript guide8 = JsonSerializer.Deserialize<GuideScript>(File.ReadAllText(Res("Tutorials/Level_03/guide.json")), json);
var t08Lessons = new[] { Level3ChapterRules.CreditCertificateLesson, Level3ChapterRules.CreditLinkLesson, Level3ChapterRules.CreditPayableLesson, Level3ChapterRules.CreditDoneLesson }
    .Select(id => guide8.lessons.FirstOrDefault(l => l.id == id)).ToList();
Check(guide8.Validate() == "" && t08Lessons.All(l => l != null && l.pages.All(p => p.IsHint)) &&
      t08Lessons[0].pages[0].waitFor == "flag:certificateReceived" && t08Lessons[1].pages[0].waitFor == "flag:computationCredit|cue:" + Level3DeskCalculator.CueCreditChosen &&
      t08Lessons[2].pages[0].waitFor == "flag:computationPayable" && t08Lessons[1].when == "flag:creditExplained",
    "the four credit lessons are hints waiting for the player's own handover, link and tape");
Check(!t08Lessons.SelectMany(l => l.pages).Any(p => p.text.Contains("12,500") || p.text.Contains("50,000") || p.text.Contains("250,000") || p.text.Contains("62,500") ||
      p.text.Contains("expense", StringComparison.OrdinalIgnoreCase) || p.text.Contains("click", StringComparison.OrdinalIgnoreCase)) &&
      !guide8.waypoints.Any(w => (w.target ?? "").Contains("ConferenceRoom")),
    "Arthur names no amount and gives away no answer to her question; touch-only words; no arrow to the conference room (a known place)");

// ---------- wiring (source checks; Unity behaviour is the user's) ----------
string cp8 = Src("Scripts/Level3/Level3Checkpointer.cs");
foreach (var (start8, command) in new[] { ("public TapeCommit CommitCertificate(", "Level3CertificateRules.HandOver(candidate"), ("public TapeCommit CommitCreditExplanation(", "Level3CertificateRules.Explain(candidate") })
{
    string body8 = Body6(cp8, start8, "/// <summary>");
    Check(Index(body8, "State.Copy()") < Index(body8, command) && Index(body8, command) < Index(body8, "CommitLevelThreeCheckpoint(candidate)") &&
          Index(body8, "CommitLevelThreeCheckpoint(candidate)") < Index(body8, "Publish(candidate);") && body8.Contains("return TapeCommit.SaveFailed;"),
        $"{start8.Trim('(')}: candidate, signed write, then publish; a failed write returns before publishing");
}
Check(cp8.Contains("if (!hadCertificate && Level3CertificateRules.Acquired(committed)) GameplayEvents.RaiseWithholdingCertificateReceived();") &&
      cp8.Contains("if (!wasPayable && committed.Case(Level3ChapterRules.BeaCaseId)?.computation?.payableNoted == true) GameplayEvents.RaiseRemainingPayableAttached();") &&
      !Body6(cp8, "private void RestoreQuietly()", "private static string ActiveDefinitionId").Contains("RaiseWithholding"),
    "both objective events come only from a published, saved change; a quiet resume raises neither");
string take8 = Body6(bea8, "private void TakeCertificate()", "private void ShowAnswers()");
Check(Index(take8, "owner.CommitCertificate(") < Index(take8, "dialogue.ContinueWith(TakeAction, lines)") && take8.Contains("case Level3Checkpointer.TapeCommit.SaveFailed:") &&
      Index(take8, "case Level3Checkpointer.TapeCommit.SaveFailed:") < Index(take8, "ShowTake();") &&
      bea8.Contains("if (paper.Raise(Level3CertificatePaper.Context.Conversation, from)") && !take8.Contains("Raise(") &&
      bea8.Contains("lines[lines.Count - 1].AwaitAction = TakeAction;"),
    "the certificate is taken deliberately: saved first, then the conversation goes on and it rises out of her hands; a failed save offers the same choice again");
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
      paper8.Contains("if (Showing == Context.Desk && IsUp && (InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive || !AtDesk)) Lower();") &&
      paper8.Contains("if (!focused && paper != null) paper.CancelDrag();") && paper8.Contains("if (paper != null && !paper.IsAttached) paper.Gather();"),
    "one physical certificate (the standard record paper and Type 8 face), built only after the saved acquisition; settings, a conversation, leaving the desk, focus loss and disabling settle it");
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
      dc8.Contains("certificate = gameObject.AddComponent<Level3CertificatePaper>();") && !dc8.Contains("CommitLevelThreeCheckpoint") &&
      Index(desk8, "if (CertificateUp)") < Index(desk8, "pressed = Pick(screen);") && desk8.Contains("certificate.Lower();") &&
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

// ---------- Bea's review clock: 16 active minutes after the practice, pauses, free next morning, one voucher ----------
ReviewClockDefinition clock9 = rc9.Clock(B7);
Check(clock9 != null && clock9.allowanceSeconds == 960f && clock9.warnSeconds.SequenceEqual(new[] { 120f, 30f }) && clock9.lateExp == 2 && clock9.lateHearts == 0 &&
      !Level3ReviewRules.ClockDue(assigned) && Level3ReviewRules.ClockDue(beaRecords) && Level3ReviewRules.ClockDue(pay8) && !Level3ReviewRules.ClockDue(ok9),
    "Bea's review gets 16 active minutes, due only after the protected calculator practice and never after approval");
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
Check(book9.Count == 7 && leaf9.GetProperty("heading").GetString() == "Section 10 – Checking a Computation" &&
      leaf9.GetProperty("body").GetString().Split("\n\n").Length == leaf9.GetProperty("clickableValues").GetArrayLength() &&
      leaf9Text.Contains("not filing a return or paying tax") && leaf9Text.Contains("Not Ready is an honest hold") &&
      !new[] { "600,000", "62,500", "50,000", "12,500", "Bea" }.Any(leaf9Text.Contains),
    "book.json adds Section 10 – Checking a Computation (CONTENT leaf): check in order, Ready is for computation review not filing/paying, an honest hold, stamp the current sheet; no client figure");
var steps9 = Json("Levels/Level_03/objectives.json").GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
Check(steps9.Count == 10 && steps9[9].GetProperty("trigger").GetString() == Level3ChapterRules.ApprovedTrigger &&
      ObjectiveTriggers.Validate(Level3ChapterRules.ApprovedTrigger) == null && ObjectiveTriggers.Events.Contains(ObjectiveTriggers.BeaCaseApproved),
    "objective 10: stamp Bea's computation and hand her folder to the Auditor (raised only from a saved approval)");
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
      auditor9.Contains("FolderToHUDIcon folder = levelFolder ? null : FolderInHand();") && auditor9.Contains("if (folder != null) folder.ReleaseQuietly();"),
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
string contact9 = Body6(stamps9, "private void StampContact(string type)", "/// <summary>Settles a press");
Check(Index(contact9, "owner.CommitComputation(") < Index(contact9, "leaf.PopStamp();") && Index(contact9, "case Level3Checkpointer.TapeCommit.Attached:") < Index(contact9, "leaf.PopStamp();") &&
      stamps9.Contains("Level2DeskStamps.ReadyPath") && stamps9.Contains("Level2DeskStamps.NotReadyPath") &&
      new[] { "sfx_stamp_pickup_01", "sfx_stamp_press_01", "sfx_stamp_return_01" }.All(stamps9.Contains) && !stamps9.Contains("CommitLevelThreeCheckpoint"),
    "the desk stamps stand on the desk's own stamp stand, use the supplied stamp cues, and only a saved stamp leaves ink");
string leaf9Src = Src("Scripts/Level3/Level3ComputationLeafModel.cs");
Check(leaf9Src.Contains("HitKind.StampBox") && leaf9Src.Contains("\"READY FOR\\nCOMPUTATION REVIEW\"") && leaf9Src.Contains("\"NOT READY\"") &&
      leaf9Src.Contains("\"STAMP OUT OF DATE\"") && leaf9Src.Contains("\"AUDITOR: START HERE\""),
    "Bea's sheet has a stamp box with both inks and an out-of-date tag, and the Auditor's START HERE mark for the returned line");
string desk9 = Src("Scripts/Level3/Level3DeskCalculator.cs");
Check(desk9.Contains("if (ActivateStamp(target)) return;") && desk9.Contains("LeafOwns(collider) || StampOwns(collider)") && desk9.Contains("CaptureStamps(lifting: true);") &&
      desk9.Contains("CaptureStamps(lifting: false);") && desk9.Contains("if (stamps) ComputeStampPoses(view, ss,"),
    "the stamps belong to the desk's one input owner and lift, fit and settle with the tools");
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

Console.WriteLine($"Level3Regression: {checks - fails}/{checks} checks passed");
return fails == 0 ? 0 : 1;
