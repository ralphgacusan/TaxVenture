# World, interaction, animation and feedback direction

Status: Planned. These are implementation targets, not claims of rendered or device-tested behavior. Apply alongside SPEC.md and each ticket; the user's manual results determine final framing/feel acceptance.

## Overall want

Make the office feel increasingly like Elijah's workspace while the tax tasks remain readable and learnable. Use the game's existing low-poly office/plaza, navy/brass/cream HUD, paper grain, handwriting accents, pins, folders and supported rigs. The new campaign should look like an evolution of current Level 1, not a new mobile shop app layered over a different game. Leave the existing introductory terminal and required tax tools usable for free.

Use THREE distinct signals: star/XP for mastery, heart for Public Trust, coin for office-budget currency. Do not draw old accuracy-score hearts alongside Public Trust. Label trust 'PUBLIC TRUST' with actual saved value and future support target. The target line reads 'Community support target: 65' under the starting fixture, not 'Ending unlocked'. Use labels/icons as well as color. Only animate actual committed changes, never an estimated state that might fail to save.

## Menu and modal hierarchy

Keep the 3D Menu room, moving-camera career board, physical tutorial binder and settings board. Add menu choices as matching readable physical buttons/labels. Targeted views must have obvious Back actions and respect current scene fade/camera-transition input blocking. Menu shop uses the same product cards and catalogue rules as the physical supply counter, fitted to the 3D menu presentation. A menu purchase is a real shared purchase, not a separate mock shop with its own wallet.

Main menu notes (user revision 2026-10-22): every main-menu action is a blue pinned note with a red pin in the modelled Settings note's style. NEW GAME / CONTINUE are runtime copies of the modelled settings_button, sized from the meshes to two equal columns in PLAY's row on the corkboard; PLAY and the copies swap by save state with no leftover gaps or colliders. The menu's Shop moves into the progress module (see SPEC menu revision).

Career uses newly earned mastery badges/times/XP, story completion, wallet and global trust as separate information groups. There is no old-progress archive screen or imported achievement history. New Game confirmation names the progress being replaced and the settings being retained. Continue carries a concise chapter/task subtitle only for the updated campaign. Empty/locked/error states are designed states, not blank cards: 'Start the updated story', 'No story to continue', 'Complete your first client case', 'Requires Chapter 3 completion', 'Purchase not saved; nothing spent'.

Priority: outgoing scene fade > destructive confirmation > result/dialogue/compulsory teaching > active shop/bag paper ownership > ordinary world input. Opening a bag/view/shop must not put a second document, dialogue or settings controller in charge of movement. Close/back restores previous ownership once, not indiscriminately enabling the player during another modal. Never let a tap intended for a Back button activate a world object behind it. Shop closes/backs out before main-menu navigation.

## Physical placement and source reuse

- Reuse authored office/plaza and current paths. Do not invent exact world coordinates in advance or move existing content to fit new props. During implementation inspect renderer bounds, player collision, sight lines and the existing camera frames. Record actual resolved anchor paths/transforms in PROGRESS.md.
- Supply counter is near the office on an accessible plaza-side edge, outside office doorway swings and required routes. Existing sources include Assets/Synty/PolygonTown/Prefabs/Props/SM_Prop_ShopCounter_01.prefab, SM_Prop_ShopShelf_01.prefab and existing office/shopkeeper art. Inspect actual assets first. Instantiate scoped copies; never change original prefabs. Add a clear 'OFFICE SUPPLIES' sign, product preview area and one interaction target, not a new retail simulation.
- Put optional postcard and supply voucher on a low-clutter reachable public consultation/display surface. Give a collectible outline/icon only when available. They cannot sit on a required case value or overlap an office-gate interaction. A small display stand/plaque is sufficient; no secret pixel hunt.
- Pencil cup and framed island print have two named office display anchors. Owned art appears on reload without repurchase. Frame is visibly different from existing wall clutter. New keepsake has one distinct shelf/desk slot. No free-placement editor or physics stacking is needed.
- Root runtime additions under a single named Campaign Foundation root in each supported scene; resolve anchors through configuration and guard duplicate bootstrap/scene reload. Existing feature roots remain intact. Record missing anchors with an actionable error; never silently build a second unrelated office.

## Interaction and motion contracts

Times below are editable feel targets, not acceptance evidence. Use existing easing and unscaled motion where a menu is paused. Animations never commit purchases/rewards at their end; commands already own those commits.

