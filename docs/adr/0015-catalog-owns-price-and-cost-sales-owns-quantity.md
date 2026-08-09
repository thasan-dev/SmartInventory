# Catalog owns price and cost (via Lots); Sales owns stock quantity and orders

Under the physical decomposition ([ADR-0014](./0014-physical-microservice-decomposition.md)), the
Catalog↔Sales boundary is drawn so that **price and cost both live in Catalog**, and **Sales owns only
stock quantity** (plus Holds and the order lifecycle). This resolves the "Catalog vs Pricing/Cost"
follow-on and **amends ADR-0011** (relocating where acquisition cost is recorded and maintained).

## Ownership

- **Catalog owns price** — a Stock Unit's price is authored master data (ADR-0002), low-frequency,
  edited beside its catalogue discounts (ADR-0010). It is **snapshotted into Sales at placement**
  (one-way read, ADR-0007).
- **Catalog owns cost** — via **Lots** (below). The weighted-average **Base Cost** per Stock Unit is
  maintained in Catalog.
- **Sales owns quantity** — on-hand, quantity Stock Movements, Holds, derived `available`, and the
  order lifecycle. Sales Stock Movements are **quantity-only**; they no longer carry `unitCost`.

## The Lot — a Goods Receipt registered in Catalog

A **Lot** is a supply receipt: a batch of goods received together from a supplier, identified by a
**lot number**, carrying header metadata (date, supplier, …) and **multiple line items**, each
`{Stock Unit, quantity received, cost}` (the cost may sum purchase price + freight + duty …).
Registering a Lot in Catalog:

1. **Feeds cost** — each line updates its Stock Unit's running **weighted-average Base Cost** in
   Catalog.
2. **Feeds quantity** — Catalog emits a per-Stock-Unit **quantity-received** event; **Sales** consumes
   it and increments on-hand.

"Lot" here is a **receipt document**, not a FIFO cost layer.

## Flow (one-way: Catalog → Sales)

```
Supply arrives → owner registers a LOT in Catalog (per-SKU qty + cost)
   → Catalog updates weighted-average Base Cost (per SKU)
   → Catalog emits "received +N for SKU"
        → SALES applies it idempotently (dedupe by lot number, via MassTransit inbox) → on-hand += N
   → SALES emits "stock updated" (for storefront/search display) — Catalog does NOT subscribe
```

Sales → Catalog carries nothing about quantity or consumption; Catalog never learns current on-hand.

## Why cost can live in Catalog even though it is derived from receipts

Cost is **only consumed at profit-calculation time**, which is **not atomic** (profit is provisional
before `Settled`, final at `Settled` — ADR-0011). Nothing at placement or dispatch needs cost to be
transactionally consistent with quantity in the same instant. So cost can be **eventually consistent**
and does not need to share the stock transaction — removing the only reason to co-locate it with the
quantity Movement. Placing cost-entry at Lot registration also puts it exactly where the owner enters
it (receiving supply) and lets the owner see price and cost together while pricing.

## Why weighted-average, not FIFO (forced by the one-way flow)

FIFO / specific-lot costing would require Catalog to know **how much was consumed** to draw down cost
layers — i.e. Catalog would have to subscribe to Sales' deduction events, breaking the one-way flow.
**Weighted-average needs only the receipts** (which Catalog already has), so it is the only cost model
compatible with "Catalog does not listen to Sales." This aligns with **ADR-0011**'s existing
weighted-average choice.

## COGS and profit

A dispatched line's cost of goods = **Base Cost × qty**, read from Catalog. For per-order profit
stability, Sales/Reports **snapshots Base Cost onto the order line at dispatch** (analogous to the
price snapshot at placement), so a later Lot cannot retroactively change a shipped order's COGS.
Profit is a **Reports** read composing Catalog (price, cost) + Sales (quantity, discounts, order
costs). There is no separate Pricing/Cost service.

## Idempotency (hard requirement)

The quantity-received handler in Sales **must be idempotent**, keyed by **lot number**, so a
replayed/duplicated receipt event cannot double-increment on-hand (the one failure mode that would
cause oversell). The existing MassTransit EF **inbox** (`InboxState`) provides this.

## Considered and rejected

- **Cost in Sales (with the quantity Movement)** — the provisional ADR-0014 position and the original
  ADR-0011 shape; rejected because cost is needed only at profit time (non-atomic), so co-location
  buys no correctness, and splitting cost-entry away from supply-receiving is less natural for the
  owner.
- **A standalone Pricing/Cost service** — rejected: fragments price out of Catalog and cost away from
  where it is entered, for what is really a read view.
- **Price in Sales** — rejected: splits the catalogue edit and separates price from the catalogue
  discounts that modify it (ADR-0010).
- **FIFO / specific-lot costing** — rejected: needs consumption feedback into Catalog, breaking the
  one-way flow; weighted-average is sufficient and already chosen (ADR-0011).

## Amendment: pricing is a lot-triggered step; price gates sellability

Price is **not** an anytime free-standing edit — its authoring is **triggered by Lot receipt** and it
**gates sellability**:

1. **Every Lot receipt makes its Stock Units *eligible for pricing*.** Registering a Lot (which already
   feeds Base Cost + emits the quantity-received event) additionally opens a **per-Stock-Unit pricing
   step** for the lines it contains — the owner's moment to (re)price when new supply (often at a new
   cost) arrives.
2. **Previous price auto-fills; the owner overrides.** If the Stock Unit already has a price, that
   value **pre-fills** the pricing step as the default; if not, it is blank. The owner may accept or
   **override** any line. Setting/confirming a price writes **Pricing History** (ADR-0020) in the same
   transaction — a confirmed reprice and an unchanged carry-forward are both recorded per that trail's
   rules.
3. **A Stock Unit is not sellable until it has a price.** "Only when price is set" is a **gate**: an
   unpriced Stock Unit does not appear as sellable (storefront/placement treat it as unavailable),
   independent of on-hand quantity. So a freshly-activated Stock Unit (structure only) becomes sellable
   only after its first lot-triggered price is set.

Consequences: pricing and cost-entry stay **together at receiving** (reinforcing the "see price and
cost together while pricing" rationale above); the price a customer pays is still the placement
snapshot (ADR-0007); and the price-changed event that feeds Sales' pricing replica (ADR-0017) fires
from this step. Considered and rejected: **price as an always-editable attribute set independent of
lots** (the pre-amendment shape) — rejected because it decouples repricing from the cost signal that
should prompt it and loses the "unpriced ⇒ not sellable" guard; **only the first lot forces a price,
later lots don't reopen it** — rejected in favour of every-lot repricing so a cost change always
surfaces a repricing moment (previous price auto-filled makes carrying the old price one click).

## Amends / relates to

**Amends ADR-0011**: acquisition `unitCost` moves off the Sales Stock Movement and onto the **Lot line
item** in Catalog; **Base Cost is maintained in Catalog**, not the Stock aggregate; Sales Movements
become quantity-only. **Relates to** ADR-0002 (price stays a catalogue attribute), ADR-0007 (price
snapshot at placement), ADR-0010 (discounts beside price in Catalog), and ADR-0014 (which this
refines). Introduces the **Lot** term to the glossary.
