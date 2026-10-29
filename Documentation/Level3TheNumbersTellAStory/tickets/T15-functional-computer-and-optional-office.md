# T15 — Buy and use the optional installed computer after chapter completion

Status: **Planned**. Blocked by: T14.

Priority: P1. Phase: E — Upgrade milestone. Estimated scope: M; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

A funded player can buy a real installed 250-coin computer and immediately inspect both verified computations, using the same menu/counter ownership.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Resources/Campaign/shop.json`
- `Assets/Scripts/RewardsSystem/CampaignShop.cs`
- `Assets/Scripts/Interactables/OfficeDecor.cs`
- `Assets/Scripts/UI/Computer/ComputerHomeUI.cs`
- `Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Computer_Monitor_01.prefab`
- `Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Computer_Keyboard_01.prefab`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Reuse supplied monitor/keyboard/terminal pieces and existing desk art; resolve a clear local installation anchor using actual bounds/camera/colliders. Do not overwrite existing intro terminal, desk tools or purchased cup. Mark missing anchor before purchase, not after spending.
2. Implement a small read-only register that opens frozen Bea/company verified computation/source/tape snapshots. Same existing viewer/physical paper access; active-case binding must not change. No auto-treatment, answer key or future filing feature is sold.
3. Reuse office_computer productID250 coins and existing purchase confirmation/command/ownership. Make eligible only actual completedL3 AND functional feature readiness. One wallet/menu+counter; duplicate/insufficient/cancel/failure changes nothing. Owned device restores without repurchase.
4. Wire T14 completed-office option: counter/computer only, no timer/copy/reopened case or second wrap-up reward, safe Exit to Career. Career-shop purchase uses same installed state at supported next office. Keep chapter snapshots durably available to this chapter register without introducing global historical-folder archive.
5. Paper/calculator remains complete free route, and purchase is optional. Stand stays pending. If this feature slice is deferred by a later explicit scope decision, leave computer pending with no sale; never block story completion. This full pack includes implementing/verifying it now before final feature gate.

## Assets, motion and feedback

Existing monitor/keyboard assembly plus local 3D placement/interaction target. Functional register display reuses viewers; source prefab untouched, no generated photorealistic computer or new package.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Catalogue/runtime fallback must agree about readiness; do not change price/ID. New register adapts existing ComputerHomeUI only where it does not run prototype all-field calculator/filing paths. Future L4 transfer/print remains reserved, not silently implemented here.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] CompletedL3+feature ready gate; pending/early purchase cannot spend. Missing anchor and rapid double confirm/rollback are tested.
- [ ] Actual250coin ownership/installation is shared at menu/counter/reload; no duplicate model or purchase.
- [ ] Both verified snapshots/sources/tapes open read-only without global case rebinding or answer-key leakage.
- [ ] Post-completion entry/exit/save failure and no-purchase path preserve chapter completion and pay nothing again.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User inspects installed body/scale/lighting/collider, honest preview/register function, camera fit, both shop routes and exit without purchase.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Functional optional computer milestone and safe completed-office route. Next recommended T16.

Direct dependents: T17. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
