using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// The Computer's desktop/home screen — two buttons: Calculate Taxes
/// (launches existing ComputerUI from Milestone 10 unchanged), and Prepare
/// Tax Return (launches FormSelectionUI, only if the case is Ready For
/// Filing). This becomes the Computer's default view; ComputerUI is now a
/// "launched app" reached through this, not the immediate default.
///
/// RESPONSIBILITIES:
/// - Show/hide the home panel
/// - Enable/disable the Prepare Tax Return button based on FilingStatus
/// - Route to ComputerUI or FormSelectionUI, each of which has its own
///   "Back" affordance that returns here (see their Hide() calls below)
///
/// CONNECTS WITH:
/// - ComputerInteractable: calls Show() on interact (replaces the old
///   direct ComputerUI.Show() call)
/// - ComputerUI: launched by button, and its own Close/Back button now
///   calls ComputerHomeUI.Show() instead of fully closing the computer
/// - FormSelectionUI: launched by button
/// - CaseManager.Instance.CurrentCase: read to gate the Prepare button
/// </summary>
public class ComputerHomeUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject homePanelRoot;

    [Header("Buttons")]
    [SerializeField] private Button calculateTaxesButton;
    [SerializeField] private Button prepareTaxReturnButton;
    [SerializeField] private TextMeshProUGUI prepareButtonTooltip;

    [Header("Launched Apps")]
    [SerializeField] private ComputerUI computerUI;
    [SerializeField] private FormSelectionUI formSelectionUI;

    private void Awake()
    {
        Hide();
        calculateTaxesButton.onClick.AddListener(LaunchCalculateTaxes);
        prepareTaxReturnButton.onClick.AddListener(LaunchPrepareTaxReturn);
    }

    public void Show()
    {
        homePanelRoot.SetActive(true);

        CaseData data = CaseManager.Instance.CurrentCase;
        bool readyForFiling = data.caseAssessment == CaseAssessment.ReadyForFiling
            || data.filingStatus == FilingStatus.ReadyForFiling;

        prepareTaxReturnButton.interactable = readyForFiling;
        prepareButtonTooltip.text = readyForFiling ? "" : "Case is not ready for filing.";
    }

    public void Hide()
    {
        homePanelRoot.SetActive(false);
    }

    private void LaunchCalculateTaxes()
    {
        homePanelRoot.SetActive(false);
        computerUI.Show();
    }

    private void LaunchPrepareTaxReturn()
    {
        homePanelRoot.SetActive(false);
        formSelectionUI.Show();
    }
}