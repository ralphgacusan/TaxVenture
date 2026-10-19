# Content and educational answer key

All names, reference numbers, issuer designs and financial figures are fictional. Documents must look like recognizable records, not generic paragraphs headed `Document 1`. Do not reproduce a real person's identifiers or official BIR seals. Display periods and payment status explicitly. Amounts below are fixed design fixtures, not Philippine tax advice or an official return.

## Approved business wording — 2026-10-18

Use **Rosa's Laundry** for the business and **laundry payments** for its main earnings. Explain the other receipts first as **payment for a separate blanket-washing job** and **payment for renting out her spare washing machine**. The separate job is ordinary business/service income; the machine payment remains rental income. Keep the work descriptions visible beside these categories.

This direction is implemented in the runtime case, records, dialogue and shop sign. Preserve `L2-ROSA-001`, S1/N1, D1–D6, E1–E4, F1/F2, semantic keys, reference numbers and evidence links. Changing a printed document title later must also update its matching documentName mapping in the same content change. Keep all amounts, scoring, reward rules and progression intact. The latest Findings interaction below uses this concrete paid-job scenario.

| Item | Player-facing wording | Amount / relationship retained |
|---|---|---|
| Business | Rosa's Laundry, Plaza Market, Stall 04 | Same Rosa and selected store/model |
| S1 / E1 / D1 | Clothes-washing payments | 12,000: bank 9,000 + cash 3,000 |
| S1 / E3 / D4 | Spare washing machine rental | 3,000, already on the summary |
| N1 / E2 / D3 | Separate blanket-washing job | 5,000, missing from the initial summary |
| E4 / D2 + D5 | Money borrowed that Rosa must repay | 20,000; same loan fixture and content-authority status |
| D6 | Laundry cash receipt | The same 3,000 already included in D1, never another amount to add |

## Learning alignment

Primary: identify inclusions from services/rent, separate source/type from treatment, distinguish cash received from income earned using evidence. Supporting: records must agree in scope, payment channel and summary coverage. Assessment is for an individual. Compensation is introduced through the demonstration; passive-income categories may appear as book vocabulary, but are not assessed without suitable records/rules. Corporate treatment is deferred; do not claim full coverage of the curriculum heading `Individuals and Corporations`.

The educational guide must distinguish `not income` from a statutory exclusion or income subject to a separate final tax. A genuine repayable borrowing is modeled as `Not income — repayable loan`, never as compensation, exempt business income, or an exclusion under Section 32(B). No debt cancellation or disguised payment is involved.

## Rosa and initial packet

Aling Rosa Santos, resident Filipino, owner/operator of Rosa's Laundry at Plaza Market, Stall 04. Use the user-selected `SM_Chr_Business_Female_03` model, shown under `TaxVentureWithCompliance > Characters`, from `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Female_03.prefab`. Retain this identity across stall dialogue, portrait presentation and finale; reuse compatible existing animation clips. No replacement model or new rig is required. Her store uses the selected `Building_4 (1)` instance from `TownEnvironment > Env > Town`, based on `Assets/Flat_Style_Vehicles/Prefabs/Buildings/Building_4.prefab`, placed near the plaza.

Cover: case `L2-ROSA-001`, tax year 2026, review focus May 2026 income schedule. Prior months/other reviewed periods are an already verified 120,000 aggregate. No deductions, credits or tax amounts are answer fields in this level. Full-year schedule context is supplied rather than pretending one month constitutes an annual return.

Initial summary S1: May clothes-washing payments 12,000; payment for renting out her spare washing machine 3,000; payment for a separate blanket-washing job absent; May total 15,000 (`SummarySubtotal`). Separate non-income memorandum: loan deposit 20,000, pending verification. Footer names the Assistance Desk as preparer; no accusatory copy.

Payment Note N1 records `Payment for: Blanket washing`, `Status: Paid in May`, reference CM-0526, with the original receipt at Rosa's shop. The summary does not list this separate job. This is a clue to investigate, not a label saying the Desk concealed income. Preliminary finding F1 references N1 and S1. For the initial comparison, N1 must contribute the actual payment statement (`ReferralNote`), not its reference, contact or location. The first field requires May total (`SummarySubtotal`), the entry the payment may affect; selecting another summary row is unsupported.

