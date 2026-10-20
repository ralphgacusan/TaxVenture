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

## 2026-10-16: plain, bigger corkboards
- Both boards now use the plain `SM_Prop_CorkBoard_03`. CorkBoard_01 had baked-in papers that hung off and overlapped the real ones.
- Boards are bigger: CAREER 420 × 790 (was 400 × 700) and STORY 1110 × 840 (was 1060 × 770), side by side with a 20 px gap. BACK moved up above the CAREER plate. Papers and cards keep their positions.
- CareerBoardRegression 67/67 (new checks: plain prop, sizes, no overlap, on-screen at 16:9, BACK clear of the plate). Scratch compile 0 errors. Play Mode: NOT RUN (user).

## 2026-10-16: boards lower, contents higher, always plain
- The boards now take their mesh from the scene's own plain level board (`Corkboard Levels`, SM_Prop_CorkBoard_03 — only its renderer, not the old notes), then fall back to `corkboardPrefab`. This fixes stray baked-in papers even when the open scene still points at CorkBoard_01 (an unreloaded scene).
- Both boards moved down 25 px; every card, paper, badge space and the badge title moved up 10 px.
- CareerBoardRegression 69/69 (new: cards inside the CAREER board, plain mesh used first). Scratch compile 0 errors. Play Mode: NOT RUN (user).

## 2026-10-20: earned-only side-quest papers

User feedback: a fresh/reset career showed two named badge papers despite 0/5 badges. CareerBoardPresenter.ApplyBadges now reveals a paper/medal only when its catalog badge has Tier > 0. Known tier-zero quests use the same question-mark placeholder as future quests, so all five slots start unknown. Badge progression, rewards, saves, earned-paper interactions and upgrade celebrations are unchanged.

Internal verification: scratch build 0 errors / 29 baseline warnings; CareerBoardRegression 72/72 (including fresh zero-tier, earned count and presenter visibility checks); SettingsRegression 123/123; Level2Regression 1,280 PASS. User Play Mode/visual checks pending: fresh/reset career has five questions, earning a badge reveals only its own paper, existing medals still open their chapter.

The same session adjusts settings action-button layout and reuses SceneFade for leave/replay fade-out and destination fade-in; handoff: Documentation/SettingsActions/README.md.
