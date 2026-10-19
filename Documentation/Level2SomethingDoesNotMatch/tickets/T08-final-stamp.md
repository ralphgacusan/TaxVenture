# T08 — Apply a satisfying reason-backed final stamp

Status: Implemented — internally verified; user Play Mode pending (2026-10-19). Blocked by: T07.

## Visible accomplishment

The player selects a supporting reason, lifts a familiar physical stamp, presses it into the assessment slip and sees a lasting imprint bound to the reviewed packet.

## Work

Extend existing Stamp3DDrag/StampUI/StampTransitionController through scoped optional behavior. Reuse models, select/drop/tap fallback and existing audio owner. Implement align, press, dry impact, rebound, return and page-bound ink per WORLD_AND_FEEDBACK. Do not build a new stamp selector app. Label final assessment as a consultant recommendation subject to Auditor approval.

Both final stamps remain selectable for complete reviewed packets. Only structural prerequisites are checked before committing; no hidden tax-correctness lock. Require a reason. Commit once at impact to current revision; stamp stores verdict/reason/evidence context/revision. Input repeated during animation is ignored. Invalid target cancels cleanly without penalty or ink. Preliminary stamp stays distinct and nonterminal.

Changes to graded work invalidate current final stamp and require reassessment; show compact history rather than unreadable ink stacking. Cosmetic page turns do not invalidate. Storing/handing in requires a current stamp, not merely an old assessmentStamped boolean. Audio/motion do not certify answer correctness.

## Acceptance

Internal status (source/data checks only; each box also needs the user's Play Mode check):

- [x] Physical press, rebound, sound and persistent paper ink are wired for both choices. *(the polished desk stamps; ink on the Final Assessment page via the page decorator)*
- [x] Reason is mandatory; unsupported but complete decision remains submittable.
- [x] Commit happens exactly once and refers to the correct packet revision.
- [x] Edits invalidate final approval; page turns do not; restamp earns nothing. *(stale ink fades to 30% with "Review changed: stamp again"; history keeps the last 3)*
- [x] Interrupted animation/invalid drop/repeated taps cannot corrupt case state. *(decision at impact only; same verdict on a current stamp is ignored)*

## Implementation (2026-10-19)

After the office review is structurally complete the review moves to FinalAssessment and the folder gains a **Final Assessment** page: "Your recommendation (the Auditor decides)", a **Reason** box (assessment reasons from Reading Records in the book: Income supported, Payment not counted, Non-income counted, Difference unexplained, Taxpayer facts wrong), "Reviewed work: Revision N" and "Earlier stamps". Tapping the stamp set turns to that page; the READY / NOT READY stamp commits once at impact through `FinalAssessment.TryStamp` (structural prerequisites only, reason required, current packet revision). Any graded change (schedule, findings, reason) makes the stamp stale; the hand-in then asks for a new stamp. The initial stamp record is untouched. Next: T09.

## Verification/handoff

Test atomic commit/revision guard, structural prerequisites, stale stamps and duplicate gestures through domain seam. Compile and input/progression tests; validate ink parent bindings. User later checks tactile feel, readable imprint and pause/exit interactions. Next T09.
