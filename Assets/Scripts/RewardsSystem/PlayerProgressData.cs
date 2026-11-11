using System;
using System.Collections.Generic;

/// <summary>Best result saved for one level (save version 2).</summary>
[Serializable]
public class LevelRecord
{
    public string levelId;
    public int bestExp;
    public int bestHearts;
    public int maxExp;
    public int maxHearts;
    public bool passed;
    public int attempts;
    public List<string> badges = new List<string>();
    // Time spent in whole seconds (0 = never measured; older saves have none).
    public int bestSeconds;
    public int lastSeconds;
    public int totalSeconds;
    public int sideQuestBestSeconds;
    public int sideQuestLastSeconds;
}

/// <summary>Best tier of one collected badge (0 none, 1 bronze, 2 silver, 3 gold).</summary>
[Serializable]
public class BadgeRecord
{
    public string badgeId;
    public int tier;
}

/// <summary>
/// Training Hub progress for one drill card (save version 4). practiceRuns =
/// finished guided runs, masteryRuns = finished mastery runs; a card is
/// MASTERED once masteryRuns > 0. Practice never touches EXP, hearts,
/// badges or levels.
/// </summary>
[Serializable]
public class TrainingRecord
{
    public string cardId;
    public int practiceRuns;
    public int masteryRuns;
    /// <summary>Fastest finished mastery run in whole seconds (0 = none).</summary>
    public int bestMasterySeconds;

    public bool Mastered => masteryRuns > 0;
}

/// <summary>
/// Saved progression. A level is COMPLETED when finished at any score, and
/// PASSED when finished at or above its passing line; only a passed level
/// unlocks the next one. Each level keeps its best result, and the career
/// totals grow only by improvements on that best (replays never farm EXP).
///
/// VERSIONS: 1 had no pass line (every completed level counts as passed);
/// 2 added best-result records and badges; 3 is written signed and
/// crash-safe by PlayerProgressManager (same fields); 4 added the Training
/// Hub lists (tutorialsSeen, trainingRecords), which never
/// affect career totals; 5 is the updated story campaign (campaign record).
/// Normalize() upgrades
/// any older save in place and repairs impossible values (negative totals,
/// a best above its maximum, duplicate ids, badge tiers out of range), so
/// whatever is on disk always loads as a consistent save. A save without a
/// campaign is only ever upgraded to version 4: it never becomes a v5
/// campaign by loading; only a confirmed New Game creates one.
/// </summary>
/// <summary>
/// One supply or keepsake the campaign holds: carried in the bag and stored in
/// the office drawer (version 5).
/// </summary>
[Serializable]
public class CampaignItemStack
{
    public string itemId;
    public int carried;
    public int stored;
}

/// <summary>One profile card pinned on the Level 1 corkboard: stable profile id and the area's classification name.</summary>
[Serializable]
public class Level1Placement
{
    public string profileId;
    public string slotId;
}

/// <summary>How often one corkboard area has received a card (and whether its first card was right).</summary>
[Serializable]
public class Level1SlotAttempts
{
    public string slotId;
    public int attempts;
    public bool firstTrySolved;
}

/// <summary>One player-entered case folder field: its stable folder key and the value as text (enum name or invariant number).</summary>
[Serializable]
public class Level1CaseField
{
    public string key;
    public string value;
}

/// <summary>
/// Marie's case in the unfinished Level 1 story run (T05). The case is
/// identified by its id and a fingerprint of its authored definition, so a
/// snapshot of another case or an older revision is never applied. Level 1's
/// documents are read-only originals (no working copies); reviewedDocuments
/// lists which of them were read. folder: "desk" (on the desk / in play) or
/// "stored" (in the HUD folder icon, ready for the Auditor). A hand-in still
/// being audited is saved as "stored": the audit only counts once its
/// summary closes, so a resume hands the folder in again without a charge.
/// </summary>
[Serializable]
public class Level1CaseRecord
{
    public const string FolderOnDesk = "desk";
    public const string FolderStored = "stored";

    public string caseId;
    public string caseFingerprint;
    public bool receptionistDone;
    public string folder = FolderOnDesk;
    public List<Level1CaseField> fields = new List<Level1CaseField>();
    public List<string> reviewedDocuments = new List<string>();
    public string caseAssessment;
    public bool assessmentStamped;
    public string computationStatus;
    public bool hasPrintedReturn;
    public bool isCarryingPrintedReturn;
    /// <summary>What the player typed into the BIR form (JsonUtility of EncodedFormData), or empty.</summary>
    public string encodedFormJson;
    public bool taxCodeBookOpened;
    /// <summary>T25: Marie's two work records (background, 2316) were taken out and put back at the desk once (never relocked).</summary>
    public bool workRecordsPrepared;
    public int auditAttempts;
    public int auditMistakes;
    public int failedAudits;
    /// <summary>The Auditor's retry guard: the signature of the last answers that failed (never the answer key).</summary>
    public string lastFailedAnswers;

    public bool IsSet => !string.IsNullOrWhiteSpace(caseId);
}

/// <summary>
/// Level 1's Office Hours game clock (T05): the in-game day and time, the
/// active (playable) seconds, the Auditor's deadline (first day and current),
/// whether it was announced, the hand-in time and day, a missed day, and the
/// review seconds an appointment extension already added (T14 consumes the
/// voucher; this only remembers the applied seconds). It is the game's
/// clock, never a statutory tax deadline.
/// </summary>
[Serializable]
public class Level1ClockRecord
{
    public const int MaxExtensionSeconds = 180;

    public bool isSet;
    public int day = 1;
    public double gameSeconds;
    public float activeSeconds;
    public float sideQuestSeconds = -1f;
    public float allowance;
    public bool hasDeadline;
    public bool announced;
    public int deadlineSeconds;
    public int deadlineFirstDay;
    public double submittedAt = -1;
    public int submittedDay;
    public bool onTimeSaid;
    public bool missedOnce;
    public int extensionSeconds;
}

/// <summary>
/// T23: the case client's consultation state in the Level 1 checkpoint.
/// clientInRoom: Marie was moved to the conference room. visits: all
/// consultation visits; ladderVisits: ladder lines since the last reset.
/// lastLineId: the line said last (never said again next). seenFailedAudits /
/// seenDay: the reset markers already reacted to; pendingSpecial: a special
/// moment still to say ("failedAudit" / "laterDay"); givenClueIds: clues
/// given since the last reset. Ids only, never answers.
/// </summary>
[Serializable]
public class Level1ConsultationRecord
{
    public bool clientInRoom;
    public int visits;
    public int ladderVisits;
    public string lastLineId = "";
    public int seenFailedAudits;
    public int seenDay = 1;
    public string pendingSpecial = "";
    public List<string> givenClueIds = new List<string>();

    /// <summary>Structural repair: non-negative counters, day 1 or later, a known special, clean ids.</summary>
    public bool Repair()
    {
        bool changed = false;
        if (visits < 0) { visits = 0; changed = true; }
        if (ladderVisits < 0) { ladderVisits = 0; changed = true; }
        if (ladderVisits > visits) { ladderVisits = visits; changed = true; }
        if (seenFailedAudits < 0) { seenFailedAudits = 0; changed = true; }
        if (seenDay < 1) { seenDay = 1; changed = true; }
        string trimmed = lastLineId == null ? "" : lastLineId.Trim();
        if (trimmed != lastLineId) { lastLineId = trimmed; changed = true; }
        if (pendingSpecial != "" && pendingSpecial != "failedAudit" && pendingSpecial != "laterDay") { pendingSpecial = ""; changed = true; }
        if (givenClueIds == null) { givenClueIds = new List<string>(); changed = true; }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<string>(givenClueIds.Count);
        foreach (string id in givenClueIds)
        {
            string clean = id?.Trim();
            if (string.IsNullOrEmpty(clean) || !seen.Add(clean)) { changed = true; continue; }
            if (clean != id) changed = true;
            kept.Add(clean);
        }
        givenClueIds = kept;
        return changed;
    }
}

/// <summary>
/// Level 1 field progress of the unfinished story run (T04): stable ids and
/// counters only, never scene objects, camera poses, held drags or answers.
/// boardPhase: "" (board not opened), "active", "solved" (all four right, the
/// guide's wrap-up and reward still to come) or "rewarded". The check* fields
/// are reserved for T06's first complete check and stay unset until then.
/// </summary>
[Serializable]
public class Level1FieldCheckpoint
{
    public const int CurrentVersion = 2;
    public const string PhaseNone = "";
    public const string PhaseActive = "active";
    public const string PhaseSolved = "solved";
    public const string PhaseRewarded = "rewarded";
    // T08 Client Copy handover, after the case result is recorded (ClientCopyRules).
    public const string HandoverNone = "";
    public const string HandoverAtAuditor = "copyAtAuditor";
    public const string HandoverCarried = "copyCarried";
    public const string HandoverDone = "followThroughDone";

    public int version;
    public int writes;
    public List<string> talkedNpcIds = new List<string>();
    public List<string> collectedProfileIds = new List<string>();
    public bool guideIntroDone;
    public string boardPhase = PhaseNone;
    public int placementAttempts;
    public int wrongPlacements;
    public List<Level1Placement> placements = new List<Level1Placement>();
    public List<Level1SlotAttempts> slotAttempts = new List<Level1SlotAttempts>();
    public List<string> objectiveTriggers = new List<string>();
    /// <summary>Guide lessons already finished in this run (welcome card, HUD tour...), so a resume does not replay them.</summary>
    public List<string> guideLessonsDone = new List<string>();
    /// <summary>T16: the Notes chest lid stays open after a quiet Continue restore.</summary>
    public bool notesChestOpened;
    /// <summary>T30: Arthur's opening talk completed (his reviewed file was presented). ReviewedFileRules.</summary>
    public bool arthurIntroDone;
    /// <summary>Records in Arthur's reviewed file (stable ids, T30; T24 used these lists for Jhun-jhun's) that reached a settled close-up.</summary>
    public List<string> presentedRecordIds = new List<string>();
    /// <summary>The same records put back after that close-up.</summary>
    public List<string> returnedRecordIds = new List<string>();
    /// <summary>T30: the reviewed file's completed first page was shown again after both records were put back.</summary>
    public bool reviewedPageRevisited;
    /// <summary>T30: the four plaza clients are open (the reviewed file was looked over, or the rounds had already begun).</summary>
    public bool roundsOpen;

    /// <summary>Marie's case (T05); empty until the office part begins.</summary>
    public Level1CaseRecord caseRecord = new Level1CaseRecord();
    /// <summary>A case snapshot that did not match the current case (other case or revision), kept aside, never applied.</summary>
    public Level1CaseRecord setAsideCase = new Level1CaseRecord();
    /// <summary>The Office Hours clock (T05).</summary>
    public Level1ClockRecord clock = new Level1ClockRecord();

    // T06 (first complete check): frozen once, corrections never change it.
    public bool checkSubmitted;
    /// <summary>Right answers on the first complete check (0-4), -1 before it.</summary>
    public int checkCorrectCount = -1;
    /// <summary>Profiles wrong on the first complete check.</summary>
    public List<string> checkWrongProfileIds = new List<string>();
    /// <summary>Quest XP settled from the first check, -1 before it.</summary>
    public int checkSettledExp = -1;
    /// <summary>Complete checks made so far (0 before the first); from 3 the board shows answers.</summary>
    public int checkCount;

    /// <summary>
    /// T08: where Marie's approved Client Copy is after the case result:
    /// HandoverAtAuditor (floating beside the Auditor), HandoverCarried (in
    /// the folder icon), or HandoverDone (waiting for the end flow).
    /// </summary>
    public string handoverPhase = HandoverNone;

    /// <summary>T23: Marie in the conference room and her consultation visits.</summary>
    public Level1ConsultationRecord consultation = new Level1ConsultationRecord();

    /// <summary>Anything to resume (a version was written by the checkpointer).</summary>
    public bool IsSet => version > 0;

    public static bool IsKnownPhase(string phase) =>
        phase == PhaseNone || phase == PhaseActive || phase == PhaseSolved || phase == PhaseRewarded;

