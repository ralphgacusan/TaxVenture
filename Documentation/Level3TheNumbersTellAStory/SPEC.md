# Level 3 implementation specification

Authority: [proposal](../Level3DesignProposal.md), the user's two attached requirements, latest existing campaign amendments, and verified live behavior. This pack resolves implementation details without authorizing runtime edits now. Stable IDs below are **proposed**, not existing APIs/assets.

## Identity and resources

Chapter `Level_03`; proposed scene `Assets/Scenes/Office_Level3.unity`; owned root `Level3Gameplay`; cases `L3-BEA-001` and `L3-MARINE-001`; badge `worth_the_tax_3`. Planned level-specific runtime paths: `Assets/Scripts/Level3/`; pure data/rules in existing Data/RewardsSystem conventions as appropriate; resources under `Assets/Resources/Levels/Level_03/` and `Assets/Resources/Tutorials/Level_03/`. All are proposed destinations. Preserve serialized enum values and original GUIDs; each new asset needs a unique .meta.

Planned content files: level.json, office_start.json, objectives.json, book.json, rewards.json, town_life.json, scene_assets.asset, two case JSONs, worksheet/rules content as a small authored structure, and guide.json. Reuse actual loaders where compatible; do not create files that no production entry point reads. T01/T02 decide the smallest compatible shape and record it before later adoption.

## Deterministic chapter phases

| Phase | Admission/action | Durable exit and next phase | Error/recovery |
|---|---|---|---|
| Reception | Story starts from completed L2, or isolated development fixture | Receive Bea assignment → ToolPractice | No fresh campaign/reset; checkpoint wins over intro |
| ToolPractice | Existing guide/workstation lesson, office-budget sample | Player attaches 80 and 72 tapes → BeaRecords | No score/coins/trust/campaign spending; cancel/resume |
| BeaRecords | Talk at plaza table, inspect/hand over B records | Actual acquisition → WorksheetDraft | Full Bag/save failure retains source |
| WorksheetDraft | Seven strips, two claim notes, relationship, subtotals | Deliberate complete Check → WorksheetCorrections or WorksheetReady | Incomplete draft never freezes grade |
| WorksheetCorrections | Existing frozen first grade; player edits wrong work | All twelve correct and current totals → WorksheetReady | Preserve correct work; Exit Desk works |
| WorksheetReady | Deliberate worksheet pickup | Same save grants +60 coins/+2 trust and stores mastery → BeaComputation | No reward before acquisition or on replay |
| BeaComputation | Select worksheet totals, apply individual rule/credit | Current work stamped and submitted → BeaReview | Rule/input changes invalidate dependents, not originals |
| BeaReview | Talk-first requested folder handover to Auditor | Supported approval +100 coins; copy AtAuditor → BeaDelivery | Returned audit focuses root cause; justified hold stays open |
| BeaDelivery | Pickup copy, service-plan/explanation, awaited handover | Delivery +4 trust, retain frozen Bea result → CompanyAppointment | Failed commit retains copy; no premature room reuse |
| CompanyAppointment | Receptionist gives second case and C records | Company active binding → CompanyComputation | Atomic binding only after Bea delivery; copies/source IDs stay scoped |
| CompanyComputation | Resolve personal trip, corporate rate, credit, balance | Current stamp/submission → CompanyReview | No second scored side quest or second field fetch loop |
| CompanyReview | Same physical audit with company facts | Approval +100 coins; copy AtAuditor → CompanyDelivery | Correct before completion; root error penalties only |
| CompanyDelivery | Explain higher amount, awaited correct-client handover | +4 trust → ClosingFinding | Claim unsupported motive must be corrected, no new misconduct system |
| ClosingFinding | Retain summary/original, choose supported journal fact | Mayor message seen → WrapUpPending | Review records returns safely; no extra trust |
| WrapUpPending | Read chapter receipt, acknowledge Continue | Chapter completed → Career or optional CompletedOffice | Failed save keeps final card pending, no award replay |
| CompletedOffice | Optional supply counter/computer after completion | Leave → Career | No review timer, reopened case, copy, or second completion |

