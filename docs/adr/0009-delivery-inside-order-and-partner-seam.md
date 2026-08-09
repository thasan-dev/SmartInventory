# Delivery is a 1:1 fulfilment entity inside the Order; couriers are a non-user roster; partners are a Method seam

**Delivery lives inside the Order aggregate, 1:1.** Marking an Order dispatched / delivered /
failed must move the Order's state, and delivery outcomes *are* those transitions (SI-24), so a
separate Delivery aggregate would force a cross-aggregate transaction for changes that always
happen together. The Delivery is therefore an **entity belonging to the Order**, and an Order
**never splits into multiple Deliveries** — consistent with whole-order dispatch
([ADR-0003](./0003-stock-held-at-order-placement.md)). The Delivery holds fulfilment data only
(Delivery Method, assigned Delivery Person, the Delivery Attempt log) and **references** the
Order's snapshotted address. It has **no state machine of its own**: the Order's state is the
single source of truth, avoiding a "delivery status vs order status" that can drift.

**Retries are logged, not transitions.** A Delivery keeps an ordered **Delivery Attempt** log
(`attemptedAt`, deliveryPerson, outcome, reason). Interim failed attempts append to the log and
**leave the Order in Dispatched**; the Order reaches `Delivered` on a successful attempt or
`Failed` only when the shop gives up. This reconciles SI-24's terminal `Failed` with the
reality that COD delivery often takes several tries.

**Delivery people are a lightweight roster, not Shop Users.** A **Delivery Person** is a
shop-scoped `{name, phone, active}` record. Couriers need no back-office access, Entra login,
or RBAC permission, so making them `Shop User`s ([ADR-0006](./0006-dual-identity-merchant-entra-customer-phone.md))
would be heavyweight and pull them into the merchant CIAM. Assignment to a Delivery is optional
(assignable at or after dispatch). This adds a third, minimal actor type beside Customer and
Shop User.

**The partner seam is a Delivery Method.** The MVP has one method — shop-handled, status set
manually by staff. A future courier partner is simply *another* Delivery Method whose adapter
translates the partner's API/webhooks into the same delivery outcomes; the **Order state
machine is identical regardless of method**. No partner integration is built now — only the
extension point exists.

**Delivery fee is a customer charge on the Order.** A flat, per-order `deliveryFee` lives on
the Order (not the Delivery), defaults from a shop-level setting, is editable and **locked at
Confirmed** (same gate as the manual discount), and joins the COD total
(`Σ line − discounts + deliveryFee`), realised as revenue at Settled. A `Returned` order
refunds the full total including the fee. The shop's internal delivery **cost** is a separate
concern owned by SI-27; only the customer-facing fee is decided here.

Considered and rejected: a **separate Delivery aggregate** — rejected because its lifecycle is
inseparable from the Order's and 1:1, so it would only add cross-aggregate coordination;
**split / multiple deliveries per order** — rejected as incompatible with whole-order dispatch
and out of MVP scope (partial fulfilment joins the returns/partial fog); **couriers as Shop
Users** — rejected as heavyweight identity for people who never use the back office; **a
per-attempt state machine on the Delivery** — rejected in favour of a plain attempt log with
the Order as the single source of truth; **zone/weight-based delivery pricing** — rejected as a
post-MVP refinement in favour of a flat editable fee.

## Amendment (ADR-0014): Delivery becomes its own service with a two-altitude status model

The physical microservice decomposition ([ADR-0014](./0014-physical-microservice-decomposition.md))
overrides the "Delivery has no state machine of its own" position above. Delivery moves out of the
Order aggregate into a separate **Logistics** service that owns an **operational fulfilment state
machine** (assigned → out-for-delivery → attempt log → delivered / failed) and is the source of
truth **for fulfilment operations**. It publishes outcome events; the **Sales** service (which owns
the Order lifecycle and Stock, per ADR-0014) **consumes** them and applies the authoritative Order
transition (`Dispatched` / `Delivered` / `Failed`) **plus every stock and money side effect**
(hold→deduction, COD collection, revenue-at-Settled) **idempotently**.

What this changes vs. the original decision:
- **Two state machines at different altitudes**, not one: fulfilment-ops (Logistics) and Order
  lifecycle (Sales), async-bridged by events. The "drift" the original decision avoided is
  **accepted as eventual status reflection** — a few seconds' lag on fulfilment status is
  low-stakes, unlike the stock/cash invariants, which stay authoritative in Sales and are **never**
  mutated directly by Logistics.
- **Order remains the source of truth for money and stock.** Logistics reports *what happened in
  the field*; Sales decides *what that means for the Order*.
- **New operational obligations:** the outcome events require **idempotency** (a duplicate
  "delivered" must not double-collect COD) and a **lost-message reconciliation sweep** (a dropped
  event must not strand an Order in `Dispatched`). These are consequences of choosing async
  cross-service messaging over the original in-aggregate call.

What still holds: 1:1 Order↔Delivery, no split shipments, Delivery Person as a non-Shop-User
roster, the Delivery Method partner seam, and the delivery **fee** living on the Order (a Sales
concern; the delivery **cost** is Pricing/Cost's). Only the *location* of the Delivery entity and
the *authority model* change.