Initially show only cover, S1, N1. Supporting pages become available from Rosa; do not permit reading unretrieved evidence through a hidden HUD link.

## Six supporting records

| ID | Physical format and essential fields | Facts |
|---|---|---|
| D1 | Cream monthly service collection sheet; Rosa's heading, May dates, two channel rows, signature | May clothes-only washing work paid: bank transfer BT-0905 = 9,000; cash receipt RC-0531 = 3,000; monthly total 12,000 |
| D2 | Pale blue bank statement extract; fictional bank, masked account, dates, reference column, two credits | BT-0905: laundry payments 9,000; LN-0510: loan release 20,000; total credits shown 29,000, explicitly not labeled income |
| D3 | Laundry Receipt CM-0526; issuer/payer/reference/date, plain description of the work, paid annotation | Market Guesthouse paid Rosa 5,000 cash for blanket washing in May; this separate job is not included in D1's clothes-only collection sheet |
| D4 | Spare Washing Machine Rental Receipt RA-0520; payer/payee, washing machine, covered dates, amount and signatures | Rosa received 3,000 cash for letting someone use her spare washing machine for a few days; no security deposit or advance payment ambiguity |
| D5 | Signed short loan agreement/release record LN-0510; lender/borrower, principal, release date, repayment obligation | Genuine principal 20,000 released to the bank account in D2; repayable, no forgiveness or interest-income question |
| D6 | Narrow service cash receipt RC-0531; Rosa's issuer block, date, laundry-work description, paid amount | 3,000 paid in cash, already included in D1's 12,000; corroborates the channel difference, never an extra income item |

Each record is one short readable face. Use generous whitespace; 6–9 meaningful fields maximum, not fake microprint. D1 and D2 can be compared side by side at the office. Dates/references must match exactly across records. D6 must say `Included in May collection sheet` in ordinary bookkeeping language without giving the final finding.

## Four challenge entries and 12 scored decisions

| Entry | Source/type answer | Treatment answer | Evidence answer |
|---|---|---|---|
| E1: 12,000 laundry payments | Business/service income | Include in the regular income schedule | D1 |
| E2: 5,000 paid for blanket washing | Business/service income | Include in the regular income schedule | D3 |
| E3: 3,000 paid for using her spare washing machine | Rental income | Include in the regular income schedule for this stated ordinary rental | D4 |
| E4: 20,000 deposit LN-0510 | Borrowed funds; not an income category | Not income: repayable loan principal | D2 + D5 bundle |

E1's evidence answer deliberately identifies its originating collection sheet; it does not ask why D1 differs from D2. D6 is available but its reconciliation role is tested only at the office. No balance reconciliation or summary total appears in the quest grading.

Distractors: compensation, business/service, rental, passive income/interest and borrowed funds as appropriate. Do not present `passive` as automatically tax-free or automatically final-taxed. Treatment labels: regular income schedule, separate treatment requiring its own rule, statutory exclusion where applicable, not income. Only applicable rules may be graded. Avoid implausibly obscure distractors.

Tutorial T0: unrelated fictional employee's paid salary slip, 8,000. Guide highlights employer/service relationship, places compensation type, inclusion treatment and payslip reference, then lets the player repeat. No Rosa summary, salary tax calculation, deduction or employer withholding lesson.

## Office answer key

- May supported schedule: 12,000 + 5,000 + 3,000 = 20,000. The 20,000 loan remains in non-income memorandum only.
- Annual supported income schedule: prior reviewed aggregate 120,000 + May 20,000 = 140,000. Do not call either total tax due or net taxable income.
- F1 actual omission: link D3 to S1; add E2's 5,000 once; mark `Corrected — blanket-washing payment included`.
- F2 apparent mismatch: D1 laundry payments 12,000 versus D2 laundry bank deposit 9,000; link D1/D2/D6; reason `3,000 collected in cash; both channels are in the monthly sheet`; mark `Explained — no additional income adjustment`.
- D6 is not another 3,000 to add. D2 bank total 29,000 is not the income total. A flag saying bank amount should be 12,000 is unsupported.
- Final Ready reason: `Income entries accounted for; omission corrected; channel difference supported`.
- Correct interim Not Ready: specific real unresolved omission/treatment/required-field problem in CURRENT packet. Not Ready because `bank amount differs` after supported explanation is a false hold.

## Short dialogue beats

