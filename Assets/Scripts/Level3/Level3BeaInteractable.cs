using System;
using System.Collections;
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
/// - BeaRecords: her introduction (studio goal, her records, the envelope
///   line). When it COMPLETES, the handover is one chapter command
///   (Level3Checkpointer.CommitBeaRecords: saved first); only then does she
///   slide her closed envelope into reach (Level3BeaTable.Present) and
///   ClientRecordsCollected ticks the objective. Cancelling changes nothing
///   and the next talk starts the introduction again; a failed save says so,
///   keeps her records and the next talk retries with the envelope line only;
/// - T04 buildup: once her featured record has been read and put back she
///   introduces her organizer (OfferHelp; the table starts it, or the next
///   talk does). Its last line waits for Help Bea (the conversation ends,
///   then the existing seat path) or Later (a short reply; nothing saved, no
///   penalty);
/// - afterwards: the questions menu (CONTENT "Source inquiries"), in any
///   order and repeatable, the one about the record read last first, plus
///   "I'll review the papers first." Talk later closes it. Once her offer was
///   made (or on a restore) it opens with her continuation line and Help Bea
///   first while she is at her table. No reward, trust or grade comes from
///   talking.
/// - T08, her computation: once tax due is attached, she presents a paper beside
///   her at a revealed dialogue line, then finishes talking. The existing safe
///   world pickup collects it afterward, saving B-WHT and its readable Bag item
///   together before the paper flies to the HUD. No modal Take action.
///   The earlier question/answers code stays for reference only (a saved
///   creditExplained is kept as it was and is no longer required).
/// - T10, her delivery (BeaDelivery, the same conference-room spot): while the
///   copy is with the Auditor she waits. Once it is carried she asks ONE
///   balance question; her approved copy's tax and payment lines are docked
///   beside the dialogue and the player points to one (dialogue taps never
///   answer it). Tax due / Tax credit: one contextual sentence, the same
///   question waits, nothing saved. Remaining payable: saved first
///   (Level3Checkpointer.CommitBalanceExplanation), her short reply, then the
///   handover line waits for the actual copy (ClientCopyHandover.ILevelDelivery:
///   drag the folder onto her or tap its icon; any other drop returns it).
///   CommitDelivery saves the copy, her closing step and the once-only +4
///   follow-through before the icon empties, her thanks, emote and toast.
///   Talk later, Back or a failed save keep everything committed so far.
/// - T14, her closing (ClosingFinding): one question about her own draft;
///   three statements are docked beside it (her docked choice card). Only the
///   one her file supports is saved (Level3Checkpointer.CommitFinding) before
///   her reply and the pointer to the receptionist; intent or a wider pattern
///   gets her one-line reply and the same question waits (nothing saved, no
///   penalty). Review her records ends the talk and opens her papers in the
///   existing Case Folder reader; the question waits. Afterwards one short line.
/// </summary>
public sealed class Level3BeaInteractable : MonoBehaviour, IInteractable, ClientCopyHandover.ILevelDelivery
{
    public const string InquiryAction = "bea-inquiry";
    /// <summary>T04 buildup: her organizer offer waits for Help Bea / Later.</summary>
    public const string OfferAction = "bea-help-offer";
    /// <summary>T08: the awaited actions of the credit step.</summary>
    public const string TakeAction = "bea-certificate-take";
    public const string CreditAction = "bea-credit-question";
    public const string HelpAction = "bea-credit-help";
    /// <summary>T10: her balance question waits for a figure on the copy.</summary>
    public const string BalanceAction = "bea-balance-question";
    /// <summary>T14: her draft question waits for a statement about her file.</summary>
    public const string FindingAction = "bea-finding-question";
    private const string RecordsFallback = "Her draft and receipts are in your Case Folder in the Bag.";
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
    private int certificateDropIndex = -1;
    /// <summary>T04 buildup: this conversation's questions card offers Help Bea first.</summary>
    private bool offerHelp;
    /// <summary>T04 buildup: Help Bea was chosen; the seat follows once the conversation has ended.</summary>
    private bool helpChosen;
    /// <summary>The conversation's shot when Help Bea was chosen: the move to the table starts from here.</summary>
    private Pose helpView;
    /// <summary>T10: a delivery commit is running (a second drop/tap in the same frame is ignored).</summary>
    private bool delivering;
    /// <summary>T14: Review her records was chosen; her papers open once the conversation has ended.</summary>
    private bool reviewAfter;

