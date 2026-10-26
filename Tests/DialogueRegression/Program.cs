using System;
using System.Collections.Generic;
using System.Linq;

int checks = 0;

void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}

DialogueLine MakeLine(string text, DialogueSpeaker speaker = DialogueSpeaker.Npc,
    string portraitId = "default", string speakerName = "NPC") =>
    new(speaker, text, portraitId, speakerName);

var invalidSession = new DialogueSession();
void CheckRejected(IReadOnlyList<DialogueLine> source, string label)
{
    int completedCalls = 0;
    int cancelledCalls = 0;

    Check(!invalidSession.TryStart(source, () => completedCalls++, () => cancelledCalls++),
        $"{label} input must be rejected.");
    Check(!invalidSession.IsActive && invalidSession.LineIndex == 0,
        $"{label} input must leave the session inactive.");
    Check(completedCalls == 0 && cancelledCalls == 0,
        $"{label} input must not invoke either callback.");
}

CheckRejected(null, "Null list");
CheckRejected(Array.Empty<DialogueLine>(), "Empty list");
CheckRejected(new DialogueLine[] { null }, "Null line");
CheckRejected(new[] { MakeLine("") }, "Empty text");
CheckRejected(new[] { MakeLine(" \t ") }, "Whitespace text");

var session = new DialogueSession();
var originalFirstLine = MakeLine("First line", DialogueSpeaker.Player, "portrait-first", "Taxpayer");
var originalLastLine = MakeLine("Last line", DialogueSpeaker.Npc, "portrait-last", "Clerk");
var sourceLines = new List<DialogueLine> { originalFirstLine, originalLastLine };
int completedCount = 0;
int cancelledCount = 0;
int rejectedStartCallbackCount = 0;

Check(session.TryStart(sourceLines, () => completedCount++, () => cancelledCount++),
    "A valid two-line conversation must start.");
Check(session.IsActive && session.LineIndex == 0, "A new conversation must be active at line zero.");
Check(session.CurrentLine != null &&
      session.CurrentLine.Speaker == DialogueSpeaker.Player &&
      session.CurrentLine.Text == "First line" &&
      session.CurrentLine.PortraitId == "portrait-first" &&
      session.CurrentLine.SpeakerName == "Taxpayer",
    "The first line must expose all of its original content.");

sourceLines[0] = MakeLine("Replacement line");
sourceLines.RemoveAt(1);
originalFirstLine.Speaker = DialogueSpeaker.Npc;
originalFirstLine.Text = "Mutated source text";
originalFirstLine.PortraitId = "mutated portrait";
originalFirstLine.SpeakerName = "Mutated speaker";
originalLastLine.Text = "Mutated final text";
originalLastLine.SpeakerName = "Mutated final speaker";

Check(session.CurrentLine.Speaker == DialogueSpeaker.Player &&
      session.CurrentLine.Text == "First line" &&
      session.CurrentLine.PortraitId == "portrait-first" &&
      session.CurrentLine.SpeakerName == "Taxpayer",
    "Changing source line fields must not change the active snapshot.");
Check(!session.TryStart(new[] { MakeLine("Second conversation") },
        () => rejectedStartCallbackCount++, () => rejectedStartCallbackCount++),
    "An active session must reject a second start.");
Check(session.LineIndex == 0 && session.CurrentLine.Text == "First line",
    "A rejected second start must preserve the active conversation.");

Action prematureCallback;
Check(!session.TryEnd(DialogueEndReason.Completed, out prematureCallback),
    "Completion must be rejected while later lines remain.");
Check(prematureCallback == null && session.IsActive && session.LineIndex == 0,
    "Rejected premature completion must preserve state and return no callback.");
Check(completedCount == 0 && cancelledCount == 0 && rejectedStartCallbackCount == 0,
    "No callback may run before the conversation reaches a terminal outcome.");

Check(session.TryAdvance(out bool completedAfterFirst) && !completedAfterFirst,
    "Advancing from the first line must continue the conversation.");
Check(session.LineIndex == 1 &&
      session.CurrentLine.Speaker == DialogueSpeaker.Npc &&
      session.CurrentLine.Text == "Last line" &&
      session.CurrentLine.PortraitId == "portrait-last" &&
      session.CurrentLine.SpeakerName == "Clerk",
    "The snapshotted final line must remain available after source mutation.");