    public static bool IsKnownHandover(string phase) =>
        phase == HandoverNone || phase == HandoverAtAuditor || phase == HandoverCarried || phase == HandoverDone;

    /// <summary>
    /// Structural repair only (no profile knowledge; Level1FieldCheckpointRules
    /// checks ids against the content): no null lists, trimmed unique ids,
    /// one placement per profile and per area (first kept), one attempt row
    /// per area, non-negative counters, wrong placements within attempts, a
    /// known phase. A newer version is left for the restore check to reject.
    /// </summary>
    public bool Repair()
    {
        bool changed = false;
        if (version < 0) { version = 0; changed = true; }
        if (writes < 0) { writes = 0; changed = true; }
        talkedNpcIds = CleanIds(talkedNpcIds, ref changed);
        collectedProfileIds = CleanIds(collectedProfileIds, ref changed);
        objectiveTriggers = CleanIds(objectiveTriggers, ref changed);
        guideLessonsDone = CleanIds(guideLessonsDone, ref changed);
        checkWrongProfileIds = CleanIds(checkWrongProfileIds, ref changed);
        presentedRecordIds = CleanIds(presentedRecordIds, ref changed);
        returnedRecordIds = CleanIds(returnedRecordIds, ref changed);
        if (boardPhase == null || !IsKnownPhase(boardPhase.Trim()))
        {
            boardPhase = PhaseNone;
            changed = true;
        }
        else if (boardPhase != boardPhase.Trim()) { boardPhase = boardPhase.Trim(); changed = true; }
        if (handoverPhase == null || !IsKnownHandover(handoverPhase.Trim()))
        {
            handoverPhase = HandoverNone;
            changed = true;
        }
        else if (handoverPhase != handoverPhase.Trim()) { handoverPhase = handoverPhase.Trim(); changed = true; }

        if (placements == null) { placements = new List<Level1Placement>(); changed = true; }
        var profiles = new HashSet<string>(StringComparer.Ordinal);
        var areas = new HashSet<string>(StringComparer.Ordinal);
        var keptPlacements = new List<Level1Placement>(placements.Count);
        foreach (Level1Placement p in placements)
        {
            string profile = p?.profileId?.Trim();
            string area = p?.slotId?.Trim();
            if (string.IsNullOrEmpty(profile) || string.IsNullOrEmpty(area) || !profiles.Add(profile) || !areas.Add(area)) { changed = true; continue; }
            if (p.profileId != profile || p.slotId != area) { p.profileId = profile; p.slotId = area; changed = true; }
            keptPlacements.Add(p);
        }
        placements = keptPlacements;

        if (slotAttempts == null) { slotAttempts = new List<Level1SlotAttempts>(); changed = true; }
        var seenAreas = new HashSet<string>(StringComparer.Ordinal);
        var keptAttempts = new List<Level1SlotAttempts>(slotAttempts.Count);
        foreach (Level1SlotAttempts a in slotAttempts)
        {
            string area = a?.slotId?.Trim();
            if (string.IsNullOrEmpty(area) || !seenAreas.Add(area)) { changed = true; continue; }
            if (a.slotId != area) { a.slotId = area; changed = true; }
            if (a.attempts < 0) { a.attempts = 0; changed = true; }
            if (a.firstTrySolved && a.attempts == 0) { a.firstTrySolved = false; changed = true; }
            keptAttempts.Add(a);
        }
        slotAttempts = keptAttempts;

        if (placementAttempts < 0) { placementAttempts = 0; changed = true; }
        if (wrongPlacements < 0) { wrongPlacements = 0; changed = true; }
        if (wrongPlacements > placementAttempts) { wrongPlacements = placementAttempts; changed = true; }
        if (checkCorrectCount < -1) { checkCorrectCount = -1; changed = true; }
        if (checkCorrectCount > 4) { checkCorrectCount = 4; changed = true; }
        if (checkSubmitted && checkCorrectCount < 0) { checkSubmitted = false; changed = true; }
        if (!checkSubmitted && (checkCorrectCount != -1 || checkWrongProfileIds.Count > 0 || checkSettledExp != -1))
        { checkCorrectCount = -1; checkWrongProfileIds.Clear(); checkSettledExp = -1; changed = true; }
        if (checkSettledExp < -1) { checkSettledExp = -1; changed = true; }
        if (!checkSubmitted && checkCount != 0) { checkCount = 0; changed = true; }
        if (checkSubmitted && checkCount < 1) { checkCount = 1; changed = true; }
        if (caseRecord == null) { caseRecord = new Level1CaseRecord(); changed = true; }
        if (setAsideCase == null) { setAsideCase = new Level1CaseRecord(); changed = true; }
        if (clock == null) { clock = new Level1ClockRecord(); changed = true; }
        changed |= RepairCase(caseRecord);
        changed |= RepairCase(setAsideCase);
        changed |= RepairClock(clock);
        if (consultation == null) { consultation = new Level1ConsultationRecord(); changed = true; }
        changed |= consultation.Repair();
        return changed;
    }

    /// <summary>Case: clean ids, one value per field key (first kept), known folder place, sane audit counters.</summary>
    private static bool RepairCase(Level1CaseRecord c)
    {
        bool changed = false;
        if (c.caseId != null && c.caseId.Trim() != c.caseId) { c.caseId = c.caseId.Trim(); changed = true; }
        if (c.folder != Level1CaseRecord.FolderOnDesk && c.folder != Level1CaseRecord.FolderStored) { c.folder = Level1CaseRecord.FolderOnDesk; changed = true; }
        c.reviewedDocuments = CleanIds(c.reviewedDocuments, ref changed);
        if (c.fields == null) { c.fields = new List<Level1CaseField>(); changed = true; }
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<Level1CaseField>(c.fields.Count);
        foreach (Level1CaseField f in c.fields)
        {
            string key = f?.key?.Trim();
            if (string.IsNullOrEmpty(key) || f.value == null || !keys.Add(key)) { changed = true; continue; }
            if (f.key != key) { f.key = key; changed = true; }
            kept.Add(f);
        }
        c.fields = kept;
        if (c.auditAttempts < 0) { c.auditAttempts = 0; changed = true; }
        if (c.auditMistakes < 0) { c.auditMistakes = 0; changed = true; }
        if (c.failedAudits < 0) { c.failedAudits = 0; changed = true; }
        if (c.failedAudits > c.auditAttempts) { c.failedAudits = c.auditAttempts; changed = true; }
        if (c.failedAudits == 0 && !string.IsNullOrEmpty(c.lastFailedAnswers)) { c.lastFailedAnswers = null; changed = true; }
        return changed;
    }

    /// <summary>
    /// Clock: day >= 1, finite non-negative times, an announcement needs a
    /// deadline, a hand-in day within the days played, extension seconds within
    /// one appointment extension. Deadlines themselves are kept exactly as saved.
    /// </summary>
    private static bool RepairClock(Level1ClockRecord k)
    {
        bool changed = false;
        if (k.day < 1) { k.day = 1; changed = true; }
        if (double.IsNaN(k.gameSeconds) || double.IsInfinity(k.gameSeconds) || k.gameSeconds < 0) { k.gameSeconds = 0; changed = true; }
        if (float.IsNaN(k.activeSeconds) || float.IsInfinity(k.activeSeconds) || k.activeSeconds < 0f) { k.activeSeconds = 0f; changed = true; }
        if (float.IsNaN(k.sideQuestSeconds) || float.IsInfinity(k.sideQuestSeconds) || k.sideQuestSeconds < -1f) { k.sideQuestSeconds = -1f; changed = true; }
        if (float.IsNaN(k.allowance) || float.IsInfinity(k.allowance) || k.allowance < 0f) { k.allowance = 0f; changed = true; }
        if (k.deadlineSeconds < 0) { k.deadlineSeconds = 0; changed = true; }
        if (k.deadlineFirstDay < 0) { k.deadlineFirstDay = 0; changed = true; }
        if (!k.hasDeadline && (k.announced || k.deadlineSeconds != 0 || k.deadlineFirstDay != 0))
        {
            k.announced = false;
            k.deadlineSeconds = 0;
            k.deadlineFirstDay = 0;
            changed = true;
        }
        if (double.IsNaN(k.submittedAt) || double.IsInfinity(k.submittedAt) || k.submittedAt < -1) { k.submittedAt = -1; changed = true; }
        if (k.submittedDay < 0 || k.submittedDay > k.day) { k.submittedDay = 0; k.submittedAt = -1; changed = true; }
        if (k.extensionSeconds < 0) { k.extensionSeconds = 0; changed = true; }
        if (k.extensionSeconds > Level1ClockRecord.MaxExtensionSeconds) { k.extensionSeconds = Level1ClockRecord.MaxExtensionSeconds; changed = true; }
        return changed;
    }

    private static List<string> CleanIds(List<string> ids, ref bool changed)
    {
        if (ids == null) { changed = true; return new List<string>(); }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<string>(ids.Count);
        foreach (string id in ids)
        {
            string trimmed = id?.Trim();
            if (string.IsNullOrEmpty(trimmed) || !seen.Add(trimmed)) { changed = true; continue; }
            if (trimmed != id) changed = true;
            kept.Add(trimmed);
        }
        return kept;
    }
}

/// <summary>One applied campaign grant or repair (T08/T09): what changed, by how much, and why.</summary>
[Serializable]
public class CampaignLedgerEntry
{
    public string claimId = "";
    /// <summary>"trust", "coins" or "repair".</summary>
    public string kind = "";
    public int amount;
    public int balanceAfter;
    public string reason = "";
}

/// <summary>
/// One installed piece of office decor (T13): the product id and the display
/// anchor it was installed at when bought. arrived records that the office
/// has shown its arrival (presentation only; never a reward or a fact).
/// </summary>
[Serializable]
public class DecorPlacement
{
    public string itemId = "";
    public string anchorId = "";
    public bool arrived;
}

/// <summary>One inventory slot (T10): a stable item id and its quantity. Empty = id "" and 0.</summary>
[Serializable]
public class InventorySlot
{
    public string itemId = "";
    public int quantity;

    public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity <= 0;

    public void Clear()
    {
        itemId = "";
        quantity = 0;
    }

    /// <summary>Trimmed id, no negative quantity, and an id only with a quantity. True when anything changed.</summary>
    public bool Repair()
    {
        bool changed = false;
        if (itemId == null) { itemId = ""; changed = true; }
        string id = itemId.Trim();
        if (id != itemId) { itemId = id; changed = true; }
        if (quantity < 0) { quantity = 0; changed = true; }
        if (itemId.Length > 0 && quantity == 0) { itemId = ""; changed = true; }
        if (itemId.Length == 0 && quantity != 0) { quantity = 0; changed = true; }
        return changed;
    }
}

/// <summary>An item held outside the slots (the old Desk Drawer, T10): save-repair overflow only since the drawer left play; Normalize puts it back in a slot.</summary>
[Serializable]
public class InventoryDrawerEntry
{
    public string itemId = "";
    public int quantity;
}

/// <summary>A keepsake on an office display anchor (T11): owned, shown in the world, not carried.</summary>
[Serializable]
public class InventoryDisplay
{
    public string anchorId = "";
    public string itemId = "";
}

/// <summary>
/// A display-only copy of acquired paper/player work, retained for reading
/// across chapters. No scene references, answer keys or transferable values.
/// </summary>
[Serializable]
public class InventoryReferencePage
{
    public string id = "";
    public string title = "";
    public List<string> lines = new List<string>();
}

/// <summary>Owned item provenance, container identities and acquired read-only reference pages.</summary>
[Serializable]
public class InventoryItemRecord
{
    public string itemId = "";
    /// <summary>How it was acquired ("receptionist", "desk", "plaza", "auditor").</summary>
    public string source = "";
    public string originalDocumentId = "";
    public string workingDocumentId = "";
    public int revision;
    public List<string> contents = new List<string>();
    public List<string> factIds = new List<string>();
    // Display-only copies of acquired records/player work. No correctValue, rule key or transfer action.
    public List<InventoryReferencePage> referencePages = new List<InventoryReferencePage>();
}