Receptionist: `Rosa's folder is in the market-client drawer. Please check the prepared summary before today's review cutoff.`
Auditor: `I review completed work until {deadline}. If a record needs checking, state what is unresolved.`
Arthur, first review: `A filled page can still need checking. Find the record behind the entry.`
Rosa, arrival: `The Desk helped me put that together. The originals are here by the stall.`
Rosa, pressure: `Can this still be finished today? I had to close the stall when I came yesterday.`
Player options: `Which job was this payment for?` / `May I review the records behind the summary?` Both are required short information beats, order flexible, no repeated talk rewards.
Rosa: `A guesthouse paid me to wash a batch of blankets. I kept that receipt separate from the daily clothes list.` / `Some customers pay in cash; others send it to the account.` These are clues, not classification answers.
Guide: `Mark the source first. Then use the guide to decide its treatment. A deposit alone does not tell the whole story.`
Quest completion: `Your annotations are ready to take back. Now check what the prepared summary actually accounts for.`
Justified hold: Auditor `You have a reason to hold this. Use the collected records to finish the unresolved item.`
Premature approval: Auditor `This payment still has no supported place in the summary. Check the record of the payment for a separate blanket-washing job.` Rosa later: `I thought it was finished. Please tell me what still needs fixing.`
False hold: Auditor `The amounts cover different payment channels. Check the cash record before requesting more paperwork.` Rosa: `I brought those receipts already. Can we use them to finish this?`
Success: Rosa `Thank you for explaining it. Now I know which records belong together.`
Story confirmation: Rosa `Yes, the Taxpayer Assistance Desk prepared that first summary.`

## Book additions and sources

Provide exactly two level-specific leaves alongside preserved shared book content: Reading Records, with brief period/scope/duplicate/purpose clues; and Review Notes, with three selectable reasons and one short explanation per source. Retain the loan educational-simplification label. No level number appears in a printed heading. Rules explain concepts, never Rosa-specific values, finding IDs or correct stamp.

Verified research entry points on 2026-10-17:

