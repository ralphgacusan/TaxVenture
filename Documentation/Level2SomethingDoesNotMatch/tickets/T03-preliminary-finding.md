# T03 — Record a concern and stamp the preliminary hold

Status: Implemented — internally verified; user Play Mode pending (2026-10-17). Blocked by: T02.

## Visible accomplishment

The player links the referral clue to the summary, attaches a finding note, presses Not Ready and receives the objective to visit Rosa at Stall 04.

## Work

Implement the smallest reusable finding interaction: tap fixed row, Add finding, select related record, select reason. Store IDs/evidence/conclusion separately from answer keys. Render the finding as a paper note on the folder's findings page. Arthur demonstrates the first selection and asks the player to perform the remaining actions; guide observes successful domain state.

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
