# Level 3 — The Numbers Tell a Story

## Current continuation rule — 30 October 2026

The user explicitly requires every collected Bag/hotbar item to stay across Levels 1 → 2 → 3, whether useful in the next chapter or not. This supersedes older retirement instructions in this pack/proposal/tickets. Keep ownership, quantities, slots, contents, pins, capacities and readable prior records; scope limits current-case use, never ownership. Consumed supplies stay consumed, explicit put-back remains deliberate, and confirmed New Game is still a new campaign. Replay/Practice/Editor remain isolated from live story inventory. Already deleted historical data is not fabricated from answer keys.

Level 3 now adopts the shared Bag/hotbar with its own checkpoint branch. Prior IDs open in the actual card reader; retained acquired-paper/player-annotation text uses read-only saved-paper pages, without CaseManager rebinding or value transfer. No global historical filing/archive gameplay is introduced.


Status,30 October2026: **T01 implemented, internally verified; user manual pending** (content preparation only). **T02 implemented, internally verified; user manual pending**: isolated Office_Level3 entry at reception, Level_03 mapping, two-case chapter snapshot in the signed save, calculator-first opening task, legacy completion guard. **T03 implemented, internally verified; user manual pending**: the physical desk calculator (Synty body, readable keys, display, printed tape) and the protected budget practice (80, then 72), unscored. **T04 implemented, internally verified; user manual pending**: Bea at her plaza worktable, her nine readable original papers handed over (saved) into the Case Folder, and repeatable source questions. **T05 implemented, internally verified; user manual pending**: Bea's physical worksheet on her table (seven source-linked strips, four sections, two claim notes, Undo), every edit saved, nothing graded. **T06 implemented, internally verified; user manual pending**: the shared calculator at Bea's table adds each money section's strips onto a clipped subtotal tape (stale when its strips change), a deliberate CHECK freezes the first 12-decision grade once (Gold 12, Silver 10-11, Bronze 0-9; 60 XP minus wrong, floor 48), marks and free hints guide corrections, and COLLECT WORKSHEET saves the collected worksheet with 60 coins, +2 Public Trust and its mastery award in one write. **T07 implemented, internally verified; user manual pending**: at the office desk Bea's computation sheet replaces the practice card; the player picks the worksheet totals and the annual-table band explicitly and attaches traceable tapes (a single calculation or a chain) for taxable income 600,000 and tax due 62,500, with source-based NEEDS RECALCULATION tags; credits and balance wait for T08. **T08 implemented, internally verified; user manual pending**: after her worksheet is collected Bea walks to the office conference room; at the credit step she hands over her withholding certificate (B-WHT, the new Type 8 paper) and asks whether withheld tax is another expense (a wrong answer opens the book's Credit Versus Expense help, nothing is penalised); at the desk the player links the certificate's tax withheld to Tax credits and attaches Remaining payable 62,500 − 12,500 = 50,000, leaving the computation complete but unreviewed. T09–T18 remain Planned. Career Chapter 3, the shop and availability are unchanged (coming soon). [Authority review](TAX_AUTHORITY.md) and [fixture/schema contract](FIXTURE_CONTRACT.md) are the T01 handoff; graded content approval remains pending.

This is the implementation ticket pack for the [revised Level 3 proposal](../Level3DesignProposal.md). It follows the existing Level 2/Campaign Foundation ticket conventions. There are **18 local tickets**, executed one at a time; numbering is local to this pack. The required side quest remains **Worth the Tax?**, as explicitly specified by the proposal. The attachment's default preference for optional side quests does not override that authored requirement. The keepsake, shopping, and replay remain optional.

## Player experience

Continue through Career from completed Chapter 2. Enter the familiar office with actual possessions and Rosa history. Learn the physical calculator using a protected, unrelated budget exercise. Meet Bea at the plaza table; build her supported worksheet through the required side quest. Compute taxable income, graduated tax, credit, and remaining balance. Stamp, obtain review, explain and deliver her copy. Receive the company appointment, resolve the unsupported personal-trip deduction, apply corporate rules, review and deliver. Record the limited Assistance Desk finding, receive the mayor's message, acknowledge wrap-up, and return to Career or the optional supply counter. Level 4 remains unavailable until its own release.

Two sequential cases, one badge, one shared calculator, two complete client deliveries. Coins: 60 +100 +100 = 260. Proposed maximum mastery XP: 260. Service trust: +2 +4 +4 = +10. Corrected work and delivery determine chapter completion; score, trust target, wallet, and purchases do not gate it.

## Read order

1. [AGENTS](AGENTS.md): authority, user-only Unity/Git work, shared-system safety.
2. [REUSE_AND_ARCHITECTURE](REUSE_AND_ARCHITECTURE.md): inspected baseline and concrete extension seams.
3. [SPEC](SPEC.md): phases, dependencies, identities, saved-state and release contracts.
4. [CONTENT](CONTENT.md): canonical records, answer fixtures, dialogue, content-authority gates.
5. [SCENE_REUSE](SCENE_REUSE.md), then [WORLD_AND_FEEDBACK](WORLD_AND_FEEDBACK.md).
6. [REWARDS_AND_VALIDATION](REWARDS_AND_VALIDATION.md), [VERIFICATION](VERIFICATION.md).
7. [TICKETS](TICKETS.md), [PROGRESS](PROGRESS.md), [KNOWN_ISSUES](KNOWN_ISSUES.md), then the selected ticket.

## Implementation order

Follow **T01 → T02 → T03 → T04 → T05 → T06 → T07 → T08 → T09 → T10 → T11 → T12 → T13 → T14 → T15 → T16 → T17 → T18** as the recommended serial route. The index lists the smaller true prerequisite sets; they do not authorize concurrent edits. T01 is a bounded content-authority/fixture preparation exception. All other feature tickets must connect their real entry point, physical presentation, state, save behavior, and targeted verification; no assets-only completion.

T18 owns normal Career availability after the integrated internal gate. Manual Unity/device acceptance stays separately pending until the user performs it. The optional computer must remain a pending teaser if its complete purchase/function slice has not shipped. Its optional status must never block corrected story completion; the full proposed feature pack includes T15.

## Boundaries

Reuse the existing first-person exploration/cameras, input, FSM, dialogue, guide, NPC life, papers, Bag, rewards, save owner, shop, clocks, and audio. This pack adds the new worksheet/tape/calculation rules and bounded two-case continuation. It does not build filing, Level 4/5 scenes, final evidence-board/endings, a comparison stand, a playable drawer, or a historical folder archive.

Validate this pack only: `python Documentation/Level3TheNumbersTellAStory/verify_pack.py`. This checks documentation, links, verified source references, ticket dependencies and numeric contracts. It does not verify Unity behavior.
