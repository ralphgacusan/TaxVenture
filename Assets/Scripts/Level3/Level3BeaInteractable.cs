using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Talking to Bea at her worktable (T04), through the existing DialogueUI
/// (the conversation camera frames her through this component; her NpcLife
/// turns her to the player and back). Lines come from town_life.json:
///
/// - before the reception assignment / during calculator practice: one short
///   line (no progress);
/// - BeaRecords: her introduction. When it COMPLETES, the handover is one
///   chapter command (Level3Checkpointer.CommitBeaRecords: saved first); only
///   then do the papers fan out of her envelope and ClientRecordsCollected
///   ticks the objective. Cancelling changes nothing and the next talk starts
///   the introduction again; a failed save says so and keeps her records;
/// - afterwards: the questions menu (CONTENT "Source inquiries"), in any
///   order and repeatable, the one about the record read last first, plus
///   "I'll review the papers first." Talk later closes it. No reward, trust
///   or grade comes from talking.
/// - T08, her computation: on her way to the office (or waiting there before
///   the tax due is on the sheet) the same questions. At the credit step she
///   offers her withholding certificate: "Take the certificate" is one chapter
///   command (Level3Checkpointer.CommitCertificate, saved first); only then
///   does it rise out of her hands to read (Level3CertificatePaper). Her
///   question (CONTENT "Withholding handover") follows: the right answer is
///   saved and fills in nothing; the wrong one opens the Tax Code Book's
///   Credit Versus Expense page on a card and comes back to the same question
///   (no trust, ethics or grade, the sheet untouched); "inspect" holds the
///   certificate up again and returns to the question. A failed save keeps
///   the same choice to try again; Talk later leaves it for the next talk.
/// </summary>
public sealed class Level3BeaInteractable : MonoBehaviour, IInteractable
{
    public const string InquiryAction = "bea-inquiry";
    /// <summary>T08: the awaited actions of the credit step.</summary>
    public const string TakeAction = "bea-certificate-take";
    public const string CreditAction = "bea-credit-question";
    public const string HelpAction = "bea-credit-help";
    /// <summary>T08: the Tax Code Book page her wrong answer opens (Levels/Level_03/book.json).</summary>
    public const string HelpSectionPrefix = "Section 9";
    private const string PlayerPortrait = "Auditor_Default";

    private Level3BeaTable table;
    private HighlightEffect highlight;
    private DialogueUI dialogue;
    private RectTransform choicePanel;
    private float promptReadyAt;
    private bool talking;
    /// <summary>T08: lines during which the certificate is held up (the first one rises out of her hands).</summary>
    private readonly HashSet<DialogueLine> paperLines = new HashSet<DialogueLine>();
    private DialogueLine handOverLine;

    public bool Talking => talking;

    public void Init(Level3BeaTable owner)
    {
        table = owner;
        highlight = HighlightEffect.For((Component)this);
    }

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText() => "Talk to " + (table != null && table.Content != null ? table.Content.speakerName : "Bea");

    private void OnDisable()
    {
        // Removal or a scene change cancels her conversation, never a handover that already saved.
        if (DialogueUI.Active != null) DialogueUI.Active.CancelDialogueFor(this);
        ClearChoices();
        Unhook();
        talking = false;
        if (Level3CertificatePaper.Instance != null && Level3CertificatePaper.Instance.Showing == Level3CertificatePaper.Context.Conversation)
            Level3CertificatePaper.Instance.Gather();
    }

    public void OnInteract()
    {
        if (talking || table == null || table.Content == null) return;
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        if (chapter == null) return;
        Level3ClientContent bea = table.Content;
        Level3Phase phase = Level3ChapterRules.PhaseOf(chapter);
        if (phase == Level3Phase.Reception) Talk(Npc(bea.beforeAssignment), null);
        else if (phase == Level3Phase.ToolPractice) Talk(Npc(bea.duringPractice), null);
        else if (phase == Level3Phase.BeaRecords) Talk(Introduction(bea), HandOver);
        else if (phase == Level3Phase.BeaComputation) OfficeTalk(chapter, bea);
        else StartInquiry();
    }

