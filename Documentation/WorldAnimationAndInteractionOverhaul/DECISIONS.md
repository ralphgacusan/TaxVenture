# Decisions

## D001 — Keep the initiative independent

Create this work in `Documentation/WorldAnimationAndInteractionOverhaul`, separate from the already active `Documentation/ImmersiveInteractionOverhaul`. Preserve all files and ongoing work in the sibling folder.

## D002 — Extend the existing NPC and dialogue path

Use `NpcStateMachine` plus `DialogueUI`'s owner and line events as presentation signals. Do not replace the dialogue FSM, callbacks, case rules, or progression flags. Existing animation controllers have idle-only states, so movement/talk gestures are not assumed to exist.

## D003 — Keep existing data transfer authoritative

The CaseFolderUI, TaxCodeBookUI and ValueTransferManager remain the sources of document content, clickable values and validation. New visuals and interactions must preserve their field IDs, case data writes, transfer rules and UI ownership.

## D004 — Use one desk placement surface

Bound dragged folder/book positions to one configured desk plane/surface. Preserve the existing open/close and intentional snap points, but do not create competing drop handlers.

## D005 — Adapt the supplied minimap tutorial

Use an unparented orthographic camera and RenderTexture/RawImage, following player X/Z in `LateUpdate`, with a stable north-up orientation. Derive NPC indicator coordinates from the actual world transform through the map camera projection. Keep URP camera data, exclude map-only/UI content, and verify office occlusion before declaring it integrated.

## D006 � Office_Level1 only; setup stop gate

The latest user instruction supersedes the original multi-scene implementation scope. Target only Office_Level1. Finish documentation/setup and stop before starting or resuming Ticket 01. Retain earlier partial implementation without claiming completion or removing unrelated work.

## D007 - Ticket 01 uses existing idle controllers and a static fallback

No new clips, controllers or profile assets. Humanoid NPCs keep their assigned "Standing Idle" controllers; the driver prefers the child Animator that has a controller, skips inactive Animators and does not restart the state already playing. Static NPCs use the procedural head fallback. Unsupported behaviors fall back to idle rather than faking gestures.

## D008 - Visible ambient motion without new assets

Static NPCs animate their model container procedurally (breathing, sway, glance, step bob); colliders stay on the unmoved root. Pacing is a separate opt-in component that only moves NPCs whose model can show walking. Humanoid walking reuses the project's unused walking FBXs through an editor tool, not hand-edited import YAML.
