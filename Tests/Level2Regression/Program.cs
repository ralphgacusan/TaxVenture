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
Check(l2Problems == null && level2Features.SetEquals(new[] { OfficeFeature.Hud, OfficeFeature.Minimap, OfficeFeature.Guide, OfficeFeature.Fade, OfficeFeature.Finale }),
    "Office_Level2 opts into HUD, minimap, guide, fade and finale");
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
Check(s1 != null && !s1.lines.Any(l => l.label.IndexOf("blanket", StringComparison.OrdinalIgnoreCase) >= 0), "S1 does not list the separate blanket-washing payment");
Check(s1 != null && (s1.footer ?? "").Contains("Taxpayer Assistance Desk"), "S1 footer names the Assistance Desk as preparer");
var n1 = rosa.review.FindDocument("N1");
Check(n1 != null && n1.lines.Any(l => l.value.Contains("CM-0526")), "N1 identifies the paid-job receipt CM-0526");
Check(rosa.documents.Count == 9 && rosa.review.initialDocumentIds.Count == 3 && rosa.documents.All(d => d.fields.All(f => !string.IsNullOrEmpty(f.semanticKey))),
    "nine case documents (3 starting pages + 6 records), only 3 held at the start, every field keyed");
Check(rosa.potentialIssues == null || rosa.potentialIssues.Count == 0, "no potential issue is listed in the folder (it would show the answer)");
Check(rosa.clientFinale != null && rosa.clientFinale.enabled && rosa.clientFinale.modelPath == "Level2Gameplay/RosaStall/Rosa" &&
      new[] { rosa.clientFinale.great, rosa.clientFinale.good, rosa.clientFinale.okay }.All(l => l.Count >= 2 && l.Last().Contains("Taxpayer Assistance Desk prepared that first summary")),
    "Rosa's ending (T11): her stall model, lines for each tier, each ending with the neutral Desk confirmation");
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
Check(folderUi.Contains("AddReviewPages(list, data);") && folderUi.Contains("data.review.HasDocument(document.id)"),
    "the compact prepared packet only exposes held source documents");
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
Check(CaseFolderUI.TryParseAmount("\u00e2\u201a\u00b112,000", out float a12) && a12 == 12000f && !CaseFolderUI.TryParseAmount("none", out _), "amounts parse from printed money");
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
Check(handoffSrc.Contains("if (Awaiting && !AtOfficeDesk())") &&
      handoffSrc.Contains("if (!AtOfficeDesk())") && handoffSrc.Contains("if (!IsOnDesk(screenPosition))"),
    "the first folder opening requires the office desk, with a desk-surface gate for drops");
Check(handoffSrc.Contains("bool handled = previousClick != null && previousClick();"),
    "tapping uses the same centred toggle as Level 1");
Check(!handoffSrc.Contains("private void PlaceOnDesk(") && handoffSrc.Contains("if (view != null && view.IsOpen) Level2OfficeStart.Review.TryPlaceFolderOnDesk();"),
    "only a successful shared view opening records first placement, retaining stored ownership");
Check(handoffSrc.Contains("bool handled = previousDrop != null && previousDrop(screenPosition);"),
    "dropping uses Level 1's animated view at the drop position");
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
Check(rosa.review.reasons.Count == 3 && rosa.review.reasons.All(r => r.label.Length <= 24) && rosa.review.FindReason("paid-another-way") == null,
    "three concise book principles that fit the finding fields (the retired bank/cash reason is gone)");
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
Check(!early.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total"), "no finding before the folder is retrieved");
Check(PreliminaryReview.TryStamp(early, rule, true) == PreliminaryReview.Outcome.NotRetrieved && early.preliminary == null, "no stamp before retrieval");

var pr = Retrieved();
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.FindingIncomplete, "an empty finding cannot be stamped");
Check(pr.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total") && pr.packetRevision == 1, "setting the concern is a packet revision");
Check(!pr.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total") && pr.packetRevision == 1, "the same value again is not a revision");
Check(pr.phase == ReviewPhase.PreliminaryReview, "an incomplete finding keeps the initial review open");
pr.SetFindingPart("F1", ReviewFindingState.Evidence, "COVER", "FullName", "Client");
pr.SetFindingPart("F1", ReviewFindingState.Reason, null, "amounts-differ", "Amounts differ");
Check(pr.FindFinding("F1").IsComplete && pr.phase == ReviewPhase.PreliminaryStamp && pr.FindFinding("F1").status == "Needs checking",
    "a complete finding moves to PreliminaryStamp with status Needs checking");
Check(PreliminaryReview.TryStamp(pr, rule, false) == PreliminaryReview.Outcome.ReadyWhileOpen && pr.preliminary == null, "Ready with an open concern is guided, not recorded");
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.WrongEvidence && pr.preliminary == null, "the wrong related record is guided, not recorded");
pr.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralNote", "Payment for blanket washing");
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.WrongReason && pr.preliminary == null, "the wrong reason is guided, not recorded");
pr.SetFindingPart("F1", ReviewFindingState.Concern, "COVER", "FullName", "Client");
pr.SetFindingPart("F1", ReviewFindingState.Reason, null, "unaccounted-payment", "Not on summary");
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.WrongConcern, "a concern outside the summary is guided, not recorded");
pr.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total");
Check(PreliminaryReview.TryStamp(pr, rule, true) == PreliminaryReview.Outcome.FindingUnchecked,
    "a stamp cannot bypass draft validation");
Check(FindingComparison.Check(pr, rule) == FindingComparison.Result.Supported && pr.FindFinding("F1").comparisonConfirmed,
    "checking a complete draft confirms a supported link");
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
Check(Regex.IsMatch(Src("Scripts/Level2/Level2PreliminaryStamp.cs"), @"phase >= ReviewPhase\.OfficeReconciliation\) return false"), "the old Level 1 stamp path stays off once the office review begins");
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
Check(Src("Scripts/UI/HUD/Doc3DLayer.cs").Contains("[DefaultExecutionOrder(500)]") &&
      Src("Scripts/UI/HUD/Doc3DLayer.cs").Contains("image.rectTransform.anchorMin = main.rect.min"),
    "the tool render captures after movement and displays inside the viewing camera viewport");
Check(stampDragSrc.Contains("Quaternion hover = Tilted(normal, inkUp, HoverTilt)") &&
      stampDragSrc.Contains("RootForPad(pad, hover, stampSize)") && stampDragSrc.Contains("inkUp * (inkHeight * HoverBack)"),
    "aiming hovers a readable leaned-back model just past the ghost, so the imprint stays visible");
Check(stampDragSrc.Contains("grabOffset") && stampDragSrc.Contains("dragMoved") &&
      !stampDragSrc.Contains("dragTarget = world + axis"),
    "grabbing preserves the visible stamp offset and aiming keeps its screen-depth pose");
Check(stampDragSrc.Contains("HighlightEffect.For(stamp)") && stampDragSrc.Contains("localBounds") &&
      stampDragSrc.Contains("AssignSortingOrder(FloatingWindow.TakeTopSortingOrder())"),
    "stamp picking uses model geometry with shared highlight and document focus");
Check(Src("Scripts/Level2/Level2FindingsSheet.cs").Contains("observation.enableAutoSizing = false;") &&
      Src("Scripts/Level2/Level2FindingsSheet.cs").Contains("475f, 198f, 28f"),
    "the handwritten finding has extra paper space without reducing the font size");
string folderUiSrc = Src("Scripts/UI/CaseFolderUI.cs");
Check(deskStampsSrc.Contains("Destroy(old)") && deskStampsSrc.Contains("GetComponents<StampSetInteractable>()") && deskStampsSrc.Contains("old.enabled = false"),
    "Level 2 replaces the old stamp-set action and stamp drags in its own scene only");
Check(deskStampsSrc.Contains("Folder.TurnToPage(StampPageIndex(), () =>") && folderUiSrc.Contains("public void TurnToPage(int target") && folderUiSrc.Contains("DeskMotion.TurnPage(root"),
    "using the stamps leafs the folder to the stamp page (page 1, or Final Assessment) with page-turn flips");
Check(deskStampsSrc.Contains("Mathf.Clamp(right + ColumnGap, 0.08f, 0.9f)") && deskStampsSrc.Contains("float centre = (bottom + top) * 0.5f") &&
      stampDragSrc.Contains("if (lifted && owner != null) owner.TryFloatPose(slot, out home, out homeUp, out size)") &&
      deskStampsSrc.Contains("ViewportToWorldPoint(new Vector3(x, y, depth))"),
    "stamps track both horizontal and vertical folder movement while remaining inside the camera view");
Check(deskStampsSrc.Contains("Folder.Closing += PutAway") && deskStampsSrc.Contains("Folder.Closing -= PutAway") &&
      Src("Scripts/UI/HUD/CaseFolder3DView.cs").Contains("pages?.NotifyClosing()"),
    "folder closing puts stamps away immediately and the opening flow never activates them");
Check(deskStampsSrc.Contains("size = screenWorldHeight * StampScreenHeight;") && stampDragSrc.Contains("Travel(home, Facing(homeUp), size"),
    "hand stamps have a fixed on-screen size whatever the camera");
Check(stampDragSrc.Contains("standModel.SetActive(false)") && stampDragSrc.Contains("standModel.SetActive(true)"),
    "the desk's own stamp model hides while its hand stamp is up and comes back on return");
Check(stampDragSrc.Contains("\"READY\" : \"NOT READY\"") && stampDragSrc.Contains("Part(PrimitiveType.Cube, \"Pad\"") && stampDragSrc.Contains("Part(PrimitiveType.Sphere, \"Grip\""),
    "each hand stamp is a clear 3D stamp (grip, block printed READY / NOT READY, inked pad)");
Check(stampDragSrc.Contains("ShowGhost(aimLocal)") && stampDragSrc.Contains("GhostAlpha") && stampDragSrc.Contains("PulseGhost()") && stampDragSrc.Contains("Level2DeskStamps.InkTextFor(type)"),
    "while dragging over the front page a see-through ghost of the exact imprint shows where it lands");
Check(stampDragSrc.Contains("ReviewInkMotion.Mode.FadeOut") && Src("Scripts/UI/ReviewInkMark.cs").Contains("ReviewInkMotion.Play(mark, ReviewInkMotion.Mode.Pop)"),
    "a recorded stamp's ink pops in; a rejected press leaves a faint mark that fades");
Check(stampDragSrc.Contains("StartCoroutine(Thump(paper))") && stampDragSrc.Contains("StartCoroutine(InkBurst(paper, local, pressTilt") &&
      stampDragSrc.Contains("body.localScale = new Vector3(1f + 0.12f * wave, 1f - 0.24f * wave"),
    "impact: squash-and-spring about the pad, paper thump and an ink ring burst");
Check(!Regex.IsMatch(stampDragSrc, @"Time\.deltaTime|new WaitForSeconds\(") && stampDragSrc.Contains("Time.unscaledDeltaTime"),
    "stamp motion runs on real time, so a pause can never freeze a stamp mid-press");
Check(Regex.IsMatch(stampDragSrc, @"try\s*\{\s*ReviewInkMark\.PopNext = true;\s*outcome = Level2PreliminaryStamp\.Stamp"),
    "an error during the decision can never leave a stamp stuck on the paper");
foreach (string file in new[] { "Scripts/Level2/FindingSlot.cs", "Scripts/UI/ReviewInkMotion.cs", "Scripts/Level2/Level2StampDrag.cs", "Scripts/Level2/Level2DeskStamps.cs" })
    Check(!Regex.IsMatch(Src(file), @"GetComponent(InChildren)?<[^>]+>\([^)]*\)\s*\?\?"), $"{Path.GetFileName(file)}: no '??' after GetComponent (Unity fake-null)");
Check(stampDragSrc.Contains("dragScreen + grabOffset") && stampDragSrc.Contains("SetModelLayer(Level2DeskStamps.StampLayer)") &&
      deskStampsSrc.Contains("stampCamera.cullingMask = 1 << StampLayer") && deskStampsSrc.Contains("main.cullingMask &= ~(1 << StampLayer)"),
    "a lifted or dragged stamp is drawn by the overlay stamp camera, never behind the folder");
Check(deskStampsSrc.Contains("onFrontPage = inside && Folder.CurrentPageIndex == StampPageIndex()"), "a stamp lands anywhere inside its stamp page");
Check(Regex.IsMatch(stampDragSrc, @"if \(onFront\)\s*\{\s*Play\(Press\(aimLocal\)\)"), "release on the front page presses where the ghost was; anywhere else glides back with no penalty");
Check(Regex.IsMatch(stampDragSrc, @"/ SlamSeconds\)[\s\S]*?Level2PreliminaryStamp\.Stamp\(type, local, pressTilt\);[\s\S]*?/ LiftOffSeconds\)"),
    "the decision is made once, at impact (after the press, before the rebound)");
Check(stampDragSrc.Contains("if (!lifted || busy || owner == null || !owner.Active) return false;"), "taps during a press are ignored");
Check(deskStampsSrc.Contains("PollPointer();") && deskStampsSrc.Contains("stamp.ScreenHit(view, screen, out float distance, includeStand)") && !stampDragSrc.Contains("IPointerDownHandler"),
    "stamps are grabbed by reading the pointer directly and testing the stamp's drawn shape on screen (no lagging collider, nothing else can swallow the grab)");
Check(stampDragSrc.Contains("Mathf.Max(24f, Screen.height * 0.025f)"), "the grab area has a finger-friendly margin");
Check(stampDragSrc.Contains("Quaternion rotation = Facing(homeUp);") && stampDragSrc.Contains("scale = Mathf.Lerp(scale, stampSize, aimT)"),
    "free in hand the stamp stays upright at its floating size; aiming blends to the imprint-sized pose");
Check(stampDragSrc.Contains("owner.PutAwaySoon(0.6f)") && deskStampsSrc.Contains("public void PutAway()") && Src("Scripts/Level2/PutAwayButton.cs").Contains("\"PUT STAMPS AWAY\""),
    "stamps go back on the stand after a stamp that counts, and a PUT STAMPS AWAY button does it any time");
Check(deskStampsSrc.Contains("Input.touchCount > 0") && deskStampsSrc.Contains("Input.GetMouseButtonDown(0)"), "touch first, mouse in the Editor");
Check(stampDragSrc.Contains("PlaceOnStand(seat)") && stampDragSrc.Contains("restSize = Mathf.Clamp(bounds.size.y") && stampDragSrc.Contains("Travel(restPosition, restRotation, restSize"),
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
inkState.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total");
inkState.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralNote", "Payment for blanket washing");
inkState.SetFindingPart("F1", ReviewFindingState.Reason, null, "unaccounted-payment", "Not on summary");
FindingComparison.Check(inkState, rule);
PreliminaryReview.TryStamp(inkState, rule, true);
inkState.preliminary.hasInk = true; inkState.preliminary.inkX = -120f; inkState.preliminary.inkY = 85f; inkState.preliminary.inkAngle = -7f;
var inkRound = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(inkState, fieldJson), fieldJson);
Check(inkRound.preliminary.hasInk && inkRound.preliminary.inkX == -120f && inkRound.preliminary.inkY == 85f && inkRound.preliminary.inkAngle == -7f,
    "where the ink landed survives a JSON round trip");
Check(Src("Scripts/Level2/Level2OfficeStart.cs").Contains("EnsurePart<Level2DeskStamps>(Level2DeskStamps.ChildName)"), "desk stamping is built once under Level2Gameplay/DeskStamps");

// ================= T04 Rosa's stall, records and handover =================
var handoverIds = rosa.review.handoverDocumentIds;
Check(handoverIds.SequenceEqual(new[] { "D1", "D3", "D4", "D5" }), "Rosa hands over four supporting records (two listed, one missing payment, one clarifier)");
foreach (string id in handoverIds)
{
    var page = rosa.review.FindDocument(id);
    Check(page != null && page.kind == "record" && rosa.documents.Any(dd => dd.documentName == page.documentName), $"{id} is a record page printed from a case document");
}
Check(handoverIds.Select(id => rosa.review.FindDocument(id).title).Distinct().Count() == 4, "four distinct record headings (collection sheet, job receipt, rental receipt, loan agreement)");
float Amount(string docName, string key)
{
    var f = rosa.documents.Single(dd => dd.documentName == docName).fields.Single(ff => ff.semanticKey == key);
    Check(CaseFolderUI.TryParseAmount(f.displayValue, out float v), $"{key} is a number");
    return v;
}
float d1Total = Amount("Collection Sheet (D1)", "D1Total");
Check(d1Total == 12000f && d1Total == Amount("Prepared May Income Summary (S1)", "SummaryServices"), "D1: 12,000 of clothes washing, the summary's first listed line");
Check(Amount("Rental Receipt (D4)", "D4Amount") == 3000f && Amount("Prepared May Income Summary (S1)", "SummaryRental") == 3000f, "D4: rental 3,000, the summary's second listed line");
Check(Amount("Laundry Receipt (D3)", "D3Amount") == 5000f, "D3: separate blanket-washing job 5,000 (not on the summary)");
Check(Amount("Loan Agreement (D5)", "D5Principal") == 20000f && rosa.documents.Single(dd => dd.documentName == "Prepared May Income Summary (S1)").fields.Any(ff => ff.displayValue.Contains("LN-0510")),
    "D5: the 20,000 loan principal explains the summary's loan memo");
Check(Amount("Prepared May Income Summary (S1)", "SummarySubtotal") == d1Total + 3000f, "S1: May total 15,000 = the two listed lines");
Check(handoverIds.All(id => rosa.documents.Single(dd => dd.documentName == rosa.review.FindDocument(id).documentName).fields.Count <= 4),
    "each handed-over record prints at most four large rows");
string recordText = string.Join("\n", handoverIds.SelectMany(id => rosa.documents.Single(dd => dd.documentName == rosa.review.FindDocument(id).documentName).fields.Select(ff => ff.label + " " + ff.displayValue)));
foreach (string reference in new[] { "CM-0526", "RA-0520", "LN-0510" })
    Check(Regex.Matches(recordText, Regex.Escape(reference)).Count >= 1, "reference appears on the records: " + reference);
Check(rosa.review.documents.Single(dd => dd.id == "S1").lines.Select(l => l.label).Take(2).SequenceEqual(
        rosa.documents.Single(dd => dd.documentName == "Prepared May Income Summary (S1)").fields.Select(ff => ff.label).Take(2)),
    "the summary page and its printed fields name the same two lines");
foreach (string leak in new[] { "income", "omitted", "Ready", "not income", "add to" })
    Check(rosa.documents.Where(dd => dd.documentName.Contains("(D")).SelectMany(dd => dd.fields).All(ff => (ff.label + " " + ff.displayValue).IndexOf(leak, StringComparison.OrdinalIgnoreCase) < 0),
        "records carry facts, no classification or answer: " + leak);

var stall = rosa.review.stall;
Check(stall.speakerName == "Aling Rosa" && stall.earlyLines.Count > 0 && stall.afterLines.Count > 0 && stall.conversationLines.Count >= 4, "Rosa has early, conversation and after lines");
string stallText = string.Join("\n", stall.earlyLines.Concat(stall.conversationLines).Concat(stall.afterLines).Select(l => l.text));
Check(stallText.Contains("Which job was this payment for?") && stallText.Contains("records behind the summary"), "both information beats are asked (paid job, original records)");
Check(stallText.Contains("finished today"), "Rosa asks if it can be finished today");
foreach (string word in new[] { "fraud", "lied", "hid ", "cheat", "conceal", "compensation", "taxable", "not income" })
    Check(stallText.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0, "Rosa states no classification and accuses no one: " + word);

var hv = Retrieved();
Check(!hv.TryCollectHandover(handoverIds) && !hv.HasDocument("D1"), "no records before the initial review is stamped");
hv.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total");
hv.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralNote", "Payment for blanket washing");
hv.SetFindingPart("F1", ReviewFindingState.Reason, null, "unaccounted-payment", "Not on summary");
FindingComparison.Check(hv, rule);
PreliminaryReview.TryStamp(hv, rule, true);
Check(!hv.TryOpenMarketDesk(), "the market desk does nothing before the handover");
Check(hv.TryCollectHandover(handoverIds) && hv.phase == ReviewPhase.Tutorial && handoverIds.All(hv.HasDocument), "the handover gives all five records once and moves to Tutorial");
Check(!hv.TryCollectHandover(handoverIds) && hv.acquiredDocumentIds.Count == 7, "talking again hands over nothing (no duplicates, no farming)");
Check(hv.HandoverDone && hv.TryOpenMarketDesk() && !hv.TryOpenMarketDesk(), "the market desk opens; MarketDeskOpened is once");
var hvRound = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(hv, fieldJson), fieldJson);
Check(hvRound.HandoverDone && hvRound.HasDocument("D5") && hvRound.marketDeskOpened, "collected records survive a JSON round trip (kept after leaving or failing)");
var badHandover = Clone();
badHandover.handoverDocumentIds.Add("S1");
Check(CaseReviewValidator.Validate(badHandover).Any(x => x.Contains("already held")), "a handover page cannot be one held at the start");
var noLines = Clone();
noLines.stall.conversationLines.Clear();
Check(CaseReviewValidator.Validate(noLines).Any(x => x.Contains("conversationLines")), "a handover needs a conversation");

// ================= T05 income toolkit tutorial =================
var swipe = new HighlighterSweep();
swipe.Begin(-0.1f);
swipe.Move(-0.09f, 0.324f);
Check(!swipe.Complete, "highlighter: a tap or tiny move never commits ink");
swipe.Move(0.05f, 0.324f);
Check(swipe.Complete, "highlighter: a deliberate partial-row swipe finishes without requiring the exact far edge");
float covered = swipe.Coverage;
swipe.Move(-0.08f, 0.324f);
Check(swipe.Coverage == covered, "highlighter: moving back over the same section retains the ink");
swipe.Begin(0.1f);
swipe.Move(-0.05f, 0.324f);
Check(swipe.Complete && swipe.End < swipe.Start, "highlighter: right-to-left gestures work like left-to-right gestures");
swipe.Begin(0f);
Check(!swipe.Complete && swipe.Coverage == 0f, "highlighter: a new stroke starts clean after an outside drop");
swipe.Move(1f, 0f);
Check(!swipe.Complete, "highlighter: invalid line widths cannot commit a stroke");
swipe.Move(1f, 0.324f);
Check(swipe.Coverage == 1f, "highlighter: a fast full sweep is bounded and does not depend on frame count");
swipe.Begin(0f);
swipe.Move(0.2f, 1f);
swipe.Move(-0.25f, 1f);
Check(Math.Abs(swipe.Coverage - 0.45f) < 0.0001f && swipe.Min == -0.25f && swipe.Max == 0.2f && swipe.Reverse,
    "highlighter: reversing across the starting point retains both sides of the ink and completes a natural back-and-forth sweep");
swipe.Begin(0f);
Check(!swipe.CanFinish(false, false) && !swipe.Aim(" ", 0f), "highlighter validation: an empty or unselected row cannot finish");
Check(swipe.Aim("employer", 0f), "highlighter: a row can be aimed before the stroke locks");
swipe.Move(0.12f, 0.324f, allowLock: false);
Check(!swipe.Locked && !swipe.CanFinish(false, false), "highlighter: traveling diagonally from the pen does not commit an incidental row");
Check(swipe.Aim("salary", -0.1f) && swipe.Coverage == 0f, "highlighter: the intended row can replace a row crossed during pickup");
swipe.Move(-0.075f, 0.324f);
Check(swipe.Locked && !swipe.Complete, "highlighter: a short deliberate sweep locks before completion");
Check(!swipe.Aim("status", 0f) && swipe.LineId == "salary", "highlighter: drifting onto another row cannot switch a locked stroke");
swipe.Move(-0.025f, 0.324f);
Check(swipe.CanFinish(false, false), "highlighter: a forgiving 23% swipe finishes the locked row without requiring the far edge");
Check(!swipe.CanFinish(true, false) && !swipe.CanFinish(false, true), "highlighter validation: UI releases and canceled gestures never commit");
var annotation = rosa.review.annotation;
var t0 = annotation.tutorial;
foreach (var option in annotation.typeOptions.Concat(annotation.treatmentOptions).Concat(t0.evidenceOptions))
{
    string caption = IncomeToolkit.CardCaption(option.id, option.label);
    Check(Regex.Replace(caption, @"\s+", " ") == Regex.Replace(option.label, @"\s+", " "),
        "card captions retain the complete label: " + option.id);
    Check(caption.Split('\n').Length == 2, "card captions use the same two-line layout: " + option.id);
}
var practiceFeedback = new AnnotationEntryState { entryId = t0.id };
Check(AnnotationActions.PracticeHint(practiceFeedback, annotation, t0).Contains("salary"), "practice guidance names the missing highlight");
practiceFeedback.highlightLineId = t0.demo.highlightLineId;
practiceFeedback.typeId = "business-service";
practiceFeedback.treatmentId = t0.demo.treatmentId;
practiceFeedback.evidenceIds.AddRange(t0.demo.evidenceIds);
Check(practiceFeedback.IsComplete && !AnnotationActions.MatchesDemonstration(practiceFeedback, t0) &&
      AnnotationActions.PracticeHint(practiceFeedback, annotation, t0).Contains("Compensation income"),
    "a full but incorrect practice sheet identifies the TYPE correction instead of appearing stuck");
practiceFeedback.typeId = t0.demo.typeId;
practiceFeedback.treatmentId = "not-income";
Check(AnnotationActions.PracticeHint(practiceFeedback, annotation, t0).StartsWith("TREATMENT:"), "practice guidance identifies the incorrect treatment");
practiceFeedback.treatmentId = t0.demo.treatmentId;
practiceFeedback.evidenceIds.Clear();
practiceFeedback.evidenceIds.Add("company-id");
Check(AnnotationActions.PracticeHint(practiceFeedback, annotation, t0).Contains("Payslip PS-0430"), "practice guidance names the payslip required as evidence");
practiceFeedback.evidenceIds.Clear();
practiceFeedback.evidenceIds.AddRange(t0.demo.evidenceIds);
Check(AnnotationActions.MatchesDemonstration(practiceFeedback, t0) && AnnotationActions.PracticeHint(practiceFeedback, annotation, t0) == "Practice complete.",
    "the demonstrated highlight, TYPE, TREATMENT and evidence produce an explicit complete result");
foreach (var column in new[] {
    (kind: AnnotationSlot.Type, count: annotation.typeOptions.Count),
    (kind: AnnotationSlot.Treatment, count: annotation.treatmentOptions.Count),
    (kind: AnnotationSlot.Evidence, count: t0.evidenceOptions.Count) })
{
    double angle = (column.kind == AnnotationSlot.Evidence ? 4d : 3d) * Math.PI / 180d;
    double halfW = (column.kind == AnnotationSlot.Evidence ? 0.065d : 0.07d) * IncomeToolkit.CardVisualScale;
    double halfD = (column.kind == AnnotationSlot.Evidence ? 0.033d : IncomeToolkit.NoteCardDepth * 0.5d) * IncomeToolkit.CardVisualScale;
    double extentX = halfW * Math.Cos(angle) + halfD * Math.Sin(angle);
    double extentZ = halfW * Math.Sin(angle) + halfD * Math.Cos(angle);
    for (int i = 0; i < column.count; i++)
    {
        var home = IncomeToolkit.CardHome(column.kind, i);
        Check(Math.Abs(home.x) + extentX < (column.kind == AnnotationSlot.Evidence ? 0.70d : 0.66d) && Math.Abs(home.z) + extentZ < 0.37d,
            $"market layout: {column.kind} card {i + 1} stays inside the tabletop, including its rotation");
        if (i > 0)
        {
            var previous = IncomeToolkit.CardHome(column.kind, i - 1);
            Check(previous.z - home.z > extentZ * 2d,
                $"market layout: {column.kind} cards {i} and {i + 1} do not overlap");
        }
    }
}
Check(IncomeToolkit.CardHome(AnnotationSlot.Evidence, 0).x - 0.065f * IncomeToolkit.CardVisualScale > 0.04f + 0.18f * 1.15f &&
      Enumerable.Range(0, 6).All(r => IncomeToolkit.CardHome(AnnotationSlot.Evidence, r).x + 0.075f * IncomeToolkit.CardVisualScale < 0.70f &&
                                      Math.Abs(IncomeToolkit.CardHome(AnnotationSlot.Evidence, r).z) + 0.05f < 0.37f),
    "market layout: record tags (up to six) sit in two columns right of the clipboard, inside the tabletop");
