# Implementation plan

## Current scope and execution gate

Only `Assets/Scenes/Office_Level1.unity` is targeted. Other office scenes are excluded. Setup is ready; stop here until the user explicitly starts Ticket 01. Earlier partial code remains for inspection, not as completed acceptance criteria.

## Future order

1. Ticket 01: inspect and verify retained animation driver, actual NPC configuration and compatible assets; finish the reusable framework.
2. Ticket 02: complete visible conversation behavior through existing dialogue/FSM events.
3. Ticket 03: inspect retained desk placement changes, wire the actual desk surface and verify drag/open/cancel flows.
4. Ticket 04: printed physical document presentation and page transitions with existing data/chips preserved.
5. Ticket 05: synchronized local interaction and validation feedback.
6. Ticket 06: tutorial-based orthographic camera/RenderTexture/RawImage minimap with live world-position NPC markers.

## Verification strategy

Preserve current dirty changes. Reuse existing regression suites where relevant, compile changed code, inspect serialized references and perform available editor checks. Report Play Mode and visual results separately. No new test framework or package is required for setup.

## Exact continuation

Tickets 01-04 are done. When the user authorizes Ticket 05, read Tickets 03-04 records first. Never run the earlier multi-scene setup script.
