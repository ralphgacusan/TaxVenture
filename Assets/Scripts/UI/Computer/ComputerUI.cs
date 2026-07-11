using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// Controls the Computer / Tax Calculator panel (Main Tab 1 - Tax
/// Computation only, for Part 1). Implements the brief's click-to-assign
/// interaction: player clicks a source value button (pulled from supporting
/// documents), then clicks a destination field slot to place it there.
///
/// RESPONSIBILITIES:
/// - Build a list of "source value" buttons from CaseData's supporting
///   documents (Gross Income sources: BIR 2316 compensation, Financial
///   Statements gross sales, Sales Records amounts)
/// - Track which source value is currently "selected" (clicked but not yet placed)
/// - Validate whether a source placed into a slot is the CORRECT value for
///   that slot (green) or wrong (red, per design doc)
/// - Trigger Calculate once required slots are filled, running
///   TaxComputationCalculator and writing results into CaseData
///
/// CONNECTS WITH:
/// - ComputerInteractable: calls Show() when the Computer is clicked at the desk
/// - ComputerFieldSlot: individual assignable fields
/// - CaseManager.Instance.CurrentCase: source of document values AND
///   destination for computed results
/// - TaxComputationCalculator: pure calculation logic
/// </summary>
public class ComputerUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject computerPanelRoot;

    [Header("Source Value Buttons (built at runtime)")]
    [SerializeField] private GameObject sourceButtonListRoot;
    [SerializeField] private GameObject sourceButtonPrefab;

    [Header("Destination Field Slots")]
    [SerializeField] private ComputerFieldSlot grossIncomeSlot;
    [SerializeField] private ComputerFieldSlot allowableExpensesSlot;
    [SerializeField] private ComputerFieldSlot taxCreditsSlot;

    [Header("Calculate")]
    [SerializeField] private GameObject calculateButton;
    [SerializeField] private TextMeshProUGUI resultSummaryText;

    private float? selectedSourceValue = null;
    private List<GameObject> spawnedSourceButtons = new List<GameObject>();

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        computerPanelRoot.SetActive(true);
        BuildSourceButtons();
        grossIncomeSlot.Clear();
        allowableExpensesSlot.Clear();
        taxCreditsSlot.Clear();
        selectedSourceValue = null;
        calculateButton.SetActive(false);
        resultSummaryText.text = "";

        grossIncomeSlot.Initialize(this, "GrossIncome");
        allowableExpensesSlot.Initialize(this, "AllowableExpenses");
        taxCreditsSlot.Initialize(this, "TaxCredits");
    }

    public void Hide()
    {
        computerPanelRoot.SetActive(false);
    }

    /// <summary>
    /// Builds clickable "source" buttons representing values pulled from
    /// CaseData's supporting documents, per design doc's Sub Tab 1.1/1.2/1.5
    /// source document lists.
    /// </summary>
    private void BuildSourceButtons()
    {
        foreach (var btn in spawnedSourceButtons) Destroy(btn);
        spawnedSourceButtons.Clear();

        // Hardcoded example values matching DocumentDataProvider's data
        // (Milestone 6), since this prototype only has one case's documents.
        AddSourceButton("BIR 2316 - Compensation", 500000f);
        AddSourceButton("Financial Statements - Gross Sales", 350000f);
        AddSourceButton("Sales Records - Amount", 25000f);
        AddSourceButton("Financial Statements - Expenses", 150000f);
        AddSourceButton("BIR 2316 - Tax Withheld", 45000f);
    }

    private void AddSourceButton(string label, float value)
    {
        GameObject buttonObj = Instantiate(sourceButtonPrefab, sourceButtonListRoot.transform);
        TextMeshProUGUI text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"{label}: \u20b1{value:N0}";

        Button button = buttonObj.GetComponent<Button>();
        float capturedValue = value;
        button.onClick.AddListener(() => OnSourceValueSelected(capturedValue));

        spawnedSourceButtons.Add(buttonObj);
    }

    private void OnSourceValueSelected(float value)
    {
        selectedSourceValue = value;
    }

    /// <summary>
    /// Called by a ComputerFieldSlot when it's clicked. Places the currently
    /// selected source value into that slot, matching design doc: "The
    /// player drags a value... drops the value into the corresponding
    /// computer field... If correct, green border. If incorrect, red border."
    /// </summary>
    public void OnSlotClicked(string slotId)
    {
        if (!selectedSourceValue.HasValue) return; // nothing selected yet

        ComputerFieldSlot targetSlot = GetSlot(slotId);
        if (targetSlot == null) return;

        // Simplified validation for this prototype: any selected value can be
        // placed into any slot (the player isn't blocked), it just visually
        // confirms with green since we don't yet have "correct answer" tagging
        // per source button. A stricter validation (matching specific source
        // to specific slot) can be added here later without changing the
        // click-to-assign flow itself.
        targetSlot.AssignValue(selectedSourceValue.Value);
        selectedSourceValue = null;

        CheckIfReadyToCalculate();
    }

    private ComputerFieldSlot GetSlot(string slotId)
    {
        return slotId switch
        {
            "GrossIncome" => grossIncomeSlot,
            "AllowableExpenses" => allowableExpensesSlot,
            "TaxCredits" => taxCreditsSlot,
            _ => null
        };
    }

    private void CheckIfReadyToCalculate()
    {
        bool allFilled = grossIncomeSlot.IsFilled && allowableExpensesSlot.IsFilled && taxCreditsSlot.IsFilled;
        calculateButton.SetActive(allFilled);
    }

    /// <summary>
    /// Wired to the Calculate button. Runs the pure computation logic and
    /// writes results directly into CaseData, per design doc: "Once all
    /// fields are correct, a Calculate button appears... system computes
    /// the result."
    /// </summary>
    public void OnCalculatePressed()
    {
        CaseData data = CaseManager.Instance.CurrentCase;

        data.grossIncome = grossIncomeSlot.CurrentValue;
        data.allowableExpenses = allowableExpensesSlot.CurrentValue;
        data.taxWithheldOrCredits = taxCreditsSlot.CurrentValue;

        data.taxableIncome = TaxComputationCalculator.ComputeTaxableIncome(data.grossIncome, data.allowableExpenses);

        // Default to 8% option if the interview hasn't set one yet, so the
        // calculator doesn't hard-fail if played out of the "ideal" order.
        TaxOption optionToUse = data.taxOption ?? TaxOption.EightPercentTaxRate;
        data.taxDue = TaxComputationCalculator.ComputeTaxDue(data.taxableIncome, optionToUse);

        data.finalTaxPayable = TaxComputationCalculator.ComputeFinalTaxPayable(data.taxDue, data.taxWithheldOrCredits);

        data.computationStatus = ComputationStatus.Computed;

        resultSummaryText.text =
            $"Taxable Income: \u20b1{data.taxableIncome:N0}\n" +
            $"Tax Due: \u20b1{data.taxDue:N0}\n" +
            $"Final Tax Payable: \u20b1{data.finalTaxPayable:N0}\n" +
            $"Status: Computed";
    }
}