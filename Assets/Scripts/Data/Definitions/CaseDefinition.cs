
using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// The complete, immutable definition of one playable case, loaded from a
/// single JSON file. This is the "master answer sheet" per the migration
/// brief — it contains the CORRECT case exactly as it should exist if
/// completed perfectly, plus explicit markers for which fields are
/// missing (hidden from the player, must be filled in) and which document
/// fields are deliberately displayed incorrectly (for the player to catch).
///
/// This class is NEVER mutated at runtime. CaseFactory (Phase 2) reads
/// this once per case-start to produce a fresh, mutable CaseData instance
/// — your existing runtime class, completely unchanged.
///
/// WHY THIS SHAPE MATCHES YOUR EXISTING CaseData FIELD-FOR-FIELD:
/// Every field here corresponds directly to a CaseData field, so
/// CaseFactory's mapping is a straight one-to-one copy with enum parsing
/// — no guessing, no inference, no hidden defaults.
/// </summary>
[Serializable]
public class CaseDefinition
{
    public string caseId;

    // ---------- Page 1 ----------
    public string caseNumber;

    // ---------- Client Model ----------
    // Stable identifier for the client.
    // Example: "client_001"
    public string clientId;

    // Name/key of the 3D client model.
    // Example: "Client_001"
    public string clientModel;

    public string taxYear;
    public string dateReceived;
    public string assignedConsultant;
    public string caseTitle;
    public string caseSummary;

    // ---------- Page 2: Client / Taxpayer Information ----------
    public string fullName;
    public string tin;
    public string birthdate;
    public string address;
    public string contactNumber;
    public string civilStatus;   // parses to CivilStatus
    public string spouseName;
    public string spouseTin;
    public string citizenship;

    // ---------- Answer Key (ground truth, never shown directly) ----------
    public AnswerKeyDefinition answerKey;

    // ---------- Fields deliberately hidden from the player at case start ----------
    // Each entry is a semanticKey (e.g. "TaxDue", "BusinessAddress") that
    // must NOT be pre-filled anywhere the player can see it — the folder/
    // documents must show it as unknown/blank until the player transfers
    // the correct value in themselves.
    public List<string> missingFieldKeys = new List<string>();

    // ---------- Supporting Documents (may include deliberately WRONG values) ----------
    public List<DocumentDefinition> documents = new List<DocumentDefinition>();

    // ---------- Grading (what the Auditor checks) ----------
    // The fields the player must fill in correctly, in the order the Auditor
    // reports them, each with its hint and real-world cost. Empty = the
    // Level 1 rules (residencyStatus, taxpayerType, incomeSource).
    public List<RequiredFieldDefinition> requiredFields = new List<RequiredFieldDefinition>();

    // What the Auditor says when the case is approved. Empty = built-in lines.
    public List<string> auditorApprovedLines = new List<string>();

    // ---------- Consultant Findings placeholder text ----------
    public List<string> potentialIssues = new List<string>();

    // ---------- Client Portraits (for DialogueUI) ----------
    public string clientPortraitDefault;
    public string clientPortraitHappy;
    public string clientPortraitSad;

    // ---------- Dialogue Sections ----------
    public List<DialogueLineDefinition> receptionistGreeting =
        new List<DialogueLineDefinition>();

    public List<DialogueLineDefinition> clientFirstMeeting =
        new List<DialogueLineDefinition>();

    public List<DialogueLineDefinition> interviewLines =
        new List<DialogueLineDefinition>();

    public List<DialogueLineDefinition> outcomeCorrectNoIssues =
        new List<DialogueLineDefinition>();

    public List<DialogueLineDefinition> outcomeCorrectWithIssues =
        new List<DialogueLineDefinition>();

    public List<DialogueLineDefinition> outcomeWrongVerdict =
        new List<DialogueLineDefinition>();

    public List<DialogueLineDefinition> outcomeWrongVerdictWithIssues =
        new List<DialogueLineDefinition>();

