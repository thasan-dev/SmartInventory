# RBAC mechanics: Policies, Roles, token-borne authorization, and a three-gate check

This resolves the mechanics left open by [ADR-0005](./0005-shop-scoped-rbac-with-custom-roles.md)
(which fixed only the *shape*: Owner super-admin, shop-defined Roles over a platform-fixed
catalog).

**Policy (renamed from Permission).** The atomic grantable capability, named
`RESOURCE.ACTION`: `PRODUCT.READ/WRITE`, `STOCK.READ/WRITE`, `ORDER.READ/WRITE`,
`REPORT.READ`, `SETTING.WRITE`, `USER.WRITE`. Actions are **READ and WRITE only** (no full
CRUD) and **WRITE implies READ**. Rationale for stopping at read/write: it delivers the real
roles (read-only clerk, accountant, stock clerk) without a 24-permission catalog an owner
can't reason about; "edit but not delete" is a distinction a small shop rarely needs.
Read/Write splits exist only where both are meaningful — REPORT is read-only; SETTING and
USER are write-only (reading them is implicit in using the back office).

**Role → User, one-to-one in the MVP.** A Role is a shop-defined bundle of Policies; a staff
Shop User holds **exactly one Role**. **Groups** (assigning a Role to a set of users) and
**multiple Roles per user** are deferred — they earn their keep only at scale and add a layer
a handful-of-staff shop won't use; both can be added later without reshaping Policy/Role. The
**Owner is not a Role** — an implicit super-admin whose checks short-circuit to allow.

**Authorization is token-borne, enforced at the API.** The user's Role/Policies are injected
into the Entra token as a claim; the back-office API authorizes from it. The domain and UI do
not enforce — the API authorization layer does.

**Three enforcement gates, outermost first:**
1. **Shop-active gate** — the target Shop is not suspended. A suspended Shop (subscription
   lapse, driven by SI-30) **blocks all access including the Owner**.
2. **Tenancy gate** — the user is an **active member** of the target `ShopId`; data is always
   scoped to that ShopId. A Policy never crosses shops (isolated storefronts, ADR-0004).
3. **Policy gate** — the user's Role includes the Policy the endpoint requires (Owner
   short-circuits).

**Staleness split (consequence of token-borne roles).** Because Policies ride in the token,
a **role/policy edit takes effect on the next token refresh** (accepted latency; keep token
lifetime modest). But **revocation is immediate**: the tenancy gate checks *active membership
live* on every request, so a deactivated user — or a suspended shop — is blocked at once
regardless of what the token still asserts. Immediate policy-push is a post-MVP nicety.

**Edge cases.** Owner is unrevocable/undemotable inside the shop (last-owner lockout is
impossible by construction); owner transfer is out of MVP scope. Deleting a Role that is
still assigned is **blocked** — reassign its users first (no orphaned users with no Policies).

Considered and rejected: **full CRUD granularity** — rejected as too many knobs for the MVP;
**keeping "Permission" / `manage_*` naming** — renamed to Policy / `RESOURCE.ACTION` for a
consistent, sentence-like convention; **Groups and multi-role in the MVP** — deferred as
scale features; **immediate policy propagation into live tokens** — rejected in favour of
refresh-based staleness plus a live active-membership check for revocation; **enforcing in the
domain layer** — rejected, authorization is an API concern. Platform Super-Admin and the
subscription/billing model that drives suspension are **out of scope here — spawned as SI-30.**

**Amendment (SI-30):** the "suspended" state / "shop-active gate" above is **renamed to
`Deactivated` / the Shop-status gate**. Full access is granted only when the Shop's status is
`Active` **and** its Subscription is currently valid; `Deactivated`/`Pending`/`Rejected`/
`On Hold` restrict or block access. See [ADR-0013](./0013-platform-admin-and-subscription.md).

## Amendment (microservice decomposition — distributed authorization)

Under the physical split ([ADR-0014](./0014-physical-microservice-decomposition.md)) the three gates
survive, but *where* each is enforced changes. Companion to the tenancy mechanism in
[ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md).

**Token enrichment at mint (the single join point).** Identity moves to **AWS Cognito**; the local
**Accounts** DB remains the source of truth for authorization (ADR-0013). A **Cognito Pre-Token-Generation
Lambda** calls Accounts' internal authorization endpoint **through the existing AWS API Gateway** (a
private, IAM/SigV4-authorized route with a VPC-Link integration to Accounts on EKS/ECS) and stamps the
returned claims into the JWT: **`shop_id`**, the user's **`role`/`policies`**, and — see below —
**`shop_status`** + **`subscription_valid`**. This is the *only* place the user → role → policy join
happens; no service holds an RBAC replica. The call is **synchronous but only at login/refresh**, off
every service's per-request critical path — if Accounts is down, existing tokens keep working and only new
mints are blocked.

