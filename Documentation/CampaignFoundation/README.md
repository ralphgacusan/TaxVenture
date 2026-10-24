# Campaign Foundation — Menu, Level 1, then Level 2

A detailed implementation ticket pack for the approved TaxVenture campaign systems. Created 2026-10-21. **Planned; nothing in this pack is implemented or manually accepted yet.**

## Overall want

The player is Elijah, a new arrival helping Coast Tales with income-tax work. Mayor Quax replaces Arthur as guide and teaches in person. The game should reward corrected learning, earn public trust through responsible follow-through, give collected records an organized home, and let first-completion coins visibly improve Elijah's office. Later reputation-driven endings must be supportable without converting mastery hearts into social trust or creating separate competing save/UI frameworks.

**Delivery A:** existing main menu and L1 become a coherent fresh campaign with New Game/Continue, safe campaign initialization, persistent L1 progress, first-check mastery, public trust, once-only currency, bag/collectibles, physical/menu shop, bookmarks/decor and review vouchers. **Delivery B:** integrate the existing one-case Rosa L2, add its knowing-choice consequences and functional comparison stand, then enable campaign continuation. Future computer benefits unlock after L3; future cases and endings are not implemented now.

**Fresh-start decision:** existing development progress will not be imported or retained in an in-game archive. New campaigns start with 0 XP, no earned badges, 40/100 Public Trust, 0 coins, no collected items/purchased upgrades, and Level 1 available. Essential starter tools and all new earning systems remain. Settings survive; normal backups/recovery protect progress earned in the updated game. This documentation update does not reset any actual save file.

## Read order

1. [AGENTS](AGENTS.md): ownership, internal-only verification, user manual acceptance and preservation rules.
2. [SPEC](SPEC.md): approved behavior, inspected baseline, saved-state/transaction contracts, release scope and starting fixtures.
3. [WORLD_AND_FEEDBACK](WORLD_AND_FEEDBACK.md): physical assets, player gestures, motion, sound and presentation.
4. [TICKETS](TICKETS.md): 23 bounded, dependency-linked slices (T22-T23 are Delivery A amendments, 2026-10-24).
5. [VERIFICATION](VERIFICATION.md): actual internal checks and separate USER manual scripts.
6. [PROGRESS](PROGRESS.md) and [KNOWN_ISSUES](KNOWN_ISSUES.md): truth of work/evidence and blockers.
7. Selected ticket under tickets/; inspect referenced current code before implementing.

## Ticket navigation

- [Start T01](tickets/T01-mastery-boundary.md).
- [Delivery A acceptance gate](tickets/T17-delivery-a-verification.md).
- [Next: Level 2 integration](tickets/T18-level2-campaign-and-inventory.md).
- [Delivery B acceptance gate](tickets/T21-delivery-b-verification.md).

## Existing systems to extend

Save ownership: Assets/Scripts/RewardsSystem/PlayerProgressData.cs and PlayerProgressManager.cs; Assets/Scripts/Save/SaveFileStore.cs. Mastery: LevelScoreLedger, LevelScoring, LevelRewardRules, LevelTotalResult and CaseProgressionManager. Presentation: UI/MenuEntry, UI/Menu/CareerBoard*, UI/HUD/OfficeHudRuntime and physical paper viewers. L1: SideQuest1 profiles/corkboard plus UI/HUD/OfficeHours. L2: Level2Checkpointer, CaseReviewState, IncomeToolkit, RosaInteractable and Level2Audit. Inspect actual symbols/paths at execution; old ticket examples can be stale.

## Reference packs

This pack follows [Level2 ordered tickets](../Level2SomethingDoesNotMatch/TICKETS.md), [hands-on physical tutorial](../Level2SomethingDoesNotMatch/tickets/T05-toolkit-tutorial.md), [resume/finale](../Level2SomethingDoesNotMatch/tickets/T11-save-and-finale.md), and [integration handoff](../Level2SomethingDoesNotMatch/tickets/T12-integration-handoff.md). Reuse current [career board](../3DMenuCareerBoard/README.md), [practice binder](../TrainingHub/README.md), [settings actions](../SettingsActions/README.md) and [story entry](../FirstStoryFlow/README.md). These references establish style/source history, not permission to revive old 80% gates or accuracy-heart trust.

## Verification responsibility

Implementers perform ONLY internal checks. The user performs ALL Unity/manual/visual/animation/touch/device/listening checks and ALL Git actions. Each ticket has separate acceptance lists. No game testing was required merely to write this pack; run python Documentation/CampaignFoundation/verify_pack.py for documentation integrity after authoring.

