# T13 — Use carried upgrades, bookmarks, vouchers and decor throughout both cases

Status: **Planned**. Blocked by: T06, T12.

Priority: P1. Phase: E — Campaign equipment integration. Estimated scope: M; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

Existing purchases remain useful and correctly scoped across Level2→Level3 and Bea→company, while all required work fits base capacity and free comparison.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scripts/RewardsSystem/CampaignInventory.cs`
- `Assets/Scripts/RewardsSystem/CampaignShop.cs`
- `Assets/Scripts/RewardsSystem/CampaignVouchers.cs`
- `Assets/Scripts/Interactables/OfficeDecor.cs`
- `Assets/Resources/Campaign/inventory.json`
- `Assets/Resources/Campaign/shop.json`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Verify T02 entry preservation against real inventory eligibility/normalization. Extend only necessary Notes/folder/book scope/resolvers. Retire L2 marked records and old profile quest objects from active use without losing campaign-wide Arthur reference/vouchers/bookmarks/keepsake/capacity/decor.
2. Bind up-to-three bookmarks to actual acquired L3 source/worksheet/certificate views. Resolve sources by case/row identity. A retired/foreign source offers unavailable+Unpin, never rebinds silently to new case amounts; no new answer shortcut or paid source access.
3. Verify Bag6→9→13 and hotbar3→4→5 ownership/prerequisite chain/prices unchanged; large upgrades depend on actual completedL2. Required tools/reference fit base capacity; strips/tapes/records stay in container. Full optional inventory uses Make room/source retention, no revived drawer.
4. Restore pencil cup/island print at compatible scene anchors without repurchase; missing valid location preserves ownership and reports state. Keep supply counter/menu one catalogue/wallet. Stand remains pending, computer remains pending until T15’s functional slice.
5. Exercise both case voucher commands with saved quantity, per-case claim, extended real clock and failed-write rollback; cap2 carried and refuse third without spend. Replays/local fixtures cannot consume live items. No new upgrades, refund, mandatory purchase or trust gain from shopping.

## Assets, motion and feedback

Existing bookmark/Bag/hotbar/decor/counter models and source viewers. Level3 anchor/source bindings only; no storage drawer or comparison-stand asset build.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

This adoption slice uses complete actual case papers/clock behavior rather than synthetic inventory-only rows. Existing L1/L2 prices/ownership/normalization remain unchanged; verify source fallback/catalogue agreement.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] Representative owned/base/full inventories round-trip L2→L3 and Bea→company without item/reset/answer leakage.
- [ ] Three pins open correct source; retired/foreign pins Unpin safely; base capacity/free comparison can complete chapter.
- [ ] All existing price/capacity/unlock/decor states agree at menu/counter, no repeated purchase or pickup.
- [ ] Voucher case independence/carry2/one-per-case/failure/reload/sandbox tests call actual commands.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User checks owned equipment/decor appearance, source-pin convenience, Make room, both voucher confirmations and no compulsory purchases.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Complete campaign equipment continuity with real two-case adoption. Next T14.

Direct dependents: T14, T17. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
