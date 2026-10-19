using UnityEngine;

/// <summary>
/// PURPOSE:
/// The records desk beside Rosa's stall (ticket T04). After Rosa hands over
/// her records, tapping it sits the player at the desk in the same desk view
/// the office desk uses (CameraController.EnterFirstPerson, "Exit Desk").
/// The first time raises MarketDeskOpened. Before the handover it only says
/// what to do first. Sitting down opens the income toolkit on the desk
/// (IncomeToolkit, ticket T05), which closes again when the player leaves.
/// </summary>
public sealed class MarketRecordsDesk : MonoBehaviour, IInteractable
{
    private Transform viewpoint;
    private HighlightEffect highlight;
    private IncomeToolkit toolkit;

    public Transform Viewpoint => viewpoint;
    public IncomeToolkit Toolkit => toolkit;

    public void Init(Transform deskViewpoint, IncomeToolkit incomeToolkit = null)
    {
        viewpoint = deskViewpoint;
        toolkit = incomeToolkit;
        highlight = HighlightEffect.For((Component)this);
    }

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        return review != null && review.HandoverDone ? "Sit at the records desk" : "Records desk";
    }

    public void OnInteract()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null) return;
        if (!review.HandoverDone)
        {
            Level2Say.Line(review.phase < ReviewPhase.MarketVisit
                ? "Check Rosa's folder at the office first."
                : "Talk to Aling Rosa first. She has the records.");
            return;
        }
        if (CameraController.Instance == null || viewpoint == null) return;

        CameraController.Instance.EnterFirstPerson(viewpoint, "Exit Desk");
        if (review.TryOpenMarketDesk()) GameplayEvents.RaiseMarketDeskOpened();
        if (toolkit != null) toolkit.RequestOpen();
        // The toolkit's own guide lines take over (tutorial and Mark the Money!).
        if (toolkit == null)
            Level2Say.Line("Rosa's records are clipped inside Supporting Documents in your folder.");
    }
}
