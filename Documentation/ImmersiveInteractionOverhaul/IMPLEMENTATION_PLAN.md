# Implementation plan

Updated 2026-10-15. Preserve current first-person exploration and T01 controls. Use existing viewpoint, control-lock and interaction APIs. Scope: consistent conversations, conversation-only Elijah visual and cinematic shots, readable dialogue UI/typewriter, truthful NPC markers and focused integration. Exploration stays first person. No exploration camera overhaul, NPC animation or player locomotion work is assigned.

## Architecture

- **T02: unify dialogue flow.** Build on existing DialogueUI and NPC adapters with one active conversation, reliable start/advance/end/cancel handling and preserved callbacks. Reuse CameraController's current control APIs. Preserve dialogue data, conditional outcomes, question choices, tutorial events, rewards and replay rules. Completion, cancellation, interview progress and exhausted content are distinct states. No prerequisite camera framework is needed.
- **T03: cinematic conversation presentation consumes T02 events.** Configure an existing Elijah standing/idle visual that is hidden outside conversations; inspect actual mesh units/pose/feet and native rendering, not filenames or imported animation/culling bounds. Fit the visual independently without resizing/teleporting the player root/capsule or editing shared NPC importers. Inspect compatible existing rig/idle assets; no walking/running setup.
- **T03 camera shots:** NPC lines use an over-Elijah-shoulder view of the NPC; player lines use the reciprocal view of Elijah with the NPC shoulder/side in frame. Freeze actual player motion and look; use one pose writer under the current camera owner, appropriate head/shoulder anchors, smooth speaker/entry/return blends and safe two-shot fallback. Verify walls, furniture, different heights, and model intersections. Do not reuse the abandoned exploration setup or global body fitting.
- **T03 UI/input:** compact safe-area bottom panel, speaker name/text/Continue and no large foreground portrait. Reveal via TMP visible-character count, rich-text-aware pacing and punctuation pauses. Dialogue-box taps and Continue share one gesture gate: while typing reveal only; after reveal a distinct next tap advances. Final-line close also needs a distinct tap. Opening click, held touch, duplicate submit/button events and gameplay taps must not skip lines.
- **T03 end ordering:** coordinate presentation completion with the existing T02 session rather than create a competing dialogue manager. On the final line or cancellation, finish camera return, hide/restore the conversation visual and hands, restore captured first-person orientation/FOV/controls, then release the conversation and dispatch its outcome. Keep locks through the return; do not fire success on cancellation. If disable/unload prevents a blend, clean up immediately using valid owned references, without restoring over another owner or touching destroyed scene objects. Current T02 releases immediately on terminal cleanup, so this ordering is a planned integration seam, not an existing capability.
- **T03 delivery checkpoints:** (1) standing Elijah visual and grounding preview, (2) speaker shots plus reliable first-person return, (3) bottom UI/typewriter and tap arbitration, (4) conversation integration/regression. Require explicit continuation and update evidence after each checkpoint.
- **T04: indicators read actual state.** Adapt NPC availability, interviews, profile and remaining-action data. Require initial synchronization, event cleanup, distinct shape plus color, restrained animation, configurable range and conversation suppression. Avoid click-stealing colliders and per-frame material allocation.
- **T05: verify existing interactions together.** Check safe cancellation, failed starts, repeated close and return to the prior input state across conversations, desk, notes, board and profile viewer. Preserve locks through final paper return. Fix only reproduced scoped failures; do not introduce a shared camera framework or redesign working transitions/notes policy as a prerequisite.
- **Preserve papers/tools.** Current profile opening/drag/layering/closing, readable layout, task refresh, callbacks, free paper placement and delayed input restoration remain authoritative.

## Dependencies and sequence

T01 (retained) → T02 (conversation lifecycle) → T03 (dialogue presentation) → T04 (NPC indicators) → T05 (integration) → T06 (regression/handoff). T04 consumes T02 status and coordinates visibility with T03. All future units need explicit approval.

Use one writer per CameraController, DialogueUI and scene. Read-only content mapping or test preparation can be delegated; shared scene/script writes must be serialized.

## Risks and acceptance

| Risk | Required behavior/evidence |
|---|---|
| Mixed input and multiple touch routes | Retain T01 pointer ownership, swipe/UI isolation and tap discrimination; user tests multi-touch |
| Empty/busy dialogue and synchronous callbacks | Reject overlap, clean failed starts and invoke progression callbacks once after valid completion |
| Interview completion versus later actions | Preserve case/presentation, profile, tutorial and replay rules; truthful markers |
| Cancellation or incorrect input return | T02 cleans its conversation state; T05 tests all existing interaction paths and fixes demonstrated leaks without a camera overhaul |
| Model fit and camera framing | Validate actual standing geometry, grounded feet, shoulder visibility, obstruction fallback and hiding outside conversation; preserve player physics dimensions |
| Typewriter input duplication | Rich text stays intact; opening/reveal/advance/close are distinct gestures; pauses/disable clean owned routines |
| Conversation return ordering | Do not unlock or run success callbacks before return finishes; cancel restores state without quest completion |
| Paper/UI conflicts | Preserve raycast priority, readable layouts and locks through final closing |
| Notes currently allow walking/look | Preserve existing policy, task refresh and swipe close; T05 tests current behavior |
| Legacy scene references | Inspect Office_Level2 before claiming cross-scene acceptance |
| Historical progression warnings | Reproduce and document; no unrelated tax/reward/save/routing fixes |

## Verification and stop gate

Compile affected runtime/Editor code and inspect references. Run the smallest relevant production checks; existing paper/progression tests cover only their stated scope. Never suppress assertions or edit reports to claim success.

User manually tests first-person exploration before/after conversation, standing Elijah and both speaker compositions, tight-space fallbacks, typewriter and dialogue-box/Continue reveal/advance/close, office/town NPCs and interview outcomes, desk/notes/board/profile, pause/focus and supported resolutions/safe areas. T06 records actual results and unrun cases. Compilation/math/source inspection are not gameplay acceptance.

After each approved unit update CURRENT_STATE, ticket status, PROGRESS and HANDOFF; report changed files, checks, limits and next-unit token range. Stop for explicit continuation. User handles Git write actions. No gameplay work starts from this documentation revision.
