# Per-order profit is a contribution margin: weighted-average COGS + flexible order costs, overheads excluded

The MVP computes **per-order profit** as a **contribution margin** — revenue minus the costs
*directly attributable* to that order. Date-range/aggregate reporting is **deferred** to
post-MVP; only per-order profit is in scope.

**Revenue** (realised at `Settled`, per SI-24) is the COD total collected:
`revenue = Σ(line effective price × qty) − manual discount + delivery fee` (the delivery fee
is revenue; discounts per [ADR-0010](./0010-discount-composition-and-precedence.md)).

**Directly-attributable costs — two kinds:**

- **Base Cost (COGS)** — a **weighted-average** acquisition cost per Stock Unit, maintained
  from a **`unitCost` recorded on each inbound Stock Movement** (which may itself sum multiple
  components: purchase price + inbound freight + duty …). This **amends SI-22**, whose Stock
  Movement previously carried only a reason. A dispatched line's cost of goods = Base Cost ×
  qty. **Weighted-average, not FIFO**: SI-22 derives on-hand as a sum of movements, not lots,
  so a running average fits the model; FIFO would require lot-layer tracking the aggregate
  doesn't keep.
- **Order Cost** — a **flexible list** of `{costType, amount}` rows the owner adds at
  `Processing`. **Cost Types are shop-defined** (Delivery, Packaging, Marketing, …) and may
  carry an optional **default amount** (so a "fixed" delivery cost pre-fills yet stays
  editable). There are **no hardcoded delivery/packaging cost fields** — everything beyond
  COGS is a generic, shop-categorised row.

**`profit = revenue − COGS-of-goods − Σ(Order Cost amounts)`.** Final at `Settled` (provisional
before). For `Failed`/`Returned`, the loss is Σ(Order Costs incurred) plus COGS **only if the
units were written off** — if the goods are manually restocked (SI-24/SI-25) their cost returns
to inventory and is not a loss; the figure therefore **finalises when the stock disposition is
recorded**. `Cancelled` is ≈ breakeven.

**Overheads are deliberately excluded.** Salaries, electricity, and rent are
organization-wide and cannot be fairly distributed per order, so they are out of this
calculation — the reported number is a **contribution margin, not net profit**. A future
reader wondering "why doesn't profit include payroll?" should read this as intentional.

The customer-facing **delivery fee** (revenue, SI-25) and a **Delivery `Order Cost`** (the
shop's delivery expense) are tracked **separately and never netted**, so a report can show both.

Considered and rejected: **FIFO / specific-lot costing** — rejected as incompatible with
SI-22's movement-sum stock model and too heavy for a manual MVP; **a single manual per-order
cost figure** — rejected in favour of derived COGS + itemised order costs, so margins reflect
real acquisition cost; **hardcoded delivery/packaging cost fields** — rejected for a flexible
shop-defined Cost Type list; **including overhead (full net profit)** — rejected because
fair per-order distribution isn't possible in the MVP; **date-range/aggregate reporting** —
deferred, not attempted now.

## Amendment (ADR-0015): acquisition cost moves to the Lot in Catalog

Under the physical decomposition ([ADR-0014](./0014-physical-microservice-decomposition.md)), the
acquisition `unitCost` **no longer rides the inbound Stock Movement**. Instead it is entered on a
**Lot** (a Goods Receipt) registered in the **Catalog** service, and the **weighted-average Base
Cost is maintained in Catalog**, not in the Stock aggregate. Sales Stock Movements become
**quantity-only**. The costing *model* is unchanged — still weighted-average (still not FIFO), Base
Cost × qty for COGS, contribution margin, overheads excluded — only the *location* changes: cost is
now Catalog-owned and read by Reports at profit time (eventual consistency is acceptable because
profit is non-atomic; final at `Settled`). To keep a shipped order's COGS stable, Base Cost is
**snapshotted onto the order line at dispatch**. See [ADR-0015](./0015-catalog-owns-price-and-cost-sales-owns-quantity.md).
