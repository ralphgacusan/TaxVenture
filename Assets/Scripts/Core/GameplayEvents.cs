
using System;

/// <summary>
/// PURPOSE:
/// Lightweight, static event hub for gameplay milestones that need to
/// broadcast "this happened" to anything interested — currently the HUD's
/// icon unlock states, but intentionally generic so future systems
/// (tutorial hints, achievements) can subscribe without new plumbing.
///
/// WHY STATIC, NOT A MONOBEHAVIOUR MANAGER:
/// These are simple one-shot signals with no state of their own to own or
/// tick — a manager GameObject would add nothing but Inspector clutter.
/// This follows the same "event, not tight reference" principle already
/// used by GameStateMachine.OnStateChanged and NpcStateMachine.OnStateChanged.
///
/// CONNECTS WITH:
/// - CaseFolderInteractable: raises CaseFolderOpened whenever the current
///   Case Folder is opened
/// - TaxCodeBookInteractable: raises TaxCodeBookFirstOpened
/// - Receptionist dialogue conclusion: raises NotesUnlockRequested
/// - CaseProgressionManager / CaseCompleteState: raises CaseCompleted
/// - HudIconButton: subscribes to the relevant event for its own icon
/// </summary>
public static class GameplayEvents
{
    // Raised whenever the Case Folder for the current case is opened.
    // This can happen once per case.
    public static event Action OnCaseFolderOpened;

    public static event Action OnTaxCodeBookFirstOpened;
    public static event Action OnNotesUnlockRequested;
    public static event Action OnTaxReturnCollected;

    // Raised whenever the current case is fully completed.
    public static event Action OnCaseCompleted;


    public static void RaiseCaseFolderOpened()
        => OnCaseFolderOpened?.Invoke();

    public static void RaiseTaxCodeBookFirstOpened()
        => OnTaxCodeBookFirstOpened?.Invoke();

    public static void RaiseNotesUnlockRequested()
        => OnNotesUnlockRequested?.Invoke();

    public static void RaiseTaxReturnCollected()
        => OnTaxReturnCollected?.Invoke();

    public static void RaiseCaseCompleted()
        => OnCaseCompleted?.Invoke();
}
