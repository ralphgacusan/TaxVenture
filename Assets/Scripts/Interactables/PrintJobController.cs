using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Simulates the printing process after the player presses "Print Tax
/// Return." Runs a timer (printDuration), shows "Printing..." feedback,
/// then flags the Printer as ready (green highlight) and CaseData as
/// having a printed form waiting.
///
/// RESPONSIBILITIES:
/// - Run the print timer coroutine
/// - Show "Printing..." status text while running
/// - On completion: set CaseData.hasPrintedReturn = true, generate and
///   store Submission Date, tell PrinterInteractable to switch to "ready" state
///
/// CONNECTS WITH:
/// - BirFormEncodingUI: calls BeginPrint()
/// - PrinterInteractable: told when printing completes
/// - CaseManager.Instance.CurrentCase: writes hasPrintedReturn, submissionDate, remarks
/// </summary>
public class PrintJobController : MonoBehaviour
{
    [SerializeField] private float printDuration = 5f;
    [SerializeField] private TextMeshProUGUI printingStatusText;
    [SerializeField] private GameObject printingStatusPanel;
    [SerializeField] private PrinterInteractable printerInteractable;

    public void BeginPrint(EncodedFormData formData)
    {
        StartCoroutine(PrintRoutine(formData));
    }

    private IEnumerator PrintRoutine(EncodedFormData formData)
    {
        printingStatusPanel.SetActive(true);
        printingStatusText.text = "Printing...";

        yield return new WaitForSeconds(printDuration);

        CaseData data = CaseManager.Instance.CurrentCase;
        data.hasPrintedReturn = true;
        data.submissionDate = "April 15, 2026";
        data.remarks = $"BIR Form {formData.selectedForm} submitted for filing.";

        printingStatusText.text = "Print complete. Collect it from the Printer.";
        yield return new WaitForSeconds(1.5f);
        printingStatusPanel.SetActive(false);

        printerInteractable.SetPrintReady(true);
    }
}