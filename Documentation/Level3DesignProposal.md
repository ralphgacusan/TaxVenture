# Level 3 — The Numbers Tell a Story

Revised design proposal • 29 October 2026 • Planning only; no runtime changes

Implementation planning: [Level 3 ticket pack](Level3TheNumbersTellAStory/README.md) and [ordered 18-ticket index](Level3TheNumbersTellAStory/TICKETS.md). This proposal remains the design basis; the pack supplies inspected source mappings, execution scope and acceptance without enabling gameplay.

This revision preserves the two cases and chapter arc while specifying campaign continuity, rewards, upgrades, dialogue, trust, and the complete new interaction loops. Existing behavior is distinguished from proposed Level 3 extensions. Sections 13–20 provide the detailed system and interaction contracts; they are part of this plan, not a separate future design task.

Reading guide: Sections 1–12 explain the chapter, cases, story and overall progression. For the detailed revision, start with [campaign continuity](#13-campaign-continuity-exact-carry-forward-contract), [trust and dialogue](#14-public-trust-hearts-reputation-history-and-dialogue-consequences), [rewards and upgrades](#15-coins-xp-badges-upgrades-and-purchase-pacing), [side-quest handling](#16-worth-the-tax--tactile-interaction-and-correction-detail), [calculator mechanics](#17-calculator-and-case-solving--exact-player-contract), and [delivery/continuation](#18-client-delivery-chapter-wrap-up-and-next-chapter-continuation).

## 1. The chapter promise

**“I can show where the amount came from, calculate what is owed, and explain it to the client.”**

Level 1 asks who the taxpayer is. Level 2 asks whether the income records support the summary. Level 3 asks what those supported records mean in money: which costs can reduce taxable income, which calculation applies, and what remains payable after supported tax credits.

The signature addition is a **physical calculator with a retained calculation tape, attached to a player-built computation worksheet**. The stamp returns as the final professional recommendation. It does not become the calculation tool.

Two cases, one required side quest, one short field visit, and two client resolutions form a proposed 24–30-minute chapter. Allow 30–35 minutes for first-time readers or corrections. Timing is a playtest target, not a measured result. Save between work sessions; players need not finish in one sitting.

## 2. Fit with the existing game

The proposal draws on the pasted consolidated design, current Level 1/2 case content, the Campaign Foundation continuation amendments, and the supporting-document standard.

Important continuity decisions:

- Marie remains Level 1's introductory case; Rosa remains Level 2's income-record investigation. Do not reopen Rosa's finished case to manufacture a larger tax problem.
- Bea is already an optional Level 1 resident: Beatriz Mendoza, a freelance graphic artist who keeps payment notes and receipts. Her promotion to client makes earlier exploration meaningful. Her introduction also works for players who never spoke to her.
- The implemented guide is Arthur. Keep his reference available and use a short contextual calculator lesson through the existing guide presentation. The pasted story's Quax remains the mayor, not Arthur's replacement. His closing response can be a receptionist-delivered message: this plan does not depend on an existing Quax actor or a revived in-person demonstration system.
- Reuse the office, Bag, book, detachable documents, comparison interaction, stamp handling, talk-first Auditor hand-in, client-copy delivery, save behavior, and campaign rewards.
- The comparison stand is deferred. Ordinary free comparison must support every task.
- The existing calculator code contains a flat-rate prototype. The new chapter requires an authored, consistent rule set rather than treating that prototype as a correct graduated computation.

| Chapter | Main responsibility | Distinct interaction | Responsibility reserved for later |
|---|---|---|---|
| 1 | Understand taxpayer circumstances | Collect and classify profiles | Independent computation |
| 2 | Check income records and readiness | Annotate, reconcile, justify a stamp | Deductions and tax calculation |
| 3 | Build a supported taxable base and compute tax | Construct worksheet, use calculator tape | Full return preparation |
| 4 | Prepare individual and corporate returns | Select and transfer figures; resolve document versions | Integrated investigation and attribution |
| 5 | Resolve the corporate review | Explore, question, verify, substantiate findings | Campaign conclusion |

Level 3 increases the number of decisions that depend on one another. Bigger figures alone do not supply difficulty.

## 3. The complete playable flow

| Beat | Target | Player action | Result and emotional purpose |
|---|---:|---|---|
| Reception | 1 min | Receive Bea's assignment and a short note that a company appointment follows | Clear workload; no repeated movement tutorial |
| Calculator introduction | 2 min | At the desk, complete a tiny unrelated calculation and attach its tape | Learn the new tool through existing contextual teaching |
| Meet Bea | 2 min | Visit her plaza worktable; ask why receipts and her proposed worksheet differ | Personal problem and reason to work outside |
| Worth the Tax? | 6–7 min | Reconstruct Bea's supported income/cost worksheet | Main tactile puzzle and scored side quest |
| Bea's computation | 4–5 min | Use the worksheet, select a bracket, calculate tax, apply supported withholding | Turn evidence into a complete answer |
| Review and client resolution | 2 min | Stamp, submit, correct if needed, give Bea her computation copy | Payoff and one completed client story |
| Company appointment | 5–7 min | Reuse the workflow with corporate rules and less guidance | Transfer challenge rather than a second tutorial |
| Review and closing discovery | 2–3 min | Finish the company case; record the Assistance Desk connection; receive the mayor's response | Qualified story escalation and next-chapter hook |

Only the current case is editable. The company appointment opens after Bea's resolution. A second waiting folder can be visible, but players do not juggle two active folders.

Bea's side-quest workbench is close to her familiar plaza location. Following the worksheet challenge she can travel to the office conference room using the established NPC movement pattern. Company records arrive through the scheduled office appointment. Avoid a second compulsory field loop.

The calculator introduction uses a separate practice slip: available office budget 100, paper order 20, courier cost 8. Player calculates 100 − 20 = 80, then 80 − 8 = 72, and attaches a tape. This is explicitly a tool exercise, separate from the campaign wallet: no real coins are spent and no invented tax percentage is taught. Short guide prompts introduce selection, subtraction, equals, and attachment using the existing tutorial presentation. No new practice board, walking mentor, or in-person demonstration framework. The actual tax-rule lesson appears when Bea's case needs it.

## 4. Case 1 — Bea: “Every Receipt Has a Place”

### Client, stakes, and lesson

Bea designs logos and menus for island businesses. She has saved receipts but mixed business expenses with personal purchases. She wants a dependable figure before deciding how much she can set aside for a shared studio.

Opening line: **“I kept the receipts this time. That means I can subtract all of them, right?”**

Elijah's task is to reconstruct her annual computation. The lesson is that income, deductible expenses, and tax credits play different roles. A receipt supports a transaction; its existence does not automatically make it deductible.

### Bounded scenario

Use a completed fictional tax-year 2026 annual record set. All documents cover the same period; these are annual computations, not a claim that a partial month determines the year's tax. Present the chapter as reviewing supplied annual case packets rather than advancing the island calendar literally one day from every earlier document.

Bea is a resident individual professional using the graduated rates and itemized deductions for this exercise. That regime is already established in her case notes. Choosing between the 8% option, itemized deductions, and OSD is outside this chapter's assessed decisions.

All supplied business costs are expressly documented as current-period ordinary business expenses meeting the scenario's stated requirements. No equipment depreciation, capital expenditure, mixed-use allocation, VAT calculation, or uncertain deductibility is hidden inside the figures.

### Authored financial fixture

| Entry | Amount | Supported use |
|---|---:|---|
| Logo-design fees | ₱500,000 | Income |
| Menu and illustration fees | ₱400,000 | Income |
| Studio rent | ₱180,000 | Allowable business expense |
| Business-only design subscriptions | ₱90,000 | Allowable business expense |
| Business printing/consumables annual total | ₱30,000 | Allowable business expense |
| Personal birthday purchases | ₱24,000 | Not deductible in this case |
| Supplier invoice behind the same printing total | ₱30,000 | Corroborating evidence; do not deduct twice |
| Supported creditable tax withheld | ₱12,500 | Credit after tax due is calculated |

Correct computation:

1. Revenue: ₱500,000 + ₱400,000 = **₱900,000**.
2. Allowed expenses: ₱180,000 + ₱90,000 + ₱30,000 = **₱300,000**.
3. Taxable income: ₱900,000 − ₱300,000 = **₱600,000**.
4. Applicable bracket: over ₱400,000 through ₱800,000.
5. Tax due: ₱22,500 + 20% × (₱600,000 − ₱400,000) = **₱62,500**.
6. Remaining tax payable: ₱62,500 − ₱12,500 = **₱50,000**.

The bracket is supported by the [BIR's 2025 published individual return and rate table](https://bir-cdn.bir.gov.ph/BIR/pdf/RMC%20No.%2034-2025%20Annex%20A.pdf). Its structure also separates taxable-income computation from tax credits. The figures above are fictional teaching fixtures; the scenario's expense eligibility still needs curriculum review before implementation.

The 2307 certificate identifies a ₱250,000 income payment already included in the ₱400,000 fee category and ₱12,500 withheld. It supplies the credit, not additional revenue. State that relationship clearly in the records. The exercise does not ask the player to calculate a withholding rate or reconcile net bank collections.

## 5. Required side quest — “Worth the Tax?”

### Purpose and connection

**Rebuild Bea's income-and-expense worksheet from her own records.** This is the preparation stage of her main case, carried out at her worktable. It is not an unrelated errand or a quiz that happens to unlock the office.

Bea hands over an annual collection sheet, a short expense summary, the relevant supporting invoices/agreements, and a personal purchase record. She also has a rough proposed worksheet: it subtracts every receipt and counts one printing expense twice.

The player turns those records into a working paper that later supplies the office computation. The side quest stops before selecting a tax bracket or computing tax due.

### Exact interaction sequence

**A. Inspect and lift a working strip.** Tap a source paper to bring it forward. Tap a meaningful printed row, then “Add to worksheet.” A short paper strip lifts from the row's position; the original stays intact. The strip retains the description, amount, period, and source reference. This uses the existing physical paper vocabulary without asking players to cut or damage originals.

**B. Place it in one of four sections.**

| Section | Purpose | Bea's correct entries |
|---|---|---|
| Income | Earnings entering this computation | Two fee categories |
| Allowable expenses | Supported costs reducing this taxable base | Rent, subscriptions, printing total |
| Not deductible | A real transaction that does not qualify here | Personal birthday purchases |
| Supporting only | Evidence already represented by another entry | Printing invoice linked to annual printing total |

Do not call all rejected amounts “tax-exempt.” Non-income, statutory exclusions, non-deductible expenses, and duplicate evidence are different ideas. This authored case does not need an extra statutory-exclusion zone.

**C. Compare claim with support.** The rough worksheet can be pinned beside the originals. When assessing the personal receipt, the player chooses a brief reason from the book: “Personal purpose.” When assessing the printing invoice, choose “Already represented” and link it to the annual printing row. Correctness depends on purpose and relationship, not paper color.

**D. Assemble the subtotals.** The board shows the chosen sections but leaves subtotal fields blank. Players tap the calculator, add their selected entries, and attach the resulting tape to Income Total and Allowable Expenses Total. The calculator sums what the player entered, including a wrong selection.

**E. Submit a complete independent check.** All seven source entries must have a destination, matching amount, and source link. Two claim notes on the rough worksheet ask for an explanation of the personal-purchase claim and printing-invoice claim, including which existing row the latter concerns. Those note fields remain accessible regardless of the player's chosen destinations; choosing a wrong tray cannot hide a required answer or reveal the correct tray. Check completeness before grading. An incomplete draft points to the empty slot and does not consume the first attempt.

**F. Correct, then collect the worksheet.** The first complete check records mastery. Feedback identifies the kind of problem and relevant source; it does not instantly replace the player's answer. Correct all outstanding treatment, linking, and subtotal errors before collecting the completed worksheet.

### Why this is a new mechanic

Level 1 places people into taxpayer classifications. Level 2 annotates the treatment of income records and checks a prepared summary. Level 3 builds a reusable numerical model: the player decides where supported amounts enter a calculation and preserves their arithmetic.

Avoid making row extraction, claim comparison, and placement into three repeated full puzzles. They are three steps in the same seven-entry worksheet activity. Limit extraction to meaningful rows; avoid unrestricted highlighting, fiddly cropping, and tiny tap targets.

### Error and feedback examples

- Personal purchase placed in expenses: “The receipt supports the purchase. What was it used for?”
- Printing invoice deducted again: “This reference also appears in the annual printing total. Does it represent another cost?”
- Correct placement, incorrect subtotal: “These selected expenses do not match the attached addition.”
- Correct value with the wrong source: “The figure needs the record covering this entry and period.”

Allow wrong content choices to remain on the board until Check. A zone must not reject an incorrect answer and reveal the solution through dragging.

### Deliverable and scoring

The acquired item is **Bea — Supported Income and Expense Worksheet**. It contains source links, selected treatments, subtotals, and attached tapes. It is available in the active folder and Bag without duplicating originals.

Proposed side-quest medal: twelve explicit decisions—seven destinations, two reasons (personal purpose and already represented), the duplicate-support link, and two subtotals. Gold: 12/12; Silver: 10–11/12; Bronze: 0–9/12. Source IDs and printed amounts are preserved automatically during extraction, so copying an automatically supplied value is not falsely scored as a separate skill. These are proposed Level 3 settings, not a change to Level 2's existing 16-decision check. See Section 15 for the dependency-aware scoring rule and XP.

Freeze the tier at the first complete independent Check. Corrections enable completion but do not upgrade that attempt. Replay can improve mastery without paying campaign rewards again. The office case uses the separate existing case-result system; do not invent a competing second side-quest grade.

## 6. The office mechanic — calculation you can trace

### New desk objects

- **Calculator:** physical desk tool, available free. Tap to focus; use large keys or select a printed number to insert it. Support backspace, clear-entry, and undo. No typing on a miniature phone keyboard is required.
- **Calculation tape:** each completed expression creates a readable line. Select a result and attach it to a named computation field. Players retain the expression, not just its final number.
- **Computation worksheet:** links the supported income/cost totals, taxable income, rule, tax due, credits, and payable balance.
- **2307 record:** adopt the already planned supporting-document Type 8 when Bea's credit is introduced. This is a source document, not a return to file.

The short office progression is:

**Use supported totals → calculate taxable income → select rule → calculate tax due → inspect credit → calculate balance → explain → stamp → review.**

### What the player actually chooses

1. Select the supported worksheet as the source for revenue and expenses; confirm each transfer into the case.
2. Calculate taxable income and attach its tape.
3. Open the year-labeled rule reference; select the bracket containing the taxable amount. The book displays the full individual table, with clear lower/upper boundary language.
4. Build the graduated expression using the bracket's base tax, threshold, and marginal rate. The tool handles arithmetic; the player determines the structure and inputs.
5. Inspect the supplied withholding certificate and attach its credit to the credit section.
6. Calculate the balance and attach its tape.
7. Choose a short explanation to Bea: “Business expenses reduce taxable income; supported withholding reduces the tax still to pay.”
8. Apply the familiar readiness stamp and hand the folder to the Auditor through the existing conversation flow.

No silent “solve case” button. Selecting a bracket may provide its labeled formula template, but it must not automatically choose the bracket or import the client's numbers.

### Stamp meaning

Use the existing Ready/Not Ready handling. The chapter-specific helper text says **“Ready for computation review.”** A Ready recommendation covers supported inputs, the selected rule, calculations, credit, and explanation. It is not confirmation that a return has been filed or money has been paid.

A justified hold remains a valid interim action. It does not complete an unresolved case. If an input changes after stamping, mark the computation for recalculation and the stamp for reassessment. Preserve the player's work; never silently replace it with the answer.

### Review and client payoff

The Auditor identifies the earliest unresolved cause: expense treatment before a dependent taxable-income result, rule selection before a dependent tax-due result, then credit/balance. This prevents five error messages caused by one wrong input.

Approved work produces a **Client Computation Copy**, using the familiar physical delivery interaction with chapter-appropriate labels. Bea receives an explanation and a practical recordkeeping suggestion: separate business expenses from personal purchases and keep support together. Elijah has not filed her return. Her future studio decision belongs to her; the game does not promise that a lower tax bill buys an upgrade.

## 7. Case 2 — Coast Tales Marine Services, Inc.: “The Same Method, a Different Rule”

### Client and role in the chapter

An independent local transport-services company supplies island shops. Its bookkeeper seeks help before annual filing preparation. It is a legitimate client, separate from the redevelopment corporation. Its staff cooperate and provide the requested records.

The player applies the same method with a new taxpayer category. Guidance is reduced: label the worksheet sections and supply the corporate reference, but do not repeat every calculator gesture or preselect the applicable rule.

### Bounded corporate fixture

Use an ordinary domestic corporation with no special incentive regime. The supplied case states that MCIT does not yet apply to its operating year. Asset information is already verified, and the rule card supplies all relevant conditions. This is a first corporate computation, not a full corporate tax survey.

| Entry | Amount | Treatment |
|---|---:|---|
| Annual service revenue | ₱8,000,000 | Income |
| Documented service operating costs | ₱1,200,000 | Allowed cost |
| Business premises rent | ₱500,000 | Allowed expense |
| Business administration costs | ₱300,000 | Allowed expense |
| Owner's personal holiday paid through the company | ₱120,000 | Not deductible in this supplied scenario |
| Supported creditable withholding | ₱300,000 | Credit against tax due |

Correct result: ₱8,000,000 − ₱2,000,000 = **₱6,000,000 taxable income**; × 25% = **₱1,500,000 tax due**; − ₱300,000 = **₱1,200,000 remaining payable**.

The regular corporate rule is 25%; qualifying smaller domestic corporations may use 20% where both the income and asset conditions are met. This client's taxable income exceeds the ₱5 million income condition, so the regular rate applies. Supply the verified asset figure rather than adding an asset-hunt puzzle. See the [Supreme Court E-Library text of RA 11534](https://elibrary.judiciary.gov.ph/thebookshelf/showdocs/2/93191) and [BIR's published CREATE MORE amendment text](https://bir-cdn.bir.gov.ph/BIR/pdf/RMC%20No.%20135-2024%20Annex%20A.pdf). Final implementation needs educator verification of this bounded 2026 scenario and exclusions.

### The one new discrepancy

The Assistance Desk-prepared schedule calls the personal holiday an “administrative expense.” Its draft total deductions are ₱2,120,000. The original travel record identifies the owner and family, with no business purpose. The ordinary business administration total is separately supported at ₱300,000.

Player actions:

1. Compare the questionable line with its supporting travel record.
2. Ask the bookkeeper the now-unlocked question: “Was this trip for company work?”
3. Record the response and retain the original as support; a conversation alone does not supersede the records.
4. Place ₱120,000 in Not Deductible, leaving allowable costs/expenses at ₱2,000,000.
5. Select the corporate rule, calculate, apply the credit, explain, stamp, and submit.

This is a compact application inside the company case. It does not become a second scored Worth the Tax? round or another town-fetch quest.

The faulty summary would produce a ₱1,170,000 balance under the same rate and credit assumptions. The supported result is ₱1,200,000. The player succeeds by producing the supported computation, even though it is higher. Do not frame “make tax smaller” as the objective.

## 8. Storyline and the chapter ending

### Opening: trust in the new consultant

Rosa's completed work has led to another appointment. If the player previously promised too much, the receptionist uses neutral wording that acknowledges the corrected outcome rather than declaring a flawless reputation. Bea's opening concern is ordinary bookkeeping, not a conspiracy.

### Middle: competent help changes everyday decisions

Bea moves from “every receipt reduces tax” to understanding the difference between a business cost, a personal purchase, and a credit. Her resolution is the chapter's warm midpoint. A returning character now trusts Elijah with a more demanding problem.

### Closing: a repeated source, a carefully limited finding

The company summary bears the same Assistance Desk preparer identity seen on Rosa's packet. Its retained original supports a correction. The bookkeeper confirms that the summary was received from that service. Keep the summary and source with the company's case records.

The Evidence Journal records:

- **Verified:** The Assistance Desk prepared another summary containing an unsupported treatment; original records support correcting it.
- **Unresolved:** Whether the error was intentional, who selected the treatment, and whether a review or replacement process failed.

The two cases do not prove a single scheme: Rosa's issue and the company's expense error have different effects. The common source justifies examining the process; it does not establish motive or authorship of manipulation.

Quax responds: **“I told people this service would make things easier. Bring me what the records support. I need to know where the process is failing.”**

This is concern, not the final accusation or authorization for unrestricted investigation. Level 4's conflicting package and version trail will justify the formal review leading into Level 5.

Final visible reward: both case outcomes appear in the chapter's saved result, the calculator remains available at the desk, and the integrated computer becomes eligible for purchase once its complete functional purchase slice exists. Show its future usefulness without opening an unfinished Level 4 or requiring immediate spending. The current inventory retires chapter-specific case tools; a browsable archive of all historical folders is not already implemented. Preserve results and choice history, but do not make a new historical-folder viewer a prerequisite for this chapter.

## 9. Difficulty, fun, and pacing

| Axis | Level 2 | Level 3 |
|---|---|---|
| Case load | One case | Two sequential cases |
| Core judgment | Income treatment and summary readiness | Expense eligibility, taxable base, rule, credit |
| Numerical work | Supported income totals | Dependent multistep computation |
| Trap | Omitted earnings; money is not automatically income | A receipt is not automatically a deduction; a credit is not an expense |
| Support | Guided hold and income toolkit | Guided first computation; reduced guidance on company case |
| Final responsibility | Justify readiness | Explain a supported amount and readiness |

Keep the fun in responsive objects, discovering relationships, repairing a plausible draft, seeing Bea's progress, and mastering a more capable desk. Use calculator key clicks, a short tape-feed motion, satisfying paper placement, and a clear client handover. Repeated actions should remain fast and skippable after their first demonstration.

Do not add a rapid arithmetic contest, random document spawns, hidden essential records, a long maze, or a harsher timer just to signal difficulty. The main challenge is choosing inputs and rules.

Keep established active-review clock and pause conventions. Introduce unfamiliar controls in protected tutorial time; independent reading and calculation follow existing active-time rules. Reaching cutoff moves unresolved work to the next review opportunity with papers and corrections retained. Extensions remain optional. Neither case's first-time rewards repeat after a day rollover.

No additional major ethical branch is required. Level 2 introduced the first, Level 4 has the next pressure point, and Level 5 carries the final confrontation. Level 3 gives professional competence room to develop. Ordinary tax mistakes affect mastery and correction needs, not moral standing.

## 10. Rewards, recovery, and mobile usability

- Retain the campaign's distinctions: XP and badges show mastery, coins buy office improvements, and Public Trust hearts reflect authored community service. Corrected work and client delivery unlock story progression; XP, badge tier, coins, purchases, and trust thresholds do not gate it.
- Extend the actual L1/L2 reward pattern: 60 coins for the corrected side quest plus 100 per approved case, or 260 total. Proposed Level 3 XP maximum is 260, separately calculated from performance; equal maxima do not make XP and coins interchangeable. Sections 13–15 define exact proposed claims and tiers.
- Carry forward real capacities, pins, supplies, ownership, and installed decor. The actual Bag starts at 6 and upgrades to 9/13; the hotbar starts at 3 and upgrades to 4/5. There is no playable storage drawer. The deferred comparison stand stays unavailable.
- Unlock the 250-coin computer only after chapter wrap-up and only if its model, placement, save behavior, and immediate useful function are implemented. Purchase remains optional. The current computer is a pending teaser, not a working upgrade merely needing an unlock flag.
- Required calculator, references, worksheet, and evidence stay free. Worksheet strips and tapes live inside the case container and cost no extra slots. A full Bag uses the existing Make room/return-to-source behavior; never invent unlimited playable drawer storage.
- Checkpoint stable milestones: record handover, worksheet drafts, first frozen grade, corrections, acquired worksheet, calculator entries/tapes, case revision, review result, client delivery, next assignment, closing discovery, rewards.
- Resume to the appropriate desk or safe standing position. Do not replay purchases, handovers, trust changes, or completion rewards. Restore a held strip as an unheld saved entry rather than restoring an animation frame.
- Use text labels with symbols as well as color. Make printed amounts and selected sources readable on mobile. Prefer tap-select/tap-place with optional dragging; paper precision must not determine tax mastery.
- Keep active case sources easy to inspect. Previously completed files remain references, not accidental input sources for the current client.

## 11. What remains for Levels 4 and 5

**Level 4 — Follow the Paper Trail:** use familiar taxable income, tax due, credits, and balance to prepare returns. Introduce form choice, field mapping, period confirmation, support assembly, version comparison, and simulated filing. The optional computer transfers only player-selected confirmed amounts and prints a draft. A changed record can require a familiar recomputation; it should not force a second full computation tutorial.

Use a fresh individual filing case with supplied verified computation rather than replaying Bea's entire expense puzzle. The second corporate package supplies the required version-tracing side quest. The company introduced here may recur if that strengthens continuity, but avoid changing its established case facts just to make it suspicious.

**Level 5 — Catch Me If You Comply:** combine taxpayer facts, supported treatment, computation, and filing inside the authorized corporate review. Add the larger explore → question → verify investigation and finding-to-evidence board. Deliberate replacement, authorization, and intent need their own support. The Level 3 journal entry is a lead, not conclusive proof.

## 12. Production scope and acceptance

Build the smallest complete chapter around these authored decisions:

1. Calculator and retained tape, with an unrelated safe demonstration.
2. Bea's field encounter and seven-entry worksheet side quest.
3. Bea's full independent computation, 2307 credit, review, and client-copy resolution.
4. One legitimate company case using the same tools with reduced guidance.
5. A bounded Assistance Desk finding, the mayor's response, saved chapter results, and functional computer purchase eligibility.

Reuse document Types 1–2 and 4–7 where applicable; introduce Type 8 only for actual withholding certificates. The worksheet, calculator tape, and computation copy are working outputs, not additions to the eight-type source-document library. Reuse the physical presentation standard; avoid an extra all-documents gallery or a new interaction framework.

Before building, finalize educator-reviewed expense assumptions, corporate scenario conditions, year rules, fictional-nation mapping, and existing reward configuration. These are content and integration tasks, not reasons to expand the chapter's mechanics.

The plan is ready for implementation specification when its client identities, financial fixtures, worksheet sections, and progression are adopted. It is not an approved implementation ticket pack or evidence of completed Unity work.

Acceptance targets for later playtests:

- A new player can distinguish allowable expenses, personal purchases, duplicate support, and credits without answer-revealing drag behavior.
- The side-quest worksheet actually feeds Bea's case; it is never discarded in favor of unrelated prefilled answers.
- Bea resolves to ₱600,000 taxable income, ₱62,500 tax due, and ₱50,000 payable; the company resolves to ₱6,000,000, ₱1,500,000, and ₱1,200,000 respectively.
- The player can explain why the whole taxable amount is not multiplied by the selected individual marginal rate.
- A correct numeric answer with unsupported inputs cannot pass as fully supported work; equivalent valid arithmetic is accepted without requiring one exact key sequence.
- Changing an input marks dependent work and the stamp for review without erasing unrelated evidence.
- Returning from a correction or interruption preserves the first mastery result and awards completion once.
- Both cases finish without an optional purchase; mobile paper handling remains comfortable.
- Players describe the story discovery as a repeated Assistance Desk problem rather than proof of intentional manipulation.
- The chapter feels like learning to compute; Level 4 still has a distinct filing responsibility to introduce.

## 13. Campaign continuity: exact carry-forward contract

### Source of truth and existing-system basis

Current code and latest amendments take precedence over older ticket examples and JSON `_readme` comments. In particular, Level 2 currently scores 16 decisions, the storage drawer has been removed from play, Arthur remains the guide, and Continue opens the Career/progress module. None of those should be accidentally reversed by this chapter.

| Inspected basis | What Level 3 preserves | Proposed extension |
|---|---|---|
| Campaign Foundation T02/T03; CampaignRun; CampaignMenu | One signed campaign, explicit Story/Replay/Practice modes, Continue into Career | Chapter 3 availability and resumable task summary |
| T07/T08; CaseResultRules; ClientCopyRules | Corrected-work progression; talk-first review; physical client-copy delivery | Two distinct case results and two deliveries before wrap-up |
| T09; CampaignRewards | Fixed once-only coin/service claims and named ledger reasons | One worksheet reward, separate Bea/company approval and delivery IDs |
| T10/T18; CampaignInventory; inventory.json | One slot-based Bag/hotbar, containers, Make room, item eligibility | Notes/folder/book scopes include Level 3; worksheet inside active folder |
| T12/T13; shop.json | One wallet/catalogue shared by counter and menu; saved capacities and decor | Functional computer purchase slice after corrected Chapter 3 |
| T14; CampaignVouchers | Two carried vouchers; +180 active seconds; one per case | Separate Bea/company voucher claims and clock state |
| T19; Level2CampaignRules | Rosa's committed choice and consequences remain durable | Short history-aware dialogue; no second Rosa penalty or retroactive repair bonus |
| T22/T23/T32/T33 | Existing dialogue, awaiting-action handover, consultation/emote/NPC-life ownership | Bea and bookkeeper content driven by chapter phase |
| SupportingDocuments; T27/T30/T31 | Eight fixed source-document types; physical handling; permanent Arthur reference | Adopt Type 8 for actual 2307 records; reuse other types |

### Entry and progression

Chapter 3 becomes playable only when actual Chapter 2 story completion is saved AND Chapter 3 content has been made available by its implementation release. Its unlock does not read the old mastery passing percentage. A Bronze side-quest badge, low XP, low trust, or empty wallet never blocks corrected story work.

Entry preserves the player's exact saved Public Trust, wallet, purchases, capacities, supplies, mastery records, settings, and Rosa choice history. Do not initialize a new campaign or assign the example entry values below to a real save.

The main menu's Continue opens Career as it already does. The Chapter 3 sheet shows `Start Chapter` or `Resume`, an appropriate current-task line, two cases, and one side-quest badge space. Starting a chapter through this sheet is the launch action. Do not invent a second direct Continue-to-level route.

The chapter completes after all of the following: corrected worksheet acquired; Bea approved and her copy delivered; company approved and its copy delivered; closing story fact recorded; wrap-up acknowledged and saved. A result popup appearing or a shop purchase never substitutes for those milestones.

### Item eligibility on entry and on case switch

| Item/system | Chapter 3 behavior |
|---|---|
| Arthur's Reviewed File | Same owned read-only reference, no reacquisition; no transfer of its answers into active work |
| Notes | Same tool identity, chapter-appropriate objectives; extend scope rather than retire it accidentally |
| Case Folder | Same container/action vocabulary; Bea becomes the active binding, then company after her delivery |
| Tax Code Book | Same viewer and ownership/access behavior; add year-labeled computation leaves; free desk copy remains available |
| Rosa's Marked Records | Chapter 2 tool retires from active inventory under the established eligibility model; never usable as Bea evidence |
| Client IDs binder | Does not reactivate the four-person Level 1 quest |
| Bookmarks | Same owned set and three-pin limit; source identities remain case-scoped |
| Vouchers | Carry exact remaining quantity; spent vouchers stay spent |
| Satchels/pouches | Exact saved capacities; no new purchase or capacity reset |
| Pencil cup/island print | Restore the same ownership and compatible office placement; no score or trust effect |
| Coast postcard | Keep ownership/readability and supported display behavior; do not promise a new display anchor automatically |
| Worksheet, strips, tapes, 2307 | Documents within active Case Folder; zero extra standalone slots |
| Calculator | Free fixed workstation equipment, not another compulsory inventory item |

For pins, resolving a retired source must give a clear unavailable-in-this-chapter state and an Unpin action, not silently open the new case with the old amount. Active worksheet and source references may use the same three-pin feature once bound to their actual viewer. This is a small content binding, not an answer panel.

The initial required set fits the existing base 3-slot hotbar plus 6-slot Bag. If optional carried objects fill capacity, use Make room before a required acquisition, retaining the source until acquisition commits. Individual receipts and tapes do not fill slots. No playable drawer or mandatory satchel purchase is added.

Completed case results and learning outcomes stay saved. Bea's frozen computation snapshot must also remain available within the chapter checkpoint through the company case for recovery and the optional computer preview. This is a bounded two-case snapshot extension; it does not imply that every previous chapter's folder can already be reopened from an archive.

## 14. Public Trust hearts, reputation history, and dialogue consequences

### Exact proposed service rewards

Use the existing PUBLIC TRUST display: five proportional hearts plus `PUBLIC TRUST n/100`, and the separate `Community support target: 65` label. The support target remains informational here. XP uses stars/numbers; accuracy does not become another public heart meter.

| Durable event | Trust | Player-facing reason | Timing |
|---|---:|---|---|
| Bea worksheet corrected and actually acquired | +2 | `Bea's records organized and checked` | Same successful save as corrected field completion |
| Approved Bea computation copy delivered | +4 | `Bea: computation explained and copy delivered` | Successful awaiting-action handover with explanation outcome recorded |
| Approved company computation copy delivered | +4 | `Marine Services: supported computation delivered` | Successful second client handover |
| Inspection, draft edits, correct key presses, repeated talk, hint use | 0 | No trust toast | Ordinary work |
| Failed check, honest wrong stamp, review retry, missed cutoff | 0 | Correction/review feedback only | Mastery/time systems handle these |
| Collecting keepsake, purchasing computer, reaching Gold | 0 | Their own reward/ownership feedback | No manufactured community confidence |

These are proposed additions to the fixed campaign reward catalogue, not currently implemented events. Maximum Level 3 service gain is +10. Each claim includes campaign + chapter + distinct activity + kind. Two case deliveries require distinct IDs; reusing a single chapter-wide `case-approved` claim would accidentally suppress the second reward.

Example complete story paths, assuming no clamping or unrelated changes:

| Prior Rosa choice | L1 completion | L2 service and ethics net | L3 entry | L3 corrected completion |
|---|---:|---:|---:|---:|
| Responsible help | 46 | +6 +2 | 54 | 64 |
| Unsupported reassurance, later resolved | 46 | +6 −8 | 44 | 54 |
| Dismissive hold, later corrected | 46 | +6 −3 | 49 | 59 |

The responsible path stays just below 65 after Level 3, leaving meaningful trust growth for later service. These are arithmetic path fixtures, not balance measurements. Existing trust always clamps to 0–100; record the actual applied delta. Completing this chapter does not erase Rosa's professional history.

### Dialogue choice policy

Three kinds of choices serve different purposes:

1. **Inquiry choices** reveal relevant context, such as what a purchase was for. They have no automatic heart reward.
2. **Service-plan choices** let Elijah explain how he will help; the plan becomes a small saved task. Completion is recognized through the normal delivery reward, never an additional dialogue bonus.
3. **Explanation choices** test whether Elijah can describe the result. A mistaken explanation gets a correction prompt, not an accusation of dishonesty.

There is no new high-stakes knowing-misconduct branch in Level 3. Preserve Rosa as the first major ethics event and the representative pressure for Level 4. A repeated three-option good/evil/dismissive menu in every chapter would weaken that progression.

### Authored conversations

**Reception, on first entry.**

- Responsible Rosa history: `Rosa appreciated getting her checked copy. Bea has booked a computation appointment.`
- Unsupported promise, resolved: `Rosa's corrected copy is delivered. Bea needs a checked computation before making plans.`
- Dismissive history: `Rosa's case is closed. Bea has brought her records and would like you to explain the result.`
- Missing/neutral history: `Bea has booked a computation appointment. Her records are ready in the plaza.`

Use one fitting line, then the task. These lines acknowledge durable history without reapplying its consequences or publicly shaming Elijah. They never claim Rosa recommended him if the recorded history does not support that wording.

**Bea, before the worksheet.**

Bea: `I mixed the studio receipts with some personal ones. Which should go into the calculation?`

Available inquiry choices, flexible order:

- `What was this purchase for?` → selecting the birthday record reveals `My birthday dinner and decorations. The studio wasn't involved.`
- `Does this invoice belong to the annual total?` → selecting the printing invoice reveals `Yes, that supplier bill is in the printing summary.`
- `Show me the period these records cover.` → points to year and coverage fields; no correct destination is named.
- `I'll review the papers first.` → leave dialogue and retain both questions for later.

Questions have appropriate selected-record context; do not require random dialogue cycling. All graded facts are also visible in accessible source records. Essential tax evidence is not hidden in a one-time conversation.

**Bea, after the supported worksheet.**

Bea: `Could you help me understand how to keep this organized next time?`

- `Let's separate business and personal receipts before I explain the total.` → save service plan `separate-receipts`; during delivery, player matches one business example and one personal example to their labeled sleeves.
- `I'll mark the worksheet so you can trace each total back to a record.` → save service plan `trace-totals`; during delivery, player points to one subtotal and opens its linked source.
- `Let me finish the computation, then we can choose how to go through it.` → save no plan yet; present those two routes at delivery.

Both service plans are equally professional and earn the same existing +4 delivery reward. They require one brief, meaningful follow-through action, not another full minigame. Optional Talk later exits without a promise or penalty. Switching an unfinished plan requires an explicit conversation and updates the pending task; completed plans do not pay again.

**Bea, when the credit becomes relevant.**

Bea: `A client withheld some tax from a payment. Is that another business expense?`

- `It reduces the remaining tax when supported by the certificate.` → proceed to the credit row and certificate link.
- `It reduces the income amount before the tax calculation.` → `Let's check where the withholding belongs.` Open the relevant book explanation; retain the player's draft until they correct it.
- `Let's inspect the certificate together.` → open the source; return to the same question or working field.

The answer is a learning check. It earns no separate hearts and does not freeze another badge attempt. The credit cannot be silently inserted because the player chose a line of dialogue.

**Company bookkeeper, after inspecting the travel record.**

Bookkeeper: `The prepared schedule calls that administrative. I thought the Desk had checked it.`

- `What was the trip's purpose?` → confirms the personal family holiday; journal references the retained travel record and interview context.
- `Let's compare that label with the original record.` → opens the existing two-paper comparison; player records the treatment.
- `I'll finish reviewing before I give you a figure.` → return to work with the question available.

A structured answer does not automatically choose the worksheet destination. The player still applies the supported treatment.

**Company delivery.**

Bookkeeper: `Your figure is higher than the Desk's. Can you show me why?`

Choose the relevant statement, then point to its supporting worksheet row:

- `The personal trip cannot reduce this company's taxable income in this case.` → show ₱120,000 adjustment and continue.
- `The company pays the individual graduated bracket.` → prompt rule review; correct the explanation without a moral penalty.
- `A higher figure proves someone intended to mislead you.` → prompt `We can support the correction. Intent needs additional evidence.` Correct the claim before final explanation commits.

**Repeated and resumed consultation.** Select one line based on actual phase: records not collected, worksheet draft, corrected worksheet, calculation pending, audit returned, new review day, copy pending, delivered. Prioritize an unresolved actionable clue, then the relevant review reaction, then short optional small talk. No repeat record drops, reward farming, false urgency, or conflicting NPC movement owner. Bea can show thought/concern/relief through the existing emotes; a heart burst is reserved for the committed service reward.

## 15. Coins, XP, badges, upgrades, and purchase pacing

### Fixed coins and performance-based mastery

| Activity | First story completion coins | Proposed mastery XP |
|---|---:|---:|
| Worth the Tax?, corrected and acquired | 60 | 60 maximum; 60 − first-check wrong decisions, floor 48 |
| Bea's supported case approved | 100 | 100 maximum |
| Company's supported case approved | 100 | 100 maximum |
| Dialogue, delivery, final card, optional shopping | 0 | No separate farmable bonus |
| Chapter total | **260** | **260 maximum** |

The XP formula reuses Level 2's forgiving reward-rule fields; coins remain fixed regardless of Bronze/Silver/Gold. No tax amount enters the coin wallet. Show tax currency as `₱` and office currency with its coin icon.

Record side-quest XP once for the chapter, and case XP once for each distinct case. An intermediate Bea receipt may summarize earned-so-far mastery, but the company result must not award the worksheet XP again. The final chapter total combines those three saved sources. Extending the current ledger/results to two cases must preserve this separation as well as the distinct coin claims.

**Twelve side-quest decisions:** seven strip destinations; personal-purpose reason; already-represented reason; duplicate invoice → printing-total relationship; income subtotal; expense subtotal. Source/amount extraction is deterministic, so no extra points for an automatically copied value. The two subtotal checks assess whether tape matches the player's selected entries, including a wrong treatment; they do not deduct again merely because the same wrong destination changed the total. Treatment correctness is assessed in the destination decisions. Full completion nevertheless requires every treatment and total to be correct.

This separates two skills fairly: choosing what belongs in a sum and actually computing that chosen sum. Gold = 12, Silver = 10–11, Bronze = 0–9. The first complete deliberate Check freezes the score. Later corrections do not lower or raise it. A completeness indicator says `12 decisions recorded`, never `12 correct`, before Check.

For each main case, propose the existing Level 2 case settings: 100 maximum XP; 2 XP per root mistake found on a submitted review; 1 XP per extra submission; maximum combined loss 15 XP. Thus corrected completion yields 85–100 XP per case. Applying these settings to new computation fields is proposed content work, not a claim that the current validator already handles them.

Do not count a stale tape, wrong taxable income, wrong tax due, and wrong balance as four mistakes when all depend on one wrong cost. Independent mistakes may each count. Same-return feedback identifies root cause IDs; retries follow the existing case-result mechanism. No draft keypress, correction check, hint, or miss-drag creates a new audit penalty. Preserve existing first-cutoff mastery behavior if configured, inside the same cap; no deadline penalty affects Public Trust.

Badge presentation reuses the existing career medal style: **Worth the Tax?**, proposed ID `worth_the_tax_3`, existing receipt emblem, and a distinct amber ribbon. It is one side-quest badge, not one badge for every document or case. The chapter card shows case outcomes, total mastery XP, first-check badge tier, earned coins, and global trust as distinct information. Do not display internal legacy score-heart values as a second social meter.

Mastery replay can improve best XP and badge under the current best-record policy. It cannot add XP repeatedly as an unlimited sum, earn the 260 coins again, change story choices/trust, or consume live supplies. Practice only updates lesson progress. Direct Editor attempts cannot change story state. A replay uses local copies of inputs and props.

### Exact existing upgrades and their Level 3 use

| Product | Existing price | Owned benefit in this chapter | Free route |
|---|---:|---|---|
| Bookmark Set | 40 | Up to three actual source/working-paper pins; useful for worksheet, cost support, certificate | Normal Bag/folder navigation |
| Satchel | 50 | Bag capacity 6 → 9 | Base inventory and container papers |
| Large Satchel | 90 | Bag capacity → 13; requires Satchel and completed Chapter 2 | Same required-content fit at base capacity |
| Belt Pouch | 60 | Hotbar 3 → 4 | Move tool between Bag and hotbar |
| Second Pouch | 100 | Hotbar → 5; requires Belt Pouch and completed Chapter 2 | Existing three usable slots |
| Review Voucher | 30 each | +180 active seconds on eligible Bea OR company case; one per case, two carried | Free next-day review with retained work |
| Brass Pencil Cup | 30 | Same installed desk decor | No gameplay effect |
| Framed Island Print | 60 | Same installed wall decor | No gameplay effect |
| Comparison Stand | 120 teaser | Remains deferred/unpurchasable; no required new interaction | Existing free comparison |
| Office Computer | 250 teaser becoming functional | Proposed purchase after completed Chapter 3 and feature readiness | Calculator/paper workflow continues |

The newer slot upgrades become available because Chapter 2 was actually completed, not because the player has started Chapter 3 or earned Gold. Reentering the chapter never awards them free or changes their prerequisite chain.

By completed Level 3, first-story earnings across Levels 1–3 total 160 +160 +260 = **580 coins**, before any existing optional pickup or spending. All currently functional permanent purchases listed above total **430**; buying all leaves **150**, so the computer must not be required immediately. Saving 250 for it is viable, while a player who prioritizes every capacity/decor purchase can buy it after later earnings. Vouchers create additional optional expenditure. Do not reimburse earlier purchases or increase rewards merely to guarantee every product at once.

### Computer milestone: a complete optional feature

The existing computer is a pending shop entry with no install anchor. Planned Level 3 implementation must add the actual model/placement and a small useful interaction before it can accept coins. Use the same product ID, price, wallet, purchase confirmation, ownership and installation command as other decor/equipment.

Immediate benefit: inspect a read-only register of the two verified computation snapshots from this chapter, select a client and open that supported worksheet with its sources/tapes. No auto-solving, automatic treatment, or new answer key. The purchase uses existing confirmed record access; it does not require a global old-case archive. In Level 4 the same owned equipment gains the planned confirmed transfer and draft-printing workflow.

After saved chapter wrap-up, offer `Career` and `Visit Supply Counter`. The latter is a short optional post-completion office state: no new review timer, no replayed client copy, no reopened case or extra chapter reward. The player may buy/install/inspect the computer or leave. A purchase made through Career's shared shop restores the same device on the next supported office visit. If an installation anchor is unavailable, refuse before spending and retain the clear reason.

This optional post-completion state is a proposed route extension. If the computer's complete feature slice has not shipped, keep its current pending teaser; do not sell a future promise. That must not prevent Chapter 3 story completion. All purchases and reward claims remain candidate → save → publish; animation and sound follow successful persistence.

## 16. Worth the Tax? — tactile interaction and correction detail

### Workbench composition

Use one close workbench camera and existing physical paper rendering. The selected source sits on the left, the current worksheet section in the center, and a compact calculator/tape dock on the right. On narrow screens, show one readable source and one active section at a time with large labeled tabs, rather than shrinking all four sections and all papers into a miniature dashboard.

An available-record rail lists collected sources; selecting a row changes which original is visible. The rough worksheet is a comparable document, not an extra permanent popup. At most two originals are in comparison focus at once. The current task line remains short: `Decide where Bea's records belong.` A neutral progress counter reports placed entries.

### Seven strips and actual record identity

The seven authored rows are the two income categories, three business costs, personal purchases, and the individual printing invoice. The invoice and annual printing total deliberately have different source IDs but a shared transaction reference. They can therefore be incorrectly counted twice as content choices. Repeatedly extracting the same source row, however, focuses its existing strip instead of making unlimited duplicate copies.

Automatic source retention prevents accidental wrong-case provenance. Players still decide the support relationship between the invoice and the annual total. Opening a strip's small source tab focuses the original row; it never opens an answer explanation before Check.

### Gesture sequence

1. Tap a readable row. Its border lifts and a brief action label appears. This is selection, not a correctness signal.
2. Tap `Add to worksheet`, or drag from the row handle. A working strip lifts in roughly 0.2–0.3 seconds.
3. Tap a destination section, or drag into its broad tray. Show a neutral placement shadow and section name before release. Every plausible content destination accepts it without green/red correctness feedback.
4. Release to settle in roughly 0.15–0.25 seconds; reflow neighboring strips without overlap. A drop outside a tray returns to its previous slot, with no mastery penalty.
5. Tap a placed strip to `Move`, `View source`, or `Undo last move`. Tap-select/tap-place offers equivalent functionality to dragging; prolonged holds are not required.
6. Tap either of the two rough-worksheet claim notes to choose its short treatment reason. Their fields stay visible regardless of destination, and the same neutral reason list remains available before checking. Do not reveal correctness by offering a single valid option only in its correct tray.
7. On the printing claim note, select `Link related entry`; tap the row being referenced. Show a removable labeled paperclip relationship. Any candidate can be chosen; checking evaluates the semantic link. This required link remains accessible even if the invoice is incorrectly placed in Expenses rather than Supporting Only.

The optional drag retains the finger-to-strip offset; the selected strip sits slightly above the finger so its label remains visible. The task tests tax reasoning, not whether a player can hit a paperclip. A lost touch/cancel returns to the last committed position.

### Calculation within the side quest

Selecting Income Total or Expenses Total opens the same calculator used later. The player chooses the included amount chips from that section, presses Add/Equals, then attaches the tape. Choosing a sum does not choose a treatment: a personally placed receipt still appears in that section until the player moves it.

Changing a section after attaching its subtotal leaves the old tape visible with `Entries changed — recalculate`. Do not erase its expression or silently overwrite its result. The player can recalculate with the revised selected inputs. Source totals, worksheet totals, and calculator results remain visibly distinct.

### Check and correction loop

Use one deliberate `Check worksheet` action. Unlike Level 1's automatic fourth-card check, this puzzle has multiple reasons, links, and numerical fields; filling the last slot is not informed consent to freeze a grade. A one-time line before the first check explains this difference from the earlier board.

If incomplete, show the missing item count and focus the next empty field. If complete, freeze the twelve-decision result, then place small pencil notes beside errors. Keep correct work stable. The first feedback names a concept and source; subsequent requested help gives a more specific clue. Any explicit worked answer used for repeated difficulty is marked as assisted in the current attempt presentation, never converted into a better first-check medal or a trust penalty. New cross-campaign hint statistics are unnecessary.

Each correction permits another Check. No per-edit buzzer or repeated whole-board lecture. Once all decisions and current totals are correct, show `Supported worksheet ready`; the player deliberately collects it. Acquisition and completion/reward save together. Reading feedback is useful; collecting the finished paper is the satisfying endpoint.

### End-to-end example

A player puts the personal purchase in expenses, places the printing invoice in Supporting Only, links it correctly, and calculates ₱324,000 from their chosen expenses. First Check flags the personal destination/reason if wrong, while recognizing that the addition matches their actual chosen rows. The hint points to purpose. They move the ₱24,000 receipt to Not Deductible, choose Personal purpose, update the stale expense tape to ₱300,000, and Check again. Corrected completion earns the fixed 60 coins/+2 trust and their frozen original mastery result. Their next objective is to compute taxable income at the office.

## 17. Calculator and case solving — exact player contract

### Desk and paper layout

Keep the Case Folder's familiar first pages: editable taxpayer information, then Supporting Documents. Add working leaves for the supported income/cost worksheet, Computation, and Review/Client Note. Reuse existing book/page-turn/extraction behavior. There is no forced rewrite into a new full-screen application.

On Computation, show six named steps: Revenue; Allowable Costs/Expenses; Taxable Income; Rule and Tax Due; Credits; Remaining Payable. Display no more than the current step plus a short completed-step summary when screen space is limited. Completed entries are revisitable. Navigation never requires the player to remember a number from a hidden page.

### Calculator inputs

The physical calculator has digits, decimal point, Backspace, Clear Entry, +, −, ×, ÷, %, Equals, and `Attach result`. Normal tap-release enters once; holding a key does not produce repeated digits. The active expression, current entry, case name, and destination label stay readable. Clear Entry clears only the current editable value; editing a recorded tape requires selecting that tape. No destructive all-tapes reset sits beside Equals.

Players may enter a number on the keypad or select a printed amount and tap `Use amount`. Selected amounts become labeled chips that carry source row and case identity. A manual entry used as evidence must be linked to its source before final review. Technical confirmation says `Value attached`, never `Correct`, before the player's check/review.

Use deterministic percent behavior: `A × 20 % =` means `A × 0.20`. Do not use consumer-calculator shortcuts such as `A + 20%` with ambiguous contextual behavior. The teaching route uses short separately labeled operations so precedence is never a hidden puzzle. Unsupported or malformed syntax prompts a clear input correction without an audit penalty.

Currency retains cents internally using an appropriate exact-money representation; display two decimals when necessary and use the educator-approved rounding rule at defined tax-output boundaries. Never silently truncate or clamp an overpayment/loss into zero. The authored cases avoid those branches; unexpected negative results stay visible as a check-needed calculation rather than being presented as a valid payable amount.

### Tape and attachment

Equals creates a draft tape containing selected inputs, operators, result, and source tabs. It does not write the result into every case field. `Attach result` opens the current relevant destination choices; player selects a field and confirms. Attaching an inappropriate but numeric result is a possible content mistake, not an automatically refused drag. Mechanical invalidity such as another client's source or no destination gives an explicit reason and no change.

Each named field has one attached tape. Replacing it retains Undo until the replacement is saved; the case review sees the current revision. The visible tape history is organized by the six steps with the latest relevant line prominent, not an endlessly growing receipt wall. Retain prerequisite lines needed to explain a chain; superseded scratch work can be collapsed and deliberately removed without deleting source records or the frozen check.

### Bea's exact calculation sequence

| Step | Player chooses | Arithmetic/result | Remaining decision |
|---|---|---|---|
| Bring in totals | Two supported worksheet totals and target fields | Revenue 900,000; expenses 300,000 | Confirmation keeps source identity |
| Taxable income | Revenue, minus expenses | 900,000 − 300,000 = 600,000 | Attach to Taxable Income |
| Select rule | Annual individual table; containing bracket | Over 400,000 through 800,000 | Confirm regime/year and bracket |
| Excess | Taxable income, minus bracket threshold | 600,000 − 400,000 = 200,000 | Attach intermediate tape to rule calculation |
| Marginal portion | Excess, times bracket percentage | 200,000 ×20% = 40,000 | Keep as linked intermediate result |
| Tax due | Base tax, plus marginal portion | 22,500 +40,000 = 62,500 | Attach to Tax Due |
| Credit | Matching certificate's withholding field | 12,500 | Attach source to Credits, not Expenses |
| Remaining payable | Tax due, minus supported credit | 62,500 −12,500 = 50,000 | Attach to Remaining Payable |

The bracket selection reveals a generic labeled formula scaffold: `base tax + rate × (taxable income − threshold)`. It supplies the selected rule's constants, not the chosen bracket or the client's tax answer. Rule cards explain conditions without containing a Bea-specific solved example.

The company route reuses all of this: choose supported 8,000,000 and 2,000,000 totals, subtract, confirm the ordinary corporate regime/rate against supplied conditions, multiply 6,000,000 ×25%, link the 300,000 credit, subtract. Keep the familiar calculator and field layout. Only the applicable rule and case facts change.

### Practice help and independence

The first tutorial teaches tool operation using non-tax budget arithmetic. The later rule explanation demonstrates graduated structure using a separate small non-client example clearly labeled with its own applicable supplied rule. It must not insert Bea's ₱600,000 or reveal which bracket contains her amount. Players can reopen instructions through the existing book/tutorial help, without replaying pickup/reward events or indefinitely freezing independent review time.

Selecting and reading a number is not a timed dexterity event. No bonus is awarded for entering the same long number manually when source selection is available. Challenge comes from source, treatment, rule, and destination decisions.

### Validation, revisions, and review

Maintain a dependency chain: supported rows → totals → taxable income → selected rule/tax due → credits/balance → stamp. Changing a taxable amount marks downstream tapes `Needs recalculation`; changing only a credit marks the balance/stamp, not the unrelated income arithmetic. Reading a source or moving its visual position does not change the case revision.

Retain a correct prior calculation as visible history while it is stale. A new stamp must cover the current revision. Do not automatically choose Not Ready or overwrite the player's professional recommendation. The player may record a justified interim hold; unresolved work remains open.

The Auditor checks source support, applicable rule/period, arithmetic, destination, and current stamp. A lucky matching final number with the wrong unsupported treatment does not pass as supported work. Equivalent valid arithmetic is accepted: a combined expression or mathematically equivalent ordered subtapes may both establish the same rule. Validation compares supported dependencies and the applied rule, not one rigid sequence of button presses.

Example returned review: `The tax-due line uses 20% of all taxable income. Review the selected bracket's base and excess.` Focus the tax-due calculation, retain the correct ₱600,000 taxable-income line and certificate, and leave only affected downstream work pending. There is no mandatory walk back to Bea for an arithmetic correction.

### Smoothness and feedback targets

| Action | Proposed feel target | Meaning |
|---|---|---|
| Calculator key | Visible depression/entry response within the next rendered frame; 0.06–0.1 s settle | Input registered, no tax judgment |
| Equals | 0.15–0.25 s tape feed with readable result immediately available | Arithmetic performed |
| Attach | 0.2–0.35 s tape settles under the selected field | Result recorded |
| Source tab | Quick camera/document focus with source heading retained | Traceability |
| Worksheet placement | 0.15–0.25 s settle, one neutral paper sound | Draft placement |
| Approved handover | Existing folder flight/awaiting-action motion, then restrained emote | Saved service completion |

These are design targets for user testing, not measured latency or animation acceptance. SFX obeys existing audio preferences; information remains in text if sound is off. Reduced-motion presentation uses still states where the existing preference is available, without claiming a new settings row has already been built. Input remains responsive; a flourish does not block the next safe action unnecessarily.

## 18. Client delivery, chapter wrap-up, and next-chapter continuation

### Two complete client resolutions

Approval records the case result and its 100 coins once. It then produces the existing read-only Client Copy flow with chapter-appropriate `COMPUTATION COPY` labeling. The copy has the verified computation and source references; it is not an official filed return or a payment receipt.

The Auditor says `Approved for this computation review. Give Bea her copy and explain the balance.` Pickup flies into the existing folder icon. Consultation enters the reusable awaiting-action step; drag the copy onto the correct client, or tap the folder icon as the existing accessibility alternative. Wrong drops return it without penalty. Another client's NPC cannot receive it.

Bea's selected service-plan action is completed before the final handover commit: either show the two receipt sleeves or trace one source-linked subtotal. The company explanation similarly resolves before the final copy handover. The explanation outcome, delivery, +4 service claim, and next task save together. A dropped connection/save failure preserves the copy and retry state, and shows no heart burst. Repeated talk never pays again.

After Bea's delivery, show a small case-closed receipt and `Next: Marine Services appointment`. Keep the player in the chapter; do not show Chapter 3 Complete or return to Career between cases. The receptionist is the sole source of the next assignment and gives a short record handover. Bea's consultation resolves and she leaves or returns to an optional idle state through the same NPC owner; the company actor uses the room only after it is available.

### Qualified closing discovery

After the company delivery, Elijah records the shared Assistance Desk preparer identity and supported correction. Choose `Record the discrepancy and retain both sources` or `Review the records once more` before committing. A speculative accusatory explanation is correctable and cannot be filed as an established fact. Neither option farms trust.

The mayor's concern can arrive through one short receptionist message, with the quoted Quax response recorded in Notes. Keep Arthur's guide identity intact; a physical Quax encounter is optional story presentation requiring its own approved actor, not a hidden prerequisite. A neutral confidence-aware closing variant may acknowledge Elijah's earlier revised promise, but must not repeat the Rosa penalty.

Final chapter receipt shows: both computations approved; both client copies delivered; worksheet badge and mastery XP; coins earned this chapter versus current balance; current Public Trust and this chapter's named +10 maximum service change; computer eligibility and price. Chapter completion commits at final Continue, then offers Career or the optional supply-counter epilogue described above.

### What Level 4 inherits

Level 4 starts with a brief appointment and the same owned office, wallet, trust/history, capacities, pins, supplies and settings. It assumes the player knows how to compute; its tutorial teaches return preparation only. The supplied verified figures may be checked/recomputed when a document change warrants it, rather than repeating Bea's seven-strip puzzle.

Owned computer: let the player choose and confirm verified amounts to transfer to a draft return. No computer: use the same figures through the paper workflow. Both routes require form/period/support review and the authored representative-pressure choice. The version-tracing quest supplies process evidence missing from Level 3. Only then does Quax commission the authorized review that begins Level 5.

The new professional-history facts carried forward are modest: how Bea was helped, whether both computations were explained/delivered, and what Assistance Desk discrepancy was actually supported. They join Rosa's existing history; they do not replace it or create a parallel morality meter.

## 19. Checkpoints, timing, interruption, and fair challenge

| Interruption point | Resume state | Never repeated |
|---|---|---|
| Before Bea records commit | Source handover available | No false inventory acquisition |
| During a strip drag/key entry | Latest stable draft; no held strip | No duplicate strip or digit from input replay |
| After first worksheet Check | Frozen first grade plus correction notes | No second first-check score |
| Worksheet corrected, before pickup | Ready worksheet available | No premature coins/trust |
| After pickup | Supported worksheet inside folder; office objective | No second 60 coins/+2 trust |
| During calculation | Attached/draft tapes and selected rule restored | No automatic answer replacement |
| After audit return | Existing sources/tapes retained; targeted correction task | No repeated penalty merely from loading |
| Approved, copy at Auditor | Approved case/result retained; pickup available | No second 100 coins |
| Copy carried, before explanation/handover | Correct client's delivery task; copy retained | No false +4 trust |
| Bea delivered, company not received | Next appointment task | No reactivation of Bea's approval/delivery |
| Company underway | Company active; Bea completed snapshot retained | No mixing sources or voucher claims |
| Final card pending | Pending wrap-up after saved client resolutions | No duplicate chapter rewards |
| Post-completion shop | Saved completion/ownership; safe exit to Career | No reopened chapter or repeated ending |

One chapter checkpoint has distinct Bea/company subrecords and one active-case identifier. This bounded extension is essential: copying Level 2's single-case result structure unchanged would lose or overwrite one result. Reuse the same signed owner/transaction boundary rather than installing another save system.

Clock policy follows existing behavior: compulsory teaching, appropriate dialogue/settings/catalogue pauses use established owners; independent source inspection, worksheet decisions, and arithmetic consume the active review budget. Visible pause reasons distinguish a lesson from working time. The calculator never adds a second unrelated countdown.

Proposed first-playtest settings: Bea receives a 16-active-minute review budget after the protected calculator introduction, including her field work and office computation. The company receives a fresh 10-active-minute budget when its appointment begins. Announce each through the existing review-hours presentation and convert using the current clock rate; do not introduce a competing timer. Do not carry Bea's nearly exhausted budget into an unseen second assignment. Next-day review is free and preserves both cases' completed work. One voucher may extend each case once, including across day rollover; players cannot buy repeated extensions on the same case.

Tune those starting values using the user's measured Level 2 reading/handling times and Level 3 playtests. Aim for most attentive first-time players to finish normal independent work before cutoff; the chapter's learning difficulty comes from decisions, not forced voucher spending. A first late review may affect the existing punctuality/mastery treatment, within configured caps, but never Public Trust or campaign access.

## 20. Quality standards and acceptance for the revised plan

These interaction choices apply documented accessibility principles to this game; they are not a claim of certified compliance. [Microsoft XAG 107](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/107) supports alternative input paths rather than demanding precise or sustained actions. [XAG 114](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/114) supports clear screen context and predictable actions. Their application here is tap-place alongside dragging, named calculator destinations, persistent context, and deliberate scored Check.

For Android, target at least a 48 ×48 dp-equivalent touch region for primary controls at the actual device scale, as recommended in [Android accessibility guidance](https://developer.android.com/guide/topics/ui/accessibility/apps). Unity world-space dimensions are not dp: verify effective on-screen size after camera/canvas scaling. Keep normal text contrast at least 4.5:1 and larger text at least 3:1, with color-independent labels. Physical styling must not produce unreadable fine print, tiny buttons, moving reading surfaces, or compulsory precision dragging.

The essential design tests are:

1. **Continuity:** L2 completion → Career → L3 start/resume preserves exact wallet, trust/history, bag/hotbar capacity, pins, supplies, decor, settings and Arthur reference. No profile round or Rosa choice repeats.
2. **Worksheet purpose:** the collected supported worksheet feeds the actual case fields by explicit selection; completing it never merely unlocks an unrelated prefilled case.
3. **Neutral drafting:** wrong-but-plausible placements and calculations remain possible; feedback does not reveal answers through snapping, color, or auto-fill.
4. **Fair scoring:** twelve first-check decisions are explicit; dependent numerical changes do not multiply the same error; hints and mechanical misses do not reduce trust.
5. **Calculator clarity:** players can distinguish a printed source amount, selected input, intermediate tape, attached result, tax due, and remaining payable. Valid equivalent calculations pass.
6. **Two-case integrity:** results, credit certificates, copies, voucher claims and rewards stay case-specific; entering company work cannot overwrite Bea's delivery or result.
7. **Service trust:** worksheet pickup +2, Bea delivery +4, company delivery +4, once each; all three Rosa history paths retain their expected differences. No heart gain from merely choosing polite dialogue.
8. **Reward/economy:** 260 fixed first-story coins; 260 maximum mastery XP under the proposed settings; correct prices/capacities; every required task works with zero purchases.
9. **Computer honesty:** only a complete installed, saved, immediately useful device accepts payment. The deferred stand stays unavailable, and a pending computer never gates chapter completion.
10. **Story restraint:** players can state what the Assistance Desk records establish and what remains unknown. A higher corrected liability does not automatically prove intent.
11. **Recovery:** interrupted drafts, returned audits, approvals, handovers, wrap-up and purchases neither lose work nor duplicate rewards, consequences or ownership.
12. **Feel:** user-tested phone layouts allow comfortable tap-place, clear calculator entry, still readable paper, brief satisfying motion, predictable Back behavior and one input owner. Routine operations do not feel like seven repeated menus.

During implementation, internal checks should cover math, state/rule transitions, once-only claims, reward paths, source binding, dependency invalidation, run-mode isolation, save failure and interruption. User acceptance covers Unity rendering, paper readability, reachable targets, actual movement, animation, audio, mobile touch, timing, performance and fun. This planning revision ran no Unity session and makes no device-tested claim.

### Scope discipline and source pointers

Level 3's required additions are the worksheet/tape/calculator interaction, two-case progression, the relevant new record presentation, authored dialogue/service plans, and their saved campaign/reward bindings. Existing L2 components supply patterns; names such as `Level2CampaignRules`, the Rosa-specific ethics delta method, and single-case result records are chapter-specific, not drop-in generic Level 3 APIs. Extend the established owners with bounded Level 3 rules rather than routing Bea through Rosa's IDs, replacing the campaign framework, or merely renaming copied components.

One optional discovery may reuse the existing keepsake acquisition pattern: a small Coast Tales illustration at Bea's table, with her permission to collect it after the record handover. It is a separate optional object with a brief artist note, no answer, no coin/XP/trust reward, and no mandatory trip. Use Make room if necessary and retain the source on cancellation. Reuse an existing supported display interaction only if a compatible anchor exists; ownership/readability does not depend on building a new shelf. This optional item can be omitted from an initial chapter slice without weakening the cases.

Inspected project references for this revision:

- [Campaign Foundation rules and amendments](CampaignFoundation/SPEC.md), [current Level 2 continuation](CampaignFoundation/LEVEL2_CONTINUATION.md), and [Delivery B handoff](CampaignFoundation/DELIVERY_B_HANDOFF.md).
- [T08: service trust and client-copy handover](CampaignFoundation/tickets/T08-public-trust-and-follow-through.md), [T09: fixed completion coins](CampaignFoundation/tickets/T09-completion-coins-and-wallet.md), and [T19: Rosa's explicit choices](CampaignFoundation/tickets/T19-rosa-choice-and-trust-consequences.md).
- [T12: bookmarks/capacity upgrades](CampaignFoundation/tickets/T12-physical-shop-and-bookmarks.md), [T13: shared shop and decor](CampaignFoundation/tickets/T13-menu-shop-and-office-decor.md), [T14: voucher rules and drawer-removal amendment](CampaignFoundation/tickets/T14-appointment-extensions.md), and [T18: item/campaign continuity](CampaignFoundation/tickets/T18-level2-campaign-and-inventory.md).
- [T33: phase-sensitive dialogue and complete Level 2 flow](CampaignFoundation/tickets/T33-level2-story-flow-and-polish.md), [world/feedback direction](CampaignFoundation/WORLD_AND_FEEDBACK.md), and [fixed supporting-document standard](SupportingDocuments.md).
- Actual `Assets/Resources/Campaign/shop.json`, `inventory.json`, `Career/chapters.json`, `Levels/Level_02/rewards.json`, Level 2 case/office content; and the `CampaignRewards`, `CampaignInventory`, `CampaignVouchers`, `CampaignRun`, `CampaignMenu`, `Level2CampaignRules`, `IncomeChallenge`, `CaseResultRules`, `LevelScoring`, and `TaxComputationCalculator` sources. Older comments in these files are not authority over their current behavior.
