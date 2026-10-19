# T03 — Record a concern and stamp the preliminary hold

Status: Implemented — internally verified; user Play Mode pending (2026-10-17). Blocked by: T02.

## Visible accomplishment

The player links the paid blanket-washing clue to the summary, attaches a finding note, presses Not Ready and receives the objective to visit Rosa at Stall 04.

## Work

Use the existing value transfer with exactly three fields on the detached slip: Entry to check, Clue, Reason. The book supplies reasons and concise contextual clues in exactly two added leaves, Reading Records and Review Notes. May total (`SummarySubtotal`) is the entry to check; the clue must say something meaningful about the proposed issue. Automatically check a complete link on field edits. Show short neutral/unsupported feedback; a supported link reveals the handwritten note and raises FindingRecorded once. Changes invalidate confirmation. Keep field objects alive for normal drop/aim feedback and use per-point coverage so a book overlapping one area cannot disable all exposed targets. The guide observes domain success rather than filling boxes or giving the answer.

Reuse physical stamps for a preliminary assessment. Require an unresolved-payment reason and F1 reference. Preliminary commit records its own assessment/revision; it must not invoke terminal filing state, Auditor transition, return printing or case completion. Add modest physical press/ink now using existing motion; T08 completes shared final-stamp polish. A wrong preliminary choice prompts guided correction without a performance deduction or fake successful hold.

After valid initial hold, objective/map point to Rosa near the plaza. Reopening finding/stamp pages shows stored state; no repeat quest or EXP. Start packet revision tracking in the domain model so later reuse is not tied to a specific paper view.

## Acceptance

