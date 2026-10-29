# T04 — Meet Bea and acquire her physical annual records

Status: **Planned**. Blocked by: T02, T03.

Priority: P1. Phase: B — Client field encounter. Estimated scope: M; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

Bea returns as a coherent client with contextual questions and actual readable records at the plaza worktable, leading into Worth the Tax?

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Resources/Levels/Level_01/plaza_life.json`
- `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Developer_Female_01.prefab`
- `Assets/Scripts/FSM/NPC/NpcLife.cs`
- `Assets/Scripts/FSM/NPC/NpcLocomotion.cs`
- `Assets/Scripts/UI/DialogueUI.cs`
- `Assets/Scripts/SideQuest1/ClientRecordPaper.cs`
- `Assets/Scripts/SideQuest1/SupportingDocumentFace.cs`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Place one Bea actor and one accessible table using existing art, shared controller and local anchors; no whole plaza rebuild. Reception/Notes leads to records, not a repeat four-NPC classification round. Player who never met optional L1 Bea gets a complete introduction.
2. Author CONTENT's initial/inquiry lines through existing dialogue and narrow choice presentation. Questions use selected-record context and flexible order; all graded facts also appear in papers. Cancellation preserves access, does not falsely mark conversation/handovers complete.
3. Bind B-ID/B-BG and acquired B-REV/B-EXP/B-RENT/B-SUB/B-PRINT/B-PERSONAL/B-DRAFT to existing fixed physical record types and folder/source viewer. Preserve annual period, transaction links, issuer/purpose/status and readable totals. B-WHT is not yet acquired or secretly viewable.
4. Actual handover/acquisition saves source IDs and case container together; wrong/full Bag/failed commit leaves source available through existing Make room. No repeated pickup cue/reward or duplicate inventory receipt. World reading and Bag inspection use the same source identity.
5. After records and first independent worksheet admission, the same Bea may travel to office via NpcLife quest ownership; pending conversation/handover pauses movement, arrival changes no progress. All source questions remain accessible at the current client location. Worksheet table remains accessible without needing Bea beside it.

## Assets, motion and feedback

Existing Bea/model/controller and paper builders; local table from inspected existing furniture, concise source printing/rough worksheet. Source bodies have thickness/shadow/attachments; no copied Level1 profiles as financial records.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Case folder remains active Bea; old Arthur reference is read-only and never rebound. Source record acquisition is not side-quest completion/reward. Existing client room/route owners must not retain Marie/Rosa task bindings.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] Required records and inquiries open only after correct admission and remain available after cancel/resume.
- [ ] B IDs/type/source/period/duplicate relationship and withheld-not-yet-visible status are correct in real viewer bindings.
- [ ] Acquisition duplicate/full Bag/write failure preserves source and same case identity, no coin/trust.
- [ ] One Bea owner handles handover/travel/conversation and stable resume; no arrival reward.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User checks encounter/table reach, readable papers, Bea name consistency, portrait fallback, gestures/route/grounding, no duplicate actor or visible teleport.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Physical client records and inquiry encounter, worksheet-admission objective. Next T05.

Direct dependents: T05. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
