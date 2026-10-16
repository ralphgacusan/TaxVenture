# Setup ready — stop before Ticket 01

User instruction on 2026-10-15: focus only on Office_Level1, finish setup, and do not start any ticket. This handoff completes that setup. Wait for a new instruction before implementation.

## Read first

README.md, AGENTS.md, PROGRESS.md, DECISIONS.md, PROJECT_AUDIT.md and the active ticket. Inspect live Git status and diffs; the working tree contains extensive unrelated work. Do not modify the sibling ImmersiveInteractionOverhaul documentation.

## Retained partial implementation

- Assets/Scripts/FSM/NPC/NpcAnimationDriver.cs and .meta: optional Animator behavior bindings, FSM/dialogue subscriptions and static mesh head fallback. Latest code is unverified.
- Assets/Scripts/FSM/NPC/NpcStateMachine.cs: requires/adds the driver. Preserve pre-existing unrelated edits.
- Assets/Scenes/Office_Level1.unity: 11 serialized animation drivers on Auditor, Client, Client1, Client2, Colleague, NPC_Jhun, NPC_Marie, NPC_Milo, NPC_TutorialGuide, NPC_Zorbix and Receptionist.
- Assets/Scripts/Interactables/DeskPlacementSurface.cs and .meta plus partial CaseFolder3DDrag.cs/TaxCodeBook3DDrag.cs integration: desk scene wiring remains incomplete.
- No completed documents/feedback/minimap implementation is claimed.

## Verified asset facts

Jhun static FBX contains Mesh/Null nodes without a skeleton. Same-character standing-idle FBX contains Mixamo LimbNodes and imports as Humanoid, while static and walking FBXs import as Generic. Do not assign a Humanoid clip to an unrigged static mesh. Current procedural fallback is not a skeletal gesture or verified locomotion system.

## First future Ticket 01 actions

1. Inspect retained animation code and Office_Level1 components; verify serialized references and compilation.
2. Evaluate static fallback readability and head-part selection, lifecycle restoration, owner filtering and FSM event ordering.
3. Configure compatible animation assets only after rig checks; keep gameplay roots/colliders/data flow stable.
4. Verify and document acceptance honestly before proceeding to Ticket 02.

## Limits

No Play Mode or visual validation occurred. Earlier compile predates latest driver changes. Other scenes are excluded, and the failed multi-scene script must not be resumed. Do not revert retained partial work or other agents' changes without an explicit request.