| Feature | Choreography | Feedback and safety |
|---|---|---|
| Profile/document pickup | Inspect first; paper lifts, tilts and flies toward bag over roughly 0.3-0.5 s | Name/category toast and one paper cue after commit. Save failure leaves collectible available; resume does not replay the award |
| Bag | Bag/binder silhouette lifts and four divider tabs fan out; selected case pack opens as a paper stack | Existing floating windows render papers. Tabs have icons/text, selected state and empty descriptions. Back gathers sheets and releases ownership |
| Bookmarks | Pin up to three collected records; small colored tabs attach to their paper edges and quick-access strip | A tap opens the original viewer/source state; third-party cases are read-only. No duplicate field values or answer key. Unpin folds tab away |
| Board check | Player fills four draft areas; the board checks itself once the fourth card is pinned (user decision 2026-10-24; one non-revealing guide heads-up at three cards; the HUD profiles icon is empty while the cards are in the tray) | Neutral placement sound before check; the draft line only says how many remain. Correctness marks, Arthur's explanation and result cues only after the committed complete check. Corrections preserve frozen tier |
| Trust change | Heart marker settles, bar eases to committed value; concise reason tag names the action/person | Reductions animate as well as increases. No constant warning pulse at low trust; support tick and label remain readable |
| Coin award | One short grouped coin flight from completed activity/result to wallet, 0.4-0.7 s | Wallet shows exact grant, then actual balance. Never play one event per coin. Skip/close does not cancel the grant |
| Counter/shop | Existing workstation camera settles; product card rises beside physical preview; Buy button depresses | Confirmation says price, effect, ownership and carry/drawer destination. Disable duplicate confirm while commit is in progress |
| Purchase success | Ownership check/stamp appears; preview object slides toward installed position or bag; short receipt | Single purchase sound through SFX setting. Failure uses readable notice and gentle return, no deduction or success sparkles |
| Voucher | Inspect labeled appointment card, confirm extension, card presses into a review-slot stamp | Clock gains +3:00 active time on committed success. Quantity and used-this-case update together; invalid use gives reason and no consumption |
| Office supply drawer | Drawer slides out, stored voucher stack fans just enough to select a quantity, selected cards lift toward bag | Show carried/stored counts and remaining carry space; transfer only after atomic success. Full carry or save failure leaves both counts and props unchanged |
| Decorations | Frame/cup settles into its named slot with a short lift-and-place motion, roughly 0.3 s | Returning to office shows the same selected piece already installed. Duplicate views do not duplicate props |
| Keepsake | Postcard flips to reveal short character story; Collect and Display are distinct actions | No public-trust reward for collecting it. Large close target and persistent 'collected' state |
| Comparison stand (B) | Two physical source sheets clamp side by side, settle independently; adjustable reading focus, not arbitrary resizing | Original free viewer/comparison still works. Stand never highlights correct evidence or submits findings |
| Guide demo | REMOVED 2026-10-25 (T15 reverted): no in-person demonstrations or practice fixture; Arthur teaches through the guide box and his in-person corkboard talk | Do not reintroduce |
| Wrap-up | Actual reward breakdown, client follow-through, optional supply-counter visit, final story receipt | No purchase obligation, automatic scene load under a live shop, doubled reward card or hidden modal fighting Continue |

## Guide identity (Quax reverted)

User decision 2026-10-25: Arthur stays the guide; T15 (Mayor Quax, practice board, demonstrations) was reverted, so the rest of this section is historical. Was: Arthur is the old guide, Elijah is the player, Quax is the new guide. Replace active new-campaign names, signposts, talk text, portrait bindings and demonstration actors consistently; historical source/reference documents need not be rewritten. Do not rename serialized identifiers or scene paths merely to change presentation. Prefer configurable speaker/actor identity so existing guide targets and practice lessons keep their contracts.

No Quax-named final model/portrait was located in the inspected sources. At execution, search again. If no supplied approved art exists, use a clearly recorded provisional existing rig with local mayor identification (for example a named badge) to implement and test functional posing. Do not claim a renamed Arthur portrait is final Quax art. Final model/portrait approval remains an explicit manual/art gate; it does not justify leaving interaction assembly to the user.

Quax demonstrates only a separate sample. In L1 he can present a fifth practice profile, open the bag with the player, demonstrate the new check action, and show a catalogue example before Elijah chooses an optional purchase. Do not place a required taxpayer correctly during a sample and then grade the copied answer. Most demonstrations are in person at stable anchors; no new walking/pathfinding system. During independent work he steps aside, stays reachable, and does not obscure papers or call out the solution.

## Sound, readability and accessibility

Route paper lift/return, card placement, confirm/error, stamp, pickup and coin cues through existing AudioManager/AudioLibrary and saved SFX preference. Search supplied clips and map intentional events; document actual selected names during implementation. No new direct play-on-awake sources that bypass SFX OFF. Music/narration choices are not reset by New Game. Do not introduce generated narration assets in this scope; preserve any existing voice settings/hooks and record missing recordings separately.

Use touch wording, readable sans-serif body text, handwriting for short accents only, high contrast, text/icon category cues, sufficient touch margins and safe-area layout. The user tests portrait/wide/tall layouts; source checks only establish anchoring declarations, not readability. Keep token/coin currency visually distinct from PHP tax document values. Independent document reading consumes review time; shop/compulsory teaching/settings pause according to SPEC, with clear resume behavior. Animation flourishes must be short and skippable where existing result controls support it; nothing should demand rapid dragging or reading while a required lesson clock runs.