Check(IncomeToolkit.CardHome(AnnotationSlot.Evidence, 3).x - IncomeToolkit.CardHome(AnnotationSlot.Evidence, 0).x > 0.20f,
    "the two record-card columns leave a visible gap between their rotated faces");
Check(annotation.HasTutorial && t0.id == "T0" && t0.demo.IsSet, "the toolkit has an unscored T0 demonstration record");
Check(annotation.entries.Count == 4, "four scored challenge entries (T06)");
Check(t0.title.Contains("Payslip") && t0.lines.Any(l => l.id == t0.demo.highlightLineId && l.amount == 8000f),
    "T0 is the unrelated 8,000 salary slip; the demonstrated line is its salary line");
string t0Text = t0.title + "\n" + t0.issuer + "\n" + string.Join("\n", t0.lines.Select(l => l.label + " " + l.value));
foreach (string word in new[] { "Rosa", "laundry", "blanket", "withholding", "deduction", "tax due" })
    Check(t0Text.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0, "T0 is unrelated to Rosa and teaches no tax computation: " + word);
string optionText = string.Join("\n", annotation.typeOptions.Concat(annotation.treatmentOptions).Concat(t0.evidenceOptions).Select(o => o.label + " " + o.rule));
foreach (string word in new[] { "Rosa", "blanket", "5,000", "20,000", "E2", "stamp", "tax-free", "final tax" })
    Check(optionText.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0, "toolkit rules are general (no client answers, no default tax status): " + word);
Check(annotation.typeOptions.Select(o => o.id).SequenceEqual(new[] { "compensation", "business-service", "rental", "passive-interest", "borrowed" }),
    "TYPE notes: compensation, business/service, rental, passive (interest), borrowed funds");
Check(annotation.treatmentOptions.Select(o => o.id).SequenceEqual(new[] { "regular-schedule", "separate-rule", "statutory-exclusion", "not-income" }),
    "TREATMENT notes are separate from TYPE: regular schedule, separate rule, statutory exclusion, not income");
Check(annotation.typeOptions.Concat(annotation.treatmentOptions).All(o => !string.IsNullOrWhiteSpace(o.rule)),
    "every note card carries an immediate rule explanation");
Check(t0.demo.introLine.Contains("not scored") || t0.demo.introLine.Contains("nothing here is scored"), "the guide says the practice is not scored");

var badDemo = Clone();
badDemo.annotation.tutorial.demo.typeId = "salary-xyz";
Check(CaseReviewValidator.Validate(badDemo).Any(x => x.Contains("demo.typeId")), "a demonstration card must exist");
var badDemoLine = Clone();
badDemoLine.annotation.tutorial.demo.highlightLineId = "nowhere";
Check(CaseReviewValidator.Validate(badDemoLine).Any(x => x.Contains("demo.highlightLineId")), "a demonstrated line must be printed on the record");
var dupOption = Clone();
dupOption.annotation.typeOptions.Add(new AnnotationOptionDefinition { id = "compensation", label = "Again" });
Check(CaseReviewValidator.Validate(dupOption).Any(x => x.Contains("used twice")), "note card ids are unique");
var noTags = Clone();
noTags.annotation.tutorial.evidenceOptions.Clear();
Check(CaseReviewValidator.Validate(noTags).Any(x => x.Contains("no evidenceOptions")), "a record needs record tags to clip");
var swappedTreatment = Clone();
swappedTreatment.annotation.tutorial.demo.treatmentId = "compensation";
Check(CaseReviewValidator.Validate(swappedTreatment).Any(x => x.Contains("demo.treatmentId")), "a TYPE id is not a TREATMENT note");

// One set of actions for every entry.
AnnotationEntryState aw = null;
AnnotationActions.Ensure(ref aw, t0);
Check(aw != null && aw.entryId == "T0" && !aw.IsComplete, "annotation work starts empty for its own entry");
Check(!AnnotationActions.Highlight(aw, t0, "not-a-line") && aw.highlightLineId == null, "only printed lines can be highlighted");
Check(AnnotationActions.Highlight(aw, t0, "T0-period") && !AnnotationActions.Highlight(aw, t0, "T0-period") &&
      AnnotationActions.Highlight(aw, t0, "T0-salary") && aw.highlightLineId == "T0-salary", "one highlighted line; a new tap moves it");
Check(!AnnotationActions.Place(aw, annotation, t0, AnnotationSlot.Type, AnnotationSlot.Treatment, "compensation", out _) && aw.treatmentId == null,
    "a TYPE card does not fit the TREATMENT place (type and treatment stay separate)");
Check(!AnnotationActions.Place(aw, annotation, t0, AnnotationSlot.Evidence, AnnotationSlot.Type, "payslip-ps0430", out _) && aw.typeId == null,
    "a record tag does not fit the TYPE place");
Check(!AnnotationActions.Place(aw, annotation, t0, AnnotationSlot.Type, AnnotationSlot.Type, "made-up", out _), "unknown cards are refused");
Check(AnnotationActions.Place(aw, annotation, t0, AnnotationSlot.Type, AnnotationSlot.Type, "rental", out string displacedA) && displacedA == null && aw.typeId == "rental",
    "a fitting card is placed even when it is not the demonstrated answer (no answer-key drop validation)");
Check(AnnotationActions.Place(aw, annotation, t0, AnnotationSlot.Type, AnnotationSlot.Type, "compensation", out string displacedB) && displacedB == "rental" && aw.typeId == "compensation",
    "a card on a filled place replaces it and the old card comes back");
Check(!AnnotationActions.Place(aw, annotation, t0, AnnotationSlot.Type, AnnotationSlot.Type, "compensation", out _), "placing the same card again changes nothing");
Check(AnnotationActions.Place(aw, annotation, t0, AnnotationSlot.Evidence, AnnotationSlot.Evidence, "company-id", out _) &&
      AnnotationActions.Place(aw, annotation, t0, AnnotationSlot.Evidence, AnnotationSlot.Evidence, "payslip-ps0430", out string displacedC) &&
      displacedC == "company-id" && aw.evidenceIds.SequenceEqual(new[] { "payslip-ps0430" }), "a single clip swaps its record");
Check(AnnotationActions.Remove(aw, AnnotationSlot.Evidence, "payslip-ps0430") && !AnnotationActions.Remove(aw, AnnotationSlot.Evidence, "payslip-ps0430") &&
      aw.evidenceIds.Count == 0 && !AnnotationActions.Remove(aw, AnnotationSlot.Type, "rental"), "a note comes off the paper once, and only if it is there");
var bundle = new AnnotationEntryDefinition
{
    id = "EX", title = "Bundle check", lines = t0.lines, maxEvidence = 2,
    evidenceOptions = new List<AnnotationOptionDefinition>
    {
        new AnnotationOptionDefinition { id = "D2", label = "D2" }, new AnnotationOptionDefinition { id = "D5", label = "D5" },
        new AnnotationOptionDefinition { id = "D1", label = "D1" }
    }
};
AnnotationEntryState bw = aw;
AnnotationActions.Ensure(ref bw, bundle);
Check(bw != aw && bw.entryId == "EX" && bw.typeId == null, "a different data-defined entry gets its own work (same actions, new record)");
AnnotationActions.Place(bw, annotation, bundle, AnnotationSlot.Evidence, AnnotationSlot.Evidence, "D2", out _);
AnnotationActions.Place(bw, annotation, bundle, AnnotationSlot.Evidence, AnnotationSlot.Evidence, "D5", out _);
Check(bw.evidenceIds.SequenceEqual(new[] { "D2", "D5" }), "a two-record clip holds a bundle (e.g. D2 + D5)");
Check(AnnotationActions.Place(bw, annotation, bundle, AnnotationSlot.Evidence, AnnotationSlot.Evidence, "D1", out string displacedD) &&
      displacedD == "D2" && bw.evidenceIds.Count == 2, "a full bundle clip swaps out its oldest record");

// Tutorial transition: practice never touches the reviewed packet or a score.
var tut = new CaseReviewState { reviewId = "L2-ROSA-001", phase = ReviewPhase.MarketVisit };
Check(!tut.TryMarkToolkitDemoSeen() && !tut.TutorialActive && !tut.TryCompleteTutorial(t0), "no toolkit tutorial before Rosa's handover");
tut.TryCollectHandover(handoverIds);
Check(tut.TutorialActive && tut.TryMarkToolkitDemoSeen() && !tut.TryMarkToolkitDemoSeen(), "the demonstration is recorded once, after the handover");
int tutRevisionBefore = tut.packetRevision;
AnnotationEntryState tw = tut.TutorialWork(t0);
AnnotationActions.Highlight(tw, t0, "T0-salary");
AnnotationActions.Place(tw, annotation, t0, AnnotationSlot.Type, AnnotationSlot.Type, "compensation", out _);
AnnotationActions.Place(tw, annotation, t0, AnnotationSlot.Treatment, AnnotationSlot.Treatment, "regular-schedule", out _);
Check(!tut.TryCompleteTutorial(t0) && tut.phase == ReviewPhase.Tutorial, "incomplete practice does not finish the tutorial");
AnnotationActions.Place(tw, annotation, t0, AnnotationSlot.Evidence, AnnotationSlot.Evidence, "company-id", out _);
Check(tw.IsComplete && !tut.TryCompleteTutorial(t0) && !tut.tutorialCompleted, "complete practice that differs from the demonstration does not finish it");
Check(!AnnotationActions.MatchesDemonstration(t0, AnnotationSlot.Evidence, "company-id") &&
      AnnotationActions.MatchesDemonstration(t0, AnnotationSlot.Type, "compensation"), "each note can be explained against the demonstration");
AnnotationActions.Place(tw, annotation, t0, AnnotationSlot.Evidence, AnnotationSlot.Evidence, "payslip-ps0430", out _);
Check(tut.TryCompleteTutorial(t0) && tut.tutorialCompleted && tut.phase == ReviewPhase.Challenge, "repeating the demonstration finishes the tutorial and opens the challenge");
Check(!tut.TryCompleteTutorial(t0) && !tut.TutorialActive, "the tutorial finishes once (no repeat event)");
Check(tut.packetRevision == tutRevisionBefore && tut.preliminary == null, "practice never changes the reviewed packet");
var tutRound = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(tut, fieldJson), fieldJson);
Check(tutRound.tutorialCompleted && tutRound.toolkitDemoSeen && tutRound.tutorialAnnotation != null &&
      tutRound.tutorialAnnotation.typeId == "compensation" && tutRound.tutorialAnnotation.evidenceIds.SequenceEqual(new[] { "payslip-ps0430" }),
    "toolkit work survives a JSON round trip (leaving and coming back restores it)");
Check(ObjectiveTriggers.Validate("event:AnnotationTutorialCompleted") == null && ObjectiveTriggers.Validate("event:ToolkitDemoFinished") == null,
    "toolkit events are valid objective/guide triggers");
Check(File.ReadAllText(Res("Levels/Level_02/objectives.json")).Contains("event:AnnotationTutorialCompleted") &&
      File.ReadAllText(Res("Tutorials/Level_02/guide.json")).Contains("\"when\": \"event:ToolkitDemoFinished\""),
    "objective and guide observe the toolkit events (the guide never fills a slot)");

string toolkitSrc = Src("Scripts/Level2/IncomeToolkit.cs");
string paperSrc = Src("Scripts/Level2/AnnotationPaper.cs");
int switchStart = toolkitSrc.IndexOf("private IEnumerator SwitchEntry(", StringComparison.Ordinal);
int turnStart = toolkitSrc.IndexOf("private IEnumerator TurnPage(", switchStart, StringComparison.Ordinal);
string switchBody = toolkitSrc.Substring(switchStart, turnStart - switchStart);
Check(switchBody.Contains("while (current != destination)") && switchBody.Contains("current += direction;") &&
      switchBody.Contains("yield return TurnPage(options.entries[current]);") && switchBody.Contains("destination > current ? 1 : -1"),
    "tab journeys animate each intervening record in either direction");
int closeStart = toolkitSrc.IndexOf("private void Close()", StringComparison.Ordinal);
int closeEnd = toolkitSrc.IndexOf("private IEnumerator DealCards()", closeStart, StringComparison.Ordinal);
string closeBody = toolkitSrc.Substring(closeStart, closeEnd - closeStart);
Check(closeBody.Contains("StopCoroutine(pageMotion)") && closeBody.Contains("record.Hinge.localRotation = Quaternion.identity"),
    "leaving the desk cancels a page journey and restores flat pages before reopening");
int demoFlyStart = toolkitSrc.IndexOf("private IEnumerator FlyToPlace(", StringComparison.Ordinal);
int demoFlyEnd = toolkitSrc.IndexOf("private IEnumerator SettleInto(", demoFlyStart, StringComparison.Ordinal);
string demoFlyBody = toolkitSrc.Substring(demoFlyStart, demoFlyEnd - demoFlyStart);
Check(demoFlyBody.Contains("card.placed = true;") && demoFlyBody.Contains("StartCard(card, SettleInto(") && demoFlyBody.Contains("yield return card.motion;"),
    "demonstration cards own their landing motion and stay on the paper instead of desk-idling home");
int dropStart = toolkitSrc.IndexOf("private void EndDrag(", StringComparison.Ordinal);
int dropEnd = toolkitSrc.IndexOf("private void CancelDrag()", dropStart, StringComparison.Ordinal);
string dropBody = toolkitSrc.Substring(dropStart, dropEnd - dropStart);
Check(dropBody.IndexOf("SlotUnder(Pick(view, screen))", StringComparison.Ordinal) <
      dropBody.IndexOf("card.hit.enabled = true", StringComparison.Ordinal),
    "a dropped card cannot intercept its own slot ray before the release is resolved");
Check(dropBody.Contains("if (OverUi(screen))") && dropBody.Contains("CancelDrag();") && dropBody.Contains("DoPlace(card, slot.Value)"),
    "card drops use the same validated placement path as taps and respect foreground UI");
Check(IncomeToolkit.CardHome(AnnotationSlot.Treatment, 0).x + 0.1f < 0.04f - 0.18f * 1.15f,
    "the orange palette keeps a visible gap from the paper");
int demoStart = toolkitSrc.IndexOf("private IEnumerator Demo()", StringComparison.Ordinal);
int demoEnd = toolkitSrc.IndexOf("private IEnumerator Wait(", StringComparison.Ordinal);
string demoBody = demoStart >= 0 && demoEnd > demoStart ? toolkitSrc.Substring(demoStart, demoEnd - demoStart) : "";
Check(demoBody.Length > 0 && !demoBody.Contains("AnnotationActions.") && !demoBody.Contains("TryCompleteTutorial"),
    "the demonstration is presentation only: it never writes the player's work");
Check(!Regex.IsMatch(toolkitSrc, @"Time\.timeScale\s*=") && toolkitSrc.Contains("public static bool TeachingPaused") &&
      toolkitSrc.Contains("if (ignorePause || !Paused) t += Time.unscaledDeltaTime"), "compulsory teaching pauses through a predicate, never Time.timeScale");
Check(toolkitSrc.Contains("LidOpenAngle = 100f") && toolkitSrc.Contains("LidOpenSeconds = 0.45f") && toolkitSrc.Contains("SettleSeconds = 0.12f") &&
      toolkitSrc.Contains("TraceSeconds = 0.22f"), "lid ~100 degrees over 0.45 s, marker trace ~0.2 s, note settle ~0.12 s");
Check(toolkitSrc.Contains("review.TryCompleteTutorial(entry)") && toolkitSrc.Contains("GameplayEvents.RaiseAnnotationTutorialCompleted()") &&
      toolkitSrc.Contains("review.TryMarkToolkitDemoSeen()) GameplayEvents.RaiseToolkitDemoFinished()"), "the domain moves on only through the player's own repeat");
int tutorialStart = toolkitSrc.IndexOf("private IEnumerator TutorialDone()", StringComparison.Ordinal);
int tutorialEnd = toolkitSrc.IndexOf("private IEnumerator BeginChallenge()", StringComparison.Ordinal);
string tutorialBody = tutorialStart >= 0 && tutorialEnd > tutorialStart ? toolkitSrc.Substring(tutorialStart, tutorialEnd - tutorialStart) : "";
Check(tutorialBody.Length > 0 && !tutorialBody.Contains("AddSideQuest") && !tutorialBody.Contains("Score.") && !toolkitSrc.Contains("packetRevision"),
    "the tutorial cannot affect scores, rewards or the packet");
Check(toolkitSrc.Contains("if (!AtDesk)") && toolkitSrc.Contains("CancelDrag();") && toolkitSrc.Contains("notes on the paper stay attached"),
    "leaving the desk closes the case, cancels a held card and keeps the work");
Check(toolkitSrc.Contains("new Vector3(w + 0.014f, 0.02f, d + 0.012f)") && toolkitSrc.Contains("DragThreshold = 12f"),
    "large card touch targets; tap/tap is the default and dragging needs a deliberate move");
Check(toolkitSrc.Contains("OverUi(screen)") && toolkitSrc.Contains("Doc3DLayer.All"), "HUD buttons and documents in front keep their touches");
Check(paperSrc.Contains("{ \"TYPE\", \"TREATMENT\", \"EVIDENCE\" }") && paperSrc.Contains("FitColor") && paperSrc.Contains("compatible destination only"),
    "note places are labelled with words and frame shapes; the green tint means a fitting place, not a correct answer");
Check(paperSrc.Contains("box.isTrigger = true") && paperSrc.Contains("Object.DestroyImmediate(shape.GetComponent<Collider>())") &&
      toolkitSrc.Contains("builder.ReleaseMaterials()"), "only labelled trigger targets collide; runtime materials are released");
Check(Src("Scripts/Level2/Level2RosaStall.cs").Contains("Desk.Init(viewpoint, IncomeToolkit.Create(root.transform, top))") &&
      Src("Scripts/Level2/MarketRecordsDesk.cs").Contains("toolkit.RequestOpen()"), "the toolkit sits on the records desk and opens when the player sits down");
Check(Src("Scripts/Data/CaseReviewState.cs").Contains("Practice edits never change packetRevision"), "tutorial work is kept apart from the reviewed packet");

// ================= T06 Mark the Money! challenge =================
var l2Rules = JsonSerializer.Deserialize<LevelRewardRules>(File.ReadAllText(Res("Levels/Level_02/rewards.json")), fieldJson);
l2Rules.Sanitize();
Check(LevelScoring.UsesAccuracyBadge(l2Rules) && l2Rules.sideQuest.goldAtCorrect == 16 && l2Rules.sideQuest.silverAtCorrect == 14 &&
      l2Rules.sideQuest.minExp == 48 && l2Rules.sideQuest.exp == 60 && l2Rules.sideQuest.hearts == 3, "Level 2 rewards: accuracy badge 16/14, EXP 60 with a 48 floor, 3 hearts");
foreach (var (correct, tier) in new[] { (0, BadgeTier.Bronze), (13, BadgeTier.Bronze), (14, BadgeTier.Silver), (15, BadgeTier.Silver), (16, BadgeTier.Gold) })
    Check(LevelScoring.AccuracyBadge(l2Rules, correct) == tier, $"tier boundary: {correct}/16 first-check correct is {tier}");
foreach (int wrong in new[] { 0, 1, 3, 16 })
{
    var award = LevelScoring.SideQuest(l2Rules, wrong);
    Check(award.Exp == Math.Max(48, 60 - wrong) && award.Hearts == 3 && award.MaxExp == 60, $"quest EXP for {wrong} wrong is {Math.Max(48, 60 - wrong)}, 3 completion hearts");
}
var l1Rules = new LevelRewardRules();
Check(!LevelScoring.UsesAccuracyBadge(l1Rules) && LevelScoring.SideQuest(l1Rules, 3).Exp == Math.Max(0, l1Rules.sideQuest.exp - 3 * l1Rules.sideQuest.expPerWrong),
    "Level 1 keeps its EXP-ratio badge and has no EXP floor");
Check(LevelScoring.SideQuestBadge(l2Rules, LevelScoring.SideQuest(l2Rules, 16)) == BadgeTier.Silver && LevelScoring.AccuracyBadge(l2Rules, 0) == BadgeTier.Bronze,
    "the forgiving EXP ratio would wrongly say Silver at 0/16, so Level 2 uses the accuracy rule instead");

var entries = annotation.entries;
Check(entries.Select(e => e.id).SequenceEqual(new[] { "E1", "E2", "E3", "E4" }) && rosa.review.annotationKey.Count == 4,
    "exactly four challenge entries, each with a key");
Check(IncomeChallenge.ScoredSlots.Length * entries.Count == 16, "4 entries x highlight, TYPE, TREATMENT and EVIDENCE = 16 decisions");
var rk = rosa.review;
Check(rk.FindKey("E1").typeId == "business-service" && rk.FindKey("E1").treatmentId == "regular-schedule" && rk.FindKey("E1").evidenceIds.SequenceEqual(new[] { "D1" }) &&
      rk.FindKey("E2").typeId == "business-service" && rk.FindKey("E2").evidenceIds.SequenceEqual(new[] { "D3" }) &&
      rk.FindKey("E3").typeId == "rental" && rk.FindKey("E3").treatmentId == "regular-schedule" && rk.FindKey("E3").evidenceIds.SequenceEqual(new[] { "D4" }) &&
      rk.FindKey("E4").typeId == "borrowed" && rk.FindKey("E4").treatmentId == "not-income" && rk.FindKey("E4").evidenceIds.SequenceEqual(new[] { "D5" }) &&
      rk.FindKey("E1").highlightLineId == "E1-total" && rk.FindKey("E2").highlightLineId == "E2-amount" &&
      rk.FindKey("E3").highlightLineId == "E3-amount" && rk.FindKey("E4").highlightLineId == "E4-credit",
    "answer key matches CONTENT, including each highlighted source line");
string t06RecordText = string.Join("\n", entries.SelectMany(e => e.lines.Select(l => l.label + " " + l.value)).Concat(entries.Select(e => e.title)));
foreach (var option in annotation.typeOptions.Concat(annotation.treatmentOptions))
    Check(t06RecordText.IndexOf(option.label, StringComparison.OrdinalIgnoreCase) < 0, "records never print a note answer: " + option.label);
string hintText = string.Join("\n", rosa.review.annotationKey.SelectMany(k => k.typeHints.Concat(k.treatmentHints).Concat(k.evidenceHints).Concat(k.highlightHints)));
foreach (string word in new[] { "summary", "15,000", "140,000", "total", "omit", "missing", "F1", "F2", "stamp", "Ready", "cash versus", "add 5" })
    Check(hintText.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0, "correction hints teach the rule only, no case answer: " + word);
Check(entries.All(e => e.maxEvidence == 1 && e.evidenceOptions.Select(o => o.id).SequenceEqual(handoverIds)),
    "every entry offers the same four handed-over records and a one-record clip (the evidence is never given away)");
Check(entries.All(e => e.lines.Count <= 4) && entries.All(e => e.lines.Count(l => l.amount > 0) == 1),
    "each annotated record is short: at most four lines and exactly one money line to highlight");
var keyless = Clone();
keyless.annotationKey.RemoveAt(0);
Check(CaseReviewValidator.Validate(keyless).Any(x => x.Contains("has no annotationKey")), "an entry without a key is rejected");
var badKey = Clone();
badKey.annotationKey[3].evidenceIds.Add("D6");
Check(CaseReviewValidator.Validate(badKey).Any(x => x.Contains("more evidence")), "a key cannot need more records than the clip holds");
var badHighlight = Clone();
badHighlight.annotationKey[0].highlightLineId = "not-a-line";
Check(CaseReviewValidator.Validate(badHighlight).Any(x => x.Contains("highlightLineId")), "a highlight key must point to a printed line on its record");
var noHints = Clone();
noHints.annotationKey[1].treatmentHints.Clear();
Check(CaseReviewValidator.Validate(noHints).Any(x => x.Contains("Treatment hints")), "every note place has correction hints");
var noHighlightHints = Clone();
noHighlightHints.annotationKey[1].highlightHints.Clear();
Check(CaseReviewValidator.Validate(noHighlightHints).Any(x => x.Contains("Highlight hints")), "the highlighted source line has correction hints");

CaseReviewState Challenger()
{
    var s = new CaseReviewState { reviewId = "L2-ROSA-001", phase = ReviewPhase.Challenge, tutorialCompleted = true };
    IncomeChallenge.Ensure(s);
    return s;
}
void Answer(CaseReviewState s, int wrong)
{
    int left = wrong;
    foreach (var e in entries)
    {
        var k = rk.FindKey(e.id);
        var w = IncomeChallenge.Work(s.challenge, e);
        w.highlightLineId = k.highlightLineId;
        w.typeId = k.typeId;
        w.treatmentId = k.treatmentId;
        w.evidenceIds = new List<string>(k.evidenceIds);
        if (left > 0) { w.typeId = k.typeId == "rental" ? "business-service" : "rental"; left--; }
        if (left > 0) { w.treatmentId = k.treatmentId == "not-income" ? "regular-schedule" : "not-income"; left--; }
        if (left > 0) { w.evidenceIds = new List<string> { "D6" }; left--; }
        if (left > 0)
        {
            w.highlightLineId = e.lines.First(line => line.id != k.highlightLineId).id;
            left--;
        }
    }
}
var none = Challenger();
var noneResult = IncomeChallenge.TryCheck(none, rk, out var noneDecisions, out var noneMissing);
Check(noneResult == IncomeChallenge.CheckResult.Incomplete && noneMissing.Count == 16 && !none.challenge.firstChecked && none.challenge.checks == 0 &&
      none.phase == ReviewPhase.Challenge, "an incomplete check scores nothing, freezes nothing and lists what is missing");
var partial = Challenger();
Answer(partial, 0);
partial.challenge.Find("E3").highlightLineId = null;
Check(IncomeChallenge.TryCheck(partial, rk, out _, out var partialMissing) == IncomeChallenge.CheckResult.Incomplete &&
      partialMissing.SequenceEqual(new[] { "E3: highlight line" }), "a source highlight is required before a check");
