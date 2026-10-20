using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// The career module: turns the saved PlayerProgressData into what menus show
/// (chapter states, pass targets, career totals, badges, the next goal) and
/// works out what changed since the player last looked (new EXP, new passes,
/// new or upgraded badges) so the board can celebrate it.
///
/// Pure C# (no UnityEngine) so it is shared by the 3D menu board and the
/// Tests/CareerBoardRegression suite. It never writes or normalizes the save.
/// Unity-side loading (chapters.json, rewards.json, PlayerPrefs) lives in
/// CareerProgressService.
/// </summary>
public static class CareerProgress
{
    /// <summary>Builds the read-only career view. data == null means the save could not be read.</summary>
    public static CareerSnapshot Build(PlayerProgressData data, CareerCatalog catalog,
        Func<CareerChapterDef, CareerChapterRules> rulesFor)
    {
        var snapshot = new CareerSnapshot { Available = data != null };
        if (catalog == null || catalog.chapters == null) return snapshot;

        snapshot.TotalExp = data != null ? Math.Max(0, data.totalExp) : 0;
        snapshot.TotalHearts = data != null ? Math.Max(0, data.totalReputation) : 0;

        CareerChapterDef previous = null;
        foreach (CareerChapterDef def in catalog.chapters)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.id)) continue;
            CareerChapterRules rules = (rulesFor != null ? rulesFor(def) : null) ?? new CareerChapterRules();
            CareerChapterView view = ProjectChapter(data, def, previous, rules);
            snapshot.Chapters.Add(view);
            if (!def.comingSoon) previous = def;

            if (view.Status != CareerChapterStatus.ComingSoon)
            {
                snapshot.CareerMaxExp += view.MaxExp;
                snapshot.CareerMaxHearts += view.MaxHearts;
            }
            if (view.Status == CareerChapterStatus.Passed) { snapshot.Passed++; snapshot.Completed++; }
            else if (view.Status == CareerChapterStatus.Finished) snapshot.Completed++;
        }

        snapshot.BadgeSlots = Math.Max(1, catalog.badgeSlots);
        int questIndex = 0;
        if (catalog.sideQuests != null)
        {
            foreach (CareerSideQuestDef quest in catalog.sideQuests)
            {
                questIndex++;
                CareerChapterView chapter = snapshot.FindChapter(quest?.chapterId);
                if (chapter == null || chapter.Rules.badgeId == null) continue;
                CareerChapterRules rules = chapter.Rules;
                snapshot.Badges.Add(new CareerBadgeView
                {
                    BadgeId = rules.badgeId,
                    Name = string.IsNullOrWhiteSpace(quest.title) ? rules.badgeName : quest.title,
                    Emblem = rules.badgeEmblem,
                    Ribbon = rules.badgeRibbon,
                    Number = quest.number > 0 ? quest.number : questIndex,
                    BestSeconds = chapter.SideQuestBestSeconds,
                    LastSeconds = chapter.SideQuestLastSeconds,
                    Tier = data != null ? Clamp(data.GetBadgeTier(rules.badgeId), 0, PlayerProgressData.MaxBadgeTier) : 0,
                    Chapter = chapter
                });
            }
        }

        snapshot.Goal = FindGoal(snapshot);
        return snapshot;
    }

    private static CareerChapterView ProjectChapter(PlayerProgressData data, CareerChapterDef def,
        CareerChapterDef previous, CareerChapterRules rules)
    {
        string id = def.id.Trim();
        LevelRecord record = data?.GetRecord(id);
        bool completed = data != null && data.completedLevelIds != null && data.completedLevelIds.Contains(id);
        bool passed = data != null && data.IsLevelPassed(id);
        // Same rule as PlayerProgressData.IsLevelUnlocked: the previous chapter must be passed.
        bool unlocked = previous == null || (data != null && data.IsLevelPassed(previous.id));
        bool hasScene = !string.IsNullOrWhiteSpace(def.scene);

        var view = new CareerChapterView
        {
            Def = def,
            Rules = rules,
            Record = record,
            BestExp = record != null ? Math.Max(0, record.bestExp) : 0,
            BestHearts = record != null ? Math.Max(0, record.bestHearts) : 0,
            Attempts = record != null ? Math.Max(0, record.attempts) : 0,
            BestSeconds = record != null ? Math.Max(0, record.bestSeconds) : 0,
            LastSeconds = record != null ? Math.Max(0, record.lastSeconds) : 0,
            TotalSeconds = record != null ? Math.Max(0, record.totalSeconds) : 0,
            SideQuestBestSeconds = record != null ? Math.Max(0, record.sideQuestBestSeconds) : 0,
            SideQuestLastSeconds = record != null ? Math.Max(0, record.sideQuestLastSeconds) : 0,
            MaxExp = record != null && record.maxExp > 0 ? record.maxExp : Math.Max(0, rules.maxExp),
            MaxHearts = record != null && record.maxHearts > 0 ? record.maxHearts : Math.Max(0, rules.maxHearts),
            PassingPercent = Clamp(rules.passingPercent, 0, 100)
        };
        view.TargetExp = Target(view.MaxExp, view.PassingPercent);
        view.TargetHearts = Target(view.MaxHearts, view.PassingPercent);

        if (def.comingSoon || !hasScene && !passed && !completed)
            view.Status = CareerChapterStatus.ComingSoon;
        else if (passed) view.Status = CareerChapterStatus.Passed;
        else if (completed) view.Status = CareerChapterStatus.Finished;
        else if (unlocked) view.Status = CareerChapterStatus.Available;
        else view.Status = CareerChapterStatus.Locked;

        bool playable = view.Status == CareerChapterStatus.Available ||
                        view.Status == CareerChapterStatus.Finished ||
                        view.Status == CareerChapterStatus.Passed;
        view.Launchable = data != null && hasScene && playable && unlocked;
        return view;
    }

    /// <summary>The chapter the player is working toward: the first playable one not yet passed.</summary>
    private static CareerChapterView FindGoal(CareerSnapshot snapshot)
    {
        CareerChapterView lastPlayable = null;
        foreach (CareerChapterView chapter in snapshot.Chapters)
        {
            if (chapter.Status == CareerChapterStatus.ComingSoon || chapter.Status == CareerChapterStatus.Locked) continue;
            if (chapter.Status != CareerChapterStatus.Passed) return chapter;
            lastPlayable = chapter;
        }
        // Every open chapter is passed: the goal moves on to the next chapter (coming soon), not back to a passed one.
        if (lastPlayable != null)
        {
            CareerChapterView next = snapshot.ChapterAfter(lastPlayable);
            if (next != null) return next;
        }
        return lastPlayable ?? (snapshot.Chapters.Count > 0 ? snapshot.Chapters[0] : null);
    }

    /// <summary>Points needed to pass: the passing percent of the maximum, rounded up (matches LevelScoring.Passed).</summary>
    public static int Target(int max, int passingPercent) =>
        max <= 0 ? 0 : (int)Math.Ceiling(max * Clamp(passingPercent, 0, 100) / 100.0);

    /// <summary>One short line telling the player what to do next.</summary>
    public static string NextStep(CareerSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Available) return "Career progress could not be read. Your save is safe.";
        CareerChapterView goal = snapshot.Goal;
        if (goal == null) return "";
        if (goal.Status == CareerChapterStatus.Passed)
            return "Every open chapter is passed. New chapters are coming soon.";
        if (goal.Status == CareerChapterStatus.ComingSoon)
            return $"Chapter {goal.Def.number} is coming soon. Until then, replay a chapter to beat your best or earn a gold badge.";
        CareerChapterView next = snapshot.ChapterAfter(goal);
        string unlocks = next != null && next.Status != CareerChapterStatus.ComingSoon
            ? $" to unlock Chapter {next.Def.number}." : ".";
        return $"Pass Chapter {goal.Def.number}: {goal.TargetExp:N0} EXP + {goal.TargetHearts:N0} hearts in one run{unlocks}";
    }

    // =========================================================
    // WHAT CHANGED SINCE THE LAST VISIT
    // =========================================================

    /// <summary>Compares the snapshot with what the board showed last time. seen == null = first visit (nothing to celebrate).</summary>
    public static CareerChanges Diff(CareerSeenState seen, CareerSnapshot snapshot)
    {
        var changes = new CareerChanges();
        if (snapshot == null || !snapshot.Available) return changes;
        changes.ExpFrom = snapshot.TotalExp;
        changes.HeartsFrom = snapshot.TotalHearts;
        if (seen == null) return changes;

        changes.ExpFrom = Clamp(seen.exp, 0, snapshot.TotalExp);
        changes.HeartsFrom = Clamp(seen.hearts, 0, snapshot.TotalHearts);
        foreach (CareerChapterView chapter in snapshot.Chapters)
        {
            string id = chapter.Def.id;
            if (chapter.Status == CareerChapterStatus.Passed && !Contains(seen.passed, id))
                changes.NewlyPassed.Add(id);
            else if (chapter.Status == CareerChapterStatus.Finished && !Contains(seen.completed, id))
                changes.NewlyFinished.Add(id);
        }
        foreach (CareerBadgeView badge in snapshot.Badges)
            if (badge.Tier > SeenTier(seen, badge.BadgeId)) changes.UpgradedBadges.Add(badge.BadgeId);
        return changes;
    }

    /// <summary>What to remember after the board has shown the snapshot.</summary>
    public static CareerSeenState Remember(CareerSnapshot snapshot)
    {
        var seen = new CareerSeenState();
        if (snapshot == null || !snapshot.Available) return seen;
        seen.exp = snapshot.TotalExp;
        seen.hearts = snapshot.TotalHearts;
        foreach (CareerChapterView chapter in snapshot.Chapters)
        {
            if (chapter.Status == CareerChapterStatus.Passed) { seen.passed.Add(chapter.Def.id); seen.completed.Add(chapter.Def.id); }
            else if (chapter.Status == CareerChapterStatus.Finished) seen.completed.Add(chapter.Def.id);
        }
        foreach (CareerBadgeView badge in snapshot.Badges)
        {
            seen.badgeIds.Add(badge.BadgeId);
            seen.badgeTiers.Add(badge.Tier);
        }
        return seen;
    }

    private static int SeenTier(CareerSeenState seen, string badgeId)
    {
        if (seen.badgeIds == null || seen.badgeTiers == null) return 0;
        for (int i = 0; i < seen.badgeIds.Count && i < seen.badgeTiers.Count; i++)
            if (seen.badgeIds[i] == badgeId) return seen.badgeTiers[i];
        return 0;
    }

    private static bool Contains(List<string> list, string id) => list != null && list.Contains(id);

    private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

    public static string StatusLabel(CareerChapterStatus status)
    {
        switch (status)
        {
            case CareerChapterStatus.Available: return "PLAY";
            case CareerChapterStatus.Finished: return "TRY AGAIN";
            case CareerChapterStatus.Passed: return "PASSED";
            case CareerChapterStatus.Locked: return "LOCKED";
            default: return "COMING SOON";
        }
    }

    /// <summary>"02:59", "1:02:59", or "--:--" when not measured.</summary>
    public static string FormatTime(int seconds)
    {
        if (seconds <= 0) return "--:--";
        int hours = seconds / 3600, minutes = seconds / 60 % 60, rest = seconds % 60;
        return hours > 0 ? $"{hours}:{minutes:00}:{rest:00}" : $"{minutes:00}:{rest:00}";
    }

    public static string TierLabel(int tier)
    {
        switch (tier)
        {
            case 3: return "Gold";
            case 2: return "Silver";
            case 1: return "Bronze";
            default: return "Not earned";
        }
    }
}

