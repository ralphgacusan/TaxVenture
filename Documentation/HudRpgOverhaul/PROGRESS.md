# Progress

Last updated: 2026-10-16

## Current stop point

**All five phases are implemented and internally verified. Phase 5 (reward screens + final layout pass) is awaiting your Play Mode check**, along with the still-open checks for Phase 4, the tiered badges and prompt fix, the reward system / Phase 3 polish, and Phase 2 rev 9.

| Phase | Status |
|---|---|
| 1 Foundation + progression card | Complete (user-verified) |
| 2 Objective tracker | Rev 9 implemented; internal checks PASS; Play Mode NOT RUN (revs 1-8 user-checked, with feedback) |
| 3 Notifications + fly-to FX | Implemented + polished, with the level reward system; internal checks PASS; Play Mode NOT RUN |
| 4 Prompts + icon badges | Implemented; internal checks PASS; Play Mode NOT RUN |
| 5 Reward screens + final layout | Implemented; internal checks PASS; Play Mode NOT RUN |

## Phase 1 — files

New (runtime-built; no scene or prefab edits):
- `Assets/Scripts/UI/HUD/HudTheme.cs` (+ .meta): palette, type scale, procedural rounded/circle/heart sprites, factory helpers and easing.
- `Assets/Scripts/UI/HUD/OfficeHudRuntime.cs` (+ .meta): bootstraps itself for Office_Level1. It creates a safe-area root after `PermanentHUD`, hides the legacy `PermanentHUD/Level` and `DebugStateLabel_Text` (and restores them on destroy), and fades the HUD out during dialogue, settings, case results, pause and non-first-person modes.
- `Assets/Scripts/UI/HUD/HudProgressionPanel.cs` (+ .meta): the top-left card (badge, level name, EXP bar and reputation bar with ghost-gain fill, career line). Revision 2 enlarged it to 460×172 and replaced the hearts with a reputation bar.

Modified (presentation hooks only, no logic change):
- `Assets/Scripts/Data/CaseProgressionManager.cs`: `CurrentLevelName` getter; static `OnCaseRewardStored` event raised right after the existing `AddCaseResult`.
- `Assets/Scripts/Data/RewardsCalculator.cs`: `MaxExp` and `MaxReputation` changed from private to public `const` (values 100 and 5 unchanged).

## Phase 1 — internal verification (agent)

- Scratch compile (`Temp/HudOverhaul`, all 278 runtime scripts against the Unity 6000.4.5f1 + Library/ScriptAssemblies references): **0 errors / 23 warnings**, the same as the pre-change baseline. No warnings come from the new files.
- Regression suites: ControlsRegression **5,734 PASS**, DialogueRegression **796 PASS**, ProgressionRegression **PASS**.
- HUD logic check (`Temp/HudOverhaul/HudCheck`, run against the compiled assembly): **137 assertions PASS**. It covers level-id parsing, bar fractions and clamping, unchanged reward maximums, every reward tier fitting the bar and heart range, accumulator totals, easing endpoints and monotonicity, and heart-fill mapping (including >10 points).
- Layout preview, rendered from the same constants at 1920×1080: no overlap with the timer, settings, minimap, icon column or joystick.
- Scene references: the hidden `Level` / `DebugStateLabel_Text` objects are referenced only by their own components, so hiding them breaks nothing.
- Revision 2 (enlarged card, reputation bar): scratch compile 0 errors / 23 baseline warnings; HUD logic check PASS (reputation-bar fraction checks replace the heart mapping); previews at 16:9, 4:3 and phone width show no overlaps. Regression suites not re-run, because no gameplay code changed since they passed.
- Revision 3 (card scaled ×1.15 → 529×198 on screen): scratch compile 0 errors / 23 baseline warnings; geometry still clears the timer at 4:3 by about 180 units.
- Play Mode / device: **NOT RUN** (user-owned).

## Phase 2 — summary

See `phases/PHASE-02-OBJECTIVE-TRACKER.md` for the file list and behaviour. The task list moved to `Assets/Resources/Tasks/task_list.json`, with the original content and a built-in fallback.

## Phase 2 — internal verification (agent)