Use existing GameStateMachine as the active coarse game phase owner. Chapter phase is saved domain data with a bounded transition validator, following L2's existing review-data pattern; it is not a second ticking FSM framework. Explicitly map coarse states to chapter phases and suppress old mandatory PrepareReturn/ArchiveCase/LevelComplete transitions that would skip L3's two-case delivery sequence. Objectives and guides observe committed state.

The 16-active-minute Bea review starts after protected tool teaching, covers field/office independent work, pauses only under existing policy, and has next-day recovery. Company receives a fresh 10-active-minute window at its own admission; no unseen inherited deadline. One voucher per case, two carried; +180 active seconds and consumed quantity save atomically. These are initial tuning fixtures, not measured playtest results.

## Checkpoint and commands

Extend the existing signed save/checkpoint owner with a bounded chapter snapshot: schema version; current phase/activeCaseId; intro/practice milestones; acquired source IDs; worksheet draft/reasons/link/current totals; first-check frozen correct/total/wrong IDs/tier; correction state; selected service plan and completion; per-case source fingerprint, taxpayer fields, supported amount bindings, selected tax regime/year/rule, tape expression/source edges/result/destination, dirty dependency flags, packet/stamp revisions, review root-cause history, case XP/result; separate clocks/voucher flags; copy phases; dialogue facts; finding/wrap-up/epilogue facts. Keep Bea's frozen result through the company case. Do not serialize Unity objects, animation frames, raw camera transforms, answer keys or speculative allegation as verified finding.

Every durable operation validates run mode, chapter/case/source ownership, prerequisites, revision, and duplicate action ID before candidate/save/publish. A failed write returns to the preceding state with the actionable source/copy still available and no success FX. Restoring state never emits reward/acquisition/dialogue-choice events. Drafts save at stable edits and app pause/quit using existing bounded dirty-save behavior, not every frame or key animation.

Claim activities: `bea-worksheet`, `bea-case-approved`, `bea-follow-through`, `marine-case-approved`, `marine-follow-through`; kind separates coins/trust/mastery as existing owners require. Scope uses campaign/Level_03/activity/kind. Voucher claims also use case ID. Case XP is per case; side-quest XP once only. Do not call Rosa-specific commit/delta APIs for Bea.

## Input, calculations and validation

Same first-person exploration, Workstation/Interview views, joysticks and existing window locks. One pointer/action owner. Tap-place equivalent to dragging; target release, cancel, Android Back, settings interruption, disable and camera-return callbacks release only their own ownership. Exit Desk is allowed during independent worksheet/calculation work; never repeat L2's superseded quest-wide exit lock or first-CHECK forced conversation.

Source extraction carries source row identity and printed amount automatically. Re-extraction focuses existing strip. Valid-content destinations accept wrong answers until deliberate Check; mechanical invalidity such as a foreign case source is refused with an explicit reason. Two claim notes/reason fields and their relationship field stay accessible regardless of placement. Twelve independent checks are defined in CONTENT; subtotal arithmetic is checked against the player's chosen set so one treatment error is not double-penalized.

Calculator needs persistent selected-source chips, editable expression, basic operators/percent, draft tape, explicit result attachment and source viewing. Use exact money and explicit rounding policy after T01 authority review; no float tolerance that permits wrong cents, silent negative clamping or implicit zero. Authored cases avoid loss/overpayment; unexpected negative work remains visible/check-needed. Percent means A ×20% = A ×0.20, not ambiguous consumer addition shortcuts. Equivalent valid expressions using supported inputs/rules pass. A matching final number alone is insufficient evidence.

Dependency chain: source/treatment → totals → taxable income → rule/tax due → credit/balance → stamp. Credit-only changes invalidate balance/stamp, not income arithmetic. Reading/moving a paper is not a revision. Old tapes remain labeled stale until explicitly replaced. Stamp remains player's recommendation for computation review, not filing/payment certification. Holds do not finish unresolved work.

## Release and continuity

Chapter 3 remains coming soon during partial tickets; isolated fixtures do not write live story. T18 verifies and exposes normal Career entry using actual completed L2 and readiness configuration, not a mutable player flag or score gate. Computer eligibility additionally requires completed L3 AND a shipped functional device slice. Stand remains pending. L4/L5 remain coming soon. No ending calculation, return filing, nation/guide remapping, or historical-folder archive is added.
