# Level 1 Living Plaza

Makes Level 1 (`Assets/Scenes/Office_Level1.unity`) feel inhabited. NPCs idle naturally, walk where the story takes them, sit, browse and chat. Four ambient townspeople are added, and the tax code book and case folder page turns are fixed. Started 2026-10-27 at the user's request. Status and verification are in `PROGRESS.md`; limitations are in `KNOWN_ISSUES.md`.

## What the player sees

| Who | Before | Now |
|---|---|---|
| **Marie** (side-quest client) | Stood still. Teleported to the conference room when nobody looked. | Strolls a short route on a phone call until the rounds open (her "just finishing a call" line). After the side quest she walks through the opened front doors and waits at the front desk. Once the receptionist has handed her case over, she walks to the conference room. If the hand-over already happened, she walks on without waiting. Tapping her on the way gets one friendly line, then she carries on. |
| **Jhun** (side-quest client) | Stood beside a bench. | Sits on that bench. When you come close and he has something to say (his marker shows), he stands up and turns to you. He sits back down after you leave or after his talk. |
| **Milo, Zorbix** | Idle. | Wander on separate looping plaza routes, pause to look around, and periodically meet for a short wave before continuing independently. A player conversation interrupts the walk. They wait while their profile handover is pending, then resume after collection; later taps offer optional small talk. |
| **Arthur** | Static T-pose mesh. Teleported to the corkboard after the client round. | His rigged twin (same mesh, Humanoid) plays his briefcase idle. After his reviewed file is collected he walks from his start marker to his corkboard spot. A resumed run finds him there. |
| **Receptionist, Auditor** | Static T-pose meshes. | Arms lowered with a slight elbow bend (runtime arm rig), plus a small forearm gesture while they speak. They stay at their posts. |
| **Four new ambient NPCs** | None. | See below. Their small talk is optional, replayable and talk-only. |

### The four ambient NPCs

| Name | Role and spot | Personality | Routine | Model (unused Synty office cast) |
|---|---|---|---|---|
| **Aling Lorna** (Lorna Dizon) | Sari-sari store owner buying receipt booklets and a ledger at the **Supply Counter** (customer side, at the end away from the crates) | busy | Browses the counter: looks over the goods and now and then reaches for something | `SM_Chr_Developer_Female_02` |
| **Mang Dante** (Dante Ramirez) | Security guard of the office building, starting on the **forecourt east of the entrance** | reserved | Visits the other eight plaza characters in order, briefly chats, then walks to the next. Busy characters and people who have moved out of the plaza are skipped. | `SM_Chr_Security_Male_01` |
| **Bea** (Beatriz Mendoza) | Freelance graphic artist, standing at the **plaza bench across from Jhun's** | friendly | Chats with her cousin Carlo: they face each other and take turns talking and listening | `SM_Chr_Developer_Female_01` |
| **Carlo** (Carlo Navarro) | Delivery rider on his break, **seated on that bench** | mildly dismissive | Sits and chats with Bea. When you talk to him he stays seated and only turns his head. | `SM_Chr_Developer_Male_02` |

None uses a protected character's model or gives a profile/ID, EXP, coins or quest progress. After Arthur opens plaza exploration, all townspeople use the same conversation markers and minimap person symbols. An ambient person's triangle becomes a check only after a completed talk; cancelled talks remain available, and optional small talk stays replayable. Ambient conversation checks last for this scene visit and are not added to campaign saving.

## How it works

### One behaviour owner per NPC: `NpcLife` (FSM/NPC)
Every walking or sitting NPC has an `NpcLife`. It follows one order of priority, so movement, animation, dialogue and quests never give conflicting commands:
1. **Quest travel** (`TravelForQuest` / `PlaceForQuest`): a required move owned by quest code (`ClientConsultation`, `TutorialNpcController`). Nothing ambient can cancel or redirect it. It always ends at its destination, either on foot or, if the route fails, through a move made only while neither the NPC nor the spot is on screen.
2. **The player's conversation with this NPC.** The activity pauses on the spot: a walk stops (quest walks resume afterwards), and a seated NPC that stands to talk gets up. The NPC turns smoothly to the player, then resumes after a short beat.
3. **Activity**: `Stand` (optionally on a `seat`, optionally chatting with a `partner`), `Patrol` (a route, stroll pace, pausing at each point), `Browse` (a counter, with occasional reaches), or `Visit` (an ordered round of nearby plaza characters).
4. **Idle**: the clip plus `NpcAnimationDriver`'s own idle variation (desynchronised per NPC).

