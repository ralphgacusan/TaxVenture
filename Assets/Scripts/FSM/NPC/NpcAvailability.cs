/// <summary>What an NPC's overhead marker should communicate.</summary>
public enum NpcAvailability
{
    /// <summary>No useful conversation right now (or not yet): no marker.</summary>
    Unavailable,
    /// <summary>Talking now moves something forward: "available" marker.</summary>
    Available,
    /// <summary>This NPC's conversation is done for now: "completed" marker.</summary>
    Completed
}

/// <summary>
/// Implemented by each NPC interactable. The interactable stays the single
/// authority for its own rules; markers only read this value.
/// </summary>
public interface INpcAvailabilitySource
{
    NpcAvailability Availability { get; }
}

/// <summary>Marker shape actually drawn.</summary>
public enum NpcMarkerShape { None, Available, Completed }

/// <summary>
/// Pure availability rules per NPC type, mirroring exactly what each
/// interactable's OnInteract/OnPointerClick would do. Kept outside Unity so
/// the mapping is unit-tested. Cancellation and rejected starts never set the
/// completion flags these read, so they can never show "completed".
/// </summary>
public static class NpcAvailabilityRules
{
    /// <summary>Greets once per case; a new case makes it available again.</summary>
    public static NpcAvailability Receptionist(bool caseLoaded, bool hasGreeting, bool spokenForCurrentCase)
    {
        if (!caseLoaded) return NpcAvailability.Unavailable;
        if (spokenForCurrentCase) return NpcAvailability.Completed;
        return hasGreeting ? NpcAvailability.Available : NpcAvailability.Unavailable;
    }

    /// <summary>One-shot small talk.</summary>
    public static NpcAvailability Colleague(bool hasSpoken) =>
        hasSpoken ? NpcAvailability.Completed : NpcAvailability.Available;

    /// <summary>
    /// Interview once; after the audit (case outcome phase) the findings
    /// presentation makes the client available again until presented.
    /// </summary>
    public static NpcAvailability Client(bool caseLoaded, bool inOutcomePhase,
        bool presentationCompleted, bool hasInterviewed)
    {
        if (!caseLoaded) return NpcAvailability.Unavailable;
        if (inOutcomePhase)
            return presentationCompleted ? NpcAvailability.Completed : NpcAvailability.Available;
        return hasInterviewed ? NpcAvailability.Completed : NpcAvailability.Available;
    }

    /// <summary>
    /// The audit itself starts from the submission tray. Before submission the
    /// auditor only repeats a reminder (no marker); a submitted-but-interrupted
    /// audit can be resumed here (available); after submission he is done.
    /// </summary>
    public static NpcAvailability Auditor(bool caseLoaded, bool auditPendingForCurrentCase, bool alreadySubmitted)
    {
        if (!caseLoaded) return NpcAvailability.Unavailable;
        if (auditPendingForCurrentCase) return NpcAvailability.Available;
        return alreadySubmitted ? NpcAvailability.Completed : NpcAvailability.Unavailable;
    }

    /// <summary>One conversation per taxpayer; it drops that taxpayer's profile.</summary>
    public static NpcAvailability Taxpayer(bool configurationValid, bool hasBeenTalkedTo)
    {
        if (hasBeenTalkedTo) return NpcAvailability.Completed;
        return configurationValid ? NpcAvailability.Available : NpcAvailability.Unavailable;
    }

    /// <summary>
    /// Appears after all four profiles; the intro opens the board. While the
    /// board runs (and while the final talk starts by itself) there is nothing
    /// to click; after the result panel the guide is done.
    /// </summary>
    public static NpcAvailability Guide(bool hasAppeared, bool hasStartedSideQuest, bool hasShownCompletionPanel)
    {
        if (hasShownCompletionPanel) return NpcAvailability.Completed;
        if (!hasAppeared || hasStartedSideQuest) return NpcAvailability.Unavailable;
        return NpcAvailability.Available;
    }

    /// <summary>
    /// What to draw: nothing while any conversation or other camera mode owns
    /// the screen, outside the range band, or when occluded.
    /// </summary>
    public static NpcMarkerShape Shape(NpcAvailability availability, bool suppressed,
        float distance, float minDistance, float maxDistance, bool occluded)
    {
        if (suppressed || occluded) return NpcMarkerShape.None;
        if (distance > maxDistance || distance < minDistance) return NpcMarkerShape.None;
        switch (availability)
        {
            case NpcAvailability.Available: return NpcMarkerShape.Available;
            case NpcAvailability.Completed: return NpcMarkerShape.Completed;
            default: return NpcMarkerShape.None;
        }
    }

    /// <summary>Restrained float: vertical bob as a fraction of marker size.</summary>
    public static float Bob(float time, float amplitude, float frequency) =>
        amplitude * (float)System.Math.Sin(time * frequency * 2.0 * System.Math.PI);

    /// <summary>Gentle pulse for the available marker only; completed stays still.</summary>
    public static float Pulse(NpcMarkerShape shape, float time, float amount, float frequency) =>
        shape == NpcMarkerShape.Available
            ? 1f + amount * (float)System.Math.Sin(time * frequency * 2.0 * System.Math.PI)
            : 1f;
}