    /// <summary>T08: during her computation (on her way, waiting, the certificate and its question).</summary>
    private void OfficeTalk(Level3ChapterCheckpoint chapter, Level3ClientContent bea)
    {
        Level3BeaOfficeVisit visit = table.Visit;
        bool walking = visit != null && visit.OnTheWay;
        bool settled = visit == null || visit.InRoom || !visit.enabled;
        if (!walking && settled && Level3CertificateRules.HandOverDue(chapter)) StartHandover(bea);
        else if (!walking && settled && Level3CertificateRules.QuestionDue(chapter)) StartQuestion(bea);
        else if (Level3CertificateRules.Acquired(chapter)) StartInquiry();
        else StartInquiry(settled ? bea.officeWaitPrompt : bea.onTheWayPrompt);
    }

    // =========================================================
    // CONVERSATIONS
    // =========================================================

    private List<DialogueLine> Npc(List<string> texts)
    {
        var builder = new DialogueBuilder(table.Content.speakerName);
        if (texts != null)
            foreach (string text in texts)
                if (!string.IsNullOrWhiteSpace(text)) builder.Npc(text, table.Content.portraitId);
        return builder.Build();
    }

    private List<DialogueLine> Introduction(Level3ClientContent bea) => Scripted(bea, bea.introduction);

