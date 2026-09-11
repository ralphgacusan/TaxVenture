using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// WORLD SPACE Case Folder used specifically for the 3D stamping workflow.
///
/// IMPORTANT:
/// - This is separate from the normal Screen Space CaseFolderUI.
/// - This panel is controlled by FloatingWindow.
/// - DO NOT use WorkspaceLayoutManager for this panel.
/// - Only Page 1 is used for the stamping workflow.
/// </summary>
public class StampCaseFolderUI : MonoBehaviour
{
    // =========================================================
    // PANEL
    // =========================================================

    [Header("World Space Panel")]
    [SerializeField] private GameObject folderPanelRoot;
    [SerializeField] private FloatingWindow floatingWindow;



    // =========================================================
    // PAPER
    // =========================================================

    [Header("The Paper")]
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;


    // =========================================================
    // STAMPING
    // =========================================================

    [Header("Stamping")]
    [SerializeField] private StampUI stampUI;
    [SerializeField] private Button paperClickTarget;


    // =========================================================
    // PAGE 1 ROWS
    // =========================================================

    [Header("Page 1 Fields")]
    [SerializeField] private Transform page1FieldRowListRoot;
    [SerializeField] private GameObject page1FieldRowsContainer;
    [SerializeField] private CaseFolderFieldRow caseFolderFieldRowPrefab;


    [Header("Stamp Transition")]
    [SerializeField] private StampTransitionController stampTransitionController;
    // =========================================================
    // STATE
    // =========================================================

    private List<CaseFolderPageContent> pages =
        new List<CaseFolderPageContent>();

    private readonly List<CaseFolderFieldRow> page1Rows =
        new List<CaseFolderFieldRow>();

    private const int Page1Index = 0;

    private int currentPageIndex = Page1Index;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // Paper click
        // -----------------------------------------------------

        if (paperClickTarget != null)
        {
            paperClickTarget.onClick.AddListener(OnPaperClicked);
        }


        // -----------------------------------------------------
        // Find FloatingWindow automatically if not assigned.
        // -----------------------------------------------------

        if (floatingWindow == null &&
            folderPanelRoot != null)
        {
            floatingWindow =
                folderPanelRoot.GetComponent<FloatingWindow>();
        }


        // -----------------------------------------------------
        // Validation
        // -----------------------------------------------------

