# T05 — Cinematic dialogue presentation

Status: **PLANNED — awaiting approval**
Dependencies: T03 camera safety/ownership, T04 session events, T02 Elijah visual/rig. Next: T06. Estimate: **10k-18k tokens**; split camera framing and UI/typewriter if needed.

## Objective and inspected entry points

Cinematic, readable speaker exchanges using the supplied shoulder/reverse-shot examples, without replacing art or adding NPC animations.

Primary files: `Assets/Scripts/UI/DialogueUI.cs`; `Assets/Scripts/Data/DialogueLine.cs`, `DialoguePortraitDatabase.cs`; `Assets/Scripts/Player/CameraController.cs`, `FirstPersonHands.cs`; T04 NPC adapters and scene camera/head targets; `Assets/Scenes/Office_Level1.unity` dialogue Canvas hierarchy. Reuse validated Elijah player visual from `Assets/3D Models/npc_elijah/`.

## Scope

- Consume current speaker/session events; compose NPC-focused and player-focused reverse shots with appropriate shoulder/side foreground where safe.
- Use character height/head/shoulder references rather than hard-coded per-name positions. Reuse collision checks and choose an unobstructed fallback in confined spaces.
- Coordinate player mesh visibility so first-person views do not enter the head/body and conversation shots can retain shoulders.
- Anchor dialogue near the safe-area bottom; clear speaker name, readable TMP text, portrait if appropriate, consistent spacing and Continue at bottom-right.
- Dialogue box taps and Continue share one gesture route. Reveal progressively with a TMP-compatible typewriter; configurable pacing and punctuation pauses.
- Tap while typing reveals the entire current line only; a distinct next gesture advances. Prevent duplicate input from tap + button/submit or held/repeated presses.
- Stop/restart owned routines safely on line changes, cancel, replacement, disable and scene teardown. Do not restart ended sessions from stale events.
- Match TaxVenture's existing style; supplied images guide composition, not asset copying.

## Acceptance criteria

- [ ] Active speaker is visually clear in office/outdoor conversations, different heights and tight spaces.
- [ ] NPC/player reverse framing is smooth and does not clip geometry or models.
- [ ] Elijah remains correctly visible/hidden for each shot; no NPC animation additions.
- [ ] Bottom dialogue UI fits mobile safe areas and supported aspect ratios without covering essential controls.
- [ ] Speaker, text and Continue are readable with consistent hierarchy.
- [ ] Typewriter, punctuation, reveal-all and next-line work as specified with one action per gesture.
- [ ] Ending/cancelling mid-type or mid-blend leaves no coroutine/input leak and preserves T04 outcomes.
- [ ] Joystick touches cannot advance dialogue; dialogue taps cannot move/look/interact through UI.

## Verification and handoff

Compile; focused tests for reveal/advance arbitration and routine cancellation. Manual: alternate speakers, long/short/rich-text lines, punctuation, rapid taps, held touch, click/submit, notch/safe-area/resolution changes, wall-constrained shots, and interruptions. Record actual scene/device evidence separately from algorithm checks.

Document framing targets/fallbacks, timing settings, UI bindings and tests; do not migrate unrelated content/reward logic. Stop for approval.

