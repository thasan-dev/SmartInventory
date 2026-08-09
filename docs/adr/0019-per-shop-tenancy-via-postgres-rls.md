# Per-shop tenancy: shared tables + `shop_id` + PostgreSQL Row-Level Security

How the physical microservices ([ADR-0014](./0014-physical-microservice-decomposition.md)) isolate one
Shop's data from another's. This **fixes the enforcement mechanism** left as a logical "partition" by
[ADR-0004](./0004-multi-tenant-isolated-storefronts.md): `shop_id` stays the tenancy partition, but
isolation is now **enforced by the database**, not by every query remembering a filter — and **not** by a
schema or database per shop.

## 1. Mechanism: shared tables, `shop_id` column, RLS — DB-enforced, fail-closed

Every shop-scoped table lives in **one shared schema** and carries a **`shop_id`** column. Isolation is a
**PostgreSQL Row-Level Security policy** on that table:

```sql
ALTER TABLE orders ENABLE ROW LEVEL SECURITY;
ALTER TABLE orders FORCE ROW LEVEL SECURITY;
CREATE POLICY shop_isolation ON orders
  USING (shop_id = current_setting('app.shop_id')::uuid);
```

The application connects as a **non-owner, non-superuser role** (RLS is bypassed by table owners /
superusers / `BYPASSRLS`), so the policy always applies. A request that has **not** set `app.shop_id` sees
**zero rows**, never all rows — a forgotten context **fails closed**, so a missing-filter bug can leak
nothing rather than everything. This replaces "every query must remember `WHERE shop_id = …`" with a
guarantee the database keeps for us.

## 2. Scope: a *policy* rule, not a *service* rule

An RLS policy is added to **every table that carries `shop_id`**, regardless of which service owns it.
**Platform-global tables carry no `shop_id` and get no policy**: the Catalog **taxonomy** (Categories,
trait schemas, variation axes — defined once, ADR-0001/0004), the **shop registry**, **Subscription
Tiers**, and **Platform Super-Admin** data. Consequence: the two *mixed* services are handled uniformly —
**Catalog** protects Products/Lots/Discounts but leaves the shared taxonomy open; **Accounts** protects
shop Users/Roles but leaves the registry/tiers open. There are **no per-shop schemas**, so nothing to
route to and no per-shop DDL.

## 3. Tenant context: `app.shop_id` via `SET LOCAL`, sourced from the token

Context is set **per transaction** with `SET LOCAL app.shop_id = …` (equivalently `set_config(…, true)`)
through an EF Core interceptor, so it **auto-resets at commit/rollback** and cannot leak across pooled
connections. The value's source, per token surface (ADR-0006/0013):

- **Merchant / back-office (Entra→Cognito token):** `shop_id` is a **claim**, minted from the user's
  membership (see §token enrichment in the [ADR-0012 amendment](./0012-rbac-mechanics-policies-roles-enforcement.md)).
- **Customer / storefront token:** `shop_id` is **bound into the token** for the storefront being visited.
- **Platform Super-Admin (no `shop_id`):** operates only on **global tables** (registry, subscription),
  so it never touches an RLS-protected table — **no `BYPASSRLS` role in the MVP**.

## 4. Tenant context in async events: on the envelope; plumbing is exempt

Every event carries **`shop_id` in a standard envelope header**. A consumer reads it and sets
`app.shop_id` **before** touching any RLS-protected table, exactly like the HTTP path. The
**outbox / inbox / message-relay tables are RLS-*exempt* infrastructure** — the relay that drains the
outbox runs as a background job with **no** tenant context, so it cannot be subject to a shop filter;
`shop_id` rides those messages as ordinary data (a header/column), not as an RLS boundary. This preserves
the outbox/inbox guarantees of [ADR-0017](./0017-inter-service-integration-patterns.md) under RLS.

## 5. Onboarding provisioning: two rows, no DDL

Because there is no schema per shop, onboarding a shop at SA approval (Pending → Active,
[ADR-0013](./0013-platform-admin-and-subscription.md)) is just **Accounts writing the shop-registry row +
the subscription row and setting status Active**. **No per-service provisioning, no `CREATE SCHEMA`, no
per-schema migration, no partial-failure saga.** A shop's data simply comes into existence as rows are
written under its `shop_id`; a new shop starts with an empty catalogue/stock and fills it via normal
writes.

## Implementation notes

- **`SET LOCAL` requires an explicit transaction.** It only lives for the current transaction, so the EF
  Core interceptor must ensure a transaction is open on **read** connections too (EF `AsNoTracking` reads
  do not always open one) — otherwise `app.shop_id` is unset and the query fails **closed** (zero rows),
  not open. Equivalently, use `set_config('app.shop_id', …, true)` (transaction-scoped) inside that
  transaction. This applies uniformly to HTTP requests and async consumers.
- **Two DB roles, never conflated.** Migrations/DDL run as the **table owner**, which *bypasses* RLS — that
  is how the policies themselves get created. The **application** connects as a **restricted, non-owner,
  non-superuser** role (`FORCE ROW LEVEL SECURITY` on owned tables covers the owner-writes-its-own-tables
  case). Keeping these separate is what makes the fail-closed guarantee real.
- **Write-path shop-status guard.** Order placement additionally checks Sales' `accepting_orders` flag
  (flipped by `ShopDeactivated`/`ShopActivated`) so a stale token cannot place orders at a deactivated shop
  — see the [ADR-0012 amendment](./0012-rbac-mechanics-policies-roles-enforcement.md).

## Considered and rejected

- **Schema-per-shop** (`shop_<id>.*`, `search_path` routing) — the earlier direction; rejected: strong
  isolation but multiplies every migration across N schemas, needs per-shop `CREATE SCHEMA` with
  partial-failure handling at onboarding, and bloats the catalog. Its isolation win is matched by RLS
  without the operational tax, for **small-shop** tenants (ADR-0012) that don't need per-tenant
  backup/restore or customization.
- **Partition-by-`shop_id`** (`PARTITION BY LIST (shop_id)`, a partition per shop) — rejected as the
  *isolation* mechanism: partitioning is a performance/data-management tool, **not** isolation (you still
  need the `shop_id` filter; forget it and you scan every partition). Its upside — smaller per-tenant
  indexes, fast `DETACH`/`DROP` — matters only for *huge* tables, which small-shop tenants don't have,
  while thousands of LIST partitions hurt planning time. **Held in reserve** as a per-table performance
  lever, layered on top of RLS if one table ever grows large — an orthogonal, later decision.
- **Database-per-shop** — strongest isolation, heaviest ops (N databases, N connection pools,
  N migration runs); unjustified for many small tenants.
- **A `BYPASSRLS` role for Platform Super-Admin** — rejected: the SA operates only on global tables in the
  MVP, so no bypass is needed. Introduce a scoped `BYPASSRLS` reporting role only if a genuine cross-shop
  read of shop-scoped data ever appears.

## Relates to

Fixes the enforcement mechanism of [ADR-0004](./0004-multi-tenant-isolated-storefronts.md) (amended).
The token-borne `shop_id` claim and distributed authorization are in the
[ADR-0012 amendment](./0012-rbac-mechanics-policies-roles-enforcement.md). Event-envelope `shop_id` and
the RLS-exempt outbox/inbox build on [ADR-0017](./0017-inter-service-integration-patterns.md). Onboarding
lifecycle is [ADR-0013](./0013-platform-admin-and-subscription.md); the client edge / token surfaces are
[ADR-0018](./0018-client-edge-aws-api-gateway-no-bff.md) / [ADR-0006](./0006-dual-identity-merchant-entra-customer-phone.md).
</content>
</invoke>
