# T15 — Activate the existing optional computer purchase and verified client register

Status: **Implemented, internally verified; user manual pending**. Blocked by: T14. (6 November 2026; Unity/device acceptance is the user's.)

**6 November 2026 implementation:** The existing `office_computer` product (250 coins, office tab) is now sold once Level 3 is completed in the story (`chapter:Level_03`, shop.json and `ShopCatalogue.BuiltIn()` identical) and installs at a new `printer_desk` surface anchor: the separate PrinterDesk beside the consultant desk (present in all three offices), so the folder, stamps, book, calculator and pencil cup keep their spots. Purchase is the unchanged `ShopRules.Purchase` / `PlayerProgressManager.CommitPurchase` transaction (status gate, request id, one debit + ledger line + ownership + placement in one candidate, signed write, publish). `OfficeDecor` builds it from the supplied Synty monitor and keyboard (referenced from `CampaignShopAssets`; `DecorModels.BuildComputer`) with the existing 0.3 s arrival, one Interactable box (disabled until it has arrived) and `OfficeComputerTerminal`. Using it opens the read-only Client Register on the existing Audit Summary card: Bea's frozen approved taxable income, tax due, tax credit (withheld), remaining payable and the finding (`Level3RegisterRules`, saved campaign only), CLOSE and HER PAPERS (her saved papers in the existing Case Folder reader). Unfinished/incomplete data shows "No verified client case yet" with no figures. The legacy ComputerHomeUI prototype (calculate/prepare return) stays unused: no auto-solver, filing, printing or Level 4 transfer. Details/commands: latest PROGRESS; limitations KNOWN_ISSUES L3-R118–L3-R123.

Priority: P1. Phase: E — Office upgrade. Estimated scope: M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

After completed Bea-only Level3, a funded player buys the computer through existing shop mechanics, sees it installed, and immediately opens Bea’s verified computation and sources.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: office_computer product250 exists as pending teaser in the same menu/counter catalogue. Existing computer art/UI is reused; read-only verified register/purchase readiness must be implemented, not inferred from the product.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/RewardsSystem/CampaignShop.cs`
- `Assets/Scripts/Interactables/OfficeDecor.cs`
- `Assets/Scripts/UI/Computer/ComputerHomeUI.cs`
- `Assets/Scripts/UI/HUD/SupplyShopScreen.cs`
- `Assets/Resources/Campaign/shop.json`
- `Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Computer_Monitor_01.prefab`
- `Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Computer_Keyboard_01.prefab`

## Work and interaction mechanics

1. Resolve current desk terminal/install anchor and reuse supplied monitor/keyboard/ComputerHomeUI. Keep folder/calculator/stamps/decor clearance and base terminal identities. No new computer model/package/shop/currency/ownership framework.
2. Provide a simple read-only Bea register entry that opens her frozen verified computation/source/working without changing chapter state. Existing paper/viewer access supplies readable details; no dense dashboard. Add a company entry only in a future user-approved second-case extension.
3. Reuse product office_computer price250 and same confirmation/atomic wallet+ownership/install receipt. Require actual completedL3 AND shipped working feature/valid anchor. Both catalogue and fallback agree; cancel/insufficient/failed write/duplicate purchase changes nothing.
4. Installation uses brief bounded motion and persists for later office visits. Completed-office route/Exit to Career stays optional, no clock or new completion. Unaffordable player returns later; purchase not required for chapter or practice.
5. Level4 introduces confirmed figure transfer into return and draft printing. Do not enable legacy prototype auto-solver/filing as immediate function or sell promise-only equipment. If slice cannot ship, leave pending and report incomplete full-pack scope.

## Assets, motion and feedback

Implement this slice with actual existing 3D bodies, readable paper faces, reachable targets and current page/camera/audio ownership. One active task/primary action; temporary source/reference only on demand. Reuse L1/L2 styling and quiet lift/settle/feed/stamp cues. No tiny auto-shrunk text, full-tool overlay or correctness flash on draft placement. Bind models/colliders/callbacks here; T17 cannot substitute for this ticket’s visual implementation. User verifies rendered/touch/feel results.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Same menu/counter product/price/wallet/ownership; completedL3/readiness/funds/anchor gate purchase.
- [x] Cancel/failure/double input/reload cannot duplicate debit/install; owned device restores without repurchase.
- [x] Register immediately shows Bea’s frozen result and sources, with no active-case mutation or answer-key leak. No company entry is required in this release.
- [x] Skip/return later/base calculator route remain complete; L4 transfer/printing stays outside Level3.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Purchase preview/installation fits office with original assets and clear desk/door/control access.
- [ ] Register is simple/readable, Bea’s case reopens, sound/settings are respected, and optional exit remains clear.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T17. Continue the revised index, retaining prior working changes and historical results.