Internal status (source checks only; each box also needs the user's Play Mode check):

- [x] F1 refers to S1/N1 and is visible as an unresolved concern. *(Findings page shows status "Needs checking")*
- [x] Selection and evidence attachment work without free typing or precision drawing. *(tap-to-pick / tap-to-place or drag on the page)*
- [x] Initial Not Ready ink and reason appear; no final audit/success path fires. *(source checks: no assessment, state, audit or completion calls)*
- [x] Guided correction is free; preliminary stamping grants no reward.
- [x] Rosa waypoint activates only after this initial review, with office exit available.

## Verification/handoff

Test initial review state transitions, reason prerequisites, duplicate actions and strict separation from final assessments. Scratch compile and relevant guide/progression tests. User later checks paper note, preliminary stamp and route prompt. Next T04.


## Removable Findings paper follow-up — 2026-10-18

Implemented — internally verified; user Play Mode pending. Findings now prints as a taped removable working paper using the existing folder row/value components and FloatingWindow document order. Moving the sheet changes presentation only. Drop inside any visible folder page to flip back to Findings and animate reattachment. See PROGRESS for exact paths and verification.

- [ ] Lift the sheet by its heading/blank area; leave it beside the folder while turning pages.
- [ ] Tap or drag summary/payment-note values into the loose sheet, then select/place its reason.
- [ ] Tap sheet, book, folder and Notes in turn; the touched document comes forward.
- [ ] Drop on Findings, then repeat on another page: the folder returns to Findings and the paper attaches.
- [ ] Repeat detach/attach and close/reopen folder/settings without lost data, duplicate papers or stuck input.
- [ ] Confirm legibility, paper/tape styling, peel/return motion, and read-only findings after stamping.


### Follow-up acceptance: detach-first editing and larger paper

- [ ] Attached: selected values cannot be placed in any finding field; reason/source rows cannot be selected or dragged.
- [ ] Detached: larger type and paper remain readable beside the folder, with raised/tilted depth and movement lean.
- [ ] Transfer works after lifting, then stops during return and while attached again.
- [ ] Return, cancelled return, folder close/reopen and settings restore the larger loose pose correctly.


## Explicit comparison redesign acceptance — 2026-10-18

Implemented — internal checks recorded in PROGRESS; user Play Mode pending. This section supersedes older instructions to choose a reason directly from the sheet.

- [ ] Sheet has exactly three fields and a short purpose/guide; no reason choice rows.
- [ ] Book includes Reviewing a Packet and selectable Review Principles with general explanations.
- [ ] Missing fields produce incomplete feedback; random complete combinations produce no-discrepancy feedback without advancing the finding objective.
- [ ] Reference/contact/location metadata is not accepted as proof of an omission.
- [ ] A supported explicit comparison reveals the handwritten observation and advances the guide once, with no reward or penalty.
- [ ] Editing any part clears confirmation and the old conclusion; stamping cannot bypass a comparison.
- [ ] Reattach/reopen restores a confirmed observation without restarting its reveal; attached editing stays blocked.
- [ ] Check book/paper text fit, button targeting, statuses, handwriting timing and focus while the paper sits beside the book/folder.


## Current polish acceptance — compact automatic slip (2026-10-18)

This supersedes the earlier explicit-comparison/button checklist above. Internal checks are in PROGRESS; user Play Mode pending.

- [ ] Only two concise added book pages; ordinary headings and three transferable reasons.
- [ ] Smaller slip follows the desk paper angle, with Entry to check/Clue/Reason and minimal copy.
- [ ] Filling three fields checks automatically; no manual button or discrepancy headline.
- [ ] Tapping a source then a slot, and dragging a source into a slot, both work.
- [ ] Aim frame, hover and accepted/rejected field feedback match other document fields.
- [ ] Touching the book does not disable exposed slip fields; covered areas still respect front-to-back order.
- [ ] Supported link writes a short note; editing rechecks it; attached editing stays blocked.
- [ ] Repeated return/reopen and stamp handoff preserve data and controls.


### Latest concrete-payment refinement — 2026-10-18

Implemented — internally verified; user Play Mode pending. Supersedes older whole-page selection wording: choose May total as Entry to check, the paid blanket-job statement as Clue, and Unlisted payment from Review Notes. Other summary rows fail validation. Arthur introduces the book note with an indirect hint. Always-stacked captions and values, backward pitch and a shared sheet/control taper are implemented.

- [ ] Check the slip's lean beside both desk papers, readability and stacked captions in all states.
- [ ] Try an unrelated summary row, then May total; confirm only the supported link reveals the handwritten note and permits the preliminary hold.
- [ ] Verify tap/place and drag/drop, aim frame, overlap/focus and return to any folder page still work.
- [ ] Check whether a new player can infer the total/payment/book-rule link without the conclusion giving it away.

Internal evidence: scratch build 0 errors/29 baseline warnings; Level2Regression 485 checks pass. Next: user acceptance of T03/T04, then T05.


### Latest clipping/transition/stamp follow-up — 2026-10-18

Implemented — internally verified; user Play Mode pending. Handwriting now has twice the height at fixed size 28. A taller paper and lower footer preserve the whole note; lift scale derives from its attached projection (+6 percent) and angles interpolate from/to the actual folder plane. Stamps use cached visible mesh bounds, shared highlight, a matching gesture-blocking pointer surface, document focus, steady camera-plane movement and uniform depth-compensated press scale. Tap-only focus, canceled-touch return and paused/modal input guards prevent unintended stamps.

- [ ] Entire supported note is readable without smaller text.
- [ ] Repeat detach/return; check no size/angle snap, including return during peel.
- [ ] Click stamp center/edges to focus, then drag over the book and folder; no paper drag leaks beneath it.
- [ ] Check outline, steady size/shape, exact ink preview and satisfying press/rebound.
- [ ] Check rejection, valid Not Ready, pause/cancel and closing during motion; rules/rewards stay intact.

Next: user visual/feel acceptance, then existing ticket order.


### User screenshot follow-up — 2026-10-18

Implemented — internally verified; user Play Mode pending. Fixed-font preferred text height now determines the note/paper height. Unsupported links clear the three fields for retry. Desk drops settle; automatic filing/stamp page turns share cancellation/restoration and do not overlap manual arrows. Replacement stamps have stand affordance and enlarged handle/body targets; lifted tools always render visibly over desk geometry. Arthur's hint slides above desk/profile-reading papers.

- [ ] Check the whole last word/line of Needs checking without smaller handwriting.
- [ ] Try a wrong complete link, then drag a new set into the cleared fields; validate a correct retry.
- [ ] Drop/reattach while on another page; repeat page turns and pick up stamps before/after a turn.
- [ ] Tap each stand handle and each lifted body, confirm outline, focus and visible aim/press over the folder and table.
- [ ] Check free desk drop settling and Arthur's upward/downward hint movement.

The book follow-up also changes shared dragging: check the original Level 1 book still opens/closes/returns correctly.


### Stamp visibility/aiming follow-up — 2026-10-18

Implemented — internally verified; user Play Mode pending. Floating/dragging/pressing now remain in the near-camera tool plane. The lower slot is raised; aiming shows a moderate readable lean with the pad over the preview. The real paper-local ink position and decision are unchanged. Document capture runs after motion and maps to the camera viewport.

- [ ] Both complete stamps remain visible after opening the set.
- [ ] Grab by handle/body and drag across every area of the folder; model and indicator remain visible together.
- [ ] Check aiming angle, pad-to-preview alignment and the whole press/rebound/return.
- [ ] Test correct Not Ready, rejected Ready, off-page return, overlap with book/notes and repeat open/close.

Internal source/logic verification cannot replace these rendered checks.


### Tap/viewport follow-up — 2026-10-18

Implemented — internally verified; user Play Mode pending. Pickup paths now open idempotently on a tap, including the later touch-release world interaction. EventSystem stand targets and Input System press edges remove a drag requirement. Model-bound fitting uses the real camera viewport/near clip and reserves room below Arthur's hint. Held tools keep front order and stable isolated render layers.

- [ ] Quick-tap each desk handle/body without moving; set opens and stays open after release.
- [ ] Aim at the top, corners and lower edge of the folder; whole model remains visible with the indicator.
- [ ] Check actual model/ink relationship at a constrained screen edge, press/rebound and rejected/supported decisions.
- [ ] Check book/notes focus, mouse/touch, letterboxed views, pause/cancel and repeated put-away/reopen.

Internal verification: build 0 errors/29 baseline warnings; Level2Regression 524 PASS; ControlsRegression 5,734 PASS. Next remains user T03/T04 acceptance, then T05.

### Shared capture and pad landing — 2026-10-19

Implemented — internally verified; user Play Mode pending. Both stamp models now share one tool capture/layer 29 and refresh their outline targets. The rubber pad projects onto the actual indicator/ink anchor at impact, staying there through pulse and paper thump. Internal projection checks cover multiple ray slopes, depth offsets and pulse scales.

- [ ] User checks Ready's highlight and complete Not Ready visibility while floating/aiming over the folder.
- [ ] Check both pads visibly land on the indicator and ink, then rebound smoothly; repeat rejected/accepted presses and put-away/reopen with other desk papers present.

Build: 0 errors/29 baseline warnings. Level2Regression: 546 PASS; Controls: 5,734 PASS; Dialogue: 799 PASS; Progression: PASS; Guide: 714 PASS. No rendered result is implied. Next: existing T03/T04 user acceptance, then T05.

