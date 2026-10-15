# Known issues and limitations

- The repository was already dirty at the audit baseline. Before edits to any shared scene or script, inspect and preserve its live diff.
- Only NPC_Marie, NPC_Jhun and NPC_Zorbix have animated (Humanoid "Standing Idle") models; the other 8 NPCs use static meshes with the procedural head fallback. The supplied character animation controllers contain a single idle state with no parameters or transitions. Existing walking FBXs do not by themselves prove the scene NPCs have compatible, assigned locomotion clips. Talk/listen/gesture animation availability remains unverified.
- Current folder/book drag scripts use camera-facing projection and are not bounded by a single configured desk surface.
- Current folder and book page changes update TMP content in place; there is no verified physical page-turn animation.
- The office minimap must be checked for roof and wall occlusion; the tutorial's scene/layers cannot be copied directly.
- Unity is already running for this project. Do not launch another instance. No Play Mode, device, or visual verification has been claimed.
- The tutorial transcript has been reviewed. Its key architecture is summarized in the audit and README; implementation must still be checked against actual office scene behavior.

## Walking animations (deferred)

NPC walking is disabled (`NpcIdlePacing.enableWalking` = false). In play-testing, walkers slid their feet and turned oddly. Clip speed matching, turning and static meshes with no legs still need work; see Ticket 01 section 18.

## Current checkpoint

Ticket 01 is code-complete but has no Play Mode/visual pass. Desk placement code (Ticket 03 partial work) is not wired to the scene surface. The generated Assembly-CSharp.csproj does not yet list DeskPlacementSurface.cs; Unity regenerates it on its next refresh. Office_Level1_COPY and Office_Level2 are excluded from future work.
