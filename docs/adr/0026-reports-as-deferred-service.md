# Reports is its own service — deferred; MVP per-order profit stays a Sales read concern

Profit/margin reporting composes data whose ownership is **split across services**: Catalog owns master
**price** and **Base Cost** (via Lots, [ADR-0015](./0015-catalog-owns-price-and-cost-sales-owns-quantity.md)),
while Sales owns **quantities, discounts, Order Costs, and lifecycle**. Because no single service holds all
of it, **Reports is a distinct service** — but it is **deferred**, not built in the MVP.

## MVP: per-order profit stays in Sales

Per-order profit (ADR-0011) is still shown on the Order in the MVP, computed **inside Sales from its
snapshots** — the line **price snapshot** at placement and the **Base Cost snapshot** onto the order line at
dispatch (ADR-0015), plus Order Costs and discounts. For a **single order**, Sales has everything; no
cross-service call is needed. Guarded by `REPORT.READ`.

## Why a separate service (not permanently folded into Sales)

**Aggregate / date-range** profit and margin reporting needs more than one order's snapshots — it needs
**current** catalogue price and Base Cost, cross-order aggregation, and composition of Catalog + Sales data.
That composition belongs in a **dedicated read-model service fed by events** (ADR-0017), not bolted onto
Sales, which does not own the cost/price master. Naming it now (even while deferring it) keeps per-order
profit correctly scoped to Sales and prevents a future date-range feature from being wedged into the wrong
service.

## Why deferred

**Date-range / aggregate profit reporting is deferred post-MVP** ([ADR-0011](./0011-per-order-contribution-margin-costing.md)),
and per-order profit is already covered by Sales' snapshots — so there is **no MVP driver** to stand up the
service now.

## When built (sketch, not MVP)

A Reports service maintains its **own composed read model** from Catalog events (price, Base Cost) and Sales
events (orders, discounts, order costs, lifecycle/revenue-at-Settled), serving date-range profit/margin
behind `REPORT.READ`. Shop-scoped → RLS (ADR-0019).

## Considered and rejected

- **Reports permanently a read concern inside Sales** — rejected: Sales lacks the cost/price master
  (ADR-0015); fine for one order via snapshots, wrong home for cross-service aggregate reporting.
- **Stand up the Reports service in the MVP** — rejected: no driver; date-range reporting deferred
  (ADR-0011).

## Relates to

Per-order profit is [ADR-0011](./0011-per-order-contribution-margin-costing.md); the cost/price ownership
split is [ADR-0015](./0015-catalog-owns-price-and-cost-sales-owns-quantity.md); the future read model would
be fed over [ADR-0017](./0017-inter-service-integration-patterns.md).
</content>
