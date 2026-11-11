# Shared cinematic production and handoff standard

Required by all [four tickets](README.md). Scope is five short new scenes, not a new cinematic framework or a rewrite of the existing intro.

## 1. Creative target

Match `Cutscene2.0`'s low-poly characters, office/town identity, authored camera storytelling, moving props and atmosphere. Improve through deliberate composition, eased motion, clear character attention, restrained editing and readable subtitles. Do not substitute a text slideshow, frozen-character orbit or a generic UI dialogue window for a cinematic.

Each scene communicates one human moment and one transition. No exposition dump, new tax lesson, forced clue answer or repeated client handover. Keep essential comparisons/decisions/delivery playable. Basic walking/turning/idle/prop interaction is enough; facial animation and lip-sync are not required.

## 2. Reference and available implementation context

Inspect before implementing:

- `Assets/Scenes/Cutscene2.0.unity`: visual set, camera framing, models, text/audio arrangement. Observe through user-supplied playback if available; do not claim it has been watched from YAML inspection.
- `Assets/EnvTimeline.playable`: actual available authored Timeline, animation/activation/audio tracks. A second intro Timeline reference has been documented missing. Do not copy missing bindings or try to reconstruct the old intro inside this feature.
- `Assets/Scripts/Cutscenes/Cutscene01Runtime.cs`: `BuildSkipButton`, `ApplySafeArea`, fades and finish guard. Its `Awake` clears legacy subtitle lines, so it is **not** an existing subtitle-overlay player to reuse unchanged. It also writes intro-viewing state and loads Office_Level1, which new scenes must not inherit.
- `Assets/Scripts/Cutscenes/CutsceneSubtitleTrack.cs`: assess reusable data shape without editing the legacy intro track in place.
- `Assets/Scripts/UI/HUD/SceneFade.cs`, `HudTheme`, `Assets/Scripts/AudioManager/AudioManager.cs`: shared appearance, transitions/settings.
- Current installed context at ticket creation: Unity `6000.4.5f1`, Timeline `1.8.12`, URP `17.4.0`, Input System `1.19.0`. Reinspect rather than upgrade. Cinemachine is not listed in the inspected package manifest, despite a brain reference in the intro. Do not add it merely to animate a camera; a dedicated camera and transform tracks suffice.

Use a short asset manifest per scene: character/prefab path, rig/avatar, reused clips, props, camera/timeline and local overrides. Candidate assets include Elijah idle/walking FBXs under `Assets/3D Models/npc_elijah`, Arthur's existing tutorial model/idle, current Marie/Rosa/Bea actor appearances, receptionist model, `Assets/AssetIntegration/NpcHumanoid.controller`, existing folder/book/desk models. These are **inspection candidates**, not proof every model supports every gesture.

## 3. Exactly 15 seconds

- Every new scene has a fixed **15.000-second presentation**, including local opening/closing fades; no extension for alternate lines. This supersedes earlier 10–15 or 15–20 suggestions for these five scenes only.
- Existing Level 1 intro retains its duration/content and first-view Skip behavior.
- Author three shots: **0.00–4.80**, **4.80–10.10**, **10.10–15.00** seconds. Adapt composition, not the story/timing budget, unless documenting a concrete improvement within the same 15 seconds.
- Fade in 0.00–0.25; fade out 14.75–15.00. Dialogue never starts under black or extends beyond the last readable frame.
- Use a fixed 15-second master presentation owner, not the longest random director or music-tail duration. Finish naturally once at t=15; shorter Skip is intentional.
- Speech must fit its cue with at least 0.25 seconds of tail room at pitch 1. If a native startup delay leaves speech unfinished near cue end, hold the picture before the subtitle fade until the take ends; Skip bypasses this safety hold. This can add wall-clock delay on a slow audio device, not extra authored presentation time. Do not seek past words or cut to another shot mid-line.
- The scene's content interval is distinct from asynchronous load/unload latency. Loading must not cause a visible extra black hold or an extra second of authored content; report transition cost separately.
- Pause on application suspension and resume from the same presentation time. Background wall time must not skip unread subtitles. Do not advance the gameplay review clock during a cinematic.

