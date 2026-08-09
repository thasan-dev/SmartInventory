# Order lifecycle: a manual state machine, and Failed vs Cancelled stock treatment

An Order moves through a **manually driven** state machine:

**Placed → Confirmed → Processing → Dispatched → Delivered → Settled**, with **Cancelled**,
**Failed**, and **Returned** as terminal exits. **Delivered is not terminal** — it opens a
return window; the terminal success state is **Settled**.

Every transition is a deliberate human action by a Shop User holding `manage_orders` (the
Owner always qualifies) — there are **no automatic or timed transitions**, consistent with
the no-expiry Holds of [ADR-0003](./0003-stock-held-at-order-placement.md). The states mean:

- **Placed** — customer submitted via self-serve checkout; no one has contacted them yet.
- **Confirmed** — a Shop User called the customer and they confirmed (COD call-to-confirm
  guard against fake/abandoned orders). The **final price is agreed here**: the manual
  per-order discount (SI-26) may be applied in Confirmed and **nowhere later**; price then
  locks.
- **Processing** — staff are preparing the order; per-order **costing** (SI-27) attaches here.
- **Dispatched** — order has left the shop; entering this state **converts Holds into
  deduction Stock Movements** (ADR-0003).
- **Delivered** — goods handed over **and full COD collected** (inseparable; no separate
  paid state, no partial payment). **Not terminal**: it opens a return window in which the
  collected revenue is *provisional*.
- **Settled** — a Shop User **manually** marks the sale final once the return window has
  passed. **Revenue is realised at Settled, not at Delivered.** There is deliberately **no
  timed auto-settle** — that would reintroduce the scheduler/edge-cases the no-timed-transition
  rule avoids; the manual settle (typically done in bulk) is accepted toil.

**Cancelled vs Failed** — the load-bearing distinction, and why this ADR exists:

- **Cancelled** stops an Order with the goods **still sellable**. Pre-dispatch it releases
  the Hold (no Movement); post-dispatch it posts a **restock** Movement (ADR-0003).
- **Failed** records a **delivery that could not be completed** — customer unreachable,
  wrong address, refused, or goods damaged in transit (reachable only from Dispatched).
  Failed **does not auto-restock**: the dispatch deduction stands and the stock is treated
  as a **loss** by default. If the goods do come back sellable, a Shop User posts a
  **manual restock Movement** separately — never as an automatic side effect of the state.
- **Returned** records a **post-delivery defect return**, reachable only from Delivered
  within the return window. Because COD cash was collected at delivery, a Return **refunds
  it out-of-band** (the shop hands cash back; the system records the refund of the collected
  total — there is no online refund) and yields **no revenue**. It is **shop-mediated**
  (staff inspect and record it, not customer self-serve) and **whole-order only**; stock is
  disposed **manually** as with Failed/Cancelled (resellable → restock Movement, defective →
  write-off).

**Why the asymmetry:** a Cancel is a deliberate recall of known-good stock, so auto-restock
is safe. A Failed delivery leaves the goods in an *unknown* condition (possibly damaged or
lost), so auto-restocking would silently re-add stock that may not exist to sell. Making
recovery an explicit manual act keeps `available` honest and books genuine losses for SI-27's
profit math. All terminal exits (Cancelled, Failed, Returned) carry a mandatory **reason**,
recorded in the Order's **Status History** (the per-transition audit trail: from/to,
timestamp, actor, reason), which also supplies the timestamps downstream tickets read
(Settled-at and Delivered-at for SI-27, Dispatched-at for SI-25).

**Customer reach:** a customer may **self-cancel only while Placed** (only a Hold is at
stake); once Confirmed they have committed on the call, so cancellation is shop-mediated.

**MVP notifications:** three lifecycle SMS fire — an order-confirmation SMS at creation (with
the ADR-0006 tracking link), a **Dispatched** SMS, and a **Delivered** SMS. Other
status-change notifications and templates remain SI-18 fog; the provider is SI-28.

Considered and rejected: **no `Confirmed` state** (fold into Placed) — rejected because the
COD call-to-confirm is a real, distinct operational step where price is agreed; **revenue
realised at Delivered** — rejected in favour of Settled because a delivered order can still be
returned within the window, so its revenue is provisional until settled; **partial returns**
and an **online/automatic refund** — rejected as out of MVP scope (returns are whole-order and
refunds are recorded out-of-band cash handbacks); **a timed auto-settle / configurable return
window** — rejected in favour of a manual settle to preserve the no-timed-transition rule (a
configurable window is a possible post-MVP addition); **Failed auto-restocking for non-damage
reasons** — rejected to avoid inferring goods' condition from the failure reason and re-adding
possibly-damaged stock; **automatic/timed transitions** generally — rejected in favour of a
fully manual flow (ADR-0003).

## Amendment: stock deduction moves from Dispatched to Confirmed

The stock side effect relocates from the **Dispatched** state to the **Confirmed** state (see the
[ADR-0003](./0003-stock-held-at-order-placement.md) amendment and
[ADR-0016](./0016-stock-quantity-ledger-projection-and-concurrency.md)). Revised state effects:

- **Confirmed** — in addition to locking price, **converts the Hold into a deduction Stock Movement**
  (`onHand −= q`; hold released). Staff-placed orders, which enter directly at Confirmed, deduct at
  creation (atomic availability check, no prior hold).
- **Dispatched** — **no longer touches stock**; it only marks that goods have left (and, per the
  ADR-0009 amendment, is driven by Logistics outcomes).

Revised **Cancelled** semantics (boundary now Confirm, not Dispatch):
- **Pre-confirm** (from Placed) — releases the Hold, no Movement.
- **Post-confirm** (Confirmed / Processing / Dispatched, goods sellable) — posts a **restock**
  Movement.

**Failed** is unchanged in spirit — reachable only from Dispatched, **no auto-restock** — but the
deduction it leaves standing is now the **Confirm** deduction, not a dispatch one. **Returned** is
unchanged. The `onHand` ≠ physical-shelf consequence during Confirmed→Dispatched is accepted (see the
ADR-0003 amendment).
