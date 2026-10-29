# T16 — Practice computation and replay mastery without changing the live campaign

Status: **Planned**. Blocked by: T06, T12, T14.

Priority: P1. Phase: F — Learning/replay integration. Estimated scope: M; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

Players can revisit unlocked instructions/drills and improve eligible best mastery using the established binder/Career paths while story state remains untouched.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scripts/Training/TrainingSession.cs`
- `Assets/Scripts/Training/PracticeModeRuntime.cs`
- `Assets/Scripts/Training/TrainingBinder3D.cs`
- `Assets/Scripts/RewardsSystem/CampaignRun.cs`
- `Assets/Scripts/RewardsSystem/CareerProgressService.cs`
- `Assets/Scripts/UI/Menu/CareerChapterSheet.cs`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Add only relevant existing-binder lessons/drills for source/tape control, count-once treatment, graduated base/excess and credit-vs-expense. Reuse exact same calculator/worksheet interactions with separate unrelated content. No new learning menu or compulsory additional chapter task.
2. Follow established lesson unlocks: controls help when introduced; mastery replay after completed chapter; no future case answers exposed in locked practice. Existing Help/book rereads do not replay rewards or create indefinite active-work pause.
3. Replay initial worksheet checks are local and can improve eligible best badge/XP under existing best-record policy, not add every attempt cumulatively. Keep original first-story worksheet result/history and actual character/service choices intact. Test two-case XP aggregate without doubled worksheet XP.
4. Use CampaignRun guards on every new domain command and actual temporary snapshots of inventory/clock/tools. Practice may save lesson progress only; replay may improve eligible best mastery only; DirectEditor cannot write story. No live coins/trust/ownership/supply/checkpoint or Rosa consequence mutation.
5. Back/leave/reopen returns to binder/Career reliably, with local props/source/guide state cleaned up and pending story Resume retained. Do not introduce cloud/profile/extra save-slot storage.

## Assets, motion and feedback

Existing binder/medal/guide and authored drill data only; same3D tool/record assemblies, no duplicate exercise-only visual implementation.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Use existing distinctions among story/replay/practice/Editor modes. Compare full live campaign snapshot before/after actual commands, not a single helper permission flag. Avoid storing answer keys as live player accomplishments.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] Practice saves lesson progress only, replay improves eligible best mastery only, Editor never changes live story.
- [ ] Attempt/reopen/partial fail/cancel/full completion cannot farm260 coins/+10 trust or consume vouchers/alter ownership.
- [ ] Both-case aggregate mastery and frozen first-story result stay separate; lower replay cannot replace better record.
- [ ] Leaving practice/replay preserves correct storyResume and releases modal/camera subscriptions.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User opens drills/replay through existing binder/Career, checks clear mode messaging and actual live progress unchanged on return.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Reusable learning/replay routes with proven campaign isolation. Next T17 after T15 too.

Direct dependents: T17. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
