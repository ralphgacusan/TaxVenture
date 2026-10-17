// Level 2 ("Something Does Not Match") regression: office-level profile,
// Rosa's case and review data, Level_02 content files, and the Level 1
// defaults those additions must not change. Run from this folder:
//   dotnet run --no-restore
// (build the scratch Assembly-CSharp first; see Level2Regression.csproj).
// Scene files are checked by Tests/VerifyLevel2Scene.py.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

int fails = 0, checks = 0;
void Check(bool ok, string what)
{
    checks++;
    if (!ok) { fails++; Console.WriteLine("FAIL: " + what); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string Res(string rel) => Path.Combine(root, "Assets", "Resources", rel);
string Src(string rel) => File.ReadAllText(Path.Combine(root, "Assets", rel));
JsonElement Json(string rel) => JsonDocument.Parse(File.ReadAllText(Res(rel))).RootElement;
string Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
var fieldJson = new JsonSerializerOptions { IncludeFields = true };

// ================= Office level profile =================
Check(OfficeLevelCatalog.LevelIdForScene("Office_Level1") == "Level_01", "Office_Level1 plays Level_01");
Check(OfficeLevelCatalog.LevelIdForScene("Office_Level2") == "Level_02", "Office_Level2 plays Level_02");
foreach (string other in new[] { "3D Menu", "Office_Level2_OLD", "Level1_Character_Intro_Cutscene", "", null })
{
    Check(OfficeLevelCatalog.LevelIdForScene(other) == null, $"unmapped scene '{other}' plays no level");
    Check(OfficeLevelCatalog.FeaturesForScene(other, new[] { "hud", "guide" }, out _).Count == 0, $"unmapped scene '{other}' builds no office system");
}
var level1Features = OfficeLevelCatalog.FeaturesForScene("Office_Level1", null, out string l1Problems);
Check(level1Features.SetEquals((OfficeFeature[])Enum.GetValues(typeof(OfficeFeature))) && l1Problems == null,
    "Office_Level1 always gets every office system, with no level.json list");
Check(OfficeLevelCatalog.FeaturesForScene("Office_Level1", Array.Empty<string>(), out _).Count == level1Features.Count,
    "an empty Level_01 list cannot switch Level 1 systems off");
Check(OfficeLevelCatalog.FeaturesForScene("Office_Level2", null, out _).Count == 0, "Level 2 without a list opts into nothing");
var parsed = OfficeLevelCatalog.ParseFeatures(new[] { " HUD", "minimap", "Guide ", "fade", "hud", "radar" }, out string parseProblems);
Check(parsed.Count == 4 && parseProblems != null && parseProblems.Contains("radar"), "feature names parse case-insensitively; unknown names are reported");
Check(!OfficeLevelCatalog.TryParseFeature("", out _) && !OfficeLevelCatalog.TryParseFeature(null, out _), "empty feature names are rejected");

// ================= Level_02 level.json =================
var level2 = Json("Levels/Level_02/level.json");
Check(Str(level2, "levelId") == "Level_02", "level.json: levelId Level_02");
Check(Str(level2, "levelName") == "Something Does Not Match", "level.json: Level 2 name");
var caseIds = level2.GetProperty("caseIds").EnumerateArray().Select(c => c.GetString()).ToList();
Check(caseIds.Count == 1 && caseIds[0] == "case_rosa_001", "level.json: exactly one case, Rosa's");
Check(File.Exists(Res("Levels/Level_02/cases/case_rosa_001.json")), "Rosa's case file exists");
var level2Features = OfficeLevelCatalog.FeaturesForScene("Office_Level2",
    level2.GetProperty("officeFeatures").EnumerateArray().Select(f => f.GetString()), out string l2Problems);
Check(l2Problems == null && level2Features.SetEquals(new[] { OfficeFeature.Hud, OfficeFeature.Minimap, OfficeFeature.Guide, OfficeFeature.Fade }),
    "Office_Level2 opts into HUD, minimap, guide and fade");
var level1 = Json("Levels/Level_01/level.json");
Check(!level1.TryGetProperty("officeFeatures", out _), "Level_01 level.json is unchanged (no officeFeatures)");
Check(level1.GetProperty("caseIds").GetArrayLength() == 1, "Level_01 still plays one case");

// ================= Rosa's case =================
CaseDefinition rosa = JsonSerializer.Deserialize<CaseDefinition>(File.ReadAllText(Res("Levels/Level_02/cases/case_rosa_001.json")), fieldJson);
Check(rosa != null && rosa.caseId == "L2-ROSA-001", "Rosa's case id is L2-ROSA-001");
Check(rosa.HasReview && rosa.review.reviewId == "L2-ROSA-001", "Rosa's case carries review data");
List<string> problems = CaseReviewValidator.Validate(rosa.review);
Check(problems.Count == 0, "Rosa's review data validates: " + string.Join("; ", problems));
Check(rosa.review.contentStatus == "educational-draft", "loan rule authority is reported honestly as an educational draft");
Check(Math.Abs(rosa.review.priorVerifiedAggregate - 120000f) < 0.01f && rosa.review.focusPeriod == "May 2026", "fixed schedule context: May 2026, 120,000 prior verified");
Check(rosa.review.initialDocumentIds.SequenceEqual(new[] { "COVER", "S1", "N1" }), "only the cover, summary S1 and note N1 are held at the start");
var s1 = rosa.review.FindDocument("S1");
Check(s1 != null && s1.kind == "summary", "S1 is the prepared summary");
Check(s1 != null && s1.lines.Where(l => l.role == "income").Sum(l => l.amount) == 15000f &&
      s1.lines.Single(l => l.role == "subtotal").amount == 15000f, "S1 prints 12,000 + 3,000 = 15,000");
Check(s1 != null && s1.lines.Any(l => l.role == "memo" && l.amount == 20000f && l.label.Contains("LN-0510")), "S1 lists the 20,000 loan as a non-income memorandum");
Check(s1 != null && !s1.lines.Any(l => l.label.IndexOf("commission", StringComparison.OrdinalIgnoreCase) >= 0), "S1 has no commission line (the omission to find)");
Check(s1 != null && (s1.footer ?? "").Contains("Taxpayer Assistance Desk"), "S1 footer names the Assistance Desk as preparer");
var n1 = rosa.review.FindDocument("N1");
Check(n1 != null && n1.lines.Any(l => l.value.Contains("CM-0526")), "N1 asks to confirm commission CM-0526");
Check(rosa.documents.Count == 9 && rosa.review.initialDocumentIds.Count == 3 && rosa.documents.All(d => d.fields.All(f => !string.IsNullOrEmpty(f.semanticKey))),
    "nine case documents (3 starting pages + 6 records), only 3 held at the start, every field keyed");
Check(rosa.potentialIssues == null || rosa.potentialIssues.Count == 0, "no potential issue is listed in the folder (it would show the answer)");
Check(rosa.clientFinale != null && !rosa.clientFinale.enabled, "no Level 1 client finale for Rosa until T11");
var graded = rosa.GradedFields().Select(f => f.key).ToList();
Check(graded.SequenceEqual(new[] { "residencyStatus", "taxpayerType", "incomeSource" }), "Rosa grades the three inherited taxpayer facts");
foreach (string key in graded)
    Check(CaseFieldRules.AnswerProblem(rosa.answerKey, key) == null, $"answerKey.{key} is a valid value");
Check(rosa.receptionistGreeting.Any(l => l.text.Contains("market-client drawer")), "the receptionist sends the player to the market-client drawer");

// Player-facing text gives no answer away and uses touch wording.
string pageText = string.Join("\n", rosa.review.documents.SelectMany(d => d.lines.Select(l => l.label + " " + l.value)).Concat(
    rosa.documents.SelectMany(d => d.fields.Select(f => f.label + " " + f.displayValue))));
foreach (string leak in new[] { "20,000 income", "140,000", "omitted", "Ready for Filing", "Not Ready", "cash versus bank", "add 5,000", "E2" })
    Check(pageText.IndexOf(leak, StringComparison.OrdinalIgnoreCase) < 0, "starting pages do not reveal: " + leak);

// ================= Review state =================
CaseReviewState state = CaseReviewState.Seed(rosa.review);
Check(state != null && state.reviewId == "L2-ROSA-001" && state.phase == ReviewPhase.OfficeWelcome, "Rosa's review starts at OfficeWelcome");
Check(state.HasDocument("COVER") && state.HasDocument("S1") && state.HasDocument("N1") && !state.HasDocument("D1"), "starting pages are held; D1 is not");
Check(!state.Acquire("S1") && state.acquiredDocumentIds.Count == 3, "collecting a held page again changes nothing");
Check(!state.Acquire("") && !state.Acquire(null), "empty page ids are ignored");
var changes = new List<string>();
state.PhaseChanged += (from, to) => changes.Add(from + ">" + to);
Check(state.TryAdvance(ReviewPhase.RetrieveFolder) && state.phase == ReviewPhase.RetrieveFolder, "receptionist talk moves the review to RetrieveFolder");
Check(!state.TryAdvance(ReviewPhase.RetrieveFolder) && !state.TryAdvance(ReviewPhase.OfficeWelcome), "phases never repeat or rewind");
Check(!state.TryAdvance((ReviewPhase)99) && state.phase == ReviewPhase.RetrieveFolder, "unknown phases are refused");
Check(changes.SequenceEqual(new[] { "OfficeWelcome>RetrieveFolder" }), "exactly one phase change was announced");
var restored = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(state, fieldJson), fieldJson);
Check(restored.phase == ReviewPhase.RetrieveFolder && restored.acquiredDocumentIds.Count == 3, "review state survives a field JSON round trip");
Check((int)ReviewPhase.Completed == 12 && (int)ReviewPhase.RetrieveFolder == 1, "ReviewPhase numbers are stable");
Check(CaseReviewState.Seed(new CaseReviewDefinition()) == null && CaseReviewState.Seed(null) == null, "ordinary cases get no review state");
var skipped = new CaseReviewDefinition { reviewId = "X", startPhase = "MarketVisit", initialDocumentIds = new List<string> { "A", "missing" },
    documents = new List<ReviewDocumentDefinition> { new ReviewDocumentDefinition { id = "A", kind = "note", title = "A",
        lines = new List<ReviewLineDefinition> { new ReviewLineDefinition { id = "a", label = "a", value = "a" } } } } };
var skippedState = CaseReviewState.Seed(skipped);
Check(skippedState.phase == ReviewPhase.MarketVisit && skippedState.acquiredDocumentIds.SequenceEqual(new[] { "A" }), "startPhase is honoured; unknown starting pages are never handed out");

// ================= T02 folder retrieval =================
var retrieval = CaseReviewState.Seed(rosa.review);
Check(!retrieval.FolderRetrieved && !retrieval.TryRetrieveFolder() && retrieval.phase == ReviewPhase.OfficeWelcome,
    "the folder cannot be taken before the receptionist assigns it");
retrieval.TryAdvance(ReviewPhase.RetrieveFolder);
int retrievedEvents = 0;
retrieval.PhaseChanged += (from, to) => retrievedEvents++;
Check(retrieval.TryRetrieveFolder() && retrieval.FolderRetrieved && retrieval.phase == ReviewPhase.PreliminaryReview,
    "taking the folder moves the review to PreliminaryReview");
Check(!retrieval.TryRetrieveFolder() && retrievedEvents == 1, "the folder is taken once; a second tap changes nothing");
Check(retrieval.acquiredDocumentIds.SequenceEqual(new[] { "COVER", "S1", "N1" }) && !retrieval.HasDocument("D1"),
    "retrieval gives only the three starting pages; supporting records stay out of reach");
var later = CaseReviewState.Seed(rosa.review);
later.TryAdvance(ReviewPhase.MarketVisit);
Check(later.FolderRetrieved && !later.TryRetrieveFolder(), "a later phase counts as retrieved and cannot retrieve again");

string drawerSrc = Src("Scripts/Level2/MarketClientDrawer.cs") + Src("Scripts/Level2/Level2CaseRetrieval.cs");
foreach (string forbidden in new[] { "isArchived", "ArchiveCaseState", "CaseCompleteState", "ChangeState(", "RewardAccumulator", "Score.", "TryReceiveValue", "ResetForRetry" })
    Check(!drawerSrc.Contains(forbidden), "retrieval cannot archive, audit, reward or move the case: no " + forbidden);
Check(Regex.Matches(drawerSrc, @"RaiseCaseFolderRetrieved\(\)").Count == 1 && drawerSrc.Contains("review.TryRetrieveFolder()"),
    "one commit point raises CaseFolderRetrieved after the review accepts it");
Check(drawerSrc.Contains("HideAndDisableFolder()") && drawerSrc.Contains("store.StoreFolderFromExternalSource()"),
    "the desk folder waits hidden; the retrieved folder goes into the HUD folder icon (count 0 -> 1)");
Check(Regex.IsMatch(drawerSrc, @"folder\.gameObject\.SetActive\(false\);\s*owner\.StoreRetrievedFolder\(\);") && drawerSrc.Contains("if (owner != null) owner.StoreRetrievedFolder();"),
    "the folder is stored when it reaches the icon, or at once if the animation is cut short");
Check(Src("Scripts/Interactables/FileCabinetInteractable.cs").Contains("data.isArchived = true;") &&
      Src("Scripts/Interactables/FileCabinetInteractable.cs").Contains("is ArchiveCaseState"),
    "the cabinet's own archive action is unchanged and still limited to ArchiveCaseState");
Check(Src("Scripts/Level2/Level2OfficeStart.cs").Contains("EnsurePart<Level2CaseRetrieval>(Level2CaseRetrieval.ChildName)"),
    "the drawer is built once under Level2Gameplay/CaseRetrieval");
Check(Src("Scripts/Level2/Level2Say.cs").Contains("GuideVoice.TrySay(text, mood, seconds)") && Src("Scripts/Level2/Level2Say.cs").Contains("LevelGate.NotifyBlocked(text)"),
    "Level 2 messages show in Arthur's bubble (visible at the desk), else the HUD toast");
foreach (Match m in Regex.Matches(drawerSrc, @"return ""([^""]+)"";|Message = ""([^""]+)"";"))
{
    string line = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
    Check(line.IndexOf("click", StringComparison.OrdinalIgnoreCase) < 0, "drawer text is touch wording: " + line);
}

// ================= T02 Rosa's pages in the case folder =================
var linkProblems = CaseReviewValidator.ValidateDocumentLinks(rosa.review, rosa.documents.Select(d => d.documentName));
Check(linkProblems.Count == 0, "every review page prints a case document: " + string.Join("; ", linkProblems));
Check(CaseReviewValidator.ValidateDocumentLinks(rosa.review, new[] { "Case Cover - L2-ROSA-001" }).Count == rosa.review.documents.Count - 1, "every page whose document is missing is rejected");
var twin = Clone();
twin.documents[2].documentName = twin.documents[1].documentName;
Check(CaseReviewValidator.ValidateDocumentLinks(twin, rosa.documents.Select(d => d.documentName)).Any(p => p.Contains("two pages")), "two pages cannot print the same document");
Check(Src("Scripts/Data/Loading/JsonCaseLoader.cs").Contains("CaseReviewValidator.ValidateDocumentLinks("), "the loader checks review page links");
string folderUi = Src("Scripts/UI/CaseFolderUI.cs");
Check(folderUi.Contains("AddReviewPages(list, data);") && folderUi.Contains("data.review.HasDocument(reviewPage.id)"),
    "the case folder adds a page per held review page after Taxpayer Information");
Check(folderUi.Contains("definition == null || !definition.HasReview"), "cases without review data keep their single folder page (Level 1)");
Check(Regex.IsMatch(folderUi, @"BuildReviewPageRows\(data, page\.Document\)") && folderUi.Contains("Instantiate(caseFolderFieldRowPrefab, page1FieldRowListRoot)"),
    "review pages print in page 1's rows (same prefab, container and font)");
foreach (var doc in rosa.documents)
{
    Check(doc.fields.Count <= 5, $"'{doc.documentName}' fits like page 1 (at most 5 rows, has {doc.fields.Count})");
    foreach (var f in doc.fields)
    {
        Check(f.displayValue.Length <= 22 && f.label.Length <= 18, $"'{f.label}: {f.displayValue}' is short enough for page 1's font");
        if (f.type == "Number") Check(CaseFolderUI.TryParseAmount(f.displayValue, out float amt) && amt > 0f, $"'{f.label}' prints as a number");
    }
}
Check(CaseFolderUI.TryParseAmount("₱12,000", out float a12) && a12 == 12000f && !CaseFolderUI.TryParseAmount("none", out _), "amounts parse from printed money");
Check(rosa.review.documents.All(d => d.title.Length <= 20), "page headings are as short as 'Taxpayer Information'");
Check(folderUi.Contains("PlaceBottomPageButton(leftEdgeZone, false);") && folderUi.Contains("PlaceBottomPageButton(rightEdgeZone, true);"),
    "previous / next page buttons sit at the bottom of the folder paper");
Check(folderUi.Contains("leftEdgeIndicator.SetActive(index > 0)") && folderUi.Contains("rightEdgeIndicator.SetActive(index < pages.Count - 1)"),
    "page arrows show like the Tax Code Book (none on a one-page folder)");
Check(!File.Exists(Path.Combine(root, "Assets", "Scripts", "Level2", "Level2PacketPages.cs")), "the side page-tab panel is gone");

// ================= T02 folder handoff (HUD icon -> desk) =================
var handoff = CaseReviewState.Seed(rosa.review);
Check(!handoff.AwaitingDeskPlacement && !handoff.TryPlaceFolderOnDesk(), "nothing to place before retrieval");
handoff.TryAdvance(ReviewPhase.RetrieveFolder);
handoff.TryRetrieveFolder();
Check(handoff.AwaitingDeskPlacement, "after retrieval the folder waits in the HUD icon");
Check(handoff.TryPlaceFolderOnDesk() && handoff.folderPlacedOnDesk && !handoff.AwaitingDeskPlacement, "putting it on the desk is recorded once");
Check(!handoff.TryPlaceFolderOnDesk(), "a second placement changes nothing");
Check(JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(handoff, fieldJson), fieldJson).folderPlacedOnDesk, "desk placement survives a JSON round trip");
string handoffSrc = Src("Scripts/Level2/Level2FolderHandoff.cs");
Check(handoffSrc.Contains("icon.ClickOverride = HandleClick") && handoffSrc.Contains("icon.DropOpenOverride = HandleDrop"),
    "the HUD folder icon's tap and drop are wrapped");
