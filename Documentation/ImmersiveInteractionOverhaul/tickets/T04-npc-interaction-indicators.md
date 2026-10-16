# T04 - NPC availability and completion markers

Status: **IMPLEMENTED — internal checks passed; user scene setup and Unity acceptance pending.**
Dependencies: T02 truthful conversation status, T03 presentation (code-complete; manual acceptance pending). Next: T05 after acceptance.

## What exists now

A floating marker above each NPC shows the truth about its conversation:

| Marker | Meaning | Look |
|---|---|---|
| Amber downward **triangle**, gently pulsing | Talking now moves something forward | Shape + colour + motion |
| Green **check mark**, still | This NPC's conversation is done for now | Different shape + colour, no pulse |
| Nothing | Unavailable, during **any** conversation (keeps T03 shots and faces clean), in another camera mode (desk, board, profile viewer), out of range, too close (marker would be overhead) or occluded | — |

Shape carries the meaning, so the markers do not rely on colour alone. Both shapes have a dark outline, so they read against bright and dark backgrounds.

### Authoritative state (no second state authority)

Each interactable implements `INpcAvailabilitySource.Availability`, using pure `NpcAvailabilityRules` that mirror its own click logic:

- **Receptionist:** available for each newly loaded case that has a greeting; completed once greeted for that case. A new case makes it available again.
- **Colleague:** available until the one-shot chat, then completed (exhausted).
- **Client:**
  - Available until interviewed, then completed.
  - In the case-outcome phase, available again for the findings presentation, then completed. An already-interviewed client can therefore become available again later.
- **Auditor:**
  - No marker before submission: talking only repeats the reminder, and the audit starts from the tray.
  - Available when a submitted audit was interrupted and can be resumed.
  - Completed once the audit is submitted.
- **Four taxpayers:** each is available until its own conversation drops its profile, then completed. They are independent of each other and of the office client. A misconfigured taxpayer, whose start T02 would refuse, shows no marker.
- **Tutorial guide:**
  - Hidden until all four profiles are collected, then available for the intro.
  - No marker while the board runs; the final talk also starts by itself.
  - Completed after the result panel.

Completion flags are set only on successful completion (T02), so cancelled or rejected conversations never show a check.

`NpcStatusMarker` re-reads availability:

- on NPC FSM state changes,
- on dialogue start and end,
- on a light poll every 0.2 s.

That keeps it correct after re-enable, scene reload and case changes driven elsewhere. The three existing `NpcVisualIndicator` body tints are untouched; they are a separate debug cue.

### Placement, motion and cost

- **Placement:** anchored above the NPC's measured head (from T03's `DialogueSpeakerFrames`; an optional `DialogueSpeakerAnchor` corrects props). Sized and gapped as fractions of NPC height, so it is scale-independent. It follows the NPC's position every frame and billboards to the camera.
- **Motion:** a small bob, a ±6% pulse on the triangle only, and a scale in/out (8/s) when appearing, hiding or changing shape.
- **Range:** 0.35–10 × NPC height. The occlusion raycast runs at the poll rate and ignores the NPC itself and the player.
- **Rendering:** shared procedural, double-sided meshes built once, one shared unlit material asset, per-marker `MaterialPropertyBlock` colours. No per-frame allocation and no material instancing. The marker objects sit at the scene root on Ignore Raycast, with no colliders and no shadows, so they cannot take clicks, taps or joystick input.

## Files

- New: `Assets/Scripts/FSM/NPC/NpcAvailability.cs` (enum, `INpcAvailabilitySource` and the pure `NpcAvailabilityRules`), `Assets/Scripts/FSM/NPC/NpcStatusMarker.cs`, `Assets/Editor/NpcMarkerSetup.cs`, `Tests/DialogueRegression/NpcMarkerTests.cs`.
- Changed (additive `Availability` adapters only): `ReceptionistInteractable`, `ColleagueInteractable`, `ClientInteractable`, `AuditorInteractable`, `TaxpayerNpcInteractable` (static data validity checked once), `TutorialNpcController` (plus `HasShownCompletionPanel`).
- Changed: the regression `Program.cs` and `.csproj`.
- No quest rule, NPC transform, collider, animation or scene file was changed by the agent.

## Internal evidence

- PASS: full Unity 6000.4.5f1 compile of runtime and Editor with T03 and T04 together: 0 errors, baseline warnings only.
- PASS: **796** contract assertions (+39 T04). They cover every NPC's availability matrix, including the client's later presentation, auditor resume over submitted, independent taxpayers, guide phases and cancellation never completing; marker shape and suppression for conversation and camera mode, range (inclusive limits), the too-close and occluded states; and motion bounds with no pulse when completed.
- PASS: on-disk files hash-match the tested copies; diffs scoped.
- NOT RUN: the setup menu, Play mode, visuals and device testing.

## User setup and manual matrix

1. Let Unity compile. In `Office_Level1` (Edit mode) run **Tools > TaxVenture > Configure NPC Markers**. It adds a marker to every NPC interactable, creates `Assets/Materials/NpcStatusMarker.mat` and logs a list. Save the scene.
2. Play and check each NPC before, during and after a successful conversation, after a cancel (deactivate `DialoguePanel` mid-line), while unavailable, from far away, from right under it, and behind a wall.
   - Receptionist: triangle → check, and a triangle again on the next case.
   - Colleague: triangle → check.
   - Client: triangle → check after the interview → triangle again in the outcome phase → check after presenting.
   - Auditor: no marker before submitting; triangle if a submitted audit was interrupted; check afterwards.
   - Each taxpayer: triangle → check on its own.
   - Guide: appears with a triangle; no marker during the board; check after the result.
   - No markers at all during any conversation or while at the desk or corkboard.
3. Check readability against indoor and outdoor backgrounds. Markers must never block clicking an NPC or stop the joystick.

Tuning lives on each `NpcStatusMarker`: colours, size, height gap, bob and pulse, range, occlusion and poll rate.

## Acceptance criteria

- [x] Available and completed are distinguishable by shape as well as colour. *(code; visual check pending)*
- [x] Four-taxpayer completion follows actual conversation and profile state.
- [x] Ordinary NPC exhaustion and later case-dependent availability are truthful (client presentation, receptionist per case, auditor resume).
- [x] Cancelled or rejected dialogue does not mark completion.
- [x] Markers follow, billboard, respect range and occlusion, and stay out of conversation shots and other camera views. *(visual check pending)*
- [x] Active and unavailable NPCs are suppressed; re-enable and reload resynchronize through the poll and events.
- [x] Marker geometry cannot take world interaction or joystick input.
- [ ] **User Unity setup and the manual matrix above.**

Known limits: an NPC whose bounds include props anchors too high (fix with a `DialogueSpeakerAnchor`). `Office_Level2` is not configured; the menu works per scene. Occlusion uses colliders, so visual-only geometry without colliders will not hide markers.
