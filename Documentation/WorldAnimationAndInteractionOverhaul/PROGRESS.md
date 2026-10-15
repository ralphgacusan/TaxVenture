# Progress

Last updated: 2026-10-15

## Current stop point

Ticket 01 is complete: the user confirmed the idle and conversation facing in Play Mode (2026-10-15). Walking is deferred (KNOWN_ISSUES.md). Only `Assets/Scenes/Office_Level1.unity` is in scope. Wait for explicit instruction before Ticket 02.

## Setup checklist

- [x] Actual project audit and dirty Git baseline recorded.
- [x] Independent README, AGENTS, audit, plan, progress, decisions, known issues, six tickets and handoff template exist.
- [x] Minimap tutorial transcript read and reference dependency satisfied.
- [x] Scope narrowed to Office_Level1 in instructions and tickets.
- [x] Earlier partial implementation inventoried and next step recorded in `handoffs/SETUP_READY.md`.

## Ticket status

| Ticket | Status | Earlier work and remaining gap |
|---|---|---|
| T01 NPC animation | Complete (walking deferred) | Rig/controller audit done (Marie, Jhun, Zorbix: Humanoid Standing Idle; 8 others: static head fallback). Driver hardened and compiled; regressions pass. Follow-up adds visible static idle, pacing and a walk setup tool (ticket section 16). Now idle-only with varied idle actions (section 18); walking disabled as a known problem. User verified in Play Mode. |
| T02 Conversation animation | Complete | Procedural talk/listen/facing/farewell in the driver (ticket section 15). Builds 0 errors; regressions pass; User verified in Play Mode, including Elijah motion and the first-tap/Milo camera fixes (ticket sections 17-18). |
| T03 Desk interaction | Deferred; partial work retained | DeskPlacementSurface and metadata exist; folder/book drag scripts reference it. Desk scene wiring and verification remain. |
| T04 3D documents | Planned; do not start | No implementation claimed. |
| T05 Feedback polish | Planned; do not start | No implementation claimed. |
| T06 Dynamic minimap | Planned; do not start | Transcript reviewed; no minimap implementation. |

## Interrupted operation

Earlier code added animation components to 11 Office_Level1 NPCs. Writing Office_Level1_COPY then failed with OSError invalid argument; the worker subsequently hit its usage limit. Current inspection finds 11 driver GUID occurrences in Office_Level1 and zero in Office_Level1_COPY/Office_Level2. Preserve all other pre-existing scene changes. Do not resume the multi-scene script.

## Earlier verification

- Dialogue baseline: 796 assertions passed.
- Controls baseline: 5,734 assertions passed.
- Progression baseline: completion, unlocking, migration, rewards and JSON checks passed.
- An earlier driver revision compiled against Unity references. The latest fallback and partial implementation were not covered by that compile.
- No Play Mode, device, or visual correctness is claimed.
- This setup-only pass checked documentation, scope and partial files; it changed documentation only.

## Ticket 01 verification (2026-10-15)

- Compile: scratch csproj from Assembly-CSharp.csproj (plus untracked DeskPlacementSurface.cs, which the stale generated csproj omits): 0 errors.
- DialogueRegression 796 PASS; ControlsRegression 5,734 PASS; ProgressionRegression PASS.
- No Play Mode, device or visual test was run.

## Next step

Wait for the user to commit Tickets 01-02 and explicitly start Ticket 03.
