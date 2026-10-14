# T06 — NPC availability and completion markers

Status: **PLANNED — awaiting approval**
Dependencies: T04 truthful conversation status and T03 modes; coordinate T05 framing. Next: T07. Estimate: **6k-10k tokens**.

## Objective and inspected entry points

Make available, completed and unavailable conversations visually distinguishable above NPCs. Existing material-color state feedback is insufficient.

Primary files: `Assets/Scripts/FSM/NPC/NpcVisualIndicator.cs`, `NpcStateMachine.cs`, NPC state files; office interactable adapters; `Assets/Scripts/SideQuest1/TaxpayerNpcInteractable.cs`, `TutorialNpcController.cs` and town events; NPC prefabs/scene targets in `Assets/Scenes/Office_Level1.unity`.

## Scope

- Evolve the existing indicator path rather than add a second competing state authority.
- Represent available with a recognizable floating triangle/marker and completed with a different shape/check plus color. Unavailable/active/out-of-range must hide or change appropriately.
- Use actual interview/profile/conversation/quest conditions and current interaction availability. Initialize from current state as well as later events; unsubscribe on teardown.
- Ordinary colleague/tutorial NPCs need adapters because their current completion paths do not use the same FSM.
- Distinguish four taxpayer-target progress from office case interview/presentation progress. An already-interviewed client may still have a useful later conversation.
- Anchor above head/bounds, follow the NPC if moved, billboard, avoid faces and dialogue framing; use restrained float/pulse and configurable distance/occlusion.
- Reuse suitable assets/materials, avoid per-frame material creation and click-blocking colliders, and keep mobile cost bounded.

## Acceptance criteria

- [ ] Player can distinguish available from completed using shape as well as color.
- [ ] Four taxpayer-target completion matches actual conversation/profile state.
- [ ] Ordinary NPC exhaustion and later case-dependent availability remain truthful.
- [ ] Cancelled/rejected dialogue does not mark completion.
- [ ] Markers follow/face appropriately, respect range/visibility and do not clutter faces or shots.
- [ ] Active/unavailable NPCs use the defined suppression state; re-enabling/scene reload synchronizes correctly.
- [ ] Marker geometry/UI does not consume world interaction or joystick input.

## Verification and handoff

Compile; focused mapping/state tests. Manual: inspect each NPC before, during, after successful conversation, after cancellation, while unavailable/out of range/occluded, after component re-enable, and on later case phases. Test readability against indoor/outdoor backgrounds and accessibility without relying on color.

Document visual mapping, anchors/configuration, authoritative state sources, results and unrun cases. Do not add NPC movement/animation or change quest completion rules. Stop for approval.