public enum CareerChapterStatus { Available, Finished, Passed, Locked, ComingSoon }

// =========================================================
// CATALOG (Resources/Career/chapters.json)
// =========================================================

[Serializable]
public class CareerCatalog
{
    public List<CareerChapterDef> chapters = new List<CareerChapterDef>();
    public List<CareerSideQuestDef> sideQuests = new List<CareerSideQuestDef>();
    /// <summary>Badge spaces on the story board (filled in side-quest order, the rest wait empty).</summary>
    public int badgeSlots = 5;
}

[Serializable]
public class CareerChapterDef
{
    public string id;
    public int number;
    /// <summary>Long name for the chapter sheet.</summary>
    public string title;
    /// <summary>Short name written on the pinned paper.</summary>
    public string noteTitle;
    public string description;
    /// <summary>Scene to load; empty = not playable yet.</summary>
    public string scene;
    /// <summary>Cases in the level (used for the maximum score when no result is saved).</summary>
    public int cases = 1;
    public bool comingSoon;
}

[Serializable]
public class CareerSideQuestDef
{
    /// <summary>Chapter that contains the side quest; its rewards.json names the badge.</summary>
    public string chapterId;
    /// <summary>Side quest number shown on its paper (0 = its order in the list).</summary>
    public int number;
    public string title;
    public string description;
}

