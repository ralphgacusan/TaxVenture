# Backend polish (Level 1 ready for reuse)

Behind-the-scenes hardening only: no scene, visual, animation or flow
changes. The goal is that Level 1's systems are safe to reuse for later
levels, and that everything a designer tunes lives in JSON.

## What changed

### Rewards (`Assets/Scripts/RewardsSystem`, `Data/RewardsCalculator.cs`)
- **Score never passes its maximum.** `LevelScoreLedger` keeps
  `TotalExp` / `TotalHearts` between 0 and `MaxExp` / `MaxHearts`.
  - Before: a perfect run plus the on-time bonus read 170 / 150.
  - A bonus (`AddBonus`) now only fills the room left below the maximum, so it wins back lost points. If there is no room, it adds nothing (no entry, no notice).
  - `BonusExp` / `BonusHearts` report what bonuses actually added.
- **Client-talk hearts stop at `conversation.count`** different clients.
- **Every amount is clamped**, so no single reward is above `LevelScoreLedger.MaxSingleAmount`. A HUD listener that throws can't break scoring.
- **`rewards.json` is validated.**
  - `LevelRewardRules.Sanitize()` clamps each number (0..100000; `passingPercent` 1..100), and cleans `heartsLostAtWrong` (positive, distinct, sorted) and `officeDoors`.
  - It logs ONE warning naming every value it fixed.
  - The file is read once per level and each caller gets a copy (`Load` / `Clone`; `ClearCache()` to re-read).
- **Case rewards come from JSON.** `RewardsCalculator` used to hold the numbers 100 / 5 / 15 / 1 / 20 in code. It now reads `rewards.json` `caseFile` (new field `wrongVerdictExp`, default 20).
  - Values are unchanged, and the Auditor path is unchanged (`LevelScoreLedger.AddCase`).

### Saving progress (`Save/SaveFileStore.cs`, `RewardsSystem/PlayerProgress*.cs`)
- **Crash-safe writes.** The save is written to `progress.json.tmp`, flushed, and then swapped in. The previous save becomes `progress.json.bak`.
- **Tamper check.** The file is an envelope `{format, payload, signature}`, where the signature is an HMAC-SHA256 of the payload.
  - A save that fails the check is kept as `progress.json.rejected` and the backup loads instead. Nothing is ever deleted.
  - This stops editing the save in a text editor. It is not DRM.
- **Old saves still load.** Plain version 1 and 2 files load and are rewritten signed. A plain file claiming version 3 or higher is rejected, because it can only be hand-made.
- **Repair on load.** `PlayerProgressData.Normalize()` (save version 3) fixes:
  - a best score above its maximum, negative totals and negative attempts;
  - duplicate level records (merged into the best of both) and duplicate badges (best tier kept);
  - badge tiers outside 0..3;
  - a passed level that was missing from the completed list.
- **The manager always exists.** `PlayerProgressManager` creates itself before the first scene loads, so a level opened straight from the editor saves too. Scene copies remove themselves.
- **A finished level is never lost.** `LevelCompleteUI` registers the result (`SetPendingResult`) as soon as the card shows.
  - If the app is paused (sent to the background on mobile) or closed before Continue, the result is saved then.
  - Each result is applied once (`LevelTotalResult.RunId`).
- **Removed farming.** The old no-score path added a level's EXP again on every replay. It now keeps the best result like a scored level.
- `PlayerProgressManager.ProgressChanged` fires after every save, so menus can refresh.

### Cases from JSON (`Data/CaseFieldRules.cs`, `ComplianceChecker`, `CaseVerdictEvaluator`)
- **What the Auditor grades comes from the case JSON.** The new `requiredFields` list gives each field's `key`, `hint` and `mistakeCost`, in report order. With no list, the Level 1 three apply.
- **`auditorApprovedLines`** gives the approval dialogue (built-in lines if empty).
- **`CaseFieldRules` is the one place that maps a key to the player's value and the answer key.** Supported keys: `residencyStatus`, `taxpayerType`, `incomeSource`, `numberOfEmployers`, `businessRegistration`, `taxOption`, `requiredForm`.
- **The case won't load with a bad grading setup.** `JsonCaseLoader` refuses a case whose graded field is unknown, listed twice, or whose answer is not a valid value. Before, such a case could never be passed.
- The Auditor's "same answers as last time" check uses the graded fields, not three hardcoded ones.
- `case_001.json` now lists its three fields with their hints and costs; the old `mistakeCosts` block moved into them. `CaseDefinition.MistakeCostFor` still reads `mistakeCosts` for older cases.
- `CaseData` no longer has Marie's case built into its defaults. The defaults are empty, and everything is filled from JSON by `CaseFactory`, which is now null-safe.

### Finite state machines
- **`Core/EnumStateMachine<T>`** is a small reusable FSM for backend phases:
  - an allowed-transition table, where refused steps are logged and ignored;
  - a `Changed` event;
  - changes requested inside a listener are queued and run in order.
- **`CaseProgressionManager.Phase`** (`LevelFlowPhase`): NotStarted, CaseActive, AwaitingNextCase, LevelComplete and LoadFailed. It replaces the `levelStarted` / `waitingForNextCase` flags.
  - Finishing a case twice, advancing with nothing waiting, or finishing after the level is complete is refused.
  - With no `CaseCompletionUI`, the next case now starts instead of the level getting stuck.
- **`GameStateMachine` fixes:**
  - A state change asked for during a transition (`CaseCompleteState` → `LevelCompleteState`) is queued until the current one finishes. Before, listeners heard "Level Complete" and then "Case Complete".
  - `Start` no longer overwrites a state the level already set; Start order between objects is not fixed.
  - A throwing state or listener can't wedge the machine.
- **`OfficeDoorGate`** uses the FSM too: Waiting, Closed, Open.

## Tuning without code
| What | Where |
|---|---|
| EXP / hearts per source, deductions, pass line, badge, door gate | `Resources/Levels/<id>/rewards.json` |
| What a case grades, hints, real-world costs, Auditor lines | `Resources/Levels/<id>/cases/<case>.json` (`requiredFields`, `auditorApprovedLines`, `auditorRetryLines`) |
| On-time bonus, late penalty, clock | `Resources/Levels/<id>/office_hours.json` |

## Checks
- Scratch compile: 0 errors.
- GuideRegression: 711 PASS. 26 of those are new backend checks: score cap, rules clamping, JSON case rewards, FSM order, atomic save and backup, signature, save repair, and JSON-graded fields.
- ProgressionRegression, DialogueRegression and ControlsRegression: PASS.
- Play Mode: NOT RUN (the user tests in Unity).
