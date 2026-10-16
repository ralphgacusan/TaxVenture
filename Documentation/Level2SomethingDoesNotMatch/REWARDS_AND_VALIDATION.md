# Rewards, recovery and verification

## Score contract

Reuse existing EXP, reputation hearts, BadgeTier, receipt emblem, medal card, score ledger, best-run career records and next-level passing presentation. Do not create a second score bar. Proposed Level2 values are intentionally forgiving, data-driven defaults awaiting user feel checks.

| Source | Maximum | Rule |
|---|---|---|
| Rosa investigation conversation | 1 heart | Once when required record conversation completes; no repeat farming |
| Mark the Money | 60 EXP, 3 hearts | First full check has 12 scored decisions. EXP = 60 minus number wrong, minimum 48. Three hearts for completing all required corrections; no penalty for tutorial or draft edits |
| Completed Rosa case | 100 EXP, 5 hearts | Deduct 2 EXP per distinct audited root cause and 1 per failed submission, combined maximum loss 15 EXP. Lose at most 1 case heart if any unsupported final submission was made. Supported interim holds are not failures |
| Late review | Within the same case loss caps | First cutoff miss adds 3 EXP loss and at most the same single case-heart loss; later days add nothing. No separate unbounded generic late penalty |
| Punctuality | Recognition | On Time stamp/label when final approval occurs before first cutoff; no extra uncapped EXP or separate reward stream |
| **Level total** | **160 EXP, 9 hearts** | Existing 80% pass rule requires at least 128 EXP and 8 hearts |

At completed mastery, even worst frozen quest accuracy (48 EXP) and capped case deductions (85 EXP, 4 hearts) produce 133 EXP and 8 hearts with required Rosa talk: a completed, corrected run can pass without restarting. Mistakes still lower rewards and badge. Test these bounds. Do not apply Level1 default case penalties in addition to these numbers.

Badge measures knowledge, not forgiving EXP: Gold = 12/12 first-check decisions; Silver = 10–11/12; Bronze = 0–9/12 after completing corrections. Use existing enum/art but a Level2 accuracy rule. **Do not pass the forgiving quest EXP ratio into LevelScoring.SideQuestBadge:** even zero correct earns 80% quest EXP and would incorrectly become Silver. Level1 badge calculation remains unchanged.

Freeze the complete first submission once; score incomplete submissions never. One incorrect treatment is one wrong response, not another error for every animation retry. Correction feedback can reveal the classification rule, but not summary coverage or F1/F2 solutions. Award quest exactly once after mastery; restoring a checkpoint does not repeat it. Draft annotation/finding revisions and stamps have no independent reward or loss. Root cause IDs deduplicate cascading errors in audit; repeated failed unchanged packets are blocked.

## Persistence

Use existing career save for completed best scores and best badge. A replay only adds career improvements under current anti-farming rules. Extend it with one optional active Level2 checkpoint, not a second storage framework. Save at cabinet retrieval, preliminary review, records collection, quest first-check, quest completion, office review commits, stamp impact, audit result and day rollover. Include frozen grade and penalty history; quitting cannot reroll a bad first check or clear deductions. Clear active checkpoint after completed result is safely recorded; failed writes must not grant duplicate rewards.

Resume acquired documents, annotations, phase, findings, review revision, current stamp, deadline day/remaining active time and reward flags. Rebind IDs to scene objects. Old saves without checkpoints still work. A game relaunch resumes a stable workstation/office spawn, not halfway through an animation or dialogue; replay unfinished presentation without repeating domain rewards. Explicit replay starts a new attempt; opening/closing book or desk never does.

## Internal checks (AI owned)

Documentation-only planning: check links, required ticket fields, dependency cycles, source paths and numerical fixtures. Do not run gameplay regression suites solely for new Markdown.

Implementation checks, from project root, using installed runtimes:

