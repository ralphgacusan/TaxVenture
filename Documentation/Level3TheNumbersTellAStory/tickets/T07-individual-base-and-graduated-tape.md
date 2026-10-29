# T07 — Compute Bea’s taxable income and graduated tax using supported tapes

Status: **Planned**. Blocked by: T01, T06.

Priority: P1. Phase: C — Individual computation. Estimated scope: M; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

At the office, players explicitly select supported totals, choose the annual bracket and construct a traceable base-plus-excess tax calculation.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scripts/UI/CaseFolderUI.cs`
- `Assets/Scripts/UI/TaxCodeBookUI.cs`
- `Assets/Scripts/Data/TaxComputationCalculator.cs`
- `Assets/Scripts/FSM/States/ComputeTaxesState.cs`
- `Assets/Scripts/Data/CaseProgressionManager.cs`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Bind the familiar folder first pages to Bea and add Computation working leaf with revenue/expenses/taxable income/rule/tax due/credit/payable steps. Explicitly select/confirm the acquired worksheet totals; no hidden key fill. Keep source tabs reachable and active-case label visible.
2. Source900,000−300,000→600,000 tape, attach to Taxable Income. Book provides full year-labeled individual table; player selects containing bracket, confirms generic formula constants and builds excess600,000−400,000=200,000, ×20%=40,000, +22,500=62,500. Select constants from reference, not a client solved example.
3. Implement exact-money opted-in individual rule evaluation after T01 authority and generic arithmetic support from T03. Equivalent combined expressions or supported subtapes pass; final-value coincidence with wrong regime/support does not. Preserve L1 prototype/guided behavior unless separately validated compatible.
4. Attach result to named field with explicit confirmation. A plausible wrong destination/value remains draft work until review; mechanical wrong-case sources refused with reason. Manual numeric entry requires source link for final support. No auto-select bracket, all-fields compute or arithmetic race.
5. Implement dependency/revision invalidation: editing a supported base/amount dirties dependent taxable/tax/payable/stamp state; reading/moving a paper does not. Preserve old tape with Needs recalculation; changing rule does not erase unrelated source totals. Save draft/attached chains and resume in stable desk state. Credit is pending until T08, so no approval yet.

## Assets, motion and feedback

Existing folder/book/calculator plus new computation leaf/tape attachment labels. Readable intermediate lines may collapse but prerequisite chain remains inspectable.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Expose actual computation facts to T09 validator; GameStateMachine shows Compute Taxes but never advances into L4 filing or legacy single-case completion. Typed exact-money domain state must not silently lose cents when bridged to older float fields.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] Actual worksheet selection and rule→tape→field chain yields600,000 and62,500; input/rule/source identities persist.
- [ ] All individual bracket boundaries/rounding per T01 and equivalent valid chains have tests; wrong whole-base marginal calculation cannot approve.
- [ ] Edit/read/credit-independent revision cases invalidate only affected dependents and retain old work.
- [ ] Exit/reload/failure preserves tape/destination/manual provenance and current phase; earlier levels unaffected.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User checks bracket/table readability, source/constant selection, tape history, intermediate flow, no long-number memory requirement or cramped keypad.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Supported individual taxable base and graduated tax due, credit/payable still pending. Next T08.

Direct dependents: T08. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
