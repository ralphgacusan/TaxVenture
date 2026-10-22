# T03 — Expose New Game, Continue and explicit run modes

Status: Planned -> Implemented, internally verified; user manual pending (2026-10-22). Blocked by: [T02 — fresh campaign and save](T02-fresh-campaign-and-save.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and before/after flow

Give the player a clear choice between starting a fresh story and returning to the saved one, and make each run's state boundary explicit. Before this ticket, MenuEntry.Play routes a first play through the opening and later play toward the career board, with no explicit New Game or Continue contract. After it, the main menu presents New Game, Continue with a chapter/task subtitle, Career, Practice, Shop, and Settings. Continue is unavailable on a fresh install; Shop remains locked until introduced. Career may display future chapters, but cannot launch unsupported new-campaign chapters.

New Game asks for confirmation before replacing the active campaign. Confirming atomically writes a fresh campaign with XP and coins at zero, no badges, claims, inventory or upgrades, Public Trust at 40/100, and Level 1 available; settings remain intact. For a supported pre-v5 save, this explicit action replaces the primary save and ordinary backup through the existing reset contract. Old progress does not enter the new campaign. Cancel or a failed write changes nothing. There is one active campaign, never multiple slots. Continue is unavailable on a fresh install or when only a pre-v5 save exists; after a committed New Game it loads the saved checkpoint or next task. After Level 1 completion it opens Career/the next supported task and must not replay the opening. Until T18–T21 are delivered together, new-campaign Level 2 is clearly marked pending integration and cannot launch through old score-heart unlocks.

The active run mode is one of story, mastery replay, practice, or temporary direct-Editor sandbox. The story route may write story progression, trust, claims, currency, and ownership. Mastery replay can improve only eligible best mastery; Practice retains lesson progress; direct Editor play is explicitly temporary. The menu and any replay confirmation state the selected mode and what may change. Temporary supplies and checkpoints never replace or spend campaign state.

User menu revision (2026-10-22, supersedes the menu labels above): no started story (no committed v5 campaign; older development, newer-version and unreadable saves included) shows one PLAY note and Settings, Tutorials hidden; PLAY commits a fresh campaign and goes straight to the unskippable opening. A started story shows NEW GAME and CONTINUE side by side in PLAY's row, then Tutorials and Settings. CONTINUE opens the saved-progress module (career board) and never launches a level. NEW GAME uses the Settings board's confirmation card ("Start a New Game?"), then PLAY's route. The main menu has no Career, Shop or subtitle rows; the Shop entry lives in the progress module (top edge beside BACK) and stays locked until the shop is introduced.

## Ownership and mechanics

The inspected route is Assets/Scripts/UI/MenuEntry.cs; menu presentation is in MainMenuUI.cs and the 3D career board presenters/builders under Assets/Scripts/UI/Menu/. PlayerProgressManager owns load/reset/write and SceneFade owns scene transition. Implement menu actions as thin commands to those owners. Do not make a presenter mutate progression in Update, on display, or after an animation.

Before leaving a running story, persist the latest stable checkpoint and wait for save success. On failure, remain in the current task, show a retryable message, and do not load another scene or erase the in-memory state. New Game's confirmed replacement is one candidate write: only publish the clean campaign after it commits. A declined confirmation or write failure leaves the prior save usable. Settings remain intact. A v5 campaign's intro-seen flag and existing first-view/skip policy survive Continue; New Game follows the existing opening policy after the fresh record commits.

Use the existing 3D paper/menu visual language. Add clearly labeled New Game and Continue rows with readable selected, disabled, and confirmation states; show the next task in Continue's subtitle. When a supported pre-v5 save is detected, explain that the updated story must be started and keep Continue disabled. Use existing select/cancel SFX and menu motion if available. No new character art or cinematic replacement is required. Disabled Career/Shop entries explain availability in plain text and remain legible in color-independent contrast.

## Non-goals

Do not redesign the career board, implement any Level 2 content, change chapter completion criteria, add save slots, alter settings, or let Practice/mastery replay spend story supplies or change story progression. Do not carry old mastery, trust, wallet, inventory, or unlocks into a fresh campaign.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Fresh install and supported pre-v5 save offer New Game with Continue disabled; v5 resume offers the correct task subtitle. Internal evidence 2026-10-22 (after the user menu revision): CampaignMenu.Build per save state in CampaignFoundationRegression - only a committed campaign shows NEW GAME / CONTINUE, all other states a single PLAY; per the revision CONTINUE opens the progress module, so it carries no task subtitle.
- [ ] Confirmed New Game replaces the primary save and ordinary backup with the fresh campaign; cancel and failed write preserve the prior save. Internal evidence: T02 commit/restore suite still passes; MenuEntry.StartNewGame loads the opening only after StartNewCampaign succeeded (source contract); cancel closes the card without any save call (panel has no save writes).
- [ ] Returning from a running story saves before routing; save failure holds the player in the current run. Internal evidence: InGameSettings.LeaveLevel calls SaveBeforeLeaving (story runs: flush pending result + Save) and returns false before any scene load (source contract). L1 has no field checkpoint until T04.
- [ ] Level 1 completion routes to Career/next supported task without replaying the opening. Internal evidence: story-mode ApplyLevelResult marks the campaign chapter complete; model then targets Career; ConsumeOpenLevelSelect requires the completed chapter.
- [ ] New-campaign Level 2 remains pending until Delivery B; old pass IDs cannot unlock its launch. Internal evidence: chapters.json Level_02 campaignPending; CareerProgress shows PENDING UPDATE and never launchable for a campaign, even after a passed Level 1.
- [ ] Practice, mastery replay, story, and direct-Editor modes have explicit boundaries; temporary runs cannot overwrite campaign checkpoints or spend live supplies. Internal evidence: CampaignRun (only Story writes story/checkpoint), replay cannot SaveCheckpoint or clear the story checkpoint, practice unchanged; no supplies exist yet, later commands must check CampaignRun.AllowsStoryWrites.
- [ ] Practice, Shop, Settings, and Career routes keep their current owners and do not mutate campaign state merely by opening. Internal evidence: CONTINUE only moves the camera to the progress module; the locked SHOP entry beside BACK only shows a notice; Practice still opens TrainingBinder3D; Settings reset uses the same New Game confirmation and route.

Implementation record: Assets/Scripts/RewardsSystem/CampaignRun.cs (run modes), CampaignMenu.cs (menu model), CareerProgress.cs (campaignPending, PendingIntegration, StoryLaunch, LaunchMode); Assets/Scripts/UI/MenuEntry.cs (StartNewGame, Continue, OpenCareer); Assets/Scripts/UI/Menu/MainMenuCampaignPanel.cs (runtime rows + confirmation under the Campaign Foundation root); MainMenu.cs, CareerBoardPresenter.cs, CareerChapterSheet.cs, SettingsBoard3D.cs, InGameSettings.cs, PlayerProgressManager.cs; Assets/Resources/Career/chapters.json. Details in ../PROGRESS.md.

USER manual acceptance — only the user performs these checks:

- [ ] Compare fresh launch, Continue, and confirmed/cancelled New Game in the main menu.
- [ ] Interrupt the story, return to the menu, and Continue; confirm the saved chapter/task resumes.
- [ ] Verify unavailable entries explain their state and cannot be opened.

## Handoff

Record menu routes and reset boundaries in ../PROGRESS.md. T04 now builds a resumable Level 1 field run on the v5 campaign. The future world interactions remain owned by their gameplay systems.
