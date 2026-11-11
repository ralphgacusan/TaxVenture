# T08 — Enter Rosa's laundry and retrieve meaningful records

## Status and readiness

**Planned. No implementation performed.** Blocked on listed ticket prerequisites. Acceptance gates remain distinct from source readiness.

## Priority and relative size/risk

P1. Size: **L**, bounded to one fresh implementation context. High: new interior/camera/collision and source ownership. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

[T01](T01-base-paperwork-with-full-bag.md), [T05](T05-living-community-and-profile-round.md), [T07](T07-bayview-level2-classification-stage.md) External decisions/assets/evidence: Accepted pilot and actual room/navigation/contact acceptance; retained optional letter is not a release gate for progress.

## Direct dependents

[T09](T09-arthur-toolkit-and-income-activity.md), [T18](T18-level2-complete-dialogue-voice-coverage.md)

## Goal and visible accomplishment

Rosa's familiar case leads into a believable working laundry where the actual record packet is physically retrieved; Kanor's optional letter is discoverable alongside the visit.

## Current behavior and evidence

Level2RosaStall chooses frontage/anchor/fallback and an exterior records desk; live four-paper packet and Rosa's single office-travel owner exist. NotesOnboardingChest already provides opening/paper pickup vocabulary. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [Level2RosaStall.cs](../../../Assets/Scripts/Level2/Level2RosaStall.cs)
- [Level2RosaConsultation.cs](../../../Assets/Scripts/Level2/Level2RosaConsultation.cs)
- [scene_assets.asset](../../../Assets/Resources/Levels/Level_02/scene_assets.asset)
- [NotesOnboardingChest.cs](../../../Assets/Scripts/SideQuest1/NotesOnboardingChest.cs)
- [NotesChestPaperPickup.cs](../../../Assets/Scripts/SideQuest1/NotesChestPaperPickup.cs)

## Scope and explicit exclusions

12 November baseline: first-person exploration plus sprint. Keep smoothing, physical NPC contact, task/dialogue views and all case scope. Third-person/jump adoption belongs only to tentative T22 and cannot block this ticket.

Retain L2 first-person exploration and sprint, working laundry interior/threshold/visibility and meaningful record retrieval, plus unlocked public-approach Kanor chest/retained reader. Exclude new case facts/records, precision access gate, new container framework, reward/moral bonus or mandatory letter. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

L2-B and optional L2-O. After preliminary hold, travel FP to laundry, remain FP inside, hear concise Rosa context, open records storage/retrieve current D1/D4/D3/D5 and place them at existing task desk. Optionally open public-side chest and retain Kanor's copied CT-07/rent letter before/after visit.

## Mechanics and domain logic

Deterministically bind room/doorway/storage/task surfaces to actual Rosa/record owners. Four real records only; D2/D6 remain legacy. Reuse chest body/lid/paper mechanics with separate letter source/claim, never Notes unlock. Letter can remain available when Bag full; owning it never blocks required certificate path.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Valid deliberate action | Neutral handling, then actual result | Existing owner validates candidate and persists before publishing | Settle to committed state |
| Save fails or command refused | Concise actionable retry; no success effect | Previous durable work/ownership unchanged | Source/task stays reachable |
| Duplicate/stale callback | Existing settled result, no second grant | Current action/case/source/revision guards | Ignore stale cosmetic completion |
| Cancel/interrupt/reload | Release only owned input; retain saved work | Never complete from voice/gesture/arrival | Restore canonical state, not animation frame |

## Visual/environment/interaction specification

Laundry work areas, washer/basket/service counter and believable records drawer/sleeve; limited readable interactables, clear set dressing. Use current Rosa identity and a modest working room; public chest outside doorway/travel lanes, no hidden key.

## Animation and physical contact

Rosa/player reach/retrieve/carry/place and credible record presentation, doors/footsteps; same Rosa can travel to office after actual handover. Chest/paper settles only after real persist.

## Audio/dialogue/voice

Shortened ordinary Rosa/workplace/optional Kanor-context dialogue voiced by T18; letter/document silent. Reuse indoor laundry ambience/SFX families and quiet paper/chest cues, no moral celebration.

## Camera/mobile/accessibility

FP street→FP doorway/interior→task and correct FP return, existing target reach and clear collision/camera space. Required threshold has flat/assist alternative; chest needs no jump.

## Data/interface/persistence

Current case acquired-source/handover milestones unchanged; minimal room-safe restore fact if not derivable. Letter uses existing campaign inventory/readable-record/claim owner, retained across chapters, no second journal/save. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

Failed paper/letter commit leaves retrievable source; interrupted lid/retrieval settles without granting. Restore same Rosa/records/room pose with no duplicate desk, ownership or compulsory handover replay. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

Simple scoped room visibility/collision boundary within existing scene; no Addressables/streaming package. Migrate only qualified used room/prop/Rosa treatments and measure against T02 ceilings.

## Internal checks — AI only

- [ ] Idempotent room/actor/desk bindings, doorway/visibility/target references, four current source IDs and no duplicate legacy records.
- [ ] Retrieve/fail/retry/cancel/reload and optional chest open/owned/missed/full-Bag/duplicate tests; missed letter changes no case/reward/ending prerequisite.
- [ ] Compile and affected L2/campaign/inventory/controls/guide/chest/reference checks; preserve L1 Notes chest and current office travel owner.

## USER Unity/device acceptance

- [ ] Walk into working laundry, retrieve actual packet, inspect meaningful objects and return; targets/collision/occlusion/Arthur doorway positions remain usable.
- [ ] Find or miss chest before/after case, read retained copy later, test focus/Back/doorway carrying; no purchase/jump gate or duplicate Rosa.
- [ ] Record actual build/device/steps/evidence for applicable gates **G1, G2, G3, G4, G8** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.

