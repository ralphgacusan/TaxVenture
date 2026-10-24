# T05 — Resume casework, review time and completion boundaries

Status: Planned -> Implemented, internally verified; user manual pending (2026-10-23). Blocked by: [T04 — Level 1 field resume](T04-level1-field-resume.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and before/after flow

A player can leave during Marie's review, reopen Continue, and find the same client documents, entered figures, review phase, deadline, and audit history. Before this ticket, the new campaign has a town/field checkpoint but does not preserve the full Level 1 case run. After it, the same v5 checkpoint also describes the active case and clock, so a relaunch cannot silently reset work or charge the player twice.

The checkpoint is a plain data snapshot. Persist stable case and document IDs, original-versus-working document identity and revision, selected fields, transferred field IDs, form inputs, review phase, required objective flags, audit submissions/findings/penalties, current day and remaining active review time, deadline/punctuality state, and any already-applied extension seconds. Never store Unity references or answer-key data in player-facing item snapshots. Keep enough case-definition/version identity to reject a checkpoint for a different case.

## Ownership and mechanics

The inspected Level 1 data seams are Assets/Scripts/Data/CaseData.cs and Assets/Scripts/Data/Definitions/CaseDefinition.cs, with CaseManager.cs owning the active case. Level2Checkpointer.cs / Level2Checkpoint is a structural example of restoring one signed-save checkpoint without replaying events, not the Level 1 case model. Inspect the actual Level 1 desk, clock, document transfer, audit, and approval owners before selecting exact adapter paths. Extend T04's existing checkpoint rather than adding a file or manager.

Save on each stable case milestone (open/retrieve, field change, transfer, form submission, audit return, day rollover, extension, and review phase change), on pause/quit and before menu exit. While an active clock runs, persist bounded dirty updates so a process loss cannot restore an unlimited timer. The save represents the game clock, not a statutory tax deadline: never alter the legal cutoff. Restore into a safe desk or spawn location, rebuild paper and HUD presentation from stable IDs, then restore clock/score state without invoking gameplay events or re-running transfers, stamps, findings, or penalties. If the checkpoint schema/case revision is unusable, keep the recoverable source record, explain the mismatch, and start at the nearest safe task phase; do not silently display different source documents.

All related state must be committed to the existing signed save. A failed write keeps the prior memory/disk snapshot and shows a retry path; leaving the scene must wait for a successful save. Appointment-extension quantity ownership is not implemented here, but the applied extension duration must be recorded so T14 can make its later consumption and clock change atomic.

Reuse current physical case files, viewers, and desk interactions. Restore papers in their stable desk/tray locations without replaying lift or stamp animation. Reuse current clock and audit cues; no new sound or animation is required for a resumed state.

## Non-goals

Do not add final scoring/results, public trust, coin rewards, new timer rules, appointment voucher purchase/consumption, bookmarks, new tax content, or generic document frameworks. Do not alter Level 2 save behavior.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Round-trip every Level 1 review phase, original/working revision, transfer, form, audit, day, cutoff, and extension field. Internal evidence 2026-10-23: CampaignFoundationRegression round-trips four case phases (before receptionist, case received, after transfers with folder stored, after an audit return with retry guard) plus the full clock (day 2, game/active time, both deadlines, missed day, 180 s extension) through SaveFileStore + signed envelope; CampaignFoundationRuntimeRegression round-trips real CaseData for Marie's case (fields by key, documents read, flags) including culture-independent numbers. Level 1 has read-only original documents only (no working copies); the revision is a definition fingerprint.
- [ ] Restore does not duplicate case transfer, audit penalties, score events, or clock extension. Internal evidence: source contracts - case restore calls no TryReceiveValue, hand-in, BeginFinalAudit, RecordAuditAttempt, Raise*, PlayEffect; counters via RestoreAuditCounters and the Auditor's RestoreRetryGuard; folder via RestoreStored (no sound/event); extension seconds restored as recorded, clamped to one extension.
- [ ] Clock resume preserves elapsed/remaining time and cannot move the legal cutoff. Internal evidence: OfficeHours.RestoreClock assigns the saved day/time/deadlines and never calls the deadline/allowance maths or replays warnings/next morning; a valid clock is unchanged by repair. The clock is the game's, not a statutory deadline.
- [ ] Wrong-case and obsolete-revision snapshots recover safely without hiding the source document. Internal evidence: ValidateCase moves another case or a changed fingerprint to setAsideCase (kept in every later save), applies nothing, and the case starts from the receptionist with a notice; the loaded case documents are always the current authored ones. Mutation (ignoring the fingerprint) failed a check.
- [ ] Write failure retains the previous durable checkpoint; menu exit retries or stays in the case. Internal evidence: shared T04 path (SaveLevelOneCheckpoint restores the previous record on failure; retry in 5 s; Settings exit waits for PrepareSettingsExit).
- [ ] Existing Level 2 checkpoint roundtrips remain unaffected. Internal evidence: Level2Regression 1326 PASS; Level2Checkpointer does not use the Level 1 records (runtime suite check); case capture/restore skip definitions with a review (Level 2).

Implementation record: Level1CaseField / Level1CaseRecord / Level1ClockRecord and Level1FieldCheckpoint.caseRecord, setAsideCase, clock (PlayerProgressData.cs); Assets/Scripts/Data/Level1CaseCheckpoint.cs (CaseData adapter, fingerprint); Level1FieldCheckpointRules.ValidateCase / Fingerprint; Level1FieldCheckpointer case/clock capture, milestone saves and RestoreCase; quiet hooks in OfficeHours, ReceptionistInteractable, FolderToHUDIcon, AuditorSubmissionTray, AuditorInteractable, TaxCodeBookInteractable, HudIconButton; Tests/CampaignFoundationRuntimeRegression (new). Details in ../PROGRESS.md.

USER manual acceptance — only the user performs these checks:

- [ ] Leave and resume during document review, after a transfer, and after an audit return.
- [ ] Resume across a day boundary and confirm the displayed deadline and remaining time match the save.
- [ ] Apply an extension only if the existing gameplay exposes a valid path, exit, and confirm it is neither lost nor applied twice.
- [ ] Confirm desk documents remain readable and correctly layered after restore.

## Handoff

Record the case fields, restore order, and clock boundary in ../PROGRESS.md. T07 consumes the corrected quest and approved case to record chapter results; T10 can build its bag/document catalogue over these stable document identities.
