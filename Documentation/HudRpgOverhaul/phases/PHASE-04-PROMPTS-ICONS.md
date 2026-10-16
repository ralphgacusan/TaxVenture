# Phase 4 — Interaction prompts + HUD icon badges

Status: Implemented (2026-10-16), awaiting your Unity check

## Goal

Make interaction prompts read like a game, and turn the raw black counters on the icon buttons into proper badges.

## Scope

- `InteractionPromptUI` restyle (keeping serialized fields and its `ShowPrompt`/`HidePrompt` API):
  - Parse `"Click to <verb> <object>"` into a key glyph (a tap icon on touch, a mouse icon on desktop) plus a verb chip and object name, e.g. `[tap] Talk · Auditor`. Plain labels without "Click to" (`Printer`, `Colleague`) show as a name-only tag.
  - A rounded navy pill above the joystick zone, with a fade/scale-in of 0.15 s.
- Icon counters (`FolderCountText`, the profile `CountText`): become a pill badge in the top-right corner of the icon, cream on brass, with a pulse when the value changes. Same text components, restyled at runtime.
- No change to which interactables show prompts or when.

## Acceptance

- Every existing prompt string still shows the correct meaning (mapping table verified in an internal test).
- Badges stay legible and do not overlap neighbouring icons at all supported aspect ratios.

## What was built (2026-10-16)

- **`HudPrompt`** (new, runtime-built next to the old `PromptPanel`, outside the fading HUD so desk prompts still show):
  - A rounded navy pill with a brass edge at the bottom centre (same spot as before), scaled ×1.15. It fades in and scales up over 0.15 s, and fades out over 0.12 s. A new text while showing gives a small pop.
  - `"Click to <verb> <object>"` becomes a glyph, a brass verb chip and the object name: `[tap] TALK  Auditor`, `[tap] SIT  Desk`, `[tap] PRESENT FINDINGS  Client`. "to", "at" and "the" are dropped.
  - The glyph is a tap ring on phones / touch-only devices and a mouse (left button filled) on desktop.
  - Plain names ("Printer", "Colleague", "Case already archived.") show as a bold name-only tag.
  - "Finish Side Quest 1 first" shows with a grey padlock glyph and muted text.
  - Widths follow the text; very long names shrink to fit (max 560 units).
- **`InteractionPromptUI`**: same serialized fields and `ShowPrompt` / `HidePrompt`. It still writes the text to its own label, then lets the pill take over through `ShowOverride` / `HideOverride` (null outside Office_Level1, so other scenes keep the old panel). The Interactor still decides when a prompt shows.
- **`HudIconBadge`** (new): the Case Folder `FolderCountText` and the profile `CountText` become a pill badge in the icon's top-right corner. It grows to the left so it never leaves the screen and pulses when the value changes. A plain "0" hides the badge; "0/4" still shows. The game keeps writing to the same TMP texts; the badge only moves and restyles them and restores everything on destroy.
  - Style note: a navy pill with a brass edge and cream bold numbers instead of cream on brass, because cream on brass is hard to read.
- **`HudTheme`**: new procedural `Tap`, `Mouse` and `Lock` sprites.

## Verification

- Compile: 0 errors / 23 baseline warnings.
- HUD check: **853 PASS**. The new checks map all 26 prompt strings the game uses (and fail if a new "Click to …" literal appears without a mapping). They also cover: the API and fields kept, Interactor timing unchanged, the 0.15 s fade, no touch blocking, idle when still, the right glyph per device, override cleanup, badge show/hide rules, the badge growing left and clearing the icon above (badges poke 4 units above an icon; icons are 20 apart), the restore, and installation.
- Controls 5,734 / Dialogue 796 / Progression PASS.
- Glyph shapes rendered and checked (tap ring, mouse, padlock).
- Play Mode NOT RUN.

## Manual check (Unity)

1. Walk up to the Auditor, Receptionist, Colleague and Printer. The pill reads `TALK  Auditor` etc. A spoken-to NPC shows the name only. The pill fades/pops in and fades out when you look away.
2. At the desk, the Computer / Stamps / Tax Code Book / Case Folder prompts show as pills (the rest of the HUD stays faded there, as before).
3. Before Side Quest 1 is done, the receptionist and desk show the padlock prompt.
4. In the Editor (mouse) the glyph is a mouse; on a phone or in the Device Simulator it is the tap ring.
5. Collect profiles: the profile badge reads 1/4 … 4/4 and pulses on each change. Store the Case Folder in its icon: the folder badge appears with 1 and pulses; when the folder leaves the icon, the badge hides.
6. At 16:9, 4:3 and phone width: badges stay readable, inside the screen, and do not touch the neighbouring icons.

