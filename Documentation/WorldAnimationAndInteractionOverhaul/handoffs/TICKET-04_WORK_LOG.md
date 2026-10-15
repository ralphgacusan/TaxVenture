# Ticket 04 work log (live)

Started 2026-10-15 on user instruction. The user has low usage left, so this log is updated after every step. If you are a new session, read this first, then `tickets/TICKET-04-3D-DOCUMENT-PRESENTATION.md` and Ticket 03 sections 15-16.

## Context you need

- Only `Assets/Scenes/Office_Level1.unity` is in scope. The user does all git and Play Mode work.
- **Unity often does not reload the scene after on-disk YAML edits** (it happened twice on 2026-10-15). Prefer runtime code (Awake/Show) over scene YAML for visuals, so changes show up without a reload.
- Open documents are 3D models with world-space canvases:
  - Folder: `ConsultantOffice/InteractiveFolderObjects/OpenCaseFolder/WorldSpaceCanvas` (CaseFolderUI on the canvas). `Paper` RectTransform is 900x1150, scale (0.7,1,1), colour (0.96,0.96,0.925).
  - Book: `.../OpenTaxCodeBook/WorldSpaceCanvas` (TaxCodeBookUI). `Paper` is 900x1100, scale 1.06.
- Page flip already exists (Ticket 03 section 16): `CaseFolderUI/TaxCodeBookUI.TurnToCurrentPage(dir)` -> `DeskMotion.TurnPage`. Build on it; do not add a second transition.

## Plan

1. [x] Inspect paper hierarchy, text colours and sizes, and value-drag state API.
2. [x] Runtime printed-paper styling (shadow, stacked sheet edges, ink-coloured typography, header rule), applied by a small component added from the UIs; no data/field changes.
3. [x] Block/defer page turns while a value is being dragged.
4. [x] Compile, run regressions, update ticket/PROGRESS/README/KNOWN_ISSUES.

## Log

- (start) Log created. Nothing changed in code yet.
- Step 1 done. Findings: folder Heading/Body TMP are black (0,0,0), size 36 (heading bold); page indicator size 30. Book heading is black, size 30 bold. Paper is a plain Image with no shadow or border. `ValueDragSource` has a private `isDragging` and no global drag state. `ValueTransferManager` only exposes selection (`HasSelection`).
- Decision: add `ValueDragSource.IsAnyDragActive` (static) and gate Next/Previous page on it. Add a runtime `PrintedPaperStyle` component (Assets/Scripts/UI/PrintedPaperStyle.cs) applied from `Show()`: paper shadow, two stacked sheets behind the paper (siblings, so they stay put while the top page flips), a thin printed border frame, and ink-coloured heading/body/indicator text. Every decoration has raycastTarget=false. No scene YAML is needed.
- Steps 2-3 done, compile 0 errors. Files: NEW `Assets/Scripts/UI/PrintedPaperStyle.cs` (+ .meta). `CaseFolderUI.cs` / `TaxCodeBookUI.cs`: `Show()` calls `PrintedPaperStyle.Apply(GetPageTurnRoot(), heading, body, indicator)`, and Next/Previous return early while `ValueDragSource.IsAnyDragActive`. `ValueDragSource.cs`: static `activeDrag`, set on drag start, cleared on end and in a new `OnDisable`. Under-sheets are offset in the paper's own plane, because the folder paper is rotated about 83 degrees inside its canvas.
- Next: run regressions, then update the docs (step 4).
- Step 4 done. Regressions all PASS (outline drag checks, Controls 5,734, Dialogue 796, Progression). Ticket 04 section 15, README, AGENTS, PROGRESS, IMPLEMENTATION_PLAN and KNOWN_ISSUES updated.
- **STOPPED HERE: Ticket 04 is code complete.** Remaining: the user's Play Mode check (ticket section 15, manual checks), then the user commits. Do not start Ticket 05 without an explicit instruction.

## Round 2: printed-ink restyle (2026-10-15, user request)

