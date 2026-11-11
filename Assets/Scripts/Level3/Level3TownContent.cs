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
    /// <summary>The first conversation; completing it hands the records over. Its last line presents the envelope (a retry after a failed save plays only that line).</summary>
    public List<Level3TownLine> introduction = new List<Level3TownLine>();
    /// <summary>T04 buildup: after the first record is read and put back she introduces her organizer; the last line waits for Help Bea / Later.</summary>
    public List<Level3TownLine> organizerIntro = new List<Level3TownLine>();
    public string helpChoice = "Help Bea";
    public string laterChoice = "Later";
    /// <summary>T04 buildup: her concise continuation (after Later, or on a restore): the questions card with Help Bea first.</summary>
    public string continuePrompt = "Ready to sort them? Ask me anything first if you like.";
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
    public string certificateReadLine = "I'll leave it here for you. Take it when we're done.";
    /// <summary>Her question and its three answers (credit, income, inspect).</summary>
    public string creditQuestion = "A client withheld some tax from a payment. Is that another business expense?";
    public List<Level3CreditAnswer> creditAnswers = new List<Level3CreditAnswer>();
    /// <summary>The help card's way back to her question.</summary>
    public string helpBack = "Back to Bea's question";

    // T10: her balance question at the conference-room spot and the copy handover (OFFICE_CASE_PLAY).
    /// <summary>Her copy is still with the Auditor.</summary>
    public string copyWaitingLine = "The Auditor still has my copy? I'll wait here for it.";
    /// <summary>One short question about the approved copy's balance (normally at most 30 words).</summary>
    public string balanceQuestion = "Before I keep it: how much is left to pay after the tax already withheld?";
    public string balancePrompt = "Point to the figure on her copy";
    /// <summary>One contextual sentence for each figure that does not answer it (nothing is saved).</summary>
    public string taxDueHint = "That's the whole tax, before the withheld tax comes off. Which line shows what's left?";
    public string creditHint = "That's what Harbor Menu already withheld for me. Which line shows what's still left?";
    public string balanceReply = "So that's what I still pay. The withheld tax already counts toward it. That makes sense.";
    /// <summary>The handover line waits for the copy (drag it onto her, or tap the folder icon).</summary>
    public string copyHandoverLine = "May I keep this copy with my records?";
    public string copyHandoverPrompt = "Drag the client copy to Bea";
    public string copyRetryPrompt = "It didn't save. Drag the client copy to Bea again.";
    /// <summary>Her one acknowledgment once the copy is hers, and what she says afterwards.</summary>
    public string deliveredLine = "Thank you. Now I know what I still owe, and why.";
    public string afterDeliveryLine = "Thanks again. I'll keep my copy with my records.";

    // T14: her one question about her own draft, after the delivery (Level3ClosingRules).
    /// <summary>One short question about what her file shows (normally at most 30 words).</summary>
    public string findingQuestion = "Before I go: my draft listed \u20B1354,000 in costs, but you used \u20B1300,000. What did my papers show?";
    public string findingPrompt = "Choose what her file shows";
    public string findingTitle = "WHAT BEA'S FILE SHOWS";
    /// <summary>Ends the conversation and opens her papers (nothing saved; the question waits).</summary>
    public string findingReview = "Review her records";
    /// <summary>The three statements: the supported one, intent, a wider pattern (ids: Level3ClosingRules.Statements).</summary>
    public List<Level3CreditAnswer> findingChoices = new List<Level3CreditAnswer>
    {
        new Level3CreditAnswer { id = Level3ClosingRules.Supported, text = "Party \u20B124,000 was private; printing \u20B130,000 listed twice.",
            reply = "So my originals corrected the draft by \u20B154,000. That shows the mistake, not why it was made." },
        new Level3CreditAnswer { id = Level3ClosingRules.Intent, text = "Someone added those costs on purpose.",
            reply = "My papers show what was wrong, not why. Let's keep to what they show." },
        new Level3CreditAnswer { id = Level3ClosingRules.Pattern, text = "Drafts like this must all be wrong.",
            reply = "My file only covers my own draft. It can't show anything about others." }
    };
    /// <summary>After the saved finding: the next step, then her line until the chapter ends.</summary>
    public string findingNext = "The receptionist said a message came for you.";
    public string afterFindingLine = "The receptionist has a message for you.";
}

