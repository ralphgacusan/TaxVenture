# TICKET-02 — Lively NPC Conversations and Contextual Animation

Status: Complete (2026-10-15) - user confirmed in Play Mode via Unity Remote
Dependencies: Ticket 01 (complete)
Implementation order: 2

Started on explicit user instruction (2026-10-15), on top of uncommitted Ticket 01 changes at the user's request.

## 1. Objective and expected outcome

Use Ticket 01 to make existing NPC conversations visibly responsive. Talking/listening states should be distinguishable when the available rigs and clips support them, and every exit—including cancellation or disable—must restore the appropriate prior idle/completed state.

## 2. Existing systems and files to inspect

- `Assets/Scripts/UI/DialogueUI.cs` and `DialogueSession.cs` (`CurrentOwner`, `CurrentLine`, `LineChanged`, `DialogueStarted`, `DialogueEnded`)
- `Assets/Scripts/FSM/NPC/NpcStateMachine.cs` and states
- Client, colleague, auditor and receptionist interactables
- Ticket 01 driver/profile and actual target Animator controllers
- `Assets/Scripts/Player/CameraController.cs` only if a read-only active-player reference is needed; it remains camera/control owner

## 3. Dependencies and prerequisites

- Ticket 01 driver is implemented and wired.
- Reuse dialogue events and identify who is speaking for each line. Preserve session owner filtering and callbacks.
- Gestures and look-at behavior require compatible assets/references confirmed in Ticket 01.

## 4. Exact implementation scope

- [x] Subscribe safely to the current dialogue UI's start/line/end events and filter to the owning NPC.
- [x] Select talking vs listening for NPC/player lines using the current line speaker.
- [x] Restore the NPC's current FSM-mapped state after normal completion/cancellation; disable removes subscriptions and re-enable resynchronizes.
- [x] Add subtle, interruptible contextual orientation or reaction only where a valid target/state exists.
- [x] Use variation only when deterministic/configurable and visually unobtrusive.

## 5. Expected files/components/scenes

Expected: Ticket 01 driver and a small conversation adapter or targeted integration in existing NPC interaction components; actual Office_Level1 NPC references. Avoid duplicating the same event subscription across all interactables if one reusable adapter is sufficient. Confirm any actual path before change.

## 6. Recommended architecture and Unity practices

Treat animation as presentation driven by `DialogueUI` events, not a replacement dialogue FSM. Subscribe/unsubscribe in component lifecycle methods. Use current owner and line speaker as the source of truth. Avoid animation polling, random gestures on every line, and direct changes to game progression.

## 7. Phases and completion criteria

1. [x] Map dialogue line speakers to owner/player and NPC presentation states.
2. [x] Add cancellation-safe event integration and compile. (0 errors.)
3. [x] Configure supported responses on actual office NPCs. (Automatic through the 11 existing drivers; tunable per NPC.)
4. [x] Verify normal, completed, cancelled, and disabled-object exits statically and in available Editor checks.

## 8. Integration requirements

Do not alter dialogue progression, current owner, input, camera mode, tutorial events, case flags, callbacks, rewards, or replay rules. A fallback can use a supported idle state and simple facing response, but must be labeled as a fallback rather than a talk gesture.

## 9. Internal verification

Inspect subscription symmetry and owner filtering; compile; check all four interactables' existing dialogue paths. If the existing Editor is already playing or busy, do not interfere. Do not report a gameplay pass from static event inspection.

## 10. Manual testing checklist

- [ ] Start and finish each available NPC conversation; confirm line speaker changes talking/listening behavior.
- [ ] Cancel or disable the conversation owner; confirm no event leak and the NPC returns to a valid state.
- [ ] Confirm completed NPCs remain visually completed/idle as appropriate.
- [ ] Confirm the interaction prompt, dialogue advance input, and camera restore still work.

## 11. Acceptance criteria

- [x] NPC responds to dialogue state through Ticket 01. (Procedural fallback on all 11 NPCs; Animator Talking/Listening states are used automatically if added later.)
- [x] Talking/listening differ only where compatible assigned animation states exist.
- [x] NPC returns to correct supported state on all dialogue endings.
- [x] Dialogue and gameplay callbacks remain unchanged. (No dialogue/interactable script edits; dialogue regression 796 PASS.)

## 12. Risks and rollback

Stale events can animate a different NPC or leave a character in a talk state. Keep subscriptions scoped to owner and undo only the adapter/component integration if it affects dialogue.

## 13. Continuation instructions

Read the active initiative docs and inspect Ticket 01's supported-state evidence. The reusable driver subscribes to the existing `DialogueUI` events and filters `CurrentOwner` to its own transform/children. `DialogueSpeaker.Player` selects Listening; `Npc` selects Talking; `DialogueEnded` resyncs the current FSM state; disable unsubscribes and re-enable syncs an active session. No dialogue or interactable scripts were changed. Imported character clips and Animator states remain unverified/unassigned, so continue to Ticket 03 while preserving that gap.

