# Ordered implementation tickets

All twelve tickets are **implemented and internally verified** (2026-10-19); T02 is user accepted and T03-T12 await the user's Play Mode checks (see PROGRESS). Each delivers an observable playable slice with logic, presentation, content and internal verification as appropriate. No tracker issues were published and no git commands are part of this workflow. Twelve tickets keep the level concrete without creating separate horizontal model/UI/backend projects.

| Ticket | Blocked by | Visible accomplishment |
|---|---|---|
| [01 — Office start](tickets/T01-office-start.md) | None | New Level2 scene reuses current Level1 world/office and opens with Rosa's one case and functioning HUD |
| [02 — Retrieve and inspect](tickets/T02-retrieve-and-inspect.md) | 01 | Player opens drawer, collects folder and compares prepared pages |
| [03 — Preliminary finding](tickets/T03-preliminary-finding.md) | 02 | Player links a entry, clue and book reason; the slip checks the link automatically before the initial hold |
| [04 — Rosa's stall](tickets/T04-rosa-stall.md) | 03 | Player visits a complete 3D encounter and receives her records |
| [05 — Hands-on toolkit](tickets/T05-toolkit-tutorial.md) | 04 | Physical toolkit opens and teaches one annotation example |
| [06 — Scored income challenge](tickets/T06-income-challenge.md) | 05 | Four records produce learning feedback, frozen tier, corrections and badge |
| [07 — Office reconciliation](tickets/T07-office-reconciliation.md) | 06 | Returning player corrects the summary and independently explains the mismatch |
| [08 — Final physical assessment](tickets/T08-final-stamp.md) | 07 | Reason-backed stamps press into paper and track the reviewed revision |
| [09 — Reviewer and recovery](tickets/T09-auditor-recovery.md) | 08 | Correct/incorrect approval and hold outcomes lead to appropriate recovery |
| [10 — Review cutoff](tickets/T10-review-cutoff.md) | 09 | Clock creates pressure, Auditor stays, safe rollover preserves work |
| [11 — Save and finish](tickets/T11-save-and-finale.md) | 10 | Full checkpoint/replay safety, Rosa ending, story fact and career result |
| [12 — Integration and handoff](tickets/T12-integration-handoff.md) | 11 | Whole level internally verified and presented for user Play Mode acceptance |

T01 includes the one-time scene creation and menu/build routing in SCENE_REUSE.md; later tickets modify that scene incrementally, never regenerate it.

Execute in this order by default. Do not run multiple AI sessions editing shared scripts simultaneously without a new ownership agreement. Each ticket lists narrower implementation boundaries; all pack-wide constraints still apply. If a ticket exceeds one practical session, record a smaller resumable phase inside it rather than splitting off disconnected asset-only work.
