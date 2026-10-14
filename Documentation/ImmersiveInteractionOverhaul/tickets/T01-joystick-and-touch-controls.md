# T01 — Joystick and touch controls

Status: **PLANNED — awaiting approval**
Dependencies: approved plan. Next: T02. Estimate: **8k-14k tokens**, varying with scene/UI wiring and device feedback.

## Objective and inspected entry points

Provide matching, polished movement and camera-look joysticks with independent, responsive input. Current movement stick exists; camera uses swipe and an inactive MobileLookArea is not a working look joystick.

Primary files: `Assets/Scripts/Mobile/VirtualJoystick.cs`, `MobileLookArea.cs`; `Assets/Scripts/Player/PlayerMovement.cs`, `FirstPersonCameraController.cs`, `ThirdPersonCameraFollow.cs`, `Interactor.cs`, `WorkstationInteractor.cs`; `Assets/Scripts/Interactables/HighlightEffect.cs`; `Assets/Scenes/Office_Level1.unity` HUD_Canvas/MobileControls; `Assets/InputSystem_Actions.inputactions` for existing bindings inspection.

## Scope

- Capture one pointer per stick, reset on up/cancel/disable/focus loss. Support simultaneous movement/look and a separate HUD tap.
- Reuse joystick behavior with explicit move/look roles. Adapt or disconnect swipe/LookArea only after mapping references; avoid simultaneous competing look sources.
- Define configurable dead zones, radial normalization and sensitivity. Preserve analog magnitude through PlayerMovement's camera-relative conversion; no full-speed movement from slight stick deflection.
- Use matching base/thumb visuals, purposeful contrast/transparency, distinct look role and touch-size/safe-area layout. Reuse available UI assets.
- Thumb follows finger responsively; visual press/return easing must not keep logical input active after release. Tune look smoothing separately from raw input.
- Reuse UI button press/selection states and existing highlight seams for restrained touch feedback; do not restyle every unrelated screen.
- Preserve desktop keyboard/mouse testing and current interaction behavior while establishing the input contract consumed by T02/T03.

## Acceptance criteria

- [ ] Both joysticks are visible, distinguishable and consistently styled on supported landscape layouts.
- [ ] Handle follows its finger, cannot be hijacked by another touch, and returns smoothly with zero residual input.
- [ ] Small deflection gives controlled analog speed; diagonal input is bounded and no dead-zone drift occurs.
- [ ] Look sensitivity/damping is configurable, responsive and not frame-rate dependent.
- [ ] Joystick/UI touches never trigger NPCs/world objects or rotate through blocked UI.
- [ ] Dialogue, desk, board and profile-viewer locks hide/reset or disable both sticks appropriately.
- [ ] Button/interaction targets provide reliable subtle feedback without unreadable overlays.

## Verification and handoff

Compile; inspect serialized bindings; add focused input/math checks where meaningful. Manual: move + look + HUD with separate fingers; drag off controls; release/cancel; pause/focus; desktop fallback; each current interaction and profile close. Record real device/resolution and unrun cases. Scene edits have one writer.

Do not implement third-person collision, character replacement or dialogue overhaul here. Update ticket status, CURRENT_STATE, PROGRESS and HANDOFF with input ownership/configuration and exact remaining manual tests. Stop for approval.

