using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Controls a single, static Case Folder panel: one "paper" in the center
/// that never moves, with Previous on its left and Next on its right.
/// Clicking Next/Previous does NOT swap GameObjects or play any animation —
/// it simply rewrites the paper's heading/body text to match the new page's
/// data. This matches Pages 1-7 from the design doc conceptually, but all
/// pages share the exact same visual paper.
///
/// RESPONSIBILITIES:
/// - Build page content (heading + body text) for Pages 1-7 from CaseData
/// - Show/hide the folder panel
/// - On Next/Previous, advance/retreat currentPageIndex and re-render the
///   single paper's text
///
/// CONNECTS WITH:
/// - CaseFolderInteractable: calls Show() when the folder is clicked
/// - Next/Previous buttons: wired to NextPage()/PreviousPage()
/// - Close button: wired to Hide()
/// - GameStateMachine: on first Show(), advances ReceiveCaseState -> ReviewDocumentsState
/// - CaseManager.Instance.CurrentCase: source of all displayed data
/// </summary>
public class CaseFolderUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject folderPanelRoot;

    [Header("The Paper (single, static — never moves)")]
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText; // e.g. "Page 2 / 7"


    [Header("Page 6 Document Links")]
    [SerializeField] private SupportingDocumentsPageLink supportingDocumentsPageLink;

    [Header("Stamping")]
    [SerializeField] private StampUI stampUI;
    [SerializeField] private GameObject stampPanel; // holds the 2 stamp buttons, right side of folder
    [SerializeField] private Button paperClickTarget; // Button component on the Paper background

    [Header("Page Edge Navigation (replaces Prev/Next buttons)")]
    [SerializeField] private PageEdgeTapZone leftEdgeZone;
    [SerializeField] private PageEdgeTapZone rightEdgeZone;
    [SerializeField] private GameObject leftEdgeIndicator; // "<" visual
    [SerializeField] private GameObject rightEdgeIndicator; // ">" visual

    [Header("Swipe to Close (replaces Close button)")]
    [SerializeField] private SwipeDownToClose swipeToClose;


    [Header("Editable Page Rows")]
    [SerializeField] private Transform page2FieldRowListRoot;
    [SerializeField] private Transform page3FieldRowListRoot;
    [SerializeField] private Transform page4FieldRowListRoot;
    [SerializeField] private Transform page5FieldRowListRoot;
    [SerializeField] private CaseFolderFieldRow caseFolderFieldRowPrefab;
    [SerializeField] private GameObject page2FieldRowsContainer;
    [SerializeField] private GameObject page3FieldRowsContainer;
    [SerializeField] private GameObject page4FieldRowsContainer;
    [SerializeField] private GameObject page5FieldRowsContainer;

    [Header("Additional Row Containers (source-only fields)")]
    [SerializeField] private Transform page1FieldRowListRoot;
    [SerializeField] private GameObject page1FieldRowsContainer;

    [SerializeField] private Transform page2FixedFieldRowListRoot; // separate from the editable Page2 rows
    [SerializeField] private GameObject page2FixedFieldRowsContainer;

    [SerializeField] private Transform page7FieldRowListRoot;
    [SerializeField] private GameObject page7FieldRowsContainer;

    private List<CaseFolderFieldRow> page2Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page3Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page4Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page5Rows = new List<CaseFolderFieldRow>();

    private const int Page2Index = 1; // Taxpayer Information
    private const int Page3Index = 2; // Income Information
    private const int Page4Index = 3; // Tax Computation
    private const int Page5Index = 4; // Filing Information


    private List<CaseFolderFieldRow> page1Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page2FixedRows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page7Rows = new List<CaseFolderFieldRow>();

    private const int Page1Index = 0;
    private const int Page6Index = 5;
    private const int Page7Index = 6;

    private List<CaseFolderPageContent> pages;
    private int currentPageIndex = 0;
    private bool hasOpenedBefore = false;

    private bool isStampingMode = false;

    private void Awake()
    {
        leftEdgeZone.OnTapped += PreviousPage;
        rightEdgeZone.OnTapped += NextPage;
        swipeToClose.OnSwipeClosed += Hide;

        if (paperClickTarget != null)
        {
            paperClickTarget.onClick.AddListener(OnPaperClicked);
        }
    }

    /// <summary>
    /// Called by CaseFolderInteractable.OnInteract(). Builds fresh page
    /// content from the current CaseData every time it's opened, so any
    /// changes made elsewhere (Interview answers, computed tax values)
    /// always show up-to-date.
    /// </summary>
    public void Show()
    {
        Debug.Log("Case Folder Show");
        pages = BuildPages(CaseManager.Instance.CurrentCase);
        WorkspaceLayoutManager.Instance.RightZone.ShowPanel(folderPanelRoot); // CHANGED: was folderPanelRoot.SetActive(true)
        currentPageIndex = 0;
        RenderPage(currentPageIndex);

        if (!hasOpenedBefore)
        {
            hasOpenedBefore = true;
            if (GameStateMachine.Instance.CurrentState is ReceiveCaseState)
            {
                GameStateMachine.Instance.ChangeState(new ReviewDocumentsState());
            }
        }
    }

    /// <summary>
    /// Opens the folder forced to Page 1, with the stamp panel visible on the
    /// right side. Called by StampSetInteractable instead of the normal Show().
    /// </summary>
    public void ShowForStamping()
    {
        pages = BuildPages(CaseManager.Instance.CurrentCase);

        WorkspaceLayoutManager.Instance.RightZone.ShowPanel(folderPanelRoot);

        isStampingMode = true;      // Move this up

        currentPageIndex = 0;

        RenderPage(currentPageIndex);

        stampPanel.SetActive(true);
        stampUI.ResetSelection();
    }

    // In Hide(), also close stamping mode:

    public void Hide()
    {
        WorkspaceLayoutManager.Instance.RightZone.HidePanel(folderPanelRoot); // CHANGED: was folderPanelRoot.SetActive(false)
        isStampingMode = false;
        stampPanel?.SetActive(false);
    }
    public void NextPage()
    {
        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;
            RenderPage(currentPageIndex);
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            RenderPage(currentPageIndex);
        }
    }

    /// <summary>
    /// Rewrites the paper's text to match the given page index. No object
    /// swapping, no animation — literally just changing .text on the same
    /// TextMeshPro components every time.
    /// </summary>
    private const int TaxComputationPageIndex = 3; // Page 4, zero-indexed

    private void RenderPage(int index)
    {
        CaseFolderPageContent page = pages[index];
        headingText.text = page.Heading;
        pageIndicatorText.text = $"Page {index + 1} / {pages.Count}";

        leftEdgeIndicator.SetActive(index > 0);
        rightEdgeIndicator.SetActive(index < pages.Count - 1);

        supportingDocumentsPageLink?.OnFolderPageChanged(index);

        if (paperClickTarget != null)
        {
            paperClickTarget.enabled = isStampingMode && index == Page1Index;
        }

        CaseData data = CaseManager.Instance.CurrentCase;

        page1FieldRowsContainer.SetActive(false);
        page2FieldRowsContainer.SetActive(false);
        page2FixedFieldRowsContainer.SetActive(false);
        page3FieldRowsContainer.SetActive(false);
        page4FieldRowsContainer.SetActive(false);
        page5FieldRowsContainer.SetActive(false);
        page7FieldRowsContainer.SetActive(false);
        bodyText.gameObject.SetActive(true);
        bodyText.text = page.Body;

        switch (index)
        {
            case Page1Index:
                bodyText.gameObject.SetActive(false);
                page1FieldRowsContainer.SetActive(true);
                BuildPage1Rows(data);
                break;

            case Page2Index:
                bodyText.gameObject.SetActive(false);
                page2FieldRowsContainer.SetActive(true);
                page2FixedFieldRowsContainer.SetActive(true);
                BuildPage2FixedRows(data);
                BuildPage2Rows(data);
                break;

            case Page3Index:
                bodyText.gameObject.SetActive(false);
                page3FieldRowsContainer.SetActive(true);
                BuildPage3Rows(data);
                break;

            case Page4Index:
                bodyText.gameObject.SetActive(false);
                page4FieldRowsContainer.SetActive(true);
                BuildPage4Rows(data);
                break;

            case Page5Index:
                bodyText.gameObject.SetActive(false);
                page5FieldRowsContainer.SetActive(true);
                BuildPage5Rows(data);
                break;

            case Page7Index:
                bodyText.gameObject.SetActive(false);
                page7FieldRowsContainer.SetActive(true);
                BuildPage7Rows(data);
                break;

                // Page6Index intentionally falls through to default —
                // SupportingDocumentsPageLink already handles it separately.
        }
    }

    /// <summary>
    /// Builds the 7 pages' worth of text content from CaseData. This is the
    /// ONLY place that needs to change when you want to add/edit page
    /// content — no new GameObjects, no new prefabs, just text here.
    /// </summary>
    private List<CaseFolderPageContent> BuildPages(CaseData data)
    {
        var list = new List<CaseFolderPageContent>();

        // Page 1 - Case Overview
        string assessmentText;

        if (!data.assessmentStamped)
        {
            assessmentText = "Case Assessment: __________";
        }
        else
        {
            assessmentText = $"Case Assessment: {EnumDisplayFormatter.Format(data.caseAssessment.ToString())}";
        }

        list.Add(new CaseFolderPageContent(
            "Case Overview",
            $"{data.caseTitle}\n\n{data.caseSummary}\n\n{assessmentText}"
        ));

        // Page 2 - Taxpayer Information
        StringBuilder p2 = new StringBuilder();
        p2.AppendLine($"Full Name: {data.fullName}");
        p2.AppendLine($"TIN: {data.tin}");
        p2.AppendLine($"Birthdate: {data.birthdate}");
        p2.AppendLine($"Address: {data.address}");
        p2.AppendLine($"Contact Number: {data.contactNumber}");
        p2.AppendLine($"Civil Status: {EnumDisplayFormatter.Format(data.civilStatus.ToString())}");
        if (data.civilStatus == CivilStatus.Married)
        {
            p2.AppendLine($"Spouse Name: {data.spouseName}");
            p2.AppendLine($"Spouse TIN: {data.spouseTin}");
        }
        p2.AppendLine($"Citizenship: {data.citizenship}");
        p2.AppendLine($"Residency Status: {EnumDisplayFormatter.Format(data.residencyStatus)}");
        p2.Append($"Taxpayer Type: {EnumDisplayFormatter.Format(data.taxpayerType)}");
        list.Add(new CaseFolderPageContent("Taxpayer Information", p2.ToString()));

        // Page 3 - Income Information
        StringBuilder p3 = new StringBuilder();
        p3.AppendLine($"Income Sources: {EnumDisplayFormatter.Format(data.incomeSource)}");
        p3.AppendLine($"Number of Employers: {EnumDisplayFormatter.Format(data.numberOfEmployers)}");
        p3.AppendLine($"Business Registration: {EnumDisplayFormatter.Format(data.businessRegistration)}");
        p3.Append($"Tax Option: {EnumDisplayFormatter.Format(data.taxOption)}");
        list.Add(new CaseFolderPageContent("Income Information", p3.ToString()));

        // Page 4 - Tax Computation Information
        // Left blank — CaseFolderFieldRow value-transfer rows display this instead.
        list.Add(new CaseFolderPageContent(
            "Tax Computation Information",
            ""
        ));

        // Page 5 - Filing Information
        StringBuilder p5 = new StringBuilder();
        p5.AppendLine($"Required Form: {(data.requiredForm.HasValue ? data.requiredForm.ToString() : "?")}");
        p5.AppendLine($"Filing Status: {EnumDisplayFormatter.Format(data.filingStatus.ToString())}");
        p5.AppendLine($"Submission Date: {(string.IsNullOrEmpty(data.submissionDate) ? "-" : data.submissionDate)}");
        p5.AppendLine($"Remarks: {(string.IsNullOrEmpty(data.remarks) ? "-" : data.remarks)}");
        if (data.encodedForm != null)
        {
            p5.Append($"Encoded Form On File: BIR {data.encodedForm.selectedForm}{(data.encodedForm.isConfirmed ? " (Confirmed)" : " (Draft)")}");
        }
        list.Add(new CaseFolderPageContent("Filing Information", p5.ToString()));

        // Page 6 - Supporting Documents
        // Left blank — SupportingDocumentsPageLink displays the clickable list instead.
        list.Add(new CaseFolderPageContent("Supporting Documents", ""));

        // Page 7 - Consultant Findings
        StringBuilder p7 = new StringBuilder();
        p7.AppendLine($"Residency Status: {EnumDisplayFormatter.Format(data.residencyStatus)}");
        p7.AppendLine($"Taxpayer Type: {EnumDisplayFormatter.Format(data.taxpayerType)}");
        p7.AppendLine();
        p7.AppendLine("Potential Issues Identified:");
        foreach (var issue in data.potentialIssuesIdentified)
        {
            p7.AppendLine($"- {issue}");
        }
        list.Add(new CaseFolderPageContent("Consultant Findings", p7.ToString()));

        return list;
    }



    /// <summary>
    /// Paper click only does something meaningful while stamping is active;
    /// otherwise it's just inert background (Next/Previous still work normally).
    /// </summary>
    private void OnPaperClicked()
    {
        if (isStampingMode && stampUI.HasSelectedStamp)
        {
            stampUI.TryApplyStampToPaper();
        }
    }

    public void Refresh()
    {
        pages = BuildPages(CaseManager.Instance.CurrentCase);
        RenderPage(currentPageIndex);
    }
    private void BuildPage1Rows(CaseData data)
    {
        ClearRows(page1Rows);

        AddSourceOnlyRow(page1Rows, page1FieldRowListRoot, "Case Number", data.caseNumber, DataValueType.Text, "CaseNumber");
        AddSourceOnlyRow(page1Rows, page1FieldRowListRoot, "Tax Year", data.taxYear, DataValueType.Text, "TaxYear");
        AddSourceOnlyRow(page1Rows, page1FieldRowListRoot, "Date Received", data.dateReceived, DataValueType.Text, "DateReceived");
        AddSourceOnlyRow(page1Rows, page1FieldRowListRoot, "Assigned Consultant", data.assignedConsultant, DataValueType.Text, "AssignedConsultant");
        AddSourceOnlyRow(page1Rows, page1FieldRowListRoot, "Case Title", data.caseTitle, DataValueType.Text, "CaseTitle");
        AddSourceOnlyRow(page1Rows, page1FieldRowListRoot, "Case Assessment",
            data.assessmentStamped ? data.caseAssessment.ToString() : "Not Yet Stamped",
            DataValueType.Text, "CaseAssessment");
    }

    private void BuildPage2FixedRows(CaseData data)
    {
        ClearRows(page2FixedRows);

        AddSourceOnlyRow(page2FixedRows, page2FixedFieldRowListRoot, "Full Name", data.fullName, DataValueType.Text, "FullName");
        AddSourceOnlyRow(page2FixedRows, page2FixedFieldRowListRoot, "TIN", data.tin, DataValueType.Text, "TIN");
        AddSourceOnlyRow(page2FixedRows, page2FixedFieldRowListRoot, "Birthdate", data.birthdate, DataValueType.Text, "Birthdate");
        AddSourceOnlyRow(page2FixedRows, page2FixedFieldRowListRoot, "Address", data.address, DataValueType.Text, "Address");
        AddSourceOnlyRow(page2FixedRows, page2FixedFieldRowListRoot, "Contact Number", data.contactNumber, DataValueType.Text, "ContactNumber");
        AddSourceOnlyRow(page2FixedRows, page2FixedFieldRowListRoot, "Civil Status", data.civilStatus.ToString(), DataValueType.Text, "CivilStatus");

        if (data.civilStatus == CivilStatus.Married)
        {
            AddSourceOnlyRow(page2FixedRows, page2FixedFieldRowListRoot, "Spouse Name", data.spouseName, DataValueType.Text, "SpouseName");
            AddSourceOnlyRow(page2FixedRows, page2FixedFieldRowListRoot, "Spouse TIN", data.spouseTin, DataValueType.Text, "SpouseTIN");
        }

        AddSourceOnlyRow(page2FixedRows, page2FixedFieldRowListRoot, "Citizenship", data.citizenship, DataValueType.Text, "Citizenship");
    }

    private void BuildPage7Rows(CaseData data)
    {
        ClearRows(page7Rows);

        AddSourceOnlyRow(page7Rows, page7FieldRowListRoot, "Residency Status (on file)",
            data.residencyStatus.HasValue ? data.residencyStatus.ToString() : "?",
            DataValueType.Text, "FindingsResidencyStatus");

        AddSourceOnlyRow(page7Rows, page7FieldRowListRoot, "Taxpayer Type (on file)",
            data.taxpayerType.HasValue ? data.taxpayerType.ToString() : "?",
            DataValueType.Text, "FindingsTaxpayerType");

        for (int i = 0; i < data.potentialIssuesIdentified.Count; i++)
        {
            AddSourceOnlyRow(page7Rows, page7FieldRowListRoot, $"Issue {i + 1}",
                data.potentialIssuesIdentified[i], DataValueType.Text, $"Issue{i}");
        }
    }

    // ---------- Page 2: Taxpayer Information ----------
    private void BuildPage2Rows(CaseData data)
    {
        ClearRows(page2Rows);

        AddEnumRow(page2Rows, page2FieldRowListRoot, "Residency Status", "ResidencyStatus", data,
            () => data.residencyStatus?.ToString(),
            v => data.residencyStatus = (ResidencyStatus)System.Enum.Parse(typeof(ResidencyStatus), v));

        AddEnumRow(page2Rows, page2FieldRowListRoot, "Taxpayer Type", "TaxpayerType", data,
            () => data.taxpayerType?.ToString(),
            v => data.taxpayerType = (TaxpayerType)System.Enum.Parse(typeof(TaxpayerType), v));
    }

    // ---------- Page 3: Income Information ----------
    private void BuildPage3Rows(CaseData data)
    {
        ClearRows(page3Rows);

        AddEnumRow(page3Rows, page3FieldRowListRoot, "Income Source", "IncomeSource", data,
            () => data.incomeSource?.ToString(),
            v => data.incomeSource = (IncomeSource)System.Enum.Parse(typeof(IncomeSource), v));

        AddEnumRow(page3Rows, page3FieldRowListRoot, "Number of Employers", "EmployerCount", data,
            () => data.numberOfEmployers?.ToString(),
            v => data.numberOfEmployers = (EmployerCount)System.Enum.Parse(typeof(EmployerCount), v));

        AddEnumRow(page3Rows, page3FieldRowListRoot, "Business Registration", "BusinessRegistration", data,
            () => data.businessRegistration?.ToString(),
            v => data.businessRegistration = (BusinessRegistration)System.Enum.Parse(typeof(BusinessRegistration), v));

        AddEnumRow(page3Rows, page3FieldRowListRoot, "Tax Option", "TaxOption", data,
            () => data.taxOption?.ToString(),
            v => data.taxOption = (TaxOption)System.Enum.Parse(typeof(TaxOption), v));
    }

    // ---------- Page 4: Tax Computation ----------
    private void BuildPage4Rows(CaseData data)
    {

        ClearRows(page4Rows);

        AddNumberRow(page4Rows, page4FieldRowListRoot, "Gross Income", "GrossIncome", data,
            () => data.grossIncome, v => data.grossIncome = v);

        AddNumberRow(page4Rows, page4FieldRowListRoot, "Allowable Expenses", "AllowableExpenses", data,
            () => data.allowableExpenses, v => data.allowableExpenses = v);

        AddNumberRow(page4Rows, page4FieldRowListRoot, "Taxable Income", "TaxableIncome", data,
            () => data.taxableIncome, v => data.taxableIncome = v);

        AddNumberRow(page4Rows, page4FieldRowListRoot, "Tax Due", "TaxDue", data,
            () => data.taxDue, v => data.taxDue = v);

        AddNumberRow(page4Rows, page4FieldRowListRoot, "Tax Credits", "TaxCredits", data,
            () => data.taxWithheldOrCredits, v => data.taxWithheldOrCredits = v);

        AddNumberRow(page4Rows, page4FieldRowListRoot, "Final Tax Payable", "FinalTaxPayable", data,
            () => data.finalTaxPayable, v => data.finalTaxPayable = v);
    }

    // ---------- Page 5: Filing Information ----------
    private void BuildPage5Rows(CaseData data)
    {
        ClearRows(page5Rows);

        AddEnumRow(page5Rows, page5FieldRowListRoot, "Required Form", "RequiredForm", data,
            () => data.requiredForm?.ToString(),
            v => data.requiredForm = (RequiredForm)System.Enum.Parse(typeof(RequiredForm), v));
    }

    // ---------- Shared helpers ----------

    private void AddEnumRow(List<CaseFolderFieldRow> rowList, Transform parent, string label, string key,
        CaseData data, System.Func<string> getCurrent, System.Action<string> setValue)
    {
        bool known = data.folderTransferredFields.Contains(key);
        string current = getCurrent();

        var rowObj = Instantiate(caseFolderFieldRowPrefab, parent);
        rowObj.Initialize(label, current, known, DataValueType.Enum, key, v =>
        {
            setValue(v.ToString());
            data.folderTransferredFields.Add(key);
            Debug.Log($"[CaseData Updated] Field: {key} | Value: {v}");
        });
        rowList.Add(rowObj);
    }

    private void AddNumberRow(List<CaseFolderFieldRow> rowList, Transform parent, string label, string key,
        CaseData data, System.Func<float> getCurrent, System.Action<float> setValue)
    {
        bool known = data.folderTransferredFields.Contains(key);
        float current = getCurrent();

        var rowObj = Instantiate(caseFolderFieldRowPrefab, parent);
        rowObj.Initialize(label, current, known, DataValueType.Number, key, v =>
        {
            float f = System.Convert.ToSingle(v);
            setValue(f);
            data.folderTransferredFields.Add(key);
            Debug.Log($"[CaseData Updated] Field: {key} | Value: {f}");
        });
        rowList.Add(rowObj);
    }

    private void ClearRows(List<CaseFolderFieldRow> rowList)
    {
        foreach (var row in rowList) Destroy(row.gameObject);
        rowList.Clear();
    }

    private void AddSourceOnlyRow(List<CaseFolderFieldRow> rowList, Transform parent, string label, string value, DataValueType type, string key)
    {
        var rowObj = Instantiate(caseFolderFieldRowPrefab, parent);
        rowObj.InitializeSourceOnly(label, value, type, key);
        rowList.Add(rowObj);
    }
}