# Handoff — SmartInventory microservice decomposition (grilling in progress)

**Date:** 2026-07-25
**Repo:** `/Users/tanveer/Projects/SmartInventory` (backend context: `backend/`)
**Jira:** project **SI**, site `thasan-dev.atlassian.net`, cloudId `7f72fac5-193c-44fa-9083-51aef0a1c26b`
**Next focus (user's instruction):** start with **per-shop schema tenancy**, after noting all leftovers.

## What this session was

An ongoing `/grill-with-docs` (grilling + domain-modeling) session re-architecting SmartInventory
from a **single `Inventories` .NET context** into **physical microservices**. Product requirements
(the SI-18 → SI-32 MVP) are unchanged; this is an architecture re-shaping. Decisions are recorded as
ADRs in `backend/docs/adr/` and glossary updates in `backend/CONTEXT.md`. **Do not re-litigate what's
already in the ADRs** — read them first.

## Decisions already recorded (read these, don't re-derive)

New ADRs:
- `backend/docs/adr/0014-physical-microservice-decomposition.md` — physical split; **Sales = Order ⊕ Stock quantity** co-located for atomic zero-oversell; two consistency rules.
- `backend/docs/adr/0015-catalog-owns-price-and-cost-sales-owns-quantity.md` — Catalog owns price + cost via **Lots** (weighted-avg Base Cost); Sales owns quantity; one-way Catalog→Sales flow.
- `backend/docs/adr/0016-stock-quantity-ledger-projection-and-concurrency.md` — movement **ledger + `onHand`/`heldQty` projection**; Hold-at-placement = sole atomic contention point (conditional UPDATE); async movements partitioned by `StockUnitId`; bucketing deferred.
- `backend/docs/adr/0017-inter-service-integration-patterns.md` — **async-events-only**; Sales pricing replica; **outbox/inbox** everywhere; **lightweight CQRS**; **choreography-first**; **thin shared contracts** + ACL + additive versioning.
- `backend/docs/adr/0018-client-edge-aws-api-gateway-no-bff.md` — **AWS API Gateway** (routing + authorizers for 3 token surfaces); **no BFF** in MVP; SPA composes reads client-side; Lambda composer deferred.

Amended ADRs (amendment sections appended):
- `0009` — Delivery → **Logistics** service, two-altitude status (Logistics owns fulfilment ops; Sales authoritative for order state + money/stock side effects).
- `0011` — acquisition `unitCost` moved off Stock Movement onto the **Lot**; Base Cost maintained in Catalog.
- `0003` — hold→deduction converts at **Confirm** (not Dispatch); cancel boundary at confirm.
- `0008` — deduction side-effect moved Dispatched → **Confirmed**; Cancelled re-cut; Dispatched no longer touches stock.

Glossary (`backend/CONTEXT.md`): added **Lot**; updated Stock Movement, Hold, Available, Dispatched, Cancelled, Base Cost.

### Services as decided so far
Sales (Order+Stock quantity) · Catalog (identity/traits/variations/price/cost-Lots/discounts/Elasticsearch) · Logistics (delivery) · Accounts (users/Groups/Subscriptions/RBAC — *internals not yet designed*) · Notification (email+SMS — *not designed*) · Payments (deferred seam). No separate Pricing/Cost service. No BFF. **Reports** referenced but not yet decided (service vs read concern).

## ✅ DONE — Per-shop tenancy + distributed authorization (resolved 2026-07-25)

The user **reversed schema-per-shop** mid-grill (switched to **PostgreSQL**) and landed on
**shared tables + `shop_id` + Postgres RLS** (DB-enforced, fail-closed). Recorded as:
- **ADR-0019** (new) — tenancy via RLS: scope = policy on every `shop_id` table, global tables exempt;
  `app.shop_id` via `SET LOCAL` from token claim (HTTP) / event-envelope header (async); outbox/inbox
  RLS-exempt; onboarding = 2 rows, no DDL; schema-per-shop and partition-per-shop rejected.
- **ADR-0012 amendment** — distributed authz: token-borne policies **+ `shop_status`/`subscription_valid`**,
  enriched by a **Cognito Pre-Token-Gen Lambda** calling **Accounts via the API Gateway** (private/IAM,
  VPC-Link to EKS/ECS); shop-status gate token-borne (~30 min staleness accepted); `ShopDeactivated` event
  → Sales freezes in-flight orders **and flips a single `accepting_orders` boolean checked at placement**
  (the sole shop-status replica, closes the write-path staleness hole); no RBAC/status-graph replica;
  user-revoked replica deferred. ADR-0019 gained an **Implementation notes** section (`SET LOCAL` needs a
  txn incl. reads; owner-role migrations vs restricted app role).
- **ADR-0004 amendment** — isolation *mechanism* fixed to RLS (points at ADR-0019).
- **CONTEXT.md** — Shop tenancy line updated + new **Tenant Isolation (RLS)** glossary term.

## ✅ DONE — Audit trails / Pricing History (resolved 2026-07-26)

- **ADR-0020** (new) — audit is a **domain trail per aggregate on a shared pattern** (append-only,
  `{actor, ts, before→after, reason?}`, same-transaction, service-local, shop-scoped/RLS) — NOT a generic
  logger, central service, or event sourcing. First & MVP-only instance: **Pricing History in Catalog**
  (base price + catalogue discount; granularity mirrors ADR-0010 specificity; reason optional). Already
  covered by existing trails: Order Status History, Stock movement ledger, Cost-via-Lots. Distinct from the
  order-line price snapshot (ADR-0007).
- **CONTEXT.md** — new **Pricing History** term (Discounts section).
- **Deferred into #3 (Accounts):** audit trails for **role/policy changes, Subscription/tier changes,
  Shop-Status transitions** — design them as instances of the ADR-0020 pattern when Accounts is modeled.

**New leftover surfaced → added as #13 below: Entra → Cognito IdP drift.** ADR-0006/0013/0018 and several
CONTEXT.md identity lines still say "Entra External ID"; the tenancy work adopted **AWS Cognito** (Pre-Token-Gen
Lambda). Reconcile the IdP choice across those ADRs + glossary as its own pass (not done here to avoid
half-updating identity everywhere).

**Next focus:** resume the leftovers below — suggest **#2 (now largely folded into the ADR-0012 amendment —
verify nothing remains)**, then **#3 Accounts internals** (the natural next cluster, since the Lambda now
depends on an Accounts authz endpoint + user/role model).

