using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One group of worksheet decisions and its three hints: where to look, the idea, then specific help.</summary>
[Serializable]
public class Level3HintSet
{
    public List<string> decisions = new List<string>();
    public List<string> lines = new List<string>();
}

[Serializable]
public class Level3HintFile
{
    public List<Level3HintSet> hints = new List<Level3HintSet>();
}

/// <summary>
/// PURPOSE:
/// The worksheet's requested hints (T06), from
/// Resources/Levels/Level_03/worksheet.json with a built-in copy if the file
/// is missing or broken. Each of the twelve decisions has three hints that
/// go from source/concept ("read the Party shop receipt; book Section 4") to
/// specific help. They open only after the first complete check, only for a
/// place that check marked, and cost nothing: no XP, grade or trust changes.
/// </summary>
public static class Level3WorksheetHints
{
    public const string ResourcePath = "Levels/Level_03/worksheet";
    public const int Levels = 3;

    private static Level3HintFile cached;

    /// <summary>Problems with a hint file (empty when every decision has exactly three non-empty hints, once).</summary>
    public static List<string> Validate(Level3HintFile file)
    {
        var problems = new List<string>();
        if (file?.hints == null) { problems.Add("no hints"); return problems; }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Level3HintSet set in file.hints)
        {
            if (set?.decisions == null || set.decisions.Count == 0) { problems.Add("a hint set names no decision"); continue; }
            if (set.lines == null || set.lines.Count != Levels || set.lines.Exists(string.IsNullOrWhiteSpace))
                problems.Add(string.Join(",", set.decisions) + ": needs exactly " + Levels + " hints");
            foreach (string id in set.decisions)
            {
                if (Array.IndexOf(Level3WorksheetCheck.DecisionIds, id) < 0) problems.Add("unknown decision '" + id + "'");
                else if (!seen.Add(id)) problems.Add("decision '" + id + "' has two hint sets");
            }
        }
        foreach (string id in Level3WorksheetCheck.DecisionIds)
            if (!seen.Contains(id)) problems.Add("decision '" + id + "' has no hints");
        return problems;
    }

    /// <summary>The hint at level 1..3 for a decision (clamped), or "".</summary>
    public static string For(Level3HintFile file, string decisionId, int level)
    {
        if (file?.hints == null) return "";
        foreach (Level3HintSet set in file.hints)
            if (set?.decisions != null && set.decisions.Contains(decisionId) && set.lines != null && set.lines.Count > 0)
                return set.lines[Mathf.Clamp(level, 1, set.lines.Count) - 1];
        return "";
    }

    public static string For(string decisionId, int level) => For(Load(), decisionId, level);

    public static Level3HintFile Load()
    {
        if (cached != null) return cached;
        TextAsset file = Resources.Load<TextAsset>(ResourcePath);
        if (file != null)
        {
            try
            {
                Level3HintFile parsed = JsonUtility.FromJson<Level3HintFile>(file.text);
                List<string> problems = Validate(parsed);
                if (problems.Count == 0) return cached = parsed;
                Debug.LogWarning($"[Level3] Resources/{ResourcePath}.json: {string.Join("; ", problems)}. Using the built-in hints.");
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Level3] Resources/{ResourcePath}.json is not valid JSON ({error.Message}). Using the built-in hints.");
            }
        }
        return cached = BuiltIn();
    }

    /// <summary>Mirrors worksheet.json.</summary>
    public static Level3HintFile BuiltIn()
    {
        var file = new Level3HintFile();
        void Set(string[] ids, string a, string b, string c) => file.hints.Add(new Level3HintSet { decisions = new List<string>(ids), lines = new List<string> { a, b, c } });
        Set(new[] { "dest-R1", "dest-R2" },
            "Read Bea's Studio fees sheet: who paid her, and for what work?",
            "Money Bea earned from her design work is what her costs are taken from.",
            "Studio fees go in Income.");
        Set(new[] { "dest-R3", "dest-R4", "dest-R5" },
            "Read the cost schedule row and the record that backs it. Tax Code Book, Section 4: Costs and Purpose.",
            "This was paid for the studio's work, in 2026, and a record shows it.",
            "A supported business cost of the year goes in Allowable expenses.");
        Set(new[] { "dest-R6" },
            "Read the Party shop receipt again, or ask Bea: what was it for? Tax Code Book, Section 4.",
            "A cost lowers Bea's income only if it was spent to earn her fees. Her own celebration wasn't.",
            "Put the party purchases in Not deductible.");
        Set(new[] { "dest-R7" },
            "Compare the Print bill with the Cost schedule. Tax Code Book, Section 5: Count Once.",
            "Both papers show the same printing order, PRINT-26. Counting the bill as well would count it twice.",
            "Keep the printing bill in Supporting only: it backs up the schedule's row.");
        Set(new[] { "reason-R6" },
            "Ask Bea what the party purchases were for.",
            "Was the dinner for the studio's work, or for her own celebration?",
            "Choose Personal purpose.");
        Set(new[] { "reason-R7" },
            "Look for PRINT-26 on Bea's other papers. Tax Code Book, Section 5: Count Once.",
            "Bea's draft adds the printing bill on top of a cost schedule that already includes it.",
            "Choose Already represented.");
        Set(new[] { "related-R7" },
            "Which worksheet row already records the same printing order?",
            "Look for the row with the same reference, PRINT-26, on the Cost schedule.",
            "Link it to Printing and supplies, the Cost schedule's row.");
        Set(new[] { "subtotal-income", "subtotal-expenses" },
            "A subtotal adds exactly the strips in its own section, each once.",
            "Tap the SUBTOTAL slot, then each strip in the section with + between them, then =, and attach the tape.",
            "Check no strip is missing, typed by hand, counted twice or taken from another section.");
        return file;
    }
}
