# 3D Menu career board

The level select in `Assets/Scenes/3D Menu.unity` (Play → camera flies to the wall) is two real 3D corkboards, built at runtime by `CareerBoardPresenter` on the `LevelSelector/Career Board` object. It keeps the old format (Back, Progress board on the left, Story board with five numbered papers and a side-quest paper on the right) but everything is scene geometry with world TextMeshPro lettering. Only the progress bars use tiny world-space canvases.

## What the player sees

| Board | Contents |
|---|---|
| **CAREER** (left) | Navy/brass name plate and three pinned index cards, largest at the top. **CAREER**: total EXP and total hearts (large numbers), each with a Level 1 HUD bar against the sum of every open chapter's maximum, plus chapters passed with one mini pushpin per chapter. **NEXT GOAL** (smaller, no bars): the chapter to play next, then its EXP and hearts as counts (best / needed), and one line saying what to do. **BADGES** (smallest): a plain sample coin medal and the earned count ("1x"), centred. The coin is deliberately not a real side-quest badge. |
| **STORY** (right) | Plate and "Tap a pinned paper" hint. Five chapter papers (200 × 240) keep the 3 + 2 arrangement, with pins nudged up and down so they look hand-pinned and a clear vertical gap between the rows. Yellow = open, grey + padlock = locked, blue-grey = coming soon. Each paper shows CHAPTER, a handwritten number, the short title, best EXP and hearts, **best time**, and either a status line (TAP TO PLAY / LOCKED / COMING SOON) or a rubber stamp (PASSED green, TRY AGAIN orange). **Red strings** run from each chapter to the next once that next chapter is unlocked: none while only Chapter 1 is open, then 1-2, and so on, never to a locked chapter. Bottom left: **SIDE QUEST BADGES N / 5**, with five fixed badge spaces (`badgeSlots`). Each side quest gets a white paper (116 × 168) with its real Level 1 medal (84 px) hanging from the pin, "SIDE QUEST n", its name, and tier and best time. Tapping the paper opens its chapter. Unused spaces show an outline, a pin hole, "?" and "Not yet found". |

Motion and feedback:
- Papers sway on their pins (the Level 1 `ProfileDragCard.UpdateSway` motion), drop in and get pinned when the camera arrives (paper sound), peel toward the player on hover/press and wobble when tapped. The chapter to play next has a gold pin and a slow pulse.
- **Since the last visit**: EXP and hearts count up, a newly passed or finished chapter has its stamp slammed on, and a new or better badge pops and spins (achievement sound). "Last seen" lives in PlayerPrefs (`TaxVenture.Career.Seen.v1`). It is a viewer convenience and never touches the save.
- Tapping a paper lifts it off its pin. It flies forward as a large clipped sheet showing the chapter number, title, story, status stamp, best result bars with the pass mark, attempts, pass line, **best time / last run / total time played**, the side-quest medal with "Side quest n: name, tier, best time", and **START** (green), **REPLAY** (brass) or a disabled LOCKED / COMING SOON button. Tap outside, CLOSE, BACK or Escape/Android back puts it back. BACK on the board returns to the main menu.
- Touch is ignored while the camera moves, while another UI is under the finger, and after a chapter starts loading.

## Reused from Office Level 1

- **Corkboard**: `SM_Prop_CorkBoard_01` (the side-quest board in `Office_Level1`), assigned to `corkboardPrefab`. It is fitted to each board size and faces the camera. Without it, a simple frame and cork panel is built instead.
- **Papers and pins**: white cards on red pushpins with the same sway as the profile cards.
- **Medals**: `HudBadgeArt.Get(emblem, tier, ribbon)`, the same sprite the HUD and reward screen use, drawn as a world sprite with depth layers and a tier glow.
- **Look**: `HudTheme` palette, fonts (`HudFontSet` UI font and handwriting font), star and heart icons, bar style, easing.

## Files

| File | Role |
|---|---|
| `Assets/Scripts/RewardsSystem/CareerProgress.cs` | **The career module** (pure C#): catalog types, `Build(save, catalog, rules)` returns a `CareerSnapshot` (chapter states, launchability, pass targets, totals, badges, goal). Also provides `NextStep`, and `Diff` / `Remember` for the "since last visit" changes. It never writes the save. |
| `Assets/Scripts/RewardsSystem/CareerProgressService.cs` | Unity side: loads `Resources/Career/chapters.json` (with a built-in fallback), resolves each chapter's rules from `rewards.json` and `LevelScoring`, builds from `PlayerProgressManager`, and stores the last-seen state. |
| `Assets/Resources/Career/chapters.json` | **User-editable** chapter list: id, number, noteTitle, title, description, scene, cases, comingSoon, and side quests. See its `_readme`. |
| `Assets/Scripts/UI/Menu/CareerBoardPresenter.cs` | Builds both boards, fills them from the snapshot, handles input (Input System `Pointer`, physics raycast), the intro and celebrations, and launching. |
| `CareerPinnedPaper.cs` | A paper on a pin: sway, intro drop/pin, hover peel, pluck wobble, stamp slam, lift. |
| `CareerChapterSheet.cs` | The flying chapter sheet with START / REPLAY. |
| `CareerBadgeMedal.cs` | Hanging Level 1 medal with glow and pop. |
| `CareerBoardButton.cs` | 3D push button (brass base and face that sinks). |
| `CareerBoardTarget.cs` | Base for anything tappable (collider, hover, press, tap). Used bare as a tap blocker. |
| `CareerBoardKit.cs` | Geometry, prop fitting, pushpins, padlock, world text, sprites and the bar canvas. |
| `Assets/Editor/CareerBoardSetup.cs` | **Tools > TaxVenture > 3D Menu > Capture Career Board** renders previews with sample saves and runs interaction checks. |
| `Tests/CareerBoardRegression` | 44 checks on the real module source, chapters.json, scene routing and wiring. |

## How to edit

- **Chapters, titles, scenes, Coming Soon**: edit `chapters.json`. A chapter unlocks when the previous playable chapter is passed.
- **Pass line, EXP/heart maximums, badge art/name**: that level's `Assets/Resources/Levels/<id>/rewards.json` (`passingPercent`, `sideQuest.badge*`).
- **Layout**: constants at the top of `CareerBoardPresenter` are in board pixels (layout 1500 × 850, origin at the centre, +y up, −z toward the camera). `boardScale` sets the world size; screens narrower than 16:9 shrink it.
- **Colours**: `CareerBoardKit` (paper, pins, ink) and `HudTheme`.

## Time spent

- `LevelScoreLedger` times every run with a clock that `CaseProgressionManager` sets to `Time.unscaledTime` (no clock in tests means no time). The level time runs from level load to level complete. The side quest time runs from the first collected taxpayer profile, or the level start, until the side-quest reward.
- `LevelTotalResult.RunSeconds` / `SideQuestSeconds` → `PlayerProgressManager.ApplyLevelResult` → `PlayerProgressData.RecordLevelTime`, stored in `LevelRecord`: `bestSeconds`, `lastSeconds`, `totalSeconds`, `sideQuestBestSeconds`, `sideQuestLastSeconds`. The fields are additive, so older saves load with 0, shown as `--:--`.
