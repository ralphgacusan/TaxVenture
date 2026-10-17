using System;
using System.Collections.Generic;

/// <summary>
/// Where a review case (Level 2) is. Values are saved later, so never
/// renumber: append new phases at the end.
/// </summary>
public enum ReviewPhase
{
    OfficeWelcome = 0,
    RetrieveFolder = 1,
    PreliminaryReview = 2,
    PreliminaryStamp = 3,
    MarketVisit = 4,
    Tutorial = 5,
    Challenge = 6,
    Feedback = 7,
    OfficeReconciliation = 8,
    FinalAssessment = 9,
    Audit = 10,
    Correction = 11,
    Completed = 12
}

/// <summary>
/// PURPOSE:
/// The player's progress through a review case, owned by the current
/// CaseData (CaseData.review; null for ordinary cases). Pure C#, so it can
/// be tested outside Unity. It only records what has happened; the Level 2
/// controller decides when a gameplay action moves it on.
///
/// RULES:
/// - Phases only move forward (a hold or correction never rewinds work).
/// - Acquired documents are a set: collecting the same page twice changes
///   nothing and reports false.
/// - Nothing here is reset by an audit retry (CaseData.ResetForRetry leaves
///   it alone): mistakes keep documents and work.
/// </summary>
[Serializable]
public class CaseReviewState
{
    public string reviewId;
    public ReviewPhase phase = ReviewPhase.OfficeWelcome;
    public List<string> acquiredDocumentIds = new List<string>();

    /// <summary>True once the retrieved folder was put down on the desk (from the HUD folder icon).</summary>
    public bool folderPlacedOnDesk;

    /// <summary>The player's findings (their conclusions; never the answer key).</summary>
    public List<ReviewFindingState> findings = new List<ReviewFindingState>();

    /// <summary>Counts meaningful changes to the reviewed work (findings now; summary/evidence later).</summary>
    public int packetRevision;

    /// <summary>The initial (preliminary) assessment, once stamped. Separate from any final assessment.</summary>
    public PreliminaryAssessmentState preliminary;

    /// <summary>True once the player has sat at the market records desk.</summary>
    public bool marketDeskOpened;

    /// <summary>Raised when the phase changed (from, to). Not serialized.</summary>
    [field: NonSerialized] public event Action<ReviewPhase, ReviewPhase> PhaseChanged;

    /// <summary>A fresh state for a review case, or null for an ordinary case.</summary>
    public static CaseReviewState Seed(CaseReviewDefinition definition)
    {
        if (definition == null || !definition.IsReview) return null;

        var state = new CaseReviewState { reviewId = definition.reviewId.Trim() };
        if (!string.IsNullOrWhiteSpace(definition.startPhase) &&
            Enum.TryParse(definition.startPhase.Trim(), out ReviewPhase start) &&
            Enum.IsDefined(typeof(ReviewPhase), start))
            state.phase = start;

        if (definition.initialDocumentIds != null)
            foreach (string id in definition.initialDocumentIds)
                if (definition.FindDocument(id) != null) state.Acquire(id);
        return state;
    }

    /// <summary>True once the player has taken the case folder out of the cabinet.</summary>
    public bool FolderRetrieved => phase >= ReviewPhase.PreliminaryReview;

    /// <summary>
    /// The cabinet retrieval: only while the review waits for it
    /// (RetrieveFolder), and only once. Moves on to PreliminaryReview.
    /// Never archives, audits, rewards or changes the case index.
    /// </summary>
    public bool TryRetrieveFolder() =>
        phase == ReviewPhase.RetrieveFolder && TryAdvance(ReviewPhase.PreliminaryReview);

    /// <summary>The folder is in the HUD folder icon and has not been put on the desk yet.</summary>
    public bool AwaitingDeskPlacement => FolderRetrieved && !folderPlacedOnDesk;

    /// <summary>Records the first time the retrieved folder is put on the desk. False before retrieval or after.</summary>
    public bool TryPlaceFolderOnDesk()
    {
        if (!AwaitingDeskPlacement) return false;
        folderPlacedOnDesk = true;
        return true;
    }

    public ReviewFindingState FindFinding(string id)
    {
        if (string.IsNullOrEmpty(id) || findings == null) return null;
        foreach (ReviewFindingState finding in findings)
            if (finding != null && finding.id == id) return finding;
        return null;
    }

    /// <summary>True while findings may still be edited for the initial review.</summary>
    public bool CanEditPreliminaryFinding =>
        FolderRetrieved && preliminary == null && phase <= ReviewPhase.PreliminaryStamp;

