/// <summary>
/// PURPOSE:
/// Pure calculation logic for Section 7 (Income Tax Computation) of the
/// design doc's Tax Code Book. Deliberately has ZERO Unity/UI dependencies —
/// it just takes numbers in, returns numbers out. This keeps the actual tax
/// math testable and reusable independent of whatever UI ends up calling it.
///
/// FORMULAS (per design doc):
/// Taxable Income = Gross Income - Allowable Expenses
/// Tax Due = computed from Taxable Income using the selected Tax Option
/// Final Tax Payable = Tax Due - Tax Credits
///
/// SIMPLIFICATION NOTE:
/// The design doc's Graduated Tax Rate technically requires bracket-based
/// computation (per BIR's actual tax table). For this greybox prototype, we
/// implement a SIMPLIFIED flat-rate approximation for both tax options, since
/// exact BIR bracket accuracy isn't the point of Phase 1. This is clearly
/// isolated in one method (ComputeTaxDue) so replacing it with real bracket
/// logic later touches nothing else in the game.
///
/// CONNECTS WITH:
/// - ComputerUI: calls these methods when the player triggers "Calculate"
/// </summary>
public static class TaxComputationCalculator
{
    public static float ComputeTaxableIncome(float grossIncome, float allowableExpenses)
    {
        return UnityEngine.Mathf.Max(0f, grossIncome - allowableExpenses);
    }

    /// <summary>
    /// Simplified tax due calculation. Real BIR graduated rates use brackets;
    /// this uses flat approximate rates for prototype purposes, clearly
    /// isolated here for later replacement with accurate bracket logic.
    /// </summary>
    public static float ComputeTaxDue(float taxableIncome, TaxOption taxOption)
    {
        switch (taxOption)
        {
            case TaxOption.EightPercentTaxRate:
                // 8% flat rate on taxable income (simplified; real rule has a
                // ₱250,000 exemption threshold before this applies - can be
                // added here later without touching any other script).
                return taxableIncome * 0.08f;

            case TaxOption.GraduatedTaxRate:
                // Simplified flat approximation of graduated rates for prototype.
                return taxableIncome * 0.20f;

            default:
                return 0f;
        }
    }

    public static float ComputeFinalTaxPayable(float taxDue, float taxCredits)
    {
        return UnityEngine.Mathf.Max(0f, taxDue - taxCredits);
    }
}