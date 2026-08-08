using System;

/// <summary>
/// PURPOSE:
/// A static event hub so gameplay scripts (ReceptionistInteractable,
/// ClientInteractable, document-review interaction, etc.) can announce
/// "this action just happened" WITHOUT knowing the tutorial system exists.
///
/// This is the seam that keeps TutorialController decoupled from NPC
/// internals. Existing interaction scripts get ONE extra line added at
/// the point where their interaction already concludes:
///
///     TutorialEvents.RaiseReceptionistInteracted();
///
/// If no tutorial is running, this is a no-op (event fires, nobody's
/// listening). Outside of the tutorial case, this costs nothing.
///
/// Add one Raise/event pair per future tutorial-relevant gameplay action
/// (documents reviewed, client interviewed, etc.) as those steps are
/// implemented — do not pre-build events for steps that don't exist yet.
/// </summary>
public static class TutorialEvents
{
    /// <summary>Fired when the player finishes talking to the Receptionist.</summary>
    public static event Action ReceptionistInteracted;
    public static void RaiseReceptionistInteracted() => ReceptionistInteracted?.Invoke();

    // Future examples (uncomment / add as those tutorial steps are built):
    // public static event Action DocumentsReviewed;
    // public static void RaiseDocumentsReviewed() => DocumentsReviewed?.Invoke();
    //
    // public static event Action ClientInterviewed;
    // public static void RaiseClientInterviewed() => ClientInterviewed?.Invoke();
}