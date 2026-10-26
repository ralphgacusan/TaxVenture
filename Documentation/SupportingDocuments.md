# TaxVenture — Supporting Document Standard

Status: agreed planning baseline · 26 October 2026 · implementation pending.

**Read this file when planning, modeling, designing, or changing supporting documents in any TaxVenture level.** Keep this as the single reference; update it when the user changes a decision.

## Core direction

Simple, stylized 3D office papers or cards that fit TaxVenture's low-poly world, cream paper, manila folders, and physical desk interactions. Recognizable documents, generous empty space, short labels, useful clues. No realistic form reproduction or dense fine print.

- **Eight reusable document types** for the campaign. Each client receives only the relevant subset.
- A type has a fixed layout across all levels. Client names, dates, amounts, and facts change; layout and meaning stay consistent.
- Types are not paper counts: three invoices are three papers of one type.
- These are game investigation records, not a universal BIR filing checklist.
- Case Folder, Taxpayer Profile, Findings Sheet, Tax Code Book, and completed tax return are separate existing game items; they do not expand this library.

## The eight types

| #   | Fixed type                                    | Shape / visual identity                                                                | Important visible details                                                                  | Player uses it to understand                              |
| --- | --------------------------------------------- | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ | --------------------------------------------------------- |
| 1   | **Identity Document**                         | Landscape rounded card; muted blue header; portrait box with a generic silhouette      | Name, citizenship when explicitly recorded, birth date, short address, masked ID reference | Who the client is; stated identity facts                  |
| 2   | **Client Background Sheet**                   | Portrait cream paper; ochre header; three clearly separated blocks                     | Name; living history; work arrangement; income activities; period covered                  | Where the person lives and how they work/earn             |
| 3   | **Compensation Certificate — 2316**           | Portrait cream paper; blue header; employer block above two amount rows                | Employee, employer, tax year, compensation amount, tax withheld, reference                 | Employment income and employer withholding                |
| 4   | **Income Record / Collection Sheet**          | Portrait cream paper; sage header; short ledger with a total band                      | Client/business, period, activity, amount, payment channel, stated total                   | What earnings the client recorded                         |
| 5   | **Invoice / Payment Record**                  | Narrow portrait paper; ochre header; large amount and clear status label               | Issuer, customer, date/reference, item/service, amount, payment status                     | What a transaction concerns; whether it is billed or paid |
| 6   | **Bank Statement**                            | Portrait pale-blue paper; bank header; aligned transaction columns                     | Account holder, masked account, period, date/reference, description, money in/out          | Movement of money; links to other records                 |
| 7   | **Contract / Agreement**                      | Portrait cream paper; muted plum header; two party blocks and simple signature strokes | Parties, agreement kind, period/date, purpose, amount if relevant, key obligation          | Why money changes hands; work, rental, or repayment terms |
| 8   | **Creditable Withholding Certificate — 2307** | Portrait cream paper; blue header; payer/payee blocks and two amount rows              | Payer, payee, period, income payment, creditable tax withheld, reference                   | Withholding supported by the payer's certificate          |

**Type 2 is an office interview record**, not an official certificate or independent proof. Mark it “Client interview notes.”

**Variants stay within their type:** passport or government ID under Type 1; employment, rental, or loan agreement under Type 7. Retain the same information zones and interactions; use an accurate title. A passport uses a compact open-book shape with the same identity layout on its readable page. Do not invent another category per client.

Type 5 covers both client-issued and supplier-issued records. Show issuer/customer clearly: a supplier invoice concerns a possible expense; a client-issued invoice concerns a sale/service. An invoice is not automatically proof of payment. Use a visible “Unpaid” or “Paid” label only when the record supports it.

## Fixed visual layout

Every paper follows this reading order:

1. **Header:** simple icon + short document title.
2. **Owner/context:** client or business, period, short reference.
3. **Evidence area:** large labeled facts or a short table.
4. **Footer:** one small source/signature/status area only when needed.

Identity card example:

```text
┌──────────────────────────────────────┐
│ [icon]  IDENTITY CARD                │
│                                      │
│ ┌──────────┐  MARIE BAUTISTA          │
│ │ generic  │  Citizenship: Filipino  │
│ │silhouette│  Born: 14 Mar 1997       │
│ └──────────┘  Address: Poblacion      │
│               ID: •••• 0101          │
└──────────────────────────────────────┘
```

