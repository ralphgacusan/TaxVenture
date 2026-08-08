using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Tax Calculator UI. Field slots are filled entirely via the click-select
/// -> click-place value transfer system (ComputerFieldSlot handles its own
/// destination logic). This script owns Calculate + exposing outputs as
/// selectable sources.
/// </summary>
public class ComputerUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject computerPanelRoot;

    [Header("Destination Field Slots")]
    [SerializeField] private ComputerFieldSlot grossIncomeSlot;
    [SerializeField] private ComputerFieldSlot allowableExpensesSlot;
    [SerializeField] private ComputerFieldSlot taxCreditsSlot;

    [Header("Calculate")]
    [SerializeField] private GameObject calculateButton;

    [Header("Output Sources")]
    [SerializeField] private ComputedResultSource taxableIncomeSource;
    [SerializeField] private ComputedResultSource taxDueSource;
    [SerializeField] private ComputedResultSource finalPayableSource;

    [Header("Return to Home")]
    [SerializeField] private ComputerHomeUI computerHomeUI;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        Debug.Log($"computerPanelRoot: {computerPanelRoot}");
        Debug.Log($"grossIncomeSlot: {grossIncomeSlot}");
        Debug.Log($"allowableExpensesSlot: {allowableExpensesSlot}");
        Debug.Log($"taxCreditsSlot: {taxCreditsSlot}");
        Debug.Log($"calculateButton: {calculateButton}");

        computerPanelRoot.SetActive(true);
        grossIncomeSlot.Clear();
        allowableExpensesSlot.Clear();
        taxCreditsSlot.Clear();
        calculateButton.SetActive(false);

        InvokeRepeating(nameof(CheckIfReadyToCalculate), 0f, 0.25f);
    }

    public void Hide()
    {
        computerPanelRoot.SetActive(false);
        CancelInvoke(nameof(CheckIfReadyToCalculate));
    }

    private void CheckIfReadyToCalculate()
    {
        bool allFilled = grossIncomeSlot.IsFilled && allowableExpensesSlot.IsFilled && taxCreditsSlot.IsFilled;
        calculateButton.SetActive(allFilled);
    }

    public void OnCalculatePressed()
    {
        CaseData data = CaseManager.Instance.CurrentCase;

        // These three are INPUT fields the player already transferred in —
        // fine to write directly, they're not part of the "must be
        // transferred to Page 4" set.
        data.grossIncome = grossIncomeSlot.CurrentValue;
        data.allowableExpenses = allowableExpensesSlot.CurrentValue;
        data.taxWithheldOrCredits = taxCreditsSlot.CurrentValue;

        // Compute results LOCALLY — do not write into CaseData yet.
        float taxableIncome = TaxComputationCalculator.ComputeTaxableIncome(data.grossIncome, data.allowableExpenses);
        TaxOption optionToUse = data.taxOption ?? TaxOption.EightPercentTaxRate;
        float taxDue = TaxComputationCalculator.ComputeTaxDue(taxableIncome, optionToUse);
        float finalTaxPayable = TaxComputationCalculator.ComputeFinalTaxPayable(taxDue, data.taxWithheldOrCredits);

        data.computationStatus = ComputationStatus.Computed;


        // Expose as sources only — Page 4 will NOT show these until the
        // player actually clicks + places them.
        taxableIncomeSource.SetValue(taxableIncome, "TaxableIncome");
        taxDueSource.SetValue(taxDue, "TaxDue");
        finalPayableSource.SetValue(finalTaxPayable, "FinalTaxPayable");
        Debug.Log($"[Calculate DONE] taxableIncomeSource instance ID = {taxableIncomeSource.GetInstanceID()}");

        if (TutorialController.Instance != null)
            TutorialController.Instance.ReportInteraction("computation_done");
    }

    public void OnBackToHomePressed()
    {
        Hide();
        computerHomeUI.Show();
    }
}