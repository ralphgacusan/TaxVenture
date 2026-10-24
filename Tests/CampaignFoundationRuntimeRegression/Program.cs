// Campaign Foundation runtime regression: checks that need the game's own
// types from the scratch Assembly-CSharp build (T05: Level 1 case snapshot
// against the real CaseData / CaseDefinition and the real Level 1 case JSON).
// The pure save rules live in Tests/CampaignFoundationRegression.
// Build the scratch Assembly-CSharp first (see the .csproj), then run.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

int checks = 0, failures = 0;
void Check(bool ok, string message)
{
    checks++;
    if (ok) Console.WriteLine("PASS: " + message);
    else { failures++; Console.WriteLine("FAIL: " + message); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var json = new JsonSerializerOptions { IncludeFields = true, ReadCommentHandling = JsonCommentHandling.Skip };
CaseDefinition LoadCase(string file) =>
    JsonSerializer.Deserialize<CaseDefinition>(File.ReadAllText(Path.Combine(root, "Assets", "Resources", "Levels", "Level_01", "cases", file)), json);

CaseDefinition marie = LoadCase("case_001.json");
CaseData FreshCase(CaseDefinition def)
{
    var data = new CaseData { caseNumber = def.caseNumber, fullName = def.fullName };
    foreach (DocumentDefinition doc in def.documents) data.supportingDocuments.Add(new SupportingDocument(doc.documentName));
    return data;
}

// ---------- identity ----------
string print = Level1CaseCheckpoint.Fingerprint(marie);
Check(marie.caseId == "case_marie_001" && marie.documents.Count == 3 && !string.IsNullOrEmpty(print) && print.Length == 16,
    "Marie's real case loads and has a 16-hex fingerprint");
Check(Level1CaseCheckpoint.Fingerprint(LoadCase("case_001.json")) == print, "the fingerprint is stable for the same definition");
CaseDefinition edited = LoadCase("case_001.json");
edited.documents[0].fields[0].displayValue += " (revised)";
Check(Level1CaseCheckpoint.Fingerprint(edited) != print, "a changed document value changes the fingerprint (obsolete revision detected)");
CaseDefinition answerOnly = LoadCase("case_001.json");
if (answerOnly.answerKey != null) answerOnly.answerKey = new AnswerKeyDefinition();
Check(Level1CaseCheckpoint.Fingerprint(answerOnly) == print, "the answer key is not part of the fingerprint");
Check(Level1CaseCheckpoint.Fingerprint(LoadCase("case_002.json")) != print, "another case has another fingerprint");

// ---------- capture / apply round trip ----------
CaseData played = FreshCase(marie);
played.residencyStatus = ResidencyStatus.ResidentCitizen;
played.taxpayerType = TaxpayerType.CompensationEarner;
played.incomeSource = IncomeSource.EmploymentIncome;
played.grossIncome = 412345.75f;
played.taxWithheldOrCredits = 1234.5f;
foreach (string key in new[] { "ResidencyStatus", "TaxpayerType", "IncomeSource", "GrossIncome", "TaxCredits" }) played.folderTransferredFields.Add(key);
played.supportingDocuments[1].isReviewed = true;
played.caseAssessment = CaseAssessment.ReadyForFiling;
played.assessmentStamped = true;
played.computationStatus = ComputationStatus.Computed;
played.hasPrintedReturn = true;

var record = new Level1CaseRecord();
Level1CaseCheckpoint.Capture(played, marie, record);
Check(record.caseId == marie.caseId && record.caseFingerprint == print && record.fields.Count == 5 &&
      record.fields.Any(f => f.key == "ResidencyStatus" && f.value == "ResidentCitizen") &&
      record.reviewedDocuments.SequenceEqual(new[] { marie.documents[1].documentName }),
    "capture keeps identity, the filled fields by key and the documents read");
Check(!record.fields.Any(f => f.key == "EmployerCount" || f.key == "TaxOption"), "fields the player never filled are not saved");

CultureInfo previous = Thread.CurrentThread.CurrentCulture;
Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
var germanRecord = new Level1CaseRecord();
Level1CaseCheckpoint.Capture(played, marie, germanRecord);
CaseData germanBack = FreshCase(marie);
int germanRejected = Level1CaseCheckpoint.Apply(germanRecord, germanBack);
Thread.CurrentThread.CurrentCulture = previous;
Check(germanRejected == 0 && germanBack.grossIncome == played.grossIncome && germanRecord.fields.First(f => f.key == "GrossIncome").value == "412345.75",
    "numbers are saved and read culture-independently");

CaseData resumed = FreshCase(marie);
int rejected = Level1CaseCheckpoint.Apply(record, resumed);
Check(rejected == 0 && resumed.residencyStatus == ResidencyStatus.ResidentCitizen && resumed.taxpayerType == TaxpayerType.CompensationEarner &&
      resumed.incomeSource == IncomeSource.EmploymentIncome && resumed.grossIncome == 412345.75f && resumed.taxWithheldOrCredits == 1234.5f,
    "apply puts every saved value back");
Check(resumed.folderTransferredFields.SetEquals(played.folderTransferredFields) && resumed.supportingDocuments[1].isReviewed &&
      !resumed.supportingDocuments[0].isReviewed && resumed.caseAssessment == CaseAssessment.ReadyForFiling && resumed.assessmentStamped &&
      resumed.computationStatus == ComputationStatus.Computed && resumed.hasPrintedReturn,
    "apply restores the filled-field marks, documents read and the case flags");
Check(resumed.fullName == marie.fullName && resumed.supportingDocuments.Count == 3, "authored case data is untouched by apply");

var again = new Level1CaseRecord();
Level1CaseCheckpoint.Capture(resumed, marie, again);
Check(JsonSerializer.Serialize(again, json) == JsonSerializer.Serialize(record, json), "capture after apply gives the same record (stable round trip)");

// ---------- bad values ----------
var bad = new Level1CaseRecord { caseId = marie.caseId, caseFingerprint = print };
bad.fields.Add(new Level1CaseField { key = "ResidencyStatus", value = "Martian" });
bad.fields.Add(new Level1CaseField { key = "GrossIncome", value = "NaN" });
bad.fields.Add(new Level1CaseField { key = "Shoe size", value = "42" });
bad.fields.Add(new Level1CaseField { key = "TaxpayerType", value = "99" });
bad.fields.Add(new Level1CaseField { key = "IncomeSource", value = "BusinessIncome" });
CaseData badCase = FreshCase(marie);
int badRejected = Level1CaseCheckpoint.Apply(bad, badCase);
Check(badRejected == 4 && badCase.residencyStatus == null && badCase.grossIncome == 0f && badCase.taxpayerType == null &&
      badCase.incomeSource == IncomeSource.BusinessIncome && badCase.folderTransferredFields.SetEquals(new[] { "IncomeSource" }),
    "unknown keys, unknown enum names, numeric enum values and non-finite numbers are refused; valid ones still apply");

// ---------- rules on the real case ----------
var checkpoint = new Level1FieldCheckpoint { version = 1, boardPhase = Level1FieldCheckpoint.PhaseRewarded, caseRecord = record };
Check(Level1FieldCheckpointRules.ValidateCase(checkpoint, marie.caseId, print, Level1CaseCheckpoint.FieldKeys) == Level1RestoreOutcome.Restored,
    "a matching case snapshot is restored as saved");
var stale = new Level1FieldCheckpoint { version = 1, boardPhase = Level1FieldCheckpoint.PhaseRewarded, caseRecord = record };
Check(Level1FieldCheckpointRules.ValidateCase(stale, marie.caseId, Level1CaseCheckpoint.Fingerprint(edited), Level1CaseCheckpoint.FieldKeys) == Level1RestoreOutcome.Reset &&
      stale.setAsideCase == record && !stale.caseRecord.IsSet,
    "a snapshot of an obsolete revision is set aside (kept), not applied");

// ---------- Level 2 checkpoint untouched ----------
string l2 = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "Level2", "Level2Checkpointer.cs"));
Check(!l2.Contains("Level1CaseCheckpoint") && !l2.Contains("Level1FieldCheckpoint"), "Level 2's checkpoint code does not use the Level 1 records");

Console.WriteLine();
Console.WriteLine($"{checks - failures}/{checks} checks passed");
return failures == 0 ? 0 : 1;
