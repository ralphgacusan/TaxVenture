using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The data shape of the editable level guide file
/// Assets/Resources/Tutorials/&lt;levelId&gt;/guide.json (Level 1:
/// Tutorials/Level_01/guide.json). It holds everything the tutorial says
/// and does: the guide character and portraits, the welcome card, the
/// objective arrows (waypoints) and the lessons with their pages.
///
/// Pure data + loading; no scene work. Field names are case-sensitive for
/// JsonUtility. Missing fields keep the defaults written below.
///
/// CONNECTS WITH:
/// - GuideTriggers (the "when" / "waitFor" vocabulary)
/// - GuideDirector (plays the lessons)
/// - ObjectiveGuide (arrows, beacon and minimap pin from "waypoints")
/// </summary>
[Serializable]
public class GuideScript
{
    // Filled by JsonUtility at runtime, so the compiler sees no assignments.
#pragma warning disable 0649
    public bool enabled = true;
    /// <summary>"touch" (default: TaxVenture is a phone game), "desktop" or "auto" (touch on phones and in the Editor).</summary>
    public string platform = "touch";
    /// <summary>True: finished lessons stay finished on the next play (saved on this device).</summary>
    public bool rememberFinishedLessons;
    /// <summary>Seconds after the HUD appears before the first lesson starts.</summary>
    public float startDelay = 0.8f;
    /// <summary>Letters per second for the typewriter text.</summary>
    public float lettersPerSecond = 55f;

    public GuideCharacter guide = new GuideCharacter();
    public GuideWelcome welcome = new GuideWelcome();
    public GuideTrailSettings trail = new GuideTrailSettings();
    public GuideWaypoint[] waypoints = new GuideWaypoint[0];
    public GuideLesson[] lessons = new GuideLesson[0];
#pragma warning restore 0649

    public const string FileName = "guide";

    public static string ResourcePath(string levelId) => $"Tutorials/{levelId}/{FileName}";

    /// <summary>Loads and validates the level's guide file. Null when missing or unusable.</summary>
    public static GuideScript Load(string levelId)
    {
        string path = ResourcePath(levelId);
        TextAsset file = Resources.Load<TextAsset>(path);
        if (file == null)
        {
            Debug.Log($"[Guide] Resources/{path}.json not found; no tutorial for {levelId}.");
            return null;
        }

        GuideScript script;
        try
        {
            script = JsonUtility.FromJson<GuideScript>(file.text);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Guide] {path}.json is not valid JSON ({e.Message}). Tutorial skipped.");
            return null;
        }