foreach (int wrong in new[] { 0, 1, 2, 3, 12, 16 })
{
    var run = Challenger();
    Answer(run, wrong);
    int revision = run.packetRevision;
    var result = IncomeChallenge.TryCheck(run, rk, out var decisions, out _);
    Check(result == IncomeChallenge.CheckResult.Checked && decisions.Count == 16 && run.challenge.firstChecked &&
          run.challenge.firstCorrect == 16 - wrong && run.challenge.firstTotal == 16 && run.challenge.firstWrong.Count == wrong,
        $"a complete first check with {wrong} wrong is frozen at {16 - wrong}/16");
    Check(run.phase == (wrong == 0 ? ReviewPhase.Feedback : ReviewPhase.Feedback) && run.challenge.mastered == (wrong == 0),
        $"the first check moves to Feedback; mastery only when all 16 are right ({wrong} wrong)");
    Check(run.packetRevision == revision && run.preliminary == null && run.findings.Count == 0,
        "checking never touches the reviewed packet, findings or stamps");
    if (wrong == 0) continue;
    Check(!IncomeChallenge.TryAward(run), "no award before every note is corrected");
    // Locks: right notes are confirmed, wrong ones flagged until changed.
    var firstWrong = run.challenge.firstWrong[0];
    string wrongEntry = firstWrong.Split('.')[0];
    var wrongSlot = (AnnotationSlot)Enum.Parse(typeof(AnnotationSlot), firstWrong.Split('.')[1]);
    Check(IncomeChallenge.IsFlagged(run.challenge, wrongEntry, wrongSlot) && !IncomeChallenge.IsLocked(run.challenge, wrongEntry, wrongSlot) &&
          IncomeChallenge.IsLocked(run.challenge, "E4", AnnotationSlot.Evidence) == !run.challenge.firstWrong.Contains("E4.Evidence"),
        "after a check right notes lock and wrong notes are flagged");
    IncomeChallenge.NoteEdited(run.challenge, wrongEntry, wrongSlot);
    Check(!IncomeChallenge.IsFlagged(run.challenge, wrongEntry, wrongSlot), "changing a flagged note clears its red mark until the next check");
    Check(IncomeChallenge.Hint(rk.FindKey(wrongEntry), wrongSlot, 1) == rk.FindKey(wrongEntry).HintsFor(wrongSlot)[0] &&
          IncomeChallenge.Hint(rk.FindKey(wrongEntry), wrongSlot, 5) == rk.FindKey(wrongEntry).HintsFor(wrongSlot).Last(),
        "hints start general and grow specific on later checks");
    // Correct everything: mastery, frozen score unchanged, award once.
    Answer(run, 0);
    Check(IncomeChallenge.TryCheck(run, rk, out _, out _) == IncomeChallenge.CheckResult.Checked && run.challenge.mastered &&
          run.challenge.firstCorrect == 16 - wrong && run.challenge.checks == 2, "corrections reach mastery without changing the frozen first score");
    Check(LevelScoring.AccuracyBadge(l2Rules, run.challenge.firstCorrect) == LevelScoring.AccuracyBadge(l2Rules, 16 - wrong),
        "corrections never upgrade the frozen tier");
    Check(IncomeChallenge.TryAward(run) && run.phase == ReviewPhase.OfficeReconciliation && !IncomeChallenge.TryAward(run) &&
          IncomeChallenge.FirstWrongCount(run.challenge) == wrong, "the award is given exactly once and opens office reconciliation");
    Check(IncomeChallenge.TryCheck(run, rk, out _, out _) == IncomeChallenge.CheckResult.NotAvailable, "a finished challenge cannot be rechecked");
    Check(run.preliminary == null && run.findings.Count == 0 && run.packetRevision == revision,
        "Mark the Money! leaves the summary, findings and final stamp for the office");
    var round = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(run, fieldJson), fieldJson);
    Check(round.challenge.awarded && round.challenge.firstCorrect == 16 - wrong && round.challenge.firstWrong.Count == wrong && !IncomeChallenge.TryAward(round),
        "the frozen grade and award flag survive a save round trip (no reroll, no second award)");
}
var bundleRun = Challenger();
Answer(bundleRun, 0);
bundleRun.challenge.Find("E4").evidenceIds = new List<string> { "D1" };
IncomeChallenge.TryCheck(bundleRun, rk, out var bundleDecisions, out _);
Check(!bundleDecisions.Single(d => d.EntryId == "E4" && d.Slot == AnnotationSlot.Evidence).Correct && bundleDecisions.Count(d => !d.Correct) == 1,
    "the wrong record for the loan deposit is one wrong decision");
bundleRun.challenge.Find("E4").evidenceIds = new List<string> { "D5" };
IncomeChallenge.TryCheck(bundleRun, rk, out bundleDecisions, out _);
Check(bundleDecisions.All(d => d.Correct) && bundleRun.challenge.firstCorrect == 15, "the loan agreement D5 is right; the first score stays frozen at 15/16");
var highlightRun = Challenger();
Answer(highlightRun, 0);
highlightRun.challenge.Find("E2").highlightLineId = "E2-date";
Check(IncomeChallenge.TryCheck(highlightRun, rk, out var highlightDecisions, out _) == IncomeChallenge.CheckResult.Checked &&
      highlightDecisions.Count(d => !d.Correct) == 1 && highlightDecisions.Single(d => d.EntryId == "E2" && d.Slot == AnnotationSlot.Highlight).Id == "E2.Highlight" &&
      highlightRun.challenge.firstCorrect == 15 && IncomeChallenge.IsFlagged(highlightRun.challenge, "E2", AnnotationSlot.Highlight) &&
      !IncomeChallenge.IsLocked(highlightRun.challenge, "E2", AnnotationSlot.Highlight),
    "a wrong source highlight is its own scored and flagged decision");
IncomeChallenge.NoteEdited(highlightRun.challenge, "E2", AnnotationSlot.Highlight);
highlightRun.challenge.Find("E2").highlightLineId = rk.FindKey("E2").highlightLineId;
IncomeChallenge.TryCheck(highlightRun, rk, out highlightDecisions, out _);
Check(highlightDecisions.All(d => d.Correct) && IncomeChallenge.IsLocked(highlightRun.challenge, "E2", AnnotationSlot.Highlight),
    "correcting a flagged source highlight clears its flag and locks it after the next check");

// A direct check must stamp the score version, so a later normal ensure cannot
// mistake its fresh 16-decision result for an old 12-decision save.
var directCheck = new CaseReviewState { reviewId = "L2-ROSA-001", phase = ReviewPhase.Challenge, tutorialCompleted = true, challenge = new ChallengeState() };
Answer(directCheck, 0);
Check(IncomeChallenge.TryCheck(directCheck, rk, out _, out _) == IncomeChallenge.CheckResult.Checked &&
      directCheck.challenge.scoringVersion == IncomeChallenge.ScoringVersion && directCheck.challenge.firstTotal == 16,
    "checking a new challenge without a prior Ensure stamps the current scoring version");
IncomeChallenge.Ensure(directCheck, rk);
Check(directCheck.challenge.firstChecked && directCheck.challenge.firstCorrect == 16 && directCheck.challenge.firstTotal == 16,
    "Ensure preserves a direct first check instead of clearing its fresh 16-decision score");

// Old unchecked / unawarded score snapshots are reset for a new 16-decision
// first check, while completed old awards are retained and migrated in place.
var legacyUnawarded = Challenger();
Answer(legacyUnawarded, 0);
legacyUnawarded.challenge.scoringVersion = 0;
legacyUnawarded.challenge.firstChecked = true;
legacyUnawarded.challenge.firstCorrect = 11;
legacyUnawarded.challenge.firstTotal = 12;
legacyUnawarded.challenge.firstWrong = new List<string> { "E1.Type" };
legacyUnawarded.challenge.checks = 2;
legacyUnawarded.challenge.mastered = false;
IncomeChallenge.Ensure(legacyUnawarded, rk);
Check(!legacyUnawarded.challenge.firstChecked && legacyUnawarded.challenge.firstCorrect == 0 && legacyUnawarded.challenge.firstTotal == 0 &&
      legacyUnawarded.challenge.firstWrong.Count == 0 && legacyUnawarded.challenge.checks == 0 &&
      legacyUnawarded.challenge.Find("E1").highlightLineId == rk.FindKey("E1").highlightLineId,
    "legacy unawarded checks reset their obsolete score but preserve player work for a new first check");
var legacyAwarded = Challenger();
Answer(legacyAwarded, 0);
legacyAwarded.challenge.scoringVersion = 0;
legacyAwarded.challenge.firstChecked = true;
legacyAwarded.challenge.firstCorrect = 11;
legacyAwarded.challenge.firstTotal = 12;
legacyAwarded.challenge.firstWrong = new List<string> { "E1.Type" };
legacyAwarded.challenge.mastered = true;
legacyAwarded.challenge.awarded = true;
legacyAwarded.phase = ReviewPhase.OfficeReconciliation;
legacyAwarded.challenge.Find("E3").highlightLineId = "E3-note";
IncomeChallenge.Ensure(legacyAwarded, rk);
Check(legacyAwarded.challenge.awarded && legacyAwarded.challenge.firstTotal == 16 && legacyAwarded.challenge.firstCorrect == 14 &&
      legacyAwarded.challenge.firstWrong.Contains("E1.Type") && legacyAwarded.challenge.firstWrong.Contains("E3.Highlight") &&
      legacyAwarded.challenge.flagged.Contains("E3.Highlight"),
    "legacy awarded checks retain the award and migrate their displayed score with each actual highlight result");
var e1Extra = Challenger();
Answer(e1Extra, 0);
e1Extra.challenge.Find("E1").evidenceIds = new List<string> { "D1", "D6" };
IncomeChallenge.TryCheck(e1Extra, rk, out var extraDecisions, out _);
Check(!extraDecisions.Single(d => d.EntryId == "E1" && d.Slot == AnnotationSlot.Evidence).Correct, "E1's evidence is its originating collection sheet only");
var tooEarly = new CaseReviewState { reviewId = "L2-ROSA-001", phase = ReviewPhase.Tutorial };
Check(IncomeChallenge.Ensure(tooEarly) == null && IncomeChallenge.TryCheck(tooEarly, rk, out _, out _) == IncomeChallenge.CheckResult.NotAvailable,
    "no challenge before the tutorial is finished");
var listRun = Challenger();
Answer(listRun, 0);
string listText = string.Join("\n", IncomeChallenge.AnnotatedList(listRun.challenge, annotation));
Check(listText.Contains("Rental income") && listText.Contains("Paid in May") && listText.Contains("D5") && !listText.Contains("20,000 total") && !listText.Contains("Total"),
    "the Annotated Income List includes each source line and note with no summed or reconciled total");

string toolkitSrc6 = Src("Scripts/Level2/IncomeToolkit.cs");
Check(Regex.Matches(toolkitSrc6, @"\.AddSideQuest\(").Count == 1 && toolkitSrc6.Contains("if (!IncomeChallenge.TryAward(review)) return;") &&
      toolkitSrc6.Contains("AddSideQuest(wrong, transform.position + transform.up * 0.3f, tier)"),
    "the ledger award happens once, guarded by IncomeChallenge.TryAward, with the frozen accuracy tier");
Check(toolkitSrc6.Contains("SideQuestCompleteUI.Instance.ShowScreen(data, afterContinue)") && toolkitSrc6.Contains("data.Fact(\"First check\""),
    "the existing reward card shows the first-check score and the tier medal");
Check(toolkitSrc6.Contains("lid.localRotation = Quaternion.Euler(lidAngle, 0f, 0f)"), "the lid opens up and back (it no longer swings into the case)");
Check(toolkitSrc6.Contains("IncomeChallenge.CheckResult.Incomplete") && toolkitSrc6.Contains("Nothing was scored."), "CHECK with missing notes only says what is missing");
Check(toolkitSrc6.Contains("RevealResults(") && toolkitSrc6.Contains("CountUp(") && toolkitSrc6.Contains("fx.Confetti(") && toolkitSrc6.Contains("ShowList(challenge)"),
    "results flip in, the score counts up, mastery celebrates and shows the Annotated Income List");
Check(toolkitSrc6.Contains("tab.done.gameObject.SetActive(allRight)"), "a tab tick only ever means confirmed right by a check");
Check(Src("Scripts/Level2/AnnotationPaper.cs").Contains("never yellow before inking") && toolkitSrc6.Contains("if (sweep.LineId != null && sweep.Locked) paper.SetStroke(sweep.LineId, sweep.Min, sweep.Max)"),
    "ink appears only where the nib has swept, never as a yellow band beforehand");
int beginInkStart = toolkitSrc6.IndexOf("private void BeginInk(", StringComparison.Ordinal);
int beginInkEnd = toolkitSrc6.IndexOf("private bool TryPaperPoint(", beginInkStart, StringComparison.Ordinal);
string beginInkBody = toolkitSrc6.Substring(beginInkStart, beginInkEnd - beginInkStart);
int finishStrokeStart = toolkitSrc6.IndexOf("private IEnumerator FinishStroke(", StringComparison.Ordinal);
int finishStrokeEnd = toolkitSrc6.IndexOf("private IEnumerator TraceAndReturn(", finishStrokeStart, StringComparison.Ordinal);
string finishStrokeBody = toolkitSrc6.Substring(finishStrokeStart, finishStrokeEnd - finishStrokeStart);
Check(beginInkBody.Contains("IsLocked(Challenge, entry.id, AnnotationSlot.Highlight)") && beginInkBody.IndexOf("return;", StringComparison.Ordinal) < beginInkBody.IndexOf("inking = true", StringComparison.Ordinal) &&
      finishStrokeBody.Contains("if (!changed && mode == Mode.Challenge && IncomeChallenge.IsLocked(Challenge, entry.id, AnnotationSlot.Highlight))") &&
      finishStrokeBody.Contains("ApplyHighlight(instant: true);") && finishStrokeBody.Contains("yield break;"),
    "a confirmed highlight cannot preview another line or commit rejected ink visuals");
int endInkStart = toolkitSrc6.IndexOf("private void EndInk()", StringComparison.Ordinal);
int endInkEnd = toolkitSrc6.IndexOf("private void CancelInk(", endInkStart, StringComparison.Ordinal);
string endInkBody = toolkitSrc6.Substring(endInkStart, endInkEnd - endInkStart);
Check(endInkBody.Contains("if (inkFromMarker && !inkMoved)") && endInkBody.Contains("LiftMarkerAnimated();") &&
      endInkBody.Contains("if (inkFromMarker) LiftMarkerAnimated();"),
    "tapping the highlighter stages it beside the paper, and a short swipe leaves it ready to retry");
Check(toolkitSrc6.Contains("markerPickupPosition = markerRestPosition +") && toolkitSrc6.Contains("markerPickedUp && !inking && markerMotion == null"),
    "the picked-up highlighter has a raised, gently floating rest pose");
Check(Src("Scripts/RewardsSystem/LevelScoreLedger.cs").Contains("BadgeTier? accuracyTier = null") &&
      Src("Scripts/SideQuest1/TutorialNpcController.cs").Contains("AddSideQuest(") && !Src("Scripts/SideQuest1/TutorialNpcController.cs").Contains("accuracyTier"),
    "Level 1's side quest award call is unchanged");
foreach (string ev in new[] { "event:AnnotationsChecked", "event:IncomeQuestCompleted" })
    Check(ObjectiveTriggers.Validate(ev) == null && File.ReadAllText(Res("Levels/Level_02/objectives.json")).Contains(ev), "objective step observes " + ev);

// ================= T07 office reconciliation =================
CaseReviewDefinition LegacyReview() => JsonSerializer.Deserialize<CaseReviewDefinition>(File.ReadAllText(Path.Combine(root, "Tests", "Level2Regression", "LegacyReview.json")), fieldJson);
var od = LegacyReview();
Check(od.scheduleSources.Count == 10 && od.scheduleSlots == 4 && od.officeFindings.Select(f => f.id).SequenceEqual(new[] { "F1", "F2" }) &&
      od.conclusions.Count == 3 && od.officeRule.IsSet, "office review data: 10 schedulable record lines, 4 schedule lines, findings F1 and F2, 3 conclusions");
// Rosa's current case takes its schedule values from the annotated papers (Mark the Money!), not record rows.
var ro = rosa.review;
Check(ro.scheduleSlots == 3 && ro.scheduleSlotLabels.SequenceEqual(new[] { "Summary line 1", "Summary line 2", "Missing entry" }),
    "Rosa's Reviewed Schedule: two summary lines plus one missing entry");
foreach (string giveaway in new[] { "clothes", "washing", "rental", "washer", "blanket", "loan", "12,000", "5,000", "3,000" })
    Check(ro.scheduleSlotLabels.All(l => l.IndexOf(giveaway, StringComparison.OrdinalIgnoreCase) < 0),
        "schedule captions never name the work or the amount: " + giveaway);
Check(ro.scheduleSources.Count == 4 && ro.scheduleSources.All(src => !string.IsNullOrEmpty(src.entryId)) &&
      ro.scheduleSources.Select(src => src.entryId).SequenceEqual(new[] { "E1", "E2", "E3", "E4" }),
    "every schedule value comes from one annotated paper (E1-E4), one source each");
foreach (var source in ro.scheduleSources)
{
    var entry = ro.annotation.entries.Single(e => e.id == source.entryId);
    var highlight = entry.lines.Single(l => l.id == ro.FindKey(entry.id).highlightLineId);
    Check(Math.Abs(highlight.amount - source.amount) < 0.01f && CaseFolderUI.TryParseAmount(highlight.value, out float shown) && Math.Abs(shown - source.amount) < 0.01f,
        "schedule amount is the annotated paper's highlighted line: " + source.semanticKey);
    Check(ro.FindScheduleSourceForEntry(entry.id) == source, "the annotated paper finds its own schedule source: " + entry.id);
}
Check(ro.ScheduleSlotLabel(2) == "Missing entry" && ro.ScheduleSlotLabel(7) == "May line 8" && od.ScheduleSlotLabel(0) == "May line 1",
    "slot captions come from data, falling back to May line N for older cases");
Check(ro.officeFindings.Single().parts.Select(pt => pt.accepts).SequenceEqual(new[] { "conclusion" }),
    "the unlisted payment is the missing-entry line itself; only its conclusion sits below the totals");
var badLabels = Clone();
badLabels.scheduleSlotLabels.RemoveAt(0);
Check(CaseReviewValidator.Validate(badLabels).Any(x => x.Contains("scheduleSlotLabels")), "every schedule line needs a caption when captions are given");
var badEntry = Clone();
badEntry.scheduleSources[0].entryId = "E9";
Check(CaseReviewValidator.Validate(badEntry).Any(x => x.Contains("unknown annotation entry")), "a schedule source must name a real annotated paper");
var twice = Clone();
twice.scheduleSources[1].entryId = "E1";
Check(CaseReviewValidator.Validate(twice).Any(x => x.Contains("more than one schedule source")), "one annotated paper supplies one schedule value");

CaseReviewState RosaOffice()
{
    var s = new CaseReviewState { reviewId = "L2-ROSA-001", phase = ReviewPhase.OfficeReconciliation,
        challenge = new ChallengeState { mastered = true, awarded = true, firstChecked = true, firstCorrect = 16 } };
    foreach (string id in new[] { "COVER", "S1", "N1", "D1", "D3", "D4", "D5" }) s.Acquire(id);
    s.findings.Add(new ReviewFindingState { id = "F1", concernDocument = "S1", concernKey = "SummarySubtotal", concernLabel = "May total" });
    return s;
}
bool Put(CaseReviewState s, int i, string entry) => OfficeReview.SetScheduleRow(s, ro, i, "Annotated" + entry, entry, entry, out _);
CaseReviewState RosaSupported()
{
    var s = RosaOffice();
    Put(s, 0, "E1"); Put(s, 1, "E3"); Put(s, 2, "E2");
    OfficeReview.SetPart(s, ro, "F1", "conclusion", null, "corrected-included", "Corrected: included");
    return s;
}
var rosaGood = OfficeReview.Evaluate(RosaSupported(), ro);
Check(rosaGood.Supported && rosaGood.MayTotal == 20000f && rosaGood.AnnualTotal == 140000f && OfficeReview.StructurallyComplete(RosaSupported(), ro),
    "Rosa: E1 + E3 listed, E2 restored, conclusion Corrected = May 20,000 and year 140,000, no root causes");
var rosaLoan = RosaOffice();
Put(rosaLoan, 0, "E1"); Put(rosaLoan, 1, "E3"); Put(rosaLoan, 2, "E4");
OfficeReview.SetPart(rosaLoan, ro, "F1", "conclusion", null, "corrected-included", "Corrected: included");
var rosaLoanResult = OfficeReview.Evaluate(rosaLoan, ro);
Check(rosaLoanResult.RootCauses.SequenceEqual(new[] { "omission", "not-income-counted:loan-LN0510" }) && rosaLoanResult.MayTotal == 35000f,
    "Rosa: the loan in the missing-entry line is the omission plus a counted loan (the trap is allowed in, then graded)");
var rosaNoConclusion = RosaOffice();
Put(rosaNoConclusion, 0, "E1"); Put(rosaNoConclusion, 1, "E3"); Put(rosaNoConclusion, 2, "E2");
Check(!OfficeReview.StructurallyComplete(rosaNoConclusion, ro), "Rosa: the review is not complete until the conclusion is filled");
var rosaWrongConclusion = RosaSupported();
OfficeReview.SetPart(rosaWrongConclusion, ro, "F1", "conclusion", null, "explained-no-adjustment", "Explained: no change");
Check(OfficeReview.Evaluate(rosaWrongConclusion, ro).RootCauses.SequenceEqual(new[] { "f1-unsupported" }),
    "Rosa: 'no change' after adding the missing entry does not describe the work");
var rosaOmitted = RosaSupported();
OfficeReview.ClearScheduleRow(rosaOmitted, 2);
Check(OfficeReview.Evaluate(rosaOmitted, ro).RootCauses.SequenceEqual(new[] { "omission" }),
    "Rosa: leaving the missing entry empty is one omission (its conclusion is the same defect)");
var rosaTwice = RosaSupported();
Check(!OfficeReview.SetScheduleRow(rosaTwice, ro, 2, "AnnotatedE1", "E1", "E1", out var rosaHeld) && rosaHeld?.semanticKey == "AnnotatedE1",
    "Rosa: the same annotated payment cannot go on two lines");
Check(!OfficeReview.SetScheduleRow(RosaOffice(), ro, 0, "D3Amount", "D3", "D3", out _),
    "Rosa: record rows are evidence to read, not schedule values");
var rosaGoodState = RosaSupported();
Check(FinalAssessment.GenerateSummary(rosaGoodState, ro).Contains("₱20,000") && FinalAssessment.GenerateSummary(rosaGoodState, ro).Contains("Missing entry: Corrected: included"),
    "Rosa: the handwritten summary states the total and the missing-entry conclusion");
var retiredSave = RosaOffice();
retiredSave.schedule = new List<ScheduleRowState> {
    new ScheduleRowState { semanticKey = "AnnotatedE1", amount = 12000f, moneyIds = new List<string> { "clothes-May" } },
    new ScheduleRowState { semanticKey = "D3Amount", amount = 5000f, moneyIds = new List<string> { "job-CM0526" } },
    null,
    new ScheduleRowState { semanticKey = "D4Amount", amount = 3000f, moneyIds = new List<string> { "rental-RA0520" } } };
int retiredRevision = retiredSave.packetRevision;
Check(OfficeReview.DropRetiredRows(retiredSave, ro) && retiredSave.schedule.Count == 3 && OfficeReview.MayTotal(retiredSave) == 12000f &&
      retiredSave.packetRevision == retiredRevision + 1 && !OfficeReview.DropRetiredRows(retiredSave, ro),
    "an older checkpoint's record-line rows and hidden fourth line are emptied once, so nothing unseen counts");
string officePlayerText = string.Join("\n", od.officeFindings.SelectMany(f => f.parts.Select(p => p.label)).Concat(od.officeFindings.Select(f => f.title)).Concat(od.conclusions.Select(c => c.label)));
foreach (string word in new[] { "20,000", "140,000", "5,000", "cash", "D3", "D6", "loan", "Ready" })
    Check(officePlayerText.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0, "office pages and conclusions carry no case answer: " + word);

CaseReviewState Office()
{
    var s = new CaseReviewState { reviewId = "L2-ROSA-001", phase = ReviewPhase.OfficeReconciliation };
    foreach (string id in new[] { "COVER", "S1", "N1", "D1", "D2", "D3", "D4", "D5", "D6" }) s.Acquire(id);
    s.findings.Add(new ReviewFindingState { id = "F1", concernDocument = "S1", concernKey = "SummarySubtotal", concernLabel = "May total" });
    return s;
}
bool Line(CaseReviewState s, int i, string key) => OfficeReview.SetScheduleRow(s, od, i, key, key.Substring(0, 2), key, out _);
void Finding(CaseReviewState s, string id, params (string part, string key)[] parts)
{
    foreach (var (part, k) in parts) OfficeReview.SetPart(s, od, id, part, null, k, k);
}
CaseReviewState Supported()
{
    var s = Office();
    Line(s, 0, "D1Total"); Line(s, 1, "D3Amount"); Line(s, 2, "D4Amount");
    Finding(s, "F1", ("record", "D3Amount"), ("conclusion", "corrected-included"));
    Finding(s, "F2", ("entry", "D1Total"), ("compare", "D2Services"), ("clue", "D6Amount"), ("reason", "paid-another-way"), ("conclusion", "explained-no-adjustment"));
    return s;
}
var good = Supported();
var goodResult = OfficeReview.Evaluate(good, od);
Check(goodResult.Supported && goodResult.MayTotal == 20000f && goodResult.AnnualTotal == 140000f,
    "supported review: May 20,000 and annual 140,000 from the player's own lines, no root causes");
Check(OfficeReview.StructurallyComplete(good, od), "a schedule line and every finding box filled is structurally complete");
var split = Office();
Line(split, 0, "D1Bank"); Line(split, 1, "D1Cash"); Line(split, 2, "D3Amount"); Line(split, 3, "D4Amount");
Finding(split, "F1", ("record", "D3Invoice"), ("conclusion", "corrected-included"));
Finding(split, "F2", ("entry", "D2Services"), ("compare", "D1Total"), ("clue", "D6Recorded"), ("reason", "paid-another-way"), ("conclusion", "explained-no-adjustment"));
Check(OfficeReview.Evaluate(split, od).Supported && OfficeReview.MayTotal(split) == 20000f,
    "bank and cash lines separately also total 20,000; F2 entry/compare may come in either order");

var omitted = Supported();
OfficeReview.ClearScheduleRow(omitted, 1);
var omittedResult = OfficeReview.Evaluate(omitted, od);
Check(omittedResult.RootCauses.SequenceEqual(new[] { "omission" }) && omittedResult.MayTotal == 15000f,
    "omitted blanket job: one root cause (F1 'corrected' without the line is the same defect, counted once)");
var loan = Supported();
Line(loan, 3, "D5Principal");
var loanResult = OfficeReview.Evaluate(loan, od);
Check(loanResult.RootCauses.SequenceEqual(new[] { "not-income-counted:loan-LN0510" }) && loanResult.MayTotal == 40000f && loanResult.AnnualTotal == 160000f,
    "loan added to income: it is allowed in, the total follows the player's lines, and the evaluator names it once");
var doubled = Supported();
Check(!OfficeReview.SetScheduleRow(doubled, od, 3, "D6Amount", "D6", "D6", out var heldBy) && heldBy?.semanticKey == "D1Total" &&
      OfficeReview.MayTotal(doubled) == 20000f, "D6 cash cannot be added again: the box names the line that already counts it");
Check(!OfficeReview.SetScheduleRow(doubled, od, 3, "D2Services", "D2", "D2", out heldBy) && heldBy?.semanticKey == "D1Total",
    "the bank credit of collected payments cannot be counted on top of the collection total");
Check(!OfficeReview.SetScheduleRow(doubled, od, 3, "D2Credits", "D2", "D2", out _), "total bank credits overlap the collection money too");
var falseFlag = Supported();
Finding(falseFlag, "F2", ("conclusion", "unresolved"));
Check(OfficeReview.Evaluate(falseFlag, od).RootCauses.SequenceEqual(new[] { "f2-false-flag" }), "holding an explained difference is a false flag");
var corrected2 = Supported();
Finding(corrected2, "F2", ("conclusion", "corrected-included"));
Check(OfficeReview.Evaluate(corrected2, od).RootCauses.SequenceEqual(new[] { "f2-false-flag" }), "adjusting for an explained difference is a false flag too");
var badClue = Supported();
Finding(badClue, "F2", ("clue", "D5Principal"));
Check(OfficeReview.Evaluate(badClue, od).RootCauses.SequenceEqual(new[] { "f2-unexplained" }), "F2 explained only by the supported links and reason");
var badF1 = Supported();
Finding(badF1, "F1", ("record", "D6Amount"));
Check(OfficeReview.Evaluate(badF1, od).RootCauses.SequenceEqual(new[] { "f1-unsupported" }), "F1 corrected only with the blanket-job record");
var missingRental = Supported();
OfficeReview.ClearScheduleRow(missingRental, 2);
Check(OfficeReview.Evaluate(missingRental, od).RootCauses.SequenceEqual(new[] { "missing-income:rental-RA0520" }), "dropping an earned line is its own root cause");

var rev = Office();
int r0 = rev.packetRevision;
Check(Line(rev, 0, "D1Total") && rev.packetRevision == r0 + 1 && !Line(rev, 0, "D1Total") && rev.packetRevision == r0 + 1,
    "the same line in the same box is idempotent (no revision)");