Check(Regex.IsMatch(handoffSrc, @"CameraMode\.Workstation[\s\S]*SitAtDeskMessage[\s\S]*IsOnDesk\(screenPosition\)[\s\S]*DropOnDeskMessage[\s\S]*PlaceOnDesk\(screenPosition\)"),
    "the folder can only be put down while sitting at the desk and dropped on it");
Check(Regex.IsMatch(handoffSrc, @"private bool HandleClick\(\)\s*\{\s*if \(Awaiting\)\s*\{\s*Level2Say\.Line"),
    "tapping the icon cannot open the folder while it waits for the desk");
Check(handoffSrc.Contains("store.ResetToUnstored()") && handoffSrc.Contains("deskFolder.RestoreAndEnableFolder()"),
    "placing uses the existing unstore + desk restore (count 1 -> 0, no second folder)");
Check(Regex.IsMatch(handoffSrc, @"BeginExternalDrag\(screenPosition\);\s*deskFolder\.EndExternalDrag\(screenPosition\);\s*deskFolder\.OpenDraggedFolder\(\);"),
    "dropping on the desk opens the folder right there");
Check(handoffSrc.Contains("AuditorSubmissionTray.SubmissionBlocked = BlockHandIn") && handoffSrc.Contains("AuditorSubmissionTray.SubmissionBlocked = null"),
    "no Auditor hand-in before the folder has been on the desk; the rule is removed with the level");