    // ---------- Consequences (Level 1 Phase 3) ----------
    // What the client says when you visit her after a failed audit (clues
    // again, never the answer). Empty = the client has nothing new to say.
    public List<DialogueLineDefinition> clientAfterFailedAudit =
        new List<DialogueLineDefinition>();

    // One-line reactions shown in a bubble with the client's portrait.
    public string clientFailedBubble;
    public string clientFailedPortrait;
    public string clientApprovedBubble;
    public string clientApprovedPortrait;

    // What each kind of mistake would mean in real life (Audit Summary).
    public MistakeCostDefinition mistakeCosts = new MistakeCostDefinition();

    // The Auditor's closing line after the 1st, 2nd, 3rd... failed audit
    // (the last one repeats), and his line when the same answers come back.
    public List<string> auditorRetryLines = new List<string>();
    public string auditorSameAnswersLine;

    // The end-of-level scene with the client (CaseFinale).
    public ClientFinaleDefinition clientFinale = new ClientFinaleDefinition();

    // The Client Copy handover after approval (Campaign Foundation T08).
    public ClientHandoverDefinition clientHandover = new ClientHandoverDefinition();

    // Visits to the client in the conference room during the case (T23).
    // Optional: left out, the client has no consultation.
    public ClientConsultationDefinition consultation;

    // Optional prepared-packet review (Level 2+). Leave out for an ordinary
    // case; only a non-empty review.reviewId turns it on.
    public CaseReviewDefinition review = new CaseReviewDefinition();

    /// <summary>True when this case carries review data (review.reviewId set).</summary>
    public bool HasReview => review != null && review.IsReview;

    /// <summary>The fields this case grades (its requiredFields, or the Level 1 defaults).</summary>
    public List<RequiredFieldDefinition> GradedFields()
    {
        var fields = new List<RequiredFieldDefinition>();
        if (requiredFields != null)
            foreach (RequiredFieldDefinition field in requiredFields)
                if (field != null && !string.IsNullOrWhiteSpace(field.key))
                    fields.Add(field);
        return fields.Count > 0 ? fields : CaseFieldRules.Defaults();
    }

    /// <summary>Real-world cost of getting a field wrong (requiredFields first, then mistakeCosts).</summary>
    public string MistakeCostFor(string fieldKey)
    {
        if (string.IsNullOrEmpty(fieldKey)) return null;
        if (requiredFields != null)
            foreach (RequiredFieldDefinition field in requiredFields)
                if (field != null && field.key == fieldKey && !string.IsNullOrWhiteSpace(field.mistakeCost))
                    return field.mistakeCost;
        return mistakeCosts != null ? mistakeCosts.For(fieldKey) : null;
    }
}

/// <summary>
/// The client's end-of-level scene: which plaza model to show, where she
/// stands, the shot, and her lines per result (great / good / okay).
/// </summary>
[Serializable]
public class ClientFinaleDefinition
{
    public bool enabled = true;
    public string name = "Marie";
    public string modelPath = "Environment/Outside/Plaza/NPCS/NPC_Marie";
    public string spotPath = "Environment/OfficeBuilding/Rooms/ConferenceRoom/Client";
    public string roomPath = "Environment/OfficeBuilding/Rooms/ConferenceRoom";
    /// <summary>The room's floor (she stands on its top surface).</summary>
    public string floorPath = "Environment/OfficeBuilding/Rooms/ConferenceRoom/Floors";
    /// <summary>1 = default distance; bigger = camera farther back.</summary>
    public float distanceScale = 1f;
    public float fieldOfView = 40f;
    /// <summary>Where she stands across the screen, 0 = left edge, 0.5 = centre.</summary>
    public float screenLeft = 0.25f;
    public List<string> great = new List<string>();
    public List<string> good = new List<string>();
    public List<string> okay = new List<string>();
    /// <summary>
    /// T08: her lines beside the result card after the Client Copy handover
    /// (she already said great/good/okay in person), per result, each with an
    /// optional emote. Empty lists use built-in defaults.
    /// </summary>
    public List<DialogueLineDefinition> farewellGreat = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> farewellGood = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> farewellOkay = new List<DialogueLineDefinition>();
}