/// <summary>
/// The campaign inventory (T10): a hotbar (the HUD icon row, 3 slots,
/// upgradable to 5), a bag (5 slots, upgradable to 12), a "hand" that only
/// holds an item while "Make room" is open, and the drawer list (save-repair
/// overflow only: the Desk Drawer was removed from play on 2026-10-25).
/// Slots hold stable item ids and quantities; container items (Profile
/// Binder, Case Folder) list what they hold in their item record. Repair()
/// is structural (no catalogue); InventoryRules.Normalize applies the item
/// catalogue (unknown ids, stack limits, required items).
/// </summary>
[Serializable]
public class CampaignInventoryRecord
{
    /// <summary>2 (T30): every Bag capacity is one slot larger (base 6, Satchel 9, Large Satchel 13).</summary>
    public const int CurrentVersion = 2;
    /// <summary>The version that added the extra Bag slot; older records get it once on repair.</summary>
    public const int ExtraBagSlotVersion = 2;
    public const int StartHotbar = 3;
    public const int MaxHotbar = 5;
    public const int StartBag = 6;
    public const int MaxBag = 13;
    /// <summary>How many recent command action ids are kept for idempotency.</summary>
    public const int MaxActionIds = 64;

    public int version = CurrentVersion;
    public int hotbarCapacity = StartHotbar;
    public int bagCapacity = StartBag;
    public List<InventorySlot> hotbar = Slots(StartHotbar);
    public List<InventorySlot> bag = Slots(StartBag);
    /// <summary>The item held while "Make room" is open (empty otherwise).</summary>
    public InventorySlot hand = new InventorySlot();
    /// <summary>"pickup" (new, can go back to its world source) or "displaced" (already owned).</summary>
    public string handSource = "";
    public List<InventoryDrawerEntry> drawer = new List<InventoryDrawerEntry>();
    /// <summary>Keepsakes shown on office display anchors (T11); one item per anchor, owned but not carried.</summary>
    public List<InventoryDisplay> displays = new List<InventoryDisplay>();
    /// <summary>T12 bookmark pins: up to three owned record ids kept at the top of the Bag (display preference only).</summary>
    public List<string> pins = new List<string>();
    public const int MaxPins = 3;
    public List<InventoryItemRecord> items = new List<InventoryItemRecord>();
    public List<string> appliedActionIds = new List<string>();
    /// <summary>Journal facts the player has already seen (new ones are marked).</summary>
    public List<string> journalSeenFactIds = new List<string>();
    /// <summary>The inventory cursor ("hotbar:0", "bag:3"), restored when the inventory reopens.</summary>
    public string selectedSlot = "";
    public bool bagIconUsed;
    public bool bagLessonShown;

    public static List<InventorySlot> Slots(int count)
    {
        var slots = new List<InventorySlot>(count);
        for (int i = 0; i < count; i++) slots.Add(new InventorySlot());
        return slots;
    }

    /// <summary>
    /// Capacities clamp to 3-5 / 6-13, slot lists match their capacity (items
    /// in slots beyond it move to a free slot or the drawer, never lost), no
    /// negative quantities, one drawer entry and one record per item, clean
    /// id lists. T30: a record from before version 2 gets its one extra Bag
    /// slot here exactly once (5 -> 6, 8 -> 9, 12 -> 13; items stay in their
    /// slots), then is version 2. True when anything changed.
    /// </summary>
    public bool Repair()
    {
        bool changed = false;
        if (version <= 0) { version = CurrentVersion; changed = true; }
        if (version < ExtraBagSlotVersion)
        {
            bagCapacity += 1;
            version = ExtraBagSlotVersion;
            changed = true;
        }
        int hotbarCap = Math.Max(StartHotbar, Math.Min(MaxHotbar, hotbarCapacity));
        if (hotbarCap != hotbarCapacity) { hotbarCapacity = hotbarCap; changed = true; }
        int bagCap = Math.Max(StartBag, Math.Min(MaxBag, bagCapacity));
        if (bagCap != bagCapacity) { bagCapacity = bagCap; changed = true; }
        if (hotbar == null) { hotbar = new List<InventorySlot>(); changed = true; }
        if (bag == null) { bag = new List<InventorySlot>(); changed = true; }
        if (hand == null) { hand = new InventorySlot(); changed = true; }
        if (handSource == null) { handSource = ""; changed = true; }
        if (drawer == null) { drawer = new List<InventoryDrawerEntry>(); changed = true; }
        if (displays == null) { displays = new List<InventoryDisplay>(); changed = true; }
        if (pins == null) { pins = new List<string>(); changed = true; }
        if (items == null) { items = new List<InventoryItemRecord>(); changed = true; }
        if (appliedActionIds == null) { appliedActionIds = new List<string>(); changed = true; }
        if (journalSeenFactIds == null) { journalSeenFactIds = new List<string>(); changed = true; }
        if (selectedSlot == null) { selectedSlot = ""; changed = true; }

        changed |= RepairSlots(hotbar);
        changed |= RepairSlots(bag);
        changed |= hand.Repair();
        if (hand.IsEmpty && handSource.Length > 0) { handSource = ""; changed = true; }

        var overflow = new List<InventorySlot>();
        changed |= FitSlots(hotbar, hotbarCapacity, overflow);
        changed |= FitSlots(bag, bagCapacity, overflow);
        foreach (InventorySlot moved in overflow)
        {
            InventorySlot free = bag.Find(s => s.IsEmpty) ?? hotbar.Find(s => s.IsEmpty);
            if (free != null) { free.itemId = moved.itemId; free.quantity = moved.quantity; }
            else AddToDrawer(moved.itemId, moved.quantity);
        }

        var drawerById = new Dictionary<string, InventoryDrawerEntry>(StringComparer.Ordinal);
        var keptDrawer = new List<InventoryDrawerEntry>(drawer.Count);
        foreach (InventoryDrawerEntry entry in drawer)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemId) || entry.quantity <= 0) { changed = true; continue; }
            string id = entry.itemId.Trim();
            if (id != entry.itemId) { entry.itemId = id; changed = true; }
            if (drawerById.TryGetValue(id, out InventoryDrawerEntry first))
            {
                first.quantity = (int)Math.Min(int.MaxValue, (long)first.quantity + entry.quantity);
                changed = true;
                continue;
            }
            drawerById[id] = entry;
            keptDrawer.Add(entry);
        }
        drawer = keptDrawer;

        // One keepsake per anchor and one anchor per keepsake.
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var shown = new HashSet<string>(StringComparer.Ordinal);
        var keptDisplays = new List<InventoryDisplay>(displays.Count);
        foreach (InventoryDisplay display in displays)
        {
            if (display == null || string.IsNullOrWhiteSpace(display.anchorId) || string.IsNullOrWhiteSpace(display.itemId)) { changed = true; continue; }
            string anchor = display.anchorId.Trim(), item = display.itemId.Trim();
            if (anchor != display.anchorId || item != display.itemId) { display.anchorId = anchor; display.itemId = item; changed = true; }
            if (!anchors.Add(anchor) || !shown.Add(item)) { changed = true; continue; }
            keptDisplays.Add(display);
        }
        displays = keptDisplays;

        var recordIds = new HashSet<string>(StringComparer.Ordinal);
        var keptItems = new List<InventoryItemRecord>(items.Count);
        foreach (InventoryItemRecord item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId)) { changed = true; continue; }
            string id = item.itemId.Trim();
            if (id != item.itemId) { item.itemId = id; changed = true; }
            if (!recordIds.Add(id)) { changed = true; continue; }
            if (item.source == null) { item.source = ""; changed = true; }
            if (item.originalDocumentId == null) { item.originalDocumentId = ""; changed = true; }
            if (item.workingDocumentId == null) { item.workingDocumentId = ""; changed = true; }
            if (item.revision < 0) { item.revision = 0; changed = true; }
            if (item.contents == null) { item.contents = new List<string>(); changed = true; }
            if (item.factIds == null) { item.factIds = new List<string>(); changed = true; }
            item.contents = Clean(item.contents, ref changed);
            item.factIds = Clean(item.factIds, ref changed);
            keptItems.Add(item);
        }
        items = keptItems;

        appliedActionIds = Clean(appliedActionIds, ref changed);
        if (appliedActionIds.Count > MaxActionIds)
        {
            appliedActionIds.RemoveRange(0, appliedActionIds.Count - MaxActionIds);
            changed = true;
        }
        journalSeenFactIds = Clean(journalSeenFactIds, ref changed);
        pins = Clean(pins, ref changed);
        if (pins.Count > MaxPins) { pins.RemoveRange(MaxPins, pins.Count - MaxPins); changed = true; }
        string selected = selectedSlot.Trim();
        if (selected != selectedSlot) { selectedSlot = selected; changed = true; }
        return changed;
    }

    /// <summary>Adds to the drawer (one entry per item, no overflow past int.MaxValue).</summary>
    public void AddToDrawer(string itemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0) return;
        if (drawer == null) drawer = new List<InventoryDrawerEntry>();
        InventoryDrawerEntry entry = drawer.Find(e => e != null && e.itemId == itemId);
        if (entry == null) drawer.Add(new InventoryDrawerEntry { itemId = itemId, quantity = quantity });
        else entry.quantity = (int)Math.Min(int.MaxValue, (long)entry.quantity + quantity);
    }

    private static bool RepairSlots(List<InventorySlot> slots)
    {
        bool changed = false;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) { slots[i] = new InventorySlot(); changed = true; }
            changed |= slots[i].Repair();
        }
        return changed;
    }

    /// <summary>Pads or trims a slot list to its capacity; items in trimmed slots go to overflow.</summary>
    private static bool FitSlots(List<InventorySlot> slots, int capacity, List<InventorySlot> overflow)
    {
        bool changed = false;
        while (slots.Count < capacity) { slots.Add(new InventorySlot()); changed = true; }
        for (int i = slots.Count - 1; i >= capacity; i--)
        {
            if (!slots[i].IsEmpty) overflow.Insert(0, slots[i]);
            slots.RemoveAt(i);
            changed = true;
        }
        return changed;
    }

    private static List<string> Clean(List<string> ids, ref bool changed)
    {
        var kept = new List<string>(ids.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string raw in ids)
        {
            if (string.IsNullOrWhiteSpace(raw)) { changed = true; continue; }
            string id = raw.Trim();
            if (id != raw) changed = true;
            if (!seen.Add(id)) { changed = true; continue; }
            kept.Add(id);
        }
        return kept;
    }
}

/// <summary>
/// The first story result of Level 1's case (T07): recorded once, when the
/// corrected classification quest and Marie's approved case are both done,
/// in the same write as its mastery result. Replays and later runs never
/// rewrite it. Plain values only: the frozen first-check tier (T06), the
/// approved case's identity, and the mastery numbers of that run.
/// </summary>
[Serializable]
public class Level1CaseResult
{
    public const string NextFollowThrough = "followThrough";

    public bool recorded;
    /// <summary>Stable scoped action id (CaseResultRules.ActionId); also kept in the campaign's rewardClaims.</summary>
    public string actionId = "";
    public string caseId = "";
    public string caseFingerprint = "";
    public bool questCorrected;
    public bool caseApproved;
    /// <summary>Right answers on the board's first complete check (0-4), -1 when unknown.</summary>
    public int firstCheckCorrect = -1;
    /// <summary>The frozen first-check tier (BadgeTier value: 0 none, 1 Bronze, 2 Silver, 3 Gold).</summary>
    public int tier;
    public int exp;
    public int maxExp;
    public int scoreHearts;
    public int maxScoreHearts;
    public float runSeconds;
    /// <summary>The story's next required step after this result ("followThrough").</summary>
    public string nextStep = "";

