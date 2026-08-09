# Domain-specific audit trails; Pricing History as the first instance

A driver appeared for **audit logs in some domains — starting with price**. This fixes *how* audit is
modeled across SmartInventory and adds the first trail. It is **not** a generic logging subsystem.

## Audit is a domain trail per aggregate, on a shared pattern — not a generic facility

An audit trail is recorded as an **explicit domain concept owned by the aggregate it audits**, following a
**shared pattern**, rather than a cross-cutting reflection logger. This is already the house stance: the
**Order Status History** glossary entry explicitly rejects "audit log / infra logging" framing ("this is
the domain trail"), and the **Stock Movement ledger** ([ADR-0016](./0016-stock-quantity-ledger-projection-and-concurrency.md))
is a *domain* ledger. A Pricing History joins that family as a third member.

**The shared pattern.** An audit trail is:
- **Append-only**, records shaped `{ actor, timestamp, before → after, reason? }`;
- **Written in the same transaction as the change it records**, so it can never drift from the state (like
  Status History and the movement ledger) — not a side-channel, not event-sourced
  ([ADR-0017](./0017-inter-service-integration-patterns.md) rejects event sourcing);
- **Local to the owning service** — not a shared table and not a central audit service (either would force
  changes to flow cross-service, against ADR-0017);
- **Shop-scoped where the audited data is**, so it carries `shop_id` and inherits RLS
  ([ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md)).

## First (and MVP-only) instance: Pricing History in Catalog

Catalog owns price and discounts ([ADR-0015](./0015-catalog-owns-price-and-cost-sales-owns-quantity.md)),
so the trail lives there. It records changes to **what the customer is charged** — **base price *and*
catalogue discount** (auditing one without the other leaves a gap):

- Record: `{ stockUnitId, changeType (price | catalogueDiscount), before → after, changedBy, changedAt,
  reason? }`. **`reason` is optional** — a routine reprice needn't carry one (unlike Cancelled/Failed,
  where it is mandatory).
- **Granularity mirrors how the value is set** ([ADR-0010](./0010-discount-composition-and-precedence.md)
  specificity): a Stock-Unit price/discount change logs **one SKU entry**; a Product-level catalogue
  discount logs **one product-level entry** — it is **not** fanned out to N SKU rows.

**Not the order's price.** This is Catalog's record of *master* price changes. The price the customer
actually paid is a separate, already-decided fact: the **snapshot on the Order Line Item at placement**
([ADR-0007](./0007-cart-is-client-side-order-is-first-aggregate.md)/ADR-0015). The two answer different
questions ("how did the catalogue price move over time" vs "what did this customer pay") and neither
replaces the other.

## Already covered — no new trail; and what is deferred

**No new trail needed** (a domain trail already exists): **Order → Status History**, **Stock → Movement
ledger**, and **Cost → the Lots themselves** (each Lot is an append-only receipt feeding Base Cost —
ADR-0015 — so cost is audited by construction).

**Deferred post-MVP (decided while designing Accounts).** The other strong candidates — **Group/permission
changes**, **Subscription/tier changes**, and **Shop-Status transitions** (a Status History for the Shop) —
are all **Accounts-owned**. The MVP ships **price only**; these are **out of the MVP**, remaining valid
post-MVP instances of the pattern above. The leading follow-up is **Shop-Status transition history** (a
Super-Admin deactivating a shop with no record of who/why is the sharpest gap) — but none ship now.

## Considered and rejected

- **A generic cross-cutting audit facility** (reflection-logging `{entity, field, old→new}` for annotated
  entities) — rejected: produces field-diff soup with no domain meaning (no "reason", wrong granularity)
  and fights the DDD layering by reaching into aggregates.
- **A central audit service / shared audit table** — rejected: would require every audited change to flow
  cross-service, against the local-ownership grain of ADR-0017.
- **Event sourcing to reconstruct history** — rejected (ADR-0017); state-based aggregates + an explicit
  append-only trail suffice.
- **Auditing base price only** (excluding catalogue discount) — rejected: the discount co-determines the
  charged price, so excluding it leaves an audit gap.

## Relates to

Sits beside [ADR-0016](./0016-stock-quantity-ledger-projection-and-concurrency.md) (movement ledger) and
the Order Status History as the audit-trail family. Owned by Catalog per
[ADR-0015](./0015-catalog-owns-price-and-cost-sales-owns-quantity.md); shop-scoped under
[ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md); granularity per
[ADR-0010](./0010-discount-composition-and-precedence.md); distinct from the order-line price snapshot of
[ADR-0007](./0007-cart-is-client-side-order-is-first-aggregate.md).
</content>