Check(Regex.IsMatch(Src("Scripts/Data/AuditorSubmissionTray.cs"), @"if \(SubmissionBlocked != null && SubmissionBlocked\(\)\)"),
    "the Auditor tray honours an optional level blocker (unset in Level 1)");
Check(Src("Scripts/Level2/Level2OfficeStart.cs").Contains("EnsurePart<Level2FolderHandoff>(Level2FolderHandoff.ChildName)"), "the handoff is built once under Level2Gameplay/FolderHandoff");
foreach (Match m in Regex.Matches(handoffSrc, @"Message = ""([^""]+)"";"))
    Check(m.Groups[1].Value.IndexOf("click", StringComparison.OrdinalIgnoreCase) < 0, "handoff text is touch wording: " + m.Groups[1].Value);

// ================= T03 finding and initial (preliminary) stamp =================
var stepTriggersAll = Json("Levels/Level_02/objectives.json").GetProperty("objectives").EnumerateArray()
    .SelectMany(o => o.GetProperty("steps").EnumerateArray()).Select(st => ObjectiveTriggers.Normalize(Str(st, "trigger"))).ToList();
var rule = rosa.review.preliminaryRule;
Check(rule.IsSet && rule.findingId == "F1" && rule.concernDocument == "S1" && rule.evidenceDocument == "N1" && rule.reasonId == "unaccounted-payment",
    "F1 rule: concern on S1, related record N1, reason unaccounted-payment");
Check(rosa.review.reasons.Count == 3 && rosa.review.reasons.All(r => r.label.Length <= 18), "three short reason choices that fit a folder row");
Check(!pageText.Contains("unaccounted-payment") && !File.ReadAllText(Res("Tutorials/Level_02/guide.json")).Contains("unaccounted-payment"),
    "the rule's answer ids are never printed");
var badRule = Clone();
badRule.preliminaryRule.reasonId = "nope";
Check(CaseReviewValidator.Validate(badRule).Any(x => x.Contains("reasonId")), "a rule with an unknown reason is rejected");
var badDoc = Clone();
badDoc.preliminaryRule.evidenceDocument = "D9";
Check(CaseReviewValidator.Validate(badDoc).Any(x => x.Contains("evidenceDocument")), "a rule naming a missing page is rejected");
var dupReason = Clone();
dupReason.reasons.Add(new ReviewReasonDefinition { id = "amounts-differ", label = "Again" });
Check(CaseReviewValidator.Validate(dupReason).Any(x => x.Contains("used twice")), "duplicate reason ids are rejected");

