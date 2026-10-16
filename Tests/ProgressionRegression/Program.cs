using System;
using System.Collections.Generic;
using System.Text.Json;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var progress = new PlayerProgressData();
progress.Normalize();
Check(progress.IsLevelUnlocked(1), "Level 1 must start unlocked.");
Check(!progress.IsLevelUnlocked(2), "Fresh saves must lock Level 2.");

for (int replay = 0; replay < 7; replay++)
    progress.RecordLevelCompletion("Level_01");
Check(progress.levelsCompleted == 1, "Seven Level 1 replays must count as one distinct level.");
Check(progress.IsLevelUnlocked(2), "Completing Level 1 must unlock Level 2.");
Check(!progress.IsLevelUnlocked(3), "An unlocked Level 2 must not unlock Level 3.");
progress.RecordLevelCompletion("Level_02");
Check(progress.IsLevelUnlocked(3), "Completing Level 2 must unlock Level 3.");
Check(!progress.IsLevelUnlocked(4), "Completing Level 2 must leave Level 4 locked.");

var nonSequential = new PlayerProgressData();
nonSequential.Normalize();
nonSequential.RecordLevelCompletion("Level_04");
Check(!nonSequential.IsLevelUnlocked(2), "A completion count cannot unlock Level 2 without Level 1.");
Check(nonSequential.IsLevelUnlocked(5), "Level 5 must depend on completion of Level 4.");

var legacy = new PlayerProgressData { levelsCompleted = 7, totalExp = 700, totalReputation = 23 };
legacy.Normalize();
Check(legacy.levelsCompleted == 1 && legacy.IsLevelUnlocked(2) && !legacy.IsLevelUnlocked(3),
    "Legacy replay totals must migrate to Level 1 only.");
Check(legacy.totalExp == 700 && legacy.totalReputation == 23, "Migration must preserve rewards.");
legacy.Normalize();
Check(legacy.levelsCompleted == 1, "Migration must be idempotent.");

var serializer = new JsonSerializerOptions { IncludeFields = true };
var restored = JsonSerializer.Deserialize<PlayerProgressData>(JsonSerializer.Serialize(progress, serializer), serializer);
restored.Normalize();
Check(restored.levelsCompleted == 2 && restored.IsLevelUnlocked(3) && !restored.IsLevelUnlocked(4),
    "Saved distinct completion IDs must survive a field-based JSON round trip.");

var dirty = new PlayerProgressData
{
    saveVersion = 1,
    levelsCompleted = 99,
    completedLevelIds = new List<string> { "Level_01", "Level_01", null, "" }
};
dirty.Normalize();
Check(dirty.levelsCompleted == 1 && !dirty.IsLevelUnlocked(3), "Normalization must deduplicate IDs, not trust the old count.");

var scored = new PlayerProgressData();
var below = scored.RecordLevelResult("Level_01", 100, 8, 150, 12, false, new[] { "know_your_taxpayer_1" });
Check(!scored.IsLevelUnlocked(2) && scored.levelsCompleted == 1, "A below-passing finish must not unlock Level 2.");
Check(below.expGain == 100 && scored.totalExp == 100 && scored.totalReputation == 8, "The first run must add its score.");
var worse = scored.RecordLevelResult("Level_01", 60, 5, 150, 12, false, null);
Check(worse.expGain == 0 && scored.totalExp == 100 && !worse.newBest, "A worse replay must not add EXP.");
var pass = scored.RecordLevelResult("Level_01", 140, 11, 150, 12, true, null);
Check(pass.expGain == 40 && pass.heartsGain == 3 && scored.totalExp == 140 && scored.totalReputation == 11,
    "A better replay must add only the improvement over the best.");
Check(scored.IsLevelUnlocked(2) && scored.GetRecord("Level_01").attempts == 3 && scored.GetRecord("Level_01").badges.Count == 1,
    "Passing must unlock Level 2 and keep the best record and badges.");
scored.RecordLevelResult("Level_01", 10, 1, 150, 12, false, null);
Check(scored.IsLevelUnlocked(2), "A later failed replay must never re-lock a passed level.");
var scoredRestored = JsonSerializer.Deserialize<PlayerProgressData>(JsonSerializer.Serialize(scored, serializer), serializer);
scoredRestored.Normalize();
Check(scoredRestored.IsLevelUnlocked(2) && scoredRestored.GetRecord("Level_01").bestExp == 140,
    "Version 2 records must survive a JSON round trip.");
var v2Fail = new PlayerProgressData { saveVersion = 2, completedLevelIds = new List<string> { "Level_01" } };
v2Fail.Normalize();
Check(!v2Fail.IsLevelUnlocked(2), "A version 2 save that completed but never passed Level 1 must keep Level 2 locked.");
var badgeSave = new PlayerProgressData();
Check(badgeSave.GetBadgeTier("know_your_taxpayer_1") == 0, "An uncollected badge has tier 0.");
Check(badgeSave.RecordBadge("know_your_taxpayer_1", 1) && badgeSave.GetBadgeTier("know_your_taxpayer_1") == 1, "Bronze is collected.");
Check(badgeSave.RecordBadge("know_your_taxpayer_1", 3) && badgeSave.GetBadgeTier("know_your_taxpayer_1") == 3, "Gold upgrades the badge.");
Check(!badgeSave.RecordBadge("know_your_taxpayer_1", 2) && badgeSave.GetBadgeTier("know_your_taxpayer_1") == 3, "A worse replay never downgrades a badge.");
Check(!badgeSave.RecordBadge("", 3) && !badgeSave.RecordBadge("x", 0), "Empty ids and tier 0 are ignored.");
var badgeRestored = JsonSerializer.Deserialize<PlayerProgressData>(JsonSerializer.Serialize(badgeSave, serializer), serializer);
Check(badgeRestored.GetBadgeTier("know_your_taxpayer_1") == 3, "Badge tiers survive a JSON round trip.");
Console.WriteLine("PASS: distinct completions, sequential unlocking, legacy migration, reward preservation, JSON round trip, pass-to-unlock, best-score records, and badge tiers.");