        if (script == null) return null;
        string problems = script.Validate();
        if (!string.IsNullOrEmpty(problems)) Debug.LogWarning($"[Guide] {path}.json:\n{problems}");
        return script;
    }

    /// <summary>Parses JSON text (used by checks); null when invalid.</summary>
    public static GuideScript Parse(string json)
    {
        try { return JsonUtility.FromJson<GuideScript>(json); }
        catch { return null; }
    }

    /// <summary>
    /// Drops lessons/pages that cannot work and returns a readable report
    /// (empty when everything is fine). Never throws.
    /// </summary>
    public string Validate()
    {
        var report = new System.Text.StringBuilder();
        if (guide == null) guide = new GuideCharacter();
        if (welcome == null) welcome = new GuideWelcome();
        if (trail == null) trail = new GuideTrailSettings();
        if (waypoints == null) waypoints = new GuideWaypoint[0];

        var keptLessons = new List<GuideLesson>();
        var ids = new HashSet<string>();
        GuideLesson[] source = lessons ?? new GuideLesson[0];
        for (int i = 0; i < source.Length; i++)
        {
            GuideLesson lesson = source[i];
            if (lesson == null) continue;
            string name = string.IsNullOrEmpty(lesson.id) ? $"lesson #{i + 1}" : $"lesson \"{lesson.id}\"";

            if (string.IsNullOrWhiteSpace(lesson.id))
            {
                report.AppendLine($"- {name}: needs an \"id\"; skipped.");
                continue;
            }
            if (!ids.Add(lesson.id))
            {
                report.AppendLine($"- {name}: duplicate id; skipped.");
                continue;
            }

            string whenProblem = GuideTriggers.Validate(lesson.when, allowWaitOnly: false);
            if (whenProblem != null)
            {
                report.AppendLine($"- {name}: when: {whenProblem}; skipped.");
                continue;
            }
            if (!string.IsNullOrEmpty(lesson.onlyIn))
            {
                string onlyProblem = GuideTriggers.Validate(lesson.onlyIn, allowWaitOnly: false);
                if (onlyProblem != null)
                {
                    report.AppendLine($"- {name}: onlyIn: {onlyProblem}; ignored.");
                    lesson.onlyIn = null;
                }
            }

            var keptPages = new List<GuidePage>();
            GuidePage[] pages = lesson.pages ?? new GuidePage[0];
            for (int p = 0; p < pages.Length; p++)
            {
                GuidePage page = pages[p];
                if (page == null) continue;
                bool isCard = page.IsWelcomeCard;
                if (!isCard && string.IsNullOrWhiteSpace(page.text) &&
                    string.IsNullOrWhiteSpace(page.textTouch) && string.IsNullOrWhiteSpace(page.textDesktop))
                {
                    report.AppendLine($"- {name} page {p + 1}: no text; skipped.");
                    continue;
                }
                if (!string.IsNullOrEmpty(page.waitFor))
                {
                    string waitProblem = GuideTriggers.Validate(page.waitFor, allowWaitOnly: true);
                    if (waitProblem != null)
                    {
                        report.AppendLine($"- {name} page {p + 1}: waitFor: {waitProblem}; it waits for a tap instead.");
                        page.waitFor = null;
                    }
                }
                if (!string.IsNullOrEmpty(page.gesture) && Array.IndexOf(GuideTriggers.Gestures, page.gesture) < 0)
                {
                    report.AppendLine($"- {name} page {p + 1}: unknown gesture \"{page.gesture}\"; using tap.");
                    page.gesture = GuideTriggers.GestureTap;
                }
                keptPages.Add(page);
            }

            if (keptPages.Count == 0)
            {
                report.AppendLine($"- {name}: no usable pages; skipped.");
                continue;
            }
            lesson.pages = keptPages.ToArray();
            keptLessons.Add(lesson);
        }

        // "after" must name a lesson that exists, otherwise the lesson could never start.
        for (int i = 0; i < keptLessons.Count; i++)
        {
            GuideLesson lesson = keptLessons[i];
            if (!string.IsNullOrEmpty(lesson.after) && !ids.Contains(lesson.after))
            {
                report.AppendLine($"- lesson \"{lesson.id}\": after \"{lesson.after}\" does not exist; ignored.");
                lesson.after = null;
            }
        }

        lessons = keptLessons.ToArray();
        return report.ToString();
    }

    /// <summary>Waypoint for an objective step trigger, or null.</summary>
    public GuideWaypoint FindWaypoint(string stepTrigger)
    {
        if (waypoints == null || string.IsNullOrEmpty(stepTrigger)) return null;
        string key = ObjectiveTriggers.Normalize(stepTrigger);
        for (int i = 0; i < waypoints.Length; i++)
            if (waypoints[i] != null && ObjectiveTriggers.Normalize(waypoints[i].step) == key)
                return waypoints[i];
        return null;
    }

    public string PortraitFile(string mood)
    {
        if (guide == null || guide.portraits == null) return null;
        string wanted = string.IsNullOrEmpty(mood) ? "default" : mood;
        string fallback = null;
        for (int i = 0; i < guide.portraits.Length; i++)
        {
            GuidePortrait p = guide.portraits[i];
            if (p == null || string.IsNullOrEmpty(p.file)) continue;
            if (string.Equals(p.mood, wanted, StringComparison.OrdinalIgnoreCase)) return p.file;
            if (string.Equals(p.mood, "default", StringComparison.OrdinalIgnoreCase)) fallback = p.file;
        }
        return fallback;
    }
}

[Serializable]
public class GuideCharacter
{
    public string name = "Arthur";
    public string role = "Senior Tax Consultant";
    /// <summary>Folder under Resources holding the portrait images (Sprite import type).</summary>
    public string portraitFolder = "Tutorials/Portraits";
    public GuidePortrait[] portraits = new GuidePortrait[0];
}

[Serializable]
public class GuidePortrait
{
    public string mood;
    public string file;
}

[Serializable]
public class GuideWelcome
{
    public string kicker = "WELCOME TO";
    public string title = "TaxVenture!";
    public string subtitle = "LEVEL 1 · YOUR FIRST DAY";
    public string tagline = "";
    public string objectivesHeading = "TODAY'S MAIN OBJECTIVES";
    public bool listObjectives = true;
    public string button = "START";
}

