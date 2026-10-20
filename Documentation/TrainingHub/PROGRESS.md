# Tutorial Hub: progress

## 2026-10-17 (latest): held in 3D
- The user asked for a less steep look and a less flat binder that feels held. The eyes now look 30 degrees down (was about 50), the binder's top tips 24 degrees away, and its two halves (built as hinged halves at the spine) fold 11 degrees toward the eyes as it is picked up. Compile 0 errors / 29 warnings; TrainingRegression 513/513. Not seen in Play Mode yet.

## 2026-10-17 (later): on the desk, in front of the character
- From the user's Play Mode screenshot: the character at the desk was visible next to a floating binder, and Arthur's medallion shimmered. Now the camera becomes that character's eyes (his model hidden while open), and the binder is picked up from the desk in front of him and rests just above it, facing him.
- Medallion shimmer: its layers were 0 to 2 binder pixels apart under the menu camera's long depth range (z-fighting), and the brass was glossy while the binder sways. Now the layers are well apart, the overlay camera has its own short depth range, and the medallion is matte.
- Renamed to **TUTORIAL BINDER** (training.json `title`).
- Verified here: compile 0 errors / 29 warnings; TrainingRegression 513/513. Not seen in Play Mode yet.

## 2026-10-17: the hub moved into the 3D Menu
- From the user's Play Mode screenshots of the separate TutorialHub scene: first off centre with a lamp hot spot, then too bright. The user asked for the menu's own look, and for the binder to live in the 3D Menu like the settings board, reached by a camera move to the computer desk ("yumuko"), as if holding the binder.
- New `TrainingBinder3D` (adds itself to the 3D Menu): the lean from the menu view to the desk and back, the binder held in front of the camera on an overlay layer, menu button pops, all binder features unchanged. `MainMenu` opens it; back from a drill the menu opens straight on it.
- Removed: `TutorialHub.unity` (and from Build Settings), `TutorialHubPresenter.cs`, the editor **Set Up Scene** item. `make_scratch_build.py` now skips deleted scripts.
- Verified here: scratch compile 0 errors / 29 warnings; TrainingRegression 513/513; SettingsRegression 93/93. **Not seen in Play Mode yet**: the lean pose, binder size and the NPC side-step need the user's check.

## 2026-10-16: built (plan approved the same day)

| Ticket | Status |
|---|---|
| T1 Save v4, recorder, guide notifications, practice guard | Done |
| T2 Catalog (`training.json`) and `TrainingCatalog` | Done |
| T3 TutorialHub scene, binder, tabs, cards, menu route, Build Settings, editor setup | Done |
| T4 Practice Mode core and the three Basics drills | Done |
| T5 Side Quest 1 drill | Done |
| T6 Case drills (desk, values with `case_002` and `case:correct`, hand-in) | Done |
| T7/T8 Celebration (hop, stamp slam), docs | Done |

### Verified here (no Unity editor)
- Scratch compile: **0 errors / 29 warnings** (same as the baseline).
- `Tests/TrainingRegression`: **511/511**.
- Unchanged and passing: GuideRegression (714), CareerBoardRegression (69/69), SettingsRegression (93/93), ProgressionRegression, ControlsRegression (5734 assertions), DialogueRegression (799 assertions), VerifyGuideTargets (17 paths, 0 missing).

### Not verified (needs the user's Play Mode / device checks)
Nothing below has been seen running. Suggested pass:
1. Unity imports the new scripts, scene and JSON without errors. If the TutorialHub scene does not open cleanly, run **Tools > TaxVenture > Tutorial Hub > Set Up Scene**.
2. **Tools > TaxVenture > Tutorial Hub > Check Content**: no warnings.
3. 3D Menu → tutorial button → the binder appears (camera framing, lamp, page legibility on a phone-sized Game view). Only BASICS is shown on a fresh save.
4. Basics 01-03: PRACTICE (guided) end to end, the result card, MASTERY RUN, the stamp, BACK TO BINDER (card hops, stamp slams once).
5. LEAVE (two taps) returns to the binder without a result.
6. Play Chapter 1 normally: after the corkboard lesson, the SIDE QUEST tab appears in the binder; after desk lessons, CASE appears. SKIP TUTORIAL opens nothing.
7. Side Quest 1 drill, both runs (the Side Quest Complete card under the result card does not move on).
8. Case drills: the player stands in the reception (not inside a wall or on a desk), the door is open, the tracker says "Talk to the receptionist", Marco's case and profile appear, `case:correct` finishes the Moving Values mastery, the Hand It In run ends on approval with nothing saved to the career (career board unchanged).
9. The hidden scene parts: nothing broken (minimap, NPCs, desk) and the run feels lighter.

## 2026-10-20: first-finish access and tutorial settings wording

User-requested fresh menu now exposes only Play and Settings until Level 1's first saved finish. The tutorial menu object is hidden in that state, and TrainingBinder3D.Open also rejects a stale/direct request. After completion, the normal binder/catalog rules resume. Training practice settings use Replay Tutorial, Resume Tutorial, Tutorial Paused and matching confirmation copy; the same drill/mode is retained on replay.

Scratch compile: 0 errors / 29 baseline warnings. SettingsRegression 133/133; TrainingRegression includes the new binder entry check. Visual/Play Mode acceptance is the user's. Handoff/checklist: Documentation/SettingsActions/FreshMenuAndLighting.md.

## Audio continuity (2026-10-20)

- Training Binder opens with the panel cue, turns modules with a page turn, uses the back cue on close, and closes with the panel cue when starting a drill. Missing drills give unavailable feedback.
- Practice result cards request completion music while shown and clear the request when the level closes. Practice still awards no career rewards.
- AudioRegression: 195 checks passed. Scratch build is pending the root integration pass; Play Mode/device checks remain user-owned.