Check(Line(rev, 0, "D3Amount") && rev.packetRevision == r0 + 2 && OfficeReview.MayTotal(rev) == 5000f, "replacing a line is one meaningful revision");
Check(!OfficeReview.SetScheduleRow(rev, od, 0, "SummarySubtotal", "S1", "S1", out _) && !OfficeReview.SetScheduleRow(rev, od, 7, "D4Amount", "D4", "D4", out _),
    "only schedulable record lines and real boxes are accepted (not the prepared summary)");
Check(OfficeReview.SetPart(rev, od, "F2", "reason", null, "amounts-differ", "Different totals") && !OfficeReview.SetPart(rev, od, "F2", "reason", null, "amounts-differ", "Different totals") &&
      !OfficeReview.SetPart(rev, od, "F9", "reason", null, "x", "x") && !OfficeReview.SetPart(rev, od, "F2", "nope", null, "x", "x"),
    "finding parts change once and only on real findings and parts");
Check(OfficeReview.ClearPart(rev, "F2", "reason") && !OfficeReview.ClearPart(rev, "F2", "reason"), "a filled box can be emptied once");
var locked = Office();
locked.phase = ReviewPhase.Audit;
Check(!Line(locked, 0, "D1Total") && !OfficeReview.CanEdit(new CaseReviewState { phase = ReviewPhase.Feedback }),
    "office review edits only from office reconciliation, never during the audit or before the quest");
var round7 = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(good, fieldJson), fieldJson);
Check(OfficeReview.Evaluate(round7, od).Supported && OfficeReview.MayTotal(round7) == 20000f && round7.officeFindings.Count == 2,
    "the reviewed schedule and findings survive a save round trip");
var blankRows = Supported();
blankRows.schedule.Add(new ScheduleRowState());
Check(OfficeReview.MayTotal(blankRows) == 20000f && OfficeReview.Evaluate(blankRows, od).Supported,
    "blank rows restored by Unity's JSON count as empty");
var s1Page = od.FindDocument("S1");
Check(s1Page.lines.Single(l => l.role == "subtotal").amount == 15000f && good.findings.Single().concernKey == "SummarySubtotal",
    "the prepared summary S1 is never edited (still 15,000) and the initial finding is untouched");
var gold = new CaseReviewState { reviewId = "L2-ROSA-001", phase = ReviewPhase.OfficeReconciliation, challenge = new ChallengeState { mastered = true, awarded = true, firstChecked = true, firstCorrect = 12 } };
Check(!OfficeReview.StructurallyComplete(gold, od) && !OfficeReview.Evaluate(gold, od).Supported,
    "a Gold quest alone leaves the office review incomplete and unsupported");
var noRule = LegacyReview();
noRule.officeRule = new OfficeReviewRuleDefinition();
Check(OfficeReview.Evaluate(Office(), noRule).Supported, "cases without an office rule are not evaluated here (Level 1 keeps its evaluator)");

var badOfficeSource = LegacyReview();
badOfficeSource.scheduleSources[0].moneyIds.Clear();
Check(CaseReviewValidator.Validate(badOfficeSource).Any(x => x.Contains("names no money")), "a schedule source must name its money");
var badOfficeRule = LegacyReview();
badOfficeRule.officeRule.findings[1].parts[3].accepted.Add("no-such-reason");
Check(CaseReviewValidator.Validate(badOfficeRule).Any(x => x.Contains("accepts unknown")), "office rule answers must be real reasons/conclusions");
var badOfficePart = LegacyReview();
badOfficePart.officeFindings[1].parts[0].accepts = "anything";
Check(CaseReviewValidator.Validate(badOfficePart).Any(x => x.Contains("accepts must be")), "office finding boxes accept record, reason or conclusion");

string officeSrc = Src("Scripts/Level2/Level2OfficeReview.cs");
Check(Src("Scripts/Level2/Level2FindingsPage.cs").Contains("Level2OfficeReview.AppendPages(pages, data)") &&
      Src("Scripts/Level2/Level2OfficeStart.cs").Contains("EnsurePart<Level2OfficeReview>(Level2OfficeReview.ChildName)"),
    "office pages are added through the existing folder page hook and Level 2 part setup");
Check(officeSrc.Contains("OfficeReview.SetScheduleRow(") && officeSrc.Contains("OfficeReview.SetPart(") && !officeSrc.Contains("officeRule") &&
      !officeSrc.Contains("Evaluate("), "the office pages write only through OfficeReview and never read the answer rule");
Check(officeSrc.Contains("already counted in") && officeSrc.Contains("slot.Cleared = ()"), "refusals say which line already counts the money; a tap empties a box");
Check(officeSrc.Contains("GameplayEvents.RaiseOfficeReviewCompleted()") && officeSrc.Contains("They stand as you wrote them"),
    "completion is announced as recorded, never as correct");
Check(Src("Scripts/Level2/FindingSlot.cs").Contains("Cleared?.Invoke();"), "finding boxes can be emptied by tapping them with nothing picked up");
Check(ObjectiveTriggers.Validate("event:OfficeReviewCompleted") == null && File.ReadAllText(Res("Levels/Level_02/objectives.json")).Contains("event:OfficeReviewCompleted"),
    "objective step observes OfficeReviewCompleted");

// The shipped Level 2 flow keeps legacy state readable, but grades only its compact tasks.
var compact = rosa.review;
Check(compact.simplifiedAssessment && compact.officeFindings.Count == 1 && compact.assessmentReasons.Count == 0,
    "Level 2 opts into one unlisted-payment task and a reason-free final stamp");
var compactGood = RosaSupported();
Check(OfficeReview.StructurallyComplete(compactGood, compact) && OfficeReview.Evaluate(compactGood, compact).Supported,
    "the compact review completes and supports a correct schedule without F2");
Check(FinalAssessment.TryStamp(compactGood, compact, true) == FinalAssessment.Outcome.Recorded &&
      FinalAudit.Evaluate(compactGood, compact, null).Outcome == FinalAudit.Outcome.Approved,
    "the compact final stamp needs no reason and still reaches auditor approval");
OfficeReview.ClearScheduleRow(compactGood, 1);
Check(FinalAssessment.IsStale(compactGood), "editing the compact schedule invalidates its stamp");
var compactWrong = RosaSupported();
OfficeReview.ClearScheduleRow(compactWrong, 2);
Check(FinalAssessment.TryStamp(compactWrong, compact, true) == FinalAssessment.Outcome.Recorded &&
      FinalAudit.Evaluate(compactWrong, compact, null).Outcome == FinalAudit.Outcome.PrematureApproval,
    "a structurally complete but wrong compact schedule can be stamped and is rejected by the auditor");
string compactSummary = FinalAssessment.GenerateSummary(compactWrong, compact);
Check(compactSummary.Contains("15,000") && !compactSummary.Contains("20,000") && !compactSummary.Contains("F2"),
    "the handwritten summary reports the player's entered total, not the answer or retired F2");
var legacyCompact = RosaSupported();
legacyCompact.assessmentReasonId = "legacy-reason";
legacyCompact.officeFindings.Add(new OfficeFindingState { id = "F2", parts = new List<OfficePartState> { new OfficePartState { partId = "reason", key = "paid-another-way" } } });
Check(FinalAssessment.TryStamp(legacyCompact, compact, true) == FinalAssessment.Outcome.Recorded &&
      FinalAudit.Evaluate(legacyCompact, compact, null).Outcome == FinalAudit.Outcome.Approved,
    "legacy checkpoint reasons and F2 entries do not block the compact review");
Check(!FinalAssessment.SetReason(legacyCompact, compact, "income-supported"),
    "retired final-reason choices cannot mutate a compact review");

// ================= T08 final assessment stamp =================
Check(od.assessmentReasons.Count == 5 && od.assessmentReasons.Count(r => r.verdict == "Ready") == 1 &&
      od.assessmentReasons.All(r => r.label.Length <= 24), "five generic assessment reasons, one for Ready");
var bookAssessment = Json("Levels/Level_02/book.json").GetProperty("sections").EnumerateArray()
    .Where(l => l.TryGetProperty("clickableValues", out _))
    .SelectMany(l => l.GetProperty("clickableValues").EnumerateArray())
    .Where(v => Str(v, "semanticKey") == "AssessmentReason").ToList();
Check(bookAssessment.Count == 0, "the simplified book has no final assessment reason choices");
foreach (var r in od.assessmentReasons)
    foreach (string word in new[] { "blanket", "5,000", "D3", "cash", "Rosa" })
        Check(r.label.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0, "assessment reason labels are generic: " + r.id);

var fa = Supported();
int faRev = fa.packetRevision;
Check(FinalAssessment.TryStamp(fa, od, true, 10f, 20f, 3f) == FinalAssessment.Outcome.NoReason && fa.finalStamp == null,
    "a reason is mandatory before stamping");
Check(fa.phase == ReviewPhase.FinalAssessment, "a structurally complete office review opens the final assessment");
Check(FinalAssessment.SetReason(fa, od, "income-supported") && fa.packetRevision == faRev + 1 && !FinalAssessment.SetReason(fa, od, "income-supported") &&
      !FinalAssessment.SetReason(fa, od, "made-up"), "choosing a reason is one revision; repeats and unknown reasons change nothing");
int beforeStamp = fa.packetRevision;
Check(FinalAssessment.TryStamp(fa, od, true, 10f, 20f, 3f) == FinalAssessment.Outcome.Recorded && FinalAssessment.IsCurrent(fa) &&
      fa.finalStamp.verdict == "Ready" && fa.finalStamp.revision == beforeStamp && fa.packetRevision == beforeStamp &&
      fa.finalStamp.hasInk && fa.finalStamp.inkX == 10f && fa.finalStamp.reasonId == "income-supported",
    "the stamp commits once to the current revision with its reason and ink, and stamping itself changes no revision");
Check(FinalAssessment.TryStamp(fa, od, true) == FinalAssessment.Outcome.AlreadyCurrent && fa.stampHistory.Count == 0,
    "pressing the same verdict again changes nothing (no double commit)");
Check(FinalAssessment.TryStamp(fa, od, false, 1f, 2f) == FinalAssessment.Outcome.Recorded && fa.finalStamp.verdict == "NotReady" &&
      fa.stampHistory.Count == 1 && fa.stampHistory[0].verdict == "Ready", "both stamps stay available; restamping keeps a compact history");
Finding(fa, "F2", ("reason", "amounts-differ"));
Check(FinalAssessment.IsStale(fa) && !FinalAssessment.IsCurrent(fa), "changing graded work makes the stamp stale (needs reassessment)");
FinalAssessment.TryStamp(fa, od, false);
Check(FinalAssessment.IsCurrent(fa) && fa.stampHistory.Count == 2, "restamping the changed review makes it current again");
for (int i = 0; i < 4; i++) FinalAssessment.TryStamp(fa, od, i % 2 == 0);
Check(fa.stampHistory.Count == FinalAssessment.HistoryLimit, "history stays compact (last 3), never stacked ink");
var notDone = Office();
Line(notDone, 0, "D1Total");
FinalAssessment.SetReason(notDone, od, "income-supported");
Check(FinalAssessment.TryStamp(notDone, od, true) == FinalAssessment.Outcome.OfficeIncomplete && notDone.finalStamp == null && notDone.phase == ReviewPhase.OfficeReconciliation,
    "only structural prerequisites: an incomplete office review cannot be stamped");
var wrongButComplete = Supported();
Line(wrongButComplete, 3, "D5Principal");
FinalAssessment.SetReason(wrongButComplete, od, "income-supported");
Check(FinalAssessment.TryStamp(wrongButComplete, od, true) == FinalAssessment.Outcome.Recorded, "a complete but wrong review can still be stamped (no hidden correctness lock)");
var earlyStamp = new CaseReviewState { phase = ReviewPhase.Challenge };
Check(FinalAssessment.TryStamp(earlyStamp, od, true) == FinalAssessment.Outcome.NotAvailable, "no final stamp before the office review");
var prelimKept = Supported();
prelimKept.preliminary = new PreliminaryAssessmentState { verdict = "NotReady", revision = 0 };
FinalAssessment.SetReason(prelimKept, od, "income-supported");
FinalAssessment.TryStamp(prelimKept, od, true);
Check(prelimKept.preliminary.verdict == "NotReady" && prelimKept.preliminary.revision == 0, "the initial stamp stays its own record");
var unityBlank = Supported();
unityBlank.finalStamp = new FinalAssessmentState();
Check(!FinalAssessment.IsCurrent(unityBlank) && !FinalAssessment.IsStale(unityBlank), "a blank stamp restored by Unity's JSON counts as no stamp");
var round8 = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(fa, fieldJson), fieldJson);
Check(FinalAssessment.IsCurrent(round8) && round8.stampHistory.Count == 3, "the stamp and history survive a save round trip");

string stampRouteSrc = Src("Scripts/Level2/Level2PreliminaryStamp.cs");
Check(stampRouteSrc.Contains("if (review.phase >= ReviewPhase.OfficeReconciliation) return StampFinal(") &&
      stampRouteSrc.Contains("FinalAssessment.TryStamp(review, definition.review, ready,"), "the desk stamps commit the final assessment once, at impact");
Check(Src("Scripts/Level2/Level2DeskStamps.cs").Contains("Folder.TurnToPage(StampPageIndex()") &&
      Src("Scripts/Level2/Level2DeskStamps.cs").Contains("Folder.FindPageIndex(Level2OfficeReview.FinalTitle)"),
    "using the stamps turns to the Final Assessment page; only that page takes the final ink");
Check(Src("Scripts/Level2/Level2OfficeReview.cs").Contains("Your recommendation (the Auditor decides)") &&
      Src("Scripts/Level2/Level2OfficeReview.cs").Contains("ReviewInkMark.SetStyle(ink, ready ? ReviewInkMark.InkGreen : ReviewInkMark.InkRed, current ? 1f : 0.3f)"),
    "the slip says the Auditor decides; stale ink fades instead of stacking");
Check(Src("Scripts/Level2/Level2FolderHandoff.cs").Contains("case FinalAudit.Outcome.NotStamped:") &&
      Src("Scripts/Level2/Level2FolderHandoff.cs").Contains("return !CanHandInReview(review, true);"), "hand-in requires a current final stamp");
Check(ObjectiveTriggers.Validate("event:FinalAssessmentStamped") == null && File.ReadAllText(Res("Levels/Level_02/objectives.json")).Contains("event:FinalAssessmentStamped"),
    "objective step observes FinalAssessmentStamped");

// ================= T09 Auditor outcomes and recovery =================
CaseReviewState Stamped(CaseReviewState s, bool ready, string reason)
{
    FinalAssessment.SetReason(s, od, reason);
    FinalAssessment.TryStamp(s, od, ready);
    return s;
}
var none9 = new string[0];
var approve = Stamped(Supported(), true, "income-supported");
var approveResult = FinalAudit.Submit(approve, od, none9);
Check(approveResult.Outcome == FinalAudit.Outcome.Approved && approve.phase == ReviewPhase.Completed && approve.audit.approved &&
      approveResult.MayTotal == 20000f && approveResult.AnnualTotal == 140000f && approveResult.NewlyCharged.Count == 0,
    "Ready on a supported packet with a Ready reason is approved and completes the review");
Check(FinalAudit.TryReward(approve) && !FinalAudit.TryReward(approve), "the case award is given exactly once");
Check(FinalAudit.Admission(approve) == FinalAudit.Outcome.NotStamped || approve.phase == ReviewPhase.Completed,
    "an approved review is finished");

var hold = Stamped(Office(), false, "payment-missing");
Line(hold, 0, "D1Total"); Line(hold, 1, "D4Amount");
Finding(hold, "F1", ("record", "D3Amount"), ("conclusion", "unresolved"));
Finding(hold, "F2", ("entry", "D1Total"), ("compare", "D2Services"), ("clue", "D6Amount"), ("reason", "paid-another-way"), ("conclusion", "explained-no-adjustment"));
Stamped(hold, false, "payment-missing");
var holdResult = FinalAudit.Submit(hold, od, none9);
Check(holdResult.Outcome == FinalAudit.Outcome.SupportedHold && hold.audit.holds == 1 && hold.audit.failedSubmissions == 0 &&
      hold.audit.charged.Count == 0 && hold.phase == ReviewPhase.Correction && !hold.audit.approved,
    "a justified Not Ready (the payment really is missing) is acknowledged: no cost, no reward, no completion");
Check(FinalAudit.Admission(hold) == FinalAudit.Outcome.Unchanged, "the same held packet cannot be handed in again unchanged (no hold spam)");
Line(hold, 2, "D3Amount");
Check(FinalAudit.Admission(hold) == FinalAudit.Outcome.NotStamped, "a changed packet needs a current stamp before the next hand-in");
Finding(hold, "F1", ("conclusion", "corrected-included"));
Stamped(hold, true, "income-supported");
Check(FinalAudit.Submit(hold, od, none9).Outcome == FinalAudit.Outcome.Approved && hold.audit.failedSubmissions == 0,
    "after a supported hold the corrected, restamped packet can be approved with no penalty");

var premature = Stamped(Supported(), true, "income-supported");
OfficeReview.ClearScheduleRow(premature, 1);
Stamped(premature, true, "income-supported");
var prematureResult = FinalAudit.Submit(premature, od, none9);
Check(prematureResult.Outcome == FinalAudit.Outcome.PrematureApproval && prematureResult.Defects.SequenceEqual(new[] { "omission" }) &&
      prematureResult.NewlyCharged.SequenceEqual(new[] { "omission" }) && premature.audit.failedSubmissions == 1,
    "Ready with the omission still there is returned; the omission is charged once (not again as a wrong verdict, total or reason)");
var feedbackLines = FinalAudit.Feedback(prematureResult, od.audit);
Check(feedbackLines.Count == 1 && feedbackLines[0].Contains("blanket-washing") && !feedbackLines[0].Contains("5,000") && !feedbackLines[0].Contains("20,000"),
    "premature-approval feedback points to the record, without giving the amounts");
Check(FinalAudit.Admission(premature) == FinalAudit.Outcome.Unchanged, "an unchanged returned packet cannot be resubmitted");
Line(premature, 0, "D1Bank"); Line(premature, 1, "D1Cash"); // a meaningful change (same money), the omission remains
Stamped(premature, true, "income-supported");
var again = FinalAudit.Submit(premature, od, none9);
Check(again.Outcome == FinalAudit.Outcome.PrematureApproval && again.NewlyCharged.Count == 0 && premature.audit.failedSubmissions == 2 &&
      premature.audit.charged.Count == 1, "the same defect returned again is not charged twice; the extra submission counts once");

var falseHold = Stamped(Supported(), false, "difference-unexplained");
var falseHoldResult = FinalAudit.Submit(falseHold, od, none9);
Check(falseHoldResult.Outcome == FinalAudit.Outcome.FalseHold && falseHoldResult.NewlyCharged.SequenceEqual(new[] { "false-hold" }),
    "Not Ready on a supported packet (the difference is explained) is an unsupported hold");
var flagged = Supported();
Finding(flagged, "F2", ("conclusion", "unresolved"));
Stamped(flagged, false, "difference-unexplained");
var flaggedResult = FinalAudit.Submit(flagged, od, none9);
Check(flaggedResult.Outcome == FinalAudit.Outcome.FalseHold && flaggedResult.NewlyCharged.SequenceEqual(new[] { "false-hold" }),
    "marking the explained difference unresolved and holding is one false-hold root cause");

var wrongReady = Stamped(Supported(), true, "payment-missing");
var wrongReadyResult = FinalAudit.Submit(wrongReady, od, none9);
Check(wrongReadyResult.Outcome == FinalAudit.Outcome.ReasonError && wrongReadyResult.NewlyCharged.SequenceEqual(new[] { "reason-unsupported" }),
    "Ready on a correct packet with an unrelated reason is returned for the reason alone");
var wrongHold = Stamped(Supported(), true, "income-supported");
OfficeReview.ClearScheduleRow(wrongHold, 1);
Stamped(wrongHold, false, "non-income-counted");
var wrongHoldResult = FinalAudit.Submit(wrongHold, od, none9);
Check(wrongHoldResult.Outcome == FinalAudit.Outcome.ReasonError && wrongHoldResult.NewlyCharged.Contains("omission") && wrongHoldResult.NewlyCharged.Contains("reason-unsupported"),
    "a hold that names a defect that is not there is a reason error");

var fieldErr = Stamped(Supported(), true, "income-supported");
var fieldResult = FinalAudit.Submit(fieldErr, od, new[] { "taxpayerType" });
Check(fieldResult.Outcome == FinalAudit.Outcome.PrematureApproval && fieldResult.Defects.SequenceEqual(new[] { "field:taxpayerType" }) &&
      FinalAudit.Feedback(fieldResult, od.audit).Single().Contains("first page"), "inherited required-field errors still block approval, with their own feedback");
var fieldHold = Stamped(Supported(), false, "facts-wrong");
Check(FinalAudit.Submit(fieldHold, od, new[] { "incomeSource" }).Outcome == FinalAudit.Outcome.SupportedHold, "holding for a wrong taxpayer fact is justified");
var unstamped = Supported();
Check(FinalAudit.Submit(unstamped, od, none9).Outcome == FinalAudit.Outcome.NotStamped && unstamped.audit.failedSubmissions == 0,
    "no current stamp: refused before review, nothing recorded");

// Reward caps and the worst-run pass bound.
var capRules = l2Rules;
Check(capRules.caseFile.maxExpLoss == 15 && capRules.caseFile.maxHeartLoss == 1 && capRules.caseFile.expPerMistake == 2 && capRules.caseFile.expPerRetry == 1,
    "Level 2 case caps: 2 EXP per root cause, 1 per returned submission, at most 15 EXP and 1 heart");
var worst = LevelScoring.Case(capRules, 10, 10, 3, 1);
Check(worst.Exp == 85 && worst.Hearts == 4, "the worst capped case still earns 85 EXP and 4 hearts");
Check(LevelScoring.Case(capRules, 0, 0).Exp == 100 && LevelScoring.Case(capRules, 0, 0).Hearts == 5, "a clean first approval earns the full case reward");
Check(LevelScoring.Case(capRules, 1, 1).Exp == 97 && LevelScoring.Case(capRules, 1, 1).Hearts == 4, "one charged defect and one return cost 3 EXP and the single heart");
int worstExp = LevelScoring.SideQuest(capRules, 12).Exp + worst.Exp;
int worstHearts = LevelScoring.SideQuest(capRules, 12).Hearts + worst.Hearts + capRules.conversation.hearts;
Check(worstExp == 133 && worstHearts == 8 && LevelScoring.Passed(capRules, worstExp, worstHearts, 160, 9),
    "worst completed run (quest floor + capped case + Rosa) is 133 EXP / 8 hearts and still passes");
var l1Case = LevelScoring.Case(new LevelRewardRules(), 2, 1);
var l1CaseRules = new LevelRewardRules();
Check(l1Case.Exp == Math.Max(0, l1CaseRules.caseFile.exp - 2 * l1CaseRules.caseFile.expPerMistake - l1CaseRules.caseFile.expPerRetry), "Level 1 case scoring is unchanged (no caps)");

string auditSrc = Src("Scripts/Level2/Level2Audit.cs");
string auditorSrc = Src("Scripts/Interactables/AuditorInteractable.cs");
Check(auditorSrc.Contains("if (LevelAudit != null && LevelAudit(this)) return;") && auditSrc.Contains("AuditorInteractable.LevelAudit = Handle"),
    "Level 2 judges hand-ins through a hook; Level 1's audit runs when no level hook handles it");
Check(Regex.IsMatch(auditSrc, @"FinalAudit\.Result result = FinalAudit\.Submit\([\s\S]*?RunLevelDialogue\("), "the outcome is recorded once, before the dialogue");
Check(auditSrc.Contains("if (!FinalAudit.TryReward(review)) { inProgress = false; return; }") && auditSrc.Contains("CalculateAndStoreCaseReward(data)") &&
      Regex.Matches(auditSrc, @"CalculateAndStoreCaseReward\(").Count == 1, "only an accepted packet awards the case, once");
Check(auditSrc.Contains("RecordAuditAttempt(result.NewlyCharged.Count, false)") && !auditSrc.Contains("ResetCurrentCaseForRetry"),
    "returns charge only new root causes and never reset the review work");
Check(Src("Scripts/Data/CaseProgressionManager.cs").Contains("Score.AddCase(label, caseAuditMistakes, caseAuditAttempts - 1, CaseExtraExpLoss, CaseExtraHeartLoss)"),
    "the case award applies the level's extra (late) loss inside the caps");
Check(ObjectiveTriggers.Validate("event:AuditReturned") == null, "AuditReturned is a valid trigger");

// ================= T10 review cutoff =================
var cd = od.clock;
Check(cd.IsSet && cd.allowanceSeconds == 720f && cd.warnSeconds.SequenceEqual(new[] { 120f, 30f }) && cd.lateExp == 3 && cd.lateHearts == 1 &&
      cd.announceLine.Contains("{deadline}") && cd.nextMorningLine.Contains("{deadline}"), "review cutoff data: 12 active minutes, warnings at 2:00 and 0:30, one late consequence");
var ck = new ReviewClockState();
Check(ReviewClock.Advance(ck, cd, 5f, false, out _) == ReviewClock.Tick.None && ck.remaining == 0f, "the clock does nothing before the opening announcement");
Check(ReviewClock.Start(ck, cd) && !ReviewClock.Start(ck, cd) && ck.remaining == 720f && ck.day == 1, "the clock starts once with the full allowance");
ReviewClock.Advance(ck, cd, 100f, true, out _);
Check(ck.remaining == 720f, "compulsory teaching, conversations, cards and menus pause the clock");
ReviewClock.Advance(ck, cd, 500f, false, out _);
Check(ck.remaining == 220f && ReviewClock.WindowOpen(ck), "independent reading and solving consume active time");
Check(ReviewClock.Advance(ck, cd, 101f, false, out float m1) == ReviewClock.Tick.Warning && m1 == 120f &&
      ReviewClock.Advance(ck, cd, 1f, false, out _) == ReviewClock.Tick.None, "the two-minute warning fires once");
Check(ReviewClock.Advance(ck, cd, 90f, false, out float m2) == ReviewClock.Tick.Warning && m2 == 30f, "the 30-second warning fires once");
Check(ReviewClock.Advance(ck, cd, 100f, false, out _) == ReviewClock.Tick.Cutoff && ck.cutoffPending && !ReviewClock.WindowOpen(ck) &&
      ReviewClock.Advance(ck, cd, 100f, false, out _) == ReviewClock.Tick.None, "at zero the window closes once");
Check(!ReviewClock.TryRollover(ck, cd, actionInFlight: true, caseDone: false, out _) && ck.cutoffPending && ck.day == 1,
    "the rollover waits while a stamp, drag or conversation owns the action");
Check(ReviewClock.TryRollover(ck, cd, false, false, out bool firstMiss) && firstMiss && ck.day == 2 && ck.remaining == 720f &&
      ck.missed && ReviewClock.WindowOpen(ck) && ck.warned.Count == 0, "next morning: fresh allowance, warnings reset, first miss marked");
ReviewClock.Advance(ck, cd, 720f, false, out _);
Check(ReviewClock.TryRollover(ck, cd, false, false, out bool secondMiss) && !secondMiss && ck.day == 3, "later misses add no consequence");
var doneClock = new ReviewClockState();
ReviewClock.Start(doneClock, cd);
ReviewClock.Advance(doneClock, cd, 720f, false, out _);
Check(!ReviewClock.TryRollover(doneClock, cd, false, true, out bool doneMiss) && !doneMiss && doneClock.day == 1 && !doneClock.missed,
    "an approved case never rolls over or counts as late");
