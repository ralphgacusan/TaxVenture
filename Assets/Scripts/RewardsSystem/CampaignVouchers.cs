using System;

/// <summary>Why a review voucher cannot be used right now (T14).</summary>
public enum VoucherUseStatus { Ready, NoVoucher, NoCase, NoDeadline, CaseDone, CutoffPassed, DayClosing, AlreadyUsed }

/// <summary>The existing review clock as the voucher rules see it (plain values read from OfficeHours).</summary>
public struct VoucherClockView
{
    public bool HasDeadline;
    public bool Announced;
    public bool CaseDone;
    /// <summary>The cutoff has passed and "next morning" is already under way.</summary>
    public bool DayClosing;
    public double GameSeconds;
    public int DeadlineSeconds;
    public int ExtensionSeconds;
}

/// <summary>The outcome of one voucher use (T14).</summary>
public sealed class VoucherUseResult
{
    public InventoryOutcome Outcome = InventoryOutcome.Rejected;
    public string Message = "";
    public string ClaimId = "";
    /// <summary>Where the used voucher came from (its slot), for the motion.</summary>
    public SlotRef From = SlotRef.None;
}

/// <summary>
/// PURPOSE:
/// Review vouchers (Campaign Foundation T14, pure). A voucher moves the
/// Auditor's cutoff for the active case 180 active review seconds later -
/// at the game clock's own rate, the same minutes the player actually plays
/// - and nothing else: no statutory tax date, no assessment, no missed
/// cutoff, no next-day recovery. One per case (a stable campaign + level +
/// case claim shared across day rollover), carry at most two (the item's
/// carry limit; a third is refused at the counter).
///
/// Use() is one transaction on a candidate campaign: one carried voucher
/// out and the per-case claim in; the caller puts the extended clock in the
/// same candidate (the Level 1 checkpoint) and writes once. Any refusal
/// changes nothing; a repeat finds the claim and changes nothing.
/// </summary>
public static class VoucherRules
{
    /// <summary>Active review seconds one voucher adds (also the most a case can ever get).</summary>
    public const int ExtensionSeconds = Level1ClockRecord.MaxExtensionSeconds;
    public const string UseKind = "voucher";

    public static string UseClaimId(string campaignId, string levelId, string caseId) =>
        CampaignRewards.ClaimId(campaignId, levelId, "case/" + (caseId ?? "").Trim(), UseKind);

    /// <summary>Can a voucher be used on this case now? The case and clock reasons come before the carried count.</summary>
    public static VoucherUseStatus Check(int carried, string caseId, VoucherClockView clock, bool usedOnThisCase)
    {
        if (string.IsNullOrWhiteSpace(caseId)) return VoucherUseStatus.NoCase;
        if (usedOnThisCase || clock.ExtensionSeconds > 0) return VoucherUseStatus.AlreadyUsed;
        if (clock.CaseDone) return VoucherUseStatus.CaseDone;
        if (!clock.HasDeadline || !clock.Announced) return VoucherUseStatus.NoDeadline;
        if (clock.DayClosing) return VoucherUseStatus.DayClosing;
        if (clock.GameSeconds >= clock.DeadlineSeconds) return VoucherUseStatus.CutoffPassed;
        if (carried <= 0) return VoucherUseStatus.NoVoucher;
        return VoucherUseStatus.Ready;
    }

    /// <summary>The player-facing reason ("" when ready).</summary>
    public static string ReasonFor(VoucherUseStatus status)
    {
        switch (status)
        {
            case VoucherUseStatus.NoVoucher: return "Carry a voucher first: buy one at the Supply Counter.";
            case VoucherUseStatus.NoCase: return "Use it during a case review.";
            case VoucherUseStatus.NoDeadline: return "Use it once the Auditor's leaving time is set, during your case review.";
            case VoucherUseStatus.CaseDone: return "This case is already approved.";
            case VoucherUseStatus.CutoffPassed:
            case VoucherUseStatus.DayClosing: return "The Auditor has already left. A voucher can't bring back a missed cutoff.";
            case VoucherUseStatus.AlreadyUsed: return "You've already used a voucher on this case. One per case.";
            default: return "";
        }
    }

    /// <summary>Game seconds one voucher adds to the cutoff at the clock's rate (180 active seconds x game seconds per real second).</summary>
    public static int CutoffShift(float gameSecondsPerRealSecond) =>
        (int)Math.Round(ExtensionSeconds * Math.Max(0.1f, gameSecondsPerRealSecond));

    /// <summary>
    /// The clock after one extension (a copy): the current cutoff moves by
    /// CutoffShift, and so does the first-day cutoff on day 1 (handing in
    /// before the extended cutoff is on time); the extension is recorded.
    /// Time, day, allowance and everything else stay as they are.
    /// </summary>
    public static Level1ClockRecord Extend(Level1ClockRecord clock, float gameSecondsPerRealSecond)
    {
        if (clock == null) return null;
        int shift = CutoffShift(gameSecondsPerRealSecond);
        return new Level1ClockRecord
        {
            isSet = clock.isSet,
            day = clock.day,
            gameSeconds = clock.gameSeconds,
            activeSeconds = clock.activeSeconds,
            sideQuestSeconds = clock.sideQuestSeconds,
            allowance = clock.allowance,
            hasDeadline = clock.hasDeadline,
            announced = clock.announced,
            deadlineSeconds = clock.deadlineSeconds + shift,
            deadlineFirstDay = clock.day <= 1 ? clock.deadlineFirstDay + shift : clock.deadlineFirstDay,
            submittedAt = clock.submittedAt,
            submittedDay = clock.submittedDay,
            onTimeSaid = clock.onTimeSaid,
            missedOnce = clock.missedOnce,
            extensionSeconds = ExtensionSeconds
        };
    }

    /// <summary>One use on a candidate campaign: one carried voucher out, the per-case claim in. Refusals and repeats change nothing.</summary>
    public static VoucherUseResult Use(CampaignRecord candidate, InventoryCatalogue items, string levelId, string caseId, string actionId)
    {
        var result = new VoucherUseResult();
        if (candidate == null || !candidate.IsSet) { result.Message = "No story to use it in."; return result; }
        if (string.IsNullOrWhiteSpace(caseId)) { result.Message = ReasonFor(VoucherUseStatus.NoCase); return result; }
        result.ClaimId = UseClaimId(candidate.campaignId, levelId, caseId);
        if (candidate.rewardClaims.Contains(result.ClaimId))
        {
            result.Outcome = InventoryOutcome.AlreadyApplied;
            result.Message = ReasonFor(VoucherUseStatus.AlreadyUsed);
            return result;
        }
        InventoryResult used = InventoryRules.ConsumeCarried(candidate.inventory, items, InventoryCatalogue.ReviewVoucher, actionId);
        if (used.Outcome != InventoryOutcome.Applied)
        {
            result.Message = ReasonFor(VoucherUseStatus.NoVoucher);
            return result;
        }
        candidate.rewardClaims.Add(result.ClaimId);
        result.From = used.Location;
        result.Outcome = InventoryOutcome.Applied;
        return result;
    }
}
