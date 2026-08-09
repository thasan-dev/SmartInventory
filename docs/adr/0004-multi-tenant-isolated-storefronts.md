# Multi-tenant SaaS with isolated storefronts

SmartInventory is a **multi-tenant SaaS**: many independent Shops, each an **isolated
storefront**. A customer shops within one Shop at a time — there is no cross-shop
browsing, no unified catalogue, and no cart or order spanning shops. `ShopId` is the
tenancy partition.

**Ownership split:** catalogue *structure* is platform-global — Categories, their trait
schemas, and variation axes are defined once and shared by all shops (consistent with
[ADR-0001](./0001-platform-defined-categories-and-traits.md)). Everything *instantiated
or transacted* is shop-scoped: Products, Stock, Orders, Discounts, and profit/costing
reporting each carry a `ShopId` and are private to one Shop. Two shops both selling
monsteras hold two separate Product records.

**Users:** Customers are shop-scoped and keyed by phone *within* a Shop (the same phone
at two shops is two Customers). Shop back-office users (Owner + staff) are also
shop-scoped.

Why: "a user creates a shop" and the SaaS framing point to many tenants, and strict
isolation keeps `ShopId` a clean partition while sidestepping cross-tenant privacy
questions. Rejected: a **marketplace** (cross-shop browse + multi-seller cart/order
splitting) — significant machinery in orders, stock, and delivery with no MVP driver;
and a **single shop** — contradicted by the SaaS model. Marketplace-style discovery is a
possible post-MVP direction.

## Amendment (microservice decomposition — isolation *mechanism* fixed)

`ShopId` remains the tenancy partition, but the **enforcement mechanism** is now fixed:
**one shared schema, a `shop_id` column, and PostgreSQL Row-Level Security** — DB-enforced
and fail-closed — **not** a schema or database per shop, and **not** an application-layer
`WHERE shop_id` on every query. See [ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md)
for the mechanism, scope (global tables are exempt), tenant-context propagation, and why
schema-per-shop and partition-per-shop were rejected.
