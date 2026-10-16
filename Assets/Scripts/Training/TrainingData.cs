using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The data of the Tutorial Hub (Arthur's tutorial binder) and the pure rules
/// on top of it. No scene work, so Tests/TrainingRegression checks it directly.
///
/// - Resources/Tutorials/Training/training.json: the tabs and drill cards
///   (TrainingCatalogFile). Each card lists the in-game lessons it covers as
///   "Level_01/move" (guide.json lesson ids).
/// - Resources/Tutorials/Training/Drills/&lt;drill&gt;.json: one practice drill
///   (TrainingDrill): where it starts, what is switched off, and two runs:
///   GUIDED (Arthur's real lessons, reused by id) and MASTERY (no hand, short
///   goals). TrainingDrillScript turns a run into a normal GuideScript.
///
/// RULES (TrainingCatalog.Build):
/// - Tabs with "alwaysOpen" (Basics) and all their cards are open from the
///   first launch. Any other card opens once one of its lessons was finished
///   in game, or its chapter is already completed in the save (old saves).
///   A tab with no open card is not shown at all.
/// - Card state: Locked, New (open, never practised), Practised (a guided or
///   mastery run finished), Mastered (a mastery run finished).
/// </summary>
[Serializable]
public class TrainingCatalogFile
{
#pragma warning disable 0649
    public string title = "TUTORIAL BINDER";
    public string owner = "Arthur";
    /// <summary>One is picked at random when the hub opens.</summary>
    public string[] greetings = new string[0];
    public TrainingTabDef[] tabs = new TrainingTabDef[0];
    public TrainingCardDef[] cards = new TrainingCardDef[0];
#pragma warning restore 0649

    public const string ResourcePath = "Tutorials/Training/training";
    public const string DrillFolder = "Tutorials/Training/Drills/";

    public static TrainingCatalogFile Load()
    {
        TextAsset file = Resources.Load<TextAsset>(ResourcePath);
        if (file == null)
        {
            Debug.LogWarning($"[Training] Resources/{ResourcePath}.json not found.");
            return new TrainingCatalogFile();
        }
        TrainingCatalogFile catalog = Parse(file.text, out string problems);
        if (!string.IsNullOrEmpty(problems)) Debug.LogWarning($"[Training] {ResourcePath}.json:\n{problems}");
        return catalog ?? new TrainingCatalogFile();
    }

    /// <summary>Parses and validates; never throws (null only for invalid JSON).</summary>
    public static TrainingCatalogFile Parse(string json, out string problems)
    {
        problems = null;
        TrainingCatalogFile file;
        try { file = JsonUtility.FromJson<TrainingCatalogFile>(json); }
        catch (Exception e)
        {
            problems = "not valid JSON: " + e.Message;
            return null;
        }
        if (file == null) return null;
        problems = file.Validate();
        return file;
    }

    /// <summary>Drops tabs and cards that cannot work; returns a readable report.</summary>
    public string Validate()
    {
        var report = new System.Text.StringBuilder();
        if (greetings == null) greetings = new string[0];

        var tabIds = new HashSet<string>(StringComparer.Ordinal);
        var keptTabs = new List<TrainingTabDef>();
        foreach (TrainingTabDef tab in tabs ?? new TrainingTabDef[0])
        {
            if (tab == null || string.IsNullOrWhiteSpace(tab.id)) { report.AppendLine("- a tab has no id; skipped."); continue; }
            if (!tabIds.Add(tab.id)) { report.AppendLine($"- tab \"{tab.id}\": duplicate id; skipped."); continue; }
            keptTabs.Add(tab);
        }
        tabs = keptTabs.ToArray();

        var cardIds = new HashSet<string>(StringComparer.Ordinal);
        var lessonOwner = new Dictionary<string, string>(StringComparer.Ordinal);
        var keptCards = new List<TrainingCardDef>();
        foreach (TrainingCardDef card in cards ?? new TrainingCardDef[0])
        {
            if (card == null || string.IsNullOrWhiteSpace(card.id)) { report.AppendLine("- a card has no id; skipped."); continue; }
            if (!cardIds.Add(card.id)) { report.AppendLine($"- card \"{card.id}\": duplicate id; skipped."); continue; }
            if (!tabIds.Contains(card.tab ?? "")) { report.AppendLine($"- card \"{card.id}\": unknown tab \"{card.tab}\"; skipped."); continue; }
            if (card.skills == null) card.skills = new string[0];
            var lessons = new List<string>();
            foreach (string lesson in card.lessons ?? new string[0])
            {
                string key = lesson?.Trim();
                if (string.IsNullOrEmpty(key) || key.IndexOf('/') <= 0)
                {
                    report.AppendLine($"- card \"{card.id}\": lesson \"{lesson}\" must look like Level_01/move; ignored.");
                    continue;
                }
                if (lessonOwner.TryGetValue(key, out string other))
                {
                    report.AppendLine($"- card \"{card.id}\": lesson \"{key}\" already belongs to \"{other}\"; ignored.");
                    continue;
                }
                lessonOwner[key] = card.id;
                lessons.Add(key);
            }
            card.lessons = lessons.ToArray();
            keptCards.Add(card);
        }
        cards = keptCards.ToArray();
        return report.ToString();
    }

    public TrainingTabDef FindTab(string id)
    {
        foreach (TrainingTabDef tab in tabs) if (tab.id == id) return tab;
        return null;
    }

    public TrainingCardDef FindCard(string id)
    {
        foreach (TrainingCardDef card in cards) if (card.id == id) return card;
        return null;
    }
}

[Serializable]
public class TrainingTabDef
{
    public string id;
    public string title;
    /// <summary>Divider tab colour (hex).</summary>
    public string color = "#4E8FD8";
    /// <summary>True: the tab and its cards are open from the first launch (Basics).</summary>
    public bool alwaysOpen;

    public Color ParsedColor =>
        !string.IsNullOrEmpty(color) && ColorUtility.TryParseHtmlString(color, out Color c) ? c : new Color32(78, 143, 216, 255);
}

[Serializable]
public class TrainingCardDef
{
    public string id;
    public string tab;
    public int number;
    public string title;
    /// <summary>The checklist lines on the card, in plain words.</summary>
    public string[] skills = new string[0];
    /// <summary>In-game lessons this card covers, as "Level_01/move" (guide.json ids).</summary>
    public string[] lessons = new string[0];
    /// <summary>Drill file name in Resources/Tutorials/Training/Drills.</summary>
    public string drill;
    /// <summary>The note on a locked card (no spoilers).</summary>
    public string lockedNote = "You haven't met this one yet.";
}

public enum TrainingCardState { Locked, New, Practised, Mastered }

public sealed class TrainingCardView
{
    public TrainingCardDef Def;
    public TrainingCardState State;
    /// <summary>Lessons of this card the player has finished in game.</summary>
    public int Seen;
    public int Total;
    public TrainingRecord Record;

    public bool Unlocked => State != TrainingCardState.Locked;
    public bool CanMaster => Record != null && (Record.practiceRuns > 0 || Record.masteryRuns > 0);
}

public sealed class TrainingTabView
{
    public TrainingTabDef Def;
    public bool Visible;
    public readonly List<TrainingCardView> Cards = new List<TrainingCardView>();
}

public sealed class TrainingHubView
{
    public readonly List<TrainingTabView> Tabs = new List<TrainingTabView>();
    public int Mastered;
    public int Unlocked;
    public int Total;

    public TrainingCardView FindCard(string id)
    {
        foreach (TrainingTabView tab in Tabs)
            foreach (TrainingCardView card in tab.Cards)
                if (card.Def.id == id) return card;
        return null;
    }
}

public static class TrainingCatalog
{
    /// <summary>The hub's view of a save. Reads the save only; never changes it.</summary>
    public static TrainingHubView Build(TrainingCatalogFile file, PlayerProgressData save)
    {
        var view = new TrainingHubView();
        if (file == null) return view;
        foreach (TrainingTabDef tab in file.tabs)
        {
            var tabView = new TrainingTabView { Def = tab };
            foreach (TrainingCardDef card in file.cards)
            {
                if (card.tab != tab.id) continue;
                TrainingCardView cardView = BuildCard(card, tab, save);
                tabView.Cards.Add(cardView);
                view.Total++;
                if (cardView.Unlocked) view.Unlocked++;
                if (cardView.State == TrainingCardState.Mastered) view.Mastered++;
            }
            tabView.Cards.Sort((a, b) => a.Def.number.CompareTo(b.Def.number));
            tabView.Visible = tab.alwaysOpen || tabView.Cards.Exists(c => c.Unlocked);
            view.Tabs.Add(tabView);
        }
        return view;
    }

    private static TrainingCardView BuildCard(TrainingCardDef card, TrainingTabDef tab, PlayerProgressData save)
    {
        var view = new TrainingCardView { Def = card, Total = card.lessons.Length };
        foreach (string lesson in card.lessons)
            if (LessonSeen(save, lesson)) view.Seen++;

        view.Record = save != null ? save.GetTrainingRecord(card.id) : null;
        bool open = tab.alwaysOpen || view.Seen > 0;
        if (!open) view.State = TrainingCardState.Locked;
        else if (view.Record != null && view.Record.Mastered) view.State = TrainingCardState.Mastered;
        else if (view.Record != null && view.Record.practiceRuns > 0) view.State = TrainingCardState.Practised;
        else view.State = TrainingCardState.New;
        return view;
    }

    /// <summary>Finished in game, or its chapter was already completed before the hub existed.</summary>
    public static bool LessonSeen(PlayerProgressData save, string lessonKey)
    {
        if (save == null || string.IsNullOrEmpty(lessonKey)) return false;
        if (save.HasSeenTutorial(lessonKey)) return true;
        int slash = lessonKey.IndexOf('/');
        string chapter = slash > 0 ? lessonKey.Substring(0, slash) : null;
        return chapter != null && save.completedLevelIds != null && save.completedLevelIds.Contains(chapter);
    }
}

// =========================================================
// DRILLS
// =========================================================

/// <summary>One practice drill (Resources/Tutorials/Training/Drills/&lt;id&gt;.json).</summary>
[Serializable]
public class TrainingDrill
{
#pragma warning disable 0649
    public string id;
    public string title;
    /// <summary>The level whose guide.json the GUIDED run reuses lessons from.</summary>
    public string levelId = "Level_01";
    public string scene = "Office_Level1";
    /// <summary>"plaza" (the level's normal start) or "office" (side quest skipped, door open, player at "spawn").</summary>
    public string start = "plaza";
    /// <summary>Scene path the player stands on at the start (office starts).</summary>
    public string spawn;
    /// <summary>Scene path the player faces at the start.</summary>
    public string face;
    /// <summary>Case file of the level to practise with instead of the level's own (e.g. case_002).</summary>
    public string practiceCase;
    /// <summary>The practice client's profile card, put in Profiles at the start (empty name = none).</summary>
    public TrainingProfile profile = new TrainingProfile();
    /// <summary>objectives.json steps marked done at the start, so the tracker and arrows point at the drill.</summary>
    public string[] objectivesDone = new string[0];
    /// <summary>Scene paths switched off for this drill (parts of the level it never uses).</summary>
    public string[] hide = new string[0];
    public TrainingRun guided = new TrainingRun();
    public TrainingRun mastery = new TrainingRun();
#pragma warning restore 0649

    public bool StartsInOffice => string.Equals(start, "office", StringComparison.OrdinalIgnoreCase);

    public static TrainingDrill Load(string drillId)
    {
        if (string.IsNullOrWhiteSpace(drillId)) return null;
        string path = TrainingCatalogFile.DrillFolder + drillId.Trim();
        TextAsset file = Resources.Load<TextAsset>(path);
        if (file == null)
        {
            Debug.LogWarning($"[Training] Resources/{path}.json not found.");
            return null;
        }
        TrainingDrill drill = Parse(file.text);
        if (drill == null) Debug.LogWarning($"[Training] Resources/{path}.json is not valid JSON.");
        return drill;
    }

    public static TrainingDrill Parse(string json)
    {
        try
        {
            TrainingDrill drill = JsonUtility.FromJson<TrainingDrill>(json);
            if (drill == null) return null;
            if (drill.objectivesDone == null) drill.objectivesDone = new string[0];
            if (drill.hide == null) drill.hide = new string[0];
            if (drill.guided == null) drill.guided = new TrainingRun();
            if (drill.mastery == null) drill.mastery = new TrainingRun();
            if (drill.profile == null) drill.profile = new TrainingProfile();
            return drill;
        }
        catch
        {
            return null;
        }
    }
}

[Serializable]
public class TrainingProfile
{
    public string id;
    public string name;
    public string description;
    /// <summary>ResidentCitizen, NonResidentCitizen, ResidentAlien or NonResidentAlien.</summary>
    public string classification = "ResidentCitizen";
    public string npcId;
}

/// <summary>A GUIDED or MASTERY run: steps play in order; hints react whenever their "when" happens.</summary>
[Serializable]
public class TrainingRun
{
    /// <summary>Shown on the practice ribbon, e.g. "Walk, look, follow".</summary>
    public string goal;
    public DrillLesson[] steps = new DrillLesson[0];
    public DrillLesson[] hints = new DrillLesson[0];
}

/// <summary>A guide lesson written in the drill, or "use": a lesson id copied from the level's guide.json.</summary>
[Serializable]
public class DrillLesson : GuideLesson
{
    public string use;
}

/// <summary>Turns one drill run into a GuideScript the normal GuideDirector plays.</summary>
public static class TrainingDrillScript
{
    public sealed class Result
    {
        public GuideScript Script;
        /// <summary>Step lesson ids in order (the ribbon counts them).</summary>
        public readonly List<string> Steps = new List<string>();
        /// <summary>The run is finished when this lesson ends.</summary>
        public string FinalStep;
        public string Problems;
    }

    /// <summary>
    /// GUIDED and MASTERY runs become a chain: the first step starts with the
    /// guide ("start"), each next step waits for the previous one ("after").
    /// A reused lesson keeps its own "when" (e.g. near a client) unless it
    /// names a lesson that is not in this drill; then it waits for the
    /// previous step instead. Hints keep their own "when" and never block.
    /// </summary>
    public static Result Compose(GuideScript baseScript, TrainingDrill drill, bool mastery)
    {
        var result = new Result();
        var problems = new System.Text.StringBuilder();
        TrainingRun run = drill == null ? null : mastery ? drill.mastery : drill.guided;
        var script = new GuideScript();
        if (baseScript != null)
        {
            script.platform = baseScript.platform;
            script.startDelay = baseScript.startDelay;
            script.lettersPerSecond = baseScript.lettersPerSecond;
            // Shared with the level's script (read only); the lessons are copies.
            script.guide = baseScript.guide ?? new GuideCharacter();
            script.trail = baseScript.trail ?? new GuideTrailSettings();
            script.waypoints = baseScript.waypoints ?? new GuideWaypoint[0];
        }
        script.enabled = true;
        script.rememberFinishedLessons = false;
        result.Script = script;
        if (run == null || run.steps == null || run.steps.Length == 0)
        {
            result.Problems = "the run has no steps";
            script.lessons = new GuideLesson[0];
            return result;
        }

        var steps = new List<GuideLesson>();
        foreach (DrillLesson entry in run.steps)
        {
            GuideLesson lesson = Resolve(entry, baseScript, problems);
            if (lesson != null) steps.Add(lesson);
        }
        var hints = new List<GuideLesson>();
        foreach (DrillLesson entry in run.hints ?? new DrillLesson[0])
        {
            GuideLesson lesson = Resolve(entry, baseScript, problems);
            if (lesson != null) hints.Add(lesson);
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (GuideLesson lesson in steps) ids.Add(lesson.id);
        foreach (GuideLesson lesson in hints) ids.Add(lesson.id);

        for (int i = 0; i < steps.Count; i++)
        {
            GuideLesson lesson = steps[i];
            string previous = i > 0 ? steps[i - 1].id : null;
            lesson.after = previous;
            lesson.when = Chain(lesson.when, previous, ids);
        }
        foreach (GuideLesson lesson in hints)
        {
            if (!string.IsNullOrEmpty(lesson.after) && !ids.Contains(lesson.after)) lesson.after = null;
            if (string.IsNullOrWhiteSpace(lesson.when)) problems.AppendLine($"- hint \"{lesson.id}\" has no \"when\".");
        }

        var all = new List<GuideLesson>(steps);
        all.AddRange(hints);
        script.lessons = all.ToArray();
        string report = script.Validate();
        if (!string.IsNullOrEmpty(report)) problems.Append(report);

        var kept = new HashSet<string>(StringComparer.Ordinal);
        foreach (GuideLesson lesson in script.lessons) kept.Add(lesson.id);
        foreach (GuideLesson lesson in steps)
            if (kept.Contains(lesson.id)) result.Steps.Add(lesson.id);
        result.FinalStep = result.Steps.Count > 0 ? result.Steps[result.Steps.Count - 1] : null;
        result.Problems = problems.ToString();
        return result;
    }

    /// <summary>A copy of the reused guide.json lesson, or of the drill's own lesson.</summary>
    private static GuideLesson Resolve(DrillLesson entry, GuideScript baseScript, System.Text.StringBuilder problems)
    {
        if (entry == null) return null;
        if (!string.IsNullOrWhiteSpace(entry.use))
        {
            string id = entry.use.Trim();
            GuideLesson source = null;
            if (baseScript != null && baseScript.lessons != null)
                foreach (GuideLesson lesson in baseScript.lessons)
                    if (lesson != null && lesson.id == id) source = lesson;
            if (source == null)
            {
                problems.AppendLine($"- \"use\": \"{id}\" is not a lesson in the level's guide.json; skipped.");
                return null;
            }
            GuideLesson copy = Copy(source);
            // A drill may restate when a reused lesson starts (e.g. "start").
            if (!string.IsNullOrWhiteSpace(entry.when)) copy.when = entry.when;
            return copy;
        }
        GuideLesson own = Copy(entry);
        if (string.IsNullOrWhiteSpace(own.id))
        {
            problems.AppendLine("- a drill lesson needs an \"id\" or a \"use\"; skipped.");
            return null;
        }
        return own;
    }

    /// <summary>The step's "when", with any lesson it names that is not in the drill replaced by the previous step.</summary>
    public static string Chain(string when, string previous, ICollection<string> ids)
    {
        string fallback = previous != null ? GuideTriggers.LessonKey(previous) : GuideTriggers.Start;
        if (string.IsNullOrWhiteSpace(when)) return fallback;
        string[] parts = when.Split('|');
        var kept = new List<string>();
        bool replaced = false;
        foreach (string raw in parts)
        {
            string part = raw.Trim();
            if (part.StartsWith("lesson:", StringComparison.Ordinal))
            {
                string id = part.Substring("lesson:".Length);
                int hash = id.IndexOf('#');
                if (hash >= 0) id = id.Substring(0, hash);
                if (!ids.Contains(id))
                {
                    if (!replaced) kept.Add(fallback);
                    replaced = true;
                    continue;
                }
            }
            if (part.Length > 0) kept.Add(part);
        }
        return kept.Count > 0 ? string.Join("|", kept) : fallback;
    }

    /// <summary>A plain GuideLesson copy (pages copied too), so the level's own script is never changed.</summary>
    public static GuideLesson Copy(GuideLesson source)
    {
        var copy = new GuideLesson
        {
            id = source.id,
            title = source.title,
            when = source.when,
            after = source.after,
            onlyIn = source.onlyIn,
            duringDialogue = source.duringDialogue,
            delay = source.delay,
            duringResults = source.duringResults,
            endIfGone = source.endIfGone
        };
        GuidePage[] pages = source.pages ?? new GuidePage[0];
        copy.pages = new GuidePage[pages.Length];
        for (int i = 0; i < pages.Length; i++)
        {
            GuidePage p = pages[i];
            copy.pages[i] = p == null ? null : new GuidePage
            {
                title = p.title, text = p.text, textTouch = p.textTouch, textDesktop = p.textDesktop, mood = p.mood,
                show = p.show, point = p.point, to = p.to, gesture = p.gesture, waitFor = p.waitFor, box = p.box,
                style = p.style, onlyOn = p.onlyOn, revealArrows = p.revealArrows
            };
        }
        return copy;
    }
}
