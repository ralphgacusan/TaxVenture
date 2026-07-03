/// <summary>
/// PURPOSE:
/// Builds the DocumentFieldData for each of the 7 supporting document types,
/// using the example values from the design document's Page 6 tables. This
/// is the document equivalent of CaseFolderUI.BuildPages() — the one place
/// you edit to change what a document displays.
///
/// WHY A SEPARATE STATIC CLASS (not inside CaseData):
/// This is presentation data (what fields to show and in what order), not
/// case state. CaseData.supportingDocuments only tracks name + reviewed
/// status; the actual field values shown in the viewer are generated here.
/// If a later milestone wants documents with per-case-randomized values,
/// this is the one place that would need to change.
///
/// CONNECTS WITH:
/// - DocumentViewerUI: calls GetFieldsFor(documentName) when a document is clicked
/// </summary>
public static class DocumentDataProvider
{
    public static DocumentFieldData GetFieldsFor(string documentName)
    {
        switch (documentName)
        {
            case "BIR Form 2316":
                return new DocumentFieldData(documentName)
                    .AddField("Employer Name", "ABC Corporation")
                    .AddField("Total Compensation", "\u20b1500,000")
                    .AddField("Tax Withheld", "\u20b145,000");

            case "BIR Form 2303":
                return new DocumentFieldData(documentName)
                    .AddField("Business Name", "JD Online Services")
                    .AddField("Taxpayer TIN", "123-456-789-000")
                    .AddField("Tax Type", "Non-VAT / 8% Tax Rate");

            case "Financial Statements":
                return new DocumentFieldData(documentName)
                    .AddField("Gross Sales", "\u20b1350,000")
                    .AddField("Total Expenses", "\u20b1150,000")
                    .AddField("Net Income", "\u20b1200,000");

            case "Sales Records":
                return new DocumentFieldData(documentName)
                    .AddField("Date", "March 15, 2026")
                    .AddField("Customer", "XYZ Trading")
                    .AddField("Amount", "\u20b125,000");

            case "Bank Statements":
                return new DocumentFieldData(documentName)
                    .AddField("Deposit", "\u20b150,000")
                    .AddField("Withdrawal", "\u20b110,000")
                    .AddField("Balance", "\u20b1250,000");

            case "Property Documents":
                return new DocumentFieldData(documentName)
                    .AddField("Property Owner", "Juan Dela Cruz")
                    .AddField("Assessed Value", "\u20b11,500,000")
                    .AddField("Selling Price", "\u20b12,000,000");

            case "Previous Year's ITR":
                return new DocumentFieldData(documentName)
                    .AddField("Declared Income", "\u20b1600,000")
                    .AddField("Tax Due", "\u20b145,000")
                    .AddField("Filing Date", "April 15, 2025");

            default:
                return new DocumentFieldData(documentName)
                    .AddField("(No data available)", "");
        }
    }
}