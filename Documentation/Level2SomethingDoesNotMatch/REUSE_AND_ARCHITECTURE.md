# Reuse and implementation contracts

Source inspected 2026-10-17. These are anchors, not permission to replace entire classes. Read live implementations before edits. Comments describing historical FSM milestones do not prove those steps are active in current Level 1.

## Keep, extend, disable

| Existing area | Keep/reuse | Level 2 change |
|---|---|---|
| `Assets/Scenes/Office_Level1.unity` | Current saved office/plaza and wired tools as one-time scene baseline | Copy once per SCENE_REUSE; keep source untouched and referenced assets shared |
| `Assets/Scenes/Office_Level2.unity` | New scene with its own GUID, initialized once from current Level1 | Specialize Level2 configuration; later runs preserve existing work and never recopy baseline |
| `Assets/Scenes/Office_Level2_OLD.unity` | Previous scene reference only; observed current build entry points here | Preserve on disk; route new Level2 selection/build to new scene without reusing old GUID |
| `Assets/Scripts/Data/CaseProgressionManager.cs` | Level loading, one case lifecycle, score ownership | Start Level_02 in office; avoid legacy profile-start assumptions; one case ID only |
| `Assets/Scripts/Managers/CaseManager.cs` | Case load/reset ownership | Remove default-Level_01 leakage for this scene through existing level configuration |
| `Assets/Resources/Levels/Level_02/level.json` | Existing resource location | Replace placeholder name/two-case manifest with one Rosa case; unreference old cases, do not delete shared fixtures |
| `Assets/Scripts/SideQuest1/OfficeDoorGate.cs`, `Assets/Scripts/RewardsSystem/LevelGate.cs` | Level 1 side-quest-first behavior | Level 2 `requireSideQuestBeforeOffice=false`; final assessment uses investigation prerequisites, not physical office lock |
| `Assets/Scripts/Interactables/FileCabinetInteractable.cs` | Actual class `FilingCabinetInteractable`, existing archive action | Add separate, narrow retrieval interaction/phase; never make retrieval archive or complete the case |
| `Assets/Scripts/Interactables/DeskDocumentAnimator.cs`, `DeskMotion.cs`, folder/book interactables | Desk settling, paper lift, book/folder drag/return | Reuse for new pages and market workstation; retain one active interaction owner |
| `Assets/Scripts/Data/DragDrop/ValueTransferManager.cs` and existing field destinations | Tap/tap or drag/drop semantic values | Transfer supported entries into summary; a valid slot fit does not certify correctness |
| `Assets/Scripts/UI/DocumentViewerManager.cs`, `SupportingDocumentsPageLink.cs`, book/paper presentation | World-space readable documents and compare behavior | Add record templates and evidence IDs; no alternate document application |
| `Assets/Scripts/UI/Stamp3DDrag.cs`, `StampUI.cs`, `StampTransitionController.cs` | Existing physical stamps and motion | Add phase/revision/reason-aware commit and satisfying impact; existing Level 1 callers retain behavior |
| `Assets/Scripts/Data/CaseVerdictEvaluator.cs` | Legacy field-completeness behavior for cases without review rules | Optional evidence-aware Level 2 readiness rules; current implementation checks non-null fields and is insufficient alone |
| `Assets/Scripts/Data/ComplianceChecker.cs`, `SubmissionResult.cs` | Audit entry/result seam | Populate supported false/missed findings; `FalseIssues` is currently a TODO placeholder, not a finished feature |
| `Assets/Scripts/Data/AuditorSubmissionTray.cs`, `Assets/Scripts/Interactables/AuditorInteractable.cs` | Folder hand-in and retry reset | Separate justified hold from failure/success; unchanged-packet prevention includes meaningful review changes |
| `Assets/Scripts/RewardsSystem/LevelScoring.cs`, `LevelScoreLedger.cs`, `LevelRewardRules.cs` | EXP/hearts, BadgeTier enum, ledger, rule loading | Level 2 per-decision scoring adapter and explicit accuracy tiers; preserve Level 1 formulas |
| `Assets/Scripts/UI/HUD/HudBadgeArt.cs`, existing reward cards | Medal art and result presentation | Receipt emblem, Mark the Money identity, frozen first score and current mastery distinguished |
| `Assets/Scripts/UI/HUD/OfficeHudRuntime.cs`, `Assets/Scripts/UI/OfficeMinimap.cs`, `Assets/Scripts/UI/Guide/GuideRuntime.cs` | Existing HUD, map, arrows, observation-only lessons | Currently scene-restricted to Office_Level1; explicitly opt in Level2 and select Level_02 content, do not globally enable in menu/training |
| `Assets/Scripts/UI/HUD/OfficeHours.cs`, `Assets/Scripts/Data/OfficeHoursMath.cs` | Clock, warning chip, pause predicates, rollover presentation | Currently Level1-only; introduce data-selected stays-present cutoff, active-time allowance and desk rollover; Level 1 leave behavior unchanged |
| `Assets/Scripts/UI/HUD/SceneFade.cs`, `CaseFinale.cs` | Fade and staged reward-card release | Currently Level1-only; Level2 opt-in, Rosa identity and approval-only completion, no premature empty card |
| `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`, `PlayerProgressManager.cs`, `Assets/Scripts/Save/SaveFileStore.cs` | Best-score/badge records, unlock policy, atomic storage | Backward-compatible optional active Level2 checkpoint through the same save owner; do not rotate signature key or invent second save file |

## Scene creation decision

