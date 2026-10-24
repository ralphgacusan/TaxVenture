/// <summary>
/// PURPOSE:
/// Whether hearts on screen mean Public Trust (Campaign Foundation T08): in a
/// committed campaign every UI heart shows Public Trust, and the level's
/// score hearts stay mastery data (saved, never drawn as hearts). Development
/// and old saves keep the original score-hearts presentation. Read-only.
/// </summary>
public static class TrustDisplay
{
    /// <summary>A committed campaign is loaded: hearts are Public Trust.</summary>
    public static bool Active
    {
        get
        {
            PlayerProgressManager manager = PlayerProgressManager.Instance;
            return manager != null && manager.CurrentProgress != null && manager.CurrentProgress.HasCampaign &&
                   CampaignSave.CanContinue(manager.CampaignState);
        }
    }

    /// <summary>The saved Public Trust (0 when there is no campaign).</summary>
    public static int PublicTrust => Active ? PlayerProgressManager.Instance.CurrentProgress.campaign.publicTrust : 0;
}