/// <summary>Reward numbers for one chapter, resolved from its rewards.json by the caller.</summary>
public class CareerChapterRules
{
    public int passingPercent = 80;
    public int maxExp;
    public int maxHearts;
    public string badgeId;
    public string badgeName;
    public string badgeEmblem;
    public string badgeRibbon;
}

// =========================================================
// VIEWS
// =========================================================

public class CareerSnapshot
{
    public bool Available;
    public int TotalExp;
    public int TotalHearts;
    public int Completed;
    public int Passed;
    /// <summary>Sum of the EXP / hearts maximums of every chapter that is not Coming Soon.</summary>
    public int CareerMaxExp;
    public int CareerMaxHearts;
    public int BadgeSlots = 5;
    public readonly List<CareerChapterView> Chapters = new List<CareerChapterView>();
    public readonly List<CareerBadgeView> Badges = new List<CareerBadgeView>();
    public CareerChapterView Goal;

    public int ChapterCount => Chapters.Count;

    public CareerChapterView FindChapter(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (CareerChapterView chapter in Chapters)
            if (chapter.Def.id == id.Trim()) return chapter;
        return null;
    }

    public CareerChapterView ChapterAfter(CareerChapterView chapter)
    {
        int index = Chapters.IndexOf(chapter);
        return index >= 0 && index + 1 < Chapters.Count ? Chapters[index + 1] : null;
    }

