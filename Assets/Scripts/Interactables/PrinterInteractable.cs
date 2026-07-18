using UnityEngine;

/// <summary>
/// PURPOSE:
/// The Printer object in the scene. Keeps its existing HighlightEffect for
/// normal look-at focus. Additionally shows GREEN once a print job has
/// completed, signaling "ready to collect" — and only THEN allows the
/// player to actually pick up the printed form.
///
/// RESPONSIBILITIES:
/// - Track print-ready state
/// - Swap to a distinct "ready" color, overriding normal highlight cues
/// - On interact (only when ready): mark the form picked up, show it in
///   FirstPersonHands' left hand, exit first-person automatically since
///   picking up paper happens standing at the printer (not seated)
///
/// CONNECTS WITH:
/// - HighlightEffect (kept, unchanged) — normal focus highlight
/// - PrintJobController: calls SetPrintReady(true) when printing finishes
/// - FirstPersonHands: told to show the held document
/// - CaseData: writes isCarryingPrintedReturn
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class PrinterInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Renderer printerRenderer;
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private GameObject printedPaperVisual; // small paper mesh sitting on printer once ready
    [SerializeField] private FirstPersonHands firstPersonHands;

    private HighlightEffect highlight;
    private bool isReady = false;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
        printedPaperVisual.SetActive(false);
    }

    public void SetPrintReady(bool ready)
    {
        isReady = ready;
        printedPaperVisual.SetActive(ready);
        if (printerRenderer != null)
        {
            printerRenderer.material.color = ready ? readyColor : Color.white;
        }
    }

    public void OnFocus()
    {
        if (!isReady) highlight.Highlight();
        // While ready, we skip the normal highlight material swap so the
        // green "ready" color stays clearly visible rather than being
        // overridden by Mat_Highlight.
    }

    public void OnUnfocus()
    {
        if (!isReady) highlight.Unhighlight();
    }

    public void OnInteract()
    {
        if (!isReady) return;

        CaseData data = CaseManager.Instance.CurrentCase;
        data.isCarryingPrintedReturn = true;

        firstPersonHands.ShowCarriedDocument();

        SetPrintReady(false); // paper collected, printer resets visually
    }

    public string GetPromptText() => isReady ? "Click to collect the printed Tax Return" : "Printer";
}