    /// <summary>Unrecorded results are empty; a recorded one keeps sane values. True when anything changed.</summary>
    public bool Repair()
    {
        bool changed = false;
        if (actionId == null) { actionId = ""; changed = true; }
        if (caseId == null) { caseId = ""; changed = true; }
        if (caseFingerprint == null) { caseFingerprint = ""; changed = true; }
        if (nextStep == null) { nextStep = ""; changed = true; }
        if (!recorded)
        {
            if (actionId.Length > 0 || caseId.Length > 0 || questCorrected || caseApproved || exp != 0 || tier != 0)
            {
                actionId = caseId = caseFingerprint = nextStep = "";
                questCorrected = caseApproved = false;
                firstCheckCorrect = -1; tier = exp = maxExp = scoreHearts = maxScoreHearts = 0; runSeconds = 0f;
                changed = true;
            }
            return changed;
        }
        if (firstCheckCorrect < -1 || firstCheckCorrect > 4) { firstCheckCorrect = Math.Max(-1, Math.Min(4, firstCheckCorrect)); changed = true; }
        if (tier < 0 || tier > 3) { tier = Math.Max(0, Math.Min(3, tier)); changed = true; }
        if (exp < 0) { exp = 0; changed = true; }
        if (maxExp < 0) { maxExp = 0; changed = true; }
        if (scoreHearts < 0) { scoreHearts = 0; changed = true; }
        if (maxScoreHearts < 0) { maxScoreHearts = 0; changed = true; }
        if (runSeconds < 0f || float.IsNaN(runSeconds)) { runSeconds = 0f; changed = true; }
        return changed;
    }
}

/// <summary>
/// The updated story campaign (version 5). Separate from the mastery records
/// (EXP, score hearts, badges): publicTrust is social confidence 0..100, coins
/// are the office budget. Plain ids and quantities only. Starting values come
/// from CreateFresh, the one factory used by every New Game.
/// </summary>
[Serializable]
public class CampaignRecord
{
    /// <summary>T04: pending ending and result, saved with service; empty in old completed campaigns.</summary>
    public ChapterEndingRecord ending = new ChapterEndingRecord();
    public const int StartingTrust = 40;
    public const int MaxTrust = 100;
    public const string FirstChapterId = "Level_01";

    public string campaignId;
    /// <summary>When New Game committed (UTC, round-trip format). Informational.</summary>
    public string createdUtc;
    /// <summary>The story chapter the campaign is in (Level 1 at the start).</summary>
    public string currentChapterId;
    public List<string> completedChapterIds = new List<string>();
    public int publicTrust;
    public int coins;
    /// <summary>Once-only reward/action claims (campaign-scoped ids).</summary>
    public List<string> rewardClaims = new List<string>();
    public List<CampaignItemStack> items = new List<CampaignItemStack>();
    public List<string> ownedUpgradeIds = new List<string>();
    /// <summary>The unfinished Level 1 story run's field progress (T04); empty when there is none.</summary>
    public Level1FieldCheckpoint levelOne = new Level1FieldCheckpoint();
    /// <summary>Level 1's first case result (T07); recorded once, never rewritten.</summary>
    public Level1CaseResult levelOneCaseResult = new Level1CaseResult();
    /// <summary>The Level 1 finale/result card is still waiting to be presented after Marie receives her copy (T08).</summary>
    public bool levelOneEndFlowPending;
    /// <summary>Completed required story activities ("Level_01/field-assignment", "Level_01/marie-follow-through"), T08.</summary>
    public List<string> completedActivityIds = new List<string>();
    /// <summary>Every applied trust/coin grant and balance repair, with its reason (T08/T09).</summary>
    public List<CampaignLedgerEntry> ledger = new List<CampaignLedgerEntry>();
    /// <summary>Hotbar, bag and journal state (T10). The older items list stays for save compatibility.</summary>
    public CampaignInventoryRecord inventory = new CampaignInventoryRecord();
    /// <summary>T12: the Supply Counter was introduced (opened once in the story); the shop is open from then on.</summary>
    public bool shopIntroduced;
    /// <summary>T13: office decor installed at its display anchors (one entry per owned decor product).</summary>
    public List<DecorPlacement> decor = new List<DecorPlacement>();
    /// <summary>T19: Rosa's explicit promise and its pending, once-only supporting-record consequence.</summary>
    public RosaEthicsRecord rosaEthics = new RosaEthicsRecord();
    /// <summary>Level 3 (T02): the chapter phase and both case subrecords of the unfinished story run; empty when there is none.</summary>
    public Level3ChapterCheckpoint levelThree = new Level3ChapterCheckpoint();

    public bool IsSet => !string.IsNullOrWhiteSpace(campaignId);

    public bool IsChapterCompleted(string chapterId) =>
        !string.IsNullOrWhiteSpace(chapterId) && completedChapterIds != null && completedChapterIds.Contains(chapterId.Trim());

    /// <summary>Records the story's first completion of a chapter (story runs only). True when it is new.</summary>
    public bool MarkChapterCompleted(string chapterId)
    {
        if (!IsSet || string.IsNullOrWhiteSpace(chapterId) || IsChapterCompleted(chapterId)) return false;
        if (completedChapterIds == null) completedChapterIds = new List<string>();
        completedChapterIds.Add(chapterId.Trim());
        return true;
    }

    /// <summary>A brand-new campaign: trust 40/100, no coins, claims, items, upgrades or completed chapters; Level 1 current.</summary>
    public static CampaignRecord CreateFresh(string campaignId, string createdUtc) => new CampaignRecord
    {
        campaignId = campaignId,
        createdUtc = createdUtc,
        currentChapterId = FirstChapterId,
        publicTrust = StartingTrust,
        coins = 0
    };
}

[Serializable]
public sealed class ChapterEndingRecord
{
    public string chapterId = "";
    public string variant = "";
    public bool consumed;
    public string resultJson = "";
}

[Serializable]
public sealed class RosaEthicsRecord
{
    public string caseId = "";
    public string commitmentId = "";
    public string choiceId = "";
    public string choiceText = "";
    public bool committed;
    public bool followThroughRequired;
    public bool followThroughDone;
    /// <summary>Actual clamped trust delta from an unsupported promise, reversed exactly at follow-through.</summary>
    public int promiseTrustDelta;
    /// <summary>Stable activity IDs of T19 consequences recorded in the campaign ledger.</summary>
    public List<string> consequenceIds = new List<string>();
}

/// <summary>
/// Level 3 chapter snapshot (Level3 T02), inside the v5 campaign like Level
/// 1's field checkpoint: the saved chapter phase, the active case and both
/// case subrecords from the start, so the company case never overwrites
/// Bea's. Stable ids and the player's own progress only: never scene
/// objects, camera poses, content answers or expected results (those are
/// read from the case content after load). Level3ChapterRules owns the
/// phase order and every transition; later tickets add their fields here.
/// </summary>
[Serializable]
public class Level3ChapterCheckpoint
{
    /// <summary>
    /// 2 = the 3 November 2026 revision (fresh entry skips the retired budget
    /// practice). 1 = an earlier snapshot, continued once by
    /// Level3ChapterRules.TryContinueLegacy before it is used or written.
    /// </summary>
    public const int CurrentVersion = 2;
    public const int LegacyVersion = 1;

    public int version;
    public int writes;
    /// <summary>The case-content revision the run was made against (FIXTURE_CONTRACT revision).</summary>
    public string contentRevision = "";
    /// <summary>A Level3Phase name ("Reception", "ToolPractice", ...).</summary>
    public string phase = "";
    public string activeCaseId = "";
    /// <summary>Durable chapter actions already applied (a repeated action changes nothing).</summary>
    public List<string> actionIds = new List<string>();
    /// <summary>Chapter facts reached ("reception-assigned"), never rewards.</summary>
    public List<string> milestones = new List<string>();
    /// <summary>Bea's case and the company's, both present from the first write.</summary>
    public List<Level3CaseProgress> cases = new List<Level3CaseProgress>();
    /// <summary>Calculator tapes the player attached (T03: the two practice tapes), one per destination.</summary>
    public List<Level3TapeRecord> tapes = new List<Level3TapeRecord>();
    /// <summary>T05: Bea's worksheet draft (the player's own placements, reasons and link; never an answer).</summary>
    public Level3WorksheetDraft worksheet = new Level3WorksheetDraft();
    /// <summary>T06: the worksheet's frozen first check and its latest check (decision ids and the player's own keys only).</summary>
    public Level3WorksheetResult worksheetCheck = new Level3WorksheetResult();
    /// <summary>Revision: the calculator's first-use demonstration was given (or the player already learned it in the old practice); never a reward.</summary>
    public bool calculatorTaught;

    public Level3TapeRecord Tape(string destinationId)
    {
        if (tapes == null || string.IsNullOrEmpty(destinationId)) return null;
        foreach (Level3TapeRecord t in tapes)
            if (t != null && t.destinationId == destinationId) return t;
        return null;
    }

    /// <summary>Anything to resume (a version was written by the checkpointer).</summary>
    public bool IsSet => version > 0;

    public Level3CaseProgress Case(string caseId)
    {
        if (cases == null || string.IsNullOrEmpty(caseId)) return null;
        foreach (Level3CaseProgress c in cases)
            if (c != null && c.caseId == caseId) return c;
        return null;
    }

    /// <summary>A deep copy (a command works on a copy; the live record changes only after its write).</summary>
    public Level3ChapterCheckpoint Copy()
    {
        var copy = new Level3ChapterCheckpoint
        {
            version = version,
            writes = writes,
            contentRevision = contentRevision,
            phase = phase,
            activeCaseId = activeCaseId,
            actionIds = actionIds != null ? new List<string>(actionIds) : new List<string>(),
            milestones = milestones != null ? new List<string>(milestones) : new List<string>(),
            calculatorTaught = calculatorTaught
        };
        if (cases != null)
            foreach (Level3CaseProgress c in cases)
                if (c != null) copy.cases.Add(c.Copy());
        if (tapes != null)
            foreach (Level3TapeRecord t in tapes)
                if (t != null) copy.tapes.Add(t.Copy());
        copy.worksheet = worksheet != null ? worksheet.Copy() : new Level3WorksheetDraft();
        copy.worksheetCheck = worksheetCheck != null ? worksheetCheck.Copy() : new Level3WorksheetResult();
        return copy;
    }

    /// <summary>
    /// Structural repair only: no null lists or strings, trimmed unique ids,
    /// one subrecord per case id, non-negative counters. An unknown phase or
    /// a newer version is left for Level3ChapterRules to refuse on restore.
    /// </summary>
    public bool Repair()
    {
        bool changed = false;
        if (version < 0) { version = 0; changed = true; }
        if (writes < 0) { writes = 0; changed = true; }
        if (contentRevision == null) { contentRevision = ""; changed = true; }
        if (phase == null) { phase = ""; changed = true; }
        if (activeCaseId == null) { activeCaseId = ""; changed = true; }
        actionIds = CleanIds(actionIds, ref changed);
        milestones = CleanIds(milestones, ref changed);
        if (cases == null) { cases = new List<Level3CaseProgress>(); changed = true; }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<Level3CaseProgress>(cases.Count);
        foreach (Level3CaseProgress c in cases)
        {
            string id = c?.caseId?.Trim();
            if (string.IsNullOrEmpty(id) || !seen.Add(id)) { changed = true; continue; }
            if (c.caseId != id) { c.caseId = id; changed = true; }
            changed |= c.Repair();
            kept.Add(c);
        }
        cases = kept;
        if (tapes == null) { tapes = new List<Level3TapeRecord>(); changed = true; }
        var destinations = new HashSet<string>(StringComparer.Ordinal);
        int before = tapes.Count;
        tapes.RemoveAll(t => t == null || string.IsNullOrWhiteSpace(t.destinationId) || !destinations.Add(t.destinationId.Trim()));
        if (tapes.Count != before) changed = true;
        foreach (Level3TapeRecord t in tapes) changed |= t.Repair();
        if (worksheet == null) { worksheet = new Level3WorksheetDraft(); changed = true; }
        changed |= worksheet.Repair();
        if (worksheetCheck == null) { worksheetCheck = new Level3WorksheetResult(); changed = true; }
        changed |= worksheetCheck.Repair();
        return changed;
    }

    internal static List<string> CleanIds(List<string> ids, ref bool changed)
    {
        if (ids == null) { changed = true; return new List<string>(); }
        var clean = new List<string>(ids.Count);
        foreach (string id in ids)
        {
            string trimmed = id?.Trim();
            if (string.IsNullOrEmpty(trimmed) || clean.Contains(trimmed)) { changed = true; continue; }
            if (trimmed != id) changed = true;
            clean.Add(trimmed);
        }
        return clean;
    }
}

