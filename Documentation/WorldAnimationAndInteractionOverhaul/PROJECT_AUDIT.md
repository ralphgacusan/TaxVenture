# Project audit

Audit baseline: 2026-10-15. This is a source and serialized-asset inspection, not a Play Mode pass.

## Repository and working state

- Unity project root: repository root `C:\Learn\Projects\TaxVenture\TaxVenture`.
- Branch at audit: `feat/immersive-interaction-overhaul`.
- The working tree already contained unstaged modifications and untracked files in Unity scenes, scripts, materials, renderers, tests, and `Documentation/ImmersiveInteractionOverhaul`. Particularly relevant dirty files include `Assets/Scenes/Office_Level1.unity`, `Assets/Scenes/Office_Level1_COPY.unity`, `Assets/Scenes/Office_Level2.unity`, `Assets/Scripts/Player/CameraController.cs`, `Assets/Scripts/Player/Interactor.cs`, `Assets/Scripts/FSM/NPC/NpcStateMachine.cs`, `Assets/Scripts/CaseFolder/CaseFolder3DDrag.cs`, and `Assets/Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs`. Treat these as shared in-progress work: inspect the live diff before editing and preserve all existing hunks.
- Existing documentation is under `Documentation/ImmersiveInteractionOverhaul`; it is a separate active initiative and remains untouched.

## Engine, pipeline, dependencies, and conventions

- `ProjectSettings/ProjectVersion.txt` selects Unity `6000.4.5f1`.
- `Packages/manifest.json` includes URP `17.4.0`, Input System `1.19.0`, UGUI `2.0.0`, AI Navigation `2.0.12`, Timeline `1.8.12`, TextMeshPro and Unity Test Framework `1.6.0`.
- `ProjectSettings/EditorBuildSettings.asset` enables `3D Menu`, `Office_Level1`, `Office_Level2`, `Level1_Character_Intro_Cutscene`, and `Settings`. This is historical build inventory; current initiative scope is Office_Level1 only.
- The project is Android-first; avoid extra rendering passes and per-frame allocations.
- Existing files use MonoBehaviours, serialized Inspector references, UGUI/TMP, and local FSM/interactable classes. No minimap implementation was found in the targeted scripts and office scene inventory.

## NPC, rig, and dialogue systems

- NPC state flow is implemented in `Assets/Scripts/FSM/NPC/NpcStateMachine.cs` and `Assets/Scripts/FSM/NPC/States/`. The states are Idle, Waiting, Interact, Dialogue, and Completed. They currently do not drive animation.
- Office NPC interactions are in `Assets/Scripts/Interactables/ClientInteractable.cs`, `ColleagueInteractable.cs`, `AuditorInteractable.cs`, and `ReceptionistInteractable.cs`. Side-quest NPC behavior is separate under `Assets/Scripts/SideQuest1/`.
- `Assets/Scripts/UI/DialogueUI.cs` exposes dialogue ownership and line state (`CurrentOwner`, `CurrentLine`, `IsLineTyping`, `LineChanged`, `DialogueStarted`, `DialogueEnded`) suitable for a presentation-only adapter. Preserve the existing completion/cancellation callbacks and game/NPC FSM logic.
- Available character assets include Jhun and Cora standing-idle Animator Controllers in `Assets/3D Models/npc_jhun_static/` and `Assets/3D Models/npc_cora_static/`, and a tutorial NPC idle controller in `Assets/3D Models/npc_tutorial_static/`. The inspected controllers contain a single standing-idle state, with no parameters or transitions. Walking FBXs exist for Jhun, Cora, Receptionist, and Elijah. No suitable talking/listening/pointing gesture set or matching locomotion controller was verified. Rig compatibility and the Animator setup on the live scene instances must be checked before assigning clips.
- Scene NPCs include Client, Auditor, Colleague, Receptionist and additional side-quest characters. `Assets/Prefabs/NPCs/Client_01.prefab`, `Client_02.prefab`, and `Client_03.prefab` are imported-model prefab instances. `Office_Level1.unity` contains NPC state/interactable components as well as separate environment and side-quest NPCs.

