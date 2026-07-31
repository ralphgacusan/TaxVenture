using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Vague, investigator-style hints for the Corkboard's Hint area — replaces
/// any prior explicit/revealing hint text. Hints suggest WHERE to look, not
/// WHAT is wrong or HOW to fix it, matching the design goal of "quick
/// investigator notes rather than explicit solutions."
///
/// Hints are static/authored (same reasoning as DocumentDataProvider /
/// TaxCodeBookData) — a fixed pool the Hint object cycles through or
/// displays statically, not derived from live compliance-check results
/// (that would reveal too much, same principle established for the
/// Auditor's vague ShortLabel messages).
/// </summary>
public static class CorkboardHintProvider
{
    public static List<string> GetHints()
    {
        return new List<string>
        {
            "Income doesn't seem consistent.",
            "Double-check withholding.",
            "Missing attachment?",
            "Verify declared source.",
            "Numbers don't quite add up somewhere.",
            "Worth a second look at the bank records.",
        };
    }
}