/// <summary>
/// The Client Copy handover (T08): the Auditor's lines when the copy appears
/// and when he is asked again, the handover conversation with the client
/// (opening lines, the line that waits for the copy, then Elijah's
/// explanation), and the client's closing emote per result. Her closing
/// lines are clientFinale's great / good / okay. Empty lists use built-in
/// defaults.
/// </summary>
[Serializable]
public class ClientHandoverDefinition
{
    public bool enabled = true;
    /// <summary>
    /// The Auditor's approval conversation after the Audit Summary, per result
    /// (great / good / okay as in clientFinale); auditorGive follows it, and
    /// the copy drops beside him when it ends.
    /// </summary>
    public List<DialogueLineDefinition> approvalGreat = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> approvalGood = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> approvalOkay = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> auditorGive = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> auditorRemind = new List<DialogueLineDefinition>();
    /// <summary>A tap on the client before the copy is picked up.</summary>
    public List<DialogueLineDefinition> waiting = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> opening = new List<DialogueLineDefinition>();
    /// <summary>Elijah's line that waits until the copy is dragged to (or tapped for) the client.</summary>
    public DialogueLineDefinition handover = new DialogueLineDefinition();
    public string handoverPrompt = "Drag the client copy to Marie";
    public string retryPrompt = "It didn't save. Drag the client copy to Marie again.";
    public List<DialogueLineDefinition> explain = new List<DialogueLineDefinition>();
    /// <summary>Level 2: the client's short line when tapped again after the handover (talk only).</summary>
    public List<DialogueLineDefinition> afterDelivery = new List<DialogueLineDefinition>();
    /// <summary>The client's emote on her closing lines, per result.</summary>
    public string greatEmote = "sparkles";
    public string goodEmote = "heart";
    public string okayEmote = "music";
}

/// <summary>Real-world meaning of a mistake, per required field (never the answer).</summary>
[Serializable]
public class MistakeCostDefinition
{
    public string residencyStatus;
    public string taxpayerType;
    public string incomeSource;

    public string For(string fieldKey)
    {
        switch (fieldKey)
        {
            case "residencyStatus": return residencyStatus;
            case "taxpayerType": return taxpayerType;
            case "incomeSource": return incomeSource;
        }
        return null;
    }
}

/// <summary>
/// The ground-truth correct values for every determinable/computable field
/// in the case. Used exclusively by grading logic (ComplianceChecker,
/// CaseVerdictEvaluator in later phases) — never copied directly into the
/// player-visible CaseData at case start.
/// </summary>
[Serializable]
public class AnswerKeyDefinition
{
    public string residencyStatus;
    public string taxpayerType;
    public string incomeSource;
    public string numberOfEmployers;
    public string businessRegistration;
    public string taxOption;
    public string requiredForm;
    public string correctAssessment;

    public float grossIncome;
    public float allowableExpenses;
    public float taxableIncome;
    public float taxDue;
    public float taxWithheldOrCredits;
    public float finalTaxPayable;

    public string employerTin;
    public string businessAddress;
    public int dependentCount;
}

/// <summary>
/// One supporting document's field content. Each field carries BOTH the
/// value shown to the player (displayValue, which may be deliberately
/// wrong) and the actual correct value, plus metadata explaining why it's
/// wrong when applicable — matching the "Incorrect Values" requirement.
/// </summary>
[Serializable]
public class DocumentFieldDefinition
{
    public string label;
    public string displayValue;
    public string correctValue;
    public string type;
    public string semanticKey;
    public bool isIncorrect;
    public string incorrectReason;
    public string issueType;
    public string severity;
}

[Serializable]
public class DocumentDefinition
{
    public string documentName;
    public List<DocumentFieldDefinition> fields =
        new List<DocumentFieldDefinition>();
}

[Serializable]
public class DialogueLineDefinition
{
    public string speaker;
    public string text;
    public string portraitId;
    /// <summary>Optional emote tag for this line (T22), e.g. "heart"; unknown tags are ignored with a warning.</summary>
    public string emote;
    /// <summary>Optional: play the emote as a burst (peak moments only).</summary>
    public bool burst;
}
