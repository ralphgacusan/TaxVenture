# T06 — Make the classification board draft, check and correction based

Status: Planned. Blocked by: [T04 — Level 1 field resume](T04-level1-field-resume.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and before/after flow

Turn the Level 1 board into an independent learning check that permits correction without giving away answers on placement. Before this ticket, the board reveals wrong drops and can penalize them immediately. After it, the player drafts four profile classifications, fills all four areas so the board checks itself, receives correction feedback, and sees the frozen tier only after all entries are corrected.

A drag or tap only changes a draft slot. It never scores, changes trust, or reveals correctness. With fewer than four drafts nothing is checked; the board only says how many remain and reveals no answer. The first complete Check computes the correct count once and freezes the grade basis. Settle quest XP once from this frozen snapshot using the current sanitized `questEXP` and `expPerWrong` configuration (current defaults: 50 EXP and 5 EXP per incorrect profile, so the result is 30–50). Store the settled amount with the first-check result; later corrections must never recalculate or deduct XP again. It then shows clear per-profile feedback and the correct classification with a short explanation for missed entries. The player must correct every miss before the tier is awarded or the quest can complete; corrections change the live draft and completion status but never change the frozen count. The awarded tier is Gold for 4/4, Silver for 3/4, and Bronze for 0–2/4 on that first check. Compulsory guide demonstrations and drafts before the first full check are unscored. The quest becomes complete only after all four entries are corrected and the required guide/board handoff finishes.

## Ownership and mechanics

The inspected seams are Assets/Scripts/SideQuest1/CorkboardSideQuestManager.cs for board attempts, TaxpayerProfileDatabase / TaxpayerProfileData.cs for profile content, TownContentLoader.cs and Resources/Levels/Level_01 for authored presentation, SideQuestCompleteUI for result presentation, and T04's four-ID checkpoint. The implementation agent must trace exact board target and drag/tap ownership before editing. The full-check action is a discrete command owned by the board/gameplay domain. Presenters subscribe to its result and never calculate grade from Update or animation completion.

Store four profile IDs, chosen classifications, first-check result, frozen correct count/tier, and correction completion in the v5 Level 1 story checkpoint. Never copy answer keys into player data; derive correctness from the approved profile database. Grade four unique profiles in story or isolated mastery-replay/practice state. Only story mode writes campaign checkpoints and trust/reward claims. A failed story save hides success and permits retry; restore redraws the draft/result without replaying award, trust, or correction events.

Keep four legible 3D cards/slots; the full board checks itself (no separate target, user decision 2026-10-24). Before checking, give neutral paper feedback only; no answer-key color, wrong marker, dialogue, or sound. After a committed check, pair correction marks with text/icons and let Quax explain the rule. Award the frozen tier and play its result cue only after corrections. Reuse card-place/return motion and SFX; any new cue belongs only to check/correction. Follow WORLD_AND_FEEDBACK.md.

## Automatic check at four (user decision 2026-10-24)

This replaces the 2026-10-23 Check Board target: after playing it, the user asked for no button. The board checks itself as soon as all four areas hold a card, whether the drafts are right or wrong.

- Trigger: the fourth card is pinned (all four areas filled). The check runs after a short beat (about 0.45 s) so the pin lands first; it waits while any card is still moving, and taking a card off before the beat ends cancels it. A drag release or tap never checks by itself.
- One non-revealing heads-up before the first check: when the third card lands, the guide says once per visit that the next card starts the check and that the first check counts.
- Re-checks for corrections run the same way, but only when the board differs from the last check (picking a card up and putting it back in the same place does not re-check). Corrections never change the frozen count, tier or XP.
- Everything after the trigger is unchanged: the first complete check freezes count, tier and XP; marks, clues (checks 1-2) or answers (check 3 on) follow; right cards stay pinned; the quest completes when all four match.
- While the board is open the profile cards live in its tray: the HUD profiles icon shows 0 and cannot be opened or dragged out; it holds the profiles again when the board closes.

## Non-goals

Do not change taxpayer facts, add an intentional misconduct choice, punish mistakes with trust loss, award coins/XP per placement, unlock the case from a draft, add the bag, or use a first-drop/attempt-count grade. Do not change Level 2's 12-decision check.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Incomplete Check reveals no correctness; placement and compulsory demo never score or mutate trust.
- [ ] Story, mastery-replay, and practice checks grade the same rule in isolated run state; only story mode changes the live checkpoint or campaign claims.
- [ ] First complete check freezes the raw correct count and settles XP once from the configured questEXP minus expPerWrong for each incorrect profile; later corrections never recalculate or deduct it. After all corrections, the awarded tier is Gold 4/4, Silver 3/4, Bronze 0–2/4, and corrections cannot upgrade it.
- [ ] Corrections complete the learning gate only when all four entries match; repeats and save retries are idempotent.
- [ ] Save/reload restores drafts, frozen tier, and correction state without replaying results.
- [ ] Content/readability checks find no pre-check answer leak in labels, colors, sounds, or guide text.
- [ ] The board checks only when all four areas hold a card (after the last pin lands; a card removed first cancels it), commits once per board state, and re-checks only a changed board.
- [ ] Before the first check, one non-revealing guide heads-up at three cards; while the board is open the HUD profiles icon is empty and cannot be opened or dragged.

USER manual acceptance — only the user performs these checks:

- [ ] Draft wrong entries and confirm the board gives no correctness hint before Check Board.
- [ ] Check a complete board with 0, 2, 3, and 4 correct entries across separate runs, correct each board, then confirm the awarded tiers are Bronze, Bronze, Silver, Gold.
- [ ] Confirm no tier is awarded before all corrections, and the first-check tier stays frozen while the case unlocks only after all four are correct.
- [ ] Save and Continue before and after the first check; verify the same draft and result return.
- [ ] Confirm the board checks itself right after the fourth card lands (not before), that moving a card off and back in the same place does not re-check, and that the HUD profiles icon is empty and locked while the board is open.

## Handoff

Record the full-check rules and any existing-board feedback that was removed in ../PROGRESS.md. T07 combines this frozen tier with the corrected quest and approved case; T06 remains independent of T05's case-clock work.
