using UnityEngine;

/// <summary>
/// PURPOSE:
/// Level 2's initial (preliminary) stamp (ticket T03), under
/// Level2Gameplay/PreliminaryStamp. The existing desk stamps are reused: when
/// one lands on the case folder during Rosa's initial review, StampUI asks
/// this part first. A supported Not Ready (finding F1 on the summary, linked
/// to the referral note, reason "Not on summary") is recorded once as the
/// preliminary assessment; anything else gets Arthur's guided correction and
/// is not recorded, scored or rewarded.
///
/// Never touches the case assessment, filing status, game state, Auditor,
/// return printing or completion: those belong to the final assessment.
/// </summary>
public sealed class Level2PreliminaryStamp : MonoBehaviour
{
    public const string ChildName = "PreliminaryStamp";

    /// <summary>
    /// The desk's stamp set. Level 1's current flow no longer uses it, so the
    /// (copied) scene keeps it switched off; Level 2 switches it back on.
    /// Its StampSetInteractable, stamp folder and stamp transition are already
    /// wired in the scene; desk view taps it on its own layer.
    /// </summary>
    public const string StampSetPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/Stamps";

    private void OnEnable() => StampUI.StampOverride = HandleStamp;

    private void Start()
    {
        Transform stampSet = PlayerPlacement.Find(gameObject.scene, StampSetPath);
        if (stampSet == null)
        {
            Debug.LogWarning($"[Level2] Stamp set '{StampSetPath}' not found; the initial review cannot be stamped.");
            return;
        }
        if (!stampSet.gameObject.activeSelf) stampSet.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (StampUI.StampOverride == (System.Func<StampType, bool>)HandleStamp)
            StampUI.StampOverride = null;
    }

    private static void Guard(string what, System.Action action)
    {
        try
        {
            action();
        }
        catch (System.Exception error)
        {
            Debug.LogError($"[Level2] Initial stamp: {what} failed; the stamp is still recorded.");
            Debug.LogException(error);
        }
    }

    /// <summary>True when Level 2 handled the stamp (the Level 1 apply is skipped).</summary>
    private bool HandleStamp(StampType type)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null || review.phase >= ReviewPhase.FinalAssessment) return false;
        return Stamp(type, null, 0f) != PreliminaryReview.Outcome.NoRule;
    }

    /// <summary>
    /// The initial review stamp pressed on the front page (Level2DeskStamps).
    /// <paramref name="inkPoint"/> is where it landed on the paper (paper-local);
    /// the ink is drawn there only when the hold is recorded.
    /// </summary>
    public static PreliminaryReview.Outcome Stamp(StampType type, Vector2? inkPoint, float inkAngle)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (review == null || definition == null || !definition.HasReview) return PreliminaryReview.Outcome.NoRule;

        PreliminaryReview.Outcome outcome =
            PreliminaryReview.TryStamp(review, definition.review.preliminaryRule, type == StampType.NotReadyForFiling);
        if (outcome == PreliminaryReview.Outcome.Recorded && inkPoint.HasValue)
        {
            review.preliminary.hasInk = true;
            review.preliminary.inkX = inkPoint.Value.x;
            review.preliminary.inkY = inkPoint.Value.y;
            review.preliminary.inkAngle = inkAngle;
        }

        // The decision is made. Everything below only shows it; one listener's
        // error must never stop the ink from drawing or the stamp from returning.
        Guard("Arthur's line", () => Level2Say.Line(PreliminaryReview.Message(outcome), outcome == PreliminaryReview.Outcome.Recorded ? "wow" : "default"));
        if (outcome == PreliminaryReview.Outcome.Recorded)
        {
            Debug.Log($"[Level2] Initial review stamped Not Ready (finding {review.preliminary.findingId}, revision {review.preliminary.revision}). Review phase: MarketVisit.");
            Guard("folder redraw", () =>
            {
                foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include))
                    folder.RefreshCurrentPage();
            });
            Guard("PreliminaryHoldStamped listeners", GameplayEvents.RaisePreliminaryHoldStamped);
        }
        return outcome;
    }
}
