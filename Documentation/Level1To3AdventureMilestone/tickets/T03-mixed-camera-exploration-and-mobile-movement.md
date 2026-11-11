# T03 — First-person exploration and sprint; stored mixed-camera pilot

## Active acceptance — 12 November 2026 (supersedes mixed-camera requirements below)

User-approved first-person baseline amendment implemented; internal and user evidence is recorded in [FIRST_PERSON_BASELINE_2026-11-12](../FIRST_PERSON_BASELINE_2026-11-12.md). First person outdoors and indoors; existing task/dialogue views restore correctly. Sprint and its joystick feedback remain in Levels 1–3. Smoothed conversation Elijah remains independent of the camera flag. The outdoor body, third-person orbit/doorway switching, jump/button and practice curb are stored disabled. The legacy `run-jump` guide ID now contains only the optional sprint hint.

User checks before T04:

- [ ] Start fresh Play in Level 1: first person in plaza and office, no visible outdoor body, no perspective switching at door, no jump button or curb lesson.
- [ ] Walk and look; drag beyond the walking rim toward chevrons to sprint, release to walk. Shift sprints on desktop. Space and former jump area do not jump or steal touches.
- [ ] End dialogue, desk, corkboard, profile reader and counter views: first-person look, movement, hands and carried paper restore correctly; one Elijah in dialogue only.
- [ ] Settings, control holds, focus loss and app suspension cancel sprint without stuck movement or tap-through. Verify move/look multitouch in both landscape orientations.
- [ ] Check sprint and existing task returns in Levels 2–3, with their existing content/save behavior unchanged.
- [ ] Confirm smoothed Level 1 dialogue Elijah remains intact. Record build/device and current first-person G3/G8 evidence.

T04 depends on this first-person control/return acceptance and the separate Arthur contact pilot. It does not require third person, jumping or traversal animation approval. T22 is tentative after T21 and requires later explicit selection.

## Historical mixed-camera contract and implementation evidence

The remaining older sections describe the stored pilot and its previous reviews. Their TP/jump requirements are deferred to T22; they are not active T03 acceptance criteria.


Latest (12 November): [third-person experience repair](../THIRD_PERSON_EXPERIENCE_REPAIR_2026-11-12.md) after the user rejected the polish below: measured scale fix (1.58 m canonical), lower-centre framing, outdoor walk/run pace, re-authored run/jump with explicit phases, jacket fix; T03 102/102, VerifyThirdPersonExperience 33/33; native acceptance pending.

Previous follow-up: [consistent third-person scale, animation/control polish and T02 fitting](../THIRD_PERSON_POLISH_2026-11-11.md), implemented and internally verified (runtime/Editor 0 errors; T03 69/69, T02 99/99, polish 27/27). This supersedes the smaller capsule visual and missing-sprint/jump statements in historical records below. Fresh user-rendered/native acceptance remains pending.

## Status and readiness

