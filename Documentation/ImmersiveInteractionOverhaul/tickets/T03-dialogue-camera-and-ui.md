# T03 - Dialogue camera and UI

Status: **ALL FOUR CHECKPOINTS IMPLEMENTED — internal checks passed; user Unity setup and acceptance pending.**
Dependencies: T02 shared conversation lifecycle (code-complete; its manual acceptance is also still pending). Next: T04 after user acceptance of T02 and T03. The user authorized the whole ticket in one go ("do ticket 3", internal tests only), so the four checkpoints were delivered together instead of one approval at a time.

## What exists now

Normal exploration is unchanged and first person. Only an admitted NPC conversation:

1. **Shows a standing, grounded Elijah** (`ConversationActorVisual`, checkpoint 1). The existing `npc_elijah_Idle.fbx` model is instantiated at runtime with its colliders stripped, posed by sampling its idle clip (no Animator controller asset is needed), and optionally looped as a subtle idle capped at 30 Hz. Scale and foot offset come from the posed model's measured geometry and a chosen standing height in world units, so the lowest posed point lands exactly on the floor. It is hidden outside conversations and while the camera is near Elijah's head. The player root, Rigidbody, capsule, NPC transforms and FBX importers are never changed, and the walking model is deliberately not used.
2. **Frames shot / reverse shot** (`DialogueCameraDirector` on the CameraController object, checkpoint 2).
   - NPC lines frame over Elijah's right shoulder at the NPC; player lines frame over the NPC's left shoulder at Elijah.
   - Both shots stay on the same side of the axis (the 180-degree rule), so Elijah stays screen-left and the NPC screen-right, as in the reference frame.
   - Distances scale with character height.
   - Each shot is tested for walls and furniture: a camera clearance sphere, a clear line from the foreground shoulder and a clear line to the speaking face. A blocked shot shortens its pull-back (100/75/50%), then falls back to a two-shot on the same side, then the opposite side, and finally a first-person look-at.
   - Elijah is placed visually at the player's floor point, clamped to a readable 0.5–0.85 × height from the NPC. That placement is visual only; the gameplay player never moves.
   - NPCs are located from an optional `DialogueSpeakerAnchor`, otherwise from their visible renderer bounds, otherwise from their colliders. No NPC contract or animation was added.
3. **Returns exactly and in order** (checkpoint 2 + T02 handoff).
   - On entry the director captures the logical first-person view from `FirstPersonCameraController` (`TryGetViewPose`), plus the FOV, the hands and the carried document.
   - On natural end or cancel: the panel hides → the camera blends back to that exact view → hands and document are restored → Elijah hides → **only then** does `DialogueUI` release the controls and run the completion or cancellation callback (`DialogueTerminalHandoff`).
   - While returning, the conversation still counts as active, so nothing else can start.
   - `AcquireDialogueControls` now also freezes the player Rigidbody (constraints and velocity only) for the whole conversation and restores it on release.
   - If the director is disabled or the scene unloads, the view is restored immediately and a pending return completes at once. The director never restores over another camera mode.
4. **Compact bottom panel and typewriter** (checkpoint 3).
   - Text is revealed progressively by TMP visible character, so rich-text tags are never typed. Sentence pauses apply only at real breaks: "3.5" does not pause and "..." pauses once.
   - Box taps (`DialogueBoxTapRelay`), Continue, submit and `AdvanceDialogue()` all go through one `DialogueAdvanceGate`. A gesture while typing reveals only that line, and a distinct later gesture advances or closes.
   - The opening frame plus a 0.15 s grace, same-frame events, and gestures less than 0.12 s apart are rejected, as are drags and long presses that aren't taps.
   - The setup tool restyles the existing panel: an invisible full-width strip that absorbs taps; a dark box 76% wide and 250 high on the 1920×1080 reference, inside the safe area via `SafeAreaBottomPanel`; the speaker name top-left in amber; auto-sized 22–32 text; Continue bottom-right. The oversized portrait object is hidden, but it and its `DialogueUI` reference are kept.
