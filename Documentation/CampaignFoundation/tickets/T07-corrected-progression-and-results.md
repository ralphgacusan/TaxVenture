# T07 — Unlock story through corrected work and separate mastery results

Status: Planned. Blocked by: [T05 — case and clock resume](T05-level1-case-and-clock-resume.md), [T06 — classification first check](T06-classification-first-check.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and before/after flow

A player who completes the classification corrections and Marie's case receives a truthful case result and continues to Marie's required follow-through regardless of score tier. Before this ticket, career availability follows the legacy passed-score list, and Level 1 has no campaign record that requires corrected work. After it, the case result is based on required learning and case milestones: all classification corrections are complete and the main case is actually approved after correction. This ticket does not mark the whole chapter complete or skip Marie's follow-through, Quax's later counter introduction, or T16's final wrap-up. A low first grade is descriptive mastery feedback, never a disguised story gate.

## Ownership and mechanics

The inspected seams are LevelScoring.cs and CareerProgressService.cs for existing accuracy scoring and gating, PlayerProgressData.RecordLevelResult and PlayerProgressManager.ApplyLevelResult for saved results, the Level 1 CaseData / CaseDefinition path for approval, and Level 1 objective, side-quest and finale paths for required milestones. Replace the new campaign's active-story unlock decision while keeping accuracy scoring separate from corrected-work completion. Implement a small pure campaign rule/command and an adapter at the gameplay milestone owners; exact new filenames are proposed implementation details.

Record the first Level 1 case/service result once with the frozen T06 tier, corrected completion fact, approved-case ID/revision, and mastery result. Preserve the existing sanitized case-XP calculation, audit deductions, lateness rules, and caps; clamp XP at zero as the existing rule does. The badge continues to depend on frozen accuracy independently of XP total and on-time restoration. Legacy audit/score deductions never become public-trust loss, and no error or ordinary lateness triggers automatic trust loss. T16 finalizes first chapter history only after follow-through, Quax's counter introduction, and the optional visit-or-skip decision. A replay may improve eligible best XP/badge data only; it must not rewrite the chapter's first tier, choices, trust, coin claims, acquisitions, or completed time. Continue advances to Marie's follow-through state, not the Level 1 intro. Until Delivery B integrates Level 2, present it as pending and keep Levels 3–5 coming soon.

Commit the case result and next follow-through checkpoint together through the existing signed atomic save. Use a stable scoped action ID so an interrupted result screen, repeated input, or relaunch cannot apply the result twice. Build a candidate, persist, then publish the snapshot and result presentation; if save fails, leave the checkpoint and old campaign state intact, hold on a retryable case-result view, and emit no success fanfare or unlock. Restore after a successful commit must not replay award events. Apply the same explicit story-mode check used by T02/T03 so Practice, mastery replay, and direct Editor attempts cannot mutate story history.

Show a concise physical/paper-style case-result card with corrected outcome, frozen Bronze/Silver/Gold tier, XP/badge mastery, and the next step: Marie's follow-through. Do not label this intermediate result “Chapter Complete.” Trust uses its own hearts only after T08; do not display the legacy score hearts as social trust or add another mastery-heart bar. Reuse current result-card movement/select audio; no new SFX is required. T08 owns the follow-through trust transaction and T16 owns final chapter completion.

## Non-goals

Do not add public-trust rules, coin rewards, the wallet/shop, new endings, new tax rules, new levels, or Level 2's ethical choice. Do not let score, XP, badge, optional pickup, or purchase determine whether the corrected story can continue.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Case result requires corrected quest and approved corrected case; sanitized case XP retains its audit/late rules and caps, while the badge remains accuracy-based and independent of XP/on-time status. Errors and ordinary lateness cause no automatic trust loss. No tier can skip Marie's follow-through or final wrap-up.
- [ ] Bronze, Silver, and Gold case results all advance to the same follow-through task.
- [ ] Repeated result input, reload, or replay cannot duplicate first history or rewrite it; best mastery improvements remain eligible.
- [ ] Result and checkpoint clear commit atomically; injected write failure retains prior state and produces no success notification.
- [ ] Practice, mastery replay, and temporary direct-Editor runs cannot mutate story result or unlocks.
- [ ] Existing accuracy hearts are never read as public trust or new-story gates.

USER manual acceptance — only the user performs these checks:

- [ ] Finish the required corrections and approved case at different frozen tiers; confirm each continues to Marie's follow-through without early chapter completion.
- [ ] Leave/relaunch while the result card is pending, then Continue; confirm there is no duplicate result or lost completion.
- [ ] Replay Level 1 and confirm first story history stays fixed while eligible best mastery may improve.

## Handoff

Record the case-result predicates and atomic next-task update in ../PROGRESS.md. T08 adds the authored public-trust and client follow-through effects; T09 adds once-only activity coins; T16 composes them into final chapter completion.
