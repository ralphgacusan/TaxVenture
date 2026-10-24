/// <summary>Where Marie's approved Client Copy is (Campaign Foundation T08).</summary>
public enum ClientCopyPhase
{
    /// <summary>No recorded case result (or not a story campaign): no copy exists.</summary>
    None,
    /// <summary>The case result is saved; the copy floats beside the Auditor.</summary>
    AtAuditor,
    /// <summary>The copy is in the folder icon; Marie waits for it.</summary>
    Carried,
    /// <summary>Marie has her copy: the follow-through is committed.</summary>
    Done
}

/// <summary>
/// PURPOSE:
/// The Client Copy handover's phase rules (T08). Pure: no Unity calls.
/// The case result (T07) is committed at approval; then the copy waits
/// beside the Auditor, is carried in the folder icon, and is handed to Marie.
/// Only handing it over commits anything (PlayerProgressManager.
/// CommitFollowThrough: trust +4, claimed once); picking it up or reading it
/// changes no campaign value. The saved phase lives in the Level 1 field
/// checkpoint (Level1FieldCheckpoint.handoverPhase); the follow-through
/// activity says whether it is done.
/// </summary>
public static class ClientCopyRules
{
    /// <summary>The awaited dialogue action of the handover line (dragging or tapping the copy).</summary>
    public const string HandoverAction = "client-copy";

    public static ClientCopyPhase Parse(string saved)
    {
        string phase = saved == null ? null : saved.Trim();
        if (phase == Level1FieldCheckpoint.HandoverDone) return ClientCopyPhase.Done;
        if (phase == Level1FieldCheckpoint.HandoverCarried) return ClientCopyPhase.Carried;
        if (phase == Level1FieldCheckpoint.HandoverAtAuditor) return ClientCopyPhase.AtAuditor;
        return ClientCopyPhase.None;
    }

    /// <summary>The checkpoint value of a phase (None and Done keep nothing).</summary>
    public static string ToSaved(ClientCopyPhase phase)
    {
        switch (phase)
        {
            case ClientCopyPhase.AtAuditor: return Level1FieldCheckpoint.HandoverAtAuditor;
            case ClientCopyPhase.Carried: return Level1FieldCheckpoint.HandoverCarried;
            case ClientCopyPhase.Done: return Level1FieldCheckpoint.HandoverDone;
            default: return Level1FieldCheckpoint.HandoverNone;
        }
    }

    /// <summary>
    /// The phase a save describes: Done once the follow-through activity is
    /// completed; None without a recorded case result; otherwise the saved
    /// checkpoint phase, the copy beside the Auditor when none (or an unknown
    /// one) was saved.
    /// </summary>
    public static ClientCopyPhase PhaseOf(PlayerProgressData data)
    {
        if (data == null || !data.HasCampaign) return ClientCopyPhase.None;
        if (CampaignRewards.IsActivityCompleted(data, CampaignRewards.Level1, CampaignRewards.FollowThrough)) return ClientCopyPhase.Done;
        if (!CaseResultRules.FollowThroughPending(data)) return ClientCopyPhase.None;
        Level1FieldCheckpoint checkpoint = data.campaign.levelOne;
        return checkpoint != null && Parse(checkpoint.handoverPhase) == ClientCopyPhase.Carried
            ? ClientCopyPhase.Carried : ClientCopyPhase.AtAuditor;
    }

    /// <summary>Tapping the floating copy: only from beside the Auditor, once.</summary>
    public static bool TryPickUp(ref ClientCopyPhase phase)
    {
        if (phase != ClientCopyPhase.AtAuditor) return false;
        phase = ClientCopyPhase.Carried;
        return true;
    }

    /// <summary>The handover can be attempted only while the copy is carried.</summary>
    public static bool CanHandOver(ClientCopyPhase phase) => phase == ClientCopyPhase.Carried;

    /// <summary>After a handover attempt: Done only when the follow-through was saved; a failed write keeps the copy carried.</summary>
    public static ClientCopyPhase AfterHandover(ClientCopyPhase phase, bool saved) =>
        saved && phase == ClientCopyPhase.Carried ? ClientCopyPhase.Done : phase;

    /// <summary>While the copy exists the case stays approved: a resume never restores the case to "case received".</summary>
    public static bool RestoresCaseWork(ClientCopyPhase phase) => phase == ClientCopyPhase.None;
}