Paper example:

```text
┌──────────────────────────────────────┐
│ [icon]  INCOME RECORD                │
│ Rosa's Laundry      MAY 2026         │
│ Reference: COL-0526                  │
│                                      │
│ ACTIVITY          CHANNEL    AMOUNT  │
│ Clothes washing   Bank       ₱9,000  │
│ Clothes washing   Cash       ₱3,000  │
│ ──────────────────────────────────── │
│ RECORDED TOTAL              ₱12,000  │
│                                      │
│ Prepared by: Rosa                    │
└──────────────────────────────────────┘
```

The recorded total is the client's claim, not an automatic confirmation that it is correct.

## Modeling and readability rules

- Reuse three basic meshes: **ID card, full sheet, narrow transaction slip**. Passport adds a simple cover behind its identity page. Use shallow thickness, lightly softened corners, matte surfaces, and minimal bends.
- Suggested proportions: ID **1.6:1** landscape; sheet **1:1.4** portrait; slip **1:1.8** portrait. Adjust world size for readability at the existing desk camera, rather than matching real paper dimensions.
- Paper is warm ivory; text is dark charcoal. Use restrained blue, ochre, sage, and plum accents. Match existing folder/book materials and the game's established font.
- Use the **same neutral bust silhouette for every portrait box**. No client photographs, detailed faces, or client-specific portrait textures.
- Put editable text on the readable face as text elements; do not bake client details into a unique texture for every paper. Reuse the background, silhouette, borders, and icons.
- Aim for **4–6 information groups** per face. Short tables show about **3–5 rows** at once. Longer records use another readable page of the same type, not smaller text.
- Names, evidence values, dates, and amounts must remain readable in inspection view on the target phone. Labels are smaller than values; no tiny legal text, crowded stamps, watermarks, barcodes, or decorative paragraphs.
- Fixed labels and field positions stay the same. Unused fields show a dash or “Not applicable.” Long values wrap without covering neighboring fields; never shrink them into microprint.
- Color identifies the paper type only. It never reveals whether evidence or an answer is correct. Pair color with a title/icon so recognition does not depend on color alone.
- Use fictional issuer designs and masked identifiers; simplified “2316” and “2307” titles are sufficient. No official seals or claim that these props are official forms.

## Animation and polish standard

Documents should feel like small physical objects: smooth movement, readable resting poses, soft shadows, and restrained paper/card sounds. Reuse the same motion style across the campaign. The timings below are starting targets for playtesting.

| Moment | Motion / feedback |
|---|---|
| Available to interact | Subtle edge highlight when focused or within reach; no constant spinning, flashing, or large floating bounce |
| Client handover | A copy slides from the client's handover point onto a nearby surface or into inspection; short tilt and settle, about 0.4–0.6 s |
| Pick up / inspect | Lift slightly, rotate to face the camera, and ease into a readable position, about 0.25–0.4 s; soft paper or card sound |
| Read | Hold still with a clear shadow and unobstructed text; any background motion stops |
| Drag / compare | Follow the pointer directly; lift the selected paper above the others with a slightly stronger shadow; no delayed trailing |
| Put down / return | Ease back to its assigned position with a small settling motion, about 0.2–0.3 s |
| Store | Gather into the client's sleeve/folder and glide toward its existing inventory destination, about 0.35–0.5 s |
| Change page | Short page slide or restrained flip, about 0.2–0.3 s; readable face rests flat afterward |

- Use gentle easing and minimal overshoot. No rubbery paper, exaggerated bending, confetti, or camera shake for ordinary inspection.
- Preserve a visible connection between where a paper came from and where it goes. Avoid unexplained popping or teleporting.
- Keep tap and drag responsive; do not replay an entrance animation on every focus change. Interrupted movement must leave the document usable and stored only once.
- Closing inspection returns the paper to its last valid place. Keep selected papers in front without hiding essential comparison fields or controls.
- Play one appropriate sound per action through the existing effects setting; no looping rustle, repeated sound while dragging, or new spoken narration requirement.
- Reuse existing motion/audio features where suitable. Prioritize readable poses and stable touch behavior over elaborate effects.