    public bool Talking => talking;

    public void Init(Level3BeaTable owner)
    {
        table = owner;
        highlight = HighlightEffect.For((Component)this);
    }

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText() => "Talk to " + (table != null && table.Content != null ? table.Content.speakerName : "Bea");

    private void OnEnable() => ClientCopyHandover.LevelDelivery = this;

    private void OnDisable()
    {
        if (ReferenceEquals(ClientCopyHandover.LevelDelivery, this)) ClientCopyHandover.LevelDelivery = null;
        // Removal or a scene change cancels her conversation, never a handover that already saved.
        if (DialogueUI.Active != null) DialogueUI.Active.CancelDialogueFor(this);
        ClearChoices();
        Unhook();
        talking = false;
        reviewAfter = false;
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
        else if (phase == Level3Phase.BeaRecords) Talk(table.IntroHeard ? EnvelopeLine(bea) : Introduction(bea), HandOver);
        else if (phase == Level3Phase.BeaComputation) OfficeTalk(chapter, bea);
        else if (phase == Level3Phase.BeaDelivery || Level3DeliveryRules.Delivered(chapter)) DeliveryTalk(chapter, bea);
        else if (table.Step == Level3TableStaging.EncounterStep.Inspected) OfferHelp();
        else StartInquiry(null, table.CanOfferHelp);
    }

    /// <summary>T08: during her computation (on her way, waiting, the certificate and its question).</summary>
    private void OfficeTalk(Level3ChapterCheckpoint chapter, Level3ClientContent bea)
    {
        Level3BeaOfficeVisit visit = table.Visit;
        bool walking = visit != null && visit.OnTheWay;
        bool settled = visit == null || visit.InRoom || !visit.enabled;
        if (!walking && settled && Level3CertificateRules.HandOverDue(chapter)) StartHandover(bea);
        // Revised T08: no follow-up question (the help is contextual on the folder page).
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

    /// <summary>A retry after a failed handover save: only the introduction's last line (she presents her envelope).</summary>
    private List<DialogueLine> EnvelopeLine(Level3ClientContent bea) =>
        bea.introduction == null || bea.introduction.Count == 0 ? Introduction(bea)
            : Scripted(bea, bea.introduction.GetRange(bea.introduction.Count - 1, 1));

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
        helpChosen = false;
        return true;
    }

    private void End()
    {
        talking = false;
        offerHelp = false;
        ClearChoices();
        Unhook();
        paperLines.Clear();
        handOverLine = null;
        certificateDropIndex = -1;
        // The certificate never stays up after her conversation (it lowers back into the player's hands).
        Level3CertificatePaper paper = Level3CertificatePaper.Instance;
        if (paper != null && paper.Showing == Level3CertificatePaper.Context.Conversation) paper.Lower();
        if (reviewAfter)
        {
            reviewAfter = false;
            if (isActiveAndEnabled) StartCoroutine(OpenRecordsWhenFree());
        }
    }