[Serializable]
public class GuideTrailSettings
{
    public bool enabled = true;
    /// <summary>True: arrows, marker and minimap pin stay hidden until a page with "revealArrows" shows (or the tutorial is skipped).</summary>
    public bool waitForGuide = true;
    /// <summary>Hex colour of the ground arrows and the goal pin.</summary>
    public string color = "#64D7FF";
    /// <summary>Metres between ground arrows.</summary>
    public float spacing = 1.1f;
    /// <summary>How far ahead the arrows reach (metres).</summary>
    public float length = 26f;
    /// <summary>Arrow travel speed (metres per second).</summary>
    public float speed = 1.6f;
    /// <summary>Floating goal marker over the target (clamps to the screen edge when off-screen).</summary>
    public bool beacon = true;
    /// <summary>Goal pin, route dots and attention pops on the minimap.</summary>
    public bool minimap = true;
    /// <summary>Arrows hide when the player is this close to the goal (metres).</summary>
    public float arriveDistance = 2.2f;
    /// <summary>Seconds between minimap reminder pops while the goal is still far away (0 = only on change).</summary>
    public float minimapReminderSeconds = 8f;

    public Color ParsedColor
    {
        get
        {
            return !string.IsNullOrEmpty(color) && ColorUtility.TryParseHtmlString(color, out Color c)
                ? c : new Color32(100, 215, 255, 255);
        }
    }
}

[Serializable]
public class GuideWaypoint
{
    /// <summary>The objectives.json step trigger this waypoint belongs to, e.g. "event:ReceptionistTalkedTo".</summary>
    public string step;
    /// <summary>One or more targets joined with |: scene paths or type:ComponentName.</summary>
    public string target;
    /// <summary>"nearestAvailable" (default), "nearest" or "first".</summary>
    public string pick;
    /// <summary>Short name on the floating marker, e.g. "Receptionist".</summary>
    public string label;
    /// <summary>Optional per-waypoint arrive distance (0 = trail default).</summary>
    public float arriveDistance;
}

[Serializable]
public class GuideLesson
{
    public string id;
    /// <summary>Optional chip shown above the text, e.g. "WALKING".</summary>
    public string title;
    /// <summary>What starts the lesson (see GuideTriggers).</summary>
    public string when;
    /// <summary>Optional lesson id that must be finished first.</summary>
    public string after;
    /// <summary>Optional condition: the lesson pauses (hidden) while it is false, e.g. "mode:Workstation".</summary>
    public string onlyIn;
    /// <summary>True: may show over a conversation (use for short hints).</summary>
    public bool duringDialogue;
    /// <summary>Seconds to wait after "when" before showing.</summary>
    public float delay;
    /// <summary>True: may show over a reward/summary card (the card's auto-continue waits).</summary>
    public bool duringResults;
    /// <summary>True: when "onlyIn" stops being true, the lesson ends instead of pausing (e.g. the card closed).</summary>
    public bool endIfGone;
    public GuidePage[] pages;
}

[Serializable]
public class GuidePage
{
    /// <summary>Optional chip above this page's text (defaults to the lesson title).</summary>
    public string title;
    public string text;
    /// <summary>Replaces text on phones/tablets.</summary>
    public string textTouch;
    /// <summary>Replaces text on PC.</summary>
    public string textDesktop;
    /// <summary>Portrait mood: default, wow, confused, sad, angry (see guide.portraits).</summary>
    public string mood;
    /// <summary>"welcome" shows the welcome card instead of a speech box.</summary>
    public string show;
    /// <summary>What to spotlight/point at (see GuideTargets).</summary>
    public string point;
    /// <summary>Drag/swipe destination for the hand.</summary>
    public string to;
    /// <summary>tap, drag, swipe, joystick, hold or none.</summary>
    public string gesture;
    /// <summary>Empty = tap to continue. Otherwise the action that finishes the page.</summary>
    public string waitFor;
    /// <summary>auto, top, bottom or center.</summary>
    public string box;
    /// <summary>"box" (default) or "hint" (small bubble, never blocks).</summary>
    public string style;
    /// <summary>Only show on this platform: "touch" or "desktop" (empty = both).</summary>
    public string onlyOn;
    /// <summary>True: the objective arrows, goal marker and minimap pin appear from this page on.</summary>
    public bool revealArrows;

    public bool IsWelcomeCard => string.Equals(show, "welcome", StringComparison.OrdinalIgnoreCase);
    public bool IsHint => string.Equals(style, "hint", StringComparison.OrdinalIgnoreCase);
    public bool WaitsForTap => string.IsNullOrWhiteSpace(waitFor) || waitFor.Trim() == GuideTriggers.Tap;

    public string TextFor(bool touch)
    {
        string specific = touch ? textTouch : textDesktop;
        return !string.IsNullOrWhiteSpace(specific) ? specific : text ?? string.Empty;
    }
}