## Proposed placement — planning, not yet implemented

**Documents belong to the client and their packet, rather than being scattered collectibles.** Exact scene anchors remain to be chosen during implementation.

| Location | Placement and access |
|---|---|
| Plaza encounter | The client presents the relevant copy after the conversation. Use a small sleeve on their nearby table/counter; where no surface exists, use a brief handover into inspection. No identity cards lying on the ground. |
| Carrying records | Keep papers grouped by client inside the existing profile/case packet and inventory flow. No separate hotbar icon for every supporting sheet. Supporting papers remain distinct from the profile summary. |
| Corkboard | The four profile cards remain the board pieces. Open a profile's supporting records for reference; do not pin every supporting paper onto the board. |
| Office desk | Keep the client's packet beside the Case Folder. Bring one paper forward to inspect and allow the needed pair to sit side by side; keep the book and folder accessible. |
| Level 2 client location | Rosa's supporting copies come from her existing records handover at the stall and stay associated with her case packet. Reuse the existing folder-page/inspection flow. |
| After use | Return papers to their assigned packet/stack. Preserve availability for rereading and case correction; closing a paper does not discard it. |

Level 1 example: inspect Jhun-jhun's identity and background during his encounter, collect his profile with access to those records, consult them while sorting, then inspect Marie's compensation certificate at her office desk. The recommended placement adds a small client-record sleeve, not another workstation or room.

## Evidence and interaction rules

- Tap to inspect, bring the face toward the player, then close/return. Reuse existing document movement and comparison controls. No new evidence-attachment puzzle is required by this standard.
- Give each paper a small number of meaningful clues. Introduce the clue's purpose through a short character response; opening a paper alone is not the learning goal.
- Keep shared names, periods, amounts, and references consistent across linked records unless a deliberate discrepancy is part of the authored case.
- Identity/address alone does not establish tax residency. Background history and relevant evidence must support that decision.
- Bank deposits are not automatically income; contracts can explain loans or other transfers. An expense invoice is not automatically an allowable deduction.
- Put factual evidence on the papers. Keep final taxpayer classifications and case verdicts for the player to determine using the book and case rules.

## Campaign introduction

| Stage                 | Introduce / reuse                                 | Learning purpose                                                                    |
| --------------------- | ------------------------------------------------- | ----------------------------------------------------------------------------------- |
| **Level 1 plaza**     | Types 1–2: identity and background                | Separate citizenship from living situation; one supported encounter, then less help |
| **Level 1 office**    | Add Type 3: compensation certificate              | Connect Marie's work arrangement with employment income; no new calculation task    |
| **Level 2**           | Add Types 4–7 as the current Rosa case needs them | Compare collections, transaction records, bank entries, and a loan agreement        |
| **Later credit case** | Add Type 8 when relevant                          | Understand supported creditable withholding                                         |
| **Later levels**      | Reuse familiar types in different combinations    | More interpretation and reconciliation; no requirement for a new type each level    |

Level 1 introduces **two types first, three by the end**. Later-level placement is provisional until those cases are designed.

Example packets: Marie **1/2/3**; Jhun-jhun **1/2**, with **7** only if his contract is needed; Rosa's investigation **4/5/6/7**. Her existing six records are six papers across four types, not six new categories.

## Acceptance check

- Recognizable silhouette, clear title, basic details, useful evidence, generous whitespace.
- Same type keeps its layout across clients and levels; only record details change.
- Readable at the game's inspection camera on a phone; meaningful values never rely on tiny print.
- Handover, inspection, comparison, and return have smooth consistent motion; documents rest still while being read.
- Sounds respect effects settings; repeated taps and interrupted animations never duplicate or lose papers.
- Every paper has a case purpose; the player is not collecting all eight for every client.
- This document defines presentation and the library; it does not change existing case answers, progression, rewards, or scenes.

Reference anchors for real form names only: [BIR 2316](https://bir-cdn.bir.gov.ph/local/pdf/2316%20Sep%202021%20ENCS_Final_corrected.pdf) · [BIR income-tax instructions including 2307](https://efps.bir.gov.ph/efps-war/EFPSWeb_war/help/help1701_v2.html). Gameplay uses simplified fictional props.