5. **Integration** (checkpoint 4). All office and town callers already go through `DialogueUI.TryStartDialogue`, so every route — receptionist, colleague, client interview and presentation, auditor, the four taxpayers, and both guide conversations — gets the same presentation with no per-NPC fork. Content, callbacks, outcomes and replay rules are untouched.

Without the setup tool, or with the director removed, conversations behave exactly as in T02, plus the typewriter.

## Files

New runtime files:

- `Assets/Scripts/UI/`: `DialogueTerminalHandoff.cs` (also defines `IDialoguePresentation`), `DialogueAdvanceGate.cs`, `DialogueRevealSchedule.cs`, `DialogueBoxTapRelay.cs`, `SafeAreaBottomPanel.cs`.
- `Assets/Scripts/Player/`: `DialogueShotMath.cs` (pure `System.Numerics`), `ConversationActorVisual.cs`, `DialogueSpeakerAnchor.cs` (also defines `DialogueSpeakerFrames`), `DialogueCameraDirector.cs`.

New Editor file: `Assets/Editor/DialoguePresentationSetup.cs`, which adds two menu items:

- **Tools > TaxVenture > Configure Dialogue Presentation**, with Undo. It measures the posed Elijah by CPU-skinning every vertex from its bones and bind poses, measures every NPC in the scene, proposes the median NPC height, creates the visual, adds the director, restyles the panel and logs a report.
- **Tools > TaxVenture > Toggle Conversation Visual Preview**. It stands an unsaved Elijah beside the player facing the selected or nearest NPC, for inspecting grounding, scale and facing.

Modified files:

- `DialogueUI.cs`: typewriter, gate, presentation calls and terminal handoff. Serialized field names, the Continue binding and the public API are unchanged. `AdvanceDialogue()` is now "one gesture"; it had no external callers.
- `CameraController.cs`: +87 lines, all additive. Read-only accessors plus the conversation Rigidbody freeze.
- `FirstPersonCameraController.cs`: +18, `TryGetViewPose`.
- `FirstPersonHands.cs`: +6, `IsShowing` and `IsShowingCarriedDocument`.

No scene, prefab, importer or data file was edited by the agent. Unity will generate `.meta` files for the 10 new scripts on import.

## Internal evidence

- **PASS: full-project compilation against the real Unity 6000.4.5f1 assemblies.**
  - The user granted read access to the installed `Editor/Data/Managed` and `Editor/Data/NetStandard` folders.
  - The project's own `Assembly-CSharp` and `Assembly-CSharp-Editor` source lists, defines (LangVersion 9) and references were taken from its generated `.csproj` files, together with the compiled package assemblies in `Library/ScriptAssemblies`.
  - The unmodified project compiles with **0 errors** (runtime and Editor), which validates the harness.
  - With T03 swapped in, **0 errors** again, with only the baseline's 2 pre-existing warnings.
  - Limit: the Editor assembly used the .NET Standard 2.1 facades rather than Unity's .NET Framework 4.7.1 profile, which was not shared.
- **PASS: 610 production contract assertions** (`Tests/DialogueRegression`): 311 carried over from T02 plus 299 new ones.
  - Handoff: ordering is hide → return → release → outcome, with the opposite callback never running, once only, stale and duplicate completions ignored, re-entrancy and forced teardown.
  - Gate: opening grace and frame, same-frame and near-duplicate events, reveal-before-advance. A rapid-tap walkthrough over three typed lines shows every line, none skipped, with a separate close and exactly one completion.
  - Typewriter: rate, sentence and clause pauses, decimals, ellipsis, closing quotes, no pause at the end of the line, rich text scheduled by visible characters only, buffer growth, RevealAll and Clear.
  - Shots: 24 height/direction configurations — cameras behind the correct shoulder, same side of the axis for both OTS shots and the two-shot, Elijah screen-left of the NPC in both shots, the speaking face within 15° of frame centre, the shoulder within 50°, a correct two-shot, tighter pull-back closer, scale invariance and no NaN for coincident heads. Placement covers too close, too far, in range, coincident and swapped limits.
