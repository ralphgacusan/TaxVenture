/// <summary>
/// PURPOSE:
/// Static provider of each supporting document's field content, including
/// what data type/semantic key each field represents for the value
/// transfer system.
/// </summary>
public static class DocumentDataProvider
{
    public static DocumentFieldData GetFieldsFor(string documentName)
    {
        switch (documentName)
        {
            case "BIR Form 2316":
                return new DocumentFieldData(documentName)
                    .AddField("Employer Name", "ABC Corporation", DataValueType.Text, "EmployerName")
                    .AddField("Total Compensation", "\u20b1500,000", DataValueType.Number, "GrossIncome")
                    .AddField("Tax Withheld", "\u20b145,000", DataValueType.Number, "TaxCredits");

            case "BIR Form 2303":
                return new DocumentFieldData(documentName)
                    .AddField("Business Name", "JD Online Services", DataValueType.Text, "BusinessName")
                    .AddField("Taxpayer TIN", "123-456-789-000", DataValueType.Text, "TIN")
                    .AddField("Tax Type", "Non-VAT / 8% Tax Rate", DataValueType.Text, "TaxOption");

            case "Financial Statements":
                return new DocumentFieldData(documentName)
                    .AddField("Gross Sales", "\u20b1350,000", DataValueType.Number, "GrossIncome")
                    .AddField("Total Expenses", "\u20b1150,000", DataValueType.Number, "AllowableExpenses")
                    .AddField("Net Income", "\u20b1200,000", DataValueType.Number, "NetIncome");

            case "Sales Records":
                return new DocumentFieldData(documentName)
                    .AddField("Date", "March 15, 2026", DataValueType.Text, "SalesDate")
                    .AddField("Customer", "XYZ Trading", DataValueType.Text, "Customer")
                    .AddField("Amount", "\u20b125,000", DataValueType.Number, "GrossIncome");

            case "Bank Statements":
                return new DocumentFieldData(documentName)
                    .AddField("Deposit", "\u20b150,000", DataValueType.Number, "Deposit")
                    .AddField("Withdrawal", "\u20b110,000", DataValueType.Number, "Withdrawal")
                    .AddField("Balance", "\u20b1250,000", DataValueType.Number, "Balance");

            case "Property Documents":
                return new DocumentFieldData(documentName)
                    .AddField("Property Owner", "Juan Dela Cruz", DataValueType.Text, "PropertyOwner")
                    .AddField("Assessed Value", "\u20b11,500,000", DataValueType.Number, "AssessedValue")
                    .AddField("Selling Price", "\u20b12,000,000", DataValueType.Number, "SellingPrice");

            case "Previous Year's ITR":
                return new DocumentFieldData(documentName)
                    .AddField("Declared Income", "\u20b1600,000", DataValueType.Number, "PriorDeclaredIncome")
                    .AddField("Tax Due", "\u20b145,000", DataValueType.Number, "PriorTaxDue")
                    .AddField("Filing Date", "April 15, 2025", DataValueType.Text, "PriorFilingDate");

            default:
                return new DocumentFieldData(documentName)
                    .AddField("(No data available)", "", DataValueType.Text, "");
        }
    }
}