- Scratch compile: **0 errors / 23 warnings** (baseline). The JsonUtility data classes use a scoped `#pragma warning disable 0649`.
- Regression suites re-run, because gameplay-side files changed: Controls **5,734 PASS**, Dialogue **796 PASS**, Progression **PASS**.
- HUD logic check: **205 assertions PASS**. It covers:
  - the real Level 1 state path (Idle → … → AuditSubmitted → CaseComplete) giving the expected active step at every point, a skipped Compute and an honest final `8 / 11`;
  - the skipped-interview path, exactly one active row, and null-safety;
  - state-name resolution (unknown and non-IGameState names rejected);
  - the JSON file matching the built-in list row for row (state, text, header);
  - clock formatting (including 3-digit hours);
  - the tracker clearing the card at widths 1662, 1440 and 1300.
- Layout preview at 1920×1080 (tracker ×1.15) and 1024×768 (×1.13): no overlaps. The preview caught a caption/chip collision and too-similar skipped/pending segments; both were fixed.
- New .meta GUIDs are unique; line endings are CRLF.
- Play Mode / device: **NOT RUN** (user-owned).

## Phase 2 revision 2 — internal verification (agent)

- Scratch compile: **0 errors / 23 warnings** (baseline).
- Regression suites re-run (side-quest and Notes code changed): Controls **5,734 PASS**, Dialogue **796 PASS**, Progression **PASS**.
- HUD logic check: **219 assertions PASS**. It covers:
  - the full Level 1 objective flow simulated trigger by trigger (current step, counts, main-objective states);
  - out-of-order triggers and null-safety;
  - trigger validation and normalisation;
  - `objectives.json` matching the built-in copy step for step, with every trigger valid;
  - the area footprint test;
  - the legacy list unchanged, and other levels falling back to it.
- Notes paper mock rendered from the same layout rules: the text sits on the ruled lines and the full Level 1 list fits (560 of 680 px).
- Checked every other user of the profile-viewer lock: Notes and the profile viewer cannot both hold it, and Notes releases only its own.
- New .meta GUIDs are unique; line endings preserved per file; the interim `Assets/Resources/Tasks` folder was removed.
- Play Mode / device: **NOT RUN** (user-owned).

## Phase 2 revision 3 — bug fixes (agent)

- **Objectives not updating:** `OfficeObjectives` disabled itself when idle, which unsubscribed every event (agent bug). It now never toggles `enabled` and seeds from public game state on start.
- **Boxes for `/ ( ) -` (and 3/8):** the Minnie SDF HUD font lacks those glyphs. New `HudFontSet` (`Assets/Resources/HUD/HudFonts.asset`): LiberationSans for the HUD, Gloria Hallelujah for the paper.
- Compile 0 errors / 23 baseline warnings; HUD check **611 PASS** (new guards for both bugs). Play Mode NOT RUN.

## Phase 2 revision 4 (agent)

- **Notes icon:** single tap handler at runtime (double-toggle fix).
- **Fades:** Settings and the icon column fade in dialogue and results.
- **"Go to the office":** now triggers on the reception floor/doors.
- **Case folder:** stored in the HUD icon → finish-case step checked. The icon can't be dragged out at count 0. A failed audit (folder returned) unchecks the step via `undoneBy`.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **661 PASS**; Controls 5,734 / Dialogue 796 / Progression PASS. Play Mode NOT RUN.

## Phase 2 revision 5 (agent)

- **HUD stays visible:** the HUD and minimap stay up while Notes or the profiles hold the player.
- **New `HudProfileCards`:** profiles are paper `FloatingWindow` cards in Office_Level1.
- **New `HudWindowLock`:** one shared hold for all paper windows.
- **Paper motion:** opt-in `FloatingWindow.UsePaperMotion` animates Notes, the cards and the Case Folder.
- **Paper depth:** Notes and the cards are drawn as stacked sheets.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **672 PASS**; Controls / Dialogue / Progression PASS. Play Mode NOT RUN.

## Phase 2 revision 6 (agent)

- **Profiles open alongside Notes:** the scene icon calls the 3D viewer directly, so the viewer now defers to the paper cards.
- **New `HudWindowFocus`:** pressing anywhere on a window brings it to the front.
- **Fly to/from icons:** paper windows fly out of and back into their HUD icons.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **679 PASS**; Controls / Dialogue / Progression PASS. Play Mode NOT RUN.

