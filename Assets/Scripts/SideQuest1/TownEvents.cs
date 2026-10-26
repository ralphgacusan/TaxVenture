using System;

/// <summary>
/// PURPOSE:
/// Static event hub for the Level 1 town intro sequence, mirroring the
/// pattern your existing GameplayEvents class likely already uses for the
/// HUD icons. Kept separate so this milestone doesn't require editing a
/// file I can't see.
///
/// If you already have a GameplayEvents static class, feel free to move
/// these events into it instead — nothing here depends on this specific
/// class name, only on the event signatures.
/// </summary>
public static class TownEvents
{
    /// <summary>Fired every time a single taxpayer profile is collected.</summary>
    public static event Action<string> OnProfileCollected;

    /// <summary>Fired once, the moment the 4th profile is collected.</summary>
    public static event Action OnAllProfilesCollected;

    /// <summary>Fired when the tutorial NPC becomes visible/clickable.</summary>
    public static event Action OnTutorialNpcReady;

    /// <summary>Fired once, when all 4 corkboard corners are correct.</summary>
    public static event Action OnSideQuestCompleted;

    /// <summary>
    /// Fired when the tutorial guide's intro conversation has opened the
    /// corkboard side quest. Notification only (HUD objectives listen).
    /// </summary>
    public static event Action OnTutorialIntroCompleted;

    /// <summary>T30: Arthur's opening talk completed; his reviewed file is presented beside him.</summary>
    public static event Action OnArthurMet;

    /// <summary>T30: both records of his file were shown and put back and page one seen again; the four clients are open.</summary>
    public static event Action OnReviewedFileLookedOver;

    public static void RaiseArthurMet() =>
        OnArthurMet?.Invoke();

    public static void RaiseReviewedFileLookedOver() =>
        OnReviewedFileLookedOver?.Invoke();

    public static void RaiseProfileCollected(string profileId) =>
        OnProfileCollected?.Invoke(profileId);

    public static void RaiseAllProfilesCollected() =>
        OnAllProfilesCollected?.Invoke();

    public static void RaiseTutorialNpcReady() =>
        OnTutorialNpcReady?.Invoke();

    public static void RaiseSideQuestCompleted() =>
        OnSideQuestCompleted?.Invoke();

    public static void RaiseTutorialIntroCompleted() =>
        OnTutorialIntroCompleted?.Invoke();
}
