# Campaign Foundation — Menu, Level 1, then Level 2

User decision (2026-10-29): T20 comparison stand is deferred to future work, with its design retained in the T20 ticket. It is not required for Delivery B, T33 or T21. Continue T33 -> T21 after implemented T19. Keep ordinary free workstation comparison available; the unimplemented stand remains unavailable for purchase, including after T21 enables Level 2. T21 verifies that unavailable state rather than requiring stand purchase, placement or persistence.

A detailed implementation ticket pack for the approved TaxVenture campaign systems. Created 2026-10-21. Implementation evidence lives in [PROGRESS](PROGRESS.md). T28, T29, T17 and T18 are internally verified; user manual acceptance remains pending. **T33 and T21 implemented, internally verified; user manual pending. Level 2 campaign access enabled.** T19 is implemented; see PROGRESS for current internal/manual verification. Physical documents are implemented only as the current level needs them; T27 retains the fixed roadmap and is not a prerequisite.

## Overall want

The player is Elijah, a new arrival helping Coast Tales with income-tax work. Arthur, Senior Tax Consultant, is the guide and teaches in person (the Mayor Quax guide planned in T15 was reverted by user decision on 2026-10-25). The game should reward corrected learning, earn public trust through responsible follow-through, give collected records an organized home, and let first-completion coins visibly improve Elijah's office. Later reputation-driven endings must be supportable without converting mastery hearts into social trust or creating separate competing save/UI frameworks.

**Delivery A:** existing main menu and L1 become a coherent fresh campaign with New Game/Continue, safe campaign initialization, persistent L1 progress, first-check mastery, public trust, once-only currency, bag/collectibles, physical/menu shop, bookmarks/decor and review vouchers. **Delivery B:** carry Level 1 systems/items forward, simplify the opening/town, animate Rosa and two new optional residents, update case folders and polish dialogue/story/flow alongside ethics and the stand, then enable continuation. Future computer benefits unlock after L3; future cases and endings are not implemented now.

**Fresh-start decision:** existing development progress will not be imported or retained in an in-game archive. New campaigns start with 0 XP, no earned badges, 40/100 Public Trust, 0 coins, no collected items/purchased upgrades, and Level 1 available. Essential starter tools and all new earning systems remain. Settings survive; normal backups/recovery protect progress earned in the updated game. This documentation update does not reset any actual save file.

## Read order

1. [AGENTS](AGENTS.md): ownership, internal-only verification, user manual acceptance and preservation rules.
2. [SPEC](SPEC.md): approved behavior, inspected baseline, saved-state/transaction contracts, release scope and starting fixtures.
3. [WORLD_AND_FEEDBACK](WORLD_AND_FEEDBACK.md): physical assets, player gestures, motion, sound and presentation.
4. [TICKETS](TICKETS.md): 33 numbered plans; T27 is a deferred incremental-adoption roadmap, not a delivery dependency. T18 follows T17 directly.
5. [VERIFICATION](VERIFICATION.md): actual internal checks and separate USER manual scripts.
6. [PROGRESS](PROGRESS.md) and [KNOWN_ISSUES](KNOWN_ISSUES.md): truth of work/evidence and blockers.
7. Selected ticket under tickets/; inspect referenced current code before implementing.

## Ticket navigation

- [Current Level 2 continuation direction](LEVEL2_CONTINUATION.md).
- [T32 — quieter town and animated Rosa](tickets/T32-level2-town-and-rosa-animation.md).
- [T33 — story flow, dialogue and polish](tickets/T33-level2-story-flow-and-polish.md).

- **Current handoff:** [Delivery B Unity/device acceptance](DELIVERY_B_HANDOFF.md). T20 remains deferred and unavailable for purchase. T19 follows the implemented T32 and retains its movement owner. [T18](tickets/T18-level2-campaign-and-inventory.md) (shared items, claims and Rosa's Types 4–7) is internally verified; user manual pending. [T27](tickets/T27-eight-document-template-library.md) retains the incremental roadmap; Types 3 and 8 and unused variants wait for relevant cases. Read the [supporting document standard](../SupportingDocuments.md).
- [Start T01](tickets/T01-mastery-boundary.md).
- [Delivery A acceptance gate](tickets/T17-delivery-a-verification.md).
- [Level 2 integration and its required documents](tickets/T18-level2-campaign-and-inventory.md).
- [Delivery B acceptance gate](tickets/T21-delivery-b-verification.md).

## Existing systems to extend

Save ownership: Assets/Scripts/RewardsSystem/PlayerProgressData.cs and PlayerProgressManager.cs; Assets/Scripts/Save/SaveFileStore.cs. Mastery: LevelScoreLedger, LevelScoring, LevelRewardRules, LevelTotalResult and CaseProgressionManager. Presentation: UI/MenuEntry, UI/Menu/CareerBoard*, UI/HUD/OfficeHudRuntime and physical paper viewers. L1: SideQuest1 profiles/corkboard plus UI/HUD/OfficeHours. L2: Level2Checkpointer, CaseReviewState, IncomeToolkit, RosaInteractable and Level2Audit. Inspect actual symbols/paths at execution; old ticket examples can be stale.

## Reference packs

This pack follows [Level2 ordered tickets](../Level2SomethingDoesNotMatch/TICKETS.md), [hands-on physical tutorial](../Level2SomethingDoesNotMatch/tickets/T05-toolkit-tutorial.md), [resume/finale](../Level2SomethingDoesNotMatch/tickets/T11-save-and-finale.md), and [integration handoff](../Level2SomethingDoesNotMatch/tickets/T12-integration-handoff.md). Reuse current [career board](../3DMenuCareerBoard/README.md), [practice binder](../TrainingHub/README.md), [settings actions](../SettingsActions/README.md) and [story entry](../FirstStoryFlow/README.md). These references establish style/source history, not permission to revive old 80% gates or accuracy-heart trust.

## Verification responsibility

Implementers perform ONLY internal checks. The user performs ALL Unity/manual/visual/animation/touch/device/listening checks and ALL Git actions. Each ticket has separate acceptance lists. No game testing was required merely to write this pack; run python Documentation/CampaignFoundation/verify_pack.py for documentation integrity after authoring.