/// <summary>
/// One number or operator on a Level 3 calculator tape (T03): the value as an
/// invariant decimal string, never a float. A number keeps where it came
/// from (a source id and its printed label) or is marked as typed by hand.
/// </summary>
[Serializable]
public class Level3CalcToken
{
    public const string Number = "number";
    public const string Operator = "op";

    public string kind = "";
    /// <summary>Invariant decimal ("100.00", "0.2") for a number; "" for an operator.</summary>
    public string value = "";
    /// <summary>"+", "-", "*" or "/" for an operator.</summary>
    public string op = "";
    public string sourceId = "";
    public string label = "";
    /// <summary>Typed on the keys (no source paper).</summary>
    public bool manual;
    /// <summary>Entered with the % key (value is already the fraction: 20% = 0.2).</summary>
    public bool percent;

    public bool IsNumber => kind == Number;
    public bool IsOperator => kind == Operator;

    public Level3CalcToken Copy() => new Level3CalcToken
    {
        kind = kind, value = value, op = op, sourceId = sourceId, label = label, manual = manual, percent = percent
    };
}

/// <summary>
/// T05: Bea's worksheet as the player has drafted it. Strips keep the row and
/// source they were lifted from and the printed amount (copied, not typed);
/// section "" = lifted but not placed yet. Notes keep the chosen reason and
/// related row. The undo stack holds the previous value of each edit. Nothing
/// here says whether a choice is right: T06's Check reads it against content.
/// </summary>
[Serializable]
public class Level3WorksheetDraft
{
    public const int MaxUndo = 24;

    /// <summary>Counts real edits (placement, reason, link, undo); reading or selecting is not one.</summary>
    public int revision;
    public List<Level3StripRecord> strips = new List<Level3StripRecord>();
    public List<Level3NoteRecord> notes = new List<Level3NoteRecord>();
    public List<Level3WorksheetUndo> undo = new List<Level3WorksheetUndo>();
    /// <summary>The source tab and row last worked on (restored on reopen).</summary>
    public string selectedSource = "";
    public string selectedRow = "";
    /// <summary>A section's membership changed since its subtotal tape was attached (T06 clears these).</summary>
    public bool incomeDirty;
    public bool expensesDirty;
    /// <summary>Every strip placed, both reasons and the link chosen at least once (raises WorksheetDrafted once).</summary>
    public bool drafted;

    public Level3StripRecord Strip(string rowId)
    {
        if (strips == null || string.IsNullOrEmpty(rowId)) return null;
        foreach (Level3StripRecord strip in strips)
            if (strip != null && strip.rowId == rowId) return strip;
        return null;
    }

    public Level3NoteRecord Note(string noteId)
    {
        if (notes == null || string.IsNullOrEmpty(noteId)) return null;
        foreach (Level3NoteRecord note in notes)
            if (note != null && note.noteId == noteId) return note;
        return null;
    }

    public Level3WorksheetDraft Copy()
    {
        var copy = new Level3WorksheetDraft
        {
            revision = revision, selectedSource = selectedSource, selectedRow = selectedRow,
            incomeDirty = incomeDirty, expensesDirty = expensesDirty, drafted = drafted
        };
        if (strips != null) foreach (Level3StripRecord s in strips) if (s != null) copy.strips.Add(s.Copy());
        if (notes != null) foreach (Level3NoteRecord n in notes) if (n != null) copy.notes.Add(n.Copy());
        if (undo != null) foreach (Level3WorksheetUndo u in undo) if (u != null) copy.undo.Add(u.Copy());
        return copy;
    }

    public bool Repair()
    {
        bool changed = false;
        if (revision < 0) { revision = 0; changed = true; }
        if (selectedSource == null) { selectedSource = ""; changed = true; }
        if (selectedRow == null) { selectedRow = ""; changed = true; }
        if (strips == null) { strips = new List<Level3StripRecord>(); changed = true; }
        if (notes == null) { notes = new List<Level3NoteRecord>(); changed = true; }
        if (undo == null) { undo = new List<Level3WorksheetUndo>(); changed = true; }
        var rows = new HashSet<string>(StringComparer.Ordinal);
        changed |= strips.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.rowId) || !rows.Add(x.rowId)) > 0;
        var noteIds = new HashSet<string>(StringComparer.Ordinal);
        changed |= notes.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.noteId) || !noteIds.Add(x.noteId)) > 0;
        changed |= undo.RemoveAll(x => x == null) > 0;
        if (undo.Count > MaxUndo) { undo.RemoveRange(0, undo.Count - MaxUndo); changed = true; }
        foreach (Level3StripRecord strip in strips)
        {
            if (strip.sourceId == null) { strip.sourceId = ""; changed = true; }
            if (strip.amount == null) { strip.amount = ""; changed = true; }
            if (strip.section == null) { strip.section = ""; changed = true; }
        }
        foreach (Level3NoteRecord note in notes)
        {
            if (note.reason == null) { note.reason = ""; changed = true; }
            if (note.related == null) { note.related = ""; changed = true; }
        }
        return changed;
    }
}

/// <summary>One worksheet strip lifted from an original row (T05).</summary>
[Serializable]
public class Level3StripRecord
{
    public string rowId = "";
    public string sourceId = "";
    /// <summary>The printed amount, copied exactly from the source row (decimal text).</summary>
    public string amount = "";
    /// <summary>"income", "expenses", "not-deductible", "supporting", or "" while not placed.</summary>
    public string section = "";

    public Level3StripRecord Copy() => new Level3StripRecord { rowId = rowId, sourceId = sourceId, amount = amount, section = section };
}

/// <summary>One claim note on the worksheet (T05): the reason and related row the player chose.</summary>
[Serializable]
public class Level3NoteRecord
{
    public string noteId = "";
    public string reason = "";
    public string related = "";

    public Level3NoteRecord Copy() => new Level3NoteRecord { noteId = noteId, reason = reason, related = related };
}

/// <summary>
/// T06: Bea's worksheet checks. The FIRST complete check is frozen here (the
/// correct count, the wrong decision ids, tier, XP and the draft revision it
/// read) and never rewritten by a correction. The LAST complete check keeps
/// what it marked and the values it read, so the marks restore on resume and
/// show only while the checked value is unchanged. Decision ids and the
/// player's own keys only: never an expected answer or label.
/// </summary>
[Serializable]
public class Level3WorksheetResult
{
    /// <summary>The assessment the frozen first check was graded under ("L3-worksheet-v1": twelve decisions); "" before any check. Never regraded.</summary>
    public string assessmentVersion = "";
    public bool firstChecked;
    public int firstRevision;
    public int firstCorrect;
    public int firstTotal;
    public List<string> firstWrong = new List<string>();
    /// <summary>BadgeTier as a number: 1 bronze, 2 silver, 3 gold (0 before the first check).</summary>
    public int firstTier;
    public int firstXp;
    /// <summary>Complete checks so far (an incomplete Check is not one and is never saved).</summary>
    public int checks;
    public int lastRevision;
    public List<string> lastWrong = new List<string>();
    /// <summary>"decisionId=value" as the last check read them (the player's own keys).</summary>
    public List<string> lastValues = new List<string>();
    /// <summary>
    /// Revision (T05 reads, T06 writes): machine batches ("earnings", "costs", "questions") a deliberate
    /// check accepted. Their rows are read-only and the next batch opens. Batch ids only, never an answer.
    /// Absent in older saves (none accepted).
    /// </summary>
    public List<string> acceptedBatches = new List<string>();
    /// <summary>
    /// T06 (L3-worksheet-v2): machine batches whose FIRST complete check is frozen into firstWrong /
    /// firstCorrect / firstTotal (once each, never improved by a correction). firstChecked turns true
    /// only when all three are frozen (nine decisions). Absent in older saves (none).
    /// </summary>
    public List<string> checkedBatches = new List<string>();
    /// <summary>T06: the batch the last saved check read (its marks are lastWrong/lastValues); "" before any machine check.</summary>
    public string lastBatch = "";

    public Level3WorksheetResult Copy() => new Level3WorksheetResult
    {
        assessmentVersion = assessmentVersion,
        firstChecked = firstChecked, firstRevision = firstRevision, firstCorrect = firstCorrect, firstTotal = firstTotal,
        firstWrong = firstWrong != null ? new List<string>(firstWrong) : new List<string>(),
        firstTier = firstTier, firstXp = firstXp, checks = checks, lastRevision = lastRevision,
        lastWrong = lastWrong != null ? new List<string>(lastWrong) : new List<string>(),
        lastValues = lastValues != null ? new List<string>(lastValues) : new List<string>(),
        acceptedBatches = acceptedBatches != null ? new List<string>(acceptedBatches) : new List<string>(),
        checkedBatches = checkedBatches != null ? new List<string>(checkedBatches) : new List<string>(),
        lastBatch = lastBatch ?? ""
    };

    public bool Repair()
    {
        bool changed = false;
        if (assessmentVersion == null) { assessmentVersion = ""; changed = true; }
        firstWrong = Level3ChapterCheckpoint.CleanIds(firstWrong, ref changed);
        lastWrong = Level3ChapterCheckpoint.CleanIds(lastWrong, ref changed);
        lastValues = Level3ChapterCheckpoint.CleanIds(lastValues, ref changed);
        acceptedBatches = Level3ChapterCheckpoint.CleanIds(acceptedBatches, ref changed);
        checkedBatches = Level3ChapterCheckpoint.CleanIds(checkedBatches, ref changed);
        if (lastBatch == null) { lastBatch = ""; changed = true; }
        if (firstRevision < 0) { firstRevision = 0; changed = true; }
        if (firstCorrect < 0) { firstCorrect = 0; changed = true; }
        if (firstTotal < 0) { firstTotal = 0; changed = true; }
        if (firstTier < 0 || firstTier > 3) { firstTier = Math.Max(0, Math.Min(3, firstTier)); changed = true; }
        if (firstXp < 0) { firstXp = 0; changed = true; }
        if (checks < 0) { checks = 0; changed = true; }
        if (lastRevision < 0) { lastRevision = 0; changed = true; }
        return changed;
    }
}

/// <summary>The previous value of one worksheet edit (Undo puts it back).</summary>
[Serializable]
public class Level3WorksheetUndo
{
    /// <summary>"extract", "place", "reason" or "related".</summary>
    public string kind = "";
    /// <summary>The strip's row or the note's id.</summary>
    public string target = "";
    public string previous = "";

    public Level3WorksheetUndo Copy() => new Level3WorksheetUndo { kind = kind, target = target, previous = previous };
}

/// <summary>
/// A calculator tape the player attached to a destination (T03 practice
/// slots; later tickets: worksheet and computation fields). The player's own
/// expression and exact result, never the expected answer.
/// </summary>
[Serializable]
public class Level3TapeRecord
{
    public string tapeId = "";
    /// <summary>"PRACTICE" for the protected budget exercise, else the case id.</summary>
    public string caseId = "";
    public string destinationId = "";
    public List<Level3CalcToken> expression = new List<Level3CalcToken>();
    /// <summary>Exact invariant decimal result.</summary>
    public string result = "";
    public string actionId = "";

    public Level3TapeRecord Copy()
    {
        var copy = new Level3TapeRecord { tapeId = tapeId, caseId = caseId, destinationId = destinationId, result = result, actionId = actionId };
        if (expression != null)
            foreach (Level3CalcToken token in expression)
                if (token != null) copy.expression.Add(token.Copy());
        return copy;
    }

    public bool Repair()
    {
        bool changed = false;
        if (tapeId == null) { tapeId = ""; changed = true; }
        if (caseId == null) { caseId = ""; changed = true; }
        if (destinationId != null && destinationId != destinationId.Trim()) { destinationId = destinationId.Trim(); changed = true; }
        if (result == null) { result = ""; changed = true; }
        if (actionId == null) { actionId = ""; changed = true; }
        if (expression == null) { expression = new List<Level3CalcToken>(); changed = true; }
        if (expression.RemoveAll(t => t == null) > 0) changed = true;
        return changed;
    }
}

