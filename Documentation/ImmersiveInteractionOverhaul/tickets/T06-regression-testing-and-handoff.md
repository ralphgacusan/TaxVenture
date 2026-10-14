# T06 - Regression evidence and final handoff

Status: **PLANNED - awaiting approval**
Dependencies: T05 integrated result. Next: final user review. Rough estimate: **8k-14k tokens**, excluding manual/device waiting and newly discovered fixes.

## Objective and existing verification seams

Confirm the implemented experience and leave evidence that another agent can trust. Re-inspect actual implementations and reports; never label a proposed feature or stale report as a pass.

Relevant checks: C# compilation and Git diff checks; Tests/VerifyProfileCards.ps1; Tests/ProgressionRegression/ProgressionRegression.csproj (PlayerProgressData only); Assets/Editor/CorkboardRegressionValidation.cs, PaperDropValidation.cs, PaperCollectionValidation.cs, PaperTrayValidation.cs; any focused tests added by T01-T05. Do not assume these cover new dialogue or camera behavior.

## Verification matrix

Record each as PASS / FAIL / NOT RUN, method, date, scene, resolution/device and evidence. Include actual results and reproduction steps, not this checklist alone.

- [ ] Movement joystick/right-side screen-swipe look indoors/outdoors; analog/dead zone; multi-touch; release/cancel/focus loss; desktop fallback; swipes never become unintended world taps.
- [ ] Normal first-person exploration and T01 controls before and after dialogue; conversation OTS presentation is active only during an NPC conversation.
- [ ] Shot/reverse-shot roles in office and town: NPC line over Elijah's shoulder with NPC face visible; player line over NPC's shoulder with Elijah face visible. Check tight spaces and document any dialogue-only two-shot fallback.
- [ ] Existing Elijah rig/assets, units, native geometry, scene references and foot grounding; verify the conversation visual does not move/teleport the gameplay player root, resize its collider, change shared NPC importers or add NPC animation.
- [ ] Movement/look stay frozen during dialogue; natural end and cancellation restore the captured first-person yaw, pitch, FOV, hands/visual visibility and input state, with controls resuming only after the return blend.
- [ ] Conversation entry, exit, cancel and failed start, plus the scoped safe-cancel/control-restoration and paper-viewer closing checks.
- [ ] Indoor NPC roles, outdoor NPCs, all four taxpayer targets, tutorial/guide NPC, ordinary completed NPCs and later available client presentation.
- [ ] Start/advance/typewriter/reveal/skip/end/cancel; opening interaction cannot skip the first line; a tap while typing reveals only that line and a distinct next gesture advances or ends the final line; repeated/busy/empty conversations, held input, tap-plus-button/submit duplication and callback counts.
- [ ] Speaker changes select the correct shot for all shared T02 NPC routes; grounded Elijah/NPC faces remain legible across height differences and tight spaces; compact bottom speaker/text/Continue panel fits safe areas without the oversized portrait layout.
- [ ] Marker availability/completion/unavailable/active states, shape distinction, range/occlusion and re-enable synchronization.
- [ ] Desk tools and objective notes, including swipe-close and correct return ownership.
- [ ] Profiles dropped/collected; tray entrance; corkboard categories and exact free pinning within bounds.
- [ ] Profile icon open, read, drag, select/topmost, edge limits, close/return, rapid toggles and controls restored after all transitions.
- [ ] Case/interview/objective/tutorial progression, successful/retry outcomes, counts and no duplicated rewards/drops.
- [ ] Pause, focus changes, scene unload, different supported screen sizes and actual mobile touch behavior.
- [ ] No new Console errors or missing references; measured performance and known limits recorded.

## Acceptance criteria

- [ ] All feasible automated/Editor checks actually run and their scope/results are recorded.
- [ ] Dialogue camera/UI regression criteria are recorded separately from first-person exploration checks; no player collider/root or shared NPC importer changes and no new NPC animation are present.
- [ ] Camera, hands/visual visibility and input return to the captured first-person state after the blend on both natural end and cancellation; input does not resume early.
- [ ] T02 session completion, completion/cancellation callbacks and success outcomes remain pending until the return blend completes; control release follows that same handoff.
- [ ] Manual/device checks performed by the user have explicit evidence; unavailable tests remain NOT RUN with concise instructions.
- [ ] No claimed overall regression pass with critical unresolved or untested flows.
- [ ] Each ticket has truthful status, modified file list, evidence, issues and continuation instructions.
- [ ] CURRENT_STATE, IMPLEMENTATION_PLAN decisions, PROGRESS and HANDOFF describe the actual delivered architecture.
- [ ] Existing uncommitted work remains preserved and no Git publishing/history action occurs without authorization.

## Deliverable and stop gate

Provide final file/change summary, test matrix, known issues with severity/reproduction and exact next action. Resolve only scoped test failures with an explicit bounded work unit; never suppress assertions or edit reports to pass. If user/manual/device acceptance is pending, mark it pending instead of declaring the overhaul complete.

No next-ticket token estimate is necessary when all tickets are accepted; estimate any specific unresolved fix separately. User performs Unity manual testing and Git write actions. Stop for final user review.
