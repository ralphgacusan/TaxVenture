# Internal checks and separate user acceptance

Authoring check only: `python Documentation/Level3TheNumbersTellAStory/verify_pack.py`. Its result concerns docs, links, references, dependencies and fixed arithmetic only. All implementation/gameplay/Unity checks below are planned and unchecked until actually run. No historical suite count is a new result.

## Implementation verification procedure

1. Reinspect current suite csproj/runtime paths and save sandbox. Use disposable fixture directories under Temp; never user save/PlayerPrefs data. Preserve current warnings and baseline regressions rather than overwrite them.
2. Existing scratch entry: `python Tests/make_scratch_build.py`, then `dotnet build Temp/ScratchBuild/Scratch.csproj --nologo`. Verify new runtime sources are included. Editor setup code may need separate compilation against installed editor DLLs; never launch Unity as an AI test. Level2Regression currently reads the built scratch DLL, so build before it; no false pass from stale binaries.
3. Add proposed `Tests/Level3Regression/Level3Regression.csproj` using existing dependency-light console conventions. Keep pure arithmetic/state/claims tests independent of Unity where possible; high-level tests call actual production commands and round-trip serialized snapshots. New test paths are proposed, not present at authoring.
4. Run affected existing suites, selected by change: CampaignFoundationRegression, CampaignFoundationRuntimeRegression, Level2Regression, Level1LivingPlazaRegression, ProgressionRegression, CareerBoardRegression, ControlsRegression, DialogueRegression, GuideRegression, TrainingRegression, AudioRegression, SettingsRegression. Verify each path at execution; use current counts/outcomes only. Do not broaden/repeat without new changes or concerns.
5. New L3 static validators should follow VerifyLevel2Assets/Scene/GuideTargets patterns: distinct GUIDs/source hash preservation, missing references, correct actual and runtime target bindings, one manager owner, disabled old flows, loaded content IDs/source maps and build/Career gates. Test declaration is not proof of reachability/rendering.
6. At T18 run the complete affected integration gate and document exact user acceptance route. Enable Career only after internal readiness; user manual acceptance remains a separate status. User-reported blockers must be addressed before calling the chapter user-accepted.

## Required internal behavior matrix

| Area | Concrete checks | Owners |
|---|---|---|
| Content authority | Annual period/regime/expense/withholding/corporate assumptions, all bracket boundaries, exact rounding, source passages/review status | T01 |
| Entry | L2 completed vs not; comingSoon remains during development; exact persisted possessions; no intro/quest reset | T02/T13/T18 |
| Scene | Saved-source hashes preserved; distinct scene GUID; idempotent setup; no active inherited quest/reward/checkpointer | T02/T17/T18 |
| Tool practice | 100−20=80−8=72, no wallet change/grade, cancel/resume, no duplicate key from held input | T03 |
| Records | Actual acquire once, full Bag/Make room, no answer key in snapshot, source/type matching, source remains on failure | T04/T08/T11 |
| Worksheet | Wrong placements accepted neutrally; no duplicate extraction; claim fields visible on wrong destination; incomplete vs first complete12; frozen correction; subtotal dependency fairness | T05/T06 |
| Calculator | Exact decimal/percent, zero/invalid/overflow/negative handling, source/manual provenance, result destination, chain equivalence, source tabs, no auto-solving | T03/T07/T08/T12 |
| Revisions | Source treatment changes invalidate relevant dependents; credit-only change preserves base; read/move is not edit; stale tape/stamp cannot approve | T07/T08/T09 |
| Review | Correct/hold/premature/stale/false hold; root causes/retries/caps; no L4 filing; wrong-case folder/certificate refused | T09/T12 |
| Clock/voucher | 16/10 active budgets, existing pauses, free rollover, two separate cases, one voucher/case across days, rollback and no real supply in replay | T09/T12/T13 |
| Choices | Context inquiry, both service routes, deferral/cancel/change plan, wrong explanation correction without ethics penalty, history variants without replayed Rosa consequences | T10/T12/T14 |
| Delivery | AtAuditor/carried/done on both cases, correct client/action, pending explanation, failed commit/reload, no Bea room/copy interference at case switch | T10/T11/T12 |
| Campaign rewards | 60/100/100 once, 60+100+100 maxXP without doubled worksheet XP, +2/+4/+4 once, path64/54/59, cap0/100, duplicate callback/write failure | T06/T09/T10/T12/T14 |
| Equipment | Actual capacity/ownership/pins/decor; stale source unpin; base9 total slots sufficient; no playable drawer/stand unlock | T13 |
| Computer | Insufficient funds/cancel/double purchase/failed save/anchor missing, same menu/counter wallet, immediate real register, eligibility only completedL3+feature readiness | T15 |
| Modes | Story vs mastery best-only vs Practice lesson-only vs DirectEditor; no checkpoint/supply/story mutation in replay | T02/T16 |
| Closing | Limited supported fact, mayor message, pending final card recovery, completion acknowledgment, optional epilogue no timer/rewards, L4/L5 pending | T14/T15/T18 |
| Modal/audio | Back/disable/focus loss, no tap-through/look leakage, camera-return sequencing, SFX/music settings, one cue/event, cleanup subscription lifetime | Each adopting ticket/T17 |

