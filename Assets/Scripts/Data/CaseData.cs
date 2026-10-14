using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Holds all data for a single tax case, matching the fields described across
/// Pages 1–7 of the design document's Case Folder. This is the single source
/// of truth for case information — the Folder UI only ever displays this
/// data, and later systems (Interview, Tax Book research, Computer/Calculator,
/// Stamp Assessment) write INTO this same object rather than each owning
/// their own copy of the data.
///
/// WHY A PLAIN CLASS (NOT a MonoBehaviour or ScriptableObject):
/// This represents one case's mutable runtime state — created fresh per case,
/// changed constantly as the player plays, and eventually serialized to JSON
/// by the Save System (later milestone). ScriptableObjects are better suited
/// for static authored data (like the Tax Code Book in Milestone 9); a plain
/// class is the correct fit for per-playthrough mutable state.
///
/// CONNECTS WITH:
/// - CaseFolderUI: reads these fields to populate each page
/// - CaseManager (introduced this milestone): holds the "current" CaseData instance
/// - Future: InterviewClientState, ComputeTaxesState, StampAssessmentState
///   all write into this same object
/// </summary>
[Serializable]
public class CaseData
{


    // ---------- Page 1: Case Overview ----------
    public string caseNumber = "ITR-2026-0101";
    public string taxYear = "2026";
    public string dateReceived = "June 20, 2026";
    public string assignedConsultant = "Player";
    public string caseTitle = "Marie Bautista Annual Income Tax Filing";
    public string caseSummary = "Marie Bautista, a local BPO employee, needs her taxpayer classification confirmed before her annual income tax return can be processed. She has lived and worked in the Philippines her entire life and earns income solely from her employer.";
    public CaseAssessment caseAssessment = CaseAssessment.NotReadyForFiling;

    public bool assessmentStamped = false;

    // ---------- Page 2: Taxpayer Information ----------
    public string fullName = "Marie Bautista";
    public string tin = "123-456-789-000";
    public string birthdate = "March 14, 1997";
    public string address = "Poblacion, Town Proper";
    public string contactNumber = "0917-123-4567";
    public CivilStatus civilStatus = CivilStatus.Single;
    public string spouseName = "";
    public string spouseTin = "";
    public string citizenship = "Filipino";

    // Unknown at case start — filled in during Interview (Milestone 7)
    public ResidencyStatus? residencyStatus = null;
    public TaxpayerType? taxpayerType = null;

    // ---------- Page 3: Income Information ----------
    public IncomeSource? incomeSource = null;
    public EmployerCount? numberOfEmployers = null;
    public BusinessRegistration? businessRegistration = null;
    public TaxOption? taxOption = null;

    // ---------- Page 4: Tax Computation Information ----------
    // All left at 0 / Not Computed until Milestone 10 (Computer & Tax Calculator)
    public float grossIncome = 0f;
    public float allowableExpenses = 0f;
    public float taxableIncome = 0f;
    public float taxDue = 0f;
    public float taxWithheldOrCredits = 0f;
    public float finalTaxPayable = 0f;
    public ComputationStatus computationStatus = ComputationStatus.NotComputed;

    public HashSet<string> folderTransferredFields = new HashSet<string>();

    // ---------- Page 5: Filing Information ----------
    public RequiredForm? requiredForm = null;
    public FilingStatus filingStatus = FilingStatus.NotReady;
    public string submissionDate = "Date";
    public string remarks = "Remarks";

    // ---------- Page 6: Supporting Documents ----------
    public List<SupportingDocument> supportingDocuments = new List<SupportingDocument>();

    // ---------- Page 7: Consultant Findings ----------
    public List<string> potentialIssuesIdentified = new List<string>
    {
        "Marie's income comes entirely from a single local employer, with no other sources of income to account for.",
        "Her taxpayer classification depends on confirming both her citizenship and her Philippine residency.",
        "Tax was already properly withheld by her employer, so no additional tax should be payable once the classification is confirmed."
    };

