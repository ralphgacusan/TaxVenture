# Progress and continuation

## T03 implemented (all four checkpoints) — 2026-10-15

The user asked for the whole ticket ("do ticket 3", internal tests only, user does Unity testing), so the four checkpoints were delivered together.

During an admitted NPC conversation only, the game now:

- shows a grounded, standing Elijah built from the existing idle model,
- frames shot/reverse-shot over the correct shoulders, with obstruction fallbacks,
- uses a compact bottom safe-area panel with a typewriter and a single reveal/advance gesture gate,
- returns to the exact captured first-person view before controls are released and outcomes run.

Exploration and T01 are unchanged.

Code is complete, but the scene is not configured yet. The user runs **Tools > TaxVenture > Configure Dialogue Presentation** once in Edit mode, then saves the scene. Until then conversations behave exactly as T02 plus the typewriter. No scene, prefab, importer, data or Git change was made by the agent.

Updated 2026-10-15. Current unit: **T03 implementation and internal verification — complete; user setup and Unity acceptance pending.** T02's own manual acceptance is also still pending.

## T04 implemented — 2026-10-15

NPC markers are in: an amber pulsing triangle for available, a green check for completed, and nothing when unavailable, during any conversation, in other camera modes, out of range or occluded. Every NPC interactable reports `Availability` through pure `NpcAvailabilityRules` that mirror its own rules: receptionist per case, client interview and later presentation, auditor resume, four independent taxpayers, guide phases. The user runs **Tools > TaxVenture > Configure NPC Markers** once and saves the scene. Unity compile 0 errors; 796 assertions pass. Not run in Unity. Details are in the T04 ticket.

## T03 revision 1 — 2026-10-15

On user request, camera changes are now instant cuts (the blends are commented out for easy restore), the over-the-shoulder angle is wider (side offset 0.40, migrated automatically in saved scenes), a white 85%-opacity backdrop sits behind the dialogue box, and content-driven or intentional solo close-ups were added (`DialogueCloseUpPolicy`, `DialogueBuilder.CloseUp()`). The Unity compile has 0 errors and 757 assertions pass. Not run in Unity. Details are in the T03 ticket under "Revision 1".

## Status

| Ticket | State | Next condition |
|---|---|---|
| T01 | Implemented/retained | Record remaining user Unity/device acceptance |
| T02 | Phases 1 and 2 implemented; internal checks passed; manual acceptance pending | User runs the T02 manual matrix |
| T03 | **All four checkpoints implemented; internal checks passed; setup and manual acceptance pending** | User runs the setup menu, the preview and the T03 manual matrix |
| T04 | **Implemented; internal checks passed; setup and manual acceptance pending** | User runs Configure NPC Markers and the T04 matrix |
| T05 | Planned: integration and safe input returns | T01–T04 accepted and explicit approval |
| T06 | Planned: regression/handoff | T05 accepted and approval |

## Retained T01

Single-pointer analog MOVE, dead zones/sensitivity, larger inward/upward layout (280/112, center 260/240), right-75%-screen UI-excluded swipe with no LOOK stick, press/return feedback, capture/release/focus reset, tap-on-release guards, safe-area support and Undo-capable setup. Existing paper/quest behavior is preserved.

## Evidence

- PASS: **full-project compilation against the real Unity 6000.4.5f1 assemblies** (the installed `Editor/Data/Managed` and `NetStandard` folders, the project's generated `.csproj` source lists, defines and references, and `Library/ScriptAssemblies` package DLLs). The unmodified project compiles with 0 errors for both runtime and Editor, which validates the harness. With T03 swapped in, 0 errors and only the 2 baseline warnings. The Editor build used .NET Standard facades instead of Unity's .NET Framework profile.
- PASS: **610 production contract assertions** (`Tests/DialogueRegression`): 311 T02 plus 299 T03 covering terminal handoff ordering, the gesture gate including a rapid-tap walkthrough, typewriter timing and the shot/reverse-shot geometry across 24 configurations.
- PASS: scoped diff. The on-disk content of all 17 written files hash-matches what was compiled and tested. Existing files changed additively except `DialogueUI.cs`. Line endings follow each file's convention.
- PASS: static asset inspection. The Elijah FBX files are in centimetres, Y up, an armless/legless stylised figure about 6.7 m tall at import scale; the idle clip is 7.57 s and the rig is Generic with no avatar.
- PASS (T02): 311 assertions and the Phase 2 scene verification as recorded in the T02 ticket. PASS (T01): 5,734 controls assertions.
- NOT RUN by agent: the setup menu, Edit-mode preview, Play mode, shots, typewriter, taps, return and input restoration, outcomes, Android, safe areas or devices. Internal checks are not gameplay tests.

## Limits and next action

Office_Level2 is unverified and unconfigured (the setup menu is per scene). Historical tutorial and progression warnings are out of scope. Known T03 risks are listed in the T03 ticket: facing sign, Generic clip sampling without an avatar, NPC bounds that include props, and the intended 0.45 s wait before outcomes. The compile check left two archives (about 22 MB) in the git-ignored `Library` folder, and they can be deleted.

User next:

1. Let Unity compile.
2. In `Office_Level1` (Edit mode) run **Configure Dialogue Presentation** and read its report.
3. Run **Toggle Conversation Visual Preview** to check grounding, scale and facing.
4. Save the scene.
5. Run the T02 and T03 manual matrices. Report Console errors, screenshots of any bad shot, and the NPC or route.

Agent next only on explicit approval: **T04 NPC indicators**. Read truthful availability from the existing NPC FSMs and the read-only status properties T02 added (`HasBeenTalkedTo`, `IsInConversation`, `HasAppeared`, `HasStartedSideQuest`), hide markers while `DialogueUI.IsAnyDialogueActive`, and add no click-stealing colliders. Rough estimate **8k–14k tokens**; the main uncertainties are the office NPCs' case-dependent availability and marker placement.
