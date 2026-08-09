# Dual identity model: Entra for merchants, app-managed phone-OTP for customers

The system has **two distinct identity mechanisms by design**, one per audience:

- **Merchants** authenticate with **email + password via Azure Entra External ID**
  (CIAM; already scaffolded by SI-17). Merchant requests carry an Entra-issued token and
  act on the back-office API, with fine-grained access governed by RBAC permissions
  (SI-29).
- **Customers** use an **app-managed, shop-scoped identity keyed by a phone number
  verified via OTP** (SMS delivered by the SI-28 provider) — entirely separate from
  Entra. Customer requests carry an app-issued session and act only on storefront
  endpoints, on their own shop-scoped data.

**Why two mechanisms:** Customers are shop-scoped (same phone at two shops = two
Customers, per [ADR-0004](./0004-multi-tenant-isolated-storefronts.md)), but an Entra
directory is tenant-global and heavyweight for guests who may never return. An
app-managed phone identity fits shop-scoping natively and keeps the storefront flow
independent of the merchant CIAM.

**Guest vs Registered** is a *status on one phone-keyed Customer record*, not two
identities:
- A verified phone yields a **Guest** Customer — no session, must OTP-verify at **every
  checkout**, delivery details entered per order.
- **Registration** upgrades that same record **in place** (saved delivery address, a
  persistent login session via phone-OTP, no per-order OTP). This dissolves any
  "guest→account linking"/merge problem — there is only ever one record per phone.
- The **register-or-stay-guest** prompt is offered **after order confirmation**, so the
  order is never blocked by an account decision (maximises COD conversion).

**Access levels on the storefront:** browsing the catalogue is **fully public** (no
auth); checkout requires phone (+OTP for guests) and a delivery address; order tracking
is keyed by phone — via an **SMS tracking link** (convenience) and **phone+OTP**
(canonical, retrieves all of that phone's orders). Registered customers see orders in
their logged-in session.

**Merchant/Customer boundary (SI-13):** structural, not a role flag on a shared account.
The two token types on two API surfaces mean a customer token can never satisfy a
back-office endpoint and vice-versa. The same human may be a Merchant of their own shop
*and* a Customer at some shop — those are unrelated identities.

Rejected: a **single IdP / unified `User` with a role discriminator** (fights
shop-scoping and forces guests into a global directory); **customers in Entra**
(directory bloat, tenant-global identity); **phone auth for merchants** (SIM loss locks
an owner out of their business).

## Amendment (SI-24): staff-placed orders waive customer OTP

"OTP-verify at every checkout" applies to **customer-initiated** checkout. SI-24 introduced
**staff-placed orders** — a Shop User taking a phone order on a customer's behalf (created
directly in the `Confirmed` state). These **waive the customer OTP**: a trusted Shop User
holding `manage_orders` vouches for the phone instead of the customer proving possession via
OTP. The Customer remains phone-keyed as usual (a new phone yields a Guest record); only the
per-checkout OTP step is skipped, and the Order's `createdBy` records which Shop User placed
it. The trade-off — a small trust concession on phone ownership for staff-entered orders — is
acceptable because the actor is an authenticated, permissioned Shop User acting within their
own shop.