/// <summary>T14: the receptionist passing on Mayor Quax's message (Arthur stays the guide; no new actor).</summary>
[Serializable]
public class Level3ReceptionContent
{
    public string speakerName = "Receptionist";
    public string portraitId = "default";
    /// <summary>Her lines, then the player's one acknowledgment (no choice, no Trust).</summary>
    public List<Level3TownLine> quaxMessage = new List<Level3TownLine>
    {
        new Level3TownLine { speaker = "Npc", text = "Mayor Quax left a message for you." },
        new Level3TownLine { speaker = "Npc", text = "\"Thank you for Bea's careful work. Bring me only what the records support.\"" },
        new Level3TownLine { speaker = "Npc", text = "\"If our process lets mistakes through, I want to know where.\"" },
        new Level3TownLine { speaker = "Player", text = "I'll keep to what the records show." }
    };
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
    /// <summary>T14: the receptionist's message (absent in older files: the built-in lines).</summary>
    public Level3ReceptionContent reception = new Level3ReceptionContent();
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
        // T04 buildup: the organizer offer and its two answers.
        if (bea.organizerIntro == null || bea.organizerIntro.Count == 0 || bea.organizerIntro.TrueForAll(l => l == null || l.speaker != "Npc"))
            problems.Add("bea needs organizerIntro lines of her own");
        else
            foreach (Level3TownLine line in bea.organizerIntro)
                if (line == null || string.IsNullOrWhiteSpace(line.text) || (line.speaker != "Npc" && line.speaker != "Player"))
                    problems.Add("every organizerIntro line needs a speaker (Npc/Player) and text");
        if (string.IsNullOrWhiteSpace(bea.helpChoice) || string.IsNullOrWhiteSpace(bea.laterChoice) || string.IsNullOrWhiteSpace(bea.continuePrompt))
            problems.Add("bea needs helpChoice, laterChoice and continuePrompt");
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
        // T10: one short balance question and the lines around the copy handover.
        if (string.IsNullOrWhiteSpace(bea.balanceQuestion) || bea.balanceQuestion.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length > 30)
            problems.Add("bea needs a balanceQuestion of at most 30 words");
        foreach (string text in new[] { bea.copyWaitingLine, bea.balancePrompt, bea.taxDueHint, bea.creditHint, bea.balanceReply,
                                        bea.copyHandoverLine, bea.copyHandoverPrompt, bea.copyRetryPrompt, bea.deliveredLine, bea.afterDeliveryLine })
            if (string.IsNullOrWhiteSpace(text)) { problems.Add("bea needs every copy delivery line (waiting, prompt, two hints, reply, handover, retry, delivered, after)"); break; }
        // T14: her finding question, its three statements and the lines after it.
        if (string.IsNullOrWhiteSpace(bea.findingQuestion) || Words(bea.findingQuestion) > 30)
            problems.Add("bea needs a findingQuestion of at most 30 words");
        foreach (string text in new[] { bea.findingPrompt, bea.findingTitle, bea.findingReview, bea.findingNext, bea.afterFindingLine })
            if (string.IsNullOrWhiteSpace(text)) { problems.Add("bea needs every finding line (prompt, title, review, next, after)"); break; }
        var statements = new List<string>();
        if (bea.findingChoices != null)
            foreach (Level3CreditAnswer choice in bea.findingChoices)
                if (choice != null && !string.IsNullOrWhiteSpace(choice.text) && !string.IsNullOrWhiteSpace(choice.reply) && Words(choice.text) <= 20 && Words(choice.reply) <= 24)
                    statements.Add(choice.id);
        if (statements.Count != Level3ClosingRules.Statements.Length || !Array.TrueForAll(Level3ClosingRules.Statements, statements.Contains))
            problems.Add("bea needs exactly the three findingChoices (supported, intent, pattern), each with a short text and reply");
        problems.AddRange(ValidateReception(file.reception));
        return problems;
    }

    private static int Words(string text) => (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>T14: the receptionist's message (empty = usable): her own short lines and at most one player line.</summary>
    public static List<string> ValidateReception(Level3ReceptionContent reception)
    {
        var problems = new List<string>();
        if (reception == null) { problems.Add("town_life has no reception"); return problems; }
        if (string.IsNullOrWhiteSpace(reception.speakerName)) problems.Add("reception needs a speakerName");
        List<Level3TownLine> lines = reception.quaxMessage;
        if (lines == null || lines.Count == 0 || lines.Count > 5 || !lines.Exists(l => l != null && l.speaker == "Npc"))
            problems.Add("reception needs 1-5 quaxMessage lines of her own");
        else
        {
            foreach (Level3TownLine line in lines)
                if (line == null || string.IsNullOrWhiteSpace(line.text) || (line.speaker != "Npc" && line.speaker != "Player") || Words(line.text) > 20)
                    problems.Add("every quaxMessage line needs a speaker (Npc/Player) and at most 20 words");
            if (lines.FindAll(l => l != null && l.speaker == "Player").Count > 1) problems.Add("the message has at most one player line (no choice)");
        }
        return problems;
    }

    /// <summary>T14: the message as dialogue lines (the receptionist's speaker name and portrait).</summary>
    public static List<DialogueLine> MessageDialogue(Level3ReceptionContent reception)
    {
        var builder = new DialogueBuilder(reception?.speakerName ?? "Receptionist");
        if (reception?.quaxMessage != null)
            foreach (Level3TownLine line in reception.quaxMessage)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
                if (line.speaker == "Player") builder.Player(line.text, "Auditor_Default");
                else builder.Npc(line.text, reception.portraitId);
            }
        return builder.Build();
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