CaseReviewState Retrieved()
{
    var s = CaseReviewState.Seed(rosa.review);
    s.TryAdvance(ReviewPhase.RetrieveFolder);
    s.TryRetrieveFolder();
    return s;
}
var early = CaseReviewState.Seed(rosa.review);
Check(!early.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May subtotal"), "no finding before the folder is retrieved");
Check(PreliminaryReview.TryStamp(early, rule, true) == PreliminaryReview.Outcome.NotRetrieved && early.preliminary == null, "no stamp before retrieval");

var pr = Retrieved();
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.FindingIncomplete, "an empty finding cannot be stamped");
Check(pr.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May subtotal") && pr.packetRevision == 1, "setting the concern is a packet revision");
Check(!pr.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May subtotal") && pr.packetRevision == 1, "the same value again is not a revision");
Check(pr.phase == ReviewPhase.PreliminaryReview, "an incomplete finding keeps the initial review open");
pr.SetFindingPart("F1", ReviewFindingState.Evidence, "COVER", "FullName", "Client");
pr.SetFindingPart("F1", ReviewFindingState.Reason, null, "amounts-differ", "Amounts differ");
Check(pr.FindFinding("F1").IsComplete && pr.phase == ReviewPhase.PreliminaryStamp && pr.FindFinding("F1").status == "Needs checking",
    "a complete finding moves to PreliminaryStamp with status Needs checking");
Check(PreliminaryReview.TryStamp(pr, rule, false) == PreliminaryReview.Outcome.ReadyWhileOpen && pr.preliminary == null, "Ready with an open concern is guided, not recorded");
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.WrongEvidence && pr.preliminary == null, "the wrong related record is guided, not recorded");
pr.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralReference", "Reference");
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.WrongReason && pr.preliminary == null, "the wrong reason is guided, not recorded");
pr.SetFindingPart("F1", ReviewFindingState.Concern, "COVER", "FullName", "Client");
pr.SetFindingPart("F1", ReviewFindingState.Reason, null, "unaccounted-payment", "Not on summary");
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.WrongConcern, "a concern outside the summary is guided, not recorded");
pr.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummaryServices", "Alterations");
int revisionBefore = pr.packetRevision;
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.Recorded, "a supported Not Ready is recorded");
Check(pr.preliminary.verdict == "NotReady" && pr.preliminary.findingId == "F1" && pr.preliminary.reasonId == "unaccounted-payment" && pr.preliminary.revision == revisionBefore,
    "the initial assessment keeps its verdict, finding, reason and packet revision");
Check(pr.phase == ReviewPhase.MarketVisit, "after the initial stamp the review moves to MarketVisit (Rosa's stall)");
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.AlreadyStamped && PreliminaryReview.TryStamp(pr, rule, false) == PreliminaryReview.Outcome.AlreadyStamped,
    "stamping again records nothing");
Check(!pr.CanEditPreliminaryFinding && !pr.SetFindingPart("F1", ReviewFindingState.Reason, null, "amounts-differ", "Amounts differ"), "the stamped finding is read-only");
var prRound = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(pr, fieldJson), fieldJson);
Check(prRound.preliminary != null && prRound.FindFinding("F1").IsComplete && prRound.packetRevision == pr.packetRevision, "findings and the initial stamp survive a JSON round trip");
foreach (PreliminaryReview.Outcome o in Enum.GetValues(typeof(PreliminaryReview.Outcome)))
    if (o != PreliminaryReview.Outcome.NoRule)
    {
        string m = PreliminaryReview.Message(o);
        Check(!string.IsNullOrWhiteSpace(m) && m.IndexOf("click", StringComparison.OrdinalIgnoreCase) < 0 && !m.Contains("5,000") && !m.Contains("20,000"),
            $"guided line for {o} is touch wording with no amounts");
    }
Check(PreliminaryReview.Message(PreliminaryReview.Outcome.Recorded).Contains("Stall 04"), "the recorded line sends the player to Stall 04");

string prelimSrc = Src("Scripts/Data/PreliminaryReview.cs") + Src("Scripts/Level2/Level2PreliminaryStamp.cs");
foreach (string forbidden in new[] { "caseAssessment", "filingStatus", "assessmentStamped", "ChangeState(", "ComplianceAudit", "PrepareReturn", "RaiseCaseCompleted", "Score.", "RewardAccumulator" })
    Check(!prelimSrc.Contains(forbidden), "the initial stamp never touches the final assessment or rewards: no " + forbidden);
Check(Regex.IsMatch(Src("Scripts/Level2/Level2PreliminaryStamp.cs"), @"phase >= ReviewPhase\.FinalAssessment\) return false"), "the final assessment keeps the normal stamps");
Check(Src("Scripts/Level2/Level2PreliminaryStamp.cs").Contains("\"Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/Stamps\"") &&
      Src("Scripts/Level2/Level2PreliminaryStamp.cs").Contains("stampSet.gameObject.SetActive(true)"),
    "Level 2 switches the desk stamp set on (the copied Level 1 scene keeps it off)");
string stampUi = Src("Scripts/UI/StampUI.cs");
Check(stampUi.IndexOf("StampOverride != null && StampOverride(type)") > 0 && stampUi.IndexOf("StampOverride != null && StampOverride(type)") < stampUi.LastIndexOf("ApplyStamp("),
    "StampUI asks the level rule before its Level 1 apply");
Check(Src("Scripts/UI/StampCaseFolderUI.cs").Contains("\"Initial review\"") && Src("Scripts/UI/StampCaseFolderUI.cs").Contains("ReviewInkMark.Show("),
    "the stamped folder shows the initial review row and its ink");
string findingsSrc = Src("Scripts/Level2/Level2FindingsPage.cs");
Check(findingsSrc.Contains("source.kind == \"summary\"") && findingsSrc.Contains("source.kind == \"note\" || source.kind == \"record\"") && findingsSrc.Contains("review.HasDocument(source.id)"),
    "finding boxes take whole groups of held lines (no exact-key hint)");
Check(findingsSrc.Contains("GameplayEvents.RaiseFindingRecorded()") && findingsSrc.Contains("!announced"), "FindingRecorded is announced once");
Check(findingsSrc.Contains("CaseFolderUI.ExtraPages = BuildPages") && findingsSrc.Contains("CaseFolderUI.ExtraPages = null"), "the Findings page exists only while Level 2 runs");
Check(Src("Scripts/Level2/Level2OfficeStart.cs").Contains("EnsurePart<Level2FindingsPage>") && Src("Scripts/Level2/Level2OfficeStart.cs").Contains("EnsurePart<Level2PreliminaryStamp>"),
    "Findings page and initial stamp are built once under Level2Gameplay");
Check(stepTriggersAll.Contains("event:FindingRecorded") && stepTriggersAll.IndexOf("event:FindingRecorded") < stepTriggersAll.IndexOf("event:PreliminaryHoldStamped"),
    "objectives: record the finding, then stamp the initial review");

// ================= T03 desk stamping on the real folder =================
string deskStampsSrc = Src("Scripts/Level2/Level2DeskStamps.cs");
string stampDragSrc = Src("Scripts/Level2/Level2StampDrag.cs");
string folderUiSrc = Src("Scripts/UI/CaseFolderUI.cs");
Check(deskStampsSrc.Contains("Destroy(old)") && deskStampsSrc.Contains("GetComponents<StampSetInteractable>()") && deskStampsSrc.Contains("old.enabled = false"),
    "Level 2 replaces the old stamp-set action and stamp drags in its own scene only");
Check(deskStampsSrc.Contains("Folder.TurnToPage(0)") && folderUiSrc.Contains("public void TurnToPage(int target") && folderUiSrc.Contains("DeskMotion.TurnPage(root"),
    "using the stamps leafs the folder back to page 1 with page-turn flips");