The user's Play Mode screenshots show the papers still look like UI: white rectangle field rows (`CaseFolderFieldRow`) that read as input boxes, white chip buttons (`TaxCodeValueChip`) in a sans font, and on book section 2 the description text overlaps the chips. The ask: text should look printed on the paper; drop fields should look like part of the paper but still show a box/indicator that they accept drops; draggable values should look draggable; keep all data and drag/transfer mechanics; make it polished and game-like.

- (r2 start) Inspecting row/chip prefabs, the selection highlighter, and available TMP fonts.
- (r2 findings)
  - Rows: `Assets/Prefabs/CaseFolderFieldRow.prefab` has one TMP "Label: Value" (size 50, LiberationSans, black) on a white `Image`. `ValueSelectionHighlighter` paints that Image white/yellow, and `ValueTypeValidationFeedback` caches the Image colours in Awake to restore after its green/red flash. So both must learn the new resting colour, or they repaint it white.
  - Chips: `TaxCodeValueChip.prefab` (500x60 white Image + ChipText, size 50) sit inside the book `Content` VerticalLayoutGroup with spacing 160. The descriptions are separate `bodyText` lines aligned by hand with blank lines, so section 2 drifts and overlaps.
  - Every section has exactly as many description paragraphs (blank-line separated) as chips (4/4 in sections 1-3).
  - Fonts available: LiberationSans SDF, GloriaHallelujah SDF and Minnie SDF (the last two in Assets/fonts, not Resources).
- (r2 design)
  - New `Assets/Scripts/UI/PaperInkStyle.cs` (static, presentation only).
  - Row: printed caption label in grey-blue ink + value in dark-blue "pen" ink (rich text in the same TMP). Drop-target rows get a pale-blue fill-in box with a thin ink outline. Value-only rows get no fill, just a printed underline and faint grip dots (still draggable).
  - Chip: soft highlighter-marker fill, thin outline, grip dots at left, ink text.
  - Book: when paragraph count == chip count, each description is printed directly under its chip (caption + spacer siblings in Content, spacing reduced to 6) and bodyText is cleared. Otherwise the old behaviour is used.
  - Hooks: `ValueSelectionHighlighter.SetColors(normal, selected)` (re-applies the current state); `ValueTypeValidationFeedback.SetRestingColor(image, color)` (updates the cached original only).
- (r2 implemented; compile 0 errors; outline drag checks, Controls 5,734, Dialogue 796 and Progression all PASS)
  - NEW `Assets/Scripts/UI/PaperInkStyle.cs` (+ .meta): FieldText rich text, StyleFieldRow, StyleValueChip, Paragraphs, CreateCaption, CreateSpacer. Decorations live under a child named `PaperInkDecor` (LayoutElement.ignoreLayout, raycastTarget false).
  - `CaseFolderFieldRow.Refresh()`: label text is now `PaperInkStyle.FieldText(...)`, then `StyleFieldRow(rowImage, labelText, isDestination, highlighter, typeFeedback)`.
  - `TaxCodeValueChip.Initialize()`: calls `StyleValueChip`.
  - `ValueSelectionHighlighter`: `SetColors(normal, selected)` plus an `isSelected` field.
  - `ValueTypeValidationFeedback`: `SetRestingColor(image, color)` updates the cached restore colour only.
  - `TaxCodeBookUI.RenderClickableValues` -> `PrintDescriptionsUnderChips`: if paragraphs == chips, bodyText is cleared, caption + 34-unit spacer siblings are added after each chip, and `Content` spacing is set to 6 (restored to the original 160 otherwise). Captions are destroyed with the chips.
- **STOPPED HERE (round 2).** Next: the user checks in Play Mode. Likely tuning: colours/sizes are constants at the top of `PaperInkStyle`; spacer height 34 and caption margin 44 in `TaxCodeBookUI`/`PaperInkStyle`.

## Round 3: book overflow fix + bigger descriptions (2026-10-15)

User screenshots: long chip labels ("Compensation Earner", "Non-Resident Citizen", "Mixed Income Earner") wrapped to two lines and spilled out of the 50-unit strip. Section 1's last value was clipped by the list viewport's Mask (ClickableValueList is 600x750; the viewport stretches inside it, height 733). The user also asked for bigger description text.

