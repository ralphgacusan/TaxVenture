using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// PURPOSE:
/// The vocabulary of guide.json "when", "onlyIn" and "waitFor" values.
/// Pure parsing/validation, no Unity scene calls, so it can be checked
/// outside the editor. GuideDirector does the listening.
///
/// EVENTS (counted; "when" = happened at any time, "waitFor" = happened
/// after the page appeared). Add "#n" to need n times, e.g. value:placed#3.
///   start                       the level HUD is up and the player is free
///   lesson:&lt;id&gt;                 that lesson finished
///   event:&lt;name&gt;                any objectives event (see ObjectiveTriggers)
///   state:&lt;game state&gt;          the game entered that state
///   dialogue:start / dialogue:end   a conversation began / ended
///   value:selected / value:placed / value:rejected   value transfer
///   corkboard:placed / corkboard:wrong               side-quest board
///
/// CONDITIONS (true right now):
///   mode:&lt;FirstPerson|Workstation|CorkboardSideQuest|ProfileViewer|Interview&gt;
///   step:&lt;objective trigger&gt;     that objectives.json step is the current one
///   near:&lt;metres&gt;:&lt;target&gt;      the player is that close to the target
///   visible:&lt;target&gt;            the target exists, is active and is on screen
///   profiles:&lt;n&gt;                at least n taxpayer profiles collected
///   case:filled                 the three case-folder answers are filled in
///   case:correct                every graded case-folder field holds the right answer (Tutorial Hub mastery)
///   dialogue:active             a conversation is on screen right now
///   open:&lt;notes|profiles|folder&gt;  that HUD window is open right now
///   active:&lt;target&gt;             the target object exists and is switched on
///   card:&lt;audit|sidequest|level|result|any&gt;  that reward/summary card is open
///   not:&lt;condition&gt;             the condition is false (e.g. not:open:notes)
///
/// WAIT-ONLY (page "waitFor"):
///   tap (default) · move:&lt;metres&gt; · look:&lt;degrees&gt; · seconds:&lt;n&gt;
///
/// Join several with | to mean "any of them".
/// </summary>
public static class GuideTriggers
{
    public enum Kind { Event, Condition, Move, Look, Seconds, Tap }

    public struct Trigger
    {
        public Kind Kind;
        /// <summary>Event: counter key. Condition: the prefix ("mode:", "near:" ...).</summary>
        public string Key;
        /// <summary>Required count for events (1 by default).</summary>
        public int Count;
        /// <summary>Condition argument (mode name, step trigger, target ...).</summary>
        public string Arg;
        /// <summary>Numeric argument (metres, degrees, seconds, profiles).</summary>
        public float Number;
        /// <summary>"not:" in front of a condition: true when the condition is false.</summary>
        public bool Negate;
    }

    public const string Tap = "tap";
    public const string Start = "start";
    public const string LessonPrefix = "lesson:";
    public const string DialogueStart = "dialogue:start";
    public const string DialogueEnd = "dialogue:end";
    public const string ValueSelected = "value:selected";
    public const string ValuePlaced = "value:placed";
    public const string ValueRejected = "value:rejected";
    public const string CorkboardPlaced = "corkboard:placed";
    public const string CorkboardWrong = "corkboard:wrong";

    public const string ModePrefix = "mode:";
    public const string StepPrefix = "step:";
    public const string NearPrefix = "near:";
    public const string VisiblePrefix = "visible:";
    public const string ProfilesPrefix = "profiles:";
    public const string CaseFilled = "case:filled";
    public const string CaseCorrect = "case:correct";
    public const string DialogueActive = "dialogue:active";
    public const string OpenPrefix = "open:";
    public const string ActivePrefix = "active:";
    public const string CardPrefix = "card:";
    public static readonly string[] Cards = { "audit", "sidequest", "level", "result", "any" };
    public const string NotPrefix = "not:";
    public static readonly string[] Windows = { "notes", "profiles", "folder" };

    public const string MovePrefix = "move:";
    public const string LookPrefix = "look:";
    public const string SecondsPrefix = "seconds:";

    public const string GestureTap = "tap";
    public static readonly string[] Gestures = { "tap", "drag", "swipe", "joystick", "hold", "none" };

    public static readonly string[] Modes = { "FirstPerson", "Workstation", "Interview", "CorkboardSideQuest", "ProfileViewer" };

    private static readonly string[] FixedEvents =
    {
        Start, DialogueStart, DialogueEnd, ValueSelected, ValuePlaced, ValueRejected, CorkboardPlaced, CorkboardWrong
    };