A side-quest client pauses while its profile handover is pending (held, dropping, waiting for collection or collecting), because its profile paper is parented to its root. Once collected, a configured patrol can resume. Quest-owned moves still take precedence.

Milo and Zorbix share a meeting marker but have separate routes. Their meeting is bounded by a timeout and a cooldown; player dialogue, quest travel or disabling either actor releases both participants. Mang Dante uses the same social pause ownership for visits, so he cannot interrupt their greeting. These ambient chats never open a dialogue window or award progress.

### Walking: `NpcLocomotion` (FSM/NPC)
- The scene has no NavMesh. Each leg of a route is planned once, when it starts, with the project's physics-grid A* (`GuideGridPath`, the guide arrows' planner). It is replanned only when the NPC is stuck, never per frame.
- Routes are authored stops. Each door gets a point just outside and one just inside, so doorways are crossed straight through the gap.
- Speed comes from the walk clip's own ground speed (`NpcAnimationDriver.AnimatedWalkSpeed`, scaled to the avatar), so feet don't slide. Root motion is off. Large turns pivot in place first, and the NPC only moves forward. The root keeps its height above the ground under it.
- If the player or another NPC is close ahead, the walker slows and waits, then takes a small step aside. It never pushes through anyone.
- Failure: a leg that can't be planned, or stays stuck after two replans, ends with `done(false)`. The owner then uses `StageWhenUnseen`.

### Poses with no clips: `NpcBodyPose` (FSM/NPC)
The project has idle and walk clips only. Sitting, reaching and holding a phone are procedural layers on top of the playing clip, applied in `LateUpdate` through the Humanoid avatar's bones:
- **Sit**: the pelvis settles onto the seat, two-bone IK keeps the feet planted, the hands rest on the thighs, and the chest leans forward while rising.
- **Reach**: one hand toward a point.
- **Phone**: the right hand held at the ear.

Each layer eases in and out, and an interrupted sit simply eases back from where it is. Models without a Humanoid avatar report `Supported = false` and stand instead.

### T-pose fixes
- **Arthur**: `NPC_TutorialGuide/Visuals/npc_tutorial_static1 (rigged)` is an instance of `npc_tutorial_static1@Standing W_Briefcase Idle.fbx`, the same mesh with a Humanoid avatar. `TutorialNpcController.visualRoot` points at it. The static copy stays in the scene but inactive.
- **Arthur's height**: startup placement measures a baked snapshot of his skinned geometry, rather than the animation's visibility bounds. The measured root-to-sole offset is retained by the mover. Restoring his corkboard pose uses the same grounding routine. This fixes the placement code behind the reported floating; visual confirmation still requires Play Mode.
- **Receptionist and Auditor**: `StaticArmRig` (FSM/NPC) measures the T-pose mesh at start, builds a tiny skeleton (body, upper arm and forearm per side), skins a runtime copy of only the arm-holding parts (torso, blazer, sleeves), and lowers the arms. Head parts stay ordinary meshes, so the existing head motion still works. The model assets themselves are never modified; both imports now allow Read/Write so this also works in builds.