Check(deskStampsSrc.Contains("Mathf.Clamp(right + ColumnGap, 0.7f, 0.9f)") && deskStampsSrc.Contains("ViewportToWorldPoint(new Vector3(x, y, depth))"),
    "stamps float in a column just right of the paper, inside the camera view");
Check(deskStampsSrc.Contains("size = screenWorldHeight * StampScreenHeight;") && stampDragSrc.Contains("Vector3.one * size"),
    "hand stamps have a fixed on-screen size whatever the camera");
Check(stampDragSrc.Contains("standModel.SetActive(false)") && stampDragSrc.Contains("standModel.SetActive(true)"),
    "the desk's own stamp model hides while its hand stamp is up and comes back on return");
Check(stampDragSrc.Contains("\"READY\" : \"NOT READY\"") && stampDragSrc.Contains("Part(PrimitiveType.Cube, \"Pad\"") && stampDragSrc.Contains("Part(PrimitiveType.Sphere, \"Grip\""),
    "each hand stamp is a clear 3D stamp (grip, block printed READY / NOT READY, inked pad)");
Check(stampDragSrc.Contains("ShowGhost(local)") && stampDragSrc.Contains("GhostAlpha") && stampDragSrc.Contains("Level2DeskStamps.InkTextFor(type)"),
    "while dragging over the front page a see-through ghost of the exact imprint shows where it lands");
Check(stampDragSrc.Contains("ReviewInkMotion.Mode.FadeOut") && Src("Scripts/UI/ReviewInkMark.cs").Contains("ReviewInkMotion.Play(mark, ReviewInkMotion.Mode.Pop)"),
    "a recorded stamp's ink pops in; a rejected press leaves a faint mark that fades");
Check(stampDragSrc.Contains("StartCoroutine(Thump(owner.Folder.PaperRect))") && stampDragSrc.Contains("StartCoroutine(Pulse(0.86f, 0.14f))"),
    "impact squash and paper thump");
Check(!Regex.IsMatch(stampDragSrc, @"Time\.deltaTime|new WaitForSeconds\(") && stampDragSrc.Contains("Time.unscaledDeltaTime"),
    "stamp motion runs on real time, so a pause can never freeze a stamp mid-press");
Check(Regex.IsMatch(stampDragSrc, @"try\s*\{\s*ReviewInkMark\.PopNext = true;\s*outcome = Level2PreliminaryStamp\.Stamp"),
    "an error during the decision can never leave a stamp stuck on the paper");
foreach (string file in new[] { "Scripts/Level2/FindingSlot.cs", "Scripts/UI/ReviewInkMotion.cs", "Scripts/Level2/Level2StampDrag.cs", "Scripts/Level2/Level2DeskStamps.cs" })
    Check(!Regex.IsMatch(Src(file), @"GetComponent(InChildren)?<[^>]+>\([^)]*\)\s*\?\?"), $"{Path.GetFileName(file)}: no '??' after GetComponent (Unity fake-null)");
Check(stampDragSrc.Contains("Vector3 axis = Tilted(normal);") && stampDragSrc.Contains("Vector3.Cross(normal, screenUpOnPaper)"),
    "a dragged stamp stays on the camera side of the paper (never behind the folder)");
Check(deskStampsSrc.Contains("onFrontPage = inside && Folder.CurrentPageIndex == 0"), "a stamp lands anywhere inside the front page");
Check(Regex.IsMatch(stampDragSrc, @"if \(onPaper && onFront\)\s*\{\s*Play\(Press\(local, world, normal\)\)"), "release on the front page presses; anywhere else glides back with no penalty");
Check(Regex.IsMatch(stampDragSrc, @"SlamSeconds, 0f, easeIn: true\);[\s\S]*?Level2PreliminaryStamp\.Stamp\(type, local, pressTilt\);[\s\S]*?ReboundSeconds"),
    "the decision is made once, at impact (after the press, before the rebound)");
Check(stampDragSrc.Contains("if (!lifted || busy || owner == null || !owner.Active) return false;"), "taps during a press are ignored");
Check(deskStampsSrc.Contains("PollPointer();") && deskStampsSrc.Contains("stamp.ScreenHit(view, screen, out float distance)") && !stampDragSrc.Contains("IPointerDownHandler"),
    "stamps are grabbed by reading the pointer directly and testing the stamp's drawn shape on screen (no lagging collider, nothing else can swallow the grab)");
Check(stampDragSrc.Contains("float margin = Mathf.Max(24f, Screen.height * 0.025f);"), "the grab area has a finger-friendly margin");
Check(stampDragSrc.Contains("dragTarget = world + axis * (size * (FaceOffset + AimHover));") && stampDragSrc.Contains("private const float DragTilt = 52f;"),
    "while aiming the pad points at the aim point and the body leans up the screen (its side and label show)");
Check(stampDragSrc.Contains("owner.PutAwaySoon(0.6f)") && deskStampsSrc.Contains("public void PutAway()") && Src("Scripts/Level2/PutAwayButton.cs").Contains("\"PUT STAMPS AWAY\""),
    "stamps go back on the stand after a stamp that counts, and a PUT STAMPS AWAY button does it any time");
Check(deskStampsSrc.Contains("Input.touchCount > 0") && deskStampsSrc.Contains("Input.GetMouseButtonDown(0)"), "touch first, mouse in the Editor");
Check(stampDragSrc.Contains("PlaceOnStand(seat)") && stampDragSrc.Contains("restSize = Mathf.Clamp(bounds.size.y") && stampDragSrc.Contains("Play(Travel(restPosition, restRotation, Vector3.one * restSize"),
    "the same hand stamps rest on the desk stand (original size and spot) and return there");
Check(stampDragSrc.Contains("Level2DeskStamps.DragLayer") && deskStampsSrc.Contains("public const int DragLayer = 7;") && stampDragSrc.Contains("SetGrab(false)"),
    "grab targets use the pointer layer (not the desk tap layer) and exist only while lifted");
Check(Src("Scripts/Level2/Level2PreliminaryStamp.cs").Contains("review.preliminary.hasInk = true;") &&
      Src("Scripts/Level2/Level2PreliminaryStamp.cs").Contains("if (outcome == PreliminaryReview.Outcome.Recorded && inkPoint.HasValue)"),
    "ink is kept only for a recorded stamp, at the point it landed");
Check(Regex.IsMatch(Src("Scripts/Level2/Level2PreliminaryStamp.cs"), @"Guard\(""folder redraw""[\s\S]*Guard\(""PreliminaryHoldStamped listeners"""),
    "the folder redraw happens before (and independently of) the event listeners");
Check(Src("Scripts/Level2/Level2FindingsPage.cs").Contains("if (index != 0) return;") && Src("Scripts/Level2/Level2FindingsPage.cs").Contains("ReviewInkMark.CreateAt(page.Paper"),
    "the ink is drawn on the front page where it was stamped");
Check(folderUiSrc.Contains("PageDecorator?.Invoke(index, new PageBuilder(this));") && folderUiSrc.Contains("public static System.Action<int, PageBuilder> PageDecorator;"),
    "the folder has an optional page decorator (null in Level 1)");
