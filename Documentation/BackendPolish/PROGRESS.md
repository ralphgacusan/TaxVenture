# Backend polish: progress

- [x] Score cap: totals stay 0..max; bonus fills only the room left; talk hearts stop at the configured count.
- [x] rewards.json validated and clamped with one warning; cached per level; case rewards (incl. new `wrongVerdictExp`) from JSON.
- [x] Saves: atomic write + backup, signed envelope, rejected file kept, legacy upgrade, repair on load, version 3.
- [x] PlayerProgressManager self-creates; finished level saved on pause/quit; each result applied once; replay farming removed.
- [x] Case grading from case JSON (`requiredFields`, `auditorApprovedLines`); loader rejects bad grading setups; CaseData defaults emptied.
- [x] EnumStateMachine; level flow FSM in CaseProgressionManager; GameStateMachine queued transitions + Start guard; OfficeDoorGate FSM.
- [x] Compile 0 errors; GuideRegression 711, Progression, Dialogue, Controls PASS.
- [ ] Play Mode check by the user: finish Level 1 on time with and without mistakes (score never above max), close the app on the Level Complete card and reopen (level saved), replay (career EXP only grows by improvements).