Check(session.TryAdvance(out bool completedAtLast) && completedAtLast,
    "Advancing at the final line must report completion to the caller.");
Check(session.IsActive && session.LineIndex == 1 && completedCount == 0 && cancelledCount == 0,
    "Reaching the final line must leave terminal claiming and callbacks to the caller.");

Check(session.TryEnd(DialogueEndReason.Completed, out Action completionCallback) &&
      completionCallback != null,
    "The caller must be able to claim completion at the final line.");
Check(!session.IsActive && session.LineIndex == 0 && session.CurrentLine == null,
    "Claiming completion must clear session state before returning the callback.");
Check(completedCount == 0 && cancelledCount == 0,
    "Claiming completion must not run the callback itself.");
completionCallback();
Check(completedCount == 1 && cancelledCount == 0 && rejectedStartCallbackCount == 0,
    "Completion must invoke only the original completion callback once.");
Check(!session.TryEnd(DialogueEndReason.Completed, out Action repeatedCompletion) &&
      repeatedCompletion == null,
    "A completed session must not yield a second completion callback.");
Check(!session.TryEnd(DialogueEndReason.Cancelled, out Action repeatedCancellation) &&
      repeatedCancellation == null,
    "A completed session must not yield a cancellation callback.");
Check(completedCount == 1 && cancelledCount == 0,
    "Repeated terminal claims must not invoke either callback.");
Check(!session.TryAdvance(out bool completedAfterEnd) && !completedAfterEnd,
    "An ended session must not advance.");

var reentrantSession = new DialogueSession();
int reentrantCompletionCount = 0;
int reentrantCancellationCount = 0;
bool callbackObservedClearedState = false;
bool callbackStartedNextSession = false;
var nextLine = MakeLine("Next session");
Check(reentrantSession.TryStart(new[] { MakeLine("First session") }, () =>
    {
        reentrantCompletionCount++;
        callbackObservedClearedState = !reentrantSession.IsActive &&
            reentrantSession.LineIndex == 0 && reentrantSession.CurrentLine == null;
        callbackStartedNextSession = reentrantSession.TryStart(new[] { nextLine },
            () => { }, () => { });
    }, () => reentrantCancellationCount++), "The reentrancy case must start.");
Check(reentrantSession.TryEnd(DialogueEndReason.Completed, out Action reentrantCallback),
    "The reentrancy case must allow its terminal claim.");
reentrantCallback();
Check(reentrantCompletionCount == 1 && reentrantCancellationCount == 0,
    "The reentrant completion must run only its original callback.");
Check(callbackObservedClearedState && callbackStartedNextSession && reentrantSession.IsActive &&
      reentrantSession.CurrentLine.Text == "Next session",
    "A terminal callback must see cleared state and be able to start the next session.");

var throwingSession = new DialogueSession();
Check(throwingSession.TryStart(new[] { MakeLine("Throwing callback") },
        () => throw new InvalidOperationException("expected callback failure"), () => { }),
    "The throwing-callback case must start.");
Check(throwingSession.TryEnd(DialogueEndReason.Completed, out Action throwingCallback),
    "The throwing-callback case must allow its terminal claim.");
bool expectedExceptionObserved = false;
try
{
    throwingCallback();
}
catch (InvalidOperationException exception) when (exception.Message == "expected callback failure")
{
    expectedExceptionObserved = true;
}
Check(expectedExceptionObserved, "The expected callback exception must propagate to its caller.");
Check(!throwingSession.IsActive && throwingSession.LineIndex == 0 && throwingSession.CurrentLine == null,
    "A throwing terminal callback must not restore the ended session state.");
Check(throwingSession.TryStart(new[] { MakeLine("After exception") }, () => { }, () => { }) &&
      throwingSession.CurrentLine.Text == "After exception",
    "A new session must still start after the previous callback throws.");

var retrySession = new DialogueSession();
int retryCompletedCount = 0;
int retryCancelledCount = 0;
Check(retrySession.TryStart(new[] { MakeLine("Can be cancelled"), MakeLine("Later line") },
        () => retryCompletedCount++, () => retryCancelledCount++),
    "The cancellation case must start.");
Check(retrySession.TryEnd(DialogueEndReason.Cancelled, out Action cancellationCallback) &&
      cancellationCallback != null,
    "Cancellation must be claimable before the final line.");