## Phase 2 revision 7 (agent)

- **New `ProfileCardSkin`:** world papers and corkboard cards use the card style.
- **New `CaseFolderUI` window mode:** fixes the icon not opening the folder outside the desk.
- **New drag in/out:**
  - `HudDragItem`: the closed folder, Notes paper and profile stack under the finger.
  - `HudIconDragOpen`: drag-out on the Notes and profile icons.
  - Overrides on `HudSubmittableIcon` for the Case Folder.
  - The profile long-press gather/stack in `HudProfileCards`.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **702 PASS**; Controls / Dialogue / Progression PASS. Play Mode NOT RUN.

## Phase 2 revision 8 (agent)

- **Paper depth fixed:** 8× too thick (agent bug); now two pages.
- **World cards:** 1.6× text and frame feedback.
- **Profile stack:** follows the live pointer.
- **Desk documents:** come forward by tucking the floating papers.
- **Sorting reset:** no longer drops windows below others.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **715 PASS**; Controls / Dialogue / Progression PASS. Play Mode NOT RUN.

## Phase 2 revision 9 (agent)

- **New `Doc3DLayer`:** 3D documents join the window order through transparent per-document copies.
- **`HudWindowFocus`:** touch resolution with pass-through; coming to the front pops.
- **Event-only profile hold**, with Console logs.
- **`ProfileCardSkin`:** landscape, compact, readable cards with a result frame.
- **New `CaseFolder3DView`:** the real desk folder from the icon.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **730 PASS**; Controls / Dialogue / Progression PASS. Play Mode NOT RUN.

## Phase 3 (agent, 2026-10-16)

- **New `HudToastQueue`, `HudRewardFlyFX`, `HudFeedback` and `HudTheme.Star`;** `HudProgressionPanel.HoldFillFor`.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **746 PASS**; Controls / Dialogue / Progression PASS. Play Mode NOT RUN.

## Phase 3 polish + level reward system (agent, 2026-10-16)

- **New:** `Resources/Levels/Level_01/rewards.json`, `LevelRewardRules`, `LevelScoring`, `LevelScoreLedger`, `LevelGate`, `Documentation/HudRpgOverhaul/REWARDS.md`.
- **Gameplay hooks:** `CaseProgressionManager` (`Score`, `RecordAuditAttempt`, `AwardConversation`, `OnAuditFailed`; the case reward now comes from the audits), `AuditorInteractable`, `TaxpayerNpcInteractable`, `CorkboardSideQuestManager` (wrong placements; `BuildResult(entry)`), `TutorialNpcController` (awards the side quest), receptionist / desk / Case Folder / `CaseFolder3DView` (gate).
- **Save:** `PlayerProgressData` version 2 (`passedLevelIds`, `levelRecords`, `RecordLevelResult`; unlock needs a pass; v1 migrated as passed). `PlayerProgressManager.ApplyLevelResult` saves the best result.
- **Screens:** `LevelTotalResult.FromLedger`; `LevelCompleteUI`, `SideQuestCompleteUI`, `CaseResultPopupUI` show score / max, %, PASSED or below passing, badge.
- **HUD:** level-score bars (150 EXP / 12 hearts for Level 1) with a passing tick that turns green, badge medallions on the card; new toasts (conversation heart, side-quest reward + badge, deductions, failed audit, locked, passing reached) with an accent edge and settle pop; hearts fly from the NPC / guide.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **796 PASS** (new: rules, scoring tiers, pass line, ledger, gate, rewards.json, hooks); ProgressionRegression PASS with new pass-to-unlock and best-record cases; Controls 5,734 PASS; Dialogue 796 PASS. Play Mode NOT RUN.

## Phase 4 (agent, 2026-10-16)

- **New:** `HudPrompt` (game-style prompt pill), `HudIconBadge` (corner count badges), `HudTheme.Tap / Mouse / Lock`.
- **Modified:** `InteractionPromptUI` (`ShowOverride` / `HideOverride`; same fields and API), `OfficeHudRuntime` (installs both).
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **853 PASS** (all 26 prompt strings mapped); Controls / Dialogue / Progression PASS. Play Mode NOT RUN.

