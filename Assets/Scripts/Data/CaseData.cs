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
    public string caseNumber = "ITR-2026-0001";
    public string taxYear = "2026";
    public string dateReceived = "June 15, 2026";
    public string assignedConsultant = "Player";
    public string caseTitle = "Juan Dela Cruz Annual Income Tax Filing";
    public string caseSummary = "Client seeks assistance in filing an annual income tax return. Employment and business records require verification.";
    public CaseAssessment caseAssessment = CaseAssessment.NotReadyForFiling;

    public bool assessmentStamped = false;

    // ---------- Page 2: Taxpayer Information ----------
    public string fullName = "Juan Dela Cruz";
    public string tin = "123-456-789-000";
    public string birthdate = "January 15, 1995";
    public string address = "Quezon City, Metro Manila";
    public string contactNumber = "0917-123-4567";
    public CivilStatus civilStatus = CivilStatus.Married;
    public string spouseName = "Maria Dela Cruz";
    public string spouseTin = "987-654-321-000";
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

    // ---------- Page 5: Filing Information ----------
    public RequiredForm? requiredForm = null;
    public FilingStatus filingStatus = FilingStatus.NotReady;
    public string submissionDate = "";
    public string remarks = "";

    // ---------- Page 6: Supporting Documents ----------
    public List<SupportingDocument> supportingDocuments = new List<SupportingDocument>();

    // ---------- Page 7: Consultant Findings ----------
    public List<string> potentialIssuesIdentified = new List<string>
    {
        "Bank deposits exceed reported sales by \u20b150,000.",
        "One sales invoice was not included in declared revenue.",
        "Business income was initially omitted from the case summary."
    };

    // ---------- Compliance Audit (Milestone 13) ----------
    public int auditMistakeCount = 0;
    public bool auditPassed = false;

    // ---------- Prepare Tax Return (Milestone 12.5) ----------
    public bool hasPrintedReturn = false;
    public bool isCarryingPrintedReturn = false;

    // What the player TYPED, kept separate from the authoritative values above
    // so ComplianceChecker can compare "typed" vs "actual" without the typed
    // data ever overwriting real CaseData fields (per "the player must type
    // EVERYTHING manually, nothing auto-fills").
    public EncodedFormData encodedForm = null;
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