Check(!retrySession.IsActive && retryCompletedCount == 0 && retryCancelledCount == 0,
    "Claiming cancellation must clear state without running either callback.");
cancellationCallback();
Check(retryCompletedCount == 0 && retryCancelledCount == 1,
    "Cancellation must invoke only its cancellation callback.");
Check(retrySession.TryStart(new[] { MakeLine("Retry after cancellation") },
        () => retryCompletedCount++, () => retryCancelledCount++),
    "A new conversation must be accepted after cancellation.");
Check(retrySession.TryEnd(DialogueEndReason.Completed, out Action retryCompletionCallback),
    "The retried one-line conversation must be completable.");
retryCompletionCallback();
Check(retryCompletedCount == 1 && retryCancelledCount == 1,
    "The retry must complete through its own completion callback only.");

// ---------------------------------------------------------------------
// T08 awaited dialogue actions. These use the same pure session that backs
// DialogueUI, so an action cannot silently become an ordinary tap-to-advance
// line while a conversation is paused for a handover.
// ---------------------------------------------------------------------
var handoverBuilder = new DialogueBuilder("Marie")
    .Npc("I have your approved return.")
    .Npc("Please bring the client copy to me.")
    .AwaitAction("client-copy", "Drag the client copy to Marie")
    .Npc("Thank you for walking me through it.");
var handoverLines = handoverBuilder.Build();
var handover = new DialogueSession();
int handoverCompleted = 0;
int handoverCancelled = 0;
Check(handoverLines.Count == 3 && handoverLines[1].AwaitAction == "client-copy" &&
      handoverLines[1].ActionPrompt == "Drag the client copy to Marie" &&
      handover.TryStart(handoverLines, () => handoverCompleted++, () => handoverCancelled++),
    "the generic dialogue builder marks a line with its action id and player prompt");
Check(handover.TryAdvance(out bool afterOpening) && !afterOpening && handover.IsAwaitingAction &&
      handover.AwaitingActionId == "client-copy" && handover.CurrentLine.ActionPrompt == "Drag the client copy to Marie",
    "the handover line pauses with its action and prompt exposed");
int waitingIndex = handover.LineIndex;
Check(!handover.TryAdvance(out bool completedByTap) && !completedByTap && handover.LineIndex == waitingIndex &&
      handover.IsAwaitingAction && !handover.TryCompleteAction("other-action") &&
      !handover.TryCompleteAction(null),
    "ordinary advance gestures and missing or wrong action ids leave the dialogue paused");
Check(handover.TryCompleteAction(" client-copy ") && !handover.IsAwaitingAction &&
      !handover.TryCompleteAction("client-copy") && handover.TryAdvance(out bool afterHandover) && !afterHandover &&
      handover.CurrentLine.Text == "Thank you for walking me through it.",
    "the matching action completes once, then the conversation continues automatically");
Action handoverCompletionValue = null;
Check(handover.TryAdvance(out bool handoverDone) && handoverDone &&
      handover.TryEnd(DialogueEndReason.Completed, out handoverCompletionValue) && handoverCompletionValue != null,
    "the resumed conversation completes normally after its closing line");
handoverCompletionValue();
Check(handoverCompleted == 1 && handoverCancelled == 0,
    "a completed handover conversation calls only its completion callback");

var pausedForCancel = new DialogueSession();
int pausedCompleted = 0;
int pausedCancelled = 0;
Action pausedCancellation = null;
Check(pausedForCancel.TryStart(new[] { new DialogueBuilder("Marie").Npc("Bring me the copy.")
        .AwaitAction("client-copy", "Drag the client copy to Marie").Build()[0] },
        () => pausedCompleted++, () => pausedCancelled++) &&
      !pausedForCancel.TryEnd(DialogueEndReason.Completed, out Action prematureAwaitedCompletion) &&
      prematureAwaitedCompletion == null &&
      pausedForCancel.TryEnd(DialogueEndReason.Cancelled, out pausedCancellation),
    "an awaited final line cannot complete early, but its conversation can be cancelled");
pausedCancellation();
Check(pausedCancelled == 1 && pausedCompleted == 0 &&
      pausedForCancel.TryStart(new[] { new DialogueBuilder("Marie").Npc("Let's try again.")
          .AwaitAction("retry-copy", "Bring the copy again").Build()[0] },
          () => pausedCompleted++, () => pausedCancelled++) &&
      pausedForCancel.IsAwaitingAction && pausedForCancel.AwaitingActionId == "retry-copy" &&
      !pausedForCancel.TryCompleteAction("client-copy"),
    "cancelling and restarting clears the old awaited action and its completion state");