[SCENE_REUSE.md](SCENE_REUSE.md) is authoritative for scene initialization. Create a new Office_Level2 from current saved Office_Level1 once, sharing referenced assets and preserving source bindings. Then disable only copied Level1-specific flow and add Level2 content. The previously observed Office_Level2 has since become Office_Level2_OLD; do not assume the new destination already exists. No whole-world prefab/additive-scene refactor is part of Level2.

## Explicit scene-asset reuse

Rosa uses `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Female_03.prefab`, referenced under Characters in `Assets/Scenes/TaxVentureWithCompliance.unity`. Her plaza-side store uses `Assets/Flat_Style_Vehicles/Prefabs/Buildings/Building_4.prefab` with the appearance/overrides of `Building_4 (1)` under Env/Town in `Assets/Scenes/TownEnvironment.unity`. These are user-selected assets, not optional placeholders. Inspect and reproduce relevant instance overrides in Office_Level2 without editing the source scenes or shared prefab originals. Reuse rig/materials and add compatible animations; construct only missing gameplay props. See WORLD_AND_FEEDBACK for GUIDs and placement rules.

## Level 1 content disabled only in Level 2

Disable automatic four-profile collection, corkboard classification startup, Know Your Taxpayer quest/award, plaza-to-office gate, Marie/Teody case selection, Level1 welcome tutorial and Auditor-emergency-departure copy. Preserve their source and Level1 scene wiring. Reuse their components only where generic behavior fits; do not rename SideQuest1 classes repo-wide.

Old Office_Level2 placeholder cases are not authoritative design. Preserve unrelated scene objects and references; disable conflicting start controllers by Level2 configuration. Inventory conflicts in ticket 01 rather than bulk deleting legacy objects.

## Minimal data boundaries

Proposed file names below are planned, not existing APIs. Use current definitions where possible; add only optional members. No general quest engine or plugin framework.

- `Levels/Level_02/level.json`: one case; capability/profile selection if necessary.
- `cases/case_001.json`: Rosa case, required fields, narrative, evidence/finding rule references.
- `income_review.json`: six document definitions, four entries, source/type/treatment options, separate answer keys, tutorial example, book-rule references.
- `objectives.json`: state-aligned steps and waypoint names.
- `rewards.json`: Level2 scoring/caps/badge configuration; no Level1 default silently substituted for a malformed Level2 review file.
- `office_hours.json`: stays-present cutoff policy and pause/rollover settings.
- `Tutorials/Level_02/guide.json`: short new-mechanic lessons only.
- Book pages use existing TaxCodeBookData/content pipeline. Inspect how it is populated before deciding on an optional level-specific content leaf.

Stable IDs: level Level_02, case L2-ROSA-001, quest mark_the_money_2; entries E1–E4, documents S1/N1/D1–D6, findings F1/F2, reasons/rules with stable strings. Display names must not be keys. Cross-reference validation rejects dangling IDs, inconsistent dates/amounts and duplicate keys. Internal answer keys are not placed on player-facing pages; offline data need not be encrypted.

Runtime state, under existing case/save ownership: current investigation phase; acquired evidence IDs; per-entry draft and first-submitted responses; frozen accuracy; corrected annotations; finding conclusions/evidence/reasons; selected income schedule rows; packet revision; preliminary/final stamp and their reason/revision; feedback root cause IDs; acknowledged hold revision; audit penalty counters; deadline active time/day/late flag; quest/reward/story flags. Defaults absent in old saves must initialize safely. Never serialize scene object references as progress.

## State and event ownership

Use one small Level2 phase controller/state model alongside existing case progression. Translate phase changes to existing progression at defined boundaries; do not keep two independent owners racing to complete the case. Reuse existing case FSM where it actually governs interactions. The controller owns prerequisites and phase; views render state; guide observes state/events. Keep deterministic evaluators free of animation and Unity timing so current scratch-built regression seams can test them.

Useful notifications: FolderRetrieved, PreliminaryFindingRecorded, PreliminaryAssessmentCommitted, RosaRecordsCollected, TutorialCompleted, AnnotationsSubmitted, AnnotationsCorrected, IncomeQuestCompleted, ReviewChanged, FinalAssessmentCommitted, SupportedHoldAcknowledged, AuditReturned, CaseApproved. These describe completed domain actions, not tutorial requests. Reopening a view reads current state; it must not emit another collection or reward action.

## Important guards

- Source item transfer is idempotent by entry ID; D6 cannot become a second independent service-income row.
- A syntactically valid but semantically wrong choice is accepted for later audit. Do not use answer-key guards as drop validation.
- Changing review data increments revision and invalidates final stamp; restamping itself does not change the packet fingerprint or earn anything.
- Incomplete mandatory fields block submission with a neutral instruction; unsupported but complete findings reach the Auditor.
- Holds never increment case index, trigger success finale, archive the case or award a case reward.
- Freeze quest score at first full check, award once after mastery, retain tier on correction, save/replay use best completed-run rules.
- Deadline/reward/modal races must have one transaction outcome; no duplicate audit, hand-in or reward on a repeated event.
- Scene reload clears subscriptions/runtime meshes/materials; returning from desk/dialogue does not duplicate Level2 roots or UI.

## Extension to later levels

Reuse evidence links, finding statuses, reason IDs, packet revision and evaluator results. Later content can add a new supported rule/finding without replacing the input, paper, stamp or Auditor loop. Do not build later scenarios now or hardcode F1/F2 into shared components.
