# T16 — Connect the Level 1 story from first approach to wrap-up

Status: Planned. Blocked by: T08, T09, T11, T13, T14, T15, T23.

Implementation (2026-10-26): implemented, internally verified; user manual pending. See [PROGRESS](../PROGRESS.md) "T16". T15 was reverted by user decision (2026-10-25), so its part of this blocker list no longer applies.

## Goal

Make the entire corrected Level 1 route one resumable campaign chapter.

Chapter 1's ending is staged by the [T08 Client Copy handover](T08-public-trust-and-follow-through.md): the Auditor's read-only copy, the walk to Marie in the conference room and the drag onto her. T16 does not restage it. T16 owns final chapter completion (replacing the temporary bridge in CaseResultRules.CompleteFollowThrough, KNOWN_ISSUES CF-41) and the wrap-up after the handover's finale and case-result card. [T23](T23-marie-consultation-visits.md)'s consultation visits are part of the route it composes.

## Guide (user decision 2026-10-25)

The guide is **Arthur**, Senior Tax Consultant, everywhere in Level 1. [T15](T15-quax-identity-and-demonstrations.md) (Mayor Quax, a practice board and demonstrations) was reverted: there is no Quax, no practice board, no `demo` guide field and no sample demonstration. Do not reintroduce them.

## Visible accomplishment

Before: first-story requirements are not unified. After New Game, Elijah meets Arthur in person, meets four taxpayers and collects their profiles, drafts the board and gets it checked, corrects each miss, approves Marie's corrected case, follows through with her Client Copy, and sees the Chapter 1 wrap-up before returning to Career. Keepsake, voucher, shop visit and mastery score are optional; the saved result returns to Career.

## Work

Join existing scenes and quest/case events into the approved route. New Game and Continue preserve the current opening-skip policy and return a resumed player to a safe location with their saved task. Arthur talks through the guide box during the opening lessons; his in-person meeting is the "guide-appears" step after the profiles: it requires proximity and a deliberate talk interaction (a camera fly-by or a tutorial-page advance does not count), and it is saved (`guideIntroDone`). Each of the four existing taxpayer conversations makes its actual profile collectible once. The profiles remain physical cards; one tap/drag places each in a draft slot. Before the board's first complete check, no visual, dialogue or sound may identify a correct or wrong placement. The first full check freezes grading; the player must correct all four placements, with neutral guidance and no change to the frozen tier, before the required quest is complete.

Board check (user decision 2026-10-24, KNOWN_ISSUES CF-38): the board checks itself 0.45 s after the fourth card is pinned. The earlier CHECK BOARD stamp/lever and the corrected-board stamp were removed by that decision; do not add a lever.

Marie is not approved merely by reaching the desk, opening the folder, hearing tutorial dialogue or handing in an incorrect form. Existing validation must accept corrected work. Marie's required follow-through happens next. Only the actual follow-through followed by the wrap-up completes the chapter; a result-card click or a shop visit cannot substitute. The wrap-up returns the player to Career and never launches Level 2 during Delivery A.

Shop (user decision 2026-10-25, see [T12](T12-physical-shop-and-bookmarks.md)): Arthur introduces the Supply Counter at the start of Level 1 (after the move and look lessons, before the townsfolk) and the first counter visit unlocks the shop. There is no second introduction before wrap-up, and approval does not unlock the shop. Visiting or buying is always optional.

### Source starting points

- **Existing:** `Assets/Scripts/SideQuest1/CorkboardSideQuestManager.cs`, `CorkboardSlot.cs`, `ProfileDragCard.cs`, `TaxpayerNpcInteractable.cs`, `TaxpayerProfileWorldPickup.cs`, `Level1FieldCheckpointer.cs`, `ClientCopyHandover`, `CaseResultRules`, `PlayerProgressManager`, `LevelCompleteUI`, and the `MenuEntry`/career-board flows.
- **Implemented 2026-10-26:** `CaseResultRules.WrapUpPending / CompleteWrapUp / BuildChapterSummary`, `PlayerProgressManager.CommitLevelOneWrapUp / LevelOneSummary`, `Assets/Scripts/UI/HUD/ChapterWrapUpView.cs`; `CompleteFollowThrough` no longer completes the chapter.

## Persistence, rewards and presentation

Save conversation/profile IDs, four draft placements, first complete check and frozen tier, correction completion, approved case state, follow-through, once-only claims, current quest position and wrap-up before changing scene. Restore derives visuals from this data without replaying dialogue, trust, pickup or reward events. The classification activity grants exactly 60 coins once and +2 trust after corrected completion. The 100-coin corrected-case claim commits with actual case approval before its result card is shown; tapping or closing the card never awards it. Marie follow-through adds +4 trust once. Thus the authored L1 outcome is 160 coins and 46 trust from the 40 start. Do not award new trust for each profile talk or any negative for ordinary errors/deadlines. Score tier is 4/4 gold, 3/4 silver, 0–2/4 bronze after corrections and cannot gate story completion. Failed persistence keeps prior state and does not display a success wrap-up.

Wrap-up: after the handover the result card reads "Case Closed · Next: wrap up Chapter 1." Its CONTINUE commits the wrap-up in one write (the wrap-up activity, Chapter 1's completion, the end of the pending end-flow marker and of the Level 1 checkpoint); only after that write succeeds does the wrap-up sheet appear. The sheet is a cream summary page clipped onto the office's manila case folder, with large "CHAPTER 1 COMPLETE" text, coins and Public Trust explained separately (read from the committed ledger), the first board check as an optional mastery note, and one RETURN TO CAREER button. It plays one paper sound when its clip snaps on, through the effects setting; no repeating cue from frame updates. Closing the app on the card resumes the finale/card (pending marker); closing it on the sheet finds the chapter already complete.

## Internal checks — AI source/build/regressions only

- [ ] End-to-end state tests reject early wrap-up (no pending end flow, no follow-through), keep the chapter incomplete after the follow-through until the wrap-up, and cover a failed wrap-up write (state unchanged, retry); earlier tickets cover early check, false case approval and incomplete corrections.
- [ ] Exact 60/100 coin and +2/+4 trust claims are once-only (160 / 46); the wrap-up changes neither; mastery replay, tutorial display, purchase and ordinary errors do not mutate campaign state (earlier ticket checks).
- [ ] Source checks: the result card commits the wrap-up before its sheet shows, the sheet only presents (no grant, completion or save) and never launches Level 2; compile and all regression suites pass.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] Play a clean run: meet Arthur in person after the profiles, collect the four profiles, submit an initially imperfect draft, correct every miss, and verify no pre-check answer reveal or early completion.
- [ ] Complete Marie's case incorrectly once, correct and resubmit, follow through, and verify the chapter completes only at the wrap-up (career shows Chapter 1 COMPLETED only after RETURN TO CAREER's write; the card before it reads Case Closed).
- [ ] Close the app on the case-closed card and relaunch: PLAY resumes the card; CONTINUE shows the wrap-up once.
- [ ] Inspect the wrap-up sheet (folder slide, sheet drop, clip snap, one paper sound, coins/trust columns, mastery line), resume behavior, optional shop path, effects volume and target-device touch feel. No manual scene construction is expected.

## Handoff

Next ticket: [T17](T17-delivery-a-verification.md) verifies Delivery A and prepares the user handoff.

## Non-goals and references

No new tax rules, new campaign save slots, purchase gate, score/trust gate, wrong-drop penalty, Level 2 launch, Quax, practice board or demonstrations. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
