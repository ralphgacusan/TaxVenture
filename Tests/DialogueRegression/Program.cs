using System;
using System.Collections.Generic;

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
var guideIntroLines = new DialogueBuilder("Tutorial Guide")
    .Npc("Good work collecting all four taxpayer profiles!")
    .Npc("Before we head back to the office, let's sort them properly.")
    .Npc("I have a small corkboard here. Each corner represents a taxpayer classification.")
    .Player("Alright, what do I do?")
    .Npc("Simple — drag each profile to the corner that matches how that taxpayer earns their income.")
    .Npc("Don't worry about getting it wrong — you can keep trying until every corner is correct.")
    .Build();

var guideCompletionLines = new DialogueBuilder("Tutorial Guide")
    .Npc("Excellent work! Every profile is in its correct spot.")
    .Npc("You've completed your first side quest — and you're already learning how to tell your taxpayers apart!")
    .Npc("That's exactly the kind of instinct you'll need in the office. Let's head back.")
    .Build();

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

CheckGuideConversation(guideIntroLines, "The guide intro conversation", 6);
CheckGuideConversation(guideCompletionLines, "The guide completion conversation", 3);

// T03 presentation contracts (terminal handoff, gesture gate, typewriter, shots).
int beforePresentation = checks;
PresentationTests.Run(Check);
int presentationChecks = checks - beforePresentation;

// T04 NPC availability/marker contracts.
int beforeMarkers = checks;
NpcMarkerTests.Run(Check);
int markerChecks = checks - beforeMarkers;

Console.WriteLine($"PASS: {checks} dialogue contract assertions ({beforePresentation} session/town/guide, " +
    $"{presentationChecks} T03 presentation, {markerChecks} T04 markers) against production DialogueSession, DialogueLine, DialogueBuilder, " +
    "TownNpcDialogueLibrary, TaxpayerProfileDatabase, DialogueTerminalHandoff, DialogueAdvanceGate, " +
    "DialogueRevealSchedule, DialogueShotMath, DialogueCloseUpPolicy and NpcAvailabilityRules.");
