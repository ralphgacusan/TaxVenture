using System;

/// <summary>
/// PURPOSE:
/// Lightweight, static event hub for gameplay milestones that need to
/// broadcast "this happened" to anything interested — currently the HUD's
/// icon unlock states, but intentionally generic so future systems (tutorial
/// hints, achievements) can subscribe without new plumbing.
///
/// WHY STATIC, NOT A MONOBEHAVIOUR MANAGER:
/// These are simple one-shot signals with no state of their own to own or
/// tick — a manager GameObject would add nothing but Inspector clutter.
/// This follows the same "event, not tight reference" principle already
/// used by GameStateMachine.OnStateChanged and NpcStateMachine.OnStateChanged;
/// it does not introduce a new architectural pattern.
///
/// CONNECTS WITH:
/// - CaseFolderInteractable: raises CaseFolderFirstOpened
/// - TaxCodeBookInteractable: raises TaxCodeBookFirstOpened
/// - (Phase 2) Receptionist dialogue conclusion: will raise NotesUnlockRequested
/// - HudIconButton: subscribes to the relevant event for its own icon
/// </summary>
public static class GameplayEvents
{
    public static event Action OnCaseFolderFirstOpened;
    public static event Action OnTaxCodeBookFirstOpened;
    public static event Action OnNotesUnlockRequested;

    public static void RaiseCaseFolderFirstOpened() => OnCaseFolderFirstOpened?.Invoke();
    public static void RaiseTaxCodeBookFirstOpened() => OnTaxCodeBookFirstOpened?.Invoke();
    public static void RaiseNotesUnlockRequested() => OnNotesUnlockRequested?.Invoke();

    public static event Action OnTaxReturnCollected;

    public static void RaiseTaxReturnCollected() => OnTaxReturnCollected?.Invoke();

}