- **PASS: scoped diff.** The on-disk content of all 17 written files hash-matches what was compiled and tested. Line endings match each file's existing convention (`DialogueUI.cs` and `FirstPersonHands.cs` are CRLF on disk), and content-only diffs are as listed above.
- **Measured assets (static inspection of the binary FBX files):**
  - All three Elijah FBX files are in centimetre units with Y up and Z forward.
  - The character is a stylised armless/legless figure whose coat reaches the floor, about 6.7 m tall at import scale. That is why the fit uses a measured, world-unit standing height instead of the raw scale.
  - The idle FBX has a Mixamo hierarchy and a 7.57 s idle clip, and is imported as Generic with no avatar.
  - The scene holds two inactive leftover Elijah instances from the abandoned third-person setup, at scale 0.53. They were left untouched.

**NOT RUN by agent:** the setup menu, Edit-mode preview, Play mode, shots, the typewriter, taps, return and input restoration, Android, safe areas or any device testing. Compilation and assertions are not gameplay acceptance.

## User setup and manual acceptance matrix

Setup (Edit mode, `Office_Level1` open):

1. Let Unity compile and confirm the Console has no errors.
2. Run **Tools > TaxVenture > Configure Dialogue Presentation**. Read its Console report: Elijah's native height, head fraction, facing yaw, the standing height it proposed, and each NPC's measured height. An NPC whose height is far off its neighbours probably shares a parent with props; give it a `DialogueSpeakerAnchor` with a head transform.
3. Run **Toggle Conversation Visual Preview**. Elijah should stand on the floor, face the NPC and be about NPC height. If he faces away, add 180 to `forwardYawOffset` on `ConversationElijahVisual`. If his size is wrong, change `standingHeight`. Run the menu again to remove the preview, then save the scene.

Play matrix:

- Exploration before and after any conversation looks and moves exactly as before, and Elijah is never visible outside a conversation.
- For a receptionist, colleague, client interview and presentation, auditor, each of the four taxpayers, and the guide intro and final dialogue:
  - NPC lines show the NPC's face over Elijah's shoulder.
  - Your lines show Elijah's face over the NPC's shoulder.
  - Elijah stays screen-left in both.
  - His feet are on the floor.
- Tight spaces (an NPC near a wall or behind a desk): the shot pulls in or becomes a side two-shot, never shooting through geometry.
- Typewriter:
  - Tapping the box while typing completes only that line; the next tap advances.
  - The last line needs its own tap to close.
  - The opening click never skips line 1.
  - Hammering taps, tap plus Continue, holding a finger and pressing Enter repeatedly never skip a line.
- Rich text and punctuation in lines read correctly.
- On end and on cancel (deactivate the `DialoguePanel` GameObject mid-line): the camera glides back to exactly your previous view, FOV and hands; MOVE and swipe-look stay frozen until the glide ends; and the outcome happens after that (for example the profile paper drops after the return, not before).
- The panel stays within notches and home bars on a phone, and is readable in portrait-safe landscape at 16:9, 19.5:9 and 4:3.

Tuning lives in the Inspector: `DialogueUI` (characters per second, pauses, gesture grace and interval), `DialogueCameraDirector` (distances, shot FOV, blend times, obstruction mask), `ConversationElijahVisual` (height, facing, idle loop).

## Revision 1 (user feedback, 2026-10-15)

