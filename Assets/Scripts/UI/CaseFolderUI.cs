using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

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

    [Header("Nav Buttons (sit left/right of the paper)")]
    [SerializeField] private GameObject previousButton;
    [SerializeField] private GameObject nextButton;

    private List<CaseFolderPageContent> pages;
    private int currentPageIndex = 0;
    private bool hasOpenedBefore = false;

    private void Awake()
    {
        Hide();
    }

    /// <summary>
    /// Called by CaseFolderInteractable.OnInteract(). Builds fresh page
    /// content from the current CaseData every time it's opened, so any
    /// changes made elsewhere (Interview answers, computed tax values)
    /// always show up-to-date.
    /// </summary>
    public void Show()
    {
        pages = BuildPages(CaseManager.Instance.CurrentCase);
        folderPanelRoot.SetActive(true);
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

    public void Hide()
    {
        folderPanelRoot.SetActive(false);
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
    private void RenderPage(int index)
    {
        CaseFolderPageContent page = pages[index];
        headingText.text = page.Heading;
        bodyText.text = page.Body;
        pageIndicatorText.text = $"Page {index + 1} / {pages.Count}";

        // Disable Previous/Next at the ends instead of letting them wrap or error.
        previousButton.SetActive(index > 0);
        nextButton.SetActive(index < pages.Count - 1);
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
        list.Add(new CaseFolderPageContent(
            "Case Overview",
            $"{data.caseTitle}\n\n{data.caseSummary}\n\nCase Assessment: {FormatEnum(data.caseAssessment.ToString())}"
        ));

        // Page 2 - Taxpayer Information
        StringBuilder p2 = new StringBuilder();
        p2.AppendLine($"Full Name: {data.fullName}");
        p2.AppendLine($"TIN: {data.tin}");
        p2.AppendLine($"Birthdate: {data.birthdate}");
        p2.AppendLine($"Address: {data.address}");
        p2.AppendLine($"Contact Number: {data.contactNumber}");
        p2.AppendLine($"Civil Status: {data.civilStatus}");
        if (data.civilStatus == CivilStatus.Married)
        {
            p2.AppendLine($"Spouse Name: {data.spouseName}");
            p2.AppendLine($"Spouse TIN: {data.spouseTin}");
        }
        p2.AppendLine($"Citizenship: {data.citizenship}");
        p2.AppendLine($"Residency Status: {(data.residencyStatus.HasValue ? data.residencyStatus.ToString() : "?")}");
        p2.Append($"Taxpayer Type: {(data.taxpayerType.HasValue ? data.taxpayerType.ToString() : "?")}");
        list.Add(new CaseFolderPageContent("Taxpayer Information", p2.ToString()));

        // Page 3 - Income Information
        StringBuilder p3 = new StringBuilder();
        p3.AppendLine($"Income Sources: {(data.incomeSource.HasValue ? data.incomeSource.ToString() : "?")}");
        p3.AppendLine($"Number of Employers: {(data.numberOfEmployers.HasValue ? data.numberOfEmployers.ToString() : "?")}");
        p3.AppendLine($"Business Registration: {(data.businessRegistration.HasValue ? data.businessRegistration.ToString() : "?")}");
        p3.Append($"Tax Option: {(data.taxOption.HasValue ? data.taxOption.ToString() : "?")}");
        list.Add(new CaseFolderPageContent("Income Information", p3.ToString()));

        // Page 4 - Tax Computation Information
        StringBuilder p4 = new StringBuilder();
        p4.AppendLine($"Gross Income: \u20b1{data.grossIncome:N0}");
        p4.AppendLine($"Allowable Expenses: \u20b1{data.allowableExpenses:N0}");
        p4.AppendLine($"Taxable Income: \u20b1{data.taxableIncome:N0}");
        p4.AppendLine($"Tax Due: \u20b1{data.taxDue:N0}");
        p4.AppendLine($"Tax Withheld / Credits: \u20b1{data.taxWithheldOrCredits:N0}");
        p4.AppendLine($"Final Tax Payable: \u20b1{data.finalTaxPayable:N0}");
        p4.Append($"Status: {FormatEnum(data.computationStatus.ToString())}");
        list.Add(new CaseFolderPageContent("Tax Computation Information", p4.ToString()));

        // Page 5 - Filing Information
        StringBuilder p5 = new StringBuilder();
        p5.AppendLine($"Required Form: {(data.requiredForm.HasValue ? data.requiredForm.ToString() : "?")}");
        p5.AppendLine($"Filing Status: {FormatEnum(data.filingStatus.ToString())}");
        p5.AppendLine($"Submission Date: {(string.IsNullOrEmpty(data.submissionDate) ? "-" : data.submissionDate)}");
        p5.Append($"Remarks: {(string.IsNullOrEmpty(data.remarks) ? "-" : data.remarks)}");
        list.Add(new CaseFolderPageContent("Filing Information", p5.ToString()));

        // Page 6 - Supporting Documents
        StringBuilder p6 = new StringBuilder();
        foreach (var doc in data.supportingDocuments)
        {
            p6.AppendLine($"{(doc.isReviewed ? "[x]" : "[ ]")} {doc.documentName}");
        }
        list.Add(new CaseFolderPageContent("Supporting Documents", p6.ToString()));

        // Page 7 - Consultant Findings
        StringBuilder p7 = new StringBuilder();
        p7.AppendLine($"Residency Status: {(data.residencyStatus.HasValue ? data.residencyStatus.ToString() : "?")}");
        p7.AppendLine($"Taxpayer Type: {(data.taxpayerType.HasValue ? data.taxpayerType.ToString() : "?")}");
        p7.AppendLine();
        p7.AppendLine("Potential Issues Identified:");
        foreach (var issue in data.potentialIssuesIdentified)
        {
            p7.AppendLine($"- {issue}");
        }
        list.Add(new CaseFolderPageContent("Consultant Findings", p7.ToString()));

        return list;
    }

    private string FormatEnum(string enumName)
    {
        return System.Text.RegularExpressions.Regex.Replace(enumName, "(\\B[A-Z])", " $1");
    }
}