# T16 — Connect the Level 1 story from first approach to wrap-up

Status: Planned. Blocked by: T08, T09, T11, T13, T14, T15.

## Goal

Make the entire corrected Level 1 route one resumable campaign chapter.

## Visible accomplishment

Before: first-story requirements are not unified. After New Game, Elijah approaches Quax, meets four taxpayers and collects their profiles, drafts and checks the physical board, corrects each miss, approves Marie's corrected case, and follows through before Chapter 1 ends. Keepsake, voucher, shop visit and mastery score are optional; the saved result returns to Career.

## Work

Join existing scenes and quest/case events into the approved route. New Game and Continue must preserve the current opening-skip policy and return a resumed player to a safe location with their saved task. Quax's first meeting requires proximity and a deliberate talk interaction; a camera fly-by or tutorial-page advance does not count. Each of the four existing taxpayer conversations makes its actual profile collectible once. The profiles remain physical cards; one tap/drag places each in a draft slot. Before the first complete Check Board action, no visual, dialogue or sound may identify a correct or wrong placement. The Check Board itself is a reachable physical board control with an explicit CHECK label. The first full check freezes grading; the player must correct all four placements, with neutral guidance and no change to the frozen tier, before the required quest is complete.

Marie is not approved merely by reaching the desk, opening the folder, hearing tutorial dialogue or handing in an incorrect form. Existing validation must accept corrected work; approval unlocks the shop. Marie's required follow-through happens next. Afterward Quax introduces the counter; a one-tap acknowledgment may be required, but visiting or buying is optional. Skipping the counter auto-continues to wrap-up. Only the actual follow-through completes the chapter requirement; a result-card click or shop visit cannot substitute. The physical receipt returns the player to Career and never launches Level 2 during Delivery A.

### Source starting points

- **Existing:** `Assets/Scripts/SideQuest1/CorkboardSideQuestManager.cs`, `CorkboardSlot.cs`, `ProfileDragCard.cs`, `TaxpayerNpcInteractable.cs`, `TaxpayerProfileWorldPickup.cs`, and existing office/case validation, Level 1 objective and `MenuEntry`/career-board flows.
- **Proposed:** one campaign activity-completion coordinator that listens to committed quest/case/follow-through results and calls the shared campaign commands. Name and API are for implementation to select; tutorial/UI presenters must not grant progress.

## Persistence, rewards and presentation

Save conversation/profile IDs, four draft placements, first complete check and frozen tier, correction completion, approved case state, follow-through, once-only claims, current quest position and wrap-up before changing scene. Restore derives visuals from this data without replaying dialogue, trust, pickup or reward events. The classification activity grants exactly 60 coins once and +2 trust after corrected completion. The 100-coin corrected-case claim commits with actual case approval before its result card is shown; clicking or closing the card never awards it. Marie follow-through adds +4 trust once. Thus the authored L1 outcome is 160 coins and 46 trust from the 40 start. Do not award new trust for each profile talk or any negative for ordinary errors/deadlines. Score tier is 4/4 gold, 3/4 silver, 0–2/4 bronze after corrections and cannot gate story completion. Failed persistence keeps prior state and does not display a success wrap-up.

Build the check control as a waist-height brass lever with a printed “CHECK BOARD” plate; pressing it pulls down an opaque result flap only after the complete first check commits. Corrections use the same cards returning with a gentle spring motion; a corrected board gets one neutral completion stamp. Wrap-up is a cream summary sheet clipped onto the existing physical office folder, with large “CHAPTER 1 COMPLETE” text and coin/trust change explained separately. Use known card/paper one-shots once after commit through the effects setting; no repeating click or cue from frame updates.

## Internal checks — AI source/build/regressions only

- [ ] End-to-end state tests reject early check, false case approval, incomplete corrections and early wrap-up; cover interruptions before/after every durable milestone and failure retry.
- [ ] Exact 60/100 coin and +2/+4 trust claims are once-only; mastery replay, tutorial display, purchase and ordinary errors do not mutate campaign state.
- [ ] Static scene/content inspection confirms reachable physical board lever, profile sources, safe restore anchors and approved case validation wiring; compile and affected progression/guide/save regressions pass.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] Play a clean run: approach Quax, collect the four profiles, submit an initially imperfect draft, correct every miss, and verify no pre-check answer reveal or early completion.
- [ ] Complete Marie's case incorrectly once, correct and resubmit, follow through, and verify the chapter completes only at the true accepted result.
- [ ] Inspect world object readability, physical controls, resume behavior, Quax demonstration, wrap-up paper, optional shop path, effects volume and target-device touch feel. No manual scene construction is expected.

## Handoff

Next ticket: [T17](T17-delivery-a-verification.md) verifies Delivery A and prepares the user handoff.

## Non-goals and references

No new tax rules, new campaign save slots, purchase gate, score/trust gate, wrong-drop penalty, or Level 2 launch. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