Action restartedCompletion = null;
Check(pausedForCancel.TryCompleteAction("retry-copy") &&
      pausedForCancel.TryEnd(DialogueEndReason.Completed, out restartedCompletion),
    "a restarted awaited conversation completes only after its own action");
restartedCompletion();
Check(pausedCompleted == 1 && pausedCancelled == 1,
    "the restarted session invokes its own completion callback once");

var advanceGate = new DialogueAdvanceGate();
Check(advanceGate.Evaluate(1, 0.2f, lineIsTyping: false) == DialogueGesture.Ignored,
    "the dialogue advance gate ignores gestures before a conversation opens");
advanceGate.Open(10, 1f);
Check(advanceGate.Evaluate(10, 1.2f, lineIsTyping: false) == DialogueGesture.Ignored &&
      advanceGate.Evaluate(11, 1.1f, lineIsTyping: false) == DialogueGesture.Ignored &&
      advanceGate.Evaluate(12, 1.16f, lineIsTyping: true) == DialogueGesture.RevealLine &&
      advanceGate.Evaluate(13, 1.2f, lineIsTyping: false) == DialogueGesture.Ignored &&
      advanceGate.Evaluate(14, 1.30f, lineIsTyping: false) == DialogueGesture.Advance,
    "ordinary DialogueAdvanceGate behavior still ignores opening/duplicate taps, reveals typed lines, then advances");
advanceGate.Close();
Check(!advanceGate.IsOpen && advanceGate.Evaluate(15, 2f, lineIsTyping: false) == DialogueGesture.Ignored,
    "closing the gate blocks later gestures");

// ---------------------------------------------------------------------
// T02 Phase 2 - town taxpayer and tutorial guide content contracts.
// These exercise the production TownNpcDialogueLibrary and
// TaxpayerProfileDatabase through the same DialogueSession the shared UI
// uses, so a content/configuration mismatch fails here instead of
// retiring an NPC in Unity without dropping its profile.
// ---------------------------------------------------------------------

string[] townNpcIds = { "npc_marie", "npc_jhunjhun", "npc_milo", "npc_zorbix" };

Check(TaxpayerProfileDatabase.All.Count == townNpcIds.Length,
    "The taxpayer database must hold exactly one profile per town NPC.");

var seenProfileIds = new HashSet<string>();
var seenClassifications = new HashSet<TaxpayerClassification>();

