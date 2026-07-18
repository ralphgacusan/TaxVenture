using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ComputerUI : MonoBehaviour, IPointerClickHandler
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

    [Header("Filing (Sub Tab 2)")]
    [SerializeField] private GameObject fileFormButton;
    [SerializeField] private TextMeshProUGUI filingResultText;

    private ComputerSourceValue selectedSource = null;
    private GameObject selectedSourceButtonObj = null;
    private List<(GameObject obj, ComputerSourceValue data)> spawnedSourceButtons = new List<(GameObject, ComputerSourceValue)>();

    [Header("Return to Home")]
    [SerializeField] private ComputerHomeUI computerHomeUI;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        computerPanelRoot.SetActive(true);
        BuildSourceButtons();

        grossIncomeSlot.Initialize(this, "GrossIncome");
        allowableExpensesSlot.Initialize(this, "AllowableExpenses");
        taxCreditsSlot.Initialize(this, "TaxCredits");

        selectedSource = null;
        selectedSourceButtonObj = null;
        calculateButton.SetActive(false);
        resultSummaryText.text = "";

        fileFormButton.SetActive(CaseManager.Instance.CurrentCase.computationStatus == ComputationStatus.Computed);
        filingResultText.text = "";
    }

    public void Hide()
    {
        computerPanelRoot.SetActive(false);
    }

    private void BuildSourceButtons()
    {
        foreach (var (obj, _) in spawnedSourceButtons) Destroy(obj);
        spawnedSourceButtons.Clear();

        AddSourceButton("BIR 2316 - Compensation", 500000f, "GrossIncome");
        AddSourceButton("Financial Statements - Gross Sales", 350000f, "GrossIncome"); // deliberately also valid-looking, but only ONE slot ("GrossIncome") — both route to the same correct slot to reflect "Gross Income = combination of sources" per design doc
        AddSourceButton("Sales Records - Amount", 25000f, "GrossIncome");
        AddSourceButton("Financial Statements - Expenses", 150000f, "AllowableExpenses");
        AddSourceButton("BIR 2316 - Tax Withheld", 45000f, "TaxCredits");
    }

    private void AddSourceButton(string label, float value, string correctSlotId)
    {
        GameObject buttonObj = Instantiate(sourceButtonPrefab, sourceButtonListRoot.transform);
        TextMeshProUGUI text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"{label}: \u20b1{value:N0}";

        var data = new ComputerSourceValue(label, value, correctSlotId);

        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(() => OnSourceValueSelected(data, buttonObj));

        spawnedSourceButtons.Add((buttonObj, data));
    }

    private void OnSourceValueSelected(ComputerSourceValue source, GameObject buttonObj)
    {
        // Deselect previous button's highlight, if any.
        ClearSelectionHighlight();

        selectedSource = source;
        selectedSourceButtonObj = buttonObj;

        // Visual "selected" feedback per design doc's "Object becomes selected."
        Image img = buttonObj.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 0.85f, 0.3f); // highlighted yellow
    }

    private void ClearSelectionHighlight()
    {
        if (selectedSourceButtonObj != null)
        {
            Image img = selectedSourceButtonObj.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
        selectedSourceButtonObj = null;
    }

    /// <summary>
    /// Called by a ComputerFieldSlot when clicked. Now performs real
    /// validation via ComputerFieldSlot.TryAssign, and clears selection
    /// regardless of correctness (matching "you tried to place it,
    /// right or wrong, you're no longer holding it" — a simple, forgiving
    /// rule appropriate for a prototype; the player just re-selects a
    /// source and tries again).
    /// </summary>
    public void OnSlotClicked(string slotId)
    {
        if (selectedSource == null) return;

        ComputerFieldSlot targetSlot = GetSlot(slotId);
        if (targetSlot == null) return;

        targetSlot.TryAssign(selectedSource);

        selectedSource = null;
        ClearSelectionHighlight();

        CheckIfReadyToCalculate();
    }

    /// <summary>
    /// IPointerClickHandler on the panel's own background — clicking any
    /// empty area of the Computer panel cancels the current selection,
    /// matching a natural "click away to deselect" expectation.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        selectedSource = null;
        ClearSelectionHighlight();
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

    public void OnCalculatePressed()
    {
        CaseData data = CaseManager.Instance.CurrentCase;

        data.grossIncome = grossIncomeSlot.CurrentValue;
        data.allowableExpenses = allowableExpensesSlot.CurrentValue;
        data.taxWithheldOrCredits = taxCreditsSlot.CurrentValue;

        data.taxableIncome = TaxComputationCalculator.ComputeTaxableIncome(data.grossIncome, data.allowableExpenses);

        TaxOption optionToUse = data.taxOption ?? TaxOption.EightPercentTaxRate;
        data.taxDue = TaxComputationCalculator.ComputeTaxDue(data.taxableIncome, optionToUse);

        data.finalTaxPayable = TaxComputationCalculator.ComputeFinalTaxPayable(data.taxDue, data.taxWithheldOrCredits);

        data.computationStatus = ComputationStatus.Computed;

        resultSummaryText.text =
            $"Taxable Income: \u20b1{data.taxableIncome:N0}\n" +
            $"Tax Due: \u20b1{data.taxDue:N0}\n" +
            $"Final Tax Payable: \u20b1{data.finalTaxPayable:N0}\n" +
            $"Status: Computed";

        fileFormButton.SetActive(true);
    }

    /// <summary>
    /// SUB TAB 2 — TAX RETURN FILING.
    /// Determines the correct BIR form per design doc Section 8 (Form
    /// Selection Guide) from the taxpayer's already-established Taxpayer
    /// Type and Tax Option, and writes Page 5's Filing Information fields.
    /// </summary>
    public void OnFileFormPressed()
    {
        CaseData data = CaseManager.Instance.CurrentCase;

        RequiredForm form = DetermineRequiredForm(data.taxpayerType, data.taxOption);
        data.requiredForm = form;
        data.filingStatus = FilingStatus.ReadyForFiling;

        filingResultText.text =
            $"Required Form: {form}\n" +
            $"Filing Status: Ready For Filing";
    }

    /// <summary>
    /// Per design doc Section 8:
    /// - BIR 1700: Compensation Earner
    /// - BIR 1701: Self-Employed / Professional / Mixed Income using Graduated Rate
    /// - BIR 1701A: Qualified Self-Employed/Professional using 8% Rate
    /// </summary>
    private RequiredForm DetermineRequiredForm(TaxpayerType? taxpayerType, TaxOption? taxOption)
    {
        if (taxpayerType == TaxpayerType.CompensationEarner)
        {
            return RequiredForm.BIR1700;
        }

        if (taxOption == TaxOption.EightPercentTaxRate)
        {
            return RequiredForm.BIR1701A;
        }

        // Mixed Income Earner, Self-Employed, or Professional using Graduated Rate
        return RequiredForm.BIR1701;
    }

    /// <summary>
    /// Wired to what was previously the "Close" button — now acts as "Back",
    /// returning to the Computer's desktop instead of exiting first-person
    /// entirely. The Workstation Close button (unchanged) still exits
    /// first-person fully.
    /// </summary>
    public void OnBackToHomePressed()
    {
        Hide();
        computerHomeUI.Show();
    }
}