1. `python Tests/make_scratch_build.py`, then `dotnet build Temp/ScratchBuild/Scratch.csproj`. Scratch build validates source against installed Unity DLLs; it is not an editor/import/Play Mode result. Current generator includes scripts under its own rules; inspect/update minimally if new directories are missed.
2. Existing suites under `Tests/ProgressionRegression`, `Tests/GuideRegression`, `Tests/ControlsRegression`, `Tests/DialogueRegression`, `Tests/CareerBoardRegression`, `Tests/SettingsRegression`, `Tests/TrainingRegression`: run relevant `dotnet run --project ...csproj` after the scratch build as required by suite references. Do not blindly use stale `Temp/HudOverhaul/HudCheck`; it is mentioned in older docs but reported removed in current guide instructions.
3. Add a focused `Tests/Level2Regression` using existing external test conventions for pure high-level review/scoring/progression results. No new test framework dependency.
4. Extend or add a Level2-aware scene/content validator. Existing `Tests/VerifyGuideTargets.py` checks Office_Level1; do not assume it validates Level2 runtime targets. Validate static scene anchors and deterministic runtime-builder declarations separately; report what cannot be resolved outside Unity.

Required behavior evidence:

- New Office_Level2 created once per SCENE_REUSE; distinct GUID, source Level1/_OLD unchanged, shared assets retained, correct build/menu mapping and setup rerun safety.
- One Level2 case; office accessible before quest; Level1 defaults unchanged.
- Initial retrieval cannot archive/finish; preliminary hold cannot trigger audit/success.
- Four entries, 12 first-check answers; all tier boundaries 0/9/10/11/12; corrections cannot upgrade frozen tier; quest award once.
- Quest completion and Gold alone do not change summary/F1/F2/final verdict.
- Correct schedule is 20,000 May and 140,000 annual; loan and D6 cannot inflate totals; wrong but structurally valid choices are submittable.
- Correct omission/false hold/channel explanation tested through review evaluator; inherited fields still checked.
- Correct Not Ready acknowledged, no failure cost, no completion; Ready with unresolved issue returns; Ready with wrong reason returns; explained mismatch does not require a hold.
- One root cause counted once despite total/verdict/reason effects; caps and worst-run pass bounds exact; no double ledger credit.
- Changing graded state invalidates final stamp; cosmetic actions do not; obsolete-revision commits rejected; unchanged resubmission blocked.
- Deadline pauses for compulsory teaching/conversations/modals, not independent solving; only one miss penalty; no Auditor disappearance in Level2.
- Checkpoint restore retains first grade/penalties and prevents replay reward; old saves migrate; completed result saves exactly once.
- No Level1 profile/corkboard startup, duplicate NPC/guide/HUD/root, dangling document IDs or conflicting input holds in Level2.

## User Play Mode checklist

AI reports these as pending until the user confirms results. Provide this checklist with each relevant ticket; user does not assemble the feature.

| Check | Expected experience |
|---|---|
| Enter Level2 from career board and directly | Correct office start, Rosa case, one HUD, correct guide/map, no Level1 quest startup |
| Cabinet → desk → initial review | Clear physical folder retrieval, readable initial pages, satisfying preliminary stamp, new objective |
| Office → stall route | Reachable waypoint, no door gate, posts/camera/props do not block movement or dialogue |
| Toolkit tutorial | Real 3D case/markers/notes, readable touch targets, no input leakage, clean exit/reentry |
| Perfect/one-wrong/three-wrong challenge runs | Gold/Silver/Bronze respectively (three wrong means 9 correct), useful correction hints, tier stays frozen |
| Badge → office | Player knows income rules but must still reconcile summary and findings |
| Compare documents | Can inspect D1/D2/D6 without losing folder/book navigation; no double-count ambiguity caused by unreadable text |
| Stamp repeatedly/change work | Audible physical impact, page-bound ink, reason legible, no double commit, changed work needs restamp |
| Premature Ready / false hold / justified hold | Different appropriate reviewer/client responses, targeted recovery, no reward or completion on hold |
| Cutoff during reading/drag/stamp/result | Consistent pause rules, safe action boundary, Auditor stays, next morning retains work |
| Quit/relaunch and replay | Resume stable progress, no bonus farm, completed career result/badge correct |
| Success/finale/continue | Rosa reaction, neutral Desk fact, correct card sequence, Level3 progression per existing gate |
| Revisit Level1 | Original quest, gate, Marie case, clock, tutorial and rewards unchanged |

Performance and visual QA: check on intended phone orientation/device, paper readability, two-document comparison, frame pacing, material allocations, audio volume, reduced-motion behavior, modal close/back and pause/resume. Do not declare performance budgets measured from source inspection.
