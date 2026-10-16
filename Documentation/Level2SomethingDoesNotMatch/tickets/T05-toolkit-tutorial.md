# T05 — Open the toolkit and teach one hands-on example

Status: Planned. Blocked by: T04.

## Visible accomplishment

At the market desk, a 3D toolkit opens, the guide demonstrates one salary record, and the player highlights a line and places classification/treatment/evidence notes themselves.

## Work

Reuse workstation camera/controls, document lift, book reference and tap/drag semantics. Create a real box/lid, marker, note cards and evidence clip with Unity meshes/primitives. Implement motions in WORLD_AND_FEEDBACK: lid opening, marker lift/trace, note snap and clip settle. No flat full-screen minigame replacement or new input framework.

T0 is an unrelated salary example. Demonstrate source observation, compensation type, treatment and supporting payslip, then wait for player actions. Type and treatment are separate slots; source highlight is unscored. Labels/icons accompany color. Compatible drop feedback never asserts a tax answer is correct. No fines, timer pressure or badge score during tutorial.

Implement a single-entry annotation view/controller reused by T06, not separate tutorial-only mechanics. Book opens through current system. Exit/back and reentry restore state and release movement/look/modal ownership cleanly. Pause compulsory teaching through a clear pause predicate; do not set Time.timeScale to zero to pause the guide.

## Acceptance

- [ ] Toolkit/tools/paper are 3D, animate and stay on desk correctly.
- [ ] Player performs highlight, type, treatment and evidence actions with large touch targets.
- [ ] Tutorial has immediate rule explanations and cannot affect scored accuracy.
- [ ] Same entry interaction can render a data-defined challenge entry.
- [ ] Exit/book/dialogue/pause do not leave stuck controls or duplicate objects.

## Verification/handoff

Compile; test annotation data/actions and tutorial transition; controls/dialogue/guide regression as applicable. Validate model roots/colliders/materials and cleanup statically. User later tests phone readability, input feel, animation and desk framing. Next T06.
