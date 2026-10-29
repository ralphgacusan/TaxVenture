using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>Focused T19 rule, signed checkpoint and content contracts.</summary>
public static class T19Regression
{
    private const string CaseId = "case_rosa_001";
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true };

    public static int Run(Action<bool, string> check, string root)
    {
        if (check == null) throw new ArgumentNullException(nameof(check));
        int count = 0;
        void C(bool passed, string message) { count++; check(passed, "T19: " + message); }

        const string responsibleText = "I cannot approve it yet. Let’s check the payment together.";
        const string promiseText = "I’ll call it ready now so you can move forward.";
        const string dismissiveText = "Not ready. Bring me complete records next time.";
        C(Level2CampaignRules.ChoiceText(Level2EthicsChoice.Responsible) == responsibleText &&
          Level2CampaignRules.ChoiceText(Level2EthicsChoice.UnsupportedPromise) == promiseText &&
          Level2CampaignRules.ChoiceText(Level2EthicsChoice.DismissiveHold) == dismissiveText &&
          Level2CampaignRules.ChoiceText(Level2EthicsChoice.None) == "",
            "the three authored choices are exact and None has no player-facing copy");

        PlayerProgressData responsible = Fresh("responsible", 40);
        C(Commit(responsible, Level2EthicsChoice.Responsible) && responsible.campaign.publicTrust == 39 &&
          responsible.campaign.rosaEthics.choiceText == responsibleText && responsible.campaign.rosaEthics.followThroughRequired &&
          !responsible.campaign.rosaEthics.followThroughDone && EthicsTotal(responsible) == -1,
            "responsible refusal commits −1 and leaves the supporting-record consequence pending");
        int responsibleAfterChoice = responsible.campaign.publicTrust;
        C(!Level2CampaignRules.ApplyFollowThrough(responsible).Changed && responsible.campaign.publicTrust == responsibleAfterChoice,
            "responsible follow-through cannot pay before the approved case is claimed");
        CompleteApprovedCase(responsible);
        C(responsible.campaign.publicTrust == 48 && EthicsTotal(responsible) == 2 &&
          EthicsEntries(responsible).Select(e => e.amount).OrderBy(n => n).SequenceEqual(new[] { -1, 3 }) &&
          ServiceTrust(responsible) == 6,
            "responsible path nets +2 ethics while T18's independent +2/+4 service trust still totals +6");
        string responsibleDone = Snapshot(responsible);
        C(!Level2CampaignRules.CommitEthicsChoice(responsible, Level2EthicsChoice.DismissiveHold, CaseId) &&
          !Level2CampaignRules.ApplyFollowThrough(responsible).Changed && Snapshot(responsible) == responsibleDone,
            "choice and follow-through retries cannot replace a committed choice or pay either claim twice");

        PlayerProgressData promise = Fresh("promise", 40);
        C(Commit(promise, Level2EthicsChoice.UnsupportedPromise) && promise.campaign.publicTrust == 43 &&
          promise.campaign.rosaEthics.choiceText == promiseText && promise.campaign.rosaEthics.promiseTrustDelta == 3 &&
          promise.campaign.rosaEthics.followThroughRequired && EthicsTotal(promise) == 3,
            "unsupported promise commits +3 and persists the amount that must later be reversed");
        CompleteApprovedCase(promise);
        C(promise.campaign.publicTrust == 38 && EthicsTotal(promise) == -8 &&
          EthicsEntries(promise).Select(e => e.amount).OrderBy(n => n).SequenceEqual(new[] { -8, -3, 3 }) &&
          ServiceTrust(promise) == 6 &&
          promise.campaign.rosaEthics.followThroughDone,
            "unsupported promise reverses its +3, then applies −8; net ethics is −8 beside T18 service trust");
        string promiseDone = Snapshot(promise);
        C(!Level2CampaignRules.CommitEthicsChoice(promise, Level2EthicsChoice.UnsupportedPromise, CaseId) &&
          !Level2CampaignRules.ApplyFollowThrough(promise).Changed && Snapshot(promise) == promiseDone,
            "an unsupported promise and its reversal remain once-only after the case follows through");

        PlayerProgressData dismissive = Fresh("dismissive", 40);
        C(Commit(dismissive, Level2EthicsChoice.DismissiveHold) && dismissive.campaign.publicTrust == 37 &&
          dismissive.campaign.rosaEthics.choiceText == dismissiveText &&
          !dismissive.campaign.rosaEthics.followThroughRequired && EthicsTotal(dismissive) == -3,
            "dismissive hold commits −3 and requests no T19 follow-through bonus");
        CompleteApprovedCase(dismissive);
        C(dismissive.campaign.publicTrust == 43 && EthicsTotal(dismissive) == -3 &&
          ServiceTrust(dismissive) == 6 && dismissive.campaign.rosaEthics.followThroughDone,
            "dismissive hold gets no ethics bonus while required T18 service trust still arrives independently");

        var cappedHigh = Fresh("cap-high", 100);
        CampaignRewards.Change highLoss = CampaignRewards.ApplyEthicsDelta(cappedHigh, "rosa-ethics/cap-high/loss", -1, "test");
        CampaignRewards.Change highGain = CampaignRewards.ApplyEthicsDelta(cappedHigh, "rosa-ethics/cap-high/gain", 3, "test");
        C(highLoss.Applied && highLoss.Amount == -1 && highGain.Applied && highGain.Amount == 1 &&
          cappedHigh.campaign.publicTrust == CampaignRecord.MaxTrust,
            "authored trust changes clamp at 100 and report the actual applied amount");
        var cappedLow = Fresh("cap-low", 1);
        CampaignRewards.Change lowLoss = CampaignRewards.ApplyEthicsDelta(cappedLow, "rosa-ethics/cap-low/loss", -8, "test");
        string lowBeforeRepeat = Snapshot(cappedLow);
        CampaignRewards.Change duplicate = CampaignRewards.ApplyEthicsDelta(cappedLow, "rosa-ethics/cap-low/loss", -8, "test");
        C(lowLoss.Applied && lowLoss.Amount == -1 && cappedLow.campaign.publicTrust == 0 &&
          duplicate.Outcome == CampaignRewards.Outcome.AlreadyClaimed && Snapshot(cappedLow) == lowBeforeRepeat &&
          !CampaignRewards.ApplyEthicsDelta(cappedLow, "unscoped", -1, "test").Applied &&
          !CampaignRewards.ApplyEthicsDelta(cappedLow, "rosa-ethics/cap-low/too-large", -9, "test").Applied,
            "authored changes clamp at 0, reject unscoped deltas, and stable claims make repeats no-ops");
        PlayerProgressData partialPromise = Fresh("partial-promise", 99);
        bool partialCommitted = Commit(partialPromise, Level2EthicsChoice.UnsupportedPromise);
        int partialPromiseGain = partialPromise.campaign.rosaEthics.promiseTrustDelta;
        CompleteApprovedCase(partialPromise);
        C(partialCommitted && partialPromiseGain == 1 && partialPromise.campaign.publicTrust == 91 &&
          EthicsTotal(partialPromise) == -8 &&
          EthicsEntries(partialPromise).Select(e => e.amount).OrderBy(n => n).SequenceEqual(new[] { -8, -1, 1 }),
            "a clipped promise stores and reverses its actual +1 before the full −8 consequence");
        PlayerProgressData zeroPromise = Fresh("zero-promise", CampaignRecord.MaxTrust);
        bool zeroCommitted = Commit(zeroPromise, Level2EthicsChoice.UnsupportedPromise);
        CompleteApprovedCase(zeroPromise);
        C(zeroCommitted && zeroPromise.campaign.rosaEthics.promiseTrustDelta == 0 && zeroPromise.campaign.publicTrust == 92 &&
          EthicsTotal(zeroPromise) == -8,
            "a promise clipped to +0 reverses +0, then still applies the full −8 consequence");

        // Use the real atomic signed store to verify that failed choice writes
        // leave the previous campaign/checkpoint visible and a retry is durable.
        string saveDirectory = Path.Combine(Path.GetTempPath(), "TaxVenture-T19-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(saveDirectory);
        try
        {
            var store = new SaveFileStore(Path.Combine(saveDirectory, "progress.json"));
            PlayerProgressData beforeChoice = Fresh("save-choice", 40);
            PutCheckpointSnapshot(beforeChoice, Level2EthicsChoice.None, false, false);
            store.Write(EncodeSigned(beforeChoice));

            PlayerProgressData prepared = Clone(ReadSigned(store));
            bool preparedChoice = Commit(prepared, Level2EthicsChoice.UnsupportedPromise);
            PutCheckpointSnapshot(prepared, Level2EthicsChoice.UnsupportedPromise, true, false);
            bool choiceSaveFailed = TryBlockedWrite(store, prepared);
            PlayerProgressData afterFailedChoice = ReadSigned(store);
            bool oldStateKept = !afterFailedChoice.campaign.rosaEthics.committed &&
                afterFailedChoice.campaign.publicTrust == 40 &&
                ReadCheckpoint(afterFailedChoice).GetProperty("rosaEthicsCommitted").GetBoolean() == false;
            C(preparedChoice && choiceSaveFailed && oldStateKept,
                "a failed pre-choice write leaves the signed prior checkpoint and trust unchanged");

            PlayerProgressData retryChoice = Clone(ReadSigned(store));
            bool retried = Commit(retryChoice, Level2EthicsChoice.UnsupportedPromise);
            PutCheckpointSnapshot(retryChoice, Level2EthicsChoice.UnsupportedPromise, true, false);
            store.Write(EncodeSigned(retryChoice));
            PlayerProgressData reloadedChoice = ReadSigned(store);
            string choiceClaim = CampaignRewards.ClaimId(reloadedChoice.campaign.campaignId, Level2CampaignRules.LevelId,
                "rosa-ethics/" + CaseId + "/unsupported-promise", CampaignRewards.TrustKind);
            bool snapshotChoice = ReadCheckpoint(reloadedChoice).GetProperty("rosaEthicsChoiceText").GetString() == promiseText &&
                ReadCheckpoint(reloadedChoice).GetProperty("rosaEthicsCommitted").GetBoolean();
            C(retried && snapshotChoice && reloadedChoice.campaign.rosaEthics.committed &&
              reloadedChoice.campaign.rosaEthics.choiceId == "rosa-ethics/" + CaseId + "/unsupported-promise" &&
              reloadedChoice.campaign.rosaEthics.commitmentId == Level2CampaignRules.CommitmentId(CaseId) &&
              reloadedChoice.campaign.rosaEthics.choiceText == promiseText &&
              reloadedChoice.campaign.rosaEthics.consequenceIds.Contains(choiceClaim) &&
              reloadedChoice.campaign.publicTrust == 43,
                "retry saves the choice ID, commitment ID/text, consequence claim, checkpoint snapshot, and trust in a signed envelope");
            string committedChoice = Snapshot(reloadedChoice);
            C(!CommitEthicsChoice(reloadedChoice, Level2EthicsChoice.UnsupportedPromise) &&
              Snapshot(reloadedChoice) == committedChoice,
                "a signed reload cannot commit the same choice or trust consequence twice");

            // Persist the approved result while its required ethical consequence is pending.
            PlayerProgressData approvedPending = Clone(reloadedChoice);
            Level2CampaignRules.ApplyCaseResult(approvedPending, ApprovedFacts());
            PutCheckpointSnapshot(approvedPending, Level2EthicsChoice.UnsupportedPromise, true, false, approved: true, copyAtAuditor: true);
            store.Write(EncodeSigned(approvedPending));
            PlayerProgressData approvedReload = ReadSigned(store);
            int pendingTrust = approvedReload.campaign.publicTrust;
            bool pendingWasSaved = approvedReload.campaign.rosaEthics.followThroughRequired &&
                !approvedReload.campaign.rosaEthics.followThroughDone && Level2CampaignRules.CaseClaimed(approvedReload) &&
                ReadCheckpoint(approvedReload).GetProperty("clientCopyPhase").GetString() == "AtAuditor" &&
                ReadCheckpoint(approvedReload).GetProperty("audit").GetProperty("approved").GetBoolean();

            PlayerProgressData preparedConsequence = Clone(approvedReload);
            bool consequenceApplied = Level2CampaignRules.ApplyFollowThrough(preparedConsequence).Changed;
            PutCheckpointSnapshot(preparedConsequence, Level2EthicsChoice.UnsupportedPromise, true, true, approved: true, copyAtAuditor: true);
            preparedConsequence.ClearCheckpoint(Level2CampaignRules.LevelId);
            bool consequenceSaveFailed = TryBlockedWrite(store, preparedConsequence);
            PlayerProgressData afterFailedConsequence = ReadSigned(store);
            bool pendingSurvived = !afterFailedConsequence.campaign.rosaEthics.followThroughDone &&
                afterFailedConsequence.campaign.publicTrust == pendingTrust &&
                ReadCheckpoint(afterFailedConsequence).GetProperty("rosaEthicsFollowThroughDone").GetBoolean() == false;
            C(pendingWasSaved && consequenceApplied && consequenceSaveFailed && pendingSurvived,
                "a failed post-approval write retains the pending reversal and the prior trust in the signed save");

            // Retry from the signed pending state, as a relaunch would.
            PlayerProgressData retryConsequence = Clone(ReadSigned(store));
            Level2CampaignRules.Grants retriedConsequence = Level2CampaignRules.ApplyFollowThrough(retryConsequence);
            retryConsequence.ClearCheckpoint(Level2CampaignRules.LevelId);
            store.Write(EncodeSigned(retryConsequence));
            PlayerProgressData reloadedConsequence = ReadSigned(store);
            int resolvedTrust = reloadedConsequence.campaign.publicTrust;
            string resolvedState = Snapshot(reloadedConsequence);
            bool retriedAgain = Level2CampaignRules.ApplyFollowThrough(reloadedConsequence).Changed;
            C(retriedConsequence.Changed && reloadedConsequence.campaign.rosaEthics.followThroughDone &&
              resolvedTrust == 38 && reloadedConsequence.CheckpointFor(Level2CampaignRules.LevelId) == null &&
              !retriedAgain && Snapshot(reloadedConsequence) == resolvedState &&
              EthicsTotal(reloadedConsequence) == -8 && ServiceTrust(reloadedConsequence) == 6,
                "reload retries the pending reversal, saves −8 ethics once beside T18 service trust, then becomes a no-op");
        }
        finally
        {
            if (Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, true);
        }

        // Static wiring/content contracts keep ordinary errors and deadlines out
        // of ethics, and pin the authored UI copy to the existing Rosa flow.
        string scripts = Path.Combine(root, "Assets", "Scripts");
        string consultation = File.ReadAllText(Path.Combine(scripts, "Level2", "Level2RosaConsultation.cs"));
        string notePage = File.ReadAllText(Path.Combine(scripts, "Level2", "Level2FindingsPage.cs"));
        string toolkit = File.ReadAllText(Path.Combine(scripts, "Level2", "IncomeToolkit.cs"));
        string rosaActor = File.ReadAllText(Path.Combine(scripts, "Level2", "RosaInteractable.cs"));
        string audit = File.ReadAllText(Path.Combine(scripts, "Level2", "Level2Audit.cs"));
        string clock = File.ReadAllText(Path.Combine(scripts, "Level2", "Level2ReviewClock.cs"));
        string campaignRules = File.ReadAllText(Path.Combine(scripts, "RewardsSystem", "Level2CampaignRules.cs"));
        string progressManager = File.ReadAllText(Path.Combine(scripts, "RewardsSystem", "PlayerProgressManager.cs"));
        string checkpointer = File.ReadAllText(Path.Combine(scripts, "Level2", "Level2Checkpointer.cs"));
        C(campaignRules.Contains(responsibleText) && campaignRules.Contains(promiseText) && campaignRules.Contains(dismissiveText),
            "production choice copy matches the three regression fixtures");
        C(consultation.Contains("unresolved") && consultation.Contains("unsupported") &&
          RegexHasSeparateConfirm(consultation),
            "the unsupported promise names the unresolved income and requires a separate confirmation action");
        C(toolkit.Contains("EthicsChoiceCommitted") && toolkit.Contains("!Challenge.firstChecked") && toolkit.Contains("IncomeChallenge.Missing(Challenge, options).Count == 0") &&
          notePage.Contains("EthicsStatus") && campaignRules.Contains("PROMISE REVISED — SUPPORT CHECKED") &&
          campaignRules.Contains("RECORDS CHECKED."),
            "the choice gates the first complete check and the existing ruled note displays both required final statuses");
        C(consultation.Contains("TryStartVisit") && rosaActor.Contains("NpcLife") && rosaActor.Contains("Life") &&
          !consultation.Contains("Instantiate(") && !consultation.Contains("AddComponent<NpcLife>"),
            "Rosa keeps the existing dialogue and NpcLife movement owner without a second actor controller");
        C(!audit.Contains("Ethics") && !audit.Contains("rosa-ethics/") &&
          !clock.Contains("Ethics") && !clock.Contains("rosa-ethics/"),
            "ordinary audit mistakes and deadline handlers do not route into ethics penalties");
        string choiceCommit = Between(progressManager, "public bool CommitLevelTwoEthicsChoice(",
            "public StoryGrants CommitLevelTwoCheckpoint(");
        C(Index(choiceCommit, "TryWriteCandidate(candidate)") >= 0 &&
          Index(choiceCommit, "TryWriteCandidate(candidate)") < Index(choiceCommit, "PublishEthicsChoice(") &&
          choiceCommit.Contains("CaptureEthicsChoice(choice, caseId)"),
            "the live choice and note are published only after the checkpoint and campaign candidate save");
        string resultCommit = Between(progressManager, "public bool CommitLevelTwoResult(LevelTotalResult levelTotal, out bool wroteNow)",
            "public StoryGrants CommitLevelTwoFollowThrough()");
        C(resultCommit.Contains("CaptureApprovedClientCopy()") &&
          Index(resultCommit, "candidate.SetCheckpoint(approvedCopy)") >= 0 &&
          Index(resultCommit, "candidate.SetCheckpoint(approvedCopy)") < Index(resultCommit, "TryWriteCandidate(candidate)") &&
          resultCommit.Contains("bool copyAlreadyDelivered = Level2CampaignRules.FollowThroughDone(progress)") &&
          resultCommit.Contains("if (facts.Approved && copyAlreadyDelivered) candidate.ClearCheckpoint(levelTotal.LevelId)") &&
          checkpointer.Contains("SyncEthicsReview") && checkpointer.Contains("CaptureApprovedClientCopy()"),
            "approval saves the approved Client Copy checkpoint and signed ethics state before publishing the case result");
        string approvedCopyCapture = Between(checkpointer, "public LevelCheckpoint CaptureApprovedClientCopy()", "public bool CommitLocalEthicsChoice(");
        C(approvedCopyCapture.Contains("savedPhase == ClientCopyPhase.Carried") &&
          approvedCopyCapture.Contains("ClientCopyPhase.Carried : ClientCopyPhase.AtAuditor"),
            "a resumed already-carried Client Copy keeps its phase; a new approved case starts beside the Auditor");

        C(consultation.Contains("int choiceIndex = i;") && consultation.Contains("actions[choiceIndex]") &&
          consultation.Contains("Time.unscaledTime < promptReadyAt") && consultation.Contains("CancelDialogueFor(Rosa)"),
            "option callbacks capture their own action, reject rapid repeat gestures, and can leave without committing");
        string dialogueDock = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "UI", "DialogueUI.cs"));
        C(consultation.Contains("ethicsDialogue.DockChoices(ethicsChoicePanel)") && dialogueDock.Contains("Screen.safeArea.min") &&
          dialogueDock.Contains("Screen.safeArea.max") && dialogueDock.Contains("group.interactable = group.blocksRaycasts = false") &&
          consultation.Contains("ethicsDialogue.ReleaseChoices(ethicsChoicePanel)"),
            "choice panels fit the canvas and stop receiving taps immediately when dismissed");
        C(notePage.Contains("Neutral ruled Rosa review note") && notePage.Contains("RECORDS CHECKED tab") &&
          notePage.Contains("elapsed < 0.45f") && !notePage.Contains("COMMITMENT ID:"),
            "the folder owns a neutral ruled note and checked tab without exposing internal commitment IDs");
        C(consultation.Contains("WaitForSecondsRealtime(0.4f)") && consultation.Contains("StartCoroutine(PlayEthicsResponseCue())"),
            "the restrained response cue is a skippable presentation beat after commitment");
        return count;
    }

    private static bool RegexHasSeparateConfirm(string source) =>
        source.Contains("ConfirmUnsupported") || source.Contains("UnsupportedConfirmed") ||
        source.Contains("ConfirmPromise") || source.Contains("AwaitAction") &&
        source.Contains("CommitEthicsChoice") && source.IndexOf("Confirm", StringComparison.OrdinalIgnoreCase) >= 0;

    private static PlayerProgressData Fresh(string id, int trust)
    {
        PlayerProgressData data = PlayerProgressData.CreateNewCampaign("t19-" + id, "2026-10-29T00:00:00Z");
        data.campaign.publicTrust = trust;
        data.SetCheckpoint(new LevelCheckpoint { levelId = Level2CampaignRules.LevelId, caseId = CaseId, reviewJson = "{}" });
        return data;
    }

    private static bool Commit(PlayerProgressData data, Level2EthicsChoice choice) =>
        Level2CampaignRules.CommitEthicsChoice(data, choice, CaseId);

    private static bool CommitEthicsChoice(PlayerProgressData data, Level2EthicsChoice choice) =>
        Level2CampaignRules.CommitEthicsChoice(data, choice, CaseId);

    private static Level2CaseFacts ApprovedFacts() => new Level2CaseFacts
    {
        HasReview = true, CaseId = CaseId, QuestCorrected = true, Approved = true
    };

    private static void CompleteApprovedCase(PlayerProgressData data)
    {
        Level2CampaignRules.ApplyCaseResult(data, ApprovedFacts());
        Level2CampaignRules.ApplyFollowThrough(data);
    }

    private static int EthicsTotal(PlayerProgressData data) => EthicsEntries(data).Sum(e => e.amount);

    private static List<CampaignLedgerEntry> EthicsEntries(PlayerProgressData data) =>
        data.campaign.ledger.Where(e => e != null && e.kind == CampaignRewards.TrustKind &&
            e.claimId.Contains("/" + Level2CampaignRules.LevelId + "/rosa-ethics/" + CaseId + "/", StringComparison.Ordinal)).ToList();

    private static int ServiceTrust(PlayerProgressData data) => data.campaign.ledger
        .Where(e => e != null && e.kind == CampaignRewards.TrustKind &&
            (e.claimId.Contains("/" + CampaignRewards.IncomeReview + "/", StringComparison.Ordinal) ||
             e.claimId.Contains("/" + CampaignRewards.RosaFollowThrough + "/", StringComparison.Ordinal)))
        .Sum(e => e.amount);

    private static void PutCheckpointSnapshot(PlayerProgressData data, Level2EthicsChoice choice, bool committed, bool complete,
        bool approved = false, bool copyAtAuditor = false)
    {
        string text = choice == Level2EthicsChoice.Responsible ? Level2CampaignRules.ResponsibleChoiceText :
            choice == Level2EthicsChoice.UnsupportedPromise ? Level2CampaignRules.UnsupportedChoiceText :
            choice == Level2EthicsChoice.DismissiveHold ? Level2CampaignRules.DismissiveChoiceText : "";
        var snapshot = new Dictionary<string, object>
        {
            ["rosaEthicsCaseId"] = committed ? CaseId : "",
            ["rosaEthicsChoiceId"] = committed ? "rosa-ethics/" + CaseId + "/" +
                (choice == Level2EthicsChoice.Responsible ? "responsible" : choice == Level2EthicsChoice.UnsupportedPromise ? "unsupported-promise" : "dismissive-hold") : "",
            ["rosaEthicsChoiceText"] = committed ? text : "",
            ["rosaEthicsCommitted"] = committed,
            ["rosaEthicsFollowThroughDone"] = complete,
            ["phase"] = approved ? "Completed" : "Challenge",
            ["audit"] = new Dictionary<string, object> { ["approved"] = approved },
            ["clientCopyPhase"] = copyAtAuditor ? "AtAuditor" : ""
        };
        LevelCheckpoint checkpoint = data.CheckpointFor(Level2CampaignRules.LevelId) ??
            new LevelCheckpoint { levelId = Level2CampaignRules.LevelId, caseId = CaseId };
        checkpoint.reviewJson = JsonSerializer.Serialize(snapshot, Json);
        data.SetCheckpoint(checkpoint);
    }

    private static JsonElement ReadCheckpoint(PlayerProgressData data)
    {
        using JsonDocument doc = JsonDocument.Parse(data.CheckpointFor(Level2CampaignRules.LevelId).reviewJson);
        return doc.RootElement.Clone();
    }

    private static bool TryBlockedWrite(SaveFileStore store, PlayerProgressData data)
    {
        if (Directory.Exists(store.TempPath)) Directory.Delete(store.TempPath, true);
        Directory.CreateDirectory(store.TempPath);
        try
        {
            store.Write(EncodeSigned(data));
            return false;
        }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
        finally
        {
            if (Directory.Exists(store.TempPath)) Directory.Delete(store.TempPath, true);
        }
    }

    private static string EncodeSigned(PlayerProgressData data)
    {
        data.Normalize();
        string payload = JsonSerializer.Serialize(data, Json);
        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["format"] = 1,
            ["payload"] = payload,
            ["signature"] = SaveFileStore.Sign(payload)
        });
    }

    private static PlayerProgressData ReadSigned(SaveFileStore store)
    {
        string text = store.Read(value => DecodeSigned(value) != null, out _, out string problem);
        if (text == null) throw new InvalidOperationException("T19 signed fixture could not be read: " + problem);
        return DecodeSigned(text);
    }

    private static PlayerProgressData DecodeSigned(string text)
    {
        try
        {
            using JsonDocument envelope = JsonDocument.Parse(text);
            string payload = envelope.RootElement.GetProperty("payload").GetString();
            if (!SaveFileStore.SignatureMatches(payload, envelope.RootElement.GetProperty("signature").GetString())) return null;
            PlayerProgressData data = JsonSerializer.Deserialize<PlayerProgressData>(payload, Json);
            data?.Normalize();
            return data;
        }
        catch { return null; }
    }

    private static PlayerProgressData Clone(PlayerProgressData data) =>
        JsonSerializer.Deserialize<PlayerProgressData>(Snapshot(data), Json);

    private static string Snapshot(PlayerProgressData data) => JsonSerializer.Serialize(data, Json);

    private static int Index(string source, string value) => source.IndexOf(value, StringComparison.Ordinal);

    private static string Between(string source, string start, string end)
    {
        int from = Index(source, start), to = Index(source, end);
        return from >= 0 && to > from ? source.Substring(from, to - from) : "";
    }
}