## 4. Shot craft

- Three purposeful compositions: relationship/context, human response, meaningful prop or transition.
- Mostly medium/two-shots and one readable insert; no extreme facial close-ups exposing static rigs.
- Each moving shot has one primary move: a short dolly/push, controlled lateral slide or gentle pan. Set eased start/end tangents. Hold enough to let the action register.
- A reasonable starting setup is a moderate perspective lens around 40–50 degrees vertical FOV, adjusted to the actual set; this is a target, not a mandatory universal lens. Keep FOV steady within a shot. Avoid rapid zooms, constant orbit, shake and sweeping rotations.
- Maintain eyelines, screen direction and geography. Do not cross the conversation axis accidentally. Cut to a new composition intentionally; do not blend a camera through a wall or table.
- Use the original office's warm/daylight balance. Read characters against uncluttered backgrounds. No new expensive global post-processing, excessive blur/bloom or forced tiny letterboxed image.
- Make props and key text readable at intended phone scale. Show only a title/footer needed for the story, not a full tax page to read in 3 seconds.
- Static props need not move constantly. One small physical event—folder placement, paper settling, closing a sketchbook—can carry the beat.

## 5. Models and animation

- Reuse existing character identities and clips. Resolve current live appearance, not outdated model notes. Reuse low-poly office/environment props in a small dedicated set; do not copy an entire gameplay scene and its managers just for a background.
- Marco may reuse an available male model not prominently identifiable as Arthur, Elijah, Auditor or another required speaker. Record the chosen identity and any local clothing/material override; do not invent a new model asset or change a shared source. Preserve his reserved demeanor.
- Check humanoid compatibility/root motion before using a clip. Shared NPC controllers do not guarantee talking, pointing, handover or facial clips; the current driver has procedural fallbacks. Do not refer to nonexistent states as implemented animations.
- Add only necessary scene-local camera/prop/actor-root curves and small compatible pose adjustments. If no clean hand-contact gesture exists, shoot the placement from an angle that does not demand precise fingers. A cut may conceal a transition, not a floating folder.
- Match walk speed to clip displacement; avoid foot sliding, floor penetration, snapped turns, intersecting hands and twitchy procedural motion.
- One owner per animated transform. On cinematic instances, disable competing gameplay motion/AI drivers; never animate a live saved client to manufacture progression. Use existing idle/walk with short blends as compatible. Do not alter shared controllers for this pack.
- End natural/skip/abort paths cleanly. Restore only state owned by the cinematic. No lingering camera, audio listener, UI, input hold or actor driver.

## 6. Subtitles

Build a normal screen-space subtitle overlay using existing TMP/font/theme resources; **not world-space story text alone**. All story lines below are subtitle-first and must make sense with sound muted. Supplied prerecorded speech is now authorized (6 November 2026). Keep the existing subtitle windows; voice IDs and matching clip catalogues supply optional offline speech. Missing/stale/oversized clips fall back to text.

- Bottom center within `Screen.safeArea`, with generous margins. Reserve space so essential props/actors do not sit behind captions.
- White/off-white readable type, modest shadow/outline and a **semi-transparent dark background panel**. Starting target: black alpha 0.65–0.75 with about 20–28 reference-pixel padding. Fit to text or use a restrained consistent width; no opaque full-width dialogue box.
- Maximum two lines at once, one speaker per cue. Show a compact speaker prefix/name so static faces do not make attribution ambiguous.
- Starting font about 34–40 at 1920×1080 reference; scale appropriately. Do not auto-shrink to unreadable text. Test current portrait font and special apostrophes for missing glyphs.
- Aim roughly 12–17 characters/second including spaces, generally 2–5 seconds per short cue. Ticket timings allow deliberate holds. If a cue is too long, shorten wording before changing scene duration.
- Display complete cues rather than typewriter text. Opacity ease about 0.1 seconds; count it within cue time. Keep gaps brief and avoid captions flickering across cuts.
- Default first cue **0.60–4.60**, second **5.20–9.80**; optional third **10.50–14.50**. T01/T02 may leave the last shot silent.
- No bottom objectives, gameplay portraits, document controls, emotes, result cards or tutorials competing with the overlay. Text content stays editable and timing data stays explicit.

