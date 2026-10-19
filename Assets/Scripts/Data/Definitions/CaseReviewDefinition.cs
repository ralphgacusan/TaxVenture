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

    /// <summary>The income toolkit (T05 tutorial; T06 challenge entries). Optional.</summary>
    public AnnotationDefinition annotation = new AnnotationDefinition();

    /// <summary>
    /// Answer key of the scored annotation entries (T06), one per entry, with
    /// rule hints for corrections. Answer data: never printed on a record.
    /// </summary>
    public List<AnnotationKeyDefinition> annotationKey = new List<AnnotationKeyDefinition>();

    /// <summary>Office review (T07): conclusions the book offers (generic, e.g. "Explained: no change").</summary>
    public List<ReviewConclusionDefinition> conclusions = new List<ReviewConclusionDefinition>();

    /// <summary>Record lines that can go into the player-reviewed schedule, with the money each one represents.</summary>
    public List<ScheduleSourceDefinition> scheduleSources = new List<ScheduleSourceDefinition>();

    /// <summary>How many lines the reviewed schedule page has.</summary>
    public int scheduleSlots = 4;

    /// <summary>Optional printed caption per schedule line (e.g. the summary's listed lines, then "Missing entry"). Empty = "May line N".</summary>
    public List<string> scheduleSlotLabels = new List<string>();

    /// <summary>The configured office findings the player records. Player-facing.</summary>
    public List<OfficeFindingDefinition> officeFindings = new List<OfficeFindingDefinition>();

    /// <summary>What a supported office review needs. Answer data: never printed.</summary>
    public OfficeReviewRuleDefinition officeRule = new OfficeReviewRuleDefinition();

    /// <summary>Final assessment reasons (T08), offered by the book. verdict/names say what each one claims.</summary>
    public List<AssessmentReasonDefinition> assessmentReasons = new List<AssessmentReasonDefinition>();

    /// <summary>What the Auditor and Rosa say for each audit outcome and root cause (T09).</summary>
    public AuditLinesDefinition audit = new AuditLinesDefinition();

    /// <summary>Use a generated work summary and structural-only final stamp, without a separate assessment reason.</summary>
    public bool simplifiedAssessment;

    /// <summary>The review cutoff (T10).</summary>
    public ReviewClockDefinition clock = new ReviewClockDefinition();

    public AssessmentReasonDefinition FindAssessmentReason(string id)
    {
        if (assessmentReasons == null || string.IsNullOrEmpty(id)) return null;
        foreach (AssessmentReasonDefinition r in assessmentReasons)
            if (r != null && r.id == id) return r;
        return null;
    }

    public ReviewConclusionDefinition FindConclusion(string id)
    {
        if (conclusions == null || string.IsNullOrEmpty(id)) return null;
        foreach (ReviewConclusionDefinition c in conclusions)
            if (c != null && c.id == id) return c;
        return null;
    }

    public ScheduleSourceDefinition FindScheduleSource(string semanticKey)
    {
        if (scheduleSources == null || string.IsNullOrEmpty(semanticKey)) return null;
        foreach (ScheduleSourceDefinition source in scheduleSources)
            if (source != null && source.semanticKey == semanticKey) return source;
        return null;
    }

    /// <summary>The schedule source taken from an annotated paper's highlighted line (null when none).</summary>
    public ScheduleSourceDefinition FindScheduleSourceForEntry(string entryId)
    {
        if (scheduleSources == null || string.IsNullOrEmpty(entryId)) return null;
        foreach (ScheduleSourceDefinition source in scheduleSources)
            if (source != null && source.entryId == entryId) return source;
        return null;
    }

    /// <summary>Printed caption of a schedule line.</summary>
    public string ScheduleSlotLabel(int index) =>
        scheduleSlotLabels != null && index >= 0 && index < scheduleSlotLabels.Count && !string.IsNullOrWhiteSpace(scheduleSlotLabels[index])
            ? scheduleSlotLabels[index]
            : "May line " + (index + 1);

    public OfficeFindingDefinition FindOfficeFinding(string id)
    {
        if (officeFindings == null || string.IsNullOrEmpty(id)) return null;
        foreach (OfficeFindingDefinition f in officeFindings)
            if (f != null && f.id == id) return f;
        return null;
    }

    public AnnotationKeyDefinition FindKey(string entryId)
    {
        if (annotationKey == null || string.IsNullOrEmpty(entryId)) return null;
        foreach (AnnotationKeyDefinition key in annotationKey)
            if (key != null && key.entryId == entryId) return key;
        return null;
    }

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

    /// <summary>Optional meaningful source lines; empty preserves legacy page-level matching.</summary>
    public List<string> evidenceKeys = new List<string>();

    /// <summary>Optional entries under review; empty preserves older page-level rules.</summary>
    public List<string> concernKeys = new List<string>();

    /// <summary>Shown only after the player supports the comparison, never in the book.</summary>
    public string supportedObservation;

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

