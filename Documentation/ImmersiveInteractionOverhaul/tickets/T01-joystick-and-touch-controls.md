# T01 — Joystick and touch controls

Status: **IMPLEMENTED — Unity setup and manual acceptance pending**
Dependencies: user approved T01. Next after acceptance/approval: T02 unified dialogue. Original estimate: **8k-14k tokens**.

## Delivered and internally verified

Single-pointer movement joystick/reset and release-frame blocking; radial dead-zone/sensitivity response; immediate logical stop with eased thumb return/press visuals; analog PlayerMovement; UI-excluded right-75%-screen swipe in the existing camera input path; fresh UI/world guards and native tap-on-release gesture discrimination; existing interaction locks; safe-area component; Undo-capable setup menu. User rejected LOOK joystick after initial setup. Revised MOVE size/handle are 280/112 and center offset (260,240), larger and further upward/inward than before. Existing paper/quest data and the user's saved scene edits remain unchanged by this follow-up.

Internal results: direct runtime C# compilation with Editor/Standalone paths passed; setup tool compiled; production JoystickInputMath regression passed **5,734 assertions**; diff check passed. Source review found center-stick drift and release-frame mouse leakage; both corrected. No Unity menu/play/device acceptance is claimed. Acceptance checkboxes below remain open until manual evidence arrives.

## User setup and manual test steps

1. In Unity, stop Play mode, open Office_Level1, and let scripts import/compile. Resolve any Console compile error before proceeding.
2. Choose **Tools > TaxVenture > Configure Mobile Controls** again. Confirm the success log, larger/inward/upward MOVE, and no visible LOOK stick. This modifies the open scene using Undo; press **Ctrl+S**. If missing references are reported, copy the exact error rather than guessing assignments.
3. Enter Play mode. For mouse UI testing press **Esc** to release the cursor if needed. Drag MOVE a little, then fully: speed should differ. Release outside its base: movement stops immediately and the thumb eases home.
4. On touch, swipe anywhere in the right 75% outside UI: look follows and stops on release. Dragging MOVE, tapping HUD or starting outside the zone must not claim camera look. Desktop mouse look remains the existing testing path; mouse is not a simulated touch-swipe test.
5. On an actual touch device, hold MOVE and swipe with a second finger, release either independently, add a third HUD tap, pause/refocus. Swipe over an NPC/paper: no unwanted dialogue/collection. A short intentional tap should still interact. Test very fast out-and-back gestures and report any false activation.
6. Test dialogue, desk, board and profile open/drag/close: movement hides/resets and swipe look is locked as appropriate. Profiles stay locked through the last paper's return. Verify intentional NPC/profile taps still work after releasing controls.
7. Check supported landscape aspects and a notched device/safe area. Rerunning setup must not recreate LOOK; old LOOK is disabled without deletion. Undo/Ctrl+S behavior remains a manual check.

Report failures with Console text, scene/device/aspect and the failing step. Do not proceed to T02 until accepted and explicitly approved. Next estimate: T02 unified dialogue **10k-18k tokens**; split into approved checkpoints if needed.

## Objective and inspected entry points

Provide a polished movement joystick and independent screen-swipe look, per the user's latest instruction. Use the existing camera touch path, not a visible LOOK joystick or a UI surface blocking world clicks. MobileLookArea remains inactive.

Primary files: `Assets/Scripts/Mobile/VirtualJoystick.cs`, `MobileLookArea.cs`; `Assets/Scripts/Player/PlayerMovement.cs`, `FirstPersonCameraController.cs`, `Interactor.cs`, `WorkstationInteractor.cs`; `Assets/Scripts/Interactables/HighlightEffect.cs`; `Assets/Scenes/Office_Level1.unity` HUD_Canvas/MobileControls; `Assets/InputSystem_Actions.inputactions` for existing bindings inspection.

## Scope

- Capture one pointer for MOVE and one for camera swipe, resetting on release/cancel/disable/focus loss. Support simultaneous movement/look and a separate HUD tap.
- Reuse joystick movement behavior and existing raw camera swipes. Disable obsolete LOOK UI/references through the setup command; avoid competing look sources or a transparent world-click blocker.
- Define configurable dead zones, radial normalization and sensitivity. Preserve analog magnitude through PlayerMovement's camera-relative conversion; no full-speed movement from slight stick deflection.
- Use purposeful base/thumb contrast/transparency and touch-size/safe-area layout. MOVE should be larger and further inward/upward; camera swipe has no visible joystick.
- Thumb follows finger responsively; visual press/return easing must not keep logical input active after release. Tune look smoothing separately from raw input.
- Reuse UI button press/selection states and existing highlight seams for restrained touch feedback; do not restyle every unrelated screen.
- Preserve desktop keyboard/mouse testing and current interaction behavior while establishing the input contract consumed by T02/T03.

## Acceptance criteria

- [ ] MOVE is visible, larger and comfortably inward/upward on supported landscape layouts; no LOOK joystick is visible.
- [ ] Handle follows its finger, cannot be hijacked by another touch, and returns smoothly with zero residual input.
- [ ] Small deflection gives controlled analog speed; diagonal input is bounded and no dead-zone drift occurs.
- [ ] Right-side swipe coverage/sensitivity is configurable, responsive, resolution-normalized and uses no competing look source.
- [ ] Joystick/UI touches never trigger NPCs/world objects or rotate through blocked UI.
- [ ] Dialogue, desk, board and profile-viewer locks reset movement and disable swipe look appropriately.
- [ ] Button/interaction targets provide reliable subtle feedback without unreadable overlays.

## Verification and handoff

Compile; inspect serialized bindings; add focused input/math checks where meaningful. Manual: move + look + HUD with separate fingers; drag off controls; release/cancel; pause/focus; desktop fallback; each current interaction and profile close. Record real device/resolution and unrun cases. Scene edits have one writer.

Preserve the current exploration view and existing player setup; dialogue overhaul belongs to its own ticket. Update ticket status, CURRENT_STATE, PROGRESS and HANDOFF with input ownership/configuration and exact remaining manual tests. Stop for approval.

