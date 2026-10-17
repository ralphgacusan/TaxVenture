
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

    // Raised when the desk case folder is stored in the HUD folder icon
    // (count 0 -> 1), and when it comes back out (failed audit / new case).
    // Notification only: the HUD objectives listen.
    public static event Action OnCaseFolderStored;
    public static event Action OnCaseFolderReturned;

    // Raised once when the player takes the case folder out of the filing
    // cabinet (Level 2 retrieval). Notification only: never archives or
    // completes the case.
    public static event Action OnCaseFolderRetrieved;

    // Level 2 initial review: a complete finding was recorded on the folder's
    // Findings page, and the initial Not Ready hold was stamped. Notification only.
    public static event Action OnFindingRecorded;
    public static event Action OnPreliminaryHoldStamped;

    // Level 2 stall: Rosa handed over her records, and the player sat at the
    // market records desk the first time. Notification only.
    public static event Action OnRosaRecordsCollected;
    public static event Action OnMarketDeskOpened;


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

    public static void RaiseCaseFolderStored()
        => OnCaseFolderStored?.Invoke();

    public static void RaiseCaseFolderReturned()
        => OnCaseFolderReturned?.Invoke();

    public static void RaiseCaseFolderRetrieved()
        => OnCaseFolderRetrieved?.Invoke();

    public static void RaiseFindingRecorded()
        => OnFindingRecorded?.Invoke();

    public static void RaisePreliminaryHoldStamped()
        => OnPreliminaryHoldStamped?.Invoke();

    public static void RaiseRosaRecordsCollected()
        => OnRosaRecordsCollected?.Invoke();

    public static void RaiseMarketDeskOpened()
        => OnMarketDeskOpened?.Invoke();
}
