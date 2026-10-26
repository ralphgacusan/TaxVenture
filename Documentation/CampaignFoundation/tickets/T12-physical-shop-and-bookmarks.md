# T12 — Add the physical supply counter and bookmarks

Status: Planned. Blocked by: T09, T10, T11.

## Goal

Make the world counter a functional view over the shared shop and inventory.

## Visible accomplishment

Before: there is no durable physical supply shop or bookmark ownership. After Marie's corrected case is approved, the shop becomes unlocked. The required Marie follow-through then occurs; Quax introduces the nearby supply counter afterward. The player can inspect and buy one bookmark set for 40 coins. Owning it adds three pins to the bag so three collected source records can be kept at the top of the catalogue. The ordinary record viewers and workstation comparison remain free and work without bookmarks.

Slot upgrades (revision 2026-10-24, see [T10](T10-unified-bag-and-documents.md)): the same counter also sells a **Satchel** for 50 coins (bag 5 to 8 slots) and a **Belt Pouch** for 60 coins (hotbar 3 to 4 slots). Each is bought once and applies through T10's UpgradeCapacity command; the new slots appear unlocked at once in the HUD row and inventory. A larger Satchel (12 slots) and a second Pouch (5 hotbar slots) are visible as teaser cards reading "Requires corrected Level 2" and cannot reach a confirmation. A Level 1 run's 160 coins cannot buy every item; choosing is intended. Upgrades are equipment, not carried items: they use no slot and cannot be sold.

Revision 2026-10-25 (user decisions, see [PROGRESS](../PROGRESS.md) and KNOWN_ISSUES CF-66/CF-67): the counter is introduced at the start of Level 1, right after the move and look lessons and before the guide leads the player to the townsfolk, with smooth view turns to the counter and back; the first counter visit unlocks the shop (case approval no longer does). Tapping the physical counter opens an on-screen catalogue book in the Bag panel's style rather than a page on a tilted 3D book; T13 reuses it for the menu.

## Work

Build a reachable, physical counter with a timber top, a brass “SUPPLIES” nameplate, a shallow tray, a hinged sample book and real price cards. Put a tied packet of three leather ribbon bookmarks on the counter, with a paper price tag reading “40 COINS / THREE PINS”. The interaction opens the actual catalogue page on the tilted book; do not replace the counter with a full-screen mock shop. Instantiate a scoped copy of the supplied counter prop; do not edit its source prefab. Case approval unlocks the shop domain, while the canonical introduction follows Marie's required follow-through. Until Quax's intro is acknowledged, menu/counter entry remains locked with a short requirement label. Visiting and buying stay optional; skipping after the intro returns to chapter wrap-up.

### Source starting points

- **Existing:** `Assets/Scripts/SideQuest1` profile interaction/pickup paths; `Assets/Synty/PolygonTown/Prefabs/Props/SM_Prop_ShopCounter_01.prefab` and `SM_Prop_ShopShelf_01.prefab`; existing world-interaction and dialogue pause patterns; `PlayerProgressData` / `PlayerProgressManager` as the signed-save owner; `AudioManager` for effects and the current settings volume.
- **Proposed:** campaign catalogue data and a shared shop/purchase/bookmark command as described in SPEC. No current symbol named “CampaignShop” or bookmark service is assumed. Keep the world counter and later menu catalogue as presenters over the same domain command and snapshot.

On successful purchase, the player turns the sample book's brass clasp; three narrow ribbon tabs flip open, then the bookmark packet lands in the first free hotbar or bag slot as a Tools & Supplies item (T10's "Make room" flow if both are full). Buying the Satchel or Belt Pouch plays a short strap-buckle cue and the new slot tiles unlock with a padlock-open snap. In inventory, tapping a source record's pin icon slides its index card into one of three ribbon tabs. A fourth choice prompts the player to replace one of the three. Label each pin with record name and icon, not colour alone. Reorder/unpin changes only saved display preference and never removes or sells the underlying evidence. Catalog inspection pauses the world clock consistently with other shop menus; opening the bag and reading records follows the ordinary active-review clock rule. Use one selected purchase cue on successful commit and a soft page/ribbon snap per pin change, with at most one cue per completed action. Send through the user-adjustable effects channel and suppress sounds during failed writes or repeated tap frames.

## Persistence and failure rules

Purchase checks story mode, shop unlock, price and existing ownership, then atomically debits 40 and grants the one bookmark-set ID. Insufficient funds, duplicate ownership, missing catalogue data or save failure leave wallet and inventory unchanged and show a clear reason; no animation may imply success before persistence. A second request with the same action ID is no-change. Upgrade purchases debit and raise capacity in the same commit; a duplicate or failed purchase leaves capacity unchanged. Buying never awards trust/XP or repeats the 60/100 coin story rewards. Three pins persist as IDs referencing owned source items, not copied case facts; deleting/unavailable records normalize to unpinned without corrupting the save. Practice, mastery replay and temporary direct-Editor runs can browse a temporary catalogue but cannot spend real coins or alter live pin choices. Bookmark access is a convenience only; all comparison and source-reading interactions remain possible for free.

## Internal checks — AI source/build/regressions only

- [ ] Catalogue price, unlock, ownership, debit and receipt use one command/domain snapshot from both world and menu entry points.
- [ ] Satchel and Belt Pouch purchases raise bag/hotbar capacity once, atomically with the debit; teaser upgrades cannot be bought.
- [ ] Regression cases cover successful/duplicate/failed/insufficient-funds purchase, three-pin replacement, missing source normalization and isolation of non-story modes.
- [ ] Static scene inspection confirms the counter/book/cards have stable colliders, deterministic anchors, a physical display model and no hidden scene wiring requirement; affected build and shop/save checks pass.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] Before case approval, verify the counter cannot be used. After approval and Marie follow-through, check the introduction, enter or skip the counter, and confirm that skipping continues to wrap-up.
- [ ] Buy the Satchel and the Belt Pouch; confirm the new bag slots and the fourth HUD slot unlock immediately and survive a relaunch.
- [ ] Buy the set once, pin three records, replace one, close/reopen the bag and compare all records at the free workstation.
- [ ] On target device, check the counter reach, book tilt/clasp animation, card labels, touch targets, clock pause and individual page/purchase sounds at the effects setting. The user does not build the scene.

## Handoff

Next ticket: [T13](T13-menu-shop-and-office-decor.md) connects menu purchases and office decor to this same shop state.

## Non-goals and references

No paid answers, selling, loot boxes, wallet rewards for browsing, bookmark durability, or locked feature purchase. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