/// <summary>
/// T07: one case's computation sheet as the player has filled it in. Each
/// total names what the player picked ("worksheet:income",
/// "worksheet:expenses", "paper:B-DRAFT/BDraftTotal") with the amount copied
/// at that moment; ruleId is the rate band the player chose from the table.
/// The working tapes (taxable income, excess, tax on the excess, tax due and,
/// from T08, the remaining payable) are the chapter's tapes. T08 adds the
/// credit step: whether the player explained Bea's certificate (a credit, not
/// an expense) and the certificate amount they linked to Tax credits
/// ("paper:B-WHT/BWithheldCredit"). Nothing here says whether a choice is right.
/// </summary>
[Serializable]
public class Level3ComputationRecord
{
    /// <summary>Counts real changes (a choice or an attached tape); reading the sheet is not one.</summary>
    public int revision;
    public string grossChoice = "";
    public string grossAmount = "";
    public string expensesChoice = "";
    public string expensesAmount = "";
    public string ruleId = "";
    /// <summary>Taxable income and tax due once both had current tapes (raises TaxDueAttached once; structural, never a grade).</summary>
    public bool taxDueNoted;
    /// <summary>T08: the player gave Bea the credit explanation for her certificate (never fills anything in).</summary>
    public bool creditExplained;
    /// <summary>T08: the certificate amount the player linked to Tax credits, copied at that moment ("" = none yet).</summary>
    public string creditChoice = "";
    public string creditAmount = "";
    /// <summary>T08: a credit was linked and the remaining payable first had a current tape (raises RemainingPayableAttached once; never a grade).</summary>
    public bool payableNoted;

    public Level3ComputationRecord Copy() => new Level3ComputationRecord
    {
        revision = revision, grossChoice = grossChoice, grossAmount = grossAmount, expensesChoice = expensesChoice,
        expensesAmount = expensesAmount, ruleId = ruleId, taxDueNoted = taxDueNoted,
        creditExplained = creditExplained, creditChoice = creditChoice, creditAmount = creditAmount, payableNoted = payableNoted
    };

    public bool Repair()
    {
        bool changed = false;
        if (revision < 0) { revision = 0; changed = true; }
        if (grossChoice == null) { grossChoice = ""; changed = true; }
        if (grossAmount == null) { grossAmount = ""; changed = true; }
        if (expensesChoice == null) { expensesChoice = ""; changed = true; }
        if (expensesAmount == null) { expensesAmount = ""; changed = true; }
        if (ruleId == null) { ruleId = ""; changed = true; }
        if (creditChoice == null) { creditChoice = ""; changed = true; }
        if (creditAmount == null) { creditAmount = ""; changed = true; }
        return changed;
    }
}

/// <summary>T09: the stamp on one case's computation sheet and the sheet revision it covers.</summary>
[Serializable]
public class Level3StampRecord
{
    /// <summary>"" (none), "ready" (Ready for computation review) or "hold" (Not Ready).</summary>
    public string type = "";
    /// <summary>The computation revision the stamp covers; any later change to the sheet makes it out of date.</summary>
    public int revision = -1;
    public string actionId = "";

    public bool placed;
    public int page;
    public float x, y, tilt;
    public Level3StampRecord Copy() => new Level3StampRecord
    { type = type, revision = revision, actionId = actionId, placed = placed, page = page, x = x, y = y, tilt = tilt };

    public bool Repair()
    {
        bool changed = false;
        if (type == null) { type = ""; changed = true; }
        if (actionId == null) { actionId = ""; changed = true; }
        if (revision < -1) { revision = -1; changed = true; }
        if (placed && (page < 0 || page > 32 || float.IsNaN(x) || float.IsInfinity(x) ||
            float.IsNaN(y) || float.IsInfinity(y) || float.IsNaN(tilt) || float.IsInfinity(tilt)))
        { placed = false; x = y = tilt = 0f; page = 0; changed = true; }
        return changed;
    }
}

/// <summary>
/// T09: the Auditor's review of one case's computation. Only decided hand-ins
/// count (a refusal records nothing). Root causes are case-scoped ids charged
/// once each; a retry is a changed hand-in after a returned review. On
/// approval the case XP and the approved sheet's own values are frozen here.
/// Never an answer key.
/// </summary>
[Serializable]
public class Level3ReviewRecord
{
    public int submissions;
    public int returned;
    public int holds;
    public int retries;
    public List<string> chargedRoots = new List<string>();
    /// <summary>"", "approved", "hold", "premature", "false-hold" or "stale-stamp".</summary>
    public string lastOutcome = "";
    public string lastRoot = "";
    /// <summary>The sheet line the Auditor marked last (a Level3ComputationRules field id).</summary>
    public string lastField = "";
    /// <summary>T09 revision: why that line was marked ("Missing", "Stale", "Typed", "Wrong", "AsExpense"; "" before this field existed).</summary>
    public string lastKind = "";
    /// <summary>The sheet revision and stamp of the last decided hand-in (an unchanged one is refused).</summary>
    public string lastSignature = "";
    /// <summary>The first review cutoff passed before approval (the existing punctuality treatment, inside the cap).</summary>
    public bool late;
    public bool approved;
    public int xp;
    public int approvedRevision = -1;
    public string taxableIncome = "";
    public string taxDue = "";
    public string credits = "";
    public string remainingPayable = "";

    public Level3ReviewRecord Copy() => new Level3ReviewRecord
    {
        submissions = submissions, returned = returned, holds = holds, retries = retries,
        chargedRoots = chargedRoots != null ? new List<string>(chargedRoots) : new List<string>(),
        lastOutcome = lastOutcome, lastRoot = lastRoot, lastField = lastField, lastKind = lastKind, lastSignature = lastSignature, late = late,
        approved = approved, xp = xp, approvedRevision = approvedRevision,
        taxableIncome = taxableIncome, taxDue = taxDue, credits = credits, remainingPayable = remainingPayable
    };

    public bool Repair()
    {
        bool changed = false;
        if (submissions < 0) { submissions = 0; changed = true; }
        if (returned < 0) { returned = 0; changed = true; }
        if (holds < 0) { holds = 0; changed = true; }
        if (retries < 0) { retries = 0; changed = true; }
        chargedRoots = Level3ChapterCheckpoint.CleanIds(chargedRoots, ref changed);
        if (lastOutcome == null) { lastOutcome = ""; changed = true; }
        if (lastRoot == null) { lastRoot = ""; changed = true; }
        if (lastField == null) { lastField = ""; changed = true; }
        if (lastKind == null) { lastKind = ""; changed = true; }
        if (lastSignature == null) { lastSignature = ""; changed = true; }
        if (taxableIncome == null) { taxableIncome = ""; changed = true; }
        if (taxDue == null) { taxDue = ""; changed = true; }
        if (credits == null) { credits = ""; changed = true; }
        if (remainingPayable == null) { remainingPayable = ""; changed = true; }
        if (xp < 0) { xp = 0; changed = true; }
        return changed;
    }
}

[Serializable]
public class Level3FolderPacket
{
    public string fullName = "", tin = "", classification = "", taxpayerType = "", incomeSource = "";
    public bool summaryAttached, certificateAttached, onDesk;
    public Level3FolderPacket Copy() => (Level3FolderPacket)MemberwiseClone();
}

/// <summary>One Level 3 case inside the chapter snapshot (T02 skeleton; later tickets add their work).</summary>
[Serializable]
public class Level3CaseProgress
{
    public string caseId = "";
    /// <summary>The case was assigned (Bea at reception; the company only after Bea's delivery).</summary>
    public bool admitted;
    /// <summary>Source ids the player actually acquired for this case (never answers).</summary>
    public List<string> acquiredSourceIds = new List<string>();
    /// <summary>Counts real edits to this case's work (reading or moving a paper is not one).</summary>
    public int packetRevision;
    /// <summary>Where this case's computation copy is: "" / "atAuditor" / "carried" / "done" (T10/T12).</summary>
    public string copyPhase = "";
    /// <summary>T10: Bea's balance question was answered from her approved copy (saved before the handover; older saves restore false).</summary>
    public bool balanceExplained;
    /// <summary>Saved wrong figure choices during Bea's follow-through, before delivery.</summary>
    public int balanceWrongAnswers;
    public bool sideQuestIntroSeen;
    public Level3FolderPacket folder;
    /// <summary>The player's own collected work in this case's folder (T06: "B-WORKSHEET", Bea's checked worksheet); never answers.</summary>
    public List<string> workIds = new List<string>();
    /// <summary>T07: this case's computation sheet at the office desk (the player's choices; its tapes are the chapter's).</summary>
    public Level3ComputationRecord computation = new Level3ComputationRecord();
    /// <summary>T09: the stamp on the computation sheet, the Auditor's review and this case's own review clock (its active budget, day and voucher extension).</summary>
    public Level3StampRecord stamp = new Level3StampRecord();
    public Level3ReviewRecord review = new Level3ReviewRecord();
    public ReviewClockState clock = new ReviewClockState();

    public Level3CaseProgress Copy() => new Level3CaseProgress
    {
        caseId = caseId,
        admitted = admitted,
        acquiredSourceIds = acquiredSourceIds != null ? new List<string>(acquiredSourceIds) : new List<string>(),
        packetRevision = packetRevision,
        copyPhase = copyPhase,
        balanceExplained = balanceExplained,
        balanceWrongAnswers = balanceWrongAnswers,
        sideQuestIntroSeen = sideQuestIntroSeen,
        folder = folder?.Copy(),
        workIds = workIds != null ? new List<string>(workIds) : new List<string>(),
        computation = computation != null ? computation.Copy() : new Level3ComputationRecord(),
        stamp = stamp != null ? stamp.Copy() : new Level3StampRecord(),
        review = review != null ? review.Copy() : new Level3ReviewRecord(),
        clock = CopyClock(clock)
    };

    /// <summary>A deep copy of a review clock (a command changes a copy).</summary>
    public static ReviewClockState CopyClock(ReviewClockState clock) => clock == null ? new ReviewClockState() : new ReviewClockState
    {
        started = clock.started, day = clock.day, remaining = clock.remaining,
        warned = clock.warned != null ? new List<float>(clock.warned) : new List<float>(),
        cutoffPending = clock.cutoffPending, missed = clock.missed, extensionSeconds = clock.extensionSeconds, extensionDay = clock.extensionDay
    };

    public bool Repair()
    {
        bool changed = false;
        acquiredSourceIds = Level3ChapterCheckpoint.CleanIds(acquiredSourceIds, ref changed);
        workIds = Level3ChapterCheckpoint.CleanIds(workIds, ref changed);
        if (computation == null) { computation = new Level3ComputationRecord(); changed = true; }
        changed |= computation.Repair();
        if (stamp == null) { stamp = new Level3StampRecord(); changed = true; }
        changed |= stamp.Repair();
        if (review == null) { review = new Level3ReviewRecord(); changed = true; }
        changed |= review.Repair();
        if (clock == null) { clock = new ReviewClockState(); changed = true; }
        if (clock.warned == null) { clock.warned = new List<float>(); changed = true; }
        if (clock.day < 1) { clock.day = 1; changed = true; }
        if (clock.remaining < 0f) { clock.remaining = 0f; changed = true; }
        if (packetRevision < 0) { packetRevision = 0; changed = true; }
        if (copyPhase == null) { copyPhase = ""; changed = true; }
        if (balanceWrongAnswers < 0) { balanceWrongAnswers = 0; changed = true; }
        return changed;
    }
}

/// <summary>
/// An unfinished level run that can be resumed after a relaunch (Level 2:
/// Rosa's review). Plain data only (no scene references): the review state as
/// JSON plus the taxpayer facts the player entered, which Unity's JSON cannot
/// store as nullable enums. Cleared when the level result is saved.
/// </summary>
[Serializable]
public class LevelCheckpoint
{
    public string levelId;
    public string caseId;
    public int writes;
    public string reviewJson;
    public string residencyStatus;
    public string taxpayerType;
    public string incomeSource;
    public List<string> transferredFields = new List<string>();

