using System;
using System.Collections.Generic;

/// <summary>One question the player can ask a Level 3 client (repeatable; never a reward or a grade).</summary>
[Serializable]
public class Level3Inquiry
{
    public string id;
    public string question;
    public string answer;
    /// <summary>Source ids this question is about: it comes first when the player last read one of them.</summary>
    public List<string> contextSources = new List<string>();
}

/// <summary>A Level 3 client at the plaza: who she is, where she works and what she says per chapter phase.</summary>
[Serializable]
public class Level3ClientContent
{
    public string id = "bea";
    public string speakerName = "Bea";
    public string fullName = "Beatriz Mendoza";
    public string portraitId = "default";
    public float height = 1.6f;
    public string idle = "Idle Relaxed";
    /// <summary>Scene path the worktable is set up near (her familiar Level 1 bench).</summary>
    public string anchorPath = "Env/Town/bench (1)";
    /// <summary>Before the reception assignment.</summary>
    public List<string> beforeAssignment = new List<string>();
    /// <summary>During calculator practice.</summary>
    public List<string> duringPractice = new List<string>();
    /// <summary>The first conversation; completing it hands the records over.</summary>
    public List<Level3TownLine> introduction = new List<Level3TownLine>();
    /// <summary>Opens the inquiry menu after the handover.</summary>
    public string inquiryPrompt = "What would you like to know?";
    public string inquiryAgain = "Anything else?";
    public List<Level3Inquiry> inquiries = new List<Level3Inquiry>();
    public string leaveChoice = "I'll review the papers first.";
    public string leaveAnswer = "Take your time. I'll be right here.";
    /// <summary>Printed on the envelope before the handover.</summary>
    public string envelopeLabel = "BEA MENDOZA\n2026 RECORDS";

    // ---- T08: the office visit and the withholding certificate ----
    /// <summary>Her walk to the office once her worksheet is collected: doorway points in order (the room spot follows).</summary>
    public List<TownPoint> toOffice = new List<TownPoint>();
    /// <summary>Where she waits: the conference room's client spot (the same one Marie and Rosa use).</summary>
    public string officeSpotPath = "Environment/OfficeBuilding/Rooms/ConferenceRoom/Client";
    /// <summary>Seconds after the worksheet is collected before she sets off.</summary>
    public float leaveDelaySeconds = 2.5f;
    /// <summary>Her questions prompt while she is on her way, and while she waits before the credit step.</summary>
    public string onTheWayPrompt = "I'm heading to your office. Ask me anything on the way.";
    public string officeWaitPrompt = "I'll wait here while you work out the tax. Anything about my records?";
    /// <summary>The credit step: what she says as she hands over B-WHT (completing it hands the certificate over, saved first).</summary>
    public List<Level3TownLine> certificateHandover = new List<Level3TownLine>();
    /// <summary>While the certificate is up: lower it to go on.</summary>
    public string certificateReadLine = "Here it is. Read it, then lower it when you're ready.";
    /// <summary>Her question and its three answers (credit, income, inspect).</summary>
    public string creditQuestion = "A client withheld some tax from a payment. Is that another business expense?";
    public List<Level3CreditAnswer> creditAnswers = new List<Level3CreditAnswer>();
    /// <summary>The help card's way back to her question.</summary>
    public string helpBack = "Back to Bea's question";
}

/// <summary>T08: one answer to Bea's certificate question (CONTENT "Withholding handover").</summary>
[Serializable]
public class Level3CreditAnswer
{
    /// <summary>Level3CertificateRules answer id: credit, income or inspect.</summary>
    public string id;
    /// <summary>What the player says.</summary>
    public string text;
    /// <summary>What Bea says back.</summary>
    public string reply;
}

[Serializable]
public class Level3TownLine
{
    /// <summary>"Npc" or "Player".</summary>
    public string speaker = "Npc";
    public string text;
    public string emote;
}

[Serializable]
public class Level3TownFile
{
    public Level3ClientContent bea = new Level3ClientContent();
}

/// <summary>
/// PURPOSE:
/// Level 3's plaza people (T04: Bea), read from Resources/Levels/Level_03/
/// town_life.json by Level3BeaTable; pure data and checks so the regression
/// suite reads the same file. CONTENT.md is the source of the authored lines.
/// Every graded fact is also printed on the papers: these lines only point
/// to them, never answer a worksheet decision.
/// </summary>
public static class Level3TownContent
{
    public const string ResourcePath = "Levels/Level_03/town_life";

