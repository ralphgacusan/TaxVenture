# World, physical assets and feedback

## Visual direction

Match Level1's existing low-poly office/plaza materials, lighting, scale and touch interactions. Reuse current character rigs, portraits, desk/book/folder/stamp props, chairs, cabinet, paper handling and supported NPC clips. All new tools and environment items are genuine Unity 3D geometry with renderers and suitable colliders. Existing world-space text/graphics may print labels onto physical paper; the market desk cannot be a full-screen flat UI substitute.

The AI implements and wires these assets. Do not leave instructions telling the user to manually create models, assign clips, place the stall, add colliders or connect UnityEvents. Source builders are acceptable if scene/runtime assembly produces the playable result automatically. Document deterministic reproduction and remove no user content.

## User-selected source assets

These selections supersede earlier generic character-placeholder and scratch-built stall recommendations.

- **Aling Rosa:** `SM_Chr_Business_Female_03` under `TaxVentureWithCompliance > Characters`. Source scene: `Assets/Scenes/TaxVentureWithCompliance.unity`. Reusable prefab: `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Female_03.prefab` (GUID `f78a881d3ec75d945ac878527dc26674`). Use its existing mesh/materials/rig and compatible idle/talk/offer/reaction animations; do not leave Rosa in the reference screenshot's T-pose. Any temporary portrait must remain clearly labeled as temporary if it does not match this model.
- **Rosa's store near the plaza:** the red-and-white awning store selected as `Building_4 (1)` under `TownEnvironment > Env > Town`. Source scene: `Assets/Scenes/TownEnvironment.unity`. Reusable prefab: `Assets/Flat_Style_Vehicles/Prefabs/Buildings/Building_4.prefab` (GUID `4c701139b81d9c140b18bb741804c1ec`). Inspect that particular scene instance for transform/material/component overrides before instantiating the prefab; retain its depicted appearance. The source scene also contains another `Building_4`, so do not select by partial name alone.

Instantiate/reuse these assets in Office_Level2. Preserve both reference scenes and original prefab assets. Do not import their whole scenes or move their source instances. Add only local signage, relevant laundry props and the nearby records desk needed for gameplay. Do not replace the selected shop with a newly modeled generic stall or extra canopy.

## Scene placement contract

Create the new Office_Level2 entry scene once from current saved Office_Level1 as specified in SCENE_REUSE.md; keep the source scene and Office_Level2_OLD untouched. Preserve the inherited routes and office rooms, and set the Level2 spawn facing reception. First inspect actual usable plaza-side space. Place the selected Building_4 store in usable space beside a plaza path, not in the route itself, with clear office visibility/signposting. Record final world transforms and parent paths in PROGRESS after implementation; do not invent coordinates before inspecting the scene.

Suggested stable names under `Level2Gameplay`: `RosaStall`, `RosaEncounter`, `MarketRecordsDesk`, `IncomeToolkit`, `CaseRetrieval`, `Level2Waypoints`. Existing office desk/rooms stay in their authored hierarchy. Runtime additions must be idempotent and scoped.

Store geometry comes from the selected Building_4 asset: inspect its actual bounds and fit it to the available plaza-side space while preserving proportions and the red-and-white awning. The earlier 2.4 x 1.8 m generic-stall footprint is superseded; do not force the building into those dimensions. Add a `Rosa's Laundry` / `Stall 04` sign and a few folded laundry bundles. The records worktable may target about 1.4 x 0.7 m at 0.78 m high, adjusted to the reused desk and player scale. New small additions may use Unity primitives/procedural geometry. No cloth physics, merchandise simulation or baked new navigation system.

Leave roughly 1.2 m clear approach space and a clear path around the desk; treat dimensions as layout targets subject to actual player collider/camera. Avoid canopy/posts obscuring Rosa's dialogue framing. Rosa stands behind/beside the stall; records desk sits beside her, within a few steps, facing an unobstructed view. Guide NPC uses an existing model beside the desk, outside the work-camera view; reuse Arthur's portrait for teaching.

Waypoint coverage: receptionist, labeled cabinet drawer, consultant desk, Rosa, market records desk, return desk, Auditor, completion meeting. Ground arrows lead to destinations, never highlight correct income lines or finding answers. Minimap marker changes with actual phase and hides under existing modal rules.

## Asset and animation list