var inkState = Retrieved();
inkState.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May subtotal");
inkState.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralReference", "Reference");
inkState.SetFindingPart("F1", ReviewFindingState.Reason, null, "unaccounted-payment", "Not on summary");
PreliminaryReview.TryStamp(inkState, rule, true);
inkState.preliminary.hasInk = true; inkState.preliminary.inkX = -120f; inkState.preliminary.inkY = 85f; inkState.preliminary.inkAngle = -7f;
var inkRound = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(inkState, fieldJson), fieldJson);
Check(inkRound.preliminary.hasInk && inkRound.preliminary.inkX == -120f && inkRound.preliminary.inkY == 85f && inkRound.preliminary.inkAngle == -7f,
    "where the ink landed survives a JSON round trip");
Check(Src("Scripts/Level2/Level2OfficeStart.cs").Contains("EnsurePart<Level2DeskStamps>(Level2DeskStamps.ChildName)"), "desk stamping is built once under Level2Gameplay/DeskStamps");

// ================= T04 Rosa's stall, records and handover =================
var handoverIds = rosa.review.handoverDocumentIds;
Check(handoverIds.SequenceEqual(new[] { "D1", "D2", "D3", "D4", "D5", "D6" }), "Rosa hands over D1-D6");
foreach (string id in handoverIds)
{
    var page = rosa.review.FindDocument(id);
    Check(page != null && page.kind == "record" && rosa.documents.Any(dd => dd.documentName == page.documentName), $"{id} is a record page printed from a case document");
}
Check(rosa.review.documents.Where(dd => dd.kind == "record").Select(dd => dd.title).Distinct().Count() == 6, "six distinct record headings (sheet, statement, invoice, receipt, agreement, cash receipt)");
float Amount(string docName, string key)
{
    var f = rosa.documents.Single(dd => dd.documentName == docName).fields.Single(ff => ff.semanticKey == key);
    Check(CaseFolderUI.TryParseAmount(f.displayValue, out float v), $"{key} is a number");
    return v;
}
float d1Bank = Amount("Collection Sheet (D1)", "D1Bank"), d1Cash = Amount("Collection Sheet (D1)", "D1Cash"), d1Total = Amount("Collection Sheet (D1)", "D1Total");
float d2Services = Amount("Bank Statement (D2)", "D2Services"), d2Loan = Amount("Bank Statement (D2)", "D2Loan"), d2Credits = Amount("Bank Statement (D2)", "D2Credits");
Check(d1Bank == 9000f && d1Cash == 3000f && d1Total == d1Bank + d1Cash && d1Total == 12000f, "D1: 9,000 bank + 3,000 cash = 12,000");
Check(d2Services == d1Bank && d2Loan == 20000f && d2Credits == d2Services + d2Loan, "D2: BT-0905 matches D1; credits 29,000 = 9,000 + 20,000 loan");
Check(Amount("Commission Invoice (D3)", "D3Amount") == 5000f, "D3: commission 5,000");
Check(Amount("Rental Receipt (D4)", "D4Amount") == 3000f, "D4: rental 3,000");
Check(Amount("Loan Agreement (D5)", "D5Principal") == d2Loan, "D5: loan principal matches the bank release");
Check(Amount("Cash Receipt (D6)", "D6Amount") == d1Cash, "D6: the cash receipt is D1's cash line (not extra income)");
string recordText = string.Join("\n", rosa.documents.SelectMany(dd => dd.fields.Select(ff => ff.label + " " + ff.displayValue)));
foreach (string reference in new[] { "BT-0905", "RC-0531", "CM-0526", "RA-0520", "LN-0510" })
    Check(Regex.Matches(recordText, Regex.Escape(reference)).Count >= 1, "reference appears on the records: " + reference);
Check(recordText.Contains("RC-0531") && rosa.documents.Single(dd => dd.documentName == "Cash Receipt (D6)").fields.Any(ff => ff.displayValue.Contains("May collection sheet")),
    "D6 says it is included in the May collection sheet");
foreach (string leak in new[] { "income", "omitted", "Ready", "not income", "add to" })
    Check(rosa.documents.Where(dd => dd.documentName.Contains("(D")).SelectMany(dd => dd.fields).All(ff => (ff.label + " " + ff.displayValue).IndexOf(leak, StringComparison.OrdinalIgnoreCase) < 0),
        "records carry facts, no classification or answer: " + leak);

var stall = rosa.review.stall;
Check(stall.speakerName == "Aling Rosa" && stall.earlyLines.Count > 0 && stall.afterLines.Count > 0 && stall.conversationLines.Count >= 4, "Rosa has early, conversation and after lines");
string stallText = string.Join("\n", stall.earlyLines.Concat(stall.conversationLines).Concat(stall.afterLines).Select(l => l.text));
Check(stallText.Contains("referral payment") && stallText.Contains("records behind the summary"), "both information beats are asked (referral source, original records)");
Check(stallText.Contains("finished today"), "Rosa asks if it can be finished today");
foreach (string word in new[] { "fraud", "lied", "hid ", "cheat", "conceal", "compensation", "taxable", "not income" })
    Check(stallText.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0, "Rosa states no classification and accuses no one: " + word);

var hv = Retrieved();
Check(!hv.TryCollectHandover(handoverIds) && !hv.HasDocument("D1"), "no records before the initial review is stamped");
hv.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May subtotal");
hv.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralReference", "Reference");
hv.SetFindingPart("F1", ReviewFindingState.Reason, null, "unaccounted-payment", "Not on summary");
PreliminaryReview.TryStamp(hv, rule, true);
Check(!hv.TryOpenMarketDesk(), "the market desk does nothing before the handover");
Check(hv.TryCollectHandover(handoverIds) && hv.phase == ReviewPhase.Tutorial && handoverIds.All(hv.HasDocument), "the handover gives all six records once and moves to Tutorial");
Check(!hv.TryCollectHandover(handoverIds) && hv.acquiredDocumentIds.Count == 9, "talking again hands over nothing (no duplicates, no farming)");
Check(hv.HandoverDone && hv.TryOpenMarketDesk() && !hv.TryOpenMarketDesk(), "the market desk opens; MarketDeskOpened is once");
var hvRound = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(hv, fieldJson), fieldJson);
Check(hvRound.HandoverDone && hvRound.HasDocument("D6") && hvRound.marketDeskOpened, "collected records survive a JSON round trip (kept after leaving or failing)");
var badHandover = Clone();
badHandover.handoverDocumentIds.Add("S1");
Check(CaseReviewValidator.Validate(badHandover).Any(x => x.Contains("already held")), "a handover page cannot be one held at the start");
var noLines = Clone();
noLines.stall.conversationLines.Clear();
Check(CaseReviewValidator.Validate(noLines).Any(x => x.Contains("conversationLines")), "a handover needs a conversation");

string stallSrc = Src("Scripts/Level2/Level2RosaStall.cs");
string rosaSrc = Src("Scripts/Level2/RosaInteractable.cs");
string deskSrc = Src("Scripts/Level2/MarketRecordsDesk.cs");
string assetsYaml = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Levels", "Level_02", "scene_assets.asset"));
Check(assetsYaml.Contains("guid: f78a881d3ec75d945ac878527dc26674") && assetsYaml.Contains("guid: 4c701139b81d9c140b18bb741804c1ec"),
    "scene assets reference the selected Rosa (SM_Chr_Business_Female_03) and store (Building_4) prefabs");
Check(assetsYaml.Contains("guid: 0b1fbdea34f261f4eacc93aa45b49b92"), "Rosa's idle is the project's humanoid Standing Idle controller (no T-pose)");
Check(stallSrc.Contains("Instantiate(assets.storePrefab, transform)") && stallSrc.Contains("Instantiate(assets.rosaPrefab, holder.transform)"),
    "store and Rosa are instances of the originals (prefabs and source scenes untouched)");
