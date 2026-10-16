# Immersive interaction improvements

Updated 2026-10-15. Keep current **first-person gameplay** and implemented T01 controls. During NPC conversations only, show Elijah standing in the scene and use cinematic over-the-shoulder/speaker reverse shots like the supplied reference. Use a compact bottom dialogue box with letter-by-letter text: tap once to finish the current line, then tap again to advance. Outside NPC dialogue the existing first-person viewpoint stays as it is; desk, board, notes and paper-viewer views keep their current behavior.

Project root: `C:\Learn\Projects\TaxVenture\TaxVenture`. Preserve quests, interviews, objectives, desk tools, paper collection/viewing and corkboard placement.

## Read first

[AGENTS.md](AGENTS.md) contains working rules; [CURRENT_STATE.md](CURRENT_STATE.md) records discovered systems; [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) defines architecture/dependencies; [PROGRESS.md](PROGRESS.md) and [HANDOFF.md](HANDOFF.md) record evidence and continuation.

## Ticket index and order

| Ticket | Scope | Dependency | Status |
|---|---|---|---|
| [T01](tickets/T01-joystick-and-touch-controls.md) | Movement joystick and screen-swipe controls | Existing gameplay | Implemented/retained at 74d53caf; manual acceptance tracked separately |
| [T02](tickets/T02-unified-dialogue-system.md) | Unified conversations and preserved outcomes | T01 and existing APIs | Phase 1 implemented/internal checks passed; manual acceptance and Phase 2 pending |
| [T03](tickets/T03-dialogue-camera-and-ui.md) | Conversation-only Elijah, speaker camera shots, bottom UI and typewriter | T02 | Requirements clarified; implementation planned in gated phases |
| [T04](tickets/T04-npc-interaction-indicators.md) | Availability/completion indicators | T02; coordinate T03 | Planned |
| [T05](tickets/T05-integration-and-game-feel.md) | Integration, safe cancellation and control restoration | T01–T04 | Implemented (3 input-leak fixes); internal checks passed; manual acceptance pending |
| [T06](tickets/T06-regression-testing-and-handoff.md) | Regression evidence and handoff | T05 | Automated checks PASS; manual matrix pending user |

Order: **T01 → T02 → T03 → T04 → T05 → T06**. Work on one approved ticket/phase, verify, update documentation, report and stop. Ticket descriptions alone do not authorize gameplay implementation. Agents perform focused internal checks; the user performs manual Unity tests and Git writes.

## Conversation presentation contract

- Exploration and other current gameplay remain first person. Elijah becomes visible only for a valid active NPC conversation, standing naturally at the player's location; hide him again on exit.
- NPC speech: frame the NPC over Elijah's shoulder. Player speech: reverse the composition to show Elijah with part of the NPC in the foreground. Use safe authored/configurable framing and an appropriate two-shot fallback in confined spaces.
- Replace the large portrait-dominated conversation layout with a compact safe-area bottom panel, speaker name, readable text and Continue cue. Match the reference composition using TaxVenture's assets/style.
- Reveal text progressively. A dialogue-box tap or Continue while typing reveals that line only; a separate next tap advances, or closes after the fully revealed final line. One gesture must not perform both actions.
- Lock movement/look during conversation and camera return. Preserve the exact prior camera/input/visual state on successful end, cancel, failed start, disable and scene cleanup. No NPC animations or walking/running player implementation.

T03 contains the detailed acceptance criteria and proposed phases. Updating requirements does not start implementation or complete the remaining T02 phase.

Reference: [supplied conversation shot and dialogue layout](references/dialogue-shot-reference.png).