## 7. Upper-right Skip

Use the same visual construction/resources as `Cutscene01Runtime.BuildSkipButton`: HudTheme panel, BrassSoft Skip label and transparent whole-button hit area. Inspect its current values: upper-right pivot, reference offset (-32,-32), size (200,84), label 36 at 1920×1080. Keep recognizable styling; adapt margins for safe areas and usable touch size.

**All five new scenes show Skip on the first viewing and later viewings.** The user's explicit request applies here. Do not copy the old intro's seen-flag gate; preserve that gate for the original intro alone.

- Visible/tappable after opening fade, always within the upper-right safe area; not behind subtitles or fade at steady state.
- One tap requests the same finish outcome as natural completion. Disable repeat activation immediately, then fade from the current opacity and hand control to the caller.
- Skip is cosmetic completion, not a case-completion command. It never grades, awards, delivers, checks evidence or bypasses an unsaved prerequisite.
- No second confirmation dialog. Handle Android Back consistently as Skip for these short scenes if the existing input policy permits; document chosen mapping and prevent the underlying menu handling the same press.
- Initial black/full-screen fade may block input while opaque; transparent steady-state curtain must not swallow all Skip taps.

## 8. Sound

Use existing quiet office/town ambience, soft paper cues and appropriate existing music only. At most one paper cue per placement, no constant whoosh for cuts, no ominous sting on Bayview/CoastCare. Silence can support suspicion without declaring guilt.

Honor existing Music/Effects settings and any established dialogue setting. Prevent duplicate level music/audio listeners and clean up on all exits. Cutscene runtime must coordinate with AudioManager rather than setting a permanent global volume or leaving ambience muted. No runtime voice-generation dependency. User-supplied clips play locally through one scene-owned 2D speech source, independent of Effects. Voice pauses with presentation and stops on release; Skip fades the current line and starts no further line.

## 9. Minimal shared presentation contract

T01 delivers only the small reusable player needed for these scenes: duration/clock, timed subtitles, matching Skip, fades, read-only presentation context and a once-only completion outcome. Reuse existing data/helpers where genuinely appropriate; no service locator, generalized dialogue engine or parallel save framework.

Completion reasons should distinguish natural end, skipped and aborted; natural/skip advance the same caller continuation once, abort does not falsely declare a watched result. Standalone preview may return to a finished state rather than loading gameplay. Presentation never calls `ApplyLevelResult`, client handover commands or intro-seen writes.

Author scenes/Timelines with stable bindings, fixed duration and editor-visible shot structure. No scene-name guessing or scanning every frame for actors/cameras. No global timeScale workaround; use owned clock/input/pause coordination at integration.

## 10. Verification and evidence

Internal: static scene/asset/GUID/binding inspection; compile current relevant source with existing harness; verify timing bounds, subtitle length, variant IDs, once-only finish/skip, cleanup and unavailable-reference behavior using meaningful tests. Avoid implementation-mirroring tests or repeating broad suites without a changed risk.

User manual: Unity import and Play Mode, rendered composition/animation/contact, real Skip taps, captions with sound off, safe areas/aspect ratios, focus loss, transitions/audio and phone performance. User review must answer whether scenes tell the intended story and match/improve the intro's quality. Do not mark visual boxes complete from serialized values.

Every ticket must record actual paths/commands/results, known limitations and pending manual checks. A missing rig/clip/binding is a named blocker or an intentionally revised shot using existing assets; placeholder art is not a polished final scene.

## Primary technical references

- [Unity Timeline: blending clips](https://docs.unity3d.com/Packages/com.unity.timeline@1.8/manual/clip-blend.html) explains overlap/blend curves; inspect APIs for the installed 1.8.12 package rather than assuming every latest 1.8 feature.
- [Unity Screen.safeArea](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/screen/safearea) describes the safe rectangle used for notch-aware overlay placement. Existing intro safe-area code is the local starting reference.

Numerical cinematography/type/timing targets above are this pack's production choices, not claims of universal industry certification.