## Tiered badges + prompt fix (agent, 2026-10-16)

- **Badges:** `BadgeTier` (Bronze finished / Silver side-quest EXP at passing / Gold perfect) in `LevelScoring`, `LevelScoreLedger`, `LevelTotalResult`; best tier saved per badge (`PlayerProgressData.RecordBadge`). New `HudBadgeArt` draws the hexagon medal with the side quest's emblem and ribbon (`rewards.json`: `badgeEmblem`, `badgeRibbon`). Shown on the card (silhouette until earned), the badge notice, the Side Quest Complete panel (pop-in) and the Level Complete text.
- **Prompt fix:** `Interactor` keeps the looked-at object's prompt while walking or with the pointer on the HUD; the interact tap is still blocked there.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **873 PASS** (tiers, ledger, medal art pixels, silhouette, emblems, wiring, Interactor rule); ProgressionRegression PASS (badge tiers: collect, upgrade, never downgrade, round trip); Controls / Dialogue PASS. Medal sheet rendered and checked. `VerifyInteractionOutlines.py` fails on an unrelated committed renderer value (K27). Play Mode NOT RUN.

## Phase 5 (agent, 2026-10-16)

- **New:** `HudRewardScreen` (+ `RewardScreenData`): runtime reward cards for Side Quest Complete, Case Results and Level Complete.
- **Modified:** `SideQuestCompleteUI`, `CaseResultPopupUI`, `LevelCompleteUI` (build the card data from their own result; fields, buttons, callbacks and saving unchanged), `OfficeHudRuntime` (HUD steps aside while a card is open).
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **893 PASS**; Progression / Controls / Dialogue PASS; previews at 4:3, 16:9 and 21:9. Play Mode NOT RUN.

## Phase 5 revision 2 (agent, 2026-10-16)

- While a reward card is open, all other UI (joystick, minimap, icons, prompt, HUD, other canvases) is hidden and then restored exactly; the card is opaque, with a banner title, dividers, rays, glow and sparkles behind the hero, outlined chips and a pulsing Continue.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **900 PASS**; Progression / Controls / Dialogue PASS. Play Mode NOT RUN.

## Phase 5 revision 3 (agent, 2026-10-16)

- Content-sized, screen-fitted reward cards with the button inside; the player is held while open; everything settles exactly.
- Clean audit skips the extra popup; a failed audit shows a findings card with one "Review the case" button.
- Level Complete → Continue saves and opens 3D Menu directly on the level select (`MenuEntry`).
- Mobile prompt: always the tap glyph, two-line "TAP TO SIT / Desk" plate with a pulsing tap button.
- Console errors on leaving the level fixed (no re-parenting / re-activating objects that are being destroyed).
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **935 PASS**; Progression / Controls / Dialogue PASS. Play Mode NOT RUN.

## Revision 4 (agent, 2026-10-16)

- The Audit Summary always shows again: an APPROVED card that auto-continues (tap to skip), or the findings card.
- New themed icons for Settings / Notes / Case Folder / Profiles (`HudIconArt`); count badges kept.
- Tax Code Book + Case Folder pages restyled (header strip, print font, round arrows, bookmark / case tab, rounded value tags and slots, pop-in); drag ghost restyled and animated (`DragGhostSkin`); Exit Desk button styled; the debug "CurrentMode" label removed.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **962 PASS** (icons distinct and solid, documents, ghost, auto-continue, audit always shown, debug label gone); Progression / Controls / Dialogue / NPC markers PASS; `VerifyInteractionOutlines.py` still fails on the unrelated committed renderer value (K27). Play Mode NOT RUN.

## Revision 5 (agent, 2026-10-16)

- Case Folder fields that would wrap show the caption on top and the value below; one-line fields are unchanged.
- Settings moves to the top of the icon column whenever the minimap is hidden (icons keep their place), and slides back when the map shows.
- **Verification:** compile 0 errors / 23 baseline warnings; HUD check **971 PASS**; Progression / Controls / Dialogue PASS. Play Mode NOT RUN.

## Next step

User: run the open Unity checks (Phase 5 first: `phases/PHASE-05-REWARD-SCREENS.md`). Agent: fix any reported defects. Once everything is confirmed, the initiative is complete.
