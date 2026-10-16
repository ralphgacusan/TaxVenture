# Level rewards, passing and unlocks

Added 2026-10-16 with the Phase 3 polish. All numbers live in **`Assets/Resources/Levels/Level_01/rewards.json`**. Edit them and press Play; no code change is needed. Fields starting with `_` are notes and are ignored. If the file is missing or broken, the built-in defaults below are used and a warning is logged.

## Level 1 rules (defaults)

| Source | Max | How it is earned / lost |
|---|---|---|
| Plaza client conversations | 4 hearts | +1 heart per client, once each, when the talk ends and the profile drops. The heart floats from that NPC into the hearts bar. |
| Side Quest 1 (corkboard) | 50 EXP, 3 hearts, badge **Know Your Taxpayer** | −5 EXP per wrong placement. Hearts are 3 / 2 / 1 / 0 for 0 / 1–2 / 3–4 / 5+ wrong placements. The guide hands it over after the completion talk; the badge is always given. |
| Tax case (per case) | 100 EXP, 5 hearts | Each failed audit costs −15 EXP and −1 heart per mistake the Auditor finds, plus −10 EXP and −1 heart for the extra submission. Floors at 0. Taken when the Auditor approves the case. |
| **Level 1 total** | **150 EXP, 12 hearts** | |

**Passing:** both EXP and hearts must reach `passingPercent` (80%: at least 120 EXP and 10 hearts). Set it to 100 to require a perfect run.

**Below passing:** the level still finishes and the result is saved, but the next level stays locked. The Level Complete screen says "Below passing… Replay this level".

**Level order:** with `requireSideQuestBeforeOffice: true`, the receptionist, the office desk and the Case Folder (3D and HUD icon) stay locked until Side Quest 1 is finished. Trying them shows "Locked — Finish Side Quest 1 first". The office entrance doors (`officeDoors`, default `Office_Entrance_Door_1` and `_2`) are physically shut: `OfficeDoorGate` swings them closed toward each other and puts an invisible wall in the doorway, and walking up to it shows "The office is closed. Finish Side Quest 1 first". When the side quest completes, the doors swing back to their scene pose and the wall is removed. The closed pose is computed at run time; the scene is not changed.

## Save (PlayerProgressData, save version 2)

- `completedLevelIds`: finished at any score. `passedLevelIds`: finished at or above passing. **Only passed levels unlock the next one.**
- `levelRecords`: per level, the best EXP and hearts, the maximums, passed, attempts and badges.
- Career `totalExp` / `totalReputation` grow only by an improvement on the level's best, so replays cannot farm EXP.
- Older saves (version 1) are migrated: every completed level counts as passed, so nobody loses an unlock.

## Code map

| Piece | File |
|---|---|
| Rules loader + defaults | `Assets/Scripts/RewardsSystem/LevelRewardRules.cs` |
| Pure maths (side quest, case, max, percent, pass) | `Assets/Scripts/RewardsSystem/LevelScoring.cs` |
| Running level score by source, badges, `EntryAdded` | `Assets/Scripts/RewardsSystem/LevelScoreLedger.cs` (owned by `CaseProgressionManager.Score`) |
| Side-quest-first gate | `Assets/Scripts/RewardsSystem/LevelGate.cs` |
| Hooks | `TaxpayerNpcInteractable` (talk heart), `CorkboardSideQuestManager` (wrong placements), `TutorialNpcController` (side-quest award), `AuditorInteractable` → `CaseProgressionManager.RecordAuditAttempt`, `CaseProgressionManager.CalculateAndStoreCaseReward` (case award) |
| Save | `PlayerProgressData.RecordLevelResult`, `PlayerProgressManager.ApplyLevelResult` |
| Screens | `LevelCompleteUI` (score / max, %, PASSED or below passing, badge), `SideQuestCompleteUI` (score / max, badge, wrong placements), `CaseResultPopupUI` (score / case max) |
| HUD | `HudProgressionPanel` (level score bars, passing tick, badge medallions), `HudFeedback` + `HudToastQueue` (new notices) |

## Collectible badges (tiers)

Finishing a side quest always earns its badge; the tier depends on how well it went:

| Tier | Rule (Level 1 numbers) |
|---|---|
| **Bronze** | Finished (3 or more wrong placements) |
| **Silver** | Side-quest EXP at or above `passingPercent` (1–2 wrong placements: 45 or 40 of 50 EXP) |
| **Gold** | Perfect: no wrong placements |

- Replays can upgrade a badge and never downgrade it (`PlayerProgressData.badgeRecords`, best tier per badge).
- **Design:** a bevelled hexagon medal (bronze / silver / gold metal) with a two-tailed ribbon and the side quest's own emblem in the centre. A not-yet-earned badge shows as a dark silhouette. Drawn in code by `HudBadgeArt` (no image files).
- **Per side quest** in `rewards.json` → `sideQuest`: `badgeId`, `badgeName`, `badgeEmblem` (`taxpayer_id` = ID card under a magnifying glass, `receipt`, `star`), `badgeRibbon` (`#RRGGBB`). Side Quest 1 "Know Your Taxpayer": `taxpayer_id`, blue ribbon `#2F8FD8`. Give each new side quest its own emblem and ribbon; new emblems are added in `HudBadgeArt.EmblemShape`.
- **Where it shows:** the progression card (top right of the card: silhouette, then the earned medal with a pulse), the "Gold badge earned" notice (with the medal), the Side Quest Complete panel (the medal pops in with a "GOLD BADGE" label), and the Level Complete text ("Know Your Taxpayer (Gold)").
