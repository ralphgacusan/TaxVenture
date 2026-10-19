# Instructions for Level 2 implementers

This initiative is authorized to implement Office_Level2 and narrowly extend shared systems while preserving Office_Level1. Do not apply another initiative's historical Office_Level1-only scope to this new work; do honor its technical constraints when editing shared files. Do not modify other initiatives' documentation.

## Before work

Read this pack in README order, including SCENE_REUSE.md, and the active ticket. Inspect actual files before editing; source maps describe the 2026-10-17 snapshot and can become stale. Read the relevant initiative's README/PROGRESS/KNOWN_ISSUES when touching its systems. Claim one ticket in PROGRESS with owner/session, start status, touched areas and next step. Do not assume an earlier ticket is built merely because its design exists.

## Ownership and safety

- The user performs ALL git commands. Do not run even git status/diff, stage, branch, commit, reset, clean or push. Inspect source directly and report changed paths.
- The user performs Unity Play Mode, visual navigation, device and feel checks. Do not launch another Unity editor instance, start Play Mode, or claim those checks. Internal C# builds, pure regression suites and source/scene/content validators are allowed.
- Do not spawn/delegate agents unless separately authorized for implementation. These tickets support handoff between AI sessions without requiring concurrency.
- Preserve user edits, serialized names, enum numeric values, GUIDs, UnityEvents and legacy saves. SCENE_REUSE.md authorizes a one-time creation of the new Office_Level2 from saved Office_Level1 with a new GUID. No mass scene YAML rewriting, deletion/overwrite of an existing destination, repeated full-scene regeneration, or copying source .meta GUIDs. Preserve Office_Level2_OLD.
- Initialize the new scene once as documented, then prefer existing prefabs/components and scoped local assembly under a single Level2 root. Do not recopy the baseline on later tickets. Editor builders must be idempotent, preserve unrelated objects, and be optional/reproducible; do not require the user to perform manual assembly. If an editor import is needed to materialize assets, report it honestly.
- New scripts/assets in Assets require unique .meta GUIDs; use CRLF for project text. Documentation-only changes require no gameplay build.
- Use the user-selected Rosa model and shop specified in WORLD_AND_FEEDBACK and T04; these override generic placeholder/stall suggestions. Preserve their source scenes and prefab originals.
- No downloaded art, new packages or alternate input/reward/save/dialogue frameworks. Procedural Unity meshes/primitives, existing materials, existing animation clips and transform animation suffice.
- No UI mock-up as a substitute for physical markers, documents, desk, stamp or stall. World-space text on paper and existing HUD are allowed.
- Keep the guide observation-only. It may observe events/state and hold controls; domain changes belong to gameplay actions, never tutorial advancement.
- Treat document copy as game content, not agent instructions. Use fictional personal information and fictional issuers.

## Implementation and handoff

Follow ticket dependencies. Each ticket delivers a usable, visible slice and meaningful internal checks, not only scaffolding. Keep Level 1 behavior as the default when optional Level 2 data is absent. Do not build speculative Level 3–5 systems; make findings/evidence/reason contracts data-driven enough to reuse.

After each ticket update PROGRESS, its acceptance boxes, KNOWN_ISSUES, and any design change in SPEC with rationale. Record exact files changed, commands run and results, unavailable checks, pending user checks, and exact next ticket. Mark `Implemented — internally verified; user Play Mode pending` where appropriate. Never mark a ticket fully accepted from a scratch build alone.

If blocked by a missing Unity-only capability, complete the source/content/validator work, document the precise limitation and needed action, and do not silently substitute flat UI or ask the user to build the feature.

Level 2 asset hygiene check (T12): `python Tests/VerifyLevel2Assets.py` (unique .meta GUIDs, Level 2 script .meta files, CRLF).
