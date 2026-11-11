# First-person baseline and stored traversal — 12 November 2026

Status: **Implemented, internally verified; user Unity/device acceptance pending.** User explicitly approved the preceding plan, then requested execution. Scope: restore first-person exploration, retain sprint and smoothing, preserve third-person/jump work disabled, and move future adoption to a tentative final ticket. No later gameplay ticket was implemented.

## Active behavior

- First-person exploration outdoors and indoors in Levels 1–3. Existing desk/corkboard/interview/reader views and directed conversations remain under their existing owners.
- Sprint works in `Office_Level1`, `Office_Level2` and `Office_Level3` independently of the camera feature: push beyond the walking rim toward the chevrons, or hold Shift on desktop; 1.4× the existing first-person pace. Release, holds, pause and focus loss clear sprint through existing cleanup.
- No exploration Elijah body, automatic doorway perspective switch, jump button or practice curb is created. Keyboard and public jump requests are ignored while the feature is off.
- The Level 1 `run-jump` lesson keeps its saved-completion identity but now shows only the silent optional sprint hint. No active guide page points to a hidden jump control.
- Level 1 smoothed conversation Elijah and its latest calibrated fit are selected independently of the camera setting. Model bytes, material remaps, clothing inset, rigs, clips, GUIDs and smoothing import policy are preserved. Other chapters retain their current model scope. All other smoothing, NPC contact and gameplay plans remain required.

## Stored feature and reactivation boundary

`Assets/Resources/Player/ExplorationBody.asset` has `mixedCameraEnabled: 0`; the ScriptableObject field and absent-resource default are off too. Existing orbit/collision, doorway hysteresis, exploration body/playables, third-person targeting, hands/carried-paper switching, jump/landing/recovery, authored clips, jump UI and curb builder remain in place and inactive.

Later explicit selection of [T22](tickets/T22-tentative-third-person-and-jump-adoption.md) owns any reactivation. The stored opt-in supports **Level 1 only**; changing the field does not implement L2/L3 adoption or restore jump onboarding. T22 must deliberately adopt those paths, reaccept native motion/touch/phone cost and preserve the accepted first-person fallback. T21 remains the required first-person release gate; T22 is tentative afterward and does not block T04–T21.

## Changed implementation paths

- `Assets/Scripts/Player/ExplorationRules.cs`: independent sprint/smoothing adoption and explicit mixed-camera opt-in rules.
- `Assets/Scripts/Player/ExplorationBodyAssets.cs` and `Assets/Resources/Player/ExplorationBody.asset`: stored disabled feature setting, existing asset references retained.
- `Assets/Scripts/Player/CameraController.cs`: sprint feedback initializes before the disabled mixed-camera setup returns.
- `Assets/Scripts/Player/PlayerMovement.cs`: sprint availability separated from retained jump/ground/recovery availability.
- `Assets/Scripts/Player/ConversationActorVisual.cs`: smoothing no longer depends on mixed-camera adoption.
- `Assets/Scripts/Mobile/JumpButton.cs`: refuses construction without enabled jump support.
- `Assets/Resources/Tutorials/Level_01/guide.json`: sprint-only onboarding with compatible lesson ID.
- `Tests/T03ExplorationRegression/Program.cs`: active default, opt-in boundary, sprint chapters, smoothing independence, hidden jump/input/guide contracts plus retained traversal checks.

The milestone instructions, decisions, spec, journeys, current state, asset ownership, coverage, ticket index, remaining tickets and acceptance gates now use the first-person baseline. Older dated repair/implementation evidence remains historical rather than being relabeled as current acceptance.

## Fresh internal evidence

| Check | Actual result |
|---|---|
| Before edits: `python3 -B Tests/run_level3_repair.py --t03` | Runtime/Editor compile 0 errors; 21/10 existing warnings; T03 102/102 |
| After edits: `python3 -B Tests/run_level3_repair.py --t03` | Runtime/Editor compile 0 errors; unchanged 21/10 warnings; T03 114/114 |
| `python3 -B Tests/run_level3_repair.py --suites` | Level3 2855/2855, including T01 29/29; controls 5734 assertions (8 existing suite compile warnings) |
| `python3 -B Tests/VerifyThirdPersonPolish.py` | 27/27 retained asset/assembly contracts |
| `python3 -B Tests/VerifyThirdPersonExperience.py` | 33/33 retained scale/clip/clothing/binding/format checks |
| `python3 -B Tests/VerifyMobileInput.py` | 7 passed |
| `python3 -B Tests/VerifyGuideTargets.py` | 14 paths, 0 missing |
| `python3 -B Tests/VerifyT02Pilot.py` | 118 passed, 0 failed |
| `python3 -B Tests/run_level3_repair.py --suites --level2` | Level2 1580 passed; 3 explicitly skipped native EventSystem comparer checks; Level3/controls unchanged and passing |
| One-off documentation validator | 22 sequential/indexed tickets, acyclic prerequisites and matching direct dependents; T04–T21 inherit FP baseline; 623 local links, 0 missing; guide JSON parses |

All generated compilation output stays under `Temp/L3Repair`; comparison snapshots/scripts stay under `/tmp`. Source changes were reviewed against pre-edit snapshots without Git. Edited runtime newline conventions were preserved. No Unity launch/import/Play Mode/render, native touch, phone, listening, real-save access/reset or Git operation was performed. These are internal results, not user gameplay acceptance.

## User acceptance and next step

Allow normal import and start a fresh Play session. Check first-person plaza/office, no jump control/Space jump, sprint chevrons and release, then dialogue/desk/corkboard/reader exits with hands/paper intact. Repeat sprint and existing task returns in Levels 2–3; check Settings/app suspension and move/look touch in both landscapes. Confirm smoothed Level 1 dialogue Elijah. Record build/device/video and first-person G3/G8 results in [VERIFICATION](VERIFICATION.md).

T04 is next after active T03 controls/returns and the separate T02 Arthur contact/pilot acceptance; dormant traversal animation is not a T04 prerequisite. Existing phone, contact, voice, curriculum and unrelated inventory blockers remain explicit in [KNOWN_ISSUES](KNOWN_ISSUES.md).