- **Instant cuts.** Every camera change is now a cut: first-person → first shot, shot → shot, and back to first person. The smooth versions are kept **commented out** in `DialogueCameraDirector.ShowLine` (`StartBlend(...)`) and `BeginReturn` (four lines), with instructions to restore them. The blend durations stay serialized under a `#pragma` so they are ready. With the instant return, controls and outcomes now follow the end of the conversation immediately.
- **Wider side angle.** The defaults are now back 0.50, side 0.40 and framing bias 0.12 (were 0.55, 0.24 and 0.18), putting the camera about 38° off the line between the characters instead of about 24°. From the camera, the two heads are now about 20° apart instead of about 12.7°, so the listener no longer hides the speaker. Scenes saved with the old defaults are migrated on load (`settingsVersion`); values the user changed by hand are kept.
- **Dialogue backdrop.** `DialogueUI` adds a rectangle directly behind the dialogue box at runtime: white at 85% opacity, 10 units of padding, with a soft shadow. It uses the box's own sprite, ignores taps, and hides with the panel. The box, fonts and colours are unchanged. `showBackdrop`, `backdropColor`, `backdropPadding` and `backdropShadow` are on `DialogueUI` in the Inspector.
- **Close-ups.** New `DialogueCloseUpPolicy` (pure). A solo close-up puts the camera 0.3 × height in front of the speaker's face, on the same side of the axis, with the listener behind the camera. It is used only when the listener is really out of frame and the face is unobstructed.
  - It triggers on lines that land: exclamations, shouted words of 4+ letters (acronyms such as BPO do not count), very short lines ("Wow.", "...Right."), interjections, dramatic ellipses, short questions and weighty words (important, penalty, excellent, sorry…).
  - Borderline lines (such as "Yes.") get one 15% of the time.
  - Never on the first line, and at least 2 lines between automatic close-ups.
  - Intentional control: `DialogueBuilder.CloseUp()` / `.OverShoulder()` mark the previous line. `DialogueLine.CameraHint` survives the session snapshot.
  - Tunable on the director: `enableCloseUps`, distance, side offset, threshold, random chance and minimum lines between.
- Evidence:
  - Full Unity 6000.4.5f1 compile: 0 errors, baseline warnings only.
  - **757** contract assertions pass (+147). They cover the head separation of at least 18° in all 24 configurations, close-up framing, side of the axis and the listener being out of frame, scores for real game lines, pacing, hints, a random rate around 15% that is reproducible per seed, and the hint surviving the snapshot.
  - On-disk files hash-match the tested copies.
  - Not run in Unity.

## Acceptance criteria

- [x] First-person exploration and T01 controls unchanged outside dialogue; shots and Elijah only during an admitted conversation. *(code; user gameplay pending)*
- [x] Standing, grounded Elijah from existing assets; no collider resize, root move, importer change or NPC animation. *(code + measured geometry; visual acceptance pending)*
- [x] NPC lines over Elijah's shoulder, player lines over the NPC's shoulder; two-shot only when blocked. *(geometry unit-tested; in-scene framing pending)*
- [x] Movement and look frozen throughout; return to the captured yaw, pitch and FOV; hands restored; input and outcomes only after the blend. *(handoff unit-tested; native behaviour pending)*
- [x] Every office and town route uses the shared path with content and outcomes preserved.
- [x] Compact bottom safe-area panel without the oversized portrait. *(setup tool; on-device readability pending)*
- [x] Progressive reveal; separate reveal and advance/close gestures; opening, held and duplicate input cannot skip. *(gate and typewriter unit-tested; native touch pending)*
- [x] Panel input cannot reach movement, look or interaction; teardown cleans up and stale events are ignored.
- [ ] **User Unity setup and the manual matrix above.**

## Known risks to watch

- Elijah's facing is derived from his face meshes relative to his head. A wrong sign shows up immediately in the preview and is fixed with `forwardYawOffset`.
- Sampling a Generic clip without an avatar is expected to work, but has not been observed in this project. If he T-poses or freezes oddly, untick `animateIdle`, or switch `modelPrefab` to `npc_elijah_static`.
- NPC framing from renderer bounds can be thrown off by attached props; `DialogueSpeakerAnchor` fixes that per NPC.
- Releasing controls and running outcomes now waits for the return (0.45 s by default). That is intended, and the blend times are adjustable.
- `Office_Level2` is not configured. The setup menu is per scene.
- The compile check left two archives in the project's `Library` folder (`T03CompileCheckBundle.tar.gz`, `T03CompileCheckPackages.tar.gz`, about 22 MB). `Library` is Unity-managed and ignored by git; they can be deleted.
