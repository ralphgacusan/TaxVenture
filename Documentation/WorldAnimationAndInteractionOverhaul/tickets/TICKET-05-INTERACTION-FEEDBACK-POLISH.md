# TICKET-05 — Interaction Animation, Feedback, and Validation Polish

Status: Implemented; awaiting user Play Mode verification
Dependencies: Tickets 03 and 04  
Implementation order: 5

Current instruction (2026-10-15): Ticket 05 explicitly authorized and implemented. Stop for user Play Mode verification; do not start Ticket 06 without explicit instruction.

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

- [x] Add restrained hover/pick-up/release/settle and page/value feedback at the local interaction seam.
- [x] Make success and failure distinguishable with icon/text/shape as well as color.
- [x] Clear or recompute stale state when a value is removed, changed, or replaced.
- [x] Handle rapid repeated actions, cancellation, and repeated submission without duplicate feedback.
- [x] Keep UI responsive and avoid long blocking transitions.

## 5. Expected files/components/scenes

Expected: targeted drag/UI scripts, existing validation popup or feedback components, and actual desk/document scene presentation. Add a small reusable local helper only if it removes meaningful duplication; no global animation manager. Confirm paths and current diffs before edits.

## 6. Recommended architecture and Unity practices

Derive visual state from current authoritative validation/data. Subscribe/unsubscribe to transfer events correctly. Use one local animation coroutine/tween owner per item and cancel/replace it on repeated input. Never let a success icon outlive the answer it represents.

## 7. Phases and completion criteria

1. [x] Trace success/failure/replacement/reset events from source through UI.
2. [x] Apply focused visual feedback with cancellation safety.
3. [x] Verify that replacement and reset clear stale feedback.
4. [x] Compile and run targeted existing checks; update checklist and known issues.

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

Review Tickets 03/04 results and test only the connected seams. Preserve existing unrelated diffs. Update progress/known issues after verification, then stop. Ticket 06 requires explicit user instruction.

## 14. Out of scope

Changing grading, tax computation, compliance criteria, tutorial progression, adding a new sound/haptics dependency, or redesigning the global UI style.


## 15. Implementation record (2026-10-15)

Runtime-only changes; no scene YAML, prefab, data, validation, field key, transfer-manager or progression edits. Existing desk motion, page turns and check/cross badges are reused. Green still means matching type + semantic key, not a correct answer.

- `ValueTypeValidationFeedback`: clears on reinitialization, null value and disable; skips inactive playback; tint/restoration uses current selection color. Hover decoration excluded from image discovery.
- `ValueSelectionHighlighter`: resyncs selection when attached/enabled and does not paint over an active feedback pulse.
- `CaseFolderFieldRow.Refresh` and `ComputerFieldSlot.TryReceiveValue`: stop old feedback before displaying replacements. Existing computer Clear already restores feedback. `ValueClickDestination` plays feedback after the manager/field refresh.
- `CaseFolderUI.ClearRows`: immediately deactivates removed rows before deferred destruction.
- `DropFeedbackFX.Restore`: restores the captured outline only during an active effect, avoiding stale outline snapshots after restyling. Existing single-coroutine replacement remains.
- NEW `Assets/Scripts/UI/ValueHoverFeedback.cs` and `.meta`: a neutral underline on draggable values/drop fields, created at runtime only in Office_Level1. No layout/raycast interference or competition with selected tint, badge, glow or motion. Clears on exit/touch release/disable; mouse hover persists after a click.
- `ValueDragSource`: disables the floating label when its owner disappears; a newer drag owns the shared visual so old end events cannot hide it.
- `ValueTransferValidationPopup`: safe subscription lifecycle, one unscaled expiry, clears on success/selection/disable; CanvasGroup hiding keeps a self-hosted listener alive and does not block input. Generic rejection copy avoids the manager reason incorrectly describing a destination based on the incoming type.
- Office_Level1's popup is authored inactive and remains inactive. Local drop FX remain the visible response; no duplicate popup was activated. Manager success/failure events remain unchanged, one event per attempt. Stamp feedback already replaces its pending timeout; HUD release already clears its pointer-down guard before resolving submission. No submission/progression changes were needed from source inspection.

### Verification

- Scratch copy of `Assembly-CSharp.csproj` at `Temp/Ticket05/Ticket05.csproj`: ProjectReferences mapped to `Library/ScriptAssemblies/*.dll`; includes new script. Baseline and final compilation: **0 errors, 23 existing warnings**. Generated Unity project unchanged.
- `dotnet run --project Tests/ControlsRegression --no-restore`: **5,734 PASS**.
- `dotnet run --project Tests/DialogueRegression --no-restore`: **796 PASS**.
- `dotnet run --project Tests/ProgressionRegression --no-restore`: **PASS**.
- `python Tests/VerifyInteractionOutlines.py`: **FAIL**, known pre-existing Mobile_Renderer outline-width assertion. Supplemental in-memory run excluding only that assertion: all remaining checks **PASS**; test and renderer assets unchanged.
- Play Mode / device / visual verification: **NOT RUN**. Acceptance criteria and manual checklist remain pending user verification.

### User Play Mode checklist

1. Hover/touch value chips and fields: a quiet underline appears and clears on exit/touch release (mouse hover persists after a click). Select a value during/after a pulse: selection stays correct.
2. Drop the right kind of value (including an incorrect answer in that category), then a wrong kind: mint check versus coral cross; no duplicate badges or accumulated scale/rotation.
3. Rapidly replace values, clear calculator fields during a pulse, and close/reopen or change folder pages: no stale badge/tint/outline remains.
4. Cancel a value drag or close its source while dragging: no floating label remains. Repeat folder/book open/close, page turns and submission/retry; controls remain responsive.
5. The optional global rejection popup should remain absent in the normal scene. If temporarily enabled for testing, repeated failures replace the same message; success/cancellation hides it immediately, including at timeScale 0.

No scene reopen is required for these code changes. Stop here for the user's check; do not start Ticket 06.