/// <summary>
/// The income annotation toolkit (Level 2 market desk). One entry view and
/// one set of actions serve the guided tutorial (T05) and the scored entries
/// (T06): highlight the source line, place a TYPE note, a TREATMENT note and
/// clip the supporting record. Player-facing option text only; scored answer
/// keys are kept apart from this (added with T06).
/// </summary>
[Serializable]
public class AnnotationDefinition
{
    /// <summary>TYPE notes (where the money comes from), e.g. Compensation income.</summary>
    public List<AnnotationOptionDefinition> typeOptions = new List<AnnotationOptionDefinition>();

    /// <summary>TREATMENT notes (what the schedule does with it), e.g. Include in regular income.</summary>
    public List<AnnotationOptionDefinition> treatmentOptions = new List<AnnotationOptionDefinition>();

    /// <summary>The unscored demonstration entry (T0: an unrelated salary slip).</summary>
    public AnnotationEntryDefinition tutorial = new AnnotationEntryDefinition();

    /// <summary>The scored challenge entries (T06). Empty until then.</summary>
    public List<AnnotationEntryDefinition> entries = new List<AnnotationEntryDefinition>();

    public bool HasTutorial => tutorial != null && !string.IsNullOrWhiteSpace(tutorial.id);

    public AnnotationOptionDefinition FindOption(AnnotationSlot slot, string id, AnnotationEntryDefinition entry)
    {
        List<AnnotationOptionDefinition> list =
            slot == AnnotationSlot.Type ? typeOptions :
            slot == AnnotationSlot.Treatment ? treatmentOptions :
            entry != null ? entry.evidenceOptions : null;
        if (list == null || string.IsNullOrEmpty(id)) return null;
        foreach (AnnotationOptionDefinition option in list)
            if (option != null && option.id == id) return option;
        return null;
    }
}

/// <summary>One note card: stable id, short printed label, icon word and a one-line rule explanation.</summary>
[Serializable]
public class AnnotationOptionDefinition
{
    public string id;
    /// <summary>Printed on the card (two short lines at most).</summary>
    public string label;
    /// <summary>Rule explanation: general, never client-specific.</summary>
    public string rule;
}

/// <summary>One record laid out for annotation (rendered on the toolkit's paper).</summary>
[Serializable]
public class AnnotationEntryDefinition
{
    /// <summary>Stable id: T0 (tutorial), E1..E4 (challenge).</summary>
    public string id;
    public string title;
    public string issuer;
    public string reference;
    /// <summary>The printed rows; the player highlights one of them (unscored).</summary>
    public List<ReviewLineDefinition> lines = new List<ReviewLineDefinition>();
    /// <summary>Record references that can be clipped as evidence.</summary>
    public List<AnnotationOptionDefinition> evidenceOptions = new List<AnnotationOptionDefinition>();
    /// <summary>How many references the evidence clip holds (2 for a bundle such as D2 + D5).</summary>
    public int maxEvidence = 1;
    /// <summary>Tutorial only: what the guide demonstrates (shown openly, never scored).</summary>
    public AnnotationDemoDefinition demo = new AnnotationDemoDefinition();
}

/// <summary>The guide's demonstration on the tutorial entry, with the line it says at each step.</summary>
[Serializable]
public class AnnotationDemoDefinition
{
    public string highlightLineId;
    public string typeId;
    public string treatmentId;
    public List<string> evidenceIds = new List<string>();
    public string introLine;
    public string highlightLine;
    public string evidenceLine;
    public string yourTurnLine;
    public string doneLine;

    public bool IsSet => !string.IsNullOrWhiteSpace(highlightLineId) && !string.IsNullOrWhiteSpace(typeId) &&
                         !string.IsNullOrWhiteSpace(treatmentId) && evidenceIds != null && evidenceIds.Count > 0;
}

/// <summary>
/// The right answers for one scored entry and the correction hints for each
/// decision (first a general rule, then a more specific one). Hints teach
/// the income rule only: never summary totals, findings or the stamp.
/// </summary>
[Serializable]
public class AnnotationKeyDefinition
{
    public string entryId;
    public string highlightLineId;
    public string typeId;
    public string treatmentId;
    /// <summary>Exactly these records (a bundle when more than one).</summary>
    public List<string> evidenceIds = new List<string>();
    public List<string> typeHints = new List<string>();
    public List<string> treatmentHints = new List<string>();
    public List<string> evidenceHints = new List<string>();
    public List<string> highlightHints = new List<string>();