foreach (string npcId in townNpcIds)
{
    Check(TownNpcDialogueLibrary.HasDialogueFor(npcId),
        $"'{npcId}' must have authored dialogue.");

    string displayName = $"Display {npcId}";
    List<DialogueLine> lines =
        TownNpcDialogueLibrary.GetDialogueFor(npcId, displayName, out bool recognized);

    Check(recognized, $"'{npcId}' must be reported as recognized content.");
    Check(lines != null && lines.Count > 1,
        $"'{npcId}' must produce a real multi-line conversation.");

    bool hasNpcLine = false;
    bool hasPlayerLine = false;

    for (int i = 0; i < lines.Count; i++)
    {
        DialogueLine line = lines[i];
        Check(line != null && !string.IsNullOrWhiteSpace(line.Text),
            $"'{npcId}' line {i} must carry text the session will accept.");

        if (line.Speaker == DialogueSpeaker.Npc)
        {
            hasNpcLine = true;
            Check(line.SpeakerName == displayName,
                $"'{npcId}' NPC line {i} must use the caller's display name.");
        }
        else
        {
            hasPlayerLine = true;
            Check(line.SpeakerName == "You",
                $"'{npcId}' player line {i} must be attributed to the player.");
        }
    }

    Check(hasNpcLine && hasPlayerLine,
        $"'{npcId}' must be a two-sided conversation.");

    // The same content must survive admission and run to exactly one
    // completion, because that completion is what drops this profile.
    var townSession = new DialogueSession();
    int townCompletedCount = 0;
    int townCancelledCount = 0;

    Check(townSession.TryStart(lines, () => townCompletedCount++, () => townCancelledCount++),
        $"'{npcId}' content must be admitted by a conversation session.");

    int advances = 0;
    bool reachedEnd = false;
    while (townSession.TryAdvance(out bool townCompleted))
    {
        advances++;
        if (townCompleted)
        {
            reachedEnd = true;
            break;
        }

        Check(advances <= lines.Count,
            $"'{npcId}' must not advance past its own line count.");
    }

    // One advance per line: the first moves off the opening line and the
    // last reports completion without moving past the final line.
    Check(reachedEnd && advances == lines.Count,
        $"'{npcId}' must report completion only after its last line.");
    Check(townSession.TryEnd(DialogueEndReason.Completed, out Action townCallback) &&
          townCallback != null,
        $"'{npcId}' must allow exactly one completion claim.");
    townCallback();
    Check(townCompletedCount == 1 && townCancelledCount == 0,
        $"'{npcId}' completion must fire its profile-drop callback once.");
    Check(!townSession.TryEnd(DialogueEndReason.Completed, out Action townRepeat) &&
          townRepeat == null && townCompletedCount == 1,
        $"'{npcId}' must not be able to drop its profile twice.");

    // Identity wiring: the npcId the scene serializes has to resolve to a
    // profile, otherwise the conversation has no deliverable outcome.
    TaxpayerProfile profile = TaxpayerProfileDatabase.GetByNpcId(npcId);
    Check(profile != null, $"'{npcId}' must resolve to a taxpayer profile.");
    Check(profile.NpcId == npcId, $"'{npcId}' profile must point back at its own NPC id.");
    Check(!string.IsNullOrWhiteSpace(profile.ProfileId) &&
          !string.IsNullOrWhiteSpace(profile.TaxpayerName) &&
          !string.IsNullOrWhiteSpace(profile.Description),
        $"'{npcId}' profile must carry displayable content.");
    Check(ReferenceEquals(TaxpayerProfileDatabase.GetById(profile.ProfileId), profile),
        $"'{npcId}' profile must be reachable by its own profile id.");
    Check(seenProfileIds.Add(profile.ProfileId),
        $"'{npcId}' must not share a profile id with another NPC.");
    Check(seenClassifications.Add(profile.CorrectClassification),
        $"'{npcId}' must not share a corkboard classification with another NPC.");
}

Check(seenProfileIds.Count == 4 && seenClassifications.Count == 4,
    "The four taxpayers must fill the four distinct corkboard corners.");

// A misconfigured npcId must be reported as unrecognized. The placeholder
// fallback is deliberately a *valid* conversation, so the recognized flag
// is the only thing preventing a broken NPC from completing and retiring
// itself without ever dropping a profile.
foreach (string unknownId in new[] { "npc_missing", "", " ", "NPC_MARIE", null })
{
    Check(!TownNpcDialogueLibrary.HasDialogueFor(unknownId),
        $"Unknown npcId '{unknownId ?? "<null>"}' must not be reported as authored content.");

    List<DialogueLine> placeholder =
        TownNpcDialogueLibrary.GetDialogueFor(unknownId, "Taxpayer", out bool unknownRecognized);

    Check(!unknownRecognized,
        $"Unknown npcId '{unknownId ?? "<null>"}' must report unrecognized content.");

    var placeholderSession = new DialogueSession();
    Check(placeholderSession.TryStart(placeholder, () => { }, () => { }),
        "The placeholder fallback is a valid session, so callers must gate on the recognized flag.");
}

Check(TaxpayerProfileDatabase.GetByNpcId("npc_missing") == null &&
      TaxpayerProfileDatabase.GetById("profile_missing") == null,
    "Unknown identifiers must not resolve to a taxpayer profile.");

// The tutorial guide's two conversations are built in code rather than
// from the library, so verify they are admissible and end once each.
// Arthur's corkboard talks come from production CorkboardTeaching.
var guideIntroLines = CorkboardTeaching.BuildIntro("Arthur");
var guideCompletionLines = CorkboardTeaching.BuildDone("Arthur");

