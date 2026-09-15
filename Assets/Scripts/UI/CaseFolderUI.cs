using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Controls the normal Case Folder UI.
///
/// Handles:
/// - 7-page case folder display
/// - Page navigation
/// - Case information
/// - Editable fields
/// - Supporting documents
///
/// This UI is opened by CaseFolderInteractable.
///
/// IMPORTANT:
/// This class contains NO stamping logic.
/// Stamping is handled separately by StampCaseFolderUI.
///
/// VISIBILITY:
/// The Case Folder is controlled by FloatingWindow.
/// WorkspaceLayoutManager is NOT used here.
/// </summary>
public class CaseFolderUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject folderPanelRoot;

    [Header("The Paper")]
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;

    [Header("Page 6 Document Links")]
    [SerializeField] private SupportingDocumentsPageLink supportingDocumentsPageLink;

    [Header("Page Edge Navigation")]
    [SerializeField] private PageEdgeTapZone leftEdgeZone;
    [SerializeField] private PageEdgeTapZone rightEdgeZone;
    [SerializeField] private GameObject leftEdgeIndicator;
    [SerializeField] private GameObject rightEdgeIndicator;

    [Header("Swipe to Close")]
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

    [Header("Additional Row Containers")]
    [SerializeField] private Transform page1FieldRowListRoot;
    [SerializeField] private GameObject page1FieldRowsContainer;

    [SerializeField] private Transform page2FixedFieldRowListRoot;
    [SerializeField] private GameObject page2FixedFieldRowsContainer;

    [SerializeField] private Transform page7FieldRowListRoot;
    [SerializeField] private GameObject page7FieldRowsContainer;

    private List<CaseFolderFieldRow> page1Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page2FixedRows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page2Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page3Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page4Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page5Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page7Rows = new List<CaseFolderFieldRow>();

    private const int Page1Index = 0;


    private List<CaseFolderPageContent> pages;
    private int currentPageIndex = 0;

    private bool hasOpenedBefore = false;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (leftEdgeZone != null)
            leftEdgeZone.OnTapped += PreviousPage;

        if (rightEdgeZone != null)
            rightEdgeZone.OnTapped += NextPage;

        if (swipeToClose != null)
            swipeToClose.OnSwipeClosed += Hide;


    }

    // ============================================================
    // SHOW
    // ============================================================

    /// <summary>
    /// Opens the normal Case Folder through FloatingWindow.
    /// Called by CaseFolderInteractable.
    /// </summary>
    public void Show()
    {
        Debug.Log("[CaseFolderUI] Show() was called");
        Debug.Log("========== CASE FOLDER SHOW ==========");

        if (CaseManager.Instance == null)
        {
            Debug.LogError(
                "[CaseFolderUI] CaseManager.Instance is NULL."
            );
            return;
        }

        if (CaseManager.Instance.CurrentCase == null)
        {
            Debug.LogError(
                "[CaseFolderUI] CurrentCase is NULL."
            );
            return;
        }

        if (folderPanelRoot == null)
        {
            Debug.LogError(
                "[CaseFolderUI] folderPanelRoot reference is NULL!"
            );
            return;
        }

        Debug.Log(
            $"[CaseFolderUI] Opening panel: {folderPanelRoot.name}"
        );

        Debug.Log(
            $"[CaseFolderUI] Before Open -> " +
            $"activeSelf={folderPanelRoot.activeSelf}, " +
            $"activeInHierarchy={folderPanelRoot.activeInHierarchy}"
        );

        // Lock player controls.
        CameraController.Instance?.LockPlayerControls();

        // Build case pages.
        pages = BuildPages(
            CaseManager.Instance.CurrentCase
        );

        // Reset to the first page.
        currentPageIndex = Page1Index;

        // Render the first page before showing the panel.
        RenderPage(currentPageIndex);

        // Directly show the UI panel.
        folderPanelRoot.SetActive(true);

        Debug.Log(
            $"[CaseFolderUI] After Open -> " +
            $"activeSelf={folderPanelRoot.activeSelf}, " +
            $"activeInHierarchy={folderPanelRoot.activeInHierarchy}"
        );

        // Change the game state only on the first opening.
        if (!hasOpenedBefore)
        {
            hasOpenedBefore = true;

            if (GameStateMachine.Instance != null &&
                GameStateMachine.Instance.CurrentState is ReceiveCaseState)
            {
                GameStateMachine.Instance.ChangeState(
                    new ReviewDocumentsState()
                );
            }
        }

        Debug.Log("========== CASE FOLDER SHOW COMPLETE ==========");
    }

    // ============================================================
    // HIDE
    // ============================================================

    /// <summary>
    /// Closes ONLY the normal Case Folder.
    /// FloatingWindow handles the actual visibility.
    /// </summary>
    public void Hide()
    {
        if (folderPanelRoot != null)
        {
            folderPanelRoot.SetActive(false);
        }

        CameraController.Instance?.UnlockPlayerControls();

        Debug.Log("[CaseFolderUI] Case Folder hidden.");
    }

    // ============================================================
    // PAGE NAVIGATION
    // ============================================================

    public void NextPage()
    {
        if (pages == null || pages.Count == 0)
            return;

        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;

            // Play paper/page-turn SFX.
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaperSFX();
            }

            RenderPage(currentPageIndex);
        }
    }

    public void PreviousPage()
    {
        if (pages == null || pages.Count == 0)
            return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;

            // Play paper/page-turn SFX.
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaperSFX();
            }

            RenderPage(currentPageIndex);
        }
    }

    // ============================================================
    // RENDER PAGE
    // ============================================================

    private void RenderPage(int index)
    {
        if (pages == null ||
            index < 0 ||
            index >= pages.Count)
        {
            return;
        }

        CaseFolderPageContent page = pages[index];

        if (headingText != null)
        {
            headingText.text = page.Heading;
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text =
                $"Page {index + 1} / {pages.Count}";
        }

        if (leftEdgeIndicator != null)
        {
            leftEdgeIndicator.SetActive(false);
        }

        if (rightEdgeIndicator != null)
        {
            rightEdgeIndicator.SetActive(false);
        }

        supportingDocumentsPageLink?.OnFolderPageChanged(index);

        CaseData data = CaseManager.Instance.CurrentCase;

        if (page1FieldRowsContainer != null)
        {
            page1FieldRowsContainer.SetActive(false);
        }

        if (page2FieldRowsContainer != null)
        {
            page2FieldRowsContainer.SetActive(false);
        }

        if (page2FixedFieldRowsContainer != null)
        {
            page2FixedFieldRowsContainer.SetActive(false);
        }

        if (page3FieldRowsContainer != null)
        {
            page3FieldRowsContainer.SetActive(false);
        }

        if (page4FieldRowsContainer != null)
        {
            page4FieldRowsContainer.SetActive(false);
        }

        if (page5FieldRowsContainer != null)
        {
            page5FieldRowsContainer.SetActive(false);
        }

        if (page7FieldRowsContainer != null)
        {
            page7FieldRowsContainer.SetActive(false);
        }

        if (bodyText != null)
        {
            bodyText.gameObject.SetActive(false);
        }

        if (index == Page1Index)
        {
            if (page1FieldRowsContainer != null)
            {
                page1FieldRowsContainer.SetActive(true);
            }

            BuildPage1Rows(data);
        }
    }

    // ============================================================
    // BUILD PAGES
    // ============================================================

    private List<CaseFolderPageContent> BuildPages(CaseData data)
    {
        var list = new List<CaseFolderPageContent>();

        list.Add(
            new CaseFolderPageContent(
                "Taxpayer Information",
                ""
            )
        );

        return list;
    }

    // ============================================================
    // PAGE ROWS
    // ============================================================

    private void BuildPage1Rows(CaseData data)
    {
        ClearRows(page1Rows);

        // Name
        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "Name",
            data.fullName,
            DataValueType.Text,
            "FullName"
        );

        // TIN ID
        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "TIN ID",
            data.tin,
            DataValueType.Text,
            "TIN"
        );

        // Taxpayer Classification
        // Uses data.residencyStatus internally,
        // but displays "Taxpayer Classification" in the UI.
        AddEnumRow(
            page1Rows,
            page1FieldRowListRoot,
            "Taxpayer Classification",
            "ResidencyStatus",
            data,
            () => data.residencyStatus?.ToString(),
            v => data.residencyStatus =
                (ResidencyStatus)System.Enum.Parse(
                    typeof(ResidencyStatus),
                    v
                )
        );

        // Income Sources
        AddEnumRow(
            page1Rows,
            page1FieldRowListRoot,
            "Income Sources",
            "IncomeSource",
            data,
            () => data.incomeSource?.ToString(),
            v => data.incomeSource =
                (IncomeSource)System.Enum.Parse(
                    typeof(IncomeSource),
                    v
                )
        );

        // Taxpayer Type
        AddEnumRow(
            page1Rows,
            page1FieldRowListRoot,
            "Taxpayer Type",
            "TaxpayerType",
            data,
            () => data.taxpayerType?.ToString(),
            v => data.taxpayerType =
                (TaxpayerType)System.Enum.Parse(
                    typeof(TaxpayerType),
                    v
                )
        );
    }

    private void BuildPage2FixedRows(CaseData data)
    {
        ClearRows(page2FixedRows);

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Full Name",
            data.fullName,
            DataValueType.Text,
            "FullName"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "TIN",
            data.tin,
            DataValueType.Text,
            "TIN"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Birthdate",
            data.birthdate,
            DataValueType.Text,
            "Birthdate"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Address",
            data.address,
            DataValueType.Text,
            "Address"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Contact Number",
            data.contactNumber,
            DataValueType.Text,
            "ContactNumber"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Civil Status",
            data.civilStatus.ToString(),
            DataValueType.Text,
            "CivilStatus"
        );

        if (data.civilStatus == CivilStatus.Married)
        {
            AddSourceOnlyRow(
                page2FixedRows,
                page2FixedFieldRowListRoot,
                "Spouse Name",
                data.spouseName,
                DataValueType.Text,
                "SpouseName"
            );

            AddSourceOnlyRow(
                page2FixedRows,
                page2FixedFieldRowListRoot,
                "Spouse TIN",
                data.spouseTin,
                DataValueType.Text,
                "SpouseTIN"
            );
        }

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Citizenship",
            data.citizenship,
            DataValueType.Text,
            "Citizenship"
        );
    }

    private void BuildPage2Rows(CaseData data)
    {
        ClearRows(page2Rows);

        AddEnumRow(
            page2Rows,
            page2FieldRowListRoot,
            "Residency Status",
            "ResidencyStatus",
            data,
            () => data.residencyStatus?.ToString(),
            v => data.residencyStatus =
                (ResidencyStatus)System.Enum.Parse(
                    typeof(ResidencyStatus),
                    v
                )
        );

        AddEnumRow(
            page2Rows,
            page2FieldRowListRoot,
            "Taxpayer Type",
            "TaxpayerType",
            data,
            () => data.taxpayerType?.ToString(),
            v => data.taxpayerType =
                (TaxpayerType)System.Enum.Parse(
                    typeof(TaxpayerType),
                    v
                )
        );
    }

    private void BuildPage3Rows(CaseData data)
    {
        ClearRows(page3Rows);

        AddEnumRow(
            page3Rows,
            page3FieldRowListRoot,
            "Income Source",
            "IncomeSource",
            data,
            () => data.incomeSource?.ToString(),
            v => data.incomeSource =
                (IncomeSource)System.Enum.Parse(
                    typeof(IncomeSource),
                    v
                )
        );

        AddEnumRow(
            page3Rows,
            page3FieldRowListRoot,
            "Number of Employers",
            "EmployerCount",
            data,
            () => data.numberOfEmployers?.ToString(),
            v => data.numberOfEmployers =
                (EmployerCount)System.Enum.Parse(
                    typeof(EmployerCount),
                    v
                )
        );

        AddEnumRow(
            page3Rows,
            page3FieldRowListRoot,
            "Business Registration",
            "BusinessRegistration",
            data,
            () => data.businessRegistration?.ToString(),
            v => data.businessRegistration =
                (BusinessRegistration)System.Enum.Parse(
                    typeof(BusinessRegistration),
                    v
                )
        );

        AddEnumRow(
            page3Rows,
            page3FieldRowListRoot,
            "Tax Option",
            "TaxOption",
            data,
            () => data.taxOption?.ToString(),
            v => data.taxOption =
                (TaxOption)System.Enum.Parse(
                    typeof(TaxOption),
                    v
                )
        );
    }

    private void BuildPage4Rows(CaseData data)
    {
        ClearRows(page4Rows);

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Gross Income",
            "GrossIncome",
            data,
            () => data.grossIncome,
            v => data.grossIncome = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Allowable Expenses",
            "AllowableExpenses",
            data,
            () => data.allowableExpenses,
            v => data.allowableExpenses = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Taxable Income",
            "TaxableIncome",
            data,
            () => data.taxableIncome,
            v => data.taxableIncome = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Tax Due",
            "TaxDue",
            data,
            () => data.taxDue,
            v => data.taxDue = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Tax Credits",
            "TaxCredits",
            data,
            () => data.taxWithheldOrCredits,
            v => data.taxWithheldOrCredits = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Final Tax Payable",
            "FinalTaxPayable",
            data,
            () => data.finalTaxPayable,
            v => data.finalTaxPayable = v
        );
    }

    private void BuildPage5Rows(CaseData data)
    {
        ClearRows(page5Rows);

        AddEnumRow(
            page5Rows,
            page5FieldRowListRoot,
            "Required Form",
            "RequiredForm",
            data,
            () => data.requiredForm?.ToString(),
            v => data.requiredForm =
                (RequiredForm)System.Enum.Parse(
                    typeof(RequiredForm),
                    v
                )
        );
    }

    private void BuildPage7Rows(CaseData data)
    {
        ClearRows(page7Rows);

        AddSourceOnlyRow(
            page7Rows,
            page7FieldRowListRoot,
            "Residency Status (on file)",
            data.residencyStatus.HasValue
                ? data.residencyStatus.ToString()
                : "?",
            DataValueType.Text,
            "FindingsResidencyStatus"
        );

        AddSourceOnlyRow(
            page7Rows,
            page7FieldRowListRoot,
            "Taxpayer Type (on file)",
            data.taxpayerType.HasValue
                ? data.taxpayerType.ToString()
                : "?",
            DataValueType.Text,
            "FindingsTaxpayerType"
        );

        for (int i = 0;
             i < data.potentialIssuesIdentified.Count;
             i++)
        {
            AddSourceOnlyRow(
                page7Rows,
                page7FieldRowListRoot,
                $"Issue {i + 1}",
                data.potentialIssuesIdentified[i],
                DataValueType.Text,
                $"Issue{i}"
            );
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private void AddEnumRow(
        List<CaseFolderFieldRow> rowList,
        Transform parent,
        string label,
        string key,
        CaseData data,
        System.Func<string> getCurrent,
        System.Action<string> setValue)
    {
        bool known =
            data.folderTransferredFields.Contains(key);

        string current =
            getCurrent();

        var rowObj =
            Instantiate(
                caseFolderFieldRowPrefab,
                parent
            );

        rowObj.Initialize(
            label,
            current,
            known,
            DataValueType.Enum,
            key,
            v =>
            {
                setValue(v.ToString());

                data.folderTransferredFields.Add(key);

                Debug.Log(
                    $"[CaseData Updated] " +
                    $"Field: {key} | Value: {v}"
                );
            }
        );

        rowList.Add(rowObj);
    }

    private void AddNumberRow(
        List<CaseFolderFieldRow> rowList,
        Transform parent,
        string label,
        string key,
        CaseData data,
        System.Func<float> getCurrent,
        System.Action<float> setValue)
    {
        bool known =
            data.folderTransferredFields.Contains(key);

        float current =
            getCurrent();

        var rowObj =
            Instantiate(
                caseFolderFieldRowPrefab,
                parent
            );

        rowObj.Initialize(
            label,
            current,
            known,
            DataValueType.Number,
            key,
            v =>
            {
                float value =
                    System.Convert.ToSingle(v);

                setValue(value);

                data.folderTransferredFields.Add(key);

                Debug.Log(
                    $"[CaseData Updated] " +
                    $"Field: {key} | Value: {value}"
                );
            }
        );

        rowList.Add(rowObj);
    }

    private void AddSourceOnlyRow(
        List<CaseFolderFieldRow> rowList,
        Transform parent,
        string label,
        string value,
        DataValueType type,
        string key)
    {
        var rowObj =
            Instantiate(
                caseFolderFieldRowPrefab,
                parent
            );

        rowObj.InitializeSourceOnly(
            label,
            value,
            type,
            key
        );

        rowList.Add(rowObj);
    }

    private void ClearRows(
        List<CaseFolderFieldRow> rowList)
    {
        foreach (var row in rowList)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        rowList.Clear();
    }
}