| Asset | Build/reuse | Required motion/feedback |
|---|---|---|
| Cabinet retrieval | Reuse cabinet, add separate readable label and folder pickup | Correct drawer slides 0.15–0.25 m, paper/folder lifts into inventory; quiet wood/paper sound; no archive event |
| Rosa/guide | Rosa: selected SM_Chr_Business_Female_03; guide: existing Level1 model; rig-compatible clips | Idle, face speaker, short talk gesture, document offer, relief/worry reaction; transform rotation or existing gesture clips, no new walking AI |
| Market workstation | Selected Building_4 store; reuse desk/chair beside it; add only needed signage/props | Existing workstation settle and exit; body movement/look locked only while working |
| Envelope | Thin cuboid/slightly layered mesh, flap mesh | Slides from Rosa's side into document tray; fans records out once |
| Records D1–D6 | Reuse physical paper renderer; distinct printed layouts | Lift/select/tilt, readable close-up, compare pair, stack/return; no paper physics needed |
| Toolkit case | Small 3D box, lid and internal slots | Lid rotates open ~100 degrees over ~0.45 s; markers/notes revealed; reverse on completion |
| Marker | 3D barrel, tip/cap; shared mesh with color variants if useful | Selected marker lifts, tip traces selected fixed line ~0.2 s, translucent stroke remains |
| Classification notes | Thin 3D paper cards with readable text and icons | Lift on select, snap to document socket with ~0.12 s settle; invalid target returns softly |
| Evidence clip | Small 3D folded strip/two-piece clip | Clips selected record reference to annotation; brief paper tick, clear reference label |
| Office finding note | Reuse note/clip meshes | Note attaches to relevant folder page; updated conclusion changes text, no correctness sparkle |
| Physical stamps | Reuse current stamps | Pick up, align, press, impact, slight rebound, leave ink, return; reason must be chosen first |

New mesh names, materials, prefabs/builder paths and scene bindings must be listed in the implementation handoff. Prefer shared materials, no per-frame material instantiation, and deterministic cleanup. Reuse available audio through AudioManager; if no suitable clip exists, generate one short local impact/rustle asset using code rather than adding a package or downloading art. Sound unavailable is recorded, not silently replaced by repeated generic confirmation audio everywhere.

## Mobile annotation interaction

Tap a document entry to inspect; tap its fixed line to highlight. Tap/tap placement is the accessible default, drag placement optional using the same semantic interaction. No tiny precise strokes or timed gestures. Show one document with up to two note slots and one evidence slot; related records can fan/compare but never bury exit/book controls. Logical touch zones should project to about 44–48 screen points minimum on reference mobile layout; actual legibility awaits user test.

The highlighted line, type note, treatment note and evidence label remain visually distinct. Colors supplement words and shapes: source stroke, type tag, treatment tag, clipped reference. Do not require remembering five marker colors from the original storyboard. Keep the satisfying toolkit and colored annotation feel while reducing the interaction vocabulary.

Unscored placement feedback: small snap, paper tick, `Placed` label. Correctness feedback appears only at the agreed quest check or Auditor review. A green target means compatible destination, not correct tax answer.

## Satisfying stamping sequence

1. Select Ready or Not Ready using existing physical models; show selected name and chosen reason on assessment slip.
2. Drop on the generous assessment zone (or tap stamp then zone). Invalid zones return without a penalty or ink.
3. Align over ~0.12 s; compress/press ~0.10 s; impact with one short dry stamp sound; rebound ~0.15 s; return ~0.25 s. These are tunable initial timings.
4. Ink appears exactly once at impact, with verdict, day/revision and brief reason. Use a paper-space decal/mesh or existing world-space print surface so ink follows the page. Do not paint a screen-space overlay floating in front of the folder.
5. The paper gives a tiny settle; optional subtle local camera response must respect reduced motion and must not jerk the look controller. No exaggerated screen shake.

Atomic state commit occurs once at impact after validating the selected reason and packet revision. During the gesture ignore repeated taps. Exit, pause, deadline and scene teardown must complete or cancel cleanly with no half-committed stamp. Never decide tax correctness from animation completion. Neither color nor celebratory sound reveals whether the chosen verdict is correct.

Preliminary imprint is small `Initial review — Not Ready`; final imprint is prominent and clearly distinguished. Restamping does not layer unreadable marks: keep one current imprint and a compact prior-review history. A changed packet visibly marks its final stamp `Needs reassessment` without hiding past work.

## Completion feel

Quest: corrected papers fan into an annotated spread, clip together, slip into folder/envelope, toolkit closes, then existing medal result appears with receipt emblem and count `X/12 on first review`. No case total, omission answer or ready stamp on this screen.

Case: Auditor receives the folder through existing physical hand-in. Approval gives a restrained nod and stamped review response, then existing-style fade/finale, Rosa's short reaction, story note and Level Complete. A return gives a paper note pointing to one concern, not the same triumph animation. Retain reward-card sequencing guards from current CaseFinale to avoid empty frames.