**Implemented, internally verified; user manual pending** (11 November 2026, AI implementation session). The Level 1 loop, sprint, jump, doorway switching, returns, targeting and onboarding are implemented and pass internal checks; the user confirmed third-person movement/JUMP but reported an invisible exploration body; rendered acceptance of the repair remains pending. Run/jump/landing animation and pilot-dependent art remain explicit gaps (see [Implementation record](#implementation-record--11-november-2026)). T02 pilot acceptance is still pending; no T02 derived model is adopted.

## Follow-up after user screenshots — 11 November 2026

The first repair did **not** pass rendered acceptance: Elijah was visible but oversized, turning and swinging his legs too fast; pilot Arthur's parts were displaced. The [follow-up repair](../CHARACTER_PLAYBACK_REPAIR_2026-11-11.md) fixes HumanPose world/local conversion and pelvis preservation, final-scale binding order, capsule-derived outdoor height, camera framing, explicit clip timing and bounded cadence/travel heading. **T02 87/87; T03 66/66; runtime/Editor 0 errors, unchanged 21/10 warnings.** Current C import is populated (4382 v / 5474 tris); native animation/deformation/feel reacceptance remains pending. No scene/rig assembly is assigned to the user, and T04 remains Planned.

## Repair record — 11 November 2026

User-rendered review found defects in the original implementation. The requested outdoor Elijah visibility repair is **implemented, internally verified; user rendered reacceptance pending**. See [fresh diagnosis, changes, exact checks and retest](../VISIBILITY_REPAIR_2026-11-11.md). T02 now passes 75/75, T03 54/54; current runtime/Editor compile with 0 errors and unchanged 21/10 warnings. This was the earlier internal repair result; the subsequent user import populated C, but screenshots rejected deformation and body scale. See the follow-up above; no new rendered/phone pass is claimed. T04–T21 remain Planned.

## Priority and relative size/risk

P1. Size: **L**, bounded to one fresh implementation context. High: camera/control/physics and shared FirstPerson assumptions. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

[T02](T02-visual-contact-and-device-pilot.md) External decisions/assets/evidence: E-PILOT accepted before broad model migration; current phone thumb/navigation acceptance.

## Direct dependents

[T04](T04-arthur-reviewed-file-embodied-teaching.md), [T05](T05-living-community-and-profile-round.md)

## Goal and visible accomplishment

One Elijah can move outdoors in third person, enter the office in first person, use a task/conversation and return to the correct exploration context with sprint/jump available.

## Current behavior and evidence

PlayerMovement walks camera-relative; CameraController/Interactor/holds/guide/shots explicitly assume FirstPerson. ThirdPersonCameraFollow is an independent old writer. ConversationActorVisual only shows Elijah during dialogue. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [CameraController.cs](../../../Assets/Scripts/Player/CameraController.cs)
- [PlayerMovement.cs](../../../Assets/Scripts/Player/PlayerMovement.cs)
- [Interactor.cs](../../../Assets/Scripts/Player/Interactor.cs)
- [PlayerControlHold.cs](../../../Assets/Scripts/Player/PlayerControlHold.cs)
- [ConversationActorVisual.cs](../../../Assets/Scripts/Player/ConversationActorVisual.cs)
- [VirtualJoystick.cs](../../../Assets/Scripts/Mobile/VirtualJoystick.cs)

## Scope and explicit exclusions

Expand current owner capability, prove L1's actual outdoor→office→task→dialogue→return loop and safe control onboarding. Preserve FP default in unadopted scenes. Exclude alternate input/camera framework, stamina/combat and mass scene regeneration. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

L1-A/B and the office doorway/task/conversation round trip. Walk/look/sprint and optional curb-hop or flat alternative in a short safe area; approach office, cross stable doorway pair, use desk/dialogue and exit to exact valid TP pose/look.

## Mechanics and domain logic

Implement SPEC camera/input rules: collision-shortened orbit, player-root reach/occlusion, held joystick outer-ring sprint, separate right jump pointer, grounded one-press jump, last-safe recovery, doorway hysteresis and deferred switching while owned. Snapshot actual exploration region/pose/look for all returns.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Stable doorway crossed | One TP/FP transition | Existing camera owner; safe region only | Hysteresis prevents oscillation |
| Task/dialogue owns view | Stable close framing | Snapshot actual prior exploration | Newest valid owner wins on return |
| Hold/focus loss/fall | Sprint/jump pointers cleared; safe body | No task completion or item loss | Return last grounded legitimate point |
| Invalid world target | Neutral refusal | Player reach/occlusion required | Keep exploration usable |

## Visual/environment/interaction specification

Reuse same Elijah identity and existing body/hands/carry visual authority. Show one grounded body outdoors and spatially correct hands/paper indoors; no doubled avatar, clipped head or altered player collider scale.

## Animation and physical contact

Compatible walk/stop/turn, accepted bounded sprint/hop/landing sequence; preserve cutscene walking imports. Contact/grounding is reviewed at real scale; missing compatible capability is explicit.

## Audio/dialogue/voice

Reuse footsteps/door/carry cues; no repeated step spam in airborne/held states. Short silent control prompts follow approved budgets; ordinary dialogue unchanged until its owning slice.

## Camera/mobile/accessibility

≥48dp jump target, safe thumb zones and simultaneous move/look/jump; closing UI touch never orbits/interacts. Back/settings/focus/disable clears only owned pointers/holds.

## Data/interface/persistence

Minimum additive teaching/region-safe checkpoint facts only where existing state cannot derive them; preserve mode enum values/UnityEvents and current save owners. Never persist a midair camera transform. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

All owner-loss/mode-change paths return to newest valid context, not blindly FP. Falling/interruptions retain carried sources; door boundary cannot oscillate or lock player out. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

No second camera/listener/controller, per-frame global searches or uncapped collision/path queries. Compare loop against T02 measured ceilings; cache references and use bounded casts.

## Internal checks — AI only

- [x] Compile changed paths; behavioral camera/control snapshot, stale-owner, dialogue/viewer/hold, doorway threshold and player-root targeting tests in current controls/dialogue/guide seams. (`T03ExplorationRegression` 49/49; Dialogue 1166, Guide 953, controls 5734 PASS.)
- [x] Exercise jump press/release/grounding/recovery, sprint cancellation, multi-pointer UI ownership and failed/cancelled transition without task progress. (Pure-rule sequences and source contracts; native physics/touch is the user's check.)
- [x] Static scene/reference checks find one writer/body/listener and preserve L2/L3 default behavior until adoption. (Level2 1580, Level3 2855 PASS; scene contracts in the T03 suite.)

## USER Unity/device acceptance

- [ ] Play actual loop in both landscapes with separate move/look/jump fingers; camera collision, door and task/conversation returns remain stable.
- [ ] Try Back/settings/focus, rapid taps, carried paper, boundary standing and fall recovery; no clipped body, stuck sprint/hold or through-wall reach.
- [ ] Record actual build/device/steps/evidence for applicable gates **G2, G3, G8** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.


## Implementation record — 11 November 2026

Owner: AI implementation session (Claude), user-selected T03 only. T01 clip/unclip safeguards, T02 review scene and campaign-save guard preserved; no T02 derived model adopted; M01-SUMMARY, M01-FOLDER-RECORD and Arthur contact untouched. No Git, Unity launch/import, ADB, real-save access, package/asset download, purchase, paid generation, agents or contact. No scene, prefab or existing asset rewritten.

### Player-visible behaviour (Office_Level1 only)

- **Outdoors third person, indoors first person.** `CameraMode.FirstPerson` still means "exploring" (serialized value and all 15 dependent checks unchanged); CameraController now keeps an exploration *view*. Outdoors the existing Main Camera look controller writes a behind-the-shoulder orbit (2.4 m, touch look shared with first person) pulled in front of walls by one bounded sphere cast; indoors it is the original first-person view with hands. One 0.35 s blend per change.
- **Stable doorway.** The office entrance line comes from `OfficeDoorGate`'s own door leaves (outward = away from the reception floor). The view changes only after the player is 0.6 m past the line (hysteresis band); standing on the threshold cannot flip it. Away from the door the floor under the player decides after 0.8 s (spawn/reload). Switching waits while a task, conversation, viewer, control hold, camera transition or the previous blend owns the camera, then re-evaluates where the player actually is.
- **Correct returns.** Desk, interview, corkboard, profile viewer and conversation return to whichever view the region gives, because the view is never changed by the task; the conversation return pose is the settled third- or first-person view. The outdoor body hides during fixed task views and while the conversation shows its own Elijah, and comes back on every end path.
- **One body.** `ExplorationBody` shows Elijah's own Humanoid model (`Elijah_Walking.fbx`, same standing height as the conversation Elijah) in third person only, feet on the capsule bottom, with no colliders and no change to the player Rigidbody/capsule/scale. Walk cycle advances by distance actually travelled (feet match at walk and sprint speed). First-person hands stay hidden in third person; the same carried-paper object rides Elijah's left hand and returns to the first-person view indoors.
- **Sprint.** Hold the movement stick in its outer ring (shown as a ring that lights while sprinting): 1.4× walk, enter at 92% stick travel, leave below 80%. Cancels on release, holds/tasks/dialogue (movement disabled), focus loss, pause and disable. No stamina.
- **Jump.** A right-side **JUMP** button (≥ 48 dp, own pointer, so move + look + jump work together; UI, so it never turns the camera or taps the world). One grounded jump per press (0.12 s step-off grace, no repeat while held, no mid-air jump); apex 0.42 m. Steps are silent in the air. A fall 3 m below, or 3 s airborne, returns the body to the last settled grounded point; nothing carried or saved changes.
- **Targeting.** In third person the centre aim may reach past the camera offset, but a target counts only within the player's own 3 m reach, not behind him, and with nothing solid between his eye and it; direct world taps also need that line of sight. First person keeps its exact original ray.
- **Safe onboarding.** A silent two-page guide hint after "look" ("Push the stick to its outer ring to run." / "Tap JUMP to hop the low curb, or just walk around it."), each ended by a real sprint/jump or simply by walking on; its completion is kept by the existing Level 1 guide-lessons record. A 0.18 m practice curb with a yellow edge is placed deterministically near the saved plaza start where its volume and a flat route beside it are clear; it gates nothing.
- **Holds.** A control hold now stops both look controllers (Main Camera's and the helper's), so the view cannot turn under a guide page.

### FirstPerson-assumption audit

| Location | Finding | Action |
|---|---|---|
| Interactor, PlayerControlHold, ObjectiveGuide, GuideDirector readiness/TrackMotion, HudWindowLock, OfficeHours wait, NpcStatusMarker, ViewTurn, DialogueCameraDirector `OwnsCamera`, Level2/3 table/consultation/book checks, CaseFolder3DView/Level2TaxCodeBookIcon `openedInMode` | All mean "exploration owns the camera"; true in both views | Kept |
| Interactor centre ray / InteractionRange taps | Camera distance would set reach in third person | Player-root reach, facing and line of sight in third person |
| DialogueCameraDirector | Would have shown the conversation Elijah beside the exploration body | Body hidden while framed; released on every end path |
| FirstPersonHands / carried paper | Camera hands would float in a third-person shot | Wanted state kept; hidden in third person; paper to Elijah's hand |
| PlayerControlHold | Held only one of two look controllers | Holds all |
| Office scenes' two FirstPersonCameraController components | Main Camera's writes the camera; CameraController referenced the helper | View sent to both; Main Camera's is the single writer (no scene edit) |
| NpcStatusMarker distance from camera | Marker shape thresholds see the camera ~2.4 m farther outdoors | Kept; user to judge (minor) |
| ThirdPersonCameraFollow | Old independent writer, not in any scene | Untouched, never enabled |

### Files

New: `Assets/Scripts/Player/ExplorationRules.cs`, `ExplorationBody.cs`, `ExplorationBodyAssets.cs`, `ExplorationPracticeCurb.cs`; `Assets/Scripts/Mobile/JumpButton.cs`; `Assets/Resources/Player/ExplorationBody.asset` (+ folder); fresh `.meta` GUIDs for all; `Tests/T03ExplorationRegression/Program.cs`.
Changed: `Assets/Scripts/Player/CameraController.cs`, `FirstPersonCameraController.cs`, `PlayerMovement.cs`, `Interactor.cs`, `InteractionRange.cs`, `FirstPersonHands.cs`, `DialogueCameraDirector.cs`, `PlayerControlHold.cs`; `Assets/Scripts/Mobile/VirtualJoystick.cs`; `Assets/Scripts/SideQuest1/OfficeDoorGate.cs`; `Assets/Scripts/UI/Guide/GuideTargets.cs`; `Assets/Resources/Tutorials/Level_01/guide.json` (one lesson inserted); `Tests/run_level3_repair.py` (opt-in `--t03`). All LF, as before.

### Commands and results

| Command | Result |
|---|---|
| `python3 -B Tests/run_level3_repair.py --build-only` | runtime 0 errors / 21 warnings, Editor 0 / 10 (unchanged) |
| `--suites --t03` | **T03ExplorationRegression 49/49** (first runs 47–48/48: two over-broad persistence checks matching old comments/"hasPrinted", corrected in the test) |
| Mutation probe (scratch `ExplorationRules.cs` without the doorway band; `Temp/T03Mutation`, deleted) | 2 doorway failures |
| `--suites` / `--suites --level2` | Level3Regression 2855/2855, controls 5734, Level2Regression 1580 (3 native EventSystem checks skipped as before) |
| GuideRegression, DialogueRegression (built from their `.csproj` lists into `Temp/L3Repair`) | 953 PASS, 1166 PASS |
| CampaignFoundationRegression | **Inherited compile failure**: `CampaignRewards.cs` needs `Level3DeliveryRules`, not in that suite's source list (R-BASELINE-TESTS). Its two pins on T03 files (CameraController InputHeld/blocker/desk order; FirstPersonCameraController.SetLook) verified directly: unchanged |
| `VerifyMobileInput.py` | all PASS |
| `VerifyInteractionOutlines.py` | **Inherited**: stops at `Assets/Settings/PC_Renderer.asset` (untouched since 3 Nov); its Interactor outline contract verified directly |
| `VerifyGuideTargets.py`, `VerifyShopAndGuideView.py` | 14 paths/0 missing; PASS |
| `--suites --t02`, `VerifyT02Pilot.py` | 64/64, 114/114 |

### Gaps and blockers (exact)

- **Animation:** no compatible run, jump/airborne or landing clip exists for Elijah's Humanoid rig (Elijah has walk only; shared NPC idles; no DCC tool). Sprint shows the walk at a faster cadence; in the air the body holds its last walk pose. Both are **not final**; owner: E-CONTACT/animation sourcing. Turning happens while moving; there is no turn-in-place clip.
- **T02 / E-PILOT:** body art is the shipped Elijah model; smoothing adoption waits for the pilot verdict.
- **Not verified here:** Unity import, Play Mode, camera collision feel, doorway placement accuracy, body fit/foot contact, carried-paper hand pose, sprint ring/jump button placement on the phone, curb placement versus NPC/Arthur routes, touch with three fingers, both landscapes, audio, frame cost.

### USER Unity/device acceptance steps

1. Continue or start Level 1 to the plaza: third-person Elijah, walking/turning grounded; drag the right side to orbit; walk the camera against walls/trees (it pulls in, never shows through).
2. Push the stick to its rim: ring lights, Elijah speeds up; release or open Settings: sprint stops. Tap JUMP (also while moving and looking): one hop per tap; hop the yellow-edged curb or walk the flat route beside it; hold JUMP: no repeat.
3. Walk into the office door: one blend to first person after the threshold; stand on the threshold and shuffle: no flicker; walk back out: one blend to third person.
4. Talk to an outdoor NPC (one Elijah in the shots), end it: back to the same third-person view; in the office sit at the desk and exit: first person with hands. Try Back, Settings, Alt-Tab/focus loss and quick repeated taps at each step.
5. Outdoors try to tap an NPC/object through a wall or behind you: refused; near and visible: works. Carry a printed paper outside (if available): it rides Elijah's hand and returns indoors.
6. Fall check: step off a high edge if one exists: returns to the last safe ground with items intact. Reload: Level 1 saves no player pose, so it starts at the plaza in third person, never mid-air.
7. Record build/device/steps for G2, G3, G8 in VERIFICATION. Next ticket waits for the user (T04/T05 not started).
