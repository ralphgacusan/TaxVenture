# Phase 5 — Reward screens + final layout pass

Status: Implemented (2026-10-16), awaiting your Unity check

## Goal

Make the moments of reward look like rewards. Then verify that the whole HUD is coherent across resolutions.

## Scope

- `CaseResultPopupUI`, `SideQuestCompleteUI`, `LevelCompleteUI`: same serialized fields, buttons and callbacks. A runtime presenter adds:
  - An EXP bar filling to the earned EXP out of the maximum, a row of reputation hearts, and a verdict pill (green Correct / coral Incorrect).
  - A **Flawless** badge only when `Branch == CorrectNoIssues`.
  - The level-complete summary: cases, time, total EXP/reputation, and the career total after applying (when `PlayerProgressManager` exists).
- Final pass: the safe area on notched devices, 4:3 / 16:9 / 19.5:9 / 21:9, overlap checks against the minimap, icon column, joystick, dialogue panel and tutorial arrow, and contrast on bright office backgrounds.

## Acceptance

- The values on every screen equal the existing calculated results (no new calculations).
- Button behaviour, callbacks and progression saving are unchanged.

## What was built (2026-10-16)

- **`HudRewardScreen`** (new): one runtime presenter for the three result panels. It turns each into a reward card:
  - **Backdrop and card:** a dimmed backdrop over the game, and a navy card with a brass edge and a thin inner brass line.
  - **Title and subtitle:** the title in brass capitals, the subtitle below it.
  - **Hero:**
    - Side quest: the tier medal, which pops in, with a "SILVER BADGE" label.
    - Case: a green CORRECT or coral INCORRECT verdict pill with the verdict name, plus a gold **FLAWLESS** ribbon only when `Branch == CorrectNoIssues`.
    - Level: a PASSED (green) or BELOW PASSING (coral) banner, the EXP %, hearts % and needed %, and the earned medals.
  - **EXP bar:** fills while the number counts up, with the passing tick on the level card.
  - **Hearts:** a row that pops in one by one, filled for earned and slate for missed (or a number when there are more than 14).
  - **Fact chips:** time, cases, issues, wrong placements, badge.
  - **Summary:** the screen's own summary text.
  - **Footer:** on the level card, the career EXP and your best on this level, read before saving. On the side-quest card, how to reach Gold.
  - **Continue button:** restyled as a brass pill with navy capitals.
  - **Entrance animation:** the card scales and fades in (0.3 s), the hero pops (0.5 s), the EXP fills (0.9 s), then the hearts pop.
- **Fit:** the content is laid out at 1040×800 and scaled to fit each panel, keeping the Continue button clear. Fit scales: 4:3 0.79, 16:9 1.00, 19.5:9 0.90, 21:9 0.85 (superseded by revision 3: the card now fits the screen, see below).
- **Unchanged:** every serialized field, the Continue / Close / Return buttons and their callbacks, the save on return, and the scene panels. The screens still write their text fields, which are only hidden. Outside Office_Level1 the plain panels show as before.
- **HUD:** the HUD and its icons now step aside while any reward card is open (resolves K5).

## Final layout pass

- Reward cards fit at 4:3, 16:9, 19.5:9, 20:9 (phone) and 21:9, with a scale of 0.79 or more; this is checked in the HUD check.
- HUD clusters (card, tracker, toasts, minimap, icon column, badges) were checked in Phases 1–4 at 4:3 / 16:9 / phone. They stay inside the safe area (`MobileControlsSafeArea`).
- The reward cards are centred with at least 400 units of margin, so notches do not reach them.
- Contrast: all HUD text sits on navy panels (cream on navy ≈ 14:1, muted on navy ≈ 7:1). It never sits directly on the bright office.
- Known limit: a very long prompt name (max 560 units, shrinks to fit) could reach the joystick zone at 4:3; current prompt texts are short.

## Verification

- Compile: 0 errors / 23 baseline warnings.
- HUD check: **893 PASS**. New checks cover: the fit at five aspects, Office-only, texts hidden not cleared, the card blocking the world with Continue on top, no new callbacks or calculations, values taken from each result, Flawless only for CorrectNoIssues, saving and return unchanged, and the HUD stepping aside.
- Progression / Controls / Dialogue PASS. Layout previews rendered (side quest 16:9, level 21:9 and 4:3).
- Play Mode NOT RUN.

## Manual check (Unity)

1. Finish the corkboard. After the guide's talk, the Side Quest Complete card opens: the backdrop dims, the medal pops in with its tier label, the EXP bar fills to x / 50, and the hearts pop in. Chips show wrong placements and the badge. Continue closes it as before. The HUD is hidden while it is open and comes back with the reward notices.
2. Finish the case and Level 1. The Level Complete card shows PASSED or BELOW PASSING, the percentages, the medal, EXP x / 150 with the passing tick, the hearts x / 12, time and cases, and (from the main menu) the career line. Return to Main Module saves and loads as before.
3. (Other levels / the client path.) The Case Results card shows the verdict pill, FLAWLESS only for a perfect case, and time and issues.
4. Resize the Game view to 4:3, 16:9, 19.5:9 and 21:9. The card always fits and the Continue button never overlaps the content.

## Revision 2 (2026-10-16): focus + polish

Feedback: the joystick, minimap and other UI stayed visible behind the card; the card looked brown; the UI should look better without dropping anything.

