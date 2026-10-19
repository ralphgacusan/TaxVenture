# T07 — Reconcile the case independently at the office

Status: Implemented — internally verified; user Play Mode pending (2026-10-19). Blocked by: T06.

## Visible accomplishment

Back at the desk, the player brings evidence into Rosa's folder, adds the missing payment for a separate blanket-washing job and explains why laundry bank deposits differ from the full laundry collection sheet.

## Work

Append annotated list/supporting records/findings through current folder/paper systems. Keep original S1 as provenance and show a distinct player-reviewed schedule; never overwrite original evidence. Use existing value-transfer behavior to select supported entries into the schedule. Sum the player's chosen values automatically; no hidden-answer-key autofill or arithmetic minigame. Reusing D6 cannot silently add an independent service-income row.

Add F2 as a discoverable comparison prompt, not a pre-resolved finding. Player compares D1/D2/D6 and selects evidence/reason/conclusion. F1 needs D3 and actual inclusion in the reviewed schedule. Use generic evidence/finding contracts; do not hardcode Rosa answers into shared UI. A complete wrong conclusion is allowed to stand until review.

Implement pure review evaluation for omission, treatment and explained difference, with root cause IDs. Keep verdict support optional per case: Level1 follows its existing evaluator. Add meaningful revision/fingerprint tracking for graded work; paper navigation is cosmetic. Book supplies general principles without exact case answers.

## Acceptance

Internal status (source/data checks only; each box also needs the user's Play Mode check):

- [x] Original summary remains readable; corrected schedule uses player-selected entries. *(S1 page untouched; Reviewed Schedule page with 4 line boxes)*
- [x] Supported totals are May20,000/annual140,000; loan/D6 double counting detected. *(the same payment is refused structurally; a loan is allowed in and named by the evaluator)*
- [x] F1 corrected only with real schedule/evidence action; F2 explained only by supported reason/links.
- [x] Wrong but complete reviews are possible; no correctness sparkle or automatic findings.
- [x] Gold quest alone cannot finalize the case; optional review rules preserve Level1 behavior.

## Implementation (2026-10-19)

After Mark the Money!, Rosa's folder gains four pages after Findings: **Annotated Income List** (read-only notes from the market), **Reviewed Schedule** (4 "May income line" boxes that take a money line from one of Rosa's records by tap or drag, plus a "Reviewed totals" row summed from the player's own lines: May, and the year with the supplied 120,000 prior months), **Finding F1 · Unlisted payment** (the initial entry and clue read-only, then Original record + Conclusion) and **Finding F2 · Do two records disagree?** (Entry to check, Compared with, Clue, Reason, Conclusion). Reasons and conclusions come from the book's Review Notes (new generic reason "Paid another way" and conclusions "Corrected: included", "Explained: no change", "Still unresolved"). Tapping a filled box with nothing picked up empties it. A line whose money is already counted (e.g. D6 cash inside the D1 total) is refused and the box says which line holds it. When the schedule has a line and every finding box is filled, `OfficeReviewCompleted` fires once ("recorded", never "correct"). `OfficeReview.Evaluate` gives May/annual totals and root causes (omission, missing income, not-income counted, double counted, F1 unsupported, F2 unexplained, F2 false flag) for the T09 Auditor. Every real edit is one packet revision; reading pages is not. Next: T08.

## Verification/handoff

Test correct/omitted/loan-added/double-counted/false-flag packets through evaluator; confirm immutable originals, idempotent transfer and meaningful revision changes. Compile and affected progression/guide suites. User later checks comparison layout, reading burden and summary clarity. Next T08.


### Consistency follow-up (2026-10-19)

Implemented — internally verified; user Play Mode pending. See SPEC and PROGRESS, “Level 1 consistency follow-up”, for the current six-page folder, shared clock/splash, receipt papers, annotated HUD collection and direct-Editor checkpoint policy. Earlier implementation descriptions above remain historical.
