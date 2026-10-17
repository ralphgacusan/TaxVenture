# T01 — Open Level 2 in the familiar office

Status: Implemented — internally verified; user Play Mode pending (2026-10-17). Blocked by: None.

## Visible accomplishment

Entering Office_Level2 shows the familiar office, one HUD, a short welcome and Rosa's assignment. The office is open before the quest. This slice creates the new scene from a controlled Level1 baseline and makes its Level2 entry playable; copying the scene alone is not completion.

## Work

Read SCENE_REUSE.md and inventory current saved Office_Level1's hierarchy, prefabs, startup controllers, client selection, input, lighting, desk/book/folder/stamps and build entry. Create new Assets/Scenes/Office_Level2.unity through one controlled native asset copy of Office_Level1 with a distinct scene GUID, sharing original asset references. Preserve Level1 and Office_Level2_OLD. If the new destination already exists, inspect and continue it without overwriting or recopying. Do not copy .meta GUIDs, hand-reconstruct scene YAML or launch another Unity editor. Record source hash/setup version and inherited bindings, then disable only copied Level1-specific flow. Update menu/build mapping to new Level2, preserving other scene selection mappings; no user assembly required.

Configure Level_02 as `Something Does Not Match`, one Rosa case. Add safe optional level capability/configuration for shared HUD, map, guide and fade; explicitly opt Level2 in without activating them in menu/training scenes. Reuse current presentation, not copied HUD frameworks. Start at reception, with objectives leading to receptionist then cabinet. Disable only Level2's profile/corkboard startup and side-quest-before-office gate.

Add initial Rosa cover/summary/referral fixtures and three Level2 book leaves through existing content pipeline. Validate IDs, bounded tax assumptions and course/primary basis for loan treatment; record source status in CONTENT. Do not implement tax rates or an official annual-return calculation. Seed review state under current case ownership, with absent optional data preserving old cases.

## Acceptance

Internal status per box (source checks only; every box also needs the user's Play Mode check):

- [x] New Office_Level2 exists with its own GUID and correct menu/build routing; Level1/_OLD unchanged and shared assets not duplicated. *(VerifyLevel2Scene.py; Unity import pending)*
- [x] Re-running setup preserves the destination and creates no duplicate world/owners. *(runtime root built once per load; editor guard acts only while the build entry is missing)*
- [x] One correct level/case loads, no Marie/Teody/corkboard objective or locked entrance. *(Level2Regression)*
- [x] Reception welcome and cabinet objective appear in existing presentation. *(data checked; on-screen result pending)*
- [x] Shared systems select Level2 content and initialize once; Level1 defaults unchanged. *(Level2Regression + all existing suites)*
- [x] Case fixtures and book rules validate; legal/course authority limits documented honestly. *(loan rule = educational draft, see CONTENT)*
- [x] Exact reused assets, conflicts, source changes and unresolved parity gaps recorded. *(PROGRESS, KNOWN_ISSUES)*

## Internal verification and handoff

Scratch compile; targeted progression/guide/content tests for scene opt-in, one-case manifest and default behavior. Source validator checks new scene GUID, source hash preservation, shared asset references, build/menu mapping, inherited bindings/anchors and duplicate bootstrap conditions. Record native setup execution versus source-only creation/import pending truthfully. User later checks office spawn, HUD, appearance and navigation. Update PROGRESS; next T02.
