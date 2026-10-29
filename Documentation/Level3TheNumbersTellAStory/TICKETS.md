# Ordered Level 3 implementation tickets

Status: **All 18 Planned — documentation only**. Read [README](README.md), [AGENTS](AGENTS.md), [SPEC](SPEC.md), [CONTENT](CONTENT.md), [source map](REUSE_AND_ARCHITECTURE.md) and the selected ticket. Local IDs do not continue or change Campaign Foundation's completed numbering.

The recommended serial order is T01→T02→T03→T04→T05→T06→T07→T08→T09→T10→T11→T12→T13→T14→T15→T16→T17→T18. True blockers below are deliberately narrower; they do not authorize parallel implementation or simultaneous shared-scene edits. Every feature slice includes its actual interaction, data/state, persistence and acceptance. T01 is a bounded preparatory content-authority/fixture exception, not an unnecessary generic refactor.

| Ticket | Blocked by | Phase | Visible outcome |
|---|---|---|---|
| [T01 — Validate and bind the bounded tax content fixtures](tickets/T01-tax-content-and-fixtures.md) | None | A | Give subsequent tickets canonical, traceable annual records, rule constants and expected results without presenting prototype rates or unapproved assumptions as current tax law. |
| [T02 — Enter a safely specialized Level 3 scene with resumable chapter state](tickets/T02-safe-scene-entry-and-resume.md) | T01 | A | An isolated Level3 attempt enters the familiar office at reception, reads the appropriate next task and restores the same campaign state without starting old quests. |
| [T03 — Operate the physical calculator and attach an unscored practice tape](tickets/T03-physical-calculator-practice.md) | T02 | A | At the actual desk the player selects values, performs subtraction and attaches persistent calculation tape using the same tool later used for tax work. |
| [T04 — Meet Bea and acquire her physical annual records](tickets/T04-bea-encounter-and-original-records.md) | T02, T03 | B | Bea returns as a coherent client with contextual questions and actual readable records at the plaza worktable, leading into Worth the Tax? |
| [T05 — Reconstruct a reversible source-linked worksheet at Bea’s table](tickets/T05-worksheet-drafting-and-source-links.md) | T04 | B | Players lift seven working strips from originals, place them into four sections and explain two questionable claims without answer-revealing snapping. |
| [T06 — Check, correct and collect the worksheet with frozen mastery and service rewards](tickets/T06-worksheet-check-correct-and-collect.md) | T03, T05 | B | The player calculates subtotals, deliberately freezes one 12-decision grade, corrects the worksheet and acquires the case input with once-only 60 coins/+2 trust. |
| [T07 — Compute Bea’s taxable income and graduated tax using supported tapes](tickets/T07-individual-base-and-graduated-tape.md) | T01, T06 | C | At the office, players explicitly select supported totals, choose the annual bracket and construct a traceable base-plus-excess tax calculation. |
| [T08 — Inspect the 2307 credit and compute Bea’s remaining balance](tickets/T08-withholding-record-and-balance.md) | T07 | C | Players acquire a real Type 8 source, distinguish credit from expense and attach the remaining 50,000 balance without duplicating certificate income. |
| [T09 — Stamp and review Bea’s current computation with recoverable review hours](tickets/T09-bea-review-stamp-and-clock.md) | T08 | C | Players recommend readiness with the familiar physical stamps, submit through the Auditor conversation, correct root causes and earn one approved-case result. |
| [T10 — Complete Bea’s chosen explanation and physical computation-copy delivery](tickets/T10-bea-service-choice-and-copy-delivery.md) | T09 | C | Bea receives an understandable approved computation through one of two equally responsible service routes, earning +4 trust only on actual completed delivery. |
| [T11 — Receive the company appointment and resolve the personal-trip evidence question](tickets/T11-company-appointment-and-evidence.md) | T10 | D | A cooperative local company becomes the second active case without overwriting Bea’s result, and players compare the unsupported draft against actual records. |
| [T12 — Compute, review and explain the company’s supported tax balance](tickets/T12-corporate-computation-review-and-delivery.md) | T01, T09, T11 | D | Players reuse the established calculator/review/copy loop with a bounded corporate rule and justify the higher supported balance to Nina. |
| [T13 — Use carried upgrades, bookmarks, vouchers and decor throughout both cases](tickets/T13-carried-equipment-and-source-pins.md) | T06, T12 | E | Existing purchases remain useful and correctly scoped across Level2→Level3 and Bea→company, while all required work fits base capacity and free comparison. |
| [T14 — Record the supported finding and complete Chapter 3 through Career](tickets/T14-qualified-finding-and-chapter-wrap-up.md) | T10, T12, T13 | E | Players close the chapter with a qualified story discovery, correct aggregate rewards/history and safe progression back to Career. |
| [T15 — Buy and use the optional installed computer after chapter completion](tickets/T15-functional-computer-and-optional-office.md) | T14 | E | A funded player can buy a real installed 250-coin computer and immediately inspect both verified computations, using the same menu/counter ownership. |
| [T16 — Practice computation and replay mastery without changing the live campaign](tickets/T16-practice-and-mastery-replay-isolation.md) | T06, T12, T14 | F | Players can revisit unlocked instructions/drills and improve eligible best mastery using the established binder/Career paths while story state remains untouched. |
| [T17 — Polish the complete physical chapter flow and mobile presentation](tickets/T17-integrated-flow-and-mobile-polish.md) | T13, T14, T15, T16 | F | The implemented chapter feels like a continuous client-service adventure with readable paper interactions, phase-correct dialogue and consistent audio/input ownership. |
| [T18 — Verify the complete campaign handoff and enable Level 3 in Career](tickets/T18-release-verification-and-career-enablement.md) | T17 | F | Only the integrated internally ready Level3 is exposed as a normal campaign chapter, with exact acceptance evidence and preserved earlier levels. |

