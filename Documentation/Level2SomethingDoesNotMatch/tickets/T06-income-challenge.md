# T06 — Complete the scored Mark the Money challenge

Status: Implemented — internally verified; user Play Mode pending (2026-10-19). Blocked by: T05.

## Visible accomplishment

The player reviews four income entries, checks annotations, learns from corrections and receives a Bronze/Silver/Gold receipt medal plus a physical annotated list to take back.

## Work

Use E1–E4 and the 12 decisions in CONTENT with the toolkit from T05. One active entry; review/revise freely before `Check annotations`. Missing slots prompt completion without scoring. Freeze the first full answer snapshot/accuracy once. Feedback identifies income-rule mistakes and references the guide; it never gives summary totals, F1/F2 conclusions or final readiness.

Require correction to mastery using entry-level hints, without further grade deductions. Annotated list contains corrected per-entry facts but no summed/reconciled case answer. Grade is frozen: Gold12, Silver10–11, Bronze0–9. Render existing medal card with first-check X/12, tier and brief learning message; award only after corrections. Use Level2 reward rule, not Level1's EXP-ratio badge inference.

Quest completion packs papers, closes toolkit and activates office return. Grant 60 minus wrong-count EXP (48–60) and 3 completion hearts once. The quest does not edit the office summary, finding conclusions or stamp. Preserve collected source records and current quest state on workstation exit; T11 adds durable cross-session restore.

## Acceptance

Internal status (source/data checks only; each box also needs the user's Play Mode check):

- [x] Exactly four entries/12 graded answers; objective right/wrong keys and meaningful rule feedback. *(E1–E4 in `review.annotation.entries`, separate `review.annotationKey` with two-step hints per note)*
- [x] All tier boundaries correct; corrections/reopening never upgrade or reroll frozen tier. *(0/9 Bronze, 10/11 Silver, 12 Gold; frozen in `ChallengeState`, survives a JSON round trip)*
- [x] Incomplete check is unscored; free drafts and tutorial do not penalize. *(Incomplete lists what is missing and changes nothing)*
- [x] All mastery corrections required; badge/reward delivered exactly once. *(`IncomeChallenge.TryAward` + ledger `SideQuestScored`)*
- [x] Gold completion leaves office reconciliation and final stamp undone. *(no packet revision, findings or stamp change; phase moves to OfficeReconciliation)*
- [x] Physical completion spread and existing-style medal card are wired. *(Annotated Income List sheet, pack-up into the case, `SideQuestCompleteUI.ShowScreen` reward card)*

## Implementation (2026-10-19)

After the T0 practice, the payslip flips away and Rosa's four records are dealt onto the clipboard with tabs E1–E4 (three progress pips each) and a CHECK button. The player swipes each record's money line, places TYPE and TREATMENT cards and clips one or two record tags (D1–D6). CHECK with anything missing shakes and names it (unscored). The first complete CHECK is frozen: pips flip green/red one by one, the lid scoreboard counts up to X/12 and shows the Bronze/Silver/Gold medal. Right notes lock with a green tick; red notes give a hint when tapped (general first, specific on later checks); re-checking is free and never changes the badge. All twelve right: confetti, the Annotated Income List, everything packs into the case and the lid shuts, then the award (EXP 60 minus first-check wrong, floor 48; 3 hearts; accuracy medal) and the existing reward card. The review moves to OfficeReconciliation and `IncomeQuestCompleted` fires. See PROGRESS for files and checks.

## Verification/handoff

Test complete submissions at 0/9/10/11/12 correct, incorrect evidence bundles, free draft revisions, frozen grade, quest award duplication and no case-answer leakage in player copy. Compile and reward/progression tests. User later checks three tier runs, satisfying annotation/reward and return route. Next T07.


### Consistency follow-up (2026-10-19)

Implemented — internally verified; user Play Mode pending. See SPEC and PROGRESS, “Level 1 consistency follow-up”, for the current six-page folder, shared clock/splash, receipt papers, annotated HUD collection and direct-Editor checkpoint policy. Earlier implementation descriptions above remain historical.