## Presentation acceptance

Source checks establish colliders, materials, animator/transform bindings, unique anchors, object counts, cleanup and serialized content. They cannot establish readable phone text, sound feel, world occlusion, route comfort or satisfying timing. Record those as user Play Mode checks, never infer visual acceptance from a compile.


## Compact Findings presentation — 2026-10-18

Preserve shared paper focus/dragging and return-to-folder animation, with a smaller slip aligned to the source folder's plane relative to the camera. Use a short title, one sentence, three large Entry to check/Clue/Reason fields, a quiet result and a short Notes-font handwritten observation. Remove the comparison button, long introductory/status prose and exaggerated free-standing tilt. Auto-check complete drafts, using `Needs checking` or `No link yet`; partial drafts stay neutral. The book has only two brief added leaves, Reading Records and Review Notes, with no level-number headings.

Keep field instances alive while values update. Reuse the existing transfer pulse/check-cross badge, hover underline and aiming frame; tint means the kind of source fits, not that the overall finding is correct. Opt-in per-point coverage lets exposed slip fields receive taps and drops when only another part is under a book. Covered points still belong to the higher document. Attached fields remain noninteractive. User checks angle, text fit, target alignment and animation feel in Unity.


Latest refinement: the paper's upper edge is narrower and laid back; its lower edge sits forward. Apply the same taper to controls and their hit rectangles. All captions remain above values. Keep the small readable slip, sparse copy, automatic quiet validation and conditional handwritten explanation. Runtime shop, records and conversation now use Rosa's Laundry and the paid blanket job. Check the angle beside both folder and book in Unity; source verification cannot establish a visual match.


### Latest paper/stamp/cabinet polish — 2026-10-18

Give the handwritten observation a 198-unit block at fixed size 28, extending the sheet below it rather than shrinking the text. Lifting grows only 6 percent from the projected attached size and preserves the folder plane with a slight peel; filing interpolates from the current pose. Stamp hover uses the existing outline; tap focuses without submitting. A stable camera-plane drag and matching filtered hit surface keep stamp size and pointer targeting consistent, with an exact ink preview on the front page. Release aligns, presses, makes a subtle uniform impact pulse and paper thump, rebounds, then returns. The cabinet replacement face is thin, dark and fitted to its drawer row. User Unity/device verification remains required for text fit, matching angles, pointer accuracy, outline visibility, render cost, physical contact and cabinet alignment.


Latest desk follow-up: measure handwriting height at unchanged size 28 and grow the lower paper/footer as needed. A free desk drop settles the loose face without moving its drag coordinate frame. Failed links empty the three boxes; the short feedback remains until the next edit. Give replacement stand models the desk outline and expanded handle/body target. Render lifted stamps in their isolated front layer throughout aiming/pressing so the table and props cannot hide them. Page turns finish before stamp lift; cancellation restores the paper pose. The book follows the grabbed point on its lifted plane; opening aligns visible model centres. Arthur's hint eases to the top in desk/profile-reading modes. Verify these in Unity and on device; no measured performance result is claimed.


Stamp aiming follow-up: both raised floating slots remain above the desk's front lip. Keep the model in the near-camera tool plane through aim and press, with a moderate turn toward the paper and the pad above the imprint. Project the paper contact into this visible plane; stamp ink remains anchored in the actual folder. Capture the isolated render after motion and map its viewport explicitly. User verifies the complete model rather than only the ink indicator.


Latest stamping refinement: a single tap opens the set, and touch release cannot close it again. Stand targets use actual EventSystem pointer-down hit surfaces over the real models. Keep the complete held stamp within the camera's actual viewport and ahead of its near clip, below the coach band; visual edge correction leaves the ink target unchanged. Hold/press keeps front ordering without a scale pop. Stable per-tool isolated renderer layers improve capture ownership, and restore on return/cleanup. User checks visibility and touch feel at every folder edge, including letterboxed views.

### Shared stamp capture and centred folder button — 2026-10-19

The latest stamp implementation supersedes the earlier separate layers 29/30: both models use one tool capture on layer 29 and the same outline refresh. This avoids asymmetric capture/front ordering and uses one render texture/camera for the set. At impact, project the actual pad centre onto the ink point and preserve that contact through the pulse and paper dip. Aim keeps a small hover gap; press closes it. Verify both Ready and Not Ready visibly cover the folder while floating, aiming and pressing, including overlap with book/notes.

The folder button centres the visible paper at the desk. Drag placement retains the chosen position; the entrance animation is unchanged. User checks endpoint, original entrance feel, touch highlight and pad/indicator/ink alignment; internal tests cannot establish these visual results or device frame pacing.