    public int EarnedBadges
    {
        get
        {
            int count = 0;
            foreach (CareerBadgeView badge in Badges) if (badge.Tier > 0) count++;
            return count;
        }
    }
}

public class CareerChapterView
{
    public CareerChapterDef Def;
    public CareerChapterRules Rules;
    public LevelRecord Record;
    public CareerChapterStatus Status;
    public bool Launchable;
    public int BestExp, BestHearts, Attempts;
    public int MaxExp, MaxHearts, PassingPercent;
    public int TargetExp, TargetHearts;
    public int BestSeconds, LastSeconds, TotalSeconds, SideQuestBestSeconds, SideQuestLastSeconds;

    /// <summary>Open to play (not Locked or Coming Soon); the story board strings these together.</summary>
    public bool IsOpen => Status == CareerChapterStatus.Available || Status == CareerChapterStatus.Finished ||
                          Status == CareerChapterStatus.Passed;

    public bool HasResult => Record != null;
    public float ExpToTarget => TargetExp <= 0 ? 1f : Math.Min(1f, BestExp / (float)TargetExp);
    public float HeartsToTarget => TargetHearts <= 0 ? 1f : Math.Min(1f, BestHearts / (float)TargetHearts);
    public string Title => string.IsNullOrWhiteSpace(Def.title) ? $"Chapter {Def.number}" : Def.title;
    public string NoteTitle => string.IsNullOrWhiteSpace(Def.noteTitle) ? Title : Def.noteTitle;
}

public class CareerBadgeView
{
    public string BadgeId, Name, Emblem, Ribbon;
    public int Tier;
    public int Number;
    public int BestSeconds, LastSeconds;
    public CareerChapterView Chapter;
}

/// <summary>What the board showed last time (saved as JSON in PlayerPrefs; a viewer convenience, not the save).</summary>
[Serializable]
public class CareerSeenState
{
    public int exp;
    public int hearts;
    public List<string> completed = new List<string>();
    public List<string> passed = new List<string>();
    public List<string> badgeIds = new List<string>();
    public List<int> badgeTiers = new List<int>();
}

public class CareerChanges
{
    public int ExpFrom;
    public int HeartsFrom;
    public readonly List<string> NewlyPassed = new List<string>();
    public readonly List<string> NewlyFinished = new List<string>();
    public readonly List<string> UpgradedBadges = new List<string>();

    public bool Any(CareerSnapshot snapshot) =>
        snapshot != null && (ExpFrom < snapshot.TotalExp || HeartsFrom < snapshot.TotalHearts ||
                             NewlyPassed.Count > 0 || NewlyFinished.Count > 0 || UpgradedBadges.Count > 0);
}
