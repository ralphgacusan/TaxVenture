using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Defines which fields appear on each BIR form type, per design doc's
/// Sub Tab 2.1/2.2/2.3 field lists. One place to add/remove fields per form
/// without touching UI code — BirFormEncodingUI just iterates whatever this
/// returns and builds input fields dynamically.
/// </summary>
public enum EncodedFieldId
{
    FullName, Tin, Address, ResidencyStatus, TaxpayerType, IncomeSource,
    GrossIncome, AllowableExpenses, TaxableIncome, TaxDue, TaxCredits, FinalTaxPayable
}

public class EncodedFieldDefinition
{
    public EncodedFieldId Id;
    public string Label;
    public bool IsNumeric;

    public EncodedFieldDefinition(EncodedFieldId id, string label, bool isNumeric)
    {
        Id = id;
        Label = label;
        IsNumeric = isNumeric;
    }
}

public static class BirFormFieldDefinition
{
    public static List<EncodedFieldDefinition> GetFieldsFor(RequiredForm form)
    {
        switch (form)
        {
            case RequiredForm.BIR1700:
                return new List<EncodedFieldDefinition>
                {
                    new EncodedFieldDefinition(EncodedFieldId.FullName, "Full Name", false),
                    new EncodedFieldDefinition(EncodedFieldId.Tin, "TIN", false),
                    new EncodedFieldDefinition(EncodedFieldId.Address, "Address", false),
                    new EncodedFieldDefinition(EncodedFieldId.ResidencyStatus, "Residency Status", false),
                    new EncodedFieldDefinition(EncodedFieldId.GrossIncome, "Gross Income", true),
                    new EncodedFieldDefinition(EncodedFieldId.TaxDue, "Tax Due", true),
                    new EncodedFieldDefinition(EncodedFieldId.TaxCredits, "Tax Credits", true),
                    new EncodedFieldDefinition(EncodedFieldId.FinalTaxPayable, "Final Tax Payable", true),
                };

            case RequiredForm.BIR1701:
                return new List<EncodedFieldDefinition>
                {
                    new EncodedFieldDefinition(EncodedFieldId.FullName, "Full Name", false),
                    new EncodedFieldDefinition(EncodedFieldId.Tin, "TIN", false),
                    new EncodedFieldDefinition(EncodedFieldId.TaxpayerType, "Taxpayer Type", false),
                    new EncodedFieldDefinition(EncodedFieldId.IncomeSource, "Income Source", false),
                    new EncodedFieldDefinition(EncodedFieldId.GrossIncome, "Gross Income", true),
                    new EncodedFieldDefinition(EncodedFieldId.AllowableExpenses, "Allowable Expenses", true),
                    new EncodedFieldDefinition(EncodedFieldId.TaxableIncome, "Taxable Income", true),
                    new EncodedFieldDefinition(EncodedFieldId.TaxDue, "Tax Due", true),
                    new EncodedFieldDefinition(EncodedFieldId.TaxCredits, "Tax Credits", true),
                    new EncodedFieldDefinition(EncodedFieldId.FinalTaxPayable, "Final Tax Payable", true),
                };

            case RequiredForm.BIR1701A:
                return new List<EncodedFieldDefinition>
                {
                    new EncodedFieldDefinition(EncodedFieldId.FullName, "Full Name", false),
                    new EncodedFieldDefinition(EncodedFieldId.Tin, "TIN", false),
                    new EncodedFieldDefinition(EncodedFieldId.TaxpayerType, "Taxpayer Type", false),
                    new EncodedFieldDefinition(EncodedFieldId.GrossIncome, "Gross Income", true),
                    new EncodedFieldDefinition(EncodedFieldId.TaxDue, "Tax Due", true),
                    new EncodedFieldDefinition(EncodedFieldId.TaxCredits, "Tax Credits", true),
                    new EncodedFieldDefinition(EncodedFieldId.FinalTaxPayable, "Final Tax Payable", true),
                };

            default:
                return new List<EncodedFieldDefinition>();
        }
    }
}