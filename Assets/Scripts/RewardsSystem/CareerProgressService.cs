using System;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Unity side of the career module: loads Resources/Career/chapters.json,
/// resolves each chapter's reward rules (rewards.json) and builds a
/// CareerSnapshot from the live save in PlayerProgressManager.
///
/// Also remembers what the menu board last showed (PlayerPrefs) so the
/// board can count up new EXP / hearts and stamp new passes on the next
/// visit. That memory is only a viewer convenience: losing it never touches
/// the real save, it just skips the celebration.
/// </summary>
public static class CareerProgressService
{
    public const string CatalogPath = "Career/chapters";
    private const string SeenKey = "TaxVenture.Career.Seen.v1";

    private static CareerCatalog catalog;

    public static CareerCatalog Catalog
    {
        get
        {
            if (catalog == null) catalog = LoadCatalog();
            return catalog;
        }
    }

    /// <summary>Forgets the cached catalog and rules (e.g. after editing the JSON in Play Mode).</summary>
    public static void ClearCache()
    {
        catalog = null;
        LevelRewardRules.ClearCache();
    }

    /// <summary>The career as saved right now (null save = progress unavailable).</summary>
    public static CareerSnapshot Current()
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        return Build(manager != null ? manager.CurrentProgress : null);
    }

    public static CareerSnapshot Build(PlayerProgressData data) =>
        CareerProgress.Build(data, Catalog, RulesFor);

    public static CareerChapterRules RulesFor(CareerChapterDef def)
    {
        LevelRewardRules rules = LevelRewardRules.Load(def.id);
        int cases = Mathf.Max(0, def.cases);
        var result = new CareerChapterRules
        {
            passingPercent = rules.passingPercent,
            maxExp = LevelScoring.MaxExp(rules, cases),
            maxHearts = LevelScoring.MaxHearts(rules, cases)
        };

        // Only chapters listed under sideQuests carry a badge (levels without a
        // rewards.json fall back to built-in rules that name the Level 1 badge).
        if (Catalog.sideQuests != null)
            foreach (CareerSideQuestDef quest in Catalog.sideQuests)
                if (quest != null && quest.chapterId == def.id && rules.sideQuest != null &&
                    !string.IsNullOrWhiteSpace(rules.sideQuest.badgeId))
                {
                    result.badgeId = rules.sideQuest.badgeId.Trim();
                    result.badgeName = rules.sideQuest.badgeName;
                    result.badgeEmblem = rules.sideQuest.badgeEmblem;
                    result.badgeRibbon = rules.sideQuest.badgeRibbon;
                }
        return result;
    }

    // =========================================================
    // LAST SEEN (menu celebration only)
    // =========================================================

    public static CareerSeenState LoadSeen()
    {
        try
        {
            string json = PlayerPrefs.GetString(SeenKey, string.Empty);
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<CareerSeenState>(json);
        }
        catch (Exception error)
        {
            Debug.LogWarning("[CareerProgressService] Ignoring unreadable last-seen state: " + error.Message);
            return null;
        }
    }

    public static void SaveSeen(CareerSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Available) return;
        PlayerPrefs.SetString(SeenKey, JsonUtility.ToJson(CareerProgress.Remember(snapshot)));
        PlayerPrefs.Save();
    }

    private static CareerCatalog LoadCatalog()
    {
        TextAsset file = Resources.Load<TextAsset>(CatalogPath);
        if (file != null)
        {
            try
            {
                CareerCatalog loaded = JsonUtility.FromJson<CareerCatalog>(file.text);
                if (loaded != null && loaded.chapters != null && loaded.chapters.Count > 0) return loaded;
                Debug.LogWarning($"[CareerProgressService] Resources/{CatalogPath}.json has no chapters. Using built-in chapters.");
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[CareerProgressService] Resources/{CatalogPath}.json is not valid JSON ({error.Message}). Using built-in chapters.");
            }
        }
        else
        {
            Debug.LogWarning($"[CareerProgressService] Resources/{CatalogPath}.json not found. Using built-in chapters.");
        }
        return BuiltInCatalog();
    }

    /// <summary>Fallback that mirrors chapters.json.</summary>
    public static CareerCatalog BuiltInCatalog()
    {
        var built = new CareerCatalog();
        built.chapters.Add(new CareerChapterDef
        {
            id = "Level_01", number = 1, noteTitle = "Tutorial", title = "Tutorial Level",
            description = "Learn your way around the office, meet your first taxpayer and close your first case.",
            scene = "Cutscene2.0", cases = 1
        });
        built.chapters.Add(new CareerChapterDef
        {
            id = "Level_02", number = 2, noteTitle = "One market client", title = "Something Does Not Match",
            description = "Check Aling Rosa's prepared income summary, follow the records back to her market stall and decide if her work is ready.",
            scene = "Office_Level2", cases = 1, campaignPending = false
        });
        for (int number = 3; number <= 5; number++)
            built.chapters.Add(new CareerChapterDef
            {
                id = $"Level_{number:00}", number = number, noteTitle = "New chapter", title = "New Chapter",
                description = "A new chapter of your career is on its way.", comingSoon = true
            });
        built.sideQuests.Add(new CareerSideQuestDef
        {
            chapterId = "Level_01", number = 1, title = "Know Your Taxpayer",
            description = "Sort the taxpayer profiles on the office corkboard. Part of Chapter 1."
        });
        return built;
    }
}
