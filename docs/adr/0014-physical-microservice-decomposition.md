# Physical microservice decomposition, with Order and Stock co-located in one Sales service

SmartInventory moves from a **single `Inventories` context** (one deployable, the current codebase)
to a set of **physically separate, independently deployed microservices**, communicating over the
message bus (MassTransit + RabbitMQ / Azure Service Bus, already in place). This supersedes the
implicit "one service" assumption behind the current backend and `CONTEXT-MAP.md`'s "single active
context so far."

This ADR records the **decomposition decision and its two load-bearing rules**; each service
boundary and cross-cutting concern (tenancy schema, Accounts/RBAC/Groups, Notification, Payments,
Warehouse) is resolved in its own follow-on ADR.

## The services (target decomposition)

- **Sales** — the transactional core: the **Order lifecycle** *and* **Stock quantity** (on-hand,
  quantity Movements, Holds, derived `available`). Owns placement and all stock/money side effects.
  Owns *quantity only* — not price, not cost (see [ADR-0015](./0015-catalog-owns-price-and-cost-sales-owns-quantity.md)).
- **Catalog** — Product identity, traits, variation axes, **price**, **cost** (Lots + weighted-average
  Base Cost), catalogue discounts, product search (Elasticsearch), caching. (ADR-0015)
- **Logistics** — delivery/fulfilment (two-altitude status model; see ADR-0009's amendment).
- **Accounts** — Shop Users, Groups, Subscriptions, RBAC. (Boundary/Groups decision: follow-on ADR.)
- **Notification** — email and SMS.
- **Payments** — deferred (COD-only MVP); a seam only, no gateway now.

There is **no separate Pricing/Cost service** — price and cost both live in Catalog (ADR-0015);
profit/margin is a **Reports** read composing Catalog (price, cost) and Sales (quantity, discounts,
order costs).

Naming note: the whole product was called "Inventories"; that name now describes the **system**, not
a service. The service that keeps the inventory invariants is **Sales** (it owns Stock quantity as
Order's consistency partner). "Sales" names the **order-and-stock-quantity transaction boundary** —
it does **not** own price or cost (Catalog, ADR-0015) or profit/revenue reporting (Reports).

## Rule 1 — Order and Stock quantity are co-located for guaranteed zero oversell

The business requires **zero oversell**: the availability check and the **Hold** creation must be a
single ACID transaction. A distributed Order↔Inventory saga would weaken ADR-0003's guarantee from
*"no oversell at placement"* to *"no oversell, eventually, with compensation"* — unacceptable here.

Therefore **Order and Stock quantity live in the same service (Sales), one database, one
transaction.** Placement stays atomic and all-or-nothing; **ADR-0003 and ADR-0007 are upheld, not
amended.** The deliberate cost: **Inventory is no longer independently scalable** — that scaling axis
is traded away to buy transactional correctness, the right priority for a COD shop with no current
scaling driver.

The invariant only needs the **deduction** side (Holds, placement) atomic. The **addition** side
(stock arriving) can be asynchronous — see Rule 2 and ADR-0015: Catalog registers a **Lot** and emits
a quantity-received event; Sales applies it idempotently. A delayed receipt only makes availability
*temporarily lower* (conservative), never higher — so async stock-in cannot cause oversell. A
*duplicated* receipt could, so the receipt handler must be **idempotent** (lot number as key, via the
MassTransit inbox).

## Rule 2 — Cross-service invariants that are NOT co-located become sagas / eventual reads

Any invariant spanning two services is **eventually consistent**, with idempotent handling and, where
a compensable multi-step action exists, an explicit compensation path plus a **reconciliation sweep**.
Accepted where a short lag is low-stakes:
- **Sales↔Logistics** — delivery outcomes (ADR-0009 amendment): Logistics owns fulfilment status;
  Sales stays authoritative for the Order lifecycle and every stock/money side effect, applied
  idempotently. Money/stock are never mutated across the async hop; only *status reflection* is
  eventual.
- **Catalog↔Sales** — **price** is snapshotted into Sales at placement (one-way read; ADR-0007);
  **cost** is only consumed at profit time, so it stays in Catalog and is read by Reports (ADR-0015).
  Neither needs strong consistency with the stock transaction.

It is **not** accepted for the stock-*commitment* invariant — which is why Rule 1 co-locates Order
and Stock quantity instead of sagafying it.

## Why

The team is committing to independent deploy/scale, fault isolation, and polyglot storage (e.g.
Elasticsearch confined to Catalog) — the standard microservice drivers. The product requirements
(SI-18 destination, the 89-story MVP) are unchanged; this is a re-shaping of *how* they are built,
not *what* is built. The bus and outbox/inbox are already in the stack, so the transport exists.

## Considered and rejected

- **Keep a single deployable / modular monolith** — safer and cheaper to reverse; rejected because
  the team explicitly chose physical services for independent deploy/scale and polyglot storage. (If
  a scaling driver never materialises, this ADR is the thing to revisit.)
- **Fully separate Order and Inventory services** — rejected by Rule 1: it turns the oversell guard
  into an eventually-consistent saga, defeating the zero-oversell requirement.
- **A standalone Pricing/Cost service** — rejected (ADR-0015): it fragments price out of Catalog and
  cost out of its own source data for a concept that is really a *read* view.
- **Delivery fully authoritative, Order a pure mirror** — rejected: it puts cash/stock side effects
  behind an async hop; Order must stay authoritative for money and stock (ADR-0009 amendment).
- **One database shared across services** — rejected as incompatible with independent deployability
  and the intended per-shop schema isolation (its own follow-on ADR).

## Amends / relates to

Supersedes the single-context assumption. **Amends ADR-0009** (Delivery → Logistics, two-altitude
status). **Upholds ADR-0003 and ADR-0007** (atomic placement, via co-location). The Catalog/Sales
price+cost split is resolved in **[ADR-0015](./0015-catalog-owns-price-and-cost-sales-owns-quantity.md)**
(which amends ADR-0011). Sets the frame for follow-on ADRs on tenancy schema-per-shop (ADR-0004),
Accounts/Groups (ADR-0012), Notification, Payments, and the Warehouse concept.