    /// <summary>Problems with the file's Bea entry (empty = usable).</summary>
    public static List<string> Validate(Level3TownFile file)
    {
        var problems = new List<string>();
        Level3ClientContent bea = file?.bea;
        if (bea == null) { problems.Add("town_life has no bea"); return problems; }
        if (string.IsNullOrWhiteSpace(bea.speakerName) || string.IsNullOrWhiteSpace(bea.fullName)) problems.Add("bea needs speakerName and fullName");
        if (bea.height < 1.3f || bea.height > 2f) problems.Add("bea height must be 1.3..2 m");
        if (bea.introduction == null || bea.introduction.Count == 0 || bea.introduction.TrueForAll(l => l == null || l.speaker != "Npc"))
            problems.Add("bea needs an introduction with her own lines");
        if (bea.introduction != null)
            foreach (Level3TownLine line in bea.introduction)
                if (line == null || string.IsNullOrWhiteSpace(line.text) || (line.speaker != "Npc" && line.speaker != "Player"))
                    problems.Add("every introduction line needs a speaker (Npc/Player) and text");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (bea.inquiries == null || bea.inquiries.Count == 0) problems.Add("bea needs inquiries");
        else
            foreach (Level3Inquiry inquiry in bea.inquiries)
                if (inquiry == null || string.IsNullOrWhiteSpace(inquiry.id) || !ids.Add(inquiry.id) ||
                    string.IsNullOrWhiteSpace(inquiry.question) || string.IsNullOrWhiteSpace(inquiry.answer))
                    problems.Add("each inquiry needs a unique id, a question and an answer");
        if (string.IsNullOrWhiteSpace(bea.leaveChoice)) problems.Add("bea needs a way to leave the questions");
        // T08: the certificate conversation (route points are optional: without them she is placed unseen).
        if (bea.certificateHandover == null || bea.certificateHandover.Count == 0 || bea.certificateHandover.TrueForAll(l => l == null || l.speaker != "Npc"))
            problems.Add("bea needs certificateHandover lines of her own");
        else
            foreach (Level3TownLine line in bea.certificateHandover)
                if (line == null || string.IsNullOrWhiteSpace(line.text) || (line.speaker != "Npc" && line.speaker != "Player"))
                    problems.Add("every certificateHandover line needs a speaker (Npc/Player) and text");
        if (string.IsNullOrWhiteSpace(bea.creditQuestion) || string.IsNullOrWhiteSpace(bea.certificateReadLine)) problems.Add("bea needs creditQuestion and certificateReadLine");
        var answers = new List<string>();
        if (bea.creditAnswers != null)
            foreach (Level3CreditAnswer answer in bea.creditAnswers)
                if (answer != null && !string.IsNullOrWhiteSpace(answer.text) && !string.IsNullOrWhiteSpace(answer.reply)) answers.Add(answer.id);
        if (answers.Count != Level3CertificateRules.Answers.Length || !Array.TrueForAll(Level3CertificateRules.Answers, answers.Contains))
            problems.Add("bea needs exactly the three creditAnswers (credit, income, inspect), each with text and a reply");
        if (bea.toOffice != null && bea.toOffice.Exists(p => p == null || !p.IsSet)) problems.Add("every toOffice point needs a position");
        if (string.IsNullOrWhiteSpace(bea.officeSpotPath)) problems.Add("bea needs an officeSpotPath");
        return problems;
    }

    /// <summary>The authored answer for an answer id (null when missing).</summary>
    public static Level3CreditAnswer CreditAnswer(Level3ClientContent client, string id) =>
        client?.creditAnswers?.Find(a => a != null && a.id == id);

    /// <summary>The inquiries in the order to offer them: those about the record read last first, the rest as authored.</summary>
    public static List<Level3Inquiry> Ordered(Level3ClientContent client, string lastReadSource)
    {
        var first = new List<Level3Inquiry>();
        var rest = new List<Level3Inquiry>();
        if (client?.inquiries == null) return first;
        foreach (Level3Inquiry inquiry in client.inquiries)
        {
            if (inquiry == null) continue;
            bool context = !string.IsNullOrEmpty(lastReadSource) && inquiry.contextSources != null && inquiry.contextSources.Contains(lastReadSource);
            (context ? first : rest).Add(inquiry);
        }
        first.AddRange(rest);
        return first;
    }
}
