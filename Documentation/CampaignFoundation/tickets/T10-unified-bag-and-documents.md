# T10 — Carry items in a hotbar and manage them in an upgradable bag

Status: Planned. Blocked by: [T05 — Level 1 case and clock resume](T05-level1-case-and-clock-resume.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

Revision 2026-10-24 (user request): the original four-section catalogue Bag is replaced by a slot inventory modelled loosely on Minecraft. The current HUD icons become the hotbar; a new Bag icon opens the inventory where the player chooses what is carried in the hotbar. Shop upgrades to slot counts are sold in [T12](T12-physical-shop-and-bookmarks.md) and shared by [T13](T13-menu-shop-and-office-decor.md).

## Goal and before/after flow

Give the player one reliable, game-like place to carry and arrange everything they own, without making it a second source of case truth. Before this ticket, the HUD row (PermanentHUD/IconButtons: NotesIconButton, CaseFolderIconButton, TaxpayerProfileIcon) is a fixed set of icons that unlock through GameplayEvents, and records are spread across profile HUDs, the physical case folder, desk papers and small runtime lists. After it, those three icons are three general-purpose hotbar slots that hold whatever item the player puts there, a Bag icon sits at the end of the row, and tapping it opens an inventory screen: the bag grid on top and a copy of the hotbar below. The player moves items between them to decide what is at hand.

## Player experience

- The HUD keeps its current placement, size, spacing, icon art (HudIconArt), badges (HudIconBadge) and drag-out behaviour. The row is 3 hotbar slots plus the Bag icon.
- Slots are not tied to one item. A slot shows the art of the item it holds; an empty slot looks like today's dimmed locked icon with a dashed outline. Level 1 starts with all three slots empty and they fill as items arrive.
- **Drop to slot (same as today's folder):** world items the player carries are dragged onto a slot icon exactly as the Case Folder is dropped onto its icon now (FolderToHUDIcon): the slot highlights under the finger, the 3D object hides and the item lands in that slot. Dropping on an empty slot places it; dropping on an occupied slot swaps, and the previous item goes to the bag ("Make room" if the bag is full). Dropping anywhere else on the HUD row auto-places it. This applies to the Case Folder, the Tax Code Book, the Client Copy and later T11 pickups. Items granted by dialogue or events (Notes from the receptionist, profiles) auto-place with the Bag bounce.
- Returning an item to the world empties its slot: OpenFolderReturnToDesk frees the folder's slot, and putting the Tax Code Book back on the desk frees the book's slot.
- The Bag icon is always present, never occupies a slot and is never locked. When an item is added it bounces once and shows a short "+1".
- Hotbar items are usable at once: tap opens the item exactly as the icon does today; drag-out into the world keeps working (folder to the Auditor tray, Client Copy to Marie, profile cards to the corkboard).
- Bag items are readable but not usable in the world. Tapping one in the inventory opens its viewer (read-only reading is always allowed). Using it in the world requires moving it to the hotbar.
- When a world action needs an item that is only in the bag, the prompt says so ("Your Case Folder is in your bag") and offers one large "Take it out" button that swaps it into the hotbar. The player is never sent to hunt through menus to finish a required step.

## Slots and capacity

| Area | Start | Upgrade 1 (Delivery A shop) | Upgrade 2 (later, teaser only) |
|---|---|---|---|
| Hotbar | 3 | 4 | 5 |
| Bag | 5 | 8 | 12 |

Starting at 3 hotbar slots keeps Level 1 identical to today until the player buys more. Locked slots are visible as greyed tiles with a small padlock and the label "Shop upgrade", so growth is visible. New hotbar slots extend the existing row with the same spacing; the Bag icon moves to stay last. Capacity values are campaign data with a hard ceiling of 5 hotbar / 12 bag; T10 owns the capacity fields and the upgrade command, T12 sells them.

## Items, containers and stacks

Item definitions live in the planned campaign resource folder with a stable ID, category (Profiles, Case Files, Tools & Supplies, Keepsakes), icon, label, required flag, stack limit and container flag.

- **Profile Binder** (container, required once collected): one slot holding every collected taxpayer profile, like a Minecraft bundle. Opening it shows the current HudProfileCards fan. Profiles are never spread across slots.
- **Case Folder** (container, required while a case is active): one slot holding the case papers and, after approval, the Client Copy. Original and working copies stay distinct IDs and revisions inside it.
- **Notes**: one slot.
- **Tax Code Book**: one slot, not required. Picked up from the desk by dragging it onto a slot, then opened from the slot anywhere with the existing TaxCodeBookUI (same sections, same first-open event). While it is carried the desk spot shows an empty book stand; tapping the stand while the book is in the bag or drawer offers "Take it out". It may be stored in the drawer, and placing it back on its stand also works.
- **Supplies** (vouchers, later the bookmark set): stack, shown as a "×3" count; stack limit per definition.
- **Keepsakes**: one per slot, never stacked.

Containers keep the 5-slot bag meaningful: without them the side quest's four profiles alone would fill it.

## Desk drawer (storage) and never-stuck rules

- A **Desk Drawer** at the player's workstation is unlimited storage, like a Minecraft chest. It opens as a third panel of the inventory only while the player is at the desk; elsewhere it shows "At your desk" and stays closed.
- The drawer lists items with the four category filter tabs (Profiles, Case Files, Tools & Supplies, Keepsakes), counts and an empty state.
- Required items (Case Folder while the case is open, Profile Binder, Client Copy) can never be put in the drawer, sold, discarded or hidden. Their slot shows a small seal icon; trying to move them there shows "Needed for your case".
- **Auto-placement:** a new item goes to the first empty hotbar slot, then the first empty bag slot, then merges into an existing stack.
- **Full on pickup:** if hotbar and bag are full, the inventory opens with the new item held in a "hand" slot and the heading "Make room". The player places it, swaps it, or sends something non-required to the drawer (from anywhere: the drawer is a destination, it just cannot be browsed away from the desk). Closing with an item still in hand returns it to its world source; required items cannot be left behind, so the panel only closes once a required item is placed.
- Content rule (validator): the number of required items a level can hold at once never exceeds the starting capacity (3 + 5). Level 1 needs at most three (Notes, Case Folder, Profile Binder); with the Tax Code Book Level 1 has four items for three hotbar slots, so the bag is used naturally.

## Touch controls

Mobile first: tap is the primary control, drag is a shortcut, and every action has a visible button.

- **Tap item, then tap slot:** move, or swap if occupied. The selected item lifts with a raised outline.
- **Drag and drop:** same result as tap-tap.
- **Press and hold:** details card (name, what it does, count, required seal).
- **Quick move button:** sends the selected item between bag and hotbar (Minecraft's shift-click), first free slot.
- **Empty hotbar slot:** dashed outline with "+"; tapping it opens the inventory with that slot targeted.
- Large targets, icon plus text labels (never colour alone), and a visible Back button. Back drops any selected (not in-hand) item back where it was.

## Evidence journal

The small evidence journal from the original ticket remains, as a page reached from a button in the inventory. It lists only learned neutral story facts with links to their source records, never answers, legal conclusions or new facts created by opening it.

## Ownership and mechanics

The inspected Level 1 seam is CaseData and CaseDefinition for authoritative case facts, TaxpayerProfileCollectionHUD / TaxpayerProfileData for collected profiles, DocumentViewerManager / DocumentViewerWindow for viewers, OfficeHudRuntime for the PermanentHUD icon row and HudIconButton for icon availability. T05 supplies saved Level 1 revision/working-state identity; do not bind to Level 2's CaseReviewState or copy answer-key fields.

- HudIconButton's unlock triggers are replaced by item presence: a hotbar slot is available when it holds an item and dimmed (empty) otherwise. GameplayEvents that unlocked an icon now grant the matching item through the campaign acquisition command. FolderToHUDIcon's drop test is generalised to any slot rect (a shared slot drop target) so every carried world item uses the same drag-onto-icon path. TaxCodeBookInteractable keeps its first-open event and gains a pick-up/put-back path.
- Existing per-icon behaviours (FloatingWindowIcon, HudSubmittableIcon, HudIconDragOpen, the folder ClickOverride/DropResolver, ClientCopyHandover's folder state) attach to whichever slot holds that item, not to a fixed GameObject path. Badges travel with the item.
- Extend the v5 campaign inventory record with: hotbarCapacity, bagCapacity, hotbar[] and bag[] slot arrays of {itemId, quantity} (empty allowed), drawer[] entries, and per-item original/working document identity, revision, acquisition source and optional neutral story-fact links. Do not serialize scene references. Missing or unknown item IDs normalize to empty slots on load with one warning; required items missing from slots are restored to the first free slot.
- Inventory commands (Acquire, Move, Swap, QuickMove, Store, Retrieve, UpgradeCapacity) live in the campaign domain, validate and commit atomically, and are idempotent per action ID. Observers (HUD, inventory screen) only render snapshots.

## Clock, focus and formal actions

While the inventory or a viewer is open, movement and world taps are locked cleanly; closing returns focus to the prior safe state. The review clock keeps running (no pause for arranging items); existing compulsory guide/settings/dialogue pauses stay as they are. Formal calculation, filing, stamping and finding submission remain workstation actions.

## Visuals, motion and sound

The inventory is a paper-and-canvas satchel in the game's existing HUD language: slot tiles in two rows, the hotbar strip visually matching the real HUD row. Opening slides the panel up and fans the slots in; items snap to slots with bounded transform motion (no imported animation clips). Reuse existing paper/selection sounds, one cue per completed action, through the effects channel; no sound on failed or repeated taps. Thumbnails match their physical source.

## Teaching (Level 1)

Following the hands-on teaching style, the lesson happens when it is needed: the first time all three slots are full and a fourth item arrives (in Level 1, usually the Tax Code Book or the Case Folder), it goes to the bag and one guide step says "Your bar is full, so it went in your bag. Tap the bag and drag it to your bar." The step completes on the real move, not on reading. No arrow is shown once the Bag icon has been used. T16 composes this step into the full Level 1 flow.

## Non-goals

Do not create durability, crafting, trading, selling, item drops in the world, random loot, a generic RPG scripting system, required-evidence acquisition, the shop purchase itself (T12), bookmark pinning (T12), office decor (T13; furniture is installed, not carried), a new legal rule or answer key, multiple item copies that disagree with the case, or a Level 5 evidence-board puzzle. Do not pause ordinary review time or make document inspection a formal case action. Do not move or restyle the existing HUD row.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Hotbar, bag and drawer resolve only to owned stable IDs and quantities in the same campaign save; capacities clamp to 3–5 hotbar and 5–12 bag.
- [ ] Move, swap, quick move, store, retrieve and auto-placement are atomic and idempotent; stacks respect their limit; containers hold all profiles / case papers in one slot.
- [ ] Required items cannot enter the drawer, be discarded or be lost on full pickup; the required-capacity validator passes for Level 1 content.
- [ ] Slots are general-purpose: any item can sit in any slot, empty slots render dimmed, and dropping a world item (folder, book, Client Copy) onto a slot places or swaps it through the same drop path as today's folder; returning it to the world empties the slot.
- [ ] The Tax Code Book opens from a slot with the same TaxCodeBookUI content and raises its first-open event once.
- [ ] Hotbar items keep today's tap and drag-out behaviour (folder to tray, Client Copy to Marie, profiles to corkboard); bag-only items are readable but world use is offered through "Take it out".
- [ ] Opening a document from any slot shows the same model/content/annotations as its physical source; original and working revisions stay distinct.
- [ ] Movement/world taps lock while the inventory or a viewer is open and restore safely; the clock keeps running; formal actions remain workstation-only.
- [ ] Opening or arranging the inventory grants no rewards, trust, facts, ownership or clock pause.
- [ ] Slot layout, capacities, drawer contents, journal state and selected slot round-trip through save/reload; unknown IDs normalize without losing the checkpoint.

USER manual acceptance — only the user performs these checks:

- [ ] In Level 1, confirm the HUD row keeps today's positions: three slots (empty and dimmed at the start) plus the Bag icon at the end.
- [ ] Drag the Case Folder and the Tax Code Book from the desk onto slot icons; confirm it feels like today's folder drop, that dropping on an occupied slot sends the old item to the bag, and that the fourth item triggers the bag lesson.
- [ ] Open the bag, move Notes into the bag and back, swap two items, use quick move and press-and-hold details; confirm locked slots show the padlock.
- [ ] Put the Case Folder in the bag, then try to hand it to the Auditor: confirm the "Take it out" prompt works in one tap.
- [ ] Fill every slot, pick up a new item: confirm "Make room" appears, the drawer accepts non-required items and refuses required ones.
- [ ] Inspect a document during a live review and confirm the clock continues; quit and resume and confirm the slot layout is unchanged.
- [ ] On device, check slot target size, snap motion, sounds at the effects setting and readability of counts and labels.

## Handoff

Record the item IDs, slot record shape, command names, the HUD-slot adapter and movement/clock ownership in ../PROGRESS.md. T11 adds world pickups through the same Acquire command and full-bag flow; T12 sells the Satchel (bag 5 to 8) and Belt Pouch (hotbar 3 to 4) through UpgradeCapacity and adds the bookmark set as a supply item; T13 shows the same upgrades in the menu shop; T18 reuses the inventory for Level 2 records.

Revision 2026-10-25 (user request, see PROGRESS "Desk Drawer removed ..."): the Desk Drawer and the empty book stand were removed from play. Everything owned is carried in the bar or the bag; an older save's drawer items return to free slots on load. The Bag's third panel is now a DETAILS side panel for the chosen item (press and hold chooses it; no pop-over card).
