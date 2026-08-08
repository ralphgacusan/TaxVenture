using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// The complete, immutable definition of one playable case, loaded from a
/// single JSON file. This is the "master answer sheet" per the migration
/// brief — it contains the CORRECT case exactly as it should exist if
/// completed perfectly, plus explicit markers for which fields are
/// missing (hidden from the player, must be filled in) and which document
/// fields are deliberately displayed incorrectly (for the player to catch).
///
/// This class is NEVER mutated at runtime. CaseFactory (Phase 2) reads
/// this once per case-start to produce a fresh, mutable CaseData instance
/// — your existing runtime class, completely unchanged.
///
/// WHY THIS SHAPE MATCHES YOUR EXISTING CaseData FIELD-FOR-FIELD:
/// Every field here corresponds directly to a CaseData field, so
/// CaseFactory's mapping is a straight one-to-one copy with enum parsing
/// — no guessing, no inference, no hidden defaults.
/// </summary>
[Serializable]
public class CaseDefinition
{
    public string caseId;

    // ---------- Page 1 ----------
    public string caseNumber;
    public string taxYear;
    public string dateReceived;
    public string assignedConsultant;
    public string caseTitle;
    public string caseSummary;

    // ---------- Page 2: Client / Taxpayer Information ----------
    public string fullName;
    public string tin;
    public string birthdate;
    public string address;
    public string contactNumber;
    public string civilStatus;   // parses to CivilStatus
    public string spouseName;
    public string spouseTin;
    public string citizenship;

    // ---------- Answer Key (ground truth, never shown directly) ----------
    public AnswerKeyDefinition answerKey;

    // ---------- Fields deliberately hidden from the player at case start ----------
    // Each entry is a semanticKey (e.g. "TaxDue", "BusinessAddress") that
    // must NOT be pre-filled anywhere the player can see it — the folder/
    // documents must show it as unknown/blank until the player transfers
    // the correct value in themselves.
    public List<string> missingFieldKeys = new List<string>();

    // ---------- Supporting Documents (may include deliberately WRONG values) ----------
    public List<DocumentDefinition> documents = new List<DocumentDefinition>();

    // ---------- Consultant Findings placeholder text ----------
    public List<string> potentialIssues = new List<string>();

    // ---------- Client Portraits (for DialogueUI) ----------
    public string clientPortraitDefault;
    public string clientPortraitHappy;
    public string clientPortraitSad;

    // ---------- Dialogue Sections ----------
    public List<DialogueLineDefinition> receptionistGreeting = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> clientFirstMeeting = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> interviewLines = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> outcomeCorrectNoIssues = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> outcomeCorrectWithIssues = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> outcomeWrongVerdict = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> outcomeWrongVerdictWithIssues = new List<DialogueLineDefinition>();
}

/// <summary>
/// The ground-truth correct values for every determinable/computable field
/// in the case. Used exclusively by grading logic (ComplianceChecker,
/// CaseVerdictEvaluator in later phases) — never copied directly into the
/// player-visible CaseData at case start.
/// </summary>
[Serializable]
public class AnswerKeyDefinition
{
    public string residencyStatus;
    public string taxpayerType;
    public string incomeSource;
    public string numberOfEmployers;
    public string businessRegistration;
    public string taxOption;
    public string requiredForm;
    public string correctAssessment;

    public float grossIncome;
    public float allowableExpenses;
    public float taxableIncome;
    public float taxDue;
    public float taxWithheldOrCredits;
    public float finalTaxPayable;

    public string employerTin;
    public string businessAddress;
    public int dependentCount;
}

/// <summary>
/// One supporting document's field content. Each field carries BOTH the
/// value shown to the player (displayValue, which may be deliberately
/// wrong) and the actual correct value, plus metadata explaining why it's
/// wrong when applicable — matching the "Incorrect Values" requirement.
/// </summary>
[Serializable]
public class DocumentFieldDefinition
{
    public string label;
    public string displayValue;    // shown to the player as-is
    public string correctValue;    // ground truth, used for grading
    public string type;            // "Number" | "Text" | "Enum" -> DataValueType
    public string semanticKey;
    public bool isIncorrect;
    public string incorrectReason;
    public string issueType;       // e.g. "Discrepancy"
    public string severity;        // e.g. "Major", "Minor"
}

[Serializable]
public class DocumentDefinition
{
    public string documentName;
    public List<DocumentFieldDefinition> fields = new List<DocumentFieldDefinition>();
}

[Serializable]
public class DialogueLineDefinition
{
    public string speaker;      // "Npc" | "Player"
    public string text;
    public string portraitId;
}