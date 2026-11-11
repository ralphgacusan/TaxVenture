/// <summary>
/// PURPOSE:
/// Bea's withholding certificate at the office credit step (Level 3 T08),
/// pure C# so the regression suite drives the same commands the conference
/// room calls. Every command runs on a COPY of the chapter snapshot; the
/// caller (Level3Checkpointer) writes it, then publishes it.
///
/// - CreditStepOpen: Bea's computation is open and her tax due has been on
///   the sheet (taxDueNoted). Only then does she hand over B-WHT.
/// - HandOver: B-WHT becomes an acquired source of her case under one stable
///   action id, in the candidate, before the paper can be seen or used.
///   A repeat (or a resume) is a Duplicate: nothing changes, nothing shows.
/// - Revised T08 (5 November 2026): the handover is one short turn and the
///   talk ends; the credit is transferred on the Case Folder's Computation
///   page (Level3ComputationRules.PlaceInput, certificate acquired only). The
///   question below is no longer asked; a saved explanation is kept as it was.
/// - The credit-versus-expense question (CONTENT "Withholding handover"):
///   Answer says what each option does. Only the right explanation is saved
///   (Explain); a wrong one opens the book's help and changes nothing (no
///   trust, ethics or grade), and "inspect" shows the certificate and comes
///   back to the same question. Explaining never fills in the credit: the
///   player links it on the sheet (Level3ComputationRules.SelectCredit).
/// No approval or reward here (T09).
/// </summary>
public static class Level3CertificateRules
{
    public const string HandOverAction = "Level_03/bea-certificate";
    public const string ExplainAction = "Level_03/bea-credit-explained";
    public const string ReceivedMilestone = "bea-certificate-received";

    /// <summary>The three answers to Bea's question, in CONTENT order.</summary>
    public const string AnswerCredit = "credit";
    public const string AnswerIncome = "income";
    public const string AnswerInspect = "inspect";
    public static readonly string[] Answers = { AnswerCredit, AnswerIncome, AnswerInspect };

    public enum AnswerKind { Unknown, Explains, NeedsHelp, Inspect }

    public const string RefuseNotYet = "Work out Bea's tax due on her sheet first. Then she'll show you one more paper.";
    public const string RefuseNotHere = "Bea's certificate cannot be handed over right now.";

    private static string Bea => Level3ChapterRules.BeaCaseId;

    /// <summary>Bea's case holds her certificate (acquired at the credit step).</summary>
    public static bool Acquired(Level3ChapterCheckpoint state) =>
        state?.Case(Bea)?.acquiredSourceIds?.Contains(Level3ChapterRules.BeaCreditSource) == true;

    /// <summary>The player explained the certificate as a credit (saved).</summary>
    public static bool Explained(Level3ChapterCheckpoint state) => state?.Case(Bea)?.computation?.creditExplained == true;

    /// <summary>The office credit step: her computation is open and her tax due has been on the sheet.</summary>
    public static bool CreditStepOpen(Level3ChapterCheckpoint state) =>
        Level3ComputationRules.Editable(state, Bea) && state.Case(Bea)?.computation?.taxDueNoted == true;

    /// <summary>The certificate handover is due now (the credit step is open and she still has it).</summary>
    public static bool HandOverDue(Level3ChapterCheckpoint state) => CreditStepOpen(state) && !Acquired(state);

    public const string OfferedAction = "Level_03/bea-certificate-offered";
    public static bool Offered(Level3ChapterCheckpoint state) => state?.actionIds?.Contains(OfferedAction) == true;
    public static Level3CommandOutcome Offer(Level3ChapterCheckpoint state, out string reason)
    {
        reason = null;
        if (Offered(state) || Acquired(state)) return Level3CommandOutcome.Duplicate;
        if (!HandOverDue(state)) { reason = RefuseNotYet; return Level3CommandOutcome.Refused; }
        state.actionIds.Add(OfferedAction);
        return Level3CommandOutcome.Applied;
    }
    /// <summary>T13: a full bar and Bag (the certificate needs its own slot to be read and dragged from).</summary>
    public const string RefuseFull = "Your bar and Bag are full. Make room, then take the certificate.";