        if (folderPanelRoot == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "folderPanelRoot is NULL. " +
                "Assign the World Space Case Folder Panel."
            );
        }

        if (floatingWindow == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "FloatingWindow is NULL. " +
                "Attach FloatingWindow to the World Space " +
                "Case Folder Panel or assign it in Inspector."
            );
        }

        if (page1FieldRowListRoot == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "page1FieldRowListRoot is NULL."
            );
        }

        if (caseFolderFieldRowPrefab == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "caseFolderFieldRowPrefab is NULL."
            );
        }


        Debug.Log(
            "[StampCaseFolderUI] Awake complete."
        );
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // FloatingWindow must initialize while the panel is
        // active. Close it only after Awake() has completed.
        // -----------------------------------------------------

        if (floatingWindow != null)
        {
            floatingWindow.CloseWindow();

            Debug.Log(
                $"[StampCaseFolderUI] Initial panel closed. " +
                $"activeSelf={folderPanelRoot.activeSelf} | " +
                $"activeInHierarchy={folderPanelRoot.activeInHierarchy}"
            );
        }
    }


    // =========================================================
    // SHOW FOR STAMPING
    // =========================================================

    public void ShowForStamping()
    {
        Debug.Log(
            "========== STAMP CASE FOLDER SHOW =========="
        );


        // -----------------------------------------------------
        // Validate CaseManager
        // -----------------------------------------------------

        if (CaseManager.Instance == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "CaseManager.Instance is NULL."
            );

            return;
        }


        // -----------------------------------------------------
        // Validate Current Case
        // -----------------------------------------------------

        if (CaseManager.Instance.CurrentCase == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "CurrentCase is NULL."
            );

            return;
        }


        // -----------------------------------------------------
        // Validate Panel
        // -----------------------------------------------------

        if (folderPanelRoot == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "folderPanelRoot is NULL."
            );

            return;
        }


        // -----------------------------------------------------
        // Find FloatingWindow if necessary
        // -----------------------------------------------------

        if (floatingWindow == null)
        {
            floatingWindow =
                folderPanelRoot.GetComponent<FloatingWindow>();
        }


        if (floatingWindow == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "FloatingWindow is NULL."
            );

            return;
        }


        // -----------------------------------------------------
        // Debug Before Open
        // -----------------------------------------------------

        Debug.Log(
            $"[StampCaseFolderUI] " +
            $"BEFORE OPEN | " +
            $"Panel={folderPanelRoot.name} | " +
            $"activeSelf={folderPanelRoot.activeSelf} | " +
            $"activeInHierarchy={folderPanelRoot.activeInHierarchy}"
        );


        // -----------------------------------------------------
        // Lock player controls
        // -----------------------------------------------------

        CameraController.Instance?.LockPlayerControls();


        // -----------------------------------------------------
        // Get CURRENT CaseData
        // -----------------------------------------------------

        CaseData data =
            CaseManager.Instance.CurrentCase;


        Debug.Log(
            $"[StampCaseFolderUI] " +
            $"Opening Case Folder with current data | " +
            $"AssessmentStamped={data.assessmentStamped} | " +
            $"Assessment={data.caseAssessment}"
        );


        // -----------------------------------------------------
        // Build Page 1
        // -----------------------------------------------------

        pages =
            BuildPages(data);

        currentPageIndex =
            Page1Index;


        // -----------------------------------------------------
        // Open FloatingWindow
        // -----------------------------------------------------

        floatingWindow.OpenWindow();


        // -----------------------------------------------------
        // Render Page 1
        // -----------------------------------------------------

        RenderPage();


        // -----------------------------------------------------
        // Debug After Open
        // -----------------------------------------------------

        Debug.Log(
            $"[StampCaseFolderUI] " +
            $"AFTER OPEN | " +
            $"Panel={folderPanelRoot.name} | " +
            $"activeSelf={folderPanelRoot.activeSelf} | " +
            $"activeInHierarchy={folderPanelRoot.activeInHierarchy}"
        );


        // -----------------------------------------------------
        // Canvas Diagnostics
        // -----------------------------------------------------

        Canvas canvas =
            folderPanelRoot.GetComponentInParent<Canvas>();


        if (canvas == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "NO PARENT CANVAS FOUND!"
            );
        }
        else
        {
            Debug.Log(
                $"[StampCaseFolderUI] " +
                $"Canvas={canvas.name} | " +
                $"Mode={canvas.renderMode} | " +
                $"Enabled={canvas.enabled} | " +
                $"SortingOrder={canvas.sortingOrder}"
            );


            if (canvas.renderMode ==
                RenderMode.WorldSpace)
            {
                Debug.Log(
                    $"[StampCaseFolderUI] " +
                    $"World Canvas Position={canvas.transform.position} | " +
                    $"Scale={canvas.transform.lossyScale} | " +
                    $"Rotation={canvas.transform.rotation.eulerAngles}"
                );


                if (canvas.worldCamera == null)
                {
                    Debug.LogWarning(
                        "[StampCaseFolderUI] " +
                        "World Space Canvas worldCamera is NULL. " +
                        "Assign Main Camera to the Canvas Event Camera."
                    );
                }
                else
                {
                    Debug.Log(
                        $"[StampCaseFolderUI] " +
                        $"World Canvas Camera={canvas.worldCamera.name}"
                    );
                }
            }
        }


        Debug.Log(
            "========== STAMP CASE FOLDER SHOW COMPLETE =========="
        );
    }


    // =========================================================
    // HIDE
    // =========================================================

    public void Hide()
    {
        Debug.Log("========== STAMP CASE FOLDER HIDE ==========");

        // Return stamps to their original desk positions
        if (stampTransitionController != null)
        {
            stampTransitionController.ReturnStampsToDesk();
        }
        else
        {
            Debug.LogWarning(
                "[StampCaseFolderUI] StampTransitionController is NULL."
            );
        }

        CameraController.Instance?.UnlockPlayerControls();

        if (floatingWindow != null)
            floatingWindow.CloseWindow();
        else if (folderPanelRoot != null)
            folderPanelRoot.SetActive(false);

        if (folderPanelRoot != null)
        {
            Debug.Log(
                $"[StampCaseFolderUI] HIDDEN | " +
                $"activeSelf={folderPanelRoot.activeSelf} | " +
                $"activeInHierarchy={folderPanelRoot.activeInHierarchy}"
            );
        }
    }

    // =========================================================
    // RENDER PAGE 1
    // =========================================================

    private void RenderPage()
    {
        if (pages == null ||
            pages.Count == 0)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "Cannot render Page 1. Pages list is empty."
            );

            return;
        }


        CaseFolderPageContent page =
            pages[Page1Index];


        // -----------------------------------------------------
        // Heading
        // -----------------------------------------------------

        if (headingText != null)
        {
            headingText.text =
                page.Heading;
        }


        // -----------------------------------------------------
        // Page Indicator
        // -----------------------------------------------------

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text =
                "Page 1 / 1";
        }


        // -----------------------------------------------------
        // Body
        // -----------------------------------------------------

        if (bodyText != null)
        {
            bodyText.gameObject.SetActive(false);
        }


        // -----------------------------------------------------
        // Page 1 Fields
        // -----------------------------------------------------

        if (page1FieldRowsContainer != null)
        {
            page1FieldRowsContainer.SetActive(true);
        }


        // -----------------------------------------------------
        // Get the LATEST CaseData
        // -----------------------------------------------------

        CaseData data =
            CaseManager.Instance.CurrentCase;


        // -----------------------------------------------------
        // Rebuild rows using latest data
        // -----------------------------------------------------

        BuildPage1Rows(data);


        // -----------------------------------------------------
        // Paper is clickable
        // -----------------------------------------------------

        if (paperClickTarget != null)
        {
            paperClickTarget.enabled = true;
        }
    }


    // =========================================================
    // PAPER CLICK
    // =========================================================

    private void OnPaperClicked()
    {
        Debug.Log(
            "[StampCaseFolderUI] " +
            "Page 1 paper clicked while stamping."
        );


        if (stampUI == null)
        {
            Debug.LogWarning(
                "[StampCaseFolderUI] " +
                "Paper clicked, but StampUI is NULL."
            );

            return;
        }


        // StampUI remains responsible for stamp application.
        // The 3D stamp drag/drop system determines when and
        // where the stamp is actually applied.
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh()
    {
        Debug.Log(
            "[StampCaseFolderUI] ===== REFRESH START ====="
        );


        // -----------------------------------------------------
        // Validate CaseManager
        // -----------------------------------------------------

        if (CaseManager.Instance == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "Cannot refresh. CaseManager.Instance is NULL."
            );

            return;
        }


        // -----------------------------------------------------
        // Validate Current Case
        // -----------------------------------------------------

        if (CaseManager.Instance.CurrentCase == null)
        {
            Debug.LogError(
                "[StampCaseFolderUI] " +
                "Cannot refresh. CurrentCase is NULL."
            );

            return;
        }


        // -----------------------------------------------------
        // Get CURRENT CaseData
        // -----------------------------------------------------

        CaseData data =
            CaseManager.Instance.CurrentCase;


        Debug.Log(
            $"[StampCaseFolderUI] " +
            $"CURRENT DATA | " +
            $"AssessmentStamped={data.assessmentStamped} | " +
            $"CaseAssessment={data.caseAssessment}"
        );


        // -----------------------------------------------------
        // Rebuild Page 1
        // -----------------------------------------------------

        pages =
            BuildPages(data);

        currentPageIndex =
            Page1Index;


        // -----------------------------------------------------
        // Re-render using updated data
        // -----------------------------------------------------

        RenderPage();


        // -----------------------------------------------------
        // Final state
        // -----------------------------------------------------

        Debug.Log(
            $"[StampCaseFolderUI] " +
            $"REFRESH COMPLETE | " +
            $"Panel activeSelf={folderPanelRoot.activeSelf} | " +
            $"activeInHierarchy={folderPanelRoot.activeInHierarchy}"
        );

        Debug.Log(
            "[StampCaseFolderUI] ===== REFRESH END ====="
        );
    }


    // =========================================================
    // BUILD PAGES
    // =========================================================

    private List<CaseFolderPageContent> BuildPages(
        CaseData data
    )
    {
        var list =
            new List<CaseFolderPageContent>();


        // -----------------------------------------------------
        // PAGE 1 - CASE OVERVIEW
        // -----------------------------------------------------

        string assessmentText;


        if (!data.assessmentStamped)
        {
            assessmentText =
                "Case Assessment: __________";
        }
        else
        {
            assessmentText =
                "Case Assessment: " +
                EnumDisplayFormatter.Format(
                    data.caseAssessment.ToString()
                );
        }


        list.Add(
            new CaseFolderPageContent(
                "Case Overview",
                $"{data.caseTitle}\n\n" +
                $"{data.caseSummary}\n\n" +
                assessmentText
            )
        );


        return list;
    }


    // =========================================================
    // BUILD PAGE 1 ROWS
    // =========================================================

    private void BuildPage1Rows(
        CaseData data
    )
    {
        ClearRows(page1Rows);


        // -----------------------------------------------------
        // Case Number
        // -----------------------------------------------------

        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "Case Number",
            data.caseNumber,
            DataValueType.Text,
            "CaseNumber"
        );


        // -----------------------------------------------------
        // Tax Year
        // -----------------------------------------------------

        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "Tax Year",
            data.taxYear,
            DataValueType.Text,
            "TaxYear"
        );


        // -----------------------------------------------------
        // Date Received
        // -----------------------------------------------------

        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "Date Received",
            data.dateReceived,
            DataValueType.Text,
            "DateReceived"
        );


        // -----------------------------------------------------
        // Assigned Consultant
        // -----------------------------------------------------

        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "Assigned Consultant",
            data.assignedConsultant,
            DataValueType.Text,
            "AssignedConsultant"
        );


        // -----------------------------------------------------
        // Case Title
        // -----------------------------------------------------

        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "Case Title",
            data.caseTitle,
            DataValueType.Text,
            "CaseTitle"
        );


        // -----------------------------------------------------
        // Case Assessment
        // -----------------------------------------------------

        string assessmentValue;

        if (data.assessmentStamped)
        {
            assessmentValue =
                EnumDisplayFormatter.Format(
                    data.caseAssessment.ToString()
                );
        }
        else
        {
            assessmentValue =
                "Not Yet Stamped";
        }


        Debug.Log(
            $"[StampCaseFolderUI] " +
            $"DISPLAYING CASE ASSESSMENT = '{assessmentValue}'"
        );


        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "Case Assessment",
            assessmentValue,
            DataValueType.Text,
            "CaseAssessment"
        );


        Debug.Log(
            $"[StampCaseFolderUI] " +
            $"Page 1 rows built. " +
            $"Case Assessment = {assessmentValue}"
        );
    }


    // =========================================================
    // CLEAR ROWS
    // =========================================================

    private void ClearRows(
        List<CaseFolderFieldRow> rowList
    )
    {
        foreach (var row in rowList)
        {
            if (row != null)
            {
                Destroy(
                    row.gameObject
                );
            }
        }


        rowList.Clear();
    }


    // =========================================================
    // SOURCE-ONLY ROW
    // =========================================================

    private void AddSourceOnlyRow(
        List<CaseFolderFieldRow> rowList,
        Transform parent,
        string label,
        string value,
        DataValueType type,
        string key
    )
    {
        if (caseFolderFieldRowPrefab == null)
        {
            Debug.LogError(
                $"[StampCaseFolderUI] " +
                $"Cannot create source row '{label}'. " +
                $"caseFolderFieldRowPrefab is NULL."
            );

            return;
        }


        if (parent == null)
        {
            Debug.LogError(
                $"[StampCaseFolderUI] " +
                $"Cannot create source row '{label}'. " +
                $"page1FieldRowListRoot is NULL."
            );

            return;
        }


        CaseFolderFieldRow rowObj =
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


        rowList.Add(
            rowObj
        );


        Debug.Log(
            $"[StampCaseFolderUI] " +
            $"Created row: {label} = '{value}'"
        );
    }
}