# T13 — Share the shop in the menu and furnish the office

Status: Planned. Blocked by: T11, T12.

## Goal

Give the menu catalogue the same purchases and furniture ownership as the world shop.

## Visible accomplishment

Before: shop state is only presented at the world counter and no office decor is installed. After: the main menu gains a shop entry backed by the same wallet and catalogue as the physical supply counter. Once introduced, the player can buy a brass pencil cup for 30 coins or a framed island print for 60 coins, then see the actual object installed in Marie's office. Each item has a physical preview and a real display position. The comparison stand teaser states “Requires ready Level 2” and the computer says “Requires corrected Level 3”; both remain visible but cannot be purchased before implementation.

## Work

Add a 3D menu shop route using the existing menu camera/board interaction patterns. Keep its entry locked until the physical shop introduction has been acknowledged. The shop view is a small timber display shelf with a hand-lettered header and individual item cards pinned beside the real model; prices and locked requirements sit on cards in world space. Selecting an item turns or lifts its actual miniature preview to face the player and opens a compact confirmation prompt. This catalogue must present the same definitions, prices, purchase result and ownership snapshot as T12's counter; no separate menu wallet or mockup-only store state.

The two Delivery A decor models are concrete: a squat brass mesh pencil cup with three coloured pencils, and a dark wood rectangular frame containing an island print. Provide distinct office display anchors: cup on the right corner of the desk, framed print on the wall above the filing shelf. Buying installs the model at its anchor with a visible snap-in motion and preserves ownership and placement after reload. If the anchor is occupied or the player moves the item, placement must have a deterministic, visible resolution rather than silently charging and hiding it. Include accessible item names/icons along with colour and shape cues.

### Source starting points

- **Existing:** `Assets/Scripts/UI/MenuEntry.cs`, `CareerBoardPresenter` and its world-built 3D card/button conventions in `Assets/Scripts/UI/Menu`; `PlayerProgressData` / `PlayerProgressManager`; the office scene and `HudTheme`/AudioManager patterns.
- **Proposed:** shared catalogue, purchase command and display-anchor records from SPEC. No current menu shop or furniture-ownership API is asserted. Integrate through menu/campaign presenters without changing current career launch behavior.

Play the menu card as a physical leaflet: it peels from its pin, rotates forward, and rests in a brass confirmation clip. The preview rotates no more than a quarter turn at a slow readable pace. After durable purchase, move the decor model from the preview shelf to its office anchor with a short 0.3-second lift-and-settle; never move a live world object before the save succeeds. Use one restrained existing select cue for opening, one coin/purchase cue for a committed sale and one soft wood/brass placement sound. Route through the configured effects volume; no looping, per-frame, or failure sounds. Shop/catalogue views pause active play consistently with other menus, and closing returns to the exact prior menu/camera state.

## Persistence and failure rules

Purchase validates story mode, introduced shop, unlock prerequisites, price and unique item ownership. Wallet debit, ownership, selected display anchor and receipt commit atomically. Failure leaves both wallet and scene presentation unchanged and can be retried; a repeated action ID cannot buy twice. Locked comparison stand and computer provide requirement text and cannot reach a purchase confirmation. The menu entry does not unlock from a tutorial page, camera visit, or old accuracy result. Furniture is installed, not placed in the bag, and cannot be sold for coins. Decor grants no trust, XP, badge, or chapter completion. All menu and counter actions read the same committed state.

## Internal checks — AI source/build/regressions only

- [ ] Menu and counter use identical catalogue IDs/prices/unlock results and persist one purchase/placement.
- [ ] Tests cover both decor items, anchor occupancy/replacement, reload, duplicate/failed purchases and lock enforcement for comparison stand/computer.
- [ ] Static inspection validates world-space card/model anchors, colliders, deterministic placement and text fit; compile plus relevant menu/shop/save checks pass.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] Before Quax introduces supplies, verify the Shop entry is locked; afterward inspect the real 3D shelf and preview, buy each decor item, and see each in its office anchor after relaunch.
- [ ] Compare coin balance and ownership at the menu and physical counter; attempt both future teasers and confirm they cannot be purchased.
- [ ] Check camera return, card readability, model scale/placement, touch targets, animation timing, effects-volume behavior and sound quality in Unity/on device. No manual scene setup is required.

## Handoff

Next ticket: [T14](T14-appointment-extensions.md) adds the bounded voucher use path.

## Non-goals and references

No Level 2 comparison stand implementation, Level 3 computer function, extra furniture set, real-money purchase, or career-board redesign. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
