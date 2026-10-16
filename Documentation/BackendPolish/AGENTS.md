# Notes for the next agent

- **Rules of this pass:** no scene, visual, animation or game-flow changes. Only robustness, JSON configuration and state machines.
- **Rewards:** never add EXP or hearts to `LevelScoreLedger.TotalExp` directly. Use its `Add*` methods; they enforce the 0..max cap.
- **New reward numbers** go in `LevelRewardRules`, with a clamp in `Sanitize()` and a line in that level's `rewards.json` `_readme`.
- **Progress:**
  - Write saves only through `PlayerProgressManager.Save()` (atomic, signed).
  - New saved fields go in `PlayerProgressData`, with repair rules in `Normalize()`.
  - Bump `CurrentSaveVersion` only when older data needs migrating.
- **Cases:**
  - Grading belongs in the case JSON (`requiredFields`).
  - A new gradeable field needs `CaseFieldRules` (`EnumType`, `PlayerValue`, `AnswerValue`), plus the field in `CaseData`, `ResetForRetry` and `AnswerKeyDefinition`.
- **Flows:**
  - Use `EnumStateMachine<T>` for backend phases, declaring the allowed transitions.
  - Gameplay states stay `IGameState` on `GameStateMachine`. Its `ChangeState` may be deferred if called during a transition, so don't read `CurrentState` right after calling it from a state's `Enter`.
- **Checks:**
  - Compile: `python Tests/make_scratch_build.py`, then `dotnet build Temp/ScratchBuild/Scratch.csproj`.
  - Tests: `dotnet run --project Tests/GuideRegression` (backend block at the end), plus `ProgressionRegression`, `DialogueRegression` and `ControlsRegression`.
