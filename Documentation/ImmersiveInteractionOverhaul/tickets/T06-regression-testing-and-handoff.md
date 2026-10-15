# T06 - Regression evidence and final handoff

Status: **AUTOMATED EVIDENCE RECORDED (2026-10-15); all manual/device rows pending user.** The overhaul is NOT declared complete.
Dependencies: T05 integrated result. Next: final user review. Rough estimate: **8k-14k tokens**, excluding manual/device waiting and newly discovered fixes.

## Objective and existing verification seams

Confirm the implemented experience and leave evidence that another agent can trust. Re-inspect actual implementations and reports; never label a proposed feature or stale report as a pass.

Relevant checks: C# compilation and Git diff checks; Tests/VerifyProfileCards.ps1; Tests/ProgressionRegression/ProgressionRegression.csproj (PlayerProgressData only); Assets/Editor/CorkboardRegressionValidation.cs, PaperDropValidation.cs, PaperCollectionValidation.cs, PaperTrayValidation.cs; any focused tests added by T01-T05. Do not assume these cover new dialogue or camera behavior.

## Verification matrix

Record each as PASS / FAIL / NOT RUN, method, date, scene, resolution/device and evidence. Include actual results and reproduction steps, not this checklist alone.

- [ ] Movement joystick/right-side screen-swipe look indoors/outdoors; analog/dead zone; multi-touch; release/cancel/focus loss; desktop fallback; swipes never become unintended world taps.
- [ ] Normal first-person exploration and T01 controls before and after dialogue; conversation OTS presentation is active only during an NPC conversation.
- [ ] Shot/reverse-shot roles in office and town: NPC line over Elijah's shoulder with NPC face visible; player line over NPC's shoulder with Elijah face visible. Check tight spaces and document any dialogue-only two-shot fallback.
- [ ] Existing Elijah rig/assets, units, native geometry, scene references and foot grounding; verify the conversation visual does not move/teleport the gameplay player root, resize its collider, change shared NPC importers or add NPC animation.
- [ ] Movement/look stay frozen during dialogue; natural end and cancellation restore the captured first-person yaw, pitch, FOV, hands/visual visibility and input state, with controls resuming only after the return blend.
- [ ] Conversation entry, exit, cancel and failed start, plus the scoped safe-cancel/control-restoration and paper-viewer closing checks.
- [ ] Indoor NPC roles, outdoor NPCs, all four taxpayer targets, tutorial/guide NPC, ordinary completed NPCs and later available client presentation.
- [ ] Start/advance/typewriter/reveal/skip/end/cancel; opening interaction cannot skip the first line; a tap while typing reveals only that line and a distinct next gesture advances or ends the final line; repeated/busy/empty conversations, held input, tap-plus-button/submit duplication and callback counts.
- [ ] Speaker changes select the correct shot for all shared T02 NPC routes; grounded Elijah/NPC faces remain legible across height differences and tight spaces; compact bottom speaker/text/Continue panel fits safe areas without the oversized portrait layout.
- [ ] Marker availability/completion/unavailable/active states, shape distinction, range/occlusion and re-enable synchronization.
- [ ] Desk tools and objective notes, including swipe-close and correct return ownership.
- [ ] Profiles dropped/collected; tray entrance; corkboard categories and exact free pinning within bounds.
- [ ] Profile icon open, read, drag, select/topmost, edge limits, close/return, rapid toggles and controls restored after all transitions.
- [ ] Case/interview/objective/tutorial progression, successful/retry outcomes, counts and no duplicated rewards/drops.
- [ ] Pause, focus changes, scene unload, different supported screen sizes and actual mobile touch behavior.
- [ ] No new Console errors or missing references; measured performance and known limits recorded.

## Acceptance criteria

- [ ] All feasible automated/Editor checks actually run and their scope/results are recorded.
- [ ] Dialogue camera/UI regression criteria are recorded separately from first-person exploration checks; no player collider/root or shared NPC importer changes and no new NPC animation are present.
- [ ] Camera, hands/visual visibility and input return to the captured first-person state after the blend on both natural end and cancellation; input does not resume early.
- [ ] T02 session completion, completion/cancellation callbacks and success outcomes remain pending until the return blend completes; control release follows that same handoff.
- [ ] Manual/device checks performed by the user have explicit evidence; unavailable tests remain NOT RUN with concise instructions.
- [ ] No claimed overall regression pass with critical unresolved or untested flows.
- [ ] Each ticket has truthful status, modified file list, evidence, issues and continuation instructions.
- [ ] CURRENT_STATE, IMPLEMENTATION_PLAN decisions, PROGRESS and HANDOFF describe the actual delivered architecture.
- [ ] Existing uncommitted work remains preserved and no Git publishing/history action occurs without authorization.

