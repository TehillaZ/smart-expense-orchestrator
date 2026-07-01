# Northwind Components Ltd. — Expense & Invoice Policy (v1)

> This is the policy your **ApprovalFlow** system enforces. It is written to be read by a human **and**
> consumed by your agent. A baseline may load this policy directly; the Nice-to-Have RAG work is to
> retrieve only the *relevant* rule(s) instead of putting the whole policy in the prompt.
> Each rule has a stable **`rule_id`** — emit these in the agent's `policy_violations[].rule_id`.
> You may extend this policy, and you may **tune the thresholds** in §6 — but if you change them you must say so
> and justify it in `docs/PRODUCT-DILEMMA.md`, and the numbers there must match what your router actually enforces.

Base currency: **USD**. All amounts are converted to USD at the **submission-date** rate before rules are applied.

---

## 1. Meals & Entertainment

| rule_id | Rule |
|---|---|
| `MEAL-01` | Personal/team meals are reimbursable up to **$75 per attendee**. Submissions must include an **attendee count**; missing it is *missing info*. |
| `MEAL-02` | **Client entertainment** over **$500** requires a **business justification** and a **client name**. Missing either → escalate. |
| `MEAL-03` | Alcohol-only receipts are not reimbursable. |

## 2. Travel

| rule_id | Rule |
|---|---|
| `TRAVEL-01` | Economy flights, standard hotels, and standard/economy ground transport (taxi, rideshare, transit) are policy-eligible. |
| `TRAVEL-02` | Any single travel expense over **$1,500** requires **manager approval** (never autonomous). |
| `TRAVEL-03` | First/business-class travel always requires approval. |

## 3. Software / SaaS

| rule_id | Rule |
|---|---|
| `SAAS-01` | Subscriptions are policy-eligible up to **$200 / month**. |

## 4. Hardware

| rule_id | Rule |
|---|---|
| `HW-01` | Hardware purchases are policy-eligible up to **$1,000**. |
| `HW-02` | Hardware **over $1,000** is a **Capital** expense → **always human-approved**. |

## 5. Global rules

| rule_id | Rule |
|---|---|
| `GLOBAL-RECEIPT` | A **receipt is required for any expense over $25**. Missing → *missing info* → escalate. |
| `GLOBAL-VENDOR` | A **new / unknown vendor** is **always reviewed** by a human, regardless of amount/category. |
| `GLOBAL-FX` | Foreign-currency items are converted to USD. A converted amount above the autonomy ceiling (or any FX item over **$1,000**) is a **hard stop** → human. |
| `GLOBAL-DUP` | A **duplicate** (same `vendor` + `invoiceNumber` + `total`) is rejected as a duplicate — **no second payment**. |
| `GLOBAL-MATH` | The line items + tax **must reconcile** to `total`. A mismatch is a hard stop → escalate (never auto-approve). |
| `GLOBAL-FRAUD` | Fraud-pattern signals (round-number to a brand-new vendor, no line-item detail, off-hours, padded quantities) are a **hard stop** → human review with the signal recorded. |

---

## 6. Autonomy thresholds (the dilemma — **default posture; tune & justify**)

These govern what the **agent may approve with NO human involvement**. They are deliberately conservative defaults;
resolving the central dilemma means *choosing your own* and defending them.

| key | default | meaning |
|---|---|---|
| `AUTONOMY-CEILING` | **$400** | The agent may auto-approve only when the USD amount is **≤ $400**. Above this → human, *even at confidence 1.0*. |
| `AUTONOMY-CONFIDENCE` | **0.90** | The agent may auto-approve only when its `confidence` is **≥ 0.90**. Below → human. |
| `AUTONOMY-HARDSTOPS` | — | Regardless of amount/confidence, these **always** force a human: new/unknown vendor (`GLOBAL-VENDOR`), FX hard stop (`GLOBAL-FX`), math mismatch (`GLOBAL-MATH`), any fraud signal (`GLOBAL-FRAUD`), missing required receipt (`GLOBAL-RECEIPT`), missing required info (`MEAL-01`/`MEAL-02`). |

**An item is `auto_approve` only if ALL hold:** amount ≤ `AUTONOMY-CEILING` · `confidence` ≥ `AUTONOMY-CONFIDENCE` ·
policy-compliant for its category · **no** hard stop. Otherwise it is `human_review` (or `reject` for a
high-severity violation, `duplicate` for a re-submission). **The deterministic router enforces this — the agent only recommends.**

---

## 7. Department budgets (for the no-overspend / concurrency scenario)

Budgets are finite and **must not be overspent** by two concurrent approvals. See `sample-invoices.json → budgets`.
Reserving budget is a saga step with a compensating *release* on failure.