### Animation assets
`Assets/AssetIntegration/NpcHumanoid.controller` is one shared Humanoid controller. Its states:
- `Standing Idle` (Jhun's clip)
- `Idle Relaxed` (the Cora clip Marie already played)
- `Briefcase Idle` (Arthur's)
- `Walking` and `Strolling` (Elijah's walk, 1.0x and 0.8x)

The walk clip is imported without loop time and the cutscenes share it, so the controller loops it with an exit-time self-transition instead of changing its import. Each NPC picks its idle with `NpcAnimationDriver.idleStateName`.

### Marie's trip: `ClientConsultation` + `MarieTripRules` + `MarieOfficeRoute`
- `ClientConsultation` stays the one owner of her move. `MarieTripRules` (pure, unit-tested) decides each leg from conditions read every frame: side quest done, front doors open (`OfficeDoorGate.IsClosed`), receptionist hand-over done (`ReceptionistInteractable.HasSpokenFor`). An event that already happened can never strand her.
- Legs: `Plaza` → `ToReception` → `AtReception` (1.5 s check-in) → `ToRoom` → `InRoom`. Arriving in the room only sets her presence (visits enabled, stand-in hidden, checkpoint saved). It is never quest progress.
- **Resume**: nothing new is saved. Once the checkpoint has restored everything, including the receptionist, she is placed (no walk) in the room if she was there or the hand-over is done, beside the front desk if only the side quest is done, and otherwise she stays in the plaza.
- `MarieOfficeRoute` (on `Level 1 NPC Life/Marie Route`) holds the authored points: entrance outside and inside, reception wait (its +Z faces the receptionist), inner door north and south, and conference door outside and inside. The room spot is still the case's `clientFinale.spotPath`.

### Arthur's walk: `ArthurOpening` + `TutorialNpcController`
When his reviewed file lands in the Bag (`OnPickupLanded`), `TutorialNpcController.WalkToCorkboard()` sends him to his scene-authored corkboard pose. Arrival changes nothing: the board still opens from his talk once all four profiles are in. `ReturnToCorkboard()` (instant) remains for resumed runs, for practice mode (he appears after the profiles), and for any run where he never walked.

### Ambient NPCs: `AmbientNpcInteractable` + `PlazaLifeContent` (SideQuest1)
A tap within reach starts the next small talk through the shared `DialogueUI`, rotating through each character's talks. Content is in `Assets/Resources/Levels/Level_01/plaza_life.json`; a missing or broken file falls back to a short built-in line. After the plaza opens, `Availability` is `Available` until one completed talk, then `Completed`; replayable small talk gives no quest progress.

### Page turns: `PageTurnStage` + `PageTurnMath` (UI/Books)
- Destination contents and page index commit only when the turn settles. The earlier version rendered the new page underneath immediately, which exposed its print before the animation ended.
- Forward: the current print turns away over a blank backing. Backward: a blank leaf returns over the current print. The destination's contents are not built during either motion.
- The moving leaf never mirrors print onto its reverse. Copies are graphics only and cannot receive touches. Attached records follow the source surface until covered, then appear on the destination only after rendering.
- New input, closing or disabling completes the pending turn once, removes copies immediately and restores the paper before revealing the committed page. Rapid navigation first settles the pending page before choosing its next destination.
- Timing uses unscaled time and the slow-frame cap in `PageTurnMath`. Both folder and book share this implementation, including Level 2.

### Discovery and Arthur's short teaching
- The four-ID collection objective deliberately has no waypoint: no blue trail, goal beacon or minimap objective dots reveal a client or pickup. Arthur's initial waypoint and his return waypoint after all four IDs remain; later office guidance is unchanged.
- The post-file invitation suggests exploring Coast Tales and talking to people, with the possibility of something useful rather than a promise of four specific gifts.
- Arthur's pre-board introduction uses four short screens: citizen categories, foreign-national categories, flip/drag/categories controls, and the first-check medal rules. No client-to-corner assignment is given. The tutorial retains the three hands-on board prompts without repeating the medal lecture.
- CATEGORIES uses the existing `HudTheme.EaseOutBack` for a 0.26-second scale/fade entrance and a 0.18-second exit. The backdrop and `IsOpen` continue blocking board input until the exit finishes. Reversing a transition starts from the current visual state; disabling or closing the owner clears it immediately.

## Where to edit

| Want to change | Edit |
|---|---|
| Ambient names, roles, small talk, emotes; Marie's on-the-way lines | `Assets/Resources/Levels/Level_01/plaza_life.json` (see its `_readme`) |
| Where Marie walks and waits | Child markers of `Level 1 NPC Life/Marie Route` (move them outside Play Mode; gizmos draw the walk) |
| Marie's phone walk, Jhun's and Carlo's bench seats, Bea's spot, Lorna's fallback spot and focus, Milo/Zorbix's route and meeting markers, Dante's visit-area center | The other children of `Level 1 NPC Life`. A seat marker sits on the seat surface under the pelvis, +Z away from the backrest. Patrol and spot markers' +Z is where the NPC looks. |
| An NPC's activity, partner, standing rules | The `NpcLife` component on that NPC |
| Walk speeds, turn rates, stopping distance | `NpcLocomotion` (the walk clip's measured speed wins once known) |
| Sit timing, lean, hands on the lap, greeting wave | `NpcBodyPose` |
| Receptionist / Auditor arm angle, elbow bend, talk gesture | `StaticArmRig` |
| Idle / walk clips | `Assets/AssetIntegration/NpcHumanoid.controller` (state names are referenced by `NpcAnimationDriver.idleStateName`, `stateBindings` and `strollStateName`) |
| Page-turn length | `pageTurnDuration` on `CaseFolderUI` / `TaxCodeBookUI` (at least `DeskMotion.PageFlipSeconds`) |