Check(stallSrc.Contains("new Vector3(-scale, scale, scale)") && stallSrc.Contains("StoreHeight / raw.size.y"), "store keeps Building_4 (1)'s X mirror and its proportions");
Check(stallSrc.Contains("animator.runtimeAnimatorController = assets.rosaIdleController"), "Rosa's Animator gets the idle controller");
Check(stallSrc.Contains("DetectFront(") && stallSrc.Contains("FindSpot(") && stallSrc.Contains("RouteClearance") && stallSrc.Contains("Blocked("),
    "the stall is placed by measured front, free space and route clearance");
Check(Regex.IsMatch(rosaSrc, @"bool handover = review\.phase == ReviewPhase\.MarketVisit;[\s\S]*\(\) => Finish\(handover\),\s*\(\) => Finish\(false\)"),
    "records are handed over only when the required conversation completes (cancel gives nothing)");
Check(rosaSrc.Contains("review.TryCollectHandover(definition.review.handoverDocumentIds)") && rosaSrc.Contains("AwardConversation(NpcId"),
    "one commit: records, then the conversation heart through the existing ledger (once per NPC)");
Check(!rosaSrc.Contains("Score.") && !rosaSrc.Contains("TotalHearts"), "the heart goes only through AwardConversation");
Check(deskSrc.Contains("CameraController.Instance.EnterFirstPerson(viewpoint, \"Exit Desk\")") && deskSrc.Contains("GameplayEvents.RaiseMarketDeskOpened()"),
    "the records desk opens the same desk view as the office desk");
Check(Src("Scripts/Level2/Level2OfficeStart.cs").Contains("EnsurePart<Level2RosaStall>(Level2RosaStall.ChildName)"), "the stall is built once under Level2Gameplay/RosaStall");
Check(stepTriggersAll.IndexOf("event:PreliminaryHoldStamped") < stepTriggersAll.IndexOf("event:RosaRecordsCollected") &&
      stepTriggersAll.IndexOf("event:RosaRecordsCollected") < stepTriggersAll.IndexOf("event:MarketDeskOpened"),
    "objectives: initial stamp, then Rosa, then the records desk");
var guideWaypoints = Json("Tutorials/Level_02/guide.json").GetProperty("waypoints").EnumerateArray().ToList();
Check(guideWaypoints.Any(w => Str(w, "step") == "event:RosaRecordsCollected" && Str(w, "target") == "Level2Gameplay/RosaStall/Rosa"), "the Rosa waypoint activates only after the initial review step");
Check(stallSrc.Contains("\"Rosa\"") && stallSrc.Contains("\"MarketRecordsDesk\""), "runtime waypoint targets exist in the stall builder");

// ================= Review validator =================
CaseReviewDefinition Clone() => JsonSerializer.Deserialize<CaseReviewDefinition>(JsonSerializer.Serialize(rosa.review, fieldJson), fieldJson);
bool Rejects(Action<CaseReviewDefinition> edit, string expect)
{
    var copy = Clone();
    edit(copy);
    return CaseReviewValidator.Validate(copy).Any(p => p.Contains(expect));
}
Check(Rejects(r => r.documents[1].id = "COVER", "used twice"), "duplicate document ids rejected");
Check(Rejects(r => r.initialDocumentIds.Add("D9"), "does not exist"), "dangling starting page rejected");
Check(Rejects(r => r.initialDocumentIds.Add("S1"), "listed twice"), "starting page listed twice rejected");
Check(Rejects(r => r.documents[1].lines[0].amount = 13000, "add up to"), "summary income not matching its subtotal rejected");
Check(Rejects(r => r.documents[1].lines.Add(new ReviewLineDefinition { id = "S1-x", label = "x", value = "1", amount = 1, role = "subtotal" }), "exactly one subtotal"), "two subtotals rejected");
Check(Rejects(r => r.documents[2].lines[0].role = "answer", "role 'answer'"), "unknown line role rejected");
Check(Rejects(r => r.documents[0].kind = "poster", "kind 'poster'"), "unknown page kind rejected");
Check(Rejects(r => r.documents[0].lines[1].id = r.documents[0].lines[0].id, "line id"), "duplicate line ids rejected");
Check(Rejects(r => r.contentStatus = "maybe", "contentStatus"), "unknown content status rejected");
Check(Rejects(r => r.startPhase = "Lunch", "startPhase"), "unknown start phase rejected");
Check(Rejects(r => r.documents[2].lines.Add(new ReviewLineDefinition { id = "N1-t", label = "t", value = "t", role = "subtotal" }), "only summary"), "subtotal outside a summary rejected");
Check(CaseReviewValidator.Validate(null).Count == 0 && CaseReviewValidator.Validate(new CaseReviewDefinition()).Count == 0, "ordinary cases have no review problems");

// ================= Level 1 defaults =================
CaseDefinition marie = JsonSerializer.Deserialize<CaseDefinition>(File.ReadAllText(Res("Levels/Level_01/cases/case_001.json")), fieldJson);
Check(marie != null && !marie.HasReview && CaseReviewState.Seed(marie.review) == null, "Level 1 case has no review data or state");
var rules1 = Json("Levels/Level_01/rewards.json");
Check(rules1.GetProperty("requireSideQuestBeforeOffice").GetBoolean(), "Level 1 still requires its side quest before the office");
Check(!Directory.Exists(Res("Levels/Level_01/book.json")) && !File.Exists(Res("Levels/Level_01/book.json")), "Level 1 has no extra book leaves");

// ================= Level_02 rewards =================
var rules2 = Json("Levels/Level_02/rewards.json");
Check(!rules2.GetProperty("requireSideQuestBeforeOffice").GetBoolean() && rules2.GetProperty("officeDoors").GetArrayLength() == 0,
    "Level 2 office is open from the start (no side-quest gate, no shut doors)");
var sq = rules2.GetProperty("sideQuest");
var cf = rules2.GetProperty("caseFile");
var conv = rules2.GetProperty("conversation");
int maxExp = sq.GetProperty("exp").GetInt32() + cf.GetProperty("exp").GetInt32();
int maxHearts = conv.GetProperty("hearts").GetInt32() * conv.GetProperty("count").GetInt32() + sq.GetProperty("hearts").GetInt32() + cf.GetProperty("hearts").GetInt32();
Check(maxExp == 160 && maxHearts == 9, $"Level 2 maxima are 160 EXP and 9 hearts (got {maxExp}, {maxHearts})");
Check(Str(sq, "badgeId") == "mark_the_money_2" && Str(sq, "badgeName") == "Mark the Money!" && Str(sq, "badgeEmblem") == "receipt",
    "Mark the Money! badge identity (receipt emblem)");
Check(rules2.GetProperty("passingPercent").GetInt32() == 80, "Level 2 keeps the 80% pass line");

// ================= Level_02 objectives and guide =================
var objectives = Json("Levels/Level_02/objectives.json");
var steps = objectives.GetProperty("objectives").EnumerateArray().SelectMany(o => o.GetProperty("steps").EnumerateArray()).ToList();
var stepTriggers = steps.Select(s => ObjectiveTriggers.Normalize(Str(s, "trigger"))).ToList();
foreach (string trigger in stepTriggers)
    Check(ObjectiveTriggers.Validate(trigger) == null, $"objective trigger valid: {trigger} ({ObjectiveTriggers.Validate(trigger)})");
Check(stepTriggers.Take(3).SequenceEqual(new[] { "event:ReceptionistTalkedTo", "event:CaseFolderRetrieved", "state:ReviewDocumentsState" }),
    "objectives lead to the receptionist, the cabinet, then opening the folder at the desk (tap or drag)");