## Dependency and rollout notes

- T02 defines safe two-case entry/checkpoint contracts early; later tickets adopt them incrementally without exposing incomplete normal Career content.
- T03's same physical calculator serves worksheet subtotals (T06), graduated calculation (T07), credit/balance (T08) and company (T12); there is no horizontal tools-only ticket.
- T04–T06 produce the actual mandatory side-quest worksheet. T07–T10 turn it into a reviewed computation and completed service. T11–T12 are the reduced-guidance transfer case, not another duplicate scored quest.
- T13 exercises existing carried upgrades against real developed records/clock paths. Basic carry preservation already belongs to T02; it cannot be postponed until T13.
- T14 completes isolated story/result aggregation; T15 adds the fully functional optional purchase. Players can skip it. An explicitly deferred computer must stay unsellable, never gate story completion; this full feature pack includes T15.
- T16 adopts completed interactions for existing practice/replay, with live-state isolation. T17 polishes integrated seams rather than implementing omitted core features.
- T18 runs the final integration/authority gates and only then enables Career3. User Unity/device acceptance stays a separate pending status. L4/L5/stand remain pending.

## Lifecycle ownership

| Requirement | Primary tickets |
|---|---|
| Current-year rules, canonical figures, grading contracts | T01/T06/T07/T08/T12 |
| Scene/spawn/controller/camera/input/environment preservation | T02/T17/T18 |
| Physical new tools, sources, Type 8, calculator/tapes | T03/T04/T05/T07/T08/T11/T15 |
| Required side quest and corrected acquisition | T04/T05/T06 |
| NPC dialogue, choices, animation and actual deliveries | T04/T10/T11/T12/T14/T17 |
| Review/stamp/clock/vouchers/corrections | T09/T12/T13 |
| Persistent claims, XP/badges/coins/trust, two-case results | T02/T06/T09/T10/T12/T14/T16 |
| Existing upgrades/shop/bookmarks/decor and optional computer | T13/T15 |
| Story continuation/Career/replay/release | T14/T16/T18 |
| Mobile feel/audio/source and device verification | Each adopting ticket/T17/T18 |

Each ticket retains isolated development gates and current L1/L2 behavior. No production source/config/scene changes occur simply by generating this index.