    public bool IsSet => !string.IsNullOrEmpty(levelId) && !string.IsNullOrEmpty(reviewJson);
}

[Serializable]
public class PlayerProgressData
{
    public const int CurrentSaveVersion = 5;
    /// <summary>Last version before the campaign; saves without a campaign stay at it.</summary>
    public const int LegacySaveVersion = 4;
    /// <summary>First version written signed; a plain (unsigned) file claiming this or later is hand-made.</summary>
    public const int SignedSinceVersion = 3;
    /// <summary>Highest badge tier (gold).</summary>
    public const int MaxBadgeTier = 3;

    public int saveVersion;
    public int totalExp;
    public int totalReputation;
    public int levelsCompleted;
    public List<string> completedLevelIds = new List<string>();
    public List<string> passedLevelIds = new List<string>();
    public List<LevelRecord> levelRecords = new List<LevelRecord>();
    public List<BadgeRecord> badgeRecords = new List<BadgeRecord>();
    /// <summary>In-game tutorial lessons the player has seen, as "Level_01/move" (version 4).</summary>
    public List<string> tutorialsSeen = new List<string>();
    public List<TrainingRecord> trainingRecords = new List<TrainingRecord>();

    /// <summary>One resumable unfinished level run (empty = none). Optional: old saves have none.</summary>
    public LevelCheckpoint activeCheckpoint = new LevelCheckpoint();

    /// <summary>Neutral story facts learned in completed levels (e.g. "rosa-desk-provenance").</summary>
    public List<string> storyFacts = new List<string>();

    /// <summary>Set after the Level 1 intro finishes; default false for fresh/older saves.</summary>
    public bool levelOneIntroSeen;

    /// <summary>The updated story campaign (version 5); empty (no id) in older saves and before New Game.</summary>
    public CampaignRecord campaign = new CampaignRecord();

    /// <summary>True when this save holds a committed updated-story campaign.</summary>
    public bool HasCampaign => campaign != null && campaign.IsSet;

    /// <summary>
    /// The complete first state of a new campaign: no EXP, score hearts, badges,
    /// level records, checkpoint, tutorials or story facts, plus a fresh
    /// campaign record. Nothing is copied from any earlier save.
    /// </summary>
    public static PlayerProgressData CreateNewCampaign(string campaignId, string createdUtc)
    {
        var data = new PlayerProgressData { campaign = CampaignRecord.CreateFresh(campaignId, createdUtc) };
        data.Normalize();
        return data;
    }

    /// <summary>The first career-board visit follows a finished Level 1, at any score.</summary>
    public bool HasFinishedFirstLevel => completedLevelIds != null && completedLevelIds.Contains("Level_01");

    /// <summary>Existing completed careers already know the intro, even without the new field.</summary>
    public bool HasSeenLevelOneIntro => levelOneIntroSeen || HasFinishedFirstLevel;

    /// <summary>The resumable run of this level, or null.</summary>
    public LevelCheckpoint CheckpointFor(string levelId) =>
        activeCheckpoint != null && activeCheckpoint.IsSet && activeCheckpoint.levelId == levelId ? activeCheckpoint : null;

    public void SetCheckpoint(LevelCheckpoint checkpoint)
    {
        if (checkpoint == null || !checkpoint.IsSet) return;
        checkpoint.writes = (activeCheckpoint != null && activeCheckpoint.levelId == checkpoint.levelId ? activeCheckpoint.writes : 0) + 1;
        activeCheckpoint = checkpoint;
    }

    /// <summary>Clears the checkpoint of this level. True when there was one.</summary>
    public bool ClearCheckpoint(string levelId)
    {
        if (CheckpointFor(levelId) == null) return false;
        activeCheckpoint = new LevelCheckpoint();
        return true;
    }

    /// <summary>Records a story fact once. True when it is new.</summary>
    public bool RecordStoryFact(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (storyFacts == null) storyFacts = new List<string>();
        if (storyFacts.Contains(id)) return false;
        storyFacts.Add(id);
        return true;
    }

    // =========================================================
    // TRAINING HUB (version 4): never touches EXP, hearts, badges or levels
    // =========================================================

    /// <summary>"Level_01" + "move" -> "Level_01/move".</summary>
    public static string TutorialKey(string levelId, string lessonId) =>
        string.IsNullOrWhiteSpace(levelId) || string.IsNullOrWhiteSpace(lessonId)
            ? null : levelId.Trim() + "/" + lessonId.Trim();

    public bool HasSeenTutorial(string key) =>
        !string.IsNullOrWhiteSpace(key) && tutorialsSeen != null && tutorialsSeen.Contains(key.Trim());

    /// <summary>Marks a lesson seen. True when it is new.</summary>
    public bool RecordTutorialSeen(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (tutorialsSeen == null) tutorialsSeen = new List<string>();
        string id = key.Trim();
        if (tutorialsSeen.Contains(id)) return false;
        tutorialsSeen.Add(id);
        return true;
    }