foreach (string forbidden in new[] { "Profile", "SideQuest", "TutorialGuide", "area:" })
    Check(!stepTriggers.Any(t => t.Contains(forbidden)), "no Level 1 plaza/corkboard/office-gate step: " + forbidden);
Check(Array.IndexOf(ObjectiveTriggers.Events, ObjectiveTriggers.CaseFolderRetrieved) >= 0, "CaseFolderRetrieved is a known objective event");
Check(GuideTriggers.TryParse("event:CaseFolderRetrieved", out _, out _), "the guide understands event:CaseFolderRetrieved");

var guide = Json("Tutorials/Level_02/guide.json");
Check(guide.GetProperty("enabled").GetBoolean() && Str(guide, "platform") == "touch", "Level 2 guide is on and written for touch");
var waypointSteps = new HashSet<string>();
foreach (var w in guide.GetProperty("waypoints").EnumerateArray())
{
    string step = ObjectiveTriggers.Normalize(Str(w, "step"));
    waypointSteps.Add(step);
    Check(stepTriggers.Contains(step), "guide waypoint matches an objective step: " + step);
    foreach (string target in Str(w, "target").Split('|'))
        Check(!target.Contains("Plaza/NPCS"), "no waypoint leads to a hidden Level 1 plaza client: " + target);
}
foreach (string step in stepTriggers) Check(waypointSteps.Contains(step), "objective step has arrows: " + step);
var lessonIds = guide.GetProperty("lessons").EnumerateArray().Select(l => Str(l, "id")).ToList();
Check(lessonIds.Distinct().Count() == lessonIds.Count, "guide lesson ids are unique");
foreach (var lesson in guide.GetProperty("lessons").EnumerateArray())
{
    string id = Str(lesson, "id");
    Check(GuideTriggers.Validate(Str(lesson, "when"), false) == null, $"{id}: when is valid");
    if (Str(lesson, "after") != null) Check(lessonIds.Contains(Str(lesson, "after")), $"{id}: after exists");
    foreach (var page in lesson.GetProperty("pages").EnumerateArray())
    {
        if (Str(page, "point") != null) Check(GuideTargets.Validate(Str(page, "point")) == null, $"{id}: point target valid");
        if (Str(page, "waitFor") != null) Check(GuideTriggers.Validate(Str(page, "waitFor"), true) == null, $"{id}: waitFor valid");
    }
}
Check(guide.GetProperty("lessons").EnumerateArray().SelectMany(l => l.GetProperty("pages").EnumerateArray()).Any(p => p.TryGetProperty("revealArrows", out var r) && r.GetBoolean()),
    "the guide reveals the objective arrows");

// ================= Book leaves and office start =================
var book = Json("Levels/Level_02/book.json");
var leaves = book.GetProperty("sections").EnumerateArray().ToList();
Check(leaves.Count == 3 && leaves.All(l => !string.IsNullOrWhiteSpace(Str(l, "heading")) && !string.IsNullOrWhiteSpace(Str(l, "body"))),
    "three Level 2 book leaves with heading and body");
string bookText = string.Join("\n", leaves.Select(l => Str(l, "heading") + " " + Str(l, "body")));
foreach (string leak in new[] { "Rosa", "12,000", "5,000", "3,000", "15,000", "20,000", "CM-0526", "LN-0510", "F1", "F2", "Ready for Filing" })
    Check(!bookText.Contains(leak), "book leaves explain concepts only, no case value: " + leak);
Check(bookText.Contains("educational"), "the loan rule is labelled an educational simplification");

var start = Json("Levels/Level_02/office_start.json");
var hide = start.GetProperty("hide").EnumerateArray().Select(h => h.GetString()).ToList();
Check(hide.Contains("Environment/Outside/Plaza/NPCS") && hide.Contains("Environment/Outside/Plaza/CorkboardSideQuestRoot"),
    "office start switches off the Level 1 plaza clients and corkboard side quest");
Check(start.GetProperty("disableComponents").EnumerateArray().Any(c => c.GetString() == "TutorialController"), "the old Level 1 popup tutorial is switched off");
Check(typeof(OfficeLevelCatalog).Assembly.GetType("TutorialController") != null, "TutorialController is a real component type");
Check(Str(start, "spawn").Contains("Reception") && Str(start, "face").EndsWith("Receptionist"), "the player starts in the reception facing the receptionist");

// Player-facing Level 2 text is touch-only.
string playerText = string.Join("\n", new[] { File.ReadAllText(Res("Tutorials/Level_02/guide.json")), bookText,
    File.ReadAllText(Res("Levels/Level_02/objectives.json")), string.Join("\n", rosa.receptionistGreeting.Select(l => l.text)) });
foreach (string word in new[] { "click", "mouse", "WASD", "keyboard" })
    Check(playerText.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0, "no desktop wording: " + word);

// ================= Source wiring =================
foreach (var (file, feature) in new[] { ("Scripts/UI/HUD/OfficeHudRuntime.cs", "Hud"), ("Scripts/UI/OfficeMinimap.cs", "Minimap"),
                                        ("Scripts/UI/Guide/GuideRuntime.cs", "Guide"), ("Scripts/UI/HUD/SceneFade.cs", "Fade") })
{
    string text = Src(file);
    Check(text.Contains($"OfficeLevelProfile.Allows(scene, OfficeFeature.{feature})") && !text.Contains("\"Office_Level1\""),
        $"{Path.GetFileName(file)} builds through the level profile ({feature})");
}
foreach (string file in new[] { "Scripts/UI/HUD/CaseFinale.cs", "Scripts/UI/HUD/OfficeHours.cs", "Scripts/UI/HUD/ConsequenceReactions.cs",
                                "Scripts/SideQuest1/OfficeDoorGate.cs", "Scripts/SideQuest1/SideQuestSplash.cs" })
    Check(Src(file).Contains("TargetSceneName = \"Office_Level1\""), $"{Path.GetFileName(file)} stays Level 1 only until its Level 2 ticket");
Check(Src("Scripts/UI/HUD/OfficeHudRuntime.cs").Contains("OfficeObjectives.Create(gameObject, OfficeLevelProfile.LevelIdFor(this))"), "HUD objectives load the scene's level");
Check(Src("Scripts/UI/Guide/GuideRuntime.cs").Contains("string levelId = OfficeLevelProfile.LevelIdFor(this);"), "guide loads the scene's level");
Check(Regex.IsMatch(Src("Scripts/Data/CaseProgressionManager.cs"), @"OfficeLevelProfile\.LevelIdFor\(gameObject\.scene\)[\s\S]{0,200}StartLevel\(string\.IsNullOrEmpty\(sceneLevelId\) \? startingLevelId : sceneLevelId\)"),
    "the copied CaseProgressionManager starts the scene's level, not its serialized Level_01");
Check(Src("Scripts/UI/TaxCodeBookUI.cs").Contains("LevelBookLeaves.Load(OfficeLevelProfile.LevelIdFor(this))"), "the book appends the level's leaves");
Check(!Regex.IsMatch(Src("Scripts/Data/CaseData.cs"), @"ResetForRetry\(\)[\s\S]*?review\s*=") , "an audit retry never clears the review state");
Check(Src("Scripts/Training/PracticeModeRuntime.cs").Contains("PlayerPlacement.PlaceBetween(gameObject.scene, drill.spawn, drill.face)"),
    "Tutorial Hub drills keep the same placement (shared PlayerPlacement)");

Console.WriteLine(fails == 0 ? $"Level2Regression: {checks} checks PASS" : $"Level2Regression: {fails} of {checks} checks FAILED");
return fails == 0 ? 0 : 1;
