using System;
using System.IO;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Owns the player's permanent, whole-game progression (total EXP,
/// total Reputation, levels completed, best results, badges) and persists
/// it to Application.persistentDataPath/Save/progress.json (Resources is
/// read-only on device).
///
/// LIFETIME: created automatically before the first scene loads and kept
/// across scenes (DontDestroyOnLoad), so every scene, including a level
/// opened directly in the editor, can use PlayerProgressManager.Instance.
/// Copies placed in scenes remove themselves.
///
/// SAVING (SaveFileStore):
/// - Atomic write with the previous save kept as progress.json.bak.
/// - The file is a small envelope { format, payload, signature }: payload
///   is the PlayerProgressData JSON and signature its HMAC, so a hand-edited
///   save is noticed. A save that fails the check is kept aside as
///   progress.json.rejected and the backup is used instead.
/// - Older plain saves (version 1 and 2) still load and are upgraded.
/// - Whatever loads is passed through PlayerProgressData.Normalize(), which
///   repairs impossible values.
///
/// CAMPAIGN (version 5, T02): at load the save is classified once
/// (CampaignState): no save, a committed campaign, an older save that needs a
/// New Game (nothing of it is imported), a newer game's save (left untouched
/// and never overwritten by Save) or unreadable files. StartNewCampaign is the
/// only way to create a campaign: it writes the full fresh record first and
/// publishes it only after the write succeeded.
///
/// APPLYING A LEVEL: LevelCompleteUI registers the finished level with
/// SetPendingResult as soon as the Level Complete card shows, and applies it
/// on Continue. If the game is paused (sent to the background on mobile) or
/// closed before Continue, the pending result is applied and saved then, so
/// a finished level is never lost. Each result is applied at most once
/// (LevelTotalResult.RunId).
/// </summary>
public class PlayerProgressManager : MonoBehaviour
{
    [Serializable]
    private class SaveEnvelope
    {
        public int format;
        public string payload;
        public string signature;
    }

    private const int EnvelopeFormat = 1;

    public static PlayerProgressManager Instance { get; private set; }

    /// <summary>Raised after progress changed and was saved (menus refresh from it).</summary>
    public static event Action<PlayerProgressData> ProgressChanged;

    private PlayerProgressData progress;
    private SaveFileStore store;
    private LevelTotalResult pendingResult;
    private string lastAppliedRunId;
    private CampaignSaveState campaignState;

    // Snapshot of the last successful write/load, never an unsaved result mutation.
    public bool HasSavedFirstChapterCompletion { get; private set; }

    private void RememberSavedCompletion() => HasSavedFirstChapterCompletion =
        CampaignMenu.HasStory(campaignState, progress) &&
        progress.campaign.IsChapterCompleted(CampaignRecord.FirstChapterId);

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Save");
    private string SavePath => Path.Combine(SaveDirectory, "progress.json");

    public PlayerProgressData CurrentProgress => progress;

    /// <summary>What the save means for the updated story (T02): fresh install, campaign, old save, newer save or unreadable.</summary>
    public CampaignSaveState CampaignState => campaignState;

    /// <summary>True only for a committed version-5 campaign; old saves never supply Continue.</summary>
    public bool CanContinueCampaign => CampaignSave.CanContinue(campaignState);

    /// <summary>Why Continue is unavailable, e.g. "Start the updated story" ("" when it is available).</summary>
    public string ContinueNotice => CampaignSave.ContinueNotice(campaignState);

    /// <summary>True when a finished level is waiting to be saved.</summary>
    public bool HasPendingResult => pendingResult != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var host = new GameObject("PlayerProgressManager");
        host.AddComponent<PlayerProgressManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A scene copy: remove just this component when the object holds
            // anything else, otherwise the whole (empty) object.
            if (GetComponents<Component>().Length <= 2) Destroy(gameObject);
            else Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        store = new SaveFileStore(SavePath);
        progress = LoadOrCreate();
        RememberSavedCompletion();

