using System.Collections.Generic;

/// <summary>One verified client case on the office computer's register (T15): frozen, read-only lines.</summary>
public sealed class Level3RegisterEntry
{
    public bool Verified;
    public string Client = "";
    public string Status = "";
    /// <summary>(label, value) as the Auditor approved them; values are display text of the frozen exact amounts.</summary>
    public readonly List<KeyValuePair<string, string>> Lines = new List<KeyValuePair<string, string>>();
}

/// <summary>
/// PURPOSE:
/// The office computer's client register (Level 3 T15). Pure: no Unity
/// calls. It reads the SAVED campaign only and returns display lines; it
/// never changes the chapter snapshot, a claim, the wallet or a possession,
/// and it never reads an answer key (expectedFields, worksheet decisions).
///
/// Bea's entry is verified only when her chapter is completed (T14) with an
/// approved review that froze all four figures. Anything less is "not
/// verified" with no figures: nothing is filled in from fixtures. The deferred
/// company case has no entry in this release.
/// </summary>
public static class Level3RegisterRules
{
    public const string NotVerified = "No verified client case yet. Cases appear here once their chapter is complete.";

    /// <summary>Bea's register entry from the saved campaign (null-safe; never mutates data).</summary>
    public static Level3RegisterEntry Bea(PlayerProgressData data, string overstatement = "")
    {
        var entry = new Level3RegisterEntry { Client = "Beatriz Mendoza", Status = NotVerified };
        Level3ChapterCheckpoint chapter = data != null && data.HasCampaign ? data.campaign.levelThree : null;
        if (chapter == null || !chapter.IsSet || !Level3ClosingRules.Completed(chapter)) return entry;
        Level3ReviewRecord review = chapter.Case(Level3ChapterRules.BeaCaseId)?.review;
        if (review == null || !review.approved) return entry;
        string[] figures = { review.taxableIncome, review.taxDue, review.credits, review.remainingPayable };
        foreach (string figure in figures) if (string.IsNullOrEmpty(figure)) return entry;
        entry.Verified = true;
        entry.Status = "Approved by the Auditor  ·  Copy delivered";
        entry.Lines.Add(new KeyValuePair<string, string>("Taxable income", Money(review.taxableIncome)));
        entry.Lines.Add(new KeyValuePair<string, string>("Tax due", Money(review.taxDue)));
        entry.Lines.Add(new KeyValuePair<string, string>(Level3DeliveryRules.Label(Level3DeliveryRules.Credit), Money(review.credits)));
        entry.Lines.Add(new KeyValuePair<string, string>(Level3DeliveryRules.Label(Level3DeliveryRules.Payable), Money(review.remainingPayable)));
        if (!string.IsNullOrEmpty(overstatement) && Level3ClosingRules.FindingRecorded(chapter))
            entry.Lines.Add(new KeyValuePair<string, string>("Finding", "Draft costs ₱" + Level3Calculator.Format(overstatement) + " above her originals"));
        return entry;
    }

    private static string Money(string amount) => "₱" + Level3Calculator.Format(amount);
}