    /// <summary>
    /// Sets one part of a finding ("concern", "evidence" or "reason"). A real
    /// change counts as a packet revision. When the finding becomes complete
    /// during the initial review, the review moves on to PreliminaryStamp.
    /// Returns true when something changed.
    /// </summary>
    public bool SetFindingPart(string findingId, string part, string documentId, string key, string label)
    {
        if (!CanEditPreliminaryFinding || string.IsNullOrWhiteSpace(findingId)) return false;
        if (findings == null) findings = new List<ReviewFindingState>();
        ReviewFindingState finding = FindFinding(findingId);
        if (finding == null)
        {
            finding = new ReviewFindingState { id = findingId };
            findings.Add(finding);
        }

        bool changed;
        switch (part)
        {
            case ReviewFindingState.Concern:
                changed = finding.concernDocument != documentId || finding.concernKey != key;
                finding.concernDocument = documentId; finding.concernKey = key; finding.concernLabel = label;
                break;
            case ReviewFindingState.Evidence:
                changed = finding.evidenceDocument != documentId || finding.evidenceKey != key;
                finding.evidenceDocument = documentId; finding.evidenceKey = key; finding.evidenceLabel = label;
                break;
            case ReviewFindingState.Reason:
                changed = finding.reasonId != key;
                finding.reasonId = key; finding.reasonLabel = label;
                break;
            default:
                return false;
        }
        if (!changed) return false;

        packetRevision++;
        if (finding.IsComplete && phase == ReviewPhase.PreliminaryReview) TryAdvance(ReviewPhase.PreliminaryStamp);
        return true;
    }

    /// <summary>True once the client handed over their records (review moved on to the Tutorial).</summary>
    public bool HandoverDone => phase >= ReviewPhase.Tutorial;

    /// <summary>
    /// The stall handover: only during MarketVisit, once. Adds every handover
    /// page to what the player holds and moves on to Tutorial. Returns true
    /// only the first time.
    /// </summary>
    public bool TryCollectHandover(IEnumerable<string> documentIds)
    {
        if (phase != ReviewPhase.MarketVisit) return false;
        if (documentIds != null)
            foreach (string id in documentIds) Acquire(id);
        return TryAdvance(ReviewPhase.Tutorial);
    }

    /// <summary>First time sitting at the market records desk after the handover. True once.</summary>
    public bool TryOpenMarketDesk()
    {
        if (!HandoverDone || marketDeskOpened) return false;
        marketDeskOpened = true;
        return true;
    }

    public bool HasDocument(string id) =>
        !string.IsNullOrEmpty(id) && acquiredDocumentIds != null && acquiredDocumentIds.Contains(id);

    /// <summary>Adds a page to what the player holds. False when already held or empty.</summary>
    public bool Acquire(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (acquiredDocumentIds == null) acquiredDocumentIds = new List<string>();
        if (acquiredDocumentIds.Contains(id)) return false;
        acquiredDocumentIds.Add(id);
        return true;
    }

    /// <summary>Moves to a later phase. False (and no change) for the same or an earlier phase.</summary>
    public bool TryAdvance(ReviewPhase to)
    {
        if (to <= phase || !Enum.IsDefined(typeof(ReviewPhase), to)) return false;
        ReviewPhase from = phase;
        phase = to;
        PhaseChanged?.Invoke(from, to);
        return true;
    }
}

/// <summary>
/// One finding as the player recorded it: the concern (a summary line), the
/// related record and a reason. Status words describe the player's work
/// ("Needs checking", later "Corrected" / "Explained"); they never certify truth.
/// </summary>
[Serializable]
public class ReviewFindingState
{
    public const string Concern = "concern";
    public const string Evidence = "evidence";
    public const string Reason = "reason";
    public const string NeedsChecking = "Needs checking";

    public string id;
    public string concernDocument;
    public string concernKey;
    public string concernLabel;
    public string evidenceDocument;
    public string evidenceKey;
    public string evidenceLabel;
    public string reasonId;
    public string reasonLabel;
    public string status = NeedsChecking;

    public bool IsComplete =>
        !string.IsNullOrEmpty(concernKey) && !string.IsNullOrEmpty(evidenceKey) && !string.IsNullOrEmpty(reasonId);
}

/// <summary>The stamped initial assessment: verdict, its reason and finding, and the packet revision it covers.</summary>
[Serializable]
public class PreliminaryAssessmentState
{
    public const string NotReady = "NotReady";

    public string verdict;
    public string findingId;
    public string reasonId;
    public int revision;

    /// <summary>Where the ink landed on the front page (paper-local), and its tilt.</summary>
    public bool hasInk;
    public float inkX;
    public float inkY;
    public float inkAngle;
}