## 🔵 IN PROGRESS — #3 Accounts internals (started 2026-07-26)

Decided so far:
- **Boundary:** **one Accounts service** owns both shop identity+RBAC (shop-scoped, RLS) *and* the
  platform-operator concerns (shop registry, Shop-Status lifecycle, Subscriptions/Tiers, Super-Admin —
  global tables). Not split for MVP.
- **RBAC = Groups (option a).** ADR-0012 + ADR-0005 amended; CONTEXT.md renamed **Role → Group**.
  Group = shop-defined bundle of static-string Policies; **User → many Groups; effective policies = union**;
  Group→Policy & User→Group mappings are DB data; Owner unchanged (implicit super-admin). Reverses BOTH
  ADR-0012 deferrals (Groups + multi-assignment). Delete-Group = remove from members (fall back to remaining
  groups); zero-groups = zero-policies (safe). Lambda stamps the policy **union** into the `policies` claim.

- **Audit trails:** **price-only for MVP (option A)** — all three Accounts trails (Group/subscription/
  shop-status changes) are **post-MVP** (ADR-0020 wording corrected). Leading post-MVP follow-up =
  Shop-Status transition history.

Confirm-only remainder of #3 (derived, not open decisions): `authz` endpoint returns `{shop_id,
effective-policy-union, shop_status, subscription_valid}`; Super-Admin + Subscription/Tier internals already
fully specified by ADR-0013; Accounts user record = Cognito `sub` ↔ `{shopId, groupIds, active}`. Minor
open follow-up: is the static Policy-string catalog a **shared thin contract** package (ADR-0017 grain) so
every service's Gate-3 check references the same constants — recommend yes, not yet recorded.

**#3 Accounts internals: effectively closed for MVP** (boundary + Groups decided; rest derived/ADR-0013).

## (superseded) START HERE — Per-shop schema tenancy

User confirmed earlier: **"separate schema per shop, created when shop is set up after admin approval."**
This **amends ADR-0004** (which fixed `ShopId` as a *partition column*, not schema-per-tenant) and
cross-cuts every service. Grill these open questions one at a time (recommend an answer for each):

1. **Scope** — does *every* service get per-shop schemas, or only the shop-scoped ones? (Platform Super-Admin / Accounts platform data is *not* shop-scoped — ADR-0013.)
2. **Provisioning** — which component creates a shop's schema(s) at onboarding (Shop Status Pending→Active, ADR-0013)? What happens on partial failure? Is it synchronous with approval or an async provisioning step? Migrations per schema.
3. **Tenant context in async events** — every event must carry `ShopId`; each consumer must route to the correct tenant schema. How is this enforced (envelope header? correlation)? Interaction with the outbox/inbox (ADR-0017) and the Sales pricing replica / stock projections.
4. **Connection routing** — per-request tenant → connection/schema selection per service (EF Core multi-tenancy). One DB many schemas vs DB-per-shop.
5. **Cross-cutting authorization interaction** — the shop-status/subscription gate (ADR-0012/0013) now lives in **Accounts** but must gate every service; resolve alongside (see leftover #2).

Then update ADR-0004 (amendment) or write a new tenancy ADR, and update `CONTEXT.md`/`CONTEXT-MAP.md`.

## All remaining leftovers (full list)

**Cross-cutting (highest impact):**
1. **Per-shop schema tenancy** — start here (above).
2. **Authorization across services** — ADR-0012's 3 gates (shop-status → tenancy → policy) were for a monolith. Distributed: gateway does authN (authorizers); who does authZ/policy? How do services learn shop Active+subscription-valid without a sync call (token claim vs cached replica)? Are Policies still token-borne, injected by whom?

**Service internals not yet designed:**
3. **Accounts** — users, **Groups** (reopens ADR-0012 "Groups deferred / one role per user"), Subscriptions, RBAC, Platform-Super-Admin placement. User note: "Policies = static strings, group-mapping in DB."
4. ✅ **DONE (2026-07-26)** — **ADR-0025**: **channel follows identity** — customers→SMS (3 lifecycle,
   SI-28 provider), merchants/staff/SA→email (staff invite, shop-status decisions, subscription warning;
   provider = **Amazon SES**). Notification = event-consuming service (ADR-0014/0017), owns templates.
   **OTP stays with the auth flow**, not Notification (sync/latency). Customers email = out of scope.
5. **Payments** — confirm stubbed seam only (COD-only MVP).

**Concepts to resolve:**
6. **Warehouse** — new ("initially 1"). Model per-warehouse stock now, or single implicit warehouse deferred? Interacts with Sales/Stock.
7. ✅ **DONE (2026-07-26)** — **ADR-0026**: Reports is its **own service but DEFERRED** (cost/price master
   is in Catalog, qty/discounts/costs in Sales — composition crosses the split). **MVP:** per-order profit
   stays a **Sales read concern** from placement/dispatch snapshots (ADR-0011/0015), `REPORT.READ`. Future
   Reports service = composed read model from Catalog+Sales events.

**Doc / spec reconciliation:**
8. 🟡 **PARTIAL (2026-07-26)** — CONTEXT-MAP.md got a **"Planned decomposition" forward note** (lists the 5
   services). Full split of `backend/CONTEXT.md` into per-service glossaries **deferred** as a dedicated
   pass — the single glossary stays authoritative until the code actually separates (splitting docs ahead of
   code would describe services that don't exist yet).
9. ✅ **RESOLVED (2026-07-26)** — SI-32 **KEPT** (per user rule "delete if obsolete, else keep"): **not
   obsolete** — requirements unchanged, and it self-supersedes via its own "where this spec and an ADR
   disagree, the ADR wins" clause, so ADR-0014–0028 override its stale sections automatically. No Jira
   action. Its stale **testing** seam is replaced by **ADR-0028** (per-service vertical-slice + Testcontainers
   Postgres + RLS assertions + ADR-0024 contract tests).

## ✅ DONE — #6 Warehouse (resolved 2026-07-26)

**ADR-0021** (new) — **single *virtual* warehouse** per shop; stock stays keyed by `StockUnitId` (no
`WarehouseId`), no allocation logic. Multi-warehouse deferred as a documented seam (the expensive part is
allocation, not the key; no MVP driver at "initially 1"). CONTEXT.md: new **Warehouse** term.

**Likely needed, not yet raised:**
10. ✅ **DONE (2026-07-26)** — **ADR-0024**: contract testing = **schema-snapshot/approval + tolerant-reader
    tests, no Pact** (single-team shared typed package already gives compile-time coupling). **Still open:**
    the per-service **integration-test seam** replacing SI-32's single-context WebApplicationFactory-per-
    aggregate → per-service vertical-slice tests (part of #9).
11. ✅ **DONE (2026-07-26)** — broker = **Amazon SNS + SQS via MassTransit** (**ADR-0022**; amended ADR-0017).
    Standard queues (idempotent+commutative/version-aware consumers, no FIFO). **Still open:** DB hosting per
    service, and the broader **Azure→AWS / Entra→Cognito doc reconciliation** (infrastructure.md, CLAUDE.md,
    ADR-0006/0013/0018) — bundle with #13.
    ✅ **DB hosting DONE (2026-07-26)** — **ADR-0027**: **database-per-service on RDS PostgreSQL** (services
    isolated by separate DBs; tenants by RLS rows within each). **#13 doc sweep partial:** infrastructure.md
    got a **target-architecture banner** (→ ADR-0014–0028) + CONTEXT-MAP.md a planned-decomposition note;
    backend/CLAUDE.md + older-ADR body text NOT rewritten (they truthfully describe the current monolith —
    rewriting to the AWS target would misdescribe running code; reconcile when the code migrates).
12. ✅ **DONE (2026-07-26)** — **ADR-0023**: OpenTelemetry end-to-end; MassTransit propagates W3C
    traceparent + CorrelationId across SNS/SQS; export to **AWS X-Ray via ADOT collector**; Serilog→OTel
    joins logs to traces. Domain Status History stays the audit system-of-record (traces are sampled).
13. **Entra → Cognito IdP drift** — tenancy work adopted AWS Cognito (Pre-Token-Gen Lambda); ADR-0006/0013/0018
    and CONTEXT.md identity lines still say "Entra External ID". Reconcile as one pass.

## How to work

- This is a **grilling** session: ask one question at a time, recommend an answer, wait; look up *facts* in the code/ADRs, put *decisions* to the user. Don't enact until shared understanding.
- Record decisions as **ADRs** (next number: **0019**) and inline **`CONTEXT.md`** glossary updates. Amend existing ADRs with an `## Amendment` section rather than overwriting.
- Surface conflicts with existing ADRs/glossary explicitly before recording.

## Suggested skills

- **`/grill-with-docs`** (runs `/grilling` + `/domain-modeling`) — resume the interview; primary skill for this work.
- **`/domain-modeling`** — for ADR authoring + `CONTEXT.md`/`CONTEXT-MAP.md` maintenance (ADR/CONTEXT formats live in that skill's dir).
- **`/prototype`** — if a tenancy/connection-routing shape needs a concrete artifact to react to.
- Atlassian MCP — to read SI-18/SI-32 and update the spec later (Jira project SI).