Check(ReviewClock.Format(720f) == "12:00" && ReviewClock.Format(29.2f) == "0:30" && ReviewClock.Format(-3f) == "0:00", "time left formats as m:ss");
var lateCase = LevelScoring.Case(l2Rules, 0, 0, cd.lateExp, cd.lateHearts);
Check(lateCase.Exp == 97 && lateCase.Hearts == 4, "the first miss costs 3 EXP and the single case heart");
Check(LevelScoring.Case(l2Rules, 8, 3, cd.lateExp, cd.lateHearts).Exp == 85 && LevelScoring.Case(l2Rules, 8, 3, cd.lateExp, cd.lateHearts).Hearts == 4,
    "late plus audit mistakes stay inside the 15 EXP / 1 heart cap");
// Exact-cutoff race: a closed window refuses the hand-in; an approval already decided is not undone by the rollover.
var raced = Stamped(Supported(), true, "income-supported");
raced.clock = new ReviewClockState();
ReviewClock.Start(raced.clock, cd);
ReviewClock.Advance(raced.clock, cd, 720f, false, out _);
Check(!ReviewClock.WindowOpen(raced.clock), "at the cutoff hand-ins close (the folder handoff checks the window first)");
var racedApproved = Stamped(Supported(), true, "income-supported");
racedApproved.clock = new ReviewClockState();
ReviewClock.Start(racedApproved.clock, cd);
FinalAudit.Submit(racedApproved, od, none9);
ReviewClock.Advance(racedApproved.clock, cd, 720f, false, out _);
Check(racedApproved.audit.approved && !ReviewClock.TryRollover(racedApproved.clock, cd, false, racedApproved.phase == ReviewPhase.Completed, out _) &&
      !racedApproved.clock.missed, "an approval decided before the cutoff produces one result: no rollover, no late charge");

var faceClock = new ReviewClockState();
Check(ReviewClock.FormatTimeOfDay(ReviewClock.GameSeconds(faceClock, cd)) == "7:30 AM", "unstarted office clock shows morning, not an expired allowance");
ReviewClock.Start(faceClock, cd);
Check(ReviewClock.FormatTimeOfDay(ReviewClock.GameSeconds(faceClock, cd)) == "7:30 AM" &&
      ReviewClock.FormatTimeOfDay(ReviewClock.DeadlineSeconds(faceClock, cd)) == "9:30 AM", "Level 2 shares Level 1 time-of-day presentation over its 12 active minutes");
ReviewClock.Advance(faceClock, cd, 360f, false, out _);
Check(ReviewClock.FormatTimeOfDay(ReviewClock.GameSeconds(faceClock, cd)) == "8:30 AM", "six active minutes advance the office clock one hour");
var faceResume = JsonSerializer.Deserialize<ReviewClockState>(JsonSerializer.Serialize(faceClock, fieldJson), fieldJson);
Check(ReviewClock.GameSeconds(faceResume, cd) == ReviewClock.GameSeconds(faceClock, cd), "checkpoint resume reconstructs the same clock face");
ReviewClock.Advance(faceClock, cd, 360f, false, out _);
Check(ReviewClock.GameSeconds(faceClock, cd) == ReviewClock.DeadlineSeconds(faceClock, cd), "cutoff face reaches its announced deadline");
ReviewClock.TryRollover(faceClock, cd, false, false, out _);
Check(ReviewClock.FormatTimeOfDay(ReviewClock.GameSeconds(faceClock, cd)) == "8:00 AM" &&
      ReviewClock.FormatTimeOfDay(ReviewClock.DeadlineSeconds(faceClock, cd)) == "10:00 AM", "next morning starts at eight with the refreshed cutoff");

string clockSrc = Src("Scripts/Level2/Level2ReviewClock.cs");
Check(clockSrc.Contains("GameplayEvents.OnNotesUnlockRequested += Announce") && clockSrc.Contains("ReviewClock.Start(review.clock, clock)"),
    "the clock starts at the receptionist's opening announcement");
Check(clockSrc.Contains("DialogueUI.IsAnyDialogueActive") && clockSrc.Contains("guide.IsBlocking") && clockSrc.Contains("toolkit.DemoRunning") &&
      clockSrc.Contains("HudRewardScreen.AnyOpen") && clockSrc.Contains("InGameSettings.IsOpen"), "pause predicate covers teaching, conversations, cards and menus");
Check(clockSrc.Contains("if (review.clock.cutoffPending && !ActionInFlight)") && clockSrc.Contains("stamps.Busy"),
    "the rollover waits for in-flight stamps and conversations");
Check(clockSrc.Contains("CaseExtraExpLoss = Mathf.Max(0, clock.lateExp)") && clockSrc.Contains("if (firstMiss && CaseProgressionManager.Instance != null)"),
    "only the first miss sets the late consequence");
Check(!clockSrc.Contains("Auditor") || !clockSrc.Contains("SetActive(false); // auditor"), "the Auditor is never hidden by the Level 2 clock");
Check(Src("Scripts/UI/HUD/OfficeHours.cs").Contains("private const string TargetSceneName = \"Office_Level1\";"), "Level 1 office hours (Auditor leaves) stay Level 1 only");
Check(Src("Scripts/Level2/Level2FolderHandoff.cs").Contains("!ReviewClock.WindowOpen(review.clock)"), "a closed window refuses the hand-in");
Check(Src("Scripts/Level2/Level2Audit.cs").Contains("if (review.clock != null && review.clock.started && !review.clock.missed)"),
    "on-time recognition only for an approval before the first cutoff");

// ================= T11 resume, story fact and finish =================
Check(!Level2Checkpoint.ShouldUseCheckpoint(true, true), "direct Editor Play starts fresh and cannot overwrite a saved checkpoint");
Check(Level2Checkpoint.ShouldUseCheckpoint(true, false), "Editor career-board entry resumes the durable attempt");
Check(Level2Checkpoint.ShouldUseCheckpoint(false, true) && Level2Checkpoint.ShouldUseCheckpoint(false, false),
    "player builds keep checkpoint resume/save regardless of startup scene");
var save11 = new PlayerProgressData();
save11.Normalize();
Check(save11.CheckpointFor("Level_02") == null && save11.storyFacts.Count == 0, "a fresh save has no checkpoint and no story facts");
var oldSave = JsonSerializer.Deserialize<PlayerProgressData>("{\"saveVersion\":4,\"totalExp\":10}", fieldJson);
oldSave.activeCheckpoint = null; oldSave.storyFacts = null;
Check(oldSave.Normalize() && oldSave.activeCheckpoint != null && oldSave.CheckpointFor("Level_02") == null && oldSave.storyFacts != null && oldSave.totalExp == 10,
    "an old save without a checkpoint defaults safely (and keeps its totals)");
Check(PlayerProgressData.CurrentSaveVersion == 5 && PlayerProgressData.LegacySaveVersion == 4 && PlayerProgressData.SignedSinceVersion == 3, "the save version (5 = campaign, 4 without) and signature contract are as expected");
var mid = Stamped(Supported(), true, "income-supported");
mid.challenge = new ChallengeState { firstChecked = true, firstCorrect = 14, firstTotal = 16, mastered = true, awarded = true, checks = 2 };
mid.challenge.firstWrong.AddRange(new[] { "E2.Type", "E4.Evidence" });
mid.audit.failedSubmissions = 1; mid.audit.charged.Add("omission");
mid.clock = new ReviewClockState { started = true, day = 2, remaining = 300f, missed = true };
mid.preliminary = new PreliminaryAssessmentState { verdict = "NotReady", findingId = "F1", reasonId = "unaccounted-payment", revision = 1 };
string midJson = JsonSerializer.Serialize(mid, fieldJson);
var cp = new LevelCheckpoint { levelId = "Level_02", caseId = "L2-ROSA-001", reviewJson = midJson, taxpayerType = "SelfEmployed" };
save11.SetCheckpoint(cp);
save11.SetCheckpoint(new LevelCheckpoint { levelId = "Level_02", caseId = "L2-ROSA-001", reviewJson = midJson });
Check(save11.CheckpointFor("Level_02")?.writes == 2 && save11.CheckpointFor("Level_01") == null, "one checkpoint per level run, rewritten in place");
var reloaded = JsonSerializer.Deserialize<PlayerProgressData>(JsonSerializer.Serialize(save11, fieldJson), fieldJson);
var back = JsonSerializer.Deserialize<CaseReviewState>(reloaded.CheckpointFor("Level_02").reviewJson, fieldJson);
Level2Checkpoint.Normalize(back);
Check(back.challenge.firstCorrect == 14 && back.challenge.firstWrong.Count == 2 && back.challenge.awarded && back.audit.failedSubmissions == 1 &&
      back.audit.charged.SequenceEqual(new[] { "omission" }) && back.clock.day == 2 && back.clock.missed && back.clock.remaining == 300f &&
      FinalAssessment.IsCurrent(back) && OfficeReview.MayTotal(back) == 20000f && back.preliminary.verdict == "NotReady",
    "relaunch restores the frozen grade, award flag, penalties, clock, schedule, findings and current stamp");
Check(!IncomeChallenge.TryAward(back) && LevelScoring.AccuracyBadge(l2Rules, back.challenge.firstCorrect) == BadgeTier.Silver,
    "a resumed run cannot be awarded again and keeps its frozen tier");
Check(Level2Checkpoint.Usable(back, "L2-ROSA-001") && !Level2Checkpoint.Usable(back, "OTHER") &&
      !Level2Checkpoint.Usable(new CaseReviewState { reviewId = "L2-ROSA-001", phase = ReviewPhase.Completed }, "L2-ROSA-001"),
    "only an unfinished checkpoint of the same review is resumed");
var blankRestore = new CaseReviewState { reviewId = "L2-ROSA-001", phase = ReviewPhase.Challenge, preliminary = new PreliminaryAssessmentState(),
    finalStamp = new FinalAssessmentState(), tutorialAnnotation = new AnnotationEntryState(), schedule = null, audit = null, clock = null };
Level2Checkpoint.Normalize(blankRestore);
Check(blankRestore.preliminary == null && blankRestore.finalStamp == null && blankRestore.tutorialAnnotation == null &&
      blankRestore.schedule != null && blankRestore.audit != null && blankRestore.clock != null,
    "blank records restored by Unity's JSON become empty again (a blank initial stamp is not 'stamped')");
var implied = Level2Checkpoint.ImpliedTriggers(back);
Check(implied.Contains("event:IncomeQuestCompleted") && implied.Contains("event:FinalAssessmentStamped") && implied.Contains("event:PreliminaryHoldStamped") &&
      implied.All(t => ObjectiveTriggers.Validate(t) == null), "the objective list catches up from the restored state (valid triggers only)");
Check(Level2Checkpoint.ImpliedTriggers(new CaseReviewState { phase = ReviewPhase.RetrieveFolder }).SequenceEqual(new[] { "event:ReceptionistTalkedTo" }),
    "an early checkpoint implies only what really happened");
Check(Level2Checkpoint.ParseEnum<TaxpayerType>("SelfEmployed") == TaxpayerType.SelfEmployed && Level2Checkpoint.ParseEnum<TaxpayerType>("") == null &&
      Level2Checkpoint.ParseEnum<TaxpayerType>("Nope") == null && Level2Checkpoint.EnumName<TaxpayerType>(null) == "",
    "taxpayer facts are stored as names (nullable enums survive the save)");
Check(save11.ClearCheckpoint("Level_02") && !save11.ClearCheckpoint("Level_02") && save11.CheckpointFor("Level_02") == null,
    "the checkpoint clears once");
Check(save11.RecordStoryFact(Level2Checkpoint.StoryFactDeskProvenance) && !save11.RecordStoryFact(Level2Checkpoint.StoryFactDeskProvenance) &&
      save11.storyFacts.Count == 1, "one neutral story fact, saved once");
var career = new PlayerProgressData();
career.Normalize();
var first = career.RecordLevelResult("Level_02", 140, 8, 160, 9, true, new List<string> { "mark_the_money_2" });
var replay = career.RecordLevelResult("Level_02", 140, 8, 160, 9, true, new List<string> { "mark_the_money_2" });
Check(first.expGain == 140 && replay.expGain == 0 && career.IsLevelPassed("Level_02") && career.IsLevelUnlocked(3),
    "a completed Level 2 result counts once, replay farms nothing, and the Level 3 gate opens");
Check(LevelScoring.MaxExp(l2Rules, 1) == 160 && LevelScoring.MaxHearts(l2Rules, 1) == 9, "Level 2 maximum: 160 EXP, 9 hearts");
string managerSrc = Src("Scripts/RewardsSystem/PlayerProgressManager.cs");
Check(Regex.IsMatch(managerSrc, @"lastAppliedRunId = levelTotal\.RunId;[\s\S]{0,400}progress\.ClearCheckpoint\(levelTotal\.LevelId\);[\s\S]*?Save\(\);"),
    "the checkpoint clears in the same write as the completed level result");
Check(managerSrc.Contains("public bool SaveCheckpoint(LevelCheckpoint checkpoint)") && managerSrc.Contains("TrainingSession.IsPracticing") &&
      managerSrc.Contains("SaveFileStore.Sign(payload)"), "checkpoints use the existing signed save owner (no second file), never practice runs");
string cpSrc = Src("Scripts/Level2/Level2Checkpointer.cs");
Check(managerSrc.Contains("!Level2Checkpointer.IsDirectEditorAttempt"), "completing a direct Editor attempt preserves the career-board checkpoint");
Check(cpSrc.Contains("if (review.audit != null && review.audit.approved) return true;") && cpSrc.Contains("OnApplicationPause") &&
      cpSrc.Contains("GameplayEvents.OnReviewDayRolled += SaveNow") && cpSrc.Contains("GameplayEvents.OnAuditReturned += SaveNow"),
    "milestones, pause/quit and periodic changes save; nothing after approval");
Check(cpSrc.Contains("manager.Score.AddSideQuest(IncomeChallenge.FirstWrongCount(review.challenge), null, tier)") &&
      cpSrc.Contains("manager.RestoreAuditCounters(") && cpSrc.Contains("OfficeObjectives.Instance.MarkDone(trigger)") &&
      !cpSrc.Contains("Raise"), "resume rebuilds score and objectives without raising gameplay events");
Check(Src("Scripts/UI/HUD/CaseFinale.cs").Contains("review.audit != null ? review.audit.failedSubmissions : 0"), "Rosa's ending follows the actual review path");
Check(Src("Scripts/Level2/Level2Audit.cs").Contains("RecordStoryFact(Level2Checkpoint.StoryFactDeskProvenance)"), "the story fact is saved only on approval");

// ================= T12 whole-level flows (pure rules end to end) =================
CaseReviewState FullRunTo(ReviewPhase stopAfter, int challengeWrong)
{
    var s = CaseReviewState.Seed(od);
    s.TryAdvance(ReviewPhase.RetrieveFolder);
    s.TryRetrieveFolder();
    s.TryPlaceFolderOnDesk();
    s.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total");
    s.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralNote", "Blanket washing");
    s.SetFindingPart("F1", ReviewFindingState.Reason, null, "unaccounted-payment", "Unlisted payment");
    FindingComparison.Check(s, od.preliminaryRule);
    PreliminaryReview.TryStamp(s, od.preliminaryRule, true);
    if (stopAfter == ReviewPhase.MarketVisit) return s;
    s.TryCollectHandover(od.handoverDocumentIds);
    s.TryMarkToolkitDemoSeen();
    var tw0 = s.TutorialWork(od.annotation.tutorial);
    var demo = od.annotation.tutorial.demo;
    AnnotationActions.Highlight(tw0, od.annotation.tutorial, demo.highlightLineId);
    AnnotationActions.Place(tw0, od.annotation, od.annotation.tutorial, AnnotationSlot.Type, AnnotationSlot.Type, demo.typeId, out _);
    AnnotationActions.Place(tw0, od.annotation, od.annotation.tutorial, AnnotationSlot.Treatment, AnnotationSlot.Treatment, demo.treatmentId, out _);
    foreach (string ev in demo.evidenceIds)
        AnnotationActions.Place(tw0, od.annotation, od.annotation.tutorial, AnnotationSlot.Evidence, AnnotationSlot.Evidence, ev, out _);
    s.TryCompleteTutorial(od.annotation.tutorial);
    var ch = IncomeChallenge.Ensure(s);
    int wrongLeft = challengeWrong;
    foreach (var e in od.annotation.entries)
    {
        var k = od.FindKey(e.id);
        var w = IncomeChallenge.Work(ch, e);
        AnnotationActions.Highlight(w, e, string.IsNullOrEmpty(k.highlightLineId) ? e.lines[0].id : k.highlightLineId);
        AnnotationActions.Place(w, od.annotation, e, AnnotationSlot.Type, AnnotationSlot.Type, wrongLeft-- > 0 ? (k.typeId == "rental" ? "business-service" : "rental") : k.typeId, out _);
        AnnotationActions.Place(w, od.annotation, e, AnnotationSlot.Treatment, AnnotationSlot.Treatment, k.treatmentId, out _);
        foreach (string ev in k.evidenceIds) AnnotationActions.Place(w, od.annotation, e, AnnotationSlot.Evidence, AnnotationSlot.Evidence, ev, out _);
    }
    IncomeChallenge.TryCheck(s, od, out _, out _);
    if (!ch.mastered)
    {
        foreach (var e in od.annotation.entries)
            IncomeChallenge.Work(ch, e).typeId = od.FindKey(e.id).typeId;
        IncomeChallenge.TryCheck(s, od, out _, out _);
    }
    IncomeChallenge.TryAward(s);
    if (stopAfter == ReviewPhase.OfficeReconciliation) return s;
    OfficeReview.SetScheduleRow(s, od, 0, "D1Total", "D1", "D1 total", out _);
    OfficeReview.SetScheduleRow(s, od, 1, "D3Amount", "D3", "D3 amount", out _);
    OfficeReview.SetScheduleRow(s, od, 2, "D4Amount", "D4", "D4 amount", out _);
    OfficeReview.SetPart(s, od, "F1", "record", "D3", "D3Amount", "D3 amount");
    OfficeReview.SetPart(s, od, "F1", "conclusion", null, "corrected-included", "Corrected: included");
    OfficeReview.SetPart(s, od, "F2", "entry", "D1", "D1Total", "D1 total");
    OfficeReview.SetPart(s, od, "F2", "compare", "D2", "D2Services", "D2");
    OfficeReview.SetPart(s, od, "F2", "clue", "D6", "D6Amount", "D6");
    OfficeReview.SetPart(s, od, "F2", "reason", null, "paid-another-way", "Paid another way");
    OfficeReview.SetPart(s, od, "F2", "conclusion", null, "explained-no-adjustment", "Explained: no change");
    FinalAssessment.TryBegin(s, od);
    return s;
}
var flow = FullRunTo(ReviewPhase.Completed, 0);
Check(flow.preliminary != null && flow.tutorialCompleted && flow.challenge.awarded && flow.challenge.firstCorrect == flow.challenge.firstTotal &&
      flow.phase == ReviewPhase.FinalAssessment && OfficeReview.Evaluate(flow, od).Supported,
    "full correct run reaches the final assessment with a supported office review");
FinalAssessment.SetReason(flow, od, "income-supported");
FinalAssessment.TryStamp(flow, od, true, 0f, 0f);
var flowAudit = FinalAudit.Submit(flow, od, none9);
var flowCase = LevelScoring.Case(l2Rules, flow.audit.charged.Count, flow.audit.failedSubmissions);
var flowQuest = LevelScoring.SideQuest(l2Rules, IncomeChallenge.FirstWrongCount(flow.challenge));
int flowExp = flowQuest.Exp + flowCase.Exp, flowHearts = flowQuest.Hearts + flowCase.Hearts + l2Rules.conversation.hearts;
Check(flowAudit.Outcome == FinalAudit.Outcome.Approved && flow.phase == ReviewPhase.Completed && flowExp == 160 && flowHearts == 9 &&
      LevelScoring.Passed(l2Rules, flowExp, flowHearts, LevelScoring.MaxExp(l2Rules, 1), LevelScoring.MaxHearts(l2Rules, 1)),
    "full correct run: approved first time, 160 / 160 EXP and 9 / 9 hearts");
Check(LevelScoring.AccuracyBadge(l2Rules, flow.challenge.firstCorrect) == BadgeTier.Gold, "full correct run earns the Gold Mark the Money! badge");

var wrongIncome = FullRunTo(ReviewPhase.Completed, 3);
Check(wrongIncome.challenge.awarded && wrongIncome.challenge.firstCorrect == wrongIncome.challenge.firstTotal - 3 &&
      LevelScoring.AccuracyBadge(l2Rules, wrongIncome.challenge.firstCorrect) == BadgeTier.Bronze && wrongIncome.phase == ReviewPhase.FinalAssessment,
    "wrong-income run: corrections finish the quest at a frozen lower tier and the office review proceeds");

var prematureRun = FullRunTo(ReviewPhase.Completed, 0);
OfficeReview.ClearScheduleRow(prematureRun, 1);
FinalAssessment.SetReason(prematureRun, od, "income-supported");
FinalAssessment.TryStamp(prematureRun, od, true);
Check(FinalAudit.Submit(prematureRun, od, none9).Outcome == FinalAudit.Outcome.PrematureApproval && prematureRun.phase == ReviewPhase.Correction &&
      prematureRun.challenge.awarded && prematureRun.HasDocument("D6"), "premature-Ready run: returned, records and quest kept");
OfficeReview.SetScheduleRow(prematureRun, od, 1, "D3Amount", "D3", "D3", out _);
FinalAssessment.TryStamp(prematureRun, od, true);
Check(FinalAudit.Submit(prematureRun, od, none9).Outcome == FinalAudit.Outcome.Approved &&
      LevelScoring.Case(l2Rules, prematureRun.audit.charged.Count, prematureRun.audit.failedSubmissions).Exp == 97,
    "after fixing it the same run is approved with a small capped deduction (2 + 1 EXP)");

var justified = FullRunTo(ReviewPhase.Completed, 0);
OfficeReview.ClearScheduleRow(justified, 1);
FinalAssessment.SetReason(justified, od, "payment-missing");
FinalAssessment.TryStamp(justified, od, false);
Check(FinalAudit.Submit(justified, od, none9).Outcome == FinalAudit.Outcome.SupportedHold && justified.audit.failedSubmissions == 0 &&
      justified.phase != ReviewPhase.Completed, "justified-hold run: acknowledged, no cost, not completed");

var falseRun = FullRunTo(ReviewPhase.Completed, 0);
FinalAssessment.SetReason(falseRun, od, "difference-unexplained");
FinalAssessment.TryStamp(falseRun, od, false);
Check(FinalAudit.Submit(falseRun, od, none9).Outcome == FinalAudit.Outcome.FalseHold, "false-hold run: challenged");

var lateRun = FullRunTo(ReviewPhase.Completed, 0);
lateRun.clock = new ReviewClockState();
ReviewClock.Start(lateRun.clock, od.clock);
ReviewClock.Advance(lateRun.clock, od.clock, od.clock.allowanceSeconds, false, out _);
ReviewClock.TryRollover(lateRun.clock, od.clock, false, false, out bool lateFirst);
FinalAssessment.SetReason(lateRun, od, "income-supported");
FinalAssessment.TryStamp(lateRun, od, true);
Check(lateFirst && FinalAudit.Submit(lateRun, od, none9).Outcome == FinalAudit.Outcome.Approved &&
      LevelScoring.Case(l2Rules, 0, 0, od.clock.lateExp, od.clock.lateHearts).Exp == 97, "late run: next morning, all work kept, approved with the one late loss");

var resumed = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(FullRunTo(ReviewPhase.OfficeReconciliation, 2), fieldJson), fieldJson);
Level2Checkpoint.Normalize(resumed);
Check(resumed.phase == ReviewPhase.OfficeReconciliation && resumed.challenge.awarded && resumed.challenge.firstCorrect == resumed.challenge.firstTotal - 2 &&
      !IncomeChallenge.TryAward(resumed) && resumed.preliminary != null && OfficeReview.CanEdit(resumed),
    "resumed run: back at the office with the frozen grade, no second award, office review open");

// Content cross-links and leakage.
Check(od.handoverDocumentIds.All(id => od.FindDocument(id)?.kind == "record") && od.annotation.entries.All(e => e.evidenceOptions.All(o => od.FindDocument(o.id) != null)),
    "every handed-over page is a record and every evidence tag names a real record");
string playerFacing = string.Join("\n", od.officeFindings.SelectMany(f => f.parts.Select(p => p.label))
    .Concat(od.assessmentReasons.Select(r => r.label)).Concat(od.conclusions.Select(c => c.label))
    .Concat(od.annotation.entries.SelectMany(e => e.lines.Select(l => l.label))));
foreach (string word in new[] { "omission", "omitted", "20,000 total", "140,000", "stamp Ready", "should be 12,000" })
    Check(playerFacing.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0, "no case answer printed on player-facing Level 2 text: " + word);
Check(rosa.review.contentStatus == "educational-draft", "the loan rule's content authority stays reported as an educational draft");

string stallSrc = Src("Scripts/Level2/Level2RosaStall.cs");
string rosaSrc = Src("Scripts/Level2/RosaInteractable.cs");
string deskSrc = Src("Scripts/Level2/MarketRecordsDesk.cs");
string assetsYaml = File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Levels", "Level_02", "scene_assets.asset"));
Check(assetsYaml.Contains("rosaPrefab: {fileID: 919132149155446097, guid: d39f95956aeb5ea40bc6d4060080e829") && assetsYaml.Contains("guid: 4c701139b81d9c140b18bb741804c1ec"),
    "Rosa wears Marie's former model (npc_cora_static, swapped 2026-10-20); the store prefab stays for scenes without an anchor");
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
Check(objectives.GetProperty("objectives").GetArrayLength() == 3 &&
      objectives.GetProperty("objectives").EnumerateArray().All(o => o.GetProperty("steps").GetArrayLength() <= 5),
    "objectives are split into three readable chapters with at most five steps each");
Check(!steps.Single(s => Str(s, "trigger") == "event:OfficeReviewCompleted").TryGetProperty("undoneBy", out _),
    "audit return does not clear the one-shot structural review milestone");
Check(Str(steps.Single(s => Str(s, "trigger") == "event:FinalAssessmentStamped"), "undoneBy") == "event:AuditReturned",
    "returned work requires a new final-stamp objective");
var introSave = new CaseReviewState { sideQuestIntroSeen = true };
Check(JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(introSave, fieldJson), fieldJson).sideQuestIntroSeen,
    "the dismissed desk introduction survives checkpoint serialization");

foreach (string trigger in stepTriggers)
    Check(ObjectiveTriggers.Validate(trigger) == null, $"objective trigger valid: {trigger} ({ObjectiveTriggers.Validate(trigger)})");
Check(stepTriggers.Take(3).SequenceEqual(new[] { "event:ReceptionistTalkedTo", "event:CaseFolderRetrieved", "state:ReviewDocumentsState" }),
    "objectives lead to the receptionist, the cabinet, then opening the folder at the desk (tap or drag)");
foreach (string forbidden in new[] { "Profile", "SideQuest", "TutorialGuide", "area:" })
    Check(!stepTriggers.Any(t => t.Contains(forbidden)), "no Level 1 plaza/corkboard/office-gate step: " + forbidden);
Check(Array.IndexOf(ObjectiveTriggers.Events, ObjectiveTriggers.CaseFolderRetrieved) >= 0, "CaseFolderRetrieved is a known objective event");
Check(GuideTriggers.TryParse("event:CaseFolderRetrieved", out _, out _), "the guide understands event:CaseFolderRetrieved");

var guide = Json("Tutorials/Level_02/guide.json");
foreach (string id in new[] { "toolkit-practice", "mark-the-money", "mark-the-money-fix" })
{
    var prompt = guide.GetProperty("lessons").EnumerateArray().Single(l => Str(l, "id") == id).GetProperty("pages").EnumerateArray().Last();
    Check(Str(prompt, "style") == "hint" && prompt.GetProperty("hintSeconds").GetSingle() <= 8f && Str(prompt, "waitFor").StartsWith("event:"),
        "side-quest prompt fades while continuing to wait for the player's action: " + id);
}
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
// Arrows only lead to places new in Level 2: the file storage room (first visit), Rosa's stall and her records desk (user direction 2026-10-20).
Check(waypointSteps.SetEquals(new[] { "event:CaseFolderRetrieved", "event:RosaRecordsCollected", "event:MarketDeskOpened" }),
    "arrows lead to the file storage room, the stall and the records desk; none to the receptionist, desk or Auditor");
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
Check(!guide.GetProperty("trail").GetProperty("waitForGuide").GetBoolean(),
    "the stall arrows show as soon as that step is active (no revealArrows page needed)");