    /// <summary>T13: the readable certificate has a slot to land in (or is already owned).</summary>
    public static bool HasRoom(CampaignInventoryRecord inventory) => Level3ReviewRules.RoomFor(inventory, InventoryCatalogue.BeaCertificate);

    // =========================================================
    // MILESTONE T01: CLIP TO BEA'S FOLDER (a full base bar and Bag)
    // =========================================================

    public const string ClipAction = "Level_03/bea-certificate-clipped";
    public const string OfferClip = "Your bar and Bag are full. Tap Clip to Bea's folder, or make room and take it.";
    public const string RefuseFolderAway = "Bea's folder isn't with you. Take it from your desk, then clip the certificate.";
    public const string ClippedLine = "Clipped into Bea's folder, under Supporting Documents.";

    /// <summary>Bea's working folder can take the paper: her computation is open and the folder is carried (not on the desk, not with the Auditor).</summary>
    public static bool FolderEligible(Level3ChapterCheckpoint state, bool folderCarried) =>
        folderCarried && Level3ComputationRules.Editable(state, Bea) && !Level3ReviewRules.FolderInWorld(state);

    /// <summary>The certificate went straight into her folder's supporting slot (it has no slot of its own while clipped).</summary>
    public static bool Clipped(Level3ChapterCheckpoint state) => Acquired(state) && state.Case(Bea)?.folder?.certificateAttached == true;

    /// <summary>
    /// The deliberate full-Bag alternative: ONE candidate in which Bea hands over B-WHT (HandOver, its own
    /// action id) and it is clipped into her carried folder's supporting slot (the existing attachment).
    /// No slot is needed, nothing is evicted, no credit is entered. A repeat is a Duplicate; outside the
    /// credit step, or without her folder, it is Refused and the candidate is left as it was.
    /// </summary>
    public static Level3CommandOutcome ClipToFolder(Level3ChapterCheckpoint candidate, bool folderCarried, out string reason)
    {
        reason = null;
        if (candidate?.actionIds != null && candidate.actionIds.Contains(HandOverAction) || Acquired(candidate))
        {
            reason = "already handed over";
            return Level3CommandOutcome.Duplicate;
        }
        // The credit step's own refusals first (HandOver changes nothing when it refuses).
        if (candidate == null || !HandOverDue(candidate)) return HandOver(candidate, out reason);
        if (!FolderEligible(candidate, folderCarried)) { reason = RefuseFolderAway; return Level3CommandOutcome.Refused; }
        if (HandOver(candidate, out reason) != Level3CommandOutcome.Applied ||
            Level3ReviewRules.SetAttachment(candidate, Level3ChapterRules.BeaCreditSource, true, out reason) != Level3CommandOutcome.Applied)
        {
            reason ??= RefuseNotHere;
            return Level3CommandOutcome.Refused;
        }
        candidate.actionIds.Add(ClipAction);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>
    /// What the saved handover does to the carried inventory, in the same candidate: an ordinary pickup
    /// takes its own slot (AcquireItem); a clipped certificate keeps only its readable pages with the
    /// carried folder, as an attached paper does (Level3ReviewRules.SyncFolderInventory). Story requires
    /// the folder it was clipped into to be owned.
    /// </summary>
    public static InventoryResult ApplyToInventory(CampaignInventoryRecord inventory, InventoryCatalogue catalogue, Level3ChapterCheckpoint state,
        CaseDefinition definition, bool requireFolder)
    {
        if (!Clipped(state)) return AcquireItem(inventory, catalogue, state, definition);
        if (inventory == null) return InventoryResult.Reject("Inventory is not ready.");
        if (requireFolder && !InventoryRules.Owns(inventory, InventoryCatalogue.CaseFolder)) return InventoryResult.Reject(RefuseFolderAway);
        var result = new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = InventoryCatalogue.CaseFolder };
        foreach (InventoryReferencePage page in ReferencePages(definition))
        {
            InventoryResult kept = InventoryRules.KeepReference(inventory, InventoryCatalogue.CaseFolder, page);
            if (kept.Changed) result = kept;
        }
        return result;
    }

