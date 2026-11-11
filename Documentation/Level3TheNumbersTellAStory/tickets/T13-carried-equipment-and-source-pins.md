# T13 — Verify carried equipment and source pins for Bea’s revised case

Status: **In progress**. Blocked by: T06, T10. (5 November 2026: four of five internal checks verified; base-capacity completion has one open design decision, KNOWN_ISSUES L3-R106.)

**5 November 2026 verification and repair:** T10's baseline was re-confirmed in the same build (2082 → 2119 Level3 checks, T10 section 41/41). Behavioral checks drive the real inventory/chapter/voucher/shop rules on the shipped Chapter 2 fixture and on a rich Bag (every Level 1/2 record, book, postcard, bookmark set, two vouchers, pins, folder contents and readable pages), base and upgraded: entry, outdoor/office snapshots, summary and certificate acquisition, approval, copy pickup and delivery keep every earlier item's slot, count, contents, pages, pins, capacity, decor and upgrades; rewards are the same fixed 100 coins/+4 at any capacity. Pins use the existing three ribbons and each record's own reader; stale or non-item pins drop quietly on load; a save with no readable copy says so. Rosa's/company/other-revision/float/text values and a 12,500 from a paper Bea never handed over are refused by `Level3ValueTransfer.TryRead`, never rebound. Vouchers: carry two, Bea's claim, +180 once, signed-write failure consumes nothing, duplicates and reload cannot repeat. Decor resolves to the same anchors every load; the comparison stand and computer stay pending teasers. **Fix:** a full bar/Bag at certificate pickup was reported as "Couldn't save"; it now refuses before any write with "Your bar and Bag are full. Make room, then take the certificate." **Open:** a base-capacity player who kept every Level 1/2 possession plus the postcard, bookmark set and a two-voucher stack has 10 items with no Level 3 exit for 9 slots once both of Bea's papers are needed (L3-R106). The certificate must occupy a slot (its credit is dragged from its own Bag paper), so this needs a user decision rather than a silent rule. Details: latest PROGRESS.

Priority: P1. Phase: E — Continuity. Estimated scope: S–M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

Existing purchases and possessions remain usable throughout Bea’s complete case without adding crowded tools or mandatory equipment. The company extension is deferred.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: Keep-all Bag/hotbar continuation already exists. This ticket integrates existing equipment with revised surfaces; it is not permission to postpone basic retention until here.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/RewardsSystem/CampaignInventory.cs`
- `Assets/Scripts/RewardsSystem/CampaignVouchers.cs`
- `Assets/Scripts/RewardsSystem/CampaignShop.cs`
- `Assets/Scripts/UI/HUD/HudHotbar.cs`
- `Assets/Scripts/Interactables/OfficeDecor.cs`
- `Assets/Resources/Campaign/inventory.json`

## Work and interaction mechanics

1. Verify all carried IDs/quantities/slots/contents/pins/capacities/keepsakes/decor against current committed inventory through Bea’s chapter phases and her office handoffs. Preserve old readable records read-only; no answer-key reconstructed annotations or global archive feature.
2. Bind existing bookmark/source resolvers to compact worksheet/folder support/stage views. Foreign-case pins remain readable references but cannot transfer into Bea’s active case; explicitly stale pins show why and can be unpinned. No new pin system or automatic rebinding to same-value source.
3. Exercise base Bag/hotbar capacity and upgraded variants; required originals/copies are not discardable and full inventory uses existing Make room behavior where needed. All required work/one comparison possible with free base tools.
4. Verify the voucher uses Bea’s current clock/claim,180 seconds, two carried/one use for Bea, and failure-atomic consumption; decor/installed tools restore once without overlapping folder/stamps. Keep comparison stand pending and no playable drawer.

### Deferred company extension

When the user reopens the second case through T11/T12, extend these same checks across the case switch: company source pins, case-scoped voucher use, and possession continuity. That future extension does not block this Bea-only ticket.

## Assets, motion and feedback

Implement this slice with actual existing 3D bodies, readable paper faces, reachable targets and current page/camera/audio ownership. One active task/primary action; temporary source/reference only on demand. Reuse L1/L2 styling and quiet lift/settle/feed/stamp cues. No tiny auto-shrunk text, full-tool overlay or correctness flash on draft placement. Bind models/colliders/callbacks here; T17 cannot substitute for this ticket’s visual implementation. User verifies rendered/touch/feel results.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Every collected possession survives Bea’s chapter transitions with same contents/quantities/capacity.
- [x] Pins/readers resolve the Bea source correctly; foreign-case values cannot leak into Bea and no missing historical text is fabricated.
- [ ] Base capacity/free viewing supports complete chapter; purchased variants do not change answers/rewards.
- [x] Bea voucher/decor save/failure/reload uses existing claims/ownership with no duplicate consumption/install.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Bag/hotbar/bookmarks remain familiar and do not obscure one-record/stage layouts.
- [ ] Test base/upgraded inventory, moving between table/office/client, voucher feedback and decor clearance.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T14, T17. Future company extension: reuse the retained T11/T12 case materials if the user adds that case. Continue the revised index, retaining prior working changes and historical results.
