using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Optional "review a prepared packet" data of a case (Level 2 onward):
/// the readable pages of the packet with stable IDs, which pages the player
/// holds at the start, and the bounded schedule context. Lives in the case
/// JSON under "review". A case WITHOUT review.reviewId is an ordinary
/// Level 1-style case; nothing here changes how it loads or plays.
///
/// Pure data (no Unity calls) so CaseReviewValidator and the external
/// regression suite can check it. Answer keys for findings and income
/// annotations are NOT stored on these pages (they belong in separate
/// rule data added by later tickets), so player-facing text never carries
/// the answer.
/// </summary>
[Serializable]
public class CaseReviewDefinition
{
    /// <summary>Stable review id, e.g. "L2-ROSA-001". Empty = not a review case.</summary>
    public string reviewId;

    /// <summary>Period under review, e.g. "May 2026".</summary>
    public string focusPeriod;

    /// <summary>Player-facing name of what is reviewed, e.g. "May 2026 income schedule".</summary>
    public string scheduleLabel;

    /// <summary>Already verified income of the other periods (a supplied aggregate, not an answer).</summary>
    public float priorVerifiedAggregate;

    /// <summary>
    /// Authority status of the teaching content: "verified" or
    /// "educational-draft" (see Documentation/Level2SomethingDoesNotMatch/CONTENT.md).
    /// </summary>
    public string contentStatus;

    /// <summary>Review phase the case starts in (ReviewPhase name). Empty = OfficeWelcome.</summary>
    public string startPhase;

    /// <summary>Pages the player can read from the start (the rest arrive through play).</summary>
    public List<string> initialDocumentIds = new List<string>();

    public List<ReviewDocumentDefinition> documents = new List<ReviewDocumentDefinition>();

    /// <summary>Pages the client hands over in person (Level 2: Rosa's records at her stall), once.</summary>
    public List<string> handoverDocumentIds = new List<string>();

    /// <summary>What the client says at the stall (before, during and after the handover).</summary>
    public StallDefinition stall = new StallDefinition();

    /// <summary>Short concern reasons the player can choose for a finding (stable ids).</summary>
    public List<ReviewReasonDefinition> reasons = new List<ReviewReasonDefinition>();

    /// <summary>What a supported initial (preliminary) Not Ready hold needs. Answer data: never printed.</summary>
    public PreliminaryRuleDefinition preliminaryRule = new PreliminaryRuleDefinition();

    public bool IsReview => !string.IsNullOrWhiteSpace(reviewId);

    public ReviewReasonDefinition FindReason(string id)
    {
        if (string.IsNullOrEmpty(id) || reasons == null) return null;
        foreach (ReviewReasonDefinition reason in reasons)
            if (reason != null && reason.id == id) return reason;
        return null;
    }

    /// <summary>The review page that prints the case document holding this semantic key (null if none).</summary>
    public ReviewDocumentDefinition PageForKey(IEnumerable<DocumentDefinition> caseDocuments, string semanticKey, out string fieldLabel)
    {
        fieldLabel = null;
        if (caseDocuments == null || string.IsNullOrEmpty(semanticKey) || documents == null) return null;
        foreach (DocumentDefinition document in caseDocuments)
        {
            if (document == null || document.fields == null) continue;
            DocumentFieldDefinition field = document.fields.Find(f => f != null && f.semanticKey == semanticKey);
            if (field == null) continue;
            foreach (ReviewDocumentDefinition page in documents)
                if (page != null && page.documentName == document.documentName)
                {
                    fieldLabel = field.label;
                    return page;
                }
        }
        return null;
    }

    public ReviewDocumentDefinition FindDocument(string id)
    {
        if (string.IsNullOrEmpty(id) || documents == null) return null;
        foreach (ReviewDocumentDefinition document in documents)
            if (document != null && document.id == id) return document;
        return null;
    }
}

/// <summary>One readable page of the packet (cover, summary, note, record).</summary>
[Serializable]
public class ReviewDocumentDefinition
{
    /// <summary>Stable id: COVER, S1, N1, D1..D6. Display titles are never keys.</summary>
    public string id;

    /// <summary>cover | summary | note | record.</summary>
    public string kind;

    public string title;

    /// <summary>
    /// The case "documents" entry (documentName) this page opens in the
    /// case-document paper window. Must match one exactly (checked on load).
    /// </summary>
    public string documentName;

    public string issuer;
    public string reference;
    public string date;

    /// <summary>Small print at the foot of the page (e.g. who prepared it).</summary>
    public string footer;

    public List<ReviewLineDefinition> lines = new List<ReviewLineDefinition>();
}

/// <summary>One fixed, readable row of a page.</summary>
[Serializable]
public class ReviewLineDefinition
{
    /// <summary>Stable row id within the page (e.g. "S1-services").</summary>
    public string id;

    public string label;

    /// <summary>Exactly what the paper prints.</summary>
    public string value;

    /// <summary>Money amount of the row (0 when the row is not money).</summary>
    public float amount;

    /// <summary>
    /// info | income | subtotal | memo. On a summary page the income rows
    /// must add up to its single subtotal row; memo rows are listed apart
    /// and never counted.
    /// </summary>
    public string role;
}

/// <summary>One concern reason a finding can give (e.g. "Not on summary").</summary>
[Serializable]
public class ReviewReasonDefinition
{
    public string id;
    /// <summary>Short printed label (fits a folder row).</summary>
    public string label;
}

/// <summary>
/// The guided initial review: which finding, on which page, linked to which
/// record, with which reason makes a supported preliminary Not Ready hold.
/// </summary>
[Serializable]
public class PreliminaryRuleDefinition
{
    public string findingId;
    public string concernDocument;
    public string evidenceDocument;
    public string reasonId;

    public bool IsSet => !string.IsNullOrWhiteSpace(findingId);
}

/// <summary>The client's lines at the market stall (ticket T04).</summary>
[Serializable]
public class StallDefinition
{
    /// <summary>Name shown on the client's dialogue lines.</summary>
    public string speakerName;
    /// <summary>Before the initial review is stamped (no records yet).</summary>
    public List<DialogueLineDefinition> earlyLines = new List<DialogueLineDefinition>();
    /// <summary>The required conversation; the records are handed over when it completes.</summary>
    public List<DialogueLineDefinition> conversationLines = new List<DialogueLineDefinition>();
    /// <summary>After the handover.</summary>
    public List<DialogueLineDefinition> afterLines = new List<DialogueLineDefinition>();
}