        Debug.Log(
            $"[PlayerProgressManager] Ready. " +
            $"EXP: {progress.totalExp}, Reputation: {progress.totalReputation}, " +
            $"Levels: {progress.levelsCompleted}"
        );
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) FlushPending("app paused");
    }

    private void OnApplicationQuit()
    {
        FlushPending("app closing");
    }

    // =========================================================
    // LEVEL RESULTS
    // =========================================================

    /// <summary>
    /// Remembers a finished level so it is saved even if the player never
    /// presses Continue (app paused or closed). Apply it with ApplyLevelResult.
    /// </summary>
    public void SetPendingResult(LevelTotalResult levelTotal)
    {
        if (levelTotal == null || levelTotal.RunId == lastAppliedRunId) return;
        if (IgnoredForPractice(levelTotal)) return;
        pendingResult = levelTotal;
    }

    private void FlushPending(string reason)
    {
        if (pendingResult == null) return;
        Debug.Log($"[PlayerProgressManager] Saving the finished level ({reason}).");
        ApplyLevelResult(pendingResult);
    }

    /// <summary>
    /// Folds a completed level's totals into permanent progression and saves.
    /// Safe to call more than once for the same result: it applies once.
    /// </summary>
    public void ApplyLevelResult(LevelTotalResult levelTotal)
    {
        if (levelTotal == null)
        {
            Debug.LogError("[PlayerProgressManager] ApplyLevelResult called with NULL levelTotal.");
            return;
        }

        if (string.IsNullOrWhiteSpace(levelTotal.LevelId))
        {
            Debug.LogError("[PlayerProgressManager] ApplyLevelResult requires a level ID.");
            return;
        }

        if (IgnoredForPractice(levelTotal)) return;

        if (levelTotal.RunId == lastAppliedRunId)
        {
            Debug.Log("[PlayerProgressManager] This level result was already saved.");
            if (pendingResult == levelTotal) pendingResult = null;
            return;
        }

        // T18: a story run of Rosa's review commits its case claim, mastery record and checkpoint in one write.
        if (IsLevelTwoStoryResult(levelTotal))
        {
            CommitLevelTwoResult(levelTotal);
            return;
        }

        lastAppliedRunId = levelTotal.RunId;
        if (pendingResult == levelTotal) pendingResult = null;
        CampaignRunMode mode = CurrentRunMode;
        // The finished run replaces its resumable checkpoint in the same write (a replay never touches the story's).
        if (mode != CampaignRunMode.MasteryReplay && (levelTotal.LevelId != OfficeLevelCatalog.Level2Id || !Level2Checkpointer.IsDirectEditorAttempt))
            progress.ClearCheckpoint(levelTotal.LevelId);

        // Story runs record the campaign's first completion of the chapter (Continue then opens the career).
        // T07: never Level 1 here: its story moves on only through the recorded case result
        // (CommitLevelOneCaseResult), Marie's follow-through and the chapter wrap-up (T16, CommitLevelOneWrapUp).
        if (CampaignRun.AllowsStoryWrites(mode) && progress.HasCampaign && levelTotal.LevelId != CampaignRecord.FirstChapterId &&
            progress.campaign.MarkChapterCompleted(levelTotal.LevelId))
            Debug.Log($"[PlayerProgressManager] Story chapter {levelTotal.LevelId} completed.");

        // Mastery records (MasteryBoundary): best result per level, badge tiers
        // and times; only a passing run unlocks the next level, and the career
        // totals grow only by improvements on the best. A run without a level
        // score (no rewards.json ledger) counts as a completion whose rewards
        // are kept as that level's best, so replaying never adds them again.
        var gain = MasteryBoundary.ApplyRun(progress, ToMasteryRun(levelTotal));
        if (levelTotal.HasScore)
        {
            Debug.Log(
                $"[PlayerProgressManager] {levelTotal.LevelId}: {levelTotal.TotalExp}/{levelTotal.MaxExp} EXP, " +
                $"{levelTotal.TotalReputation}/{levelTotal.MaxReputation} hearts, passed: {levelTotal.Passed}, " +
                $"new best: {gain.newBest} (+{gain.expGain} EXP, +{gain.heartsGain} hearts to totals)."
            );
        }

        Debug.Log(
            $"[PlayerProgressManager] Applied level result. " +
            $"New totals -> EXP: {progress.totalExp}, " +
            $"Reputation: {progress.totalReputation}, " +
            $"Levels: {progress.levelsCompleted}"
        );

        Save();
    }

    /// <summary>The run mode in force (T03): a practice drill wins; a level loaded without a menu route is a direct Editor run.</summary>
    public static CampaignRunMode CurrentRunMode => CampaignRun.Resolve(CampaignRun.Chosen, TrainingSession.IsPracticing);

    // =========================================================
    // LEVEL 1 CASE RESULT (T07)
    // =========================================================

    public enum CaseResultCommit { NotStory, Committed, AlreadyRecorded, NotEligible, SaveFailed }

    /// <summary>True when this finished run is Level 1 of a story campaign (its result goes through CommitLevelOneCaseResult).</summary>
    public bool IsStoryCaseResult(LevelTotalResult levelTotal) =>
        levelTotal != null && levelTotal.LevelId == CampaignRecord.FirstChapterId && progress != null && progress.HasCampaign &&
        CampaignSave.CanContinue(campaignState) && CampaignRun.AllowsStoryWrites(CurrentRunMode);

    /// <summary>
    /// Records Level 1's first case result once: the result, its claim and the
    /// run's mastery record are applied to a candidate copy, written, and only
    /// then made live. The corrected quest is read from the saved checkpoint;
    /// reaching the level result means the case was approved. On a failed
    /// write nothing changes (no success is shown) and the caller may retry.
    /// </summary>
    public CaseResultCommit CommitLevelOneCaseResult(LevelTotalResult levelTotal, string caseId, string caseFingerprint)
    {
        if (!IsStoryCaseResult(levelTotal)) return CaseResultCommit.NotStory;
        if (progress.campaign.levelOneCaseResult != null && progress.campaign.levelOneCaseResult.recorded)
        {
            lastAppliedRunId = levelTotal.RunId;
            if (pendingResult == levelTotal) pendingResult = null;
            LastCaseCoins = LastLedger(CampaignRewards.ClaimId(progress.campaign.campaignId, CampaignRewards.Level1,
                CampaignRewards.CaseApproved, CampaignRewards.CoinsKind));
            return CaseResultCommit.AlreadyRecorded;
        }

        Level1CaseResult result = CaseResultRules.Build(CampaignRecord.FirstChapterId, progress.campaign.levelOne, caseId, caseFingerprint,
            caseApproved: true, levelTotal.TotalExp, levelTotal.MaxExp, levelTotal.TotalReputation, levelTotal.MaxReputation, levelTotal.RunSeconds);
        if (!CaseResultRules.CanRecord(true, result.questCorrected, result.caseApproved))
        {
            Debug.LogWarning("[PlayerProgressManager] Level 1 case result not recorded: the classification quest is not corrected in the saved story.");
            return CaseResultCommit.NotEligible;
        }

        PlayerProgressData candidate = CloneProgress();
        CaseResultRules.Outcome outcome = CaseResultRules.Apply(candidate, result, ToMasteryRun(levelTotal));
        if (outcome == CaseResultRules.Outcome.Rejected) return CaseResultCommit.NotEligible;
        if (outcome == CaseResultRules.Outcome.AlreadyRecorded) return CaseResultCommit.AlreadyRecorded;
        if (!TryWriteCandidate(candidate)) return CaseResultCommit.SaveFailed;

        lastAppliedRunId = levelTotal.RunId;
        if (pendingResult == levelTotal) pendingResult = null;
        LastCaseCoins = LastLedger(CampaignRewards.ClaimId(progress.campaign.campaignId, CampaignRewards.Level1,
            CampaignRewards.CaseApproved, CampaignRewards.CoinsKind));
        Debug.Log($"[PlayerProgressManager] Level 1 case result recorded ({result.actionId}): first check {result.firstCheckCorrect}/4, " +
                  $"tier {result.tier}, {result.exp}/{result.maxExp} EXP, wallet {progress.campaign.coins}. Next: {result.nextStep}.");
        return CaseResultCommit.Committed;
    }

    /// <summary>The coins granted with the case result (T09), for the result card; Amount 0 when none.</summary>
    public CampaignRewards.Change LastCaseCoins { get; private set; }

    // =========================================================
    // PUBLIC TRUST AND COINS (T08 / T09)
    // =========================================================

    /// <summary>What a committed story milestone changed (Saved false: nothing was written or shown).</summary>
    public struct StoryGrants
    {
        public bool Saved;
        public bool AlreadyDone;
        public CampaignRewards.Change Trust;
        public CampaignRewards.Change Coins;
    }

    /// <summary>Marie's follow-through is waiting (a recorded case result; T08).</summary>
    public bool FollowThroughPending => progress != null && CampaignSave.CanContinue(campaignState) &&
                                        !TrainingSession.IsPracticing && CaseResultRules.FollowThroughPending(progress);

    /// <summary>The approved Level 1 end flow still needs to run after the Client Copy handover (T08).</summary>
    public bool PendingLevelOneEndFlow => progress != null && progress.HasCampaign &&
                                          CampaignSave.CanContinue(campaignState) &&
                                          progress.campaign.levelOneEndFlowPending;

    /// <summary>
    /// The corrected Level 1 field assignment (the classification quest),
    /// story runs only: Public Trust +2 and 60 coins, each claimed once, with
    /// the activity, in one candidate write. questCorrected comes from the
    /// board owner (all four right). Already claimed: AlreadyDone, nothing changes.
    /// </summary>
    public StoryGrants CommitFieldAssignment(bool questCorrected)
    {
        var grants = new StoryGrants();
        if (!questCorrected || progress == null || !progress.HasCampaign || !CampaignSave.CanContinue(campaignState) ||
            !CampaignRun.AllowsStoryWrites(CurrentRunMode)) return grants;
        if (CampaignRewards.IsActivityCompleted(progress, CampaignRewards.Level1, CampaignRewards.FieldAssignment))
        {
            grants.Saved = grants.AlreadyDone = true;
            return grants;
        }
        PlayerProgressData candidate = CloneProgress();
        CampaignRewards.CompleteActivity(candidate, CampaignRewards.Level1, CampaignRewards.FieldAssignment);
        grants.Trust = CampaignRewards.Grant(candidate, CampaignRewards.Level1, CampaignRewards.FieldAssignment, CampaignRewards.TrustKind);
        grants.Coins = CampaignRewards.Grant(candidate, CampaignRewards.Level1, CampaignRewards.FieldAssignment, CampaignRewards.CoinsKind);
        grants.Saved = TryWriteCandidate(candidate);
        if (grants.Saved)
            Debug.Log($"[PlayerProgressManager] Field assignment completed: trust {grants.Trust.After}/100, coins {grants.Coins.After}.");
        return grants;
    }

    /// <summary>
    /// Marie's follow-through (T08): Public Trust +4 (claimed once), the
    /// activity and the pending end-flow marker, in one candidate write
    /// (Chapter 1 completes later, at its wrap-up: CommitLevelOneWrapUp). Called once the Client Copy is handed to Marie
    /// (ClientCopyHandover); only a recorded story case result makes it pending.
    /// </summary>
    public StoryGrants CommitFollowThrough()
    {
        var grants = new StoryGrants();
        if (progress == null || !progress.HasCampaign || !CampaignSave.CanContinue(campaignState) ||
            !CampaignRun.AllowsStoryWrites(CurrentRunMode) || TrainingSession.IsPracticing) return grants;
        if (!FollowThroughPending)
        {
            grants.Saved = grants.AlreadyDone = progress != null && progress.HasCampaign &&
                CampaignRewards.IsActivityCompleted(progress, CampaignRewards.Level1, CampaignRewards.FollowThrough);
            return grants;
        }
        PlayerProgressData candidate = CloneProgress();
        grants.Trust = CaseResultRules.CompleteFollowThrough(candidate);
        grants.Saved = TryWriteCandidate(candidate);
        if (grants.Saved)
            Debug.Log($"[PlayerProgressManager] Marie's follow-through done: trust {grants.Trust.After}/100; the Chapter 1 wrap-up is next.");
        return grants;
    }

    /// <summary>
    /// The Chapter 1 wrap-up (T16), after the finale/result card: the chapter's
    /// completion, the wrap-up activity and the end of the pending marker and
    /// Level 1 checkpoint, in one candidate write. Story runs only, after Marie's
    /// follow-through. A failed write leaves everything for the player to resume
    /// and retry (no success sheet is shown).
    /// </summary>
    public bool CommitLevelOneWrapUp()
    {
        if (progress == null || !progress.HasCampaign || !CampaignSave.CanContinue(campaignState) ||
            !CampaignRun.AllowsStoryWrites(CurrentRunMode) || TrainingSession.IsPracticing ||
            !CaseResultRules.WrapUpPending(progress)) return false;

        PlayerProgressData candidate = CloneProgress();
        if (!CaseResultRules.CompleteWrapUp(candidate)) return false;
        bool saved = TryWriteCandidate(candidate);
        if (saved) Debug.Log("[PlayerProgressManager] Chapter 1 wrap-up: chapter completed.");
        return saved;
    }

    /// <summary>The wrap-up sheet's committed facts (earned coins and trust, wallet, tier).</summary>
    public CaseResultRules.ChapterSummary LevelOneSummary() => CaseResultRules.BuildChapterSummary(progress);

    /// <summary>The ledger line of a claim as a Change (Amount 0 when it is not there).</summary>
    private CampaignRewards.Change LastLedger(string claimId)
    {
        var change = new CampaignRewards.Change { Outcome = CampaignRewards.Outcome.AlreadyClaimed };
        if (progress == null || !progress.HasCampaign) return change;
        CampaignLedgerEntry entry = progress.campaign.ledger.FindLast(e => e.claimId == claimId);
        if (entry == null) return change;
        change.Outcome = CampaignRewards.Outcome.Applied;
        change.Kind = entry.kind;
        change.Amount = entry.amount;
        change.After = entry.balanceAfter;
        change.Before = entry.balanceAfter - entry.amount;
        change.Reason = entry.reason;
        return change;
    }

    // =========================================================
    // INVENTORY (T10): hotbar and bag
    // =========================================================

    /// <summary>Raised after an inventory change is committed (story: written; other runs: kept in memory).</summary>
    public static event Action InventoryChanged;

    private CampaignInventoryRecord sessionInventory;
    private UnityEngine.SceneManagement.Scene sessionInventoryScene;

    /// <summary>Story runs of a committed campaign use the saved inventory; practice, replays and Editor runs get a temporary one per scene.</summary>
    public bool UsesStoryInventory => progress != null && progress.HasCampaign && CampaignSave.CanContinue(campaignState) &&
                                      CampaignRun.AllowsStoryWrites(CurrentRunMode) && !TrainingSession.IsPracticing;

    /// <summary>
    /// T13: the 3D Menu between runs may spend the campaign's coins in the
    /// same catalogue (no run is chosen, nothing is launching, no drill).
    /// Level scenes still need a story run (UsesStoryInventory).
    /// </summary>
    public bool CanShopFromMenu => progress != null && progress.HasCampaign && CampaignSave.CanContinue(campaignState) &&
                                   CampaignRun.Chosen == CampaignRunMode.None && !TrainingSession.IsPracticing && !MenuEntry.IsStoryLaunching &&
                                   UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == MenuEntry.MenuSceneName;

    /// <summary>Purchases may spend real coins: a story run, or the menu between runs (T13). Anything else only browses.</summary>
    public bool CanSpendCoins => UsesStoryInventory || CanShopFromMenu;

    /// <summary>The live inventory for this run. Observers only read it; changes go through CommitInventory.</summary>
    public CampaignInventoryRecord Inventory
    {
        get
        {
            if (UsesStoryInventory) return progress.campaign.inventory;
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (sessionInventory == null || sessionInventoryScene != scene)
            {
                // A temporary run never sees or spends the story's items.
                sessionInventory = new CampaignInventoryRecord();
                sessionInventoryScene = scene;
            }
            return sessionInventory;
        }
    }

    /// <summary>
    /// Runs one inventory command (InventoryRules) on a candidate copy. A
    /// refusal or no-change returns at once; a change is written first in a
    /// story run (SaveFailed on a failed write, nothing changes) and only then
    /// becomes the live inventory and raises InventoryChanged.
    /// </summary>
    public InventoryResult CommitInventory(Func<CampaignInventoryRecord, InventoryResult> command)
    {
        if (command == null) return InventoryResult.Reject("No command");
        CampaignInventoryRecord candidate = JsonUtility.FromJson<CampaignInventoryRecord>(JsonUtility.ToJson(Inventory));
        InventoryResult result = command(candidate) ?? InventoryResult.Reject("No result");
        if (!result.Changed) return result;
        candidate.Repair();
        if (UsesStoryInventory)
        {
            PlayerProgressData next = CloneProgress();
            next.campaign.inventory = candidate;
            if (!TryWriteCandidate(next))
            {
                Debug.LogWarning($"[PlayerProgressManager] Inventory change for '{result.ItemId}' not saved; nothing changed.");
                result.Outcome = InventoryOutcome.SaveFailed;
                return result;
            }
        }
        else sessionInventory = candidate;
        try { InventoryChanged?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
        return result;
    }

    // ---------- world pickups (T11) ----------

    private readonly System.Collections.Generic.HashSet<string> sessionClaims =
        new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
    private UnityEngine.SceneManagement.Scene sessionClaimsScene;

    private System.Collections.Generic.HashSet<string> SessionClaims
    {
        get
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (sessionClaimsScene != scene)
            {
                sessionClaims.Clear();
                sessionClaimsScene = scene;
            }
            return sessionClaims;
        }
    }

    private string ClaimCampaignId => UsesStoryInventory ? progress.campaign.campaignId : "temporary";

    /// <summary>The once-per-campaign claim id of a world pickup (PickupRules format).</summary>
    public string PickupClaimId(string levelId, string pickupId) => PickupRules.ClaimId(ClaimCampaignId, levelId, pickupId);

    /// <summary>The claim id of a collected taxpayer profile card.</summary>
    public string ProfileClaimId(string levelId, string profileId) => PickupRules.ProfileClaimId(ClaimCampaignId, levelId, profileId);

    /// <summary>Story: claimed in this campaign. Other runs: claimed in this temporary run.</summary>
    public bool IsPickupClaimed(string claimId) =>
        !string.IsNullOrEmpty(claimId) &&
        (UsesStoryInventory ? progress.campaign.rewardClaims.Contains(claimId) : SessionClaims.Contains(claimId));

    /// <summary>
    /// T11: one world pickup as one transaction: the inventory command and its
    /// claim are applied to a candidate and written together before anything
    /// is published (a failed write changes neither, SaveFailed). onceOnly: an
    /// existing claim refuses (AlreadyApplied) and nothing is minted again.
    /// Optional pickups grant no coins, trust, XP or badge.
    /// </summary>
    public InventoryResult CommitPickup(string claimId, bool onceOnly, Func<CampaignInventoryRecord, InventoryResult> command)
    {
        if (command == null || string.IsNullOrEmpty(claimId)) return InventoryResult.Reject("No pickup");
        bool claimed = IsPickupClaimed(claimId);
        if (claimed && onceOnly) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied };
        CampaignInventoryRecord inventory = JsonUtility.FromJson<CampaignInventoryRecord>(JsonUtility.ToJson(Inventory));
        InventoryResult result = command(inventory) ?? InventoryResult.Reject("No result");
        if (!result.Ok && !result.Changed) return result;
        if (!result.Changed && claimed) return result;
        inventory.Repair();
        if (UsesStoryInventory)
        {
            PlayerProgressData candidate = CloneProgress();
            candidate.campaign.inventory = inventory;
            if (!claimed) candidate.campaign.rewardClaims.Add(claimId);
            if (!TryWriteCandidate(candidate))
            {
                Debug.LogWarning($"[PlayerProgressManager] Pickup '{claimId}' not saved; nothing changed.");
                result.Outcome = InventoryOutcome.SaveFailed;
                return result;
            }
        }
        else
        {
            sessionInventory = inventory;
            SessionClaims.Add(claimId);
        }
        try { InventoryChanged?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
        return result;
    }

    // ---------- Supply Counter (T12) ----------

    /// <summary>Raised after a purchase is written (the wallet and the inventory changed together).</summary>
    public static event Action<PurchaseResult> PurchaseCommitted;

    /// <summary>The story's shop is open (the Supply Counter was introduced).</summary>
    public bool ShopIntroduced => progress != null && progress.HasCampaign && progress.campaign.shopIntroduced;

    /// <summary>
    /// The first visit to the Supply Counter in a story run introduces the
    /// shop (one write; nothing else changes). Temporary runs never write it.
    /// </summary>
    public bool MarkShopIntroduced()
    {
        if (!UsesStoryInventory) return false;
        if (progress.campaign.shopIntroduced) return true;
        PlayerProgressData candidate = CloneProgress();
        candidate.campaign.shopIntroduced = true;
        return TryWriteCandidate(candidate);
    }

    /// <summary>
    /// One purchase as one transaction (ShopRules.Purchase on a candidate):
    /// the debit, the ledger line, ownership, the receipt claim and the new
    /// capacity or item are written together before anything is shown.
    /// Practice, replays and Editor runs only browse (refused, nothing spent).
    /// The menu between runs spends too (T13); with no hand to hold an item
    /// there, a full bar and bag refuse the purchase.
    /// A failed write changes nothing (SaveFailed); retrying with the same
    /// request id after success is AlreadyApplied (never a second charge).
    /// </summary>
    public PurchaseResult CommitPurchase(ShopCatalogue catalogue, InventoryCatalogue items, string productId, string requestId)
    {
        if (!CanSpendCoins)
            return new PurchaseResult { ProductId = productId ?? "", Message = ShopRules.ReasonFor(ShopStatus.BrowseOnly, null, null, null) };
        PlayerProgressData candidate = CloneProgress();
        PurchaseResult result = ShopRules.Purchase(candidate, catalogue, items, productId, requestId, noHand: !UsesStoryInventory);
        if (result.Outcome != InventoryOutcome.Applied) return result;
        if (!TryWriteCandidate(candidate))
        {
            Debug.LogWarning($"[PlayerProgressManager] Purchase of '{productId}' not saved; nothing spent.");
            return new PurchaseResult
            {
                Outcome = InventoryOutcome.SaveFailed, ProductId = productId, Message = "Purchase not saved; nothing spent. Try again.",
                CoinsBefore = result.CoinsBefore, CoinsAfter = result.CoinsBefore
            };
        }
        Debug.Log($"[PlayerProgressManager] Bought '{productId}': coins {result.CoinsBefore} -> {result.CoinsAfter}.");
        try { InventoryChanged?.Invoke(); PurchaseCommitted?.Invoke(result); }
        catch (Exception e) { Debug.LogException(e); }
        return result;
    }

    // ---------- review vouchers (T14) ----------

    /// <summary>The once-per-case claim of a voucher use (VoucherRules format; "temporary" outside the story).</summary>
    public string VoucherClaimId(string levelId, string caseId) => VoucherRules.UseClaimId(ClaimCampaignId, levelId, caseId);

    /// <summary>Story: used on this case in this campaign. Other runs: used in this temporary run.</summary>
    public bool IsVoucherUsed(string levelId, string caseId) => IsPickupClaimed(VoucherClaimId(levelId, caseId));

    /// <summary>
    /// T14: one voucher use as one transaction. Story: one carried voucher
    /// out, the per-case claim in and the Level 1 checkpoint with the extended
    /// clock, written together before anything is shown (a failed write
    /// changes nothing: SaveFailed, safe to retry; a repeat finds the claim).
    /// Practice, replays and Editor runs use their temporary supplies and a
    /// claim for this visit only. No coins, trust, XP or tax date change.
    /// </summary>
    public VoucherUseResult CommitVoucherUse(InventoryCatalogue items, string levelId, string caseId, Level1FieldCheckpoint checkpoint, string actionId) =>
        CommitVoucher(items, levelId, caseId, actionId, candidate =>
        {
            if (checkpoint == null || !UsesLevelOneCheckpoint) return;
            Level1FieldCheckpoint previous = candidate.campaign.levelOne;
            checkpoint.version = Level1FieldCheckpoint.CurrentVersion;
            checkpoint.writes = (previous != null && previous.IsSet ? previous.writes : 0) + 1;
            candidate.campaign.levelOne = checkpoint;
        });

    /// <summary>
    /// T18: a voucher on Rosa's review. Story: the voucher, the per-case claim
    /// and the Level 2 checkpoint holding the extended clock in one write (a
    /// failed write changes nothing). Other runs: temporary supplies and a claim
    /// for this visit only, as in Level 1.
    /// </summary>
    public VoucherUseResult CommitLevelTwoVoucherUse(InventoryCatalogue items, string caseId, LevelCheckpoint checkpoint, string actionId) =>
        CommitVoucher(items, Level2CampaignRules.LevelId, caseId, actionId, candidate =>
        {
            if (checkpoint != null && checkpoint.IsSet && UsesLevelTwoStory) candidate.SetCheckpoint(checkpoint);
        });

    private VoucherUseResult CommitVoucher(InventoryCatalogue items, string levelId, string caseId, string actionId, Action<PlayerProgressData> attachCheckpoint)
    {
        if (UsesStoryInventory)
        {
            PlayerProgressData candidate = CloneProgress();
            VoucherUseResult result = VoucherRules.Use(candidate.campaign, items, levelId, caseId, actionId);
            if (result.Outcome != InventoryOutcome.Applied) return result;
            attachCheckpoint?.Invoke(candidate);
            if (!TryWriteCandidate(candidate))
            {
                Debug.LogWarning($"[PlayerProgressManager] Voucher use on '{caseId}' not saved; nothing changed.");
                return new VoucherUseResult
                {
                    Outcome = InventoryOutcome.SaveFailed, ClaimId = result.ClaimId,
                    Message = "The voucher was not used: that could not be saved. Nothing changed; try again."
                };
            }
            try { InventoryChanged?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
            return result;
        }

        string claim = VoucherClaimId(levelId, caseId);
        if (SessionClaims.Contains(claim))
            return new VoucherUseResult { Outcome = InventoryOutcome.AlreadyApplied, ClaimId = claim, Message = VoucherRules.ReasonFor(VoucherUseStatus.AlreadyUsed) };
        CampaignInventoryRecord inventory = JsonUtility.FromJson<CampaignInventoryRecord>(JsonUtility.ToJson(Inventory));
        InventoryResult used = InventoryRules.ConsumeCarried(inventory, items, InventoryCatalogue.ReviewVoucher, actionId);
        if (used.Outcome != InventoryOutcome.Applied)
            return new VoucherUseResult { ClaimId = claim, Message = VoucherRules.ReasonFor(VoucherUseStatus.NoVoucher) };
        sessionInventory = inventory;
        SessionClaims.Add(claim);
        try { InventoryChanged?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
        return new VoucherUseResult { Outcome = InventoryOutcome.Applied, ClaimId = claim, From = used.Location };
    }

    /// <summary>
    /// T13: the office has shown a decor piece's arrival (its lift-and-settle),
    /// so later visits show it in place. Presentation only; story runs only.
    /// </summary>
    public bool MarkDecorArrived(string itemId)
    {
        if (!UsesStoryInventory) return false;
        PlayerProgressData candidate = CloneProgress();
        if (!DecorRules.MarkArrived(candidate.campaign, itemId)) return true;
        return TryWriteCandidate(candidate);
    }

    /// <summary>The profile ids the campaign's Profile Binder holds (story runs; empty otherwise).</summary>
    public System.Collections.Generic.List<string> CollectedProfileIds()
    {
        var ids = new System.Collections.Generic.List<string>();
        if (!UsesStoryInventory) return ids;
        InventoryItemRecord binder = InventoryRules.Record(progress.campaign.inventory, InventoryCatalogue.ProfileBinder);
        if (binder != null && InventoryRules.Owns(progress.campaign.inventory, InventoryCatalogue.ProfileBinder)) ids.AddRange(binder.contents);
        return ids;
    }

    /// <summary>Applies the item catalogue to the loaded inventory once per run (unknown ids, stacks, required items).</summary>
    public bool NormalizeInventory(InventoryCatalogue catalogue)
    {
        var warnings = new System.Collections.Generic.List<string>();
        InventoryResult result = CommitInventory(r => new InventoryResult
        {
            Outcome = InventoryRules.Normalize(r, catalogue, warnings) ? InventoryOutcome.Applied : InventoryOutcome.NoChange
        });
        foreach (string warning in warnings) Debug.LogWarning("[PlayerProgressManager] " + warning);
        return result.Outcome != InventoryOutcome.SaveFailed;
    }

    private PlayerProgressData CloneProgress() => JsonUtility.FromJson<PlayerProgressData>(JsonUtility.ToJson(progress));

    /// <summary>Writes a complete candidate; only after the write succeeds does it become the live progress.</summary>
    private bool TryWriteCandidate(PlayerProgressData candidate)
    {
        if (candidate == null || store == null || !CampaignSave.AllowsOrdinarySave(campaignState)) return false;
        try
        {
            candidate.Normalize();
            store.Write(Encode(candidate));
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerProgressManager] Save failed: {e.Message}");
            return false;
        }
        progress = candidate;
        RememberSavedCompletion();
        try { ProgressChanged?.Invoke(progress); }
        catch (Exception e) { Debug.LogException(e); }
        return true;
    }

    /// <summary>
    /// Before a story run leaves its scene (menu / replay): save any waiting
    /// result and the progress, and report whether that worked. Other run modes
    /// have nothing of the story to save. On false the caller stays in the run.
    /// </summary>
    public bool SaveBeforeLeaving()
    {
        if (!CampaignRun.AllowsStoryWrites(CurrentRunMode)) return true;
        if (pendingResult != null) FlushPending("leaving the level");
        // T18: a story result whose commit failed is still pending: stay and retry.
        if (pendingResult != null && IsLevelTwoStoryResult(pendingResult)) return false;
        return Save();
    }

    /// <summary>The mastery boundary's plain view of a finished level (TotalReputation = accuracy score hearts).</summary>
    private static MasteryRun ToMasteryRun(LevelTotalResult levelTotal) => new MasteryRun
    {
        LevelId = levelTotal.LevelId,
        HasScore = levelTotal.HasScore,
        Exp = levelTotal.TotalExp,
        Hearts = levelTotal.TotalReputation,
        MaxExp = levelTotal.MaxExp,
        MaxHearts = levelTotal.MaxReputation,
        Passed = levelTotal.Passed,
        Badges = levelTotal.Badges,
        BadgeTiers = levelTotal.BadgeTiers?.ConvertAll(tier => (int)tier),
        RunSeconds = levelTotal.RunSeconds,
        SideQuestSeconds = levelTotal.SideQuestSeconds
    };

    /// <summary>A Training Hub practice run never reaches the career (EXP, hearts, badges, levels).</summary>
    private bool IgnoredForPractice(LevelTotalResult levelTotal)
    {
        if (!TrainingSession.IsPracticing) return false;
        Debug.Log($"[PlayerProgressManager] Practice run of {levelTotal.LevelId}: not saved to the career.");
        if (pendingResult == levelTotal) pendingResult = null;
        return true;
    }

    // =========================================================
    // LEVEL CHECKPOINT AND STORY FACTS (Level 2)
    // =========================================================

    /// <summary>Saves the resumable state of an unfinished level run (signed, atomic, same save file).</summary>
    /// <summary>Clear only the unfinished attempt; retain earned career results.</summary>
    public bool ClearCheckpoint(string levelId)
    {
        LevelCheckpoint previous = progress.CheckpointFor(levelId);
        progress.ClearCheckpoint(levelId);
        if (Save()) return true;
        if (previous != null) progress.SetCheckpoint(previous);
        return false;
    }

    /// <summary>Confirmed fresh start. Replace both save generations so recovery cannot resurrect old progress.</summary>
    public bool ResetProgress()
    {
        var fresh = new PlayerProgressData();
        fresh.Normalize();
        try
        {
            string encoded = Encode(fresh);
            store.Reset(encoded);
        }
        catch (Exception error)
        {
            Debug.LogError("[PlayerProgressManager] Reset failed: " + error.Message);
            return false;
        }
        Publish(fresh, CampaignSave.Classify(true, fresh));
        return true;
    }

    /// <summary>
    /// Confirmed New Game (T02): builds the complete first campaign state, writes
    /// it as the main save and the backup, and only then makes it the current
    /// progress. Nothing from any earlier save is carried over; settings are not
    /// touched. On failure the previous files are put back, the current progress
    /// stays as it was and the error can be shown; trying again is safe.
    /// </summary>
    public bool StartNewCampaign(out string error)
    {
        error = null;
        if (store == null)
        {
            error = "The save is not ready.";
            return false;
        }

        PlayerProgressData candidate = PlayerProgressData.CreateNewCampaign(
            Guid.NewGuid().ToString("N"), DateTime.UtcNow.ToString("o"));

        SaveFileStore.Generations previous;
        try
        {
            previous = store.Capture();
        }
        catch (Exception e)
        {
            error = "Could not read the current save (" + e.Message + ").";
            Debug.LogError("[PlayerProgressManager] New Game not started: " + error);
            return false;
        }

        if (!CampaignSave.TryCommit(candidate, Encode, store.Reset, () => store.Restore(previous), out error))
        {
            Debug.LogError("[PlayerProgressManager] New Game not started: " + error);
            return false;
        }

        Publish(candidate, CampaignSaveState.Campaign);
        Debug.Log($"[PlayerProgressManager] New campaign {candidate.campaign.campaignId} started.");
        return true;
    }

    /// <summary>Makes a durably written replacement the current progress (reset or New Game).</summary>
    private void Publish(PlayerProgressData fresh, CampaignSaveState state)
    {
        progress = fresh;
        campaignState = state;
        RememberSavedCompletion();
        pendingResult = null;
        lastAppliedRunId = null;
        PlayerPrefs.DeleteKey("TaxVenture.Career.Seen.v1");
        PlayerPrefs.DeleteKey("TaxVenture.Guide.Level_01.finished");
        PlayerPrefs.DeleteKey("TaxVenture.Guide.Level_02.finished");
        PlayerPrefs.Save();
        TrainingSession.End();
        CareerProgressService.ClearCache();
        try { ProgressChanged?.Invoke(progress); }
        catch (Exception error) { Debug.LogException(error); }
    }

    public bool SaveCheckpoint(LevelCheckpoint checkpoint)
    {
        // Practice drills and mastery replays never replace the story's resumable checkpoint.
        if (progress == null || checkpoint == null || !checkpoint.IsSet || TrainingSession.IsPracticing ||
            CurrentRunMode == CampaignRunMode.MasteryReplay) return false;
        // T18: written as a candidate, so a failed write leaves the previous checkpoint in memory and on disk.
        PlayerProgressData candidate = CloneProgress();
        candidate.SetCheckpoint(checkpoint);
        return TryWriteCandidate(candidate);
    }

    /// <summary>The resumable run of this level, or null.</summary>
    public LevelCheckpoint GetCheckpoint(string levelId) => progress != null ? progress.CheckpointFor(levelId) : null;

    // =========================================================
    // LEVEL 2 IN THE CAMPAIGN (T18)
    // =========================================================

    /// <summary>Rosa's review as the campaign rules see it, read from the live review (set by Level2Checkpointer).</summary>
    public static Func<Level2CaseFacts> LevelTwoFacts;

    /// <summary>A story run of a committed campaign: Level 2's checkpoint, claims and supplies are the campaign's own.</summary>
    public bool UsesLevelTwoStory => progress != null && progress.HasCampaign && CampaignSave.CanContinue(campaignState) &&
                                     CampaignRun.AllowsStoryWrites(CurrentRunMode) && !TrainingSession.IsPracticing;

    public bool IsLevelTwoStoryResult(LevelTotalResult levelTotal) =>
        levelTotal != null && levelTotal.LevelId == Level2CampaignRules.LevelId && UsesLevelTwoStory;

    /// <summary>What the last committed Level 2 story write granted (presentation only).</summary>
    public Level2CampaignRules.Grants LastLevelTwoGrants { get; private set; }

    /// <summary>Rosa's follow-through is waiting for the approved case (T19/T33 deliver it at Rosa).</summary>
    public bool LevelTwoFollowThroughPending => UsesLevelTwoStory && Level2CampaignRules.FollowThroughPending(progress);

    /// <summary>
    /// Commits Rosa's one T19 choice and review checkpoint atomically. In
    /// isolated runs only the transient review snapshot changes; campaign
    /// trust/history remains untouched.
    /// </summary>
    public bool CommitLevelTwoEthicsChoice(Level2EthicsChoice choice, string caseId, out CampaignRewards.Change trust)
    {
        trust = default;
        Level2Checkpointer checkpointOwner = Level2Checkpointer.Active;
        if (checkpointOwner == null) return false;
        if (!UsesLevelTwoStory || Level2Checkpointer.IsDirectEditorAttempt)
            return checkpointOwner.CommitLocalEthicsChoice(choice, caseId);

        LevelCheckpoint checkpoint = checkpointOwner.CaptureEthicsChoice(choice, caseId);
        if (checkpoint == null || !checkpoint.IsSet) return false;
        PlayerProgressData candidate = CloneProgress();
        candidate.SetCheckpoint(checkpoint);
        int before = progress.campaign.publicTrust;
        if (!Level2CampaignRules.CommitEthicsChoice(candidate, choice, caseId)) return false;
        if (!TryWriteCandidate(candidate)) return false;

        checkpointOwner.PublishEthicsChoice(choice, caseId);
        trust = new CampaignRewards.Change
        {
            Outcome = CampaignRewards.Outcome.Applied,
            Kind = CampaignRewards.TrustKind,
            Before = before,
            After = progress.campaign.publicTrust,
            Amount = progress.campaign.publicTrust - before,
            Reason = "Rosa's promise was recorded"
        };
        LastLevelTwoGrants = new Level2CampaignRules.Grants { Changed = true, Trust = trust };
        Debug.Log($"[PlayerProgressManager] Rosa's ethics choice saved: {caseId}, trust {trust.Before}->{trust.After}/100.");
        return true;
    }

    /// <summary>
    /// One Level 2 milestone: the checkpoint and the claims it has earned (the
    /// corrected income review: +2 trust, 60 coins once) go into one candidate
    /// and one write. Outside a story run only the checkpoint is kept, as
    /// before (never claims). A failed write changes nothing; the next save retries.
    /// </summary>
    public StoryGrants CommitLevelTwoCheckpoint(LevelCheckpoint checkpoint, Level2CaseFacts facts)
    {
        var grants = new StoryGrants();
        if (!UsesLevelTwoStory)
        {
            grants.Saved = SaveCheckpoint(checkpoint);
            return grants;
        }
        if (checkpoint == null || !checkpoint.IsSet) return grants;
        PlayerProgressData candidate = CloneProgress();
        candidate.SetCheckpoint(checkpoint);
        Level2CampaignRules.Grants due = Level2CampaignRules.ApplyCheckpointClaims(candidate, facts);
        grants.Saved = TryWriteCandidate(candidate);
        if (!grants.Saved) return grants;
        grants.Trust = due.Trust;
        grants.Coins = due.Coins;
        if (due.Changed)
        {
            LastLevelTwoGrants = due;
            Debug.Log($"[PlayerProgressManager] Rosa's income review completed: trust {progress.campaign.publicTrust}/100, coins {progress.campaign.coins}.");
        }
        return grants;
    }

    /// <summary>
    /// Rosa's approved review (the level result of a story run): the case
    /// claim (100 coins once, approved and corrected only), the mastery record
    /// (frozen first-check tier) and the end of the checkpoint, in one write.
    /// True when saved now or earlier. On a failed write nothing changes, the
    /// result stays pending (SaveBeforeLeaving keeps the player on the card) and
    /// trying again is safe. Chapter 2 completes only after the follow-through.
    /// </summary>
    public bool CommitLevelTwoResult(LevelTotalResult levelTotal) => CommitLevelTwoResult(levelTotal, out _);

    /// <summary>As above; wroteNow is true only for the write that recorded it (presentation shows the grant once).</summary>
    public bool CommitLevelTwoResult(LevelTotalResult levelTotal, out bool wroteNow)
    {
        wroteNow = false;
        if (!IsLevelTwoStoryResult(levelTotal)) return false;
        if (levelTotal.RunId == lastAppliedRunId) return true;
        Level2CaseFacts facts = LevelTwoFacts != null ? LevelTwoFacts() : default;
        LevelCheckpoint approvedCopy = null;
        bool copyAlreadyDelivered = Level2CampaignRules.FollowThroughDone(progress);
        if (facts.Approved && !copyAlreadyDelivered)
        {
            approvedCopy = Level2Checkpointer.Active != null ? Level2Checkpointer.Active.CaptureApprovedClientCopy() : null;
            if (approvedCopy == null || !approvedCopy.IsSet)
            {
                pendingResult = levelTotal;
                Debug.LogWarning("[PlayerProgressManager] Rosa's approved case has no resumable Client Copy checkpoint; the result stays pending for a retry.");
                return false;
            }
        }
        PlayerProgressData candidate = CloneProgress();
        Level2CampaignRules.Grants grants = Level2CampaignRules.ApplyCaseResult(candidate, facts);
        // The approved review and copy state are one write, closing the crash gap before the copy spawns.
        if (facts.Approved && copyAlreadyDelivered) candidate.ClearCheckpoint(levelTotal.LevelId);
        else if (facts.Approved) candidate.SetCheckpoint(approvedCopy);
        else Debug.LogWarning("[PlayerProgressManager] Level 2 result without an approved review: no case claim; the checkpoint is kept.");
        MasteryBoundary.ApplyRun(candidate, ToMasteryRun(levelTotal));
        if (!TryWriteCandidate(candidate))
        {
            pendingResult = levelTotal;
            Debug.LogWarning("[PlayerProgressManager] Rosa's case result not saved; nothing changed. It stays pending for a retry.");
            return false;
        }
        lastAppliedRunId = levelTotal.RunId;
        if (pendingResult == levelTotal) pendingResult = null;
        LastLevelTwoGrants = grants;
        wroteNow = true;
        Debug.Log($"[PlayerProgressManager] Rosa's case result recorded: coins {progress.campaign.coins}, trust {progress.campaign.publicTrust}/100, " +
                  $"follow-through pending {Level2CampaignRules.FollowThroughPending(progress)}.");
        return true;
    }

    /// <summary>
    /// Rosa's required follow-through (+4 trust once, after the saved approved
    /// case); completes Chapter 2 when everything required is done. T19/T33 call
    /// it from Rosa's follow-through conversation. Story runs only; a failed
    /// write changes nothing (Saved false) and a repeat is AlreadyDone.
    /// </summary>
    public StoryGrants CommitLevelTwoFollowThrough()
    {
        var grants = new StoryGrants();
        if (!UsesLevelTwoStory) return grants;
        if (Level2Checkpointer.IsDirectEditorAttempt) return grants;
        RosaEthicsRecord ethics = progress.campaign.rosaEthics;
        bool ethicsPending = ethics != null && ethics.committed && ethics.followThroughRequired && !ethics.followThroughDone;
        if (Level2CampaignRules.FollowThroughDone(progress) && !ethicsPending)
        {
            grants.Saved = grants.AlreadyDone = true;
            return grants;
        }
        PlayerProgressData candidate = CloneProgress();
        int trustBefore = progress.campaign.publicTrust;
        Level2CampaignRules.Grants due = Level2CampaignRules.ApplyFollowThrough(candidate);
        if (!due.Changed) return grants;
        // The checkpoint kept only for the Client Copy ends in the same write (nothing left to resume).
        candidate.ClearCheckpoint(Level2CampaignRules.LevelId);
        grants.Saved = TryWriteCandidate(candidate);
        if (!grants.Saved) return grants;
        grants.Trust = due.Trust;
        grants.Trust.Before = trustBefore;
        grants.Trust.After = progress.campaign.publicTrust;
        grants.Trust.Amount = grants.Trust.After - trustBefore;
        grants.Trust.Outcome = CampaignRewards.Outcome.Applied;
        grants.Trust.Reason = "Rosa's records follow-through";
        LastLevelTwoGrants = due;
        Level2Checkpointer.Active?.PublishEthicsFollowThrough();
        Debug.Log($"[PlayerProgressManager] Rosa's follow-through done: trust {progress.campaign.publicTrust}/100; chapter complete {due.ChapterCompleted}.");
        return grants;
    }

    // =========================================================
    // LEVEL 1 FIELD CHECKPOINT (T04, inside the v5 campaign)
    // =========================================================

    /// <summary>Only a story run of a committed campaign keeps the Level 1 field checkpoint.</summary>
    public bool UsesLevelOneCheckpoint =>
        progress != null && progress.HasCampaign && CampaignSave.CanContinue(campaignState) &&
        CampaignRun.AllowsStoryCheckpoint(CurrentRunMode) &&
        // T07: once the case result is recorded the case work is over; T08: the
        // checkpoint still keeps the Client Copy handover until Marie has her copy.
        (progress.campaign.levelOneCaseResult == null || !progress.campaign.levelOneCaseResult.recorded ||
         CaseResultRules.FollowThroughPending(progress) || progress.campaign.levelOneEndFlowPending);

    /// <summary>Where Marie's Client Copy is in the saved story (T08; None outside a story campaign).</summary>
    public ClientCopyPhase SavedClientCopyPhase =>
        UsesLevelOneCheckpoint ? ClientCopyRules.PhaseOf(progress) : ClientCopyPhase.None;

    /// <summary>
    /// T08: this story run ends Level 1's case with the Client Copy handover
    /// (Marie's follow-through not done yet), so the finale and the case
    /// result card wait for it. False for practice, replays and Editor runs.
    /// </summary>
    public bool ClientCopyHandoverExpected =>
        progress != null && progress.HasCampaign && CampaignSave.CanContinue(campaignState) &&
        CampaignRun.AllowsStoryWrites(CurrentRunMode) && !TrainingSession.IsPracticing &&
        !CampaignRewards.IsActivityCompleted(progress, CampaignRewards.Level1, CampaignRewards.FollowThrough);

    /// <summary>
    /// Saves the complete candidate as the campaign's Level 1 field checkpoint.
    /// On a failed write the previous checkpoint is put back in memory (the
    /// file still holds it), nothing is reported as saved, and the caller may retry.
    /// </summary>
    public bool SaveLevelOneCheckpoint(Level1FieldCheckpoint candidate)
    {
        if (candidate == null || !UsesLevelOneCheckpoint) return false;
        Level1FieldCheckpoint previous = progress.campaign.levelOne;
        candidate.version = Level1FieldCheckpoint.CurrentVersion;
        candidate.writes = (previous != null && previous.IsSet ? previous.writes : 0) + 1;
        progress.campaign.levelOne = candidate;
        if (Save()) return true;
        progress.campaign.levelOne = previous;
        return false;
    }

    /// <summary>A copy of the saved Level 1 field checkpoint (null when there is none or this run does not use it).</summary>
    public Level1FieldCheckpoint GetLevelOneCheckpoint()
    {
        if (!UsesLevelOneCheckpoint || progress.campaign.levelOne == null || !progress.campaign.levelOne.IsSet) return null;
        return JsonUtility.FromJson<Level1FieldCheckpoint>(JsonUtility.ToJson(progress.campaign.levelOne));
    }

    /// <summary>Replay from the start: drops the Level 1 field checkpoint (restored in memory if the write fails).</summary>
    public bool ClearLevelOneCheckpoint()
    {
        if (progress != null && progress.HasCampaign && CampaignRun.AllowsStoryWrites(CurrentRunMode) &&
            (FollowThroughPending || PendingLevelOneEndFlow)) return false;
        if (!UsesLevelOneCheckpoint) return true;
        Level1FieldCheckpoint previous = progress.campaign.levelOne;
        if (previous == null || !previous.IsSet) return true;
        progress.campaign.levelOne = new Level1FieldCheckpoint();
        if (Save()) return true;
        progress.campaign.levelOne = previous;
        return false;
    }

    /// <summary>Remember a fully viewed first intro in the existing signed save.</summary>
    public bool MarkLevelOneIntroSeen()
    {
        if (progress.levelOneIntroSeen) return true;
        progress.levelOneIntroSeen = true;
        if (Save()) return true;
        progress.levelOneIntroSeen = false;
        return false;
    }

    /// <summary>Records a neutral story fact once and saves.</summary>
    public bool RecordStoryFact(string id)
    {
        if (progress == null || TrainingSession.IsPracticing || !progress.RecordStoryFact(id)) return false;
        Save();
        return true;
    }

    // =========================================================
    // TRAINING HUB
    // =========================================================

    /// <summary>Records an in-game tutorial lesson as seen ("Level_01/move") and saves when it is new.</summary>
    public bool RecordTutorialSeen(string key)
    {
        if (progress == null || !progress.RecordTutorialSeen(key)) return false;
        Save();
        return true;
    }

    /// <summary>Counts a finished practice run and saves. True when the card just became mastered.</summary>
    public bool RecordTrainingRun(string cardId, bool mastery, float seconds)
    {
        if (progress == null || string.IsNullOrWhiteSpace(cardId)) return false;
        bool newlyMastered = progress.RecordTrainingRun(cardId, mastery, seconds);
        Save();
        return newlyMastered;
    }

    // =========================================================
    // SAVE / LOAD
    // =========================================================

    /// <summary>Writes the current progress (signed, atomic). Returns false on failure.</summary>
    public bool Save()
    {
        try
        {
            // A newer game's save is never overwritten by ordinary saving (only a confirmed New Game/reset replaces it).
            if (!CampaignSave.AllowsOrdinarySave(campaignState))
            {
                Debug.LogWarning("[PlayerProgressManager] Not saved: the save on this device is from a newer version of TaxVenture.");
                return false;
            }
            progress.Normalize();
            store.Write(Encode(progress));
            RememberSavedCompletion();
            Debug.Log($"[PlayerProgressManager] Progress saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerProgressManager] Save failed: {e.Message}");
            return false;
        }

        try
        {
            ProgressChanged?.Invoke(progress);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        return true;
    }

    private PlayerProgressData LoadOrCreate()
    {
        if (!store.Exists)
        {
            Debug.Log($"[PlayerProgressManager] No save file found at {SavePath}. Starting fresh.");
            campaignState = CampaignSave.Classify(false, null);
            var fresh = new PlayerProgressData();
            fresh.Normalize();
            return fresh;
        }

        string text = store.Read(t => TryDecode(t, out _, out _), out bool fromBackup, out string problem);
        if (text == null || !TryDecode(text, out PlayerProgressData data, out bool legacy))
        {
            Debug.LogError(
                $"[PlayerProgressManager] No usable save at {SavePath} ({problem ?? "unreadable"}). " +
                "Starting fresh; the old files were kept (.bak / .rejected).");
            campaignState = CampaignSave.Classify(true, null);
            var fresh = new PlayerProgressData();
            fresh.Normalize();
            return fresh;
        }

        if (fromBackup)
            Debug.LogWarning($"[PlayerProgressManager] The save was not usable ({problem}); loaded the backup instead.");

        // Decided before Normalize: an old save stays an old save (never a campaign).
        campaignState = CampaignSave.Classify(true, data);
        if (campaignState == CampaignSaveState.UnsupportedFuture)
        {
            // Keep the newer game's file exactly as it is; this version neither reads nor rewrites it.
            Debug.LogWarning($"[PlayerProgressManager] The save is version {data.saveVersion}, newer than this game " +
                             $"({PlayerProgressData.CurrentSaveVersion}). It was left untouched.");
            var fresh = new PlayerProgressData();
            fresh.Normalize();
            return fresh;
        }

        bool repaired = data.Normalize();
        Debug.Log($"[PlayerProgressManager] Progress loaded from {(fromBackup ? store.BackupPath : SavePath)}.");

        // Upgrade old or repaired saves on disk right away (the old file
        // becomes the .bak, so nothing is lost).
        if (legacy || repaired || fromBackup)
        {
            try
            {
                store.Write(Encode(data));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerProgressManager] Could not write the upgraded save: {e.Message}");
            }
        }
        return data;
    }

    private static string Encode(PlayerProgressData data)
    {
        string payload = JsonUtility.ToJson(data, true);
        var envelope = new SaveEnvelope
        {
            format = EnvelopeFormat,
            payload = payload,
            signature = SaveFileStore.Sign(payload)
        };
        return JsonUtility.ToJson(envelope, true);
    }

    /// <summary>
    /// Reads a save: a signed envelope (signature must match) or, for saves
    /// written before version 3, the plain PlayerProgressData JSON.
    /// </summary>
    private static bool TryDecode(string text, out PlayerProgressData data, out bool legacy)
    {
        data = null;
        legacy = false;
        try
        {
            SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(text);
            if (envelope != null && envelope.format > 0 && !string.IsNullOrEmpty(envelope.payload))
            {
                if (!SaveFileStore.SignatureMatches(envelope.payload, envelope.signature))
                {
                    Debug.LogWarning("[PlayerProgressManager] The save's signature does not match (edited or damaged).");
                    return false;
                }
                data = JsonUtility.FromJson<PlayerProgressData>(envelope.payload);
                return data != null;
            }

            // Plain save from before signing. Only genuine old versions are
            // accepted this way; a plain file claiming the current version
            // can only be a hand-made one.
            data = JsonUtility.FromJson<PlayerProgressData>(text);
            if (data == null || data.saveVersion >= PlayerProgressData.SignedSinceVersion) return false;
            legacy = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerProgressManager] The save could not be parsed: {e.Message}");
            data = null;
            return false;
        }
    }
}