void CheckGuideConversation(List<DialogueLine> guideLines, string label, int expectedCount)
{
    Check(guideLines.Count == expectedCount, $"{label} must keep its authored line count.");

    for (int i = 0; i < guideLines.Count; i++)
    {
        Check(!string.IsNullOrWhiteSpace(guideLines[i].Text),
            $"{label} line {i} must carry text.");
    }

    var guideSession = new DialogueSession();
    int guideCompleted = 0;
    int guideCancelled = 0;

    Check(guideSession.TryStart(guideLines, () => guideCompleted++, () => guideCancelled++),
        $"{label} must be admitted.");

    // Cancelling must never run the outcome: the board must not open and
    // the result panel must not show from an interrupted conversation.
    Check(guideSession.TryEnd(DialogueEndReason.Cancelled, out Action guideCancelCallback),
        $"{label} must be cancellable before its last line.");
    guideCancelCallback();
    Check(guideCompleted == 0 && guideCancelled == 1,
        $"{label} cancellation must not run its success outcome.");

    // Retried after cancellation, it must still be able to complete once.
    Check(guideSession.TryStart(guideLines, () => guideCompleted++, () => guideCancelled++),
        $"{label} must be retryable after cancellation.");

    while (guideSession.TryAdvance(out bool guideAtEnd) && !guideAtEnd) { }

    Check(guideSession.TryEnd(DialogueEndReason.Completed, out Action guideCallback),
        $"{label} retry must be completable.");
    guideCallback();
    Check(guideCompleted == 1 && guideCancelled == 1,
        $"{label} must run its success outcome exactly once.");
}

// T26: the board talk is a three-line reminder (the four client biographies were removed).
CheckGuideConversation(guideIntroLines, "The guide intro conversation", 3);
CheckGuideConversation(guideCompletionLines, "The guide completion conversation", 3);

// ---------------------------------------------------------------------
// Level 1 teaching rules (Phase 1): tell the story, never the answer.
// ---------------------------------------------------------------------
string[] bannedLabels = { "resident", "citizen", "alien", "classification" };
bool NamesALabel(string text)
{
    string lower = (text ?? "").ToLowerInvariant();
    foreach (string word in bannedLabels)
        if (lower.Contains(word)) return true;
    return false;
}

foreach (string npcId in townNpcIds)
{
    List<DialogueLine> talk = TownNpcDialogueLibrary.GetDialogueFor(npcId, "Npc", out _);
    Check(talk.Count >= 4 && talk.Count <= 7, $"'{npcId}' talk must stay short (4-7 lines), has {talk.Count}.");
    foreach (DialogueLine line in talk)
        Check(!NamesALabel(line.Text), $"'{npcId}' must not name a classification: \"{line.Text}\"");

    TaxpayerProfile profile = TaxpayerProfileDatabase.GetByNpcId(npcId);
    int words = profile.Description.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
    Check(words >= 9 && words <= 14, $"'{npcId}' profile must keep the card length (9-14 words), has {words}.");
    Check(!NamesALabel(profile.Description), $"'{npcId}' profile must not name a classification.");

    // Every corner gets a reaction: why for the right one, a hint for each wrong one.
    var seen = new HashSet<string>();
    foreach (TaxpayerClassification corner in Enum.GetValues(typeof(TaxpayerClassification)))
    {
        string reaction = CorkboardTeaching.ReactionFor(profile, corner);
        Check(!string.IsNullOrWhiteSpace(reaction), $"'{npcId}' needs a reaction for {corner}.");
        Check(!NamesALabel(reaction), $"'{npcId}' reaction for {corner} must not name a classification.");
        seen.Add(reaction);
    }
    Check(seen.Count == 4, $"'{npcId}' right answer and the three kinds of miss must read differently.");
}

foreach (var line in CorkboardTeaching.IntroLines)
    Check(!NamesALabel(line.Value), $"Arthur's intro must stay indirect: \"{line.Value}\"");
// T26: a reminder with the Arthur already met, not one line per corner.
Check(CorkboardTeaching.IntroLines.Count >= 1 && CorkboardTeaching.IntroLines.Count <= CorkboardTeaching.MaxIntroLines &&
      CorkboardTeaching.MaxIntroLines == 3 && CorkboardTeaching.IntroLines.All(l => !l.Key),
    "Arthur's board introduction is at most three of his own short lines.");
string introText26 = string.Join(" ", CorkboardTeaching.IntroLines.Select(l => l.Value));
string[] clients26 = { "Marie", "Jhun", "Milo", "Zorbix", "passport has never", "overseas", "ticket home", "fell for this place" };
Check(clients26.All(c => introText26.IndexOf(c, StringComparison.OrdinalIgnoreCase) < 0) &&
      introText26.Contains("nationality") && introText26.Contains("where they live") && introText26.Contains("whole board"),
    "the introduction acknowledges the profiles, reminds the two details and says the whole board is checked, describing no client");
