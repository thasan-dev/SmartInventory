# Notification: channel follows recipient identity (customers→SMS, merchants/platform→email)

The original MVP scope was **three lifecycle SMS** to customers. The microservice/Accounts work
([ADR-0012](./0012-rbac-mechanics-policies-roles-enforcement.md)/[ADR-0013](./0013-platform-admin-and-subscription.md))
introduced recipients who are **not** phone-identified — staff, the Owner, the Platform Super-Admin — so
**email is net-new**. Rule: **notify on the channel the recipient is identified by.**

## Channel rule + MVP trigger catalog

- **Customers are phone-identified → SMS** (via the SI-28 provider). The three lifecycle messages, unchanged:
  order **confirmation + tracking link** (at placement), **Dispatched**, **Delivered**.
- **Merchants / staff / Platform Super-Admin are email-identified → email.** Net-new:
  - **Staff invite** (Accounts invites a Shop User by email — CONTEXT.md Shop User).
  - **Shop-status decisions to the Owner** — Activated / Rejected / On Hold / Deactivated (ADR-0013).
  - **Subscription expiry / lapse warning** to the Owner (SA-managed validity, ADR-0013).

This is a **rule, not a per-message choice**, and it explains *why* email enters the MVP — it rides in with
the platform/Accounts flows, not the storefront.

## Service shape (already settled) + provider

The **Notification service** consumes domain events from the other services and sends via providers — a
service, trivial CQRS ([ADR-0014](./0014-physical-microservice-decomposition.md)/[ADR-0017](./0017-inter-service-integration-patterns.md)),
owning its own templates. **SMS provider = SI-28.** **Email provider = Amazon SES** (AWS-native, consistent
with the rest of the stack) — the email counterpart of SI-28.

## OTP is *not* a Notification concern

Phone-OTP **delivery** (checkout/login, ADR-0006) stays with the **auth/identity flow**, not this
service: OTP is **synchronous and latency-sensitive** on the login/checkout path, tightly bound to the auth
transaction, whereas Notification is **async, event-driven**. Routing OTP through the event bus would add
latency and coupling for no gain. (Recommended split — revisit if OTP and lifecycle SMS should share one
provider integration.)

## Considered and rejected

- **Per-message channel choice** — rejected for the identity rule (simpler, predictable, matches how each
  actor is reachable).
- **Email to customers** — out of scope: customers are phone-only in the MVP (no customer email captured,
  ADR-0006).
- **OTP inside Notification** — rejected (latency/coupling; see above).

## Relates to

Recipients come from [ADR-0013](./0013-platform-admin-and-subscription.md) (shop-status, subscription) and
Accounts (staff invite); triggers arrive as events over [ADR-0022](./0022-message-broker-sns-sqs-via-masstransit.md);
SMS content/provider detail is SI-28.
</content>
