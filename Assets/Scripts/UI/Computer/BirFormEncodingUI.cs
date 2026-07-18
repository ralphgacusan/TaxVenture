using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// The manual encoding screen itself. Builds input rows dynamically from
/// BirFormFieldDefinition (never hardcodes fields per form type). Writes
/// everything typed into CaseData.encodedForm — deliberately NEVER reads
/// from CaseData's authoritative fields to pre-fill anything, per the
/// explicit "nothing auto-fills" requirement. The player must look at the
/// Case Folder / Computation results themselves and type.
///
/// RESPONSIBILITIES:
/// - Spawn one EncodedFieldInputRow per field for the chosen form
/// - Track completion (all rows non-empty) to reveal Confirm
/// - On Confirm: lock all rows read-only, swap button to Print
/// - On Print: hand off to PrintJobController
///
/// CONNECTS WITH:
/// - FormSelectionUI: launches this with a chosen RequiredForm
/// - BirFormFieldDefinition: field list source
/// - CaseManager.Instance.CurrentCase.encodedForm: write target
/// - PrintJobController: triggered on Print
/// </summary>
public class BirFormEncodingUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI formTitleText;

    [Header("Field Rows")]
    [SerializeField] private Transform fieldListRoot;
    [SerializeField] private GameObject fieldRowPrefab;

    [Header("Confirm / Print")]
    [SerializeField] private Button actionButton; // becomes Confirm, then Print
    [SerializeField] private TextMeshProUGUI actionButtonLabel;

    [SerializeField] private PrintJobController printJobController;
    [SerializeField] private ComputerHomeUI computerHomeUI;

    private List<EncodedFieldInputRow> spawnedRows = new List<EncodedFieldInputRow>();
    private EncodedFormData formData;
    private bool isConfirmed = false;

    private void Awake()
    {
        Hide();
        actionButton.onClick.AddListener(OnActionButtonPressed);
    }

    public void Show(RequiredForm form)
    {
        panelRoot.SetActive(true);
        formTitleText.text = $"BIR Form {form}";

        formData = new EncodedFormData { selectedForm = form };
        CaseManager.Instance.CurrentCase.encodedForm = formData;
        isConfirmed = false;

        BuildRows(form);
        UpdateActionButton();
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    private void BuildRows(RequiredForm form)
    {
        foreach (var row in spawnedRows) Destroy(row.gameObject);
        spawnedRows.Clear();

        var fields = BirFormFieldDefinition.GetFieldsFor(form);
        foreach (var field in fields)
        {
            GameObject rowObj = Instantiate(fieldRowPrefab, fieldListRoot);
            EncodedFieldInputRow row = rowObj.GetComponent<EncodedFieldInputRow>();
            row.Initialize(field, "", OnFieldValueChanged);
            spawnedRows.Add(row);
        }
    }

    private void OnFieldValueChanged(EncodedFieldId id, string value)
    {
        // Deliberately NO validation here — "Do NOT validate during typing."
        SetFieldValue(id, value);
        UpdateActionButton();
    }

    private void SetFieldValue(EncodedFieldId id, string value)
    {
        switch (id)
        {
            case EncodedFieldId.FullName: formData.fullName = value; break;
            case EncodedFieldId.Tin: formData.tin = value; break;
            case EncodedFieldId.Address: formData.address = value; break;
            case EncodedFieldId.ResidencyStatus: formData.residencyStatus = value; break;
            case EncodedFieldId.TaxpayerType: formData.taxpayerType = value; break;
            case EncodedFieldId.IncomeSource: formData.incomeSource = value; break;
            case EncodedFieldId.GrossIncome: formData.grossIncome = value; break;
            case EncodedFieldId.AllowableExpenses: formData.allowableExpenses = value; break;
            case EncodedFieldId.TaxableIncome: formData.taxableIncome = value; break;
            case EncodedFieldId.TaxDue: formData.taxDue = value; break;
            case EncodedFieldId.TaxCredits: formData.taxCredits = value; break;
            case EncodedFieldId.FinalTaxPayable: formData.finalTaxPayable = value; break;
        }
    }

    /// <summary>
    /// Only checks whether fields have SOMETHING typed (not whether it's
    /// correct — "Do NOT validate during Confirm. The Auditor is the ONLY
    /// validation system.").
    /// </summary>
    private bool AllFieldsFilled()
    {
        foreach (var row in spawnedRows)
        {
            var field = GetFieldValue(row.FieldId);
            if (string.IsNullOrWhiteSpace(field)) return false;
        }
        return true;
    }

    private string GetFieldValue(EncodedFieldId id)
    {
        switch (id)
        {
            case EncodedFieldId.FullName: return formData.fullName;
            case EncodedFieldId.Tin: return formData.tin;
            case EncodedFieldId.Address: return formData.address;
            case EncodedFieldId.ResidencyStatus: return formData.residencyStatus;
            case EncodedFieldId.TaxpayerType: return formData.taxpayerType;
            case EncodedFieldId.IncomeSource: return formData.incomeSource;
            case EncodedFieldId.GrossIncome: return formData.grossIncome;
            case EncodedFieldId.AllowableExpenses: return formData.allowableExpenses;
            case EncodedFieldId.TaxableIncome: return formData.taxableIncome;
            case EncodedFieldId.TaxDue: return formData.taxDue;
            case EncodedFieldId.TaxCredits: return formData.taxCredits;
            case EncodedFieldId.FinalTaxPayable: return formData.finalTaxPayable;
            default: return "";
        }
    }

    private void UpdateActionButton()
    {
        if (isConfirmed)
        {
            actionButtonLabel.text = "Print Tax Return";
            actionButton.interactable = true;
            return;
        }

        bool ready = AllFieldsFilled();
        actionButtonLabel.text = "Confirm Form";
        actionButton.interactable = ready;
    }

    private void OnActionButtonPressed()
    {
        if (!isConfirmed)
        {
            ConfirmForm();
        }
        else
        {
            StartPrint();
        }
    }

    private void ConfirmForm()
    {
        isConfirmed = true;
        formData.isConfirmed = true;

        foreach (var row in spawnedRows)
        {
            row.SetReadOnly(true);
        }

        UpdateActionButton();
    }

    private void StartPrint()
    {
        Hide();
        printJobController.BeginPrint(formData);
        CameraController.Instance.ExitFirstPerson();
    }

    public void OnBackPressed()
    {
        // Back is only meaningful before confirming — once confirmed, the
        // form is locked and the player should proceed to Print rather
        // than escape. We still allow leaving (e.g. to re-check the Case
        // Folder) without losing progress, since formData is stored on
        // CaseData.encodedForm, not local state.
        Hide();
        computerHomeUI.Show();
    }
}