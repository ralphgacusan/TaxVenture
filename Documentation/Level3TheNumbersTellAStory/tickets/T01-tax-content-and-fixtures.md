# T01 — Validate and bind the bounded tax content fixtures

Status: **Planned**. Blocked by: None.

Priority: P1. Phase: A — Content preparation. Estimated scope: M; bounded preparatory exception; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

Give subsequent tickets canonical, traceable annual records, rule constants and expected results without presenting prototype rates or unapproved assumptions as current tax law.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scripts/Data/TaxComputationCalculator.cs`
- `Assets/Resources/Levels/Level_02/cases/case_rosa_001.json`
- `Documentation/SupportingDocuments.md`
- `Documentation/Level3DesignProposal.md`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Read CONTENT, REWARDS_AND_VALIDATION and proposal Sections4–7/15–17. Recheck exact current primary tax sources linked there; record passage, date, applicability and curriculum review status. Resolve individual graduated boundaries, corporate ordinary/small-company conditions, operating-year/MCIT scope, expense/credit assumptions and monetary rounding. Do not infer eligibility from a receipt alone or a foreign rule.
2. Keep the authored 2026 annual cases and financial fixtures. Formalize B/C source IDs, row/transaction references, periods, fixed-type mappings, withholding-subset relationships and answer keys. Keys remain separate from player snapshots. Supply the full annual individual bracket reference and a separate non-client worked example; no client-specific answers in book leaves.
3. Identify the production data-loader/validation shape to extend. Document proposed resource schema and exact source-to-field bindings in this pack before T02 creates it. Do not replace existing L1 calculator/rates globally just to make new fixtures pass.
4. Validate every fixture with exact decimal arithmetic, including independent wrong examples, bracket thresholds just below/at/above boundaries and rounding boundaries. Explicitly distinguish B-DRAFT costs 354,000, corrected300,000 and company draft2,120,000/corrected2,000,000.
5. Record verified vs educator-pending content status. If curriculum approval is unavailable, finish verified fixture/schema/document work and leave a named production-release gate; do not fabricate approval or prevent unrelated isolated development. T18 cannot call graded production content approved while that gate remains unresolved.

## Assets, motion and feedback

No new models/audio. Define only papers actually used, including Type 8 need; generic source layouts follow the existing standard.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Preparatory content exception, not runtime implementation or a new legal-rule framework. Subsequent data loaders must consume these fixtures; isolated drafts may exist while formal educator acceptance is pending. Changing a tax assumption requires updating dependent fixtures/review notes before adoption.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] Canonical IDs, periods, sources and expected chains are documented without conflicting amounts.
- [ ] Exact arithmetic verifies Bea600,000/62,500/50,000 and company6,000,000/1,500,000/1,200,000; current applicability and rounding status are explicit.
- [ ] All12 worksheet decisions and independent/dependent mistake treatment are specified; source keys are excluded from player data.
- [ ] Content-authority gate has an actual reviewed status or named pending reviewer/source, never a presumed pass.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Curriculum owner verifies bounded expense, credit, year/rate, corporate exceptions and rounding assumptions before graded release; no Unity check can substitute.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Canonical content/schema decisions and authority evidence in CONTENT/KNOWN_ISSUES/PROGRESS. No gameplay changes required. Next T02.

Direct dependents: T02, T07, T12. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