## Fix: prompts never appeared (2026-10-16)

Cause: `Interactor` cleared its focus, and so hid the prompt, on every frame where the joystick was held or the pointer was over any UI. While you walked, no prompt could ever appear. It only showed if you stood still and aimed the screen centre at something within 2.5 m. That was true of the old grey box as well.

Fix: looking at an object now keeps its prompt and outline while walking or with the pointer on the HUD. The interact tap is still blocked over the joystick and UI, exactly as before.

Correction to the manual check above: the desk view has no prompts. The `Interactor` only runs while walking, and desk items use their own outline and tap, so step 2 does not apply. Prompts appear when the screen centre is on an interactable within 2.5 m: NPCs, the desk, the corkboard and the filing cabinet.

## Revision (2026-10-16): mobile prompt

TaxVenture is a phone game, so the prompt always shows the **tap** glyph (a padlock when locked), never a mouse. New look (`../prompt-mobile.png`):
- a larger navy plate with a round cyan tap button that softly pulses;
- two lines: a brass caption **TAP TO SIT** over the name **Desk** in large cream bold;
- locked prompts show a grey padlock button, a coral **LOCKED** caption and the reason;
- plain names show alone.

## Revision (2026-10-16): themed HUD icons + desk documents

- **Icons** (`HudIconArt`, drawn in code; preview `../hud-icons.png`). One navy tile with a bevelled brass rim and a top gloss, with a distinct picture on each:
  - Settings: a cream gear.
  - Notes: a ruled notepad with a red binding and a yellow pencil.
  - Case Folder: a manila folder with a paper and a red case label.
  - Profiles: two stacked blue taxpayer ID cards.

  Notes and Profiles no longer share a sprite. Same buttons, handlers and count badges.
- **Tax Code Book and Case Folder pages** (`PrintedPaperStyle`, `PaperInkStyle`; preview `../documents-preview.png`):
  - warm paper;
  - a tinted header strip with a brass rule under the title;
  - a clean print font instead of the old italic;
  - round navy page-turn arrows with brass rims;
  - a red bookmark ribbon on the Tax Code Book and a navy "CASE FILE" tab on the folder;
  - value tags as rounded brass-edged tags with a grip and a soft lift shadow (brighter when selected);
  - fill-in fields as rounded blue slots;
  - field captions in small slate, values in bold pen-blue, blanks in italic grey.

  Tags and rows pop onto the page in a stagger when a page opens or turns (`PaperPopIn`). Data, layout, drag / drop and validation are unchanged.
- **Drag ghost** (`DragGhostSkin`): the floating value is a navy tag with a brass rim, a tag hole and a lifted shadow. It pops up when picked up, floats just above the finger so the thumb never hides it, and leans into the drag direction.
- **Exit Desk / Exit Corkboard:** a navy pill with a brass rim and cream capitals.
- **Debug text removed:** the "CurrentMode: …" label (top left) was a leftover `OnGUI` in `CameraController`.

Manual check:
1. At the desk, open the Tax Code Book and the Case Folder. Both look like the preview, the values pop in, and the arrows turn pages.
2. Drag a value from the book to a folder slot. The tag pops up above your finger and tilts as you move. Dropping works exactly as before.
3. The HUD icons are four different pictures; the folder and profile counts still show.
4. There is no "CurrentMode" text at the top left.

## Revision (2026-10-16): long fields, Settings placement

- **Case Folder fields:**
  - A field that fits on one line stays "Name   Marie Bautista".
  - A field that would wrap now puts the caption on its own line and the value below, for example "Taxpayer Classification" on top and "**Resident Citizen**" underneath. The value no longer breaks after the caption.
  - The row grows to fit (the label already sizes itself to its text).
  - Implemented in `PaperInkStyle.SetFieldText` / `FieldTextStacked`, used by `CaseFolderFieldRow`.
- **Settings button:** when the minimap is hidden (desk, corkboard, or a screen too small for the map), Settings slides to the top of the right icon column, centred on the icons. The icons stay exactly where they are, keeping the gap. When the map shows again, Settings slides back to the left of the map (`OfficeMinimapHUD`).

Manual check: at the desk, Taxpayer Classification / Taxpayer Type / Income Sources show the caption above the value. The gear sits at the top right above the Notes icon, and the icons have not moved. Leave the desk, and the gear slides back beside the minimap.