    /// <summary>
    /// The introduction completed: the single domain commit of the encounter. Only a saved handover lets her
    /// present the envelope (no Arthur line over hers: the envelope and its prompt are the next step).
    /// </summary>
    private void HandOver()
    {
        table.HeardIntroduction();
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        Level3Checkpointer.TapeCommit outcome = owner.CommitBeaRecords(out string hint);
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                Debug.Log("[Level3] Bea handed over her records (saved). Chapter: WorksheetDraft.");
                table.Present();
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                table.SyncPapers();
                break;
            default:
                if (!string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint);
                break;
        }
    }

    // =========================================================
    // T04 BUILDUP: HER ORGANIZER AND HELP BEA / LATER
    // =========================================================

    /// <summary>
    /// After her featured record was read and put back: a short line about her organizer, waiting for
    /// Help Bea / Later. Nothing is saved; T05's machine is not working yet. False when it cannot start now.
    /// </summary>
    public bool OfferHelp()
    {
        if (talking || table == null || table.Content == null || table.Step != Level3TableStaging.EncounterStep.Inspected) return false;
        List<DialogueLine> lines = Scripted(table.Content, table.Content.organizerIntro);
        if (lines.Count == 0) return false;
        lines[lines.Count - 1].AwaitAction = OfferAction;
        lines[lines.Count - 1].ActionPrompt = table.Content.helpChoice + "?";
        if (!Talk(lines, Concluded)) return false;
        Hook();
        if (dialogue.IsAwaitingAction && !dialogue.IsLineTyping) ShowOffer();
        return true;
    }

    private void ShowOffer()
    {
        table.OfferShown();
        ShowPanel(new List<string> { table.Content.helpChoice, table.Content.laterChoice },
            new List<Action> { () => ChooseHelp(OfferAction), Later },
            table.Content.speakerName.ToUpperInvariant() + "'S RECORDS", talkLater: false);
    }

    /// <summary>Help Bea: the conversation ends first (controls released); Concluded takes the seat in the same frame, from this shot.</summary>
    private void ChooseHelp(string awaited)
    {
        ClearChoices();
        helpChosen = true;
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : null;
        helpView = view != null ? new Pose(view.transform.position, view.transform.rotation) : default;
        if (dialogue == null || dialogue.ContinueWith(awaited, null) == -2)
        {
            helpChosen = false;
            dialogue?.CancelDialogueFor(this);
        }
    }

    /// <summary>Later: a short reply, nothing saved, no penalty. Her table and her questions wait.</summary>
    private void Later()
    {
        ClearChoices();
        List<DialogueLine> lines = new DialogueBuilder(table.Content.speakerName).Npc(table.Content.leaveAnswer, table.Content.portraitId).Build();
        if (dialogue == null || dialogue.ContinueWith(OfferAction, lines) < 0) dialogue?.CancelDialogueFor(this);
    }

    /// <summary>A conversation completed: if Help Bea was chosen, the same seat path as the table's tap (one camera move, one splash).</summary>
    private void Concluded()
    {
        if (!helpChosen) return;
        helpChosen = false;
        table.AcceptHelp(helpView != default ? helpView : (Pose?)null);
    }

    // =========================================================
    // QUESTIONS (CONTENT "Source inquiries")
    // =========================================================

    /// <summary>Her questions. help: T04 buildup's concise continuation (her continuation line, Help Bea first).</summary>
    private void StartInquiry(string prompt = null, bool help = false)
    {
        string opening = !string.IsNullOrWhiteSpace(prompt) ? prompt : help ? table.Content.continuePrompt : table.Content.inquiryPrompt;
        List<DialogueLine> lines = AwaitQuestion(new DialogueBuilder(table.Content.speakerName).Npc(opening, table.Content.portraitId).Build());
        if (!Talk(lines, Concluded)) return;
        offerHelp = help;
        Hook();
        if (dialogue.IsAwaitingAction && !dialogue.IsLineTyping) ShowChoices();
    }

    private void Hook()
    {
        // Never twice for one conversation (a repeated callback would open a second choice card or sound).
        Unhook();
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
        PresentCertificateIfDue();
        if (line == null || dialogue == null || DialogueUI.Active != dialogue) return;
        switch (line.AwaitAction)
        {
            case InquiryAction: ShowChoices(); break;
            case OfferAction: ShowOffer(); break;
            case CreditAction: ShowAnswers(); break;
            case HelpAction: ShowHelp(); break;
            case BalanceAction: ShowCopyFigures(); break;
            case FindingAction: ShowFindingChoices(); break;
            default: ClearChoices(); break;
        }
    }

    /// <summary>T08: the certificate is up exactly while one of its lines shows (the paper takes no touches in a conversation).</summary>
    private void OnLineChanged(DialogueLine line)
    {
        if (dialogue == null || DialogueUI.Active != dialogue) return;
        PresentCertificateIfDue();
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

    // DialogueSession snapshots authored lines. Like the Auditor, use the admitted line index.
    public static bool CertificateLineReached(int current, int handover) => handover >= 0 && current > handover;
    private void PresentCertificateIfDue()
    {
        if (dialogue == null || DialogueUI.Active != dialogue || !CertificateLineReached(dialogue.CurrentLineIndex, certificateDropIndex)) return;
        // Consume before calling out: line-change/reveal callbacks can both run for this line.
        certificateDropIndex = -1;
        Level3CertificatePaper.Ensure(transform).PresentFrom(transform);
    }

    private void StartHandover(Level3ClientContent bea)
    {
        List<DialogueLine> lines = Scripted(bea, bea.certificateHandover);
        if (lines.Count == 0) return;
        // Let the player hear and advance past one complete line before the handover starts.
        lines.Insert(0, new DialogueBuilder(table.Content.speakerName)
            .Npc("Thanks for helping me work through the figures.", table.Content.portraitId).Build()[0]);
        certificateDropIndex = lines.Count - 1;
        lines.Add(new DialogueBuilder(table.Content.speakerName).Npc(bea.certificateReadLine, table.Content.portraitId).Build()[0]);
        if (!Talk(lines, null)) { certificateDropIndex = -1; return; }
        Hook();
        PresentCertificateIfDue();
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

    // =========================================================
    // T10: HER BALANCE QUESTION AND THE COPY HANDOVER
    // =========================================================

    private static string Money(string amount) => string.IsNullOrEmpty(amount) ? "" : "\u20B1" + Level3Calculator.Format(amount);

    /// <summary>Her delivery talk by saved stage (copy with the Auditor / question / handover / delivered).</summary>
    private void DeliveryTalk(Level3ChapterCheckpoint chapter, Level3ClientContent bea)
    {
        Level3BeaOfficeVisit visit = table.Visit;
        if (visit != null && visit.OnTheWay) { StartInquiry(bea.onTheWayPrompt); return; }
        switch (Level3DeliveryRules.StageOf(chapter))
        {
            case Level3DeliveryRules.Stage.CopyAtAuditor: Talk(Npc(new List<string> { bea.copyWaitingLine }), null); break;
            case Level3DeliveryRules.Stage.Explain: StartBalanceQuestion(bea); break;
            case Level3DeliveryRules.Stage.HandOver: StartCopyHandover(bea); break;
            case Level3DeliveryRules.Stage.Delivered: ClosingTalk(chapter, bea); break;
            default: StartInquiry(); break;
        }
    }

    private DialogueLine QuestionLine(Level3ClientContent bea, string text) =>
        new DialogueBuilder(bea.speakerName).Npc(text, bea.portraitId).AwaitAction(BalanceAction, bea.balancePrompt).Build()[0];

    private DialogueLine HandoverLine(Level3ClientContent bea) =>
        new DialogueBuilder(bea.speakerName).Npc(bea.copyHandoverLine, bea.portraitId).AwaitAction(ClientCopyRules.HandoverAction, bea.copyHandoverPrompt).Build()[0];

    private void StartBalanceQuestion(Level3ClientContent bea)
    {
        OfficeHudRuntime.Instance?.CloseCaseFolderView();
        if (!Talk(new List<DialogueLine> { QuestionLine(bea, bea.balanceQuestion) }, null)) return;
        Hook();
        if (dialogue.IsAwaitingAction && !dialogue.IsLineTyping) ShowCopyFigures();
    }

    /// <summary>A resume after the saved explanation: straight to the handover line.</summary>
    private void StartCopyHandover(Level3ClientContent bea)
    {
        OfficeHudRuntime.Instance?.CloseCaseFolderView();
        if (Talk(new List<DialogueLine> { HandoverLine(bea) }, null)) Hook();
    }

    /// <summary>
    /// The approved copy's tax and payment lines (its frozen figures), docked beside the dialogue:
    /// one large row per figure to point to, and Talk later. No figure is marked right or wrong here.
    /// </summary>
    private void ShowCopyFigures()
    {
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        if (Level3DeliveryRules.StageOf(chapter) != Level3DeliveryRules.Stage.Explain) { ClearChoices(); return; }
        ClearChoices();
        if (dialogue == null) return;
        Canvas canvas = dialogue.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        const float width = 980f, rowHeight = 92f, gap = 12f, top = 72f;
        string[] evidence = Level3DeliveryRules.Evidence;
        RectTransform body = HudTheme.CreatePanel("Bea copy figures", canvas.rootCanvas.transform, 18f);
        choicePanel = body.parent as RectTransform;
        choicePanel.anchorMin = choicePanel.anchorMax = new Vector2(0.5f, 0.5f);
        choicePanel.pivot = new Vector2(0.5f, 0.5f);
        choicePanel.sizeDelta = new Vector2(width, top + evidence.Length * (rowHeight + gap) + 22f);
        choicePanel.SetAsLastSibling();
        promptReadyAt = Time.unscaledTime + 0.25f;

        TextMeshProUGUI heading = HudTheme.CreateLabel("Heading", body, table.Content.speakerName.ToUpperInvariant() + "'S CLIENT COPY \u00B7 TAX AND PAYMENT", 24f,
            HudTheme.Brass, TextAlignmentOptions.Left, FontStyles.Bold);
        HudTheme.PlaceTopLeft(heading.rectTransform, 30f, 16f, 700f, 40f);

        RectTransform later = HudTheme.CreateRect("Talk later", body);
        HudTheme.PlaceTopLeft(later, width - 64f - 160f, 14f, 160f, 44f);
        Image laterFace = HudTheme.AddImage(later, HudTheme.Rounded, HudTheme.Track, 8f);
        laterFace.raycastTarget = true;
        Button laterButton = later.gameObject.AddComponent<Button>();
        laterButton.targetGraphic = laterFace;
        laterButton.onClick.AddListener(() =>
        {
            if (Time.unscaledTime < promptReadyAt || dialogue == null) return;
            AudioManager.Instance?.QueueInteractionSFX("continue");
            ClearChoices();
            dialogue.CancelDialogueFor(this);
        });
        TextMeshProUGUI laterText = HudTheme.CreateLabel("Talk later text", later, "Talk later", 22f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Normal);
        HudTheme.Stretch(laterText.rectTransform, 3f);

        // The copy's own paper and ink: the same figures the Auditor approved, nothing recalculated.
        Color32 paper = new Color32(246, 240, 226, 255), ink = new Color32(38, 34, 30, 255);
        int attempt = chapter.Case(Level3ChapterRules.BeaCaseId).balanceWrongAnswers + 1;
        Button first = null;
        for (int i = 0; i < evidence.Length; i++)
        {
            string id = evidence[i];
            RectTransform row = HudTheme.CreateRect("Figure " + id, body);
            HudTheme.PlaceTopLeft(row, 30f, top + i * (rowHeight + gap), width - 64f, rowHeight);
            Image face = HudTheme.AddImage(row, HudTheme.Rounded, paper, 10f);
            face.raycastTarget = true;
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(() =>
            {
                if (Time.unscaledTime < promptReadyAt || choicePanel == null) return;
                AudioManager.Instance?.PlayPaperSFX();
                PointTo(id, attempt);
            });
            TextMeshProUGUI label = HudTheme.CreateLabel("Label", row, Level3DeliveryRules.Label(id), 30f, ink, TextAlignmentOptions.Left, FontStyles.Normal);
            HudTheme.PlaceTopLeft(label.rectTransform, 22f, 0f, 520f, rowHeight);
            TextMeshProUGUI amount = HudTheme.CreateLabel("Amount", row, Money(Level3DeliveryRules.Figure(chapter, id)), 34f, ink, TextAlignmentOptions.Right, FontStyles.Bold);
            HudTheme.PlaceTopLeft(amount.rectTransform, 540f, 0f, width - 64f - 540f - 24f, rowHeight);
            if (first == null) first = button;
        }
        if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        dialogue.DockChoices(choicePanel);
    }

    /// <summary>
    /// The player points to one figure. Not the balance: one contextual sentence and the same question
    /// waits; the saved attempt affects the earned follow-through reward. The balance is saved before her reply and handover.
    /// </summary>
    private void PointTo(string id, int attempt)
    {
        ClearChoices();
        Level3ClientContent bea = table.Content;
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (dialogue == null || owner == null || owner.State == null) return;
        var builder = new DialogueBuilder(bea.speakerName)
            .Player(Level3DeliveryRules.Label(id) + ": " + Money(Level3DeliveryRules.Figure(owner.State, id)) + ".", PlayerPortrait);
        Level3Checkpointer.TapeCommit outcome = owner.CommitBalanceExplanation(id, out string hint, attempt);
        if (outcome == Level3Checkpointer.TapeCommit.SaveFailed)
        {
            // Nothing saved: the same question and figures wait.
            dialogue.SetActionPrompt(hint);
            ShowCopyFigures();
            return;
        }
        if (outcome == Level3Checkpointer.TapeCommit.Refused)
        {
            dialogue.CancelDialogueFor(this);
            if (!string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint);
            return;
        }
        if (!Level3DeliveryRules.Answers(id))
        {
            List<DialogueLine> retry = builder.Build();
            retry.Add(QuestionLine(bea, id == Level3DeliveryRules.TaxDue ? bea.taxDueHint : bea.creditHint));
            int errors = owner.State.Case(Level3ChapterRules.BeaCaseId).balanceWrongAnswers;
            if (errors % 3 == 0 && errors <= 9)
                retry.Add(new DialogueBuilder("Arthur").Npc("Follow-through reward: +" + Level3DeliveryRules.FollowThroughTrust(errors) + " Public Trust. Take another look at the balance.", "Arthur_Default").Build()[0]);
            if (dialogue.ContinueWith(BalanceAction, retry) < 0) dialogue.CancelDialogueFor(this);
            return;
        }
        Debug.Log("[Level3] Bea's balance explained from her approved copy (saved). The copy is still carried.");
        List<DialogueLine> lines = builder.Npc(bea.balanceReply, bea.portraitId).Build();
        lines.Add(HandoverLine(bea));
        if (dialogue.ContinueWith(BalanceAction, lines) < 0) dialogue.CancelDialogueFor(this);
    }

    // =========================================================
    // T14: WHAT HER FILE SHOWS
    // =========================================================

    /// <summary>After her delivery: the draft question until the finding is saved, then one short line.</summary>
    private void ClosingTalk(Level3ChapterCheckpoint chapter, Level3ClientContent bea)
    {
        switch (Level3ClosingRules.StageOf(chapter))
        {
            case Level3ClosingRules.Stage.Finding: StartFinding(bea); break;
            case Level3ClosingRules.Stage.Message: Talk(Npc(new List<string> { bea.afterFindingLine }), null); break;
            default: Talk(Npc(new List<string> { bea.afterDeliveryLine }), null); break;
        }
    }

    private DialogueLine FindingLine(Level3ClientContent bea, string text) =>
        new DialogueBuilder(bea.speakerName).Npc(text, bea.portraitId).AwaitAction(FindingAction, bea.findingPrompt).Build()[0];

    private void StartFinding(Level3ClientContent bea)
    {
        OfficeHudRuntime.Instance?.CloseCaseFolderView();
        if (!Talk(new List<DialogueLine> { FindingLine(bea, bea.findingQuestion) }, null)) return;
        Hook();
        if (dialogue.IsAwaitingAction && !dialogue.IsLineTyping) ShowFindingChoices();
    }

    /// <summary>The three statements (her docked choice card), Review her records and Talk later. Nothing is marked right or wrong here.</summary>
    private void ShowFindingChoices()
    {
        Level3ChapterCheckpoint chapter = Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
        if (Level3ClosingRules.StageOf(chapter) != Level3ClosingRules.Stage.Finding) { ClearChoices(); return; }
        Level3ClientContent bea = table.Content;
        var labels = new List<string>();
        var actions = new List<Action>();
        foreach (string id in Level3ClosingRules.Statements)
        {
            Level3CreditAnswer choice = bea.findingChoices?.Find(c => c != null && c.id == id);
            if (choice == null) continue;
            string chosen = id;
            labels.Add(choice.text);
            actions.Add(() => StateFinding(chosen));
        }
        labels.Add(bea.findingReview);
        actions.Add(ReviewRecords);
        ShowPanel(labels, actions, bea.findingTitle);
    }

    /// <summary>
    /// The player says one statement. Intent or a wider pattern: her one-line reply waits on the same
    /// question (nothing saved, no penalty). The supported one: saved first, then her reply and the next step.
    /// </summary>
    private void StateFinding(string id)
    {
        ClearChoices();
        Level3ClientContent bea = table.Content;
        Level3Checkpointer owner = Level3Checkpointer.Active;
        Level3CreditAnswer choice = bea.findingChoices?.Find(c => c != null && c.id == id);
        if (dialogue == null || owner == null || owner.State == null || choice == null) return;
        var builder = new DialogueBuilder(bea.speakerName).Player(choice.text, PlayerPortrait);
        if (id != Level3ClosingRules.Supported)
        {
            List<DialogueLine> retry = builder.Build();
            retry.Add(FindingLine(bea, choice.reply));
            if (dialogue.ContinueWith(FindingAction, retry) < 0) dialogue.CancelDialogueFor(this);
            return;
        }
        Level3Checkpointer.TapeCommit outcome = owner.CommitFinding(id, out string hint);
        if (outcome == Level3Checkpointer.TapeCommit.SaveFailed)
        {
            // Nothing saved: the same question and statements wait.
            dialogue.SetActionPrompt(hint);
            ShowFindingChoices();
            return;
        }
        if (outcome == Level3Checkpointer.TapeCommit.Refused)
        {
            dialogue.CancelDialogueFor(this);
            if (!string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint);
            return;
        }
        Debug.Log("[Level3] Bea's finding is recorded from her own file (saved). The receptionist's message is next.");
        List<DialogueLine> lines = builder.Npc(choice.reply, bea.portraitId).Npc(bea.findingNext, bea.portraitId).Build();
        if (dialogue.ContinueWith(FindingAction, lines) < 0) dialogue.CancelDialogueFor(this);
    }

    /// <summary>Review her records: the talk ends (nothing saved); her papers open in the existing Case Folder reader once it has.</summary>
    private void ReviewRecords()
    {
        ClearChoices();
        reviewAfter = true;
        if (dialogue != null && dialogue.CancelDialogueFor(this)) return;
        reviewAfter = false;
        if (isActiveAndEnabled) StartCoroutine(OpenRecordsWhenFree());
    }

    private IEnumerator OpenRecordsWhenFree()
    {
        float until = Time.unscaledTime + 3f;
        while (DialogueUI.IsAnyDialogueActive && Time.unscaledTime < until) yield return null;
        HudHotbar bar = HudHotbar.Instance;
        if (bar == null || !bar.OpenViewer(InventoryCatalogue.CaseFolder)) Level3BeaTable.Level3Say(RecordsFallback);
    }

    // ClientCopyHandover.ILevelDelivery: the existing folder icon tap/drag during her handover line.

    public bool AwaitingCopy => talking && dialogue != null && DialogueUI.Active == dialogue && dialogue.CurrentOwner == this &&
                                DialogueUI.ActiveAwaitingAction == ClientCopyRules.HandoverAction;

    public string ClientName => table != null && table.Content != null ? table.Content.speakerName : "Bea";

    public bool TryIconTap()
    {
        if (!AwaitingCopy) return false;
        DeliverCopy();
        return true;
    }

    public HudDragOutcome? ResolveDrop(Vector2 screen)
    {
        if (!AwaitingCopy) return null;
        RectTransform icon = OfficeHudRuntime.Instance != null ? OfficeHudRuntime.Instance.CaseFolderIcon : null;
        if (icon != null && RectTransformUtility.RectangleContainsScreenPoint(icon, screen, null)) return HudDragOutcome.Returned;
        if (!OverBea(screen)) return HudDragOutcome.Returned;
        return DeliverCopy() ? HudDragOutcome.Consumed : HudDragOutcome.Returned;
    }

    private bool OverBea(Vector2 screen)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(screen);
        foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            if (hit.collider != null && (hit.collider.GetComponentInParent<Level3BeaInteractable>() == this || hit.collider.transform.IsChildOf(transform))) return true;
        return false;
    }

    /// <summary>
    /// The copy reached her: one chapter command, saved first (copy done, her closing step, +4 once).
    /// Only then does the icon empty, the paper sound, her thanks, emote and the toast follow. A failed
    /// or refused write keeps the copy carried and the same line waiting.
    /// </summary>
    private bool DeliverCopy()
    {
        if (!AwaitingCopy || delivering) return false;
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return false;
        delivering = true;
        try
        {
            Level3Checkpointer.TapeCommit outcome = owner.CommitDelivery(out Level3DeliveryGrants grants, out string hint);
            if (outcome == Level3Checkpointer.TapeCommit.SaveFailed || outcome == Level3Checkpointer.TapeCommit.Refused)
            {
                dialogue.SetActionPrompt(outcome == Level3Checkpointer.TapeCommit.SaveFailed ? table.Content.copyRetryPrompt : hint);
                return false;
            }
            if (outcome == Level3Checkpointer.TapeCommit.Attached) PlayDelivered(grants);
            Debug.Log("[Level3] Bea has her copy (saved). Chapter: " + owner.Phase + ".");
            List<DialogueLine> thanks = new DialogueBuilder(table.Content.speakerName).Npc(table.Content.deliveredLine, table.Content.portraitId).Emote("heart").Build();
            if (dialogue.ContinueWith(ClientCopyRules.HandoverAction, thanks) < 0) dialogue.CancelDialogueFor(this);
            return true;
        }
        finally { delivering = false; }
    }

    private void PlayDelivered(Level3DeliveryGrants grants)
    {
        Level2FolderHandoff.FindStore(gameObject.scene)?.ReleaseQuietly();
        Transform icon = Level2FolderHandoff.FindFolderIcon(gameObject.scene);
        if (icon != null) icon.GetComponent<HudIconButton>()?.RefreshFolderState();
        AudioManager.Instance?.PlayPaperSFX();
        WorldEmote.Burst(transform, Emotion.Thankful, 7);
        // A non-story attempt or an already credited campaign shows no toast (a default Change reads as Applied).
        if (!grants.Changed || !grants.Trust.Applied || grants.Trust.Amount <= 0) return;
        Vector3 head = transform.position + Vector3.up * 1.6f;
        if (DialogueSpeakerFrames.TryResolve(this, out DialogueSpeakerFrame frame)) head = frame.Head;
        HudFeedback.AnnounceCampaign(grants.Trust, default, head);
        OfficeHudRuntime.Instance?.PeekDuringDialogue(3.2f);
    }

    private void ShowChoices()
    {
        List<Level3Inquiry> ordered = Level3TownContent.Ordered(table.Content, table.LastReadSource);
        var labels = new List<string>();
        var actions = new List<Action>();
        if (offerHelp && table.CanOfferHelp)
        {
            labels.Add(table.Content.helpChoice);
            actions.Add(() => ChooseHelp(InquiryAction));
        }
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

    /// <summary>The same docked choice card as Rosa's (Level2RosaConsultation): one button per question (or answer), Talk later (not when a choice already says Later).</summary>
    private void ShowPanel(List<string> labels, List<Action> actions, string title = null, bool talkLater = true)
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

        if (talkLater)
        {
            RectTransform later = HudTheme.CreateRect("Talk later", body);
            HudTheme.PlaceTopLeft(later, 756f, 14f, 160f, 40f);
            Image laterFace = HudTheme.AddImage(later, HudTheme.Rounded, HudTheme.Track, 8f);
            laterFace.raycastTarget = true;
            Button laterButton = later.gameObject.AddComponent<Button>();
            laterButton.targetGraphic = laterFace;
            laterButton.onClick.AddListener(() =>
            {
                if (Time.unscaledTime < promptReadyAt || dialogue == null) return;
                AudioManager.Instance?.QueueInteractionSFX("continue");
                ClearChoices();
                dialogue.CancelDialogueFor(this);
            });
            TextMeshProUGUI laterText = HudTheme.CreateLabel("Talk later text", later, "Talk later", 20f, HudTheme.TextPrimary,
                TextAlignmentOptions.Center, FontStyles.Normal);
            HudTheme.Stretch(laterText.rectTransform, 3f);
        }

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
