# Phase 1 — HUD foundation + progression card

Status: **Complete — card size user-verified in Play Mode (2026-10-15)**

## Goal

Replace the top-left "Level 1" text and the visible debug state label with a compact progression card that shows real level progress. Build the shared theme and runtime host that later phases reuse.

## Scope

- `HudTheme` (new): palette, type sizes, procedurally generated rounded-panel / circle / heart sprites (cached once), and UI factory helpers.
- `OfficeHudRuntime` (new): self-bootstraps in Office_Level1 only. It creates a safe-area root under `HUD_Canvas` and hides the legacy `PermanentHUD/Level` and `DebugStateLabel_Text` objects (deactivated, not deleted). It owns HUD visibility and fades the HUD out during dialogue, settings, case results, FSM pause and non-first-person camera modes.
- `HudProgressionPanel` (new): the top-left card.
  - Level badge (the level number in a brass ring), `LEVEL 1` heading and the level name from `level.json` (`Tutorial Level`).
  - EXP bar: level EXP earned out of the maximum available (`cases × 100`), with an ease-out fill animation and a short bright "gain" segment.
  - Reputation bar (revision 2): a heart icon, a coral bar and `4 / 5`, filling to level reputation out of the maximum (`cases × 5`). It animates just after the EXP bar, and the row icon pops as the value climbs.
  - Career line, `Career 340 EXP` (top-right of the card). It is shown only when `PlayerProgressManager` exists, so a run started directly from the scene hides it instead of showing 0.
- Minimal read-only integration:
  - `CaseProgressionManager.OnCaseRewardStored` (static event, raised after a reward is stored) and `CurrentLevelName`.
  - `RewardsCalculator.MaxExp` / `MaxReputation` changed from private to public constants (values unchanged).

## Revision 2 (user feedback, 2026-10-15)

The card was enlarged from 372×134 to 460×172 reference units: a 68 px badge, type at 26/21/18, and 18 px bars. The heart row became a reputation progress bar. Revision 3 then scaled the whole card ×1.15 (on screen 529×198). Even at 4:3 it ends about 180 units before the centred timer.

## Out of scope

Objective tracker and timer (Phase 2), toasts and fly-to FX (Phase 3), prompts and icons (Phase 4), result screens (Phase 5).

## Acceptance

- [x] Compiles with 0 new errors and warnings; the regression suites pass; 137 HUD logic assertions pass (agent, 2026-10-15).
- [ ] In Play Mode the top-left card appears with `LEVEL 1 · Tutorial Level`, an empty EXP bar `0 / 100` and an empty reputation bar `0 / 5` (user).
- [ ] The old "Level 1" text and the "State: …" debug label are gone (user).
- [ ] The card fades out during NPC dialogue, the settings panel, desk/workstation (non-first-person) views and the case-result popup, then fades back in (user).
- [ ] After finishing the case, the bar animates to the earned EXP (e.g. `85 / 100`) and the reputation bar fills to the earned reputation; the values match the result popup (user).
- [ ] No overlap with the minimap or right icon column, and nothing clipped at 16:9, 19.5:9 (phone) or 4:3 (user).
- [ ] No new Console errors or warnings (user).

## Manual check (Unity)

1. Open `Office_Level1` and enter Play Mode, either directly or from the main menu (the main menu also shows the Career line).
2. Look at the top-left: you should see the card and no debug text.
3. Talk to the receptionist and confirm the card fades out, then returns when the dialogue ends.
4. Sit at the desk or computer and confirm the card hides. Exit and confirm it comes back.
5. Complete the case. When the result popup closes, check that the EXP and reputation bars animate to the popup's values.
6. In the Game view, switch through 1920×1080, 2400×1080 and 1024×768.
7. Check the Console for `[OfficeHud]` errors.