// ================= Book leaves and office start =================
var book = Json("Levels/Level_02/book.json");
var leaves = book.GetProperty("sections").EnumerateArray().ToList();
Check(leaves.Count == 6 && leaves.All(l => !string.IsNullOrWhiteSpace(Str(l, "heading")) && !string.IsNullOrWhiteSpace(Str(l, "body"))) &&
      leaves.Select((l, i) => Str(l, "heading").StartsWith("Section " + (i + 4) + " – ")).All(x => x),
    "six leaves numbered after the shared book (Section 4 to 9), in its heading style");
Check(leaves.All(l => Str(l, "heading").IndexOf("cheat", StringComparison.OrdinalIgnoreCase) < 0), "no leaf is called a cheat sheet");
var referenceLeaves = leaves.Take(3).ToList();
Check(referenceLeaves.All(l => l.GetProperty("clickableValues").GetArrayLength() > 0 &&
          l.GetProperty("clickableValues").EnumerateArray().All(v => v.GetProperty("readOnly").GetBoolean()) &&
          Str(l, "body").Split("\n\n").Length == l.GetProperty("clickableValues").GetArrayLength()),
    "reference pages print read-only terms like the shared pages' values, one explanation under each");
string termText = string.Join("\n", referenceLeaves.SelectMany(l => l.GetProperty("clickableValues").EnumerateArray().Select(v => Str(v, "displayText"))));
string cheatText = termText + "\n" + string.Join("\n", referenceLeaves.Select(l => Str(l, "body")));
foreach (var option in rosa.review.annotation.typeOptions.Concat(rosa.review.annotation.treatmentOptions))
    Check(termText.IndexOf(option.label, StringComparison.OrdinalIgnoreCase) >= 0, "the reference pages explain the toolkit card: " + option.label);
foreach (string idea in new[] { "Missing Entry", "Listed Line", "Bank Deposit", "Evidence", "paid back", "once", "annotated papers" })
    Check(cheatText.IndexOf(idea, StringComparison.OrdinalIgnoreCase) >= 0, "the reference pages teach: " + idea);
Check(Src("Scripts/Data/DragDrop/TaxCodeValueChip.cs").Contains("if (value.readOnly)") && Src("Scripts/Data/DragDrop/TaxCodeValueChip.cs").Contains("dragSource.enabled = false"),
    "a read-only term cannot be picked up");
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

// ================= Automatic findings checks =================
var draft = Retrieved();
Check(FindingComparison.Check(null, rule) == FindingComparison.Result.Incomplete &&
      FindingComparison.Check(draft, null) == FindingComparison.Result.Incomplete,
    "a comparison needs the retrieved packet and a rule");
Check(FindingComparison.Check(draft, rule) == FindingComparison.Result.Incomplete,
    "an empty comparison is incomplete rather than a false clean-packet result");
draft.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May summary");
draft.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralReference", "Reference");
draft.SetFindingPart("F1", ReviewFindingState.Reason, null, rule.reasonId, "Unaccounted payment");
int draftRevision = draft.packetRevision;
Check(FindingComparison.Check(draft, rule) == FindingComparison.Result.NoDiscrepancy &&
      !draft.FindFinding("F1").comparisonConfirmed,
    "reference metadata alone does not support the missing-payment observation");
Check(PreliminaryReview.TryStamp(draft, rule, true) == PreliminaryReview.Outcome.WrongEvidence,
    "the stamp cannot bypass meaningful-statement validation");
Check(draft.packetRevision == draftRevision && draft.preliminary == null && draft.phase == ReviewPhase.PreliminaryStamp,
    "a failed comparison records no revision, stamp or market transition");
draft.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralNote", "Payment for blanket washing");
draft.SetFindingPart("F1", ReviewFindingState.Reason, null, "amounts-differ", "Amounts differ");
Check(FindingComparison.Check(draft, rule) == FindingComparison.Result.NoDiscrepancy,
    "a real clue paired with an unrelated principle is not a supported comparison");
draft.SetFindingPart("F1", ReviewFindingState.Reason, null, rule.reasonId, "Unaccounted payment");
draftRevision = draft.packetRevision;
Check(FindingComparison.Check(draft, rule) == FindingComparison.Result.Supported &&
      draft.FindFinding("F1").comparisonConfirmed && draft.packetRevision == draftRevision && draft.preliminary == null,
    "automatic checking supports an observation without correcting, scoring or stamping the packet");
Check(FindingComparison.Check(draft, rule) == FindingComparison.Result.Supported && draft.packetRevision == draftRevision,
    "rechecking the same comparison is idempotent");
Check(!draft.SetFindingPart("F1", ReviewFindingState.Reason, null, rule.reasonId, "Unaccounted payment") &&
      draft.FindFinding("F1").comparisonConfirmed,
    "replacing a field with the same choice keeps confirmation");
var draftRound = JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(draft, fieldJson), fieldJson);
Check(draftRound.FindFinding("F1").comparisonConfirmed, "comparison confirmation survives serialization");
draft.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralContact", "Ask Rosa");
Check(!draft.FindFinding("F1").comparisonConfirmed && FindingComparison.Check(draft, rule) == FindingComparison.Result.NoDiscrepancy,
    "changing a confirmed clue invalidates the old conclusion");
draft.SetFindingPart("F1", ReviewFindingState.Evidence, "D3", "D3Invoice", "Invoice");
Check(FindingComparison.Check(draft, rule) == FindingComparison.Result.NoDiscrepancy,
    "an uncollected record cannot support a comparison");
Check(!new ReviewFindingState().comparisonConfirmed, "older unstamped saves default to needing a comparison");
var reviewNoteValues = leaves.Single(l => Str(l, "heading").EndsWith("Finding Reasons")).GetProperty("clickableValues").EnumerateArray().ToList();
var principles = reviewNoteValues.Where(v => Str(v, "semanticKey") == "FindingReason").ToList();
var conclusionLeaf = leaves.Single(l => Str(l, "heading").EndsWith("Review Conclusions"));
var bookConclusions = conclusionLeaf.GetProperty("clickableValues").EnumerateArray().Where(v => Str(v, "semanticKey") == "FindingConclusion").ToList();
Check(principles.Count == rosa.review.reasons.Count, "every review principle has exactly one book source");
Check(bookConclusions.Count == rosa.review.conclusions.Count && reviewNoteValues.Count == principles.Count &&
      bookConclusions.All(c => rosa.review.FindConclusion(Str(c, "enumValue"))?.label == Str(c, "displayText") && c.GetProperty("type").GetInt32() == 2),
    "every office conclusion has exactly one book source with its stable id");
foreach (var choice in principles)
{
    string id = Str(choice, "enumValue");
    Check(rosa.review.FindReason(id)?.label == Str(choice, "displayText") && Str(choice, "semanticKey") == "FindingReason" &&
          choice.GetProperty("type").GetInt32() == 2, "book principle transfers its stable id: " + id);
}
Check(leaves.Single(l => Str(l, "heading").EndsWith("Finding Reasons")).GetProperty("body").GetString().Split("\n\n").Length == principles.Count,
    "each book source has a matching explanatory paragraph for the existing inline book layout");
Check(conclusionLeaf.GetProperty("body").GetString().Split("\n\n").Length == bookConclusions.Count,
    "review conclusions have their own page with one explanation per draggable label");
Check(!string.IsNullOrWhiteSpace(rule.supportedObservation) && rule.evidenceKeys.Contains("ReferralNote"),
    "comparison data supplies the meaningful clue and the post-discovery conclusion");

// A concrete total/payment link rejects another entry on the same summary.
var totalDraft = Retrieved();
totalDraft.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummaryServices", "Clothes washing");
totalDraft.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralNote", "Payment for blanket washing");
totalDraft.SetFindingPart("F1", ReviewFindingState.Reason, null, rule.reasonId, "Unlisted payment");
Check(FindingComparison.Check(totalDraft, rule) == FindingComparison.Result.NoDiscrepancy &&
      PreliminaryReview.TryStamp(totalDraft, rule, true) == PreliminaryReview.Outcome.WrongConcern,
    "a random summary entry cannot substitute for the monthly total");
totalDraft.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total");
Check(FindingComparison.Check(totalDraft, rule) == FindingComparison.Result.Supported,
    "May total plus the paid job and the unlisted-payment rule is supported");
Check(rule.concernKeys.SequenceEqual(new[] { "SummarySubtotal" }), "the specific total entry is configured in data");
Check(rosa.caseSummary.Contains("Laundry") && fieldsForScenario(), "the opening scenario names the simple laundry business");
bool fieldsForScenario() => rosa.documents.Any(doc => doc.fields.Any(f => f.semanticKey == "ReferralNote" && f.displayValue == "Blanket washing"));
Check(!pageText.Contains("referral", StringComparison.OrdinalIgnoreCase) && !pageText.Contains("commission", StringComparison.OrdinalIgnoreCase),
    "printed documents describe laundry work rather than referrals or commissions");
Check(rosa.documents.Single(doc => doc.documentName == "Collection Sheet (D1)").fields.Any(f => f.displayValue.Contains("Clothes washing only")),
    "the everyday clothes collection sheet explicitly excludes the separate blanket job");
Check(FindingsPaperPerspective.WidthAt(0f) < FindingsPaperPerspective.WidthAt(1f) &&
      FindingsPaperPerspective.WidthAt(-1f) == FindingsPaperPerspective.WidthAt(0f) &&
      FindingsPaperPerspective.WidthAt(2f) == FindingsPaperPerspective.WidthAt(1f),
    "paper taper recedes at the top and clamps beyond its edges");
for (int row = 0; row < 3; row++)
{
    float taper = FindingsPaperPerspective.WidthAt((183f + row * 94f + 43f) / 780f);
    Check(taper >= 0.92f && taper <= 1.03f, "drop field taper stays within the paper silhouette: " + row);
}

// Transfer presentation must survive a value update and expose only the uncovered point.
string slipSource = Src("Scripts/Level2/Level2FindingsSheet.cs");
string slotSource = Src("Scripts/Level2/FindingSlot.cs");
Check(!slipSource.Contains("COMPARE RECORDS") && !slipSource.Contains("DISCREPANCY FOUND"),
    "the compact slip has no manual comparison button or discrepancy heading");
Check(slotSource.Contains("feedback?.ShowFeedback(true)") && slotSource.Contains("feedback?.ShowFeedback(false)"),
    "finding destinations retain the shared accepted/rejected transfer presentation");
Check(Src("Scripts/UI/HUD/HudWindowFocus.cs").Contains("w.UsesPointRaycastFiltering") &&
      slipSource.Contains("ICanvasRaycastFilter") && slipSource.Contains("footprint.Contains(screen)"),
    "slip coverage is checked at the hit point instead of blocking the whole exposed paper");
Check(leaves.All(l => !Str(l, "heading").Contains("Level")) &&
      leaves.All(l => Str(l, "body").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length <= 90),
    "the book leaves use ordinary headings and at most 90 words each (one readable page)");

// Depth changes during a press must keep projected size constant.
foreach (float d in new[] { 0.2f, 0.5f, 1f, 2f, 4f })
    Check(Math.Abs(Level2StampDrag.ScaleAtDepth(0.17f, 1f, d) / d - 0.17f) < 0.00001f,
        "stamp perspective scale preserves the same screen size at depth " + d);
Check(Level2StampDrag.ScaleAtDepth(0.17f, 0f, 1f) == 0.17f &&
      Level2StampDrag.ScaleAtDepth(0.17f, 1f, -1f) == 0.17f,
    "invalid depth cannot produce a negative or infinite stamp size");
Check(Src("Scripts/Level2/Level2StampHitTarget.cs").Contains("Owner.ScreenHit(view, screen, out _, IncludeStand)") &&
      Src("Scripts/Level2/Level2StampHitTarget.cs").Contains("IBeginDragHandler"),
    "the stamp pointer surface uses the same visual picking and blocks underlying document drags");

Check(deskStampsSrc.Contains("toolLayer.CompositeOverDocuments = true") &&
      Src("Scripts/UI/HUD/Doc3DLayer.cs").Contains("other.IsComposited && other.sortingOrder < sortingOrder"),
    "lifted stamps also cover composited documents, with existing windows unchanged by default");
Check(deskStampsSrc.Contains("view.nearClipPlane * 1.6f") && deskStampsSrc.Contains("baseData.cameraStack.Insert(0, stampCamera)") &&
      deskStampsSrc.Contains("stampCamera.nearClipPlane = Mathf.Max(0.005f, main.nearClipPlane * 0.1f)"),
    "stamps float near the camera and the overlay camera (first in the stack) never clips them");

// Unsupported drafts clear once, retain the finding identity, and can be retried.
var retryDraft = Retrieved();
retryDraft.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummaryServices", "Clothes washing");
retryDraft.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralReference", "Reference");
retryDraft.SetFindingPart("F1", ReviewFindingState.Reason, null, "amounts-differ", "Different totals");
Check(FindingComparison.Check(retryDraft, rule) == FindingComparison.Result.NoDiscrepancy, "bad retry fixture has no supported link");
int beforeClear = retryDraft.packetRevision;
Check(retryDraft.ClearFindingDraft("F1") && retryDraft.packetRevision == beforeClear + 1 &&
      !retryDraft.FindFinding("F1").IsComplete && retryDraft.FindFinding("F1").concernKey == null &&
      retryDraft.FindFinding("F1").evidenceKey == null && retryDraft.FindFinding("F1").reasonId == null &&
      !retryDraft.FindFinding("F1").comparisonConfirmed, "all three draft slots clear together without confirming or stamping");
Check(!retryDraft.ClearFindingDraft("F1") && retryDraft.packetRevision == beforeClear + 1,
    "clearing an empty draft again is a no-op");
retryDraft.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total");
retryDraft.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralNote", "Blanket washing");
retryDraft.SetFindingPart("F1", ReviewFindingState.Reason, null, "unaccounted-payment", "Unlisted payment");
Check(FindingComparison.Check(retryDraft, rule) == FindingComparison.Result.Supported &&
      PreliminaryReview.TryStamp(retryDraft, rule, true) == PreliminaryReview.Outcome.Recorded,
    "a supported retry after clearing can complete the existing stamp flow");
Check(!retryDraft.ClearFindingDraft("F1") && retryDraft.FindFinding("F1").IsComplete,
    "clearing never erases an already stamped finding");
Check(slipSource.Contains("observation.GetPreferredValues") && slipSource.Contains("paperHeight += extra") &&
      slipSource.Contains("observation.enableAutoSizing = false"),
    "handwriting height is measured at its actual fixed font size and extends the sheet");
Check(slipSource.Contains("StartCoroutine(SettleOnDesk())"), "dropping the loose slip on the desk settles its printed face");
Check(deskStampsSrc.Contains("toolLayer.AlwaysComposite = false") && deskStampsSrc.Contains("SetDeskAffordance(CanUseDesk)") &&
      stampDragSrc.Contains("if (documentLayer != null && lifted) documentLayer.BringToFront(false)"),
    "the overlay camera covers world occluders; the capture only steps in (on top) above paper windows");
Check(folderUiSrc.Contains("StopPageTurn(false)") && folderUiSrc.Contains("target == currentPageIndex"),
    "automatic folder turns share cancellation/restoration and skip an already selected page");
Check(Src("Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs").Contains("VisualCentre(openTaxCodeBook)") &&
      Src("Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs").Contains("heldPointerPosition = surfacePoint + dragOffset - deskSurface.DragLiftOffset"),
    "book placement aligns model centres and finger tracking compensates the lifted plane");
string folderDragSrc = Src("Scripts/CaseFolder/CaseFolder3DDrag.cs");
int folderTargetStart = folderDragSrc.IndexOf("private void UpdateDragTarget(", StringComparison.Ordinal);
int folderTargetEnd = folderDragSrc.IndexOf("\n    private ", folderTargetStart + 20, StringComparison.Ordinal);
string folderTargetBody = folderDragSrc.Substring(folderTargetStart, folderTargetEnd - folderTargetStart);
Check(folderDragSrc.Contains("TaxCodeBook3DDrag.VisualCentre(closedFolder)") &&
      folderTargetBody.Contains("dragStartPosition - dragOffset + deskSurface.DragLiftOffset") &&
      folderTargetBody.Contains("heldPointerPosition = surfacePoint + dragOffset - deskSurface.DragLiftOffset") &&
      folderTargetBody.Contains("heldPointerPosition, out Vector3 clampedPosition") &&
      folderDragSrc.Contains("Vector3 heldPosition = heldPointerPosition;") &&
      folderDragSrc.Contains("heldPosition += deskSurface.DragLiftOffset;"),
    "a held case follows the pointer at its visible centre while desk clamping only changes its resting drop target");

Check(Src("Scripts/Level2/Level2StampSet.cs").Contains("Owner.OpenFromDesk()") &&
      stampDragSrc.Contains("owner.OpenFromDesk()") && deskStampsSrc.Contains("if (!Active) Toggle()"),
    "press-down pickup and touch-release world tap open idempotently rather than toggling closed");

// The landed pad covers the imprint exactly and the imprint always fits on the page.
foreach (float stampSize in new[] { 0.05f, 0.2f, 1f })
{
    var pad = new UnityEngine.Vector3(0.3f, -0.1f, 2f);
    var turned = new UnityEngine.Quaternion(0.2588190f, 0f, 0f, 0.9659258f); // 30 degrees about X
    foreach (var rot in new[] { UnityEngine.Quaternion.identity, turned })
    {
        UnityEngine.Vector3 padRoot = Level2StampDrag.RootForPad(pad, rot, stampSize);
        UnityEngine.Vector3 face = padRoot - rot * (UnityEngine.Vector3.up * (0.5f * stampSize));
        Check((face - pad).magnitude < 0.00001f, "the rubber face centre lands exactly on the imprint point (size " + stampSize + ")");
    }
}
var inkPage = new UnityEngine.Rect(-500f, -700f, 1000f, 1400f);
var inkSize = new UnityEngine.Vector2(440f, 160f);
foreach (var centre in new[] { new UnityEngine.Vector2(480f, 690f), new UnityEngine.Vector2(-499f, -10f), new UnityEngine.Vector2(0f, 0f) })
{
    UnityEngine.Vector2 c = Level2DeskStamps.FitInk(centre, inkPage, inkSize, 18f);
    Check(c.x - 238f >= inkPage.xMin - 0.01f && c.x + 238f <= inkPage.xMax + 0.01f && c.y - 98f >= inkPage.yMin - 0.01f && c.y + 98f <= inkPage.yMax + 0.01f,
        "an imprint aimed near the edge is nudged fully onto the page");
}
Check(Level2DeskStamps.FitInk(new UnityEngine.Vector2(10f, 20f), inkPage, inkSize, 18f) == new UnityEngine.Vector2(10f, 20f),
    "an imprint already on the page lands exactly where it was aimed");
Check(Math.Abs(Level2StampDrag.PadWidth - 0.94f) < 0.0001f && deskStampsSrc.Contains("stampSize = inkWidth * 1.02f / Level2StampDrag.PadWidth"),
    "the stamp is sized so its pad covers the imprint");
Check(stampDragSrc.Contains("PinToContact(local, stampSize, wobble)") && stampDragSrc.Contains("body.localPosition = new Vector3(0f, -FaceOffset, 0f)"),
    "impact keeps the pad pinned on the imprint while the squash, wobble and paper thump animate");
Check(stampDragSrc.Contains("bool onFront = aimValid || TryAim(screen, out aimLocal)") && stampDragSrc.Contains("touchDrag ? screen + Vector2.up * (Screen.height * TouchAimLift)"),
    "the press lands on the last ghost shown, aimed above the finger on touch");
Check(Regex.IsMatch(stampDragSrc, @"Travel\(restPosition, restRotation, restSize, seconds, size \* 0\.4f\);\s*SetModelLayer\(Level2DeskStamps\.DragLayer\)"),
    "returning stamps stay above the folder until they land on the stand");
Check(Src("Scripts/UI/HUD/HudWindowFocus.cs").Contains("!HitsDocument(layer.transform)"),
    "pressing a lifted stamp focuses the stamps, not the folder underneath");
Check(stampDragSrc.Contains("owner.AcquireToolLayer()") && deskStampsSrc.Contains("Doc3DLayer.Create(null, transform, \"Desk stamps\")") &&
      !stampDragSrc.Contains("StampType.ReadyForFiling ? 30 : 29"),
    "both stamps share one capture camera and layer, with no Ready/Not Ready capture split");
Check(stampDragSrc.Contains("highlight.RefreshTargets(); highlight.SetAffordance(true)"),
    "both models refresh the existing highlight after entering their shared layer");
Check(Src("Scripts/UI/HUD/CaseFolder3DView.cs").Contains("Vector2 screen = screenPosition ?? main.pixelRect.center;") &&
      Src("Scripts/UI/HUD/CaseFolder3DView.cs").Contains("FitReadingPose(main, screenPosition);"),
    "folder taps centre the paper endpoint without replacing icon entrance animation or drag-drop positioning");

// ================= Source wiring =================
Check(Level2StampDrag.FitScreenAxis(90f, 130f, 100f, 500f) == 10f &&
      Level2StampDrag.FitScreenAxis(470f, 520f, 100f, 500f) == -20f &&
      Level2StampDrag.FitScreenAxis(200f, 300f, 100f, 500f) == 0f,
    "viewport fitting corrects either edge while leaving a fully visible stamp in place");
Check(Level2StampDrag.FitScreenAxis(-100f, 700f, 100f, 500f) == 0f &&
      Level2StampDrag.FitScreenAxis(0f, 800f, 100f, 500f) == -100f,
    "an oversized model centres rather than oscillating between camera edges");
foreach (var frame in new[] { (75f, 405f), (200f, 800f), (0f, 1080f) })
{
    float bottom = frame.Item1, top = frame.Item2;
    foreach (float sampleStart in new[] { bottom - 40f, bottom + 10f, top - 10f })
    {
        float correction = Level2StampDrag.FitScreenAxis(sampleStart, sampleStart + 50f, bottom, top);
        Check(sampleStart + correction >= bottom && sampleStart + 50f + correction <= top,
            "the full stamp fits in an offset/letterboxed camera viewport");
    }
}
Check(stampDragSrc.Contains("view.pixelRect") && stampDragSrc.Contains("view.nearClipPlane * 1.1f") &&
      stampDragSrc.Contains("documentLayer.BringToFront(false)"),
    "held/press model bounds stay in front and inside the camera viewport");
Check(deskStampsSrc.Contains("toolLayer.PersistentRenderLayer = StampLayer") && deskStampsSrc.Contains("public const int StampLayer = 28;") &&
      Src("Scripts/UI/HUD/Doc3DLayer.cs").Contains("if (temporaryLayers) RestoreLayers()"),
    "tool capture uses stable isolated layers instead of transient per-render swaps");
Check(Src("Scripts/Level2/Level2StampHitTarget.cs").Contains("Owner.TapStand()") &&
      deskStampsSrc.Contains("hit.IncludeStand = true") && deskStampsSrc.Contains("mouse.leftButton.wasPressedThisFrame"),
    "desk pickup has a real pointer-down target and Input System press edge without requiring drag");
