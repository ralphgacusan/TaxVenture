using UnityEngine;

/// <summary>
/// PURPOSE:
/// The records desk beside Rosa's stall (ticket T04). After Rosa hands over
/// her records, tapping it sits the player at the desk in the same desk view
/// the office desk uses (CameraController.EnterFirstPerson, "Exit Desk").
/// The first time raises MarketDeskOpened. Before the handover it only says
/// what to do first. The hands-on toolkit on this desk is ticket T05.
/// </summary>
public sealed class MarketRecordsDesk : MonoBehaviour, IInteractable
{
    private Transform viewpoint;
    private HighlightEffect highlight;

    public Transform Viewpoint => viewpoint;

    public void Init(Transform deskViewpoint)
    {
        viewpoint = deskViewpoint;
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
        Level2Say.Line("Here are Rosa's six records. You can read each one in your folder, after the Findings page.");
    }
}