## 14. Out of scope

New dialogue lines, dialogue UI redesign, new facial animation, voice lip-sync, camera changes, or game/NPC FSM redesign.

## 15. Implementation (2026-10-15)

All in `Assets/Scripts/FSM/NPC/NpcAnimationDriver.cs`; no dialogue, interactable, camera or scene edits. No talk/listen clips exist, so this is a labeled procedural fallback layered on each NPC's idle (static mesh parts, or Humanoid head/neck/spine bones on top of Standing Idle).

- Owner-filtered `LineChanged`, new `LineRevealed` subscription and `DialogueEnded`; symmetric unsubscribe in `OnDisable`.
- Facing: head turns up to `conversationTurnLimit`, the upper body turns the rest up to `maxConversationBodyTurn` (35 deg). The NPC root, colliders and conversation viewpoints never rotate (walker roots still turn via `NpcIdlePacing`).
- NPC line (Talking): speech head motion while text types, settling after `LineRevealed`; slight forward lean; opening beat by mood from punctuation: statement nod, question head tilt, "!" strong nod and lean, "..."/"um"/"well," look-aside.
- Player line (Listening): head tilt, slight lean back, acknowledging nods after the line is shown (repeating every 2.6 s); a player question gets a brief look-aside.
- Variation is deterministic: line index parity picks the side, punctuation picks the mood.
- Endings: Completed gives a farewell nod; Cancelled just settles. Idle actions pause and resume 1.5-3 s later. Disable restores the model pose.
- Tuning: `conversationStrength` (0-2) and `maxConversationBodyTurn` per NPC.

Verification: runtime and editor builds 0 errors; dialogue 796, controls 5,734 and progression regressions PASS. No Play Mode test.

## 16. Manual checks

1. Talk to a static NPC (Client, Receptionist, Auditor) and a rigged one (Jhun, Marie): the NPC turns head and upper body toward you, moves while its text types and settles when typing ends.
2. Lines ending in "?" tilt the head, "!" give a strong nod, "..." a look aside; player lines get a head tilt and nods.
3. Finish a conversation: a farewell nod, then idle resumes after a moment. Close one early: no nod, smooth return.
4. Other NPCs keep idling during your conversation; no head parts detach; the camera, dialogue advance and prompts behave as before.

## 17. Elijah (player) conversation motion (user request, 2026-10-15)

- The talk/listen math moved to the shared static `Assets/Scripts/FSM/NPC/ConversationMotion.cs`; `NpcAnimationDriver` now calls it (same behavior as section 15).
- `Assets/Scripts/Player/ConversationActorVisual.cs` (the conversation-only Elijah model) layers the same motion on its sampled idle: his own lines are speaking, the NPC's lines are listening, and his head turns toward the NPC up to `headTurnLimit` (25 deg). Bones are found by Mixamo name (Spine, Spine1/Spine2, Neck, Head). Because the idle is sampled at a capped rate, bones are reset to the last sampled pose every frame before the offset, so offsets never accumulate.
- New Inspector fields on ConversationActorVisual: `conversationMotion`, `conversationStrength`, `speechDegrees`, `headTurnLimit`. No setup tool and no scene edit; the gameplay player root is untouched.
- Verification: builds 0 errors; dialogue 796, controls 5,734 and progression regressions PASS. No Play Mode test.
- Manual check: in an NPC conversation, Elijah moves his head while his own lines type (nod, tilt on "?", strong nod on "!"), tilts and nods while the NPC speaks, and looks toward the NPC. No bone drift or twisting builds up over a long conversation.

## 18. Play-test fixes: first tap and Milo camera (2026-10-15)

Reported: the first tap on any NPC only focused it (Idle -> Waiting in Editor.log) and a second tap started the conversation; Milo's conversation sometimes kept the first-person view.

- First tap: tests run through Unity Remote (touch). `MobileInputGuard` required the Input System `tap`, which only fires when the finger lifts within 0.2 s, so ordinary or Remote-delayed taps were dropped. Taps are now a touch that ended normally within 0.5 s and moved less than max(EventSystem drag threshold, 2% of the short screen side); the same tolerance applies to `IsDragOrCancelled`. Affects all world taps and pointer clicks that use the guard (NPCs, office interactables).
- Milo camera: `DialogueCameraDirector.TryFrame` fell back to first person whenever props on the approach side blocked every shot, so it depended on where the player stood. It now also tries placing the conversation Elijah at 40/80/120 degrees either side of the NPC (only spots with a clear line to the NPC and free space) before falling back.
- These files belong to the sibling ImmersiveInteractionOverhaul (T03/T05); its documentation was not edited.
- Verification: builds 0 errors; dialogue 796, controls 5,734 and progression regressions PASS. No Play Mode test.
- Manual check: a single normal tap on each NPC starts the conversation; swipes still turn the camera and do not start conversations. Talk to Milo from several sides: an over-shoulder or two-shot angle appears each time unless he is fully enclosed.
