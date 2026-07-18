/// <summary>
/// PURPOSE:
/// Holds exactly what the player TYPED into the BIR form fields — this is
/// deliberately a SEPARATE object from CaseData's authoritative fields
/// (fullName, tin, grossIncome, etc.). The player's encoding is compared
/// against the authoritative CaseData values later by ComplianceChecker.
/// If we stored typed values directly into CaseData's real fields, we'd
/// have no way to detect "the player mistyped the TIN" — the mistake would
/// silently overwrite the truth. Keeping them separate is what makes
/// encoding mistakes possible and detectable, per the design goal.
///
/// CONNECTS WITH:
/// - BirFormEncodingUI: writes into this as the player types
/// - ComplianceChecker: reads this AND CaseData's real fields, compares them
/// </summary>
[System.Serializable]
public class EncodedFormData
{
    public RequiredForm selectedForm;

    // Text fields (typed, may contain mistakes)
    public string fullName = "";
    public string tin = "";
    public string address = "";
    public string residencyStatus = "";
    public string taxpayerType = "";
    public string incomeSource = "";

    // Numeric fields (typed as strings since TMP_InputField is text-based;
    // parsed for comparison, but kept as string here to preserve exactly
    // what the player entered, including malformed input)
    public string grossIncome = "";
    public string allowableExpenses = "";
    public string taxableIncome = "";
    public string taxDue = "";
    public string taxCredits = "";
    public string finalTaxPayable = "";

    public bool isConfirmed = false;
}