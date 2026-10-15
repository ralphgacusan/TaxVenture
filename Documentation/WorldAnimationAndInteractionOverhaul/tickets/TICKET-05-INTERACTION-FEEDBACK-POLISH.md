# TICKET-05 — Interaction Animation, Feedback, and Validation Polish

Status: Planned  
Dependencies: Tickets 03 and 04  
Implementation order: 5

Current instruction (2026-10-15): setup only. Do not begin or resume this ticket without a new user instruction. Existing partial implementation is historical, unverified work.

## 1. Objective and expected outcome

Make desk and answer interactions feel immediate and make correctness feedback reflect the current underlying value/validation state, including replacement and correction.

## 2. Existing systems and files to inspect

- Ticket 03 drag/place state and Ticket 04 page/value visuals
- `Assets/Scripts/Data/DragDrop/ValueTransferManager.cs`
- `ValueTransferValidationPopup.cs`, `ValueTypeValidationFeedback.cs`, `ValueSelectionHighlighter.cs`
- Existing result/assessment UI components and audio hooks where applicable
- `Assets/Scripts/Interactables/DeskItemHighlight.cs` and current object highlight behavior

## 3. Dependencies and prerequisites

- Tickets 03 and 04 are implemented and their active state signals are understood.
- Reuse existing transfer success/failure events and current answer validation source of truth.
- Optional audio is only used if an existing sound and supported path are available.

## 4. Exact implementation scope

- [ ] Add restrained hover/pick-up/release/settle and page/value feedback at the local interaction seam.
- [ ] Make success and failure distinguishable with icon/text/shape as well as color.
- [ ] Clear or recompute stale state when a value is removed, changed, or replaced.
- [ ] Handle rapid repeated actions, cancellation, and repeated submission without duplicate feedback.
- [ ] Keep UI responsive and avoid long blocking transitions.

## 5. Expected files/components/scenes

Expected: targeted drag/UI scripts, existing validation popup or feedback components, and actual desk/document scene presentation. Add a small reusable local helper only if it removes meaningful duplication; no global animation manager. Confirm paths and current diffs before edits.

## 6. Recommended architecture and Unity practices

Derive visual state from current authoritative validation/data. Subscribe/unsubscribe to transfer events correctly. Use one local animation coroutine/tween owner per item and cancel/replace it on repeated input. Never let a success icon outlive the answer it represents.

## 7. Phases and completion criteria

1. [ ] Trace success/failure/replacement/reset events from source through UI.
2. [ ] Apply focused visual feedback with cancellation safety.
3. [ ] Verify that replacement and reset clear stale feedback.
4. [ ] Compile and run targeted existing checks; update checklist and known issues.

## 8. Integration requirements

Use existing validation rules and transfer events. Do not add duplicate answer state, alter pass/fail rules, lock controls longer, or make animation a prerequisite for gameplay progress.

## 9. Internal verification

Inspect event subscription lifecycle, transfer/replacement/clear routes, and existing tests. Compile. If no Editor run is available, mark visual behavior NOT RUN and leave the manual checklist.

## 10. Manual testing checklist

- [ ] Hover, pick up, drag, release, and cancel book/folder interactions rapidly.
- [ ] Transfer a correct and an incompatible value; confirm distinct text/icon/color feedback.
- [ ] Replace/remove a previously correct value; confirm stale success clears immediately.
- [ ] Correct an incorrect answer and confirm the current state is shown without duplicate popups.
- [ ] Confirm no animation blocks the next input or leaves controls locked.

## 11. Acceptance criteria

- [ ] Physical interaction feedback is subtle and responsive.
- [ ] Correct/incorrect states differ without color alone.
- [ ] Feedback always matches current answer/transfer state.
- [ ] Rapid input/cancel/retry creates no stuck animation or duplicate event.

## 12. Risks and rollback

Feedback can drift from business rules if it stores its own validation result. Derive state from existing manager and clear local presentation on each changed value. Roll back the local animation while keeping the authoritative transfer path.

## 13. Continuation instructions

Review Tickets 03/04 results and test only the connected seams. Preserve existing unrelated diffs. Update progress/known issues after verification, then proceed to Ticket 06.

## 14. Out of scope

Changing grading, tax computation, compliance criteria, tutorial progression, adding a new sound/haptics dependency, or redesigning the global UI style.