## USER manual acceptance scripts

Use isolated seeded test campaigns supplied by implementers, not hand-editing the real campaign. AI builds all scene/prefab bindings and fixtures; user only accepts the result.

1. Finish L2 or use a safe completedL2 fixture. Continue opens Career. Start L3 with base inventory, then a separate owned-upgrades fixture. Check correct wallet/hearts/history/decor/supplies/capacities and no repeated acquisition. Confirm original L1/L2 still work after shared changes.
2. Inspect first-person walking/look/joysticks, existing doors/walls/ceiling behavior, reception spawn, lighting and desk/table reach. Confirm there is no new third-person mode or duplicated manager/NPC.
3. Complete protected budget practice. Inspect real calculator depth/model, large keys, readable display/tape, responsive press, tap/drag alternatives and Exit Desk. Wallet remains unchanged. Interrupt/Back/settings/app-focus and return without stuck controls.
4. Meet Bea; use source questions in either order; leave dialogue early and return. Collect/read originals. Test full Bag using fixture and Make room. Confirm table/source targets readable and one actual record acquisition.
5. Build worksheet imperfectly: personal expense in wrong tray, duplicate invoice counted, wrong relationship/reason, arithmetic matching a wrong chosen set. Observe neutral drafting. Leave/reopen, Check incomplete, then deliberately freeze first complete score. Correct through source-linked hints, retain initial medal, collect worksheet once. Check +60 coins/+2 trust after saved acquisition only.
6. At office compute taxable income, choose bracket, calculate excess/marginal/base chain, collect certificate, place credit. Try whole-base marginal multiplication, wrong destination, manual source entry, stale tape and credit-as-expense. Observe review cause and preserved unrelated work. Confirm correct result600,000/62,500/50,000. Inspect stamp ghost/body/ink/current-revision behavior and talk-first hand-in.
7. Run both Bea service-plan routes/deferred plan on separate fixtures. Complete the single brief task, read/carry/hand over correct copy with drag and tap. Interrupt before pickup, carried, explanation and handover. Confirm +4 once, no chapter-complete card yet, then company appointment.
8. Meet Nina only after Bea delivery. Inspect new source identity, original/summary comparison and question, exclude personal trip, compute company result6,000,000/1,500,000/1,200,000, choose supported explanation, deliver. Confirm separate100 coins/+4 and no second side-quest badge or overwritten Bea data.
9. Test each case cutoff, free next day, one voucher per case, third voucher refusal and failed commit fixture. Confirm active time pauses consistently, returns keep work, and lateness does not lower public hearts.
10. Read qualified finding and mayor message. Try Review records then return; do not confuse discrepancy with proof of intent. Acknowledge final card; verify260 chapter coins before spending,260 max mastery on perfect fixture,1 badge,+10 service maximum and preserved prior history. L4/L5 remain coming soon. Reload pending wrap-up, completed Career and optional office.
11. Visit optional counter or shared Career shop. Check all actual upgrade prices/ownership. Buy computer on a funded fixture, inspect real installed device and verified two-client register. Try insufficient funds, cancel, double confirm, save failure and missing-anchor fixture. Stand stays pending. Leave without purchasing and chapter remains complete.
12. Replay/practice then resume story: only eligible best mastery or lesson progress changes; supplies/coins/trust/choices/checkpoint remain intact. Test portrait/tall/wide phone sizes, Android Back, SFX/music off/on, still-paper reading, camera transitions, NPC travel/feet/gestures, occlusion, touch targets and measured Android frame time/memory. Record defects and actual measurements; no preset performance pass is assumed.

Inherited manual gaps and CF-37 outline-renderer issue stay explicit. New feature acceptance must not silently repair project-wide render settings or certify old gaps from source inspection.