    private List<DialogueLine> Scripted(Level3ClientContent bea, List<Level3TownLine> script)
    {
        var definitions = new List<DialogueLineDefinition>();
        if (script == null) return CaseFactory.BuildDialogue(definitions, bea.speakerName);
        foreach (Level3TownLine line in script)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
            bool player = line.speaker == "Player";
            definitions.Add(new DialogueLineDefinition
            {
                speaker = player ? "Player" : "Npc",
                text = line.text,
                portraitId = player ? PlayerPortrait : bea.portraitId,
                emote = line.emote
            });
        }
        return CaseFactory.BuildDialogue(definitions, bea.speakerName);
    }

    private bool Talk(List<DialogueLine> lines, Action completed)
    {
        if (lines == null || lines.Count == 0) return false;
        dialogue = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (dialogue == null) return false;
        bool admitted = dialogue.TryStartDialogue(lines,
            () => { End(); completed?.Invoke(); },
            End,
            this);
        if (!admitted) return false;
        AudioManager.Instance?.QueueInteractionSFX("npc");
        talking = true;
        return true;
    }

    private void End()
    {
        talking = false;
        ClearChoices();
        Unhook();
        paperLines.Clear();
        handOverLine = null;
        // The certificate never stays up after her conversation (it lowers back into the player's hands).
        Level3CertificatePaper paper = Level3CertificatePaper.Instance;
        if (paper != null && paper.Showing == Level3CertificatePaper.Context.Conversation) paper.Lower();
    }

    /// <summary>The introduction completed: the single domain commit of the encounter.</summary>
    private void HandOver()
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        Level3Checkpointer.TapeCommit outcome = owner.CommitBeaRecords(out string hint);
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                Debug.Log("[Level3] Bea handed over her records (saved). Chapter: WorksheetDraft.");
                table.SyncPapers(animate: true);
                Level3BeaTable.Level3Say(table.Content.speakerName + "'s records are on her table and in your Case Folder. Sit at the table to read them.");
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                table.SyncPapers(animate: false);
                break;
            default:
                if (!string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint);
                break;
        }
    }

    // =========================================================
    // QUESTIONS (CONTENT "Source inquiries")
    // =========================================================

    private void StartInquiry(string prompt = null)
    {
        string opening = string.IsNullOrWhiteSpace(prompt) ? table.Content.inquiryPrompt : prompt;
        List<DialogueLine> lines = AwaitQuestion(new DialogueBuilder(table.Content.speakerName).Npc(opening, table.Content.portraitId).Build());
        if (!Talk(lines, null)) return;
        Hook();
        if (dialogue.IsAwaitingAction && !dialogue.IsLineTyping) ShowChoices();
    }

    private void Hook()
    {
        dialogue.LineRevealed += OnLineRevealed;
        dialogue.LineChanged += OnLineChanged;
    }

    /// <summary>The last line waits for a question (the choice card), not a tap.</summary>
    private static List<DialogueLine> AwaitQuestion(List<DialogueLine> lines)
    {
        if (lines != null && lines.Count > 0)
        {
            lines[lines.Count - 1].AwaitAction = InquiryAction;
            lines[lines.Count - 1].ActionPrompt = "Choose a question";
        }
        return lines;
    }

    private void Unhook()
    {
        if (dialogue == null) return;
        dialogue.LineRevealed -= OnLineRevealed;
        dialogue.LineChanged -= OnLineChanged;
    }

    private void OnLineRevealed(DialogueLine line)
    {
        if (line == null || dialogue == null || DialogueUI.Active != dialogue) return;
        switch (line.AwaitAction)
        {
            case InquiryAction: ShowChoices(); break;
            case TakeAction: ShowTake(); break;
            case CreditAction: ShowAnswers(); break;
            case HelpAction: ShowHelp(); break;
            default: ClearChoices(); break;
        }
    }

    /// <summary>T08: the certificate is up exactly while one of its lines shows (the paper takes no touches in a conversation).</summary>
    private void OnLineChanged(DialogueLine line)
    {
        if (dialogue == null || DialogueUI.Active != dialogue) return;
        Level3CertificatePaper paper = Level3CertificatePaper.Instance;
        if (paper == null) return;
        if (line != null && paperLines.Contains(line))
        {
            // The first time it comes out of her hands, held toward the player; afterwards from the player's own hands.
            Vector3? from = line == handOverLine ? transform.position + Vector3.up * 1.05f + transform.forward * 0.32f : (Vector3?)null;
            if (paper.Raise(Level3CertificatePaper.Context.Conversation, from) && from.HasValue) AudioManager.Instance?.PlayDocumentSFX(false);
        }
        else if (paper.Showing == Level3CertificatePaper.Context.Conversation) paper.Lower();
    }

    // =========================================================
    // T08: THE WITHHOLDING CERTIFICATE AND HER QUESTION
    // =========================================================

    private void StartHandover(Level3ClientContent bea)
    {
        List<DialogueLine> lines = Scripted(bea, bea.certificateHandover);
        if (lines.Count == 0) return;
        lines[lines.Count - 1].AwaitAction = TakeAction;
        lines[lines.Count - 1].ActionPrompt = "Take the certificate";
        if (!Talk(lines, null)) return;
        Hook();
        if (dialogue.IsAwaitingAction && !dialogue.IsLineTyping) ShowTake();
    }

    private void StartQuestion(Level3ClientContent bea)
    {
        if (!Talk(AskLines(bea, null), null)) return;
        Hook();
        if (dialogue.IsAwaitingAction && !dialogue.IsLineTyping) ShowAnswers();
    }

    /// <summary>[an optional line with the certificate up], then her question waiting for an answer.</summary>
    private List<DialogueLine> AskLines(Level3ClientContent bea, string readText)
    {
        var builder = new DialogueBuilder(bea.speakerName);
        if (!string.IsNullOrWhiteSpace(readText)) builder.Npc(readText, bea.portraitId);
        builder.Npc(bea.creditQuestion, bea.portraitId).AwaitAction(CreditAction, "Answer " + bea.speakerName);
        List<DialogueLine> lines = builder.Build();
        if (lines.Count > 1) paperLines.Add(lines[0]);
        return lines;
    }

    private void ShowTake()
    {
        ShowPanel(new List<string> { "Take the certificate" }, new List<Action> { TakeCertificate },
            table.Content.speakerName.ToUpperInvariant() + "'S CERTIFICATE");
    }

    /// <summary>The deliberate handover: saved first; only then does the paper rise out of her hands.</summary>
    private void TakeCertificate()
    {
        ClearChoices();
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null || dialogue == null) return;
        Level3Checkpointer.TapeCommit outcome = owner.CommitCertificate(out string hint);
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                Debug.Log("[Level3] Bea handed over her withholding certificate (saved).");
                List<DialogueLine> lines = AskLines(table.Content, table.Content.certificateReadLine);
                handOverLine = lines[0];
                if (dialogue.ContinueWith(TakeAction, lines) < 0) dialogue.CancelDialogueFor(this);
                break;
            case Level3Checkpointer.TapeCommit.SaveFailed:
                // Nothing was handed over: she still holds it, and the same choice waits.
                Level3BeaTable.Level3Say(hint);
                ShowTake();
                break;
            default:
                if (!string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint);
                dialogue.CancelDialogueFor(this);
                break;
        }
    }

    private void ShowAnswers()
    {
        var labels = new List<string>();
        var actions = new List<Action>();
        foreach (string id in Level3CertificateRules.Answers)
        {
            Level3CreditAnswer answer = Level3TownContent.CreditAnswer(table.Content, id);
            if (answer == null) continue;
            string chosen = id;
            labels.Add(answer.text);
            actions.Add(() => Answer(chosen));
        }
        ShowPanel(labels, actions, "ANSWER " + table.Content.speakerName.ToUpperInvariant());
    }

    private void Answer(string id)
    {
        ClearChoices();
        Level3ClientContent bea = table.Content;
        Level3CreditAnswer answer = Level3TownContent.CreditAnswer(bea, id);
        if (answer == null || dialogue == null) return;
        var builder = new DialogueBuilder(bea.speakerName).Player(answer.text, PlayerPortrait);
        switch (Level3CertificateRules.Answer(id))
        {
            case Level3CertificateRules.AnswerKind.Explains:
            {
                Level3Checkpointer owner = Level3Checkpointer.Active;
                string hint = Level3CertificateRules.RefuseNotHere;
                Level3Checkpointer.TapeCommit outcome = owner != null ? owner.CommitCreditExplanation(out hint) : Level3Checkpointer.TapeCommit.Refused;
                if (outcome == Level3Checkpointer.TapeCommit.SaveFailed)
                {
                    // Nothing saved: the same question and answers wait.
                    Level3BeaTable.Level3Say(hint);
                    ShowAnswers();
                    return;
                }
                if (outcome == Level3Checkpointer.TapeCommit.Refused)
                {
                    if (!string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint);
                    dialogue.CancelDialogueFor(this);
                    return;
                }
                // Saved: she understands; nothing is filled in on the sheet (the player links the credit there).
                Debug.Log("[Level3] Bea's certificate explained as a credit (saved). The sheet is unchanged.");
                builder.Npc(answer.reply, bea.portraitId);
                if (dialogue.ContinueWith(CreditAction, builder.Build()) < 0) dialogue.CancelDialogueFor(this);
                return;
            }
            case Level3CertificateRules.AnswerKind.NeedsHelp:
                // Not saved, no trust or ethics, the sheet untouched: the book's help, then the same question.
                builder.Npc(answer.reply, bea.portraitId).AwaitAction(HelpAction, "Read the Tax Code Book's help");
                if (dialogue.ContinueWith(CreditAction, builder.Build()) < 0) dialogue.CancelDialogueFor(this);
                return;
            case Level3CertificateRules.AnswerKind.Inspect:
            {
                List<DialogueLine> lines = builder.Build();
                lines.AddRange(AskLines(bea, answer.reply));
                if (dialogue.ContinueWith(CreditAction, lines) < 0) dialogue.CancelDialogueFor(this);
                return;
            }
        }
    }

    /// <summary>The Tax Code Book's Credit Versus Expense page on the docked card (the same leaf the book shows), then back to her question.</summary>
    private void ShowHelp()
    {
        ClearChoices();
        if (dialogue == null) return;
        Canvas canvas = dialogue.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        TaxCodeSection section = LevelBookLeaves.Load(Level3ChapterRules.LevelId).Find(s => s != null && s.heading != null && s.heading.StartsWith(HelpSectionPrefix, StringComparison.Ordinal));
        string heading = section != null ? section.heading : HelpSectionPrefix + " – Credit Versus Expense";
        string body = section != null ? section.body : "Your Tax Code Book explains credits and expenses in " + HelpSectionPrefix + ".";

        const float width = 700f, bodyHeight = 500f;
        RectTransform panelBody = HudTheme.CreatePanel("Bea credit help", canvas.rootCanvas.transform, 18f);
        choicePanel = panelBody.parent as RectTransform;
        choicePanel.anchorMin = choicePanel.anchorMax = new Vector2(0.5f, 0.5f);
        choicePanel.pivot = new Vector2(0.5f, 0.5f);
        choicePanel.sizeDelta = new Vector2(width, 70f + bodyHeight + 130f);
        choicePanel.SetAsLastSibling();
        promptReadyAt = Time.unscaledTime + 0.35f;

        TextMeshProUGUI title = HudTheme.CreateLabel("Heading", panelBody, "TAX CODE BOOK · " + heading.ToUpperInvariant(), 22f, HudTheme.Brass,
            TextAlignmentOptions.TopLeft, FontStyles.Bold);
        HudTheme.PlaceTopLeft(title.rectTransform, 26f, 16f, width - 52f, 44f);
        title.textWrappingMode = TextWrappingModes.Normal;
        title.enableAutoSizing = true;
        title.fontSizeMin = 16f;
        title.fontSizeMax = 22f;
        TextMeshProUGUI text = HudTheme.CreateLabel("Body", panelBody, body, 30f, HudTheme.TextPrimary, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        HudTheme.PlaceTopLeft(text.rectTransform, 26f, 70f, width - 52f, bodyHeight);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = 22f;
        text.fontSizeMax = 30f;
        TextMeshProUGUI where = HudTheme.CreateLabel("Where", panelBody, "It is also in your Tax Code Book, " + heading + ".", 20f, HudTheme.TextPrimary,
            TextAlignmentOptions.Left, FontStyles.Italic);
        HudTheme.PlaceTopLeft(where.rectTransform, 26f, 70f + bodyHeight, width - 52f, 32f);

        RectTransform back = HudTheme.CreateRect("Back", panelBody);
        HudTheme.PlaceTopLeft(back, 26f, 70f + bodyHeight + 40f, width - 52f, 78f);
        Image face = HudTheme.AddImage(back, HudTheme.Rounded, new Color32(34, 48, 61, 255), 12f);
        face.raycastTarget = true;
        Button button = back.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.onClick.AddListener(() =>
        {
            if (Time.unscaledTime < promptReadyAt || choicePanel == null) return;
            AudioManager.Instance?.QueueInteractionSFX("continue");
            BackToQuestion();
        });
        TextMeshProUGUI label = HudTheme.CreateLabel("Back text", back, table.Content.helpBack, 30f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 12f);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(back.gameObject);
        AudioManager.Instance?.PlayEffect("sfx_book_open_01");
        dialogue.DockChoices(choicePanel);
    }

    /// <summary>The same question again (the same conversation, the same certificate and sheet).</summary>
    private void BackToQuestion()
    {
        ClearChoices();
        AudioManager.Instance?.PlayEffect("sfx_book_close_01");
        if (dialogue == null || dialogue.ContinueWith(HelpAction, AskLines(table.Content, null)) < 0) dialogue?.CancelDialogueFor(this);
    }

    private void ShowChoices()
    {
        List<Level3Inquiry> ordered = Level3TownContent.Ordered(table.Content, table.LastReadSource);
        var labels = new List<string>();
        var actions = new List<Action>();
        foreach (Level3Inquiry inquiry in ordered)
        {
            Level3Inquiry asked = inquiry;
            labels.Add(inquiry.question);
            actions.Add(() => Ask(asked));
        }
        labels.Add(table.Content.leaveChoice);
        actions.Add(Leave);
        ShowPanel(labels, actions);
    }

    private void Ask(Level3Inquiry inquiry)
    {
        ClearChoices();
        List<DialogueLine> lines = AwaitQuestion(new DialogueBuilder(table.Content.speakerName)
            .Player(inquiry.question, PlayerPortrait)
            .Npc(inquiry.answer, table.Content.portraitId)
            .Npc(table.Content.inquiryAgain, table.Content.portraitId)
            .Build());
        if (dialogue == null || dialogue.ContinueWith(InquiryAction, lines) < 0) ShowChoices();
    }

    private void Leave()
    {
        ClearChoices();
        List<DialogueLine> lines = new DialogueBuilder(table.Content.speakerName)
            .Player(table.Content.leaveChoice, PlayerPortrait)
            .Npc(table.Content.leaveAnswer, table.Content.portraitId)
            .Build();
        if (dialogue == null || dialogue.ContinueWith(InquiryAction, lines) < 0) dialogue?.CancelDialogueFor(this);
    }

    /// <summary>The same docked choice card as Rosa's (Level2RosaConsultation): one button per question (or answer), Talk later.</summary>
    private void ShowPanel(List<string> labels, List<Action> actions, string title = null)
    {
        ClearChoices();
        if (dialogue == null) return;
        Canvas canvas = dialogue.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        const float rowHeight = 78f, gap = 10f, top = 70f;
        float height = top + labels.Count * (rowHeight + gap) + 20f;
        RectTransform body = HudTheme.CreatePanel("Bea questions", canvas.rootCanvas.transform, 18f);
        choicePanel = body.parent as RectTransform;
        choicePanel.anchorMin = choicePanel.anchorMax = new Vector2(0.5f, 0.5f);
        choicePanel.pivot = new Vector2(0.5f, 0.5f);
        choicePanel.sizeDelta = new Vector2(980f, height);
        choicePanel.SetAsLastSibling();
        promptReadyAt = Time.unscaledTime + 0.2f;

        RectTransform heading = HudTheme.CreateRect("Heading", body);
        HudTheme.PlaceTopLeft(heading, 30f, 16f, 700f, 40f);
        TextMeshProUGUI headingText = heading.gameObject.AddComponent<TextMeshProUGUI>();
        headingText.font = HudTheme.Font;
        headingText.text = title ?? "ASK " + table.Content.speakerName.ToUpperInvariant();
        headingText.fontSize = 24f;
        headingText.fontStyle = FontStyles.Bold;
        headingText.color = HudTheme.Brass;
        headingText.raycastTarget = false;

        RectTransform later = HudTheme.CreateRect("Talk later", body);
        HudTheme.PlaceTopLeft(later, 756f, 14f, 160f, 40f);
        Image laterFace = HudTheme.AddImage(later, HudTheme.Rounded, HudTheme.Track, 8f);
        laterFace.raycastTarget = true;
        Button laterButton = later.gameObject.AddComponent<Button>();
        laterButton.targetGraphic = laterFace;
        laterButton.onClick.AddListener(() =>
        {
            if (Time.unscaledTime < promptReadyAt || dialogue == null) return;
            ClearChoices();
            dialogue.CancelDialogueFor(this);
        });
        TextMeshProUGUI laterText = HudTheme.CreateLabel("Talk later text", later, "Talk later", 20f, HudTheme.TextPrimary,
            TextAlignmentOptions.Center, FontStyles.Normal);
        HudTheme.Stretch(laterText.rectTransform, 3f);

        Button first = null;
        for (int i = 0; i < labels.Count; i++)
        {
            int index = i;
            RectTransform rect = HudTheme.CreateRect("Question", body);
            HudTheme.PlaceTopLeft(rect, 30f, top + i * (rowHeight + gap), 916f, rowHeight);
            Image face = HudTheme.AddImage(rect, HudTheme.Rounded, new Color32(34, 48, 61, 255), 12f);
            face.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(() =>
            {
                if (Time.unscaledTime < promptReadyAt || choicePanel == null) return;
                AudioManager.Instance?.QueueInteractionSFX("continue");
                actions[index]?.Invoke();
            });
            TextMeshProUGUI label = HudTheme.CreateLabel("Question text", rect, labels[i], 30f, HudTheme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Normal);
            HudTheme.Stretch(label.rectTransform, 16f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = 22f;
            label.fontSizeMax = 30f;
            if (first == null) first = button;
        }
        if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        dialogue.DockChoices(choicePanel);
    }

    private void ClearChoices()
    {
        if (choicePanel != null)
        {
            if (dialogue != null && dialogue.isActiveAndEnabled) dialogue.ReleaseChoices(choicePanel);
            else Destroy(choicePanel.gameObject);
        }
        choicePanel = null;
    }
}