- `PaperInkStyle.StyleValueChip`: chip text NoWrap + auto-size 24..40, right margin 10; chip LayoutElement min/preferred height 58.
- `TaxCodeBookUI.PrintDescriptionsUnderChips`:
  - Descriptions are one flowing paragraph (line breaks -> spaces), starting at `descriptionFontSize` 36.
  - `FitDescriptions` shrinks them in 2-pt steps (min 26) until `LayoutUtility.GetPreferredHeight(Content)` fits `viewport.rect.height`.
  - Spacer 14, spacing 4.
  - `SetInlineListArea` grows ClickableValueList to 700x860 with its top edge kept, only when the list area is a direct child of the Paper. It is restored when the authored body layout is used.
  - New serialized fields have defaults: descriptionFontSize, minDescriptionFontSize, inlineListSize.
- `RenderClickableValues`: old chips/captions are SetActive(false) before Destroy, so they don't count in the same-frame layout measurement.
- Compile 0 errors; ControlsRegression and outline checks PASS.
- **STOPPED HERE (round 3).** Next: user Play Mode check of book sections 1-3 (no overflow, labels on one line, readable descriptions). Tuning: inlineListSize, descriptionFontSize on the book's TaxCodeBookUI; ChipFontMax/ChipHeight constants in PaperInkStyle.

## Round 4: drop validation visuals + centred folder (2026-10-15)

User asked: keep the validation rule (same kind of value = green even if the answer is wrong; different kind = red) but make the feedback look better and more game-like; make the case folder contents wider and centred, with nothing else changed.

- Rule unchanged: `ValueTypeValidationFeedback.IsCorrectValue` (type + semantic key, i.e. the field's category). Callers: CaseFolderFieldRow, ValueClickDestination (computer form), ComputerFieldSlot (ForceRestore).
- NEW `Assets/Scripts/UI/DropFeedbackFX.cs` (+ .meta):
  - Check/cross badge pops in at the field's right edge (overshoot, hold, fade; 0.95 s).
  - Accepted drop: scale punch. Rejected drop: decaying rotation wobble. Scale/rotation only, so layout groups are unaffected.
  - The edge Outline glows in the result colour and is then restored.
  - Restore() on finish/disable.
- `ValueTypeValidationFeedback`:
  - Old `correctColor`/`incorrectColor` (pure green/red) replaced by `successColor` (0.33,0.76,0.5) / `errorColor` (0.92,0.37,0.35) and `tintStrength` 0.7. The new field names mean old serialized pure colours no longer apply.
  - The FeedbackRoutine is now a smooth pulse (rise in 15%, SmoothStep back) over feedbackDuration+0.3 s, and calls DropFeedbackFX.
  - Auto-detected targets skip `DropFeedbackBadge`/`PaperInkDecor` children. ForceRestore also restores the FX.
- Compile 0 errors. NEXT: centre/widen the folder layout in `CaseFolderUI` (all row lists are Paper children 650 wide at x+50; heading is top-left at x 220; LeftEdgeZone is 150 in from the left vs RightEdgeZone 45).
- Folder centring done: `CaseFolderUI.CentreContentColumn()` runs once from Show(), and only on the world-space canvas. A second CaseFolderUI exists on HUD_Canvas/CaseFolderPanel and is left as is.
  - Heading, all page row lists (page1/2fixed/2/3/4/5/7), supportingDocumentsPageLink and pageIndicator are re-anchored to centre-x at x 0. Lists and heading get width `contentWidth` 760 (serialized; was 650 at x+50). The heading is centre-aligned.
  - LeftEdgeZone is mirrored to RightEdgeZone's x (45 in from the edge; was 150), so the wider column isn't under the tap strip.
- Compile 0 errors. Outline checks, Controls 5,734, Dialogue 796 and Progression all PASS.
- **STOPPED HERE (round 4).** Next: user Play Mode check (drop the right/wrong kind of value; look at the folder layout). Tuning: colours/tintStrength on ValueTypeValidationFeedback, constants in DropFeedbackFX, contentWidth on the 3D folder's CaseFolderUI.

## Completion

- 2026-10-15: the user confirmed Ticket 04 in Play Mode and marked it complete. Docs updated. Ticket 05 NOT started (the user asked to wait).