## Deliverable and stop gate

Provide final file/change summary, test matrix, known issues with severity/reproduction and exact next action. Resolve only scoped test failures with an explicit bounded work unit; never suppress assertions or edit reports to pass. If user/manual/device acceptance is pending, mark it pending instead of declaring the overhaul complete.

No next-ticket token estimate is necessary when all tickets are accepted; estimate any specific unresolved fix separately. User performs Unity manual testing and Git write actions. Stop for final user review.


## Results (2026-10-15)

Branch `feat/immersive-interaction-overhaul` at `a05978e2`, plus the uncommitted T05 changes (two scripts and the docs). The agent ran these on Windows 11 with Unity 6000.4.5f1 installed. The user runs every Unity and device check.

### Automated checks

| Check | Result | Scope |
|---|---|---|
| `Assembly-CSharp` compile (installed Unity `Managed` and `Library/ScriptAssemblies` package DLLs, generated source list and defines) | PASS: 0 errors, 8 warnings (2 distinct, both pre-existing) | Runtime scripts, including T05 |
| `Tests/DialogueRegression` | PASS: 796 assertions | Session, town and guide (311), T03 presentation (446), T04 markers (39). Pure logic only, so no camera, UI or input. |
| `Tests/ControlsRegression` | PASS: 5,734 assertions | `JoystickInputMath` only |
| `Tests/ProgressionRegression` | PASS | `PlayerProgressData` only |
| `Tests/VerifyProfileCards.ps1` | PASS (3 checks) | Profile card prefab material and text fit, plus `Office_Level1` prefab use |
| Scene setup present in `Office_Level1` | PASS (static GUID check) | `DialogueCameraDirector` ×1, `ConversationActorVisual` ×1, `NpcStatusMarker` ×11, `NpcStatusMarker.mat` exists |
| Git diff against HEAD | PASS | Only the two T05 scripts and the docs changed. No `.unity`, `.prefab`, `.meta` or importer changes. The untracked `Assets/_Recovery/0 (2).unity` belongs to the user and was left untouched. |
| `Assets/Editor/CorkboardRegressionValidation`, `PaperDropValidation`, `PaperCollectionValidation`, `PaperTrayValidation` | NOT RUN | They need the Unity Editor. The user runs them from their Editor menus. |

### Manual matrix

Every row is **NOT RUN (user)**. No Unity Play-mode or device result has been reported for T01–T05.

| Area | Steps (summary) | Status |
|---|---|---|
| T01 controls | Joystick analog/dead zone, right-side swipe look, multi-touch, focus loss, desktop fallback, swipe never becomes a tap | NOT RUN |
| T02 conversations | Every office and town NPC route; cancel and failed start; callback counts, interview flags, four taxpayer drops, guide and board | NOT RUN |
| T03 presentation | Shot and reverse shot per NPC, tight-space fallback, Elijah grounded and facing, typewriter reveal then advance, opening tap cannot skip, return to the exact first-person view | NOT RUN |
| T04 markers | Available, completed, hidden states; suppressed in conversation; range and occlusion | NOT RUN |
| T05 integration | The five checks in the T05 ticket (popup during conversation, viewer refusal, closing-tap fall-through) | NOT RUN |
| Papers and tools | Drop, collect, tray, exact pinning, viewer open, drag, topmost and close; notes swipe-close; desk tools | NOT RUN |
| Platform | Pause and focus, scene unload, aspect ratios and safe areas, Android performance, Console clean | NOT RUN |

### Known issues and risks (not yet reproduced in Unity)

- **Medium:** Elijah may face the wrong way. Fix with `forwardYawOffset` ±180.
- **Medium:** sampling the Generic idle clip without an avatar may fail. Fall back to the static model, or untick `animateIdle`.
- **Low:** NPC bounds that include props can mis-frame shots. Add a `DialogueSpeakerAnchor`.
- **Low:** a tutorial step can appear on top of a conversation. This is cosmetic only; T05 fixed the input side.
- **Unverified:** `Office_Level2` is not configured for T03 or T04.
- **Out of scope:** historical progression warnings in `docs/LOGIC_AUDIT.md`.

### Next action

1. The user runs the manual matrix, starting with the T05 checks and one T03 conversation, plus the four Editor validations.
2. The user reports any FAIL with its step, NPC, Console output and a screenshot.
3. The user commits the T05 changes and docs.

Any fix becomes a separately estimated work unit. The overhaul is complete only after the manual rows pass.
