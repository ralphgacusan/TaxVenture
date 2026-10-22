# T02 — Start a fresh campaign and establish the v5 save boundary

Status: Planned -> Implemented, internally verified; user manual pending (2026-10-21). Blocked by: [T01 — mastery boundary](T01-mastery-boundary.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and visible result

Establish the schema-5 campaign record and make New Game the only way to begin the updated story. A fresh install offers New Game and keeps Continue unavailable. When the existing save owner recognizes a valid supported pre-v5 progress record, none of its data enters the new campaign or makes Continue available. The menu explains that the updated story must be started; old mastery totals, level results, checkpoints, and unlocks do not become campaign state. Until the player confirms New Game, the story remains unstarted. After the new campaign has been durably created, Continue resumes it through the ordinary checkpoint and recovery flow.

The first committed campaign starts with schema version 5, a new campaign identity, Public Trust 40/100, zero XP and coins, no badges, no claimed rewards, empty inventory and drawer, no owned upgrades, no prior chapter completion, and Level 1 available. No Level 2 launch is inherited from old pass records or checkpoints. Settings remain intact. A confirmed New Game replaces the existing primary save and ordinary backup through the current reset contract; it does not create a special copy of old progress.

## Ownership and mechanics

The save owner remains PlayerProgressManager over PlayerProgressData and SaveFileStore. Extend the existing record and commands; do not add a second manager, alternate save document, PlayerPrefs economy, or another signing key. Keep schema version 5, the existing signed envelope, signing key, SignedSinceVersion = 3 cutoff, atomic replacement, and ordinary backup recovery. Preserve the current decoder and signature policy unchanged. In particular, unsigned data claiming a signed version remains rejected, and a valid unsupported future version is not treated as an old save or silently overwritten.

On load, distinguish the states needed by the menu: no campaign, an existing valid v5 campaign, a valid supported pre-v5 save that requires New Game, and the existing invalid-signature or unsupported-future compatibility/error cases. Detection is only for choosing the right menu message and disabling Continue; it never imports or exposes old progress. Follow the save owner's existing primary/backup recovery rules. Do not add a matrix of old schema fixtures or change acceptance of any prior format.

Create a fresh campaign only through the confirmed New Game operation. Build its complete initial candidate in memory, write it through the existing signed atomic store, and publish it in CurrentProgress only after durable success. A failed encode, signature, backup recovery, reset, or write leaves the prior in-memory and durable state usable and reports a retryable failure. A successful new campaign uses normal save and backup behavior from then on. Keep settings in their current owner and preserve them across New Game. The new campaign's story position follows the existing opening/first-view policy only after the player starts New Game; merely detecting an old save must not launch the opening or create a started story.

T03 owns the New Game/Continue menu actions and confirmation presentation. This ticket supplies the save-state behavior they use: Continue is unavailable for a fresh install or supported pre-v5 save, and the latter receives a concise “Start the updated story” explanation. No extra screen, animation, or new SFX is needed. No existing user save is reset while authoring or verifying this ticket; replacement checks use disposable test data.

## Non-goals

Do not carry old progression into the campaign, expose it through a viewer, change the old decoder/signature policy, add New Game/Continue UI, alter settings, change mastery scoring, or implement trust/reward commands and Level 1 checkpoints. Do not create actual campaign save files as part of this documentation work.

## Acceptance

Internal checks — AI source/data checks only; record evidence in ../PROGRESS.md:

- [ ] Fresh install creates no campaign until New Game is confirmed; the committed initial record has the specified defaults and can be saved and continued normally. Internal evidence 2026-10-21: CampaignFoundationRegression checks Classify(no file) = NoCampaign, the CreateNewCampaign defaults (v5, id, trust 40/100, 0 XP/coins, no badges/records/claims/items/upgrades/chapters, Level 1 current, no checkpoint), JSON reload and ordinary save after commit.
- [ ] A valid supported pre-v5 save is identified only as requiring a new story; no old XP, badges, pass IDs, checkpoint, wallet, inventory, or trust enters the new campaign. Internal evidence: v2/v4 and campaign-less v5 data classify as RequiresNewStory ('Start the updated story', no Continue); Normalize keeps them version 4; the committed file equals the fresh candidate with none of the old values.
- [ ] Existing v5 campaigns still load and recover through the ordinary backup path; unsupported future versions and invalid signatures retain the existing protective behavior. Internal evidence: damaged primary recovers the campaign from .bak; a bad signature is kept as .rejected and the backup used; a v6 save classifies UnsupportedFuture, is neither normalized nor rewritten at load, and ordinary Save() refuses to overwrite it (source contract checks).
- [ ] Injected candidate-write failure publishes no partial reset and keeps the prior durable and in-memory state usable; retry commits the fresh campaign once. Internal evidence: encode failure, first-write failure and failure after the main file was replaced all leave both old generations byte-identical; retry writes the same campaign id to main and backup. Removing the restore step in a scratch copy made 2 checks fail.
- [ ] The existing signing key, signed envelope, version-3 cutoff, settings owner, and normal campaign backup behavior remain intact. Internal evidence: decoder/sign code unchanged (source checks), no PlayerPrefs.DeleteAll, SettingsRegression 133/133, all suites listed in ../PROGRESS.md pass.

Implementation record: CampaignRecord/CampaignItemStack and PlayerProgressData.CreateNewCampaign (Assets/Scripts/RewardsSystem/PlayerProgressData.cs); CampaignSaveState and CampaignSave (Classify, CanContinue, ContinueNotice, AllowsOrdinarySave, TryCommit) in Assets/Scripts/RewardsSystem/CampaignSave.cs; SaveFileStore.Capture/Restore; PlayerProgressManager.CampaignState, CanContinueCampaign, ContinueNotice and StartNewCampaign(out error). No menu UI changed; T03 calls these. Details in ../PROGRESS.md.

USER manual acceptance — only the user performs these checks:

- [ ] On a disposable pre-v5 test profile, verify the menu explains that the updated story must be started and Continue is unavailable; confirm New Game shows its replacement prompt.
- [ ] Start a fresh story, leave after a saved checkpoint, and Continue; verify the ordinary resume flow and settings retention.

## Handoff

Record the schema-5 defaults, compatibility behavior, and failed-write result in ../PROGRESS.md. T03 adds explicit New Game and Continue routes on this boundary. T02 does not import or resume a pre-v5 save.
