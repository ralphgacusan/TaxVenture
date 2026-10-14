# Immersive interaction overhaul

Status: **T01 code implemented; Unity setup and user gameplay acceptance pending**. Updated 2026-10-14 on `feat/immersive-interaction-overhaul`. T02 has not started.

This is documentation inside the existing Unity project, not a second Unity project. Repository root: `C:\Learn\Projects\TaxVenture\TaxVenture`. All `Assets/`, `Packages/`, `ProjectSettings/`, and `Tests/` paths in these documents are relative to that root.

## Goal and scope

Make TaxVenture a responsive mobile investigation/adventure game: third-person exploration, a visible animated player, collision-aware cameras, close interaction viewpoints, consistent conversations, readable dialogue, and truthful NPC indicators. Preserve existing quests, interview outcomes, objective tracking, desk tools, profile collection/viewing, and corkboard placement.

The supplied images guide near/far third-person framing and speaker/reverse-shoulder composition with bottom dialogue UI. They do not authorize replacing the environment or importing their characters. **User-selected main character: Elijah**, using existing assets under `Assets/3D Models/npc_elijah/`. T02 must validate and configure Elijah rather than substitute a Synty character.

No NPC animations, art replacement, new quest/reward rules, package upgrades, or unrelated fixes belong to this overhaul. Basic **player** idle/walk animation is explicitly included in T02.

## Read first

1. [AGENTS.md](AGENTS.md): working rules and change safety.
2. [CURRENT_STATE.md](CURRENT_STATE.md): inspected systems and evidence limits.
3. [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md): decisions, dependencies, risks, and verification.
4. [PROGRESS.md](PROGRESS.md) and [HANDOFF.md](HANDOFF.md): continuation state.

## Ticket index and order

| Ticket | Deliverable | Depends on | Status | Rough implementation tokens |
|---|---|---|---|---|
| [T01](tickets/T01-joystick-and-touch-controls.md) | Movement joystick and screen-swipe look; touch ownership and analog movement | Approved | Code ready; revised setup/test pending | 8k-14k original estimate |
| [T02](tickets/T02-third-person-camera-and-collision.md) | Exploration camera, collision, player model/locomotion | T01 | Planned | 16k-28k; split recommended |
| [T03](tickets/T03-interaction-camera-transitions.md) | Shared interaction entry/exit and close viewpoints | T02 | Planned | 12k-20k; split if needed |
| [T04](tickets/T04-unified-dialogue-system.md) | Shared conversation lifecycle and preserved outcomes | T03 | Planned | 10k-18k |
| [T05](tickets/T05-dialogue-camera-and-ui.md) | Speaker framing, bottom UI, typewriter | T03, T04; T02 model | Planned | 10k-18k |
| [T06](tickets/T06-npc-interaction-indicators.md) | Available/completed/unavailable indicators | T04; coordinate T05 framing | Planned | 6k-10k |
| [T07](tickets/T07-integration-and-game-feel.md) | Cross-system polish and interruption handling | T01-T06 | Planned | 8k-14k |
| [T08](tickets/T08-regression-testing-and-handoff.md) | Evidence-backed regression and handoff | T07 | Planned | 6k-12k |

Default order: **T01 -> T02 -> T03 -> T04 -> T05 -> T06 -> T07 -> T08**. Estimates include focused coding, checks, and documentation; they exclude waiting for device tests, new assets, and major discovered defects. Tokens cannot reliably be converted to an account usage percentage. Re-estimate from the live diff before each work unit.

T02 should use separately approved checkpoints within its single ticket: camera ownership/exploration, collision tuning, then model/animation integration. T03/T05 can similarly be split if the available budget is smaller than the estimate. Never start a later checkpoint or ticket automatically.

## Agent coordination

Independent read-only inspection and test preparation can run in parallel. After shared contracts are agreed, dialogue-content mapping (T04) and asset suitability inspection (T02) are safe separate tasks. T06 presentation can proceed independently after T04 exposes stable conversation status. T05 and T06 must agree on framing/marker visibility.

Use one writer for `CameraController.cs`, one for `DialogueUI.cs`, and one for `Office_Level1.unity` at a time. T01/T02/T03 all touch controls and camera ownership: serialize those changes. Never assign concurrent scene/prefab edits. Parallel work does not waive the user's one-ticket approval gate.

## Overall progress

- Documentation and eight tickets: created; T01 code and focused internal checks completed, awaiting Unity scene setup/gameplay acceptance.
- Earlier profile viewer work was committed by the user before T01; it remains preserved.
- Next action: follow T01's Unity setup/test checklist. After acceptance and explicit approval, start T02a (follow/ownership), not the whole T02 automatically.
