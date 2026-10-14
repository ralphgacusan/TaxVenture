# Instructions for agents working on this overhaul

## Current direction

Keep the existing first-person exploration view and implemented T01 controls. Use the six-ticket index in README. T02 is unified dialogue using existing camera/control APIs. T05 checks safe cancellation, input restoration and paper-viewer closing. Reuse existing CameraController ownership; T03 adds conversation-specific presentation, not a replacement exploration controller. Exploration remains first person. T03 alone adds a conversation-only standing Elijah visual, reference-style speaker camera shots, compact bottom dialogue UI and typewriter/tap behavior. Restore the original first-person view and hide the conversation visual on every exit; no exploration camera overhaul is assigned. Every future ticket requires explicit approval.

## Authority and startup

Follow the user's latest instruction and applicable repository instructions. This file lives under Documentation; agents assigned overhaul tickets must explicitly read it even when editing runtime files elsewhere. Do not treat it as permission to implement unapproved tickets.

Before each work unit read this file, README.md, PROGRESS.md, HANDOFF.md, and the assigned ticket. Inspect Git status/current branch and the live relevant code, serialized scene references, prefab links, and existing checks. Repository-root paths are defined in README.md. Existing `docs/GAME_FLOW_CURRENT.md` and `docs/LOGIC_AUDIT.md` are historical context, not proof that every finding still exists.

## Scope and workflow

- Initial session is documentation only. Start gameplay work only after explicit approval.
- Work on one approved ticket/checkpoint, verify it, update progress/handoff/ticket status, report, and stop. Do not proceed based on silence.
- Inspect before replacing. Reuse existing controllers, dialogue data, UI, assets, and validation seams. Proposals in the plan are not already implemented APIs.
- Preserve taxpayer paper drop/bounce, collection flight, tray entrance, free board placement, viewer opening/drag/layering/closing, and its delayed control restoration.
- Preserve each NPC's content, case/interview flags, tutorial events, callbacks, rewards, and replay rules. Cancellation must not count as successful completion.
- Do not add NPC animation or unrelated progression fixes. T03 may configure existing Elijah assets into a grounded standing/idle conversation-only visual. Do not change the player collider/root scale or shared NPC imports to fit it; no locomotion system is assigned.

## Unity conventions

- Use the existing Unity version/package set unless a separately justified change is approved. No nested Unity projects or unrequested packages.
- One camera transform writer and one owner for interaction/control state. Respect active owners during cancellations, overlays, scene unload, and disabled objects; avoid blindly re-enabling controls.
- Capture/reset touch ownership and joystick input on release, cancellation, pause/focus loss, and disable. Keep UI touches out of world interaction. Preserve desktop testing support.
- Initialize Unity native-backed objects in lifecycle methods or safe lazy runtime paths, never MonoBehaviour constructors/field initializers. An earlier MaterialPropertyBlock failure is a regression to avoid.
- Use serialized/configurable references and existing data identifiers. Preserve serialized names or migrate them explicitly; preserve enum values and UnityEvent bindings when changing APIs.
- Avoid competing coroutines, duplicated event subscriptions, instantiated shared-material edits, and per-frame allocations where practical. Unsubscribe and clean up owned objects/locks.
- Respect existing Rigidbody movement, UI raycasting, safe areas, TMP readability and Android performance. Preserve current camera/input ownership and viewer raycast isolation.
- Save deliberate scene changes with Unity where available; inspect resulting diffs and `.meta` GUIDs. Do not broadly rewrite scene YAML or regenerate unrelated assets. Do not overwrite an unsaved open scene without reconciling it with the user.

## Verification and reporting

Run the smallest meaningful checks for the ticket. Distinguish static inspection, C# compilation, geometry calculations, EditMode/PlayMode checks, and actual manual/device playtests. A compilation or simulated calculation is not a gameplay pass.

Reuse `Tests/VerifyProfileCards.ps1`, `Tests/ProgressionRegression/ProgressionRegression.csproj`, and relevant `Assets/Editor/*Validation.cs` when their behavior is affected. Inspect their current scope before citing them. Do not edit a report to produce a pass, disable assertions, or suppress real failures.

If Unity/device execution is unavailable, record NOT RUN with the reason and give concise manual steps. User can perform gameplay tests; do not claim they happened until results arrive. Record resolution/aspect/device and reproduction steps for failures. Existing reports in Library may be stale.

## Git and shared changes

Never discard, revert, overwrite, or stage unrelated work. Never commit, merge, push, switch branches, delete files, or rewrite history without explicit authorization. Do not create a branch merely because tickets exist. If a new branch is requested without an exact name, use `codex/` as the default prefix.

At planning baseline, `CameraController.cs` and `CollectedProfilesWorldPanel.cs` had uncommitted changes; the user subsequently committed/pushed them and created `feat/immersive-interaction-overhaul`. Preserve that paper behavior and inspect live status rather than assuming the old diff remains. Coordinate single-writer ownership of shared scripts and all scene/prefab files; read others' changes before integrating.

The user performs Git write actions and manual Unity gameplay testing to save usage. Agents run focused internal checks and provide concise commands/Editor steps; do not commit/push or run a manual playthrough on the user's behalf during this work.

After a work unit update the assigned ticket, CURRENT_STATE.md as implementation changes, PROGRESS.md, and HANDOFF.md. Report files changed, behavior, tests/results, unresolved issues, exact next step, and the next unit's rough token range and uncertainties. Stop for approval.
