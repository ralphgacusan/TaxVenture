# T09 — Review decisions and recover without restarting

Status: Planned. Blocked by: T08.

## Visible accomplishment

The Auditor accepts a supported packet, returns a premature approval, challenges an unnecessary hold, or acknowledges a justified hold. Rosa's short response makes the consequence concrete.

## Work

Reuse folder hand-in, audit results, existing case manager/state progression and retry reset. Integrate evidence-aware readiness with inherited required-field checking. Fill real/missed/false finding results rather than treating SubmissionResult.FalseIssues as already implemented. Separate `Supported interim hold` from failed audit and approved completed case.

Supported hold: acknowledge valid reason, retain all work, return objective to unresolved office task, no penalty/reward/index/finale. False hold or premature approval: reviewer points to the relevant record/rule, not automatic correction. Wrong Ready reason can return independently on an otherwise correct packet. Deduplicate cascading mistakes using root cause IDs. Block unchanged resubmissions, including unsupported hold spam; meaningful changed review can resubmit.

Wire reward caps from REWARDS_AND_VALIDATION instead of stacking default Level1 charges. Apply penalties at final reward with transparent pending-loss feedback. No penalties for free editing, tutorial or initial guided hold. Client dialogue reflects delay/rework without invented fines. Accepted review goes to a provisional completion staging hook for T11; no duplicate case reward or unfinished finale.

## Acceptance

- [ ] All four verdict outcomes work, with reason/evidence correctness evaluated.
- [ ] Supported hold never finishes the level or charges a failed-attempt penalty.
- [ ] False findings and missed correction produce targeted, different feedback.
- [ ] Documents/quest/mastery retained; revised packet requires current stamp.
- [ ] Root causes deduplicate; combined case deduction <=15EXP/1heart.
- [ ] Only accepted final packet can award case completion once.

## Verification/handoff

Test outcome matrix, wrong reason, inherited-field error, unchanged packet block, reward idempotence/caps and Level1 fallback. Compile; progression/dialogue/reward regressions. User later checks clarity, Rosa's reactions and full correction loop. Next T10.
