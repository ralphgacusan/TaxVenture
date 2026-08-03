using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Displays the printed Tax Return exactly as it was confirmed, per
/// whichever BIR form the player actually filed (1700/1701/1701A).
/// Fully dynamic — field list comes from BirFormFieldDefinition
/// (Milestone 12.5, unchanged), so adding a future 4th form only requires
/// adding it there; this script never hardcodes which fields belong to
/// which form.
///
/// RESPONSIBILITIES:
/// - Read CaseData.encodedForm (what was actually confirmed/printed)
/// - Spawn one read-only TaxReturnViewerRow per field defined for that form
/// - Show "no return has been filed yet" state if encodedForm is null/unconfirmed
///
/// CONNECTS WITH:
/// - HudSubmittableIcon (Tax Return icon): onClickOpenPanel calls Show()
/// - BirFormFieldDefinition: field list per form type
/// - CaseData.encodedForm: the actual filed values (EncodedFormData)
/// </summary>
public class TaxReturnViewerUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI formTitleText;
    [SerializeField] private GameObject noReturnMessage; // shown if nothing has been printed yet

    [Header("Rows")]
    [SerializeField] private Transform rowListRoot;
    [SerializeField] private TaxReturnViewerRow rowPrefab;

    [Header("Close")]
    [SerializeField] private SwipeDownToClose swipeToClose; // reuse R5's gesture if desired

    private List<TaxReturnViewerRow> spawnedRows = new List<TaxReturnViewerRow>();

    private void Awake()
    {
        if (swipeToClose != null)
            swipeToClose.OnSwipeClosed += Hide;

        panelRoot.SetActive(false);
    }

    public void Show()
    {
        CameraController.Instance?.LockPlayerControls();

        CaseData data = CaseManager.Instance.CurrentCase;
        EncodedFormData form = data.encodedForm;

        WorkspaceLayoutManager.Instance.LeftZone.ShowPanel(panelRoot);

        bool hasFiledReturn = form != null && form.isConfirmed;
        noReturnMessage.SetActive(!hasFiledReturn);
        rowListRoot.gameObject.SetActive(hasFiledReturn);

        if (!hasFiledReturn)
        {
            formTitleText.text = "No Tax Return Filed Yet";
            ClearRows();
            return;
        }

        formTitleText.text = $"BIR Form {form.selectedForm}";
        BuildRows(form);
    }

    public void Hide()
    {
        WorkspaceLayoutManager.Instance.LeftZone.HidePanel(panelRoot);

        CameraController.Instance?.UnlockPlayerControls();
    }

    private void BuildRows(EncodedFormData form)
    {
        ClearRows();

        var fieldDefs = BirFormFieldDefinition.GetFieldsFor(form.selectedForm);

        foreach (var def in fieldDefs)
        {
            string displayValue = GetFieldValue(form, def.Id);
            if (def.IsNumeric && float.TryParse(displayValue, out float numeric))
            {
                displayValue = $"\u20b1{numeric:N0}";
            }

            var rowObj = Instantiate(rowPrefab, rowListRoot);
            rowObj.Initialize(def.Label, displayValue);
            spawnedRows.Add(rowObj);
        }
    }

    /// <summary>
    /// Maps an EncodedFieldId to the actual string the player confirmed for
    /// it. Mirrors the same switch used when WRITING these fields in
    /// BirFormEncodingUI — kept in sync manually since they represent two
    /// different directions of the same field set (write vs. read).
    /// </summary>
    private string GetFieldValue(EncodedFormData form, EncodedFieldId id)
    {
        return id switch
        {
            EncodedFieldId.FullName => form.fullName,
            EncodedFieldId.Tin => form.tin,
            EncodedFieldId.Address => form.address,
            EncodedFieldId.ResidencyStatus => form.residencyStatus,
            EncodedFieldId.TaxpayerType => form.taxpayerType,
            EncodedFieldId.IncomeSource => form.incomeSource,
            EncodedFieldId.GrossIncome => form.grossIncome,
            EncodedFieldId.AllowableExpenses => form.allowableExpenses,
            EncodedFieldId.TaxableIncome => form.taxableIncome,
            EncodedFieldId.TaxDue => form.taxDue,
            EncodedFieldId.TaxCredits => form.taxCredits,
            EncodedFieldId.FinalTaxPayable => form.finalTaxPayable,
            _ => ""
        };
    }

    private void ClearRows()
    {
        foreach (var row in spawnedRows) Destroy(row.gameObject);
        spawnedRows.Clear();
    }
}