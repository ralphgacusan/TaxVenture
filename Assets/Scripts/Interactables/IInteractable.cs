using UnityEngine;

/// <summary>
/// PURPOSE:
/// Contract that any "interactable" object in the game world must implement.
/// This includes desks, computers, folders, tax books, corkboards, filing
/// cabinets, and NPCs (clients, auditor).
///
/// WHY AN INTERFACE:
/// The Interactor (attached to the player) should never need to know what
/// TYPE of object it's looking at. It just calls these three methods and lets
/// each object decide what "being focused" or "being interacted with" means
/// for itself. This keeps the interaction system scalable — adding a new
/// interactable type never requires modifying Interactor.cs.
///
/// IMPLEMENTED BY (this milestone and future ones):
/// - DeskInteractable (Milestone 2/3)
/// - ComputerInteractable (Milestone 10)
/// - CaseFolderInteractable (Milestone 5)
/// - TaxBookInteractable (Milestone 9)
/// - CorkboardInteractable (Milestone 11)
/// - NPCInteractable (Milestone 8)
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called every frame the player's raycast is looking at this object.
    /// Typically used to trigger highlight visuals.
    /// </summary>
    void OnFocus();

    /// <summary>
    /// Called the frame the raycast stops looking at this object
    /// (looked away, walked away, or another object is now closer).
    /// Typically used to remove highlight visuals.
    /// </summary>
    void OnUnfocus();

    /// <summary>
    /// Called when the player presses the interact input while focused on this object.
    /// </summary>
    void OnInteract();

    /// <summary>
    /// Text shown in the interact prompt UI, e.g. "Press E to open Desk".
    /// Lets each object customize its own prompt without Interactor.cs
    /// needing special-case logic per object type.
    /// </summary>
    string GetPromptText();
}