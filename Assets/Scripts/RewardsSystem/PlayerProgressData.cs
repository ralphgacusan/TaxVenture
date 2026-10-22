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
    public const int CurrentVersion = 1;
    public const string PhaseNone = "";
    public const string PhaseActive = "active";
    public const string PhaseSolved = "solved";
    public const string PhaseRewarded = "rewarded";

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

    // T06 (first complete check): reserved, unset in T04.
    public bool checkSubmitted;
    public int checkCorrectCount = -1;
    public List<string> checkWrongProfileIds = new List<string>();

    /// <summary>Anything to resume (a version was written by the checkpointer).</summary>
    public bool IsSet => version > 0;

    public static bool IsKnownPhase(string phase) =>
        phase == PhaseNone || phase == PhaseActive || phase == PhaseSolved || phase == PhaseRewarded;

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
        if (boardPhase == null || !IsKnownPhase(boardPhase.Trim()))
        {
            boardPhase = PhaseNone;
            changed = true;
        }
        else if (boardPhase != boardPhase.Trim()) { boardPhase = boardPhase.Trim(); changed = true; }

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

/// <summary>
/// The updated story campaign (version 5). Separate from the mastery records
/// (EXP, score hearts, badges): publicTrust is social confidence 0..100, coins
/// are the office budget. Plain ids and quantities only. Starting values come
/// from CreateFresh, the one factory used by every New Game.
/// </summary>
[Serializable]
public class CampaignRecord
{
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
        if (c.campaignId != null && c.campaignId.Trim() != c.campaignId) { c.campaignId = c.campaignId.Trim(); changed = true; }
        if (c.completedChapterIds == null) { c.completedChapterIds = new List<string>(); changed = true; }
        if (c.rewardClaims == null) { c.rewardClaims = new List<string>(); changed = true; }
        if (c.ownedUpgradeIds == null) { c.ownedUpgradeIds = new List<string>(); changed = true; }
        if (c.items == null) { c.items = new List<CampaignItemStack>(); changed = true; }
        if (c.levelOne == null) { c.levelOne = new Level1FieldCheckpoint(); changed = true; }
        changed |= c.levelOne.Repair();
        c.completedChapterIds = CleanIds(c.completedChapterIds, ref changed);
        c.rewardClaims = CleanIds(c.rewardClaims, ref changed);
        c.ownedUpgradeIds = CleanIds(c.ownedUpgradeIds, ref changed);

        if (c.IsSet && string.IsNullOrWhiteSpace(c.currentChapterId)) { c.currentChapterId = CampaignRecord.FirstChapterId; changed = true; }
        int trust = Math.Max(0, Math.Min(CampaignRecord.MaxTrust, c.publicTrust));
        if (trust != c.publicTrust) { c.publicTrust = trust; changed = true; }
        if (c.coins < 0) { c.coins = 0; changed = true; }

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