    /// <summary>The certificate's printed lines as read-only reference pages (six lines a page).</summary>
    private static System.Collections.Generic.List<InventoryReferencePage> ReferencePages(CaseDefinition definition)
    {
        var pages = new System.Collections.Generic.List<InventoryReferencePage>();
        if (definition == null) return pages;
        foreach (ClientRecord record in Level3RecordRules.Build(definition, new[] { Level3ChapterRules.BeaCreditSource }))
        {
            var lines = new System.Collections.Generic.List<string> { record.issuer, record.reference, record.date };
            foreach (ClientRecordRow row in record.rows) lines.Add(row.label + ": " + row.value);
            for (int start = 0; start < lines.Count; start += 6)
                pages.Add(new InventoryReferencePage { id = definition.caseId + "/B-WHT/" + start,
                    title = "Withholding certificate", lines = lines.GetRange(start, System.Math.Min(6, lines.Count - start)) });
        }
        return pages;
    }

    public static InventoryResult AcquireItem(CampaignInventoryRecord inventory, InventoryCatalogue catalogue, Level3ChapterCheckpoint state, CaseDefinition definition, string actionId = "Level_03/bea-certificate-item", SlotRef? target = null)
    {
        if (!Acquired(state) || inventory == null || catalogue == null) return InventoryResult.Reject("The certificate is not ready.");
        if (InventoryRules.Owns(inventory, InventoryCatalogue.BeaCertificate)) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = InventoryCatalogue.BeaCertificate };
        if (!target.HasValue && !HasRoom(inventory)) return InventoryResult.Reject(RefuseFull);
        var info = new InventoryItemRecord { itemId = InventoryCatalogue.BeaCertificate, source = "Bea", workingDocumentId = Level3ChapterRules.BeaCreditSource };
        info.referencePages.AddRange(ReferencePages(definition));
        InventoryResult result = InventoryRules.Acquire(inventory, catalogue, InventoryCatalogue.BeaCertificate, 1, actionId, target, info);
        if (result.Ok)
            foreach (InventoryReferencePage page in info.referencePages) InventoryRules.KeepReference(inventory, InventoryCatalogue.BeaCertificate, page);
        return result;
    }

    /// <summary>Her question is waiting for the right explanation (she handed it over; it was not explained yet).</summary>
    public static bool QuestionDue(Level3ChapterCheckpoint state) => Acquired(state) && !Explained(state) && Level3ComputationRules.Editable(state, Bea);

    public static AnswerKind Answer(string answerId) =>
        answerId == AnswerCredit ? AnswerKind.Explains : answerId == AnswerIncome ? AnswerKind.NeedsHelp :
        answerId == AnswerInspect ? AnswerKind.Inspect : AnswerKind.Unknown;

    /// <summary>B-WHT is handed over (acquired in the candidate). Refused outside the credit step; a repeat is a Duplicate.</summary>
    public static Level3CommandOutcome HandOver(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (candidate?.actionIds != null && candidate.actionIds.Contains(HandOverAction) || Acquired(candidate))
        {
            reason = "already handed over";
            return Level3CommandOutcome.Duplicate;
        }
        if (candidate == null || Level3ChapterRules.Problem(candidate) != null || !Level3ComputationRules.Editable(candidate, Bea))
        {
            reason = RefuseNotHere;
            return Level3CommandOutcome.Refused;
        }
        if (!CreditStepOpen(candidate)) { reason = RefuseNotYet; return Level3CommandOutcome.Refused; }
        Level3CaseProgress bea = candidate.Case(Bea);
        bea.acquiredSourceIds.Add(Level3ChapterRules.BeaCreditSource);
        candidate.actionIds.Add(HandOverAction);
        if (!candidate.milestones.Contains(ReceivedMilestone)) candidate.milestones.Add(ReceivedMilestone);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>The right explanation is saved (it fills in nothing). Refused without the certificate; a repeat is a Duplicate.</summary>
    public static Level3CommandOutcome Explain(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (Explained(candidate)) { reason = "already explained"; return Level3CommandOutcome.Duplicate; }
        if (!Acquired(candidate) || !Level3ComputationRules.Editable(candidate, Bea)) { reason = RefuseNotHere; return Level3CommandOutcome.Refused; }
        Level3CaseProgress bea = candidate.Case(Bea);
        if (bea.computation == null) bea.computation = new Level3ComputationRecord();
        bea.computation.creditExplained = true;
        if (!candidate.actionIds.Contains(ExplainAction)) candidate.actionIds.Add(ExplainAction);
        return Level3CommandOutcome.Applied;
    }
}