    public List<string> HintsFor(AnnotationSlot slot) =>
        slot == AnnotationSlot.Type ? typeHints : slot == AnnotationSlot.Treatment ? treatmentHints :
        slot == AnnotationSlot.Highlight ? highlightHints : evidenceHints;
}

/// <summary>A conclusion the player can write on a finding (from the book). Generic wording only.</summary>
[Serializable]
public class ReviewConclusionDefinition
{
    public string id;
    public string label;
}

/// <summary>
/// One record line that can be put in the reviewed schedule. moneyIds name the
/// actual payments it contains, so the same money can never be counted twice
/// (e.g. a cash receipt already inside a monthly total).
/// </summary>
[Serializable]
public class ScheduleSourceDefinition
{
    public string semanticKey;
    /// <summary>Optional: an annotation entry (E1..) whose highlighted line on the annotated paper supplies this value instead of a record row.</summary>
    public string entryId;
    public float amount;
    public List<string> moneyIds = new List<string>();
}

/// <summary>An office finding page: a title, a short prompt and its fill-in parts.</summary>
[Serializable]
public class OfficeFindingDefinition
{
    public string id;
    public string title;
    /// <summary>Earlier finding whose entry/clue print read-only on top (F1 continues the initial review).</summary>
    public string continuesFinding;
    public List<OfficeFindingPartDefinition> parts = new List<OfficeFindingPartDefinition>();
}

/// <summary>One fill-in box: accepts a record line ("record"), a book reason ("reason") or a book conclusion ("conclusion").</summary>
[Serializable]
public class OfficeFindingPartDefinition
{
    public const string Record = "record";
    public const string Reason = "reason";
    public const string Conclusion = "conclusion";

    public string id;
    public string label;
    public string accepts;
}

/// <summary>Answer data for the office review evaluator (T07/T09). Never printed.</summary>
[Serializable]
public class OfficeReviewRuleDefinition
{
    /// <summary>Payments that must be in a supported schedule.</summary>
    public List<string> requiredMoney = new List<string>();
    /// <summary>Payments that must stay out (not income), e.g. a loan.</summary>
    public List<string> forbiddenMoney = new List<string>();
    /// <summary>The omitted payment (its absence is the omission root cause).</summary>
    public string omittedMoney;
    public List<OfficeFindingRuleDefinition> findings = new List<OfficeFindingRuleDefinition>();

    public bool IsSet => requiredMoney != null && requiredMoney.Count > 0;
}

/// <summary>A supported finding: accepted keys/ids per part, and money that must really be in the schedule.</summary>
[Serializable]
public class OfficeFindingRuleDefinition
{
    public string findingId;
    public List<OfficePartRuleDefinition> parts = new List<OfficePartRuleDefinition>();
    /// <summary>Parts whose answers may be given in either order (e.g. "entry" and "compare").</summary>
    public List<string> unorderedParts = new List<string>();
    public string needsScheduleMoney;
    /// <summary>Root cause id named when this finding is not supported (e.g. "f1-unsupported").</summary>
    public string rootCause;
    /// <summary>Conclusions that wrongly hold/adjust an explained difference (false flag root cause).</summary>
    public List<string> falseFlagConclusions = new List<string>();
}

[Serializable]
public class OfficePartRuleDefinition
{
    public string partId;
    public List<string> accepted = new List<string>();
}

/// <summary>
/// A final assessment reason. verdict is the stamp it supports ("Ready" or
/// "NotReady"); names are the root-cause prefixes a Not Ready reason really
/// identifies (e.g. "omission"). Answer meaning only; the label is generic.
/// </summary>
[Serializable]
public class AssessmentReasonDefinition
{
    public string id;
    public string label;
    public string verdict;
    public List<string> names = new List<string>();
}

/// <summary>One Auditor line for a root cause (matched by prefix), pointing to the record/rule, never fixing it.</summary>
[Serializable]
public class AuditFeedbackDefinition
{
    public string root;
    public string line;
}

[Serializable]
public class AuditLinesDefinition
{
    public List<string> approved = new List<string>();
    public string justifiedHold;
    public string prematureApproval;
    public string falseHold;
    public string reasonError;
    public string unchanged;
    public string rosaHold;
    public string rosaPremature;
    public string rosaFalseHold;
    public List<AuditFeedbackDefinition> feedback = new List<AuditFeedbackDefinition>();
}

/// <summary>The review cutoff: active real seconds per day, warnings, and the one late consequence.</summary>
[Serializable]
public class ReviewClockDefinition
{
    public float allowanceSeconds = 720f;
    public List<float> warnSeconds = new List<float> { 120f, 30f };
    public int lateExp = 3;
    public int lateHearts = 1;
    public string announceLine;
    public string warnLine;
    public string missedLine;
    public string nextMorningLine;
    public string onTimeLine;

    public bool IsSet => allowanceSeconds > 0f;
}