    // Every prefix a new "|" alternative may start with. Anything else after
    // a "|" belongs to the previous alternative (e.g. "step:area:A|B" paths).
    private static readonly string[] Prefixes =
    {
        Tap, Start, LessonPrefix, ObjectiveTriggers.EventPrefix, ObjectiveTriggers.StatePrefix, "dialogue:", "value:",
        "corkboard:", OpenPrefix, ActivePrefix, CardPrefix, NotPrefix, ModePrefix, StepPrefix, NearPrefix, VisiblePrefix, ProfilesPrefix, "case:", MovePrefix,
        LookPrefix, SecondsPrefix
    };

    public static string LessonKey(string id) => LessonPrefix + id;

    /// <summary>Splits "a|b" into alternatives, keeping "|" inside scene-path arguments.</summary>
    public static List<string> SplitAlternatives(string text)
    {
        var parts = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return parts;
        string[] raw = text.Split('|');
        for (int i = 0; i < raw.Length; i++)
        {
            string piece = raw[i].Trim();
            if (parts.Count > 0 && !StartsWithKnownPrefix(piece))
                parts[parts.Count - 1] = parts[parts.Count - 1] + "|" + piece;
            else
                parts.Add(piece);
        }
        return parts;
    }

    private static bool StartsWithKnownPrefix(string piece)
    {
        for (int i = 0; i < Prefixes.Length; i++)
            if (piece.StartsWith(Prefixes[i], StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Parses all alternatives; invalid ones are left out (see Validate).</summary>
    public static List<Trigger> ParseAll(string text)
    {
        var result = new List<Trigger>();
        List<string> parts = SplitAlternatives(text);
        for (int i = 0; i < parts.Count; i++)
            if (TryParse(parts[i], out Trigger trigger, out _)) result.Add(trigger);
        return result;
    }

    /// <summary>Null when every alternative is valid, otherwise a short reason.</summary>
    public static string Validate(string text, bool allowWaitOnly)
    {
        List<string> parts = SplitAlternatives(text);
        if (parts.Count == 0) return "missing";
        for (int i = 0; i < parts.Count; i++)
        {
            if (!TryParse(parts[i], out Trigger trigger, out string problem)) return problem;
            if (!allowWaitOnly && (trigger.Kind == Kind.Move || trigger.Kind == Kind.Look ||
                                   trigger.Kind == Kind.Seconds || trigger.Kind == Kind.Tap))
                return $"\"{parts[i]}\" can only be used in waitFor";
        }
        return null;
    }

    public static bool TryParse(string text, out Trigger trigger, out string problem)
    {
        trigger = default;
        trigger.Count = 1;
        problem = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            problem = "empty trigger";
            return false;
        }
        text = text.Trim();

        // "not:<condition>" flips a condition (e.g. not:open:notes = Notes closed).
        if (text.StartsWith(NotPrefix, StringComparison.Ordinal))
        {
            if (!TryParse(text.Substring(NotPrefix.Length), out trigger, out problem)) return false;
            if (trigger.Kind != Kind.Condition)
            {
                problem = $"\"{text}\": not: only works with conditions (mode:, open:, active:, near: ...)";
                return false;
            }
            trigger.Negate = !trigger.Negate;
            return true;
        }

        // Wait-only.
        if (text == Tap)
        {
            trigger.Kind = Kind.Tap;
            return true;
        }
        if (TryNumber(text, MovePrefix, out float number, ref problem))
        {
            trigger.Kind = Kind.Move;
            trigger.Number = number;
            return true;
        }
        if (problem != null) return false;
        if (TryNumber(text, LookPrefix, out number, ref problem))
        {
            trigger.Kind = Kind.Look;
            trigger.Number = number;
            return true;
        }
        if (problem != null) return false;
        if (TryNumber(text, SecondsPrefix, out number, ref problem))
        {
            trigger.Kind = Kind.Seconds;
            trigger.Number = number;
            return true;
        }
        if (problem != null) return false;

        // Conditions.
        if (text.StartsWith(ModePrefix, StringComparison.Ordinal))
        {
            string mode = text.Substring(ModePrefix.Length).Trim();
            if (Array.IndexOf(Modes, mode) < 0)
            {
                problem = $"unknown mode \"{mode}\" (use {string.Join(", ", Modes)})";
                return false;
            }
            trigger.Kind = Kind.Condition;
            trigger.Key = ModePrefix;
            trigger.Arg = mode;
            return true;
        }
        if (text.StartsWith(StepPrefix, StringComparison.Ordinal))
        {
            string step = ObjectiveTriggers.Normalize(text.Substring(StepPrefix.Length));
            string stepProblem = ObjectiveTriggers.Validate(step);
            if (stepProblem != null)
            {
                problem = "step: " + stepProblem;
                return false;
            }
            trigger.Kind = Kind.Condition;
            trigger.Key = StepPrefix;
            trigger.Arg = step;
            return true;
        }
        if (text.StartsWith(NearPrefix, StringComparison.Ordinal))
        {
            string rest = text.Substring(NearPrefix.Length);
            int colon = rest.IndexOf(':');
            if (colon <= 0 || !float.TryParse(rest.Substring(0, colon), NumberStyles.Float, CultureInfo.InvariantCulture, out number) ||
                number <= 0f || colon == rest.Length - 1)
            {
                problem = "near needs metres and a target, e.g. near:4:type:TaxpayerNpcInteractable";
                return false;
            }
            string targetProblem = GuideTargets.Validate(rest.Substring(colon + 1));
            if (targetProblem != null)
            {
                problem = "near: " + targetProblem;
                return false;
            }
            trigger.Kind = Kind.Condition;
            trigger.Key = NearPrefix;
            trigger.Number = number;
            trigger.Arg = rest.Substring(colon + 1).Trim();
            return true;
        }
        if (text.StartsWith(VisiblePrefix, StringComparison.Ordinal))
        {
            string target = text.Substring(VisiblePrefix.Length).Trim();
            string targetProblem = GuideTargets.Validate(target);
            if (targetProblem != null)
            {
                problem = "visible: " + targetProblem;
                return false;
            }
            trigger.Kind = Kind.Condition;
            trigger.Key = VisiblePrefix;
            trigger.Arg = target;
            return true;
        }
        if (TryNumber(text, ProfilesPrefix, out number, ref problem))
        {
            trigger.Kind = Kind.Condition;
            trigger.Key = ProfilesPrefix;
            trigger.Number = number;
            return true;
        }
        if (problem != null) return false;
        if (text.StartsWith(CardPrefix, StringComparison.Ordinal))
        {
            string card = text.Substring(CardPrefix.Length).Trim();
            if (Array.IndexOf(Cards, card) < 0)
            {
                problem = $"unknown card \"{card}\" (use {string.Join(", ", Cards)})";
                return false;
            }
            trigger.Kind = Kind.Condition;
            trigger.Key = CardPrefix;
            trigger.Arg = card;
            return true;
        }
        if (text.StartsWith(ActivePrefix, StringComparison.Ordinal))
        {
            string target = text.Substring(ActivePrefix.Length).Trim();
            string targetProblem = GuideTargets.Validate(target);
            if (targetProblem != null)
            {
                problem = "active: " + targetProblem;
                return false;
            }
            trigger.Kind = Kind.Condition;
            trigger.Key = ActivePrefix;
            trigger.Arg = target;
            return true;
        }
        if (text.StartsWith(OpenPrefix, StringComparison.Ordinal))
        {
            string window = text.Substring(OpenPrefix.Length).Trim();
            if (Array.IndexOf(Windows, window) < 0)
            {
                problem = $"unknown window \"{window}\" (use {string.Join(", ", Windows)})";
                return false;
            }
            trigger.Kind = Kind.Condition;
            trigger.Key = OpenPrefix;
            trigger.Arg = window;
            return true;
        }
        if (text == DialogueActive)
        {
            trigger.Kind = Kind.Condition;
            trigger.Key = DialogueActive;
            return true;
        }
        if (text == CaseFilled || text == CaseCorrect)
        {
            trigger.Kind = Kind.Condition;
            trigger.Key = text;
            return true;
        }

        // Events, with an optional "#n" count.
        string key = text;
        int hash = text.LastIndexOf('#');
        if (hash > 0)
        {
            if (!int.TryParse(text.Substring(hash + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) || count < 1)
            {
                problem = $"\"{text}\": the count after # must be a whole number of 1 or more";
                return false;
            }
            trigger.Count = count;
            key = text.Substring(0, hash).Trim();
        }

        if (Array.IndexOf(FixedEvents, key) >= 0 ||
            (key.StartsWith(LessonPrefix, StringComparison.Ordinal) && key.Length > LessonPrefix.Length))
        {
            trigger.Kind = Kind.Event;
            trigger.Key = key;
            return true;
        }
        if (key.StartsWith(ObjectiveTriggers.EventPrefix, StringComparison.Ordinal) ||
            key.StartsWith(ObjectiveTriggers.StatePrefix, StringComparison.Ordinal))
        {
            string normalized = ObjectiveTriggers.Normalize(key);
            string objectiveProblem = ObjectiveTriggers.Validate(normalized);
            if (objectiveProblem != null)
            {
                problem = objectiveProblem;
                return false;
            }
            trigger.Kind = Kind.Event;
            trigger.Key = normalized;
            return true;
        }

        problem = $"unknown trigger \"{text}\"";
        return false;
    }

    private static bool TryNumber(string text, string prefix, out float number, ref string problem)
    {
        number = 0f;
        if (!text.StartsWith(prefix, StringComparison.Ordinal)) return false;
        if (!float.TryParse(text.Substring(prefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out number) || number < 0f)
        {
            problem = $"\"{text}\" needs a number after {prefix}";
            return false;
        }
        return true;
    }
}
