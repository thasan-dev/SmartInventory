# Shop-scoped RBAC with custom roles over a fixed permission catalog

A Shop is not a single-user back office: the **Owner** can add staff users, revoke their
access, and manage what each can do through **roles**. The model:

- **Permission** — a grantable capability from a **platform-defined fixed catalog**
  (e.g. `manage_products`, `manage_stock`, `view_orders`, `manage_orders`,
  `view_reports`, `manage_users`). Shops cannot invent new permission types; permissions
  map to real system capabilities.
- **Role** — a **shop-defined** named bundle of permissions the Owner creates and
  assigns to users. A Shop may define as many roles as it wants.
- **Owner (Merchant)** — an implicit super-admin with every permission, unrevocable,
  exactly one per Shop.

All three (Shop Users, Roles) are shop-scoped. This deliberately expands beyond SI-18's
"shop owner vs customer" framing, which implied an owner-only back office; the shop owner
asked for delegated staff access with custom roles.

Why a fixed permission catalog rather than shop-defined permissions: permissions must
correspond to enforceable capabilities in code, so letting shops invent them would be
meaningless. Fixed catalog + custom roles gives shops flexible delegation while keeping
enforcement tractable. Rejected: **owner-only** back office (simplest, no MVP need per
the original spec — overruled by the owner's requirement) and **fully shop-defined
permissions** (incoherent — nothing to enforce).

Authentication and enforcement mechanics (how users sign in, how permissions are
checked) are deferred to SI-21. This ADR records only the tenancy/ownership shape.

## Amendment (SI-29): mechanics resolved, "Permission" renamed to "Policy"

The mechanics deferred here are resolved in
[ADR-0012](./0012-rbac-mechanics-policies-roles-enforcement.md). Notably, the atomic
capability this ADR calls a **Permission** is **renamed to `Policy`** and named
`RESOURCE.ACTION` (e.g. `PRODUCT.WRITE`), with READ/WRITE actions only (WRITE⊇READ). A staff
user holds exactly one Role in the MVP (Groups and multi-role deferred); enforcement is
token-borne with a three-gate check (shop-active → tenancy → policy); platform-driven Shop
**suspension** (subscription) is spawned as SI-30.

## Amendment (Accounts internals): "Role" → "Group", multi-assignment

Designing Accounts reversed the deferral above. **"Role" is renamed to `Group`** — a shop-defined bundle
of Policies — and a user may be assigned **one or more Groups**, with **effective policies = the union**.
This retires "exactly one Role per user." See the Groups amendment in
[ADR-0012](./0012-rbac-mechanics-policies-roles-enforcement.md).
