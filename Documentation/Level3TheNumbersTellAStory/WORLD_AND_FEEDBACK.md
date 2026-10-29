# Physical presentation, NPC life and interaction feedback

All timings/dimensions below are proposed starting targets; no rendered/phone/frame-time acceptance is claimed. Use existing cream/navy/brass paper styling, low-poly assets, printing, safe focus, page turns, modal owners and SFX settings. New environment objects require genuine 3D bodies; text may reuse world-space paper faces and close-up UI as existing papers do.

## Required assets and binding ownership

| Assembly | Existing asset/presentation to reuse | Actual new work | Ticket |
|---|---|---|---|
| Calculator | Synty Calculator_01/02 prop | Local focus hit targets, readable keys/display, tape dock; not duplicate source model | T03 |
| Worksheet | Existing physical paper slab/printing/extraction | Four labeled sections, seven strips, two claim notes, relationship clip, subtotal fields | T05/T06 |
| Calculation tape | Existing paper/material/body builders | Narrow 3D paper sections with expression/source tabs and attachment points | T03, T07/T08 |
| Bea | Existing Developer_Female_01 and shared Humanoid controller | One level-configured actor, table/room route and phase dialogue | T04/T10 |
| Nina | Existing Developer_Female_02, intentional reused model | One new authored bookkeeper role, delayed admission and gestures | T11/T12 |
| Source papers | Standard Types1/2/4/5/7 and source-linked working papers | Specific B/C printed content; Type 8 2307 face/body binding if absent | T04/T08/T11 |
| Copy/stamp | Existing folder/ghost/pickup/stamp bodies | COMPUTATION COPY text and case-aware action admission | T09/T10/T12 |
| Computer | Existing monitor/keyboard/terminal pieces | Local installed assembly/anchor and actual read-only verified register | T15 |
| Optional art card | Existing keepsake/card interaction | Bea illustration/card note using owned/procedural vector print, no downloaded art | T17 optional |

Prefer local prefab/mesh variants or procedural bounded assemblies, with correct dimensions/pivots/materials/colliders. New small paper strips should read around 0.12–0.18 m wide on the desk and full sheet around 0.21 ×0.30 m where consistent with existing art; fit using actual source asset bounds. These are starting proportions, not permission to globally rescale the office. Tape should have visible thickness/curved leading edge and backing shadow, not a flat HUD rectangle pretending to be a world prop. No rig needed for paper; deterministic transforms/tween routines suffice.

No model download/generation is needed for calculators or monitors already supplied. If a body/clip cannot be made by existing procedural primitives, document exact missing shape, scale, material, collider, format/import settings and integration point. A practical mesh/primitives alternative is preferred to leaving a placeholder as final. Never claim a new asset exists before creating/verifying it.

## Interaction choreography

| Trigger | Visual/motion | Feedback/ownership contract |
|---|---|---|
| Source selection/extraction | Source row border, strip lifts 0.2–0.3s; original unchanged | One neutral paper-pickup cue; no correct/incorrect tint |
| Draft placement | Broad tray shadow/name, 0.15–0.25s settle/reflow | All plausible destinations accept; off-tray returns safely; tap-place equals drag |
| Claim link | Select claim, Link related entry, tap row; removable labeled clip | Same options regardless of wrong destination; do not reveal answer by admission |
| Calculator key | Next-frame input display, short key depression 0.06–0.1s | One tap-release input; no held repeat; neutral click if suitable supplied clip exists |
| Equals | Result immediately legible; 0.15–0.25s feed | Arithmetic only, no success/failure tax judgment |
| Attach result | Destination named first; tape settles 0.2–0.35s | Save appropriate state first; `Value attached`, not `Correct` |
| Changed dependency | Pencil stale tag, old tape retained | `Entries changed — recalculate`; do not auto-answer |
| Deliberate Check | Brief pencil/check pass, first score receipt, targeted notes | Completeness before grade; later correction never overwrites first result |
| Stamp | Existing lift/ghost/ink/return sequence | Ink/current revision commits once; not an award or filing event |
| Auditor/copy handover | Existing talk-first await, folder flight; drag or icon tap | Correct case/client only; failed commit retains source/copy, no trust FX |
| Committed service | Existing emote and HUD easing to saved heart value | Named +2/+4 reason after save only; no morality icon on a tax mistake |
| Computer purchase | Existing preview/confirmation/install receipt; model settles | Same wallet/ownership; real register function available immediately |

Suggested supplied cues: card_pickup_02 for lift, card_place_01 for settle, card_return_02 for return, existing stamp cue family for their physical events. Actual AudioLibrary names must be resolved; filenames carry sfx_ prefixes. Inspect existing keyboard/UI clips for a quiet key cue, otherwise a suitable neutral existing UI cue or silent visual press is acceptable. No unsupported assertion of a dedicated tape/printer recording. Rate-limit key/strip cues and feed one bounded cue per completed operation, not each character/frame. Reuse AudioEnvironment's existing office/plaza/music ownership; no new looping AudioSource or repeated music restart at each objective.

## Reading, screen sizes and controls

At workbench: selected source left, current worksheet section center, compact calculator/tape dock right. Narrow screens page between readable sections instead of shrinking everything simultaneously. At consultant desk show current computation step and compact completed-step summary. Large labeled destination tabs, stable source names/amounts and visible active-case name. Normal word labels and icons accompany color.

Use effective 48dp-equivalent primary touch regions at actual mobile rendering scale, readable contrast (4.5:1 normal text; 3:1 large), existing safe areas and consistent Back. Map screen units to device scale rather than assume a world-space centimeter value meets touch guidance. Optional drag offset keeps labels above the finger. No speed/precision/long-hold gate for reasoning. Honor existing SFX/music preferences and existing reduced-motion flag where supported; do not claim a new settings row. Viewers remain still while reading; skip flourishes without skipping domain operations.

Reuse current HudWindowLock/CameraController/WorkstationUI policies. Release only the caller's hold, await actual camera-return callback before entering another view, prevent world-tap passthrough and keyboard/UI clicks moving the player. Exit during independent work is allowed and preserves draft. Only brief owned transitions/compulsory teaching/action steps can block. App focus loss/disable cancels held pointer state and settles to stable saved state.

## NPC and environment quality

Use NpcLife/NpcLocomotion and existing procedural gestures/emotes; no unverified dedicated talk/handover/sit clip. Compatible rig/Avatar is checked before animation assignment. Conversations pause ambient/quest movement appropriately. Pending handover cannot be walked away from. Nina does not occupy Bea's spot before delivery. Quax's message uses receptionist/Notes, with no renamed Arthur portrait.

Preserve inherited lighting/materials/camera collision and doors. New props must rest on surfaces, have correct pivots/reachable colliders, avoid room entrances/camera clipping and use reasonable existing material/texture/mesh complexity. Cache meshes/layout/references; do not build text/geometry or raycast entire scene per frame. Profile actual Android frame time/memory during user checks; static simplicity is not measured performance acceptance.
