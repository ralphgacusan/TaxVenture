using System;

/// <summary>T04 contract tests for the production NpcAvailabilityRules.</summary>
public static class NpcMarkerTests
{
    private const NpcAvailability U = NpcAvailability.Unavailable;
    private const NpcAvailability A = NpcAvailability.Available;
    private const NpcAvailability C = NpcAvailability.Completed;

    public static void Run(Action<bool, string> check)
    {
        // Receptionist: once per case, again for a new case.
        check(NpcAvailabilityRules.Receptionist(false, true, false) == U, "Receptionist: no case loaded → no marker.");
        check(NpcAvailabilityRules.Receptionist(true, true, false) == A, "Receptionist: new case with a greeting → available.");
        check(NpcAvailabilityRules.Receptionist(true, false, false) == U, "Receptionist: empty greeting cannot complete → no marker.");
        check(NpcAvailabilityRules.Receptionist(true, true, true) == C, "Receptionist: greeted for this case → completed.");

        // Colleague: one-shot.
        check(NpcAvailabilityRules.Colleague(false) == A && NpcAvailabilityRules.Colleague(true) == C,
            "Colleague: available until spoken to, then completed (exhausted).");
        check(NpcAvailabilityRules.Shape(NpcAvailabilityRules.Colleague(false), false, 5f, 0.5f, 10f, false) == NpcMarkerShape.Available &&
              NpcAvailabilityRules.Shape(NpcAvailabilityRules.Colleague(true), false, 5f, 0.5f, 10f, false) == NpcMarkerShape.Completed,
            "ambient plaza conversations use the same triangle while available and check after a completed talk.");

        // Client: interview, then case-dependent presentation.
        check(NpcAvailabilityRules.Client(false, false, false, false) == U, "Client: no case → no marker.");
        check(NpcAvailabilityRules.Client(true, false, false, false) == A, "Client: not interviewed → available.");
        check(NpcAvailabilityRules.Client(true, false, false, true) == C, "Client: interviewed, before outcome → completed for now.");
        check(NpcAvailabilityRules.Client(true, true, false, true) == A,
            "Client: outcome phase makes an already-interviewed client available again for the presentation.");
        check(NpcAvailabilityRules.Client(true, true, true, true) == C, "Client: presentation done → completed.");
        check(NpcAvailabilityRules.Client(true, true, false, false) == A, "Client: outcome phase availability does not depend on the interview flag.");

        // Auditor: tray starts the audit; reminder is not a marker.
        check(NpcAvailabilityRules.Auditor(false, false, false) == U, "Auditor: no case → no marker.");
        check(NpcAvailabilityRules.Auditor(true, false, false) == U, "Auditor: pre-submission reminder only → no marker.");
        check(NpcAvailabilityRules.Auditor(true, true, false) == A, "Auditor: interrupted audit can be resumed → available.");
        check(NpcAvailabilityRules.Auditor(true, false, true) == C, "Auditor: audit submitted → completed.");
        check(NpcAvailabilityRules.Auditor(true, true, true) == A, "Auditor: a pending resume takes priority over 'submitted'.");

        // Taxpayers: one conversation each, tied to their profile drop.
        check(NpcAvailabilityRules.Taxpayer(true, false) == A, "Taxpayer: valid and not talked to → available.");
        check(NpcAvailabilityRules.Taxpayer(true, true) == C, "Taxpayer: talked to (profile dropped) → completed.");
        check(NpcAvailabilityRules.Taxpayer(false, false) == U, "Taxpayer: misconfigured (start would be refused) → no marker.");
        check(NpcAvailabilityRules.Taxpayer(false, true) == C, "Taxpayer: completion is never hidden.");

        // Four taxpayers progress independently of each other and of the office client.
        bool[] talked = { true, false, true, false };
        int completed = 0, available = 0;
        foreach (bool t in talked)
        {
            NpcAvailability a = NpcAvailabilityRules.Taxpayer(true, t);
            if (a == C) completed++;
            if (a == A) available++;
        }
        check(completed == 2 && available == 2, "Each taxpayer's marker must follow its own conversation, not the others'.");
        check(NpcAvailabilityRules.Client(true, false, false, false) == A && completed == 2,
            "Town progress must not change the office client's marker.");

        // Guide.
        check(NpcAvailabilityRules.Guide(false, false, false) == U, "Guide: hidden before all four profiles.");
        check(NpcAvailabilityRules.Guide(true, false, false) == A, "Guide: appeared → available for the intro.");
        check(NpcAvailabilityRules.Guide(true, true, false) == U, "Guide: board running / final talk starts itself → no marker.");
        check(NpcAvailabilityRules.Guide(true, true, true) == C, "Guide: result panel shown → completed.");

        // Cancellation/rejection never sets the completion flags, so the same
        // inputs as before a cancelled attempt give the same marker.
        check(NpcAvailabilityRules.Taxpayer(true, false) == A && NpcAvailabilityRules.Colleague(false) == A &&
              NpcAvailabilityRules.Guide(true, false, false) == A,
            "A cancelled or rejected conversation must leave the NPC available, never completed.");

        // Marker shapes and suppression.
        check(NpcAvailabilityRules.Shape(A, false, 5f, 0.5f, 10f, false) == NpcMarkerShape.Available, "Available → triangle.");
        check(NpcAvailabilityRules.Shape(C, false, 5f, 0.5f, 10f, false) == NpcMarkerShape.Completed, "Completed → check.");
        check(NpcAvailabilityRules.Shape(U, false, 5f, 0.5f, 10f, false) == NpcMarkerShape.None, "Unavailable → nothing.");
        check(NpcAvailabilityRules.Shape(A, true, 5f, 0.5f, 10f, false) == NpcMarkerShape.None &&
              NpcAvailabilityRules.Shape(C, true, 5f, 0.5f, 10f, false) == NpcMarkerShape.None,
            "Any conversation / other camera mode hides every marker (keeps T03 shots clean).");
        check(NpcAvailabilityRules.Shape(A, false, 11f, 0.5f, 10f, false) == NpcMarkerShape.None, "Out of range → hidden.");
        check(NpcAvailabilityRules.Shape(A, false, 0.3f, 0.5f, 10f, false) == NpcMarkerShape.None, "Too close (marker overhead) → hidden.");
        check(NpcAvailabilityRules.Shape(A, false, 10f, 0.5f, 10f, false) == NpcMarkerShape.Available &&
              NpcAvailabilityRules.Shape(A, false, 0.5f, 0.5f, 10f, false) == NpcMarkerShape.Available, "Range limits are inclusive.");
        check(NpcAvailabilityRules.Shape(C, false, 5f, 0.5f, 10f, true) == NpcMarkerShape.None, "Occluded → hidden.");
        check(NpcMarkerShape.Available != NpcMarkerShape.Completed, "Available and completed must be different shapes, not just colours.");

        // Restrained motion.
        float maxBob = 0f, minPulse = 2f, maxPulse = 0f;
        bool completedStill = true;
        for (int i = 0; i < 2000; i++)
        {
            float t = i * 0.01f;
            maxBob = Math.Max(maxBob, Math.Abs(NpcAvailabilityRules.Bob(t, 0.12f, 0.6f)));
            float p = NpcAvailabilityRules.Pulse(NpcMarkerShape.Available, t, 0.06f, 1.1f);
            minPulse = Math.Min(minPulse, p);
            maxPulse = Math.Max(maxPulse, p);
            completedStill &= NpcAvailabilityRules.Pulse(NpcMarkerShape.Completed, t, 0.06f, 1.1f) == 1f;
        }
        check(completedStill, "Completed markers must not pulse.");
        check(maxBob <= 0.12f + 1e-4f && maxBob > 0.11f, "Bob must stay within its amplitude.");
        check(minPulse >= 0.94f - 1e-4f && maxPulse <= 1.06f + 1e-4f, "Pulse must stay within ±6%.");
    }
}