- [BIR RMC 60-2025 Annex A](https://bir-cdn.bir.gov.ph/BIR/pdf/RMC%20NO.%2060-2025%20Annex%20A.pdf): current amendment text includes Section 32 income categories. Verify exact passages and any subsequent amendments before shipping curriculum copy; this pack does not infer tax rates from it.
- [BIR income-tax return forms](https://www.bir.gov.ph/bir-forms?tab=Income+Tax+Return): official context for filing categories; does not establish that this simplified schedule is an official complete return.
- [RA 8424, Supreme Court E-Library](https://elibrary.judiciary.gov.ph/thebookshelf/showdocs/2/3896): historical Section 32 foundation only; do not copy old rates/thresholds as current.

The design's loan-principal rule is an educational accounting assumption for a genuine obligation to repay. This research pass did not locate a directly applicable current Philippine primary-source passage specifically establishing that rule. Ticket 01 must document the course-authority/primary-source basis before finalizing that graded rule; do not cite US benefit rules or an unrelated Philippine securities-loan regulation as support. If verification is unavailable, keep implementation fixture clearly marked educational draft and report that limitation; no fabricated legal citation. This is a content verification task, not a reason to omit the physical loan record or stop unrelated implementation.

**T01 source status (2026-10-17): educational draft, unverified.** A further search found no Philippine BIR issuance, Supreme Court or CTA decision that directly states that genuine loan principal is not income. The only directly on-point text found was a US case (*James v. United States*), which this pack says must not be cited. The other BIR loan guidance found concerns interest deduction and documentary stamp tax, not income inclusion. So the case data marks `review.contentStatus: "educational-draft"`, and the Level 2 book leaf calls the loan rule "an educational simplification" with no citation. Before T06 grades E4's treatment as final, someone with course authority (the instructor or curriculum owner) should confirm the rule or supply the source. Until then, keep the draft label.

No interest/dividend/final-tax percentages, deadlines from real tax law, or borrowing-as-statutory-exclusion claims are authorized by this pack. Game review cutoff is fictional office scheduling.


## Compact Findings copy and book sources — 2026-10-18

The book's transferable reason labels are **Unlisted payment**, **Different totals**, and **Not earnings**. Stable IDs remain `unaccounted-payment`, `amounts-differ`, and `maybe-not-income`, all under semantic key `FindingReason`. These are generic concepts with short explanations, not client-specific instructions. No choices remain on the slip.

The slip fields are **Entry to check**, **Clue**, and **Reason**. Labels always sit above values, including filled and read-only fields. Its introduction is `A clue may explain something on another page.` Filling or replacing a field triggers automatic validation. A supported initial link reveals: `The blanket-washing payment is missing from May's total. Check the original receipt before approving it.` Partial drafts and unsupported links use the short statuses `Add three details` and `No link yet`; a supported observation reads `Needs checking`, not a final approval.

This compact automatic design supersedes the earlier manual comparison copy. The runtime laundry wording and paid-job migration are implemented. All evidence IDs, references and monetary fixtures stay intact.


### Concrete total/payment clue — implemented 2026-10-18

The opening link is **Entry to check: May total · ₱15,000 → Clue: Payment for: Blanket washing → Reason: Unlisted payment**. The book explanation is `A monthly sum should include every earned payment for that month, including separate jobs.` Arthur introduces Review Notes, then hints that some jobs are recorded separately. Neither book nor hint names Rosa's answer. N1's legacy semantic key `ReferralNote` and D3's CM-0526 reference remain stable; the printed story no longer concerns referrals. E1 and E2 both classify as business/service income. They remain separate entries backed by separate records, with no duplicated job in D1.


## Simplified case content — 2026-10-20 (supersedes the D1-D6 and F2 tables above)

| Paper | Printed rows | Role |
|---|---|---|
| S1 May summary | Clothes washing 12,000 · Washer rental 3,000 · May total 15,000 · Memo, not income: Loan LN-0510 20,000 · Prepared with Assistance Desk | Prepared work under review |
| N1 Payment note | Payment for: Blanket washing · Paid in May · CM-0526 · Ask Aling Rosa · Original record at her shop | Initial clue (unchanged) |
| D1 Collection sheet | Work: Clothes washing only · Month: May 1 to 31 · Paid by customers 12,000 · Signed R. Santos | Backs listed line 1 |
| D4 Rental receipt | RA-0520, May 20 · Spare washing machine · Rented May 18 to 20 · Paid in cash 3,000 | Backs listed line 2 |
| D3 Blanket wash receipt | CM-0526, May 26 · Market Guesthouse · Blanket washing · Paid in cash 5,000 | The missing payment |
| D5 Loan agreement | Bayan Credit Coop · Loan LN-0510 20,000 · Released to bank May 10 · Must repay monthly, 10 months | Explains the deposit: borrowed |

Annotated papers (Mark the Money!, same entries at the office): E1 Paid in May 12,000 -> Business/service, Regular, D1. E2 Amount 5,000 -> Business/service, Regular, D3. E3 Amount 3,000 -> Rental, Regular, D4. E4 Credit 20,000 (Loan release LN-0510) -> Borrowed funds, Not income, D5. Sixteen decisions; one record per clip.

Office answer key: Listed: Clothes washing = E1 12,000; Listed: Washer rental = E3 3,000; Missing entry = E2 5,000; May 20,000; year 120,000 + 20,000 = 140,000; conclusion `Corrected: included`. Placing E4 counts the loan (graded), and leaving the missing line empty is the omission. Rosa now says the coop money must be paid back, a clue rather than a classification.

Book leaves (no client values): Cheat Sheet: The Cards (each TYPE and TREATMENT card), Cheat Sheet: Is It Income? (earned vs borrowed, bank credit alone is not proof, evidence = the one record), Cheat Sheet: Checking a Summary (tick listed lines, find the missing earned payment, leave out non-income, count once, write the conclusion), Review Notes (Unlisted payment, Different totals, Not earnings), Review Conclusions. The loan rule keeps its educational-simplification label.


## Book leaves — 2026-10-20 (replaces the cheat-sheet list above)

Section 4 – Income Types: Compensation Income, Business / Service Income, Rental Income, Passive Income (Interest), Borrowed Funds. Section 5 – Income Treatment: Regular Income Schedule, Separate Rule, Statutory Exclusion, Not Income (labelled an educational simplification). Section 6 – Checking a Summary: Listed Line, Missing Entry, Bank Deposit, Evidence. Each term is read-only, with one explanation underneath. Sections 7 and 8 keep the selectable reasons and conclusions. No client values appear.
