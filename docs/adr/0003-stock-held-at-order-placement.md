# Stock is held (reserved) at order placement

Placing an order creates a **Hold** on each Stock Unit's quantity after validating that
`available (= on-hand − reserved) ≥ requested`. The hold is the hard oversell guard.
**Dispatch** converts the hold into a deduction Stock Movement; **cancel** (before
dispatch) releases the hold with no movement. Holds **do not auto-expire** — only
dispatch or cancel releases them.

This deliberately goes beyond SI-18's "dispatch deducts stock" baseline, which by itself
would allow two customers to both order the last unit and be resolved by manual cancel.
The shop owner chose to prevent oversell up front rather than clean it up after the fact.

Why no expiry: orders are cash-on-delivery and worked manually, so a real order should
legitimately hold its stock until the shop dispatches or cancels it; auto-expiry would
silently release stock under an active order and complicate the order lifecycle (SI-24).
Stale orders are cleared by the shop cancelling them.

Consequence: `on-hand` alone no longer reflects sellability — reads that gate purchase
must use `available`. Timed expiry and a distinct "available" projection for the storefront
are possible post-MVP additions.

Considered and rejected: no reservation / deduct-at-dispatch only (simplest, oversell
handled manually) — rejected because the shop wants oversell prevented at placement.

## Amendment: the Hold converts to a deduction at Confirm, not Dispatch

The hold→deduction conversion moves from **Dispatch** to **Confirm**. A Hold is still created at
placement (the oversell guard for the Placed→Confirmed window is unchanged), but entering
**Confirmed** converts the hold into a deduction Stock Movement (`onHand −= q`, hold released),
because Confirm is where the sale is committed (price also locks there, ADR-0008). The cancel
boundary moves with it:

- **Pre-confirm** cancel (from Placed) releases the Hold with **no** Movement.
- **Post-confirm** cancel (Confirmed / Processing / Dispatched, goods sellable) posts a **restock**
  Movement — the stock was already deducted at Confirm.

Trade-off accepted: during Confirmed→Dispatched the goods are physically present but already
subtracted from `onHand`, so **`onHand` means "uncommitted / sellable stock," not "physical shelf
count."** This reverses the deduct-at-dispatch rationale originally recorded here and in ADR-0008; it
was chosen so a confirmed COD order is treated as sold at the moment of confirmation. Quantity storage
and concurrency mechanics are in
[ADR-0016](./0016-stock-quantity-ledger-projection-and-concurrency.md).