foreach (var (file, feature) in new[] { ("Scripts/UI/HUD/OfficeHudRuntime.cs", "Hud"), ("Scripts/UI/OfficeMinimap.cs", "Minimap"),
                                        ("Scripts/UI/Guide/GuideRuntime.cs", "Guide"), ("Scripts/UI/HUD/SceneFade.cs", "Fade") })
{
    string text = Src(file);
    Check(text.Contains($"OfficeLevelProfile.Allows(scene, OfficeFeature.{feature})") && !text.Contains("\"Office_Level1\""),
        $"{Path.GetFileName(file)} builds through the level profile ({feature})");
}
Check(Src("Scripts/UI/HUD/CaseFinale.cs").Contains("OfficeLevelProfile.Allows(scene, OfficeFeature.Finale)"), "CaseFinale is opted in through the level profile (finale)");
foreach (string file in new[] { "Scripts/UI/HUD/OfficeHours.cs", "Scripts/UI/HUD/ConsequenceReactions.cs",
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

// ================= Polish pass: reference tools and readable papers =================
string bookIconSrc = Src("Scripts/Level2/Level2TaxCodeBookIcon.cs");
Check(Src("Scripts/Level2/Level2OfficeStart.cs").Contains("EnsurePart<Level2TaxCodeBookIcon>(Level2TaxCodeBookIcon.ChildName)"),
    "the Tax Code Book icon is built once under Level2Gameplay");
Check(Level2TaxCodeBookIcon.IconPosition.y == -300f && Level2AnnotatedPapers.IconPosition.y == -440f && Level2TaxCodeBookIcon.IconPosition.x == Level2AnnotatedPapers.IconPosition.x,
    "right-side column: Notes (-20), Case Folder (-160), Tax Code Book (-300), Annotated Papers (-440)");
Check(bookIconSrc.Contains("TaxCodeBook3DDrag.TryStoreAt = StoreFromDesk") && bookIconSrc.Contains("FlyIntoIcon(desk)") &&
      bookIconSrc.Contains("review.taxBookStored = true") && bookIconSrc.Contains("desk.ParkClosedBook()"),
    "dragging the desk book onto its icon flies it in and stores it (saved with the review); the desk copy is parked");
Check(bookIconSrc.Contains("iconDrag.CanDrag = () => Stored &&") && bookIconSrc.Contains("if (!Stored)") && !bookIconSrc.Contains("Drag the book here"),
    "an empty icon cannot be dragged out and a tap explains; there is no on-screen drag prompt");
Check(File.ReadAllText(Res("Tutorials/Level_02/guide.json")).Contains("drag it onto the red book icon so it travels with you"),
    "Arthur mentions carrying the book as a clue, not an order");
Check(bookIconSrc.Contains("class FloatingBookGrab") && bookIconSrc.Contains("book.gameObject.AddComponent<FloatingBookGrab>()") && bookIconSrc.Contains("if (OverStoreTarget(screen)) CloseFloating();") &&
      bookIconSrc.Contains("bookLayer.AlwaysComposite = true"),
    "the open book can be dragged by its page and dropped into its icon (any hotbar slot or the Bag in Level 1), and always draws in front while out");
Check(Src("Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs").Contains("public static void SetHeldTint(") &&
      Src("Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs").Contains("bool tint = isDragging && hasMovedDuringDrag;") && bookIconSrc.Contains("TaxCodeBook3DDrag.SetHeldTint("),
    "the desk book and the floating book are red-tinted while held, and the tint is cleared afterwards");
Check(bookIconSrc.Contains("CloseFloating();") && bookIconSrc.Contains("OpenFloating(desk, screenPosition)") && bookIconSrc.Contains("RestoreFloating") &&
      bookIconSrc.Contains("OpenTaxCodeBookReturnToDesk") && bookIconSrc.Contains("handler.enabled = true"),
    "a stored book toggles open in the centre and back into the icon, restoring its desk handlers afterwards");
// The floating book's pose comes from its page (upright, facing the camera, fitted to the view), not from the desk.
string folderViewSrc = Src("Scripts/UI/HUD/CaseFolder3DView.cs");
Check(folderViewSrc.Contains("toRotation = main.transform.rotation * Quaternion.Inverse(paperInFolder);") && bookIconSrc.Contains("relRotation = inv * savedRotation;") &&
      bookIconSrc.Contains("relPosition = inv * (savedPosition - viewpoint.position);") &&
      bookIconSrc.Contains("ApplyLocal(main, pinPosition, pinRotation, pinScale);") && bookIconSrc.Contains("[DefaultExecutionOrder(450)]") &&
      bookIconSrc.Contains("private void LateUpdate() => UpdateFloating();"),
    "folder uses readable page-facing framing while the book retains its camera-relative desk framing");
Check(bookIconSrc.Contains("TaxCodeBookFloatingModel3D.TryStoreAt = StoreOpenFromDesk") && bookIconSrc.Contains("desk.ParkOpenBook()") &&
      Src("Scripts/TaxCodeBook/TaxCodeBookFloatingModel3D.cs").Contains("if (TryStoreAt != null && TryStoreAt(this, eventData.position))") &&
      Src("Scripts/TaxCodeBook/TaxCodeBookFloatingModel3D.cs").Contains("SetHeld(isDragging && hasDragTarget);") &&
      Src("Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs").Contains("public void ParkOpenBook()"),
    "the OPEN desk book can be dragged onto the icon too (red-tinted while held) and is stored the same way");
Check(HudIconArt.Render(64, "book").Count(px => px.a > 200 && px.r > px.g + 60 && px.r > px.b + 60) > 300 &&
      bookIconSrc.Contains("new Color32(176, 40, 42, 235)"),
    "the stored-book icon and its drag ghost are red, like the desk book");
// Smooth paper dragging: no per-frame layer swaps, renderer scans or repeated captures for still papers.
Check(AnnotationPaperCollectionFace.PaperLayers.Distinct().Count() == 4 &&
      AnnotationPaperCollectionFace.PaperLayers.All(l => l >= 10 && l < 28) &&
      AnnotationPaperCollectionFace.PaperLayers.All(l => !new[] { Level2DeskStamps.StampLayer, Level2DeskStamps.DragLayer, 29, 30, 31 }.Contains(l)),
    "each collected paper renders on its own spare layer, clear of the stamp, binder, settings and document layers");
string docLayerSrc = Src("Scripts/UI/HUD/Doc3DLayer.cs");
Check(docLayerSrc.Contains("if (!RenderOnlyWhenChanged || NeedsRender(main)) RenderDocument(main);") && docLayerSrc.Contains("if (FootprintRect != null)") &&
      docLayerSrc.Contains("public bool RenderOnlyWhenChanged { get; set; }") && !docLayerSrc.Contains("RenderOnlyWhenChanged { get; set; } = true"),
    "the document compositor optimisations are opt-in, so the folder, book and Level 1 documents behave as before");
string bookDragSrc = Src("Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs");
Check(bookDragSrc.Contains("TryStoreAt(this, eventData.position)") && bookDragSrc.Contains("if (StoredElsewhere) return;") &&
      bookDragSrc.Contains("|| StoredElsewhere)"),
    "a stored book is never put back on the desk by desk resets or leaving the desk");
var bookState = new CaseReviewState();
Check(!bookState.taxBookStored && JsonSerializer.Deserialize<CaseReviewState>(JsonSerializer.Serialize(new CaseReviewState { taxBookStored = true }, fieldJson), fieldJson).taxBookStored,
    "the stored book survives a save round trip and starts on the desk");
Check(bookIconSrc.Contains("SideQuestSplash.IsShowing") && bookIconSrc.Contains("IncomeToolkit.TeachingPaused") && bookIconSrc.Contains("cutoffPending"),
    "the book icon waits for splashes, the guided practice and the cutoff like the other paper icons");
Check(HudIconArt.Icons.Contains("book") && HudIconArt.Render(32, "book").Count(px => px.a > 200) > 200,
    "the book icon has its own drawn art");
string bookUiSrc = Src("Scripts/UI/TaxCodeBookUI.cs");
Check(bookUiSrc.Contains("// The book always opens on page 1.\r\n        currentSectionIndex = 0;") && !bookUiSrc.Contains("shownLevelLeaves") && bookUiSrc.Contains("FitBody()"),
    "the book always opens on page 1, and long leaves shrink to fit");
string faceSrc = Src("Scripts/Level2/AnnotationPaperCollectionFace.cs");
Check(faceSrc.Contains("new ToolkitBuilder(layer) { SelfLight = 0.5f }") && faceSrc.Contains("view.documentLayer.AlwaysComposite = true;") &&
      Src("Scripts/Level2/AnnotationPaper.cs").Contains("material.EnableKeyword(\"_EMISSION\")") && faceSrc.Contains("AddDropShadow()"),
    "the collected papers keep lit 3D shading with a little self-light and a drop shadow (not flat)");
Check(faceSrc.Contains("IncomeToolkit.BuildCardVisual(builder, attached, slot, option)") && Src("Scripts/Level2/IncomeToolkit.cs").Contains("BuildCardVisual(b, card, kind, option);") &&
      faceSrc.Contains("public void SetAnnotations(bool show, bool animate) { }") && !Src("Scripts/Level2/Level2AnnotatedPapers.cs").Contains("face.Toggle"),
    "the collected papers attach the same cards as the market desk and show only their annotated front (no flip)");
Check(Src("Scripts/Level2/Level2AnnotatedPapers.cs").Contains("slots.Add(new Vector2((i - (count - 1) * 0.5f) * step, RowLift))"),
    "the four annotated papers open in a single row (slight overlap only when the screen is narrow)");
Check(faceSrc.Contains("class AnnotatedValueRow : MonoBehaviour, IDataValueSource") && faceSrc.Contains("AddComponent<ValueDragSource>().Initialize(row, row)") &&
      faceSrc.Contains("Level2OfficeReview.AnnotatedSource(entry.id, out source)") && faceSrc.Contains("paper.TryLineBand(lineId"),
    "the highlighted line of an annotated paper is a tap/drag value source only while the office review is open");
Check(officeSrc.Contains("OfferAnnotatedPapers()") && Src("Scripts/Level2/Level2AnnotatedPapers.cs").Contains("public bool OpenForReference()"),
    "an empty schedule line brings out the annotated papers");
string findingsPageSrc = Src("Scripts/Level2/Level2FindingsPage.cs");
string sheetSrc2 = Src("Scripts/Level2/Level2FindingsSheet.cs");
Check(Level2FindingsPage.PageTitle == "Findings" && findingsPageSrc.Contains("Level2FindingsSheet.Create(page, null,") &&
      !findingsPageSrc.Contains("DrawCover(") && !findingsPageSrc.Contains("AddRecordPages(pages, data, definition);"),
    "the Findings page holds the actual Findings paper; the supporting records are not folder pages");
string annotationPaperSrc = Src("Scripts/Level2/AnnotationPaper.cs");
Check(annotationPaperSrc.Contains("paper.inkMaterial.renderQueue = UnderTextQueue;") && annotationPaperSrc.Contains("UnderTextQueue = 2980") && annotationPaperSrc.Contains("RowFont(count)"),
    "highlighter ink draws under the printed text, and short records print larger rows");
Check(rosa.review.annotation.entries.SelectMany(e => e.lines).All(l => l.label.Length <= 14),
    "annotated row labels are short enough for the larger type");
Check(faceSrc.Contains("Level2OfficeReview.PresentSchedule()") && officeSrc.Contains("public static void PresentSchedule()"),
    "picking up a highlighted amount turns an open folder to the Reviewed Schedule and brings it forward");
Check(sheetSrc2.Contains("FileIntoFolder(target)") && sheetSrc2.Contains("folder.TurnToPage(index, () => turned = true)") &&
      sheetSrc2.Contains("FileInto(folder, false, () => done = true)") && sheetSrc2.Contains("CompleteGroupFile();"),
    "dropping the loose Findings paper anywhere on the folder (no hold) hovers while the folder turns to Findings, then attaches there");
Check(sheetSrc2.Contains("AttachedWidthFit = 0.6f") && sheetSrc2.Contains("LooseScale = 0.8f") && sheetSrc2.Contains("DeskMotion.EaseOutBack(t, 0.9f)"),
    "attached Findings is smaller on its page; loose it is larger, with a peel-up lift animation");
string finalSrc = officeSrc.Substring(officeSrc.IndexOf("private static void DrawFinal"), officeSrc.IndexOf("private static void DrawHandwrittenSummary") - officeSrc.IndexOf("private static void DrawFinal"));
Check(finalSrc.LastIndexOf("DrawHandwrittenSummary(") > finalSrc.LastIndexOf("FinalHistory") && finalSrc.LastIndexOf("DrawHandwrittenSummary(") > finalSrc.LastIndexOf("FinalRevision"),
    "the handwritten summary is the last row of the Final Assessment page");
Check(faceSrc.Contains("view.documentLayer.PersistentRenderLayer = layer;") && faceSrc.Contains("view.documentLayer.FootprintRect = view.card;") &&
      faceSrc.Contains("view.documentLayer.RenderOnlyWhenChanged = true;") && faceSrc.Contains("camera.cullingMask &= ~(1 << paperLayer);"),
    "collected papers use a stable layer, their window footprint and change-only captures; the world camera skips them");
string stackSrc = Src("Scripts/Level2/Level2SupportingStack.cs");
Check(stackSrc.Contains("FeatureScale = 1.16f") && stackSrc.Contains("MaxLayoutScale = 0.8f") && stackSrc.Contains("FeaturedIndex("),
    "spread papers: a larger Findings column and records up to 0.8 scale (was 0.62)");
Check(Src("Scripts/Level2/Level2RecordPaper.cs").Contains("StyleRecordRow") && Src("Scripts/Level2/Level2RecordPaper.cs").Contains("Handed(definition.review, candidate.id)"),
    "record papers print only handed-over records, in the larger record row style");

// ================= Records desk, book colour, folder return and audit summary =================
string toolkitSrcR = Src("Scripts/Level2/IncomeToolkit.cs");
Check(Src("Scripts/UI/WorkstationUI.cs").Contains("public static System.Func<bool> ExitBlocked;") &&
      Src("Scripts/UI/WorkstationUI.cs").Contains("if (ExitBlocked != null && ExitBlocked()) return;") &&
      toolkitSrcR.Contains("WorkstationUI.ExitBlocked = () => QuestInProgress || HoldingExitAfterQuest;") &&
      toolkitSrcR.Contains("AtRecordsDesk() && (HudRewardScreen.AnyOpen || (Level2AnnotatedPapers.Instance != null && Level2AnnotatedPapers.Instance.IsCollecting))") &&
      toolkitSrcR.Contains("(active.mode == Mode.Tutorial || active.mode == Mode.Challenge)"),
    "no Exit Desk during the Mark the Money! practice and challenge; it returns once the quest is done");
Check(Src("Scripts/UI/Guide/GuideCoachView.cs").Contains("public static System.Func<bool> HintAtBottom;") &&
      toolkitSrcR.Contains("GuideCoachView.HintAtBottom = () => AtRecordsDesk() && active.mode != Mode.Done;") && toolkitSrcR.Contains("WorkstationUI.ExitBlocked = null;"),
    "at the records desk Arthur's lines sit at the bottom, and both hooks are released when the desk is disabled");
Check(toolkitSrcR.Contains("PaperCenter = new Vector3(0.04f, 0.052f, 0.025f)") && toolkitSrcR.Contains("new Vector3(0.04f, 0.023f, -0.27f)"),
    "the annotation paper and the highlighter sit a little higher, clear of Arthur's bubble");
string bookDragSrcR = Src("Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs");
Check(bookDragSrcR.Contains("ApplyRedCover(closedTaxCodeBook);") && bookDragSrcR.Contains("ApplyRedCover(openTaxCodeBook);") &&
      bookDragSrcR.Contains("public static void SetHeldTint(GameObject model, bool held) => ApplyRedCover(model);") && !bookDragSrcR.Contains("OldHeldTint"),
    "the Tax Code Book is red all over and no longer changes colour when touched or held");
string handoffSrcR = Src("Scripts/Level2/Level2FolderHandoff.cs");
Check(!handoffSrcR.Contains("StoredAfterPlacement") && handoffSrcR.Contains("RecordDeskPlacement();"),
    "later openings reuse the shared toggle without a Level 2 reopen override");

Check(toolkitSrcR.Contains("private const float BoxScale = 0.02f;") && !toolkitSrcR.Contains("transform.rotation, 0.45f,") &&
      toolkitSrcR.Contains("card.body.gameObject.SetActive(!stowed)"),
    "cards kept in the case shrink away and hide: no small leftover cards at the start or after packing up");
string auditTalkSrc = Src("Scripts/Level2/Level2Audit.cs");
Check(auditTalkSrc.Contains("instance.StartCoroutine(instance.TalkWhenFree(auditor, lines, after, result.Outcome))") &&
      auditTalkSrc.Contains("I checked your Reviewed Schedule against Rosa's records") &&
      !auditTalkSrc.Contains("Level2Say.Line(definition.clock?.onTimeLine"),
    "the Auditor always explains the audit first (waiting for any open conversation), then the Audit Summary; no separate on-time line from Arthur");
Check(toolkitSrcR.Contains("StartCoroutine(LeaveDeskAfterCollection());") && toolkitSrcR.Contains("SideQuestCompleteUI.Instance.ShowScreen(data, afterContinue)") &&
      toolkitSrcR.Contains("camera.ExitFirstPerson();"),
    "Continue on Side Quest Complete collects the annotated papers, then leaves the records desk automatically");
Check(leaves.Any(l => Str(l, "heading") == "Section 7 \u2013 Finding Reasons") && !leaves.Any(l => Str(l, "heading").Contains("Review Notes")) &&
      !officeSrc.Contains("from Review Notes in your book"),
    "the book's reasons page is titled Finding Reasons, and the hints that point to it say so");
string bookIconSrcR = Src("Scripts/Level2/Level2TaxCodeBookIcon.cs");
Check(!bookIconSrcR.Contains("OverOfficeDesk(") && bookIconSrcR.Contains("else if (OverDeskStack(screen))") &&
      bookIconSrcR.Contains("Vector2.Distance(screen, spot) < Screen.height * DeskStackRadius") && bookIconSrcR.Contains("review.taxBookStored = false;") &&
      bookIconSrcR.Contains("desk.UnparkClosedBook();") && Src("Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs").Contains("public void UnparkClosedBook()"),
    "the carried book always opens first when dragged out of its icon; the open book goes back onto its desk stack only when released over its own stack spot (2026-10-26)");
string auditWaitSrc = Src("Scripts/Level2/Level2Audit.cs");
Check(auditWaitSrc.Contains("instance.StartCoroutine(AfterDialogueCloses(conclude))") &&
      auditWaitSrc.Contains("if (!DialogueUI.IsAnyDialogueActive && !AnyDialoguePanelShowing()) break;") &&
      Src("Scripts/UI/DialogueUI.cs").Contains("public bool IsPanelShowing =>"),
    "the Audit Summary appears only after the Auditor's last line is closed and his dialogue box has left the screen");
var summaryDef = rosa.review;
var approvedResult = new FinalAudit.Result { Outcome = FinalAudit.Outcome.Approved, MayTotal = 20000f, AnnualTotal = 140000f };
var approvedState = new CaseReviewState { audit = new AuditTrackState { failedSubmissions = 1 }, clock = new ReviewClockState { started = true, day = 1 } };
RewardScreenData approvedCard = Level2Audit.Summary(approvedResult, approvedState, summaryDef);
Check(approvedCard.Title == "Audit Summary" && approvedCard.HasStatus && approvedCard.StatusGood && approvedCard.StatusText == "APPROVED" &&
      approvedCard.AutoContinueSeconds > 0f && approvedCard.Facts.Any(f => f.Value == "₱20,000") && approvedCard.Facts.Any(f => f.Value == "₱140,000") &&
      approvedCard.Facts.Any(f => f.Key == "Review cutoff" && f.Value == "On time"),
    "approval shows Level 1's Audit Summary card (APPROVED, totals, timing) before the level completes");
var returnedResult = new FinalAudit.Result { Outcome = FinalAudit.Outcome.PrematureApproval };
returnedResult.Defects.Add("omission");
RewardScreenData returnedCard = Level2Audit.Summary(returnedResult, new CaseReviewState(), summaryDef);
Check(returnedCard.Title == "Audit Summary" && !returnedCard.StatusGood && returnedCard.StatusText == "RETURNED" &&
      returnedCard.ButtonLabel == "Review the case" && returnedCard.Issues.Count >= 1 && returnedCard.AutoContinueSeconds == 0f,
    "a returned review shows the same card with what to fix, and waits for the player");
string auditSrcR = Src("Scripts/Level2/Level2Audit.cs");
Check(auditSrcR.Contains("auditor.ShowLevelSummary(Summary(result, review, definition), \"Audit Summary\", Finish)") &&
      auditSrcR.IndexOf("CalculateAndStoreCaseReward") > auditSrcR.IndexOf("void Finish()") &&
      Src("Scripts/Interactables/AuditorInteractable.cs").Contains("summaryPopupUI.ShowScreen(data, plainText, closed);"),
    "the case reward and level completion follow when the Audit Summary closes, as in Office Level 1");

// ================= Guided polish pass (2026-10-20, user direction) =================
// Start, receptionist and arrows.
var startCfg = Json("Levels/Level_02/office_start.json");
Check(startCfg.GetProperty("blend").GetSingle() <= 0.2f && Str(startCfg, "spawn").EndsWith("Reception/Doors/Office_Entrance_Door_2") &&
      Str(startCfg, "fallbackSpawn").EndsWith("Reception/Doors"),
    "the player starts just inside the street entrance, facing the receptionist (old point as fallback)");
Check(Src("Scripts/Level2/Level2OfficeStart.cs").Contains("blend = 0.16f,") && Src("Scripts/Level2/Level2OfficeStart.cs").Contains("config.fallbackSpawn"),
    "the built-in start matches office_start.json");
Check(Regex.Matches(Src("Scripts/Data/AuditorSubmissionTray.cs"), @"auditor\.BeginFinalAudit\(\);").Count == 1,
    "the tray starts the audit once (a second call replayed the Auditor's lines)");
Check(Src("Scripts/Level2/Level2Audit.cs").Contains("if (inProgress) return true;"), "Level 2 handles one hand-in at a time");
Check(File.ReadAllText(Res("Tutorials/Level_02/guide.json")).Contains("flag:questDone") && File.ReadAllText(Res("Tutorials/Level_02/guide.json")).Contains("flag:redNotes") && File.ReadAllText(Res("Tutorials/Level_02/guide.json")).Contains("flag:finalStamped"),
    "guide waits also accept the already-done state, so a fast player never leaves Arthur stuck");
Check(Src("Scripts/UI/HUD/FloatingWindow.cs").Contains("DebugTrace.Log(") && !Src("Scripts/UI/HUD/FloatingWindow.cs").Contains(" Debug.Log("),
    "per-touch window logs are compiled out (smoother drags)");

// Third feedback round (2026-10-20).
Check(!Src("Scripts/Level2/Level2FindingsPage.cs").Contains("ClearFindingDraft") &&
      Src("Scripts/Level2/Level2FindingsPage.cs").Contains("FindingComparison.Clue(FindingComparison.FirstMismatch(review, rule))"),
    "a wrong Findings link keeps all three answers and writes a clue instead of clearing them");
var clueDraft = Retrieved();
clueDraft.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummaryServices", "Clothes washing");
clueDraft.SetFindingPart("F1", ReviewFindingState.Evidence, "N1", "ReferralNote", "Blanket washing");
clueDraft.SetFindingPart("F1", ReviewFindingState.Reason, null, "unaccounted-payment", "Unlisted payment");
Check(FindingComparison.Check(clueDraft, rule) == FindingComparison.Result.NoDiscrepancy &&
      FindingComparison.FirstMismatch(clueDraft, rule) == ReviewFindingState.Concern && clueDraft.FindFinding("F1").IsComplete,
    "the clue points at the first wrong box (here: Entry to check) and nothing is erased");
clueDraft.SetFindingPart("F1", ReviewFindingState.Concern, "S1", "SummarySubtotal", "May total");
clueDraft.SetFindingPart("F1", ReviewFindingState.Reason, null, "amounts-differ", "Different totals");
Check(FindingComparison.FirstMismatch(clueDraft, rule) == ReviewFindingState.Reason &&
      FindingComparison.Clue(ReviewFindingState.Reason).Contains("Section 7") &&
      !FindingComparison.Clue(ReviewFindingState.Reason).Contains("Unlisted payment"),
    "the reason clue sends the player to Section 7 without naming the answer");
var skipState = new CaseReviewState { phase = ReviewPhase.Tutorial };
Check(skipState.TrySkipTutorial() && skipState.tutorialCompleted && skipState.toolkitDemoSeen && skipState.phase == ReviewPhase.Challenge &&
      !skipState.TrySkipTutorial(), "skipping the practice goes straight to the challenge, once");
Check(Src("Scripts/Level2/IncomeToolkit.cs").Contains("\"SKIP PRACTICE") && Src("Scripts/Level2/IncomeToolkit.cs").Contains("review.TrySkipTutorial()"),
    "the records desk offers SKIP PRACTICE during Arthur's demo and the practice");
Check(Src("Scripts/UI/HUD/HudRewardScreen.cs").Contains("if (press && lastButtonClickFrame != Time.frameCount) sceneButton.onClick.Invoke();"),
    "a reward card's Continue works even if an overlay swallows the click (never stuck)");
Check(Src("Scripts/UI/Guide/GuideDirector.cs").Contains("pendingAside = text;"), "Arthur's short lines wait while a lesson box is up (no overlap)");
Check(Src("Scripts/UI/HUD/DropAimIndicator.cs").Contains("HudTheme.Check") && Src("Scripts/Data/DragDrop/ValueTypeValidationFeedback.cs").Contains("PopCheck()"),
    "a value that fits shows a green check while dragging and when placed");
Check(Src("Scripts/UI/Books/PageEdgeTapZone.cs").Contains("TouchPadScale") && Src("Scripts/UI/PrintedPaperStyle.cs").Contains("new Vector2(98f, 98f)"),
    "page arrows are larger, with a padded touch area and a press punch");
Check(Src("Scripts/Interactables/DeskMotion.cs").Contains("public static float Step") && Src("Scripts/UI/HUD/FloatingWindow.cs").Contains("PaperSettle()") &&
      Src("Scripts/Data/DragDrop/DragVisualManager.cs").Contains("private void LateUpdate()") &&
      Src("Scripts/CaseFolder/CaseFolder3DDrag.cs").Contains("Mathf.Max(dragSmoothness, 28f)"),
    "page turns, paper windows, the value tag and the desk folder/book move smoothly");
Check(Src("Scripts/Level2/Level2FindingsSheet.cs").Contains("TuckAway()") && Src("Scripts/Level2/Level2FindingsSheet.cs").Contains("PopBack()"),
    "a loose Findings slip tucks away with the folder and pops back when it reopens");

// Annotated paper layering and rows (2026-10-20).
Check(FloatingWindow.LayerStep == 2 && Src("Scripts/UI/HUD/FloatingWindow.cs").Contains("currentSortingOrder += LayerStep;"),
    "paper windows take every other layer, so each owns the layer just above its 3D face");
Check(Src("Scripts/Level2/AnnotationPaperCollectionFace.cs").Contains("band.sortingOrder = window.SortingOrder + 1;"),
    "the highlighted-amount band draws over its own paper and under any paper in front of it");
Check(DragVisualManager.TopSortingOrder > 29000 && Src("Scripts/UI/HUD/DropAimIndicator.cs").Contains("SortingOrder = 29000") &&
      Src("Scripts/Data/DragDrop/DragVisualManager.cs").Contains("topCanvas.renderMode = RenderMode.ScreenSpaceOverlay;") &&
      Src("Scripts/Data/DragDrop/DragVisualManager.cs").Contains("dragVisual.transform.SetParent(host.transform, false);"),
    "the dragged value lives on its own top-level overlay canvas, over every paper; the aim frame sits just under it");
Check(CaseFolderUI.RowTargetHeight == 110f && Src("Scripts/UI/CaseFolderUI.cs").Contains("FitRowHeights();"),
    "folder value rows grow into the page's spare room (bigger drag sources and drop targets, never past the page)");
Check(PaperInkStyle.ChipHeight == 68f && PaperInkStyle.ChipHeightCompact == 58f &&
      Src("Scripts/UI/TaxCodeBookUI.cs").Contains("new[] { PaperInkStyle.ChipHeight, PaperInkStyle.ChipHeightCompact }"),
    "book value chips are taller, falling back to the old height on a crowded page");
string paperSrcRows = Src("Scripts/Level2/AnnotationPaper.cs");
Check(paperSrcRows.Contains("Mathf.Min(0.05f, 0.198f / Mathf.Max(1f, count - 0.5f))") && paperSrcRows.Contains("rows <= 4 ? 0.25f : 0.2f") &&
      paperSrcRows.Contains("RowFontMin = 0.15f"),
    "annotated rows are taller with bigger print, and long values shrink to fit instead of being cut");
for (int rowsN = 1; rowsN <= 8; rowsN++)
{
    float stepN = Math.Min(0.05f, 0.198f / Math.Max(1f, rowsN - 0.5f));
    Check(0.098f - (rowsN - 1) * stepN - stepN * 0.5f >= -0.1001f, $"{rowsN} rows end above the card slots");
}
string greeting = string.Join("\n", rosa.receptionistGreeting.Select(l => l.text));
Check(greeting.Contains("{deadline}") && greeting.Contains("cutoff"), "the receptionist tells today's review cutoff time");
Check(Src("Scripts/Interactables/ReceptionistInteractable.cs").Contains("line.Text.Replace(\"{deadline}\", deadline)") &&
      Src("Scripts/Interactables/ReceptionistInteractable.cs").Contains("ReviewClock.DeadlineSeconds(state, clock)"),
    "the receptionist's {deadline} is the review clock's cutoff time");
Check(string.IsNullOrEmpty(rosa.review.clock.announceLine), "Arthur no longer announces the cutoff (the receptionist does)");
string guideText = File.ReadAllText(Res("Tutorials/Level_02/guide.json"));
Check(!guideText.Contains("Follow the arrows to the receptionist") && !guideText.Contains("\"revealArrows\":"),
    "no arrows to the receptionist or the office");

// Taxpayer Profile page.
string profile = rosa.review.profileText ?? "";
Check(profile.Length > 40 && profile.Split('.').Count(s => s.Trim().Length > 0) == 1, "the Taxpayer Profile is one sentence");
foreach (string answer in new[] { "Resident Citizen", "resident", "Self-Employed", "self-employed", "Business Income", "business income", "Compensation" })
    Check(!profile.Contains(answer), "the profile hints without naming the answer: " + answer);
foreach (string clue in new[] { "Filipino", "no boss", "runs her own", "customers" })
    Check(profile.Contains(clue), "the profile carries the clue: " + clue);
string folderSrcP = Src("Scripts/UI/CaseFolderUI.cs");
Check(folderSrcP.Contains("public const string TaxpayerProfilePageTitle = \"Taxpayer Profile\";") &&
      folderSrcP.IndexOf("TaxpayerProfilePageTitle, \"\")") < folderSrcP.IndexOf("string title = captured.kind == \"summary\"") &&
      folderSrcP.Contains("BuildProfileRows(data, profile)") && folderSrcP.Contains("\"About the taxpayer\""),
    "the Taxpayer Profile page comes right after Taxpayer Information, printed like the other rows");

// Guide cues and flags.
Check(GuideTriggers.TryParse("cue:FindingsPaperLifted", out var cueT, out _) && cueT.Kind == GuideTriggers.Kind.Event && cueT.Key == "cue:FindingsPaperLifted",
    "cue:<name> is a counted guide event");
Check(GuideTriggers.TryParse("flag:bookStored", out var flagT, out _) && flagT.Kind == GuideTriggers.Kind.Condition &&
      GuideTriggers.TryParse("not:flag:bookStored", out var notFlag, out _) && notFlag.Negate && !GuideTriggers.TryParse("flag:", out _, out _),
    "flag:<name> is a condition (and works with not:)");
GuideFlags.Source = n => n == "bookStored";
Check(GuideFlags.Get("bookStored") && !GuideFlags.Get("other") && !GuideFlags.Get(null), "GuideFlags reads the level's facts");
GuideFlags.Source = null;
Check(!GuideFlags.Get("bookStored"), "no level, no flags");
var cueNames = Regex.Matches(guideText, "cue:([A-Za-z]+)").Select(m => m.Groups[1].Value).Distinct().ToList();
string allSrc = string.Join("\n", Directory.GetFiles(Path.Combine(root, "Assets", "Scripts"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
foreach (string cue in cueNames)
    Check(allSrc.Contains("RaiseGuideCue(\"" + cue + "\")"), "the guide's cue is raised by gameplay: " + cue);
Check(cueNames.Count >= 6, "the guide waits on the new cues");
string startSrcF = Src("Scripts/Level2/Level2OfficeStart.cs");
foreach (string flag in Regex.Matches(guideText, "flag:([A-Za-z]+)").Select(m => m.Groups[1].Value).Distinct())
    Check(startSrcF.Contains("case \"" + flag + "\":"), "Level2OfficeStart provides the guide flag: " + flag);
Check(Src("Scripts/UI/Guide/GuideDirector.cs").Contains("GameplayEvents.OnGuideCue += HandleGuideCue;") &&
      Src("Scripts/UI/Guide/GuideDirector.cs").Contains("GameplayEvents.OnGuideCue -= HandleGuideCue;") &&
      Src("Scripts/UI/Guide/GuideDirector.cs").Contains("return GuideFlags.Get(t.Arg);"),
    "the director listens to cues and reads flags (observation only)");

// Guided case flow.
var lessonList = guide.GetProperty("lessons").EnumerateArray().ToList();
var orderIds = lessonList.Select(l => Str(l, "id")).ToList();
string[] lessonFlow = { "welcome", "to-cabinet", "file-room", "to-desk", "desk-open", "profile", "prepared-pages", "findings-paper",
    "file-findings", "initial-stamp", "to-rosa", "records-desk", "toolkit-colours", "toolkit-practice", "mark-the-money",
    "mark-the-money-fix", "annotated-papers", "office-review", "final-assessment", "to-auditor", "audit-returned" };
Check(lessonFlow.All(id => orderIds.Contains(id)) && lessonFlow.Select(id => orderIds.IndexOf(id)).SequenceEqual(lessonFlow.Select(id => orderIds.IndexOf(id)).OrderBy(i => i)),
    "Arthur's lessons follow the case: file room, profile, findings, stamp, toolkit colours, papers, schedule, final stamp");
string Lesson(string id) => string.Join("\n", lessonList.Single(l => Str(l, "id") == id).GetProperty("pages").EnumerateArray().Select(p => Str(p, "text")));
// The HUD view temporarily reparents OpenCaseFolder out of the desk hierarchy.
// That path cannot be the only state fallback after tapping the folder icon.
var deskOpenLesson = lessonList.Single(l => Str(l, "id") == "desk-open");
var profileLesson = lessonList.Single(l => Str(l, "id") == "profile");
Check(GuideTriggers.ParseAll(Str(deskOpenLesson.GetProperty("pages")[0], "waitFor"))
          .Any(t => t.Key == GuideTriggers.OpenPrefix && t.Arg == "folder") &&
      GuideTriggers.ParseAll(Str(profileLesson, "when"))
          .Any(t => t.Key == GuideTriggers.OpenPrefix && t.Arg == "folder"),
    "tap Case Folder completes and the Taxpayer Profile lesson starts while the real folder is reparented");
string folderOpenFlow = Src("Scripts/UI/HUD/CaseFolder3DView.cs");
int openSignal = folderOpenFlow.IndexOf("GameplayEvents.RaiseCaseFolderOpened();");
Check(openSignal > folderOpenFlow.IndexOf("if (!pages.ShowFor3DView(Close))") &&
      openSignal > folderOpenFlow.IndexOf("grab.Owner = this;"),
    "a successful HUD folder open reports the same gameplay event as opening the desk folder");
Check(Lesson("file-room").Contains("file storage room") && Str(lessonList.Single(l => Str(l, "id") == "file-room"), "when").Contains("flag:inFileRoom"),
    "Arthur introduces the file storage room when the player walks in");
Check(Lesson("profile").Contains("Taxpayer Profile") && Lesson("profile").Contains("Section 1") && Lesson("profile").Contains("Section 3"),
    "Arthur sends the player from the profile to book Sections 1 to 3");
Check(Lesson("findings-paper").Contains("Section 7") && Lesson("prepared-pages").Contains("Payment Note"),
    "the Findings lessons name the pages and the reasons section");
Check(Lesson("initial-stamp").Contains("stamp set") && Lesson("initial-stamp").Contains("ghost"), "the stamp lesson teaches tapping the set and aiming the ghost");
Check(Lesson("toolkit-colours").Contains("BLUE") && Lesson("toolkit-colours").Contains("ORANGE") && Lesson("toolkit-colours").Contains("PURPLE") &&
      Lesson("toolkit-colours").Contains("Section 4") && Lesson("toolkit-colours").Contains("Section 5"),
    "Arthur explains the card colours and the book sections for them early in the side quest");
Check(Lesson("annotated-papers").Contains("two seconds") && Lesson("office-review").Contains("Section 6") &&
      Lesson("office-review").Contains("Section 8") && Lesson("final-assessment").Contains("Section 9"),
    "the second part teaches the stack gesture, the schedule rules and the final stamp with book sections");
var bookHeadings = leaves.Select(l => Str(l, "heading")).ToList();
foreach (Match m in Regex.Matches(guideText, @"Section (\d)"))
{
    int n = int.Parse(m.Groups[1].Value);
    Check(n <= 3 || bookHeadings.Any(h => h.StartsWith("Section " + n + " –")), "a book section Arthur names exists: " + n);
}
Check(bookHeadings.Any(h => h.Contains("Blue Cards")) && bookHeadings.Any(h => h.Contains("Orange Cards")) && bookHeadings.Any(h => h.EndsWith("Final Assessment")),
    "book headings name what they help with");
Check(rosa.review.audit.feedback.All(f => Regex.IsMatch(f.line, "Section|annotated paper")),
    "every Auditor correction points to a book section or the annotated papers");

// Side quest box, desk exit, annotated papers.
string toolkitSrcP = Src("Scripts/Level2/IncomeToolkit.cs");
Check(!toolkitSrcP.Contains("if (open) SetLid(LidOpenAngle, LidOpenSeconds);") &&
      toolkitSrcP.Contains("// Finished: the case stays shut and the papers are already in their icon."),
    "once the toolkit packs up its lid stays shut, also when the desk is revisited");
Check(!toolkitSrcP.Contains("Mark the Money! is done. Take what you learned back to the office."),
    "revisiting the finished records desk shows no Arthur line over Exit Desk");
string papersSrcP = Src("Scripts/Level2/Level2AnnotatedPapers.cs");
Check(Level2AnnotatedPapers.StackHoldSeconds == 2f && papersSrcP.Contains("Gather(finger)") && papersSrcP.Contains("CloseAllPapers();") &&
      papersSrcP.Contains("scatterMotion = StartCoroutine(Scatter());"),
    "hold any annotated paper 2 s to stack them; drop on the icon puts them away, anywhere else scatters them back");
Check(!Src("Scripts/Level2/AnnotationPaperCollectionFace.cs").Contains("view.BuildCloseButton();"), "annotated papers have no close (x) button");
Check(Src("Scripts/Level2/AnnotationPaperCollectionFace.cs").Contains("Level2AnnotatedPapers.Instance.SwallowTap"),
    "releasing a long press does not also pick up a highlighted amount");

// Drawer, career board and side-quest time.
string drawerSrcP = Src("Scripts/Level2/MarketClientDrawer.cs") + Src("Scripts/Level2/Level2CaseRetrieval.cs");
Check(drawerSrcP.Contains("yield return Riffle();") && drawerSrcP.Contains("FacingCamera(view") && drawerSrcP.Contains("Ease.OutBack") &&
      drawerSrcP.Contains("ALING ROSA SANTOS") && drawerSrcP.Contains("\"MarketTab\""),
    "the drawer rolls out, riffles the folders and shows Rosa's named folder before it flies to the icon");
var chaptersDoc = JsonDocument.Parse(File.ReadAllText(Res("Career/chapters.json"))).RootElement;
Check(chaptersDoc.GetProperty("sideQuests").EnumerateArray().Any(q => Str(q, "chapterId") == "Level_02" && q.GetProperty("number").GetInt32() == 2),
    "the 3D menu lists Mark the Money! as side quest 2 (its medal shows after Level Complete)");
Check(Src("Scripts/Data/CaseProgressionManager.cs").Contains("GameplayEvents.OnMarketDeskOpened += HandleMarketDeskOpened;"),
    "the Mark the Money! time starts at Rosa's records desk");

// Outside environment from Level 1, Rosa at Zorbix's spot, Rosa/Marie model swap (2026-10-20).
string stallSrcA = Src("Scripts/Level2/Level2RosaStall.cs");
Check(Level2RosaStall.AnchorPath == "Environment/Outside/Plaza/RosaStallAnchor" && stallSrcA.Contains("if (anchor != null)") &&
      stallSrcA.Contains("BuildAtAnchor(assets, anchor);") && Regex.IsMatch(stallSrcA, @"private void BuildAtAnchor[\s\S]*?BuildRosa[\s\S]*?BuildDesk[\s\S]*?BuildSign") &&
      !Regex.IsMatch(stallSrcA, @"private void BuildAtAnchor[\s\S]*?storePrefab[\s\S]*?private static Bounds RendererBounds"),
    "with the scene anchor, Rosa stands on it with her desk and sign beside her, and no shop building is built");
Check(Level2RosaStall.RosaHeight > 1.4f && Level2RosaStall.RosaHeight < 1.9f && stallSrcA.Contains("FitHeight(model, position.y);"),
    "Rosa's model is fitted to a person's height whatever its import scale");
string syncSrc = Src("Editor/Level2EnvironmentSync.cs");
Check(File.Exists(Path.Combine(root, "Assets", "Editor", "Level2EnvironmentSync.cs.meta")) &&
      syncSrc.Contains("[MenuItem(\"TaxVenture/Level 2/Copy Outside Environment From Level 1\")]") &&
      syncSrc.Contains("Unsupported.CopyGameObjectsToPasteboard()") && syncSrc.Contains("\"npc_\"") &&
      syncSrc.Contains("NPC_Zorbix") && syncSrc.Contains("SM_Chr_Business_Female_03.prefab") && syncSrc.Contains("RemapReferences"),
    "the editor tool copies Level 1's Env (without its NPCs), anchors Rosa at Zorbix's spot and gives Marie Rosa's former model");
foreach (string forbidden in new[] { "OfficeBuilding/Rooms", "Level2Gameplay", "case_rosa_001", "guide.json", "objectives.json" })
    Check(!syncSrc.Contains(forbidden), "the environment tool leaves the office, gameplay and data alone: " + forbidden);

// Rosa's stall at the town's Building_4 (2026-10-20).
string stallSrcB = Src("Scripts/Level2/Level2RosaStall.cs");
Check(Level2RosaStall.SceneStorePath == "Env/Town/Building_4" &&
      stallSrcB.IndexOf("BuildAtSceneStore(assets, sceneStore);") < stallSrcB.IndexOf("BuildAtAnchor(assets, anchor);"),
    "the town's own Building_4 hosts Rosa's stall when the scene has it (before the Zorbix anchor)");
Check(Regex.Matches(stallSrcB, @"BuildRosa\(assets, rosa, FacingTo\(rosa, desk, front\)\)").Count == 2,
    "Rosa faces her records table (she still turns to the player while talking)");
Check(stallSrcB.Contains("if (!Blocked(spot + Vector3.up * (SignHalfExtents.y + 0.15f), SignHalfExtents, rotation)) return spot;") &&
      Regex.Matches(stallSrcB, @"\}, rosa, desk(, door \+ front \* 0\.6f)?\), front\);").Count == 2,
    "the sign goes on the first spot that hits nothing solid and stays clear of Rosa and the table");
Check(!Regex.IsMatch(stallSrcB, @"Destroy\(Store"), "the stall never destroys the town's building");
Check(stallSrcB.Contains("Vector3 front = DetectFrontAround(store, b);") && stallSrcB.Contains("SurfaceOut(colliders, b, side, extent, 0.66f)") &&
      stallSrcB.Contains("Vector3 doorSide = store.lossyScale.x < 0f ? side : -side;") && stallSrcB.Contains("Vector3 rosa = Ground(door + front * 1.1f - doorSide * 1.1f, groundY);"),
    "the stall is on Building_4's real front (awning side, measured at awning height), with Rosa beside the door");

// Level 2 lighting: scoped key/fill balance and matte learning materials.
string lightingStart = Src("Scripts/Level2/Level2OfficeStart.cs");
string toolkitMaterials = Src("Scripts/Level2/AnnotationPaper.cs");
double LightSetting(string name) => double.Parse(Regex.Match(lightingStart, name + @"\.intensity = ([\d.]+)f;").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
double keyIntensity = LightSetting("daylight"), fillIntensity = LightSetting("fill");
Check(lightingStart.Contains("BalanceLighting();") && keyIntensity >= 0.6 && keyIntensity <= 1.2 && fillIntensity >= 0.15 && fillIntensity < keyIntensity * 0.4 && keyIntensity + fillIntensity <= 1.35, "Level 2 keeps a readable daylight key and a gentle fill within a bounded combined intensity");
Check(lightingStart.Contains("PlayerPlacement.Find(gameObject.scene, \"Env/Directional Light\")") && lightingStart.Contains("Environment/OfficeBuilding/Inside/Directional Light (2)") && !lightingStart.Contains("RenderSettings."), "lighting edits stay on Level 2 scene objects and do not change global lighting or Level 1");
Check(toolkitMaterials.Contains("material.SetFloat(\"_Smoothness\", 0f)") && toolkitMaterials.Contains("material.SetFloat(\"_SpecularHighlights\", 0f)") && toolkitMaterials.Contains("material.EnableKeyword(\"_SPECULARHIGHLIGHTS_OFF\")"), "toolkit paper/plastic is matte so direct light does not wash out the printed surfaces");

// Folder regression: no phantom startup access or icon-sized drag rest pose.
int folderCheckStart = checks, folderFailStart = fails;
string folderIconControl = Src("Scripts/UI/HUD/HudIconButton.cs");
string iconAwakeBody = folderIconControl.Split("private void Awake()")[1].Split("private void OnEnable()")[0];
Check(iconAwakeBody.Contains("UnlockTrigger.CaseFolderOpened") && iconAwakeBody.Contains("SetLocked();"),
    "case-folder icon starts locked instead of exposing an empty folder on a new run");
Check(folderIconControl.Contains("GameplayEvents.OnCaseFolderStored += RefreshFolderAvailability;") &&
      folderIconControl.Contains("GameplayEvents.OnCaseFolderStored -= RefreshFolderAvailability;"),
    "drawer retrieval unlocks the folder icon only when the real folder reaches storage");
string folderHudOpen = Src("Scripts/UI/HUD/CaseFolder3DView.cs").Split("public bool Open(Vector2? screenPosition)")[1].Split("public void Close()")[0];
Check(folderHudOpen.IndexOf("folder.gameObject.SetActive(true);") < folderHudOpen.IndexOf("ApplyPose(fromPosition, fromRotation, fromScale);") &&
      folderHudOpen.IndexOf("folder.gameObject.SetActive(true);") < folderHudOpen.IndexOf("folder.SetParent(null, true);"),
    "first HUD activation initializes the model at full size before the temporary icon-scale entrance");
string deskFolderOpen = Src("Scripts/CaseFolder/CaseFolder3DDrag.cs").Split("private void ShowOpenFolder(")[1].Split("public void ResetFolder()")[0];
Check(deskFolderOpen.Contains("openFolder.transform.rotation = originalOpenRotation;") &&
      deskFolderOpen.Contains("openFolder.transform.localScale = originalOpenScale;") &&
      deskFolderOpen.IndexOf("openFolder.transform.localScale = originalOpenScale;") < deskFolderOpen.IndexOf("deskAnimator.PlayOpen("),
    "desk opening restores the authored folder pose before starting its animation");
string floatingFolderSource = Src("Scripts/CaseFolder/FloatingModel3D.cs");
string floatingDisable = floatingFolderSource.Split("private void OnDisable()")[1].Split("private void Update()")[0];
string floatingDragDisable = floatingFolderSource.Split("public void SetDraggingEnabled(")[1].Split("public bool IsDraggingEnabled")[0];
Check(floatingDisable.Contains("isDragging = false;") && floatingDisable.Contains("hasDragTarget = false;") &&
      floatingDragDisable.Contains("isDragging = false;") && floatingDragDisable.Contains("hasDragTarget = false;"),
    "hidden or externally animated folders cancel the old drag writer instead of fighting the next opening");

string folderResumeBody = Src("Scripts/Level2/Level2FolderHandoff.cs").Split("public static void RestorePlacedFolder(")[1].Split("public static FolderToHUDIcon FindStore(")[0];
Check(folderResumeBody.Contains("SetAvailable(FolderToHUDIcon.GetStoredFolderCount() > 0)") &&
      !folderResumeBody.Contains("RaiseCaseFolderOpened"),
    "resuming a desk folder keeps the empty icon locked without replaying first-open events");

// These two cancellation paths only touch managed flags when no outline is
// attached. Bypass the native MonoBehaviour constructor; do not call Awake,
// transform, rendering, input, or any other Unity-native API in this runner.
var folderDragState = (FloatingModel3D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(FloatingModel3D));
var privateInstance = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var draggingField = typeof(FloatingModel3D).GetField("isDragging", privateInstance);
var targetField = typeof(FloatingModel3D).GetField("hasDragTarget", privateInstance);
draggingField.SetValue(folderDragState, true);
targetField.SetValue(folderDragState, true);
folderDragState.SetDraggingEnabled(false);
Check(!folderDragState.IsDragging() && !(bool)targetField.GetValue(folderDragState),
    "disabling folder dragging cancels both active drag and pending target in production code");
draggingField.SetValue(folderDragState, true);
targetField.SetValue(folderDragState, true);
typeof(FloatingModel3D).GetMethod("OnDisable", privateInstance).Invoke(folderDragState, null);
Check(!folderDragState.IsDragging() && !(bool)targetField.GetValue(folderDragState),
    "hiding the actual folder cancels its stale drag state in production code");

// Level 2 must delegate presentation to Level 1, retaining the stored count.
Check(!handoffSrc.Contains("private void PlaceOnDesk(") &&
      handoffSrc.Contains("bool handled = previousClick != null && previousClick();") &&
      handoffSrc.Contains("bool handled = previousDrop != null && previousDrop(screenPosition);"),
    "Level 2 reuses the Level 1 toggle/drop presentation instead of unstoring and reopening the desk folder");
string folderPointerSource = Src("Scripts/UI/HUD/HudSubmittableIcon.cs");
Check(folderPointerSource.Contains("private bool CanUseIcon() => isAvailable && CanDragOut();") &&
      folderPointerSource.Split("private void ToggleFloatingWindow()")[1].Contains("if (!CanUseIcon()) return;"),
    "empty folder icons reject click dispatch as well as drag-out");
Check(folderIconControl.Contains("GameplayEvents.OnCaseFolderReturned += RefreshFolderAvailability;") &&
      folderIconControl.Contains("GameplayEvents.OnCaseFolderReturned -= RefreshFolderAvailability;"),
    "returning the folder to the desk relocks its empty icon");
Check(folderHudOpen.Contains("if (FolderToHUDIcon.GetStoredFolderCount() <= 0) return false;"),
    "direct shared view opening also rejects an empty icon");

// Drive production input dispatch with only managed fields and delegates.
var folderPointer = (HudSubmittableIcon)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(HudSubmittableIcon));
// Campaign Foundation T05 fix: the icon count is the set of stored folders (no bare static int).
var storedSet = (HashSet<FolderToHUDIcon>)typeof(FolderToHUDIcon).GetField("storedFolders", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
var savedFolders = new List<FolderToHUDIcon>(storedSet);
void SetStoredCount(int count)
{
    storedSet.Clear();
    for (int i = 0; i < count; i++) storedSet.Add((FolderToHUDIcon)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(FolderToHUDIcon)));
}
void RestoreStoredFolders() { storedSet.Clear(); foreach (FolderToHUDIcon f in savedFolders) storedSet.Add(f); }
var availabilityField = typeof(HudSubmittableIcon).GetField("isAvailable", privateInstance);
var canUseIcon = typeof(HudSubmittableIcon).GetMethod("CanUseIcon", privateInstance);
int folderClickCalls = 0;
folderPointer.ClickOverride = () => { folderClickCalls++; return true; };
try
{
    availabilityField.SetValue(folderPointer, true);
    SetStoredCount(0);
    folderPointer.OnPointerDown(null); // must reject before dereferencing event data
    typeof(HudSubmittableIcon).GetMethod("ToggleFloatingWindow", privateInstance).Invoke(folderPointer, null);
    Check(!(bool)canUseIcon.Invoke(folderPointer, null) && folderClickCalls == 0 &&
          !(bool)typeof(HudSubmittableIcon).GetField("pointerIsDown", privateInstance).GetValue(folderPointer),
        "count zero rejects production pointer-down and click callback even when serialized availability is true");
    SetStoredCount(1);
    typeof(HudSubmittableIcon).GetMethod("ToggleFloatingWindow", privateInstance).Invoke(folderPointer, null);
    typeof(HudSubmittableIcon).GetMethod("ToggleFloatingWindow", privateInstance).Invoke(folderPointer, null);
    Check((bool)canUseIcon.Invoke(folderPointer, null) && folderClickCalls == 2 && FolderToHUDIcon.GetStoredFolderCount() == 1,
        "stored folder permits repeated toggle dispatch without consuming its badge count");
    availabilityField.SetValue(folderPointer, false);
    Check(!(bool)canUseIcon.Invoke(folderPointer, null), "availability still blocks a stored folder during a level lock");
    availabilityField.SetValue(folderPointer, true);
    SetStoredCount(0);
    typeof(HudSubmittableIcon).GetField("kind", privateInstance).SetValue(folderPointer, HudSubmittableIcon.SubmittableKind.TaxReturn);
    Check((bool)canUseIcon.Invoke(folderPointer, null), "the folder-count guard does not block the Tax Return icon");
}
finally { RestoreStoredFolders(); }

Check(Src("Scripts/CaseFolder/FolderToHUDIcon.cs").Contains("public bool TryTakeOutForView()") &&
      Src("Scripts/UI/HUD/CaseFolder3DView.cs").Contains("if (screenPosition.HasValue) TakeOutOfIcon();"),
    "dragging out withdraws stored ownership until the animated folder returns");
// Withdrawal uses the existing store, and repeated input cannot mint a folder.
var withdrawnStore = (FolderToHUDIcon)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(FolderToHUDIcon));
var storedField = typeof(FolderToHUDIcon).GetField("folderStored", privateInstance);
try
{
    storedField.SetValue(withdrawnStore, true);
    storedSet.Clear();
    storedSet.Add(withdrawnStore);
    Check(withdrawnStore.TryTakeOutForView() && FolderToHUDIcon.GetStoredFolderCount() == 0 && !withdrawnStore.IsFolderStored(),
        "drag-out removes the badge and stored status in production storage code");
    Check(!withdrawnStore.TryTakeOutForView() && FolderToHUDIcon.GetStoredFolderCount() == 0,
        "a withdrawn folder cannot be withdrawn a second time");
    withdrawnStore.ReturnFromView();
    withdrawnStore.ReturnFromView();
    Check(withdrawnStore.IsFolderStored() && FolderToHUDIcon.GetStoredFolderCount() == 1,
        "returning restores exactly one folder even after duplicate return callbacks");
    storedField.SetValue(withdrawnStore, false);
    Check(!withdrawnStore.TryTakeOutForView(), "an unstored physical folder cannot supply a HUD withdrawal");
}
finally { RestoreStoredFolders(); }
string folderViewSource = Src("Scripts/UI/HUD/CaseFolder3DView.cs");
Check(folderViewSource.Contains("store?.ReturnFromView();") &&
      folderViewSource.Contains("if (t >= 1f) Restore();") &&
      folderPointerSource.Contains("if (!CanUseIcon()) return;"),
    "animated return restores the badge while empty-icon dispatch stays guarded");
Check(Src("Scripts/UI/CaseFolderUI.cs").Contains("public bool ShowFor3DView(System.Action close)") &&
      Src("Scripts/UI/HUD/CaseFolder3DView.cs").Contains("pages.ShowFor3DView(Close)"),
    "the 3D folder uses a world-paper endpoint instead of competing flat-window animation");
Check(Src("Scripts/UI/HUD/CaseFolder3DView.cs").Contains("private void OnDisable()") &&
      Src("Scripts/UI/CaseFolderUI.cs").Contains("public void HideFor3DView()"),
    "disabling the 3D folder owner releases its paper hold and restores its folder");
Check(Src("Scripts/UI/HUD/CaseFolder3DView.cs").Contains("DocumentPhysicsRaycaster") &&
      Src("Scripts/UI/HUD/CaseFolder3DView.cs").Contains("FitReadingPose("),
    "held 3D folder stays reachable and fits a readable camera-facing paper pose");
foreach (float aspect in new[] { 16f / 9f, 4f / 3f, 9f / 16f })
foreach (float fov in new[] { 40f, 60f, 85f })
{
    float fit = CaseFolder3DView.FitReadingScale(0.4f, 0.3f, 0.75f, fov, aspect);
    double visibleHeight = 2d * 0.75d * Math.Tan(fov * Math.PI / 360d);
    Check(fit > 0 && 0.4f * fit <= visibleHeight * aspect * 0.681 && 0.3f * fit <= visibleHeight * 0.681,
        "production reading fit keeps the full paper within 68 percent of both viewport axes");
}
Check(CaseFolder3DView.FitReadingScale(0, 0.3f, 0.75f, 60f, 1.7f) == 1f,
    "invalid paper dimensions do not generate a broken animation scale");
string worldPaperSource = Src("Scripts/UI/CaseFolderUI.cs");
string showWorldBody = worldPaperSource.Split("public bool ShowFor3DView(System.Action close)")[1].Split("public void HideFor3DView()")[0];
Check(!showWorldBody.Contains("window.OpenWindow") && showWorldBody.Contains("window.enabled = false;") &&
      showWorldBody.Contains("HudWindowLock.Acquire(this)"),
    "world-paper rendering pauses the flat controller and acquires one owned player hold");
string worldPaperDisable = worldPaperSource.Split("private void OnDisable()")[1].Split("public void Show()")[0];
Check(worldPaperDisable.Contains("ReleaseWindowHold();") && worldPaperDisable.Contains("close?.Invoke();"),
    "external paper deactivation releases the hold and asks its owner to restore");
Check(Src("Scripts/UI/HUD/DocumentPhysicsRaycaster.cs").Contains("IsChildOf(Document)") &&
      folderViewSrc.Contains("documentRaycaster.enabled = HudWindowLock.IsHeld;") &&
      folderViewSrc.Contains("documentRaycaster.enabled = false;"),
    "the temporary physics raycaster filters unrelated world targets and is disabled before restoration");
Check(!Src("Scripts/UI/HUD/OfficeHudRuntime.cs").Contains("folderIcon.DropOpenOverride = screen => view.Open(screen);") &&
      Src("Scripts/UI/HUD/OfficeHudRuntime.cs").Contains("A rejected 3D open must not start the legacy flat window"),
    "rejected 3D drop opening is consumed instead of falling through into a second legacy viewer");
Check(worldPaperSource.Contains("ReleaseDeskHold();") && !worldPaperSource.Contains("CameraController.Instance?.LockPlayerControls();"),
    "physical desk-paper storage releases an owned hold rather than orphaning a global control lock");
Check(folderViewSrc.Split("internal Vector3 GrabOffset(PointerEventData data)")[1].Contains("ApplyPose(toPosition, toRotation, toScale);"),
    "grabbing during entrance settles the full-size model before calculating the drag offset");
Console.WriteLine($"Folder regression: {checks - folderCheckStart} checks, {fails - folderFailStart} failures (source contracts and managed drag-state paths; Unity visual checks not run).");
Console.WriteLine(fails == 0 ? $"Level2Regression: {checks} checks PASS" : $"Level2Regression: {fails} of {checks} checks FAILED");
return fails == 0 ? 0 : 1;
