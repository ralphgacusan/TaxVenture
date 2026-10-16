# Progress: 3D Menu career board

## 2026-10-16: rebuilt as real 3D (replaces the world-canvas attempt)

Done:
- Career module `CareerProgress` + `CareerProgressService` + `Resources/Career/chapters.json`: snapshot of chapters, pass lines, totals, badges, next goal, and the "since last visit" diff. It refreshes live on `PlayerProgressManager.ProgressChanged`.
- 3D boards: `SM_Prop_CorkBoard_01` ×2, navy/brass plates, pinned index cards (career, next goal, badges), five chapter papers + side-quest paper with red string, 3D back button, flying chapter sheet with 3D START/REPLAY.
- Animations: idle sway, drop-and-pin intro, hover peel, tap wobble, stamp slam on new passes, badge pop/spin, EXP/hearts count-up, bar fills, sheet fly-in/out and hand-held drift.
- Removed the old `CareerLevelCard` script and prefab (the flat world-canvas cards). The scene component's fields were updated: `corkboardPrefab`, `originalBoardName`, `boardScale`.

Verification:
- Scratch compile of all runtime scripts: **0 errors**, 29 warnings (baseline, none in Career files).
- Editor script `CareerBoardSetup.cs` compiles against it: 0 errors.
- `Tests/CareerBoardRegression`: **44/44**. Controls 5734, Dialogue 799, Guide 711, Progression: all pass.
- Editor preview capture: **NOT RUN.** The open editor was not focused, so it did not recompile. `Library/CareerBoardSetup.request` was left in place, so the capture runs the next time the editor recompiles while focused.
- Play Mode / device: **NOT RUN** (user).

Play Mode checklist for the user:
1. Play → level select: papers and cards drop in and get pinned, then sway. Cork boards face the camera and are not mirrored or rotated.
2. Tap Chapter 1 → sheet flies out → START loads the intro cutscene. Tap Chapter 2 on a fresh save → LOCKED. Tap 3-5 → COMING SOON.
3. Tap outside, CLOSE, BACK and Escape all close the sheet. BACK again returns to the main menu.
4. Finish Level 1 → return: EXP and hearts count up, the PASSED or TRY AGAIN stamp slams, and the badge pops.
5. A 4:3 window keeps both boards in view.

## 2026-10-16 (second pass): layout changes requested after the first test

Done:
- CAREER board: removed the badges card. It now has two larger cards with bigger, more readable text. **CAREER** shows EXP and hearts with bars against every open chapter's maximum, chapters passed, and the badge count (a sample coin + "Nx", not a real badge). **NEXT GOAL** is unchanged in content but larger.
- STORY board: all badges moved to the bottom-left **SIDE QUEST BADGES** area, which has five fixed spaces. Each side quest has a small clickable paper with its pinned Level 1 medal, side quest number, name, tier and best time. The rest are empty "Not yet found" spaces. The intro, sway, medal pop and sheet behaviour are kept.
- Red strings now link chapter to chapter once the next chapter is unlocked (the Chapter 1 → side quest string was removed).
- Chapter pins are nudged up and down; the arrangement is unchanged.
- Time spent: tracked per run and per side quest, saved, and shown on the chapter papers (best time), badge papers (best side-quest time) and the sheet (best / last / total, side-quest time).
- Fix: a save change while the board is open no longer replays the drop-in intro.

Verification:
- Scratch compile: 0 errors, 29 warnings (baseline). Editor script: 0 errors.
- CareerBoardRegression **57/57**. GuideRegression **714** (3 new ledger-clock checks), Controls 5734, Dialogue 799, Progression: all pass.
- Editor capture and Play Mode: **NOT RUN** (the user tests manually). `Library/CareerBoardSetup.request` is in place, so the capture runs on the next focused recompile.

## 2026-10-16 (third pass): three career cards, bigger badges, no overlap

Done:
- CAREER board now has three cards. CAREER is the large one (290 tall, EXP/hearts bars, chapters). NEXT GOAL is smaller (180) and shows counts only, with no bars. A new BADGES card (112) shows a centred sample coin and the count.
- Story board: chapter papers are slightly smaller (200 × 240) and the right two columns moved right, so Chapter 2 and Chapter 4 have about a 38 px vertical gap. Badge papers are larger (116 × 168) with 84 px medals (was 62). Badge tilts are smaller so neighbours never touch.
- New regression check: parses the pin and size constants and fails if any two papers overlap or leave the story corkboard.

Verification: scratch compile 0 errors / 29 warnings; editor script 0 errors; CareerBoardRegression 61/61; Guide 714, Controls, Dialogue, Progression all pass. Play Mode: NOT RUN (user).

## 2026-10-16 (fourth pass): polish

- Fix: the chapter sheet now flies out of, and back into, every paper the same way. A negative paper tilt was read from `localEulerAngles` as about 356°, so Chapters 1, 3 and 5 and some badge papers spun almost a full turn; only Chapters 2 and 4 (positive tilts) looked right. The tilt is now normalized with `Mathf.DeltaAngle`, and the animation itself is unchanged.
- The PASSED / TRY AGAIN stamp is smaller (128 × 34, 21 px text).
- Chapter 1 hangs a little higher (pin y 268 → 282).
- Secondary text on the career cards (headings, captions, units, goal hint) uses `CareerBoardKit.InkCard` (38, 41, 50), close to black.
- CareerBoardRegression 63/63. Scratch compile 0 errors; the editor script and the other suites pass. Play Mode: NOT RUN (user).
