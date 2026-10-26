/// <summary>What the main menu corkboard shows for the current save (built by CampaignMenu.Build).</summary>
public sealed class CampaignMenuModel
{
    /// <summary>A committed version-5 campaign exists; this alone never unlocks the menu.</summary>
    public bool HasStory;

    /// <summary>Level 1 story completion in the last successfully saved campaign.</summary>
    public bool HasCompletedFirstChapter;

    public bool ShowPlay => !HasCompletedFirstChapter;

    /// <summary>Saved Level 1 completion: NEW GAME and CONTINUE side by side in PLAY's row.</summary>
    public bool ShowNewGameAndContinue => HasCompletedFirstChapter;

    /// <summary>The Tutorials note (Training Hub) appears after saved Level 1 completion.</summary>
    public bool ShowTutorials => HasCompletedFirstChapter;

    public string ConfirmTitle;
    public string ConfirmBody;
    public string ConfirmButton;
    public string CancelButton;
}

/// <summary>
/// Pure menu model. Campaign existence is used only to reuse an interrupted
/// story. Returning-menu access requires saved story completion of Level 1,
/// never mastery XP, pass scores, intro history or campaign creation.
/// Runtime callers supply the save owner's last successfully committed gate.
/// </summary>
public static class CampaignMenu
{
    public const string ConfirmTitle = "Start a New Game?";
    public const string ConfirmBody = "This will replace your current story progress, including XP, badges, public trust, coins, " +
                                      "collected items, and purchases. Your settings will stay the same.";
    public const string ConfirmButton = "Start New Game";
    public const string CancelButton = "Cancel";
    public const string NewGameFailed = "Your new game could not be saved. Your current story is unchanged. Tap Start New Game to try again.";
    public const string ShopLockedMessage = "Visit the Supply Counter beside the office in Chapter 1 to open the shop.";

    public static CampaignMenuModel Build(CampaignSaveState state, PlayerProgressData data, bool? savedCompletion = null) => new CampaignMenuModel
    {
        HasStory = HasStory(state, data),
        HasCompletedFirstChapter = HasStory(state, data) &&
            (savedCompletion ?? data.campaign.IsChapterCompleted(CampaignRecord.FirstChapterId)),
        ConfirmTitle = ConfirmTitle,
        ConfirmBody = ConfirmBody,
        ConfirmButton = ConfirmButton,
        CancelButton = CancelButton
    };

    /// <summary>Only the committed campaign counts: never XP, badges, old unlocks, a session flag or a file merely existing.</summary>
    public static bool HasStory(CampaignSaveState state, PlayerProgressData data) =>
        CampaignSave.CanContinue(state) && data != null && data.HasCampaign;

    /// <summary>
    /// The shop is unlocked once it has been introduced in the story: the
    /// first visit to the Supply Counter early in Chapter 1 (T12, user
    /// decision 2026-10-25). The menu catalogue itself arrives with T13.
    /// </summary>
    public static bool ShopUnlocked(PlayerProgressData data) => data != null && data.HasCampaign && data.campaign.shopIntroduced;
}
