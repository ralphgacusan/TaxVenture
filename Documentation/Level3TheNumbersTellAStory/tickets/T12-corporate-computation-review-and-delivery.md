# T12 — Compute, review and explain the company’s supported tax balance

Status: **Planned**. Blocked by: T01, T09, T11.

Priority: P1. Phase: D — Corporate transfer challenge. Estimated scope: M+; existing mechanics with new rule/content; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

Players reuse the established calculator/review/copy loop with a bounded corporate rule and justify the higher supported balance to Nina.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scripts/Data/TaxComputationCalculator.cs`
- `Assets/Scripts/UI/TaxCodeBookUI.cs`
- `Assets/Scripts/RewardsSystem/CampaignRewards.cs`
- `Assets/Scripts/RewardsSystem/LevelScoring.cs`
- `Assets/Scripts/UI/DialogueSession.cs`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Reuse current worksheet/computation/tape/credit/stamp/review components. Player adjusts unsupported draft costs 2,120,000 to2,000,000 using C-TRIP purpose support and leaves personal120,000 non-deductible. No second full twelve-decision grade; this is a main-case application.
2. Select supported revenue 8,000,000−costs 2,000,000→taxable 6,000,000. Corporate rule reference explains ordinary25% and qualifying smaller rate conditions; supplied assets80,000,000 and net taxable>5,000,000 establish ordinary rule. Bound MCIT/incentives per T01. Compute1,500,000 tax−300,000 supported credit→1,200,000 payable.
3. Preserve reduced guidance, actual rule choice and valid equivalent source-linked arithmetic. Wrong individual bracket, unsupported trip deduction, double credit or cross-case source remains reviewable mistake. Dependency invalidation and current stamp requirements reuse T07–T09.
4. Company actual review approves its separate case result/XP/100coin claim, copyAtAuditor and stopped clock in one save. Keep chapter/Bea snapshot. Root cause/retry cap same as Bea; voucher claim independent, free rollover retains work.
5. Nina asks why amount exceeds Desk draft1,170,000. CONTENT’s explanation branches require point to120,000 adjustment. Wrong rule/motive claims prompt correction, not ethics penalty. Awaited correct-client copy delivery saves explanation/copyDone/+4 trust and ClosingFinding admission. No duplicate side-quest XP/badge or chapter completion yet.

## Assets, motion and feedback

Existing developed calculator/tapes/book/stamp/copy/Type 8, company printed rule/adjustment labels and existing emote/FX. No additional arithmetic minigame or sourced art.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Second case is the critical regression for distinct reward/copy/clock identity. Do not use one chapter-wide approval claim or reuse Bea delivery target. Completed case snapshots support later optional computer register.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] Real company chain yields6,000,000/1,500,000/1,200,000 with supported personal adjustment; wrong rules/treatments/credit fail appropriately.
- [ ] Company100 coins/+4 trust and case XP occur once and do not regrant worksheet/Bea outcomes.
- [ ] Both copy phases/explanations/failed-save/reload/day/voucher paths remain distinct; root error scoring bounded.
- [ ] Supported explanation admits closing finding; speculative intent correction has no moral penalty or auto-approved journal allegation.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User completes company from appointment to actual delivery, checks comparative draft/corrected amount explanation and familiar controls with less guidance.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Both main cases resolved and company delivery saved; closing discovery next. Next recommended T13.

Direct dependents: T13, T14, T16. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