## Desk, documents, input, and validation

- Office scene objects include `Desk`, `DeskArea`, `DeskViewpoint`, `CaseFolder`, `OpenCaseFolder`, `TaxCodeBook`, and `TaxCodeBookDeskDrop`. Existing 3D models are under `Assets/3D Models/CaseFolder/`, `Assets/3D Models/TaxCodeBook/`, and `Assets/3D Models/Core Items/`.
- `Assets/Scripts/CaseFolder/CaseFolder3DDrag.cs` and `Assets/Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs` own pointer drag/open transitions. Their current drag projection uses a camera-facing plane and they do not establish a bounded desk placement surface.
- `Assets/Scripts/UI/CaseFolderUI.cs` builds TMP field rows from the current case data; `TaxCodeBookUI.cs` displays ScriptableObject sections and clickable values. Page navigation changes text in place. The content and data model already feed interactive rows/chips.
- `Assets/Scripts/Data/DragDrop/ValueTransferManager.cs`, `ValueClickDestination.cs`, `ValueDragSource.cs`, `CaseFolderFieldRow.cs`, and `TaxCodeValueChip.cs` carry value transfer and validation. Preserve these data paths; do not duplicate validation.
- `Assets/Scripts/Player/Interactor.cs`, `WorkstationInteractor.cs`, `CameraController.cs`, and `MobileInputGuard.cs` coordinate pointer input, camera/workstation modes, and input blocking. Existing `CameraController` owns camera and control transitions. Document close/page changes must respect that ownership.
- The main presentation is built from 3D document meshes plus TMP/UGUI panels and colliders. The assets already look like a desk workflow, but the current folder/book content is a text-panel presentation rather than a physical page-turn implementation.
- Existing outcome and transfer feedback is handled by `ValueTransferValidationPopup.cs`, `ValueTypeValidationFeedback.cs`, and `ValueTransferManager` events; inspect these before adding feedback.

## Scene integration and verification

- `Office_Level1` is the primary office scene referenced by the implemented client/auditor interactions; `Office_Level2` is enabled in build settings but is outside the current initiative scope.
- Existing scripts and scene YAML are already modified. No scene or prefab should be bulk-regenerated. Add components through the editor where available or use a narrowly reviewed serialized change.
- The Unity project and Unity processes are already open. Do not start a second editor instance. Static source/YAML checks and any available Editor.log compilation evidence must be reported separately from visual/manual Play Mode verification.
- Existing tests are under `Tests/`, including progression regression checks and small verification scripts. Reuse only checks whose scope covers the changed behavior. No new broad test framework is needed.

## Minimap tutorial and adaptation notes

The supplied transcript was inspected. Its relevant approach is an orthographic top-down camera feeding a RenderTexture displayed by a RawImage; the camera remains north-up while following player X/Z in `LateUpdate`; the UI shows a player marker and the minimap camera excludes UI-only layers. It also disables minimap shadows and removes the second AudioListener, while preserving URP camera data. Adapt this with an unparented camera, actual NPC world positions, lifecycle-safe markers, a compact safe-area HUD, and office-specific roof/occlusion checks. Do not copy tutorial coordinates, scene objects, layers, or scripts.

## Main risks

- Dirty scene/runtime files may contain work from another agent; a broad scene save or replacement could erase it.
- Existing Animator controllers do not provide conversation gestures or locomotion transitions; do not claim those behaviors until compatible states/clips are assigned and verified.
- The desk drop plane can project objects outside the usable surface or into overlaps; bounds, height, and collision response must be explicit.
- Replacing the document UIs can break interactive row colliders, transferred values, pagination, and TMP readability.
- A top-down minimap camera may see roof/upper geometry or draw world prompts; verify camera culling and a real office layout before completion.
- No gameplay or device test is claimed by this audit.
