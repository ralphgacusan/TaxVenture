# World Animation and Interaction Overhaul

This initiative improves NPC animation and conversation feedback, desk document handling, printed document presentation, answer feedback, and office navigation. It is intentionally separate from `Documentation/ImmersiveInteractionOverhaul`.

## Project baseline

- Unity `6000.4.5f1`, Universal Render Pipeline `17.4.0`.
- Target scene: `Assets/Scenes/Office_Level1.unity` only. Other office scenes are outside this initiative scope.
- The gameplay is Android-first and uses Input System `1.19.0`, UGUI `2.0.0`, TextMeshPro, and AI Navigation `2.0.12`.
- The working tree was already dirty on `feat/immersive-interaction-overhaul` when this initiative began. Preserve those in-progress changes and do not edit the sibling initiative's documentation.

## Execution gate

Tickets 01-04 are complete (user-verified; walking deferred). Do not start Ticket 05 or later until the user explicitly asks.

## Start here

Before implementation, read `AGENTS.md`, `PROJECT_AUDIT.md`, `IMPLEMENTATION_PLAN.md`, `PROGRESS.md`, `DECISIONS.md`, and the active ticket. Inspect current Git status and the live files mentioned in the ticket.

## Tickets

| Order | Ticket | Status | Dependency |
|---:|---|---|---|
| 1 | [Reusable NPC Animation Framework](tickets/TICKET-01-NPC-ANIMATION-FRAMEWORK.md) | Complete (walking deferred) | Audit |
| 2 | [NPC Conversation Animations](tickets/TICKET-02-NPC-CONVERSATION-ANIMATIONS.md) | Complete | Ticket 01 |
| 3 | [Desk Interaction Overhaul](tickets/TICKET-03-DESK-INTERACTION-OVERHAUL.md) | Complete | Audit |
| 4 | [3D Document Presentation](tickets/TICKET-04-3D-DOCUMENT-PRESENTATION.md) | Complete | Ticket 03 |
| 5 | [Interaction Feedback Polish](tickets/TICKET-05-INTERACTION-FEEDBACK-POLISH.md) | Planned | Tickets 03 and 04 |
| 6 | [Dynamic Minimap](tickets/TICKET-06-DYNAMIC-MINIMAP.md) | Planned | Audit and tutorial review |

## Tutorial reference

The [minimap tutorial](https://youtu.be/YNbuhDiaXg4) uses an orthographic overhead camera, a RenderTexture shown in a RawImage, a fixed north-up camera that follows the player in `LateUpdate`, and separate map indicators. It disables the minimap camera's shadows and AudioListener and excludes HUD layers from its culling mask. This initiative will adapt those ideas to the project's URP camera data, office occlusion, actual NPC transforms, and safe-area UI. The transcript was inspected; its wording is not copied here.

## Status and handoff

Current status is tracked in [PROGRESS.md](PROGRESS.md); architecture choices are in [DECISIONS.md](DECISIONS.md), and blockers are in [KNOWN_ISSUES.md](KNOWN_ISSUES.md). Use the [handoff template](handoffs/HANDOFF_TEMPLATE.md) when another session must continue.
