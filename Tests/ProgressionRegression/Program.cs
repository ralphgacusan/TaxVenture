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
Console.WriteLine("PASS: distinct completions, sequential unlocking, legacy migration, reward preservation, and JSON round trip.");
