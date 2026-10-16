# Decisions

## D001 â€” Keep the initiative independent

Create this work in `Documentation/WorldAnimationAndInteractionOverhaul`, separate from the already active `Documentation/ImmersiveInteractionOverhaul`. Preserve all files and ongoing work in the sibling folder.

## D002 â€” Extend the existing NPC and dialogue path

Use `NpcStateMachine` plus `DialogueUI`'s owner and line events as presentation signals. Do not replace the dialogue FSM, callbacks, case rules, or progression flags. Existing animation controllers have idle-only states, so movement/talk gestures are not assumed to exist.

## D003 â€” Keep existing data transfer authoritative

The CaseFolderUI, TaxCodeBookUI and ValueTransferManager remain the sources of document content, clickable values and validation. New visuals and interactions must preserve their field IDs, case data writes, transfer rules and UI ownership.

## D004 â€” Use one desk placement surface

Bound dragged folder/book positions to one configured desk plane/surface. Preserve the existing open/close and intentional snap points, but do not create competing drop handlers.

## D005 â€” Adapt the supplied minimap tutorial

Use an unparented orthographic camera and RenderTexture/RawImage, following player X/Z in `LateUpdate`, with a stable north-up orientation. Derive NPC indicator coordinates from the actual world transform through the map camera projection. Keep URP camera data, exclude map-only/UI content, and verify office occlusion before declaring it integrated.

## D006 — Office_Level1 only; setup stop gate

The latest user instruction supersedes the original multi-scene implementation scope. Target only Office_Level1. Finish documentation/setup and stop before starting or resuming Ticket 01. Retain earlier partial implementation without claiming completion or removing unrelated work.

## D007 - Ticket 01 uses existing idle controllers and a static fallback

No new clips, controllers or profile assets. Humanoid NPCs keep their assigned "Standing Idle" controllers; the driver prefers the child Animator that has a controller, skips inactive Animators and does not restart the state already playing. Static NPCs use the procedural head fallback. Unsupported behaviors fall back to idle rather than faking gestures.

## D008 - Visible ambient motion without new assets

Static NPCs animate their model container procedurally (breathing, sway, glance, step bob); colliders stay on the unmoved root. Pacing is a separate opt-in component that only moves NPCs whose model can show walking. Humanoid walking reuses the project's unused walking FBXs through an editor tool, not hand-edited import YAML.

## D009 - Desk surface is the desk model's top collider

Ticket 03 uses the BoxCollider on `Desk/Visual/SM_Prop_Desk_03` (top y 0.75, the visible desk top) as the single placement surface, not the lower `Desk` root collider. Drags project at the item's resting height and clamp with an asymmetric footprint. Blocked moves slide or hold, and are never rejected outright, so a drop over the desk always stays valid. Stack snap-back on close stays in the existing return-to-desk scripts.

## D010 - Desk motion is presentation-only and completable

Desk document animation lives in `DeskDocumentAnimator` (on `Desk`) and `DeskMotion`. The existing scripts still own every state change and pass it in as a completion callback. Any tween can be completed instantly (before a grab, reset, HUD store or audit retry), so no object is left mid-flight. Leaving the desk uses a `CameraController.WorkstationExited` event rather than giving the camera desk references.


## D011 — Ticket 05 feedback follows the current presentation lifecycle

Keep ValueTransferManager, category validation, data writes and submission/progression logic unchanged. Refresh/reinitialize/disable clears old transient feedback; replacement feedback begins after the field refresh. Selection supplies the resting tint without overwriting active pulses. Hover uses a separate neutral, raycast-free underline so it cannot compete for the drop outline or transform. Its runtime installation is restricted to Office_Level1; existing shared feedback lifecycle fixes stay in the common components. Preserve the inactive global popup; if enabled, it is optional nonblocking text with one unscaled expiry and balanced subscriptions. All changes are runtime code, requiring no scene reload.


## D012 — Runtime, north-up Office_Level1 minimap

Bootstrap one scene-owned camera/HUD only for Office_Level1. Reuse the existing HUD_Canvas with a dedicated safe-area wrapper. Reserve the current right-hand Settings/document button column rather than moving existing controls. The user reference guides the compact framed game HUD; dark navy, warm gold, cyan player arrow and amber people diamonds provide shape as well as color distinction.

The inspected ceiling slabs share Default with the floors/walls. Use a fixed horizontal camera clipping plane at world Y2.8 (camera Y25, near22.2/far26) to exclude the slabs without changing layers, materials or scene geometry. This is a single-floor map; wall cuts and plaza/indoor readability must be user-verified. Keep a 256-square RenderTexture, disable optional camera effects and stop rendering when hidden. Preserve gameplay camera ownership and scene lifetime cleanup.

Use live NpcStatusMarker roots as discovery anchors rather than only NPC FSMs, since colleague/guide have no FSM. Marker display must not use overhead CurrentShape, which depends on range/occlusion. Suppress the hidden tutorial guide until HasAppeared; no game/quest state writes. No objective arrows, navigation routes, map input or zoom controls.


## D013 — User-requested top-right row layout

Supersedes D012's original reservation of the right-hand icon column. Put minimap at the top-right safe-area corner, Settings directly left, and translate the existing icon container below it. Preserve icon styling, child layout, size and input bindings; runtime placement avoids requiring a scene reload.