**Gate 1 — Shop-status gate: token-borne.** `shop_status` + `subscription_valid` are computed by Accounts
at mint and carried in the token; each service reads them from the claim. The customer/storefront token is
checked for shop-status **at mint** by its own Lambda authorizer, so a deactivated shop stops issuing new
storefront sessions. Accepted staleness: a status/subscription change takes effect on **next token refresh
(~30 min)** — including up to a ~30-min window where a just-deactivated shop can still operate. The one
**immediate hard effect — freezing in-flight orders — is event-driven, not gate-driven**: Accounts emits
`ShopDeactivated` and **Sales** consumes it to freeze the orders (choreography, ADR-0017).

**Write-path guard against the staleness window.** Because the gate is token-borne, a customer/staff member
already holding a valid token could otherwise **place a new order at a just-deactivated shop** until the
token expires — unbounded for a long-lived storefront session, well past the ~30 min tolerance. So the same
`ShopDeactivated`/`ShopActivated` events **also flip a single `accepting_orders` boolean in Sales**, and
**order placement checks it**. This closes the write hole in seconds. That one boolean is the **sole
shop-status replica in the system** — not the RBAC/status graph (which stays token-borne); reads (browsing)
remain token-borne and may lag harmlessly.

**Gate 2 — Tenancy gate: enforced by RLS.** "Data is scoped to the target `ShopId`" is now the database's
job — the `shop_id` claim sets `app.shop_id` and PostgreSQL Row-Level Security scopes every read/write
([ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md)), replacing an application-layer `WHERE shop_id`.

**Gate 3 — Policy gate: token-borne, per service.** Unchanged in spirit: each service authorizes from the
`policies` claim; the Owner short-circuits.

**Staleness note (revised).** With RBAC token-borne and no user replica, a **user-level** change (revoke a
staff member, shrink a Role) also takes effect on **next refresh (~30 min)** — the MVP accepts this rather
than carry a per-service user store. A thin `user-revoked` replica for *immediate* lockout is a documented
post-MVP option, deferred.

**Superseded by this amendment:** "Policies are injected into the **Entra** token" — the surface is now
**Cognito** with a Pre-Token-Gen Lambda (the Entra→Cognito identity-provider change is tracked as a
separate infra reconciliation, and touches ADR-0006/0013/0018). The immediate-revocation-via-live-check of
the original staleness split is relaxed to refresh-based for the MVP (above).

## Amendment (Accounts internals — Groups replace Roles; multi-assignment)

Designing the **Accounts** service reopened the deferrals above. Both are now **reversed**: the MVP ships
**Groups**, and a user may hold **more than one**.

- **Policy — unchanged.** Platform-fixed static-string catalog (`RESOURCE.ACTION`, `WRITE ⊇ READ`).
- **Group — the shop-defined bundle of Policies, renaming and generalizing "Role."** A Group is a named
  set of Policies the Owner composes; the **Group → Policy** mapping and **User → Group** mapping are
  **data in the Accounts DB** (Policies stay static strings in code). Shop-scoped → carries `shop_id` →
  RLS ([ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md)).
- **A user is assigned one *or more* Groups; effective policies = the *union*** of those groups' policies.
  This resolves *both* previously-deferred features (Groups **and** multiple-assignments-per-user) in one
  model — the "exactly one Role per user" rule above is **retired**.
- **Owner — unchanged.** Implicit super-admin, not a Group, holds every Policy, checks short-circuit.
- **Token.** The Pre-Token-Gen Lambda stamps the **union** of the user's groups' policies into the
  `policies` claim (still bounded by the small fixed catalog, so JWT size is a non-issue).

**Consequence — deletion rule relaxed.** ADR-0012 *blocked* deleting an assigned Role (one-role-per-user
made deletion orphan a user with no Policies). Under multi-assignment that risk is gone: **deleting a Group
simply removes it from its members**, who fall back to their remaining Groups; a user in **zero** Groups
has **zero** policies — a safe, recoverable state (identity is separate; the Owner re-assigns). No
"reassign first" gate.

**Terminology:** **"Role" retires**, folded into **Group** (glossary + ADR-0005 updated).
