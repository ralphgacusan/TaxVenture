using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// The Tax Return encoding screen. Field definitions come from
/// BirFormFieldDefinition (which form has which fields); each field is
/// filled via the click-select/click-place system rather than typing.
/// </summary>
public class BirFormEncodingUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI formTitleText;

    [Header("Field Slots")]
    [SerializeField] private Transform fieldListRoot;
    [SerializeField] private TaxReturnFieldSlot fieldSlotPrefab;

    [Header("Confirm / Print")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonLabel;

    [SerializeField] private PrintJobController printJobController;
    [SerializeField] private ComputerHomeUI computerHomeUI;

    private List<TaxReturnFieldSlot> spawnedSlots = new List<TaxReturnFieldSlot>();
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

        BuildSlots(form);
        UpdateActionButton();
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    private void BuildSlots(RequiredForm form)
    {
        foreach (var slot in spawnedSlots) Destroy(slot.gameObject);
        spawnedSlots.Clear();

        var fields = BirFormFieldDefinition.GetFieldsFor(form);
        foreach (var field in fields)
        {
            var slotObj = Instantiate(fieldSlotPrefab, fieldListRoot);
            slotObj.Initialize(field, OnFieldValueSet);
            spawnedSlots.Add(slotObj);
        }
    }

    private void OnFieldValueSet(EncodedFieldId id, string value)
    {
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

    private bool AllSlotsFilled()
    {
        foreach (var slot in spawnedSlots)
        {
            if (!slot.IsFilled) return false;
        }
        return true;
    }

    private void UpdateActionButton()
    {
        if (isConfirmed)
        {
            actionButtonLabel.text = "Print Tax Return";
            actionButton.interactable = true;
            return;
        }

        actionButtonLabel.text = "Confirm Form";
        actionButton.interactable = AllSlotsFilled();
    }

    private void OnActionButtonPressed()
    {
        if (!isConfirmed)
        {
            isConfirmed = true;
            formData.isConfirmed = true;
            UpdateActionButton();
        }
        else
        {
            Hide();
            printJobController.BeginPrint(formData);
        }
    }

    public void OnBackPressed()
    {
        Hide();
        computerHomeUI.Show();
    }
}