using System;

/// <summary>What the save on this device means for the updated story (read once at load).</summary>
public enum CampaignSaveState
{
    /// <summary>No save file: a fresh install. New Game only.</summary>
    NoCampaign = 0,
    /// <summary>A committed version-5 campaign: Continue resumes it.</summary>
    Campaign = 1,
    /// <summary>A valid save from before the campaign: nothing is imported; the player must start the updated story.</summary>
    RequiresNewStory = 2,
    /// <summary>A valid save written by a newer game version: kept untouched, never overwritten by ordinary saving.</summary>
    UnsupportedFuture = 3,
    /// <summary>Save files exist but neither the main file nor the backup could be used (they were kept aside).</summary>
    Unreadable = 4
}

/// <summary>
/// PURPOSE:
/// The campaign save boundary (Campaign Foundation T02), pure C# so the
/// regression suite runs it on disposable files.
///
/// - Classify: decides the CampaignSaveState from the decoded save BEFORE
///   Normalize, so an old save is recognized as old (it is never turned into
///   a campaign by loading). Detection only picks the menu message and keeps
///   Continue off; it never reads old values into the campaign.
/// - TryCommit: a confirmed New Game encodes the complete candidate and
///   writes it with the store's reset (main file and backup replaced). It
///   returns false on any failure, so the caller publishes the candidate only
///   after it is durably saved and otherwise keeps its previous state.
///
/// PlayerProgressManager owns the files and the live progress; T03 adds the
/// New Game / Continue menu actions on top of this.
/// </summary>
public static class CampaignSave
{
    public const string StartUpdatedStoryNotice = "Start the updated story";
    public const string NoStoryNotice = "No story to continue";
    public const string FutureVersionNotice = "This save is from a newer version of TaxVenture";
    public const string UnreadableNotice = "Your save could not be read";

    /// <summary>
    /// The state of a loaded save. saveExists: a main or backup file is on disk;
    /// decoded: the save that passed the signature check (null when none did).
    /// Call before Normalize.
    /// </summary>
    public static CampaignSaveState Classify(bool saveExists, PlayerProgressData decoded)
    {
        if (!saveExists) return CampaignSaveState.NoCampaign;
        if (decoded == null) return CampaignSaveState.Unreadable;
        if (decoded.saveVersion > PlayerProgressData.CurrentSaveVersion) return CampaignSaveState.UnsupportedFuture;
        if (decoded.saveVersion == PlayerProgressData.CurrentSaveVersion && decoded.HasCampaign) return CampaignSaveState.Campaign;
        return CampaignSaveState.RequiresNewStory;
    }

    /// <summary>Only a committed campaign can be continued.</summary>
    public static bool CanContinue(CampaignSaveState state) => state == CampaignSaveState.Campaign;

    /// <summary>Ordinary saving is allowed except over a newer game's save.</summary>
    public static bool AllowsOrdinarySave(CampaignSaveState state) => state != CampaignSaveState.UnsupportedFuture;

    /// <summary>Short menu explanation for why Continue is unavailable ("" when it is available).</summary>
    public static string ContinueNotice(CampaignSaveState state)
    {
        switch (state)
        {
            case CampaignSaveState.Campaign: return "";
            case CampaignSaveState.RequiresNewStory: return StartUpdatedStoryNotice;
            case CampaignSaveState.UnsupportedFuture: return FutureVersionNotice;
            case CampaignSaveState.Unreadable: return UnreadableNotice;
            default: return NoStoryNotice;
        }
    }

    /// <summary>
    /// Writes a confirmed New Game candidate. encode turns it into the signed
    /// file text; replaceAll writes it as the main save and the backup;
    /// restorePrevious puts the earlier files back when replaceAll fails part
    /// way. On failure nothing is published and the error says why (retry is safe).
    /// </summary>
    public static bool TryCommit(PlayerProgressData candidate, Func<PlayerProgressData, string> encode,
        Action<string> replaceAll, Action restorePrevious, out string error)
    {
        error = null;
        if (candidate == null || !candidate.HasCampaign || candidate.saveVersion != PlayerProgressData.CurrentSaveVersion)
        {
            error = "The new campaign is incomplete.";
            return false;
        }
        if (encode == null || replaceAll == null)
        {
            error = "The save is not ready.";
            return false;
        }

        string text;
        try
        {
            text = encode(candidate);
            if (string.IsNullOrEmpty(text)) throw new InvalidOperationException("the new campaign could not be encoded");
        }
        catch (Exception e)
        {
            error = "Could not prepare the new story (" + e.Message + ").";
            return false;
        }

        try
        {
            replaceAll(text);
        }
        catch (Exception e)
        {
            error = "Could not save the new story (" + e.Message + ").";
            try
            {
                restorePrevious?.Invoke();
            }
            catch (Exception restoreError)
            {
                error += " The previous save files could not be put back (" + restoreError.Message + ").";
            }
            return false;
        }
        return true;
    }
}