var overlong26 = new List<KeyValuePair<bool, string>>();
for (int i = 0; i < 4; i++) overlong26.Add(new KeyValuePair<bool, string>(false, "line " + i));
CorkboardTeaching.SetIntro(overlong26);
Check(CorkboardTeaching.IntroLines.Count <= 3 && CorkboardTeaching.IntroLines[0].Value != "line 0", "a town.json intro longer than three lines keeps the built-in one");
CorkboardTeaching.ClearOverrides();

// T26: the board's static category reference (vocabulary, never an answer).
var categories26 = CorkboardTeaching.Categories;
Check(categories26.Count == 4 && categories26.Select(c => c.Category).Distinct().Count() == 4 &&
      categories26.Select(c => c.Category).SequenceEqual(new[] { TaxpayerClassification.ResidentCitizen, TaxpayerClassification.NonResidentCitizen,
          TaxpayerClassification.ResidentAlien, TaxpayerClassification.NonResidentAlien }),
    "the reference lists the four board categories once each, always in the same order");
foreach (CorkboardTeaching.CategoryMeaning c in categories26)
{
    string m = c.Meaning ?? "";
    int sentences = m.Count(ch => ch == '.' || ch == '!' || ch == '?');
    bool saysFilipino = m.Contains("A Filipino citizen");
    bool saysHomeHere = m.Contains("home is in the Philippines") || m.Contains("home is now in the Philippines");
    Check(sentences == 1 && m.Split(' ').Length <= 14 && saysFilipino == CorkboardTeaching.IsFilipino(c.Category) &&
          saysHomeHere == CorkboardTeaching.LivesHere(c.Category) && c.Name == string.Join(" ", System.Text.RegularExpressions.Regex.Split(c.Category.ToString(), "(?<!^)(?=[A-Z])")).Replace("Non Resident", "Non-Resident"),
        $"{c.Category}: one short sentence from the board's two distinctions (Filipino or not, home here or not), under its formal name");
    Check(clients26.All(n => m.IndexOf(n, StringComparison.OrdinalIgnoreCase) < 0) && !m.Contains("profile") && !m.Contains("corner"),
        $"{c.Category}: names no client, profile or destination");
}
CorkboardTeaching.SetCategoryMeaning(TaxpayerClassification.ResidentAlien, "Edited meaning.");
Check(CorkboardTeaching.Categories[2].Meaning == "Edited meaning." && CorkboardTeaching.Categories[0].Meaning == categories26[0].Meaning,
    "a town.json meaning rewords only its own category");
CorkboardTeaching.ClearOverrides();
Check(CorkboardTeaching.Categories[2].Meaning == categories26[2].Meaning, "clearing restores the built-in meanings");

// The two hidden questions.
Check(CorkboardTeaching.Compare(TaxpayerClassification.ResidentCitizen, TaxpayerClassification.ResidentCitizen) == CorkboardTeaching.Miss.None, "same corner = no miss");
Check(CorkboardTeaching.Compare(TaxpayerClassification.ResidentCitizen, TaxpayerClassification.ResidentAlien) == CorkboardTeaching.Miss.Citizenship, "passport miss");
Check(CorkboardTeaching.Compare(TaxpayerClassification.ResidentCitizen, TaxpayerClassification.NonResidentCitizen) == CorkboardTeaching.Miss.Residence, "home miss");
Check(CorkboardTeaching.Compare(TaxpayerClassification.ResidentCitizen, TaxpayerClassification.NonResidentAlien) == CorkboardTeaching.Miss.Both, "both missed");
Check(CorkboardTeaching.Compare(TaxpayerClassification.NonResidentAlien, TaxpayerClassification.ResidentAlien) == CorkboardTeaching.Miss.Residence, "visitor vs settled = home miss");