- **Only the card shows.** While a reward card is open, every other UI branch next to it is hidden, and so is every other screen-space canvas. That covers the joystick, look area, minimap, icon column, prompt, tutorial panel, timer, toasts, effects and drag canvas. They are hidden with CanvasGroups and put back exactly as they were when the card closes. UI that another script fades back in is kept hidden while the card is open. The minimap also checks `HudRewardScreen.AnyOpen`. World-space canvases are not touched, and the backdrop dims them.
- **Card:** the body is opaque navy (the brass frame no longer bleeds through as brown), with a soft light sheen at the top and a darker backdrop.
- **Title:** a brass ribbon banner with folded tails and navy capitals. The subtitle is brass italic, followed by a brass divider with a diamond.
- **Hero:**
  - Slowly turning light rays and a pulsing glow behind the medal or verdict, coloured by the result (bronze / silver / gold tier, green / coral verdict, gold or grey pass).
  - Six twinkling sparkles around it.
- **Fact chips** have a brass outline. A divider sits above the summary.
- **Continue** gently pulses once the entrance animation has finished.
- Everything from revision 1 is kept: the values, medal, bars, hearts, chips, summary, footer, buttons, callbacks and saving.

Verification: compile 0 errors / 23 baseline warnings; HUD check **900 PASS** (new: hide and restore of the other UI, world-space untouched, kept hidden while open, opaque card, banner / dividers / rays / sparkles, minimap). Progression / Controls / Dialogue PASS. Previews updated (`../reward-screen-*.png`). Play Mode NOT RUN.

Manual check: open each card. Nothing but the card (and the dimmed world) is visible: no joystick, minimap, icons, prompt or HUD. Close it, and everything returns exactly as before (the joystick works, the icons are clickable, the minimap is back).

## Revision 3 (2026-10-16): flow, layout, mobile

Feedback: the cards had empty vertical gaps; the player could still move and look around while a card was open; the audit needed too many screens and buttons; Continue on Level Complete failed (the Progress scene is gone); prompts showed a mouse on a phone game; the console showed errors on leaving the level.

- **Card fits its content.** The card's height follows what it shows, with even spacing:
  - banner, subtitle, divider;
  - hero;
  - EXP bar, hearts;
  - chips;
  - divider and summary (measured);
  - footer.

  It is centred and scaled to fit the screen, so it no longer depends on the old panel's size. The scene's own button moves inside the card, bottom centre, as a large brass button with a shine and a soft shadow. It is the same button with the same callback. Previews: `../reward-screen-side-quest.png`, `../reward-screen-level.png`, `../reward-screen-audit-findings.png`.
- **The player is held** (no walking or looking around) while any card is open, and released on close. The hero, the hearts and the EXP bar settle to exact final values when the entrance ends, so nothing keeps moving. Each heart pops into a fixed slot.
- **Audit flow (fewer taps):**
  - A clean audit no longer opens the extra "Audit Summary" popup. The Auditor already says so in the dialogue, and the case reward / Level Complete card follows straight away (one tap fewer).
  - A failed audit opens an **Audit Summary findings card**:
    - "The Auditor found N things to fix";
    - a coral NEEDS ANOTHER LOOK banner;
    - each finding as a row;
    - a "Case reward −X EXP −Y hearts" chip;
    - what happens next;
    - one button, **REVIEW THE CASE** (same callback as the old Close).
- **Continue → level select.** Level Complete saves (unchanged), then loads **3D Menu** and opens it directly on the level select / progress view (`MenuEntry.OpenLevelSelect`, read by `ToggleCamera.Start`). The old Progress scene is no longer used; the old `mainModuleSceneName` value is only logged.
- **Polish:** a halo glow around the card, a larger banner with a shadow, an EXP bar with a dark well and a shine, wider issue and hero glows.

Verification: compile 0 errors / 23 baseline warnings; HUD check **935 PASS** (new: card fits the screen for 760/900/960-tall cards at five aspects with scale ≥ 0.85, content-sized card, button inside the card, player held, settle, audit skip / findings card, Continue → 3D Menu level select, used-once flag, no SetActive / SetParent on scene exit); Progression / Controls / Dialogue PASS. Play Mode NOT RUN.

Manual check:
1. Finish the side quest. The card is compact: medal, then the bars, then the chips, then the summary, with CONTINUE inside the card. You cannot move or look while it is open. Close it, and you can move again.
2. Submit the case with a mistake. The Audit Summary card lists the findings, with REVIEW THE CASE. Fix the case and submit it clean. There is no summary popup: Level Complete comes up right after the Auditor's lines.
3. On Level Complete, press CONTINUE. The game saves and lands on the 3D Menu level select (no main-menu step), and Level 2 is unlocked if you passed.
4. Leave Play Mode or the level. There are no console errors about SetParent or SetActive.

## Revision 4 (2026-10-16): Audit Summary always shows

Feedback: the Audit Summary did not appear. That was revision 3's skip on a clean audit. It now always shows:
- **Clean audit:** an **APPROVED** card ("The Auditor reviewed your case", "Every required fact is correct", Mistakes 0). It continues by itself after 3.5 s; a strip drains along the button, and tapping CONTINUE goes at once. The auto-continue presses the same button, so the same callback runs.
- **Failed audit:** the findings card with REVIEW THE CASE, as before.
