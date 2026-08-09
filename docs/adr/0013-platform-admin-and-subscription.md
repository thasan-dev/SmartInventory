# Platform administration & subscription: a manual operator layer with a Shop lifecycle

SI-29 surfaced a need above the shops — a platform operator who can block a Shop Owner, tied
to a subscription. This resolves that layer. It is a **manual, lightweight** model: **no
automated billing, no payment gateway** (consistent with SI-18's COD-only / no-online-payment
scope). It expands SI-18's original MVP destination to include a platform/subscription layer.

**Platform Super-Admin** — the SaaS operator's staff: a **fourth actor** beside
Shop User/Merchant, Customer, and Delivery Person, and the only one that is **not shop-scoped**
(it operates across all Shops — the deliberate exception to storefront isolation,
[ADR-0004](./0004-multi-tenant-isolated-storefronts.md)). Authenticates via Entra External ID
on a **separate platform-admin surface** (its own token, no `ShopId`), provisioned by the
operator (not self-service), and sits **outside the shop Policy/RBAC system** entirely. A
platform-admin token can never satisfy a shop or storefront endpoint (structural boundary, like
Merchant/Customer in [ADR-0006](./0006-dual-identity-merchant-entra-customer-phone.md)).

**Onboarding is self-register → verify → activate.** The Owner self-registers (Entra),
completes shop details, and requests a subscription; the Platform Super-Admin verifies and
activates. This gives the Shop a **status lifecycle**:

- **Pending Verification** — registered, details done, subscription requested; not public.
- **Active** — SA-approved + subscription valid; storefront live, back office full.
- **Rejected** — SA declined; the **Owner may re-request** (→ Pending).
- **On Hold** — SA block; the Owner **cannot** re-request; **only the SA lifts** it (→ Pending).
- **Deactivated** *(renamed from SI-29's "Suspended")* — an Active shop turned off by
  subscription lapse or SA action: **storefront offline, back office blocked, in-flight orders
  frozen**; SA reactivates (→ Active).

Only **Active + valid subscription** grants full access — this generalises SI-29's shop-active
gate into a **Shop-status gate** (the outermost of the three; see
[ADR-0012](./0012-rbac-mechanics-policies-roles-enforcement.md)).

**Subscription** — ties a Shop (via its Owner) to a **Subscription Tier** and a **validity
period** (`validFrom`/`validUntil`), set and extended **manually by the SA**; **payment is
out-of-band** (bank transfer/cash/invoice — the system records entitlement, not money). **Lapse
is enforced live** at the gate (`Active` AND currently valid) — no scheduler, matching SI-29's
live-check pattern; renewal = SA extends `validUntil`. No grace period (the SA can extend).

**Tiering via feature flags, entitlement owned locally.** A **Subscription Tier** maps to a set
of **feature flags** that gate features. The **MVP ships a single Tier** (all MVP features on),
but the tier→feature-flag mechanism is built now so tiers can be added without rework. The
**local backend database is the source of truth** for access and entitlement (Shop → Tier →
flags); Entra provides **identity only**, and no external billing system owns entitlement.

Considered and rejected: **automated/integrated subscription billing (payment gateway)** —
deferred as a research-backed fast-follow, out of place in a COD/no-online-payment MVP;
**multiple tiers now** — one tier ships, but the mechanism is built for extensibility;
**timed/scheduled lapse enforcement** — rejected for a live gate check (no cron); **a grace
period** — omitted (SA extends `validUntil` instead); **delegating entitlement to Entra or an
external billing system** — rejected; the local DB is the source of truth; **a carve-out
letting Deactivated shops finish in-flight orders** — rejected for simple freeze-all leverage
(the SA warns/extends rather than stranding live orders); **self-service platform-admin
accounts** — rejected; operator-provisioned only.
