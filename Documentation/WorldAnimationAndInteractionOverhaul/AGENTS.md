# Agent instructions

## Current user scope and stop point (2026-10-15)

Only `Assets/Scenes/Office_Level1.unity` is in scope. Disregard Office_Level1_COPY and Office_Level2 for this initiative. Ticket 01 is complete (walking deferred); Ticket 02 is complete; do not start Ticket 03 until the user asks; do not start another ticket until the user explicitly asks. Read `handoffs/TICKET-01_CODE_COMPLETE.md` before the next implementation session. The user does all git actions and Play Mode testing.

Before any work, read `README.md`, `AGENTS.md`, `PROGRESS.md`, `DECISIONS.md`, and the active ticket. Read `PROJECT_AUDIT.md` when the task depends on the initial inventory. Inspect the actual Unity project and current Git state before editing. Identify the current implementation phase and completed acceptance criteria.

- Do not redo completed work unless a concrete defect requires it.
- Do not overwrite unrelated changes or revert another agent's work.
- Follow existing architecture and naming conventions when suitable.
- Avoid unnecessary packages, plugins, assets, abstractions, and duplicate systems.
- Keep changes focused on the active ticket.
- Preserve existing game behavior and compatible public interfaces.
- Do not claim a feature is complete without verifying it.
- After each ticket or meaningful phase, update `PROGRESS.md`, the ticket status, `DECISIONS.md` if needed, and `KNOWN_ISSUES.md`; record modified files, verification, incomplete work, dependencies, and the exact next step.
- Do not modify `Documentation/ImmersiveInteractionOverhaul` as part of this initiative.
- Do not commit, reset, clean, stage, or push changes unless separately requested.
- Preserve serialized Unity names, GUIDs, scene references, public APIs, enum values, and UnityEvent bindings. Use small, reviewable changes to already-dirty runtime files.
- Do not launch another Unity editor instance when this project is already open. Distinguish source inspection or compilation evidence from actual Play Mode testing.

## Completion report

At handoff, state implemented behavior, files changed, checks actually run and their results, limitations, prioritized manual checks, and the exact next ticket. Never mark an unverified feature complete.