// Overrides apply and clear (town.json path).
TaxpayerProfile marieProfile = TaxpayerProfileDatabase.GetByNpcId("npc_marie");
string marieBuiltIn = marieProfile.Description;
TaxpayerProfileDatabase.SetDescription(marieProfile.ProfileId, "Edited note.");
Check(marieProfile.Description == "Edited note.", "a description override applies");
TaxpayerProfileDatabase.ClearOverrides();
Check(marieProfile.Description == marieBuiltIn, "clearing restores the built-in description");
TownNpcDialogueLibrary.SetOverride("npc_marie", new List<KeyValuePair<bool, string>>
{
    new KeyValuePair<bool, string>(true, "Hi."), new KeyValuePair<bool, string>(false, "Hello.")
});
Check(TownNpcDialogueLibrary.GetDialogueFor("npc_marie", "Marie", out bool overrideRecognized).Count == 2 && overrideRecognized, "a talk override applies");
TownNpcDialogueLibrary.SetOverride("npc_missing", new List<KeyValuePair<bool, string>> { new KeyValuePair<bool, string>(false, "x") });
Check(!TownNpcDialogueLibrary.HasDialogueFor("npc_missing"), "an override cannot make an unknown NPC look authored");
TownNpcDialogueLibrary.ClearOverrides();
Check(TownNpcDialogueLibrary.GetDialogueFor("npc_marie", "Marie").Count == 6, "clearing restores the built-in talk");

// T03 presentation contracts (terminal handoff, gesture gate, typewriter, shots).
// 2026-10-26: a waiting line goes on with lines that depend on what just happened (the Auditor's hand-in).
var inserting = new DialogueSession();
var askLine = MakeLine("Hand me the Case Folder.");
askLine.AwaitAction = "auditor-submit";
int insertedDone = 0;
Check(inserting.TryStart(new[] { MakeLine("Hi!", DialogueSpeaker.Player), askLine, MakeLine("Thanks.") }, () => insertedDone++),
    "Insert: the hand-in conversation starts.");
Check(inserting.TryAdvance(out bool insertEnded) && !insertEnded && inserting.AwaitingActionId == "auditor-submit",
    "Insert: the second line waits for the folder.");
Check(inserting.TryInsertAfterCurrent(new[] { MakeLine("Let me check."), MakeLine("Here's my summary.") }) == 2,
    "Insert: new lines go right after the waiting line.");
Check(inserting.TryInsertAfterCurrent(new DialogueLine[] { MakeLine("") }) == -1 && inserting.TryInsertAfterCurrent(null) == -1,
    "Insert: empty or blank lines are refused.");
Check(!inserting.TryAdvance(out _), "Insert: the waiting line still waits (taps do nothing).");
Check(inserting.TryCompleteAction("auditor-submit") && !inserting.TryCompleteAction("auditor-submit"),
    "Insert: the action completes once.");
Check(inserting.TryAdvance(out insertEnded) && !insertEnded && inserting.CurrentLine.Text == "Let me check.",
    "Insert: the conversation continues with the inserted line.");
Check(inserting.TryAdvance(out insertEnded) && inserting.CurrentLine.Text == "Here's my summary." &&
      inserting.TryAdvance(out insertEnded) && inserting.CurrentLine.Text == "Thanks." &&
      inserting.TryAdvance(out insertEnded) && insertEnded,
    "Insert: inserted lines come before the original rest of the conversation.");
Check(new DialogueSession().TryInsertAfterCurrent(new[] { MakeLine("x") }) == -1, "Insert: nothing is inserted into an inactive session.");

int beforePresentation = checks;
PresentationTests.Run(Check);
int presentationChecks = checks - beforePresentation;

// T04 NPC availability/marker contracts.
int beforeMarkers = checks;
NpcMarkerTests.Run(Check);
int markerChecks = checks - beforeMarkers;

// T22 NPC emotion emotes (Campaign Foundation).
int beforeEmotes = checks;
EmoteTests.Run(Check);
int emoteChecks = checks - beforeEmotes;

Console.WriteLine($"PASS: {checks} dialogue contract assertions ({beforePresentation} session/town/guide, " +
    $"{presentationChecks} T03 presentation, {markerChecks} T04 markers, {emoteChecks} T22 emotes) against production DialogueSession, DialogueLine, DialogueBuilder, " +
    "TownNpcDialogueLibrary, TaxpayerProfileDatabase, DialogueTerminalHandoff, DialogueAdvanceGate, " +
    "DialogueRevealSchedule, DialogueShotMath, DialogueCloseUpPolicy, NpcAvailabilityRules, EmoteCatalogue and EmoteShapes.");