    // ---------- Compliance Audit (Milestone 13) ----------
    public int auditMistakeCount = 0;
    public bool auditPassed = false;

    // ---------- Prepare Tax Return (Milestone 12.5) ----------
    public bool hasPrintedReturn = false;
    public bool isCarryingPrintedReturn = false;


    // ---------- Case Outcome / Archive (Milestone 14) ----------
    public bool clientPresentationCompleted = false;
    public bool isArchived = false;
    public bool isCarryingCaseFolder = true; // conceptually "in hand" through to archiving; see FilingCabinetInteractable note


    // What the player TYPED, kept separate from the authoritative values above
    // so ComplianceChecker can compare "typed" vs "actual" without the typed
    // data ever overwriting real CaseData fields (per "the player must type
    // EVERYTHING manually, nothing auto-fills").
    public EncodedFormData encodedForm = null;

    // ---------- Final Audit Result (R11/R12) ----------
    public bool finalVerdictWasCorrect = false;
    public int finalMissedIssueCount = 0;


    /// <summary>
    /// Called when the Auditor rejects a submission (not 100% correct).
    /// Clears everything the player is responsible for discovering/filling in
    /// so the case is a genuine clean restart, while keeping case identity
    /// fields (name, TIN, case number, documents, etc.) untouched since those
    /// are authored data, not player answers.
    ///
    /// Level 1 scope: only the three required taxpayer facts are checked/reset.
    /// Extend this as later milestones add more player-entered fields
    /// (numberOfEmployers, businessRegistration, taxOption, computation, etc.)
    /// so a retry never carries over a previous wrong or right guess.
    /// </summary>
    public void ResetForRetry()
    {
        // Page 2/3 — the facts ComplianceChecker verifies
        residencyStatus = null;
        taxpayerType = null;
        incomeSource = null;

        // Other player-set classification fields (not yet checked this level,
        // but reset for consistency so nothing carries over silently)
        numberOfEmployers = null;
        businessRegistration = null;
        taxOption = null;

        // Page 1 — the verdict the player stamped
        caseAssessment = CaseAssessment.NotReadyForFiling;
        assessmentStamped = false;

        // Whatever the player typed into the encoded form, if any
        encodedForm = null;

        // Audit bookkeeping — cleared so a stale pass/fail doesn't leak into the retry
        auditMistakeCount = 0;
        auditPassed = false;
        finalVerdictWasCorrect = false;
        finalMissedIssueCount = 0;

        // Folder carry state — back in the player's hand at case start
        isCarryingCaseFolder = true;
    }

}

public enum CaseAssessment { UnderReview, ReadyForComputation, ReadyForFiling, NotReadyForFiling, Filed }
public enum CivilStatus { Single, Married, Widowed, Separated }
public enum ResidencyStatus { ResidentCitizen, NonResidentCitizen, ResidentAlien, NonResidentAlien }
public enum TaxpayerType { CompensationEarner, SelfEmployed, Professional, MixedIncomeEarner }
public enum IncomeSource { EmploymentIncome, BusinessIncome, ProfessionalIncome, MixedIncome }
public enum EmployerCount { OneEmployer, MultipleEmployers }
public enum BusinessRegistration { Registered, NotRegistered }
public enum TaxOption { GraduatedTaxRate, EightPercentTaxRate }
public enum ComputationStatus { NotComputed, Computed }
public enum RequiredForm { BIR1700, BIR1701, BIR1701A }
public enum FilingStatus { NotReady, ReadyForFiling, Filed }




/// <summary>
/// A single supporting document entry (Page 6). Full document viewing with
/// detailed fields arrives in Milestone 6 — for now this just holds enough
/// to list documents by name in the folder.
/// </summary>
[Serializable]
public class SupportingDocument
{
    public string documentName;
    public bool isReviewed;

    public SupportingDocument(string name)
    {
        documentName = name;
        isReviewed = false;
    }
}