    public TrainingRecord GetTrainingRecord(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId) || trainingRecords == null) return null;
        string id = cardId.Trim();
        foreach (TrainingRecord record in trainingRecords)
            if (record != null && record.cardId == id) return record;
        return null;
    }

    /// <summary>
    /// Counts a finished practice run of a drill card (mastery = the run without
    /// the guide's hand). Returns true when this run made the card mastered.
    /// </summary>
    public bool RecordTrainingRun(string cardId, bool mastery, float seconds)
    {
        if (string.IsNullOrWhiteSpace(cardId)) return false;
        if (trainingRecords == null) trainingRecords = new List<TrainingRecord>();
        TrainingRecord record = GetTrainingRecord(cardId);
        if (record == null)
        {
            record = new TrainingRecord { cardId = cardId.Trim() };
            trainingRecords.Add(record);
        }
        bool wasMastered = record.Mastered;
        if (mastery)
        {
            if (record.masteryRuns < int.MaxValue) record.masteryRuns++;
            int run = ToSeconds(seconds);
            if (run > 0) record.bestMasterySeconds = MinPositive(record.bestMasterySeconds, run);
        }
        else if (record.practiceRuns < int.MaxValue)
        {
            record.practiceRuns++;
        }
        return !wasMastered && record.Mastered;
    }

    /// <summary>Best tier collected for a badge (0 = not collected).</summary>
    public int GetBadgeTier(string badgeId)
    {
        if (string.IsNullOrWhiteSpace(badgeId) || badgeRecords == null) return 0;
        foreach (BadgeRecord record in badgeRecords)
            if (record != null && record.badgeId == badgeId.Trim()) return record.tier;
        return 0;
    }

    /// <summary>Keeps the best tier of a badge; returns true when it is new or upgraded.</summary>
    public bool RecordBadge(string badgeId, int tier)
    {
        if (string.IsNullOrWhiteSpace(badgeId) || tier <= 0) return false;
        tier = Math.Min(tier, MaxBadgeTier);
        if (badgeRecords == null) badgeRecords = new List<BadgeRecord>();
        string id = badgeId.Trim();
        foreach (BadgeRecord record in badgeRecords)
        {
            if (record == null || record.badgeId != id) continue;
            if (tier <= record.tier) return false;
            record.tier = tier;
            return true;
        }
        badgeRecords.Add(new BadgeRecord { badgeId = id, tier = tier });
        return true;
    }

    /// <summary>Legacy unlock (previous level passed); compatibility entry point for MasteryBoundary.</summary>
    public bool IsLevelUnlocked(int levelNumber) => MasteryBoundary.LegacyLevelUnlocked(this, levelNumber);

    public bool IsLevelPassed(string levelId) =>
        !string.IsNullOrWhiteSpace(levelId) && passedLevelIds != null && passedLevelIds.Contains(levelId.Trim());

    public LevelRecord GetRecord(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId) || levelRecords == null) return null;
        string id = levelId.Trim();
        foreach (LevelRecord record in levelRecords)
            if (record != null && record.levelId == id) return record;
        return null;
    }

    /// <summary>
    /// Saves a finished run: keeps the best EXP / hearts, marks the level
    /// passed (never un-passes it), adds badges, and returns how much the
    /// career totals grew (only the improvement over the previous best).
    /// Compatibility entry point: the rule lives in MasteryBoundary.RecordResult.
    /// </summary>
    public (int expGain, int heartsGain, bool newBest) RecordLevelResult(
        string levelId, int exp, int hearts, int maxExp, int maxHearts, bool passed, IEnumerable<string> badges) =>
        MasteryBoundary.RecordResult(this, levelId, exp, hearts, maxExp, maxHearts, passed, badges);

    /// <summary>
    /// Adds a finished run's time to that level's record (call after RecordLevelResult):
    /// fastest run, last run and total time played; the side quest keeps its fastest
    /// and last time. Times of 0 or less are ignored. Returns false without a record.
    /// </summary>
    public bool RecordLevelTime(string levelId, float runSeconds, float sideQuestSeconds)
    {
        LevelRecord record = GetRecord(levelId);
        if (record == null) return false;
        int run = ToSeconds(runSeconds);
        if (run > 0)
        {
            record.lastSeconds = run;
            record.bestSeconds = record.bestSeconds > 0 ? Math.Min(record.bestSeconds, run) : run;
            record.totalSeconds = (int)Math.Min(int.MaxValue, (long)record.totalSeconds + run);
        }
        int side = ToSeconds(sideQuestSeconds);
        if (side > 0)
        {
            record.sideQuestLastSeconds = side;
            record.sideQuestBestSeconds = record.sideQuestBestSeconds > 0 ? Math.Min(record.sideQuestBestSeconds, side) : side;
        }
        return run > 0 || side > 0;
    }

    private static int ToSeconds(float seconds) =>
        float.IsNaN(seconds) || seconds <= 0f ? 0 : seconds >= int.MaxValue ? int.MaxValue : (int)Math.Round(seconds);

    public bool RecordLevelCompletion(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return false;

        Normalize();

        string normalizedLevelId = levelId.Trim();
        if (completedLevelIds.Contains(normalizedLevelId))
            return false;

        completedLevelIds.Add(normalizedLevelId);
        if (!passedLevelIds.Contains(normalizedLevelId))
            passedLevelIds.Add(normalizedLevelId);
        levelsCompleted = completedLevelIds.Count;
        return true;
    }

    public bool Normalize()
    {
        bool changed = false;

        if (completedLevelIds == null)
        {
            completedLevelIds = new List<string>();
            changed = true;
        }

        if (passedLevelIds == null)
        {
            passedLevelIds = new List<string>();
            changed = true;
        }

        if (levelRecords == null)
        {
            levelRecords = new List<LevelRecord>();
            changed = true;
        }

        if (badgeRecords == null)
        {
            badgeRecords = new List<BadgeRecord>();
            changed = true;
        }

        if (tutorialsSeen == null) { tutorialsSeen = new List<string>(); changed = true; }
        if (trainingRecords == null) { trainingRecords = new List<TrainingRecord>(); changed = true; }
        if (activeCheckpoint == null) { activeCheckpoint = new LevelCheckpoint(); changed = true; }
        if (activeCheckpoint.transferredFields == null) { activeCheckpoint.transferredFields = new List<string>(); changed = true; }
        if (storyFacts == null) { storyFacts = new List<string>(); changed = true; }
        if (campaign == null) { campaign = new CampaignRecord(); changed = true; }
        changed |= RepairCampaign();

        if (saveVersion == 0 && levelsCompleted > 0 && completedLevelIds.Count == 0)
        {
            completedLevelIds.Add("Level_01");
            changed = true;
        }

        completedLevelIds = CleanIds(completedLevelIds, ref changed);

        // Version 1 had no pass line: every level finished then counts as passed.
        if (saveVersion < 2)
        {
            foreach (string levelId in completedLevelIds)
            {
                if (!passedLevelIds.Contains(levelId))
                {
                    passedLevelIds.Add(levelId);
                    changed = true;
                }
            }
        }

        passedLevelIds = CleanIds(passedLevelIds, ref changed);

        // A passed level is always a completed one.
        foreach (string levelId in passedLevelIds)
        {
            if (!completedLevelIds.Contains(levelId))
            {
                completedLevelIds.Add(levelId);
                changed = true;
            }
        }

        if (levelRecords.RemoveAll(r => r == null || string.IsNullOrWhiteSpace(r.levelId)) > 0)
            changed = true;
        changed |= RepairRecords();
        changed |= RepairBadges();
        changed |= RepairTraining();

        if (totalExp < 0) { totalExp = 0; changed = true; }
        if (totalReputation < 0) { totalReputation = 0; changed = true; }

        int uniqueLevelCount = completedLevelIds.Count;
        if (levelsCompleted != uniqueLevelCount)
        {
            levelsCompleted = uniqueLevelCount;
            changed = true;
        }

        // Only a save holding a campaign is version 5; older data stays a pre-campaign save.
        int version = HasCampaign ? CurrentSaveVersion : LegacySaveVersion;
        if (saveVersion < version)
        {
            saveVersion = version;
            changed = true;
        }

        return changed;
    }

    /// <summary>Clean campaign lists, trust 0..MaxTrust, no negative coins or quantities, one stack per item.</summary>
    private bool RepairCampaign()
    {
        bool changed = false;
        CampaignRecord c = campaign;
        if (c.ending == null) { c.ending = new ChapterEndingRecord(); changed = true; }
        if (c.campaignId != null && c.campaignId.Trim() != c.campaignId) { c.campaignId = c.campaignId.Trim(); changed = true; }
        if (c.completedChapterIds == null) { c.completedChapterIds = new List<string>(); changed = true; }
        if (c.rewardClaims == null) { c.rewardClaims = new List<string>(); changed = true; }
        if (c.ownedUpgradeIds == null) { c.ownedUpgradeIds = new List<string>(); changed = true; }
        if (c.items == null) { c.items = new List<CampaignItemStack>(); changed = true; }
        if (c.levelOne == null) { c.levelOne = new Level1FieldCheckpoint(); changed = true; }
        changed |= c.levelOne.Repair();
        if (c.levelOneCaseResult == null) { c.levelOneCaseResult = new Level1CaseResult(); changed = true; }
        changed |= c.levelOneCaseResult.Repair();
        if (c.levelThree == null) { c.levelThree = new Level3ChapterCheckpoint(); changed = true; }
        changed |= c.levelThree.Repair();
        if (c.completedActivityIds == null) { c.completedActivityIds = new List<string>(); changed = true; }
        c.completedActivityIds = CleanIds(c.completedActivityIds, ref changed);
        if (c.ledger == null) { c.ledger = new List<CampaignLedgerEntry>(); changed = true; }
        changed |= c.ledger.RemoveAll(e => e == null || string.IsNullOrWhiteSpace(e.kind)) > 0;
        if (c.inventory == null) { c.inventory = new CampaignInventoryRecord(); changed = true; }
        changed |= c.inventory.Repair();
        if (c.rosaEthics == null) { c.rosaEthics = new RosaEthicsRecord(); changed = true; }
        if (c.rosaEthics.consequenceIds == null) { c.rosaEthics.consequenceIds = new List<string>(); changed = true; }
        c.rosaEthics.consequenceIds = CleanIds(c.rosaEthics.consequenceIds, ref changed);
        if (c.rosaEthics.caseId == null) { c.rosaEthics.caseId = ""; changed = true; }
        if (c.rosaEthics.commitmentId == null) { c.rosaEthics.commitmentId = ""; changed = true; }
        if (c.rosaEthics.choiceId == null) { c.rosaEthics.choiceId = ""; changed = true; }
        if (c.rosaEthics.choiceText == null) { c.rosaEthics.choiceText = ""; changed = true; }
        changed |= RepairDecor(c);
        c.completedChapterIds = CleanIds(c.completedChapterIds, ref changed);
        c.rewardClaims = CleanIds(c.rewardClaims, ref changed);
        c.ownedUpgradeIds = CleanIds(c.ownedUpgradeIds, ref changed);

        if (c.IsSet && string.IsNullOrWhiteSpace(c.currentChapterId)) { c.currentChapterId = CampaignRecord.FirstChapterId; changed = true; }
        int trust = Math.Max(0, Math.Min(CampaignRecord.MaxTrust, c.publicTrust));
        if (trust != c.publicTrust) { c.publicTrust = trust; changed = true; }
        if (c.coins < 0)
        {
            // T09: a corrupt negative balance becomes a safe zero, with an explicit repair record.
            c.ledger.Add(new CampaignLedgerEntry { kind = "repair", amount = -c.coins, balanceAfter = 0, reason = $"Coin balance {c.coins} repaired to 0" });
            c.coins = 0;
            changed = true;
        }

        var stacks = new Dictionary<string, CampaignItemStack>(StringComparer.Ordinal);
        var kept = new List<CampaignItemStack>(c.items.Count);
        foreach (CampaignItemStack item in c.items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId)) { changed = true; continue; }
            string id = item.itemId.Trim();
            if (id != item.itemId) { item.itemId = id; changed = true; }
            if (item.carried < 0) { item.carried = 0; changed = true; }
            if (item.stored < 0) { item.stored = 0; changed = true; }
            if (stacks.TryGetValue(id, out CampaignItemStack first))
            {
                first.carried = (int)Math.Min(int.MaxValue, (long)first.carried + item.carried);
                first.stored = (int)Math.Min(int.MaxValue, (long)first.stored + item.stored);
                changed = true;
                continue;
            }
            stacks[id] = item;
            kept.Add(item);
        }
        c.items = kept;
        return changed;
    }

    /// <summary>T13: one placement per decor item, trimmed ids; anchors are checked against the catalogue later (DecorRules.Resolve).</summary>
    private static bool RepairDecor(CampaignRecord c)
    {
        bool changed = false;
        if (c.decor == null) { c.decor = new List<DecorPlacement>(); return true; }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<DecorPlacement>(c.decor.Count);
        foreach (DecorPlacement placement in c.decor)
        {
            if (placement == null || string.IsNullOrWhiteSpace(placement.itemId)) { changed = true; continue; }
            string id = placement.itemId.Trim();
            if (id != placement.itemId) { placement.itemId = id; changed = true; }
            string anchor = (placement.anchorId ?? "").Trim();
            if (anchor != placement.anchorId) { placement.anchorId = anchor; changed = true; }
            if (!seen.Add(id)) { changed = true; continue; }
            kept.Add(placement);
        }
        c.decor = kept;
        return changed;
    }

    /// <summary>One record per level, values inside their own maximum.</summary>
    private bool RepairRecords()
    {
        bool changed = false;
        var seen = new Dictionary<string, LevelRecord>(StringComparer.Ordinal);
        var kept = new List<LevelRecord>(levelRecords.Count);
        foreach (LevelRecord record in levelRecords)
        {
            string id = record.levelId.Trim();
            if (id != record.levelId) { record.levelId = id; changed = true; }

            if (record.maxExp < 0) { record.maxExp = 0; changed = true; }
            if (record.maxHearts < 0) { record.maxHearts = 0; changed = true; }
            int exp = Math.Max(0, record.maxExp > 0 ? Math.Min(record.bestExp, record.maxExp) : record.bestExp);
            int hearts = Math.Max(0, record.maxHearts > 0 ? Math.Min(record.bestHearts, record.maxHearts) : record.bestHearts);
            if (exp != record.bestExp || hearts != record.bestHearts)
            {
                record.bestExp = exp;
                record.bestHearts = hearts;
                changed = true;
            }
            if (record.attempts < 0) { record.attempts = 0; changed = true; }
            if (record.bestSeconds < 0) { record.bestSeconds = 0; changed = true; }
            if (record.lastSeconds < 0) { record.lastSeconds = 0; changed = true; }
            if (record.totalSeconds < 0) { record.totalSeconds = 0; changed = true; }
            if (record.sideQuestBestSeconds < 0) { record.sideQuestBestSeconds = 0; changed = true; }
            if (record.sideQuestLastSeconds < 0) { record.sideQuestLastSeconds = 0; changed = true; }
            if (record.badges == null) { record.badges = new List<string>(); changed = true; }
            record.badges = CleanIds(record.badges, ref changed);

            if (seen.TryGetValue(id, out LevelRecord first))
            {
                // Duplicate entries for one level merge into the best of both.
                first.bestExp = Math.Max(first.bestExp, record.bestExp);
                first.bestHearts = Math.Max(first.bestHearts, record.bestHearts);
                first.passed |= record.passed;
                first.attempts += record.attempts;
                first.bestSeconds = MinPositive(first.bestSeconds, record.bestSeconds);
                first.sideQuestBestSeconds = MinPositive(first.sideQuestBestSeconds, record.sideQuestBestSeconds);
                first.totalSeconds = (int)Math.Min(int.MaxValue, (long)first.totalSeconds + record.totalSeconds);
                foreach (string badge in record.badges)
                    if (!first.badges.Contains(badge)) first.badges.Add(badge);
                changed = true;
                continue;
            }
            seen[id] = record;
            kept.Add(record);

            if (record.passed && !passedLevelIds.Contains(id)) { passedLevelIds.Add(id); changed = true; }
            if (!completedLevelIds.Contains(id)) { completedLevelIds.Add(id); changed = true; }
        }
        levelRecords = kept;
        return changed;
    }

    private static int MinPositive(int a, int b) => a <= 0 ? b : b <= 0 ? a : Math.Min(a, b);

    /// <summary>One entry per badge, tier 0..MaxBadgeTier; empty ids removed.</summary>
    private bool RepairBadges()
    {
        bool changed = false;
        var best = new Dictionary<string, BadgeRecord>(StringComparer.Ordinal);
        var kept = new List<BadgeRecord>(badgeRecords.Count);
        foreach (BadgeRecord badge in badgeRecords)
        {
            if (badge == null || string.IsNullOrWhiteSpace(badge.badgeId)) { changed = true; continue; }
            string id = badge.badgeId.Trim();
            int tier = Math.Max(0, Math.Min(MaxBadgeTier, badge.tier));
            if (id != badge.badgeId || tier != badge.tier) { badge.badgeId = id; badge.tier = tier; changed = true; }
            if (best.TryGetValue(id, out BadgeRecord first))
            {
                first.tier = Math.Max(first.tier, tier);
                changed = true;
                continue;
            }
            best[id] = badge;
            kept.Add(badge);
        }
        badgeRecords = kept;
        return changed;
    }

    /// <summary>A clean lesson list and one record per card with sane counts.</summary>
    private bool RepairTraining()
    {
        bool changed = false;
        tutorialsSeen = CleanIds(tutorialsSeen, ref changed);

        var best = new Dictionary<string, TrainingRecord>(StringComparer.Ordinal);
        var kept = new List<TrainingRecord>(trainingRecords.Count);
        foreach (TrainingRecord record in trainingRecords)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.cardId)) { changed = true; continue; }
            string id = record.cardId.Trim();
            if (id != record.cardId) { record.cardId = id; changed = true; }
            if (record.practiceRuns < 0) { record.practiceRuns = 0; changed = true; }
            if (record.masteryRuns < 0) { record.masteryRuns = 0; changed = true; }
            if (record.bestMasterySeconds < 0) { record.bestMasterySeconds = 0; changed = true; }
            if (best.TryGetValue(id, out TrainingRecord first))
            {
                first.practiceRuns = (int)Math.Min(int.MaxValue, (long)first.practiceRuns + record.practiceRuns);
                first.masteryRuns = (int)Math.Min(int.MaxValue, (long)first.masteryRuns + record.masteryRuns);
                first.bestMasterySeconds = MinPositive(first.bestMasterySeconds, record.bestMasterySeconds);
                changed = true;
                continue;
            }
            best[id] = record;
            kept.Add(record);
        }
        trainingRecords = kept;
        return changed;
    }

    private static List<string> CleanIds(List<string> ids, ref bool changed)
    {
        var cleanedIds = new List<string>(ids.Count);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (string levelId in ids)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                changed = true;
                continue;
            }

            string cleanedId = levelId.Trim();
            if (cleanedId != levelId)
                changed = true;

            if (!seenIds.Add(cleanedId))
            {
                changed = true;
                continue;
            }

            cleanedIds.Add(cleanedId);
        }

        return cleanedIds;
    }
}
