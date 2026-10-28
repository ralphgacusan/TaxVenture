/// <summary>
/// PURPOSE:
/// Pure rules for Marie's walk to her case (Level 1 Living Plaza), kept out
/// of Unity so the regression suite can check every order of events.
/// The trip is driven by conditions that are read each time (side quest
/// done, front doors open, receptionist hand-over done), never by one-shot
/// events, so an event that already happened can never strand her.
/// </summary>
public static class MarieTripRules
{
    public enum Leg { Plaza, ToReception, AtReception, ToRoom, InRoom }

    /// <summary>
    /// The leg to start now (the same leg = keep going). From the plaza she
    /// leaves once the side quest is done and the front doors are open,
    /// heading straight for the room when the hand-over already happened.
    /// At the desk she moves on once the hand-over is done and her short
    /// check-in moment is over.
    /// </summary>
    public static Leg Next(Leg current, bool sideQuestDone, bool doorsClosed, bool receptionDone, bool checkInOver)
    {
        if (!sideQuestDone) return current;
        switch (current)
        {
            case Leg.Plaza:
                if (doorsClosed) return Leg.Plaza;
                return receptionDone ? Leg.ToRoom : Leg.ToReception;
            case Leg.AtReception:
                return receptionDone && checkInOver ? Leg.ToRoom : Leg.AtReception;
            default:
                return current;
        }
    }

    /// <summary>Where a resumed run puts her (no walk): her saved presence and the restored flags decide.</summary>
    public static Leg Resume(bool savedInRoom, bool sideQuestDone, bool receptionDone)
    {
        if (savedInRoom || (sideQuestDone && receptionDone)) return Leg.InRoom;
        return sideQuestDone ? Leg.AtReception : Leg.Plaza;
    }

    /// <summary>A walk to the room starts outside the office when she has not been through the front doors yet.</summary>
    public static bool StartsOutside(Leg current) => current == Leg.Plaza || current == Leg.ToReception;
}
