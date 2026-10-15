# Handoff - Ticket 01 code-complete

Date: 2026-10-15

## Outcome

One `NpcAnimationDriver` per NPC (11 serialized in Office_Level1, plus a runtime add from `NpcStateMachine`) maps FSM and owned-dialogue events to Animator states with cross-fades, falls back to idle for missing states, and runs a subtle procedural head motion on static meshes. NPC_Marie, NPC_Jhun and NPC_Zorbix play their Humanoid "Standing Idle" controllers; the other 8 NPCs are static meshes. No talk/gesture/walk clips exist, so none are claimed.

## Changed files

- [x] `Assets/Scripts/FSM/NPC/NpcAnimationDriver.cs`: prefers the child Animator that has a controller, skips CrossFade on inactive Animators, does not restart the state already playing, resets its state cache on enable.
- [x] Ticket 01, README, AGENTS, PROGRESS, DECISIONS (D007), KNOWN_ISSUES, IMPLEMENTATION_PLAN and this handoff.
- No scene, prefab or asset edits in this session. Earlier partial edits (NpcStateMachine.cs, 11 drivers in Office_Level1) were inspected and kept.

## Verification

- Compile (scratch csproj against Unity DLLs): 0 errors.
- DialogueRegression 796 PASS; ControlsRegression 5,734 PASS; ProgressionRegression PASS.
- User Play Mode check passed on 2026-10-15 (no device test).

## Acceptance criteria

- [x] Per-NPC profiles through one driver; missing Animator/state is a safe no-op.
- [x] Play Mode: user confirmed idle variety and conversation facing (2026-10-15). Walking deferred; see Ticket 01 section 18.

## Follow-up setup (required)

Run Tools > TaxVenture > Configure NPC Animation with Office_Level1 open, read the Console report, then save the scene. See ticket section 16.

## Manual checks (Office_Level1 only)

1. Console shows no Animator or NpcAnimationDriver warnings/errors on load.
2. NPC_Marie, NPC_Jhun and NPC_Zorbix (Plaza) loop their idle without a restart pop or sliding.
3. Static NPCs (Client, Receptionist, Auditor and others) show only a subtle head sway; during their own dialogue the head turns toward the player, nods on NPC lines and tilts on player lines. No head parts detach or remain offset after dialogue.
4. Talking to each NPC type still opens, advances and closes dialogue normally.
5. Disable the `Visual` child's Animator on Jhun: no errors, dialogue still works.

## Next action

If the checks pass, mark Ticket 01 Complete, then start Ticket 02 only on the user's instruction (read `tickets/TICKET-02-NPC-CONVERSATION-ANIMATIONS.md` and `NpcAnimationDriver.cs` first).
